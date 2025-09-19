
using System;
using org.unirail;
using org.unirail.collections;
namespace org.unirail
{
    namespace Agent
    {
        public static partial class AdHocProtocol
        {
            public partial class Observer_
            {
                ///<summary>
                ///A request from the Observer to check if its data is stale. The Agent will respond either
                ///with an updated `Project` pack or with this same pack to confirm it's already up-to-date.
                ///</summary>
                public partial class Up_to_date : IEquatable<Up_to_date>, AdHoc.Channel.Receiver.BytesDst, AdHoc.Channel.Transmitter.BytesSrc
                {

                    public int __id => __id_;
                    public const int __id_ = 12;
                    #region info

                    public string? info { get; set; } = null; //Can be used to return an error description if an update check fails.

                    public partial struct info_
                    { //Can be used to return an error description if an update check fails.

                        public const int STR_LEN_MAX = 65000;
                    }
                    #endregion

                    public int GetHashCode
                    {
                        get
                        {
                            var _hash = 3001003L;
                            #region info
                            if (info != null)
                                _hash = HashCode.Combine(_hash, info);
                            #endregion

                            return (int)_hash;
                        }
                    }
                    bool IEquatable<Up_to_date>.Equals(Up_to_date? _pack)
                    {
                        if (_pack == null)
                            return false;
                        bool __t;
                        #region info
                        if (info == null)
                        {
                            if (_pack.info != null)
                                return false;
                        }
                        else if (_pack.info == null || !info!.Equals(_pack.info))
                            return false;
                        #endregion

                        return true;
                    }

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

                                    if (!__dst.init_fields_nulls(info != null ? 1 : 0, 1))
                                        return false;

                                    __dst.flush_fields_nulls();
                                    goto case 2;
                                case 2:
                                    #region info

                                    if (__dst.is_null(1))
                                        goto case 3;
                                    if (__dst.put(info!, 3)) goto case 3;
                                    return false;
                                case 3:
                                #endregion

                                default:
                                    return true;
                            }
                    }

                    bool AdHoc.Channel.Receiver.BytesDst.__put_bytes(AdHoc.Channel.Receiver __src)
                    {
                        var __slot = __src.slot!;
                        int __i = 0, __t = 0, __v = 0;
                        ulong __value = 0;
                        for (; ; )
                            switch (__slot.state)
                            {
                                case 0:

                                    if (__src.get_fields_nulls(0))
                                        goto case 1;
                                    return false;
                                case 1:
                                    #region info

                                    if (__src.is_null(1))
                                        goto case 3;

                                    if (__src.try_get_string(Agent.AdHocProtocol.Observer_.Up_to_date.info_.STR_LEN_MAX, 2)) goto case 2;
                                    return false;
                                case 2:
                                    info = __src.get_string();
                                    goto case 3;
                                case 3:
                                    #endregion

                                    return true;
                                default: return true;
                            }
                    }
                }
            }
        }
    }
}
