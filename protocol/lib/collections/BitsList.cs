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
using System.Runtime.CompilerServices;
using System.Text;

namespace org.unirail.collections;

public interface BitsList<T>
    where T : struct
{
    public static ulong mask(int bits) => (1UL << bits) - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static uint size(uint src) => src >> 3;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int index(uint item_Xbits) => (int)(item_Xbits >> LEN);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int bit(uint item_Xbits) => (int)(item_Xbits & MASK);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static byte to_byte(T src) => Unsafe.As<T, byte>(ref src);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static T from_byte(byte src) => Unsafe.As<byte, T>(ref src);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static T from_byte(ulong src) => from_byte((byte)src);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static T from_byte(long src) => from_byte((byte)src);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T value(ulong src, int bit, ulong mask) => from_byte(src >> bit & mask);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T value(ulong prev, ulong next, int bit, int bits, ulong mask) => from_byte(((next & BitsList<T>.mask(bit + bits - BITS)) << BITS - bit | prev >> bit) & mask);

    public const int LEN = 6;
    public const int BITS = 1 << LEN;
    public const uint MASK = BITS - 1;

    static int len4bits(uint bits) => (int)((bits + BITS) >> LEN);

    abstract class R : ICloneable, IEquatable<R>
    {
        protected ulong[] values = Array.Empty<ulong>();
        public int Count { get; protected set; }

        protected ulong mask;
        public readonly int bits;
        public readonly T default_value;

        protected R(int bits_per_item)
        {
            mask = mask(bits = bits_per_item);
            default_value = default;
        }

        protected R(int bits_per_item, int length)
        {
            mask = mask(bits = bits_per_item);
            values = new ulong[len4bits((uint)(length * bits))];
            default_value = default;
        }

        protected R(int bits_per_item, T default_value, int Count)
        {
            mask = mask(bits = bits_per_item);
            this.Count = Math.Abs(Count);
            values = new ulong[len4bits((uint)(this.Count * bits))];
            if (to_byte(this.default_value = default_value) == 0)
                return;
            for (var i = 0; i < Count; i++)
                append(this, i, default_value);
        }

        public int Capacity() => values.Length * BITS / bits;

        //Adjusts the length of the storage array based on the number of items.
        //If 0 < items , it adjusts the storage space according to the 'items' parameter.
        //If items < 0, it cleans up and allocates -items space.
        protected void Capacity(int items)
        {
            if (0 < items) //If positive, adjust the array size to fit the specified number of items.
            {
                if (items < Count)
                    Count = items; //Adjust the size if items are less than the current size.

                Array.Resize(ref values, len4bits((uint)(items * bits)));
                return;
            }

            //If negative, clear the array and allocate space for the absolute value of items.
            var new_values_length = len4bits((uint)(-items * bits));

            if (values.Length != new_values_length)
            {
                //Allocate new space or set it to an empty array if new length is 0.
                values = new_values_length == 0 ?
                             Array.Empty<ulong>() :
                             new ulong[new_values_length];

                Count = 0;
                return;
            }

            clear(); //Clear the array.
        }

        protected void clear()
        {
            Array.Fill(values, 0UL, 0, Math.Min(index((uint)(bits * Count)), values.Length - 1));
            Count = 0;
        }

        public bool isEmpty() => Count == 0;

        public override int GetHashCode()
        {
            var hash = HashCode.Combine(149989999, Count);
            var i = index((uint)Count);
            hash = HashCode.Combine(hash, values[i] & (1UL << bit((uint)Count)) - 1);
            while (-1 < --i)
                hash = HashCode.Combine(hash, values[i]);
            return hash;
        }

        public override bool Equals(object? other) => other != null && Equals(other as R);

        public bool Equals(R? other)
        {
            if (other == null || other.Count != Count)
                return false;

            var i = index((uint)Count);
            var mask = (1UL << bit((uint)Count)) - 1;
            return (values[i] & mask) == (other.values[i] & mask) &&
                   new Span<ulong>(values, 0, i).SequenceEqual(new Span<ulong>(other.values, 0, i));
        }

        public T Get() => this[Count - 1];

        public virtual T this[int item]
        {
            get
            {
                var _index = (uint)index((uint)(item *= bits));
                var _bit = bit((uint)item);
                return BITS < _bit + bits ?
                           value(values[_index], values[_index + 1], _bit, bits, mask) :
                           value(values[_index], _bit, mask);
            }
            set => throw new NotImplementedException();
        }

        protected static void add(R dst, long src) => append(dst, dst.Count, from_byte(src));

        protected static void add(R dst, T src) => append(dst, dst.Count, src);

        protected static void add(R dst, int item, T value)
        {
            if (dst.Count == item)
            {
                append(dst, item, value);
                return;
            }

            if (dst.Count < item)
            {
                set1(dst, item, value);
                return;
            }

            var p = (uint)(item * dst.bits);
            item = index(p);
            var src = dst.values;
            var dst_ = dst.values;
            if (dst.Capacity() * BITS < p)
                dst.Capacity(-Math.Max(dst.Capacity() + dst.Capacity() / 2, len4bits(p)));
            var v = to_byte(value) & dst.mask;
            var _bit = bit(p);
            if (0 < _bit)
            {
                var i = src[item];
                var k = BITS - _bit;
                if (k < dst.bits)
                {
                    dst_[item] = i << k >> k | v << _bit;
                    v = v >> k | i >> _bit << dst.bits - k;
                }
                else
                {
                    dst_[item] = i << k >> k | v << _bit | i >> _bit << _bit + dst.bits;
                    v = i >> _bit + dst.bits | src[item + 1] << k - dst.bits & dst.mask;
                }

                item++;
            }

            dst.Count++;
            for (var max = len4bits((uint)(dst.Count * dst.bits)); ;)
            {
                var i = src[item];
                dst_[item] = i << dst.bits | v;
                if (max < ++item)
                    break;
                v = i >> BITS - dst.bits;
            }
        }

        protected static void set(R dst, int from, params T[] src)
        {
            for (var i = src.Length; -1 < --i;)
                set1(dst, from + i, src[i]);
        }

        protected static void set(R dst, int from, params byte[] src)
        {
            for (var i = src.Length; -1 < --i;)
                set1(dst, from + i, from_byte(src[i]));
        }

        protected static void set(R dst, int from, params sbyte[] src)
        {
            for (var i = src.Length; -1 < --i;)
                set1(dst, from + i, from_byte(src[i]));
        }

        protected static void set(R dst, int from, params ushort[] src)
        {
            for (var i = src.Length; -1 < --i;)
                set1(dst, from + i, from_byte(src[i]));
        }

        protected static void set(R dst, int from, params short[] src)
        {
            for (var i = src.Length; -1 < --i;)
                set1(dst, from + i, from_byte(src[i]));
        }

        protected static void set(R dst, int from, params int[] src)
        {
            for (var i = src.Length; -1 < --i;)
                set1(dst, from + i, from_byte(src[i]));
        }

        protected static void set(R dst, int from, params uint[] src)
        {
            for (var i = src.Length; -1 < --i;)
                set1(dst, from + i, from_byte(src[i]));
        }

        protected static void set(R dst, int from, params long[] src)
        {
            for (var i = src.Length; -1 < --i;)
                set1(dst, from + i, from_byte(src[i]));
        }

        protected static void set(R dst, int from, params ulong[] src)
        {
            for (var i = src.Length; -1 < --i;)
                set1(dst, from + i, from_byte(src[i]));
        }

        protected static void set1(R dst, int item, T src)
        {
            var total_bits = (uint)(item * dst.bits);
            if (item < dst.Count)
            {
                var _index = index(total_bits);
                var _bit = bit(total_bits);
                var k = BITS - _bit;

                var v = to_byte(src) & dst.mask;
                var i = dst.values[_index];

                if (k < dst.bits)
                {
                    dst.values[_index] = i << k >> k | v << _bit;
                    dst.values[_index + 1] = dst.values[_index + 1] >> dst.bits - k << dst.bits - k | v >> k;
                }
                else
                    dst.values[_index] = ~(~0UL >> BITS - dst.bits << _bit) & i | v << _bit;

                return;
            }

            if (dst.Capacity() <= item)
                dst.Capacity(Math.Max(dst.Capacity() + dst.Capacity() / 2, len4bits((uint)(total_bits + dst.bits))));
            if (to_byte(dst.default_value) != 0)
                for (var t = dst.Count; t < item; t++)
                    append(dst, item, dst.default_value);
            append(dst, item, src);
            dst.Count = item + 1;
        }

        private static void append(R dst, int item, T src)
        {
            var v = to_byte(src) & dst.mask;

            var p = (uint)(item * dst.bits);
            int index = BitsList<T>.index(p),
                bit = BitsList<T>.bit(p);

            var k = BITS - bit;
            var i = dst.values[index];

            if (k < dst.bits)
            {
                dst.values[index] = i << k >> k | v << bit;
                dst.values[index + 1] = v >> k;
            }
            else
                dst.values[index] = ~(~0UL << bit) & i | v << bit;
        }

        protected static void removeAt(R dst, int item)
        {
            if (item + 1 == dst.Count)
            {
                if (to_byte(dst.default_value) == 0)
                    append(dst, item, default); //zeroed place
                dst.Count--;
                return;
            }

            var _index = index((uint)(item *= dst.bits));
            var _bit = bit((uint)item);

            var k = BITS - _bit;
            var i = dst.values[_index];

            if (_index + 1 == dst.Capacity())
            {
                if (_bit == 0)
                    dst.values[_index] = i >> dst.bits;
                else if (k < dst.bits)
                    dst.values[_index] = i << k >> k;
                else if (dst.bits < k)
                    dst.values[_index] = i << k >> k | i >> _bit + dst.bits << _bit;

                dst.Count--;
                return;
            }

            if (_bit == 0)
                dst.values[_index] = i >>= dst.bits;
            else if (k < dst.bits)
            {
                var ii = dst.values[_index + 1];
                dst.values[_index] = i << k >> k | ii >> _bit + dst.bits - BITS << _bit;
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

                    dst.values[_index] = i << k >> k | i >> _bit + dst.bits << _bit | ii << BITS - dst.bits;
                    dst.values[++_index] = i = ii >> dst.bits;
                }

            for (var max = dst.Count * dst.bits >> LEN; _index < max;)
            {
                var ii = dst.values[_index + 1];
                dst.values[_index] = i << dst.bits >> dst.bits | ii << BITS - dst.bits;
                dst.values[++_index] = i = ii >> dst.bits;
            }

            dst.Count--;
        }

        public object Clone()
        {
            var dst = (R)MemberwiseClone();
            dst.values = (ulong[])values.Clone();
            return dst;
        }

        public override string ToString() => ToString(null).ToString();

        public StringBuilder ToString(StringBuilder? dst)
        {
            if (dst == null)
                dst = new StringBuilder(Count * 4);
            else
                dst.EnsureCapacity(dst.Length + Count * 4);
            var src = values[(uint)0];
            for (int bp = 0, max = Count * bits, i = 1; bp < max; bp += bits, i++)
            {
                var _bit = bit((uint)bp);
                var index1 = (uint)(index((uint)bp) + 1);
                var _value = BITS < _bit + bits ?
                                 value(src, src = values[index1], _bit, bits, mask) :
                                 value(src, _bit, mask);
                dst.Append(_value).Append('\t');
                if (i % 10 == 0)
                    dst.Append('\t').Append(i / 10 * 10).Append('\n');
            }

            return dst;
        }

        public int indexOf(T value)
        {
            for (int item = 0, max = Count * bits; item < max; item += bits)
                if (value.Equals(this[item]))
                    return item / bits;
            return -1;
        }

        public int lastIndexOf(T value) => lastIndexOf(Count, value);

        public int lastIndexOf(int from, T value)
        {
            for (var i = Math.Min(from, Count); -1 < --i;)
                if (value.Equals(this[i]))
                    return i;
            return -1;
        }

        protected static bool remove(R dst, T value)
        {
            var ret = false;
            for (var i = dst.Count; -1 < (i = dst.lastIndexOf(i, value));)
            {
                ret = true;
                removeAt(dst, i);
            }

            return ret;
        }

        public T[] ToArray(T[]? dst)
        {
            if (Count == 0)
                return null;
            if (dst == null || dst.Length < Count)
                dst = new T[Count];
            for (int item = 0, max = Count * bits; item < max; item += bits)
                dst[item / bits] = this[item];
            return dst;
        }

        public bool Contains(T item) => -1 < indexOf(item);

        public int IndexOf(T item) => indexOf(item);

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

        public void Dispose() { }

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
        public RW(int bitsPerItem) : base(bitsPerItem) { }

        public RW(int bitsPerItem, int length) : base(bitsPerItem, length) { }

        public RW(int bitsPerItem, T defaultValue, int length) : base(bitsPerItem, defaultValue, length) { }

        public RW Add1(long value)
        {
            add(this, value);
            return this;
        }

        public RW Add(params T[] values)
        {
            foreach (var value in values)
                Set(Count, value);
            return this;
        }

        public RW Add1(int index, long src)
        {
            add(this, index, from_byte(src));
            return this;
        }

        public RW Remove(T value)
        {
            remove(this, value);
            return this;
        }

        public RW RemoveAt(int item)
        {
            removeAt(this, item);
            return this;
        }

        public RW Remove()
        {
            removeAt(this, base.Count - 1);
            return this;
        }

        public override T this[int item] { get => base[item]; set => set1(this, item, value); }

        public RW Set(int index, params byte[] values)
        {
            set(this, index, values);
            return this;
        }

        public RW Set(int index, params T[] values)
        {
            set(this, index, values);
            return this;
        }

        public RW Set(int index, params short[] values)
        {
            set(this, index, values);
            return this;
        }

        public RW Set(int index, params ushort[] values)
        {
            set(this, index, values);
            return this;
        }

        public RW Set(int index, params int[] values)
        {
            set(this, index, values);
            return this;
        }

        public RW Set(int index, params uint[] values)
        {
            set(this, index, values);
            return this;
        }

        public RW Set(int index, params long[] values)
        {
            set(this, index, values);
            return this;
        }

        public RW Set(int index, params ulong[] values)
        {
            set(this, index, values);
            return this;
        }

        public bool retainAll(R chk)
        {
            var fix = base.Count;
            T v;
            for (var item = 0; item < base.Count; item++)
                if (!chk.Contains(v = this[item]))
                    remove(this, v);
            return fix != base.Count;
        }

        public RW Fit() => Capacity(base.Count);

        public RW Clear()
        {
            clear();
            return this;
        }

        public new RW Capacity(int value)
        {
            if (value < 1)
            {
                values = Array.Empty<ulong>();
                base.Count = 0;
            }
            else base.Capacity(value);

            return this;
        }

        public new int Count
        {
            get => base.Count;
            set
            {
                if (value < 1)
                    Clear();
                else if (base.Count < value)
                    set1(this, value - 1, default_value);
                else
                    base.Count = value;
            }
        }

        public RW Resize(int size)
        {
            Count = size;
            return this;
        }

        RW Clone() => (RW)base.Clone();
    }
}