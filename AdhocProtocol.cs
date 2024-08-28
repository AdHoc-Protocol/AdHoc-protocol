//  MIT License
//
//  Copyright © 2020 Chikirev Sirguy, Unirail Group. All rights reserved.
//  For inquiries, please contact:  al8v5C6HU4UtqE9@gmail.com
//  GitHub Repository: https://github.com/AdHoc-Protocol
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to use,
//  copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
//  the Software, and to permit others to do so, under the following conditions:
//
//  1. The above copyright notice and this permission notice must be included in all
//     copies or substantial portions of the Software.
//
//  2. Users of the Software must provide a clear acknowledgment in their user
//     documentation or other materials that their solution includes or is based on
//     this Software. This acknowledgment should be prominent and easily visible,
//     and can be formatted as follows:
//     "This product includes software developed by Chikirev Sirguy and the Unirail Group
//     (https://github.com/AdHoc-Protocol)."
//
//  3. If you modify the Software and distribute it, you must include a prominent notice
//     stating that you have changed the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT, OR OTHERWISE, ARISING FROM,
//  OUT OF, OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using System;
using org.unirail.Meta;

namespace org.unirail{
    /**
     * AdHoc agent protocol description
     */
    /**
        <see cref = 'Agent.Project'          id = '5'/>
        <see cref = 'Agent.Login'            id = '8'/>
        <see cref = 'Agent.Proto'            id = '7'/>
        <see cref = 'Agent.RequestResult'    id = '6'/>
        <see cref = 'Agent.Signup'           id = '12'/>
        <see cref = 'Agent.Version'          id = '13'/>
        <see cref = 'Observer.Layout'        id = '9'/>
        <see cref = 'Observer.Show_Code'     id = '11'/>
        <see cref = 'Observer.Up_to_date'    id = '10'/>
        <see cref = 'Server.Info'            id = '0'/>
        <see cref = 'Server.Invitation'      id = '3'/>
        <see cref = 'Server.Result'          id = '1'/>
    */
    public interface AdhocProtocol :
        _<
            AdhocProtocol.Agent.Project.Host.Pack.Field.DataType, //propagate DataType constants set to all hosts
            AdhocProtocol.Agent.Project.Channel.Stage.Type
        >{
        class max_65_000_chars{
            [D(+65_000)] string TYPEDEF; // Maximum 65,000 characters
        }

        class max_1_000_chars{
            [D(+1_000)] string TYPEDEF; // Maximum 1,000 characters
        }

        class Root{
            string           name;
            max_65_000_chars doc; // Documentation with a maximum of 65,000 characters
            string           inline_doc;
        }


        /**
        <see cref = 'InJAVA'/> following packs generate in JAVA with full implementation
        <see cref = 'Result'/>
        <see cref = 'Agent.Proto'/>
        <see cref = 'Agent.Login'/>
        <see cref = 'Agent.Version'/>
        <see cref = 'Agent.Signup'/>
        <see cref = 'Server.Invitation'/>
        <see cref = 'Agent.Project.Channel.Stage.Branch'/>
        <see cref = 'InJAVA'/>-- by default rest of the packs in JAVA generate abstracted (without implementation)
        */
        struct Server /*ā*/ : Host{
            public class Invitation /*Ā*/{
                public ulong uid; // Unique identifier for the invitation
            }

            public class Info /*ā*/{
                string           task;
                max_65_000_chars info; // Information with a maximum of 65,000 characters
            }

            public class Result /*ć*/{
                string                    task;
                [D(3_000_0000)] Binary[,] result; //3 megabytes compressed binary
                max_65_000_chars          info;   // Information with a maximum of 65,000 characters
            }
        }


        /**
            <see cref = 'InCS'/> following packs generate in C# with full implementation
            <see cref = 'Server.Info'/>
            <see cref = 'Server.Result'/>
            <see cref = 'RequestResult'/>
            <see cref = 'Version'/>
            <see cref = 'Signup'/>
            <see cref = 'Login'/>
            <see cref = 'Proto'/>
            <see cref = 'Observer.Up_to_date'/>
            <see cref = 'Observer.Show_Code'/>
            <see cref = 'InCS'/>-- by default rest of the packs in C#  generate abstracted (without implementation)
         */
        struct Agent /*ÿ*/ : Host{
            public class Project /*ą*/ : Root{
                string                  task; // Unique ID
                string                  namespacE;
                long                    time;
                [D(0x1_FFFF)] Binary[,] source; // Max 130k zipped sources

#region FIXED ORDER
                [D(0xFFFF)] Host.Pack.Field[,] fields;
                [D(0xFFFF)] Host.Pack[,]       packs;
                [D(0x1FF)]  Host[,]            hosts;    //512 max
                [D(0xFF)]   Channel[,]         channels; // 512/2 = 256 max
#endregion

                public class Host : Root{
                    ushort uid;   // Layout ID
                    Langs  langs; // Languages set to generate source code. A bit-per language.

                    /**
                        Value:  16 Least Significant Bits - hash_equal info
                                16 Most  Significant Bits - impl info
                     */
                    Map<ushort, uint> pack_impl_hash_equal; // Pack -> impl_hash_equal

                    /**
                        16 Least Significant Bits - hash_equal info
                        16 Most  Significant Bits - impl info
                     */
                    uint default_impl_hash_equal;

                    Map<ushort, Langs> field_impl; // Field -> impl

                    [D(65_000)] ushort[,] packs; // Local constants or enums, referred to as packs, are declared within the scope of the current host.

                    [Flags]
                    public enum Langs : ushort{
                        InCPP,
                        InRS,
                        InCS,
                        InJAVA,
                        InGO,
                        InTS,
                        All = 0xFFFF
                    }

                    public class Pack : Root{
                        ushort  id;     // Pack's identifier
                        ushort? parent; // Identifier of the parent pack
                        ushort  uid;    // Layout identifier

                        ushort? nested_max; // Maximum cyclic depth
                        bool    referred;   // Indicator of being referred

                        [D(65_000)] int[,] fields;
                        [D(65_000)] int[,] static_fields;

                        public class Field : Root{
                            [D(32)] int[,] dims; // Dimensions

                            uint? map_set_len;
                            uint? map_set_array;

                            ushort exT;
                            uint?  exT_len;
                            uint?  exT_array;

                            ushort? inT;

                            long?                  min_value;
                            long?                  max_value;
                            [MinMax(-1, 1)] sbyte? dir;

                            double? min_valueD;
                            double? max_valueD;

                            [MinMax(1, 7)] byte? bits;       // Can store/transfer a value in less than 7 bits
                            byte?                null_value; // Value that substitutes NULL for bits-field or 255 if field is nullable primitive
#region Map V params
                            ushort? exTV;
                            uint?   exTV_len; // If exTV is string - max chars
                            uint?   exTV_array;

                            ushort?                inTV;
                            long?                  min_valueV;
                            long?                  max_valueV;
                            [MinMax(-1, 1)] sbyte? dirV;

                            double? min_valueDV;
                            double? max_valueDV;

                            [MinMax(1, 7)] byte? bitsV;       // Can store/transfer a value in less than 7 bits
                            byte?                null_valueV; // Value that substitutes NULL for bits-field or 255 if nullable primitive
#endregion

                            long?                       value_int;    // Constant value
                            double?                     value_double; // Constant value
                            max_1_000_chars             value_string; // Constant value
                            [D(255)] max_1_000_chars[,] array;        // Constant array values

                            public enum DataType{
                                t_constants = 65535,
                                t_enum_sw   = 65534,
                                t_enum_exp  = 65533,
                                t_flags     = 65532,
                                t_bool      = 65531,
                                t_int8      = 65530,
                                t_binary    = 65529,
                                t_uint8     = 65528,
                                t_int16     = 65527,
                                t_uint16    = 65526,
                                t_char      = 65525,
                                t_int32     = 65524,
                                t_uint32    = 65523,
                                t_int64     = 65522,
                                t_uint64    = 65521,
                                t_float     = 65520,
                                t_double    = 65519,
                                t_string    = 65518,
                                t_map       = 65517,
                                t_set       = 65516,
                                t_subpack   = 65514,
                            }
                        }
                    }
                }

                public class Channel : Root{
                    ushort                hostL;
                    [D(0xFFFF)] ushort[,] hostL_transmitting_packs;
                    [D(0xFFFF)] ushort[,] hostL_related_packs;

                    ushort                hostR;
                    [D(0xFFFF)] ushort[,] hostR_transmitting_packs;
                    [D(0xFFFF)] ushort[,] hostR_related_packs;

                    [D(0xFFF)] Stage[,] stages;

                    ushort uid; // Layout ID

                    public class Stage : Root{
                        ushort timeout;
                        ushort uid; // Layout ID

                        [D(0xFFF)] Branch[,] branchesL;
                        [D(0xFFF)] Branch[,] branchesR;

                        public class Branch{
                            ushort uid; // Layout ID
                            string doc;
                            ushort goto_stage; // Target stage

                            [D(0xFFFF)] ushort[,] packs;
                        }

                        public enum Type : ushort{
                            Exit      = ushort.MaxValue,
                            None      = ushort.MaxValue - 1,
                            Remaining = ushort.MaxValue - 2,
                        }
                    }
                }
            }

            public class RequestResult /*Ą*/ // The request for pending task result
            {
                string task;
            }

            public class Signup /*ă*/{
                [D(50)] Binary[,] oauth; // OAuth token with a maximum of 50 characters
            }

            public class Login /*Ă*/{
                public ulong uid; // Unique identifier for the login
            }

            /*
             The first pack negotiates versions
            */
            public class Version /*ÿ*/{
                public uint uid; // Unique identifier for the version
            }


            public class Proto /*Ć*/{
                string                 task; // Unique ID
                string                 name;
                [D(512_000)] Binary[,] proto; // Max 65k zipped
            }
        }

        class Entity{
            Type tYpe;

            public enum Type : byte{
                Project,
                Host,
                Pack,
                Field,
                Channel,
                Stage,
            }

            ushort idx;
        }

        /**
        <see cref = 'InTS'/>
        */
        struct Observer /*Ā*/ : Host{
            public class Layout /*Ĉ*/{
                byte split;
                View host_packs;
                View pack_fields;

                class View{
                    int   X; // Viewer parameters
                    int   Y;
                    float zoom;

                    Map<uint, XY> id2xy; // Fast access to info by ID
                }

                public class XY{
                    int x;
                    int y;
                }

                Map<uint, XY> channels_id2xy;
            }

            public class Up_to_date /*Ċ*/ // Request to send updated Project pack or Up_to_date if data is not changed
            {
                max_65_000_chars info; // Can be an updating error description
            }

            //JetBrains Rider
            //https://www.jetbrains.com/help/rider/Opening_Files_from_Command_Line.html

            //VS Code
            // https://code.visualstudio.com/docs/editor/command-line#_launching-from-command-line
            //-g or --goto	When used with file:line{:character}, opens a file at a specific line and optional character position.
            //This argument is provided since some operating systems permit : in a file name.
            public class Show_Code /*ĉ*/ : Entity{ } // Request to show entity in editor
        }

        interface Communication /*ÿ*/ : ChannelFor<Agent, Server>{
            interface Info_Result : // Packs set
                _<
                    Server.Info,
                    Server.Result
                >{ }

            [Timeout(12)]
            interface Start /*ÿ*/ : L,
                                    _< /*ÿ*/
                                        Agent.Version,
                                        VersionMatching
                                    >{ }

            interface VersionMatching /*Ā*/ : R,
                                              _<                     /*Ā*/
                                                  Server.Invitation, // Version OK invite to login
                                                  Login
                                              >,
                                              _<               /*ā*/
                                                  Server.Info, // Version not match. Replay the problem info
                                                  Exit
                                              >{ }

            interface Login /*ā*/ : L,
                                    _< /*Ă*/
                                        Agent.Login,
                                        Agent.Signup,
                                        LoginResponse
                                    >{ }

            [Timeout(12)]
            interface LoginResponse /*Ă*/ : R,
                                            _<                     /*ă*/
                                                Server.Invitation, // Login OK
                                                TodoJobRequest
                                            >,
                                            _<               /*Ą*/
                                                Server.Info, // Login expire/wrong ...etc.
                                                Exit
                                            >{ }

            [Timeout(12)]
            interface TodoJobRequest /*ă*/ : L,
                                             _< /*ą*/
                                                 Agent.RequestResult,
                                                 Agent.Project,
                                                 Project
                                             >,
                                             _< /*Ć*/
                                                 Agent.Proto,
                                                 Proto
                                             >{ }

            interface Project /*Ą*/ : R,
                                      _< /*ć*/
                                          Info_Result,
                                          Exit
                                      >{ }

            interface Proto /*ą*/ : R,
                                    _< /*Ĉ*/
                                        Info_Result,
                                        Exit
                                    >{ }
        }

        interface ObserverCommunication /*Ā*/ : ChannelFor<Agent, Observer>{
            interface Start /*Ć*/ : L,
                                    _< /*ÿ*/
                                        Observer.Layout,
                                        Layout
                                    >, _< /*Ā*/
                                        Agent.Project,
                                        Operate
                                    >{ }

            interface Layout /*ć*/ : L,
                                     _< /*ā*/
                                         Agent.Project,
                                         Operate
                                     >{ }

            interface Operate /*Ĉ*/ : R,
                                      _< /*Ă*/
                                          Observer.Show_Code,
                                          Observer.Layout
                                      >,
                                      _< /*ă*/
                                          Observer.Up_to_date,
                                          RefreshProject
                                      >{ }

            interface RefreshProject /*ĉ*/ : L,
                                             _< /*Ą*/
                                                 Agent.Project,
                                                 Observer.Up_to_date,
                                                 Operate
                                             >{ }
        }
    }
}