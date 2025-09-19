
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
                            ///<summary>Describes a single constant or enum member within the protocol.</summary>
                            public partial interface Constant : AdHoc.Channel.Transmitter.BytesSrc
                            {

                                int AdHoc.Channel.Transmitter.BytesSrc.__id => __id_;
                                public const int __id_ = -6;
                                #region exT

                                public ushort _exT { get; }
                                #endregion
                                #region value_int

                                public long? _value_int { get; }
                                #endregion
                                #region value_double

                                public double? _value_double { get; }
                                #endregion
                                #region value_string

                                public string? _value_string { get; }
                                public partial struct _value_string_
                                { //The value if the constant is a string.

                                    public const int STR_LEN_MAX = 1000;
                                }
                                #endregion
                                #region array

                                //Get a reference to the field data for existence and equality checks
                                public object? _array();

                                //Get the length of all item's fixed-length collections of the multidimensional field
                                public int _array_len { get; }

                                //Get the element of the collection
                                public string _array(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                                public partial struct _array_
                                { //The values if the constant is an array.

                                    public const int STR_LEN_MAX = 1000;
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

                                                if (!__dst.init_fields_nulls(_value_int != null ? 1 : 0, 2))
                                                    return false;
                                                if (_value_double != null) __dst.set_fields_nulls(1 << 1);
                                                if (_value_string != null)
                                                    __dst.set_fields_nulls(1 << 2);
                                                if (_array() != null)
                                                    __dst.set_fields_nulls(1 << 3);
                                                if (_name != null)
                                                    __dst.set_fields_nulls(1 << 4);
                                                if (_doc != null)
                                                    __dst.set_fields_nulls(1 << 5);
                                                if (_inline_doc != null)
                                                    __dst.set_fields_nulls(1 << 6);

                                                __dst.flush_fields_nulls();
                                                goto case 3;
                                            case 3:
                                                #region value_int

                                                if (__dst.is_null(1))
                                                    goto case 4;
                                                if (__dst.put((long)_value_int!.Value, 4)) goto case 4;
                                                return false;
                                            case 4:
                                                #endregion
                                                #region value_double

                                                if (__dst.is_null(1 << 1))
                                                    goto case 5;
                                                if (__dst.put(_value_double!.Value, 5)) goto case 5;
                                                return false;
                                            case 5:
                                                #endregion
                                                #region value_string

                                                if (__dst.is_null(1 << 2))
                                                    goto case 6;
                                                if (__dst.put(_value_string!, 6)) goto case 6;
                                                return false;
                                            case 6:
                                                #endregion
                                                #region array

                                                if (__dst.is_null(1 << 3))
                                                    goto case 8;

                                                if (__slot.index_max_1(_array_len) == 0)
                                                {
                                                    if (__dst.put_val(0, 1, 8)) goto case 8;
                                                    return false;
                                                }
                                                if (!__dst.put_val((uint)__slot.index_max1, 1, 7)) return false;

                                                goto case 7;
                                            case 7:

                                                for (var b = true; b;)
                                                    if (!__dst.put(_array(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ? 7U : 8U))
                                                        return false;

                                                goto case 8;
                                            case 8:
                                                #endregion
                                                #region name

                                                if (__dst.is_null(1 << 4))
                                                    goto case 9;
                                                if (__dst.put(_name!, 9)) goto case 9;
                                                return false;
                                            case 9:
                                                #endregion
                                                #region doc

                                                if (__dst.is_null(1 << 5))
                                                    goto case 10;
                                                if (__dst.put(_doc!, 10)) goto case 10;
                                                return false;
                                            case 10:
                                                #endregion
                                                #region inline_doc

                                                if (__dst.is_null(1 << 6))
                                                    goto case 11;
                                                if (__dst.put(_inline_doc!, 11)) goto case 11;
                                                return false;
                                            case 11:
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
