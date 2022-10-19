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

namespace org.unirail.collections
{
    public interface BoolNullList
    {
        abstract class R : BitsList.R, IList<bool?>
        {
            public virtual void Add(bool? item) { throw new NotImplementedException(); }

            public bool Contains(bool? item) => 0 < IndexOf(item);

            public void CopyTo(bool?[] array, int arrayIndex) { throw new NotImplementedException(); }

            public virtual bool Remove(bool? item) { throw new NotImplementedException(); }

            public int IndexOf(bool? item) { throw new NotImplementedException(); }

            public virtual void Insert(int index, bool? item) { throw new NotImplementedException(); }

            public new virtual bool? this[int index]
            {
                get => base[index] switch
                       {
                           1 => true,
                           2 => false,
                           _ => null
                       };
                set => throw new NotImplementedException();
            }

            public int get(int index) => base[index];


            protected R(int   count) : base(2, count) { }
            protected R(bool? fill_value, int Count) : base(2, fill_value == null ? 0 : fill_value.Value ? 1 : 2, Count) { }

            public R Clone() { return (R)base.Clone(); }


            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

            public IEnumerator<bool?> GetEnumerator() => new Enumerator(this);

            public struct Enumerator : IEnumerator<bool?>, IEnumerator
            {
                private readonly IList<bool?> _list;
                private          int          _index;

                private bool? _current;

                internal Enumerator(IList<bool?> list)
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

                public bool? Current => _current!;

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
            public override void Add(bool? item) => set(Count, item);

            public override bool Remove(bool? item)
            {
                var i = IndexOf(item);
                if (i < 0) return false;
                removeAt(i);
                return true;
            }

            public override void Insert(int index, bool? item) => add(this, index, (byte)(item == null ? 0 : item.Value ? 1 : 2));

            public override bool? this[int index] { get => base[index]; set => set(index, value); }


            public override void Clear() => clear();


            public override bool IsReadOnly => true;

            public override void RemoveAt(int index) => removeAt(index);


            public RW(int count) : base(count) { }

            public RW(bool? fill_value, int count) : base(fill_value, count) { }

            public RW(params bool[] values) : base(values.Length) { set(0, values); }

            public RW(params bool?[] values) : base(values.Length) { set(0, values); }

            public void add(bool? value) { add(this, value == null ? 0 : value.Value ? 1 : 2); }

            public void remove(bool? value) { remove(this, (byte)(value == null ? 0 : value.Value ? 1 : 2)); }

            public void removeAt(int item) { removeAt(this, item); }

            public void set(bool? value) { set(this, Count, value == null ? 0 : value.Value ? 1L : 2L); }

            public void set(int item, int   value) { set(this, item, value); }
            public void set(int item, bool  value) { set(this, item, value ? 1L : 2L); }
            public void set(int item, bool? value) { set(this, item, value == null ? 0 : value.Value ? 1L : 2L); }


            public void set(int index, params bool[] values)
            {
                for (int i = 0, max = values.Length; i < max; i++)
                    set(index + i, values[i]);
            }

            public void set(int index, params bool?[] values)
            {
                for (int i = 0, max = values.Length; i < max; i++)
                    set(index + i, values[i]);
            }

            public void clear() { base.clear(); }


            public RW Clone() { return (RW)base.Clone(); }
        }
    }
}