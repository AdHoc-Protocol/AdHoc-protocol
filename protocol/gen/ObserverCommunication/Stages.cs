
using System;
using org.unirail;
using org.unirail.Agent;
namespace org.unirail.ObserverCommunication;
public interface Stages : AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header>
{
    #region> Stage code
    #endregion> Āā.Stage

    public class EXIT_ : Stages
    {
        public static readonly EXIT_ ONE = new EXIT_();

        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> EXIT OnSerialized
            #endregion> Āā_EXIT.OnSerialized
            context.channel.transmitter.LastTransmission();
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack) { throw new InvalidOperationException(); }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack) { throw new InvalidOperationException(); }
        public void OnSerialized(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack) { throw new InvalidOperationException(); }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack) { throw new InvalidOperationException(); }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack) { throw new InvalidOperationException(); }
    }

    public static void onERROR(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> stage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack, string error)
    {
        #region> onERROR
        #endregion> Āā.onERROR
        context.channel.ext_channal.Close();
        if (receivePack != null)
            AdHoc.Channel.Receiver.error_handler.error(
                context.channel.receiver,
                AdHoc.Channel.Receiver.OnError.ERROR,
                new Exception($"Error `{error}` detected at stage: {stage}, during receiving of pack with id: {receivePack.__id}"));
        else
            AdHoc.Channel.Transmitter.error_handler.error(
                context.channel.transmitter,
                AdHoc.Channel.Transmitter.OnError.ERROR,
                new Exception($"Error `{error}` detected at stage: {stage}, during transmitting of pack with id: {(sendPack != null ? sendPack.__id : "unknown")}"));
        O.OnActivate(context, stage, sendHeaders, sendPack, receiveHeaders, receivePack);
    }

    public static void onTIMEOUT(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> stage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
    {
        #region> onTIMEOUT
        #endregion> Āā.onTIMEOUT
        context.channel.ext_channal.CloseAndDispose();
        if (receivePack != null)
            AdHoc.Channel.Receiver.error_handler.error(
                context.channel.receiver,
                AdHoc.Channel.Receiver.OnError.TIMEOUT,
                new Exception($"Timeout detected at stage: {stage}, during receiving of pack with id: {receivePack.__id}"));
        else
            AdHoc.Channel.Transmitter.error_handler.error(
                context.channel.transmitter,
                AdHoc.Channel.Transmitter.OnError.TIMEOUT,
                new Exception($"Timeout detected at stage: {stage}, during transmitting of pack with id: {(sendPack != null ? sendPack.__id : "unknown")}"));

        O.OnActivate(context, stage, sendHeaders, sendPack, receiveHeaders, receivePack);
    }
    public partial class Start : Stages
    {
        public static readonly Start ONE = new();
        public const uint uid = 0;

        public static readonly TimeSpan TransmitTimeout = TimeSpan.FromMilliseconds(65535);

        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnActivate code
            #endregion> Āāÿ.OnActivate
            context.stage = this;
            context.channel.ext_channal.TransmitTimeout = (int)TransmitTimeout.TotalMilliseconds;
            context.channel.ext_channal.ReceiveTimeout = int.MaxValue;
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnFailure code
            #endregion> Āāÿ.OnFailure
            onTIMEOUT(context, this, sendHeaders, sendPack, receiveHeaders, receivePack);
        }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerializing code
            #endregion> Āāÿ.OnSerializing

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Agent_.Project.__id_:
                case Agent.AdHocProtocol.LayoutFile_.Info.__id_:
                    return null;
                default:
                    onERROR(context, this, headers, pack, null, null, "Sending unexpected id:" + pack.__id + " at Stage:" + this);
                    return " receive packet with unexpected id ";
            }
        }

        public void OnSerialized(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerialized code
            #endregion> Āāÿ.OnSerialized

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Agent_.Project.__id_:
                    Operate.ONE.OnActivate(context, this, headers!, pack, null, null);
                    ((Agent.AdHocProtocol.Agent_.Project)pack).__OnSent_via_ObserverCommunication_at_Start(context);
                    return;
                case Agent.AdHocProtocol.LayoutFile_.Info.__id_:
                    LayoutSent.ONE.OnActivate(context, this, headers!, pack, null, null);
                    ((Agent.AdHocProtocol.LayoutFile_.Info)pack).__OnSent_via_ObserverCommunication_at_Start(context, LayoutSent.ONE.transmitter);
                    return;

                default:
                    onERROR(context, this, headers, pack, null, null, "Sent unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceiving code
            #endregion> Āāÿ.OnReceiving
            throw new NotImplementedException();
        }
        public Start() { transmitter = new Transmitter(this); }

        public readonly Transmitter transmitter;

        public class Transmitter(Start stage)
        {
            public bool send(Agent.AdHocProtocol.Agent_.Project src, Context context) { return context.stage == stage && context.channel.transmitter.sending_put(context, src, 0); }
            public bool send(Agent.AdHocProtocol.LayoutFile_.Info src, Context context) { return context.stage == stage && context.channel.transmitter.sending_put(context, src, 0); }
        }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceived code
            #endregion> Āāÿ.OnReceived
            throw new NotImplementedException();
        }
    }
    public partial class LayoutSent : Stages
    {
        public static readonly LayoutSent ONE = new();
        public const uint uid = 1;

        public static readonly TimeSpan TransmitTimeout = TimeSpan.FromMilliseconds(65535);

        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnActivate code
            #endregion> ĀāĀ.OnActivate
            context.stage = this;
            context.channel.ext_channal.TransmitTimeout = (int)TransmitTimeout.TotalMilliseconds;
            context.channel.ext_channal.ReceiveTimeout = int.MaxValue;
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnFailure code
            #endregion> ĀāĀ.OnFailure
            onTIMEOUT(context, this, sendHeaders, sendPack, receiveHeaders, receivePack);
        }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerializing code
            #endregion> ĀāĀ.OnSerializing

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Agent_.Project.__id_:
                    return null;
                default:
                    onERROR(context, this, headers, pack, null, null, "Sending unexpected id:" + pack.__id + " at Stage:" + this);
                    return " receive packet with unexpected id ";
            }
        }

        public void OnSerialized(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerialized code
            #endregion> ĀāĀ.OnSerialized

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Agent_.Project.__id_:
                    Operate.ONE.OnActivate(context, this, headers!, pack, null, null);
                    ((Agent.AdHocProtocol.Agent_.Project)pack).__OnSent_via_ObserverCommunication_at_LayoutSent(context);
                    return;

                default:
                    onERROR(context, this, headers, pack, null, null, "Sent unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceiving code
            #endregion> ĀāĀ.OnReceiving
            throw new NotImplementedException();
        }
        public LayoutSent() { transmitter = new Transmitter(this); }

        public readonly Transmitter transmitter;

        public class Transmitter(LayoutSent stage)
        {
            public bool send(Agent.AdHocProtocol.Agent_.Project src, Context context) { return context.stage == stage && context.channel.transmitter.sending_put(context, src, 0); }
        }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceived code
            #endregion> ĀāĀ.OnReceived
            throw new NotImplementedException();
        }
    }
    public partial class Operate : Stages
    {
        public static readonly Operate ONE = new();
        public const uint uid = 2;

        public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromMilliseconds(65535);
        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnActivate code
            #endregion> Āāā.OnActivate
            context.stage = this;
            context.channel.ext_channal.TransmitTimeout = int.MaxValue;
            context.channel.ext_channal.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnFailure code
            #endregion> Āāā.OnFailure
            onTIMEOUT(context, this, sendHeaders, sendPack, receiveHeaders, receivePack);
        }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerializing code
            #endregion> Āāā.OnSerializing
            throw new NotImplementedException();
        }

        public void OnSerialized(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerialized code
            #endregion> Āāā.OnSerialized

            throw new NotSupportedException("Not Implemented Exception");
        }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Observer_.Up_to_date.__id_:
                case Agent.AdHocProtocol.Observer_.Show_Code.__id_:
                case Agent.AdHocProtocol.LayoutFile_.Info.__id_:

                    return null;
                default:
                    onERROR(context, this, null, null, headers, pack, "Receiving unexpected id:" + pack.__id + " at Stage:" + this);
                    return " unexpected id ";
            }
            ;
            #region> OnReceiving code
            #endregion> Āāā.OnReceiving
        }
        public Operate() { transmitter = new Transmitter(this); }

        public readonly Transmitter transmitter;

        public class Transmitter(Operate stage)
        {
        }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceived code
            #endregion> Āāā.OnReceived

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Observer_.Up_to_date.__id_:
                    RefreshProject.ONE.OnActivate(context, this, null, null, headers!, pack);
                    ((Agent.AdHocProtocol.Observer_.Up_to_date)pack).__OnReceived_via_ObserverCommunication_at_Operate(context, RefreshProject.ONE.transmitter);

                    return;
                case Agent.AdHocProtocol.Observer_.Show_Code.__id_:
                    #region> Show_Code OnReceived Event
                    //🌭<
                    Agent.AdHocProtocol.Observer_.Show_Code.OnReceived_via_ObserverCommunication_at_Operate.notify((AdHocProtocol.Observer_.Show_Code)context.channel.receiver.u8, context);
                    //🌭/>
                    #endregion> ĀĒāāĒ.OnReceivedEvent

                    return;

                case Agent.AdHocProtocol.LayoutFile_.Info.__id_:

                    ((Agent.AdHocProtocol.LayoutFile_.Info)pack).__OnReceived_via_ObserverCommunication_at_Operate(context);

                    return;

                default:
                    onERROR(context, this, null, null, headers, pack, "Received unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
    }
    public partial class RefreshProject : Stages
    {
        public static readonly RefreshProject ONE = new();
        public const uint uid = 3;

        public static readonly TimeSpan TransmitTimeout = TimeSpan.FromMilliseconds(65535);

        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnActivate code
            #endregion> ĀāĂ.OnActivate
            context.stage = this;
            context.channel.ext_channal.TransmitTimeout = (int)TransmitTimeout.TotalMilliseconds;
            context.channel.ext_channal.ReceiveTimeout = int.MaxValue;
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnFailure code
            #endregion> ĀāĂ.OnFailure
            onTIMEOUT(context, this, sendHeaders, sendPack, receiveHeaders, receivePack);
        }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerializing code
            #endregion> ĀāĂ.OnSerializing

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Agent_.Project.__id_:
                case Agent.AdHocProtocol.Observer_.Up_to_date.__id_:
                    return null;
                default:
                    onERROR(context, this, headers, pack, null, null, "Sending unexpected id:" + pack.__id + " at Stage:" + this);
                    return " receive packet with unexpected id ";
            }
        }

        public void OnSerialized(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerialized code
            #endregion> ĀāĂ.OnSerialized

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Agent_.Project.__id_:
                    Operate.ONE.OnActivate(context, this, headers!, pack, null, null);
                    ((Agent.AdHocProtocol.Agent_.Project)pack).__OnSent_via_ObserverCommunication_at_RefreshProject(context);
                    return;

                case Agent.AdHocProtocol.Observer_.Up_to_date.__id_:
                    Operate.ONE.OnActivate(context, this, headers!, pack, null, null);
                    ((Agent.AdHocProtocol.Observer_.Up_to_date)pack).__OnSent_via_ObserverCommunication_at_RefreshProject(context);
                    return;

                default:
                    onERROR(context, this, headers, pack, null, null, "Sent unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceiving code
            #endregion> ĀāĂ.OnReceiving
            throw new NotImplementedException();
        }
        public RefreshProject() { transmitter = new Transmitter(this); }

        public readonly Transmitter transmitter;

        public class Transmitter(RefreshProject stage)
        {
            public bool send(Agent.AdHocProtocol.Agent_.Project src, Context context) { return context.stage == stage && context.channel.transmitter.sending_put(context, src, 0); }
            public bool send(Agent.AdHocProtocol.Observer_.Up_to_date src, Context context) { return context.stage == stage && context.channel.transmitter.sending_put(context, src, 0); }
        }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceived code
            #endregion> ĀāĂ.OnReceived
            throw new NotImplementedException();
        }
    }

    public static readonly Start O = Start.ONE; //Init Stage
}
