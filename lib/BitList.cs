// AdHoc protocol - data interchange format and source code generator
// Copyright 2020 Chikirev Sirguy, Unirail Group. All rights reserved.
// cheblin@gmail.org
// https://github.com/orgs/AdHoc-Protocol
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace org.unirail.collections
{
    public interface BitList
    {
        abstract class R : ICloneable, IEquatable<R>, IList<bool>
        {
            public virtual void Add(bool item) { throw new NotImplementedException(); }

            public virtual void Clear() { throw new NotImplementedException(); }


            public virtual bool Remove(bool item) { throw new NotImplementedException(); }

            public virtual bool IsReadOnly => true;


            public virtual void Insert(int index, bool item) { throw new NotImplementedException(); }

            public virtual void RemoveAt(int index) { throw new NotImplementedException(); }

            public virtual bool this[int index] { get => get(index); set => throw new NotImplementedException(); }


            public int  Count                                { get; protected set; }
            public int  IndexOf(bool  item)                  { throw new NotImplementedException(); }
            public bool Contains(bool item)                  => IndexOf(item) != -1;
            public void CopyTo(bool[] array, int arrayIndex) { throw new NotImplementedException(); }

            protected internal ulong[] array = Array.Empty<ulong>();


            protected static int len4bits(int bits) { return 1 + (bits >> LEN); }

            public const int  LEN  = 6;
            public const int  BITS = 1 << LEN;
            public const uint MASK = BITS - 1;

            protected static uint index(uint item_X_bits) { return item_X_bits >> LEN; }

            protected static ulong mask(int bits) { return (1UL << bits) - 1; }

            public const ulong FFFFFFFFFFFFFFFF = ~0UL;
            public const int   OI               = int.MaxValue;
            public const int   IO               = int.MinValue;


            protected internal int _used;

            protected internal int used()
            {
                if (-1 < _used) return _used;

                _used &= OI;

                var i = _used - 1;
                while (-1 < i && array[i] == 0) i--;

                return _used = i + 1;
            }

            protected internal int used(int bit)
            {
                if (Count <= bit) Count = bit + 1;

                var index = bit >> LEN;
                if (index < used()) return index;

                if (array.Length < (_used = index + 1)) Array.Resize(ref array, Math.Max(2 * array.Length, _used));

                return index;
            }


            public bool get(int bit)
            {
                var index = bit >> LEN;
                return index < used() && (array[index] & 1UL << bit) != 0;
            }

            public int get(int bit, int FALSE, int TRUE)
            {
                var index = bit >> LEN;
                return index < used() && (array[index] & 1UL << bit) != 0 ? TRUE : FALSE;
            }

            public int get(ulong[] dst, int from_bit, int to_bit)
            {
                var ret = (to_bit - from_bit - 1 >> LEN) + 1;

                var index = from_bit >> LEN;

                if ((from_bit & MASK) == 0) Array.Copy(array, index, dst, 0, ret - 1);
                else
                    for (var i = 0; i < ret - 1; i++, index++)
                        dst[i] = array[index] >> from_bit | array[index + 1] << -from_bit;


                var mask = FFFFFFFFFFFFFFFF >> -to_bit;
                dst[ret - 1] =
                    (to_bit - 1 & MASK) < (from_bit & MASK)
                        ? array[index] >> from_bit | (array[index + 1] & mask) << -from_bit
                        : (array[index] & mask) >> from_bit;

                return ret;
            }


            public int next1(int bit)
            {
                var index = bit >> LEN;
                if (used() <= index) return -1;

                for (var i = array[index] & FFFFFFFFFFFFFFFF << bit;; i = array[index])
                {
                    if (i       != 0) return index * BITS + BitOperations.TrailingZeroCount(i);
                    if (++index == _used) return -1;
                }
            }


            public int next0(int bit)
            {
                var index = bit >> LEN;
                if (used() <= index) return bit;

                for (var i = ~array[index] & FFFFFFFFFFFFFFFF << bit;; i = ~array[index])
                {
                    if (i       != 0) return index * BITS + BitOperations.TrailingZeroCount(i);
                    if (++index == _used) return _used * BITS;
                }
            }

            public int prev1(int bit)
            {
                var index = bit >> LEN;
                if (used() <= index) return last1() - 1;


                for (var i = array[index] & FFFFFFFFFFFFFFFF >> -(bit + 1);; i = array[index])
                {
                    if (i       != 0) return (index + 1) * BITS - 1 - BitOperations.LeadingZeroCount(i);
                    if (index-- == 0) return -1;
                }
            }


            public int prev0(int bit)
            {
                var index = bit >> LEN;
                if (used() <= index) return bit;

                for (var i = ~array[index] & FFFFFFFFFFFFFFFF >> -(bit + 1);; i = ~array[index])
                {
                    if (i       != 0) return (index + 1) * BITS - 1 - BitOperations.LeadingZeroCount(i);
                    if (index-- == 0) return -1;
                }
            }


            public int last1() { return used() == 0 ? 0 : BITS * (_used - 1) + BITS - BitOperations.LeadingZeroCount(array[_used - 1]); }


            public bool isEmpty() { return _used == 0; }


            public int rank(int bit)
            {
                var max = bit >> LEN;

                if (max < used())
                    for (int i = 0, sum = 0;; i++)
                        if (i < max) sum += BitOperations.PopCount(array[i]);
                        else return sum + BitOperations.PopCount(array[i] & FFFFFFFFFFFFFFFF >> BITS - (bit + 1));

                return cardinality();
            }


            public int cardinality()
            {
                for (int i = 0, sum = 0;; i++)
                    if (i < used()) sum += BitOperations.PopCount(array[i]);
                    else return sum;
            }

            public int bit(int cardinality)
            {
                int i = 0, c = 0;
                while ((c += BitOperations.PopCount(array[i])) < cardinality) i++;

                var v = array[i];
                var z = BitOperations.LeadingZeroCount(v);

                for (var p = 1UL << BITS - 1; cardinality < c; z++)
                    if ((v & p >> z) != 0)
                        c--;

                return i * 32 + BITS - z;
            }


            public int length() { return array.Length * BITS; }

            public object Clone()
            {
                R dst = new RW(0);
                dst.Count = Count;
                dst._used = _used;
                if (0 < array.Length) array = (ulong[])array.Clone();
                return dst;
            }


            public override string ToString() { return ToString(null).ToString(); }

            public StringBuilder ToString(StringBuilder? dst)
            {
                var _size = Count;
                var max   = _size / BITS;

                if (dst == null) dst = new StringBuilder((max + 1) * 68);
                else dst.EnsureCapacity(dst.Length + (max     + 1) * 68);
                dst.Append(string.Format("%-8s%-8s%-8s%-8s%-8s%-8s%-8s%-7s%s", "0", "7", "15", "23", "31", "39", "47", "55", "63"));
                dst.Append('\n');
                dst.Append(string.Format("%-8s%-8s%-8s%-8s%-8s%-8s%-8s%-7s%s", "|", "|", "|", "|", "|", "|", "|", "|", "|"));
                dst.Append('\n');

                for (var i = 0; i < max; i++)
                {
                    var v = array[i];
                    for (var s = 0; s < BITS; s++)
                        dst.Append((v & 1UL << s) == 0 ? '.' : '*');
                    dst.Append(i * BITS);
                    dst.Append('\n');
                }

                if (0 < (_size &= 63))
                {
                    var v = array[max];
                    for (var s = 0; s < _size; s++)
                        dst.Append((v & 1UL << s) == 0 ? '.' : '*');
                }


                return dst;
            }

            public override bool Equals(object? obj) { return obj != null && Equals(obj as R); }

            public bool Equals(R? other) { return other != null && new Span<ulong>(array, 0, used()).SequenceEqual(new Span<ulong>(other.array, 0, used())); }


            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

            public IEnumerator<bool> GetEnumerator() => new Enumerator(this);

            public struct Enumerator : IEnumerator<bool>, IEnumerator
            {
                private readonly IList<bool> _list;
                private          int         _index;

                private bool _current;

                internal Enumerator(IList<bool> list)
                {
                    _list    = list;
                    _index   = 0;
                    _current = default;
                }

                public void Dispose() { }

                public bool MoveNext()
                {
                    _current = _list[_index];
                    _index++;
                    return true;
                }

                public bool Current => _current!;

                object? IEnumerator.Current => Current;


                void IEnumerator.Reset()
                {
                    _index   = 0;
                    _current = default;
                }
            }
        }


        class RW : R
        {
            public override void Add(bool item) => add(Count, item);

            public override void Clear() => clear();

            public override bool Remove(bool item)
            {
                var i = IndexOf(item);
                if (i < 0) return false;
                remove(i);
                return true;
            }

            public override bool IsReadOnly                   => false;
            public override void Insert(int index, bool item) => add(index, item);

            public override void RemoveAt(int index) => remove(index);

            public override bool this[int index] { get => get(index); set => set(index, value); }

            public RW(int length)
            {
                if (0 < length) array = new ulong[(length - 1 >> LEN) + 1];
            }

            public RW(bool fill_value, int Count)
            {
                var len = len4bits(this.Count = Count);
                array = new ulong[len];

                _used = len | IO;

                if (fill_value) set1(0, Count - 1);
            }

            public RW(R src, int from_bit, int to_bit)
            {
                if (src.Count <= from_bit) return;
                Count = Math.Min(to_bit, src.Count - 1) - from_bit;

                var i2 = src.get(to_bit) ? to_bit : src.prev1(to_bit);

                if (i2 == -1) return;

                array = new ulong[(i2 - 1 >> LEN) + 1];
                _used = array.Length | IO;

                int
                    i1    = src.get(from_bit) ? from_bit : src.next1(from_bit),
                    index = i1 >> LEN,
                    max   = (i2 >> LEN) + 1,
                    i     = 0;

                for (var v = src.array[index] >> i1;; v >>= i1, i++)
                    if (index + 1 < max)
                        array[i] = v | (v = src.array[index + i]) << BITS - i1;
                    else
                    {
                        array[i] = v;
                        return;
                    }
            }


            public void and(R and)
            {
                if (this == and) return;

                if (and.used() < used())
                    while (_used > and._used)
                        array[--_used] = 0;

                for (var i = 0; i < _used; i++) array[i] &= and.array[i];

                _used |= IO;
            }


            public void or(R or)
            {
                if (or.used() < 1 || this == or) return;

                var u = _used;
                if (used() < or.used())
                {
                    if (array.Length < or._used) Array.Resize(ref array, Math.Max(2 * array.Length, or._used));
                    _used = or._used;
                }

                var min = Math.Min(u, or._used);

                for (var i = 0; i < min; i++)
                    array[i] |= or.array[i];

                if (min      < or._used) Array.Copy(or.array, min, array,    min, or._used - min);
                else if (min < u) Array.Copy(array,           min, or.array, min, u        - min);
            }


            public void xor(R xor)
            {
                if (xor.used() < 1 || xor == this) return;

                var u = _used;
                if (used() < xor.used())
                {
                    if (array.Length < xor._used) Array.Resize(ref array, Math.Max(2 * array.Length, xor._used));
                    _used = xor._used;
                }

                var min = Math.Min(u, xor._used);
                for (var i = 0; i < min; i++)
                    array[i] ^= xor.array[i];

                if (min      < xor._used) Array.Copy(xor.array, min, array,     min, xor._used - min);
                else if (min < u) Array.Copy(array,             min, xor.array, min, u         - min);

                _used |= IO;
            }

            public void andNot(R not)
            {
                for (var i = Math.Min(used(), not.used()) - 1; -1 < i; i--) array[i] &= ~not.array[i];

                _used |= IO;
            }

            public bool intersects(R set)
            {
                for (var i = Math.Min(_used, set._used) - 1; i >= 0; i--)
                    if ((array[i] & set.array[i]) != 0)
                        return true;

                return false;
            }

            public void fit() { length(Count); }

            void length(int bits)
            {
                if (0 < bits)
                {
                    if (bits < Count)
                    {
                        set0(bits, Count + 1);
                        Count = bits;
                    }

                    Array.Resize(ref array, (int)(index((uint)bits) + 1));

                    _used |= IO;
                    return;
                }

                Count = 0;
                _used = 0;
                array = bits == 0 ? Array.Empty<ulong>() : new ulong[index((uint)-bits) + 1];
            }

            public void flip(int bit)
            {
                var index                                                          = used(bit);
                if ((array[index] ^= 1UL << bit) == 0 && index + 1 == _used) _used |= IO;
            }


            public void flip(int from_bit, int to_bit)
            {
                if (from_bit == to_bit) return;

                var from_index = from_bit >> LEN;
                var to_index   = used(to_bit - 1);

                var from_mask = FFFFFFFFFFFFFFFF << from_bit;
                var to_mask   = FFFFFFFFFFFFFFFF >> -to_bit;

                if (from_index == to_index)
                {
                    if ((array[from_index] ^= from_mask & to_mask) == 0 && from_index + 1 == _used) _used |= IO;
                }
                else
                {
                    array[from_index] ^= from_mask;

                    for (var i = from_index + 1; i < to_index; i++) array[i] ^= FFFFFFFFFFFFFFFF;

                    array[to_index] ^= to_mask;
                    _used           |= IO;
                }
            }

            public void set(int index, params bool[] values)
            {
                for (int i = 0, max = values.Length; i < max; i++)
                    if (values[i]) set1(index + i);
                    else set0(index           + i);
            }


            public void set1(int bit)
            {
                var index = used(bit); //!!!
                array[index] |= 1UL << bit;
            }


            public void add(bool value) { set(Count, value); }

            public void set(int bit, bool value)
            {
                if (value)
                    set1(bit);
                else
                    set0(bit);
            }

            public void set(int bit, int value)
            {
                if (value == 0)
                    set0(bit);
                else
                    set1(bit);
            }

            public void set(int bit, int value, int TRUE)
            {
                if (value == TRUE)
                    set1(bit);
                else
                    set0(bit);
            }

            public void set1(int from_bit, int to_bit)
            {
                if (from_bit == to_bit) return;

                var from_index = from_bit >> LEN;
                var to_index   = used(to_bit - 1);

                var from_mask = FFFFFFFFFFFFFFFF << from_bit;
                var to_mask   = FFFFFFFFFFFFFFFF >> -to_bit;

                if (from_index == to_index) array[from_index] |= from_mask & to_mask;
                else
                {
                    array[from_index] |= from_mask;

                    for (var i = from_index + 1; i < to_index; i++)
                        array[i] = FFFFFFFFFFFFFFFF;

                    array[to_index] |= to_mask;
                }
            }


            public void set(int from_bit, int to_bit, bool value)
            {
                if (value)
                    set1(from_bit, to_bit);
                else
                    set0(from_bit, to_bit);
            }


            public void set0(int bit)
            {
                if (Count <= bit) Count = bit + 1;

                var index = bit >> LEN;

                if (index < used())
                    if (index + 1 == _used && (array[index] &= ~(1UL << bit)) == 0) _used |= IO;
                    else
                        array[index] &= ~(1UL << bit);
            }


            public void set0(int from_bit, int to_bit)
            {
                if (Count <= to_bit) Count = to_bit + 1;

                if (from_bit == to_bit) return;

                var from_index = from_bit >> LEN;
                if (used() <= from_index) return;

                var to_index = to_bit - 1 >> LEN;
                if (_used <= to_index)
                {
                    to_bit   = last1();
                    to_index = _used - 1;
                }

                var from_mask = FFFFFFFFFFFFFFFF << from_bit;
                var to_mask   = FFFFFFFFFFFFFFFF >> -to_bit;

                if (from_index == to_index)
                {
                    if ((array[from_index] &= ~(from_mask & to_mask)) == 0)
                        if (from_index + 1 == _used)
                            _used |= IO;
                }
                else
                {
                    array[from_index] &= ~from_mask;

                    for (var i = from_index + 1; i < to_index; i++) array[i] = 0;

                    array[to_index] &= ~to_mask;

                    _used |= IO;
                }
            }

            public void add(ulong src) { add(src, BITS); }

            public void add(ulong src, int bits)
            {
                if (BITS < bits) bits = BITS;

                var size = Count;
                Count += bits;

                if ((src &= ~(1UL << bits - 1)) == 0) return;

                used(size + BITS - BitOperations.LeadingZeroCount(src));

                var bit = (int)(size & MASK);

                if (bit == 0) array[index((uint)Count)] = src;
                else
                {
                    array[index((uint)size)] &= src                                             << bit | mask(bit);
                    if (index((uint)size) < index((uint)Count)) array[index((uint)Count)] = src >> bit;
                }
            }

            public void add(int key, bool value)
            {
                if (key < last1())
                {
                    var index = key >> LEN;

                    ulong m = FFFFFFFFFFFFFFFF << key, v = array[index];

                    m = (v & m) << 1 | v & ~m;

                    if (value) m |= 1UL << key;

                    while (++index < _used)
                    {
                        array[index       - 1] = m;
                        var t = v >> BITS - 1;
                        v = array[index];
                        m = v << 1 | t;
                    }

                    array[index - 1] =  m;
                    _used            |= IO;
                }
                else if (value)
                {
                    var index1 = used(key); //!!!
                    array[index1] |= 1UL << key;
                }
                Count++;
            }

            public void clear()
            {
                for (used(); _used > 0;) array[--_used] = 0;
                Count = 0;
            }


            public void remove(int bit)
            {
                if (Count <= bit) return;

                Count--;

                var index = bit >> LEN;
                if (used() <= index) return;


                var last = last1();
                if (bit      == last) set0(bit);
                else if (bit < last)
                {
                    ulong m = FFFFFFFFFFFFFFFF << bit, v = array[index];

                    v = v >> 1 & m | v & ~m;

                    while (++index < _used)
                    {
                        m = array[index];

                        array[index - 1] = (m & 1) << BITS - 1 | v;
                        v                = m >> 1;
                    }

                    array[index - 1] =  v;
                    _used            |= IO;
                }
            }
        }
    }
}