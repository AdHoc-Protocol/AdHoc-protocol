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
///Represents a dictionary that allows nullable value type keys.
///Standard Dictionary implementation does not allow nullable keys. This class overcomes that limitation.
///</summary>
///<typeparam name="K">The type of keys in the dictionary</typeparam>
///<typeparam name="V">The type of values in the dictionary.</typeparam>
public class NullableKeyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>, IEquatable<NullableKeyDictionary<K, V>>
    where K : class ?{
    public NullableKeyDictionary(IDictionary<K, V>               dictionary) => _dictionary = new Dictionary<K, V>(dictionary);
    public NullableKeyDictionary(IDictionary<K, V>               dictionary, IEqualityComparer<K>? comparer) => _dictionary = new Dictionary<K, V>(dictionary, comparer);
    public NullableKeyDictionary(IEnumerable<KeyValuePair<K, V>> collection) => _dictionary = new Dictionary<K, V>(collection);
    public NullableKeyDictionary(IEnumerable<KeyValuePair<K, V>> collection, IEqualityComparer<K>? comparer) => _dictionary = new Dictionary<K, V>(collection, comparer);
    public NullableKeyDictionary(IEqualityComparer<K>?           comparer) => _dictionary = new Dictionary<K, V>(comparer);
    public NullableKeyDictionary(int                             capacity) => _dictionary = new Dictionary<K, V>(capacity);
    public NullableKeyDictionary(int                             capacity, IEqualityComparer<K>? comparer) => _dictionary = new Dictionary<K, V>(capacity, comparer);

    private int _version;

    //The main dictionary to store non-null keys
    private Dictionary<K, V> _dictionary = new Dictionary<K, V>();

    //The value associated with the null key
    private V _nullKeyEntry = default(V);

    //A flag to indicate if a null key exists in the dictionary
    private bool _nullKeyExists = false;

    //Indexer to get or set values associated with a key
    public V this[K key]
    {
        get => key != null      ? _dictionary[key]
               : _nullKeyExists ? _nullKeyEntry
                                  : throw new KeyNotFoundException(); //Throw exception if key not found
        set
        {
            _version++;
            if( key != null )
                _dictionary[key] = value; //Set value for non-null key
            else
            {
                _nullKeyEntry  = value; //Set value for null key
                _nullKeyExists = true;
            }
        }
    }

    //Method to add a key-value pair to the dictionary
    public void Add(K key, V value)
    {
        if( key != null )
            _dictionary.Add(key, value); //Add non-null key-value pair
        else
        {
            if( _nullKeyExists )
                throw new ArgumentException("An element with the same key already exists in the dictionary."); //Throw exception if null key already exists

            _nullKeyEntry  = value; //Set value for null key
            _nullKeyExists = true;
        }

        _version++;
    }

    //Method to try to add a key-value pair to the dictionary without throwing an exception
    public bool TryAdd(K key, V value)
    {
        if( key != null )
        {
            if( _dictionary.ContainsKey(key) )
                return false;            //Return false if non-null key already exists
            _dictionary.Add(key, value); //Add non-null key-value pair
            _version++;
            return true;
        }

        if( _nullKeyExists )
            return false; //Return false if null key already exists

        _nullKeyEntry  = value; //Set value for null key
        _nullKeyExists = true;
        _version++;
        return true;
    }

    //Property to get the count of key-value pairs in the dictionary
    public int Count => _dictionary.Count + (_nullKeyExists ?
                                                 1 :
                                                 0);

    //Method to remove a key-value pair from the dictionary
    public bool Remove(K key)
    {
        if( key != null ) //Remove non-null key-value pair
            if( _dictionary.Remove(key) )
            {
                _version++;
                return true;
            }
            else
                return false;

        if( !_nullKeyExists )
            return false; //Return false if null key does not exist

        _nullKeyEntry  = default(V); //Reset value for null key
        _nullKeyExists = false;
        _version++;
        return true;
    }

    //Method to clear the dictionary
    public void Clear()
    {
        _version++;

        _dictionary.Clear();         //Clear non-null key-value pairs
        _nullKeyExists = false;      //Reset flag for null key
        _nullKeyEntry  = default(V); //Reset value for null key
    }

    //Method to try to get the value associated with a key
    public bool TryGetValue(K key, out V value)
    {
        if( key != null )
            return _dictionary.TryGetValue(key, out value); //Try to get value for non-null key

        if( _nullKeyExists )
        {
            value = _nullKeyEntry; //Get value for null key
            return true;
        }

        value = default(V); //Set default value
        return false;
    }

    //Method to check if a key exists in the dictionary
    public bool ContainsKey(K key) => key != null ?
                                          _dictionary.ContainsKey(key) :
                                          _nullKeyExists;

    //Method to check if the dictionary equals another object
    public override bool Equals(object? obj) => obj is NullableKeyDictionary<K, V> other && Equals(other);

    //Method to check if the dictionary equals another dictionary
    public bool Equals(NullableKeyDictionary<K, V>? other)
    {
        if( other             == null                 ||
            _nullKeyExists    != other._nullKeyExists ||
            _dictionary.Count != other._dictionary.Count )
            return false;

        if( _nullKeyExists )
            if( _nullKeyEntry == null )
            {
                if( other._nullKeyEntry != null )
                    return false;
            }
            else if( !_nullKeyEntry.Equals(other._nullKeyEntry) )
                return false;

        foreach( var pair in _dictionary )
            if( !other._dictionary.TryGetValue(pair.Key, out var value) )
                if( value == null )
                {
                    if( pair.Value != null )
                        return false;
                }
                else if( !value.Equals(pair) )
                    return false;

        return true;
    }

    //Method to get the hash code of the dictionary
    public override int GetHashCode()
    {
        var hash = 0x1b873593;
        hash = HashCode.Combine(hash, _dictionary.GetHashCode());
        hash = HashCode.Combine(hash, _nullKeyExists);
        return HashCode.Combine(hash, _nullKeyEntry);
    }

    //Method to get an enumerator for the dictionary
    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => new Enumerator(this);

    //Method to get an enumerator for the dictionary (required for the IEnumerable interface)
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    public Enumerator GetEnumerator_() => new Enumerator(this);

    public struct Enumerator : IEnumerator<KeyValuePair<K, V>>{
        private readonly NullableKeyDictionary<K, V> _src;
        private readonly int                         _version;
        private          Dictionary<K, V>.Enumerator _src_enum;
        private          bool                        _on_src_enum = true;

        internal Enumerator(NullableKeyDictionary<K, V> dictionary)
        {
            _src      = dictionary;
            _src_enum = dictionary._dictionary.GetEnumerator();
            _version  = dictionary._version;
        }

        public bool MoveNext()
        {
            if( _version != _src._version )
                throw new InvalidOperationException();

            return _on_src_enum && ((_on_src_enum = _src_enum.MoveNext()) || _src._nullKeyExists);
        }

        public void Dispose() { }

        void IEnumerator.Reset()
        {
            if( _version != _src._version )
                throw new InvalidOperationException();

            _on_src_enum = true;
            _src_enum    = _src._dictionary.GetEnumerator();
        }

        object? IEnumerator.Current => Current;

        public KeyValuePair<K?, V> Current
        {
            get
            {
                if( _version != _src._version )
                    throw new InvalidOperationException();

                return _on_src_enum ?
                           new KeyValuePair<K?, V>(_src_enum.Current.Key, _src_enum.Current.Value) :
                           new KeyValuePair<K?, V>(null,                  _src._nullKeyEntry);
            }
        }
    }
}