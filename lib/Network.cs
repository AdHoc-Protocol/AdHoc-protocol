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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace org.unirail
{
    public interface Network
    {
        interface INT // internal side
        {
            public interface BytesSrc : AdHoc.EXT.BytesSrc, AdHoc.EXT.BytesSrc.Producer
            {
                BytesDst? mate { get; }

                void Connected(TCP.Flow flow);
                void Closed();

                void AdHoc.EXT.BytesSrc.Close()
                {
                    var t = token();
                    if (t is TCP.Flow flow) flow.Close();
                }

                bool AdHoc.EXT.BytesSrc.isOpen()
                {
                    var t = token();
                    if (t is not TCP.Flow flow) return false;
                    var socket = flow.AcceptSocket;
                    return socket is { Connected: true };
                }
            }

            public interface BytesDst : AdHoc.EXT.BytesDst
            {
                BytesSrc? mate { get; }

                void Connected(TCP.Flow flow);
                void Closed();
            }
        }

        /** https://docs.microsoft.com/en-us/dotnet/framework/network-programming/socket-performance-enhancements-in-version-3-5?redirectedfrom=MSDN
          The pattern for performing an asynchronous socket operation with this class
            consists of the following steps:
    
            1.  Allocate a
                new [SocketAsyncEventArgs](https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socketasynceventargs) context
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
        public abstract class TCP
        {
            public readonly int buffer_size;

            public TCP(int bufferSize)
            {
                buffer_size = bufferSize;
                flows       = new Pool<Flow>.MultiThreaded<Flow>(() => new Flow(this));
            }

            protected void recycle(Flow free)
            {
                free.server_ip = null;
                free.int_src?.subscribe(null, null);
                if (free.int_src != null)
                {
                    free.int_src.subscribe(null, null);
                    free.int_src = null;
                }

                free.int_dst = null;
                free.mate  = null;
                free.SetBuffer(0, free.Buffer.Length);
                free.time = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                flows.put(free);
            }

            protected readonly Pool<Flow>.MultiThreaded<Flow> flows;

            protected abstract void cleanup(Flow flow);

            public abstract void shutdown();

            public class Flow : SocketAsyncEventArgs
            {
                private readonly TCP         _host;
                internal         Flow?       mate;
                public           ulong       time = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                internal         IPEndPoint? server_ip;
                public           Socket?     ext_socket;

                public Flow(TCP host)
                {
                    _host = host;
                    SetBuffer(ArrayPool<byte>.Shared.Rent(host.buffer_size), 0, host.buffer_size);
                    DisconnectReuseSocket = true;
                }

                public void SwitchTo(INT.BytesDst? dst, INT.BytesSrc? src)
                {
                    if (int_dst == null) // client
                    {
                        int_src = src;
                        if (mate     != null) mate.int_dst = dst;
                        else if (dst != null)
                        {
                            (mate = _host.flows.get()).mate = this; //make mate receiving flow
                            mate.receive(ext_socket!, dst);
                        }
                    }
                    else //
                    {
                        int_dst = dst;
                        if (mate     != null) mate.int_src = src;
                        else if (src != null)
                        {
                            (mate = _host.flows.get()).mate = this; //make mate transmitting flow
                            mate.ext_socket                     = ext_socket;
                            (mate.int_src = src).subscribe(mate.handleBytesOf, mate); //set callback
                        }
                    }
                }

                public void Close()
                {
                    var socket = Interlocked.Exchange(ref ext_socket, null);
                    if (socket == null) return;
                    var cleanup_mate = mate != null && Interlocked.Exchange(ref mate.ext_socket, null) != null;

                    socket.Disconnect(true);

                    if (server_ip != null) // Client Transmitter
                    {
                        if (cleanup_mate) _host.recycle(mate!); //mate Client Receiver

                        _host.cleanup(this);
                    }
                    else if (int_src != null) // Server Transmitter
                    {
                        if (cleanup_mate) _host.cleanup(mate!); //mate Server Receiver

                        _host.recycle(this);
                    }
                    else if (int_dst != null && mate?.server_ip == null) //  Server Receiver
                    {
                        if (cleanup_mate)
                        {
                            //mate  Server Transmitter
                            mate!.int_src!.Closed();
                            _host.recycle(mate!);
                        }

                        _host.cleanup(this);
                    }
                    else //Client Receiver
                    {
                        if (cleanup_mate) _host.cleanup(mate!);
                        _host.recycle(this);
                    }
                }

                protected override void OnCompleted(SocketAsyncEventArgs arg)
                {
                    base.OnCompleted(arg);
                    //LastOperation
                    //more easily facilitates using a single completion callback delegate for multiple kinds of
                    //asynchronous socket operations. This property describes the asynchronous socket operation that was most recently completed
                    if (arg.SocketError == SocketError.Success)
                        switch (arg.LastOperation)
                        {
                            case SocketAsyncOperation.Connect:
                                connected();
                                return;
                            case SocketAsyncOperation.Send:
                                transmitting();
                                return;
                            case SocketAsyncOperation.Receive:
                                receiving();
                                return;
                        }

                    Close();
                }


#region Receiving
                public INT.BytesDst? int_dst;

                public void receive(Socket src, INT.BytesDst dst)
                {
                    int_dst  = dst;
                    ext_socket = src;
                    
                    int_dst.Connected(this); //notify
                    if (mate == null) int_dst.mate?.subscribe(handleBytesOf, this);
                    if (!ext_socket.ReceiveAsync(this)) receiving();
                }

                void receiving()
                {
                    if (ext_socket == null) return;
                    do
                        if (0 < BytesTransferred) //the number of bytes transferred in the socket operation.
                            int_dst!.Write(Buffer, 0, BytesTransferred);
                        else // If zero is returned from a read operation, the remote end has closed the connection.
                        {
                            Close();
                            return;
                        }
                    while (!ext_socket.ReceiveAsync(this));//true: if the I/O operation is pending. fals:e if the I/O operation completed synchronously. 
                }
#endregion

#region Transmitting
                public INT.BytesSrc? int_src;

                public void connected()
                {
                    ext_socket = ConnectSocket;
                    var dst = int_src!.mate;
                    if (dst != null) //fullduplex client
                    {
                        (mate = _host.flows.get()).mate = this;
                        mate.receive(ext_socket!, dst);
                    }

                    int_src.Connected(this);
                    int_src.subscribe(handleBytesOf, this);
                }

                public void handleBytesOf(AdHoc.EXT.BytesSrc src)
                {
                    if (busy()) return;
                    if (int_src == null) //temporary listen on server receiver
                    {
                        (mate = _host.flows.get()).mate = this; //make transmitter flow

                        mate.ext_socket = ext_socket;
                        mate.busy();
                        (mate.int_src = (INT.BytesSrc)src).subscribe(mate.handleBytesOf, mate); //switch callback
                        idle();
                        mate.transmitting();
                    }
                    else transmitting(); //
                }

                private int _busy;

                //                                                  Returns The original value 
                public bool busy() { return Interlocked.CompareExchange(ref _busy, 1, 0) == 1; }

                public void idle() { Interlocked.Exchange(ref _busy, 0); }

                void transmitting()
                {
                    if (ext_socket == null) return;

                    var exit = true;
                    try
                    {
                        for (int count; 0 < (count = int_src.Read(Buffer!, 0, Buffer.Length));)
                        {
                            SetBuffer(0, count);
                            if (!ext_socket.SendAsync(this)) continue;
                            exit = false;
                            return;
                        }
                    }
                    finally
                    {
                        if (exit) idle();
                    }
                }
#endregion
            }


            public class Server : TCP
            {
                private readonly Pool<INT.BytesDst> outputs;

                public Server(int                       bufferSize,
                              int                       Backlog,
                              Func<INT.BytesDst>           supplier,
                              Func<IPEndPoint, Socket>? socketBuilder,
                              params IPEndPoint[]       ips) : this(bufferSize, Backlog, new Pool<INT.BytesDst>.MultiThreaded<INT.BytesDst>(supplier), socketBuilder, ips) { }

                public Server(int                       bufferSize,
                              int                       Backlog,
                              Pool<INT.BytesDst>           outputs,
                              Func<IPEndPoint, Socket>? socketBuilder,
                              params IPEndPoint[]       ips) : base(bufferSize)
                {
                    this.outputs = outputs;
                    bind(Backlog, socketBuilder, ips);
                }

                public readonly List<Socket> listeners = new();

                public void bind(int Backlog, Func<IPEndPoint, Socket>? socketBuilder, params IPEndPoint[] ips)
                {
                    socketBuilder ??= ip => new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    foreach (var ip in ips)
                    {
                        var socket = socketBuilder(ip);
                        listeners.Add(socket);

                        socket.Bind(ip);
                        socket.Listen(Backlog);

                        var arg = new SocketAsyncEventArgs();
                        arg.Completed += accept;

                        if (!socket.AcceptAsync(arg)) accept(socket, arg);
                    }
                }

                protected override void cleanup(Flow receiver)
                {
                    receiver.int_dst!.Closed();
                    var src = receiver.int_dst.mate;
                    if (src != null)
                    {
                        src.Closed();
                        src.subscribe(null, null);
                    }

                    outputs.put(receiver.int_dst);
                    receiver.int_dst = null;

                    recycle(receiver);
                }

                //https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.fieldoffsetattribute?view=netcore-3.1
                protected void accept(object? socket, SocketAsyncEventArgs arg)
                {
                    var server = (Socket)socket!;
                    do
                    {
                        if (arg.SocketError == SocketError.Success) flows.get().receive(arg.AcceptSocket!, outputs.get());

                        arg.AcceptSocket = null;
                    }
                    while (!server.AcceptAsync(arg));
                }

                public override void shutdown() => listeners.ForEach(socket => socket.Close());
            }

            public class Client : TCP
            {
                public readonly Flow transmitter;

                public readonly Action   OnConnectingTimout;
                public readonly TimeSpan timeout;

                public Client(int bufferSize, Action onConnectingTimout, TimeSpan timeout) : base(bufferSize)
                {
                    OnConnectingTimout = onConnectingTimout;
                    this.timeout       = timeout;
                    transmitter        = flows.get();
                }

                public void bind(INT.BytesSrc src, IPEndPoint dst)
                {
                    src.Close();
                    src.subscribe(handleBytesOf, dst);
                }

                public void handleBytesOf(AdHoc.EXT.BytesSrc src)
                {
                    var output = (INT.BytesSrc)src;
                    if (output.token(null) is not IPEndPoint ip) return;

                    output.subscribe(null, transmitter); //connecting progress mark
                    transmitter.int_src = output;

                    //The caller must set the SocketAsyncEventArgs.RemoteEndPoint property to the IPEndPoint of the remote host to connect to.
                    transmitter.RemoteEndPoint = ip;

                    var len = src.Read(transmitter.Buffer, 0, transmitter.Buffer.Length); //try to read in buffer


                    //https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.connectasync?view=net-5.0
                    //Optionally, a buffer may be provided which will atomically be sent on the socket after the ConnectAsync method succeeds.
                    //In this case, the SocketAsyncEventArgs.Buffer property needs to be set to the buffer containing the data to send and the
                    //SocketAsyncEventArgs.Count property needs to be set to the number of bytes of data to send from the buffer.
                    //Once a connection is established, this buffer of data is sent.
                    if (0 < len) transmitter.SetBuffer(0, len);

                    //https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.connectasync?view=netcore-3.1
                    if (!Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, transmitter)) transmitter.connected();
                    else
                    {
                        Task.Delay(timeout)
                            .ContinueWith(t =>
                                          {
                                              if (transmitter.ConnectSocket is not { Connected: true }) OnConnectingTimout();
                                          });
                    }
                }

                protected override void cleanup(Flow transmitter)
                {
                    transmitter.int_src!.Closed();
                    if (transmitter.int_src!.mate != null) transmitter.int_src.mate.Closed();

                    transmitter.int_src!.subscribe(handleBytesOf, transmitter.server_ip); //restore client init state. can be recconected lately

                    transmitter.server_ip = null;
                    transmitter.int_src    = null;

                    recycle(transmitter);
                }

                public override void shutdown() => transmitter.Close();
            }

            public interface Pool<T>
            {
                void put(T item);
                T    get();

                public class SingleThreaded<T> : Pool<T>
                {
                    private WeakReference<Stack<T>> items = new(new Stack<T>(5));

                    private readonly Func<T> supplier;

                    public SingleThreaded(Func<T> supplier) { this.supplier = supplier; }

                    public T get()
                    {
                        return items.TryGetTarget(out var i) && i.TryPop(out var item)
                                   ? item
                                   : supplier();
                    }

                    public void put(T item)
                    {
                        if (!items.TryGetTarget(out var s)) items = new WeakReference<Stack<T>>(s = new Stack<T>(5));
                        s.Push(item);
                    }
                }

                public class MultiThreaded<T> : Pool<T>
                {
                    private readonly ThreadLocal<SingleThreaded<T>> threadLocal = new();

                    private readonly Func<T> supplier;
                    public MultiThreaded(Func<T> supplier) { this.supplier = supplier; }

                    public void put(T item) => (threadLocal.Value ??= new SingleThreaded<T>(supplier)).put(item);
                    public T    get()       => (threadLocal.Value ??= new SingleThreaded<T>(supplier)).get();
                }
            }
        }

        class Wire
        {
            public           AdHoc.EXT.BytesDst dst;
            private readonly byte[]             buffer;
            private          int                src_byte;
            private          int                src_bytes;

            public Wire(AdHoc.EXT.BytesSrc.Producer producer, AdHoc.EXT.BytesDst dst, int buffer_size)
            {
                this.dst = dst;
                buffer   = new byte[buffer_size];
                producer.subscribe(handleBytesOf, null);
            }

            public void connect(AdHoc.EXT.BytesSrc src, AdHoc.EXT.BytesDst dst)
            {
                for (int len; 0 < (len = src.Read(buffer, 0, buffer.Length));)
                    dst.Write(buffer, 0, len);
            }

            public void handleBytesOf(AdHoc.EXT.BytesSrc src)
            {
                for (int len; 0 < (len = src.Read(buffer, 0, buffer.Length));)
                    dst.Write(buffer, 0, len);
            }
        }


        class UDP
        {
            //use TCP implementation over UDP Wireguard https://www.wireguard.com/
        }
    }
}