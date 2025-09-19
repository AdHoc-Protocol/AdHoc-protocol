
using System;
using org.unirail;
using org.unirail.Agent;
namespace org.unirail.Communication;
public interface Stages : AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header>
{
    #region> Stage code
    #endregion> Āÿ.Stage

    public class EXIT_ : Stages
    {
        public static readonly EXIT_ ONE = new EXIT_();

        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> EXIT OnSerialized
            #endregion> Āÿ_EXIT.OnSerialized
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
        #endregion> Āÿ.onERROR
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
        #endregion> Āÿ.onTIMEOUT
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
            #endregion> Āÿÿ.OnActivate
            context.stage = this;
            context.channel.ext_channal.TransmitTimeout = (int)TransmitTimeout.TotalMilliseconds;
            context.channel.ext_channal.ReceiveTimeout = int.MaxValue;
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnFailure code
            #endregion> Āÿÿ.OnFailure
            onTIMEOUT(context, this, sendHeaders, sendPack, receiveHeaders, receivePack);
        }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerializing code
            #endregion> Āÿÿ.OnSerializing

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Agent_.Version.__id_:
                    return null;
                default:
                    onERROR(context, this, headers, pack, null, null, "Sending unexpected id:" + pack.__id + " at Stage:" + this);
                    return " receive packet with unexpected id ";
            }
        }

        public void OnSerialized(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerialized code
            #endregion> Āÿÿ.OnSerialized

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Agent_.Version.__id_:
                    VersionMatching.ONE.OnActivate(context, this, headers!, pack, null, null);
                    #region> Version OnSent Event
                    //🌭<
                    Agent.AdHocProtocol.Agent_.Version.OnSent_via_Communication_at_Start.notify((AdHocProtocol.Agent_.Version)context.channel.transmitter.u8, context);
                    //🌭/>
                    #endregion> Āďÿÿď.OnSentEvent

                    return;

                default:
                    onERROR(context, this, headers, pack, null, null, "Sent unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceiving code
            #endregion> Āÿÿ.OnReceiving
            throw new NotImplementedException();
        }
        public Start() { transmitter = new Transmitter(this); }

        public readonly Transmitter transmitter;

        public class Transmitter(Start stage)
        {
            public bool send(AdHocProtocol.Agent_.Version src, Context context) { return context.stage == stage && context.channel.transmitter.sending_put(context, Agent.AdHocProtocol.Agent_.Version.Handler.ONE, src); }
        }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceived code
            #endregion> Āÿÿ.OnReceived
            throw new NotImplementedException();
        }
    }
    public partial class VersionMatching : Stages
    {
        public static readonly VersionMatching ONE = new();
        public const uint uid = 1;

        public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromMilliseconds(65535);
        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnActivate code
            #endregion> ĀÿĀ.OnActivate
            context.stage = this;
            context.channel.ext_channal.TransmitTimeout = int.MaxValue;
            context.channel.ext_channal.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnFailure code
            #endregion> ĀÿĀ.OnFailure
            onTIMEOUT(context, this, sendHeaders, sendPack, receiveHeaders, receivePack);
        }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerializing code
            #endregion> ĀÿĀ.OnSerializing
            throw new NotImplementedException();
        }

        public void OnSerialized(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerialized code
            #endregion> ĀÿĀ.OnSerialized

            throw new NotSupportedException("Not Implemented Exception");
        }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Server_.Info.__id_:
                case Agent.AdHocProtocol.Server_.Invitation.__id_:

                    return null;
                default:
                    onERROR(context, this, null, null, headers, pack, "Receiving unexpected id:" + pack.__id + " at Stage:" + this);
                    return " unexpected id ";
            }
            ;
            #region> OnReceiving code
            #endregion> ĀÿĀ.OnReceiving
        }
        public VersionMatching() { transmitter = new Transmitter(this); }

        public readonly Transmitter transmitter;

        public class Transmitter(VersionMatching stage)
        {
        }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceived code
            #endregion> ĀÿĀ.OnReceived

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Server_.Info.__id_:
                    EXIT_.ONE.OnActivate(context, this, null, null, headers!, pack);
                    ((Agent.AdHocProtocol.Server_.Info)pack).__OnReceived_via_Communication_at_VersionMatching(context);

                    return;
                case Agent.AdHocProtocol.Server_.Invitation.__id_:
                    Login.ONE.OnActivate(context, this, null, null, headers!, pack);
                    #region> Invitation OnReceived Event
                    //🌭<
                    Agent.AdHocProtocol.Server_.Invitation.OnReceived_via_Communication_at_VersionMatching.notify(context, Login.ONE.transmitter);
                    //🌭/>
                    #endregion> ĀĂÿĀĂ.OnReceivedEvent

                    return;

                default:
                    onERROR(context, this, null, null, headers, pack, "Received unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
    }
    public partial class Login : Stages
    {
        public static readonly Login ONE = new();
        public const uint uid = 2;

        public static readonly TimeSpan TransmitTimeout = TimeSpan.FromMilliseconds(65535);

        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnActivate code
            #endregion> Āÿā.OnActivate
            context.stage = this;
            context.channel.ext_channal.TransmitTimeout = (int)TransmitTimeout.TotalMilliseconds;
            context.channel.ext_channal.ReceiveTimeout = int.MaxValue;
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnFailure code
            #endregion> Āÿā.OnFailure
            onTIMEOUT(context, this, sendHeaders, sendPack, receiveHeaders, receivePack);
        }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerializing code
            #endregion> Āÿā.OnSerializing

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Agent_.Login.__id_:
                    return null;
                default:
                    onERROR(context, this, headers, pack, null, null, "Sending unexpected id:" + pack.__id + " at Stage:" + this);
                    return " receive packet with unexpected id ";
            }
        }

        public void OnSerialized(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerialized code
            #endregion> Āÿā.OnSerialized

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Agent_.Login.__id_:
                    LoginResponse.ONE.OnActivate(context, this, headers!, pack, null, null);
                    ((Agent.AdHocProtocol.Agent_.Login)pack).__OnSent_via_Communication_at_Login(context);
                    return;

                default:
                    onERROR(context, this, headers, pack, null, null, "Sent unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceiving code
            #endregion> Āÿā.OnReceiving
            throw new NotImplementedException();
        }
        public Login() { transmitter = new Transmitter(this); }

        public readonly Transmitter transmitter;

        public class Transmitter(Login stage)
        {
            public bool send(Agent.AdHocProtocol.Agent_.Login src, Context context) { return context.stage == stage && context.channel.transmitter.sending_put(context, src, 0); }
        }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceived code
            #endregion> Āÿā.OnReceived
            throw new NotImplementedException();
        }
    }
    public partial class LoginResponse : Stages
    {
        public static readonly LoginResponse ONE = new();
        public const uint uid = 3;

        public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromMilliseconds(65535);
        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnActivate code
            #endregion> ĀÿĂ.OnActivate
            context.stage = this;
            context.channel.ext_channal.TransmitTimeout = int.MaxValue;
            context.channel.ext_channal.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnFailure code
            #endregion> ĀÿĂ.OnFailure
            onTIMEOUT(context, this, sendHeaders, sendPack, receiveHeaders, receivePack);
        }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerializing code
            #endregion> ĀÿĂ.OnSerializing
            throw new NotImplementedException();
        }

        public void OnSerialized(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerialized code
            #endregion> ĀÿĂ.OnSerialized

            throw new NotSupportedException("Not Implemented Exception");
        }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Server_.Info.__id_:
                case Agent.AdHocProtocol.Server_.Invitation.__id_:
                case Agent.AdHocProtocol.Server_.InvitationUpdate.__id_:

                    return null;
                default:
                    onERROR(context, this, null, null, headers, pack, "Receiving unexpected id:" + pack.__id + " at Stage:" + this);
                    return " unexpected id ";
            }
            ;
            #region> OnReceiving code
            #endregion> ĀÿĂ.OnReceiving
        }
        public LoginResponse() { transmitter = new Transmitter(this); }

        public readonly Transmitter transmitter;

        public class Transmitter(LoginResponse stage)
        {
        }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceived code
            #endregion> ĀÿĂ.OnReceived

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Server_.Info.__id_:
                    EXIT_.ONE.OnActivate(context, this, null, null, headers!, pack);
                    ((Agent.AdHocProtocol.Server_.Info)pack).__OnReceived_via_Communication_at_LoginResponse(context);

                    return;
                case Agent.AdHocProtocol.Server_.Invitation.__id_:
                    TodoJobRequest.ONE.OnActivate(context, this, null, null, headers!, pack);
                    #region> Invitation OnReceived Event
                    //🌭<
                    Agent.AdHocProtocol.Server_.Invitation.OnReceived_via_Communication_at_LoginResponse.notify(context, TodoJobRequest.ONE.transmitter);
                    //🌭/>
                    #endregion> ĀĂÿĂĂ.OnReceivedEvent

                    return;

                case Agent.AdHocProtocol.Server_.InvitationUpdate.__id_:
                    TodoJobRequest.ONE.OnActivate(context, this, null, null, headers!, pack);
                    ((Agent.AdHocProtocol.Server_.InvitationUpdate)pack).__OnReceived_via_Communication_at_LoginResponse(context, TodoJobRequest.ONE.transmitter);

                    return;

                default:
                    onERROR(context, this, null, null, headers, pack, "Received unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
    }
    public partial class TodoJobRequest : Stages
    {
        public static readonly TodoJobRequest ONE = new();
        public const uint uid = 4;

        public static readonly TimeSpan TransmitTimeout = TimeSpan.FromMilliseconds(65535);

        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnActivate code
            #endregion> Āÿă.OnActivate
            context.stage = this;
            context.channel.ext_channal.TransmitTimeout = (int)TransmitTimeout.TotalMilliseconds;
            context.channel.ext_channal.ReceiveTimeout = int.MaxValue;
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnFailure code
            #endregion> Āÿă.OnFailure
            onTIMEOUT(context, this, sendHeaders, sendPack, receiveHeaders, receivePack);
        }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerializing code
            #endregion> Āÿă.OnSerializing

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Agent_.Proto.__id_:
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
            #endregion> Āÿă.OnSerialized

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Agent_.Proto.__id_:
                    Proto.ONE.OnActivate(context, this, headers!, pack, null, null);
                    ((Agent.AdHocProtocol.Agent_.Proto)pack).__OnSent_via_Communication_at_TodoJobRequest(context);
                    return;
                case Agent.AdHocProtocol.Agent_.Project.__id_:
                    Project.ONE.OnActivate(context, this, headers!, pack, null, null);
                    ((Agent.AdHocProtocol.Agent_.Project)pack).__OnSent_via_Communication_at_TodoJobRequest(context);
                    return;

                default:
                    onERROR(context, this, headers, pack, null, null, "Sent unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceiving code
            #endregion> Āÿă.OnReceiving
            throw new NotImplementedException();
        }
        public TodoJobRequest() { transmitter = new Transmitter(this); }

        public readonly Transmitter transmitter;

        public class Transmitter(TodoJobRequest stage)
        {
            public bool send(Agent.AdHocProtocol.Agent_.Proto src, Context context) { return context.stage == stage && context.channel.transmitter.sending_put(context, src, 0); }
            public bool send(Agent.AdHocProtocol.Agent_.Project src, Context context) { return context.stage == stage && context.channel.transmitter.sending_put(context, src, 0); }
        }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceived code
            #endregion> Āÿă.OnReceived
            throw new NotImplementedException();
        }
    }
    public partial class Project : Stages
    {
        public static readonly Project ONE = new();
        public const uint uid = 5;

        public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromMilliseconds(65535);
        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnActivate code
            #endregion> ĀÿĄ.OnActivate
            context.stage = this;
            context.channel.ext_channal.TransmitTimeout = int.MaxValue;
            context.channel.ext_channal.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnFailure code
            #endregion> ĀÿĄ.OnFailure
            onTIMEOUT(context, this, sendHeaders, sendPack, receiveHeaders, receivePack);
        }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerializing code
            #endregion> ĀÿĄ.OnSerializing
            throw new NotImplementedException();
        }

        public void OnSerialized(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerialized code
            #endregion> ĀÿĄ.OnSerialized

            throw new NotSupportedException("Not Implemented Exception");
        }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Server_.Info.__id_:
                case Agent.AdHocProtocol.Server_.Result.__id_:

                    return null;
                default:
                    onERROR(context, this, null, null, headers, pack, "Receiving unexpected id:" + pack.__id + " at Stage:" + this);
                    return " unexpected id ";
            }
            ;
            #region> OnReceiving code
            #endregion> ĀÿĄ.OnReceiving
        }
        public Project() { transmitter = new Transmitter(this); }

        public readonly Transmitter transmitter;

        public class Transmitter(Project stage)
        {
        }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceived code
            #endregion> ĀÿĄ.OnReceived

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Server_.Info.__id_:
                    EXIT_.ONE.OnActivate(context, this, null, null, headers!, pack);
                    ((Agent.AdHocProtocol.Server_.Info)pack).__OnReceived_via_Communication_at_Project(context);

                    return;

                case Agent.AdHocProtocol.Server_.Result.__id_:
                    EXIT_.ONE.OnActivate(context, this, null, null, headers!, pack);
                    ((Agent.AdHocProtocol.Server_.Result)pack).__OnReceived_via_Communication_at_Project(context);

                    return;

                default:
                    onERROR(context, this, null, null, headers, pack, "Received unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
    }
    public partial class Proto : Stages
    {
        public static readonly Proto ONE = new();
        public const uint uid = 6;

        public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromMilliseconds(65535);
        public void OnActivate(Context context, AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> prevStage, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnActivate code
            #endregion> Āÿą.OnActivate
            context.stage = this;
            context.channel.ext_channal.TransmitTimeout = int.MaxValue;
            context.channel.ext_channal.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
        }
        public void OnFailure(Context context, FailureReason reason, string? description, Channel.Transmitter.Header? sendHeaders, AdHoc.Channel.Transmitter.BytesSrc? sendPack, Channel.Receiver.Header? receiveHeaders, AdHoc.Channel.Receiver.BytesDst? receivePack)
        {
            #region> OnFailure code
            #endregion> Āÿą.OnFailure
            onTIMEOUT(context, this, sendHeaders, sendPack, receiveHeaders, receivePack);
        }
        public string? OnSerializing(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerializing code
            #endregion> Āÿą.OnSerializing
            throw new NotImplementedException();
        }

        public void OnSerialized(Context context, Channel.Transmitter.Header? headers, AdHoc.Channel.Transmitter.BytesSrc pack)
        {
            #region> OnSerialized code
            #endregion> Āÿą.OnSerialized

            throw new NotSupportedException("Not Implemented Exception");
        }
        public string? OnReceiving(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Server_.Info.__id_:
                case Agent.AdHocProtocol.Server_.Result.__id_:

                    return null;
                default:
                    onERROR(context, this, null, null, headers, pack, "Receiving unexpected id:" + pack.__id + " at Stage:" + this);
                    return " unexpected id ";
            }
            ;
            #region> OnReceiving code
            #endregion> Āÿą.OnReceiving
        }
        public Proto() { transmitter = new Transmitter(this); }

        public readonly Transmitter transmitter;

        public class Transmitter(Proto stage)
        {
        }
        public void OnReceived(Context context, Channel.Receiver.Header? headers, AdHoc.Channel.Receiver.BytesDst pack)
        {
            #region> OnReceived code
            #endregion> Āÿą.OnReceived

            switch (pack.__id)
            {
                case Agent.AdHocProtocol.Server_.Info.__id_:
                    EXIT_.ONE.OnActivate(context, this, null, null, headers!, pack);
                    ((Agent.AdHocProtocol.Server_.Info)pack).__OnReceived_via_Communication_at_Proto(context);

                    return;

                case Agent.AdHocProtocol.Server_.Result.__id_:
                    EXIT_.ONE.OnActivate(context, this, null, null, headers!, pack);
                    ((Agent.AdHocProtocol.Server_.Result)pack).__OnReceived_via_Communication_at_Proto(context);

                    return;

                default:
                    onERROR(context, this, null, null, headers, pack, "Received unexpected id:" + pack.__id + " at Stage:" + this);
                    return;
            }
        }
    }

    public static readonly Start O = Start.ONE; //Init Stage
}
