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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace org.unirail.collections;

///<summary>
///Defines a contract for a list that efficiently manages nullable reference types of type <typeparamref name="T"/>.
///Implementations typically optimize memory and performance using two strategies:
///<list type="bullet">
///    <item>
///        <term>Compressed Strategy</term>
///        <description>Minimizes memory usage by storing only non-null values in a compact array, with a <see cref="BitList.RW"/> tracking nullity.</description>
///    </item>
///    <item>
///        <term>Flat Strategy</term>
///        <description>Prioritizes fast access by storing all elements, including _nulls, in a single array at their logical indices.</description>
///    </item>
///</list>
///Implementations may dynamically switch between these strategies based on null density and a configurable threshold,
///balancing memory efficiency and access speed.
///</summary>
///<typeparam name="T">The type of elements stored in the list, which must be a reference type.</typeparam>
public interface NullableObjectList<T>
    where T : class
{
    ///<summary>
    ///Abstract base class providing read-only functionality for a list that efficiently manages nullable reference types.
    ///It employs two internal storage strategies: Compressed and Flat.
    ///<para>
    ///<b>Compressed Strategy:</b>
    ///- Optimizes memory for lists with many _nulls.
    ///- Non-null values are stored contiguously in the <see cref="values"/> array. The physical index in <see cref="values"/> is determined by the rank of the corresponding bit in <see cref="nulls"/>.
    ///- A <see cref="BitList.RW"/> instance (<see cref="nulls"/>) tracks the nullity of each logical position: `true` if the value is non-null (and present in <see cref="values"/>), `false` if null.
    ///- `size_card` holds the count of non-null values (the physical size of the non-null portion of the <see cref="values"/> array).
    ///- The total logical count is managed by `_nulls.Count`.
    ///- Accessing elements involves using the bitlist to determine nullity and the physical index in `values`.
    ///</para>
    ///<para>
    ///<b>Flat Strategy:</b>
    ///- Optimizes access speed for lists with fewer _nulls.
    ///- All elements, including _nulls, are stored directly at their logical indices in the <see cref="values"/> array.
    ///- The <see cref="nulls"/> field is `null`.
    ///- `size_card` holds the total logical count (the number of elements currently represented in the <see cref="values"/> array, which may be less than its allocated length).
    ///- Nullity is determined by checking if an element in `values` is `null`.
    ///</para>
    ///The strategy can switch dynamically in concrete implementations (like <see cref="RW"/>) based on null density relative to the <see cref="flatStrategyThreshold"/>.
    ///</summary>
    ///<typeparam name="T">The type of elements in the list, which must be a reference type.</typeparam>
    public abstract class R : ICloneable, IReadOnlyList<T?>, IEquatable<R>
    {
        ///<summary>
        ///Tracks nullity in the compressed strategy using a <see cref="BitList.RW"/>.
        ///Each bit corresponds to a logical index: <c>true</c> for non-null, <c>false</c> for null.
        ///The position of a <c>true</c> bit determines the physical index in the <see cref="values"/> array via its rank.
        ///In flat strategy, this is <c>null</c>, as nullity is tracked by explicit <c>null</c> values in <see cref="values"/>.
        ///</summary>
        protected BitList.RW? nulls;

        ///<summary>
        ///Stores the list's elements.
        ///In compressed strategy, contains only non-null values; in flat strategy, contains all elements, including _nulls.
        ///The size of this array in compressed strategy is <see cref="size_card"/>. In flat strategy, its allocated length determines the maximum accessible logical index, and <see cref="size_card"/> is the logical count.
        ///</summary>
        protected internal T?[] values;

        ///<summary>
        ///Provides type-safe equality checks and hashing for non-null values of type <typeparamref name="T"/>.
        ///</summary>
        protected readonly EqualityComparer<T> equal_hash_V;

        ///<summary>
        ///In compressed strategy, this holds the count of non-null elements (Cardinality), which is also the number of elements currently stored in the <see cref="values"/> array.
        ///In flat strategy, this holds the total logical count of the list.
        ///</summary>
        protected internal int size_card = 0;

        ///<summary>
        ///Threshold for switching from compressed to flat strategy.
        ///When in compressed strategy, if the Cardinality (<see cref="size_card"/>) exceeds this value, the list may switch to the flat strategy upon a modification that adds a non-null element or extends the logical size significantly.
        ///When switching back from flat to compressed, this value might be used to determine if compressed is suitable (e.g., if calculated cardinality is below the threshold).
        ///Defaults to 1024.
        ///</summary>
        protected internal int flatStrategyThreshold = 1024;

        ///<summary>
        ///Indicates the current internal storage strategy: <c>true</c> for flat, <c>false</c> for compressed.
        ///</summary>
        protected internal bool isFlatStrategy = false;

        ///<summary>
        ///Initializes a new read-only instance with a specified equality comparer.
        ///The list is initially empty and in the compressed strategy.
        ///</summary>
        ///<param name="equal_hash_V">The equality comparer for type <typeparamref name="T"/>. If <c>null</c>, the default comparer is used.</param>
        protected R(EqualityComparer<T> equal_hash_V)
        {
            this.equal_hash_V = equal_hash_V ?? EqualityComparer<T>.Default;
            values = []; //Empty array for initial efficiency
        }

        ///<summary>
        ///Switches to the flat strategy, reallocating <see cref="values"/> to include all elements, including _nulls.
        ///</summary>
        protected void SwitchToFlatStrategy() { SwitchToFlatStrategy(nulls!.count); }

        ///<summary>
        ///Switches the list's internal representation to the flat strategy, ensuring the underlying array
        ///has at least the specified <paramref name="capacity"/> capacity.
        ///This is an internal helper used when the target size is known during an operation (like Set).
        ///</summary>
        ///<param name="capacity">The minimum required capacity for the flat strategy array.</param>
        protected void SwitchToFlatStrategy(int capacity)
        {
            if (Count == 0)
            {
                if (values.Length == 0)
                    values = new T?[16];
                isFlatStrategy = true;
                return;
            }

            var compressed = values;
            values = new T?[Math.Max(16, capacity)];
            for (int i = -1, ii = 0; (i = nulls!.Next1(i)) != -1;)
                values[i] = compressed[ii++];

            size_card = nulls.Count;
            nulls = null;
            isFlatStrategy = true;
        }

        ///<summary>
        ///Switches the list's internal representation to the compressed strategy.
        ///This involves creating a new <see cref="BitList.RW"/> to track nullity,
        ///compacting non-null values into a smaller <see cref="values"/> array,
        ///and updating <see cref="size_card"/> to the new count of non-null values.
        ///</summary>
        protected void SwitchToCompressedStrategy()
        {
            nulls = new BitList.RW(size_card);
            var ii = 0;
            for (var i = 0; i < size_card; i++)
                if (values[i] != null)
                {
                    nulls.Set1(i);
                    values[ii++] = values[i];
                }

            Array.Resize(ref values, ii);
            size_card = ii;
            isFlatStrategy = false;
        }

        ///<summary>
        ///Copies a range of logical elements into a destination array, preserving _nulls.
        ///Elements are copied from the logical <paramref name="index"/> up to <paramref name="index"/> + <paramref name="len"/> - 1.
        ///If the destination array is null or too small, a new array of the required size is created.
        ///</summary>
        ///<param name="index">The zero-based starting logical index from which to begin copying.</param>
        ///<param name="len">The number of logical elements to copy.</param>
        ///<param name="dst">The destination array. If null or insufficient length, a new array is created and returned.</param>
        ///<returns>The destination array containing the copied logical elements. If <paramref name="len"/> is zero or the source range is empty, returns the provided <paramref name="dst"/> array (which might be null).</returns>
        public T?[] ToArray(int index, int len, T?[]? dst)
        {
            if (Count == 0)
                return dst;
            index = Math.Max(0, index);
            len = Math.Min(len, Count - index);
            if (len <= 0)
                return dst;

            if (dst == null || dst.Length < len)
                dst = new T?[len];

            if (isFlatStrategy)
                Array.Copy(values, index, dst, 0, Math.Min(len, size_card - index));
            else
                for (int i = 0, srcIndex = index; i < len && srcIndex < Count; i++, srcIndex++)
                    dst[i] = HasValue(srcIndex) ? values[nulls!.Rank(srcIndex) - 1] : null;
            return dst;
        }

        ///<summary>
        ///Checks if this list contains all elements (including _nulls) present in another <see cref="R"/> list.
        ///The comparison uses the equality comparer provided during construction.
        ///</summary>
        ///<param name="src">The source list whose elements are checked for presence in this list.</param>
        ///<returns><c>true</c> if every logical element (value or null) from <paramref name="src"/> is found in this list at least once, <c>false</c> otherwise.</returns>
        public bool ContainsAll(R src)
        {
            for (int i = 0, s = src.Count; i < s; i++)
                if (IndexOf(src[i]) == -1)
                    return false;
            return true;
        }

        ///<summary>
        ///Returns the allocated physical capacity of the internal <see cref="values"/> array.
        ///<remarks>
        ///In compressed strategy, this capacity may be larger than the number of stored non-null values (<see cref="size_card"/>).
        ///In flat strategy, this capacity may be larger than the logical count (<see cref="size_card"/>).
        ///This value indicates the potential storage without requiring reallocation of the primary data array.
        ///</remarks>
        ///</summary>
        ///<returns>The allocated length of the internal <see cref="values"/> array.</returns>
        public int Capacity() => values.Length;

        ///<summary>
        ///Gets the total logical size of the list, including _nulls.
        ///This is the number of elements that can be accessed via logical indices from 0 to <c>Count - 1</c>.
        ///</summary>
        public int Count => isFlatStrategy ? size_card : //In flat mode, size_card is the total count.
                                nulls!.Count;            //In compressed mode, bitlist's count is the total count.

        ///<summary>
        ///Gets the number of non-null elements currently stored in the list.
        ///</summary>
        ///<value>The count of non-null elements.</value>
        public int Cardinality => isFlatStrategy ? values.Count(v => v != null) : //Need to count in flat mode.
                                      size_card;                                  //In compressed mode, size_card is the cardinality.

        ///<summary>
        ///Checks if the list is logically empty (has a <see cref="Count"/> of zero).
        ///</summary>
        ///<returns><c>true</c> if the list has no logical elements, <c>false</c> otherwise.</returns>
        public bool IsEmpty => Count < 1;

        ///<summary>
        ///Checks if the specified logical index contains a non-null value.
        ///This is equivalent to checking if <c>this[index] != null</c> but is more efficient.
        ///</summary>
        ///<param name="index">The zero-based logical index to check.</param>
        ///<returns>
        ///<c>true</c> if the element at the index is non-null and the index is within the list's bounds;
        ///<c>false</c> if the element is null or the index is out of bounds.</returns>
        public bool HasValue(int index) => 0 <= index && index < Count && (                                            //Index out of bounds.
                                                                              isFlatStrategy ? values[index] != null : //Flat: Check value directly.
                                                                                  nulls!.Get(index));                  //Compressed: Check the bitlist.

        ///<summary>
        ///Finds the logical index of the next non-null value after the specified index.
        ///</summary>
        ///<param name="index">The index to start the search from. Pass -1 to start from the beginning.</param>
        ///<returns>The logical index of the next non-null value, or -1 if no non-null value is found after <paramref name="index"/>.</returns>
        public int NextValueIndex(int index)
        {
            if (!isFlatStrategy)
                return nulls!.Next1(index);

            for (var i = index; ++i < size_card;)
                if (values[i] != null)
                    return i;
            return -1;
        }

        ///<summary>
        ///Finds the logical index of the previous non-null value before the specified index.
        ///</summary>
        ///<param name="index">The index to start the search from. Pass -1 or >= <see cref="Count"/>, to start searches from the end.</param>
        ///<returns>The logical index of the previous non-null value, or -1 if no non-null value is found before <paramref name="index"/>.</returns>
        public int PrevValueIndex(int index)
        {
            if (!isFlatStrategy)
                return nulls!.Prev1(index);
            for (var i = index; -1 < --i;)
                if (values[i] != null)
                    return i;
            return -1;
        }

        ///<summary>
        ///Finds the logical index of the next null value after the specified index.
        ///</summary>
        ///<param name="index">The index to start the search from. Pass -1 to start from the beginning.</param>
        ///<returns>The logical index of the next null value, or -1 if no null value is found at or after <paramref name="index"/>.</returns>
        public int NextNullIndex(int index)
        {
            if (!isFlatStrategy)
                return nulls!.Next0(index);

            for (var i = index; ++i < size_card;)
                if (values[i] == null)
                    return i;
            return -1;
        }

        ///<summary>
        ///Finds the logical index of the previous null value before the specified index.
        ///</summary>
        ///<param name="index">The index to start the search from. Pass -1 or >= <see cref="Count"/>, to start searches from the end.</param>
        ///<returns>The logical index of the previous null value, or -1 if no null value is found before <paramref name="index"/>.</returns>
        public int PrevNullIndex(int index)
        {
            if (!isFlatStrategy)
                return nulls!.Prev0(index);

            for (var i = index; -1 < --i;)
                if (values[i] == null)
                    return i;
            return -1;
        }

        ///<summary>
        ///Gets the element at the specified logical index.
        ///<remarks>
        ///If the index is out of bounds (< 0 or >= <see cref="Count"/>), returns null.
        ///In flat strategy, this directly accesses the <see cref="values"/> array.
        ///In compressed strategy, it checks the corresponding bit in <see cref="nulls"/> and, if non-null, retrieves the value from <see cref="values"/> using the rank.
        ///</remarks>
        ///</summary>
        ///<param name="index">The zero-based logical index of the element to get.</param>
        ///<returns>The value at the specified logical index, or <c>null</c> if the element is null or the index is out of bounds.</returns>
        public virtual T? this[int index]
        {
            get => index < 0 || index >= Count ? null : isFlatStrategy ? values[index]
                                                    : nulls!.Get(index) ? values[nulls.Rank(index) - 1]
                                                                        : null;
            set => throw new NotSupportedException("Setting values is not supported in R.");
        }

        ///<summary>
        ///Searches for the first occurrence of the specified <paramref name="value"/> (including null) in the list and returns its logical index.
        ///The comparison uses the equality comparer provided during construction.
        ///</summary>
        ///<param name="value">The value to locate in the list (can be null).</param>
        ///<returns>The zero-based logical index of the first occurrence of <paramref name="value"/>, if found; otherwise, -1.</returns>
        public int IndexOf(T? value)
        {
            if (isFlatStrategy) //Flat: Linear scan using the provided equality comparer.
            {
                for (var i = 0; i < size_card; i++)            //size_card is Count in flat mode
                    if (equal_hash_V.Equals(values[i], value)) //Use EqualityComparer<T>.Equals to handle potential _nulls in values array correctly.
                        return i;
                return -1;
            }

            if (value == null)
                return NextNullIndex(-1); //Search for the first null index.

            for (var i = 0; i < size_card; i++)
                if (equal_hash_V.Equals(values[i], value))
                    return nulls!.Bit(i + 1);

            return -1;
        }

        ///<summary>
        ///Determines whether an element with the specified <paramref name="value"/> (including null) is in the list.
        ///The comparison uses the equality comparer provided during construction.
        ///</summary>
        ///<param name="value">The value to locate in the list (can be null).</param>
        ///<returns><c>true</c> if <paramref name="value"/> is found in the list; otherwise, <c>false</c>.</returns>
        public bool Contains(T? value) => IndexOf(value) != -1;

        ///<summary>
        ///Searches for the last occurrence of the specified <paramref name="value"/> (including null) in the list and returns its logical index.
        ///The comparison uses the equality comparer provided during construction.
        ///</summary>
        ///<param name="value">The value to locate in the list (can be null).</param>
        ///<returns>The zero-based logical index of the last occurrence of <paramref name="value"/>, if found; otherwise, -1.</returns>
        public int LastIndexOf(T? value)
        {
            if (isFlatStrategy)
            {
                for (var i = size_card; -1 < --i;)
                    if (equal_hash_V.Equals(values[i], value))
                        return i;
                return -1;
            }

            if (value == null)
                return PrevNullIndex(Count);
            for (var i = size_card; -1 < --i;)
                if (equal_hash_V.Equals(values[i], value))
                    return nulls!.Bit(i + 1);

            return -1;
        }

        ///<summary>
        ///Computes a hash code based on the list's logical content (the sequence of elements, including _nulls).
        ///<remarks>
        ///The hash code calculation differs slightly between the two strategies, which may lead to different hash codes
        ///for logically identical lists represented by different strategies. This is a known limitation based on the current implementation details.
        ///The flat strategy includes null positions in the hash calculation, while the compressed strategy hashes only the non-null values.
        ///</remarks>
        ///</summary>
        ///<returns>The hash code for the list.</returns>
        public override int GetHashCode()
        {
            var hash = 17;
            if (isFlatStrategy)
                for (var i = 0; i < size_card; i++)
                    hash = HashCode.Combine(hash, equal_hash_V.GetHashCode(values[i]));
            else
                for (int i = 0, s = nulls.Count; i < s; i++)
                    hash = HashCode.Combine(hash, equal_hash_V.GetHashCode(nulls![i] ? values[i] : null));

            return HashCode.Combine(hash, Count);
        }

        ///<summary>
        ///Compares this list with another object for logical equality.
        ///</summary>
        ///<param name="obj">The object to compare with.</param>
        ///<returns><c>true</c> if the object is an equivalent <see cref="R"/> instance with the same logical sequence of elements (including _nulls), <c>false</c> otherwise.</returns>
        public override bool Equals(object? obj) => obj is R other && Equals(other);

        ///<summary>
        ///Compares this list to another <see cref="R"/> instance for logical equality.
        ///Two lists are considered logically equal if they have the same <see cref="Count"/> and
        ///the element at each logical index is equal (using the configured equality comparer for non-_nulls, and treating _nulls as equal to null).
        ///</summary>
        ///<param name="other">The other <see cref="R"/> instance to compare with.</param>
        ///<returns><c>true</c> if the lists are logically equal, <c>false</c> otherwise.</returns>
        public bool Equals(R? other)
        {
            if (other == this)
                return true;
            if (other == null || Count != other.Count)
                return false;

            for (var i = 0; i < Count; i++)
            {
                var value1 = this[i];
                var value2 = other[i];

                if (value1 != value2 && (value1 == null || !equal_hash_V.Equals(value1, value2)))
                    return false;
            }

            return true;
        }

        ///<summary>
        ///Creates a deep copy of this list.
        ///The cloned list will have the same logical content and strategy as the original.
        ///</summary>
        ///<returns>A new <see cref="R"/> instance (or a derived type if called on a subclass) with the same content, equality comparer, and internal state.</returns>
        public object Clone()
        {
            var dst = (R)MemberwiseClone();
            dst.nulls = nulls?.Clone() as BitList.RW;
            dst.values = (T?[])values.Clone();
            return dst;
        }

        ///<summary>
        ///Returns a JSON string representation of the list's logical content.
        ///Elements are represented as strings (or "null" for null elements), enclosed in square brackets and separated by commas.
        ///</summary>
        ///<returns>A string containing the JSON representation of the list.</returns>
        public override string ToString() => ToJSON(new StringBuilder()).ToString();

        ///<summary>
        ///Appends a JSON string representation of the list's logical content to a <see cref="StringBuilder"/>.
        ///Elements are represented as strings (or "null" for null elements), enclosed in square brackets and separated by commas.
        ///</summary>
        ///<param name="sb">The StringBuilder to append the JSON representation to.</param>
        ///<returns>The StringBuilder instance after the JSON representation has been appended.</returns>
        public StringBuilder ToJSON(StringBuilder sb)
        {
            sb.Append('[');
            var count = Count;
            if (0 < count)
            {
                //Rough estimate: 10 characters for value + 1 for comma/space
                //Use a more generous estimate like 20 characters per element
                sb.EnsureCapacity(sb.Length + count * 20);

                for (var i = 0; i < count; i++)
                {
                    if (i > 0)
                        sb.Append(',');

                    var value = this[i];
                    sb.Append(value == null ? "null" : value is string s ? $"\"{s.Replace("\"", "\\\"")}\""
                                                                         : value.ToString());
                }
            }

            sb.Append(']');
            return sb;
        }

        ///<summary>
        ///Sets a value at a specific logical index in the target list, handling internal storage strategies.
        ///This method can extend the list if the index is greater than or equal to the current logical count.
        ///It handles updates (setting a non-null to non-null), setting a null (making non-null become null),
        ///and inserting/setting a non-null (making null become non-null or extending with a non-null).
        ///May trigger resizing or strategy switches.
        ///</summary>
        ///<param name="dst">The target <see cref="R"/> instance (typically an <see cref="RW"/> instance) to modify.</param>
        ///<param name="index">The zero-based logical index to set.</param>
        ///<param name="value">The nullable value to set at the specified index.</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative.</exception>
        protected static void Set(R dst, int index, T? value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (dst.isFlatStrategy)
            {
                if (dst.values.Length <= index)
                {
                    Array.Resize(ref dst.values, Math.Max(16, index * 3 / 2));
                    dst.size_card = index + 1;
                }
                else if (dst.size_card <= index)
                    dst.size_card = index + 1;

                dst.values[index] = value;
            }
            else if (value == null)
            {
                if (dst.nulls.Get(index))
                    dst.size_card = BitList.Resize(dst.values, dst.values, dst.nulls.Rank(index) - 1, dst.size_card, -1);

                dst.nulls.Set0(index);
            }
            else if (dst.nulls!.Get(index))
                dst.values[dst.nulls.Rank(index) - 1] = value;
            else if (dst.values.Length <= dst.size_card && dst.flatStrategyThreshold < dst.size_card)
            {
                dst.SwitchToFlatStrategy(Math.Max(dst.nulls!.count, index + 1));
                dst.values[index] = value;
                dst.size_card = Math.Max(dst.size_card, index + 1);
            }
            else
            {
                var rank = dst.nulls.Rank(index);

                dst.size_card = BitList.Resize(dst.values,
                                               dst.values.Length == dst.size_card ? dst.values = new T[Math.Max(16, dst.size_card * 3 / 2)] : dst.values,
                                               rank, dst.size_card, 1);

                dst.values[rank] = value;
                dst.nulls.Set1(index);
            }
        }

        ///<summary>
        ///Returns an enumerator that iterates through the list's logical elements.
        ///</summary>
        ///<returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the elements in the list.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        ///<summary>
        ///Returns an enumerator that iterates through the list's logical elements.
        ///</summary>
        ///<returns>An <see cref="IEnumerator{T?}" /> that can be used to iterate through the elements in the list.</returns>
        public IEnumerator<T?> GetEnumerator() => new Enumerator(this);

        ///<summary>
        ///Provides an efficient enumerator for iterating over the logical elements of a <see cref="NullableObjectList{T}.R"/> list.
        ///</summary>
        public struct Enumerator : IEnumerator<T?>
        {
            private readonly R _list;
            private int _index;

            internal Enumerator(R list)
            {
                _list = list;
                _index = -1;
            }

            public T? Current => _list[_index];

            object? IEnumerator.Current => Current;
            public bool MoveNext() => ++_index < _list.Count;
            public void Reset() => _index = -1;
            public void Dispose() { }
        }
    }

    ///<summary>
    ///Provides a read-write implementation for a list that efficiently manages nullable reference types,
    ///extending the read-only functionality of <see cref="R"/> with methods for adding, removing, and modifying elements.
    ///It dynamically switches between compressed and flat storage strategies to optimize performance and memory usage.
    ///</summary>
    ///<typeparam name="T">The type of elements in the list, which must be a reference type and allows null values.</typeparam>
    public class RW : R, IList<T?>
    {
        ///<summary>
        ///Constructs an empty read-write list with a specified equality comparer and initial internal capacity.
        ///The list starts in the compressed strategy with a logical count of zero.
        ///</summary>
        ///<param name="equal_hash_V">The equality comparer for type <typeparamref name="T"/>. If <c>null</c>, the default comparer is used.</param>
        ///<param name="items">The initial capacity for the internal {@code values} array, which determines the
        ///                    number of elements  the list can hold without resizing.
        ///                    If positive, it sets the initial capacity.
        ///                    If negative, the list is initialized with a capacity and size of {@code -items},
        ///                    filled  with null elements..</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is negative.</exception>
        public RW(EqualityComparer<T> equal_hash_V, int items) : base(equal_hash_V)
        {
            var length = Math.Abs(items);
            nulls = new BitList.RW(length);
            values = length == 0 ? [] : new T[Math.Max(16, length)]; //Allocate values array with initial capacity

            if (items < 0)
                Set1(-items - 1, null); //+ set size
        }

        ///<summary>
        ///Sets the threshold for automatically switching from the compressed strategy to the flat strategy.
        ///When the number of non-null elements (<see cref="Cardinality"/>) in the compressed strategy meets or exceeds this threshold,
        ///the list may switch to the flat strategy upon the next modification operation that increases cardinality or extends the list.
        ///Setting a new threshold may trigger an immediate strategy switch if the current state warrants it.
        ///</summary>
        ///<param name="threshold">The new threshold value. Must be non-negative. A higher value favors the compressed strategy, while a lower value favors the flat strategy.</param>
        public void FlatStrategyThreshold(int threshold)
        {
            if (isFlatStrategy)
            {
                if (Cardinality < (flatStrategyThreshold = threshold))
                    SwitchToCompressedStrategy();
            }
            else if ((flatStrategyThreshold = threshold) <= size_card)
                SwitchToFlatStrategy();
        }

        ///<summary>
        ///Creates a deep copy of this read-write list.
        ///The cloned list is an independent <see cref="RW"/> instance with the same logical content and internal state (strategy, capacity, threshold).
        ///</summary>
        ///<returns>A new <see cref="RW"/> instance that is a deep copy of this list.</returns>
        public new RW Clone() => (RW)base.Clone();

        ///<summary>
        ///Removes the last logical element from the list.
        ///Decreases the list's logical <see cref="Count"/> by one.
        ///</summary>
        ///<returns>This <see cref="RW"/> instance, allowing for method chaining.</returns>
        public RW Remove() => Remove(Count - 1);

        ///<summary>
        ///Removes the logical element at the specified <paramref name="index"/>.
        ///Shifts any subsequent elements to the left. Decreases the list's logical <see cref="Count"/> by one.
        ///This operation may trigger internal resizing.
        ///</summary>
        ///<param name="index">The zero-based logical index of the element to remove.</param>
        ///<returns>This <see cref="RW"/> instance, allowing for method chaining.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or greater than or equal to <see cref="Count"/>.</exception>
        public RW Remove(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (Count < 1 || Count <= index)
                return this;
            if (isFlatStrategy)
                size_card = BitList.Resize(values, values, index, size_card, -1);
            else
            {
                if (nulls.Get(index))
                    size_card = BitList.Resize(values, values, nulls.Rank(index) - 1, size_card, -1);
                nulls.RemoveAt(index);
            }

            return this;
        }

        ///<summary>
        ///Sets the value of the last logical element in the list.
        ///If the list is empty, this method behaves like <see cref="Add(T?)"/>.
        ///</summary>
        ///<param name="value">The nullable value to set at the last index.</param>
        ///<returns>This <see cref="RW"/> instance, allowing for method chaining.</returns>
        public RW Set1(T? value)
        {
            Set(this, Count > 0 ? Count - 1 : 0, value);
            return this;
        }

        ///<summary>
        ///Sets the value of the logical element at the specified <paramref name="index"/>.
        ///If <paramref name="index"/> is greater than or equal to <see cref="Count"/>, the list is extended with _nulls up to <paramref name="index"/> - 1, and the value is placed at <paramref name="index"/>.
        ///</summary>
        ///<param name="index">The zero-based logical index to set.</param>
        ///<param name="value">The nullable value to set at the specified index.</param>
        ///<returns>This <see cref="RW"/> instance, allowing for method chaining.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative.</exception>
        public RW Set1(int index, T? value)
        {
            Set(this, index, value);
            return this;
        }

        ///<summary>
        ///Sets a sequence of values in the list starting at the specified logical <paramref name="index"/>.
        ///The values from the <paramref name="src"/> array are placed at logical indices <paramref name="index"/>, <paramref name="index"/> + 1, ..., <paramref name="index"/> + <paramref name="src.Length"/> - 1.
        ///This operation can extend the list if the range <paramref name="index"/> to <paramref name="index"/> + <paramref name="src.Length"/> - 1 goes beyond the current <see cref="Count"/>.
        ///</summary>
        ///<param name="index">The zero-based logical starting index in the list where the values will be set.</param>
        ///<param name="src">The array of nullable values to set. If null, the method does nothing.</param>
        ///<returns>This <see cref="RW"/> instance, allowing for method chaining.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative.</exception>
        public RW Set(int index, params T?[] src)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            for (var i = src.Length; -1 < --i;)
                Set(this, index + i, src[i]);
            return this;
        }

        ///<summary>
        ///Sets a range of values in the list, copying a slice from a source array.
        ///Elements are copied from <paramref name="src"/> starting at <paramref name="src_index"/> for <paramref name="len"/> elements
        ///and placed into this list starting at logical <paramref name="index"/>.
        ///This operation can extend the list if the target range goes beyond the current <see cref="Count"/>.
        ///</summary>
        ///<param name="index">The zero-based logical starting index in the list where the values will be placed.</param>
        ///<param name="src">The source array containing the values to copy from.</param>
        ///<param name="src_index">The zero-based starting index in the source array.</param>
        ///<param name="len">The number of elements to copy from the source array.</param>
        ///<returns>This <see cref="RW"/> instance, allowing for method chaining.</returns>
        ///<exception cref="ArgumentNullException">Thrown if <paramref name="src"/> is null.</exception>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative, <paramref name="src_index"/> is negative, <paramref name="len"/> is negative, or the slice [<paramref name="src_index"/>, <paramref name="src_index"/> + <paramref name="len"/>) is outside the bounds of the source array.</exception>
        public RW Set(int index, T?[] src, int src_index, int len)
        {
            //NOTE: Does not throw ArgumentOutOfRangeException for invalid ranges, consistent with original but not standard collection behavior.
            ArgumentNullException.ThrowIfNull(src);
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(src_index);
            ArgumentOutOfRangeException.ThrowIfNegative(len);
            if (src_index + len > src.Length)
                throw new ArgumentOutOfRangeException(nameof(len), "Source array range is out of bounds.");

            for (var i = len - 1; -1 < i; --i)
                Set1(index + i, src[src_index + i]);
            return this;
        }

        ///<summary>
        ///Adds a nullable value to the end of the list.
        ///Increases the list's logical <see cref="Count"/> by one.
        ///This operation may trigger internal resizing or strategy switches.
        ///</summary>
        ///<param name="value">The nullable value to append.</param>
        public void Add(T? value) => Set(this, Count, value);

        ///<summary>
        ///Inserts a single nullable value at the specified logical <paramref name="index"/>.
        ///Existing elements at <paramref name="index"/> and later indices are shifted to the right by one.
        ///If <paramref name="index"/> is equal to <see cref="Count"/>, the value is appended. If <paramref name="index"/> is greater than <see cref="Count"/>,
        ///the list is extended with _nulls up to <paramref name="index"/> - 1, and the value is placed at <paramref name="index"/>.
        ///This increases the logical <see cref="Count"/> by one. May trigger internal resizing or strategy switches.
        ///</summary>
        ///<param name="index">The zero-based logical index at which to insert the value.</param>
        ///<param name="value">The nullable value to insert.</param>
        ///<returns>This <see cref="RW"/> instance, allowing for method chaining.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative.</exception>
        public RW Add1(int index, T? value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (Count <= index)
            {
                Set1(index, value); //Extend list via set1 for out-of-bounds
                return this;
            }

            if (isFlatStrategy)
            {
                size_card = BitList.Resize(values, values.Length <= size_card ? new T[Math.Max(16, size_card * 3 / 2)] : values, index, size_card, 1);
                values[index] = value;
                return this;
            }

            if (value == null)
            {
                nulls!.Insert(index, false);
                return this;
            }

            if (values.Length <= size_card && flatStrategyThreshold <= size_card)
            {
                SwitchToFlatStrategy(size_card);
                nulls.Set1(index);
                size_card++;
                values[index] = value;
            }
            else
            {
                var i = nulls!.Rank(index) - 1;
                size_card = BitList.Resize(values, values.Length <= size_card ? values = new T[Math.Max(16, size_card * 3 / 2)] : values, i, size_card, 1);
                nulls.Insert(index, true);
                values[i] = value;
            }

            return this;
        }

        ///<summary>
        ///Appends multiple nullable values to the end of the list.
        ///This is equivalent to calling <see cref="Add(T?)"/> for each element in the provided array.
        ///</summary>
        ///<param name="items">The array of nullable values to append. If null, the method does nothing.</param>
        ///<returns>This <see cref="RW"/> instance, allowing for method chaining.</returns>
        public RW Add(params T?[]? items) => items == null ? this : Set(Count, items);

        ///<summary>
        ///Appends all logical elements from another <see cref="R"/> instance to the end of this list.
        ///This is equivalent to calling <see cref="Add(T?)"/> for each element in the source list.
        ///</summary>
        ///<param name="src">The source list whose elements will be appended. Cannot be null.</param>
        ///<returns>This <see cref="RW"/> instance, allowing for method chaining.</returns>
        ///<exception cref="ArgumentNullException">Thrown if <paramref name="src"/> is null.</exception>
        public RW AddAll(R src)
        {
            for (int i = 0, s = src.Count; i < s; i++)
                Add(src[i]);
            return this;
        }

        ///<summary>
        ///Gets or sets the element at the specified logical index.
        ///The getter behaves as defined in the base class <see cref="R"/>.
        ///Setting a value at an existing index modifies the element. Setting a value at an index
        ///greater than or equal to <see cref="Count"/> extends the list with _nulls up to the index
        ///minus one, and places the value at the index.
        ///Accessing the setter might trigger internal resizing or strategy switches based on the
        ///value being set and the current list state (especially in the compressed strategy).
        ///</summary>
        ///<param name="index">The zero-based logical index of the element to get or set.</param>
        ///<returns>The nullable element at the specified <paramref name="index"/>.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown by the setter if <paramref name="index"/> is negative.</exception>
        public override T? this[int index]
        {
            get => base[index];
            set => Set(this, index, value);
        }

        ///<summary>
        ///Clears all elements from the list, resulting in a list with a logical <see cref="Count"/> of zero.
        ///Resets the list to the initial state of the compressed strategy (empty values array, empty bitlist).
        ///</summary>
        public void Clear()
        {
            if (Count == 0)
                return;
            Array.Clear(values, 0, size_card);

            size_card = 0;
            isFlatStrategy = false;
            if (nulls == null)
                nulls = new BitList.RW(0);
            else
                nulls.Clear();
        }

        ///<summary>
        ///Sets the allocated physical capacity of the internal storage structures.
        ///If the new capacity is less than the current <see cref="Cardinality"/> (in compressed) or <see cref="Count"/> (in flat),
        ///elements beyond the new capacity are lost.
        ///If the new capacity is less than 1, the list is cleared.
        ///</summary>
        ///<param name="capacity">The new allocated capacity for the internal arrays/bitlist.</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is negative.</exception>
        public void Capacity_(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);
            if (capacity < 1)
            {
                size_card = 0;
                nulls = new BitList.RW(0);
                values = [];
                return;
            }

            if (isFlatStrategy)
                size_card = Math.Min(size_card, capacity);
            else
            {
                nulls.Capacity_(capacity);
                size_card = nulls!.Cardinality;
            }

            if (values.Length != capacity)
                Array.Resize(ref values, capacity);
        }

        ///<summary>
        ///Sets the logical size (<see cref="Count"/>) of the list.
        ///If the new count is greater than the current count, the list is extended with null elements.
        ///If the new count is less than the current count, elements beyond the new count are removed.
        ///If the new count is less than 1, the list is cleared.
        ///This operation may trigger internal resizing or strategy switches.
        ///</summary>
        ///<param name="count">The new logical count for the list. Must be non-negative.</param>
        ///<returns>This <see cref="RW"/> instance, allowing for method chaining.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is negative.</exception>
        public RW Count_(int count)
        {
            if (count < 1)
                Clear();

            else if (isFlatStrategy)
            {
                if (values.Length != count)
                    Array.Resize(ref values, count);
                size_card = Math.Min(size_card, count);
            }
            else
            {
                nulls!.Count_(count);
                size_card = nulls.Cardinality;
            }

            return this;
        }

        ///<summary>
        ///Trims the list's allocated capacity to match its current requirements.
        ///If in compressed strategy, it trims the <see cref="values"/> array capacity to exactly fit the <see cref="Cardinality"/>, and the bitlist capacity to fit the <see cref="Count"/>.
        ///If in flat strategy, it trims the <see cref="values"/> array capacity to exactly fit the <see cref="Count"/> and may trigger a switch to compressed strategy if the null density (calculated by counting non-_nulls) falls below the <see cref="flatStrategyThreshold"/>.
        ///</summary>
        ///<returns>This <see cref="RW"/> instance, allowing for method chaining.</returns>
        public RW Fit()
        {
            Capacity_(Count);

            if (!isFlatStrategy)
            {
                if (values.Length != size_card)
                    Array.Resize(ref values, size_card);
                return this;
            }

            if (flatStrategyThreshold < Cardinality)
                return this;
            SwitchToCompressedStrategy();

            return this;
        }

        ///<summary>
        ///Copies the elements of the list to a destination array, starting at a specified array index.
        ///The elements are copied in their logical sequence, including _nulls.
        ///</summary>
        ///<param name="dst">The one-dimensional array that is the destination of the elements copied from the list. Must not be null.</param>
        ///<param name="dstIndex">The zero-based index in the destination array at which copying begins. Must be non-negative.</param>
        ///<exception cref="ArgumentNullException">Thrown when <paramref name="dst"/> is null.</exception>
        ///<exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="dstIndex"/> is less than 0.</exception>
        ///<exception cref="ArgumentException">Thrown when the destination array is not large enough to hold the elements from the list starting from <paramref name="dstIndex"/>.</exception>
        public void CopyTo(T?[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
            if (dst.Length - dstIndex < Count)
                throw new ArgumentException("Destination array is not long enough.");

            if (Count == 0)
                return;

            if (isFlatStrategy)
                Array.Copy(values, 0, dst, dstIndex, Math.Min(size_card, Count));
            else
            {
                //In compressed strategy, reconstruct the logical sequence with _nulls.
                for (int i = 0, index = 0, ii = 0; i < Count; i++)
                {
                    dst[dstIndex + i] = HasValue(i) ? values[ii++] : null;
                }
            }
        }

        ///<summary>
        ///Removes the first occurrence of a specific nullable value from the list.
        ///The comparison uses the equality comparer provided during construction.
        ///If the value is null, the first null element is removed.
        ///Shifts any subsequent elements to the left. Decreases the logical <see cref="Count"/> by one if found.
        ///This operation may trigger internal resizing.
        ///</summary>
        ///<param name="item">The nullable value to remove.</param>
        ///<returns><c>true</c> if <paramref name="item"/> was successfully removed from the list; otherwise, <c>false</c>. This method returns false if <paramref name="item"/> is not found in the list.</returns>
        public bool Remove(T? item)
        {
            var i = IndexOf(item);

            if (i == -1)
                return false;
            Remove(i);
            return true;
        }

        ///<summary>
        ///Gets a value indicating whether the list is read-only.
        ///This implementation is always read-write, so this property always returns <c>false</c>.
        ///</summary>
        public bool IsReadOnly => false;

        ///<summary>
        ///Inserts a nullable value at the specified logical <paramref name="index"/>.
        ///This is equivalent to calling <see cref="Add1(int, T?)"/>.
        ///</summary>
        ///<param name="index">The zero-based logical index at which to insert the value.</param>
        ///<param name="item">The nullable value to insert.</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative.</exception>
        public void Insert(int index, T? item) => Add1(index, item);

        ///<summary>
        ///Removes the logical element at the specified <paramref name="index"/>.
        ///This is equivalent to calling <see cref="Remove(int)"/>.
        ///</summary>
        ///<param name="index">The zero-based logical index of the element to remove.</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or greater than or equal to <see cref="Count"/>.</exception>
        public void RemoveAt(int index) => Remove(index);

        ///<summary>
        ///Swaps the elements at two specified logical indices in the list.
        ///The values at <paramref name="index1"/> and <paramref name="index2"/> are exchanged.
        ///</summary>
        ///<param name="index1">The zero-based logical index of the first element to swap.</param>
        ///<param name="index2">The zero-based logical index of the second element to swap.</param>
        ///<returns>This <see cref="RW"/> instance, allowing for method chaining.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index1"/> or <paramref name="index2"/> is negative or greater than or equal to <see cref="Count"/>.</exception>
        public RW Swap(int index1, int index2)
        {
            if (index1 < 0 || index1 >= Count)
                throw new ArgumentOutOfRangeException(nameof(index1), "Index1 must be non-negative and less than the list's count.");
            if (index2 < 0 || index2 >= Count)
                throw new ArgumentOutOfRangeException(nameof(index2), "Index2 must be non-negative and less than the list's count.");

            var value1 = this[index1];
            var value2 = this[index2];
            if (value1 == value2)
                return this;
            this[index1] = value2;
            this[index2] = value1;

            return this;
        }
    }
}