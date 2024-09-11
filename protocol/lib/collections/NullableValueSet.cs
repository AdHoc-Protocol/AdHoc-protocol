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

namespace org.unirail.collections;

using System;
using System.Collections;
using System.Collections.Generic;

///<summary>
///Represents a set that allows nullable value types.
///This class is designed to use memory efficiently.
///For example, a standard HashSet stores <see cref="Nullable{T}"/> (e.g., <see cref="int?"/>) as an 8-byte entity.
///However, only one value can be null in a set context.
///This implementation stores <see cref="int?"/> type as 4-byte entries and uses a flag to represent the null value.
///</summary>
///<typeparam name="T">The type of elements in the set. Must be a value type.</typeparam>
public class NullableValueSet<T> : IEnumerable<T?>, IEquatable<NullableValueSet<T>>
    where T : struct
{
    public NullableValueSet(ISet<T> set) => _set = new HashSet<T>(set);
    public NullableValueSet(ISet<T> set, IEqualityComparer<T>? comparer) => _set = new HashSet<T>(set, comparer);
    public NullableValueSet(IEnumerable<T> collection) => _set = new HashSet<T>(collection);
    public NullableValueSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer) => _set = new HashSet<T>(collection, comparer);
    public NullableValueSet(IEqualityComparer<T>? comparer) => _set = new HashSet<T>(comparer);
    public NullableValueSet(int capacity) => _set = new HashSet<T>(capacity);
    public NullableValueSet(int capacity, IEqualityComparer<T>? comparer) => _set = new HashSet<T>(capacity, comparer);

    private int _version;

    //The main set to store non-null elements
    private HashSet<T> _set;

    //The flag to indicate if the null value exists in the set
    private bool _nullValueExists = false;

    //Adds an element to the set
    public bool Add(T? item)
    {
        _version++;
        if (item.HasValue)
            return _set.Add(item.Value); //Add non-null element

        if (_nullValueExists)
            return false; //Return false if null value already exists

        _nullValueExists = true;
        return true;
    }

    //Removes an element from the set
    public bool Remove(T? item)
    {
        if (item.HasValue) //Remove non-null element
            if (_set.Remove(item.Value))
            {
                _version++;
                return true;
            }
            else
                return false;

        if (!_nullValueExists)
            return false; //Return false if null value does not exist

        _nullValueExists = false;
        _version++;
        return true;
    }

    //Clears the set
    public void Clear()
    {
        _version++;
        _set.Clear();             //Clear non-null elements
        _nullValueExists = false; //Reset flag for null value
    }

    //Checks if the set contains an element
    public bool Contains(T? item) => item.HasValue ? _set.Contains(item.Value) : _nullValueExists;

    //Gets the count of elements in the set
    public int Count => _set.Count + (_nullValueExists ? 1 : 0);

    //Checks if the set equals another object
    public override bool Equals(object? obj) => obj is NullableValueSet<T> other && Equals(other);

    //Checks if the set equals another set
    public bool Equals(NullableValueSet<T>? other)
    {
        if (other == null ||
            _nullValueExists != other._nullValueExists ||
            _set.Count != other._set.Count)
            return false;

        if (_nullValueExists && !_set.SetEquals(other._set))
            return false;

        return true;
    }

    //Gets the hash code of the set
    public override int GetHashCode()
    {
        var hash = 0x1b873593;
        hash = HashCode.Combine(hash, _set.GetHashCode());
        return HashCode.Combine(hash, _nullValueExists);
    }
    public bool SetEquals(NullableValueSet<T>? other) => other != null && _nullValueExists == other._nullValueExists && _set.SetEquals(other._set);

    //Gets an enumerator for the set
    public IEnumerator<T?> GetEnumerator() => new Enumerator(this);

    //Gets an enumerator for the set (required for the IEnumerable interface)
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    //Enumerator for nullable values
    public Enumerator GetEnumerator_() => new Enumerator(this);

    public struct Enumerator : IEnumerator<T?>
    {
        private readonly NullableValueSet<T> _src;
        private readonly int _version;
        private HashSet<T>.Enumerator _srcEnum;
        private bool _onSrcEnum;

        internal Enumerator(NullableValueSet<T> set)
        {
            _src = set;
            _srcEnum = set._set.GetEnumerator();
            _version = set._version;
            _onSrcEnum = true;
        }

        public bool MoveNext()
        {
            if (_version != _src._version)
                throw new InvalidOperationException();

            return _onSrcEnum && ((_onSrcEnum = _srcEnum.MoveNext()) || _src._nullValueExists);
        }

        public void Dispose() { }

        void IEnumerator.Reset()
        {
            if (_version != _src._version)
                throw new InvalidOperationException();

            _onSrcEnum = true;
            _srcEnum = _src._set.GetEnumerator();
        }

        object? IEnumerator.Current => Current;

        public T? Current
        {
            get
            {
                if (_version != _src._version)
                    throw new InvalidOperationException();

                return _onSrcEnum ? _srcEnum.Current : null;
            }
        }
    }
}
