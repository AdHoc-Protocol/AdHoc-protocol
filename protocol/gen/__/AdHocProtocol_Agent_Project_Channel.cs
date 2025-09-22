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
                    ///<summary>
                    ///Describes a single communication Channel between two Hosts.
                    ///</summary>
                    public partial interface Channel : AdHoc.Channel.Transmitter.BytesSrc
                    {
                        int AdHoc.Channel.Transmitter.BytesSrc.__id => __id_;
                        public const int __id_ = -1;
                        #region uid
                        public byte _uid { get; }
                        #endregion
                        #region hostL
                        public byte _hostL { get; }
                        #endregion
                        #region hostL_transmitting_packs
                        //Get a reference to the field data for existence and equality checks
                        public object? _hostL_transmitting_packs();

                        //Get the length of all item's fixed-length collections of the multidimensional field
                        public int _hostL_transmitting_packs_len { get; }

                        //Get the element of the collection
                        public ushort _hostL_transmitting_packs(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);

                        public partial struct _hostL_transmitting_packs_
                        {
                            public const int ARRAY_LEN_MAX = 65535;
                        }
                        #endregion
                        #region hostL_related_packs
                        //Get a reference to the field data for existence and equality checks
                        public object? _hostL_related_packs();

                        //Get the length of all item's fixed-length collections of the multidimensional field
                        public int _hostL_related_packs_len { get; }

                        //Get the element of the collection
                        public ushort _hostL_related_packs(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);

                        public partial struct _hostL_related_packs_
                        {
                            public const int ARRAY_LEN_MAX = 65535;
                        }
                        #endregion
                        #region hostR
                        public byte _hostR { get; }
                        #endregion
                        #region hostR_transmitting_packs
                        //Get a reference to the field data for existence and equality checks
                        public object? _hostR_transmitting_packs();

                        //Get the length of all item's fixed-length collections of the multidimensional field
                        public int _hostR_transmitting_packs_len { get; }

                        //Get the element of the collection
                        public ushort _hostR_transmitting_packs(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);

                        public partial struct _hostR_transmitting_packs_
                        {
                            public const int ARRAY_LEN_MAX = 65535;
                        }
                        #endregion
                        #region hostR_related_packs
                        //Get a reference to the field data for existence and equality checks
                        public object? _hostR_related_packs();

                        //Get the length of all item's fixed-length collections of the multidimensional field
                        public int _hostR_related_packs_len { get; }

                        //Get the element of the collection
                        public ushort _hostR_related_packs(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);

                        public partial struct _hostR_related_packs_
                        {
                            public const int ARRAY_LEN_MAX = 65535;
                        }
                        #endregion
                        #region stages
                        //Get a reference to the field data for existence and equality checks
                        public object? _stages();

                        //Get the length of all item's fixed-length collections of the multidimensional field
                        public int _stages_len { get; }

                        //Get the element of the collection
                        public Agent.AdHocProtocol.Agent_.Project.Channel.Stage _stages(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);

                        public partial struct _stages_
                        {
                            public const int ARRAY_LEN_MAX = 4095;
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

                                        if (!__dst.Allocate(3, 1))
                                            return false;
                                        __dst.put((byte)_uid);
                                        __dst.put((byte)_hostL);
                                        __dst.put((byte)_hostR);

                                        goto case 2;
                                    case 2:

                                        if (!__dst.init_fields_nulls(_hostL_transmitting_packs() != null ?
                                                                         1 :
                                                                         0, 2))
                                            return false;
                                        if (_hostL_related_packs() != null) __dst.set_fields_nulls(1 << 1);
                                        if (_hostR_transmitting_packs() != null)
                                            __dst.set_fields_nulls(1 << 2);
                                        if (_hostR_related_packs() != null)
                                            __dst.set_fields_nulls(1 << 3);
                                        if (_stages() != null)
                                            __dst.set_fields_nulls(1 << 4);
                                        if (_parent != 0xFFFF)
                                            __dst.set_fields_nulls(1 << 5);
                                        if (_constants() != null)
                                            __dst.set_fields_nulls(1 << 6);
                                        if (_name != null)
                                            __dst.set_fields_nulls(1 << 7);

                                        __dst.flush_fields_nulls();
                                        goto case 3;
                                    case 3:
                                        #region hostL_transmitting_packs
                                        if (__dst.is_null(1))
                                            goto case 5;

                                        if (__slot.index_max_1(_hostL_transmitting_packs_len) == 0)
                                        {
                                            if (__dst.put_val(0, 2, 5)) goto case 5;
                                            return false;
                                        }

                                        if (!__dst.put_val((uint)__slot.index_max1, 2, 4)) return false;

                                        goto case 4;
                                    case 4:

                                        if ((__v = __dst.remaining / 2) < (__i = __slot.index_max1 - __slot.index1))
                                        {
                                            if (0 < __v)
                                            {
                                                __slot.index1 = __v += __i = __slot.index1;
                                                for (; __i < __v; __i++)
                                                    __dst.put((ushort)_hostL_transmitting_packs(__dst, __slot, __i));
                                            }

                                            __dst.retry_at(4);
                                            return false;
                                        }

                                        __i += __v = __slot.index1;
                                        for (; __v < __i; __v++) __dst.put((ushort)_hostL_transmitting_packs(__dst, __slot, __v));
                                        goto case 5;
                                    case 5:
                                        #endregion
                                        #region hostL_related_packs
                                        if (__dst.is_null(1 << 1))
                                            goto case 7;

                                        if (__slot.index_max_1(_hostL_related_packs_len) == 0)
                                        {
                                            if (__dst.put_val(0, 2, 7)) goto case 7;
                                            return false;
                                        }

                                        if (!__dst.put_val((uint)__slot.index_max1, 2, 6)) return false;

                                        goto case 6;
                                    case 6:

                                        if ((__v = __dst.remaining / 2) < (__i = __slot.index_max1 - __slot.index1))
                                        {
                                            if (0 < __v)
                                            {
                                                __slot.index1 = __v += __i = __slot.index1;
                                                for (; __i < __v; __i++)
                                                    __dst.put((ushort)_hostL_related_packs(__dst, __slot, __i));
                                            }

                                            __dst.retry_at(6);
                                            return false;
                                        }

                                        __i += __v = __slot.index1;
                                        for (; __v < __i; __v++) __dst.put((ushort)_hostL_related_packs(__dst, __slot, __v));
                                        goto case 7;
                                    case 7:
                                        #endregion
                                        #region hostR_transmitting_packs
                                        if (__dst.is_null(1 << 2))
                                            goto case 9;

                                        if (__slot.index_max_1(_hostR_transmitting_packs_len) == 0)
                                        {
                                            if (__dst.put_val(0, 2, 9)) goto case 9;
                                            return false;
                                        }

                                        if (!__dst.put_val((uint)__slot.index_max1, 2, 8)) return false;

                                        goto case 8;
                                    case 8:

                                        if ((__v = __dst.remaining / 2) < (__i = __slot.index_max1 - __slot.index1))
                                        {
                                            if (0 < __v)
                                            {
                                                __slot.index1 = __v += __i = __slot.index1;
                                                for (; __i < __v; __i++)
                                                    __dst.put((ushort)_hostR_transmitting_packs(__dst, __slot, __i));
                                            }

                                            __dst.retry_at(8);
                                            return false;
                                        }

                                        __i += __v = __slot.index1;
                                        for (; __v < __i; __v++) __dst.put((ushort)_hostR_transmitting_packs(__dst, __slot, __v));
                                        goto case 9;
                                    case 9:
                                        #endregion
                                        #region hostR_related_packs
                                        if (__dst.is_null(1 << 3))
                                            goto case 11;

                                        if (__slot.index_max_1(_hostR_related_packs_len) == 0)
                                        {
                                            if (__dst.put_val(0, 2, 11)) goto case 11;
                                            return false;
                                        }

                                        if (!__dst.put_val((uint)__slot.index_max1, 2, 10)) return false;

                                        goto case 10;
                                    case 10:

                                        if ((__v = __dst.remaining / 2) < (__i = __slot.index_max1 - __slot.index1))
                                        {
                                            if (0 < __v)
                                            {
                                                __slot.index1 = __v += __i = __slot.index1;
                                                for (; __i < __v; __i++)
                                                    __dst.put((ushort)_hostR_related_packs(__dst, __slot, __i));
                                            }

                                            __dst.retry_at(10);
                                            return false;
                                        }

                                        __i += __v = __slot.index1;
                                        for (; __v < __i; __v++) __dst.put((ushort)_hostR_related_packs(__dst, __slot, __v));
                                        goto case 11;
                                    case 11:
                                        #endregion
                                        #region stages
                                        if (__dst.is_null(1 << 4))
                                            goto case 13;

                                        if (__slot.index_max_1(_stages_len) == 0)
                                        {
                                            if (__dst.put_val(0, 2, 13)) goto case 13;
                                            return false;
                                        }

                                        if (!__dst.put_val((uint)__slot.index_max1, 2, 12)) return false;

                                        goto case 12;
                                    case 12:

                                        for (var b = true; b;)
                                            if (!__dst.put_bytes(_stages(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ?
                                                                                                             12U :
                                                                                                             13U))
                                                return false;

                                        goto case 13;
                                    case 13:
                                        #endregion
                                        #region parent
                                        if (__dst.is_null(1 << 5))
                                            goto case 14;
                                        if (__dst.put((ushort)_parent, 14)) goto case 14;
                                        return false;
                                    case 14:
                                        #endregion
                                        #region constants
                                        if (__dst.is_null(1 << 6))
                                            goto case 16;

                                        if (__slot.index_max_1(_constants_len) == 0)
                                        {
                                            if (__dst.put_val(0, 2, 16)) goto case 16;
                                            return false;
                                        }

                                        if (!__dst.put_val((uint)__slot.index_max1, 2, 15)) return false;

                                        goto case 15;
                                    case 15:

                                        if ((__v = __dst.remaining / 4) < (__i = __slot.index_max1 - __slot.index1))
                                        {
                                            if (0 < __v)
                                            {
                                                __slot.index1 = __v += __i = __slot.index1;
                                                for (; __i < __v; __i++)
                                                    __dst.put((int)_constants(__dst, __slot, __i));
                                            }

                                            __dst.retry_at(15);
                                            return false;
                                        }

                                        __i += __v = __slot.index1;
                                        for (; __v < __i; __v++) __dst.put((int)_constants(__dst, __slot, __v));
                                        goto case 16;
                                    case 16:
                                        #endregion
                                        #region name
                                        if (__dst.is_null(1 << 7))
                                            goto case 17;
                                        if (__dst.put(_name!, 17)) goto case 17;
                                        return false;
                                    case 17:
                                        #endregion

                                        if (!__dst.init_fields_nulls(_doc != null ?
                                                                         1 :
                                                                         0, 17))
                                            return false;
                                        if (_inline_doc != null) __dst.set_fields_nulls(1 << 1);

                                        __dst.flush_fields_nulls();
                                        goto case 18;
                                    case 18:
                                        #region doc
                                        if (__dst.is_null(1))
                                            goto case 19;
                                        if (__dst.put(_doc!, 19)) goto case 19;
                                        return false;
                                    case 19:
                                        #endregion
                                        #region inline_doc
                                        if (__dst.is_null(1 << 1))
                                            goto case 20;
                                        if (__dst.put(_inline_doc!, 20)) goto case 20;
                                        return false;
                                    case 20:
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