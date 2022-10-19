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
using System.Text;

namespace org.unirail.collections
{
    public interface BitsList
    {
        public const ulong FFFFFFFFFFFFFFFF = ~0UL;

        public static ulong mask(int bits) => (1UL << bits) - 1;

        public static uint size(uint src) => src >> 3;

        public static int index(uint item_Xbits) => (int)(item_Xbits >> LEN);

        public static int bit(uint item_Xbits) => (int)(item_Xbits & MASK);

        public static byte value(ulong src, int bit, ulong mask) => (byte)(src >> bit & mask);

        public static byte value(ulong prev, ulong next, int bit, int bits, ulong mask) => (byte)(((next & BitsList.mask(bit + bits - BITS)) << BITS - bit | prev >> bit) & mask);

        public const int  LEN  = 6;
        public const int  BITS = 1<<LEN;
        public const uint MASK = BITS - 1;

        static int len4bits(uint bits) => (int)((bits + BITS) >> LEN);


        abstract class R : ICloneable, IEquatable<R>, IList<byte>
        {
            public virtual void Add(byte item) { throw new NotImplementedException(); }

            public virtual void Clear() { throw new NotImplementedException(); }

            public bool Contains(byte item) => 0 < indexOf(item);

            public void CopyTo(byte[] array, int arrayIndex) { throw new NotImplementedException(); }

            public virtual bool Remove(byte item) { throw new NotImplementedException(); }

            public virtual bool IsReadOnly         => true;
            public         int  IndexOf(byte item) => indexOf(item);

            public virtual void Insert(int index, byte item) { throw new NotImplementedException(); }

            public virtual void RemoveAt(int index) { throw new NotImplementedException(); }


            public virtual byte this[int item]
            {
                get
                {
                    var _index = (uint)index((uint)(item *= bits));
                    var _bit   = bit((uint)item);

                    var index1 = _index + 1;
                    return BITS < _bit + bits
                               ? value(values[_index], values[index1], _bit, bits, mask)
                               : value(values[_index], _bit,           mask);
                }
                set => throw new NotImplementedException();
            }

            protected ulong[] values = Array.Empty<ulong>();
            public    int     Count { get; protected set; }

            public bool isEmpty() => Count == 0;

            protected ulong mask;
            public    int   bits { get; protected set; }


            protected R(int bits_per_item) => mask = mask(bits = bits_per_item);

            protected R(int bits_per_item, int count)
            {
                mask   = mask(bits = bits_per_item);
                values = new ulong[len4bits((uint)(count * bits))];
            }

            public readonly byte default_value;

            protected R(int bits_per_item, int default_value, int Count) : this(bits_per_item, Count)
            {
                this.Count = Count;
                if ((this.default_value = (byte)(default_value & 0xFF)) != 0)
                    while (-1 < --Count)
                        set(this, Count, default_value);
            }


            public override int GetHashCode()
            {
                var i = index((uint)Count);

                var hash = HashCode.Combine(149989999, values[i] & (ulong)((1L << bit((uint)Count)) - 1));

                while (-1 < --i) hash = HashCode.Combine(hash, values[i]);

                return hash;
            }

            public override bool Equals(object? other)
            {
                return other != null &&
                       Equals(other as R);
            }

            public bool Equals(R? other)
            {
                var i = index((uint)Count);
                var m = (1UL << bit((uint)Count)) - 1;

                return other           != null                  &&
                       other.Count     == Count                 &&
                       (values[i] & m) == (other.values[i] & m) &&
                       new Span<ulong>(values, 0, i).SequenceEqual(new Span<ulong>(other.values, 0, i));
            }


            public int length
            {
                get => values.Length * BITS / bits;
                //if 0 < value - fit storage space according `items` param
                //if value < 0 - cleanup and allocate spase
                set
                {
                    if (0 < value)
                    {
                        if (value < Count) Count = value;

                        Array.Resize(ref values, len4bits((uint)(value * bits)));
                        return;
                    }

                    if (values.Length == value) clear();
                    else
                    {
                        Count = 0;
                        values = value == 0
                                     ? Array.Empty<ulong>()
                                     : new ulong[len4bits((uint)(-value * bits))];
                    }
                }
            }

            protected void clear()
            {
                for (var i = index((uint)(bits * Count)) + 1; -1 < --i;) values[i] = 0;
                Count = 0;
            }

            protected internal static void write(R dst, int size)
            {
                if (dst.length < len4bits((uint)((dst.Count = size) * dst.bits))) dst.length = -size * dst.bits;
            }


            protected static void add(R dst, int item, byte value)
            {
                if (dst.Count <= item)
                {
                    set(dst, item, value);
                    return;
                }

                var p = (uint)(item * dst.bits);

                item = index(p);

                var src  = dst.values;
                var dst_ = dst.values;

                if (dst.length * BITS < p) dst.length = -Math.Max(dst.length + dst.length / 2, len4bits(p));

                var v    = value & dst.mask;
                var _bit = bit(p);
                if (0 < _bit)
                {
                    var i = src[item];
                    var k = BITS - _bit;
                    if (k < dst.bits)
                    {
                        dst_[item] = i << k >> k | v         << _bit;
                        v          = v      >> k | i >> _bit << dst.bits - k;
                    }
                    else
                    {
                        dst_[item] = i << k >> k | v << _bit | i >> _bit << _bit + dst.bits;
                        v          = i      >> _bit                              + dst.bits | src[item + 1] << k - dst.bits & dst.mask;
                    }

                    item++;
                }

                dst.Count++;

                for (var max = len4bits((uint)(dst.Count * dst.bits));;)
                {
                    var i = src[item];
                    dst_[item] = i << dst.bits | v;
                    if (max < ++item) break;
                    v = i >> BITS - dst.bits;
                }
            }

            protected static void set(R dst, int from, params byte[] src)
            {
                for (var i = src.Length; -1 < --i;) set(dst, from + i, src[i]);
            }

            protected static void set(R dst, int from, params ushort[] src)
            {
                for (var i = src.Length; -1 < --i;) set(dst, from + i, (byte)src[i]);
            }

            protected static void set(R dst, int from, params short[] src)
            {
                for (var i = src.Length; -1 < --i;) set(dst, from + i, (byte)src[i]);
            }

            protected static void set(R dst, int from, params int[] src)
            {
                for (var i = src.Length; -1 < --i;) set(dst, from + i, (byte)src[i]);
            }

            protected static void set(R dst, int from, params long[] src)
            {
                for (var i = src.Length; -1 < --i;) set(dst, from + i, (byte)src[i]);
            }

            protected static void set(R dst, int item, byte src)
            {
                var v      = src & dst.mask;
                var p      = (uint)(item * dst.bits);
                var _index = index(p);
                var _bit   = bit(p);

                var k = BITS - _bit;
                var i = dst.values[_index];

                if (item < dst.Count)
                {
                    if (k < dst.bits)
                    {
                        dst.values[_index]     = i                      << k            >> k            | v << _bit;
                        dst.values[_index + 1] = dst.values[_index + 1] >> dst.bits - k << dst.bits - k | v >> k;
                    }
                    else dst.values[_index] = ~(~0UL >> BITS - dst.bits << _bit) & i | v << _bit;

                    return;
                }

                if (dst.length <= item) dst.length = Math.Max(dst.length + dst.length / 2, len4bits((uint)(p + dst.bits)));

                if (dst.default_value != 0)
                    for (var t = dst.Count; t < item; t++) set2(dst, item, dst.default_value);
			
                set2(dst, item, src);

                dst.Count = item + 1;
            }

            private static void set2(R dst, int item, byte src)
            {
                var v      = src & dst.mask;
                var p      = (uint)(item * dst.bits);
                var _index = index(p);
                var _bit   = bit(p);

                var k = BITS - _bit;
                var i = dst.values[_index];
                
                if (k < dst.bits)
                {
                    dst.values[_index]     = i << k >> k | v << _bit;
                    dst.values[_index + 1] = v >> k;
                }
                else dst.values[_index] = ~(~0UL << _bit) & i | v << _bit;
            }

            protected static void add(R dst, long src) => set(dst, dst.Count, src);

            protected static void removeAt(R dst, int item)
            {
                if (item + 1 == dst.Count)
                {
                    if (dst.default_value == 0) set2(dst, item, 0); //zeroed place
                    dst.Count--;
                    return;
                }

                var _index = index((uint)(item *= dst.bits));
                var _bit   = bit((uint)item);

                var k = BITS - _bit;
                var i = dst.values[_index];

                if (_index + 1 == dst.length)
                {
                    if (_bit          == 0) dst.values[_index]       = i           >> dst.bits;
                    else if (k        < dst.bits) dst.values[_index] = i      << k >> k;
                    else if (dst.bits < k) dst.values[_index]        = i << k >> k | i >> _bit + dst.bits << _bit;

                    dst.Count--;
                    return;
                }

                if (_bit   == 0) dst.values[_index] = i >>= dst.bits;
                else if (k < dst.bits)
                {
                    var ii = dst.values[_index + 1];

                    dst.values[_index]   = i << k >> k | ii >> _bit + dst.bits - BITS << _bit;
                    dst.values[++_index] = i = ii >> dst.bits;
                }
                else if (dst.bits < k)
                    if (_index + 1 == dst.values.Length)
                    {
                        dst.values[_index] = i << k >> k | i >> _bit + dst.bits << _bit;
                        dst.Count--;
                        return;
                    }
                    else
                    {
                        var ii = dst.values[_index + 1];

                        dst.values[_index]   = i << k >> k | i >> _bit + dst.bits << _bit | ii << BITS - dst.bits;
                        dst.values[++_index] = i = ii >> dst.bits;
                    }

                for (var max = dst.Count * dst.bits; _index * BITS < max;)
                {
                    var ii = dst.values[_index + 1];
                    dst.values[_index]   = i << dst.bits >> dst.bits | ii << BITS - dst.bits;
                    dst.values[++_index] = i = ii        >> dst.bits;
                }

                dst.Count--;
            }


            public object Clone()
            {
                var dst = new RW(bits);
                dst.Count = Count;
                if (0 < dst.length) dst.values = (ulong[])values.Clone();
                return dst;
            }


            public override string ToString() => ToString(null).ToString();

            public StringBuilder ToString(StringBuilder? dst)
            {
                if (dst == null) dst = new StringBuilder(Count * 4);
                else dst.EnsureCapacity(dst.Length + Count     * 4);

                var src = values[(uint)0];
                for (int bp = 0, max = Count * bits, i = 1; bp < max; bp += bits, i++)
                {
                    var _bit   = bit((uint)bp);
                    var index1 = (uint)(index((uint)bp) + 1);
                    var _value = (long)(BITS < _bit + bits
                                            ? value(src, src = values[index1], _bit, bits, mask)
                                            : value(src, _bit,                 mask));

                    dst.Append(_value).Append('\t');

                    if (i % 10 == 0) dst.Append('\t').Append(i / 10 * 10).Append('\n');
                }

                return dst;
            }

            public int indexOf(byte value)
            {
                for (int item = 0, max = Count * bits; item < max; item += bits)
                    if (value == this[item])
                        return item / bits;

                return -1;
            }

            public int lastIndexOf(byte value) => lastIndexOf(Count, value);

            public int lastIndexOf(int from, byte value)
            {
                for (var i = from; -1 < --i;)
                    if (value == this[i])
                        return i;

                return -1;
            }


            public R subList(int fromIndex, int toIndex) => null;


            protected static bool remove(R dst, byte value)
            {
                var ret = false;
                for (var i = dst.Count; -1 < (i = dst.lastIndexOf(i, value));)
                {
                    ret = true;
                    removeAt(dst, i);
                }

                return ret;
            }

            public bool contains(byte value) => -1 < indexOf(value);

            public byte[] toArray(byte[] dst)
            {
                if (Count == 0) return null;
                if (dst == null || dst.Length < Count) dst = new byte[Count];

                for (int item = 0, max = Count * bits; item < max; item += bits)
                    dst[item / bits] = this[item];

                return dst;
            }

            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

            public IEnumerator<byte> GetEnumerator() => new Enumerator(this);

            public struct Enumerator : IEnumerator<byte>, IEnumerator
            {
                private readonly IList<byte> _list;
                private          int         _index;

                private byte _current;

                internal Enumerator(IList<byte> list)
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

                public byte Current => _current!;

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
            public override void Add(byte item) => set(this, Count, item);

            public override void Clear() => clear();

            public override bool Remove(byte item) => remove(this, item);


            public override bool IsReadOnly                   => true;
            public override void Insert(int index, byte item) => add(index, item);

            public override void RemoveAt(int index) => removeAt(index);

            public override byte this[int index] { get => base[index]; set => set(this, index, value); }

            public RW(int bits_per_item) : base(bits_per_item) { }

            public RW(int bits_per_item, int count) : base(bits_per_item, count) { }

            public RW(int bits_per_item, int defaultValue, int Count) : base(bits_per_item, defaultValue, Count) { }

            public RW(int bits_per_item, params byte[] values) : base(bits_per_item, values.Length) => set(this, 0, values);


            public RW(int bits_per_item, params ushort[] values) : base(bits_per_item, values.Length) => set(this, 0, values);


            public RW(int bits_per_item, params short[] values) : base(bits_per_item, values.Length) => set(this, 0, values);


            public RW(int bits_per_item, params int[] values) : base(bits_per_item, values.Length) => set(this, 0, values);


            public RW(int bits_per_item, params long[] values) : base(bits_per_item, values.Length) => set(this, 0, values);


            public void add(byte value) => add(this, value);

            public void add(int      index, byte src) => add(this, index, src);
            public void removeAt(int item)  => removeAt(this, item);
            public void remove(byte  value) => remove(this, value);

            public void set(int item, byte value) => set(this, item, value);


            public void set(int index, params ushort[] values) => set(this, index, values);

            public bool retainAll(R chk)
            {
                var fix = Count;

                byte v;
                for (var item = 0; item < Count; item++)
                    if (!chk.contains(v = this[item]))
                        remove(this, v);

                return fix != Count;
            }

            public void clear() => base.clear();

            public void fit() => length = -Count;

            RW Clone() => (RW)base.Clone();
        }
    }
}