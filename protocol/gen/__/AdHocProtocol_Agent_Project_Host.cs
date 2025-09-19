
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
                    ///Describes a single Host within the user's project.
                    ///</summary>
                    public partial interface Host : AdHoc.Channel.Transmitter.BytesSrc
                    {

                        int AdHoc.Channel.Transmitter.BytesSrc.__id => __id_;
                        public const int __id_ = -4;
                        #region uid

                        public byte _uid { get; }
                        #endregion
                        #region langs

                        public Agent.AdHocProtocol.Agent_.Project.Host.Langs _langs { get; }
                        #endregion
                        #region contexts

                        public ushort _contexts { get; }
                        public partial struct _contexts_
                        {

                            public const ushort NULL = (ushort)0x0;
                            public const ushort MIN = 0x1;
                        }
                        #endregion
                        #region pack_impl_hash_equal

                        //Get a reference to the field data for existence and equality checks
                        public object? _pack_impl_hash_equal();

                        //Get the number of items in the Map at the specific location of the multidimensional field
                        public int _pack_impl_hash_equal_len { get; }

                        //Prepare and initialize before enumerating items in the Map at a specific location of the multidimensional field
                        public void _pack_impl_hash_equal_Init(Base.Transmitter ctx, Base.Transmitter.Slot __slot);

                        ///Value:  16 Least Significant Bits - hash_equal info
                        ///        16 Most  Significant Bits - impl info<summary>Maps a pack index to its language-specific implementation and hash equality settings.</summary>
                        public ushort _pack_impl_hash_equal_NextItem_Key(Base.Transmitter ctx, Base.Transmitter.Slot __slot); //Pack -> impl_hash_equal

                        //Get the value of the item's Value in the Map
                        public uint _pack_impl_hash_equal_Val(Base.Transmitter ctx, Base.Transmitter.Slot __slot);

                        public interface _pack_impl_hash_equal_
                        { //Pack -> impl_hash_equal

                            public const int TYPE_LEN_MAX = 255;
                        }
                        #endregion
                        #region default_impl_hash_equal

                        public uint _default_impl_hash_equal { get; }
                        #endregion
                        #region field_impl

                        //Get a reference to the field data for existence and equality checks
                        public object? _field_impl();

                        //Get the number of items in the Map at the specific location of the multidimensional field
                        public int _field_impl_len { get; }

                        //Prepare and initialize before enumerating items in the Map at a specific location of the multidimensional field
                        public void _field_impl_Init(Base.Transmitter ctx, Base.Transmitter.Slot __slot);

                        ///<summary>Maps a field index to its language-specific implementation settings.</summary>
                        public ushort _field_impl_NextItem_Key(Base.Transmitter ctx, Base.Transmitter.Slot __slot);

                        //Get the value of the item's Value in the Map
                        public Agent.AdHocProtocol.Agent_.Project.Host.Langs _field_impl_Val(Base.Transmitter ctx, Base.Transmitter.Slot __slot);

                        public interface _field_impl_
                        {

                            public const int TYPE_LEN_MAX = 255;
                        }
                        #endregion
                        #region packs

                        //Get a reference to the field data for existence and equality checks
                        public object? _packs();

                        //Get the length of all item's fixed-length collections of the multidimensional field
                        public int _packs_len { get; }

                        //Get the element of the collection
                        public ushort _packs(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item);
                        public partial struct _packs_
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

                                        if (!__dst.Allocate(7, 1))
                                            return false;
                                        __dst.put((byte)_uid);
                                        __dst.put((ushort)_langs);
                                        __dst.put((uint)_default_impl_hash_equal);

                                        goto case 2;
                                    case 2:

                                        if (!__dst.init_fields_nulls(_contexts != 0x0 ? 1 : 0, 2))
                                            return false;
                                        if (_pack_impl_hash_equal() != null) __dst.set_fields_nulls(1 << 1);
                                        if (_field_impl() != null)
                                            __dst.set_fields_nulls(1 << 2);
                                        if (_packs() != null)
                                            __dst.set_fields_nulls(1 << 3);
                                        if (_parent != 0xFFFF)
                                            __dst.set_fields_nulls(1 << 4);
                                        if (_constants() != null)
                                            __dst.set_fields_nulls(1 << 5);
                                        if (_name != null)
                                            __dst.set_fields_nulls(1 << 6);
                                        if (_doc != null)
                                            __dst.set_fields_nulls(1 << 7);

                                        __dst.flush_fields_nulls();
                                        goto case 3;
                                    case 3:
                                        #region contexts

                                        if (__dst.is_null(1))
                                            goto case 4;
                                        if (__dst.put((ushort)_contexts, 4)) goto case 4;
                                        return false;
                                    case 4:
                                        #endregion
                                        #region pack_impl_hash_equal

                                        if (__dst.is_null(1 << 1))
                                            goto case 9;

                                        if (!__dst.Allocate(6, 4)) return false;
                                        if (__slot.no_items(_pack_impl_hash_equal_len, 255)) goto case 9;
                                        #region sending map info

                                        __slot.put_info();
                                        goto case 5;
                                    case 5:
                                        #endregion

                                        _pack_impl_hash_equal_Init(__dst, __slot);
                                        goto case 6;
                                    #region sending key
                                    case 6:
                                        if (__dst.put((ushort)_pack_impl_hash_equal_NextItem_Key(__dst, __slot), 7))
                                            goto case 7;
                                        return false;
                                    case 7:
                                        #endregion
                                        #region sending value
                                        if (__dst.put((uint)_pack_impl_hash_equal_Val(__dst, __slot), 8))
                                            goto case 8;
                                        return false;
                                    case 8:
                                        #endregion
                                        if (__slot.next_index1())
                                            goto case 6;

                                        goto case 9;
                                    case 9:
                                        #endregion
                                        #region field_impl

                                        if (__dst.is_null(1 << 2))
                                            goto case 14;

                                        if (!__dst.Allocate(6, 9)) return false;
                                        if (__slot.no_items(_field_impl_len, 255)) goto case 14;
                                        #region sending map info

                                        __slot.put_info();
                                        goto case 10;
                                    case 10:
                                        #endregion

                                        _field_impl_Init(__dst, __slot);
                                        goto case 11;
                                    #region sending key
                                    case 11:
                                        if (__dst.put((ushort)_field_impl_NextItem_Key(__dst, __slot), 12))
                                            goto case 12;
                                        return false;
                                    case 12:
                                        #endregion
                                        #region sending value
                                        if (__dst.put((ushort)_field_impl_Val(__dst, __slot), 13))
                                            goto case 13;
                                        return false;
                                    case 13:
                                        #endregion
                                        if (__slot.next_index1())
                                            goto case 11;

                                        goto case 14;
                                    case 14:
                                        #endregion
                                        #region packs

                                        if (__dst.is_null(1 << 3))
                                            goto case 16;

                                        if (__slot.index_max_1(_packs_len) == 0)
                                        {
                                            if (__dst.put_val(0, 2, 16)) goto case 16;
                                            return false;
                                        }
                                        if (!__dst.put_val((uint)__slot.index_max1, 2, 15)) return false;

                                        goto case 15;
                                    case 15:

                                        if ((__v = __dst.remaining / 2) < (__i = __slot.index_max1 - __slot.index1))
                                        {
                                            if (0 < __v)
                                            {
                                                __slot.index1 = __v += __i = __slot.index1;
                                                for (; __i < __v; __i++)
                                                    __dst.put((ushort)_packs(__dst, __slot, __i));
                                            }
                                            __dst.retry_at(15);
                                            return false;
                                        }
                                        __i += __v = __slot.index1;
                                        for (; __v < __i; __v++) __dst.put((ushort)_packs(__dst, __slot, __v));
                                        goto case 16;
                                    case 16:
                                        #endregion
                                        #region parent

                                        if (__dst.is_null(1 << 4))
                                            goto case 17;
                                        if (__dst.put((ushort)_parent, 17)) goto case 17;
                                        return false;
                                    case 17:
                                        #endregion
                                        #region constants

                                        if (__dst.is_null(1 << 5))
                                            goto case 19;

                                        if (__slot.index_max_1(_constants_len) == 0)
                                        {
                                            if (__dst.put_val(0, 2, 19)) goto case 19;
                                            return false;
                                        }
                                        if (!__dst.put_val((uint)__slot.index_max1, 2, 18)) return false;

                                        goto case 18;
                                    case 18:

                                        if ((__v = __dst.remaining / 4) < (__i = __slot.index_max1 - __slot.index1))
                                        {
                                            if (0 < __v)
                                            {
                                                __slot.index1 = __v += __i = __slot.index1;
                                                for (; __i < __v; __i++)
                                                    __dst.put((int)_constants(__dst, __slot, __i));
                                            }
                                            __dst.retry_at(18);
                                            return false;
                                        }
                                        __i += __v = __slot.index1;
                                        for (; __v < __i; __v++) __dst.put((int)_constants(__dst, __slot, __v));
                                        goto case 19;
                                    case 19:
                                        #endregion
                                        #region name

                                        if (__dst.is_null(1 << 6))
                                            goto case 20;
                                        if (__dst.put(_name!, 20)) goto case 20;
                                        return false;
                                    case 20:
                                        #endregion
                                        #region doc

                                        if (__dst.is_null(1 << 7))
                                            goto case 21;
                                        if (__dst.put(_doc!, 21)) goto case 21;
                                        return false;
                                    case 21:
                                        #endregion

                                        if (!__dst.init_fields_nulls(_inline_doc != null ? 1 : 0, 21))
                                            return false;

                                        __dst.flush_fields_nulls();
                                        goto case 22;
                                    case 22:
                                        #region inline_doc

                                        if (__dst.is_null(1))
                                            goto case 23;
                                        if (__dst.put(_inline_doc!, 23)) goto case 23;
                                        return false;
                                    case 23:
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
