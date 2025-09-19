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
using System.Numerics;
using System.Runtime.CompilerServices;

namespace org.unirail.collections;

///<summary>
///A highly optimized set for nullable 2-byte unmanaged types (e.g., ushort, char).
///It uses a bitset ("flat") strategy for dense data and a custom hash table for sparse data.
///This class is not thread-safe.
///</summary>
public class NullableTwoBytesSet<K> : ISet<K?>, IReadOnlySet<K?>
    where K : unmanaged
{
    private ushort[]? _buckets;
    private K[]? _keys;
    private ushort[]? _links; //0-based indices into _keys for collision chains.

    //The _keys array is split into two regions to optimize for non-colliding entries:
    //1. Lo-Region (indices 0 to _loSize-1): Stores keys that have caused a hash collision.
    //2. Hi-Region (indices _keys.Length - _hiSize to _keys.Length-1): Stores keys that have not caused a collision.
    private int _hiSize; //Number of active entries in the high region.
    private int _loSize; //Number of active entries in the low region.

    private int _flatCount;
    private int _version;
    private uint _mask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref ushort GetBucket(uint hashCode) => ref _buckets![hashCode & _mask];

    private bool _hasNullKey;

    private const int DefaultCapacity = 4;
    protected const int VALUES_SIZE = 0x10000;         //65536
    protected const int NULLS_SIZE = VALUES_SIZE / 64; //1024
    public const int flatStrategyThreshold = 0x7FFF;   //Max capacity for hash phase

    private ulong[]? _nulls; //Bitset for flat mode. A set bit at index `i` means the value `i` is present.
    public bool IsFlatStrategy => _nulls != null;

    public NullableTwoBytesSet() : this(0) { }

    public NullableTwoBytesSet(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (Unsafe.SizeOf<K>() != 2)
            throw new InvalidOperationException($"The Key type {typeof(K).Name} is {Unsafe.SizeOf<K>()} bytes, but must be 2 bytes.");

        if (capacity > 0)
            Initialize(capacity);
    }

    public NullableTwoBytesSet(IEnumerable<K?> collection) : this(collection is ICollection<K?> coll ? coll.Count : 0)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (Unsafe.SizeOf<K>() != 2)
            throw new InvalidOperationException($"The Key type {typeof(K).Name} is {Unsafe.SizeOf<K>()} bytes, but must be 2 bytes.");
        AddRange(collection);
    }

    private void AddRange(IEnumerable<K?> enumerable)
    {
        foreach (var key in enumerable)
            Add(key);
    }

    public int Count => (IsFlatStrategy ? _flatCount : _loSize + _hiSize) + (_hasNullKey ? 1 : 0);

    public bool Add(K? key)
    {
        if (!key.HasValue)
        {
            if (_hasNullKey)
                return false;
            _hasNullKey = true;
            _version++;
            return true;
        }

        while (true)
        {
            var kv = key.Value;

            if (IsFlatStrategy)
            {
                var i = toUshort(kv);
                ref var bitword = ref _nulls![i >> 6];
                var t = bitword;
                if (t == (bitword |= 1UL << i))
                    return false;
                _flatCount++;
                _version++;
                return true;
            }

            if (_buckets == null)
                Initialize(DefaultCapacity);

            if (_keys!.Length <= _loSize + _hiSize)
            {
                var newSize = _keys.Length * 2;
                if (newSize > flatStrategyThreshold && _keys.Length < flatStrategyThreshold)
                    newSize = flatStrategyThreshold;
                Resize(newSize);
                if (IsFlatStrategy)
                    continue; //Retry with the new flat strategy
            }

            var kUshort = toUshort(kv);
            ref var bucket = ref GetBucket(kUshort);
            var index = bucket - 1;

            int dstIndex;

            if (index == -1) //Bucket is empty: place new entry in hi-region
                dstIndex = _keys.Length - 1 - _hiSize++;
            else
            {
                for (int i = index, collisions = 0; ;)
                {
                    if (toUshort(_keys[i]) == kUshort)
                        return false;

                    if (_loSize <= i)
                        break;
                    i = _links![i];

                    if (_loSize + 1 < collisions++)
                        throw new InvalidOperationException("Concurrent operations not supported.");
                }

                if (_links!.Length == (dstIndex = _loSize++))
                    Array.Resize(ref _links, Math.Max(16, Math.Min(_buckets!.Length, _links.Length * 2)));

                _links[dstIndex] = (ushort)index;
            }

            _keys[dstIndex] = kv;
            bucket = (ushort)(dstIndex + 1);
            _version++;
            return true;
        }
    }

    void ICollection<K?>.Add(K? key) => Add(key);

    public void Clear()
    {
        if (Count == 0)
            return;

        _hasNullKey = false;
        _version++;

        if (IsFlatStrategy)
        {
            _flatCount = 0;
            Array.Clear(_nulls!, 0, _nulls!.Length);
        }
        else
        {
            if (_buckets != null)
                Array.Clear(_buckets, 0, _buckets.Length);
            _loSize = 0;
            _hiSize = 0;
        }
    }

    public bool Contains(K? key)
    {
        if (!key.HasValue)
            return _hasNullKey;
        var kv = key.Value;
        if (IsFlatStrategy)
            return exists(kv);

        if (_loSize + _hiSize == 0)
            return false;

        var kUshort = toUshort(kv);
        var i = GetBucket(kUshort) - 1;
        if (i == -1)
            return false;

        for (var collisions = 0; ;)
        {
            if (toUshort(_keys![i]) == kUshort)
                return true;

            if (_loSize <= i)
                return false; //terminal entity
            i = _links![i];

            if (_loSize < ++collisions)
                throw new InvalidOperationException("Hash collision chain is unexpectedly long. Possible data corruption.");
        }
    }

    public void CopyTo(K?[] dst, int dstIndex)
    {
        ArgumentNullException.ThrowIfNull(dst);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
        if (dst.Length - dstIndex < Count)
            throw new ArgumentException("Destination array is not long enough.");

        if (_hasNullKey)
            dst[dstIndex++] = null;

        if (IsFlatStrategy)
            for (var i = -1; (i = next1(i)) != -1;)
                dst[dstIndex++] = Unsafe.As<int, K>(ref i);
        else
        {
            //Copy lo-region
            for (var i = 0; i < _loSize; i++)
                dst[dstIndex++] = _keys[i];
            //Copy hi-region
            for (var i = _keys.Length - _hiSize; i < _keys.Length; i++)
                dst[dstIndex++] = _keys[i];
        }
    }

    private int Initialize(int capacity)
    {
        _version++;
        _flatCount = 0;

        if (flatStrategyThreshold < capacity)
        {
            _nulls = new ulong[NULLS_SIZE];
            _buckets = null;
            _keys = null;
            _links = null;
            _loSize = _hiSize = 0;
            return VALUES_SIZE;
        }

        _nulls = null;

        var size = GetPowerOfTwo(capacity);
        _buckets = new ushort[size];
        _keys = new K[size];
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
            Initialize(newSize);
            for (var token = -1; (token = next1(token, oldNulls)) != -1;)
                Copy(Unsafe.As<int, K>(ref token));
            return;
        }

        if (flatStrategyThreshold < newSize)
        {
            _nulls = new ulong[NULLS_SIZE];
            //Copy lo-region
            for (var i = 0; i < _loSize; i++)
                exists1(_keys![i], _nulls);
            //Copy hi-region
            for (var i = _keys!.Length - _hiSize; i < _keys.Length; i++)
                exists1(_keys[i], _nulls);

            _flatCount = _loSize + _hiSize;
            _buckets = null;
            _keys = null;
            _links = null;
            _loSize = 0;
            _hiSize = 0;
            return;
        }

        //Hash-to-hash resize
        var oldKeys = _keys;
        var oldLoSize = _loSize;
        var oldHiSize = _hiSize;
        if (_links.Length < 0xFF && _links.Length < _buckets.Length)
            _links = _buckets; //reuse buckets as links
        Initialize(newSize);

        //Re-insert old elements
        for (var i = 0; i < oldLoSize; i++)
            Copy(oldKeys![i]);
        for (var i = oldKeys!.Length - oldHiSize; i < oldKeys.Length; i++)
            Copy(oldKeys[i]);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(K key)
    {
        ref var bucket = ref GetBucket(toUshort(key));
        var i = bucket - 1;
        int dstIndex;

        if (i == -1) //Empty bucket, insert into hi-region
            dstIndex = _keys!.Length - 1 - _hiSize++;
        else //Collision, insert into lo-region
        {
            if (_links!.Length == (dstIndex = _loSize++))
                Array.Resize(ref _links, Math.Max(16, Math.Min(_buckets!.Length, _links.Length * 2)));
            _links[dstIndex] = (ushort)i;
        }

        _keys![dstIndex] = key;
        bucket = (ushort)(dstIndex + 1);
    }

    //Moves data from src index to dst index and updates all links pointing to src.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Move(int src, int dst, ref K dstKey)
    {
        if (src == dst)
            return;

        //Get a ref to the key being moved to avoid repeated array lookups.
        ref var srcKey = ref _keys![src];
        ref var srcBucket = ref GetBucket(toUshort(srcKey));
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

        //Assign to the ref parameter using the ref local, which directly modifies the original array slot.
        dstKey = srcKey;
    }

    public bool Remove(K? key)
    {
        if (!key.HasValue)
        {
            if (!_hasNullKey)
                return false;
            _hasNullKey = false;
            _version++;
            return true;
        }

        var kv = key.Value;

        if (IsFlatStrategy)
        {
            var i = toUshort(kv);
            ref var bitword = ref _nulls![i >> 6];
            var t = bitword;
            if (t == (bitword &= ~(1UL << i)))
                return false;

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

        ref var removeKey = ref _keys![removeIndex];

        if (_loSize <= removeIndex)
        {
            if (toUshort(removeKey) != kUshort)
                return false;

            Move(_keys.Length - _hiSize, removeIndex, ref removeKey);
            _hiSize--;
            bucket = 0;
            _version++;
            return true;
        }

        ref var link = ref _links![removeIndex];
        if (toUshort(removeKey) == kUshort) //Head of chain matches
            bucket = (ushort)(link + 1);    //Point bucket to the next item in the chain.
        else
        {
            ref var lastKey = ref removeKey;
            var last = removeIndex;
            if (toUshort(removeKey = ref _keys![removeIndex = link]) == kUshort) //The key is found at the second node
                if (removeIndex < _loSize)
                    link = _links[removeIndex]; //'SecondNode' is in 'lo Region', relink to bypasses 'SecondNode'
                else
                {
                    removeKey = _keys[last]; //Copies `keys[last]` to `keys[removeIndex]`
                    bucket = (ushort)(removeIndex + 1);
                    removeKey = ref _keys![removeIndex = last];
                }
            else if (_loSize <= removeIndex)
                return false;
            else
                for (var collisions = 0; ;)
                {
                    lastKey = ref removeKey;
                    ref var prevLink = ref link;

                    if (toUshort(removeKey = ref _keys![removeIndex = link = ref _links![last = removeIndex]]) == kUshort)
                    {
                        if (removeIndex < _loSize)
                            link = _links[removeIndex];
                        else
                        {
                            removeKey = lastKey;
                            removeKey = ref lastKey;
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

        Move(_loSize - 1, removeIndex, ref removeKey);
        _loSize--;
        _version++;
        return true;
    }

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<K?> IEnumerable<K?>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<K?>
    {
        private readonly NullableTwoBytesSet<K> _set;
        private int _version;

        private int _index; //-2=before null key, -1=at null key, 0=before flat/low iteration,

        //1 to _flatCount/_loSize=flat/low region, _keys.Length - _hiSize to _keys.Length - 1=high region,
        //int.MaxValue - 1=end
        private K? _current;
        private int _bitIndex; //Tracks bit position for flat strategy iteration

        internal Enumerator(NullableTwoBytesSet<K> set)
        {
            _set = set;
            _version = set._version;
            _index = set._hasNullKey ? -2 : -1;
            _current = default;
            _bitIndex = -1; //Initialize for flat strategy
        }

        public bool MoveNext()
        {
            if (_version != _set._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            if (_set.Count == 0)
                return false;

            if (++_index == -1)
            {
                _current = null;
                return _set._hasNullKey; //Only return true if null key exists
            }

            if (_index == int.MaxValue || (_set.IsFlatStrategy ? _set._flatCount : _set._loSize + _set._hiSize) == 0)
            {
                _index = int.MaxValue - 1;
                return false;
            }

            if (_set.IsFlatStrategy)
            {
                //Flat strategy: Find next set bit
                _bitIndex = _set.next1(_bitIndex);
                if (_bitIndex != -1)
                {
                    _current = Unsafe.As<int, K>(ref _bitIndex);
                    return true;
                }

                _index = int.MaxValue - 1; //No more bits
                return false;
            }

            if (_index == _set._loSize)
            {
                if (_set._hiSize == 0)
                {
                    _index = int.MaxValue - 1;
                    return false;
                }

                _index = _set._keys!.Length - _set._hiSize; //Jump to high region
            }

            if (_index >= _set._keys!.Length)
            {
                _index = int.MaxValue - 1;
                return false;
            }

            _current = _set._keys[_index];
            return true;
        }

        public K? Current => _version != _set._version ? throw new InvalidOperationException("Collection was modified during enumeration.") : _index == int.MaxValue - 1 || _index == -2 || _index == -1 && !_set._hasNullKey ? throw new InvalidOperationException("Enumeration has either not started or has finished.")
                                                                                                                                                                                                                              : _current;

        object? IEnumerator.Current => Current;

        public void Reset()
        {
            _version = _set._version;
            _index = _set._hasNullKey ? -2 : -1;
            _current = default;
            _bitIndex = -1;
        }

        public void Dispose() { }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort toUshort(K key) => Unsafe.As<K, ushort>(ref key);
    #region Flat Strategy Helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool exists(K key) => (_nulls![toUshort(key) >> 6] & 1UL << toUshort(key)) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void exists1(K key, ulong[] nulls) => nulls[toUshort(key) >> 6] |= 1UL << toUshort(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected int next1(int bit) => next1(bit, _nulls!);

    protected static int next1(int bit, ulong[] nulls)
    {
        if (0xFFFF <= bit)
            return -1;
        var i = ++bit;
        var index = i >> 6;
        var value = nulls![index] & ~0UL << (i & 63);

        while (value == 0)
            if (++index == NULLS_SIZE)
                return -1;
            else
                value = nulls[index];

        return (index << 6) + BitOperations.TrailingZeroCount(value);
    }

    //Helper to count set bits in _nulls for flat strategy
    private int CalculateFlatCount()
    {
        var count = 0;
        for (var i = 0; i < NULLS_SIZE; i++)
            count += BitOperations.PopCount(_nulls![i]);
        return count;
    }
    #endregion
    #region ISet Explicit / Remaining Implementation
    public bool IsReadOnly => false;

    public void ExceptWith(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Count == 0)
            return;
        if (other is NullableTwoBytesSet<K> otherSet && otherSet.Count == 0)
            return;

        if (IsFlatStrategy && other is NullableTwoBytesSet<K> { IsFlatStrategy: true } otherTbs)
        {
            for (var i = 0; i < NULLS_SIZE; i++)
                _nulls![i] &= ~otherTbs._nulls![i];
            _hasNullKey &= !otherTbs._hasNullKey;
            _flatCount = CalculateFlatCount();
            _version++;
            return;
        }

        foreach (var key in other)
            Remove(key);
    }

    public void IntersectWith(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (IsFlatStrategy && other is NullableTwoBytesSet<K> { IsFlatStrategy: true } otherTbs)
        {
            for (var i = 0; i < NULLS_SIZE; i++)
                _nulls![i] &= otherTbs._nulls![i];
            _hasNullKey &= otherTbs._hasNullKey;
            _flatCount = CalculateFlatCount();
            _version++;
            return;
        }

        var t = new NullableTwoBytesSet<K>();
        foreach (var key in other)
            if (Contains(key))
                t.Add(key);

        _buckets = t._buckets;
        _keys = t._keys;
        _links = t._links;
        _hiSize = t._hiSize;
        _loSize = t._loSize;
        _flatCount = t._flatCount;
        _version++;
        _mask = t._mask;
        _hasNullKey = t._hasNullKey;
        _nulls = t._nulls;
    }

    public void SymmetricExceptWith(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other == this)
        {
            Clear();
            return;
        }

        if (IsFlatStrategy && other is NullableTwoBytesSet<K> { IsFlatStrategy: true } otherTbs)
        {
            for (var i = 0; i < NULLS_SIZE; i++)
                _nulls![i] ^= otherTbs._nulls![i];
            _hasNullKey ^= otherTbs._hasNullKey;
            _flatCount = CalculateFlatCount();
            _version++;
            return;
        }

        foreach (var key in other)
            if (!Remove(key))
                Add(key);
    }

    public void UnionWith(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (IsFlatStrategy && other is NullableTwoBytesSet<K> { IsFlatStrategy: true } otherTbs)
        {
            for (var i = 0; i < NULLS_SIZE; i++)
                _nulls![i] |= otherTbs._nulls![i];
            _hasNullKey |= otherTbs._hasNullKey;
            _flatCount = CalculateFlatCount();
            _version++;
            return;
        }

        foreach (var key in other)
            Add(key);
    }

    public bool IsProperSubsetOf(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var otherCollection = other as ICollection<K?> ?? new HashSet<K?>(other);
        return Count < otherCollection.Count && IsSubsetOf(otherCollection);
    }

    public bool IsProperSupersetOf(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var otherCollection = other as ICollection<K?> ?? new HashSet<K?>(other);
        return Count > otherCollection.Count && IsSupersetOf(otherCollection);
    }

    public bool IsSubsetOf(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Count == 0)
            return true;
        var otherAsSet = other as ISet<K?> ?? new HashSet<K?>(other);
        if (Count > otherAsSet.Count)
            return false;

        //Fast path
        if (IsFlatStrategy && other is NullableTwoBytesSet<K> { IsFlatStrategy: true } otherTbs)
        {
            if (_hasNullKey && !otherTbs._hasNullKey)
                return false;
            for (var i = 0; i < NULLS_SIZE; i++)
                if ((_nulls![i] & ~otherTbs._nulls![i]) != 0)
                    return false;

            return true;
        }

        foreach (var key in this)
            if (!otherAsSet.Contains(key))
                return false;
        return true;
    }

    public bool IsSupersetOf(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other is ICollection<K?> { Count: 0 })
            return true;

        //Fast path
        if (IsFlatStrategy && other is NullableTwoBytesSet<K> { IsFlatStrategy: true } otherTbs)
        {
            if (otherTbs._hasNullKey && !_hasNullKey)
                return false;
            for (var i = 0; i < NULLS_SIZE; i++)
                if ((otherTbs._nulls![i] & ~_nulls![i]) != 0)
                    return false;

            return true;
        }

        foreach (var key in other)
            if (!Contains(key))
                return false;
        return true;
    }

    public bool Overlaps(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Count == 0)
            return false;

        //Fast path
        if (IsFlatStrategy && other is NullableTwoBytesSet<K> { IsFlatStrategy: true } otherTbs)
        {
            if (_hasNullKey && otherTbs._hasNullKey)
                return true;
            for (var i = 0; i < NULLS_SIZE; i++)
                if ((_nulls![i] & otherTbs._nulls![i]) != 0)
                    return true;

            return false;
        }

        foreach (var key in other)
            if (Contains(key))
                return true;
        return false;
    }

    public bool SetEquals(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var otherAsSet = other as ISet<K?> ?? new HashSet<K?>(other);
        return Count == otherAsSet.Count && IsSupersetOf(otherAsSet);
    }
    #endregion

    public int EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        var currentCapacity = _keys?.Length ?? (IsFlatStrategy ? VALUES_SIZE : 0);
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
            {
                Resize(GetPowerOfTwo(count));
            }
        }
        else if (_keys != null)
        {
            var newSize = GetPowerOfTwo(count);
            if (newSize < _keys.Length)
            {
                Resize(newSize);
            }
        }
    }
}