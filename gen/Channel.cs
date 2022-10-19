
using System;

namespace  org.unirail
{
    public class Channel
    {
        public long session_id = 0;
        public long client_id = 0;
        public long param1 = 0;
        public long param2 = 0;
        public long [] longs = null;
        public Object info = null;

        public class  Realm
        {
            private Realm() { }
            public Func< Agent.ServerToAgent.Busy > new_ServerToAgent_Busy = () =>  new Agent.ServerToAgent.Busy();
            public Func< Agent.Project.Channel > new_Project_Channel = () =>  throw new Exception("Producer of Project.Channel is not assigned");
            public Func< Agent.Project.Host.Port.Pack.Field > new_Project_Host_Port_Pack_Field = () =>  throw new Exception("Producer of Project.Host.Port.Pack.Field is not assigned");
            public Func< Agent.Project.Host > new_Project_Host = () =>  throw new Exception("Producer of Project.Host is not assigned");
            public Func< Agent.Layout.View.Info.Driver > new_Layout_View_Info = () =>  Agent.Layout.View.Info.Driver.ONE;
            public Func< Agent.ServerToAgent.Info > new_ServerToAgent_Info = () =>  new Agent.ServerToAgent.Info();
            public Func< Agent.Layout > new_Layout = () =>  throw new Exception("Producer of Layout is not assigned");
            public Func< Agent.AgentToServer.Login > new_AgentToServer_Login = () =>  new Agent.AgentToServer.Login();
            public Func< Agent.ServerToAgent.LoginRejected.Driver > new_ServerToAgent_LoginRejected = () =>  Agent.ServerToAgent.LoginRejected.Driver.ONE;
            public Func< Agent.Project.Host.Port.Pack > new_Project_Host_Port_Pack = () =>  throw new Exception("Producer of Project.Host.Port.Pack is not assigned");
            public Func< Agent.Project.Host.Port > new_Project_Host_Port = () =>  throw new Exception("Producer of Project.Host.Port is not assigned");
            public Func< Agent.Project > new_Project = () =>  throw new Exception("Producer of Project is not assigned");
            public Func< Agent.AgentToServer.Proto > new_AgentToServer_Proto = () =>  new Agent.AgentToServer.Proto();
            public Func< Agent.AgentToServer.RequestResult > new_AgentToServer_RequestResult = () =>  new Agent.AgentToServer.RequestResult();
            public Func< Agent.ServerToAgent.Result > new_ServerToAgent_Result = () =>  new Agent.ServerToAgent.Result();
            public Func< Agent.ObserverToAgent.Show_Code > new_ObserverToAgent_Show_Code = () =>  new Agent.ObserverToAgent.Show_Code();
            public Func< Agent.ObserverToAgent.Up_to_date > new_ObserverToAgent_Up_to_date = () =>  new Agent.ObserverToAgent.Up_to_date();
            public Func< Agent.Upload.Driver > new_Upload = () =>  Agent.Upload.Driver.ONE;
            public Func< Agent.Layout.View > new_Layout_View = () =>  throw new Exception("Producer of Layout.View is not assigned");


            public Realm CopyTo(Realm dst)
            {
                dst.new_ServerToAgent_Busy = new_ServerToAgent_Busy;
                dst.new_Project_Channel = new_Project_Channel;
                dst.new_Project_Host_Port_Pack_Field = new_Project_Host_Port_Pack_Field;
                dst.new_Project_Host = new_Project_Host;
                dst.new_Layout_View_Info = new_Layout_View_Info;
                dst.new_ServerToAgent_Info = new_ServerToAgent_Info;
                dst.new_Layout = new_Layout;
                dst.new_AgentToServer_Login = new_AgentToServer_Login;
                dst.new_ServerToAgent_LoginRejected = new_ServerToAgent_LoginRejected;
                dst.new_Project_Host_Port_Pack = new_Project_Host_Port_Pack;
                dst.new_Project_Host_Port = new_Project_Host_Port;
                dst.new_Project = new_Project;
                dst.new_AgentToServer_Proto = new_AgentToServer_Proto;
                dst.new_AgentToServer_RequestResult = new_AgentToServer_RequestResult;
                dst.new_ServerToAgent_Result = new_ServerToAgent_Result;
                dst.new_ObserverToAgent_Show_Code = new_ObserverToAgent_Show_Code;
                dst.new_ObserverToAgent_Up_to_date = new_ObserverToAgent_Up_to_date;
                dst.new_Upload = new_Upload;
                dst.new_Layout_View = new_Layout_View;
                return dst;
            }
            public static readonly Realm DEFAULT = new();
        }
    }
}
