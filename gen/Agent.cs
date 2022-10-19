
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using org.unirail;
using System.Linq;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using org.unirail.collections;
using System.Runtime.CompilerServices;

using org.unirail.Agent;
/*
AdHoc agent protocol description

*/


namespace  org.unirail
{
    /*
    <see cref = 'InCS'/> following packs generate in C# with implementation
    <see cref = 'Server.ToAgent.Info'/>
    <see cref = 'Server.ToAgent.Result'/>
    <see cref = 'Server.ToAgent.LoginRejected'/>
    <see cref = 'Server.ToAgent.Busy'/>
    <see cref = 'ToServer.RequestResult'/>
    <see cref = 'ToServer.Login'/>
    <see cref = 'ToServer.Proto'/>
    <see cref = 'Observer.ToAgent.Up_to_date'/>
    <see cref = 'Observer.ToAgent.Show_Code'/>
    <see cref = 'InCS'/>-- by default rest of the packs in C#  generate abstracted (without implementation)

    */
    namespace Agent
    {

        public interface Layout :   ObserverCommunication.Receivable, ObserverCommunication.Transmittable
        {


            public const int _id_ = 9;
            void ObserverCommunication.Receivable.Received(ObserverCommunication via) => ObserverCommunication.onReceiveListener.Received(via, this);

            void ObserverCommunication.Transmittable.Sent(ObserverCommunication via) => ObserverCommunication.onTransmitListener.Sent(via, this);



#region split

            public byte _split { get;  set;  }

#endregion

#region host_packs
            protected Layout.View _host_packs_new_item(AdHoc.Receiver scope) => (Layout.View)scope.int_dst!.Receiving(scope, Layout.View._id_)!;

            public Layout.View? _host_packs { get;  set;  }

#endregion

#region pack_fields
            protected Layout.View _pack_fields_new_item(AdHoc.Receiver scope) => (Layout.View)scope.int_dst!.Receiving(scope, Layout.View._id_)!;

            public Layout.View? _pack_fields { get;  set;  }

#endregion





            AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
            {
                int _i = 0, _t = 0, _v = 0;
                for(;;)
                    switch(_dst.state)
                    {
                        case 0:
                        if(_dst.put_val(_id_, 2, 1)) goto case 1;
                            return null;
                        case 1:
                            if(! _dst.allocate(1, 1)) return null;
                            _dst.put(_split);
                        goto case 2;
                        case 2:
                            if(!_dst.init_fields_nulls(_host_packs != null ? 1 : 0, 2)) return null;
                            if(_pack_fields != null) _dst.set_fields_nulls(1 << 1);
                            _dst.flush_fields_nulls();
                        goto case 3;
#region host_packs
                        case 3:
                        if(_dst.is_null(1)) goto case  4 ;
                            _dst.state = 4;
                            return _host_packs;
#endregion
#region pack_fields
                        case 4:
                        if(_dst.is_null(1  << 1)) goto case  5 ;
                            _dst.state = 5;
                            return _pack_fields;
#endregion
                        case 5:
                        default:
                            return null;
                    }
            }

            AdHoc.INT.BytesDst? AdHoc.INT.BytesDst.put_bytes(AdHoc.Receiver _src)
            {
                int _i = 0, _t = 0, _v = 0;
                for(;;)
                    switch(_src.state)
                    {
                        case 0:
                            if(! _src.try_get4(1, 1)) return null;
                        goto case 1;
                        case 1:
                            _split = _src.get4<byte>() ;;
                        goto case 2;
                        case 2:
                        if(_src.get_fields_nulls(2)) goto case 3;
                            return null;
#region host_packs
                        case 3:
                        if(_src.is_null(1)) goto case 5;
                            _src.state = 4;
                            return   _host_packs_new_item(_src) ;
                        case 4:
                            _host_packs = (Layout.View)_src.output;
                        goto case 5 ;
#endregion
#region pack_fields
                        case 5:
                        if(_src.is_null(1  << 1)) goto case 7;
                            _src.state = 6;
                            return   _pack_fields_new_item(_src) ;
                        case 6:
                            _pack_fields = (Layout.View)_src.output;
                        goto case 7 ;
#endregion
                        case 7:
                        default:
                            return null;
                    }
            }



            public interface View :   AdHoc.INT.BytesDst, AdHoc.INT.BytesSrc
            {


                public const int _id_ = -6;




#region X

                public int _X { get;  set;  }

#endregion

#region Y

                public int _Y { get;  set;  }

#endregion

#region zoom

                public double _zoom { get;  set;  }

#endregion

#region id2info

                public void  _id2info_new(Context.Provider ctx, int item_len);

                public object? _id2info();

                public int _id2info_Count();

                public void _id2info(Context.Provider ctx, uint key, ulong value);

                public void _id2info_Init(Context.Provider ctx);

                public uint _id2info_Next_K(Context.Provider ctx);

                public ulong  _id2info_V(Context.Provider ctx);

#endregion





                AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_dst.state)
                        {
                            case 0:
                                throw new NotSupportedException();
                            case 1:
                                if(! _dst.allocate(16, 1)) return null;
                                _dst.put(_X);
                                _dst.put(_Y);
                                _dst.put(_zoom);
                            goto case 2;
                            case 2:
                                if(!_dst.init_fields_nulls(_id2info() != null ? 1 : 0, 2)) return null;
                                _dst.flush_fields_nulls();
                            goto case 3;
#region id2info
                            case 3:
                            if(_dst.is_null(1)) goto case  8 ;
                                if(!_dst.allocate(5, 3)) return null;
                            if(_dst.zero_items(_id2info_Count())) goto case 8 ;
#region sending map info
                                if(_dst.put_map_info(false, false, 0, 4, 4, 8)) continue;
                            goto case 4;
#endregion
#region sending key
                            case  4:
                                _id2info_Init(_dst);
                            goto case 5;
                            case 5:
                                if(!_dst.put(_id2info_Next_K(_dst), 6)) return null;
                            goto case 6;
#endregion
#region sending value
                            case 6:
                                if(!_dst.put(_id2info_V(_dst), 7)) return null;
                            goto case 7;
                            case 7 :
                            if(_dst.next_index()) goto case 5;
                            goto case 8;
#endregion
#endregion
                            case 8:
                            default:
                                return null;
                        }
                }

                AdHoc.INT.BytesDst? AdHoc.INT.BytesDst.put_bytes(AdHoc.Receiver _src)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_src.state)
                        {
                            case 0:
                                if(! _src.try_get4(4, 1)) return null;
                            goto case 1;
                            case 1:
                                _X = _src.get4<int>() ;;
                            goto case 2;
                            case 2:
                                if(! _src.try_get4(4, 3)) return null;
                            goto case 3;
                            case 3:
                                _Y = _src.get4<int>() ;;
                            goto case 4;
                            case 4:
                                if(! _src.try_get8(8, 5)) return null;
                            goto case 5;
                            case 5:
                                _zoom = _src.get8<double>() ;;
                            goto case 6;
                            case 6:
                            if(_src.get_fields_nulls(6)) goto case 7;
                                return null;
#region id2info
                            case 7:
                            if(_src.is_null(1)) goto case 13;
                                if(!_src.get_info(7) || !_src.get_items_count(8)) return null;
                            goto case 8 ;
                            case 8 :
                                _id2info_new(_src, _src.items_count);
                            if(_src.items_count < 1) goto case 13;
                            if(_src.index_max == 0) goto case 13;
                            goto case 9 ;
#region receiving key
                            case 9:
                                if(! _src.try_get4(4, 10)) return null;
                            goto case 10;
                            case 10 :
                                _src.key_set(_src.get4< uint >());
                            goto case 11;
#endregion
#region receiving value
                            case 11:
                                if(! _src.try_get8(8, 12)) return null;
                            goto case 12;
                            case 12 :
                                _id2info(_src, _src.key_get< uint >(), _src.get8< ulong >());
#endregion
                            if(_src.next_index()) goto case 9;
                            goto case 13 ;
#endregion
                            case 13:
                            default:
                                return null;
                        }
                }




                public interface Info
                {
                    public const int _id_ = -7;
                    public const ulong EMPTY_PACK = 0UL;
                    public  interface x
                    {

                        public static int get(ulong pack)

                        => (int)(int)(pack  & ((1UL << 32) - 1)) ;


                        public static ulong set(int src, ulong pack)
                        => (ulong)(pack & (ulong)(~(((1UL << 32) - 1)))  | ((ulong)(uint)src));

                    }
                    public  interface y
                    {

                        public static int get(ulong pack)

                        => (int)(int)(pack  >> 32 & ((1UL << 32) - 1)) ;


                        public static ulong set(int src, ulong pack)
                        => (ulong)(pack & (ulong)(~(((1UL << 32) - 1) << 32))  | ((ulong)(uint)src  << 32));

                    }



                    public class Driver : AdHoc.INT.BytesDst, AdHoc.INT.BytesSrc
                    {

                        public static readonly Driver ONE = new Driver();




                        AdHoc.INT.BytesDst? AdHoc.INT.BytesDst.put_bytes(AdHoc.Receiver _src)
                        {
                            switch(_src.state)
                            {
                                case 0:
                                    if(!_src.try_get8(8, 1)) return null;
                                    goto default;
                                default:
                                    return null;
                            }
                        }

                        AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
                        {
                            switch(_dst.state)
                            {
                                case 0:
                                    throw new NotSupportedException();
                                case 1:
                                    if(!_dst.put_val(_dst.int_values_src!(), 8, 2)) return null;
                                    goto default;
                                default:
                                    return null;
                            }
                        }

                    }


                }

            }

        }
        public class Upload
        {

            public const int _id_ = 3;

            public class Driver : Communication.Receivable
            {
                public static readonly Driver ONE = new Driver();
                public override bool Equals(object? obj) => obj == ONE;


                void Communication.Receivable.Received(Communication via) => Communication.onReceiveListener.Received_Upload(via);
                AdHoc.INT.BytesDst? AdHoc.INT.BytesDst.put_bytes(AdHoc.Receiver _src) => null;


            }

        }
        public interface Project :   Communication.Transmittable, ObserverCommunication.Transmittable
        {


            public const int _id_ = 5;

            void Communication.Transmittable.Sent(Communication via) => Communication.onTransmitListener.Sent(via, this);
            void ObserverCommunication.Transmittable.Sent(ObserverCommunication via) => ObserverCommunication.onTransmitListener.Sent(via, this);



#region task

            public string? _task { get;    }

#endregion

#region namespace_

            public string? _namespace_ { get;    }

#endregion

#region time

            public long _time { get;    }

#endregion

#region fields

            public object? _fields();

            public int _fields_Count {get;}

            public Project.Host.Port.Pack.Field? _fields(Context.Provider ctx, int item);
            public  interface fields_
            {
                const int LEN_MAX  = 65000;

            }

#endregion

#region packs

            public object? _packs();

            public int _packs_Count {get;}

            public Project.Host.Port.Pack? _packs(Context.Provider ctx, int item);
            public  interface packs_
            {
                const int LEN_MAX  = 65000;

            }

#endregion

#region hosts

            public object? _hosts();

            public int _hosts_Count {get;}

            public Project.Host? _hosts(Context.Provider ctx, int item);
            public  interface hosts_
            {
                const int LEN_MAX  = 65000;

            }

#endregion

#region ports

            public object? _ports();

            public int _ports_Count {get;}

            public Project.Host.Port? _ports(Context.Provider ctx, int item);
            public  interface ports_
            {
                const int LEN_MAX  = 65000;

            }

#endregion

#region channels

            public object? _channels();

            public int _channels_Count {get;}

            public Project.Channel? _channels(Context.Provider ctx, int item);
            public  interface channels_
            {
                const int LEN_MAX  = 65000;

            }

#endregion

#region name

            public string? _name { get;    }

#endregion

#region doc

            public string? _doc { get;    }

#endregion

#region inline_doc

            public string? _inline_doc { get;    }

#endregion





            AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
            {
                int _i = 0, _t = 0, _v = 0;
                for(;;)
                    switch(_dst.state)
                    {
                        case 0:
                        if(_dst.put_val(_id_, 2, 1)) goto case 1;
                            return null;
                        case 1:
                            if(! _dst.allocate(8, 1)) return null;
                            _dst.put(_time);
                        goto case 2;
                        case 2:
                            if(!_dst.init_fields_nulls(_task != null ? 1 : 0, 2)) return null;
                            if(_namespace_ != null) _dst.set_fields_nulls(1 << 1);
                            if(_fields() != null) _dst.set_fields_nulls(1 << 2);
                            if(_packs() != null) _dst.set_fields_nulls(1 << 3);
                            if(_hosts() != null) _dst.set_fields_nulls(1 << 4);
                            if(_ports() != null) _dst.set_fields_nulls(1 << 5);
                            if(_channels() != null) _dst.set_fields_nulls(1 << 6);
                            if(_name != null) _dst.set_fields_nulls(1 << 7);
                            _dst.flush_fields_nulls();
                        goto case 3;
#region task
                        case 3:
                        if(_dst.is_null(1)) goto case  4 ;
                        if(_dst.put(_task, 4)) goto case 4;
                            return null;
#endregion
#region namespace_
                        case 4:
                        if(_dst.is_null(1  << 1)) goto case  5 ;
                        if(_dst.put(_namespace_, 5)) goto case 5;
                            return null;
#endregion
#region fields
                        case 5:
                        if(_dst.is_null(1  << 2)) goto case  9 ;
                            _v = _fields_Count;
                            if(! _dst.put_len(_v, 2, _v == 0 ? 9u :  6)) return null;
                        if(_v == 0) goto case 9;
                        goto case 6;
                        case 6:
                            _v =  _dst.index_max;
                            _i = _dst.index;
                            for(;  _dst.allocate(1, 6) ; _dst.put((byte) 0))
                                do
                                    if(_i == _v)
                                    {
                                        if(0 < (_i & 7)) _dst.put((byte) 0);
                                    goto case 9;
                                    }
                                    else if(_fields(_dst, _i) != null)
                                    {
                                        var max = Math.Min(_v, 8 + _i & ~7) ;
                                        _dst.index2 = max;
                                        _dst.index    = _i;
                                        _v         = 1 << (_i & 7);
                                        while(++_i < max)
                                            if(_fields(_dst, _i) != null)
                                                _v |= 1 << (_i & 7);
                                        _dst.put((byte) _v);
                                    goto case 7;
                                    }
                                while((_i++ & 7) < 7);
                            _dst.index = _i;
                            return null;
                        case 7:
                            _dst.state = 8;
                            return _fields(_dst, _dst.index) ;
                        case 8:
                            for(; _dst.next_index2() ;)
                            if(_fields(_dst, _dst.index) != null) goto case 7;
                        if(_dst.index < _dst.index_max)goto case 6;
                        goto case 9;
#endregion
#region packs
                        case 9:
                        if(_dst.is_null(1  << 3)) goto case  13 ;
                            _v = _packs_Count;
                            if(! _dst.put_len(_v, 2, _v == 0 ? 13u :  10)) return null;
                        if(_v == 0) goto case 13;
                        goto case 10;
                        case 10:
                            _v =  _dst.index_max;
                            _i = _dst.index;
                            for(;  _dst.allocate(1, 10) ; _dst.put((byte) 0))
                                do
                                    if(_i == _v)
                                    {
                                        if(0 < (_i & 7)) _dst.put((byte) 0);
                                    goto case 13;
                                    }
                                    else if(_packs(_dst, _i) != null)
                                    {
                                        var max = Math.Min(_v, 8 + _i & ~7) ;
                                        _dst.index2 = max;
                                        _dst.index    = _i;
                                        _v         = 1 << (_i & 7);
                                        while(++_i < max)
                                            if(_packs(_dst, _i) != null)
                                                _v |= 1 << (_i & 7);
                                        _dst.put((byte) _v);
                                    goto case 11;
                                    }
                                while((_i++ & 7) < 7);
                            _dst.index = _i;
                            return null;
                        case 11:
                            _dst.state = 12;
                            return _packs(_dst, _dst.index) ;
                        case 12:
                            for(; _dst.next_index2() ;)
                            if(_packs(_dst, _dst.index) != null) goto case 11;
                        if(_dst.index < _dst.index_max)goto case 10;
                        goto case 13;
#endregion
#region hosts
                        case 13:
                        if(_dst.is_null(1  << 4)) goto case  17 ;
                            _v = _hosts_Count;
                            if(! _dst.put_len(_v, 2, _v == 0 ? 17u :  14)) return null;
                        if(_v == 0) goto case 17;
                        goto case 14;
                        case 14:
                            _v =  _dst.index_max;
                            _i = _dst.index;
                            for(;  _dst.allocate(1, 14) ; _dst.put((byte) 0))
                                do
                                    if(_i == _v)
                                    {
                                        if(0 < (_i & 7)) _dst.put((byte) 0);
                                    goto case 17;
                                    }
                                    else if(_hosts(_dst, _i) != null)
                                    {
                                        var max = Math.Min(_v, 8 + _i & ~7) ;
                                        _dst.index2 = max;
                                        _dst.index    = _i;
                                        _v         = 1 << (_i & 7);
                                        while(++_i < max)
                                            if(_hosts(_dst, _i) != null)
                                                _v |= 1 << (_i & 7);
                                        _dst.put((byte) _v);
                                    goto case 15;
                                    }
                                while((_i++ & 7) < 7);
                            _dst.index = _i;
                            return null;
                        case 15:
                            _dst.state = 16;
                            return _hosts(_dst, _dst.index) ;
                        case 16:
                            for(; _dst.next_index2() ;)
                            if(_hosts(_dst, _dst.index) != null) goto case 15;
                        if(_dst.index < _dst.index_max)goto case 14;
                        goto case 17;
#endregion
#region ports
                        case 17:
                        if(_dst.is_null(1  << 5)) goto case  21 ;
                            _v = _ports_Count;
                            if(! _dst.put_len(_v, 2, _v == 0 ? 21u :  18)) return null;
                        if(_v == 0) goto case 21;
                        goto case 18;
                        case 18:
                            _v =  _dst.index_max;
                            _i = _dst.index;
                            for(;  _dst.allocate(1, 18) ; _dst.put((byte) 0))
                                do
                                    if(_i == _v)
                                    {
                                        if(0 < (_i & 7)) _dst.put((byte) 0);
                                    goto case 21;
                                    }
                                    else if(_ports(_dst, _i) != null)
                                    {
                                        var max = Math.Min(_v, 8 + _i & ~7) ;
                                        _dst.index2 = max;
                                        _dst.index    = _i;
                                        _v         = 1 << (_i & 7);
                                        while(++_i < max)
                                            if(_ports(_dst, _i) != null)
                                                _v |= 1 << (_i & 7);
                                        _dst.put((byte) _v);
                                    goto case 19;
                                    }
                                while((_i++ & 7) < 7);
                            _dst.index = _i;
                            return null;
                        case 19:
                            _dst.state = 20;
                            return _ports(_dst, _dst.index) ;
                        case 20:
                            for(; _dst.next_index2() ;)
                            if(_ports(_dst, _dst.index) != null) goto case 19;
                        if(_dst.index < _dst.index_max)goto case 18;
                        goto case 21;
#endregion
#region channels
                        case 21:
                        if(_dst.is_null(1  << 6)) goto case  25 ;
                            _v = _channels_Count;
                            if(! _dst.put_len(_v, 2, _v == 0 ? 25u :  22)) return null;
                        if(_v == 0) goto case 25;
                        goto case 22;
                        case 22:
                            _v =  _dst.index_max;
                            _i = _dst.index;
                            for(;  _dst.allocate(1, 22) ; _dst.put((byte) 0))
                                do
                                    if(_i == _v)
                                    {
                                        if(0 < (_i & 7)) _dst.put((byte) 0);
                                    goto case 25;
                                    }
                                    else if(_channels(_dst, _i) != null)
                                    {
                                        var max = Math.Min(_v, 8 + _i & ~7) ;
                                        _dst.index2 = max;
                                        _dst.index    = _i;
                                        _v         = 1 << (_i & 7);
                                        while(++_i < max)
                                            if(_channels(_dst, _i) != null)
                                                _v |= 1 << (_i & 7);
                                        _dst.put((byte) _v);
                                    goto case 23;
                                    }
                                while((_i++ & 7) < 7);
                            _dst.index = _i;
                            return null;
                        case 23:
                            _dst.state = 24;
                            return _channels(_dst, _dst.index) ;
                        case 24:
                            for(; _dst.next_index2() ;)
                            if(_channels(_dst, _dst.index) != null) goto case 23;
                        if(_dst.index < _dst.index_max)goto case 22;
                        goto case 25;
#endregion
#region name
                        case 25:
                        if(_dst.is_null(1  << 7)) goto case  26 ;
                        if(_dst.put(_name, 26)) goto case 26;
                            return null;
#endregion
                        case 26:
                            if(!_dst.init_fields_nulls(_doc != null ? 1 : 0, 26)) return null;
                            if(_inline_doc != null) _dst.set_fields_nulls(1 << 1);
                            _dst.flush_fields_nulls();
                        goto case 27;
#region doc
                        case 27:
                        if(_dst.is_null(1)) goto case  28 ;
                        if(_dst.put(_doc, 28)) goto case 28;
                            return null;
#endregion
#region inline_doc
                        case 28:
                        if(_dst.is_null(1  << 1)) goto case  29 ;
                        if(_dst.put(_inline_doc, 29)) goto case 29;
                            return null;
#endregion
                        case 29:
                        default:
                            return null;
                    }
            }




            public interface Host :   AdHoc.INT.BytesSrc
            {


                public const int _id_ = -2;




#region langs

                public Project.Host.Langs _langs { get;    }

#endregion

#region pack_impl_hash_equal

                public object? _pack_impl_hash_equal();

                public int _pack_impl_hash_equal_Count();

                public void _pack_impl_hash_equal_Init(Context.Provider ctx);

                public ushort _pack_impl_hash_equal_Next_K(Context.Provider ctx);

                public uint  _pack_impl_hash_equal_V(Context.Provider ctx);

#endregion

#region default_impl_hash_equal

                public uint _default_impl_hash_equal { get;    }

#endregion

#region field_impl

                public object? _field_impl();

                public int _field_impl_Count();

                public void _field_impl_Init(Context.Provider ctx);

                public ushort _field_impl_Next_K(Context.Provider ctx);

                public Project.Host.Langs  _field_impl_V(Context.Provider ctx);

#endregion

#region packs

                public object? _packs();

                public int _packs_Count {get;}

                public ushort _packs(Context.Provider ctx, int item);
                public  interface packs_
                {
                    const int LEN_MAX  = 65000;

                }

#endregion

#region name

                public string? _name { get;    }

#endregion

#region doc

                public string? _doc { get;    }

#endregion

#region inline_doc

                public string? _inline_doc { get;    }

#endregion





                AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_dst.state)
                        {
                            case 0:
                                throw new NotSupportedException();
                            case 1:
                                if(! _dst.allocate(6, 1)) return null;
                                _dst.put((ushort) _langs);
                                _dst.put(_default_impl_hash_equal);
                            goto case 2;
                            case 2:
                                if(!_dst.init_fields_nulls(_pack_impl_hash_equal() != null ? 1 : 0, 2)) return null;
                                if(_field_impl() != null) _dst.set_fields_nulls(1 << 1);
                                if(_packs() != null) _dst.set_fields_nulls(1 << 2);
                                if(_name != null) _dst.set_fields_nulls(1 << 3);
                                if(_doc != null) _dst.set_fields_nulls(1 << 4);
                                if(_inline_doc != null) _dst.set_fields_nulls(1 << 5);
                                _dst.flush_fields_nulls();
                            goto case 3;
#region pack_impl_hash_equal
                            case 3:
                            if(_dst.is_null(1)) goto case  8 ;
                                if(!_dst.allocate(5, 3)) return null;
                            if(_dst.zero_items(_pack_impl_hash_equal_Count())) goto case 8 ;
#region sending map info
                                if(_dst.put_map_info(false, false, 0, 4, 4, 8)) continue;
                            goto case 4;
#endregion
#region sending key
                            case  4:
                                _pack_impl_hash_equal_Init(_dst);
                            goto case 5;
                            case 5:
                                if(!_dst.put(_pack_impl_hash_equal_Next_K(_dst), 6)) return null;
                            goto case 6;
#endregion
#region sending value
                            case 6:
                                if(!_dst.put(_pack_impl_hash_equal_V(_dst), 7)) return null;
                            goto case 7;
                            case 7 :
                            if(_dst.next_index()) goto case 5;
                            goto case 8;
#endregion
#endregion
#region field_impl
                            case 8:
                            if(_dst.is_null(1  << 1)) goto case  13 ;
                                if(!_dst.allocate(5, 8)) return null;
                            if(_dst.zero_items(_field_impl_Count())) goto case 13 ;
#region sending map info
                                if(_dst.put_map_info(false, false, 0, 9, 9, 13)) continue;
                            goto case 9;
#endregion
#region sending key
                            case  9:
                                _field_impl_Init(_dst);
                            goto case 10;
                            case 10:
                                if(!_dst.put(_field_impl_Next_K(_dst), 11)) return null;
                            goto case 11;
#endregion
#region sending value
                            case 11:
                                if(!_dst.put((ushort) _field_impl_V(_dst), 12)) return null;
                            goto case 12;
                            case 12 :
                            if(_dst.next_index()) goto case 10;
                            goto case 13;
#endregion
#endregion
#region packs
                            case 13:
                            if(_dst.is_null(1  << 2)) goto case  15 ;
                                _v = _packs_Count;
                                if(! _dst.put_len(_v, 2, _v == 0 ? 15u :  14)) return null;
                            if(_v == 0) goto case 15;
                            goto case 14;
                            case 14:
                                if((_v = _dst.remaining / 2) < (_i = _dst.index_max - _dst.index))
                                {
                                    if(0 < _v)
                                    {
                                        _dst.index = _v += _i = _dst.index;
                                        for(;  _i < _v; _i++)  _dst.put(_packs(_dst, _i));
                                    }
                                    _dst.retry_at(14);
                                    return null;
                                }
                                _i += _v = _dst.index;
                                for(; _v < _i; _v++) _dst.put(_packs(_dst, _v));
                            goto case 15;
#endregion
#region name
                            case 15:
                            if(_dst.is_null(1  << 3)) goto case  16 ;
                            if(_dst.put(_name, 16)) goto case 16;
                                return null;
#endregion
#region doc
                            case 16:
                            if(_dst.is_null(1  << 4)) goto case  17 ;
                            if(_dst.put(_doc, 17)) goto case 17;
                                return null;
#endregion
#region inline_doc
                            case 17:
                            if(_dst.is_null(1  << 5)) goto case  18 ;
                            if(_dst.put(_inline_doc, 18)) goto case 18;
                                return null;
#endregion
                            case 18:
                            default:
                                return null;
                        }
                }




                [Flags]
                public enum Langs : ushort
                {
                    InCPP = 1,
                    InRS = 2,
                    InCS = 4,
                    InJAVA = 8,
                    InGO = 16,
                    InTS = 32,
                    All = 65535,
                }

                public interface Port :   AdHoc.INT.BytesSrc
                {


                    public const int _id_ = -3;




#region host

                    public ushort _host { get;    }

#endregion

#region transmitted_packs

                    public object? _transmitted_packs();

                    public int _transmitted_packs_Count {get;}

                    public ushort _transmitted_packs(Context.Provider ctx, int item);
                    public  interface transmitted_packs_
                    {
                        const int LEN_MAX  = 65000;

                    }

#endregion

#region related_packs

                    public object? _related_packs();

                    public int _related_packs_Count {get;}

                    public ushort _related_packs(Context.Provider ctx, int item);
                    public  interface related_packs_
                    {
                        const int LEN_MAX  = 65000;

                    }

#endregion

#region name

                    public string? _name { get;    }

#endregion

#region doc

                    public string? _doc { get;    }

#endregion

#region inline_doc

                    public string? _inline_doc { get;    }

#endregion





                    AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
                    {
                        int _i = 0, _t = 0, _v = 0;
                        for(;;)
                            switch(_dst.state)
                            {
                                case 0:
                                    throw new NotSupportedException();
                                case 1:
                                    if(! _dst.allocate(2, 1)) return null;
                                    _dst.put(_host);
                                goto case 2;
                                case 2:
                                    if(!_dst.init_fields_nulls(_transmitted_packs() != null ? 1 : 0, 2)) return null;
                                    if(_related_packs() != null) _dst.set_fields_nulls(1 << 1);
                                    if(_name != null) _dst.set_fields_nulls(1 << 2);
                                    if(_doc != null) _dst.set_fields_nulls(1 << 3);
                                    if(_inline_doc != null) _dst.set_fields_nulls(1 << 4);
                                    _dst.flush_fields_nulls();
                                goto case 3;
#region transmitted_packs
                                case 3:
                                if(_dst.is_null(1)) goto case  5 ;
                                    _v = _transmitted_packs_Count;
                                    if(! _dst.put_len(_v, 2, _v == 0 ? 5u :  4)) return null;
                                if(_v == 0) goto case 5;
                                goto case 4;
                                case 4:
                                    if((_v = _dst.remaining / 2) < (_i = _dst.index_max - _dst.index))
                                    {
                                        if(0 < _v)
                                        {
                                            _dst.index = _v += _i = _dst.index;
                                            for(;  _i < _v; _i++)  _dst.put(_transmitted_packs(_dst, _i));
                                        }
                                        _dst.retry_at(4);
                                        return null;
                                    }
                                    _i += _v = _dst.index;
                                    for(; _v < _i; _v++) _dst.put(_transmitted_packs(_dst, _v));
                                goto case 5;
#endregion
#region related_packs
                                case 5:
                                if(_dst.is_null(1  << 1)) goto case  7 ;
                                    _v = _related_packs_Count;
                                    if(! _dst.put_len(_v, 2, _v == 0 ? 7u :  6)) return null;
                                if(_v == 0) goto case 7;
                                goto case 6;
                                case 6:
                                    if((_v = _dst.remaining / 2) < (_i = _dst.index_max - _dst.index))
                                    {
                                        if(0 < _v)
                                        {
                                            _dst.index = _v += _i = _dst.index;
                                            for(;  _i < _v; _i++)  _dst.put(_related_packs(_dst, _i));
                                        }
                                        _dst.retry_at(6);
                                        return null;
                                    }
                                    _i += _v = _dst.index;
                                    for(; _v < _i; _v++) _dst.put(_related_packs(_dst, _v));
                                goto case 7;
#endregion
#region name
                                case 7:
                                if(_dst.is_null(1  << 2)) goto case  8 ;
                                if(_dst.put(_name, 8)) goto case 8;
                                    return null;
#endregion
#region doc
                                case 8:
                                if(_dst.is_null(1  << 3)) goto case  9 ;
                                if(_dst.put(_doc, 9)) goto case 9;
                                    return null;
#endregion
#region inline_doc
                                case 9:
                                if(_dst.is_null(1  << 4)) goto case  10 ;
                                if(_dst.put(_inline_doc, 10)) goto case 10;
                                    return null;
#endregion
                                case 10:
                                default:
                                    return null;
                            }
                    }




                    public interface Pack :   AdHoc.INT.BytesSrc
                    {


                        public const int _id_ = -4;




#region id

                        public ushort _id { get;    }

#endregion

#region parent

                        public ushort? _parent { get;    }

#endregion

#region nested_max

                        public ushort? _nested_max { get;    }

#endregion

#region referred

                        public bool? _referred { get;    }
                        public  interface referred_
                        {
                            const int NULL = 0;

                        }

#endregion

#region fields

                        public object? _fields();

                        public int _fields_Count {get;}

                        public int _fields(Context.Provider ctx, int item);
                        public  interface fields_
                        {
                            const int LEN_MAX  = 65000;

                        }

#endregion

#region static_fields

                        public object? _static_fields();

                        public int _static_fields_Count {get;}

                        public int _static_fields(Context.Provider ctx, int item);
                        public  interface static_fields_
                        {
                            const int LEN_MAX  = 65000;

                        }

#endregion

#region value_type

                        public bool _value_type { get;    }

#endregion

#region name

                        public string? _name { get;    }

#endregion

#region doc

                        public string? _doc { get;    }

#endregion

#region inline_doc

                        public string? _inline_doc { get;    }

#endregion





                        AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
                        {
                            int _i = 0, _t = 0, _v = 0;
                            for(;;)
                                switch(_dst.state)
                                {
                                    case 0:
                                        throw new NotSupportedException();
                                    case 1:
                                        if(! _dst.allocate(2, 1)) return null;
                                        _dst.put(_id);
                                    goto case 2;
                                    case 2 :
                                        if(! _dst.init_bits(1, 2)) return null;
#region referred
                                        _dst.put(_referred);
#endregion
#region value_type
                                        _dst.put(_value_type);
#endregion
                                    goto case 3;
                                    case 3 :
                                        _dst.end_bits();
                                    goto case 4;
                                    case 4:
                                        if(!_dst.init_fields_nulls(_parent != null ? 1 : 0, 4)) return null;
                                        if(_nested_max != null) _dst.set_fields_nulls(1 << 1);
                                        if(_fields() != null) _dst.set_fields_nulls(1 << 2);
                                        if(_static_fields() != null) _dst.set_fields_nulls(1 << 3);
                                        if(_name != null) _dst.set_fields_nulls(1 << 4);
                                        if(_doc != null) _dst.set_fields_nulls(1 << 5);
                                        if(_inline_doc != null) _dst.set_fields_nulls(1 << 6);
                                        _dst.flush_fields_nulls();
                                    goto case 5;
#region parent
                                    case 5:
                                    if(_dst.is_null(1)) goto case  6 ;
                                    if(_dst.put(_parent, 6)) goto case 6;
                                        return null;
#endregion
#region nested_max
                                    case 6:
                                    if(_dst.is_null(1  << 1)) goto case  7 ;
                                    if(_dst.put(_nested_max, 7)) goto case 7;
                                        return null;
#endregion
#region fields
                                    case 7:
                                    if(_dst.is_null(1  << 2)) goto case  9 ;
                                        _v = _fields_Count;
                                        if(! _dst.put_len(_v, 2, _v == 0 ? 9u :  8)) return null;
                                    if(_v == 0) goto case 9;
                                    goto case 8;
                                    case 8:
                                        if((_v = _dst.remaining / 4) < (_i = _dst.index_max - _dst.index))
                                        {
                                            if(0 < _v)
                                            {
                                                _dst.index = _v += _i = _dst.index;
                                                for(;  _i < _v; _i++)  _dst.put(_fields(_dst, _i));
                                            }
                                            _dst.retry_at(8);
                                            return null;
                                        }
                                        _i += _v = _dst.index;
                                        for(; _v < _i; _v++) _dst.put(_fields(_dst, _v));
                                    goto case 9;
#endregion
#region static_fields
                                    case 9:
                                    if(_dst.is_null(1  << 3)) goto case  11 ;
                                        _v = _static_fields_Count;
                                        if(! _dst.put_len(_v, 2, _v == 0 ? 11u :  10)) return null;
                                    if(_v == 0) goto case 11;
                                    goto case 10;
                                    case 10:
                                        if((_v = _dst.remaining / 4) < (_i = _dst.index_max - _dst.index))
                                        {
                                            if(0 < _v)
                                            {
                                                _dst.index = _v += _i = _dst.index;
                                                for(;  _i < _v; _i++)  _dst.put(_static_fields(_dst, _i));
                                            }
                                            _dst.retry_at(10);
                                            return null;
                                        }
                                        _i += _v = _dst.index;
                                        for(; _v < _i; _v++) _dst.put(_static_fields(_dst, _v));
                                    goto case 11;
#endregion
#region name
                                    case 11:
                                    if(_dst.is_null(1  << 4)) goto case  12 ;
                                    if(_dst.put(_name, 12)) goto case 12;
                                        return null;
#endregion
#region doc
                                    case 12:
                                    if(_dst.is_null(1  << 5)) goto case  13 ;
                                    if(_dst.put(_doc, 13)) goto case 13;
                                        return null;
#endregion
#region inline_doc
                                    case 13:
                                    if(_dst.is_null(1  << 6)) goto case  14 ;
                                    if(_dst.put(_inline_doc, 14)) goto case 14;
                                        return null;
#endregion
                                    case 14:
                                    default:
                                        return null;
                                }
                        }




                        public interface Field :   AdHoc.INT.BytesSrc
                        {


                            public const int _id_ = -5;




#region dims

                            public object? _dims();

                            public int _dims_Count {get;}

                            public int _dims(Context.Provider ctx, int item);
                            public  interface dims_
                            {
                                const int LEN_MAX  = 65000;

                            }

#endregion

#region Ext

                            public ushort _Ext { get;    }

#endregion

#region Int

                            public ushort? _Int { get;    }

#endregion

#region min_value

                            public long? _min_value { get;    }

#endregion

#region max_value

                            public long? _max_value { get;    }

#endregion

#region dir

                            public byte? _dirˍ { get;    }
                            public  interface dir_
                            {
                                const int NULL = 3;
                                static byte ? INT(sbyte ? EXT)
                                {
                                    return EXT == null ? 3 :
                                           (byte?)(EXT.Value - -1);
                                }
                                static sbyte ? EXT(byte ? INT)
                                {
                                    return INT == 3 ? null :
                                           (sbyte?)(INT.Value + -1);
                                }
                                const long MIN = -1L;
                                const long MAX = 1L;

                            }

#endregion

#region min_valueD

                            public double? _min_valueD { get;    }

#endregion

#region max_valueD

                            public double? _max_valueD { get;    }

#endregion

#region bits

                            public byte? _bitsˍ { get;    }
                            public  interface bits_
                            {
                                const int NULL = 7;
                                static byte ? INT(byte ? EXT)
                                {
                                    return EXT == null ? 7 :
                                           (byte?)(EXT.Value - 1);
                                }
                                static byte ? EXT(byte ? INT)
                                {
                                    return INT == 7 ? null :
                                           (byte?)(INT.Value + 1);
                                }
                                const long MIN = 1L;
                                const long MAX = 7L;

                            }

#endregion

#region null_value

                            public byte? _null_value { get;    }

#endregion

#region ExtV

                            public ushort? _ExtV { get;    }

#endregion

#region IntV

                            public ushort? _IntV { get;    }

#endregion

#region min_valueV

                            public long? _min_valueV { get;    }

#endregion

#region max_valueV

                            public long? _max_valueV { get;    }

#endregion

#region dirV

                            public byte? _dirVˍ { get;    }
                            public  interface dirV_
                            {
                                const int NULL = 3;
                                static byte ? INT(sbyte ? EXT)
                                {
                                    return EXT == null ? 3 :
                                           (byte?)(EXT.Value - -1);
                                }
                                static sbyte ? EXT(byte ? INT)
                                {
                                    return INT == 3 ? null :
                                           (sbyte?)(INT.Value + -1);
                                }
                                const long MIN = -1L;
                                const long MAX = 1L;

                            }

#endregion

#region min_valueDV

                            public double? _min_valueDV { get;    }

#endregion

#region max_valueDV

                            public double? _max_valueDV { get;    }

#endregion

#region bitsV

                            public byte? _bitsVˍ { get;    }
                            public  interface bitsV_
                            {
                                const int NULL = 7;
                                static byte ? INT(byte ? EXT)
                                {
                                    return EXT == null ? 7 :
                                           (byte?)(EXT.Value - 1);
                                }
                                static byte ? EXT(byte ? INT)
                                {
                                    return INT == 7 ? null :
                                           (byte?)(INT.Value + 1);
                                }
                                const long MIN = 1L;
                                const long MAX = 7L;

                            }

#endregion

#region null_valueV

                            public byte? _null_valueV { get;    }

#endregion

#region value_int

                            public long? _value_int { get;    }

#endregion

#region value_double

                            public double? _value_double { get;    }

#endregion

#region value_string

                            public string? _value_string { get;    }

#endregion

#region the_set

                            public bool _the_set { get;    }

#endregion

#region array

                            public object? _array();

                            public int _array_Count {get;}

                            public string? _array(Context.Provider ctx, int item);
                            public  interface array_
                            {
                                const int LEN_MAX  = 65000;

                            }

#endregion

#region name

                            public string? _name { get;    }

#endregion

#region doc

                            public string? _doc { get;    }

#endregion

#region inline_doc

                            public string? _inline_doc { get;    }

#endregion





                            AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
                            {
                                int _i = 0, _t = 0, _v = 0;
                                for(;;)
                                    switch(_dst.state)
                                    {
                                        case 0:
                                            throw new NotSupportedException();
                                        case 1:
                                            if(! _dst.allocate(2, 1)) return null;
                                            _dst.put(_Ext);
                                        goto case 2;
                                        case 2 :
                                            if(! _dst.init_bits(2, 2)) return null;
#region dir
                                            _dst.put_bits((int)(_dirˍ ?? 3), 2);
#endregion
#region bits
                                            _dst.put_bits((int)(_bitsˍ ?? 7), 3);
#endregion
#region dirV
                                            _dst.put_bits((int)(_dirVˍ ?? 3), 2);
#endregion
#region bitsV
                                            _dst.put_bits((int)(_bitsVˍ ?? 7), 3);
#endregion
#region the_set
                                            _dst.put(_the_set);
#endregion
                                        goto case 3;
                                        case 3 :
                                            _dst.end_bits();
                                        goto case 4;
                                        case 4:
                                            if(!_dst.init_fields_nulls(_dims() != null ? 1 : 0, 4)) return null;
                                            if(_Int != null) _dst.set_fields_nulls(1 << 1);
                                            if(_min_value != null) _dst.set_fields_nulls(1 << 2);
                                            if(_max_value != null) _dst.set_fields_nulls(1 << 3);
                                            if(_min_valueD != null) _dst.set_fields_nulls(1 << 4);
                                            if(_max_valueD != null) _dst.set_fields_nulls(1 << 5);
                                            if(_null_value != null) _dst.set_fields_nulls(1 << 6);
                                            if(_ExtV != null) _dst.set_fields_nulls(1 << 7);
                                            _dst.flush_fields_nulls();
                                        goto case 5;
#region dims
                                        case 5:
                                        if(_dst.is_null(1)) goto case  7 ;
                                            _v = _dims_Count;
                                            if(! _dst.put_len(_v, 2, _v == 0 ? 7u :  6)) return null;
                                        if(_v == 0) goto case 7;
                                        goto case 6;
                                        case 6:
                                            if((_v = _dst.remaining / 4) < (_i = _dst.index_max - _dst.index))
                                            {
                                                if(0 < _v)
                                                {
                                                    _dst.index = _v += _i = _dst.index;
                                                    for(;  _i < _v; _i++)  _dst.put(_dims(_dst, _i));
                                                }
                                                _dst.retry_at(6);
                                                return null;
                                            }
                                            _i += _v = _dst.index;
                                            for(; _v < _i; _v++) _dst.put(_dims(_dst, _v));
                                        goto case 7;
#endregion
#region Int
                                        case 7:
                                        if(_dst.is_null(1  << 1)) goto case  8 ;
                                        if(_dst.put(_Int, 8)) goto case 8;
                                            return null;
#endregion
#region min_value
                                        case 8:
                                        if(_dst.is_null(1  << 2)) goto case  9 ;
                                        if(_dst.put(_min_value, 9)) goto case 9;
                                            return null;
#endregion
#region max_value
                                        case 9:
                                        if(_dst.is_null(1  << 3)) goto case  10 ;
                                        if(_dst.put(_max_value, 10)) goto case 10;
                                            return null;
#endregion
#region min_valueD
                                        case 10:
                                        if(_dst.is_null(1  << 4)) goto case  11 ;
                                        if(_dst.put(_min_valueD, 11)) goto case 11;
                                            return null;
#endregion
#region max_valueD
                                        case 11:
                                        if(_dst.is_null(1  << 5)) goto case  12 ;
                                        if(_dst.put(_max_valueD, 12)) goto case 12;
                                            return null;
#endregion
#region null_value
                                        case 12:
                                        if(_dst.is_null(1  << 6)) goto case  13 ;
                                        if(_dst.put(_null_value, 13)) goto case 13;
                                            return null;
#endregion
#region ExtV
                                        case 13:
                                        if(_dst.is_null(1  << 7)) goto case  14 ;
                                        if(_dst.put(_ExtV, 14)) goto case 14;
                                            return null;
#endregion
                                        case 14:
                                            if(!_dst.init_fields_nulls(_IntV != null ? 1 : 0, 14)) return null;
                                            if(_min_valueV != null) _dst.set_fields_nulls(1 << 1);
                                            if(_max_valueV != null) _dst.set_fields_nulls(1 << 2);
                                            if(_min_valueDV != null) _dst.set_fields_nulls(1 << 3);
                                            if(_max_valueDV != null) _dst.set_fields_nulls(1 << 4);
                                            if(_null_valueV != null) _dst.set_fields_nulls(1 << 5);
                                            if(_value_int != null) _dst.set_fields_nulls(1 << 6);
                                            if(_value_double != null) _dst.set_fields_nulls(1 << 7);
                                            _dst.flush_fields_nulls();
                                        goto case 15;
#region IntV
                                        case 15:
                                        if(_dst.is_null(1)) goto case  16 ;
                                        if(_dst.put(_IntV, 16)) goto case 16;
                                            return null;
#endregion
#region min_valueV
                                        case 16:
                                        if(_dst.is_null(1  << 1)) goto case  17 ;
                                        if(_dst.put(_min_valueV, 17)) goto case 17;
                                            return null;
#endregion
#region max_valueV
                                        case 17:
                                        if(_dst.is_null(1  << 2)) goto case  18 ;
                                        if(_dst.put(_max_valueV, 18)) goto case 18;
                                            return null;
#endregion
#region min_valueDV
                                        case 18:
                                        if(_dst.is_null(1  << 3)) goto case  19 ;
                                        if(_dst.put(_min_valueDV, 19)) goto case 19;
                                            return null;
#endregion
#region max_valueDV
                                        case 19:
                                        if(_dst.is_null(1  << 4)) goto case  20 ;
                                        if(_dst.put(_max_valueDV, 20)) goto case 20;
                                            return null;
#endregion
#region null_valueV
                                        case 20:
                                        if(_dst.is_null(1  << 5)) goto case  21 ;
                                        if(_dst.put(_null_valueV, 21)) goto case 21;
                                            return null;
#endregion
#region value_int
                                        case 21:
                                        if(_dst.is_null(1  << 6)) goto case  22 ;
                                        if(_dst.put(_value_int, 22)) goto case 22;
                                            return null;
#endregion
#region value_double
                                        case 22:
                                        if(_dst.is_null(1  << 7)) goto case  23 ;
                                        if(_dst.put(_value_double, 23)) goto case 23;
                                            return null;
#endregion
                                        case 23:
                                            if(!_dst.init_fields_nulls(_value_string != null ? 1 : 0, 23)) return null;
                                            if(_array() != null) _dst.set_fields_nulls(1 << 1);
                                            if(_name != null) _dst.set_fields_nulls(1 << 2);
                                            if(_doc != null) _dst.set_fields_nulls(1 << 3);
                                            if(_inline_doc != null) _dst.set_fields_nulls(1 << 4);
                                            _dst.flush_fields_nulls();
                                        goto case 24;
#region value_string
                                        case 24:
                                        if(_dst.is_null(1)) goto case  25 ;
                                        if(_dst.put(_value_string, 25)) goto case 25;
                                            return null;
#endregion
#region array
                                        case 25:
                                        if(_dst.is_null(1  << 1)) goto case  29 ;
                                            _v = _array_Count;
                                            if(! _dst.put_len(_v, 2, _v == 0 ? 29u :  26)) return null;
                                        if(_v == 0) goto case 29;
                                        goto case 26;
                                        case 26:
                                            _v =  _dst.index_max;
                                            _i = _dst.index;
                                            for(;  _dst.allocate(1, 26) ; _dst.put((byte) 0))
                                                do
                                                    if(_i == _v)
                                                    {
                                                        if(0 < (_i & 7)) _dst.put((byte) 0);
                                                    goto case 29;
                                                    }
                                                    else if(_array(_dst, _i) != null)
                                                    {
                                                        var max = Math.Min(_v, 8 + _i & ~7) ;
                                                        _dst.index2 = max;
                                                        _dst.index    = _i;
                                                        _v         = 1 << (_i & 7);
                                                        while(++_i < max)
                                                            if(_array(_dst, _i) != null)
                                                                _v |= 1 << (_i & 7);
                                                        _dst.put((byte) _v);
                                                    goto case 27;
                                                    }
                                                while((_i++ & 7) < 7);
                                            _dst.index = _i;
                                            return null;
                                        case 27:
                                        if(_dst.put(_array(_dst, _dst.index)!, 28)) goto case 28;
                                            return null;
                                        case 28:
                                            for(; _dst.next_index2() ;)
                                            if(_array(_dst, _dst.index) != null) goto case 27;
                                        if(_dst.index < _dst.index_max)goto case 26;
                                        goto case 29;
#endregion
#region name
                                        case 29:
                                        if(_dst.is_null(1  << 2)) goto case  30 ;
                                        if(_dst.put(_name, 30)) goto case 30;
                                            return null;
#endregion
#region doc
                                        case 30:
                                        if(_dst.is_null(1  << 3)) goto case  31 ;
                                        if(_dst.put(_doc, 31)) goto case 31;
                                            return null;
#endregion
#region inline_doc
                                        case 31:
                                        if(_dst.is_null(1  << 4)) goto case  32 ;
                                        if(_dst.put(_inline_doc, 32)) goto case 32;
                                            return null;
#endregion
                                        case 32:
                                        default:
                                            return null;
                                    }
                            }




                            public enum DataType : ushort
                            {
                                t_constants = 65535,
                                t_enum_sw = 65534,
                                t_enum_exp = 65533,
                                t_flags = 65532,
                                t_bool = 65531,
                                t_int8 = 65530,
                                t_binary = 65529,
                                t_uint8 = 65528,
                                t_int16 = 65527,
                                t_uint16 = 65526,
                                t_char = 65525,
                                t_int32 = 65524,
                                t_uint32 = 65523,
                                t_int64 = 65522,
                                t_uint64 = 65521,
                                t_float = 65520,
                                t_double = 65519,
                                t_string = 65518,
                                t_map = 65517,
                                t_set = 65516,
                                t_subpack = 65514,
                            }

                        }

                    }

                }

            }
            public interface Channel :   AdHoc.INT.BytesSrc
            {


                public const int _id_ = -1;




#region portA

                public ushort _portA { get;    }

#endregion

#region portB

                public ushort _portB { get;    }

#endregion

#region name

                public string? _name { get;    }

#endregion

#region doc

                public string? _doc { get;    }

#endregion

#region inline_doc

                public string? _inline_doc { get;    }

#endregion





                AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_dst.state)
                        {
                            case 0:
                                throw new NotSupportedException();
                            case 1:
                                if(! _dst.allocate(4, 1)) return null;
                                _dst.put(_portA);
                                _dst.put(_portB);
                            goto case 2;
                            case 2:
                                if(!_dst.init_fields_nulls(_name != null ? 1 : 0, 2)) return null;
                                if(_doc != null) _dst.set_fields_nulls(1 << 1);
                                if(_inline_doc != null) _dst.set_fields_nulls(1 << 2);
                                _dst.flush_fields_nulls();
                            goto case 3;
#region name
                            case 3:
                            if(_dst.is_null(1)) goto case  4 ;
                            if(_dst.put(_name, 4)) goto case 4;
                                return null;
#endregion
#region doc
                            case 4:
                            if(_dst.is_null(1  << 1)) goto case  5 ;
                            if(_dst.put(_doc, 5)) goto case 5;
                                return null;
#endregion
#region inline_doc
                            case 5:
                            if(_dst.is_null(1  << 2)) goto case  6 ;
                            if(_dst.put(_inline_doc, 6)) goto case 6;
                                return null;
#endregion
                            case 6:
                            default:
                                return null;
                        }
                }




            }

        }
        namespace AgentToServer
        {
            public class Login : IEquatable< Login >,  Communication.Transmittable
            {


                public const int _id_ = 8;

                void Communication.Transmittable.Sent(Communication via) => Communication.onTransmitListener.Sent(via, this);



#region client

                public string? client { get; set; }

#endregion


                public  int GetHashCode
                {
                    get
                    {
                        var _hash = 3001003L;
#region client
                        _hash = HashCode.Combine(_hash, client);
#endregion
                        return (int) _hash;
                    }
                }
                bool IEquatable<Login>.Equals(Login? _pack)
                {
                    if(_pack == null) return false;
                    bool _t;
#region client
                    if((_t = client != null) == (_pack.client == null) || _t &&  ! client!.Equals(_pack.client)) return false;
#endregion
                    return true;
                }




                AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_dst.state)
                        {
                            case 0:
                            if(_dst.put_val(_id_, 2, 1)) goto case 1;
                                return null;
                            case 1:
                                if(!_dst.init_fields_nulls(client != null ? 1 : 0, 1)) return null;
                                _dst.flush_fields_nulls();
                            goto case 2;
#region client
                            case 2:
                            if(_dst.is_null(1)) goto case  3 ;
                            if(_dst.put(client, 3)) goto case 3;
                                return null;
#endregion
                            case 3:
                            default:
                                return null;
                        }
                }




            }
            public class RequestResult : IEquatable< RequestResult >,  Communication.Transmittable    //the request for pending task result
            {


                public const int _id_ = 6;

                void Communication.Transmittable.Sent(Communication via) => Communication.onTransmitListener.Sent(via, this);



#region task

                public string? task { get; set; }

#endregion


                public  int GetHashCode
                {
                    get
                    {
                        var _hash = 3001003L;
#region task
                        _hash = HashCode.Combine(_hash, task);
#endregion
                        return (int) _hash;
                    }
                }
                bool IEquatable<RequestResult>.Equals(RequestResult? _pack)
                {
                    if(_pack == null) return false;
                    bool _t;
#region task
                    if((_t = task != null) == (_pack.task == null) || _t &&  ! task!.Equals(_pack.task)) return false;
#endregion
                    return true;
                }




                AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_dst.state)
                        {
                            case 0:
                            if(_dst.put_val(_id_, 2, 1)) goto case 1;
                                return null;
                            case 1:
                                if(!_dst.init_fields_nulls(task != null ? 1 : 0, 1)) return null;
                                _dst.flush_fields_nulls();
                            goto case 2;
#region task
                            case 2:
                            if(_dst.is_null(1)) goto case  3 ;
                            if(_dst.put(task, 3)) goto case 3;
                                return null;
#endregion
                            case 3:
                            default:
                                return null;
                        }
                }




            }
            public class Proto : IEquatable< Proto >,  Communication.Transmittable
            {


                public const int _id_ = 7;

                void Communication.Transmittable.Sent(Communication via) => Communication.onTransmitListener.Sent(via, this);



#region task

                public string? task { get; set; }

#endregion

#region name

                public string? name { get; set; }

#endregion

#region proto
                public byte[] proto_new(int item_len)
                => proto = new byte[Math.Min(item_len, proto_.LEN_MAX)];

                public byte[] ? proto;

                public int proto_Count {get; protected set;}

                public void protoˌ(IList<byte>? _src)
                {
                    if(_src == null)
                    {
                        proto = null;
                        return;
                    }
                    var _max = Math.Min(proto_.LEN_MAX, _src.Count);
                    var _items = proto;
                    if(_items == null || _items.Length != _max) _items = proto_new(_max);
                    for(var _i = 0; _i < _max; _i++)
                        _items[_i] = _src[_i] ;
                }

                public  interface proto_
                {
                    const int LEN_MAX  = 650000;

                }

#endregion


                public  int GetHashCode
                {
                    get
                    {
                        var _hash = 3001003L;
#region task
                        _hash = HashCode.Combine(_hash, task);
#endregion
#region name
                        _hash = HashCode.Combine(_hash, name);
#endregion
#region proto
                        if(proto != null)
                            for(int _i = 0, MAX = proto_Count; _i < MAX; _i++) _hash = HashCode.Combine(_hash, proto[_i]);
#endregion
                        return (int) _hash;
                    }
                }
                bool IEquatable<Proto>.Equals(Proto? _pack)
                {
                    if(_pack == null) return false;
                    bool _t;
#region task
                    if((_t = task != null) == (_pack.task == null) || _t &&  ! task!.Equals(_pack.task)) return false;
#endregion
#region name
                    if((_t = name != null) == (_pack.name == null) || _t &&  ! name!.Equals(_pack.name)) return false;
#endregion
#region proto
                    if((_t = proto != null) == (_pack.proto == null) || _t &&  proto.Length != _pack.proto.Length) return false;
                    if(_t)
                        for(int _i = 0, MAX = proto_Count; _i < MAX; _i++)
                            if(proto[_i] != _pack.proto[_i]) return false;
#endregion
                    return true;
                }




                AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_dst.state)
                        {
                            case 0:
                            if(_dst.put_val(_id_, 2, 1)) goto case 1;
                                return null;
                            case 1:
                                if(!_dst.init_fields_nulls(task != null ? 1 : 0, 1)) return null;
                                if(name != null) _dst.set_fields_nulls(1 << 1);
                                if(proto != null) _dst.set_fields_nulls(1 << 2);
                                _dst.flush_fields_nulls();
                            goto case 2;
#region task
                            case 2:
                            if(_dst.is_null(1)) goto case  3 ;
                            if(_dst.put(task, 3)) goto case 3;
                                return null;
#endregion
#region name
                            case 3:
                            if(_dst.is_null(1  << 1)) goto case  4 ;
                            if(_dst.put(name, 4)) goto case 4;
                                return null;
#endregion
#region proto
                            case 4:
                            if(_dst.is_null(1  << 2)) goto case  6 ;
                                _v = proto.Length;
                                if(! _dst.put_len(_v, 3, _v == 0 ? 6u :  5)) return null;
                            if(_v == 0) goto case 6;
                            goto case 5;
                            case 5:
                                if((_v = _dst.remaining) < (_i = _dst.index_max - _dst.index))
                                {
                                    if(0 < _v)
                                    {
                                        _dst.index = _v += _i = _dst.index;
                                        for(;  _i < _v; _i++)  _dst.put(proto![_i]);
                                    }
                                    _dst.retry_at(5);
                                    return null;
                                }
                                _i += _v = _dst.index;
                                for(; _v < _i; _v++) _dst.put(proto![_v]);
                            goto case 6;
#endregion
                            case 6:
                            default:
                                return null;
                        }
                }




            }

        }
        namespace Entity
        {
            public enum Type : byte
            {
                Project = 0,
                Host = 1,
                Port = 2,
                Pack = 3,
                Field = 4,
                Channel = 5,
            }

        }
        namespace ObserverToAgent
        {
            public class Up_to_date : IEquatable< Up_to_date >,  ObserverCommunication.Receivable, ObserverCommunication.Transmittable   //request to send updated Project pack or Up_to_date  if data is not changed
            {


                public const int _id_ = 10;
                void ObserverCommunication.Receivable.Received(ObserverCommunication via) => ObserverCommunication.onReceiveListener.Received(via, this);

                void ObserverCommunication.Transmittable.Sent(ObserverCommunication via) => ObserverCommunication.onTransmitListener.Sent(via, this);



#region info

                public string? info { get; set; }

#endregion


                public  int GetHashCode
                {
                    get
                    {
                        var _hash = 3001003L;
#region info
                        _hash = HashCode.Combine(_hash, info);
#endregion
                        return (int) _hash;
                    }
                }
                bool IEquatable<Up_to_date>.Equals(Up_to_date? _pack)
                {
                    if(_pack == null) return false;
                    bool _t;
#region info
                    if((_t = info != null) == (_pack.info == null) || _t &&  ! info!.Equals(_pack.info)) return false;
#endregion
                    return true;
                }




                AdHoc.INT.BytesSrc? AdHoc.INT.BytesSrc.get_bytes(AdHoc.Transmitter _dst)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_dst.state)
                        {
                            case 0:
                            if(_dst.put_val(_id_, 2, 1)) goto case 1;
                                return null;
                            case 1:
                                if(!_dst.init_fields_nulls(info != null ? 1 : 0, 1)) return null;
                                _dst.flush_fields_nulls();
                            goto case 2;
#region info
                            case 2:
                            if(_dst.is_null(1)) goto case  3 ;
                            if(_dst.put(info, 3)) goto case 3;
                                return null;
#endregion
                            case 3:
                            default:
                                return null;
                        }
                }

                AdHoc.INT.BytesDst? AdHoc.INT.BytesDst.put_bytes(AdHoc.Receiver _src)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_src.state)
                        {
                            case 0:
                            if(_src.get_fields_nulls(0)) goto case 1;
                                return null;
#region info
                            case 1:
                            if(_src.is_null(1)) goto case 3;
                                if(! _src. get_string(2)) return null;
                                info = _src.get_string();
                            goto case 3;
                            case 2 :
                                info = _src.get_string();
                            goto case 3;
#endregion
                            case 3:
                            default:
                                return null;
                        }
                }



            }
            /*
            JetBrains Rider
            https://www.jetbrains.com/help/rider/Opening_Files_from_Command_Line.html
            VS Code
            https://code.visualstudio.com/docs/editor/command-line#_launching-from-command-line
            -g or --goto	When used with file:line{:character}, opens a file at a specific line and optional character position.
            This argument is provided since some operating systems permit : in a file name.

            */
            public class Show_Code : IEquatable< Show_Code >,  ObserverCommunication.Receivable    //request to show entity in editor
            {


                public const int _id_ = 11;
                void ObserverCommunication.Receivable.Received(ObserverCommunication via) => ObserverCommunication.onReceiveListener.Received(via, this);




#region Type

                public Entity.Type Type
                {
                    get
                        => (Entity.Type)(_bits0  & 7) ;
                    set
                    {
                        _bits0 = (byte)(_bits0 &  ~ 7 | ((byte)value));
                    }
                }


#endregion

#region uid

                public uint uid { get; set; }

#endregion


                public  int GetHashCode
                {
                    get
                    {
                        var _hash = 3001003L;
#region Type
                        _hash = HashCode.Combine(_hash, Type);
#endregion
#region uid
                        _hash = HashCode.Combine(_hash, uid);
#endregion
                        return (int) _hash;
                    }
                }
                bool IEquatable<Show_Code>.Equals(Show_Code? _pack)
                {
                    if(_pack == null) return false;
                    bool _t;
#region Type
                    if(Type != _pack.Type) return false;
#endregion
#region uid
                    if(uid != _pack.uid) return false;
#endregion
                    return true;
                }


                private byte _bits0 = 0;



                AdHoc.INT.BytesDst? AdHoc.INT.BytesDst.put_bytes(AdHoc.Receiver _src)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_src.state)
                        {
                            case 0:
                                if(! _src.try_get4(4, 1)) return null;
                            goto case 1;
                            case 1:
                                uid = _src.get4<uint>() ;;
                            goto case 2;
                            case 2:
                                _src.init_bits();
                                if(! _src.try_get_bits(3, 2)) return null;
#region Type
                                _bits0 = (byte)(_bits0 &  ~ 7 | (_src.get_bits< int >()));
#endregion
                            goto case  3;
                            case 3:
                            default:
                                return null;
                        }
                }



            }

        }
        namespace ServerToAgent
        {
            public class Busy : IEquatable< Busy >,  Communication.Receivable
            {


                public const int _id_ = 4;
                void Communication.Receivable.Received(Communication via) => Communication.onReceiveListener.Received(via, this);




#region task

                public string? task { get; set; }

#endregion

#region info

                public string? info { get; set; }

#endregion

#region timout

                public long timout { get; set; }

#endregion


                public  int GetHashCode
                {
                    get
                    {
                        var _hash = 3001003L;
#region task
                        _hash = HashCode.Combine(_hash, task);
#endregion
#region info
                        _hash = HashCode.Combine(_hash, info);
#endregion
#region timout
                        _hash = HashCode.Combine(_hash, timout);
#endregion
                        return (int) _hash;
                    }
                }
                bool IEquatable<Busy>.Equals(Busy? _pack)
                {
                    if(_pack == null) return false;
                    bool _t;
#region task
                    if((_t = task != null) == (_pack.task == null) || _t &&  ! task!.Equals(_pack.task)) return false;
#endregion
#region info
                    if((_t = info != null) == (_pack.info == null) || _t &&  ! info!.Equals(_pack.info)) return false;
#endregion
#region timout
                    if(timout != _pack.timout) return false;
#endregion
                    return true;
                }





                AdHoc.INT.BytesDst? AdHoc.INT.BytesDst.put_bytes(AdHoc.Receiver _src)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_src.state)
                        {
                            case 0:
                                if(! _src.try_get8(8, 1)) return null;
                            goto case 1;
                            case 1:
                                timout = _src.get8<long>() ;;
                            goto case 2;
                            case 2:
                            if(_src.get_fields_nulls(2)) goto case 3;
                                return null;
#region task
                            case 3:
                            if(_src.is_null(1)) goto case 5;
                                if(! _src. get_string(4)) return null;
                                task = _src.get_string();
                            goto case 5;
                            case 4 :
                                task = _src.get_string();
                            goto case 5;
#endregion
#region info
                            case 5:
                            if(_src.is_null(1  << 1)) goto case 7;
                                if(! _src. get_string(6)) return null;
                                info = _src.get_string();
                            goto case 7;
                            case 6 :
                                info = _src.get_string();
                            goto case 7;
#endregion
                            case 7:
                            default:
                                return null;
                        }
                }



            }
            public class LoginRejected
            {

                public const int _id_ = 2;

                public class Driver : Communication.Receivable
                {
                    public static readonly Driver ONE = new Driver();
                    public override bool Equals(object? obj) => obj == ONE;


                    void Communication.Receivable.Received(Communication via) => Communication.onReceiveListener.Received_ServerToAgent_LoginRejected(via);
                    AdHoc.INT.BytesDst? AdHoc.INT.BytesDst.put_bytes(AdHoc.Receiver _src) => null;


                }

            }
            public class Info : IEquatable< Info >,  Communication.Receivable
            {


                public const int _id_ = 0;
                void Communication.Receivable.Received(Communication via) => Communication.onReceiveListener.Received(via, this);




#region task

                public string? task { get; set; }

#endregion

#region info

                public string? info { get; set; }

#endregion


                public  int GetHashCode
                {
                    get
                    {
                        var _hash = 3001003L;
#region task
                        _hash = HashCode.Combine(_hash, task);
#endregion
#region info
                        _hash = HashCode.Combine(_hash, info);
#endregion
                        return (int) _hash;
                    }
                }
                bool IEquatable<Info>.Equals(Info? _pack)
                {
                    if(_pack == null) return false;
                    bool _t;
#region task
                    if((_t = task != null) == (_pack.task == null) || _t &&  ! task!.Equals(_pack.task)) return false;
#endregion
#region info
                    if((_t = info != null) == (_pack.info == null) || _t &&  ! info!.Equals(_pack.info)) return false;
#endregion
                    return true;
                }





                AdHoc.INT.BytesDst? AdHoc.INT.BytesDst.put_bytes(AdHoc.Receiver _src)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_src.state)
                        {
                            case 0:
                            if(_src.get_fields_nulls(0)) goto case 1;
                                return null;
#region task
                            case 1:
                            if(_src.is_null(1)) goto case 3;
                                if(! _src. get_string(2)) return null;
                                task = _src.get_string();
                            goto case 3;
                            case 2 :
                                task = _src.get_string();
                            goto case 3;
#endregion
#region info
                            case 3:
                            if(_src.is_null(1  << 1)) goto case 5;
                                if(! _src. get_string(4)) return null;
                                info = _src.get_string();
                            goto case 5;
                            case 4 :
                                info = _src.get_string();
                            goto case 5;
#endregion
                            case 5:
                            default:
                                return null;
                        }
                }



            }
            public class Result : IEquatable< Result >,  Communication.Receivable
            {


                public const int _id_ = 1;
                void Communication.Receivable.Received(Communication via) => Communication.onReceiveListener.Received(via, this);




#region task

                public string? task { get; set; }

#endregion

#region result
                public byte[] result_new(int item_len)
                => result = new byte[Math.Min(item_len, result_.LEN_MAX)];

                public byte[] ? result;

                public int result_Count {get; protected set;}

                public void resultˌ(IList<byte>? _src)
                {
                    if(_src == null)
                    {
                        result = null;
                        return;
                    }
                    var _max = Math.Min(result_.LEN_MAX, _src.Count);
                    var _items = result;
                    if(_items == null || _items.Length != _max) _items = result_new(_max);
                    for(var _i = 0; _i < _max; _i++)
                        _items[_i] = _src[_i] ;
                }

                public  interface result_
                {
                    const int LEN_MAX  = 650000;

                }

#endregion


                public  int GetHashCode
                {
                    get
                    {
                        var _hash = 3001003L;
#region task
                        _hash = HashCode.Combine(_hash, task);
#endregion
#region result
                        if(result != null)
                            for(int _i = 0, MAX = result_Count; _i < MAX; _i++) _hash = HashCode.Combine(_hash, result[_i]);
#endregion
                        return (int) _hash;
                    }
                }
                bool IEquatable<Result>.Equals(Result? _pack)
                {
                    if(_pack == null) return false;
                    bool _t;
#region task
                    if((_t = task != null) == (_pack.task == null) || _t &&  ! task!.Equals(_pack.task)) return false;
#endregion
#region result
                    if((_t = result != null) == (_pack.result == null) || _t &&  result.Length != _pack.result.Length) return false;
                    if(_t)
                        for(int _i = 0, MAX = result_Count; _i < MAX; _i++)
                            if(result[_i] != _pack.result[_i]) return false;
#endregion
                    return true;
                }





                AdHoc.INT.BytesDst? AdHoc.INT.BytesDst.put_bytes(AdHoc.Receiver _src)
                {
                    int _i = 0, _t = 0, _v = 0;
                    for(;;)
                        switch(_src.state)
                        {
                            case 0:
                            if(_src.get_fields_nulls(0)) goto case 1;
                                return null;
#region task
                            case 1:
                            if(_src.is_null(1)) goto case 3;
                                if(! _src. get_string(2)) return null;
                                task = _src.get_string();
                            goto case 3;
                            case 2 :
                                task = _src.get_string();
                            goto case 3;
#endregion
#region result
                            case 3:
                            if(_src.is_null(1  << 1)) goto case 6;
                                if(!_src.get_len(3, 4)) return null;
                            goto case 4;
                            case 4 :
                                result_new(_src.index_max);
                            if(_src.index_max  < 1) goto case 6;
                            goto case 5;
                            case 5:
                                if((_t = _src.remaining) < (_i = _src.index_max - _src.index))
                                {
                                    if(0 < _t)
                                    {
                                        _src.index = (_t += _i = _src.index);
                                        for(;  _i < _t; _i++) result![_i]  =  _src.get4< byte >(1);
                                    }
                                    _src.retry_at(5);
                                    return null;
                                }
                                _i += _t = _src.index;
                                for(; _t < _i; _t++)  result![_t]  =  _src.get4< byte >(1);
                            goto case 6;
#endregion
                            case 6:
                            default:
                                return null;
                        }
                }



            }

        }



        public class Communication : Channel, AdHoc.INT.BytesDst.Consumer, AdHoc.INT.BytesSrc.Producer
        {

            public readonly EXT_Dst ext_dst ;
            public readonly EXT_Src ext_src ;

            public Communication()
            {
                ext_dst =  new EXT_Dst(this);
                ext_src = new EXT_Src(this);
            }

            public class EXT_Dst : AdHoc.Receiver, Network.INT.BytesDst
            {

                public EXT_Dst(INT.BytesDst.Consumer ? int_dst) : base(int_dst, 2) { }


                public void Connected(Network.TCP.Flow flow) => onConnectListener(flow, (Communication)int_dst!);

                public Network.INT.BytesSrc? mate => ((Communication)int_dst!).ext_src;
                public void Closed() => onCloseListener((Communication)int_dst!);
            }



            public class  EXT_Src  : AdHoc.Transmitter, Network.INT.BytesSrc
            {

                public EXT_Src(INT.BytesSrc.Producer ? int_src) : base(int_src, null)
                {
                    int_values_src = () =>
                    {
                        var pack = 0UL;
                        sending_value.get(ref pack);
                        return pack;
                    };
                }

                public Network.INT.BytesDst? mate => ((Communication)int_src!).ext_dst;

                public void Connected(Network.TCP.Flow flow) => onConnectListener(flow, (Communication)int_src!);

                public void Closed() => onCloseListener((Communication)int_src!);


                private object? _token;

                public  object?                   token(object? token) => Interlocked.Exchange(ref _token, token);

                public object? token() => _token;

                private Action<EXT.BytesSrc>? subscriber;

                public void subscribe(Action<EXT.BytesSrc>? subscriber, object? token)
                {
                    _token = token;
                    if((this.subscriber = subscriber) != null && !isOpen())  subscriber.Invoke(this);
                }

                public static int power_of_2_sending_queue_size = 5;

                public readonly RingBuffer < Transmittable? > sending        = new(power_of_2_sending_queue_size);
                public readonly RingBuffer<ulong>          sending_value  = new(power_of_2_sending_queue_size);

                private volatile int Lock;
                public void send(Transmittable src)
                {
                    while(!sending.put_multithreaded(src)) Thread.SpinWait(10);
                    subscriber?.Invoke(this);
                }

                public void send(Transmittable src, ulong pack)
                {
                    while(Interlocked.CompareExchange(ref Lock, 1, 0) != 0) Thread.SpinWait(10);
                    while(!sending_value.put_multithreaded(pack)) Thread.SpinWait(10);
                    while(!sending.put_multithreaded(src)) Thread.SpinWait(10);
                    Lock = 0;
                    subscriber?.Invoke(this);
                }

                bool EXT.BytesSrc.isOpen() => base.isOpen() || 0 < sending.size;
            }

            public static Action<Network.TCP.Flow, Communication> onConnectListener = (flow, channel) => {};
            public static Action<Communication> onCloseListener = (channel) => {};

            public static Transmittable.Listener onTransmitListener = Transmittable.Listener.STUB;
            public interface Transmittable : AdHoc.INT.BytesSrc
            {
                protected internal void Sent(Communication via);
                interface Listener
                {
                    void Sent(Communication via, Project pack);
                    void Sent(Communication via, AgentToServer.RequestResult pack);
                    void Sent(Communication via, AgentToServer.Login pack);
                    void Sent(Communication via, AgentToServer.Proto pack);

                    class Stub : Listener
                    {
                        public void Sent(Communication via, Project pack) {}
                        public void Sent(Communication via, AgentToServer.RequestResult pack) {}
                        public void Sent(Communication via, AgentToServer.Login pack) {}
                        public void Sent(Communication via, AgentToServer.Proto pack) {}

                    }

                    static readonly Stub STUB = new();
                }
            }

            public AdHoc.INT.BytesSrc? Sending(AdHoc.Transmitter dst)
            {
                Transmittable? transmittable = null;
                ext_src.sending.get(ref transmittable);
                return transmittable;
            }

            public void Sent(AdHoc.Transmitter dst, AdHoc.INT.BytesSrc input) => ((Transmittable)input).Sent(this);
            public void send(Transmittable src) => ext_src.send(src);




            public static Receivable.Listener onReceiveListener = Receivable.Listener.STUB;
            public interface Receivable : AdHoc.INT.BytesDst
            {
                protected internal void Received(Communication via);

                public interface Listener
                {
                    void Received(Communication via, ServerToAgent.Busy pack);
                    void Received(Communication via, ServerToAgent.Info pack);
                    void Received_ServerToAgent_LoginRejected(Communication via);
                    void Received(Communication via, ServerToAgent.Result pack);
                    void Received_Upload(Communication via);

                    class Stub : Listener
                    {
                        public void Received(Communication via, ServerToAgent.Result pack) {}
                        public void Received_Upload(Communication via) {}
                        public void Received_ServerToAgent_LoginRejected(Communication via) {}
                        public void Received(Communication via, ServerToAgent.Busy pack) {}
                        public void Received(Communication via, ServerToAgent.Info pack) {}

                    }

                    static readonly Stub STUB = new();
                }
            }

            public static Channel.Realm realm = Channel.Realm.DEFAULT;
            public AdHoc.INT.BytesDst? Receiving(AdHoc.Receiver src, int id) => id switch
        {
                ServerToAgent.Busy._id_ => realm.new_ServerToAgent_Busy(),
                                            Layout.View.Info._id_ => Layout.View.Info.Driver.ONE,
                                            ServerToAgent.Info._id_ => realm.new_ServerToAgent_Info(),
                                            Layout._id_ => realm.new_Layout(),
                                            ServerToAgent.LoginRejected._id_ => ServerToAgent.LoginRejected.Driver.ONE,
                                            ServerToAgent.Result._id_ => realm.new_ServerToAgent_Result(),
                                            ObserverToAgent.Show_Code._id_ => realm.new_ObserverToAgent_Show_Code(),
                                            ObserverToAgent.Up_to_date._id_ => realm.new_ObserverToAgent_Up_to_date(),
                                            Upload._id_ => Upload.Driver.ONE,
                                            Layout.View._id_ => realm.new_Layout_View(),
                                            _ => null
            };

            public void Received(AdHoc.Receiver  src, AdHoc.INT.BytesDst output) => ((Receivable)output).Received(this);

        }

        public class ObserverCommunication : Channel, AdHoc.INT.BytesDst.Consumer, AdHoc.INT.BytesSrc.Producer
        {

            public readonly EXT_Dst ext_dst ;
            public readonly EXT_Src ext_src ;

            public ObserverCommunication()
            {
                ext_dst =  new EXT_Dst(this);
                ext_src = new EXT_Src(this);
            }

            public class EXT_Dst : AdHoc.Receiver, Network.INT.BytesDst
            {

                public EXT_Dst(INT.BytesDst.Consumer ? int_dst) : base(int_dst, 2) { }


                public void Connected(Network.TCP.Flow flow) => onConnectListener(flow, (ObserverCommunication)int_dst!);

                public Network.INT.BytesSrc? mate => ((ObserverCommunication)int_dst!).ext_src;
                public void Closed() => onCloseListener((ObserverCommunication)int_dst!);
            }



            public class  EXT_Src  : AdHoc.Transmitter, Network.INT.BytesSrc
            {

                public EXT_Src(INT.BytesSrc.Producer ? int_src) : base(int_src, null)
                {
                    int_values_src = () =>
                    {
                        var pack = 0UL;
                        sending_value.get(ref pack);
                        return pack;
                    };
                }

                public Network.INT.BytesDst? mate => ((ObserverCommunication)int_src!).ext_dst;

                public void Connected(Network.TCP.Flow flow) => onConnectListener(flow, (ObserverCommunication)int_src!);

                public void Closed() => onCloseListener((ObserverCommunication)int_src!);


                private object? _token;

                public  object?                   token(object? token) => Interlocked.Exchange(ref _token, token);

                public object? token() => _token;

                private Action<EXT.BytesSrc>? subscriber;

                public void subscribe(Action<EXT.BytesSrc>? subscriber, object? token)
                {
                    _token = token;
                    if((this.subscriber = subscriber) != null && !isOpen())  subscriber.Invoke(this);
                }

                public static int power_of_2_sending_queue_size = 5;

                public readonly RingBuffer < Transmittable? > sending        = new(power_of_2_sending_queue_size);
                public readonly RingBuffer<ulong>          sending_value  = new(power_of_2_sending_queue_size);

                private volatile int Lock;
                public void send(Transmittable src)
                {
                    while(!sending.put_multithreaded(src)) Thread.SpinWait(10);
                    subscriber?.Invoke(this);
                }

                public void send(Transmittable src, ulong pack)
                {
                    while(Interlocked.CompareExchange(ref Lock, 1, 0) != 0) Thread.SpinWait(10);
                    while(!sending_value.put_multithreaded(pack)) Thread.SpinWait(10);
                    while(!sending.put_multithreaded(src)) Thread.SpinWait(10);
                    Lock = 0;
                    subscriber?.Invoke(this);
                }

                bool EXT.BytesSrc.isOpen() => base.isOpen() || 0 < sending.size;
            }

            public static Action<Network.TCP.Flow, ObserverCommunication> onConnectListener = (flow, channel) => {};
            public static Action<ObserverCommunication> onCloseListener = (channel) => {};

            public static Transmittable.Listener onTransmitListener = Transmittable.Listener.STUB;
            public interface Transmittable : AdHoc.INT.BytesSrc
            {
                protected internal void Sent(ObserverCommunication via);
                interface Listener
                {
                    void Sent(ObserverCommunication via, Project pack);
                    void Sent(ObserverCommunication via, Layout pack);
                    void Sent(ObserverCommunication via, ObserverToAgent.Up_to_date pack);

                    class Stub : Listener
                    {
                        public void Sent(ObserverCommunication via, Project pack) {}
                        public void Sent(ObserverCommunication via, Layout pack) {}
                        public void Sent(ObserverCommunication via, ObserverToAgent.Up_to_date pack) {}

                    }

                    static readonly Stub STUB = new();
                }
            }

            public AdHoc.INT.BytesSrc? Sending(AdHoc.Transmitter dst)
            {
                Transmittable? transmittable = null;
                ext_src.sending.get(ref transmittable);
                return transmittable;
            }

            public void Sent(AdHoc.Transmitter dst, AdHoc.INT.BytesSrc input) => ((Transmittable)input).Sent(this);
            public void send(Transmittable src) => ext_src.send(src);




            public static Receivable.Listener onReceiveListener = Receivable.Listener.STUB;
            public interface Receivable : AdHoc.INT.BytesDst
            {
                protected internal void Received(ObserverCommunication via);

                public interface Listener
                {
                    void Received(ObserverCommunication via, Layout pack);
                    void Received(ObserverCommunication via, ObserverToAgent.Up_to_date pack);
                    void Received(ObserverCommunication via, ObserverToAgent.Show_Code pack);

                    class Stub : Listener
                    {
                        public void Received(ObserverCommunication via, Layout pack) {}
                        public void Received(ObserverCommunication via, ObserverToAgent.Up_to_date pack) {}
                        public void Received(ObserverCommunication via, ObserverToAgent.Show_Code pack) {}

                    }

                    static readonly Stub STUB = new();
                }
            }

            public static Channel.Realm realm = Channel.Realm.DEFAULT;
            public AdHoc.INT.BytesDst? Receiving(AdHoc.Receiver src, int id) => id switch
        {
                ServerToAgent.Busy._id_ => realm.new_ServerToAgent_Busy(),
                                            Layout.View.Info._id_ => Layout.View.Info.Driver.ONE,
                                            ServerToAgent.Info._id_ => realm.new_ServerToAgent_Info(),
                                            Layout._id_ => realm.new_Layout(),
                                            ServerToAgent.LoginRejected._id_ => ServerToAgent.LoginRejected.Driver.ONE,
                                            ServerToAgent.Result._id_ => realm.new_ServerToAgent_Result(),
                                            ObserverToAgent.Show_Code._id_ => realm.new_ObserverToAgent_Show_Code(),
                                            ObserverToAgent.Up_to_date._id_ => realm.new_ObserverToAgent_Up_to_date(),
                                            Upload._id_ => Upload.Driver.ONE,
                                            Layout.View._id_ => realm.new_Layout_View(),
                                            _ => null
            };

            public void Received(AdHoc.Receiver  src, AdHoc.INT.BytesDst output) => ((Receivable)output).Received(this);

        }


    }
}
