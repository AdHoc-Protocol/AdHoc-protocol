
using System;
using org.unirail;
using org.unirail.collections;
namespace org.unirail
{
    namespace Agent
    {
        public static partial class AdHocProtocol
        {
            public partial class Agent_
            {
                public partial interface Project
                {
                    public partial interface Host
                    {
                        ///<summary>
                        ///Describes a single Pack (data structure) within the user's project.
                        ///</summary>
                        public partial interface Pack : AdHoc.Channel.Transmitter.BytesSrc
                        {

                            int AdHoc.Channel.Transmitter.BytesSrc.__id => __id_;
                            public const int __id_ = -5;
                            #region id

                            public ushort _id { get; }
                            #endregion
                            #region uid

                            public ushort _uid { get; }
                            #endregion
                            #region nested_max

                            public byte _nested_max { get; }
                            public partial struct _nested_max_
                            {

                                public const byte NULL = (byte)0xFF;
                                public const byte MAX = 0xFE;
                            }
                            #endregion
                            #region referred

                            public bool _referred { get; }
                            #endregion
                            #region fields

                            //Get a reference to the field data for existence and equality checks
                            public object? _fields();

                            //Get the length of all item's fixed-length collections of the multidimensional field
                            public int _fields_len { get; }

                            //Get the element of the collection
                            public int _fields(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                            public partial struct _fields_
                            {

                                public const int ARRAY_LEN_MAX = 65000;
                            }
                            #endregion
                            #region parent

                            public ushort _parent { get; }
                            public partial struct _parent_
                            {

                                public const ushort NULL = (ushort)0xFFFF;
                                public const ushort MAX = 0xFFFE;
                            }
                            #endregion
                            #region constants

                            //Get a reference to the field data for existence and equality checks
                            public object? _constants();

                            //Get the length of all item's fixed-length collections of the multidimensional field
                            public int _constants_len { get; }

                            //Get the element of the collection
                            public int _constants(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                            public partial struct _constants_
                            {

                                public const int ARRAY_LEN_MAX = 65000;
                            }
                            #endregion
                            #region name

                            public string? _name { get; }
                            public partial struct _name_
                            {

                                public const int STR_LEN_MAX = 255;
                            }
                            #endregion
                            #region doc

                            public string? _doc { get; }
                            public partial struct _doc_
                            { //Field for full, XML-style documentation.

                                public const int STR_LEN_MAX = 65000;
                            }
                            #endregion
                            #region inline_doc

                            public string? _inline_doc { get; }
                            public partial struct _inline_doc_
                            { //Field for a short, single-line summary.

                                public const int STR_LEN_MAX = 255;
                            }
                            #endregion

                            bool AdHoc.Channel.Transmitter.BytesSrc.__get_bytes(AdHoc.Channel.Transmitter __dst)
                            {
                                var __slot = __dst.slot!;
                                int __i = 0, __t = 0, __v = 0;
                                ulong __value = 0;
                                for (; ; )
                                    switch (__slot.state)
                                    {
                                        case 0:
                                            throw new NotSupportedException();
                                        case 1:

                                            if (!__dst.Allocate(4, 1))
                                                return false;
                                            __dst.put((ushort)_id);
                                            __dst.put((ushort)_uid);

                                            goto case 2;
                                        case 2:

                                            if (!__dst.init_bits(1, 2))
                                                return false;
                                            #region referred
                                            __dst.put((bool)_referred);
                                            #endregion

                                            goto case 3;
                                        case 3:

                                            __dst.end_bits();
                                            goto case 4;
                                        case 4:

                                            if (!__dst.init_fields_nulls(_nested_max != 0xFF ? 1 : 0, 4))
                                                return false;
                                            if (_fields() != null) __dst.set_fields_nulls(1 << 1);
                                            if (_parent != 0xFFFF)
                                                __dst.set_fields_nulls(1 << 2);
                                            if (_constants() != null)
                                                __dst.set_fields_nulls(1 << 3);
                                            if (_name != null)
                                                __dst.set_fields_nulls(1 << 4);
                                            if (_doc != null)
                                                __dst.set_fields_nulls(1 << 5);
                                            if (_inline_doc != null)
                                                __dst.set_fields_nulls(1 << 6);

                                            __dst.flush_fields_nulls();
                                            goto case 5;
                                        case 5:
                                            #region nested_max

                                            if (__dst.is_null(1))
                                                goto case 6;
                                            if (__dst.put((byte)_nested_max, 6)) goto case 6;
                                            return false;
                                        case 6:
                                            #endregion
                                            #region fields

                                            if (__dst.is_null(1 << 1))
                                                goto case 8;

                                            if (__slot.index_max_1(_fields_len) == 0)
                                            {
                                                if (__dst.put_val(0, 2, 8)) goto case 8;
                                                return false;
                                            }
                                            if (!__dst.put_val((uint)__slot.index_max1, 2, 7)) return false;

                                            goto case 7;
                                        case 7:

                                            if ((__v = __dst.remaining / 4) < (__i = __slot.index_max1 - __slot.index1))
                                            {
                                                if (0 < __v)
                                                {
                                                    __slot.index1 = __v += __i = __slot.index1;
                                                    for (; __i < __v; __i++)
                                                        __dst.put((int)_fields(__dst, __slot, __i));
                                                }
                                                __dst.retry_at(7);
                                                return false;
                                            }
                                            __i += __v = __slot.index1;
                                            for (; __v < __i; __v++) __dst.put((int)_fields(__dst, __slot, __v));
                                            goto case 8;
                                        case 8:
                                            #endregion
                                            #region parent

                                            if (__dst.is_null(1 << 2))
                                                goto case 9;
                                            if (__dst.put((ushort)_parent, 9)) goto case 9;
                                            return false;
                                        case 9:
                                            #endregion
                                            #region constants

                                            if (__dst.is_null(1 << 3))
                                                goto case 11;

                                            if (__slot.index_max_1(_constants_len) == 0)
                                            {
                                                if (__dst.put_val(0, 2, 11)) goto case 11;
                                                return false;
                                            }
                                            if (!__dst.put_val((uint)__slot.index_max1, 2, 10)) return false;

                                            goto case 10;
                                        case 10:

                                            if ((__v = __dst.remaining / 4) < (__i = __slot.index_max1 - __slot.index1))
                                            {
                                                if (0 < __v)
                                                {
                                                    __slot.index1 = __v += __i = __slot.index1;
                                                    for (; __i < __v; __i++)
                                                        __dst.put((int)_constants(__dst, __slot, __i));
                                                }
                                                __dst.retry_at(10);
                                                return false;
                                            }
                                            __i += __v = __slot.index1;
                                            for (; __v < __i; __v++) __dst.put((int)_constants(__dst, __slot, __v));
                                            goto case 11;
                                        case 11:
                                            #endregion
                                            #region name

                                            if (__dst.is_null(1 << 4))
                                                goto case 12;
                                            if (__dst.put(_name!, 12)) goto case 12;
                                            return false;
                                        case 12:
                                            #endregion
                                            #region doc

                                            if (__dst.is_null(1 << 5))
                                                goto case 13;
                                            if (__dst.put(_doc!, 13)) goto case 13;
                                            return false;
                                        case 13:
                                            #endregion
                                            #region inline_doc

                                            if (__dst.is_null(1 << 6))
                                                goto case 14;
                                            if (__dst.put(_inline_doc!, 14)) goto case 14;
                                            return false;
                                        case 14:
                                        #endregion

                                        default:
                                            return true;
                                    }
                            }
                        }
                    }
                }
            }
        }
    }
}
