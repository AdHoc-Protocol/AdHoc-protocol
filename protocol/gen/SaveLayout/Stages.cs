
using System;
using org.unirail;
using org.unirail.Agent;
namespace org.unirail.SaveLayout;
public interface Stages : AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header>
{
    #region> Stage code
    #endregion> ĀĀ.Stage

    public class EXIT_ : Stages
    {
        public static readonly EXIT_ ONE = new EXIT_();

        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> EXIT OnSerialized
            #endregion> ĀĀ_EXIT.OnSerialized
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
        #endregion> ĀĀ.onERROR
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
        #endregion> ĀĀ.onTIMEOUT
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
        public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromMilliseconds(65535);
        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnActivate code
            #endregion> ĀĀÿ.OnActivate
            context.stage = this;
            context.channel.ext_channal.TransmitTimeout = (int)TransmitTimeout.TotalMilliseconds;
            context.channel.ext_channal.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnFailure code
            #endregion> ĀĀÿ.OnFailure
            onTIMEOUT(context, this, sendHeaders, sendPack, receiveHeaders, receivePack);
        }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerializing code
            #endregion> ĀĀÿ.OnSerializing

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.LayoutFile_.UID.__id_:
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
            #endregion> ĀĀÿ.OnSerialized

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.LayoutFile_.UID.__id_:

                    ((Agent.AdHocProtocol.LayoutFile_.UID)pack).__OnSent_via_SaveLayout_at_Start(context, Start.ONE.transmitter);
                    return;

                case Agent.AdHocProtocol.LayoutFile_.Info.__id_:

                    ((Agent.AdHocProtocol.LayoutFile_.Info)pack).__OnSent_via_SaveLayout_at_Start(context, Start.ONE.transmitter);
                    return;

                default:
                    onERROR(context, this, headers, pack, null, null, "Sent unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.LayoutFile_.UID.__id_:
                case Agent.AdHocProtocol.LayoutFile_.Info.__id_:

                    return null;
                default:
                    onERROR(context, this, null, null, headers, pack, "Receiving unexpected id:" + pack.__id + " at Stage:" + this);
                    return " unexpected id ";
            }
            ;
            #region> OnReceiving code
            #endregion> ĀĀÿ.OnReceiving
        }
        public Start() { transmitter = new Transmitter(this); }

        public readonly Transmitter transmitter;

        public class Transmitter(Start stage)
        {
            public bool send(Agent.AdHocProtocol.LayoutFile_.UID src, Context context) { return context.stage == stage && context.channel.transmitter.sending_put(context, src, 0); }
            public bool send(Agent.AdHocProtocol.LayoutFile_.Info src, Context context) { return context.stage == stage && context.channel.transmitter.sending_put(context, src, 0); }
        }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceived code
            #endregion> ĀĀÿ.OnReceived

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.LayoutFile_.UID.__id_:

                    ((Agent.AdHocProtocol.LayoutFile_.UID)pack).__OnReceived_via_SaveLayout_at_Start(context, Start.ONE.transmitter);

                    return;

                case Agent.AdHocProtocol.LayoutFile_.Info.__id_:

                    ((Agent.AdHocProtocol.LayoutFile_.Info)pack).__OnReceived_via_SaveLayout_at_Start(context, Start.ONE.transmitter);

                    return;

                default:
                    onERROR(context, this, null, null, headers, pack, "Received unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
    }

    public static readonly Start O = Start.ONE; //Init Stage
}
