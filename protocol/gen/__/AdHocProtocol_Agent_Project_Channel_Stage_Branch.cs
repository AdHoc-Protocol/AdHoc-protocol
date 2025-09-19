
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
                    public partial interface Channel
                    {
                        public partial interface Stage
                        {
                            ///<summary>
                            ///Describes a single transition (Branch) from a Stage, which is triggered by sending a specific pack.
                            ///</summary>
                            public partial interface Branch : AdHoc.Channel.Transmitter.BytesSrc
                            {

                                int AdHoc.Channel.Transmitter.BytesSrc.__id => __id_;
                                public const int __id_ = -3;
                                #region doc

                                public string? _doc { get; }
                                public partial struct _doc_
                                {

                                    public const int STR_LEN_MAX = 255;
                                }
                                #endregion
                                #region goto_stage

                                public ushort _goto_stage { get; }
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

                                    public const int ARRAY_LEN_MAX = 65535;
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
                                                __dst.put((ushort)_goto_stage);

                                                goto case 2;
                                            case 2:

                                                if (!__dst.init_fields_nulls(_doc != null ? 1 : 0, 2))
                                                    return false;
                                                if (_packs() != null) __dst.set_fields_nulls(1 << 1);

                                                __dst.flush_fields_nulls();
                                                goto case 3;
                                            case 3:
                                                #region doc

                                                if (__dst.is_null(1))
                                                    goto case 4;
                                                if (__dst.put(_doc!, 4)) goto case 4;
                                                return false;
                                            case 4:
                                                #endregion
                                                #region packs

                                                if (__dst.is_null(1 << 1))
                                                    goto case 6;

                                                if (__slot.index_max_1(_packs_len) == 0)
                                                {
                                                    if (__dst.put_val(0, 2, 6)) goto case 6;
                                                    return false;
                                                }
                                                if (!__dst.put_val((uint)__slot.index_max1, 2, 5)) return false;

                                                goto case 5;
                                            case 5:

                                                if ((__v = __dst.remaining / 2) < (__i = __slot.index_max1 - __slot.index1))
                                                {
                                                    if (0 < __v)
                                                    {
                                                        __slot.index1 = __v += __i = __slot.index1;
                                                        for (; __i < __v; __i++)
                                                            __dst.put((ushort)_packs(__dst, __slot, __i));
                                                    }
                                                    __dst.retry_at(5);
                                                    return false;
                                                }
                                                __i += __v = __slot.index1;
                                                for (; __v < __i; __v++) __dst.put((ushort)_packs(__dst, __slot, __v));
                                                goto case 6;
                                            case 6:
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
