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
///Represents a collection of keys and values.
///This map implementation uses a custom hash table and is optimized for performance.
///</summary>
///<typeparam name="K">The type of the reference type keys in the map. Must be a class.</typeparam>
///<typeparam name="V">The type of the values in the map.</typeparam>
[DebuggerDisplay("Count = {Count}")]
public class Map<K, V> : IDictionary<K, V>, IReadOnlyDictionary<K, V>, IEquatable<Map<K, V>>
    where K : class
{
    private struct Entry
    {
        public int hash;
        public K key;
        public V value;
    }

    private uint[]? _buckets;
    private Entry[]? _entries;
    private uint[]? _links; //0-based indices into _entries for collision chains.
    private int _hiSize;    //Number of active entries in the high region.
    private int _loSize;    //Number of active entries in the low region.

    private int _version;
    private uint _mask;
    private readonly IEqualityComparer<K> _comparer;

    private KeysCollection? _keys;
    private ValuesCollection? _values;

    private const int DefaultCapacity = 4;
    #region Constructors
    ///<summary>
    ///Initializes a new instance of the <see cref="Map{K, V}"/> class that is empty, has the default initial capacity, and uses the default equality comparer for the key type.
    ///</summary>
    public Map() : this(0, null) { }

    ///<summary>
    ///Initializes a new instance of the <see cref="Map{K, V}"/> class that is empty, has the specified initial capacity, and uses the default equality comparer for the key type.
    ///</summary>
    ///<param name="capacity">The initial number of elements that the <see cref="Map{K, V}"/> can contain.</param>
    ///<exception cref="ArgumentOutOfRangeException">capacity is less than 0.</exception>
    public Map(int capacity) : this(capacity, null) { }

    ///<summary>
    ///Initializes a new instance of the <see cref="Map{K, V}"/> class that contains elements copied from the specified <see cref="IDictionary{K, V}"/> and uses the default equality comparer for the key type.
    ///</summary>
    ///<param name="dictionary">The <see cref="IDictionary{K, V}"/> whose elements are copied to the new <see cref="Map{K, V}"/>.</param>
    ///<exception cref="ArgumentNullException">dictionary is null.</exception>
    ///<exception cref="ArgumentException">dictionary contains one or more duplicate keys.</exception>
    public Map(IDictionary<K, V> dictionary) : this(dictionary, null) { }

    ///<summary>
    ///Initializes a new instance of the <see cref="Map{K, V}"/> class that is empty, has the default initial capacity, and uses the specified <see cref="IEqualityComparer{K}"/>.
    ///</summary>
    ///<param name="comparer">The <see cref="IEqualityComparer{K}"/> implementation to use when comparing keys, or null to use the default <see cref="EqualityComparer{K}.Default"/> for the type of the key.</param>
    public Map(IEqualityComparer<K>? comparer) : this(0, comparer) { }

    ///<summary>
    ///Initializes a new instance of the <see cref="Map{K, V}"/> class that is empty, has the specified initial capacity, and uses the specified <see cref="IEqualityComparer{K}"/>.
    ///</summary>
    ///<param name="capacity">The initial number of elements that the <see cref="Map{K, V}"/> can contain.</param>
    ///<param name="comparer">The <see cref="IEqualityComparer{K}"/> implementation to use when comparing keys, or null to use the default <see cref="EqualityComparer{K}.Default"/> for the type of the key.</param>
    ///<exception cref="ArgumentOutOfRangeException">capacity is less than 0.</exception>
    public Map(int capacity, IEqualityComparer<K>? comparer)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _comparer = comparer ?? EqualityComparer<K>.Default;
        if (capacity > 0)
            Initialize(capacity);
    }

    ///<summary>
    ///Initializes a new instance of the <see cref="Map{K, V}"/> class that contains elements copied from the specified <see cref="IDictionary{K, V}"/> and uses the specified <see cref="IEqualityComparer{K}"/>.
    ///</summary>
    ///<param name="dictionary">The <see cref="IDictionary{K, V}"/> whose elements are copied to the new <see cref="Map{K, V}"/>.</param>
    ///<param name="comparer">The <see cref="IEqualityComparer{K}"/> implementation to use when comparing keys, or null to use the default <see cref="EqualityComparer{K}.Default"/> for the type of the key.</param>
    ///<exception cref="ArgumentNullException">dictionary is null.</exception>
    ///<exception cref="ArgumentException">dictionary contains one or more duplicate keys.</exception>
    public Map(IDictionary<K, V> dictionary, IEqualityComparer<K>? comparer)
        : this(dictionary?.Count ?? 0, comparer)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        AddRange(dictionary);
    }
    #endregion

    private void AddRange(IEnumerable<KeyValuePair<K, V>> enumerable)
    {
        foreach (var pair in enumerable)
            Add(pair.Key, pair.Value);
    }

    public int Count => _loSize + _hiSize;

    public bool IsReadOnly => false;

    public V this[K key]
    {
        get
        {
            if (TryGetValue(key, out var value))
                return value;
            throw new KeyNotFoundException("The given key was not present in the dictionary.");
        }
        set
        {
            ArgumentNullException.ThrowIfNull(key);
            TryInsert(key, value, true);
        }
    }

    public ICollection<K> Keys => _keys ??= new KeysCollection(this);
    public ICollection<V> Values => _values ??= new ValuesCollection(this);

    IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
    IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref uint GetBucket(uint hashCode) => ref _buckets![hashCode & _mask];

    private int Initialize(int capacity)
    {
        _version++;
        var size = GetPowerOfTwo(capacity);
        _buckets = new uint[size];
        _entries = new Entry[size];
        _links = new uint[Math.Min(size, 16)];
        _loSize = _hiSize = 0;
        _mask = (uint)(size - 1);
        return size;
    }

    private bool TryInsert(K key, V value, bool overwrite)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_buckets == null)
            Initialize(DefaultCapacity);
        else if (_entries!.Length <= _loSize + _hiSize)
            Resize(_entries.Length * 2);

        var hashCode = _comparer.GetHashCode(key);
        ref var bucket = ref GetBucket((uint)hashCode);

        int dstIndex;
        if (bucket == 0)
            dstIndex = _entries.Length - 1 - _hiSize++; //Bucket is empty: Place new entry in the Hi-Region (non-colliding entries).dstIndex = _entries.Length - 1 - _hiSize++;
        else                                            //Collision detected (bucket is not empty).
        {
            //Traverse collision chain to check for existing key or find insertion point.
            for (uint i = bucket - 1, collisions = 0; ;)
            {
                ref var entry = ref _entries[i];
                if (entry.hash == hashCode && _comparer.Equals(entry.key, key))
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
            _links[dstIndex] = bucket - 1; //New entry links to the previous head of the chain.
        }

        ref var newEntry = ref _entries[dstIndex];
        newEntry.hash = hashCode;
        newEntry.key = key;
        newEntry.value = value;

        bucket = (uint)(dstIndex + 1);
        _version++;
        return true;
    }

    public void Add(K key, V value)
    {
        if (!TryInsert(key, value, false))
            throw new ArgumentException("An item with the same key has already been added.");
    }

    public void Add(KeyValuePair<K, V> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        if (Count == 0)
            return;
        _version++;
        if (_buckets != null)
            Array.Clear(_buckets, 0, _buckets.Length);
        _loSize = _hiSize = 0;
    }

    public bool ContainsKey(K key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_loSize + _hiSize == 0)
            return false;

        var hashCode = _comparer.GetHashCode(key);
        var i = (int)GetBucket((uint)hashCode) - 1;
        if (i == -1)
            return false;

        for (var collisions = 0; ;)
        {
            ref var entry = ref _entries![i];
            if (entry.hash == hashCode && _comparer.Equals(entry.key, key))
                return true;

            if (_loSize <= i)
                return false;

            i = (int)_links![i];
            if (_loSize < ++collisions)
                throw new InvalidOperationException("Hash collision chain is unexpectedly long. Possible data corruption.");
        }
    }

    public bool ContainsValue(V value)
    {
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

    public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_loSize + _hiSize == 0)
        {
            value = default;
            return false;
        }

        var hashCode = _comparer.GetHashCode(key);
        var i = (int)GetBucket((uint)hashCode) - 1;
        if (i == -1)
        {
            value = default;
            return false;
        }

        for (var collisions = 0; ;)
        {
            ref var entry = ref _entries![i];
            if (entry.hash == hashCode && _comparer.Equals(entry.key, key))
            {
                value = entry.value;
                return true;
            }

            if (_loSize <= i)
            {
                value = default;
                return false;
            }

            i = (int)_links![i];
            if (_loSize < ++collisions)
                throw new InvalidOperationException("Hash collision chain is unexpectedly long. Possible data corruption.");
        }
    }

    private int Resize(int newSize)
    {
        _version++;
        var oldEntries = _entries;
        var oldLoSize = _loSize;
        var oldHiSize = _hiSize;
        if (_links.Length < 0xFF && _links.Length < _buckets.Length)
            _links = _buckets; //reuse buckets as links
        Initialize(newSize);

        for (var i = 0; i < oldLoSize; i++)
            Copy(in oldEntries![i]);
        for (var i = oldEntries!.Length - oldHiSize; i < oldEntries.Length; i++)
            Copy(in oldEntries[i]);
        return newSize;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(in Entry entry)
    {
        ref var bucket = ref GetBucket((uint)entry.hash); //Use the cached hash
        var i = (int)bucket - 1;
        int dstIndex;

        if (i == -1) //Empty bucket, insert into hi-region
            dstIndex = _entries!.Length - 1 - _hiSize++;
        else //Collision, insert into lo-region
        {
            if (_links!.Length == (dstIndex = _loSize++))
                Array.Resize(ref _links, Math.Max(16, Math.Min(_buckets!.Length, _links.Length * 2)));
            _links[dstIndex] = (uint)i;
        }

        _entries![dstIndex] = entry;
        bucket = (uint)(dstIndex + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Move(int src, int dst, ref Entry dstEntry)
    {
        if (src == dst)
            return;

        ref var srcEntry = ref _entries![src];
        ref var srcBucket = ref GetBucket((uint)srcEntry.hash);
        var index = (int)srcBucket - 1;

        if (index == src) //The bucket points to it
            srcBucket = (uint)(dst + 1);
        else //A link points to it. Find that link.
        {
            ref var link = ref _links![index];
            for (; link != src; link = ref _links![index])
                index = (int)link;

            link = (uint)dst; //Update the found link to point to 'dst'
        }

        if (src < _loSize)
            _links![dst] = _links![src];

        dstEntry = srcEntry;
    }

    public bool Remove(K key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_loSize + _hiSize == 0)
            return false;

        var hashCode = _comparer.GetHashCode(key);
        ref var bucket = ref GetBucket((uint)hashCode);
        var removeIndex = (int)bucket - 1;
        if (removeIndex == -1)
            return false;

        ref var removeEntry = ref _entries![removeIndex];

        if (_loSize <= removeIndex) //In hi-region (no collision chain)
        {
            if (removeEntry.hash != hashCode || !_comparer.Equals(removeEntry.key, key))
                return false;

            Move(_entries.Length - _hiSize, removeIndex, ref removeEntry);
            _hiSize--;
            bucket = 0;
            _version++;
            return true;
        }

        ref var link = ref _links![removeIndex];

        if (removeEntry.hash == hashCode && _comparer.Equals(removeEntry.key, key)) //Head of chain matches
            bucket = link + 1;                                                      //Point bucket to the next item
        else
        {
            //Traverse the collision chain to find the item.
            var last = removeIndex;
            ref var lastEntry = ref removeEntry;

            //Check the second node in the chain
            removeEntry = ref _entries![removeIndex = (int)link];
            if (removeEntry.hash == hashCode && _comparer.Equals(removeEntry.key, key))
            {
                if (removeIndex < _loSize)       //'SecondNode' is in lo-region, relink to bypass it.
                    link = _links![removeIndex]; //`link` is a ref to _links[last], so this modifies the previous link.
                else                             //'SecondNode' is in hi-region (end of chain)
                {
                    //Complex swap: make the hi-node the new head and mark the old head for removal.
                    removeEntry = lastEntry;
                    removeEntry = ref lastEntry;
                    bucket = (uint)(removeIndex + 1);
                    removeIndex = last;
                }
            }
            else if (_loSize <= removeIndex)
                return false; //The second node was in hi-region but didn't match.
            else              //Continue traversing the lo-region chain.
                for (var collisions = 0; ;)
                {
                    lastEntry = ref removeEntry;
                    ref var prevLink = ref link;

                    //Atomically: back up current index to 'last', make 'link' a ref to the current link, advance 'removeIndex'
                    removeEntry = ref _entries![removeIndex = (int)(link = ref _links![last = removeIndex])];

                    if (removeEntry.hash == hashCode && _comparer.Equals(removeEntry.key, key))
                    {
                        if (removeIndex < _loSize)      //Found in lo-region, bypass it.
                            link = _links[removeIndex]; //`link` is a ref to the previous link field. Update it.
                        else                            //Found, but it links to hi-region. Complex swap.
                        {
                            removeEntry = lastEntry;
                            removeEntry = ref lastEntry;
                            prevLink = (uint)removeIndex;
                            removeIndex = last;
                        }

                        break;
                    }

                    if (_loSize <= removeIndex)
                        return false; //Reached end of chain.
                    if (_loSize + 1 < collisions++)
                        throw new InvalidOperationException("Concurrent operations not supported.");
                }
        }

        //Plug the hole at removeIndex by moving the last lo-region item into it.
        Move(_loSize - 1, removeIndex, ref removeEntry);
        _loSize--;
        _version++;
        return true;
    }

    public bool Remove(KeyValuePair<K, V> item) => TryGetValue(item.Key, out var value) && EqualityComparer<V>.Default.Equals(value, item.Value) && Remove(item.Key);

    public bool Contains(KeyValuePair<K, V> item) => TryGetValue(item.Key, out var value) && EqualityComparer<V>.Default.Equals(value, item.Value);

    public void CopyTo(KeyValuePair<K, V>[] dst, int dstIndex)
    {
        ArgumentNullException.ThrowIfNull(dst);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
        if (dst.Length - dstIndex < Count)
            throw new ArgumentException("Destination array is not long enough.");

        if (_entries == null)
            return;

        for (var i = 0; i < _loSize; i++)
        {
            ref var entry = ref _entries[i];
            dst[dstIndex++] = new KeyValuePair<K, V>(entry.key, entry.value);
        }

        if (_hiSize == 0)
            return;

        for (var i = _entries.Length - _hiSize; i < _entries.Length; i++)
        {
            ref var entry = ref _entries[i];
            dst[dstIndex++] = new KeyValuePair<K, V>(entry.key, entry.value);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetPowerOfTwo(int capacity) => capacity <= DefaultCapacity ? DefaultCapacity : (int)BitOperations.RoundUpToPowerOf2((uint)capacity);

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<KeyValuePair<K, V>>
    {
        private readonly Map<K, V> _map;
        private int _version;
        private int _index; //-1=before entries, 0 to _loSize=low region, _entries.Length - _hiSize to _entries.Length=high region
        private KeyValuePair<K, V> _current;

        internal Enumerator(Map<K, V> map)
        {
            _map = map;
            _version = map._version;
            _index = -1;
            _current = default;
        }

        public bool MoveNext()
        {
            if (_version != _map._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");
            if (_map.Count == 0)
                return false;

            if (++_index == int.MaxValue)
            {
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

                _index = _map._entries!.Length - _map._hiSize; //Jump to high region
            }

            if (_index == _map._entries!.Length)
            {
                _index = int.MaxValue - 1;
                return false;
            }

            ref var entry = ref _map._entries[_index];
            _current = new KeyValuePair<K, V>(entry.key, entry.value);
            return true;
        }

        public KeyValuePair<K, V> Current => _version != _map._version ? throw new InvalidOperationException("Collection was modified during enumeration.") : _index == int.MaxValue - 1 || _index == -1 ? throw new InvalidOperationException("Enumeration has either not started or has finished.")
                                                                                                                                                                                                         : _current;

        object? IEnumerator.Current => Current;

        public void Reset()
        {
            _version = _map._version;
            _index = -1;
            _current = default;
        }

        public void Dispose() { }
    }

    public int EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        var currentCapacity = _entries?.Length ?? 0;
        if (currentCapacity >= capacity)
            return currentCapacity;
        _version++;
        return _buckets == null ? Initialize(capacity) : Resize(GetPowerOfTwo(capacity));
    }

    public void TrimExcess()
    {
        var count = Count;
        if (count == 0)
        {
            Initialize(0);
            _version++;
            return;
        }

        if (_entries != null && GetPowerOfTwo(count) < _entries.Length)
            Resize(GetPowerOfTwo(count));
    }
    #region Equality
    public bool Equals(Map<K, V>? other)
    {
        if (other == null || Count != other.Count || !_comparer.Equals(other._comparer))
            return false;

        //Count is the same, now check all keys.
        foreach (var pair in this)
        {
            if (!other.TryGetValue(pair.Key, out var otherValue) || !EqualityComparer<V>.Default.Equals(pair.Value, otherValue))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as Map<K, V>);

    public override int GetHashCode()
    {
        //Start with the hash of the comparer. Dictionaries with different comparers are not equal.
        var hashCode = _comparer.GetHashCode();

        //Use a commutative operation (XOR) on the hash codes of all entries
        //to ensure the final hash is order-independent.

        //Iterate lo-region
        for (var i = 0; i < _loSize; i++)
        {
            var entry = _entries![i];
            hashCode ^= entry.hash ^ (entry.value?.GetHashCode() ?? 0);
        }

        if (_hiSize == 0)
            return hashCode;

        //Iterate hi-region
        for (var i = _entries!.Length - _hiSize; i < _entries.Length; i++)
        {
            var entry = _entries[i];
            hashCode ^= entry.hash ^ (entry.value?.GetHashCode() ?? 0);
        }

        return hashCode;
    }
    #endregion
    #region Helper Collections(Unchanged, work as - is)
    [DebuggerDisplay("Count = {Count}")]
    public sealed class KeysCollection : ICollection<K>, IReadOnlyCollection<K>
    {
        private readonly Map<K, V> map;
        public KeysCollection(Map<K, V> map) => this.map = map;
        public int Count => map.Count;
        public bool IsReadOnly => true;

        public void CopyTo(K[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
            if (dst.Length - dstIndex < Count)
                throw new ArgumentException("Destination array is not long enough.");

            foreach (var item in map)
                dst[dstIndex++] = item.Key;
        }

        public bool Contains(K item) => map.ContainsKey(item);
        public Enumerator GetEnumerator() => new(map);
        IEnumerator<K> IEnumerable<K>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        void ICollection<K>.Add(K item) => throw new NotSupportedException();
        bool ICollection<K>.Remove(K item) => throw new NotSupportedException();
        void ICollection<K>.Clear() => throw new NotSupportedException();

        public struct Enumerator : IEnumerator<K>
        {
            private Map<K, V>.Enumerator _dictEnumerator;
            internal Enumerator(Map<K, V> dictionary) => _dictEnumerator = dictionary.GetEnumerator();
            public bool MoveNext() => _dictEnumerator.MoveNext();
            public K Current => _dictEnumerator.Current.Key;
            object? IEnumerator.Current => Current;
            public void Dispose() => _dictEnumerator.Dispose();
            public void Reset() => _dictEnumerator.Reset();
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    public sealed class ValuesCollection : ICollection<V>, IReadOnlyCollection<V>
    {
        private readonly Map<K, V> map;
        public ValuesCollection(Map<K, V> map) => this.map = map;
        public int Count => map.Count;
        public bool IsReadOnly => true;

        public void CopyTo(V[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
            if (dst.Length - dstIndex < Count)
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
            private Map<K, V>.Enumerator _dictEnumerator;
            internal Enumerator(Map<K, V> dictionary) => _dictEnumerator = dictionary.GetEnumerator();
            public bool MoveNext() => _dictEnumerator.MoveNext();
            public V Current => _dictEnumerator.Current.Value;
            object? IEnumerator.Current => Current;
            public void Dispose() => _dictEnumerator.Dispose();
            public void Reset() => _dictEnumerator.Reset();
        }
    }
    #endregion
}