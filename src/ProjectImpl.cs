// Copyright 2025 Chikirev Sirguy, Unirail Group
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// For inquiries, please contact: al8v5C6HU4UtqE9@gmail.com
// GitHub Repository: https://github.com/AdHoc-Protocol

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using org.unirail.Meta;
using Project = org.unirail.Agent.AdHocProtocol.Agent_.Project;

// Reference to Microsoft.CodeAnalysis documentation: https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel?view=roslyn-dotnet-3.11.0
namespace org.unirail{
    /// <summary>
    /// Represents the implementation of a project, serving as the root entity for protocol description.
    /// </summary>
    public class ProjectImpl : Entity, Project{
        public ulong _uid => uid;

        /// <summary>
        /// Multiplexers declared in this project (after import merging: in this project and every project it imports).
        /// </summary>
        public List<MultiplexImpl> multiplexes = [];

        public object? _multiplex() => multiplexes.Count < 1 ?
                                           null :
                                           multiplexes;

        public int               _multiplex_len                                                             => multiplexes.Count;
        public Project.Multiplex _multiplex(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item) => multiplexes[item];

        /// <summary>
        /// UIDs of imported projects.
        /// </summary>
        public ulong[]? imported_projects_uid;


        /// <summary>
        /// Iterates through packs within the project scope, applying an action to transmittable packs.
        /// If depth is 0, includes only packs defined directly under the project (not inside a host).
        /// If depth > 0, recursively includes all packs within the project, including those inside hosts and other nested structures.
        /// </summary>
        /// <param name="depth">The depth of scope to traverse (0 for immediate project scope, >0 for deeper scopes).</param>
        /// <param name="dst">The action to apply to each transmittable pack.</param>
        public void for_packs_in_scope(uint depth, Action<HostImpl.PackImpl> dst)
        {
            foreach( var entity in entities.Values.Where(e => e.in_project == this && (0 < depth || e.in_host == null)) )
                if( entity is HostImpl.PackImpl pack && pack.is_transmittable )
                    dst(pack);
        }

        /// <summary>
        /// Dictionary to group related packets under descriptive names.
        /// Key: Symbol representing the named pack set interface.
        /// Value: NamedPackSet instance containing the grouped packs.
        /// </summary>
        public static readonly Dictionary<ISymbol, ConnectionImpl.NamedPackSet> named_packs = new(SymbolEqualityComparer.Default);

        /// <summary>
        /// The request and the response of every RPC shorthand method: Pack Set expressions, as a branch's PACKS is.
        /// <para>
        /// <see cref="EntityVisitor.VisitMethodDeclaration"/> runs during the syntactic AST walk: the packs and Pack Sets declared
        /// after the method are not known yet, and no <see cref="ConnectionImpl.NamedPackSet"/> has its <c>packs</c> yet. So each
        /// expression is recorded here with its synthesized branch and the entity whose source it is in, and resolved in phase
        /// 4.2.b by the resolver every other Pack Set goes through - after Pack Sets are built and before States (phase 4.3)
        /// consume the branches. The response's elements share one resolution: an `X&lt;…&gt;` in one removes from the others.
        /// </para>
        /// </summary>
        public static readonly List<(ConnectionImpl.BranchImpl branch, Entity resolver, SyntaxNode[] exprs, string what, int line)> rpc_deferred_packset_expansions = new();

        // List of packs that inject fields into other packs.
        public readonly List<HostImpl.PackImpl> injector_packs = [];

        // List of header modifiers.
        public readonly List<HostImpl.PackImpl> header_modifiers = [];


        // List of packs that serve as headers.
        public readonly List<HostImpl.PackImpl> header_packs = [];


        /// <summary>
        /// Start index of pack ID information in the source code. -1 indicates no information found yet.
        /// </summary>
        public int packs_id_info_start = -1;

        /// <summary>
        /// End index of pack ID information in the source code. -1 indicates no information found yet.
        /// </summary>
        public int packs_id_info_end = -1;

        /// <summary>
        /// File path of the project's source code file.
        /// </summary>
        public string file_path;

        /// <summary>
        /// List of all projects parsed in the current execution. The first project is considered the root project.
        /// </summary>
        public static readonly List<ProjectImpl> projects = [];

        /// <summary>
        /// Gets the root project, which is the first project in the <see cref="projects"/> list.
        /// </summary>
        public static ProjectImpl root_project => projects[0];

        // Metadata symbols for various meta-interfaces used in protocol description.
        static        INamedTypeSymbol Meta_HeaderFor;
        static        INamedTypeSymbol Meta_Binary;
        static        INamedTypeSymbol Meta_longJS;
        static        INamedTypeSymbol Meta_ulongJS;
        static        INamedTypeSymbol Meta_FieldsInjectInto;
        public static INamedTypeSymbol Meta_DateTimeDef;
        public static INamedTypeSymbol Meta_TimeSpanDef;
        public static INamedTypeSymbol Meta_Duration;

        internal static INamedTypeSymbol Meta_Connects;
        internal static INamedTypeSymbol Meta_VirtuallyConnects;
        internal static INamedTypeSymbol Meta_Multiplex;
        static          INamedTypeSymbol Meta_Modify_Target;
        static          INamedTypeSymbol Meta_Modify_Connection;
        static          INamedTypeSymbol Meta_Host;
        static          INamedTypeSymbol Meta_Stream_0;
        static          INamedTypeSymbol Meta_File_0;
        static          INamedTypeSymbol Meta_IfSendingFrom;
        static          INamedTypeSymbol Meta_Resumable; // Resumable<HOST, PACKS> inside a Connection — guaranteed delivery, see ResumableImpl
        internal static INamedTypeSymbol Meta_KeepName;
        internal static INamedTypeSymbol Meta_KeepDoc;
        internal static INamedTypeSymbol Meta_SkipName;
        internal static INamedTypeSymbol Meta_SkipDoc;

        // Stream transform-stage / flow attribute bases (org.unirail.Meta). An attribute (user-declared OR built-in
        // like Zstd/ChaCha20) that derives from one of these declares a byte-transform stage; StreamFlowAttribute
        // names a reusable ordered chain of stages.
        internal static INamedTypeSymbol Meta_StreamStage;
        internal static INamedTypeSymbol Meta_StreamCompression;
        internal static INamedTypeSymbol Meta_StreamCipher;
        internal static INamedTypeSymbol Meta_StreamFlow;

        // Trim markers (org.unirail.Meta). A trim occupies a POSITION in the same ordered attribute list as the stages
        // but transforms nothing: it cuts the chain there for one Endpoint, so that side holds bytes at that depth
        // instead of a typed value. See StreamTrimAttribute in Meta.cs.
        internal static INamedTypeSymbol Meta_StreamTrim;
        internal static INamedTypeSymbol Meta_Trim_ToStream_1;   // ToStreamAttribute<IfSendingFrom>
        internal static INamedTypeSymbol Meta_Trim_FromStream_1; // FromStreamAttribute<IfSendingFrom>
        internal static INamedTypeSymbol Meta_Trim_Stream_2;     // StreamAttribute<To, From> — both cuts at one depth

        // Dedup table: one shared chain container per distinct stage configuration (canonical key -> container pack).
        internal static readonly Dictionary<string, HostImpl.PackImpl> stream_chains = new();

        /// <summary>
        /// THE resolution of a trim's endpoint argument, cached per argument symbol. Both consumers read it from here:
        /// the early pass that records which legs each field is cut on (which drives the transmitting-pack distribution
        /// and the `Connection.ToStream`/`FromStream` projection) and <see cref="HasDocs.collect_stream_stages"/>, which
        /// puts the same codes into the trim's sub-pack. One argument is therefore resolved — and diagnosed — exactly
        /// once, however many declarations name it.
        /// <para>A cut rides as the connection index with its SIGN telling which host sends: <c>conn.idx</c> for the Left
        /// one, <c>~conn.idx</c> for the Right.</para>
        /// </summary>
        static readonly Dictionary<ITypeSymbol, (string show, List<(string name, long val)> encoded)> cut_endpoints = new(SymbolEqualityComparer.Default);

        internal static (string show, List<(string name, long val)> encoded) resolve_cut(ITypeSymbol arg, string side, string where, int line)
        {
            if( cut_endpoints.TryGetValue(arg, out var hit) ) return hit;

            var encoded = new List<(string name, long val)>();
            var show    = new List<string>();

            foreach( var (connection, host) in resolve_endpoints(arg, $"{side} trim", where, line) )
            {
                long code;
                if( host == connection.hostL && host == connection.hostR )
                {
                    // A host connected to itself sends from both sides of the connection - as the caller (Left, the side
                    // that dialed) and as the acceptor (Right) - so `IfSendingFrom<Host, Connection>` names both legs.
                    encoded.Add(($"{host._name}_via_{connection._name}",   connection.idx));
                    encoded.Add(($"{host._name}_via_{connection._name}_R", ~connection.idx));
                    show.Add($"{host._name}@{connection._name}");
                    continue;
                }

                if( host      == connection.hostL ) code = connection.idx;
                else if( host == connection.hostR ) code = ~connection.idx;
                else
                {
                    AdHocAgent.LOG.Warning("The {side} trim on {entity} (line {line}) names IfSendingFrom<{host}, {connection}>, but {host} is not a participant of {connection}; that endpoint is skipped.",
                                           side, where, line, host.symbol, connection.symbol);
                    continue;
                }

                encoded.Add(($"{host._name}_via_{connection._name}", code));
                show.Add($"{host._name}@{connection._name}");
            }

            if( encoded.Count == 0 )
                AdHocAgent.exit($"The {side} trim on {where} (line {line}) resolves to no usable endpoint, so the cut could never happen. Name an endpoint whose host actually participates in its connection.", 77);

            return cut_endpoints[arg] = (string.Join(", ", show), encoded);
        }

        /// <summary>
        /// Classifies a trim attribute type into its (ToStream endpoint, FromStream endpoint) argument pair. Shared by
        /// the early cut-recording pass and by the chain builder, so both read one definition of what a trim marker is.
        /// </summary>
        internal static (ITypeSymbol? to, ITypeSymbol? from) trim_of(INamedTypeSymbol t, string where, int line)
        {
            var def = t.OriginalDefinition;
            if( equals(def, Meta_Trim_ToStream_1) ) return (t.TypeArguments[0], null);
            if( equals(def, Meta_Trim_FromStream_1) ) return (null, t.TypeArguments[0]);
            if( equals(def, Meta_Trim_Stream_2) ) return (t.TypeArguments[0], t.TypeArguments[1]);

            AdHocAgent.exit($"'{t}' derives from StreamTrimAttribute but is not one of the recognized trim markers " +
                            $"(ToStream<E>, FromStream<E>, Stream<To, From>) on {where} (line {line}). Custom trims are not supported.", 2);
            return (null, null);
        }

        // Walk the base-type chain of t looking for base b (classifies a stage/flow attribute by its role base).
        internal static bool derives_from(INamedTypeSymbol? t, INamedTypeSymbol? b)
        {
            for( var x = t; x != null; x = x.BaseType )
                if( SymbolEqualityComparer.Default.Equals(x, b) )
                    return true;
            return false;
        }

        internal static bool is_stream_stage(INamedTypeSymbol? t) => derives_from(t, Meta_StreamStage);
        internal static bool is_stream_flow(INamedTypeSymbol?  t) => derives_from(t, Meta_StreamFlow);
        internal static bool is_stream_trim(INamedTypeSymbol?  t) => derives_from(t, Meta_StreamTrim);

        /// <summary>
        /// Resolves the endpoint argument of a directional declaration into concrete (Connection, Host) pairs.
        /// Accepts, recursively: a C# <b>tuple</b> of any of these forms (how several directions are declared at once),
        /// a named Endpoint Set (<see cref="EndpointSetImpl"/> — both the `_&lt;(…)&gt;` and the single-`IfSendingFrom`
        /// spelling), and a bare `IfSendingFrom&lt;Host, Connection&gt;`. Anything else is a hard error: an unresolvable
        /// endpoint silently strips the direction off a declaration, which is worse than refusing to build.
        /// </summary>
        internal static List<(ConnectionImpl connection, HostImpl host)> resolve_endpoints(ITypeSymbol arg, string what, string where, int line)
        {
            var dst = new List<(ConnectionImpl connection, HostImpl host)>();

            void walk(ITypeSymbol t)
            {
                if( t is INamedTypeSymbol nts )
                {
                    if( nts.IsTupleType ) // (FromA, FromB, …) — several endpoints in one argument
                    {
                        foreach( var element in nts.TupleElements ) walk(element.Type);
                        return;
                    }

                    if( entities.TryGetValue(nts, out var entity) && entity is EndpointSetImpl set ) // a named Endpoint / Endpoint Set
                    {
                        foreach( var endpoint in set.Endpoints )
                            if( !dst.Contains(endpoint) )
                                dst.Add(endpoint);
                        return;
                    }

                    if( equals(nts.OriginalDefinition, Meta_IfSendingFrom) ) // a bare IfSendingFrom<Host, Connection>
                        if( entities.TryGetValue(nts.TypeArguments[0], out var h) && h is HostImpl fromHost &&
                            entities.TryGetValue(nts.TypeArguments[1], out var c) && c is ConnectionImpl viaConnection )
                        {
                            var endpoint = (viaConnection, fromHost);
                            if( !dst.Contains(endpoint) ) dst.Add(endpoint);
                            return;
                        }
                }

                AdHocAgent.exit($"The {what} on {where} (line {line}) names '{t}', which is not an Endpoint. Expected `IfSendingFrom<Host, Connection>`, an interface declaring one, " +
                                "a named Endpoint Set, or a tuple of those — e.g. `(FromProducer, FromBackup)`.", 77);
            }

            walk(arg);
            return dst;
        }

        static INamedTypeSymbol Meta_End;
        static INamedTypeSymbol Meta_Close;

        /// <summary>
        /// Parser class responsible for traversing the C# syntax tree and extracting protocol description information.
        /// </summary>
        class Protocol_Description_Parser : CSharpSyntaxWalker{
            /// <summary>
            /// Currently processed entity during syntax tree traversal.
            /// </summary>
            public HasDocs? current;

            /// <summary>
            /// The project being parsed by this parser instance.
            /// </summary>
            ProjectImpl project;

            /// <summary>
            /// The Roslyn compilation object, providing semantic information about the code.
            /// </summary>
            readonly CSharpCompilation compilation;

            /// <summary>
            /// Initializes a new instance of the <see cref="Protocol_Description_Parser"/> class.
            /// </summary>
            /// <param name="compilation">The Roslyn compilation object.</param>
            public Protocol_Description_Parser(CSharpCompilation compilation) : base(SyntaxWalkerDepth.StructuredTrivia)
            {
                this.compilation = compilation;
                // Initialize metadata symbols for meta-interfaces.
                Meta_HeaderFor   = compilation.GetTypeByMetadataName("org.unirail.Meta.HeaderFor`1")!;
                Meta_Binary      = compilation.GetTypeByMetadataName("org.unirail.Meta.Binary")!;
                Meta_longJS      = compilation.GetTypeByMetadataName("org.unirail.Meta.longJS")!;
                Meta_ulongJS     = compilation.GetTypeByMetadataName("org.unirail.Meta.ulongJS")!;

                Meta_FieldsInjectInto = compilation.GetTypeByMetadataName("org.unirail.Meta.FieldsInjectInto`1")!;
                Meta_DateTimeDef      = compilation.GetTypeByMetadataName("org.unirail.Meta.DateTimeDef")!;
                Meta_TimeSpanDef      = compilation.GetTypeByMetadataName("org.unirail.Meta.TimeSpanDef")!;
                Meta_Duration         = compilation.GetTypeByMetadataName("org.unirail.Meta.Duration")!;

                Meta_Connects          = compilation.GetTypeByMetadataName("org.unirail.Meta.Connects`2")!;
                Meta_VirtuallyConnects = compilation.GetTypeByMetadataName("org.unirail.Meta.VirtuallyConnects`3")!;
                Meta_Multiplex         = compilation.GetTypeByMetadataName("org.unirail.Meta.Multiplex`1")!;
                Meta_Modify_Connection = compilation.GetTypeByMetadataName("org.unirail.Meta.Modify`3")!;
                Meta_Modify_Target     = compilation.GetTypeByMetadataName("org.unirail.Meta.Modify`1")!;

                Meta_Host = compilation.GetTypeByMetadataName("org.unirail.Meta.Host")!;

                Meta_Stream_0          = compilation.GetTypeByMetadataName("org.unirail.Meta.Stream")!;
                Meta_File_0            = compilation.GetTypeByMetadataName("org.unirail.Meta.File")!;
                Meta_IfSendingFrom     = compilation.GetTypeByMetadataName("org.unirail.Meta.IfSendingFrom`2")!;
                Meta_Resumable         = compilation.GetTypeByMetadataName("org.unirail.Meta.Resumable`2")!;
                Meta_KeepName          = compilation.GetTypeByMetadataName("org.unirail.Meta.KeepNameAttribute")!;
                Meta_KeepDoc           = compilation.GetTypeByMetadataName("org.unirail.Meta.KeepDocAttribute")!;
                Meta_SkipName          = compilation.GetTypeByMetadataName("org.unirail.Meta.SkipNameAttribute")!;
                Meta_SkipDoc           = compilation.GetTypeByMetadataName("org.unirail.Meta.SkipDocAttribute")!;
                Meta_StreamStage       = compilation.GetTypeByMetadataName("org.unirail.Meta.StreamStageAttribute")!;
                Meta_StreamCompression = compilation.GetTypeByMetadataName("org.unirail.Meta.StreamCompressionStageAttribute")!;
                Meta_StreamCipher      = compilation.GetTypeByMetadataName("org.unirail.Meta.StreamCipherStageAttribute")!;
                Meta_StreamFlow        = compilation.GetTypeByMetadataName("org.unirail.Meta.StreamFlowAttribute")!;
                Meta_StreamTrim        = compilation.GetTypeByMetadataName("org.unirail.Meta.StreamTrimAttribute")!;
                Meta_Trim_ToStream_1   = compilation.GetTypeByMetadataName("org.unirail.Meta.ToStreamAttribute`1")!;
                Meta_Trim_FromStream_1 = compilation.GetTypeByMetadataName("org.unirail.Meta.FromStreamAttribute`1")!;
                Meta_Trim_Stream_2     = compilation.GetTypeByMetadataName("org.unirail.Meta.StreamAttribute`2")!;

                Meta_End   = compilation.GetTypeByMetadataName("org.unirail.Meta.End")!;
                Meta_Close = compilation.GetTypeByMetadataName("org.unirail.Meta.Close")!;
            }

            /// <summary>
            /// The namespace of the currently visited namespace declaration.
            /// </summary>
            string namespace_ = "";

            /// <summary>
            /// Documentation of the currently visited namespace declaration.
            /// </summary>
            string namespace_doc = "";

            public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
            {
                namespace_doc = node.GetLeadingTrivia().Aggregate("", (current, trivia) => current + get_doc(trivia));

                namespace_ = node.Name.ToString();
                base.VisitFileScopedNamespaceDeclaration(node);
            }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                namespace_doc = node.GetLeadingTrivia().Aggregate("", (current, trivia) => current + get_doc(trivia));

                namespace_ = node.Name.ToString();
                base.VisitNamespaceDeclaration(node);
            }


            public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                var model  = compilation.GetSemanticModel(node.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(node)!;

                if( symbol.OriginalDefinition.ContainingType == null ) //The top-level C# interface serves as the project’s declaration.
                {
                    current = project = new(projects.Count == 0 //root project
                                                ?
                                                null :
                                                projects[0], compilation, node, namespace_);

                    projects.Add(project);

                    if( !string.IsNullOrEmpty(namespace_doc) )
                    {
                        project._doc  = namespace_doc + project._doc;
                        namespace_doc = "";
                    }
                }
                else
                {
                    // Validate regex patterns immediately at declaration site for BOTH templates and named sets
                    var filterAttrs = new[]
                                      {
                                          (Meta_KeepName, "KeepName"), (Meta_KeepDoc, "KeepDoc"),
                                          (Meta_SkipName, "SkipName"), (Meta_SkipDoc, "SkipDoc")
                                      };

                    void ValidateRegexPatterns()
                    {
                        foreach( var (metaSym, attrLabel) in filterAttrs )
                        {
                            foreach( var attr in symbol.GetAttributes().Where(a => equals(a.AttributeClass, metaSym)) )
                            {
                                var pattern = attr.ConstructorArguments[0].Value as string;
                                if( string.IsNullOrWhiteSpace(pattern) )
                                {
                                    AdHocAgent.LOG.Error(
                                                         "Entity '{name}' (line {line}): " +
                                                         "[{attr}] has an empty or null regex pattern.",
                                                         symbol.Name,
                                                         node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                                                         attrLabel);
                                    AdHocAgent.exit("Provide a valid regex pattern and rerun.");
                                }

                                try { new Regex(pattern); }
                                catch( ArgumentException ex )
                                {
                                    AdHocAgent.LOG.Error(
                                                         "Entity '{name}' (line {line}): " +
                                                         "[{attr}(\"{pattern}\")] contains an invalid regex: {msg}",
                                                         symbol.Name,
                                                         node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                                                         attrLabel, pattern, ex.Message);
                                    AdHocAgent.exit("Fix the regex pattern and rerun.");
                                }
                            }
                        }
                    }

                    // Parameterized Generic Interfaces act as Filter Templates (e.g. MyFilter<SCOPE>)
                    // They should NOT be instantiated as concrete Packs or Named Sets in the protocol tree directly.
                    if( symbol.TypeParameters.Length > 0 )
                    {
                        if( symbol.TypeParameters.Length != 1 )
                        {
                            // Explain WHY it's treated as a filter template (the filter attributes)
                            // and show both the wrong form and the correct fix.
                            var filterAttrsPresent = symbol
                                                     .GetAttributes()
                                                     .Any(a => equals(a.AttributeClass, Meta_KeepName) ||
                                                               equals(a.AttributeClass, Meta_KeepDoc)  ||
                                                               equals(a.AttributeClass, Meta_SkipName) ||
                                                               equals(a.AttributeClass, Meta_SkipDoc));

                            AdHocAgent.LOG.Error(
                                                 "Filter template '{name}' (line {line}) declares {count} generic parameters, "                     +
                                                 "but a filter template must have exactly ONE generic parameter — the scope placeholder.\n\n"       +
                                                 "A generic interface decorated with[KeepName], [KeepDoc], [SkipName], or [SkipDoc] "               +
                                                 "is automatically treated as a filter template. "                                                  +
                                                 "Filter templates define HOW to filter; the caller supplies WHAT to filter at the usage site.\n\n" +
                                                 "  Wrong:   interface {name}<A, B> {{ }}\n"                                                        +
                                                 "  Correct: interface {name}<SCOPE> {{ }}\n\n"                                                     +
                                                 "If this interface is not intended to be a filter template, remove the filter attributes.\n"       +
                                                 "If it is, collapse the {count} parameters into a single <SCOPE> parameter and use "               +
                                                 "tuple syntax at the call site to pass multiple scopes:\n"                                         +
                                                 "  interface SomeState : l__________< {name}<(@Project, @Host.Lib)> > {{ }}",
                                                 symbol.Name,
                                                 node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                                                 symbol.TypeParameters.Length,
                                                 symbol.Name,
                                                 symbol.TypeParameters.Length,
                                                 symbol.Name);

                            AdHocAgent.exit("Correct the filter template declaration and rerun.");
                        }

                        ValidateRegexPatterns();
                        return; // valid filter template — not registered as a concrete entity
                    }

                    ValidateRegexPatterns();

                    for( var sym = symbol;; )
                    {
                        var interfaces = sym.Interfaces;
                        if( 0 < interfaces.Length && interfaces[0].isMeta() )
                            switch( interfaces[0].Name )
                            {
                                case "Modify":
                                    sym = (INamedTypeSymbol)interfaces[0].TypeArguments[0];
                                    continue;
                                case "Connects":
                                case "VirtuallyConnects":
                                    current = new ConnectionImpl(project, compilation, node);
                                    break;
                                case "Multiplex":
                                    current = new MultiplexImpl(project, compilation, node);
                                    break;

                                case "Actor":
                                    if( in_Actor(symbol) != null )
                                        AdHocAgent.exit($"The Actor {symbol} at line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1} cannot be declared inside another Actor.");
                                    current = new ConnectionImpl.ActorImpl(project, compilation, node);
                                    break;
                                case "IfSendingFrom":
                                    // A named, single Endpoint: `interface FromProducer : IfSendingFrom<Host, Connection> {}`.
                                    // It is an Endpoint Set of exactly one endpoint, registered like the `_<(...)>` form so it
                                    // can be referenced directly by a `[ToStream<>]`/`[FromStream<>]`/`[Stream<,>]` trim AND composed
                                    // inside larger `_<(...)>` Endpoint Sets. Resolution of its single `IfSendingFrom<>` base
                                    // happens in EndpointSetImpl.modify (via modify_by_implement's `IfSendingFrom` handling).
                                    current = new EndpointSetImpl(project, compilation, node);
                                    project.endpoint_sets.Add((EndpointSetImpl)current);
                                    break;
                                case "Resumable":
                                    // `interface Name : Resumable<HOST, PACKS> { … }` inside a Connection — guaranteed delivery of PACKS
                                    // when HOST sends them there. Registered as a header pack (see ResumableImpl); PACKS is collected
                                    // into `target_packs` by the ordinary header-init pass, HOST is checked once the hosts are known.
                                    current = new ResumableImpl(project, compilation, node);
                                    break;
                                case "_":
                                    // Differentiate between a Pack Set and an Endpoint Set.
                                    // Both use the `_<>` syntax, but they have different semantic meanings.
                                    // An Endpoint Set contains `IfSendingFrom<>` and should NOT be treated as a NamedPackSet.

                                    // Its elements say which: `IfSendingFrom<…>`, a named endpoint (`FromX : IfSendingFrom<…>`) or another Endpoint
                                    // Set, alone or in a tuple - `_<(FromCamera, FromRecorder)>`. The guard stops a cycle of sets naming each other.
                                    var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

                                    bool endpoint(ITypeSymbol arg) => arg is INamedTypeSymbol nts &&
                                                                      (nts.IsTupleType ?
                                                                           nts.TupleElements.Any(e => endpoint(e.Type)) :
                                                                           equals(nts.OriginalDefinition, Meta_IfSendingFrom) ||
                                                                           nts.TypeKind == TypeKind.Interface && seen.Add(nts) &&
                                                                           nts.Interfaces.Any(i => equals(i.OriginalDefinition, Meta_IfSendingFrom) ||
                                                                                                   i.isMeta() && i.Name == "_" && i.TypeArguments.Any(endpoint)));

                                    if( interfaces[0].TypeArguments.Any(endpoint) )
                                    {
                                        current = new EndpointSetImpl(project, compilation, node);
                                        project.endpoint_sets.Add((EndpointSetImpl)current);
                                    }
                                    else
                                    {
                                        // This is a true Pack Set.
                                        named_packs.Add(symbol, new(project, compilation, node));
                                        current = null;
                                    }

                                    break;
                                case "l____________":
                                case "L____________":
                                case "____________r":
                                case "____________R":
                                    AdHocAgent.exit($"The error declaration {symbol} at the line: {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}, Expected 'struct' instead of 'interface'.");
                                    return;
                                default:
                                    current = null;
                                    break;
                            }
                        else
                            new HostImpl.PackImpl(project, compilation, node)._included = true; //Actors Hierarchy holder

                        break;
                    }
                }

                base.VisitInterfaceDeclaration(node);
            }

            public override void VisitMethodDeclaration(MethodDeclarationSyntax method)
            {
                var model  = compilation.GetSemanticModel(method.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(method) as IMethodSymbol;

                if( symbol == null ) return;

                var connSym = in_Connection(symbol);
                if( connSym == null )
                {
                    base.VisitMethodDeclaration(method);
                    return; // Ignore methods declared outside connection scopes
                }

                var connection = entities[connSym] as ConnectionImpl;
                if( connection == null ) return;

                // Grab the exact line number for precise error reporting
                var line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                if( in_Actor(symbol) != null )
                    AdHocAgent.exit($"Line {line}: RPC shorthand method '{symbol.Name}' cannot be declared inside another Actor.");

                // Only RPC methods with a non-void return type qualify as shorthand.
                // Fire-and-forget patterns must use the full FSM declaration.
                var returnType = symbol.ReturnType;
                if( returnType.SpecialType == SpecialType.System_Void )
                    AdHocAgent.exit($"Line {line}: Method '{symbol.Name}' inside a Connection has a void return type. " +
                                    $"Only RPC methods with a return type are valid as shorthand. "                     +
                                    $"Use the full FSM declaration (Actor / State / Branch) for fire-and-forget patterns.");

                string direction = null; // "L" or "R"

                // The request and the response are Pack Set expressions, as a branch's PACKS is: a pack, a Pack Set, a
                // Project/Host/Pack scope with or without `@`, `_<…>`, `X<…>`, a filter template, or a tuple of those. They are
                // resolved in phase 4.2.b, when every pack and Pack Set is known - see rpc_deferred_packset_expansions.
                List<SyntaxNode> responseExprs = new();

                // 1. Parse Return Type for Direction and Response Payload
                if( method.ReturnType is TupleTypeSyntax returnTuple )
                    foreach( var elem in returnTuple.Elements )
                        switch( model.GetTypeInfo(elem.Type).Type?.Name )
                        {
                            case "____________R":
                                direction = "R";
                                break;
                            case "L____________":
                                direction = "L";
                                break;
                            case "____________r":
                            case "l____________":
                                AdHocAgent.exit($"Line {line}: Follower direction marker '{elem.Type}' cannot be used in RPC shorthand method '{symbol.Name}'. " +
                                                $"RPC request/response flows inherently require state transitions, which demand Master authority. "            +
                                                $"Please use the Master markers ('L____________' or '____________R') instead.");
                                break;
                            default:
                                responseExprs.Add(elem.Type);
                                break;
                        }
                else
                    switch( returnType.Name )
                    {
                        // A bare non-tuple return must be the response itself — a direction marker alone is not valid shorthand.
                        case "____________R":
                        case "L____________":
                            AdHocAgent.exit($"Line {line}: Method '{symbol.Name}' returns only a direction marker '{returnType.Name}' with no response Pack. " +
                                            $"Shorthand requires at least one response Pack in the return type. "                                              +
                                            $"Use a tuple return, e.g. '(____________R, YourResponsePack) {symbol.Name}(...)'.");
                            break;
                        case "____________r":
                        case "l____________":
                            AdHocAgent.exit($"Line {line}: Method '{symbol.Name}' returns only a follower direction marker '{returnType.Name}'. " +
                                            $"Shorthand requires at least one response Pack and a Master marker ('L____________' or '____________R') in a tuple.");
                            break;
                        default:
                            responseExprs.Add(method.ReturnType);
                            break;
                    }

                // Shorthand is only for RPC: a response must be there. What it resolves to is checked in phase 4.2.b.
                if( responseExprs.Count == 0 )
                    AdHocAgent.exit($"Line {line}: RPC shorthand method '{symbol.Name}' has no response Packs in its return type. " +
                                    $"A direction marker alone is not a valid shorthand return. "                                   +
                                    $"Include at least one Pack type, e.g. '(____________R, YourResponsePack) {symbol.Name}(...)'.");

                // 2. Parse Parameter List for Request Payload: one parameter, a Pack Set expression.
                // The user must explicitly declare and pass a NoArg (or any empty) pack for no-argument calls.
                if( symbol.Parameters.Length == 0 )
                    AdHocAgent.exit($"Line {line}: Shorthand RPC method '{symbol.Name}' has no parameters. "                             +
                                    $"To express a no-argument call, declare an empty class (e.g. 'class NoArg {{ }}') in your project " +
                                    $"and pass it as the sole parameter: '{symbol.Name}(NoArg _)'.");
                else if( symbol.Parameters.Length != 1 )
                    AdHocAgent.exit($"Line {line}: Shorthand RPC method '{symbol.Name}' must have exactly 1 parameter "       +
                                    $"(use a Tuple for multiple OR-packs, or a declared NoArg pack for a no-argument call). " +
                                    $"Found {symbol.Parameters.Length} parameters.");

                SyntaxNode[] requestExprs = [method.ParameterList.Parameters[0].Type!];

                var parsedUids = new List<ulong>();
                var marks      = new List<TextSpan>();
                foreach( var t in method.Identifier.TrailingTrivia )
                {
                    if( t.IsKind(SyntaxKind.MultiLineCommentTrivia) )
                    {
                        var m = org.unirail.HasDocs._uid.Match(t.ToString());
                        if( m.Success )
                        {
                            var arr = m.Groups[1].Value.Split(' ').Select(str => str.to_base256_value()).ToArray();
                            parsedUids.Add(arr[0]);
                            marks.Add(t.Span);
                        }
                    }
                }

                var uidIdx = 0;

                void CreateRPCActor(string actorName, string direction)
                {
                    var actor = new ConnectionImpl.ActorImpl(project, null, method, false)
                                {
                                    model               = model,
                                    parent_artificial   = entities[symbol.ContainingType], // support hierarchy
                                    _name               = actorName,
                                    _MaxActiveInstances = int.MaxValue,              // UNLIMITED optimization
                                    _fake_uid_pos       = method.Identifier.Span.End // The Actor remains persistent
                                };

                    if( uidIdx < parsedUids.Count )
                    {
                        actor.uid_marks.Add(marks[uidIdx]);
                        actor.uid      = parsedUids[uidIdx++];
                    }

                    ((HasDocs)actor).symbol = symbol;
                    connection.actors.Add(actor);

                    // Add to entities map if missing (Bidirectional uses 2 actors, we map only the first to the method symbol)
                    if( !entities.ContainsKey(symbol) ) entities.Add(symbol, actor);

                    // Initialize internal States and Branches
                    var stateCall = new ConnectionImpl.StateImpl(project, null, method)
                                    {
                                        _name             = "Call",
                                        parent_artificial = actor,
                                        _fake_uid_pos     = -1, //  Do not persist to source
                                        uid               = 0   //  Hardcoded deterministic UID
                                    };
                    actor.states.Add(stateCall);


                    // Response packs are always present (enforced before CreateRPCActor is called),
                    // so we always produce a two-state (Call → Return → End) FSM.
                    var stateReturn = new ConnectionImpl.StateImpl(project, null, method)
                                      {
                                          _name             = "Return",
                                          parent_artificial = actor,
                                          _fake_uid_pos     = -1, //Do not persist to source
                                          uid               = 1   //Hardcoded deterministic UID
                                      };
                    actor.states.Add(stateReturn);

                    // Ensure the shared End sentinel exists.
                    ConnectionImpl.StateImpl.End ??= new ConnectionImpl.StateImpl(root_project) { _name = "End", symbol = Meta_End };

                    var callBranch = new ConnectionImpl.BranchImpl(project)
                                     {
                                         Side              = direction,
                                         IsMasterAuthority = true,
                                         IsTransitional    = true,
                                         packs             = [], // phase 4.2.b
                                         uid_pos           = -1, // Do not persist to source
                                         uid               = 0,  // Hardcoded deterministic UID
                                         goto_state        = stateReturn
                                     };
                    (direction == "L" ?
                         stateCall.branchesL :
                         stateCall.branchesR).Add(callBranch);

                    var returnBranch = new ConnectionImpl.BranchImpl(project)
                                       {
                                           Side = direction == "L" ?
                                                      "R" :
                                                      "L",
                                           IsMasterAuthority = true,
                                           IsTransitional    = true,
                                           packs             = [], // phase 4.2.b
                                           uid_pos           = -1, // Do not persist to source
                                           uid               = 0,  // Hardcoded deterministic UID (0 because it's unique in its state)
                                           goto_state        = ConnectionImpl.StateImpl.End
                                       };
                    (direction == "R" ?
                         stateReturn.branchesL :
                         stateReturn.branchesR).Add(returnBranch);

                    // The request and the response are resolved in phase 4.2.b, when every pack and Pack Set is known, and before
                    // States (phase 4.3) consume the branches. For a bidirectional RPC this runs once per synthesized actor, so each
                    // side gets its own resolved pack list.
                    rpc_deferred_packset_expansions.Add((callBranch, connection, requestExprs, $"request of the RPC '{symbol.Name}'", line));
                    rpc_deferred_packset_expansions.Add((returnBranch, connection, responseExprs.ToArray(), $"response of the RPC '{symbol.Name}'", line));
                }

                var isBidirectional = direction == null;

                // 3. Synthesize the FSM Blocks
                if( isBidirectional )
                {
                    CreateRPCActor(symbol.Name + "L", "L");
                    CreateRPCActor(symbol.Name + "R", "R");
                }
                else CreateRPCActor(symbol.Name, direction);

                base.VisitMethodDeclaration(method);
            }


            /// <summary>
            /// Traverses the symbol hierarchy upwards to determine if the given symbol is declared
            /// within the body of a Connection interface.
            /// </summary>
            /// <param name="symbol">The symbol to check.</param>
            /// <returns>The ISymbol representing the enclosing Connection, or null if not inside a Connection.</returns>
            public static ISymbol? in_Connection(ISymbol? symbol)
            {
                for( var current = symbol?.ContainingType; current != null; current = current.ContainingType )
                    foreach( var iface in current.AllInterfaces )
                        if( SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, Meta_Connects) ||
                            SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, Meta_VirtuallyConnects) )
                            return current;

                return null;
            }

            public static ISymbol? in_Actor(ISymbol? symbol)
            {
                for( var current = symbol?.ContainingType; current != null; current = current.ContainingType )
                    foreach( var iface in current.AllInterfaces )
                        if( iface.Name == "Actor" && iface.isMeta() )
                            return current;

                return null;
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                var model      = compilation.GetSemanticModel(node.SyntaxTree);
                var symbol     = model.GetDeclaredSymbol(node)!;
                var interfaces = symbol.Interfaces;

                var conn = in_Connection(symbol);

                // A state struct is identified by the presence of one or more branch attributes
                // ([l____________], [L____________<Target>], [____________r], [____________R<Target>], [_____lr_____]).
                var isState = symbol.GetAttributes().Any(a => ProjectImpl.IsBranchAttribute(a.AttributeClass));

                if( isState )
                {
                    if( conn == null ) AdHocAgent.exit($"The State '{symbol.Name}' (line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}) must be declared inside a Connection/Actor body.", 22);

                    // A state struct is purely a carrier for branch attributes — it MUST be empty.
                    if( node.Members.Count > 0 )
                        AdHocAgent.exit($"The state struct '{symbol.Name}' (line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}) must be empty — states cannot contain fields, methods, or nested types. Move any members outside the state and express communication exclusively via branch attributes.", 22);

                    current = new ConnectionImpl.StateImpl(project, compilation, node); //Actor state
                }
                else
                {
                    if( conn != null )
                        AdHocAgent.exit($"The entity '{symbol.Name}' (line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}) cannot be declared inside a Connection body. But it declared within Connection {conn}", 22);

                    if( entities.GetValueOrDefault(symbol.ContainingType) is ProjectImpl && //directly in the project scope
                        interfaces.Any(i =>
                                           equals(i.ConstructedFrom, Meta_Host) ||
                                           equals(i.ConstructedFrom, Meta_Modify_Target) && i.TypeArguments[0].Interfaces.Any(ii => equals(ii.ConstructedFrom, Meta_Host))
                                      )
                      )
                        current = new HostImpl(project, compilation, node); //host
                    else if( 0 < interfaces.Length )
                        AdHocAgent.exit($"Unknown struct {symbol} (line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}) entity type. If it is a Host, it should extend 'org.unirail.Meta.Host'. If it is a Host Modifier, it should extend 'org.unirail.Meta.Modify'.");
                    else
                        current = new HostImpl.PackImpl(project, compilation, node); //explicitly assigned a constant collection
                }

                base.VisitStructDeclaration(node);
            }


            public override void VisitClassDeclaration(ClassDeclarationSyntax clazz)
            {
                var model  = compilation.GetSemanticModel(clazz.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(clazz)!;

                var conn = in_Connection(symbol);
                if( !symbol.Interfaces.Any(i => equals(i.ConstructedFrom, Meta_HeaderFor)) && conn != null )
                    AdHocAgent.exit($"The entity '{symbol.Name}' (line {clazz.GetLocation().GetLineSpan().StartLinePosition.Line + 1}) cannot be declared inside a Connection body. But it declared within Connection {conn}", 22);

                current = new HostImpl.PackImpl(project, compilation, clazz);
                base.VisitClassDeclaration(clazz);
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax ENUM)
            {
                var model  = compilation.GetSemanticModel(ENUM.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(ENUM)!;
                var conn   = in_Connection(symbol);

                if( conn != null )
                    AdHocAgent.exit($"The entity '{symbol.Name}' (line {ENUM.GetLocation().GetLineSpan().StartLinePosition.Line + 1}) cannot be declared inside a Connection body. But it declared within Connection {conn}", 22);
                current = new HostImpl.PackImpl(project, compilation, ENUM);

                base.VisitEnumDeclaration(ENUM);
            }


            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);

                foreach( var variable in node.Declaration.Variables )
                    current = model.GetDeclaredSymbol(variable) is IFieldSymbol fld && (fld.IsStatic || fld.IsConst) ?
                                  new HostImpl.PackImpl.ConstantImpl(project, node, variable, model) :
                                  new HostImpl.PackImpl.FieldImpl(project, node, variable, model);

                base.VisitFieldDeclaration(node);
            }


            public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);

                current = new HostImpl.PackImpl.ConstantImpl(project, node, model);
                base.VisitEnumMemberDeclaration(node);
            }


            public override void VisitTrivia(SyntaxTrivia trivia)
            {
                if( trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) )
                    if( current != null && current.line_in_src_code == trivia.GetLocation().GetMappedLineSpan().StartLinePosition.Line + 1 )
                        current._inline_doc += trivia.ToString().Trim('\r', '\n', '\t', ' ', '/');

                base.VisitTrivia(trivia);
            }


            /// <summary>
            /// Returns the raw text appearing after a Dashboard `<see .../>` element up to whichever comes
            /// first: the next newline OR the next `<` character (start of another XML element). The
            /// `<`-stop matters when prior buggy runs collapsed the Dashboard into a single physical line —
            /// we must still read only THIS entry's tag, not the trailing entries' markup.
            /// </summary>
            static string ExtractTrailingUserText(XmlEmptyElementSyntax commentLine)
            {
                var tree    = commentLine.SyntaxTree;
                var srcText = tree.GetText();
                var start   = commentLine.Span.End;
                var line    = srcText.Lines.GetLineFromPosition(start);
                var end     = line.End;
                if( end <= start ) return "";

                // Clip at the next `<` so we never read past the end of this entry.
                var scan             = srcText.ToString(new TextSpan(start, end - start));
                var ltIx             = scan.IndexOf('<');
                if( ltIx >= 0 ) scan = scan[..ltIx];

                // Strip `/`, `*`, surrounding whitespace — these are doc-comment filler, not user intent.
                return scan.Trim().Trim('/', '*', ' ', '\t');
            }

            void read_number(XmlEmptyElementSyntax line, ISymbol cref, string kind)
            {
                var at   = line.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                var prj  = (ProjectImpl)entities[compilation.GetSemanticModel(line.SyntaxTree).GetDeclaredSymbol(((DocumentationCommentTriviaSyntax)line.Parent!).ParentTrivia.Token.Parent!)!];
                var text = ExtractTrailingUserText(line);

                static int run(string s, int from)
                {
                    while( from < s.Length && 'ÿ' <= s[from] && s[from] <= 'ǿ' ) from++;
                    return from;
                }

                // `number[ number…][ ~was][ tags]`. The packs are numbered in decimal: `number[ id][ ~was]` - the number, then
                // the id it goes on the wire under, if it does. A project line carries its imports, a bidirectional RPC one
                // number per actor, a virtual connection its wire id in decimal. An imported pack sent under an id of this
                // project's own is listed by that id alone: `id[ tags]`.
                var decimal_ = kind is ProjectImpl.TABLE_PACKS or ProjectImpl.TABLE_IMPORTED;

                // a pack's number as it was first written in these tables, base256, is read too - and written in decimal
                bool number(string w) => decimal_ && w.All(char.IsAsciiDigit) || run(w, 0) == w.Length;

                ulong value(string w) => decimal_ && w.All(char.IsAsciiDigit) ?
                                             ulong.Parse(w) :
                                             w.to_base256_value();

                List<ulong> list  = [];
                var         was   = ulong.MaxValue;
                var         id    = -1;
                var         words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var         w     = 0;
                for( ; w < words.Length; w++ )
                {
                    var word = words[w];
                    if( number(word) ) list.Add(value(word));
                    else if( word[0] == '~' && 1 < word.Length && number(word[1..]) ) was = value(word[1..]); // the number it had
                    else if( !decimal_ && id == -1 && word.All(char.IsAsciiDigit) ) id = int.Parse(word);   // a virtual connection's wire id
                    else break;                                                                           // the tags
                }

                switch( kind )
                {
                    case ProjectImpl.TABLE_PACKS when 1 < list.Count: // the number, the wire id
                        id   = (int)list[1];
                        list = [list[0]];
                        break;
                    case ProjectImpl.TABLE_IMPORTED when list.Count == 1: // the wire id alone
                        id   = (int)list[0];
                        list = [];
                        break;
                }

                if( list.Count == 0 && id == -1 )
                    AdHocAgent.exit($"The numbers table line `{line}` (line {at}) has no number after `/>`.");

                var tags = string.Join(' ', words[w..]);
                if( !prj.numbers.TryAdd(cref, ([.. list], was, tags, at)) )
                    AdHocAgent.exit($"The numbers table lists {cref} (line {at}) more than once.");

                // the wire id and the tags of a pack live here now: the rest of the parser reads them where it always did
                if( id != -1 && cref is INamedTypeSymbol type ) prj.pack_id_info[type] = id;
                if( 0 < tags.Length ) prj.pack_user_tags[cref] = tags;
            }

            public override void VisitXmlCrefAttribute(XmlCrefAttributeSyntax node)
            {
                var comment_line = (XmlEmptyElementSyntax)node.Parent!;

                var model = compilation.GetSemanticModel(node.Cref.SyntaxTree);
                var cref  = model.GetSymbolInfo(node.Cref).Symbol;

                if( comment_line.Parent is DocumentationCommentTriviaSyntax table && ProjectImpl.numbers_table(table) != null && ProjectImpl.numbers_section(comment_line) is { } kind )
                {
                    if( cref == null )
                        AdHocAgent.exit($"The numbers table line `{comment_line}` (line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}) refers to `{node.Cref}`, which is not there. " +
                                        "Renamed by hand? Rename it in the line too, or its number - its identity - is lost. Deleted? Delete the line.");

                    read_number(comment_line, cref!, kind);
                    goto END;
                }

                if( cref == null )
                    AdHocAgent.exit($"In meta information `{comment_line}` (line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}) the reference to `{node.Cref.ToString()}` on `{current}` is unreachable. ");

                current._doc = current._doc?.Replace(comment_line.Parent!.GetText().ToString(), "");


#region reading of the project saved packs id info
                // `packs_id_info_end` is pre-seeded by HasDocs()' constructor to the project's own source
                // position on first entity construction. That means every cref appearing before the
                // project's `public interface X { }` declaration — i.e. inside the Dashboard `/** ... */`
                // block — passes this guard. The block then narrows _start/_end to the actual doc comment.
                if( node.SpanStart < project.packs_id_info_end )
                    switch( cref!.Kind )
                    {
                        case SymbolKind.NamedType:
                            if( project.packs_id_info_start == -1 )
                            {
                                project.packs_id_info_start = comment_line.Parent.Span.Start - 3; //-3 cut `/**`
                                project.packs_id_info_end   = comment_line.Parent.Span.End;
                            }

                            // Capture `id='N'` when present; Dashboard lines without an id are still
                            // recorded so their trailing user text is preserved. Duplicate cref lines
                            // (e.g. left over from a corrupted rewrite) are silently accepted — the last
                            // `id` wins.
                            if( comment_line.Attributes.Count > 1 && comment_line.Attributes[1] is XmlTextAttributeSyntax idAttr )
                                if( int.TryParse(idAttr.TextTokens.ToString(), out var id) )
                                    project.pack_id_info[(INamedTypeSymbol)cref] = id;

                            // Capture everything after `/>` up to end-of-line as unified-pool text.
                            // Preserves user tags/comments even for packs without an id.
                            var trailing = ExtractTrailingUserText(comment_line);
                            if( !string.IsNullOrEmpty(trailing) )
                                project.pack_user_tags[cref!] = trailing;

                            break;
                        default:
                            AdHocAgent.exit($"Packs id info (line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}) contains reference to unknown entity {node}");
                            break;
                    }
#endregion
                else
                {
                    if( current is HostImpl host ) //language &  generate/skip implementation at current lang config
                    {
                        var lang = node.Cref.ToString() switch
                                   {
                                       "InCS"   => (ushort)Project.Host.Langs.InCS,
                                       "InGO"   => (ushort)Project.Host.Langs.InGO,
                                       "InRS"   => (ushort)Project.Host.Langs.InRS,
                                       "InTS"   => (ushort)Project.Host.Langs.InTS,
                                       "InCPP"  => (ushort)Project.Host.Langs.InCPP,
                                       "InJAVA" => (ushort)Project.Host.Langs.InJAVA,
                                       _        => (ushort)0
                                   };
                        if( 0 < lang )
                        {
#region read host language configuration and create a new scope
                            host._langs |= (Project.Host.Langs)lang; //register language config
                            var txt = comment_line.Parent!.DescendantNodes().FirstOrDefault(t => node.Span.End < t.Span.Start)?.ToString().Trim() ?? "";

                            // The modifier is the one or two `+`/`-` characters right after the marker. Anything else on
                            // the line - a trailing `//` comment, most often - is not a modifier, and a marker written
                            // without one means `++`, the documented default. (Reading the first character blindly made
                            // a commented marker inherit the previous setting instead, quietly.)
                            var mod  = 0 < txt.Length && (txt[0] == '+' || txt[0] == '-') ?
                                           txt :
                                           "";
                            var impl = 0 < mod.Length ?
                                           mod[0] :
                                           '+';
                            var hash = 1 < mod.Length && (mod[1] == '+' || mod[1] == '-') ?
                                           mod[1] :
                                           '+';

                            host._default_impl_hash_equal = impl switch
                                                            {
                                                                '-' => host._default_impl_hash_equal & ~(uint)lang, // Clear Implementation bit (Low)
                                                                _   => host._default_impl_hash_equal | lang,        // Set Implementation bit (Low)
                                                            };

                            host._default_impl_hash_equal = hash switch
                                                            {
                                                                '-' => host._default_impl_hash_equal & ~(uint)(lang << 16), // Clear Hash/Equals bit (High)
                                                                _   => host._default_impl_hash_equal | (uint)(lang << 16),  // Set Hash/Equals bit (High)
                                                            };

                            // Create and add the new language scope to the host
                            host.LangScopes.Add(new() { Config = host._default_impl_hash_equal });
#endregion
                            goto END;
                        }

#region Add the cref target to the current language scope
                        if( cref == null ) AdHocAgent.exit($"`Reference to unknown entity {node.Cref} (line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}) on {host} host configuration detected.");

                        if( host.LangScopes.Count != 0 )
                            host.LangScopes.Last().Targets.Add((cref, node.Cref.ToString()[0] == '@')!);
                        else
                            AdHocAgent.LOG.Warning("Configuration target '{cref}' (line {line}) found on host '{host}' without a preceding language specifier like '<see cref=\"InCS\"/>'. This configuration will be ignored.", cref, node.GetLocation().GetLineSpan().StartLinePosition.Line + 1, host);
#endregion
                        goto END;
                    }


                    if( cref == null ) AdHocAgent.exit($"`Reference to unknown entity {node.Parent} (line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}) detected. Correct or delete it");
                }

                END:
                base.VisitXmlCrefAttribute(node);
            }
        }

        /// <summary>
        /// Stores pack ID information read from the source code file.
        /// Key: INamedTypeSymbol representing the pack.
        /// Value: Integer ID assigned to the pack.
        ///  Reconciles Pack IDs. This method handles:
        /// 1. Reading existing IDs from the source file.
        /// 2. Assigning new IDs to newly created packs.
        /// 3. Resolving duplicates and renumbering.
        /// 4. Writing changes back to the .cs file.
        /// </summary>
        public readonly Dictionary<INamedTypeSymbol, int> pack_id_info = new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Trailing user text captured from Dashboard lines (`<see cref='X' .../>  user text here`).
        /// This text is merged with the pack's class `///` doc into a unified pool used by
        /// [KeepDoc]/[SkipDoc] filters and branch tag matching.
        /// </summary>
        public readonly Dictionary<ISymbol, string> pack_user_tags = new(SymbolEqualityComparer.Default);

        /// <summary>
        /// The numbers tables of this project: an entity's number, the one it had before a renumbering (ulong.MaxValue -
        /// none), the user's text after them, and the line. Read from the doc comments before the closing brace of the
        /// project's interface; they replace the `/*…*/` marks of the entities that have a name to refer to.
        /// </summary>
        public readonly Dictionary<ISymbol, (ulong[] numbers, ulong was, string tail, int line)> numbers = new(SymbolEqualityComparer.Default);

        /// <summary>The numbers tables, in the order they are written: each a doc comment `/** kind` before the project's interface.</summary>
        public const string TABLE_PACKS       = "packs";
        public const string TABLE_IMPORTED    = "imported";
        public const string TABLE_PROJECT     = "project";
        public const string TABLE_HOSTS       = "hosts";
        public const string TABLE_CONNECTIONS = "connections";
        public const string TABLE_ACTORS      = "actors";
        public const string TABLE_STATES      = "states";

        static readonly HashSet<string> TABLES = [TABLE_PACKS, TABLE_IMPORTED, TABLE_PROJECT, TABLE_HOSTS, TABLE_CONNECTIONS, TABLE_ACTORS, TABLE_STATES];

        /// <summary>
        /// The kind of the numbers table `dt` is, or null - it is not one. A numbers table is a doc comment of a project's
        /// interface (or, as they were first written, one before its closing brace) whose text opens with its kind.
        /// </summary>
        public static string? numbers_table(DocumentationCommentTriviaSyntax dt)
        {
            if( dt.ParentTrivia.Token.Parent is not InterfaceDeclarationSyntax { Parent: BaseNamespaceDeclarationSyntax or CompilationUnitSyntax } i ) return null;
            if( dt.ParentTrivia.Token != i.CloseBraceToken && dt.ParentTrivia.Token != i.GetFirstToken() ) return null;

            var kind = dt.Content.OfType<XmlTextSyntax>().FirstOrDefault()?.TextTokens.Select(t => t.Text.Trim()).FirstOrDefault(t => t.Length != 0)?.Split(' ')[0];
            return kind != null && TABLES.Contains(kind) ?
                       kind :
                       null;
        }

        /// <summary>
        /// The section of a numbers table `line` is in: the kind named last before it at the start of a line of its block -
        /// `/** packs`, `/** project`, or a section's own line (`hosts`, `connections`…). The text after a line's `/>` is its
        /// numbers and tags, never a section.
        /// </summary>
        public static string? numbers_section(XmlEmptyElementSyntax line)
        {
            if( line.Parent is not DocumentationCommentTriviaSyntax dt || numbers_table(dt) is not { } kind ) return null;

            var at_line_start = true; // the block's first text follows `/**`
            foreach( var node in dt.Content )
            {
                if( node == line ) return kind;
                if( node is not XmlTextSyntax xml )
                {
                    at_line_start = false; // the text right after an element is that element's numbers and tags
                    continue;
                }

                foreach( var token in xml.TextTokens )
                    if( token.IsKind(SyntaxKind.XmlTextLiteralNewLineToken) ) at_line_start = true;
                    else if( token.Text.Trim() is { Length: > 0 } t )
                    {
                        if( at_line_start && TABLES.Contains(t.Split(' ')[0]) ) kind = t.Split(' ')[0];
                        at_line_start = false;
                    }
            }

            return kind;
        }

        /// <summary>The entities this run renumbered, with the number each had: it goes into the table line as `~old`.</summary>
        readonly Dictionary<Entity, ulong> renumbered = [];

        /// <summary>
        /// The region-key prefixes of the renumbered hosts: a region's key starts with its project's slot and its entity's
        /// number, `old` before the renumbering, `new` after. The deployment moves custom code from the old keys to the new.
        /// Called after `init()`, when a project's number is its slot (the root's 0).
        /// </summary>
        public static IEnumerable<(string old, string @new)> renumbered_prefixes() =>
            from prj in projects
            from host in prj.hosts.Where(h => h.project == prj)
            let was = prj.was(host)
            where was != ulong.MaxValue
            select (prj.uid.to_base256_chars() + was.to_base256_chars(), prj.uid.to_base256_chars() + host.uid.to_base256_chars());

        /// <summary>
        /// The number `e` of this project had before it was renumbered (its `~old`), for what is keyed by numbers to follow it:
        /// custom-code regions, the Observer's layout. ulong.MaxValue - it was not.
        /// </summary>
        public ulong was(Entity e) => renumbered.TryGetValue(e, out var old) ?
                                          old :
                                          ((HasDocs)e).symbol is { } s && numbers.TryGetValue(s, out var read) && read.numbers.Length != 0 && read.numbers[0] == e.uid ?
                                              read.was :
                                              ulong.MaxValue;

        /// <summary>A tabled entity got a number this run: the tables are to be written.</summary>
        bool numbers_changed;

        /// <summary>The project's own number as persisted: the root's is replaced by 0 for the generation.</summary>
        ulong persisted_uid = ulong.MaxValue;

        /// <summary>
        /// Returns true if the attribute class is one of the five branch-declaration attributes
        /// (either overload — with or without the PACKS generic argument).
        /// </summary>
        public static bool IsBranchAttribute(INamedTypeSymbol? ac)
        {
            if( ac == null || !ac.isMeta() ) return false;
            return ac.Name is
                       "L____________Attribute" or
                       "l____________Attribute" or
                       "____________RAttribute" or
                       "____________rAttribute" or
                       "_____lr_____Attribute";
        }

        /// <summary>
        /// `[ClearAttributes]` — on a `Modify&lt;&gt;` modifier, replace the target's attribute set with an empty one.
        /// The marker itself is never read as metadata; its only job is to make the modifier's attribute list
        /// non-empty, which is what tells the merge that the set is being replaced rather than left alone.
        /// </summary>
        public static bool IsClearAttributes(INamedTypeSymbol? ac) => ac != null && ac.isMeta() && ac.Name == "ClearAttributesAttribute";

        /// <summary>
        /// Does this entity's attribute — as written on a `Modify&lt;&gt;` modifier — take part in the replacement of
        /// the target's attribute set? Everything `read_attributes` consumes does: a transform chain, a trim, and any
        /// ordinary metadata that survives <c>IsInternalMeta</c> (`[Timeout]`, `[TransmitTimeout]`, `[Resumable]`, the
        /// user's own attributes). Branch attributes do not: they spell a state's transitions, which is its body
        /// rather than its tuning, and are merged like fields.
        /// </summary>
        public static bool IsReplaceableAttribute(INamedTypeSymbol? ac) => ac != null &&
                                                                           !IsBranchAttribute(ac) &&
                                                                           (is_stream_stage(ac) || is_stream_flow(ac) || is_stream_trim(ac) || IsClearAttributes(ac) || !HasDocs.IsInternalMeta(ac));

        /// <summary>
        /// Reads pack ID information from the source file, updates pack IDs, and writes changes back to the file.
        /// </summary>
        /// <param name="once">HashSet to track processed projects and prevent infinite recursion in imported project processing.</param>
        /// <returns>ISet of transmittable packs without related packs.</returns>
        /// <summary>
        /// The edits that move this project's numbering into its numbers tables: the `/*…*/` marks of its tabled entities go,
        /// and the tables before the closing brace of its interface are written anew. Empty when the file already says so.
        /// </summary>
        /// <param name="wire">The id each of this project's packs and virtual connections goes on the wire under, if it does.</param>
        /// <param name="imported">The imported packs this project sends under an id of its own, not their project's.</param>
        List<(int start, int end, string text)> numbers_edits(string src, IReadOnlyDictionary<Entity, int> wire, IEnumerable<(Entity e, int id)> imported)
        {
            List<(int start, int end, string text)> edits = [];

            var own = new List<Entity> { this };
            own.AddRange(hosts.Where(h => h.project == this && is_tabled(h)));
            own.AddRange(connections.Where(c => c.project == this && is_tabled(c)));
            var actors = connections.Where(c => c.project == this).SelectMany(c => c.actors).Where(is_tabled).ToList();
            own.AddRange(actors);
            own.AddRange(actors.SelectMany(a => a.states).Where(s => s.project == this && is_tabled(s)));
            own.AddRange(all_packs.Concat(constants_packs).Where(p => p.project == this && is_tabled(p)).Distinct());

            // the id table of the old form, before the project's interface: its content is in the numbers tables now
            if( packs_id_info_start != -1 )
            {
                var t = node!.SyntaxTree.GetText();
                edits.Add((t.Lines.GetLineFromPosition(packs_id_info_start).Start, t.Lines.GetLineFromPosition(packs_id_info_end).EndIncludingLineBreak, ""));
            }

            foreach( var mark in own.SelectMany(e => e.uid_marks).Distinct().OrderBy(m => m.Start) )
            {
                var (from, to) = (mark.Start, mark.End);
                if( 0 < from && src[from - 1] == ' ' && to < src.Length && (src[to] == ' ' || src[to] == '\r' || src[to] == '\n') ) from--;
                edits.Add((from, to, ""));
            }

            var prefix = symbol!.ToDisplayString() + ".";
            string cref(Entity e)
            {
                if( e == this ) return _name;
                var full = ((HasDocs)e).symbol is IMethodSymbol m ?
                               m.ContainingType.ToDisplayString() + "." + m.Name :
                               ((HasDocs)e).symbol!.ToDisplayString();
                return full.StartsWith(prefix) ?
                           full[prefix.Length..] :
                           full;
            }

            // The numbers right after `/>`, base256. A pack's wire id follows in decimal, in a column of its own across the packs'
            // table (`width`): `<see cref='TunnelId'/>Ā        139`.
            string see(string cref) => "<see cref='" + cref + "'/>";

            string head(Entity e, IEnumerable<ulong> list) => see(cref(e)) + string.Join(' ', list.Select(v => v.to_base256_chars()));

            // `digits` - the wire ids' column width: a pack without a wire id keeps its tags in the column of the others'
            string line(string indent, Entity e, IEnumerable<ulong> list, int width, int digits)
            {
                var sb  = new StringBuilder(indent).Append(head(e, list));
                var sym = e == this ? symbol! : ((HasDocs)e).symbol!;
                if( wire.TryGetValue(e, out var id) )
                    if( e is HostImpl.PackImpl ) sb.Append(' ', Math.Max(1, width - (sb.Length - indent.Length))).Append(id).Append(' ', digits - id.ToString().Length);
                    else sb.Append(' ').Append(id); // a virtual connection's
                else if( e is HostImpl.PackImpl && pack_user_tags.TryGetValue(sym, out var t) && !string.IsNullOrEmpty(t) )
                    sb.Append(' ', Math.Max(1, width - (sb.Length - indent.Length)) + digits);

                numbers.TryGetValue(sym, out var read);
                if( renumbered.TryGetValue(e, out var old) ) sb.Append(" ~").Append(old.to_base256_chars());
                else if( read.numbers is { Length: > 0 } && read.was != ulong.MaxValue && read.numbers[0] == e.uid ) sb.Append(" ~").Append(read.was.to_base256_chars());
                return tags(sb, sym).ToString().TrimEnd() + "\n"; // the column's padding goes when nothing follows it
            }

            // a pack's tags (the text after its line): branches find packs by them, [KeepDoc]/[SkipDoc] filter by them
            StringBuilder tags(StringBuilder sb, ISymbol sym) => pack_user_tags.TryGetValue(sym, out var t) && !string.IsNullOrEmpty(t) ?
                                                                     sb.Append(' ').Append(t) :
                                                                     sb;

            var decl   = (TypeDeclarationSyntax)node!;
            var text   = node.SyntaxTree.GetText();
            var dl     = text.Lines.GetLineFromPosition(decl.SpanStart); // the project's interface line
            var block  = src[dl.Start..(dl.Start + src[dl.Start..].TakeWhile(c => c is ' ' or '\t').Count())];
            // one level deeper than the interface, in the file's own unit: that of the interface's first member, else a tab
            var inner  = decl.Members.FirstOrDefault() is { } member ?
                             new string(src[text.Lines.GetLineFromPosition(member.SpanStart).Start..].TakeWhile(c => c is ' ' or '\t').ToArray()) :
                             "";
            var indent = block.Length < inner.Length && inner.StartsWith(block) ?
                             inner :
                             block + "\t";

            // Two block comments: the packs, then everything else. A block opens with its first section's kind (`/** packs`,
            // `/** project`); each next section has its kind on a line of its own, after a blank line.
            var sb = new StringBuilder();

            void section(string kind, IEnumerable<(Entity e, ulong[] list)> rows, bool first)
            {
                var list = rows.ToList();
                if( list.Count == 0 ) return;
                var width = kind == TABLE_PACKS ?
                                list.Max(r => head(r.e, r.list).Length) + 4 :
                                0;
                var digits = kind == TABLE_PACKS ?
                                 list.Max(r => wire.TryGetValue(r.e, out var id) ? id.ToString().Length : 0) :
                                 0;
                if( first ) sb.Append(block).Append("/** ").Append(kind).Append('\n');
                else sb.Append('\n').Append(block).Append("    ").Append(kind).Append('\n');
                foreach( var (e, l) in list ) sb.Append(line(indent, e, l, width, digits));
            }

            // the packs first: they are what one looks up most
            var packs  = own.OfType<HostImpl.PackImpl>().OrderBy(cref).Select(e => ((Entity)e, new[] { e.uid })).ToList();
            var theirs = imported.OrderBy(i => ((HasDocs)i.e).symbol!.ToDisplayString()).ToList();
            if( 0 < packs.Count || 0 < theirs.Count )
            {
                if( packs.Count == 0 ) sb.Append(block).Append("/** ").Append(TABLE_PACKS).Append('\n');
                section(TABLE_PACKS, packs, true);

                // imported packs sent under an id of this project's own: the id alone, the number is their project's
                if( 0 < theirs.Count )
                {
                    sb.Append('\n').Append(block).Append("    ").Append(TABLE_IMPORTED).Append(" packs, sent under an id of this project\n");
                    foreach( var (e, id) in theirs )
                    {
                        var sym = ((HasDocs)e).symbol!;
                        sb.Append(tags(new StringBuilder(indent).Append(see(sym.ToDisplayString())).Append(id), sym).Append('\n'));
                    }
                }

                sb.Append(block).Append("*/\n");
            }

            section(TABLE_PROJECT, [(this, [persisted_uid, .. (imported_projects_uid ?? []).Select(u => AdHoc.Connection.Transmitter.zig_zag((long)(persisted_uid - u), 63))])], true);
            section(TABLE_HOSTS, own.OfType<HostImpl>().OrderBy(e => e.uid).Select(e => ((Entity)e, new[] { e.uid })), false);
            section(TABLE_CONNECTIONS, own.OfType<ConnectionImpl>().OrderBy(e => e.uid).Select(e => ((Entity)e, new[] { e.uid })), false);
            section(TABLE_ACTORS, actors.GroupBy(a => ((HasDocs)a).symbol, SymbolEqualityComparer.Default) // an RPC method: one line for both its actors
                                        .OrderBy(g => g.First().uid)
                                        .Select(g => ((Entity)g.First(), g.Select(a => a.uid).ToArray())), false);
            section(TABLE_STATES, own.OfType<ConnectionImpl.StateImpl>().OrderBy(cref).Select(e => ((Entity)e, new[] { e.uid })), false);
            sb.Append(block).Append("*/\n");

            // Where they go: in place of the tables already before the interface, else right before the interface's own leading
            // comments. The id table of the old form goes (above), and so do tables left before the interface's closing brace
            // (as they were first written), with the blank line before them.
            var numbers_tables = (SyntaxToken t) => t.LeadingTrivia.Where(tr => tr.GetStructure() is DocumentationCommentTriviaSyntax dt && numbers_table(dt) != null).ToList();

            var top = numbers_tables(decl.GetFirstToken());
            int start, end;
            if( 0 < top.Count )
            {
                start = text.Lines.GetLineFromPosition(top[0].SpanStart).Start;
                end   = text.Lines.GetLineFromPosition(top[^1].Span.End).EndIncludingLineBreak;
            }
            else // right before the interface's own leading comments - the id table of the old form among them goes (above)
            {
                var first = decl.GetFirstToken().LeadingTrivia.FirstOrDefault(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia));
                start = end = text.Lines.GetLineFromPosition(first == default ?
                                                                 decl.SpanStart :
                                                                 first.SpanStart).Start;
            }

            var now = sb.ToString();
            if( src[start..end] != now ) edits.Add((start, end, now));

            var bottom = numbers_tables(decl.CloseBraceToken);
            if( 0 < bottom.Count )
            {
                var from = text.Lines.GetLineFromPosition(bottom[0].SpanStart);
                var gone = 0 < from.LineNumber && text.Lines[from.LineNumber - 1].Span.IsEmpty ?
                               text.Lines[from.LineNumber - 1].Start :
                               from.Start;
                edits.Add((gone, text.Lines.GetLineFromPosition(bottom[^1].Span.End).EndIncludingLineBreak, ""));
            }

            return edits;
        }

        public ISet<HostImpl.PackImpl> read_packs_id_info_and_write_update(HashSet<object> once)
        {
            List<ProjectImpl> imported_projects = [];
#region process imported projects and collect them
            foreach( var I in symbol!.Interfaces )
                if( entities.TryGetValue(I, out var value) && value is ProjectImpl prj )
                    if( once.Add(prj) )
                    {
                        imported_projects.Add(prj);
                        prj.read_packs_id_info_and_write_update(once);
                    }
#endregion


            // Packs in states are transmittable and must have a valid `id`.
            var transmittable_packs_without_related = connections
                                                      .Where(conn => conn.hostL!.included && conn.hostR!.included)
                                                      .SelectMany(conn => conn.actors) // Traverse actors
                                                      .SelectMany(actor => actor.states)
                                                      .SelectMany(state => state.branchesL.Concat(state.branchesR))
                                                      .SelectMany(branch => branch.packs)
                                                      .ToHashSet(); //collect valid transmittable packs

            // Check for the correct usage of empty packs as type.
#region Validate empty packs
            // Time-typedef packs (Duration / DateTimeDef / TimeSpanDef) are intentionally
            // bodyless — their constraints come from the Meta interface they implement and
            // are populated as `_constants_` later (line 1436). Excluding them here keeps
            // referencing fields pointed at the pack so the min/max propagation at line 1942
            // can apply `_min_value = 0` and `_max_value = pack._constants_[0]._value_int`.
            //
            // Named Stream/File packs (`class X : Stream {}` / `: File {}`) are also intentionally
            // bodyless — the framing IS the payload (chunked bytes for Stream, length-prefixed
            // bytes for File). A field referencing such a pack carries that framed byte stream,
            // not a boolean presence flag, so the substitute-with-`bool` path is wrong for them.
            foreach( var pack in all_packs.Where(pack => pack.fields.Count == 0 && !(pack.is_Header || pack.is_FieldsInjectInto || pack.is_Duration || pack.is_DateTimeDef || pack.is_TimeSpanDef || pack.is_Stream || pack.is_File)) ) // Empty packs: packs without fields.
            {
                var used = false;

                foreach( var fld in raw_fields.Values.Where(fld => fld.is_Map && fld.get_exT_pack == pack) )
                    AdHocAgent.exit($"The field `{fld.symbol}` at the line: {fld.line_in_src_code} is a Map with a key of empty pack {pack.symbol}, which is unsupported and unnecessary.");

                foreach( var fld in raw_fields.Values.Where(fld => fld.get_exT_pack == pack)
                                              .Concat(raw_fields.Values.Where(fld => fld.is_Map && fld.V.get_exT_pack == pack).Select(fld => fld.V!)) //field value has empty packs as type
                       )                                                                                                                              //change field type to boolean
                {
                    used = true;
                    fld.switch_to_boolean();
                }

                if( used )
                    AdHocAgent.report("WRN", "Empty pack used as field datatype — replaced with `bool`",
                                      $"Pack: {AdHocAgent.ansi(pack.symbol?.ToString() ?? "(unnamed)",                "1")}",
                                      $"File: {AdHocAgent.ansi($"{AdHocAgent.provided_path}:{pack.line_in_src_code}", "36")}",
                                      "",
                                      "This pack declares no fields, so any field that references it carries no",
                                      "payload. Such references have been substituted with `bool` (true = present).");
                if( transmittable_packs_without_related.Contains(pack) || resumable_service.Contains(pack) ) continue; // transmittable, or a service pack of the resume handshake


                //NOT transmittable constants set
                pack._id = (ushort)Project.Host.Pack.Field.DataType.t_constants; //Switch to using pack as a set of constants.
                constants_packs.Add(pack);
            }
#endregion


            var update_packs_id_info = false; //Update packs_id_info in the source file is needed to reflect changes.

            var imported_projects_pack_id_info = imported_projects
                                                 .SelectMany(prj => prj.pack_id_info)
                                                 .ToDictionary(pair => pair.Key, pair => pair.Value, SymbolEqualityComparer.Default);

            var included_packs = transmittable_packs_without_related.Where(p => p.included).ToArray(); //packs with persistent is

            // Virtual connections (VirtuallyConnects<L,R,PATH>) share the same persistent id pool as
            // transmittable packs — they need a stable, unique id across runs and are persisted in
            // the same source-file Dashboard.
            // A connection a multiplexer relays takes one as well: the id of its tunnel on the carrier.
            var included_virtual_conns = connections.Where(c => c.is_tunnelled && (c.included || c.relay_carrier != null) && c.symbol != null && c.project == this).ToArray();

#region Apply collected pack_id_info to packs
            foreach( var pack in included_packs ) //Extract saved transmittable pack ID information
                if( pack.symbol != null && !pack._name.Equals(pack.symbol.Name) )
                {
                    AdHocAgent.LOG.Error("The name of the pack {entity} (line:{line}) has been changed to {new_name}. However, the pack cannot be assigned an ID until its name is manually corrected", pack.symbol, pack.line_in_src_code, pack._name);
                    AdHocAgent.exit("", 66);
                }
                else if( pack.symbol != null && pack_id_info.TryGetValue(pack.symbol, out var id) ||              // If the root project does not have the pack ID info...
                         pack.symbol != null && imported_projects_pack_id_info.TryGetValue(pack.symbol, out id) ) //imported projects may have it
                    pack._id = (ushort)id;

            // Same id-application step for virtual connections — read back the persisted id by symbol.
            foreach( var vconn in included_virtual_conns )
                if( !vconn._name.Equals(vconn.symbol!.Name) )
                {
                    AdHocAgent.LOG.Error("The name of the virtual connection {entity} (line:{line}) has been changed to {new_name}. However, the connection cannot be assigned an ID until its name is manually corrected", vconn.symbol, vconn.line_in_src_code, vconn._name);
                    AdHocAgent.exit("", 66);
                }
                else if( pack_id_info.TryGetValue((INamedTypeSymbol)vconn.symbol!, out var id) ||
                         imported_projects_pack_id_info.TryGetValue((INamedTypeSymbol)vconn.symbol!, out id) )
                    vconn._id = (ushort)id;
#endregion

#region Detect obsolete pack ID records to trigger a rewrite
            var included_pack_symbols                                                                                                                              = included_packs.Where(p => p.symbol != null).Select(p => p.symbol!).ToHashSet(SymbolEqualityComparer.Default);
            var included_virtual_conn_symbols                                                                                                                      = included_virtual_conns.Select(c => (ISymbol)c.symbol!).ToHashSet(SymbolEqualityComparer.Default);
            if( pack_id_info.Keys.Any(symbol => !included_pack_symbols.Contains(symbol) && !included_virtual_conn_symbols.Contains(symbol)) ) update_packs_id_info = true;
#endregion

            if( new FileInfo(AdHocAgent.provided_path).IsReadOnly ) // Check if the protocol description file is locked
            {
                AdHocAgent.LOG.Warning($"The protocol description file {node!.SyntaxTree.FilePath} is read-only. As a result, the pack ID update process was skipped.");
                return transmittable_packs_without_related; // Return the collected packs without updating the pack IDs
            }

#region Detect pack's id duplication
            // Build the combined ID pool: every entity that holds (or wants) a persistent id from
            // the same numbering space. Packs and virtual connections share this space, so a
            // duplicate across either kind triggers a renumber.
            (string label, ushort id, object entity, ProjectImpl proj)[] combinedAssignedIds =
                included_packs.Where(pk => pk.is_explicitly_transmittable)
                              .Select(pk => ("pack", pk._id, (object)pk, pk.project))
                              .Concat(included_virtual_conns.Where(vc => vc._id < (int)Project.Host.Pack.Field.DataType.t_subpack)
                                                            .Select(vc => ("virtual connection", vc._id, (object)vc, vc.project)))
                              .ToArray();

            foreach( var grp in combinedAssignedIds.GroupBy(e => e.id).Where(g => 1 < g.Count()) )
            {
                update_packs_id_info = true;
                var list = grp.Aggregate("", (current, e) => current + (e.entity is HostImpl.PackImpl p ?
                                                                            p.full_path :
                                                                            ((ConnectionImpl)e.entity).full_path) + "\n");
                AdHocAgent.LOG.Warning("Entities \n{List} with the same id = {Id} detected. One assignment will be preserved, and the others will be renumbered.", list, grp.Key);

                // an imported project's id stands: it lives its own life and does not know who extends it
                var keep = grp.FirstOrDefault(e => e.proj != this).entity ?? grp.First().entity;

                foreach( var e in grp.Where(x => !ReferenceEquals(x.entity, keep)) )
                    switch( e.entity )
                    {
                        case HostImpl.PackImpl pk: pk._id = (int)Project.Host.Pack.Field.DataType.t_subpack; break; // reset for renumbering
                        case ConnectionImpl vc:    vc._id = (int)Project.Host.Pack.Field.DataType.t_subpack; break;
                    }
            }
#endregion

#region Renumbering pack IDs if necessary
            // Shared id pool: a virtual connection at id=N excludes any pack from id=N and vice versa.
            // Packs are assigned first (preserving prior allocation behavior), then virtual connections.
            // New ids come after the imported projects' greatest: those grow without knowing about this one.
            for( var id = imported_projects_pack_id_info.Values.DefaultIfEmpty(-1).Max() + 1;; id++ ) //set new ids
                if( included_packs.All(pack => pack._id     != id) &&
                    included_virtual_conns.All(vc => vc._id != id) )
                {
                    var pack = transmittable_packs_without_related.FirstOrDefault(pack => pack._id == (int)Project.Host.Pack.Field.DataType.t_subpack);
                    if( pack != null )
                    {
                        update_packs_id_info = true;
                        pack._id = (ushort)id;
                        continue;
                    }

                    var vconn = included_virtual_conns.FirstOrDefault(vc => vc._id == (int)Project.Host.Pack.Field.DataType.t_subpack);
                    if( vconn == null ) break;   //no more entity without id
                    update_packs_id_info = true; //mark need to update packs_id_info in protocol description file
                    vconn._id            = (ushort)id;
                }
#endregion
            // The ids on the wire, into the numbers tables: of this project's packs and virtual connections in their own lines, of an
            // imported pack in a line of its own when this project sends it under another id than its project gives it.
            var wire = included_packs.Where(p => p.project == this && p.symbol != null).ToDictionary(p => (Entity)p, p => (int)p._id);
            foreach( var vc in included_virtual_conns ) wire[vc] = vc._id;

            var imported = included_packs.Where(p => p.project != this && p.symbol != null &&
                                                     !(imported_projects_pack_id_info.TryGetValue(p.symbol, out var theirs) && theirs == p._id))
                                         .Select(p => ((Entity)p, (int)p._id))
                                         .ToList();

            var numbering = numbers_edits(node!.SyntaxTree.ToString(), wire, imported);
            if( updated_uid.Count == 0 && numbering.Count == 0 ) return transmittable_packs_without_related;

            var top      = 0;
            var src_code = node!.SyntaxTree.ToString();

            // Built in memory, not straight onto the file: this method runs on EVERY `init()`, and the
            // common case is that it reproduces the description file byte for byte (no new ids, no new
            // uids). Truncating and rewriting it anyway bumped the file's timestamp, which is the very
            // signal `refresh()` uses to decide whether a re-parse is needed — so the Observer's
            // `Up_to_date` request always looked like "the file changed" and always paid for a full
            // re-parse plus a full project re-send. The comparison at the end of this method is what
            // lets the file (and its timestamp) be left alone.
            using StringWriter dst = new();


#region persist entity uid
            // The marks of the entities that still have them (branches, packs), the marks of the tabled ones removed, and the
            // numbers tables: one pass over the source in position order.
            foreach( var (start, end, text) in updated_uid.Select(u => (u.Item1, u.Item1, "/*" + u.Item2.to_base256_chars() + "*/"))
                                                          .Concat(numbering)
                                                          .OrderBy(e => e.Item1)
                                                          .ThenBy(e => e.Item2) )
            {
                dst.Write(src_code[top..start]);
                dst.Write(text);
                top = end;
            }
#endregion

            dst.Write(src_code[top..]);

            // Write only a real change. `File.WriteAllText` and the `StreamWriter(path)` this replaced
            // both default to UTF-8 without BOM, so the bytes on disk are unchanged for the cases that
            // do get written.
            var updated = dst.ToString();
            if( !string.Equals(updated, src_code, StringComparison.Ordinal) )
            {
                System.IO.File.WriteAllText(node!.SyntaxTree.FilePath, updated);
                AdHocAgent.LOG.Information("Updated persistent ids/uids in {File}", node!.SyntaxTree.FilePath);
            }

            return transmittable_packs_without_related;
        }

        /// <summary>
        /// Refreshes the project by re-initializing if any project files have changed since last processing.
        /// </summary>
        /// <param name="on_time">The time of the last refresh check.</param>
        /// <returns>True if the project was refreshed, false otherwise.</returns>
        public static bool refresh(DateTime on_time)
        {
            if( processing_files.All(path => new FileInfo(path).LastWriteTime < processing_time) ) return on_time < processing_time; // Check if all files' last write times are older than processingTime.
            init();                                                                                                                  // If any file changed, reinitialize the project.
            return true;
        }

        /// <summary>
        /// List to store paths of processed files for change detection.
        /// </summary>
        static List<string> processing_files = [];

        /// <summary>
        /// Timestamp of the last project processing.
        /// </summary>
        static DateTime processing_time = DateTime.Now;

        /// <summary>
        /// Applies a simple mangling to a string name, capitalizing the first lowercase character found.
        /// Used to resolve naming conflicts in nested types, particularly in Java.
        /// </summary>
        /// <param name="name">The name to mangle.</param>
        /// <returns>The mangled name, or the original name if no lowercase characters are found.</returns>
        public static string mangling(string name)
        {
            for( var i = 0; i < name.Length; i++ )
                if( char.IsLower(name[i]) )
                    return name[..i] + char.ToUpper(name[i]) + name[(i + 1)..];
            return name;
        }

        /// <summary>
        /// Dictionary to store runtime reflection types for packs.
        /// Key: Fully qualified name of the pack.
        /// Value: Reflection Type object.
        /// </summary>
        static Dictionary<string, Type> types = []; //runtime reflection types

        /// <summary>
        /// Retrieves the reflection Type object for a given pack symbol.
        /// </summary>
        /// <param name="pack">The symbol of the pack.</param>
        /// <returns>The reflection Type object for the pack.</returns>
        public static Type pack_reflection(ISymbol pack) => types[pack.ToString()!];

        /// <summary>
        /// Retrieves the FieldInfo object for a given field symbol using reflection.
        /// </summary>
        /// <param name="field">The symbol of the field.</param>
        /// <returns>The FieldInfo object for the field.</returns>
        public static FieldInfo field_reflection(ISymbol field)
        {
            var str = field.ToString()!;
            var i   = str.LastIndexOf('.');
            return types[str[..i]].GetField(str[(i + 1)..], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)!;
        }

        /// <summary>
        /// Retrieves the MethodInfo object for a given method symbol using reflection.
        /// </summary>
        /// <param name="method">The symbol of the method.</param>
        /// <returns>The MethodInfo object for the method.</returns>
        public static MethodInfo method_reflection(ISymbol method) => types[method.ContainingType.ToString()!].GetMethod(method.Name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)!;

        private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Drops every piece of static state a previous <see cref="init"/> left behind, so a second
        /// pass starts from the same slate as the first.
        /// </summary>
        /// <remarks>
        /// <para>`init()` is not a one-shot: the Observer's `Up_to_date` request routes through
        /// <see cref="refresh"/>, which re-runs it whenever a description file changed on disk. Every
        /// collection below is keyed by, or holds, artifacts of ONE compilation — Roslyn symbols, or
        /// type names — so carrying them over is wrong in two distinct ways:</para>
        /// <para>`types` is keyed by type NAME (a string), which repeats verbatim across compilations:
        /// its `Add` threw `An item with the same key has already been added` on the second pass, so
        /// refresh failed outright and the Observer could never pick up an edited file.</para>
        /// <para>The symbol-keyed dictionaries do not collide — symbols from two compilations are
        /// distinct instances — so they fail silently instead, accumulating entities from parses that
        /// no longer exist and leaking them for the process lifetime.</para>
        /// <para>Add to this method, not to `init()`'s body, when introducing new parse-scoped statics.</para>
        /// </remarks>
        static void reset_static_state()
        {
            processing_files.Clear();
            processing_time = DateTime.Now;
            projects.Clear();

            types.Clear();             // keyed by type name — the duplicate-key crash on re-init
            entities.Clear();          // symbol → Entity, for the compilation being discarded
            named_packs.Clear();
            raw_fields.Clear();
            raw_static_fields.Clear();
            stream_chains.Clear();
            cut_endpoints.Clear();
            rpc_deferred_packset_expansions.Clear(); // drained during init; cleared in case a pass bailed out

            // Terminal-state singletons are lazily built from the live compilation's `Meta.End` /
            // `Meta.Close` symbols and hold a reference to the then-current root project. Drop them so
            // the next pass rebuilds them against its own symbols rather than the discarded ones.
            ConnectionImpl.StateImpl.End   = null;
            ConnectionImpl.StateImpl.Close = null;

            // Collection-capacity defaults are overwritten only when the description declares
            // `_DefaultMaxLengthOf`. Without this reset, removing that declaration would silently keep
            // the previous run's values instead of falling back to the built-in 255.
            HostImpl.PackImpl.FieldImpl._DefaultMaxLengthOf.Strings = 255;
            HostImpl.PackImpl.FieldImpl._DefaultMaxLengthOf.Arrays  = 255;
            HostImpl.PackImpl.FieldImpl._DefaultMaxLengthOf.Maps    = 255;
            HostImpl.PackImpl.FieldImpl._DefaultMaxLengthOf.Sets    = 255;
            HostImpl.PackImpl.FieldImpl._DefaultMaxLengthOf.Uncompressed = 1024;
        }

        /// <summary>
        /// Initializes the project by parsing source files, compiling them, and extracting protocol description information.
        /// This method is static and resets all project-related static data.
        /// </summary>
        /// <returns>The root ProjectImpl instance after initialization.</returns>
        public static ProjectImpl init() // These attributes are required only for the code generator, not for the observer.
        {
            reset_static_state();
            // Parse syntax trees from provided paths.
            var trees = new[] { AdHocAgent.provided_path }
                        .Concat(AdHocAgent.provided_paths)
                        .Select(path =>
                                {
                                    // Add a file path and last write time to the respective lists.
                                    processing_files.Add(path);

                                    // Read the source code from the file.
                                    StreamReader file = new(path);
                                    var          src  = file.ReadToEnd();
                                    file.Close();

                                    // Return a parsed syntax tree for the file.
                                    return SyntaxFactory.ParseSyntaxTree(src, path: path);
                                }).ToArray();

            // Compile the syntax trees into an assembly.
            var compilation = CSharpCompilation.Create("Output",
                                                       trees,
                                                       ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                                                       .Split(Path.PathSeparator) // Load trusted assemblies.
                                                       .Select(path => MetadataReference.CreateFromFile(path)),
                                                       new(OutputKind.DynamicallyLinkedLibrary,
                                                           optimizationLevel: OptimizationLevel.Debug,
                                                           warningLevel: 0, // Suppress warnings by setting warning level to 0.
                                                           assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

            // Instantiate a protocol description parser with the compilation result.
            var parser = new Protocol_Description_Parser(compilation);

            // Emit the compiled assembly to a memory stream.
            using( var ms = new MemoryStream() )
            {
                var result = compilation.Emit(ms); // Write IL code into memory.

                // Check if the compilation was successful.
                if( !result.Success )
                {
                    // Log errors and exit if compilation fails.
                    AdHocAgent.LOG.Error("The protocol description file {ProvidedPath} has an issue:\n",
                                         AdHocAgent.provided_path);
                    AdHocAgent.LOG.Error(string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString()).ToArray()));
                    AdHocAgent.exit("Please fix the problem and rerun");
                }

                // Reset the stream position and load the assembly from memory.
                ms.Seek(0, SeekOrigin.Begin);
                var _types = Assembly.Load(ms.ToArray()).GetTypes();

                // Add parsed types to the protocol parser.
                foreach( var type in _types ) types.Add(type.ToString().Replace("+", "."), type);
            }

            // Visit all syntax trees to parse project details.
            foreach( var tree in trees )
                parser.Visit(tree.GetRoot());

            // Ensure at least one project is detected.
            if( projects.Count == 0 ) AdHocAgent.exit($"No project detected. Provided file {AdHocAgent.provided_path} is incomplete or in the wrong format. Try using the init template.");

            var problematic_projects = string.Join(",\n", projects.Where(prj => string.IsNullOrEmpty(prj._namespacE)).Select(prj => prj.symbol! + " in the file " + prj.node!.SyntaxTree.FilePath));
            if( problematic_projects != "" )
            {
                AdHocAgent.LOG.Error("The following projects do not have a namespace defined: {problematic_projects}", problematic_projects);
                AdHocAgent.exit("Please define the appropriate namespaces and try again.");
            }

            var root_project = projects[0]; // Set the first project as the root and include it in the project structure.
            root_project._included = true;

            // Expose the description files through the `source` file tree — each streamed from disk on demand,
            // the whole list compressed by the pack's `[Zstd]` chain. Nothing is packed or buffered here.
            root_project.source = Files.ONE.Send(processing_files);

#region Create proxy-packs for imported projects
            // Proxy packs are designed to encapsulate all relevant information from imported projects while maintaining and supporting the hierarchical structure of projects-packs.
            foreach( var prj in projects.Skip(1) )
            {
                prj.proxy = new((Entity)prj);
                foreach( var e in entities.Values.Where(e => e.parent_by_source_code == prj) )
                    e.parent_artificial = prj.proxy;
            }
#endregion


            var once = new HashSet<object>(20); // A HashSet is used to track objects and avoid cycles in references (prevent circular references).
            root_project.Init(once);            // Initialize the root project


#region Populate DateTimeDef and TimeSpanDef packs with constants
            var longTypeSymbol = compilation.GetTypeByMetadataName("System.Int64")!;

            // Filter for both DateTimeDef and TimeSpanDef
            foreach( var pack in root_project.all_packs.Concat(root_project.constants_packs)
                                             .Where(p => (p.is_DateTimeDef || p.is_TimeSpanDef || p.is_Duration) && p.symbol != null)
                                             .Distinct() )
            {
                var type = pack_reflection(pack.symbol!);

                try
                {
                    var instance = Activator.CreateInstance(type);

                    // Helper to add constants to the pack
                    void AddConstant(string name, long value)
                    {
                        var constant = new HostImpl.PackImpl.ConstantImpl(root_project, pack.model, longTypeSymbol, null, value)
                                       {
                                           _name = name
                                       };
                        pack._constants_.Add(constant);
                        root_project.constant_fields.Add(constant);
                    }

                    // ---------------------------------------------------------
                    // STRATEGY 1: Absolute Time (DateTimeDef)
                    // ---------------------------------------------------------
                    if( pack.is_DateTimeDef )
                    {
                        var minProp       = type.GetProperty("min");
                        var maxProp       = type.GetProperty("max");
                        var precisionProp = type.GetProperty("precision");

                        var minLimit = DateTimeOffset.MinValue.LocalDateTime;
                        var maxLimit = DateTimeOffset.MaxValue.LocalDateTime;

                        var minVal = minProp == null ?
                                         minLimit :
                                         (DateTime)minProp.GetValue(instance)!;

                        if( minVal < minLimit || maxLimit < minVal ) AdHocAgent.exit($"Invalid min value '{minVal:o}'. Allowed range is [{minLimit:o} .. {maxLimit:o}].");

                        var maxVal = maxProp == null ?
                                         maxLimit :
                                         (DateTime)maxProp.GetValue(instance)!;

                        if( maxVal < minLimit || maxLimit < maxVal ) AdHocAgent.exit($"Invalid max value '{maxVal:o}'. Allowed range is [{minLimit:o} .. {maxLimit:o}].");

                        if( maxVal < DateTimeOffset.MinValue.LocalDateTime || DateTimeOffset.MaxValue.LocalDateTime < maxVal )
                            AdHocAgent.exit("Errot");

                        var precisionVal = precisionProp == null ?
                                               TimeSpan.FromMinutes(1) :
                                               (TimeSpan)precisionProp.GetValue(instance)!;

                        if( maxVal             < minVal ) AdHocAgent.exit($"In DateTimeDef {type.FullName}, max value '{maxVal}' cannot be less than min value '{minVal}'.");
                        if( precisionVal.Ticks <= 0 ) AdHocAgent.exit($"In DateTimeDef {type.FullName}, precision must be a positive time span.");

                        // Detect '@' for enhanced precision strategy
                        var precisionPropSymbol = pack.symbol.GetMembers("precision").OfType<IPropertySymbol>().FirstOrDefault();
                        var precisionSyntax     = precisionPropSymbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as PropertyDeclarationSyntax;
                        var enhancePrecision    = precisionSyntax != null && precisionSyntax.Identifier.Text.StartsWith("@");

                        var range = maxVal - minVal;

                        // Calculate bits required for the requested range
                        var steps = range.Ticks > 0 && precisionVal.Ticks > 0 ?
                                        (ulong)(range.Ticks / precisionVal.Ticks) :
                                        0;
                        var requiredBits = steps == 0 ?
                                               1 :
                                               64 - BitOperations.LeadingZeroCount(steps);

                        // Round up to next full Byte (AdHoc Standard)
                        var wireSizeBytes                      = (requiredBits + 7) / 8;
                        var allocatedBits                      = wireSizeBytes      * 8;
                        if( allocatedBits == 0 ) allocatedBits = 8;

                        var totalStorableSteps = 1UL << allocatedBits;

                        if( enhancePrecision )
                        {
                            // Mode 2: Enhanced Precision (Float Precision)
                            // Use spare bits to subdivide the time step.
                            var spareBits = allocatedBits - requiredBits;
                            if( 0 < spareBits )
                            {
                                var newPrecisionTicks = precisionVal.Ticks / (1L << spareBits);

                                // Hard limit: Cannot go below 1ms as system normalizes to Milliseconds
                                if( newPrecisionTicks < TimeSpan.TicksPerMillisecond ) newPrecisionTicks = TimeSpan.TicksPerMillisecond;

                                precisionVal = TimeSpan.FromTicks(newPrecisionTicks);
                            }

                            // Adjust Max to fill the byte boundary perfectly
                            if( 0 < totalStorableSteps ) maxVal = minVal.AddTicks((long)(totalStorableSteps - 1) * precisionVal.Ticks);
                        }
                        else if( totalStorableSteps > 0 )
                        {
                            // Mode 1: Range Expansion (Default)
                            // Keep precision fixed, float Max significantly into the future.
                            // Cap so the projected maxVal stays within DateTime range (and the
                            // step×precision product stays within long), otherwise AddTicks throws.
                            var maxAdditionalTicks                                         = DateTime.MaxValue.Ticks                          - minVal.Ticks;
                            var maxPossibleSteps                                           = (ulong)(maxAdditionalTicks / precisionVal.Ticks) + 1;
                            if( totalStorableSteps > maxPossibleSteps ) totalStorableSteps = maxPossibleSteps;
                            maxVal = minVal.AddTicks((long)(totalStorableSteps - 1) * precisionVal.Ticks);
                        }

                        // Export (Normalized to Milliseconds)
                        AddConstant("min",       (minVal.Ticks - UnixEpoch.Ticks) / TimeSpan.TicksPerMillisecond);
                        AddConstant("max",       (maxVal.Ticks - UnixEpoch.Ticks) / TimeSpan.TicksPerMillisecond);
                        AddConstant("precision", (long)precisionVal.TotalMilliseconds);
                    }
                    // ---------------------------------------------------------
                    // STRATEGY 2: Relative History (TimeSpanDef)
                    // ---------------------------------------------------------
                    else if( pack.is_TimeSpanDef )
                    {
                        var intervalProp  = type.GetProperty("interval");
                        var precisionProp = type.GetProperty("precision");

                        var intervalVal = intervalProp == null ?
                                              TimeSpan.FromDays(1) :
                                              (TimeSpan)intervalProp.GetValue(instance)!;
                        var precisionVal = precisionProp == null ?
                                               TimeSpan.FromSeconds(1) :
                                               (TimeSpan)precisionProp.GetValue(instance)!;

                        if( intervalVal.Ticks  <= 0 ) AdHocAgent.exit($"In TimeSpanDef {type.FullName}, interval must be positive.");
                        if( precisionVal.Ticks <= 0 ) AdHocAgent.exit($"In TimeSpanDef {type.FullName}, precision must be positive.");

                        // 1. Convert to Milliseconds (Base normalization unit)
                        var requestedMs = intervalVal.TotalMilliseconds;
                        var precisionMs = precisionVal.TotalMilliseconds;
                        var gapMs       = 60000D; // 1 Minute Protection Gap

                        if( precisionMs < 1 ) precisionMs = 1; // Hard limit 1ms

                        // 2. Step 1: Byte Allocation
                        // Calculate raw steps needed for just the requested interval
                        var requiredSteps = (ulong)Math.Ceiling(requestedMs / precisionMs);

                        // Find smallest Byte container (1, 2, 3...) that fits
                        var bytes = 1;
                        while( 1UL << bytes * 8 < requiredSteps && bytes < 8 ) bytes++;

                        // 3. Step 2: Ensure Gap Security
                        var containerCapacity = 1UL << bytes * 8;
                        var spareCapacity     = containerCapacity - requiredSteps;
                        var requiredGapSteps  = (ulong)Math.Ceiling(gapMs / precisionMs);

                        // If spare space is strictly less than 1 minute gap, we must upgrade container size
                        if( spareCapacity < requiredGapSteps && bytes < 8 )
                        {
                            bytes++;
                            containerCapacity = 1UL << bytes * 8;
                        }

                        // 4. Step 3: Refine Precision (Optimization)
                        // Use the massive spare capacity to improve resolution.
                        // Formula: New Precision = (RequestedInterval + Gap) / TotalCapacity
                        var totalTimeNeededMs    = requestedMs + gapMs;
                        var optimizedPrecisionMs = totalTimeNeededMs / containerCapacity;

                        // Since we export 'precision' as long (integer ms), we must Ceil to ensure coverage.
                        // Example: If calc is 5.8ms, we must use 6ms. Using 5ms would reduce total capacity below requirement.
                        var finalPrecisionMs                        = (long)Math.Ceiling(optimizedPrecisionMs);
                        if( finalPrecisionMs < 1 ) finalPrecisionMs = 1;

                        // 5. Calculate Final Interval (Cycle Capacity)
                        // The 'interval' constant acts as the modulo wrapper (Ring Buffer Size)
                        var finalIntervalMs = (long)containerCapacity * finalPrecisionMs;

                        // Export
                        AddConstant("interval",  finalIntervalMs);
                        AddConstant("precision", finalPrecisionMs);
                    }
                    // ---------------------------------------------------------
                    // STRATEGY 3: Elapsed Time (Duration)
                    // ---------------------------------------------------------
                    else if( pack.is_Duration )
                    {
                        var maxProp          = type.GetProperty("max");
                        var precisionProp    = type.GetProperty("precision");
                        var MAX_SAFE_INTEGER = (1L << 53) - 1 - 1; //-1!

                        // Read declared values, falling back to interface defaults when the
                        // implementor omits a property.
                        var maxSteps = maxProp == null ?
                                           MAX_SAFE_INTEGER :
                                           Math.Min(MAX_SAFE_INTEGER, Convert.ToInt64(maxProp.GetValue(instance)));

                        var precisionVal = precisionProp == null ?
                                               TimeSpan.FromSeconds(1) :
                                               (TimeSpan)precisionProp.GetValue(instance)!;

                        if( maxSteps           <= 0 ) AdHocAgent.exit($"In Duration {type.FullName}, max must be a positive step count.");
                        if( precisionVal.Ticks <= 0 ) AdHocAgent.exit($"In Duration {type.FullName}, precision must be a positive time span.");


                        // `max` is a STEP count: the generator sizes the field from it (Generator.range2bytes) and emits it as
                        // MAX. The declared count is widened to fill the whole bytes it needs - the manual's spare-bits
                        // rule - so 30_000 becomes 65_535 and int.MaxValue becomes 0xFFFF_FFFF. It once went out as the BYTE
                        // count, which the generator read as a step count: `max => int.MaxValue` became MAX = 4 in one byte.
                        var bytes = 64 - BitOperations.LeadingZeroCount((ulong)maxSteps) + 7 >> 3;
                        AddConstant("max", 7 <= bytes ? //=========================== max the first!
                                               MAX_SAFE_INTEGER :
                                               (1L << bytes * 8) - 1);
                        AddConstant("precision", Math.Min((1L << 53) - 1, (long)Math.Max(1.0, precisionVal.TotalMilliseconds)));
                    }
                }
                catch( Exception ex )
                {
                    AdHocAgent.LOG.Error("Failed to process Time Definition class {Type}: {Message}", type.FullName, ex.Message);
                    AdHocAgent.exit($"Please check the implementation of the custom {pack.symbol?.Name} type.");
                }
            }
#endregion

            // Collect all typedef packs and preserve them while removing from the root project.
            var TYPEDEF_defined_in_project = root_project.all_packs.Where(pack => pack.is_typedef).Distinct().ToArray();
            root_project.all_packs.RemoveAll(pack => pack.is_typedef);

            // Include all constants-packs and enums in the root project by default.
            foreach( var pack in root_project.constants_packs )
                pack._included = true;


#region Resumable: the connection it is declared in and the host it guards
            foreach( var res in root_project.resumables )
            {
                res._nested_max = 123; // the marker; Init above recomputed it from the fields, as for every pack
                res.connection  = (ConnectionImpl)entities[res.connection_symbol];
                if( entities.TryGetValue(res.host_arg, out var e) && e is HostImpl host && (host == res.connection.hostL || host == res.connection.hostR) )
                    res.host = host;
                else
                    AdHocAgent.exit($"Resumable {res.symbol} (line {res.line_in_src_code}) names {res.host_arg} as HOST, but {res.connection.symbol} connects {res.connection.hostL?.symbol} and {res.connection.hostR?.symbol}. HOST has to be one of them.", 77);
            }
#endregion

#region Process in the root_project collected connections
            // Filter connections, exclude modifiers, include only valid ones, and assign each a unique index.
            root_project.connections = root_project.connections
                                                   .Distinct()
                                                   .Select((conn, idx) =>
                                                           {
                                                               conn._included = true;
                                                               conn.idx       = idx; // Assign index to each connection.
                                                               return conn;
                                                           })
                                                   .ToList();

            // If no valid connections are found, exit with an error.
            if( root_project.connections.Count == 0 )
                AdHocAgent.exit("There is no information available about connections.", 45);

            foreach( var conn in root_project.connections ) // Collect packs across all actors in this connection
            {
                foreach( var actor in conn.actors )
                {
                    actor.InitActor(once, conn.hostL, conn.hostR);
                    if( actor.symbol.isConnects() ) continue;

                    for( var symbol = actor.parent_by_source_code!.symbol; symbol != null && !symbol.isConnects(); symbol = symbol.ContainingType ) // packs to support actor nesting hierarchy
                        if( entities[symbol] is HostImpl.PackImpl pack )                                                                            //support hierarchy
                        {
                            conn.hostL!.const_enum_packs.AddIfNew(pack);
                            conn.hostR!.const_enum_packs.AddIfNew(pack);
                        }
                }

                conn.actors.RemoveAll(actor => //cleanup, if exists, empty Actor0 and detect useless actors
                                      {
                                          if( 0 < actor.hostL_directly_transmitting_packs.Count || 0 < actor.hostR_directly_transmitting_packs.Count ) return false;
                                          if( actor._name != "Actor0" )
                                              AdHocAgent.exit($"The actor {actor.symbol} (line {actor.line_in_src_code}) does not have any packs to transmit. Delete it.");
                                          return true;
                                      });
            }
#endregion

#region Resumable: keep the packs the host really transmits
            foreach( var res in root_project.resumables )
            {
                var where = res.symbol!.ToString();

                var transmitted = new HashSet<HostImpl.PackImpl>();
                var left        = res.host == res.connection.hostL;
                foreach( var actor in res.connection.actors )
                    transmitted.UnionWith(left ? actor.hostL_directly_transmitting_packs : actor.hostR_directly_transmitting_packs);

                foreach( var pack in res.target_packs!.Where(pack => !transmitted.Contains(pack)).ToArray() )
                {
                    AdHocAgent.LOG.Warning("Resumable {entity} (line {line}) lists {pack}, but {host} does not transmit it on {connection}; the pack is dropped from the set.", where, res.line_in_src_code, pack.full_path, res.host.symbol, res.connection.symbol);
                    res.target_packs.Remove(pack);
                }

                if( res.target_packs.Count == 0 )
                    AdHocAgent.exit($"Resumable {where} (line {res.line_in_src_code}) is left with no pack: {res.host.symbol} transmits none of the packs it lists on {res.connection.symbol}.", 77);
            }

            // One journal per leg: the generator keeps a single position header per transmitter, so a second declaration
            // for the same host on the same connection has nowhere to go. The pack sets merge into one declaration.
            foreach( var grp in root_project.resumables.GroupBy(res => (res.connection, res.host)).Where(g => 1 < g.Count()) )
                AdHocAgent.exit($"{string.Join(" and ", grp.Select(res => res.symbol!.ToString()))} both make {grp.Key.host.symbol} resumable on {grp.Key.connection.symbol}. One Resumable per host per connection: merge their pack sets into one declaration.", 77);
#endregion

#region UDP: every pack on the connection is guaranteed, both ways
            // A datagram is lost on its own, not only with the connection, and the journal of a Resumable is what brings it back:
            // over UDP a pack that is not journalled may simply never arrive. So [UDP] on a connection is a Resumable for each of its
            // hosts, covering every pack that host transmits there - but the packs [UDP<EXCLUDE_PACKS_SET>] lets go, and the service
            // packs, added below.
            foreach( var conn in root_project.connections )
            {
                foreach( var entity in conn.actors.SelectMany(actor => actor.states.Select(state => (Entity)state).Prepend(actor)) )
                    if( !equals(entity.symbol, conn.symbol) && ResumableImpl.UDP(entity.symbol) != null ) //Actor0 may be the connection itself
                        AdHocAgent.exit($"[UDP] on {entity.symbol} (line {entity.line_in_src_code}): the transport is chosen for a whole connection. Move it to {conn.symbol}.", 2);

                var udp = ResumableImpl.UDP(conn.symbol);
                if( udp == null ) continue;

                var declared = root_project.resumables.FirstOrDefault(res => res.connection == conn);
                if( declared != null )
                    AdHocAgent.exit($"The connection {conn.symbol} (line {conn.line_in_src_code}) is [UDP]: every pack on it is guaranteed already, both ways. Remove the Resumable {declared.symbol} (line {declared.line_in_src_code}); to leave packs out, list them in [UDP<EXCLUDE_PACKS_SET>].", 77);

                var excluded = conn.udp_excluded(udp);
                var sent     = new HashSet<HostImpl.PackImpl>();
                foreach( var host in new[] { conn.hostL!, conn.hostR! }.Distinct() ) // a connection of a host to itself has one side
                {
                    var left  = host == conn.hostL;
                    var sends = conn.actors.SelectMany(actor => left ? actor.hostL_directly_transmitting_packs : actor.hostR_directly_transmitting_packs).Distinct().ToArray();
                    sent.UnionWith(sends);
                    var guaranteed = sends.Where(pack => !excluded.Contains(pack)).ToArray();
                    if( guaranteed.Length != 0 ) // a host that sends nothing guaranteed here has nothing to journal
                        new ResumableImpl(root_project, conn, host, guaranteed);
                }

                // The excluded packs that travel here travel as before, unguarded - that is what the exclusion is for. Only a listed pack
                // that is not sent on this connection at all is worth a word: a slip in the set, as a Resumable listing a pack its HOST does not send.
                foreach( var pack in excluded.Where(pack => !sent.Contains(pack)) )
                    AdHocAgent.LOG.Warning("[UDP<…>] on {connection} (line {line}) lists {pack} to exclude, but no host sends it on this connection: nothing to exclude.", conn.symbol, conn.line_in_src_code, pack.full_path);
            }
#endregion

#region Resumable service packs: synthesize the trio once, hand each resumable connection the ones each side sends
            // Resume_ and Ack_ travel from the receiver of a journalled stream to its sender, Lost_ and Ask_ the other way;
            // each carries the stream position it speaks of - Ask_ the one it was sent at, so that a receiver that has not got that
            // far knows it lost the tail and asks for it: over UDP nothing behind a lost last pack would show the gap. They belong
            // to no state: the connection's
            // default actor lists them among the packs its sides transmit, which is all the pipeline needs to give them
            // classes, serializers and ids, while the generated code services them outside the application's FSM.
            if( 0 < root_project.resumables.Count )
            {
                // Their uids, like their ids, are not persisted: they follow the topmost uid the packs of this project hold.
                var uid = root_project.all_packs.Concat(root_project.constants_packs).Where(p => p.uid < ulong.MaxValue && projects.All(prj => prj.proxy != p)).Select(p => p.uid).DefaultIfEmpty(0UL).Max(); // the project proxies sit in their own range at the top

                foreach( var res in root_project.resumables.Where(res => res.udp) ) // the headers of the [UDP] connections: made here, not declared, so no uid is kept for them either
                    res.uid = ++uid;

                HostImpl.PackImpl service(string name, bool pos, string doc)
                {
                    var pack = new HostImpl.PackImpl(root_project, name) { uid = ++uid, _doc = doc };
                    if( pos )
                    {
                        pack.fields.Add(ResumableImpl.position(root_project, "pos"));
                        pack.fields.Add(ResumableImpl.time(root_project, "time"));
                    }
                    root_project.resumable_service.Add(pack);
                    return pack;
                }

                var Resume = service("Resume_", true,  "Resume handshake, receiver to sender: the stream position consumed so far; the sender replays its journal from there.");
                var Ack    = service("Ack_",    true,  "Resume handshake, receiver to sender: the stream position consumed so far; the sender may drop the journal behind it.");
                var Lost   = service("Lost_",   true,  "Resume handshake, sender to receiver: the journal no longer holds the position asked for; this is the earliest one it can replay from.");
                var Ask    = service("Ask_",    true,  "Resume handshake, sender to receiver: sent behind the data when the sender wants its journal acknowledged, with the position it was sent at; the receiver answers with Ack_ for everything before it, and with Resume_ when it has not got that far.");

                foreach( var conn in root_project.connections )
                {
                    var L = root_project.resumables.Any(res => res.connection == conn && res.host == conn.hostL); // the Left host journals what it sends here
                    var R = root_project.resumables.Any(res => res.connection == conn && res.host == conn.hostR);
                    if( !L && !R ) continue;

                    var actor   = conn.actors.FirstOrDefault(a => a._name == "Actor0") ?? conn.actors[0];
                    var L_sends = actor.hostL_directly_transmitting_packs;
                    var R_sends = actor.hostR_directly_transmitting_packs;
                    if( L )
                    {
                        L_sends.AddIfNew(Lost);
                        L_sends.AddIfNew(Ask);
                        R_sends.AddIfNew(Resume);
                        R_sends.AddIfNew(Ack);
                    }

                    if( R )
                    {
                        R_sends.AddIfNew(Lost);
                        R_sends.AddIfNew(Ask);
                        L_sends.AddIfNew(Resume);
                        L_sends.AddIfNew(Ack);
                    }
                }
            }
#endregion


#region Collect, enumerate, and validate hosts
            // Filter, deduplicate, and sort hosts
            root_project.hosts = root_project.hosts
                                             .Where(host => host.included && !host.IsModifier)
                                             .Distinct()
                                             .OrderBy(host => host._name)
                                             .ToList();

            // Assign each host a unique index.
            for( var idx = 0; idx < root_project.hosts.Count; idx++ )
                root_project.hosts[idx].idx = idx;

            // Validate that all hosts have language implementation information.
            var missingLangInfo = root_project.hosts.Where(host => host._langs == 0).ToList();
            if( missingLangInfo.Count != 0 )
            {
                foreach( var host in missingLangInfo )
                    AdHocAgent.LOG.Error(
                                         "The host {host} lacks language implementation information. Please use the C# `///<see cref=\"InLANG\"/>` XML comment on the host to add it.",
                                         host.symbol
                                        );

                AdHocAgent.exit("Correct detected problems and restart.", 45);
            }

            // Remove from each host's scope any packs registered at the project level.
            foreach( var host in root_project.hosts )
                host.const_enum_packs.RemoveAll(pack => root_project.constants_packs.Contains(pack));
#endregion


            HostImpl.PackImpl.FieldImpl.init(root_project);

#region Process typedefs
            {
                var flds = raw_fields.Values;
                // Continue processing until no further changes are made.
                for( var rerun = true; rerun; )
                {
                    rerun = false;

                    foreach( var T in TYPEDEF_defined_in_project )
                    {
                        var src = T.fields[0];
                        raw_fields.Remove(src.symbol!); // Remove typedef field.

                        foreach( var dst in flds )
                        {
                            void copy_type(HostImpl.PackImpl.FieldImpl src, HostImpl.PackImpl.FieldImpl dst)
                            {
                                if( (src.is_Map || src.is_Set) && (dst.is_Map || dst.is_Set || dst._name == "") )
                                    AdHocAgent.exit($"The type definition '{src.symbol}' declares a Map/Set that contains another Map/Set type '{dst.symbol}'. " +
                                                    "This nested Map/Set declaration is not supported. Please adjust the type hierarchy and try again.");

                                rerun = true;

                                // Copy basic type properties from source to destination.
                                dst.exT_pack      = src.exT_pack;
                                dst.exT_primitive = src.exT_primitive;
                                dst.inT           = src.inT;

                                dst._dir = src._dir;

                                // Check for type nesting or clashes.
                                if(
                                    dst._map_set_len   != null && src._map_set_len   != null ||
                                    dst._map_set_array != null && src._map_set_array != null ||
                                    dst._exT_array     != null && src._exT_array     != null ||
                                    dst.is_String && dst._max_value != null && src._max_value != null //the string char cap, declared on both sides
                                )
                                {
                                    AdHocAgent.LOG.Error("Typedef {typedef} may generate invalid type nesting or clashes when embedded in {field}.", T, dst);
                                    AdHocAgent.exit("Please fix the problem and rerun");
                                }

                                // Copy attributes from typedef to field
                                dst.copy_attributes_from = src;

                                // Handle an edge case for Map Value types.
                                if( dst._name == "" ) //Map Value
                                    if( src._map_set_len   != null ||
                                        src._map_set_array != null || src.dims != null )
                                    {
                                        AdHocAgent.LOG.Error("Typedef {typedef} may generate invalid type nesting or clashes when embedded in Value type of {field}.", T, dst);
                                        AdHocAgent.exit("Please fix the problem and rerun");
                                    }

                                // Merge dimensions.
                                if( src.dims != null )
                                    if( dst.dims == null ) dst.dims = src.dims;
                                    else dst.dims                   = dst.dims.Concat(src.dims).ToArray();
                                // Copy map and array properties.
                                if( src._map_set_len   != null ) dst._map_set_len   = src._map_set_len;
                                if( src._map_set_array != null ) dst._map_set_array = src._map_set_array;

                                if( src._exT_array != null ) dst._exT_array = src._exT_array;

                                if( src.is_Map ) dst.V = src.V; // Copy map value properties.

                                dst._min_value = src._min_value;

                                // On a string field `max_value` carries the `[D(+N)]` char cap, not a numeric range, and the two
                                // merge differently: a numeric typedef dictates the range unconditionally, while a string typedef
                                // that declares no cap must leave the cap the field itself declares intact.
                                if( !dst.is_String || src._max_value != null ) dst._max_value = src._max_value;

                                dst._min_valueD = src._min_valueD;
                                dst._max_valueD = src._max_valueD;

                                dst._bits = src._bits;

                                // Merge null info.
                                if( src._null_value.HasValue )
                                {
                                    dst._null_value = dst._null_value == null ?
                                                          src._null_value :
                                                          (byte)(dst._null_value.Value | src._null_value.Value);

                                    if( (dst.is_Map      || //Map Key
                                         dst._name == "" || //Map Value
                                         dst.is_Set) &&
                                        src.fld_node?.Declaration.Type is NullableTypeSyntax ) //declare looks like T? field;
                                        dst.set_null_value_bit(2);                             //set Generic nullable bit
                                }
                            }


                            // Apply typedef fields to matching destination fields.

                            if( SymbolEqualityComparer.Default.Equals(T.symbol, dst.exT_pack) ) copy_type(src, dst);

                            if( dst.is_Map && SymbolEqualityComparer.Default.Equals(T.symbol, dst.V!.exT_pack) ) copy_type(src, dst.V);
                        }
                    }
                }
            }
#endregion

#region Reinterpret named Stream/File typed fields as bare conduits
            // A field whose datatype is a named `[S(N)] class X : Stream {}` / `: File {}` pack is parsed EXACTLY
            // like a bare `[S(N)] Stream` / `[S(N)] File` field. Such a pack has no instance fields (enforced in the
            // PackImpl constructor), so it contributes nothing beyond the framing kind and the cap: the field becomes
            // a raw conduit and stops being a sub-pack reference. Consequences, all intended:
            //   * `exT` is t_stream / t_file (not the pack index) and `max_value` carries the cap in bytes,
            //   * no pack id and no headers on the wire in this nested position,
            //   * the pack's `_referred` stays false when it is used only as a field datatype — and a pack that is
            //     not transmittable on any channel drops out entirely, exactly like the bare datatype it stands for.
            // A transform chain declared on the pack travels with it: the field rebuilds the chain in field scope
            // (materialized later, in the Pass-B field loop that calls `collect_stream_stages`).
            // Placed after the typedef pass, so a TYPEDEF aliasing such a pack is covered too, and before
            // "Validate Stream Fields", which then sees a fully formed raw conduit.
            {
                HostImpl.PackImpl? named_conduit(HostImpl.PackImpl.FieldImpl? f) =>
                    f?.exT_pack != null && entities.TryGetValue(f.exT_pack, out var e) && e is HostImpl.PackImpl { } p && (p.is_Stream || p.is_File) ?
                        p :
                        null;

                foreach( var fld in raw_fields.Values )
                {
                    var conduit = named_conduit(fld);

                    // A raw conduit is a whole-field wire format — it cannot be an element of an array, Map or Set,
                    // nor a Map key/value, precisely as a bare `Stream`/`File` cannot.
                    var misplaced = named_conduit(fld.V) ??
                                    (conduit != null && (fld.is_Set || fld.is_Map || fld._exT_array != null || fld._map_set_array != null || fld._dims_len != 0) ?
                                         conduit :
                                         null);
                    if( misplaced != null )
                        AdHocAgent.exit($"The field '{fld.symbol}' (line: {fld.line_in_src_code}) puts the Stream/File pack `{misplaced.symbol}` inside a collection (array / Map / Set). " +
                                        "A field of that type is parsed like a bare `Stream`/`File` conduit — a whole-field wire format with its own framing — so it cannot be a "        +
                                        "collection element. Declare a single field of this type, or wrap the conduit in an ordinary pack and collect that.", 88);

                    if( conduit == null ) continue;

                    fld.exT_pack      = null;
                    fld.exT_primitive = fld.inT = (int)(conduit.is_Stream ?
                                                            Project.Host.Pack.Field.DataType.t_stream :
                                                            Project.Host.Pack.Field.DataType.t_file);
                    fld._max_value  = Math.Abs(conduit._stream_max); // the `[S(N)]` cap; the kind already lives in exT
                    fld._null_value = null;                          // a conduit is framing, not a value: `X? fld;` has nothing to null — same as a bare `Stream?`/`File?`

                    // Inherit the chain the pack declares. The field's OWN entries are already in the list (Pass A,
                    // `FieldImpl.init`, runs before this region), so the pack's are APPENDED after them: the field's
                    // sit closer to the LEAF, the pack's closer to the WIRE. That is the right nesting — the pack
                    // declares how this conduit type reaches the wire, the field adds a transform inside it. So
                    // `[Zstd] class X : Stream {}` used as `[Base64] X fld;` is `leaf -> Base64 -> Zstd -> wire`.
                    // Each entry keeps the pack's own model (the pack may live in another syntax tree) and the pack's
                    // name, so a role conflict between an inherited stage and the field's own can name its source.
                    if( conduit.node != null )
                        foreach( var a in conduit.node.AttributeLists.SelectMany(l => l.Attributes) )
                        {
                            if( (conduit.model.GetSymbolInfo(a).Symbol as IMethodSymbol)?.ContainingType is not { } t ) continue;
                            if( !is_stream_stage(t) && !is_stream_flow(t) && !is_stream_trim(t) ) continue;

                            (fld._stream_stage_attrs ??= []).Add((conduit.model, a, conduit.full_path));
                        }
                }
            }
#endregion


#region Validate Stream Fields
            // Only the RAW conduits are validated here. What a TRIMMED field must be checked for — the payload kind
            // (a pack, or a single string), the chain target, the cut depth — is checked where the trim itself is
            // resolved, in `collect_stream_stages` (Pass B): one gate, whether the trim was written on the field, on
            // the pack, or inside a flow.
            foreach( var field in raw_fields.Values )
                if( field.is_raw_conduit && field._max_value == null )
                    AdHocAgent.exit($"The bare Stream/File field '{field.symbol}' (line: {field.line_in_src_code}) must have a maximum size defined using the [S(N)] attribute — directly, via a TYPEDEF, or by typing the field with a named `[S(N)] class X : Stream/File` pack. (A field made directional by a `[ToStream<E>]` / `[FromStream<E>]` / `[Stream<To, From>]` trim wraps a bounded payload — a pack, or a string capped by [D(+N)] — and needs no [S(N)].)", 88);
#endregion

            HostImpl.PackImpl.init(root_project);
#region Resolve Host Language Configurations
            foreach( var host in root_project.hosts )
            {
                // Start with the implicit default (all ++).
                var finalDefault = 0xFFFFFFFF;

                foreach( var scope in host.LangScopes )
                    if( scope.Targets.Count == 0 ) // An empty scope sets or overwrites the default. The last one processed wins.
                        // Principle 2: Top-Down and Persistent.
                        // This scope has no targets, so it updates the active rule for all subsequent entities.
                        finalDefault = scope.Config;
                    else
                        // Principle 3: Confined Group.
                        // This scope has specific targets. We apply the config active at this moment
                        // to these targets only. We DO NOT update runningDefault, effectively
                        // "resuming" the previous default after this scope.
                        foreach( var (targetSymbol, recursive) in scope.Targets )
                        {
                            void apply(HostImpl.PackImpl p) => host.pack_impl[p.symbol!] = scope.Config;
                            var depth = recursive ?
                                            uint.MaxValue :
                                            0U; // A shallow search
                            switch( targetSymbol.Kind )
                            {
                                case SymbolKind.Field: host.field_impl[targetSymbol] = (Project.Host.Langs)scope.Config; break;
                                // It's a Pack Set. Apply the config to each pack within the set.
                                case SymbolKind.NamedType when named_packs.TryGetValue(targetSymbol, out var packSet):

                                    foreach( var pack in packSet.packs )
                                        pack.for_packs_in_scope(depth, apply);
                                    break;
                                case SymbolKind.NamedType when entities.TryGetValue(targetSymbol, out var entity):

                                    switch( entity )
                                    {
                                        case ProjectImpl prj:     prj.for_packs_in_scope(depth, apply); break;
                                        case HostImpl h:          h.for_packs_in_scope(depth, apply); break;
                                        case HostImpl.PackImpl p: p.for_packs_in_scope(depth, apply); break;
                                        default:
                                            AdHocAgent.exit(
                                                            $"The entity '{targetSymbol.ToDisplayString()}' (a {entity.GetType().Name}) " +
                                                            $"cannot be used as a configuration target in host '{host.symbol?.Name}'.\n"  +
                                                            $"Only transmittable packets or containers of packets are allowed.", 45);
                                            break;
                                    }

                                    break;
                                default:
                                    AdHocAgent.exit(
                                                    $"Invalid configuration target '{targetSymbol.ToDisplayString()}' found in host '{host.symbol?.Name}'.\n"         +
                                                    $"Modifiers (++, +-, -+, --) must target transmittable entities: Packs, Pack Sets, Hosts, Projects, or Fields.\n" +
                                                    $"The provided symbol is a {targetSymbol.Kind}.", 45);
                                    break;
                            }
                        }

                // After iterating through all scopes, set the final default value on the host.
                // This value will be used for any pack not explicitly mentioned in an override scope.
                host._default_impl_hash_equal = finalDefault;
            }
#endregion

#region Set pack indices and collect all fields
            HashSet<HostImpl.PackImpl.FieldImpl>    all_fields = [];
            HashSet<HostImpl.PackImpl.ConstantImpl> constants  = [];

            // Temporarily collect all packs before filtering, so we can index fields
            var temp_all_packs = root_project.all_packs
                                             .Concat(root_project.constants_packs)
                                             .Distinct()
                                             .OrderBy(p => p.full_path)
                                             .ToList();

            for( var idx = 0; idx < temp_all_packs.Count; idx++ )
            {
                var pack = temp_all_packs[idx];
                pack.idx = idx;

                all_fields.AddRange(pack.fields);
                constants.AddRange(pack._constants_);
            }

            constants.AddRange(root_project.hosts.SelectMany(host => host._constants_));                                                                     // Collect all constants from the hosts.
            constants.AddRange(root_project.multiplexes.SelectMany(mux => mux._constants_));                                                                 // Collect all constants from the multiplexers.
            constants.AddRange(root_project.connections.SelectMany(conn => conn._constants_));                                                               // Collect all constants from the connections.
            constants.AddRange(root_project.connections.SelectMany(conn => conn.actors.SelectMany(actor => actor.states).SelectMany(st => st._constants_))); // Collect all constants from the states.

            root_project.fields          = all_fields.OrderBy(fld => temp_all_packs.First(pack => pack.fields.Contains(fld)).full_path + fld._name).ToList();
            root_project.constant_fields = constants.OrderBy(fld => temp_all_packs.FirstOrDefault(pack => pack._constants_.Contains(fld))?.full_path ?? "" + fld._name).ToList();

            for( var idx = 0; idx < root_project.fields.Count; idx++ ) root_project.fields[idx].idx                   = idx; //set fields  idx
            for( var idx = 0; idx < root_project.constant_fields.Count; idx++ ) root_project.constant_fields[idx].idx = idx; //set fields  idx
#endregion

            foreach( var fld in root_project.fields.Where(fld => fld.get_exT_pack != null) )
            {
                var pack = fld.get_exT_pack;
                if( !pack.is_Duration ) continue;
                fld._min_value = 0;
                fld._max_value = pack._constants_[0]._value_int; //max the first!
            }

            //  populates the resolved endpoint lists within each EndpointSetImpl
            // by processing their base interfaces (`_<...>`) before they are needed by the stream logic.
#region Initialize Endpoint Sets
            once.Clear();
            start();
            foreach( var endpointSet in root_project.endpoint_sets ) endpointSet.Init(once);
            if( restart() ) // Handle potential cyclic dependencies between sets
                foreach( var endpointSet in root_project.endpoint_sets )
                    endpointSet.Init(once);
#endregion
#region Record the legs each field is cut on
            // Which legs a field is cut on has to be known HERE, before the transmitting-pack distribution below decides
            // which side may skip a payload's serialization glue. The field's chain — where the cut's DEPTH lives — can
            // not be built this early: its container is a pack, and `root_project.packs` is not assembled until after
            // that distribution has run. So the cut is recorded on the FIELD now, and the chain reuses this very
            // resolution later (`ProjectImpl.resolve_cut` caches per endpoint argument, so nothing is resolved twice).
            //
            // One record, two readers: the distribution below asks the field directly (`cut_on`), and the per-connection
            // view the generator receives (`Connection.ToStream`/`FromStream`) is projected from the same sets at emit
            // time. A trim on a PACK is recorded here too, onto every field typed with that pack: the pack's chain
            // container is shared by all of them and so cannot tell their legs apart.
            void record_trim(HostImpl.PackImpl.FieldImpl fld, INamedTypeSymbol trim)
            {
                var where      = fld.symbol?.ToString() ?? fld._name;
                var (to, from) = trim_of(trim, where, fld.line_in_src_code);
                if( to != null ) (fld.cut_to_codes ??= []).UnionWith(resolve_cut(to, "ToStream", where, fld.line_in_src_code).encoded.Select(x => (int)x.val));
                if( from != null ) (fld.cut_from_codes ??= []).UnionWith(resolve_cut(from, "FromStream", where, fld.line_in_src_code).encoded.Select(x => (int)x.val));
            }

            void record_entry(HostImpl.PackImpl.FieldImpl fld, INamedTypeSymbol t)
            {
                if( is_stream_trim(t) ) record_trim(fld, t);
                else if( is_stream_flow(t) ) // a flow's own attributes ARE its declared entry list — a trim may sit among them
                    foreach( var declared in t.GetAttributes() )
                        if( declared.AttributeClass is { } inner && is_stream_trim(inner) )
                            record_trim(fld, inner);
            }

            foreach( var fld in root_project.fields.Where(fld => fld._stream_stage_attrs != null) )
                foreach( var entry in fld._stream_stage_attrs! )
                    if( (entry.model.GetSymbolInfo(entry.attr).Symbol as IMethodSymbol)?.ContainingType is { } t )
                        record_entry(fld, t);

            // A trim written on the PACK a field is typed with names legs exactly as a field's own does, and the
            // FIELD is where the per-leg answer has to live: `cut_on` is asked of the field, and the per-connection
            // view the generator receives (`Connection.ToStream`/`FromStream`) is projected from these very sets.
            // Carried only by the pack's chain container — ONE container shared by every field of that type — a cut
            // has nothing left to tell the legs apart by, and fires wherever its sending host sends the payload
            // instead of on the leg it names. A pack sent standalone is unaffected either way: its container is its
            // own record, and it travels the legs it is listed on.
            // A conduit (`Stream`/`File`) pack needs no entry here — the pass that reinterprets such fields as bare
            // conduits has already copied its attributes into the field's own list, which the loop above reads.
            foreach( var fld in root_project.fields )
                if( fld.get_exT_pack is HostImpl.PackImpl payload && payload.attributes_source is{ } src )
                    foreach( var a in src.node.AttributeLists.SelectMany(l => l.Attributes) )
                        if( (src.model.GetSymbolInfo(a).Symbol as IMethodSymbol)?.ContainingType is { } t )
                            record_entry(fld, t);

            // One line per cut field, naming the legs it is cut on. This is the per-leg answer everything
            // downstream works from, and the first place a cut that fires on the wrong leg becomes visible —
            // long before anyone reads the generated code.
            string show_leg(int code)
            {
                var conn = root_project.connections.FirstOrDefault(c => c.idx == (code < 0 ? ~code : code));
                if( conn == null ) return code.ToString();
                return $"{(code < 0 ? conn.hostR : conn.hostL)?._name}@{conn._name}";
            }

            foreach( var fld in root_project.fields.Where(f => f.cut_to_codes != null || f.cut_from_codes != null) )
                AdHocAgent.LOG.Information("cut field {field}: ToStream[{to}] FromStream[{from}]",
                                           fld.symbol?.ToString() ?? fld._name,
                                           string.Join(", ", (fld.cut_to_codes ?? []).Select(show_leg)),
                                           string.Join(", ", (fld.cut_from_codes ?? []).Select(show_leg)));
#endregion

            /*
             * ------------------------------------------------------------------------------------------------
             * ARCHITECTURAL CONCEPT: STREAM-INDUCED INFRASTRUCTURE ASYMMETRY
             * ------------------------------------------------------------------------------------------------
             *
             * In AdHoc Protocol, most packet structures are "Symmetrical." If Host A sends a packet
             * containing a complex type 'T' to Host B, both sides require the full metadata and "Glue"
             * (serializers, sub-type definitions, and enum mappings) for 'T'. Host A needs it to encode;
             * Host B needs it to decode.
             *
             * But a TRIM breaks this symmetry by introducing the concept of an "Opaque Pipe" versus a
             * "Typed Transmitter/receiver."
             *
             *      [ToStream<SenderEndPoint>]   T fld;  : only the Sender   needs T's infrastructure.
             *      [FromStream<SenderEndPoint>] T fld;  : only the Receiver needs T's infrastructure.
             *
             * This holds at ANY cut depth: the side left holding raw bytes never materializes T, so it needs
             * nothing of T's schema whether the cut sits at the leaf or between two transform stages.
             *
             * 2. THE ASYMMETRY PROBLEM:
             *    Consider a packet 'BytesTime' containing:
             *    [FromStream<IfSendingFrom<Monitoring, Connection>>] public DiskIO bytes;
             *
             *    If 'Monitoring' (Host L) sends 'BytesTime' to 'Observer' (Host R):
             *    - Host L is the SENDER of the packet, but for the 'bytes' field, it is merely the PIPE.
             *      It takes a pre-existing stream and shoves it into the packet. It does NOT need the
             *      metadata/glue for 'DiskIO'.
             *    - Host R is the RECEIVER of the packet, and it is the receiver of the stream. To make sense
             *      of the 'bytes' field, it MUST have the full infrastructure for 'DiskIO' to deserialize
             *      the content of that stream.
             *
             * 3. HOW IT IS RECORDED:
             *    `hostX_related_packs` answers one question — what does the OTHER side parse out of what X sends.
             *    So a FromStream payload IS there (X pipes bytes, the peer rehydrates a typed pack) and a ToStream
             *    payload is NOT (X serializes it, the peer sees opaque bytes). The two connection-level lists carry
             *    the rest of the answer, the half about X itself:
             *
             *      hostX_to_packs   : X serializes these, the peer materializes nothing — disjoint from `related`.
             *      hostX_from_packs : X serializes NOTHING of these — a subset of `related`, subtracted from it.
             *
             *    Which gives, per direction:  peer parses = transmit ∪ related
             *                                 X encodes   = transmit ∪ (related − from_packs) ∪ to_packs
             *
             * ------------------------------------------------------------------------------------------------
             */
#region Distribute transmittable packs across available Actors within Connections
            // Temporary buffers to categorize dependencies discovered during the recursive crawl:
            // symRel: the RELATED packs — everything the receiving side parses out of what this side sends.
            // to:     payloads only the sender materializes; the receiver holds opaque bytes (ToStream).
            // from:   payloads only the receiver materializes; the sender hands over raw bytes (FromStream).
            //         `from` is a subset of symRel — the receiver parses these, and this list adds that the
            //         sender does not serialize them.
            HashSet<HostImpl.PackImpl> symRel = [], to = [], from = [];

            HashSet<object> get_once()
            {
                once.Clear();
                return once;
            }

            foreach( var conn in root_project.connections )
            {
                foreach( var actor in conn.actors )
                {
                    // ===========================================================================
                    // PASS 1: GLOBAL CRAWL PER ACTOR (to collect the related packs)
                    // ===========================================================================

                    // 1.1 Crawl the type-graph of all root packets sent by Host L.
                    symRel.Clear();
                    to.Clear();
                    from.Clear();
                    foreach( var pack in actor.hostL_directly_transmitting_packs ) pack.collect_inderectly_transmitting_packs(symRel, to, from, conn, false, get_once());

                    // --- Related packs: what R parses ---
                    // `symRel` holds the types R materializes out of L’s packets: the nested types both sides
                    // carry, plus the payload of every field L hands over as raw bytes for R to rehydrate.
                    symRel.ExceptWith(actor.hostL_directly_transmitting_packs);
                    actor.hostL_indirectly_transmitting_packs.AddRange(symRel);
                    actor.hostL_indirectly_transmitting_packs.ForEach(pack => pack._included = true);

                    // 1.2 Crawl the type-graph of all root packets sent by Host R.
                    symRel.Clear();
                    to.Clear();
                    from.Clear();
                    foreach( var pack in actor.hostR_directly_transmitting_packs ) pack.collect_inderectly_transmitting_packs(symRel, to, from, conn, true, get_once());

                    // --- Related packs: what L parses ---
                    symRel.ExceptWith(actor.hostR_directly_transmitting_packs);
                    actor.hostR_indirectly_transmitting_packs.AddRange(symRel);
                    actor.hostR_indirectly_transmitting_packs.ForEach(pack => pack._included = true);


                    // ===========================================================================
                    // PASS 2: STATE-BY-STATE CRAWL (to preserve asymmetric state context)
                    // ===========================================================================
                    foreach( var state in actor.states )
                    {
                        // 2.1 DISCOVERY FOR LEFT SIDE ORIGIN (L -> R)
                        var state_packs_L = state.branchesL.SelectMany(br => br.packs).Distinct().ToList();

                        symRel.Clear();
                        to.Clear();
                        from.Clear();
                        foreach( var pack in state_packs_L )
                            pack.collect_inderectly_transmitting_packs(symRel, to, from, conn, false, get_once());

                        foreach( var pack in state_packs_L )
                        {
                            to.Remove(pack);
                            from.Remove(pack);
                        }

                        // --- Asymmetric Streams: ToStream (L -> R) ---
                        if( 0 < to.Count )
                        {
                            var tempToL = new HashSet<HostImpl.PackImpl>(to);
                            foreach( var pack in to )
                            {
                                pack.collect_inderectly_transmitting_packs(tempToL, conn, false, get_once());
                                pack._included = true;
                            }

                            tempToL.ExceptWith(actor.hostL_indirectly_transmitting_packs);
                            tempToL.ExceptWith(actor.hostL_directly_transmitting_packs);
                            foreach( var pack in tempToL ) pack._included = true;
                            conn.toL.UnionWith(tempToL); // Aggregated to Connection
                            to.Clear();
                        }

                        // --- Asymmetric Streams: FromStream (L -> R) ---
                        if( 0 < from.Count )
                        {
                            var tempFromL = new HashSet<HostImpl.PackImpl>(from);
                            foreach( var pack in from )
                            {
                                pack.collect_inderectly_transmitting_packs(tempFromL, conn, false, get_once());
                                pack._included = true;
                            }

                            // NOT subtracted against `hostL_indirectly_transmitting_packs`: these packs are IN it by
                            // design — that list is what R parses, and R parses exactly these. This list is the other
                            // half of the answer, the one that says L does not serialize them. A pack L sends
                            // standalone is dropped: L serializes that one, so nothing here to subtract for it.
                            tempFromL.ExceptWith(actor.hostL_directly_transmitting_packs);
                            foreach( var pack in tempFromL ) pack._included = true;
                            conn.fromL.UnionWith(tempFromL); // Aggregated to Connection
                            from.Clear();
                        }

                        // 2.2 DISCOVERY FOR RIGHT SIDE ORIGIN (R -> L)
                        var state_packs_R = state.branchesR.SelectMany(br => br.packs).Distinct().ToList();

                        symRel.Clear();
                        to.Clear();
                        from.Clear();
                        foreach( var pack in state_packs_R )
                            pack.collect_inderectly_transmitting_packs(symRel, to, from, conn, true, get_once());

                        foreach( var pack in state_packs_R )
                        {
                            to.Remove(pack);
                            from.Remove(pack);
                        }

                        // --- Asymmetric Streams: ToStream (R -> L) ---
                        if( 0 < to.Count )
                        {
                            var tempToR = new HashSet<HostImpl.PackImpl>(to);
                            foreach( var pack in to )
                            {
                                pack.collect_inderectly_transmitting_packs(tempToR, conn, true, get_once());
                                pack._included = true;
                            }

                            tempToR.ExceptWith(actor.hostR_indirectly_transmitting_packs);
                            tempToR.ExceptWith(actor.hostR_directly_transmitting_packs);
                            foreach( var pack in tempToR ) pack._included = true;
                            conn.toR.UnionWith(tempToR); // Aggregated to Connection
                            to.Clear();
                        }

                        // --- Asymmetric Streams: FromStream (R -> L) ---
                        if( 0 < from.Count )
                        {
                            var tempFromR = new HashSet<HostImpl.PackImpl>(from);
                            foreach( var pack in from )
                            {
                                pack.collect_inderectly_transmitting_packs(tempFromR, conn, true, get_once());
                                pack._included = true;
                            }

                            // See the L→R note above: `hostR_indirectly_transmitting_packs` is what L parses and holds
                            // these by design; this list only adds that R does not serialize them.
                            tempFromR.ExceptWith(actor.hostR_directly_transmitting_packs);
                            foreach( var pack in tempFromR ) pack._included = true;
                            conn.fromR.UnionWith(tempFromR); // Aggregated to Connection
                            from.Clear();
                        }
                    }
                }
            }
#endregion

#region Reject a slot that would need two wire forms on one host
            // A payload's wire form is decided in the SLOT that carries it — the field of the carrying pack — and a
            // pack has ONE serializer and ONE parser per host, shared by every connection that host speaks. So a slot
            // can hold one form per host and direction, and no more.
            //
            // A trim names a leg, which is finer: (sending host, connection). The two granularities agree as long as
            // the slot's form is the same on every leg a host takes it over. When they disagree — cut on one
            // connection, plain on another, same host, same direction — only one form can be generated and the other
            // leg silently gets the wrong one. Refused here rather than emitted, because nothing on the wire announces
            // the difference and the mismatch surfaces as a torn payload at the far end.
            //
            // A `string` slot changes form on BOTH ends of a cut leg, not only on the end left holding raw bytes: the
            // typed end writes or reads UTF-8 there and varint characters everywhere else. So for a string any cut on
            // a leg counts for the sender and for the receiver alike — a `ToStream` string sent over two connections,
            // cut on one of them, needs two serializers just as a `FromStream` pack does.
            //
            // The escape is the shape the telemetry families already use: one carrier pack per leg (`Bytes` /
            // `BytesTime`), so no slot is ever asked to be two things.
            {
                // (slot, host, is the host RECEIVING here) -> the legs seen, split by the form the slot takes
                var forms = new Dictionary<(HostImpl.PackImpl.FieldImpl fld, HostImpl host, bool receiving), (List<string> cut, List<string> plain)>();

                void note(HostImpl.PackImpl.FieldImpl fld, HostImpl? host, bool receiving, bool is_cut, string leg)
                {
                    if( host == null ) return;
                    if( !forms.TryGetValue((fld, host, receiving), out var seen) ) forms[(fld, host, receiving)] = seen = ([], []);
                    var side = is_cut ?
                                   seen.cut :
                                   seen.plain;
                    if( !side.Contains(leg) ) side.Add(leg);
                }

                foreach( var conn in root_project.connections )
                    foreach( var actor in conn.actors )
                        foreach( var isR in (bool[])[ false, true ] )
                        {
                            var sender   = isR ? conn.hostR : conn.hostL;
                            var receiver = isR ? conn.hostL : conn.hostR;
                            var leg      = $"{sender?._name}@{conn._name}";

                            foreach( var pack in isR ?
                                                     actor.hostR_directly_transmitting_packs.Concat(actor.hostR_indirectly_transmitting_packs) :
                                                     actor.hostL_directly_transmitting_packs.Concat(actor.hostL_indirectly_transmitting_packs) )
                                foreach( var fld in pack.fields )
                                {
                                    if( fld.cut_to_codes == null && fld.cut_from_codes == null ) continue;
                                    var cut = fld.cut_on(conn, isR);
                                    var any = cut.to || cut.from;                                  // a string takes the cut form on both ends
                                    note(fld, sender, false, fld.is_String ? any : cut.from, leg);  // the sender's serializer
                                    note(fld, receiver, true, fld.is_String ? any : cut.to, leg);  // the receiver's parser
                                }
                        }

                // Every conflict at once: they are usually several declarations of one shape, and fixing them
                // one build at a time hides how many there are.
                var conflicts = new List<string>();
                foreach( var (key, seen) in forms )
                {
                    if( seen.cut.Count == 0 || seen.plain.Count == 0 ) continue;
                    var role = key.receiving ?
                                   "receives" :
                                   "sends";
                    var (cut_form, plain_form) = key.fld.is_String ?
                                                     ("UTF-8", "varint characters") :
                                                     ("raw bytes", "a typed value");
                    conflicts.Add($"  '{key.fld.symbol?.ToString() ?? key.fld._name}' (line {key.fld.line_in_src_code}) on host {key.host._name}: " +
                                  $"{cut_form} where it {role} over [{string.Join(", ", seen.cut)}], {plain_form} where it {role} over [{string.Join(", ", seen.plain)}].");
                }

                if( 0 < conflicts.Count )
                    AdHocAgent.exit($"{conflicts.Count} trim{(conflicts.Count == 1 ? "" : "s")} ask{(conflicts.Count == 1 ? "s" : "")} for two wire forms at once:\n" +
                                    string.Join("\n", conflicts)                                                                                                      +
                                    "\nOne pack has one serializer and one parser per host, so only one of the two can be generated and the other leg would silently carry the wrong form. " +
                                    "Give each leg its own carrier pack — the shape `Bytes` / `BytesTime` use — or move the trim so that no slot has to differ between legs.", 2);
            }
#endregion

            // Read packs, add their IDs, and update project metadata, excluding the root project.
            once.Clear();

            var packs = root_project.read_packs_id_info_and_write_update(once); //temporary

#region Resumable service packs: ids right above the topmost one in use
            // They have no symbol, so no Dashboard line and no persisted id: they take the ids above the highest one the packs
            // and virtual connections of this project hold, in their fixed order, and follow it up as the numbering grows.
            if( 0 < root_project.resumable_service.Count )
            {
                var top = packs.Where(pack => pack.is_explicitly_transmittable).Select(pack => (int)pack._id)
                               .Concat(root_project.connections.Where(conn => conn.is_tunnelled && (conn.included || conn.relay_carrier != null) && conn._id < (int)Project.Host.Pack.Field.DataType.t_subpack).Select(conn => (int)conn._id))
                               .DefaultIfEmpty(-1)
                               .Max();
                foreach( var pack in root_project.resumable_service ) pack._id = (ushort)++top;
            }
#endregion

            // now we know which packs are explicitly transmittable
#region Bind declared headers to its (only explicitly transmittable) packs
            foreach( var prj in projects )
                for( var index = 0; index < prj.header_packs.Count; index++ )
                {
                    var header = prj.header_packs[index];
                    foreach( var target in header.target_packs!.Where(pack => pack is { is_explicitly_transmittable: true, is_Header: false, is_FieldsInjectInto: false }) )
                    {
                        var fld = new HostImpl.PackImpl.FieldImpl(prj, null, null)
                                  {
                                      _name    = "_header" + index,                     //special unique name
                                      exT_pack = header.symbol,                         //reference to the header pack
                                      exT_made = header.symbol == null ? header : null //a header the agent made has no symbol
                                  };
                        fld.idx = root_project.fields.Count;
                        root_project.fields.Add(fld);
                        target.fields.Add(fld);  //artificial field bind to the header definition
                        header._included = true; //this header included
                    }
                }
#endregion

            // Add transmittable packs and enums/constants marked as "included" to the collection.
            packs.UnionWith(root_project.all_packs.Where(p => p.included && !p.is_Header));
            packs.UnionWith(root_project.constants_packs);

            foreach( var pack in root_project.all_packs.Where(pack => pack._id is (int)Project.Host.Pack.Field.DataType.t_subpack or (int)Project.Host.Pack.Field.DataType.t_constants) )
                pack.fields.RemoveAll(fld => fld._name.StartsWith("_header")); //Headers are only present on **Standalone Packets**, not **Sub-Packets**.

            root_project.all_packs.RemoveAll(pack => pack._id is (int)Project.Host.Pack.Field.DataType.t_subpack or (int)Project.Host.Pack.Field.DataType.t_constants);

#region Ensure unique names, resolving conflicts in-place before user confirmation.
            var header_names = new HashSet<string>();
            var fields_names = new HashSet<string>();
            var was_renamed  = false;


            foreach( var pack in root_project.header_packs )
            {
                var suffix       = 1;
                var originalName = pack._name;
                while( header_names.Contains(pack._name) )
                {
                    was_renamed = true;
                    pack._name  = $"{originalName}_{suffix++}";
                }

                if( originalName != pack._name )
                    AdHocAgent.LOG.Warning($"Header '{pack.symbol}' at line {pack.line_in_src_code}: Name '{originalName}' was a duplicate and proposed to be renamed to '{pack._name}'.");

                header_names.Add(pack._name);
                foreach( var fld in pack.fields )
                {
                    suffix       = 1;
                    originalName = fld._name;
                    while( fields_names.Contains(fld._name) || fld._name == pack._name )
                    {
                        was_renamed = true;
                        fld._name   = $"{originalName}_{suffix++}";
                    }

                    if( originalName != fld._name )
                        AdHocAgent.LOG.Warning($"Field '{fld.symbol}' at line {fld.line_in_src_code}: Name '{originalName}' had a conflict and proposed to be renamed to '{fld._name}'.");

                    fields_names.Add(fld._name);
                }
            }

            if( was_renamed )
            {
                AdHocAgent.LOG.Warning("Proposed renames were performed to resolve naming conflicts. Do you accept these changes and wish to continue? (yes/no)");

                if( Console.ReadLine()?.Trim().ToLower() != "yes" )
                    AdHocAgent.exit("User declined the automatic renames. Please correct the source files manually and restart.");
            }
#endregion

            var nullableHeaderFields = string.Join('\n', root_project
                                                         .header_packs
                                                         .SelectMany(pack => pack.fields)
                                                         .Where(fld => fld.nullable)
                                                         .Select(fld => fld.symbol + " at line:" + fld.line_in_src_code));
            if( 0 < nullableHeaderFields.Length )
                AdHocAgent.exit("Error: Nullable header fields are not allowed.\n" +
                                "Please correct the following fields:\n"           +
                                nullableHeaderFields);


            // Traverse hierarchy upwards and ensure parent packs are included, so the generated code
            // can declare the outer container that scopes a transmittable nested type.
            //
            // A parent pack is reclassified as a pure namespace shell (id=t_constants, fields cleared)
            // ONLY if it has no fields AND nobody inherits from it. A pack that has its own data fields
            // is a real transmittable type (e.g. `Item` holding shared fields that `Show_Code : Item`
            // inherits) and must keep its fields and id intact.
            foreach( var pack in packs.ToArray() )
                for( var p = pack; p.parent_by_source_code is HostImpl.PackImpl parent_pack; p = parent_pack )
                {
                    parent_pack._included = true;
                    if( !packs.Add(parent_pack) ) continue;

                    var hasSubclass = root_project.all_packs.Any(q => q.symbol is { BaseType: { } bt } && equals(bt.OriginalDefinition, parent_pack.symbol));
                    if( parent_pack.fields.Count == 0 && !hasSubclass )
                    {
                        parent_pack._id = (ushort)Project.Host.Pack.Field.DataType.t_constants; // pure shell
                    }
                    // else: real data class — leave fields and _id untouched.
                }

            // Add packs from hosts, ensuring they are marked as included
            foreach( var host in root_project.hosts )
                packs.UnionWith(host.pack_impl.Keys
                                    .Where(symbol1 => entities[symbol1] is HostImpl.PackImpl)
                                    .Select(symbol2 =>
                                            {
                                                var pack = (HostImpl.PackImpl)entities[symbol2];
                                                pack._included = true;
                                                return pack;
                                            }));

            packs.UnionWith(root_project.header_packs.Where(p => p.included)); //mix Headers packs

            // Save all used packs in order of their full path.
            root_project.packs = packs.OrderBy(pack => pack.full_path).ToList();

            // Re-index packs now that the final list is set
            for( var idx = 0; idx < root_project.packs.Count; idx++ )
            {
                var pack = root_project.packs[idx];
                pack.across_idx = (ushort?)(pack.idx = idx);
            }


            packs.Clear(); // re-use
#region Detect redundant pack's language information.
            root_project.hosts.ForEach(host =>
                                       {
                                           packs.Clear(); // re-use

                                           foreach( var conn in root_project.connections.Where(conn => conn.hostL == host || conn.hostR == host) )
                                           {
                                               // Check connection-level asymmetric packs
                                               if( host == conn.hostL )
                                               {
                                                   packs.UnionWith(conn.toL);
                                                   packs.UnionWith(conn.fromR);
                                               }
                                               else
                                               {
                                                   packs.UnionWith(conn.fromL);
                                                   packs.UnionWith(conn.toR);
                                               }

                                               foreach( var actor in conn.actors )
                                               {
                                                   packs.UnionWith(actor.hostL_directly_transmitting_packs);
                                                   packs.UnionWith(actor.hostL_indirectly_transmitting_packs);
                                                   packs.UnionWith(actor.hostR_directly_transmitting_packs);
                                                   packs.UnionWith(actor.hostR_indirectly_transmitting_packs);
                                               }
                                           }

                                           //now in packs all host's transmitted and received packs


                                           //check enums usage, and to do precise distributions among hosts
                                           var host_used_enums = packs.SelectMany(pack => pack.fields)
                                                                      .SelectMany(fld => new[] { fld.get_exT(), fld.V?.get_exT() })
                                                                      .Where(p => p != null && (p.is_enum || p.is_constants_set))
                                                                      .Distinct();


                                           //import used enums in the  host scope
                                           host.const_enum_packs.AddRange(host_used_enums);
                                           // enums if they were hidden behind a typedef field.
                                           host.const_enum_packs.AddRange(packs.Where(p => p.is_enum || p.is_constants_set));
                                           // Constants / enum packs declared inside this host's body go to this host.
                                           // Same namespace-shell filter as the broadcast rule below: only packs that
                                           // actually carry constants or enum members — skip actor-hierarchy holders.
                                           host.const_enum_packs.AddRange(root_project.constants_packs.Where(p =>
                                                                                                                 p.in_host == host && (p.is_enum || 0 < p._constants_.Count)));
                                           // Project-level constants / enum packs — declared directly in the project's scope
                                           // (outside any host body) OR brought in via `_<T>` at project level — are BROADCAST
                                           // to every host, per the distribution rule in the README.
                                           //
                                           // Filter: broadcast only packs that actually carry constants or enum members. This
                                           // excludes namespace-shell PackImpls created for actor-hierarchy holders (interfaces
                                           // like `ServerToMonitoring.Management` that are namespaces for actors, not constants
                                           // containers). Those shells were being re-classified as `t_constants` by the generic
                                           // interface-PackImpl constructor even though they own no constant fields.
                                           host.const_enum_packs.AddRange(root_project.constants_packs.Where(p =>
                                                                                                                 p.in_host == null && (p.is_enum || 0 < p._constants_.Count)));

                                           // Project-level `_<T>` imports across ALL projects (root + sub-projects). These are
                                           // explicit overrides — even if T is declared inside a nested host scope, the import
                                           // at project level says "broadcast it everywhere". Captured during ProjectImpl.modify.
                                           foreach( var prj in projects )
                                               host.const_enum_packs.AddRange(prj.project_level_imported_consts.Where(p => p.is_enum || 0 < p._constants_.Count));

                                           // Cross-pack constant references: for every pack in the host's effective scope
                                           // (transmitted/received  OR  present in pack_impl — because pack_impl entries mean
                                           //  the host emits code for those packs and must compile against them), walk each
                                           // constant's initializer expression. Any `X.Y` reference resolving to a constant
                                           // field in ANOTHER constants/enum pack pulls that owning pack into this host's
                                           // const_enum_packs so the emitted code can resolve the identifier.
                                           //
                                           // Example: `static uint REMOTE_CONNECT = Mask.REMOTE | Action.CONNECT;` declared
                                           // inside `Event` references constants in `Event.Mask` and `Event.Action` — both
                                           // must travel with `Event` into every host that sends, receives, or implements it.
                                           var scopePacks = new HashSet<HostImpl.PackImpl>(packs);
                                           foreach( var sym in host.pack_impl.Keys )
                                               if( entities.TryGetValue(sym, out var ent) && ent is HostImpl.PackImpl pk )
                                                   scopePacks.Add(pk);

                                           foreach( var pack in scopePacks )
                                               foreach( var cst in pack._constants_ )
                                               {
                                                   if( cst.fld_node == null ) continue;
                                                   ExpressionSyntax? initExpr = null;
                                                   foreach( var v in cst.fld_node.Declaration.Variables )
                                                       if( equals(cst.model.GetDeclaredSymbol(v), cst.symbol) )
                                                       {
                                                           initExpr = v.Initializer?.Value;
                                                           break;
                                                       }

                                                   if( initExpr == null ) continue;

                                                   foreach( var refNode in initExpr.DescendantNodesAndSelf()
                                                                                   .Where(n => n is IdentifierNameSyntax or MemberAccessExpressionSyntax) )
                                                   {
                                                       var refSym = cst.model.GetSymbolInfo(refNode).Symbol;
                                                       if( refSym == null || !raw_static_fields.TryGetValue(refSym, out var refConst) ) continue;
                                                       var ownerType = refConst.symbol?.ContainingType;
                                                       if( ownerType == null ) continue;
                                                       // Exception: only PURE constants/enum packs — those NOT transmitted by this host —
                                                       // go into const_enum_packs. If a referenced owner pack is already in this host's
                                                       // transmitted/received set, it's handled through the normal pack pipeline and does
                                                       // not need a second entry here.
                                                       if( entities.TryGetValue(ownerType, out var owner) &&
                                                           owner is HostImpl.PackImpl ownerPack           && (ownerPack.is_constants_set || ownerPack.is_enum) &&
                                                           !packs.Contains(ownerPack) )
                                                           host.const_enum_packs.Add(ownerPack);
                                                   }
                                               }

                                           host.const_enum_packs = host.const_enum_packs.Distinct().ToList();

                                           // --- PRUNING LOGIC START ---
                                           // Collect symbols of all packs actually present in this host's scope (transmitted, received, or enums)
                                           var relevantSymbols = packs.Select(p => p.symbol)
                                                                      .Concat(host.const_enum_packs.Select(p => p.symbol))
                                                                      .ToHashSet(SymbolEqualityComparer.Default);

                                           // Remove configurations from pack_impl for packs that are NOT used by this host
                                           var orphanedSymbols = host.pack_impl.Keys
                                                                     .Where(sym => !relevantSymbols.Contains(sym))
                                                                     .ToList();

                                           if( orphanedSymbols.Count < 1 ) return;

                                           var list = string.Join("\n", orphanedSymbols.Select(sym => $"- {sym}"));
                                           // 1. Log the error with literal formatting
                                           AdHocAgent.LOG.Error(
                                                                "Host {host:l} (line: {line}) has redundant language configurations:\n{list:l}",
                                                                host.symbol, host.line_in_src_code, list);

                                           // 2. Log the warning
                                           AdHocAgent.LOG.Warning("This usually indicates a typo. Redundant settings will be ignored.");


                                           // If the user chooses to continue, we prune the redundant data so the generator remains clean.
                                           foreach( var sym in orphanedSymbols ) host.pack_impl.Remove(sym);
                                           // --- PRUNING LOGIC END ---
                                       });


            // Note: earlier versions promoted packs common to every host up to project scope by
            // stripping them from each host.const_enum_packs. That defeated the `_<EnumOrConst>`
            // broadcast contract — project-level constants/enums must remain in every host's
            // const_enum_packs (per README "Distribution Over Hosts"). Keep them in-place here.
#endregion


            var transmittable = root_project.packs.Where(p => !p.is_Header).ToArray();


            // Windows file system treats file and directory names as case-insensitive. FOO.txt and foo.txt are treated as equivalent.
            // Linux file system treats file and directory names as case-sensitive. FOO.txt and foo.txt are treated as distinct files.
            //
            // This creates a problem in Java, where case-sensitive class names can cause issues when compiled into a case-insensitive file system like Windows.
            // The best workaround is to detect and prevent this situation.

            var problem = false;

            foreach( var by_parent in transmittable.GroupBy(pack => pack.parent_by_source_code) )
                foreach( var by_name in by_parent.GroupBy(pack => pack._name.ToLower()).Where(g => 1 < g.Count()) )
                {
                    var parent_sym = by_parent.Key == null ?
                                         root_project.symbol :
                                         by_parent.Key is HostImpl.PackImpl pp ?
                                             pp.symbol :
                                             by_parent.Key.symbol;

                    var lines = new List<string> { "Names that differ only by case:" };
                    lines.AddRange(by_name.Select(p => $"  • {AdHocAgent.ansi(p._name, "1")}    {AdHocAgent.ansi($"{AdHocAgent.provided_path}:{p.line_in_src_code}", "36")}"));

                    AdHocAgent.report("ERR", $"Duplicate nested types in {AdHocAgent.ansi(parent_sym?.ToString() ?? "<root>", "36")}", lines.ToArray());
                    problem = true;
                }

            if( problem )
            {
                AdHocAgent.report("ERR", "Cross-platform naming conflict",
                                  "File names are case-insensitive on Windows but case-sensitive on Linux.",
                                  "Java classes generated from names that differ only by case collide on",
                                  "Windows — one `.class` overwrites the other — breaking cross-platform builds.",
                                  "",
                                  "Resolution: rename the duplicates above so they differ by more than case.");
                AdHocAgent.exit("", 1);
            }


            var packs_with_parent = transmittable.Where(pack => pack._parent != null).ToArray();

            bool repeat;
            do
            {
                repeat = false;
                foreach( var pack in packs_with_parent )
                    if( pack.parent_by_source_code != null && pack._name.Equals(pack.parent_by_source_code!._name) )
                    {
                        var new_name = mangling(pack._name);
                        AdHocAgent.LOG.Warning("Pack `{Pack}` (line {Line}) is declared inside body of the parent pack {ParentPack} and has the same as parent name . Some languages (Java) not allowed this.\n The name will be changed to `{NewName}`",
                                               pack.symbol, pack.line_in_src_code, pack.parent_by_source_code._name, new_name);
                        pack._name = new_name;
                        repeat     = true; //name changed, this may bring new conflict, repeat check
                    }
            }
            while( repeat );

#region A nested type that hides a type of an enclosing scope
            // C# resolves a name to the innermost type that has it. A nested pack or enum named like a type of an
            // enclosing scope therefore captures every reference to that name inside its parent: a field declared
            // `Name field;` next to a nested `class Name` gets the nested type, whatever the author meant. Recursion
            // (a pack with a field of its own type) is legal and is not reported; only the hiding is.
            foreach( var pack in Entity.entities.Values.OfType<HostImpl.PackImpl>().Distinct() ) // every pack and enum, not only the transmittable ones
            {
                var s = pack.symbol;
                if( s?.ContainingType == null ) continue;

                for( var scope = s.ContainingType.ContainingSymbol; scope != null; scope = scope.ContainingSymbol )
                {
                    if( scope is not INamespaceOrTypeSymbol container ) continue;
                    var hidden = container.GetTypeMembers(s.Name).FirstOrDefault(t => !SymbolEqualityComparer.Default.Equals(t, s));
                    if( hidden == null ) continue;

                    AdHocAgent.LOG.Warning("`{Pack}` (line {Line}) has the name of `{Hidden}`, declared in an enclosing scope. Inside `{Parent}` the name now means the nested type: a field there that refers to `{Name}` gets `{Pack}`, not `{Hidden}`. Rename one of them.",
                                           s.ToDisplayString(), pack.line_in_src_code, hidden.ToDisplayString(), s.ContainingType.ToDisplayString(), s.Name);
                    break;
                }
            }
#endregion

#region Update referred status of packs
            foreach( var pack in transmittable )
                pack._referred = pack.is_transmittable && root_project.fields.Any(fld => fld.get_exT_pack == pack || fld.is_Map && fld.V.get_exT_pack == pack);
#endregion

#region Resumable headers: the host and the connection
            // Hosts and connections are numbered by now.
            foreach( var res in root_project.resumables )
            {
                res.Host._value_int = res.host.idx;
                res.Conn._value_int = res.connection.idx;
            }
#endregion

#region Read attributes for projects, hosts, connections, actors, states, and packs
            // 1. Projects (Root and Imports)
            projects.ForEach(prj => prj.read_attributes(prj.model, prj.node!));

            // 2. Hosts
            root_project.hosts.ForEach(host => host.read_attributes(host.model, host.node!));

            // 3. Connections, Actors, and States
            foreach( var conn in root_project.connections )
            {
                // Connection Level
                if( conn.node != null ) conn.read_attributes(conn.model, conn.node!);

                // Actor Level (Including Synthetic Actor0)
                foreach( var actor in conn.actors )
                    if( actor._name != "Actor0" )
                        actor.read_attributes(actor.model, actor.node == null ?
                                                               actor.synthetic_node :
                                                               actor.node);

                // State Level
                foreach( var state in conn.actors.SelectMany(a => a.states) )
                    if( state.node is MemberDeclarationSyntax memberNode )
                        state.read_attributes(state.model, memberNode);
            }

            // 4. Packs and Fields (Including Synthetic/Proxy Packs)
            // We use a for-loop because read_attributes adds new Proxy Packs to root_project.packs
            // to represent multi-argument attributes (like [Contact] or [License]).
            for( var i = 0; i < root_project.packs.Count; i++ )
            {
                var pack = root_project.packs[i];

                // Process attributes on the Pack itself
                if( pack.node != null )
                    pack.read_attributes(pack.model, pack.node);

                // Process attributes on every Field within the pack
                foreach( var fld in pack.fields )
                    if( fld.fld_node != null )
                    {
                        fld.read_attributes(fld.model, fld.fld_node);
                        if( fld.copy_attributes_from != null ) // If this field is based on a TYPEDEF, read attributes from the TYPEDEF source symbol
                            fld.read_attributes(fld.copy_attributes_from.model, fld.copy_attributes_from.fld_node!, fld.copy_attributes_from.symbol);
                        if( fld._stream_stage_attrs != null ) // build the transform chain now: its container is a pack, and root_project.packs exists only by this phase
                            fld.collect_stream_stages(fld._stream_stage_attrs);
                    }
            }
#endregion

#region Chains of packs and connections
            // Built here, once the packs of every connection and the resumables are known. The chain written on a pack is the
            // pack's. The chain written on a connection runs over the whole stream of the connection - unless the connection
            // guarantees packs: [UDP], or a Resumable, over TCP as well. A stage keeps its state between the packs of a stream,
            // and that state does not survive a lost datagram, a replay from the journal - which holds the pack as it went out,
            // compressed and encrypted - or a reconnect. There the chain is applied to every pack the connection carries, as if
            // it were written on the pack: to the packs left unguarded too, to the service packs of the resume handshake never.
            // A pack with a chain of its own keeps it.
            foreach( var pack in root_project.packs.Where(pack => pack._chain_attrs != null).ToArray() ) // ToArray: a chain adds its container to the packs
                pack.collect_stream_stages(pack._chain_attrs!);

            foreach( var conn in root_project.connections.Where(conn => conn._chain_attrs != null) )
            {
                if( root_project.resumables.All(res => res.connection != conn) )
                {
                    conn.collect_stream_stages(conn._chain_attrs!); // over the whole stream
                    continue;
                }

                string written(HasDocs on) => "[" + string.Join(", ", on._chain_attrs!.Select(a => a.attr.ToString())) + "]";
                bool   carries(ConnectionImpl by, HostImpl.PackImpl pack) => by.actors.Any(actor => actor.hostL_directly_transmitting_packs.Contains(pack) || actor.hostR_directly_transmitting_packs.Contains(pack));
                bool   per_pack(ConnectionImpl by) => by._chain_attrs != null && root_project.resumables.Any(res => res.connection == by);

                var chain = written(conn);
                var took  = new List<string>();
                foreach( var pack in conn.actors.SelectMany(actor => actor.hostL_directly_transmitting_packs.Concat(actor.hostR_directly_transmitting_packs)).Distinct().Where(pack => !root_project.resumable_service.Contains(pack)).ToArray() )
                {
                    if( pack._chain_attrs != null )
                    {
                        if( pack._chain_of == null ) // a chain of its own
                        {
                            var own     = pack._chain_attrs.Select(a => a.attr.Name.ToString()).ToHashSet();
                            var missing = conn._chain_attrs!.Select(a => a.attr.Name.ToString()).Where(stage => !own.Contains(stage)).ToArray();
                            if( 0 < missing.Length )
                                AdHocAgent.LOG.Warning("The connection {connection} (line {line}) applies its chain {chain} to every pack it carries, but {pack} (line {pack_line}) has a chain of its own, {own}, and keeps it: without {missing}. Write the whole chain on the pack if it is to have them.",
                                                       conn.symbol, conn.line_in_src_code, chain, pack.symbol, pack.line_in_src_code, written(pack), string.Join(", ", missing));
                        }
                        else if( written(pack) != chain )
                            AdHocAgent.exit($"The pack {pack.symbol} (line {pack.line_in_src_code}) travels on {pack._chain_of.symbol}, which applies its chain {written(pack)} to every pack it carries, and on {conn.symbol}, which applies {chain}: a pack has one chain. Write the chain of the pack on the pack itself.", 2);

                        continue;
                    }

                    if( pack.is_File )
                        AdHocAgent.exit($"The connection {conn.symbol} (line {conn.line_in_src_code}) guarantees packs, so its chain {chain} is applied to each pack it carries, and the File pack {pack.symbol} (line {pack.line_in_src_code}) takes none: a File is a single length-prefixed conduit. Make it a Stream pack, or take the chain off the connection and write it on the packs.", 2);

                    foreach( var other in root_project.connections.Where(other => other != conn && !per_pack(other) && carries(other, pack)) )
                        AdHocAgent.exit($"The pack {pack.symbol} (line {pack.line_in_src_code}) travels on {conn.symbol}, which guarantees packs and so applies its chain {chain} to every pack it carries, and on {other.symbol}, which does not: the pack would carry the chain there as well. Write the chain on the pack itself, or give each connection packs of its own.", 2);

                    pack._chain_attrs = conn._chain_attrs;
                    pack._chain_of    = conn;
                    pack.collect_stream_stages(conn._chain_attrs!);
                    took.Add(pack._name);
                }

                AdHocAgent.LOG.Information("The connection {connection} (line {line}) guarantees packs: its chain {chain} is applied to each pack, not to the stream. Packs: {packs}", conn.symbol, conn.line_in_src_code, chain, string.Join(", ", took));
            }
#endregion

#region Resumable: the connection says how long its sessions outlive the socket
            // The declarations of one connection share one parked slot, so the wait is a property of the connection:
            // [Resumable(minutes)] on it, read with the other attributes above. Without it the journal would be dropped
            // with the socket.
            // On a [UDP] connection [UDP(minutes)] says it as well; [UDP] alone leaves it to [Resumable(minutes)].
            // (The stage chain of such a connection went to its packs above: see "Chains of packs and connections".)
            foreach( var conn in root_project.connections.Where(conn => root_project.resumables.Any(res => res.connection == conn)) )
            {
                var minutes = conn._constants_.FirstOrDefault(c => c._name == "resume_");
                var udp     = ResumableImpl.UDP(conn.symbol);
                if( udp is { ConstructorArguments.Length: 1 } )
                {
                    var arg = udp.ConstructorArguments[0];
                    if( minutes == null )
                    {
                        minutes = new HostImpl.PackImpl.ConstantImpl(conn.project, conn.model, arg.Type!, null, arg.Value) { idx = root_project.constant_fields.Count, _name = "resume_" };
                        conn._constants_.Add(minutes);
                        root_project.constant_fields.Add(minutes);
                    }
                    else if( minutes._value_int != Convert.ToInt64(arg.Value) )
                        AdHocAgent.exit($"The connection {conn.symbol} (line {conn.line_in_src_code}) says [UDP({arg.Value})] and [Resumable({minutes._value_int})]: two waits for one session. Keep one of them.", 2);
                }

                if( minutes == null )
                    AdHocAgent.exit(udp == null ?
                                        $"The connection {conn.symbol} (line {conn.line_in_src_code}) declares a Resumable but carries no [Resumable(minutes)]: say how long a session outlives its socket, at least one minute." :
                                        $"The connection {conn.symbol} (line {conn.line_in_src_code}) is [UDP] without minutes: say how long a session outlives its socket - [UDP(minutes)], at least one minute.", 2);
                if( minutes!._value_int < 1 )
                    AdHocAgent.exit($"[{(udp == null ? "Resumable" : "UDP")}({minutes._value_int})] on the connection {conn.symbol} (line {conn.line_in_src_code}) is too short: a resumable session has to outlive its socket for at least one minute.", 2);
            }
#endregion

#region UDP: the generator is told the transport, and the connection has to be one that transport can carry
            // [UDP] is a guarantee for the packs (the Resumables made above) and a transport: the generator wires the connection to
            // the UDP runtime instead of TCP. It learns it the way it learns the minutes: by a marker among the constants of the
            // connection, `udp_`.
            foreach( var conn in root_project.connections )
            {
                if( ResumableImpl.UDP(conn.symbol) == null ) continue;

                foreach( var host in new[] { conn.hostL!, conn.hostR! }.Distinct() )
                    if( (host._langs & Project.Host.Langs.InTS) != 0 )
                        AdHocAgent.exit($"The connection {conn.symbol} (line {conn.line_in_src_code}) is [UDP], and its host {host.symbol} is generated in TypeScript: the TypeScript runtime has no UDP transport - a browser cannot open a UDP socket. Take [UDP] off the connection, or InTS off the host.", 2);

                var mux = root_project.multiplexes.FirstOrDefault(m => m.connections.Contains(conn));
                if( mux != null )
                    AdHocAgent.exit($"The connection {conn.symbol} (line {conn.line_in_src_code}) is [UDP], and the multiplexer {mux.symbol} (line {mux.line_in_src_code}) lists it: a multiplexed port is a TCP port, a UDP connection has a port of its own. Take it out of the multiplexer.", 2);

                var marker = new HostImpl.PackImpl.ConstantImpl(conn.project, conn.model, conn.model.Compilation.GetSpecialType(SpecialType.System_Boolean), null, true) { idx = root_project.constant_fields.Count, _name = "udp_" };
                conn._constants_.Add(marker);
                root_project.constant_fields.Add(marker);
            }
#endregion

            // Configures `across_idx` for managing parent-child relationships.
            //  For instance, a pack declaration may be nested within another pack, a project, or a host. To manage this, we need a consistent ordering.
            // `across_idx` is assigned sequentially across a virtual collection of collections ordered as follows: packs, hosts, multiplexers, connections, actors, states and fields.
            // The Server's `Entity.parent` walks the same order — keep them in step.

            var acrossIdx = (ushort)root_project.packs.Count; //

            foreach( var host1 in root_project.hosts ) host1.across_idx     = acrossIdx++;
            foreach( var mux in root_project.multiplexes ) mux.across_idx   = acrossIdx++;
            foreach( var conn in root_project.connections ) conn.across_idx = acrossIdx++;

            var actors = root_project.connections.SelectMany(conn => conn.actors).ToArray();

            foreach( var actor in actors ) actor.across_idx                                   = acrossIdx++;
            foreach( var state in actors.SelectMany(actor => actor.states) ) state.across_idx = acrossIdx++;
            foreach( var fld in root_project.fields ) fld.across_idx                          = acrossIdx++;

            root_project.across_idx = ushort.MaxValue;

            // Re-stamp AFTER the pass. `reset_static_state()` stamped this before parsing, but a pass
            // that persists new ids/uids writes the description file part-way through and so leaves it
            // newer than that mark — which `refresh()` would then read as "the user edited it again".
            // Anchoring the mark at the end of the pass means only edits made after this point count.
            processing_time = DateTime.Now;

            return root_project; //READY
        }

        // Define a constant for the base UID to avoid magic numbers and improve readability.
        const ulong ProxyUidBase = 0xFFFF;

        // Flag to indicate if the dependency list has changed, triggering a file write.
        bool dependency_list_changed;

        /// <summary>
        /// Recursively collects all unique transitive project dependencies.
        /// </summary>
        /// <param name="dst">A HashSet to store collected projects and prevent cycles.</param>
        void CollectTransitiveDependencies(HashSet<ProjectImpl> dst)
        {
            // Iterate through direct dependencies
            foreach( var prj in symbol!.Interfaces
                                       .Select(i => entities.GetValueOrDefault(i))
                                       .OfType<ProjectImpl>().Where(prj => dst.Add(prj)) )
                prj.CollectTransitiveDependencies(dst);
        }

        HashSet<ProjectImpl>? _all_imports;

        /// <summary>
        /// Every project this one imports, directly or transitively; itself excluded. This is the precedence order
        /// two `Modify&lt;&gt;` modifiers of one target are compared by: the one whose project imports the other's
        /// stands higher and decides.
        /// </summary>
        public HashSet<ProjectImpl> all_imports
        {
            get
            {
                if( _all_imports != null ) return _all_imports;
                CollectTransitiveDependencies(_all_imports = []);
                return _all_imports;
            }
        }

        /// <summary>
        /// The entities of this project that are numbered in its numbers tables rather than by a `/*…*/` mark after their name:
        /// every entity with a name a table line can refer to. Branches have none and keep their marks; fields are not numbered.
        /// Entities the parser makes up with a fixed number (the states of an RPC, the End state) are not persisted at all.
        /// </summary>
        public static bool is_tabled(Entity e) => e switch
                                                  {
                                                      ProjectImpl                               => true,
                                                      HostImpl or ConnectionImpl                => e.symbol != null, // not one the parser made up

                                                      ConnectionImpl.ActorImpl a                => a._fake_uid_pos is >= 0 || a._fake_uid_pos == null && a.symbol != null && a._name != "Actor0",
                                                      ConnectionImpl.StateImpl s                => s._fake_uid_pos == null && s.symbol != null,
                                                      HostImpl.PackImpl p                       => p.symbol != null && !p.is_typedef && p.origin == null && projects.All(prj => prj.proxy != p),
                                                      _                                         => false
                                                  };

        /// <summary>A project's number is the millisecond it was made, counted from 2024-09-22: when that was.</summary>
        static string born(ulong uid) => DateTimeOffset.FromUnixTimeMilliseconds((long)(uid + 0x192_8A31_D95EUL)).ToString("yyyy-MM-dd HH:mm:ss.fff");

        /// <summary>The actors an RPC method makes: one, or two for a bidirectional one — the method's symbol is on both.</summary>
        IEnumerable<ConnectionImpl.ActorImpl> actors_of(ISymbol method) => connections.Where(c => c.project == this).SelectMany(c => c.actors).Where(a => equals(((HasDocs)a).symbol, method));

        /// <summary>Takes the numbers of this project's tables. While marks and tables both exist, the two of one entity must agree.</summary>
        void apply_numbers()
        {
            // A project has one identity. A second mark after its name means it was not seen once and a new one was made:
            // the one to keep is the one the project was born with, and only a human can tell which.
            if( 1 < uid_marks.Count )
            {
                var src = node!.SyntaxTree.GetText();
                AdHocAgent.exit($"The project {_name} has {uid_marks.Count} numbers after its name:\n" +
                                string.Join("\n", uid_marks.Select(m => HasDocs._uid.Match(src.ToString(m)).Groups[1].Value.Split(' ')[0])
                                                           .Select(s => $"  /*{s}*/  born {born(s.to_base256_value())}")) +
                                "\nA project's number is its identity: keep the one it was born with and delete the others.");
            }

            foreach( var (symbol, (list, _, _, line)) in numbers )
            {
                Entity[] targets = symbol is IMethodSymbol ?
                                       [.. actors_of(symbol)] :
                                       entities.TryGetValue(symbol, out var e) ?
                                           [e] :
                                           [];

                if( list.Length == 0 ) // only the wire id this project sends an imported pack under
                {
                    if( targets.Length == 0 || targets[0].project == this || targets[0] is not (HostImpl.PackImpl or ConnectionImpl { is_virtual: true }) )
                        AdHocAgent.exit($"The numbers table of {_name} (line {line}) gives {symbol} no number. Only a pack or a virtual connection of an imported project is listed by its id alone.");
                    continue;
                }

                if( targets.Length == 0 || !is_tabled(targets[0]) )
                    AdHocAgent.exit($"The numbers table of {_name} (line {line}) lists {symbol}, which is not numbered in a table. Remove the line.");

                if( targets[0] == this ) // the project: its number, then its imports - each a signed difference from it, zigzag
                {
                    if( uid != ulong.MaxValue && uid != list[0] )
                        AdHocAgent.exit($"The project {_name} has the mark /*{uid.to_base256_chars()}*/ (born {born(uid)}) and the number {list[0].to_base256_chars()} (born {born(list[0])}) in its numbers table (line {line}). Keep one of them.");
                    uid = list[0];
                    if( 1 < list.Length ) imported_projects_uid = list.Skip(1).Select(z => uid - (ulong)AdHoc.Connection.Receiver.zig_zag(z)).ToArray();
                    continue;
                }

                if( targets[0].project != this )
                    AdHocAgent.exit($"The numbers table of {_name} (line {line}) lists {symbol} of the project {targets[0].project._name}: a project numbers only its own entities.");

                if( list.Length != targets.Length )
                    AdHocAgent.exit($"The numbers table of {_name} (line {line}) gives {symbol} {list.Length} number(s), but it has {targets.Length} to number.");

                for( var i = 0; i < targets.Length; i++ )
                    if( targets[i].uid != ulong.MaxValue && targets[i].uid != list[i] )
                        AdHocAgent.exit($"{symbol} has the mark /*{targets[i].uid.to_base256_chars()}*/ and the number {list[i].to_base256_chars()} in the numbers table (line {line}). Keep one of them.");
                    else targets[i].uid = list[i];
            }
        }

        public override void Init(HashSet<object> once)
        {
            // --- Step 1: Depth-First Recursive Initialization (Post-Order Traversal) ---
            // This ensures all imported projects are initialized before this one, making their
            // entities available for merging.
            foreach( var prj in symbol!.Interfaces
                                       .Select(i => entities.GetValueOrDefault(i))
                                       .OfType<ProjectImpl>() )
                prj.Init(once);

            // Process project-level `_<T>` imports declared on this project's interface BaseList.
            // ProjectImpl.Init bypasses the base Entity.Init, so Init_Collect_Modification (which is
            // what normally walks the BaseList for `_<>`/`X<>` modifications) never fires for projects.
            // Run that walk explicitly here so project-level Pack Set inclusions take effect — most
            // importantly, so constants/enum packs imported via `_<EnumOrConst>` get registered in
            // `project_level_imported_consts` and broadcast to every host (per README distribution rule).
            if( node?.BaseList != null )
                foreach( var item in node.BaseList.Types )
                    modify_by_implement(item, true, (sym, sn, add, depth) => modify(sym, add, depth, once));

            apply_numbers();

            // --- Step 2: Self UID Generation & Transitive Dependency Reconciliation ---
            // This logic runs *after* all dependencies are initialized.

            // A: Generate a new UID for this project if it doesn't have one.
            if( uid == ulong.MaxValue )
            {
                // Only a project that was never numbered gets a new identity. One whose numbers tables are there but lack its
                // line lost it: a new number would silently make it a different project for everything that imports it.
                if( 0 < numbers.Count )
                    AdHocAgent.exit($"The numbers tables of {_name} have no line for the project itself (<see cref='{_name}'/>number). " +
                                    "A project's number is its identity: restore the line - a new number would make it a different project for everything that imports it.");

                uid             = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 0x192_8A31_D95EUL;
                numbers_changed = true; // the project's line in its numbers table
                AdHocAgent.LOG.Information("{project} is a new project: its number is {uid:l}, born {born:l}", _name, uid.to_base256_chars(), born(uid));
            }

            persisted_uid = uid;

            // B: Collect all current transitive dependencies and their UIDs.
            var transitive_dependencies = new HashSet<ProjectImpl>();
            CollectTransitiveDependencies(transitive_dependencies);
            var current_dependency_uids = transitive_dependencies.Select(p => p.uid).ToHashSet();

            // A project's number is its identity: none of the projects it extends may have it, and they are all told apart by it.
            // Checked here, while the numbers are the persisted ones - later the root's becomes 0 and its imports' their slots.
            foreach( var same in transitive_dependencies.Append(this).GroupBy(p => p.uid).Where(g => 1 < g.Count()) )
                AdHocAgent.exit($"The projects {string.Join(", ", same.Select(p => p._name))} have one number {same.Key.to_base256_chars()} (born {born(same.Key)}): one is a copy of the other. " +
                                $"Delete the number of the copy - its project line in the numbers table - and the next run gives it a new one.");

            // C: Reconcile with the persisted list to reuse vacant slots.
            var original_persisted_uids = imported_projects_uid ?? [];
            var reconciled_uids         = original_persisted_uids.ToList(); // Work with a mutable list.

            // In number order: which new import takes which vacant slot must not depend on hash order - a slot is the
            // `project_uid` of every region of the import's entities.
            var new_uids = current_dependency_uids.Except(reconciled_uids).Order().ToList();
            var vacant_indices = new List<int>();

            for( var i = 0; i < reconciled_uids.Count; i++ )
                if( !current_dependency_uids.Contains(reconciled_uids[i]) )
                    vacant_indices.Add(i); // This slot is now "vacant" or a "tombstone".

            // Fill vacant slots first.
            var new_uids_to_fill = Math.Min(new_uids.Count, vacant_indices.Count);
            for( var i = 0; i < new_uids_to_fill; i++ )
            {
                AdHocAgent.LOG.Warning("{project}: the import it recorded in slot {slot}, number {old:l} (born {old_born:l}), is not among its imports any more; {now} (number {new:l}, born {new_born:l}) takes the slot. " +
                                       "If {now} is that import under a new number, its identity was lost and made anew - find out why.",
                                       _name, vacant_indices[i] + 1, reconciled_uids[vacant_indices[i]].to_base256_chars(), born(reconciled_uids[vacant_indices[i]]),
                                       transitive_dependencies.First(p => p.uid == new_uids[i])._name, new_uids[i].to_base256_chars(), born(new_uids[i]));
                reconciled_uids[vacant_indices[i]] = new_uids[i];
            }

            // Append the rest if there are more new UIDs than vacant slots.
            if( new_uids_to_fill < new_uids.Count ) reconciled_uids.AddRange(new_uids.Skip(new_uids_to_fill));

            // D: Check for changes and flag for persistence back to the source file.
            var new_persisted_uids = reconciled_uids.ToArray();
            if( !original_persisted_uids.SequenceEqual(new_persisted_uids) )
            {
                dependency_list_changed = true;
                imported_projects_uid   = new_persisted_uids;
            }

            if( this == root_project )
            {
                uid = 0;
                foreach( var prj in transitive_dependencies )
                {
                    prj.proxy!.parent_artificial = proxy;

                    // The stable index for the proxy UID is now determined from the reconciled list.
                    var stableIndex = Array.IndexOf(imported_projects_uid!, prj.uid) + 1; // + 1 : cause 0 for root_project

                    // Assign the final, stable UID. The IDs are assigned from a reserved range
                    // (e.g., 65535, 65534, ...) to distinguish them from regular pack UIDs.
                    prj.proxy.uid = ProxyUidBase - (ulong)stableIndex;
                    prj.uid       = (ulong)stableIndex;
                }
            }


            // --- Step 3: Entity Composition and Initialization ---

            // Process direct dependencies to merge entities (hosts, connections, etc.)
            foreach( var prj in symbol!.Interfaces
                                       .Select(i => entities.GetValueOrDefault(i))
                                       .OfType<ProjectImpl>() )
            {
                // --- Merge Entities and Validate ---
                // Helper to check for duplicate names, preventing ambiguity in the final protocol.
                void check_duplicate(string entityType, IEnumerable<Entity> mergedSet)
                {
                    var errors = mergedSet.GroupBy(e => e._name)
                                          .Where(g => 1 < g.Count())
                                          .Select(g => $"- {g.Key} (defined in: {string.Join(", ", g.Select(e => e.project._name))})")
                                          .ToList();

                    if( errors.Count != 0 ) AdHocAgent.exit($"The following {entityType} have duplicate names after importing '{prj._name}':\n{string.Join("\n", errors)}\nThis is not allowed. Please rename or remove the duplicates to resolve the conflict.");
                }

                // Merge hosts, connections, and packs from the imported project into the current scope.
                hosts.AddRange(prj.hosts);
                check_duplicate("hosts", hosts);

                connections.AddRange(prj.connections.Where(conn => !conn.is_Modifier));
                check_duplicate("connections", connections);

                // A multiplexer is a connection too: its name shares the connections' namespace.
                multiplexes.AddRange(prj.multiplexes);
                check_duplicate("connections and multiplexers", connections.Cast<Entity>().Concat(multiplexes));

                constants_packs.AddNew(prj.constants_packs);
                all_packs.AddNew(prj.all_packs);
            }

            foreach( var host in hosts.Where(host => host.IsModifier) ) //process host modifications
            {
                var modify_host = host.modify_host;

                modify_host._langs |= host._langs; // Merge the language targets (e.g., InCS, InJAVA).

                // Merge the unresolved language configuration scopes from the modifier to the target host.
                // The main resolution logic in the init() method will process these scopes later,
                // after Pack Sets have been fully resolved.
                modify_host.LangScopes.AddRange(host.LangScopes);
            }

            hosts.RemoveAll(host => host.IsModifier);


            // --- Step 4: CORRECT INITIALIZATION ORDER ---
            // The order of these blocks is CRITICAL for dependency resolution. An entity must not be
            // initialized until all entities it depends on are themselves fully initialized.

            // 4.1: Initialize Connections. This is a foundational step as it resolves the `hostL`
            // and `hostR` properties, which are dependencies for Endpoint Sets.
            once.Clear();
            start();
            foreach( var conn in connections ) conn.Init(once);
            if( restart() )
                foreach( var conn in connections )
                    conn.Init(once);

            // 4.1.x: Reject duplicate connections. A pair of hosts may be joined by at most ONE
            // connection — physical Connects<> and virtual VirtuallyConnects<> draw from the same
            // pool, and the host order in the declaration is irrelevant: Connects<A,B> and
            // Connects<B,A> join the same pair. Parallel declarations are never needed — packs of
            // both directions live in one connection, and a single virtual connection already
            // multiplexes up to MaxTunnels concurrent tunnels over its path.
            {
                var found_duplicates = false;
                foreach( var group in connections.Where(c => !c.is_Modifier && c.hostL != null && c.hostR != null)
                                                 .GroupBy(c => string.CompareOrdinal(c.hostL!._name, c.hostR!._name) < 0 ?
                                                                   (c.hostL._name, c.hostR._name) :
                                                                   (c.hostR._name, c.hostL._name))
                                                 .Where(g => 1 < g.Count()) )
                {
                    found_duplicates = true;
                    var conns = group.ToList();
                    AdHocAgent.LOG.Error("The hosts {hostA} and {hostB} are joined by more than one connection: {connections}. A pair of hosts may be joined by at most one connection — physical or virtual, in either host order. Merge the packs into one connection; a virtual connection already multiplexes up to MaxTunnels concurrent tunnels, so parallel tunnel declarations are redundant.",
                                         conns[0].hostL!.symbol, conns[0].hostR!.symbol,
                                         string.Join(", ", conns.Select(c => $"{c.symbol} (line {c.line_in_src_code})")));
                }

                if( found_duplicates ) AdHocAgent.exit("Fix the problem and restart");
            }

            // 4.1.b: Resolve Multiplex<(C1, C2, …)>. Every connection now has hostL/hostR, so each multiplexer
            // can find the one host its connections share and collect the hosts on their far ends.
            foreach( var mux in multiplexes ) mux.resolve();
            MultiplexImpl.check_shared_connections(multiplexes);

            // 4.1.a: Resolve viaPath for VirtuallyConnects<L, R, PATH>.
            // Now every connection has hostL/hostR set, so we can walk each virtual connection's
            // PATH, validate it forms a contiguous chain of *physical* hops L → PATH → R, and
            // store the chain as indices into this project's `connections` list.
            foreach( var vconn in connections.Where(c => c.is_virtual) )
            {
                var vi      = vconn.symbol!.Interfaces.First(I => equals(I.OriginalDefinition, Meta_VirtuallyConnects));
                var pathArg = vi.TypeArguments[2];

                // PATH is either a single host type (one intermediate hop) or a ValueTuple of host types.
                var intermediates = new List<ITypeSymbol>();
                if( pathArg is INamedTypeSymbol pts && pts.IsTupleType )
                    intermediates.AddRange(pts.TupleElements.Select(e => e.Type));
                else
                    intermediates.Add(pathArg);

                if( intermediates.Count == 0 )
                {
                    AdHocAgent.LOG.Error("The virtual connection {conn} (line {line}) has an empty PATH. At least one intermediate host is required.", vconn.symbol, vconn.line_in_src_code);
                    AdHocAgent.exit("Fix the problem and restart");
                }

                // Assemble the set of hosts the chain must pass through: L, the intermediates,
                // and R. The PATH intermediates may be listed in ANY order — the strict ordered
                // chain L → … → R is reconstructed below by walking the physical Connects<>
                // graph, so the order they appear in the source declaration is irrelevant.
                var nodes = new List<HostImpl> { vconn.hostL! };

                foreach( var t in intermediates )
                {
                    // PATH names the RELAY hosts the tunnelled bytes physically pass through, so every
                    // element must resolve to a declared host. Anything else (a pack, an enum, a stray
                    // type) can never carry a route, and left unchecked it surfaces much later as a
                    // puzzling "cannot be routed".
                    var relay = entities.GetValueOrDefault(t) as HostImpl;

                    if( relay == null )
                    {
                        AdHocAgent.LOG.Error("The virtual connection {conn} (line {line}) lists {type} in PATH, but that is not a host. PATH names the relay hosts the tunnelled bytes physically pass through: every element has to be a `struct : Host` declared in this project.", vconn.symbol, vconn.line_in_src_code, t);
                        AdHocAgent.exit("Fix the problem and restart");
                    }

                    nodes.Add(relay!.IsModifier ?
                                  relay.modify_host! :
                                  relay);
                }

                nodes.Add(vconn.hostR!);

                // Renders a host sequence joined by `sep`, highlighting the positions listed in `mark`.
                string render(IEnumerable<HostImpl> seq, string sep, params int[] mark) =>
                    string.Join(sep, seq.Select((h, i) => mark.Contains(i) ?
                                                              AdHocAgent.ansi(h._name, "1;31") :
                                                              h._name));

                // Reject any repeated host. A tunnel route is a SIMPLE path: every host on it appears
                // exactly once — an endpoint terminates the tunnel, a relay accepts it on one physical leg
                // and forwards it on the next. A host named twice would have to hand the same tunnel back
                // to itself: the bytes re-enter a host that already forwarded them (a routing loop), and
                // both visits compete for the same per-host tunnel_id demux state.
                for( var ci = 0; ci < nodes.Count; ci++ )
                    for( var cj = ci + 1; cj < nodes.Count; cj++ )
                        if( equals(nodes[ci].symbol, nodes[cj].symbol) || nodes[ci]._name == nodes[cj]._name )
                        {
                            AdHocAgent.report("ERR", $"The virtual connection {AdHocAgent.ansi(vconn._name, "36")} (line {vconn.line_in_src_code}) visits the host {AdHocAgent.ansi(nodes[ci]._name, "1")} twice",
                                              $"declared L, PATH…, R: {render(nodes, ", ", ci, cj)}",
                                              "",
                                              "A tunnel route is a simple path L → PATH… → R on which every host appears exactly once:",
                                              "the endpoints terminate the tunnel, and each relay accepts it on one physical leg and",
                                              "forwards it on the next. A host that appears twice would have to hand the same tunnel",
                                              "back to itself — a routing loop, with both visits competing for the same tunnel_id",
                                              "demux state on that host.",
                                              "",
                                              $"Resolution: name {AdHocAgent.ansi(nodes[ci]._name, "1")} once across L, PATH and R. Parallel tunnels between one",
                                              "pair of hosts are expressed by MaxTunnels, never by repeating a host in PATH.");
                            AdHocAgent.exit("Fix the problem and restart");
                        }

                // Index of the physical Connects<> joining hosts a and b (either direction),
                // or -1 when no physical edge between them was declared.
                int physicalEdge(HostImpl a, HostImpl b)
                {
                    for( var ci = 0; ci < connections.Count; ci++ )
                    {
                        var c = connections[ci];
                        if( c.is_virtual ) continue; // virtuals can only ride physical edges
                        if( c.hostL == null || c.hostR == null ) continue;
                        if( (equals(c.hostL.symbol, a.symbol) && equals(c.hostR.symbol, b.symbol)) ||
                            (equals(c.hostL.symbol, b.symbol) && equals(c.hostR.symbol, a.symbol)) )
                            return ci;
                    }

                    return -1;
                }

                // Physical-edge adjacency between every pair of listed hosts.
                var nodeCount = nodes.Count;
                var edge      = new int[nodeCount, nodeCount];
                for( var i = 0; i < nodeCount; i++ )
                    for( var j = 0; j < nodeCount; j++ )
                        edge[i, j] = i == j ?
                                         -1 :
                                         physicalEdge(nodes[i], nodes[j]);

                // Discover the Hamiltonian path from L (node 0) to R (last node) that visits every
                // listed host exactly once along physical edges. L is pinned as the start and R as
                // the end; the order of the intermediates between them is what we solve for. The
                // route must be unique — otherwise the chain would be ambiguous.
                var endNode   = nodeCount - 1;
                var visited   = new bool[nodeCount];
                var order     = new List<int>(nodeCount);
                var solutions = new List<int[]>();

                void walk(int cur)
                {
                    if( solutions.Count > 1 ) return; // ambiguity already proven — stop early
                    order.Add(cur);
                    visited[cur] = true;

                    if( order.Count == nodeCount )
                    {
                        if( cur == endNode ) solutions.Add(order.ToArray());
                    }
                    else
                        for( var nx = 0; nx < nodeCount; nx++ )
                        {
                            if( visited[nx] || edge[cur, nx] == -1 ) continue;
                            if( nx                           == endNode && order.Count != nodeCount - 1 ) continue; // R must come last
                            walk(nx);
                        }

                    order.RemoveAt(order.Count - 1);
                    visited[cur] = false;
                }

                walk(0);

                if( solutions.Count == 0 )
                {
                    // The chain can only be assembled from the physical legs that exist between the listed
                    // hosts, so listing them puts the gap in plain sight.
                    var legs = new List<string>();
                    for( var i = 0; i < nodeCount; i++ )
                        for( var j = i + 1; j < nodeCount; j++ )
                            if( edge[i, j] != -1 )
                                legs.Add($"{nodes[i]._name} ↔ {nodes[j]._name} ({connections[edge[i, j]]._name})");

                    AdHocAgent.report("ERR", $"The virtual connection {AdHocAgent.ansi(vconn._name, "36")} (line {vconn.line_in_src_code}) cannot be routed",
                                      $"declared L, PATH…, R: {render(nodes, ", ")}",
                                      $"physical legs among these hosts: {(legs.Count == 0 ? "none" : string.Join(", ", legs))}",
                                      "",
                                      "The listed hosts must form ONE contiguous chain of physical Connects<> from L to R that",
                                      "visits each of them exactly once. Add the missing Connects<> leg, or list in PATH every",
                                      "intermediate host the route really passes through — a host left out of PATH is not routed",
                                      "through, and a host that is not on the physical chain cannot be routed at all.");
                    AdHocAgent.exit("Fix the problem and restart");
                }

                if( solutions.Count > 1 )
                {
                    AdHocAgent.report("ERR", $"The virtual connection {AdHocAgent.ansi(vconn._name, "36")} (line {vconn.line_in_src_code}) is ambiguous",
                                      $"route: {render(solutions[0].Select(n => nodes[n]), " → ")}",
                                      $"route: {render(solutions[1].Select(n => nodes[n]), " → ")}",
                                      "",
                                      "The listed hosts admit more than one physical chain from L to R, so the tunnel has no",
                                      "single route to generate. Adjust PATH — or the topology — so exactly one chain visits",
                                      "every listed host once.");
                    AdHocAgent.exit("Fix the problem and restart");
                }

                // Translate the discovered host order into the strictly ordered list of physical
                // connection indices: viaPath = [L↔n1, n1↔n2, …, nk↔R].
                var chain    = solutions[0];
                var resolved = new int[nodeCount - 1];
                for( var hop = 0; hop < nodeCount - 1; hop++ )
                    resolved[hop] = edge[chain[hop], chain[hop + 1]];

                // Last gate before the route reaches the generator: it must still be a simple path. `walk`
                // marks every node it enters, so a host seen twice here — or one physical leg carrying two
                // hops of the same route — can only mean the search itself regressed. Report the offending
                // route instead of shipping a viaPath that loops back through a host.
                var repeated = chain.GroupBy(n => nodes[n]._name).FirstOrDefault(g => 1 < g.Count());

                if( repeated != null || resolved.Distinct().Count() != resolved.Length || resolved.Any(e => e < 0) )
                {
                    int[] marks = repeated == null ?
                                      [] :
                                      chain.Select((n, i) => (n, i)).Where(x => nodes[x.n]._name == repeated.Key).Select(x => x.i).ToArray();

                    AdHocAgent.report("ERR", $"The virtual connection {AdHocAgent.ansi(vconn._name, "36")} (line {vconn.line_in_src_code}) resolved to a route that is not a simple path",
                                      $"resolved: {render(chain.Select(n => nodes[n]), " → ", marks)}",
                                      repeated != null ?
                                          $"the host {AdHocAgent.ansi(repeated.Key, "1")} appears {repeated.Count()} times on it" :
                                          "one physical leg carries two different hops of the same route",
                                      "",
                                      "A tunnel enters and leaves every host on its route exactly once. The declaration itself",
                                      "passed validation, so this route came out of the search — please report it as a bug.");
                    AdHocAgent.exit("Fix the problem and restart");
                }

                vconn.viaPath = resolved;

                AdHocAgent.LOG.Information("The virtual connection {conn} (line {line}) is routed {route} over {legs}", vconn._name, vconn.line_in_src_code,
                                           string.Join(" → ", chain.Select(n => nodes[n]._name)),
                                           string.Join(" + ", resolved.Select(ri => connections[ri]._name)));
            }

            // 4.1.b: Initialize Actors (resolve named actors and their states)
            start();
            foreach( var actor in connections.SelectMany(c => c.actors) ) actor.Init(once);
            if( restart() )
                foreach( var actor in connections.SelectMany(c => c.actors) )
                    actor.Init(once);

            // 4.2: Initialize Pack Sets (`NamedPackSet`). These are needed by states.
            once.Clear();
            start();
            foreach( var pack in named_packs.Values ) pack.Init(once);
            if( restart() )
                foreach( var pack in named_packs.Values )
                    pack.Init(once);

            // 4.2.b: Resolve the requests and responses of the RPC-shorthand methods into their branches, by the resolver every
            // Pack Set goes through. Must run after Pack Sets are fully resolved (4.2) and before States (4.3) consume the branches.
            foreach( var (branch, resolver, exprs, what, ln) in rpc_deferred_packset_expansions )
            {
                var resolved = new List<HostImpl.PackImpl>();
                foreach( var expr in exprs ) resolver.resolve_pack_set(expr, resolved);

                if( resolved.Count == 0 )
                    AdHocAgent.exit($"Line {ln}: the {what} ({string.Join(", ", exprs.Select(e => e.ToString()))}) resolves to no transmittable pack. " +
                                    "It takes what a branch's PACKS takes: packs, Pack Sets, Project/Host/Pack scopes with or without `@`, " +
                                    "`_<…>`, `X<…>` exclusions, filter templates, or a tuple of those.");

                foreach( var pack in resolved )
                    if( !branch.packs.Contains(pack) )
                        branch.packs.Add(pack.ValidateTypeUsage(ln));
            }

            rpc_deferred_packset_expansions.Clear();

            // 4.3: Initialize States. States depend on Connections (for context) and Pack Sets (for content).
            once.Clear();
            start();
            foreach( var st in connections.SelectMany(conn => conn.actors.SelectMany(actor => actor.states)).ToArray() )
                st.Init(once);

            // 4.4: Initialize and apply pack modifications (Injectors and Headers).
            // This block must run before final pack initialization.
            start();
            foreach( var pack in injector_packs ) pack.Init(once);
            if( restart() )
                foreach( var pack in injector_packs )
                    pack.Init(once);

            foreach( var injector in injector_packs ) //inject template fields
                foreach( var target in injector.target_packs!.Where(pack => !pack.is_Header && !pack.is_HeaderModifier && !pack.is_Modifier && !pack.is_FieldsInjectInto) )
                {
                    target.fields.RemoveAll(fld => injector.fields.Any(f => fld._name == f._name));
                    target.fields.AddRange(injector.fields);
                }


            start();
            foreach( var pack in header_packs ) pack.Init(once);
            if( restart() )
                foreach( var pack in header_packs )
                    pack.Init(once);

            //==================
            start();
            foreach( var header_modifier in header_modifiers )
            {
                if( header_modifier.target_packs == null )
                {
                    var header = (HostImpl.PackImpl)entities[header_modifier.this_modify.First().TypeArguments[0]];
                    header_modifier.target_packs = header.target_packs; //redirect modification to header of the header target packs collection
                }

                header_modifier.Init(once);
            }

            if( restart() )
                foreach( var header_modifier in header_modifiers )
                    header_modifier.Init(once);


            foreach( var header_modifier in header_modifiers ) //inject fields to headers from modifiers
            {
                var header = (HostImpl.PackImpl)entities[header_modifier.this_modify.First().TypeArguments[0]];
                header.fields.RemoveAll(fld => header_modifier.fields.Any(f => fld._name == f._name));
                header.fields.AddRange(header_modifier.fields);
            }


            // 4.5: Initialize All Regular Packs. This is the final pack-level initialization.
            once.Clear();
            start();
            foreach( var pack in all_packs )
            {
                pack.Init(once);
                pack._nested_max = (byte)pack.calculate_fields_type_depth(once);
            }

            if( restart() )
                foreach( var pack in all_packs )
                    pack.Init(once);

            // 4.6:Initialize Endpoint Sets. This MUST happen AFTER connections and hosts
            // are fully initialized, ensuring that `EndpointSetImpl.modify` can successfully resolve
            // the `IfSendingFrom<Host, Connection>` definitions.
            once.Clear();
            start();
            foreach( var endpointSet in endpoint_sets ) endpointSet.Init(once);
            if( restart() )
                foreach( var endpointSet in endpoint_sets )
                    endpointSet.Init(once);

#region Assign project scope UIDs
            List<Entity>   unassigned = [];
            HashSet<ulong> assigned   = [];

            // `library`: the entities of the same kind this project imports. Their numbers are the library's own - it lives its own
            // life and does not know who extends it - so this project numbers after them: a new entity takes a number above the
            // library's greatest, and an entity whose number the library took since is renumbered, its old number kept in its
            // table line (`~old`) for its custom-code regions to follow.
            void set_persistent_uid(IEnumerable<Entity> entities, IEnumerable<Entity>? library = null) // Our goal is to minimize the UID, to reduce the footprint in the source code
            {
                var uid = 0UL;
                if( library != null )
                {
                    // Actor0 is the body of its connection: it has a fixed number of its own and takes part in no numbering
                    var theirs = library.Where(e => e.uid < ulong.MaxValue && e._name != "Actor0").GroupBy(e => e.uid).ToDictionary(g => g.Key, g => g.First());
                    if( 0 < theirs.Count ) uid = theirs.Keys.Max() + 1;
                    assigned.UnionWith(theirs.Keys);

                    foreach( var entity in entities.Where(e => e.origin == null && e._name != "Actor0" && e.uid < ulong.MaxValue && theirs.ContainsKey(e.uid)) )
                    {
                        AdHocAgent.LOG.Warning("{entity} (line {line}) has the number {uid:l}, which the imported {library} gives to {theirs}. The imported project decides: {entity} is renumbered, its custom code moves to the regions of its new number.",
                                               entity.symbol, entity.line_in_src_code, entity.uid.to_base256_chars(), theirs[entity.uid].project._name, theirs[entity.uid].symbol);
                        renumbered.TryAdd(entity, entity.uid);
                        entity.uid = ulong.MaxValue;
                    }
                }

                // Check for duplicate UIDs within the same project.
                foreach( var by_projects in entities
                                            .Where(e => e._name != "Actor0") //special case. have no own uid cause shared body with a connection
                                            .Where(e => e.uid   < ulong.MaxValue)
                                            .GroupBy(e => e.project) ) // Group by project
                    foreach( var by_uid in by_projects
                                           .GroupBy(e => e.uid)
                                           .Where(g => 1 < g.Count()) ) // Find duplicate UIDs within the project group
                    {
                        var list = string.Join("\n", by_uid.Select(e => $"{e.symbol}  line: {e.line_in_src_code}"));
                        AdHocAgent.LOG.Warning(
                                               "Duplicate entities detected: {List} with the same UID = {Id}. This may have been accidentally copied. Please delete the duplicate assignment.",
                                               list,
                                               $"/*{by_uid.Key.to_base256_chars()}*/"
                                              );
                        AdHocAgent.exit("", 66);
                    }

                foreach( var entity in entities.Where(e => e.origin == null) ) // Exclude clones and include only items belonging to this project
                    if( entity.uid == ulong.MaxValue ) unassigned.Add(entity);
                    else assigned.Add(entity.uid);

                foreach( var entity in unassigned )
                {
                    while( assigned.Contains(uid) ) uid++;
                    if( is_tabled(entity) ) numbers_changed = true; // its number goes to the tables
                    else
                    {
                        if( entity.uid_pos < 0 ) throw new();
                        updated_uid.Add((entity.uid_pos, uid));
                    }

                    entity.uid = (ushort)uid++;
                }

                assigned.Clear();
                unassigned.Clear();
            }

            // The uid of a host that dials a multiplexed port is the byte it opens the port with: one byte, and no other host
            // dialing that port may have it. A clash is never renumbered here: the custom-code regions of a host are keyed by
            // its uid, and a silent renumbering would orphan them.
            void check_host_uids()
            {
                var failed = false;

                foreach( var host in hosts.Where(h => !h.IsModifier && 0xFF < h.uid && h.uid < ulong.MaxValue) )
                {
                    failed = true;
                    AdHocAgent.LOG.Error("The host {host} ({project}, line {line}) has the uid {uid:l}. A host uid is one byte, 0..255: it is the byte the host opens a multiplexed port with.",
                                         host.symbol, host.project._name, host.line_in_src_code, $"{host.uid} ({host.uid.to_base256_chars()})");
                }

                foreach( var mux in multiplexes )
                    foreach( var same in mux.hosts.Skip(1).Distinct().GroupBy(h => h.uid).Where(g => 1 < g.Count()) )
                    {
                        failed = true;
                        AdHocAgent.LOG.Error("The hosts\n{hosts:l}\ndial the multiplexer {mux} (line {line}) with the same uid {uid:l}: the port cannot tell them apart. Delete the lines of every host but one in the hosts table, and the next run gives them free uids.",
                                             string.Join("\n", same.Select(h => $"  {h.symbol} ({h.project._name}, line {h.line_in_src_code})")),
                                             mux.symbol, mux.line_in_src_code, $"{same.Key} ({same.Key.to_base256_chars()})");
                    }

                if( failed ) AdHocAgent.exit("Fix the problem and restart");
            }

            // Every entity is told apart across the project and its imports by its project's slot and its own number - except a
            // host on the wire: its number alone is the byte it opens a multiplexed port with. So hosts, and only hosts, are
            // numbered across the project and everything it imports: after the imported ones, which keep theirs.
            set_persistent_uid(hosts.Where(e => e.project == this), hosts.Where(e => e.project != this));
            check_host_uids();

            set_persistent_uid(connections.Where(e => e.project == this));

            // Set UIDs for Actors
            set_persistent_uid(connections.Where(e => e.project == this).SelectMany(c => c.actors));

            set_persistent_uid(all_packs.Concat(constants_packs).Where(p => !p.is_typedef && p.project == this));

            HashSet<ConnectionImpl.BranchImpl> unassigned_ = [];

            foreach( var states in connections.Where(e => e.project == this).SelectMany(c => c.actors).Select(actor => actor.states) )
            {
                set_persistent_uid(states);

                foreach( var state in states )
                {
                    foreach( var branch in state.branchesR.Where(br => br.origin == null) )
                        if( branch.uid == ulong.MaxValue ) unassigned_.Add(branch);
                        else assigned.Add(branch.uid);

                    foreach( var branch in state.branchesL.Concat(state.branchesR.Where(br => br.origin == null)) ) //exclude R branches of LR (clones of L)
                        if( branch.uid == ulong.MaxValue ) unassigned_.Add(branch);
                        else assigned.Add(branch.uid);

                    var uid = 0UL;
                    foreach( var branch in unassigned_ )
                    {
                        while( assigned.Contains(uid) ) uid++;
                        if( branch.uid_pos < 0 ) throw new();
                        updated_uid.Add((branch.uid_pos, uid));
                        branch.uid = (ushort)uid++;
                    }

                    foreach( var br in state.branchesR.Where(br => br.origin != null) ) //only R branches of LR (clones of L)
                    {
                        if( br.origin!.uid == ulong.MaxValue ) //it can happen if br.origin was deleted as the result of modification
                        {
                            while( assigned.Contains(uid) ) uid++;
                            br.origin.uid = uid++;
                        }

                        br.uid = 0xFFFF - br.origin.uid;
                    }

                    assigned.Clear();
                    unassigned_.Clear();
                }
            }
#endregion
        }

        /// <summary>
        /// List to store updated UIDs and their positions in the source file for writing back.
        /// Item1: Position in the source file where the UID comment should be updated.
        /// Item2: The new UID value.
        /// </summary>
        List<(int, ulong)> updated_uid = [];

        public override bool Init_As_Modifier_Dispatch_Modifications_On_Targets(HashSet<object> once) => false;

        /// <summary>
        /// Constants/enum packs explicitly imported via project-level `_<T>` on this project's interface.
        /// Per the README "Distribution Over Hosts" rule, these must be broadcast to every host in the
        /// project — even if T is declared inside a nested host scope (the import is the override).
        /// </summary>
        public HashSet<HostImpl.PackImpl> project_level_imported_consts = [];

        public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once)
        {
            if( entities.TryGetValue(by_what, out var value) ) //project's entity
                switch( value )
                {
                    case ProjectImpl: return;
                    case HostImpl host:
                        if( add )
                            AdHocAgent.LOG.Information("Only enums, constant sets, connections, or other projects can be imported into the project. Importing the host {host} (line {line}) will be ignored. Instead, reference the host {host} as the endpoint within the {project} connections.", host, host.line_in_src_code, host, _name);
                        else
                        {
                            hosts.Remove(host);
                            connections.RemoveAll(conn => conn.hostR == host || conn.hostL == host);
                        }

                        break;
                    case HostImpl.PackImpl pack: //import single pack to the project

                        if( pack.is_constants_set || pack.is_enum )
                        {
                            if( !add )
                            {
                                constants_packs.Remove(pack);
                                project_level_imported_consts.Remove(pack);
                            }
                            else
                            {
                                if( !constants_packs.Contains(pack) ) constants_packs.Add(pack);
                                project_level_imported_consts.Add(pack);
                            }

                            return;
                        }

                        if( add )
                            AdHocAgent.LOG.Information("Only enums, constant sets, connections, or other projects may be imported into the project. Importing the pack {pack} (line {line}) will be ignored. instead, reference the pack {pack} in a branch of a state within the {project} connections.", pack, pack.line_in_src_code, pack, _name);
                        else
                            foreach( var st in connections.SelectMany(conn => conn.actors.SelectMany(actor => actor.states)) )
                            {
                                foreach( var br in st.branchesL )
                                    br.packs.Remove(pack);
                                foreach( var br in st.branchesR )
                                    br.packs.Remove(pack);
                            }

                        break;
                    case ConnectionImpl connection: // import single connection to the project.

                        if( !add ) connections.Remove(connection);
                        else if( !connections.Contains(connection) ) connections.Add(connection);
                        return;
                }
            else AdHocAgent.exit($"The source code for '{by_what}' could not be found. Have you remembered to include the source files of other imported projects?");
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectImpl"/> class.
        /// </summary>
        /// <param name="root_project">The root project, or null if this is the root project itself.</param>
        /// <param name="compilation">The Roslyn compilation object providing semantic information.</param>
        /// <param name="node">The syntax node representing the project's interface declaration.</param>
        /// <param name="namespace_">The namespace of the project.</param>
        public ProjectImpl(ProjectImpl? root_project, CSharpCompilation compilation, InterfaceDeclarationSyntax node, string namespace_) : base(null, compilation, node)
        {
            file_path = node.SyntaxTree.FilePath;

            _namespacE = namespace_;
            this.node  = node;

            if( root_project == null ) return; //not root project
            project = root_project;
        }

        /// <summary>
        /// A proxy pack used to represent this project in the entity hierarchy when it is imported by another project.
        /// </summary>
        public HostImpl.PackImpl? proxy;

        /// <summary>
        /// Gets the current task name from <see cref="AdHocAgent"/>.
        /// </summary>
        public string? _task => AdHocAgent.task;

        /// <summary>
        /// Gets or sets the namespace of the project.
        /// </summary>
        public string? _namespacE { get; set; }

        /// <summary>
        /// Gets or sets a timestamp for the project.
        /// </summary>
        public long _time { get; set; }

        /// <summary>
        /// Open source mode: the URL of the project's `adhoc` folder on GitHub, verified by <see cref="AdHocAgent.OpenSource"/>
        /// before the upload. Null when the URL is not given.
        /// </summary>
        public string? _github => AdHocAgent.github;

        /// <summary>
        /// Open source mode: the project's topical tags, English words separated by a single space. Null when the URL is not given.
        /// </summary>
        public string? _tags => AdHocAgent.tags;


        /// <summary>
        /// The protocol description files this project was built from, as the <c>source</c> field's file tree — one
        /// entry per file, each streamed straight from disk and the whole list compressed by the pack's `[Zstd]` chain.
        /// Filled in <see cref="init"/>; null until then (and on the Observer path, which never uploads).
        /// </summary>
        public Files? source;

        public Agent.AdHocProtocol.FileEntry.List? _source => source;

        /// <summary>
        /// A list of all hosts defined in this project.
        /// </summary>
        public List<HostImpl> hosts = new(3);

        /// <summary>
        /// Gets the number of hosts in this project.
        /// </summary>
        public int _hosts_len => hosts.Count;

        /// <summary>
        /// Provides access to the list of hosts for serialization purposes.
        /// </summary>
        /// <returns>The list of hosts.</returns>
        public object? _hosts() => hosts;

        /// <summary>
        /// Retrieves a specific host at the given index.
        /// </summary>
        /// <param name="ctx">The transmitter context (not used here).</param>
        /// <param name="slot">The transmitter slot (not used here).</param>
        /// <param name="d">The index of the host to retrieve.</param>
        /// <returns>The host at the specified index.</returns>
        public Project.Host _hosts(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int d) => hosts[d];


        /// <summary>
        /// Gets a distinct collection of all fields, including virtual 'V' fields used as Map value types.
        /// </summary>
        /// <returns>IEnumerable of FieldImpl representing all fields.</returns>
        static IEnumerable<HostImpl.PackImpl.FieldImpl?> all_fields() => raw_fields.Values.Concat(raw_fields.Values.Select(fld => fld.V).Where(fld => fld != null)).Distinct();

        /// <summary>
        /// Dictionary to store raw field definitions parsed from the source code.
        /// Key: Symbol representing the field.
        /// Value: FieldImpl instance.
        /// </summary>
        public static Dictionary<ISymbol, HostImpl.PackImpl.FieldImpl> raw_fields = new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Dictionary to store raw static field (constant) definitions parsed from the source code.
        /// Key: Symbol representing the static field.
        /// Value: ConstantImpl instance.
        /// </summary>
        public static Dictionary<ISymbol, HostImpl.PackImpl.ConstantImpl> raw_static_fields = new(SymbolEqualityComparer.Default);

        /// <summary>
        /// List of constant fields defined in this project.
        /// </summary>
        public List<HostImpl.PackImpl.ConstantImpl> constant_fields = [];

        /// <summary>
        /// Provides access to the list of constant fields for serialization purposes.
        /// Returns null if the list is empty.
        /// </summary>
        /// <returns>The list of constant fields or null if empty.</returns>
        public object? _constant_fields() => 0 < constant_fields.Count ?
                                                 constant_fields :
                                                 null;

        /// <summary>
        /// Gets the count of constant fields in this project.
        /// </summary>
        public int _constant_fields_len => constant_fields.Count;

        /// <summary>
        /// Retrieves a specific constant field at the given index.
        /// </summary>
        /// <param name="ctx">The transmitter context (not used here).</param>
        /// <param name="__slot">The transmitter slot (not used here).</param>
        /// <param name="item">The index of the constant field to retrieve.</param>
        /// <returns>The constant field at the specified index.</returns>
        public Project.Host.Pack.Constant _constant_fields(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item) => constant_fields[item];

        /// <summary>
        /// List of fields defined in this project.
        /// </summary>
        public List<HostImpl.PackImpl.FieldImpl> fields = [];

        /// <summary>
        /// Provides access to the list of fields for serialization purposes.
        /// Returns null if the list is empty.
        /// </summary>
        /// <returns>The list of fields or null if empty.</returns>
        public object? _fields() => 0 < fields.Count ?
                                        fields :
                                        null;

        /// <summary>
        /// Gets the count of fields in this project.
        /// </summary>
        public int _fields_len => fields.Count;

        /// <summary>
        /// Retrieves a specific field at the given index.
        /// </summary>
        /// <param name="ctx">The transmitter context (not used here).</param>
        /// <param name="slot">The transmitter slot (not used here).</param>
        /// <param name="item">The index of the field to retrieve.</param>
        /// <returns>The field at the specified index.</returns>
        public Project.Host.Pack.Field _fields(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item) => fields[item];

        /// <summary>
        /// List of all packs defined in this project, including transmittable and non-transmittable packs.
        /// </summary>
        public readonly List<HostImpl.PackImpl> all_packs = []; //eventually only transmittable packs

        /// <summary>
        /// List of constant packs defined in this project, including enums and constant sets.
        /// </summary>
        public readonly List<HostImpl.PackImpl> constants_packs = []; //enums + constant sets

        /// <summary>
        /// List of packs that are used in the project.
        /// </summary>
        public List<HostImpl.PackImpl> packs;

        /// <summary>
        /// Provides access to the list of packs for serialization purposes.
        /// Returns null if the list is empty.
        /// </summary>
        /// <returns>The list of packs or null if empty.</returns>
        public object? _packs() => 0 < packs.Count ?
                                       packs :
                                       null;

        /// <summary>
        /// Gets the count of packs in this project.
        /// </summary>
        public int _packs_len => packs.Count;

        /// <summary>
        /// Retrieves a specific pack at the given index.
        /// </summary>
        /// <param name="ctx">The transmitter context (not used here).</param>
        /// <param name="slot">The transmitter slot (not used here).</param>
        /// <param name="d">The index of the pack to retrieve.</param>
        /// <returns>The pack at the specified index.</returns>
        public Project.Host.Pack _packs(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int d) => packs[d];

        /// <summary>
        /// List of connections defined in this project.
        /// </summary>
        public List<ConnectionImpl> connections = [];

        /// <summary>
        /// Gets the count of connections in this project.
        /// </summary>
        public int _connections_len => connections.Count;

        /// <summary>
        /// Provides access to the list of connections for serialization purposes.
        /// Returns null if the list is empty.
        /// </summary>
        /// <returns>The list of connections or null if empty.</returns>
        public object? _connections() => connections.Count < 1 ?
                                             null :
                                             connections;


        /// <summary>
        /// Retrieves a specific connection at the given index.
        /// </summary>
        /// <param name="ctx">The transmitter context (not used here).</param>
        /// <param name="slot">The transmitter slot (not used here).</param>
        /// <param name="d">The index of the connection to retrieve.</param>
        /// <returns>The connection at the specified index.</returns>
        public Project.Connection _connections(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int d) => connections[d];

        /// <summary>
        /// Represents the implementation of a Host entity within a project.
        /// </summary>
        public class HostImpl : Entity, Project.Host{
            public byte _uid => (byte)uid;

            /// <summary>
            /// Iterates through packs within the host's scope, applying an action to transmittable packs.
            /// If depth is 0, includes only packs defined directly under the host.
            /// If depth > 0, recursively includes all nested packs within the host.
            /// </summary>
            /// <param name="depth">The depth of scope to traverse (0 for immediate host scope, >0 for deeper scopes).</param>
            /// <param name="dst">The action to apply to each transmittable pack.</param>
            public void for_packs_in_scope(uint depth, Action<PackImpl> dst)
            {
                foreach( var entity in entities.Values.Where(e => e.in_host == this && (0 < depth || e.parent_by_source_code == this)) )
                    if( entity is PackImpl pack && pack.is_transmittable )
                        dst(pack);
            }

            public class LangScope{
                public          uint                                   Config;
                public readonly List<(ISymbol symbol, bool recursive)> Targets = [];
            }

            public readonly List<LangScope> LangScopes = [];

            /// <summary>
            /// Gets or sets the target languages for which code should be generated for this host.
            /// </summary>
            public Project.Host.Langs _langs { get; set; }

            public override bool included   => _included ?? in_project.included;
            public          bool IsModifier => modify_host != null;

            /// <summary>
            /// Initializes a new instance of the <see cref="HostImpl"/> class.
            /// </summary>
            /// <param name="project">The project this host belongs to.</param>
            /// <param name="compilation">The Roslyn compilation object.</param>
            /// <param name="host">The syntax node representing the host's struct declaration.</param>
            public HostImpl(ProjectImpl project, CSharpCompilation compilation, StructDeclarationSyntax host) : base(project, compilation, host)
            {
                // 0xFFFFFFFF means:
                // Low 16 bits (0xFFFF): All languages implement the pack/field
                // High 16 bits (0xFFFF): All languages generate Hash/Equals
                _default_impl_hash_equal = 0xFFFF_FFFF;
                project.hosts.Add(this);
            }

            /// <summary>
            /// Gets the host being modified by this host, if this host is a modifier.
            /// Returns null if this host is not a modifier.
            /// </summary>
            public HostImpl? modify_host => equals(symbol!.Interfaces[0].ConstructedFrom, Meta_Modify_Target) ?
                                                (HostImpl)entities[symbol!.Interfaces[0].TypeArguments[0]] :
                                                null;

#region Pack implementation hash and equals configuration
            /// <summary>
            /// Dictionary to store pack-specific implementation configurations for hash code and equals methods.
            /// Key: Symbol representing the pack.
            /// Value: Implementation flags in the Low 16 bits and Hash/Equals flags in the High 16 bits.
            /// </summary>
            public readonly Dictionary<ISymbol, uint> pack_impl = new(SymbolEqualityComparer.Default); //pack -> impl information

            /// <summary>
            /// Enumerator for iterating over the <see cref="pack_impl"/> dictionary.
            /// </summary>
            Dictionary<ISymbol, uint>.Enumerator pack_impl_enum;

            /// <summary>
            /// Provides access to the pack implementation hash and equals configuration dictionary for serialization purposes.
            /// Returns null if the dictionary is empty.
            /// </summary>
            /// <returns>The pack implementation hash and equals configuration dictionary or null if empty.</returns>
            public object? _pack_impl_hash_equal() => pack_impl.Count == 0 ?
                                                          null :
                                                          pack_impl;

            /// <summary>
            /// Gets the count of pack implementation hash and equals configurations.
            /// </summary>
            public int _pack_impl_hash_equal_len => pack_impl.Count;

            /// <summary>
            /// Initializes the enumerator for iterating over the <see cref="pack_impl"/> dictionary during serialization.
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            public void _pack_impl_hash_equal_Init(Base_.Transmitter ctx, Base_.Transmitter.Slot slot) => pack_impl_enum = pack_impl.GetEnumerator();


            /// <summary>
            /// Moves to the next item in the <see cref="pack_impl"/> dictionary and retrieves the key (pack index).
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <returns>The index of the next pack.</returns>
            public ushort _pack_impl_hash_equal_NextItem_Key(Base_.Transmitter ctx, Base_.Transmitter.Slot slot)
            {
                pack_impl_enum.MoveNext();
                return (ushort)entities[pack_impl_enum.Current.Key].idx;
            }

            /// <summary>
            /// Retrieves the value (configuration flags) for the current item in the <see cref="pack_impl"/> dictionary.
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <returns>The configuration flags for the current pack.</returns>
            public uint _pack_impl_hash_equal_Val(Base_.Transmitter ctx, Base_.Transmitter.Slot slot) => pack_impl_enum.Current.Value;

            /// <summary>
            /// Default implementation configuration for hash code and equals methods.
            /// By default, auto-generation is enabled for all languages.
            /// </summary>
            public uint _default_impl_hash_equal { get; set; } //by default a bit per language
#endregion

#region Field implementation language configuration
            /// <summary>
            /// Dictionary to store field-specific language implementation configurations.
            /// Key: Symbol representing the field.
            /// Value: Langs enum value indicating the language-specific implementation setting.
            /// </summary>
            public readonly Dictionary<ISymbol, Project.Host.Langs> field_impl = new(SymbolEqualityComparer.Default);

            /// <summary>
            /// Enumerator for iterating over the <see cref="field_impl"/> dictionary.
            /// </summary>
            Dictionary<ISymbol, Project.Host.Langs>.Enumerator field_impl_enum;

            /// <summary>
            /// Provides access to the field implementation language configuration dictionary for serialization purposes.
            /// Returns null if the dictionary is empty.
            /// </summary>
            /// <returns>The field implementation language configuration dictionary or null if empty.</returns>
            public object? _field_impl() => field_impl.Count == 0 ?
                                                null :
                                                field_impl;

            /// <summary>
            /// Gets the count of field implementation language configurations.
            /// </summary>
            public int _field_impl_len => field_impl.Count;

            /// <summary>
            /// Initializes the enumerator for iterating over the <see cref="field_impl"/> dictionary during serialization.
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            public void _field_impl_Init(Base_.Transmitter ctx, Base_.Transmitter.Slot slot) => field_impl_enum = field_impl.GetEnumerator();

            /// <summary>
            /// Moves to the next item in the <see cref="field_impl"/> dictionary and retrieves the key (field index).
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <returns>The index of the next field.</returns>
            public ushort _field_impl_NextItem_Key(Base_.Transmitter ctx, Base_.Transmitter.Slot slot)
            {
                field_impl_enum.MoveNext();
                return (ushort)raw_fields[field_impl_enum.Current.Key].idx;
            }

            /// <summary>
            /// Retrieves the value (language configuration) for the current item in the <see cref="field_impl"/> dictionary.
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <returns>The language configuration for the current field.</returns>
            public Project.Host.Langs _field_impl_Val(Base_.Transmitter ctx, Base_.Transmitter.Slot slot) => field_impl_enum.Current.Value;
#endregion

            /// <summary>
            /// List of constant packs dedicated to this host.
            /// </summary>
            public List<PackImpl> const_enum_packs = []; // Host-dedicated enum/constants packs

            /// <summary>
            /// Provides access to the list of host-dedicated packs for serialization purposes.
            /// Returns null if the list is empty.
            /// </summary>
            /// <returns>The list of host-dedicated packs or null if empty.</returns>
            public object? _packs() => 0 < const_enum_packs.Count ?
                                           const_enum_packs :
                                           null;

            /// <summary>
            /// Gets the count of host-dedicated packs.
            /// </summary>
            public int _packs_len => const_enum_packs.Count;

            /// <summary>
            ///Indices of local packs (constants/enums) declared directly within this host's scope.
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <param name="item">The index of the host-dedicated pack to retrieve.</param>
            /// <returns>The index of the host-dedicated pack at the specified index.</returns>
            public ushort _packs(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => (ushort)const_enum_packs[item].idx;

            public override    bool Init_As_Modifier_Dispatch_Modifications_On_Targets(HashSet<object> once)                         => false;
            protected override void Init_Collect_Modification(Entity                                   target, HashSet<object> once) { }

            public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once)
            {
                if( entities.TryGetValue(by_what, out var value) && value is PackImpl pack )
                    if( !pack.is_constants_set && !pack.is_enum ) { AdHocAgent.LOG.Information("Only enums or constant sets can be explicitly imported into a Host. Importing pack {pack} (line {line}) into {host} will be ignored.", pack, pack.line_in_src_code, this); }
                    else if( !add ) const_enum_packs.Remove(pack);
                    else if( !const_enum_packs.Contains(pack) ) const_enum_packs.Add(pack);
            }

            /// <summary>
            /// Represents the implementation of a Pack entity within a Host or Project.
            /// Packs can be sub-packs, enums, or constant sets.
            /// </summary>
            public class PackImpl : Entity, Project.Host.Pack{
                /// <summary>
                /// Pack identity for grouping: declared packs compare by symbol, synthesized packs (no symbol) by instance.
                /// </summary>
                public static readonly IEqualityComparer<PackImpl> Identity = new IdentityComparer();

                sealed class IdentityComparer : IEqualityComparer<PackImpl>{
                    public bool Equals(PackImpl? a, PackImpl? b) => ReferenceEquals(a, b) || a != null && b != null && a.symbol != null && b.symbol != null && SymbolEqualityComparer.Default.Equals(a.symbol, b.symbol);
                    public int  GetHashCode(PackImpl p)         => p.symbol == null ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(p) : SymbolEqualityComparer.Default.GetHashCode(p.symbol);
                }

                public ushort _uid        => (ushort)uid;
                public ushort _link       { get; set; } = ushort.MaxValue;
                public long   _stream_max { get; set; }

                /// <summary>
                /// Iterates through packs within the current pack's scope, applying an action to transmittable packs.
                /// If depth is 0, this pack itself is processed if it's transmittable.
                /// If depth > 0, recursively includes all nested transmittable packs.
                /// </summary>
                /// <param name="depth">The depth of scope to traverse (0 for this pack only, >0 for nested packs).</param>
                /// <param name="dst">The action to apply to each transmittable pack.</param>
                /// <param name="pack_set">The symbol of the named pack set, if this operation is part of processing a set. Can be null.</param>
                public void for_packs_in_scope(uint depth, Action<PackImpl> dst)
                {
                    if( depth == 0 )
                    {
                        if( is_transmittable ) dst(this); // A shallow search includes only this pack if it's transmittable.
                        return;
                    }

                    // A recursive search explicitly EXCLUDES this container pack, processing only its nested children.
                    foreach( var entity in entities.Values.Where(e => e.parent_by_source_code == this) )
                        if( entity is PackImpl pack )
                        {
                            if( pack.is_transmittable ) dst(pack);
                            pack.for_packs_in_scope(depth - 1, dst); // ...then RECURSIVELY includes all its descendant packs.
                        }
                }

                public bool is_TimeSpanDef => symbol?.Interfaces.Any(i => equals(i.OriginalDefinition, Meta_TimeSpanDef)) ?? false;
                public bool is_DateTimeDef => symbol?.Interfaces.Any(i => equals(i.OriginalDefinition, Meta_DateTimeDef)) ?? false;
                public bool is_Duration    => symbol?.Interfaces.Any(i => equals(i.OriginalDefinition, Meta_Duration))    ?? false;


                ///<summary>True when the pack is a named, chunked Stream — declared as <c>class X : Stream {}</c>.
                ///Returns false for packs that were flattened to a primitive value pack — see <see cref="_stream_demoted"/>.</summary>
                public bool is_Stream => (symbol?.Interfaces.Any(i => equals(i, Meta_Stream_0)) ?? false);

                ///<summary>True when the pack is a named, length-prefixed File — declared as <c>class X : File {}</c>.
                ///Returns false for packs that were flattened to a primitive value pack — see <see cref="_stream_demoted"/>.</summary>
                public bool is_File => (symbol?.Interfaces.Any(i => equals(i, Meta_File_0)) ?? false);

                public ushort _id                         { get; set; } = (int)Project.Host.Pack.Field.DataType.t_subpack; //pack id
                public bool   is_explicitly_transmittable => _id < (int)Project.Host.Pack.Field.DataType.t_subpack;

                /// <summary>
                /// Maximum nesting depth of pack types within this pack's fields.
                /// </summary>
                public byte _nested_max { get; set; }

                /// <summary>
                /// Indicates whether this pack is referred to by any field.
                /// </summary>
                public bool _referred { get; set; }

                /// <summary>
                /// List of fields defined within this pack.
                /// </summary>
                public List<FieldImpl> fields = [];

                /// <summary>
                /// Provides access to the list of fields for serialization purposes.
                /// Returns null if the list is empty.
                /// </summary>
                /// <returns>The list of fields or null if empty.</returns>
                public object? _fields() => 0 < fields.Count ?
                                                fields :
                                                null;

                /// <summary>
                /// Gets the count of fields in this pack.
                /// </summary>
                public int _fields_len => fields.Count;

                /// <summary>
                /// Retrieves the index of a specific field at the given index.
                /// </summary>
                /// <param name="ctx">The transmitter context (not used here).</param>
                /// <param name="slot">The transmitter slot (not used here).</param>
                /// <param name="item">The index of the field to retrieve.</param>
                /// <returns>The index of the field at the specified index.</returns>
                public int _fields(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => fields[item].idx;

                /// <summary>
                /// The data alone fits in 64 bits — without the bits nullable fields may add. Such a pack MAY be a Value
                /// pack; see <see cref="IsValuePack"/> for the answer that holds for certain.
                /// </summary>
                public bool MayByValuePack() => estimate_bits(false, []) < 65;

                /// <summary>
                /// Certainly a Value pack: numeric fields and Value packs only, and fewer than 65 bits even when every
                /// field is counted at its DECLARED width and every nullable field adds a bit. Both only overestimate —
                /// a `[MinMax]` narrows a field, a null value may fit an unused one — so a pack this says yes to is
                /// flattened by the generator in every language. The reverse does not hold: a pack narrowed below 65 bits
                /// only by its `[MinMax]` attributes is a Value pack this says no to.
                /// </summary>
                public bool IsValuePack() => estimate_bits(true, []) < 65;

                /// <summary>
                /// An upper bound of the pack's serialized size in bytes, or -1: there is none - a Stream, a File, a pack that
                /// contains itself, a field that is a conduit or has a chain of its own. Every field is counted at its declared
                /// width and with room for its null and its varint length, a string at three bytes a char, a collection at its
                /// declared or default maximal length: it only overestimates. It is what takes compression off a pack that
                /// cannot grow to the size where compression pays (see <see cref="HasDocs.collect_stream_stages"/>).
                /// <paramref name="path"/> holds the packs being expanded above this one.
                /// </summary>
                internal long upper_bound_bytes(HashSet<PackImpl> path)
                {
                    if( is_Stream || is_File || !path.Add(this) ) return -1;
                    try
                    {
                        long size = 0;
                        foreach( var fld in fields )
                        {
                            var bytes = fld.upper_bound_bytes(path);
                            if( bytes < 0 ) return -1;
                            if( int.MaxValue < (size += bytes) ) return int.MaxValue; // far past any length worth asking about
                        }

                        return size;
                    }
                    finally
                    {
                        path.Remove(this);
                    }
                }

                /// <summary>
                /// An upper bound of the pack's size in bits; 100 for anything that cannot be part of a Value pack.
                /// <paramref name="path"/> holds the packs being expanded above this one: a pack met again on its own
                /// path contains itself and is never a Value pack. A pack met again BESIDE itself — two fields of one
                /// type — counts twice, as it does on the wire.
                /// </summary>
                long estimate_bits(bool with_nulls, HashSet<PackImpl> path)
                {
                    if( !path.Add(this) ) return 100;
                    try
                    {
                        long size = 0;
                        foreach( var fld in fields )
                        {
                            if( fld.is_Stream || fld.is_Map || fld.is_Set || fld._exT_array != null || fld._map_set_array != null ) return 100;

                            if( fld.get_exT_pack is PackImpl subPack )
                                size += subPack.is_enum ?
                                            fld.inT.HasValue ?
                                                type_bit_size(fld.inT.Value) :
                                                0 :
                                            subPack.estimate_bits(with_nulls, path);
                            else if( fld.exT_primitive.HasValue ) size += type_bit_size(fld.exT_primitive.Value);

                            if( with_nulls && fld.nullable ) size++;
                            if( 64 < size ) return size;
                        }

                        return size;
                    }
                    finally
                    {
                        path.Remove(this);
                    }
                }

                /// <summary>
                /// Helper function to map a DataType enum value to its bit size.
                /// </summary>
                static int type_bit_size(int primitiveType) => (Project.Host.Pack.Field.DataType)primitiveType switch
                                                               {
                                                                   Project.Host.Pack.Field.DataType.t_bool => 1,
                                                                   Project.Host.Pack.Field.DataType.t_int8 or
                                                                       Project.Host.Pack.Field.DataType.t_uint8 => 8,
                                                                   Project.Host.Pack.Field.DataType.t_int16 or
                                                                       Project.Host.Pack.Field.DataType.t_uint16 or
                                                                       Project.Host.Pack.Field.DataType.t_char => 16,
                                                                   Project.Host.Pack.Field.DataType.t_int32 or
                                                                       Project.Host.Pack.Field.DataType.t_uint32 or
                                                                       Project.Host.Pack.Field.DataType.t_float => 32,
                                                                   Project.Host.Pack.Field.DataType.t_int64 or
                                                                       Project.Host.Pack.Field.DataType.t_uint64 or
                                                                       Project.Host.Pack.Field.DataType.t_double or
                                                                       Project.Host.Pack.Field.DataType.t_date => 64,
                                                                   _ => 100
                                                               };

                /// <summary>
                /// Gets the EnumDeclarationSyntax node associated with this pack, if it's an enum.
                /// Null if it's not an enum.
                /// </summary>
                EnumDeclarationSyntax? enum_node;

                /// <summary>
                /// Indicates whether the underlying enum type should be calculated automatically (default int)
                /// or if it was explicitly specified by the user.
                /// </summary>
                public bool is_calculate_enum_type => enum_node!.BaseList == null; //user does not explicitly assign enum type (int by default)

                /// <summary>
                /// Indicates whether this pack is an enum.
                /// </summary>
                public bool is_enum => enum_node != null;

                /// <summary>
                /// Indicates whether this pack is a constant set.
                /// </summary>
                public bool is_constants_set => _id == (int)Project.Host.Pack.Field.DataType.t_constants;

                /// <summary>
                /// Indicates whether this pack is a typedef.
                /// </summary>
                public bool is_typedef => fields.Count == 1 && fields is [{ _name: "TYPEDEF" }];

                /// <summary>
                /// Indicates whether this pack is transmittable (i.e., not an enum, constant set, or typedef).
                /// </summary>
                public bool is_transmittable => !is_enum
                                                && !is_constants_set
                                                && !is_typedef
                                                && !is_FieldsInjectInto
                                                && !is_Header
                                                && !is_Modifier
                                                && !is_Duration
                                                && !is_DateTimeDef
                                                && !is_TimeSpanDef;

                public HashSet<PackImpl>? target_packs; // is_FieldsInjectInto || is_HeaderFor collect packs

                public bool is_Header => _id == (int)Project.Host.Pack.Field.DataType.t_char;
                public bool is_HeaderModifier;
                public bool is_FieldsInjectInto;

#region Enum pack constructor
                /// <summary>
                /// Initializes a new instance of the <see cref="PackImpl"/> class for enum and attribute.
                /// </summary>
                /// <param name="project">The project this pack belongs to.</param>
                /// <param name="compilation">The Roslyn compilation object.</param>
                /// <param name="ENUM">The EnumDeclarationSyntax node representing the enum.</param>
                //+ used as Attribute data holder
                public PackImpl(ProjectImpl project, CSharpCompilation? compilation = null, EnumDeclarationSyntax? ENUM = null) : base(project, compilation, ENUM)
                {
                    if( (enum_node = ENUM) == null ) return; //attributes short path

                    _id = (ushort)(symbol.GetAttributes().Any(a => a.AttributeClass!.ToString()!.Equals("System.FlagsAttribute")) ? //enum type
                                       Project.Host.Pack.Field.DataType.t_enum_flags :
                                       Project.Host.Pack.Field.DataType.t_enum_sw); //probably need to check

                    project.constants_packs.Add(this);   //enums register in the project scope
                    in_host?.const_enum_packs.Add(this); //register enum on host level scope. else stays in the project scope
                }
#endregion

#region Struct-based constant set pack constructor
                /// <summary>
                /// Initializes a new instance of the <see cref="PackImpl"/> class for struct-based constant sets.
                /// </summary>
                /// <param name="project">The project this pack belongs to.</param>
                /// <param name="compilation">The Roslyn compilation object.</param>
                /// <param name="constants_set">The StructDeclarationSyntax node representing the constant set.</param>
                public PackImpl(ProjectImpl project, CSharpCompilation compilation, StructDeclarationSyntax constants_set) : base(project, compilation, constants_set)
                {
                    _id = (ushort)Project.Host.Pack.Field.DataType.t_constants; //constants set
                    project.constants_packs.Add(this);                          // register constants set
                    in_host?.const_enum_packs.Add(this);                        //register on host level scope. else stays in the project scope
                }
#endregion

#region Interface-based constant set pack constructor
                /// <summary>
                /// Initializes a new instance of the <see cref="PackImpl"/> class for struct-based constant sets.
                /// </summary>
                /// <param name="project">The project this pack belongs to.</param>
                /// <param name="compilation">The Roslyn compilation object.</param>
                /// <param name="constants_set">The StructDeclarationSyntax node representing the constant set.</param>
                public PackImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax constants_set) : base(project, compilation, constants_set)
                {
                    _id = (ushort)Project.Host.Pack.Field.DataType.t_constants; //constants set
                    project.constants_packs.Add(this);                          // register constants set
                    in_host?.const_enum_packs.Add(this);                        //register on host level scope. else stays in the project scope
                }
#endregion

                /// <summary>
                /// Indicates whether this pack is used as an attribute.
                /// </summary>
                public bool is_attribute => symbol?.BaseType?.Name == "Attribute";

#region Class-based pack constructor
                /// <summary>
                /// Initializes a new instance of the <see cref="PackImpl"/> class for class-based packs.
                /// </summary>
                /// <param name="project">The project this pack belongs to.</param>
                /// <param name="compilation">The Roslyn compilation object.</param>
                /// <param name="pack">The ClassDeclarationSyntax node representing the pack.</param>
                public PackImpl(ProjectImpl project, CSharpCompilation compilation, ClassDeclarationSyntax pack) : base(project, compilation, pack)
                {
                    _id = (int)Project.Host.Pack.Field.DataType.t_subpack; //by default subpack type

                    if( is_attribute ) return;

                    if( symbol.Interfaces.Any(i => equals(i.OriginalDefinition, Meta_DateTimeDef) ||
                                                   equals(i.OriginalDefinition, Meta_TimeSpanDef) ||
                                                   equals(i.OriginalDefinition, Meta_Duration)) )
                    {
                        _id = (ushort)Project.Host.Pack.Field.DataType.t_bool; //mark DateTime attribute
                        project.constants_packs.Add(this);
                        return;
                    }

                    is_FieldsInjectInto = symbol.Interfaces.Any(i => equals(i.ConstructedFrom, Meta_FieldsInjectInto));

                    if( is_FieldsInjectInto ) //The template class itself is not preserved as a packet; only its fields are distributed.
                    {
                        project.injector_packs.Add(this);
                        target_packs = [];
                        return;
                    }

                    if( is_HeaderModifier = symbol.Interfaces.Any(i =>
                                                                      equals(i.ConstructedFrom, Meta_Modify_Target) &&
                                                                      i.TypeArguments.Length == 1                   &&
                                                                      i.TypeArguments[0].Interfaces.Any(i => equals(i.ConstructedFrom, Meta_HeaderFor))
                                                                 ) )
                    {
                        project.header_modifiers.Add(this);
                        return;
                    }

                    if( symbol.Interfaces.Any(i => equals(i.ConstructedFrom, Meta_HeaderFor)) )
                    {
                        _id = (ushort)Project.Host.Pack.Field.DataType.t_char; //mark as Header
                        project.header_packs.Add(this);
                        target_packs = [];
                    }

                    // Named Stream/File packs: `[S(N)] class X : Stream {}` / `: File {}`.
                    // The [S(N)] attribute is the max payload size enforced by the runtime;
                    // it is mandatory because every stream-bearing entity needs an explicit cap
                    // (same rule as stream-typed fields — see SAttribute handling in FieldImpl.init).
                    //
                    // Sign convention on `_stream_max` (read by consumers of the wire metadata):
                    //   _stream_max  < 0  → Stream kind (chunked, interruptible)        → store as -N
                    //   _stream_max  > 0  → File   kind (single length-prefix)          → store as +N
                    //   _stream_max == 0  → neither (a normal pack)
                    if( is_Stream || is_File )
                    {
                        if( is_Stream && is_File )
                            AdHocAgent.exit($"Pack `{symbol}` (line: {line_in_src_code}) inherits from BOTH `Stream` and `File`. Pick one — they describe different wire formats (chunked vs. single length-prefix).", 88);

                        // Stream/File packs are pure framing wrappers: `[S(N)]` (plus an optional transform chain) is
                        // their entire wire contribution, so they carry NO instance fields. Constants and static
                        // fields are fine — they ride along as pack metadata, never as wire payload.
                        // A field that uses such a pack as its datatype is reinterpreted as a bare Stream/File
                        // conduit (see the "Reinterpret named Stream/File typed fields" pass in ProjectImpl.init),
                        // which is why an instance field here could never reach the wire.
                        var instance_fields = pack.Members.OfType<FieldDeclarationSyntax>()
                                                  .Where(f => !f.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.ConstKeyword)))
                                                  .SelectMany(f => f.Declaration.Variables.Select(v => $"`{v.Identifier}` (line: {v.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1})"))
                                                  .ToArray();
                        if( instance_fields.Length != 0 )
                            AdHocAgent.exit($"Stream/File pack `{symbol}` (line: {line_in_src_code}) must have no instance fields, but declares {string.Join(", ", instance_fields)}. "  +
                                            "A Stream/File pack carries framing only — a field of this type is parsed exactly like a bare `[S(N)] Stream`/`File` field and never "       +
                                            "becomes a sub-pack, so an instance field here has nowhere to go. Delete them (constants and static fields are fine), or move them to an " +
                                            "ordinary pack.", 88);

                        var sAttr = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "SAttribute");
                        if( sAttr == null )
                            AdHocAgent.exit($"Pack `{symbol}` (line: {line_in_src_code}) inherits from `{(is_Stream ? "Stream" : "File")}` and requires the `[S(N)]` attribute defining the maximum size in bytes.", 88);

                        if( sAttr.ConstructorArguments.Length != 1 )
                            AdHocAgent.exit($"The `[S(N)]` attribute on pack `{symbol}` (line: {line_in_src_code}) must have exactly one argument.", 88);

                        long n;
                        try { n = Convert.ToInt64(sAttr.ConstructorArguments[0].Value); }
                        catch( Exception )
                        {
                            AdHocAgent.exit($"The `[S(N)]` attribute argument on pack `{symbol}` (line: {line_in_src_code}) must be an integer compile-time constant.", 88);
                            return;
                        }

                        // The cap must exceed 8 bytes: a payload that fits in 8 bytes or fewer is no larger than a single
                        // 64-bit primitive, so it belongs in a fixed primitive or a Binary collection (no chunk/length
                        // framing overhead), not in a Stream/File conduit.
                        if( n <= 8 )
                            AdHocAgent.exit($"The `[S(N)]` attribute on pack `{symbol}` (line: {line_in_src_code}) must specify a maximum size greater than 8 bytes, but got {n}. A payload that small is no larger than a single 64-bit primitive — use a primitive or a Binary collection instead of a Stream/File pack.", 88);

                        // Encode the kind in the sign of _stream_max (see comment above).
                        _stream_max = is_Stream ?
                                          -n :
                                          n;
                    }


                    project.all_packs.Add(this);
                }
#endregion

#region Proxy pack constructor
                /// <summary>
                /// Initializes a new instance of the <see cref="PackImpl"/> class as a proxy for a project.
                /// </summary>
                /// <param name="project">The project to create a proxy for.</param>
                public PackImpl(Entity project) : base(project.project, null, null)
                {
                    uid = (ulong)(0xFFFF - projects.Count); //temporary in a special distinct range. look at imported_projects_uid

                    _name       = project._name;
                    _doc        = project._doc;
                    _inline_doc = project._inline_doc;
                    symbol      = project.symbol;
                    _constants_ = project._constants_;

                    _id = (ushort)Project.Host.Pack.Field.DataType.t_constants; //constants set
                    root_project.constants_packs.Add(this);                     // register constants set
                    in_host?.const_enum_packs.Add(this);                        //register on host level scope. else stays in the project scope
                }
#endregion

#region Synthesized pack constructor
                /// <summary>
                /// Initializes a new instance of the <see cref="PackImpl"/> class for synthesized proxy packs like '_Void'.
                /// </summary>
                /// <param name="project">The project this pack belongs to.</param>
                /// <param name="synthesizedName">The programmatic name for the pack.</param>
                internal PackImpl(ProjectImpl project, string synthesizedName) : base(project, null, null)
                {
                    _name     = synthesizedName;
                    _id       = (ushort)Project.Host.Pack.Field.DataType.t_subpack;
                    _included = true;
                    uid       = 0;
                    project.all_packs.Add(this);
                }
#endregion


                /// <summary>
                /// Recursively crawls the field hierarchy of a pack this side sends, collecting what the RECEIVING
                /// side parses out of it. A ToStream cut prunes the walk — the receiver holds opaque bytes there;
                /// a FromStream cut does not — the receiver rehydrates that payload into a typed pack.
                /// </summary>
                /// <param name="dst">The related packs: everything the receiving side materializes.</param>
                /// <param name="ToStream">Payloads only the sender materializes; the receiver sees opaque bytes.</param>
                /// <param name="FromStream">Payloads the sender does not materialize — a subset of <paramref name="dst"/>.</param>
                /// <param name="connection">The connection whose leg the cuts are resolved against.</param>
                /// <param name="isR">True when the RIGHT host is the sender on this crawl.</param>
                /// <param name="once">Recursion guard to prevent infinite loops in cyclic type graphs.</param>
                /// <summary>
                /// Walks the inheritance chain upward. Every base class that resolves to a registered
                /// <see cref="PackImpl"/> must travel with this pack so the generated `class Show_Code
                /// extends Item` references have the base type available in each target host.
                /// </summary>
                void collect_base_packs(Action<PackImpl> add)
                {
                    for( var bt = symbol?.BaseType; bt != null; bt = bt.BaseType )
                        if( entities.TryGetValue(bt.OriginalDefinition, out var ent) && ent is PackImpl basePack )
                            add(basePack);
                }

                internal void collect_inderectly_transmitting_packs(ISet<PackImpl> dst, ISet<PackImpl> ToStream, ISet<PackImpl> FromStream, ConnectionImpl connection, bool isR, ISet<object> once)
                {
                    if( !once.Add(this) ) return;

                    // Base classes ride along with every derived pack. Their own fields / bases are then
                    // recursively walked (base may also have field types that need to come with us).
                    collect_base_packs(bp =>
                                       {
                                           if( dst.Add(bp) ) bp.collect_inderectly_transmitting_packs(dst, ToStream, FromStream, connection, isR, once);
                                       });
                    foreach( var fld in fields )
                    {
                        // `dst` collects what the RECEIVING side materializes: this walk answers "which packs does the
                        // other end parse out of what this side sends". Asked of the field's own record of its cuts,
                        // written when its chain was materialized.
                        var (cut_to, cut_from) = fld.cut_on(connection, isR);

                        // Cut so that the SENDER hands over raw bytes: the receiver still parses the payload into a
                        // typed pack, so it belongs in `dst` and the walk carries on into its own fields. `FromStream`
                        // records it besides, so the connection-level list can tell the sender to emit no serializer.
                        if( cut_from && fld.exT_pack != null ) FromStream.Add((PackImpl)entities[fld.exT_pack]); // a `string` payload carries no pack schema

                        // Cut the other way: the sender serializes, the receiver holds opaque bytes and materializes
                        // nothing — the payload is none of the receiver's business, so the walk stops here.
                        if( cut_to )
                        {
                            if( fld.exT_pack != null ) ToStream.Add((PackImpl)entities[fld.exT_pack]); // a `string` payload carries no pack schema
                            continue;
                        }

                        // The receiver parses this field: its type joins `dst` and the walk descends into it.
                        // Enums / constant sets are never transmitted: they travel to the hosts that need them
                        // through `const_enum_packs` (see the `host_used_enums` pass). Adding one here would list
                        // it among the actor's related packs and the generator would emit it as a data pack.
                        // (Other non-transmittable field types — TimeSpanDef / DateTimeDef / typedef shells such
                        // as `SecondsADay` — MUST stay: the host emits their type from this very list.)
                        var ext = fld.get_exT();
                        if( ext != null && !(ext.is_enum || ext.is_constants_set) && dst.Add(ext) ) ext.collect_inderectly_transmitting_packs(dst, ToStream, FromStream, connection, isR, once);

                        var vExt = fld.V?.get_exT();
                        if( vExt != null && !(vExt.is_enum || vExt.is_constants_set) && dst.Add(vExt) ) vExt.collect_inderectly_transmitting_packs(dst, ToStream, FromStream, connection, isR, once);
                    }

                    once.Remove(this);
                }

                internal void collect_inderectly_transmitting_packs(ISet<PackImpl> dst, ConnectionImpl connection, bool isR, ISet<object> once)
                {
                    if( !once.Add(this) ) return;

                    collect_base_packs(bp =>
                                       {
                                           if( dst.Add(bp) ) bp.collect_inderectly_transmitting_packs(dst, connection, isR, once);
                                       });

                    foreach( var fld in fields )
                    {
                        // NOTE: this overload asks only about the LEFT-sender leg, whatever `isR` says — a literal
                        // translation of the former `Array.BinarySearch(connection.FromStream, fld.idx)`, whose index was
                        // taken unsigned (`fld.idx` is never negative, so the `~` branch was dead) and therefore never
                        // matched a Right-sender entry. Kept as-is: this walk only decides how deep to recurse.
                        if( fld.cut_on(connection, false).from ) continue;

                        // Handle standard symmetric fields (enums / constant sets are not transmitted — see above)
                        var ext = fld.get_exT();
                        if( ext != null && !(ext.is_enum || ext.is_constants_set) && dst.Add(ext) ) ext.collect_inderectly_transmitting_packs(dst, connection, isR, once);

                        var vExt = fld.V?.get_exT();
                        if( vExt != null && !(vExt.is_enum || vExt.is_constants_set) && dst.Add(vExt) ) vExt.collect_inderectly_transmitting_packs(dst, connection, isR, once);
                    }

                    once.Remove(this);
                }

                /// <summary>
                /// Initializes static data and processes enums and default collection lengths for all packs.
                /// </summary>
                /// <param name="root_project">The root project instance.</param>
                internal static void init(ProjectImpl root_project)
                {
                    var all_fields = ProjectImpl.all_fields().ToList();


#region all_default_collection_capacity read, delete, and apply operations.
                    var all_default_collection_capacity = root_project.constants_packs.Where(en => en._name.Equals("_DefaultMaxLengthOf")).ToArray();

                    foreach( var pack in all_default_collection_capacity.OrderBy(en => en.in_project == root_project) ) //The root project settings should be placed last in order to override all inherited project settings
                        pack._constants_.ForEach(fld =>
                                                 {
                                                     switch( fld._name )
                                                     {
                                                         case "Strings":
                                                             FieldImpl._DefaultMaxLengthOf.Strings = (int)fld._value_int!;
                                                             break;
                                                         case "Arrays":
                                                             FieldImpl._DefaultMaxLengthOf.Arrays = (int)fld._value_int!;
                                                             break;
                                                         case "Maps":
                                                             FieldImpl._DefaultMaxLengthOf.Maps = (int)fld._value_int!;
                                                             break;
                                                         case "Sets":
                                                             FieldImpl._DefaultMaxLengthOf.Sets = (int)fld._value_int!;
                                                             break;
                                                         case "Uncompressed":
                                                             FieldImpl._DefaultMaxLengthOf.Uncompressed = (int)fld._value_int!;
                                                             break;
                                                     }
                                                 });

                    foreach( var en in all_default_collection_capacity ) // remove _DefaultMaxLengthOf from enums
                    {
                        en._constants_.ForEach(fld => raw_fields.Remove(fld.symbol!));
                        root_project.constants_packs.Remove(en);
                    }

                    //apply acquired default length settings
                    foreach( var fld in all_fields )
                    {
                        if( fld!.is_Map ) fld._map_set_len     ??= (uint?)FieldImpl._DefaultMaxLengthOf.Maps;
                        else if( fld.is_Set ) fld._map_set_len ??= (uint?)FieldImpl._DefaultMaxLengthOf.Sets;

                        if( fld.is_String ) fld._max_value ??= FieldImpl._DefaultMaxLengthOf.Strings;
                        if( fld._map_set_array < 8 ) //no length part specified
                            fld._map_set_array = (uint)FieldImpl._DefaultMaxLengthOf.Arrays << 3 | fld._map_set_array!.Value;
                        if( fld._exT_array < 8 ) //no length part specified
                            fld._exT_array = (uint)FieldImpl._DefaultMaxLengthOf.Arrays << 3 | fld._exT_array!.Value;
                    }
#endregion


#region process enums
                    foreach( var enum_ in root_project.constants_packs.Where(e => e.is_enum) )
                    {
                        if( !enum_.included && enum_.in_project.included ) enum_._included = true;

                        if( enum_._constants_.Count < 2 )
                            AdHocAgent.exit($"Enum {enum_.symbol} line:{enum_.line_in_src_code} has only one field. This is redundant. Delete it or add more fields.");


                        switch( enum_._id ) //auto-numbering
                        {
                            case (int)Project.Host.Pack.Field.DataType.t_enum_flags:
                                // For flag enums, calculate max by combining all flags using bitwise OR
                                var M = enum_._constants_.Where(fld => fld._value_int != null).Aggregate(0L, (i, fld) => i | fld._value_int!.Value);

                                var s = 1L;

                                foreach( var fld in enum_._constants_.Where(fld => fld._value_int == null) )
                                {
                                    while( (M | s) == M ) s <<= 1;
                                    fld._value_int =   s;
                                    s              <<= 1;
                                }

                                break;
                            case (int)Project.Host.Pack.Field.DataType.t_enum_sw:
                                // Find enum fields that have non-null integer values and sort by their value
                                var has_value = enum_._constants_.Where(f => f._value_int != null).OrderBy(f => f._value_int).ToArray();

                                var i = 0L;

                                if( has_value.Length != 0 ) // If there are fields with values
                                {
                                    // Check if this might be a flags enum that's missing the [Flags] attribute.
                                    // A flags enum is one whose non-zero values are all distinct bits: every non-zero value a power of two,
                                    // at least three of them (0, 1, 2 and 1, 2, 4 differ only from the third value on), none negative
                                    // (a negative value is a sentinel, not a bit). Zero is allowed: it is the empty set, "NONE".
                                    // A small counting enum (0, 1, 2 / 1, 2, 3 / 0, 2, 4) is therefore not reported.
                                    if( 1 < has_value.Length )
                                    {
                                        var values  = has_value.Select(fld => fld._value_int!.Value).ToArray();
                                        var nonzero = values.Where(val => val != 0).ToArray();

                                        if( values.All(val => 0 <= val) &&
                                            3 <= nonzero.Length &&
                                            nonzero.All(val => (val & val - 1) == 0) )
                                            AdHocAgent.LOG.Information("The`{Enum}` enum appears to be a flags enum. The {Flags} attribute may be missing. {correct} ?", enum_._name, "[Flags]", "\n[Flags]\nenum " + enum_._name + "{...}");
                                    }

                                    // Get the minimum value and set the initial counter
                                    var mIn = i = (long)has_value.First()._value_int!;

                                    // If not all static fields have values
                                    if( has_value.Length < enum_._constants_.Count )
                                    {
                                        var z = 0;
                                        // Fill in missing values between existing values
                                        foreach( var fld in has_value.Skip(1).Where(fld => ++i < fld._value_int) )
                                        {
                                            while( i < fld._value_int )
                                            {
                                                // Find the next field with a null value
                                                z = enum_._constants_.FindIndex(z, f => f._value_int == null);
                                                if( z == -1 ) goto done; // Exit if no more null fields

                                                // Assign sequential values to null fields
                                                enum_._constants_[z++]._value_int = i++;
                                            }
                                        }

                                        // Fill in with values below the minimum value
                                        while( 0 < mIn )
                                        {
                                            var fs = enum_._constants_[z++];
                                            fs._value_int ??= --mIn;
                                            if( z == enum_._constants_.Count ) goto done;
                                        }

                                        // Fill in remaining null fields with increasing values
                                        while( z < enum_._constants_.Count )
                                        {
                                            var fs = enum_._constants_[z++];
                                            fs._value_int ??= ++i;
                                        }
                                    }

                                    done:

                                    // Verify that the values are sequential
                                    if( enum_._constants_.OrderBy(f => f._value_int).Skip(1).Any(fld => ++mIn < fld._value_int) ) goto next;
                                }
                                else // If no fields have values, assign sequential values to all fields
                                    enum_._constants_.ForEach(fld => fld._value_int = i++);

                                // Mark this enum as having values that can be represented with an expression
                                enum_._id = (int)Project.Host.Pack.Field.DataType.t_enum_exp;

                                break;
                        }

                        next:

                        // Calculate minimum value for enum fields
                        // For enums without fields (boolean-like), minimum is 0
                        var min = enum_._constants_.Count == 0 ?
                                      0 :
                                      enum_._constants_.Min(f => f._value_int)!.Value;
                        // Calculate maximum value for enum fields
                        // For enums without fields (boolean-like), maximum is 0
                        var max = enum_._constants_.Count == 0 ?
                                      0 :
                                      enum_._constants_.Max(f => f._value_int)!.Value;

                        // For flag enums, calculate max by combining all flags using bitwise OR
                        if( enum_._id == (int)Project.Host.Pack.Field.DataType.t_enum_flags )             // the enum is flag
                            max = enum_._constants_.Aggregate(0L, (i, fld) => i | fld._value_int!.Value); //set all flags


                        var this_enum_used_fields = all_fields.Where(fld => equals(enum_.symbol, fld?.exT_pack)).ToArray(); // all fields that use this enum type


                        if( enum_.is_calculate_enum_type )                  // If the user does not explicitly specify enum type (defaults to int),
                            enum_._constants_[0].set_exT_ByRange(min, max); // calculate the most efficient numeric type based on the enum's value range


#region Propagate enum parameters to the fields where they are used.
                        // Convert enum to boolean if it has no practical use as enum
                        // This happens when: no fields, single field, or all fields have same value
                        if( max == min && 0 < this_enum_used_fields.Length )
                        {
                            enum_._id = (ushort)Project.Host.Pack.Field.DataType.t_subpack; //mark on delete
                            var problem = enum_._constants_.Count == 0 ?
                                              " no field" :
                                              enum_._constants_.Count == 1 ?
                                                  " only one field" :
                                                  " fields with same values";

                            AdHocAgent.LOG.Warning("Enum {EnumSymbol} has {Problem}. As field data type it\'s useless and will be replaced with boolean", enum_.symbol, problem);

                            foreach( var fld in this_enum_used_fields )
                                fld.switch_to_boolean();

                            continue;
                        }

                        //rest of fields
                        foreach( var fld in this_enum_used_fields ) //=================================================== propagate
                        {
                            // Set internal type range based on enum kind
                            if( enum_._id == (int)Project.Host.Pack.Field.DataType.t_enum_sw )
                                fld.set_inT_ByRange(0, enum_._constants_.Count);
                            else
                                fld.set_inT_ByRange(min, max);

                            fld._min_value = min; //acceptable range
                            fld._max_value = max; //acceptable range
                        }
#endregion
                    }
#endregion


                    root_project.constants_packs.RemoveAll(enum_ => enum_._id == (ushort)Project.Host.Pack.Field.DataType.t_subpack); // remove marked to delete enums
                }

                /// <summary>
                /// Cyclic dependency depth for type resolution.
                /// </summary>
                int cyclic_depth;

                /// <summary>
                /// Calculates the nesting depth of pack types within this pack's fields to detect cyclic dependencies.
                /// </summary>
                /// <param name="path">Set to track visited packs in the current path, used to detect cycles.</param>
                /// <returns>The maximum nesting depth detected in the current path, or 0 if no new depth is found.</returns>
                internal int calculate_fields_type_depth(ISet<object> path)
                {
                    if( path.Count == 0 ) cyclic_depth = 0;

                    try
                    {
                        foreach( var datatype in fields.Where(f => f.exT_pack != null).Select(f => (PackImpl)entities[f.exT_pack!])
                                                       .Concat(fields.Where(f => f.is_Map && f.V.exT_pack != null).Select(f => (PackImpl)entities[f.V!.exT_pack!])).Distinct() )
                        {
                            if( !path.Add(datatype) )
                            {
                                cyclic_depth = Math.Max(cyclic_depth, path.Count);
                                continue;
                            }

                            datatype.calculate_fields_type_depth(path);
                            path.Remove(datatype);
                        }
                    }
                    catch( Exception )
                    {
                        foreach( var fld in fields.Where(f => f.exT_pack != null && !entities.ContainsKey(f.exT_pack!))
                                                  .Concat(fields.Where(f => f.V != null && f.V.exT_pack != null && !entities.ContainsKey(f.V.exT_pack!))) )
                            AdHocAgent.LOG.Error("Line {line}: Unsupported field type {type} detected for field {field}.", fld.line_in_src_code,
                                                 fld.fld_node.Declaration.Type, fld);
                        AdHocAgent.exit("", 23);
                    }

                    return path.Count == 0 ?
                               cyclic_depth :
                               0;
                }


                public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once)
                {
                    // `Stream` and `File` are framing-only marker interfaces. When a pack inherits
                    // them — `class X : Stream {}` / `: File {}` — the BaseList walker treats them
                    // like any other interface and tries to dispatch a modification, which is wrong:
                    // there's nothing to import from a marker. Swallow the call here so named
                    // Stream/File packs don't trip the "Unexpected attempt to apply add modification"
                    // branch below. The pack's Stream/File semantics are picked up by the
                    // `is_Stream` / `is_File` properties and the `[S(N)]` handling in the constructor.
                    if( equals(by_what, Meta_Stream_0) || equals(by_what, Meta_File_0) ) return;

                    // Stage/flow attribute base classes (e.g. `class Lz4 : StreamCompressionStageAttribute`) are
                    // attribute DEFINITIONS, not pack modifications — swallow like the Stream/File markers above.
                    if( by_what is INamedTypeSymbol _bw && (ProjectImpl.is_stream_stage(_bw) || ProjectImpl.is_stream_flow(_bw)) ) return;

                    // A user class deriving from `StreamTrimAttribute` reaches the walker the same way — but unlike a
                    // stage or a flow it can never be used: `collect_stream_stages` only recognizes the three built-in
                    // markers. Say so here, where the declaration is, instead of letting it fall through to the generic
                    // "Unexpected attempt to apply add modification" below (or, if never applied, pass unnoticed).
                    if( by_what is INamedTypeSymbol _bt && ProjectImpl.is_stream_trim(_bt) )
                        AdHocAgent.exit($"`{symbol}` (line: {line_in_src_code}) derives from `{_bt.Name}`, but custom trim markers are not supported. " +
                                        "Use the built-in `ToStream<E>`, `FromStream<E>` or `Stream<To, From>` markers — a trim carries no parameters and no implementation, so there is nothing for a subclass to add.", 2);

                    if( target_packs != null ) //is_FieldsInjectInto || is_HeaderFor collect packs
                    {
                        void apply_(PackImpl pack, bool add) //modify collection of target packs to which FieldsInjectInto of   Header applyed
                        {
                            if( add ) target_packs.Add(pack);
                            else target_packs.Remove(pack);
                        }

                        ConnectionImpl.NamedPackSet.collect_packs_in_scope(by_what, add, depth, apply_, once);
                    }
                    else if( raw_fields.TryGetValue(by_what, out var by_fld) ) apply(by_fld);
                    else if( raw_static_fields.TryGetValue(by_what, out var by_st_fld) ) apply2(by_st_fld);
                    else if( entities.TryGetValue(by_what, out var value) && value is PackImpl by_pack )
                    {
                        by_pack.Init(once);
                        foreach( var fLd in by_pack.fields ) apply(fLd);
                        foreach( var fLd in by_pack._constants_ ) apply2(fLd);
                    }
                    else
                        AdHocAgent.exit($"Unexpected attempt to apply {(add ? "add" : "remove")} modification by {by_what} to the pack {symbol} (line: {line_in_src_code}).");


                    void apply(FieldImpl fld)
                    {
                        if( add )
                        {
                            var i = fields.FindIndex(f => f._name == fld._name); //with the same name

                            if( i == -1 ) fields.Add(fld);
                            else if( inited ) fields[i] = fld; //modifier force override existing
                        }
                        else
                            fields.Remove(fld);
                    }

                    void apply2(ConstantImpl fld)
                    {
                        if( add )
                        {
                            var i = _constants_.FindIndex(s => s._name == fld._name); //same name

                            if( -1 < i )
                                if( inited ) _constants_.RemoveAt(i); //modifier force
                                else return;                          //normal init

                            _constants_.Remove(fld);
                        }
                        else
                            _constants_.Remove(fld);
                    }
                }


                public override string ToString() => (symbol?.ToString() ?? _name) +
                                                     _id switch
                                                     {
                                                         (int)Project.Host.Pack.Field.DataType.t_enum_exp   => " : enum expr",
                                                         (int)Project.Host.Pack.Field.DataType.t_enum_sw    => " : enum switch",
                                                         (int)Project.Host.Pack.Field.DataType.t_enum_flags => " : enum flags",
                                                         (int)Project.Host.Pack.Field.DataType.t_constants  => " : constants",
                                                         (int)Project.Host.Pack.Field.DataType.t_subpack    => " : sub pack",
                                                         (int)Project.Host.Pack.Field.DataType.t_char       => " : header",
                                                         < (int)Project.Host.Pack.Field.DataType.t_subpack  => $" : pack {_id} ",
                                                         _                                                  => "???"
                                                     };

                /// <summary>
                /// Represents the implementation of a Constant entity within a Pack.
                /// </summary>
                public class ConstantImpl : HasDocs, Project.Host.Pack.Constant{
                    /// <summary>
                    /// Gets the value of this constant. If a substitute field is defined, its value is returned; otherwise, the constant's own value is returned.
                    /// </summary>
                    /// <returns>The value of the constant, potentially from a substituted field.</returns>
                    public object? value_of() // Get the real value, possibly from a substituted field if defined
                    {
                        // If a substitution value is defined for the field, use the substituted field’s value; otherwise, return the value of the original field

                        var info = field_reflection((substitute_value_from == null ?
                                                         this :
                                                         raw_static_fields[substitute_value_from])
                                                    .symbol!);

                        try // Return the calculated value of the substituted or original field via runtime reflection
                        {
                            return info.GetValue(null);
                        }
                        catch( Exception ) { return info.GetRawConstantValue(); }
                    }


                    public ushort _exT { get; set; }

                    /// <summary>
                    /// Integer value of the constant, used for integer, boolean, char and enum types.
                    /// </summary>
                    public long? _value_int { get; set; } // The specific value/array of the constant is calculated and assigned in the field constructor.

                    /// <summary>
                    /// Double value of the constant, used for float and double types.
                    /// </summary>
                    public double? _value_double { get; set; }

                    /// <summary>
                    /// String value of the constant, used for string types.
                    /// </summary>
                    public string? _value_string { get; set; }

#region array
                    /// <summary>
                    /// Array value of the constant, used for array types.
                    /// </summary>
                    public Array? _array_;

                    /// <summary>
                    /// Provides access to an element of the array value for serialization purposes.
                    /// </summary>
                    /// <param name="ctx">The transmitter context (not used here).</param>
                    /// <param name="slot">The transmitter slot (not used here).</param>
                    /// <param name="item">The index of the array element to retrieve.</param>
                    /// <returns>The string representation of the array element at the specified index.</returns>
                    public string _array(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => _array_!.GetValue(item)!.ToString()!;

                    /// <summary>
                    /// Gets the length of the array value.
                    /// </summary>
                    public int _array_len => _array_!.Length;

                    /// <summary>
                    /// Provides access to the array value for serialization purposes.
                    /// </summary>
                    /// <returns>The array value.</returns>
                    public object? _array() => _array_;
#endregion

                    /// <summary>
                    /// Symbol of the static field from which this constant's value is substituted (used with [ValueFor] attribute).
                    /// Null if no substitution is used.
                    /// </summary>
                    public ISymbol? substitute_value_from;


                    /// <summary>
                    /// FieldDeclarationSyntax node associated with this constant, if it's a field-based constant.
                    /// Null if it's an enum member constant or attribute-defined constant.
                    /// </summary>
                    public readonly FieldDeclarationSyntax? fld_node;

                    /// <summary>
                    /// Checks if the constant's name is valid. Exits if the name is prohibited.
                    /// </summary>
                    void check_name()
                    {
                        if( _name.Equals(symbol.Name) ) return;
                        AdHocAgent.LOG.Warning("The field '{entity}' name at the {provided_path} line: {line} is prohibited.", symbol, AdHocAgent.provided_path, line_in_src_code);
                        AdHocAgent.exit(" Please correct the name.");
                    }

                    /// <summary>
                    /// Sets the external type (_exT) of the constant based on the provided value range.
                    /// Selects the smallest suitable integer type (int8, uint8, int16, uint16, int32, uint32, int64, uint64) to fit the range.
                    /// </summary>
                    /// <param name="min">The minimum value of the range.</param>
                    /// <param name="max">The maximum value of the range.</param>
                    public void set_exT_ByRange(BigInteger min, BigInteger max)
                    {
                        if( min == max )
                        {
                            AdHocAgent.LOG.Error("The applied value range for the '{field}' field line:{line} doesn't make sense.", this, line_in_src_code);
                            AdHocAgent.exit("", -1);
                        }

                        if( min < 0 )
                            if( min      < int.MinValue   || int.MaxValue   < max ) _exT = (int)Project.Host.Pack.Field.DataType.t_int64;
                            else if( min < short.MinValue || short.MaxValue < max ) _exT = (int)Project.Host.Pack.Field.DataType.t_int32;
                            else if( min < sbyte.MinValue || sbyte.MaxValue < max ) _exT = (int)Project.Host.Pack.Field.DataType.t_int16;
                            else _exT                                                    = (int)Project.Host.Pack.Field.DataType.t_int8;
                        else if( max > uint.MaxValue ) _exT   = (int)Project.Host.Pack.Field.DataType.t_uint64;
                        else if( max > ushort.MaxValue ) _exT = (int)Project.Host.Pack.Field.DataType.t_uint32;
                        else if( max > byte.MaxValue ) _exT   = (int)Project.Host.Pack.Field.DataType.t_uint16;
                        else _exT                             = (int)Project.Host.Pack.Field.DataType.t_uint8;
                    }

                    /// <summary>
                    /// Initializes a new instance of the <see cref="ConstantImpl"/> class for enum member constants.
                    /// </summary>
                    /// <param name="project">The project this constant belongs to.</param>
                    /// <param name="node">The EnumMemberDeclarationSyntax node representing the enum member.</param>
                    /// <param name="model">The semantic model.</param>
                    public ConstantImpl(ProjectImpl project, EnumMemberDeclarationSyntax node, SemanticModel model) : base(project, node.Identifier.ToString(), node) //enum field
                    {
                        this.model = model;
                        symbol     = model.GetDeclaredSymbol(node)!;

                        check_name();
                        if( entities[symbol!.ContainingType] is PackImpl pack ) pack._constants_.Add(this);
                        else AdHocAgent.exit($"`{entities[symbol!.ContainingType].full_path}` cannot contains any fields. Delete `{_name}`.");

                        raw_static_fields.Add(symbol, this);

                        init_exT(symbol.ContainingType.EnumUnderlyingType!, node.EqualsValue?.Value);
                        if( !_name.Equals(symbol.Name) )
                            AdHocAgent.LOG.Warning("The name of {entity} (line {line}) is prohibited and changed to {new_name}. Please correct the name.", symbol, line_in_src_code, _name);
                    }

                    /// <summary>
                    /// Initializes a new instance of the <see cref="ConstantImpl"/> class for field-based constants (static or const fields).
                    /// </summary>
                    /// <param name="project">The project this constant belongs to.</param>
                    /// <param name="node">The FieldDeclarationSyntax node representing the field declaration.</param>
                    /// <param name="variable">The VariableDeclaratorSyntax node representing the variable declarator.</param>
                    /// <param name="model">The semantic model.</param>
                    public ConstantImpl(ProjectImpl project, FieldDeclarationSyntax node, VariableDeclaratorSyntax variable, SemanticModel model) : base(project, model.GetDeclaredSymbol(variable)!.Name, node) //pack fields
                    {
                        this.model = model;
                        symbol     = model.GetDeclaredSymbol(variable)!;
                        check_name();
                        fld_node = node;

                        raw_static_fields.Add(symbol, this);


                        entities[symbol!.ContainingType]._constants_.Add(this);
                        if( variable.Initializer == null )
                            AdHocAgent.exit($"The static field `{symbol}` (line {line_in_src_code}) is not initialized but must be. Please correct the code.");

                        init_exT(model.GetTypeInfo(node.Declaration.Type).Type!, variable.Initializer!.Value);
                    }

                    /// <summary>
                    /// Initializes a new instance of the <see cref="ConstantImpl"/> class for attribute-defined constants.
                    /// </summary>
                    /// <param name="project">The project this constant belongs to.</param>
                    /// <param name="model">The semantic model.</param>
                    /// <param name="Type">The data type of the constant.</param>
                    /// <param name="src">The ExpressionSyntax node representing the constant's value expression.</param>
                    /// <param name="constant">Optional pre-calculated constant value, if available.</param>
                    public ConstantImpl(ProjectImpl project, SemanticModel model, ITypeSymbol Type, ExpressionSyntax src, object? constant) : base(project, "", null)
                    {
                        this.model = model;
                        init_exT(Type, src, constant);
                    }

                    /// <summary>
                    /// Initializes the external type (_exT) and value of the constant based on the provided type symbol and value expression.
                    /// </summary>
                    /// <param name="Type">The type symbol of the constant.</param>
                    /// <param name="src">The ExpressionSyntax node representing the constant's value expression.</param>
                    /// <param name="constant">Optional pre-calculated constant value, if available.</param>
                    internal void init_exT(ITypeSymbol Type, ExpressionSyntax? src, object? constant = null)
                    {
                        var is_array = Type is IArrayTypeSymbol; // Check if the parameter type is an array


                        // Determine the actual type to work with (element type if it's an array)
                        var actualType = Type;


                        if( src != null )
                            if( is_array )
                            {
                                actualType = ((IArrayTypeSymbol)Type).ElementType;
                                try { _array_ = (Array?)(constant ?? value_of()); }
                                catch( Exception e )
                                {
                                    Console.WriteLine(e);
                                    throw;
                                }

                                constant = null;
                            }
                            else if( constant == null )
                                constant = src.IsKind(SyntaxKind.NumericLiteralExpression) ?
                                               model.GetConstantValue(src).Value :
                                               src.IsKind(SyntaxKind.IdentifierName) ?
                                                   raw_static_fields[model.GetSymbolInfo(src).Symbol!].value_of() :
                                                   value_of(); // runtime constant value


                        switch( actualType.SpecialType )
                        {
                            case SpecialType.System_Boolean:
                                _exT = (int)Project.Host.Pack.Field.DataType.t_bool;

                                if( constant != null )
                                    _value_int = (bool)constant ?
                                                     1 :
                                                     0;
                                break;
                            case SpecialType.System_SByte:
                                _exT = (int)Project.Host.Pack.Field.DataType.t_int8;

                                if( constant != null ) _value_int = Convert.ToInt64(constant);
                                break;
                            case SpecialType.System_Byte:
                                if( constant != null ) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_uint8;
                                break;
                            case SpecialType.System_Int16:
                                if( constant != null ) _value_int = Convert.ToInt64(constant); // a boxed enum member, not a boxed short: no unboxing cast
                                _exT = (int)Project.Host.Pack.Field.DataType.t_int16;
                                break;
                            case SpecialType.System_UInt16:
                                if( constant != null ) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                break;
                            case SpecialType.System_Char:
                                if( constant != null ) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_char;
                                break;
                            case SpecialType.System_Int32:
                                if( constant != null ) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_int32;
                                break;
                            case SpecialType.System_UInt32:
                                if( constant != null ) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_uint32;
                                break;
                            case SpecialType.System_Int64:
                                if( constant != null ) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_int64;
                                break;
                            case SpecialType.System_UInt64:
                                if( constant != null ) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                break;
                            case SpecialType.System_Single:
                                if( constant != null ) _value_double = Convert.ToDouble(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_float;
                                break;
                            case SpecialType.System_Double:
                                if( constant != null ) _value_double = Convert.ToDouble(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_double;
                                break;
                            case SpecialType.System_String:
                                if( constant != null ) _value_string = constant?.ToString();
                                _exT = (int)Project.Host.Pack.Field.DataType.t_string;
                                break;
                            default:
                                if( SymbolEqualityComparer.Default.Equals(actualType, Meta_Binary) )
                                    _exT = (int)Project.Host.Pack.Field.DataType.t_binary;
                                else if( SymbolEqualityComparer.Default.Equals(actualType, Meta_longJS) )
                                {
                                    _exT = (int)Project.Host.Pack.Field.DataType.t_int64;
                                    if( constant != null ) _value_int = Convert.ToInt64(constant);
                                }
                                else if( SymbolEqualityComparer.Default.Equals(actualType, Meta_ulongJS) )
                                {
                                    _exT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                    if( constant != null ) _value_int = Convert.ToInt64(constant);
                                }
                                else //       none primitive types
                                {
                                    // `_name` is "" and `symbol == null` for the attribute-argument constructor
                                    // (object initializer runs only after the constructor returns), so fall back
                                    // to the source expression for both a useful name and a real line number.
                                    var loc = src?.GetLocation().GetLineSpan().StartLinePosition.Line + 1 ?? line_in_src_code;
                                    var nameForMsg = string.IsNullOrEmpty(_name) ?
                                                         src?.Parent?.ToString().Trim() ?? "<attribute argument>" :
                                                         _name;
                                    AdHocAgent.exit($"Constant field '{nameForMsg}' (line {loc}) cannot have {actualType} type. Supported: bool, byte/sbyte, short/ushort, int/uint, long/ulong, char, float, double, string, binary, longJS, ulongJS.", 56);
                                }

                                break;
                        }
                    }
                }

                /// <summary>
                /// Represents the implementation of a Field entity within a Pack.
                /// </summary>
                public class FieldImpl : HasDocs, Project.Host.Pack.Field{
                    public FieldImpl? copy_attributes_from;


                    /// <summary>
                    /// FieldDeclarationSyntax node associated with this field.
                    /// </summary>
                    public readonly FieldDeclarationSyntax? fld_node;

                    /// <summary>
                    /// Initializes a new instance of the <see cref="FieldImpl"/> class for virtual fields (used for Map value types).
                    /// </summary>
                    /// <param name="project">The project this field belongs to.</param>
                    /// <param name="node">The FieldDeclarationSyntax node (can be null for virtual fields).</param>
                    /// <param name="model">The semantic model (can be null for virtual fields).</param>
                    public FieldImpl(ProjectImpl project, FieldDeclarationSyntax? node, SemanticModel? model) : base(project, "", null) //virtual field used to hold information of V in Map(K,V)
                    {
                        fld_node   = node;
                        this.model = model!; // model is guaranteed to be non-null when FieldImpl is created for real fields
                    }

                    /// <summary>
                    /// Indicates whether this field is a Set collection.
                    /// </summary>
                    public bool is_Set;


                    /// <summary>
                    /// Indicates whether this field is a Map collection.
                    /// </summary>
                    public bool is_Map => V != null;

                    /// <summary>
                    /// Indicates whether this field is of String type.
                    /// </summary>
                    public bool is_String => exT_primitive == (int?)Project.Host.Pack.Field.DataType.t_string;

                    /// <summary>
                    /// Indicates whether this field is stream-based: a raw `Stream`/`File` conduit, or a field made
                    /// DIRECTIONAL by a trim attribute (`[ToStream&lt;E&gt;]` / `[FromStream&lt;E&gt;]` / `[Stream&lt;To, From&gt;]`).
                    /// <para>The trim part only answers true once this field's chain has been materialized — see
                    /// <see cref="cut_to_codes"/>.</para>
                    /// </summary>
                    public bool is_Stream => is_raw_conduit || cut_to_codes != null || cut_from_codes != null;

                    // A raw, unbounded Stream/File conduit (the bare datatype or a named Stream/File pack), NOT a field
                    // made directional by a trim attribute. Only these require & accept the [S(N)] cap; a trimmed field
                    // wraps a bounded payload - a pack, or a string capped by [D(+N)] - and needs none.
                    public bool is_raw_conduit => exT_primitive == (int)Project.Host.Pack.Field.DataType.t_stream || exT_primitive == (int)Project.Host.Pack.Field.DataType.t_file;

                    // The chain entries this field accumulates, in dataflow order (leaf -> wire); materialized in Pass B
                    // (read_attributes phase), where creating the container pack via root_project is safe. Two sources
                    // feed it, in this order:
                    //   1. the field's own stage / flow / trim attributes (Pass A — `FieldImpl.init`),
                    //   2. the chain declared by the named Stream/File pack the field is typed with, if any
                    //      ("Reinterpret named Stream/File typed fields as bare conduits", which runs after Pass A).
                    // Each entry carries the SemanticModel that BINDS it — the two sources may live in different syntax
                    // trees, and a model only binds nodes from its own — and the name of the pack it was inherited from,
                    // so a conflict between an inherited stage and the field's own can say where the other one came from.
                    internal List<(SemanticModel model, AttributeSyntax attr, string? inherited_from)>? _stream_stage_attrs;

                    public bool is_Header;
                    public bool is_HeaderModifier;

                    /// <summary>
                    /// The legs on which this field is CUT, each encoded as the connection index with its SIGN naming the
                    /// sending host — <c>conn.idx</c> for the Left one, <c>~conn.idx</c> for the Right. On a
                    /// <see cref="cut_to_codes"/> leg the receiver takes raw bytes; on a <see cref="cut_from_codes"/> leg
                    /// the sender supplies them.
                    /// </summary>
                    /// <remarks>
                    /// Written by the "Record the legs each field is cut on" pass, from two sources: the trims this field
                    /// declares itself, and the trims of the pack it is typed with, which name legs for every field of that
                    /// type. <see cref="HasDocs.collect_stream_stages"/> adds the same codes when it builds the trim's
                    /// sub-pack. Every write goes through <see cref="ProjectImpl.resolve_cut"/>, so the endpoints behind a
                    /// cut are resolved and diagnosed in exactly one place. Read by the transmitting-pack distribution,
                    /// through <see cref="cut_on"/>. A field no trim reaches keeps them null.
                    /// </remarks>
                    internal HashSet<int>? cut_to_codes;
                    internal HashSet<int>? cut_from_codes;

                    /// <summary>Is this field cut on <paramref name="connection"/> when the sender is its Right host (<paramref name="isR"/>) or its Left one?</summary>
                    internal (bool to, bool from) cut_on(ConnectionImpl connection, bool isR)
                    {
                        var code = isR ?
                                       ~connection.idx :
                                       connection.idx;
                        return (cut_to_codes?.Contains(code) ?? false, cut_from_codes?.Contains(code) ?? false);
                    }

                    public void notHeaderField()
                    {
                        if( is_Header || is_HeaderModifier )
                            AdHocAgent.exit($"The {(is_Header ? "header" : "header modifier")} field `{_name}` line:{line_in_src_code} of `{entities[symbol!.ContainingType].full_path}` must be a non-nullable, non-varint, single, primitive type. The current type is invalid.");
                    }

                    /// <summary>
                    /// Initializes a new instance of the <see cref="FieldImpl"/> class for regular fields declared in packs.
                    /// </summary>
                    /// <param name="project">The project this field belongs to.</param>
                    /// <param name="node">The FieldDeclarationSyntax node representing the field declaration.</param>
                    /// <param name="variable">The VariableDeclaratorSyntax node representing the variable declarator.</param>
                    /// <param name="model">The semantic model.</param>
                    public FieldImpl(ProjectImpl project, FieldDeclarationSyntax node, VariableDeclaratorSyntax variable, SemanticModel model) : base(project, model.GetDeclaredSymbol(variable)!.Name, node) //pack fields
                    {
                        this.model = model;
                        symbol     = model.GetDeclaredSymbol(variable)!;
                        fld_node   = node;

                        if( _name.Length != 0 )
                            if( !_name.Equals(symbol.Name) )
                            {
                                AdHocAgent.LOG.Warning("The field '{entity}' name at the {provided_path} line: {line} is prohibited.", symbol, AdHocAgent.provided_path, line_in_src_code);
                                AdHocAgent.exit(" Please correct the name.");
                            }

                        var T = model.GetTypeInfo(node.Declaration.Type).Type!;

                        if( ((INamedTypeSymbol)symbol!.ContainingSymbol).TypeKind == TypeKind.Struct )
                            AdHocAgent.exit($"All fields in the constant collection "                                               +
                                            $"{(INamedTypeSymbol)model.GetTypeInfo(node.Declaration.Type)!.Type!.ContainingSymbol}" +
                                            " must be const or static with assigned value, but the field '"                         +
                                            $"{symbol.Name}' (line {line_in_src_code}) is not. Correct the problem and rerun.");

                        raw_fields.Add(symbol, this);

                        switch( entities[symbol!.ContainingType] )
                        {
                            case PackImpl pack:
                                if( pack.is_Stream || pack.is_File )
                                    AdHocAgent.exit($"Stream/File pack `{pack.symbol}` carries framing only and cannot contain instance fields (constants and static fields are fine). Delete the field `{_name}` (line {line_in_src_code}).", 88);
                                pack.fields.Add(this);
                                is_Header         = pack.is_Header;
                                is_HeaderModifier = pack.is_HeaderModifier;
                                break;
                            default: AdHocAgent.exit($"The entity `{entities[symbol!.ContainingType].full_path}` cannot contains any fields. Delete the field `{_name}` line:{line_in_src_code}."); break;
                        }

                        if( T is INamedTypeSymbol namedType )
                        {
                            var originalDef = namedType.OriginalDefinition;
                            if( equals(originalDef, Meta_Stream_0) )
                            {
                                inT = exT_primitive = (int)Project.Host.Pack.Field.DataType.t_stream;
                                return;
                            }

                            if( equals(originalDef, Meta_File_0) )
                            {
                                inT = exT_primitive = (int)Project.Host.Pack.Field.DataType.t_file;
                                return;
                            }

                            // A field is made directional by a TRIM ATTRIBUTE — `[ToStream<E>] Pack fld;` — not by its
                            // type. The legs it is cut on land in `cut_to_codes` / `cut_from_codes` when the field's
                            // chain is materialized; nothing about a cut is decided here.
                        }


                        void KV_params(ITypeSymbol KV, FieldImpl dst) // Processes key-value parameters for collection types (Sets and Maps)
                        {
                            notHeaderField();

                            if( KV.NullableAnnotation == NullableAnnotation.Annotated ) dst.set_null_value_bit(2); // generic-param slot itself is nullable: Set<Type?> f; / Map<Type?,V> f; / Map<K,Type?> f; / Set<Type[,]?> f
                            switch( KV )
                            {
                                case IArrayTypeSymbol array:                                                                              // generic-param slot is an array: Set<Type[,,]> f; / Map<K,Type[,,]> f
                                    if( array.ElementType.NullableAnnotation == NullableAnnotation.Annotated ) dst.set_null_value_bit(0); // ...with nullable elements: Set<Type?[,,]> f; / Map<K,Type?[,,]> f
                                    dst._exT_array = (uint)(array.Rank - 1);

                                    dst.init_exT((INamedTypeSymbol)array.ElementType); // Initialize with the array's element type
                                    return;
                                case INamedTypeSymbol type:
                                    dst.init_exT(type); // Handle non-array types directly
                                    return;
                            }
                        }


                        if( T.isSet() )
                        {
                            is_Set = true;

                            switch( T )
                            {
                                case IArrayTypeSymbol array:                                                                          //Set<int>[,] field;
                                    if( array.ElementType.NullableAnnotation == NullableAnnotation.Annotated ) set_null_value_bit(1); //Set<int>?[,] field;
                                    _map_set_array = (uint)(array.Rank - 1);

                                    KV_params(((INamedTypeSymbol)array.ElementType).TypeArguments[0], this);

                                    break;
                                case INamedTypeSymbol type:
                                    KV_params(type.TypeArguments[0], this);
                                    break;
                            }

                            if( exT_primitive == (int)Project.Host.Pack.Field.DataType.t_bool )
                                AdHocAgent.exit($"The field `{symbol}` at the line: {line_in_src_code} is a Set of `bool` type, which is unsupported and unnecessary.");
                        }
                        else if( T.isMap() )
                        {
                            V = new(project, node, model); //The Map Value info

                            void KV(INamedTypeSymbol type)
                            {
                                KV_params(type.TypeArguments[0], this);
                                KV_params(type.TypeArguments[1], V);
                            }

                            switch( T )
                            {
                                case IArrayTypeSymbol array:                                                                          //Map<int, int> [,] field;
                                    if( array.ElementType.NullableAnnotation == NullableAnnotation.Annotated ) set_null_value_bit(1); //Map<int, int>?[,] field;
                                    _map_set_array = (uint)(array.Rank - 1);

                                    KV((INamedTypeSymbol)array.ElementType);

                                    break;
                                case INamedTypeSymbol type:
                                    KV(type);
                                    break;
                            }

                            if( exT_primitive == (int)Project.Host.Pack.Field.DataType.t_bool )
                                AdHocAgent.exit($"The field `{symbol}` at the line: {line_in_src_code} is a Map with a key of type `bool`, which is unsupported and unnecessary.");
                        }
                        else
                            switch( T )
                            {
                                case IArrayTypeSymbol array:
                                    notHeaderField();

                                    switch( array.ElementType )
                                    {
                                        case IArrayTypeSymbol array_ext:                                                              //int[][]  field;
                                            if( array_ext.NullableAnnotation == NullableAnnotation.Annotated ) set_null_value_bit(1); //int[]?[] field;
                                            _map_set_array = (uint)(array.Rank - 1);

                                            _exT_array = (uint)(array_ext.Rank - 1);
                                            init_exT((INamedTypeSymbol)array_ext.ElementType);
                                            break;

                                        case INamedTypeSymbol type:
                                            if( array.ElementType.NullableAnnotation == NullableAnnotation.Annotated ) set_null_value_bit(0);
                                            _exT_array = (uint)(array.Rank - 1);
                                            init_exT(type);

                                            break;
                                    }

                                    break;

                                case INamedTypeSymbol type:
                                    if( fld_node?.Declaration.Type is NullableTypeSyntax ) //the field is a single nullable primitive (e.g., int? field;)
                                    {
                                        set_null_value_bit(0); // !!!
                                        init_exT(type is { SpecialType: SpecialType.None, TypeArguments.Length: > 0 } ?
                                                     (INamedTypeSymbol)type.TypeArguments[0] :
                                                     type);
                                    }
                                    else
                                        init_exT(type);

                                    if( exT_primitive is null or (int)Project.Host.Pack.Field.DataType.t_string or (int)Project.Host.Pack.Field.DataType.t_binary || nullable ) notHeaderField();
                                    break;
                            }
                    }

                    /// <summary>
                    /// Switches this field to boolean type, used when an empty pack is used as a field type.
                    /// </summary>
                    public void switch_to_boolean()
                    {
                        exT_pack      = null;
                        exT_primitive = (int?)Project.Host.Pack.Field.DataType.t_bool;
                        inT           = (int?)Project.Host.Pack.Field.DataType.t_bool;
                        _bits = (byte)(nullable ?
                                           2 :
                                           1);
                    }

                    /// <summary>
                    /// Static class to store default maximum lengths for collection types.
                    /// </summary>
                    public class _DefaultMaxLengthOf{
                        /// <summary>
                        /// Default maximum length for String fields.
                        /// </summary>
                        public static int Strings = 255;

                        /// <summary>
                        /// Default maximum length for Array fields.
                        /// </summary>
                        public static int Arrays = 255;

                        /// <summary>
                        /// Default maximum length for Map fields.
                        /// </summary>
                        public static int Maps = 255;

                        /// <summary>
                        /// Default maximum length for Set fields.
                        /// </summary>
                        public static int Sets = 255;

                        /// <summary>
                        /// How long a pack may be, in bytes, to go uncompressed: the compression stage is taken off the chain of a pack that
                        /// cannot be longer. 0 - off none.
                        /// </summary>
                        public static int Uncompressed = 1024;
                    }

#region External Type Configuration (_exT)
                    /// <summary>
                    /// Initializes the external type (_exT) of the field based on the provided type symbol.
                    /// </summary>
                    /// <param name="T">The INamedTypeSymbol representing the field's type.</param>
                    /// <returns>The INamedTypeSymbol that was used for initialization (potentially unwrapped from nullable type).</returns>
                    internal INamedTypeSymbol init_exT(INamedTypeSymbol T)
                    {
                        if( T.NullableAnnotation == NullableAnnotation.Annotated )
                        {
                            set_null_value_bit(0);
                            T = T.TypeArguments.Length == 0 ?
                                    T.ConstructedFrom :
                                    (INamedTypeSymbol)T.TypeArguments[0];
                        }

                        switch( T.SpecialType )
                        {
                            case SpecialType.System_Boolean:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_bool;

                                _bits = (byte)(nullable ?
                                                   2 : //2->NULL  1->true  0->false
                                                   1);

                                break;
                            case SpecialType.System_SByte:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_int8;
                                break;
                            case SpecialType.System_Byte:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_uint8;
                                break;
                            case SpecialType.System_Int16:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_int16;
                                break;
                            case SpecialType.System_UInt16:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                break;
                            case SpecialType.System_Char:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_char;
                                break;
                            case SpecialType.System_Int32:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_int32;
                                break;
                            case SpecialType.System_UInt32:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_uint32;
                                break;
                            case SpecialType.System_Int64:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_int64;
                                break;
                            case SpecialType.System_UInt64:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                break;
                            case SpecialType.System_Single:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_float;
                                break;
                            case SpecialType.System_Double:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_double;
                                break;
                            case SpecialType.System_DateTime:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_date;
                                _min_value    = 0;
                                _max_value = DateTimeOffset.MaxValue.ToUnixTimeMilliseconds() - (nullable ?
                                                                                                     1 :
                                                                                                     0); //nullable reserves the top value (253402300799999) as the null sentinel
                                break;
                            case SpecialType.System_String:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_string;
                                break;
                            default:
                                if( SymbolEqualityComparer.Default.Equals(T, Meta_Binary) )
                                    exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_binary;
                                else if( SymbolEqualityComparer.Default.Equals(T, Meta_longJS) )
                                {
                                    exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_int64;
                                    _min_value    = -0x1FFFFFFFFFFFFF;
                                    _max_value    = 0x1FFFFFFFFFFFFF;
                                }
                                else if( SymbolEqualityComparer.Default.Equals(T, Meta_ulongJS) )
                                {
                                    exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                    _min_value    = 0;
                                    _max_value    = 0x1FFFFFFFFFFFFF;
                                }
                                else //       none primitive types
                                {
                                    exT_primitive = null;
                                    if( T.IsImplicitClass )
                                        AdHocAgent.exit($"Constants set {T} cannot be referenced. But field {_name} (line {line_in_src_code}) do.", 56);
                                    exT_pack = T;
                                }

                                break;
                        }

                        return T;
                    }

                    /// <summary>
                    /// Sets the external type (_exT) of the field based on the provided value range.
                    /// Selects the smallest suitable integer type (int8, uint8, int16, uint16, int32, uint32, int64, uint64) to fit the range.
                    /// </summary>
                    /// <param name="min">The minimum value of the range.</param>
                    /// <param name="max">The maximum value of the range.</param>
                    public void set_exT_ByRange(BigInteger min, BigInteger max)
                    {
                        if( min == max )
                        {
                            AdHocAgent.LOG.Error("The applied value range for the '{field}' field line:{line} doesn't make sense.", this, line_in_src_code);
                            AdHocAgent.exit("", -1);
                        }

                        if( min < 0 )
                            if( min      < int.MinValue   || int.MaxValue   < max ) exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int64;
                            else if( min < short.MinValue || short.MaxValue < max ) exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int32;
                            else if( min < sbyte.MinValue || sbyte.MaxValue < max ) exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int16;
                            else exT_primitive                                                    = (int)Project.Host.Pack.Field.DataType.t_int8;
                        else if( uint.MaxValue   < max ) exT_primitive = (int)Project.Host.Pack.Field.DataType.t_uint64;
                        else if( ushort.MaxValue < max ) exT_primitive = (int)Project.Host.Pack.Field.DataType.t_uint32;
                        else if( byte.MaxValue   < max ) exT_primitive = (int)Project.Host.Pack.Field.DataType.t_uint16;
                        else exT_primitive                             = (int)Project.Host.Pack.Field.DataType.t_uint8;
                    }

                    /// <summary>
                    /// An upper bound of what this field adds to the serialized size of its pack, in bytes, or -1: there is none.
                    /// See <see cref="PackImpl.upper_bound_bytes"/>.
                    /// </summary>
                    internal long upper_bound_bytes(HashSet<PackImpl> path)
                    {
                        if( is_Stream || _stream_stage_attrs != null ) return -1; // a conduit, or bytes a chain of its own transforms

                        // a collection of `max` items of `item` bytes: every item with its null, behind its length
                        static long many(long item, long max) => Math.Min((item + 1) * max + 9, int.MaxValue);

                        // one value of the type of `of`, which is this field or the value of its Map
                        long one(FieldImpl of)
                        {
                            long bytes;
                            if( of.get_exT_pack is { } pack ) bytes = pack.is_enum || pack.is_constants_set ? 8 : pack.upper_bound_bytes(path);
                            else if( of.is_String ) bytes           = (of._max_value ?? _DefaultMaxLengthOf.Strings) * 3 + 5; // a varint a char, three bytes at most, behind the varint of their number
                            else if( of.exT_primitive == (int)Project.Host.Pack.Field.DataType.t_binary ) bytes = 1;
                            else bytes = of.exT_primitive is { } type && type_bit_size(type) is var bits and < 100 ? bits / 8 + 2 : -1; // with the length of a varint and a null
                            if( bytes < 0 ) return -1;
                            return of._exT_array is { } array ? many(bytes, array >> 3) : bytes + 1;
                        }

                        var size = one(this);
                        if( size < 0 ) return -1;
                        if( is_Map )
                        {
                            var value = one(V!);
                            if( value < 0 ) return -1;
                            size += value;
                        }

                        if( is_Map || is_Set ) size = many(size, _map_set_len ?? (uint)(is_Map ? _DefaultMaxLengthOf.Maps : _DefaultMaxLengthOf.Sets));
                        if( _map_set_array is { } arrays ) size = many(size, arrays >> 3);
                        if( dims != null )
                            foreach( var dim in dims )
                                size = many(size, dim >> 1);
                        return size;
                    }

                    /// <summary>
                    /// Gets the PackImpl instance representing the external pack type of this field.
                    /// Returns null if the field is of a primitive type.
                    /// </summary>
                    internal PackImpl? get_exT_pack => exT_pack == null ?
                                                           exT_made :
                                                           (PackImpl)entities[exT_pack];

                    /// <summary>
                    /// The external pack type when it is a pack the agent made and no symbol names: the header of a <c>[UDP]</c>
                    /// connection. Null otherwise.
                    /// </summary>
                    internal PackImpl? exT_made;

                    /// <summary>
                    /// Gets the PackImpl instance representing the final external pack type of this field through typedefs chain.
                    /// Returns null if the field is of a primitive type.
                    /// </summary>
                    internal PackImpl? get_exT()
                    {
                        var ext                                    = get_exT_pack;
                        while( ext != null && ext.is_typedef ) ext = ext.fields[0].get_exT_pack;
                        return ext;
                    }

                    /// <summary>
                    /// Symbol of the external pack type for this field. Null if the field is of a primitive type.
                    /// </summary>
                    public INamedTypeSymbol? exT_pack;

                    /// <summary>
                    /// External primitive type of the field. Null if the field is of a pack type.
                    /// </summary>
                    public int? exT_primitive;


                    public ushort _exT => (ushort)(exT_primitive ?? get_exT_pack?.idx)!;

                    /// <summary>
                    /// Array rank of the external type (if it's an array). Stores rank - 1.
                    /// </summary>
                    public uint? _exT_array { get; set; }

                    /// <summary>
                    /// Maximum length of the Map or Set collection.
                    /// </summary>
                    public uint? _map_set_len { get; set; } //mandatory if Map or Set

                    /// <summary>
                    /// Array rank of the Map or Set collection (if it's an array of collections). Stores rank - 1.
                    /// The lower 3 bits store the rank, and higher bits can store other flags.
                    /// </summary>
                    public uint? _map_set_array { get; set; } //the flat array of Map/Set/Array collection params
#endregion

#region inT - Internal Type Configuration
                    /// <summary>
                    /// Sets the internal type (_inT) of the field based on the provided value range.
                    /// Selects the smallest suitable unsigned integer type (uint8, uint16, uint32, uint64) to fit the range.
                    /// </summary>
                    /// <param name="min">The minimum value of the range.</param>
                    /// <param name="max">The maximum value of the range.</param>
                    public void set_inT_ByRange(BigInteger min, BigInteger max)
                    {
                        if( min == max )
                        {
                            AdHocAgent.LOG.Error("The applied value range for the '{field}' field line:{line} doesn't make sense.", this, line_in_src_code);
                            AdHocAgent.exit("", -1);
                        }

                        var range = max - min;

                        if( nullable && range < 0x80 ) range += 1;

                        switch( (ulong)range ) //by range
                        {
                            case < 0x80:

                                inT = (int)Project.Host.Pack.Field.DataType.t_uint8; //bits field. values range < 0x80

                                _bits = (byte)(32 - BitOperations.LeadingZeroCount((uint)(max + 1 - min)));
                                break;
                            case <= byte.MaxValue:
                                inT = (int)Project.Host.Pack.Field.DataType.t_uint8;
                                break;
                            case <= ushort.MaxValue:
                                inT = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                break;
                            case <= uint.MaxValue:
                                inT = (int)Project.Host.Pack.Field.DataType.t_uint32;
                                break;
                            default:
                                inT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                break;
                        }
                    }


                    public int?    inT; //internal Type. if it is null then  the field type is a reference  to other packs
                    public ushort? _inT => (ushort)(inT ?? get_exT_pack!.idx);
#endregion

                    /// <summary>
                    /// Data direction for VarInt encoding (1 for ascending, -1 for descending, 0 for zero-based amplitude).
                    /// Null if not specified or not applicable.
                    /// </summary>
                    public sbyte _dir { get; set; } = Project.Host.Pack.Field._dir__.NULL;

                    /// <summary>
                    /// Minimum allowed value for the field (used for validation and range checks).
                    /// </summary>
                    public long? _min_value { get; set; }

                    /// <summary>
                    /// Maximum allowed value for the field (used for validation and range checks).
                    /// </summary>
                    public long? _max_value { get; set; }

                    /// <summary>
                    /// Set by the first [MinMax] / [A] / [V] / [X] seen on this field; a second one is an error.
                    /// Per field for its whole lifetime - never reset between the field's attribute lists.
                    /// </summary>
                    bool _check_once;

                    /// <summary>
                    /// Checks if the MinMax attribute is used correctly and exits if it's misused with VarInt attributes.
                    /// </summary>
                    void check_MinMax_using()
                    {
                        if( !_check_once )
                        {
                            _check_once = true;
                            return;
                        }

                        AdHocAgent.LOG.Error("The MinMax attribute for Field {field} (line: {line}) cannot be used with VarInt attributes[X, V, A]. Use VarInt attribute arguments to set MinMax restrictions.", _name, line_in_src_code);
                        AdHocAgent.exit("Please resolve the issue and try running again.");
                    }

                    /// <summary>
                    /// Minimum allowed double value for the field (used for validation and range checks for float/double types).
                    /// </summary>
                    public double? _min_valueD { get; set; }

                    /// <summary>
                    /// Maximum allowed double value for the field (used for validation and range checks for float/double types).
                    /// </summary>
                    public double? _max_valueD { get; set; }

                    /// <summary>
                    /// Number of bits used for encoding the field's value (used for bit-packing optimization).
                    /// </summary>
                    public byte _bits { get; set; } = Project.Host.Pack.Field._bits__.NULL;

                    /// <summary>
                    /// Null-value flag bits for nullable fields. Bits are not mutually exclusive.
                    /// Bit 0: leaf / innermost element type is nullable (e.g. <c>int? f</c>, <c>string?[][] f</c>, <c>Set&lt;Type?[,]&gt; f</c>).
                    /// Bit 1: inner collection / inner array is nullable as an element of an outer multi-dim array (e.g. <c>Set&lt;int&gt;?[,] f</c>, <c>int[]?[] f</c>).
                    /// Bit 2: Set/Map generic-parameter slot itself is nullable-annotated (e.g. <c>Set&lt;Type?&gt; f</c>, <c>Set&lt;Type[,]?&gt; f</c>, <c>Map&lt;Type?, V&gt; f</c>).
                    /// </summary>
                    public byte? _null_value { get; set; }

                    /// <summary>
                    /// Sets a specific null value flag bit.
                    /// </summary>
                    /// <param name="bit">The bit index to set (0, 1, or 2).</param>
                    public void set_null_value_bit(int bit) => _null_value = (byte)(_null_value == null ?
                                                                                        1 << bit :
                                                                                        _null_value.Value | 1 << bit);

                    /// <summary>
                    /// Indicates whether this field is nullable.
                    /// </summary>
                    internal bool nullable => _null_value != null && (_null_value.Value & 1) == 1;

#region Map Value Type Configuration (V)
                    /// <summary>
                    /// FieldImpl instance representing the value type for Map fields.
                    /// Null if this field is not a Map.
                    /// </summary>
                    public FieldImpl? V; //Map Value datatype Info


                    public ushort? _exTV       => V?._exT;
                    public uint?   _exTV_array => V?._exT_array;

                    public ushort? _inTV        => V?._inT;
                    public sbyte   _dirV        => V?._dir ?? Project.Host.Pack.Field._dir__.NULL;
                    public long?   _min_valueV  => V?._min_value;
                    public long?   _max_valueV  => V?._max_value;
                    public double? _min_valueDV => V?._min_valueD;
                    public double? _max_valueDV => V?._max_valueD;

                    public byte _bitsV => V?._bits ?? Project.Host.Pack.Field._bitsV__.NULL;

                    public byte? _null_valueV => V?._null_value;
#endregion

#region Array Dimensions Configuration (dims)
                    /// <summary>
                    /// Array dimensions for multi-dimensional arrays.
                    /// Each element in the array represents a dimension size.
                    /// Positive even numbers are constant dimensions, positive odd numbers are fixed-size dimensions.
                    /// </summary>
                    public int[]? dims;

                    /// <summary>
                    /// Provides access to the array dimensions for serialization purposes.
                    /// Returns null if dims is null.
                    /// </summary>
                    /// <returns>The array of dimensions or null if dims is null.</returns>
                    public object? _dims() => dims;

                    /// <summary>
                    /// Gets the length of the dimensions array.
                    /// </summary>
                    public int _dims_len => dims?.Length ?? 0;

                    /// <summary>
                    /// Retrieves a specific dimension size at the given index.
                    /// </summary>
                    /// <param name="ctx">The transmitter context (not used here).</param>
                    /// <param name="slot">The transmitter slot (not used here).</param>
                    /// <param name="item">The index of the dimension to retrieve.</param>
                    /// <returns>The dimension size at the specified index.</returns>
                    public int _dims(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => dims![item]; // Potential null ref exception
#endregion

                    /// <summary>
                    /// Retrieves the value of an expression, resolving constant field references if necessary.
                    /// </summary>
                    /// <param name="src">The ExpressionSyntax node representing the value expression.</param>
                    /// <returns>The value of the expression as an object.</returns>
                    object? value_of(ExpressionSyntax src) => src is IdentifierNameSyntax const_fld ?                                    // value of the expression,
                                                                  raw_static_fields[model.GetSymbolInfo(const_fld).Symbol!].value_of() : //value of referenced constant field
                                                                  model.GetConstantValue(src).Value;                                     //value of expression, case when using reflection value useless

                    /// <summary>
                    /// Initializes static data and processes field attributes for all fields in the project.
                    /// </summary>
                    /// <param name="project">The root project instance.</param>
                    public static void init(ProjectImpl project)
                    {
#region Process Attributes
#region Process static fields with ValueForAttribute
                        foreach( var src_fld in raw_static_fields.Values.Where(fld => fld.fld_node != null) )
                            foreach( var args_list in from list in src_fld.fld_node!.AttributeLists
                                                      from attr in list.Attributes
                                                      where attr.Name.ToString().Equals("ValueFor")
                                                      select attr.ArgumentList
                                                      into args_list
                                                      where args_list != null
                                                      select args_list )
                            {
                                // Transfer calculated values of static fields with the [ValueFor(dst_const_field)] attribute to their corresponding dedicated constant fields.
                                var dst_const_fld = raw_static_fields[src_fld.model.GetSymbolInfo(args_list.Arguments[0].Expression).Symbol!];
                                if( dst_const_fld.substitute_value_from != null )
                                {
                                    AdHocAgent.LOG.Error("The const field {const_field} (line {line}) already has a value assigned from static field {current_static}, and the static field {new_static} would override it. This redundancy is unnecessary and serves no purpose.", dst_const_fld, dst_const_fld.line_in_src_code, dst_const_fld.substitute_value_from, src_fld.symbol);
                                    AdHocAgent.exit("Fix the problem and rerun");
                                }

                                dst_const_fld.substitute_value_from = src_fld.symbol;
                                dst_const_fld._exT                  = src_fld._exT;
                                dst_const_fld._value_double         = src_fld._value_double;
                                dst_const_fld._value_int            = src_fld._value_int;
                                dst_const_fld._value_string         = src_fld._value_string;
                                dst_const_fld._array_               = src_fld._array_;
                            }
#endregion

                        var dims = new List<int>();

                        foreach( var fld in raw_fields.Values )
                        {
                            var FLD = fld;

                            T _value_of<T>(ExpressionSyntax src) => (T)Convert.ChangeType(FLD.value_of(src)!, typeof(T));

                            BigInteger big_int_value_of(ExpressionSyntax src) => FLD.value_of(src)! switch
                                                                                 {
                                                                                     ulong value  => value,
                                                                                     long value   => value,
                                                                                     uint value   => value,
                                                                                     int value    => value,
                                                                                     ushort value => value,
                                                                                     short value  => value,
                                                                                     char value   => value,
                                                                                     byte value   => value,
                                                                                     sbyte value  => value,
                                                                                     _            => throw new InvalidOperationException("Unsupported data type")
                                                                                 };

                            foreach( var list in fld.fld_node!.AttributeLists ) //process fields attributes
                            {
                                var KV = list.Target == null ? //allpy to the map/set generics
                                             ' ' :
                                             list.Target.ToString().ToUpper()[0];
                                switch( KV ) //check that the generic target attributes are used correctly
                                {
                                    case 'V':
                                        if( (FLD = fld.V) == null ) //attributes are for Value generic of the Map type field
                                            AdHocAgent.exit($"You have inappropriately used the 'Val:' attributes target on the '{fld}' field (line {fld.line_in_src_code}). The 'Val:' can only be applied to fields of Map type.", 2);
                                        break;
                                    case 'K' when !(fld.is_Set || fld.is_Map): //attributes are for Key generic of the Map/Set type field
                                        AdHocAgent.exit($"You have inappropriately used the 'Key:' attributes target on the '{fld}' field (line {fld.line_in_src_code}). The 'Key:' can only be applied to fields of Map or Set type.", 2);
                                        break;
                                }

                                // `_check_once` is deliberately NOT reset here. At most one of [MinMax] / [A] / [V] / [X] may target a
                                // given field, and this loop walks the attribute LISTS of one field - resetting per list let
                                // `[MinMax(0, 1000)] [X] int x;` through, silently applying both, while the same pair inside one
                                // list `[MinMax(0, 1000), X]` was rejected. The flag starts false and each FieldImpl (and each Map
                                // value, which is a FieldImpl of its own) is visited once, so no reset is needed.
                                foreach( var attr_ctor in list.Attributes )
                                {
                                    var name           = attr_ctor.Name.ToString();
                                    var attr_args_list = attr_ctor.ArgumentList;

                                    // Stream transform-stage / flow / trim attributes are recognized as a byte-transform
                                    // CHAIN, not ordinary metadata: collect them here (in source order across all lists)
                                    // and materialize the chain later in Pass B. Built-in (Zstd/ChaCha20) and custom
                                    // stages both flow through this one path, and so do the trim markers, whose meaning
                                    // is their POSITION among the stages.
                                    if( (fld.model.GetSymbolInfo(attr_ctor).Symbol as IMethodSymbol)?.ContainingType is { } _atype && (ProjectImpl.is_stream_stage(_atype) || ProjectImpl.is_stream_flow(_atype) || ProjectImpl.is_stream_trim(_atype)) )
                                    {
                                        (fld._stream_stage_attrs ??= []).Add((fld.model, attr_ctor, null)); // the field's own: its own model, not inherited
                                        continue;
                                    }

                                    switch( !name.EndsWith("Attribute") ?
                                                $"{name}Attribute" :
                                                name )
                                    {
                                        case "SAttribute":
                                        {
                                            if( !FLD.is_raw_conduit )
                                            {
                                                // A field typed with a named Stream/File pack IS a raw conduit (it is rewritten into one
                                                // right after the typedef pass), but its cap is already declared on that pack — one source
                                                // of truth. Report that case precisely instead of the generic message below.
                                                if( FLD.exT_pack != null && entities.TryGetValue(FLD.exT_pack, out var _e) && _e is PackImpl { } _conduit && (_conduit.is_Stream || _conduit.is_File) )
                                                    AdHocAgent.exit($"The [S(N)] attribute on field '{fld.symbol}' (line: {fld.line_in_src_code}) duplicates a cap that its datatype already declares: "  +
                                                                    $"pack `{_conduit.symbol}` is `[S({Math.Abs(_conduit._stream_max)})]`. Delete the attribute from the field, or type the field as a " +
                                                                    "bare `Stream`/`File` and give it its own cap.", 2);
                                                AdHocAgent.exit($"The [S(N)] attribute applies only to a bare Stream/File field (a raw, unbounded conduit), not to a field that wraps a bounded payload — a pack, or a string capped by [D(+N)] — but was found on field '{fld.symbol}' (line: {fld.line_in_src_code}). A `[ToStream<E>]` / `[FromStream<E>]` / `[Stream<To, From>]` trim needs no cap: the payload bounds itself.", 2);
                                            }

                                            // The attribute constructor ensures there is exactly one argument.
                                            var attr_args = attr_args_list!.Arguments;
                                            var s_n       = (long)big_int_value_of(attr_args[0].Expression);

                                            // The cap must exceed 8 bytes: a payload that fits in 8 bytes or fewer is no larger than a
                                            // single 64-bit primitive, so it belongs in a fixed primitive or a Binary collection (no
                                            // chunk/length framing overhead), not in a raw Stream/File conduit.
                                            if( s_n <= 8 ) AdHocAgent.exit($"The `[S(N)]` attribute on field '{fld.symbol}' (line: {fld.line_in_src_code}) must specify a maximum size greater than 8 bytes, but got {s_n}. A payload that small is no larger than a single 64-bit primitive — use a primitive or a Binary collection instead of a Stream/File conduit.", 2);

                                            FLD._max_value = s_n;
                                        }
                                            continue;

                                        case "DAttribute":
                                        {
                                            FLD.notHeaderField();

                                            var attr_args = attr_args_list!.Arguments;
                                            if( attr_args.Count == 0 )
                                                AdHocAgent.exit($"The [Dims] attribute on the field {fld} (line {fld.line_in_src_code}) has no declared dimensions, which is incorrect.", 2);

                                            foreach( var exp in attr_args.Select(arg => arg.Expression) )
                                                if( exp is PrefixUnaryExpressionSyntax _exp ) //read and control of using Dims attribute args
                                                {
                                                    var val = _value_of<uint>(_exp.Operand);

                                                    switch( _exp.ToString()[0] )
                                                    {
                                                        case '-': //A const length dimension
                                                            dims.Add((int)(val << 1));
                                                            continue;
                                                        case '~': //A fixed length dimension, set at field initialization.
                                                            dims.Add((int)(val << 1 | 1));
                                                            continue;
                                                        case '+': //the type size(length) value.
                                                            if( KV == ' ' && (fld.is_Map || fld.is_Set) ) fld._map_set_len = val;
                                                            else if( fld.is_String ) FLD._max_value                        = val; //the char cap rides `max_value`
                                                            else
                                                            {
                                                                AdHocAgent.LOG.Error("The attribute's[D] argument with an prepended `+` character can only be applied to fields of the Set, Map, or string type. However, the field {field}(line:{line}) is not of these types.", FLD, fld.line_in_src_code);
                                                                AdHocAgent.exit("", -1);
                                                            }

                                                            continue;
                                                    }
                                                }
                                                else //[D] attribute argument without a prefix is the array len param
                                                {
                                                    if( FLD!._exT_array == null && fld._map_set_array == null ) //the length of an array without a declared array detected.
                                                    {
                                                        AdHocAgent.LOG.Error("The `[D]` attribute argument `{arg}`, without prefix character, specifies the maximum length of an array. However, the field ‘{field}’ (line:{line}) does not have an array declaration such as ‘[]’, ‘[,]’, or ‘[,,]’ .", exp, FLD, fld.line_in_src_code);
                                                        AdHocAgent.exit("Please specify array type and retry", -1);
                                                    }

                                                    if( FLD._exT_array != null )                         //fully correct using argument
                                                        FLD._exT_array |= _value_of<uint>(exp) << 3;     //take the max length of the array from `exp`
                                                    else                                                 //not fully correct but ok
                                                        FLD._map_set_array |= _value_of<uint>(exp) << 3; //take the max length of the array of Map/Set/Array collection from the `exp`
                                                }

                                            if( KV == ' ' ) // not Key or Val generics
                                            {
                                                switch( dims.Count )
                                                {
                                                    case 0: continue;
                                                    case 1:
                                                        if( (fld.is_Set || fld.is_Map) && fld._map_set_array == null )
                                                        {
                                                            AdHocAgent.LOG.Error("The `[D]` attribute argument `{arg}`, specifies the maximum length of an array of collection. However, the field ‘{field}’ on line {line}’ does not have an array declaration such as ‘[]’, ‘[,]’, or ‘[,,]’ .", attr_args[0], FLD, fld.line_in_src_code);
                                                            AdHocAgent.exit("Please specify array type and retry", -1);
                                                        }

                                                        if( fld._map_set_array != null )                     //fully correct using argument
                                                            fld._map_set_array |= (uint)(dims[0] >> 1 << 3); //take the max length of the array of Map/Set/Array collection from the dims[0]
                                                        else if( fld._map_set_array == null )                //not fully correct but ok, simple flat array
                                                        {
                                                            if( FLD!._exT_array == null )
                                                            {
                                                                AdHocAgent.LOG.Error("The `[D]` attribute argument `{arg}`, specifies the maximum length of an array. However, the field ‘{field}’ on line {line}’ does not have an array declaration such as ‘[]’, ‘[,]’, or ‘[,,]’ .", attr_args[0], FLD, fld.line_in_src_code);
                                                                AdHocAgent.exit("Please specify array type and retry", -1);
                                                            }

                                                            FLD._exT_array |= (uint?)(dims[0] >> 1 << 3); //take the max length of the array from dims[0]
                                                        }

                                                        dims.Clear();

                                                        continue;
                                                }

                                                FLD!.dims          = dims.ToArray(); //Multidimensional array
                                                fld._map_set_array = null;           //Cancel the array of Map/Set/Array collections. it cannot coexist with Multidimensional array.
                                                dims.Clear();
                                            }
                                            else if( 0 < dims.Count ) // in dims cannot be apply to the Key or Val generics params
                                            {
                                                AdHocAgent.LOG.Error("The attribute’s [D] arguments for Key or Value types cannot be multi-dimensional, meaning they cannot have a prepended - or ~ character. However, the field {field} on line {line} does not meet this requirement", FLD, fld.line_in_src_code);
                                                AdHocAgent.exit("", -1);
                                            }
                                        }
                                            continue;

                                        case "MinMaxAttribute":
                                        {
                                            FLD.check_MinMax_using();

                                            if( FLD.exT_primitive is (int)Project.Host.Pack.Field.DataType.t_bool or <= (int)Project.Host.Pack.Field.DataType.t_string )
                                            {
                                                AdHocAgent.LOG.Error("The '{attribute}' attribute cannot be applied to the '{field}' field (line {line}) because its type does not support this attribute.", FLD, name, fld.line_in_src_code);
                                                AdHocAgent.exit("", -1);
                                            }

                                            var attr_args = attr_args_list!.Arguments;
                                            if( FLD.exT_primitive is (int)Project.Host.Pack.Field.DataType.t_float or (int)Project.Host.Pack.Field.DataType.t_double )
                                            {
                                                FLD._min_valueD = (double?)FLD.value_of(attr_args[0].Expression);
                                                FLD._max_valueD = (double?)FLD.value_of(attr_args[1].Expression);

                                                if( FLD._max_valueD < FLD._min_valueD ) (FLD._min_valueD, FLD._max_valueD) = (FLD._max_valueD, FLD._min_valueD);

                                                //`inT` тоже: понижение меняет и внутренний тип. Иначе у `[MinMax] double`, влезающего
                                                //во float, exT_primitive становится float, а inT остаётся double — и `zero` печатает
                                                //`public float minmax_double = (double) 0;`, что не компилируется.
                                                FLD.exT_primitive = FLD.inT = FLD._min_valueD < float.MinValue || float.MaxValue < FLD._max_valueD ?
                                                                        (int)Project.Host.Pack.Field.DataType.t_double :
                                                                        (int)Project.Host.Pack.Field.DataType.t_float;
                                            }
                                            else
                                                setByRange(FLD, big_int_value_of(attr_args[0].Expression), big_int_value_of(attr_args[1].Expression));
                                        }
                                            continue;
                                        case "AAttribute":
                                            check_varinT_target(FLD, "A");
                                            FLD.notHeaderField();
                                            FLD.check_MinMax_using();
                                            FLD._dir = 1;

                                            if( attr_args_list == null ||
                                                attr_args_list.Arguments.Count == 1 &&
                                                (
                                                    attr_args_list.Arguments[0].NameColon == null ||
                                                    attr_args_list.Arguments[0].NameColon!.Name.ToString().Equals("minMostProbableValue") // the parameter name declared on AAttribute in Meta.cs
                                                    //Max is omitted and determined by the applied type.
                                                )
                                              )
                                            {
                                                var most_probable_value = attr_args_list == null ?
                                                                              0 :
                                                                              big_int_value_of(attr_args_list.Arguments[0].Expression);

                                                switch( FLD.exT_primitive )
                                                {
                                                    case (int)Project.Host.Pack.Field.DataType.t_int16:
                                                        FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                                        FLD._max_value    = short.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint16:
                                                    case (int)Project.Host.Pack.Field.DataType.t_char:
                                                        FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                                        FLD._max_value    = ushort.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_int32:
                                                        FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint32;
                                                        FLD._max_value    = int.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint32:
                                                        FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint32;
                                                        FLD._max_value    = uint.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_int64:
                                                        FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                                        FLD._max_value    = long.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint64:
                                                        FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                                        FLD._max_value    = -1L; // ulong.MaxValue;
                                                        break;
                                                }

                                                if( attr_args_list == null ) FLD._min_value = 0;
                                                else
                                                    setByRange(FLD, most_probable_value, (FLD._max_value == -1L ?
                                                                                              ulong.MaxValue :
                                                                                              (ulong)FLD._max_value!.Value) + most_probable_value);
                                            }
                                            else if( attr_args_list.Arguments.Count == 1 ) //the single argument is named max: and minMostProbableValue stays 0
                                                setByRange(FLD,
                                                           0,
                                                           big_int_value_of(attr_args_list.Arguments[0].Expression));
                                            else //two arguments
                                                setByRange(FLD,
                                                           big_int_value_of(attr_args_list.Arguments[0].Expression),
                                                           big_int_value_of(attr_args_list.Arguments[1].Expression));

                                            check_is_varinTable(FLD);
                                            break;
                                        case "VAttribute":
                                            check_varinT_target(FLD, "V");
                                            FLD.notHeaderField();
                                            FLD.check_MinMax_using();
                                            FLD._dir = -1;

                                            if( attr_args_list == null ||
                                                attr_args_list.Arguments.Count == 1 &&
                                                (
                                                    attr_args_list.Arguments[0].NameColon == null ||
                                                    attr_args_list.Arguments[0].NameColon!.Name.ToString().Equals("maxMostProbableValue") // the parameter name declared on VAttribute in Meta.cs
                                                    //Min is omitted and determined by the applied type.
                                                )
                                              )
                                            {
                                                var most_probable_value = attr_args_list == null ?
                                                                              0 :
                                                                              big_int_value_of(attr_args_list.Arguments[0].Expression);
                                                switch( FLD.exT_primitive )
                                                {
                                                    case (int)Project.Host.Pack.Field.DataType.t_int16:
                                                        FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int16;
                                                        FLD.inT           = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                                        FLD._min_value    = short.MinValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint16:
                                                    case (int)Project.Host.Pack.Field.DataType.t_char:
                                                        FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int32;
                                                        FLD.inT           = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                                        FLD._min_value    = -ushort.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_int32:
                                                        FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int32;
                                                        FLD.inT           = (int)Project.Host.Pack.Field.DataType.t_uint32;
                                                        FLD._min_value    = int.MinValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint32:
                                                        FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int64;
                                                        FLD.inT           = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                                        FLD._min_value    = -uint.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_int64:
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint64:
                                                        FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int64;
                                                        FLD.inT           = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                                        FLD._min_value    = long.MinValue;
                                                        break;
                                                }

                                                if( attr_args_list == null ) FLD._max_value = 0;
                                                else
                                                    setByRange(FLD, most_probable_value + FLD._min_value!.Value, most_probable_value);
                                            }
                                            else if( attr_args_list.Arguments.Count == 1 ) //the single argument is named min: and maxMostProbableValue stays 0
                                                setByRange(FLD,
                                                           big_int_value_of(attr_args_list.Arguments[0].Expression),
                                                           0);
                                            else //two arguments
                                                setByRange(FLD,
                                                           big_int_value_of(attr_args_list.Arguments[1].Expression),
                                                           big_int_value_of(attr_args_list.Arguments[0].Expression));


                                            check_is_varinTable(FLD);
                                            break;
                                        case "XAttribute":
                                            check_varinT_target(FLD, "X");
                                            FLD.notHeaderField();
                                            FLD.check_MinMax_using();
                                            FLD._dir = 0;

                                            if( attr_args_list == null ||
                                                attr_args_list.Arguments.Count        == 1    &&
                                                attr_args_list.Arguments[0].NameColon != null &&
                                                attr_args_list.Arguments[0].NameColon!.Name.ToString().Equals("zero") // the parameter name declared on XAttribute in Meta.cs
                                              )
                                                //Amplitude is omitted and determined by the applied type.
                                            {
                                                long min;
                                                long max;

                                                switch( FLD.exT_primitive )
                                                {
                                                    case (int)Project.Host.Pack.Field.DataType.t_int16:
                                                        if( attr_args_list == null )
                                                        {
                                                            FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_int16;
                                                            goto chk;
                                                        }

                                                        min = -short.MaxValue;
                                                        max = short.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_char:
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint16:

                                                        min            = -ushort.MaxValue;
                                                        FLD._max_value = max = ushort.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_int32:
                                                        if( attr_args_list == null )
                                                        {
                                                            FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_int32;
                                                            goto chk;
                                                        }

                                                        min = -int.MaxValue;
                                                        max = int.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint32:
                                                        min            = -uint.MaxValue;
                                                        FLD._max_value = max = uint.MaxValue;
                                                        break;
                                                    default:
                                                        if( attr_args_list == null )
                                                        {
                                                            FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_int64;
                                                            goto chk;
                                                        }

                                                        min = -long.MaxValue;
                                                        max = long.MaxValue;
                                                        break;
                                                }

                                                FLD.set_exT_ByRange(min, max); //counting inT
                                                FLD.inT = FLD.exT_primitive;   //signed

                                                var zero = (long)(FLD._min_value = attr_args_list == null ?
                                                                                       0L :
                                                                                       _value_of<long>(attr_args_list.Arguments[0].Expression));
                                                FLD.set_exT_ByRange(min + zero, max + zero);
                                            }
                                            else
                                            {
                                                var zero      = 0L;
                                                var amplitude = 0L;

                                                if( attr_args_list.Arguments[0].NameColon == null || attr_args_list.Arguments[0].NameColon!.Name.ToString().Equals("amplitude") ) // the parameter name declared on XAttribute in Meta.cs
                                                {
                                                    amplitude = _value_of<long>(attr_args_list.Arguments[0].Expression);
                                                    if( 1 < attr_args_list.Arguments.Count )
                                                        zero = _value_of<long>(attr_args_list.Arguments[1].Expression);
                                                }
                                                else
                                                {
                                                    zero      = _value_of<long>(attr_args_list.Arguments[0].Expression);
                                                    amplitude = _value_of<long>(attr_args_list.Arguments[1].Expression);
                                                }

                                                FLD._max_value = amplitude;
                                                FLD._min_value = zero;

                                                FLD.set_exT_ByRange(-amplitude, amplitude); //counting inT
                                                FLD.inT = FLD.exT_primitive;                //signed

                                                FLD.set_exT_ByRange(zero - amplitude, zero + amplitude);
                                            }

                                            chk:
                                            check_is_varinTable(FLD);
                                            break;
                                    }

                                    continue;

                                    // Called after the attribute's arguments have been applied: they define the span that actually
                                    // gets varint-encoded - `val - min` for [A], `max - val` for [V], ZigZag(`val - zero`) for [X].
                                    // `inT` measures that span (`set_inT_ByRange` yields uint8, or a bits field, exactly when it
                                    // fits in one byte); `exT` must NOT be used here - it also carries the base offset and stays
                                    // wide, so `[A(1000, 1100)] int` kept an uint16 `exT` for a span of 100 and slipped through.
                                    // A span of one byte has no leading zero septets to skip: that range belongs to [MinMax].
                                    void check_is_varinTable(FieldImpl FLD)
                                    {
                                        if( (int)Project.Host.Pack.Field.DataType.t_float < FLD.inT && FLD.inT < (int)Project.Host.Pack.Field.DataType.t_uint8 ) return;
                                        AdHocAgent.exit($"The varint attribute on the field '{FLD.symbol}' (line: {FLD.line_in_src_code}) declares a value span that fits in a single byte, so varint has no leading zero bytes to skip and cannot compress anything. Use `[MinMax(min, max)]` for a range this narrow — it transmits the same compact field without the varint machinery.", 2);
                                    }

                                    // Varint skips leading zero bytes, so [A]/[V]/[X] apply only to an integer whose INTERNAL type
                                    // `inT` - the one that actually goes on the wire - spans more than one byte:
                                    // int16 / uint16 / char / int32 / uint32 / int64 / uint64.
                                    // Verified before the attribute body runs: the body rewrites `exT_primitive` through
                                    // `set_exT_ByRange`, so the trailing `check_is_varinTable` inspects the substituted type, not the
                                    // declared one - `[X] string s;` used to pass it as int64, and `[A(5)] string s;` /
                                    // `[V(5)] string s;` died on a null `_max_value` / `_min_value` before reaching it.
                                    // `check_is_varinTable` stays where it is: it catches the range being narrowed below 2 bytes by
                                    // the attribute's own arguments.
                                    void check_varinT_target(FieldImpl FLD, string attr)
                                    {
                                        if( (int)Project.Host.Pack.Field.DataType.t_float < FLD.inT && FLD.inT < (int)Project.Host.Pack.Field.DataType.t_uint8 ) return;

                                        AdHocAgent.exit($"The varint attribute `[{attr}]` on the field '{FLD.symbol}' (line: {FLD.line_in_src_code}) " +
                                                        (FLD.inT == null ?
                                                             $"is applied to the non-primitive datatype `{FLD.exT_pack}`. If that datatype is a TYPEDEF, declare `[{attr}]` on the TYPEDEF field itself — the typedef pass propagates `inT`, `dir`, `min_value` and `max_value` to every field that uses it." :
                                                             FLD.inT is (int)Project.Host.Pack.Field.DataType.t_bool or (int)Project.Host.Pack.Field.DataType.t_int8 or (int)Project.Host.Pack.Field.DataType.t_uint8 ?
                                                                 "is applied to a field whose internal type spans a single byte, so varint has no leading zero bytes to skip. Widen the integer type, relax the [MinMax] range, or drop the attribute." :
                                                                 "is applied to a non-integer field. Varint compression is available only when the internal type is int16, uint16, char, int32, uint32, int64 or uint64 — not for bool, int8/uint8, float/double, string, Binary, DateTime or Stream/File."), 2);
                                    }
                                }
                            }
                        }
#endregion
                    }

                    /// <summary>
                    /// Sets the min and max value and updates the external and internal types based on the range.
                    /// </summary>
                    /// <param name="fld">The FieldImpl instance to configure.</param>
                    /// <param name="min">The minimum value.</param>
                    /// <param name="max">The maximum value.</param>
                    static void setByRange(FieldImpl fld, BigInteger min, BigInteger max)
                    {
                        if( max < min ) (max, min) = (min, max); //swap

                        fld._min_value = (long)min;
                        try { fld._max_value = (long)max; }
                        catch( Exception ) { fld._max_value = (long)(ulong)max; }

                        fld.set_exT_ByRange(min, max);
                        fld.set_inT_ByRange(min, max);
                    }
                }

                public PackImpl ValidateTypeUsage(int line)
                {
                    // 1. Identify the specific category of the "Wrong" type
                    string? category = null;

                    if( is_DateTimeDef ) category           = "DateTimeDef (Time Metadata)";
                    else if( is_TimeSpanDef ) category      = "TimeSpanDef (Time Metadata)";
                    else if( is_Duration ) category         = "Duration (Time Metadata)";
                    else if( is_typedef ) category          = "TYPEDEF (Type Alias)";
                    else if( is_FieldsInjectInto ) category = "FieldsInjectInto (Template)";
                    else if( is_Header ) category           = "Header (Packet Metadata)";
                    else if( is_constants_set ) category    = "Constants Set";
                    else if( is_enum ) category             = "Enum";
                    else if( is_Modifier ) category         = "Modifier (Meta-Logic)";

                    // 2. If it's not any of the above, it's a valid Transmittable Pack
                    if( category == null ) return this;

                    AdHocAgent.exit(
                                    $"Error at line {line}: Invalid pack '{symbol}' .\n"                                                           +
                                    $"  Type '{symbol}' is a {category} and not a transmittable message type.\n"                                   +
                                    $"  {category} types can only be used as field types within Pack declaration, not as standalone payloads.\n\n" +
                                    $"  Correct usage: Define a Pack with a field of this type, then use that Pack"
                                   );
                    return this;
                }
            }
        }

        /// <summary>
        /// A multiplexer: <c>interface Name : Multiplex&lt;(C1, C2, …)&gt;</c>. Its connections share exactly one host,
        /// which serves them all on one port and switches between them. It is a connection-level entity with the
        /// same placement rules as a connection.
        /// <para>
        /// On the wire it carries host indices: the shared host first, then the host on the far end of each
        /// connection, in the order the connections are listed.
        /// </para>
        /// </summary>
        public class MultiplexImpl : Entity, Project.Multiplex{
            /// <summary>The multiplexed connections, in declaration order.</summary>
            public readonly List<ConnectionImpl> connections = [];

            /// <summary>The shared host first, then the far-end host of each connection in <see cref="connections"/>.</summary>
            public readonly List<HostImpl> hosts = [];

            public MultiplexImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax node) : base(project, compilation, node)
            {
                project.multiplexes.Add(this);
                if( parent_by_source_code is not ProjectImpl ) AdHocAgent.exit($"The definition of the multiplexer {symbol} (line {line_in_src_code}) should be placed directly within the project’s scope.");
            }

            /// <summary>
            /// Collects the connections of the <c>CONNECTIONS</c> tuple, validates them, finds the shared host and
            /// fills <see cref="hosts"/>. Runs after every connection has resolved <c>hostL</c>/<c>hostR</c>.
            /// </summary>
            public void resolve()
            {
                connections.Clear();
                hosts.Clear();

                var I = symbol!.Interfaces.First(i => equals(i.OriginalDefinition, Meta_Multiplex));

                if( I.TypeArguments[0] is not INamedTypeSymbol { IsTupleType: true } tuple || tuple.TupleElements.Length < 2 )
                {
                    AdHocAgent.exit($"The multiplexer {symbol} (line {line_in_src_code}) should list at least two connections as a tuple: `Multiplex<(ConnectionA, ConnectionB)>`. A single connection has nothing to switch between.");
                    return;
                }

                foreach( var type in tuple.TupleElements.Select(e => e.Type) )
                {
                    if( !entities.TryGetValue(type, out var e) || e is not ConnectionImpl conn || conn.is_Modifier )
                    {
                        AdHocAgent.exit($"The multiplexer {symbol} (line {line_in_src_code}) lists {type}, which is not a connection. Only connections declared with `Connects<HostA, HostB>` can be multiplexed.");
                        return;
                    }

                    if( conn.is_virtual )
                        AdHocAgent.exit($"The multiplexer {symbol} (line {line_in_src_code}) lists the virtual connection {conn.symbol}. A virtual connection rides a tunnel through relay hosts and never arrives on a port of its own, so it cannot be multiplexed.");

                    if( connections.Contains(conn) )
                        AdHocAgent.exit($"The multiplexer {symbol} (line {line_in_src_code}) lists the connection {conn.symbol} more than once.");

                    connections.Add(conn);
                }

                string joins() => string.Join(", ", connections.Select(c => $"{c.symbol} ({c.hostL!.symbol} ↔ {c.hostR!.symbol})"));

                // The shared host - the one that listens on the port - is a party of more of the connections than any other host.
                // A connection it is not a party of is RELAYED: it takes the socket of the connection's host that dials the port
                // and carries the bytes in a tunnel over the physical connection that joins it with the other host - the carrier.
                var shared = connections.SelectMany(c => new[] { c.hostL!, c.hostR! })
                                        .GroupBy(h => h)
                                        .Select(g => (host: g.Key, count: g.Count()))
                                        .OrderByDescending(p => p.count)
                                        .ToList();

                if( shared[0].count < 2 || 1 < shared.Count && shared[1].count == shared[0].count )
                    AdHocAgent.exit($"The connections of the multiplexer {symbol} (line {line_in_src_code}) should share one host — the one that serves them all on one port, a party of at least two of them and of more than any other host. They join: {joins()}.");

                var common = shared[0].host;
                hosts.Add(common);

                foreach( var c in connections )
                    if( c.hostL == common || c.hostR == common ) hosts.Add(c.hostL == common ? c.hostR! : c.hostL!);
                    else
                    {
                        // the carriers: the physical connections joining the shared host with either end of the relayed one
                        var carriers = new[] { c.hostL!, c.hostR! }.Select(end => (end, carrier: project.connections.FirstOrDefault(x => !x.is_virtual && !x.is_Modifier && (x.hostL == common && x.hostR == end || x.hostR == common && x.hostL == end))))
                                                                  .Where(p => p.carrier != null)
                                                                  .ToList();
                        if( carriers.Count != 1 )
                            AdHocAgent.exit(carriers.Count == 0 ?
                                                $"The multiplexer {symbol} (line {line_in_src_code}) lists {c.symbol} ({c.hostL!.symbol} ↔ {c.hostR!.symbol}), which {common.symbol}, the host its port is on, is not a party of. {common.symbol} can relay it only in a tunnel over a physical connection with one of its hosts, and there is none. Declare one, or take {c.symbol} out of the multiplexer." :
                                                $"The multiplexer {symbol} (line {line_in_src_code}) lists {c.symbol} ({c.hostL!.symbol} ↔ {c.hostR!.symbol}), which {common.symbol}, the host its port is on, is not a party of, and both its hosts have a physical connection with {common.symbol}: {carriers[0].carrier!.symbol}, {carriers[1].carrier!.symbol}. It is not clear which of them dials the port. Take {c.symbol} out of the multiplexer.");

                        if( c.relay_carrier != null && c.relay_carrier != carriers[0].carrier )
                            AdHocAgent.exit($"The connection {c.symbol} is relayed by more than one multiplexer.");

                        c.relay_carrier = carriers[0].carrier;
                        hosts.Add(c.hostL == carriers[0].end ? c.hostR! : c.hostL!); // the host that dials the port: the other end
                    }

                // the byte a socket opens with is the uid of the host that dials: one host, one connection on the port
                foreach( var twice in hosts.Skip(1).Select((h, i) => (h, c: connections[i])).GroupBy(p => p.h).Where(g => 1 < g.Count()) )
                    AdHocAgent.exit($"The host {twice.Key.symbol} dials the multiplexer {symbol} (line {line_in_src_code}) for more than one connection: {string.Join(", ", twice.Select(p => p.c.symbol))}. A socket names its connection by the uid of the host that dials, so a host has one connection on a port.");

                AdHocAgent.LOG.Information("multiplexer {multiplexer}: {host} switches {connections}", symbol, hosts[0].symbol,
                                           string.Join(", ", connections.Select((c, i) => c.relay_carrier == null ?
                                                                                              $"{c.symbol} ↔ {hosts[i + 1].symbol}" :
                                                                                              $"{c.symbol} ↔ {hosts[i + 1].symbol}, relayed over {c.relay_carrier.symbol}")));
            }

            /// <summary>
            /// A connection arrives on exactly one port, so it may belong to at most one multiplexer. Two multiplexers
            /// over the same set of connections are the most blatant case and are reported as such.
            /// </summary>
            public static void check_shared_connections(List<MultiplexImpl> multiplexes)
            {
                var failed = false;

                foreach( var same in multiplexes.GroupBy(m => string.Join(",", m.connections.Select(c => c._name).Order()))
                                                .Where(g => 1 < g.Count()) )
                {
                    failed = true;
                    AdHocAgent.LOG.Error("The multiplexers {multiplexers} multiplex the same set of connections. Keep one of them.",
                                         string.Join(", ", same.Select(m => $"{m.symbol} (line {m.line_in_src_code})")));
                }

                if( failed ) AdHocAgent.exit("Fix the problem and restart");

                foreach( var shared in multiplexes.SelectMany(m => m.connections.Select(c => (c, m)))
                                                  .GroupBy(p => p.c)
                                                  .Where(g => 1 < g.Count()) )
                {
                    failed = true;
                    AdHocAgent.LOG.Error("The connection {connection} is multiplexed by more than one multiplexer: {multiplexers}. A connection arrives on exactly one port, so it may belong to at most one multiplexer.",
                                         shared.Key.symbol,
                                         string.Join(", ", shared.Select(p => $"{p.m.symbol} (line {p.m.line_in_src_code})")));
                }

                if( failed ) AdHocAgent.exit("Fix the problem and restart");
            }

            public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once) { } // a multiplexer owns no packs

            public object? _hosts()    => hosts;
            public int     _hosts_len => hosts.Count;
            //the window of the transmit buffer this portion fills: host indexes from `item` on
            public void _hosts(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item, Span<byte> dst)
            {
                for( var i = 0; i < dst.Length; i++ ) dst[i] = (byte)hosts[item + i].idx;
            }
        }

        /// <summary>
        /// Represents the implementation of a Connection entity within a Project.
        /// Connections define communication pathways between Hosts.
        /// </summary>
        public class ConnectionImpl : Entity, Project.Connection{
            public byte _uid => (byte)uid;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConnectionImpl"/> class.
            /// </summary>
            /// <param name="project">The project this connection belongs to.</param>
            /// <param name="compilation">The Roslyn compilation object.</param>
            /// <param name="Connection">The InterfaceDeclarationSyntax node representing the connection interface.</param>
            public ConnectionImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax Connection) : base(project, compilation, Connection) //struct based
            {
                project.connections.Add(this);
                if( parent_by_source_code is not ProjectImpl ) AdHocAgent.exit($"The definition of the connection {symbol} (line {line_in_src_code}) should be placed directly within the project’s scope.");

                if( actors.Count == 0 ) actors.Add(new ActorImpl(project, compilation, Connection, true) { _MaxActiveInstances = 1 }); //default Actor0 placeholder


                var interfaces = symbol!.Interfaces;
                if( 0 < interfaces.Length )
                    switch( interfaces[0].OriginalDefinition )
                    {
                        case var def when equals(def, Meta_Connects):
                            // A host may connect to itself: peers of one kind (broker to broker, controller to controller).
                            // Every socket still has a caller (L, the side that dialed) and an acceptor (R), so the state
                            // machines, the RPC roles and the header directions keep their meaning; the host simply holds
                            // both sides of the connection.
                            return;

                        case var def when equals(def, Meta_VirtuallyConnects):
                            read_virtual_connection_limits();
                            if( !equals(symbol!.Interfaces[0].TypeArguments[0], symbol!.Interfaces[0].TypeArguments[1]) ) return;
                            AdHocAgent.LOG.Error("The virtual connection {conn} (line {line}) should connect two distinct hosts.", symbol, line_in_src_code);
                            AdHocAgent.exit("Fix the problem and restart");
                            return;

                        case var def when equals(def, Meta_Modify_Connection):
                            if( symbol!.Interfaces[0].TypeArguments[0] is INamedTypeSymbol sym && sym.TypeKind == TypeKind.Interface ) return; //minimal test
                            AdHocAgent.LOG.Error("The connection {connection} (line {line}) can Modify other connections only. But {Modify} is not", symbol, line_in_src_code, symbol!.Interfaces[0].TypeArguments[0]);
                            AdHocAgent.exit("Fix the problem and restart");

                            return;
                    }

                AdHocAgent.LOG.Error("The connection {connection} (line {line}) should `implements` the {Connects}, {VirtuallyConnects}, or {Modify}  interfaces.", symbol, line_in_src_code, "org.unirail.Meta.Connects<HostA,HostB>", "org.unirail.Meta.VirtuallyConnects<HostA,HostB,PATH>", "org.unirail.Meta.Modify<ModifyActor>");
                AdHocAgent.exit("Fix the problem and restart");
            }

            /// <summary>
            /// Reads the <c>VirtuallyConnects</c> knobs — <see cref="_MaxTunnels"/> and
            /// <see cref="_MaxStream_KiloBytes"/> — from this connection's interface body. Each default
            /// interface method (<c>MaxTunnels =&gt; 1</c>, <c>MaxStream_KiloBytes =&gt; uint.MaxValue</c>) may be
            /// overridden by re-declaring the property in the connection interface; when overridden, its
            /// compile-time constant value is taken, otherwise the property keeps its initializer default
            /// (which mirrors the <c>VirtuallyConnects</c> default). Mirrors the
            /// <see cref="ActorImpl"/> <c>MaxActiveInstances</c> reading pattern.
            /// </summary>
            void read_virtual_connection_limits()
            {
                if( node is not InterfaceDeclarationSyntax iface ) return;

                foreach( var prop in iface.Members.OfType<PropertyDeclarationSyntax>() )
                {
                    var expr = prop.ExpressionBody?.Expression ?? prop.Initializer?.Value;
                    if( expr == null || model.GetConstantValue(expr).Value is not { } val ) continue;

                    switch( prop.Identifier.ValueText )
                    {
                        case "MaxTunnels":
                            _MaxTunnels = Convert.ToInt32(val);
                            if( _MaxTunnels < 1 )
                            {
                                AdHocAgent.LOG.Warning("The virtual connection {conn} (line {line}) sets MaxTunnels to {value}; MaxTunnels must be a positive value (>= 1).", symbol, line_in_src_code, _MaxTunnels);
                                AdHocAgent.exit("Fix the problem and restart");
                            }

                            break;
                        case "MaxStream_KiloBytes":
                            _MaxStream_KiloBytes = Convert.ToUInt32(val);
                            break;
                    }
                }
            }

            public override bool included => _included ?? in_project.included;

            /// <summary>
            /// Host entity on the left side of the connection.
            /// </summary>
            public HostImpl? hostL;

            public byte _hostL => (byte)hostL.idx;


            /// <summary>
            /// Host entity on the right side of the connection.
            /// </summary>
            public HostImpl? hostR;

            public byte _hostR => (byte)hostR!.idx;

            /// <summary>
            /// True if this connection was declared via VirtuallyConnects&lt;L,R,PATH&gt; — i.e. a logical
            /// connection tunneled across one or more physical Connects&lt;&gt; hops listed in <see cref="viaPath"/>.
            /// </summary>
            public bool is_virtual => symbol != null && symbol.Interfaces.Any(I => equals(I.OriginalDefinition, Meta_VirtuallyConnects));

            /// <summary>
            /// Persistent unique id allocated for VirtuallyConnects from the same pool as transmittable packs.
            /// Defaults to the <c>t_subpack</c> sentinel (= "not yet assigned"); the renumbering pass in
            /// <see cref="ProjectImpl.read_packs_id_info_and_write_update"/> replaces it with the smallest
            /// unused id and persists it in the source-file Dashboard alongside transmittable packs.
            /// Has no meaning on a physical Connects&lt;&gt; connection — it stays at the sentinel value.
            /// </summary>
            public ushort _id { get; set; } = (int)Project.Host.Pack.Field.DataType.t_subpack;

            /// <summary>
            /// A physical connection a multiplexer relays (its shared host is not a party of it): the physical connection that
            /// carries its bytes in a tunnel - between the shared host and this connection's host that does not dial the port.
            /// Such a connection takes a wire id (<see cref="_id"/>) for its tunnel on the carrier, like a virtual one. null - not relayed.
            /// </summary>
            public ConnectionImpl? relay_carrier;

            /// <summary>A virtual connection, or a relayed one: its tunnel has a wire id from the packs' pool.</summary>
            public bool is_tunnelled => is_virtual || relay_carrier != null;

            /// <summary>
            /// Stream transform-chain reference: the idx of the shared t_constants/referred container that
            /// holds this connection's stage chain (compression / cipher), or <see cref="ushort.MaxValue"/>
            /// when the connection carries no chain. Populated by <see cref="HasDocs.collect_stream_stages"/> exactly
            /// like <see cref="HostImpl.PackImpl._link"/>. On a virtual connection the chain rides the chunked tunnel
            /// end-to-end through PATH; on a physical Connects&lt;&gt; it wraps the bytes of that single hop.
            /// </summary>
            public ushort _link { get; set; } = ushort.MaxValue;

            internal int[] viaPath = [];

            public object? _viaPath() => viaPath.Length == 0 ?
                                             null :
                                             viaPath;

            public int _viaPath_len                                                           => viaPath.Length;
            public int _viaPath(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item) => viaPath[item];

            public int  _MaxTunnels          { get; set; } = 256;           // VirtuallyConnects.MaxTunnels default (one-to-one; physical Connects<> keep this)
            public uint _MaxStream_KiloBytes { get; set; } = uint.MaxValue; // VirtuallyConnects.MaxStream_KiloBytes default (unbounded)

            public override bool Init_As_Modifier_Dispatch_Modifications_On_Targets(HashSet<object> once)
            {
                foreach( var modified_connection in this_modify.Select(s => s.TypeArguments[0]) ) //normally only one modified connection
                    foreach( var state in symbol!.GetTypeMembers() )                              //states declared in this connection body
                        foreach( var by_state_modefied_entity in state.Interfaces.Where(I => I.isModify()).Select(I => I.TypeArguments[0]) )
                            if( !equals(modified_connection, by_state_modefied_entity) && !equals(modified_connection, by_state_modefied_entity.OriginalDefinition.ContainingType) )
                                AdHocAgent.LOG.Warning("State {state} (line: {line}) in connection {symbol}, modifying Actor {conn}, is attempting to modify entity <{modefied}> from a different connection. This is likely an error.", state, entities[state].line_in_src_code, symbol, modified_connection, by_state_modefied_entity);

                foreach( var I in this_modify )
                {
                    is_Modifier = true;
                    var target_connection = (ConnectionImpl)entities[I.TypeArguments[0]];

                    if( I.TypeArguments.Length == 3 ) // Modify<TargetConnection, HostA, HostB>
                    {
                        target_connection.hostL = (HostImpl)entities[I.TypeArguments[1]];
                        target_connection.hostR = (HostImpl)entities[I.TypeArguments[2]];
                    }

                    Adopt_Attributes_Onto(target_connection);
                    Init_Collect_Modification(target_connection, once);
                    return true;
                }


                foreach( var state in symbol!.GetTypeMembers().SelectMany(s => s.Interfaces).Where(I => I.isModify()) )
                {
                    //      interface ModifyConnection  {
                    //          interface ModifyState Modify<ModifiedState>,
                    //            _<(
                    //                Server.Info,
                    //                AuthorisationRequest
                    //            )>{ }
                    //
                    //  case
                    is_Modifier = true;
                    var by_state_target = entities[state.TypeArguments[0].OriginalDefinition.ContainingType];
                    Adopt_Attributes_Onto(by_state_target);
                    Init_Collect_Modification(by_state_target, once);
                    return true;
                }

                var i = symbol!.Interfaces.First(I => SymbolEqualityComparer.Default.Equals(I.OriginalDefinition, Meta_Connects) ||
                                                      SymbolEqualityComparer.Default.Equals(I.OriginalDefinition, Meta_VirtuallyConnects));
                hostL = (HostImpl)entities[i.TypeArguments[0]];
                hostR = (HostImpl)entities[i.TypeArguments[1]];

                // Virtual connections are pure routing metadata — they may legitimately carry no actors or packs.
                if( !is_virtual && !actors.SelectMany(actor => actor.states).Any() ) AdHocAgent.exit($"Connection {symbol} (line {line_in_src_code}) does not have any actor with any states. Please add a state and restart.");
                return false;
            }

            /// <summary>
            /// Applies a state modification to this connection, adding or removing a state.
            /// </summary>
            /// <param name="state">The state to apply.</param>
            /// <param name="add">True to add the state, false to remove it.</param>
            /// <param name="SwapHosts">True if the hosts in the cloned state branches should be swapped (for LR connection modifications).</param>
            void apply(StateImpl state, bool add, bool SwapHosts)
            {
                var i = actors[0].states.FindIndex(st => equals(st.symbol, state.symbol));

                if( i == -1 )
                {
                    if( !add ) return;

                    var cloned = state.clone();
                    if( SwapHosts )
                        (cloned.branchesL, cloned.branchesR) = (cloned.branchesR, cloned.branchesL);

                    actors[0].states.Add(cloned);
                }
                else if( !add ) actors[0].states.RemoveAt(i);
            }

            public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once)
            {
                bool SwapHosts;

                if( entities.TryGetValue((SwapHosts = by_what.Name.Equals("SwapHosts")) && by_what.isMeta() ?
                                             ((INamedTypeSymbol)by_what).TypeArguments[0] :
                                             by_what, out var value) )
                    switch( value )
                    {
                        case ConnectionImpl connection:
                            connection.Init(once);

                            connection.actors[0].states.ForEach(state => apply(state, add, SwapHosts));
                            break;
                        case StateImpl state:
                            apply(state, add, SwapHosts);
                            break;
                    }
            }


            /// <summary>
            /// Represents a named set of packs, grouped under an interface declaration with the '_' interface name.
            /// </summary>
            public class NamedPackSet : Entity{
                /// <summary>
                /// HashSet to store packs belonging to this named pack set.
                /// </summary>
                public HashSet<HostImpl.PackImpl> packs = [];

                /// <summary>
                /// Initializes a new instance of the <see cref="NamedPackSet"/> class.
                /// </summary>
                /// <param name="project">The project this named pack set belongs to.</param>
                /// <param name="compilation">The Roslyn compilation object.</param>
                /// <param name="node">The InterfaceDeclarationSyntax node representing the named pack set interface.</param>
                internal NamedPackSet(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax node) : base(project, compilation, node) { }

                /// <summary>
                /// Initializes the Pack Set and applies the [Filter] attributes if present.
                /// </summary>
                public override void Init(HashSet<object> once)
                {
                    // 1. Standard initialization: This resolves all _<(...)> and X<(...)> groupings
                    // and populates the 'packs' collection.
                    if( inited || !once.Add(this) && (cyclic = true) ) return;

                    if( symbol != null )
                        if( !Init_As_Modifier_Dispatch_Modifications_On_Targets(once) )
                            Init_Collect_Modification(this, once);

                    if( symbol != null ) { FilterPacks(packs, symbol); }

                    once.Remove(this);
                    set_inited();
                }

                public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once) => collect_packs_in_scope(by_what, add, depth, (pack, _add) =>
                                                                                                                                                        {
                                                                                                                                                            if( _add ) packs.Add(pack);
                                                                                                                                                            else packs.Remove(pack);
                                                                                                                                                        }, once);

                // Method to collect packs within a given scope and apply an action to them
                // Parameters:
                // - scope: The symbol representing the scope to search for packs
                // - add: Boolean indicating whether to add or remove packs
                // - depth: The depth of the scope traversal
                // - apply: An action delegate to apply to each pack found, taking a PackImpl and the 'add' boolean
                // - once: the recursion guard of the Init in progress, if any - a nested Pack Set is built with it
                public static void collect_packs_in_scope(ISymbol scope, bool add, uint depth, Action<HostImpl.PackImpl, bool> apply, HashSet<object>? once = null)
                {
                    if( named_packs.TryGetValue(scope, out var nps) )
                    {
                        // A Pack Set may name another one declared after it: build that one first, or its packs are not there yet.
                        // The guard is shared when there is one, so a cycle between sets is caught as any cyclic Init is.
                        if( !nps.inited ) nps.Init(once ?? []);
                        foreach( var pack in nps.packs ) pack.for_packs_in_scope(depth, p => apply(p, add));
                        return;
                    }

                    // NEW: Detect bare (uninstantiated) filter templates used as pack sets
                    if( scope is INamedTypeSymbol nts && nts.TypeParameters.Length == 1 )
                    {
                        var hasFilterAttr = nts.GetAttributes().Any(a =>
                                                                        equals(a.AttributeClass, ProjectImpl.Meta_KeepName) ||
                                                                        equals(a.AttributeClass, ProjectImpl.Meta_KeepDoc)  ||
                                                                        equals(a.AttributeClass, ProjectImpl.Meta_SkipName) ||
                                                                        equals(a.AttributeClass, ProjectImpl.Meta_SkipDoc));

                        if( hasFilterAttr )
                        {
                            AdHocAgent.LOG.Error(
                                                 "Filter template '{name}' cannot be used without a scope argument.\n"          +
                                                 "A filter template defines HOW to filter; you must supply WHAT to filter:\n\n" +
                                                 "  // Wrong:\n"                                                                +
                                                 "  interface MyState : l__________< {name} > {{}}\n\n"                         +
                                                 "  // Correct:\n"                                                              +
                                                 "  interface MyState : l__________< {name}<@Project> > {{}}",
                                                 nts.Name, nts.Name, nts.Name);
                            AdHocAgent.exit("Supply a scope argument to the filter template and rerun.");
                        }
                    }

                    // Check if the scope is a registered Entity (Project, Host, or Pack).
                    if( entities.TryGetValue(scope, out var entity) )
                        switch( entity )
                        {
                            case ProjectImpl prj:
                                prj.for_packs_in_scope(depth, pack => apply(pack, add));
                                return;
                            case HostImpl host:
                                host.for_packs_in_scope(depth, pack => apply(pack, add));
                                return;
                            case HostImpl.PackImpl pack:
                                pack.for_packs_in_scope(depth, _pack => apply(_pack, add));
                                return;
                        }

                    AdHocAgent.LOG.Error("Unresolved symbol '{entity}' (line {line}) in PackSet collection.", scope, (scope.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line ?? -1) + 1);
                    AdHocAgent.exit("Fix and restart");
                }
            }

            /// <summary>
            /// The packs <c>[UDP&lt;EXCLUDE_PACKS_SET&gt;]</c> leaves out of the guarantee, resolved as any Pack Set is: packs, Pack
            /// Sets, <c>@</c> scopes, filter templates and <c>X&lt;&gt;</c> exclusions. Empty for a plain <c>[UDP]</c>.
            /// </summary>
            internal HashSet<HostImpl.PackImpl> udp_excluded(AttributeData udp)
            {
                var set = new HashSet<HostImpl.PackImpl>();
                var name = (udp.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax)?.Name switch
                           {
                               GenericNameSyntax g                        => g,
                               QualifiedNameSyntax { Right: GenericNameSyntax g } => g, // [Meta.UDP<…>]
                               _                                          => null
                           };
                if( name != null )
                    modify_by_implement(name.TypeArgumentList.Arguments[0], true, (sym, _, add, depth) => NamedPackSet.collect_packs_in_scope(sym, add, depth, (pack, a) =>
                                                                                                                                                                {
                                                                                                                                                                    if( a ) set.Add(pack);
                                                                                                                                                                    else set.Remove(pack);
                                                                                                                                                                }));
                return set;
            }

            public List<ActorImpl> actors = [];

            public object? _actors() => actors.Count == 0 ?
                                            null :
                                            actors;

            public int                      _actors_len                                                           => actors.Count;
            public Project.Connection.Actor _actors(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item) => actors[item];

            /// <summary>
            /// Packs that serve as the Source for a stream originating from the Right host.
            /// Only the Right host (Source) requires the full serialization infrastructure for these types.
            /// </summary>
            public HashSet<HostImpl.PackImpl> toR = [];

            /// <summary>
            /// Packs that serve as the Sink for a stream originating from the Right host.
            /// Only the Left host (Sink) requires the full deserialization infrastructure for these types.
            /// </summary>
            public HashSet<HostImpl.PackImpl> fromR = [];

            public ushort[]
                _toR,
                _fromR;

            public object? _hostR_to_packs() => toR.Count < 1 ?
                                                    null :
                                                    _toR = toR.Select(p => (ushort)p.idx).ToArray();

            public int    _hostR_to_packs_len                                                           => _toR.Length;
            public ushort _hostR_to_packs(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item) => _toR[item];


            public object? _hostR_from_packs() => fromR.Count < 1 ?
                                                      null :
                                                      _fromR = fromR.Select(p => (ushort)p.idx).ToArray();

            public int    _hostR_from_packs_len                                                           => _fromR.Length;
            public ushort _hostR_from_packs(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item) => _fromR[item];

            /// <summary>
            /// Packs that serve as the Source for a stream originating from the Left host.
            /// Only the Left host (Source) requires the full serialization infrastructure for these types.
            /// </summary>
            public HashSet<HostImpl.PackImpl> toL = [];

            /// <summary>
            /// Packs that serve as the Sink for a stream originating from the Left host.
            /// Only the Right host (Sink) requires the full deserialization infrastructure for these types.
            /// </summary>
            public HashSet<HostImpl.PackImpl> fromL = [];

            public ushort[]
                _toL,
                _fromL;


            public object? _hostL_to_packs() => toL.Count < 1 ?
                                                    null :
                                                    _toL = toL.Select(p => (ushort)p.idx).ToArray();

            public int    _hostL_to_packs_len                                                           => _toL.Length;
            public ushort _hostL_to_packs(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item) => _toL[item];


            public object? _hostL_from_packs() => fromL.Count < 1 ?
                                                      null :
                                                      _fromL = fromL.Select(p => (ushort)p.idx).ToArray();

            public int    _hostL_from_packs_len                                                           => _fromL.Length;
            public ushort _hostL_from_packs(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item) => _fromL[item];

            public class ActorImpl : Entity, Project.Connection.Actor{
                /// <summary>
                /// List of packs transmitted by the left host and received by the right host in this connection.
                /// </summary>
                public List<HostImpl.PackImpl> hostL_directly_transmitting_packs = [];

                public ushort _uid                { get; set; }
                public int    _MaxActiveInstances { get; set; } = -1;
                public bool   _Multicasting       { get; set; }

                /// <summary>
                /// Provides access to the list of packs transmitted by the left host for serialization purposes.
                /// Returns null if the list is empty.
                /// </summary>
                /// <returns>The list of packs transmitted by the left host or null if empty.</returns>
                public object? _hostL_transmitting_packs() => hostL_directly_transmitting_packs.Count == 0 ?
                                                                  null :
                                                                  hostL_directly_transmitting_packs;

                /// <summary>
                /// Gets the count of packs transmitted by the left host.
                /// </summary>
                public int _hostL_transmitting_packs_len => hostL_directly_transmitting_packs.Count;

                /// <summary>
                /// Retrieves the index of a specific pack transmitted by the left host at the given index.
                /// </summary>
                /// <param name="ctx">The transmitter context (not used here).</param>
                /// <param name="slot">The transmitter slot (not used here).</param>
                /// <param name="item">The index of the pack to retrieve.</param>
                /// <returns>The index of the pack transmitted by the left host at the specified index.</returns>
                public ushort _hostL_transmitting_packs(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => (ushort)hostL_directly_transmitting_packs[item].idx;


                /// <summary>
                /// List of packs related to transmission by the left host and received by the right host in this connection (including sub-packs and enums).
                /// </summary>
                public List<HostImpl.PackImpl> hostL_indirectly_transmitting_packs = [];

                /// <summary>
                /// Provides access to the list of packs related to transmission by the left host for serialization purposes.
                /// Returns null if the list is empty.
                /// </summary>
                /// <returns>The list of packs related to transmission by the left host or null if empty.</returns>
                public object? _hostL_related_packs() => hostL_indirectly_transmitting_packs.Count == 0 ?
                                                             null :
                                                             hostL_indirectly_transmitting_packs;

                /// <summary>
                /// Gets the count of packs related to transmission by the left host.
                /// </summary>
                public int _hostL_related_packs_len => hostL_indirectly_transmitting_packs.Count;

                /// <summary>
                /// Retrieves the index of a specific pack related to transmission by the left host at the given index.
                /// </summary>
                /// <param name="ctx">The transmitter context (not used here).</param>
                /// <param name="slot">The transmitter slot (not used here).</param>
                /// <param name="item">The index of the pack to retrieve.</param>
                /// <returns>The index of the pack related to transmission by the left host at the specified index.</returns>
                public ushort _hostL_related_packs(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => (ushort)hostL_indirectly_transmitting_packs[item].idx;


                /// <summary>
                /// List of packs directly transmitted by the right host and received by the left host in this connection.
                /// </summary>
                public List<HostImpl.PackImpl> hostR_directly_transmitting_packs = [];

                /// <summary>
                /// Provides access to the list of packs transmitted by the right host for serialization purposes.
                /// Returns null if the list is empty.
                /// </summary>
                /// <returns>The list of packs transmitted by the right host or null if empty.</returns>
                public object? _hostR_transmitting_packs() => hostR_directly_transmitting_packs.Count == 0 ?
                                                                  null :
                                                                  hostR_directly_transmitting_packs;

                /// <summary>
                /// Gets the count of packs transmitted by the right host.
                /// </summary>
                public int _hostR_transmitting_packs_len => hostR_directly_transmitting_packs.Count;

                /// <summary>
                /// Retrieves the index of a specific pack transmitted by the right host at the given index.
                /// </summary>
                /// <param name="ctx">The transmitter context (not used here).</param>
                /// <param name="slot">The transmitter slot (not used here).</param>
                /// <param name="item">The index of the pack to retrieve.</param>
                /// <returns>The index of the pack transmitted by the right host at the specified index.</returns>
                public ushort _hostR_transmitting_packs(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => (ushort)hostR_directly_transmitting_packs[item].idx;

                /// <summary>
                /// List of packs related to transmission by the right host and received by the left in this connection (including sub-packs and enums).
                /// </summary>
                public List<HostImpl.PackImpl> hostR_indirectly_transmitting_packs = [];

                /// <summary>
                /// Provides access to the list of packs related to transmission by the right host for serialization purposes.
                /// Returns null if the list is empty.
                /// </summary>
                /// <returns>The list of packs related to transmission by the right host or null if empty.</returns>
                public object? _hostR_related_packs() => hostR_indirectly_transmitting_packs.Count == 0 ?
                                                             null :
                                                             hostR_indirectly_transmitting_packs;

                /// <summary>
                /// Gets the count of packs related to transmission by the right host.
                /// </summary>
                public int _hostR_related_packs_len => hostR_indirectly_transmitting_packs.Count;

                /// <summary>
                /// Retrieves the index of a specific pack related to transmission by the right host at the given index.
                /// </summary>
                /// <param name="ctx">The transmitter context (not used here).</param>
                /// <param name="slot">The transmitter slot (not used here).</param>
                /// <param name="item">The index of the pack to retrieve.</param>
                /// <returns>The index of the pack related to transmission by the right host at the specified index.</returns>
                public ushort _hostR_related_packs(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => (ushort)hostR_indirectly_transmitting_packs[item].idx;


                /// <summary>
                /// List of states defined within this connection. States represent processing steps within the connection.
                /// </summary>
                public List<StateImpl> states = [];

                /// <summary>
                /// Provides access to the list of states for serialization purposes.
                /// Returns null if the list is empty.
                /// </summary>
                /// <returns>The list of states or null if empty.</returns>
                public object? _states() => states.Count == 0 ?
                                                null :
                                                states;

                /// <summary>
                /// Gets the count of states in this connection.
                /// </summary>
                public int _states_len => states.Count;

                /// <summary>
                /// Retrieves a specific state at the given index.
                /// </summary>
                /// <param name="ctx">The transmitter context (not used here).</param>
                /// <param name="slot">The transmitter slot (not used here).</param>
                /// <param name="item">The index of the state to retrieve.</param>
                /// <returns>The state at the specified index.</returns>
                public Project.Connection.Actor.State _states(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => states[item];

                public ushort _parent => _name == "Actor0" ?
                                             Project.Host._parent__.NULL :
                                             parent_by_source_code?.across_idx ?? Project.Host._parent__.NULL;

                public ActorImpl(ProjectImpl prj, CSharpCompilation? compilation, SyntaxNode? node, bool isActor0 = false) : base(prj,
                                                                                                                                  isActor0 ?
                                                                                                                                      null :
                                                                                                                                      compilation,
                                                                                                                                  isActor0 ?
                                                                                                                                      null :
                                                                                                                                      node as BaseTypeDeclarationSyntax)
                {
                    if( isActor0 )
                    {
                        this.node = node as BaseTypeDeclarationSyntax;
                        model     = compilation.GetSemanticModel(node.SyntaxTree);
                        symbol    = model.GetDeclaredSymbol(this.node)!;
                        _name     = "Actor0";
                        uid       = 0; // !!!
                    }
                    else if( node is InterfaceDeclarationSyntax interfaceNode )
                    {
                        var maxInstancesProp = interfaceNode.Members
                                                            .OfType<PropertyDeclarationSyntax>()
                                                            .FirstOrDefault(p => p.Identifier.ValueText.EndsWith("MaxActiveInstances"));

                        var expression = maxInstancesProp?.ExpressionBody?.Expression
                                         ?? maxInstancesProp?.Initializer?.Value;

                        if( expression != null )
                        {
                            _Multicasting = expression.IsKind(SyntaxKind.UnaryPlusExpression);


                            _MaxActiveInstances = model.GetConstantValue(expression).Value is null ?
                                                      1 :
                                                      Convert.ToInt32(model.GetConstantValue(expression).Value);

                            if( _Multicasting && _MaxActiveInstances == 1 )
                                AdHocAgent.LOG.Warning(
                                                       "Actor '{actor}' (line: {line}) is configured for multicasting (using the '+' modifier), "             +
                                                       "but 'MaxActiveInstances' is set to 1. "                                                               +
                                                       "A multicast group limited to a single member is logically redundant and acts as a standard unicast. " +
                                                       "Please remove the '+' modifier or increase the instance limit.",
                                                       full_path, line_in_src_code);
                        }

                        switch( _name )
                        {
                            case "Actor0":
                            case "id":
                            case "Id":
                            case "OnSerializing":
                            case "OnSerialized":
                            case "OnReceiving":
                            case "OnReceived":
                            case "OnEvent":
                            case "BroadcastActor":
                            case "BroadcastActorInstance":
                            case "Actors":
                            case "Actor":
                            case "Connection":
                            case "Virtual":
                                AdHocAgent.exit($"Error: '{_name}' (line {line_in_src_code}) is a reserved identifier and cannot be assigned.", 123);
                                break;
                        }

                        var sym                        = symbol;
                        while( !sym.isConnects() ) sym = sym!.ContainingType;
                        ((ConnectionImpl)entities[sym!]).actors.Add(this);
                    }
                    else if( node is MethodDeclarationSyntax method ) { synthetic_node = method; }
                }

                public MethodDeclarationSyntax synthetic_node;

                public void InitActor(HashSet<object> once, HostImpl hostL, HostImpl hostR)
                {
                    if( states.Count == 0 ) return;

                    once.Clear();

#region Traverse graph, graft external states, validate, reorder
                    // 1. Resolve null transitions on locally-declared states only (self-loop default).
                    //    Grafted states have their null branches resolved as they are cloned in.
                    foreach( var st in states )
                        foreach( var br in st.branchesL.Concat(st.branchesR) )
                            br.goto_state ??= st;

                    // 2. DFS traversal from the initial state (states[0]).
                    //    When a branch targets a state not present in the local list, clone it ("graft"),
                    //    add the clone to states, redirect the branch, and continue traversal.
                    //    graftMap ensures each original external state produces exactly one local clone
                    //    even when multiple branches point to the same external state.
                    var linkedStates = new List<StateImpl>();
                    var graftMap     = new Dictionary<StateImpl, StateImpl>(); // original → local clone

                    StateImpl ensureLocal(StateImpl target)
                    {
                        if( states.Contains(target) ) return target;                          // already local
                        if( graftMap.TryGetValue(target, out var existing) ) return existing; // already cloned

                        var clone = target.clone();
                        graftMap[target] = clone;

                        // Resolve null branches on the fresh clone before it enters traversal.
                        foreach( var cbr in clone.branchesL.Concat(clone.branchesR) )
                            cbr.goto_state ??= clone;

                        states.Add(clone); // make it part of this actor's state list
                        return clone;
                    }

                    void scan(StateImpl src)
                    {
                        if( !once.Add(src) ) return; // cycle / already-visited guard

                        // Only states with transitional branches belong in the linked chain.
                        if( src.branchesL.Concat(src.branchesR).Any(br => br.IsTransitional) )
                            linkedStates.Add(src); // collect in DFS order; states[0] → index 0

                        foreach( var br in src.branchesL.Concat(src.branchesR) )
                        {
                            if( br.goto_state == StateImpl.End || br.goto_state == StateImpl.Close ) continue;

                            br.goto_state = ensureLocal(br.goto_state); // graft if external, redirect in place
                            scan(br.goto_state);
                        }
                    }

                    scan(states[0]); // drives the entire traversal; all grafting happens here

#region Check for duplicate state names.
                    var duplicateStateNames = states.GroupBy(s => s._name)
                                                    .Where(g => g.Count() > 1)
                                                    .ToList();

                    if( 0 < duplicateStateNames.Count )
                    {
                        var report = new StringBuilder();
                        report.AppendLine($"\n[!] FSM Integrity Error: Duplicate state names detected in Actor '{full_path}'");
                        report.AppendLine($"Location: {node?.SyntaxTree.FilePath} (around line {line_in_src_code})");
                        report.AppendLine(new string('-', 80));
                        report.AppendLine("Full State Map for this Actor:");
                        foreach( var st in states )
                        {
                            var isDup = duplicateStateNames.Any(g => g.Key == st._name);
                            var prefix = isDup ?
                                             " [!] " :
                                             "     ";
                            var originInfo = st.origin != null ?
                                                 $"(Grafted from {st.origin.parent_by_source_code?.full_path}, original line: {st.origin.line_in_src_code})" :
                                                 "(Original)";
                            report.AppendLine($"{prefix}State: '{st._name}' {originInfo}");
                        }

                        report.AppendLine(new string('-', 80));
                        report.AppendLine("Conflicts Detail:");
                        foreach( var group in duplicateStateNames )
                        {
                            report.AppendLine($"  - Name '{group.Key}' is defined {group.Count()} times:");
                            foreach( var st in group )
                            {
                                var loc = st.origin != null ?
                                              $"Source: {st.origin.symbol?.Locations[0].GetMappedLineSpan()}" :
                                              "Local Declaration";
                                report.AppendLine($"    * {loc}");
                            }
                        }

                        AdHocAgent.LOG.Error(report.ToString());
                        AdHocAgent.exit("State name collision detected. Reusable FSM blocks must be grafted into Actors where their state names do not conflict with existing ones.");
                    }
#endregion
                    if( linkedStates.Count == 0 ) //only  fire-and-forget functions
                    {
                        if( _MaxActiveInstances != -1 )
                        {
                            if( _name != "Actor0" )
                                AdHocAgent.LOG.Warning(
                                                       "Actor '{actor}' (line: {line})contains only fire-and-forget functions but assigned MaxActiveInstances value." +
                                                       "MaxActiveInstances is ignored for this actor type. ",
                                                       full_path, line_in_src_code);
                            _Multicasting       = false;
                            _MaxActiveInstances = -1;
                        }
                    }
                    else if( linkedStates.Count < states.Count )
                        AdHocAgent.exit(
                                        $"\n[FSM Integrity Error] Actor '{full_path}' (line: {line_in_src_code}) illegally mixes linked and isolated states.\n"                                          +
                                        $"  Linked chain states : {string.Join(", ", linkedStates.Select(s => $"'{s._name}'"))}\n"                                                                       +
                                        $"  Isolated states     : {string.Join(", ", states.Where(st => !linkedStates.Contains(st)).Select(s => $"'{s._name}'"))}\n\n"                                   +
                                        $"An Actor's FSM must be strictly one of the following:\n"                                                                                                       +
                                        $"  • Stateful Actor  — A single, connected state machine. All states must belong to a chain linked by transitional branches (L____________ / ____________R).\n" +
                                        $"  • Singleton Actor — A collection of completely independent states using ONLY non-transitional branches (l____________ / ____________r / _____lr_____).\n\n"  +
                                        $"Resolution: Extract the isolated states into their own separate Actor.");
                    else if( _MaxActiveInstances == -1 && 2 < linkedStates.Count ) // not a simple request-response actor
                        AdHocAgent.exit($"The stateful Actor '{full_path}' (line {line_in_src_code}) must explicitly define 'MaxActiveInstances'. Use 'int MaxActiveInstances => UNLIMITED;' for no limit, but be aware of potential DDoS vulnerabilities.");


                    if( _Multicasting && states.Count == 2 )
                    {
                        var Call   = states[0];
                        var Return = states[1];

                        // Check for exact RPC Ping-Pong signature: "Call -> Return -> End" FSM, 1 branch each, alternating sides
                        if( Call.branchesL.Count   + Call.branchesR.Count   == 1 &&
                            Return.branchesL.Count + Return.branchesR.Count == 1 &&
                            Call.branchesL.Count                            == Return.branchesR.Count )
                        {
                            var (Call_bra, Return_bra) = Call.branchesL.Count == 1 ?
                                                             (Call.branchesL, Return.branchesR) :
                                                             (Call.branchesR, Return.branchesL);

                            if( Call_bra[0].goto_state == Return && (Return_bra[0].goto_state == StateImpl.End || Return_bra[0].goto_state == StateImpl.Close) )
                            {
                                _Multicasting = false;
                                AdHocAgent.LOG.Warning(
                                                       "Actor '{actor}' (line: {line}) is configured for multicasting (using the '+' modifier on MaxActiveInstances), " +
                                                       "but it implements a short-lived RPC flow (Call -> Return -> End). "                                             +
                                                       "Multicasting is designed for long-lived, continuous streams or subscriptions. "                                 +
                                                       "Please remove the '+' modifier or redesign the FSM.",
                                                       full_path, line_in_src_code);
                            }
                        }
                    }


                    // 6. Assign final sequential indices.
                    for( var i = 0; i < states.Count; i++ )
                        states[i].idx = i;
#endregion

#region Validate Unique Pack IDs per State/Host (including non-transitional states)
                    // Isolated (non-transitional) states play like a grouping of functions where the actor never changes state.
                    // Because they never transition, they are essentially globally active within the Actor.
                    var isolatedStates = states.Where(s => !linkedStates.Contains(s)).ToList();

                    void check_unique_packs(StateImpl? linkedSt, string side, Func<StateImpl, List<BranchImpl>> branchSelector)
                    {
                        // Collect packs while maintaining a reference to the state AND the specific branch
                        var packOccurrences = new List<(HostImpl.PackImpl pack, StateImpl state, BranchImpl branch)>();

                        var activeStates = isolatedStates.ToList();
                        if( linkedSt != null ) activeStates.Add(linkedSt);

                        foreach( var state in activeStates )
                            foreach( var br in branchSelector(state) )
                                foreach( var pack in br.packs )
                                    packOccurrences.Add((pack, state, br));

                        // Group by pack identity to find duplicates
                        var duplicates = packOccurrences
                                         .GroupBy(x => x.pack, HostImpl.PackImpl.Identity)
                                         .Where(g => 1 < g.Count())
                                         .ToList();

                        if( 0 < duplicates.Count )
                        {
                            // ANSI styling. Modern Windows 10+/Win Terminal/PS7/macOS/Linux render these natively;
                            // NO_COLOR=1 disables — see https://no-color.org/.
                            var color = Environment.GetEnvironmentVariable("NO_COLOR") == null;

                            string esc(string code) => color ?
                                                           $"[{code}m" :
                                                           "";

                            string R   = esc("0"),  B   = esc("1"),  Dim = esc("2");
                            string Red = esc("91"), Yel = esc("93"), Grn = esc("92"), Cyn = esc("96"), Mag = esc("95"), Gry = esc("90");

                            var report = new StringBuilder();
                            var bar    = new string('━', 78);

                            string scope = linkedSt != null ?
                                               $"active scope of state {Mag}'{linkedSt._name}'{R}" :
                                               $"the {Mag}non-transitional states{R} {Dim}(Singleton actor){R}";

                            report.AppendLine();
                            report.AppendLine($"{Red}{B}{bar}{R}");
                            report.AppendLine($"{Red}{B}  FSM AMBIGUITY{R}{Red}  ·  same packet ID claimed by multiple handlers{R}");
                            report.AppendLine($"{Red}{B}{bar}{R}");
                            report.AppendLine();
                            report.AppendLine($"  {Cyn}actor:{R}  {Mag}{full_path}{R}");
                            report.AppendLine($"  {Cyn}side: {R}  {B}{side}{R} host");
                            report.AppendLine($"  {Cyn}scope:{R}  {scope}");
                            report.AppendLine();

                            foreach( var dup in duplicates )
                            {
                                var pack    = dup.First().pack;
                                var occList = dup.ToList();

                                report.AppendLine($"  {Yel}▌ packet{R}  {Mag}{B}{pack.full_path}{R}   {Gry}({pack.symbol}){R}");

                                for( var i = 0; i < occList.Count; i++ )
                                {
                                    var occurrence = occList[i];
                                    var connector = i == occList.Count - 1 ?
                                                        "└─" :
                                                        "├─";

                                    string brType = occurrence.branch.IsTransitional ?
                                                        $"{Yel}Transitional → {occurrence.branch.goto_state?._name ?? "End"}{R}" :
                                                        $"{Grn}Stay{R}";

                                    string loc = occurrence.state.origin != null ?
                                                     $"grafted from {occurrence.state.origin.parent_by_source_code?.full_path} line {occurrence.state.origin.line_in_src_code}" :
                                                     $"line {occurrence.state.line_in_src_code}";

                                    report.AppendLine($"  {Gry}{connector}{R} state {Mag}'{occurrence.state._name}'{R}  {Dim}·{R}  {brType}  {Dim}·{R}  {Gry}{loc}{R}");
                                }

                                report.AppendLine();
                            }

                            report.AppendLine($"  {Cyn}why:{R}   non-transitional states ({Dim}l____________{R}, {Dim}____________r{R}, {Dim}_____lr_____{R}) are");
                            report.AppendLine($"         {B}always active{R} — they overlay every linked state. While the actor is in any");
                            report.AppendLine($"         linked state X, both X's handlers and every overlay's handlers run side-by-side.");
                            report.AppendLine($"         A packet ID landing in two of them fires two handlers for the same wire packet,");
                            report.AppendLine($"         and the dispatcher cannot pick. Stay/Stay duplicates are still a real conflict.");
                            report.AppendLine();
                            // Build the concrete X<...> list. Single pack → X<Pack>; multiple → X<(Pack1, Pack2, ...)>.
                            var conflictPacks = duplicates.Select(d => d.First().pack.full_path).ToArray();
                            var exclusion = conflictPacks.Length == 1 ?
                                                $"X<{conflictPacks[0]}>" :
                                                $"X<({string.Join(", ", conflictPacks)})>";
                            var sideAttr = side == "Left" ?
                                               "l____________" :
                                               "____________r";

                            report.AppendLine($"  {Grn}fix:{R}   add this exclusion to the broad inclusion on the {B}{side}{R} side:");
                            report.AppendLine($"           {B}{exclusion}{R}");
                            report.AppendLine($"         e.g. inside the PACKS expression of the overlay branch:");
                            report.AppendLine($"           {Dim}[{sideAttr}<(SomeHost, @SomeScope, {exclusion})>]{R}");
                            report.AppendLine();
                            report.AppendLine($"{Red}{B}{bar}{R}");

                            // Bypass Serilog's single-line wrapping for the multi-line report.
                            Console.Write(report.ToString());
                            AdHocAgent.exit("FSM Ambiguity detected — see report above.");
                        }
                    }

                    if( linkedStates.Count == 0 )
                    {
                        // Actor is a Singleton (only non-transitional states). Check all of them together.
                        check_unique_packs(null, "Left",  s => s.branchesL);
                        check_unique_packs(null, "Right", s => s.branchesR);
                    }
                    else
                    {
                        // Actor is Stateful. Check EACH linked state against itself AND all non-transitional states.
                        foreach( var st in linkedStates )
                        {
                            check_unique_packs(st, "Left",  s => s.branchesL);
                            check_unique_packs(st, "Right", s => s.branchesR);
                        }
                    }
#endregion

#region Merge branches with same goto state
                    void merge_same_goto_state_branches(List<BranchImpl> brs)
                    {
                        foreach( var g in brs.GroupBy(br => br.goto_state!.symbol, SymbolEqualityComparer.Default)
                                             .Where(g => 1 < g.Count()).ToArray() )
                        {
                            var dst = g.First();
                            foreach( var br in g.Skip(1) )
                            {
                                brs.Remove(br);
                                dst.packs.AddRange(br.packs);
                            }

                            dst.packs = dst.packs
                                           .GroupBy(p => p, HostImpl.PackImpl.Identity)
                                           .Select(g => g.First())
                                           .ToList();
                        }
                    }

                    foreach( var st in states )
                    {
                        merge_same_goto_state_branches(st.branchesL);
                        merge_same_goto_state_branches(st.branchesR);
                    }
#endregion


                    hostL._included = true;
                    hostR._included = true;

                    foreach( var pack in states.SelectMany(state => state.branchesL.SelectMany(branch => branch.packs)).Distinct() )
                    {
                        hostL_directly_transmitting_packs.Add(pack);
                        pack._included = true;
                    }

                    foreach( var pack in states.SelectMany(state => state.branchesR.SelectMany(branch => branch.packs)).Distinct() )
                    {
                        hostR_directly_transmitting_packs.Add(pack);
                        pack._included = true;
                    }

                    if( hostL_directly_transmitting_packs.Count == 0 && hostR_directly_transmitting_packs.Count == 0 )
                        AdHocAgent.exit($"The actor {symbol} (line {line_in_src_code}) does not have any packs to transmit.");
                }


                /// <summary>
                /// Applies a state modification to this connection, adding or removing a state.
                /// </summary>
                /// <param name="state">The state to apply.</param>
                /// <param name="add">True to add the state, false to remove it.</param>
                /// <param name="SwapHosts">True if the hosts in the cloned state branches should be swapped (for LR connection modifications).</param>
                void apply(StateImpl state, bool add, bool SwapHosts)
                {
                    var i = states.FindIndex(st => equals(st.symbol, state.symbol));

                    if( i == -1 )
                    {
                        if( !add ) return;

                        var cloned = state.clone();
                        if( SwapHosts )
                            (cloned.branchesL, cloned.branchesR) = (cloned.branchesR, cloned.branchesL);

                        states.Add(cloned);
                    }
                    else if( !add ) states.RemoveAt(i);
                }

                public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once)
                {
                    bool SwapHosts;

                    if( entities.TryGetValue((SwapHosts = by_what.Name.Equals("SwapHosts")) && by_what.isMeta() ?
                                                 ((INamedTypeSymbol)by_what).TypeArguments[0] :
                                                 by_what, out var value) )
                        switch( value )
                        {
                            case ConnectionImpl connection:
                                connection.Init(once);

                                connection.actors[0].states.ForEach(state => apply(state, add, SwapHosts));
                                break;
                            case StateImpl state:
                                apply(state, add, SwapHosts);
                                break;
                        }
                }
            }

            /// <summary>
            /// Represents a State entity within a Connection. States define processing steps in a communication flow.
            /// </summary>
            public class StateImpl : Entity, Project.Connection.Actor.State{
                public ushort _uid => (ushort)uid;

                /// <summary>
                /// A special terminal target state that deallocates the current Actors instance.
                /// This effectively deletes the linked actors,
                /// while leaving the underlying physical connection open for other actors.
                /// </summary>
                public static StateImpl? End;

                /// <summary>
                /// A special terminal target state that gracefully terminates the physical connection.
                /// This ensures the transmission queue is fully drained before closing the pipe.
                /// Once all pending data is sent, the communication link between hosts is safely shut down.
                /// </summary>
                public static StateImpl? Close;


                /// <summary>
                /// Initializes a new instance of the <see cref="StateImpl"/> class for the Exit state.
                /// This constructor is used only for creating the static Exit state instance.
                /// </summary>
                internal StateImpl(ProjectImpl project) : base(project, null, null) { _name = ""; }

                /// <summary>
                /// Initializes a new instance of the <see cref="StateImpl"/> class.
                /// </summary>
                /// <param name="project">The project this state belongs to.</param>
                /// <param name="compilation">The Roslyn compilation object.</param>
                /// <param name="state">The StructDeclarationSyntax node representing the state struct.</param>
                internal StateImpl(ProjectImpl project, CSharpCompilation? compilation, SyntaxNode state) : base(project, compilation, state as BaseTypeDeclarationSyntax)
                {
                    switch( parent_by_source_code )
                    {
                        case ConnectionImpl conn: // unnamed , default actor
                            conn.actors[0].states.Add(this);
                            parent_artificial = conn.actors[0]; // Metadata redirection
                            break;
                        case ActorImpl namedCh:
                            namedCh.states.Add(this);
                            break;
                        default:
                            if( state is MethodDeclarationSyntax ) break; // It will be explicitly linked during VisitMethodDeclaration
                            AdHocAgent.LOG.Error("State {state} (line {line}) declaration must be within a connection or a actor scope...", symbol, line_in_src_code);
                            AdHocAgent.exit("Fix the problem and try again");
                            break;
                    }
                }


                //  on code
                //
                //      interface ModifyConnection  {
                //          interface ModifyState: Modify<ModifiedState>,
                //            _<
                //                Server.Info//
                //               ,//
                //                AuthorisationRequest
                //            >{ }
                //
                // pass to Init Modify<ModifiedState>
                //Gets the interfaces implemented by the parent connection that are of type Modify<State>.
                //Used to identify states that modify other states in the same connection.
                public IEnumerable<INamedTypeSymbol> by_parent_actor_modify => symbol is ITypeSymbol ts ?
                                                                                   ts.OriginalDefinition.ContainingType.Interfaces.Where(I => I.isModify()) :
                                                                                   [];

                public override bool Init_As_Modifier_Dispatch_Modifications_On_Targets(HashSet<object> once)
                {
                    if( base.Init_As_Modifier_Dispatch_Modifications_On_Targets(once) ) return true; //direct modification of other states

                    Init_Collect_Modification(this, once); //just build self

                    if( by_parent_actor_modify.Any() ) //and cast to target connections
                        foreach( var what_modify in by_parent_actor_modify )
                            ((ConnectionImpl)entities[what_modify.TypeArguments[0]]).apply(this, true, false);

                    return true;
                }


                protected override void Init_Collect_Modification(Entity target, HashSet<object> once)
                {
                    var target_state = (StateImpl)target;

                    if( node is not StructDeclarationSyntax structNode ) return;

                    foreach( var attrList in structNode.AttributeLists )
                        foreach( var attrSyntax in attrList.Attributes )
                        {
                            var ctor = model.GetSymbolInfo(attrSyntax).Symbol as IMethodSymbol;
                            var ac   = ctor?.ContainingType;
                            if( !IsBranchAttribute(ac) ) continue;

                            // Classify the attribute by name.
                            string side;
                            bool   isMaster,          isTransitional;
                            int    targetArgIdx = -1, packsArgIdx = -1;

                            switch( ac!.Name )
                            {
                                case "L____________Attribute":
                                    side           = "L";
                                    isMaster       = true;
                                    isTransitional = true;
                                    targetArgIdx   = 0;
                                    packsArgIdx = ac.TypeArguments.Length >= 2 ?
                                                      1 :
                                                      -1;
                                    break;
                                case "l____________Attribute":
                                    side           = "L";
                                    isMaster       = false;
                                    isTransitional = false;
                                    packsArgIdx = ac.TypeArguments.Length >= 1 ?
                                                      0 :
                                                      -1;
                                    break;
                                case "____________RAttribute":
                                    side           = "R";
                                    isMaster       = true;
                                    isTransitional = true;
                                    targetArgIdx   = 0;
                                    packsArgIdx = ac.TypeArguments.Length >= 2 ?
                                                      1 :
                                                      -1;
                                    break;
                                case "____________rAttribute":
                                    side           = "R";
                                    isMaster       = false;
                                    isTransitional = false;
                                    packsArgIdx = ac.TypeArguments.Length >= 1 ?
                                                      0 :
                                                      -1;
                                    break;
                                case "_____lr_____Attribute":
                                    side           = "LR";
                                    isMaster       = false;
                                    isTransitional = false;
                                    packsArgIdx = ac.TypeArguments.Length >= 1 ?
                                                      0 :
                                                      -1;
                                    break;
                                default:
                                    continue;
                            }

                            // Leading-trivia doc and /*uid*/ comments attach to the whole attribute list.
                            var commonDoc = string.Join(' ', attrList.GetLeadingTrivia().Select(t => get_doc(t)));

                            ushort? parsedUid = null;
                            foreach( var t in attrSyntax.DescendantTrivia() )
                                if( t.IsKind(SyntaxKind.MultiLineCommentTrivia) )
                                {
                                    var m = HasDocs._uid.Match(t.ToString());
                                    if( m.Success )
                                    {
                                        parsedUid = (ushort)m.Groups[1].Value.to_base256_value();
                                        break;
                                    }
                                }

                            // Resolve TARGET_STATE for transitional attributes.
                            StateImpl? update_to   = null;
                            StateImpl? update_from = null;
                            if( targetArgIdx >= 0 && targetArgIdx < ac.TypeArguments.Length )
                            {
                                var targetSym = ac.TypeArguments[targetArgIdx];
                                if( targetSym.isMeta() && targetSym.Name == "X" && targetSym is INamedTypeSymbol xnts )
                                {
                                    bool scan(INamedTypeSymbol src, Func<INamedTypeSymbol, bool> todo) => todo(src) && src.TypeArguments.OfType<INamedTypeSymbol>().All(s => scan(s, todo));

                                    scan(xnts, t => !entities.TryGetValue(t, out var e) || e is not StateImpl st || (update_from = st) == null);
                                }
                                else if( targetSym.Name == "Close" )
                                {
                                    Close     ??= new(projects[0]) { symbol = Meta_Close, _name = "Close" };
                                    update_to =   Close;
                                }
                                else if( targetSym.Name == "End" )
                                {
                                    End       ??= new(projects[0]) { symbol = Meta_End, _name = "End" };
                                    update_to =   End;
                                }
                                else if( entities.TryGetValue(targetSym, out var ent) && ent is StateImpl st )
                                {
                                    st.Init(once);
                                    var actor = target.parent_by_source_code is ConnectionImpl c ?
                                                    c.actors[0] :
                                                    (ActorImpl)target.parent_by_source_code!;
                                    update_to = actor.states.FirstOrDefault(x => equals(x.symbol, st.symbol)) ?? st;
                                }
                                else
                                {
                                    AdHocAgent.LOG.Error("State {state} (line {line}): transitional branch attribute targets `{target}`, which is not a known state, `Close`, `End`, or `X<...>`. Fix the target and rerun.",
                                                         symbol, target_state.line_in_src_code, targetSym.ToString());
                                    AdHocAgent.exit("");
                                }
                            }

                            // uid_pos marks where the persistent `/*uid*/` comment is inserted. We place it at
                            // the very end of the attribute body — after the generic-argument `>` and/or the
                            // constructor-argument `)` — so the output reads:
                            //     [____________r<(PackA, PackB)>/*ÿ*/]
                            // This keeps a single `]` close at the end regardless of whether multiple attributes
                            // are stacked in one `[]` pair. Roslyn's `attrSyntax.Span.End` already points there.
                            var uid_pos = attrSyntax.Span.End;

                            var targetBranches = side == "R" ?
                                                     target_state.branchesR :
                                                     target_state.branchesL;

                            BranchImpl branch;
                            if( is_Modifier )
                            {
                                branch = targetBranches.FirstOrDefault(br => br.goto_state == (update_to ?? update_from));
                                if( branch == null )
                                {
                                    branch = new(project) { uid_pos = uid_pos, _doc = "" };
                                    targetBranches.Add(branch);
                                }

                                branch.goto_state = update_to ?? update_from;
                            }
                            else
                            {
                                branch = new(project)
                                         {
                                             uid_pos = uid_pos,
                                             _doc    = "",
                                             goto_state = isTransitional ?
                                                              update_to :
                                                              this
                                         };
                                targetBranches.Add(branch);
                            }

                            branch._doc += commonDoc;
                            if( parsedUid.HasValue ) branch.uid = parsedUid.Value;
                            branch.Side              = side;
                            branch.IsMasterAuthority = isMaster;
                            branch.IsTransitional    = isTransitional;

                            void apply(HostImpl.PackImpl _pack, bool add)
                            {
                                _pack.ValidateTypeUsage(target_state.line_in_src_code);
                                if( !add )
                                    branch.packs.RemoveAll(p => equals(p.symbol, _pack.symbol));
                                else if( branch.packs.All(p => !equals(p.symbol, _pack.symbol)) )
                                    branch.packs.Add(_pack);
                            }

                            // Buffer for packs drawn from `@`-prefixed scopes. The branch-level KeepDoc/SkipDoc
                            // regexes apply only to this buffer — never to direct inclusions or subtractions.
                            var scopedPacks = new HashSet<HostImpl.PackImpl>();

                            var modifyAction = (ISymbol sym, SyntaxNode sn, bool add, uint depth) =>
                                               {
                                                   // State-terminal markers (Close/End) are not valid PACKS arguments; they're
                                                   // TARGET_STATE-only. Silently ignore if seen.
                                                   if( sym.isMeta() && (sym.Name == "Close" || sym.Name == "End") ) return;

                                                   // Named Pack Set:
                                                   //   * `Foo` (no `@`, depth==0)         → direct inclusion, branch filters bypass (spec §5).
                                                   //   * `@Foo` (with `@`, depth>0) + add → feed set contents into `scopedPacks` so branch filters apply.
                                                   //   * subtraction                      → always remove the set's contents.
                                                   if( named_packs.TryGetValue(sym, out var nps) )
                                                   {
                                                       if( !add )
                                                           branch.packs.RemoveAll(p => nps.packs.Any(pp => equals(p.symbol, pp.symbol)));
                                                       else if( depth == 0 )
                                                           nps.packs.Where(p => !branch.packs.Any(pp => equals(p.symbol, pp.symbol))).ToList().ForEach(p => branch.packs.Add(p));
                                                       else // `@` scope: branch-level filters will narrow this further.
                                                           foreach( var p in nps.packs )
                                                               scopedPacks.Add(p);
                                                       return;
                                                   }

                                                   // Subtraction via X<...> is unconditional — it bypasses regex filters.
                                                   if( !add )
                                                   {
                                                       switch( entities.GetValueOrDefault(sym) )
                                                       {
                                                           case ProjectImpl prj: prj.for_packs_in_scope(depth, p => apply(p,  false)); break;
                                                           case HostImpl host:   host.for_packs_in_scope(depth, p => apply(p, false)); break;
                                                           case HostImpl.PackImpl pk:
                                                               if( 0 < depth ) pk.for_packs_in_scope(depth, p => apply(p.ValidateTypeUsage(target_state.line_in_src_code), false));
                                                               else apply(pk, false);
                                                               break;
                                                           default:
                                                               AdHocAgent.LOG.Error("Unexpected item {item} (line:{line}) in the {state} PACKS argument", sym, sn.GetLocation().GetLineSpan().StartLinePosition.Line + 1, symbol);
                                                               AdHocAgent.exit("Fix the problem and restart.");
                                                               break;
                                                       }

                                                       return;
                                                   }

                                                   // `depth == 0` ⇒ direct inclusion (no `@` prefix). KeepDoc/SkipDoc do NOT apply.
                                                   if( depth == 0 )
                                                   {
                                                       switch( entities.GetValueOrDefault(sym) )
                                                       {
                                                           case ProjectImpl prj:      prj.for_packs_in_scope(0, p => apply(p,  true)); break;
                                                           case HostImpl host:        host.for_packs_in_scope(0, p => apply(p, true)); break;
                                                           case HostImpl.PackImpl pk: apply(pk, true); break;
                                                           default:
                                                               AdHocAgent.LOG.Error("Unexpected item {item} (line:{line}) in the {state} PACKS argument", sym, sn.GetLocation().GetLineSpan().StartLinePosition.Line + 1, symbol);
                                                               AdHocAgent.exit("Fix the problem and restart.");
                                                               break;
                                                       }

                                                       return;
                                                   }

                                                   // `depth > 0` ⇒ `@`-prefixed scope. Buffer enumerated packs; filters will be applied below.
                                                   switch( entities.GetValueOrDefault(sym) )
                                                   {
                                                       case ProjectImpl prj:      prj.for_packs_in_scope(depth, p => scopedPacks.Add(p)); break;
                                                       case HostImpl host:        host.for_packs_in_scope(depth, p => scopedPacks.Add(p)); break;
                                                       case HostImpl.PackImpl pk: pk.for_packs_in_scope(depth, p => scopedPacks.Add(p.ValidateTypeUsage(target_state.line_in_src_code))); break;
                                                       default:
                                                           AdHocAgent.LOG.Error("Unexpected item {item} (line:{line}) in the {state} PACKS argument", sym, sn.GetLocation().GetLineSpan().StartLinePosition.Line + 1, symbol);
                                                           AdHocAgent.exit("Fix the problem and restart.");
                                                           break;
                                                   }
                                               };

                            // Resolve the PACKS generic argument (if this overload carries one).
                            var hasPacksGeneric = packsArgIdx >= 0 && attrSyntax.Name is GenericNameSyntax ggn && packsArgIdx < ggn.TypeArgumentList.Arguments.Count;
                            if( hasPacksGeneric )
                                modify_by_implement(((GenericNameSyntax)attrSyntax.Name).TypeArgumentList.Arguments[packsArgIdx], true, modifyAction);

                            // Extract KeepDoc / SkipDoc / KeepName / SkipName constructor arguments.
                            var (keepDoc, skipDoc, keepName, skipName) = CollectBranchFilters(attrSyntax);
                            var keepDocRegex  = CompileBranchRegex(keepDoc,  attrSyntax, "KeepDoc");
                            var skipDocRegex  = CompileBranchRegex(skipDoc,  attrSyntax, "SkipDoc");
                            var keepNameRegex = CompileBranchRegex(keepName, attrSyntax, "KeepName");
                            var skipNameRegex = CompileBranchRegex(skipName, attrSyntax, "SkipName");
                            var anyFilter     = keepDocRegex != null || skipDocRegex != null || keepNameRegex != null || skipNameRegex != null;

                            // No PACKS generic AND at least one filter → implicit whole-project scope (§4 Case A).
                            if( !hasPacksGeneric && anyFilter )
                                project.for_packs_in_scope(uint.MaxValue, p => scopedPacks.Add(p));

                            // Apply filters in spec order: KeepDoc → SkipDoc → KeepName → SkipName.
                            foreach( var p in scopedPacks )
                            {
                                if( keepDocRegex != null || skipDocRegex != null )
                                {
                                    var pool = UnifiedPackDoc(p);
                                    if( keepDocRegex != null && !keepDocRegex.IsMatch(pool) ) continue;
                                    if( skipDocRegex != null && skipDocRegex.IsMatch(pool) ) continue;
                                }

                                if( keepNameRegex != null || skipNameRegex != null )
                                {
                                    var name = p.full_path;
                                    if( keepNameRegex != null && !keepNameRegex.IsMatch(name) ) continue;
                                    if( skipNameRegex != null && skipNameRegex.IsMatch(name) ) continue;
                                }

                                apply(p, true);
                            }

                            if( side == "LR" ) target_state.branchesR.Add(branch.clone());
                        }

                    if( target_state.branchesL.Any(b => b.IsMasterAuthority) && target_state.branchesR.Any(b => b.IsMasterAuthority) )
                    {
                        AdHocAgent.LOG.Error("State {state} line:{line} contains BOTH Left (L____________) and Right (____________R) Master authority branches. A state can only assign Master authority to one side.", target_state.symbol, target_state.line_in_src_code);
                        AdHocAgent.exit("Please fix the state definition and try again.");
                    }
                }

                /// <summary>
                /// Extracts the four optional filter arguments (<c>KeepDoc</c>, <c>SkipDoc</c>, <c>KeepName</c>,
                /// <c>SkipName</c>) from a branch attribute. Arguments may be positional (0 → KeepDoc,
                /// 1 → SkipDoc, 2 → KeepName, 3 → SkipName) or named (<c>KeepDoc:</c>, etc.). Empty strings
                /// mean no filtering for that side.
                /// </summary>
                (string keepDoc, string skipDoc, string keepName, string skipName) CollectBranchFilters(AttributeSyntax attrSyntax)
                {
                    var keepDoc  = "";
                    var skipDoc  = "";
                    var keepName = "";
                    var skipName = "";
                    if( attrSyntax.ArgumentList == null ) return (keepDoc, skipDoc, keepName, skipName);

                    var pos = 0;
                    foreach( var arg in attrSyntax.ArgumentList.Arguments )
                    {
                        var constant = model.GetConstantValue(arg.Expression);
                        var value = constant.HasValue && constant.Value is string s ?
                                        s :
                                        null;
                        if( value == null )
                        {
                            pos++;
                            continue;
                        }

                        var name = arg.NameColon?.Name.Identifier.Text ?? arg.NameEquals?.Name.Identifier.Text;
                        switch( name )
                        {
                            case "KeepDoc":  keepDoc  = value; break;
                            case "SkipDoc":  skipDoc  = value; break;
                            case "KeepName": keepName = value; break;
                            case "SkipName": skipName = value; break;
                            case null:
                                switch( pos )
                                {
                                    case 0: keepDoc  = value; break;
                                    case 1: skipDoc  = value; break;
                                    case 2: keepName = value; break;
                                    case 3: skipName = value; break;
                                }

                                break;
                        }

                        pos++;
                    }

                    return (keepDoc, skipDoc, keepName, skipName);
                }

                /// <summary>
                /// Compiles a branch attribute regex argument. An empty / null pattern yields <c>null</c>
                /// (meaning "no filter"). Invalid patterns terminate with a user-facing error.
                /// </summary>
                static Regex? CompileBranchRegex(string pattern, AttributeSyntax attrSyntax, string paramName)
                {
                    if( string.IsNullOrEmpty(pattern) ) return null;
                    try { return new Regex(pattern, RegexOptions.Compiled); }
                    catch( ArgumentException ex )
                    {
                        AdHocAgent.LOG.Error("Branch attribute at line {line}: {param} pattern `{pattern}` is not a valid regex — {msg}",
                                             attrSyntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                                             paramName, pattern, ex.Message);
                        AdHocAgent.exit("Fix the regex and rerun.");
                        return null;
                    }
                }

                /// <summary>
                /// Merges the class-level `///` doc with any Dashboard-line trailing text into one
                /// searchable pool per pack. Both sources are treated identically by <c>KeepDoc</c>/<c>SkipDoc</c>
                /// regexes on branch attributes and by interface-level <c>[KeepDoc]</c>/<c>[SkipDoc]</c> filters.
                /// </summary>
                static string UnifiedPackDoc(HostImpl.PackImpl pack)
                {
                    var doc = pack._doc ?? "";
                    if( pack.symbol != null )
                    {
                        foreach( var prj in projects )
                            if( prj.pack_user_tags.TryGetValue(pack.symbol, out var extra) && !string.IsNullOrEmpty(extra) )
                                doc = doc + " " + extra;
                    }

                    return doc;
                }

                public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once) { }


                /// <summary>
                /// List of branches for the left side of this state.
                /// </summary>
                public List<BranchImpl> branchesL = [];

                /// <summary>
                /// Provides access to the list of left branches for serialization purposes.
                /// </summary>
                /// <returns>The list of left branches.</returns>
                public object? _branchesL() => branchesL;

                /// <summary>
                /// Gets the count of left branches in this state.
                /// </summary>
                public int _branchesL_len => branchesL.Count;

                /// <summary>
                /// Retrieves a specific left branch at the given index.
                /// </summary>
                /// <param name="ctx">The transmitter context (not used here).</param>
                /// <param name="slot">The transmitter slot (not used here).</param>
                /// <param name="item">The index of the left branch to retrieve.</param>
                /// <returns>The left branch at the specified index.</returns>
                public Project.Connection.Actor.State.Branch _branchesL(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => branchesL[item];

                /// <summary>
                /// List of branches for the right side of this state.
                /// </summary>
                public List<BranchImpl> branchesR = [];

                /// <summary>
                /// Provides access to the list of right branches for serialization purposes.
                /// </summary>
                /// <returns>The list of right branches.</returns>
                public object? _branchesR() => branchesR;

                /// <summary>
                /// Gets the count of right branches in this state.
                /// </summary>
                public int _branchesR_len => branchesR.Count;

                /// <summary>
                /// Retrieves a specific right branch at the given index.
                /// </summary>
                /// <param name="ctx">The transmitter context (not used here).</param>
                /// <param name="slot">The transmitter slot (not used here).</param>
                /// <param name="item">The index of the right branch to retrieve.</param>
                /// <returns>The right branch at the specified index.</returns>
                public Project.Connection.Actor.State.Branch _branchesR(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => branchesR[item];

                /// <summary>
                /// Creates a clone of this StateImpl instance, including its branches, for modification purposes.
                /// </summary>
                /// <returns>A new StateImpl instance that is a clone of the current instance.</returns>
                public StateImpl clone() => new(project)
                                            {
                                                origin      = this,
                                                _doc        = _doc,
                                                _inline_doc = _inline_doc,
                                                _name       = _name,
                                                symbol      = symbol,
                                                uid         = uid,
                                                _constants_ = _constants_.ToList(),
                                                branchesL   = branchesL.Select(br => br.clone()).ToList(),
                                                branchesR   = branchesR.Select(br => br.clone()).ToList(),
                                            };
            }

            /// <summary>
            /// Represents a Branch entity within a State. Branches define communication flows within a state and specify packs to be transmitted.
            /// </summary>
            public class BranchImpl(ProjectImpl project) : Project.Connection.Actor.State.Branch{
                /// <summary>
                /// Original BranchImpl instance if this is a clone, otherwise null.
                /// </summary>
                public BranchImpl? origin;

                public bool   IsMasterAuthority { get; set; }         // L____________ / ____________R
                public bool   IsTransitional    { get; set; }         // l____________ / ____________r / _____lr_____
                public string Side              { get; set; } = "LR"; // "L", "R", "LR"

                /// <summary>
                /// Creates a clone of this BranchImpl instance for modification purposes.
                /// </summary>
                /// <returns>A new BranchImpl instance that is a clone of the current instance.</returns>
                public BranchImpl clone() => new(project)
                                             {
                                                 origin  = this,
                                                 _doc    = _doc,
                                                 uid_pos = -1,
                                                 uid = uid < ulong.MaxValue ?
                                                           0xFFFF - uid :
                                                           ulong.MaxValue,
                                                 packs             = packs.ToList(),
                                                 IsMasterAuthority = IsMasterAuthority,
                                                 IsTransitional    = IsTransitional,
                                                 Side              = Side,
                                                 goto_state        = goto_state
                                             };

                public ulong uid = ulong.MaxValue;

                /// <summary>
                /// Project this branch belongs to.
                /// </summary>
                public ProjectImpl project = project;

                /// <summary>
                /// Position of the UID comment in the source code.
                /// </summary>
                public int uid_pos;

                public string? _doc { get; set; }

                /// <summary>
                /// State entity that this branch transitions to.
                /// </summary>
                public StateImpl? goto_state; //if null Exit state

                public ushort _goto_state => goto_state == StateImpl.Close ?
                                                 Project.Connection.Actor.State.Close :
                                                 goto_state == StateImpl.End ?
                                                     Project.Connection.Actor.State.End :
                                                     (ushort)goto_state!.idx;

                /// <summary>
                /// List of packs transmitted in this branch.
                /// </summary>
                public List<HostImpl.PackImpl> packs = [];

                /// <summary>
                /// Provides access to the list of packs transmitted in this branch for serialization purposes.
                /// Returns null if the list is empty.
                /// </summary>
                /// <returns>The list of packs transmitted in this branch or null if empty.</returns>
                public object? _packs() => packs.Count == 0 ?
                                               null :
                                               packs;

                /// <summary>
                /// Gets the count of packs transmitted in this branch.
                /// </summary>
                public int _packs_len => packs.Count;

                /// <summary>
                /// Retrieves the index of a specific pack transmitted in this branch at the given index.
                /// </summary>
                /// <param name="ctx">The transmitter context (not used here).</param>
                /// <param name="slot">The transmitter slot (not used here).</param>
                /// <param name="item">The index of the pack to retrieve.</param>
                /// <returns>The index of the pack transmitted in this branch at the specified index.</returns>
                public ushort _packs(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => (ushort)packs[item].idx;
            }
        }

        public readonly List<EndpointSetImpl> endpoint_sets = [];

        public readonly List<ResumableImpl> resumables = [];

        /// <summary>
        /// The service packs of the resume handshake, synthesized once per project as soon as it declares a Resumable, in this
        /// fixed order: <c>Resume_</c>, <c>Ack_</c>, <c>Lost_</c>, <c>Ask_</c>. Each carries a position in the journalled stream,
        /// <c>pos</c> and <c>time</c>; the one of <c>Ask_</c> is where it was sent. <c>Resume_</c> and <c>Ack_</c> travel from the receiver of
        /// the stream to its sender, <c>Lost_</c> and <c>Ask_</c> the other way. They sit at the project root, take the four ids
        /// right above the topmost one in use, and belong to no state: each resumable connection lists them among the packs its
        /// sides transmit, nothing more.
        /// </summary>
        public readonly List<HostImpl.PackImpl> resumable_service = [];

        /// <summary>
        /// A guaranteed-delivery declaration: <c>interface Name : Resumable&lt;HOST, PACKS&gt; { … }</c> (Meta.Resumable).
        /// <para>
        /// Shipped to the generator as a <b>header pack</b> named <c>Name_</c> — the trailing underscore is the marker — with one
        /// field, <c>int pos_Name</c>: the pack's position in the journalled stream. The generated code sets and reads it on
        /// its own; the application never sees it. The pack's constants, in transported order:
        /// <list type="bullet">
        ///   <item><c>mb</c> — <c>resumable_megabytes</c>: the journal ceiling of the leg. How long a session outlives its
        ///   socket is not here: the legs of one connection share one parked slot, so it is <c>[Resumable(minutes)]</c> on the
        ///   connection, a constant of the connection like any attribute's;</item>
        ///   <item><c>Host</c>, <c>Conn</c> — the idx of the sending host and of the connection, set once those are numbered.</item>
        /// </list>
        /// Its targets are bound like any other header's: every pack in PACKS that HOST really transmits on the connection
        /// gets an artificial <c>_headerN</c> field typed with this pack. A pack named in PACKS but not transmitted there is
        /// dropped with a warning.
        /// </para>
        /// <para>
        /// Servicing the stream takes four packs, synthesized once per project as soon as it declares a Resumable — see
        /// <see cref="resumable_service"/>. They belong to no state and no branch: each resumable connection lists them among
        /// the packs its sides transmit, so they get classes, serializers and ids like any pack, while the generated code
        /// services them outside the application's FSM.
        /// </para>
        /// </summary>
        public class ResumableImpl : HostImpl.PackImpl{
            /// <summary>The Connection this declaration sits in — a Resumable is legal nowhere else.</summary>
            public readonly ISymbol connection_symbol;

            /// <summary>The connection and the host of it whose sending is guaranteed. Resolved before the connections are processed.</summary>
            public ConnectionImpl connection = null!;
            public HostImpl       host       = null!;

            public int resumable_megabytes = 100;

            /// <summary>Bits of the seconds a position carries next to its 32-bit offset: 21, wrapping every 24 days.</summary>
            public const int TIME_BITS = 21;

            /// <summary>The offset of a pack in the sender's journalled stream, 32 bits.</summary>
            public static FieldImpl position(ProjectImpl project, string name) => new(project, null, null)
            {
                _name         = name,
                exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int32,
                inT           = (int)Project.Host.Pack.Field.DataType.t_int32
            };

            /// <summary>The low <see cref="TIME_BITS"/> of the unix seconds a pack is sent at.</summary>
            public static FieldImpl time(ProjectImpl project, string name)
            {
                var       fld = new FieldImpl(project, null, null) { _name = name, _min_value = 0, _max_value = (1L << TIME_BITS) - 1 };
                fld.set_exT_ByRange(0, fld._max_value.Value);
                fld.set_inT_ByRange(0, fld._max_value.Value);
                return fld;
            }

            /// <summary>The idx of the sending host and of the connection — known once hosts and connections are numbered.</summary>
            public readonly ConstantImpl Host, Conn;

            /// <summary>
            /// Not declared: made by the agent for a <c>[UDP]</c> connection, one per host that sends there, covering every pack it
            /// sends — see <see cref="ResumableImpl(ProjectImpl, ConnectionImpl, HostImpl, IEnumerable{HostImpl.PackImpl})"/>.
            /// </summary>
            public readonly bool udp;

            /// <summary>
            /// Builds the header in full at parse time, so every later phase treats it as the ordinary header it is: Init collects
            /// its targets from PACKS, modifiers may add fields, "Bind declared headers" gives each target its `_headerN` field,
            /// the name and field collection passes see nothing special. The server tells it from a declared header by
            /// `id == t_char && referred == true && nested_max == 123` — a combination no header can have otherwise.
            /// </summary>
            public ResumableImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax node) : base(project, compilation, node)
            {
                ISymbol? conn = null;
                for( var t = symbol!.ContainingType; t != null && conn == null; t = t.ContainingType )
                    if( t.AllInterfaces.Any(i => equals(i.OriginalDefinition, Meta_Connects) || equals(i.OriginalDefinition, Meta_VirtuallyConnects)) )
                        conn = t;
                if( conn == null )
                    AdHocAgent.exit($"{symbol} (line {line_in_src_code}) is a Resumable, and a Resumable can be declared only inside a Connection body. Move it into the Connection whose leg it guards.", 2);
                connection_symbol = conn!;

                // The interface-based base constructor registers a constants set. This is a header — undo that.
                project.constants_packs.Remove(this);
                in_host?.const_enum_packs.Remove(this);

                project.all_packs.Add(this);
                target_packs = [];

                foreach( var prop in node.Members.OfType<PropertyDeclarationSyntax>() )
                {
                    var expr = prop.ExpressionBody?.Expression ?? prop.Initializer?.Value;
                    if( expr == null || model.GetConstantValue(expr).Value is not { } val ) continue;

                    switch( prop.Identifier.ValueText )
                    {
                        case "resumable_megabytes": resumable_megabytes = Convert.ToInt32(val); break;
                    }
                }

                if( resumable_megabytes < 1 )
                    AdHocAgent.exit($"{symbol} (line {line_in_src_code}) sets resumable_megabytes to {resumable_megabytes}; the journal needs at least one megabyte.", 2);

                (Host, Conn) = header(project, model, symbol.Name);
            }

            /// <summary>
            /// The declaration a <c>[UDP]</c> connection stands for on the side of <paramref name="host"/>: every pack the host
            /// transmits there is guaranteed. A datagram can be lost on its own, not only with the connection, and the journal is
            /// what brings it back, so over UDP nothing may travel without it. Made once the actors of the connection say what
            /// each side transmits; named <c>Connection_Host</c>, parented by the connection, as a declared one would be.
            /// </summary>
            public ResumableImpl(ProjectImpl project, ConnectionImpl connection, HostImpl host, IEnumerable<HostImpl.PackImpl> packs) : base(project, $"{connection._name}_{host._name}")
            {
                udp               = true;
                connection_symbol = connection.symbol!;
                this.connection   = connection;
                this.host         = host;
                parent_artificial = connection;
                target_packs      = [..packs];
                (Host, Conn)      = header(project, connection.model, _name);
                _nested_max       = 123; // the marker, set for the declared ones where their connection is resolved
            }

            /// <summary>
            /// What makes the pack the header the generator keys on: the id of a header, the name <c>Name_</c>, the two fields,
            /// the constants, the registrations.
            /// </summary>
            (ConstantImpl Host, ConstantImpl Conn) header(ProjectImpl project, SemanticModel model, string name)
            {
                _id   = (ushort)Project.Host.Pack.Field.DataType.t_char; // a Header
                _name = name + "_";                                      // the marker the generator keys on
                project.header_packs.Add(this);
                project.resumables.Add(this);

                // The header's fields: the pack's position in the journalled stream and the seconds it is sent at, named per
                // declaration: header field names are unique project-wide.
                fields.Add(position(project, "pos_" + name));
                fields.Add(time(project, "time_" + name));
                fields[0].is_Header = fields[1].is_Header = true;

                // The constants, in transported order: mb, Host, Conn. Collected and indexed with every other constant in
                // "Set pack indices and collect all fields"; Host and Conn get their values once hosts and connections are numbered.
                var Int32 = model.Compilation.GetSpecialType(SpecialType.System_Int32);
                ConstantImpl constant(string cname, long value) { var c = new ConstantImpl(project, model, Int32, null, value) { _name = cname }; _constants_.Add(c); return c; }
                constant("mb", resumable_megabytes);

                _referred = true; // the marker: "Update referred status of packs" touches transmittable packs only, a header is not one
                return (constant("Host", 0), constant("Conn", 0));
            }

            /// <summary><c>[UDP]</c>, <c>[UDP(minutes)]</c>, <c>[UDP&lt;EXCLUDE_PACKS_SET&gt;]</c> or <c>[UDP&lt;EXCLUDE_PACKS_SET&gt;(minutes)]</c> on <paramref name="symbol"/>, or null.</summary>
            public static AttributeData? UDP(ISymbol? symbol) => symbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass is { Name: "UDPAttribute" } c && c.ContainingNamespace?.ToString() == "org.unirail.Meta");

            /// <summary>The HOST argument of <c>Resumable&lt;HOST, PACKS&gt;</c>.</summary>
            public ITypeSymbol host_arg => symbol!.Interfaces.First(i => equals(i.OriginalDefinition, Meta_Resumable)).TypeArguments[0];
        }

        public class EndpointSetImpl : Entity{
            /// <summary>
            /// The resolved list of concrete (Connection, Host) endpoints this set represents.
            /// This is populated during the Init() phase.
            /// </summary>
            public readonly List<(ConnectionImpl Connection, HostImpl Host)> Endpoints = [];

            public EndpointSetImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax node) : base(project, compilation, node) { }

            public override bool Init_As_Modifier_Dispatch_Modifications_On_Targets(HashSet<object> once) => false;

            /// <summary>
            /// Overridden modify method to build the list of endpoints.
            /// This is called recursively by the base Init() method.
            /// </summary>
            public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once)
            {
                if( !add ) return; // Removing endpoints is not a supported feature.

                if( by_what is INamedTypeSymbol nts )
                    // Case 1: The symbol is another EndpointSet. Recursively add its endpoints.
                    if( entities.TryGetValue(nts, out var entity) && entity is EndpointSetImpl otherSet )
                    {
                        otherSet.Init(once); // Ensure the nested set is initialized first.
                        Endpoints.AddRange(otherSet.Endpoints);
                    }
                    // Case 2: The symbol is a concrete IfSendingFrom<Host, Connection>. Resolve and add it.
                    else if( equals(nts.OriginalDefinition, Meta_IfSendingFrom) )
                        if(
                            entities.TryGetValue(nts.TypeArguments[0], out var h) && h is HostImpl fromHost &&
                            entities.TryGetValue(nts.TypeArguments[1], out var c) && c is ConnectionImpl viaConnection
                        )
                        {
                            var endpoint = (connection: viaConnection, host: fromHost);
                            if( !Endpoints.Contains(endpoint) ) { Endpoints.Add(endpoint); }
                        }
            }
        }
    }

    /// <summary>
    /// Abstract base class for entities that have documentation and names.
    /// </summary>
    public abstract class HasDocs{
#region declaration parent-children relationship
        //  `across_idx` is for managing parent-child relationships.
        //  For instance, a pack declaration may be nested within another pack, a project, or a host. To manage this, we need a consistent ordering.
        // `across_idx` is assigned sequentially across a virtual collection of collections ordered as follows: packs, hosts, multiplexers, connections, actors, states and fields.

        public ushort? across_idx;

        public HasDocs? parent_artificial;

        public HasDocs? parent_by_source_code => symbol == null || symbol!.OriginalDefinition.ContainingType == null ?
                                                     parent_artificial :
                                                     Entity.entities[symbol!.OriginalDefinition.ContainingType];

        public ushort _parent => (parent_by_source_code is ProjectImpl prj ?
                                      prj.proxy :
                                      parent_artificial ?? parent_by_source_code)?.across_idx ?? Project.Host._parent__.NULL;

        public ProjectImpl in_project
        {
            get
            {
                for( var e = this;; e = e.parent_by_source_code )
                    if( e is ProjectImpl project )
                        return project;
            }
        }


        public ProjectImpl.HostImpl? in_host
        {
            get
            {
                for( var e = this; e != null; e = e.parent_by_source_code )
                    switch( e )
                    {
                        case ProjectImpl.HostImpl host: return host;
                        case ProjectImpl:               return null;
                    }

                return null;
            }
        }


        public string full_path => parent_by_source_code == null || parent_by_source_code == ProjectImpl.root_project ?
                                       _name :
                                       parent_by_source_code.full_path + "." + _name;
#endregion

        static readonly Regex LeadingSpaces             = new(@"^\s+", RegexOptions.Multiline);
        static readonly Regex InlineCommentsCleaner     = new(@"^\s*/{2,}", RegexOptions.Multiline);
        static readonly Regex BlockCommentsStart        = new(@"/\*+", RegexOptions.Multiline);
        static readonly Regex BlockCommentsEnd          = new(@"\s*\*+/", RegexOptions.Multiline);
        static readonly Regex CleanupAsterisk           = new(@"^\s*\*+", RegexOptions.Multiline);
        static readonly Regex CleanupSeeCref            = new(@"<\s*see\s*cref .*>", RegexOptions.Multiline);

        /// <summary>
        /// Regular expression to match UID comments in the source code (e.g., /*ÿ*/).
        /// </summary>
        public static readonly Regex _uid = new(@"\/\*([\u00FF-\u01FF\s]+)\*\/");


        // Pre-compiled Regex for performance. They are thread-safe.
        static readonly Regex LeadingWhitespaceRegex = new(@"^\s*", RegexOptions.Compiled);
        static readonly Regex CommentStripperRegex   = new(@"^\s*(///|/\*\*|\*|\*/)\s?", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Takes raw C# comment trivia, cleans it, and converts it into a rich HTML representation.
        /// This single function handles indentation normalization, comment stripping, and XML-to-HTML transformation.
        /// </summary>
        /// <param name="trivia">The SyntaxTrivia containing the XML documentation comment.</param>
        /// <returns>A formatted HTML string ready for display, or an empty string if the trivia is not a valid comment.</returns>
        static string ToHtml(SyntaxTrivia trivia)
        {
            // --- Part 1: Clean Trivia to Raw XML ---
            var rawComment = trivia.ToFullString();
            if( string.IsNullOrWhiteSpace(rawComment) || rawComment.TrimStart().StartsWith("#") )
                return ""; // Skip empty comments and preprocessor directives

            var lines = rawComment.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Normalize indentation by removing the most common leading whitespace
            var mostCommonIndent = 0;
            var indentCounts     = new Dictionary<int, int>();
            foreach( var line in lines )
            {
                if( string.IsNullOrWhiteSpace(line) ) continue;
                var indentLength = LeadingWhitespaceRegex.Match(line).Value.Replace("\t", "    ").Length;
                indentCounts.TryGetValue(indentLength, out var count);
                indentCounts[indentLength] = count + 1;
            }

            var indentRemover = new Regex($"^\\s{{0,{mostCommonIndent}}}");
            var cleanedLines  = lines.Select(line => indentRemover.Replace(line, "", 1));

            var rebuiltComment = string.Join("\n", cleanedLines);
            var cleanXml       = CommentStripperRegex.Replace(rebuiltComment, "").Trim();

            if( string.IsNullOrWhiteSpace(cleanXml) ) return "";

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml($"<root>{cleanXml}</root>");
                var htmlBuilder = new StringBuilder();

                void TransformNode(XmlNode node)
                {
                    foreach( XmlNode child in node.ChildNodes )
                        switch( child.NodeType )
                        {
                            case XmlNodeType.Text:
                                htmlBuilder.Append(child.Value);
                                break;

                            case XmlNodeType.Element:
                                var    element = (XmlElement)child;
                                var    tagName = element.LocalName.ToLowerInvariant();
                                string content;

                                switch( tagName )
                                {
                                    case "summary":
                                    case "remarks":
                                    case "returns":
                                        htmlBuilder.Append($"<div class=\"doc-{tagName}\"><strong>{char.ToUpper(tagName[0]) + tagName.Substring(1)}</strong>");
                                        TransformNode(element);
                                        htmlBuilder.Append("</div>");
                                        break;
                                    case "param":
                                    case "typeparam":
                                        content = $"<dt>{element.GetAttribute("name")}</dt><dd>";
                                        htmlBuilder.Append($"<div class=\"doc-{tagName}\">{content}");
                                        TransformNode(element);
                                        htmlBuilder.Append("</dd></div>");
                                        break;
                                    case "exception":
                                        var cref = element.GetAttribute("cref");
                                        content = $"<dt><code>{cref.Split('.').Last()}</code></dt><dd>";
                                        htmlBuilder.Append($"<div class=\"doc-{tagName}\">{content}");
                                        TransformNode(element);
                                        htmlBuilder.Append("</dd></div>");
                                        break;
                                    case "para":
                                        htmlBuilder.Append("<p>");
                                        TransformNode(element);
                                        htmlBuilder.Append("</p>");
                                        break;
                                    case "example":
                                    case "code":
                                        content = element.InnerText.Trim('\r', '\n');
                                        htmlBuilder.Append($"<div class=\"doc-code-block\"><pre><code class=\"language-csharp\">{content}</code></pre></div>");
                                        break;
                                    case "c":
                                        htmlBuilder.Append($"<code>{element.InnerText}</code>");
                                        break;
                                    case "see":
                                        var seeCref = element.GetAttribute("cref");
                                        var seeText = string.IsNullOrWhiteSpace(element.InnerText) ?
                                                          seeCref.Split('.').Last() :
                                                          element.InnerText;
                                        htmlBuilder.Append($"<code>{seeText}</code>");
                                        break;
                                    case "paramref":
                                    case "typeparamref":
                                        htmlBuilder.Append($"<em>{element.GetAttribute("name")}</em>");
                                        break;
                                    default:
                                        TransformNode(element);
                                        break;
                                }

                                break;
                        }
                }

                TransformNode(xmlDoc.DocumentElement);
                return htmlBuilder.ToString();
            }
            catch( XmlException ) { return $"<div class=\"doc-malformed\"><pre>{cleanXml}</pre></div>"; }
        }

        // This part takes the clean XML string and converts it to the @@marker format.
        // =================================================================================
        static string ToIntermediateFormat(string xmlContent)
        {
            // The recursive function that processes the XML node tree.
            void ProcessNode(XNode node, StringBuilder builder)
            {
                // Case 1: The node is a plain text node.
                // Append its value directly to preserve all whitespace, including line breaks.
                if( node is XText textNode ) { builder.Append(textNode.Value); }
                // Case 2: The node is an XML element (a tag).
                else if( node is XElement element )
                {
                    // Use a switch to handle known tags.
                    // Unknown tags will have their content processed, but the tag itself is ignored.
                    switch( element.Name.LocalName.ToLower() )
                    {
                        case "summary":
                            builder.Append("@@SUMMARY_START@@");
                            // Recurse to process all children of this tag.
                            foreach( var childNode in element.Nodes() ) ProcessNode(childNode, builder);
                            builder.Append("@@SUMMARY_END@@");
                            break;

                        case "remarks":
                            builder.Append("@@REMARKS_START@@");
                            foreach( var childNode in element.Nodes() ) ProcessNode(childNode, builder);
                            builder.Append("@@REMARKS_END@@");
                            break;

                        case "para":
                            // Add a space for better separation in the intermediate format.
                            builder.Append(" @@PARA_START@@ ");
                            foreach( var childNode in element.Nodes() ) ProcessNode(childNode, builder);
                            builder.Append(" @@PARA_END@@ ");
                            break;

                        // Handle both <c> and <code> for inline code.
                        case "c":
                        case "code":
                            builder.Append(" @@CODE_START@@ ");
                            // For code, we still want to trim the inner value to keep it clean.
                            // element.Value gets all descendant text concatenated, which is what we want here.
                            builder.Append(element.Value.Trim());
                            builder.Append(" @@CODE_END@@ ");
                            break;

                        // For any other tag (e.g., <see>, <paramref>), we don't add markers,
                        // but we still process its children to extract any text content inside.
                        default:
                            foreach( var childNode in element.Nodes() ) ProcessNode(childNode, builder);
                            break;
                    }
                }
            }

            if( string.IsNullOrEmpty(xmlContent) )
                return string.Empty;

            try
            {
                // Wrap content in a single <root> to handle XML fragments.
                var result = new StringBuilder();

                // Start processing from the root, iterating through all its children.
                // This ensures that text outside of <summary> or <remarks> is also included.
                foreach( var node in XElement.Parse($"<root>{xmlContent}</root>").Nodes() ) { ProcessNode(node, result); }

                // Return the final string, trimming any potential whitespace from the <root> wrapper.
                return result.ToString().Trim();
            }
            catch( XmlException )
            {
                return xmlContent; //Treating content as plain text
            }
        }


        /// <summary>
        /// Returns true when a `/** … */` block is metadata for the code generator rather than user
        /// documentation. Two metadata shapes are recognized:
        /// <list type="bullet">
        ///   <item><description>
        ///     <b>Language config</b> — top-level language-marker cref (`<see cref="InCS"/>`,
        ///     `<see cref="InJAVA"/>`, etc.). The block routes per-host language implementation.
        ///   </description></item>
        ///   <item><description>
        ///     <b>Dashboard</b> — top-level cref carrying an `id='N'` attribute. The block is the
        ///     protocol-wide pack-id inventory at the top of the file.
        ///   </description></item>
        /// </list>
        /// In both cases the surrounding prose and the listed crefs are routing / catalog directives —
        /// they must not leak into entity docs.
        /// </summary>
        static bool IsLangConfigBlock(DocumentationCommentTriviaSyntax dt)
        {
            foreach( var xml in dt.Content )
                if( xml is XmlEmptyElementSyntax xe )
                {
                    // Language markers — any of these at the top level marks the whole block as routing.
                    foreach( var attr in xe.Attributes.OfType<XmlCrefAttributeSyntax>() )
                        if( attr.Cref.ToString() is "InCS" or "InCPP" or "InJAVA" or "InRS" or "InGO" or "InTS" or "All" )
                            return true;

                    // Dashboard markers — any cref carrying an `id='N'` attribute identifies the block
                    // as the pack-id inventory (the top-of-file Dashboard). Those entries are parsed
                    // separately by VisitXmlCrefAttribute and must not pollute entity docs.
                    if( xe.Attributes.OfType<XmlTextAttributeSyntax>().Any(a => a.Name.ToString() == "id") )
                        return true;
                }

            return false;
        }

        /// <summary>
        /// Extracts documentation text from a SyntaxTrivia object, normalizing and cleaning up the documentation.
        /// </summary>
        /// <param name="trivia">The SyntaxTrivia object containing documentation comments.</param>
        /// <returns>The cleaned and normalized documentation string.</returns>
        public static string get_doc(SyntaxTrivia trivia)
        {
            if( AdHocAgent.is_diagramming ) return ToHtml(trivia);

            // Metadata blocks (Dashboard inventory, host language-config) are already filtered out
            // upstream by IsLangConfigBlock and never reach this point, so here we concat content
            // verbatim — no cref-with-id filter, no metadata scrubbing — and let the cleanup regexes
            // below deal with incidental inline `<see cref/>` markers inside real prose.
            var str = trivia.GetStructure() is DocumentationCommentTriviaSyntax dt ?
                          string.Concat(dt.Content.Select(x => x.ToFullString())) :
                          trivia.ToFullString();

            if( string.IsNullOrWhiteSpace(str) || str.Trim().StartsWith('#') ) return ""; // Skip preprocessor instructions

            // --- Indentation Normalization ---
            var len2Count = new Dictionary<int, int>();
            var allLines  = str.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

            foreach( var line in allLines )
            {
                if( string.IsNullOrWhiteSpace(line) ) continue;

                var match = LeadingSpaces.Match(line);
                if( match.Success )
                {
                    var spaces = match.Value.Replace("\t", "    ");
                    len2Count.TryGetValue(spaces.Length, out var count);
                    len2Count[spaces.Length] = count + 1;
                }
            }

            if( len2Count.Count != 0 )
            {
                // Find the most common indentation level of non-empty lines
                var mostCommonIndent = len2Count.OrderByDescending(kvp => kvp.Value).First().Key;
                // Create a regex to remove that specific amount of leading whitespace
                str = new Regex(@"^(\s){" + mostCommonIndent + "}", RegexOptions.Multiline).Replace(str, "");
            }

            // --- Comment Syntax Stripping ---
            str = InlineCommentsCleaner.Replace(str, ""); // Handles ///
            str = BlockCommentsStart.Replace(str, "");    // Handles /**
            str = BlockCommentsEnd.Replace(str, "");      // Handles */
            str = CleanupAsterisk.Replace(str, "");       // Handles leading * on each line

            // --- Unwanted Tag Removal ---
            str = CleanupSeeCref.Replace(str, "");

            // Final cleanup and return
            return ToIntermediateFormat(str.Trim());
        }

        public SemanticModel model;

        public override string  ToString()  => _name;
        public          string  _name       { get; set; }
        public          string? _doc        { get; set; }
        public          string? _inline_doc { get; set; }

        /// <summary>
        /// Index of this entity in the root project's typed collection (packs, hosts, connections, states).
        /// Initialized to int.MaxValue and assigned during project initialization.
        ///    Purpose: Used for high-performance O(1) array access in the generated protocol code.
        ///    Stability: Volatile. If you add a new packet in the middle of your C# interface, the idx of everything below it will change.
        ///    Scope: Usually local to a specific collection (e.g., pack_idx or host_idx).
        ///    Usage: Serialization, indexing into metadata arrays, and switch-case generation.
        /// </summary>
        public int idx = int.MaxValue; //index in the root project typed collection

        /// <summary>
        /// Brushes a given name to make it valid, capitalizing the first lowercase character if needed.
        /// Used to avoid naming conflicts with reserved keywords or prohibited names.
        /// </summary>
        /// <param name="name">The name to brush.</param>
        /// <param name="class_name">Optional class name to check for conflicts against (not currently used).</param>
        /// <param name="line">Source-code line number used in error messages when the name is rejected.</param>
        /// <returns>The brushed name, or the original name if no brushing is needed.</returns>
        public static string brush(string name, string class_name = "", int line = 0)
        {
            if( name != class_name && (name.Equals("_DefaultMaxLengthOf") || !is_prohibited(name, line)) ) return name;


            var new_name = name;

            for( var i = 0; i < name.Length; i++ )
                if( char.IsLower(name[i]) )
                {
                    new_name = new_name[..i] + char.ToUpper(new_name[i]) + new_name[(i + 1)..];
                    if( new_name == class_name || is_prohibited(new_name, line) ) continue;

                    return new_name;
                }

            return name;
        }

        /// <summary>
        /// Starting character position of this entity in the source code.
        /// </summary>
        public int char_in_source_code = -1;

        /// <summary>
        /// Project this entity belongs to.
        /// </summary>
        public ProjectImpl project;

        /// <summary>
        /// Initializes a new instance of the <see cref="HasDocs"/> class.
        /// </summary>
        /// <param name="prj">The project this entity belongs to.</param>
        /// <param name="name">The name of the entity.</param>
        /// <param name="node">The CSharpSyntaxNode associated with this entity.</param>
        public HasDocs(ProjectImpl? prj, string name, CSharpSyntaxNode? node)
        {
            project = prj ?? (ProjectImpl)this; //prj == null only for projects

            if( node == null ) return;

            name  = name[(name.LastIndexOf('.') + 1)..];
            _name = brush(name, line: node.GetLocation().GetMappedLineSpan().StartLinePosition.Line + 1);

            char_in_source_code = node.GetLocation().SourceSpan.Start;

            // Join with "\n", NOT Concat: Roslyn hands every `//` line as its OWN trivia, and get_doc
            // trims each one. Concatenating them edge to edge welded words together across the line
            // break - the emitted javadoc read "makes9/17/33/65" where the source had a line end.
            // A block comment is a single trivia and keeps its own newlines, so only `//` runs broke.
            var doc = string.Join("\n",
                                    node.GetLeadingTrivia()
                                        .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                                                    t.GetStructure() is DocumentationCommentTriviaSyntax dt &&
                                                    dt.Content.Any(xml => xml is not XmlEmptyElementSyntax) &&
                                                    !IsLangConfigBlock(dt) &&
                                                    ProjectImpl.numbers_table(dt) == null) // the numbers tables are not documentation
                                        .Select(get_doc));

            if( project.packs_id_info_end == -1 ) project.packs_id_info_end = char_in_source_code;


            if( 0 < (doc = doc.Trim('\r', '\n', '\t', ' ')).Length ) _doc = doc + "\n";
        }

        /// <summary>
        /// Compares two ISymbol objects for equality using SymbolEqualityComparer.Default.
        /// </summary>
        /// <param name="x">The first ISymbol object.</param>
        /// <param name="y">The second ISymbol object.</param>
        /// <returns>True if the symbols are equal, false otherwise.</returns>
        public static bool equals(ISymbol? x, ISymbol? y) => SymbolEqualityComparer.Default.Equals(x, y);

        /// <summary>
        /// Checks if a given name is prohibited (reserved keyword or special case) across C#, C++, Java, TypeScript, Rust, and Go.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <param name="line">Source-code line number used in the error message when the name starts or ends with an underscore.</param>
        /// <returns>True if the name is prohibited, false otherwise.</returns>
        public static bool is_prohibited(string name, int line = 0)
        {
            if( name[0] == '_' || name[^1] == '_' )
            {
                AdHocAgent.LOG.Error("Entity names cannot start or end with an underscore _. Please correct the name '{name}' (line: {line}) and try again.", name, line);
                AdHocAgent.exit("");
            }

            return name switch
                   {
                       // C#
                       "abstract" or "as" or "base" or "bool" or "break" or "byte" or "case" or "catch" or
                           "char" or "checked" or "class" or "const" or "continue" or "decimal" or "default" or
                           "delegate" or "do" or "double" or "else" or "enum" or "event" or "explicit" or "extern" or
                           "false" or "finally" or "fixed" or "float" or "for" or "foreach" or "goto" or "if" or
                           "implicit" or "in" or "int" or "interface" or "internal" or "is" or "lock" or "long" or
                           "namespace" or "new" or "null" or "object" or "operator" or "out" or "override" or "params" or
                           "private" or "protected" or "public" or "readonly" or "ref" or "return" or "sbyte" or
                           "sealed" or "short" or "sizeof" or "stackalloc" or "static" or "string" or "struct" or
                           "switch" or "this" or "throw" or "true" or "try" or "typeof" or "uint" or "ulong" or
                           "unchecked" or "unsafe" or "ushort" or "using" or "virtual" or "void" or "volatile" or

                           // C++
                           "alignas" or "alignof" or "and" or "and_eq" or "asm" or "auto" or "bitand" or "bitor" or
                           "bool" or "break" or "case" or "catch" or "char" or "char16_t" or "char32_t" or "class" or
                           "compl" or "concept" or "const" or "consteval" or "constexpr" or "constinit" or "const_cast" or
                           "continue" or "decltype" or "default" or "delete" or "do" or "double" or "dynamic_cast" or
                           "else" or "enum" or "explicit" or "export" or "extern" or "false" or "float" or "for" or
                           "friend" or "goto" or "if" or "inline" or "int" or "long" or "mutable" or "namespace" or
                           "new" or "noexcept" or "nullptr" or "operator" or "or" or "or_eq" or "private" or
                           "protected" or "public" or "reflexpr" or "register" or "reinterpret_cast" or "requires" or
                           "return" or "short" or "signed" or "sizeof" or "static" or "static_assert" or "static_cast" or
                           "struct" or "switch" or "template" or "this" or "thread_local" or "throw" or "true" or
                           "try" or "typedef" or "typeid" or "typename" or "union" or "unsigned" or "using" or "virtual" or
                           "void" or "volatile" or "wchar_t" or "while" or "xor" or "xor_eq" or

                           // Java
                           "abstract" or "assert" or "boolean" or "break" or "byte" or "case" or "catch" or
                           "char" or "class" or "const" or "continue" or "default" or "do" or "double" or "else" or
                           "enum" or "extends" or "final" or "finally" or "float" or "for" or "goto" or "if" or
                           "implements" or "import" or "instanceof" or "int" or "interface" or "long" or "native" or
                           "new" or "null" or "package" or "private" or "protected" or "public" or "return" or
                           "short" or "static" or "strictfp" or "super" or "switch" or "synchronized" or "this" or
                           "throw" or "throws" or "transient" or "true" or "try" or "void" or "volatile" or "while" or

                           // TypeScript
                           "any" or "as" or "boolean" or "break" or "case" or "catch" or "class" or "const" or
                           "continue" or "debugger" or "declare" or "default" or "delete" or "do" or "else" or
                           "enum" or "export" or "extends" or "false" or "finally" or "for" or "from" or "function" or
                           "if" or "implements" or "import" or "in" or "instanceof" or "interface" or "is" or
                           "keyof" or "let" or "module" or "namespace" or "never" or "new" or "null" or "number" or
                           "object" or "package" or "private" or "protected" or "public" or "readonly" or "require" or
                           "return" or "string" or "super" or "switch" or "symbol" or "this" or "throw" or "true" or
                           "try" or "type" or "typeof" or "undefined" or "unique" or "unknown" or "var" or "void" or
                           "while" or "with" or "yield" or

                           // Rust
                           "abstract" or "as" or "async" or "await" or "become" or "box" or "break" or "const" or
                           "continue" or "crate" or "do" or "dyn" or "else" or "enum" or "extern" or "false" or
                           "final" or "fn" or "for" or "if" or "impl" or "in" or "let" or "loop" or "macro" or
                           "match" or "mod" or "move" or "mut" or "override" or "priv" or "pub" or "ref" or
                           "return" or "self" or "Self" or "static" or "struct" or "super" or "trait" or "true" or
                           "try" or "type" or "typeof" or "union" or "unsafe" or "use" or "where" or "while" or

                           // Go
                           "break" or "case" or "chan" or "const" or "continue" or "default" or "defer" or "else" or
                           "fallthrough" or "for" or "func" or "go" or "goto" or "if" or "import" or "interface" or
                           "package" or "range" or "return" or "select" or "struct" or "switch" or "type" or
                           "var" or

                           // Reserved keywords or special cases across multiple languages
                           "arguments" or "eval" or "null" or "true" or "false" or "undefined" or "void" => true,
                       _ => false
                   };
        }

        /// <summary>
        /// Gets the line number in the source code where this entity is declared.
        /// </summary>
        public int line_in_src_code => symbol == null ?
                                           -1 :
                                           symbol!.Locations[0].GetLineSpan().StartLinePosition.Line + 1;

        /// <summary>
        /// List of constant members defined directly within this entity (e.g., enum members or constant fields in a constant set).
        /// </summary>
        public List<ProjectImpl.HostImpl.PackImpl.ConstantImpl> _constants_ = [];

        /// <summary>
        /// Provides access to the list of constant members for serialization purposes.
        /// Returns null if the list is empty.
        /// </summary>
        /// <returns>The list of constant members or null if empty.</returns>
        public object? _constants() => 0 < _constants_.Count ?
                                           _constants_ :
                                           null;

        /// <summary>
        /// Gets the count of constant members in this entity.
        /// </summary>
        public int _constants_len => _constants_.Count;

        /// <summary>
        /// Retrieves the index of a specific constant member at the given index.
        /// </summary>
        /// <param name="ctx">The transmitter context (not used here).</param>
        /// <param name="slot">The transmitter slot (not used here).</param>
        /// <param name="item">The index of the constant member to retrieve.</param>
        /// <returns>The index of the constant member at the specified index.</returns>
        public int _constants(Base_.Transmitter ctx, Base_.Transmitter.Slot slot, int item) => _constants_[item].idx;

        /// <summary>
        /// Set when a `Modify&lt;&gt;` modifier that declares attributes has taken this entity's attribute set over:
        /// everything below reads the modifier's declaration instead of this one's. `clear` is the
        /// `[ClearAttributes]` form, where the replacing set is empty and nothing is read at all.
        /// <para>Recorded while the modifiers are merged, which is long before any attribute is read — the redirection
        /// is what carries the decision across that distance.</para>
        /// </summary>
        public (SemanticModel model, MemberDeclarationSyntax node, ISymbol symbol, HasDocs by, bool clear)? attributes_from;

        /// <summary>True on a modifier whose attributes have moved onto its target, so they are not read here as well.</summary>
        public bool attributes_donated;

        /// <summary>
        /// The declaration whose attributes this entity actually carries: its own, or the one a `Modify&lt;&gt;`
        /// modifier put in their place. `null` when the set is empty — cleared by `[ClearAttributes]`, given away
        /// to a target, or never written. Read by anything that has to look at an entity's attributes OUTSIDE
        /// <see cref="read_attributes"/>, which applies the same substitution itself.
        /// </summary>
        internal (SemanticModel model, MemberDeclarationSyntax node)? attributes_source
        {
            get
            {
                if( attributes_donated ) return null;
                if( attributes_from is{ } replaced ) return replaced.clear ? null : (replaced.model, replaced.node);
                return this is Entity { node: { } own } ? (model, own) : null;
            }
        }

        /// <summary>
        /// Reads and processes attributes applied to this entity.
        /// </summary>
        /// <param name="model">The semantic model to resolve attribute symbols.</param>
        /// <param name="node">The MemberDeclarationSyntax node representing the entity with attributes.</param>
        /// <param name="sourceOverride">Optional symbol to read metadata from (e.g., from a TYPEDEF source field).</param>
        public void read_attributes(SemanticModel model, MemberDeclarationSyntax node, ISymbol? sourceOverride = null)
        {
            // A modifier's attributes belong to the entity it modifies, not to the modifier: they moved, they were
            // not copied, so a modifier that is also an ordinary transmittable pack carries none of its own.
            if( attributes_donated ) return;

            if( attributes_from is{ } replaced )
            {
                if( replaced.clear ) return; // `[ClearAttributes]`: the replacing set is empty
                model          = replaced.model;
                node           = replaced.node;
                sourceOverride = replaced.symbol; // argument VALUES come from reflection over the declaring symbol
            }

            var targetSymbol = sourceOverride ?? symbol;
            if( targetSymbol == null ) return;

            // 1. Unified metadata retrieval
            IList<CustomAttributeData> reflectionData;
            try
            {
                reflectionData = targetSymbol switch
                                 {
                                     INamedTypeSymbol => ProjectImpl.pack_reflection(targetSymbol).GetCustomAttributesData(),
                                     IMethodSymbol    => ProjectImpl.method_reflection(targetSymbol).GetCustomAttributesData(),
                                     _                => ProjectImpl.field_reflection(targetSymbol).GetCustomAttributesData()
                                 };
            }
            catch { return; }

            // Stream transform-stage / flow attributes are recognized as a byte-transform CHAIN (built-in Zstd/ChaCha20
            // AND user-declared stages alike), NOT ordinary metadata constants. Pull them out before the IsInternalMeta
            // filter and the generic-constants path below. Pack/host/etc. materialize the chain here; FIELDS instead
            // collect in Pass A and call collect_stream_stages explicitly in the Pass-B field loop.
            if( this is not ProjectImpl.HostImpl.PackImpl.FieldImpl )
            {
                var stageAttrs = node.AttributeLists.SelectMany(l => l.Attributes)
                                     .Where(s => (model.GetSymbolInfo(s).Symbol as IMethodSymbol)?.ContainingType is { } t && (ProjectImpl.is_stream_stage(t) || ProjectImpl.is_stream_flow(t) || ProjectImpl.is_stream_trim(t)))
                                     .Select(s => (model, s, (string?)null)) // declared right here: one model, nothing inherited
                                     .ToList();
                if( stageAttrs.Count > 0 )
                    if( this is ProjectImpl.HostImpl.PackImpl or ProjectImpl.ConnectionImpl ) _chain_attrs = stageAttrs; // built later: see the region "Chains of packs and connections"
                    else collect_stream_stages(stageAttrs);
            }

            // 2. Filter and Group Syntax by Attribute Type
            var attributeGroups = node.AttributeLists
                                      .SelectMany(list => list.Attributes)
                                      .Select(syntax => new { Syntax = syntax, Symbol = model.GetSymbolInfo(syntax).Symbol?.ContainingType as INamedTypeSymbol })
                                      .Where(x => x.Symbol != null && !ProjectImpl.is_stream_stage(x.Symbol) && !ProjectImpl.is_stream_flow(x.Symbol) && !ProjectImpl.is_stream_trim(x.Symbol) && !IsInternalMeta(x.Symbol))
                                      .GroupBy(x => x.Symbol, SymbolEqualityComparer.Default);

            foreach( var group in attributeGroups )
            {
                /*
                            This section describes how to generate code based on different attribute scenarios.

                            If there is a single attribute with a single argument, generate:
                            Example: public const string Tag = "Name";

                            If the argument is an array, generate:
                            Example: public static readonly long[] Tag = {123, 456};

                            If there is a single attribute with multiple arguments, generate an interface:
                            Example:
                            public interface Tag {
                                public const string name = "Name";
                                public const long value = 123L;
                                public static readonly long[] longs = {123, 456};
                                public static readonly string[] strings = {"123", "456"};
                            }

                            If there are multiple attributes with the same name, use [AttributeUsage] to allow multiple instances:
                            Reference: https://learn.microsoft.com/en-us/dotnet/api/system.attributeusageattribute.allowmultiple?view=net-8.0
                            Example usage:
                            [Tag(TagName)]
                            [Tag(TagName)]
                            [Tag(TagName)]

                            If there is a single argument, generate constants for each:
                            Example:
                            public const string Tag_0 = "Name";
                            public const string Tag_1 = "Name";

                            If there are multiple arguments, generate nested interfaces:
                            Example:
                            public interface Tag {
                                interface _0 {
                                    public const string name = "Name";
                                    public const string Description = "Description";
                                }
                                interface _1 {
                                    public const string name = "Name";
                                    public const string Description = "Description";
                                }
                            }

                             If attributes have the same name but originate from different namespaces,(though unlikely, but possible) generate interfaces with namespace paths:
                            Example:
                            public interface Tag {
                                interface path_to_Tag {
                                    interface _0 {
                                        public const string name = "Name";
                                        public const string Description = "Description";
                                    }
                                    interface _1 {
                                        public const string name = "Name";
                                        public const string Description = "Description";
                                    }
                                }
                                interface other_path_to_other_Tag {
                                    interface _0 {
                                        public const long name = 1;
                                        public const string Description = "Description";
                                    }
                                    interface _1 {
                                        public const long value = 1;
                                        public const string Description = "Description";
                                    }
                                }
                            }
                           */

                var attrType   = (INamedTypeSymbol)group.Key!;
                var roslynName = attrType.ToString()!;

                // Match Reflection data by full name (Reflection uses +, Roslyn uses .)
                var matchedMeta = reflectionData
                                  .Where(d => d.AttributeType.FullName?.Replace('+', '.') == roslynName)
                                  .ToList();

                if( matchedMeta.Count == 0 ) continue;

                var displayName = attrType.Name.EndsWith("Attribute") ?
                                      attrType.Name[..^9] :
                                      attrType.Name;

                // SCENARIO: Single instance, simple value -> Inline as a constant
                if( group.Count() == 1 && matchedMeta.Count == 1 &&
                    (group.First().Syntax.ArgumentList == null || group.First().Syntax.ArgumentList.Arguments.Count < 2) )
                {
                    MapAttributeToConstants(group.First().Syntax, matchedMeta[0], _constants_, true, displayName);
                    continue;
                }

                // SCENARIO: Multiple instances or complex arguments -> Create container Pack
                var container = CreateAttributeContainer(displayName, this);
                for( var i = 0; i < matchedMeta.Count; i++ )
                {
                    var targetList = container._constants_;
                    if( matchedMeta.Count > 1 ) // Multiple [Attr] instances: create _0, _1 sub-containers
                    {
                        var instancePack = CreateAttributeContainer($"_{i}", container);
                        targetList = instancePack._constants_;
                    }

                    var syntax = group.ElementAtOrDefault(i)?.Syntax;
                    if( syntax != null ) MapAttributeToConstants(syntax, matchedMeta[i], targetList, false, displayName);
                }
            }
        }

        /// <summary>
        /// Bridges Roslyn syntax and Reflection metadata to create protocol constants from C# attributes.
        /// </summary>
        private void MapAttributeToConstants(
            AttributeSyntax                                  attrSyntax,
            System.Reflection.CustomAttributeData            data,
            List<ProjectImpl.HostImpl.PackImpl.ConstantImpl> dst,
            bool                                             isInline,
            string                                           attrDisplayName)
        {
            // 1. Handle argument-less attributes (e.g., [Deprecated]) as a Boolean 'True'.
            if( attrSyntax.ArgumentList == null || attrSyntax.ArgumentList.Arguments.Count == 0 )
            {
                // [Resumable] with no argument on a State means "hold, inherit the Connection's minutes": a -1 sentinel
                // int named `resume_` (the same marker the positional path renames to), which the generator resolves to
                // the connection value. Every other argument-less attribute stays a Boolean 'True'.
                var resumable = attrDisplayName == "Resumable";
                var type      = model.Compilation.GetSpecialType(resumable ? SpecialType.System_Int32 : SpecialType.System_Boolean);
                var val       = resumable ? (object)-1 : true;
                var fld = new ProjectImpl.HostImpl.PackImpl.ConstantImpl(project, model, type, null, val)
                          {
                              idx   = ProjectImpl.root_project.constant_fields.Count,
                              _name = resumable ? "resume_" : attrDisplayName
                          };
                dst.Add(fld);
                ProjectImpl.root_project.constant_fields.Add(fld);
                return;
            }

            // 2. Resolve the Constructor to get parameter names for positional arguments
            var ctor = model.GetSymbolInfo(attrSyntax).Symbol as IMethodSymbol;

            // 3. Process Positional Arguments
            for( var i = 0; i < data.ConstructorArguments.Count; i++ )
            {
                var typedVal = data.ConstructorArguments[i];
                var param    = ctor?.Parameters.ElementAtOrDefault(i);
                try
                {
                    var argName = param?.Name ?? $"arg{i}";
                    var argType = param?.Type ?? model.Compilation.GetSpecialType(SpecialType.System_Object);

                    // Reflection provides the evaluated constant value (even for enums or arrays)
                    var val = UnpackAttributeValue(typedVal.Value);

                    var constant = new ProjectImpl.HostImpl.PackImpl.ConstantImpl(project, model, argType, attrSyntax.ArgumentList.Arguments[i].Expression, val)
                                   {
                                       idx = ProjectImpl.root_project.constant_fields.Count,
                                       // If inline (single arg), name the constant after the Attribute itself.
                                       // Otherwise, use the parameter name.
                                       _name = isInline ?
                                                   attrDisplayName == "Resumable" ?
                                                       "resume_" : // the marker the generator keys on: the minutes a parked session waits, on a connection or a state
                                                       attrDisplayName :
                                                   argName
                                   };
                    dst.Add(constant);
                    ProjectImpl.root_project.constant_fields.Add(constant);
                }
                catch( Exception e )
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            // 4. Process Named Arguments (e.g., [MyAttr(Priority = 10)])
            foreach( var namedArg in data.NamedArguments )
            {
                var argName = namedArg.MemberName;

                // Find the type of the property/field being assigned
                var attrType = model.GetSymbolInfo(attrSyntax).Symbol?.ContainingType;
                var member   = attrType?.GetMembers(argName).FirstOrDefault();
                var argType = member switch
                              {
                                  IPropertySymbol p => p.Type,
                                  IFieldSymbol f    => f.Type,
                                  _                 => model.Compilation.GetSpecialType(SpecialType.System_Object)
                              };

                var constant = new ProjectImpl.HostImpl.PackImpl.ConstantImpl(project, model, argType, null, UnpackAttributeValue(namedArg.TypedValue.Value))
                               {
                                   idx   = ProjectImpl.root_project.constant_fields.Count,
                                   _name = argName
                               };
                dst.Add(constant);
                ProjectImpl.root_project.constant_fields.Add(constant);
            }
        }

        /// <summary>
        /// Recursively converts Reflection's CustomAttributeTypedArgument values into
        /// simple objects or arrays that the Protocol Generator can serialize.
        /// </summary>
        private object? UnpackAttributeValue(object? value)
        {
            if( value is System.Collections.ObjectModel.ReadOnlyCollection<System.Reflection.CustomAttributeTypedArgument> coll )
            {
                // Attribute argument is an array (e.g., Roles = new[] {"Admin", "User"})
                return coll.Select(v => UnpackAttributeValue(v.Value)).ToArray();
            }

            // If the value is a Type object (typeof(X)), we store the string name for the protocol
            if( value is System.Type t ) return t.FullName;

            return value;
        }

        // Attributes whose presence is .NET infrastructure (used by the compiler, the
        // reflection runtime, or the user's own attribute-on-attribute decorations) and
        // therefore carry no protocol-transmittable meaning. Skipping these prevents
        // their arguments from being routed through ConstantImpl, which would otherwise
        // reject enum-typed arguments like `AttributeTargets.Class` from [AttributeUsage].
        internal static bool IsInternalMeta(INamedTypeSymbol sym) => sym.ToString()!.StartsWith("org.unirail.Meta.") && !sym.Name.Contains("Timeout") && sym.Name != "ResumableAttribute" ||
                                                                     sym.Name                            == "FlagsAttribute"                          ||
                                                                     sym.ContainingNamespace?.ToString() == "System";

        /// <summary>
        /// TAGS for the sub-packs of a stream-chain container, carried in <c>Pack.id</c>. Existing `DataType` values
        /// are reused: as a pack id none of them can mean a data type, and a contiguous run lets the reader recognize
        /// "this pack is a chain entry" with one range test. The container itself keeps <c>t_constants</c>.
        /// <code>
        /// t_bool   (65531) trim: ToStream    — the receiver stops here and takes raw bytes
        /// t_int8   (65530) trim: FromStream  — the sender supplies raw bytes here
        /// t_binary (65529) stage: compression
        /// t_uint8  (65528) stage: cipher
        /// t_int16  (65527) stage: no role — a plain byte transform
        /// </code>
        /// </summary>
        internal const ushort tag_ToStream    = (ushort)Project.Host.Pack.Field.DataType.t_bool;
        internal const ushort tag_FromStream  = (ushort)Project.Host.Pack.Field.DataType.t_int8;
        internal const ushort tag_compression = (ushort)Project.Host.Pack.Field.DataType.t_binary;
        internal const ushort tag_cipher      = (ushort)Project.Host.Pack.Field.DataType.t_uint8;
        internal const ushort tag_stage       = (ushort)Project.Host.Pack.Field.DataType.t_int16;

        private ProjectImpl.HostImpl.PackImpl CreateAttributeContainer(string name, HasDocs parent, ushort id = (ushort)Project.Host.Pack.Field.DataType.t_constants)
        {
            var packs = ProjectImpl.root_project.packs;
            var pack = new ProjectImpl.HostImpl.PackImpl(ProjectImpl.root_project) // container belongs to root_project (added to its packs); the calling entity's `project` may be unset in early passes
                       {
                           _id               = id,
                           idx               = packs.Count,
                           across_idx        = (ushort)packs.Count,
                           _name             = name,
                           parent_artificial = parent
                       };
            packs.Add(pack);
            return pack;
        }

        /// <summary>
        /// The stage, flow and trim attributes written on a pack or on a connection, in source order. Their chain is not built
        /// where they are read: whether the chain of a connection runs over its stream or is applied to each of its packs is
        /// known only when the packs of every connection and the resumables are - see the region "Chains of packs and
        /// connections". On a pack that took the chain of a connection these are the attributes of that connection.
        /// </summary>
        internal List<(SemanticModel model, AttributeSyntax attr, string? inherited_from)>? _chain_attrs;

        /// <summary>
        /// The connection whose chain this pack took, or null: the chain is the pack's own, or there is none.
        /// </summary>
        internal ProjectImpl.ConnectionImpl? _chain_of;

        /// <summary>
        /// Recognizes a stream transform chain (stage attributes in source order — which is DATAFLOW order, leaf → wire;
        /// StreamFlowAttribute flows expanded to their declared stages) applied to this field or pack. The list is
        /// reversed to wire-first before anything is built from it, so the transported representation is unchanged by
        /// the direction the user writes in. Validates roles (at most one compressor, one cipher) and
        /// the field target, then materializes the chain once as a shared t_constants container (D1) whose ordered
        /// ref-constants point at per-stage sub-packs holding the stage's full parameter list (D2). A field references it
        /// via inT; reuses existing pack/constant machinery — no new wire schema.
        /// <para>Each sub-pack carries the UNION of all the stage attribute's constructor parameters — every declared param
        /// is present, typed (a cipher's Binary[,] key -> t_binary, a byte? -> nullable t_uint8, an int level -> t_int32),
        /// with its compile-time value, or a null value meaning the param is runtime-injected (the user must supply it).</para>
        /// </summary>
        /// <param name="attrs">
        /// The chain entries in dataflow order, each with the <b>SemanticModel that binds it</b> and, when the entry was
        /// not written on this entity, the name of the pack it was <b>inherited</b> from. A field typed with a named
        /// <c>Stream</c>/<c>File</c> pack collects the pack's entries first and its own after them, and the two may come
        /// from different syntax trees — hence the per-entry model, since a model only binds nodes from its own tree.
        /// </param>
        internal void collect_stream_stages(List<(SemanticModel model, AttributeSyntax attr, string? inherited_from)> attrs)
        {
            var any_model = attrs[0].model; // for compilation-wide lookups only (special types, ConstantImpl) — never to bind a node

            string disp(INamedTypeSymbol t) => t.Name.EndsWith("Attribute") ?
                                                   t.Name[..^"Attribute".Length] :
                                                   t.Name;

            // A stage may be written on this entity, or INHERITED from the named Stream/File pack a field is typed with.
            // Every conflict diagnostic below appends this: without it a clash with an inherited stage is baffling —
            // the offending stage appears nowhere in the declaration the message points at.
            string src(string? from) => from == null ?
                                            "" :
                                            $" (inherited from the conduit pack `{from}`)";

            string role(INamedTypeSymbol t) => ProjectImpl.derives_from(t, ProjectImpl.Meta_StreamCompression) ? "compression" :
                                               ProjectImpl.derives_from(t, ProjectImpl.Meta_StreamCipher)      ? "cipher" : "stage";

            // Resolve a stage-attribute parameter's declared type to the DataType-bearing symbol fed to ConstantImpl:
            // unwrap a Binary[,] array to its Binary element, and a Nullable<T> (e.g. byte?) to T (marking nullable).
            ITypeSymbol param_type(ITypeSymbol pt, out bool nullable)
            {
                nullable = false;
                if( pt is IArrayTypeSymbol arr ) pt = arr.ElementType; // Binary[,] -> Binary
                if( pt is INamedTypeSymbol nt && nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T )
                {
                    nullable = true;
                    pt       = nt.TypeArguments[0]; // byte? -> byte
                }

                return pt;
            }

            // Values supplied by the APPLIED inline ctor (arg syntax; omitted optionals -> ctor default). name -> value.
            // `model` is the one that binds `a` — the entry's own, never a neighbour's (they may be in different trees).
            Dictionary<string, object?> applied_syntax(SemanticModel model, IMethodSymbol ctor, AttributeSyntax a)
            {
                var map  = new Dictionary<string, object?>();
                var args = a.ArgumentList?.Arguments;
                for( var i = 0; i < ctor.Parameters.Length; i++ )
                {
                    var     p = ctor.Parameters[i];
                    object? v = null;
                    if( args is { } aa && i < aa.Count )
                    {
                        var cv              = model.GetConstantValue(aa[i].Expression);
                        if( cv.HasValue ) v = cv.Value;
                    }
                    else if( p.HasExplicitDefaultValue ) v = p.ExplicitDefaultValue;

                    map[p.Name] = v;
                }

                return map;
            }

            // Values supplied by a flow's declared stage (Roslyn AttributeData). name -> value.
            Dictionary<string, object?> applied_data(AttributeData d)
            {
                var map = new Dictionary<string, object?>();
                var ps  = d.AttributeConstructor?.Parameters ?? default;
                for( var i = 0; i < ps.Length; i++ )
                    map[ps[i].Name] = i < d.ConstructorArguments.Length ? d.ConstructorArguments[i].Value : ps[i].HasExplicitDefaultValue ? ps[i].ExplicitDefaultValue : null;
                return map;
            }

            // The UNION of every constructor's parameters (ordered by ascending ctor arity, deduped by name): each param
            // carries name + resolved type + nullable + the value the application supplied, or null = runtime-injected.
            // A name that recurs across constructors must keep the same type (see the conflict check below).
            List<(string name, ITypeSymbol type, bool nullable, object? val)> union_params(INamedTypeSymbol t, Dictionary<string, object?> applied)
            {
                var outp = new List<(string, ITypeSymbol, bool, object?)>();
                // name -> the FIRST declaration we unioned (its resolved type + nullability + the param symbol, kept for
                // the diagnostic's source location). A HashSet would only tell us a name repeats; we need the prior
                // declaration to tell a benign repeat from a conflicting one.
                var seen = new Dictionary<string, (ITypeSymbol type, bool nul, IParameterSymbol p)>();
                foreach( var cc in t.InstanceConstructors.OrderBy(cc => cc.Parameters.Length) )
                    foreach( var p in cc.Parameters )
                    {
                        var pt = param_type(p.Type, out var nul);
                        if( seen.TryGetValue(p.Name, out var prev) )
                        {
                            // Arg-name uniqueness per stage. A name may repeat across a stage's constructors ONLY when it
                            // denotes the SAME declared type — the documented telescoping/buffer-overload pattern, e.g.
                            // `MyStage(int level=1)` + `MyStage(int level, Binary[,] key)`, where `level` legitimately
                            // recurs so the attribute form and the runtime-buffer form share it. The SAME name with a
                            // DIFFERENT type is a genuine duplicate: each param becomes one uniquely-named constant field
                            // in the generated stage sub-pack, so two conflicting declarations cannot both be emitted —
                            // the current union would silently keep the first and drop the other. This can arise ONLY on a
                            // user-defined stage (built-in stages have no conflicting repeats); force the user to rename.
                            if( !SymbolEqualityComparer.Default.Equals(prev.type, pt) || prev.nul != nul )
                            {
                                var l1 = (prev.p.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line + 1)?.ToString() ?? "?";
                                var l2 = (p.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line      + 1)?.ToString() ?? "?";
                                AdHocAgent.exit($"Stream stage '{disp(t)}' ({t}) declares parameter '{p.Name}' with conflicting types across its constructors: "   +
                                                $"'{prev.type}'{(prev.nul ? "?" : "")} (line {l1}) vs '{pt}'{(nul ? "?" : "")} (line {l2}). "                      +
                                                "A stage parameter name must denote a single type — each param is emitted as one constant field in the generated " +
                                                "stage, so a name cannot carry two meanings (one declaration would be silently dropped). Rename one of them.", 2);
                            }

                            continue; // same-typed repeat: already unioned from the first (lower-arity) constructor
                        }

                        var val = applied.GetValueOrDefault(p.Name);
                        seen[p.Name] = (pt, nul, p);
                        // Param rule (by value): a param with no value and no default is RUNTIME-injected. The user may
                        // declare it as a non-nullable type and never assign it — nullability is OUR internal process:
                        // we represent every runtime param as nullable (and apply null). Design-time params carry their value.
                        outp.Add((p.Name, pt, nul || val == null, val));
                    }

                return outp;
            }

            // 1. resolve attrs (source order) into ordered entries, expanding flows in place. An entry is either a
            //    transform STAGE (carrying its full arg list) or a TRIM marker (carrying the endpoint argument(s) of
            //    its ToStream / FromStream side); a trim's whole meaning is the position it occupies among the stages.
            //    `from` propagates the entry's origin (null = written right here) into every diagnostic.
            var entries = new List<(INamedTypeSymbol type, List<(string name, ITypeSymbol type, bool nullable, object? val)> args, ITypeSymbol? cut_to, ITypeSymbol? cut_from, string? from)>();

            // classify one trim attribute type into its (ToStream endpoint, FromStream endpoint) argument pair
            (ITypeSymbol? to, ITypeSymbol? from) trim_of(INamedTypeSymbol t) => ProjectImpl.trim_of(t, symbol?.ToString() ?? _name, line_in_src_code);

            foreach( var (entry_model, a, inherited_from) in attrs )
            {
                if( entry_model.GetSymbolInfo(a).Symbol is not IMethodSymbol ctor ) continue; // bound by ITS OWN model — the entries may span syntax trees
                var t = ctor.ContainingType;
                if( ProjectImpl.is_stream_flow(t) )
                {
                    foreach( var fa in t.GetAttributes() ) // a flow's own attributes ARE its declared entry list
                        if( fa.AttributeClass is { } fc )
                            if( ProjectImpl.is_stream_stage(fc) ) entries.Add((fc, union_params(fc, applied_data(fa)), null, null, inherited_from));
                            else if( ProjectImpl.is_stream_trim(fc) )
                            {
                                var (to, from) = trim_of(fc);
                                entries.Add((fc, [], to, from, inherited_from));
                            }
                }
                else if( ProjectImpl.is_stream_stage(t) ) entries.Add((t, union_params(t, applied_syntax(entry_model, ctor, a)), null, null, inherited_from));
                else if( ProjectImpl.is_stream_trim(t) )
                {
                    var (to, from) = trim_of(t);
                    entries.Add((t, [], to, from, inherited_from));
                }
            }

            if( entries.Count == 0 ) return;

            // 1a. Compression is taken off a pack that cannot grow to the size where it pays: a compressor spends microseconds
            //     on a frame however short, and a short pack hardly shrinks. The other stages of the chain stay. Both sides
            //     get the same chain: the pack has one. A chain with a trim is left as written - what its cut hands over
            //     is named by the stages around it. `_DefaultMaxLengthOf.Uncompressed` says how long a pack may be to go
            //     uncompressed; 0 - none is.
            if( this is ProjectImpl.HostImpl.PackImpl packed && 0 < ProjectImpl.HostImpl.PackImpl.FieldImpl._DefaultMaxLengthOf.Uncompressed && entries.All(e => e.cut_to == null && e.cut_from == null) &&
                !(symbol is INamedTypeSymbol declared_as && (ProjectImpl.is_stream_flow(declared_as) || ProjectImpl.is_stream_stage(declared_as) || ProjectImpl.is_stream_trim(declared_as))) ) // a flow and a stage are declared as classes too: they are not packs
            {
                var at = entries.FindIndex(e => ProjectImpl.derives_from(e.type, ProjectImpl.Meta_StreamCompression));
                if( at != -1 )
                {
                    var bound = packed.upper_bound_bytes([]);
                    if( -1 < bound && bound <= ProjectImpl.HostImpl.PackImpl.FieldImpl._DefaultMaxLengthOf.Uncompressed )
                    {
                        AdHocAgent.LOG.Warning("The pack {pack} (line {line}) is {bound} bytes at most: the compression {stage:l} is taken off it, the rest of its chain stays. A pack that cannot be longer than {max} bytes goes uncompressed; `_DefaultMaxLengthOf.Uncompressed` sets that length, 0 compresses every pack.",
                                               symbol?.ToString() ?? _name, line_in_src_code, bound, disp(entries[at].type), ProjectImpl.HostImpl.PackImpl.FieldImpl._DefaultMaxLengthOf.Uncompressed);
                        entries.RemoveAt(at);
                        if( entries.Count == 0 ) return;
                    }
                }
            }

            // A chain of trim markers ALONE is the ordinary case, not an error: `[ToStream<E>] Pack fld;` hands the
            // plain serialized payload over as opaque bytes with no transform in between — a cut at position 0 of an
            // empty stage list.

            // 1b. DECLARATION ORDER IS DATAFLOW ORDER — left = app/leaf, right = wire. `[Zstd, ChaCha20]`
            //     reads `pack -> Zstd -> ChaCha20 -> wire`: the leftmost stage receives the serialized bytes
            //     first, the rightmost hands them to the wire (and the receiver runs the list backwards).
            //     A flow expands in place and its own list obeys the very same convention, so reversing the
            //     FLATTENED list is also exactly right for a mixed `[SomeStage, SomeFlow]` application.
            //     Reversing HERE, before anything downstream looks at the list, keeps the canonical dedup key
            //     and the materialized container's ordered stage refs in the WIRE-FIRST order they have always
            //     been in — the transmitted representation is byte-for-byte unchanged; only the direction the
            //     user writes the chain in has flipped.
            //     A trim rides in this same list, so reversing it keeps each cut between the very stages it was
            //     written between — its stored position, like the stages, is counted from the wire.
            var declared = entries.ToList(); // the order the user actually wrote — for the log line and the position checks
            entries.Reverse();

            // transform stages alone: every role/uniqueness rule below is about transforms, never about a trim
            var stages = entries.Where(e => e.cut_to == null && e.cut_from == null).Select(e => (e.type, e.args, e.from)).ToList();

            var where = symbol?.ToString() ?? _name;

            // 2. validate roles (the role itself now travels, as the stage sub-pack's `id` tag — see CreateAttributeContainer)
            //    Both offenders are named, with their origin: one of them may have been inherited from a conduit pack
            //    and so appear nowhere in the declaration this message points at.
            void one_of_a_role(INamedTypeSymbol role_base, string role_name)
            {
                var found = stages.Where(r => ProjectImpl.derives_from(r.type, role_base)).ToList();
                if( found.Count < 2 ) return;
                AdHocAgent.exit($"The stream chain on {where} (line {line_in_src_code}) has more than one {role_name}: " +
                                string.Join(" and ", found.Select(r => $"'{disp(r.type)}'{src(r.from)}"))               +
                                $". At most one {role_name} is allowed per chain.", 2);
            }

            one_of_a_role(ProjectImpl.Meta_StreamCompression, "compressor");
            one_of_a_role(ProjectImpl.Meta_StreamCipher,      "cipher");

            // 1c. the Zstd level is the one knob that decides whether a chain helps a wire or hurts it: 1–6 are wire
            //     levels (hundreds of MB/s), 13 and above are archival settings for files at rest — level 20 runs at
            //     about 10 MB/s, and a 256 KiB reply behind it cost the sending thread 25 ms in the CQL-over-AdHoc
            //     benchmark, twenty times the native transport. The attribute's default is 3, zstd's own. A level
            //     outside zstd's 1..22 is an error; an archival one is a warning, since a description may mean it.
            foreach( var r in declared )
                if( r.type.ToDisplayString() == "org.unirail.Meta.ZstdAttribute" && r.args.FirstOrDefault(p => p.name == "level").val is int level )
                {
                    if( level < 1 || 22 < level )
                        AdHocAgent.exit($"The Zstd stage on {where} (line {line_in_src_code}){src(r.from)} asks for level {level}; zstd knows levels 1 to 22 (3 is its default and the level for a wire).", 2);
                    if( 12 < level )
                        AdHocAgent.LOG.Warning("The Zstd stage on {where:l} (line {line}){from:l} asks for level {level}: levels above 12 are archival settings that compress at a few MB/s and stall the sender. For a wire use 1 to 6; [Zstd] alone means 3.",
                                               where, line_in_src_code, src(r.from), level);
                }

            // 2a. compress-then-encrypt is the only sensible pairing, so in declaration order (leaf -> wire) the
            //     compressor must stand BEFORE the cipher. The reverse encrypts first and then feeds the ciphertext
            //     to the compressor, which cannot shrink it — pure CPU on both ends for nothing. It is also exactly
            //     how a chain written for the old wire-first convention reads now, so this doubles as the migration
            //     signal. Role-less stages (e.g. [Base64]) are unconstrained: only this one pair has a right order.
            {
                var cipher_at = declared.FindIndex(r => ProjectImpl.derives_from(r.type, ProjectImpl.Meta_StreamCipher));
                var press_at  = declared.FindIndex(r => ProjectImpl.derives_from(r.type, ProjectImpl.Meta_StreamCompression));
                if( 0 <= cipher_at && 0 <= press_at && cipher_at < press_at )
                    AdHocAgent.exit($"The stream chain on {where} (line {line_in_src_code}) encrypts before it compresses: the cipher '{disp(declared[cipher_at].type)}'{src(declared[cipher_at].from)} stands to the left of the compressor '{disp(declared[press_at].type)}'{src(declared[press_at].from)}. " +
                                    $"A chain is written in dataflow order — left = app/leaf, right = wire — so the compressor comes first: [{disp(declared[press_at].type)}, {disp(declared[cipher_at].type)}]. Compressing ciphertext gains nothing. "                                                       +
                                    "(A stage inherited from a conduit pack always sits to the RIGHT of the field's own — the pack declares how its type reaches the wire, the field adds a transform inside that. If the order is wrong, the two must be written in one place: move the stage onto the pack, or off it.)", 2);
            }

            // 2b. stage-name uniqueness within the chain. Every stage materializes as a sub-pack AND a chain
            //     ref-constant named after the attribute (disp(type) — see the CreateAttributeContainer /
            //     ConstantImpl calls below, both keyed on disp(r.type)). The generated `chain_N` builder likewise
            //     gives each stage a slot by that name. Two stages sharing a display name would emit two same-named
            //     constants into one container (one silently dropped) and collide in the builder. The role checks
            //     above only forbid >1 compressor and >1 cipher; a plain `stage`-role repeat (e.g. [Base64, Base64])
            //     or two distinct stage types whose simple names coincide across namespaces (a.Foo + b.Foo -> "Foo")
            //     still reach here. Stop and force a rename — the name is the chain's only handle on the stage.
            {
                var seen_stage = new Dictionary<string, (INamedTypeSymbol type, string? from)>();
                foreach( var r in stages )
                {
                    var nm = disp(r.type);

                    // `ToStream` / `FromStream` name the trim sub-packs in the very same container, so a stage may not claim them
                    if( entries.Any(e => e.cut_to != null || e.cut_from != null) && nm is "ToStream" or "FromStream" )
                        AdHocAgent.exit($"The stream chain on {where} (line {line_in_src_code}) has a stage named '{nm}'{src(r.from)}, which is the name this chain's trim marker already takes in the generated container. Rename the stage.", 2);

                    if( seen_stage.TryGetValue(nm, out var prev) )
                    {
                        if( SymbolEqualityComparer.Default.Equals(prev.type, r.type) )
                            AdHocAgent.exit($"The stream chain on {where} (line {line_in_src_code}) applies the stage '{nm}' ({r.type}) more than once{src(prev.from)}{src(r.from)}. " +
                                            "Each stage may appear at most once per chain — it is materialized as a single sub-pack named after the attribute, so a repeat would collide. Remove the duplicate.", 2);

                        var l1 = (prev.type.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line + 1)?.ToString() ?? "?";
                        var l2 = (r.type.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line    + 1)?.ToString() ?? "?";
                        AdHocAgent.exit($"The stream chain on {where} (line {line_in_src_code}) contains two different stages that share the display name '{nm}': " +
                                        $"'{prev.type}' (line {l1}){src(prev.from)} and '{r.type}' (line {l2}){src(r.from)}. "                                      +
                                        "Each stage is materialized as a sub-pack named after the attribute, so stage names must be unique within a chain. Rename one of them.", 2);
                    }

                    seen_stage[nm] = (r.type, r.from);
                }
            }

            // 2c. param-name uniqueness ACROSS the whole chain. The chain's structure (stage names + order) is
            //     encoded separately from its call-info ("with what params to call"). Every stage's params are
            //     flattened into that one call scope and addressed by name, so a name must be unique across the
            //     ENTIRE chain, not merely within a single stage's constructors (union_params guards the latter).
            //     Two stages each declaring e.g. `level`, or a `key`, would collide in the flattened call list.
            //     This can only happen on user-defined stages; force a rename.
            {
                var seen_param = new Dictionary<string, (INamedTypeSymbol type, string? from)>(); // param name -> the stage that first declared it
                foreach( var r in stages )
                    foreach( var p in r.args )
                        if( seen_param.TryGetValue(p.name, out var owner) )
                            AdHocAgent.exit($"The stream chain on {where} (line {line_in_src_code}) has two stages declaring a parameter named '{p.name}': "     +
                                            $"'{disp(owner.type)}' ({owner.type}){src(owner.from)} and '{disp(r.type)}' ({r.type}){src(r.from)}. "               +
                                            "A chain's parameters share one flat call scope, so every parameter name must be unique across the whole chain. Rename one of them.", 2);
                        else
                            seen_param[p.name] = (r.type, r.from);
            }

            // 3. field target gate: only a pack-reference field, a bare Stream field, or a single string field may
            //    carry a chain — or a trim, which is the same list: whatever a stage may wrap, a cut may hand over.
            //    A plain string's varint-char bytes are the leaf; a TRIMMED string field hands its UTF-8 form over on
            //    the cut leg — see the README's "String payloads - UTF-8 at the boundary". Strings inside
            //    collections/maps/sets are NOT eligible.
            //    A field typed with a Value pack or an enum is a primitive on the wire: every language writes it in
            //    place, so a stage has no framing to ride on and a cut no boundary to hand bytes over at — README,
            //    "Where a chain may sit". Only a pack that is a Value pack FOR CERTAIN is refused here (`IsValuePack`).
            if( this is ProjectImpl.HostImpl.PackImpl.FieldImpl fg )
            {
                var payload = fg.get_exT_pack;
                if( payload != null && (payload.is_enum || payload.is_constants_set || payload.IsValuePack()) )
                    AdHocAgent.exit($"A stream transform chain (or a trim) on field '{where}' (line {line_in_src_code}) is not allowed: its type '{payload.symbol}' is " +
                                    (payload.is_enum || payload.is_constants_set ?
                                         "an enum" :
                                         "a Value pack") +
                                    ", which every language writes in place — there is no framing for a stage and no boundary for a cut. Transmit it as it is, or group it with more data in an ordinary pack and put the chain there.", 2);

                if( !(payload != null                                                     ||
                      fg.exT_primitive == (int)Project.Host.Pack.Field.DataType.t_stream ||
                      (fg.exT_primitive == (int)Project.Host.Pack.Field.DataType.t_string &&
                       !fg.is_Set && !fg.is_Map && fg._dims_len == 0 && fg._exT_array == null && fg._map_set_array == null)) )
                    AdHocAgent.exit($"A stream transform chain (or a trim) on field '{where}' (line {line_in_src_code}) is allowed only on a pack-typed field, a bare Stream field, or a single string field — not on a File / primitive / value-pack field or a collection. Group the data inside a pack and put the chain on the pack.", 2);
            }

            // 3b. pack target gate: only a `File` pack is forbidden a chain — and a TRIM with it, for the same reason.
            //     A File is a single length-prefixed conduit: there is no per-chunk framing for a byte transform to ride
            //     on, a length-changing stage (compression) has no length field to grow, and a cut has no chunk boundary
            //     at which to hand bytes over (a File payload is raw bytes on both ends anyway). Both a `Stream` pack
            //     (chunked) AND an ordinary pack (transparently compressed/encrypted on every transmission) may carry a
            //     chain; in both cases the pack reaches it via `_link` -> the t_constants/referred container.
            //     A File pack therefore has nothing to pass down to a field typed with it.
            if( this is ProjectImpl.HostImpl.PackImpl { is_File: true } )
                AdHocAgent.exit($"A transform chain on File pack '{where}' (line {line_in_src_code}) is not allowed — a File is a single length-prefixed conduit with no room for a byte-transform stage, nor a boundary at which a trim could hand bytes over. Inherit `Stream` (chunked), or apply the chain to an ordinary or Stream pack.", 2);

            // 3b2. No pack-MODIFIER gate: a chain written on a `Modify<Target>` pack is the TARGET's chain. The
            //      modifier hands its whole attribute set over in `Adopt_Attributes_Onto`, and the target reaches
            //      this method through that redirection — `where` below already names the target, not the modifier.
            //      This is the only way to re-chain a pack imported from a project whose author cannot know which
            //      connections a later extension will add.

            // 3c. connection target: no gate. ANY connection may carry a chain — a virtual connection wraps the bytes
            //     its endpoints exchange end-to-end through PATH, a physical Connects<> wraps the bytes of that one hop.
            //     The dedup key below still distinguishes the two shapes (no actors = plain tunnel, actors = serving hosts).

            // 3d. TRIM rules. Positions are read in DECLARATION order here — that is how the user wrote them and how
            //     every diagnostic below has to speak.
            var cuts_to   = declared.FindAll(e => e.cut_to   != null);
            var cuts_from = declared.FindAll(e => e.cut_from != null);
            if( 0 < cuts_to.Count + cuts_from.Count )
            {
                if( 1 < cuts_to.Count )
                    AdHocAgent.exit($"The stream chain on {where} (line {line_in_src_code}) cuts the receiving side twice. At most one ToStream trim per chain — one payload cannot be handed over at two depths at once.", 2);
                if( 1 < cuts_from.Count )
                    AdHocAgent.exit($"The stream chain on {where} (line {line_in_src_code}) cuts the sending side twice. At most one FromStream trim per chain.", 2);

                // Both directions must cut at the SAME depth. Different depths would leave the store holding one form
                // and owing another, so it would have to run the stages in between — re-adding to it exactly what the
                // deeper cut was there to remove. `Stream<To, From>` expresses the intended shape in one marker.
                if( cuts_to.Count == 1 && cuts_from.Count == 1 )
                {
                    var to_at   = declared.FindIndex(e => e.cut_to   != null);
                    var from_at = declared.FindIndex(e => e.cut_from != null);
                    if( to_at != from_at )
                        AdHocAgent.exit($"The stream chain on {where} (line {line_in_src_code}) puts its ToStream and FromStream trims at different depths. " +
                                        "A store that receives one form and must send another has to transform between them — which re-adds the very stages the cut removed. " +
                                        "Put both at one depth, written as a single `Stream<To, From>` marker.", 2);
                }

                // A trim only makes sense where one side holds a typed value the other may skip: a field or a pack.
                // On a connection the chain wraps everything that link carries, and "this endpoint holds bytes" is
                // what a relay already is.
                if( this is ProjectImpl.ConnectionImpl )
                    AdHocAgent.exit($"The chain on connection '{where}' (line {line_in_src_code}) carries a trim. A trim cuts the chain of one field or one pack; a connection chain wraps everything the link carries, " +
                                    "where an endpoint holding raw bytes is simply a relay. Move the trim onto the pack (or field) whose payload the endpoint should keep opaque.", 2);

                // Warn if the payload being handed over as opaque bytes is barely bigger than a Value pack: its data fits
                // in 64 bits and only its null bits push it over — a Value pack itself was already refused at gate 3.
                // The cut is legal and fires, but the framing it needs costs more than the payload it wraps.
                if( this is ProjectImpl.HostImpl.PackImpl.FieldImpl { get_exT_pack: ProjectImpl.HostImpl.PackImpl payload } && payload.MayByValuePack() )
                    AdHocAgent.LOG.Warning("The trimmed field '{field}' (line: {line}) hands over '{pack}', which is small enough to be a Value pack — streaming it costs more than transmitting it.",
                                           where, line_in_src_code, payload.symbol);
            }

            // 3e. Resolve every trim's endpoint argument — through the shared, cached `ProjectImpl.resolve_cut`, so a
            //     field's cut is NOT resolved a second time here: the "Record the legs each field is cut on" pass has
            //     already asked for the same argument and the answer (with its diagnostics) comes out of the cache.
            //     A cut rides as the connection index, `conn.idx` for the Left host and `~conn.idx` for the Right — the
            //     same encoding the field's `cut_to_codes` / `cut_from_codes` and the `Connection.ToStream` projection use.
            (string show, List<(string name, long val)> encoded) cut(ITypeSymbol arg, string side) => ProjectImpl.resolve_cut(arg, side, where, line_in_src_code);

            // 4. canonical key + display (param value, or "runtime" when null — so valued vs runtime-injected configs never collide)
            string pval(object? v) => v?.ToString() ?? "runtime";


            var key = this switch
                      {
                          ProjectImpl.HostImpl.PackImpl.FieldImpl fld => fld.exT_pack == null ?
                                                                             "std|" :
                                                                             "datatype|",
                          ProjectImpl.HostImpl.PackImpl => "std|",
                          ProjectImpl.ConnectionImpl conn => conn.actors.Count == 0 ? // plain tunnel
                                                                 "std|" :
                                                                 "extra|", // tunnel serve hosts
                          _ => "std|"
                      } + string.Join("|", entries.Select(st => st.cut_to != null || st.cut_from != null ?
                                                                    // a trim contributes its kind, its endpoint ARGUMENT and — by sitting here — its position,
                                                                    // so two otherwise identical chains cutting at different depths never share a container
                                                                    $"trim({st.cut_to?.ToString() ?? "-"},{st.cut_from?.ToString() ?? "-"})" :
                                                                    st.type.Name + "(" + string.Join(",", st.args.Select(arg => $"{arg.name}:{arg.type.Name}{(arg.nullable ? "?" : "")}={pval(arg.val)}")) + ")"));

            // reported in DECLARATION order (leaf -> wire), i.e. exactly as the user wrote the chain
            var logLine = "leaf -> " + string.Join(" -> ", declared.Select(r => r.cut_to != null || r.cut_from != null ?
                                                                                   "|cut" + (r.cut_to != null ?
                                                                                                 " ToStream(" + cut(r.cut_to, "ToStream").show + ")" :
                                                                                                 "") + (r.cut_from != null ?
                                                                                                            " FromStream(" + cut(r.cut_from, "FromStream").show + ")" :
                                                                                                            "") + "|" :
                                                                                   r.args.Count > 0 ?
                                                                                       $"{disp(r.type)}({string.Join(",", r.args.Select(p => $"{p.name}={pval(p.val)}"))})[{role(r.type)}]" :
                                                                                       $"{disp(r.type)}[{role(r.type)}]")) + " -> wire";

            // 5. dedup → one shared container per distinct config (D1: reuse the t_constants pack kind)
            var reused = ProjectImpl.stream_chains.TryGetValue(key, out var chain);
            if( !reused )
            {
                chain = CreateAttributeContainer("chain_" + ProjectImpl.stream_chains.Count, null); //parent == null ! referred by link fields
                chain!._nested_max = this switch
                                     {
                                         ProjectImpl.HostImpl.PackImpl.FieldImpl => 1,
                                         ProjectImpl.HostImpl.PackImpl           => 2,
                                         ProjectImpl.ConnectionImpl              => 3,
                                         _                                       => 0
                                     };
                chain.uid       = (ulong)idx;
                chain._referred = true; // chain-root marker: id==t_constants && referred==true is otherwise impossible (referred is only set for transmittable packs used as a field's exT type), so it uniquely tags the D1 container — never a constant-set, an enum, or a D2 stage sub-pack.
                var Int32 = any_model.Compilation.GetSpecialType(SpecialType.System_Int32);

                // The ordered ref-constant that puts a materialized sub-pack at its place in the container. Emitted
                // AFTER the sub-pack's own constants — that sequence fixes the `constant_fields` indices, so it is
                // part of the transported representation and must not be rearranged.
                void link_entry(ProjectImpl.HostImpl.PackImpl pack, string name)
                {
                    var link = new ProjectImpl.HostImpl.PackImpl.ConstantImpl(project, any_model, Int32, null, (long)pack.idx) { idx = ProjectImpl.root_project.constant_fields.Count, _name = name }; // D2: ordered ref → sub-pack idx
                    chain!._constants_.Add(link);
                    ProjectImpl.root_project.constant_fields.Add(link);
                }

                void add_const(ProjectImpl.HostImpl.PackImpl to, ITypeSymbol type, string name, object? value)
                {
                    var c = new ProjectImpl.HostImpl.PackImpl.ConstantImpl(project, any_model, type, null, value) { idx = ProjectImpl.root_project.constant_fields.Count, _name = name };
                    to._constants_.Add(c);
                    ProjectImpl.root_project.constant_fields.Add(c);
                }

                // A trim rides the container as a sub-pack named after its direction, holding one int per endpoint
                // (encoded in 3e above). No new field has to be added to the meta-protocol: the cut's DEPTH is simply
                // where this sub-pack sits in the container's ordered list, exactly like a stage.
                void trim_pack(string name, ITypeSymbol endpoints_arg)
                {
                    var pack = CreateAttributeContainer(name, chain, name == "ToStream" ?
                                                                        tag_ToStream :
                                                                        tag_FromStream);
                    foreach( var (endpoint_name, code) in cut(endpoints_arg, name).encoded )
                        add_const(pack, Int32, endpoint_name, code);

                    link_entry(pack, name);
                }

                foreach( var st in entries )
                    if( st.cut_to != null || st.cut_from != null ) // a trim: no params, no implementation — only its direction(s), endpoints and position
                    {
                        if( st.cut_to != null ) trim_pack("ToStream", st.cut_to);
                        if( st.cut_from != null ) trim_pack("FromStream", st.cut_from);
                    }
                    else
                    {
                        // stage sub-pack named after the attribute, tagged with its ROLE — which the generator could
                        // not see before: it was validated here and thrown away
                        var stage = CreateAttributeContainer(disp(st.type), chain, ProjectImpl.derives_from(st.type, ProjectImpl.Meta_StreamCompression) ?
                                                                                       tag_compression :
                                                                                       ProjectImpl.derives_from(st.type, ProjectImpl.Meta_StreamCipher) ?
                                                                                           tag_cipher :
                                                                                           tag_stage);
                        foreach( var (argName, argT, _, argVal) in st.args )        // EVERY declared param: typed, with its value or null = runtime-injected
                            add_const(stage, argT, argName, argVal);

                        link_entry(stage, disp(st.type));
                    }

                ProjectImpl.stream_chains[key] = chain;
            }


            switch( this )
            {
                // 6. reference: a field points inT at the container; a pack (and, symmetrically, a connection)
                //    points _link at it (the per-use link — set even when the container is reused, so an entity sharing a
                //    deduplicated chain still records its own reference rather than relying on the container's single
                //    `parent`, which dedup makes unreliable).
                case ProjectImpl.HostImpl.PackImpl.FieldImpl fld: fld.inT    = chain!.idx; break;
                case ProjectImpl.HostImpl.PackImpl pk:            pk._link   = (ushort)chain!.idx; break;
                case ProjectImpl.ConnectionImpl conn:             conn._link = (ushort)chain!.idx; break;
            }

            if( reused )
                AdHocAgent.LOG.Information("stream chain on {entity} (line {line}) reuses config [{chain}] (container #{idx}) — consider naming it as a StreamFlowAttribute for a readable name in the Observer.", where, line_in_src_code, logLine, chain!.idx);
            else
                AdHocAgent.LOG.Information("stream chain on {entity} (line {line}): {chain}  =>  container #{idx}", where, line_in_src_code, logLine, chain!.idx);
        }

        /// <summary>
        /// Symbol representing this entity in the Roslyn syntax tree.
        /// </summary>
        public ISymbol? symbol;
    }

    public abstract class Entity : HasDocs{
        public Entity(ProjectImpl prj, CSharpCompilation? compilation, BaseTypeDeclarationSyntax? node) : base(prj, node == null ?
                                                                                                                        "" :
                                                                                                                        node.Identifier.ToString(), node)
        {
            if( compilation == null || node == null ) return;
            this.node   = node;
            model       = compilation.GetSemanticModel(node.SyntaxTree);
            base.symbol = symbol = model.GetDeclaredSymbol(node)!;

            try
            {
                if( parent_by_source_code == prj ) parent_artificial = project.proxy;
            }
            catch( Exception e )
            {
                Console.WriteLine(e);
                throw;
            }

            entities.Add(symbol, this);
            if( !_name.Equals(symbol.Name) )
            {
                AdHocAgent.LOG.Warning("The entity '{entity}' name at the {provided_path} line: {line} is prohibited. Please correct the name manually.", symbol, AdHocAgent.provided_path, line_in_src_code);
                AdHocAgent.exit("");
            }


            foreach( var (arr, span) in from t in node.Identifier.TrailingTrivia
                                        where t.IsKind(SyntaxKind.MultiLineCommentTrivia)
                                        let m = _uid.Match(t.ToString())
                                        where m.Success
                                        select (m.Groups[1].Value.Split(' ').Select(str => str.to_base256_value()).ToArray(), t.Span) )
            {
                uid_marks.Add(span); // every mark goes when the entity moves into a table, a stray second one too
                if( 1 < uid_marks.Count ) continue; // the first mark is the one that counts

                uid = arr[0];
                if( 1 < arr.Length ) // only project may have a list of imported projects
                    // Restore the full UIDs from the delta encoding (project.uid - imported.uid).
                    // This is done to reduces source code bloat.
                    ((ProjectImpl)this).imported_projects_uid = arr.Skip(1).Select(u => uid - u).ToArray();
            }
        }


        public                 Entity?                     origin;
        public static readonly Dictionary<ISymbol, Entity> entities = new(SymbolEqualityComparer.Default);

        public BaseTypeDeclarationSyntax? node;


        public INamedTypeSymbol? symbol;

        public bool? _included;

        public virtual bool included => _included ?? false;


        //Identify and mark the scope of entities requiring re-initialization due to a detected cyclic dependency.
        uint _inited = int.MaxValue;

        internal static bool cyclic; //If a cyclic dependency is detected during initialization, re-initialization is required.
        static          uint inited_seed;
        static          uint fix_inited_seed;

        /// <summary>
        /// Starts a new initialization pass.
        /// </summary>
        public static void start()
        {
            cyclic          = false;
            fix_inited_seed = inited_seed;
        }

        /// <summary>
        /// Checks if a re-initialization pass is required due to a detected cycle.
        /// </summary>
        /// <returns><c>true</c> if a restart is needed; otherwise, <c>false</c>.</returns>
        public static bool restart()
        {
            if( !cyclic ) return false;
            inited_seed = fix_inited_seed;
            cyclic      = false;
            return true;
        }

        /// <summary>
        /// Gets a value indicating whether this entity has been fully initialized in the current pass.
        /// </summary>
        public bool inited => _inited < inited_seed;

        /// <summary>
        /// Marks this entity as fully initialized for the current pass.
        /// </summary>
        public void set_inited() => _inited = ++inited_seed;

        /// <summary>
        /// Gets the interfaces implemented by this entity that are of type `Modify<...>`, indicating what it modifies.
        /// </summary>
        public IEnumerable<INamedTypeSymbol> this_modify => symbol == null ? //if this is the virtual
                                                                [] :
                                                                symbol.Interfaces.Where(I => I.isModify());

        /// <summary>
        /// Initializes the entity, handling cyclic dependencies and modifications.
        /// </summary>
        /// <param name="once">A set used to track visited entities to detect cycles.</param>
        public virtual void Init(HashSet<object> once)
        {
            if( inited || !once.Add(this) && (cyclic = true) ) return; //Ensure the entity is initialized only once and prevent re-entry caused by cyclic references.

            if( symbol != null )
                if( !Init_As_Modifier_Dispatch_Modifications_On_Targets(once) )
                    Init_Collect_Modification(this, once);

            once.Remove(this);
            set_inited(); //Only from this point is the entity fully initialized
        }

        /// <summary>
        /// Gets or sets a value indicating whether this entity is a modifier.
        /// </summary>
        public bool is_Modifier;

        /// <summary>
        /// Initializes this entity as a modifier, dispatching its modifications to the target entities.
        /// </summary>
        /// <param name="once">A set used to track visited entities.</param>
        /// <returns><c>true</c> if this entity is a modifier and processed as such; otherwise, <c>false</c>.</returns>
        public virtual bool Init_As_Modifier_Dispatch_Modifications_On_Targets(HashSet<object> once)
        {
            foreach( var m in this_modify )
            {
                is_Modifier = true;

                var target = entities[m.TypeArguments[0]];
                Adopt_Attributes_Onto(target);
                Init_Collect_Modification(target, once);
            }

            return is_Modifier;
        }

        /// <summary>
        /// Hands this modifier's attribute set to the entity it modifies, REPLACING whatever that entity declares.
        /// <para>
        /// Fields merge and attributes replace, because the two are different kinds of thing. A field is named and
        /// independent, so layers can each contribute one. An attribute list is ordered and position-bearing — a
        /// transform chain means what it means only as a whole — so the layer that re-declares it states all of it.
        /// This is also the only way to re-declare the attributes of a pack imported from another project, whose
        /// author cannot know which connections a later extension will add.
        /// </para>
        /// <para>
        /// A modifier declaring nothing replaceable leaves the target alone, which is what a modifier that only
        /// merges fields has to mean; `[ClearAttributes]` is how an empty replacing set is written. Branch attributes
        /// take no part: they spell a state's transitions, its body rather than its tuning.
        /// </para>
        /// </summary>
        protected void Adopt_Attributes_Onto(Entity target)
        {
            if( node == null || ReferenceEquals(target, this) ) return;

            var replacing = node.AttributeLists
                                .SelectMany(l => l.Attributes)
                                .Select(a => (syntax: a, cls: model.GetSymbolInfo(a).Symbol?.ContainingType as INamedTypeSymbol))
                                .Where(x => ProjectImpl.IsReplaceableAttribute(x.cls))
                                .ToList();

            if( replacing.Count == 0 ) return; // declares none — the target keeps its own

            var clearing = replacing.Count(x => ProjectImpl.IsClearAttributes(x.cls));
            if( 0 < clearing && clearing < replacing.Count )
                AdHocAgent.exit($"The modifier '{symbol}' (line {line_in_src_code}) carries `[ClearAttributes]` together with other attributes. "                        +
                                $"The marker replaces {target.symbol}'s attribute set with an EMPTY one, so the attributes written beside it could never apply. "        +
                                "Drop the marker to replace the set with those attributes, or drop them to clear it.", 2);

            // The topmost layer wins: of two modifiers of one target, the one whose project imports the other's.
            if( target.attributes_from is{ } held && !ReferenceEquals(held.by, this) )
            {
                var mine = in_project;
                var his  = held.by.in_project;

                if( his.all_imports.Contains(mine) )
                {
                    attributes_donated = true; // a higher layer holds the target: ours is overruled, not re-homed onto us
                    return;
                }


                if( !mine.all_imports.Contains(his) )
                    AdHocAgent.exit($"Both '{held.by.symbol}' (project {his.symbol}) and '{symbol}' (project {mine.symbol}) replace the attributes of {target.symbol}, " +
                                    "and neither project imports the other, so which one wins would be decided by composition order. "                                  +
                                    "Keep one of them, or move the replacement into a project that imports both.", 2);
            }

            target.attributes_from = (model, node, symbol!, this, 0 < clearing);
            attributes_donated     = true;

            AdHocAgent.LOG.Information("The attributes of {target} are replaced by the modifier {modifier} (line {line}): [{was}] -> [{now}]",
                                       target.symbol, symbol, line_in_src_code,
                                       string.Join(", ", target.node?.AttributeLists.SelectMany(l => l.Attributes).Select(a => a.ToString()) ?? []),
                                       0 < clearing ?
                                           "" :
                                           string.Join(", ", replacing.Select(x => x.syntax.ToString())));
        }

        /// <summary>
        /// Collects the modifications defined in this entity and applies them to a target entity.
        /// </summary>
        /// <param name="target">The entity to be modified.</param>
        /// <param name="once">A set used to track visited entities.</param>
        protected virtual void Init_Collect_Modification(Entity target, HashSet<object> once)
        {
            //   UP
            //   ↓
            //  down
            // Process modifications from XML documentation comments.
            foreach( var comment in node!.GetLeadingTrivia()
                                         .Select(t => t.GetStructure())
                                         .OfType<DocumentationCommentTriviaSyntax>() )
                foreach( var see in comment.DescendantNodes()
                                           .OfType<XmlCrefAttributeSyntax>() )
                    target.modify(model.GetSymbolInfo(see.Cref).Symbol!, !see.Parent!.Parent!.DescendantNodes().FirstOrDefault(t => see.Span.End < t.Span.Start)!.ToString().Trim().StartsWith("-"), 0, once);

            if( node!.BaseList == null || node is EnumDeclarationSyntax ) return; // enum BaseList holds only the underlying primitive type (e.g. `: byte`), never a modification target

            //left -> right
            // Process modifications from implemented interfaces BaseList.
            foreach( var item in node!.BaseList!.Types )
                modify_by_implement(item, true, (sym, sn, add, depth) => target.modify(sym, add, depth, once));
        }

        /// <summary>
        /// Filters a set of packs by applying regex patterns ([KeepName], [SkipDoc], etc.) defined on the provided generic symbol.
        /// </summary>
        /// <param name="packs">The set of packs to filter. Modified in-place.</param>
        /// <param name="filterSymbol">The interface symbol that declares the regex filter attributes.</param>
        public static void FilterPacks(HashSet<ProjectImpl.HostImpl.PackImpl> packs, ISymbol filterSymbol)
        {
            if( filterSymbol == null ) return;

            List<string> GetPatterns(INamedTypeSymbol metaAttr) => filterSymbol.GetAttributes()
                                                                               .Where(a => equals(a.AttributeClass, metaAttr))
                                                                               .Select(a => a.ConstructorArguments[0].Value as string)
                                                                               .Where(s => !string.IsNullOrEmpty(s))
                                                                               .ToList()!;

            Regex? BuildCombinedRegex(List<string> patterns)
            {
                if( patterns.Count == 0 ) return null;
                var combined = string.Join("|", patterns.Select(p => $"(?:{p})"));
                try { return new Regex(combined, RegexOptions.Compiled); }
                catch( ArgumentException ex )
                {
                    AdHocAgent.LOG.Error("Invalid regular expression in filter template '{name}': {msg}", filterSymbol.Name, ex.Message);
                    AdHocAgent.exit("Please fix the regular expression and rerun.");
                    return null;
                }
            }

            var keepNameRegex = BuildCombinedRegex(GetPatterns(ProjectImpl.Meta_KeepName));
            var keepDocRegex  = BuildCombinedRegex(GetPatterns(ProjectImpl.Meta_KeepDoc));
            var skipNameRegex = BuildCombinedRegex(GetPatterns(ProjectImpl.Meta_SkipName));
            var skipDocRegex  = BuildCombinedRegex(GetPatterns(ProjectImpl.Meta_SkipDoc));

            if( keepNameRegex != null ) packs.RemoveWhere(p => !keepNameRegex.IsMatch(p.full_path));
            if( keepDocRegex  != null ) packs.RemoveWhere(p => !keepDocRegex.IsMatch(UnifiedDoc(p)));
            if( skipNameRegex != null ) packs.RemoveWhere(p => skipNameRegex.IsMatch(p.full_path));
            if( skipDocRegex  != null ) packs.RemoveWhere(p => skipDocRegex.IsMatch(UnifiedDoc(p)));

            static string UnifiedDoc(ProjectImpl.HostImpl.PackImpl p)
            {
                var doc = p._doc ?? "";
                if( p.symbol == null ) return doc;
                foreach( var prj in ProjectImpl.projects )
                    if( prj.pack_user_tags.TryGetValue(p.symbol, out var extra) && !string.IsNullOrEmpty(extra) )
                        doc = doc + " " + extra;
                return doc;
            }
        }

        /// <summary>
        /// Resolves a Pack Set expression written in this entity's source - a pack, a Pack Set, a Project/Host/Pack scope with or
        /// without `@`, `_&lt;…&gt;`, `X&lt;…&gt;`, a filter template, or a tuple of those - into <paramref name="dst"/>, in order:
        /// what a branch's PACKS resolves to. Called with the same <paramref name="dst"/> for several expressions, an `X&lt;…&gt;` in a
        /// later one removes what an earlier one added.
        /// </summary>
        internal void resolve_pack_set(SyntaxNode expr, List<ProjectImpl.HostImpl.PackImpl> dst) =>
            modify_by_implement(expr, true, (sym, _, add, depth) => ProjectImpl.ConnectionImpl.NamedPackSet.collect_packs_in_scope(sym, add, depth, (p, a) =>
                                                                                                                               {
                                                                                                                                   if( !a ) dst.Remove(p);
                                                                                                                                   else if( !dst.Contains(p) ) dst.Add(p);
                                                                                                                               }));

        /// <summary>
        /// Recursively processes an entity's BaseList to apply modifications.
        /// </summary>
        /// <param name="sn">The syntax node of the base type.</param>
        /// <param name="add">True to add modifications, false to remove them (for `X<...>` syntax).</param>
        /// <param name="modify">The action to perform the modification.</param>
        protected void modify_by_implement(SyntaxNode sn, bool add, Action<ISymbol, SyntaxNode, bool, uint> modify)
        {
            var type = sn is SimpleBaseTypeSyntax sbt ?
                           sbt.Type :
                           sn;

            var typeSymbol = model.GetTypeInfo(type).Type;

            switch( type )
            {
                case TupleTypeSyntax tuple:
                {
                    foreach( var elem in tuple.Elements )
                        modify_by_implement(elem.Type, add, modify);
                    return;
                }
                case GenericNameSyntax gns:
                    switch( gns.Identifier.ValueText )
                    {
                        case "_":
                        case "FieldsInjectInto":
                        case "HeaderFor":
                            modify_by_implement(gns.TypeArgumentList.Arguments[0], add, modify);
                            if( gns.TypeArgumentList.Arguments.Count > 1 && gns.Identifier.ValueText == "_" )
                                modify_by_implement(gns.TypeArgumentList.Arguments[1], add, modify);
                            return;

                        case "Resumable":
                            // Resumable<HOST, PACKS>: the first argument names a host, not packs. Only PACKS goes through the pack-set walker.
                            modify_by_implement(gns.TypeArgumentList.Arguments[1], add, modify);
                            return;

                        case "X":
                            modify_by_implement(gns.TypeArgumentList.Arguments[0], false, modify);
                            return;

                        case "Modify":
                        case "Connects":
                        case "VirtuallyConnects":
                            return;

                        case "IfSendingFrom":
                            // A concrete `IfSendingFrom<Host, Connection>` endpoint. It is NOT a `<SCOPE>` filter
                            // template — break out so the constructed symbol is dispatched to modify() below, where
                            // EndpointSetImpl.modify resolves it into a (Connection, Host) endpoint.
                            break;

                        default:
                            // Generic Composeable Filters <SCOPE> architecture
                            if( typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType )
                            {
                                var innerPacks = new HashSet<ProjectImpl.HostImpl.PackImpl>();

                                // Get the template's ACTUAL syntax node via Roslyn's DeclaringSyntaxReferences
                                // (NOT from the entities dict — filter templates are intentionally not registered there)
                                var templateSyntax = namedType.OriginalDefinition
                                                              .DeclaringSyntaxReferences
                                                              .FirstOrDefault()?.GetSyntax() as InterfaceDeclarationSyntax;

                                if( templateSyntax?.BaseList != null )
                                {
                                    // Template HAS a BaseList (e.g., interface ActiveOnly<SCOPE> : _<SCOPE>, X<Legacy> {})
                                    //
                                    // Strategy: Use the TEMPLATE's own semantic model to walk its BaseList.
                                    // When we encounter the type parameter symbol (SCOPE), we redirect resolution
                                    // to the call-site argument syntax using the CALLER's model.
                                    var templateModel = model.Compilation.GetSemanticModel(templateSyntax.SyntaxTree);
                                    var paramSymbol   = namedType.OriginalDefinition.TypeParameters[0];

                                    var savedModel = this.model;
                                    this.model = templateModel; // temporarily switch to template's model

                                    foreach( var baseItem in templateSyntax.BaseList.Types )
                                        modify_by_implement(baseItem, true, (sym, innerSn, innerAdd, depth) =>
                                                                            {
                                                                                // Intercept: if the resolved symbol IS the type parameter,
                                                                                // redirect to the actual call-site argument
                                                                                if( SymbolEqualityComparer.Default.Equals(sym, paramSymbol) ||
                                                                                    sym is ITypeParameterSymbol )
                                                                                {
                                                                                    var m = this.model;
                                                                                    this.model = savedModel; // restore caller's model
                                                                                    modify_by_implement(gns.TypeArgumentList.Arguments[0], innerAdd,
                                                                                                        (s2, sn2, a2, d2) =>
                                                                                                        {
                                                                                                            ProjectImpl.ConnectionImpl.NamedPackSet
                                                                                                                       .collect_packs_in_scope(s2, a2, d2, (p, a) =>
                                                                                                                                                           {
                                                                                                                                                               if( a ) innerPacks.Add(p);
                                                                                                                                                               else innerPacks.Remove(p);
                                                                                                                                                           });
                                                                                                        });
                                                                                    this.model = m; // switch back to template's model
                                                                                }
                                                                                else
                                                                                {
                                                                                    // Non-parameter symbol — process normally
                                                                                    ProjectImpl.ConnectionImpl.NamedPackSet
                                                                                               .collect_packs_in_scope(sym, innerAdd, depth, (p, a) =>
                                                                                                                                             {
                                                                                                                                                 if( a ) innerPacks.Add(p);
                                                                                                                                                 else innerPacks.Remove(p);
                                                                                                                                             });
                                                                                }
                                                                            });

                                    this.model = savedModel; // restore after BaseList processing
                                }
                                else
                                {
                                    // Simple template without BaseList — just evaluate scope argument directly
                                    modify_by_implement(gns.TypeArgumentList.Arguments[0], true, (sym, innerSn, innerAdd, depth) =>
                                                                                                 {
                                                                                                     ProjectImpl.ConnectionImpl.NamedPackSet
                                                                                                                .collect_packs_in_scope(sym, innerAdd, depth, (p, a) =>
                                                                                                                                                              {
                                                                                                                                                                  if( a ) innerPacks.Add(p);
                                                                                                                                                                  else innerPacks.Remove(p);
                                                                                                                                                              });
                                                                                                 });
                                }

                                // Apply regex filters defined on the template
                                FilterPacks(innerPacks, namedType.OriginalDefinition);

                                // Push surviving packs to the parent modify action with the OUTER add flag
                                // (preserves X<> semantics — if this template was inside X<>, add is false)
                                foreach( var p in innerPacks )
                                    modify(p.symbol!, sn, add, 0);

                                return;
                            }

                            break;
                    }

                    break;
            }

            modify(typeSymbol!, sn, add, sn.GetFirstToken().Text[0] == '@' ?
                                             uint.MaxValue :
                                             0U);
        }


        /// <summary>
        /// Modifies the current entity by adding or removing other entities, such as packs or fields.
        /// This method is the programmatic endpoint for applying declarative modifications defined in the protocol source code, including:
        /// <list type="bullet">
        ///   <item>Indirect external modifications via <c>Modify<T></c> interfaces.</item>
        ///   <item>Direct modifications via inheritance, like <c>_<...></c> (add) and <c>X<...></c> (remove).</item>
        ///   <item>Direct modifications via doc comments, like <c><see .../>+</c> (add) and <c><see .../>-</c> (remove).</item>
        /// </list>
        /// </summary>
        /// <param name="by_what">The symbol representing the source entity (e.g., a pack, field, project, or host) that provides the items for modification.</param>
        /// <param name="add">If <c>true</c>, adds entities from the source (e.g., via <c>_<...></c>); if <c>false</c>, removes them (e.g., via <c>X<...></c>).</param>
        /// <param name="depth">
        /// Controls the depth of entity collection from the <paramref name="by_what"/> source. A value of `1` is triggered by prefixing a type with `@`
        /// in an inheritance list (e.g., `interface MyState : @PackGroup`), while `0` is the default.
        /// <list type="bullet">
        ///   <item>
        ///     <term>0 (Shallow)</term>
        ///     <description>
        ///       When <paramref name="by_what"/> is a <see cref="ProjectImpl.HostImpl.PackImpl"/>, only the pack itself is processed.
        ///       <br/>
        ///       When <paramref name="by_what"/> is a <see cref="ProjectImpl"/> or <see cref="ProjectImpl.HostImpl"/>, only packs declared directly within it are processed.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>1 (Deep, triggered by `@`)</term>
        ///     <description>
        ///       When <paramref name="by_what"/> is a <see cref="ProjectImpl.HostImpl.PackImpl"/>, the pack itself and all packs declared one level deep inside it are processed.
        ///       <br/>
        ///       When <paramref name="by_what"/> is a <see cref="ProjectImpl"/> or <see cref="ProjectImpl.HostImpl"/>, all transmittable packs are collected recursively from its entire hierarchy.
        ///     </description>
        ///   </item>
        /// </list>
        /// </param>
        /// <param name="once">A set to track visited entities, preventing infinite recursion during the initialization process due to cyclic dependencies.</param>
        /// <remarks>
        /// This abstract method is implemented by derived classes like <see cref="ProjectImpl"/>, <see cref="ProjectImpl.HostImpl"/>, and <see cref="ProjectImpl.HostImpl.PackImpl"/>
        /// to handle specific modification logic. For instance, it is used for adding/removing packs in a connection or injecting fields for <c>FieldsInjectInto</c> and <c>HeaderFor</c> packs.
        /// </remarks>
        public abstract void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once);

        /// <summary>
        ///     Purpose: Provides a "Permanent ID" that survives code refactoring.
        ///     Stability: Stable. It is saved as a comment in your source code (e.g., /*ÿ*/). Even if you move a packet from one file to another or change its name, the uid stays the same.
        ///     Scope: Project-wide or Global.
        ///     Usage: Used by the Visualizer and protocol versioning tools to track the "same" entity across different versions of the protocol, even if the runtime idx changes.
        /// </summary>
        public ulong uid = ulong.MaxValue;

        /// <summary>Where its `/*…*/` marks are in the source: removed when the entity is numbered in a table.</summary>
        public readonly List<TextSpan> uid_marks = [];

        public int? _fake_uid_pos;

        public int uid_pos //uid position in the source code
        {
            get
            {
                if( _fake_uid_pos != null ) return _fake_uid_pos.Value;
                if( symbol        == null ) return -1;
                var span = symbol.Locations[0].SourceSpan;
                return span.Start + span.Length;
            }
        }
    }

    /// <summary>
    /// Provides extension methods for various types used in the protocol parser.
    /// </summary>
    public static class Extensions{
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
        {
            if( collection is null || enumerable is null ) return;

            foreach( var cur in enumerable ) collection.Add(cur);
        }

        /// <summary>
        /// Checks if the symbol represents a `Connects<...>` (or `VirtuallyConnects<...>`) meta-interface.
        /// Walks AllInterfaces because VirtuallyConnects inherits from Connects, so the marker
        /// may sit at any position depending on declaration order.
        /// </summary>
        /// <param name="sym">The symbol to check.</param>
        /// <returns><c>true</c> if the symbol is a `Connects` (physical or virtual) interface; otherwise, <c>false</c>.</returns>
        public static bool isConnects(this ISymbol? sym) => sym is INamedTypeSymbol s &&
                                                            s.AllInterfaces.Any(i =>
                                                                                    SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, ProjectImpl.Meta_Connects) ||
                                                                                    SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, ProjectImpl.Meta_VirtuallyConnects));

        /// <summary>
        /// Checks if the symbol represents any meta-interface from the org.unirail.Meta namespace.
        /// </summary>
        /// <param name="sym">The symbol to check.</param>
        /// <returns>True if the symbol is a meta-interface, false otherwise.</returns>
        public static bool isMeta(this ISymbol? sym) => sym != null && sym.ToString()!.StartsWith("org.unirail.Meta");

        /// <summary>
        /// Checks if the symbol represents a Modify meta-interface.
        /// </summary>
        /// <param name="sym">The symbol to check.</param>
        /// <returns>True if the symbol is a Modify meta-interface, false otherwise.</returns>
        public static bool isModify(this ISymbol? sym) => sym != null && sym.ToString()!.StartsWith("org.unirail.Meta.Modify<");

        /// <summary>
        /// Checks if the symbol represents a Set meta-interface.
        /// </summary>
        /// <param name="sym">The symbol to check.</param>
        /// <returns>True if the symbol is a Set meta-interface, false otherwise.</returns>
        public static bool isSet(this ISymbol? sym) => sym != null && sym.ToString()!.StartsWith("org.unirail.Meta.Set<");

        /// <summary>
        /// Checks if the symbol represents a Map meta-interface.
        /// </summary>
        /// <param name="sym">The symbol to check.</param>
        /// <returns>True if the symbol is a Map meta-interface, false otherwise.</returns>
        public static bool isMap(this ISymbol? sym) => sym != null && sym.ToString()!.StartsWith("org.unirail.Meta.Map<");

        /// <summary>
        /// Converts a base256 encoded string to a ulong value.
        /// </summary>
        /// <param name="str">The base256 encoded string.</param>
        /// <returns>The ulong value represented by the string.</returns>
        public static ulong to_base256_value(this string str)
        {
            var ret = 0UL;
            for( var i = 0; i < str.Length; i++ )
                ret |= (ulong)(str[i] - base256) << i * 8;

            return ret;
        }

        /// <summary>
        /// Base value for base256 encoding (0xFF).
        /// </summary>
        const int base256 = 0xFF;

        /// <summary>
        /// Converts a ulong value to a base256 encoded string.
        /// </summary>
        /// <param name="src">The ulong value to encode.</param>
        /// <returns>The base256 encoded string.</returns>
        public static string to_base256_chars(this ulong src)
        {
            var chars = new char[8]; // a ulong: up to 8 bytes
            return new(chars, 0, src.to_base256_chars(chars));
        }

        /// <summary>
        /// Converts a ulong value to base256 encoded characters and writes them to a character array.
        /// </summary>
        /// <param name="src">The ulong value to encode.</param>
        /// <param name="dst">The character array to write the encoded characters to.</param>
        /// <returns>The number of characters written to the destination array.</returns>
        public static int to_base256_chars(this ulong src, char[] dst)
        {
            var i = 0;
            do dst[i++] = (char)((src & 0xFF) + base256);
            while( 0 < (src >>= 8) );

            return i;
        }

        /// <summary>
        /// Adds elements from a source list to a destination list, ensuring no duplicates are added.
        /// </summary>
        /// <typeparam name="T">The type of elements in the lists.</typeparam>
        /// <param name="dst">The destination list to add elements to.</param>
        /// <param name="src">The source list to add elements from.</param>
        public static void AddNew<T>(this List<T> dst, List<T> src) => src.ForEach(t =>
                                                                                   {
                                                                                       if( !dst.Contains(t) ) dst.Add(t);
                                                                                   });

        public static void AddIfNew<T>(this List<T> list, T item)
        {
            if( !list.Contains(item) )
                list.Add(item);
        }
    }
}
