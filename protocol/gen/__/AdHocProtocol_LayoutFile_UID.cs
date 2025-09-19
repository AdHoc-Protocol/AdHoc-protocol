
using System;
using org.unirail;
using org.unirail.collections;
namespace org.unirail
{
    namespace Agent
    {
        public static partial class AdHocProtocol
        {
            public partial class LayoutFile_
            {
                ///<summary>
                ///Maps the persistent UIDs of protocol entities (hosts, packs, etc.) to their layout keys.
                ///This ensures that diagram positions are preserved across sessions, even if volatile internal IDs change.
                ///</summary>
                public partial class UID : IEquatable<UID>, AdHoc.Channel.Receiver.BytesDst, AdHoc.Channel.Transmitter.BytesSrc
                {

                    public int __id => __id_;
                    public const int __id_ = 0;
                    #region hosts
                    public ulong[]? hosts_new(int size)
                    { //preallocate space
                        return _hosts = new ulong[size];
                    }

                    public ulong[]? _hosts;

                    public int hosts_len => _hosts!.Length;
                    public void hosts(ulong[]? __src)
                    {

                        if (__src == null)
                        {
                            _hosts = null;
                            return;
                        }

                        _hosts = AdHoc.Resize<ulong>(__src, Math.Min(__src!.Length, Agent.AdHocProtocol.LayoutFile_.UID.hosts_.ARRAY_LEN_MAX), 0);
                        ;
                    }

                    public partial struct hosts_
                    { //Maps host UIDs to their layout positions.

                        public const int ARRAY_LEN_MAX = 255;
                    }
                    #endregion
                    #region packs
                    public ulong[]? packs_new(int size)
                    { //preallocate space
                        return _packs = new ulong[size];
                    }

                    public ulong[]? _packs;

                    public int packs_len => _packs!.Length;
                    public void packs(ulong[]? __src)
                    {

                        if (__src == null)
                        {
                            _packs = null;
                            return;
                        }

                        _packs = AdHoc.Resize<ulong>(__src, Math.Min(__src!.Length, Agent.AdHocProtocol.LayoutFile_.UID.packs_.ARRAY_LEN_MAX), 0);
                        ;
                    }

                    public partial struct packs_
                    { //Maps pack UIDs to their layout positions.

                        public const int ARRAY_LEN_MAX = 65535;
                    }
                    #endregion
                    #region branches
                    public ulong[]? branches_new(int size)
                    { //preallocate space
                        return _branches = new ulong[size];
                    }

                    public ulong[]? _branches;

                    public int branches_len => _branches!.Length;
                    public void branches(ulong[]? __src)
                    {

                        if (__src == null)
                        {
                            _branches = null;
                            return;
                        }

                        _branches = AdHoc.Resize<ulong>(__src, Math.Min(__src!.Length, Agent.AdHocProtocol.LayoutFile_.UID.branches_.ARRAY_LEN_MAX), 0);
                        ;
                    }

                    public partial struct branches_
                    { //Maps branch UIDs to their layout positions.

                        public const int ARRAY_LEN_MAX = 4095;
                    }
                    #endregion

                    public int GetHashCode
                    {
                        get
                        {
                            var _hash = 3001003L;
                            #region hosts
                            if (_hosts != null)

                                for (int __j = 0, MAX = hosts_len; __j < MAX; __j++)
                                    _hash = HashCode.Combine(_hash, _hosts[__j]);
                            #endregion
                            #region packs
                            if (_packs != null)

                                for (int __j = 0, MAX = packs_len; __j < MAX; __j++)
                                    _hash = HashCode.Combine(_hash, _packs[__j]);
                            #endregion
                            #region branches
                            if (_branches != null)

                                for (int __j = 0, MAX = branches_len; __j < MAX; __j++)
                                    _hash = HashCode.Combine(_hash, _branches[__j]);
                            #endregion

                            return (int)_hash;
                        }
                    }
                    bool IEquatable<UID>.Equals(UID? _pack)
                    {
                        if (_pack == null)
                            return false;
                        bool __t;
                        #region hosts

                        if (_hosts != _pack._hosts)
                            if (_hosts == null || _pack._hosts == null || _hosts!.Length != _pack._hosts!.Length)
                                return false;
                            else
                                for (int __j = 0, MAX = hosts_len; __j < MAX; __j++)
                                    if (_hosts[__j] != _pack._hosts[__j])
                                        return false;
                        #endregion
                        #region packs

                        if (_packs != _pack._packs)
                            if (_packs == null || _pack._packs == null || _packs!.Length != _pack._packs!.Length)
                                return false;
                            else
                                for (int __j = 0, MAX = packs_len; __j < MAX; __j++)
                                    if (_packs[__j] != _pack._packs[__j])
                                        return false;
                        #endregion
                        #region branches

                        if (_branches != _pack._branches)
                            if (_branches == null || _pack._branches == null || _branches!.Length != _pack._branches!.Length)
                                return false;
                            else
                                for (int __j = 0, MAX = branches_len; __j < MAX; __j++)
                                    if (_branches[__j] != _pack._branches[__j])
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

                                    if (!__dst.init_fields_nulls(_hosts != null ? 1 : 0, 1))
                                        return false;
                                    if (_packs != null) __dst.set_fields_nulls(1 << 1);
                                    if (_branches != null)
                                        __dst.set_fields_nulls(1 << 2);

                                    __dst.flush_fields_nulls();
                                    goto case 2;
                                case 2:
                                    #region hosts

                                    if (__dst.is_null(1))
                                        goto case 4;

                                    if (__slot.index_max_1(_hosts!.Length) == 0)
                                    {
                                        if (__dst.put_val(0, 1, 4)) goto case 4;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 1, 3)) return false;

                                    goto case 3;
                                case 3:

                                    if ((__v = __dst.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __v)
                                        {
                                            __slot.index1 = __v += __i = __slot.index1;
                                            for (; __i < __v; __i++)
                                                __dst.put((ulong)_hosts![__i]);
                                        }
                                        __dst.retry_at(3);
                                        return false;
                                    }
                                    __i += __v = __slot.index1;
                                    for (; __v < __i; __v++) __dst.put((ulong)_hosts![__v]);
                                    goto case 4;
                                case 4:
                                    #endregion
                                    #region packs

                                    if (__dst.is_null(1 << 1))
                                        goto case 6;

                                    if (__slot.index_max_1(_packs!.Length) == 0)
                                    {
                                        if (__dst.put_val(0, 2, 6)) goto case 6;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 2, 5)) return false;

                                    goto case 5;
                                case 5:

                                    if ((__v = __dst.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __v)
                                        {
                                            __slot.index1 = __v += __i = __slot.index1;
                                            for (; __i < __v; __i++)
                                                __dst.put((ulong)_packs![__i]);
                                        }
                                        __dst.retry_at(5);
                                        return false;
                                    }
                                    __i += __v = __slot.index1;
                                    for (; __v < __i; __v++) __dst.put((ulong)_packs![__v]);
                                    goto case 6;
                                case 6:
                                    #endregion
                                    #region branches

                                    if (__dst.is_null(1 << 2))
                                        goto case 8;

                                    if (__slot.index_max_1(_branches!.Length) == 0)
                                    {
                                        if (__dst.put_val(0, 2, 8)) goto case 8;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 2, 7)) return false;

                                    goto case 7;
                                case 7:

                                    if ((__v = __dst.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __v)
                                        {
                                            __slot.index1 = __v += __i = __slot.index1;
                                            for (; __i < __v; __i++)
                                                __dst.put((ulong)_branches![__i]);
                                        }
                                        __dst.retry_at(7);
                                        return false;
                                    }
                                    __i += __v = __slot.index1;
                                    for (; __v < __i; __v++) __dst.put((ulong)_branches![__v]);
                                    goto case 8;
                                case 8:
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
                                    #region hosts

                                    if (__src.is_null(1))
                                        goto case 5;

                                    if (__slot.get_len1(Agent.AdHocProtocol.LayoutFile_.UID.hosts_.ARRAY_LEN_MAX, 1, 2)) goto case 2;
                                    return false;
                                case 2:

                                    hosts_new(__slot.index_max1);
                                    if (__slot.index_max1 < 1) goto case 5;
                                    __slot._index1 = -1;
                                    goto case 3;
                                case 3:

                                    if ((__t = __src.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __t)
                                        {
                                            __slot.index1 = (__t += __i = __slot.index1);
                                            for (; __i < __t; __i++)
                                            {

                                                _hosts![__i] = (ulong)__src.get_ulong();
                                            }
                                        }
                                        __src.retry_get8(8, 4);
                                        return false;
                                    }
                                    __i += __t = __slot.index1;
                                    for (; __t < __i; __t++)
                                    {

                                        _hosts![__t] = (ulong)__src.get_ulong();
                                    }

                                    goto case 5; //leap
                                case 4:

                                    {

                                        _hosts![__slot.index1] = (ulong)__src.get_ulong_();
                                    }

                                    if (__slot.next_index1())
                                        goto case 3;
                                    goto case 5;
                                case 5:
                                    #endregion
                                    #region packs

                                    if (__src.is_null(1 << 1))
                                        goto case 9;

                                    if (__slot.get_len1(Agent.AdHocProtocol.LayoutFile_.UID.packs_.ARRAY_LEN_MAX, 2, 6)) goto case 6;
                                    return false;
                                case 6:

                                    packs_new(__slot.index_max1);
                                    if (__slot.index_max1 < 1) goto case 9;
                                    __slot._index1 = -1;
                                    goto case 7;
                                case 7:

                                    if ((__t = __src.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __t)
                                        {
                                            __slot.index1 = (__t += __i = __slot.index1);
                                            for (; __i < __t; __i++)
                                            {

                                                _packs![__i] = (ulong)__src.get_ulong();
                                            }
                                        }
                                        __src.retry_get8(8, 8);
                                        return false;
                                    }
                                    __i += __t = __slot.index1;
                                    for (; __t < __i; __t++)
                                    {

                                        _packs![__t] = (ulong)__src.get_ulong();
                                    }

                                    goto case 9; //leap
                                case 8:

                                    {

                                        _packs![__slot.index1] = (ulong)__src.get_ulong_();
                                    }

                                    if (__slot.next_index1())
                                        goto case 7;
                                    goto case 9;
                                case 9:
                                    #endregion
                                    #region branches

                                    if (__src.is_null(1 << 2))
                                        goto case 13;

                                    if (__slot.get_len1(Agent.AdHocProtocol.LayoutFile_.UID.branches_.ARRAY_LEN_MAX, 2, 10)) goto case 10;
                                    return false;
                                case 10:

                                    branches_new(__slot.index_max1);
                                    if (__slot.index_max1 < 1) goto case 13;
                                    __slot._index1 = -1;
                                    goto case 11;
                                case 11:

                                    if ((__t = __src.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __t)
                                        {
                                            __slot.index1 = (__t += __i = __slot.index1);
                                            for (; __i < __t; __i++)
                                            {

                                                _branches![__i] = (ulong)__src.get_ulong();
                                            }
                                        }
                                        __src.retry_get8(8, 12);
                                        return false;
                                    }
                                    __i += __t = __slot.index1;
                                    for (; __t < __i; __t++)
                                    {

                                        _branches![__t] = (ulong)__src.get_ulong();
                                    }

                                    goto case 13; //leap
                                case 12:

                                    {

                                        _branches![__slot.index1] = (ulong)__src.get_ulong_();
                                    }

                                    if (__slot.next_index1())
                                        goto case 11;
                                    goto case 13;
                                case 13:
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
