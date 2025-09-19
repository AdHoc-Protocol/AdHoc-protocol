
using System;
using org.unirail;
#region> Import
#endregion> Āÿ.Context.Import
namespace org.unirail.Communication;

public partial class Context
{
    public virtual void Connected()
    {
        #region> Context Connected
        #endregion> Āÿ.Context.Connected
    }
    #region> Context
    #endregion> Āÿ.Context

    public virtual void Closed()
    {
        #region> Context Closed
        #endregion> Āÿ.Context.Closed
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
