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
using System.Numerics;
using System.Runtime.CompilerServices;

namespace org.unirail.collections;

///<summary>
///Represents a set of unique, nullable reference type elements of type <typeparamref name="K"/>.
///This set implementation uses a custom hash table and is optimized for performance, supporting a single null element.
///</summary>
///<typeparam name="K">The type of the reference type elements in the set. Must be a class.</typeparam>
[DebuggerDisplay("Count = {Count}")]
public class NullableSet<K> : ISet<K?>, IReadOnlyCollection<K?>, IEquatable<NullableSet<K>>
    where K : class
{
    private struct Entry
    {
        public int hash;
        public K key;
    }

    private uint[]? _buckets;
    private Entry[]? _entries;
    private uint[]? _links; //0-based indices into _entries for collision chains.
    private int _hiSize;    //Number of active entries in the high region.
    private int _loSize;    //Number of active entries in the low region.

    private bool _hasNullKey;

    private int _version;
    private uint _mask;
    private readonly IEqualityComparer<K> _comparer;

    private const int DefaultCapacity = 4;
    #region Constructors
    ///<summary>
    ///Initializes a new instance of the <see cref="NullableSet{K}"/> class that is empty and uses the default equality comparer for the element type <typeparamref name="K"/>.
    ///</summary>
    public NullableSet() : this(0, null) { }

    ///<summary>
    ///Initializes a new instance of the <see cref="NullableSet{K}"/> class with the specified capacity and uses the default equality comparer for the element type <typeparamref name="K"/>.
    ///</summary>
    ///<param name="capacity">The initial number of elements that the <see cref="NullableSet{K}"/> can contain.</param>
    ///<exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
    public NullableSet(int capacity) : this(capacity, null) { }

    ///<summary>
    ///Initializes a new instance of the <see cref="NullableSet{K}"/> class that contains elements copied from the specified collection
    ///and uses the default equality comparer for the element type <typeparamref name="K"/>.
    ///</summary>
    ///<param name="collection">The collection whose elements (which can include <see langword="null"/>) are copied to the new set.</param>
    ///<exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public NullableSet(IEnumerable<K?> collection) : this(collection, null) { }

    ///<summary>
    ///Initializes a new instance of the <see cref="NullableSet{K}"/> class that is empty and uses the specified equality comparer for non-null elements of type <typeparamref name="K"/>.
    ///</summary>
    ///<param name="comparer">
    ///The <see cref="IEqualityComparer{K}"/> implementation to use when comparing non-null elements,
    ///or <see langword="null"/> to use the default <see cref="EqualityComparer{K}.Default"/> for type <typeparamref name="K"/>.
    ///This comparer will not be invoked with <see langword="null"/> arguments.
    ///</param>
    public NullableSet(IEqualityComparer<K>? comparer) : this(0, comparer) { }

    ///<summary>
    ///Initializes a new instance of the <see cref="NullableSet{K}"/> class with the specified capacity and equality comparer for non-null elements of type <typeparamref name="K"/>.
    ///</summary>
    ///<param name="capacity">The initial number of elements that the <see cref="NullableSet{K}"/> can contain.</param>
    ///<param name="comparer">
    ///The <see cref="IEqualityComparer{K}"/> implementation to use when comparing non-null elements,
    ///or <see langword="null"/> to use the default <see cref="EqualityComparer{K}.Default"/> for type <typeparamref name="K"/>.
    ///This comparer will not be invoked with <see langword="null"/> arguments.
    ///</param>
    ///<exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
    public NullableSet(int capacity, IEqualityComparer<K>? comparer)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _comparer = comparer ?? EqualityComparer<K>.Default;
        if (capacity > 0)
            Initialize(capacity);
    }

    ///<summary>
    ///Initializes a new instance of the <see cref="NullableSet{K}"/> class that contains elements copied from the specified collection
    ///and uses the specified equality comparer for non-null elements of type <typeparamref name="K"/>.
    ///</summary>
    ///<param name="collection">The collection whose elements (which can include <see langword="null"/>) are copied to the new set.</param>
    ///<param name="comparer">
    ///The <see cref="IEqualityComparer{K}"/> implementation to use when comparing non-null elements,
    ///or <see langword="null"/> to use the default <see cref="EqualityComparer{K}.Default"/> for type <typeparamref name="K"/>.
    ///This comparer will not be invoked with <see langword="null"/> arguments.
    ///</param>
    ///<exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public NullableSet(IEnumerable<K?> collection, IEqualityComparer<K>? comparer) : this((collection as ICollection<K?>)?.Count ?? 0, comparer)
    {
        ArgumentNullException.ThrowIfNull(collection);
        UnionWith(collection);
    }
    #endregion

    public int Count => _loSize + _hiSize + (_hasNullKey ? 1 : 0);

    public bool IsReadOnly => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetHashCode(K key) => _comparer.GetHashCode(key) & 0x7FFFFFFF; //Mask to ensure non-negative

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref uint GetBucket(uint hashCode) => ref _buckets![hashCode & _mask];

    private int Initialize(int capacity)
    {
        _version = 0;
        var size = GetPowerOfTwo(capacity);
        _buckets = new uint[size];
        _entries = new Entry[size];
        _links = new uint[Math.Min(size, 16)];
        _loSize = _hiSize = 0;
        _mask = (uint)(size - 1);
        return size;
    }

    ///<summary>
    ///Adds an element to the set.
    ///</summary>
    ///<param name="item">The element to add to the set.</param>
    ///<returns><see langword="true"/> if the element is added to the set; <see langword="false"/> if the element is already in the set.</returns>
    public bool Add(K? item)
    {
        if (item == null)
        {
            if (_hasNullKey)
                return false;
            _hasNullKey = true;
            _version++;
            return true;
        }

        if (_buckets == null)
            Initialize(DefaultCapacity);

        if (_entries!.Length <= _loSize + _hiSize)
            Resize(_entries.Length * 2);

        var hashCode = GetHashCode(item);
        ref var bucket = ref GetBucket((uint)hashCode);
        var index = (int)bucket - 1;

        if (index != -1)
            for (int i = index, collisions = 0; ;)
            {
                ref var entry = ref _entries[i];
                if (entry.hash == hashCode && _comparer.Equals(entry.key, item))
                    return false; //Already exists

                if (_loSize <= i)
                    break;

                i = (int)_links![i];
                if (_loSize + 1 < collisions++)
                    throw new InvalidOperationException("Concurrent operations not supported.");
            }

        int dstIndex;
        if (index == -1)
            dstIndex = _entries.Length - 1 - _hiSize++; //Bucket is empty: place new entry in hi-region
        else                                            //Collision: place new entry in lo-region
        {
            if (_links!.Length == (dstIndex = _loSize++))
                Array.Resize(ref _links, Math.Max(16, Math.Min(_buckets!.Length, _links.Length * 2)));
            _links[dstIndex] = (uint)index;
        }

        ref var newEntry = ref _entries[dstIndex];
        newEntry.hash = hashCode;
        newEntry.key = item;

        bucket = (uint)(dstIndex + 1);
        _version++;
        return true;
    }

    void ICollection<K?>.Add(K? item) => Add(item);

    public void Clear()
    {
        if (Count == 0)
            return;
        _version++;
        _hasNullKey = false;
        if (_buckets != null)
            Array.Clear(_buckets, 0, _buckets.Length);
        _loSize = _hiSize = 0;
    }

    ///<summary>
    ///Determines whether the set contains a specific element.
    ///</summary>
    ///<param name="item">The element to locate in the set.</param>
    ///<returns><see langword="true"/> if the set contains the specified element; otherwise, <see langword="false"/>.</returns>
    public bool Contains(K? item)
    {
        if (item == null)
            return _hasNullKey;
        if (_loSize + _hiSize == 0)
            return false;

        var hashCode = GetHashCode(item);
        var i = (int)GetBucket((uint)hashCode) - 1;
        if (i == -1)
            return false;

        for (var collisions = 0; ;)
        {
            ref var entry = ref _entries![i];
            if (entry.hash == hashCode && _comparer.Equals(entry.key, item))
                return true;

            if (_loSize <= i)
                return false;

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

            link = (uint)dst;
        }

        if (src < _loSize)
            _links![dst] = _links![src];

        dstEntry = srcEntry;
    }

    ///<summary>
    ///Removes the specified element from the set.
    ///</summary>
    ///<param name="key">The element to remove.</param>
    ///<returns><see langword="true"/> if the element is successfully found and removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(K? key)
    {
        if (key == null)
        {
            if (!_hasNullKey)
                return false;
            _hasNullKey = false;
            _version++;
            return true;
        }

        if (_loSize + _hiSize == 0)
            return false;

        var hashCode = GetHashCode(key);
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
            bucket = link + 1;                                                      //Point bucket to the next key
        else
        {
            var last = removeIndex;
            ref var lastEntry = ref removeEntry;

            if ((removeEntry = ref _entries![removeIndex = (int)link]).hash == hashCode && _comparer.Equals(removeEntry.key, key))
                if (removeIndex < _loSize)
                    link = _links![removeIndex];
                else
                {
                    removeEntry = lastEntry;
                    removeEntry = ref lastEntry;
                    bucket = (uint)(removeIndex + 1);
                    removeIndex = last;
                }
            else if (_loSize <= removeIndex)
                return false;
            else
                for (var collisions = 0; ;)
                {
                    lastEntry = ref removeEntry;
                    ref var prevLink = ref link;

                    if ((removeEntry = ref _entries![removeIndex = (int)(link = ref _links![last = removeIndex])]).hash == hashCode && _comparer.Equals(removeEntry.key, key))
                    {
                        if (removeIndex < _loSize)
                            link = _links[removeIndex];
                        else
                        {
                            removeEntry = lastEntry;
                            removeEntry = ref lastEntry;
                            prevLink = (uint)removeIndex;
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

    public void CopyTo(K?[] dst, int dstIndex)
    {
        ArgumentNullException.ThrowIfNull(dst);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
        if (dst.Length - dstIndex < Count)
            throw new ArgumentException("Destination array is not long enough.");

        if (_hasNullKey)
            dst[dstIndex++] = null;

        if (_entries == null)
            return;

        for (var i = 0; i < _loSize; i++)
            dst[dstIndex++] = _entries[i].key;
        if (_hiSize == 0)
            return;
        for (var i = _entries.Length - _hiSize; i < _entries.Length; i++)
            dst[dstIndex++] = _entries[i].key;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetPowerOfTwo(int capacity) => capacity <= DefaultCapacity ? DefaultCapacity : (int)BitOperations.RoundUpToPowerOf2((uint)capacity);

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<K?> IEnumerable<K?>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<K?>
    {
        private readonly NullableSet<K> _set;
        private int _version;
        private int _index; //-2=before null, -1=null, 0 to _loSize-1=lo-region, _entries.Length - _hiSize to _entries.Length-1=hi-region
        private K? _current;

        internal Enumerator(NullableSet<K> set)
        {
            _set = set;
            _version = set._version;
            _index = _set._hasNullKey ? -2 : -1;
            _current = default;
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
                return true;
            }

            if (_index == int.MaxValue || _set._loSize + _set._hiSize == 0)
            {
                _index = int.MaxValue - 1;
                return false;
            }

            if (_index == _set._loSize)
            {
                if (_set._hiSize == 0)
                {
                    _index = int.MaxValue - 1;
                    return false;
                }

                _index = _set._entries!.Length - _set._hiSize; //Jump to hi-region
            }

            if (_index == _set._entries!.Length)
            {
                _index = int.MaxValue - 1;
                return false;
            }

            _current = _set._entries[_index].key;
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
            _hasNullKey = false;
            _version++;
            return;
        }

        if (_entries != null && GetPowerOfTwo(count) < _entries.Length)
            Resize(GetPowerOfTwo(count));
    }
    #region ISet < T> Implementation
    public void ExceptWith(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Count == 0)
            return;
        if (other == this)
        {
            Clear();
            return;
        }

        foreach (var element in other)
            Remove(element);
    }

    public void IntersectWith(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Count == 0 || other == this)
            return;

        var t = new NullableSet<K>();
        foreach (var key in other)
            if (Contains(key))
                t.Add(key);

        _buckets = t._buckets;
        _entries = t._entries;
        _links = t._links;
        _hiSize = t._hiSize;
        _loSize = t._loSize;
        _version++;
        _mask = t._mask;
        _hasNullKey = t._hasNullKey;
    }

    public bool IsProperSubsetOf(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var (otherCount, otherSet) = GetCountAndSet(other);
        return Count < otherCount && IsSubsetOf(otherSet);
    }

    public bool IsProperSupersetOf(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var (otherCount, _) = GetCountAndSet(other);
        return Count > otherCount && IsSupersetOf(other);
    }

    public bool IsSubsetOf(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var (otherCount, otherSet) = GetCountAndSet(other);
        if (Count > otherCount)
            return false;

        var set = other as NullableSet<K> ?? new NullableSet<K>(otherSet, _comparer);
        foreach (var item in this)
            if (!set.Contains(item))
                return false;
        return true;
    }

    public bool IsSupersetOf(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        foreach (var item in other)
            if (!Contains(item))
                return false;
        return true;
    }

    public bool Overlaps(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Count == 0)
            return false;
        foreach (var item in other)
            if (Contains(item))
                return true;
        return false;
    }

    public bool SetEquals(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var (otherCount, _) = GetCountAndSet(other);
        return Count == otherCount && IsSupersetOf(other);
    }

    public void SymmetricExceptWith(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other == this)
        {
            Clear();
            return;
        }

        var otherSet = new NullableSet<K>(other, _comparer);
        foreach (var item in otherSet)
            if (!Remove(item))
                Add(item);
    }

    public void UnionWith(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        foreach (var item in other)
            Add(item);
    }

    private (int Count, IEnumerable<K?> Set) GetCountAndSet(IEnumerable<K?> other)
    {
        if (other is ICollection<K?> coll)
            return (coll.Count, other);
        if (other is IReadOnlyCollection<K?> roColl)
            return (roColl.Count, other);

        //This is inefficient as it requires enumeration, but is the fallback.
        var list = new List<K?>(other);
        return (list.Count, list);
    }
    #endregion
    #region Equality
    public bool Equals(NullableSet<K>? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (!_comparer.Equals(other._comparer))
            return false;

        return SetEquals(other);
    }

    public override bool Equals(object? obj) => Equals(obj as NullableSet<K>);

    public override int GetHashCode()
    {
        var hashCode = _comparer.GetHashCode();

        //Use a commutative operation (XOR) on the hash codes of all items
        //to ensure the final hash is order-independent.
        if (_hasNullKey)
            hashCode ^= 0; //Hash for the null item

        if (_entries is null)
            return hashCode;

        //Iterate lo-region
        for (var i = 0; i < _loSize; i++)
            hashCode ^= _entries[i].hash;
        if (_hiSize == 0)
            return hashCode;

        //Iterate hi-region
        for (var i = _entries.Length - _hiSize; i < _entries.Length; i++)
            hashCode ^= _entries[i].hash;

        return hashCode;
    }
    #endregion
}