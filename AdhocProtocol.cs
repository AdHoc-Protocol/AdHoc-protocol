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
    /// <summary>
    /// This file defines the **meta-protocol** for the AdHoc system itself. It orchestrates the communication
    /// between the `AdHocAgent` (the developer's tool), the code-generation `Server`, and the `Observer`
    /// (the browser-based visualizer).
    ///
    /// It specifies the data structures (`packs`) like `Agent.Project`, the communication endpoints (`hosts`)
    /// such as `Server` and `Agent`, and the stateful communication flows (`channels`) that connect them,
    /// complete with stages and branching logic.
    /// </summary>
    /**
        <see cref = 'Agent.Login'            id = '5'/>
        <see cref = 'Agent.Project'          id = '8'/>
        <see cref = 'Agent.Proto'            id = '9'/>
        <see cref = 'Agent.Version'          id = '2'/>
        <see cref = 'LayoutFile.Info'        id = '1'/>
        <see cref = 'LayoutFile.UID'         id = '0'/>
        <see cref = 'Observer.Show_Code'     id = '11'/>
        <see cref = 'Observer.Up_to_date'    id = '12'/>
        <see cref = 'Server.Info'            id = '4'/>
        <see cref = 'Server.Invitation'      id = '3'/>
        <see cref = 'Server.Result'          id = '10'/>
        <see cref = 'Server.InvitationUpdate'    id = '6'/>
    */
    public interface AdHocProtocol /*Ĭāƥſą*/ :
        // This `_<>` block acts as a "Pack Set" inclusion. It propagates the constants from the `DataType` enum
        // to all hosts defined in this protocol, ensuring they are globally available and consistently defined.
        _<
            AdHocProtocol.Agent.Project.Host.Pack.Field.DataType //propagate DataType constants set to all hosts
        >{
        // --- Reusable Type Definitions (TYPEDEFs) ---
        // These classes serve as reusable type aliases (`TYPEDEFs`). A class with a single `TYPEDEF` field
        // allows complex type definitions with attributes to be declared once and reused across multiple fields.

        /// <summary>
        /// Defines a reusable type alias (`TYPEDEF`) for a string that can hold up to 65,000 characters.
        /// The `[D(+N)]` attribute is used to override the default string limit (255 chars) for larger payloads.
        /// </summary>
        class max_65_000_chars{
            [D(+65_000)] string TYPEDEF;
        }

        /// <summary>
        /// Defines a reusable type alias (`TYPEDEF`) for a string that can hold up to 1,000 characters.
        /// This is suitable for shorter metadata like constant values or inline documentation.
        /// </summary>
        class max_1_000_chars{
            [D(+1_000)] string TYPEDEF;
        }

        // --- Common Base Packs ---
        // These packs define common fields that can be inherited by other packs, promoting composition
        // and reducing redundant field declarations.

        /// <summary>
        /// A base pack representing a generic entity with a name and documentation.
        /// This serves as a foundational building block for other metadata-carrying packs like `Constants`.
        /// </summary>
        class Entity /*ÿ*/{
            string           name;
            max_65_000_chars doc;        // Field for full, XML-style documentation.
            string           inline_doc; // Field for a short, single-line summary.
        }

        /// <summary>
        /// A base pack for entities that reference a set of constants, inheriting fields from `Entity`.
        /// This structure is used to link protocol entities to their attributes (which are modeled as constants).
        /// The `parent` field creates a hierarchy, while `constants` is an array of indices into the global `constant_fields` array.
        /// </summary>
        class Constants /*Ā*/ : Entity{
            /// <summary>
            /// Optional reference to a parent entity's index, used to build the protocol's hierarchical structure.
            /// The virtual array of all entities is ordered: packs, hosts, channels, stages.
            /// A value of 0xFFFF (ushort.MaxValue) signifies that this entity has no parent.
            /// </summary>
            [MinMax(0, 0xFFFF - 1)] ushort? parent;

            /// <summary>
            /// An array of indices that map to the project-level 'constant_fields' array.
            /// This mechanism links an entity (like a pack or field) to its associated attributes and metadata.
            /// </summary>
            [D(65_000)] int[,] constants;
        }

        /// <summary>
        /// Represents a reference to a specific item (e.g., Pack, Host, Field) within the protocol structure.
        /// This pack is used by the `Observer` to send commands to the `Agent` related to a specific UI element,
        /// enabling features like "Show Code" for interactive visualization.
        /// </summary>
        public class Item /*ā*/{
            /// <summary>
            /// The category of the referenced item, defined by the `Type` enum.
            /// An enum can be used directly as a field type.
            /// </summary>
            Type tYpe;

            public enum Type /*ė*/ : byte{ // Enumeration defining the possible types of an item.
                Project,                   // A reference to the entire project.
                Host,                      // A reference to a specific host.
                Pack,                      // A reference to a specific pack.
                Field,                     // A reference to a specific field.
                Constant,                  // A reference to a constant.
                Channel,                   // A reference to a communication channel.
                Stage,                     // A reference to a stage within a channel's state machine.
            }

            /// <summary>
            /// The index of the item within its corresponding container array in the `Agent.Project` pack.
            /// </summary>
            ushort idx;
        }

        // =================================================================================================
        // == HOST DEFINITIONS
        // =================================================================================================
        // Hosts are the active participants, or endpoints, in the protocol. Each host struct
        // specifies its target languages and other implementation details, allowing the generator
        // to produce tailored, platform-specific code.

        /// <summary>
        /// Defines the Server host. It is responsible for receiving protocol descriptions,
        /// generating source code, and sending back the results or any errors.
        /// The packs nested within this host define the messages it can send or receive.
        /// </summary>
        /**
        <see cref = 'InJAVA'/> The following packs of the `Server` host are fully implemented and generated in JAVA.
        <see cref = 'Result'/>
        <see cref = 'Agent.Proto'/>
        <see cref = 'Agent.Login'/>
        <see cref = 'Agent.Version'/>
        <see cref = 'Server.Invitation'/>
        <see cref = 'Server.InvitationUpdate'/>
        <see cref = 'Agent.Project.Channel.Stage.Branch'/>
        <see cref = 'InJAVA'/>-- The remaining packs are generated in JAVA as abstract (without implementation).
        */
        struct Server /*ÿ*/ : Host{
            /// <summary>
            /// An empty pack sent by the Server to invite the Agent to the next communication stage (e.g., proceed to login).
            /// Empty packs are implemented as highly efficient singletons, making them ideal for signaling state transitions.
            /// </summary>
            public class Invitation /*Ă*/{ }

            /// <summary>
            /// Sent by the Server after a successful login to provide the Agent with a new, temporary (volatile) UUID for the session.
            /// The 128-bit UUID is split into two `ulong` fields. Its volatile nature prevents reuse and supports automated
            /// CI/CD workflows, as the new UUID is automatically stored in the `AdHocAgent.toml` config file.
            /// </summary>
            public class InvitationUpdate /*ă*/{
                /// <summary>The higher 64 bits of the new 128-bit volatile UUID.</summary>
                public ulong uuid_hi;

                /// <summary>The lower 64 bits of the new 128-bit volatile UUID.</summary>
                public ulong uuid_lo;
            }

            /// <summary>
            /// A generic informational or error message pack sent from the Server to the Agent.
            /// </summary>
            public class Info /*Ą*/{
                /// <summary>The unique task ID this information relates to.</summary>
                string task;

                /// <summary>The detailed informational or error message content.</summary>
                max_65_000_chars info;
            }

            /// <summary>
            /// Contains the final result of a code generation task, sent from the Server to the Agent.
            /// </summary>
            public class Result /*ą*/{
                /// <summary>The unique task ID this result corresponds to.</summary>
                string task;

                /// <summary>The generated code, compressed as a binary array. The `[D(3_000_000)]` attribute sets the max size to 30MB.</summary>
                [D(3_000_000)] Binary[,] result;

                /// <summary>Additional information, server anonsments.</summary>
                max_65_000_chars info;
            }
        }

        /// <summary>
        /// Defines the Agent host, which corresponds to the `AdHocAgent` command-line tool.
        /// Its primary role is to serialize the user's protocol definition into the `Project` pack and send it to the Server.
        /// </summary>
        /**
            <see cref = 'InCS'/> The following packs of the `Agent` host are fully implemented and generated in C#.
            <see cref = 'Server.Info'/>
            <see cref = 'LayoutFile.UID'/>
            <see cref = 'LayoutFile.Info'/>
            <see cref = 'LayoutFile.Info.View'/>
            <see cref = 'LayoutFile.Info.XY'/>
            <see cref = 'Server.InvitationUpdate'/>
            <see cref = 'Server.Result'/>
            <see cref = 'Version'/>
            <see cref = 'Login'/>
            <see cref = 'Proto'/>
            <see cref = 'Observer.Up_to_date'/>
            <see cref = 'Observer.Show_Code'/>
            <see cref = 'InCS'/>-- The remaining packs are generated in C# as abstract (without implementation).
         */
        struct Agent /*Ā*/ : Host{
            // --- META-PROTOCOL: The 'Project' pack describes the entire protocol structure ---

            /// <summary>
            /// This is the central "meta-pack" of the system. It contains a complete, serialized
            /// description of a user's AdHoc protocol project. The Agent constructs this pack and sends
            /// it to the Server, which uses this structured data to perform code generation.
            /// </summary>
            public class Project /*Ć*/ : Constants{
                /// <summary>A unique ID for this specific code generation task.</summary>
                string task;

                /// <summary>The root namespace of the user's generated code.</summary>
                string namespacE;

                /// <summary>The timestamp of when the project was submitted for generation.</summary>
                long time;

                /// <summary>The compressed (PPMd) source code of the user's protocol description files.</summary>
                [D(0x1_FFFF)] Binary[,] source;

                /// <summary>The permanent, unique ID of the project itself.</summary>
                ulong uid;

                /// <summary>UIDs of other AdHoc projects imported by this one. The root project is at index 0.</summary>
                [D(0xFF)] ulong[,] imported_projects_uid;

                // --- FIXED ORDER METADATA ARRAYS ---
                // The order of these arrays is critical. The Server's parser relies on this exact sequence
                // to correctly deserialize the project's structure.
#region FIXED ORDER
                /// <summary>A flat list of all fields defined across all packs in the project.</summary>
                [D(0xFFFF)] Host.Pack.Field[,] fields;

                /// <summary>A flat list of all constants and attributes defined in the project.</summary>
                [D(0xFFFF)] Host.Pack.Constant[,] constant_fields;

                /// <summary>A flat list of all packs (data structures) defined in the project.</summary>
                [D(0xFFFF)] Host.Pack[,] packs;

                /// <summary>A list of all hosts defined in the project.</summary>
                [D(0xFF)] Host[,] hosts;

                /// <summary>A list of all communication channels defined in the project.</summary>
                [D(0xFF)] Channel[,] channels;
#endregion

                /// <summary>
                /// Describes a single Host within the user's project.
                /// </summary>
                public class Host /*ć*/ : Constants{
                    /// <summary>Persistent unique identifier for this host.</summary>
                    byte uid;

                    /// <summary>A bitmask of `Langs` flags indicating which languages to generate code for.</summary>
                    Langs langs;

                    /// <summary>If defined, enables MultiContext mode for this host, allowing the specified number of concurrent logical sessions on one connection.</summary>
                    [MinMax(1, 0xFFFF)] ushort? contexts;

                    /**
                        Value:  16 Least Significant Bits - hash_equal info
                                16 Most  Significant Bits - impl info
                     */
                    /// <summary>Maps a pack index to its language-specific implementation and hash equality settings.</summary>
                    Map<ushort, uint> pack_impl_hash_equal; // Pack -> impl_hash_equal

                    /**
                        16 Least Significant Bits - hash_equal info
                        16 Most  Significant Bits - impl info
                     */
                    /// <summary>Default implementation and hash equality settings applied to all packs in this host unless overridden.</summary>
                    uint default_impl_hash_equal;

                    /// <summary>Maps a field index to its language-specific implementation settings.</summary>
                    Map<ushort, Langs> field_impl;

                    /// <summary>Indices of local packs (constants/enums) declared directly within this host's scope.</summary>
                    [D(65_000)] ushort[,] packs;

                    [Flags]
                    public enum Langs /*Ę*/ : ushort{
                        InCPP   = 1 << 0,
                        InRS    = 1 << 1,
                        InCS    = 1 << 2,
                        InJAVA  = 1 << 3,
                        InGO    = 1 << 4,
                        InTS    = 1 << 5,
                        InSwift = 1 << 6,
                        All     = 0xFFFF
                    }

                    /// <summary>
                    /// Describes a single Pack (data structure) within the user's project.
                    /// </summary>
                    public class Pack /*Ĉ*/ : Constants{
                        /// <summary>The generated, project-specific ID for this pack, used for on-the-wire identification.</summary>
                        ushort id;

                        /// <summary>Persistent unique identifier for this pack, stable across compilations.</summary>
                        ushort uid;

                        /// <summary>Maximum nesting depth for recursively defined packs (e.g., a tree data structure).</summary>
                        [MinMax(0, 0xFF - 1)] byte? nested_max;

                        /// <summary>True if this pack is used as a field type within another pack (i.e., as a sub-pack).</summary>
                        bool referred;

                        /// <summary>Indices into the global `fields` array for fields belonging to this pack.</summary>
                        [D(65_000)] int[,] fields;

                        /// <summary>
                        /// Describes a single Field within a Pack, including its type, constraints, and attributes.
                        /// </summary>
                        public class Field /*ĉ*/ : Entity{
                            /// <summary>Dimensions for multi-dimensional arrays, as defined by `[D(-N, ~N)]` attributes in the user's protocol.</summary>
                            [D(32)] int[,] dims;

                            /// <summary>Maximum length constraint for a Map or Set collection.</summary>
                            uint? map_set_len;

                            /// <summary>Array dimensions for a Map or Set collection.</summary>
                            uint? map_set_array;

                            // --- Type Information (External & Internal) ---
                            /// <summary>The external (application-facing) data type of the field (e.g., the type used in the generated C# or Java class).</summary>
                            ushort exT;

                            /// <summary>Length constraint for the external type (e.g., max string length).</summary>
                            uint? exT_len;

                            /// <summary>Array dimensions for the external type.</summary>
                            uint? exT_array;

                            /// <summary>The internal (storage-optimized) data type. Can differ from `exT` to save space or improve performance.</summary>
                            ushort? inT;

                            // --- Value Constraints ---
                            long? min_value;
                            long? max_value;

                            /// <summary>Specifies the direction for Varint compression: -1 for V(max-val), 0 for X(zigzag), 1 for A(min-val).</summary>
                            [MinMax(-1, 1)] sbyte? dir;

                            double? min_valueD;
                            double? max_valueD;

                            /// <summary>Number of bits required (1-7) if this field is part of a bitfield, enabling multiple fields to be packed into a single byte.</summary>
                            [MinMax(1, 7)] byte? bits;

                            /// <summary>A bitmask that defines nullability behavior and special values, enabling efficient handling of optional fields.</summary>
                            byte? null_value; // If the field is a bits-field:
                            //   Represents a value that substitutes NULL or null if the bits-field is not nullable.
                            // If the field is not a bits-field:
                            // - Bit 0: Set to 1 if the field is a single nullable primitive or if the field's Generic type is nullable:
                            //          (e.g., int? field;)
                            //          (e.g., Set<Type?> field;)
                            //          (e.g., Set<Type[,]?> field;).
                            // - Bit 1: Set to 1 if the field is a (multidimensional) collection of nullable elements:
                            //          (e.g., Type?[,] field;)
                            //          (e.g., Set<Type>?[] field;).
                            // - Bit 2: Set to 1 if the field's Generic type is a      collection of nullable elements:
                            //          (e.g., Set<Type?[,,]> field;).

#region Map Value Parameters
                            // These fields specifically describe the 'Value' part of a Key-Value Map.
                            ushort? exTV;
                            uint?   exTV_len;
                            uint?   exTV_array;

                            ushort?                inTV;
                            long?                  min_valueV;
                            long?                  max_valueV;
                            [MinMax(-1, 1)] sbyte? dirV;

                            double?              min_valueDV;
                            double?              max_valueDV;
                            [MinMax(1, 7)] byte? bitsV;
                            byte?                null_valueV;
#endregion

                            /// <summary>An array of indices pointing to packs that represent this field's custom attributes.</summary>
                            public Pack[,] attributes;

                            /// <summary>Internal enumeration of all possible data types recognized by the generator. These are abstract types mapped to platform-specific ones during code generation.</summary>
                            public enum DataType /*ę*/{
                                t_constants  = 65535, // Reserved for a constant set type
                                t_enum_sw    = 65534, // Reserved for switch enums
                                t_enum_exp   = 65533, // Reserved for expression enums
                                t_enum_flags = 65532, // Reserved for flags enum
                                t_bool       = 65531, // Reserved for boolean type
                                t_int8       = 65530, // Reserved for 8-bit signed integers
                                t_binary     = 65529, // Reserved for binary data
                                t_uint8      = 65528, // Reserved for 8-bit unsigned integers
                                t_int16      = 65527, // Reserved for 16-bit signed integers
                                t_uint16     = 65526, // Reserved for 16-bit unsigned integers
                                t_char       = 65525, // Reserved for characters
                                t_int32      = 65524, // Reserved for 32-bit signed integers
                                t_uint32     = 65523, // Reserved for 32-bit unsigned integers
                                t_int64      = 65522, // Reserved for 64-bit signed integers
                                t_uint64     = 65521, // Reserved for 64-bit unsigned integers
                                t_float      = 65520, // Reserved for a float type
                                t_double     = 65519, // Reserved for double type
                                t_string     = 65518, // Reserved for a string type
                                t_map        = 65517, // Reserved for a map type
                                t_set        = 65516, // Reserved for a set type
                                t_subpack    = 65515, // Reserved for a sub-pack type
                            }
                        }

                        /// <summary>Describes a single constant or enum member within the protocol.</summary>
                        public class Constant /*Ċ*/ : Entity{
                            ushort                      exT;
                            long?                       value_int;    // The value if the constant is an integer type.
                            double?                     value_double; // The value if the constant is a floating-point type.
                            max_1_000_chars             value_string; // The value if the constant is a string.
                            [D(255)] max_1_000_chars[,] array;        // The values if the constant is an array.
                        }
                    }
                }

                /// <summary>
                /// Describes a single communication Channel between two Hosts.
                /// </summary>
                public class Channel /*ċ*/ : Constants{
                    /// <summary>Persistent unique identifier for this channel.</summary>
                    byte uid;

                    /// <summary>Index of the Left Host in the project's `hosts` array.</summary>
                    byte hostL;

                    /// <summary>A list of pack indices that the Left Host is allowed to transmit on this channel.</summary>
                    [D(0xFFFF)] ushort[,] hostL_transmitting_packs;

                    /// <summary>A list of pack indices related to (e.g., received by) the Left Host on this channel.</summary>
                    [D(0xFFFF)] ushort[,] hostL_related_packs;

                    /// <summary>Index of the Right Host in the project's `hosts` array.</summary>
                    byte hostR;

                    /// <summary>A list of pack indices that the Right Host is allowed to transmit on this channel.</summary>
                    [D(0xFFFF)] ushort[,] hostR_transmitting_packs;

                    /// <summary>A list of pack indices related to (e.g., received by) the Right Host on this channel.</summary>
                    [D(0xFFFF)] ushort[,] hostR_related_packs;

                    /// <summary>The set of all stages that make up this channel's state machine.</summary>
                    [D(0xFFF)] Stage[,] stages;

                    /// <summary>
                    /// Describes a single state (Stage) in the channel's state machine.
                    /// </summary>
                    public class Stage /*Č*/ : Constants{
                        /// <summary>Persistent unique identifier for this stage.</summary>
                        ushort uid;

                        /// <summary>The timeout in seconds for this stage. If exceeded, a connection error is typically triggered.</summary>
                        ushort timeout;

                        /// <summary>The set of possible transitions (branches) for the Left Host from this stage.</summary>
                        [D(0xFFF)] Branch[,] branchesL;

                        /// <summary>The set of possible transitions (branches) for the Right Host from this stage.</summary>
                        [D(0xFFF)] Branch[,] branchesR;

                        /// <summary>
                        /// Describes a single transition (Branch) from a Stage, which is triggered by sending a specific pack.
                        /// </summary>
                        public class Branch /*č*/{
                            string doc;

                            /// <summary>The index of the stage to transition to. A value of `ushort.MaxValue` is a special signal to terminate the connection.</summary>
                            ushort goto_stage;

                            /// <summary>The set of packs that can be sent to trigger this transition.</summary>
                            [D(0xFFFF)] ushort[,] packs;
                        }

                        /// <summary>A special constant representing a transition that terminates the connection.</summary>
                        const ushort Exit = ushort.MaxValue;
                    }
                }
            }

            // --- Agent-Specific Action Packs ---

            /// <summary>
            /// Contains the user's credentials (a permanent UUID) used for authentication with the Server.
            /// </summary>
            public class Login /*Ď*/{
                public ulong uuid_hi; // Higher 64 bits of the 128-bit UUID.
                public ulong uuid_lo; // Lower 64 bits of the 128-bit UUID.
            }

            /// <summary>
            /// The first pack sent by the Agent to the Server to negotiate the protocol version.
            /// </summary>
            public class Version /*ď*/{
                /// <summary>A unique hash or identifier representing the agent's protocol version.</summary>
                public uint uid;
            }

            /// <summary>
            /// A pack used to send a `.proto` file (or files) to the Server for conversion into the AdHoc format.
            /// </summary>
            public class Proto /*Đ*/{
                string task; // A unique ID for this conversion task.
                string name;

                /// <summary>The binary content of the `.proto` file(s).</summary>
                [D(512_000)] Binary[,] proto;
            }
        }

        /// <summary>
        /// Defines the Observer host, representing the browser-based visualizer tool.
        /// It requests project data from the Agent and sends UI interaction commands back.
        /// </summary>
        /**
        <see cref = 'InTS'/>All packs of the `Observer` host are fully implemented and generated in TypeScript
        */
        struct Observer /*ā*/ : Host{
            /// <summary>
            /// A request from the Observer to check if its data is stale. The Agent will respond either
            /// with an updated `Project` pack or with this same pack to confirm it's already up-to-date.
            /// </summary>
            public class Up_to_date /*đ*/{
                max_65_000_chars info; // Can be used to return an error description if an update check fails.
            }

            /// <summary>
            /// A command from the Observer requesting that the Agent open the source code for a specific protocol item
            /// in the user's configured local IDE.
            /// </summary>

            //JetBrains Rider
            //https://www.jetbrains.com/help/rider/Opening_Files_from_Command_Line.html

            //VS Code
            // https://code.visualstudio.com/docs/editor/command-line#_launching-from-command-line
            //-g or --goto	When used with a file:line{:character}, opens a file at a specific line and optional character position.
            //This argument is provided since some operating systems permit : in a file name.
            public class Show_Code /*Ē*/ : Item{ }
        }

        /// <summary>
        /// Defines a virtual host to model the `.layout` file on disk. This allows saving and loading
        /// the visual state of the Observer's diagrams as a standard protocol interaction,
        /// rather than handling it as a special case.
        /// </summary>
        /**
        <see cref = 'InCS'/>All packs of the virtual `LayoutFile` host are fully implemented and generated in C#
        */
        struct LayoutFile /*Ă*/ : Host{
            /// <summary>
            /// Maps the persistent UIDs of protocol entities (hosts, packs, etc.) to their layout keys.
            /// This ensures that diagram positions are preserved across sessions, even if volatile internal IDs change.
            /// </summary>
            public class UID /*ē*/{
                [D(0xFF)]   ulong[,] hosts;    // Maps host UIDs to their layout positions.
                [D(0xFFFF)] ulong[,] packs;    // Maps pack UIDs to their layout positions.
                [D(0xFFF)]  ulong[,] branches; // Maps branch UIDs to their layout positions.
            }

            /// <summary>
            /// Contains the actual layout information, such as coordinates, zoom levels, and splitter positions
            /// for the various diagrams displayed in the Observer.
            /// </summary>
            public class Info /*Ĕ*/{
                View host_packs;  // View settings (zoom, pan) for the host-packs diagram.
                View pack_fields; // View settings for the pack-fields diagram.
                View channels;

                class XY /*ĕ*/{
                    int x; // X-coordinate. A value of int.MinValue indicates an unassigned position.
                    int y; // Y-coordinate.
                }

                class View /*Ė*/ : XY{
                    int  x;
                    int  y;
                    int  w;
                    int  h;

                    ushort hue;

                    int    panX;
                    int    panY;
                    float  zoom; // The zoom level for this view.
                }

                [D(0xFF)]   XY[,] hosts;    // Stores positions for hosts in the Hosts Diagram.
                [D(0xFFFF)] XY[,] packs;    // Stores positions for packs in the Packs Diagram.
                [D(0xFFF)]  XY[,] branches; // Stores positions for branches in the Channels Diagram.
            }
        }
        // =================================================================================================
        // == CHANNEL DEFINITIONS
        // =================================================================================================
        // Channels define the communication flows and state machines between two hosts.

        /// <summary>
        /// The main stateful communication channel between the Agent and the Server.
        /// It defines the entire workflow: version check -> login -> job submission -> result retrieval.
        /// </summary>
        interface Communication /*ÿ*/ : ChannelFor<Agent, Server>{
            /// <summary>A "Named Pack Set" that groups the two possible final responses from the Server (`Info` or `Result`).
            /// This simplifies referencing them in the state machine branches below.</summary>
            interface Info_Result : // This interface defines the pack set.
                _<
                    Server.Info,
                    Server.Result
                >{ }

            // --- State Machine Definition ---
            // Each nested interface here defines a "Stage" in the communication lifecycle.
            /// <summary>STAGE 1: The initial state. The Agent (`L` for Left host) must send its `Version` pack,
            /// which transitions the state machine to the `VersionMatching` stage.</summary>
            [TransmitTimeout(12)]                       // Sets a 12-second timeout for this stage.
            interface Start /*ÿ*/ : L,                  // 'L' indicates the Left host (Agent) is the sender in this stage.
                                    _< /*ÿ*/            // This block defines a "Branch" for this stage.
                                        Agent.Version,  // The pack that can be sent.
                                        VersionMatching // The stage to transition to upon sending.
                                    >{ }

            /// <summary>STAGE 2: The Server (`R` for Right host) validates the version. It can either send
            /// an `Invitation` (on success, moving to `Login` stage) or an `Info` pack (on failure, terminating with `Exit`).</summary>
            [TransmitTimeout(1)]
            interface VersionMatching /*Ā*/ : R,       // 'R' indicates the Right host (Server) is the sender.
                                              _< /*ÿ*/ // Branch 1: Success case.
                                                  Server.Invitation,
                                                  Login
                                              >,
                                              _< /*Ā*/ // Branch 2: Failure case.
                                                  Server.Info,
                                                  Exit // `Exit` is a special target that terminates the connection.
                                              >{ }

            /// <summary>STAGE 3: The Agent is expected to send its `Login` credentials, which moves the state to `LoginResponse`.</summary>
            interface Login /*ā*/ : L,
                                    _< /*ÿ*/
                                        Agent.Login,
                                        LoginResponse
                                    >{ }

            /// <summary>STAGE 4: The Server validates the login. It can respond with an `Invitation` (with an optional UUID update)
            /// on success, or an `Info` pack on failure.</summary>
            [TransmitTimeout(12)]
            interface LoginResponse /*Ă*/ : R,
                                            _< /*ÿ*/                     // Branch for successful login.
                                                Server.Invitation,       // A successful login response.
                                                Server.InvitationUpdate, // A successful login that also updates the agent's volatile UUID.
                                                TodoJobRequest           // Transition to the job request stage.
                                            >,
                                            _< /*Ā*/ // Branch for failed login.
                                                Server.Info,
                                                Exit
                                            >{ }

            /// <summary>STAGE 5: The authenticated Agent can now send a generation job, either a `Project` or a `Proto` file.</summary>
            [TransmitTimeout(12)]
            interface TodoJobRequest /*ă*/ : L,
                                             _< /*ÿ*/ // Branch for a standard Project submission.
                                                 Agent.Project,
                                                 Project
                                             >,
                                             _< /*Ā*/ // Branch for a Proto file conversion submission.
                                                 Agent.Proto,
                                                 Proto
                                             >{ }

            /// <summary>STAGE 6 (Project): The Server processes the project and sends a final response from the `Info_Result` pack set, then exits.</summary>
            interface Project /*Ą*/ : R,
                                      _<               /*ÿ*/
                                          Info_Result, // Use the named pack set for the possible responses.
                                          Exit
                                      >{ }

            /// <summary>STAGE 6 (Proto): The Server processes the proto file and sends a final response from the `Info_Result` pack set, then exits.</summary>
            interface Proto /*ą*/ : R,
                                    _< /*ÿ*/
                                        Info_Result,
                                        Exit
                                    >{ }
        }

        /// <summary>
        /// A simple, stateless channel for saving and restoring layout UID translations between the Agent and the virtual LayoutFile.
        /// `LR` indicates that both hosts can send and receive packs in the `Start` stage without a state change.
        /// </summary>
        interface SaveLayout /*Ā*/ : ChannelFor<Agent, LayoutFile>{
            interface Start /*ÿ*/ : LR,      // `LR` allows bidirectional communication in this stage.
                                    _< /*ÿ*/ // This is a self-referencing branch; the stage does not change after sending.
                                        LayoutFile.UID,
                                        LayoutFile.Info
                                    >{ };
        }

        /// <summary>
        /// Defines the persistent communication channel between the `Agent` and the `Observer`, allowing for
        /// interactive updates and commands for the visualizer.
        /// </summary>
        interface ObserverCommunication /*ā*/ : ChannelFor<Agent, Observer>{
            /// <summary>STATE 1/3: The Agent initiates the session by pushing layout info and/or the full project to the Observer for initial rendering.</summary>
            interface Start /*ÿ*/ : L,
                                    _< /*ÿ*/ // Branch 1: Agent can send layout info first.
                                        LayoutFile.Info,
                                        LayoutSent // Transitions to a state where only the project can be sent.
                                    >,
                                    _< /*Ā*/ // Branch 2: Agent can send the project directly if there's no layout info.
                                        Agent.Project,
                                        Operate // Transitions directly to the interactive state.
                                    >{ }

            /// <summary>A transient state ensuring the project is sent immediately after the layout information.</summary>
            interface LayoutSent /*Ā*/ : L,
                                         _<                 /*ÿ*/
                                             Agent.Project, // Only `Agent.Project` can be sent from this state.
                                             Operate        // Transition to the main interactive stage.
                                         >{ }

            /// <summary>STATE 2/3: The Observer is in an interactive state and can send commands (`Show_Code`, `Up_to_date`) to the Agent.</summary>
            interface Operate /*ā*/ : R,
                                      _< /*ÿ*/                // Branch 1: Observer sends commands that do not require a data refresh from the agent.
                                          Observer.Show_Code, // The stage remains `Operate` after these are sent.
                                          LayoutFile.Info
                                      >,
                                      _< /*Ā*/ // Branch 2: Observer requests a data refresh.
                                          Observer.Up_to_date,
                                          RefreshProject // Transition to a stage where the Agent will respond with data.
                                      >{ }

            /// <summary>STATE 3/3: The Agent responds to the Observer's update request with either the new project data or an "up-to-date" signal.</summary>
            interface RefreshProject /*Ă*/ : L,
                                             _<                       /*ÿ*/
                                                 Agent.Project,       // Option 1: Send the updated project data.
                                                 Observer.Up_to_date, // Option 2: Send a signal that the data is already current.
                                                 Operate              // In either case, return to the interactive 'Operate' stage.
                                             >{ }
        }
    }
}