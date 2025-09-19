
using System;
using org.unirail;
#region> Import
#endregion> Āā.Context.Import
namespace org.unirail.ObserverCommunication;

public partial class Context
{
    public virtual void Connected()
    {
        #region> Context Connected
        #endregion> Āā.Context.Connected
    }
    #region> Context
    #endregion> Āā.Context

    public virtual void Closed()
    {
        #region> Context Closed
        #endregion> Āā.Context.Closed
        stage = Stages.O;
    }

    public volatile AdHoc.Channel.Stage<Context, Channel.Transmitter.Header, Channel.Receiver.Header> stage = Stages.O;

    public readonly int id;
    public readonly Channel channel;

    public Context(int id, Channel channel)
    {
        this.id = id;
        this.channel = channel;
    }
}
