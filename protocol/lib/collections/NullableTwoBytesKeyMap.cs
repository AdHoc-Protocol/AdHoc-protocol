//Copyright 2025 Chikirev Sirguy, Unirail Group
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.
//
//For inquiries, please contact: al8v5C6HU4UtqE9@gmail.com
//GitHub Repository: https://github.com/AdHoc-Protocol

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace org.unirail.collections;

///<summary>
///A highly optimized dictionary for nullable 2-byte unmanaged keys (e.g., ushort, char).
///It uses a bitset and a value array ("flat") strategy for dense data and a custom hash table for sparse data.
///This class is not thread-safe.
///</summary>
public class NullableTwoBytesKeyMap<K, V> : IDictionary<K?, V>, IReadOnlyDictionary<K?, V>
    where K : unmanaged
{
    private struct Entry
    {
        public K key;
        public V value;
    }

    private V[]? _flatValues; //Value storage for flat mode.

    private ushort[]? _buckets;
    private Entry[]? _entries;
    private ushort[]? _links; //0-based indices into _entries for collision chains.

    //The _entries array is split into two regions to optimize for non-colliding entries:
    //1. Lo-Region (indices 0 to _loSize-1): Stores entries that have caused a hash collision.
    //2. Hi-Region (indices _entries.Length - _hiSize to _entries.Length-1): Stores entries that have not caused a collision.
    private int _hiSize; //Number of active entries in the high region.
    private int _loSize; //Number of active entries in the low region.

    private int _flatCount;
    private int _version;
    private uint _mask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref ushort GetBucket(uint hashCode) => ref _buckets![hashCode & _mask];

    private bool _hasNullKey;
    private V _nullKeyValue = default!;

    private const int DefaultCapacity = 4;
    protected const int VALUES_SIZE = 0x10000;         //65536
    protected const int NULLS_SIZE = VALUES_SIZE / 64; //1024
    public const int flatStrategyThreshold = 0x7FFF;   //Max capacity for hash phase

    private ulong[]? _nulls; //Bitset for flat mode presence.
    public bool IsFlatStrategy => _nulls != null;

    private KeyCollection? _keys;
    private ValueCollection? _values;

    public NullableTwoBytesKeyMap() : this(0) { }

    public NullableTwoBytesKeyMap(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (Unsafe.SizeOf<K>() != 2)
            throw new InvalidOperationException($"The Key type {typeof(K).Name} is {Unsafe.SizeOf<K>()} bytes, but must be 2 bytes.");

        if (capacity > 0)
            Initialize(capacity);
    }

    public NullableTwoBytesKeyMap(IDictionary<K?, V> dictionary) : this(dictionary?.Count ?? 0)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        if (Unsafe.SizeOf<K>() != 2)
            throw new InvalidOperationException($"The Key type {typeof(K).Name} is {Unsafe.SizeOf<K>()} bytes, but must be 2 bytes.");
        AddRange(dictionary);
    }

    public NullableTwoBytesKeyMap(IEnumerable<KeyValuePair<K?, V>> collection) : this((collection as ICollection<KeyValuePair<K?, V>>)?.Count ?? 0)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (Unsafe.SizeOf<K>() != 2)
            throw new InvalidOperationException($"The Key type {typeof(K).Name} is {Unsafe.SizeOf<K>()} bytes, but must be 2 bytes.");
        AddRange(collection);
    }

    private void AddRange(IEnumerable<KeyValuePair<K?, V>> enumerable)
    {
        foreach (var pair in enumerable)
            Add(pair.Key, pair.Value);
    }

    public int Count => (IsFlatStrategy ? _flatCount : _loSize + _hiSize) + (_hasNullKey ? 1 : 0);

    public V this[K? key]
    {
        get
        {
            if (TryGetValue(key, out var value))
                return value;

            throw new KeyNotFoundException();
        }
        set => TryInsert(key, value, true);
    }

    public void Add(K? key, V value)
    {
        if (!TryInsert(key, value, false))
            throw new ArgumentException("An item with the same key has already been added.");
    }

    void ICollection<KeyValuePair<K?, V>>.Add(KeyValuePair<K?, V> item) => TryInsert(item.Key, item.Value, true);

    private bool TryInsert(K? key, V value, bool overwrite)
    {
        if (!key.HasValue)
        {
            if (_hasNullKey)
            {
                if (!overwrite)
                    return false;
                _nullKeyValue = value;
                return false;
            }

            _hasNullKey = true;
            _nullKeyValue = value;
            _version++;
            return true;
        }

        while (true)
        {
            var kv = key.Value;

            if (IsFlatStrategy)
            {
                var k = toUshort(kv);
                if ((_nulls![k >> 6] & 1UL << k) != 0) //Key exists
                {
                    if (!overwrite)
                        return false;
                    _flatValues![k] = value;
                    return false;
                }

                _nulls[k >> 6] |= 1UL << k;
                _flatValues![k] = value;
                _flatCount++;
                _version++;
                return true;
            }

            if (_buckets == null)
                Initialize(DefaultCapacity);
            else if (_entries!.Length <= _loSize + _hiSize)
            {
                var newSize = _entries.Length * 2;
                if (newSize > flatStrategyThreshold && _entries.Length < flatStrategyThreshold)
                    newSize = flatStrategyThreshold;
                Resize(newSize);
                if (IsFlatStrategy)
                    continue; //Retry with the new flat strategy
            }

            ref var bucket = ref GetBucket((uint)key.GetHashCode());
            var kUshort = toUshort(kv);
            int dstIndex;

            if (bucket == 0) //Bucket is empty: Place new entry in the Hi-Region (non-colliding entries).
                dstIndex = _entries.Length - 1 - _hiSize++;
            else //Collision detected (bucket is not empty).
            {
                //Traverse a collision chain to check for an existing key or find insertion point.
                for (int i = bucket - 1, collisions = 0; ;)
                {
                    ref var entry = ref _entries[i];
                    if (toUshort(entry.key) == kUshort)
                    {
                        //Key found: Overwrite if allowed, otherwise return false (add failed).
                        if (!overwrite)
                            return false;
                        entry.value = value;
                        _version++;   //Update version as value changed.
                        return false; //Return false as no *new* entry was added (it was an update).
                    }

                    if (_loSize <= i)
                        break; //the key is not found, jump to the adding the new entry in the collided chain (Lo-Region)

                    i = _links![i];
                    if (_loSize + 1 < collisions++)
                        throw new InvalidOperationException("Concurrent operations not supported.");
                }

                //Key not found in chain: Add new entry to the Lo-Region (colliding entries).
                if (_links!.Length == (dstIndex = _loSize++))
                    Array.Resize(ref _links, Math.Max(16, Math.Min(_buckets!.Length, _links.Length * 2)));
                _links[dstIndex] = (ushort)(bucket - 1); //New entry links to the previous head of the chain.
            }

            ref var newEntry = ref _entries[dstIndex];
            newEntry.key = kv;
            newEntry.value = value;

            bucket = (ushort)(dstIndex + 1);
            _version++;  //Update version for successful addition.
            return true; //Item successfully added.
        }
    }

    public void Clear()
    {
        if (Count == 0)
            return;

        _hasNullKey = false;
        _nullKeyValue = default!;
        _version++;

        if (IsFlatStrategy)
        {
            _flatCount = 0;
            Array.Clear(_nulls!, 0, _nulls!.Length);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<V>())
                Array.Clear(_flatValues!, 0, _flatValues!.Length);
        }
        else
        {
            if (_buckets != null)
                Array.Clear(_buckets, 0, _buckets.Length);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<V>() && _loSize + _hiSize > 0)
                Array.Clear(_entries!, 0, _entries!.Length);
            _loSize = 0;
            _hiSize = 0;
        }
    }

    public bool ContainsKey(K? key)
    {
        if (!key.HasValue)
            return _hasNullKey;
        var kv = key.Value;
        if (IsFlatStrategy)
            return (_nulls![toUshort(kv) >> 6] & 1UL << toUshort(kv)) != 0;

        if (_loSize + _hiSize == 0)
            return false;

        var kUshort = toUshort(kv);
        var i = GetBucket(kUshort) - 1;
        if (i == -1)
            return false;

        for (var collisions = 0; ;)
        {
            if (toUshort(_entries![i].key) == kUshort)
                return true;

            if (_loSize <= i)
                return false; //terminal entity
            i = _links![i];

            if (_loSize < ++collisions)
                throw new InvalidOperationException("Hash collision chain is unexpectedly long. Possible data corruption.");
        }
    }

    public bool ContainsValue(V value)
    {
        if (_hasNullKey && EqualityComparer<V>.Default.Equals(_nullKeyValue, value))
            return true;

        if (IsFlatStrategy)
        {
            var comparer = EqualityComparer<V>.Default;
            for (var i = 0; i < NULLS_SIZE; i++)
            {
                var bits = _nulls![i];
                if (bits == 0)
                    continue; //Skip empty 64-bit segments

                var baseIndex = i << 6; //i * 64
                while (bits != 0)
                {
                    var bitPos = BitOperations.TrailingZeroCount(bits);
                    var index = baseIndex + bitPos;
                    if (comparer.Equals(_flatValues![index], value))
                        return true;
                    bits &= bits - 1; //Clear the least significant set bit
                }
            }

            return false;
        }

        //Iterate lo-region
        for (var i = 0; i < _loSize; i++)
            if (EqualityComparer<V>.Default.Equals(_entries![i].value, value))
                return true;
        if (_hiSize == 0)
            return false;

        //Iterate hi-region
        for (var i = _entries!.Length - _hiSize; i < _entries.Length; i++)
            if (EqualityComparer<V>.Default.Equals(_entries![i].value, value))
                return true;

        return false;
    }

    public bool TryGetValue(K? key, [MaybeNullWhen(false)] out V value)
    {
        if (!key.HasValue)
        {
            if (_hasNullKey)
            {
                value = _nullKeyValue;
                return true;
            }

            value = default;
            return false;
        }

        var kv = key.Value;
        if (IsFlatStrategy)
        {
            var k = toUshort(kv);
            if ((_nulls![k >> 6] & 1UL << k) != 0)
            {
                value = _flatValues![k];
                return true;
            }

            value = default;
            return false;
        }

        if (_loSize + _hiSize == 0)
        {
            value = default;
            return false;
        }

        var kUshort = toUshort(kv);
        var i = GetBucket(kUshort) - 1;
        if (i == -1)
        {
            value = default;
            return false;
        }

        for (var collisions = 0; ;)
        {
            ref var entry = ref _entries![i];
            if (toUshort(entry.key) == kUshort)
            {
                value = entry.value;
                return true;
            }

            if (_loSize <= i)
            {
                value = default;
                return false;
            }

            i = _links![i];

            if (_loSize < ++collisions)
                throw new InvalidOperationException("Hash collision chain is unexpectedly long. Possible data corruption.");
        }
    }

    public void CopyTo(KeyValuePair<K?, V>[] dst, int dstIndex)
    {
        ArgumentNullException.ThrowIfNull(dst);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
        if (dst.Length - dstIndex < Count)
            throw new ArgumentException("Destination array is not long enough.");

        if (_hasNullKey)
            dst[dstIndex++] = new KeyValuePair<K?, V>(null, _nullKeyValue);

        if (IsFlatStrategy)
            for (var i = -1; (i = next1(i)) != -1;)
                dst[dstIndex++] = new KeyValuePair<K?, V>(Unsafe.As<int, K>(ref i), _flatValues![i]);
        else
        {
            for (var i = 0; i < _loSize; i++)
            {
                ref var entry = ref _entries![i];
                dst[dstIndex++] = new KeyValuePair<K?, V>(entry.key, entry.value);
            }

            for (int i = 0, j = _entries!.Length - _hiSize; i < _hiSize; i++, j++)
            {
                ref var entry = ref _entries![j];
                dst[dstIndex++] = new KeyValuePair<K?, V>(entry.key, entry.value);
            }
        }
    }

    private int Initialize(int capacity)
    {
        _version++;
        _flatCount = 0;

        if (flatStrategyThreshold < capacity)
        {
            _nulls = new ulong[NULLS_SIZE];
            _flatValues = new V[VALUES_SIZE];
            _buckets = null;
            _entries = null;
            _links = null;
            _loSize = _hiSize = 0;
            return VALUES_SIZE;
        }

        _nulls = null;
        _flatValues = null;

        var size = GetPowerOfTwo(capacity);
        _buckets = new ushort[size];
        _entries = new Entry[size];
        _links = new ushort[Math.Min(size, 16)];
        _loSize = _hiSize = 0;
        _mask = (uint)(size - 1);

        return size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetPowerOfTwo(int capacity) => capacity <= DefaultCapacity ? DefaultCapacity : (int)BitOperations.RoundUpToPowerOf2((uint)capacity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Resize(int newSize)
    {
        _version++;

        if (IsFlatStrategy)
        {
            if (flatStrategyThreshold < newSize)
                return;
            var oldNulls = _nulls!;
            var oldFlatValues = _flatValues!;
            Initialize(newSize);
            for (var token = -1; (token = next1(token, oldNulls)) != -1;)
            {
                Copy(Unsafe.As<int, K>(ref token), oldFlatValues[token]);
            }

            return;
        }

        if (flatStrategyThreshold < newSize)
        {
            _nulls = new ulong[NULLS_SIZE];
            _flatValues = new V[VALUES_SIZE];

            for (var i = 0; i < _loSize; i++)
            {
                ref var entry = ref _entries![i];
                var index = toUshort(entry.key);
                _nulls[index >> 6] |= 1UL << index;
                _flatValues[index] = entry.value;
            }

            if (0 < _hiSize)
                for (var i = _entries!.Length - _hiSize; i < _entries.Length; i++)
                {
                    ref var entry = ref _entries![i];
                    var index = toUshort(entry.key);
                    _nulls[index >> 6] |= 1UL << index;
                    _flatValues[index] = entry.value;
                }

            _flatCount = _loSize + _hiSize;
            _buckets = null;
            _entries = null;
            _links = null;
            _loSize = 0;
            _hiSize = 0;
            return;
        }

        //Hash-to-hash resize
        var oldEntries = _entries;
        var oldLoSize = _loSize;
        var oldHiSize = _hiSize;
        if (_links.Length < 0xFF && _links.Length < _buckets.Length)
            _links = _buckets; //reuse buckets as links
        Initialize(newSize);

        for (var i = 0; i < oldLoSize; i++)
            Copy(oldEntries![i].key, oldEntries[i].value);
        for (var i = oldEntries!.Length - oldHiSize; i < oldEntries.Length; i++)
            Copy(oldEntries[i].key, oldEntries[i].value);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(K key, V value)
    {
        ref var bucket = ref GetBucket(toUshort(key));
        var i = bucket - 1;
        int dstIndex;

        if (i == -1) //Empty bucket, insert into hi-region
            dstIndex = _entries!.Length - 1 - _hiSize++;
        else //Collision, insert into lo-region
        {
            if (_links!.Length == (dstIndex = _loSize++))
                Array.Resize(ref _links, Math.Max(16, Math.Min(_buckets!.Length, _links.Length * 2)));
            _links[dstIndex] = (ushort)i;
        }

        ref var entry = ref _entries![dstIndex];
        entry.key = key;
        entry.value = value;
        bucket = (ushort)(dstIndex + 1);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Move(int src, int dst, ref Entry dstEntry)
    {
        if (src == dst)
            return;

        ref var srcEntry = ref _entries![src];
        ref var srcBucket = ref GetBucket(toUshort(srcEntry.key));
        var index = srcBucket - 1;

        if (index == src) //The bucket points to it
            srcBucket = (ushort)(dst + 1);
        else //A link points to it. Find that link.
        {
            ref var link = ref _links![index];
            for (; link != src; link = ref _links![index])
                index = link;

            link = (ushort)dst;
        }

        if (src < _loSize)
            _links![dst] = _links![src];

        dstEntry = srcEntry;
    }

    public bool Remove(K? key)
    {
        if (!key.HasValue)
        {
            if (!_hasNullKey)
                return false;
            _hasNullKey = false;
            _nullKeyValue = default!;
            _version++;
            return true;
        }

        var kv = key.Value;

        if (IsFlatStrategy)
        {
            var k = toUshort(kv);
            ref var bitword = ref _nulls![k >> 6];
            var t = bitword;
            if (t == (bitword &= ~(1UL << k)))
                return false;

            if (RuntimeHelpers.IsReferenceOrContainsReferences<V>())
                _flatValues![k] = default!;
            _flatCount--;
            _version++;
            return true;
        }

        if (_loSize + _hiSize == 0)
            return false;

        var kUshort = toUshort(kv);
        ref var bucket = ref GetBucket(kUshort);
        var removeIndex = bucket - 1;
        if (removeIndex == -1)
            return false;

        ref var removeEntry = ref _entries![removeIndex];

        if (_loSize <= removeIndex) //In hi-region
        {
            if (toUshort(removeEntry.key) != kUshort)
                return false;

            Move(_entries.Length - _hiSize, removeIndex, ref removeEntry);
            _hiSize--;
            bucket = 0;
            _version++;
            return true;
        }

        //In lo-region
        ref var link = ref _links![removeIndex];
        if (toUshort(removeEntry.key) == kUshort)
            bucket = (ushort)(link + 1);
        else
        {
            var last = removeIndex;
            ref var lastEntry = ref removeEntry;

            if (toUshort((removeEntry = ref _entries![removeIndex = (int)link]).key) == kUshort) //The key is found at the second node
                if (removeIndex < _loSize)
                    link = _links[removeIndex]; //'SecondNode' is in 'lo Region', relink to bypasses 'SecondNode'
                else
                {
                    removeEntry = lastEntry;
                    removeEntry = ref lastEntry;
                    bucket = (ushort)(removeIndex + 1);
                    removeIndex = last;
                }
            else if (_loSize <= removeIndex)
                return false;
            else
                for (var collisions = 0; ;)
                {
                    lastEntry = ref removeEntry;
                    ref var prevLink = ref link;

                    if (toUshort((removeEntry = ref _entries![removeIndex = (int)(link = ref _links![last = removeIndex])]).key) == kUshort)
                    {
                        if (removeIndex < _loSize)
                            link = _links[removeIndex];
                        else
                        {
                            removeEntry = lastEntry;
                            removeEntry = ref lastEntry;
                            prevLink = (ushort)removeIndex;
                            removeIndex = last;
                        }

                        break;
                    }

                    if (_loSize <= removeIndex)
                        return false;
                    if (_loSize + 1 < collisions++)
                        throw new InvalidOperationException("Concurrent operations not supported.");
                }
        }

        Move(_loSize - 1, removeIndex, ref removeEntry);
        _loSize--;
        _version++;
        return true;
    }

    bool ICollection<KeyValuePair<K?, V>>.Remove(KeyValuePair<K?, V> item) => TryGetValue(item.Key, out var value) && EqualityComparer<V>.Default.Equals(value, item.Value) && Remove(item.Key);

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<KeyValuePair<K?, V>> IEnumerable<KeyValuePair<K?, V>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<KeyValuePair<K?, V>>
    {
        private readonly NullableTwoBytesKeyMap<K, V> _map;
        private int _version;
        private int _index; //Tracks position: -2=before null key, -1=null key, 0 to _loSize-1=lo-region, _entries.Length-_hiSize to _entries.Length-1=hi-region, or flat index
        private KeyValuePair<K?, V> _current;

        internal Enumerator(NullableTwoBytesKeyMap<K, V> map)
        {
            _map = map;
            _version = map._version;
            _index = map._hasNullKey ? -2 : -1;
            _current = default;
        }

        public bool MoveNext()
        {
            if (_version != _map._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            if (_map.Count == 0)
                return false;

            if (++_index == -1)
            {
                _current = new KeyValuePair<K?, V>(null, _map._nullKeyValue);
                return true;
            }

            if (_index == int.MaxValue)
            {
                _index = int.MaxValue - 1;
                return false;
            }

            if (_map.IsFlatStrategy)
            {
                var nextBit = _map.next1(_index);
                if (nextBit != -1)
                {
                    _current = new KeyValuePair<K?, V>(Unsafe.As<int, K>(ref nextBit), _map._flatValues![nextBit]);
                    _index = nextBit;
                    return true;
                }

                _index = int.MaxValue - 1;
                return false;
            }

            if (_index == _map._loSize)
            {
                if (_map._hiSize == 0)
                {
                    _index = int.MaxValue - 1;
                    return false;
                }

                _index = _map._entries!.Length - _map._hiSize; //Jump to hi-region
            }

            if (_index == _map._entries!.Length)
            {
                _index = int.MaxValue - 1;
                return false;
            }

            ref var entry = ref _map._entries[_index];
            _current = new KeyValuePair<K?, V>(entry.key, entry.value);
            return true;
        }

        public KeyValuePair<K?, V> Current => _version != _map._version ? throw new InvalidOperationException("Collection was modified during enumeration.") : _index == int.MaxValue - 1 || _index == -2 || _index == -1 && !_map._hasNullKey ? throw new InvalidOperationException("Enumeration has either not started or has finished.")
                                                                                                                                                                                                                                               : _current;

        object? IEnumerator.Current => Current;

        public void Reset()
        {
            _version = _map._version;
            _index = _map._hasNullKey ? -2 : -1;
            _current = default;
        }

        public void Dispose() { }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort toUshort(K key) => Unsafe.As<K, ushort>(ref key);
    #region Flat Strategy Helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected int next1(int bit) => next1(bit, _nulls!);

    protected static int next1(int bit, ulong[] nulls)
    {
        if (0xFFFF <= bit)
            return -1;
        var i = ++bit;
        var index = i >> 6;
        if (index >= nulls.Length)
            return -1;
        var value = nulls[index] & ~0UL << (i & 63);

        while (value == 0)
            if (++index == NULLS_SIZE)
                return -1;
            else
                value = nulls[index];

        return (index << 6) + BitOperations.TrailingZeroCount(value);
    }
    #endregion
    #region Dictionary Properties
    public bool IsReadOnly => false;
    public ICollection<K?> Keys => _keys ??= new KeyCollection(this);
    public ICollection<V> Values => _values ??= new ValueCollection(this);
    IEnumerable<K?> IReadOnlyDictionary<K?, V>.Keys => Keys;
    IEnumerable<V> IReadOnlyDictionary<K?, V>.Values => Values;

    bool ICollection<KeyValuePair<K?, V>>.Contains(KeyValuePair<K?, V> item) => TryGetValue(item.Key, out var value) && EqualityComparer<V>.Default.Equals(value, item.Value);
    #endregion

    public int EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        var currentCapacity = _entries?.Length ?? (IsFlatStrategy ? VALUES_SIZE : 0);
        if (currentCapacity >= capacity)
            return currentCapacity;
        _version++;
        if (_buckets == null && !IsFlatStrategy)
            return Initialize(capacity);
        var newSize = GetPowerOfTwo(capacity);
        Resize(newSize);
        return newSize;
    }

    public void TrimExcess()
    {
        var count = Count;
        if (count == 0)
        {
            Initialize(0);
            return;
        }

        if (IsFlatStrategy)
        {
            if (count <= flatStrategyThreshold)
                Resize(GetPowerOfTwo(count));
        }
        else if (_entries != null)
        {
            var newSize = GetPowerOfTwo(count);
            if (newSize < _entries.Length)
                Resize(newSize);
        }
    }
    #region Collection Wrappers
    [DebuggerDisplay("Count = {Count}")]
    public sealed class KeyCollection : ICollection<K?>, IReadOnlyCollection<K?>
    {
        private readonly NullableTwoBytesKeyMap<K, V> map;
        public KeyCollection(NullableTwoBytesKeyMap<K, V> dictionary) => map = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        public int Count => map.Count;
        public bool IsReadOnly => true;

        public void CopyTo(K?[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
            if (dst.Length - dstIndex < map.Count)
                throw new ArgumentException("Destination array is not long enough.");
            foreach (var item in map)
                dst[dstIndex++] = item.Key;
        }

        public bool Contains(K? item) => map.ContainsKey(item);
        public Enumerator GetEnumerator() => new(map);
        IEnumerator<K?> IEnumerable<K?>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        void ICollection<K?>.Add(K? item) => throw new NotSupportedException();
        bool ICollection<K?>.Remove(K? item) => throw new NotSupportedException();
        void ICollection<K?>.Clear() => throw new NotSupportedException();

        public struct Enumerator : IEnumerator<K?>
        {
            private NullableTwoBytesKeyMap<K, V>.Enumerator _dictEnumerator;
            internal Enumerator(NullableTwoBytesKeyMap<K, V> dictionary) => _dictEnumerator = dictionary.GetEnumerator();
            public bool MoveNext() => _dictEnumerator.MoveNext();
            public K? Current => _dictEnumerator.Current.Key;
            object? IEnumerator.Current => Current;
            public void Dispose() => _dictEnumerator.Dispose();
            public void Reset() => _dictEnumerator.Reset();
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    public sealed class ValueCollection : ICollection<V>, IReadOnlyCollection<V>
    {
        private readonly NullableTwoBytesKeyMap<K, V> map;
        public ValueCollection(NullableTwoBytesKeyMap<K, V> dictionary) => map = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        public int Count => map.Count;
        public bool IsReadOnly => true;

        public void CopyTo(V[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
            if (dst.Length - dstIndex < map.Count)
                throw new ArgumentException("Destination array is not long enough.");
            foreach (var item in map)
                dst[dstIndex++] = item.Value;
        }

        public bool Contains(V item) => map.ContainsValue(item);

        public Enumerator GetEnumerator() => new(map);
        IEnumerator<V> IEnumerable<V>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        void ICollection<V>.Add(V item) => throw new NotSupportedException();
        bool ICollection<V>.Remove(V item) => throw new NotSupportedException();
        void ICollection<V>.Clear() => throw new NotSupportedException();

        public struct Enumerator : IEnumerator<V>
        {
            private NullableTwoBytesKeyMap<K, V>.Enumerator _dictEnumerator;
            internal Enumerator(NullableTwoBytesKeyMap<K, V> dictionary) => _dictEnumerator = dictionary.GetEnumerator();
            public bool MoveNext() => _dictEnumerator.MoveNext();
            public V Current => _dictEnumerator.Current.Value;
            object? IEnumerator.Current => Current;
            public void Dispose() => _dictEnumerator.Dispose();
            public void Reset() => _dictEnumerator.Reset();
        }
    }
    #endregion
}