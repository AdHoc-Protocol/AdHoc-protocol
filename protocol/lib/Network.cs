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
using System.Diagnostics;
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
                INT_EXT_CONNECT = 6,            // Event triggered when the server initiates a connection to the client.
                EXT_INT_CONNECT = 7,            // Event triggered when the client initiates a connection to the server.
                CLOSE = OPCode.CLOSE, // Close event, using the WebSocket CLOSE opcode.
                PING = OPCode.PING,  // Ping event, using the WebSocket PING opcode to check connection health.
                PONG = OPCode.PONG,  // Pong event, using the WebSocket PONG opcode as a response to ping.
                EMPTY_FRAME = 11,           // Event for an empty frame, often used as a keep-alive or placeholder.
            }

            // Internal enum for WebSocket masking
            internal enum Mask
            {
                FIN = 0b1000_0000, // Final fragment flag
                OPCODE = 0b0000_1111, // Opcode mask
                MASK = 0b1000_0000, // Mask bit
                LEN = 0b0111_1111, // Payload length mask
            }

            public enum OPCode
            {
                CONTINUATION = 0x00, // Continuation frame opcode
                TEXT_FRAME = 0x01, // Text frame opcode
                BINARY_FRAME = 0x02, // Binary frame opcode
                CLOSE = 0x08, // Connection close opcode
                PING = 0x09, // Ping frame opcode
                PONG = 0x0A  // Pong frame opcode
            }

            internal enum State
            {
                HANDSHAKE = 0,  // Handshake state
                NEW_FRAME = 1,  // New frame received state
                PAYLOAD_LENGTH_BYTE = 2,  // Processing the first byte of payload length
                PAYLOAD_LENGTH_BYTES = 3,  // Processing subsequent bytes of payload length
                XOR0 = 4,  // First XOR byte
                XOR1 = 5,  // Second XOR byte
                XOR2 = 6,  // Third XOR byte
                XOR3 = 7,  // Fourth XOR byte
                DATA0 = 8,  // First data byte
                DATA1 = 9,  // Second data byte
                DATA2 = 10, // Third data byte
                DATA3 = 11, // Fourth data byte
                DISCARD = 12  // Discarding state
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
            public readonly Channel channels;  // Head of the one-way linked list of communication channels. Each channel points to the next via ch.next.
            private readonly Action<SocketAsyncEventArgs> buffers;   // Thread-local pool of reusable ByteBuffers.
            protected const long FREE = -1; // Constant in receive_time field representing a free channel.

            public readonly Func<TCP<SRC, DST>, Channel> new_channel; // Factory function for creating new channels.
            public TimeSpan channels_idle_timeout;
            public TimeSpan trusted_channels_idle_timeout = new TimeSpan(0, 1, 0);

            public string name; // Name identifier for this TCP host instance.

            public TCP(string name, Func<TCP<SRC, DST>, Channel> new_channel, int buffer_size, TimeSpan channels_idle_timeout)
            {
                this.channels_idle_timeout = channels_idle_timeout;
                this.name = name;
                buffers = dst => dst.SetBuffer(ArrayPool<byte>.Shared.Rent(buffer_size), 0, buffer_size);
                channels = (this.new_channel = new_channel)(this); // Initialize channels using the factory function.
            }

            // Attempts to find and allocate a free channel from the existing pool, or creates a new one if necessary.
            // Returns: An available channel for communication between SRC and DST types.
            protected Channel allocate()
            {
                var ch = channels; // Start with the head of the channel linked list
                for (; !ch.activate(); ch = ch.next)
                    // If we've reached the end of the channel chain without finding a free channel,
                    // we need to extend the chain with a new channel
                    if (ch.next == null)
                    {
                        // Factory pattern: Create new channel instance using the provided constructor function
                        var ret = new_channel(this);

                        // Initialize channel timestamps to activate
                        ret.receive_time = ret.transmit_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        // Append the new channel to the end of the chain. Loop handles race conditions where another thread might have added a channel.
                        while (Interlocked.CompareExchange(ref ch.next, ret, null) != null)
                            ch = ch.next;

                        return ret;
                    }

                // Channel successfully allocated - update its transmit timestamp before returning
                ch.transmit_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return ch;
            }

            // Event handler (can be implemented as a collection of handlers) for failures, logging exceptions and optionally printing stack traces in debug mode.
            public Action<object, Exception> onFailure = (src, t) =>
                                                         {
                                                             Console.WriteLine("onFailure " + src);
#if DEBUG
                                                             Console.WriteLine(new Exception("onFailure").StackTrace);
#endif
                                                             Console.WriteLine(t);
                                                         };


            // Forces the maintenance thread to wake up and perform maintenance immediately,
            // regardless of the current schedule or timeout.
            public virtual void trigger_maintenance() { }

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
                                                      Console.WriteLine("debugging stack of onEvent");
                                                      Console.WriteLine(new StackTrace().ToString());

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
                public EndPoint peer_ip => ext!.RemoteEndPoint!; // The IP address of the peer.
                public long peer_id = 0;                  // Identifier for the peer.
                public long session_id = 0;                  // Session identifier.

                public long receive_time = FREE; // Timestamp of the last received data. If receive_time == FREE, this channel is available  for reuse.

                // Try to atomically mark an existing channel as in-use by updating its receive_time from FREE to current timestamp.
                public bool activate() => Interlocked.CompareExchange(ref receive_time, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), FREE) != FREE;
                public long transmit_time = FREE;          // Timestamp of the last transmitted data.
                public bool is_active => 0 < receive_time; // Check if the channel is active

                public readonly TCP<SRC, DST> host; // Reference to the host TCP instance.

                public bool trusted; // Indicates if the channel can be re-established upon connection interruption.

                public Channel(TCP<SRC, DST> host)
                {
                    receive_mate.Completed += OnCompleted;
                    DisconnectReuseSocket = true;
                    onNewBytesToTransmitArrive =
                        _ =>
                        {
                            if (ext != null && idle_transmitter_activated())
                                transmit();
                        };
                }
                #region close
                //To ensure that all outgoing data is fully transmitted to the peer, it's necessary to close the socket with a reasonable delay.
                //request asynchronous closing this channel with delay_seconds
                public void Close(int delay_seconds)
                {
                    schedule_maintenance();
                    receive_time = transmit_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + delay_seconds * 1000L - (long)host.channels_idle_timeout.TotalMilliseconds; //reasonable delay
                    host.trigger_maintenance();
                }

                //force request asynchronous closing this channel ASAP
                public void Close()
                {
                    trusted = false;
                    schedule_maintenance();
                    receive_time = transmit_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - (long)host.channels_idle_timeout.TotalMilliseconds;
                    host.trigger_maintenance();
                }

                // Close the connection without cleanup to allow trusted user to reconnect within the timeout session and potentially restore the previous state.
                // Note: This approach may lead to a DDoS risk; use it only for trusted users connections.
                //
                // Do not call this method directly; doing so may disrupt ongoing send/receive operations, risking crashes and inconsistent states.
                // Use close() instead, as it ensures the channel is not busy.
                //
                // !! Never call this method directly from a sending or receiving thread; instead, trigger the maintenance thread.
                //
                public virtual void Close_not_dispose()
                {
                    if (ext == null) return; // Exit if the connection is not initialized or already closed.

                    host.onEvent(this, (int)Network.Channel.Event.INT_EXT_DISCONNECT); // Notify the host of the disconnection event.
                    //!!!!!!!!! CRITICAL:
                    //When using a connection-oriented Socket, always call the Shutdown method before
                    //closing the Socket. This ensures that all data is sent and received on the
                    //connected socket before it is closed.
                    //
                    //Call the Close method to free all managed and unmanaged resources associated
                    //with the Socket. Do not attempt to reuse the Socket after closing.
                    try { ext?.Shutdown(SocketShutdown.Both); }
                    catch (Exception e) { }

                    //Call the Close method to free all managed and unmanaged resources associated
                    //with the Socket. Do not attempt to reuse the Socket after closing.
                    ext?.Close();
                    Thread.SpinWait(1); // Pause briefly to allow for any necessary cleanup.
                    ext = null;         // Reset connection state variables.
                    activate_transmitter();

                    // If neither transmitter nor receiver is open, proceed to close and dispose resources.
                    if ((transmitter == null || !transmitter.isOpen()) && (receiver == null || !receiver.isOpen()))
                        Close_and_dispose();
                }

                //Do not call this method directly; doing so may disrupt ongoing send/receive operations, risking crashes and inconsistent states.
                //Use close() instead, as it ensures the channel is not busy.
                //
                // !! Never call this method directly from a sending or receiving thread; instead, trigger the maintenance thread.
                //
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
                #endregion
                public Action<Channel>? on_connected; // Event handler (can be implemented as a collection of handlers)  Used for signaling when a connection is established.
                public Action<Channel>? on_disposed;  // Event handler (can be implemented as a collection of handlers)  Signals that the entity is being disposed.
                #region Transmitting
                public SRC? transmitter;

                // Event handler (which can be a collection of handlers) invoked when all available bytes of the packet
                // have been successfully transmitted through the socket.
                public Action<Channel>? on_all_packs_sent;

                protected override void OnCompleted(SocketAsyncEventArgs _transmit) // Notifies that the asynchronous operation has completed.
                {
                    if (locked_for_maintenance()) return; // Check if the channel is locked by the maintenance process.
                    try
                    {
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
                                    activate_transmitter();
                                    transmit();
                                    return;
                            }
                        else
                            host.onFailure(this, new Exception("SocketError:" + _transmit.SocketError));
                    }
                    finally { pending_send_receive_completed(); }
                }

                internal void transmiter_connected(Socket? ext) // Handler triggered when an outgoing connection to a remote peer is established from this host.
                {
                    this.ext = ext;
                    transmit_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    host.onEvent(this, (int)Network.Channel.Event.INT_EXT_CONNECT); // Notify host of successful outgoing connection.

                    if (Buffer == null)
                        host.buffers(this);
                    else
                        SetBuffer(0, Buffer.Length);

                    idle_transmitter();                                                           // the transmitter is ready to transmit.
                    transmitter!.subscribeOnNewBytesToTransmitArrive(onNewBytesToTransmitArrive); // Subscribe to new outgoing data notifications.

                    if (receiver == null) return;                     // Half-duplex mode: No receiving planned for this channel.
                    if (receive_mate.Buffer == null) host.buffers(receive_mate); // Allocate receive buffer if needed.

                    on_connected?.Invoke(this); // Notify that the channel is fully established for transmission.

                    if (!ext!.ReceiveAsync(receive_mate)) receive(); // Initiates read operation to read bytes from `ext` into `Buffer`. If going asynchronous, notified upon completion in the `OnCompleted()` method.
                }

                // Callback function triggered when new bytes are available in the INTernal source layer for transmission via EXTernal.
                protected readonly Action<AdHoc.BytesSrc> onNewBytesToTransmitArrive;

                private void transmit()
                {
                    do
                        while (transmit(Buffer!)) // Load bytes from SRC to transmit.
                            if (ext!.SendAsync(this))
                                return;            //If going asynchronous, notified upon completion in the `OnCompleted()` method.
                    while (transmitter_in_use()); // Loop until transmission is complete.

                    on_all_packs_sent?.Invoke(this); // Notify that all data was sent; transmitter returns to idle.
                }

                // Sets the buffer for data transmission. Uses SetBuffer to specify the location and length of data to send.
                // Returns false if there is no data to send (i.e., Read returns less than 1 byte).
                protected virtual bool transmit(byte[] dst)
                {
                    var bytes = transmitter!.Read(dst, 0, dst.Length);
                    if (bytes < 1) return false; // No data available for transmission
                    SetBuffer(0, bytes);          // Configure buffer with the read data
                    return true;
                }

                protected volatile int transmit_lock = 1;
                protected bool transmitter_in_use() => Interlocked.Exchange(ref transmit_lock, 0) != 0;

                protected void idle_transmitter() => transmit_lock = 0;

                protected bool idle_transmitter_activated() => Interlocked.Increment(ref transmit_lock) == 1;

                protected void activate_transmitter() => Interlocked.Increment(ref transmit_lock);
                #endregion
                #region Receiving
                public DST? receiver; // Destination for bytes received from the external socket.
                internal readonly SocketAsyncEventArgs receive_mate = new();
                public volatile bool stop_receiving; // Flag to stop data reception when set to true. Used for controlling reception in the channel stages workflow.

                public void start_receive() // re-Starts receiving data if previously stopped, resetting stop flag and initiating read.
                {
                    if (!stop_receiving)
                        return;
                    stop_receiving = false;

                    // Initiates a read operation to receive bytes from `ext` into `receive_mate.Buffer`.
                    // If the operation is asynchronous, it is triggered via ReceiveAsync, and OnCompleted() will be called upon completion.
                    // If the operation cannot proceed asynchronously, the `receive()` method is called synchronously.
                    receive_mate.SetBuffer(receive_mate.Buffer);
                    if (!ext!.ReceiveAsync(receive_mate)) receive();
                }

                protected void OnCompleted(object? src, SocketAsyncEventArgs _receive)
                {
                    if (locked_for_maintenance()) return; // Check if the channel is locked by the maintenance process.
                    receive_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    try
                    {
                        switch (_receive.SocketError)
                        {
                            //LastOperation
                            //more easily facilitates using a single completion callback delegate for multiple kinds of
                            //asynchronous socket operations. This property describes the asynchronous socket operation that was most recently completed
                            case SocketError.Success:
                                switch (_receive.LastOperation)
                                {
                                    case SocketAsyncOperation.Disconnect:
                                        host.onEvent(this, (int)Network.Channel.Event.EXT_INT_DISCONNECT);
                                        return;
                                    case SocketAsyncOperation.Receive:
                                        receive();
                                        return;
                                }

                                break;
                            case SocketError.TimedOut:
                                host.onEvent(this, (int)Network.Channel.Event.TIMEOUT);
                                break;
                            default:
                                host.onFailure(this, new Exception("SocketError:" + _receive.SocketError));
                                break;
                        }
                    }
                    finally { pending_send_receive_completed(); }
                }

                internal void receiver_connected(Socket ext) // Initializes receiver when a new external connection is established, setting up buffers and handlers.
                {
                    this.ext = ext;
                    receive_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    host.onEvent(this, (int)Network.Channel.Event.EXT_INT_CONNECT); // Notify host that an external connection has been accepted.
                    if (!ext.Connected)                                            //The incoming connection is closed within the event handler.
                        return;                                                     // If connection was closed during `host.onEvent` event handling, clean up and exit.

                    if (receive_mate.Buffer == null) host.buffers(receive_mate);

                    stop_receiving = false;


                    if (transmitter != null)
                    {
                        idle_transmitter();
                        if (Buffer == null) host.buffers(this);
                        transmitter.subscribeOnNewBytesToTransmitArrive(onNewBytesToTransmitArrive); // Subscribe for bytes transmission events.
                    }

                    on_connected?.Invoke(this); // Signal that the channel is fully established.

                    // Initiates a read operation to receive bytes from `ext` into `receive_mate.Buffer`.
                    // If the operation is asynchronous, it is triggered via ReceiveAsync, and OnCompleted() will be called upon completion.
                    // If the operation cannot proceed asynchronously, the `receive()` method is called synchronously.
                    if (!this.ext!.ReceiveAsync(receive_mate)) receive();
                }

                private void receive() // Processes incoming data in the `receive_mate.Buffer`
                {
                    try
                    {
                        do
                        {
                            if (receive_mate.BytesTransferred == 0) //the number of bytes transferred in the socket operation.
                            {                                        //If zero is returned from last read operation, the remote end has closed the connection.
                                Close();
                                return;
                            }

                            if (stop_receiving) return;
                            receive(receive_mate.Buffer!, receive_mate.Offset + receive_mate.BytesTransferred);
                            if (stop_receiving) return;
                        }
                        while (!ext!.ReceiveAsync(receive_mate));
                    }
                    catch (Exception e) { host.onFailure(this, e); } // Handle any exceptions encountered during reading.
                }

                // Writes the received bytes to the internal layer destination.
                // Ensure that `receive_mate.Buffer` is properly set before calling this method.
                protected virtual void receive(byte[] src, int src_bytes) => receiver!.Write(src, 0, src_bytes);
                #endregion

                public Channel? next;
                #region maintenance

                private volatile int maintenance_lock = 0; // Prevents channel closure during ongoing `completed` operations or new entries into the `completed` method during maintenance lock.

                // Handles channel maintenance operations.
                // This method should not be called directly; it is exclusively executed by the `maintenance_thread`.
                //
                // Maintenance is triggered either when the channel's `idle_timeout` is reached or upon an explicit request (e.g., for channel closure).
                // It only begins after all pending send and receive operations for the channel are completed.
                //
                // By default, this method closes the channel, but it can be extended to include additional maintenance functionality as needed.
                protected internal void maintenance()
                {
                    // If an external connection is null or connection is not trusted, close and dispose.
                    if (ext == null || !trusted) // Close and dispose if connection is not trusted or closed.
                        Close_and_dispose();      // Dispose and release resources.
                    else
                        Close_not_dispose(); // Close, but preserve the channel state.
                }

                protected internal bool waiting_for_maintenance() { return maintenance_lock < 0; }

                protected internal bool ready_for_maintenance() { return maintenance_lock == int.MinValue; }
                protected internal void maintenance_completed() { Interlocked.Exchange(ref maintenance_lock, 0); }

                protected void pending_send_receive_completed() { Interlocked.Decrement(ref maintenance_lock); }

                // if completedLock < 0 locked for maintenance
                protected bool locked_for_maintenance()
                {
                    int t;
                    do
                        t = maintenance_lock;
                    while (Interlocked.CompareExchange(ref maintenance_lock, t < 0 ?
                                                                                 t :
                                                                                 t + 1, t) != t);
                    return t < 0;
                }

                protected internal void schedule_maintenance()
                {
                    int t;
                    do
                        t = maintenance_lock;
                    while (Interlocked.CompareExchange(ref maintenance_lock, t < 0 ?
                                                                                 t :
                                                                                 int.MinValue + t, t) != t);
                }
                #endregion
            }

            public class WebSocket : Channel
            {
                #region> WebSocket code
                #endregion> Network.TCP.WebSocket

                // WebSocket requires a TCP server with a buffer size of at least 256 bytes.
                // Ensure the buffer is large enough to hold any entire HTTP header field if parsing.
                public WebSocket(TCP<SRC, DST> host) : base(host) { }

                /// <summary>
                /// Closes the WebSocket connection gracefully by sending a CLOSE frame with the specified code and reason.
                /// </summary>
                /// <param name="code">The close code indicating the reason for closure.</param>
                /// <param name="why">Optional reason for closure. If null, only the code is sent.</param>
                public void close_gracefully(int code, string? why)
                {
                    // Obtain a CLOSE frame from the frame pool and set the opcode for closure.
                    var frame = frames.Value!.get();
                    frame.OPcode = Network.WebSocket.OPCode.CLOSE;

                    // Set the close code in the first two bytes of the frame buffer.
                    frame.buffer[0] = (byte)(code >> 8);
                    frame.buffer[1] = (byte)code;

                    if (why == null) // If no reason is provided, only send the code.
                        frame.buffer_bytes = 2;
                    else
                    {
                        // Copy the reason into the buffer, starting after the close code.
                        for (int i = 0, max = why.Length; i < max; i++) frame.buffer[i + 2] = (byte)why[i];
                        frame.buffer_bytes = 2 + why.Length;
                    }

                    // Mark this frame as the urgent frame data to be transmitted immediately.
                    recycle_frame(Interlocked.Exchange(ref urgent_frame_data, frame_data));
                    onNewBytesToTransmitArrive(null); // Trigger transmission of the new frame
                }


                public void ping(string? msg)
                {
                    var frame = frames.Value!.get(); //try reuse
                    frame.OPcode = Network.WebSocket.OPCode.CLOSE;

                    if (msg == null)
                        frame.buffer_bytes = 0;
                    else
                    {
                        for (int i = 0, max = msg.Length; i < max; i++)
                            frame.buffer[i] = (byte)msg[i];

                        frame.buffer_bytes = msg.Length;
                    }

                    recycle_frame(Interlocked.Exchange(ref urgent_frame_data, frame_data));
                    onNewBytesToTransmitArrive(null); //trigger transmitting
                }

                public override void Close_not_dispose()
                {
                    if (ext == null) return;
                    state = Network.WebSocket.State.HANDSHAKE;
                    sent_closing_frame = false;
                    frame_bytes_left = BYTE = xor0 = xor1 = xor2 = xor3 = 0;
                    OPcode = Network.WebSocket.OPCode.CONTINUATION;
                    if (frame_data != null) recycle_frame(frame_data);
                    if (urgent_frame_data != null) recycle_frame(urgent_frame_data);
                    base.Close();
                }
                #region Transmitting
                private bool sent_closing_frame;
                volatile ControlFrameData? urgent_frame_data;

                // Use SetBuffer to specify the data range in `dst` to send; return false if no data is available.
                protected override bool transmit(byte[] dst)
                {
                    // Attempt to retrieve any urgent frame data; reset `urgent_frame_data` if retrieved.
                    var frame_data = Interlocked.Exchange(ref urgent_frame_data, null);

                    if (frame_data == null)
                    {
                        // If no urgent data, use the primary frame data. Check if a ready frame is available.
                        frame_data = this.frame_data;
                        if (!catch_ready_frame())
                            frame_data = null;
                    }


                    //https://datatracker.ietf.org/doc/html/rfc6455#section-5.2

                    //0                   1                   2                   3
                    //0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    //+-+-+-+-+-------+-+-------------+-------------------------------+
                    //|F|R|R|R| opcode|M| Payload len |    Extended payload length    |
                    //|I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
                    //|N|V|V|V|       |S|             |   (if payload len==126/127)   |
                    //| |1|2|3|       |K|             |                               |
                    //+-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +

                    // Calculate starting offset to write into `dst` based on header length requirements.
                    // Offset by 10 bytes to account for potential maximum header size.
                    var s = (frame_data != null ?
                                 frame_data.buffer_bytes + 2 :
                                 0) + 10;

                    // Read data into `dst` starting at offset `s`.
                    var len = transmitter!.Read(dst, s, dst.Length - s);

                    if (0 < len) // If data was read, process the WebSocket frame header
                    {
                        var max = s + len; // Final end position in `dst` after the payload is written.

                        switch (len) // Build the WebSocket frame header based on payload length.
                        {
                            case < 126:
                                // Payload length is 0-125 bytes, encoded directly as 7 bits.
                                dst[s -= 2] = (int)Network.WebSocket.Mask.FIN | (int)Network.WebSocket.OPCode.BINARY_FRAME; // Final binary frame marker.
                                dst[s + 1] = (byte)len;                                                                    // Directly encode payload length.
                                break;

                            case < 0x1_0000:
                                // Payload length is 126-65,535 bytes, requiring a 16-bit extended length field.
                                dst[s -= 4] = (int)Network.WebSocket.Mask.FIN | (int)Network.WebSocket.OPCode.BINARY_FRAME; // Final binary frame marker.
                                dst[s + 1] = 126;                                                                          // 126 signals that a 16-bit length field follows.
                                dst[s + 2] = (byte)(len >> 8);                                                             // Most significant byte of length.
                                dst[s + 3] = (byte)len;                                                                    // Least significant byte of length.
                                break;

                            default:
                                // Payload length exceeds 65,535 bytes, requiring a 64-bit extended length field.
                                dst[s -= 10] = (int)Network.WebSocket.Mask.FIN | (int)Network.WebSocket.OPCode.BINARY_FRAME; // Final binary frame marker.
                                dst[s + 1] = 127;                                                                          // 127 signals that a 64-bit length field follows.

                                // 8-byte extended length field (most significant 4 bytes are zero).
                                dst[s + 2] = 0;
                                dst[s + 3] = 0;
                                dst[s + 4] = 0;
                                dst[s + 5] = 0;
                                dst[s + 6] = (byte)(len >> 24); // Most significant byte of the length.
                                dst[s + 7] = (byte)(len >> 16);
                                dst[s + 8] = (byte)(len >> 8);
                                dst[s + 9] = (byte)len; // Least significant byte of the length.
                                break;
                        }

                        // If a control frame (e.g., CLOSE frame) is set, write it into `dst`.
                        if (frame_data != null)
                        {
                            sent_closing_frame = frame_data.OPcode == Network.WebSocket.OPCode.CLOSE;   // Flag if the frame is a CLOSE frame.
                            recycle_frame(frame_data.get_frame(dst, s -= frame_data.buffer_bytes + 2)); // Write control frame to `dst`.
                        }

                        // Set the buffer to start at position `s` for transmission and send `max - s` bytes.
                        SetBuffer(s, max - s);
                        return true;
                    }

                    // If no data was read and no control frame is pending, reset the buffer and return false.
                    if (frame_data == null)
                    {
                        SetBuffer(0, 0); // Reset buffer size to default.
                        return false;
                    }

                    // Send any available control frame if there is no payload.
                    sent_closing_frame = frame_data.OPcode == Network.WebSocket.OPCode.CLOSE;
                    recycle_frame(frame_data.get_frame(dst, 0)); // Write control frame to `dst`.

                    // Set buffer to transmit the control frame.
                    SetBuffer(0, s); // Set buffer to begin from the start with `s` bytes for transmission.
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

                // Holds the current control frame data for transmission, used for WebSocket operations.
                volatile ControlFrameData? frame_data;

                // A lock state indicating the status of the frame data, such as ready or in standby.
                volatile int frame_lock;

                // Allocates a new control frame with the specified operation code (e.g., CLOSE, PING, etc.).
                // This function will replace `frame_data` if the frame lock is not in the `FRAME_READY` state.
                protected void allocate_frame_data(Network.WebSocket.OPCode OPcode)
                {
                    // Ensure `frame_lock` is set to `FRAME_STANDBY` only if the current state is `FRAME_READY`.
                    // This prevents accidental allocation when the frame is already in use.
                    if (Interlocked.CompareExchange(ref frame_lock, FRAME_STANDBY, FRAME_READY) != FRAME_READY)
                    {
                        // Set `frame_lock` to `FRAME_STANDBY` to indicate frame allocation is in progress.
                        Interlocked.Exchange(ref frame_lock, FRAME_STANDBY);

                        // Obtain a fresh frame from the frame pool.
                        frame_data = frames.Value!.get();
                    }

                    // Reset the frame’s buffer size and set its operation code.
                    frame_data.buffer_bytes = 0;
                    frame_data.OPcode = OPcode;
                }

                // Recycles the specified control frame, returning it to the frame pool for future reuse.
                // If `frame_data` is still set to the specified frame, atomically replace it with `null`.
                protected void recycle_frame(ControlFrameData? frame)
                {
                    if (frame == null) return;

                    // Atomically clear `frame_data` if it still points to the specified frame.
                    Interlocked.CompareExchange(ref frame_data, null, frame);

                    // Return the frame to the frame pool.
                    frames.Value!.put(frame);
                }

                // Marks the current frame as ready for transmission and triggers the transmission process.
                // This activates the frame lock for sending and notifies the transmission handler.
                protected void frame_ready()
                {
                    Interlocked.Exchange(ref frame_lock, FRAME_READY); // Activate sending.
                    onNewBytesToTransmitArrive(null);                  // Trigger transmission.
                }

                // Checks if the frame is in a ready state and atomically clears the lock.
                // Returns true if the frame was ready and successfully reset; otherwise, false.
                protected bool catch_ready_frame() => Interlocked.CompareExchange(ref frame_lock, 0, FRAME_READY) == FRAME_READY;

                protected const int FRAME_STANDBY = 1, FRAME_READY = 2;

                protected class ControlFrameData
                {
                    // WebSocket operation code for control frames (e.g., CLOSE, PING).
                    public Network.WebSocket.OPCode OPcode;

                    // The length of the data in the buffer; control frames must have a payload ≤125 bytes and cannot be fragmented.
                    public int buffer_bytes;

                    // Buffer for storing control frame payload data (max 125 bytes).
                    public readonly byte[] buffer = new byte[125];

                    // SHA-1 instance used to compute the hash for WebSocket handshake.
                    public readonly SHA1 sha = SHA1.Create();

                    // Computes the WebSocket handshake response, inserting it into the `dst` buffer starting at `pos`.
                    // Returns the final length of the handshake message written to `dst`.
                    public int put_UPGRAGE_WEBSOCKET_responce_into(byte[] src, byte[] dst, int pos, int max)
                    {
                        var len = 0;

                        // Extract 'Sec-WebSocket-Key' value from `src` and copy it byte by byte into `buffer`.
                        // Stop copying when a carriage return (`\r`) is found, which marks the end of the key.
                        for (int b; pos < max && (b = src[pos]) != '\r'; pos++, len++)
                            buffer[len] = (byte)b;

                        // Append WebSocket GUID to `buffer` after the key for SHA-1 hashing.
                        GUID.CopyTo(buffer, len);

                        // Compute SHA-1 hash of 'Sec-WebSocket-Key' + GUID, storing the result back in `buffer`.
                        sha.TryComputeHash(new ReadOnlySpan<byte>(buffer, 0, len + GUID.Length), buffer, out len);

                        // Copy the initial WebSocket HTTP response headers template into `dst`.
                        UPGRAGE_WEBSOCKET.CopyTo((Span<byte>)dst);

                        // Base64 encode the SHA-1 hash result and copy it to `dst` after the response headers.
                        len = base64(buffer, 0, len, dst, UPGRAGE_WEBSOCKET.Length);

                        // Append "\r\n\r\n" to signify the end of the HTTP headers in the handshake response.
                        rnrn.CopyTo(dst, len);

                        return len + rnrn.Length;
                    }

                    // Static GUID used for WebSocket handshake hashing.
                    static readonly byte[] GUID = Encoding.ASCII.GetBytes("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");

                    // Byte sequence for marking end of HTTP headers.
                    static readonly byte[] rnrn = Encoding.ASCII.GetBytes("\r\n\r\n");

                    // HTTP headers for WebSocket handshake response, with placeholders for dynamic values.
                    static readonly byte[] UPGRAGE_WEBSOCKET = Encoding.ASCII.GetBytes(
                                                                                       "HTTP/1.1 101 Switching Protocols\r\n" +
                                                                                       "Server: AdHoc\r\n" +
                                                                                       "Connection: Upgrade\r\n" +
                                                                                       "Upgrade: websocket\r\n" +
                                                                                       "Sec-WebSocket-Accept: "
                                                                                      );

                    // Encodes `src` bytes into Base64, starting from `off` to `end`, and writes the result to `dst` at `dst_pos`.
                    // Returns the new position in `dst` after writing.
                    private int base64(byte[] src, int off, int end, byte[] dst, int dst_pos)
                    {
                        for (var max = off + (end - off) / 3 * 3; off < max;)
                        {
                            // Read three bytes and convert to four 6-bit Base64 characters.
                            var bits = (src[off++] & 0xff) << 16 | (src[off++] & 0xff) << 8 | (src[off++] & 0xff);
                            dst[dst_pos++] = base64_[(bits >> 18) & 0x3f];
                            dst[dst_pos++] = base64_[(bits >> 12) & 0x3f];
                            dst[dst_pos++] = base64_[(bits >> 6) & 0x3f];
                            dst[dst_pos++] = base64_[bits & 0x3f];
                        }

                        // Handle padding for any remaining bytes.
                        if (off == end) return dst_pos;
                        var b = src[off++] & 0xff;
                        dst[dst_pos++] = base64_[b >> 2];

                        if (off == end)
                        {
                            dst[dst_pos++] = base64_[(b << 4) & 0x3f];
                            dst[dst_pos++] = (byte)'=';
                            dst[dst_pos++] = (byte)'=';
                        }
                        else
                        {
                            dst[dst_pos++] = base64_[(b << 4) & 0x3f | ((b = src[off] & 0xff) >> 4)];
                            dst[dst_pos++] = base64_[(b << 2) & 0x3f];
                            dst[dst_pos++] = (byte)'=';
                        }

                        return dst_pos;
                    }

                    // Base64 character encoding table.
                    static readonly byte[] base64_ = Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/");

                    // Prepares the control frame header and payload, writing it to `dst` starting at `dst_byte`.
                    // Returns this `ControlFrameData` instance for further reuse.
                    public ControlFrameData get_frame(byte[] dst, int dst_byte)
                    {
                        // Insert WebSocket FIN and OPCode flags for the frame header.
                        dst[dst_byte++] = (byte)((int)Network.WebSocket.Mask.FIN | (int)OPcode);

                        // Insert payload length (control frames max 125 bytes).
                        dst[dst_byte++] = (byte)buffer_bytes;

                        // If there is payload data, copy it into `dst`.
                        if (buffer_bytes > 0)
                            Array.Copy(buffer, 0, dst, dst_byte, buffer_bytes);

                        return this;
                    }

                    // Appends data from `src` into `buffer` starting at `buffer_bytes`.
                    // Adjusts `buffer_bytes` to reflect the new length.
                    internal void put_data(byte[] src, int start, int end)
                    {
                        var bytes = end - start;
                        Array.Copy(src, start, buffer, buffer_bytes, bytes);
                        buffer_bytes += bytes;
                    }
                }


                protected static readonly ThreadLocal<Pool<ControlFrameData>> frames = new(() => new Pool<ControlFrameData>(() => new ControlFrameData()));

                private static uint[] Sec_Websocket_Key_ = AdHoc.boyer_moore_pattern("Sec-Websocket-Key: ");

                public virtual void parsing_HTTP_header(byte[] bytes, int max)
                {
                    // If the WebSocket upgrade response already exists in transmit_buffer, skip parsing
                    if (0 < Count) return;

                    // Search for the 'Sec-WebSocket-Key' header in a case-insensitive manner
                    var pos = AdHoc.boyer_moore_ASCII_Case_insensitive(bytes, Sec_Websocket_Key_);
                    if (pos == -1) return; // Header not found, exit parsing

                    // Acquire a pooled helper object to store the 'Sec-WebSocket-Key' value temporarily
                    var pool = frames.Value!;
                    var helper = pool.get();


                    // Generate and append the WebSocket upgrade response using the captured key value.
                    SetBuffer(0, helper.put_UPGRAGE_WEBSOCKET_responce_into(bytes, Buffer!, pos, max));

                    // Return the helper object back to the pool for reuse.
                    pool.put(helper);
                }

                //manage receive_mate.SetBuffer
                protected override void receive(byte[] src, int src_bytes)
                {
                    for (int start = 0, index = 0; ;)
                        switch (state)
                        {
                            case Network.WebSocket.State.HANDSHAKE:

                                if (
                                    src[src_bytes - 4] == (byte)'\r' && // Check for complete HTTP header (ends with \r\n\r\n) in the src
                                    src[src_bytes - 3] == (byte)'\n' &&
                                    src[src_bytes - 2] == (byte)'\r' &&
                                    src[src_bytes - 1] == (byte)'\n')
                                {
                                    parsing_HTTP_header(src, src_bytes);

                                    if (Count == 0) //Sec-WebSocket-Key in the header not found
                                    {
                                        host.onFailure(this, new Exception("Sec-WebSocket-Key in the header not found, Unexpected handshake :" + Encoding.ASCII.GetString(src, 0, src_bytes)));
                                        Close_and_dispose();
                                        return;
                                    }

                                    state = Network.WebSocket.State.NEW_FRAME;
                                    Interlocked.Exchange(ref transmit_lock, ext!.SendAsync(this) ? //trigger transmitting
                                                                                1 :                //on complete - unlock itself
                                                                                0);                //unlocked for new data

                                    host.onEvent(this, (int)Network.WebSocket.Event.EXT_INT_CONNECT);
                                    return;
                                }


                                var n = src_bytes - 1;
                                // Move backwards from the end of the source buffer, looking for the last newline character.
                                // This ensures that we only process complete header lines, as any incomplete data will
                                // be excluded by setting 'n' to the last newline position.
                                for (; -1 < n && src[n] != '\n'; n--) ;
                                if (n == -1)
                                {
                                    // No newline character found, indicating an incomplete or invalid header
                                    // Trigger failure callback with an error message indicating possible causes:
                                    // buffer may be too small or received bytes are malformed, preventing a proper handshake.
                                    host.onFailure(this, new Exception("No \\r\\n found in the received header, likely due to insufficient buffer size or invalid data. Unexpected handshake: " + Encoding.ASCII.GetString(src, 0, src_bytes)));

                                    // Close the connection and release resources
                                    Close_and_dispose();
                                    return;
                                }

                                // Set the buffer's limit to 'n', so only fully received header lines are processed.
                                parsing_HTTP_header(src, n); // Parse the headers within the limited range.

                                // Copy any unprocessed data (tail section) back into the buffer start.
                                // This handles the case where the end of the buffer contains an incomplete line.
                                Array.Copy(src, n, src, 0, src_bytes -= n + 1);
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
                #endregion

                public class Client : TCP<SRC, DST>
                {
                    #region > WebSocket Client code
                    #endregion > Network.TCP.WebSocket.Client

                    // WebSocket instance for managing connection and data transmission.
                    private ClientWebSocket ws;

                    // Function to create a new WebSocket instance, allowing dependency injection for testing.
                    public Func<ClientWebSocket> newClientWebSocket = () => new ClientWebSocket();

                    // Synchronization lock for transmissions to prevent concurrent writes.
                    private volatile int transmit_lock = 1;

                    // Buffer size for data transfer.
                    public readonly int bufferSize;

                    // Cancellation token for managing connection lifecycle.
                    private readonly CancellationTokenSource cts = new CancellationTokenSource();

                    // Prevents multiple concurrent connection attempts.
                    private volatile bool isConnecting;

                    // Server URI to which the client is connected.
                    public Uri server { get; private set; }

                    // Constructor to initialize client with required parameters.
                    public Client(string name, Func<TCP<SRC, DST>, Channel> new_channel, int bufferSize, TimeSpan channels_idle_timeout)
                        : base(name, new_channel, bufferSize, channels_idle_timeout)
                    {
                        this.bufferSize = bufferSize;
                    }

                    // Initiates a connection to the specified server with a default timeout.
                    public void Connect(Uri server, Action<SRC> onConnected, Action<Exception> onConnectingFailure) => Connect(server, onConnected, onConnectingFailure, TimeSpan.FromSeconds(5));

                    // Asynchronously connects to the server, handling connection lifecycle and callbacks.
                    public async void Connect(Uri server, Action<SRC> onConnected, Action<Exception> onConnectingFailure, TimeSpan connectingTimout)
                    {
                        if (isConnecting || ws?.State == WebSocketState.Open)
                        {
                            onConnectingFailure(new InvalidOperationException("Connection already in progress or established"));
                            return;
                        }

                        isConnecting = true;
                        this.server = server;
                        transmit_lock = 1;
                        ws = newClientWebSocket();

                        // Transmission and reception buffers
                        var transmit_buffer = new byte[bufferSize];
                        var receive_buffer = new byte[bufferSize];

                        // Set WebSocket buffer options for data handling.
                        ws.Options.SetBuffer(bufferSize, bufferSize, receive_buffer);

                        // Asynchronous function for sending data from the transmitter.
                        async void transmitting(AdHoc.BytesSrc src)
                        {
                            // Only transmit if the lock is free.
                            if (Interlocked.Exchange(ref transmit_lock, 1) == 1)
                                return;

                            // Send buffered data until empty, then release the lock.
                            for (int len; 0 < (len = channels.transmitter!.Read(transmit_buffer, 0, transmit_buffer.Length));)
                                await ws.SendAsync(new ReadOnlyMemory<byte>(transmit_buffer, 0, len), WebSocketMessageType.Binary, true, cts.Token);

                            transmit_lock = 0;
                            channels.on_all_packs_sent?.Invoke(channels);
                        }

                        // Asynchronous function for receiving data from WebSocket.
                        async Task receiving()
                        {
                            while (!cts.Token.IsCancellationRequested && ws.State == WebSocketState.Open)
                                try
                                {
                                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(receive_buffer), cts.Token);

                                    // Close connection on receiving close frame.
                                    if (result.MessageType == WebSocketMessageType.Close)
                                    {
                                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", cts.Token);
                                        return;
                                    }

                                    // Write received data to the receiver buffer.
                                    channels.receiver!.Write(receive_buffer, 0, result.Count);
                                }
                                catch (OperationCanceledException) when (cts.Token.IsCancellationRequested) { }
                                catch (Exception)
                                {
                                    onEvent(channels, (int)Network.Channel.Event.EXT_INT_DISCONNECT);
                                    break;
                                }
                        }

                        // Gracefully closes WebSocket if connected.
                        void CloseWebSocket()
                        {
                            if (ws?.State == WebSocketState.Open)
                                try { ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).Wait(); }
                                catch (Exception)
                                { // Ignore errors during close operation.
                                }
                        }

                        // Sets cleanup actions for channel disposal.
                        channels.on_disposed = _ =>
                                               {
                                                   cts.Cancel();
                                                   CloseWebSocket();
                                               };

                        try
                        {
                            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                            connectCts.CancelAfter(connectingTimout);

                            // Connect to the WebSocket server asynchronously.
                            await ws.ConnectAsync(server, connectCts.Token);

                            // Invoke callback on successful connection.
                            onConnected(channels.transmitter!);
                            transmit_lock = 0;
                            channels.transmitter!.subscribeOnNewBytesToTransmitArrive(transmitting);

                            // Start receiving data in a separate task.
                            _ = Task.Run(receiving);
                        }
                        catch (Exception ex) { onConnectingFailure(ex); }

                        // Constructs a descriptive string representation of the client.
                        toString = new StringBuilder(50)
                                   .Append("Client ")
                                   .Append(name)
                                   .Append(" -> ")
                                   .Append(server)
                                   .ToString();
                    }

                    // String representation of the client, showing name and connected server.
                    private string toString;
                    public override string ToString() => toString;

                    // Disconnects the client asynchronously, releasing resources.
                    public async Task DisconnectAsync()
                    {
                        if (cts.IsCancellationRequested) return;

                        cts.Cancel();

                        if (ws?.State == WebSocketState.Open)
                            try
                            {
                                using var closeTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, closeTimeoutCts.Token);
                            }
                            catch (Exception)
                            { // Ignore exceptions during close.
                            }
                            finally
                            {
                                ws.Dispose();
                                ws = null;
                            }
                    }
                }
            }

            public class Server : TCP<SRC, DST>
            {
                #region> Server code
                #endregion> Network.TCP.Server

                public Server(string name,
                              Func<TCP<SRC, DST>, Channel> new_channel,
                              int bufferSize,
                              TimeSpan channels_idle_timeout,
                              int Backlog,
                              Func<IPEndPoint, Socket>? socketBuilder,
                              params IPEndPoint[] ips) : base(name, new_channel, bufferSize, channels_idle_timeout)
                {
                    maintenance_thread = new Thread(() =>
                                                    {
                                                        for (; ; )
                                                        {
                                                            maintenance_lock.EnterWriteLock(); // Lock before waiting or performing maintenance
                                                            try
                                                            {
                                                                StartMaintenance();
                                                                var waitTime = Maintenance(DateTime.UtcNow.Ticks); // Calculate next maintenance timeout
                                                                if (RestartMaintenance()) continue;
                                                                when.Wait((int)waitTime); // Wait for the calculated timeout for the next maintenance cycle.
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                onFailure(this, ex); // Handles any exceptions that occur during maintenance.
                                                            }
                                                            finally
                                                            {
                                                                maintenance_lock.ExitWriteLock(); // Ensure lock release
                                                            }
                                                        }
                                                    })
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

                                                                                       on_accept_args.AcceptSocket = null; //the socket must be cleared since the context object is being reused
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

                // Verifies that the current thread is the maintenance thread; otherwise, throws an exception.
                // Ensures maintenance tasks run exclusively on the maintenance_thread.
                protected void EnsureThisIsMaintenanceThread()
                {
                    if (Thread.CurrentThread != maintenance_thread) { throw new Exception("Maintenance must be executed exclusively from the maintenance_thread. Use Server.Maintenance()"); }
                }

                private readonly ReaderWriterLockSlim maintenance_lock = new ReaderWriterLockSlim();

                private readonly ManualResetEventSlim when = new ManualResetEventSlim(false);

                //0 - idle
                //1 - running
                //2 - if idle: run
                //    if running: re-run
                private volatile int maintenance_state = 0;

                // Sets the maintenance state to running (1).
                protected void StartMaintenance() { maintenance_state = 1; }

                // Resets the maintenance state if it was greater than idle (0) and indicates if a restart is needed.
                protected bool RestartMaintenance() { return 1 < Interlocked.Exchange(ref maintenance_state, 0); }

                // Returns true if maintenance is already running, and sets the state to 2 if idle.
                protected bool MaintenanceRunning() { return 0 < Interlocked.Exchange(ref maintenance_state, 2); }


                // Forces the maintenance thread to wake up and perform maintenance immediately,
                // regardless of the current schedule or timeout.
                public override void trigger_maintenance()
                {
                    if (MaintenanceRunning()) return; // If maintenance is already running, set state to re-run and skip.
                    maintenance_lock.EnterWriteLock(); // Acquire lock before signaling
                    try
                    {
                        when.Set(); // Wake up the maintenance thread
                    }
                    finally
                    {
                        maintenance_lock.ExitWriteLock(); // Release lock
                    }
                }

                // Calculates the next maintenance interval based on active channels.
                // Can be overridden to provide custom timing logic.
                protected virtual int Maintenance(long time)
                {
                    for (; ; )
                    {

                        var timeout = maintenance_duty_cycle;

                        for (var channel = channels; channel != null; channel = channel.next)
                            if (channel.is_active)
                            {
                                if (!channel.waiting_for_maintenance())
                                {
                                    var t = (long)channels_idle_timeout.TotalMilliseconds - (time - Math.Max(channel.receive_time, channel.transmit_time));

                                    if (500 < t)
                                    {
                                        if (t < timeout) timeout = t;
                                        continue;
                                    }

                                    channel.schedule_maintenance();//request. maybe already ready_for_maintenance
                                }
                                //channel waiting for maintenance

                                if (channel.ready_for_maintenance())
                                {
                                    channel.maintenance();
                                    channel.maintenance_completed();
                                }
                                else timeout = 0;//re-run request
                            }

                        if (0 < timeout) return (int)timeout;
                    }
                }

                // Minimum duration (in milliseconds) between maintenance operations.
                public long maintenance_duty_cycle = 5000;


                private string toString;
                public override string ToString() => toString;

                public void shutdown()
                {
                    tcp_listeners.ForEach(socket => socket.Close());
                    for (var channel = channels; channel != null; channel = channel.next)
                        if (channel.is_active)
                        {
                            channel.trusted = false;
                            channel.Close();
                        }
                }
            }

            public class Client : TCP<SRC, DST>
            {
                #region> Client code
                #endregion> Network.TCP.Client
                private readonly SocketAsyncEventArgs onConnecting = new();

                public Client(string name, Func<TCP<SRC, DST>, Channel> new_channel, int bufferSize, TimeSpan channels_idle_timeout) : base(name, new_channel, bufferSize, channels_idle_timeout) => onConnecting.Completed += (_, _) => OnConnected();

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