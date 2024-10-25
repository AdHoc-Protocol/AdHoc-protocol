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

namespace org.unirail.collections;

public interface BoolNullList
{
    // Abstract class representing a read-only list of nullable bool values
    abstract class R : BitsList<byte>.R
    {
        // Indexer to get nullable bool values
        public new virtual bool? this[int index]
        {
            get => base[index] switch
            {
                1 => true,
                0 => false,
                _ => null
            };
            set => throw new NotImplementedException();
        }

        // Gets the raw byte value at the specified index
        public int get(int index) => base[index];

        // Constructor with specified length
        protected R(int length) : base(2, length)
        {
        }

        // Constructor with default value and count
        protected R(bool? default_value, int Count) : base(2, (byte)(default_value == null ? 2 : default_value.Value ? 1 : 0), Count)
        {
        }

        // Clones the current object
        public new R Clone() => (R)base.Clone();
    }

    // Read-write implementation of the nullable bool list
    class RW : R
    {
        // Removes the specified nullable bool value
        public bool Remove(bool? item)
        {
            var i = IndexOf((byte)(item == null ?
                2 :
                item!.Value ?
                    1 :
                    0));
            if (i < 0) return false;
            removeAt(i);
            return true;
        }

        // Inserts a nullable bool value at the specified index
        public void Insert(int index, bool? item) => add(this, index, (byte)(item == null ? 2 : item.Value ? 1 : 0));

        // Indexer to get or set nullable bool values
        public override bool? this[int index]
        {
            get => base[index];
            set => set(index, value);
        }

        // Removes the value at the specified index
        public void RemoveAt(int index) => removeAt(index);

        // Constructor with specified length
        public RW(int length) : base(length)
        {
        }

        // Constructor with default value and count
        public RW(bool? default_value, int Count) : base(default_value, Count)
        {
        }

        // Adds a non-nullable bool value to the list
        public RW Add(bool value)
        {
            add(this, value ? 1 : 0);
            return this;
        }

        // Adds a nullable bool value to the list
        public RW Add(bool? value)
        {
            add(this, value == null ? 2 : value.Value ? 1 : 0);
            return this;
        }


        // Removes the specified nullable bool value from the list
        public RW remove(bool? value)
        {
            remove(this, (byte)(value == null ? 2 : value.Value ? 1 : 0));
            return this;
        }

        // Removes the specified non-nullable bool value from the list
        public RW remove(bool value)
        {
            remove(this, (byte)(value ? 1 : 0));
            return this;
        }

        // Removes the value at the specified index
        public RW removeAt(int item)
        {
            removeAt(this, item);
            return this;
        }


        // Sets the last item to the specified non-nullable bool value
        public RW Set1(bool value)
        {
            set1(this, Count, (byte)(value ? 1 : 0));
            return this;
        }

        // Sets the last item to the specified nullable bool value
        public RW Set1(bool? value)
        {
            set1(this, Count, (byte)(value == null ? 2 : value.Value ? 1 : 0));
            return this;
        }

        // Sets the item at the specified index to the specified non-nullable bool value
        public RW Set1(int item, bool value)
        {
            set1(this, item, (byte)(value ? 1 : 0));
            return this;
        }

        // Sets the item at the specified index to the specified nullable bool value
        public RW Set1(int item, bool? value)
        {
            set1(this, item, (byte)(value == null ? 2 : value!.Value ? 1 : 0));
            return this;
        }

        // Sets values starting from the specified index with an array of non-nullable bool values
        public RW set(int index, params bool[] values)
        {
            for (int i = 0, max = values.Length; i < max; i++)
                Set1(index + i, values[i]);
            return this;
        }

        // Sets values starting from the specified index with an array of nullable bool values
        public RW set(int index, params bool?[] values)
        {
            for (int i = 0, max = values.Length; i < max; i++)
                Set1(index + i, values[i]);
            return this;
        }

        // Fits the internal storage to the number of items
        public RW Fit()
        {
            Capacity = base.Count;
            return this;
        }

        // Clears the list
        public RW Clear()
        {
            clear();
            return this;
        }

        // Gets or sets the length of the list
        public new int Capacity
        {
            get => Capacity();
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

        // Gets or sets the count of items in the list
        public new int Count
        {
            get => base.Count;
            set
            {
                if (value < 1) Clear();
                else if (base.Count < value) set1(this, value - 1, default_value);
                else base.Count = value;
            }
        }

        // Resizes the list to the specified size
        public RW Resize(int size)
        {
            Count = size;
            return this;
        }

        // Clones the current object
        public RW Clone() => (RW)base.Clone();
    }
}