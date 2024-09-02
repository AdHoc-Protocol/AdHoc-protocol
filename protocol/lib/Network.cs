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
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using org.unirail.collections;

namespace org.unirail
{
    public interface Network
    {
        public interface Channel
        {
            public enum Event
            {          // External to internal connection
                EXT_INT_CONNECT = 0, // Internal to external connection
                INT_EXT_CONNECT = 1, // External to internal disconnection
                EXT_INT_DISCONNECT = 2, // Internal to external disconnection
                INT_EXT_DISCONNECT = 3, // Timeout event
                TIMEOUT = 4,
            }
        }

        public interface WebSocket
        {
            public enum Event
            {
                INT_EXT_CONNECT = 6,            // Internal to external connection
                EXT_INT_CONNECT = 7,            // External to internal connection
                CLOSE = OPCode.CLOSE, // Close event
                PING = OPCode.PING,  // Ping event
                PONG = OPCode.PONG,  // Pong event
                EMPTY_FRAME = 11            // Empty frame event
            }

            // Internal enum for WebSocket masking
            internal enum Mask
            {
                FIN = 0b1000_0000, // Final frame bit
                OPCODE = 0b0000_1111, // Opcode mask
                LEN = 0b0111_1111  // Length mask
            }

            public enum OPCode
            {
                CONTINUATION = 0x00, //denotes a continuation frame
                TEXT_FRAME = 0x01, //denotes a text frame
                BINARY_FRAME = 0x02, //denotes a binary frame
                CLOSE = 0x08, //denotes a connection close
                PING = 0x09, //denotes a ping
                PONG = 0x0A  //denotes a pong
            }

            internal enum State
            {
                HANDSHAKE = 0,
                NEW_FRAME = 1,
                PAYLOAD_LENGTH_BYTE = 2,
                PAYLOAD_LENGTH_BYTES = 3,
                XOR0 = 4,
                XOR1 = 5,
                XOR2 = 6,
                XOR3 = 7,
                DATA0 = 8,
                DATA1 = 9,
                DATA2 = 10,
                DATA3 = 11,
                DISCARD = 12
            }
        }

        /** https://docs.microsoft.com/en-us/dotnet/framework/network-programming/socket-performance-enhancements-in-version-3-5?redirectedfrom=MSDN
          The pattern for performing an asynchronous socket operation with this class
            consists of the following steps:

            1.  Allocate a new [SocketAsyncEventArgs](https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socketasynceventargs) context
                object, or get a free one from an application pool.

            2.  Set properties on the context object to the operation about to be performed
                (the callback delegate method and data buffer, for example).

            3.  Call the appropriate socket method (xxxAsync) to initiate the asynchronous
                operation.

            4.  If the asynchronous socket method (xxxAsync) returns true in the callback,
                query the context properties for completion status.

            5.  If the asynchronous socket method (xxxAsync) returns false in the callback,
                the operation completed synchronously. The context properties may be queried
                for the operation result.

            6.  Reuse the context for another operation, put it back in the pool, or discard
                it.

            The lifetime of the new asynchronous socket operation context object is
            determined by references in the application code and asynchronous I/O
            references. It is not necessary for the application to retain a reference to an
            asynchronous socket operation context object after it is submitted as a
            parameter to one of the asynchronous socket operation methods. It will remain
            referenced until the completion callback returns. However it is advantageous for
            the application to retain the reference to the context object so that it can be
            reused for a future asynchronous socket operation.
         */
        public abstract class TCP<SRC, DST>
            where DST : AdHoc.BytesDst
            where SRC : AdHoc.BytesSrc
        {
            #region> TCP code
            #endregion> Network.TCP
            public readonly Channel channels;
            private readonly Action<SocketAsyncEventArgs> buffers;
            protected const long FREE = -1;

            public readonly Func<TCP<SRC, DST>, Channel> new_channel;
            public TimeSpan timeout;

            public string name;

            public TCP(string name, Func<TCP<SRC, DST>, Channel> new_channel, int buffer_size, TimeSpan timeout)
            {
                this.timeout = timeout;
                this.name = name;
                buffers = dst => dst.SetBuffer(ArrayPool<byte>.Shared.Rent(buffer_size), 0, buffer_size);
                channels = (this.new_channel = new_channel)(this);
            }

            protected Channel allocate()
            {
                var ch = channels;
                for (; Interlocked.CompareExchange(ref ch.receive_time, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), FREE) != FREE; ch = ch.next)
                    if (ch.next == null)
                    {
                        var ret = new_channel(this);
                        ret.receive_time = ret.transmit_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        while (Interlocked.CompareExchange(ref ch.next, ret, null) != null)
                            ch = ch.next;

                        return ret;
                    }

                ch.transmit_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return ch;
            }

            // Action for handling failures
            public Action<object, Exception> onFailure = (src, t) =>
                                                         {
                                                             Console.WriteLine("onFailure " + src);
#if DEBUG
                                                             Console.WriteLine(new Exception("onFailure").StackTrace);
#endif
                                                             Console.WriteLine(t);
                                                         };

            // Method to swap event handlers
            public Action<Channel, int> swap(Action<Channel, int> other)
            {
                var ret = onEvent; // Store current event handler
                onEvent = other;   // Swap with new handler
                return ret;        // Return old handler
            }

            public Action<Channel, int> onEvent = (channel, Event) =>
                                                  {
#if DEBUG
                                                      Console.WriteLine(new Exception("onEvent").StackTrace);
#endif
                                                      switch (Event)
                                                      {
                                                          case (int)Network.Channel.Event.EXT_INT_CONNECT:
                                                              Console.WriteLine(channel.host + ":Received connection from " + channel.peer_ip);
                                                              return;
                                                          case (int)Network.Channel.Event.INT_EXT_CONNECT:
                                                              Console.WriteLine(channel.host + ":Connected to " + channel.peer_ip);
                                                              return;
                                                          case (int)Network.Channel.Event.EXT_INT_DISCONNECT:
                                                              Console.WriteLine(channel.host + ":Remote peer " + channel.peer_ip + " has dropped the connection.");
                                                              return;
                                                          case (int)Network.Channel.Event.INT_EXT_DISCONNECT:
                                                              Console.WriteLine(channel.host + ":This host has dropped the connection to " + channel.peer_ip);
                                                              return;
                                                          case (int)Network.Channel.Event.TIMEOUT:
                                                              Console.WriteLine(channel.host + ":Timeout while receiving from " + channel.peer_ip);
                                                              return;
                                                          case (int)Network.WebSocket.Event.EXT_INT_CONNECT:
                                                              Console.WriteLine(channel.host + ":Websocket from " + channel.peer_ip);
                                                              return;
                                                          case (int)Network.WebSocket.Event.INT_EXT_CONNECT:
                                                              Console.WriteLine(channel.host + ":Websocket to " + channel.peer_ip);
                                                              return;
                                                          case (int)Network.WebSocket.Event.PING:
                                                              Console.WriteLine(channel.host + ":PING from " + channel.peer_ip);
                                                              return;
                                                          case (int)Network.WebSocket.Event.PONG:
                                                              Console.WriteLine(channel.host + ":PONG from " + channel.peer_ip);
                                                              return;
                                                          default:
                                                              Console.WriteLine(channel.peer_ip + " event: " + Event);
                                                              break;
                                                      }
                                                  };

            public class Channel : SocketAsyncEventArgs
            {
                #region> Channel code
                #endregion> Network.TCP.Channel

                public Socket? ext;                             // External socket
                public EndPoint peer_ip => ext!.RemoteEndPoint!; // Peer IP address
                public long peer_id = 0;                  // Peer ID
                public long session_id = 0;                  // Session ID

                public long receive_time = FREE;          // Time of last receive
                public long transmit_time = FREE;          // Time of last transmit
                public bool is_active => 0 < receive_time; // Check if channel is active

                public readonly TCP<SRC, DST> host;

                public TimeSpan timeout;

                public Channel(TCP<SRC, DST> host)
                {
                    timeout = (this.host = host).timeout;
                    receive_mate.Completed += OnCompleted;
                    DisconnectReuseSocket = true;
                    onNewBytesToTransmitArrive =
                        _ =>
                        {
                            if (ext != null && Interlocked.Increment(ref transmit_lock) == 1)
                                transmit();
                        };
                }

                public long maintenance(long time)
                {
                    time -= (long)timeout.TotalMilliseconds;
                    time = Math.Min(receive_time - time, transmit_time - time);

                    if (500 < time)
                        return time;
                    if (ext == null)
                        Close_and_dispose();
                    else
                        Close();
                    return long.MaxValue;
                }

                //close connections but preserve state
                public virtual void Close()
                {
                    if (ext == null)
                        return; // Do nothing if no external socket
                    //!!!!!!!!! CRITICAL:
                    //When using a connection-oriented Socket, always call the Shutdown method before
                    //closing the Socket. This ensures that all data is sent and received on the
                    //connected socket before it is closed.
                    //
                    //Call the Close method to free all managed and unmanaged resources associated
                    //with the Socket. Do not attempt to reuse the Socket after closing.
                    try { ext?.Shutdown(SocketShutdown.Both); }
                    catch (Exception e) { }

                    ext?.Close();
                    ext = null;
                    transmit_lock = 1;
                    host.onEvent(this, (int)Network.Channel.Event.INT_EXT_DISCONNECT);

                    if ((transmitter == null || !transmitter.isOpen()) && (receiver == null || !receiver.isOpen()))
                        Close_and_dispose();
                }

                public Action<Channel>? on_connected;
                public Action<Channel>? on_disposed;

                public void Close_and_dispose()
                {
                    if (Interlocked.Exchange(ref receive_time, FREE) == FREE)
                        return;

                    Close();
                    transmitter?.Close();
                    receiver?.Close();

                    RemoteEndPoint = null;

                    if (Buffer != null)
                    {
                        ArrayPool<byte>.Shared.Return(Buffer);
                        SetBuffer(null, 0, 0);
                    }

                    if (receive_mate.Buffer != null)
                    {
                        ArrayPool<byte>.Shared.Return(receive_mate.Buffer);
                        receive_mate.SetBuffer(null, 0, 0);
                    }

                    on_disposed?.Invoke(this);
                }

                #region Transmitting
                public SRC? transmitter;
                protected volatile int transmit_lock = 1;
                public Action<Channel>? onSent; //Event handler called when all available in socket bytes have been sent

                protected override void OnCompleted(SocketAsyncEventArgs _transmit)
                {
                    //base.OnCompleted(arg);

                    //LastOperation
                    //more easily facilitates using a single completion callback delegate for multiple kinds of
                    //asynchronous socket operations. This property describes the asynchronous socket operation that was most recently completed
                    if (_transmit.SocketError == SocketError.Success)
                        switch (_transmit.LastOperation)
                        {
                            case SocketAsyncOperation.Connect:
                                transmiter_connected(ConnectSocket);
                                return;
                            case SocketAsyncOperation.Disconnect:
                                host.onEvent(this, (int)Network.Channel.Event.INT_EXT_DISCONNECT);
                                return;
                            case SocketAsyncOperation.Send:
                                Interlocked.Increment(ref transmit_lock);
                                transmit();
                                return;
                        }
                    else
                        host.onFailure(this, new Exception("SocketError:" + _transmit.SocketError));
                }

                internal void transmiter_connected(Socket? ext)
                {
                    this.ext = ext;
                    transmit_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    host.onEvent(this, (int)Network.Channel.Event.INT_EXT_CONNECT);

                    if (Buffer == null)
                        host.buffers(this);
                    else
                        SetBuffer(0, Buffer.Length);

                    transmit_lock = 0;
                    transmitter!.subscribeOnNewBytesToTransmitArrive(onNewBytesToTransmitArrive);

                    if (receiver == null)
                        return;
                    if (receive_mate.Buffer == null)
                        host.buffers(receive_mate);

                    on_connected?.Invoke(this);

                    if (!ext!.ReceiveAsync(receive_mate))
                        receive(); //trigger receiving
                }

                protected readonly Action<AdHoc.BytesSrc> onNewBytesToTransmitArrive; //Callback function called when new bytes in the source are available for transmission

                private void transmit()
                {
                    do
                        while (transmit(Buffer!))
                            if (ext!.SendAsync(this))
                                return;
                    while (Interlocked.Exchange(ref transmit_lock, 0) != 0);

                    onSent?.Invoke(this);
                }

                //use SetBuffer to point where is the data for sending, return false if there is no data
                protected virtual bool transmit(byte[] dst)
                {
                    var bytes = transmitter!.Read(dst, 0, dst.Length);
                    if (bytes < 1)
                        return false;
                    SetBuffer(0, bytes);
                    return true;
                }
                #endregion
                #region Receiving
                public DST? receiver;
                internal readonly SocketAsyncEventArgs receive_mate = new();
                public volatile bool stop_receiving; //transmitting only

                public void start_receive()
                {
                    if (!stop_receiving)
                        return;
                    stop_receiving = false;
                    receive_mate.SetBuffer(receive_mate.Buffer);
                    if (!ext!.ReceiveAsync(receive_mate))
                        receive();
                }

                protected void OnCompleted(object? src, SocketAsyncEventArgs _receive)
                {
                    receive_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    //LastOperation
                    //more easily facilitates using a single completion callback delegate for multiple kinds of
                    //asynchronous socket operations. This property describes the asynchronous socket operation that was most recently completed
                    if (_receive.SocketError == SocketError.Success)
                        switch (_receive.LastOperation)
                        {
                            case SocketAsyncOperation.Disconnect:
                                host.onEvent(this, (int)Network.Channel.Event.EXT_INT_DISCONNECT);
                                return;
                            case SocketAsyncOperation.Receive:
                                receive();
                                return;
                        }
                    else if (_receive.SocketError == SocketError.TimedOut) { host.onEvent(this, (int)Network.Channel.Event.TIMEOUT); }
                    else
                        host.onFailure(this, new Exception("SocketError:" + _receive.SocketError));
                }

                internal void receiver_connected(Socket ext)
                {
                    this.ext = ext;
                    receive_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    host.onEvent(this, (int)Network.Channel.Event.EXT_INT_CONNECT);
                    if (!ext.Connected) //The incoming connection is closed within the event handler.
                    {
                        Close_and_dispose();
                        return;
                    }

                    if (receive_mate.Buffer == null)
                        host.buffers(receive_mate);

                    stop_receiving = false;
                    if (!this.ext!.ReceiveAsync(receive_mate))
                        receive(); //trigger receiving

                    if (transmitter == null)
                        return;
                    transmit_lock = 0; //unlock
                    if (Buffer == null)
                        host.buffers(this);

                    on_connected?.Invoke(this);

                    transmitter.subscribeOnNewBytesToTransmitArrive(onNewBytesToTransmitArrive);
                }

                private void receive()
                {
                    try
                    {
                        do
                        {
                            if (receive_mate.BytesTransferred == 0) //the number of bytes transferred in the socket operation.
                            {                                        //If zero is returned from a read operation, the remote end has closed the connection.
                                Close();
                                return;
                            }

                            if (stop_receiving)
                                return;
                            receive(receive_mate.Buffer!, receive_mate.Offset + receive_mate.BytesTransferred);
                            if (stop_receiving)
                                return;
                        }
                        while (!ext!.ReceiveAsync(receive_mate));
                    }
                    catch (Exception e) { host.onFailure(this, e); } //on close() ext turn to null
                }

                //manage receive_mate.SetBuffer
                protected virtual void receive(byte[] src, int src_bytes) => receiver!.Write(src, 0, src_bytes);
                #endregion

                public Channel? next;
            }

            public class WebSocket : Channel
            {
                #region> WebSocket code
                #endregion> Network.TCP.WebSocket

                //Websocket need TCP server with buffers size at least 256 bytes
                public WebSocket(TCP<SRC, DST> host) : base(host) { }

                public void close_gracefully(int code, string? why)
                {
                    var frame = frames.Value!.get();
                    frame.OPcode = Network.WebSocket.OPCode.CLOSE;

                    frame.buffer[0] = (byte)(code >> 8);
                    frame.buffer[1] = (byte)code;
                    if (why == null)
                        frame.buffer_bytes = 2;
                    else
                    {
                        for (int i = 0, max = why.Length; i < max; i++)
                            frame.buffer[i + 2] = (byte)why[i];

                        frame.buffer_bytes = 2 + why.Length;
                    }

                    urgent_frame_data = frame;
                    onNewBytesToTransmitArrive(null); //trigger transmitting
                }

                public void ping(string? msg)
                {
                    var frame = frames.Value!.get();
                    frame.OPcode = Network.WebSocket.OPCode.CLOSE;

                    if (msg == null)
                        frame.buffer_bytes = 0;
                    else
                    {
                        for (int i = 0, max = msg.Length; i < max; i++)
                            frame.buffer[i] = (byte)msg[i];

                        frame.buffer_bytes = msg.Length;
                    }

                    urgent_frame_data = frame;
                    onNewBytesToTransmitArrive(null); //trigger transmitting
                }

                public override void Close()
                {
                    if (ext == null)
                        return;
                    state = Network.WebSocket.State.HANDSHAKE;
                    sent_closing_frame = false;
                    frame_bytes_left = 0;
                    if (frame_data != null)
                        recycle_frame(frame_data);
                    if (urgent_frame_data != null)
                        recycle_frame(urgent_frame_data);
                    base.Close();
                }

                #region Transmitting
                private bool sent_closing_frame;
                volatile ControlFrameData? urgent_frame_data;

                //use SetBuffer to point where is the data for sending, return false if there is no data
                protected override bool transmit(byte[] dst)
                {
                    var frame_data = Interlocked.Exchange(ref urgent_frame_data, null);

                    if (frame_data == null)
                    {
                        frame_data = this.frame_data;
                        if (!catch_ready_frame())
                            frame_data = null;
                    }

                    //write into `dst` opt code and length

                    //https://datatracker.ietf.org/doc/html/rfc6455#section-5.2

                    //0                   1                   2                   3
                    //0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    //+-+-+-+-+-------+-+-------------+-------------------------------+
                    //|F|R|R|R| opcode|M| Payload len |    Extended payload length    |
                    //|I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
                    //|N|V|V|V|       |S|             |   (if payload len==126/127)   |
                    //| |1|2|3|       |K|             |                               |
                    //+-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +

                    var s = (frame_data != null ?
                                 frame_data.buffer_bytes + 2 :
                                 0) + 10; // Offset 10 byte. Preallocate place for max possible header length

                    var len = transmitter!.Read(dst, s, dst.Length - s); // Receive data into `dst` start from s position
                    if (0 < len)                                        // Write into `dst` opt code and length
                    {
                        var max = s + len;
                        switch (len)
                        {
                            case < 126:                                                                                     //if 0-125,
                                dst[s -= 2] = (int)Network.WebSocket.Mask.FIN | (int)Network.WebSocket.OPCode.BINARY_FRAME; //always last and binary
                                dst[s + 1] = (byte)len;                                                                    //that is the payload length.
                                break;
                            case < 0x1_0000:
                                dst[s -= 4] = (int)Network.WebSocket.Mask.FIN | (int)Network.WebSocket.OPCode.BINARY_FRAME; //always last and binary
                                dst[s + 1] = 126;                                                                          //If 126,

                                dst[s + 2] = (byte)(len >> 8); ////the following 2 bytes interpreted as a 16 -bit unsigned integer are the payload length.
                                dst[s + 3] = (byte)len;
                                break;
                            default:
                                dst[s -= 10] = (int)Network.WebSocket.Mask.FIN | (int)Network.WebSocket.OPCode.BINARY_FRAME; //always last and binary
                                dst[s + 1] = 127;                                                                          //If 127,

                                dst[s + 2] = 0; //the following 8 bytes interpreted as a 64-bit unsigned integer (the most significant bit MUST be 0) are the payload length.
                                dst[s + 3] = 0;
                                dst[s + 4] = 0;
                                dst[s + 5] = 0;
                                dst[s + 6] = (byte)(len >> 24);
                                dst[s + 7] = (byte)(len >> 16);
                                dst[s + 8] = (byte)(len >> 8);
                                dst[s + 9] = (byte)len;
                                break;
                        }

                        if (frame_data != null)
                        {
                            sent_closing_frame = frame_data.OPcode == Network.WebSocket.OPCode.CLOSE;
                            recycle_frame(frame_data.get_frame(dst, s -= frame_data.buffer_bytes + 2)); //write control frame into `dst` and recicle it
                        }

                        SetBuffer(s, max - s); //point where is the data for sending
                        return true;
                    }

                    if (frame_data == null)
                        return false;

                    sent_closing_frame = frame_data.OPcode == Network.WebSocket.OPCode.CLOSE;
                    recycle_frame(frame_data.get_frame(dst, 0)); //write control frame into `dst` and recicle it

                    SetBuffer(0, s); //point where is the data for sending
                    return true;
                }
                #endregion
                #region Receiving
                Network.WebSocket.State state = Network.WebSocket.State.HANDSHAKE;
                Network.WebSocket.OPCode OPcode;

                int frame_bytes_left,
                    BYTE,
                    xor0,
                    xor1,
                    xor2,
                    xor3;

                volatile ControlFrameData? frame_data;
                volatile int frame_lock;

                protected void allocate_frame_data(Network.WebSocket.OPCode OPcode)
                {
                    if (Interlocked.CompareExchange(ref frame_lock, FRAME_STANDBY, FRAME_READY) != FRAME_READY)
                    {
                        Interlocked.Exchange(ref frame_lock, FRAME_STANDBY);
                        frame_data = frames.Value!.get();
                    }

                    frame_data.buffer_bytes = 0;
                    frame_data.OPcode = OPcode;
                }

                protected void recycle_frame(ControlFrameData? frame)
                {
                    if (frame == null)
                        return;

                    Interlocked.CompareExchange(ref frame_data, null, frame);
                    frames.Value!.put(frame);
                }

                protected void frame_ready()
                {
                    Interlocked.Exchange(ref frame_lock, FRAME_READY); //activate sending
                    onNewBytesToTransmitArrive(null);                  //trigger transmitting
                }

                protected bool catch_ready_frame() => Interlocked.CompareExchange(ref frame_lock, 0, FRAME_READY) == FRAME_READY;

                protected const int FRAME_STANDBY = 1, FRAME_READY = 2;

                protected class ControlFrameData
                {
                    public Network.WebSocket.OPCode OPcode;
                    public int buffer_bytes;
                    public readonly byte[] buffer = new byte[125]; //All control frames MUST have a payload length of 125 bytes or less and MUST NOT be fragmented. https://datatracker.ietf.org/doc/html/rfc6455#section-5.5
                    public readonly SHA1 sha = SHA1.Create();

                    public int put_UPGRAGE_WEBSOCKET_responce(byte[] dst, int len)
                    {
                        GUID.CopyTo(buffer, len);
                        sha.TryComputeHash(new ReadOnlySpan<byte>(buffer, 0, len + GUID.Length), buffer, out len);

                        UPGRAGE_WEBSOCKET.CopyTo((Span<byte>)dst);

                        len = base64(buffer, 0, len, dst, UPGRAGE_WEBSOCKET.Length);
                        rnrn.CopyTo(dst, len);
                        return len + rnrn.Length;
                    }

                    static readonly byte[] GUID = Encoding.ASCII.GetBytes("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
                    static readonly byte[] rnrn = Encoding.ASCII.GetBytes("\r\n\r\n");

                    static readonly byte[] UPGRAGE_WEBSOCKET = Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\n" + "Server: AdHoc\r\n" + "Connection: Upgrade\r\n" + "Upgrade: websocket\r\n" + "Sec-WebSocket-Accept: ");

                    private int base64(byte[] src, int off, int end, byte[] dst, int dst_pos)
                    {
                        for (var max = off + (end - off) / 3 * 3; off < max;)
                        {
                            var bits = (src[off++] & 0xff) << 16 | (src[off++] & 0xff) << 8 | (src[off++] & 0xff);
                            dst[dst_pos++] = base64_[(bits >> 18) & 0x3f];
                            dst[dst_pos++] = base64_[(bits >> 12) & 0x3f];
                            dst[dst_pos++] = base64_[(bits >> 6) & 0x3f];
                            dst[dst_pos++] = base64_[bits & 0x3f];
                        }

                        if (off == end)
                            return dst_pos;

                        var b = src[off++] & 0xff;
                        dst[dst_pos++] = base64_[b >> 2];
                        if (off == end)
                        {
                            dst[dst_pos++] = base64_[(b << 4) & 0x3f];
                            dst[dst_pos++] = ((byte)'=');
                            dst[dst_pos++] = ((byte)'=');
                        }
                        else
                        {
                            dst[dst_pos++] = base64_[(b << 4) & 0x3f | ((b = src[off] & 0xff) >> 4)];
                            dst[dst_pos++] = base64_[(b << 2) & 0x3f];
                            dst[dst_pos++] = (byte)'=';
                        }

                        return dst_pos;
                    }

                    static readonly byte[] base64_ = Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/");

                    public ControlFrameData get_frame(byte[] dst, int dst_byte)
                    {
                        dst[dst_byte++] = (byte)((int)Network.WebSocket.Mask.FIN | (int)OPcode);
                        dst[dst_byte++] = (byte)buffer_bytes;

                        if (0 < buffer_bytes)
                            Array.Copy(buffer, 0, dst, dst_byte, buffer_bytes); //always  src.buffer_bytes < 126
                        return this;
                    }

                    internal void put_data(byte[] src, int start, int end)
                    {
                        var bytes = end - start;
                        Array.Copy(src, start, buffer, buffer_bytes, bytes);
                        buffer_bytes += bytes;
                    }
                }

                protected static readonly ThreadLocal<Pool<ControlFrameData>> frames = new(() => new Pool<ControlFrameData>(() => new ControlFrameData()));

                //manage receive_mate.SetBuffer
                protected override void receive(byte[] src, int src_bytes)
                {
                    for (int start = 0, index = 0; ;)
                        switch (state)
                        {
                            case Network.WebSocket.State.HANDSHAKE:

                                if (
                                    src[src_bytes - 4] == (byte)'\r' ||
                                    src[src_bytes - 3] == (byte)'\n' ||
                                    src[src_bytes - 2] == (byte)'\r' ||
                                    src[src_bytes - 1] == (byte)'\n')
                                {
                                    for (var i = 0; i < src_bytes; i++)
                                        switch ((char)src[i]) //search Sec-WebSocket-Key header
                                        {
                                            case 'S':
                                            case 's':
                                                if (src[i + 3] == '-' &&
                                                    src[i + 13] == '-' &&
                                                    src[i + 17] == ':')
                                                    switch ((char)src[i + 16])
                                                    {
                                                        case 'Y':
                                                        case 'y':
                                                            switch ((char)src[i + 15])
                                                            {
                                                                case 'E':
                                                                case 'e':
                                                                    switch ((char)src[i + 14])
                                                                    {
                                                                        case 'K':
                                                                        case 'k':
                                                                            for (i += 18; i < src_bytes; i++)
                                                                                if (src[i] != ' ')
                                                                                {
                                                                                    var pool = frames.Value!;
                                                                                    var helper = pool.get(); // Get helper frame

                                                                                    for (int e = i, ii = 0, b; e < src_bytes; e++, ii++)
                                                                                        if ((b = src[e]) == ' ' || b == '\r')
                                                                                        {
                                                                                            state = Network.WebSocket.State.NEW_FRAME;
                                                                                            receive_mate.SetBuffer(0, src.Length);

                                                                                            SetBuffer(0, helper.put_UPGRAGE_WEBSOCKET_responce(Buffer!, ii));

                                                                                            pool.put(helper);

                                                                                            Interlocked.Exchange(ref transmit_lock, ext!.SendAsync(this) ? //trigger transmitting
                                                                                                                                        1 :                //on complete - unlock itself
                                                                                                                                        0);                //unlocked for new data
                                                                                            host.onEvent(this, (int)Network.WebSocket.Event.EXT_INT_CONNECT);
                                                                                            return;
                                                                                        }
                                                                                        else
                                                                                            helper.buffer[ii] = (byte)b; //getting Sec-WebSocket-Key value into tmp
                                                                                }

                                                                            break;
                                                                    }

                                                                    break;
                                                            }

                                                            break;
                                                    }

                                                break;
                                        }

                                    host.onFailure(this, new Exception("Unexpected handshake:" + Encoding.ASCII.GetString(src, 0, src_bytes)));
                                    Close_and_dispose();
                                    return;
                                }

                                receive_mate.SetBuffer(src_bytes, src.Length); //continue receiving
                                return;
                            case Network.WebSocket.State.NEW_FRAME:

                                if (!get_byte(Network.WebSocket.State.NEW_FRAME, ref index, src_bytes))
                                    return;
                                OPcode = (Network.WebSocket.OPCode)(BYTE & (int)Network.WebSocket.Mask.OPCODE);
                                goto case Network.WebSocket.State.PAYLOAD_LENGTH_BYTE;
                            case Network.WebSocket.State.PAYLOAD_LENGTH_BYTE:
                                if (!get_byte(Network.WebSocket.State.PAYLOAD_LENGTH_BYTE, ref index, src_bytes))
                                    return;

                                if ((BYTE & (int)Network.WebSocket.Mask.FIN) == 0)
                                {
                                    host.onFailure(this, new Exception("Frames sent from client to server have MASK bit set to 1"));
                                    Close();
                                    return;
                                }

                                xor0 = 0;
                                //https://datatracker.ietf.org/doc/html/rfc6455#section-5.2
                                if (125 < (frame_bytes_left = BYTE & (int)Network.WebSocket.Mask.LEN)) //if 0-125, that is the payload length
                                {
                                    xor0 = frame_bytes_left == 126 ? //If 126, the following 2 bytes interpreted as a 16 -bit unsigned integer are the payload length.
                                               2 :
                                               8; //If 127, the following 8 bytes interpreted as a 64-bit unsigned integer (the most significant bit MUST be 0) are the payload length.
                                    frame_bytes_left = 0;
                                }

                                goto case Network.WebSocket.State.PAYLOAD_LENGTH_BYTES;
                            case Network.WebSocket.State.PAYLOAD_LENGTH_BYTES:
                                for (; 0 < xor0; xor0--)
                                    if (get_byte(Network.WebSocket.State.PAYLOAD_LENGTH_BYTES, ref index, src_bytes))
                                        frame_bytes_left = (frame_bytes_left << 8) | BYTE;
                                    else
                                        return;
                                goto case Network.WebSocket.State.XOR0;
                            case Network.WebSocket.State.XOR0:
                                if (get_byte(Network.WebSocket.State.XOR0, ref index, src_bytes))
                                    xor0 = BYTE;
                                else
                                    return;
                                goto case Network.WebSocket.State.XOR1;
                            case Network.WebSocket.State.XOR1:
                                if (get_byte(Network.WebSocket.State.XOR1, ref index, src_bytes))
                                    xor1 = BYTE;
                                else
                                    return;
                                goto case Network.WebSocket.State.XOR2;
                            case Network.WebSocket.State.XOR2:
                                if (get_byte(Network.WebSocket.State.XOR2, ref index, src_bytes))
                                    xor2 = BYTE;
                                else
                                    return;
                                goto case Network.WebSocket.State.XOR3;
                            case Network.WebSocket.State.XOR3:
                                if (get_byte(Network.WebSocket.State.XOR3, ref index, src_bytes))
                                    xor3 = BYTE;
                                else
                                    return;

                                switch (OPcode)
                                {
                                    case Network.WebSocket.OPCode.PING:
                                        allocate_frame_data(Network.WebSocket.OPCode.PONG);

                                        if (frame_bytes_left == 0)
                                        {
                                            host.onEvent(this, (int)Network.WebSocket.Event.PING);
                                            frame_ready();
                                            state = Network.WebSocket.State.NEW_FRAME;
                                            continue;
                                        }

                                        break;

                                    case Network.WebSocket.OPCode.CLOSE:

                                        if (sent_closing_frame) //received close confirmation
                                        {
                                            host.onEvent(this, (int)Network.WebSocket.Event.CLOSE);
                                            Close(); //gracefully the close confirmation frame was sent
                                            return;
                                        }

                                        allocate_frame_data(Network.WebSocket.OPCode.CLOSE);

                                        if (frame_bytes_left == 0)
                                        {
                                            host.onEvent(this, (int)Network.WebSocket.Event.CLOSE);
                                            frame_ready();
                                            state = Network.WebSocket.State.NEW_FRAME;
                                            continue;
                                        }

                                        break;
                                    case Network.WebSocket.OPCode.PONG: //discard
                                        //Pong frame MAY be sent unsolicited.  This serves as a
                                        //unidirectional heartbeat.  A response to an unsolicited Pong frame is not expected.
                                        host.onEvent(this, (int)Network.WebSocket.Event.PONG);
                                        state = frame_bytes_left == 0 ?
                                                    Network.WebSocket.State.NEW_FRAME :
                                                    Network.WebSocket.State.DISCARD;
                                        continue;
                                    default:
                                        if (frame_bytes_left == 0) //empty frame
                                        {
                                            host.onEvent(this, (int)Network.WebSocket.Event.EMPTY_FRAME);
                                            state = Network.WebSocket.State.NEW_FRAME;
                                            continue;
                                        }

                                        break;
                                }

                                start = index;
                                goto case Network.WebSocket.State.DATA0;
                            case Network.WebSocket.State.DATA0:
                                if (decode_and_continue(start, ref index, src_bytes))
                                    continue;
                                return;
                            case Network.WebSocket.State.DATA1:
                                if (need_more_bytes(Network.WebSocket.State.DATA1, ref start, ref index, src_bytes))
                                    return;
                                if (decode_byte_and_continue(xor1, ref start, ref index))
                                    continue;

                                goto case Network.WebSocket.State.DATA2;
                            case Network.WebSocket.State.DATA2:
                                if (need_more_bytes(Network.WebSocket.State.DATA2, ref start, ref index, src_bytes))
                                    return;
                                if (decode_byte_and_continue(xor2, ref start, ref index)) continue;

                                goto case Network.WebSocket.State.DATA3;
                            case Network.WebSocket.State.DATA3:
                                if (need_more_bytes(Network.WebSocket.State.DATA3, ref start, ref index, src_bytes))
                                    return;
                                if (decode_byte_and_continue(xor3, ref start, ref index)) continue;

                                if (decode_and_continue(start, ref index, src_bytes))
                                    continue;
                                return;

                            case Network.WebSocket.State.DISCARD:
                                var bytes = Math.Min(src_bytes - start, frame_bytes_left);
                                index += bytes; //discard
                                if ((frame_bytes_left -= bytes) == 0)
                                {
                                    state = Network.WebSocket.State.NEW_FRAME;
                                    continue;
                                }

                                state = Network.WebSocket.State.DISCARD; //trigger continue receiving more bytes
                                return;
                        }
                }

                bool get_byte(Network.WebSocket.State state_if_no_more_bytes, ref int index, int max)
                {
                    if (index == max)
                    {
                        state = state_if_no_more_bytes;
                        return false;
                    }

                    BYTE = receive_mate.Buffer![index++];
                    return true;
                }

                bool decode_and_continue(int start, ref int index, int max)
                {
                    for (; ; )
                    {
                        if (need_more_bytes(Network.WebSocket.State.DATA0, ref start, ref index, max))
                            return false;
                        if (decode_byte_and_continue(xor0, ref start, ref index))
                            return true;
                        if (need_more_bytes(Network.WebSocket.State.DATA1, ref start, ref index, max))
                            return false;
                        if (decode_byte_and_continue(xor1, ref start, ref index))
                            return true;
                        if (need_more_bytes(Network.WebSocket.State.DATA2, ref start, ref index, max))
                            return false;
                        if (decode_byte_and_continue(xor2, ref start, ref index))
                            return true;
                        if (need_more_bytes(Network.WebSocket.State.DATA3, ref start, ref index, max))
                            return false;
                        if (decode_byte_and_continue(xor3, ref start, ref index))
                            return true;
                    }
                }

                bool need_more_bytes(Network.WebSocket.State state_if_no_more_bytes, ref int start, ref int index, int max)
                {
                    if (index < max)
                        return false;

                    var src = receive_mate.Buffer!;
                    switch (OPcode) //query more bytes
                    {
                        case Network.WebSocket.OPCode.PING:
                        case Network.WebSocket.OPCode.CLOSE:
                            frame_data!.put_data(src, start, index);
                            break;
                        default:

                            receiver!.Write(src, start, index - start);
                            break;
                    }

                    state = frame_bytes_left == 0 ?
                                Network.WebSocket.State.NEW_FRAME :
                                state_if_no_more_bytes;
                    return true;
                }

                bool decode_byte_and_continue(int XOR, ref int start, ref int index)
                {
                    var src = receive_mate.Buffer!;

                    src[index] = (byte)(src[index++] ^ XOR);
                    if (0 < --frame_bytes_left)
                        return false;

                    state = Network.WebSocket.State.NEW_FRAME;

                    switch (OPcode)
                    {
                        case Network.WebSocket.OPCode.PING:
                        case Network.WebSocket.OPCode.CLOSE:
                            frame_data!.put_data(src, start, index);
                            host.onEvent(this, (int)OPcode);
                            frame_ready();
                            return true;
                        default:

                            receiver!.Write(src, start, index - start);
                            return true;
                    }
                }
                #endregion

                public class Client : TCP<SRC, DST>
                {
                    #region> WebSocket Client code
                    #endregion> Network.TCP.WebSocket.Client

                    private ClientWebSocket ws;
                    public Func<ClientWebSocket> newClientWebSocket = () => new ClientWebSocket();

                    private int transmit_lock = 1;
                    public readonly int bufferSize;
                    public Client(string name, Func<TCP<SRC, DST>, Channel> new_channel, int bufferSize, TimeSpan timeout) : base(name, new_channel, bufferSize, timeout) { this.bufferSize = bufferSize; }

                    public Uri server { get; private set; }

                    public void Connect(Uri server, Action<SRC> onConnected, Action<Exception> onConnectingFailure) => Connect(server, onConnected, onConnectingFailure, TimeSpan.FromSeconds(5));

                    public void Connect(Uri server, Action<SRC> onConnected, Action<Exception> onConnectingFailure, TimeSpan connectingTimout) //needed exactly URI, not IPEndPoint. because URI is provided HTTP-host-header value
                    {
                        this.server = server;
                        transmit_lock = 1;
                        ws = newClientWebSocket();

                        var transmit_buffer = new byte[bufferSize];
                        var receive_buffer = new byte[bufferSize];

                        ws.Options.SetBuffer(bufferSize, bufferSize, receive_buffer);

                        Action<AdHoc.BytesSrc> transmitting = async src =>
                                                              {
                                                                  if (Interlocked.Exchange(ref transmit_lock, 1) == 1)
                                                                      return;

                                                                  for (int len; 0 < (len = channels.transmitter!.Read(transmit_buffer, 0, transmit_buffer.Length));)
                                                                      await ws.SendAsync(new ReadOnlyMemory<byte>(transmit_buffer, 0, len), WebSocketMessageType.Binary, true, CancellationToken.None);

                                                                  transmit_lock = 0;
                                                                  channels.onSent?.Invoke(channels);
                                                              };
                        var receiving = async () =>
                                        {
                                            for (; ; )
                                                try
                                                {
                                                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(receive_buffer), CancellationToken.None);

                                                    if (result.MessageType == WebSocketMessageType.Close)
                                                    {
                                                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                                                        return;
                                                    }

                                                    channels.receiver!.Write(receive_buffer, 0, result.Count);
                                                }
                                                catch (Exception e)
                                                {
                                                    onEvent(channels, (int)Network.Channel.Event.EXT_INT_DISCONNECT);
                                                    break;
                                                }
                                        };

                        channels.on_disposed = ch => ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);

                        ws
                            .ConnectAsync(server, new CancellationTokenSource(connectingTimout).Token)
                            .ContinueWith(
                                          t =>
                                          {
                                              if (t.IsCompletedSuccessfully)
                                              {
                                                  onConnected(channels.transmitter!);
                                                  Task.Run(receiving);

                                                  transmit_lock = 0;
                                                  channels.transmitter!.subscribeOnNewBytesToTransmitArrive(transmitting); //trigger sending
                                              }
                                              else
                                              {
                                                  onFailure(this, t.Exception!);
                                                  onConnectingFailure(t.Exception!);
                                              }
                                          });

                        toString = new StringBuilder(50)
                                   .Append("Client ")
                                   .Append(name)
                                   .Append(" -> ")
                                   .Append(server)
                                   .ToString();
                    }

                    private string toString;
                    public override string ToString() => toString;

                    void Disconnect() => ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
            }

            public class Server : TCP<SRC, DST>
            {
                #region> Server code
                #endregion> Network.TCP.Server

                public Server(string name,
                              Func<TCP<SRC, DST>, Channel> new_channel,
                              int bufferSize,
                              TimeSpan timeout,
                              int Backlog,
                              Func<IPEndPoint, Socket>? socketBuilder,
                              params IPEndPoint[] ips) : base(name, new_channel, bufferSize, timeout)
                {
                    maintenance_thread = new Thread(() => { Thread.Sleep(maintenance(DateTime.Now.Millisecond)); })
                    {
                        Name = "Maintain server " + name,
                        IsBackground = true
                    };
                    maintenance_thread.Start();

                    bind(Backlog, socketBuilder, ips);
                }

                public readonly List<Socket> tcp_listeners = new();

                public void bind(int Backlog, Func<IPEndPoint, Socket>? socketBuilder, params IPEndPoint[] ips)
                {
                    var sb = new StringBuilder(50)
                             .Append("Server ")
                             .Append(name);

                    socketBuilder ??= ip => new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    foreach (var ip in ips)
                    {
                        sb.Append('\n')
                          .Append("\t\t -> ")
                          .Append(ip);
                        var tcp_listener = socketBuilder(ip);
                        tcp_listeners.Add(tcp_listener);
                        tcp_listener.Bind(ip);
                        tcp_listener.Listen(Backlog);
                        var on_accept_args = new SocketAsyncEventArgs();

                        EventHandler<SocketAsyncEventArgs> on_accept_handler = (_, _) =>
                                                                               {
                                                                                   do
                                                                                   {
                                                                                       if (on_accept_args.SocketError == SocketError.Success)
                                                                                           allocate().receiver_connected(on_accept_args.AcceptSocket!);

                                                                                       on_accept_args.AcceptSocket = null; //socket must be cleared since the context object is being reused
                                                                                   }
                                                                                   while (!tcp_listener.AcceptAsync(on_accept_args));
                                                                               };
                        on_accept_args.Completed += on_accept_handler;
                        if (!tcp_listener.AcceptAsync(on_accept_args))
                            on_accept_handler(tcp_listener, on_accept_args);
                    }

                    toString = sb.ToString();
                }

                private readonly Thread maintenance_thread;

                // Async forces the maintenance thread to wake up and perform maintenance immediately,
                // regardless of the current schedule or timeout.
                public void maintain() => maintenance_thread.Interrupt(); //kick maintenance

                public long maintenance_duty_cycle = 5000L;

                // This method iterates through all active channels to determine the time
                // for the next maintenance operation. It can be overridden if a different
                // maintenance calculation logic is required
                protected virtual TimeSpan maintenance(long time)
                {
                    var timeout = maintenance_duty_cycle;
                    for (var channel = channels; channel != null; channel = channel.next)
                        if (channel.is_active)
                            timeout = Math.Min(timeout, channel.maintenance(time));

                    return TimeSpan.FromMilliseconds(timeout);
                }

                private string toString;
                public override string ToString() => toString;

                public void shutdown()
                {
                    tcp_listeners.ForEach(socket => socket.Close());
                    for (var ch = channels; ch != null; ch = ch.next)
                        if (ch.is_active)
                            ch.Close_and_dispose();
                }
            }

            public class Client : TCP<SRC, DST>
            {
                #region> Client code
                #endregion> Network.TCP.Client
                private readonly SocketAsyncEventArgs onConnecting = new();

                public Client(string name, Func<TCP<SRC, DST>, Channel> new_channel, int bufferSize, TimeSpan timeout) : base(name, new_channel, bufferSize, timeout) => onConnecting.Completed += (_, _) => OnConnected();

                private void OnConnected()
                {
                    if (onConnecting.SocketError != SocketError.Success || onConnecting.LastOperation != SocketAsyncOperation.Connect)
                        return;
                    channels.transmiter_connected(onConnecting.ConnectSocket);
                    onConnected!(channels.transmitter!);
                }

                private Action<SRC>? onConnected; //notify

                public void Connect(IPEndPoint server, Action<SRC> onConnected, Action<Exception> onConnectingFailure) => Connect(server, onConnected, onConnectingFailure, TimeSpan.FromSeconds(5));

                public void Connect(IPEndPoint server, Action<SRC> onConnected, Action<Exception> onConnectingFailure, TimeSpan connectingTimout)
                {
                    toString = new StringBuilder(50)
                               .Append("Client ")
                               .Append(name)
                               .Append(" -> ")
                               .Append(server)
                               .ToString();

                    this.onConnected = onConnected;

                    onConnecting.RemoteEndPoint = server; //The caller must set the SocketAsyncEventArgs.RemoteEndPoint property to the IPEndPoint of the remote host to connect to.

                    if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, onConnecting))
                        Task.Delay(connectingTimout)
                            .ContinueWith(
                                          _ =>
                                          {
                                              if (channels.ConnectSocket is not { Connected: true })
                                                  onConnectingFailure(new Exception($"Connection to the {server} in {connectingTimout}, timeout"));
                                          });
                    else
                        OnConnected();
                }

                public void Disconnect()
                {
                    if (channels.ext == null || !channels.ext.Connected)
                        return;
                    channels.Close();
                }

                private string toString;
                public override string ToString() => toString;
            }
        }

        class Wire
        {
            protected readonly byte[] buffer;

            protected AdHoc.BytesSrc? src;
            protected Action<AdHoc.BytesSrc>? subscriber;

            public Wire(AdHoc.BytesSrc src, AdHoc.BytesDst dst, int buffer_size)
            {
                buffer = new byte[buffer_size];
                connect(src, dst);
            }

            public void connect(AdHoc.BytesSrc src, AdHoc.BytesDst dst)
            {
                this.src?.subscribeOnNewBytesToTransmitArrive(subscriber); //off hook
                subscriber = (this.src = src).subscribeOnNewBytesToTransmitArrive(
                                                                                  _ =>
                                                                                  {
                                                                                      for (int len; 0 < (len = src.Read(buffer, 0, buffer.Length));)
                                                                                          dst.Write(buffer, 0, len);
                                                                                  });
            }
        }

        class UDP
        {
            //use TCP implementation over UDP Wireguard https://www.wireguard.com/
        }
    }
}