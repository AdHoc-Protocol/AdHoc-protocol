//  MIT License
//
//  Copyright © 2020 Chikirev Sirguy, Unirail Group. All rights reserved.
//  For inquiries, please contact:  al8v5C6HU4UtqE9@gmail.com
//  GitHub Repository: https://github.com/AdHoc-Protocol
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to use,
//  copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
//  the Software, and to permit others to do so, under the following conditions:
//
//  1. The above copyright notice and this permission notice must be included in all
//     copies or substantial portions of the Software.
//
//  2. Users of the Software must provide a clear acknowledgment in their user
//     documentation or other materials that their solution includes or is based on
//     this Software. This acknowledgment should be prominent and easily visible,
//     and can be formatted as follows:
//     "This product includes software developed by Chikirev Sirguy and the Unirail Group
//     (https://github.com/AdHoc-Protocol)."
//
//  3. If you modify the Software and distribute it, you must include a prominent notice
//     stating that you have changed the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT, OR OTHERWISE, ARISING FROM,
//  OUT OF, OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace org.unirail.collections;

public interface BitsNullList<T> where T : struct
{
    // Abstract class representing a read-only list of bits with nullable values
    abstract class R : BitsList<T>.R
    {
        public T null_val { get; protected set; } // The value representing null

        // Constructor with bits per item and null value
        protected R(int bits_per_item, T null_val) : base(bits_per_item) => this.null_val = (T)(ValueType)null_val;

        // Constructor with bits per item, null value, and count
        protected R(int bits_per_item, T null_val, int Count) : base(bits_per_item, null_val, Count) => this.null_val = null_val;

        // Constructor with bits per item, null value, default value, and count
        protected R(int bits_per_item, T null_val, T default_value, int Count) : base(bits_per_item, default_value, Count) => this.null_val = null_val;

        // Checks if the value at the specified index is not the null value
        public bool hasValue(int index) => !this[index].Equals(null_val);

        // Checks if the list contains the specified item
        public bool Contains(T? item) => 0 < IndexOf(item);

        // Finds the index of the specified item
        public int IndexOf(T? item) => item == null ?
            indexOf(null_val) :
            indexOf(item.Value);

        // Indexer to get or set nullable values
        public virtual T? this[int index]
        {
            get
            {
                var v = base[index];
                return v.Equals(null_val) ?
                    null :
                    v;
            }
            set => throw new NotImplementedException("BitsList.R is readonly");
        }

        // Gets the raw value at the specified index
        public T raw(int index) => base[index];

        // Clones the current object
        R Clone()
        {
            return (R)base.Clone();
        }

        // Converts the list to a string representation
        StringBuilder ToString(StringBuilder? dst)
        {
            if (dst == null) dst = new StringBuilder(Count * 4);
            else dst.EnsureCapacity(dst.Length + Count * 4);
            var src = values[(uint)0];
            for (int bp = 0, max = Count * bits, i = 1; bp < max; bp += bits, i++)
            {
                var _bit = BitsList<T>.bit((uint)bp);
                var index = (uint)(BitsList<T>.index((uint)bp) + 1);
                var _value = (long)(ValueType)(BitsList<T>.BITS < _bit + bits ?
                    BitsList<T>.value(src, src = values[index], _bit, bits, mask) :
                    BitsList<T>.value(src, _bit, mask));
                if (_value.Equals(null_val)) dst.Append("null");
                else dst.Append(_value);
                dst.Append('\t');
                if (i % 10 == 0) dst.Append('\t').Append(i / 10 * 10).Append('\n');
            }

            return dst;
        }

        // Gets an enumerator for the list
        public Enumerator GetEnumerator() => new Enumerator(this);
    }

    // Enumerator for iterating through the list
    public struct Enumerator : IEnumerator<T?>, IEnumerator
    {
        private readonly R _list;
        private int _index;

        private T? _current;

        // Constructor initializing the enumerator with the list
        internal Enumerator(R list)
        {
            _list = list;
            _index = 0;
            _current = default;
        }

        public void Dispose()
        {
        }

        // Moves to the next item in the list
        public bool MoveNext()
        {
            _current = _list[_index];
            _index++;
            return true;
        }

        public T? Current => _current!;

        object? IEnumerator.Current => Current;

        // Resets the enumerator
        void IEnumerator.Reset()
        {
            _index = 0;
            _current = default;
        }
    }

    // Read-write implementation of the list
    class RW : R
    {
        // Constructor with bits per item and null value
        public RW(int bits_per_item, T null_val) : base(bits_per_item, null_val)
        {
        }

        // Constructor with bits per item, null value, and count
        public RW(int bits_per_item, T null_val, int Count) : base(bits_per_item, null_val, Count)
        {
        }

        // Constructor with bits per item, null value, default value, and count
        public RW(int bits_per_item, T null_val, T? default_value, int Count) : base(bits_per_item, null_val, default_value == null ?
            null_val :
            default_value.Value, Count)
        {
        }

        // Sets the value at the specified index
        public void Set(int item, int value) => set(this, item, value);

        // Removes the specified item from the list
        public bool Remove(T? item)
        {
            var i = IndexOf(item);
            if (i < 0) return false;
            removeAt(i);
            return true;
        }

        // Inserts an item at the specified index
        public void Insert(int index, T? item) => Add1(item == null ?
            null_val :
            item.Value);

        // Clears the list
        public void Clear() => clear();

        // Removes the item at the specified index
        public void RemoveAt(int index) => removeAt(index);

        // Adds an item to the list
        public void Add1(T? value) => Add1(Count, value);
        public void Add1(T value) => Add1(Count, value);

        // Adds an item at the specified index
        public void Add1(int index, T? value) => Add1(index, value ?? null_val);

        public void Add1(int index, T src)
        {
            if (index < Count) add(this, index, src);
            else Set(index, src);
        }

        // Removes the specified value from the list
        public void remove(T? value) => remove(this, value ?? null_val);
        public void remove(T value) => remove(this, value);

        // Removes the item at the specified index
        public void removeAt(int item) => removeAt(this, item);

        // Indexer to get or set the value at the specified index
        public override T? this[int item]
        {
            get => base[item];
            set => set1(this, item, value ?? null_val);
        }

        // Sets the raw value at the specified index
        public void raw(int index, int value) => set1(this, index, (T)(ValueType)value);

        // Sets multiple values starting from the specified index
        public RW Set(int item, params T?[] values)
        {
            for (var i = values.Length; -1 < --i;)
                set1(this, item + i, values[i] == null ?
                    null_val :
                    (T)values[i]!.Value);
            return this;
        }

        // Sets multiple sbyte values starting from the specified index
        public RW Set(int item, params sbyte?[] values)
        {
            for (var i = values.Length; -1 < --i;)
                set1(this, item + i, values[i] == null ?
                    null_val :
                    (T)(ValueType)values[i]!.Value);
            return this;
        }

        // Sets multiple short values starting from the specified index
        public RW Set(int item, params short?[] values)
        {
            for (var i = values.Length; -1 < --i;)
                set1(this, item + i, values[i] == null ?
                    null_val :
                    (T)(ValueType)values[i]!.Value);
            return this;
        }

        // Sets multiple ushort values starting from the specified index
        public RW Set(int item, params ushort?[] values)
        {
            for (var i = values.Length; -1 < --i;)
                set1(this, item + i, values[i] == null ?
                    null_val :
                    (T)(ValueType)values[i]!.Value);
            return this;
        }

        // Sets multiple int values starting from the specified index
        public RW Set(int item, params int?[] values)
        {
            for (var i = values.Length; -1 < --i;)
                set1(this, item + i, values[i] == null ?
                    null_val :
                    (T)(ValueType)values[i]!.Value);
            return this;
        }

        // Sets multiple uint values starting from the specified index
        public RW Set(int item, params uint?[] values)
        {
            for (var i = values.Length; -1 < --i;)
                set1(this, item + i, values[i] == null ?
                    null_val :
                    (T)(ValueType)values[i]!.Value);
            return this;
        }

        // Sets multiple long values starting from the specified index
        public RW Set(int item, params long?[] values)
        {
            for (var i = values.Length; -1 < --i;)
                set1(this, item + i, values[i] == null ?
                    null_val :
                    (T)(ValueType)values[i]!.Value);
            return this;
        }

        // Sets multiple ulong values starting from the specified index
        public RW Set(int item, params ulong?[] values)
        {
            for (var i = values.Length; -1 < --i;)
                set1(this, item + i, values[i] == null ?
                    null_val :
                    (T)(ValueType)values[i]!.Value);
            return this;
        }

        // Sets a single value at the specified index
        public RW Set1(int item, T value)
        {
            set1(this, item, (T)(ValueType)value);
            return this;
        }

        // Sets multiple values starting from the specified index
        public RW Set(int item, params T[] values)
        {
            set(this, item, values);
            return this;
        }

        // Sets multiple sbyte values starting from the specified index
        public RW Set(int item, params sbyte[] values)
        {
            set(this, item, values);
            return this;
        }

        // Sets multiple short values starting from the specified index
        public RW Set(int item, params short[] values)
        {
            set(this, item, values);
            return this;
        }

        // Sets multiple ushort values starting from the specified index
        public RW Set(int item, params ushort[] values)
        {
            set(this, item, values);
            return this;
        }

        // Sets multiple int values starting from the specified index
        public RW Set(int item, params int[] values)
        {
            set(this, item, values);
            return this;
        }

        // Sets multiple uint values starting from the specified index
        public RW Set(int item, params uint[] values)
        {
            set(this, item, values);
            return this;
        }

        // Sets multiple long values starting from the specified index
        public RW Set(int item, params long[] values)
        {
            set(this, item, values);
            return this;
        }

        // Sets multiple ulong values starting from the specified index
        public RW Set(int item, params ulong[] values)
        {
            set(this, item, values);
            return this;
        }

        // Clones the current object
        public new RW Clone() => (RW)base.Clone();

        // Adjusts the length to fit the number of items
        public RW Fit()
        {
            Capacity = base.Count;
            return this;
        }

        // Property to get or set the length of the list
        public new int Capacity
        {
            get => base.Capacity();
            set
            {
                if (value < 1)
                {
                    values = Array.Empty<ulong>();
                    Count = 0;
                }
                else Capacity(value);
            }
        }

        // Property to get or set the count of items in the list
        public new int Count
        {
            get => base.Count;
            set
            {
                if (value < 1) Clear();
                else if (base.Count < value) Set1(value - 1, default_value);
                else base.Count = value;
            }
        }

        // Resizes the list to the specified size
        public RW Resize(int size)
        {
            Count = size;
            return this;
        }
    }
}