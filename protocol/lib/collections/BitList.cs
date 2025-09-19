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
using System.Text;

namespace org.unirail.collections;

///<summary>
///Represents a dynamic list of bits, optimized for both space (memory footprint)
///and time (performance) efficiency.
///<para>
///<see cref="BitList"/> acts like a highly efficient, growable bit vector or bitset,
///suitable for managing large sequences of boolean flags or performing complex
///bitwise operations. It employs internal optimizations, such as tracking
///sequences of leading '1's implicitly, to minimize memory usage.
///</para>
///<para>
///Implementations provide methods for querying individual bits, finding runs of
///'0's or '1's, calculating cardinality (rank/population count), and potentially
///modifying the bit sequence.
///</para>
///</summary>
public interface BitList
{
    ///<summary>
    ///An abstract base class providing a read-only view and core implementation
    ///details for a <see cref="BitList"/>.
    ///<para>
    ///<b>Bit Indexing and Representation:</b>
    ///.                 MSB                LSB
    ///.                 |                 |
    ///bits in the list [0, 0, 0, 1, 1, 1, 1] Leading 3 zeros and trailing 4 ones
    ///index in the list 6 5 4 3 2 1 0
    ///shift left  ≪
    ///shift right  ≫
    ///</para>
    ///<para>
    ///<b>Storage Optimization:</b>
    ///This class utilizes several optimizations:
    ///<list type="bullet">
    ///    <item><b>Trailing Ones (`trailingOnesCount`):</b> A sequence of '1' bits at the
    ///    beginning (indices 0 to <see cref="trailingOnesCount"/> - 1) are stored implicitly
    ///    by just keeping count, not using space in the <see cref="values"/> array.</item>
    ///    <item><b>Explicit Bits (`values`):</b> Bits *after* the implicit trailing ones
    ///    are packed into a <see cref="ulong"/>[] array. The first conceptual bit stored in
    ///    <see cref="values"/> always corresponds to the first '0' bit after the trailing ones.
    ///    The last bit stored in <see cref="values"/> corresponds to the highest-indexed '1'
    ///    bit (<see cref="Last1"/>).</item>
    ///    <item><b>Trailing Zeros:</b> '0' bits from index <see cref="Last1"/> + 1 up to
    ///    <see cref="Count"/> - 1 are also implicit and not stored in <see cref="values"/>.</item>
    ///    <item><b>Used Count (`used`):</b> Tracks how many <see cref="ulong"/> elements in the
    ///    <see cref="values"/> array actually contain non-zero data, allowing the array to
    ///    potentially be larger than strictly needed for current bits.</item>
    ///</list>
    ///This structure provides the foundation for concrete readable and writable
    ///<see cref="BitList"/> implementations.
    ///</para>
    ///</summary>
    public abstract class R : ICloneable, IReadOnlyList<bool>, IEquatable<R>
    {
        ///<summary>
        ///The logical number of bits in this list. This defines the valid range of
        ///indices [0, count-1].
        ///It includes implicitly stored trailing ones and trailing zeros, as well as
        ///explicitly stored bits in <see cref="values"/>.
        ///</summary>
        protected internal int count;

        ///<summary>
        ///The count of consecutive '1' bits starting from index 0. These bits are
        ///stored implicitly and are *not* represented in the <see cref="values"/> array.
        ///If <see cref="trailingOnesCount"/> is 0, the list starts with a '0' (or is empty).
        ///</summary>
        protected internal int trailingOnesCount;

        ///<summary>
        ///The backing array storing the explicit bits of the <see cref="BitList"/>.
        ///<para>
        ///Contains bits from index <see cref="trailingOnesCount"/> up to <see cref="Last1"/>.
        ///Bits are packed into <see cref="ulong"/>s, 64 bits per element.
        ///Within each <see cref="ulong"/>, bits are stored LSB-first (index 0 of the conceptual
        ///sub-array of 64 bits corresponds to the lowest index within that block).
        ///The <see cref="values"/> array element at index <c>i</c> stores bits corresponding
        ///to the global bit indices
        ///<c>[trailingOnesCount + i*64, trailingOnesCount + (i+1)*64 - 1]</c>.
        ///</para>
        ///<para>
        ///Trailing zeros beyond <see cref="Last1"/> up to <see cref="Count"/> are not stored.
        ///May contain trailing <see cref="ulong"/> elements that are all zero.
        ///Initialized to a shared empty array for efficiency.
        ///</para>
        ///</summary>
        protected internal ulong[] values = [];

        ///<summary>
        ///The number of <see cref="ulong"/> elements currently used in the <see cref="values"/> array.
        ///This is the index of the highest element containing a '1' bit, plus one.
        ///It can be less than <see cref="values.Length"/>.
        ///A negative value (specifically, having the sign bit set via <c>used |= IO</c>)
        ///indicates that the count might be stale (due to operations like clearing bits
        ///in the last used word) and needs recalculation via <see cref="Used()"/>.
        ///</summary>
        protected internal int used;

        ///<summary>
        ///Returns the logical count (number of bits) of this <see cref="BitList"/>.
        ///This determines the valid range of bit indices [0, count-1].
        ///</summary>
        ///<returns>The number of bits in the list.</returns>
        public int Count => count;

        ///<summary>
        ///Calculates the minimum number of <see cref="ulong"/> elements needed to store a
        ///given number of bits.
        ///</summary>
        ///<param name="bits">The number of bits.</param>
        ///<returns>The required length of a <see cref="ulong"/>[] array.</returns>
        protected internal static int Len4Bits(int bits) => bits + BITS - 1 >> LEN;

        ///<summary>
        ///The base-2 logarithm of <see cref="BITS"/>, used for calculating array indices
        ///(<c>bit >> LEN</c>). Value is 6.
        ///</summary>
        protected const int LEN = 6;

        ///<summary>
        ///The number of bits in a <see cref="ulong"/>. Value is 64.
        ///</summary>
        protected const int BITS = 1 << LEN; //64

        ///<summary>
        ///A mask to extract the bit position within a <see cref="ulong"/> element
        ///(<c>bit & MASK</c>). Value is 63 (0b111111).
        ///</summary>
        protected const int MASK = BITS - 1; //63

        ///<summary>
        ///Calculates the index within the <see cref="values"/> array corresponding to a
        ///global bit index. Note: This does *not* account for <see cref="trailingOnesCount"/>.
        ///The bit index must be relative to the start of the <see cref="values"/> array.
        ///</summary>
        ///<param name="bit">The bit index *relative to the start of the <see cref="values"/> array*.</param>
        ///<returns>The index in the <see cref="values"/> array.</returns>
        protected static int index(int bit) => bit >> LEN;

        ///<summary>
        ///Creates a <see cref="ulong"/> mask with the least significant <paramref name="bits"/> set to '1'.
        ///For example, <c>Mask(3)</c> returns <c>0b111</c> (7).
        ///If <paramref name="bits"/> is 0, returns 0. If <paramref name="bits"/> is 64 or more, returns ~0UL (all ones).
        ///</summary>
        ///<param name="bits">The number of low-order bits to set (0-64).</param>
        ///<returns>A <see cref="ulong"/> with the specified number of LSBs set.</returns>
        protected static ulong Mask(int bits) => bits >= BITS ? ~0UL : (1UL << bits) - 1;

        ///<summary>
        ///Integer maximum value constant (<c>0x7FFFFFFF</c>). Used for bit manipulation on <see cref="used"/>.
        ///</summary>
        protected const int OI = int.MaxValue;

        ///<summary>
        ///Integer minimum value constant (<c>0x80000000</c>). Used to mark the <see cref="used"/> count as potentially stale.
        ///</summary>
        protected const int IO = int.MinValue;

        ///<summary>
        ///Calculates or retrieves the number of <see cref="ulong"/> elements in the
        ///<see cref="values"/> array that are actively used (contain at least one '1' bit).
        ///<para>
        ///If the internal <see cref="used"/> field is non-negative, it's considered accurate
        ///and returned directly. If it's negative (marked stale via <c>used |= IO</c>),
        ///this method recalculates the count by scanning <see cref="values"/> backwards from
        ///the last known potential position to find the highest-indexed non-zero element.
        ///The internal <see cref="used"/> field is updated with the accurate count before returning.
        ///</para>
        ///</summary>
        ///<returns>The number of <see cref="ulong"/> elements in <see cref="values"/> actively storing bits.
        ///Returns 0 if <see cref="values"/> is empty or contains only zeros.</returns>
        protected internal int Used()
        {
            if (used >= 0)
                return used;

            used &= OI;
            var u = used - 1;

            while (u >= 0 && values[u] == 0)
                u--;

            return used = u + 1;
        }

        ///<summary>
        ///Ensures the internal state (<see cref="count"/> and <see cref="values"/> array capacity) can accommodate
        ///the specified bit index, expanding if necessary. It also returns the calculated
        ///index within the <see cref="values"/> array for the given bit.
        ///<para>
        ///If <paramref name="bit"/> is greater than or equal to the current <see cref="Count"/>,
        ///<see cref="count"/> is updated to <paramref name="bit"/> + 1.
        ///If the calculated <see cref="values"/> index is outside the current bounds of used elements
        ///or the allocated length of <see cref="values"/>, the <see cref="values"/> array is resized (typically
        ///grows by 50%) and the <see cref="used"/> count is updated.
        ///</para>
        ///</summary>
        ///<param name="bit">The global bit position (0-indexed) to ensure accommodation for.</param>
        ///<returns>The index in the <see cref="values"/> array where the bit resides,
        ///or -1 if the bit falls within the implicit <see cref="trailingOnesCount"/> range.</returns>
        protected int Used(int bit)
        {
            if (count <= bit)
                count = bit + 1;
            var index = bit - trailingOnesCount >> LEN;
            if (index < 0)
                return -1;
            if (index < Used())
                return index;
            if (values.Length < (used = index + 1))
                Array.Resize(ref values, Math.Max(values.Length + (values.Length >> 1), used));
            return index;
        }

        ///<summary>
        ///Retrieves the value of the bit at the specified global index.
        ///</summary>
        ///<param name="bit">The global bit index (0-indexed) to retrieve.</param>
        ///<returns><c>true</c> if the bit at the specified index is '1', <c>false</c>
        ///if it is '0'. Returns <c>false</c> if the index is negative or
        ///greater than or equal to <see cref="Count"/>.</returns>
        public bool Get(int bit)
        {
            if (bit < 0 || bit >= count)
                return false;
            if (bit < trailingOnesCount)
                return true;
            var index = bit - trailingOnesCount >> LEN;
            return index < Used() && (values[index] & 1UL << (bit - trailingOnesCount & MASK)) != 0;
        }

        ///<summary>
        ///Retrieves the value of the bit at the specified index and returns one of two
        ///provided integer values based on the result.
        ///This is a convenience method equivalent to <c>get(bit) ? TRUE : FALSE</c>.
        ///</summary>
        ///<param name="bit">The global bit index (0-indexed) to check.</param>
        ///<param name="FALSE">The value to return if the bit at <paramref name="bit"/> is '0' or out of bounds.</param>
        ///<param name="TRUE">The value to return if the bit at <paramref name="bit"/> is '1'.</param>
        ///<returns><paramref name="TRUE"/> if <see cref="get"/> is true, otherwise <paramref name="FALSE"/>.</returns>
        public int Get(int bit, int FALSE, int TRUE) => Get(bit) ? TRUE : FALSE;

        ///<summary>
        ///Copies a range of bits from this <see cref="BitList"/> into a destination
        ///<see cref="ulong"/> array, starting at the beginning of the destination array.
        ///<para>
        ///Bits are copied starting from <paramref name="fromBit"/> (inclusive) up to
        ///<paramref name="toBit"/> (exclusive). The destination array <paramref name="dst"/> is assumed
        ///to be zero-initialized or the caller handles merging. Bits are packed into
        ///<paramref name="dst"/> starting at index 0, bit 0.
        ///</para>
        ///</summary>
        ///<param name="dst">The destination <see cref="ulong"/> array to copy bits into.</param>
        ///<param name="fromBit">The starting global bit index in this <see cref="BitList"/> (inclusive).</param>
        ///<param name="toBit">The ending global bit index in this <see cref="BitList"/> (exclusive).</param>
        ///<returns>The number of bits actually copied. This may be less than
        ///<c><paramref name="toBit"/> - <paramref name="fromBit"/></c> if the range exceeds the list's count or
        ///the destination array's capacity. Returns 0 if the range is invalid
        ///or out of bounds.</returns>
        public int Get(ulong[] dst, int fromBit, int toBit)
        {
            if (toBit <= fromBit || fromBit < 0 || Count <= fromBit)
                return 0;
            toBit = Math.Min(toBit, Count);
            var bitsToCopy = toBit - fromBit;

            //Calculate number of full 64-bit chunks that fit in dst
            var fullLongs = Math.Min(bitsToCopy >> LEN, dst.Length);

            for (var i = 0; i < fullLongs; i++)
                dst[i] = Get64(fromBit + i * 64);

            var copiedBits = fullLongs * 64;
            var remainingBits = bitsToCopy - copiedBits;
            if (remainingBits == 0)
                return copiedBits;

            dst[fullLongs] = Get64(fromBit + copiedBits) & Mask(remainingBits);

            return copiedBits + remainingBits;
        }

        ///<summary>
        ///Finds the next '1' bit in the <see cref="BitList"/> after the specified bit.
        ///</summary>
        ///<param name="bit">The bit to start searching from. Pass -1 to start from the beginning.</param>
        ///<returns>The index of the next '1' bit, or -1 if no '1' bit is found after the specified bit.</returns>
        public int Next1(int bit)
        {
            if (count == 0 || bit++ < -1 || count <= bit)
                return -1;
            if (bit < trailingOnesCount)
                return bit;

            var last1 = Last1;
            if (bit == last1)
                return last1;
            if (last1 < bit)
                return -1;

            var bitOffset = bit - trailingOnesCount;
            var index = bitOffset >> LEN;

            for (var value = values[index] & ~0UL << (bitOffset & MASK); ; value = values[++index])
                if (value != 0)
                    return trailingOnesCount + (index << LEN) + BitOperations.TrailingZeroCount(value);
        }

        ///<summary>
        ///Finds the next '0' bit in the <see cref="BitList"/> after the specified bit.
        ///</summary>
        ///<param name="bit">The bit to start searching from. Pass -1 to start from the beginning.</param>
        ///<returns>The index of next '0' bit, or -1 if no '0' bit is found after the specified index.</returns>
        public int Next0(int bit)
        {
            if (count == 0)
                return -1;

            if (++bit < trailingOnesCount)
                return trailingOnesCount == count ? -1 : trailingOnesCount;

            if (count <= bit)
                return -1;

            var last1 = Last1;

            if (bit == last1)
                return last1 + 1 < count ? bit + 1 : -1;

            if (last1 < bit)
                return last1 + 1 < count ? bit : -1;

            var bitOffset = bit - trailingOnesCount;
            var index = bitOffset >> LEN;

            for (var value = ~values[index] & ~0UL << (bitOffset & MASK); ; value = ~values[++index])
                if (value != 0)
                    return trailingOnesCount + (index << LEN) + BitOperations.TrailingZeroCount(value);
        }

        ///<summary>
        ///Finds the previous '1' bit in the <see cref="BitList"/> before the specified bit.
        ///</summary>
        ///<param name="bit">The bit to start searching from. Pass -1 or >= <see cref="Count"/>, to start searches from the end.</param>
        ///<returns>The index of previous '1' bit, or -1 if no '1' bit is found before the specified bit.</returns>
        public int Prev1(int bit)
        {
            if (Count == 0 || bit < -1)
                return -1;

            bit = Count <= bit || bit == -1 ? Count - 1 : bit - 1;

            if (bit < trailingOnesCount)
                return bit;
            var last1 = Last1;
            if (last1 < bit)
                return last1;

            var bitOffset = bit - trailingOnesCount;
            var index = bitOffset >> LEN;

            for (var value = values[index] & Mask((bitOffset & MASK) + 1); ; value = values[--index])
                if (value == 0)
                {
                    if (index == 0)
                        return trailingOnesCount - 1;
                }
                else
                    return trailingOnesCount + (index << LEN) + BITS - 1 - BitOperations.LeadingZeroCount(value);
        }

        ///<summary>
        ///Finds the previous '0' bit in the <see cref="BitList"/> before the specified bit.
        ///</summary>
        ///<param name="bit">The bit to start searching from. Pass -1 or >= <see cref="Count"/>, to start searches from the end.</param>
        ///<returns>The index of previous '0' bit, or -1 if no '0' bit is found before the specified bit.</returns>
        public int Prev0(int bit)
        {
            if (Count == 0 || bit < -1)
                return -1;

            bit = Count <= bit || bit == -1 ? Count - 1 : bit - 1;

            if (bit < trailingOnesCount)
                return -1;

            if (Last1 < bit)
                return bit;

            var bitInValues = bit - trailingOnesCount;
            var index = bitInValues >> LEN;

            for (var value = ~values[index] & Mask((bitInValues & MASK) + 1); ; value = ~values[--index])
                if (value != 0)
                    return trailingOnesCount + (index << LEN) + BITS - 1 - BitOperations.LeadingZeroCount(value);
        }

        ///<summary>
        ///Returns the index of the highest-numbered ('leftmost' or most significant)
        ///bit that is set to '1'.
        ///</summary>
        ///<value>
        ///    The index of the highest set bit, or -1 if the <see cref="BitList"/>
        ///    contains no '1' bits (i.e., it's empty or all zeros).
        ///</value>
        public int Last1 => Used() == 0 ? trailingOnesCount - 1 : trailingOnesCount + (used - 1 << LEN) + BITS - 1 - BitOperations.LeadingZeroCount(values[used - 1]);

        ///<summary>
        ///Checks if this <see cref="BitList"/> contains only '0' bits (or is empty).
        ///</summary>
        ///<value>
        ///    <c>true</c> if the list has count 0, or if <see cref="trailingOnesCount"/>
        ///    is 0 and the <see cref="values"/> array contains no set bits;
        ///    <c>false</c> otherwise.
        ///</value>
        public bool IsAllZeros => trailingOnesCount == 0 && Used() == 0;

        ///<summary>
        ///Calculates the number of '1' bits from index 0 up to and including the
        ///specified bit index (also known as rank or population count).
        ///</summary>
        ///<param name="bit">The global bit index (inclusive) up to which to count set bits.
        ///If negative, returns 0. If greater than or equal to <see cref="Count"/>,
        ///counts up to <see cref="Count"/> - 1.</param>
        ///<returns>The total number of '1' bits in the range [0, bit].</returns>
        public int Rank(int bit)
        {
            if (bit < 0 || count == 0)
                return 0;
            if (count <= bit)
                bit = count - 1;
            if (bit < trailingOnesCount)
                return bit + 1;
            if (Used() == 0)
                return trailingOnesCount;

            var last1 = Last1;
            if (last1 < bit)
                bit = last1;

            var index = bit - trailingOnesCount >> LEN;
            var sum = trailingOnesCount + BitOperations.PopCount(values[index] & Mask(bit - trailingOnesCount - (index << LEN) + 1));
            for (var i = 0; i < index; i++)
                sum += BitOperations.PopCount(values[i]);

            return sum;
        }

        ///<summary>
        ///Returns the total number of bits set to '1' in this <see cref="BitList"/>.
        ///This is equivalent to calling <c>rank(Count - 1)</c>.
        ///</summary>
        ///<value>The total number of '1' bits (cardinality).</value>
        public int Cardinality => Rank(count - 1);

        ///<summary>
        ///Finds the global bit index of the Nth set bit ('1'). If the Nth '1' exists,
        ///<c>rank(result) == cardinality</c>.
        ///</summary>
        ///<param name="cardinality">The rank (1-based count) of the '1' bit to find. For example,
        ///<c>cardinality = 1</c> finds the first '1', <c>cardinality = 2</c>
        ///finds the second '1', etc.</param>
        ///<returns>The 0-based global index of the bit with the specified cardinality,
        ///or -1 if the cardinality is less than 1 or greater than the total
        ///number of '1's in the list (<see cref="Cardinality"/>).</returns>
        public int Bit(int cardinality)
        {
            if (cardinality <= 0 || Cardinality < cardinality)
                return -1;

            if (cardinality <= trailingOnesCount)
                return cardinality - 1;

            var remainingCardinality = cardinality - trailingOnesCount;
            var totalBits = Last1 + 1 - trailingOnesCount;

            for (var i = 0; i < Used() && remainingCardinality > 0; i++)
            {
                var value = values[i];
                var bits = Math.Min(BITS, totalBits - (i << LEN));
                var count = BitOperations.PopCount(value & Mask(bits));

                if (remainingCardinality <= count)
                    for (var j = 0; j < bits; j++)
                        if ((value & 1UL << j) != 0)
                            if (--remainingCardinality == 0)
                                return trailingOnesCount + (i << LEN) + j;
                remainingCardinality -= count;
            }

            return -1;
        }

        ///<summary>
        ///Generates a hash code for this BitList.
        ///The hash code is based on the count, trailing ones count, and the content of the values array.
        ///</summary>
        ///<returns>The hash code value for this BitList.</returns>
        public override int GetHashCode()
        {
            var hash = 197;
            for (var i = Used(); -1 < --i;)
                hash = HashCode.Combine(hash, values[i]);
            hash = HashCode.Combine(hash, trailingOnesCount);
            return HashCode.Combine(hash, Count);
        }

        ///<summary>
        ///Returns the total potential bit capacity of the underlying storage, including trailing ones and allocated values.
        ///This value represents the maximum bit index that could be addressed without resizing the values array,
        ///plus the bits represented by trailingOnesCount.
        ///</summary>
        ///<returns>The length of the BitList in bits, considering allocated storage.</returns>
        public int Capacity => trailingOnesCount + (values.Length << LEN);

        ///<summary>
        ///Creates and returns a deep copy of this R instance.
        ///The cloned object will have the same count, trailing ones count, and bit values as the original.
        ///</summary>
        ///<returns>A clone of this R instance.</returns>
        public object Clone()
        {
            var dst = (R)MemberwiseClone();
            dst.values = values.Length == 0 ? values : (ulong[])values.Clone();
            return dst;
        }

        ///<summary>
        ///Compares this BitList to another object for equality.
        ///</summary>
        ///<param name="obj">The object to compare with.</param>
        ///<returns>true if the objects are equal, false otherwise.
        ///Objects are considered equal if they are both BitList instances of the same class and have the same content.</returns>
        public override bool Equals(object? obj) => obj != null && GetType() == obj.GetType() && Equals((R)obj);

        ///<summary>
        ///Compares this BitList to another BitList for equality.
        ///</summary>
        ///<param name="other">The BitList to compare with.</param>
        ///<returns>true if the BitLists are equal, false otherwise.
        ///BitLists are considered equal if they have the same count, trailing ones count, and bit values.</returns>
        public bool Equals(R other)
        {
            if (other == null || Count != other.Count || trailingOnesCount != other.trailingOnesCount)
                return false;
            for (var i = Used(); -1 < --i;)
                if (values[i] != other.values[i])
                    return false;
            return true;
        }

        ///<summary>
        ///Returns a JSON string representation of the BitList.
        ///</summary>
        ///<returns>Bit sequence as a JSON array of 0s and 1s.</returns>
        public override string ToString() => ToJson(new StringBuilder()).ToString();

        ///<summary>
        ///Appends the BitList's bits as a JSON array to a StringBuilder.
        ///</summary>
        ///<param name="sb">StringBuilder to append to.</param>
        ///<returns>Updated StringBuilder.</returns>
        public StringBuilder ToJson(StringBuilder sb)
        {
            sb.EnsureCapacity(sb.Length + (Used() + (trailingOnesCount >> LEN) + 1) * 68);

            sb.Append('[');
            if (0 < count)
            {
                for (var i = 0; i < trailingOnesCount; i++)
                    sb.Append(1).Append(',');
                var last1 = Last1;
                for (var i = 0; i < Used(); i++)
                {
                    var v = values[i];
                    for (var s = 0; s < Math.Min(BITS, last1 + 1 - trailingOnesCount - (i << LEN)); s++)
                        sb.Append((v & 1UL << s) == 0 ? 0 : 1).Append(',');
                }

                for (var i = last1 + 1; i < count; i++)
                    sb.Append(0).Append(',');
                if (sb[sb.Length - 1] == ',')
                    sb.Length--;
            }

            sb.Append(']');
            return sb;
        }

        ///<summary>
        ///Counts the number of leading '0' bits (zeros at the most significant end,
        ///highest indices) in this <see cref="BitList"/>.
        ///Equivalent to <c>Count - 1 - Last1</c> for non-empty lists.
        ///</summary>
        ///<returns>The number of leading zero bits. Returns <see cref="Count"/> if the list
        ///is all zeros or empty. Returns 0 if the highest bit (at <c>Count-1</c>)
        ///is '1'.</returns>
        public int NumberOfLeading0 => count == 0 ? 0 : count - 1 - Last1;

        ///<summary>
        ///Counts the number of trailing '0' bits (zeros at the least significant end,
        ///lowest indices) in this <see cref="BitList"/>.
        ///This is equivalent to the index of the first '1' bit, or <see cref="Count"/> if
        ///the list contains only '0's.
        ///</summary>
        ///<value>
        ///    The number of trailing zero bits. Returns <see cref="Count"/> if the list
        ///    is all zeros or empty. Returns 0 if the first bit (index 0) is '1'.
        ///</value>
        public int NumberOfTrailing0
        {
            get
            {
                if (count == 0 || 0 < trailingOnesCount)
                    return 0;
                var i = Next1(-1);
                return i == -1 ? count : i;
            }
        }

        ///<summary>
        ///Counts the number of trailing '1' bits (ones at the least significant end,
        ///lowest indices) in this <see cref="BitList"/>.
        ///This directly corresponds to the <see cref="trailingOnesCount"/> optimization field.
        ///</summary>
        ///<returns>The number of implicitly stored trailing '1' bits. Returns 0 if the
        ///list starts with '0' or is empty.</returns>
        public int NumberOfTrailing1 => trailingOnesCount;

        ///<summary>
        ///Counts the number of leading '1' bits (ones at the most significant end,
        ///highest indices) in this <see cref="BitList"/>.
        ///</summary>
        ///<returns>The number of leading '1' bits. Returns 0 if the list ends in '0',
        ///is empty, or contains only '0's. Returns <see cref="Count"/> if the list
        ///contains only '1's.</returns>
        public int NumberOfLeading1()
        {
            if (count > 0)
            {
                var last1 = Last1;
                return last1 + 1 == count ? last1 - Prev0(last1) : 0;
            }

            return 0;
        }

        ///<summary>
        ///Extracts a 64-bit word (<see cref="ulong"/>) from this <see cref="R"/> starting at the specified bit index.
        ///Handles cases where the word spans the trailing ones, explicit values, or trailing zeros regions.
        ///</summary>
        ///<param name="bit">The starting global bit index (0-indexed).</param>
        ///<returns>A <see cref="ulong"/> containing the 64 bits starting at <c>bit</c>.
        ///Returns all ones if entirely within trailing ones, all zeros if entirely in trailing zeros,
        ///or a combination based on the values array otherwise.</returns>
        public ulong Get64(int bit)
        {
            if (bit + BITS <= trailingOnesCount)
                return ulong.MaxValue;
            if (Last1 < bit)
                return 0L;

            var ret = 0UL;
            var bits_fetched = 0;

            if (bit < trailingOnesCount)
            {
                bits_fetched = trailingOnesCount - bit;
                ret = Mask(bits_fetched);
            }

            var index = bit >> LEN;
            if (Used() <= index)
                return ret;

            bit += bits_fetched - trailingOnesCount;
            var pos = bit & MASK;

            var bits_needed = BITS - bits_fetched;
            var bits = values[index] >>> pos;

            if (pos != 0 && index + 1 < used)
                bits |= values[index + 1] << BITS - pos;

            if (bits_needed < BITS)
                bits &= Mask(bits_needed);

            ret |= bits << bits_fetched;

            return ret;
        }

        ///<summary>
        ///Finds the index of the first bit where this <see cref="R"/> differs from another.
        ///</summary>
        ///<param name="other">The other <see cref="R"/> to compare with.</param>
        ///<returns>The index of the first differing bit, or <c>Math.Min(other.Count, Count())</c> if
        ///they are identical up to the shorter length.</returns>
        public int FindFirstDifference(R other)
        {
            var checkLimit = Math.Min(other.Count, Count);
            var toc1 = other.trailingOnesCount;
            var toc2 = trailingOnesCount;
            var commonTOC = Math.Min(toc1, toc2);

            if (toc1 != toc2)
                return commonTOC;

            var bit = commonTOC;
            while (bit < checkLimit)
            {
                var word1 = other.Get64(bit);
                var word2 = Get64(bit);
                if (word1 != word2)
                {
                    var diffOffset = BitOperations.TrailingZeroCount(word1 ^ word2);
                    var diffBit = bit + diffOffset;
                    return Math.Min(diffBit, checkLimit);
                }

                bit = bit > checkLimit - BITS ? checkLimit : bit + BITS;
            }

            return checkLimit;
        }

        ///<summary>
        ///Implements the indexer for the read-only list.
        ///</summary>
        ///<param name="index">The index of the bit to retrieve.</param>
        ///<returns>The bit at the specified index.</returns>
        public bool this[int index] => Get(index);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<bool> IEnumerable<bool>.GetEnumerator() => GetEnumerator();

        ///<summary>
        ///Returns a struct-based enumerator for efficient iteration over bits.
        ///</summary>
        ///<returns>Enumerator for the BitList.</returns>
        public Enumerator GetEnumerator() => new(this);

        ///<summary>
        ///Struct-based enumerator for iterating over BitList bits efficiently.
        ///Avoids heap allocation for better performance.
        ///</summary>
        public struct Enumerator : IEnumerator<bool>
        {
            private readonly R _list;
            private int _index;

            internal Enumerator(R list)
            {
                _list = list;
                _index = -1;
            }

            public bool Current => _list.Get(_index);

            object? IEnumerator.Current => Current;

            public bool MoveNext() => ++_index < _list.Count;

            public void Reset() => _index = -1;
            public void Dispose() { }
        }
    }

    ///<summary>
    ///A concrete, mutable implementation of <see cref="BitList"/> extending the read-only
    ///base <see cref="R"/>.
    ///<para>
    ///This class provides methods to set, clear, flip, add, insert, and remove bits,
    ///as well as perform bulk operations and manage the list's count and capacity.
    ///It inherits the optimized storage mechanism from <see cref="R"/> (using
    ///<see cref="R.trailingOnesCount"/> and a <see cref="R.values"/> array) and updates this
    ///structure efficiently during modifications.
    ///</para>
    ///</summary>
    public class RW : R, IList<bool>
    {
        ///<summary>
        ///Constructs an empty <see cref="RW"/> BitList with an initial capacity hint.
        ///The underlying storage (<see cref="R.values"/> array) will be allocated to hold at least
        ///<paramref name="bits"/>, potentially reducing reallocations if the approximate
        ///final count is known. The logical count remains 0.
        ///</summary>
        ///<param name="bits">The initial capacity hint in bits. If non-positive,
        ///a default initial capacity might be used or allocation deferred.</param>
        public RW(int bits)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(bits);
            if (bits > 0)
                values = new ulong[Len4Bits(bits)];
        }

        ///<summary>
        ///Constructs a new <see cref="RW"/> BitList of a specified initial count, with all
        ///bits initialized to a specified default value.
        ///</summary>
        ///<param name="default_value">The boolean value to initialize all bits to
        ///(<c>true</c> for '1', <c>false</c> for '0').</param>
        ///<param name="count">The initial number of bits in the list. Must be non-negative.</param>
        ///<exception cref="ArgumentException">Thrown if count is negative.</exception>
        public RW(bool defaultValue, int count)
        {
            if (0 < count)
                if (defaultValue)
                    trailingOnesCount = this.count = count;
                else
                    this.count = count;
        }

        ///<summary>
        ///Performs a bitwise AND operation between this <see cref="RW"/> and another
        ///read-only <see cref="R"/>.
        ///The result is stored in this <see cref="RW"/>, modifying it in place.
        ///</summary>
        ///<param name="and">The other <see cref="R"/> to perform AND with.</param>
        ///<returns>This <see cref="RW"/> instance after the AND operation.</returns>
        public RW And(R and)
        {
            if (and == null || and.Count == 0)
            {
                var s = count;
                Clear();
                count = s;
                return this;
            }

            if (count <= and.trailingOnesCount)
                return this;

            if (and.Used() == 0)
            {
                Set0(and.trailingOnesCount, Count);
                return this;
            }

            var minToc = Math.Min(trailingOnesCount, and.trailingOnesCount);
            var minSize = Math.Min(count, and.Count);

            if (minSize <= minToc)
            {
                if (Used() > 0)
                    Array.Clear(values, 0, Used());
                trailingOnesCount = minSize;
                count = minSize;
                used = 0;
                return this;
            }

            if (minSize < count)
                count = minSize;

            var bit = minToc;
            var i = 0;

            if (and.trailingOnesCount < trailingOnesCount)
            {
                Set0(and.trailingOnesCount);
            }
            else if (trailingOnesCount < and.trailingOnesCount && (trailingOnesCount & MASK) != 0)
            {
                if (Used() == 0)
                    return this;

                var dif = and.trailingOnesCount - trailingOnesCount;
                var index = dif >> LEN;
                var pos = dif & MASK;

                var mask = Mask(pos);
                var v = values[index];
                values[index] = v & mask | v & and.values[0] << pos;

                i = index + 1;
                bit = trailingOnesCount + (i << LEN);
            }

            for (; bit < minSize && i < Used(); i++, bit += BITS)
                values[i] &= and.Get64(bit);

            used |= IO;
            return this;
        }

        ///<summary>
        ///Performs a bitwise OR operation between this <see cref="RW"/> and another
        ///read-only <see cref="R"/>.
        ///The result is stored in this <see cref="RW"/>, modifying it in place.
        ///</summary>
        ///<param name="or">The other <see cref="R"/> to perform OR with.</param>
        ///<returns>This <see cref="RW"/> instance after the OR operation.</returns>
        public RW Or(R or)
        {
            if (or == null || or.Count == 0 || or.IsAllZeros)
                return this;

            if (IsAllZeros)
            {
                count = or.count;
                trailingOnesCount = or.trailingOnesCount;
                if (or.Used() > 0)
                {
                    if (values == null || values.Length < or.Used())
                        values = (ulong[])or.values.Clone();
                    else
                        Array.Copy(or.values, 0, values, 0, or.Used());
                }

                used = or.Used();
                return this;
            }

            var maxSize = Math.Max(count, or.Count);
            var maxToc = Math.Max(trailingOnesCount, or.trailingOnesCount);

            if (Used() == 0 && or.Used() == 0)
            {
                trailingOnesCount = maxToc;
                count = maxSize;
                return this;
            }

            var last1 = Last1;
            for (int i = Next0(maxToc - 1), ii = or.Next0(maxToc - 1); ;)
            {
                while (i < ii)
                {
                    if (i == -1)
                    {
                        maxToc = last1 + 1;
                        goto BreakLoop;
                    }

                    i = Next0(i);
                }

                while (ii < i)
                {
                    if (ii == -1)
                    {
                        maxToc = or.Last1 + 1;
                        goto BreakLoop;
                    }

                    ii = or.Next0(ii);
                }

                if (i == ii)
                {
                    maxToc = i == -1 ? Math.Max(last1, or.Last1) + 1 : i;
                    break;
                }
            }

        BreakLoop:

            if (maxSize <= maxToc)
            {
                if (Used() > 0)
                    Array.Clear(values, 0, Used());
                trailingOnesCount = maxSize;
                count = maxSize;
                used = 0;
                return this;
            }

            var bit = trailingOnesCount;

            if (trailingOnesCount < maxToc)
            {
                var max = last1 + 1 - trailingOnesCount;
                values = ShiftRight(values, values, 0, max, maxToc - trailingOnesCount, true);
                bit = trailingOnesCount = maxToc;
                used |= IO;
            }

            for (var i = 0; bit < maxSize && i < Used(); i++, bit += BITS)
                values[i] |= or.Get64(bit);

            count = maxSize;
            return this;
        }

        ///<summary>
        ///Performs a bitwise XOR operation between this <see cref="RW"/> and another
        ///read-only <see cref="R"/>.
        ///The result is stored in this <see cref="RW"/>, modifying it in place.
        ///</summary>
        ///<param name="xor">The other <see cref="R"/> to perform XOR with.</param>
        ///<returns>This <see cref="RW"/> instance after the XOR operation.</returns>
        public RW Xor(R xor)
        {
            if (xor == null || xor.Count == 0)
                return this;

            if (IsAllZeros)
            {
                count = xor.count;
                trailingOnesCount = xor.trailingOnesCount;
                if (xor.Used() > 0)
                {
                    if (values.Length < xor.Used())
                        values = (ulong[])xor.values.Clone();
                    else
                        Array.Copy(xor.values, 0, values, 0, xor.Used());
                }

                used = xor.Used();
                return this;
            }

            if (xor.IsAllZeros)
            {
                if (count < xor.count)
                    count = xor.count;
                return this;
            }

            var maxSize = Math.Max(count, xor.Count);
            var first1 = FindFirstDifference(xor);

            if (maxSize <= first1)
            {
                Clear();
                count = maxSize;
                return this;
            }

            var minSize = Math.Min(Count, xor.Count);
            var newToc = 0;

            if (first1 == 0)
            {
                for (var x = ~0UL; x == ~0UL;)
                {
                    x = xor.Get64(newToc) ^ Get64(newToc);
                    if (x == ~0UL)
                        newToc += 64;
                    else
                        newToc += BitOperations.TrailingZeroCount(~x);

                    if (maxSize <= newToc)
                    {
                        trailingOnesCount = newToc;
                        used = 0;
                        count = maxSize;
                        return this;
                    }
                }
            }

            var last1 = Last1;
            if (last1 < newToc)
            {
                if (Used() > 0)
                    Array.Clear(values, 0, Used());
                trailingOnesCount = newToc;
                count = maxSize;
                used = 0;
                return this;
            }

            var maxLast1 = Math.Max(last1, xor.Last1);
            var newValuesLen = Len4Bits(newToc <= maxLast1 ? maxLast1 - newToc + 1 : 0);
            var i = 0;

            if (newToc < trailingOnesCount)
            {
                var shiftBits = trailingOnesCount - newToc;
                var sb = Len4Bits(shiftBits);
                values = 0 < Used() ? ShiftLeft(values, 0, last1 - trailingOnesCount + 1, shiftBits, false) : values.Length < sb ? new ulong[sb]
                                                                                                                                 : values;

                var index = shiftBits >> LEN;
                for (var b = newToc; i < index; i++, b += BITS)
                    values[i] = ~xor.Get64(b);

                var pos = shiftBits & MASK;
                if (pos != 0)
                {
                    var maskLower = Mask(pos);
                    var val = values[i];
                    var xorVal = xor.Get64(newToc + (i << LEN));

                    values[i] = (val ^ xorVal) & ~maskLower |
                                ~xorVal & maskLower;
                }

                trailingOnesCount = newToc;
                count = maxSize;

                used += sb;
                used |= IO;
                return this;
            }

            if (trailingOnesCount < newToc)
            {
                var shiftBits = newToc - trailingOnesCount;
                ShiftRight(values, values, 0, last1 - trailingOnesCount + 1, shiftBits, true);
            }
            else if (values.Length < newValuesLen)
            {
                var newValues = new ulong[newValuesLen];
                Array.Copy(values, 0, newValues, 0, Math.Min(values.Length, newValuesLen));
                values = newValues;
            }

            var bit = newToc;
            trailingOnesCount = newToc;

            for (var max = Len4Bits(maxSize); i < max; i++, bit += BITS)
                values[i] ^= xor.Get64(bit);

            count = maxSize;
            used |= IO;
            return this;
        }

        ///<summary>
        ///Performs a bitwise AND NOT operation: <c>thisBitList AND NOT otherBitList</c>.
        ///Clears bits in this <see cref="RW"/> where the corresponding bit in the
        ///<c>not</c> <see cref="R"/> is set.
        ///</summary>
        ///<param name="not">The <see cref="R"/> to perform NOT and AND with.</param>
        ///<returns>This <see cref="RW"/> instance after the AND NOT operation.</returns>
        public RW AndNot(R not)
        {
            if (not == null || not.IsAllZeros || count == 0)
                return this;

            if (IsAllZeros)
                return this;

            var first1InNot = not.Next1(-1);
            if (first1InNot == -1)
                first1InNot = not.Count;
            var resToc = Math.Min(trailingOnesCount, first1InNot);
            var resSize = count;

            if (resSize <= resToc)
            {
                if (Used() > 0)
                    Array.Clear(values, 0, Used());
                trailingOnesCount = resSize;
                count = resSize;
                used = 0;
                return this;
            }

            var bitsInResultValues = resSize - resToc;
            if (bitsInResultValues <= 0)
            {
                if (Used() > 0)
                    Array.Clear(values, 0, Used());
                trailingOnesCount = resToc;
                count = resSize;
                used = 0;
                return this;
            }

            var resultValuesLen = Len4Bits(bitsInResultValues);

            ulong[] resultValues;
            bool inPlace;
            var originalUsedCached = -1;

            if (trailingOnesCount == resToc && values.Length >= resultValuesLen)
            {
                originalUsedCached = Used();
                resultValues = values;
                inPlace = true;
            }
            else
            {
                resultValues = new ulong[resultValuesLen];
                inPlace = false;
            }

            for (var i = 0; i < resultValuesLen; i++)
            {
                var currentAbsBitStart = resToc + (i << LEN);
                var thisWord = Get64(currentAbsBitStart);
                if (thisWord == 0UL)
                {
                    resultValues[i] = 0UL;
                    continue;
                }

                var notWord = not.Get64(currentAbsBitStart);
                resultValues[i] = thisWord & ~notWord;
            }

            if (inPlace && resultValuesLen < originalUsedCached)
                Array.Clear(values, resultValuesLen, originalUsedCached - resultValuesLen);

            trailingOnesCount = resToc;
            count = resSize;
            values = resultValues;
            used = resultValuesLen | IO;

            return this;
        }

        ///<summary>
        ///Checks if this <see cref="RW"/> intersects with another <see cref="R"/> (i.e.,
        ///if there is at least one bit position where both are '1').
        ///</summary>
        ///<param name="other">The other <see cref="R"/> to check for intersection.</param>
        ///<returns><c>true</c> if there is an intersection, <c>false</c> otherwise.</returns>
        public bool Intersects(R other)
        {
            if (other == null || count == 0 || other.Count == 0)
                return false;

            var checkLimit = Math.Min(count, other.Count);
            var commonTOC = Math.Min(trailingOnesCount, other.trailingOnesCount);
            if (commonTOC > 0)
                return true;

            var range1End = Math.Min(checkLimit, trailingOnesCount);
            if (commonTOC < range1End)
            {
                var next1InOther = other.Next1(commonTOC - 1);
                if (next1InOther != -1 && next1InOther < range1End)
                    return true;
            }

            var range2End = Math.Min(checkLimit, other.trailingOnesCount);
            if (commonTOC < range2End)
            {
                var next1InThis = Next1(commonTOC - 1);
                if (next1InThis != -1 && next1InThis < range2End)
                    return true;
            }

            var valuesCheckStart = Math.Max(trailingOnesCount, other.trailingOnesCount);
            var thisRelBitStart = Math.Max(0, valuesCheckStart - trailingOnesCount);
            var otherRelBitStart = Math.Max(0, valuesCheckStart - other.trailingOnesCount);
            var thisWordStartIndex = thisRelBitStart >> LEN;
            var otherWordStartIndex = otherRelBitStart >> LEN;
            var endBitInclusive = checkLimit - 1;
            var thisRelBitEnd = endBitInclusive - trailingOnesCount;
            var otherRelBitEnd = endBitInclusive - other.trailingOnesCount;
            var thisUsed = Used();
            var otherUsed = other.Used();
            var thisWordEndIndex = thisRelBitEnd < 0 ? -1 : Math.Min(thisUsed - 1, thisRelBitEnd >> LEN);
            var otherWordEndIndex = otherRelBitEnd < 0 ? -1 : Math.Min(otherUsed - 1, otherRelBitEnd >> LEN);
            var loopStartIndex = Math.Max(thisWordStartIndex, otherWordStartIndex);
            var loopEndIndex = Math.Max(thisWordEndIndex, otherWordEndIndex);

            for (var wordIndex = loopStartIndex; wordIndex <= loopEndIndex; wordIndex++)
            {
                var thisWord = wordIndex >= thisWordStartIndex && wordIndex <= thisWordEndIndex ? values[wordIndex] : 0UL;

                var otherWord = wordIndex >= otherWordStartIndex && wordIndex <= otherWordEndIndex ? other.values[wordIndex] : 0UL;

                if (thisWord == 0UL && otherWord == 0UL)
                    continue;

                var commonMask = ~0UL;

                var wordAbsStartBit = trailingOnesCount + (wordIndex << LEN);
                if (wordAbsStartBit < valuesCheckStart)
                {
                    if (trailingOnesCount <= other.trailingOnesCount)
                    {
                        if (wordIndex == thisWordStartIndex)
                            commonMask &= ~0UL << (thisRelBitStart & MASK);
                    }
                    else if (wordIndex == otherWordStartIndex)
                        commonMask &= ~0UL << (otherRelBitStart & MASK);
                }

                var wordAbsEndBit = wordAbsStartBit + BITS;
                if (wordAbsEndBit > checkLimit)
                {
                    if (trailingOnesCount >= other.trailingOnesCount)
                    {
                        if (wordIndex == thisWordEndIndex)
                            commonMask &= Mask((thisRelBitEnd & MASK) + 1);
                    }
                    else if (wordIndex == otherWordEndIndex)
                        commonMask &= Mask((otherRelBitEnd & MASK) + 1);
                }

                if ((thisWord & otherWord & commonMask) != 0)
                    return true;
            }

            return false;
        }

        ///<summary>
        ///Flips the bit at the specified position. If the bit is '0', it becomes '1',
        ///and vice versa.
        ///</summary>
        ///<param name="bit">The bit position to flip (0-indexed).</param>
        ///<returns>This <see cref="RW"/> instance after flipping the bit.</returns>
        public RW Flip(int bit)
        {
            if (bit < 0)
                return this;
            return Get(bit) ? Set0(bit) : Set1(bit);
        }

        ///<summary>
        ///Flips a range of bits from <c>fromBit</c> (inclusive) to <c>toBit</c>
        ///(exclusive).
        ///For each bit in the range, if it's '0', it becomes '1', and if it's '1', it
        ///becomes '0'.
        ///</summary>
        ///<param name="fromBit">The starting bit position of the range to flip (inclusive, 0-indexed).</param>
        ///<param name="toBit">The ending bit position of the range to flip (exclusive, 0-indexed).</param>
        ///<returns>This <see cref="RW"/> instance after flipping the bits in the specified range.</returns>
        public RW Flip(int fromBit, int toBit)
        {
            if (fromBit < 0)
                fromBit = 0;
            if (toBit <= fromBit)
                return this;

            var last1 = Last1;

            if (fromBit == trailingOnesCount)
            {
                if (Used() == 0)
                {
                    trailingOnesCount = toBit;
                    count = Math.Max(count, toBit);
                    return this;
                }

                Fill(3, values, 0, Math.Min(last1 + 1, toBit) - trailingOnesCount);
                var shiftBits = 0;
                var i = 0;
                for (; i < used;)
                {
                    var v = values[i];
                    if (v == ~0UL)
                        shiftBits += 64;
                    else
                    {
                        shiftBits += BitOperations.TrailingZeroCount(~v);
                        break;
                    }
                }

                if (shiftBits == last1 + 1 - trailingOnesCount)
                {
                    trailingOnesCount += shiftBits + ((count = Math.Max(count, toBit)) - (last1 + 1));
                    Array.Clear(values, 0, used);
                    used = 0;
                    return this;
                }

                var len = Len4Bits(last1 - trailingOnesCount - shiftBits + ((count = Math.Max(count, toBit)) - last1));
                values = ShiftRight(values, values.Length < len ? new ulong[len] : values, 0, last1 - trailingOnesCount + 1, shiftBits, true);
                trailingOnesCount += shiftBits;
                used = len | IO;
                return this;
            }

            if (fromBit < trailingOnesCount)
            {
                var shiftBits = trailingOnesCount - fromBit;
                var zeros = Math.Min(trailingOnesCount, toBit) - fromBit;

                if (Used() > 0)
                {
                    var len = Len4Bits(Math.Max(last1 - trailingOnesCount + shiftBits, toBit - trailingOnesCount));
                    values = ShiftLeft(values, 0, last1 - trailingOnesCount + 1 + shiftBits, shiftBits, false);
                    used = len;
                    Fill(0, values, 0, zeros);
                }
                else if (values.Length < (used = Len4Bits(Math.Max(shiftBits, toBit - trailingOnesCount))))
                    values = new ulong[used * 3 / 2];

                Fill(1, values, zeros, zeros + trailingOnesCount - fromBit);
                trailingOnesCount = fromBit;
                count = Math.Max(count, toBit);
                used |= IO;
                return this;
            }

            if (last1 < toBit)
            {
                var u = Math.Max(Used(), Len4Bits(toBit - trailingOnesCount));

                if (values.Length < u)
                    if (used == 0)
                        values = new ulong[u];
                    else
                        Array.Resize(ref values, u);

                used = u;

                if (last1 < fromBit)
                {
                    Fill(1, values, fromBit - trailingOnesCount, toBit - trailingOnesCount);
                    count = Math.Max(count, toBit);
                    return this;
                }

                Fill(1, values, last1 + 1 - trailingOnesCount, toBit + 1 - trailingOnesCount);
                toBit = last1;
            }

            count = Math.Max(count, toBit);

            var pos = fromBit - trailingOnesCount & MASK;
            var index = fromBit - trailingOnesCount >> LEN;
            if (pos > 0)
            {
                var mask = Mask(pos);
                var v = values[index];
                values[index] = v & mask | ~v & ~mask;
                index++;
            }

            var index2 = toBit - trailingOnesCount >> LEN;
            while (index < index2)
                values[index++] = ~values[index];

            var pos2 = toBit - trailingOnesCount & MASK;
            if (pos2 > 0)
            {
                var mask = Mask(pos2);
                var v = values[index2];
                values[index2] = ~(v & mask) | v & ~mask;
            }

            return this;
        }

        public RW Set<T>(int index, params T[] values)
            where T : struct
        {
            if (index < 0)
                return this;
            var end = index + values.Length;
            if (count < end)
                count = end;
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < values.Length; i++)
                if (comparer.Equals(values[i], default))
                    Set0(index + i);
                else
                    Set1(index + i);
            return this;
        }

        ///<summary>
        ///Sets a sequence of bits starting at a specified index, using values from a
        ///boolean array.
        ///The <see cref="BitList"/> count will be increased if necessary to accommodate the
        ///sequence.
        ///</summary>
        ///<param name="index">The starting global bit index (0-indexed, inclusive) to begin setting.
        ///Must be non-negative.</param>
        ///<param name="values">An array of boolean values. <c>values[i]</c> determines the
        ///state of the bit at <c>index + i</c> (<c>true</c> for '1', <c>false</c> for '0').</param>
        ///<returns>This <see cref="RW"/> instance for method chaining.</returns>
        public RW Set(int index, params bool[] values)
        {
            if (index < 0)
                return this;
            var end = index + values.Length;
            if (count < end)
                count = end;

            for (var i = 0; i < values.Length; i++)
                if (values[i])
                    Set1(index + i);
                else
                    Set0(index + i);
            return this;
        }

        ///<summary>
        ///Sets the bit at the specified index to the given boolean value.
        ///The <see cref="BitList"/> count will be increased if the index is outside the current range.
        ///</summary>
        ///<param name="bit">The global bit index (0-indexed) to set. Must be non-negative.</param>
        ///<param name="value">The boolean value to set the bit to (<c>true</c> for '1', <c>false</c> for '0').</param>
        ///<returns>This <see cref="RW"/> instance for method chaining.</returns>
        public RW Set(int bit, bool value)
        {
            if (bit < 0)
                return this;
            if (count <= bit)
                count = bit + 1;
            return value ? Set1(bit) : Set0(bit);
        }

        ///<summary>
        ///Sets the bit at the specified index based on an integer value.
        ///The bit is set to '1' if the value is non-zero, and '0' if the value is zero.
        ///The <see cref="BitList"/> count will be increased if the index is outside the current range.
        ///</summary>
        ///<param name="bit">The global bit index (0-indexed) to set. Must be non-negative.</param>
        ///<param name="value">The integer value. If <c>value != 0</c>, sets the bit to '1', otherwise sets it to '0'.</param>
        ///<returns>This <see cref="RW"/> instance for method chaining.</returns>
        public RW Set(int bit, int value) => Set(bit, value != 0);

        ///<summary>
        ///Sets the bit at the specified index based on comparing an integer value to a
        ///reference 'TRUE' value.
        ///The bit is set to '1' if <c>value == TRUE</c>, and '0' otherwise.
        ///The <see cref="BitList"/> count will be increased if the index is outside the current range.
        ///</summary>
        ///<param name="bit">The global bit index (0-indexed) to set. Must be non-negative.</param>
        ///<param name="value">The integer value to compare.</param>
        ///<param name="true">The integer value representing the 'true' state.</param>
        ///<returns>This <see cref="RW"/> instance for method chaining.</returns>
        public RW Set(int bit, int value, int @true) => Set(bit, value == @true);

        ///<summary>
        ///Sets all bits within a specified range to a given boolean value.
        ///The range is defined from <paramref name="from_bit"/> (inclusive) to <paramref name="to_bit"/> (exclusive).
        ///The <see cref="BitList"/> count will be increased if <paramref name="to_bit"/> is beyond the current count.
        ///</summary>
        ///<param name="from_bit">The starting global bit index of the range (inclusive, 0-indexed). Must be non-negative.</param>
        ///<param name="to_bit">The ending global bit index of the range (exclusive, 0-indexed). Must not be less than <paramref name="from_bit"/>.</param>
        ///<param name="value">The boolean value to set all bits in the range to (<c>true</c> for '1', <c>false</c> for '0').</param>
        ///<returns>This <see cref="RW"/> instance for method chaining.</returns>
        public RW Set(int fromBit, int toBit, bool value)
        {
            if (fromBit < 0 || toBit <= fromBit)
                return this;
            if (count < toBit)
                count = toBit;
            return value ? Set1(fromBit, toBit) : Set0(fromBit, toBit);
        }

        ///<summary>
        ///Sets the bit at the specified index to '1'.
        ///Handles adjustments to <see cref="R.trailingOnesCount"/> and the <see cref="R.values"/> array,
        ///including potential merging of adjacent '1' sequences and shifting bits if
        ///a '0' within the <see cref="R.values"/> array (conceptually, the first '0' after
        ///trailing ones) is changed to '1'. Expands storage if necessary.
        ///Increases list count if <paramref name="bit"/> >= <see cref="R.count"/>.
        ///</summary>
        ///<param name="bit">The global bit index (0-indexed) to set to '1'. Must be non-negative.</param>
        ///<returns>This <see cref="RW"/> instance for method chaining.</returns>
        public RW Set1(int bit)
        {
            if (bit < 0)
                return this;
            if (bit < trailingOnesCount)
                return this;
            if (count <= bit)
                count = bit + 1;

            if (bit == trailingOnesCount)
            {
                if (Used() == 0)
                {
                    trailingOnesCount++;
                    return this;
                }

                var next1 = Next1(bit);
                var last1 = Last1;
                var valuesLast1 = last1 - trailingOnesCount;

                if (bit + 1 == next1)
                {
                    var next0After = Next0(next1 - 1);
                    var spanOf1End = next0After == -1 ? last1 + 1 : next0After;

                    if (last1 < spanOf1End)
                    {
                        Array.Clear(values, 0, Used());
                        used = 0;
                    }
                    else
                    {
                        values = ShiftRight(values, values, next1 - trailingOnesCount, valuesLast1 + 1, spanOf1End - trailingOnesCount, true);
                        used |= IO;
                    }

                    trailingOnesCount = spanOf1End;
                    return this;
                }

                values = ShiftRight(values, values, 0, valuesLast1 + 1, 1, true);
                trailingOnesCount++;
                used |= IO;
                return this;
            }

            var bitOffset = bit - trailingOnesCount;
            var index = bitOffset >> LEN;

            if (values.Length < index + 1)
                Array.Resize(ref values, Math.Max(values.Length * 3 / 2, index + 1));
            if (used <= index)
                used = index + 1;

            values[index] |= 1UL << (bitOffset & MASK);
            return this;
        }

        ///<summary>
        ///Sets all bits within a specified range to '1'.
        ///The range is [<paramref name="from_bit"/>, <paramref name="to_bit"/>). Handles adjustments to
        ///<see cref="R.trailingOnesCount"/> and the <see cref="R.values"/> array, potentially merging
        ///runs of '1's and shifting bits. Expands storage if needed.
        ///Increases list count if <paramref name="to_bit"/> > <see cref="R.count"/>.
        ///</summary>
        ///<param name="from_bit">The starting global bit index (inclusive, 0-indexed). Must be non-negative.</param>
        ///<param name="to_bit">The ending global bit index (exclusive, 0-indexed). Must not be less than <paramref name="from_bit"/>.</param>
        ///<returns>This <see cref="RW"/> instance for method chaining.</returns>
        public RW Set1(int fromBit, int toBit)
        {
            if (fromBit < 0 || toBit <= fromBit)
                return this;
            if (count < toBit)
                count = toBit;
            if (toBit <= trailingOnesCount)
                return this;

            var last1 = Last1;

            if (fromBit <= trailingOnesCount)
            {
                var nextZero = Next0(toBit - 1);
                toBit = nextZero == -1 ? count : nextZero;

                if (last1 < toBit)
                {
                    Array.Clear(values, 0, Used());
                    used = 0;
                    trailingOnesCount = toBit;
                    return this;
                }

                if (Used() > 0)
                    values = ShiftRight(values, values, 0, last1 - trailingOnesCount + 1, toBit - trailingOnesCount, true);

                trailingOnesCount = toBit;
                used |= IO;
                return this;
            }

            var max = toBit - trailingOnesCount >> LEN;
            if (values.Length < max + 1)
                Array.Resize(ref values, Math.Max(values.Length * 3 / 2, max + 1));
            if (used < max + 1)
                used = max + 1;

            Fill(1, values, fromBit - trailingOnesCount, toBit - trailingOnesCount);
            return this;
        }

        ///<summary>
        ///Sets the bit at the specified index to '0'.
        ///Handles adjustments to <see cref="R.trailingOnesCount"/> and the <see cref="R.values"/> array.
        ///If a bit within the <see cref="R.trailingOnesCount"/> region is cleared, it splits
        ///the implicit '1's, potentially creating new explicit entries in the <see cref="R.values"/>
        ///array and shifting existing ones. Expands storage if necessary.
        ///Increases list count if <paramref name="bit"/> >= <see cref="R.count"/>.
        ///</summary>
        ///<param name="bit">The global bit index (0-indexed) to set to '0'. Must be non-negative.</param>
        ///<returns>This <see cref="RW"/> instance for method chaining.</returns>
        public RW Set0(int bit)
        {
            if (bit < 0)
                return this;

            if (count <= bit)
            {
                count = bit + 1;
                return this;
            }

            var last1 = Last1;
            if (last1 < bit)
            {
                count = Math.Max(count, bit + 1);
                return this;
            }

            if (bit < trailingOnesCount)
            {
                if (bit + 1 == trailingOnesCount && Used() == 0)
                {
                    trailingOnesCount--;
                    return this;
                }

                var bitsInValues = last1 - trailingOnesCount + 1;
                var shift = trailingOnesCount - bit;

                used = Len4Bits(shift + bitsInValues);

                if (bitsInValues > 0)
                    values = ShiftLeft(values, 0, bitsInValues, shift, trailingOnesCount == 1);
                else if (values.Length < used)
                    values = new ulong[used];

                if (shift > 1)
                    Fill(1, values, 1, shift);

                trailingOnesCount = bit;
                used |= IO;
                return this;
            }

            var bitInValues = bit - trailingOnesCount;
            values[bitInValues >> LEN] &= ~(1UL << (bitInValues & MASK));
            if (bit == last1)
                used |= IO;

            return this;
        }

        ///<summary>
        ///Sets all bits within a specified range to '0'.
        ///The range is [<paramref name="from_bit"/>, <paramref name="to_bit"/>). Handles adjustments to
        ///<see cref="R.trailingOnesCount"/> and the <see cref="R.values"/> array, potentially splitting
        ///implicit '1's runs and shifting bits. Expands storage if needed.
        ///Increases list count if <paramref name="to_bit"/> > <see cref="R.count"/>.
        ///</summary>
        ///<param name="from_bit">The starting global bit index (inclusive, 0-indexed). Must be non-negative.</param>
        ///<param name="to_bit">The ending global bit index (exclusive, 0-indexed). Must not be less than <paramref name="from_bit"/>.</param>
        ///<returns>This <see cref="RW"/> instance for method chaining.</returns>
        public RW Set0(int fromBit, int toBit)
        {
            if (fromBit < 0 || toBit <= fromBit)
                return this;
            if (count < toBit)
                count = toBit;

            var last1 = Last1;
            if (last1 < fromBit)
            {
                count = Math.Max(count, toBit);
                return this;
            }

            toBit = Math.Min(toBit, count);
            if (toBit <= fromBit)
                return this;

            var last1InValue = last1 - trailingOnesCount;
            var bitsInValues = last1InValue < 0 ? 0 : last1InValue + 1;

            if (toBit <= trailingOnesCount)
            {
                var shift = trailingOnesCount - toBit;
                trailingOnesCount = fromBit;
                used = Len4Bits(shift + bitsInValues);

                if (bitsInValues > 0)
                    values = ShiftLeft(values, 0, bitsInValues, toBit - fromBit + shift, true);
                else if (values.Length < used)
                    values = new ulong[Math.Max(values.Length + (values.Length >> 1), used)];

                if (shift > 0)
                    Fill(1, values, toBit - fromBit, toBit - fromBit + shift);
            }
            else if (fromBit < trailingOnesCount)
            {
                var shift = trailingOnesCount - fromBit;
                trailingOnesCount = fromBit;
                used = Len4Bits(Math.Max(shift + bitsInValues, toBit - trailingOnesCount));

                if (bitsInValues > 0)
                    values = ShiftLeft(values, 0, bitsInValues, shift, true);
                else if (values.Length < used)
                    values = new ulong[Math.Max(values.Length + (values.Length >> 1), used)];

                Fill(0, values, 0, toBit - trailingOnesCount);
            }
            else
                Fill(0, values, fromBit - trailingOnesCount, toBit - trailingOnesCount);

            used |= IO;
            return this;
        }

        ///<summary>
        ///Inserts a '0' bit at the specified index, shifting all existing bits at
        ///and after that index one position to the right (towards higher indices).
        ///Increases the count by one. Handles adjustments to <see cref="R.trailingOnesCount"/>
        ///and the <see cref="R.values"/> array.
        ///</summary>
        ///<param name="bit">The global bit index (0-indexed) at which to insert the '0'.
        ///Must be non-negative. If <c>bit >= count</c>, acts like appending a '0'.</param>
        ///<returns>This <see cref="RW"/> instance for method chaining.</returns>
        public RW Add0(int bit)
        {
            if (bit < 0)
                return this;

            if (bit < count)
                count++;
            else
                count = bit + 1;

            var last1 = Last1;
            if (last1 < bit)
                return this;
            var last1InValues = last1 - trailingOnesCount;

            if (bit < trailingOnesCount)
            {
                var shiftBits = trailingOnesCount - bit;
                used = Len4Bits(last1 - trailingOnesCount + 1 + shiftBits);
                trailingOnesCount = bit;

                if (last1InValues > 0)
                    values = ShiftLeft(values, 0, last1InValues + 1, shiftBits, true);
                else if (values.Length < used)
                    values = new ulong[used];

                Fill(1, values, 1, 1 + shiftBits);
            }
            else
            {
                used = Len4Bits(last1InValues + 1 + 1) | IO;
                values = ShiftLeft(values, bit - trailingOnesCount, last1InValues + 1, 1, true);
            }

            return this;
        }

        ///<summary>
        ///Inserts a '1' bit at the specified index, shifting all existing bits at
        ///and after that index one position to the right (towards higher indices).
        ///Increases the count by one. Handles adjustments to <see cref="R.trailingOnesCount"/>
        ///and the <see cref="R.values"/> array, potentially merging with adjacent '1's.
        ///</summary>
        ///<param name="bit">The global bit index (0-indexed) at which to insert the '1'.
        ///Must be non-negative. If <c>bit >= count</c>, acts like appending a '1'.</param>
        ///<returns>This <see cref="RW"/> instance for method chaining.</returns>
        public RW Add1(int bit)
        {
            if (bit < 0)
                return this;

            if (bit <= trailingOnesCount)
            {
                trailingOnesCount++;
                count++;
                return this;
            }

            var bitInValues = bit - trailingOnesCount;
            var index = bitInValues >> LEN;
            var last1 = Last1;
            var valuesLast1 = last1 - trailingOnesCount;

            if (Used() == 0)
            {
                if (values.Length < index + 1)
                    values = new ulong[index + 1];
            }
            else if (bit <= last1)
            {
                values = ShiftLeft(values, bit - trailingOnesCount, valuesLast1 + 1, 1, false);
            }

            used = Len4Bits(Math.Max(bitInValues + 1, valuesLast1 + 1 + 1));
            values[index] |= 1UL << (bitInValues & MASK);

            if (count < bit)
                count = bit + 1;
            else
                count++;

            return this;
        }

        ///<summary>
        ///Removes any trailing zero bits from the end of this <see cref="BitList"/> by
        ///adjusting the count down to the index of the last '1' bit plus one.
        ///If the list is all zeros or empty, the count becomes 0.
        ///</summary>
        ///<returns>This <see cref="RW"/> instance after trimming.</returns>
        public RW Trim()
        {
            Capacity_(Last1 + 1);
            return this;
        }

        ///<summary>
        ///Adjusts the capacity of the underlying <see cref="R.values"/> array to be the
        ///minimum count required to hold the current logical bits (up to <see cref="R.Count"/>).
        ///This can reduce memory usage if the list was previously larger or if
        ///operations caused overallocation. It potentially clears bits between the
        ///new count and the old count if shrinking.
        ///</summary>
        ///<returns>This <see cref="RW"/> instance after adjusting capacity.</returns>
        public RW Fit() => Capacity_(count);

        ///<summary>
        ///Sets the logical length (count) of this <see cref="BitList"/> to the specified
        ///number of bits.
        ///<para>
        ///If the new length <paramref name="bits"/> is less than the current <see cref="R.Count"/>, the list
        ///is truncated. Bits at indices <paramref name="bits"/> and higher are discarded.
        ///This may involve adjusting <see cref="R.trailingOnesCount"/> and clearing bits
        ///within the <see cref="R.values"/> array. The underlying <see cref="R.values"/> array capacity
        ///is also reduced to match the new requirement.
        ///</para>
        ///<para>
        ///If <paramref name="bits"/> is greater than the current count, the list is conceptually
        ///padded with '0' bits at the end to reach the new length. The underlying
        ///<see cref="R.values"/> array capacity might be increased, but no new '1' bits are set.
        ///</para>
        ///<para>
        ///If <paramref name="bits"/> is 0, the list is effectively cleared.
        ///</para>
        ///</summary>
        ///<param name="bits">The desired new length (count) of the <see cref="BitList"/> in bits.</param>
        ///<returns>This <see cref="RW"/> instance after setting the length.</returns>
        public RW Capacity_(int bits)
        {
            if (bits < 1)
            {
                Clear();
                return this;
            }

            if (bits <= trailingOnesCount)
            {
                trailingOnesCount = bits;
                values = [];
                used = 0;
                count = bits;
            }
            else if (bits < count)
            {
                var last1 = Last1;
                if (last1 < bits)
                {
                    count = bits;
                    return this;
                }

                var len = Len4Bits(bits - trailingOnesCount);
                if (len < values.Length)
                    Array.Resize(ref values, len);
                used = Math.Min(Used(), len) | IO;

                Set0(bits, last1 + 1);
                count = bits;
            }

            return this;
        }

        ///<summary>
        ///Sets the logical count of this <see cref="BitList"/>.
        ///If the new count is smaller than the current count, the list is truncated,
        ///discarding bits at indices <paramref name="count"/> and above. This is similar to
        ///<see cref="Capacity_"/> but might not shrink the underlying array capacity.
        ///If the new count is larger, the list is expanded, conceptually padding with
        ///'0' bits.
        ///</summary>
        ///<param name="count">The desired new count of the <see cref="BitList"/>. Must be non-negative.</param>
        ///<returns>This <see cref="RW"/> instance after resizing.</returns>
        public RW Count_(int count)
        {
            if (count < this.count)
            {
                if (count < 1)
                    Clear();
                else
                {
                    Set0(count, this.count);
                    this.count = count;
                }
            }
            else if (this.count < count)
                this.count = count;

            return this;
        }

        ///<summary>
        ///Resets this <see cref="BitList"/> to an empty state.
        ///Sets count and trailingOnesCount to 0, clears the <see cref="R.values"/> array
        ///(sets elements to 0), and resets the <see cref="used"/> count to 0.
        ///The capacity of the <see cref="R.values"/> array may be retained.
        ///</summary>
        ///<returns>This <see cref="RW"/> instance after clearing.</returns>
        public void Clear()
        {
            Array.Clear(values, 0, Used());
            used = 0;
            count = 0;
            trailingOnesCount = 0;
        }

        ///<summary>
        ///Creates and returns a deep copy of this <see cref="RW"/> instance.
        ///The clone will have the same count, trailing ones count, and bit values
        ///as the original, with its own independent copy of the underlying data.
        ///</summary>
        ///<returns>A new <see cref="RW"/> instance identical to this one.</returns>
        public new RW Clone() => (RW)base.Clone();

        ///<summary>
        ///Fills a range of bits within a <see cref="ulong"/> array with a specified value (0, 1, or 2 for toggle/flip).
        ///Operates on the conceptual bitstream represented by the array.
        ///</summary>
        ///<param name="src">The value to fill with: 0 (clear), 1 (set), any other (flip).</param>
        ///<param name="dst">The destination <see cref="ulong"/> array.</param>
        ///<param name="lo_bit">The starting bit index (inclusive, relative) of the range to fill.</param>
        ///<param name="hi_bit">The ending bit index (exclusive, relative) of the range to fill.</param>
        private static void Fill(int src, ulong[] dst, int loBit, int hiBit)
        {
            var loIndex = loBit >> LEN;
            var hiIndex = hiBit - 1 >> LEN;
            var loOffset = loBit & MASK;
            var hiOffset = hiBit - 1 & MASK;

            if (loIndex == hiIndex)
            {
                switch (src)
                {
                    case 0:
                        dst[loIndex] &= ~(Mask(hiBit - loBit) << loOffset);
                        return;
                    case 1:
                        dst[loIndex] |= Mask(hiBit - loBit) << loOffset;
                        return;
                    default:
                        dst[loIndex] ^= Mask(hiBit - loBit) << loOffset;
                        return;
                }
            }

            switch (src)
            {
                case 0:
                    dst[loIndex] &= Mask(loOffset);
                    for (var i = loIndex + 1; i < hiIndex; i++)
                        dst[i] = 0UL;
                    dst[hiIndex] &= ~Mask(hiOffset + 1);
                    break;
                case 1:
                    dst[loIndex] |= ~Mask(loOffset);
                    for (var i = loIndex + 1; i < hiIndex; i++)
                        dst[i] = ~0UL;
                    dst[hiIndex] |= Mask(hiOffset + 1);
                    break;
                default:
                    dst[loIndex] ^= ~Mask(loOffset);
                    for (var i = loIndex + 1; i < hiIndex; i++)
                        dst[i] ^= ~0UL;
                    dst[hiIndex] ^= Mask(hiOffset + 1);
                    break;
            }
        }

        ///<summary>
        ///Shifts a range of bits within a <see cref="ulong"/> array to the right (towards
        ///lower bit indices, LSB). Equivalent to <c>&gt;&gt;&gt;</c> operation on the conceptual bitstream.
        ///Optionally clears the bits vacated at the high end of the range.
        ///
        ///
        ///works like right bit-shift >>> on primitives.
        ///.                 MSB               LSB
        ///.                 |                 |
        ///bits in the list [0, 0, 0, 1, 1, 1, 1] Leading 3 zeros and trailing 4 ones
        ///index in the list 6 5 4 3 2 1 0
        ///shift left  ≪
        ///shift right  ≫
        ///
        ///</summary>
        ///<param name="src">The source <see cref="ulong"/> array.</param>
        ///<param name="dst">The destination <see cref="ulong"/> array. May be the same as src.</param>
        ///<param name="lo_bit">The starting bit index (inclusive, relative) of the range to shift.</param>
        ///<param name="hi_bit">The ending bit index (exclusive, relative) of the range to shift.</param>
        ///<param name="shift_bits">The number of positions to shift right (must be positive).</param>
        ///<param name="clear">If true, the vacated bits at the high end (indices <c>[hi_bit - shift_bits, hi_bit)</c>) are set to 0.</param>
        ///<returns>The modified <see cref="dst"/> array.</returns>
        internal static ulong[] ShiftRight(ulong[] src, ulong[] dst, int loBit, int hiBit, int shiftBits, bool clear)
        {
            if (hiBit <= loBit || shiftBits < 1)
                return src;

            if (src != dst && loBit > 0)
                Array.Copy(src, 0, dst, 0, Len4Bits(loBit));
            if (shiftBits < hiBit - loBit)
                BitCopy(src, loBit + shiftBits, dst, loBit, hiBit - loBit - shiftBits);

            if (clear)
                Fill(0, dst, hiBit - shiftBits, hiBit);
            return dst;
        }

        ///<summary>
        ///Shifts a range of bits within a <see cref="ulong"/> array to the left (towards
        ///higher bit indices, MSB). Equivalent to <c>&lt;&lt;</c> operation on the conceptual bitstream.
        ///Handles potential reallocation if the shift requires expanding the array.
        ///Optionally clears the bits vacated at the low end of the range.
        /// /// works like left bit-shift ≪ on primitives.
        ///.                 MSB               LSB
        ///.                 |                 |
        ///bits in the list [0, 0, 0, 1, 1, 1, 1] Leading 3 zeros and trailing 4 ones
        ///index in the list 6 5 4 3 2 1 0
        ///shift left  ≪
        ///shift right  ≫
        ///
        ///</summary>
        ///<param name="src">The source <see cref="ulong"/> array.</param>
        ///<param name="lo_bit">The starting bit index (inclusive, relative) of the range to shift.</param>
        ///<param name="hi_bit">The ending bit index (exclusive, relative) of the range to shift.</param>
        ///<param name="shift_bits">The number of positions to shift left (must be positive).</param>
        ///<param name="clear">If true, the vacated bits at the low end (indices <c>[lo_bit, lo_bit + shift_bits)</c>) are set to 0.</param>
        ///<returns>The modified <see cref="src"/> array, or a new, larger array if reallocation occurred.</returns>
        internal static ulong[] ShiftLeft(ulong[] src, int loBit, int hiBit, int shiftBits, bool clear)
        {
            if (hiBit <= loBit || shiftBits < 1)
                return src;

            var max = Len4Bits(hiBit + shiftBits);
            var dst = src;

            if (src.Length < max)
            {
                dst = new ulong[max * 3 / 2];
                if (loBit >> LEN > 0)
                    Array.Copy(src, 0, dst, 0, loBit >> LEN);
            }

            BitCopy(src, loBit, dst, loBit + shiftBits, hiBit - loBit);

            if (clear)
                Fill(0, dst, loBit, loBit + shiftBits);

            return dst;
        }

        ///<summary>
        ///Copies a specified number of bits from a source <see cref="ulong"/> array region
        ///to a destination <see cref="ulong"/> array region. Handles overlapping regions correctly.
        ///</summary>
        ///<param name="src">The source <see cref="ulong"/> array.</param>
        ///<param name="src_bit">The starting bit position in the source (relative index).</param>
        ///<param name="dst">The destination <see cref="ulong"/> array (can be the same as src).</param>
        ///<param name="dst_bit">The starting bit position in the destination (relative index).</param>
        ///<param name="bits">The number of bits to copy.</param>
        private static void BitCopy(ulong[] src, int srcBit, ulong[] dst, int dstBit, int bits)
        {
            var srcBits = srcBit + bits;
            var dstBits = dstBit + bits;

            var last = bits >> LEN;
            var lastBits = (bits - 1 & MASK) + 1;

            if (dst == src && dstBit < srcBit)
            {
                for (var i = 0; i < last; i++)
                    Set(Get_(src, srcBit + (i << LEN), srcBits), dst, dstBit + (i << LEN), dstBits);
                if (lastBits > 0)
                {
                    var s = Get_(src, srcBit + (last << LEN), srcBits);
                    var d = Get_(dst, dstBit + (last << LEN), dstBits);
                    Set(d ^ (s ^ d) & Mask(lastBits), dst, dstBit + (last << LEN), dstBits);
                }
            }
            else
            {
                for (var i = 0; i < last; i++)
                    Set(Get_(src, srcBit + bits - (i + 1 << LEN), srcBits), dst, dstBit + bits - (i + 1 << LEN), dstBits);
                if (lastBits > 0)
                {
                    var s = Get_(src, srcBit, srcBits);
                    var d = Get_(dst, dstBit, dstBits);
                    Set(d ^ (s ^ d) & Mask(lastBits), dst, dstBit, dstBits);
                }
            }
        }

        ///<summary>
        ///Extracts a 64-bit word (ulong) from a <see cref="ulong"/> array, starting at a
        ///specified bit offset within the array's conceptual bitstream.
        ///Handles words that span across two <see cref="ulong"/> elements.
        ///</summary>
        ///<param name="src">The source <see cref="ulong"/> array.</param>
        ///<param name="bit">The starting bit position (0-based index relative to the start of src).</param>
        ///<param name="src_bits">The total number of valid bits in the conceptual bitstream represented by src (used for boundary checks).</param>
        ///<returns>The 64-bit word starting at the specified bit position. Bits beyond <paramref name="src_bits"/> are treated as 0.</returns>
        protected static ulong Get_(ulong[] src, int bit, int srcBits)
        {
            if (bit < 0 || bit >= srcBits)
                return 0;
            var index = bit >> LEN;
            var offset = bit & MASK;
            var result = src[index] >> offset;
            if (offset > 0 && bit + BITS - offset < srcBits && index + 1 < src.Length)
                result |= src[index + 1] << BITS - offset;
            return result;
        }

        ///<summary>
        ///Sets (writes) a 64-bit word (ulong) into a destination <see cref="ulong"/> array
        ///at a specified bit offset within the array's conceptual bitstream.
        ///Handles words that span across two <see cref="ulong"/> elements. Assumes destination
        ///array is large enough.
        ///</summary>
        ///<param name="src">The 64-bit word to write.</param>
        ///<param name="dst">The destination <see cref="ulong"/> array.</param>
        ///<param name="bit">The starting bit position (0-based index relative to start of dst) to write to.</param>
        ///<param name="dst_bits">The total number of valid bits in the destination conceptual bitstream (used for boundary checks/masking).</param>
        private static void Set(ulong src, ulong[] dst, int bit, int dstBits)
        {
            var index = bit >> LEN;
            var offset = bit & MASK;

            if (offset == 0)
                dst[index] = src;
            else
            {
                dst[index] = dst[index] & Mask(offset) | src << offset;
                var next = index + 1;
                if (next < dst.Length && next < Len4Bits(dstBits))
                    dst[next] = dst[next] & ~0UL << offset | src >> BITS - offset;
            }
        }

        ///<summary>
        ///Finds the index of the first bit where two BitLists differ.
        ///</summary>
        ///<param name="other">The other BitList to compare with.</param>
        ///<returns>The index of the first differing bit, or the minimum of the two counts if they are identical up to the shorter length.</returns>
        public int FindFirstDifference(R other)
        {
            var checkLimit = Math.Min(count, other.count);
            var toc1 = trailingOnesCount;
            var toc2 = other.trailingOnesCount;
            var commonToc = Math.Min(toc1, toc2);

            if (toc1 != toc2)
                return commonToc;

            var bit = commonToc;
            while (bit < checkLimit)
            {
                var word1 = Get_(values, bit - trailingOnesCount, count - trailingOnesCount);
                var word2 = Get_(other.values, bit - other.trailingOnesCount, other.count - other.trailingOnesCount);
                if (word1 != word2)
                {
                    var diffOffset = BitOperations.TrailingZeroCount(word1 ^ word2);
                    var diffBit = bit + diffOffset;
                    return Math.Min(diffBit, checkLimit);
                }

                bit += BITS;
            }

            return checkLimit;
        }

        ///<summary>
        ///Implements the indexer for the list.
        ///</summary>
        ///<param name="index">The index of the bit to retrieve or set.</param>
        ///<returns>The bit at the specified index.</returns>
        public bool this[int index]
        {
            get => Get(index);
            set => Set(index, value);
        }

        ///<summary>
        ///Implements the enumerator for the list.
        ///</summary>
        ///<returns>An enumerator for the bits in the list.</returns>
        public new IEnumerator<bool> GetEnumerator() => base.GetEnumerator();

        ///<summary>
        ///Adds a bit to the end of the list.
        ///</summary>
        ///<param name="item">The bit to add.</param>
        public void Add(bool item) => Set(count, item);

        ///<summary>
        ///Determines whether the list contains a specific bit.
        ///</summary>
        ///<param name="item">The bit to locate.</param>
        ///<returns><c>true</c> if the bit is found; otherwise, <c>false</c>.</returns>
        public bool Contains(bool item) => IndexOf(item) >= 0;

        ///<summary>
        ///Copies the bits of the list to an array, starting at a particular array index.
        ///</summary>
        ///<param name="dst">The array to copy to.</param>
        ///<param name="dstIndex">The zero-based index in the array at which copying begins.</param>
        public void CopyTo(bool[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
            if (dst.Length - dstIndex < Count)
                throw new ArgumentException("Destination array is not long enough.");

            for (var i = 0; i < count; i++)
                dst[dstIndex + i] = Get(i);
        }

        ///<summary>
        ///Determines the index of a specific bit in the list.
        ///</summary>
        ///<param name="item">The bit to locate.</param>
        ///<returns>The index of the bit if found; otherwise, -1.</returns>
        public int IndexOf(bool item) => item ? Next1(-1) : Next0(-1);

        ///<summary>
        ///Inserts a bit at a specified index.
        ///</summary>
        ///<param name="index">The zero-based index at which the bit should be inserted.</param>
        ///<param name="item">The bit to insert.</param>
        public void Insert(int index, bool item)
        {
            if (item)
                Add1(index);
            else
                Add0(index);
        }

        ///<summary>
        ///Removes the first occurrence of a specific bit from the list.
        ///</summary>
        ///<param name="item">The bit to remove.</param>
        ///<returns><c>true</c> if the bit was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(bool item)
        {
            var index = IndexOf(item);
            if (index < 0)
                return false;
            RemoveAt(index);
            return true;
        }

        ///<summary>
        ///Removes the bit at a specified index.
        ///</summary>
        ///<param name="index">The zero-based index of the bit to remove.</param>
        public void RemoveAt(int index)
        {
            var bit = index;
            if (bit < 0 || bit >= count)
                return;
            count--;

            if (bit < trailingOnesCount)
            {
                trailingOnesCount--;
                return;
            }

            var last1 = Last1;
            if (last1 < bit)
                return;

            if (bit == last1)
            {
                bit -= trailingOnesCount;
                values[bit >> LEN] &= ~(1UL << (bit & MASK));
                used |= IO;
                return;
            }

            var last1InValues = last1 - trailingOnesCount;

            if (bit != trailingOnesCount)
                ShiftRight(values, values, bit - trailingOnesCount, last1InValues + 1, 1, true);
            else
            {
                var next0 = Next0(bit);
                if (next0 == bit + 1)
                {
                    ShiftRight(values, values, 1, last1InValues + 1, 1, true);
                    return;
                }

                if (next0 == -1 || last1 < next0)
                {
                    trailingOnesCount += last1InValues;
                    Array.Clear(values, 0, Used());
                }
                else
                {
                    var shift = next0 - bit;
                    trailingOnesCount += shift - 1;
                    ShiftRight(values, values, 1, last1InValues + 1, shift, true);
                }
            }

            used |= IO;
        }

        ///<summary>
        ///Gets a value indicating whether the list is read-only.
        ///</summary>
        public bool IsReadOnly => false;
    }

    ///<summary>
    ///Resizes an array by moving elements within or between arrays, optimized for dynamic array resizing.
    ///This method is a low-level utility for array resizing, allowing elements to be shifted
    ///to accommodate insertion or removal of space at a specified index. Unlike a simple expansion
    ///using Array.Resize followed by manual shifting, this method directly positions elements
    ///in the destination array with the required shift in a single, coordinated operation.
    ///</summary>
    ///<param name="src">The source array containing the original elements.</param>
    ///<param name="dst">The destination array where elements are moved. Can be the same as src for in-place resizing.
    ///                  When enlarging, dst is typically a newly allocated array with sufficient capacity.</param>
    ///<param name="index">The starting index where resizing occurs (elements are inserted or removed).
    ///                   Must be non-negative and typically less than or equal to count.</param>
    ///<param name="count">The current number of valid elements in the source array.</param>
    ///<param name="resize">The resize amount. Positive values enlarge the array by adding space at index,
    ///                    shifting elements right; negative values shrink it by removing elements from index,
    ///                    shifting elements left. Zero leaves the count unchanged but is not optimized separately.</param>
    ///<returns>The new count of the array after resizing, representing the updated number of valid elements
    ///in the destination array up to which elements are properly copied.</returns>
    public static int Resize<T>(T[] src, T[] dst, int index, int count, int resize)
    {
        if (src == null || dst == null)
            throw new ArgumentNullException(src == null ? nameof(src) : nameof(dst));
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative.");
        if (count < 0 || count > src.Length)
            throw new ArgumentOutOfRangeException(nameof(count), "Count is invalid.");
        if (resize == 0)
            return count;

        if (resize < 0)
        {
            if (src != dst && 0 < index)
                Array.Copy(src, 0, dst, 0, index);

            if (count <= index + -resize)
                return index;
            Array.Copy(src, index + -resize, dst, index, count - (index + -resize));
            return count + resize;
        }

        if (count < 1)
            return Math.Max(index, count) + resize;

        if (index < count)
            if (index == 0)
                Array.Copy(src, 0, dst, resize, count);
            else
            {
                if (src != dst && 0 < index)
                    Array.Copy(src, 0, dst, 0, index);
                Array.Copy(src, index, dst, index + resize, count - index);
            }
        else if (src != dst)
            Array.Copy(src, 0, dst, 0, count);

        return Math.Max(index, count) + resize;
    }
}