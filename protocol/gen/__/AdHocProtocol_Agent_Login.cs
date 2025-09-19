
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
                ///Contains the user's credentials (a permanent UUID) used for authentication with the Server.
                ///</summary>
                public partial class Login : IEquatable<Login>, AdHoc.Channel.Transmitter.BytesSrc
                {

                    public int __id => __id_;
                    public const int __id_ = 5;
                    #region uuid_hi

                    public ulong uuid_hi { get; set; } = 0; //Higher 64 bits of the 128-bit UUID.
                    #endregion
                    #region uuid_lo

                    public ulong uuid_lo { get; set; } = 0; //Lower 64 bits of the 128-bit UUID.
                    #endregion

                    public int GetHashCode
                    {
                        get
                        {
                            var _hash = 3001003L;
                            #region uuid_hi
                            _hash = HashCode.Combine(_hash, uuid_hi);
                            #endregion
                            #region uuid_lo
                            _hash = HashCode.Combine(_hash, uuid_lo);
                            #endregion

                            return (int)_hash;
                        }
                    }
                    bool IEquatable<Login>.Equals(Login? _pack)
                    {
                        if (_pack == null)
                            return false;
                        bool __t;
                        #region uuid_hi
                        if (uuid_hi != _pack.uuid_hi)
                            return false;
                        #endregion
                        #region uuid_lo
                        if (uuid_lo != _pack.uuid_lo)
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

                                    if (!__dst.Allocate(16, 1))
                                        return false;
                                    __dst.put((ulong)uuid_hi);
                                    __dst.put((ulong)uuid_lo);

                                    goto case 2;
                                case 2:

                                default:
                                    return true;
                            }
                    }
                }
            }
        }
    }
}
