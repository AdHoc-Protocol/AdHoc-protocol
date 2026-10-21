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
	/** packs
		<see cref='Agent.Login'/>Ď                                    5
		<see cref='Agent.Project'/>Ć                                  8
		<see cref='Agent.Project.Connection'/>ċ
		<see cref='Agent.Project.Connection.Actor'/>Ě
		<see cref='Agent.Project.Connection.Actor.State'/>Č
		<see cref='Agent.Project.Connection.Actor.State.Branch'/>č
		<see cref='Agent.Project.Host'/>ć
		<see cref='Agent.Project.Host.Langs'/>Ę
		<see cref='Agent.Project.Host.Pack'/>Ĉ
		<see cref='Agent.Project.Host.Pack.Constant'/>Ċ
		<see cref='Agent.Project.Host.Pack.Field'/>ĉ
		<see cref='Agent.Project.Host.Pack.Field.DataType'/>ę
		<see cref='Agent.Project.Multiplex'/>Ğ
		<see cref='Agent.Proto'/>Đ                                    9
		<see cref='Agent.Version'/>ď                                  2
		<see cref='Constants'/>Ā
		<see cref='Entity'/>ÿ
		<see cref='FileEntry'/>ě
		<see cref='FileEntry.List'/>Ĝ
		<see cref='Item'/>ā
		<see cref='Item.Type'/>ė
		<see cref='LayoutFile.Info'/>Ĕ                                1
		<see cref='LayoutFile.Info.View'/>Ė
		<see cref='LayoutFile.Info.XY'/>ĕ
		<see cref='LayoutFile.UID'/>ē                                 0
		<see cref='Observer.Show_Code'/>Ē                             11
		<see cref='Observer.Up_to_date'/>đ                            12
		<see cref='Server.Info'/>Ą                                    4
		<see cref='Server.Invitation'/>Ă                              3
		<see cref='Server.InvitationUpdate'/>ă                        6
		<see cref='Server.Result'/>ą                                  10
		<see cref='SrcZip'/>ĝ
	*/
	/** project
		<see cref='AdHocProtocol'/>Ĭāƥſą

	    hosts
		<see cref='Server'/>ÿ
		<see cref='Agent'/>Ā
		<see cref='Observer'/>ā
		<see cref='LayoutFile'/>Ă

	    connections
		<see cref='Communication'/>ÿ
		<see cref='SaveLayout'/>Ā
		<see cref='ObserverCommunication'/>ā
	*/
    /// <summary>
    /// This file defines the meta-protocol for the AdHoc system. It orchestrates the communication
    /// between the AdHocAgent (the developer's tool), the code-generation Server, and the Observer
    /// (the browser-based visualizer).
    ///
    /// It specifies data structures (packs), communication endpoints (hosts), and the
    /// stateful connections that link them, defining protocol states and branching logic.
    /// </summary>
	public interface AdHocProtocol :
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
        class Entity {
            string           name;
            max_65_000_chars doc;        // Field for full, XML-style documentation.
            string           inline_doc; // Field for a short, single-line summary.
        }

        /// <summary>
        /// A base pack for entities that reference a set of constants, inheriting fields from `Entity`.
        /// This structure is used to link protocol entities to their attributes (which are modeled as constants).
        /// The `parent` field creates a hierarchy, while `constants` is an array of indices into the global `constant_fields` array.
        /// </summary>
        class Constants : Entity{
            /// <summary>
            /// Optional reference to a parent entity's index, used to build the protocol's hierarchical structure.
            /// The virtual array of all entities is ordered: packs, hosts, multiplexers, connections, actors, states, fields.
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
        public class Item {
            /// <summary>
            /// The category of the referenced item, defined by the `Type` enum.
            /// An enum can be used directly as a field type.
            /// </summary>
            Type tYpe;

            public enum Type : byte{ // Enumeration defining the possible types of an item.
                Project,                   // A reference to the entire project.
                Host,                      // A reference to a specific host.
                Pack,                      // A reference to a specific pack.
                Field,                     // A reference to a specific field.
                Constant,                  // A reference to a constant.
                Connection,                // A reference to a physical  link.
                Actor,                     // A reference to a logical link within Connection.
                State,                     // A reference to a state within a Actor's state machine.
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
        <see cref = 'Agent.Project.Connection.Actor.State.Branch'/>
        <see cref = 'InJAVA'/>-- The remaining packs are generated in JAVA as abstract (without implementation).
        */
        struct Server : Host{
            /// <summary>
            /// An empty pack sent by the Server to invite the Agent to the next communication state (e.g., proceed to login).
            /// Empty packs are implemented as highly efficient singletons, making them ideal for signaling state transitions.
            /// </summary>
            public class Invitation { }

            /// <summary>
            /// Sent by the Server after a successful login to provide the Agent with a new session identifier.
            /// Its volatile nature prevents reuse and supports automated CI/CD workflows,
            /// as the new UUID is automatically stored in the `AdHocAgent.toml` config file.
            /// </summary>
            public class InvitationUpdate {
                /// <summary>The higher 64 bits of the new 128-bit volatile UUID.</summary>
                public ulong uuid_hi;

                /// <summary>The lower 64 bits of the new 128-bit volatile UUID.</summary>
                public ulong uuid_lo;
            }

            /// <summary>
            /// A generic informational or error message pack sent from the Server to the Agent.
            /// </summary>
            public class Info {
                /// <summary>The unique task ID this information relates to.</summary>
                string task;

                /// <summary>The detailed informational or error message content.</summary>
                max_65_000_chars info;
            }

            /// <summary>
            /// Contains the final result of a code generation task, sent from the Server to the Agent.
            /// </summary>
            public class Result {
                /// <summary>The unique task ID this result corresponds to.</summary>
                string task;

                /// <summary>The generated code, compressed on the wire and typed at both ends: the Agent unpacks it to disk.</summary>
                [SrcZip]
                FileEntry.List result;

                /// <summary>Additional information or server announcements.</summary>
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
        struct Agent : Host{
            // --- META-PROTOCOL: The 'Project' pack describes the entire protocol structure ---

            /// <summary>
            /// This is the central "meta-pack" of the system. It contains a complete, serialized
            /// description of a user's AdHoc protocol project. The Agent constructs this pack and sends
            /// it to the Server, which uses this structured data to perform code generation.
            /// </summary>
            public class Project : Constants{
                /// <summary>A unique ID for this specific code generation task.</summary>
                string task;

                /// <summary>The root namespace of the user's generated code.</summary>
                string namespacE;

                /// <summary>The timestamp of when the project was submitted for generation.</summary>
                long time;

                /// <summary>
                /// The user's protocol description files. The Server stores the archive rather than reading it, so the
                /// cut hands it over still compressed: `SrcZip` runs, the marker to its right stops the receiver there.
                /// </summary>
                [SrcZip, ToStream<IfSendingFrom<Agent, Communication>>]
                FileEntry.List source;

                /// <summary>The permanent, unique ID of the project itself.</summary>
                ulong uid;

                /// <summary>
                /// Present only when the project asks to be listed in the catalog of projects using the AdHoc protocol. The URL of the `adhoc` folder in the user's public GitHub
                /// repository that holds a copy of every protocol description file sent in `source`:
                /// `https://github.com/owner/repo/tree/branch/path/to/adhoc`. The repository must have at least
                /// 500 stars. The Agent verifies the stars and the copies before sending; the Server re-verifies them.
                /// </summary>
                max_1_000_chars github;

                /// <summary>
                /// Open source mode only, otherwise absent. Topical tags of the project: English words separated
                /// by a single space.
                /// </summary>
                max_1_000_chars tags;

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
                [D(0xFF)] Host[,]      hosts;
                [D(0xFF)] Multiplex[,] multiplex;

                /// <summary>A list of all connections defined in the project.</summary>
                [D(0xFF)] Connection[,] connections;
#endregion

                /// <summary>
                /// Describes a single Host within the user's project.
                /// </summary>
                public class Host : Constants{
                    /// <summary>Persistent unique identifier for this host.</summary>
                    byte uid;

                    /// <summary>A bitmask of `Langs` flags indicating which languages to generate code for.</summary>
                    Langs langs;

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

                    /// <summary>Indices of local packs (constants/enums) declared directly within this host's scope or used by packs that transit throught the host.</summary>
                    [D(65_000)] ushort[,] packs;

                    [Flags]
                    public enum Langs : ushort{
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
                    public class Pack : Constants{
                        /// <summary>The generated, project-specific ID for this pack, used for on-the-wire identification.</summary>
                        ushort id;

                        /// <summary>Persistent unique identifier for this pack, stable across compilations.</summary>
                        ushort uid;

                        /// <summary>
                        /// Index of this pack's <b>stream transform chain</b> container, or 0xFFFF when it carries none.
                        /// <para>
                        /// The container is a pack of the `t_constants` kind with `referred == true` (a combination no
                        /// other pack has). Its `constants` are ordered references — one int each, holding a sub-pack
                        /// index — to one sub-pack per chain entry, listed <b>wire end first</b>. `nested_max` records
                        /// what the chain was declared on: 1 field, 2 pack, 3 connection.
                        /// </para>
                        /// <para>
                        /// <b>What each entry is, is read from the sub-pack's `id`.</b> Existing `DataType` values are
                        /// reused as tags: in this position none of them can mean a data type, and the run is
                        /// contiguous, so `t_int16 &lt;= id &lt;= t_bool` identifies a chain entry in one test. The
                        /// sub-pack's `name` (`Zstd`, `ChaCha20`, `ToStream`, …) is for humans; the tag carries meaning.
                        /// <list type="table">
                        ///   <item><description><c>t_bool</c> (65531) — <b>trim: ToStream</b>. On a leg it names, the
                        ///   RECEIVER stops unwrapping at this depth and takes raw bytes.</description></item>
                        ///   <item><description><c>t_int8</c> (65530) — <b>trim: FromStream</b>. On a leg it names, the
                        ///   SENDER supplies raw bytes at this depth.</description></item>
                        ///   <item><description><c>t_binary</c> (65529) — stage, role <b>compression</b>.</description></item>
                        ///   <item><description><c>t_uint8</c> (65528) — stage, role <b>cipher</b>.</description></item>
                        ///   <item><description><c>t_int16</c> (65527) — stage, <b>no role</b>: a plain byte transform.</description></item>
                        /// </list>
                        /// </para>
                        /// <para>
                        /// A <b>stage</b> sub-pack's own constants are its full parameter list — a value for each
                        /// design-time parameter, null for each one to be injected at runtime.
                        /// </para>
                        /// <para>
                        /// A <b>trim</b> sub-pack's constants are the endpoints it applies to, one int each: the
                        /// connection index, positive when the sender is that connection's Left host, bitwise-inverted
                        /// (`~idx`) when it is the Right one.
                        /// A trim transforms nothing — it marks a position. Stages closer to the wire than the cut
                        /// still run on that leg; everything beyond it, including the pack's own serialization, does
                        /// not, and the payload crosses as opaque bytes. Every other leg runs the chain in full.
                        /// </para>
                        /// </summary>
                        ushort link;

                        /// <summary>
                        /// Maximum payload size in bytes, with the kind encoded in the sign:
                        ///   &lt; 0  → Stream kind (chunked, interruptible)  — stored as -N
                        ///   &gt; 0 → File   kind (single length-prefix)     — stored as +N
                        ///   == 0 → not a Stream/File (ordinary pack)
                        /// </summary>
                        long stream_max;

                        /// <summary>Maximum nesting depth for recursively defined packs (e.g., a tree data structure).</summary>
                        [MinMax(0, 0xFF - 1)] byte? nested_max;

                        /// <summary>True if this pack is used as a field type within another pack (i.e., as a sub-pack).</summary>
                        bool referred;

                        /// <summary>Indices into the global `fields` array for fields belonging to this pack.</summary>
                        [D(65_000)] int[,] fields;

                        /// <summary>
                        /// Describes a single Field within a Pack, including its type, constraints, and attributes.
                        /// </summary>
                        public class Field : Entity{
                            /// <summary>Dimensions for multi-dimensional arrays, as defined by `[D(-N, ~N)]` attributes.</summary>
                            [D(32)] int[,] dims;

                            /// <summary>Maximum length constraint for a Map or Set collection.</summary>
                            uint? map_set_len;

                            /// <summary>Array dimensions for a Map or Set collection.</summary>
                            uint? map_set_array;

                            // --- Type Information (External & Internal) ---
                            /// <summary>The external (application-facing) data type of the field.</summary>
                            ushort exT;

                            /// <summary>Array dimensions for the external type.</summary>
                            uint? exT_array;

                            /// <summary>The internal (storage-optimized) data type. Can differ from `exT` to save space.</summary>
                            ushort? inT;

                            // --- Value Constraints ---
                            long? min_value;

                            /// <summary>The upper bound, in units that follow `exT`:
                            ///   `t_string`          → the `[D(+N)]` cap in CHARACTERS (default `_DefaultMaxLengthOf.Strings`).
                            ///                        On the streamed route of a directional string the wire prefix is a UTF-8 BYTE
                            ///                        total - a derived bound (at most 3 bytes per char), not this number.
                            ///   `t_stream`/`t_file` → the `[S(N)]` conduit cap in BYTES.
                            ///   anything else       → the numeric range bound.</summary>
                            long? max_value;

                            /// <summary>Specifies the direction for Varint compression: -1 for V(max-val), 0 for X(zigzag), 1 for A(min-val).</summary>
                            [MinMax(-1, 1)] sbyte? dir;

                            double? min_valueD;
                            double? max_valueD;

                            /// <summary>Number of bits required (1-7) if this field is part of a bitfield.</summary>
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

                            /// <summary>Internal enumeration of all data types recognized by the generator. Abstract types are mapped to platform-specific ones during code generation.</summary>
                            public enum DataType {
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
                                t_stream     = 65515, // Reserved for a stream type
                                t_file       = 65514, // Reserved for a file type
                                t_date       = 65513, // Reserved for standard date time type
                                t_subpack    = 65512, // Reserved for a sub-pack type
                            }
                        }

                        /// <summary>Describes a single constant or enum member within the protocol.</summary>
                        public class Constant : Entity{
                            ushort                      exT;
                            long?                       value_int;    // The value if the constant is an integer type.
                            double?                     value_double; // The value if the constant is a floating-point type.
                            max_1_000_chars             value_string; // The value if the constant is a string.
                            [D(255)] max_1_000_chars[,] array;        // The values if the constant is an array.
                        }
                    }
                }

                public class Multiplex : Constants{
                    byte[,] hosts;
                }

                /// <summary>
                /// Describes a single Connection between two Hosts.
                /// </summary>
                public class Connection : Constants{
                    /// <summary>Persistent unique identifier for this connection.</summary>
                    byte uid;

                    /// <summary>Index of the Left Host in the project's `hosts` array.</summary>
                    byte hostL;

                    /// <summary>Index of the Right Host in the project's `hosts` array.</summary>
                    byte hostR;

                    public ushort id;
                    public ushort link;
                    public int[,] viaPath;
                    public int    MaxTunnels;
                    public uint   MaxStream_KiloBytes;

                    [D(0xFFF)] Actor[,] actors;

                    /// <summary>Packs sent from the Left Host as a structured Source; the Right Host receives them as opaque raw bytes.</summary>
                    [D(0xFFFF)] ushort[,] hostL_to_packs;

                    /// <summary>Packs sent from the Left Host as raw bytes; the Right Host rehydrates them into structured objects (Sink).</summary>
                    [D(0xFFFF)] ushort[,] hostL_from_packs;

                    /// <summary>Packs sent from the Right Host as a structured Source; the Left Host receives them as opaque raw bytes.</summary>
                    [D(0xFFFF)] ushort[,] hostR_to_packs;

                    /// <summary>Packs sent from the Right Host as raw bytes; the Left Host rehydrates them into structured objects (Sink).</summary>
                    [D(0xFFFF)] ushort[,] hostR_from_packs;

                    /// <summary>
                    /// Actor.
                    /// </summary>
                    public class Actor : Constants{
                        /// <summary>Persistent unique identifier for this actor.</summary>
                        ushort uid;

                        /// <summary>
                        /// Defines the maximum number of concurrent instances and the addressing mode.
                        /// </summary>
                        /// <value>
                        /// Use a standard integer (e.g., <c>10</c>) for individual addressing.
                        /// Use the plus prefix (e.g., <c>+10</c>) to enable Multicasting/Pub-Sub behavior.
                        /// </value>
                        int MaxActiveInstances;

                        bool Multicasting;

#region L
                        /// <summary>A list of pack indices that the Left Host is allowed to transmit on this connection.</summary>
                        [D(0xFFFF)] ushort[,] hostL_transmitting_packs;

                        /// <summary>
                        /// What the RIGHT Host parses out of `hostL_transmitting_packs`: the nested types it walks into,
                        /// plus the payload of every field the Left Host hands over as raw bytes for it to rehydrate.
                        /// A payload the Left Host serializes and the Right Host receives as opaque bytes is NOT here —
                        /// that one is in `hostL_to_packs`. `hostL_from_packs` names the subset of this list the Left
                        /// Host itself does not serialize.
                        /// </summary>
                        [D(0xFFFF)] ushort[,] hostL_related_packs;
#endregion
#region R
                        /// <summary>A list of pack indices that the Right Host is allowed to transmit on this connection.</summary>
                        [D(0xFFFF)] ushort[,] hostR_transmitting_packs;

                        /// <summary>
                        /// What the LEFT Host parses out of `hostR_transmitting_packs`: the nested types it walks into,
                        /// plus the payload of every field the Right Host hands over as raw bytes for it to rehydrate.
                        /// A payload the Right Host serializes and the Left Host receives as opaque bytes is NOT here —
                        /// that one is in `hostR_to_packs`. `hostR_from_packs` names the subset of this list the Right
                        /// Host itself does not serialize.
                        /// </summary>
                        [D(0xFFFF)] ushort[,] hostR_related_packs;
#endregion

                        /// <summary>The set of all states that make up this connection's state machine.</summary>
                        [D(0xFFF)] State[,] states;

                        /// <summary>
                        /// Describes a single state (State) in the connection's state machine.
                        /// </summary>
                        public class State : Constants{
                            /// <summary>Persistent unique identifier for this state.</summary>
                            ushort uid;

                            /// <summary>The set of possible transitions (branches) for the Left Host from this state.</summary>
                            [D(0xFFF)] Branch[,] branchesL;


                            /// <summary>The set of possible transitions (branches) for the Right Host from this state.</summary>
                            [D(0xFFF)] Branch[,] branchesR;


                            /// <summary>
                            /// Describes a single transition (Branch) from a State, which is triggered by sending a specific pack.
                            /// </summary>
                            public class Branch {
                                max_65_000_chars doc;

                                /// <summary>The index of the state to transition to. A value of `ushort.MaxValue` signifies connection termination.</summary>
                                ushort goto_state;

                                /// <summary>The set of packs that can be sent to trigger this transition.</summary>
                                [D(0xFFFF)] ushort[,] packs;
                            }

                            /// <summary>
                            /// A special terminal target state that deallocates the current Actors instance.
                            /// This effectively deletes the linked actors,
                            /// while leaving the underlying physical connection open for other actors.
                            /// </summary>
                            const ushort End = ushort.MaxValue - 1;

                            /// <summary>
                            /// A special terminal target state that gracefully terminates the physical connection.
                            /// This ensures the transmission queue is fully drained before closing the pipe.
                            /// Once all pending data is sent, the communication link between hosts is safely shut down.
                            /// </summary>
                            const ushort Close = ushort.MaxValue;
                        }
                    }
                }
            }

            // --- Agent-Specific Action Packs ---

            /// <summary>
            /// Contains the user's credentials used for authentication with the Server.
            /// </summary>
            public class Login {
                public ulong uuid_hi; // Higher 64 bits of the 128-bit UUID.
                public ulong uuid_lo; // Lower 64 bits of the 128-bit UUID.
            }

            /// <summary>
            /// The first pack sent by the Agent to negotiate the protocol version.
            /// </summary>
            public class Version {
                /// <summary>
                /// The culture identifier for the current CultureInfo.
                /// </summary>
                public byte LCID;

                /// <summary>
                /// local UTC offset in 15-minute units
                /// </summary>
                /// <returns></returns>
                public byte zone;

                /// <summary>A unique hash representing the agent's protocol version.</summary>
                public ushort uid;
            }

            /// <summary>
            /// A pack used to send a `.proto` file to the Server for conversion into AdHoc format.
            /// </summary>
            public class Proto {
                string task; // A unique ID for this conversion task.
                string name;

                /// <summary>The binary content of the `.proto` file(s).</summary>
                [Zstd]
                [D(+5_120_000)] string proto;
            }
        }

        /// <summary>
        /// Defines the Observer host, representing the browser-based visualizer.
        /// It requests project data from the Agent and sends UI interaction commands back.
        /// </summary>
        /**
        <see cref = 'InTS'/>All packs of the `Observer` host are fully implemented and generated in TypeScript
        */
        struct Observer : Host{
            /// <summary>
            /// A request from the Observer to check if its project data is stale.
            /// </summary>
            public class Up_to_date {
                max_65_000_chars info; // Can be used to return an error description if an update check fails.
            }

            /// <summary>
            /// A command requesting the Agent to open source code for a specific protocol item in the local IDE.
            /// </summary>

            //JetBrains Rider
            //https://www.jetbrains.com/help/rider/Opening_Files_from_Command_Line.html

            //VS Code
            // https://code.visualstudio.com/docs/editor/command-line#_launching-from-command-line
            //-g or --goto	When used with a file:line{:character}, opens a file at a specific line and optional character position.
            //This argument is provided since some operating systems permit : in a file name.
            //⚙️
            public class Show_Code : Item{ }
        }

        /// <summary>
        /// Defines a virtual host representing the `.layout` file on disk. This allows saving
        /// diagram states as standard protocol interactions.
        /// </summary>
        /**
        <see cref = 'InCS'/>All packs of the virtual `LayoutFile` host are fully implemented and generated in C#
        */
        struct LayoutFile : Host{
            /// <summary>
            /// Maps persistent UIDs of entities to their layout keys, preserving diagram
            /// positions across compilations and sessions.
            /// </summary>
            ///🔠
            public class UID {
                [D(0xFF)]   ulong[,] hosts;    // Maps host UIDs to their layout positions.
                [D(0xFFFF)] ulong[,] packs;    // Maps pack UIDs to their layout positions.
                [D(0xFFF)]  ulong[,] branches; // Maps branch UIDs to their layout positions.
            }

            /// <summary>
            /// Contains the actual layout information, such as coordinates, zoom levels, and splitter positions
            /// for the various diagrams displayed in the Observer.
            /// </summary>
            ///🔠
            ///⚙️
            public class Info {
                View host_packs;  // View settings (zoom, pan) for the host-packs diagram.
                View pack_fields; // View settings for the pack-fields diagram.
                View connections;

                class XY {
                    int x; // X-coordinate. A value of int.MinValue indicates an unassigned position.
                    int y; // Y-coordinate.
                }

                class View : XY{
                    int x;
                    int y;
                    int w;
                    int h;

                    ushort hue;

                    int   panX;
                    int   panY;
                    float zoom; // The zoom level for this view.
                }

                [D(0xFF)]   XY[,] hosts;    // Stores positions for hosts in the Hosts Diagram.
                [D(0xFFFF)] XY[,] packs;    // Stores positions for packs in the Packs Diagram.
                [D(0xFFF)]  XY[,] branches; // Stores positions for branches in the Connections Diagram.
            }
        }
        // =================================================================================================
        // == CONNECTION DEFINITIONS
        // =================================================================================================
        // Connections define communication links and state machines between host pairs.

        /// <summary>
        /// The main stateful connection between the Agent and the Server.
        /// Defines the lifecycle: version check -> login -> job submission -> result retrieval.
        /// </summary>
        interface Communication : Connects<Agent, Server>{
            /// <summary>A "Named Pack Set" that groups the two possible final responses from the Server (`Info` or `Result`).
            /// This simplifies referencing them in the state machine branches below.</summary>
            interface Info_Result : // This interface defines the pack set.
                _<(
                    Server.Info,
                    Server.Result
                    )>{ }

            // --- State Machine Definition ---
            // Each struct here defines a "State" in the communication lifecycle. Branches are declared as
            // attributes. Transitional forms carry the target state as their first type argument.

            /// <summary>STAGE 1: The initial state. The Agent (Left host) must send its `Version`,
            /// which transitions the state machine to the `VersionMatching` state.</summary>
            [TransmitTimeout(12)]                                       // Sets a 12-second timeout for this state.
            [L____________ /*ÿ*/<VersionMatching, Agent.Version>/*ÿ*/] /*ÿ*/ // Master L→R: send Version, transition to VersionMatching.
            struct Start /*ÿ*/{ }

            /// <summary>STAGE 2: The Server (Right host) validates the version.</summary>
            [TransmitTimeout(1)]
            [____________R /*ÿ*/<Login, Server.Invitation>/*ÿ*/] /*ÿ*/ // Success: send Invitation, move to Login.
            [____________R /*Ā*/<Close, Server.Info>/*Ā*/] /*Ā*/       // Failure: send Info, close the connection.
            struct VersionMatching /*Ā*/{ }

            /// <summary>STAGE 3: The Agent sends its `Login` credentials, which moves the state to `LoginResponse`.</summary>
            [L____________ /*ÿ*/<LoginResponse, Agent.Login>/*ÿ*/] /*ÿ*/
            struct Login /*ā*/{ }

            /// <summary>STAGE 4: The Server validates the login. It can respond with an `Invitation` (with an optional UUID update)
            /// on success, or an `Info` pack on failure.</summary>
            [TransmitTimeout(12)]
            [____________R /*ÿ*/<TodoJobRequest, (Server.Invitation, Server.InvitationUpdate)>/*ÿ*/] /*ÿ*/ // Success branch.
            [____________R /*Ā*/<Close, Server.Info>/*Ā*/] /*Ā*/                                           // Failure branch.
            struct LoginResponse /*Ă*/{ }

            /// <summary>STAGE 5: The authenticated Agent can now send a generation job, either a `Project` or a `Proto` file.</summary>
            [TransmitTimeout(2)]
            [L____________ /*ÿ*/<Project, Agent.Project>/*ÿ*/] /*ÿ*/ // Project submission.
            [L____________ /*Ā*/<Proto, Agent.Proto>/*Ā*/] /*Ā*/     // Proto file conversion submission.
            struct TodoJobRequest /*ă*/{ }

            /// <summary>STAGE 6 (Project): The Server processes the project and sends a final response from the `Info_Result` pack set, then exits.</summary>
            [ReceiveTimeout(120)]
            [____________R /*ÿ*/<Close, Info_Result>/*ÿ*/] /*ÿ*/
            struct Project /*Ą*/{ }

            /// <summary>STAGE 6 (Proto): The Server processes the proto file and sends a final response from the `Info_Result` pack set, then exits.</summary>
            [____________R /*ÿ*/<Close, Info_Result>/*ÿ*/] /*ÿ*/
            struct Proto /*ą*/{ }
        }

        /// <summary>
        /// A simple, stateless connection for saving and restoring layout UID translations between the Agent and the virtual LayoutFile.
        /// `LR` indicates that both hosts can send and receive packs in the `Start` state without a state change.
        /// </summary>
        interface SaveLayout : Connects<Agent, LayoutFile>{
            [KeepDoc("🔠")] interface LayoutFilter<SCOPE>{ }

            // `_____lr_____` allows bidirectional peer communication in this state. The branch is
            // self-referencing; the state does not change after sending.
            [_____lr_____ /*ÿ*/<LayoutFilter<@AdHocProtocol>>/*ÿ*/] /*ÿ*/ //🔠
            struct Start /*ÿ*/{ }
        }

        /// <summary>
        /// Defines the persistent connection between the `Agent` and the `Observer`, allowing for
        /// interactive updates and commands for the visualizer.
        /// </summary>
        interface ObserverCommunication : Connects<Agent, Observer>{
            /// <summary>STATE 1/3: The Agent initiates the session by pushing layout info and/or the full project to the Observer for initial rendering.</summary>
            [L____________ /*ÿ*/<LayoutSent, LayoutFile.Info>/*ÿ*/] /*ÿ*/ // Branch 1: Agent sends layout info first, transitions to LayoutSent.
            [L____________ /*Ā*/<Operate, Agent.Project>/*Ā*/] /*Ā*/      // Branch 2: Agent sends the project directly, jumps to Operate.
            struct Start /*ÿ*/{ }

            /// <summary>A transient state ensuring the project is sent immediately after the layout information.</summary>
            [L____________ /*ÿ*/<Operate, Agent.Project>/*ÿ*/] /*ÿ*/
            struct LayoutSent /*Ā*/{ }

            [KeepDoc("⚙️")] interface LayoutFilter2<SCOPE>{ }

            /// <summary>STATE 2/3: The Observer is in an interactive state and can send commands (`Show_Code`, `Up_to_date`) to the Agent.</summary>
            [____________r /*ÿ*/<LayoutFilter2<@AdHocProtocol>>/*ÿ*/] /*ÿ*/       //⚙️ Branch 1: Observer sends commands (no state transition).
            [____________R /*Ā*/<RefreshProject, Observer.Up_to_date>/*Ā*/] /*Ā*/ // Branch 2: Observer requests a data refresh.
            struct Operate /*ā*/{ }

            /// <summary>STATE 3/3: The Agent responds to the Observer's update request with either the new project data or an "up-to-date" signal.</summary>
            [L____________ /*ÿ*/<Operate, (Agent.Project, Observer.Up_to_date)>/*ÿ*/] /*ÿ*/
            struct RefreshProject /*Ă*/{ }
        }


        /// <summary>
        /// The on-the-wire format of a file archive, and the single place it is defined.
        ///
        /// Every leg that hands an archive over as opaque bytes cuts the chain to the RIGHT of this flow, so the
        /// opaque side holds exactly what this declaration produces. That the legs agree is what makes a stored
        /// archive replayable: nothing on the wire announces the configuration, and a reader that ran a different
        /// one simply fails. Naming it once is what keeps them from drifting apart.
        ///
        /// Change it and every archive stored earlier stops decoding, all of them at once. While old archives must
        /// stay readable, declare a new flow beside this one instead of editing it.
        /// </summary>
        [Zstd]
        public class SrcZip : StreamFlowAttribute{ }

        public class FileEntry {
            /// '/'-separated archive-relative path.
            [D(+4096)] string path;

            /// Raw file content.
            [S(0x5_000_000)] Stream bytes;

            /// Carries no chain and no trim of its own. Several packs carry this list, each over its own leg, and
            /// each states at its own field what that leg needs: `Agent.Project.source`, `Server.Result.result`,
            /// `Monitoring.Management.Upload.response`. One declaration here would have to serve all three at once.
            public class  List {
                [D(0xFFFF)] FileEntry[,] files;
            }
        }
    }
}
