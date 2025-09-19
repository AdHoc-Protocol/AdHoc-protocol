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
using System.Runtime.Intrinsics;
using System.Text;

namespace org.unirail.collections;

///<summary>
///A high-performance struct for storing a set of one-byte keys (byte or sbyte) and an optional null key,
///using a Vector256<ulong> to represent 256 bits (keys 0-255). Optimized for SIMD operations, minimal allocations, and safe code.
///The _count field tracks the number of elements, with -1 indicating an invalid state.
///Check <see cref="IsValid"/> once before start using the NullableByteSet (as you do check not is null with a reference type)
///</summary>
///<typeparam name="K">Key type, restricted to a one-byte struct (byte or sbyte).</typeparam>
public struct NullableByteSet<K> : ISet<K?>, IReadOnlySet<K?>
    where K : unmanaged
{
    private Vector256<ulong> _bits = Vector256<ulong>.Zero; //256 bits: 4 ulongs for keys 0-63, 64-127, 128-191, 192-255
    internal uint _version = 0;                             //Tracks modifications for enumeration consistency

    internal bool _hasNullKey = false; //Tracks null key presence

    ///<summary>
    ///Tracks the total number of elements in the set.
    ///<list type="bullet">
    ///<item><term>-1</term><description>The set is in an invalid state (uninitialized or explicitly invalidated).</description></item>
    ///<item><term>-2</term><description>The actual count needs to be recalculated (lazy evaluation).</description></item>
    ///<item><term>0 to 257</term><description>The precise number of elements currently in the set (0-256 byte keys + 0 or 1 null key).</description></item>
    ///</list>
    ///</summary>
    private int _count = -1; //Total elements, -1 indicates invalid state

    ///<summary>
    ///Gets the number of elements contained in the set.
    ///This property uses lazy evaluation: if the count is marked for recalculation (-2),
    ///it will compute the PopCount for all bits and update the internal count.
    ///</summary>
    ///<exception cref="InvalidOperationException">Thrown if the set is in an invalid state (i.e., <see cref="IsValid"/> is false).</exception>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count switch
        {
            -
            1 => throw new InvalidOperationException("Set is not valid."),
            -
            2 => _count = BitOperations.PopCount(_bits.GetElement(0)) +
                          BitOperations.PopCount(_bits.GetElement(1)) +
                          BitOperations.PopCount(_bits.GetElement(2)) +
                          BitOperations.PopCount(_bits.GetElement(3)) +
                          (_hasNullKey ? 1 : 0), //Account for the null key
            _ => _count
        };
    }

    ///<summary>
    ///Gets a value indicating whether the set is in a valid state (as not null in reference types).
    ///Check the `IsValid` once before starting using the NullableByteSet (as you do check not is null with a reference type)
    ///</summary>
    public bool IsValid => _count != -1;

    ///<summary>
    ///Marks the set as invalid and clears its contents.
    ///An invalid set cannot be used until re-initialized by a constructor.
    ///</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invalidate() => _count = -1;

    //Static constructor for K type validation, runs once per generic instantiation (e.g., NullableByteSet<byte>).
    static NullableByteSet()
    {
        if (Unsafe.SizeOf<K>() != 1)
            throw new InvalidOperationException($"The Key type {typeof(K).Name} is {Unsafe.SizeOf<K>()} bytes, but must be 1 bytes.");
    }

    ///<summary>
    ///Initializes an empty <see cref="NullableByteSet{K}"/>, validating that K is a one-byte type.
    ///</summary>
    ///<exception cref="InvalidOperationException">Thrown by static constructor if K is not byte or sbyte.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NullableByteSet() => _count = 0;

    ///<summary>
    ///Initializes a new <see cref="NullableByteSet{K}"/> with elements from the specified items.
    ///</summary>
    ///<param name="items">An array of keys to add to the set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NullableByteSet(params K?[] items) : this()
    {
        foreach (var item in items)
            Add(item);
    }

    ///<summary>
    ///Determines whether the current set equals another <see cref="NullableByteSet{K}"/>.
    ///Two sets are equal if they have the same elements and the same internal count.
    ///</summary>
    ///<param name="other">The other set to compare with.</param>
    ///<returns>True if the sets are equal; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(NullableByteSet<K> other) => _count == other._count && _bits == other._bits && _hasNullKey == other._hasNullKey;

    public override bool Equals(object? obj) => obj is NullableByteSet<K> other && Equals(other);

    public static bool operator ==(NullableByteSet<K> left, NullableByteSet<K> right) => left.Equals(right);
    public static bool operator !=(NullableByteSet<K> left, NullableByteSet<K> right) => !left.Equals(right);

    ///<summary>
    ///Gets the hash code for the set.
    ///</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(_bits, _count, _hasNullKey);

    ///<summary>
    ///Calculates the rank of a key (number of non-null byte elements less than or equal the key).
    ///</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int Rank(byte key)
    {
        var index = key >> 6;
        var rank = BitOperations.PopCount(_bits.GetElement(index) << (63 - (key & 63))); //Corrected shift for relative key
        for (var i = 0; i < index; i++)
            rank += BitOperations.PopCount(_bits.GetElement(i));
        return rank;
    }

    ///<summary>
    ///Sets the bit for the specified byte key (0-255) to 1 (adds the key).
    ///</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Set1(byte key)
    {
        ref var was = ref Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref _bits), key >> 6);

        var now = was | (1UL << (key & 63)); //Use relative key bit

        if (was == now)
            return false;

        was = now;
        _count++;
        _version++;
        return true;
    }

    ///<summary>
    ///Adds the null key to the set if not already present. This is a low-level manipulation method.
    ///Throws an <see cref="InvalidOperationException"/> if the set is invalid (`!IsValid`).
    ///</summary>
    ///<returns><c>true</c> if the null key was added; <c>false</c> if it was already present.</returns>
    internal bool Set1() //Corresponds to adding null
    {
        if (_hasNullKey)
            return false;
        _hasNullKey = true;
        _count++;
        _version++;
        return true;
    }

    ///<summary>
    ///Sets the bit for the specified byte key (0-255) to 0 (removes the key).
    ///</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Set0(byte key)
    {
        ref var was = ref Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref _bits), key >> 6);

        var now = was & ~(1UL << (key & 63)); //Use relative key bit

        if (was == now)
            return false;

        was = now; //Direct write back
        _count--;
        _version++;
        return true;
    }

    ///<summary>
    ///Removes the null key from the set if present. This is a low-level manipulation method.
    ///Throws an <see cref="InvalidOperationException"/> if the set is invalid (`!IsValid`).
    ///</summary>
    ///<returns><c>true</c> if the null key was removed; <c>false</c> if it was not present.</returns>
    public bool Set0() //Corresponds to removing null
    {
        if (!_hasNullKey)
            return false;
        _hasNullKey = false;
        _count--;
        _version++;
        return true;
    }

    ///<summary>
    ///Gets or sets the presence of a non-null byte key (0-255).
    ///</summary>
    internal bool this[int key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Get((byte)key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _ = value ? Set1((byte)key) : Set0((byte)key);
    }

    ///<summary>
    ///Checks if a non-null byte key (0-255) is present in the set.
    ///</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Get(byte key) => (_bits.GetElement(key >> 6) & (1UL << (key & 63))) != 0;

    ///<summary>
    ///Checks if the current set contains all elements of another.
    ///</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsAll(NullableByteSet<K> other)
    {
        if (other._hasNullKey && !_hasNullKey)
            return false; //If other has null but we don't
        //Check if all bits set in other._bits are also set in _bits
        return Vector256.Equals(_bits & other._bits, other._bits).Equals(Vector256<ulong>.AllBitsSet);
    }

    ///<summary>
    ///Adds all items from the source collection. This clears the current set first.
    ///</summary>
    ///<param name="src">The enumerable collection of keys to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NullableByteSet<K> Set(IEnumerable<K?> src)
    {
        Clear(); //Ensure it starts empty before setting
        foreach (var item in src)
            Add(item);
        //Add internally updates _version and _count.
        return this;
    }

    ///<summary>
    ///Performs a union operation with another <see cref="NullableByteSet{K}"/>.
    ///Elements from the source set are added to the current set.
    ///</summary>
    ///<param name="src">The set to union with.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Or(NullableByteSet<K> src)
    {
        _bits |= src._bits;
        _hasNullKey |= src._hasNullKey; //Union null key presence
        _version++;
        _count = -2; //Mark _count for recalculation as bits and null key might have changed
    }

    ///<summary>
    ///Performs an intersection operation with another <see cref="NullableByteSet{K}"/>.
    ///Only elements present in both sets are retained in the current set.
    ///</summary>
    ///<param name="src">The set to intersect with.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void And(NullableByteSet<K> src)
    {
        _bits &= src._bits;
        _hasNullKey &= src._hasNullKey; //Intersect null key presence
        _version++;
        _count = -2; //Mark _count for recalculation
    }

    ///<summary>
    ///Removes elements present in another set (difference).
    ///</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ExceptWith(NullableByteSet<K> other)
    {
        _bits &= ~other._bits;
        if (other._hasNullKey)
            _hasNullKey = false; //If other had null, we remove null from ourselves
        _version++;
        _count = -2; //Mark _count for recalculation
    }

    ///<summary>
    ///Performs symmetric difference with another set.
    ///</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Xor(NullableByteSet<K> src)
    {
        _bits ^= src._bits;
        _hasNullKey ^= src._hasNullKey; //Symmetric difference for null key
        _version++;
        _count = -2; //Mark _count for recalculation
    }

    public const int INVALID = -1;

    ///<summary>
    ///Finds the smallest non-null byte key greater than the specified key.
    ///Returns <see cref="INVALID"/> if no such key exists.
    ///</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int Next1(int key)
    {
        if (++key >= 256)
            return INVALID; //No more keys in the 0-255 range

        var index = key >> 6;

        var word = _bits.GetElement(index) >> (key & 63); //Shift to make the current bit the LSB

        if (word != 0)
            return key + BitOperations.TrailingZeroCount(word); //Found in current word

        for (var i = index + 1; i < 4; i++)
        {
            word = _bits.GetElement(i);
            if (word != 0)
                return (i << 6) + BitOperations.TrailingZeroCount(word); //Found in a subsequent word
        }

        return INVALID; //No more non-null byte keys found
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IEnumerator<K?> IEnumerable<K?>.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<K?>
    {
        private readonly NullableByteSet<K> _set;
        private readonly uint _version; //For checking concurrent modification

        //State: -2=before start, -1=at null, 0-255=byte key, 256=finished
        private int _key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(in NullableByteSet<K> set)
        {
            _set = set;
            _version = set._version;
            _key = _set._hasNullKey ? -2 : -1; //If has null, start before it. Otherwise, start before first byte key.
        }

        public K? Current
        {
            get
            {
                //Check for invalid states: before start or after end
                if (_key < -1 || _key > 255)
                    throw new InvalidOperationException("Enumeration has either not started or has already finished.");

                //If state represents the null element, return null
                if (_key == -1)
                    return null;

                //Otherwise, _key is 0-255. Convert it to K.
                var byteKey = (byte)_key;
                return Unsafe.As<byte, K>(ref byteKey);
            }
        }

        object? IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            //Ensure the collection has not been modified during enumeration
            if (_version != _set._version)
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

            if (_key == -2) //Was before null, now move to it
            {
                _key = -1;
                return true;
            }

            //Find the next byte key.
            //If _key was -1 (from null), Next1(-1) finds the first key.
            //If _key was a byte, Next1(key) finds the next one.
            _key = _set.Next1(_key);

            if (_key != INVALID)
                return true; //Found a non-null byte key

            //No more keys found, set to a finished state
            _key = 256;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            if (_version != _set._version)
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            _key = _set._hasNullKey ? -2 : -1;
        }

        public void Dispose() { }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ICollection<K?>.Add(K? item) => Add(item);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(K? key)
    {
        if (!key.HasValue)
            return Set1();
        var kv = key.Value;
        return Set1(Unsafe.As<K, byte>(ref kv));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(K? key)
    {
        if (key.HasValue)
        {
            var kv = key.Value;
            return Set0(Unsafe.As<K, byte>(ref kv));
        }

        //key is null
        if (!_hasNullKey)
            return false; //Doesn't have null
        _hasNullKey = false;
        _count--; //Decrement count for null key
        _version++;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(K? key)
    {
        if (!key.HasValue)
            return _hasNullKey;
        var kv = key.Value;
        return Get(Unsafe.As<K, byte>(ref kv));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(K?[] dst, int dstIndex)
    {
        ArgumentNullException.ThrowIfNull(dst);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
        if (dst.Length - dstIndex < Count)
            throw new ArgumentException("Destination array is not long enough.");

        if (Count == 0)
            return;

        var currentDstIndex = dstIndex;

        //Handle null key first if it exists
        if (_hasNullKey)
            dst[currentDstIndex++] = default(K?); // Add null value (K? default is null)

        //Iterate over byte keys
        var byteKeyCount = Count - (_hasNullKey ? 1 : 0);
        var copiedByteKeys = 0;

        for (var i = 0; i < 4; i++)
        {
            var segment = _bits.GetElement(i);
            if (segment == 0)
                continue;

            for (var j = 0; j < 64; j++)
            {
                if ((segment & (1UL << j)) != 0) //Check the j-th bit in the current segment (multiple statements, so braces needed)
                {
                    var byteKey = (byte)((i << 6) + j);
                    dst[currentDstIndex++] = Unsafe.As<byte, K>(ref byteKey);
                    copiedByteKeys++;
                    if (copiedByteKeys == byteKeyCount)
                        return; //Single statement, no braces needed here
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (Count == 0)
            return; //Only clear if not already empty. Count recalculates, so this is safe.
        _bits = Vector256<ulong>.Zero;
        _hasNullKey = false;
        _count = 0;
        _version++;
    }

    public bool IsReadOnly => false;

    int IReadOnlyCollection<K?>.Count => Count;

    public bool IsProperSubsetOf(IEnumerable<K?> other)
    {
        var _other = new NullableByteSet<K>();
        _other.Set(other);
        return Count < _other.Count && _other.ContainsAll(this);
    }

    public bool IsProperSupersetOf(IEnumerable<K?> other)
    {
        var _other = new NullableByteSet<K>();
        _other.Set(other);
        return _other.Count < Count && ContainsAll(_other);
    }

    public bool IsSubsetOf(IEnumerable<K?> other) => new NullableByteSet<K>().Set(other).ContainsAll(this);

    public bool IsSupersetOf(IEnumerable<K?> other) => ContainsAll(new NullableByteSet<K>().Set(other));

    public bool SetEquals(IEnumerable<K?> other) => this == new NullableByteSet<K>().Set(other);

    public void IntersectWith(IEnumerable<K?> other) => And(new NullableByteSet<K>().Set(other));

    public void UnionWith(IEnumerable<K?> other) => Or(new NullableByteSet<K>().Set(other));

    public bool Overlaps(IEnumerable<K?> other)
    {
        if (Count == 0)
            return false;
        foreach (var item in other)
            if (Contains(item))
                return true;
        return false;
    }

    public void ExceptWith(IEnumerable<K?> other)
    {
        foreach (var item in other)
            Remove(item);
    }

    public void SymmetricExceptWith(IEnumerable<K?> other) => Xor(new NullableByteSet<K>().Set(other));

    public override string ToString() => ToJSON(new StringBuilder()).ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilder ToJSON(StringBuilder sb)
    {
        sb.Append('{');
        using var e = GetEnumerator();
        var first = true;
        while (e.MoveNext())
        {
            if (first)
                first = false;
            else
                sb.Append(',');

            sb.Append('"');
            if (e.Current.HasValue)
                sb.Append(e.Current.Value);
            else
                sb.Append("null"); //JSON "null" string for the key
            sb.Append("\":null");  //As per original, value is always "null"
        }

        sb.Append('}');
        return sb;
    }
}