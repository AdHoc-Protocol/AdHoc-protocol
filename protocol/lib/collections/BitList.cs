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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace org.unirail.collections;

public interface BitList<T>
    where T : struct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static byte to(T src) => Unsafe.As<T, byte>(ref src);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static T from(bool src) => Unsafe.As<bool, T>(ref src);

    abstract class R : ICloneable, IEquatable<R>
    {
        protected int _count;

        public virtual int Count
        {
            get => _count;
            protected
                set
            {
            }
        }

        protected internal ulong[] values = Array.Empty<ulong>();

        protected static int len4bits(int bits) => 1 + (bits >> LEN);

        public const int LEN = 6;          //long has 64 bits. 6 bits in mask = 63.
        public const int BITS = 1 << LEN;  //64
        public const uint MASK = BITS - 1; //63

        protected static uint index(uint item_X_bits) => item_X_bits >> LEN;

        protected static ulong mask(int bits) => (1UL << bits) - 1;

        public const int OI = int.MaxValue;
        public const int IO = int.MinValue;

        protected internal int _used;

        protected internal int used()
        {
            if (-1 < _used)
                return _used;
            _used &= OI;
            var i = _used - 1;
            while (-1 < i && values[i] == 0)
                i--;
            return _used = i + 1;
        }

        protected internal int used(int bit)
        {
            if (Count <= bit)
                _count = bit + 1;
            var index = bit >> LEN;
            if (index < used())
                return index;
            if (values.Length < (_used = index + 1))
                Array.Resize(ref values, Math.Max(2 * values.Length, _used));
            return index;
        }

        public T get(int bit)
        {
            var index = bit >> LEN;
            return from(index < used() && (values[index] & 1UL << bit) != 0);
        }

        public T get(int bit, T FALSE, T TRUE)
        {
            var index = bit >> LEN;
            return index < used() && (values[index] & 1UL << bit) != 0 ? TRUE : FALSE;
        }

        public T get(ulong[] dst, int from_bit, int to_bit)
        {
            var ret = (to_bit - from_bit - 1 >> LEN) + 1;
            var index = from_bit >> LEN;
            if ((from_bit & MASK) == 0)
                Array.Copy(values, index, dst, 0, ret - 1);
            else
                for (var i = 0; i < ret - 1; i++, index++)
                    dst[i] = values[index] >> from_bit | values[index + 1] << -from_bit;
            var mask = ~0UL >> -to_bit;
            dst[ret - 1] =
                (to_bit - 1 & MASK) < (from_bit & MASK) ? values[index] >> from_bit | (values[index + 1] & mask) << -from_bit : (values[index] & mask) >> from_bit;
            return from(ret != 0);
        }

        public int next1(int bit)
        {
            var index = bit >> LEN;
            if (used() <= index)
                return -1;
            for (var i = values[index] & ~0UL << bit; ; i = values[index])
            {
                if (i != 0)
                    return index * BITS + BitOperations.TrailingZeroCount(i);
                if (++index == _used)
                    return -1;
            }
        }

        public int next0(int bit)
        {
            var index = bit >> LEN;
            if (used() <= index)
                return bit;
            for (var i = ~values[index] & ~0UL << bit; ; i = ~values[index])
            {
                if (i != 0)
                    return index * BITS + BitOperations.TrailingZeroCount(i);
                if (++index == _used)
                    return _used * BITS;
            }
        }

        public int prev1(int bit)
        {
            var index = bit >> LEN;
            if (used() <= index)
                return last1() - 1;
            for (var i = values[index] & ~0UL >> -(bit + 1); ; i = values[index])
            {
                if (i != 0)
                    return (index + 1) * BITS - 1 - BitOperations.LeadingZeroCount(i);
                if (index-- == 0)
                    return -1;
            }
        }

        public int prev0(int bit)
        {
            var index = bit >> LEN;
            if (used() <= index)
                return bit;
            for (var i = ~values[index] & ~0UL >> -(bit + 1); ; i = ~values[index])
            {
                if (i != 0)
                    return (index + 1) * BITS - 1 - BitOperations.LeadingZeroCount(i);
                if (index-- == 0)
                    return -1;
            }
        }

        public int last1()
        {
            return used() == 0 ? 0 : BITS * (_used - 1) + BITS - BitOperations.LeadingZeroCount(values[_used - 1]);
        }

        public bool isEmpty() => _used == 0;

        public int rank(int bit)
        {
            var max = bit >> LEN;
            if (max < used())
                for (int i = 0, sum = 0; ; i++)
                    if (i < max)
                        sum += BitOperations.PopCount(values[i]);
                    else
                        return sum + BitOperations.PopCount(values[i] & ~0UL >> BITS - (bit + 1));
            return cardinality();
        }

        public int cardinality()
        {
            for (int i = 0, sum = 0; ; i++)
                if (i < used())
                    sum += BitOperations.PopCount(values[i]);
                else
                    return sum;
        }

        public int bit(int cardinality)
        {
            int i = 0, c = 0;
            while ((c += BitOperations.PopCount(values[i])) < cardinality)
                i++;
            var v = values[i];
            var z = BitOperations.LeadingZeroCount(v);
            for (var p = 1UL << BITS - 1; cardinality < c; z++)
                if ((v & p >> z) != 0)
                    c--;
            return i * 32 + BITS - z;
        }

        public int Capacity() => values.Length * BITS;

        public object Clone()
        {
            var dst = (R)MemberwiseClone();
            values = (ulong[])values.Clone();
            return dst;
        }

        public override int GetHashCode()
        {
            var hash = HashCode.Combine(197, Count);
            for (var i = used(); --i >= 0;)
                hash = HashCode.Combine(hash, values[i]);

            return hash;
        }

        public override bool Equals(object? obj) => obj != null && Equals(obj as R);

        public bool Equals(R? other) => other != null && new Span<ulong>(values, 0, used()).SequenceEqual(new Span<ulong>(other.values, 0, used()));

        public override string ToString()
        {
            return ToString(null).ToString();
        }

        public StringBuilder ToString(StringBuilder? dst)
        {
            var _size = Count;
            var max = _size / BITS;
            if (dst == null)
                dst = new StringBuilder((max + 1) * 68);
            else
                dst.EnsureCapacity(dst.Length + (max + 1) * 68);
            dst.Append(string.Format("{0,-8}{1,-8}{2,-8}{3,-8}{4,-8}{5,-8}{6,-8}{7,-7}{8}", "0", "7", "15", "23", "31", "39", "47", "55", "63"));
            dst.Append('\n');
            dst.Append(string.Format("|{0,-7}|{1,-7}|{2,-7}|{3,-7}|{4,-7}|{5,-7}|{6,-7}|{7,-6}|", "", "", "", "", "", "", "", "", ""));
            dst.Append('\n');
            for (var i = 0; i < max; i++)
            {
                var v = values[i];
                for (var s = 0; s < BITS; s++)
                    dst.Append((v & 1UL << s) == 0 ? '.' : '*');
                dst.Append(i * BITS);
                dst.Append('\n');
            }

            if (0 < (_size &= 63))
            {
                var v = values[max];
                for (var s = 0; s < _size; s++)
                    dst.Append((v & 1UL << s) == 0 ? '.' : '*');
            }

            return dst;
        }

        public virtual T this[int index]
        {
            get => get(index);
            set => throw new NotImplementedException();
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
    }

    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private readonly R _list;
        private int _index;

        private T _current;

        internal Enumerator(R list)
        {
            _list = list;
            _index = 0;
            _current = default;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            _current = _list[_index];
            _index++;
            return true;
        }

        public T Current => _current!;

        object? IEnumerator.Current => _current;

        void IEnumerator.Reset()
        {
            _index = 0;
            _current = default;
        }
    }

    class RW : R
    {
        public RW(int length)
        {
            if (0 < length)
                values = new ulong[len4bits(length)];
        }

        public RW(T default_value, int Count)
        {
            var len = len4bits(_count = Math.Abs(Count));
            values = new ulong[len];
            _used = len | IO;

            if (to(default_value) != 0 && 0 < Count)
                Set1(0, Count - 1);
        }

        public RW(R src, int from_bit, int to_bit)
        {
            if (src.Count <= from_bit)
                return;
            _count = Math.Min(to_bit, src.Count - 1) - from_bit;
            var i2 = to(src.get(to_bit)) != 0 ? to_bit : src.prev1(to_bit);
            if (i2 == -1)
                return;
            values = new ulong[(i2 - 1 >> LEN) + 1];
            _used = values.Length | IO;
            int
                i1 = to(src.get(from_bit)) != 0 ? from_bit : src.next1(from_bit),
                index = i1 >> LEN,
                max = (i2 >> LEN) + 1,
                i = 0;
            for (var v = src.values[index] >> i1; ; v >>= i1, i++)
                if (index + 1 < max)
                    values[i] = v | (v = src.values[index + i]) << BITS - i1;
                else
                {
                    values[i] = v;
                    return;
                }
        }

        public RW and(R and)
        {
            if (this == and)
                return this;
            if (and.used() < used())
                while (_used > and._used)
                    values[--_used] = 0;
            for (var i = 0; i < _used; i++)
                values[i] &= and.values[i];
            _used |= IO;
            return this;
        }

        public RW or(R or)
        {
            if (or.used() < 1 || this == or)
                return this;
            ;
            var u = _used;
            if (used() < or.used())
            {
                if (values.Length < or._used)
                    Array.Resize(ref values, Math.Max(2 * values.Length, or._used));
                _used = or._used;
            }

            var min = Math.Min(u, or._used);
            for (var i = 0; i < min; i++)
                values[i] |= or.values[i];
            if (min < or._used)
                Array.Copy(or.values, min, values, min, or._used - min);
            else if (min < u)
                Array.Copy(values, min, or.values, min, u - min);
            return this;
        }

        public RW xor(R xor)
        {
            if (xor.used() < 1 || xor == this)
                return this;
            ;
            var u = _used;
            if (used() < xor.used())
            {
                if (values.Length < xor._used)
                    Array.Resize(ref values, Math.Max(2 * values.Length, xor._used));
                _used = xor._used;
            }

            var min = Math.Min(u, xor._used);
            for (var i = 0; i < min; i++)
                values[i] ^= xor.values[i];
            if (min < xor._used)
                Array.Copy(xor.values, min, values, min, xor._used - min);
            else if (min < u)
                Array.Copy(values, min, xor.values, min, u - min);
            _used |= IO;
            return this;
        }

        public RW andNot(R not)
        {
            for (var i = Math.Min(used(), not.used()) - 1; -1 < i; i--)
                values[i] &= ~not.values[i];
            _used |= IO;
            return this;
        }

        public bool intersects(R set)
        {
            for (var i = Math.Min(_used, set._used) - 1; i >= 0; i--)
                if ((values[i] & set.values[i]) != 0)
                    return true;
            return false;
        }

        public RW flip(int bit)
        {
            var index = used(bit);
            if ((values[index] ^= 1UL << bit) == 0 && index + 1 == _used)
                _used |= IO;
            return this;
        }

        public RW flip(int from_bit, int to_bit)
        {
            if (from_bit == to_bit)
                return this;
            ;
            var from_index = from_bit >> LEN;
            var to_index = used(to_bit - 1);
            var from_mask = ~0UL << from_bit;
            var to_mask = ~0UL >> -to_bit;
            if (from_index == to_index)
            {
                if ((values[from_index] ^= from_mask & to_mask) == 0 && from_index + 1 == _used)
                    _used |= IO;
            }
            else
            {
                values[from_index] ^= from_mask;
                for (var i = from_index + 1; i < to_index; i++)
                    values[i] ^= ~0UL;
                values[to_index] ^= to_mask;
                _used |= IO;
            }

            return this;
        }

        public RW Set(int index, params T[] values)
        {
            for (int i = 0, max = values.Length; i < max; i++)
                if (to(values[i]) != 0)
                    Set1(index + i);
                else
                    Set0(index + i);
            return this;
        }

        public RW Set1(int bit)
        {
            var index = used(bit); //!!!
            values[index] |= 1UL << bit;
            return this;
        }

        public override T this[int index]
        {
            get => get(index);
            set => Set(index, value);
        }

        public RW Set(int bit, T value)
        {
            if (to(value) != 0)
                Set1(bit);
            else
                Set0(bit);
            return this;
        }

        public RW Set(int bit, T value, T TRUE)
        {
            if (to(value) == to(TRUE))
                Set1(bit);
            else
                Set0(bit);
            return this;
        }

        public RW Set1(int from_bit, int to_bit)
        {
            if (from_bit == to_bit)
                return this;
            ;
            var from_index = from_bit >> LEN;
            var to_index = used(to_bit - 1);
            var from_mask = ~0UL << from_bit;
            var to_mask = ~0UL >> -to_bit;
            if (from_index == to_index)
                values[from_index] |= from_mask & to_mask;
            else
            {
                values[from_index] |= from_mask;
                for (var i = from_index + 1; i < to_index; i++)
                    values[i] = ~0UL;
                values[to_index] |= to_mask;
            }

            return this;
        }

        public RW Set(int from_bit, int to_bit, T value)
        {
            if (to(value) != 0)
                Set1(from_bit, to_bit);
            else
                Set0(from_bit, to_bit);
            return this;
        }

        public RW Set0(int bit)
        {
            if (Count <= bit)
                _count = bit + 1;
            var index = bit >> LEN;
            if (index < used())
                if (index + 1 == _used && (values[index] &= ~(1UL << bit)) == 0)
                    _used |= IO;
                else
                    values[index] &= ~(1UL << bit);
            return this;
        }

        public RW Set0(int from_bit, int to_bit)
        {
            if (Count <= to_bit)
                _count = to_bit + 1;
            if (from_bit == to_bit)
                return this;
            ;
            var from_index = from_bit >> LEN;
            if (used() <= from_index)
                return this;
            ;
            var to_index = to_bit - 1 >> LEN;
            if (_used <= to_index)
            {
                to_bit = last1();
                to_index = _used - 1;
            }

            var from_mask = ~0UL << from_bit;
            var to_mask = ~0UL >> -to_bit;
            if (from_index == to_index)
            {
                if ((values[from_index] &= ~(from_mask & to_mask)) == 0)
                    if (from_index + 1 == _used)
                        _used |= IO;
            }
            else
            {
                values[from_index] &= ~from_mask;
                for (var i = from_index + 1; i < to_index; i++)
                    values[i] = 0;
                values[to_index] &= ~to_mask;
                _used |= IO;
            }

            return this;
        }

        public RW Add(T value) => Set(Count, value);

        public RW Add(params T[] values)
        {
            foreach (var value in values)
                Set(Count, value);
            return this;
        }

        public RW Add(ulong src) => Add(src, BITS);

        public RW Add(ulong src, int bits)
        {
            if (BITS < bits)
                bits = BITS;
            var size = _count;
            _count += bits;
            if ((src &= ~(1UL << bits - 1)) == 0)
                return this;
            used(size + BITS - BitOperations.LeadingZeroCount(src));
            var bit = (int)(size & MASK);
            if (bit == 0)
                values[index((uint)_count)] = src;
            else
            {
                values[index((uint)size)] &= src << bit | mask(bit);
                if (index((uint)size) < index((uint)_count))
                    values[index((uint)_count)] = src >> bit;
            }

            return this;
        }

        public RW Add(int key, T value)
        {
            if (key < last1())
            {
                var index = key >> LEN;
                ulong m = ~0UL << key, v = values[index];
                m = (v & m) << 1 | v & ~m;
                if (to(value) != 0)
                    m |= 1UL << key;
                while (++index < _used)
                {
                    values[index - 1] = m;
                    var t = v >> BITS - 1;
                    v = values[index];
                    m = v << 1 | t;
                }

                values[index - 1] = m;
                _used |= IO;
            }
            else if (to(value) != 0)
            {
                var index1 = used(key); //!!!
                values[index1] |= 1UL << key;
            }

            _count++;
            return this;
        }

        public RW remove(int bit)
        {
            if (Count <= bit)
                return this;
            ;
            _count--;
            var index = bit >> LEN;
            if (used() <= index)
                return this;
            ;
            var last = last1();
            if (bit == last)
                Set0(bit);
            else if (bit < last)
            {
                ulong m = ~0UL << bit, v = values[index];
                v = v >> 1 & m | v & ~m;
                while (++index < _used)
                {
                    m = values[index];
                    values[index - 1] = (m & 1) << BITS - 1 | v;
                    v = m >> 1;
                }

                values[index - 1] = v;
                _used |= IO;
            }

            return this;
        }

        public RW fit()
        {
            return Capacity(Count);
        }

        public new int Capacity() => values.Length * BITS;

        //Adjusts the length of the storage array based on the number of items.
        //If 0 < items , it adjusts the storage space according to the 'items' parameter.
        //If items < 0, it cleans up and allocates -items space.
        public RW Capacity(int bits)
        {
            if (0 < bits)
            {
                if (bits < Count)
                {
                    Set0(bits, Count + 1);
                    _count = bits;
                }

                Array.Resize(ref values, (int)(index((uint)bits) + 1));
                _used |= IO;
                return this;
            }

            _count = 0;
            _used = 0;
            values = bits == 0 ? Array.Empty<ulong>() : new ulong[index((uint)-bits) + 1];
            return this;
        }

        public new int Count
        {
            get => _count;
            set
            {
                if (value < _count)
                    if (value < 1)
                        clear();
                    else
                    {
                        Set0(value - 1, base.Count);
                        _count = value;
                    }
                else if (_count < value)
                    Set0(value - 1);
            }
        }

        public RW Resize(int size)
        {
            Count = size;
            return this;
        }

        public RW clear()
        {
            for (used(); _used > 0;)
                values[--_used] = 0;
            _count = 0;
            return this;
        }

        public static implicit operator T[](RW src)
        {
            var dst = new T[src.Count];
            for (var i = 0; i < src.Count; i++)
                dst[i] = src[i];
            return dst;
        }

        public static implicit operator RW(T[] src) => new RW(src.Length).Set(0, src);
    }
}