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
    public interface BitsNullList
    {
        abstract class R : BitsList.R, IList<byte?>
        {
            public virtual void Add(byte? item) { throw new NotImplementedException(); }

            public bool Contains(byte? item) => 0 < IndexOf(item);

            public void CopyTo(byte?[] array, int arrayIndex) { throw new NotImplementedException(); }

            public virtual bool Remove(byte? item) { throw new NotImplementedException(); }

            public int IndexOf(byte? item) => item == null ? indexOf(null_val) : indexOf(item.Value);

            public virtual void Insert(int index, byte? item) { throw new NotImplementedException(); }

            public virtual byte? this[int item]
            {
                get
                {
                    var v = get(item);
                    return v == null_val ? null : v;
                }
                set => throw new NotImplementedException();
            }

            public byte get(int item)
            {
                var index = (uint)BitsList.index((uint)(item *= bits));
                var bit   = BitsList.bit((uint)item);

                return BitsList.BITS < bit + bits ? BitsList.value(values[index], values[index + 1], bit, bits, mask) : BitsList.value(values[index], bit, mask);
            }

            public byte null_val { get; protected set; }

            protected R(int null_val, int bits_per_item) : base(bits_per_item) { this.null_val = (byte)null_val; }


            protected R(int null_val, int bits_per_item, int count) : base(bits_per_item, count)
            {
                if ((this.null_val = (byte)null_val) != 0) nulls(this, 0, count);
            }

            protected R(int null_val, int bits_per_item, int fill_value, int Count) : base(bits_per_item, Count)
            {
                this.null_val = (byte)null_val;
                this.Count    = Count;

                if (fill_value == 0) return;
                while (-1      < --Count) set(this, Count, fill_value);
            }


            protected static void nulls(R dst, int from, int upto)
            {
                while (from < upto) set(dst, from++, dst.null_val);
            }

            public bool hasValue(int index) { return this[index] != null_val; }

            R Clone() { return (R)base.Clone(); }

            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

            public IEnumerator<byte?> GetEnumerator() => new Enumerator(this);

            public struct Enumerator : IEnumerator<byte?>, IEnumerator
            {
                private readonly IList<byte?> _list;
                private          int          _index;

                private byte? _current;

                internal Enumerator(IList<byte?> list)
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

                public byte? Current => _current!;

                object? IEnumerator.Current => Current;


                void IEnumerator.Reset()
                {
                    _index   = 0;
                    _current = default;
                }
            }

            StringBuilder ToString(StringBuilder? dst)
            {
                if (dst == null) dst = new StringBuilder(Count * 4);
                else dst.EnsureCapacity(dst.Length + Count     * 4);

                var src = values[(uint)0];
                for (int bp = 0, max = Count * bits, i = 1; bp < max; bp += bits, i++)
                {
                    var  _bit   = BitsList.bit((uint)bp);
                    var index  = (uint)(BitsList.index((uint)bp) + 1);
                    var  _value = (long)(BitsList.BITS < _bit + bits ? BitsList.value(src, src = values[index], _bit, bits, mask) : BitsList.value(src, _bit, mask));

                    if (_value == null_val) dst.Append("null");
                    else dst.Append(_value);

                    dst.Append('\t');

                    if (i % 10 == 0) dst.Append('\t').Append(i / 10 * 10).Append('\n');
                }

                return dst;
            }
        }

        class RW : R
        {
            public override void Add(byte? item)            => this[Count] = item;
            public          void set(int   item, int value) => set(this, item, value);

            public override bool Remove(byte? item)
            {
                var i = IndexOf(item);
                if (i < 0) return false;
                removeAt(i);

                return true;
            }

            public override void Insert(int index, byte? item) => add(item == null ? null_val : item.Value);

            public override void Clear() => clear();


            public override bool IsReadOnly { get; }

            public override void RemoveAt(int index) => removeAt(index);

            public RW(int null_val, int bits_per_item) : base(null_val, bits_per_item) { }

            public RW(int null_val, int bits_per_item, int count) : base(null_val, bits_per_item, count) { }

            public RW(int null_val, int bits_per_item, int? fill_value, int items) : base(null_val, bits_per_item, fill_value ?? null_val, items) { }


            public RW(int null_val, int bits_per_item, params byte[] values) : base(null_val, bits_per_item, values.Length) { set(0, values); }


            public RW(int null_val, int bits_per_item, params byte?[] values) : base(null_val, bits_per_item, values.Length) { set(0, values); }


            public RW(int null_val, int bits_per_item, params ushort[] values) : base(null_val, bits_per_item, values.Length) { set(0, values); }


            public RW(int null_val, int bits_per_item, params ushort?[] values) : base(null_val, bits_per_item, values.Length) { set(0, values); }


            public RW(int null_val, int bits_per_item, params short[] values) : base(null_val, bits_per_item, values.Length) { set(0, values); }


            public RW(int null_val, int bits_per_item, params short?[] values) : base(null_val, bits_per_item, values.Length) { set(0, values); }


            public RW(int null_val, int bits_per_item, params int[] values) : base(null_val, bits_per_item, values.Length) { set(0, values); }


            public RW(int null_val, int bits_per_item, params int?[] values) : base(null_val, bits_per_item, values.Length) { set(0, values); }


            public RW(int null_val, int bits_per_item, params long[] values) : base(null_val, bits_per_item, values.Length) { set(0, values); }


            public RW(int null_val, int bits_per_item, params long?[] values) : base(null_val, bits_per_item, values.Length) { set(0, values); }


            public void add(byte? value) => add(Count, value);
            public void add(byte  value) => add(Count, value);


            public void add(int index, byte? value) => add(index, value ?? null_val);

            public void add(int index, byte src)
            {
                if (index < Count) add(this, index, src);
                else set(index, src);
            }


            public void remove(byte? value) { remove(this, value ?? null_val); }
            public void remove(byte  value) { remove(this, value); }


            public void removeAt(int item) { removeAt(this, item); }


            public override byte? this[int item] { get => base[item]; set => set(this, item, value ?? null_val); }


            public void set(int item, params byte?[] values)
            {
                var fix = Count;

                for (var i = values.Length; -1 < --i;)
                    set(this, item + i, values[i] ?? null_val);

                if (fix < item && null_val != 0) nulls(this, fix, item);
            }

            public void set(int item, params ushort?[] values)
            {
                var fix = Count;

                for (var i = values.Length; -1 < --i;)
                    set(this, item + i, values[i] ?? null_val);

                if (fix < item && null_val != 0) nulls(this, fix, item);
            }

            public void set(int item, params short?[] values)
            {
                var fix = Count;

                for (var i = values.Length; -1 < --i;)
                    set(this, item + i, values[i] ?? null_val);

                if (fix < item && null_val != 0) nulls(this, fix, item);
            }

            public void set(int item, params int?[] values)
            {
                var fix = Count;

                for (var i = values.Length; -1 < --i;)
                    set(this, item + i, values[i] ?? null_val);

                if (fix < item && null_val != 0) nulls(this, fix, item);
            }

            public void set(int item, params long?[] values)
            {
                var fix = Count;

                for (var i = values.Length; -1 < --i;)
                    set(this, item + i, values[i] ?? null_val);

                if (fix < item && null_val != 0) nulls(this, fix, item);
            }

            public void set(int item, params byte[] values)
            {
                var fix = Count;
                set(this, item, values);

                if (fix < item && null_val != 0) nulls(this, fix, item);
            }

            public void set(int item, params ushort[] values)
            {
                var fix = Count;
                set(this, item, values);

                if (fix < item && null_val != 0) nulls(this, fix, item);
            }

            public void set(int item, params short[] values)
            {
                var fix = Count;
                set(this, item, values);

                if (fix < item && null_val != 0) nulls(this, fix, item);
            }

            public void set(int item, params int[] values)
            {
                var fix = Count;
                set(this, item, values);

                if (fix < item && null_val != 0) nulls(this, fix, item);
            }

            public void set(int item, params long[] values)
            {
                var fix = Count;
                set(this, item, values);

                if (fix < item && null_val != 0) nulls(this, fix, item);
            }

            public void clear()
            {
                if (Count < 1) return;
                nulls(this, 0, Count);
                Count = 0;
            }

            public new RW Clone() { return (RW)base.Clone(); }
        }
    }
}