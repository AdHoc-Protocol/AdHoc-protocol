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
///Defines a generic interface for a list of primitive types, constrained to value types (structs).
///Allows specifying a defaultValue for filling gaps when resizing or setting indices beyond the current count.
///defaultValue can be used as a null value (or a sentinel value) for the primitive type T, representing an "empty" or "undefined" state
///</summary>
///<typeparam name="T">The type of elements in the list, which must be a value type (struct).</typeparam>
public interface PrimitiveList<T>
    where T : struct
{
    abstract class R : ICloneable, IReadOnlyList<T>, IEquatable<R>
    {
        protected internal T[] values = [];

        ///<summary>
        ///Number of elements in the list.
        ///</summary>
        protected internal int count;

        ///<summary>
        ///Default value used for filling gaps in the list.
        ///Can be used as a null value (or a sentinel value) for the primitive type T, representing an "empty" or "undefined" state
        ///</summary>
        protected T defaultValue;

        ///<summary>
        ///Gets the number of elements in the list.
        ///</summary>
        public int Count => count;

        ///<summary>
        ///Gets the capacity of the internal array.
        ///</summary>
        public int Capacity => values.Length;

        ///<summary>
        ///Checks if the list is empty.
        ///</summary>
        ///<returns>True if the list contains no elements; otherwise, false.</returns>
        public bool IsEmpty() => count == 0;

        ///<summary>
        ///Gets or sets the element at the specified index.
        ///</summary>
        ///<param name="index">The zero-based index of the element.</param>
        ///<returns>The element at the specified index.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or out of bounds.</exception>
        ///<exception cref="NotSupportedException">Thrown when attempting to set a value in a read-only list.</exception>
        public virtual T this[int index]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfNegative(index);
                if (index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index), "Index is out of bounds.");
                return values[index];
            }
            set => throw new NotSupportedException("Setting values is not supported in R.");
        }

        ///<summary>
        ///Gets a reference to the element at the specified index.
        ///</summary>
        ///<param name="index">The zero-based index of the element.</param>
        ///<returns>A reference to the element at the specified index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal ref T FindValue(int index) => ref values[index];

        ///<summary>
        ///Finds the first index of the specified value.
        ///</summary>
        ///<param name="value">The value to locate.</param>
        ///<returns>The zero-based index of the first occurrence, or -1 if not found.</returns>
        public int IndexOf(T value)
        {
            for (var i = 0; i < count; i++)
                if (EqualityComparer<T>.Default.Equals(values[i], value))
                    return i;
            return -1;
        }

        ///<summary>
        ///Checks if the list contains the specified value.
        ///</summary>
        ///<param name="value">The value to check for.</param>
        ///<returns>True if the value is found; otherwise, false.</returns>
        public bool Contains(T value) => IndexOf(value) != -1;

        ///<summary>
        ///Finds the last index of the specified value.
        ///</summary>
        ///<param name="value">The value to locate.</param>
        ///<returns>The zero-based index of the last occurrence, or -1 if not found.</returns>
        public int LastIndexOf(T value)
        {
            for (var i = count; -1 < --i;)
                if (EqualityComparer<T>.Default.Equals(values[i], value))
                    return i;
            return -1;
        }

        ///<summary>
        ///Determines whether the specified object is equal to the current list.
        ///</summary>
        ///<param name="obj">The object to compare with the current list.</param>
        ///<returns>True if the specified object is equal to the current list; otherwise, false.</returns>
        public override bool Equals(object? obj) => obj is R other && Equals(other);

        ///<summary>
        ///Calculates the hash code for the list.
        ///</summary>
        ///<returns>A hash code for the current list.</returns>
        public override int GetHashCode()
        {
            var hash = 131311;

            for (int i = 0, s = count; i < s; i++)
                hash = HashCode.Combine(hash, EqualityComparer<T>.Default.GetHashCode(values[i]));
            return HashCode.Combine(hash, Count);
        }

        ///<summary>
        ///Determines whether the specified list is equal to the current list.
        ///</summary>
        ///<param name="other">The list to compare with the current list.</param>
        ///<returns>True if the specified list is equal to the current list; otherwise, false.</returns>
        public bool Equals(R? other)
        {
            if (other == this)
                return true;
            if (other == null || count != other.count)
                return false;
            for (var i = 0; i < count; i++)
                if (!EqualityComparer<T>.Default.Equals(values[i], other.values[i]))
                    return false;

            return true;
        }

        ///<summary>
        ///Creates a shallow copy of the list.
        ///</summary>
        ///<returns>A new list with the same elements as the current list.</returns>
        public object Clone()
        {
            var clone = (R)MemberwiseClone();
            clone.values = (T[])values.Clone();
            return clone;
        }

        ///<summary>
        ///Returns a JSON string representation of the list.
        ///</summary>
        ///<returns>A JSON string representing the list.</returns>
        public override string ToString() => ToJSON(new StringBuilder()).ToString();

        ///<summary>
        ///Appends a JSON representation of the list to a StringBuilder.
        ///</summary>
        ///<param name="sb">The StringBuilder to append to.</param>
        ///<returns>The StringBuilder with the JSON representation appended.</returns>
        public StringBuilder ToJSON(StringBuilder sb)
        {
            sb.Append('[');
            if (count > 0)
            {
                sb.EnsureCapacity(sb.Length + count * 8);
                for (var i = 0; i < count; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    sb.Append(values[i]);
                }
            }

            sb.Append(']');
            return sb;
        }

        ///<summary>
        ///Returns an enumerator for the list.
        ///</summary>
        ///<returns>An enumerator that iterates through the list.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        ///<summary>
        ///Returns a strongly-typed enumerator for the list.
        ///</summary>
        ///<returns>An enumerator that iterates through the list.</returns>
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        ///<summary>
        ///Struct that enumerates the elements of the list.
        ///</summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly R list;
            private int index;

            ///<summary>
            ///Initializes a new instance of the Enumerator.
            ///</summary>
            ///<param name="list">The list to enumerate.</param>
            ///<exception cref="ArgumentNullException">Thrown if <paramref name="list"/> is null.</exception>
            internal Enumerator(R list)
            {
                this.list = list ?? throw new ArgumentNullException(nameof(list));
                index = -1;
            }

            ///<summary>
            ///Gets the current element in the enumeration.
            ///</summary>
            public T Current => list[index];

            ///<summary>
            ///Gets the current element as an object.
            ///</summary>
            object IEnumerator.Current => Current;

            ///<summary>
            ///Advances the enumerator to the next element.
            ///</summary>
            ///<returns>True if the enumerator was advanced; false if the enumeration is complete.</returns>
            public bool MoveNext() => ++index < list.count;

            ///<summary>
            ///Resets the enumerator to its initial position.
            ///</summary>
            public void Reset() => index = -1;

            ///<summary>
            ///Disposes the enumerator.
            ///</summary>
            public void Dispose() { }
        }
    }

    ///<summary>
    ///A read-write list implementation for primitive types, extending <see cref="R"/> with mutability features.
    ///Supports dynamic resizing, default value filling, and bulk operations. When resizing or setting indices
    ///beyond the current count, gaps are filled with a specified default value, enabling meaningful initialization.
    ///defaultValue can be used as a null value (or a sentinel value) for the primitive type T, representing an "empty" or "undefined" state
    ///</summary>
    class RW : R, IList<T>
    {
        ///<summary>
        ///Indicates whether to fill gaps with the default value.
        ///</summary>
        protected readonly bool fill;

        ///<summary>
        ///Initializes a new instance of the RW class with the specified default value and capacity.
        ///</summary>
        ///<param name="defaultValue">The default value to use for filling gaps.</param>
        ///<param name="capacity">The initial capacity of the list.</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is negative.</exception>
        public RW(T defaultValue, int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");

            this.defaultValue = defaultValue;
            fill = !EqualityComparer<T>.Default.Equals(defaultValue, default);
            if (0 < capacity)
                values = new T[capacity];
        }

        ///<summary>
        ///Creates a shallow copy of the list.
        ///</summary>
        ///<returns>A new list with the same elements and settings as the current list.</returns>
        public new RW Clone() => (RW)base.Clone();

        ///<summary>
        ///Removes the last element from the list.
        ///</summary>
        ///<returns>The current list instance.</returns>
        public RW Remove()
        {
            if (count == 0)
                return this;
            RemoveAt(count - 1);
            return this;
        }

        ///<summary>
        ///Sets the last element or the default value if the list is empty.
        ///</summary>
        ///<param name="value">The value to set.</param>
        ///<returns>The current list instance.</returns>
        public RW Set(T value) => Set(Math.Max(0, count - 1), value);

        ///<summary>
        ///Sets the element at the specified index, resizing the list if necessary.
        ///</summary>
        ///<param name="index">The zero-based index to set the value at.</param>
        ///<param name="value">The value to set.</param>
        ///<returns>The current list instance.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative.</exception>
        public RW Set(int index, T value)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative.");
            Set(this, index, value);
            return this;
        }

        ///<summary>
        ///Internal method to set a value at the specified index, handling resizing and filling.
        ///</summary>
        ///<param name="dst">The target list instance.</param>
        ///<param name="index">The zero-based index to set the value at.</param>
        ///<param name="value">The value to set.</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative.</exception>
        protected static void Set(RW dst, int index, T value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (dst.values.Length <= index)
                Array.Resize(ref dst.values, Math.Max(index + 1, dst.values.Length * 3 / 2));

            dst.values[index] = value;
            if (dst.count <= index)
            {
                if (dst.fill)
                    Array.Fill(dst.values, dst.defaultValue, dst.count, index - dst.count);
                dst.count = index + 1;
            }
        }

        ///<summary>
        ///Sets multiple elements starting at the specified index from an array.
        ///</summary>
        ///<param name="index">The zero-based index to start setting values.</param>
        ///<param name="src">The source array containing the values.</param>
        ///<returns>The current list instance.</returns>
        ///<exception cref="ArgumentNullException">Thrown if <paramref name="src"/> is null.</exception>
        public RW Set(int index, params T[] src) => Set(index, src, 0, src.Length);

        ///<summary>
        ///Sets multiple elements starting at the specified index from a segment of an array.
        ///</summary>
        ///<param name="index">The zero-based index to start setting values.</param>
        ///<param name="src">The source array containing the values.</param>
        ///<param name="srcIndex">The zero-based index in the source array to start copying from.</param>
        ///<param name="len">The number of elements to copy.</param>
        ///<returns>The current list instance.</returns>
        ///<exception cref="ArgumentNullException">Thrown if <paramref name="src"/> is null.</exception>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/>, <paramref name="srcIndex"/>, or <paramref name="len"/> is negative, or if the source range exceeds array bounds.</exception>
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
        ///Adds a value to the end of the list.
        ///</summary>
        ///<param name="value">The value to add.</param>
        public void Add(T value) => Set(count, value);

        ///<summary>
        ///Inserts a value at the specified index.
        ///</summary>
        ///<param name="index">The zero-based index to insert the value at.</param>
        ///<param name="value">The value to insert.</param>
        ///<returns>The current list instance.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative.</exception>
        public RW Add(int index, T value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (Count <= index)
            {
                Set(index, value);
                return this;
            }

            count = BitList.Resize(values, values.Length <= count ? values = new T[Math.Max(16, count * 3 / 2)] : values, index, count, 1);

            values[index] = value;

            return this;
        }

        ///<summary>
        ///Appends multiple values to the end of the list.
        ///</summary>
        ///<param name="items">The values to append.</param>
        ///<returns>The current list instance.</returns>
        ///<exception cref="ArgumentNullException">Thrown if <paramref name="items"/> is null.</exception>
        public RW Add(params T[] items) => Set(count, items);

        ///<summary>
        ///Appends all elements from another list to the end of the current list.
        ///</summary>
        ///<param name="src">The source list to copy elements from.</param>
        ///<returns>The current list instance.</returns>
        ///<exception cref="ArgumentNullException">Thrown if <paramref name="src"/> is null.</exception>
        public RW AddAll(R src)
        {
            ArgumentNullException.ThrowIfNull(src);
            return Set(count, src.values, 0, src.count);
        }

        ///<summary>
        ///Removes all elements from the list.
        ///</summary>
        public void Clear() => count = 0;

        ///<summary>
        ///Sets the capacity of the internal array.
        ///</summary>
        ///<param name="capacity">The desired capacity.</param>
        ///<returns>The current list instance.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is negative.</exception>
        public RW Capacity_(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);

            if (values.Length != capacity)
                Array.Resize(ref values, capacity);
            if (capacity < count)
                count = capacity;
            return this;
        }

        ///<summary>
        ///Sets the number of elements in the list, expanding or truncating as needed.
        ///</summary>
        ///<param name="newCount">The desired number of elements.</param>
        ///<returns>The current list instance.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="newCount"/> is negative.</exception>
        public RW Count_(int newCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(newCount);
            if (newCount == 0)
                Clear();
            else if (Count < newCount)
                Set(newCount - 1, defaultValue);
            else
                count = newCount;
            return this;
        }

        ///<summary>
        ///Adjusts the capacity to match the current number of elements.
        ///</summary>
        ///<returns>The current list instance.</returns>
        public RW Fit()
        {
            Capacity_(count);
            return this;
        }

        ///<summary>
        ///Swaps the elements at the specified indices.
        ///</summary>
        ///<param name="index1">The zero-based index of the first element.</param>
        ///<param name="index2">The zero-based index of the second element.</param>
        ///<returns>The current list instance.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index1"/> or <paramref name="index2"/> is negative or out of bounds.</exception>
        public RW Swap(int index1, int index2)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index1);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index1, Count);
            ArgumentOutOfRangeException.ThrowIfNegative(index2);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index2, Count);

            if (index1 == index2)
                return this;

            var val1 = this[index1];
            var val2 = this[index2];
            if (EqualityComparer<T>.Default.Equals(val1, val2))
                return this;
            (values[index1], values[index2]) = (values[index2], values[index1]);

            return this;
        }

        ///<summary>
        ///Gets or sets the element at the specified index.
        ///</summary>
        ///<param name="index">The zero-based index of the element.</param>
        ///<returns>The element at the specified index.</returns>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or out of bounds when getting, or negative when setting.</exception>
        public override T this[int index]
        {
            get => base[index];
            set => Set(this, index, value);
        }

        ///<summary>
        ///Checks if the list contains the specified value.
        ///</summary>
        ///<param name="item">The value to check for.</param>
        ///<returns>True if the value is found; otherwise, false.</returns>
        public bool Contains(T item) => base.Contains(item);

        ///<summary>
        ///Copies the elements of the list to an array.
        ///</summary>
        ///<param name="dst">The destination array.</param>
        ///<param name="dstIndex">The zero-based index in the destination array to start copying to.</param>
        ///<exception cref="ArgumentNullException">Thrown if <paramref name="dst"/> is null.</exception>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="dstIndex"/> is negative.</exception>
        ///<exception cref="ArgumentException">Thrown if the destination array is too small.</exception>
        public void CopyTo(T[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
            if (dst.Length - dstIndex < Count)
                throw new ArgumentException("Destination array is not long enough.");

            Array.Copy(values, 0, dst, dstIndex, count);
        }

        ///<summary>
        ///Removes the first occurrence of the specified value.
        ///</summary>
        ///<param name="item">The value to remove.</param>
        ///<returns>True if the value was removed; otherwise, false.</returns>
        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index == -1)
                return false;
            RemoveAt(index);
            return true;
        }

        ///<summary>
        ///Gets a value indicating whether the list is read-only.
        ///</summary>
        public bool IsReadOnly => false;

        ///<summary>
        ///Inserts a value at the specified index.
        ///</summary>
        ///<param name="index">The zero-based index to insert the value at.</param>
        ///<param name="item">The value to insert.</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative.</exception>
        public void Insert(int index, T item) => Add(index, item);

        ///<summary>
        ///Removes the element at the specified index.
        ///</summary>
        ///<param name="index">The zero-based index of the element to remove.</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or out of bounds.</exception>
        public void RemoveAt(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            count = BitList.Resize(values, values, index, count, -1);
        }
    }
}