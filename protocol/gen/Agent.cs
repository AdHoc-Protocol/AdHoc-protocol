//MIT License
//
//Copyright © 2020 Chikirev Sirguy, Unirail Group. All rights reserved.
//For inquiries, please contact:  al8v5C6HU4UtqE9@gmail.com
//GitHub Repository: https://github.com/AdHoc-Protocol
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to use,
//copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
//the Software, and to permit others to do so, under the following conditions:
//
//1. The above copyright notice and this permission notice must be included in all
//   copies or substantial portions of the Software.
//
//2. Users of the Software must provide a clear acknowledgment in their user
//   documentation or other materials that their solution includes or is based on
//   this Software. This acknowledgment should be prominent and easily visible,
//   and can be formatted as follows:
//   "This product includes software developed by Chikirev Sirguy and the Unirail Group
//   (https://github.com/AdHoc-Protocol)."
//
//3. If you modify the Software and distribute it, you must include a prominent notice
//   stating that you have changed the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT, OR OTHERWISE, ARISING FROM,
//OUT OF, OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

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

namespace org.unirail
{
    /*
    <see cref = 'InCS'/> following packs generate in C# with full implementation
    <see cref = 'Server.Info'/>
    <see cref = 'Server.Result'/>
    <see cref = 'RequestResult'/>
    <see cref = 'Version'/>
    <see cref = 'Signup'/>
    <see cref = 'Login'/>
    <see cref = 'Proto'/>
    <see cref = 'Observer.Up_to_date'/>
    <see cref = 'Observer.Show_Code'/>
    <see cref = 'InCS'/>-- by default rest of the packs in C#  generate abstracted (without implementation)

    */
    namespace Agent
    {
        public class Signup : IEquatable<Signup>, Communication.Transmitter.Transmittable
        {
            public int __id => __id_;
            public const int __id_ = 12;

            void Communication.Transmitter.Transmittable.Sent(Communication.Transmitter via) => Communication.Transmitter.onTransmit.Sent(via, this);
            #region oauth
            public byte[]? oauth_new(int size)
            { //preallocate space
                return _oauth = new byte[size];
            }

            public byte[]? _oauth;

            public int oauth_len => _oauth!.Length;

            public void oauth(byte[]? __src)
            {
                if (__src == null)
                {
                    _oauth = null;
                    return;
                }

                _oauth = AdHoc.Resize<byte>(__src, Math.Min(__src!.Length, Agent.Signup.oauth_.ARRAY_LEN_MAX), 0);
                ;
            }

            public struct oauth_
            { //OAuth token with a maximum of 50 characters

                public const int ARRAY_LEN_MAX = 50;
            }
            #endregion

            public int GetHashCode
            {
                get
                {
                    var _hash = 3001003L;
                    #region oauth
                    if (_oauth != null)
                        for (int __i = 0, MAX = oauth_len; __i < MAX; __i++)
                            _hash = HashCode.Combine(_hash, _oauth[__i]);
                    #endregion

                    return (int)_hash;
                }
            }

            bool IEquatable<Signup>.Equals(Signup? _pack)
            {
                if (_pack == null)
                    return false;

                bool __t;
                #region oauth
                if (_oauth != _pack._oauth)
                    if (_oauth == null || _pack._oauth == null || _oauth!.Length != _pack._oauth!.Length)
                        return false;
                    else
                        for (int __i = 0, MAX = oauth_len; __i < MAX; __i++)
                            if (_oauth[__i] != _pack._oauth[__i])
                                return false;
                #endregion

                return true;
            }

            bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
            {
                var __slot = __dst.slot!;
                int __i = 0, __t = 0, __v = 0;
                for (; ; )
                    switch (__slot.state)
                    {
                        case 0:
                            if (__dst.put_val(__id_, 1, 1))
                                goto case 1;
                            return false;

                        case 1:

                            if (!__dst.init_fields_nulls(_oauth != null ?
                                                             1 :
                                                             0, 1))
                                return false;

                            __dst.flush_fields_nulls();
                            goto case 2;
                        case 2:
                            #region oauth
                            if (__dst.is_null(1))
                                goto case 4;

                            if (__slot.index_max_1(_oauth!.Length) == 0)
                            {
                                if (__dst.put_val(0, 1, 4)) goto case 4;
                                return false;
                            }

                            if (!__dst.put_val((uint)__slot.index_max1, 1, 3)) return false;

                            goto case 3;
                        case 3:

                            if ((__v = __dst.remaining) < (__i = __slot.index_max1 - __slot.index1))
                            {
                                if (0 < __v)
                                {
                                    __slot.index1 = __v += __i = __slot.index1;
                                    for (; __i < __v; __i++)
                                        __dst.put((byte)_oauth![__i]);
                                }

                                __dst.retry_at(3);
                                return false;
                            }

                            __i += __v = __slot.index1;
                            for (; __v < __i; __v++) __dst.put((byte)_oauth![__v]);
                            goto case 4;
                        case 4:
                        #endregion

                        default:
                            return true;
                    }
            }
        }

        public struct Invitation : IEquatable<Invitation>
        {
            public int __id => __id_;
            public const int __id_ = 3;

            public ulong uid = 0; //Unique identifier for the invitation

            public Invitation() { }
            public Invitation(ulong src) => uid = src;

            public class Handler : Communication.Receiver.Receivable, AdHoc.Transmitter.BytesSrc
            {
                public int __id => 3;
                public static readonly Handler ONE = new();

                void Communication.Receiver.Receivable.Received(Communication.Receiver via) => Communication.Receiver.onReceive.Received(via, (Invitation)via.u8);

                bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
                {
                    var __slot = __dst.slot!;
                    ulong _bits;
                    switch (__slot.state)
                    {
                        case 0:
                            __dst.pull_value();
                            if (__dst.put_val(__id_, 1, 1))
                                goto case 1;
                            return false;
                        case 1:
                            return __dst.put((ulong)__dst.u8, 123);

                        default: return true;
                    }
                }

                bool AdHoc.Receiver.BytesDst.__put_bytes(AdHoc.Receiver __src)
                {
                    var __slot = __src.slot!;
                    switch (__slot.state)
                    {
                        case 0:
                            return __src.get_ulong_u8(123);

                        default:
                            return true;
                    }
                }
            }

            public bool Equals(Invitation other) => uid == other.uid;

            public static bool operator ==(Invitation? a, Invitation? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.uid == b!.Value.uid);
            public static bool operator !=(Invitation? a, Invitation? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.uid != b!.Value.uid);

            public override bool Equals(object? other) => other is Invitation p && p.uid == uid;
            public override int GetHashCode() => uid.GetHashCode();

            public static implicit operator ulong(Invitation a) => a.uid;
            public static implicit operator Invitation(ulong a) => new Invitation(a);
        }

        public class Proto : IEquatable<Proto>, Communication.Transmitter.Transmittable
        {
            public int __id => __id_;
            public const int __id_ = 7;

            void Communication.Transmitter.Transmittable.Sent(Communication.Transmitter via) => Communication.Transmitter.onTransmit.Sent(via, this);
            #region task
            public string? task { get; set; } = null; //Unique ID

            public struct task_
            { //Unique ID

                public const int STR_LEN_MAX = 255;
            }
            #endregion
            #region name
            public string? name { get; set; } = null;

            public struct name_
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

                _proto = AdHoc.Resize<byte>(__src, Math.Min(__src!.Length, Agent.Proto.proto_.ARRAY_LEN_MAX), 0);
                ;
            }

            public struct proto_
            { //Max 65k zipped

                public const int ARRAY_LEN_MAX = 512000;
            }
            #endregion

            public int GetHashCode
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
                    if (_proto != null)
                        for (int __i = 0, MAX = proto_len; __i < MAX; __i++)
                            _hash = HashCode.Combine(_hash, _proto[__i]);
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
                        for (int __i = 0, MAX = proto_len; __i < MAX; __i++)
                            if (_proto[__i] != _pack._proto[__i])
                                return false;
                #endregion

                return true;
            }

            bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
            {
                var __slot = __dst.slot!;
                int __i = 0, __t = 0, __v = 0;
                for (; ; )
                    switch (__slot.state)
                    {
                        case 0:
                            if (__dst.put_val(__id_, 1, 1))
                                goto case 1;
                            return false;

                        case 1:

                            if (!__dst.init_fields_nulls(task != null ?
                                                             1 :
                                                             0, 1))
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

        public interface Layout : ObserverCommunication.Receiver.Receivable, ObserverCommunication.Transmitter.Transmittable
        {
            int AdHoc.Receiver.BytesDst.__id => __id_;
            int AdHoc.Transmitter.BytesSrc.__id => __id_;
            public const int __id_ = 9;
            void ObserverCommunication.Receiver.Receivable.Received(ObserverCommunication.Receiver via) => ObserverCommunication.Receiver.onReceive.Received(via, this);

            void ObserverCommunication.Transmitter.Transmittable.Sent(ObserverCommunication.Transmitter via) => ObserverCommunication.Transmitter.onTransmit.Sent(via, this);
            #region split
            public byte _split { get; set; }
            #endregion
            #region host_packs
            protected Layout.View _host_packs_new_item(AdHoc.Receiver scope) => (Layout.View)scope.Allocate(Layout.View.__id_)!;

            public Agent.Layout.View? _host_packs { get; set; }
            #endregion
            #region pack_fields
            protected Layout.View _pack_fields_new_item(AdHoc.Receiver scope) => (Layout.View)scope.Allocate(Layout.View.__id_)!;

            public Agent.Layout.View? _pack_fields { get; set; }
            #endregion
            #region channels_id2xy
            //Create a new field. Allocate the field collection to be populated.
            public void _channels_id2xy_new(Context.Receiver ctx, Context.Receiver.Slot __slot, int items_count);

            //Get a reference to the field data for existence and equality checks
            public object? _channels_id2xy();

            //Get the number of items in the Map at the specific location of the multidimensional field
            public int _channels_id2xy_len { get; }

            //Put the Key and the Value to the Map at the specific location of the multidimensional field
            public void _channels_id2xy(Context.Receiver ctx, Context.Receiver.Slot __slot, uint key, Agent.Layout.XY value);

            //Prepare and initialize before enumerating items in the Map at a specific location of the multidimensional field
            public void _channels_id2xy_Init(Context.Transmitter ctx, Context.Transmitter.Slot __slot);

            public uint _channels_id2xy_NextItem_Key(Context.Transmitter ctx, Context.Transmitter.Slot __slot);

            //Get the value of the item's Value in the Map
            public Agent.Layout.XY _channels_id2xy_Val(Context.Transmitter ctx, Context.Transmitter.Slot __slot);

            public interface _channels_id2xy_
            {
                public const int TYPE_LEN_MAX = 255;
            }
            #endregion

            bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
            {
                var __slot = __dst.slot!;
                int __i = 0, __t = 0, __v = 0;
                for (; ; )
                    switch (__slot.state)
                    {
                        case 0:
                            if (__dst.put_val(__id_, 1, 1))
                                goto case 1;
                            return false;

                        case 1:

                            if (!__dst.Allocate(1, 1))
                                return false;
                            __dst.put((byte)_split);

                            goto case 2;
                        case 2:

                            if (!__dst.init_fields_nulls(_host_packs != null ?
                                                             1 :
                                                             0, 2))
                                return false;
                            if (_pack_fields != null) __dst.set_fields_nulls(1 << 1);
                            if (_channels_id2xy() != null)
                                __dst.set_fields_nulls(1 << 2);

                            __dst.flush_fields_nulls();
                            goto case 3;
                        case 3:
                            #region host_packs
                            if (__dst.is_null(1))
                                goto case 4;
                            if (__dst.put_bytes(_host_packs!, 4)) goto case 4;
                            return false;
                        case 4:
                            #endregion
                            #region pack_fields
                            if (__dst.is_null(1 << 1))
                                goto case 5;
                            if (__dst.put_bytes(_pack_fields!, 5)) goto case 5;
                            return false;
                        case 5:
                            #endregion
                            #region channels_id2xy
                            if (__dst.is_null(1 << 2))
                                goto case 10;

                            if (!__dst.Allocate(5, 5)) return false;
                            if (__slot.no_items(_channels_id2xy_len, 255)) goto case 10;
                            #region sending map info
                            __slot.put_info();
                            goto case 6;
                        case 6:
                            #endregion

                            _channels_id2xy_Init(__dst, __slot);
                            goto case 7;
                        #region sending key
                        case 7:
                            if (__dst.put((uint)_channels_id2xy_NextItem_Key(__dst, __slot), 8))
                                goto case 8;
                            return false;
                        case 8:
                            #endregion
                            #region sending value
                            if (__dst.put((ulong)(ulong)_channels_id2xy_Val(__dst, __slot), 9))
                                goto case 9;
                            return false;
                        case 9:
                            #endregion
                            if (__slot.next_index1())
                                goto case 7;

                            goto case 10;
                        case 10:
                        #endregion

                        default:
                            return true;
                    }
            }

            bool AdHoc.Receiver.BytesDst.__put_bytes(AdHoc.Receiver __src)
            {
                var __slot = __src.slot!;
                int __i = 0, __t = 0, __v = 0;
                for (; ; )
                    switch (__slot.state)
                    {
                        case 0:

                            if (!__src.has_1bytes(1))
                                return false;
                            _split = (byte)__src.get_byte();
                            goto case 2; //leap
                        case 1:
                            _split = (byte)__src.get_byte_();
                            goto case 2;
                        case 2:

                            if (__src.get_fields_nulls(2))
                                goto case 3;
                            return false;
                        case 3:
                            #region host_packs
                            if (__src.is_null(1))
                                goto case 5;

                            {
                                var val = __src.try_get_bytes(_host_packs_new_item(__src), 4);
                                if (val == null) return false;
                                _host_packs = val;
                            }
                            goto case 5; //leap
                        case 4:
                            _host_packs = __slot.get_bytes<Agent.Layout.View>();
                            goto case 5;
                        case 5:
                            #endregion
                            #region pack_fields
                            if (__src.is_null(1 << 1))
                                goto case 7;

                            {
                                var val = __src.try_get_bytes(_pack_fields_new_item(__src), 6);
                                if (val == null) return false;
                                _pack_fields = val;
                            }
                            goto case 7; //leap
                        case 6:
                            _pack_fields = __slot.get_bytes<Agent.Layout.View>();
                            goto case 7;
                        case 7:
                            #endregion
                            #region channels_id2xy
                            if (__src.is_null(1 << 2))
                                goto case 13;

                            if (__slot.try_get_info(7) && __slot.try_items_count(Agent.Layout._channels_id2xy_.TYPE_LEN_MAX, 8)) goto case 8;
                            return false;
                        case 8:

                            _channels_id2xy_new(__src, __slot, __v = __slot.items_count(Agent.Layout._channels_id2xy_.TYPE_LEN_MAX));

                            if (__v == 0) goto case 13;

                            if (__slot.leap()) goto case 13;

                            goto case 9;
                        case 9:
                            #region receiving key
                            if (!__src.has_4bytes(10))
                                return false;
                            __slot._ulong = (uint)__src.get_uint();
                            goto case 11; //leap
                        case 10:
                            __slot._ulong = (uint)__src.get_uint_();
                            goto case 11;
                        case 11:
                            #endregion
                            #region receiving value
                            if (!__src.has_8bytes(12))
                                return false;

                            _channels_id2xy(__src, __slot, (uint)__slot._ulong, new Agent.Layout.XY((ulong)__src.get_ulong()));
                            if (__slot.next_index1()) goto case 9;

                            goto case 13; //leap
                        case 12:

                            _channels_id2xy(__src, __slot, (uint)__slot._ulong, new Agent.Layout.XY((ulong)__src.get_ulong_()));
                            if (__slot.next_index1()) goto case 9;

                            goto case 13;
                        case 13:
                        #endregion
                        #endregion

                        default:
                            return true;
                    }
            }

            public struct XY : IEquatable<XY>
            {
                public int __id => __id_;
                public const int __id_ = -8;

                public ulong Value = 0x0;
                public XY() { }
                public XY(ulong src) => Value = src;

                public int x
                {
                    get
                    {
                        var _inT = (Value & 0xFFFF_FFFFUL);
                        return (int)((long)_inT - 0x8000_0000L);
                    }
                    set => Value = (ulong)(Value & 0xFFFF_FFFF_0000_0000UL | (ulong)((uint)(value + 0x8000_0000L)));
                }

                public int y
                {
                    get
                    {
                        var _inT = (Value >> 32 & 0xFFFF_FFFFUL);
                        return (int)((long)_inT - 0x8000_0000L);
                    }
                    set => Value = (ulong)(Value & 0xFFFF_FFFFUL | (ulong)((uint)(value + 0x8000_0000L)) << 32);
                }

                public class Handler : AdHoc.Receiver.BytesDst, AdHoc.Transmitter.BytesSrc
                {
                    public int __id => -8;
                    public static readonly Handler ONE = new();

                    bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
                    {
                        var __slot = __dst.slot!;
                        ulong _bits;
                        switch (__slot.state)
                        {
                            case 0:
                                __dst.pull_value();
                                throw new NotSupportedException();
                            case 1:
                                return __dst.put_val(__dst.u8, 8, 2);
                            default:
                                return true;
                        }
                    }

                    bool AdHoc.Receiver.BytesDst.__put_bytes(AdHoc.Receiver __src)
                    {
                        var __slot = __src.slot!;
                        switch (__slot.state)
                        {
                            case 0:
                                return __src.try_get8(8, 1);

                            default:
                                return true;
                        }
                    }
                }

                public bool Equals(XY other) => Value == other.Value;

                public static bool operator ==(XY? a, XY? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.Value == b!.Value.Value);
                public static bool operator !=(XY? a, XY? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.Value != b!.Value.Value);

                public override bool Equals(object? other) => other is XY p && p.Value == Value;
                public override int GetHashCode() => Value.GetHashCode();

                public static implicit operator ulong(XY a) => a.Value;
                public static implicit operator XY(ulong a) => new XY(a);
            }

            public interface View : AdHoc.Receiver.BytesDst, AdHoc.Transmitter.BytesSrc
            {
                int AdHoc.Receiver.BytesDst.__id => __id_;
                int AdHoc.Transmitter.BytesSrc.__id => __id_;
                public const int __id_ = -7;
                #region X
                public int _X { get; set; }
                #endregion
                #region Y
                public int _Y { get; set; }
                #endregion
                #region zoom
                public float _zoom { get; set; }
                #endregion
                #region id2xy
                //Create a new field. Allocate the field collection to be populated.
                public void _id2xy_new(Context.Receiver ctx, Context.Receiver.Slot __slot, int items_count);

                //Get a reference to the field data for existence and equality checks
                public object? _id2xy();

                //Get the number of items in the Map at the specific location of the multidimensional field
                public int _id2xy_len { get; }

                //Put the Key and the Value to the Map at the specific location of the multidimensional field
                public void _id2xy(Context.Receiver ctx, Context.Receiver.Slot __slot, uint key, Agent.Layout.XY value);

                //Prepare and initialize before enumerating items in the Map at a specific location of the multidimensional field
                public void _id2xy_Init(Context.Transmitter ctx, Context.Transmitter.Slot __slot);

                public uint _id2xy_NextItem_Key(Context.Transmitter ctx, Context.Transmitter.Slot __slot); //Fast access to info by ID

                //Get the value of the item's Value in the Map
                public Agent.Layout.XY _id2xy_Val(Context.Transmitter ctx, Context.Transmitter.Slot __slot);

                public interface _id2xy_
                { //Fast access to info by ID

                    public const int TYPE_LEN_MAX = 255;
                }
                #endregion

                bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
                {
                    var __slot = __dst.slot!;
                    int __i = 0, __t = 0, __v = 0;
                    for (; ; )
                        switch (__slot.state)
                        {
                            case 0:
                                throw new NotSupportedException();
                            case 1:

                                if (!__dst.Allocate(12, 1))
                                    return false;
                                __dst.put((int)_X);
                                __dst.put((int)_Y);
                                __dst.put((uint)BitConverter.SingleToUInt32Bits(_zoom));

                                goto case 2;
                            case 2:

                                if (!__dst.init_fields_nulls(_id2xy() != null ?
                                                                 1 :
                                                                 0, 2))
                                    return false;

                                __dst.flush_fields_nulls();
                                goto case 3;
                            case 3:
                                #region id2xy
                                if (__dst.is_null(1))
                                    goto case 8;

                                if (!__dst.Allocate(5, 3)) return false;
                                if (__slot.no_items(_id2xy_len, 255)) goto case 8;
                                #region sending map info
                                __slot.put_info();
                                goto case 4;
                            case 4:
                                #endregion

                                _id2xy_Init(__dst, __slot);
                                goto case 5;
                            #region sending key
                            case 5:
                                if (__dst.put((uint)_id2xy_NextItem_Key(__dst, __slot), 6))
                                    goto case 6;
                                return false;
                            case 6:
                                #endregion
                                #region sending value
                                if (__dst.put((ulong)(ulong)_id2xy_Val(__dst, __slot), 7))
                                    goto case 7;
                                return false;
                            case 7:
                                #endregion
                                if (__slot.next_index1())
                                    goto case 5;

                                goto case 8;
                            case 8:
                            #endregion

                            default:
                                return true;
                        }
                }

                bool AdHoc.Receiver.BytesDst.__put_bytes(AdHoc.Receiver __src)
                {
                    var __slot = __src.slot!;
                    int __i = 0, __t = 0, __v = 0;
                    for (; ; )
                        switch (__slot.state)
                        {
                            case 0:

                                if (!__src.has_4bytes(1))
                                    return false;
                                _X = (int)__src.get_int();
                                goto case 2; //leap
                            case 1:
                                _X = (int)__src.get_int_();
                                goto case 2;
                            case 2:

                                if (!__src.has_4bytes(3))
                                    return false;
                                _Y = (int)__src.get_int();
                                goto case 4; //leap
                            case 3:
                                _Y = (int)__src.get_int_();
                                goto case 4;
                            case 4:

                                if (!__src.has_4bytes(5))
                                    return false;
                                _zoom = (float)BitConverter.UInt32BitsToSingle((uint)(__src.get_uint()));
                                goto case 6; //leap
                            case 5:
                                _zoom = (float)BitConverter.UInt32BitsToSingle((uint)(__src.get_uint_()));
                                goto case 6;
                            case 6:

                                if (__src.get_fields_nulls(6))
                                    goto case 7;
                                return false;
                            case 7:
                                #region id2xy
                                if (__src.is_null(1))
                                    goto case 13;

                                if (__slot.try_get_info(7) && __slot.try_items_count(Agent.Layout.View._id2xy_.TYPE_LEN_MAX, 8)) goto case 8;
                                return false;
                            case 8:

                                _id2xy_new(__src, __slot, __v = __slot.items_count(Agent.Layout.View._id2xy_.TYPE_LEN_MAX));

                                if (__v == 0) goto case 13;

                                if (__slot.leap()) goto case 13;

                                goto case 9;
                            case 9:
                                #region receiving key
                                if (!__src.has_4bytes(10))
                                    return false;
                                __slot._ulong = (uint)__src.get_uint();
                                goto case 11; //leap
                            case 10:
                                __slot._ulong = (uint)__src.get_uint_();
                                goto case 11;
                            case 11:
                                #endregion
                                #region receiving value
                                if (!__src.has_8bytes(12))
                                    return false;

                                _id2xy(__src, __slot, (uint)__slot._ulong, new Agent.Layout.XY((ulong)__src.get_ulong()));
                                if (__slot.next_index1()) goto case 9;

                                goto case 13; //leap
                            case 12:

                                _id2xy(__src, __slot, (uint)__slot._ulong, new Agent.Layout.XY((ulong)__src.get_ulong_()));
                                if (__slot.next_index1()) goto case 9;

                                goto case 13;
                            case 13:
                            #endregion
                            #endregion

                            default:
                                return true;
                        }
                }
            }
        }

        public struct Login : IEquatable<Login>
        {
            public int __id => __id_;
            public const int __id_ = 8;

            public ulong uid = 0; //Unique identifier for the login

            public Login() { }
            public Login(ulong src) => uid = src;

            public class Handler : AdHoc.Receiver.BytesDst, Communication.Transmitter.Transmittable
            {
                public int __id => 8;
                public static readonly Handler ONE = new();

                void Communication.Transmitter.Transmittable.Sent(Communication.Transmitter via) => Communication.Transmitter.onTransmit.Sent(via, (Login)via.u8);

                bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
                {
                    var __slot = __dst.slot!;
                    ulong _bits;
                    switch (__slot.state)
                    {
                        case 0:
                            __dst.pull_value();
                            if (__dst.put_val(__id_, 1, 1))
                                goto case 1;
                            return false;
                        case 1:
                            return __dst.put((ulong)__dst.u8, 123);

                        default: return true;
                    }
                }

                bool AdHoc.Receiver.BytesDst.__put_bytes(AdHoc.Receiver __src)
                {
                    var __slot = __src.slot!;
                    switch (__slot.state)
                    {
                        case 0:
                            return __src.get_ulong_u8(123);

                        default:
                            return true;
                    }
                }
            }

            public bool Equals(Login other) => uid == other.uid;

            public static bool operator ==(Login? a, Login? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.uid == b!.Value.uid);
            public static bool operator !=(Login? a, Login? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.uid != b!.Value.uid);

            public override bool Equals(object? other) => other is Login p && p.uid == uid;
            public override int GetHashCode() => uid.GetHashCode();

            public static implicit operator ulong(Login a) => a.uid;
            public static implicit operator Login(ulong a) => new Login(a);
        }

        public interface Project : Communication.Transmitter.Transmittable, ObserverCommunication.Transmitter.Transmittable
        {
            int AdHoc.Transmitter.BytesSrc.__id => __id_;
            public const int __id_ = 5;

            void Communication.Transmitter.Transmittable.Sent(Communication.Transmitter via) => Communication.Transmitter.onTransmit.Sent(via, this);
            void ObserverCommunication.Transmitter.Transmittable.Sent(ObserverCommunication.Transmitter via) => ObserverCommunication.Transmitter.onTransmit.Sent(via, this);
            #region task
            public string? _task { get; }

            public struct _task_
            { //Unique ID

                public const int STR_LEN_MAX = 255;
            }
            #endregion
            #region namespacE
            public string? _namespacE { get; }

            public struct _namespacE_
            {
                public const int STR_LEN_MAX = 255;
            }
            #endregion
            #region time
            public long _time { get; }
            #endregion
            #region source
            //Get a reference to the field data for existence and equality checks
            public object? _source();

            //Get the length of all item's fixed-length collections of the multidimensional field
            public int _source_len { get; }

            //Get the element of the collection
            public byte _source(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

            public struct _source_
            { //Max 130k zipped sources

                public const int ARRAY_LEN_MAX = 131071;
            }
            #endregion
            #region fields
            //Get a reference to the field data for existence and equality checks
            public object? _fields();

            //Get the length of all item's fixed-length collections of the multidimensional field
            public int _fields_len { get; }

            //Get the element of the collection
            public Agent.Project.Host.Pack.Field _fields(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

            public struct _fields_
            {
                public const int ARRAY_LEN_MAX = 65535;
            }
            #endregion
            #region packs
            //Get a reference to the field data for existence and equality checks
            public object? _packs();

            //Get the length of all item's fixed-length collections of the multidimensional field
            public int _packs_len { get; }

            //Get the element of the collection
            public Agent.Project.Host.Pack _packs(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

            public struct _packs_
            {
                public const int ARRAY_LEN_MAX = 65535;
            }
            #endregion
            #region hosts
            //Get a reference to the field data for existence and equality checks
            public object? _hosts();

            //Get the length of all item's fixed-length collections of the multidimensional field
            public int _hosts_len { get; }

            //Get the element of the collection
            public Agent.Project.Host _hosts(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

            public struct _hosts_
            { //512 max

                public const int ARRAY_LEN_MAX = 511;
            }
            #endregion
            #region channels
            //Get a reference to the field data for existence and equality checks
            public object? _channels();

            //Get the length of all item's fixed-length collections of the multidimensional field
            public int _channels_len { get; }

            //Get the element of the collection
            public Agent.Project.Channel _channels(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

            public struct _channels_
            { //512/2 = 256 max

                public const int ARRAY_LEN_MAX = 255;
            }
            #endregion
            #region name
            public string? _name { get; }

            public struct _name_
            {
                public const int STR_LEN_MAX = 255;
            }
            #endregion
            #region doc
            public string? _doc { get; }

            public struct _doc_
            { //Documentation with a maximum of 65,000 characters

                public const int STR_LEN_MAX = 65000;
            }
            #endregion
            #region inline_doc
            public string? _inline_doc { get; }

            public struct _inline_doc_
            {
                public const int STR_LEN_MAX = 255;
            }
            #endregion

            bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
            {
                var __slot = __dst.slot!;
                int __i = 0, __t = 0, __v = 0;
                for (; ; )
                    switch (__slot.state)
                    {
                        case 0:
                            if (__dst.put_val(__id_, 1, 1))
                                goto case 1;
                            return false;

                        case 1:

                            if (!__dst.Allocate(8, 1))
                                return false;
                            __dst.put((long)_time);

                            goto case 2;
                        case 2:

                            if (!__dst.init_fields_nulls(_task != null ?
                                                             1 :
                                                             0, 2))
                                return false;
                            if (_namespacE != null) __dst.set_fields_nulls(1 << 1);
                            if (_source() != null)
                                __dst.set_fields_nulls(1 << 2);
                            if (_fields() != null)
                                __dst.set_fields_nulls(1 << 3);
                            if (_packs() != null)
                                __dst.set_fields_nulls(1 << 4);
                            if (_hosts() != null)
                                __dst.set_fields_nulls(1 << 5);
                            if (_channels() != null)
                                __dst.set_fields_nulls(1 << 6);
                            if (_name != null)
                                __dst.set_fields_nulls(1 << 7);

                            __dst.flush_fields_nulls();
                            goto case 3;
                        case 3:
                            #region task
                            if (__dst.is_null(1))
                                goto case 4;
                            if (__dst.put(_task!, 4)) goto case 4;
                            return false;
                        case 4:
                            #endregion
                            #region namespacE
                            if (__dst.is_null(1 << 1))
                                goto case 5;
                            if (__dst.put(_namespacE!, 5)) goto case 5;
                            return false;
                        case 5:
                            #endregion
                            #region source
                            if (__dst.is_null(1 << 2))
                                goto case 7;

                            if (__slot.index_max_1(_source_len) == 0)
                            {
                                if (__dst.put_val(0, 3, 7)) goto case 7;
                                return false;
                            }

                            if (!__dst.put_val((uint)__slot.index_max1, 3, 6)) return false;

                            goto case 6;
                        case 6:

                            if ((__v = __dst.remaining) < (__i = __slot.index_max1 - __slot.index1))
                            {
                                if (0 < __v)
                                {
                                    __slot.index1 = __v += __i = __slot.index1;
                                    for (; __i < __v; __i++)
                                        __dst.put((byte)_source(__dst, __slot, __i));
                                }

                                __dst.retry_at(6);
                                return false;
                            }

                            __i += __v = __slot.index1;
                            for (; __v < __i; __v++) __dst.put((byte)_source(__dst, __slot, __v));
                            goto case 7;
                        case 7:
                            #endregion
                            #region fields
                            if (__dst.is_null(1 << 3))
                                goto case 9;

                            if (__slot.index_max_1(_fields_len) == 0)
                            {
                                if (__dst.put_val(0, 2, 9)) goto case 9;
                                return false;
                            }

                            if (!__dst.put_val((uint)__slot.index_max1, 2, 8)) return false;

                            goto case 8;
                        case 8:

                            for (var b = true; b;)
                                if (!__dst.put_bytes(_fields(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ?
                                                                                                 8U :
                                                                                                 9U))
                                    return false;

                            goto case 9;
                        case 9:
                            #endregion
                            #region packs
                            if (__dst.is_null(1 << 4))
                                goto case 11;

                            if (__slot.index_max_1(_packs_len) == 0)
                            {
                                if (__dst.put_val(0, 2, 11)) goto case 11;
                                return false;
                            }

                            if (!__dst.put_val((uint)__slot.index_max1, 2, 10)) return false;

                            goto case 10;
                        case 10:

                            for (var b = true; b;)
                                if (!__dst.put_bytes(_packs(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ?
                                                                                                10U :
                                                                                                11U))
                                    return false;

                            goto case 11;
                        case 11:
                            #endregion
                            #region hosts
                            if (__dst.is_null(1 << 5))
                                goto case 13;

                            if (__slot.index_max_1(_hosts_len) == 0)
                            {
                                if (__dst.put_val(0, 2, 13)) goto case 13;
                                return false;
                            }

                            if (!__dst.put_val((uint)__slot.index_max1, 2, 12)) return false;

                            goto case 12;
                        case 12:

                            for (var b = true; b;)
                                if (!__dst.put_bytes(_hosts(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ?
                                                                                                12U :
                                                                                                13U))
                                    return false;

                            goto case 13;
                        case 13:
                            #endregion
                            #region channels
                            if (__dst.is_null(1 << 6))
                                goto case 15;

                            if (__slot.index_max_1(_channels_len) == 0)
                            {
                                if (__dst.put_val(0, 1, 15)) goto case 15;
                                return false;
                            }

                            if (!__dst.put_val((uint)__slot.index_max1, 1, 14)) return false;

                            goto case 14;
                        case 14:

                            for (var b = true; b;)
                                if (!__dst.put_bytes(_channels(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ?
                                                                                                   14U :
                                                                                                   15U))
                                    return false;

                            goto case 15;
                        case 15:
                            #endregion
                            #region name
                            if (__dst.is_null(1 << 7))
                                goto case 16;
                            if (__dst.put(_name!, 16)) goto case 16;
                            return false;
                        case 16:
                            #endregion

                            if (!__dst.init_fields_nulls(_doc != null ?
                                                             1 :
                                                             0, 16))
                                return false;
                            if (_inline_doc != null) __dst.set_fields_nulls(1 << 1);

                            __dst.flush_fields_nulls();
                            goto case 17;
                        case 17:
                            #region doc
                            if (__dst.is_null(1))
                                goto case 18;
                            if (__dst.put(_doc!, 18)) goto case 18;
                            return false;
                        case 18:
                            #endregion
                            #region inline_doc
                            if (__dst.is_null(1 << 1))
                                goto case 19;
                            if (__dst.put(_inline_doc!, 19)) goto case 19;
                            return false;
                        case 19:
                        #endregion

                        default:
                            return true;
                    }
            }

            public interface Channel : AdHoc.Transmitter.BytesSrc
            {
                int AdHoc.Transmitter.BytesSrc.__id => __id_;
                public const int __id_ = -1;
                #region hostL
                public ushort _hostL { get; }
                #endregion
                #region hostL_transmitting_packs
                //Get a reference to the field data for existence and equality checks
                public object? _hostL_transmitting_packs();

                //Get the length of all item's fixed-length collections of the multidimensional field
                public int _hostL_transmitting_packs_len { get; }

                //Get the element of the collection
                public ushort _hostL_transmitting_packs(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                public struct _hostL_transmitting_packs_
                {
                    public const int ARRAY_LEN_MAX = 65535;
                }
                #endregion
                #region hostL_related_packs
                //Get a reference to the field data for existence and equality checks
                public object? _hostL_related_packs();

                //Get the length of all item's fixed-length collections of the multidimensional field
                public int _hostL_related_packs_len { get; }

                //Get the element of the collection
                public ushort _hostL_related_packs(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                public struct _hostL_related_packs_
                {
                    public const int ARRAY_LEN_MAX = 65535;
                }
                #endregion
                #region hostR
                public ushort _hostR { get; }
                #endregion
                #region hostR_transmitting_packs
                //Get a reference to the field data for existence and equality checks
                public object? _hostR_transmitting_packs();

                //Get the length of all item's fixed-length collections of the multidimensional field
                public int _hostR_transmitting_packs_len { get; }

                //Get the element of the collection
                public ushort _hostR_transmitting_packs(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                public struct _hostR_transmitting_packs_
                {
                    public const int ARRAY_LEN_MAX = 65535;
                }
                #endregion
                #region hostR_related_packs
                //Get a reference to the field data for existence and equality checks
                public object? _hostR_related_packs();

                //Get the length of all item's fixed-length collections of the multidimensional field
                public int _hostR_related_packs_len { get; }

                //Get the element of the collection
                public ushort _hostR_related_packs(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                public struct _hostR_related_packs_
                {
                    public const int ARRAY_LEN_MAX = 65535;
                }
                #endregion
                #region stages
                //Get a reference to the field data for existence and equality checks
                public object? _stages();

                //Get the length of all item's fixed-length collections of the multidimensional field
                public int _stages_len { get; }

                //Get the element of the collection
                public Agent.Project.Channel.Stage _stages(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                public struct _stages_
                {
                    public const int ARRAY_LEN_MAX = 4095;
                }
                #endregion
                #region uid
                public ushort _uid { get; }
                #endregion
                #region name
                public string? _name { get; }

                public struct _name_
                {
                    public const int STR_LEN_MAX = 255;
                }
                #endregion
                #region doc
                public string? _doc { get; }

                public struct _doc_
                { //Documentation with a maximum of 65,000 characters

                    public const int STR_LEN_MAX = 65000;
                }
                #endregion
                #region inline_doc
                public string? _inline_doc { get; }

                public struct _inline_doc_
                {
                    public const int STR_LEN_MAX = 255;
                }
                #endregion

                bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
                {
                    var __slot = __dst.slot!;
                    int __i = 0, __t = 0, __v = 0;
                    for (; ; )
                        switch (__slot.state)
                        {
                            case 0:
                                throw new NotSupportedException();
                            case 1:

                                if (!__dst.Allocate(6, 1))
                                    return false;
                                __dst.put((ushort)_hostL);
                                __dst.put((ushort)_hostR);
                                __dst.put((ushort)_uid);

                                goto case 2;
                            case 2:

                                if (!__dst.init_fields_nulls(_hostL_transmitting_packs() != null ?
                                                                 1 :
                                                                 0, 2))
                                    return false;
                                if (_hostL_related_packs() != null) __dst.set_fields_nulls(1 << 1);
                                if (_hostR_transmitting_packs() != null)
                                    __dst.set_fields_nulls(1 << 2);
                                if (_hostR_related_packs() != null)
                                    __dst.set_fields_nulls(1 << 3);
                                if (_stages() != null)
                                    __dst.set_fields_nulls(1 << 4);
                                if (_name != null)
                                    __dst.set_fields_nulls(1 << 5);
                                if (_doc != null)
                                    __dst.set_fields_nulls(1 << 6);
                                if (_inline_doc != null)
                                    __dst.set_fields_nulls(1 << 7);

                                __dst.flush_fields_nulls();
                                goto case 3;
                            case 3:
                                #region hostL_transmitting_packs
                                if (__dst.is_null(1))
                                    goto case 5;

                                if (__slot.index_max_1(_hostL_transmitting_packs_len) == 0)
                                {
                                    if (__dst.put_val(0, 2, 5)) goto case 5;
                                    return false;
                                }

                                if (!__dst.put_val((uint)__slot.index_max1, 2, 4)) return false;

                                goto case 4;
                            case 4:

                                if ((__v = __dst.remaining / 2) < (__i = __slot.index_max1 - __slot.index1))
                                {
                                    if (0 < __v)
                                    {
                                        __slot.index1 = __v += __i = __slot.index1;
                                        for (; __i < __v; __i++)
                                            __dst.put((ushort)_hostL_transmitting_packs(__dst, __slot, __i));
                                    }

                                    __dst.retry_at(4);
                                    return false;
                                }

                                __i += __v = __slot.index1;
                                for (; __v < __i; __v++) __dst.put((ushort)_hostL_transmitting_packs(__dst, __slot, __v));
                                goto case 5;
                            case 5:
                                #endregion
                                #region hostL_related_packs
                                if (__dst.is_null(1 << 1))
                                    goto case 7;

                                if (__slot.index_max_1(_hostL_related_packs_len) == 0)
                                {
                                    if (__dst.put_val(0, 2, 7)) goto case 7;
                                    return false;
                                }

                                if (!__dst.put_val((uint)__slot.index_max1, 2, 6)) return false;

                                goto case 6;
                            case 6:

                                if ((__v = __dst.remaining / 2) < (__i = __slot.index_max1 - __slot.index1))
                                {
                                    if (0 < __v)
                                    {
                                        __slot.index1 = __v += __i = __slot.index1;
                                        for (; __i < __v; __i++)
                                            __dst.put((ushort)_hostL_related_packs(__dst, __slot, __i));
                                    }

                                    __dst.retry_at(6);
                                    return false;
                                }

                                __i += __v = __slot.index1;
                                for (; __v < __i; __v++) __dst.put((ushort)_hostL_related_packs(__dst, __slot, __v));
                                goto case 7;
                            case 7:
                                #endregion
                                #region hostR_transmitting_packs
                                if (__dst.is_null(1 << 2))
                                    goto case 9;

                                if (__slot.index_max_1(_hostR_transmitting_packs_len) == 0)
                                {
                                    if (__dst.put_val(0, 2, 9)) goto case 9;
                                    return false;
                                }

                                if (!__dst.put_val((uint)__slot.index_max1, 2, 8)) return false;

                                goto case 8;
                            case 8:

                                if ((__v = __dst.remaining / 2) < (__i = __slot.index_max1 - __slot.index1))
                                {
                                    if (0 < __v)
                                    {
                                        __slot.index1 = __v += __i = __slot.index1;
                                        for (; __i < __v; __i++)
                                            __dst.put((ushort)_hostR_transmitting_packs(__dst, __slot, __i));
                                    }

                                    __dst.retry_at(8);
                                    return false;
                                }

                                __i += __v = __slot.index1;
                                for (; __v < __i; __v++) __dst.put((ushort)_hostR_transmitting_packs(__dst, __slot, __v));
                                goto case 9;
                            case 9:
                                #endregion
                                #region hostR_related_packs
                                if (__dst.is_null(1 << 3))
                                    goto case 11;

                                if (__slot.index_max_1(_hostR_related_packs_len) == 0)
                                {
                                    if (__dst.put_val(0, 2, 11)) goto case 11;
                                    return false;
                                }

                                if (!__dst.put_val((uint)__slot.index_max1, 2, 10)) return false;

                                goto case 10;
                            case 10:

                                if ((__v = __dst.remaining / 2) < (__i = __slot.index_max1 - __slot.index1))
                                {
                                    if (0 < __v)
                                    {
                                        __slot.index1 = __v += __i = __slot.index1;
                                        for (; __i < __v; __i++)
                                            __dst.put((ushort)_hostR_related_packs(__dst, __slot, __i));
                                    }

                                    __dst.retry_at(10);
                                    return false;
                                }

                                __i += __v = __slot.index1;
                                for (; __v < __i; __v++) __dst.put((ushort)_hostR_related_packs(__dst, __slot, __v));
                                goto case 11;
                            case 11:
                                #endregion
                                #region stages
                                if (__dst.is_null(1 << 4))
                                    goto case 13;

                                if (__slot.index_max_1(_stages_len) == 0)
                                {
                                    if (__dst.put_val(0, 2, 13)) goto case 13;
                                    return false;
                                }

                                if (!__dst.put_val((uint)__slot.index_max1, 2, 12)) return false;

                                goto case 12;
                            case 12:

                                for (var b = true; b;)
                                    if (!__dst.put_bytes(_stages(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ?
                                                                                                     12U :
                                                                                                     13U))
                                        return false;

                                goto case 13;
                            case 13:
                                #endregion
                                #region name
                                if (__dst.is_null(1 << 5))
                                    goto case 14;
                                if (__dst.put(_name!, 14)) goto case 14;
                                return false;
                            case 14:
                                #endregion
                                #region doc
                                if (__dst.is_null(1 << 6))
                                    goto case 15;
                                if (__dst.put(_doc!, 15)) goto case 15;
                                return false;
                            case 15:
                                #endregion
                                #region inline_doc
                                if (__dst.is_null(1 << 7))
                                    goto case 16;
                                if (__dst.put(_inline_doc!, 16)) goto case 16;
                                return false;
                            case 16:
                            #endregion

                            default:
                                return true;
                        }
                }

                public interface Stage : AdHoc.Transmitter.BytesSrc
                {
                    int AdHoc.Transmitter.BytesSrc.__id => __id_;
                    public const int __id_ = -2;
                    #region timeout
                    public ushort _timeout { get; }
                    #endregion
                    #region uid
                    public ushort _uid { get; }
                    #endregion
                    #region branchesL
                    //Get a reference to the field data for existence and equality checks
                    public object? _branchesL();

                    //Get the length of all item's fixed-length collections of the multidimensional field
                    public int _branchesL_len { get; }

                    //Get the element of the collection
                    public Agent.Project.Channel.Stage.Branch _branchesL(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                    public struct _branchesL_
                    {
                        public const int ARRAY_LEN_MAX = 4095;
                    }
                    #endregion
                    #region branchesR
                    //Get a reference to the field data for existence and equality checks
                    public object? _branchesR();

                    //Get the length of all item's fixed-length collections of the multidimensional field
                    public int _branchesR_len { get; }

                    //Get the element of the collection
                    public Agent.Project.Channel.Stage.Branch _branchesR(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                    public struct _branchesR_
                    {
                        public const int ARRAY_LEN_MAX = 4095;
                    }
                    #endregion
                    #region name
                    public string? _name { get; }

                    public struct _name_
                    {
                        public const int STR_LEN_MAX = 255;
                    }
                    #endregion
                    #region doc
                    public string? _doc { get; }

                    public struct _doc_
                    { //Documentation with a maximum of 65,000 characters

                        public const int STR_LEN_MAX = 65000;
                    }
                    #endregion
                    #region inline_doc
                    public string? _inline_doc { get; }

                    public struct _inline_doc_
                    {
                        public const int STR_LEN_MAX = 255;
                    }
                    #endregion

                    bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
                    {
                        var __slot = __dst.slot!;
                        int __i = 0, __t = 0, __v = 0;
                        for (; ; )
                            switch (__slot.state)
                            {
                                case 0:
                                    throw new NotSupportedException();
                                case 1:

                                    if (!__dst.Allocate(4, 1))
                                        return false;
                                    __dst.put((ushort)_timeout);
                                    __dst.put((ushort)_uid);

                                    goto case 2;
                                case 2:

                                    if (!__dst.init_fields_nulls(_branchesL() != null ?
                                                                     1 :
                                                                     0, 2))
                                        return false;
                                    if (_branchesR() != null) __dst.set_fields_nulls(1 << 1);
                                    if (_name != null)
                                        __dst.set_fields_nulls(1 << 2);
                                    if (_doc != null)
                                        __dst.set_fields_nulls(1 << 3);
                                    if (_inline_doc != null)
                                        __dst.set_fields_nulls(1 << 4);

                                    __dst.flush_fields_nulls();
                                    goto case 3;
                                case 3:
                                    #region branchesL
                                    if (__dst.is_null(1))
                                        goto case 5;

                                    if (__slot.index_max_1(_branchesL_len) == 0)
                                    {
                                        if (__dst.put_val(0, 2, 5)) goto case 5;
                                        return false;
                                    }

                                    if (!__dst.put_val((uint)__slot.index_max1, 2, 4)) return false;

                                    goto case 4;
                                case 4:

                                    for (var b = true; b;)
                                        if (!__dst.put_bytes(_branchesL(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ?
                                                                                                            4U :
                                                                                                            5U))
                                            return false;

                                    goto case 5;
                                case 5:
                                    #endregion
                                    #region branchesR
                                    if (__dst.is_null(1 << 1))
                                        goto case 7;

                                    if (__slot.index_max_1(_branchesR_len) == 0)
                                    {
                                        if (__dst.put_val(0, 2, 7)) goto case 7;
                                        return false;
                                    }

                                    if (!__dst.put_val((uint)__slot.index_max1, 2, 6)) return false;

                                    goto case 6;
                                case 6:

                                    for (var b = true; b;)
                                        if (!__dst.put_bytes(_branchesR(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ?
                                                                                                            6U :
                                                                                                            7U))
                                            return false;

                                    goto case 7;
                                case 7:
                                    #endregion
                                    #region name
                                    if (__dst.is_null(1 << 2))
                                        goto case 8;
                                    if (__dst.put(_name!, 8)) goto case 8;
                                    return false;
                                case 8:
                                    #endregion
                                    #region doc
                                    if (__dst.is_null(1 << 3))
                                        goto case 9;
                                    if (__dst.put(_doc!, 9)) goto case 9;
                                    return false;
                                case 9:
                                    #endregion
                                    #region inline_doc
                                    if (__dst.is_null(1 << 4))
                                        goto case 10;
                                    if (__dst.put(_inline_doc!, 10)) goto case 10;
                                    return false;
                                case 10:
                                #endregion

                                default:
                                    return true;
                            }
                    }

                    public enum Type : ushort
                    {
                        Exit = 65535,
                        None = 65534,
                        Remaining = 65533,
                    }

                    public interface Branch : AdHoc.Transmitter.BytesSrc
                    {
                        int AdHoc.Transmitter.BytesSrc.__id => __id_;
                        public const int __id_ = -3;
                        #region uid
                        public ushort _uid { get; }
                        #endregion
                        #region doc
                        public string? _doc { get; }

                        public struct _doc_
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
                        public ushort _packs(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                        public struct _packs_
                        {
                            public const int ARRAY_LEN_MAX = 65535;
                        }
                        #endregion

                        bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
                        {
                            var __slot = __dst.slot!;
                            int __i = 0, __t = 0, __v = 0;
                            for (; ; )
                                switch (__slot.state)
                                {
                                    case 0:
                                        throw new NotSupportedException();
                                    case 1:

                                        if (!__dst.Allocate(4, 1))
                                            return false;
                                        __dst.put((ushort)_uid);
                                        __dst.put((ushort)_goto_stage);

                                        goto case 2;
                                    case 2:

                                        if (!__dst.init_fields_nulls(_doc != null ?
                                                                         1 :
                                                                         0, 2))
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

            public interface Host : AdHoc.Transmitter.BytesSrc
            {
                int AdHoc.Transmitter.BytesSrc.__id => __id_;
                public const int __id_ = -4;
                #region uid
                public ushort _uid { get; }
                #endregion
                #region langs
                public Agent.Project.Host.Langs _langs { get; }
                #endregion
                #region pack_impl_hash_equal
                //Get a reference to the field data for existence and equality checks
                public object? _pack_impl_hash_equal();

                //Get the number of items in the Map at the specific location of the multidimensional field
                public int _pack_impl_hash_equal_len { get; }

                //Prepare and initialize before enumerating items in the Map at a specific location of the multidimensional field
                public void _pack_impl_hash_equal_Init(Context.Transmitter ctx, Context.Transmitter.Slot __slot);

                /*
                Value:  16 Least Significant Bits - hash_equal info
                           16 Most  Significant Bits - impl info

                */
                public ushort _pack_impl_hash_equal_NextItem_Key(Context.Transmitter ctx, Context.Transmitter.Slot __slot); //Pack -> impl_hash_equal

                //Get the value of the item's Value in the Map
                public uint _pack_impl_hash_equal_Val(Context.Transmitter ctx, Context.Transmitter.Slot __slot);

                public interface _pack_impl_hash_equal_
                { //Pack -> impl_hash_equal

                    public const int TYPE_LEN_MAX = 255;
                }
                #endregion
                #region default_impl_hash_equal
                public uint _default_impl_hash_equal { get; }
                #endregion
                #region field_impl
                //Get a reference to the field data for existence and equality checks
                public object? _field_impl();

                //Get the number of items in the Map at the specific location of the multidimensional field
                public int _field_impl_len { get; }

                //Prepare and initialize before enumerating items in the Map at a specific location of the multidimensional field
                public void _field_impl_Init(Context.Transmitter ctx, Context.Transmitter.Slot __slot);

                public ushort _field_impl_NextItem_Key(Context.Transmitter ctx, Context.Transmitter.Slot __slot); //Field -> impl

                //Get the value of the item's Value in the Map
                public Agent.Project.Host.Langs _field_impl_Val(Context.Transmitter ctx, Context.Transmitter.Slot __slot);

                public interface _field_impl_
                { //Field -> impl

                    public const int TYPE_LEN_MAX = 255;
                }
                #endregion
                #region packs
                //Get a reference to the field data for existence and equality checks
                public object? _packs();

                //Get the length of all item's fixed-length collections of the multidimensional field
                public int _packs_len { get; }

                //Get the element of the collection
                public ushort _packs(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                public struct _packs_
                { //Local constants or enums, referred to as packs, are declared within the scope of the current host.

                    public const int ARRAY_LEN_MAX = 65000;
                }
                #endregion
                #region name
                public string? _name { get; }

                public struct _name_
                {
                    public const int STR_LEN_MAX = 255;
                }
                #endregion
                #region doc
                public string? _doc { get; }

                public struct _doc_
                { //Documentation with a maximum of 65,000 characters

                    public const int STR_LEN_MAX = 65000;
                }
                #endregion
                #region inline_doc
                public string? _inline_doc { get; }

                public struct _inline_doc_
                {
                    public const int STR_LEN_MAX = 255;
                }
                #endregion

                bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
                {
                    var __slot = __dst.slot!;
                    int __i = 0, __t = 0, __v = 0;
                    for (; ; )
                        switch (__slot.state)
                        {
                            case 0:
                                throw new NotSupportedException();
                            case 1:

                                if (!__dst.Allocate(8, 1))
                                    return false;
                                __dst.put((ushort)_uid);
                                __dst.put((ushort)_langs);
                                __dst.put((uint)_default_impl_hash_equal);

                                goto case 2;
                            case 2:

                                if (!__dst.init_fields_nulls(_pack_impl_hash_equal() != null ?
                                                                 1 :
                                                                 0, 2))
                                    return false;
                                if (_field_impl() != null) __dst.set_fields_nulls(1 << 1);
                                if (_packs() != null)
                                    __dst.set_fields_nulls(1 << 2);
                                if (_name != null)
                                    __dst.set_fields_nulls(1 << 3);
                                if (_doc != null)
                                    __dst.set_fields_nulls(1 << 4);
                                if (_inline_doc != null)
                                    __dst.set_fields_nulls(1 << 5);

                                __dst.flush_fields_nulls();
                                goto case 3;
                            case 3:
                                #region pack_impl_hash_equal
                                if (__dst.is_null(1))
                                    goto case 8;

                                if (!__dst.Allocate(5, 3)) return false;
                                if (__slot.no_items(_pack_impl_hash_equal_len, 255)) goto case 8;
                                #region sending map info
                                __slot.put_info();
                                goto case 4;
                            case 4:
                                #endregion

                                _pack_impl_hash_equal_Init(__dst, __slot);
                                goto case 5;
                            #region sending key
                            case 5:
                                if (__dst.put((ushort)_pack_impl_hash_equal_NextItem_Key(__dst, __slot), 6))
                                    goto case 6;
                                return false;
                            case 6:
                                #endregion
                                #region sending value
                                if (__dst.put((uint)_pack_impl_hash_equal_Val(__dst, __slot), 7))
                                    goto case 7;
                                return false;
                            case 7:
                                #endregion
                                if (__slot.next_index1())
                                    goto case 5;

                                goto case 8;
                            case 8:
                                #endregion
                                #region field_impl
                                if (__dst.is_null(1 << 1))
                                    goto case 13;

                                if (!__dst.Allocate(5, 8)) return false;
                                if (__slot.no_items(_field_impl_len, 255)) goto case 13;
                                #region sending map info
                                __slot.put_info();
                                goto case 9;
                            case 9:
                                #endregion

                                _field_impl_Init(__dst, __slot);
                                goto case 10;
                            #region sending key
                            case 10:
                                if (__dst.put((ushort)_field_impl_NextItem_Key(__dst, __slot), 11))
                                    goto case 11;
                                return false;
                            case 11:
                                #endregion
                                #region sending value
                                if (__dst.put((ushort)_field_impl_Val(__dst, __slot), 12))
                                    goto case 12;
                                return false;
                            case 12:
                                #endregion
                                if (__slot.next_index1())
                                    goto case 10;

                                goto case 13;
                            case 13:
                                #endregion
                                #region packs
                                if (__dst.is_null(1 << 2))
                                    goto case 15;

                                if (__slot.index_max_1(_packs_len) == 0)
                                {
                                    if (__dst.put_val(0, 2, 15)) goto case 15;
                                    return false;
                                }

                                if (!__dst.put_val((uint)__slot.index_max1, 2, 14)) return false;

                                goto case 14;
                            case 14:

                                if ((__v = __dst.remaining / 2) < (__i = __slot.index_max1 - __slot.index1))
                                {
                                    if (0 < __v)
                                    {
                                        __slot.index1 = __v += __i = __slot.index1;
                                        for (; __i < __v; __i++)
                                            __dst.put((ushort)_packs(__dst, __slot, __i));
                                    }

                                    __dst.retry_at(14);
                                    return false;
                                }

                                __i += __v = __slot.index1;
                                for (; __v < __i; __v++) __dst.put((ushort)_packs(__dst, __slot, __v));
                                goto case 15;
                            case 15:
                                #endregion
                                #region name
                                if (__dst.is_null(1 << 3))
                                    goto case 16;
                                if (__dst.put(_name!, 16)) goto case 16;
                                return false;
                            case 16:
                                #endregion
                                #region doc
                                if (__dst.is_null(1 << 4))
                                    goto case 17;
                                if (__dst.put(_doc!, 17)) goto case 17;
                                return false;
                            case 17:
                                #endregion
                                #region inline_doc
                                if (__dst.is_null(1 << 5))
                                    goto case 18;
                                if (__dst.put(_inline_doc!, 18)) goto case 18;
                                return false;
                            case 18:
                            #endregion

                            default:
                                return true;
                        }
                }

                [Flags]
                public enum Langs : ushort
                {
                    All = 65535,
                    InCPP = 1,
                    InCS = 4,
                    InGO = 16,
                    InJAVA = 8,
                    InRS = 2,
                    InTS = 32,
                }

                public interface Pack : AdHoc.Transmitter.BytesSrc
                {
                    int AdHoc.Transmitter.BytesSrc.__id => __id_;
                    public const int __id_ = -5;
                    #region id
                    public ushort _id { get; }
                    #endregion
                    #region parent
                    public ushort? _parent { get; }
                    #endregion
                    #region uid
                    public ushort _uid { get; }
                    #endregion
                    #region nested_max
                    public ushort? _nested_max { get; }
                    #endregion
                    #region referred
                    public bool _referred { get; }
                    #endregion
                    #region fields
                    //Get a reference to the field data for existence and equality checks
                    public object? _fields();

                    //Get the length of all item's fixed-length collections of the multidimensional field
                    public int _fields_len { get; }

                    //Get the element of the collection
                    public int _fields(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                    public struct _fields_
                    {
                        public const int ARRAY_LEN_MAX = 65000;
                    }
                    #endregion
                    #region static_fields
                    //Get a reference to the field data for existence and equality checks
                    public object? _static_fields();

                    //Get the length of all item's fixed-length collections of the multidimensional field
                    public int _static_fields_len { get; }

                    //Get the element of the collection
                    public int _static_fields(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                    public struct _static_fields_
                    {
                        public const int ARRAY_LEN_MAX = 65000;
                    }
                    #endregion
                    #region name
                    public string? _name { get; }

                    public struct _name_
                    {
                        public const int STR_LEN_MAX = 255;
                    }
                    #endregion
                    #region doc
                    public string? _doc { get; }

                    public struct _doc_
                    { //Documentation with a maximum of 65,000 characters

                        public const int STR_LEN_MAX = 65000;
                    }
                    #endregion
                    #region inline_doc
                    public string? _inline_doc { get; }

                    public struct _inline_doc_
                    {
                        public const int STR_LEN_MAX = 255;
                    }
                    #endregion

                    bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
                    {
                        var __slot = __dst.slot!;
                        int __i = 0, __t = 0, __v = 0;
                        for (; ; )
                            switch (__slot.state)
                            {
                                case 0:
                                    throw new NotSupportedException();
                                case 1:

                                    if (!__dst.Allocate(4, 1))
                                        return false;
                                    __dst.put((ushort)_id);
                                    __dst.put((ushort)_uid);

                                    goto case 2;
                                case 2:

                                    if (!__dst.init_bits(1, 2))
                                        return false;
                                    #region referred
                                    __dst.put(_referred);
                                    #endregion

                                    goto case 3;
                                case 3:

                                    __dst.end_bits();
                                    goto case 4;
                                case 4:

                                    if (!__dst.init_fields_nulls(_parent != null ?
                                                                     1 :
                                                                     0, 4))
                                        return false;
                                    if (_nested_max != null) __dst.set_fields_nulls(1 << 1);
                                    if (_fields() != null)
                                        __dst.set_fields_nulls(1 << 2);
                                    if (_static_fields() != null)
                                        __dst.set_fields_nulls(1 << 3);
                                    if (_name != null)
                                        __dst.set_fields_nulls(1 << 4);
                                    if (_doc != null)
                                        __dst.set_fields_nulls(1 << 5);
                                    if (_inline_doc != null)
                                        __dst.set_fields_nulls(1 << 6);

                                    __dst.flush_fields_nulls();
                                    goto case 5;
                                case 5:
                                    #region parent
                                    if (__dst.is_null(1))
                                        goto case 6;
                                    if (__dst.put((ushort)_parent!.Value, 6)) goto case 6;
                                    return false;
                                case 6:
                                    #endregion
                                    #region nested_max
                                    if (__dst.is_null(1 << 1))
                                        goto case 7;
                                    if (__dst.put((ushort)_nested_max!.Value, 7)) goto case 7;
                                    return false;
                                case 7:
                                    #endregion
                                    #region fields
                                    if (__dst.is_null(1 << 2))
                                        goto case 9;

                                    if (__slot.index_max_1(_fields_len) == 0)
                                    {
                                        if (__dst.put_val(0, 2, 9)) goto case 9;
                                        return false;
                                    }

                                    if (!__dst.put_val((uint)__slot.index_max1, 2, 8)) return false;

                                    goto case 8;
                                case 8:

                                    if ((__v = __dst.remaining / 4) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __v)
                                        {
                                            __slot.index1 = __v += __i = __slot.index1;
                                            for (; __i < __v; __i++)
                                                __dst.put((int)_fields(__dst, __slot, __i));
                                        }

                                        __dst.retry_at(8);
                                        return false;
                                    }

                                    __i += __v = __slot.index1;
                                    for (; __v < __i; __v++) __dst.put((int)_fields(__dst, __slot, __v));
                                    goto case 9;
                                case 9:
                                    #endregion
                                    #region static_fields
                                    if (__dst.is_null(1 << 3))
                                        goto case 11;

                                    if (__slot.index_max_1(_static_fields_len) == 0)
                                    {
                                        if (__dst.put_val(0, 2, 11)) goto case 11;
                                        return false;
                                    }

                                    if (!__dst.put_val((uint)__slot.index_max1, 2, 10)) return false;

                                    goto case 10;
                                case 10:

                                    if ((__v = __dst.remaining / 4) < (__i = __slot.index_max1 - __slot.index1))
                                    {
                                        if (0 < __v)
                                        {
                                            __slot.index1 = __v += __i = __slot.index1;
                                            for (; __i < __v; __i++)
                                                __dst.put((int)_static_fields(__dst, __slot, __i));
                                        }

                                        __dst.retry_at(10);
                                        return false;
                                    }

                                    __i += __v = __slot.index1;
                                    for (; __v < __i; __v++) __dst.put((int)_static_fields(__dst, __slot, __v));
                                    goto case 11;
                                case 11:
                                    #endregion
                                    #region name
                                    if (__dst.is_null(1 << 4))
                                        goto case 12;
                                    if (__dst.put(_name!, 12)) goto case 12;
                                    return false;
                                case 12:
                                    #endregion
                                    #region doc
                                    if (__dst.is_null(1 << 5))
                                        goto case 13;
                                    if (__dst.put(_doc!, 13)) goto case 13;
                                    return false;
                                case 13:
                                    #endregion
                                    #region inline_doc
                                    if (__dst.is_null(1 << 6))
                                        goto case 14;
                                    if (__dst.put(_inline_doc!, 14)) goto case 14;
                                    return false;
                                case 14:
                                #endregion

                                default:
                                    return true;
                            }
                    }

                    public interface Field : AdHoc.Transmitter.BytesSrc
                    {
                        int AdHoc.Transmitter.BytesSrc.__id => __id_;
                        public const int __id_ = -6;
                        #region dims
                        //Get a reference to the field data for existence and equality checks
                        public object? _dims();

                        //Get the length of all item's fixed-length collections of the multidimensional field
                        public int _dims_len { get; }

                        //Get the element of the collection
                        public int _dims(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                        public struct _dims_
                        { //Dimensions

                            public const int ARRAY_LEN_MAX = 32;
                        }
                        #endregion
                        #region map_set_len
                        public uint? _map_set_len { get; }
                        #endregion
                        #region map_set_array
                        public uint? _map_set_array { get; }
                        #endregion
                        #region exT
                        public ushort _exT { get; }
                        #endregion
                        #region exT_len
                        public uint? _exT_len { get; }
                        #endregion
                        #region exT_array
                        public uint? _exT_array { get; }
                        #endregion
                        #region inT
                        public ushort? _inT { get; }
                        #endregion
                        #region min_value
                        public long? _min_value { get; }
                        #endregion
                        #region max_value
                        public long? _max_value { get; }
                        #endregion
                        #region dir
                        public sbyte? _dir { get; }

                        public struct _dir_
                        {
                            public const sbyte MIN = -1;
                            public const sbyte MAX = 0x1;
                        }
                        #endregion
                        #region min_valueD
                        public double? _min_valueD { get; }
                        #endregion
                        #region max_valueD
                        public double? _max_valueD { get; }
                        #endregion
                        #region bits
                        public byte? _bits { get; }

                        public struct _bits_
                        { //Can store/transfer a value in less than 7 bits

                            public const byte MIN = 0x1;
                            public const byte MAX = 0x7;
                        }
                        #endregion
                        #region null_value
                        public byte? _null_value { get; }
                        #endregion
                        #region exTV
                        public ushort? _exTV { get; }
                        #endregion
                        #region exTV_len
                        public uint? _exTV_len { get; }
                        #endregion
                        #region exTV_array
                        public uint? _exTV_array { get; }
                        #endregion
                        #region inTV
                        public ushort? _inTV { get; }
                        #endregion
                        #region min_valueV
                        public long? _min_valueV { get; }
                        #endregion
                        #region max_valueV
                        public long? _max_valueV { get; }
                        #endregion
                        #region dirV
                        public sbyte? _dirV { get; }

                        public struct _dirV_
                        {
                            public const sbyte MIN = -1;
                            public const sbyte MAX = 0x1;
                        }
                        #endregion
                        #region min_valueDV
                        public double? _min_valueDV { get; }
                        #endregion
                        #region max_valueDV
                        public double? _max_valueDV { get; }
                        #endregion
                        #region bitsV
                        public byte? _bitsV { get; }

                        public struct _bitsV_
                        { //Can store/transfer a value in less than 7 bits

                            public const byte MIN = 0x1;
                            public const byte MAX = 0x7;
                        }
                        #endregion
                        #region null_valueV
                        public byte? _null_valueV { get; }
                        #endregion
                        #region value_int
                        public long? _value_int { get; }
                        #endregion
                        #region value_double
                        public double? _value_double { get; }
                        #endregion
                        #region value_string
                        public string? _value_string { get; }

                        public struct _value_string_
                        { //Constant value

                            public const int STR_LEN_MAX = 1000;
                        }
                        #endregion
                        #region array
                        //Get a reference to the field data for existence and equality checks
                        public object? _array();

                        //Get the length of all item's fixed-length collections of the multidimensional field
                        public int _array_len { get; }

                        //Get the element of the collection
                        public string _array(Context.Transmitter ctx, Context.Transmitter.Slot __slot, int item);

                        public struct _array_
                        { //Constant array values

                            public const int STR_LEN_MAX = 1000;
                            public const int ARRAY_LEN_MAX = 255;
                        }
                        #endregion
                        #region name
                        public string? _name { get; }

                        public struct _name_
                        {
                            public const int STR_LEN_MAX = 255;
                        }
                        #endregion
                        #region doc
                        public string? _doc { get; }

                        public struct _doc_
                        { //Documentation with a maximum of 65,000 characters

                            public const int STR_LEN_MAX = 65000;
                        }
                        #endregion
                        #region inline_doc
                        public string? _inline_doc { get; }

                        public struct _inline_doc_
                        {
                            public const int STR_LEN_MAX = 255;
                        }
                        #endregion

                        bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
                        {
                            var __slot = __dst.slot!;
                            int __i = 0, __t = 0, __v = 0;
                            for (; ; )
                                switch (__slot.state)
                                {
                                    case 0:
                                        throw new NotSupportedException();
                                    case 1:

                                        if (!__dst.Allocate(2, 1))
                                            return false;
                                        __dst.put((ushort)_exT);

                                        goto case 2;
                                    case 2:

                                        if (!__dst.init_bits(2, 2))
                                            return false;
                                        #region dir
                                        __dst.put_bits((int)(_dir == null ?
                                                                 2 :
                                                                 (int)((sbyte)(_dir!.Value + 0x1))), 2);
                                        #endregion
                                        #region bits
                                        __dst.put_bits((int)(_bits == null ?
                                                                 0 :
                                                                 (int)(_bits!.Value)), 3);
                                        #endregion
                                        #region dirV
                                        __dst.put_bits((int)(_dirV == null ?
                                                                 2 :
                                                                 (int)((sbyte)(_dirV!.Value + 0x1))), 2);
                                        #endregion
                                        #region bitsV
                                        __dst.put_bits((int)(_bitsV == null ?
                                                                 0 :
                                                                 (int)(_bitsV!.Value)), 3);
                                        #endregion

                                        goto case 3;
                                    case 3:

                                        __dst.end_bits();
                                        goto case 4;
                                    case 4:

                                        if (!__dst.init_fields_nulls(_dims() != null ?
                                                                         1 :
                                                                         0, 4))
                                            return false;
                                        if (_map_set_len != null) __dst.set_fields_nulls(1 << 1);
                                        if (_map_set_array != null)
                                            __dst.set_fields_nulls(1 << 2);
                                        if (_exT_len != null)
                                            __dst.set_fields_nulls(1 << 3);
                                        if (_exT_array != null)
                                            __dst.set_fields_nulls(1 << 4);
                                        if (_inT != null)
                                            __dst.set_fields_nulls(1 << 5);
                                        if (_min_value != null)
                                            __dst.set_fields_nulls(1 << 6);
                                        if (_max_value != null)
                                            __dst.set_fields_nulls(1 << 7);

                                        __dst.flush_fields_nulls();
                                        goto case 5;
                                    case 5:
                                        #region dims
                                        if (__dst.is_null(1))
                                            goto case 7;

                                        if (__slot.index_max_1(_dims_len) == 0)
                                        {
                                            if (__dst.put_val(0, 1, 7)) goto case 7;
                                            return false;
                                        }

                                        if (!__dst.put_val((uint)__slot.index_max1, 1, 6)) return false;

                                        goto case 6;
                                    case 6:

                                        if ((__v = __dst.remaining / 4) < (__i = __slot.index_max1 - __slot.index1))
                                        {
                                            if (0 < __v)
                                            {
                                                __slot.index1 = __v += __i = __slot.index1;
                                                for (; __i < __v; __i++)
                                                    __dst.put((int)_dims(__dst, __slot, __i));
                                            }

                                            __dst.retry_at(6);
                                            return false;
                                        }

                                        __i += __v = __slot.index1;
                                        for (; __v < __i; __v++) __dst.put((int)_dims(__dst, __slot, __v));
                                        goto case 7;
                                    case 7:
                                        #endregion
                                        #region map_set_len
                                        if (__dst.is_null(1 << 1))
                                            goto case 8;
                                        if (__dst.put((uint)_map_set_len!.Value, 8)) goto case 8;
                                        return false;
                                    case 8:
                                        #endregion
                                        #region map_set_array
                                        if (__dst.is_null(1 << 2))
                                            goto case 9;
                                        if (__dst.put((uint)_map_set_array!.Value, 9)) goto case 9;
                                        return false;
                                    case 9:
                                        #endregion
                                        #region exT_len
                                        if (__dst.is_null(1 << 3))
                                            goto case 10;
                                        if (__dst.put((uint)_exT_len!.Value, 10)) goto case 10;
                                        return false;
                                    case 10:
                                        #endregion
                                        #region exT_array
                                        if (__dst.is_null(1 << 4))
                                            goto case 11;
                                        if (__dst.put((uint)_exT_array!.Value, 11)) goto case 11;
                                        return false;
                                    case 11:
                                        #endregion
                                        #region inT
                                        if (__dst.is_null(1 << 5))
                                            goto case 12;
                                        if (__dst.put((ushort)_inT!.Value, 12)) goto case 12;
                                        return false;
                                    case 12:
                                        #endregion
                                        #region min_value
                                        if (__dst.is_null(1 << 6))
                                            goto case 13;
                                        if (__dst.put((long)_min_value!.Value, 13)) goto case 13;
                                        return false;
                                    case 13:
                                        #endregion
                                        #region max_value
                                        if (__dst.is_null(1 << 7))
                                            goto case 14;
                                        if (__dst.put((long)_max_value!.Value, 14)) goto case 14;
                                        return false;
                                    case 14:
                                        #endregion

                                        if (!__dst.init_fields_nulls(_min_valueD != null ?
                                                                         1 :
                                                                         0, 14))
                                            return false;
                                        if (_max_valueD != null) __dst.set_fields_nulls(1 << 1);
                                        if (_null_value != null) __dst.set_fields_nulls(1 << 2);
                                        if (_exTV != null) __dst.set_fields_nulls(1 << 3);
                                        if (_exTV_len != null) __dst.set_fields_nulls(1 << 4);
                                        if (_exTV_array != null) __dst.set_fields_nulls(1 << 5);
                                        if (_inTV != null) __dst.set_fields_nulls(1 << 6);
                                        if (_min_valueV != null) __dst.set_fields_nulls(1 << 7);

                                        __dst.flush_fields_nulls();
                                        goto case 15;
                                    case 15:
                                        #region min_valueD
                                        if (__dst.is_null(1))
                                            goto case 16;
                                        if (__dst.put((ulong)BitConverter.DoubleToUInt64Bits(_min_valueD!.Value), 16)) goto case 16;
                                        return false;
                                    case 16:
                                        #endregion
                                        #region max_valueD
                                        if (__dst.is_null(1 << 1))
                                            goto case 17;
                                        if (__dst.put((ulong)BitConverter.DoubleToUInt64Bits(_max_valueD!.Value), 17)) goto case 17;
                                        return false;
                                    case 17:
                                        #endregion
                                        #region null_value
                                        if (__dst.is_null(1 << 2))
                                            goto case 18;
                                        if (__dst.put((byte)_null_value!.Value, 18)) goto case 18;
                                        return false;
                                    case 18:
                                        #endregion
                                        #region exTV
                                        if (__dst.is_null(1 << 3))
                                            goto case 19;
                                        if (__dst.put((ushort)_exTV!.Value, 19)) goto case 19;
                                        return false;
                                    case 19:
                                        #endregion
                                        #region exTV_len
                                        if (__dst.is_null(1 << 4))
                                            goto case 20;
                                        if (__dst.put((uint)_exTV_len!.Value, 20)) goto case 20;
                                        return false;
                                    case 20:
                                        #endregion
                                        #region exTV_array
                                        if (__dst.is_null(1 << 5))
                                            goto case 21;
                                        if (__dst.put((uint)_exTV_array!.Value, 21)) goto case 21;
                                        return false;
                                    case 21:
                                        #endregion
                                        #region inTV
                                        if (__dst.is_null(1 << 6))
                                            goto case 22;
                                        if (__dst.put((ushort)_inTV!.Value, 22)) goto case 22;
                                        return false;
                                    case 22:
                                        #endregion
                                        #region min_valueV
                                        if (__dst.is_null(1 << 7))
                                            goto case 23;
                                        if (__dst.put((long)_min_valueV!.Value, 23)) goto case 23;
                                        return false;
                                    case 23:
                                        #endregion

                                        if (!__dst.init_fields_nulls(_max_valueV != null ?
                                                                         1 :
                                                                         0, 23))
                                            return false;
                                        if (_min_valueDV != null) __dst.set_fields_nulls(1 << 1);
                                        if (_max_valueDV != null) __dst.set_fields_nulls(1 << 2);
                                        if (_null_valueV != null) __dst.set_fields_nulls(1 << 3);
                                        if (_value_int != null) __dst.set_fields_nulls(1 << 4);
                                        if (_value_double != null) __dst.set_fields_nulls(1 << 5);
                                        if (_value_string != null) __dst.set_fields_nulls(1 << 6);
                                        if (_array() != null) __dst.set_fields_nulls(1 << 7);

                                        __dst.flush_fields_nulls();
                                        goto case 24;
                                    case 24:
                                        #region max_valueV
                                        if (__dst.is_null(1))
                                            goto case 25;
                                        if (__dst.put((long)_max_valueV!.Value, 25)) goto case 25;
                                        return false;
                                    case 25:
                                        #endregion
                                        #region min_valueDV
                                        if (__dst.is_null(1 << 1))
                                            goto case 26;
                                        if (__dst.put((ulong)BitConverter.DoubleToUInt64Bits(_min_valueDV!.Value), 26)) goto case 26;
                                        return false;
                                    case 26:
                                        #endregion
                                        #region max_valueDV
                                        if (__dst.is_null(1 << 2))
                                            goto case 27;
                                        if (__dst.put((ulong)BitConverter.DoubleToUInt64Bits(_max_valueDV!.Value), 27)) goto case 27;
                                        return false;
                                    case 27:
                                        #endregion
                                        #region null_valueV
                                        if (__dst.is_null(1 << 3))
                                            goto case 28;
                                        if (__dst.put((byte)_null_valueV!.Value, 28)) goto case 28;
                                        return false;
                                    case 28:
                                        #endregion
                                        #region value_int
                                        if (__dst.is_null(1 << 4))
                                            goto case 29;
                                        if (__dst.put((long)_value_int!.Value, 29)) goto case 29;
                                        return false;
                                    case 29:
                                        #endregion
                                        #region value_double
                                        if (__dst.is_null(1 << 5))
                                            goto case 30;
                                        if (__dst.put((ulong)BitConverter.DoubleToUInt64Bits(_value_double!.Value), 30)) goto case 30;
                                        return false;
                                    case 30:
                                        #endregion
                                        #region value_string
                                        if (__dst.is_null(1 << 6))
                                            goto case 31;
                                        if (__dst.put(_value_string!, 31)) goto case 31;
                                        return false;
                                    case 31:
                                        #endregion
                                        #region array
                                        if (__dst.is_null(1 << 7))
                                            goto case 33;

                                        if (__slot.index_max_1(_array_len) == 0)
                                        {
                                            if (__dst.put_val(0, 1, 33)) goto case 33;
                                            return false;
                                        }

                                        if (!__dst.put_val((uint)__slot.index_max1, 1, 32)) return false;

                                        goto case 32;
                                    case 32:

                                        for (var b = true; b;)
                                            if (!__dst.put(_array(__dst, __slot, __slot.index1)!, (b = __slot.next_index1()) ?
                                                                                                      32U :
                                                                                                      33U))
                                                return false;

                                        goto case 33;
                                    case 33:
                                        #endregion

                                        if (!__dst.init_fields_nulls(_name != null ?
                                                                         1 :
                                                                         0, 33))
                                            return false;
                                        if (_doc != null) __dst.set_fields_nulls(1 << 1);
                                        if (_inline_doc != null) __dst.set_fields_nulls(1 << 2);

                                        __dst.flush_fields_nulls();
                                        goto case 34;
                                    case 34:
                                        #region name
                                        if (__dst.is_null(1))
                                            goto case 35;
                                        if (__dst.put(_name!, 35)) goto case 35;
                                        return false;
                                    case 35:
                                        #endregion
                                        #region doc
                                        if (__dst.is_null(1 << 1))
                                            goto case 36;
                                        if (__dst.put(_doc!, 36)) goto case 36;
                                        return false;
                                    case 36:
                                        #endregion
                                        #region inline_doc
                                        if (__dst.is_null(1 << 2))
                                            goto case 37;
                                        if (__dst.put(_inline_doc!, 37)) goto case 37;
                                        return false;
                                    case 37:
                                    #endregion

                                    default:
                                        return true;
                                }
                        }

                        public enum DataType : ushort
                        {
                            t_binary = 65529,
                            t_bool = 65531,
                            t_char = 65525,
                            t_constants = 65535,
                            t_double = 65519,
                            t_enum_exp = 65533,
                            t_enum_sw = 65534,
                            t_flags = 65532,
                            t_float = 65520,
                            t_int16 = 65527,
                            t_int32 = 65524,
                            t_int64 = 65522,
                            t_int8 = 65530,
                            t_map = 65517,
                            t_set = 65516,
                            t_string = 65518,
                            t_subpack = 65514,
                            t_uint16 = 65526,
                            t_uint32 = 65523,
                            t_uint64 = 65521,
                            t_uint8 = 65528,
                        }
                    }
                }
            }
        }

        public struct Show_Code : IEquatable<Show_Code>
        { //Request to show entity in editor

            public int __id => __id_;
            public const int __id_ = 11;

            public uint Value = 0x0;
            public Show_Code() { }
            public Show_Code(uint src) => Value = src;

            public ushort idx
            {
                get
                {
                    var _inT = (Value & 0xFFFFU);
                    return (ushort)_inT;
                }
                set => Value = (uint)(Value & 0x7_0000UL | (ulong)(value));
            }

            public Agent.Entity.Type tYpe
            {
                get
                {
                    var _inT = (Value >> 16 & 0x7U);
                    return (Agent.Entity.Type)_inT;
                }
                set => Value = (uint)(Value & 0xFFFFUL | (ulong)((byte)value) << 16);
            }

            public class Handler : ObserverCommunication.Receiver.Receivable, AdHoc.Transmitter.BytesSrc
            {
                public int __id => 11;
                public static readonly Handler ONE = new();

                void ObserverCommunication.Receiver.Receivable.Received(ObserverCommunication.Receiver via) => ObserverCommunication.Receiver.onReceive.Received(via, (Show_Code)via.u8);

                bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
                {
                    var __slot = __dst.slot!;
                    ulong _bits;
                    switch (__slot.state)
                    {
                        case 0:
                            __dst.pull_value();
                            if (__dst.put_val(__id_, 1, 1))
                                goto case 1;
                            return false;
                        case 1:
                            return __dst.put_val(__dst.u8, 3, 2);
                        default: return true;
                    }
                }

                bool AdHoc.Receiver.BytesDst.__put_bytes(AdHoc.Receiver __src)
                {
                    var __slot = __src.slot!;
                    switch (__slot.state)
                    {
                        case 0:
                            return __src.try_get8(3, 1);

                        default:
                            return true;
                    }
                }
            }

            public bool Equals(Show_Code other) => Value == other.Value;

            public static bool operator ==(Show_Code? a, Show_Code? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.Value == b!.Value.Value);
            public static bool operator !=(Show_Code? a, Show_Code? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.Value != b!.Value.Value);

            public override bool Equals(object? other) => other is Show_Code p && p.Value == Value;
            public override int GetHashCode() => Value.GetHashCode();

            public static implicit operator uint(Show_Code a) => a.Value;
            public static implicit operator Show_Code(uint a) => new Show_Code(a);

            public static implicit operator ulong(Show_Code a) => (ulong)(a.Value);
            public static implicit operator Show_Code(ulong a) => new Show_Code((uint)a);

            public struct Nullable : IEquatable<Nullable>
            {
                public Nullable() { }
                public Nullable(uint value) => this.value = value;
                public Nullable(Show_Code value) => Value = value;

                public uint value = NULL;

                public Show_Code Value { get => new Show_Code(value); set => this.value = value.Value; }

                public bool hasValue => value != NULL;
                public void to_null() => value = NULL;

                public const uint NULL = (uint)0x6_0000;

                public bool Equals(Nullable other) => value == other.value;

                public static bool operator ==(Nullable? a, Nullable? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.value == b!.Value.value);
                public static bool operator !=(Nullable? a, Nullable? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.value != b!.Value.value);

                public static bool operator ==(Nullable a, Show_Code b) => a.value == b;
                public static bool operator !=(Nullable a, Show_Code b) => a.value != b;
                public static bool operator ==(Show_Code a, Nullable b) => a == b.value;
                public static bool operator !=(Show_Code a, Nullable b) => a != b.value;
                public override bool Equals(object? other) => other is Nullable p && p.value == value;
                public override int GetHashCode() => value.GetHashCode();
                public static implicit operator uint(Nullable a) => a.value;
                public static implicit operator Nullable(uint a) => new Nullable(a);
                public static implicit operator Nullable(Show_Code a) => new Nullable(a);
                public static implicit operator Nullable(uint? a) => a ?? NULL;
            }
        }

        public struct Version : IEquatable<Version>
        {
            public int __id => __id_;
            public const int __id_ = 13;

            public uint uid = 0; //Unique identifier for the version

            public Version() { }
            public Version(uint src) => uid = src;

            public class Handler : AdHoc.Receiver.BytesDst, Communication.Transmitter.Transmittable
            {
                public int __id => 13;
                public static readonly Handler ONE = new();

                void Communication.Transmitter.Transmittable.Sent(Communication.Transmitter via) => Communication.Transmitter.onTransmit.Sent(via, (Version)via.u8);

                bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
                {
                    var __slot = __dst.slot!;
                    ulong _bits;
                    switch (__slot.state)
                    {
                        case 0:
                            __dst.pull_value();
                            if (__dst.put_val(__id_, 1, 1))
                                goto case 1;
                            return false;
                        case 1:
                            return __dst.put((uint)__dst.u8, 123);

                        default: return true;
                    }
                }

                bool AdHoc.Receiver.BytesDst.__put_bytes(AdHoc.Receiver __src)
                {
                    var __slot = __src.slot!;
                    switch (__slot.state)
                    {
                        case 0:
                            return __src.get_uint_u8(123);

                        default:
                            return true;
                    }
                }
            }

            public bool Equals(Version other) => uid == other.uid;

            public static bool operator ==(Version? a, Version? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.uid == b!.Value.uid);
            public static bool operator !=(Version? a, Version? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.uid != b!.Value.uid);

            public override bool Equals(object? other) => other is Version p && p.uid == uid;
            public override int GetHashCode() => uid.GetHashCode();

            public static implicit operator uint(Version a) => a.uid;
            public static implicit operator Version(uint a) => new Version(a);

            public static implicit operator ulong(Version a) => (ulong)(a.uid);
            public static implicit operator Version(ulong a) => new Version((uint)a);

            public struct Nullable : IEquatable<Nullable>
            {
                public Nullable() { }
                public Nullable(ulong value) => this.value = value;
                public Nullable(Version value) => Value = value;

                public ulong value = NULL;

                public Version Value { get => new Version((uint)(value)); set => this.value = (ulong)value.uid; }

                public bool hasValue => value != NULL;
                public void to_null() => value = NULL;

                public const ulong NULL = (ulong)0x1_0000_0000;

                public bool Equals(Nullable other) => value == other.value;

                public static bool operator ==(Nullable? a, Nullable? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.value == b!.Value.value);
                public static bool operator !=(Nullable? a, Nullable? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.value != b!.Value.value);

                public static bool operator ==(Nullable a, Version b) => a.value == (ulong)b.uid;
                public static bool operator !=(Nullable a, Version b) => a.value != (ulong)b.uid;
                public static bool operator ==(Version a, Nullable b) => (ulong)a.uid == b.value;
                public static bool operator !=(Version a, Nullable b) => (ulong)a.uid != b.value;
                public override bool Equals(object? other) => other is Nullable p && p.value == value;
                public override int GetHashCode() => value.GetHashCode();
                public static implicit operator ulong(Nullable a) => a.value;
                public static implicit operator Nullable(ulong a) => new Nullable(a);
                public static implicit operator Nullable(Version a) => new Nullable(a);
            }
        }

        public class Info : IEquatable<Info>, Communication.Receiver.Receivable
        {
            public int __id => __id_;
            public const int __id_ = 0;
            void Communication.Receiver.Receivable.Received(Communication.Receiver via) => Communication.Receiver.onReceive.Received(via, this);
            #region task
            public string? task { get; set; } = null;

            public struct task_
            {
                public const int STR_LEN_MAX = 255;
            }
            #endregion
            #region info
            public string? info { get; set; } = null; //Information with a maximum of 65,000 characters

            public struct info_
            { //Information with a maximum of 65,000 characters

                public const int STR_LEN_MAX = 65000;
            }
            #endregion

            public int GetHashCode
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

            bool AdHoc.Receiver.BytesDst.__put_bytes(AdHoc.Receiver __src)
            {
                var __slot = __src.slot!;
                int __i = 0, __t = 0, __v = 0;
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

                            if (__src.try_get_string(Agent.Info.task_.STR_LEN_MAX, 2)) goto case 2;
                            return false;
                        case 2:
                            task = __src.get_string();
                            goto case 3;
                        case 3:
                            #endregion
                            #region info
                            if (__src.is_null(1 << 1))
                                goto case 5;

                            if (__src.try_get_string(Agent.Info.info_.STR_LEN_MAX, 4)) goto case 4;
                            return false;
                        case 4:
                            info = __src.get_string();
                            goto case 5;
                        case 5:
                        #endregion

                        default:
                            return true;
                    }
            }
        }

        public class Result : IEquatable<Result>, Communication.Receiver.Receivable
        {
            public int __id => __id_;
            public const int __id_ = 1;
            void Communication.Receiver.Receivable.Received(Communication.Receiver via) => Communication.Receiver.onReceive.Received(via, this);
            #region task
            public string? task { get; set; } = null;

            public struct task_
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

                _result = AdHoc.Resize<byte>(__src, Math.Min(__src!.Length, Agent.Result.result_.ARRAY_LEN_MAX), 0);
                ;
            }

            public struct result_
            { //3 megabytes compressed binary

                public const int ARRAY_LEN_MAX = 30000000;
            }
            #endregion
            #region info
            public string? info { get; set; } = null; //Information with a maximum of 65,000 characters

            public struct info_
            { //Information with a maximum of 65,000 characters

                public const int STR_LEN_MAX = 65000;
            }
            #endregion

            public int GetHashCode
            {
                get
                {
                    var _hash = 3001003L;
                    #region task
                    _hash = HashCode.Combine(_hash, task);
                    #endregion
                    #region result
                    if (_result != null)
                        for (int __i = 0, MAX = result_len; __i < MAX; __i++)
                            _hash = HashCode.Combine(_hash, _result[__i]);
                    #endregion
                    #region info
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
                        for (int __i = 0, MAX = result_len; __i < MAX; __i++)
                            if (_result[__i] != _pack._result[__i])
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

            bool AdHoc.Receiver.BytesDst.__put_bytes(AdHoc.Receiver __src)
            {
                var __slot = __src.slot!;
                int __i = 0, __t = 0, __v = 0;
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

                            if (__src.try_get_string(Agent.Result.task_.STR_LEN_MAX, 2)) goto case 2;
                            return false;
                        case 2:
                            task = __src.get_string();
                            goto case 3;
                        case 3:
                            #endregion
                            #region result
                            if (__src.is_null(1 << 1))
                                goto case 6;

                            if (__slot.get_len1(Agent.Result.result_.ARRAY_LEN_MAX, 4, 4)) goto case 4;
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
                                    for (; __i < __t; __i++) { _result![__i] = (byte)__src.get_byte(); }
                                }

                                __src.retry_at(5);
                                return false;
                            }

                            __i += __t = __slot.index1;
                            for (; __t < __i; __t++) { _result![__t] = (byte)__src.get_byte(); }

                            goto case 6;
                        case 6:
                            #endregion
                            #region info
                            if (__src.is_null(1 << 2))
                                goto case 8;

                            if (__src.try_get_string(Agent.Result.info_.STR_LEN_MAX, 7)) goto case 7;
                            return false;
                        case 7:
                            info = __src.get_string();
                            goto case 8;
                        case 8:
                        #endregion

                        default:
                            return true;
                    }
            }
        }

        public class RequestResult : IEquatable<RequestResult>, Communication.Transmitter.Transmittable
        { //The request for pending task result

            public int __id => __id_;
            public const int __id_ = 6;

            void Communication.Transmitter.Transmittable.Sent(Communication.Transmitter via) => Communication.Transmitter.onTransmit.Sent(via, this);
            #region task
            public string? task { get; set; } = null;

            public struct task_
            {
                public const int STR_LEN_MAX = 255;
            }
            #endregion

            public int GetHashCode
            {
                get
                {
                    var _hash = 3001003L;
                    #region task
                    _hash = HashCode.Combine(_hash, task);
                    #endregion

                    return (int)_hash;
                }
            }

            bool IEquatable<RequestResult>.Equals(RequestResult? _pack)
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

                return true;
            }

            bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
            {
                var __slot = __dst.slot!;
                int __i = 0, __t = 0, __v = 0;
                for (; ; )
                    switch (__slot.state)
                    {
                        case 0:
                            if (__dst.put_val(__id_, 1, 1))
                                goto case 1;
                            return false;

                        case 1:

                            if (!__dst.init_fields_nulls(task != null ?
                                                             1 :
                                                             0, 1))
                                return false;

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

                        default:
                            return true;
                    }
            }
        }

        public class Up_to_date : IEquatable<Up_to_date>, ObserverCommunication.Receiver.Receivable, ObserverCommunication.Transmitter.Transmittable
        { //Request to send updated Project pack or Up_to_date if data is not changed

            public int __id => __id_;
            public const int __id_ = 10;
            void ObserverCommunication.Receiver.Receivable.Received(ObserverCommunication.Receiver via) => ObserverCommunication.Receiver.onReceive.Received(via, this);

            void ObserverCommunication.Transmitter.Transmittable.Sent(ObserverCommunication.Transmitter via) => ObserverCommunication.Transmitter.onTransmit.Sent(via, this);
            #region info
            public string? info { get; set; } = null; //Can be an updating error description

            public struct info_
            { //Can be an updating error description

                public const int STR_LEN_MAX = 65000;
            }
            #endregion

            public int GetHashCode
            {
                get
                {
                    var _hash = 3001003L;
                    #region info
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

            bool AdHoc.Transmitter.BytesSrc.__get_bytes(AdHoc.Transmitter __dst)
            {
                var __slot = __dst.slot!;
                int __i = 0, __t = 0, __v = 0;
                for (; ; )
                    switch (__slot.state)
                    {
                        case 0:
                            if (__dst.put_val(__id_, 1, 1))
                                goto case 1;
                            return false;

                        case 1:

                            if (!__dst.init_fields_nulls(info != null ?
                                                             1 :
                                                             0, 1))
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

            bool AdHoc.Receiver.BytesDst.__put_bytes(AdHoc.Receiver __src)
            {
                var __slot = __src.slot!;
                int __i = 0, __t = 0, __v = 0;
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

                            if (__src.try_get_string(Agent.Up_to_date.info_.STR_LEN_MAX, 2)) goto case 2;
                            return false;
                        case 2:
                            info = __src.get_string();
                            goto case 3;
                        case 3:
                        #endregion

                        default:
                            return true;
                    }
            }
        }

        namespace Entity
        {
            public enum Type : byte
            {
                Channel = 4,
                Field = 3,
                Host = 1,
                Pack = 2,
                Project = 0,
                Stage = 5,
            }
        }

        public interface Communication
        {
            public static Network.TCP<Communication.Transmitter, Communication.Receiver>.Channel new_TCP_channel(Network.TCP<Communication.Transmitter, Communication.Receiver> host)
            {
                var channel = new Network.TCP<Communication.Transmitter, Communication.Receiver>.Channel(host);
                _ = new Transmitter(channel);
                _ = new Receiver(channel);
                return channel;
            }

            public static Network.TCP<Communication.Transmitter, Communication.Receiver>.WebSocket new_WebSocket_channel(Network.TCP<Communication.Transmitter, Communication.Receiver> host)
            {
                var channel = new Network.TCP<Communication.Transmitter, Communication.Receiver>.WebSocket(host);
                _ = new Transmitter(channel);
                _ = new Receiver(channel);
                return channel;
            }

            public class Transmitter : AdHoc.Transmitter, AdHoc.Transmitter.EventsHandler
            {
                #region> Transmitter code
                #endregion> ÿ.Transmitter
                public Network.TCP<Communication.Transmitter, Communication.Receiver>.Channel? channel;

                public Transmitter() : base(null)
                {
                    channel = null;
                    handler = this;
                }

                public void onSending(Transmitter dst, BytesSrc src)
                {
                    #region> on Sending
                    var r = channel!.receiver!;
                    if (r.curr_stage.on_transmitting(src.__id) == Stage.ERROR)
                    {
                        channel.Close_and_dispose();
                        throw new Exception($"At stage: {r.curr_stage.name}, sending an unexpected packet with id: {src.__id}, name: {src.GetType().FullName}, detected.");
                    }
                    #endregion> ÿ.Transmitter.Sending
                }

                public void onSent(Transmitter dst, BytesSrc src)
                {
                    #region> on Sent
                    var r = channel!.receiver!;
                    if ((r.curr_stage = (r.prev_stage = r.curr_stage).on_transmitting(src.__id)) == Stage.ERROR)
                    {
                        channel.Close_and_dispose();
                        throw new Exception($"At stage: {r.prev_stage.name}, sending an unexpected packet with id: {src.__id}, name: {src.GetType().FullName}, detected.");
                    }
                    #endregion> ÿ.Transmitter.Sent
                    ((Transmittable)src).Sent(this);
                }

                public Transmitter(Network.TCP<Communication.Transmitter, Communication.Receiver>.Channel channel, int power_of_2_sending_queue_size = 5) : base(null)
                {
                    (this.channel = channel).transmitter = this;
                    handler = this;
                }

                public static Transmittable.Handler onTransmit = Transmittable.Handler.STUB;

                public interface Transmittable : BytesSrc
                {
                    void Sent(Communication.Transmitter via);

                    interface Handler
                    {
                        void Sent(Communication.Transmitter via, Project pack);
                        void Sent(Communication.Transmitter via, Proto pack);
                        void Sent(Communication.Transmitter via, Signup pack);
                        void Sent(Communication.Transmitter via, RequestResult pack);
                        void Sent(Communication.Transmitter via, Login pack);
                        void Sent(Communication.Transmitter via, Version pack);

                        class Stub : Handler
                        {
                            public void Sent(Communication.Transmitter via, Project pack) { }
                            public void Sent(Communication.Transmitter via, Proto pack) { }
                            public void Sent(Communication.Transmitter via, Signup pack) { }
                            public void Sent(Communication.Transmitter via, Login pack) { }
                            public void Sent(Communication.Transmitter via, RequestResult pack) { }
                            public void Sent(Communication.Transmitter via, Version pack) { }
                        }

                        static readonly Stub STUB = new();
                    }
                }

                public bool send(Version pack) => base.send(Version.Handler.ONE, pack);
                public bool send(Proto pack) => base.send(pack);
                public bool send(Signup pack) => base.send(pack);
                public bool send(RequestResult pack) => base.send(pack);
                public bool send(Login pack) => base.send(Login.Handler.ONE, pack);
                public bool send(Project pack) => base.send(pack);
            }

            public class Receiver : AdHoc.Receiver, AdHoc.Receiver.EventsHandler
            {
                #region> Receiver code
                public Stage prev_stage = Stages.O;
                public Stage curr_stage = Stages.O;

                public override void Close()
                {
                    base.Close();
                    prev_stage = Stages.O;
                    curr_stage = Stages.O;
                }
                #endregion> ÿ.Receiver
                public readonly Network.TCP<Communication.Transmitter, Communication.Receiver>.Channel channel;

                public Receiver() : base(null, 1)
                {
                    handler = this;
                    channel = null;
                }

                public Receiver(Network.TCP<Communication.Transmitter, Communication.Receiver>.Channel channel) : base(null, 1)
                {
                    handler = this;
                    (this.channel = channel).receiver = this;
                }

                public static Receivable.Handler onReceive = Receivable.Handler.STUB;

                public interface Receivable : BytesDst
                {
                    protected internal void Received(Communication.Receiver via);

                    public interface Handler
                    {
                        void Received(Communication.Receiver via, Invitation pack);
                        void Received(Communication.Receiver via, Info pack);
                        void Received(Communication.Receiver via, Result pack);

                        class Stub : Handler
                        {
                            public void Received(Communication.Receiver via, Invitation pack) { }
                            public void Received(Communication.Receiver via, Info pack) { }
                            public void Received(Communication.Receiver via, Result pack) { }
                        }

                        static readonly Stub STUB = new();
                    }
                }

                public static _Allocator allocator = _Allocator.DEFAULT;

                public override BytesDst Allocate(int id) => id switch
                {
                    Info.__id_ => allocator.new_Info(),
                    Invitation.__id_ => Invitation.Handler.ONE,
                    Layout.__id_ => allocator.new_Layout(),
                    Result.__id_ => allocator.new_Result(),
                    Show_Code.__id_ => Show_Code.Handler.ONE,
                    Up_to_date.__id_ => allocator.new_Up_to_date(),
                    Layout.View.__id_ => allocator.new_Layout_View(),
                    Layout.XY.__id_ => Layout.XY.Handler.ONE,
                    _ => throw new Exception("Received a packet with unknown id:" + id)
                };

                public override BytesDst Receiving(int id)
                {
                    #region> on receiving
                    if ((curr_stage = (prev_stage = curr_stage).on_receiving(id)) == Stage.ERROR)
                    {
                        channel.Close_and_dispose();
                        throw new Exception($"At stage:{prev_stage.name}, receiving an unexpected pack with id:{id}, detected");
                    }
                    #endregion> ÿ.Receiver.receiving
                    return Allocate(id);
                }

                public void onReceived(Receiver src, BytesDst dst)
                {
                    #region> on received
                    if (curr_stage == Stage.EXIT) channel.Close_and_dispose();
                    #endregion> ÿ.Receiver.received
                    ((Receivable)dst).Received(this);
                }
            }

            public interface Stages
            {
                public static readonly AdHoc.Stage Start = new(0, "Start", TimeSpan.FromSeconds(12),
                                                               on_transmitting: id => id switch
                                                                                      {
                                                                                          Agent.Version.__id_ => VersionMatching,

                                                                                          _ => AdHoc.Stage.ERROR
                                                                                      }
                                                              );

                public static readonly AdHoc.Stage VersionMatching = new(1, "VersionMatching", TimeSpan.FromSeconds(65535),
                                                                         on_receiving: id => id switch
                                                                                             {
                                                                                                 Agent.Info.__id_ => AdHoc.Stage.EXIT,
                                                                                                 Agent.Invitation.__id_ => Login,

                                                                                                 _ => AdHoc.Stage.ERROR
                                                                                             }
                                                                        );

                public static readonly AdHoc.Stage Login = new(2, "Login", TimeSpan.FromSeconds(65535),
                                                               on_transmitting: id => id switch
                                                                                      {
                                                                                          Agent.Login.__id_ or
                                                                                              Agent.Signup.__id_ => LoginResponse,

                                                                                          _ => AdHoc.Stage.ERROR
                                                                                      }
                                                              );

                public static readonly AdHoc.Stage LoginResponse = new(3, "LoginResponse", TimeSpan.FromSeconds(12),
                                                                       on_receiving: id => id switch
                                                                                           {
                                                                                               Agent.Info.__id_ => AdHoc.Stage.EXIT,
                                                                                               Agent.Invitation.__id_ => TodoJobRequest,

                                                                                               _ => AdHoc.Stage.ERROR
                                                                                           }
                                                                      );

                public static readonly AdHoc.Stage TodoJobRequest = new(4, "TodoJobRequest", TimeSpan.FromSeconds(12),
                                                                        on_transmitting: id => id switch
                                                                                               {
                                                                                                   Agent.RequestResult.__id_ or
                                                                                                       Agent.Project.__id_ => Project,
                                                                                                   Agent.Proto.__id_ => Proto,

                                                                                                   _ => AdHoc.Stage.ERROR
                                                                                               }
                                                                       );

                public static readonly AdHoc.Stage Project = new(5, "Project", TimeSpan.FromSeconds(65535),
                                                                 on_receiving: id => id switch
                                                                                     {
                                                                                         Agent.Info.__id_ or
                                                                                             Agent.Result.__id_ => AdHoc.Stage.EXIT,

                                                                                         _ => AdHoc.Stage.ERROR
                                                                                     }
                                                                );

                public static readonly AdHoc.Stage Proto = new(6, "Proto", TimeSpan.FromSeconds(65535),
                                                               on_receiving: id => id switch
                                                                                   {
                                                                                       Agent.Info.__id_ or
                                                                                           Agent.Result.__id_ => AdHoc.Stage.EXIT,

                                                                                       _ => AdHoc.Stage.ERROR
                                                                                   }
                                                              );

                public static readonly AdHoc.Stage O = Start; //Init Stage
            }
        }

        public interface ObserverCommunication
        {
            public static Network.TCP<ObserverCommunication.Transmitter, ObserverCommunication.Receiver>.Channel new_TCP_channel(Network.TCP<ObserverCommunication.Transmitter, ObserverCommunication.Receiver> host)
            {
                var channel = new Network.TCP<ObserverCommunication.Transmitter, ObserverCommunication.Receiver>.Channel(host);
                _ = new Transmitter(channel);
                _ = new Receiver(channel);
                return channel;
            }

            public static Network.TCP<ObserverCommunication.Transmitter, ObserverCommunication.Receiver>.WebSocket new_WebSocket_channel(Network.TCP<ObserverCommunication.Transmitter, ObserverCommunication.Receiver> host)
            {
                var channel = new Network.TCP<ObserverCommunication.Transmitter, ObserverCommunication.Receiver>.WebSocket(host);
                _ = new Transmitter(channel);
                _ = new Receiver(channel);
                return channel;
            }

            public class Transmitter : AdHoc.Transmitter, AdHoc.Transmitter.EventsHandler
            {
                #region> Transmitter code
                #endregion> Ā.Transmitter
                public Network.TCP<ObserverCommunication.Transmitter, ObserverCommunication.Receiver>.Channel? channel;

                public Transmitter() : base(null)
                {
                    channel = null;
                    handler = this;
                }

                public void onSending(Transmitter dst, BytesSrc src)
                {
                    #region> on Sending
                    if (ObserverCommunication.Receiver.curr_stage.on_transmitting(src.__id) == Stage.ERROR)
                        AdHocAgent.exit($"At stage: {ObserverCommunication.Receiver.curr_stage.name}, sending an unexpected packet with id: {src.__id}, name: {src.GetType().FullName}, detected.");
                    #endregion> Ā.Transmitter.Sending
                }

                public void onSent(Transmitter dst, BytesSrc src)
                {
                    #region> on Sent
                    if ((ObserverCommunication.Receiver.curr_stage = (ObserverCommunication.Receiver.prev_stage = ObserverCommunication.Receiver.curr_stage).on_transmitting(src.__id)) == Stage.ERROR)
                        AdHocAgent.exit($"At stage: {ObserverCommunication.Receiver.prev_stage.name}, sending an unexpected packet with id: {src.__id}, name: {src.GetType().FullName}, detected.");
                    #endregion> Ā.Transmitter.Sent
                    ((Transmittable)src).Sent(this);
                }

                public Transmitter(Network.TCP<ObserverCommunication.Transmitter, ObserverCommunication.Receiver>.Channel channel, int power_of_2_sending_queue_size = 5) : base(null)
                {
                    (this.channel = channel).transmitter = this;
                    handler = this;
                }

                public static Transmittable.Handler onTransmit = Transmittable.Handler.STUB;

                public interface Transmittable : BytesSrc
                {
                    void Sent(ObserverCommunication.Transmitter via);

                    interface Handler
                    {
                        void Sent(ObserverCommunication.Transmitter via, Layout pack);
                        void Sent(ObserverCommunication.Transmitter via, Project pack);
                        void Sent(ObserverCommunication.Transmitter via, Up_to_date pack);

                        class Stub : Handler
                        {
                            public void Sent(ObserverCommunication.Transmitter via, Layout pack) { }
                            public void Sent(ObserverCommunication.Transmitter via, Project pack) { }
                            public void Sent(ObserverCommunication.Transmitter via, Up_to_date pack) { }
                        }

                        static readonly Stub STUB = new();
                    }
                }

                public bool send(Layout pack) => base.send(pack);
                public bool send(Project pack) => base.send(pack);
                public bool send(Up_to_date pack) => base.send(pack);
            }

            public class Receiver : AdHoc.Receiver, AdHoc.Receiver.EventsHandler
            {
                #region> Receiver code
                public static Stage curr_stage = Stages.O; // exist only one Observer instance
                public static Stage prev_stage = Stages.O; // exist only one Observer instance

                public override void Close()
                {
                    base.Close();
                    curr_stage = Stages.O;
                }
                #endregion> Ā.Receiver
                public readonly Network.TCP<ObserverCommunication.Transmitter, ObserverCommunication.Receiver>.Channel channel;

                public Receiver() : base(null, 1)
                {
                    handler = this;
                    channel = null;
                }

                public Receiver(Network.TCP<ObserverCommunication.Transmitter, ObserverCommunication.Receiver>.Channel channel) : base(null, 1)
                {
                    handler = this;
                    (this.channel = channel).receiver = this;
                }

                public static Receivable.Handler onReceive = Receivable.Handler.STUB;

                public interface Receivable : BytesDst
                {
                    protected internal void Received(ObserverCommunication.Receiver via);

                    public interface Handler
                    {
                        void Received(ObserverCommunication.Receiver via, Show_Code pack);
                        void Received(ObserverCommunication.Receiver via, Layout pack);
                        void Received(ObserverCommunication.Receiver via, Up_to_date pack);

                        class Stub : Handler
                        {
                            public void Received(ObserverCommunication.Receiver via, Show_Code pack) { }
                            public void Received(ObserverCommunication.Receiver via, Layout pack) { }
                            public void Received(ObserverCommunication.Receiver via, Up_to_date pack) { }
                        }

                        static readonly Stub STUB = new();
                    }
                }

                public static _Allocator allocator = _Allocator.DEFAULT;

                public override BytesDst Allocate(int id) => id switch
                {
                    Info.__id_ => allocator.new_Info(),
                    Invitation.__id_ => Invitation.Handler.ONE,
                    Layout.__id_ => allocator.new_Layout(),
                    Result.__id_ => allocator.new_Result(),
                    Show_Code.__id_ => Show_Code.Handler.ONE,
                    Up_to_date.__id_ => allocator.new_Up_to_date(),
                    Layout.View.__id_ => allocator.new_Layout_View(),
                    Layout.XY.__id_ => Layout.XY.Handler.ONE,
                    _ => throw new Exception("Received a packet with unknown id:" + id)
                };

                public override BytesDst Receiving(int id)
                {
                    #region> on receiving
                    if ((curr_stage = (prev_stage = curr_stage).on_receiving(id)) == Stage.ERROR)
                        AdHocAgent.exit($"At stage:{prev_stage.name}, receiving an unexpected pack with id:{id}, detected");
                    #endregion> Ā.Receiver.receiving
                    return Allocate(id);
                }

                public void onReceived(Receiver src, BytesDst dst)
                {
                    #region> on received
                    #endregion> Ā.Receiver.received
                    ((Receivable)dst).Received(this);
                }
            }

            public interface Stages
            {
                public static readonly AdHoc.Stage Start = new(7, "Start", TimeSpan.FromSeconds(65535),
                                                               on_transmitting: id => id switch
                                                                                      {
                                                                                          Agent.Layout.__id_ => Layout,
                                                                                          Agent.Project.__id_ => Operate,

                                                                                          _ => AdHoc.Stage.ERROR
                                                                                      }
                                                              );

                public static readonly AdHoc.Stage Layout = new(8, "Layout", TimeSpan.FromSeconds(65535),
                                                                on_transmitting: id => id switch
                                                                                       {
                                                                                           Agent.Project.__id_ => Operate,

                                                                                           _ => AdHoc.Stage.ERROR
                                                                                       }
                                                               );

                public static readonly AdHoc.Stage Operate = new(9, "Operate", TimeSpan.FromSeconds(65535),
                                                                 on_receiving: id => id switch
                                                                                     {
                                                                                         Agent.Up_to_date.__id_ => RefreshProject,
                                                                                         Agent.Show_Code.__id_ or
                                                                                             Agent.Layout.__id_ => Operate,

                                                                                         _ => AdHoc.Stage.ERROR
                                                                                     }
                                                                );

                public static readonly AdHoc.Stage RefreshProject = new(10, "RefreshProject", TimeSpan.FromSeconds(65535),
                                                                        on_transmitting: id => id switch
                                                                                               {
                                                                                                   Agent.Project.__id_ or
                                                                                                       Agent.Up_to_date.__id_ => Operate,

                                                                                                   _ => AdHoc.Stage.ERROR
                                                                                               }
                                                                       );

                public static readonly AdHoc.Stage O = Start; //Init Stage
            }
        }
    }

    public class _Allocator
    {
        public Func<Agent.Project.Channel.Stage.Branch> new_Project_Channel_Stage_Branch = () => throw new Exception("The producer of Agent.Project.Channel.Stage.Branch is not assigned");
        public Func<Agent.Project.Channel> new_Project_Channel = () => throw new Exception("The producer of Agent.Project.Channel is not assigned");
        public Func<Agent.Project.Host.Pack.Field> new_Project_Host_Pack_Field = () => throw new Exception("The producer of Agent.Project.Host.Pack.Field is not assigned");
        public Func<Agent.Project.Host> new_Project_Host = () => throw new Exception("The producer of Agent.Project.Host is not assigned");
        public Func<Agent.Info> new_Info = () => new Agent.Info();
        public Func<Agent.Invitation.Handler> new_Invitation = () => Agent.Invitation.Handler.ONE;
        public Func<Agent.Layout> new_Layout = () => throw new Exception("The producer of Agent.Layout is not assigned");
        public Func<Agent.Login.Handler> new_Login = () => Agent.Login.Handler.ONE;
        public Func<Agent.Project.Host.Pack> new_Project_Host_Pack = () => throw new Exception("The producer of Agent.Project.Host.Pack is not assigned");
        public Func<Agent.Project> new_Project = () => throw new Exception("The producer of Agent.Project is not assigned");
        public Func<Agent.Proto> new_Proto = () => new Agent.Proto();
        public Func<Agent.RequestResult> new_RequestResult = () => new Agent.RequestResult();
        public Func<Agent.Result> new_Result = () => new Agent.Result();
        public Func<Agent.Show_Code.Handler> new_Show_Code = () => Agent.Show_Code.Handler.ONE;
        public Func<Agent.Signup> new_Signup = () => new Agent.Signup();
        public Func<Agent.Project.Channel.Stage> new_Project_Channel_Stage = () => throw new Exception("The producer of Agent.Project.Channel.Stage is not assigned");
        public Func<Agent.Up_to_date> new_Up_to_date = () => new Agent.Up_to_date();
        public Func<Agent.Version.Handler> new_Version = () => Agent.Version.Handler.ONE;
        public Func<Agent.Layout.View> new_Layout_View = () => throw new Exception("The producer of Agent.Layout.View is not assigned");
        public Func<Agent.Layout.XY.Handler> new_Layout_XY = () => Agent.Layout.XY.Handler.ONE;

        public _Allocator CopyTo(_Allocator dst)
        {
            dst.new_Project_Channel_Stage_Branch = new_Project_Channel_Stage_Branch;
            dst.new_Project_Channel = new_Project_Channel;
            dst.new_Project_Host_Pack_Field = new_Project_Host_Pack_Field;
            dst.new_Project_Host = new_Project_Host;
            dst.new_Info = new_Info;
            dst.new_Invitation = new_Invitation;
            dst.new_Layout = new_Layout;
            dst.new_Login = new_Login;
            dst.new_Project_Host_Pack = new_Project_Host_Pack;
            dst.new_Project = new_Project;
            dst.new_Proto = new_Proto;
            dst.new_RequestResult = new_RequestResult;
            dst.new_Result = new_Result;
            dst.new_Show_Code = new_Show_Code;
            dst.new_Signup = new_Signup;
            dst.new_Project_Channel_Stage = new_Project_Channel_Stage;
            dst.new_Up_to_date = new_Up_to_date;
            dst.new_Version = new_Version;
            dst.new_Layout_View = new_Layout_View;
            dst.new_Layout_XY = new_Layout_XY;

            return dst;
        }

        public static readonly _Allocator DEFAULT = new();
    }
}