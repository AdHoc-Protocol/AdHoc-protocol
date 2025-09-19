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
using System.Runtime.CompilerServices;
using System.Text;

namespace org.unirail.collections;

///<summary>
///Interface for a bit-packed list implementation that stores primitives with value in range 0..0xEF in a space-efficient manner
///using an array of ulongs. Each item occupies a fixed number of (1..7)bits , specified at creation.
///</summary>
public interface BitsList<T>
    where T : struct
{
    ///<summary>
    ///Abstract base class for BitsList, providing core functionality for a list of bit-packed integers.
    ///Each item occupies a fixed number of (1..7) bits, defined at construction, stored in an array of ulongs.
    ///</summary>
    public abstract class R : ICloneable, IReadOnlyList<T>, IEquatable<R>
    {
        ///<summary>
        ///Array storing the bit-packed data.
        ///</summary>
        protected ulong[] values = [];

        ///<summary>
        ///Converts a struct of type T to a byte.
        ///</summary>
        ///<param name="src">The source struct.</param>
        ///<returns>The byte representation of the struct.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static byte ToByte(T src) => Unsafe.As<T, byte>(ref src);

        ///<summary>
        ///Converts a byte to a struct of type T.
        ///</summary>
        ///<param name="src">The source byte.</param>
        ///<returns>The struct representation of the byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        protected static T FromByte(byte src) => Unsafe.As<byte, T>(ref src);

        ///<summary>
        ///Converts a ulong to a struct of type T by casting to byte.
        ///</summary>
        ///<param name="src">The source ulong.</param>
        ///<returns>The struct representation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static T FromByte(ulong src) => FromByte((byte)src);

        ///<summary>
        ///Converts a long to a struct of type T by casting to byte.
        ///</summary>
        ///<param name="src">The source long.</param>
        ///<returns>The struct representation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static T FromByte(long src) => FromByte((byte)src);

        ///<summary>
        ///Extracts a value of type T from a ulong at a given bit position using a mask.
        ///</summary>
        ///<param name="src">The source ulong.</param>
        ///<param name="bit">The starting bit position.</param>
        ///<param name="mask">The mask to apply.</param>
        ///<returns>The extracted value of type T.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static T GetValue(ulong src, int bit, ulong mask) => FromByte(src >> bit & mask);

        ///<summary>
        ///Extracts a value of type T from two consecutive ulongs, handling cases where the value spans across ulong boundaries.
        ///</summary>
        ///<param name="prev">The previous ulong element.</param>
        ///<param name="next">The next ulong element.</param>
        ///<param name="bit">The starting bit position in the previous ulong.</param>
        ///<param name="bits">The number of bits to extract.</param>
        ///<param name="mask">The mask to apply to the extracted bits.</param>
        ///<returns>The extracted value of type T.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static T GetValue(ulong prev, ulong next, int bit, int bits, ulong mask) => FromByte(((next & Mask(bit + bits - BITS)) << BITS - bit | prev >> bit) & mask);

        ///<summary>
        ///Number of items currently stored in the list.
        ///</summary>
        protected int count = 0;

        ///<summary>
        ///Mask to isolate the bits of each item.
        ///</summary>
        protected internal readonly ulong mask;

        ///<summary>
        ///Number of bits per item (1 to 7).
        ///</summary>
        public readonly byte bitsPerItem;

        ///<summary>
        ///Default value used when extending the list.
        ///</summary>
        public readonly T default_value;

        ///<summary>
        ///The value representing null for this bit list.
        ///</summary>
        public T null_val { get; protected set; } //The value representing null

        ///<summary>
        ///Constructs an empty BitsList with the specified number of bits per item and a default value of 0.
        ///</summary>
        ///<param name="bitsPerItem">The number of bits per item, must be between 1 and 7 (inclusive).</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if bitsPerItem is not between 1 and 7.</exception>
        protected R(byte bitsPerItem)
        {
            if (bitsPerItem < 1 || bitsPerItem > 7)
                throw new ArgumentOutOfRangeException(nameof(bitsPerItem), $"Bits per item must be between 1 and 7.");

            mask = Mask(this.bitsPerItem = bitsPerItem);
            default_value = default;
        }

        ///<summary>
        ///Constructs a BitsList with the specified bits per item and initial capacity.
        ///Initializes with zero size and a default value of 0.
        ///</summary>
        ///<param name="bitsPerItem">Number of bits per item (1 to 7).</param>
        ///<param name="length">Initial capacity in items.</param>
        protected R(byte bitsPerItem, uint length) : this(bitsPerItem) { values = new ulong[LengthForBits(length * this.bitsPerItem)]; }

        ///<summary>
        ///Constructs a BitsList with specified bits per item, default value, and size.
        ///Populates the list with the default value if non-zero.
        ///</summary>
        ///<param name="bitsPerItem">Number of bits per item (1 to 7).</param>
        ///<param name="defaultValue">Default value for items.</param>
        ///<param name="size">Initial size in items.</param>
        protected R(byte bitsPerItem, T defaultValue, int size) : this(bitsPerItem, (uint)size)
        {
            default_value = defaultValue;
            count = Math.Max(0, size); //Ensure size is not negative

            if (ToByte(default_value) != 0)
                for (var i = 0; i < count; i++)
                    Set_(this, (uint)i, defaultValue); //Initialize with default value if it's not zero
        }

        ///<summary>
        ///Returns the number of items in the list.
        ///</summary>
        public int Count => count;

        ///<summary>
        ///Returns the current capacity of the list in items.
        ///</summary>
        public int Capacity() => (values.Length << LEN) / bitsPerItem;

        ///<summary>
        ///Adjusts the storage capacity of the list.
        ///If <paramref name="items"/> is greater than 0, sets the capacity to at least <paramref name="items"/>, trimming excess if necessary.
        ///If <paramref name="items"/> is less than or equal to 0, clears the list and optionally allocates initial capacity for <code>-items</code> items if -items > 0.
        ///</summary>
        ///<param name="items">Desired capacity in items. If negative, the absolute value is used for initial capacity after clearing if greater than zero. If zero, clears and sets capacity to zero.</param>
        protected void Capacity(int items)
        {
            if (0 < items)
            {
                if (items < count)
                    count = items;
                var newLength = LengthForBits((uint)(items * bitsPerItem));
                if (values.Length != newLength)
                    Array.Resize(ref values, (int)newLength);
            }
            else
            {
                var newLength = LengthForBits((uint)(-items * bitsPerItem));
                if (values.Length != newLength)
                {
                    values = newLength == 0 ? [] : new ulong[newLength];
                    count = 0;
                }
                else
                    Clear();
            }
        }

        ///<summary>
        ///Clears all items in the list by setting their bits to zero and resets the size to 0.
        ///</summary>
        protected void Clear()
        {
            if (0 < count)
                Array.Fill(values, 0UL, 0, BitList.R.Len4Bits(count * bitsPerItem));
            count = 0;
        }

        ///<summary>
        ///Checks if the list is empty.
        ///</summary>
        ///<returns><code>true</code> if the list has no items, <code>false</code> otherwise.</returns>
        public bool IsEmpty() => count == 0;

        public override int GetHashCode()
        {
            if (count == 0)
                return 149989999;
            var hash = HashCode.Combine(149989999, Count);
            var i = Index(Count * bitsPerItem);
            if (i < values.Length)
                hash = HashCode.Combine(hash, values[i] & Mask(Bit(Count * bitsPerItem)));
            while (--i >= 0)
                hash = HashCode.Combine(hash, values[i]); //Hash the rest of the ulong array
            return hash;
        }

        public override bool Equals(object? other) => other != null && GetType() == other.GetType() && Equals((R)other);

        ///<summary>
        ///Determines whether the specified <see cref="R"/> is equal to the current <see cref="R"/>.
        ///</summary>
        ///<param name="other">The <see cref="R"/> to compare with the current <see cref="R"/>.</param>
        ///<returns><code>true</code> if the specified <see cref="R"/> is equal to the current <see cref="R"/>; otherwise, <code>false</code>.</returns>
        public bool Equals(R? other)
        {
            if (other == this)
                return true;
            if (other == null || other.count != count)
                return false;
            if (count == 0)
                return true;

            var lastLongIndex = Index(count * bitsPerItem);
            var lastMask = Mask(Bit(count * bitsPerItem));
            if (values.Length > lastLongIndex && other.values.Length > lastLongIndex && (values[lastLongIndex] & lastMask) != (other.values[lastLongIndex] & lastMask))
                return false;
            for (var i = lastLongIndex - 1; i >= 0; i--)
                if (values[i] != other.values[i])
                    return false;

            return true;
        }

        ///<summary>
        ///Retrieves the value of the last item in the list.
        ///</summary>
        ///<returns>The value of the last item as a <typeparamref name="T"/>.</returns>
        ///<exception cref="IndexOutOfRangeException">Thrown if the list is empty.</exception>
        public T GetLast() => this[Count - 1];

        T IReadOnlyList<T>.this[int index] => Get(index);

        ///<summary>
        ///Retrieves the value at the specified index.
        ///</summary>
        ///<param name="item">The index of the item (0 to <see cref="Count"/>-1).</param>
        ///<returns>The value at the specified index as a <typeparamref name="T"/>.</returns>
        ///<exception cref="IndexOutOfRangeException">Thrown if <paramref name="item"/> is out of bounds.</exception>
        public T Get(int item) => this[item];

        ///<summary>
        ///Gets the item at the specified index.
        ///</summary>
        ///<param name="item">The index of the item to access.</param>
        ///<returns>The item at the specified index.</returns>
        ///<exception cref="IndexOutOfRangeException">Thrown if <paramref name="item"/> is out of bounds.</exception>
        public virtual T this[int item]
        {
            get
            {
                if (item < 0 || count <= item)
                    throw new IndexOutOfRangeException($"Index {item} is out of range [0, {count})");
                var bitPosition = item * bitsPerItem;
                var index = Index(bitPosition);
                var bitOffset = Bit(bitPosition);
                return BITS < bitOffset + bitsPerItem ? GetValue(values[index], values[index + 1], bitOffset, bitsPerItem, mask) : GetValue(values[index], bitOffset, mask);
            }
            set => throw new NotImplementedException("BitsList.R is readonly");
        }

        ///<summary>
        ///Adds a value to the end of the list.
        ///</summary>
        ///<param name="dst">The <see cref="R"/> instance to modify.</param>
        ///<param name="src">The value to add.</param>
        protected static void Add(R dst, T src) => Set1(dst, dst.count, src);

        ///<summary>
        ///Adds a value at the specified index, shifting subsequent elements to the right.
        ///Extends the list if the index equals the current size; otherwise, inserts within the list.
        ///</summary>
        ///<param name="dst">The <see cref="R"/> instance to modify.</param>
        ///<param name="item">The index to insert at (0 to <see cref="Count"/>).</param>
        ///<param name="value">The value to insert.</param>
        ///<exception cref="IndexOutOfRangeException">Thrown if <paramref name="item"/> is less than 0.</exception>
        protected static void Add(R dst, int item, T value)
        {
            if (item < 0)
                throw new IndexOutOfRangeException("Index cannot be negative.");
            if (dst.count < item)
            {
                Set1(dst, item, value); //Extend list if item is beyond current count
                return;
            }

            dst.values = BitList.RW.ShiftLeft(dst.values, item * dst.bitsPerItem, dst.count * dst.bitsPerItem, dst.bitsPerItem, true);
            dst.count++;
            Set1(dst, item, value);
        }

        ///<summary>
        ///Protected static method to set a range of items from an array of type T starting at a given index.
        ///</summary>
        ///<param name="dst">The destination R instance.</param>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The array of type T values to set.</param>
        protected static void Set(R dst, int from, ReadOnlySpan<T> src)
        {
            for (var i = 0; i < src.Length; i++)
                Set1(dst, from + i, src[i]);
        }

        ///<summary>
        ///Protected static method to set a range of items from a byte array starting at a given index.
        ///</summary>
        ///<param name="dst">The destination R instance.</param>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The byte array of values to set.</param>
        protected static void Set(R dst, int from, ReadOnlySpan<byte> src)
        {
            for (var i = 0; i < src.Length; i++)
                Set1(dst, from + i, FromByte(src[i]));
        }

        ///<summary>
        ///Protected static method to set a range of items from a sbyte array starting at a given index.
        ///</summary>
        ///<param name="dst">The destination R instance.</param>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The sbyte array of values to set.</param>
        protected static void Set(R dst, int from, ReadOnlySpan<sbyte> src)
        {
            for (var i = 0; i < src.Length; i++)
                Set1(dst, from + i, FromByte(src[i]));
        }

        ///<summary>
        ///Protected static method to set a range of items from a ushort array starting at a given index.
        ///</summary>
        ///<param name="dst">The destination R instance.</param>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The ushort array of values to set.</param>
        protected static void Set(R dst, int from, ReadOnlySpan<ushort> src)
        {
            for (var i = 0; i < src.Length; i++)
                Set1(dst, from + i, FromByte(src[i]));
        }

        ///<summary>
        ///Protected static method to set a range of items from a short array starting at a given index.
        ///</summary>
        ///<param name="dst">The destination R instance.</param>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The short array of values to set.</param>
        protected static void Set(R dst, int from, ReadOnlySpan<short> src)
        {
            for (var i = 0; i < src.Length; i++)
                Set1(dst, from + i, FromByte(src[i]));
        }

        ///<summary>
        ///Protected static method to set a range of items from an int array starting at a given index.
        ///</summary>
        ///<param name="dst">The destination R instance.</param>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The int array of values to set.</param>
        protected static void Set(R dst, int from, ReadOnlySpan<int> src)
        {
            for (var i = 0; i < src.Length; i++)
                Set1(dst, from + i, FromByte(src[i]));
        }

        ///<summary>
        ///Protected static method to set a range of items from a uint array starting at a given index.
        ///</summary>
        ///<param name="dst">The destination R instance.</param>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The uint array of values to set.</param>
        protected static void Set(R dst, int from, ReadOnlySpan<uint> src)
        {
            for (var i = 0; i < src.Length; i++)
                Set1(dst, from + i, FromByte(src[i]));
        }

        ///<summary>
        ///Protected static method to set a range of items from a long array starting at a given index.
        ///</summary>
        ///<param name="dst">The destination R instance.</param>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The long array of values to set.</param>
        protected static void Set(R dst, int from, ReadOnlySpan<long> src)
        {
            for (var i = 0; i < src.Length; i++)
                Set1(dst, from + i, FromByte(src[i]));
        }

        ///<summary>
        ///Protected static method to set a range of items from a ulong array starting at a given index.
        ///</summary>
        ///<param name="dst">The destination R instance.</param>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The ulong array of values to set.</param>
        protected static void Set(R dst, int from, ReadOnlySpan<ulong> src)
        {
            for (var i = 0; i < src.Length; i++)
                Set1(dst, from + i, FromByte(src[i]));
        }

        ///<summary>
        ///Sets a value at the specified index, extending the list if necessary.
        ///If the index is beyond the current size, fills intervening positions with the default value.
        ///</summary>
        ///<param name="dst">The <see cref="R"/> instance to modify.</param>
        ///<param name="item">The index to set (0 to <see cref="Count"/>).</param>
        ///<param name="src">The value to set.</param>
        protected static void Set1(R dst, int item, T src)
        {
            var valueByte = ToByte(src);
            var maskedValue = valueByte & dst.mask;
            var totalBits = item * dst.bitsPerItem;

            if (item < dst.count)
            {
                var index = Index(totalBits);
                var bit = Bit(totalBits);
                var hi = BITS - bit;

                if (hi < dst.bitsPerItem)
                {
                    var mask = ~0UL << bit;
                    dst.values[index] = dst.values[index] & ~mask | maskedValue << bit;
                    var bitsInNextUlong = dst.bitsPerItem - hi;
                    dst.values[index + 1] = dst.values[index + 1] & ~Mask(bitsInNextUlong) | maskedValue >> hi;
                }
                else
                {
                    var mask = dst.mask << bit;
                    dst.values[index] = dst.values[index] & ~mask | maskedValue << bit;
                }

                return;
            }

            if (dst.Capacity() <= item)
                dst.Capacity((int)Math.Max(dst.Capacity() * 3 / 2, LengthForBits(totalBits + dst.bitsPerItem) * BITS / dst.bitsPerItem)); //Ensure capacity in items
            if (ToByte(dst.default_value) != 0)
                for (var i = dst.count; i < item; i++)
                    Set_(dst, (uint)i, dst.default_value);

            Set_(dst, (uint)item, src);
            dst.count = item + 1;
        }

        ///<summary>
        ///Appends a value at the specified index.
        ///Handles cases where the value spans two ulongs.
        ///</summary>
        ///<param name="dst">The <see cref="R"/> instance to modify.</param>
        ///<param name="item">The index to append at.</param>
        ///<param name="src">The value to append.</param>
        private static void Set_(R dst, uint item, T src)
        {
            var valueByte = ToByte(src);
            var maskedValue = valueByte & dst.mask;

            var bitPosition = (int)item * dst.bitsPerItem;
            var index = Index(bitPosition);
            var bitOffset = Bit(bitPosition);
            var bitsRemainingInUlong = BITS - bitOffset;

            if (bitsRemainingInUlong < dst.bitsPerItem)
            {
                var maskForFirstUlong = ~0UL << bitOffset;
                dst.values[index] = dst.values[index] & ~maskForFirstUlong | maskedValue << bitOffset;
                var bitsInNextUlong = dst.bitsPerItem - bitsRemainingInUlong;
                dst.values[index + 1] = dst.values[index + 1] & ~Mask(bitsInNextUlong) | maskedValue >> bitsRemainingInUlong;
            }
            else
            {
                var maskForUlong = dst.mask << bitOffset;
                dst.values[index] = dst.values[index] & ~maskForUlong | maskedValue << bitOffset;
            }
        }

        ///<summary>
        ///Removes an item at the specified index, shifting subsequent elements to the left.
        ///If the index is the last item, simply reduces the size unless the default value requires clearing.
        ///</summary>
        ///<param name="dst">The <see cref="R"/> instance to modify.</param>
        ///<param name="item">The index to remove (0 to <see cref="Count"/>-1).</param>
        protected static void RemoveAt(R dst, int item)
        {
            if (item < 0 || item >= dst.count)
                throw new IndexOutOfRangeException($"Index {item} is out of range [0, {dst.count})");

            if (item == dst.count - 1)
            {
                if (ToByte(dst.default_value) == 0)
                    Set_(dst, (uint)item, default);
                dst.count--;
                return;
            }

            BitList.RW.ShiftRight(dst.values, dst.values, item * dst.bitsPerItem, dst.count * dst.bitsPerItem, dst.bitsPerItem, true);
            dst.count--;
        }

        public object Clone()
        {
            try
            {
                var dst = (R)MemberwiseClone();
                if (dst.Capacity() > 0)
                    dst.values = (ulong[])values.Clone();
                return dst;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null!;
            }
        }

        public override string ToString() => ToString(null).ToString();

        ///<summary>
        ///Appends a string representation of the BitList to the provided StringBuilder.
        ///If null StringBuilder is provided, a new one will be created.
        ///</summary>
        ///<param name="dst">The StringBuilder to append to, or null to create a new one.</param>
        ///<returns>The StringBuilder containing the string representation.</returns>
        public StringBuilder ToString(StringBuilder? dst)
        {
            if (dst == null)
                dst = new StringBuilder(Count * 4); //Initialize StringBuilder if null, estimate capacity
            else
                dst.EnsureCapacity(dst.Length + Count * 4); //Ensure capacity

            var currentULong = values.Length > 0 ? values[0] : 0; //Avoid index out of range if values is empty

            for (int itemIndex = 0, bitPosition = 0; itemIndex < Count; itemIndex++, bitPosition += bitsPerItem)
            {
                var bitOffset = Bit(bitPosition);
                var nextULongIndex = Index(bitPosition) + 1;
                var itemValue = BITS < bitOffset + bitsPerItem ? GetValue(currentULong, nextULongIndex < values.Length ? values[nextULongIndex] : 0, bitOffset, bitsPerItem, mask) //Handle boundary and array bounds
                                                               : GetValue(currentULong, bitOffset, mask);

                dst.Append(itemValue).Append('\t');
                if ((itemIndex + 1) % 10 == 0)
                    dst.Append('\t').Append((itemIndex + 1) / 10 * 10).Append('\n');
                if (BITS < bitOffset + bitsPerItem && nextULongIndex < values.Length)
                    currentULong = values[nextULongIndex]; //Update currentULong if needed and within bounds
            }

            return dst;
        }

        ///<summary>
        ///Finds the index of the first occurrence of a specified value in the list.
        ///</summary>
        ///<param name="value">The value to search for.</param>
        ///<returns>The index of the first occurrence of <paramref name="value"/> if found; otherwise, -1.</returns>
        public int IndexOf(T value)
        {
            var v = (byte)(ToByte(value) & mask);
            for (var i = 0; i < count; i++)
                if (v == ToByte(this[i]))
                    return i;
            return -1;
        }

        ///<summary>
        ///Finds the index of the last occurrence of a specified value in the list.
        ///</summary>
        ///<param name="value">The value to search for.</param>
        ///<returns>The index of the last occurrence of <paramref name="value"/> if found; otherwise, -1.</returns>
        public int LastIndexOf(T value) => LastIndexOf(count - 1, value);

        ///<summary>
        ///Finds the index of the last occurrence of a specified value in the list within the range of elements up to the specified index.
        ///</summary>
        ///<param name="from">The zero-based starting index of the backward search.</param>
        ///<param name="value">The value to search for.</param>
        ///<returns>The index of the last occurrence of <paramref name="value"/> if found; otherwise, -1.</returns>
        public int LastIndexOf(int from, T value)
        {
            var v = (byte)(ToByte(value) & mask);
            for (var i = Math.Min(from, count - 1); 0 <= i; i--)
                if (v == ToByte(this[i]))
                    return i;
            return -1;
        }

        ///<summary>
        ///Removes all occurrences of the specified value from the list.
        ///</summary>
        ///<param name="dst">The <see cref="R"/> instance to modify.</param>
        ///<param name="value">The value to remove.</param>
        protected static void Remove(R dst, T value)
        {
            for (var i = dst.count; -1 < (i = dst.LastIndexOf(i, value));)
                RemoveAt(dst, i);
        }

        ///<summary>
        ///Checks if the list contains the specified value.
        ///</summary>
        ///<param name="value">The value to check for.</param>
        ///<returns><code>true</code> if the value is present, <code>false</code> otherwise.</returns>
        public bool Contains(T value) => IndexOf(value) != -1;

        ///<summary>
        ///Converts the list to an array of <typeparamref name="T"/>.
        ///</summary>
        ///<param name="dst">The destination array. If null or insufficient, a new array is created.</param>
        ///<returns>An array of <typeparamref name="T"/> containing all values, or an empty array if the list is empty.</returns>
        public T[] ToArray(T[]? dst)
        {
            if (count == 0)
                return [];        //Return empty array instead of null
            dst ??= new T[count]; //Create new array if dst is null or too small
            if (dst.Length < count)
                dst = new T[count]; //Ensure dst is large enough

            for (var i = 0; i < count; i++)
                dst[i] = Get(i);
            return dst;
        }

        public void CopyTo(T[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
            if (dst.Length - dstIndex < Count)
                throw new ArgumentException("Destination array is not long enough.");

            for (var i = 0; i < count; i++)
            {
                dst[dstIndex + i] = Get(i);
            }
        }

        ///<summary>
        ///Creates a mask with the specified number of least significant bits set to 1.
        ///Example: <code>Mask(3)</code> returns <code>0b111</code> (7 in decimal).
        ///</summary>
        ///<param name="bits">Number of bits for the mask, ranging from 0 to 7.</param>
        ///<returns>A <see cref="ulong"/> mask with the specified bits set.</returns>
        public static ulong Mask(int bits) => (1UL << bits) - 1;

        ///<summary>
        ///Computes the array index for a given bit position.
        ///Since each <see cref="ulong"/> holds BITS bits, the index is calculated as <code>bit_position / BITS</code>.
        ///</summary>
        ///<param name="bitPosition">The bit position in the array.</param>
        ///<returns>The index of the <see cref="ulong"/> containing the bit.</returns>
        protected static int Index(int bitPosition) => bitPosition >> LEN; //Using LEN constant

        ///<summary>
        ///Computes the bit offset within a <see cref="ulong"/> for a given bit position.
        ///This is the remainder when <paramref name="bitPosition"/> is divided by BITS.
        ///</summary>
        ///<param name="bitPosition">The bit position in the array.</param>
        ///<returns>The bit offset (0 to 63) within the <see cref="ulong"/>.</returns>
        protected static int Bit(int bitPosition) => bitPosition & MASK; //Using MASK constant

        ///<summary>
        ///Extracts a value from a <see cref="ulong"/> starting at a specified bit position.
        ///The value is isolated using the provided mask.
        ///</summary>
        ///<param name="src">Source <see cref="ulong"/> value containing the bits.</param>
        ///<param name="bit">Starting bit position within the <see cref="ulong"/> (0 to 63).</param>
        ///<param name="mask">Mask to isolate the desired bits.</param>
        ///<returns>The extracted value as a <see cref="byte"/>.</returns>
        protected static byte GetByteValue(ulong src, int bit, ulong mask) => (byte)(src >> bit & mask);

        ///<summary>
        ///Extracts a value spanning two <see cref="ulong"/>s starting at a specified bit position.
        ///used when the value crosses the boundary between two <see cref="ulong"/>s.
        ///</summary>
        ///<param name="prev">Previous <see cref="ulong"/> value.</param>
        ///<param name="next">Next <see cref="ulong"/> value.</param>
        ///<param name="bit">Starting bit position in <paramref name="prev"/> (0 to 63).</param>
        ///<param name="bits">Number of bits to extract.</param>
        ///<param name="mask">Mask to isolate the desired bits.</param>
        ///<returns>The extracted value as a <see cref="byte"/>.</returns>
        protected static byte GetByteValue(ulong prev, ulong next, int bit, int bits, ulong mask) => (byte)(((next & Mask(bit + bits - BITS)) << BITS - bit | prev >> bit) & mask);

        ///<summary>
        ///Calculates the number of <see cref="ulong"/>s needed to store a given number of bits.
        ///Uses ceiling division by BITS.
        ///</summary>
        ///<param name="bits">Total number of bits to store.</param>
        ///<returns>Number of <see cref="ulong"/>s required.</returns>
        public static uint LengthForBits(uint bits) => (bits + BITS - 1) / BITS; //More efficient ceiling division

        public static uint LengthForBits(int bits) => (uint)((bits + BITS - 1) / BITS); //More efficient ceiling division

        ///<summary>
        ///Number of bits in a <see cref="ulong"/>.
        ///</summary>
        protected const byte BITS = 64;

        ///<summary>
        ///Mask for bit operations, equal to 63 (0b111111).
        ///</summary>
        public const byte MASK = BITS - 1;

        ///<summary>
        ///Number of bits to shift for indexing, equal to log2(BITS) = 6.
        ///</summary>
        public const byte LEN = 6;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); //Delegate to the generic version (or the struct version, see below)

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator(); //Delegate to the struct-returning version

        public Enumerator GetEnumerator() => new(this);

        ///<summary>
        ///Enumerator struct for <see cref="R"/>.
        ///</summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly R _list;
            private int _index;
            private T _current; //Store current value to avoid get property call twice in some cases

            internal Enumerator(R list)
            {
                _list = list;
                _index = -1;
                _current = default; //Initialize current
            }

            ///<inheritdoc/>
            public T Current => _index == -1 ? throw new InvalidOperationException("Enumeration has either not started or has finished.") : _current;

            ///<inheritdoc/>
            object? IEnumerator.Current => Current;

            ///<inheritdoc/>
            public bool MoveNext()
            {
                if (++_index < _list.Count)
                {
                    _current = _list[_index]; //Fetch and store current value
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _index = -1;
                _current = default; //Reset current
            }

            public void Dispose() { }
        }
    }

    ///<summary>
    ///Read-write extension of <see cref="BitsList{T}"/>, adding methods that modify the list and return the instance for chaining.
    ///Extends <see cref="R"/> with additional functionality for dynamic manipulation.
    ///</summary>
    public class RW : R, IList<T>
    {
        public RW(byte bitsPerItem) : base(bitsPerItem) { }

        public RW(byte bitsPerItem, uint length) : base(bitsPerItem, length) { }

        public RW(byte bitsPerItem, T defaultValue, int size) : base(bitsPerItem, defaultValue, size) { }

        ///<inheritdoc/>
        public bool IsReadOnly => false;

        public void Add(T item) => R.Add(this, item);

        ///<inheritdoc/>
        public void Insert(int index, T item) => R.Add(this, index, item);

        ///<inheritdoc/>
        public bool Remove(T item)
        {
            var initialCount = Count;
            R.Remove(this, item);
            return initialCount != Count;
        }

        public void RemoveAt(int index) => R.RemoveAt(this, index);

        ///<summary>
        ///Removes the last item and returns this instance for chaining.
        ///</summary>
        ///<returns>This <see cref="RW"/> instance.</returns>
        ///<exception cref="IndexOutOfRangeException">Thrown if the list is empty.</exception>
        public RW RemoveLast()
        {
            if (count == 0)
                throw new IndexOutOfRangeException("List is empty");
            RemoveAt(this, count - 1);
            return this;
        }

        ///<summary>
        ///Sets the value of the last item and returns this instance for chaining.
        ///</summary>
        ///<param name="value">The value to set.</param>
        ///<returns>This <see cref="RW"/> instance.</returns>
        ///<exception cref="IndexOutOfRangeException">Thrown if the list is empty.</exception>
        public RW SetLast(T value)
        {
            if (count == 0)
                throw new IndexOutOfRangeException("List is empty");
            Set1(this, count - 1, value);
            return this;
        }

        ///<summary>
        ///Sets the value at the specified index and returns this instance for chaining.
        ///</summary>
        ///<param name="item">The index to set (0 to <see cref="Count"/>).</param>
        ///<param name="value">The value to set.</param>
        ///<returns>This <see cref="RW"/> instance.</returns>
        ///<exception cref="IndexOutOfRangeException">Thrown if <paramref name="item"/> is out of bounds.</exception>
        public RW Set(int item, T value)
        {
            Set1(this, item, value);
            return this;
        }

        ///<inheritdoc/>
        public override T this[int item]
        {
            get => base[item];
            set => Set1(this, item, value);
        }

        ///<summary>
        ///Protected static method to set a range of items from an array of type T starting at a given index.
        ///</summary>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The array of type T values to set.</param>
        public RW Set(int from, ReadOnlySpan<T> src)
        {
            Set(this, from, src);
            return this;
        }

        ///<summary>
        ///Protected static method to set a range of items from a byte array starting at a given index.
        ///</summary>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The byte array of values to set.</param>
        public RW Set(int from, ReadOnlySpan<byte> src)
        {
            Set(this, from, src);
            return this;
        }

        ///<summary>
        ///Protected static method to set a range of items from a sbyte array starting at a given index.
        ///</summary>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The sbyte array of values to set.</param>
        public RW Set(int from, ReadOnlySpan<sbyte> src)
        {
            Set(this, from, src);
            return this;
        }

        ///<summary>
        ///Protected static method to set a range of items from a ushort array starting at a given index.
        ///</summary>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The ushort array of values to set.</param>
        public RW Set(int from, ReadOnlySpan<ushort> src)
        {
            Set(this, from, src);
            return this;
        }

        ///<summary>
        ///Protected static method to set a range of items from a short array starting at a given index.
        ///</summary>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The short array of values to set.</param>
        public RW Set(int from, ReadOnlySpan<short> src)
        {
            Set(this, from, src);
            return this;
        }

        ///<summary>
        ///Protected static method to set a range of items from an int array starting at a given index.
        ///</summary>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The int array of values to set.</param>
        public RW Set(int from, ReadOnlySpan<int> src)
        {
            Set(this, from, src);
            return this;
        }

        ///<summary>
        ///Protected static method to set a range of items from a uint array starting at a given index.
        ///</summary>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The uint array of values to set.</param>
        public RW Set(int from, ReadOnlySpan<uint> src)
        {
            Set(this, from, src);
            return this;
        }

        ///<summary>
        ///Protected static method to set a range of items from a long array starting at a given index.
        ///</summary>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The long array of values to set.</param>
        public RW Set(int from, ReadOnlySpan<long> src)
        {
            Set(this, from, src);
            return this;
        }

        ///<summary>
        ///Protected static method to set a range of items from a ulong array starting at a given index.
        ///</summary>
        ///<param name="from">The starting item index.</param>
        ///<param name="src">The ulong array of values to set.</param>
        public RW Set(int from, ReadOnlySpan<ulong> src)
        {
            Set(this, from, src);
            return this;
        }

        ///<summary>
        ///Retains only the items present in the specified list and indicates if the list changed.
        ///Removes all items not found in <paramref name="chk"/>.
        ///</summary>
        ///<param name="chk">The <see cref="R"/> instance containing values to retain.</param>
        ///<returns><code>true</code> if this list was modified, <code>false</code> otherwise.</returns>
        public bool RetainAll(R? chk)
        {
            if (chk == null)
                return false;
            var originalCount = Count;
            for (var item = 0; item < Count; item++)
            {
                if (!chk.Contains(this[item]))
                {
                    Remove(this, this[item]);
                    item--; //Decrement item to re-examine the current index after removal
                }
            }

            return originalCount != Count;
        }

        ///<summary>
        ///Adjusts the capacity to match the current size and returns this instance for chaining.
        ///Trims excess capacity to optimize memory usage.
        ///</summary>
        ///<returns>This <see cref="RW"/> instance.</returns>
        public RW Fit() => Capacity(count);

        ///<summary>
        ///Sets the capacity of the list and returns this instance for chaining.
        ///If <paramref name="items"/> is less than 1, clears the list and sets capacity to 0; otherwise, adjusts to at least <paramref name="items"/>.
        ///</summary>
        ///<param name="items">The desired capacity in items.</param>
        ///<returns>This <see cref="RW"/> instance.</returns>
        public RW Capacity(int items)
        {
            if (items < 1)
            {
                values = [];
                count = 0;
            }
            else
                base.Capacity(items);

            return this;
        }

        ///<inheritdoc/>
        public new void Clear() => base.Clear();

        ///<summary>
        ///Sets the size of the list, extending with the default value if necessary, and returns this instance.
        ///If <paramref name="count"/> is less than 1, clears the list; if greater than current size, extends with <see cref="default_value"/>.
        ///</summary>
        ///<param name="count">The desired size in items.</param>
        ///<returns>This <see cref="RW"/> instance.</returns>
        public RW Count_(int count)
        {
            if (count < 0)
                Clear();
            else if (this.count < count)
                Set1(this, count - 1, default_value);
            else
                this.count = count;
            return this;
        }

        public new RW Clone() => (RW)base.Clone();

        ///<inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        ///<inheritdoc/>
        public new IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        ///<inheritdoc/>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex) { base.CopyTo(array, arrayIndex); }
    }
}