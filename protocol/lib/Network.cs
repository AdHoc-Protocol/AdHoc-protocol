//Copyright 2025 Chikirev Sirguy, Unirail Group
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.
//
//For inquiries, please contact: al8v5C6HU4UtqE9@gmail.com
//GitHub Repository: https://github.com/AdHoc-Protocol

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using org.unirail.collections;

namespace org.unirail
{
    ///<summary>
    ///Provides high-performance networking components.
    ///</summary>
    public interface Network
    {
        ///<summary>
        ///Provides an abstract base for high-performance TCP communication, supporting both client and server roles.
        ///It leverages the <see cref="SocketAsyncEventArgs"/> (SAEA) pattern for non-blocking, scalable network I/O.
        ///</summary>
        ///<remarks>
        ///This class implements the enhanced asynchronous socket pattern to achieve optimal performance by minimizing object allocations:
        ///<list type="number">
        ///<item><description>A pool of reusable <see cref="SocketAsyncEventArgs"/> objects (channels) is maintained to avoid repeated instantiation.</description></item>
        ///<item><description>Properties of a context object are set for an operation (e.g., buffer, callback).</description></item>
        ///<item><description>An asynchronous socket method (e.g., <code>AcceptAsync</code>, <code>ReceiveAsync</code>, <code>SendAsync</code>) is called.</description></item>
        ///<item><description>The method's return value indicates if the I/O operation completed synchronously or will complete asynchronously.</description></item>
        ///<item><description>A callback delegate is invoked upon asynchronous completion.</description></item>
        ///<item><description>Operation results are processed within the callback using the context object's properties.</description></item>
        ///</list>
        ///This approach, combined with buffer pooling via <see cref="ArrayPool{T}"/>, significantly reduces memory allocations and garbage collection overhead, making it ideal for high-throughput applications.
        ///</remarks>
        ///<seealso cref="SocketAsyncEventArgs"/>
        ///<seealso href="https://docs.microsoft.com/en-us/dotnet/framework/network-programming/socket-performance-enhancements-in-version-3-5">Socket Performance Enhancements in .NET 3.5</seealso>
        public abstract class TCP
        {
            #region> TCP code
            #endregion> Network.TCP

            ///<summary>
            ///Gets the head of a singly linked list of communication channels. Each channel represents a unique connection
            ///and is linked via its <see cref="ExternalChannel.next"/> property.
            ///</summary>
            public readonly ExternalChannel channels;

            ///<summary>
            ///An action that configures buffers for a <see cref="SocketAsyncEventArgs"/> instance.
            ///It uses a shared <see cref="ArrayPool{T}"/> to rent buffers, minimizing memory allocations.
            ///</summary>
            private readonly Action<SocketAsyncEventArgs> buffers;

            ///<summary>
            ///A constant value used in <see cref="ExternalChannel.receive_time"/> to mark a channel as free and available for reuse.
            ///</summary>
            protected const long CHANNEL_FREE = -1;

            ///<summary>
            ///Gets the factory function used to create new <see cref="ExternalChannel"/> instances for new connections.
            ///</summary>
            public readonly Func<TCP, ExternalChannel> NewChannel;

            ///<summary>
            ///Gets or sets the name of this TCP instance, used for logging and identification.
            ///</summary>
            public string name;

            ///<summary>
            ///Initializes a new instance of the <see cref="TCP"/> class.
            ///</summary>
            ///<param name="name">A descriptive name for the TCP instance (e.g., "WebServer" or "ApiClient").</param>
            ///<param name="newChannel">A factory function that creates channel objects for new connections.</param>
            ///<param name="onFailure">A callback for handling exceptions and failures during network operations.</param>
            ///<param name="bufferSize">The size of the send and receive buffers for each channel.</param>
            public TCP(string name, Func<TCP, ExternalChannel> newChannel, Action<object, Exception> onFailure, int bufferSize = 1024)
            {
                this.name = name;
                buffers = dst => dst.SetBuffer(ArrayPool<byte>.Shared.Rent(bufferSize), 0, bufferSize);
                channels = (this.NewChannel = newChannel)(this);
                this.onFailure = onFailure;
            }

            ///<summary>
            ///Initializes a new instance of the <see cref="TCP"/> class with a default failure handler that prints to the console.
            ///</summary>
            ///<param name="name">A descriptive name for the TCP instance (e.g., "WebServer" or "ApiClient").</param>
            ///<param name="newChannel">A factory function that creates channel objects for new connections.</param>
            ///<param name="bufferSize">The size of the send and receive buffers for each channel.</param>
            public TCP(string name, Func<TCP, ExternalChannel> newChannel, int bufferSize = 1024) : this(name, newChannel, onFailurePrintConsole, bufferSize) { }

            ///<summary>
            ///Allocates a channel for a new connection.
            ///It reuses a free channel from the pool if one exists; otherwise, it creates and adds a new channel to the end of the list.
            ///</summary>
            ///<returns>An available <see cref="ExternalChannel"/> ready for a new connection.</returns>
            protected ExternalChannel allocate()
            {
                var ch = channels;
                for (; !ch.IsActivateDeactivated(); ch = ch.next)
                    if (ch.next == null)
                    {
                        var ret = NewChannel(this);
                        ret.receive_time = ret.transmit_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        //Atomically add the new channel to the end of the list.
                        while (Interlocked.CompareExchange(ref ch.next, ret, null) != null)
                            ch = ch.next;
                        return ret;
                    }

                ch.transmit_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return ch;
            }

            ///<summary>
            ///Synchronously triggers the maintenance process. The base implementation is empty and should be overridden in derived classes.
            ///</summary>
            public virtual void TriggerMaintenance() { }

            ///<summary>
            ///Asynchronously triggers the maintenance process. The base implementation is empty and should be overridden in derived classes.
            ///</summary>
            ///<returns>A task that represents the asynchronous trigger operation.</returns>
            public virtual async Task TriggerMaintenanceAsync() { }

            ///<summary>
            ///A sample event handler that logs all defined network events to the console. Suitable for debugging.
            ///</summary>
            public static readonly Action<AdHoc.Channel.External, int> onEventPrintConsole =
                (channel, eventId) =>
            {
#if DEBUG
                //Console.WriteLine("debugging stack of onEvent");
                //Console.WriteLine(new StackTrace().ToString());
#endif
                var parts = channel?.ToString()?.Split(':') ?? ["", ""];
                var source = parts[0].Trim();
                var peer = parts[1].Trim();

                Console.WriteLine(eventId switch
                {
                    (int)ExternalChannel.Event.REMOTE_CONNECT => $"{source}: Accepted connection from {peer}",
                    (int)ExternalChannel.Event.THIS_CONNECT => $"{source}: Connected to {peer}",
                    (int)ExternalChannel.Event.REMOTE_CLOSE_GRACEFUL => $"{source}: Remote peer gracefully closed connection with {peer}",
                    (int)ExternalChannel.Event.THIS_CLOSE_GRACEFUL => $"{source}: Gracefully closed connection to {peer}",
                    (int)ExternalChannel.Event.REMOTE_CLOSE_ABRUPTLY => $"{source}: Connection abruptly closed by remote peer or network failure with {peer}",
                    (int)ExternalChannel.Event.THIS_CLOSE_ABRUPTLY => $"{source}: Abruptly closed connection to {peer}",
                    (int)ExternalChannel.Event.RECEIVE_TIMEOUT => $"{source}: Timeout while receiving from {peer}",
                    (int)ExternalChannel.Event.TRANSMIT_TIMEOUT => $"{source}: Timeout while transmitting to {peer}",
                    (int)ExternalChannel.Event.WEBSOCKET_THIS_CONNECT => $"{source}: WebSocket Connected to {peer}",
                    (int)ExternalChannel.Event.WEBSOCKET_REMOTE_CONNECTED => $"{source}: WebSocket connection established from {peer}",
                    (int)ExternalChannel.Event.WEBSOCKET_HANDSHAKE_FAILURE => $"{source}: WebSocket handshake failed with {peer}. Invalid upgrade request.",
                    (int)ExternalChannel.Event.WEBSOCKET_PROTOCOL_ERROR => $"{source}: WebSocket protocol error from {peer}. Terminating connection.",
                    (int)ExternalChannel.Event.WEBSOCKET_PING => $"{source}: PING received from {peer}",
                    (int)ExternalChannel.Event.WEBSOCKET_PONG => $"{source}: PONG received from {peer}",
                    (int)ExternalChannel.Event.WEBSOCKET_EMPTY_FRAME => $"{source}: Received an empty data frame from {peer}",
                    (int)ExternalChannel.Event.WEBSOCKET_REMOTE_CLOSE_GRACEFUL => $"{source}: WebSocket peer gracefully closed connection with {peer}",
                    (int)ExternalChannel.Event.WEBSOCKET_THIS_CLOSE_GRACEFUL => $"{source}: Gracefully closed WebSocket connection to {peer}",
                    (int)ExternalChannel.Event.WEBSOCKET_REMOTE_CLOSE_ABRUPTLY => $"{source}: WebSocket connection abruptly closed by remote peer or network failure with {peer}",
                    (int)ExternalChannel.Event.WEBSOCKET_THIS_CLOSE_ABRUPTLY => $"{source}: Abruptly closed WebSocket connection to {peer}",
                    (int)ExternalChannel.Event.WEBSOCKET_RECEIVE_TIMEOUT => $"{source}: WebSocket timeout while receiving from {peer}",
                    (int)ExternalChannel.Event.WEBSOCKET_TRANSMIT_TIMEOUT => $"{source}: WebSocket timeout while transmitting to {peer}",
                    _ => $"{source}: Unknown event {eventId} on connection with {peer}"
                });
            };

            ///<summary>
            ///The callback delegate invoked to report exceptions and failures.
            ///</summary>
            private readonly Action<object, Exception> onFailure;

            ///<summary>
            ///A sample failure handler that logs exception details to the console. Suitable for debugging.
            ///</summary>
            public static readonly Action<object, Exception> onFailurePrintConsole = (src, t) =>
            {
                Console.WriteLine($"onFailure {src}");
#if DEBUG
                Console.WriteLine(new Exception("onFailure").StackTrace);
#endif
                Console.WriteLine(t);
            };

            ///<summary>
            ///Represents a single communication channel over a TCP connection.
            ///This class extends <see cref="SocketAsyncEventArgs"/> to manage the state and buffers for asynchronous I/O operations on a dedicated socket.
            ///</summary>
            public class ExternalChannel : SocketAsyncEventArgs, AdHoc.Channel.External
            {
                #region> ExternalChannel code
                #endregion> Network.TCP.ExternalChannel

                ///<summary>
                ///Returns a string representation of the channel, including the host name and endpoint details.
                ///</summary>
                ///<returns>A string describing the channel's state and connection endpoints.</returns>
                public override string ToString() => IsActive ? $"{host.name} : {ext!.LocalEndPoint} to {ext!.RemoteEndPoint}" : $"{host.name} : closed";

                ///<summary>
                ///Gets the underlying <see cref="Socket"/> for this channel's connection. It is null if the channel is not connected.
                ///</summary>
                public Socket? ext;

                ///<summary>
                ///Gets or sets the internal channel that provides data sources, destinations, and event handling logic.
                ///This property must be set via the <see cref="Init"/> method before communication can begin.
                ///</summary>
                public AdHoc.Channel.Internal Internal { get; set; }

                ///<summary>
                ///Initializes the external channel with its internal counterpart, linking it to the application logic.
                ///</summary>
                ///<param name="_internal">The internal channel implementation.</param>
                public void Init(AdHoc.Channel.Internal _internal) { Internal = _internal; }

                ///<summary>
                ///The timestamp (in Unix milliseconds) of the last successful data reception.
                ///A value of <see cref="TCP.CHANNEL_FREE"/> indicates that this channel is inactive and available for reuse.
                ///</summary>
                public long receive_time = CHANNEL_FREE;

                ///<summary>
                ///Attempts to atomically activate a free channel by setting its <see cref="receive_time"/> from <see cref="CHANNEL_FREE"/> to the current timestamp.
                ///</summary>
                ///<returns><c>false</c> if the channel was successfully activated from a free state; <c>true</c> if it was already active.</returns>
                public bool IsActivateDeactivated() => Interlocked.CompareExchange(ref receive_time, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), CHANNEL_FREE) != CHANNEL_FREE;

                ///<summary>
                ///Atomically deactivates an active channel by setting its <see cref="receive_time"/> to <see cref="CHANNEL_FREE"/>.
                ///</summary>
                ///<returns><c>true</c> if the channel was active and is now deactivated; <c>false</c> if it was already free.</returns>
                protected bool IsDeactivateActivated() => Interlocked.Exchange(ref receive_time, CHANNEL_FREE) != CHANNEL_FREE;

                ///<summary>
                ///The timestamp (in Unix milliseconds) of the last successful data transmission.
                ///</summary>
                public long transmit_time = CHANNEL_FREE;

                ///<summary>
                ///Gets a value indicating whether the channel is currently active (i.e., not marked as free).
                ///</summary>
                public bool IsActive => receive_time != CHANNEL_FREE;

                ///<summary>
                ///Gets a reference to the parent <see cref="TCP"/> instance that manages this channel.
                ///</summary>
                public readonly TCP host;

                ///<summary>
                ///Initializes a new instance of the <see cref="ExternalChannel"/> class.
                ///</summary>
                ///<param name="host">The parent <see cref="TCP"/> instance that owns this channel.</param>
                public ExternalChannel(TCP host)
                {
                    //This SAEA instance is primarily for sends, while ReceiveMate is for receives.
                    //Both can trigger the same completion handler because SAEA operations are distinct.
                    ReceiveMate.Completed += OnCompleted;
                    DisconnectReuseSocket = true;
                    onNewBytesToTransmitArrive =
                        _ =>
                    {
                        //If the socket exists and we can successfully acquire the transmitter lock, start sending.
                        if (ext != null && IsActivateDeactivatedTransmitter())
                            transmit();
                    };
                    this.host = host;
                }
                #region close
                ///<summary>
                ///Initiates a graceful shutdown of the connection.
                ///This method shuts down the send channel, signaling the remote peer that no more data will be sent.
                ///The channel remains open to receive data until the peer also closes its end, which is detected by a zero-byte read.
                ///</summary>
                public virtual void Close()
                {
                    if (!IsActive)
                        return;
                    isClosingGracefully = true;
                    try
                    {
                        ext?.Shutdown(SocketShutdown.Send);
                    }
                    catch (Exception e)
                    {
                        host.onFailure(this, e);
                        Abort();
                    }
                }

                ///<summary>
                ///A flag indicating whether a graceful shutdown has been initiated by this host.
                ///</summary>
                private bool isClosingGracefully;

                ///<summary>
                ///Forcibly closes the socket connection by shutting down both send and receive streams and closing the socket.
                ///This is an internal helper for abrupt shutdowns that suppresses exceptions.
                ///</summary>
                protected void CloseInt()
                {
                    isClosingGracefully = false; //Reset the flag, as this is an abrupt closure.
                    try
                    {
                        ext?.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception)
                    { /* Ignored */
                    }

                    try
                    {
                        ext?.Close();
                    }
                    catch (Exception)
                    { /* Ignored */
                    }
                }

                ///<summary>
                ///Abruptly terminates the connection and releases associated resources.
                ///Unlike a graceful close, this method does not wait for pending data and immediately closes the socket.
                ///</summary>
                public virtual void Abort()
                {
                    isClosingGracefully = false; //Ensure graceful close flag is cleared.

                    if (ext != null)
                    {
                        try
                        {
                            ext.Close();
                        }
                        catch (Exception)
                        { /* Ignored */
                        }

                        ext = null;
                    }

                    if (!IsDeactivateActivated())
                        return;
                    ActivateTransmitter();
                    CloseInt();
                    CloseAndDispose();
                }

                ///<summary>
                ///Finalizes the closure of the connection, releasing all resources and marking the channel as free for reuse.
                ///This includes returning buffers to the pool and clearing endpoint information.
                ///</summary>
                public void CloseAndDispose()
                {
                    if (!IsDeactivateActivated())
                        return;
                    CloseInt();
                    Internal?.BytesSrc?.Close();
                    Internal?.BytesDst?.Close();
                    RemoteEndPoint = null;
                    if (Buffer != null)
                    {
                        ArrayPool<byte>.Shared.Return(Buffer);
                        SetBuffer(null, 0, 0);
                    }

                    if (ReceiveMate.Buffer != null)
                    {
                        ArrayPool<byte>.Shared.Return(ReceiveMate.Buffer);
                        ReceiveMate.SetBuffer(null, 0, 0);
                    }
                }
                #endregion
                #region Transmitting
                ///<summary>
                ///Handles the completion of asynchronous socket operations such as Connect, Disconnect, and Send.
                ///</summary>
                ///<param name="_transmit">The <see cref="SocketAsyncEventArgs"/> instance for the completed operation.</param>
                protected override void OnCompleted(SocketAsyncEventArgs _transmit)
                {
                    //Wait if maintenance is in progress to prevent race conditions.
                    while (locked_for_maintenance())
                        Thread.SpinWait(5);
                    try
                    {
                        int eventType;
                        switch (_transmit.SocketError)
                        {
                            case SocketError.Success:
                                switch (_transmit.LastOperation)
                                {
                                    case SocketAsyncOperation.Connect:
                                        //This path is now only used by the legacy Client.ConnectAsync.
                                        //The newer version in this file no longer relies on this.
                                        transmitterConnected(ConnectSocket);
                                        return;
                                    case SocketAsyncOperation.Disconnect:
                                        Internal.OnExternalEvent(this, (int)Event.THIS_CLOSE_ABRUPTLY);
                                        CloseAndDispose();
                                        return;
                                    case SocketAsyncOperation.Send:
                                        ActivateTransmitter();
                                        transmit(); //Continue sending if more data is available.
                                        return;
                                }

                                return;
                            case SocketError.TimedOut:
                                eventType = (int)Event.TRANSMIT_TIMEOUT;
                                break;
                            case SocketError.ConnectionReset:
                            case SocketError.ConnectionAborted:
                            case SocketError.NetworkReset:
                                eventType = (int)Event.REMOTE_CLOSE_ABRUPTLY;
                                break;
                            default:
                                host.onFailure(this, new SocketException((int)_transmit.SocketError));
                                eventType = (int)Event.REMOTE_CLOSE_ABRUPTLY;
                                break;
                        }

                        Internal.OnExternalEvent(this, eventType);
                        CloseAndDispose();
                    }
                    finally
                    {
                        pending_send_receive_completed();
                    }
                }

                ///<summary>
                ///Configures the channel for transmission after a successful connection, setting up buffers and starting I/O loops.
                ///</summary>
                ///<param name="ext">The newly connected socket.</param>
                internal void transmitterConnected(Socket? ext)
                {
                    isClosingGracefully = false;

                    this.ext = ext;
                    transmit_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    Internal.OnExternalEvent(this, (int)Event.THIS_CONNECT);
                    if (Buffer == null)
                        host.buffers(this);
                    else
                        SetBuffer(0, Buffer.Length);
                    IdleTransmitter();
                    Internal.BytesSrc?.subscribeOnNewBytesToTransmitArrive(onNewBytesToTransmitArrive);
                    if (Internal.BytesDst == null)
                        return;
                    if (ReceiveMate.Buffer == null)
                        host.buffers(ReceiveMate);
                    //Start the receive loop. If ReceiveAsync completes synchronously, call receive() immediately.
                    if (!ext!.ReceiveAsync(ReceiveMate))
                        receive();
                }

                ///<summary>
                ///The callback action invoked when new bytes are available in the transmitter.
                ///</summary>
                protected readonly Action<AdHoc.BytesSrc> onNewBytesToTransmitArrive;

                ///<summary>
                ///Gets or sets the send timeout in milliseconds.
                ///A negative value in the getter indicates that a graceful close is in progress.
                ///Setting a negative value flags the connection for a graceful close (initiated once the send buffer is drained) and sets the timeout to its absolute value.
                ///</summary>
                public virtual int TransmitTimeout
                {
                    get => isClosingGracefully ? -ext!.SendTimeout : //Return as negative to signal closing state
                               ext!.SendTimeout;
                    set => ext!.SendTimeout = value < 0 && (isClosingGracefully = true) ? -value : value;
                }

                ///<summary>
                ///Manages the asynchronous sending of data. It repeatedly reads from the source transmitter
                ///and issues non-blocking send operations until the transmitter is empty.
                ///</summary>
                private void transmit()
                {
                    do
                        //Continuously read from the source and send data.
                        while (transmit(Buffer!))
                            //If SendAsync completes asynchronously, the OnCompleted callback will resume the process.
                            if (ext!.SendAsync(this))
                                return;
                    //This outer loop handles a race condition where new data arrives while the transmitter lock is being released.
                    while (IsDeactivateActiveTransmitter());
                    //At this point, the transmitter is idle, and all pending data is in the socket's send buffer.
                    OnTransmitterDrained();
                }

                ///<summary>
                ///A hook method invoked when the internal transmitter has been fully drained of pending data.
                ///</summary>
                ///<remarks>
                ///This method is called by the <code>transmit()</code> loop after it has sent all available data from the
                ///transmitter. It serves as an extension point for derived classes to implement
                ///protocol-specific final actions.
                ///<para>
                ///For example, the <see cref="WebSocket"/> class overrides this method to send a CLOSE frame if a
                ///graceful shutdown was previously requested.
                ///</para>
                ///The default implementation handles the raw TCP graceful close sequence by calling
                ///<see cref="Close"/> if a graceful close was initiated.
                ///</remarks>
                protected virtual void OnTransmitterDrained()
                {
                    if (isClosingGracefully)
                        Close();
                }

                ///<summary>
                ///Loads a chunk of data from the internal transmitter into the specified buffer for sending.
                ///</summary>
                ///<param name="dst">The destination buffer for the data.</param>
                ///<returns><c>true</c> if data was loaded into the buffer; <c>false</c> if no data was available.</returns>
                protected virtual bool transmit(byte[] dst)
                {
                    if (Internal.BytesSrc == null)
                        return false;
                    var bytes = Internal.BytesSrc.Read(dst, 0, dst.Length);
                    if (bytes < 1)
                        return false;
                    SetBuffer(0, bytes);
                    return true;
                }

                ///<summary>
                ///A lock variable to manage the transmitter's state (0 = idle, >0 = active).
                ///</summary>
                protected volatile int TransmitLock = 1;

                ///<summary>
                ///Atomically deactivates the transmitter by setting its lock to 0.
                ///</summary>
                ///<returns><c>true</c> if the transmitter was active before this call; otherwise, <c>false</c>.</returns>
                protected bool IsDeactivateActiveTransmitter() => Interlocked.Exchange(ref TransmitLock, 0) != 0;

                ///<summary>
                ///Sets the transmitter state to idle (lock = 0), allowing a new send operation to begin.
                ///</summary>
                protected void IdleTransmitter() => TransmitLock = 0;

                ///<summary>
                ///Atomically activates the transmitter if it is currently idle.
                ///</summary>
                ///<returns><c>true</c> if the transmitter was successfully activated from an idle state; otherwise, <c>false</c>.</returns>
                protected bool IsActivateDeactivatedTransmitter() => Interlocked.Increment(ref TransmitLock) == 1;

                ///<summary>
                ///Increments the transmitter's active operation count, keeping it in an active state.
                ///</summary>
                protected void ActivateTransmitter() => Interlocked.Increment(ref TransmitLock);
                #endregion
                #region Receiving
                ///<summary>
                ///A dedicated <see cref="SocketAsyncEventArgs"/> instance used exclusively for receive operations
                ///to avoid conflicts with send operations on the primary instance.
                ///</summary>
                internal readonly SocketAsyncEventArgs ReceiveMate = new();

                ///<summary>
                ///Handles the completion of asynchronous receive operations.
                ///</summary>
                ///<param name="src">The object that raised the event.</param>
                ///<param name="_receive">The <see cref="SocketAsyncEventArgs"/> instance for the completed operation.</param>
                protected void OnCompleted(object? src, SocketAsyncEventArgs _receive)
                {
                    //Wait if maintenance is in progress to prevent race conditions.
                    while (locked_for_maintenance())
                        Thread.SpinWait(5);
                    receive_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    try
                    {
                        int eventType;
                        switch (_receive.SocketError)
                        {
                            case SocketError.Success:
                                if (_receive.BytesTransferred == 0)
                                {
                                    //A zero-byte read signifies that the remote peer has gracefully closed its end of the connection.
                                    //The event type depends on whether this host initiated the close sequence.
                                    var @event = isClosingGracefully ? (int)Event.THIS_CLOSE_GRACEFUL : (int)Event.REMOTE_CLOSE_GRACEFUL;
                                    Internal.OnExternalEvent(this, @event);
                                    CloseAndDispose();
                                    return;
                                }

                                if (_receive.LastOperation != SocketAsyncOperation.Receive)
                                    return;

                                receive(); //Process the received data and start the next receive operation.
                                return;
                            case SocketError.TimedOut:
                                eventType = (int)Event.RECEIVE_TIMEOUT;
                                break;
                            case SocketError.ConnectionReset:
                            case SocketError.ConnectionAborted:
                            case SocketError.NetworkReset:
                                eventType = (int)Event.REMOTE_CLOSE_ABRUPTLY;
                                break;
                            default:
                                host.onFailure(this, new SocketException((int)_receive.SocketError));
                                eventType = (int)Event.REMOTE_CLOSE_ABRUPTLY;
                                break;
                        }

                        Internal.OnExternalEvent(this, eventType);
                        CloseAndDispose();
                    }
                    finally
                    {
                        pending_send_receive_completed();
                    }
                }

                ///<summary>
                ///Configures the channel for receiving data from a new external connection, setting up buffers and starting I/O loops.
                ///</summary>
                ///<param name="ext">The newly accepted socket.</param>
                internal void receiver_connected(Socket ext)
                {
                    this.ext = ext;
                    receive_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    Internal.OnExternalEvent(this, (int)Event.REMOTE_CONNECT);
                    if (!ext.Connected)
                        return;
                    if (ReceiveMate.Buffer == null)
                        host.buffers(ReceiveMate);
                    if (Internal.BytesSrc != null)
                    {
                        IdleTransmitter();
                        if (Buffer == null)
                            host.buffers(this);
                        Internal.BytesSrc.subscribeOnNewBytesToTransmitArrive(onNewBytesToTransmitArrive);
                    }

                    //Start the receive loop. If ReceiveAsync completes synchronously, call receive() immediately.
                    if (!this.ext!.ReceiveAsync(ReceiveMate))
                        receive();
                }

                ///<summary>
                ///Stops receiving data on this channel by shutting down the receive direction of the socket.
                ///</summary>
                public void StopReceiving() { ext?.Shutdown(SocketShutdown.Receive); }

                ///<summary>
                ///Gets or sets the receive timeout in milliseconds.
                ///A negative value in the getter indicates that a graceful close is in progress.
                ///Setting a negative value initiates a graceful close via <see cref="Close"/> and sets the timeout to its absolute value.
                ///</summary>
                public virtual int ReceiveTimeout
                {
                    get => isClosingGracefully ? -ext!.ReceiveTimeout : ext!.ReceiveTimeout;
                    set
                    {
                        if (value < 0)
                        {
                            if (!isClosingGracefully)
                                Close(); //Initiate graceful close as a side effect.
                            ext!.ReceiveTimeout = -value;
                        }
                        else
                            ext!.ReceiveTimeout = value;
                    }
                }

                ///<summary>
                ///Processes received data and immediately initiates the next asynchronous receive operation.
                ///</summary>
                private void receive()
                {
                    try
                    {
                        do
                        {
                            if (ReceiveMate.BytesTransferred == 0)
                            {
                                CloseInt(); //Should have been handled by OnCompleted, but this is a safeguard.
                                return;
                            }

                            receive(ReceiveMate.Buffer!, ReceiveMate.Offset + ReceiveMate.BytesTransferred);
                        }
                        //Loop as long as ReceiveAsync completes synchronously.
                        while (!ext!.ReceiveAsync(ReceiveMate));
                    }
                    catch (Exception e)
                    {
                        host.onFailure(this, e);
                    }
                }

                ///<summary>
                ///Delivers the received data from the buffer to the internal receiver.
                ///</summary>
                ///<param name="src">The source buffer containing the received data.</param>
                ///<param name="src_bytes">The number of bytes received in the buffer.</param>
                protected virtual void receive(byte[] src, int src_bytes) => Internal.BytesDst?.Write(src, 0, src_bytes);
                #endregion

                ///<summary>
                ///A pointer to the next channel in the host's singly linked list of channels.
                ///</summary>
                public ExternalChannel? next;
                #region maintenance
                ///<summary>
                ///A lock variable used to synchronize channel maintenance with active send/receive operations. Its value represents the channel's I/O state:
                ///<list type="bullet">
                ///<item><term>&lt; 0</term><description>Maintenance is in progress or scheduled. The channel is locked for new I/O.</description></item>
                ///<item><term>== int.MinValue</term><description>The channel has no active I/O and is ready for maintenance processing.</description></item>
                ///<item><term>&gt; 0</term><description>The count of active, pending I/O operations.</description></item>
                ///<item><term>== 0</term><description>The channel is idle (no pending I/O).</description></item>
                ///</list>
                ///</summary>
                private volatile int maintenance_lock = 0;

                ///<summary>
                ///When overridden in a derived class, performs channel-specific maintenance tasks, such as handling timeouts.
                ///This method requires exclusive access to the channel, which is ensured by the maintenance lock.
                ///</summary>
                ///<returns>The time in milliseconds until the next maintenance task is due, or <see cref="uint.MaxValue"/> if none.</returns>
                protected internal virtual uint maintenance() { return uint.MaxValue; }

                ///<summary>
                ///Checks if maintenance is currently scheduled or in progress.
                ///</summary>
                ///<returns><c>true</c> if the maintenance lock is active; otherwise, <c>false</c>.</returns>
                protected internal bool waiting_for_maintenance() => maintenance_lock < 0;

                ///<summary>
                ///Checks if the channel is ready for maintenance (i.e., no active I/O operations).
                ///</summary>
                ///<returns><c>true</c> if ready for maintenance; otherwise, <c>false</c>.</returns>
                protected internal bool ready_for_maintenance() => maintenance_lock == int.MinValue;

                ///<summary>
                ///Resets the maintenance lock, allowing normal I/O operations to resume after maintenance is complete.
                ///</summary>
                protected internal void maintenance_completed() => Interlocked.Exchange(ref maintenance_lock, 0);

                ///<summary>
                ///Decrements the active I/O operation counter upon completion of a send or receive operation.
                ///</summary>
                protected void pending_send_receive_completed() => Interlocked.Decrement(ref maintenance_lock);

                ///<summary>
                ///Attempts to acquire a lock for a new I/O operation. It fails if maintenance is in progress.
                ///</summary>
                ///<returns><c>true</c> if the channel is locked for maintenance (preventing I/O); <c>false</c> if the I/O operation can proceed.</returns>
                protected bool locked_for_maintenance()
                {
                    int t;
                    do
                        t = maintenance_lock;
                    while (Interlocked.CompareExchange(ref maintenance_lock, t < 0 ? t : t + 1, t) != t);
                    return t < 0;
                }

                ///<summary>
                ///Schedules maintenance by setting the maintenance lock, which prevents new I/O operations from starting.
                ///</summary>
                protected internal void schedule_maintenance()
                {
                    int t;
                    do
                        t = maintenance_lock;
                    while (Interlocked.CompareExchange(ref maintenance_lock, t < 0 ? t : int.MinValue + t, t) != t);
                }
                #endregion

                ///<summary>
                ///Marker attribute to annotate parameters that represent an event code.
                ///</summary>
                [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
                public class EventAttribute : Attribute
                {
                    //No elements needed, as it's a marker attribute.
                }

                ///<summary>
                ///Defines a vocabulary of event types that signal state changes within an ExternalChannel.
                ///</summary>
                ///<remarks>
                ///<h3>Design Philosophy</h3>
                ///<p>
                ///Events are 32-bit integers constructed using a bitmask system. This design allows multiple
                ///independent properties of an event—such as its source, manner, and underlying
                ///action—to be encoded into a single, efficient value. This enables flexible and
                ///high-performance event handling through simple bitwise operations.
                ///</p>
                ///<p>
                ///The system is composed of two main parts:
                ///<ul>
                ///<li><b>Base Actions (Lower 16 bits):</b> These are the fundamental "nouns" of an
                ///    event, like <code>CONNECT</code> or <code>CLOSE</code>. They describe the core action that occurred.</li>
                ///<li><b>Flags (Higher 16 bits):</b> Defined in the <see cref="__Event.Mask"/> class, these are
                ///    "adjectives" that add context to the base action. They specify properties like
                ///    the event's initiator (e.g., <see cref="__Event.IsRemote(Event)"/>), its manner (e.g., <see cref="__Event.IsGraceful(Event)"/>),
                ///    or the protocol layer (e.g., <see cref="__Event.IsWebSocket(Event)"/>).</li>
                ///</ul>
                ///
                ///<h3>Usage Examples</h3>
                ///
                ///<h4>1. Checking for a specific composite event:</h4>
                ///<code>
                ///if (eventId == (int)Event.WEBSOCKET_REMOTE_CONNECTED) {
                ///    // Logic for when a new WebSocket client completes its handshake.
                ///}
                ///</code>
                ///
                ///<h4>2. Checking for a general category of event (e.g., any close event):</h4>
                ///<code>
                ///if (myEvent.IsClose()) {
                ///    // This will trigger for THIS_CLOSE_GRACEFUL, REMOTE_CLOSE_ABRUPTLY, etc.
                ///}
                ///</code>
                ///
                ///<h4>3. Checking for specific properties (e.g., any event initiated remotely and abruptly):</h4>
                ///<code>
                ///if (myEvent.IsRemote() &amp;&amp; myEvent.IsAbrupt()) {
                ///    // This could be a connection reset (RST) from the peer.
                ///    Console.WriteLine("Connection was terminated abruptly by the remote peer.");
                ///}
                ///</code>
                ///</remarks>
                [Flags]
                public enum Event
                {
                    //--- Composite TCP/General Events ---
                    REMOTE_CONNECT = __Event.Mask.REMOTE | __Event.Action.CONNECT,
                    THIS_CONNECT = __Event.Mask.THIS | __Event.Action.CONNECT,
                    REMOTE_CLOSE_GRACEFUL = __Event.Mask.REMOTE | __Event.Mask.GRACEFUL | __Event.Action.CLOSE,
                    THIS_CLOSE_GRACEFUL = __Event.Mask.THIS | __Event.Mask.GRACEFUL | __Event.Action.CLOSE,
                    REMOTE_CLOSE_ABRUPTLY = __Event.Mask.REMOTE | __Event.Mask.ABRUPT | __Event.Action.CLOSE,
                    THIS_CLOSE_ABRUPTLY = __Event.Mask.THIS | __Event.Mask.ABRUPT | __Event.Action.CLOSE,
                    TRANSMIT_TIMEOUT = __Event.Mask.TRANSMIT | __Event.Action.TIMEOUT,
                    RECEIVE_TIMEOUT = __Event.Mask.RECEIVE | __Event.Action.TIMEOUT,

                    //--- Composite WebSocket-Specific Events ---
                    WEBSOCKET_THIS_CONNECT = __Event.Mask.WEBSOCKET | __Event.Mask.THIS | __Event.Action.CONNECT,
                    WEBSOCKET_REMOTE_CONNECTED = __Event.Mask.WEBSOCKET | __Event.Mask.REMOTE | __Event.Action.CONNECT,
                    WEBSOCKET_HANDSHAKE_FAILURE = __Event.Mask.WEBSOCKET | __Event.Mask.REMOTE | __Event.Mask.ABRUPT | __Event.Action.HANDSHAKE_FAILURE,
                    WEBSOCKET_PROTOCOL_ERROR = __Event.Mask.WEBSOCKET | __Event.Mask.REMOTE | __Event.Mask.ABRUPT | __Event.Action.PROTOCOL_ERROR,
                    WEBSOCKET_PING = __Event.Mask.WEBSOCKET | __Event.Mask.REMOTE | __Event.Action.PING,
                    WEBSOCKET_PONG = __Event.Mask.WEBSOCKET | __Event.Mask.REMOTE | __Event.Action.PONG,
                    WEBSOCKET_EMPTY_FRAME = __Event.Mask.WEBSOCKET | __Event.Mask.REMOTE | __Event.Action.EMPTY_FRAME,
                    WEBSOCKET_REMOTE_CLOSE_GRACEFUL = __Event.Mask.WEBSOCKET | __Event.Mask.REMOTE | __Event.Mask.GRACEFUL | __Event.Action.CLOSE,
                    WEBSOCKET_THIS_CLOSE_GRACEFUL = __Event.Mask.WEBSOCKET | __Event.Mask.THIS | __Event.Mask.GRACEFUL | __Event.Action.CLOSE,
                    WEBSOCKET_REMOTE_CLOSE_ABRUPTLY = __Event.Mask.WEBSOCKET | __Event.Mask.REMOTE | __Event.Mask.ABRUPT | __Event.Action.CLOSE,
                    WEBSOCKET_THIS_CLOSE_ABRUPTLY = __Event.Mask.WEBSOCKET | __Event.Mask.THIS | __Event.Mask.ABRUPT | __Event.Action.CLOSE,

                    WEBSOCKET_TRANSMIT_TIMEOUT = __Event.Mask.WEBSOCKET | __Event.Mask.TRANSMIT | __Event.Action.TIMEOUT,
                    WEBSOCKET_RECEIVE_TIMEOUT = __Event.Mask.WEBSOCKET | __Event.Mask.RECEIVE | __Event.Action.TIMEOUT
                }
            }

            ///<summary>
            ///Extends <see cref="ExternalChannel"/> to implement the WebSocket protocol,
            ///handling the initial HTTP upgrade handshake and subsequent frame-based communication.
            ///</summary>
            public class WebSocket : ExternalChannel
            {
                #region> WebSocket code
                #endregion> Network.TCP.WebSocket

                ///<summary>
                ///Initializes a new instance of the <see cref="WebSocket"/> class.
                ///</summary>
                ///<param name="host">The parent <see cref="TCP"/> instance that owns this channel.</param>
                public WebSocket(TCP host) : base(host) { }

                ///<summary>Flag to initiate a graceful WebSocket close after the transmitter is drained.</summary>
                protected volatile bool _wsCloseGraceful = false; //WebSocket process CloseGraceful is different than raw Socket
                ///<summary>Flag to prevent redundant CLOSE frames from being sent during a graceful shutdown.</summary>
                protected volatile bool _wsClosingGraceful = false;

                ///<summary>
                ///Gets or sets the send timeout. Setting a negative value initiates a graceful WebSocket close.
                ///</summary>
                public override int TransmitTimeout
                {
                    get => base.TransmitTimeout;
                    set => base.TransmitTimeout = value < 0 && (_wsCloseGraceful = true) ? -value : value;
                }

                ///<summary>
                ///When the transmitter is drained, sends a WebSocket CLOSE frame if a graceful close was requested.
                ///</summary>
                protected override void OnTransmitterDrained()
                {
                    if (_wsCloseGraceful)
                        CloseGraceful(1000, "Normal Closure");
                }

                ///<summary>
                ///Gets or sets the receive timeout. Setting a negative value initiates a graceful WebSocket close.
                ///</summary>
                public override int ReceiveTimeout
                {
                    get => base.ReceiveTimeout;
                    set
                    {
                        if (value < 0)
                        {
                            CloseGraceful(1000, "Normal Closure"); //Initiate graceful close as a side effect.
                            base.ReceiveTimeout = -value;
                        }
                        else
                            base.ReceiveTimeout = value;
                    }
                }

                ///<summary>
                ///Initiates a graceful WebSocket close by sending a CLOSE frame.
                ///</summary>
                public override void Close() { CloseGraceful(1000, "AdHoc server closing"); }

                ///<summary>
                ///Sends a WebSocket CLOSE frame to gracefully terminate the connection.
                ///</summary>
                ///<param name="code">The WebSocket close status code (as per RFC 6455).</param>
                ///<param name="why">An optional, human-readable reason for the closure.
                ///The reason MUST be valid UTF-8 and will be truncated if its byte representation exceeds 123 bytes.</param>
                public void CloseGraceful(int code, string? why)
                {
                    if (_wsClosingGraceful)
                        return;
                    _wsClosingGraceful = true;
                    //1. Get a reusable frame object from the pool.
                    var frame = frames.Value!.get();

                    frame.OPcode = OPCode.CLOSE;
                    //2. Encode the 16-bit status code (Big Endian).
                    frame.buffer[0] = (byte)(code >> 8);
                    frame.buffer[1] = (byte)code;
                    frame.buffer_bytes = 2;

                    if (!string.IsNullOrEmpty(why))
                    {
                        var whyBytes = Encoding.UTF8.GetBytes(why);

                        //The status code uses 2 bytes, leaving 123 for the reason phrase.
                        var len = Math.Min(whyBytes.Length, 123);

                        //3. Copy the reason bytes into the buffer.
                        Array.Copy(whyBytes, 0, frame.buffer, 2, len);
                        frame.buffer_bytes += len;
                    }

                    //4. Atomically set the urgent frame, recycling any previously set one.
                    recycle_frame(Interlocked.Exchange(ref urgent_frame_data, frame));

                    //5. Trigger the transmitter to send this urgent frame immediately.
                    onNewBytesToTransmitArrive(null!);
                }

                ///<summary>
                ///Sends a WebSocket PING frame to the remote peer, often used for keep-alive checks.
                ///</summary>
                ///<param name="msg">An optional payload to include in the PING frame. Max length is 125 bytes.</param>
                public void ping(string? msg)
                {
                    var frame = frames.Value!.get();
                    frame.OPcode = OPCode.PING;
                    if (msg == null)
                        frame.buffer_bytes = 0;
                    else
                    {
                        //Note: This assumes ASCII payload. For arbitrary binary data, use a different encoding method.
                        for (int i = 0, max = msg.Length; i < max; i++)
                            frame.buffer[i] = (byte)msg[i];
                        frame.buffer_bytes = msg.Length;
                    }

                    recycle_frame(Interlocked.Exchange(ref urgent_frame_data, frame_data));
                    onNewBytesToTransmitArrive(null!);
                }

                ///<summary>
                ///Abruptly closes the WebSocket connection and resets the parsing state machine and all related resources.
                ///</summary>
                public override void Abort()
                {
                    if (ext == null)
                        return;
                    state = State.HANDSHAKE;
                    sent_closing_frame = false;
                    frame_bytes_left = BYTE = xor0 = xor1 = xor2 = xor3 = 0;
                    OPcode = OPCode.CONTINUATION;
                    if (frame_data != null)
                        recycle_frame(frame_data);
                    if (urgent_frame_data != null)
                        recycle_frame(urgent_frame_data);
                    base.Abort();
                }
                #region Transmitting
                ///<summary>
                ///Tracks whether a CLOSE frame has been sent, as part of the WebSocket closing handshake.
                ///</summary>
                private bool sent_closing_frame;

                ///<summary>
                ///Holds a single urgent control frame (e.g., CLOSE, PING) to be sent before any data frames.
                ///</summary>
                volatile ControlFrameData? urgent_frame_data;

                ///<summary>
                ///Overrides the base transmit logic to construct and send WebSocket frames. It prioritizes urgent
                ///control frames and then packages data from the transmitter into data frames.
                ///</summary>
                ///<param name="dst">The destination buffer for the composed WebSocket frame.</param>
                ///<returns><c>true</c> if a frame was prepared for sending; otherwise, <c>false</c>.</returns>
                protected override bool transmit(byte[] dst)
                {
                    var frame_data = Interlocked.Exchange(ref urgent_frame_data, null);
                    if (frame_data == null)
                    {
                        frame_data = this.frame_data;
                        if (!catch_ready_frame())
                            frame_data = null;
                    }

                    //Constructs a WebSocket frame according to RFC 6455, handling various payload lengths.
                    //   0                   1                   2                   3
                    //   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    //  +-+-+-+-+-------+-+-------------+-------------------------------+
                    //  |F|R|R|R| opcode|M| Payload len |    Extended payload length    |
                    //  |I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
                    //  |N|V|V|V|       |S|             |   (if payload len==126/127)   |
                    //  | |1|2|3|       |K|             |                               |
                    //  +-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
                    var s = (frame_data != null ? frame_data.buffer_bytes + 2 : //Header bytes for control frame
                                 0) +
                            10; //Max header bytes for data frame
                    var len = Internal.BytesSrc!.Read(dst, s, dst.Length - s);
                    if (0 < len)
                    {
                        var max = s + len;
                        switch (len)
                        {
                            case < 126:
                                dst[s -= 2] = (int)Mask.FIN | (int)OPCode.BINARY_FRAME;
                                dst[s + 1] = (byte)len;
                                break;
                            case < 0x1_0000:
                                dst[s -= 4] = (int)Mask.FIN | (int)OPCode.BINARY_FRAME;
                                dst[s + 1] = 126;
                                dst[s + 2] = (byte)(len >> 8);
                                dst[s + 3] = (byte)len;
                                break;
                            default:
                                dst[s -= 10] = (int)Mask.FIN | (int)OPCode.BINARY_FRAME;
                                dst[s + 1] = 127;
                                dst[s + 2] = 0;
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
                            sent_closing_frame = frame_data.OPcode == OPCode.CLOSE;
                            recycle_frame(frame_data.get_frame(dst, s -= frame_data.buffer_bytes + 2));
                        }

                        SetBuffer(s, max - s);
                        return true;
                    }

                    if (frame_data == null)
                    {
                        SetBuffer(0, 0);
                        return false;
                    }

                    sent_closing_frame = frame_data.OPcode == OPCode.CLOSE;
                    recycle_frame(frame_data.get_frame(dst, 0));
                    SetBuffer(0, s);
                    return true;
                }
                #endregion
                #region Receiving
                ///<summary>
                ///The current state of the WebSocket frame parsing state machine.
                ///</summary>
                State state = State.HANDSHAKE;

                ///<summary>
                ///The opcode of the current WebSocket frame being processed.
                ///</summary>
                OPCode OPcode;

                ///<summary>
                ///State variables for the frame parsing logic.
                ///</summary>
                int frame_bytes_left, BYTE, xor0, xor1, xor2, xor3;

                ///<summary>
                ///Holds the data for the current control frame (PING, PONG, CLOSE) being assembled.
                ///</summary>
                volatile ControlFrameData? frame_data;

                ///<summary>
                ///A lock to manage the state of the current control frame data object.
                ///</summary>
                volatile int frame_lock;

                ///<summary>
                ///Allocates a pooled <see cref="ControlFrameData"/> object to assemble an outgoing control frame.
                ///</summary>
                ///<param name="OPcode">The WebSocket opcode for the control frame.</param>
                protected void allocate_frame_data(OPCode OPcode)
                {
                    if (Interlocked.CompareExchange(ref frame_lock, FRAME_STANDBY, FRAME_READY) != FRAME_READY)
                    {
                        Interlocked.Exchange(ref frame_lock, FRAME_STANDBY);
                        frame_data = frames.Value!.get();
                    }

                    frame_data.buffer_bytes = 0;
                    frame_data.OPcode = OPcode;
                }

                ///<summary>
                ///Returns a <see cref="ControlFrameData"/> object to the thread-local pool for reuse.
                ///</summary>
                ///<param name="frame">The frame object to recycle.</param>
                protected void recycle_frame(ControlFrameData? frame)
                {
                    if (frame == null)
                        return;
                    Interlocked.CompareExchange(ref frame_data, null, frame);
                    frames.Value!.put(frame);
                }

                ///<summary>
                ///Marks the current control frame as ready for transmission and signals the transmitter.
                ///</summary>
                protected void frame_ready()
                {
                    Interlocked.Exchange(ref frame_lock, FRAME_READY);
                    onNewBytesToTransmitArrive(null!);
                }

                ///<summary>
                ///Atomically checks if a control frame is ready for transmission and, if so, claims it.
                ///</summary>
                ///<returns><c>true</c> if a frame was ready and successfully claimed; otherwise, <c>false</c>.</returns>
                protected bool catch_ready_frame() => Interlocked.CompareExchange(ref frame_lock, 0, FRAME_READY) == FRAME_READY;

                ///<summary>Constant representing the 'standby' state for the frame lock, indicating it's allocated but not ready.</summary>
                protected const int FRAME_STANDBY = 1;

                ///<summary>Constant representing the 'ready' state for the frame lock, indicating it's ready for transmission.</summary>
                protected const int FRAME_READY = 2;

                ///<summary>
                ///A helper class to manage the data and construction of WebSocket control frames and handshake responses.
                ///</summary>
                protected class ControlFrameData
                {
                    ///<summary>
                    ///The WebSocket opcode for this control frame (e.g., CLOSE, PING, PONG).
                    ///</summary>
                    public OPCode OPcode;

                    ///<summary>
                    ///The number of payload bytes currently in the <see cref="buffer"/>. Max 125 bytes per RFC 6455.
                    ///</summary>
                    public int buffer_bytes;

                    ///<summary>
                    ///The buffer holding the payload for a control frame.
                    ///</summary>
                    public readonly byte[] buffer = new byte[125];

                    ///<summary>
                    ///A reusable <see cref="SHA1"/> instance for WebSocket handshake key generation.
                    ///</summary>
                    public readonly SHA1 sha = SHA1.Create();

                    ///<summary>
                    ///Constructs the server's WebSocket handshake response by calculating the `Sec-WebSocket-Accept` key
                    ///and writing the full HTTP 101 response into the destination buffer.
                    ///</summary>
                    ///<param name="src">The source buffer containing the client's HTTP Upgrade request.</param>
                    ///<param name="dst">The destination buffer for the handshake response.</param>
                    ///<param name="pos">The starting position of the 'Sec-WebSocket-Key' value in the source buffer.</param>
                    ///<param name="max">The maximum index to parse in the source buffer.</param>
                    ///<returns>The total length of the handshake response written to the destination buffer.</returns>
                    public int put_UPGRADE_WEBSOCKET_response_into(byte[] src, byte[] dst, int pos, int max)
                    {
                        var len = 0; //Initialize length counter.

                        //Extract 'Sec-WebSocket-Key' from the HTTP Upgrade request header.
                        for (int b; pos < max && (b = src[pos]) != '\r'; pos++, len++) //Iterate until carriage return '\r' is found.
                            buffer[len] = (byte)b;                                     //Copy 'Sec-WebSocket-Key' value to buffer.

                        GUID.CopyTo(buffer, len); //Append the standard WebSocket GUID to the key for hashing.

                        sha.TryComputeHash(new ReadOnlySpan<byte>(buffer, 0, len + GUID.Length), buffer, out len); //Compute the SHA-1 hash.

                        UPGRADE_WEBSOCKET.CopyTo((Span<byte>)dst); //Copy the HTTP 101 response headers to the destination buffer.

                        len = base64(buffer, 0, len, dst, UPGRADE_WEBSOCKET.Length); //Base64 encode the hash and append it to the response.

                        rnrn.CopyTo(dst, len); //Append the final CRLF sequence to terminate the headers.

                        return len + rnrn.Length; //Return the total length of the response.
                    }

                    ///<summary>The standard GUID appended to the WebSocket key before hashing, as per RFC 6455.</summary>
                    static readonly byte[] GUID = Encoding.ASCII.GetBytes("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");

                    ///<summary>A byte array representing the CRLF sequence "\r\n\r\n" used to terminate HTTP headers.</summary>
                    static readonly byte[] rnrn = Encoding.ASCII.GetBytes("\r\n\r\n");

                    ///<summary>A pre-built byte array containing the static portion of the HTTP 101 Switching Protocols response.</summary>
                    static readonly byte[] UPGRADE_WEBSOCKET = Encoding.ASCII.GetBytes(
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Server: AdHoc\r\n" +
                        "Connection: Upgrade\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Sec-WebSocket-Accept: ");

                    ///<summary>
                    ///Encodes a byte array segment to its Base64 representation.
                    ///This custom implementation avoids the allocations of <see cref="Convert.ToBase64String"/>.
                    ///</summary>
                    ///<param name="src">The source byte array to encode.</param>
                    ///<param name="off">The starting offset in the source array.</param>
                    ///<param name="end">The ending index (exclusive) in the source array.</param>
                    ///<param name="dst">The destination buffer for the Base64-encoded output.</param>
                    ///<param name="dst_pos">The starting position in the destination buffer.</param>
                    ///<returns>The new position in the destination buffer after writing the encoded data.</returns>
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

                    ///<summary>The Base64 encoding character set.</summary>
                    static readonly byte[] base64_ = Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/");

                    ///<summary>
                    ///Constructs a complete WebSocket control frame (header and payload) in the destination buffer.
                    ///</summary>
                    ///<param name="dst">The destination buffer for the frame.</param>
                    ///<param name="dst_byte">The starting position in the destination buffer.</param>
                    ///<returns>The current instance, for method chaining.</returns>
                    public ControlFrameData get_frame(byte[] dst, int dst_byte)
                    {
                        dst[dst_byte++] = (byte)((int)Mask.FIN | (int)OPcode);
                        dst[dst_byte++] = (byte)buffer_bytes;
                        if (buffer_bytes > 0)
                            Array.Copy(buffer, 0, dst, dst_byte, buffer_bytes);
                        return this;
                    }

                    ///<summary>
                    ///Appends data to this control frame's payload buffer.
                    ///</summary>
                    ///<param name="src">The source byte array.</param>
                    ///<param name="start">The starting index in the source array.</param>
                    ///<param name="end">The ending index (exclusive) in the source array.</param>
                    internal void put_data(byte[] src, int start, int end)
                    {
                        var bytes = end - start;
                        Array.Copy(src, start, buffer, buffer_bytes, bytes);
                        buffer_bytes += bytes;
                    }
                }

                ///<summary>
                ///A thread-local pool for reusing <see cref="ControlFrameData"/> objects to reduce allocations and GC pressure.
                ///</summary>
                protected static readonly ThreadLocal<Pool<ControlFrameData>> frames = new(() => new Pool<ControlFrameData>(() => new ControlFrameData()));

                ///<summary>
                ///A pre-compiled Boyer-Moore search pattern for the "Sec-WebSocket-Key: " HTTP header.
                ///</summary>
                private static uint[] Sec_Websocket_Key_ = AdHoc.boyer_moore_pattern("Sec-WebSocket-Key: ");

                ///<summary>
                ///Parses the incoming HTTP request headers, finds the 'Sec-WebSocket-Key', and generates the WebSocket handshake response.
                ///</summary>
                ///<param name="bytes">A byte array containing the raw HTTP request headers.</param>
                ///<param name="max">The number of valid bytes in the array to parse.</param>
                public virtual void parsing_HTTP_header(byte[] bytes, int max)
                {
                    if (0 < Count)
                        return;
                    var pos = AdHoc.boyer_moore_ASCII_Case_insensitive(bytes, Sec_Websocket_Key_);
                    if (pos == -1)
                        return;
                    var pool = frames.Value!;
                    var helper = pool.get();
                    SetBuffer(0, helper.put_UPGRADE_WEBSOCKET_response_into(bytes, Buffer!, pos, max));
                    pool.put(helper);
                }

                ///<summary>
                ///Overrides the base receive logic to process incoming data through the WebSocket parsing state machine.
                ///This method handles the initial HTTP handshake and all subsequent data and control frames.
                ///The use of 'goto case' is a performance optimization to avoid method call overhead in the tight, byte-by-byte parsing loop.
                ///</summary>
                ///<param name="src">The source buffer containing the received data.</param>
                ///<param name="src_bytes">The number of valid bytes in the buffer.</param>
                protected override void receive(byte[] src, int src_bytes)
                {
                    for (int start = 0, index = 0; ;)
                        switch (state)
                        {
                            case State.HANDSHAKE:

                                if (4 <= src_bytes &&
                                    src[src_bytes - 4] == (byte)'\r' &&
                                    src[src_bytes - 3] == (byte)'\n' &&
                                    src[src_bytes - 2] == (byte)'\r' &&
                                    src[src_bytes - 1] == (byte)'\n')
                                {
                                    parsing_HTTP_header(src, src_bytes);
                                    if (Count == 0)
                                    {
                                        Internal.OnExternalEvent(this, (int)Event.WEBSOCKET_HANDSHAKE_FAILURE);
                                        host.onFailure(this, new Exception($"Sec-WebSocket-Key not found in header. Unexpected handshake: {Encoding.ASCII.GetString(src, 0, src_bytes)}"));
                                        Abort();
                                        return;
                                    }

                                    state = State.NEW_FRAME;
                                    Interlocked.Exchange(ref TransmitLock, ext!.SendAsync(this) ? 1 : 0);
                                    Internal.OnExternalEvent(this, (int)Event.WEBSOCKET_REMOTE_CONNECTED);
                                    return;
                                }

                                var n = src_bytes - 1;
                                for (; -1 < n && src[n] != '\n'; n--)
                                    ;
                                if (n == -1)
                                    return;

                                parsing_HTTP_header(src, n);
                                Array.Copy(src, n, src, 0, src_bytes -= n + 1);
                                ReceiveMate.SetBuffer(src_bytes, src.Length);
                                return;
                            case State.NEW_FRAME:
                                if (!get_byte(State.NEW_FRAME, ref index, src_bytes))
                                    return;
                                OPcode = (OPCode)(BYTE & (int)Mask.OPCODE);
                                goto case State.PAYLOAD_LENGTH_BYTE;
                            case State.PAYLOAD_LENGTH_BYTE:
                                if (!get_byte(State.PAYLOAD_LENGTH_BYTE, ref index, src_bytes))
                                    return;
                                if ((BYTE & (int)Mask.FIN) == 0)
                                {
                                    Internal.OnExternalEvent(this, (int)Event.WEBSOCKET_PROTOCOL_ERROR);
                                    host.onFailure(this, new Exception("Frames sent from client to server must have the MASK bit set to 1."));
                                    Abort();
                                    return;
                                }

                                xor0 = 0;
                                if (125 < (frame_bytes_left = BYTE & (int)Mask.LEN))
                                {
                                    xor0 = frame_bytes_left == 126 ? 2 : 8;
                                    frame_bytes_left = 0;
                                }

                                goto case State.PAYLOAD_LENGTH_BYTES;
                            case State.PAYLOAD_LENGTH_BYTES:
                                for (; 0 < xor0; xor0--)
                                    if (get_byte(State.PAYLOAD_LENGTH_BYTES, ref index, src_bytes))
                                        frame_bytes_left = (frame_bytes_left << 8) | BYTE;
                                    else
                                        return;
                                goto case State.XOR0;
                            case State.XOR0:
                                if (get_byte(State.XOR0, ref index, src_bytes))
                                    xor0 = BYTE;
                                else
                                    return;
                                goto case State.XOR1;
                            case State.XOR1:
                                if (get_byte(State.XOR1, ref index, src_bytes))
                                    xor1 = BYTE;
                                else
                                    return;
                                goto case State.XOR2;
                            case State.XOR2:
                                if (get_byte(State.XOR2, ref index, src_bytes))
                                    xor2 = BYTE;
                                else
                                    return;
                                goto case State.XOR3;
                            case State.XOR3:
                                if (get_byte(State.XOR3, ref index, src_bytes))
                                    xor3 = BYTE;
                                else
                                    return;
                                switch (OPcode)
                                {
                                    case OPCode.PING:
                                        allocate_frame_data(OPCode.PONG);
                                        if (frame_bytes_left == 0)
                                        {
                                            Internal.OnExternalEvent(this, (int)Event.WEBSOCKET_PING);
                                            frame_ready();
                                            state = State.NEW_FRAME;
                                            continue;
                                        }

                                        break;
                                    case OPCode.CLOSE:
                                        if (sent_closing_frame)
                                        {
                                            Internal.OnExternalEvent(this, (int)Event.WEBSOCKET_THIS_CLOSE_GRACEFUL);

                                            Close();
                                            _wsCloseGraceful = false;
                                            _wsClosingGraceful = false;
                                            return;
                                        }

                                        allocate_frame_data(OPCode.CLOSE);
                                        break;
                                    case OPCode.PONG:
                                        Internal.OnExternalEvent(this, (int)Event.WEBSOCKET_PONG);
                                        state = frame_bytes_left == 0 ? State.NEW_FRAME : State.DISCARD;
                                        continue;
                                    default: //Handles BINARY, TEXT, CONTINUATION
                                        if (frame_bytes_left == 0)
                                        {
                                            Internal.OnExternalEvent(this, (int)Event.WEBSOCKET_EMPTY_FRAME);
                                            state = State.NEW_FRAME;
                                            continue;
                                        }

                                        break;
                                }

                                start = index;
                                goto case State.DATA0;
                            case State.DATA0:
                                if (decode_and_continue(start, ref index, src_bytes))
                                    continue;
                                return;
                            case State.DATA1:
                                if (need_more_bytes(State.DATA1, ref start, ref index, src_bytes))
                                    return;
                                if (decode_byte_and_continue(xor1, ref start, ref index))
                                    continue;
                                goto case State.DATA2;
                            case State.DATA2:
                                if (need_more_bytes(State.DATA2, ref start, ref index, src_bytes))
                                    return;
                                if (decode_byte_and_continue(xor2, ref start, ref index)) continue;
                                goto case State.DATA3;
                            case State.DATA3:
                                if (need_more_bytes(State.DATA3, ref start, ref index, src_bytes))
                                    return;
                                if (decode_byte_and_continue(xor3, ref start, ref index)) continue;
                                if (decode_and_continue(start, ref index, src_bytes))
                                    continue;
                                return;
                            case State.DISCARD:
                                var bytes = Math.Min(src_bytes - start, frame_bytes_left);
                                index += bytes;
                                if ((frame_bytes_left -= bytes) == 0)
                                {
                                    state = State.NEW_FRAME;
                                    continue;
                                }

                                return;
                        }
                }

                ///<summary>
                ///Decodes a block of payload data using the XOR masking key. This loop continues until the frame is fully decoded
                ///or the end of the current buffer is reached.
                ///</summary>
                ///<param name="start">The starting index in the receive buffer for the current data block.</param>
                ///<param name="index">The current parsing index in the receive buffer.</param>
                ///<param name="max">The maximum valid index in the receive buffer.</param>
                ///<returns><c>true</c> to continue processing in the main loop; <c>false</c> if more data is needed.</returns>
                bool decode_and_continue(int start, ref int index, int max)
                {
                    for (; ; )
                    {
                        if (need_more_bytes(State.DATA0, ref start, ref index, max))
                            return false;
                        if (decode_byte_and_continue(xor0, ref start, ref index))
                            return true;
                        if (need_more_bytes(State.DATA1, ref start, ref index, max))
                            return false;
                        if (decode_byte_and_continue(xor1, ref start, ref index))
                            return true;
                        if (need_more_bytes(State.DATA2, ref start, ref index, max))
                            return false;
                        if (decode_byte_and_continue(xor2, ref start, ref index))
                            return true;
                        if (need_more_bytes(State.DATA3, ref start, ref index, max))
                            return false;
                        if (decode_byte_and_continue(xor3, ref start, ref index))
                            return true;
                    }
                }

                ///<summary>
                ///Checks if more bytes are needed from the network to continue parsing. If the current buffer is exhausted,
                ///it delivers any fully decoded data and sets the state machine to wait for the next buffer.
                ///</summary>
                ///<param name="state_if_no_more_bytes">The state to transition to if more bytes are needed.</param>
                ///<param name="start">The starting index of the current data block.</param>
                ///<param name="index">The current parsing index.</param>
                ///<param name="max">The maximum valid index in the buffer.</param>
                ///<returns><c>true</c> if more bytes are needed; <c>false</c> if the buffer has sufficient data to continue.</returns>
                bool need_more_bytes(State state_if_no_more_bytes, ref int start, ref int index, int max)
                {
                    if (index < max)
                        return false;
                    var src = ReceiveMate.Buffer!;
                    switch (OPcode)
                    {
                        case OPCode.PING:
                        case OPCode.CLOSE:
                            frame_data!.put_data(src, start, index);
                            break;
                        default:
                            Internal.BytesDst!.Write(src, start, index - start);
                            break;
                    }

                    state = frame_bytes_left == 0 ? State.NEW_FRAME : state_if_no_more_bytes;
                    return true;
                }

                ///<summary>
                ///Decodes a single payload byte using the specified XOR key. If this is the last byte of the frame,
                ///it delivers the completed payload and resets the state machine for the next frame.
                ///</summary>
                ///<param name="XOR">The XOR key byte for decoding.</param>
                ///<param name="start">The starting index of the current data block.</param>
                ///<param name="index">The current parsing index.</param>
                ///<returns><c>true</c> if the frame is now complete; <c>false</c> if more bytes remain in the frame.</returns>
                bool decode_byte_and_continue(int XOR, ref int start, ref int index)
                {
                    var src = ReceiveMate.Buffer!;
                    src[index] = (byte)(src[index++] ^ XOR);
                    if (0 < --frame_bytes_left)
                        return false;
                    state = State.NEW_FRAME;
                    switch (OPcode)
                    {
                        case OPCode.PING:
                            frame_data!.put_data(src, start, index);
                            Internal.OnExternalEvent(this, (int)Event.WEBSOCKET_PING);
                            frame_ready(); //Send the PONG response.
                            return true;
                        case OPCode.CLOSE:
                            frame_data!.put_data(src, start, index);
                            Internal.OnExternalEvent(this, (int)Event.WEBSOCKET_REMOTE_CLOSE_GRACEFUL);
                            frame_ready();
                            return true;
                        default:
                            Internal.BytesDst!.Write(src, start, index - start);
                            return true;
                    }
                }

                ///<summary>
                ///Retrieves the next byte from the receive buffer. If the buffer is exhausted, it updates the state machine
                ///and returns false to signal that more data is needed.
                ///</summary>
                ///<param name="state_if_no_more_bytes">The state to transition to if the buffer is empty.</param>
                ///<param name="index">The current parsing index in the buffer.</param>
                ///<param name="max">The maximum valid index in the buffer.</param>
                ///<returns><c>true</c> if a byte was successfully retrieved; <c>false</c> if more data is needed.</returns>
                bool get_byte(State state_if_no_more_bytes, ref int index, int max)
                {
                    if (index == max)
                    {
                        state = state_if_no_more_bytes;
                        return false;
                    }

                    BYTE = ReceiveMate.Buffer![index++];
                    return true;
                }
                #endregion

                ///<summary>
                ///Implements a WebSocket client using the built-in <see cref="System.Net.WebSockets.ClientWebSocket"/>.
                ///</summary>
                ///<remarks>
                ///This is not a generic WebSocket wrapper. It is a specialized class designed to
                ///exclusively service an internal receiver (<see cref="AdHoc.BytesDst"/>) and transmitter (<see cref="AdHoc.BytesSrc"/>).
                ///Its purpose is to act as a dedicated transport layer, bridging the internal data flow with the network
                ///without exposing public send or receive methods.
                ///</remarks>
                public class Client<INT> : AdHoc.Channel.External
                    where INT : class, AdHoc.Channel.Internal
                {
                    #region> Fields and Properties
                    ///<summary>The underlying WebSocket client provided by the .NET framework.</summary>
                    private ClientWebSocket? ws;
                    ///<summary>A lock to ensure that transmit operations are serialized.</summary>
                    private volatile int _transmitLock = 1;
                    ///<summary>A cancellation token source to signal shutdown for all asynchronous operations.</summary>
                    private readonly CancellationTokenSource _cts = new();
                    ///<summary>A flag to prevent concurrent connection attempts.</summary>
                    private volatile int _isConnecting = 0; //Use Interlocked for thread-safety

                    ///<summary>Gets the URI of the connected peer.</summary>
                    public Uri PeerIp { get; private set; }
                    ///<summary>A flag indicating that a graceful close has been initiated.</summary>
                    private bool _isClosingGracefully;
                    ///<summary>The configured transmit timeout in milliseconds.</summary>
                    private int _transmitTimeout = 100_000;

                    ///<summary>
                    ///Gets or sets the internal channel that provides data sources, destinations, and event handling logic.
                    ///</summary>
                    public AdHoc.Channel.Internal Internal { get; set; }

                    ///<summary>
                    ///Gets or sets the send timeout. Setting a negative value initiates a graceful close.
                    ///</summary>
                    public int TransmitTimeout
                    {
                        get => _isClosingGracefully ? -_transmitTimeout : _transmitTimeout;
                        set => _transmitTimeout = (_isClosingGracefully = value < 0) ? -value : value;
                    }

                    ///<summary>The configured receive timeout in milliseconds.</summary>
                    private int _receiveTimeout = 100_1000;

                    ///<summary>
                    ///Gets or sets the receive timeout. Setting a negative value initiates a graceful close.
                    ///</summary>
                    public virtual int ReceiveTimeout
                    {
                        get => _isClosingGracefully ? -_receiveTimeout : _receiveTimeout;
                        set
                        {
                            if (value < 0)
                            {
                                _receiveTimeout = -value;
                                Close();
                            }
                            else
                            {
                                _receiveTimeout = value;
                            }
                        }
                    }

                    ///<summary>
                    ///Initiates a graceful shutdown of the WebSocket connection.
                    ///</summary>
                    public void Close() => CloseAndDispose();

                    ///<summary>
                    ///Asynchronously sends a close frame and disposes the WebSocket client.
                    ///</summary>
                    public async void CloseAndDispose()
                    {
                        if (ws == null)
                            return;
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "AdHoc server closing", CancellationToken.None);
                    }

                    ///<summary>
                    ///Abruptly terminates the WebSocket connection.
                    ///</summary>
                    public void Abort() => ws?.Abort();
                    ///<summary>The name of this client instance for logging.</summary>
                    private readonly string _name;
                    ///<summary>The cached string representation of the client's current state.</summary>
                    private string _toString;
                    ///<summary>The callback for handling exceptions.</summary>
                    private readonly Action<object, Exception> _onFailure;
                    #endregion

                    ///<summary>
                    ///Initializes a new instance of the <see cref="Client{INT}"/> class with a default console failure logger.
                    ///</summary>
                    ///<param name="name">A descriptive name for the client instance.</param>
                    ///<param name="newInternal">A factory function to create the internal channel logic.</param>
                    ///<param name="bufferSize">The size for send and receive buffers.</param>
                    public Client(string name, Func<AdHoc.Channel.External, INT> newInternal, int bufferSize = 1024) : this(name, newInternal, onFailurePrintConsole, bufferSize) { }

                    ///<summary>
                    ///Initializes a new instance of the <see cref="Client{INT}"/> class.
                    ///</summary>
                    ///<param name="name">A descriptive name for the client instance.</param>
                    ///<param name="newInternal">A factory function to create the internal channel logic.</param>
                    ///<param name="onFailure">A callback to handle exceptions and failures.</param>
                    ///<param name="bufferSize">The size for send and receive buffers.</param>
                    public Client(string name, Func<AdHoc.Channel.External, INT> newInternal, Action<object, Exception> onFailure, int bufferSize = 1024)
                    {
                        Internal = newInternal(this);
                        _name = name;
                        _onFailure = onFailure ?? throw new ArgumentNullException(nameof(onFailure));
                        _toString = $"{_name} : disconnected";
                        transmitBuffer = new byte[bufferSize];
                        ws = new ClientWebSocket();
                        ws.Options.SetBuffer(bufferSize, bufferSize, receiveBuffer = new byte[bufferSize]);
                    }

                    ///<summary>
                    ///Asynchronously connects to a WebSocket server with a specified timeout.
                    ///</summary>
                    ///<param name="server">The URI of the WebSocket server.</param>
                    ///<param name="connectingTimeout">The maximum time to wait for the connection to be established.</param>
                    ///<returns>A task that represents the asynchronous connect operation. The task completes with the internal channel instance on success, or null on failure.</returns>
                    ///<exception cref="InvalidOperationException">Thrown if a connection is already in progress or established.</exception>
                    ///<example>
                    ///<code>
                    ///var client = new Client&lt;MyInternalChannel&gt;(
                    ///    name: "MyClient",
                    ///    newInternal: (ext) => new MyInternalChannel(ext),
                    ///    onFailure: (sender, ex) => Console.WriteLine($"Failed: {ex.Message}")
                    ///);
                    ///
                    ///var serverUri = new Uri("ws://localhost:8080");
                    ///var timeout = TimeSpan.FromSeconds(5);
                    ///try
                    ///{
                    ///    var internalChannel = await client.ConnectAsync(serverUri, timeout);
                    ///    if (internalChannel != null)
                    ///    {
                    ///        Console.WriteLine($"Connected to {serverUri}");
                    ///        // Use the internalChannel for communication
                    ///    }
                    ///}
                    ///catch (Exception ex)
                    ///{
                    ///    Console.WriteLine($"Connection failed: {ex.Message}");
                    ///}
                    ///</code>
                    ///</example>
                    public Task<INT> ConnectAsync(Uri server, TimeSpan connectingTimeout)
                    {
                        //Use Interlocked to prevent race conditions on the connection flag
                        if (Interlocked.CompareExchange(ref _isConnecting, 1, 0) == 1 || ws?.State == WebSocketState.Open)
                        {
                            var ex = new InvalidOperationException("Connection already in progress or established.");
                            _onFailure(this, ex);
                            throw ex;
                        }

                        var connectionTcs = new TaskCompletionSource<INT?>();

                        //Start the actual connection logic in a background task so this method can return the Task immediately.
                        _ = ConnectInternalAsync(server, connectingTimeout, connectionTcs);

                        return connectionTcs.Task;
                    }

                    ///<summary>
                    ///Asynchronously connects to a WebSocket server with a default 5-second timeout.
                    ///</summary>
                    ///<param name="server">The URI of the WebSocket server.</param>
                    ///<returns>A task that represents the asynchronous connect operation. The task completes with the internal channel instance on success, or null on failure.</returns>
                    public Task<INT?> ConnectAsync(Uri server) => ConnectAsync(server, TimeSpan.FromSeconds(5));

                    ///<summary>
                    ///Internal method to handle the actual connection logic and manage the TaskCompletionSource.
                    ///</summary>
                    private async Task ConnectInternalAsync(Uri server, TimeSpan connectingTimeout, TaskCompletionSource<INT?> tcs)
                    {
                        try
                        {
                            _transmitLock = 1;

                            using var connectCts = new CancellationTokenSource(connectingTimeout);
                            using var linkedConnectCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, connectCts.Token);

                            PeerIp = server;
                            await ws!.ConnectAsync(server, linkedConnectCts.Token);

                            _toString = $"{_name} {GetLocalEndpoint(server)} : {server}";

                            tcs.SetResult((INT)Internal);

                            Internal.OnExternalEvent(this, (int)Event.WEBSOCKET_THIS_CONNECT);

                            Interlocked.Exchange(ref _transmitLock, 0);

                            Internal.BytesSrc!.subscribeOnNewBytesToTransmitArrive(TransmitFromInternal);
                            _ = Task.Run(ReceiveToInternal, _cts.Token);
                        }
                        catch (Exception ex)
                        {
                            _onFailure(this, ex);
                            _toString = $"{_name} : connection failed";
                            ws?.Dispose();
                            ws = null;
                            //Complete the task with a null result to signal failure.
                            tcs.SetResult(null);
                        }
                        finally
                        {
                            Interlocked.Exchange(ref _isConnecting, 0);
                        }
                    }

                    ///<summary>
                    ///Helper to find the local endpoint of a TCP connection. This is a best-effort attempt.
                    ///</summary>
                    private string GetLocalEndpoint(Uri server)
                    {
                        try
                        {
                            var remoteIPs = Dns.GetHostAddresses(server.DnsSafeHost);
                            var properties = IPGlobalProperties.GetIPGlobalProperties();
                            var localEndPoint = properties.GetActiveTcpConnections()
                                                    .FirstOrDefault(c => c.RemoteEndPoint.Port == server.Port && remoteIPs.Contains(c.RemoteEndPoint.Address))
                                                    ?.LocalEndPoint;
                            return localEndPoint?.ToString() ?? "local:unknown";
                        }
                        catch
                        {
                            return "local:unknown";
                        }
                    }

                    ///<summary>The buffer used for sending data.</summary>
                    private byte[] transmitBuffer;

                    ///<summary>
                    ///The transmitter loop that reads from the internal source and sends data over the WebSocket.
                    ///</summary>
                    private async void TransmitFromInternal(AdHoc.BytesSrc src)
                    {
                        //Acquire lock to ensure only one send loop runs at a time.
                        if (Interlocked.Exchange(ref _transmitLock, 1) == 1)
                        {
                            return;
                        }

                        try
                        {
                            if (ws?.State != WebSocketState.Open)
                                return;
                            int bytesRead;
                            while (0 < (bytesRead = Internal.BytesSrc!.Read(transmitBuffer, 0, transmitBuffer.Length)))
                            {
                                try
                                {
                                    using var sendCts = new CancellationTokenSource(_transmitTimeout);
                                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, sendCts.Token);
                                    await ws.SendAsync(new ReadOnlyMemory<byte>(transmitBuffer, 0, bytesRead), WebSocketMessageType.Binary, true, linkedCts.Token);
                                }
                                catch (OperationCanceledException) when (!_cts.IsCancellationRequested)
                                {
                                    Internal.OnExternalEvent(this, (int)Event.WEBSOCKET_TRANSMIT_TIMEOUT);
                                    break;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Internal!.OnExternalEvent(this, (int)Event.WEBSOCKET_REMOTE_CLOSE_ABRUPTLY);
                        }
                        finally
                        {
                            Interlocked.Exchange(ref _transmitLock, 0); //Release lock
                            if (_isClosingGracefully)
                                Close();
                        }
                    }

                    ///<summary>The buffer used for receiving data.</summary>
                    private byte[] receiveBuffer;

                    ///<summary>
                    ///The receiver loop that reads from the WebSocket and writes data to the internal destination.
                    ///</summary>
                    private async Task ReceiveToInternal()
                    {
                        while (!_cts.Token.IsCancellationRequested && ws?.State == WebSocketState.Open)
                            try
                            {
                                using var receiveCts = new CancellationTokenSource(_receiveTimeout);
                                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, receiveCts.Token);
                                var result = await ws.ReceiveAsync(receiveBuffer, linkedCts.Token);
                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Peer closed connection", CancellationToken.None);
                                    Internal!.OnExternalEvent(this, (int)Event.WEBSOCKET_REMOTE_CLOSE_GRACEFUL);
                                    return;
                                }

                                Internal!.BytesDst!.Write(receiveBuffer, 0, result.Count);
                            }
                            catch (OperationCanceledException)
                            {
                                if (_cts.Token.IsCancellationRequested)
                                    break;
                                Internal!.OnExternalEvent(this, (int)Event.WEBSOCKET_RECEIVE_TIMEOUT);
                                break;
                            }
                            catch (Exception)
                            {
                                if (!_cts.IsCancellationRequested)
                                    Internal!.OnExternalEvent(this, (int)Event.WEBSOCKET_REMOTE_CLOSE_ABRUPTLY);
                                break;
                            }
                    }

                    ///<summary>
                    ///Asynchronously disconnects the client and releases resources.
                    ///</summary>
                    ///<returns>A task that represents the asynchronous disconnect operation.</returns>
                    public async Task DisconnectAsync()
                    {
                        if (_cts.IsCancellationRequested)
                            return;
                        _cts.Cancel();
                        _toString = $"{_name} : disconnected";
                        if (ws?.State == WebSocketState.Open)
                        {
                            try
                            {
                                using var closeTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", closeTimeoutCts.Token);
                            }
                            catch (Exception)
                            {
                                //Ignore exceptions during close, as the goal is to disconnect.
                            }
                        }

                        ws?.Dispose();
                        ws = null;
                    }

                    ///<summary>
                    ///Returns a string representing the current state of the client.
                    ///</summary>
                    ///<returns>A string with the client's name and connection status.</returns>
                    public override string ToString() => _toString;
                }

                ///<summary>
                ///Defines bitmasks for parsing the first two bytes of a WebSocket frame header, as specified in RFC 6455.
                ///</summary>
                internal enum Mask
                {
                    ///<summary>Bitmask for the FIN flag, indicating the final fragment of a message.</summary>
                    FIN = 0b1000_0000,

                    ///<summary>Bitmask to extract the opcode from the first byte of a frame.</summary>
                    OPCODE = 0b0000_1111,

                    ///<summary>Bitmask for the MASK flag, indicating if the payload is masked.</summary>
                    MASK = 0b1000_0000,

                    ///<summary>Bitmask to extract the initial payload length from the second byte of a frame.</summary>
                    LEN = 0b0111_1111
                }

                ///<summary>
                ///Defines the standard WebSocket frame opcodes as specified in RFC 6455.
                ///</summary>
                public enum OPCode
                {
                    ///<summary>Denotes a continuation frame for a fragmented message.</summary>
                    CONTINUATION = 0x00,

                    ///<summary>Denotes a text frame containing UTF-8 encoded data.</summary>
                    TEXT_FRAME = 0x01,

                    ///<summary>Denotes a binary frame containing arbitrary binary data.</summary>
                    BINARY_FRAME = 0x02,

                    ///<summary>Denotes a connection close frame.</summary>
                    CLOSE = 0x08,

                    ///<summary>Denotes a ping frame for heartbeat/keep-alive.</summary>
                    PING = 0x09,

                    ///<summary>Denotes a pong frame, typically a response to a ping.</summary>
                    PONG = 0x0A
                }

                ///<summary>
                ///Defines the states for the WebSocket frame parsing state machine.
                ///</summary>
                internal enum State
                {
                    ///<summary>Initial state, processing the HTTP handshake before WebSocket framing begins.</summary>
                    HANDSHAKE = 0,

                    ///<summary>Ready to parse the first byte of a new WebSocket frame.</summary>
                    NEW_FRAME = 1,

                    ///<summary>Parsing the second byte, which contains the MASK flag and initial payload length.</summary>
                    PAYLOAD_LENGTH_BYTE = 2,

                    ///<summary>Parsing the 2 or 8 extended payload length bytes (if initial length is 126 or 127).</summary>
                    PAYLOAD_LENGTH_BYTES = 3,

                    ///<summary>Parsing the first byte of the 4-byte masking key.</summary>
                    XOR0 = 4,

                    ///<summary>Parsing the second byte of the 4-byte masking key.</summary>
                    XOR1 = 5,

                    ///<summary>Parsing the third byte of the 4-byte masking key.</summary>
                    XOR2 = 6,

                    ///<summary>Parsing the fourth byte of the 4-byte masking key.</summary>
                    XOR3 = 7,

                    ///<summary>Processing the first byte of a 4-byte payload block.</summary>
                    DATA0 = 8,

                    ///<summary>Processing the second byte of a 4-byte payload block.</summary>
                    DATA1 = 9,

                    ///<summary>Processing the third byte of a 4-byte payload block.</summary>
                    DATA2 = 10,

                    ///<summary>Processing the fourth byte of a 4-byte payload block.</summary>
                    DATA3 = 11,

                    ///<summary>State for discarding payload bytes, typically after processing a control frame.</summary>
                    DISCARD = 12
                }
            }

            ///<summary>
            ///Implements a TCP server that listens for incoming connections on one or more endpoints,
            ///dispatching each new connection to a dedicated <see cref="ExternalChannel"/>.
            ///</summary>
            public class Server : TCP
            {
                #region> Server code
                #endregion> Network.TCP.Server

                ///<summary>
                ///Initializes a new instance of the <see cref="Server"/> class.
                ///</summary>
                ///<param name="name">A descriptive name for the server instance.</param>
                ///<param name="newChannel">A factory function to create channel objects for new connections.</param>
                ///<param name="onFailure">A callback for handling exceptions and failures.</param>
                ///<param name="bufferSize">The size of send and receive buffers for each channel.</param>
                ///<param name="Backlog">The maximum length of the pending connections queue.</param>
                ///<param name="socketBuilder">An optional factory to create custom listener sockets. If null, standard TCP sockets are used.</param>
                ///<param name="ips">An array of <see cref="IPEndPoint"/>s to bind the server to.</param>
                public Server(string name,
                              Func<TCP, ExternalChannel> newChannel,
                              Action<object, Exception> onFailure,
                              int bufferSize,
                              int Backlog,
                              Func<IPEndPoint, Socket>? socketBuilder,
                              params IPEndPoint[] ips) : base(name, newChannel, onFailure, bufferSize)
                {
                    //Start a background task for periodic channel maintenance.
                    Task.Run(async () =>
                             {
                                 while (true)
                                 {
                                     await maintenance_lock.WaitAsync();
                                     try
                                     {
                                         StartMaintenance();
                                         var waitTime = await MaintenanceAsync(DateTime.UtcNow.Ticks);
                                         if (RestartMaintenance())
                                             continue; //If re-run was requested, loop immediately.
                                         await Task.Delay(waitTime);
                                     }
                                     catch (Exception ex)
                                     {
                                         onFailure(this, ex);
                                     }
                                     finally
                                     {
                                         maintenance_lock.Release();
                                     }
                                 }
                             });
                    bind(Backlog, socketBuilder, ips);
                }

                ///<summary>
                ///Initializes a new instance of the <see cref="Server"/> class with a default failure handler.
                ///</summary>
                ///<param name="name">A descriptive name for the server instance.</param>
                ///<param name="newChannel">A factory function to create channel objects for new connections.</param>
                ///<param name="bufferSize">The size of send and receive buffers for each channel.</param>
                ///<param name="Backlog">The maximum length of the pending connections queue.</param>
                ///<param name="socketBuilder">An optional factory to create custom listener sockets. If null, standard TCP sockets are used.</param>
                ///<param name="ips">An array of <see cref="IPEndPoint"/>s to bind the server to.</param>
                public Server(string name,
                              Func<TCP, ExternalChannel> newChannel,
                              int bufferSize,
                              int Backlog,
                              Func<IPEndPoint, Socket>? socketBuilder,
                              params IPEndPoint[] ips) : this(name, newChannel, onFailurePrintConsole, bufferSize, Backlog, socketBuilder, ips) { }

                ///<summary>
                ///A list of <see cref="Socket"/> instances that are actively listening for incoming connections.
                ///</summary>
                public readonly List<Socket> tcp_listeners = new();

                ///<summary>
                ///Binds the server to the specified IP endpoints and begins listening for incoming connections.
                ///For each endpoint, it starts an asynchronous accept loop.
                ///</summary>
                ///<param name="Backlog">The maximum length of the pending connections queue.</param>
                ///<param name="socketBuilder">An optional factory function to create custom listener sockets. If null, a standard TCP socket is used.</param>
                ///<param name="ips">An array of <see cref="IPEndPoint"/>s to bind the server to.</param>
                public void bind(int Backlog, Func<IPEndPoint, Socket>? socketBuilder, params IPEndPoint[] ips)
                {
                    var sb = new StringBuilder(50).Append("Server ").Append(name);
                    socketBuilder ??= ip => new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    foreach (var ip in ips)
                    {
                        sb.Append('\n').Append("\t\t : ").Append(ip);
                        var tcp_listener = socketBuilder(ip);
                        tcp_listeners.Add(tcp_listener);
                        tcp_listener.Bind(ip);
                        tcp_listener.Listen(Backlog);
                        var on_accept_args = new SocketAsyncEventArgs();
                        //This handler will be reused for all accept operations on this listener.
                        EventHandler<SocketAsyncEventArgs> on_accept_handler = (_, _) =>
                        {
                            do
                            {
                                if (on_accept_args.SocketError == SocketError.Success)
                                    allocate().receiver_connected(on_accept_args.AcceptSocket!);
                                on_accept_args.AcceptSocket = null;
                            }
                            //Loop as long as AcceptAsync completes synchronously.
                            while (!tcp_listener.AcceptAsync(on_accept_args));
                        };
                        on_accept_args.Completed += on_accept_handler;
                        //Start the first accept operation.
                        if (!tcp_listener.AcceptAsync(on_accept_args))
                            on_accept_handler(tcp_listener, on_accept_args);
                    }

                    toString = sb.ToString();
                }
                #region Maintenance
                ///<summary>
                ///A semaphore to ensure that only one maintenance cycle runs at a time.
                ///</summary>
                private readonly SemaphoreSlim maintenance_lock = new(1, 1);

                ///<summary>
                ///Tracks the maintenance state: 0 (idle), 1 (running), 2 (re-run requested).
                ///</summary>
                private volatile int maintenance_state = 0;

                ///<summary>
                ///Sets the maintenance state to 'running' (1).
                ///</summary>
                protected void StartMaintenance() => maintenance_state = 1;

                ///<summary>
                ///Resets the maintenance state to 'idle' (0) and checks if a re-run was requested.
                ///</summary>
                ///<returns><c>true</c> if a re-run is needed; otherwise, <c>false</c>.</returns>
                protected bool RestartMaintenance() => 1 < Interlocked.Exchange(ref maintenance_state, 0);

                ///<summary>
                ///Atomically checks if maintenance is already running and, if not, requests a re-run.
                ///</summary>
                ///<returns><c>true</c> if maintenance was already running or a re-run was requested; otherwise, <c>false</c>.</returns>
                protected bool MaintenanceRunning() => 0 < Interlocked.Exchange(ref maintenance_state, 2);

                ///<summary>
                ///Triggers a maintenance cycle. If a cycle is already running, it schedules another one to run immediately after.
                ///</summary>
                public override void TriggerMaintenance() => MaintenanceRunning();

                ///<summary>
                ///Asynchronously triggers a maintenance cycle.
                ///</summary>
                ///<returns>A completed task.</returns>
                public override Task TriggerMaintenanceAsync()
                {
                    TriggerMaintenance();
                    return Task.CompletedTask;
                }

                ///<summary>
                ///Asynchronously performs maintenance on all active channels, handling tasks like timeouts,
                ///and calculates the delay until the next maintenance cycle.
                ///</summary>
                ///<param name="time">The current time in ticks, used for timeout calculations.</param>
                ///<returns>A task that resolves to the next maintenance interval in milliseconds.</returns>
                protected virtual async Task<int> MaintenanceAsync(long time)
                {
                    while (true)
                    {
                        var timeout = maintenance_duty_cycle;
                        for (var channel = channels; channel != null; channel = channel.next)
                            if (channel.IsActive)
                            {
                                if (channel.ready_for_maintenance())
                                {
                                    timeout = Math.Min(channel.maintenance(), timeout);
                                    channel.maintenance_completed();
                                }
                                else
                                    timeout = 0; //If any channel is busy, re-check soon.
                            }

                        if (timeout > 0)
                            return (int)timeout;
                        await Task.Delay(100); //Wait briefly before re-checking busy channels.
                    }
                }

                ///<summary>
                ///The default minimum interval in milliseconds between maintenance cycles.
                ///</summary>
                public uint maintenance_duty_cycle = 5000;
                #endregion

                ///<summary>The cached string representation of the server.</summary>
                private string? toString;

                ///<summary>
                ///Returns a string representation of the server, including its name and listening endpoints.
                ///</summary>
                ///<returns>A descriptive string for the server.</returns>
                public override string ToString() => toString ?? base.ToString()!;

                ///<summary>
                ///Gracefully shuts down the server by closing all listening sockets and aborting all active client connections.
                ///</summary>
                public void shutdown()
                {
                    tcp_listeners.ForEach(socket => socket.Close());
                    for (var channel = channels; channel != null; channel = channel.next)
                        if (channel.IsActive)
                            channel.Abort();
                }
            }

            ///<summary>
            ///Implements a TCP client for establishing a single outgoing connection.
            ///This client uses the high-performance SocketAsyncEventArgs pattern internally but exposes a modern async/await (TAP) interface.
            ///</summary>
            public class Client<INT> : TCP
                where INT : class, AdHoc.Channel.Internal
            {
                ///<summary>The cached string representation of the client's current state.</summary>
                private string? toString;

                ///<summary>
                ///Initializes a new instance of the <see cref="Client{INT}"/> class.
                ///</summary>
                ///<param name="name">The name of the client for identification.</param>
                ///<param name="newInternal">A factory that creates the internal channel implementation for the client's connection.</param>
                ///<param name="onFailure">A callback for handling failures during network operations.</param>
                ///<param name="bufferSize">The buffer size for socket operations.</param>
                public Client(string name,
                              Func<AdHoc.Channel.External, INT> newInternal,
                              Action<object, Exception> onFailure, int bufferSize = 1024)
                    : base(name, host => new ExternalChannel(host), onFailure, bufferSize)
                {
                    channels.Internal = newInternal(channels);
                }

                ///<summary>
                ///Initializes a new instance of the <see cref="Client{INT}"/> class with a default failure handler.
                ///</summary>
                ///<param name="name">The name of the client for identification.</param>
                ///<param name="newInternal">A factory that creates the internal channel implementation for the client's connection.</param>
                ///<param name="bufferSize">The buffer size for socket operations.</param>
                public Client(string name, Func<AdHoc.Channel.External, INT> newInternal, int bufferSize = 1024) : this(name, newInternal, onFailurePrintConsole, bufferSize) { }

                ///<summary>
                ///Asynchronously connects to a TCP server with a configurable timeout and cancellation support.
                ///</summary>
                ///<param name="server">The remote server's IP endpoint.</param>
                ///<param name="connectingTimeout">The timeout duration for the connection attempt.</param>
                ///<param name="cancellationToken">A token to cancel the connection attempt.</param>
                ///<returns>A task that returns the internal channel implementation on a successful connection.</returns>
                ///<exception cref="InvalidOperationException">Thrown if a connection is already active or in progress.</exception>
                ///<exception cref="SocketException">Thrown if the connection fails due to a socket error.</exception>
                ///<exception cref="OperationCanceledException">Thrown if the operation is canceled by the token or if it times out.</exception>
                ///<example>
                ///<code>
                ///var client = new Client&lt;MyInternalChannel&gt;(
                ///    name: "MyClient",
                ///    newInternal: (ext) => new MyInternalChannel(ext),
                ///    onFailure: (sender, ex) => Console.WriteLine($"Failed: {ex.Message}")
                ///);
                ///
                ///var server = new IPEndPoint(IPAddress.Loopback, 8080);
                ///var timeout = TimeSpan.FromSeconds(5);
                ///try
                ///{
                ///    var internalChannel = await client.ConnectAsync(server, timeout);
                ///    Console.WriteLine($"Connected to {server}");
                ///    // Use the internalChannel for communication
                ///}
                ///catch (Exception ex)
                ///{
                ///    Console.WriteLine($"Connection failed: {ex.Message}");
                ///}
                ///</code>
                ///</example>
                public async Task<INT> ConnectAsync(IPEndPoint server, TimeSpan connectingTimeout,
                                                    CancellationToken cancellationToken = default)
                {
                    if (channels.IsActive)
                    {
                        throw new InvalidOperationException("Connection already established.");
                    }

                    ResetChannelState();
                    toString = $"Client {name} : connecting to {server}";

                    var socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    try
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cts.CancelAfter(connectingTimeout);

                        await socket.ConnectAsync(server, cts.Token);

                        //If connection succeeds, configure the reusable channel with the new socket and start I/O.
                        channels.transmitterConnected(socket);
                        toString = $"Client {name} : connected to {channels.ext!.RemoteEndPoint}";
                        return (INT)channels.Internal;
                    }
                    catch (Exception ex)
                    {
                        socket.Close(); //Ensure socket is closed on any failure.
                        ResetChannelState();
                        toString = $"Client {name} : connection failed to {server}";
                        onFailure(this, ex); //Report the failure.
                        throw;               //Re-throw the exception to the caller.
                    }
                }

                ///<summary>
                ///Disconnects the client from the server.
                ///</summary>
                public void Disconnect()
                {
                    if (channels.IsActive)
                    {
                        channels.Abort();
                        ResetChannelState();
                        toString = $"Client {name} : disconnected";
                    }
                }

                ///<summary>
                ///Asynchronously disconnects the client from the server.
                ///</summary>
                ///<returns>A completed task after disconnection.</returns>
                public Task DisconnectAsync()
                {
                    Disconnect();
                    return Task.CompletedTask;
                }

                ///<summary>
                ///Resets the channel state to prepare for a new connection attempt.
                ///</summary>
                private void ResetChannelState()
                {
                    if (channels.ext != null)
                    {
                        channels.ext.Dispose();
                        channels.ext = null;
                    }

                    channels.AcceptSocket = null; //SAEA property
                    channels.RemoteEndPoint = null;
                }

                ///<summary>
                ///Returns the string representation of the client's current state.
                ///</summary>
                ///<returns>A string with the client's name and connection status.</returns>
                public override string ToString() => toString ?? base.ToString()!;
            }
        }

        ///<summary>
        ///Implements an in-memory "wire" that directly connects a <see cref="AdHoc.BytesSrc"/> (source)
        ///to a <see cref="AdHoc.BytesDst"/> (destination), facilitating data flow within the same process without a network socket.
        ///</summary>
        class Wire
        {
            ///<summary>A buffer used for transferring data between the source and destination.</summary>
            protected readonly byte[] buffer;

            ///<summary>The source of bytes for the data wire.</summary>
            protected AdHoc.BytesSrc? src;

            ///<summary>The subscriber action for handling new bytes from the source.</summary>
            protected Action<AdHoc.BytesSrc>? subscriber;

            ///<summary>
            ///Initializes a new instance of the <see cref="Wire"/> class.
            ///</summary>
            ///<param name="src">The source of bytes.</param>
            ///<param name="dst">The destination for bytes.</param>
            ///<param name="buffer_size">The size of the internal buffer for data transfer.</param>
            public Wire(AdHoc.BytesSrc src, AdHoc.BytesDst dst, int buffer_size)
            {
                buffer = new byte[buffer_size];
                connect(src, dst);
            }

            ///<summary>
            ///Connects a source and destination, establishing a data flow between them.
            ///</summary>
            ///<param name="src">The source of bytes.</param>
            ///<param name="dst">The destination for bytes.</param>
            public void connect(AdHoc.BytesSrc src, AdHoc.BytesDst dst)
            {
                //Set up a subscription that polls the source and writes to the destination whenever new data is available.
                subscriber = (this.src = src).subscribeOnNewBytesToTransmitArrive(
                    _ =>
                    {
                        for (int len; 0 < (len = src.Read(buffer, 0, buffer.Length));)
                            dst.Write(buffer, 0, len);
                    });
            }
        }

        ///<summary>
        ///A placeholder for a future UDP-based network communication implementation.
        ///</summary>
        ///<remarks>
        ///For reliable, ordered, and secure communication over UDP, it is recommended to
        ///layer a custom reliability protocol or utilize a secure tunnel solution such as WireGuard (https://www.wireguard.com/).
        ///</remarks>
        class UDP
        {
            //For robust UDP-based communication, consider using this TCP implementation over WireGuard: https://www.wireguard.com/
        }
    }

    ///<summary>
    ///Provides extension methods for interpreting <see cref="Network.TCP.ExternalChannel.Event"/> bitmasks.
    ///</summary>
    public static class __Event
    {
        ///<summary>Checks if the event is any type of CONNECT event.</summary>
        public static bool IsConnect(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.ACTION) == Action.CONNECT; }

        ///<summary>Checks if the event is any type of CLOSE event.</summary>
        public static bool IsClose(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.ACTION) == Action.CLOSE; }

        ///<summary>Checks if the event is a TIMEOUT event.</summary>
        public static bool IsTimeout(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.ACTION) == Action.TIMEOUT; }

        ///<summary>Checks if the event is a WebSocket handshake failure.</summary>
        public static bool IsHandshakeFailure(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.ACTION) == Action.HANDSHAKE_FAILURE; }

        ///<summary>Checks if the event is a WebSocket protocol error.</summary>
        public static bool IsProtocolError(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.ACTION) == Action.PROTOCOL_ERROR; }

        ///<summary>Checks if the event is a WebSocket PING.</summary>
        public static bool IsPing(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.ACTION) == Action.PING; }

        ///<summary>Checks if the event is a WebSocket PONG.</summary>
        public static bool IsPong(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.ACTION) == Action.PONG; }

        ///<summary>Checks if the event is a WebSocket empty data frame notification.</summary>
        public static bool IsEmptyFrame(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.ACTION) == Action.EMPTY_FRAME; }

        ///<summary>Checks if the event indicates a graceful action (e.g., graceful close).</summary>
        public static bool IsGraceful(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.GRACEFUL) != 0; }
        ///<summary>Checks if the event is specifically a graceful CLOSE event.</summary>
        public static bool IsCloseGraceful(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.ACTION) == Action.CLOSE && ((int)evt & Mask.GRACEFUL) != 0; }

        ///<summary>Checks if the event indicates an abrupt action (e.g., abrupt close).</summary>
        public static bool IsAbrupt(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.ABRUPT) != 0; }

        ///<summary>Checks if the event was initiated by the remote peer.</summary>
        public static bool IsRemote(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.REMOTE) != 0; }

        ///<summary>Checks if the event is specific to the WebSocket protocol layer.</summary>
        public static bool IsWebSocket(this Network.TCP.ExternalChannel.Event evt) { return ((int)evt & Mask.WEBSOCKET) != 0; }

        ///<summary>Gets a human-readable description of the composite event.</summary>
        ///<returns>A string describing the event.</returns>
        public static string GetDescription(this Network.TCP.ExternalChannel.Event evt)
        {
            return evt switch
            {
                Network.TCP.ExternalChannel.Event.REMOTE_CONNECT => "Remote connection accepted",
                Network.TCP.ExternalChannel.Event.THIS_CONNECT => "Local connection established",
                Network.TCP.ExternalChannel.Event.REMOTE_CLOSE_GRACEFUL => "Remote graceful close",
                Network.TCP.ExternalChannel.Event.THIS_CLOSE_GRACEFUL => "Local graceful close",
                Network.TCP.ExternalChannel.Event.REMOTE_CLOSE_ABRUPTLY => "Remote abrupt close",
                Network.TCP.ExternalChannel.Event.THIS_CLOSE_ABRUPTLY => "Local abrupt close",
                Network.TCP.ExternalChannel.Event.TRANSMIT_TIMEOUT => "Transmit timeout",
                Network.TCP.ExternalChannel.Event.RECEIVE_TIMEOUT => "Receive timeout",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_THIS_CONNECT => "WebSocket local connection established",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_REMOTE_CONNECTED => "WebSocket remote connected",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_HANDSHAKE_FAILURE => "WebSocket handshake failure",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_PROTOCOL_ERROR => "WebSocket protocol error",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_PING => "WebSocket ping",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_PONG => "WebSocket pong",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_EMPTY_FRAME => "WebSocket empty frame",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_REMOTE_CLOSE_GRACEFUL => "WebSocket remote graceful close",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_THIS_CLOSE_GRACEFUL => "WebSocket local graceful close",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_REMOTE_CLOSE_ABRUPTLY => "WebSocket remote abrupt close",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_THIS_CLOSE_ABRUPTLY => "WebSocket local abrupt close",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_TRANSMIT_TIMEOUT => "WebSocket transmit timeout",
                Network.TCP.ExternalChannel.Event.WEBSOCKET_RECEIVE_TIMEOUT => "WebSocket receive timeout",
                _ => "Unknown event"
            };
        }

        ///<summary>
        ///Defines the base action types for events.
        ///</summary>
        public static class Action
        {
            public const int CONNECT = 1;
            public const int CLOSE = 2;
            public const int TIMEOUT = 3;
            public const int PING = 4;
            public const int PONG = 5;

            public const int EMPTY_FRAME = 6;
            public const int HANDSHAKE_FAILURE = 7;
            public const int PROTOCOL_ERROR = 8;

            ///<summary>Gets a human-readable description of the base action part of an event code.</summary>
            ///<returns>A string describing the action.</returns>
            public static string GetDescription(int evenT)
            {
                return (evenT & Mask.ACTION) switch
                {
                    CONNECT => "Connection established",
                    CLOSE => "Connection terminated",
                    TIMEOUT => "Timeout occurred",
                    PING => "Ping received",
                    PONG => "Pong received",
                    EMPTY_FRAME => "Empty frame received",
                    HANDSHAKE_FAILURE => "Handshake failure",
                    PROTOCOL_ERROR => "Protocol error",
                    _ => "Unknown action"
                };
            }
        }

        ///<summary>
        ///Defines the bitmasks used to encode event properties.
        ///</summary>
        public static class Mask
        {
            //--- Source Flags (Bits 31-30) ---
            public const int THIS = 0;
            public const int REMOTE = 1 << 31;

            //--- Manner Flags (Bits 29-28) ---
            public const int GRACEFUL = 1 << 30;
            public const int ABRUPT = 1 << 29;

            //--- I/O Direction Flags (Bits 27-26) ---
            public const int TRANSMIT = 1 << 28;
            public const int RECEIVE = 1 << 27;

            //--- Protocol/Context Flags (Bit 25) ---
            public const int WEBSOCKET = 1 << 26;

            //--- Action Mask ---
            public const int ACTION = 0xFFFF;
        }
    }
}