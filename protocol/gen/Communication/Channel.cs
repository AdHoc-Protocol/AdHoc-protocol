
using System;
using org.unirail.collections;
using org.unirail;
#region> Channel import code
#endregion> Āÿ.Channel.Import
namespace org.unirail.Communication;
public partial class Channel : AdHoc.Channel.Internal
{
    #region> Channel code
    #endregion> Āÿ.Channel
    ///<summary>
    ///A factory method that creates a new, standard TCP communication channel.
    ///This function is designed to be passed as the `new_channel` delegate to the `Server` constructor
    ///to create a server that handles plain TCP clients.
    ///</summary>
    ///<param name="host">The underlying TCP connection object that this channel will wrap and manage.</param>
    ///<returns>A new instance of <c>Network.TCP.ExternalChannel</c>.</returns>
    ///<remarks>
    ///This method also instantiates a <c>Channel</c> to manage the I/O and lifecycle
    ///of the newly created TCP channel.
    ///</remarks>
    ///<example>
    ///How to use this factory when creating a standard TCP server:
    ///<code>
    ///var ipEndPoint = new IPEndPoint(IPAddress.Any, 8080);
    ///
    ///// Create a new server that accepts standard TCP connections
    ///// by passing this factory method to the constructor.
    ///var tcpServer = new Server(
    ///    name: "MyTCPServer",
    ///    new_channel: Channel.NewTCPChannel, // Pass the method directly
    ///    bufferSize: 8192,
    ///    Backlog: 100,
    ///    socketBuilder: null,
    ///    ips: ipEndPoint
    ///);
    ///</code>
    ///</example>
    public static Network.TCP.ExternalChannel NewTCPChannel(Network.TCP host)
    {
        var channel = new Network.TCP.ExternalChannel(host);
        channel.Internal = new Channel(channel);
        return channel;
    }

    ///<summary>
    ///A factory method that creates a new WebSocket channel, which operates over a TCP connection.
    ///This function is designed to be passed as the `new_channel` delegate to the `Server` constructor
    ///to create a server that handles WebSocket clients.
    ///</summary>
    ///<param name="host">The underlying TCP connection object that this channel will wrap and manage.</param>
    ///<returns>A new instance of <c>Network.TCP.WebSocket</c>, returned as its base type <c>Network.TCP.ExternalChannel</c>.</returns>
    ///<remarks>
    ///This method also instantiates a <c>Channel</c> to manage the WebSocket
    ///protocol's message framing and lifecycle for the new channel.
    ///</remarks>
    ///<example>
    ///How to use this factory when creating a WebSocket server:
    ///<code>
    ///var ipEndPoint = new IPEndPoint(IPAddress.Any, 8081);
    ///
    ///// Create a new server that accepts WebSocket connections
    ///// by passing this WebSocket-specific factory method.
    ///var webSocketServer = new Server(
    ///    name: "MyWebSocketServer",
    ///    new_channel: Channel.NewWebSocketChannel, // Pass the method directly
    ///    bufferSize: 8192,
    ///    Backlog: 100,
    ///    socketBuilder: null,
    ///    ips: ipEndPoint
    ///);
    ///</code>
    ///</example>
    public static Network.TCP.ExternalChannel NewWebSocketChannel(Network.TCP host)
    {
        var channel = new Network.TCP.WebSocket(host);
        channel.Internal = new Channel(channel);
        return channel;
    }

    private readonly Context contexts;
    public Context context(int id) { return contexts; }

    public readonly AdHoc.Channel.External ext_channal; //external channel
    public readonly Transmitter transmitter;
    public readonly Receiver receiver;
    public AdHoc.BytesDst? BytesDst => receiver;
    public AdHoc.BytesSrc? BytesSrc => transmitter;

    public void OnExternalEvent(AdHoc.Channel.External channel, int evenT)
    {
        #region> OnExternalEvent code
        #endregion> Āÿ.OnExternalEvent

        if (((Network.TCP.ExternalChannel.Event)evenT).IsConnect())
            contexts.Connected();
        else if (((Network.TCP.ExternalChannel.Event)evenT).IsClose())
            contexts.Closed();

        OnEvent.notify(this, evenT);
    }

    public Channel(AdHoc.Channel.External ext_channal)
    {
        this.ext_channal = ext_channal;
        contexts = new Context(0, this);
        transmitter = new Transmitter(this);
        receiver = new Receiver(this);
        ext_channal.Internal = this; //!!! the LAST
    }

    public class Transmitter : AdHoc.Channel.Transmitter, AdHoc.Channel.Transmitter.EventsHandler
    {

        public org.unirail.Communication.Channel? channel;

        protected override void Reset()
        {
            base.Reset();
            _LastTransmission = false;
            foreach (var data in sending_.Clear())
            {
                data.src = null;
                data.context = null;
            }
        }

        //Callback triggered after a packet is marked as sent from the INT to the EXT layer.
        //Note: This does not guarantee that the socket has transmitted all bytes of the packet.
        protected override void _OnSerialized(BytesSrc transmitted)
        {
            sending_out.context.stage.OnSerialized(sending_out.context, sending_out, transmitted);
        }

        public Transmitter(org.unirail.Communication.Channel channel, int power_of_2_sending_queue_size = 5) : base(null)
        {
            sending_ = new RingBuffer<DataPass>(power_of_2_sending_queue_size, () => new DataPass(channel));
            sending_in = new DataPass(channel);
            sending_out = new DataPass(channel);
            this.channel = channel;
        }

        public interface Header
        {
        }
        public class DataPass : AdHoc.Channel.Transmitter.BytesSrc, Header
        {
            public readonly org.unirail.Communication.Channel channel;
            internal DataPass(org.unirail.Communication.Channel channel) { this.channel = channel; }
            public ulong value;     //value pack payload
            public Context context; //context at this instance start

            public readonly byte[] bytes = new byte[0];
            internal int bytes_used = 0; //number of used bytes in the bytes buffer

            public int __id => src.__id;
            public AdHoc.Channel.Transmitter.BytesSrc src; //transmitting source
            public bool __get_bytes(AdHoc.Channel.Transmitter __dst)
            {

                var __slot = __dst.slot;
                int __v, __i;
                switch (__slot.state)
                {
                    case 0:
                        if (!__dst.put_val((uint)src.__id, 1, 1))
                            return false;
                        goto case 1;
                    case 1:
                        __slot.index_max_0(bytes_used);
                        goto case 2;
                    case 2:
                        if ((__slot.index0 += __dst.put(bytes, __slot.index0, __slot.index_max0 - __slot.index0, 2)) < __slot.index_max0)
                            return false;
                        return __slot.src_(src).__get_bytes(__dst);
                    default:
                        return true;
                }
            }
        }
        #region sending

        ///<summary>
        ///RingBuffer acting as the sending queue for <see cref="DataPass"/> items.
        ///</summary>
        readonly RingBuffer<DataPass> sending_;
        public override bool IsIdle() { return !sending_.IsEmpty; }

        ///<summary>
        ///Temporary <see cref="DataPass"/> instance for enqueuing, reused to reduce allocations.
        ///</summary>
        DataPass sending_in;
        ///<summary>
        ///<see cref="DataPass"/> instance for dequeue, reused.
        ///</summary>
        internal DataPass sending_out;

        protected override BytesSrc? _OnSerializing()
        {
            if (sending_.IsEmpty)
                return null;
            sending_lock_acquire();
            try
            {
                if (sending_.IsEmpty)
                    return null; //Indicate queue is full
                sending_out = sending_.Get(sending_out);
            }
            finally
            {
                sending_lock_release();
            }

            u8 = sending_out.value;
            u4 = (uint)sending_out.value;
            var error = sending_out.context.stage.OnSerializing(sending_out.context, sending_out, sending_out.src);
            if (error == null)
                return sending_out;
            Transmitter.error_handler.error(null, Transmitter.OnError.REJECTED, new InvalidOperationException(error));
            return _OnSerializing(); //pulling next pack to transmit
        }
        protected volatile bool _LastTransmission = false;

        ///<summary>
        ///Initiates a graceful shutdown sequence for the transmitter.
        ///<para>
        ///This method performs two primary actions:
        ///<list type="number">
        ///    <item><description>Sets a flag to block any new packets from being added to the send queue.</description></item>
        ///    <item><description>Discards all packets currently pending in the send queue.</description></item>
        ///</list>
        ///It then signals the underlying network channel to close gracefully after the current
        ///in-flight transmission completes. This signal is sent using a special convention:
        ///passing a <b>negative</b> timeout value to the channel's <c>TransmitTimeout</c> method.
        ///</para>
        ///</summary>
        ///<param name="timeout">The timeout duration to apply for the graceful close operation on the external channel.</param>
        internal void LastTransmission(TimeSpan timeout)
        {
            _LastTransmission = true;
            sending_.Clear();
            if (channel?.ext_channal != null)
                channel.ext_channal.TransmitTimeout = -(int)timeout.TotalMilliseconds;
        }
        internal void LastTransmission() { LastTransmission(TimeSpan.FromMilliseconds(channel!.ext_channal.TransmitTimeout)); }

        ///<summary>
        ///Sends a <see cref="Transmitter.BytesSrc"/> (packet data source) to the sending queue.
        ///</summary>
        ///<param name="context">An id of context.</param>
        ///<param name="src">The <see cref="Transmitter.BytesSrc"/> to send.</param>
        ///<param name="value">A value to associate with the data pass.</param>
        ///<returns>True if successfully added to queue, false if queue is full.</returns>
        internal bool sending_put(Context context, Transmitter.BytesSrc src, ulong value)
        {
            if (_LastTransmission || sending_.IsFull)
                return false;
            var notify = false;

            sending_lock_acquire();
            try
            {
                if (sending_.IsFull)
                    return false; //Indicate queue is full

                notify = sending_.IsEmpty;
                sending_in.src = src;
                sending_in.context = context;
                sending_in.value = value;

                sending_in.bytes_used = 0;
                sending_in = sending_.Put(sending_in); //enqueue.
            }
            finally
            {
                sending_lock_release();
            }

            if (notify)
                notify_subscribers();
            return true;
        }
        #endregion
    }

    public class Receiver : AdHoc.Channel.Receiver
    {

        public readonly org.unirail.Communication.Channel channel;

        public Receiver() : base(null, 1)
        {
            channel = null;
            receiving_data = new DataPass(null);
            receiving_data.context = channel.context(0);
        }

        public Receiver(org.unirail.Communication.Channel channel) : base(null, 1)
        {
            this.channel = channel;
            receiving_data = new DataPass(channel);
            receiving_data.context = channel.context(0);
        }

        public interface Header
        {
        }
        public class DataPass : AdHoc.Channel.Receiver.BytesDst, Header
        {
            public readonly org.unirail.Communication.Channel channel;
            internal DataPass(org.unirail.Communication.Channel channel) { this.channel = channel; }
            public ulong value;     //value pack payload
            public Context context; //context at this instance start

            public readonly byte[] bytes = new byte[0];
            internal int bytes_used = 0; //number of used bytes in the bytes buffer

            public int __id => dst.__id;
            public AdHoc.Channel.Receiver.BytesDst dst; //receiving destination
            public bool ignore_receiving = false;       //receiving pack is outdated, so do not dispatch on received
            public bool __put_bytes(AdHoc.Channel.Receiver __src)
            {
                var __slot = __src.slot;
                int __t, __i;
                switch (__slot.state)
                {
                    case 0:
                        __slot.index_max_0(bytes_used);
                        goto case 1;
                    case 1:
                        if ((__slot.index0 += __src.get_bytes(bytes, __slot.index0, __slot.index_max0 - __slot.index0, 1)) < __slot.index_max0)
                            return false;

                        context = channel.context(0);
                        var error = context.stage.OnReceiving(context, this, dst);
                        if (error != null)
                            AdHoc.Channel.Receiver.error_handler.error(__src, AdHoc.Channel.Receiver.OnError.REJECTED, new InvalidOperationException(error));
                        __src.u8_ = 0;
                        return __slot.dst_(dst).__put_bytes(__src);
                    default:
                        return true;
                }
            }
        }

        internal readonly DataPass receiving_data;

        //Callback triggered once enough bytes are received from the external layer to identify the packet type.
        protected override BytesDst _OnReceiving(int id)
        {
            #region> OnReceiving
            #endregion> Āÿ.Receiver.OnReceiving
            AdHoc.Channel.Receiver.BytesDst ret;
            switch (id)
            {
                case Agent.AdHocProtocol.Server_.Info.__id_:
                    receiving_data.dst = _Allocator.DEFAULT.new_AdHocProtocol_Server_Info(this);
                    ret = receiving_data.dst;
                    break;
                case Agent.AdHocProtocol.Server_.Invitation.__id_:
                    receiving_data.dst = Agent.AdHocProtocol.Server_.Invitation.Handler.ONE;
                    ret = receiving_data.dst;
                    break;
                case Agent.AdHocProtocol.Server_.InvitationUpdate.__id_:
                    receiving_data.dst = _Allocator.DEFAULT.new_AdHocProtocol_Server_InvitationUpdate(this);
                    ret = receiving_data.dst;
                    break;
                case Agent.AdHocProtocol.Server_.Result.__id_:
                    receiving_data.dst = _Allocator.DEFAULT.new_AdHocProtocol_Server_Result(this);
                    ret = receiving_data.dst;
                    break;

                default:
                    error_handler.error(this, OnError.INVALID_ID, new ArgumentOutOfRangeException("Unknown pack id:" + id));
                    return null;
            }

            var error = (receiving_data.context = channel.context(0)).stage.OnReceiving(receiving_data.context, null, receiving_data.dst);
            if (error != null)
                error_handler.error(this, OnError.REJECTED, new InvalidOperationException(error));
            return ret;
        }
        //Callback triggered once a packet is fully received and ready for dispatch to the internal layer.
        protected override void _OnReceived(BytesDst received)
        {
            receiving_data.context.stage.OnReceived(receiving_data.context, receiving_data, received);
        }
    }

    public class OnEvent
    {
        public static void notify(Channel channel, [Network.TCP.ExternalChannel.Event] int evenT) => handlers?.Invoke(channel, evenT);
        public static event Action<Channel, int> handlers;
    }
}
