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
///Defines a contract for a list that stores nullable primitive values (<typeparamref name="T"/> must be a struct).
///<para>
///This interface supports lists of primitives that can contain null values, providing efficient
///methods for adding, removing, and accessing elements while tracking nullity effectively.
///</para>
///</summary>
///<typeparam name="T">The type of primitive values in the list (must be a struct).</typeparam>
public interface NullablePrimitiveList<T>
    where T : struct
{
    ///<summary>
    ///Abstract base class providing core logic for a list of nullable primitive values.
    ///It implements <see cref="IReadOnlyList{T?}"/> and supports two internal storage strategies
    ///(Compressed and Flat) optimized for different null densities.
    ///<para>
    ///It uses a <see cref="BitList.RW"/> to track null values efficiently and an array to store
    ///actual non-null values (Compressed) or all potential values (Flat). The storage strategy
    ///switches dynamically based on the number of non-nulls.
    ///</para>
    ///</summary>
    ///<typeparam name="T">The type of primitive values stored in the list (must be a struct).</typeparam>
    abstract class R : ICloneable, IReadOnlyList<T?>, IEquatable<R>
    {
        ///<summary>
        ///Tracks the presence of non-null values using a <see cref="BitList.RW"/>.
        ///A bit set to <c>true</c> at a logical index indicates a non-null value is present;
        ///a bit set to <c>false</c> indicates the element at that logical index is null.
        ///The <see cref="BitList.RW.Count"/> property represents the logical size of the list.
        ///</summary>
        protected BitList.RW nulls;

        ///<summary>
        ///Array storing the list's values.
        ///<para>
        ///- In <b>Compressed Strategy</b> (<see cref="isFlatStrategy"/> is <c>false</c>), stores only non-null values contiguously.
        ///  The index in this array corresponds to the rank of the logical index in the <c>nulls</c> bitlist.
        ///</para>
        ///<para>
        ///- In <b>Flat Strategy</b> (<see cref="isFlatStrategy"/> is <c>true</c>), stores all elements (including placeholders
        ///  for nulls, typically <c>default(T)</c>). The index in this array is the same as the logical index.
        ///</para>
        ///</summary>
        protected T[] values = [];

        ///<summary>
        ///Number of non-null elements in the list.
        ///In <b>Compressed Strategy</b>, this field explicitly tracks the count and is the effective size
        ///of the used portion of the <see cref="values"/> array.
        ///In <b>Flat Strategy</b>, this field is not actively maintained for count; the number of non-nulls
        ///is derived dynamically from <c>nulls.Cardinality()</c>. This field is primarily relevant
        ///for the Compressed strategy and the strategy switching threshold check.
        ///</summary>
        protected int cardinality = 0;

        ///<summary>
        ///Gets the number of non-null elements currently stored in the list.
        ///</summary>
        ///<value>The count of non-null elements.</value>
        public int Cardinality => isFlatStrategy ? nulls.Cardinality : cardinality;

        ///<summary>
        ///Threshold for switching from <b>Compressed</b> to <b>Flat Strategy</b>.
        ///When the number of non-null elements (<see cref="cardinality"/>) reaches or exceeds this value,
        ///the list considers switching to the Flat strategy for potentially better access performance.
        ///The default value is 1024. Setting this value to 0 or a very high number can effectively
        ///force one strategy (Flat if 0, Compressed if very high).
        ///</summary>
        protected int flatStrategyThreshold = 1024;

        ///<summary>
        ///Indicates the current internal storage strategy.
        ///<c>true</c> signifies <b>Flat Strategy</b>; <c>false</c> signifies <b>Compressed Strategy</b>.
        ///</summary>
        protected bool isFlatStrategy = false;

        ///<summary>
        ///Switches the internal storage strategy to <b>Flat Strategy</b>.
        ///<para>
        ///In Flat Strategy, the <see cref="values"/> array is resized (if needed) to match the logical count,
        ///and non-null values are copied to their corresponding logical indices. Null placeholders
        ///(typically <c>default(T)</c>) fill the gaps. Direct indexing is used for value access.
        ///This method uses the current logical count (<c>_nulls.Count</c>) as the target capacity.
        ///</para>
        ///<para>
        ///Precondition: The <c>_nulls</c> bitlist should accurately reflect the logical state before calling.
        ///</para>
        ///</summary>
        protected void SwitchToFlatStrategy() { SwitchToFlatStrategy(nulls!.Count); }

        ///<summary>
        ///Switches the internal storage strategy to <b>Flat Strategy</b> with a specified minimum capacity.
        ///<para>
        ///The <see cref="values"/> array is resized to at least <paramref name="capacity"/> and
        ///at least the current logical count (<c>_nulls.Count</c>). Non-null values from the
        ///compressed storage are copied to their corresponding logical indices in the new
        ///or resized array.
        ///</para>
        ///</summary>
        ///<param name="capacity">The minimum desired capacity for the internal <see cref="values"/> array after switching.</param>
        protected void SwitchToFlatStrategy(int capacity)
        {
            if (Count == 0) //If the list is logically empty.
            {
                if (values.Length == capacity)
                    values = new T[16]; //Ensure minimum capacity if needed.
                isFlatStrategy = true;
                cardinality = 0; //Flat strategy uses _nulls.Cardinality dynamically, so reset internal counter.
                return;
            }

            var compressed = values;
            values = new T[Math.Max(16, capacity)];
            for (int i = -1, ii = 0; (i = nulls.Next1(i)) != -1;)
                values[i] = compressed[ii++];

            isFlatStrategy = true;
            cardinality = 0; //Flat strategy doesn't use this field for Count of values
        }

        ///<summary>
        ///Switches the internal storage strategy to <b>Compressed Strategy</b>.
        ///<para>
        ///In Compressed Strategy, only non-null values are stored contiguously in the
        ///<see cref="values"/> array. Values are packed to remove gaps, and the array is
        ///resized to match the number of non-null elements (<see cref="cardinality"/>).
        ///Access requires using the rank of the logical index in the <c>_nulls</c> bitlist
        ///to find the corresponding index in the compressed <c>values</c> array.
        ///</para>
        ///</summary>
        protected void SwitchToCompressedStrategy()
        {
            cardinality = nulls.Cardinality;
            var ii = 0;
            for (var i = -1; (i = nulls.Next1(i)) != -1;)
                values[ii++] = values[i]; //packing

            Array.Resize(ref values, ii);
            isFlatStrategy = false;
        }

        ///<summary>
        ///Gets the number of elements in the list (including _nulls).
        ///This corresponds to the size of the <c>_nulls</c> bitlist.
        ///</summary>
        public int Count => nulls.Count;

        ///<summary>
        ///Gets the capacity of the internal <c>values</c> array.
        ///<para>
        ///- In <b>Compressed Strategy</b>, this is the capacity allocated for storing non-null values.
        ///  It is typically equal to or slightly larger than <see cref="cardinality"/>.
        ///</para>
        ///<para>
        ///- In <b>Flat Strategy</b>, this is the capacity allocated for storing values
        ///  at logical indices. It is typically equal to or larger than <see cref="Count"/>.
        ///</para>
        ///</summary>
        public int Capacity => values.Length;

        ///<summary>
        ///Checks if the list is empty (contains zero elements).
        ///</summary>
        ///<returns><c>true</c> if <see cref="Count"/> is less than 1; <c>false</c> otherwise.</returns>
        public bool IsEmpty() => Count < 1;

        ///<summary>
        ///Checks if the element at the specified logical index is non-null.
        ///</summary>
        ///<param name="index">The zero-based logical index to check.</param>
        ///<returns><c>true</c> if the element at <paramref name="index"/> is non-null; <c>false</c> if it is null.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or greater than or equal to <see cref="Count"/>.</exception>
        public bool HasValue(int index) => nulls.Get(index);

        ///<summary>
        ///Finds the logical index of the next non-null value after the specified index.
        ///</summary>
        ///<param name="index">The index to start the search from. Pass -1 to start from the beginning.</param>
        ///<returns>The logical index of the next non-null value, or -1 if no non-null value is found after <paramref name="index"/>.</returns>
        public int NextValueIndex(int index) => nulls.Next1(index);

        ///<summary>
        ///Finds the logical index of the previous non-null value before the specified index.
        ///</summary>
        ///<param name="index">The index to start the search from. Pass -1 or >= <see cref="Count"/>, to start searches from the end.</param>
        ///<returns>The logical index of the previous non-null value, or -1 if no non-null value is found before <paramref name="index"/>.</returns>
        public int PrevValueIndex(int index) => nulls.Prev1(index);

        ///<summary>
        ///Finds the logical index of the next null value after the specified index.
        ///</summary>
        ///<param name="index">The index to start the search from. Pass -1 to start from the beginning.</param>
        ///<returns>The logical index of the next null value, or -1 if no null value is found at or after <paramref name="index"/>.</returns>
        public int NextNullIndex(int index) => nulls.Next0(index);

        ///<summary>
        ///Finds the logical index of the previous null value before the specified index.
        ///</summary>
        ///<param name="index">The index to start the search from. Pass -1 or >= <see cref="Count"/>, to start searches from the end.</param>
        ///<returns>The logical index of the previous null value, or -1 if no null value is found before <paramref name="index"/>.</returns>
        public int PrevNullIndex(int index) => nulls.Prev0(index);

        ///<summary>
        ///Gets the nullable value at the specified logical index.
        ///Returns null if the element at the index is null.
        ///This property is read-only in the base <see cref="R"/> class.
        ///</summary>
        ///<param name="index">The zero-based logical index of the element to get.</param>
        ///<returns>The nullable value at the specified index.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or greater than or equal to <see cref="Count"/>.</exception>
        ///<exception cref="NotSupportedException">Thrown when attempting to set a value, as the base <see cref="R"/> class is read-only.</exception>
        public virtual T? this[int index]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfNegative(index);
                if (index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index), "Index is out of bounds.");
                return HasValue(index) ? isFlatStrategy ? values[index] : values[nulls.Rank(index) - 1] : null;
            }
            set => throw new NotSupportedException("Setting values is not supported in R.");
        }

        ///<summary>
        ///Finds and returns a reference to the underlying primitive value at the specified logical index.
        ///This method is intended for internal use by derived classes (like <see cref="RW"/>)
        ///when direct modification of an *existing non-null* value is needed.
        ///</summary>
        ///<param name="index">The zero-based logical index of the value.</param>
        ///<returns>A reference to the primitive value in the internal array, or a null reference if the element at the index is null or the index is out of bounds.</returns>
        ///<remarks>
        ///The caller MUST check <see cref="HasValue(int)"/> before calling this method
        ///and ensure the index is within bounds (<0 and < Count). Accessing a null
        ///or out-of-bounds index via the returned reference will lead to undefined behavior
        ///(likely an AccessViolationException or similar).
        ///</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal ref T FindValue(int index)
        {
            if (!HasValue(index))
                return ref Unsafe.NullRef<T>();
            if (isFlatStrategy)
                return ref values[index];
            return ref values[nulls.Rank(index) - 1];
        }

        ///<summary>
        ///Finds the first occurrence of the specified nullable value in the list.
        ///</summary>
        ///<param name="value">The nullable value to locate. Can be null.</param>
        ///<returns>
        ///The zero-based logical index of the first occurrence of the value if found;
        ///otherwise, -1. If <paramref name="value"/> is null, returns the index of the
        ///first null element, or -1 if no _nulls are present.
        ///</returns>
        public int IndexOf(T? value)
        {
            if (value == null)
                return NextNullIndex(-1);
            var val = value.Value;
            if (isFlatStrategy)
            {
                for (var i = -1; (i = nulls.Next1(i)) != -1;)
                    if (EqualityComparer<T>.Default.Equals(values[i], val))
                        return i;
                return -1;
            }

            var ii = Array.IndexOf(values, val, 0, cardinality);
            return ii < 0 ? -1 : nulls.Bit(ii + 1);
        }

        ///<summary>
        ///Determines whether a non-null value is in the list.
        ///</summary>
        ///<param name="value">The non-null value to locate.</param>
        ///<returns><c>true</c> if <paramref name="value"/> is found in the list; otherwise, <c>false</c>.</returns>
        public bool Contains(T value) => IndexOf(value) != -1;

        ///<summary>
        ///Determines whether a nullable value (which can be null) is in the list.
        ///</summary>
        ///<param name="value">The nullable value to locate. Can be null.</param>
        ///<returns>
        ///<c>true</c> if <paramref name="value"/> is found in the list (comparing value equality for non-_nulls,
        ///and checking for the presence of any null element if <paramref name="value"/> is null);
        ///otherwise, <c>false</c>.
        ///</returns>
        public bool Contains(T? value) => value == null ? NextNullIndex(-1) != -1 : Contains(value.Value);

        ///<summary>
        ///Finds the last occurrence of the specified nullable value in the list.
        ///</summary>
        ///<param name="value">The nullable value to locate. Can be null.</param>
        ///<returns>
        ///The zero-based logical index of the last occurrence of the value if found;
        ///otherwise, -1. If <paramref name="value"/> is null, returns the index of the
        ///last null element, or -1 if no _nulls are present.
        ///</returns>
        public int LastIndexOf(T? value)
        {
            if (value == null)
                return PrevNullIndex(Count);

            if (isFlatStrategy)
            {
                for (var i = -1; (i = nulls.Prev1(i)) != -1;)
                    if (EqualityComparer<T>.Default.Equals(values[i], value.Value))
                        return i;
                return -1;
            }

            var ii = Array.LastIndexOf(values, value, cardinality - 1);
            return ii < 0 ? -1 : nulls.Bit(ii + 1);
        }

        ///<summary>
        ///Determines whether the specified object is equal to the current list based on logical content.
        ///Equality is determined by comparing the count, null patterns, and non-null values.
        ///</summary>
        ///<param name="obj">The object to compare with the current list.</param>
        ///<returns><c>true</c> if the specified object is equal to the current list; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj) => obj is R other && Equals(other);

        ///<summary>
        ///Computes a hash code for the current list based on its logical content.
        ///The hash code is consistent regardless of the internal storage strategy (Compressed or Flat).
        ///</summary>
        ///<returns>A hash code for the current list.</returns>
        public override int GetHashCode()
        {
            var hash = 131311;

            for (int i = 0, s = nulls.Count, ii = 0; i < s; i++)
                hash = HashCode.Combine(hash, nulls![i] ? EqualityComparer<T>.Default.GetHashCode(values[isFlatStrategy ? i : ii++]) : 300463);
            return HashCode.Combine(hash, Count);
        }

        ///<summary>
        ///Performs a detailed equality check between this list and another <c>R</c> instance
        ///based on their logical content (count, null pattern, and values at non-null positions).
        ///The comparison is strategy-aware to efficiently compare lists regardless of their
        ///internal representation.
        ///</summary>
        ///<param name="other">The other <c>R</c> instance to compare with.</param>
        ///<returns><c>true</c> if the lists are logically equal; otherwise, <c>false</c>.</returns>
        public bool Equals(R? other)
        {
            if (other == this)
                return true;
            if (other == null || Count != other.Count || !nulls.Equals(other.nulls))
                return false;
            if (isFlatStrategy)
            {
                if (other.isFlatStrategy)
                {
                    for (var i = -1; (i = nulls.Next1(i)) != -1;)
                        if (!EqualityComparer<T>.Default.Equals(values[i], other.values[i]))
                            return false;
                }
                else
                    for (int i = -1, ii = 0; (i = nulls.Next1(i)) != -1;)
                        if (!EqualityComparer<T>.Default.Equals(values[i], other.values[ii++]))
                            return false;
            }
            else if (other.isFlatStrategy)
            {
                for (int i = -1, ii = 0; (i = nulls.Next1(i)) != -1;)
                    if (!EqualityComparer<T>.Default.Equals(values[ii], other.values[i]))
                        return false;
            }
            else
                for (var i = 0; i < cardinality; i++)
                    if (!EqualityComparer<T>.Default.Equals(values[i], other.values[i]))
                        return false;

            return true;
        }

        ///<summary>
        ///Creates a deep copy of the current list instance.
        ///The internal storage strategy and content are duplicated.
        ///</summary>
        ///<returns>A new <c>R</c> instance that is a deep copy of this list.</returns>
        public object Clone()
        {
            var dst = (R)MemberwiseClone();
            dst.values = (T[])values.Clone();
            dst.nulls = nulls.Clone();
            return dst;
        }

        ///<summary>
        ///Returns a JSON string representation of the list's logical content.
        ///Null values are represented as "null", non-null values by their string representation.
        ///</summary>
        ///<returns>A JSON-formatted string representing the list.</returns>
        public override string ToString() => ToJSON(new StringBuilder()).ToString();

        ///<summary>
        ///Appends the list's logical content in JSON format to a <see cref="StringBuilder"/>.
        ///Nulls are represented as "null"; non-_nulls as their string values.
        ///</summary>
        ///<param name="sb">The <see cref="StringBuilder"/> to append to.</param>
        ///<returns>The updated <see cref="StringBuilder"/> instance.</returns>
        public StringBuilder ToJSON(StringBuilder sb)
        {
            sb.Append('[');
            var size = Count;
            if (size > 0)
            {
                sb.EnsureCapacity(sb.Length + size * 8); //Pre-allocate for efficiency
                for (int i = -1, ii = -1; i < size;)
                {
                    if ((i = NextValueIndex(i)) == -1)
                    { //No more non-null values or reached end of list.
                        while (++ii < size)
                        {
                            sb.Append("null");
                            if (ii < size)
                                sb.Append(',');
                        }

                        break;
                    }

                    while (++ii < i)
                    {
                        sb.Append("null");
                        if (ii < size)
                            sb.Append(',');
                    }

                    sb.Append(this[i]);
                    if (i < size)
                        sb.Append(',');
                }
            }

            sb.Append(']');
            return sb;
        }

        ///<summary>
        ///Static helper method to set a value at a specific logical index in a target list instance (typically an <see cref="RW"/>).
        ///This method encapsulates the core logic for writing, handling internal storage strategies,
        ///resizing of the underlying value array, updates to the null tracking bitlist,
        ///cardinality updates, and potential strategy switches.
        ///<para>
        ///If <paramref name="index"/> is greater than or equal to the current logical count (<see cref="Count"/>),
        ///the list is extended to include elements up to <paramref name="index"/>, filling any gaps
        ///with _nulls before setting the value at <paramref name="index"/>.
        ///</para>
        ///</summary>
        ///<param name="dst">The target <see cref="R"/> instance (expected to be an <see cref="RW"/> instance) to modify.</param>
        ///<param name="index">The zero-based logical index at which to set the value.</param>
        ///<param name="value">The nullable value to set at the specified index.</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative.</exception>
        protected static void Set(R dst, int index, T? value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (value == null)
            {
                if (dst.isFlatStrategy)
                {
                    if (dst.values.Length <= dst.nulls.Count)
                        Array.Resize(ref dst.values, Math.Max(index + 1, dst.values.Length * 3 / 2 + 1));
                }
                else if (dst.nulls.Get(index))
                    dst.cardinality = BitList.Resize(dst.values, dst.values, dst.nulls.Rank(index) - 1, dst.cardinality, -1);

                dst.nulls.Set0(index);
            }
            else if (dst.isFlatStrategy)
            {
                if (dst.values.Length <= index)
                    Array.Resize(ref dst.values, Math.Max(index + 1, dst.values.Length * 3 / 2));

                dst.values[index] = value.Value;
                dst.nulls.Set1(index);
            }
            else if (dst.nulls.Get(index))
                dst.values[dst.nulls.Rank(index) - 1] = value.Value;
            else
            {
                if (dst.values.Length <= dst.cardinality && dst.flatStrategyThreshold <= dst.cardinality)
                {
                    dst.SwitchToFlatStrategy(Math.Max(index + 1, dst.nulls.Count * 3 / 2));
                    dst.values[index] = value.Value;
                }
                else
                {
                    var rank = dst.nulls.Rank(index);

                    dst.cardinality = BitList.Resize(dst.values,
                                                     dst.values.Length <= dst.cardinality ? dst.values = new T[dst.cardinality * 3 / 2 + 2] : dst.values, rank, dst.cardinality, 1);
                    dst.values[rank] = value.Value;
                }

                dst.nulls.Set1(index);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T?> GetEnumerator() => new Enumerator(this);

        ///<summary>
        ///Provides a struct-based enumerator for iterating over the nullable elements of the list.
        ///</summary>
        public struct Enumerator : IEnumerator<T?>
        {
            private readonly R _list;
            private int _index;

            ///<summary>
            ///Initializes a new instance of the Enumerator struct.
            ///</summary>
            ///<param name="list">The list to enumerate.</param>
            internal Enumerator(R list)
            {
                _list = list ?? throw new ArgumentNullException(nameof(list));
                _index = -1;
            }

            public T? Current => -1 < _index && _index < _list.Count ? _list[_index] : null;

            object? IEnumerator.Current => Current;
            public bool MoveNext() => ++_index < _list.Count;
            public void Reset() => _index = -1;
            public void Dispose() { }
        }
    }

    ///<summary>
    ///Read-write implementation of <see cref="NullablePrimitiveList2{T}"/>, extending the core logic from <see cref="R"/>.
    ///<para>
    ///Provides comprehensive methods to modify the list, including adding, removing, setting,
    ///inserting, and managing capacity and size, while correctly handling null values and
    ///dynamically switching between Compressed and Flat storage strategies.
    ///</para>
    ///</summary>
    class RW : R, IList<T?>
    {
        ///<summary>
        ///Initializes a new instance of the <see cref="RW"/> list with a specified initial capacity and initial size.
        ///</summary>
        ///<param name="items">
        ///The initial capacity. This determines the number of elements the list can hold without resizing.
        ///</param>
        ///<remarks>
        ///If <paramref name="items"/> is positive, the list is initialized with that capacity and a logical Count of 0 (empty).
        ///<para/>
        ///If <paramref name="items"/> is negative, the list is initialized with a capacity and a logical size equal to the absolute value of <paramref name="items"/> (<c>-items</c>).
        ///In this case, the list will contain <c>-items</c> elements, all initially set to <c>null</c>.
        ///</remarks>

        public RW(int items) => nulls = -1 < items ? new BitList.RW(items) : //items is capacity
                                            new BitList.RW(false, -items);   //-items is capacity and Count of null items

        ///<summary>
        ///Sets the threshold for switching between Compressed and Flat strategies.
        ///<para>
        ///If the current number of non-_nulls (<see cref="cardinality"/>) warrants a strategy switch
        ///based on the new threshold (e.g., cardinality bigger new threshold when compressed, or
        ///cardinality  less new threshold when flat), the switch is performed immediately.
        ///Setting a threshold of 0 effectively forces the list into Flat strategy as soon as
        ///any non-null element is added or set. Setting a very high threshold (e.g., <see cref="int.MaxValue"/>)
        ///can prevent switching to Flat strategy.
        ///</para>
        ///</summary>
        ///<param name="threshold">The new non-negative threshold value for switching to Flat strategy.</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="threshold"/> is negative.</exception>
        public void FlatStrategyThreshold(int threshold)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(flatStrategyThreshold = threshold);
            if (threshold <= cardinality && !isFlatStrategy)
                SwitchToFlatStrategy();
            else if (threshold > cardinality && isFlatStrategy)
                SwitchToCompressedStrategy();
        }

        ///<summary>
        ///Creates a deep copy of the current <see cref="RW"/> instance.
        ///</summary>
        ///<returns>A new <c>RW</c> instance that is a deep copy of this list.</returns>
        public new RW Clone() => (RW)base.Clone();

        ///<summary>
        ///Removes the last element from the list, reducing the logical count by one.
        ///If the list is empty, this method does nothing.
        ///</summary>
        ///<returns>This instance for chaining.</returns>
        public RW Remove() => Count == 0 ? this : Remove(Count - 1);

        ///<summary>
        ///Removes the element at the specified logical index, reducing the logical count by one.
        ///Subsequent elements are shifted to fill the gap.
        ///</summary>
        ///<param name="index">The zero-based logical index of the element to remove.</param>
        ///<returns>This instance for chaining.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or greater than or equal to <see cref="Count"/>.</exception>
        public RW Remove(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            if (isFlatStrategy)
                BitList.Resize(values, values, index, nulls.count, -1);
            else if (nulls.Get(index))
                cardinality = BitList.Resize(values, values, nulls.Rank(index) - 1, cardinality, -1);

            nulls.RemoveAt(index);

            return this;
        }

        ///<summary>
        ///Sets a nullable value at the last logical index (<see cref="Count"/> - 1).
        ///If the list is empty (<see cref="Count"/> is 0), this sets the value at logical index 0,
        ///effectively adding the first element.
        ///</summary>
        ///<param name="value">The nullable value to set (can be null).</param>
        ///<returns>This instance for chaining.</returns>
        public RW Set(T? value) => Set(Math.Max(0, Count - 1), value);

        ///<summary>
        ///Sets a primitive value at the last logical index (<see cref="Count"/> - 1).
        ///If the list is empty (<see cref="Count"/> is 0), this sets the value at logical index 0,
        ///effectively adding the first element.
        ///</summary>
        ///<param name="value">The primitive value to set.</param>
        ///<returns>This instance for chaining.</returns>
        public RW Set(T value) => Set(Math.Max(0, Count - 1), value);

        ///<summary>
        ///Sets a nullable value at the specified logical index.
        ///If <paramref name="index"/> is greater than or equal to the current logical count (<see cref="Count"/>),
        ///the list is extended to include elements up to <paramref name="index"/>, filling any gaps
        ///with _nulls before setting the value at <paramref name="index"/>.
        ///<para>
        ///This method utilizes the static <see cref="R.Set(R, int, T?)"/> helper to perform the operation,
        ///which handles strategy management, resizing, and null tracking.
        ///</para>
        ///</summary>
        ///<param name="index">The zero-based logical index at which to set the value.</param>
        ///<param name="value">The nullable value to set (can be null).</param>
        ///<returns>This instance for chaining.</returns>
        public RW Set(int index, T? value)
        {
            Set(this, index, value);
            return this;
        }

        ///<summary>
        ///Sets a primitive value at the specified logical index.
        ///If <paramref name="index"/> is greater than or equal to the current logical count (<see cref="Count"/>),
        ///the list is extended and elements up to <paramref name="index"/> are filled with _nulls before setting
        ///the specified non-null value at <paramref name="index"/>.
        ///<para>
        ///This method delegates to <see cref="Set(int, T?)"/>.
        ///</para>
        ///</summary>
        ///<param name="index">The zero-based logical index at which to set the value.</param>
        ///<param name="value">The primitive value to set.</param>
        ///<returns>This instance for chaining.</returns>
        public RW Set(int index, T value)
        {
            Set(this, index, value);
            return this;
        }

        ///<summary>
        ///Sets multiple nullable values starting at the specified logical index.
        ///If the range <c>[index, index + values.Length - 1]</c> extends beyond the current
        ///logical count, the list is extended, and any gaps between the original count
        ///and <paramref name="index"/> (if <paramref name="index"/> is beyond the original count)
        ///are filled with _nulls before setting the provided values.
        ///<para>
        ///Each value is set individually using <see cref="Set(int, T?)"/>.
        ///</para>
        ///</summary>
        ///<param name="index">The zero-based logical index to start setting values.</param>
        ///<param name="values">An array of nullable values to set. Can be null or empty.</param>
        ///<returns>This instance for chaining.</returns>
        public RW Set(int index, params T?[]? values)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (values == null)
                return this;
            for (var i = 0; i < values.Length; i++)
                Set(this, index + i, values[i]);
            return this;
        }

        ///<summary>
        ///Sets multiple primitive values starting at the specified logical index.
        ///If the range <c>[index, index + src.Length - 1]</c> extends beyond the current
        ///logical count, the list is extended, and any gaps between the original count
        ///and <paramref name="index"/> (if <paramref name="index"/> is beyond the original count)
        ///are filled with _nulls before setting the provided non-null values.
        ///<para>
        ///This method delegates to <see cref="Set(int, T[], int, int)"/>.
        ///</para>
        ///</summary>
        ///<param name="index">The zero-based logical index to start setting values.</param>
        ///<param name="src">An array of primitive values to set. Cannot be null.</param>
        ///<returns>This instance for chaining.</returns>
        public RW Set(int index, params T[] src) => Set(index, src, 0, src.Length);

        ///<summary>
        ///Sets a range of primitive values in the list, starting at a specified logical index,
        ///from a source primitive array.
        ///If the destination range <c>[index, index + len - 1]</c> extends beyond the current
        ///logical count, the list is extended, and any gaps between the original count
        ///and <paramref name="index"/> (if <paramref name="index"/> is beyond the original count)
        ///are filled with _nulls before setting the provided non-null values.
        ///<para>
        ///Each value is set individually using <see cref="Set(int, T?)"/>.
        ///</para>
        ///</summary>
        ///<param name="index">The zero-based logical index in this list to start setting values.</param>
        ///<param name="src">The source array of primitive values. Cannot be null.</param>
        ///<param name="srcIndex">The zero-based index in the source array to start reading values from.</param>
        ///<param name="len">The number of elements to set from the source array.</param>
        ///<returns>This instance for chaining.</returns>
        public RW Set(int index, T[] src, int srcIndex, int len)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(srcIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(len);
            ArgumentNullException.ThrowIfNull(src);
            if (srcIndex + len > src.Length)
                throw new ArgumentOutOfRangeException(nameof(len), "Source range exceeds array bounds.");

            for (var i = len; -1 < --i;)
                Set(this, index + i, src[srcIndex + i]);
            return this;
        }

        ///<summary>
        ///Sets a range of nullable values in the list, starting at a specified logical index,
        ///from a source nullable primitive array.
        ///If the destination range <c>[index, index + len - 1]</c> extends beyond the current
        ///logical count, the list is extended, and any gaps between the original count
        ///and <paramref name="index"/> (if <paramref name="index"/> is beyond the original count)
        ///are filled with _nulls before setting the provided values.
        ///<para>
        ///Each value is set individually using <see cref="Set(int, T?)"/>.
        ///</para>
        ///</summary>
        ///<param name="index">The zero-based logical index in this list to start setting values.</param>
        ///<param name="src">The source array of nullable primitive values. Cannot be null.</param>
        ///<param name="srcIndex">The zero-based index in the source array to start reading values from.</param>
        ///<param name="len">The number of elements to set from the source array.</param>
        ///<returns>This instance for chaining.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/>, <paramref name="srcIndex"/>, or <paramref name="len"/> is negative, or if the source range (<c>srcIndex + len</c>) exceeds the source array bounds.</exception>
        ///<exception cref="ArgumentNullException">Thrown if <paramref name="src"/> is null.</exception>
        public RW Set(int index, T?[] src, int srcIndex, int len)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(srcIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(len);
            ArgumentNullException.ThrowIfNull(src);
            if (srcIndex + len > src.Length)
                throw new ArgumentOutOfRangeException(nameof(len), "Source range exceeds array bounds.");

            for (var i = len; -1 < --i;)
                Set(this, index + i, src[srcIndex + i]);
            return this;
        }

        ///<summary>
        ///Adds a nullable value to the end of the list, increasing the logical count by one.
        ///This is equivalent to calling <c>Set(Count, value)</c>.
        ///</summary>
        ///<param name="value">The nullable value to add (can be null).</param>
        ///<returns>This instance for chaining.</returns>
        public void Add(T? value) => Set(Count, value);

        ///<summary>
        ///Adds a primitive value to the end of the list, increasing the logical count by one.
        ///This is equivalent to calling <c>Set(Count, value)</c>.
        ///</summary>
        ///<param name="value">The primitive value to add.</param>
        ///<returns>This instance for chaining.</returns>
        public RW Add(T value) => Set(Count, value);

        ///<summary>
        ///Adds a nullable value at the specified logical index, inserting it and shifting subsequent elements to the right.
        ///If <paramref name="index"/> is greater than or equal to the current logical count (<see cref="Count"/>),
        ///this is equivalent to calling <c>Add(value)</c> or <c>Set(index, value)</c>, extending the list
        ///and filling any intermediate indices with _nulls.
        ///</summary>
        ///<param name="index">The zero-based logical index at which to insert the value.</param>
        ///<param name="value">The nullable value to add (can be null).</param>
        ///<returns>This instance for chaining.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative.</exception>
        public RW Add(int index, T? value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (Count <= index)
            {
                Set(index, value); //Extend list via set1 for out-of-bounds
                return this;
            }

            if (isFlatStrategy)
            {
                BitList.Resize(values, values.Length <= nulls.Count ? new T[Math.Max(16, nulls.Count * 3 / 2)] : values, index, nulls.Count, 1);
                if (value == null)
                    nulls.Insert(index, false);
                else
                {
                    nulls.Insert(index, true);
                    values[index] = value.Value;
                }

                return this;
            }

            if (value == null)
            {
                nulls!.Insert(index, false);
                return this;
            }

            if (values.Length <= cardinality && flatStrategyThreshold <= cardinality)
            {
                SwitchToFlatStrategy(cardinality);
                nulls.Set1(index);
                cardinality++;
                values[index] = value.Value;
            }
            else
            {
                var i = nulls!.Rank(index);
                if (index <= nulls.Last1)
                    i--;
                cardinality = BitList.Resize(values, values.Length <= cardinality ? values = new T[Math.Max(16, cardinality * 3 / 2)] : values, i, cardinality, 1);
                nulls.Insert(index, true);
                values[i] = value.Value;
            }

            return this;
        }

        ///<summary>
        ///Adds multiple primitive values to the end of the list.
        ///This is equivalent to calling <c>Set(Count, items)</c>.
        ///</summary>
        ///<param name="items">An array of primitive values to add. Cannot be null.</param>
        ///<returns>This instance for chaining.</returns>
        public RW Add(params T[] items) => Set(Count, items);

        ///<summary>
        ///Adds multiple nullable values to the end of the list.
        ///This is equivalent to calling <c>Set(Count, items)</c>.
        ///</summary>
        ///<param name="items">An array of nullable values to add. Can be null or empty.</param>
        ///<returns>This instance for chaining.</returns>
        public RW Add(params T?[] items) => Set(Count, items);

        ///<summary>
        ///Adds all elements from another <c>R</c> list to the end of this list.
        ///The elements are copied using their logical values and nullity.
        ///</summary>
        ///<param name="src">The source <c>R</c> list from which to add elements. Cannot be null.</param>
        ///<returns>This instance for chaining.</returns>
        ///<exception cref="ArgumentNullException">Thrown if <paramref name="src"/> is null.</exception>
        public RW AddAll(R src)
        {
            ArgumentNullException.ThrowIfNull(src);
            for (int i = 0, s = src.Count; i < s; i++)
                Add(src[i]);
            return this;
        }

        ///<summary>
        ///Clears all elements from the list, resulting in an empty list with a logical count of 0.
        ///The internal values array is set to an empty array, and cardinality is reset to 0.
        ///The strategy reverts to Compressed.
        ///</summary>
        ///<returns>This instance for chaining.</returns>
        public RW Clear()
        {
            cardinality = 0;
            nulls.Clear();
            values = [];
            isFlatStrategy = false;
            return this;
        }

        ///<summary>
        ///Sets the capacity of the internal values array.
        ///If the new capacity is less than the current logical count (<see cref="Count"/>),
        ///the list is logically truncated first to match the new capacity.
        ///If the new capacity is less than the current number of non-_nulls (<see cref="cardinality"/>)
        ///when in Compressed strategy, this method's behavior is undefined or may throw.
        ///The method attempts to manage the strategy based on the new capacity and cardinality.
        ///</summary>
        ///<param name="capacity">The new capacity for the internal values array. Must be non-negative.</param>
        ///<returns>This instance for chaining.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is negative.</exception>
        public RW Capacity_(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);
            var shrink = capacity < nulls.Count;
            nulls.Capacity_(capacity);

            if (capacity < 1)
            {
                cardinality = 0;
                values = [];
                return this;
            }

            if (isFlatStrategy)
            {
                if (shrink && nulls.Cardinality <= flatStrategyThreshold)
                    SwitchToCompressedStrategy();
                else if (values.Length != capacity)
                    Array.Resize(ref values, capacity);
            }
            else
            {
                if (shrink)
                    cardinality = nulls.Cardinality;
                if (flatStrategyThreshold <= cardinality)
                    SwitchToFlatStrategy(capacity);
                else if (values.Length != capacity)
                    Array.Resize(ref values, capacity);
            }

            return this;
        }

        ///<summary>
        ///Sets the logical size of the list.
        ///If <paramref name="newCount"/> is less than the current <see cref="Count"/>, the list is truncated.
        ///If <paramref name="newCount"/> is greater than the current <see cref="Count"/>, the list is extended.
        ///</summary>
        ///<param name="newCount">The new logical size of the list. Must be non-negative.</param>
        ///<returns>This instance for chaining.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="newCount"/> is negative.</exception>
        public RW Count_(int newCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(newCount);
            if (newCount < 1)
            {
                Clear();
                return this;
            }

            if (Count < newCount) Set(newCount - 1, (T?)null); //expand
            else if (!isFlatStrategy)
            {
                nulls.Count_(newCount);
                cardinality = nulls.Cardinality;
            }

            return this;
        }

        ///<summary>
        ///Trims excess capacity in the internal values array to match the current logical size (<see cref="Count"/>).
        ///If the current strategy is Flat and the cardinality (<see cref="nulls.Cardinality"/>) is
        ///below the <see cref="flatStrategyThreshold"/> after trimming, the strategy is switched to Compressed.
        ///</summary>
        ///<returns>This instance for chaining.</returns>
        public RW Fit()
        {
            Capacity_(Count);

            if (!isFlatStrategy || flatStrategyThreshold < nulls.Cardinality)
                return this;

            SwitchToCompressedStrategy();

            return this;
        }

        ///<summary>
        ///Trims all trailing null elements from the list, effectively resizing the list
        ///to the logical index of the last non-null element plus one. If the list
        ///contains only _nulls, it is cleared.
        ///If the strategy was Flat and the cardinality drops below the threshold after trimming,
        ///the strategy is switched to Compressed.
        ///</summary>
        ///<returns>This instance for chaining.</returns>
        public RW Trim()
        {
            Capacity_(nulls.Last1 + 1);
            if (isFlatStrategy && cardinality < flatStrategyThreshold)
                SwitchToCompressedStrategy();
            return this;
        }

        ///<summary>
        ///Swaps the elements at two specified logical indices.
        ///Handles elements being null or non-null correctly, irrespective of the internal storage strategy.
        ///</summary>
        ///<param name="index1">The zero-based logical index of the first element to swap.</param>
        ///<param name="index2">The zero-based logical index of the second element to swap.</param>
        ///<returns>This instance for chaining.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index1"/> or <paramref name="index2"/> is negative or greater than or equal to <see cref="Count"/>.</exception>
        public RW Swap(int index1, int index2)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index1);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index1, Count);
            ArgumentOutOfRangeException.ThrowIfNegative(index2);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index2, Count);

            if (index1 == index2)
                return this;

            var val1 = this[index1]; //Get logical value (handles null, strategy)
            var val2 = this[index2]; //Get logical value
            if (val1 == null && val2 == null || val1 != null && val2 != null && EqualityComparer<T>.Default.Equals(val1!.Value, val2!.Value))
                return this;
            //Use the standard Set method, which handles _nulls, strategy switching,
            //array resizing, and cardinality updates internally.
            Set(this, index1, val2); //Set index1 to val2
            Set(this, index2, val1); //Set index2 to val1

            return this;
        }

        ///<summary>
        ///Gets or sets the nullable value at the specified logical <paramref name="index"/>.
        ///<para>
        ///Getting a value at an index less than <see cref="Count"/> returns the element's value (or null).
        ///Getting a value at an index greater than or equal to <see cref="Count"/> throws <see cref="ArgumentOutOfRangeException"/>.
        ///</para>
        ///<para>
        ///Setting a value at an index less than <see cref="Count"/> updates the existing element.
        ///Setting a value at an index greater than or equal to <see cref="Count"/> extends the list
        ///to include elements up to <paramref name="index"/>, filling any intermediate indices with nulls
        ///before setting the value at <paramref name="index"/>.
        ///</para>
        ///</summary>
        ///<param name="index">The zero-based logical index of the element to get or set.</param>
        ///<returns>The nullable value at the specified <paramref name="index"/>.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown when getting a value if <paramref name="index"/> is negative or greater than or equal to <see cref="Count"/>. Thrown when setting a value if <paramref name="index"/> is negative.</exception>
        public override T? this[int index]
        {
            get => base[index];
            set => Set(this, index, value);
        }

        void ICollection<T?>.Clear() => Clear();

        ///<summary>
        ///Checks if the list contains the specified nullable value.
        ///</summary>
        ///<param name="item">The nullable value to check for.</param>
        ///<returns>true if the list contains the <paramref name="item"/>, false otherwise.</returns>
        public bool Contains(T? item) => base.Contains(item);

        ///<summary>
        ///Copies the entire <see cref="RW"/> to a compatible one-dimensional array, starting at the specified index of the target array.
        ///This is explicitly implemented for <see cref="ICollection{T?}"/>.
        ///</summary>
        ///<param name="dst">The one-dimensional array that is the destination of the elements copied from the list. The array must have zero-based indexing.</param>
        ///<param name="dstIndex">The zero-based index in <paramref name="dst"/> at which copying begins.</param>
        ///<exception cref="ArgumentNullException">Thrown if <paramref name="dst"/> is null.</exception>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="dstIndex"/> is less than 0.</exception>
        ///<exception cref="ArgumentException">Thrown if the number of elements in the source list is greater than the available space from <paramref name="dstIndex"/> to the end of the destination array.</exception>
        public void CopyTo(T?[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfNegative(dstIndex);
            if (Count > dst.Length - dstIndex)
                throw new ArgumentException("Destination array is not long enough to copy all items.", nameof(dst));

            for (var i = 0; i < Count; i++)
                dst[dstIndex + i] = this[i];
        }

        ///<summary>
        ///Removes the first occurrence of a specific nullable value from the <see cref="RW"/>.
        ///This is explicitly implemented for <see cref="ICollection{T?}"/> and delegates to <see cref="Remove(int)"/> after finding the index.
        ///</summary>
        ///<param name="item">The nullable value to remove from the list.</param>
        ///<returns><c>true</c> if <paramref name="item"/> was successfully removed from the list; otherwise, <c>false</c>. This method returns <c>false</c> if <paramref name="item"/> is not found in the original list.</returns>
        public bool Remove(T? item)
        {
            var i = IndexOf(item); //Use IndexOf(T? item) from base class
            if (i < 0)
                return false;
            RemoveAt(i);
            return true;
        }

        ///<summary>
        ///Gets a value indicating whether the <see cref="RW"/> is read-only.
        ///This implementation is always false, as <see cref="RW"/> provides modification methods.
        ///</summary>
        public bool IsReadOnly => false;

        ///<summary>
        ///Inserts a nullable value at the specified <paramref name="index"/>.
        ///</summary>
        ///<param name="index">The index at which to insert the value.</param>
        ///<param name="item">The nullable value to insert.</param>
        public void Insert(int index, T? item) => Add(index, item);

        ///<summary>
        ///Removes the element at the specified <paramref name="index"/>.
        ///</summary>
        ///<param name="index">The index of the element to remove.</param>
        public void RemoveAt(int index) => Remove(index);
    }
}