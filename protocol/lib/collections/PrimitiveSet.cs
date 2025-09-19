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
///Represents a set of unique primitive values of type <typeparamref name="K"/>.
///This set implementation uses a hash table and is optimized for primitive types.
///</summary>
///<typeparam name="K">The type of the primitive values in the set. Must be an unmanaged type.</typeparam>
public class PrimitiveSet<K> : ISet<K>, IReadOnlyCollection<K>
    where K : unmanaged
{
    private uint[]? _buckets;
    private K[]? _keys;
    private uint[]? _links; //0-based indices into _keys for collision chains.

    //The _keys array is split into two regions to optimize for non-colliding entries:
    //1. Lo-Region (indices 0 to _loSize-1): Stores keys that have caused a hash collision.
    //2. Hi-Region (indices _keys.Length - _hiSize to _keys.Length-1): Stores keys that have not caused a collision.
    private int _hiSize; //Number of active entries in the high region.
    private int _loSize; //Number of active entries in the low region.

    private int _version;
    private uint _mask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref uint GetBucket(uint hashCode) => ref _buckets![hashCode & _mask];

    private const int DefaultCapacity = 4;

    public PrimitiveSet() : this(0) { }

    public PrimitiveSet(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (capacity > 0)
            Initialize(capacity);
    }

    public PrimitiveSet(IEnumerable<K> collection) : this(collection is ICollection<K> coll ? coll.Count : 0)
    {
        ArgumentNullException.ThrowIfNull(collection);
        AddRange(collection);
    }

    private void AddRange(IEnumerable<K> enumerable)
    {
        foreach (var key in enumerable)
            Add(key);
    }

    public int Count => _loSize + _hiSize;

    public bool Add(K key)
    {
        if (_buckets == null)
            Initialize(DefaultCapacity);

        if (_keys!.Length <= _loSize + _hiSize)
        {
            Resize(_keys.Length * 2);
        }

        ref var bucket = ref GetBucket((uint)key.GetHashCode());
        var index = (int)bucket - 1;

        int dstIndex;

        if (index == -1) //Bucket is empty: place new entry in hi-region
            dstIndex = _keys.Length - 1 - _hiSize++;
        else
        {
            for (int i = index, collisions = 0; ;)
            {
                if (EqualityComparer<K>.Default.Equals(_keys[i], key))
                    return false;

                if (_loSize <= i)
                    break; //Reached a hi-region entry, end of this chain.
                i = (int)_links![i];

                if (_loSize + 1 < collisions++)
                    throw new InvalidOperationException("Concurrent operations not supported.");
            }

            if (_links!.Length == (dstIndex = _loSize++))
                Array.Resize(ref _links, Math.Max(16, Math.Min(_buckets!.Length, _links.Length * 2)));

            _links[dstIndex] = (uint)index;
        }

        _keys[dstIndex] = key;
        bucket = (uint)(dstIndex + 1);
        _version++;
        return true;
    }

    void ICollection<K>.Add(K key) => Add(key);

    public void Clear()
    {
        if (Count == 0)
            return;

        _version++;

        if (_buckets != null)
            Array.Clear(_buckets, 0, _buckets.Length);
        _loSize = 0;
        _hiSize = 0;
    }

    public bool Contains(K key)
    {
        if (_loSize + _hiSize == 0)
            return false;

        var i = (int)GetBucket((uint)key.GetHashCode()) - 1;
        if (i == -1)
            return false;

        for (var collisions = 0; ;)
        {
            if (EqualityComparer<K>.Default.Equals(_keys![i], key))
                return true;

            if (_loSize <= i)
                return false; //Reached end of chain (a hi-region entry)
            i = (int)_links![i];

            if (_loSize < ++collisions)
                throw new InvalidOperationException("Hash collision chain is unexpectedly long. Possible data corruption.");
        }
    }

    public void CopyTo(K[] dst, int dstIndex)
    {
        ArgumentNullException.ThrowIfNull(dst);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
        if (dst.Length - dstIndex < Count)
            throw new ArgumentException("Destination array is not long enough.");

        //Copy lo-region
        Array.Copy(_keys!, 0, dst, dstIndex, _loSize);
        //Copy hi-region
        Array.Copy(_keys!, _keys!.Length - _hiSize, dst, dstIndex + _loSize, _hiSize);
    }

    private int Initialize(int capacity)
    {
        _version++;

        var size = GetPowerOfTwo(capacity);
        _buckets = new uint[size];
        _keys = new K[size];
        _links = new uint[Math.Min(size, 16)];
        _loSize = _hiSize = 0;
        _mask = (uint)(size - 1);

        return size;
    }

    private static int GetPowerOfTwo(int capacity) => capacity <= DefaultCapacity ? DefaultCapacity : (int)BitOperations.RoundUpToPowerOf2((uint)capacity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Resize(int newSize)
    {
        _version++;
        var oldKeys = _keys;
        var oldLoSize = _loSize;
        var oldHiSize = _hiSize;
        if (_links!.Length < 0xFF && _links.Length < _buckets!.Length)
            _links = _buckets; //reuse buckets as links
        Initialize(newSize);

        for (var i = 0; i < oldLoSize; i++)
            Copy(in oldKeys![i]);
        for (var i = oldKeys!.Length - oldHiSize; i < oldKeys.Length; i++)
            Copy(in oldKeys[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(in K key)
    {
        ref var bucket = ref GetBucket((uint)key.GetHashCode());
        var i = (int)bucket - 1;
        int dstIndex;

        if (i == -1) //Empty bucket, insert into hi-region
            dstIndex = _keys!.Length - 1 - _hiSize++;
        else //Collision, insert into lo-region
        {
            if (_links!.Length == (dstIndex = _loSize++))
                Array.Resize(ref _links, Math.Max(16, Math.Min(_buckets!.Length, _links.Length * 2)));
            _links[dstIndex] = (uint)i;
        }

        _keys![dstIndex] = key;
        bucket = (uint)(dstIndex + 1);
    }

    //Moves data from src index to dst index and updates all links pointing to src.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Move(int src, int dst, ref K dstKey)
    {
        if (src == dst)
            return;

        ref var srcKey = ref _keys![src]; //Optimized: Direct reference to _keys[src]
        ref var srcBucket = ref GetBucket((uint)srcKey.GetHashCode());
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
        dstKey = srcKey;
    }

    public bool Remove(K key)
    {
        if (_loSize + _hiSize == 0)
            return false;

        ref var bucket = ref GetBucket((uint)key.GetHashCode());
        var removeIndex = (int)bucket - 1;
        if (removeIndex == -1)
            return false;

        ref var removeKey = ref _keys![removeIndex];

        if (_loSize <= removeIndex) //In hi-region (no collision chain)
        {
            if (!EqualityComparer<K>.Default.Equals(removeKey, key))
                return false;

            Move(_keys.Length - _hiSize, removeIndex, ref removeKey);
            _hiSize--;
            bucket = 0; //The bucket pointed to the hi-item directly and is now empty.
            _version++;
            return true;
        }

        ref var link = ref _links![removeIndex];
        //Item is in lo-region, or is the head of a chain.
        if (EqualityComparer<K>.Default.Equals(removeKey, key)) //Head of chain matches
            bucket = link + 1;                                  //Point bucket to the next item
        else
        {
            var last = removeIndex;
            ref var lastKey = ref removeKey;
            if (EqualityComparer<K>.Default.Equals(removeKey = ref _keys[removeIndex = (int)link], key)) //The key is found at 'SecondNode'
                if (removeIndex < _loSize)
                    link = _links[removeIndex]; //'SecondNode' is in 'lo Region', relink to bypasses 'SecondNode'
                else
                {
                    removeKey = lastKey;
                    removeKey = ref lastKey;
                    bucket = (uint)(removeIndex + 1);
                    removeIndex = last;
                }
            else if (_loSize <= removeIndex)
                return false;
            else
                for (var collisions = 0; ;)
                {
                    lastKey = ref removeKey;
                    ref var prevLink = ref link;

                    if (EqualityComparer<K>.Default.Equals(removeKey = ref _keys[removeIndex = (int)(link = ref _links![last = removeIndex])], key))
                    {
                        if (removeIndex < _loSize)
                            link = _links[removeIndex];
                        else
                        {
                            removeKey = lastKey;
                            removeKey = ref lastKey;
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

        Move(_loSize - 1, removeIndex, ref removeKey);
        _loSize--;
        _version++;
        return true;
    }

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<K> IEnumerable<K>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<K>
    {
        private readonly PrimitiveSet<K> _set;
        private int _version;
        private int _index;
        private K _current;

        internal Enumerator(PrimitiveSet<K> set)
        {
            _set = set;
            _version = set._version;
            _index = -1;
            _current = default;
        }

        public bool MoveNext()
        {
            if (_version != _set._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");
            if (_set.Count == 0)
                return false;
            if (++_index == _set._keys!.Length)
            {
                _index = _set._keys.Length;
                return false;
            }

            if (_index == _set._loSize)
                if (_set._hiSize == 0)
                {
                    _index = _set._keys.Length;
                    return false;
                }
                else
                    _index = _set._keys.Length - _set._hiSize;

            _current = _set._keys![_index];
            return true;
        }

        public K Current => _version != _set._version ? throw new InvalidOperationException("Collection was modified during enumeration.") : _index == -1 || _index == _set._keys!.Length ? throw new InvalidOperationException("Enumeration has either not started or has finished.")
                                                                                                                                                                                          : _current;

        object IEnumerator.Current => Current;

        public void Reset()
        {
            _version = _set._version;
            _index = -1;
            _current = default;
        }

        public void Dispose() { }
    }
    #region ISet Explicit / Remaining Implementation
    public bool IsReadOnly => false;

    public void ExceptWith(IEnumerable<K> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Count == 0)
            return;
        if (other is PrimitiveSet<K> otherSet && otherSet.Count == 0)
            return;

        foreach (var key in other)
            Remove(key);
    }

    public void IntersectWith(IEnumerable<K> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Count == 0 || other == this)
            return;

        var t = new PrimitiveSet<K>();
        foreach (var key in other)
            if (Contains(key))
                t.Add(key);

        _buckets = t._buckets;
        _keys = t._keys;
        _links = t._links;
        _hiSize = t._hiSize;
        _loSize = t._loSize;
        _version++;
        _mask = t._mask;
    }

    public void SymmetricExceptWith(IEnumerable<K> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other == this)
        {
            Clear();
            return;
        }

        foreach (var key in other)
            if (!Remove(key))
                Add(key);
    }

    public void UnionWith(IEnumerable<K> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        foreach (var key in other)
            Add(key);
    }

    public bool IsProperSubsetOf(IEnumerable<K> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var otherCollection = other as ICollection<K> ?? new HashSet<K>(other);
        return Count < otherCollection.Count && IsSubsetOf(otherCollection);
    }

    public bool IsProperSupersetOf(IEnumerable<K> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var otherCollection = other as ICollection<K> ?? new HashSet<K>(other);
        return Count > otherCollection.Count && IsSupersetOf(otherCollection);
    }

    public bool IsSubsetOf(IEnumerable<K> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Count == 0)
            return true;
        var otherAsSet = other as ISet<K> ?? new HashSet<K>(other);
        if (Count > otherAsSet.Count)
            return false;

        foreach (var key in this)
            if (!otherAsSet.Contains(key))
                return false;
        return true;
    }

    public bool IsSupersetOf(IEnumerable<K> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other is ICollection<K> { Count: 0 })
            return true;

        foreach (var key in other)
            if (!Contains(key))
                return false;
        return true;
    }

    public bool Overlaps(IEnumerable<K> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Count == 0)
            return false;

        foreach (var key in other)
            if (Contains(key))
                return true;
        return false;
    }

    public bool SetEquals(IEnumerable<K> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var otherAsSet = other as ISet<K> ?? new HashSet<K>(other);
        return Count == otherAsSet.Count && IsSupersetOf(otherAsSet);
    }
    #endregion

    public int EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        var currentCapacity = _keys?.Length ?? 0;
        if (currentCapacity >= capacity)
            return currentCapacity;
        _version++;
        if (_buckets == null)
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

        if (_keys != null)
        {
            var newSize = GetPowerOfTwo(count);
            if (newSize < _keys.Length)
            {
                Resize(newSize);
            }
        }
    }
}