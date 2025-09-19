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
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace org.unirail.collections;

///<summary>
///A specialized dictionary that maps keys of type K (restricted to one byte type) to values of type V.
///Allows associating null values with any key.
///<para>
///The map employs two internal strategies for storing values to optimize for both space and performance:
///<list type="number">
///  <item>
///    <b>Compressed (Rank-Based) Strategy:</b> used when fewer than 128 keys have non-null values.
///    Stores only non-null values in a compact array, using a bitset to track keys with non-null values.
///  </item>
///  <item>
///    <b>Flat (One-to-One) Strategy:</b> Activated when 128 or more keys have non-null values.
///    Uses a fixed-size array of 256 elements for direct O(1) access, with each index corresponding to a byte key.
///  </item>
///</list>
///The transition between strategies occurs automatically when the 128th non-null value is added.
///</para>
///</summary>
///<typeparam name="K">The key type, must be a struct (one byte type).</typeparam>
///<typeparam name="V">The value type, can be any type.</typeparam>
public class ByteMap<K, V> : IDictionary<K, V>, IReadOnlyDictionary<K, V>, IEquatable<ByteMap<K, V>>
    where K : unmanaged
{
    protected ByteSet<K> keys;     //Bit array tracking all present keys (0-255 range)
    protected ByteSet<K> nullsVal; //Bit array tracking keys with non-null values
    protected V[] values;          //Array storing non-null values
    protected static EqualityComparer<V> equal_hash_V = EqualityComparer<V>.Default;

    ///<summary>
    ///Initializes a new instance of ByteMap with default capacity, validating that K is either one byte type.
    ///Starts with the Compressed (Rank-Based) Strategy.
    ///</summary>
    ///<exception cref="ArgumentException">Thrown if K is not one byte type.</exception>
    public ByteMap()
    {
        if (Unsafe.SizeOf<K>() != 1)
            throw new ArgumentException("ByteSet only supports one byte type as key types.");

        nullsVal = [];
        keys = [];
        values = new V[16];
    }

    public ByteMap(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(capacity, 256);
        ArgumentOutOfRangeException.ThrowIfNegative(value: capacity);
        if (Unsafe.SizeOf<K>() != 1)
            throw new ArgumentException("ByteSet only supports one byte type as key types.");

        nullsVal = [];
        keys = [];
        values = new V[capacity <= 0 ? 16 : (int)BitOperations.RoundUpToPowerOf2((uint)capacity)];
    }

    ///<summary>
    ///Indicates whether the map is using the Flat (One-to-One) Strategy.
    ///</summary>
    public bool IsFlat => values.Length == 256;

    ///<summary>
    ///Determines whether the specified ByteMap is equal to the current instance.
    ///</summary>
    ///<param name="other">The ByteMap to compare with the current instance.</param>
    ///<returns>True if the maps are equal, false otherwise.</returns>
    public bool Equals(ByteMap<K, V>? other)
    {
        if (other == this)
            return true;
        if (other == null ||
            !keys.Equals(other.keys) ||
            Count != other.Count ||
            !nullsVal.Equals(other.nullsVal))
            return false;

        for (int t = -1, i = 0; (t = nullsVal.Next1(t)) != -1; i++)
            if (!equal_hash_V.Equals(IsFlat ? values[t] : values[i], other.IsFlat ? other.values[t] : other.values[i]))
                return false;
        return true;
    }

    ///<summary>
    ///Gets or sets the value associated with the specified key.
    ///Returns default(V) if the key is not found.
    ///When setting, a null value marks the key as present with a null value.
    ///</summary>
    public V this[K key]
    {
        get
        {
            var _key = Unsafe.As<K, byte>(ref key);
            return keys.Get(_key) && nullsVal.Get(_key) ? values[IsFlat ? _key : nullsVal.Rank(_key) - 1] : default;
        }
        set => Add_(key, value);
    }

    ///<summary>
    ///Adds or updates a key-value pair in the map.
    ///Handles the transition to the Flat strategy when the 128th non-null value is added.
    ///</summary>
    ///<param name="key">The key to add or update.</param>
    ///<param name="value">The value to associate with the key (can be null).</param>
    ///<returns>True if the key was added, false if it was updated.</returns>
    public bool Add_(K key, V value)
    {
        var _key = Unsafe.As<K, byte>(ref key);

        if (equal_hash_V.Equals(value, default))
        {
            if (nullsVal.Set0(_key) && !IsFlat)
                BitList.Resize(values, values, nullsVal.Rank(_key), nullsVal.Count + 1, -1);
            return keys.Set1(_key);
        }

        if (nullsVal[_key])
        {
            values[IsFlat ? _key : nullsVal.Rank(_key) - 1] = value;
            return false;
        }

        if (IsFlat)
            values[_key] = value;
        else if (nullsVal.Count == 128 && !nullsVal[_key])
        {
            var values_ = new V[256];
            for (int key_ = -1, ii = 0; (key_ = keys.Next1(key_)) != -1;)
                if (nullsVal[key_])
                    values_[key_] = values[ii++];
            (values = values_)[_key] = value;
        }
        else
        {
            var r = nullsVal.Rank(_key);
            BitList.Resize(values,
                           nullsVal.Count < values.Length ? values : values = new V[values.Length * 2],
                           r, nullsVal.Count, 1);
            values[r] = value;
        }

        nullsVal.Set1(_key);
        return keys.Set1(_key);
    }

    ///<summary>Gets the collection of keys in the map.</summary>
    public ICollection<K> Keys => new KeyCollection(this);

    IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;

    ///<summary>Gets the collection of values in the map.</summary>
    public ICollection<V> Values => new ValueCollection(this);

    IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;

    ///<summary>Gets the number of key-value pairs in the map.</summary>
    public int Count => keys.Count;

    ///<summary>Gets the current capacity of the values array.</summary>
    public int Capacity => values.Length;

    ///<summary>Gets a value indicating whether the map is read-only (always false).</summary>
    public bool IsReadOnly => false;

    ///<summary>Adds a key-value pair to the map.</summary>
    public void Add(K key, V value) => this[key] = value;

    ///<summary>Adds a key-value pair from a KeyValuePair structure.</summary>
    public void Add(KeyValuePair<K, V> item) => this[item.Key] = item.Value;

    ///<summary>Checks if the specified key exists in the map.</summary>
    public bool ContainsKey(K key)
    {
        var _key = Unsafe.As<K, byte>(ref key);
        return keys.Get(_key);
    }

    ///<summary>Removes the specified key and its associated value from the map.</summary>
    public bool Remove(K key)
    {
        var _key = Unsafe.As<K, byte>(ref key);
        if (!keys.Set0(_key))
            return false;

        if (nullsVal.Set0(_key) && !IsFlat)
            BitList.Resize(values, values, nullsVal.Rank(_key), nullsVal.Count + 1, -1);
        return true;
    }

    ///<summary>Removes a specific key-value pair if it exists.</summary>
    public bool Remove(KeyValuePair<K, V> item)
    {
        if (!TryGetValue(item.Key, out var current) || !equal_hash_V.Equals(current, item.Value))
            return false;
        return Remove(item.Key);
    }

    ///<summary>Tries to get the value associated with the specified key.</summary>
    public bool TryGetValue(K key, out V value)
    {
        value = this[key];
        return ContainsKey(key);
    }

    ///<summary>Removes all key-value pairs from the map.</summary>
    public void Clear()
    {
        keys.Clear();
        nullsVal.Clear();
    }

    ///<summary>Checks if the map contains the specified key-value pair.</summary>
    public bool Contains(KeyValuePair<K, V> item) => TryGetValue(item.Key, out var value) && equal_hash_V.Equals(value, item.Value);

    ///<summary>Copies all key-value pairs to an dst starting at the specified index.</summary>
    public void CopyTo(KeyValuePair<K, V>[] dst, int dstIndex)
    {
        ArgumentNullException.ThrowIfNull(dst);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
        if (dst.Length - dstIndex < Count)
            throw new ArgumentException("Destination array is not long enough.");

        foreach (var kvp in this)
            dst[dstIndex++] = kvp;
    }

    ///<summary>
    ///Returns an enumerator that iterates through the dictionary's key-value pairs,
    ///including the null key if it exists.
    ///</summary>
    ///<returns>An enumerator for the dictionary.</returns>
    public Enumerator GetEnumerator() => new Enumerator(this);

    ///<summary>
    ///Returns an enumerator that iterates through the dictionary's key-value pairs.
    ///</summary>
    ///<returns>An <see cref="IEnumerator"/> object that can be used to iterate through the dictionary.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    ///<summary>
    ///Returns an enumerator that iterates through the dictionary's key-value pairs.
    ///</summary>
    ///<returns>An <see cref="IEnumerator{KeyValuePair{K?, V}}"/> that can be used to iterate through the dictionary.</returns>
    IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() => GetEnumerator();

    ///<summary>
    ///Enumerator for iterating over key-value pairs in the ByteMap.
    ///</summary>
    public struct Enumerator : IEnumerator<KeyValuePair<K, V>>
    {
        private readonly ByteMap<K, V> _map;
        private int _key;
        private uint _version;
        private int valIndex;
        private V currentValue;

        internal Enumerator(ByteMap<K, V> map)
        {
            _map = map;
            Reset();
        }

        public KeyValuePair<K, V> Current => _version != _map.keys._version ? throw new InvalidOperationException("Collection was modified during enumeration.") : _key is ByteSet<K>.INVALID or int.MaxValue ? //Check if enumeration has not started or has finished
                                                                                                                                                                   throw new InvalidOperationException("Enumeration has either not started or has finished.")
                                                                                                                                                                                                              : new KeyValuePair<K, V>(Unsafe.As<int, K>(ref _key), currentValue);

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_version != _map.keys._version)
                throw new InvalidOperationException("Collection was modified");

            if (_key == int.MaxValue)
                return false;

            if ((_key = _map.keys.Next1(_key)) == ByteSet<K>.INVALID)
            {
                _key = int.MaxValue;
                return false;
            }

            currentValue = _map.nullsVal[_key] ? _map.values[_map.IsFlat ? _key : ++valIndex] : default;
            return true;
        }

        public void Reset()
        {
            _key = ByteSet<K>.INVALID;
            valIndex = -1;
            currentValue = default;
            _version = _map.keys._version;
        }

        public void Dispose() { currentValue = default; }
    }

    ///<summary>
    ///Returns a string representation of the map in JSON-like format.
    ///</summary>
    public override string ToString()
    {
        var json = new StringBuilder();
        json.Append('{');

        using var e = GetEnumerator();
        while (e.MoveNext())
        {
            json.Append('\n');
            json.Append($"{e.Current.Key}:{e.Current.Value},");
        }

        if (json.Length > 1)
            json.Length -= 1;
        json.Append('}');
        return json.ToString();
    }

    ///<summary>
    ///A read-only collection of keys in the ByteMap.
    ///</summary>
    private class KeyCollection : ICollection<K>
    {
        private readonly ByteMap<K, V> _map;

        public KeyCollection(ByteMap<K, V> map) => _map = map;

        public int Count => _map.Count;

        public bool IsReadOnly => true;

        public void Add(K item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(K item) => _map.ContainsKey(item);

        public bool Remove(K item) => throw new NotSupportedException();

        public void CopyTo(K[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
            if (dst.Length - dstIndex < Count)
                throw new ArgumentException("Destination array is not long enough.");

            foreach (var kvp in _map)
                dst[dstIndex++] = kvp.Key;
        }

        public IEnumerator<K> GetEnumerator() => new KeyEnumerator(_map);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        ///<summary>
        ///Enumerator for iterating over keys in the ByteMap.
        ///</summary>
        private struct KeyEnumerator : IEnumerator<K>
        {
            private readonly ByteMap<K, V> _map;
            private int _key;
            private uint _version;

            internal KeyEnumerator(ByteMap<K, V> map)
            {
                _map = map;
                Reset();
            }

            public K Current => _version != _map.keys._version ? throw new InvalidOperationException("Collection was modified during enumeration.") : _key == ByteSet<K>.INVALID ? //Check if enumeration has not started or has finished
                                                                                                                                                      throw new InvalidOperationException("Enumeration has either not started or has finished.")
                                                                                                                                                                                 : Unsafe.As<int, K>(ref _key);

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_version != _map.keys._version)
                    throw new InvalidOperationException("Collection was modified");

                return (_key = _map.keys.Next1(_key)) != ByteSet<K>.INVALID;
            }

            public void Reset()
            {
                _key = ByteSet<K>.INVALID;
                _version = _map.keys._version;
            }

            public void Dispose() { }
        }
    }

    ///<summary>
    ///A read-only collection of values in the ByteMap.
    ///</summary>
    private class ValueCollection : ICollection<V>
    {
        private readonly ByteMap<K, V> _map;

        public ValueCollection(ByteMap<K, V> map) => _map = map;

        public int Count => _map.Count;

        public bool IsReadOnly => true;

        public void Add(V item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(V item) => _map.Any(kvp => equal_hash_V.Equals(kvp.Value, item));

        public bool Remove(V item) => throw new NotSupportedException();

        public void CopyTo(V[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
            if (dst.Length - dstIndex < Count)
                throw new ArgumentException("Destination array is not long enough.");

            foreach (var kvp in _map)
                dst[dstIndex++] = kvp.Value;
        }

        public IEnumerator<V> GetEnumerator() => new ValueEnumerator(_map);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        ///<summary>
        ///Enumerator for iterating over values in the ByteMap.
        ///</summary>
        private struct ValueEnumerator : IEnumerator<V>
        {
            private readonly ByteMap<K, V> _map;
            private int _key;
            private uint _version;
            private int valIndex;
            private V currentValue;

            internal ValueEnumerator(ByteMap<K, V> map)
            {
                _map = map;
                Reset();
            }

            public V Current => _version != _map.keys._version ? throw new InvalidOperationException("Collection was modified during enumeration.") : _key == ByteSet<K>.INVALID ? //Check if enumeration has not started or has finished
                                                                                                                                                      throw new InvalidOperationException("Enumeration has either not started or has finished.")
                                                                                                                                                                                 : currentValue;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_version != _map.keys._version)
                    throw new InvalidOperationException("Collection was modified");

                if ((_key = _map.keys.Next1(_key)) == ByteSet<K>.INVALID)
                {
                    currentValue = default;
                    return false;
                }

                currentValue = _map.nullsVal[_key] ? _map.values[_map.IsFlat ? _key : ++valIndex] : default;
                return true;
            }

            public void Reset()
            {
                _key = ByteSet<K>.INVALID;
                valIndex = -1;
                currentValue = default;
                _version = _map.keys._version;
            }

            public void Dispose() { currentValue = default; }
        }
    }
}