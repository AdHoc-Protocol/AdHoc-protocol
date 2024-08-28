//MIT License
//
//Copyright © 2020 Chikirev Sirguy, Unirail Group. All rights reserved.
//For inquiries, please contact:  al8v5C6HU4UtqE9@gmail.com
//GitHub Repository: https://github.com/AdHoc-Protocol
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to use,
//copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
//the Software, and to permit others to do so, under the following conditions:
//
//1. The above copyright notice and this permission notice must be included in all
//   copies or substantial portions of the Software.
//
//2. Users of the Software must provide a clear acknowledgment in their user
//   documentation or other materials that their solution includes or is based on
//   this Software. This acknowledgment should be prominent and easily visible,
//   and can be formatted as follows:
//   "This product includes software developed by Chikirev Sirguy and the Unirail Group
//   (https://github.com/AdHoc-Protocol)."
//
//3. If you modify the Software and distribute it, you must include a prominent notice
//   stating that you have changed the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT, OR OTHERWISE, ARISING FROM,
//OUT OF, OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;

namespace org.unirail.collections;

///<summary>
///Represents a list that allows nullable value types.
///For example, standard List implementation stores <see cref="Nullable{T}"/> (e.g., <see cref="int?"/>) as 8-byte entities.
///This class is designed to use memory efficiently by storing information about null values as bits in a bit set.
///Non-null values are stored as 4-byte entities.
///</summary>
///<typeparam name="T">The type of elements in the list. Must be a value type.</typeparam>
public interface NullableValueList<T>
    where T : struct{
    //Abstract class representing a read-only list of nullable values
    abstract class R : ICloneable, IReadOnlyList<T?>, IEquatable<R>{
        //Enumerator implementation
        IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<T?>).GetEnumerator();

        //Enumerator for nullable values
        public IEnumerator<T?> GetEnumerator() => new Enumerator(this);

        //Enumerator for nullable values
        public Enumerator GetEnumerator_() => new Enumerator(this);

        //Enumerator struct for nullable values
        public struct Enumerator : IEnumerator<T?>{
            private readonly R   _list;
            private          int _index;

            public Enumerator(R list)
            {
                _list  = list;
                _index = -1;
            }

            public T? Current => -1 < _index && _index < _list.Count ?
                                     _list[_index] :
                                     null;

            object? IEnumerator.Current => Current;

            public bool MoveNext() => ++(_index) < _list.Count;

            public void Reset() => _index = -1;

            public void Dispose() { }
        }

        //BitList to track null values
        public BitList<bool>.RW nulls;

        //List to store actual values
        public List<T> values;

        //Gets the total number of items (including nulls)
        public int Count => nulls.Count;

        //Gets the length of the nulls BitList
        public int Capacity => nulls.Capacity();

        //Checks if the list is empty
        public bool isEmpty => Count < 1;

        //Checks if the value at the specified index is not null
        public bool hasValue(int index) => nulls.get(index);

        //Gets the next index with a value after the specified index
        public int nextValueIndex(int index) { return nulls.next1(index); }

        //Gets the previous index with a value before the specified index
        public int prevValueIndex(int index) { return nulls.prev1(index); }

        //Gets the next index with a null after the specified index
        public int nextNullIndex(int index) { return nulls.next0(index); }

        //Gets the previous index with a null before the specified index
        public int prevNullIndex(int index) { return nulls.prev0(index); }

        //Gets or sets the value at the specified index
        public virtual T? this[int index]
        {
            get => hasValue(index) ?
                       values[nulls.rank(index) - 1] :
                       null;
            set => throw new NotImplementedException();
        }

        //Gets the index of the specified value
        public int IndexOf(T? value)
        {
            if( value == null )
                return nextNullIndex(0);

            var i = values.IndexOf(value.Value);
            return i < 0 ?
                       i :
                       nulls.bit(i);
        }

        //Gets the last index of the specified value
        public int LastIndexOf(T value)
        {
            var i = values.LastIndexOf(value);
            return i < 0 ?
                       i :
                       nulls.bit(i);
        }

        //Clones the current object
        public object Clone()
        {
            var dst = (R)MemberwiseClone();
            dst.nulls  = (BitList<bool>.RW)nulls.Clone();
            dst.values = new List<T>();
            foreach( var item in values )
                dst.values.Add(item);

            return dst;
        }

        //Checks equality with another object
        public override bool Equals(object? obj) => obj != null && Equals(obj as R);

        //Checks equality with another R object
        public bool Equals(R? other) => other != null && other.Count == Count && values.Equals(other.values) && nulls.Equals(other.nulls);

        //Gets the hash code for the current object
        public override int GetHashCode() => HashCode.Combine(nulls.GetHashCode(), values.GetHashCode());
    }

    //Read-write implementation of the nullable list
    class RW : R, IList<T?>{
        //Gets or sets the value at the specified index
        public override T? this[int index]
        {
            get => hasValue(index) ?
                       values[nulls.rank(index) - 1] :
                       null;
            set
            {
                if( value.HasValue )
                    if( nulls.get(index) )
                        values[nulls.rank(index) - 1] = value.Value;
                    else
                    {
                        nulls.Set1(index);
                        index = nulls.rank(index) - 1;
                        if( index < Count )
                            values.Insert(index, value.Value);
                        else
                            values.Add(value.Value);
                    }
                else if( Count <= index )
                    nulls.Set0(index); //resize
                else if( nulls.get(index) )
                {
                    values.RemoveAt(nulls.rank(index) - 1);
                    nulls.Set0(index);
                }
            }
        }

        //Gets or sets the count of items in the list
        public new int Count
        {
            get => base.Count;
            set
            {
                if( value < 1 )
                    Clear();
                else
                {
                    nulls.Count = value;
                    if( nulls.cardinality() < values.Count )
                        values.RemoveRange(nulls.cardinality(), values.Count - nulls.cardinality());
                }
            }
        }

        //Resizes the list to the specified size
        public RW Resize(int size)
        {
            Count = size;
            return this;
        }

        //Adds an item to the list
        public void Add(T? item)
        {
            if( item == null )
                nulls.Add(false);
            else
            {
                values.Add(item.Value);
                nulls.Add(true);
            }
        }

        //Adds an item at the specified index
        public RW Add(int index, T? item)
        {
            if( index < Count )
                Insert(index, item);
            else
                this[index] = item;

            return this;
        }

        //Clears the list
        void ICollection<T?>.Clear()
        {
            nulls.clear();
            values.Clear();
        }

        //Checks if the list contains the specified item
        public bool Contains(T? item) => -1 < IndexOf(item);

        //Copies the list items to an array starting at the specified array index
        public void CopyTo(T?[] array, int arrayIndex)
        {
            for( var i = 0; i++ < Count; )
                array[i + arrayIndex] = this[i];
        }

        //Removes the specified item from the list
        public bool Remove(T? item)
        {
            var i = IndexOf(item);
            if( i < 0 )
                return false;
            RemoveAt(i);
            return true;
        }

        //Gets a value indicating whether the list is read-only
        public bool IsReadOnly => false;

        //Inserts an item at the specified index
        public void Insert(int index, T? item)
        {
            if( item == null )
                nulls.Add(false);
            else
            {
                nulls.Add(index, true);
                values.Insert(nulls.rank(index) - 1, item.Value);
            }
        }

        //Removes the item at the specified index
        public void RemoveAt(int index)
        {
            if( Count < 1 || Count <= index )
                return;

            if( nulls.get(index) )
                values.RemoveAt(nulls.rank(index) - 1);
            nulls.remove(index);
        }

        //Removes the last item from the list
        public void Remove() => RemoveAt(Count - 1);

        //Constructor with specified length
        public RW(int length)
        {
            nulls  = new BitList<bool>.RW(length);
            values = new List<T>(length);
        }

        protected T? default_value;

        //Constructor with default value and count. If 0 < count the collection is initialized with the default value.
        public RW(T? default_value, int count)
        {
            this.default_value = default_value;

            nulls = new BitList<bool>.RW(false, count);

            if( count < 1 )
            {
                values = new List<T>(-count);
                return;
            }

            values = new List<T>(count);

            if( default_value == null )
                return;
            while( 0 < --count )
                values.Add(default_value.Value);
        }

        //Constructor with enumerator, default value, and count
        public RW(IEnumerator<T?> src, T? default_value, int count)
        {
            this.default_value = default_value;
            nulls              = new BitList<bool>.RW(count);
            values             = new List<T>(count);

            while( src.MoveNext() && Count < count )
                Add(src.Current);
            while( Count < count )
                Add(default_value);
        }

        //Sets the value at the last index
        public RW Set(T? value)
        {
            this[Count - 1] = value;
            return this;
        }

        //Sets values starting from the specified index
        public RW Set(int index, T?[] values)
        {
            for( var i = values.Length; --i >= 0; )
                this[index + i] = values[i];
            return this;
        }

        //Sets values starting from the specified index with source index and length
        public RW Set(int index, T?[] values, int src_index, int len)
        {
            for( var i = len; --i >= 0; )
                this[index + i] = values[src_index + i];
            return this;
        }

        //Adds an array of items to the list
        public RW Add(T?[] items)
        {
            if( items.Length == 0 )
                return this;
            var c = Count;
            this[Count + items.Length - 1] = default_value;

            return Set(c, items);
        }

        //Adds all items from another R object
        public RW AddAll(R src)
        {
            this[Count + src.Count - 1] = default_value;
            for( int i = 0, s = src.Count; i < s; i++ )
                this[i] = src[i];

            return this;
        }

        //Adds all items from an enumerator
        public RW AddAll(IEnumerator<T?> src)
        {
            while( src.MoveNext() )
                Add(src.Current);
            return this;
        }

        //Clears the list
        public RW Clear()
        {
            values.Clear();
            nulls.clear();
            return this;
        }

        //Gets the length of the nulls BitList
        public int Capacity() => nulls.Capacity();

        //Gets the length of the nulls BitList
        public RW Capacity(int length)
        {
            nulls.Capacity(length);
            values.Capacity = length;
            var c = nulls.cardinality();
            if( c < values.Count )
                values.RemoveRange(c, values.Count - c);
            return this;
        }

        //Swaps values at two specified indexes
        public RW Swap(int index1, int index2)
        {
            int exist, empty;
            if( nulls.get(index1) )
            {
                if( nulls.get(index2) )
                {
                    var a = nulls.rank(index1) - 1;
                    var b = nulls.rank(index2) - 1;
                    var A = values[a];
                    values[a] = values[b];
                    values[b] = A;
                    return this;
                }

                exist = nulls.rank(index1) - 1;
                empty = nulls.rank(index2);
                nulls.Set0(index1);
                nulls.Set1(index2);
            }
            else if( nulls.get(index2) )
            {
                exist = nulls.rank(index2) - 1;
                empty = nulls.rank(index1);
                nulls.Set1(index1);
                nulls.Set0(index2);
            }
            else
                return this;

            var v = values[exist];
            values.RemoveAt(exist);
            Add(empty, v);
            return this;
        }
    }
}