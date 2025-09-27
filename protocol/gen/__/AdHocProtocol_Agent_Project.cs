
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
                ///<summary>
                ///This is the central "meta-pack" of the system. It contains a complete, serialized
                ///description of a user's AdHoc protocol project. The Agent constructs this pack and sends
                ///it to the Server, which uses this structured data to perform code generation.
                ///</summary>
                public partial interface Project : AdHoc.Channel.Transmitter.BytesSrc
                {

                    int AdHoc.Channel.Transmitter.BytesSrc.__id => __id_;
                    public const int __id_ = 8;
                    #region task

                    public string? _task { get; }
                    public partial struct _task_
                    {

                        public const int STR_LEN_MAX = 255;
                    }
                    #endregion
                    #region namespacE

                    public string? _namespacE { get; }
                    public partial struct _namespacE_
                    {

                        public const int STR_LEN_MAX = 255;
                    }
                    #endregion
                    #region time

                    public long _time { get; }
                    #endregion
                    #region source

                    //Get a reference to the field data for existence and equality checks
                    public object? _source();

                    //Get the length of all item's fixed-length collections of the multidimensional field
                    public int _source_len { get; }

                    //Get the element of the collection
                    public byte _source(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                    public partial struct _source_
                    {

                        public const int ARRAY_LEN_MAX = 131071;
                    }
                    #endregion
                    #region uid

                    public ulong _uid { get; }
                    #endregion
                    #region imported_projects_uid

                    //Get a reference to the field data for existence and equality checks
                    public object? _imported_projects_uid();

                    //Get the length of all item's fixed-length collections of the multidimensional field
                    public int _imported_projects_uid_len { get; }

                    //Get the element of the collection
                    public ulong _imported_projects_uid(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                    public partial struct _imported_projects_uid_
                    {

                        public const int ARRAY_LEN_MAX = 255;
                    }
                    #endregion
                    #region fields

                    //Get a reference to the field data for existence and equality checks
                    public object? _fields();

                    //Get the length of all item's fixed-length collections of the multidimensional field
                    public int _fields_len { get; }

                    //Get the element of the collection
                    public Agent.AdHocProtocol.Agent_.Project.Host.Pack.Field _fields(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                    public partial struct _fields_
                    {

                        public const int ARRAY_LEN_MAX = 65535;
                    }
                    #endregion
                    #region constant_fields

                    //Get a reference to the field data for existence and equality checks
                    public object? _constant_fields();

                    //Get the length of all item's fixed-length collections of the multidimensional field
                    public int _constant_fields_len { get; }

                    //Get the element of the collection
                    public Agent.AdHocProtocol.Agent_.Project.Host.Pack.Constant _constant_fields(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                    public partial struct _constant_fields_
                    {

                        public const int ARRAY_LEN_MAX = 65535;
                    }
                    #endregion
                    #region packs

                    //Get a reference to the field data for existence and equality checks
                    public object? _packs();

                    //Get the length of all item's fixed-length collections of the multidimensional field
                    public int _packs_len { get; }

                    //Get the element of the collection
                    public Agent.AdHocProtocol.Agent_.Project.Host.Pack _packs(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                    public partial struct _packs_
                    {

                        public const int ARRAY_LEN_MAX = 65535;
                    }
                    #endregion
                    #region hosts

                    //Get a reference to the field data for existence and equality checks
                    public object? _hosts();

                    //Get the length of all item's fixed-length collections of the multidimensional field
                    public int _hosts_len { get; }

                    //Get the element of the collection
                    public Agent.AdHocProtocol.Agent_.Project.Host _hosts(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                    public partial struct _hosts_
                    {

                        public const int ARRAY_LEN_MAX = 255;
                    }
                    #endregion
                    #region channels

                    //Get a reference to the field data for existence and equality checks
                    public object? _channels();

                    //Get the length of all item's fixed-length collections of the multidimensional field
                    public int _channels_len { get; }

                    //Get the element of the collection
                    public Agent.AdHocProtocol.Agent_.Project.Channel _channels(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                    public partial struct _channels_
                    {

                        public const int ARRAY_LEN_MAX = 255;
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
                                    if (__dst.put_val(__id_, 1, 1))
                                        goto case 1;
                                    return false;

                                case 1:

                                    if (!__dst.Allocate(16, 1))
                                        return false;
                                    __dst.put((long)_time);
                                    __dst.put((ulong)_uid);

                                    goto case 2;
                                case 2:

                                    if (!__dst.init_fields_nulls(_task != null ? 1 : 0, 2))
                                        return false;
                                    if (_namespacE != null) __dst.set_fields_nulls(1 << 1);
                                    if (_source() != null)
                                        __dst.set_fields_nulls(1 << 2);
                                    if (_imported_projects_uid() != null)
                                        __dst.set_fields_nulls(1 << 3);
                                    if (_fields() != null)
                                        __dst.set_fields_nulls(1 << 4);
                                    if (_constant_fields() != null)
                                        __dst.set_fields_nulls(1 << 5);
                                    if (_packs() != null)
                                        __dst.set_fields_nulls(1 << 6);
                                    if (_hosts() != null)
                                        __dst.set_fields_nulls(1 << 7);

                                    __dst.flush_fields_nulls();
                                    goto case 3;
                                case 3:
                                    #region task

                                    if (__dst.is_null(1))
                                        goto case 4;
                                    if (__dst.put(_task!, 4)) goto case 4;
                                    return false;
                                case 4:
                                    #endregion
                                    #region namespacE

                                    if (__dst.is_null(1 << 1))
                                        goto case 5;
                                    if (__dst.put(_namespacE!, 5)) goto case 5;
                                    return false;
                                case 5:
                                    #endregion
                                    #region source

                                    if (__dst.is_null(1 << 2))
                                        goto case 7;

                                    if (__slot.index_max_1(_source_len) == 0)
                                    {
                                        if (__dst.put_val(0, 3, 7)) goto case 7;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 3, 6)) return false;

                                    goto case 6;
                                case 6:

                                    if ((__v = __dst.remaining) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __v)
                                        {
                                            __slot.index1 = __v += __i = __slot.index1;
                                            for (; __i < __v; __i++)
                                                __dst.put((byte)_source(__dst, __slot, __i));
                                        }
                                        __dst.retry_at(6);
                                        return false;
                                    }
                                    __i += __v = __slot.index1;
                                    for (; __v < __i; __v++) __dst.put((byte)_source(__dst, __slot, __v));
                                    goto case 7;
                                case 7:
                                    #endregion
                                    #region imported_projects_uid

                                    if (__dst.is_null(1 << 3))
                                        goto case 9;

                                    if (__slot.index_max_1(_imported_projects_uid_len) == 0)
                                    {
                                        if (__dst.put_val(0, 1, 9)) goto case 9;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 1, 8)) return false;

                                    goto case 8;
                                case 8:

                                    if ((__v = __dst.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __v)
                                        {
                                            __slot.index1 = __v += __i = __slot.index1;
                                            for (; __i < __v; __i++)
                                                __dst.put((ulong)_imported_projects_uid(__dst, __slot, __i));
                                        }
                                        __dst.retry_at(8);
                                        return false;
                                    }
                                    __i += __v = __slot.index1;
                                    for (; __v < __i; __v++) __dst.put((ulong)_imported_projects_uid(__dst, __slot, __v));
                                    goto case 9;
                                case 9:
                                    #endregion
                                    #region fields

                                    if (__dst.is_null(1 << 4))
                                        goto case 11;

                                    if (__slot.index_max_1(_fields_len) == 0)
                                    {
                                        if (__dst.put_val(0, 2, 11)) goto case 11;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 2, 10)) return false;

                                    goto case 10;
                                case 10:

                                    for (var b = true; b;)
                                        if (!__dst.put_bytes(_fields(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ? 10U : 11U))
                                            return false;

                                    goto case 11;
                                case 11:
                                    #endregion
                                    #region constant_fields

                                    if (__dst.is_null(1 << 5))
                                        goto case 13;

                                    if (__slot.index_max_1(_constant_fields_len) == 0)
                                    {
                                        if (__dst.put_val(0, 2, 13)) goto case 13;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 2, 12)) return false;

                                    goto case 12;
                                case 12:

                                    for (var b = true; b;)
                                        if (!__dst.put_bytes(_constant_fields(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ? 12U : 13U))
                                            return false;

                                    goto case 13;
                                case 13:
                                    #endregion
                                    #region packs

                                    if (__dst.is_null(1 << 6))
                                        goto case 15;

                                    if (__slot.index_max_1(_packs_len) == 0)
                                    {
                                        if (__dst.put_val(0, 2, 15)) goto case 15;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 2, 14)) return false;

                                    goto case 14;
                                case 14:

                                    for (var b = true; b;)
                                        if (!__dst.put_bytes(_packs(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ? 14U : 15U))
                                            return false;

                                    goto case 15;
                                case 15:
                                    #endregion
                                    #region hosts

                                    if (__dst.is_null(1 << 7))
                                        goto case 17;

                                    if (__slot.index_max_1(_hosts_len) == 0)
                                    {
                                        if (__dst.put_val(0, 1, 17)) goto case 17;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 1, 16)) return false;

                                    goto case 16;
                                case 16:

                                    for (var b = true; b;)
                                        if (!__dst.put_bytes(_hosts(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ? 16U : 17U))
                                            return false;

                                    goto case 17;
                                case 17:
                                    #endregion

                                    if (!__dst.init_fields_nulls(_channels() != null ? 1 : 0, 17))
                                        return false;
                                    if (_parent != 0xFFFF) __dst.set_fields_nulls(1 << 1);
                                    if (_constants() != null) __dst.set_fields_nulls(1 << 2);
                                    if (_name != null) __dst.set_fields_nulls(1 << 3);
                                    if (_doc != null) __dst.set_fields_nulls(1 << 4);
                                    if (_inline_doc != null) __dst.set_fields_nulls(1 << 5);

                                    __dst.flush_fields_nulls();
                                    goto case 18;
                                case 18:
                                    #region channels

                                    if (__dst.is_null(1))
                                        goto case 20;

                                    if (__slot.index_max_1(_channels_len) == 0)
                                    {
                                        if (__dst.put_val(0, 1, 20)) goto case 20;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 1, 19)) return false;

                                    goto case 19;
                                case 19:

                                    for (var b = true; b;)
                                        if (!__dst.put_bytes(_channels(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ? 19U : 20U))
                                            return false;

                                    goto case 20;
                                case 20:
                                    #endregion
                                    #region parent

                                    if (__dst.is_null(1 << 1))
                                        goto case 21;
                                    if (__dst.put((ushort)_parent, 21)) goto case 21;
                                    return false;
                                case 21:
                                    #endregion
                                    #region constants

                                    if (__dst.is_null(1 << 2))
                                        goto case 23;

                                    if (__slot.index_max_1(_constants_len) == 0)
                                    {
                                        if (__dst.put_val(0, 2, 23)) goto case 23;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 2, 22)) return false;

                                    goto case 22;
                                case 22:

                                    if ((__v = __dst.remaining / 4) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __v)
                                        {
                                            __slot.index1 = __v += __i = __slot.index1;
                                            for (; __i < __v; __i++)
                                                __dst.put((int)_constants(__dst, __slot, __i));
                                        }
                                        __dst.retry_at(22);
                                        return false;
                                    }
                                    __i += __v = __slot.index1;
                                    for (; __v < __i; __v++) __dst.put((int)_constants(__dst, __slot, __v));
                                    goto case 23;
                                case 23:
                                    #endregion
                                    #region name

                                    if (__dst.is_null(1 << 3))
                                        goto case 24;
                                    if (__dst.put(_name!, 24)) goto case 24;
                                    return false;
                                case 24:
                                    #endregion
                                    #region doc

                                    if (__dst.is_null(1 << 4))
                                        goto case 25;
                                    if (__dst.put(_doc!, 25)) goto case 25;
                                    return false;
                                case 25:
                                    #endregion
                                    #region inline_doc

                                    if (__dst.is_null(1 << 5))
                                        goto case 26;
                                    if (__dst.put(_inline_doc!, 26)) goto case 26;
                                    return false;
                                case 26:
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
