//  MIT License
//
//  Copyright © 2020 Chikirev Sirguy, Unirail Group. All rights reserved.
//  For inquiries, please contact:  al8v5C6HU4UtqE9@gmail.com
//  GitHub Repository: https://github.com/AdHoc-Protocol
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to use,
//  copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
//  the Software, and to permit others to do so, under the following conditions:
//
//  1. The above copyright notice and this permission notice must be included in all
//     copies or substantial portions of the Software.
//
//  2. Users of the Software must provide a clear acknowledgment in their user
//     documentation or other materials that their solution includes or is based on
//     this Software. This acknowledgment should be prominent and easily visible,
//     and can be formatted as follows:
//     "This product includes software developed by Chikirev Sirguy and the Unirail Group
//     (https://github.com/AdHoc-Protocol)."
//
//  3. If you modify the Software and distribute it, you must include a prominent notice
//     stating that you have changed the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT, OR OTHERWISE, ARISING FROM,
//  OUT OF, OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using org.unirail.collections;

namespace org.unirail
{
    public class AdHoc
    {
        public interface BytesSrc
        {
            //if 0 < return   : bytes read in DST buffer
            //if return == 0  : may have more data but not enough space in provided DST buffer
            //if return == -1 : no more data left
            int Read(byte[] dst, int dst_byte, int dst_bytes);
            Action<BytesSrc>? subscribeOnNewBytesToTransmitArrive(Action<BytesSrc>? subscriber); //Subscribe to be notified when new bytes are available for transmission
            bool isOpen();
            void Close();
        }

        public interface BytesDst
        {
            /**
             * write bytes
             * ATTENTION! The data in the provided buffer "src" may change due to buffer reuse.
             */
            int Write(byte[] src, int src_byte, int src_bytes);

            bool isOpen();
            void Close();
        }

        public class Stage
        {
            public readonly ushort uid;
            public readonly string name;
            public readonly TimeSpan timeout;
            public readonly Func<int, Stage> on_transmitting;
            public readonly Func<int, Stage> on_receiving;

            public Stage(ushort uid, string name, TimeSpan timeout, Func<int, Stage>? on_transmitting = null, Func<int, Stage>? on_receiving = null)
            {
                this.uid = uid;
                this.name = name;
                this.timeout = timeout;
                this.on_transmitting = on_transmitting ?? (
                                                              _ => ERROR);
                this.on_receiving = on_receiving ?? (
                                                        _ => ERROR);
            }

            public override string ToString() => name;

            public static readonly Stage EXIT = new(ushort.MaxValue, "Exit", TimeSpan.MaxValue,
                                                    _ => ERROR,
                                                    _ => ERROR);

            public static readonly Stage ERROR = new(ushort.MaxValue, "Error", TimeSpan.MaxValue,
                                                     _ => ERROR,
                                                     _ => ERROR);
        }

        internal const uint OK = int.MaxValue,
                            STR = OK - 100,
                            RETRY = STR + 1,
                            VAL4 = RETRY + 1,
                            VAL8 = VAL4 + 1,
                            INT1 = VAL8 + 1,
                            INT2 = INT1 + 1,
                            INT4 = INT2 + 1,
                            LEN0 = INT4 + 1,
                            LEN1 = LEN0 + 1,
                            LEN2 = LEN1 + 1,
                            BITS = LEN2 + 1,
                            BITS_BYTES = BITS + 1,
                            VARINT = BITS_BYTES + 1;

        protected int bit;

        public string? str;
        public uint bits;

        public byte[]? buffer;
        public int byte_; //position in buffer
        public int byte_max;

        public uint mode;

        internal uint u4;
        public ulong u8;
        public ulong u8_;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public T get8<T>() => Unsafe.As<ulong, T>(ref u8);

        private int bytes_left;
        private int bytes_max;

        public int remaining => byte_max - byte_;

        public static T[] Resize<T>(T[] src, int new_length, T fill_value)
        {
            var len = src.Length;
            if (len == new_length)
                return src;

            Array.Resize(ref src, new_length);
            if (len < new_length)
                Array.Fill(src, fill_value, len, new_length - len);
            return src;
        }

        public static List<T> Resize<T>(List<T> list, int newLength, T fillValue)
        {
            if (list.Count < newLength)
                while (list.Count < newLength)
                    list.Add(fillValue);
            else
                list.RemoveRange(newLength, list.Count - newLength);

            return list;
        }

        ///<summary>
        ///Resizes the specified list to the specified new length.
        ///If the new length is greater than the current length, the list is expanded and filled with the specified value.
        ///If the new length is less than the current length, the list is truncated.
        ///</summary>
        ///<typeparam name="T">The type of elements in the list.</typeparam>
        ///<param name="list">The list to resize.</param>
        ///<param name="newLength">The desired length of the list.</param>
        ///<param name="fillValue">The value to use for filling the list if it is expanded.</param>
        ///<returns>The resized list.</returns>
        public static IList<T> Resize<T>(IList<T> list, int newLength, T fillValue)
        {
            switch (list)
            {
                case T[] array:
                    return Resize(array, newLength, fillValue);

                case List<T> list_:
                    return Resize(list_, newLength, fillValue);

                default:
                    //If the list is neither an array nor a List<T>, handle the resizing manually
                    if (list.Count < newLength)
                        while (list.Count < newLength)
                            list.Add(fillValue);
                    else
                        while (list.Count > newLength)
                            list.RemoveAt(list.Count - 1);

                    return list;
            }
        }

        public static List<T> toList<T>(IList<T> src, int max)
        {
            var ret = new List<T>(max);

            if (max < src.Count)
                for (var i = 0; i < max; i++)
                    ret.Add(src[i]);
            else
                ret.AddRange(src);

            return ret;
        }

        public static T[] sizeArray<T>(T fill, int size)
        {
            var ret = new T[size];
            //Only fill the array if fill is not the default value for the type (e.g., 0 for integers, null for reference types)
            if (EqualityComparer<T>.Default.Equals(fill, default))
                return ret;

            //Fill the array with the specified fill value
            while (-1 < --size)
                ret[size] = fill;
            return ret;
        }

        public static List<T> sizeList<T>(T fill, int size)
        {
            var ret = new List<T>(size);

            while (-1 < --size)
                ret.Add(fill);
            return ret;
        }
        #region CRC
        private const int CRC_LEN_BYTES = 2; //CRC len in bytes

        private static readonly ushort[] tab = { 0, 4129, 8258, 12387, 16516, 20645, 24774, 28903, 33032, 37161, 41290, 45419, 49548, 53677, 57806, 61935 };

        //!!!! Https://github.com/redis/redis/blob/95b1979c321eb6353f75df892ab8be68cf8f9a77/src/crc16.c
        //Output for "123456789"     : 31C3 (12739)
        private static ushort crc16(byte src, ushort crc)
        {
            crc = (ushort)(tab[(crc >> 12 ^ src >> 4) & 0x0F] ^ crc << 4);
            return (ushort)(tab[(crc >> 12 ^ src & 0x0F) & 0x0F] ^ crc << 4);
        }
        #endregion

        public abstract class Receiver : Context.Receiver, BytesDst
        {
            public interface BytesDst
            {
                bool __put_bytes(Receiver src);
                int __id { get; }
            }

            public interface EventsHandler
            {
                // Callback triggered once enough bytes are received from the external layer to identify the packet type.
                void onReceiving(Receiver src, BytesDst dst) { }
                // Callback triggered once a packet is fully received and ready for dispatch to the internal layer.
                void onReceived(Receiver src, BytesDst dst) { }
            }

            public EventsHandler handler;
            public EventsHandler exchange(EventsHandler handler) => Interlocked.Exchange(ref this.handler, handler);
            private readonly byte id_bytes;

            public Receiver(EventsHandler handler, int id_bytes)
            {
                this.handler = handler;
                bytes_left = bytes_max = this.id_bytes = (byte)id_bytes;
                slot_ref = new(new Slot(this, null));
            }

            public static OnErrorHandler error_handler = OnErrorHandler.DEFAULT;

            public enum OnError
            {
                FFFF_ERROR = 0,
                CRC_ERROR = 1,
                BYTES_DISTORTION = 3,
                OVERFLOW = 4,
                INVALID_ID = 5
            }

            public interface OnErrorHandler
            {
                static OnErrorHandler DEFAULT = new ToConsole();

                class ToConsole : OnErrorHandler
                {
                    public void error(AdHoc.BytesDst src, OnError error, Exception? ex = null)
                    {
                        switch (error)
                        {
                            case OnError.FFFF_ERROR:
                                Console.WriteLine("FFFF_ERROR at " + src + (ex == null ?
                                                                                "" :
                                                                                ex + Environment.StackTrace));
                                return;
                            case OnError.CRC_ERROR:
                                Console.WriteLine("CRC_ERROR at " + src + (ex == null ?
                                                                               "" :
                                                                               ex + Environment.StackTrace));
                                return;
                            case OnError.BYTES_DISTORTION:
                                Console.WriteLine("BYTES_DISTORTION at " + src + (ex == null ?
                                                                                      "" :
                                                                                      ex + Environment.StackTrace));
                                return;
                            case OnError.OVERFLOW:
                                Console.WriteLine("OVERFLOW at " + src + (ex == null ?
                                                                              "" :
                                                                              ex + Environment.StackTrace));
                                return;
                            case OnError.INVALID_ID:
                                Console.WriteLine("INVALID_ID at " + src + (ex == null ?
                                                                                "" :
                                                                                Environment.StackTrace));
                                return;
                        }
                    }
                }

                void error(AdHoc.BytesDst src, OnError error, Exception? ex = null);
            }

            public static OnErrorHandler_ error_handler_ = OnErrorHandler_.DEFAULT;

            public interface OnErrorHandler_
            {
                static OnErrorHandler_ DEFAULT = new ToConsole();

                class ToConsole : OnErrorHandler_
                {
                    public void error(AdHoc.BytesSrc src, OnError error, Exception? ex = null)
                    {
                        switch (error)
                        {
                            case OnError.OVERFLOW:
                                Console.WriteLine("OVERFLOW at " + src + (ex == null ?
                                                                              "" :
                                                                              ex + Environment.StackTrace));
                                return;
                        }
                    }
                }

                void error(AdHoc.BytesSrc src, OnError error, Exception? ex = null);
            }

            public class Framing : AdHoc.BytesDst, EventsHandler
            {
                public bool isOpen() => upper_layer.isOpen();
                public Receiver upper_layer; //the upper layer external interface
                public EventsHandler handler;     //interface to the upper layer internal consumer

                public EventsHandler exchange(EventsHandler handler) => Interlocked.Exchange(ref this.handler, handler);

                public Framing(Receiver upper_layer) => switch_to(upper_layer);

                public void switch_to(Receiver upper_layer)
                {
                    reset();

                    if (this.upper_layer != null)
                    {
                        this.upper_layer.Reset();
                        this.upper_layer.exchange(handler); //off hook
                    }

                    handler = (this.upper_layer = upper_layer).exchange(this);
                }

                private void error_reset(OnError error)
                {
                    error_handler.error(this, error);
                    reset();
                }

                public void Close()
                {
                    reset();
                    upper_layer.Close();
                }

                private void reset()
                {
                    bits = 0;
                    shift = 0;
                    crc0 = 0;
                    crc1 = 0;
                    crc2 = 0;
                    crc3 = 0;
                    dst_byte = 0;
                    raw = 0;
                    waiting_for_dispatching_pack = null;

                    if (!FF) //this packet received, but next packet start FF mark does not deected, so switch to SEEK_FF mode
                        state = State.SEEK_FF;
                }

                /**
                 * write bytes
                 * ATTENTION! The data in the provided buffer "src" may change due to buffer reuse.
                 */
                public int Write(byte[] src, int src_byte, int src_bytes)
                {
                    if (src_bytes < 1)
                        return 0;
                    var limit = src_byte + src_bytes;
                    dst_byte = 0;
                    switch (state)
                    {
                        case State.SEEK_FF: //bytes distortion was detected, skip bytes until FF sync mark
                            while (src_byte < limit)
                                if (src[src_byte++] == 0xFF)
                                {
                                    state = State.NORMAL;
                                    if (FF)
                                        error_handler.error(this, OnError.FFFF_ERROR);
                                    FF = true;
                                    if (src_byte < limit)
                                        goto write;
                                    return src_bytes;
                                }
                                else
                                    FF = false;

                            return src_bytes;
                        case State.Ox7F:
                            if (FF = (raw = src[src_byte++]) == 0xFF) //FF here is an error
                            {
                                error_reset(OnError.BYTES_DISTORTION);
                                goto write;
                            }

                            bits |= ((raw & 1) << 7 | 0x7F) << shift;
                            put(src, 0);
                            write(src, 1, State.NORMAL);
                            goto case State.Ox7F_;
                        case State.Ox7F_:
                            while (raw == 0x7F)
                            {
                                if (src_byte == limit)
                                {
                                    write(src, dst_byte, State.Ox7F_);
                                    return src_bytes;
                                }

                                if (FF = (raw = src[src_byte++]) == 0xFF) //FF here is an error
                                {
                                    error_reset(OnError.BYTES_DISTORTION);
                                    goto write;
                                }

                                bits |= (raw << 6 | 0x3F) << shift;
                                if ((shift += 7) < 8)
                                    continue;
                                shift -= 8;
                                put(src, dst_byte++);
                            }

                            bits |= (raw >> 1) << shift;
                            if ((shift += 7) < 8)
                                break;
                            shift -= 8;
                            if (src_byte == dst_byte)
                            {
                                write(src, dst_byte, State.NORMAL);
                                dst_byte = 0;
                            }

                            put(src, dst_byte++);
                            state = State.NORMAL;
                            break;
                    }

                write:
                    while (src_byte < limit)
                    {
                        if ((raw = src[src_byte++]) == 0x7F)
                        {
                            FF = false;
                            if (src_byte == limit)
                            {
                                write(src, dst_byte, State.Ox7F);
                                return src_bytes;
                            }

                            if (FF = (raw = src[src_byte++]) == 0xFF) //FF here is an error
                            {
                                error_reset(OnError.BYTES_DISTORTION);
                                goto write;
                            }

                            bits |= ((raw & 1) << 7 | 0x7F) << shift;

                            put(src, dst_byte++);

                            while (raw == 0x7F)
                            {
                                if (src_byte == limit)
                                {
                                    write(src, dst_byte, State.Ox7F_);
                                    return src_bytes;
                                }

                                if (FF = (raw = src[src_byte++]) == 0xFF) //FF here is an error
                                {
                                    error_reset(OnError.BYTES_DISTORTION);
                                    goto write;
                                }

                                bits |= ((raw & 1) << 6 | 0x3F) << shift;
                                if ((shift += 7) < 8)
                                    continue;
                                shift -= 8;

                                put(src, dst_byte++);
                            }

                            bits |= (raw >> 1) << shift;
                            if ((shift += 7) < 8)
                                continue;
                            shift -= 8;
                        }
                        else if (raw == 0xFF) //mark
                        {
                            if (FF)
                            {
                                error_handler.error(this, OnError.FFFF_ERROR);
                                continue;
                            }

                            FF = true;
                            if (state == State.SEEK_FF) //can happence after any call of  put(src, dec_position++) that can  call >>> checkCrcThenDispatch >>> reset() so cleanup
                            {
                                reset();
                                state = State.NORMAL;
                            }
                            else
                                write(src, dst_byte, State.NORMAL);

                            continue;
                        }
                        else
                            bits |= raw << shift;

                        FF = false;
                        put(src, dst_byte++);
                    }

                    write(src, dst_byte, State.NORMAL);
                    return src_bytes;
                }

                private void put(byte[] dst, int dst_index)
                {
                    crc3 = crc2; //shift crc history
                    crc2 = crc1;
                    crc1 = crc0;

                    crc0 = crc16((byte)bits, crc1);
                    dst[dst_index] = (byte)bits;

                    bits >>= 8;
                }

                public void onReceiving(Receiver src, BytesDst dst) => handler.onReceiving(src, dst);

                public void onReceived(Receiver src, BytesDst pack)
                {
                    pack_crc = 0;
                    pack_crc_byte = CRC_LEN_BYTES - 1;
                    waiting_for_dispatching_pack = pack;
                    dispatch_on_0 = false;

                    while (0 < src.remaining && waiting_for_dispatching_pack != null)
                        getting_crc(src.get_byte());
                }

                private void write(byte[] src, int limit, State state_if_ok)
                {
                    state = state_if_ok;
                    if (limit == 0)
                        return; //no decoded bytes
                    var BYTE = 0;
                    while (waiting_for_dispatching_pack != null)
                    {
                        getting_crc(src[BYTE++]);
                        if (BYTE == limit)
                            return;
                    }

                    upper_layer.Write(src, BYTE, limit - BYTE);

                    if (upper_layer.mode == OK || !FF)
                        return;
                    error_reset(OnError.BYTES_DISTORTION); //not enough bytes to complete the current packet but already next pack frame detected. error
                }

                private BytesDst? waiting_for_dispatching_pack;

                private bool dispatch_on_0;

                private void getting_crc(int crc_byte)
                {
                    if (dispatch_on_0)
                    {
                        if (crc_byte == 0)
                            handler.onReceived(upper_layer, waiting_for_dispatching_pack!); //dispatching
                        else
                            error_handler.error(this, OnError.CRC_ERROR); //bad CRC
                        reset();
                        return;
                    }

                    pack_crc |= (ushort)(crc_byte << pack_crc_byte * 8);

                    pack_crc_byte--;
                    if (-1 < pack_crc_byte)
                        return; //need more

                    if (crc2 == pack_crc)
                        handler.onReceived(upper_layer, waiting_for_dispatching_pack!); //dispatching
                    else if (crc16((byte)(pack_crc >> 8), crc3) == crc2)
                    {
                        dispatch_on_0 = true;
                        return;
                    }
                    else
                        error_handler.error(this, OnError.CRC_ERROR); //bad CRC

                    reset();
                }

                private int bits;
                private int shift;
                private ushort pack_crc; //from packet crc
                private ushort crc0;     //calculated crc history
                private ushort crc1;
                private ushort crc2;
                private ushort crc3;
                private int pack_crc_byte;
                private int raw; //fix fetched byte
                private int dst_byte;
                private bool FF;
                private State state = State.SEEK_FF;

                private enum State
                {
                    NORMAL = 0,
                    Ox7F = 1,
                    Ox7F_ = 2,
                    SEEK_FF = 3
                }
            }
            #region Slot
            internal class Slot : Context.Receiver.Slot
            {
                public BytesDst dst;

                public int fields_nulls;
                public DST get_bytes<DST>() => (DST)next.dst;

                internal Slot next;
                internal readonly Slot? prev;

                public Slot(Receiver dst, Slot? prev) : base(dst)
                {
                    this.prev = prev;
                    if (prev != null)
                        prev.next = this;
                }
            }

            internal Slot? slot;
            internal WeakReference<Slot> slot_ref;
            #endregion

            public bool get_fields_nulls(uint this_case)
            {
                if (byte_ < byte_max)
                {
                    slot!.fields_nulls = buffer![byte_++];
                    return true;
                }

                slot!.state = this_case;
                mode = RETRY;
                return false;
            }

            public bool is_null(int field) => (slot!.fields_nulls & field) == 0;

            public bool byte_nulls(byte shift)
            {
                u4 = get_byte();
                if (u4 == 0)
                    return false;
                u8 = u8_ |= (ulong)u4 << shift;
                return true;
            }

            public bool byte_nulls(ulong null_value)
            {
                u4 = get_byte();
                if (u4 == 0)
                    return false;

                u8 = u8_ |= null_value;
                return true;
            }

            public bool byte_nulls(byte shift, ulong null_value)
            {
                u4 = get_byte();
                if (u4 == 0)
                    return false;

                u8 = u8_ |= u4 == 0xFF ?
                                null_value :
                                (ulong)u4 << shift;
                return true;
            }

            public bool idle() => slot == null;

            bool not_get4()
            {
                if (remaining < bytes_left)
                {
                    var r = remaining;
                    u4 |= get4<uint>(r) << (bytes_max - bytes_left) * 8;
                    bytes_left -= r;
                    return true;
                }

                u4 |= get4<uint>(bytes_left) << (bytes_max - bytes_left) * 8;
                return false;
            }

            public abstract BytesDst Receiving(int id); //throws Exception if wrong id

            public bool isOpen() => slot != null;

            public virtual void Close() => Reset();

            protected void Reset()
            {
                if (slot == null)
                    return;

                for (var s = slot; s != null; s = s.next)
                    s.dst = null;
                slot = null;

                if (chs != null)
                {
                    ArrayPool<char>.Shared.Return(chs);
                    chs = null;
                }

                buffer = null;
                mode = OK;
                bytes_left = bytes_max = id_bytes;
                u4 = 0;
                //dont set   u8 = 0; preserve probably a value pack data for framing layer.
                //dont set   str = null; preserve probably a value pack data for framing layer.
            }

            /**
             * write bytes
             * ATTENTION! The data in the provided buffer "src" may change due to buffer reuse.
             */
            public int Write(byte[] src, int src_byte, int src_bytes)
            {
                if (src_bytes < 1)
                    return 0;
                for (buffer = src, byte_max = (byte_ = src_byte) + src_bytes; byte_ < byte_max;)
                {
                    if (slot?.dst == null)
                        try
                        {
                            if (not_get4())
                                goto exit; //read id

                            var dst = Receiving((int)u4); //throws Exception if wrong id
                            if (slot == null && !slot_ref.TryGetTarget(out slot))
                                slot_ref = new WeakReference<Slot>(slot = new Slot(this, null));

                            slot.dst = dst;
                            bytes_left = bytes_max = id_bytes;
                            u4 = 0;
                            u8 = 0;
                            u8_ = 0;
                            slot.state = 0;
                            handler.onReceiving(this, dst);
                            if (slot == null)
                                return -1; //receiving event handler has reset this
                        }
                        catch (Exception ex)
                        {
                            Reset();
                            error_handler.error(this, OnError.INVALID_ID, ex);
                            break;
                        }
                    else //internal write
                        switch (mode)
                        {
                            case INT1:
                                if (not_get4())
                                    goto exit;
                                u8 = (ulong)(sbyte)u4;
                                break;
                            case INT2:
                                if (not_get4())
                                    goto exit;
                                u8 = (ulong)(short)u4;
                                break;
                            case INT4:
                                if (not_get4())
                                    goto exit;
                                u8 = (ulong)(int)u4;
                                break;
                            case VAL4:
                                if (not_get4())
                                    goto exit;
                                break;
                            case VAL8:

                                if (remaining < bytes_left)
                                {
                                    var r = remaining;
                                    u8 |= get8<ulong>(r) << (bytes_max - bytes_left) * 8;
                                    bytes_left -= r;
                                    goto exit;
                                }

                                u8 |= get8<ulong>(bytes_left) << (bytes_max - bytes_left) * 8;
                                break;
                            case LEN0:
                                if (not_get4())
                                    goto exit;
                                slot.check_len0((int)u4);
                                break;
                            case LEN1:
                                if (not_get4())
                                    goto exit;
                                slot.check_len1((int)u4);
                                break;
                            case LEN2:
                                if (not_get4())
                                    goto exit;
                                slot.check_len2((int)u4);
                                break;
                            case VARINT:
                                if (varint())
                                    break;
                                goto exit;

                            case STR:

                                if (!varint())
                                    goto exit;

                                if (u8_ == ulong.MaxValue)
                                    if (check_length_and_getting_string())
                                        break;
                                    else
                                        goto exit;

                                chs![u4++] = (char)u8;
                                if (getting_string())
                                    break;
                                goto exit;
                        }

                    mode = OK;

                    for (; ; )
                        if (!slot.dst!.__put_bytes(this))
                            goto exit; //data over
                        else
                        {
                            if (slot.prev == null)
                                break;
                            slot = slot.prev;
                        }

                    handler.onReceived(this, slot.dst); //dispatching

                    u4 = 0;
                    bytes_left = bytes_max = id_bytes;
                    if (slot == null)
                        return -1;   //received event handler has reset this
                    slot.dst = null; //preparing to read next packet data
                }

                if (slot != null && slot.dst == null)
                    Reset();

                exit:
                buffer = null;

                return byte_ - src_byte;
            }

            public DST get_bytes<DST>(DST dst)
                where DST : BytesDst
            {
                slot!.state = 0;
                dst.__put_bytes(this);
                return dst;
            }

            public DST? try_get_bytes<DST>(DST dst, uint next_case)
                where DST : class?, BytesDst
            {
                var s = slot!;

                (slot = s.next ?? (s.next = new Slot(this, s))).dst = dst;
                slot!.state = 0;
                u8_ = 0;
                if (dst.__put_bytes(this))
                {
                    slot = s;
                    return dst;
                }

                s.state = next_case;

                return null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void retry_at(uint the_case)
            {
                slot!.state = the_case;
                mode = RETRY;
            }

            public bool has_bytes(uint next_case)
            {
                if (byte_ < byte_max)
                    return true;
                mode = RETRY;
                slot!.state = next_case;
                return false;
            }

            public bool has_1bytes(uint get_case) => 0 < byte_max - byte_ || retry_get4(1, get_case);

            public sbyte get_sbyte_() => (sbyte)u4;
            public sbyte get_sbyte() => (sbyte)buffer![byte_++];
            public byte get_byte_() => (byte)u4;
            public byte get_byte() => buffer![byte_++];

            public bool has_2bytes(uint get_case) => 1 < byte_max - byte_ || retry_get4(2, get_case);

            public short get_short_() => (short)u4;

            public short get_short()
            {
                var ret = Endianness.OK.Int16(buffer!, byte_);
                byte_ += 2;
                return ret;
            }

            public ushort get_ushort_() => (ushort)u4;

            public ushort get_ushort()
            {
                var ret = Endianness.OK.UInt16(buffer!, byte_);
                byte_ += 2;
                return ret;
            }

            public bool has_4bytes(uint get_case) => 3 < byte_max - byte_ || retry_get4(4, get_case);

            public int get_int_() => (int)u4;

            public int get_int()
            {
                var ret = Endianness.OK.Int32(buffer!, byte_);
                byte_ += 4;
                return ret;
            }

            public uint get_uint_() => u4;

            public uint get_uint()
            {
                var ret = Endianness.OK.UInt32(buffer!, byte_);
                byte_ += 4;
                return ret;
            }

            public bool has_8bytes(uint get_case) => 7 < byte_max - byte_ || retry_get8(8, get_case);

            public long get_long_() => (long)u8;

            public long get_long()
            {
                var ret = Endianness.OK.Int64(buffer!, byte_);
                byte_ += 8;
                return ret;
            }

            public ulong get_ulong_() => u8;

            public ulong get_ulong()
            {
                var ret = Endianness.OK.UInt64(buffer!, byte_);
                byte_ += 8;
                return ret;
            }

            public double get_double()
            {
                var ret = BitConverter.UInt64BitsToDouble(Endianness.OK.UInt64(buffer!, byte_));
                byte_ += 8;
                return ret;
            }

            public double get_double_() => BitConverter.UInt64BitsToDouble(u8);

            public float get_float()
            {
                var ret = BitConverter.UInt32BitsToSingle(Endianness.OK.UInt32(buffer!, byte_));
                byte_ += 4;
                return ret;
            }

            public float get_float_() => BitConverter.UInt32BitsToSingle(u4);
            #region get_into_u8
            public bool get_sbyte_u8(uint get_case)
            {
                if (0 < byte_max - byte_)
                {
                    u8 = buffer![byte_++];
                    return true;
                }

                retry_get4(1, get_case);
                mode = INT1;
                return false;
            }

            public bool get_byte_u8(uint get_case)
            {
                if (byte_max - byte_ == 0)
                    return retry_get8(1, get_case);
                u8 = buffer![byte_++];
                return true;
            }

            public bool get_short_u8(uint get_case)
            {
                if (1 < byte_max - byte_)
                {
                    u8 = (ulong)Endianness.OK.Int16(buffer!, byte_);
                    byte_ += 2;
                    return true;
                }

                retry_get4(2, get_case);
                mode = INT2;
                return false;
            }

            public bool get_ushort_u8(uint get_case)
            {
                if (byte_max - byte_ < 2)
                    return retry_get8(2, get_case);
                u8 = Endianness.OK.UInt16(buffer!, byte_);
                byte_ += 2;
                return true;
            }

            public bool get_int_u8(uint get_case)
            {
                if (3 < byte_max - byte_)
                {
                    u8 = (ulong)Endianness.OK.Int32(buffer!, byte_);
                    byte_ += 4;
                    return true;
                }

                retry_get4(4, get_case);
                mode = INT4;
                return false;
            }

            public bool get_uint_u8(uint get_case)
            {
                if (byte_max - byte_ < 4)
                    return retry_get8(4, get_case);
                u8 = Endianness.OK.UInt32(buffer!, byte_);
                byte_ += 4;
                return true;
            }

            public bool get_long_u8(uint get_case)
            {
                if (byte_max - byte_ < 8)
                    return retry_get8(8, get_case);
                u8 = (ulong)Endianness.OK.Int64(buffer!, byte_);
                byte_ += 8;
                return true;
            }

            public bool get_ulong_u8(uint get_case)
            {
                if (byte_max - byte_ < 8)
                    return retry_get8(8, get_case);
                u8 = Endianness.OK.UInt64(buffer!, byte_);
                byte_ += 8;
                return true;
            }
            #endregion
            #region 8
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool try_get8(int bytes, uint get8_case)
            {
                if (remaining < bytes)
                    return retry_get8(bytes, get8_case);
                u8 = get8<ulong>(bytes);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool retry_get8(int bytes, uint get8_case)
            {
                bytes_left = (bytes_max = bytes) - remaining;
                u8 = get8<ulong>(remaining);
                slot!.state = get8_case;
                mode = VAL8;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public T get8<T>(int bytes)
            {
                ulong u8 = 0;
                byte_ += bytes;
                switch (bytes)
                {
                    case 8:
                        u8 = Endianness.OK.UInt64(buffer!, byte_ - 8);
                        break;
                    case 7:
                        u8 = Endianness.OK.UInt32(buffer!, byte_ - 7);
                        u8 |= (ulong)Endianness.OK.UInt16(buffer!, byte_ - 3) << 32;
                        u8 |= (ulong)buffer![byte_ - 1] << 48;
                        break;
                    case 6:
                        u8 = Endianness.OK.UInt32(buffer!, byte_ - 6);
                        u8 |= (ulong)Endianness.OK.UInt16(buffer!, byte_ - 2) << 32;
                        break;
                    case 5:
                        u8 = Endianness.OK.UInt32(buffer!, byte_ - 5);
                        u8 |= (ulong)buffer![byte_ - 1] << 32;
                        break;
                    case 4:
                        u8 = Endianness.OK.UInt32(buffer!, byte_ - 4);
                        break;
                    case 3:
                        u8 = Endianness.OK.UInt16(buffer!, byte_ - 3);
                        u8 |= (ulong)buffer![byte_ - 1] << 16;
                        break;
                    case 2:
                        u8 = Endianness.OK.UInt16(buffer!, byte_ - 2);
                        break;
                    case 1:
                        u8 = buffer![byte_ - 1];
                        break;
                }

                return Unsafe.As<ulong, T>(ref u8);
            }
            #endregion
            #region 4
            public bool try_get4(uint next_case) => try_get4(bytes_left, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool try_get4(int bytes, uint next_case)
            {
                if (remaining < bytes)
                    return retry_get4(bytes, next_case);
                u4 = get4<uint>(bytes);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool retry_get4(int bytes, uint get4_case)
            {
                bytes_left = (bytes_max = bytes) - remaining;
                u4 = get4<uint>(remaining);
                slot!.state = get4_case;
                mode = VAL4;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public T get4<T>() => Unsafe.As<uint, T>(ref u4);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public T get4<T>(int bytes)
            {
                uint u4 = 0;
                byte_ += bytes;
                switch (bytes)
                {
                    case 4:
                        u4 = Endianness.OK.UInt32(buffer!, byte_ - 4);
                        break;
                    case 3:
                        u4 = Endianness.OK.UInt16(buffer!, byte_ - 3);
                        u4 |= (uint)buffer![byte_ - 1] << 16;
                        break;
                    case 2:
                        u4 = Endianness.OK.UInt16(buffer!, byte_ - 2);
                        break;
                    case 1:
                        u4 = buffer![byte_ - 1];
                        break;
                }

                return Unsafe.As<uint, T>(ref u4);
            }
            #endregion
            #region bits
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void init_bits()
            {
                bits = 0;
                bit = 8;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public T get_bits<T>() => Unsafe.As<uint, T>(ref u4);

            public T get_bits<T>(int len_bits)
            {
                uint ret;
                if (bit + len_bits < 9)
                {
                    ret = bits >> bit & 0xFFU >> 8 - len_bits;
                    bit += len_bits;
                }
                else
                {
                    ret = (bits >> bit | (bits = buffer![byte_++]) << 8 - bit) & 0xFFU >> 8 - len_bits;
                    bit = bit + len_bits - 8;
                }

                return Unsafe.As<uint, T>(ref ret);
            }

            public bool try_get_bits(int len_bits, uint this_case)
            {
                if (bit + len_bits < 9)
                {
                    u4 = bits >> bit & 0xFFU >> 8 - len_bits;
                    bit += len_bits;
                }
                else if (byte_ < byte_max)
                {
                    u4 = (bits >> bit | (bits = buffer![byte_++]) << (8 - bit)) & 0xFFU >> 8 - len_bits;
                    bit = bit + len_bits - 8;
                }
                else //not enough data
                {
                    retry_at(this_case);
                    return false;
                }

                return true;
            }
            #endregion
            #region varint
            public bool try_get8(uint next_case) => try_get8(bytes_left, next_case);

            public bool try_get_varint_bits1(int bits, uint this_case)
            {
                if (!try_get_bits(bits, this_case))
                    return false;
                bytes_left = bytes_max = get_bits<int>() + 1;
                return true;
            }

            public bool try_get_varint_bits(int bits, uint this_case)
            {
                if (!try_get_bits(bits, this_case))
                    return false;
                bytes_left = bytes_max = get_bits<int>();
                return true;
            }

            public bool try_get_varint(uint next_case)
            {
                u8 = 0;
                bytes_left = 0;

                if (varint())
                    return true;

                slot!.state = next_case;
                mode = VARINT;
                return false;
            }

            private bool varint()
            {
                for (ulong b; byte_ < byte_max; u8 |= (b & 0x7FUL) << bytes_left, bytes_left += 7)
                    if ((b = buffer![byte_++]) < 0x80)
                    {
                        u8 |= b << bytes_left;
                        return true;
                    }

                return false;
            }

            public static long zig_zag(ulong src) => -(long)(src & 1) ^ (long)(src >> 1);
            #endregion
            #region dims
            private int[] dims = Array.Empty<int>(); //temporary buffer for the receiving string and more

            public void init_dims(int size)
            {
                u8 = 1;
                if (size <= dims.Length)
                    return;
                ArrayPool<int>.Shared.Return(dims);
                dims = ArrayPool<int>.Shared.Rent(size);
            }

            public int dim(int index) => dims[index];

            public void dim(int max, int index)
            {
                var dim = get4<int>();
                if (max < dim)
                    error_handler.error(this, OnError.OVERFLOW, new ArgumentOutOfRangeException("In dim(int max, int index){} max < dim : " + max + " < " + dim));

                u8 *= (ulong)dim;
                dims[index] = dim;
            }

            public int length(long max)
            {
                var len = get4<int>();
                if (len <= max)
                    return len;

                error_handler.error(this, OnError.OVERFLOW, new ArgumentOutOfRangeException("In length(long max){} max < len : " + max + " < " + len));
                u8 = 0;
                return 0;
            }
            #endregion
            #region string
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public string? get_string()
            {
                var ret = str;
                str = null;
                return ret;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool try_get_string(uint max_chars, int get_string_case)
            {
                u4 = max_chars;
                u8_ = ulong.MaxValue; //indicate state before string length received
                u8 = 0;              //varint receiving string char holde
                bytes_left = 0;              //varint pointer

                if (varint() && //getting string length into u8
                    check_length_and_getting_string())
                    return true;

                slot!.state = (uint)get_string_case;
                mode = STR; //lack of received bytes, switch to reading lines internally
                return false;
            }

            private char[]? chs;

            private bool check_length_and_getting_string()
            {
                if (u4 < u8)
                    error_handler.error(this, OnError.OVERFLOW, new ArgumentOutOfRangeException("In check_length_and_getting_string(){} u4 < u8 : " + u4 + " < " + u8));
                if (chs == null)
                    chs = ArrayPool<char>.Shared.Rent((int)u8);
                else if (chs.Length < (int)u8)
                {
                    ArrayPool<char>.Shared.Return(chs);
                    chs = ArrayPool<char>.Shared.Rent((int)u8);
                }

                u8_ = u8; //store string length into u8_
                u4 = 0;  //index receiving char

                return getting_string();
            }

            private bool getting_string()
            {
                while (u4 < u8_)
                {
                    u8 = 0;
                    bytes_left = 0;
                    if (varint())
                        chs![u4++] = (char)u8;
                    else
                        return false;
                }

                str = new string(chs!, 0, (int)u4);
                return true;
            }
            #endregion

            public override string ToString()
            {
                if (slot == null)
                    return "";

                var s = slot;
                while (s.prev != null)
                    s = s.prev;
                var str = "";
                var offset = "";
                for (; s != slot; s = s.next, offset += "\t")
                    str += $"{offset}{s.dst.GetType()}\t{s.state}\n";

                str += $"{offset}{slot.dst.GetType()}\t{s.state}\n";

                return str;
            }
        }

        public abstract class Transmitter : Context.Transmitter, BytesSrc
        {
            public interface BytesSrc
            {
                bool __get_bytes(Transmitter dst);
                int __id { get; }
            }

            public interface EventsHandler
            {
                // Callback triggered after a packet is pulled from the sending queue, before sending from the internal (INT) to the external (EXT) layer.
                void onSending(Transmitter dst, BytesSrc src) { }

                // Callback triggered after a packet is sent from the INT to the EXT layer.
                // Note: This does not guarantee that all bytes of the packet have been transmitted by the socket.
                void onSent(Transmitter dst, BytesSrc src) { }
            }

            protected EventsHandler handler;
            public EventsHandler exchange(EventsHandler handler) => Interlocked.Exchange(ref this.handler, handler);

            public Transmitter(EventsHandler handler, int power_of_2_sending_queue_size = 5)
            {
                this.handler = handler;
                sending_ = new(power_of_2_sending_queue_size);
                sending_value = new(power_of_2_sending_queue_size);
                slot_ref = new(new Slot(this, null));
            }

            private Action<AdHoc.BytesSrc>? subscriber;

            public Action<AdHoc.BytesSrc>? subscribeOnNewBytesToTransmitArrive(Action<AdHoc.BytesSrc>? subscriber)
            {
                var tmp = this.subscriber;
                if ((this.subscriber = subscriber) != null && 0 < sending_.Count)
                    subscriber!.Invoke(this);
                return tmp;
            }
            #region sending
            public readonly RingBuffer<BytesSrc> sending_;
            public readonly RingBuffer<ulong> sending_value;
            private volatile int Lock;

            public bool send(BytesSrc src)
            {
                while (Interlocked.CompareExchange(ref Lock, 1, 0) != 0)
                    Thread.SpinWait(10);
                if (sending_.Put(src))
                {
                    Lock = 0;
                    subscriber?.Invoke(this);
                    return true;
                }

                Lock = 0;
                return false;
            }

            public bool send(BytesSrc src, ulong pack)
            {
                while (Interlocked.CompareExchange(ref Lock, 1, 0) != 0)
                    Thread.SpinWait(10);

                if (sending_.Put(src))
                {
                    sending_value.Put(pack);
                    Lock = 0;
                    subscriber?.Invoke(this);
                    return true;
                }

                Lock = 0;
                return false;
            }
            #endregion
            #region value_pack transfer
            public void pull_value() => sending_value.Get(ref u8);

            public bool put_bytes(ulong src, BytesSrc handler, uint next_case)
            {
                u8 = src;
                return put_bytes(handler, next_case);
            }

            public void put_bytes(ulong src, BytesSrc handler)
            {
                u8 = src;
                put_bytes(handler);
            }

            public void put_bytes(BytesSrc src)
            {
                slot!.state = 1; //skip write id
                src.__get_bytes(this);
            }

            public bool put_bytes(BytesSrc src, uint next_case)
            {
                var s = slot;

                (slot = s.next ?? (s.next = new Slot(this, s))).src = src;
                slot.state = 1; //skip write id

                if (src.__get_bytes(this))
                {
                    slot = s;
                    return true;
                }

                s.state = next_case;
                return false;
            }
            #endregion

            public class Framing : AdHoc.BytesSrc, EventsHandler
            {
                public bool isOpen() => upper_layer.isOpen();
                public Transmitter upper_layer; //the upper layer external interface
                public EventsHandler handler;     //interface to the upper level internal producer

                public EventsHandler exchange(EventsHandler handler) => Interlocked.Exchange(ref this.handler, handler);

                public Framing(Transmitter upper_layer) => switch_to(upper_layer);

                public void switch_to(Transmitter upper_layer)
                {
                    bits = 0;
                    shift = 0;
                    crc = 0;
                    if (this.upper_layer != null)
                    {
                        this.upper_layer.Reset();
                        this.upper_layer.exchange(handler);
                    }

                    handler = (this.upper_layer = upper_layer).exchange(this);
                }

                private int enc_position; //where start to put encoded bytes
                private int raw_position; //start position for temporarily storing raw bytes from the upper layer

                private bool allocate_raw_bytes_space(int limit)
                {
                    //divide free space.
                    raw_position = enc_position +
                                   1 +    //for 0xFF byte - frame start mark.
                                   (limit - enc_position) / 8 +    //ensure enough space for encoded bytes in a worse case
                                   CRC_LEN_BYTES + 2; //guaranty space for CRC + its expansion

                    return raw_position < limit;
                }

                public void Close()
                {
                    Reset();
                    upper_layer.Close();
                }

                protected void Reset()
                {
                    upper_layer.Reset();
                    bits = 0;
                    shift = 0;
                    crc = 0;
                }

                public int Read(byte[] dst, int dst_byte, int dst_bytes)
                {
                    enc_position = dst_byte;
                    var limit = dst_byte + dst_bytes;

                    while (allocate_raw_bytes_space(limit))
                    {
                        var fix = raw_position;
                        var len = upper_layer.Read(dst, raw_position, limit - raw_position);

                        if (len < 1)
                            return dst_byte < enc_position ?
                                       enc_position - dst_byte :
                                       len;

                        for (var max = fix + len; raw_position < max;)
                            enc_position = encode(dst[raw_position++], dst, enc_position);
                    }

                    return dst_byte < enc_position ?
                               enc_position - dst_byte :
                               0;
                }

                public void onSending(Transmitter dst, BytesSrc src)
                {
                    handler.onSending(dst, src);
                    dst.buffer![enc_position++] = 0xFF; //write starting frame byte
                }

                public void onSent(Transmitter dst, BytesSrc src)
                {
                    while (raw_position < dst.byte_)
                        enc_position = encode(dst.buffer![raw_position++], dst.buffer, enc_position);

                    //the packet sending completed write crc
                    int fix = crc; //crc will continue counting on  encode() calling , so fix it
                    enc_position = encode(fix & 0xFF, dst.buffer!, encode(fix >> 8 & 0xFF, dst.buffer!, enc_position));
                    if (0 < shift)
                    {
                        dst.buffer![enc_position++] = (byte)bits;
                        if (bits == 0x7F)
                            dst.buffer![enc_position++] = 0;
                    }

                    if (allocate_raw_bytes_space(dst.byte_max))
                        dst.byte_ = raw_position;
                    else
                        dst.byte_max = raw_position = dst.byte_; //no more space. prevent continue

                    bits = 0;
                    shift = 0;
                    crc = 0;
                    handler.onSent(dst, src);
                }

                private int encode(int src, byte[] dst, int dst_byte)
                {
                    crc = crc16((byte)src, crc);
                    var v = (bits |= src << shift) & 0xFF;
                    if ((v & 0x7F) == 0x7F)
                    {
                        dst[dst_byte++] = 0x7F;
                        bits >>= 7;
                        if (shift < 7)
                            shift++;
                        else //a full byte in enc_bits
                        {
                            if ((bits & 0x7F) == 0x7F)
                            {
                                dst[dst_byte++] = 0x7F;
                                bits >>= 7;
                                shift = 1;
                                return dst_byte;
                            }

                            dst[dst_byte++] = (byte)bits;
                            shift = 0;
                            bits = 0;
                        }

                        return dst_byte;
                    }

                    dst[dst_byte++] = (byte)v;
                    bits >>= 8;
                    return dst_byte;
                }

                public Action<AdHoc.BytesSrc>? subscribeOnNewBytesToTransmitArrive(Action<AdHoc.BytesSrc>? subscriber) => upper_layer.subscribeOnNewBytesToTransmitArrive(subscriber);
                private int bits;
                private int shift;
                private ushort crc;
            }
            #region Slot
            internal sealed class Slot : Context.Transmitter.Slot
            {
                internal BytesSrc src;

                internal int fields_nulls;

                internal Slot? next;
                internal readonly Slot? prev;

                public Slot(Transmitter src, Slot? prev) : base(src)
                {
                    this.prev = prev;
                    if (prev != null)
                        prev.next = this;
                }
            }

            internal WeakReference<Slot> slot_ref;
            internal Slot? slot;
            #endregion

            public bool init_fields_nulls(int field0_bit, uint this_case)
            {
                if (!Allocate(1, this_case))
                    return false;
                slot!.fields_nulls = field0_bit;
                return true;
            }

            public void set_fields_nulls(int field) { slot!.fields_nulls |= field; }

            public void flush_fields_nulls() { put((byte)slot!.fields_nulls); }

            public bool is_null(int field) => (slot!.fields_nulls & field) == 0;

            public bool isOpen() => slot != null;

            public virtual void Close() => Reset();

            protected void Reset()
            {
                if (slot == null)
                    return;

                for (var s = slot; s != null; s = s.next)
                    s.src = null;
                slot = null;
                sending_.Clear();
                sending_value.Clear();
                buffer = null;
                mode = OK;
                u4 = 0;
                bytes_left = 0; //requires correct bitwise sending
            }

            //if dst == null - clean / reset state
            //
            //if 0 < return - bytes read
            //if return == 0 - not enough space in provided buffer dst available
            //if return == -1 -  no more packets left

            public int Read(byte[] dst, int dst_byte, int dst_bytes)
            {
                if (dst_bytes < 1)
                    return 0;

                for (buffer = dst, byte_max = (byte_ = dst_byte) + dst_bytes; byte_ < byte_max;)
                {
                    if (slot?.src == null)
                    {
                        BytesSrc? src = null;

                        if (!sending_.Get(ref src))
                        {
                            Reset();
                            goto exit;
                        }

                        if (slot == null && !slot_ref.TryGetTarget(out slot))
                            slot_ref = new WeakReference<Slot>(slot = new Slot(this, null));

                        slot.src = src;
                        slot.state = 0; //write id request
                        u4 = 0;
                        bytes_left = 0;
                        handler.onSending(this, src);
                        if (slot == null)
                            return -1; //sending event handler has reset this
                    }
                    else
                        switch (mode) //the packet transmission was interrupted, recall where we stopped
                        {
                            case STR:
                                if (!varint())
                                    goto exit;

                                if (u4 == uint.MaxValue)
                                    u4 = 0;

                                while (u4 < str!.Length)
                                    if (!varint(str[(int)u4++]))
                                        goto exit;

                                str = null;
                                break;
                            case VAL4:
                                if (byte_max - byte_ < bytes_left)
                                    goto exit;
                                put_val(u4, bytes_left);
                                break;
                            case VAL8:
                                if (byte_max - byte_ < bytes_left)
                                    goto exit;
                                put_val(u8, bytes_left);
                                break;
                            case BITS_BYTES:
                                if (byte_max - byte_ < bits_transaction_bytes_)
                                    goto exit;     //space for one full transaction
                                bits_byte = byte_; //preserve space for bits info
                                byte_++;
                                put_val(u8, bytes_left);
                                break;
                            case VARINT:
                                if (varint())
                                    break;
                                goto exit;
                            case BITS:
                                if (byte_max - byte_ < bits_transaction_bytes_)
                                    goto exit;     //space for one full transaction
                                bits_byte = byte_; //preserve space for bits info
                                byte_++;
                                break;
                        }

                    mode = OK; //restore the state
                    for (; ; )
                        if (!slot!.src!.__get_bytes(this))
                            goto exit;
                        else
                        {
                            if (slot.prev == null)
                                break;
                            slot = slot.prev;
                        }

                    handler.onSent(this, slot.src);
                    if (slot == null)
                        return -1;   //sent event handler has reset this
                    slot.src = null; //sing of the request next packet
                }

                if (slot != null && slot.src == null)
                    slot = null;

                exit:
                buffer = null;
                return dst_byte < byte_ ?
                           byte_ - dst_byte :
                           -1; //no more packets left
            }

            public bool Allocate(uint bytes, uint this_case)
            {
                slot!.state = this_case;
                if (bytes <= remaining)
                    return true;
                mode = RETRY;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(bool src) => put_bits(src ?
                                                      1 :
                                                      0, 1);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(bool? src) => put_bits(src.HasValue ?
                                                       src.Value ?
                                                           3 :
                                                           2 :
                                                       0, 2);
            #region bits
            private int bits_byte = -1;
            private uint bits_transaction_bytes_;

            public bool init_bits_(uint transaction_bytes, uint this_case)
            {
                if ((bits_transaction_bytes_ = transaction_bytes) <= byte_max - byte_)
                    return true;
                slot!.state = this_case;
                byte_ = bits_byte; //trim byte at bits_byte index
                mode = BITS;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool init_bits(uint transaction_bytes, uint this_case)
            {
                if (byte_max - byte_ < (bits_transaction_bytes_ = transaction_bytes))
                {
                    slot.state = this_case;
                    mode = RETRY;
                    return false;
                }

                bits = 0;
                bit = 0;
                bits_byte = byte_++; //Allocate space
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put_bits(int src, int len_bits)
            {
                bits |= (uint)src << bit;
                if ((bit += len_bits) < 9)
                    return; //yes 9! not 8!  to avoid allocating the next byte after the current one is full. it might be redundant
                buffer![bits_byte] = (byte)bits;
                bits >>= 8;
                bit -= 8;
                bits_byte = byte_++;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put_bits(int src, int len_bits, uint continue_at_case)
            {
                bits |= (uint)src << bit;
                if ((bit += len_bits) < 9)
                    return true; //yes 9! not 8!  to avoid allocating the next byte after the current one is full. it might be redundant
                buffer![bits_byte] = (byte)bits;
                bits >>= 8;
                bit -= 8;
                if (byte_max - byte_ < bits_transaction_bytes_)
                {
                    slot!.state = continue_at_case;
                    return false;
                }

                bits_byte = byte_++;
                return true;
            }

            //end of varint mode called once per batch
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void end_bits()
            {
                if (0 < bit)
                    buffer![bits_byte] = (byte)bits;
                else
                    byte_ = bits_byte; //trim byte at bits_byte index. allocated, but not used
            }

            public bool put_nulls(int nulls, int nulls_bits, uint continue_at_case)
            {
                if (put_bits(nulls, nulls_bits, continue_at_case))
                    return true;

                mode = BITS;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void continue_bits_at(uint continue_at_case)
            {
                slot!.state = continue_at_case;
                byte_ = bits_byte;
                mode = BITS;
            }
            #endregion

            public bool put_bits_bytes(int info, int info_bits, ulong value, int value_bytes, uint continue_at_case)
            {
                if (put_bits(info, info_bits, continue_at_case))
                {
                    put_val(value, value_bytes);
                    return true;
                }

                u8 = value;
                bytes_left = value_bytes;
                mode = BITS_BYTES;
                return false;
            }
            #region varint
            private static int bytes1(ulong src) => src < 1 << 8 ?
                                                        1 :
                                                        2;

            public bool put_varint21(ulong src, uint continue_at_case)
            {
                var bytes = bytes1(src);
                return put_bits_bytes(bytes - 1, 1, src, bytes, continue_at_case);
            }

            public bool put_varint21(ulong src, uint continue_at_case, int nulls, int nulls_bits)
            {
                var bytes = bytes1(src);
                return put_bits_bytes(bytes - 1 << nulls_bits | nulls, nulls_bits + 1, src, bytes, continue_at_case);
            }

            private static int bytes2(ulong src) => src < 1 << 8 ? 1
                                                    : src < 1 << 16 ? 2
                                                                      : 3;

            public bool put_varint32(ulong src, uint continue_at_case)
            {
                var bytes = bytes2(src);
                return put_bits_bytes(bytes, 2, src, bytes, continue_at_case);
            }

            public bool put_varint32(ulong src, uint continue_at_case, int nulls, int nulls_bits)
            {
                var bytes = bytes2(src);
                return put_bits_bytes(bytes << nulls_bits | nulls, nulls_bits + 2, src, bytes, continue_at_case);
            }

            private static int bytes3(ulong src) => src < 1L << 16 ? src < 1L << 8 ?
                                                                         1 :
                                                                         2
                                                    : src < 1L << 24 ? 3
                                                                       : 4;

            public bool put_varint42(ulong src, uint continue_at_case)
            {
                var bytes = bytes3(src);
                return put_bits_bytes(bytes - 1, 2, src, bytes, continue_at_case);
            }

            public bool put_varint42(ulong src, uint continue_at_case, int nulls, int nulls_bits)
            {
                var bytes = bytes3(src);
                return put_bits_bytes(bytes - 1 << nulls_bits | nulls, nulls_bits + 2, src, bytes, continue_at_case);
            }

            private static int bytes4(ulong src) => src < 1 << 24 ? src < 1 << 16 ?
                                                                        src < 1 << 8 ?
                                                                            1 :
                                                                            2 :
                                                                        3
                                                    : src < 1L << 32 ? 4
                                                    : src < 1L << 40 ? 5
                                                    : src < 1L << 48 ? 6
                                                                       : 7;

            public bool put_varint73(ulong src, uint continue_at_case)
            {
                var bytes = bytes4(src);
                return put_bits_bytes(bytes, 3, src, bytes, continue_at_case);
            }

            public bool put_varint73(ulong src, uint continue_at_case, int nulls, int nulls_bits)
            {
                var bytes = bytes4(src);
                return put_bits_bytes(bytes << nulls_bits | nulls, nulls_bits + 3, src, bytes, continue_at_case);
            }

            private static int bytes5(ulong src) => src < 1L << 32 ? src < 1 << 16 ? src < 1 << 8 ?
                                                                                         1 :
                                                                                         2
                                                                     : src < 1 << 24 ? 3
                                                                                       : 4
                                                    : src < 1L << 48 ? src < 1L << 40 ?
                                                                           5 :
                                                                           6
                                                    : src < 1L << 56 ? 7
                                                                       : 8;

            public bool put_varint83(ulong src, uint continue_at_case)
            {
                var bytes = bytes5(src);
                return put_bits_bytes(bytes - 1, 3, src, bytes, continue_at_case);
            }

            public bool put_varint83(ulong src, uint continue_at_case, int nulls, int nulls_bits)
            {
                var bytes = bytes5(src);
                return put_bits_bytes(bytes - 1 << nulls_bits | nulls, nulls_bits + 3, src, bytes, continue_at_case);
            }

            public bool put_varint84(ulong src, uint continue_at_case)
            {
                var bytes = bytes5(src);
                return put_bits_bytes(bytes, 4, src, bytes, continue_at_case);
            }

            public bool put_varint84(ulong src, uint continue_at_case, int nulls, int nulls_bits)
            {
                var bytes = bytes5(src);
                return put_bits_bytes(bytes << nulls_bits | nulls, nulls_bits + 4, src, bytes, continue_at_case);
            }

            public bool put_varint(ulong src, uint next_case)
            {
                if (varint(src))
                    return true;

                slot!.state = next_case;
                mode = VARINT;
                return false;
            }

            private bool varint() => varint(u8_);

            private bool varint(ulong src)
            {
                for (; byte_ < byte_max; buffer![byte_++] = (byte)(0x80 | src), src >>= 7)
                    if (src < 0x80)
                    {
                        buffer![byte_++] = (byte)src;
                        return true;
                    }

                u8_ = src;
                return false;
            }

            public static ulong zig_zag(long src, int right) => (ulong)(src << 1 ^ src >> right);
            #endregion

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put_val(ulong src, int bytes, uint next_field_case)
            {
                if (remaining < bytes)
                {
                    put(src, bytes, next_field_case);
                    return false;
                }

                put_val(src, bytes);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put_val(ulong src, int bytes)
            {
                byte_ += bytes;
                switch (bytes)
                {
                    case 8:
                        Endianness.OK.UInt64(src, buffer!, byte_ - 8);
                        return;
                    case 7:
                        Endianness.OK.UInt32((uint)src, buffer!, byte_ - 7);
                        Endianness.OK.UInt16((ushort)(src >> 32), buffer!, byte_ - 3);
                        buffer![byte_ - 1] = (byte)(src >> 48);
                        return;
                    case 6:
                        Endianness.OK.UInt32((uint)src, buffer!, byte_ - 6);
                        Endianness.OK.UInt16((ushort)(src >> 32), buffer!, byte_ - 2);
                        return;
                    case 5:
                        Endianness.OK.UInt32((uint)src, buffer!, byte_ - 5);
                        buffer![byte_ - 1] = (byte)(src >> 32);
                        return;
                    case 4:
                        Endianness.OK.UInt32((uint)src, buffer!, byte_ - 4);
                        return;
                    case 3:
                        Endianness.OK.UInt16((ushort)src, buffer!, byte_ - 3);
                        buffer![byte_ - 1] = (byte)(src >> 16);
                        return;
                    case 2:
                        Endianness.OK.UInt16((ushort)src, buffer!, byte_ - 2);
                        return;
                    case 1:
                        buffer![byte_ - 1] = (byte)src;
                        return;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put_val(uint src, int bytes, uint next_field_case)
            {
                if (remaining < bytes)
                {
                    put(src, bytes, next_field_case);
                    return false;
                }

                put_val(src, bytes);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put_val(uint src, int bytes)
            {
                byte_ += bytes;
                switch (bytes)
                {
                    case 4:
                        Endianness.OK.UInt32(src, buffer!, byte_ - 4);
                        return;
                    case 3:
                        Endianness.OK.UInt16((ushort)src, buffer!, byte_ - 3);
                        buffer![byte_ - 1] = (byte)(src >> 16);
                        return;
                    case 2:
                        Endianness.OK.UInt16((ushort)src, buffer!, byte_ - 2);
                        return;
                    case 1:
                        buffer![byte_ - 1] = (byte)src;
                        return;
                }
            }

            public bool put(string src, uint next_case)
            {
                u4 = uint.MaxValue; //indicate state before string length send
                if (!varint((ulong)src.Length))
                    goto exit;
                u4 = 0; //indicate state after string length sent

                while (u4 < src.Length)
                    if (!varint(src[(int)u4++]))
                        goto exit;
                return true;

            exit:
                slot!.state = next_case;
                str = src; //switch to sending internally
                mode = STR;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            private void put(uint src, int bytes, uint next_case)
            {
                slot!.state = next_case;
                bytes_left = bytes;
                u4 = src;
                mode = VAL4;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            private void put(ulong src, int bytes, uint next_case)
            {
                slot!.state = next_case;
                bytes_left = bytes;
                u8 = src;
                mode = VAL8;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void retry_at(uint the_case)
            {
                slot!.state = the_case;
                mode = RETRY;
            }

            public int bytes4value(int value) => value < 0xFFFF ? value < 0xFF ?
                                                                      value == 0 ?
                                                                          0 :
                                                                          1 :
                                                                      2
                                                 : value < 0xFFFFFF ? 3
                                                                      : 4;

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(sbyte src) => buffer![byte_++] = (byte)src;

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(sbyte? src) => buffer![byte_++] = (byte)src!.Value;

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(sbyte src, uint next_case) => put((byte)src, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(sbyte? src, uint next_case) => put((byte)src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(byte src) => buffer![byte_++] = src;

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(byte? src) => buffer![byte_++] = src!.Value;

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(byte? src, uint next_case) => put(src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(byte src, uint next_case)
            {
                if (byte_ < byte_max)
                {
                    put(src);
                    return true;
                }

                put(src, 1, next_case);
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(short? src, uint next_case) => put((ushort)src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(short src, uint next_case) => put((ushort)src, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(ushort? src, uint next_case) => put(src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(ushort src, uint next_case)
            {
                if (remaining < 2)
                {
                    put(src, 2, next_case);
                    return false;
                }

                put(src);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(short src) => put((ushort)src);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(short? src) => put((ushort)src!.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(ushort? src) => put(src!.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(ushort src)
            {
                Endianness.OK.UInt16(src, buffer!, byte_);
                byte_ += 2;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(int src, uint next_case) => put((uint)src, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(int? src, uint next_case) => put((uint)src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(float src, uint next_case) => put(Unsafe.As<float, uint>(ref src), next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(float? src, uint next_case)
            {
                var f = src!.Value;
                return put(Unsafe.As<float, uint>(ref f), next_case);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(uint? src, uint next_case) => put(src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(uint src, uint next_case)
            {
                if (remaining < 4)
                {
                    put(src, 4, next_case);
                    return false;
                }

                put(src);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(int src) => put((uint)src);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(int? src) => put((uint)src!.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(float? src)
            {
                var f = src!.Value;
                put(Unsafe.As<float, uint>(ref f));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(float src) => put(Unsafe.As<float, uint>(ref src));

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(uint? src) => put(src!.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(uint src)
            {
                Endianness.OK.UInt32(src, buffer!, byte_);
                byte_ += 4;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(long src, uint next_case) => put((ulong)src, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(long? src, uint next_case) => put((ulong)src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(double src, uint next_case) => put(Unsafe.As<double, ulong>(ref src), next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(double? src, uint next_case)
            {
                var d = src!.Value;
                return put(Unsafe.As<double, ulong>(ref d), next_case);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(ulong? src, uint next_case) => put(src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(ulong src, uint next_case)
            {
                if (remaining < 8)
                {
                    put(src, 8, next_case);
                    return false;
                }

                put(src);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(double src) => put(Unsafe.As<double, ulong>(ref src));

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(double? src)
            {
                var d = src!.Value;
                put(Unsafe.As<double, ulong>(ref d));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(long src) => put((ulong)src);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(long? src) => put((ulong)src!.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(ulong? src) => put(src!.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(ulong src)
            {
                Endianness.OK.UInt64(src, buffer, byte_);
                byte_ += 8;
            }

            public override string ToString()
            {
                if (slot == null)
                    return "";
                var s = slot;
                while (s.prev != null)
                    s = s.prev;
                var str = "";
                var offset = "";
                for (; s != slot; s = s.next, offset += "\t")
                    str += $"{offset}{s.src.GetType()}\t{s.state}\n";

                str += $"{offset}{slot.src.GetType()}\t{s.state}\n";

                return str;
            }
        }

        public class ArrayEqualHash<T> : IEqualityComparer<IList<T>>
        {
            public bool Equals(IList<T>? x, IList<T>? y) => (x == null || y == null) ?
                                                                x == y :
                                                                x.Count == y.Count && x.SequenceEqual(y);

            public int GetHashCode(IList<T> list) => list.Aggregate(17, (current, item) => HashCode.Combine(current, item));
        }

        private static readonly ConcurrentDictionary<object, object> pool = new();

        public static IEqualityComparer<IList<T>> getArrayEqualHash<T>()
        {
            var t = typeof(T);
            if (pool.TryGetValue(t, out var value))
                return (IEqualityComparer<IList<T>>)value;
            var ret = new ArrayEqualHash<T>();
            pool[t] = ret;
            return ret;
        }

        interface Endianness
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public short Int16(byte[] src, int index);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public ushort UInt16(byte[] src, int index);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public int Int32(byte[] src, int index);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public uint UInt32(byte[] src, int index);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public long Int64(byte[] src, int index);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public ulong UInt64(byte[] src, int index);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void Int16(short src, byte[] dst, int index);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void UInt16(ushort src, byte[] dst, int index);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void Int32(int src, byte[] dst, int index);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void UInt32(uint src, byte[] dst, int index);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void Int64(long src, byte[] dst, int index);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void UInt64(ulong src, byte[] dst, int index);

            private class LE : Endianness
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public short Int16(byte[] src, int index) => Unsafe.ReadUnaligned<short>(ref src[index]);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public ushort UInt16(byte[] src, int index) => Unsafe.ReadUnaligned<ushort>(ref src[index]);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public int Int32(byte[] src, int index) => Unsafe.ReadUnaligned<int>(ref src[index]);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public uint UInt32(byte[] src, int index) => Unsafe.ReadUnaligned<uint>(ref src[index]);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public long Int64(byte[] src, int index) => Unsafe.ReadUnaligned<long>(ref src[index]);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public ulong UInt64(byte[] src, int index) => Unsafe.ReadUnaligned<ulong>(ref src[index]);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void Int16(short src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 2)), src);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void UInt16(ushort src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 2)), src);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void Int32(int src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 4)), src);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void UInt32(uint src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 4)), src);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void Int64(long src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 8)), src);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void UInt64(ulong src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 8)), src);
            }

            private class BE : Endianness
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public short Int16(byte[] src, int index) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<short>(ref src[index]));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public ushort UInt16(byte[] src, int index) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ref src[index]));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public int Int32(byte[] src, int index) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref src[index]));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public uint UInt32(byte[] src, int index) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref src[index]));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public long Int64(byte[] src, int index) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<long>(ref src[index]));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public ulong UInt64(byte[] src, int index) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref src[index]));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void Int16(short src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 2)), BinaryPrimitives.ReverseEndianness(src));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void UInt16(ushort src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 2)), BinaryPrimitives.ReverseEndianness(src));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void Int32(int src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 4)), BinaryPrimitives.ReverseEndianness(src));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void UInt32(uint src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 4)), BinaryPrimitives.ReverseEndianness(src));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void Int64(long src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 8)), BinaryPrimitives.ReverseEndianness(src));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void UInt64(ulong src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 8)), BinaryPrimitives.ReverseEndianness(src));
            }

            public static readonly Endianness OK = BitConverter.IsLittleEndian ?
                                                       new LE() :
                                                       new BE();
        }

        public struct NullableBool : IEquatable<NullableBool>
        {
            public NullableBool() { }

            public NullableBool(bool value) => Value = value;
            public NullableBool(byte value) => this.value = value;
            public byte value = NULL;

            public bool Value
            {
                get => value == 1;
                set => this.value = (byte)(value ?
                                               1 :
                                               0);
            }

            public bool hasValue => value != NULL;
            public void to_null() => value = NULL;

            public static bool operator ==(NullableBool? a, NullableBool? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.value == b!.Value.value);
            public static bool operator !=(NullableBool? a, NullableBool? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.value != b!.Value.value);

            public static bool operator ==(NullableBool a, NullableBool b) => a.value == b.value;
            public static bool operator !=(NullableBool a, NullableBool b) => a.value != b.value;

            public static bool operator ==(NullableBool a, bool b) => a.value != NULL && a.value == (byte)(b ?
                                                                                                               1 :
                                                                                                               0);

            public static bool operator !=(NullableBool a, bool b) => a.value == NULL || a.value != (byte)(b ?
                                                                                                               1 :
                                                                                                               0);

            public static bool operator ==(bool a, NullableBool b) => b.value != NULL && b.value == (byte)(a ?
                                                                                                               1 :
                                                                                                               0);

            public static bool operator !=(bool a, NullableBool b) => b.value == NULL || b.value != (byte)(a ?
                                                                                                               1 :
                                                                                                               0);

            public override bool Equals(object? other) => other is NullableBool p && p.value == value;
            public bool Equals(NullableBool other) => value == other.value;
            public override int GetHashCode() => value.GetHashCode();

            public static explicit operator bool(NullableBool a) => a.Value;
            public static implicit operator NullableBool(bool a) => new NullableBool(a);

            public static implicit operator NullableBool(bool? a) => a == null ?
                                                                         NULL :
                                                                         a.Value;

            public static explicit operator byte(NullableBool a) => a.value;
            public static implicit operator NullableBool(byte a) => new NullableBool(a);
            public const byte NULL = 2;
        }

        //Decoding table for base64
        private static readonly byte[] char2byte = new byte[256];

        static AdHoc()
        {
            for (int i = 'A'; i <= 'Z'; i++)
                char2byte[i] = (byte)(i - 'A');
            for (int i = 'a'; i <= 'z'; i++)
                char2byte[i] = (byte)(i - 'a' + 26);
            for (int i = '0'; i <= '9'; i++)
                char2byte[i] = (byte)(i - '0' + 52);
            char2byte['+'] = 62;
            char2byte['/'] = 63;
        }

        ///<summary>
        ///Decodes base64 encoded bytes in place.
        ///</summary>
        ///<param name="bytes">The byte array containing the base64 encoded bytes.</param>
        ///<param name="src_index">The starting index in the source array to begin decoding.</param>
        ///<param name="dst_index">The starting index in the destination array to place decoded bytes.</param>
        ///<param name="len">The length of the base64 encoded bytes to decode.</param>
        ///<returns>The length of the decoded bytes.</returns>
        public static int base64decode(byte[] bytes, int src_index, int dst_index, int len)
        {
            var max = src_index + len;

            while (bytes[max - 1] == '=') { max--; }

            var new_len = max - src_index;
            for (var i = new_len >> 2; 0 < i; i--) //Process full 4-character blocks
            {
                var b = char2byte[bytes[src_index++]] << 18 |
                        char2byte[bytes[src_index++]] << 12 |
                        char2byte[bytes[src_index++]] << 6 |
                        char2byte[bytes[src_index++]];

                bytes[dst_index++] = (byte)(b >> 16);
                bytes[dst_index++] = (byte)(b >> 8);
                bytes[dst_index++] = (byte)b;
            }

            switch (new_len & 3)
            {
                case 3:
                    //If there are 3 characters remaining, decode them into 2 bytes
                    var b = char2byte[bytes[src_index++]] << 12 |
                            char2byte[bytes[src_index++]] << 6 |
                            char2byte[bytes[src_index]];
                    bytes[dst_index++] = (byte)(b >> 10); //Extract first byte
                    bytes[dst_index++] = (byte)(b >> 2);  //Extract second byte
                    break;
                case 2:
                    //If there are 2 characters remaining, decode them into 1 byte
                    bytes[dst_index++] = (byte)((char2byte[bytes[src_index++]] << 6 | char2byte[bytes[src_index]]) >> 4);
                    break;
            }

            return dst_index;
        }

        //Using DNS as readonly key-value storage https://datatracker.ietf.org/doc/html/rfc1035
        public static Memory<byte>[] value(string key)
        {
            byte[] Create_DNS_TXT_Record_Request(string domain)
            {
                var id = (ushort)new Random().Next(65536); //Generate a random query ID

                var request = new byte[12 + domain.Length + 2 + 4]; //Initialize the request packet

                //Set DNS header fields
                request[0] = (byte)(id >> 8);
                request[1] = (byte)(id & 0xFF);
                request[2] = 0x01; //QR=0, OPCODE=0, AA=0, TC=0, RD=1
                request[5] = 0x01; //QDCOUNT=1

                //Add the domain name to the question section
                var index = 12;
                var p = index++;

                foreach (var ch in domain)
                    if (ch == '.')
                    {
                        request[p] = (byte)(index - p - 1);
                        p = index++;
                    }
                    else
                        request[index++] = (byte)ch;

                request[p] = (byte)(index - p - 1); //Set the length for the last label

                index += 2;    //Terminate domain name, set question type (TXT) and class (IN)
                request[index++] = 0x10; //QTYPE = TXT
                request[++index] = 0x01; //QCLASS = IN

                return request;
            }

            static Memory<byte>[] Parse_DNS_TXT_Record_Response(byte[] response)
            {
                var questionCount = (response[4] << 8) | response[5]; //Extract question and answer counts from the header
                var answerCount = (response[6] << 8) | response[7];

                var index = 12;

                for (var i = 0; i < questionCount; i++, index += 5) //Skip the question section
                    while (response[index] != 0)
                        index += response[index] + 1;

                var dst_index = 0;
                var dst_index_ = 0;
                var records = new Memory<byte>[answerCount];
                for (var i = 0; i < answerCount; i++) //Parse each answer
                {
                    index += 2; //Skip NAME field
                    //TYPE            two octets containing one of the RR TYPE codes.
                    var TYPE = (ushort)((response[index] << 8) | response[index + 1]);
                    //CLASS           two octets containing one of the RR CLASS codes.
                    //
                    //TTL             a 32 bit signed integer that specifies the time interval
                    //                that the resource record may be cached before the source
                    //                of the information should again be consulted.  Zero
                    //                values are interpreted to mean that the RR can only be
                    //                used for the transaction in progress, and should not be
                    //                cached.  For example, SOA records are always distributed
                    //                with a zero TTL to prohibit caching.  Zero values can
                    //                also be used for extremely volatile data.
                    index += 8;                                                          //Skip all above
                    var RDLENGTH = (ushort)(response[index] << 8 | response[index + 1]); //an unsigned 16 bit integer that specifies the length in  octets of the RDATA field.
                    index += 2;
                    //TXT-DATA        One or more <character-string>s. where <character-string> is a single length octet followed by that number of characters
                    //!!! attention records in reply may follow in arbitrary order

                    if (TYPE == 16) //TXT record
                        for (var max = index + RDLENGTH; index < max;)
                        {
                            var len = response[index++];
                            Array.Copy(response, index, response, dst_index, len);
                            dst_index += len;
                            index += len;
                        }

                    records[i] = new Memory<byte>(response, dst_index_, dst_index - dst_index_);
                    dst_index_ = dst_index;
                }

                return records;
            }

            var ep = new IPEndPoint(IPAddress.Any, 0);

            using (var udpClient = new UdpClient())
                foreach (var os_dns in NetworkInterface.GetAllNetworkInterfaces()
                                                       .Where(n => n.OperationalStatus == OperationalStatus.Up)
                                                       .SelectMany(n => n.GetIPProperties().DnsAddresses)
                                                       .ToArray())
                    try
                    {
                        var request = Create_DNS_TXT_Record_Request(key);

                        udpClient.Send(request, request.Length, os_dns.ToString(), 53);

                        var response = udpClient.Receive(ref ep);

                        return Parse_DNS_TXT_Record_Response(response);
                    }
                    catch (Exception e) { }

            return null;
        }

        // <summary>
        /// Calculates the number of bytes required to encode a span of characters using varint encoding.
        /// </summary>
        /// <param name="src">The span of characters to be encoded.</param>
        /// <returns>The total number of bytes required for varint encoding.</returns>
        public static int varint_bytes(ReadOnlySpan<char> src)
        {
            var bytes = 0;
            foreach (var ch in src)
                bytes += ch < 0x80 ?
                             1 :
                             ch < 0x4_000 ?
                                 2 :
                                 3;
            return bytes;
        }

        /// <summary>
        /// Counts the number of characters that can be represented by a span of bytes in varint encoding.
        /// </summary>
        /// <param name="src">The span of bytes in varint encoding.</param>
        /// <returns>The number of characters that can be represented by the input bytes.</returns>
        public static int varint_chars(ReadOnlySpan<byte> src)
        {
            var chars = 0;
            foreach (var b in src)
                if (b < 0x80)
                    chars++;
            return chars;
        }

        /// <summary>
        /// Encodes a portion of a string into a byte array using varint encoding.
        /// </summary>
        /// <param name="src">The source string to encode.</param>
        /// <param name="dst">The destination byte array.</param>
        /// <returns>
        /// A 64-bit unsigned integer containing two pieces of information:
        /// - High 32 bits: The index in the source string of the first character not processed (i.e., the next character to be encoded if the operation were to continue).
        /// - Low 32 bits: The number of bytes written to the destination array.
        ///
        /// To extract these values:
        /// - Next character to process: (int)(result >> 32)
        /// - Bytes written: (int)(result & 0xFFFFFFFF)
        /// </returns>
        public static ulong varint(ReadOnlySpan<char> src, Span<byte> dst)
        {
            var src_from = 0;
            var dst_from = 0;

            // Iterate through the source string, starting from the specified index
            for (int dst_max = dst.Length, src_max = src.Length, ch; src_from < src_max; src_from++)
                if ((ch = src[src_from]) < 0x80) // Most frequent case: ASCII characters (0-127) These characters are encoded as a single byte
                {
                    // Check if there's enough space in the destination array for 1 byte
                    if (dst_from == dst_max) break;

                    // Encode the character in 1 byte (no special encoding needed)
                    dst[dst_from++] = (byte)ch;
                }
                else if (ch < 0x4_000)
                {
                    // Check if there's enough space in the destination array for 2 bytes
                    if (dst_max - dst_from < 2) break;

                    // Encode the character in 2 bytes using varint encoding
                    dst[dst_from++] = (byte)(0x80 | ch); // First byte: Set the MSB and use 7 LSBs of ch
                    dst[dst_from++] = (byte)(ch >> 7);   // Second byte: Use the remaining 7 bits of ch
                }
                else // Less frequent case
                {
                    // Check if there's enough space in the destination array for 3 bytes
                    if (dst_max - dst_from < 3) break;

                    // Encode the character in 3 bytes using varint encoding
                    dst[dst_from++] = (byte)(0x80 | ch);      // First byte: Set the MSB and use 7 LSBs of ch
                    dst[dst_from++] = (byte)(0x80 | ch >> 7); // Second byte: Set the MSB and use next 7 bits of ch
                    dst[dst_from++] = (byte)(ch >> 14);       // Third byte: Use the remaining 2 bits of ch
                }

            // Return the result: high 32 bits contain the next character index to process,
            // low 32 bits contain the number of bytes written to the destination array
            return (ulong)(uint)src_from << 32 | (uint)dst_from;
        }

        /// <summary>
        /// Decodes a portion of a byte array into a string using varint decoding.
        /// </summary>
        /// <param name="src">The source byte array to decode.</param>
        /// <param name="ret">A 32-bit integer containing two pieces of information:
        ///     - Low 16 bits: The partial character value from a previous call (if any).
        ///     - High 16 bits: The number of bits already processed for the partial character.
        /// </param>
        /// <param name="dst">The StringBuilder to append the decoded characters to.</param>
        /// <returns>
        /// A 32-bit integer containing two pieces of information:
        /// - Low 16 bits: The partial character value (if decoding is incomplete).
        /// - High 8 bits: The number of bits processed for the partial character.
        /// This return value can be used as the 'ret' parameter in a subsequent call to continue decoding.
        /// </returns>
        public static int varint(ReadOnlySpan<byte> src, int ret, StringBuilder dst)
        {
            var src_from = 0;
            var dst_to = src.Length;
            // Extract the partial character and shift from the ret parameter
            var ch = ret & 0xFFFF;      // Low 16 bits: partial character value
            var s = (byte)(ret >> 16); // High 8 bits: number of bits already processed
            int b;
            while (src_from < dst_to)
                if ((b = src[src_from++]) < 0x80) // If the high bit is not set, this is the last byte of the character
                {
                    // Combine the partial character with the current byte and append to StringBuilder
                    dst.Append((char)(b << s | ch));
                    s = 0; // Reset the shift
                    ch = 0; // Reset the partial character
                }
                else // If the high bit is set, this is not the last byte of the character
                {
                    // Add the 7 bits of this byte to the partial character
                    ch |= (b & 0x7F) << s;
                    s += 7; // Increase the shift by 7 bits
                }

            // Return the current state (partial character and shift) for potential continuation
            return s << 16 | ch;
        }

        public static uint[] boyer_moore_pattern(string src)
        {
            var ret = new uint[src.Length];
            for (var i = src.Length; -1 < --i;)
                if (ret[i] == 0)
                    for (int ii = i, ch = src[i], p = i << 8 | ch; -1 < ii; ii--)
                        if (src[ii] == ch)
                            ret[ii] = (uint)p;
            return ret;
        }

        // Case-sensitive
        public static int boyer_moore_ASCII_Case_sensitive(byte[] bytes, uint[] pattern) //return pattern's last byte position in the `bytes`
        {
            for (int len = pattern.Length, i = len - 1, max = bytes.Length - len + 1; i < max;)
            {
                for (var j = len; -1 < --j;)
                {
                    var p = pattern[j];

                    if ((byte)p == bytes[i + j]) continue; // Compare characters

                    // Use the last occurrence to determine how far to skip
                    var last = p >>> 8; // Extract last occurrence position
                    i += (int)Math.Max(1, j - last);
                    goto next;
                }

                return i; //return found pattern's last byte position in the `bytes`
            next:;
            }

            return -1; // Pattern not found
        }

        // Case-insensitive
        public static int boyer_moore_ASCII_Case_insensitive(byte[] bytes, uint[] pattern) //return pattern's last byte position in the `bytes`
        {
            for (int len = pattern.Length, i = len - 1, max = bytes.Length - len + 1; i < max;)
            {
                for (var j = len; -1 < --j;)
                {
                    var p = pattern[j];

                    switch ((sbyte)p - bytes[i + j])
                    {
                        case 0:
                            continue;
                        case 32:
                            if ('a' <= p) continue;
                            break;
                        case -32:
                            if ('A' <= p) continue;
                            break;
                    }

                    // Use the last occurrence to determine how far to skip
                    var last = p >>> 8; // Extract last occurrence position
                    i += (int)Math.Max(1, j - last);
                    goto next;
                }

                return i; //return found pattern's last byte position in the `bytes`
            next:;
            }

            return -1; // Pattern not found
        }
    }
}