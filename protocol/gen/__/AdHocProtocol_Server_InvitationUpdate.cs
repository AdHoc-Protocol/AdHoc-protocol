
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
                ///Sent by the Server after a successful login to provide the Agent with a new, temporary (volatile) UUID for the session.
                ///The 128-bit UUID is split into two `ulong` fields. Its volatile nature prevents reuse and supports automated
                ///CI/CD workflows, as the new UUID is automatically stored in the `AdHocAgent.toml` config file.
                ///</summary>
                public partial class InvitationUpdate : IEquatable<InvitationUpdate>, AdHoc.Channel.Receiver.BytesDst
                {

                    public int __id => __id_;
                    public const int __id_ = 6;
                    #region uuid_hi

                    public ulong uuid_hi { get; set; } = 0;
                    #endregion
                    #region uuid_lo

                    public ulong uuid_lo { get; set; } = 0;
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
                    bool IEquatable<InvitationUpdate>.Equals(InvitationUpdate? _pack)
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

                    bool AdHoc.Channel.Receiver.BytesDst.__put_bytes(AdHoc.Channel.Receiver __src)
                    {
                        var __slot = __src.slot!;
                        int __i = 0, __t = 0, __v = 0;
                        ulong __value = 0;
                        for (; ; )
                            switch (__slot.state)
                            {
                                case 0:

                                    if (!__src.has_8bytes(1))
                                        return false;
                                    uuid_hi = (ulong)__src.get_ulong();
                                    goto case 2; //leap
                                case 1:
                                    uuid_hi = (ulong)__src.get_ulong_();
                                    goto case 2;
                                case 2:

                                    if (!__src.has_8bytes(3))
                                        return false;
                                    uuid_lo = (ulong)__src.get_ulong();
                                    goto case 4; //leap
                                case 3:
                                    uuid_lo = (ulong)__src.get_ulong_();
                                    goto case 4;
                                case 4:

                                    return true;
                                default: return true;
                            }
                    }
                }
            }
        }
    }
}
