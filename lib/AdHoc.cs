// AdHoc protocol - data interchange format and source code generator
// Copyright 2020 Chikirev Sirguy, Unirail Group. All rights reserved.
// cheblin@gmail.org
// https://github.com/orgs/AdHoc-Protocol
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace org.unirail
{
    public class AdHoc
    {
        public interface EXT
        {
            public interface BytesSrc
            {
                int  Read(byte[] dst, int dst_byte, int dst_bytes);
                void Close(); //to network command
                bool isOpen();


                interface Producer
                {
                    void    subscribe(Action<BytesSrc>? subscriber, object? token);
                    object? token(object?               token);

                    object? token();
                }
            }

            public interface BytesDst
            {
                void Write(byte[] src, int src_byte, int src_bytes);
                void Close();
                bool isOpen();
            }
        }

        public interface INT
        {
            public interface BytesDst
            {
                BytesDst? put_bytes(Receiver src); 

                interface Consumer
                {
                    BytesDst? Receiving(Receiver src, int      id);
                    void      Received(Receiver  src, BytesDst dst);
                }
            }

            public interface BytesSrc
            {
                BytesSrc? get_bytes(Transmitter dst);

                interface Producer
                {
                    BytesSrc? Sending(Transmitter dst);
                    void      Sent(Transmitter    dst, BytesSrc src);
                }
            }
        }


        public static int trailingZeros(uint i)
        {
            var n = 7;
            i <<= 24;
            var y = i << 4;
            if (y != 0)
            {
                n -= 4;
                i =  y;
            }

            y = i << 2;
            return (int)(y == 0
                             ? n - (i     << 1 >> 31)
                             : n - 2 - (y << 1 >> 31));
        }

        internal const uint OK       = int.MaxValue,
                            STR      = OK       - 100,
                            DONE     = STR      + 1,
                            VAL4     = DONE     + 1,
                            VAL8     = VAL4     + 1,
                            LEN      = VAL8     + 1,
                            BASE_LEN = LEN      + 1,
                            BITS     = BASE_LEN + 1,
                            VARINTS  = BITS     + 1,
                            VARINT   = VARINTS  + 1;

        protected int bit;

        public object? obj;
        public string? str;
        public uint    bits;

        public byte[]? buffer;
        public int     BYTE; //position in buffer
        public int     len;  //buffer length

        public uint mode;

        public uint  u4;
        public ulong u8;

        public int bytes_left;

        public int remaining => len - BYTE;

        protected Assertion assertion = Assertion.UNEXPECT;

        protected enum Assertion
        {
            PACK_END  = 2, //End of a pack reached.
            SPACE_END = 1, //End of space reached
            UNEXPECT  = 0
        }

#region CRC
        private static readonly ushort[] tab = { 0, 4129, 8258, 12387, 16516, 20645, 24774, 28903, 33032, 37161, 41290, 45419, 49548, 53677, 57806, 61935 };

        // !!!! Https://github.com/redis/redis/blob/95b1979c321eb6353f75df892ab8be68cf8f9a77/src/crc16.c
        //Output for "123456789"     : 31C3 (12739)
        private static ushort crc16(byte src, ushort crc)
        {
            crc = (ushort)(tab[(crc >> 12 ^ src >> 4) & 0x0F] ^ crc << 4);
            return (ushort)(tab[(crc >> 12 ^ src & 0x0F) & 0x0F] ^ crc << 4);
        }
#endregion


        public class Receiver : AdHoc, EXT.BytesDst, Context.Provider
        {
            public           AdHoc.INT.BytesDst.Consumer? int_dst;
            private readonly int                          id_bytes;

            public Receiver(AdHoc.INT.BytesDst.Consumer? int_dst, int id_bytes)
            {
                this.int_dst = int_dst;
                bytes_left   = this.id_bytes = id_bytes;
            }


            public class UART : EXT.BytesDst
            {
                public void     Close()  => dst.Close();
                public bool     isOpen() => dst.isOpen();
                public Receiver dst;

                public UART(Receiver dst) { this.dst = dst; }

                private byte[] dec_array = new byte[3];

                public void Write(byte[]? src, int src_byte, int src_bytes)
                {
                    if (src == null)
                    {
                        dst.Write(null, 0, 0);
                        dec_bits  = 0;
                        dec_shift = 0;
                        dec_crc   = 0;
                        dec_bytes = 0;
                        dec_byte  = 0;
                        dec_state = State.NORMAL;
                        return;
                    }

                    //tood change to slice
                    if (src_byte < 1)
                    {
                        dec_array[1] = src[src_byte++];
                        src_bytes--;
                        write_int(dec_array, 1, 1);
                    }

                    write_int(src, src_byte, src_bytes);
                }

                //in-place decoding.
                // zero index is reserved for decoding purpose to prevent overlapping
                private int write_int(byte[] src, int get, int count)
                {
                    if (count < 1) return 0;
                    var limit = get + count;
                    var put   = 0;
                    switch (dec_state)
                    {
                        case State.SEEK_FF_SYNC: //bytes distortion was detected, skip bytes until FF sync mark
                            while (get < limit)
                                if (src[get++] == 0xFF)
                                {
                                    dec_state = State.NORMAL;
                                    if (get < limit) goto write;
                                    return count;
                                }

                            return count;
                        case State.PLACE1:
                            dec_bits |= (((dec_byte = src[get++]) & 1) << 7 | 0x7F) << dec_shift;
                            set(src, put++);
                            goto case State.PLACE2;
                        case State.PLACE2:
                            while (dec_byte == 0x7F)
                            {
                                if (!(get < limit))
                                {
                                    write(src, put, Assertion.SPACE_END, State.PLACE2, Error.BYTES_DISTORTION);
                                    return count;
                                }

                                dec_bits |= ((dec_byte = src[get++]) << 6 | 0x3F) << dec_shift;
                                if ((dec_shift += 7) < 8) continue;
                                dec_shift -= 8;
                                set(src, put++);
                            }

                            dec_state =  State.NORMAL;
                            dec_bits  |= (dec_byte >> 1) << dec_shift;
                            if ((dec_shift += 7) < 8) break;
                            dec_shift -= 8;
                            set(src, put++);
                            break;
                    }

                    write:
                    while (get < limit)
                    {
                        if ((dec_byte = src[get++]) == 0x7F)
                        {
                            if (!(get < limit))
                            {
                                write(src, put, Assertion.SPACE_END, State.PLACE1, Error.BYTES_DISTORTION);
                                return count;
                            }

                            dec_bits |= (((dec_byte = src[get++]) & 1) << 7 | 0x7F) << dec_shift;
                            set(src, put++);
                            while (dec_byte == 0x7F)
                            {
                                if (!(get < limit))
                                {
                                    write(src, put, Assertion.SPACE_END, State.PLACE2, Error.BYTES_DISTORTION);
                                    return count;
                                }

                                dec_bits |= (((dec_byte = src[get++]) & 1) << 6 | 0x3F) << dec_shift;
                                if ((dec_shift += 7) < 8) continue;
                                dec_shift -= 8;
                                set(src, put++);
                            }

                            dec_bits |= (dec_byte >> 1) << dec_shift;
                            if ((dec_shift += 7) < 8) continue;
                            dec_shift -= 8;
                        }
                        else if (dec_byte == 0xFF) //mark
                        {
                            dec_bits  = 0;
                            dec_shift = 0;
                            if (put + dec_bytes < dst.id_bytes)
                            {
                                dec_bytes = 0;
                                error_handler.error(Error.TOO_SHORT_PACK);
                                continue; //maybe bytes lost
                            }

                            dec_bytes = 0;
                            if ((src[put - 2] << 8 | src[put - 1]) == dec_fix_crc[put - 3 & 3]) //last two bytes is checksum. check it
                            {
                                write(src, put - 2, Assertion.PACK_END, State.NORMAL, Error.BYTES_DISTORTION);
                                put = 0;
                            }
                            else //bad CRC
                            {
                                error_handler.error(Error.CRC_ERROR);
                                dst.Write(null, 0, 0);
                            }

                            dec_state = State.NORMAL;
                            continue;
                        }
                        else dec_bits |= dec_byte << dec_shift;

                        set(src, put++);
                    }

                    write(src, put, Assertion.SPACE_END, State.NORMAL, Error.BYTES_DISTORTION);
                    return count;
                }

                private void write(byte[] src, int limit, Assertion assertion, State state_if_ok, Error error)
                {
                    dec_bytes     += limit; //fix the number of bytes already processed
                    dst.assertion =  assertion;
                    dst.Write(src, 0, limit);
                    if (dst.assertion != assertion)
                    {
                        error_handler.error(error);
                        dst.Write(null, 0, 0);
                        dec_state = State.SEEK_FF_SYNC;
                    }
                    else dec_state = state_if_ok;

                    dst.assertion = Assertion.UNEXPECT; //return to normal state
                }


                private void set(byte[] dst, int put)
                {
                    dec_fix_crc[put & 3] =   dec_crc = crc16((byte)dec_bits, dec_crc);
                    dst[put]             =   (byte)dec_bits;
                    dec_bits             >>= 8;
                }


                public ErrorHandler error_handler = ErrorHandler.DEFAULT;

                public enum Error { TOO_SHORT_PACK = 0, CRC_ERROR = 1, BYTES_DISTORTION = 3 }

                //todo replace with lambda?
                public interface ErrorHandler
                {
                    static ErrorHandler DEFAULT = new ToConsole();

                    class ToConsole : ErrorHandler
                    {
                        public void error(Error error)
                        {
                            switch (error)
                            {
                                case Error.TOO_SHORT_PACK:
                                    Console.Error.Write("====================TOO_SHORT_PACK");
                                    return;
                                case Error.CRC_ERROR:
                                    Console.Error.Write("===================CRC_ERROR");
                                    return;
                                case Error.BYTES_DISTORTION:
                                    Console.Error.Write("===================BYTES_DISTORTION");
                                    return;
                            }
                        }
                    }

                    void error(Error error);
                }

                private int      dec_bits;
                private int      dec_shift;
                private ushort   dec_crc;
                private ushort[] dec_fix_crc = new ushort[4]; //last 4 checksum
                private int      dec_bytes;                   //decoded bytes so fsr
                private int      dec_byte;                    //fix fetched byte

                private State dec_state = State.NORMAL;

                private enum State
                {
                    NORMAL       = 0,
                    PLACE1       = 1,
                    PLACE2       = 2,
                    SEEK_FF_SYNC = 3
                }
            }


#region Slot
            private class Slot
            {
                public uint                state;
                public AdHoc.INT.BytesDst? dst;

                public int  base_index;
                public int  base_index_max;
                public uint base_nulls;

                public int fields_nulls;

                public int  index     = 1;
                public int  index_max = 1;
                public uint items_nulls;


                public Slot? next;
                public Slot? prev;

                public Slot(Slot? prev)
                {
                    this.prev = prev;
                    if (prev != null) prev.next = this;
                }

                public override string ToString()
                {
                    var s                    = this;
                    while (s.prev != null) s = s.prev;
                    var str                  = "\n";
                    for (var i = 0;; i++)
                    {
                        for (var ii = i; 0 < ii; ii--) str += "\t";
                        str += s.dst.GetType() + "\n";
                        if (s == this) break;
                        s = s.next;
                    }

                    return str;
                }

                internal ContextExt? context;
            }

            private Slot?               slot;
            private WeakReference<Slot> slot_ref = new(new Slot(null));

            private void free_slot()
            {
                if (slot!.context != null)
                {
                    ctx          = slot.context.prev;
                    slot.context = null;
                }

                slot = slot.prev;
            }
#endregion

#region Context
            private class ContextExt : Context
            {
                internal AdHoc.INT.BytesDst? key;
                internal AdHoc.INT.BytesDst? value;

                internal string? key_string;
                internal ulong   key_long;

                public          ContextExt? next;
                public readonly ContextExt? prev;

                public ContextExt(ContextExt? prev)
                {
                    this.prev = prev;
                    if (prev != null) prev.next = this;
                }
            }

            private ContextExt?               ctx;
            private WeakReference<ContextExt> context_ref = new(new ContextExt(null));


            public Context context
            {
                get
                {
                    if (slot!.context != null) return slot.context;
                    if (ctx == null && !context_ref.TryGetTarget(out ctx)) context_ref = new WeakReference<ContextExt>(ctx = new ContextExt(null));
                    else if (ctx.next == null) ctx                                     = ctx.next = new ContextExt(ctx);
                    else ctx                                                           = ctx.next;
                    return slot.context = ctx;
                }
            }
#endregion

            public AdHoc.INT.BytesDst output
            {
                get
                {
                    var output = slot!.next!.dst;
                    slot.next.dst = null;
                    return output;
                }
            }

            public AdHoc.INT.BytesDst key
            {
                get
                {
                    var key = slot.context.key;
                    slot.context.key = null;
                    return key;
                }
                set => slot.context.key = value;
            }

            public AdHoc.INT.BytesDst value
            {
                get
                {
                    var value = slot.context.value;
                    slot.context.value = null;
                    return value;
                }
                set => slot.context.value = value;
            }

            public string? key_string { get => slot.context.key_string; set => slot.context.key_string = value; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public T key_get<T>() => Unsafe.As<ulong, T>(ref slot.context.key_long);

            public void key_set<T>(T key) => slot.context.key_long = Unsafe.As<T, ulong>(ref key);

            public bool get_info(uint the_case)
            {
                if (0 < remaining)
                {
                    _                       = context;
                    slot!.context!.key_long = (ulong)buffer![BYTE++] << 32;
                    return true;
                }

                retry_at(the_case);
                return false;
            }

            public bool hasNullKey() => (key_get<ulong>() >> 39 & 1) == 1;

            public bool hasNullKey(uint key_val_case, uint end_case)
            {
                if (hasNullKey()) return true;
                state = index_max == 0
                            ? end_case
                            : key_val_case;
                return false;
            }

            public bool hasNullKey(uint null_values_case, uint key_val_case, uint next_field_case)
            {
                var has = hasNullKey();
                if (has && nullKeyHasValue()) return true; //not jump. step to send value of key == null
                //if key == null does not exists or it's value == null
                //no need to receive value,  so can calculate next jump
                state = 0 < index_max                    ? null_values_case : //jump to send keys which value == null
                        0 < (index_max = key_get<int>()) ? key_val_case :     // jump to send KV
                                                           next_field_case;   //jump out
                return has;
            }

            public bool nullKeyHasValue() { return (slot.context.key_long >> 38 & 1) == 1; }


            public bool get_items_count(uint next_case) => get_len((int)(slot!.context!.key_long >> 32 & 7), next_case);

            public bool null_values_count(uint next_case)
            {
                slot.context.key_long |= (ulong)index_max; //preserve key_val_count
                return get_len((int)(slot.context.key_long >> 35 & 7), next_case);
            }

            public int items_count => key_get<int>() + index_max + (hasNullKey()
                                                                        ? 1
                                                                        : 0);


            public bool no_null_values(uint key_val_case, uint end_case)
            {
                if (0 < index_max) return false; //keys which value == null
                state = 0 < (index_max = key_get<int>())
                            ? key_val_case
                            : end_case;
                return true;
            }

            public bool no_key_val() => (index_max = key_get<int>()) < 1;


            public uint state { get => slot!.state; set => slot!.state = value; }

            public int index { get => slot!.index; set => slot!.index = value; }

            public int index_max
            {
                get => slot!.index_max;
                set
                {
                    slot!.index_max = value;
                    slot.index      = 0;
                }
            }

            public int base_index { get => slot!.base_index; set => slot!.base_index = value; }

            public int base_index_max
            {
                get => slot!.base_index_max;
                set
                {
                    slot!.base_index_max = value;
                    slot.base_index      = 0;
                }
            }

            public uint nulls { get => slot!.items_nulls; set => slot!.items_nulls = value; }

            public void set_nulls(uint nulls, int index)
            {
                slot!.index       = index + trailingZeros(nulls);
                slot!.items_nulls = nulls;
            }

            public bool find_exist(int index)
            {
                var nulls = buffer[BYTE++];
                if (nulls == 0) return false;
                slot!.index       = index + trailingZeros(nulls);
                slot!.items_nulls = nulls;
                return true;
            }

            public bool find_base_exist(int base_index)
            {
                var nulls = buffer[BYTE++];
                if (nulls == 0) return false;
                slot!.base_index = base_index + trailingZeros(nulls);
                slot!.base_nulls = nulls;
                return true;
            }

            public bool null_at_index()      { return (nulls & 1 << (index & 7)) == 0; }
            public bool null_at_base_index() => (base_nulls & 1 << (base_index & 7)) == 0;

            public bool no_items_data(uint retry_at_case, uint no_items_case)
            {
                for (uint nulls; BYTE < len;)
                {
                    if ((nulls = buffer![BYTE++]) != 0)
                    {
                        slot!.index += trailingZeros(slot!.items_nulls = nulls);
                        return false;
                    }

                    if (slot!.index_max <= (slot!.index += 8))
                    {
                        state = no_items_case;
                        return false;
                    }
                }

                retry_at(retry_at_case);
                return true;
            }

            public uint base_nulls { get => slot!.base_nulls; set => slot!.base_nulls = value; }

            public void set_base_nulls(uint nulls, int base_index)
            {
                slot!.base_index = base_index + trailingZeros(nulls);
                slot!.base_nulls = nulls;
            }

            public bool next_index()      => ++slot!.index      < slot!.index_max;
            public bool next_base_index() => ++slot!.base_index < slot!.base_index_max;


            public bool get_fields_nulls(uint this_case)
            {
                if (BYTE < len)
                {
                    slot!.fields_nulls = buffer![BYTE++];
                    return true;
                }

                slot!.state = this_case;
                mode        = DONE;
                return false;
            }

            public bool is_null(int field) => (slot!.fields_nulls & field) == 0;

            public bool field_is_null() => u4 == 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool get_len(int bytes, uint next_case)
            {
                if (remaining < bytes)
                {
                    retry_get4(bytes, next_case);
                    mode = LEN;
                    return false;
                }

                slot!.index_max = get4<int>(bytes);
                slot.index      = 0;
                return true;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool get_base_len(int bytes, uint next_case)
            {
                if (remaining < bytes)
                {
                    retry_get4(bytes, next_case);
                    mode = BASE_LEN;
                    return false;
                }

                slot!.base_index_max = get4<int>(bytes);
                slot.base_index      = 0;
                return true;
            }


            public bool idle() => slot == null;


            bool not_get4()
            {
                if (remaining < bytes_left)
                {
                    var r = remaining;
                    u4         =  u4 << r * 8 | get4<uint>(r);
                    bytes_left -= r;
                    return true;
                }

                u4 = u4 << bytes_left * 8 | get4<uint>(bytes_left);
                return false;
            }

            public override string ToString()
            {
                return "Receiver{"            +
                       "\t\n bit="            + bit         +
                       "\t\n str='"           + str         + '\'' +
                       "\t\n bits="           + bits        +
                       "\t\n buffer="         + buffer      +
                       "\t\n mode="           + mode        +
                       "\t\n u4="             + u4          +
                       "\t\n u8="             + u8          +
                       "\t\n bytes_left="     + bytes_left  +
                       "\t\n assertion="      + assertion   +
                       "\t\n id_bytes="       + id_bytes    +
                       "\t\n OutputConsumer=" + int_dst     +
                       "\t\n slot="           + slot        +
                       "\t\n slot_ref="       + slot_ref    +
                       "\t\n context="        + context     +
                       "\t\n context_ref="    + context_ref +
                       '}';
            }

            public void Write(byte[]? src, int src_byte, int src_bytes)
            {
                if (src == null)
                {
                    buffer    = null;
                    assertion = Assertion.UNEXPECT;
                    if (slot == null) return;
                    mode       = OK;
                    bytes_left = id_bytes;
                    u4         = 0;
                    u8         = 0;
                    str        = null;
                    slot.dst   = null;
                    slot.state = 0;
                    while (slot != null)
                    {
                        slot.dst = null;
                        free_slot();
                    }

                    return;
                }

                if ((len = src_bytes) < 1)
                    if (assertion == Assertion.PACK_END) //PACK_END unexpected.
                    {
                        assertion = Assertion.UNEXPECT;
                        return;
                    }

                buffer = src;
                BYTE   = src_byte;
                for (; BYTE < len;)
                {
                    if (slot?.dst == null)
                    {
                        if (not_get4()) //read id
                        {
                            if (slot != null) free_slot(); //remove hardlinks
                            break;
                        }

                        var id = u4;
                        bytes_left = id_bytes;
                        u4         = 0;
                        if (slot == null && !slot_ref.TryGetTarget(out slot)) slot_ref = new WeakReference<Slot>(slot = new Slot(null));
                        if ((slot.dst = int_dst!.Receiving(this, (int)id)) == null)
                        {
                            slot = null;
                            break;
                        }

                        u8         = 0;
                        slot.state = 0;
                    }
                    else // internal write
                        switch (mode)
                        {
                            case VAL8:
                                if (remaining < bytes_left)
                                {
                                    var r = remaining;
                                    u8         =  u8 << r * 8 | get8<ulong>(r);
                                    bytes_left -= r;
                                    goto exit;
                                }

                                u8 = u8 << bytes_left * 8 | get8<ulong>(bytes_left);
                                break;
                            case VAL4:
                                if (not_get4()) goto exit;
                                break;
                            case LEN:
                                if (not_get4()) goto exit;
                                index_max = (int)u4;
                                break;
                            case VARINT:
                                if (BYTE < len && retry_get_varint(state)) break;
                                goto exit;
                            case BASE_LEN:
                                if (not_get4()) goto exit;
                                base_index_max = (int)u4;
                                break;
                            case STR:
                                var i = 0;
                                for (;; i++)
                                    if (BYTE + i == len)
                                    {
                                        if (buff!.Length < bytes_left + i) //have to expand buff.
                                        {
                                            var tmp = buff;
                                            buff = ArrayPool<byte>.Shared.Rent(bytes_left + i + i / 2);
                                            Buffer.BlockCopy(tmp, 0, buff, 0, bytes_left);
                                            ArrayPool<byte>.Shared.Return(tmp);
                                        }

                                        Buffer.BlockCopy(buffer!, BYTE, buff, bytes_left, i);
                                        bytes_left += i;
                                        goto exit;
                                    }
                                    else if (buffer![BYTE + i] == 0xFF) break;

                                if (buff!.Length < bytes_left + i) //not enough space in buff
                                {
                                    //comment on  check start
                                    if (bytes_left <= BYTE) //there is enough space at the beginning, just use it
                                    {
                                        Buffer.BlockCopy(buff, 0, buffer, BYTE - bytes_left, bytes_left);
                                        str = Encoding.UTF8.GetString(new Span<byte>(buffer, BYTE - bytes_left, bytes_left + i));
                                    }
                                    else if (bytes_left + remaining <= buffer.Length)
                                    {
                                        Buffer.BlockCopy(buffer, BYTE, buffer, bytes_left, remaining);
                                        len += bytes_left - BYTE;
                                        Buffer.BlockCopy(buff, 0, buffer, 0, BYTE = bytes_left);
                                        str = Encoding.UTF8.GetString(new Span<byte>(buffer, 0, bytes_left + i));
                                    }
                                    else //expand buff
                                    {
                                        var tmp = buff;
                                        buff = ArrayPool<byte>.Shared.Rent(bytes_left + i);
                                        Buffer.BlockCopy(tmp,    0,    buff, 0,          bytes_left);
                                        Buffer.BlockCopy(buffer, BYTE, buff, bytes_left, i);
                                        ArrayPool<byte>.Shared.Return(tmp);
                                        str = Encoding.UTF8.GetString(new Span<byte>(buff, 0, bytes_left + i));
                                    }
                                }
                                else
                                {
                                    Buffer.BlockCopy(buffer, BYTE, buff, bytes_left, i);
                                    str = Encoding.UTF8.GetString(new Span<byte>(buff, 0, bytes_left + i));
                                }

                                BYTE       += i + 1;
                                bytes_left =  0;
                                ArrayPool<byte>.Shared.Return(buff);
                                buff = null;
                                break;
                        }

                    mode = OK;
                    for (AdHoc.INT.BytesDst? dst;;)
                        if ((dst = slot!.dst!.put_bytes(this)) == null)
                        {
                            if (mode      < OK) goto exit; //no more data
                            if (slot.prev == null) break;  //it was the root level everything, all the data is received. further dispatching
                            // slot.dst = null;  //return from the depths we do not clean it will be possible to use
                            free_slot();
                        }
                        else //deeper into the hierarchy
                        {
                            slot       = slot.next ??= slot.next = new Slot(slot);
                            slot.dst   = dst;
                            slot.state = 0;
                        }

                    bytes_left = id_bytes; // !!!!!!!!!!!!!
                    u4         = 0;
                    slot.state = 0;
                    switch (assertion)
                    {
                        case Assertion.SPACE_END: //unexpected. discard data.
                            assertion = Assertion.UNEXPECT;
                            return;
                        case Assertion.PACK_END:               //expected
                            int_dst!.Received(this, slot.dst); //dispatching
                            slot.dst = null;                   //preparing to read next packet data
                            return;
                    }

                    int_dst!.Received(this, slot.dst); //dispatching
                    slot.dst = null;                   //preparing to read next packet data
                }

                exit:
                buffer = null;
                if (assertion == Assertion.PACK_END) //PACK_END unexpected.
                    assertion = Assertion.UNEXPECT;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void retry_at(uint the_case)
            {
                slot!.state = the_case;
                mode        = DONE;
            }

            public bool no_index(uint on_fail_case, int on_fail_fix_index)
            {
                if (BYTE < len) return false;
                retry_at(on_fail_case);
                index = on_fail_fix_index;
                return true;
            }

            public bool no_base_index(uint on_fail_case, int on_fail_fix_base_index)
            {
                if (BYTE < len) return false;
                retry_at(on_fail_case);
                base_index = on_fail_fix_base_index;
                return true;
            }

            public bool try_get8(uint next_case) => try_get8(bytes_left, next_case);


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool try_get8(int bytes, uint get8_case)
            {
                if (remaining < bytes) return retry_get8(bytes, get8_case);
                u8 = get8<ulong>(bytes);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool retry_get8(int bytes, uint get8_case)
            {
                bytes_left  = bytes - remaining;
                u8          = get8<ulong>(remaining);
                slot!.state = get8_case;
                mode        = VAL8;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public T get8<T>() => Unsafe.As<ulong, T>(ref u8);


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T get8<T>(int byTes)
            {
                ulong u8 = 0;
                BYTE += byTes;
                switch (byTes)
                {
                    case 8:
                        u8 |= (ulong)buffer![BYTE - 8] << 56;
                        goto case 7;
                    case 7:
                        u8 |= (ulong)buffer![BYTE - 7] << 48;
                        goto case 6;
                    case 6:
                        u8 |= (ulong)buffer![BYTE - 6] << 40;
                        goto case 5;
                    case 5:
                        u8 |= (ulong)buffer![BYTE - 5] << 32;
                        goto case 4;
                    case 4:
                        u8 |= (ulong)buffer![BYTE - 4] << 24;
                        goto case 3;
                    case 3:
                        u8 |= (ulong)buffer![BYTE - 3] << 16;
                        goto case 2;
                    case 2:
                        u8 |= (ulong)buffer![BYTE - 2] << 8;
                        goto case 1;
                    case 1:
                        u8 |= buffer![BYTE - 1];
                        break;
                }

                return Unsafe.As<ulong, T>(ref u8);
            }

            public bool try_get4(uint next_case) => try_get4(bytes_left, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool try_get4(int bytes, uint next_case)
            {
                if (remaining < bytes) return retry_get4(bytes, next_case);
                u4 = get4<uint>(bytes);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool retry_get4(int bytes, uint get4_case)
            {
                bytes_left  = bytes - remaining;
                u4          = get4<uint>(remaining);
                slot!.state = get4_case;
                mode        = VAL4;
                return false;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)] public T get4<T>() => Unsafe.As<uint, T>(ref u4);


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T get4<T>(int byTes)
            {
                uint u4 = 0;
                BYTE += byTes;
                switch (byTes)
                {
                    case 4:
                        u4 |= (uint)buffer![BYTE - 4] << 24;
                        goto case 3;
                    case 3:
                        u4 |= (uint)buffer![BYTE - 3] << 16;
                        goto case 2;
                    case 2:
                        u4 |= (uint)buffer![BYTE - 2] << 8;
                        goto case 1;
                    case 1:
                        u4 |= buffer![BYTE - 1];
                        break;
                }

                return Unsafe.As<uint, T>(ref u4);
            }

#region bits
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void init_bits()
            {
                bits = 0;
                bit  = 8;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool get_bit() => u4 != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool? get_bool() => u4 switch
                                       {
                                           0 => null,
                                           1 => true,
                                           _ => false
                                       };

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public T get_bits<T>() => Unsafe.As<uint, T>(ref u4);

            public T get_bits<T>(int len_bits)
            {
                uint ret;
                if (bit + len_bits < 9)
                {
                    ret =  bits >> bit & 0xFFU >> 8 - len_bits;
                    bit += len_bits;
                }
                else
                {
                    ret = (bits >> bit | (bits = buffer![BYTE++]) << 8 - bit) & 0xFFU >> 8 - len_bits;
                    bit = bit + len_bits - 8;
                }

                return Unsafe.As<uint, T>(ref ret);
            }


            public bool try_get_bits(int len_bits, uint this_case)
            {
                if (bit + len_bits < 9)
                {
                    u4  =  bits >> bit & 0xFFU >> 8 - len_bits;
                    bit += len_bits;
                }
                else if (BYTE < len)
                {
                    u4  = (bits >> bit | (bits = buffer![BYTE++]) << (8 - bit)) & 0xFFU >> 8 - len_bits;
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

            public short zig_zag2(ulong src) => (short)(-(short)(src & 1) ^ (short)(src >> 1));
            public int   zig_zag4(ulong src) => -(int)(src  & 1) ^ (int)(src  >> 1);
            public long  zig_zag8(ulong src) => -(long)(src & 1) ^ (long)(src >> 1);

            public bool try_get_varint(uint next_case)
            {
                u8         = 0;
                bytes_left = 0;
                return retry_get_varint(next_case);
            }

            private bool retry_get_varint(uint next_case)
            {
                while (BYTE < len)
                {
                    ulong b = buffer![BYTE++];
                    if (0x7F < b)
                    {
                        u8         |= (b & 0x7FUL) << bytes_left;
                        bytes_left += 7;
                        continue;
                    }

                    u8 |= b << bytes_left;
                    return true;
                }

                state = next_case;
                mode  = VARINT;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public string? get_string()
            {
                var ret = str;
                str = null;
                return ret;
            }

            private byte[]? buff; //temporary buffer for the receiving string and more

#region temporary store received params
            public void init_buff(int size) => buff = ArrayPool<byte>.Shared.Rent(size);

            public void clean_buff()
            {
                ArrayPool<byte>.Shared.Return(buff!);
                buff = null;
            }

            public int get(int pos, int bytes)
            {
                var u4 = 0;
                switch (bytes)
                {
                    case 4:
                        u4 |= buff![pos + 3] << 24;
                        goto case 3;
                    case 3:
                        u4 |= buff![pos + 2] << 16;
                        goto case 2;
                    case 2:
                        u4 |= buff![pos + 1] << 8;
                        goto case 1;
                    case 1:
                        u4 |= buff![pos];
                        return u4;
                }

                return u4;
            }

            public void put(int pos, int bytes)
            {
                switch (bytes)
                {
                    case 4:
                        buff![pos + 3] = (byte)(u4 >> 24);
                        goto case 3;
                    case 3:
                        buff![pos + 2] = (byte)(u4 >> 16);
                        goto case 2;
                    case 2:
                        buff![pos + 1] = (byte)(u4 >> 8);
                        goto case 1;
                    case 1:
                        buff![pos] = (byte)(u4 & 0xFF);
                        break;
                }
            }
#endregion

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool get_string(int get_string_case)
            {
                for (var i = 0; BYTE + i < len; i++)
                    if (buffer![BYTE + i] == 0xFF)
                    {
                        str  =  Encoding.UTF8.GetString(new Span<byte>(buffer, BYTE, i));
                        BYTE += i + 1;
                        return true;
                    }

                bytes_left = remaining; // in bytes_left - how many bytes in buff
                buff       = ArrayPool<byte>.Shared.Rent(bytes_left + bytes_left / 2);
                Buffer.BlockCopy(buffer!, BYTE, buff, 0, bytes_left);
                slot!.state = (uint)get_string_case;
                mode        = STR; //lack of received bytes, switch to reading lines internally
                return false;
            }

            public void Close()  => Write(null, 0, 0);
            public bool isOpen() => slot != null;
        }

        public class Transmitter : AdHoc, EXT.BytesSrc, Context.Provider
        {
            public INT.BytesSrc.Producer? int_src;
            public Func<ulong>?           int_values_src;

            public Transmitter(INT.BytesSrc.Producer? int_src, Func<ulong>? int_values_src)
            {
                this.int_src        = int_src;
                this.int_values_src = int_values_src;
            }

            public class UART : EXT.BytesSrc
            {
                public Transmitter src;
                public UART(Transmitter src) { this.src = src; }

                public int Read(byte[]? dst, int dst_byte, int dst_bytes)
                {
                    if (dst == null)
                    {
                        src.Read(dst, 0, 0);
                        enc_bits  = 0;
                        enc_shift = 0;
                        enc_crc   = 0;
                        return -1;
                    }

                    var fix_p = dst_byte;
                    var limit = dst_byte + dst_bytes;
                    while (12 < limit - dst_byte)
                    {
                        var s = dst_byte + (limit - dst_byte) / 8 + 3;
                        src.assertion = Assertion.SPACE_END; //expect by default
                        var r   = src.Read(dst, s, limit - s);
                        var max = s + r;
                        if (src.assertion == Assertion.UNEXPECT || s == max) break;
                        for (; s < max; s++) dst_byte = encode(dst[s], dst, dst_byte);
                        if (src.assertion == Assertion.PACK_END)
                        {
                            int crc = enc_crc;
                            dst_byte = encode(crc >> 8 & 0xFF, dst, dst_byte);
                            dst_byte = encode(crc      & 0xFF, dst, dst_byte);
                            if (0 < enc_shift) dst[dst_byte++] = (byte)enc_bits;
                            enc_bits        = 0;
                            enc_shift       = 0;
                            enc_crc         = 0;
                            dst[dst_byte++] = 0xFF;
                        }
                    }

                    src.assertion = Assertion.UNEXPECT; //return to normal state
                    return 0 < dst_byte - fix_p
                               ? dst_byte - fix_p
                               : -1;
                }

                private int encode(int src, byte[] dst, int dst_byte)
                {
                    enc_crc = crc16((byte)src, enc_crc);
                    var v = (enc_bits |= src << enc_shift) & 0xFF;
                    if ((v & 0x7F) == 0x7F)
                    {
                        dst[dst_byte++] =   0x7F;
                        enc_bits        >>= 7;
                        if (enc_shift < 7) enc_shift++;
                        else //                          a full byte in enc_bits
                        {
                            if ((enc_bits & 0x7F) == 0x7F)
                            {
                                dst[dst_byte++] =   0x7F;
                                enc_bits        >>= 7;
                                enc_shift       =   1;
                                return dst_byte;
                            }

                            dst[dst_byte++] = (byte)enc_bits;
                            enc_shift       = 0;
                            enc_bits        = 0;
                        }

                        return dst_byte;
                    }

                    dst[dst_byte++] =   (byte)v;
                    enc_bits        >>= 8;
                    return dst_byte;
                }

                private int    enc_bits;
                private int    enc_shift;
                private ushort enc_crc;

                public void Close()  => Read(null, 0, 0);
                public bool isOpen() => src.isOpen();
            }


#region Slot
            protected sealed class Slot
            {
                internal int base_index;
                internal int base_index2;
                internal int base_index_max;
                internal int fields_nulls;

                internal int index;
                internal int index2;
                internal int index_max;

                internal          Slot? next;
                internal readonly Slot? prev;

                public Slot(Slot? prev)
                {
                    this.prev = prev;
                    if (prev != null) prev.next = this;
                }

                internal AdHoc.INT.BytesSrc? src;

                internal uint state;

                internal ContextExt? context;
            }

            protected WeakReference<Slot> slot_ref = new(new Slot(null));
            protected Slot?               slot;

            private void free_slot()
            {
                if (slot!.context != null)
                {
                    ctx          = slot.context.prev;
                    slot.context = null;
                }

                slot = slot.prev;
            }
#endregion

#region Context
            internal class ContextExt : Context
            {
                public          ContextExt? next;
                public readonly ContextExt? prev;

                public ContextExt(ContextExt? prev)
                {
                    this.prev = prev;
                    if (prev != null) prev.next = this;
                }
            }


            private ContextExt?               ctx;
            private WeakReference<ContextExt> context_ref = new(new ContextExt(null));

            public Context context
            {
                get
                {
                    if (slot!.context != null) return slot.context;
                    if (ctx == null && !context_ref.TryGetTarget(out ctx)) context_ref = new WeakReference<ContextExt>(ctx = new ContextExt(null));
                    else if (ctx.next == null) ctx                                     = ctx.next = new ContextExt(ctx);
                    else ctx                                                           = ctx.next;
                    return slot.context = ctx;
                }
            }
#endregion


            public uint state { get => slot!.state; set => slot!.state = value; }

            public int index { get => slot!.index; set => slot!.index = value; }

            public int index2 { get => slot!.index2; set => slot!.index2 = value; }

            public int index_max
            {
                get => slot!.index_max;
                set
                {
                    slot!.index_max = value;
                    slot!.index     = 0;
                }
            }

            public int base_index { get => slot!.base_index; set => slot!.base_index = value; }

            public int base_index_max
            {
                get => slot!.base_index_max;
                set
                {
                    slot!.base_index_max = value;
                    slot!.base_index     = 0;
                }
            }

            public int base_index2 { set => slot!.base_index2 = value; }

            public bool next_index2() => ++slot!.index < slot!.index2;
            public bool next_index()  => ++slot!.index < slot!.index_max;

            public bool next_base_index2() => ++slot!.base_index < slot!.base_index2;
            public bool next_base_index()  => ++slot!.base_index < slot!.base_index_max;

            public int index_next(int next_state)
            {
                ++slot.index;
                state = (uint)(slot.index_max == slot.index
                                   ? next_state + 1
                                   : next_state);
                return slot.index - 1;
            }

            public bool init_fields_nulls(int field0_bit, uint current_case)
            {
                if (!allocate(1, current_case)) return false;
                slot!.fields_nulls = field0_bit;
                return true;
            }

            public void set_fields_nulls(int field) { slot!.fields_nulls |= field; }

            public void flush_fields_nulls() { put((byte)slot!.fields_nulls); }

            public bool is_null(int field) => (slot!.fields_nulls & field) == 0;


            public bool idle() => slot == null;


            // if dst == null - clean / reset state
            public int Read(byte[]? dst, int dst_byte, int dst_bytes)
            {
                if (dst == null)
                {
                    if (slot == null) return -1;
                    buffer    = null;
                    assertion = Assertion.UNEXPECT;
                    while (slot != null)
                    {
                        slot.src = null;
                        free_slot();
                    }

                    mode       = OK;
                    u4         = 0;
                    bytes_left = 0; //requires correct bitwise sending
                    return -1;
                }

                buffer = dst;
                BYTE   = dst_byte;
                len    = dst_bytes;
                var fix = BYTE;
                for (; BYTE < len;)
                {
                    if (slot?.src == null)
                    {
                        if (slot == null && !slot_ref.TryGetTarget(out slot)) slot_ref = new WeakReference<Slot>(slot = new Slot(null));
                        if ((slot!.src = int_src!.Sending(this)) == null)
                        {
                            buffer = null;
                            free_slot(); //remove hardlink
                            assertion = Assertion.UNEXPECT;
                            return 0 < BYTE - fix
                                       ? BYTE - fix
                                       : -1;
                        }

                        slot.state = 0; //write id request
                        bytes_left = 0;
                        slot.index = 0;
                    }
                    else
                        switch (mode) //the packet transmission was interrupted, recall where we stopped
                        {
                            case STR:
                                if (!encode(str!)) goto exit;
                                str = null;
                                break;
                            case VAL4:
                                if (len - BYTE < bytes_left) goto exit;
                                put_val(u4, bytes_left);
                                break;
                            case VAL8:
                                if (len - BYTE < bytes_left) goto exit;
                                put_val(u8, bytes_left);
                                break;
                            case VARINTS:
                                if (len - BYTE < 25) goto exit; //space for one full transaction
                                bits_byte = BYTE;               //preserve space for bits info
                                BYTE++;
                                put_val(u8, bytes_left);
                                break;
                            case VARINT:
                                if (BYTE < len && put_varint(u8, state)) break;
                                goto exit;
                            case BITS:
                                if (len - BYTE < 4) return 0;
                                bits_byte = BYTE; //preserve space for bits info
                                BYTE++;
                                break;
                        }

                    mode = OK;                                          //restore the state
                    for (INT.BytesSrc? src;;)                           
                        if ((src = slot!.src!.get_bytes(this)) == null) //not going deeper in the hierarchy
                        {
                            if (mode       < OK) goto exit; //there is not enough space in the provided buffer for further work
                            if (slot!.prev == null) break;  //it was the root level all packet data sent
                            //slot.src = null               // do not do this. sometime can be used
                            free_slot();                    //return to the prev level and continue processing it
                        }
                        else //go into the hierarchy deeper
                        {
                            slot        = slot.next ?? (slot.next = new Slot(slot));
                            slot!.src   = src;
                            slot!.state = 1; //skip write id
                        }

                    int_src!.Sent(this, slot.src);
                    slot.src = null; //sing of next packet data request 
                    if (assertion == Assertion.UNEXPECT) continue;
                    slot      = null;
                    assertion = Assertion.PACK_END;
                    break;
                }

                exit:
                buffer = null;
                return BYTE - fix;
            }

            public bool allocate(int bytes, uint current_case)
            {
                if (bytes <= remaining) return true;
                slot!.state = current_case;
                mode        = DONE;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(bool src) => put_bits(src
                                                      ? 1
                                                      : 0, 1);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(bool? src) => put_bits(src.HasValue
                                                       ? src.Value
                                                             ? 3
                                                             : 2
                                                       : 0, 2);


#region bits
            private int bits_byte = -1;

            public bool allocate(uint current_case) //space request (20 bytes) for at least one transaction is called once on the first varint, as continue of `init_bits`
            {
                if (17 < remaining) return true;
                slot!.state = current_case;
                BYTE        = bits_byte; //trim byte at bits_byte index
                mode        = BITS;
                return false;
            }

            public bool init_bits(uint current_case) => init_bits(20, current_case); //varint init_bits

            //mandatory check for enough space
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool init_bits(int space_bytes, uint current_case)
            {
                if (len - BYTE < space_bytes)
                {
                    slot.state = current_case;
                    mode       = DONE;
                    return false;
                }

                bits      = 0;
                bit       = 0;
                bits_byte = BYTE++; //allocate space
                return true;
            }

            //check, if in bits enougt data, then flush first `bits` byte into uotput buffer at bits_byte index
            //and switch to new place - bits_byte
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put_bits(int src, int len_bits)
            {
                bits |= (uint)src << bit;
                if ((bit += len_bits) < 9) return false; //yes 9! not 8!  to avoid allocating the next byte after the current one is full. it is might be redundant
                buffer![bits_byte] =   (byte)bits;
                bits               >>= 8;
                bit                -=  8;
                bits_byte          =   BYTE++;
                return true;
            }

            //end of varint mode called once per batch
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void end_bits()
            {
                if (0 < bit) buffer![bits_byte] = (byte)bits;
                else BYTE                       = bits_byte; //trim byte at bits_byte index. allocated, but not used
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void continue_bits_at(uint continue_at_case)
            {
                slot!.state = continue_at_case;
                BYTE        = bits_byte;
                mode        = BITS;
            }
#endregion

            private bool put_varint(int bytes_info, int bits, ulong varint, int bytes, uint continue_at_case)
            {
                //            break here is OK
                if (put_bits(bytes_info, bits) && remaining < 25) //wost case 83: 3 bits x 3times x 8 bytes
                {
                    u8         = varint; //fix value
                    bytes_left = bytes;  //fix none zero LSB length
                    state      = continue_at_case;
                    BYTE       = bits_byte;
                    mode       = VARINTS;
                    return false;
                }

                put_val(varint, bytes);
                return true;
            }

            public bool put_varint(int bits, uint continue_at_case)
            {
                if (!put_bits(0, bits) || 20 < remaining) return true;
                continue_bits_at(continue_at_case);
                return false;
            }

            private static int bytes1(ulong src)
            {
                return src < 1 << 8
                           ? 1
                           : 2;
            }

            public bool put_varint21(ulong src, uint continue_at_case)
            {
                var bytes = bytes1(src);
                return put_varint(bytes - 1, 1, src & 0xFFFFFFFFL, bytes, continue_at_case);
            }

            public bool put_varint211(ulong src, uint continue_at_case)
            {
                var bytes = bytes1(src);
                return put_varint(bytes - 1 << 1 | 1, 2, src & 0xFFFFFFFFL, bytes, continue_at_case);
            }

            public ulong zig_zag(short src) { return (ushort)(src << 1 ^ src >> 15); }

            private static int bytes2(ulong src) { return src < 1 << 8 ? 1 : src < 1 << 16 ? 2 : 3; }

            public bool put_varint32(ulong src, uint continue_at_case)
            {
                if (src == 0) return put_varint(2, continue_at_case);
                var bytes = bytes2(src);
                return put_varint(bytes, 2, src & 0xFFFFFFFFL, bytes, continue_at_case);
            }

            public bool put_varint321(ulong src, uint continue_at_case)
            {
                if (src is 0) return put_varint(3, continue_at_case);
                var bytes = bytes2(src);
                return put_varint(bytes << 1 | 1, 3, src & 0xFFFF_FFL, bytes, continue_at_case);
            }


            public ulong zig_zag(int src) { return (uint)(src << 1 ^ src >> 31); }

            private static int bytes3(ulong src)
            {
                return src < 1L << 16 ? src < 1L << 8
                                            ? 1
                                            : 2 :
                       src < 1L << 24 ? 3 : 4;
            }

            public bool put_varint42(ulong src, uint continue_at_case)
            {
                var bytes = bytes3(src);
                return put_varint(bytes - 1, 2, src, bytes, continue_at_case);
            }

            public bool put_varint421(ulong src, uint continue_at_case)
            {
                var bytes = bytes3(src);
                return put_varint(bytes - 1 << 1 | 1, 3, src, bytes, continue_at_case);
            }

            public ulong zig_zag(long src) { return (ulong)(src << 1 ^ src >> 63); }

            private static int bytes4(ulong src)
            {
                return src < 1 << 24 ? src < 1 << 16
                                           ? src < 1 << 8
                                                 ? 1
                                                 : 2
                                           : 3 :
                       src < 1L << 32 ? 4 :
                       src < 1L << 40 ? 5 :
                       src < 1L << 48 ? 6 : 7;
            }

            public bool put_varint73(ulong src, uint continue_at_case)
            {
                if (src == 0) return put_varint(3, continue_at_case);
                var bytes = bytes4(src);
                return put_varint(bytes, 3, src, bytes, continue_at_case);
            }

            public bool put_varint731(ulong src, uint continue_at_case)
            {
                if (src is 0) return put_varint(4, continue_at_case);
                var bytes = bytes4(src);
                return put_varint(bytes << 1 | 1, 4, src, bytes, continue_at_case);
            }

            private static int bytes5(ulong src)
            {
                return src < 1L << 32 ? src < 1 << 16 ? src < 1 << 8
                                                            ? 1
                                                            : 2 :
                                        src < 1 << 24 ? 3 : 4 :
                       src < 1L << 48 ? src < 1L << 40
                                            ? 5
                                            : 6 :
                       src < 1L << 56 ? 7 : 8;
            }


            public bool put_varint83(ulong src, uint continue_at_case)
            {
                var bytes = bytes5(src);
                return put_varint(bytes - 1, 3, src, bytes, continue_at_case);
            }

            public bool put_varint831(ulong src, uint continue_at_case)
            {
                var bytes = bytes5(src);
                return put_varint(bytes - 1 << 1 | 1, 4, src, bytes, continue_at_case);
            }

            public bool put_varint84(ulong src, uint continue_at_case)
            {
                if (src == 0) return put_varint(4, continue_at_case);
                var bytes = bytes5(src);
                return put_varint(bytes, 4, src, bytes, continue_at_case);
            }

            public bool put_varint841(ulong src, uint continue_at_case)
            {
                if (src is 0) return put_varint(5, continue_at_case);
                var bytes = bytes5(src);
                return put_varint(bytes << 1 | 1, 5, src, bytes, continue_at_case);
            }

            public bool put_varint(ulong src, uint next_case)
            {
                while (BYTE < len)
                {
                    if (src < 0x80)
                    {
                        buffer![BYTE++] = (byte)src;
                        return true;
                    }

                    buffer![BYTE++] =   (byte)(~0x7FUL | src & 0x7FUL);
                    src             >>= 7;
                }

                u8    = src;
                state = next_case;
                mode  = VARINT;
                return false;
            }

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
                BYTE += bytes;
                switch (bytes)
                {
                    case 8:
                        buffer![BYTE - 8] = (byte)(src >> 56);
                        goto case 7;
                    case 7:
                        buffer![BYTE - 7] = (byte)(src >> 48);
                        goto case 6;
                    case 6:
                        buffer![BYTE - 6] = (byte)(src >> 40);
                        goto case 5;
                    case 5:
                        buffer![BYTE - 5] = (byte)(src >> 32);
                        goto case 4;
                    case 4:
                        buffer![BYTE - 4] = (byte)(src >> 24);
                        goto case 3;
                    case 3:
                        buffer![BYTE - 3] = (byte)(src >> 16);
                        goto case 2;
                    case 2:
                        buffer![BYTE - 2] = (byte)(src >> 8);
                        goto case 1;
                    case 1:
                        buffer![BYTE - 1] = (byte)src;
                        return;
                }
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put_len(int len, int bytes, uint next_case)
            {
                slot!.index_max = len;
                slot.index      = 0;
                return put_val((uint)len, bytes, next_case);
            }


            public bool no_more_items(uint key_value_case, uint end_case)
            {
                if (++slot.index < slot.index_max) return false;
                if (0 < index2)
                {
                    index_max = index2;
                    state     = key_value_case;
                }
                else state = end_case;

                return true;
            }

            public bool no_more_items(uint end_case)
            {
                if (0 < index2)
                {
                    index_max = index2;
                    return false;
                }

                state = end_case;
                return true;
            }

            //The method is split. cause of items == 0 no more queries!
            public bool zero_items(int items)
            {
                if (items == 0)
                {
                    put((byte)0);
                    return true;
                }

                index_max = items;
                return false;
            }

            public bool put_set_info(bool null_key_present, int end_case)
            {
                var items         = index_max;
                var null_key_bits = 0;
                if (null_key_present)
                {
                    null_key_bits = 1 << 7;
                    if (--items == 0)
                    {
                        put((byte)null_key_bits);
                        state = (uint)end_case;
                        return true;
                    }
                }

                index_max = items; //key-value items
                var bytes = bytes4value(items);
                put((byte)(null_key_bits | bytes));
                put_val((uint)items, bytes, 0);
                return false;
            }

            public bool put_map_info(bool null_key_present, bool null_key_has_value, int keys_null_value_count, uint next_case, uint key_val_case, uint next_field_case)
            {
                var items = index_max;
                var null_key_bits = null_key_has_value
                                        ? 1 << 6
                                        : 0;
                if (null_key_present)
                {
                    null_key_bits |= 1 << 7;
                    if (--items == 0)
                    {
                        put((byte)null_key_bits);
                        state = next_field_case;
                        return true;
                    }
                }

                if (0 < keys_null_value_count)
                {
                    index_max = keys_null_value_count; //keys with null value
                    var keys_null_value_count_bytes = bytes4value(keys_null_value_count);
                    items  -= keys_null_value_count;
                    index2 =  items; //key-value items preserve
                    var key_val_count_bytes = bytes4value(items);
                    put((byte)(null_key_bits | keys_null_value_count_bytes << 3 | key_val_count_bytes));
                    if (0 < items) put_val((uint)items, key_val_count_bytes, 0);
                    put_val((uint)keys_null_value_count, keys_null_value_count_bytes, 0);
                    state = next_case;
                    return false;
                }

                state     = key_val_case;
                index_max = items; //key-value items
                var bytes = bytes4value(items);
                put((byte)(null_key_bits | bytes));
                put_val((uint)items, bytes, 0);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put_base_len(int base_len, int bytes, uint next_case)
            {
                slot!.base_index_max = base_len;
                slot!.base_index     = 0;
                return put_val((uint)base_len, bytes, next_case);
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
                BYTE += bytes;
                switch (bytes)
                {
                    case 4:
                        buffer![BYTE - 4] = (byte)(src >> 24);
                        goto case 3;
                    case 3:
                        buffer![BYTE - 3] = (byte)(src >> 16);
                        goto case 2;
                    case 2:
                        buffer![BYTE - 2] = (byte)(src >> 8);
                        goto case 1;
                    case 1:
                        buffer![BYTE - 1] = (byte)src;
                        return;
                }
            }


            public bool put(string src, uint next_case)
            {
                bytes_left = src.Length; //here, bytes_left - number of unsent chars from the end of the string
                if (encode(src)) return true;
                slot!.state = next_case;
                str         = src; // switch to send internarly
                mode        = STR;
                return false;
            }

            bool encode(string str)
            {
                while (0 < bytes_left) //here, bytes_left - number of unsent chars from the end of the string
                {
                    var chs = Math.Min(bytes_left, remaining / 4); //the number of characters that can be guaranteed to fit into  `remaining` space
                    if (chs == 0) return false;                    //the provided buffer has run out of space
                    BYTE       += Encoding.UTF8.GetBytes(str.AsSpan(str!.Length - bytes_left, chs), new Span<byte>(buffer, BYTE, remaining));
                    bytes_left -= chs;
                }

                if (BYTE == len) return false;
                buffer![BYTE++] = 0xFF; //sign - end of the string
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void put(uint src, int bytes, uint next_case)
            {
                slot!.state = next_case;
                bytes_left  = bytes;
                u4          = src;
                mode        = VAL4;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void put(ulong src, int bytes, uint next_case)
            {
                slot!.state = next_case;
                bytes_left  = bytes;
                u8          = src;
                mode        = VAL8;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void retry_at(uint the_case)
            {
                slot!.state = the_case;
                mode        = DONE;
            }


            public int bytes4value(int value) => value < 0xFFFF ? value < 0xFF
                                                                      ? value == 0
                                                                            ? 0
                                                                            : 1
                                                                      : 2 :
                                                 value < 0xFFFFFF ? 3 : 4;

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void put(sbyte src) => buffer![BYTE++] = (byte)src;

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void put(sbyte? src) => buffer![BYTE++] = (byte)src!.Value;

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool put(sbyte src, uint next_case) => put((byte)src, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool put(sbyte? src, uint next_case) => put((byte)src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void put(byte src) => buffer![BYTE++] = src;

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void put(byte? src) => buffer![BYTE++] = src!.Value;

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool put(byte? src, uint next_case) => put(src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public bool put(byte src, uint next_case)
            {
                if (BYTE < len)
                {
                    put(src);
                    return true;
                }

                put(src, 1, next_case);
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool put(short? src, uint next_case) => put((ushort)src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool put(short src, uint next_case) => put((ushort)src, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool put(ushort? src, uint next_case) => put(src!.Value, next_case);

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void put(short src) => put((ushort)src);

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void put(short? src) => put((ushort)src!.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void put(ushort? src) => put(src!.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void put(ushort src)
            {
                BYTE              += 2;
                buffer![BYTE - 2] =  (byte)(src >> 8);
                buffer![BYTE - 1] =  (byte)src;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool put(int src, uint next_case) => put((uint)src, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool put(int? src, uint next_case) => put((uint)src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool put(float src, uint next_case) => put(Unsafe.As<float, uint>(ref src), next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool put(float? src, uint next_case)
            {
                var f = src!.Value;
                return put(Unsafe.As<float, uint>(ref f), next_case);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool put(uint? src, uint next_case) => put(src!.Value, next_case);

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void put(int src) => put((uint)src);

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void put(int? src) => put((uint)src!.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void put(float? src)
            {
                var f = src!.Value;
                put(Unsafe.As<float, uint>(ref f));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void put(float src) => put(Unsafe.As<float, uint>(ref src));


            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void put(uint? src) => put(src!.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void put(uint src)
            {
                BYTE              += 4;
                buffer![BYTE - 4] =  (byte)(src >> 24);
                buffer![BYTE - 3] =  (byte)(src >> 16);
                buffer![BYTE - 2] =  (byte)(src >> 8);
                buffer![BYTE - 1] =  (byte)src;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool put(long src, uint next_case) => put((ulong)src, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool put(long? src, uint next_case) => put((ulong)src!.Value, next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool put(double src, uint next_case) => put(Unsafe.As<double, ulong>(ref src), next_case);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool put(double? src, uint next_case)
            {
                var d = src!.Value;
                return put(Unsafe.As<double, ulong>(ref d), next_case);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public bool put(ulong? src, uint next_case) => put(src!.Value, next_case);

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)] public void put(double src) => put(Unsafe.As<double, ulong>(ref src));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void put(double? src)
            {
                var d = src!.Value;
                put(Unsafe.As<double, ulong>(ref d));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public void put(long src) => put((ulong)src);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public void put(long? src) => put((ulong)src!.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public void put(ulong? src) => put(src!.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void put(ulong src)
            {
                BYTE              += 8;
                buffer![BYTE - 8] =  (byte)(src >> 56);
                buffer![BYTE - 7] =  (byte)(src >> 48);
                buffer![BYTE - 6] =  (byte)(src >> 40);
                buffer![BYTE - 5] =  (byte)(src >> 32);
                buffer![BYTE - 4] =  (byte)(src >> 24);
                buffer![BYTE - 3] =  (byte)(src >> 16);
                buffer![BYTE - 2] =  (byte)(src >> 8);
                buffer![BYTE - 1] =  (byte)src;
            }

            public void Close()  => Read(null, 0, 0);
            public bool isOpen() => slot != null;
        }
        //Dictionarry with nullable Key
        public class Dictionary<K, V> : System.Collections.Generic.Dictionary<K, V>
        {
            public Dictionary() { }
            public Dictionary(IDictionary<K, V>               dictionary) : base(dictionary) { }
            public Dictionary(IDictionary<K, V>               dictionary, IEqualityComparer<K>? comparer) : base(dictionary, comparer) { }
            public Dictionary(IEnumerable<KeyValuePair<K, V>> collection) : base(collection) { }
            public Dictionary(IEnumerable<KeyValuePair<K, V>> collection, IEqualityComparer<K>? comparer) : base(collection, comparer) { }
            public Dictionary(IEqualityComparer<K>?           comparer) : base(comparer) { }
            public Dictionary(int                             capacity) : base(capacity) { }
            public Dictionary(int                             capacity, IEqualityComparer<K>? comparer) : base(capacity, comparer) { }
            protected Dictionary(SerializationInfo            info,     StreamingContext      context) : base(info, context) { }


            public int COUNT => Count + (hasNullKey
                                             ? 1
                                             : 0);

            public bool hasNullKey = false;
            public V    NullKeyValue;
        }

        public class RingBuffer<T>
        {
            private readonly T[]  buffer;
            private readonly uint mask;
            private volatile int  Lock = 0;
            private volatile uint _get = 0;
            private volatile uint _put = 0;

            public RingBuffer(int power_of_2) => buffer = new T[(mask = (1U << power_of_2) - 1) + 1];

            public int length => buffer.Length;

            public int size => (int)(_put - _get);

            public bool get_multithreaded(ref T value)
            {
                while (Interlocked.CompareExchange(ref Lock, 1, 0) != 0) Thread.SpinWait(10);

                var ret = get(ref value);

                Lock = 0;
                return ret;
            }

            public bool get(ref T value)
            {
                if (_get == _put) return false;
                value = buffer[(int)(_get++) & mask];
                return true;
            }

            public bool put_multithreaded(T value)
            {
                if (size + 1                                       == buffer.Length) return false;
                while (Interlocked.CompareExchange(ref Lock, 1, 0) != 0) Thread.SpinWait(10);

                var ret = put(value);
                Lock = 0;
                return ret;
            }

            public bool put(T value)
            {
                if (size + 1 == buffer.Length) return false;

                buffer[(int)(_put++) & mask] = value;

                return true;
            }

            public void clear()
            {
                _get = 0;
                _put = 0;
            }
        }
    }
}