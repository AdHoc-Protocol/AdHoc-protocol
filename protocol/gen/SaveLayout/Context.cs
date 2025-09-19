
using System;
using org.unirail;
#region> Import
#endregion> ĀĀ.Context.Import
namespace org.unirail.SaveLayout;

public partial class Context
{
    public virtual void Connected()
    {
        #region> Context Connected
        #endregion> ĀĀ.Context.Connected
    }
    #region> Context
    #endregion> ĀĀ.Context

    public virtual void Closed()
    {
        #region> Context Closed
        #endregion> ĀĀ.Context.Closed
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
