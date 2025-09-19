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
///Supports a special null key and allows associating null values with any key.
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
public class NullableByteMap<K, V> : IDictionary<K?, V?>, IReadOnlyDictionary<K?, V?>, IEquatable<NullableByteMap<K, V>>
    where K : unmanaged
{
    protected NullableByteSet<K> keys;     //Bit array tracking all present keys (0-255 range)
    protected NullableByteSet<K> nullsVal; //Bit array tracking keys with non-null values
    protected V[] values;                  //Array storing non-null values
    protected V? _nullKeyValue;            //Dedicated storage for null key's value
    protected static EqualityComparer<V> equal_hash_V = EqualityComparer<V>.Default;

    ///<summary>
    ///Gets the value associated with the null key, if present.
    ///</summary>
    public V? NullKeyValue => _nullKeyValue;

    public bool HasNullKey => keys._hasNullKey;

    ///<summary>
    ///Initializes a new instance of NullableByteMap with default capacity, validating that K is either one byte type.
    ///Starts with the Compressed (Rank-Based) Strategy.
    ///</summary>
    ///<exception cref="ArgumentException">Thrown if K is not one byte type.</exception>
    public NullableByteMap()
    {
        if (Unsafe.SizeOf<K>() != 1)
            throw new ArgumentException("ByteSet only supports one byte type as key types.");

        nullsVal = [];
        keys = [];
        values = new V[16];
    }

    public NullableByteMap(int capacity)
    {
        if (Unsafe.SizeOf<K>() != 1)
            throw new ArgumentException("ByteSet only supports one byte type as key types.");

        nullsVal = [];
        keys = [];
        values = new V[GetPowerOfTwo(capacity)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetPowerOfTwo(int capacity) => capacity <= 0 ? 16 : (int)BitOperations.RoundUpToPowerOf2((uint)capacity);

    ///<summary>
    ///Determines whether the specified NullableByteMap is equal to the current instance.
    ///</summary>
    ///<param name="other">The NullableByteMap to compare with the current instance.</param>
    ///<returns>True if the maps are equal, false otherwise.</returns>
    public bool Equals(NullableByteMap<K, V>? other)
    {
        if (other == this)
            return true;
        if (other == null ||
            !keys.Equals(other.keys) ||
            keys._hasNullKey && !equal_hash_V.Equals(_nullKeyValue, other.NullKeyValue) ||
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
    public V? this[K? key]
    {
        get
        {
            if (key == null)
                return keys._hasNullKey ? _nullKeyValue : default;

            var k = key.Value;
            var _key = Unsafe.As<K, byte>(ref k);
            if (!keys.Get(_key))
                return default;

            return nullsVal.Get(_key) ? values[IsFlat ? _key : nullsVal.Rank(_key) - 1] : default;
        }
        set => Add_(key, value);
    }

    ///<summary>
    ///Adds or updates a key-value pair in the map.
    ///Handles the transition to the Flat strategy when the 128th non-null value is added.
    ///</summary>
    ///<param name="key">The key to add or update (can be null).</param>
    ///<param name="value">The value to associate with the key (can be null).</param>
    ///<returns>True if the key was added, false if it was updated.</returns>
    public bool Add_(K? key, V? value)
    {
        if (key == null)
        {
            _nullKeyValue = value;
            return keys.Set1();
        }

        var k = key.Value;
        var _key = Unsafe.As<K, byte>(ref k);

        if (equal_hash_V.Equals(value, default))
        {
            if (nullsVal.Set0(_key) && !IsFlat)
                BitList.Resize(values, values, nullsVal.Rank(_key), nullsVal.Count + 1, -1);

            return keys.Set1(_key);
        }

        if (nullsVal[_key])
        {
            values[IsFlat ? _key : nullsVal.Rank(_key) - 1] = value!;
            return false;
        }

        if (IsFlat)
            values[_key] = value!;
        else if (nullsVal.Count == 128 && !nullsVal[_key]) //Switch to Flat (One-to-One) Strategy
        {
            var values_ = new V[256];
            for (int key_ = -1, ii = 0; (key_ = keys.Next1(key_)) != -1;)
                if (nullsVal[key_])
                    values_[key_] = values[ii++];

            (values = values_)[_key] = value!;
        }
        else
        {
            var r = nullsVal.Rank(_key); //Get the rank for the key

            BitList.Resize(values,
                           nullsVal.Count < values.Length ? values : values = new V[values.Length * 2], r, nullsVal.Count, 1);
            values[r] = value!;
        }

        nullsVal.Set1(_key);

        return keys.Set1(_key);
    }

    ///<summary>
    ///Indicates whether the map is using the Flat (One-to-One) Strategy.
    ///</summary>
    protected bool IsFlat => values.Length == 256;

    ///<summary>Gets the collection of keys in the map.</summary>
    public ICollection<K?> Keys => new KeyCollection(this);

    IEnumerable<K?> IReadOnlyDictionary<K?, V?>.Keys => Keys;

    ///<summary>Gets the collection of values in the map.</summary>
    public ICollection<V?> Values => new ValueCollection(this);

    IEnumerable<V?> IReadOnlyDictionary<K?, V?>.Values => Values;

    ///<summary>Gets the number of key-value pairs in the map, including the null key if present.</summary>
    public int Count => keys.Count;

    ///<summary>Gets the current capacity of the values array.</summary>
    public int Capacity => values.Length;

    ///<summary>Gets a value indicating whether the map is read-only (always false).</summary>
    public bool IsReadOnly => false;

    ///<summary>Adds a key-value pair to the map.</summary>
    public void Add(K? key, V? value) => this[key] = value;

    ///<summary>Adds a key-value pair from a KeyValuePair structure.</summary>
    public void Add(KeyValuePair<K?, V?> item) => this[item.Key] = item.Value;

    ///<summary>Checks if the specified key exists in the map.</summary>
    public bool ContainsKey(K? key)
    {
        if (!key.HasValue)
            return keys._hasNullKey;
        var k = key.Value;
        return keys.Get(Unsafe.As<K, byte>(ref k));
    }

    ///<summary>Removes the specified key and its associated value from the map.</summary>
    public bool Remove(K? key)
    {
        if (key == null)
        {
            _nullKeyValue = default!;
            return keys.Set0();
        }

        var k = key.Value;
        var _key = Unsafe.As<K, byte>(ref k);
        if (!keys.Set0(_key))
            return false;

        if (nullsVal.Set0(_key) && !IsFlat)
            BitList.Resize(values, values, nullsVal.Rank(_key), nullsVal.Count + 1, -1); //Resize to remove the value, shifting elements to fill the gap.
        return true;
    }

    ///<summary>Removes a specific key-value pair if it exists.</summary>
    public bool Remove(KeyValuePair<K?, V?> item)
    {
        if (!TryGetValue(item.Key, out var current) || !equal_hash_V.Equals(current, item.Value))
            return false;
        return Remove(item.Key);
    }

    ///<summary>Tries to get the value associated with the specified key.</summary>
    public bool TryGetValue(K? key, out V? value)
    {
        value = this[key];
        return ContainsKey(key);
    }

    ///<summary>Removes all key-value pairs from the map.</summary>
    public void Clear()
    {
        keys.Clear();
        nullsVal.Clear();
        _nullKeyValue = default;
    }

    ///<summary>Checks if the map contains the specified key-value pair.</summary>
    public bool Contains(KeyValuePair<K?, V?> item) => TryGetValue(item.Key, out var value) && equal_hash_V.Equals(value, item.Value);

    ///<summary>Copies all key-value pairs to an dst starting at the specified index.</summary>
    public void CopyTo(KeyValuePair<K?, V?>[] dst, int dstIndex)
    {
        ArgumentNullException.ThrowIfNull(dst);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
        if (dst.Length - dstIndex < Count)
            throw new ArgumentException("Destination array is not long enough.");

        foreach (var kvp in this)
            dst[dstIndex++] = kvp;
    }

    ///<summary>
    ///A read-only collection of keys in the NullableByteMap.
    ///</summary>
    private class KeyCollection : ICollection<K?>
    {
        private readonly NullableByteMap<K, V> _map;

        public KeyCollection(NullableByteMap<K, V> map) => _map = map;

        ///<summary>Gets the number of keys in the collection.</summary>
        public int Count => _map.Count;

        ///<summary>Gets a value indicating whether the collection is read-only (always true).</summary>
        public bool IsReadOnly => true;

        ///<summary>Not supported - throws NotSupportedException.</summary>
        public void Add(K? item) => throw new NotSupportedException();

        ///<summary>Not supported - throws NotSupportedException.</summary>
        public void Clear() => throw new NotSupportedException();

        ///<summary>Checks if the collection contains the specified key.</summary>
        public bool Contains(K? item) => _map.ContainsKey(item);

        ///<summary>Not supported - throws NotSupportedException.</summary>
        public bool Remove(K? item) => throw new NotSupportedException();

        ///<summary>Copies all keys to an dst starting at the specified index.</summary>
        public void CopyTo(K?[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
            if (dst.Length - dstIndex < Count)
                throw new ArgumentException("Destination array is not long enough.");

            foreach (var kvp in _map)
                dst[dstIndex++] = kvp.Key;
        }

        ///<summary>Gets an enumerator for iterating over the keys.</summary>
        public IEnumerator<K?> GetEnumerator() => new KeyEnumerator(_map);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    ///<summary>
    ///A read-only collection of values in the NullableByteMap.
    ///</summary>
    private class ValueCollection : ICollection<V?>
    {
        private readonly NullableByteMap<K, V> _map;

        public ValueCollection(NullableByteMap<K, V> map) => _map = map;

        ///<summary>Gets the number of values in the collection.</summary>
        public int Count => _map.Count;

        ///<summary>Gets a value indicating whether the collection is read-only (always true).</summary>
        public bool IsReadOnly => true;

        ///<summary>Not supported - throws NotSupportedException.</summary>
        public void Add(V? item) => throw new NotSupportedException();

        ///<summary>Not supported - throws NotSupportedException.</summary>
        public void Clear() => throw new NotSupportedException();

        ///<summary>Checks if the collection contains the specified value.</summary>
        public bool Contains(V? item) => _map.Any(kvp => equal_hash_V.Equals(kvp.Value, item));

        ///<summary>Not supported - throws NotSupportedException.</summary>
        public bool Remove(V? item) => throw new NotSupportedException();

        ///<summary>Copies all values to an dst starting at the specified index.</summary>
        public void CopyTo(V?[] dst, int dstIndex)
        {
            ArgumentNullException.ThrowIfNull(dst);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)dstIndex, (uint)dst.Length);
            if (dst.Length - dstIndex < Count)
                throw new ArgumentException("Destination array is not long enough.");

            foreach (var kvp in _map)
                dst[dstIndex++] = kvp.Value;
        }

        ///<summary>Gets an enumerator for iterating over the values.</summary>
        public IEnumerator<V?> GetEnumerator() => new ValueEnumerator(_map);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
    IEnumerator<KeyValuePair<K?, V>> IEnumerable<KeyValuePair<K?, V>>.GetEnumerator() => GetEnumerator();

    ///<summary>
    ///Enumerator for iterating over key-value pairs in the NullableByteMap.
    ///</summary>
    public struct Enumerator : IEnumerator<KeyValuePair<K?, V?>>
    {
        private readonly NullableByteMap<K, V> _map;
        private int _key; //Current key being enumerated (NULL, INVALID, or byte 0-255)
        private uint _version;
        private int _valIndex; //Index into the compressed 'values' array for non-flat strategy

        internal Enumerator(NullableByteMap<K, V> map)
        {
            _map = map;
            Reset();
        }

        ///<summary>Moves to the next key-value pair.</summary>
        public bool MoveNext()
        {
            if (_version != _map.keys._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            if (_map.Count == 0)
                return false;

            if (++_key == -1)
            {
                _current = new KeyValuePair<K?, V>(null, _map._nullKeyValue);
                return true;
            }

            if (_key == int.MaxValue)
                return false;
            if ((_key = _map.keys.Next1(_key)) == -1)
            {
                _key = int.MaxValue;
                return false;
            }

            _current = new KeyValuePair<K?, V>(Unsafe.As<int, K>(ref _key), _map.nullsVal[_key] ? _map.values[_map.IsFlat ? _key : ++_valIndex] : default);

            return true;
        }

        private KeyValuePair<K?, V> _current;

        public KeyValuePair<K?, V> Current => _version != _map.keys._version ? throw new InvalidOperationException("Collection was modified during enumeration.") : _key == int.MaxValue || _key == -2 || _key == -1 && !_map.HasNullKey ? throw new InvalidOperationException("Enumeration has either not started or has finished.")
                                                                                                                                                                                                                                         : _current;

        object IEnumerator.Current => Current;

        ///<summary>Resets the enumerator to its initial state.</summary>
        public void Reset()
        {
            _key = _map.HasNullKey ? -2 : -1;
            _valIndex = -1;
            _version = _map.keys._version;
            _current = default;
        }

        ///<summary>Disposes the enumerator (no-op).</summary>
        public void Dispose() { }
    }

    ///<summary>
    ///Enumerator for iterating over values in the NullableByteMap.
    ///</summary>
    private struct ValueEnumerator : IEnumerator<V?>
    {
        private readonly NullableByteMap<K, V> _map;
        private int _key;
        private uint _version;
        private int valIndex;
        private V? currentValue;

        internal ValueEnumerator(NullableByteMap<K, V> map)
        {
            _map = map;
            _key = -1;
            _version = map.keys._version;
        }

        ///<summary>Gets the current value.</summary>
        public V? Current => currentValue;

        object IEnumerator.Current => Current;

        ///<summary>Moves to the next value.</summary>
        public bool MoveNext()
        {
            if (_version != _map.keys._version)
                throw new InvalidOperationException("Collection was modified");

            if ((_key = _map.keys.Next1(_key)) == -1)
            {
                currentValue = default;
                return false;
            }

            currentValue = _map.nullsVal[_key] ? _map.values[_map.IsFlat ? _key : ++valIndex] : default;
            return true;
        }

        ///<summary>Resets the enumerator to its initial state.</summary>
        public void Reset()
        {
            _key = -1;
            valIndex = -1;
            currentValue = default;
            _version = _map.keys._version;
        }

        ///<summary>Disposes the enumerator (no-op).</summary>
        public void Dispose() { currentValue = default; }
    }

    ///<summary>
    ///Enumerator for iterating over keys in the NullableByteMap.
    ///</summary>
    private struct KeyEnumerator : IEnumerator<K?>
    {
        private readonly NullableByteMap<K, V> _map;
        private int _key;
        private uint _version;

        internal KeyEnumerator(NullableByteMap<K, V> map)
        {
            _map = map;
            Reset();
        }

        ///<summary>Gets the current key.</summary>
        public K? Current => _version != _map.keys._version ? throw new InvalidOperationException("Collection was modified during enumeration.") : _key == int.MaxValue - 1 || _key == -2 || _key == -1 && !_map.HasNullKey ? throw new InvalidOperationException("Enumeration has either not started or has finished.")
                                                                                                                                               : _key == -1 ? null
                                                                                                                                                                                                                            : Unsafe.As<int, K>(ref _key);

        object IEnumerator.Current => Current;

        ///<summary>Moves to the next key.</summary>
        public bool MoveNext()
        {
            if (_version != _map.keys._version)
                throw new InvalidOperationException("Collection was modified during enumeration.");

            if (_map.Count == 0)
                return false;

            if (++_key == -1 || _key != int.MaxValue && (_key = _map.keys.Next1(_key)) != -1)
                return true;
            _key = int.MaxValue - 1;
            return false;
        }

        ///<summary>Resets the enumerator to its initial state.</summary>
        public void Reset()
        {
            _key = _map.HasNullKey ? -2 : -1;
            _version = _map.keys._version;
        }

        ///<summary>Disposes the enumerator (no-op).</summary>
        public void Dispose() { }
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
            if (e.Current.Key == null)
                json.Append("null:").Append(e.Current.Value);
            else
                json.Append($"{e.Current.Key}:").Append(e.Current.Value);
            json.Append(',');
        }

        if (1 < json.Length)
            json.Length -= 1;
        json.Append('}');
        return json.ToString();
    }
}