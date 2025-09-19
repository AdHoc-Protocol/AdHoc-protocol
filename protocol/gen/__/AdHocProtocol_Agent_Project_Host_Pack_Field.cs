
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
                        public partial interface Pack
                        {
                            ///<summary>
                            ///Describes a single Field within a Pack, including its type, constraints, and attributes.
                            ///</summary>
                            public partial interface Field : AdHoc.Channel.Transmitter.BytesSrc
                            {

                                int AdHoc.Channel.Transmitter.BytesSrc.__id => __id_;
                                public const int __id_ = -7;
                                #region dims

                                //Get a reference to the field data for existence and equality checks
                                public object? _dims();

                                //Get the length of all item's fixed-length collections of the multidimensional field
                                public int _dims_len { get; }

                                //Get the element of the collection
                                public int _dims(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                                public partial struct _dims_
                                {

                                    public const int ARRAY_LEN_MAX = 32;
                                }
                                #endregion
                                #region map_set_len

                                public uint? _map_set_len { get; }
                                #endregion
                                #region map_set_array

                                public uint? _map_set_array { get; }
                                #endregion
                                #region exT

                                public ushort _exT { get; }
                                #endregion
                                #region exT_len

                                public uint? _exT_len { get; }
                                #endregion
                                #region exT_array

                                public uint? _exT_array { get; }
                                #endregion
                                #region inT

                                public ushort? _inT { get; }
                                #endregion
                                #region min_value

                                public long? _min_value { get; }
                                #endregion
                                #region max_value

                                public long? _max_value { get; }
                                #endregion
                                #region dir

                                public sbyte _dir { get; }
                                public partial struct _dir_
                                {

                                    public const sbyte NULL = (sbyte)0x2;
                                    public const sbyte MIN = -1;
                                    public const sbyte MAX = 0x1;
                                }
                                #endregion
                                #region min_valueD

                                public double? _min_valueD { get; }
                                #endregion
                                #region max_valueD

                                public double? _max_valueD { get; }
                                #endregion
                                #region bits

                                public byte _bits { get; }
                                public partial struct _bits_
                                {

                                    public const byte NULL = (byte)0x0;
                                    public const byte MIN = 0x1;
                                    public const byte MAX = 0x7;
                                }
                                #endregion
                                #region null_value

                                public byte? _null_value { get; }
                                #endregion
                                #region exTV

                                public ushort? _exTV { get; }
                                #endregion
                                #region exTV_len

                                public uint? _exTV_len { get; }
                                #endregion
                                #region exTV_array

                                public uint? _exTV_array { get; }
                                #endregion
                                #region inTV

                                public ushort? _inTV { get; }
                                #endregion
                                #region min_valueV

                                public long? _min_valueV { get; }
                                #endregion
                                #region max_valueV

                                public long? _max_valueV { get; }
                                #endregion
                                #region dirV

                                public sbyte _dirV { get; }
                                public partial struct _dirV_
                                {

                                    public const sbyte NULL = (sbyte)0x2;
                                    public const sbyte MIN = -1;
                                    public const sbyte MAX = 0x1;
                                }
                                #endregion
                                #region min_valueDV

                                public double? _min_valueDV { get; }
                                #endregion
                                #region max_valueDV

                                public double? _max_valueDV { get; }
                                #endregion
                                #region bitsV

                                public byte _bitsV { get; }
                                public partial struct _bitsV_
                                {

                                    public const byte NULL = (byte)0x0;
                                    public const byte MIN = 0x1;
                                    public const byte MAX = 0x7;
                                }
                                #endregion
                                #region null_valueV

                                public byte? _null_valueV { get; }
                                #endregion
                                #region attributes

                                //Get a reference to the field data for existence and equality checks
                                public object? _attributes();

                                //Get the length of all item's fixed-length collections of the multidimensional field
                                public int _attributes_len { get; }

                                //Get the element of the collection
                                public Agent.AdHocProtocol.Agent_.Project.Host.Pack _attributes(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                                public partial struct _attributes_
                                {

                                    public const int ARRAY_LEN_MAX = 255;
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

                                                if (!__dst.Allocate(2, 1))
                                                    return false;
                                                __dst.put((ushort)_exT);

                                                goto case 2;
                                            case 2:

                                                if (!__dst.init_bits(2, 2))
                                                    return false;
                                                #region dir
                                                __dst.put_bits((int)((int)((sbyte)(_dir + 0x1))), 2);
                                                #endregion
                                                #region bits
                                                __dst.put_bits((int)((int)(_bits)), 3);
                                                #endregion
                                                #region dirV
                                                __dst.put_bits((int)((int)((sbyte)(_dirV + 0x1))), 2);
                                                #endregion
                                                #region bitsV
                                                __dst.put_bits((int)((int)(_bitsV)), 3);
                                                #endregion

                                                goto case 3;
                                            case 3:

                                                __dst.end_bits();
                                                goto case 4;
                                            case 4:

                                                if (!__dst.init_fields_nulls(_dims() != null ? 1 : 0, 4))
                                                    return false;
                                                if (_map_set_len != null) __dst.set_fields_nulls(1 << 1);
                                                if (_map_set_array != null)
                                                    __dst.set_fields_nulls(1 << 2);
                                                if (_exT_len != null)
                                                    __dst.set_fields_nulls(1 << 3);
                                                if (_exT_array != null)
                                                    __dst.set_fields_nulls(1 << 4);
                                                if (_inT != null)
                                                    __dst.set_fields_nulls(1 << 5);
                                                if (_min_value != null)
                                                    __dst.set_fields_nulls(1 << 6);
                                                if (_max_value != null)
                                                    __dst.set_fields_nulls(1 << 7);

                                                __dst.flush_fields_nulls();
                                                goto case 5;
                                            case 5:
                                                #region dims

                                                if (__dst.is_null(1))
                                                    goto case 7;

                                                if (__slot.index_max_1(_dims_len) == 0)
                                                {
                                                    if (__dst.put_val(0, 1, 7)) goto case 7;
                                                    return false;
                                                }
                                                if (!__dst.put_val((uint)__slot.index_max1, 1, 6)) return false;

                                                goto case 6;
                                            case 6:

                                                if ((__v = __dst.remaining / 4) < (__i = __slot.index_max1 - __slot.index1))
                                                {
                                                    if (0 < __v)
                                                    {
                                                        __slot.index1 = __v += __i = __slot.index1;
                                                        for (; __i < __v; __i++)
                                                            __dst.put((int)_dims(__dst, __slot, __i));
                                                    }
                                                    __dst.retry_at(6);
                                                    return false;
                                                }
                                                __i += __v = __slot.index1;
                                                for (; __v < __i; __v++) __dst.put((int)_dims(__dst, __slot, __v));
                                                goto case 7;
                                            case 7:
                                                #endregion
                                                #region map_set_len

                                                if (__dst.is_null(1 << 1))
                                                    goto case 8;
                                                if (__dst.put((uint)_map_set_len!.Value, 8)) goto case 8;
                                                return false;
                                            case 8:
                                                #endregion
                                                #region map_set_array

                                                if (__dst.is_null(1 << 2))
                                                    goto case 9;
                                                if (__dst.put((uint)_map_set_array!.Value, 9)) goto case 9;
                                                return false;
                                            case 9:
                                                #endregion
                                                #region exT_len

                                                if (__dst.is_null(1 << 3))
                                                    goto case 10;
                                                if (__dst.put((uint)_exT_len!.Value, 10)) goto case 10;
                                                return false;
                                            case 10:
                                                #endregion
                                                #region exT_array

                                                if (__dst.is_null(1 << 4))
                                                    goto case 11;
                                                if (__dst.put((uint)_exT_array!.Value, 11)) goto case 11;
                                                return false;
                                            case 11:
                                                #endregion
                                                #region inT

                                                if (__dst.is_null(1 << 5))
                                                    goto case 12;
                                                if (__dst.put((ushort)_inT!.Value, 12)) goto case 12;
                                                return false;
                                            case 12:
                                                #endregion
                                                #region min_value

                                                if (__dst.is_null(1 << 6))
                                                    goto case 13;
                                                if (__dst.put((long)_min_value!.Value, 13)) goto case 13;
                                                return false;
                                            case 13:
                                                #endregion
                                                #region max_value

                                                if (__dst.is_null(1 << 7))
                                                    goto case 14;
                                                if (__dst.put((long)_max_value!.Value, 14)) goto case 14;
                                                return false;
                                            case 14:
                                                #endregion

                                                if (!__dst.init_fields_nulls(_min_valueD != null ? 1 : 0, 14))
                                                    return false;
                                                if (_max_valueD != null) __dst.set_fields_nulls(1 << 1);
                                                if (_null_value != null) __dst.set_fields_nulls(1 << 2);
                                                if (_exTV != null) __dst.set_fields_nulls(1 << 3);
                                                if (_exTV_len != null) __dst.set_fields_nulls(1 << 4);
                                                if (_exTV_array != null) __dst.set_fields_nulls(1 << 5);
                                                if (_inTV != null) __dst.set_fields_nulls(1 << 6);
                                                if (_min_valueV != null) __dst.set_fields_nulls(1 << 7);

                                                __dst.flush_fields_nulls();
                                                goto case 15;
                                            case 15:
                                                #region min_valueD

                                                if (__dst.is_null(1))
                                                    goto case 16;
                                                if (__dst.put(_min_valueD!.Value, 16)) goto case 16;
                                                return false;
                                            case 16:
                                                #endregion
                                                #region max_valueD

                                                if (__dst.is_null(1 << 1))
                                                    goto case 17;
                                                if (__dst.put(_max_valueD!.Value, 17)) goto case 17;
                                                return false;
                                            case 17:
                                                #endregion
                                                #region null_value

                                                if (__dst.is_null(1 << 2))
                                                    goto case 18;
                                                if (__dst.put((byte)_null_value!.Value, 18)) goto case 18;
                                                return false;
                                            case 18:
                                                #endregion
                                                #region exTV

                                                if (__dst.is_null(1 << 3))
                                                    goto case 19;
                                                if (__dst.put((ushort)_exTV!.Value, 19)) goto case 19;
                                                return false;
                                            case 19:
                                                #endregion
                                                #region exTV_len

                                                if (__dst.is_null(1 << 4))
                                                    goto case 20;
                                                if (__dst.put((uint)_exTV_len!.Value, 20)) goto case 20;
                                                return false;
                                            case 20:
                                                #endregion
                                                #region exTV_array

                                                if (__dst.is_null(1 << 5))
                                                    goto case 21;
                                                if (__dst.put((uint)_exTV_array!.Value, 21)) goto case 21;
                                                return false;
                                            case 21:
                                                #endregion
                                                #region inTV

                                                if (__dst.is_null(1 << 6))
                                                    goto case 22;
                                                if (__dst.put((ushort)_inTV!.Value, 22)) goto case 22;
                                                return false;
                                            case 22:
                                                #endregion
                                                #region min_valueV

                                                if (__dst.is_null(1 << 7))
                                                    goto case 23;
                                                if (__dst.put((long)_min_valueV!.Value, 23)) goto case 23;
                                                return false;
                                            case 23:
                                                #endregion

                                                if (!__dst.init_fields_nulls(_max_valueV != null ? 1 : 0, 23))
                                                    return false;
                                                if (_min_valueDV != null) __dst.set_fields_nulls(1 << 1);
                                                if (_max_valueDV != null) __dst.set_fields_nulls(1 << 2);
                                                if (_null_valueV != null) __dst.set_fields_nulls(1 << 3);
                                                if (_attributes() != null) __dst.set_fields_nulls(1 << 4);
                                                if (_name != null) __dst.set_fields_nulls(1 << 5);
                                                if (_doc != null) __dst.set_fields_nulls(1 << 6);
                                                if (_inline_doc != null) __dst.set_fields_nulls(1 << 7);

                                                __dst.flush_fields_nulls();
                                                goto case 24;
                                            case 24:
                                                #region max_valueV

                                                if (__dst.is_null(1))
                                                    goto case 25;
                                                if (__dst.put((long)_max_valueV!.Value, 25)) goto case 25;
                                                return false;
                                            case 25:
                                                #endregion
                                                #region min_valueDV

                                                if (__dst.is_null(1 << 1))
                                                    goto case 26;
                                                if (__dst.put(_min_valueDV!.Value, 26)) goto case 26;
                                                return false;
                                            case 26:
                                                #endregion
                                                #region max_valueDV

                                                if (__dst.is_null(1 << 2))
                                                    goto case 27;
                                                if (__dst.put(_max_valueDV!.Value, 27)) goto case 27;
                                                return false;
                                            case 27:
                                                #endregion
                                                #region null_valueV

                                                if (__dst.is_null(1 << 3))
                                                    goto case 28;
                                                if (__dst.put((byte)_null_valueV!.Value, 28)) goto case 28;
                                                return false;
                                            case 28:
                                                #endregion
                                                #region attributes

                                                if (__dst.is_null(1 << 4))
                                                    goto case 30;

                                                if (__slot.index_max_1(_attributes_len) == 0)
                                                {
                                                    if (__dst.put_val(0, 1, 30)) goto case 30;
                                                    return false;
                                                }
                                                if (!__dst.put_val((uint)__slot.index_max1, 1, 29)) return false;

                                                goto case 29;
                                            case 29:

                                                for (var b = true; b;)
                                                    if (!__dst.put_bytes(_attributes(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ? 29U : 30U))
                                                        return false;

                                                goto case 30;
                                            case 30:
                                                #endregion
                                                #region name

                                                if (__dst.is_null(1 << 5))
                                                    goto case 31;
                                                if (__dst.put(_name!, 31)) goto case 31;
                                                return false;
                                            case 31:
                                                #endregion
                                                #region doc

                                                if (__dst.is_null(1 << 6))
                                                    goto case 32;
                                                if (__dst.put(_doc!, 32)) goto case 32;
                                                return false;
                                            case 32:
                                                #endregion
                                                #region inline_doc

                                                if (__dst.is_null(1 << 7))
                                                    goto case 33;
                                                if (__dst.put(_inline_doc!, 33)) goto case 33;
                                                return false;
                                            case 33:
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
}
