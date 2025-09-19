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
///Represents a set of unique nullable primitive values of type <typeparamref name="K"/>.
///This set implementation uses a hash table and is optimized for nullable primitive types.
///It supports the inclusion of a single null value.
///</summary>
///<typeparam name="K">The type of the primitive values in the set. Must be an unmanaged type.</typeparam>
public class NullablePrimitiveSet<K> : ISet<K?>, IReadOnlyCollection<K?>
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

    private bool _hasNullKey;

    private const int DefaultCapacity = 4;

    public NullablePrimitiveSet() : this(0) { }

    public NullablePrimitiveSet(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (capacity > 0)
            Initialize(capacity);
    }

    public NullablePrimitiveSet(IEnumerable<K?> collection) : this(collection is ICollection<K?> coll ? coll.Count : 0)
    {
        ArgumentNullException.ThrowIfNull(collection);
        AddRange(collection);
    }

    private void AddRange(IEnumerable<K?> enumerable)
    {
        foreach (var key in enumerable)
            Add(key);
    }

    public int Count => _loSize + _hiSize + (_hasNullKey ? 1 : 0);

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

        var kv = key.Value;

        if (_buckets == null)
            Initialize(DefaultCapacity);

        if (_keys!.Length <= _loSize + _hiSize)
        {
            Resize(_keys.Length * 2);
        }

        ref var bucket = ref GetBucket((uint)kv.GetHashCode());
        var index = (int)bucket - 1;

        int dstIndex;

        if (index == -1) //Bucket is empty: place new entry in hi-region
            dstIndex = _keys.Length - 1 - _hiSize++;
        else
        {
            for (int i = index, collisions = 0; ;)
            {
                if (EqualityComparer<K>.Default.Equals(_keys[i], kv))
                    return false;

                if (_loSize <= i)
                    break;
                i = (int)_links![i];

                if (_loSize + 1 < collisions++)
                    throw new InvalidOperationException("Concurrent operations not supported.");
            }

            if (_links!.Length == (dstIndex = _loSize++))
                Array.Resize(ref _links, Math.Max(16, Math.Min(_buckets!.Length, _links.Length * 2)));

            _links[dstIndex] = (uint)index;
        }

        _keys[dstIndex] = kv;
        bucket = (uint)(dstIndex + 1);
        _version++;
        return true;
    }

    void ICollection<K?>.Add(K? key) => Add(key);

    public void Clear()
    {
        if (Count == 0)
            return;

        _hasNullKey = false;
        _version++;

        if (_buckets != null)
            Array.Clear(_buckets, 0, _buckets.Length);
        _loSize = 0;
        _hiSize = 0;
    }

    public bool Contains(K? key)
    {
        if (!key.HasValue)
            return _hasNullKey;
        var kv = key.Value;

        if (_loSize + _hiSize == 0)
            return false;

        var i = (int)GetBucket((uint)kv.GetHashCode()) - 1;
        if (i == -1)
            return false;

        for (var collisions = 0; ;)
        {
            if (EqualityComparer<K>.Default.Equals(_keys![i], kv))
                return true;

            if (_loSize <= i)
                return false; //terminal entity
            i = (int)_links![i];

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
        //Copy lo-region
        for (var i = 0; i < _loSize; i++)
            dst[dstIndex++] = _keys![i];
        if (_hiSize == 0)
            return;
        //Copy hi-region
        for (var i = _keys!.Length - _hiSize; i < _keys.Length; i++)
            dst[dstIndex++] = _keys[i];
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetPowerOfTwo(int capacity) => capacity <= DefaultCapacity ? DefaultCapacity : (int)BitOperations.RoundUpToPowerOf2((uint)capacity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Resize(int newSize)
    {
        _version++;
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

        ref var srcKey = ref _keys![src];
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

        if (_loSize + _hiSize == 0)
            return false;

        ref var bucket = ref GetBucket((uint)kv.GetHashCode());
        var removeIndex = (int)bucket - 1;
        if (removeIndex == -1)
            return false;

        ref var removeKey = ref _keys![removeIndex];

        if (_loSize <= removeIndex)
        {
            if (!EqualityComparer<K>.Default.Equals(removeKey, kv))
                return false;

            Move(_keys.Length - _hiSize, removeIndex, ref removeKey);
            _hiSize--;
            bucket = 0;
            _version++;
            return true;
        }

        ref var link = ref _links![removeIndex];

        //Item is in lo-region, or is the head of a chain.
        if (EqualityComparer<K>.Default.Equals(removeKey, kv)) //Head of chain matches
            bucket = link + 1;                                 //Point bucket to the next item
        else
        {
            //Traverse the collision chain to find the item.
            var last = removeIndex;
            ref var lastKey = ref removeKey;

            if (EqualityComparer<K>.Default.Equals((removeKey = ref _keys![removeIndex = (int)link]), kv))
                if (removeIndex < _loSize)       //'SecondNode' is in lo-region, relink to bypass it.
                    link = _links![removeIndex]; //`link` is a ref to _links[last], so this modifies the previous link.
                else                             //'SecondNode' is in hi-region (end of chain)
                {
                    removeKey = lastKey;
                    removeKey = ref lastKey;
                    bucket = (uint)(removeIndex + 1);
                    removeIndex = last;
                }
            else if (_loSize <= removeIndex)
                return false; //The second node was in hi-region but didn't match.
            else              //Continue traversing the lo-region chain.
                for (var collisions = 0; ;)
                {
                    lastKey = ref removeKey;
                    ref var prevLink = ref link;

                    if (EqualityComparer<K>.Default.Equals((removeKey = ref _keys![removeIndex = (int)(link = ref _links![last = removeIndex])]), kv))
                    {
                        if (removeIndex < _loSize)      //Found in lo-region, bypass it.
                            link = _links[removeIndex]; //`link` is a ref to the previous link field. Update it.
                        else                            //Found, but it links to hi-region. Complex swap.
                        {
                            removeKey = lastKey;
                            removeKey = ref lastKey;
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
        private readonly NullablePrimitiveSet<K> _set;
        private int _version;
        private int _index; //-2=before null value, -1=null value, 0 to _loSize=low region, _keys.Length - _hiSize to _keys.Length=high region, int.MaxValue - 1=end
        private K? _current;

        internal Enumerator(NullablePrimitiveSet<K> set)
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

                _index = _set._keys!.Length - _set._hiSize; //Jump to high region
            }

            if (_index == _set._keys!.Length)
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
        }

        public void Dispose() { }
    }
    #region ISet Explicit / Remaining Implementation
    public bool IsReadOnly => false;

    public void ExceptWith(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Count == 0)
            return;
        if (other is NullablePrimitiveSet<K> otherSet && otherSet.Count == 0)
            return;

        foreach (var key in other)
            Remove(key);
    }

    public void IntersectWith(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var t = new NullablePrimitiveSet<K>();
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
        _hasNullKey = t._hasNullKey;
    }

    public void SymmetricExceptWith(IEnumerable<K?> other)
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

    public void UnionWith(IEnumerable<K?> other)
    {
        ArgumentNullException.ThrowIfNull(other);
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