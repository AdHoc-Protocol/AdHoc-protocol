using System;
using org.unirail.Meta;

namespace org.unirail
{
    /**
     * AdHoc agent protocol description 
     */
    /**
		<see cref = 'Agent.Project'                   id = '5'/>
		<see cref = 'Agent.ToServer.Login'            id = '8'/>
		<see cref = 'Agent.ToServer.Proto'            id = '7'/>
		<see cref = 'Agent.ToServer.RequestResult'    id = '6'/>
		<see cref = 'Observer.Layout'                 id = '9'/>
		<see cref = 'Observer.ToAgent.Show_Code'      id = '11'/>
		<see cref = 'Observer.ToAgent.Up_to_date'     id = '10'/>
		<see cref = 'Server.ToAgent.Busy'             id = '4'/>
		<see cref = 'Server.ToAgent.Info'             id = '0'/>
		<see cref = 'Server.ToAgent.LoginRejected'    id = '2'/>
		<see cref = 'Server.ToAgent.Result'           id = '1'/>
		<see cref = 'Upload'                          id = '3'/>
	*/
    public interface AdhocProtocol : _<AdhocProtocol.Agent.Project.Host.Port.Pack.Field.DataType>//propagate DataType constants set to all hosts
    {
        class Upload { }

        class Root
        {
            string name;
            string doc;
            string inline_doc;
        }

        /** 
        <see cref = 'InJAVA'/>
        <see cref = 'ToAgent.Result'/> 
        <see cref = 'Agent.ToServer.Proto'/>
        <see cref = 'Agent.ToServer.Login'/>  
        <see cref = 'InJAVA'/>--
        */
        struct Server : Host
        {
            /**
		     * public port ToAgent
		     */
            public interface ToAgent : _<Upload>
            {
                class LoginRejected { }

                class Busy
                {
                    string task;
                    string info;
                    long   timout;
                }

                class Info
                {
                    string task;
                    string info;
                }

                class Result
                {
                    string                 task;
                    [Dims(~650000)] Binary result;
                }
            }
        }

        /**
            <see cref = 'InCS'/> following packs generate in C# with implementation

            <see cref = 'Server.ToAgent.Info'/>  
            <see cref = 'Server.ToAgent.Result'/>
            <see cref = 'Server.ToAgent.LoginRejected'/> 
            <see cref = 'Server.ToAgent.Busy'/>
            <see cref = 'ToServer.RequestResult'/> 
            <see cref = 'ToServer.Login'/>
            <see cref = 'ToServer.Proto'/>
            <see cref = 'Observer.ToAgent.Up_to_date'/>
            <see cref = 'Observer.ToAgent.Show_Code'/>

            <see cref = 'InCS'/>-- by default rest of the packs in C#  generate abstracted (without implementation)
         */
        struct Agent : Host
        {
            public class Project : Root
            {
                string task; //unique id 
                string namespace_;
                long   time;

#region FIXED ORDER
                [Dims(~65000)] Host.Port.Pack.Field fields;
                [Dims(~65000)] Host.Port.Pack       packs;
                [Dims(~65000)] Host                 hosts;
                [Dims(~65000)] Host.Port            ports;
                [Dims(~65000)] Channel              channels;
#endregion

                public class Host : Root
                {
                    Langs langs; //languages set to generate source code. a-bit-per language.

                    /**
                        value:  16 Least Significant Bits - hash_equal info
                                16 Most  Significant Bits - impl info
                     */
                    Map<ushort, uint> pack_impl_hash_equal; //pack -> impl_hash_equal 

                    /**
                        16 Least Significant Bits - hash_equal info
                        16 Most  Significant Bits - impl info 
                     */
                    uint default_impl_hash_equal;

                    Map<ushort, Langs> field_impl; // field -> impl

                    [Dims(~65000)] ushort packs; //only this-host-scope packs

                    [Flags]
                    public enum Langs : ushort
                    {
                        InCPP,
                        InRS,
                        InCS,
                        InJAVA,
                        InGO,
                        InTS,
                        All = 0xFFFF
                    }

                    public class Port : Root
                    {
                        ushort                host;
                        [Dims(~65000)] ushort transmitted_packs; //packs directly transmitted through  port   
                        [Dims(~65000)] ushort related_packs;     //packs indirectly transmitted by host  

                        public class Pack : Root
                        {
                            ushort  id;
                            ushort? parent; //parent pack  

                            ushort? nested_max; //cyclic depth
                            bool?   referred;

                            [Dims(~65000)] int fields;
                            [Dims(~65000)] int static_fields;

                            bool value_type; //marked with Meta.Value pack, with (1 < boolean/number fields) and data size fit to 64 bits

                            public class Field : Root
                            {
                                [Dims(~65000)] int dims;

                                ushort  Ext;
                                ushort? Int;

                                long?                  min_value;
                                long?                  max_value;
                                [MinMax(-1, 1)] sbyte? dir;

                                double? min_valueD;
                                double? max_valueD;

                                [MinMax(1, 7)] byte? bits;       // can store/transfer a value in less then 7 bits
                                byte?                null_value; // value that substitute NULL for bits-field's or 255 if fiels is nullable primitive

#region Map V params
                                ushort? ExtV;
                                ushort? IntV;

                                long?                  min_valueV;
                                long?                  max_valueV;
                                [MinMax(-1, 1)] sbyte? dirV;

                                double? min_valueDV;
                                double? max_valueDV;

                                [MinMax(1, 7)] byte? bitsV;       // can store/transfer a value in less then 7 bits
                                byte?                null_valueV; // value that substitute NULL for bits-field's or 255 if nullable primitive
#endregion

                                long?   value_int;    //constant value 
                                double? value_double; //constant value
                                string  value_string; //constant value

                                bool the_set;

                                [Dims(~65000)] string array; //constant array values

                                public enum DataType
                                {
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
                }

                public class Channel : Root
                {
                    ushort portA;
                    ushort portB;
                }
            }

            public interface ToServer : _<Project>
            {
                public class RequestResult //the request for pending task result
                {
                    string task;
                }

                public class Login
                {
                    string client;
                }

                public class Proto
                {
                    string                 task; //unique id
                    string                 name;
                    [Dims(~650000)] Binary proto;
                }
            }

            public interface ToObserver : _<Project>,
                                          _<Observer.Layout>,
                                          _<Observer.ToAgent.Up_to_date> //reply  on Up_to_date, if data is not changed
            { }
        }

        class Entity
        {
            Type type;

            public enum Type : byte
            {
                Project,
                Host,
                Port,
                Pack,
                Field,
                Channel,
            }

            uint uid;
        }

        /**
        <see cref = 'InTS'/> 
        */
        struct Observer : Host
        {
            public class Layout
            {
                byte split;
                View host_packs;
                View pack_fields;

                class View
                {
                    int    X; //viewer itself params
                    int    Y;
                    double zoom;

                    Map<uint, Info> id2info; //need to get info by id fast

                    public class Info 
                    {
                        int x;
                        int y;
                    }
                }
            }

            public interface ToAgent : _<Layout>
            {
                class Up_to_date //request to send updated Project pack or Up_to_date  if data is not changed
                {
                    string info; //can be an updating error description 
                }

                //JetBrains Rider
                //https://www.jetbrains.com/help/rider/Opening_Files_from_Command_Line.html

                //VS Code
                // https://code.visualstudio.com/docs/editor/command-line#_launching-from-command-line
                //-g or --goto	When used with file:line{:character}, opens a file at a specific line and optional character position.
                //This argument is provided since some operating systems permit : in a file name.
                class Show_Code : Entity { } //request to show entity in editor
            }
        }

        interface Communication : Communication_Channel_Of<Agent.ToServer, Server.ToAgent> { }

        interface ObserverCommunication : Communication_Channel_Of<Agent.ToObserver, Observer.ToAgent> { }
    }
}