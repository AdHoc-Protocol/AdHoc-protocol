
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
                ///Contains the final result of a code generation task, sent from the Server to the Agent.
                ///</summary>
                public partial class Result : IEquatable<Result>, AdHoc.Channel.Receiver.BytesDst
                {

                    public int __id => __id_;
                    public const int __id_ = 10;
                    #region task

                    public string? task { get; set; } = null;

                    public partial struct task_
                    {

                        public const int STR_LEN_MAX = 255;
                    }
                    #endregion
                    #region result
                    public byte[]? result_new(int size)
                    { //preallocate space
                        return _result = new byte[size];
                    }

                    public byte[]? _result;

                    public int result_len => _result!.Length;
                    public void result(byte[]? __src)
                    {

                        if (__src == null)
                        {
                            _result = null;
                            return;
                        }

                        _result = AdHoc.Resize<byte>(__src, Math.Min(__src!.Length, Agent.AdHocProtocol.Server_.Result.result_.ARRAY_LEN_MAX), 0);
                        ;
                    }

                    public partial struct result_
                    {

                        public const int ARRAY_LEN_MAX = 3000000;
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
                            #region result
                            if (_result != null)

                                for (int __j = 0, MAX = result_len; __j < MAX; __j++)
                                    _hash = HashCode.Combine(_hash, _result[__j]);
                            #endregion
                            #region info
                            if (info != null)
                                _hash = HashCode.Combine(_hash, info);
                            #endregion

                            return (int)_hash;
                        }
                    }
                    bool IEquatable<Result>.Equals(Result? _pack)
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
                        #region result

                        if (_result != _pack._result)
                            if (_result == null || _pack._result == null || _result!.Length != _pack._result!.Length)
                                return false;
                            else
                                for (int __j = 0, MAX = result_len; __j < MAX; __j++)
                                    if (_result[__j] != _pack._result[__j])
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

                                    if (__src.try_get_string(Agent.AdHocProtocol.Server_.Result.task_.STR_LEN_MAX, 2)) goto case 2;
                                    return false;
                                case 2:
                                    task = __src.get_string();
                                    goto case 3;
                                case 3:
                                    #endregion
                                    #region result

                                    if (__src.is_null(1 << 1))
                                        goto case 6;

                                    if (__slot.get_len1(Agent.AdHocProtocol.Server_.Result.result_.ARRAY_LEN_MAX, 3, 4)) goto case 4;
                                    return false;
                                case 4:

                                    result_new(__slot.index_max1);
                                    if (__slot.index_max1 < 1) goto case 6;
                                    __slot._index1 = -1;
                                    goto case 5;
                                case 5:

                                    if ((__t = __src.remaining) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __t)
                                        {
                                            __slot.index1 = (__t += __i = __slot.index1);
                                            for (; __i < __t; __i++)
                                            {

                                                _result![__i] = (byte)__src.get_byte();
                                            }
                                        }
                                        __src.retry_at(5);
                                        return false;
                                    }
                                    __i += __t = __slot.index1;
                                    for (; __t < __i; __t++)
                                    {

                                        _result![__t] = (byte)__src.get_byte();
                                    }

                                    goto case 6;
                                case 6:
                                    #endregion
                                    #region info

                                    if (__src.is_null(1 << 2))
                                        goto case 8;

                                    if (__src.try_get_string(Agent.AdHocProtocol.Server_.Result.info_.STR_LEN_MAX, 7)) goto case 7;
                                    return false;
                                case 7:
                                    info = __src.get_string();
                                    goto case 8;
                                case 8:
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
