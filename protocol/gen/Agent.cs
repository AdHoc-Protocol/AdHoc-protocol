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
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using org.unirail;
using System.Linq;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using org.unirail.collections;
using System.Runtime.CompilerServices;
using org.unirail.Agent;

namespace org.unirail
{
    ///<summary>
    ///Defines the Agent host, which corresponds to the `AdHocAgent` command-line tool.
    ///Its primary role is to serialize the user's protocol definition into the `Project` pack and send it to the Server.
    ///</summary>
    namespace Agent
    {
        ///<summary>
        ///This file defines the **meta-protocol** for the AdHoc system itself. It orchestrates the communication
        ///between the `AdHocAgent` (the developer's tool), the code-generation `Server`, and the `Observer`
        ///(the browser-based visualizer).
        ///
        ///It specifies the data structures (`packs`) like `Agent.Project`, the communication endpoints (`hosts`)
        ///such as `Server` and `Agent`, and the stateful communication flows (`channels`) that connect them,
        ///complete with stages and branching logic.
        ///</summary>
        public static partial class AdHocProtocol
        {
            ///<summary>
            ///Defines the Agent host, which corresponds to the `AdHocAgent` command-line tool.
            ///Its primary role is to serialize the user's protocol definition into the `Project` pack and send it to the Server.
            ///</summary>
            public static partial class Agent_
            {
                ///<summary>
                ///Contains the user's credentials (a permanent UUID) used for authentication with the Server.
                ///</summary>
                public partial class Login
                {
                    public void __OnSent_via_Communication_at_Login(Communication.Context context)
                    {
                        #region> Login OnSent Event
                        //🌭<
                        OnSent_via_Communication_at_Login.notify(this, context);
                        //🌭/>
                        #endregion> ĀĎÿāĎ.OnSentEvent
                    }

                    public class OnSent_via_Communication_at_Login
                    {
                        public static void notify(Login pack, Communication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Login, Communication.Context> handlers;
                    }
                }

                ///<summary>
                ///This is the central "meta-pack" of the system. It contains a complete, serialized
                ///description of a user's AdHoc protocol project. The Agent constructs this pack and sends
                ///it to the Server, which uses this structured data to perform code generation.
                ///</summary>
                public partial interface Project
                {
                    void __OnSent_via_Communication_at_TodoJobRequest(Communication.Context context)
                    {
                        #region> Project OnSent Event
                        //🌭<
                        OnSent_via_Communication_at_TodoJobRequest.notify(this, context);
                        //🌭/>
                        #endregion> ĀĆÿăĆ.OnSentEvent
                    }

                    public class OnSent_via_Communication_at_TodoJobRequest
                    {
                        public static void notify(Project pack, Communication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Project, Communication.Context> handlers;
                    }

                    void __OnSent_via_ObserverCommunication_at_Start(ObserverCommunication.Context context)
                    {
                        #region> Project OnSent Event
                        //🌭<
                        OnSent_via_ObserverCommunication_at_Start.notify(this, context);
                        //🌭/>
                        #endregion> ĀĆāÿĆ.OnSentEvent
                    }

                    public class OnSent_via_ObserverCommunication_at_Start
                    {
                        public static void notify(Project pack, ObserverCommunication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Project, ObserverCommunication.Context> handlers;
                    }

                    void __OnSent_via_ObserverCommunication_at_LayoutSent(ObserverCommunication.Context context)
                    {
                        #region> Project OnSent Event
                        //🌭<
                        OnSent_via_ObserverCommunication_at_LayoutSent.notify(this, context);
                        //🌭/>
                        #endregion> ĀĆāĀĆ.OnSentEvent
                    }

                    public class OnSent_via_ObserverCommunication_at_LayoutSent
                    {
                        public static void notify(Project pack, ObserverCommunication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Project, ObserverCommunication.Context> handlers;
                    }

                    void __OnSent_via_ObserverCommunication_at_RefreshProject(ObserverCommunication.Context context)
                    {
                        #region> Project OnSent Event
                        //🌭<
                        OnSent_via_ObserverCommunication_at_RefreshProject.notify(this, context);
                        //🌭/>
                        #endregion> ĀĆāĂĆ.OnSentEvent
                    }

                    public class OnSent_via_ObserverCommunication_at_RefreshProject
                    {
                        public static void notify(Project pack, ObserverCommunication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Project, ObserverCommunication.Context> handlers;
                    }

                    ///<summary>
                    ///Describes a single communication Channel between two Hosts.
                    ///</summary>
                    public partial interface Channel
                    {
                        ///<summary>
                        ///Describes a single state (Stage) in the channel's state machine.
                        ///</summary>
                        public partial interface Stage
                        {
                            ///<summary>
                            ///Describes a single transition (Branch) from a Stage, which is triggered by sending a specific pack.
                            ///</summary>
                            public partial interface Branch { }
                        }
                    }

                    ///<summary>
                    ///Describes a single Host within the user's project.
                    ///</summary>
                    public partial interface Host
                    {
                        [Flags]
                        public enum Langs : ushort
                        {
                            All = 65535,
                            InCPP = 1,
                            InCS = 4,
                            InGO = 16,
                            InJAVA = 8,
                            InRS = 2,
                            InSwift = 64,
                            InTS = 32,
                        }

                        ///<summary>
                        ///Describes a single Pack (data structure) within the user's project.
                        ///</summary>
                        public partial interface Pack
                        {
                            ///<summary>Describes a single constant or enum member within the protocol.</summary>
                            public partial interface Constant { }

                            ///<summary>
                            ///Describes a single Field within a Pack, including its type, constraints, and attributes.
                            ///</summary>
                            public partial interface Field
                            {
                                ///<summary>Internal enumeration of all possible data types recognized by the generator. These are abstract types mapped to platform-specific ones during code generation.</summary>
                                public enum DataType : ushort
                                {
                                    t_binary = 65529, //Reserved for binary data
                                    t_bool = 65531, //Reserved for boolean type
                                    t_char = 65525, //Reserved for characters
                                    t_constants = 65535, //Reserved for a constant set type
                                    t_double = 65519, //Reserved for double type
                                    t_enum_exp = 65533, //Reserved for expression enums
                                    t_enum_flags = 65532, //Reserved for flags enum
                                    t_enum_sw = 65534, //Reserved for switch enums
                                    t_float = 65520, //Reserved for a float type
                                    t_int16 = 65527, //Reserved for 16-bit signed integers
                                    t_int32 = 65524, //Reserved for 32-bit signed integers
                                    t_int64 = 65522, //Reserved for 64-bit signed integers
                                    t_int8 = 65530, //Reserved for 8-bit signed integers
                                    t_map = 65517, //Reserved for a map type
                                    t_set = 65516, //Reserved for a set type
                                    t_string = 65518, //Reserved for a string type
                                    t_subpack = 65515, //Reserved for a sub-pack type
                                    t_uint16 = 65526, //Reserved for 16-bit unsigned integers
                                    t_uint32 = 65523, //Reserved for 32-bit unsigned integers
                                    t_uint64 = 65521, //Reserved for 64-bit unsigned integers
                                    t_uint8 = 65528, //Reserved for 8-bit unsigned integers
                                }
                            }
                        }
                    }
                }

                ///<summary>
                ///A pack used to send a `.proto` file (or files) to the Server for conversion into the AdHoc format.
                ///</summary>
                public partial class Proto
                {
                    public void __OnSent_via_Communication_at_TodoJobRequest(Communication.Context context)
                    {
                        #region> Proto OnSent Event
                        //🌭<
                        OnSent_via_Communication_at_TodoJobRequest.notify(this, context);
                        //🌭/>
                        #endregion> ĀĐÿăĐ.OnSentEvent
                    }

                    public class OnSent_via_Communication_at_TodoJobRequest
                    {
                        public static void notify(Proto pack, Communication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Proto, Communication.Context> handlers;
                    }
                }

                public partial struct Version
                {
                    public class OnSent_via_Communication_at_Start
                    {
                        public static void notify(Version pack, Communication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Version, Communication.Context> handlers;
                    }
                }
            }

            ///<summary>
            ///Represents a reference to a specific item (e.g., Pack, Host, Field) within the protocol structure.
            ///This pack is used by the `Observer` to send commands to the `Agent` related to a specific UI element,
            ///enabling features like "Show Code" for interactive visualization.
            ///</summary>
            public static partial class Item
            {
                public enum Type : byte
                { //Enumeration defining the possible types of an item.
                    Channel = 5,        //A reference to a communication channel.
                    Constant = 4,        //A reference to a constant.
                    Field = 3,        //A reference to a specific field.
                    Host = 1,        //A reference to a specific host.
                    Pack = 2,        //A reference to a specific pack.
                    Project = 0,        //A reference to the entire project.
                    Stage = 6,        //A reference to a stage within a channel's state machine.
                }
            }

            ///<summary>
            ///Defines a virtual host to model the `.layout` file on disk. This allows saving and loading
            ///the visual state of the Observer's diagrams as a standard protocol interaction,
            ///rather than handling it as a special case.
            ///</summary>
            public static partial class LayoutFile_
            {
                ///<summary>
                ///Contains the actual layout information, such as coordinates, zoom levels, and splitter positions
                ///for the various diagrams displayed in the Observer.
                ///</summary>
                public partial class Info
                {
                    public void __OnReceived_via_ObserverCommunication_at_Operate(ObserverCommunication.Context context)
                    {
                        #region> Info OnReceived Event
                        //🌭<
                        OnReceived_via_ObserverCommunication_at_Operate.notify(this, context);
                        //🌭/>
                        #endregion> ĀĔāāĔ.OnReceivedEvent
                    }

                    public class OnReceived_via_ObserverCommunication_at_Operate
                    {
                        public static void notify(Info pack, ObserverCommunication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Info, ObserverCommunication.Context> handlers;
                    }

                    public void __OnSent_via_ObserverCommunication_at_Start(ObserverCommunication.Context context, ObserverCommunication.Stages.LayoutSent.Transmitter transmitter_)
                    {
                        #region> Info OnSent Event
                        //🌭<
                        OnSent_via_ObserverCommunication_at_Start.notify(this, context, transmitter_);
                        //🌭/>
                        #endregion> ĀĔāÿĔ.OnSentEvent
                    }

                    public class OnSent_via_ObserverCommunication_at_Start
                    {
                        public static void notify(Info pack, ObserverCommunication.Context context, ObserverCommunication.Stages.LayoutSent.Transmitter transmitter_) => handlers?.Invoke(pack, context, transmitter_);
                        public static event Action<Info, ObserverCommunication.Context, ObserverCommunication.Stages.LayoutSent.Transmitter> handlers;
                    }

                    public void __OnReceived_via_SaveLayout_at_Start(SaveLayout.Context context, SaveLayout.Stages.Start.Transmitter transmitter_)
                    {
                        #region> Info OnReceived Event
                        //🌭<
                        OnReceived_via_SaveLayout_at_Start.notify(this, context, transmitter_);
                        //🌭/>
                        #endregion> ĀĔĀÿĔ.OnReceivedEvent
                    }

                    public class OnReceived_via_SaveLayout_at_Start
                    {
                        public static void notify(Info pack, SaveLayout.Context context, SaveLayout.Stages.Start.Transmitter transmitter_) => handlers?.Invoke(pack, context, transmitter_);
                        public static event Action<Info, SaveLayout.Context, SaveLayout.Stages.Start.Transmitter> handlers;
                    }

                    public void __OnSent_via_SaveLayout_at_Start(SaveLayout.Context context, SaveLayout.Stages.Start.Transmitter transmitter_)
                    {
                        #region> Info OnSent Event
                        //🌭<
                        OnSent_via_SaveLayout_at_Start.notify(this, context, transmitter_);
                        //🌭/>
                        #endregion> ĀĔĀÿĔ.OnSentEvent
                    }

                    public class OnSent_via_SaveLayout_at_Start
                    {
                        public static void notify(Info pack, SaveLayout.Context context, SaveLayout.Stages.Start.Transmitter transmitter_) => handlers?.Invoke(pack, context, transmitter_);
                        public static event Action<Info, SaveLayout.Context, SaveLayout.Stages.Start.Transmitter> handlers;
                    }

                    public partial class View { }

                    public partial struct XY { }
                }

                ///<summary>
                ///Maps the persistent UIDs of protocol entities (hosts, packs, etc.) to their layout keys.
                ///This ensures that diagram positions are preserved across sessions, even if volatile internal IDs change.
                ///</summary>
                public partial class UID
                {
                    public void __OnReceived_via_SaveLayout_at_Start(SaveLayout.Context context, SaveLayout.Stages.Start.Transmitter transmitter_)
                    {
                        #region> UID OnReceived Event
                        //🌭<
                        OnReceived_via_SaveLayout_at_Start.notify(this, context, transmitter_);
                        //🌭/>
                        #endregion> ĀēĀÿē.OnReceivedEvent
                    }

                    public class OnReceived_via_SaveLayout_at_Start
                    {
                        public static void notify(UID pack, SaveLayout.Context context, SaveLayout.Stages.Start.Transmitter transmitter_) => handlers?.Invoke(pack, context, transmitter_);
                        public static event Action<UID, SaveLayout.Context, SaveLayout.Stages.Start.Transmitter> handlers;
                    }

                    public void __OnSent_via_SaveLayout_at_Start(SaveLayout.Context context, SaveLayout.Stages.Start.Transmitter transmitter_)
                    {
                        #region> UID OnSent Event
                        //🌭<
                        OnSent_via_SaveLayout_at_Start.notify(this, context, transmitter_);
                        //🌭/>
                        #endregion> ĀēĀÿē.OnSentEvent
                    }

                    public class OnSent_via_SaveLayout_at_Start
                    {
                        public static void notify(UID pack, SaveLayout.Context context, SaveLayout.Stages.Start.Transmitter transmitter_) => handlers?.Invoke(pack, context, transmitter_);
                        public static event Action<UID, SaveLayout.Context, SaveLayout.Stages.Start.Transmitter> handlers;
                    }
                }
            }

            ///<summary>
            ///Defines the Observer host, representing the browser-based visualizer tool.
            ///It requests project data from the Agent and sends UI interaction commands back.
            ///</summary>
            public static partial class Observer_
            {
                public partial struct Show_Code
                {
                    public class OnReceived_via_ObserverCommunication_at_Operate
                    {
                        public static void notify(Show_Code pack, ObserverCommunication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Show_Code, ObserverCommunication.Context> handlers;
                    }
                }

                ///<summary>
                ///A request from the Observer to check if its data is stale. The Agent will respond either
                ///with an updated `Project` pack or with this same pack to confirm it's already up-to-date.
                ///</summary>
                public partial class Up_to_date
                {
                    public void __OnReceived_via_ObserverCommunication_at_Operate(ObserverCommunication.Context context, ObserverCommunication.Stages.RefreshProject.Transmitter transmitter_)
                    {
                        #region> Up_to_date OnReceived Event
                        //🌭<
                        OnReceived_via_ObserverCommunication_at_Operate.notify(this, context, transmitter_);
                        //🌭/>
                        #endregion> Āđāāđ.OnReceivedEvent
                    }

                    public class OnReceived_via_ObserverCommunication_at_Operate
                    {
                        public static void notify(Up_to_date pack, ObserverCommunication.Context context, ObserverCommunication.Stages.RefreshProject.Transmitter transmitter_) => handlers?.Invoke(pack, context, transmitter_);
                        public static event Action<Up_to_date, ObserverCommunication.Context, ObserverCommunication.Stages.RefreshProject.Transmitter> handlers;
                    }

                    public void __OnSent_via_ObserverCommunication_at_RefreshProject(ObserverCommunication.Context context)
                    {
                        #region> Up_to_date OnSent Event
                        //🌭<
                        OnSent_via_ObserverCommunication_at_RefreshProject.notify(this, context);
                        //🌭/>
                        #endregion> ĀđāĂđ.OnSentEvent
                    }

                    public class OnSent_via_ObserverCommunication_at_RefreshProject
                    {
                        public static void notify(Up_to_date pack, ObserverCommunication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Up_to_date, ObserverCommunication.Context> handlers;
                    }
                }
            }

            ///<summary>
            ///Defines the Server host. It is responsible for receiving protocol descriptions,
            ///generating source code, and sending back the results or any errors.
            ///The packs nested within this host define the messages it can send or receive.
            ///</summary>
            public static partial class Server_
            {
                ///<summary>
                ///A generic informational or error message pack sent from the Server to the Agent.
                ///</summary>
                public partial class Info
                {
                    public void __OnReceived_via_Communication_at_VersionMatching(Communication.Context context)
                    {
                        #region> Info OnReceived Event
                        //🌭<
                        OnReceived_via_Communication_at_VersionMatching.notify(this, context);
                        //🌭/>
                        #endregion> ĀĄÿĀĄ.OnReceivedEvent
                    }

                    public class OnReceived_via_Communication_at_VersionMatching
                    {
                        public static void notify(Info pack, Communication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Info, Communication.Context> handlers;
                    }

                    public void __OnReceived_via_Communication_at_LoginResponse(Communication.Context context)
                    {
                        #region> Info OnReceived Event
                        //🌭<
                        OnReceived_via_Communication_at_LoginResponse.notify(this, context);
                        //🌭/>
                        #endregion> ĀĄÿĂĄ.OnReceivedEvent
                    }

                    public class OnReceived_via_Communication_at_LoginResponse
                    {
                        public static void notify(Info pack, Communication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Info, Communication.Context> handlers;
                    }

                    public void __OnReceived_via_Communication_at_Project(Communication.Context context)
                    {
                        #region> Info OnReceived Event
                        //🌭<
                        OnReceived_via_Communication_at_Project.notify(this, context);
                        //🌭/>
                        #endregion> ĀĄÿĄĄ.OnReceivedEvent
                    }

                    public class OnReceived_via_Communication_at_Project
                    {
                        public static void notify(Info pack, Communication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Info, Communication.Context> handlers;
                    }

                    public void __OnReceived_via_Communication_at_Proto(Communication.Context context)
                    {
                        #region> Info OnReceived Event
                        //🌭<
                        OnReceived_via_Communication_at_Proto.notify(this, context);
                        //🌭/>
                        #endregion> ĀĄÿąĄ.OnReceivedEvent
                    }

                    public class OnReceived_via_Communication_at_Proto
                    {
                        public static void notify(Info pack, Communication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Info, Communication.Context> handlers;
                    }
                }

                ///<summary>
                ///An empty pack sent by the Server to invite the Agent to the next communication stage (e.g., proceed to login).
                ///Empty packs are implemented as highly efficient singletons, making them ideal for signaling state transitions.
                ///</summary>
                public static partial class Invitation
                {
                    public const int __id_ = 3;

                    public class Handler : AdHoc.Channel.Receiver.BytesDst
                    {
                        public int __id => __id_;
                        public static readonly Handler ONE = new Handler();

                        bool AdHoc.Channel.Receiver.BytesDst.__put_bytes(AdHoc.Channel.Receiver __src) => true;
                    }

                    public class OnReceived_via_Communication_at_VersionMatching
                    {
                        public static void notify(Communication.Context context, Communication.Stages.Login.Transmitter transmitter_) => handlers?.Invoke(context, transmitter_);
                        public static event Action<Communication.Context, Communication.Stages.Login.Transmitter> handlers;
                    }

                    public class OnReceived_via_Communication_at_LoginResponse
                    {
                        public static void notify(Communication.Context context, Communication.Stages.TodoJobRequest.Transmitter transmitter_) => handlers?.Invoke(context, transmitter_);
                        public static event Action<Communication.Context, Communication.Stages.TodoJobRequest.Transmitter> handlers;
                    }
                }

                ///<summary>
                ///Sent by the Server after a successful login to provide the Agent with a new, temporary (volatile) UUID for the session.
                ///The 128-bit UUID is split into two `ulong` fields. Its volatile nature prevents reuse and supports automated
                ///CI/CD workflows, as the new UUID is automatically stored in the `AdHocAgent.toml` config file.
                ///</summary>
                public partial class InvitationUpdate
                {
                    public void __OnReceived_via_Communication_at_LoginResponse(Communication.Context context, Communication.Stages.TodoJobRequest.Transmitter transmitter_)
                    {
                        #region> InvitationUpdate OnReceived Event
                        //🌭<
                        OnReceived_via_Communication_at_LoginResponse.notify(this, context, transmitter_);
                        //🌭/>
                        #endregion> ĀăÿĂă.OnReceivedEvent
                    }

                    public class OnReceived_via_Communication_at_LoginResponse
                    {
                        public static void notify(InvitationUpdate pack, Communication.Context context, Communication.Stages.TodoJobRequest.Transmitter transmitter_) => handlers?.Invoke(pack, context, transmitter_);
                        public static event Action<InvitationUpdate, Communication.Context, Communication.Stages.TodoJobRequest.Transmitter> handlers;
                    }
                }

                ///<summary>
                ///Contains the final result of a code generation task, sent from the Server to the Agent.
                ///</summary>
                public partial class Result
                {
                    public void __OnReceived_via_Communication_at_Project(Communication.Context context)
                    {
                        #region> Result OnReceived Event
                        //🌭<
                        OnReceived_via_Communication_at_Project.notify(this, context);
                        //🌭/>
                        #endregion> ĀąÿĄą.OnReceivedEvent
                    }

                    public class OnReceived_via_Communication_at_Project
                    {
                        public static void notify(Result pack, Communication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Result, Communication.Context> handlers;
                    }

                    public void __OnReceived_via_Communication_at_Proto(Communication.Context context)
                    {
                        #region> Result OnReceived Event
                        //🌭<
                        OnReceived_via_Communication_at_Proto.notify(this, context);
                        //🌭/>
                        #endregion> Āąÿąą.OnReceivedEvent
                    }

                    public class OnReceived_via_Communication_at_Proto
                    {
                        public static void notify(Result pack, Communication.Context context) => handlers?.Invoke(pack, context);
                        public static event Action<Result, Communication.Context> handlers;
                    }
                }
            }
        }
    }

    public class _Allocator
    {
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Agent_.Project.Channel.Stage.Branch> new_AdHocProtocol_Agent_Project_Channel_Stage_Branch = (srs) => throw new Exception("The producer of Agent.AdHocProtocol.Agent_.Project.Channel.Stage.Branch is not assigned");
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Agent_.Project.Channel> new_AdHocProtocol_Agent_Project_Channel = (srs) => throw new Exception("The producer of Agent.AdHocProtocol.Agent_.Project.Channel is not assigned");
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Agent_.Project.Host.Pack.Constant> new_AdHocProtocol_Agent_Project_Host_Pack_Constant = (srs) => throw new Exception("The producer of Agent.AdHocProtocol.Agent_.Project.Host.Pack.Constant is not assigned");
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Agent_.Project.Host.Pack.Field> new_AdHocProtocol_Agent_Project_Host_Pack_Field = (srs) => throw new Exception("The producer of Agent.AdHocProtocol.Agent_.Project.Host.Pack.Field is not assigned");
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Agent_.Project.Host> new_AdHocProtocol_Agent_Project_Host = (srs) => throw new Exception("The producer of Agent.AdHocProtocol.Agent_.Project.Host is not assigned");
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Server_.Info> new_AdHocProtocol_Server_Info = (srs) => new Agent.AdHocProtocol.Server_.Info();
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.LayoutFile_.Info> new_AdHocProtocol_LayoutFile_Info = (srs) => new Agent.AdHocProtocol.LayoutFile_.Info();
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Server_.Invitation.Handler> new_AdHocProtocol_Server_Invitation = (srs) => Agent.AdHocProtocol.Server_.Invitation.Handler.ONE;
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Server_.InvitationUpdate> new_AdHocProtocol_Server_InvitationUpdate = (srs) => new Agent.AdHocProtocol.Server_.InvitationUpdate();
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Agent_.Login> new_AdHocProtocol_Agent_Login = (srs) => new Agent.AdHocProtocol.Agent_.Login();
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Agent_.Project.Host.Pack> new_AdHocProtocol_Agent_Project_Host_Pack = (srs) => throw new Exception("The producer of Agent.AdHocProtocol.Agent_.Project.Host.Pack is not assigned");
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Agent_.Project> new_AdHocProtocol_Agent_Project = (srs) => throw new Exception("The producer of Agent.AdHocProtocol.Agent_.Project is not assigned");
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Agent_.Proto> new_AdHocProtocol_Agent_Proto = (srs) => new Agent.AdHocProtocol.Agent_.Proto();
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Server_.Result> new_AdHocProtocol_Server_Result = (srs) => new Agent.AdHocProtocol.Server_.Result();
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Observer_.Show_Code.Handler> new_AdHocProtocol_Observer_Show_Code = (srs) => Agent.AdHocProtocol.Observer_.Show_Code.Handler.ONE;
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Agent_.Project.Channel.Stage> new_AdHocProtocol_Agent_Project_Channel_Stage = (srs) => throw new Exception("The producer of Agent.AdHocProtocol.Agent_.Project.Channel.Stage is not assigned");
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.LayoutFile_.UID> new_AdHocProtocol_LayoutFile_UID = (srs) => new Agent.AdHocProtocol.LayoutFile_.UID();
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Observer_.Up_to_date> new_AdHocProtocol_Observer_Up_to_date = (srs) => new Agent.AdHocProtocol.Observer_.Up_to_date();
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.Agent_.Version.Handler> new_AdHocProtocol_Agent_Version = (srs) => Agent.AdHocProtocol.Agent_.Version.Handler.ONE;
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.LayoutFile_.Info.View> new_AdHocProtocol_LayoutFile_Info_View = (srs) => new Agent.AdHocProtocol.LayoutFile_.Info.View();
        public Func<AdHoc.Channel.Receiver, Agent.AdHocProtocol.LayoutFile_.Info.XY.Handler> new_AdHocProtocol_LayoutFile_Info_XY = (srs) => Agent.AdHocProtocol.LayoutFile_.Info.XY.Handler.ONE;

        public _Allocator CopyTo(_Allocator dst)
        {
            dst.new_AdHocProtocol_Agent_Project_Channel_Stage_Branch = new_AdHocProtocol_Agent_Project_Channel_Stage_Branch;
            dst.new_AdHocProtocol_Agent_Project_Channel = new_AdHocProtocol_Agent_Project_Channel;
            dst.new_AdHocProtocol_Agent_Project_Host_Pack_Constant = new_AdHocProtocol_Agent_Project_Host_Pack_Constant;
            dst.new_AdHocProtocol_Agent_Project_Host_Pack_Field = new_AdHocProtocol_Agent_Project_Host_Pack_Field;
            dst.new_AdHocProtocol_Agent_Project_Host = new_AdHocProtocol_Agent_Project_Host;
            dst.new_AdHocProtocol_Server_Info = new_AdHocProtocol_Server_Info;
            dst.new_AdHocProtocol_LayoutFile_Info = new_AdHocProtocol_LayoutFile_Info;
            dst.new_AdHocProtocol_Server_Invitation = new_AdHocProtocol_Server_Invitation;
            dst.new_AdHocProtocol_Server_InvitationUpdate = new_AdHocProtocol_Server_InvitationUpdate;
            dst.new_AdHocProtocol_Agent_Login = new_AdHocProtocol_Agent_Login;
            dst.new_AdHocProtocol_Agent_Project_Host_Pack = new_AdHocProtocol_Agent_Project_Host_Pack;
            dst.new_AdHocProtocol_Agent_Project = new_AdHocProtocol_Agent_Project;
            dst.new_AdHocProtocol_Agent_Proto = new_AdHocProtocol_Agent_Proto;
            dst.new_AdHocProtocol_Server_Result = new_AdHocProtocol_Server_Result;
            dst.new_AdHocProtocol_Observer_Show_Code = new_AdHocProtocol_Observer_Show_Code;
            dst.new_AdHocProtocol_Agent_Project_Channel_Stage = new_AdHocProtocol_Agent_Project_Channel_Stage;
            dst.new_AdHocProtocol_LayoutFile_UID = new_AdHocProtocol_LayoutFile_UID;
            dst.new_AdHocProtocol_Observer_Up_to_date = new_AdHocProtocol_Observer_Up_to_date;
            dst.new_AdHocProtocol_Agent_Version = new_AdHocProtocol_Agent_Version;
            dst.new_AdHocProtocol_LayoutFile_Info_View = new_AdHocProtocol_LayoutFile_Info_View;
            dst.new_AdHocProtocol_LayoutFile_Info_XY = new_AdHocProtocol_LayoutFile_Info_XY;

            return dst;
        }

        public static readonly _Allocator DEFAULT = new();
    }
}