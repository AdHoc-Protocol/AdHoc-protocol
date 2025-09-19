
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
                ///A pack used to send a `.proto` file (or files) to the Server for conversion into the AdHoc format.
                ///</summary>
                public partial class Proto : IEquatable<Proto>, AdHoc.Channel.Transmitter.BytesSrc
                {

                    public int __id => __id_;
                    public const int __id_ = 9;
                    #region task

                    public string? task { get; set; } = null; //A unique ID for this conversion task.

                    public partial struct task_
                    { //A unique ID for this conversion task.

                        public const int STR_LEN_MAX = 255;
                    }
                    #endregion
                    #region name

                    public string? name { get; set; } = null;

                    public partial struct name_
                    {

                        public const int STR_LEN_MAX = 255;
                    }
                    #endregion
                    #region proto
                    public byte[]? proto_new(int size)
                    { //preallocate space
                        return _proto = new byte[size];
                    }

                    public byte[]? _proto;

                    public int proto_len => _proto!.Length;
                    public void proto(byte[]? __src)
                    {

                        if (__src == null)
                        {
                            _proto = null;
                            return;
                        }

                        _proto = AdHoc.Resize<byte>(__src, Math.Min(__src!.Length, Agent.AdHocProtocol.Agent_.Proto.proto_.ARRAY_LEN_MAX), 0);
                        ;
                    }

                    public partial struct proto_
                    {

                        public const int ARRAY_LEN_MAX = 512000;
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
                            #region name
                            if (name != null)
                                _hash = HashCode.Combine(_hash, name);
                            #endregion
                            #region proto
                            if (_proto != null)

                                for (int __j = 0, MAX = proto_len; __j < MAX; __j++)
                                    _hash = HashCode.Combine(_hash, _proto[__j]);
                            #endregion

                            return (int)_hash;
                        }
                    }
                    bool IEquatable<Proto>.Equals(Proto? _pack)
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
                        #region name
                        if (name == null)
                        {
                            if (_pack.name != null)
                                return false;
                        }
                        else if (_pack.name == null || !name!.Equals(_pack.name))
                            return false;
                        #endregion
                        #region proto

                        if (_proto != _pack._proto)
                            if (_proto == null || _pack._proto == null || _proto!.Length != _pack._proto!.Length)
                                return false;
                            else
                                for (int __j = 0, MAX = proto_len; __j < MAX; __j++)
                                    if (_proto[__j] != _pack._proto[__j])
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

                                    if (!__dst.init_fields_nulls(task != null ? 1 : 0, 1))
                                        return false;
                                    if (name != null) __dst.set_fields_nulls(1 << 1);
                                    if (_proto != null)
                                        __dst.set_fields_nulls(1 << 2);

                                    __dst.flush_fields_nulls();
                                    goto case 2;
                                case 2:
                                    #region task

                                    if (__dst.is_null(1))
                                        goto case 3;
                                    if (__dst.put(task!, 3)) goto case 3;
                                    return false;
                                case 3:
                                    #endregion
                                    #region name

                                    if (__dst.is_null(1 << 1))
                                        goto case 4;
                                    if (__dst.put(name!, 4)) goto case 4;
                                    return false;
                                case 4:
                                    #endregion
                                    #region proto

                                    if (__dst.is_null(1 << 2))
                                        goto case 6;

                                    if (__slot.index_max_1(_proto!.Length) == 0)
                                    {
                                        if (__dst.put_val(0, 3, 6)) goto case 6;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 3, 5)) return false;

                                    goto case 5;
                                case 5:

                                    if ((__v = __dst.remaining) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __v)
                                        {
                                            __slot.index1 = __v += __i = __slot.index1;
                                            for (; __i < __v; __i++)
                                                __dst.put((byte)_proto![__i]);
                                        }
                                        __dst.retry_at(5);
                                        return false;
                                    }
                                    __i += __v = __slot.index1;
                                    for (; __v < __i; __v++) __dst.put((byte)_proto![__v]);
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
