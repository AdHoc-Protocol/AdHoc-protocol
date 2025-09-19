
using System;
using org.unirail;
using org.unirail.collections;
namespace org.unirail
{
    namespace Agent
    {
        public static partial class AdHocProtocol
        {
            public partial class Server_
            {
                ///<summary>
                ///A generic informational or error message pack sent from the Server to the Agent.
                ///</summary>
                public partial class Info : IEquatable<Info>, AdHoc.Channel.Receiver.BytesDst
                {

                    public int __id => __id_;
                    public const int __id_ = 4;
                    #region task

                    public string? task { get; set; } = null;

                    public partial struct task_
                    {

                        public const int STR_LEN_MAX = 255;
                    }
                    #endregion
                    #region info

                    public string? info { get; set; } = null;

                    public partial struct info_
                    {

                        public const int STR_LEN_MAX = 65000;
                    }
                    #endregion

                    public int GetHashCode
                    {
                        get
                        {
                            var _hash = 3001003L;
                            #region task
                            if (task != null)
                                _hash = HashCode.Combine(_hash, task);
                            #endregion
                            #region info
                            if (info != null)
                                _hash = HashCode.Combine(_hash, info);
                            #endregion

                            return (int)_hash;
                        }
                    }
                    bool IEquatable<Info>.Equals(Info? _pack)
                    {
                        if (_pack == null)
                            return false;
                        bool __t;
                        #region task
                        if (task == null)
                        {
                            if (_pack.task != null)
                                return false;
                        }
                        else if (_pack.task == null || !task!.Equals(_pack.task))
                            return false;
                        #endregion
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
                                    #region task

                                    if (__src.is_null(1))
                                        goto case 3;

                                    if (__src.try_get_string(Agent.AdHocProtocol.Server_.Info.task_.STR_LEN_MAX, 2)) goto case 2;
                                    return false;
                                case 2:
                                    task = __src.get_string();
                                    goto case 3;
                                case 3:
                                    #endregion
                                    #region info

                                    if (__src.is_null(1 << 1))
                                        goto case 5;

                                    if (__src.try_get_string(Agent.AdHocProtocol.Server_.Info.info_.STR_LEN_MAX, 4)) goto case 4;
                                    return false;
                                case 4:
                                    info = __src.get_string();
                                    goto case 5;
                                case 5:
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
