
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
                ///Contains the actual layout information, such as coordinates, zoom levels, and splitter positions
                ///for the various diagrams displayed in the Observer.
                ///</summary>
                public partial class Info : IEquatable<Info>, AdHoc.Channel.Receiver.BytesDst, AdHoc.Channel.Transmitter.BytesSrc
                {

                    public int __id => __id_;
                    public const int __id_ = 1;
                    #region host_packs
                    protected AdHocProtocol.LayoutFile_.Info.View host_packs_new_item(AdHoc.Channel.Receiver scope) => _Allocator.DEFAULT.new_AdHocProtocol_LayoutFile_Info_View(scope);

                    public Agent.AdHocProtocol.LayoutFile_.Info.View? host_packs { get; set; } = null; //View settings (zoom, pan) for the host-packs diagram.
                    #endregion
                    #region pack_fields
                    protected AdHocProtocol.LayoutFile_.Info.View pack_fields_new_item(AdHoc.Channel.Receiver scope) => _Allocator.DEFAULT.new_AdHocProtocol_LayoutFile_Info_View(scope);

                    public Agent.AdHocProtocol.LayoutFile_.Info.View? pack_fields { get; set; } = null; //View settings for the pack-fields diagram.
                    #endregion
                    #region channels
                    protected AdHocProtocol.LayoutFile_.Info.View channels_new_item(AdHoc.Channel.Receiver scope) => _Allocator.DEFAULT.new_AdHocProtocol_LayoutFile_Info_View(scope);

                    public Agent.AdHocProtocol.LayoutFile_.Info.View? channels { get; set; } = null;
                    #endregion
                    #region hosts
                    public Agent.AdHocProtocol.LayoutFile_.Info.XY[]? hosts_new(int size)
                    { //preallocate space
                        return _hosts = new Agent.AdHocProtocol.LayoutFile_.Info.XY[size];
                    }

                    public Agent.AdHocProtocol.LayoutFile_.Info.XY[]? _hosts;

                    public int hosts_len => _hosts!.Length;
                    public void hosts(Agent.AdHocProtocol.LayoutFile_.Info.XY[]? __src)
                    {

                        if (__src == null)
                        {
                            _hosts = null;
                            return;
                        }

                        _hosts = AdHoc.Resize<Agent.AdHocProtocol.LayoutFile_.Info.XY>(__src, Math.Min(__src!.Length, Agent.AdHocProtocol.LayoutFile_.Info.hosts_.ARRAY_LEN_MAX), new Agent.AdHocProtocol.LayoutFile_.Info.XY());
                        ;
                    }

                    public partial struct hosts_
                    { //Stores positions for hosts in the Hosts Diagram.

                        public const int ARRAY_LEN_MAX = 255;
                    }
                    #endregion
                    #region packs
                    public Agent.AdHocProtocol.LayoutFile_.Info.XY[]? packs_new(int size)
                    { //preallocate space
                        return _packs = new Agent.AdHocProtocol.LayoutFile_.Info.XY[size];
                    }

                    public Agent.AdHocProtocol.LayoutFile_.Info.XY[]? _packs;

                    public int packs_len => _packs!.Length;
                    public void packs(Agent.AdHocProtocol.LayoutFile_.Info.XY[]? __src)
                    {

                        if (__src == null)
                        {
                            _packs = null;
                            return;
                        }

                        _packs = AdHoc.Resize<Agent.AdHocProtocol.LayoutFile_.Info.XY>(__src, Math.Min(__src!.Length, Agent.AdHocProtocol.LayoutFile_.Info.packs_.ARRAY_LEN_MAX), new Agent.AdHocProtocol.LayoutFile_.Info.XY());
                        ;
                    }

                    public partial struct packs_
                    { //Stores positions for packs in the Packs Diagram.

                        public const int ARRAY_LEN_MAX = 65535;
                    }
                    #endregion
                    #region branches
                    public Agent.AdHocProtocol.LayoutFile_.Info.XY[]? branches_new(int size)
                    { //preallocate space
                        return _branches = new Agent.AdHocProtocol.LayoutFile_.Info.XY[size];
                    }

                    public Agent.AdHocProtocol.LayoutFile_.Info.XY[]? _branches;

                    public int branches_len => _branches!.Length;
                    public void branches(Agent.AdHocProtocol.LayoutFile_.Info.XY[]? __src)
                    {

                        if (__src == null)
                        {
                            _branches = null;
                            return;
                        }

                        _branches = AdHoc.Resize<Agent.AdHocProtocol.LayoutFile_.Info.XY>(__src, Math.Min(__src!.Length, Agent.AdHocProtocol.LayoutFile_.Info.branches_.ARRAY_LEN_MAX), new Agent.AdHocProtocol.LayoutFile_.Info.XY());
                        ;
                    }

                    public partial struct branches_
                    { //Stores positions for branches in the Channels Diagram.

                        public const int ARRAY_LEN_MAX = 4095;
                    }
                    #endregion

                    public int GetHashCode
                    {
                        get
                        {
                            var _hash = 3001003L;
                            #region host_packs
                            if (host_packs != null)
                                _hash = HashCode.Combine(_hash, host_packs);
                            #endregion
                            #region pack_fields
                            if (pack_fields != null)
                                _hash = HashCode.Combine(_hash, pack_fields);
                            #endregion
                            #region channels
                            if (channels != null)
                                _hash = HashCode.Combine(_hash, channels);
                            #endregion
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
                    bool IEquatable<Info>.Equals(Info? _pack)
                    {
                        if (_pack == null)
                            return false;
                        bool __t;
                        #region host_packs
                        if (host_packs == null)
                        {
                            if (_pack.host_packs != null)
                                return false;
                        }
                        else if (_pack.host_packs == null || !host_packs!.Equals(_pack.host_packs))
                            return false;
                        #endregion
                        #region pack_fields
                        if (pack_fields == null)
                        {
                            if (_pack.pack_fields != null)
                                return false;
                        }
                        else if (_pack.pack_fields == null || !pack_fields!.Equals(_pack.pack_fields))
                            return false;
                        #endregion
                        #region channels
                        if (channels == null)
                        {
                            if (_pack.channels != null)
                                return false;
                        }
                        else if (_pack.channels == null || !channels!.Equals(_pack.channels))
                            return false;
                        #endregion
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

                                    if (!__dst.init_fields_nulls(host_packs != null ? 1 : 0, 1))
                                        return false;
                                    if (pack_fields != null) __dst.set_fields_nulls(1 << 1);
                                    if (channels != null)
                                        __dst.set_fields_nulls(1 << 2);
                                    if (_hosts != null)
                                        __dst.set_fields_nulls(1 << 3);
                                    if (_packs != null)
                                        __dst.set_fields_nulls(1 << 4);
                                    if (_branches != null)
                                        __dst.set_fields_nulls(1 << 5);

                                    __dst.flush_fields_nulls();
                                    goto case 2;
                                case 2:
                                    #region host_packs

                                    if (__dst.is_null(1))
                                        goto case 3;
                                    if (__dst.put_bytes(host_packs!, 3)) goto case 3;
                                    return false;
                                case 3:
                                    #endregion
                                    #region pack_fields

                                    if (__dst.is_null(1 << 1))
                                        goto case 4;
                                    if (__dst.put_bytes(pack_fields!, 4)) goto case 4;
                                    return false;
                                case 4:
                                    #endregion
                                    #region channels

                                    if (__dst.is_null(1 << 2))
                                        goto case 5;
                                    if (__dst.put_bytes(channels!, 5)) goto case 5;
                                    return false;
                                case 5:
                                    #endregion
                                    #region hosts

                                    if (__dst.is_null(1 << 3))
                                        goto case 7;

                                    if (__slot.index_max_1(_hosts!.Length) == 0)
                                    {
                                        if (__dst.put_val(0, 1, 7)) goto case 7;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 1, 6)) return false;

                                    goto case 6;
                                case 6:

                                    if ((__v = __dst.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __v)
                                        {
                                            __slot.index1 = __v += __i = __slot.index1;
                                            for (; __i < __v; __i++)
                                                __dst.put((ulong)_hosts![__i]);
                                        }
                                        __dst.retry_at(6);
                                        return false;
                                    }
                                    __i += __v = __slot.index1;
                                    for (; __v < __i; __v++) __dst.put((ulong)_hosts![__v]);
                                    goto case 7;
                                case 7:
                                    #endregion
                                    #region packs

                                    if (__dst.is_null(1 << 4))
                                        goto case 9;

                                    if (__slot.index_max_1(_packs!.Length) == 0)
                                    {
                                        if (__dst.put_val(0, 2, 9)) goto case 9;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 2, 8)) return false;

                                    goto case 8;
                                case 8:

                                    if ((__v = __dst.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __v)
                                        {
                                            __slot.index1 = __v += __i = __slot.index1;
                                            for (; __i < __v; __i++)
                                                __dst.put((ulong)_packs![__i]);
                                        }
                                        __dst.retry_at(8);
                                        return false;
                                    }
                                    __i += __v = __slot.index1;
                                    for (; __v < __i; __v++) __dst.put((ulong)_packs![__v]);
                                    goto case 9;
                                case 9:
                                    #endregion
                                    #region branches

                                    if (__dst.is_null(1 << 5))
                                        goto case 11;

                                    if (__slot.index_max_1(_branches!.Length) == 0)
                                    {
                                        if (__dst.put_val(0, 2, 11)) goto case 11;
                                        return false;
                                    }
                                    if (!__dst.put_val((uint)__slot.index_max1, 2, 10)) return false;

                                    goto case 10;
                                case 10:

                                    if ((__v = __dst.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __v)
                                        {
                                            __slot.index1 = __v += __i = __slot.index1;
                                            for (; __i < __v; __i++)
                                                __dst.put((ulong)_branches![__i]);
                                        }
                                        __dst.retry_at(10);
                                        return false;
                                    }
                                    __i += __v = __slot.index1;
                                    for (; __v < __i; __v++) __dst.put((ulong)_branches![__v]);
                                    goto case 11;
                                case 11:
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
                                    #region host_packs

                                    if (__src.is_null(1))
                                        goto case 3;

                                    {
                                        var val = __src.try_get_bytes(host_packs_new_item(__src), 2);
                                        if (val == null) return false;
                                        host_packs = val;
                                    }
                                    goto case 3; //leap
                                case 2:
                                    host_packs = __slot.get_bytes<Agent.AdHocProtocol.LayoutFile_.Info.View>();
                                    goto case 3;
                                case 3:
                                    #endregion
                                    #region pack_fields

                                    if (__src.is_null(1 << 1))
                                        goto case 5;

                                    {
                                        var val = __src.try_get_bytes(pack_fields_new_item(__src), 4);
                                        if (val == null) return false;
                                        pack_fields = val;
                                    }
                                    goto case 5; //leap
                                case 4:
                                    pack_fields = __slot.get_bytes<Agent.AdHocProtocol.LayoutFile_.Info.View>();
                                    goto case 5;
                                case 5:
                                    #endregion
                                    #region channels

                                    if (__src.is_null(1 << 2))
                                        goto case 7;

                                    {
                                        var val = __src.try_get_bytes(channels_new_item(__src), 6);
                                        if (val == null) return false;
                                        channels = val;
                                    }
                                    goto case 7; //leap
                                case 6:
                                    channels = __slot.get_bytes<Agent.AdHocProtocol.LayoutFile_.Info.View>();
                                    goto case 7;
                                case 7:
                                    #endregion
                                    #region hosts

                                    if (__src.is_null(1 << 3))
                                        goto case 11;

                                    if (__slot.get_len1(Agent.AdHocProtocol.LayoutFile_.Info.hosts_.ARRAY_LEN_MAX, 1, 8)) goto case 8;
                                    return false;
                                case 8:

                                    hosts_new(__slot.index_max1);
                                    if (__slot.index_max1 < 1) goto case 11;
                                    __slot._index1 = -1;
                                    goto case 9;
                                case 9:

                                    if ((__t = __src.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __t)
                                        {
                                            __slot.index1 = (__t += __i = __slot.index1);
                                            for (; __i < __t; __i++)
                                            {

                                                _hosts![__i] = new Agent.AdHocProtocol.LayoutFile_.Info.XY((ulong)__src.get_ulong());
                                            }
                                        }
                                        __src.retry_get8(8, 10);
                                        return false;
                                    }
                                    __i += __t = __slot.index1;
                                    for (; __t < __i; __t++)
                                    {

                                        _hosts![__t] = new Agent.AdHocProtocol.LayoutFile_.Info.XY((ulong)__src.get_ulong());
                                    }

                                    goto case 11; //leap
                                case 10:

                                    {

                                        _hosts![__slot.index1] = new Agent.AdHocProtocol.LayoutFile_.Info.XY((ulong)__src.get_ulong_());
                                    }

                                    if (__slot.next_index1())
                                        goto case 9;
                                    goto case 11;
                                case 11:
                                    #endregion
                                    #region packs

                                    if (__src.is_null(1 << 4))
                                        goto case 15;

                                    if (__slot.get_len1(Agent.AdHocProtocol.LayoutFile_.Info.packs_.ARRAY_LEN_MAX, 2, 12)) goto case 12;
                                    return false;
                                case 12:

                                    packs_new(__slot.index_max1);
                                    if (__slot.index_max1 < 1) goto case 15;
                                    __slot._index1 = -1;
                                    goto case 13;
                                case 13:

                                    if ((__t = __src.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __t)
                                        {
                                            __slot.index1 = (__t += __i = __slot.index1);
                                            for (; __i < __t; __i++)
                                            {

                                                _packs![__i] = new Agent.AdHocProtocol.LayoutFile_.Info.XY((ulong)__src.get_ulong());
                                            }
                                        }
                                        __src.retry_get8(8, 14);
                                        return false;
                                    }
                                    __i += __t = __slot.index1;
                                    for (; __t < __i; __t++)
                                    {

                                        _packs![__t] = new Agent.AdHocProtocol.LayoutFile_.Info.XY((ulong)__src.get_ulong());
                                    }

                                    goto case 15; //leap
                                case 14:

                                    {

                                        _packs![__slot.index1] = new Agent.AdHocProtocol.LayoutFile_.Info.XY((ulong)__src.get_ulong_());
                                    }

                                    if (__slot.next_index1())
                                        goto case 13;
                                    goto case 15;
                                case 15:
                                    #endregion
                                    #region branches

                                    if (__src.is_null(1 << 5))
                                        goto case 19;

                                    if (__slot.get_len1(Agent.AdHocProtocol.LayoutFile_.Info.branches_.ARRAY_LEN_MAX, 2, 16)) goto case 16;
                                    return false;
                                case 16:

                                    branches_new(__slot.index_max1);
                                    if (__slot.index_max1 < 1) goto case 19;
                                    __slot._index1 = -1;
                                    goto case 17;
                                case 17:

                                    if ((__t = __src.remaining / 8) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __t)
                                        {
                                            __slot.index1 = (__t += __i = __slot.index1);
                                            for (; __i < __t; __i++)
                                            {

                                                _branches![__i] = new Agent.AdHocProtocol.LayoutFile_.Info.XY((ulong)__src.get_ulong());
                                            }
                                        }
                                        __src.retry_get8(8, 18);
                                        return false;
                                    }
                                    __i += __t = __slot.index1;
                                    for (; __t < __i; __t++)
                                    {

                                        _branches![__t] = new Agent.AdHocProtocol.LayoutFile_.Info.XY((ulong)__src.get_ulong());
                                    }

                                    goto case 19; //leap
                                case 18:

                                    {

                                        _branches![__slot.index1] = new Agent.AdHocProtocol.LayoutFile_.Info.XY((ulong)__src.get_ulong_());
                                    }

                                    if (__slot.next_index1())
                                        goto case 17;
                                    goto case 19;
                                case 19:
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
