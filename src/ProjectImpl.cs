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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Project = org.unirail.Agent.AdHocProtocol.Agent_.Project;

// Reference to Microsoft.CodeAnalysis documentation: https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel?view=roslyn-dotnet-3.11.0
namespace org.unirail
{
    /// <summary>
    /// Represents the implementation of a project, serving as the root entity for protocol description.
    /// </summary>
    public class ProjectImpl : Entity, Project
    {
        public ulong _uid => uid;

        /// <summary>
        /// UIDs of imported projects.
        /// </summary>
        public ulong[]? imported_projects_uid;

        /// <summary>
        /// Provides access to the array of imported project UIDs for serialization purposes.
        /// </summary>
        /// <returns>The array of imported project UIDs.</returns>
        public object? _imported_projects_uid() => imported_projects_uid;

        /// <summary>
        /// Gets the length of the imported project UIDs array.
        /// </summary>
        public int _imported_projects_uid_len => imported_projects_uid?.Length ?? 0; // Handle null case

        /// <summary>
        /// Retrieves a specific imported project UID at the given index.
        /// </summary>
        /// <param name="ctx">The transmitter context (not used here).</param>
        /// <param name="__slot">The transmitter slot (not used here).</param>
        /// <param name="item">The index of the imported project UID to retrieve.</param>
        /// <returns>The imported project UID at the specified index.</returns>
        public ulong _imported_projects_uid(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item) => imported_projects_uid![item]; // Null check might be needed


        /// <summary>
        /// Iterates through packs within the project scope, applying an action to transmittable packs.
        /// If depth is 0, includes only packs defined directly under the project (not inside a host).
        /// If depth > 0, recursively includes all packs within the project, including those inside hosts and other nested structures.
        /// </summary>
        /// <param name="depth">The depth of scope to traverse (0 for immediate project scope, >0 for deeper scopes).</param>
        /// <param name="dst">The action to apply to each transmittable pack.</param>
        public void for_packs_in_scope(uint depth, Action<HostImpl.PackImpl> dst)
        {
            foreach (var entity in entities.Values.Where(e => e.in_project == this && (0 < depth || e.in_host == null)))
                if (entity is HostImpl.PackImpl pack && pack.is_transmittable)
                    dst(pack);
        }

        /// <summary>
        /// Dictionary to group related packets under descriptive names.
        /// Key: Symbol representing the named pack set interface.
        /// Value: NamedPackSet instance containing the grouped packs.
        /// </summary>
        public static readonly Dictionary<ISymbol, ChannelImpl.NamedPackSet> named_packs = new(SymbolEqualityComparer.Default);

        // List of packs that inject fields into other packs.
        public readonly List<HostImpl.PackImpl> injector_packs = [];

        // List of header modifiers.
        public readonly List<HostImpl.PackImpl> header_mofifiers = [];


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
        public static INamedTypeSymbol Meta_HeaderFor;
        public static INamedTypeSymbol Meta_FieldsInjectInto;

        public static INamedTypeSymbol Meta_ChannelFor;
        public static INamedTypeSymbol Meta_Modify_Target;
        public static INamedTypeSymbol Meta_Modify_Channel;
        public static INamedTypeSymbol Meta_Host;
        public static INamedTypeSymbol Meta_MultiContextHost;
        public static INamedTypeSymbol Meta_Set;
        public static INamedTypeSymbol Meta_Map;

        /// <summary>
        /// Parser class responsible for traversing the C# syntax tree and extracting protocol description information.
        /// </summary>
        class Protocol_Description_Parser : CSharpSyntaxWalker
        {
            /// <summary>
            /// Finds the ProjectImpl instance associated with a given type symbol by traversing up the containing type hierarchy.
            /// </summary>
            /// <param name="src">The type symbol to find the project for.</param>
            /// <returns>The ProjectImpl instance containing the type symbol.</returns>
            /// <exception cref="Exception">If the project source code cannot be found for the given symbol.</exception>
            ProjectImpl project_of(ITypeSymbol src)
            {
                var parent = src.ContainingType;

                while (true)
                {
                    var prj = projects.FirstOrDefault(prj => equals(prj.symbol, parent));
                    if (prj != null) return prj;
                    parent = parent.ContainingType;
                    if (parent == null) AdHocAgent.exit($"Cannot find project source code of {src}", 44);
                }
            }

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
                Meta_HeaderFor = compilation.GetTypeByMetadataName("org.unirail.Meta.HeaderFor`1");
                Meta_FieldsInjectInto = compilation.GetTypeByMetadataName("org.unirail.Meta.FieldsInjectInto`1");

                Meta_ChannelFor = compilation.GetTypeByMetadataName("org.unirail.Meta.ChannelFor`2");

                Meta_Modify_Channel = compilation.GetTypeByMetadataName("org.unirail.Meta.Modify`3");
                Meta_Modify_Target = compilation.GetTypeByMetadataName("org.unirail.Meta.Modify`1");

                Meta_MultiContextHost = compilation.GetTypeByMetadataName("org.unirail.Meta.MultiContextHost");
                Meta_Host = compilation.GetTypeByMetadataName("org.unirail.Meta.Host");
                Meta_Set = compilation.GetTypeByMetadataName("org.unirail.Meta.Set`1");
                Meta_Map = compilation.GetTypeByMetadataName("org.unirail.Meta.Map`2");
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
                var model = compilation.GetSemanticModel(node.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(node)!;

                if (symbol.OriginalDefinition.ContainingType == null) //The top-level C# interface serves as the project’s declaration.
                {
                    current = project = new ProjectImpl(projects.Count == 0 //root project
                                                            ?
                                                            null :
                                                            projects[0], compilation, node, namespace_);

                    projects.Add(project);

                    if (!string.IsNullOrEmpty(namespace_doc))
                    {
                        project._doc = namespace_doc + project._doc;
                        namespace_doc = "";
                    }
                }
                else
                    for (var sym = symbol; ;)
                    {
                        var interfaces = sym.Interfaces;
                        if (0 < interfaces.Length && interfaces[0].isMeta())
                            switch (interfaces[0].Name)
                            {
                                case "Modify":
                                    sym = (INamedTypeSymbol)interfaces[0].TypeArguments[0];
                                    continue;
                                case "ChannelFor":
                                    current = new ChannelImpl(project, compilation, node); //Channel
                                    break;
                                case "L" or "R" or "LR":
                                    current = new ChannelImpl.StageImpl(project, compilation, node); //host stage
                                    break;
                                case "_":
                                    named_packs.Add(symbol, new ChannelImpl.NamedPackSet(project, compilation, node)); //set of packs
                                    current = null;
                                    break;
                                default:
                                    current = null;
                                    break;
                            }
                        else AdHocAgent.exit($"Unknown type interface entity {symbol}");

                        break;
                    }

                base.VisitInterfaceDeclaration(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(node)!;
                var interfaces = symbol.Interfaces;

                if (entities[symbol.ContainingType] is ProjectImpl && //directly in the project scope
                    interfaces.Any(i =>
                                       equals(i.ConstructedFrom, Meta_Host) ||
                                       equals(i.ConstructedFrom, Meta_MultiContextHost) ||
                                       equals(i.ConstructedFrom, Meta_Modify_Target) && i.TypeArguments[0].Interfaces.Any(ii => equals(ii.ConstructedFrom, Meta_Host) || equals(ii.ConstructedFrom, Meta_MultiContextHost))
                                  )
                  )
                    current = new HostImpl(project, compilation, node); //host
                else if (0 < interfaces.Length)
                    AdHocAgent.exit($"Unknown struct {symbol} entity type. If it is a Host, it should extend 'org.unirail.Meta.Host'. If it is a Host Modifier, it should extend 'org.unirail.Meta.Modify'.");
                else
                    current = new HostImpl.PackImpl(project, compilation, node); //explicitly assigned a constant collection

                base.VisitStructDeclaration(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax clazz)
            {
                current = new HostImpl.PackImpl(project, compilation, clazz);
                base.VisitClassDeclaration(clazz);
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax ENUM)
            {
                current = new HostImpl.PackImpl(project, compilation, ENUM);

                base.VisitEnumDeclaration(ENUM);
            }


            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);

                foreach (var variable in node.Declaration.Variables)
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
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                    if (current != null && current.line_in_src_code == trivia.GetLocation().GetMappedLineSpan().StartLinePosition.Line + 1)
                        current._inline_doc += trivia.ToString().Trim('\r', '\n', '\t', ' ', '/');

                base.VisitTrivia(trivia);
            }


            public override void VisitXmlCrefAttribute(XmlCrefAttributeSyntax node)
            {
                var comment_line = (XmlEmptyElementSyntax)node.Parent!;

                var model = compilation.GetSemanticModel(node.Cref.SyntaxTree);
                var cref = model.GetSymbolInfo(node.Cref).Symbol;

                if (cref == null)
                    AdHocAgent.exit($"In meta information `{comment_line}` the reference to `{node.Cref.ToString()}` on `{current}` is unreachable. ");


                current._doc = current._doc?.Replace(comment_line.Parent!.GetText().ToString(), "");


                #region reading of the project saved packs id info
                if (node.SpanStart < project.packs_id_info_end)
                    switch (cref!.Kind)
                    {
                        case SymbolKind.NamedType:
                            if (project.packs_id_info_start == -1)
                            {
                                project.packs_id_info_start = comment_line.Parent.Span.Start - 3; //-3 cut `/**`
                                project.packs_id_info_end = comment_line.Parent.Span.End;
                            }

                            var id = int.Parse(((XmlTextAttributeSyntax)comment_line.Attributes[1]).TextTokens.ToString());

                            project.pack_id_info.Add((INamedTypeSymbol)cref, id);

                            break;
                        default:
                            AdHocAgent.exit($"Packs id info contains reference to unknown entity {node}");
                            break;
                    }
                #endregion
                else
                {
                    if (current is HostImpl host) //language &  generate/skip implementation at current lang config
                    {
                        var lang = node.Cref.ToString() switch
                        {
                            "InCS" => (ushort)Project.Host.Langs.InCS,
                            "InGO" => (ushort)Project.Host.Langs.InGO,
                            "InRS" => (ushort)Project.Host.Langs.InRS,
                            "InTS" => (ushort)Project.Host.Langs.InTS,
                            "InCPP" => (ushort)Project.Host.Langs.InCPP,
                            "InJAVA" => (ushort)Project.Host.Langs.InJAVA,
                            _ => (ushort)0
                        };
                        if (0 < lang)
                        {
                            #region read host language configuration and create a new scope
                            host._langs |= (Project.Host.Langs)lang; //register language config
                            var txt = comment_line.Parent!.DescendantNodes().FirstOrDefault(t => node.Span.End < t.Span.Start)?.ToString().Trim() ?? "";

                            // This state variable holds the configuration for the current scope being parsed.
                            host._default_impl_hash_equal = (0 < txt.Length ? //the first char after last `>` impl pack
                                                                 txt[0] :
                                                                 ' ') switch
                            {
                                '+' => host._default_impl_hash_equal | (uint)(lang << 16),    //Implementing pack in this language.
                                '-' => (uint)(host._default_impl_hash_equal & ~(lang << 16)), //Abstracting pack in this language.
                                _ => host._default_impl_hash_equal
                            };

                            host._default_impl_hash_equal = (1 < txt.Length ? //the second char after last `>` - hash and equals methods
                                                                 txt[1] :
                                                                 ' ') switch
                            {
                                '+' => host._default_impl_hash_equal | lang,          //Implementing the hash and equals methods for the pack.
                                '-' => (uint)(host._default_impl_hash_equal & ~lang), //Abstracting the hash and equals methods for the pack.
                                _ => host._default_impl_hash_equal
                            };

                            // Create and add the new language scope to the host for later processing.
                            host.LangScopes.Add(new HostImpl.LangScope { Config = host._default_impl_hash_equal });
                            #endregion
                            goto END;
                        }

                        #region Add the cref target to the current language scope
                        if (cref == null) AdHocAgent.exit($"`Reference to unknown entity {node.Cref} on {host} host configuration detected.");

                        if (host.LangScopes.Any())
                            // Add the referenced symbol to the list of targets for the most recently defined scope.
                            host.LangScopes.Last().Targets.Add(cref);
                        else
                            AdHocAgent.LOG.Warning("Configuration target '{cref}' found on host '{host}' without a preceding language specifier like '<see cref=\"InCS\"/>'. This configuration will be ignored.", cref, host);
                        #endregion
                        goto END;
                    }


                    if (cref == null) AdHocAgent.exit($"`Reference to unknown entity {node.Parent} detected. Correct or delete it");
                }

            END:
                base.VisitXmlCrefAttribute(node);
            }
        }

        /// <summary>
        /// Stores pack ID information read from the source code file.
        /// Key: INamedTypeSymbol representing the pack.
        /// Value: Integer ID assigned to the pack.
        /// </summary>
        public readonly Dictionary<INamedTypeSymbol, int> pack_id_info = new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Reads pack ID information from the source file, updates pack IDs, and writes changes back to the file.
        /// </summary>
        /// <param name="once">HashSet to track processed projects and prevent infinite recursion in imported project processing.</param>
        /// <returns>ISet of transmittable packs without related packs.</returns>
        public ISet<HostImpl.PackImpl> read_packs_id_info_and_write_update(HashSet<object> once)
        {
            List<ProjectImpl> imported_projects = [];
            #region process imported projects and collect them
            foreach (var I in symbol!.Interfaces)
                if (entities.TryGetValue(I, out var value) && value is ProjectImpl prj)
                    if (once.Add(prj))
                    {
                        imported_projects.Add(prj);
                        prj.read_packs_id_info_and_write_update(once);
                    }
            #endregion
            var branches = channels
                           .Where(ch => ch.hostL!.included && ch.hostR!.included)
                           .SelectMany(ch => ch.stages)
                           .SelectMany(stage => stage.branchesL.Concat(stage.branchesR)).ToArray();

            // Packs in stages are transmittable and must have a valid `id`.
            var transmittable_packs_without_related = branches
                                                      .SelectMany(branch => branch.packs)
                                                      .ToHashSet(); //collect valid transmittable packs

            // Check for the correct usage of empty packs as type.
            #region Validate empty packs
            foreach (var pack in all_packs.Where(pack => pack.fields.Count == 0 && !(pack.is_Header || pack.is_FieldsInjectInto))) // Empty packs: packs without fields.
            {
                var used = false;

                foreach (var fld in raw_fields.Values.Where(fld => fld.is_Map && fld.get_exT_pack == pack))
                    AdHocAgent.exit($"The field `{fld.symbol}` at the line: {fld.line_in_src_code} is a Map with a key of empty pack {pack.symbol}, which is unsupported and unnecessary.");

                foreach (var fld in raw_fields.Values.Where(fld => fld.get_exT_pack == pack)
                                              .Concat(raw_fields.Values.Where(fld => fld.is_Map && fld.V.get_exT_pack == pack).Select(fld => fld.V!)) //field value has empty packs as type
                       )                                                                                                                              //change field type to boolean
                {
                    used = true;
                    fld.switch_to_boolean();
                }

                if (used) AdHocAgent.LOG.Warning("Pack {Pack} is empty and, as a field datatype, is therefore redundant. References to it will be replaced with a boolean.", pack.symbol);
                if (transmittable_packs_without_related.Contains(pack)) continue; // If pack is transmittable


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

            #region Apply collected pack_id_info to packs
            foreach (var pack in included_packs) //Extract saved transmittable pack ID information
                if (!pack._name.Equals(pack.symbol!.Name))
                {
                    AdHocAgent.LOG.Error("The name of the pack {entity} (line:{line}) has been changed to {new_name}. However, the pack cannot be assigned an ID until its name is manually corrected", pack.symbol, pack.line_in_src_code, pack._name);
                    AdHocAgent.exit("", 66);
                }
                else if (pack_id_info.TryGetValue(pack.symbol!, out var id) ||              // If the root project does not have the pack ID info...
                         imported_projects_pack_id_info.TryGetValue(pack.symbol!, out id)) //imported projects may have it
                    pack._id = (ushort)id;
            #endregion

            #region Detect obsolete pack ID records to trigger a rewrite
            var included_pack_symbols = included_packs.Select(p => p.symbol).ToHashSet(SymbolEqualityComparer.Default);
            if (pack_id_info.Keys.Any(symbol => !included_pack_symbols.Contains(symbol))) update_packs_id_info = true;
            #endregion

            if (new FileInfo(AdHocAgent.provided_path).IsReadOnly) // Check if the protocol description file is locked
            {
                AdHocAgent.LOG.Warning($"The protocol description file {node!.SyntaxTree.FilePath} is read-only. As a result, the pack ID update process was skipped.");
                return transmittable_packs_without_related; // Return the collected packs without updating the pack IDs
            }

            #region Detect pack's id duplication
            foreach (var pks in included_packs.Where(pk => pk._id < (int)Project.Host.Pack.Field.DataType.t_subpack).GroupBy(pack => pack._id).Where(g => 1 < g.Count()))
            {
                update_packs_id_info = true;
                var list = pks.Aggregate("", (current, pk) => current + pk.full_path + "\n");
                AdHocAgent.LOG.Warning("Packs \n{List} with the same id = {Id} detected. One assignment will be preserved, and the others will be renumbered.", list, pks.Key);

                //find a one to preserve it's id in root project first
                var pk = pks.FirstOrDefault(pk => pk.project == this) ?? pks.First();

                foreach (var _pk in pks.Where(_pk => _pk != pk))
                    _pk._id = (int)Project.Host.Pack.Field.DataType.t_subpack; // reset for renumbering
            }
            #endregion

            #region Renumbering pack IDs if necessary
            for (var id = 0; ; id++)                             //set new packs id
                if (included_packs.All(pack => pack._id != id)) //find not in use id
                {
                    var pack = transmittable_packs_without_related.FirstOrDefault(pack => pack._id == (int)Project.Host.Pack.Field.DataType.t_subpack);
                    if (pack == null) break;    //no more pack without id
                    update_packs_id_info = true; //mark need to update packs_id_info in protocol description file
                    pack._id = (ushort)id;
                }
            #endregion
            if (!update_packs_id_info && updated_uid.Count == 0) return transmittable_packs_without_related;

            var top = 0;
            var tmp = new char[10];


            //================================= Update the current packs' ID information in the protocol description file.
            top = 0;

            string long_full_path(HostImpl.PackImpl pack) => pack.project == this ?
                                                                 pack.full_path :
                                                                 pack.symbol!.ToString()!; //namespace + project_name + pack.full_path


            var text_max_width = transmittable_packs_without_related.Select(p => long_full_path(p).Length).Max() + 4;
            var src_code = node!.SyntaxTree.ToString();
            using StreamWriter dst = new(node!.SyntaxTree.FilePath);


            if (update_packs_id_info)
            {
                //head of the src file without pack id info
                dst.Write(src_code[..((packs_id_info_start == -1) ? //no saved packs id info in the source file
                                          packs_id_info_end :
                                          packs_id_info_start)]);
                dst.Write("/**\n");

                // Packs without saved info
                foreach (var pack in included_packs.Where(pack => !imported_projects_pack_id_info.TryGetValue(pack.symbol!, out var id) || pack._id != id).OrderBy(pack => long_full_path(pack)))
                {
                    dst.Write("\t\t<see cref = '");

                    var path = long_full_path(pack);
                    dst.Write(path);
                    dst.Write("'");

                    for (var i = text_max_width - path.Length; 0 < i; i--) dst.Write(" ");
                    dst.Write("id = '");
                    dst.Write(pack._id.ToString());
                    dst.Write("'/>\n");
                }

                dst.Write("\t*/\n\t");
                top = packs_id_info_end;
            }

            foreach (var (pos, uid) in updated_uid.OrderBy(b => b.Item1))
            {
                dst.Write(src_code[top..pos]);

                dst.Write("/*");
                var len = uid.to_base256_chars(tmp);
                dst.Write(tmp, 0, len);
                if (0xFFFF < uid && has_imported_projects) //this is a project with imported projects
                    foreach (var prj_uid in imported_projects_uid!)
                    {
                        dst.Write(' ');
                        len = (uid - prj_uid) // Reduces source code bloat
                            .to_base256_chars(tmp);
                        dst.Write(tmp, 0, len);
                    }

                dst.Write("*/");
                top = pos;
            }

            dst.Write(src_code[top..]);

            dst.Flush();
            dst.Close();

            return transmittable_packs_without_related;
        }

        /// <summary>
        /// Indicates whether this project has imported projects.
        /// </summary>
        public bool has_imported_projects => symbol!.Interfaces.Any(I => entities.TryGetValue(I, out var value) && value is ProjectImpl);

        /// <summary>
        /// Refreshes the project by re-initializing if any project files have changed since last processing.
        /// </summary>
        /// <param name="on_time">The time of the last refresh check.</param>
        /// <returns>True if the project was refreshed, false otherwise.</returns>
        public static bool refresh(DateTime on_time)
        {
            if (processing_files.All(path => new FileInfo(path).LastWriteTime < processing_time)) return on_time < processing_time; // Check if all files' last write times are older than processingTime.
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
            for (var i = 0; i < name.Length; i++)
                if (char.IsLower(name[i])) { return name[..i] + char.ToUpper(name[i]) + name[(i + 1)..]; }

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
        public static FieldInfo fld_reflection(ISymbol field)
        {
            var str = field.ToString()!;
            var i = str.LastIndexOf('.');
            return types[str[..i]].GetField(str[(i + 1)..], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)!;
        }

        /// <summary>
        /// Initializes the project by parsing source files, compiling them, and extracting protocol description information.
        /// This method is static and resets all project-related static data.
        /// </summary>
        /// <returns>The root ProjectImpl instance after initialization.</returns>
        public static ProjectImpl init() // These attributes are required only for the code generator, not for the observer.
        {
            processing_files.Clear();
            processing_time = DateTime.Now;
            projects.Clear();

            // Parse syntax trees from provided paths.
            var trees = new[] { AdHocAgent.provided_path }
                        .Concat(AdHocAgent.provided_paths)
                        .Select(path =>
                                {
                                    // Add a file path and last write time to the respective lists.
                                    processing_files.Add(path);

                                    // Read the source code from the file.
                                    StreamReader file = new(path);
                                    var src = file.ReadToEnd();
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
                                                       new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                                                    optimizationLevel: OptimizationLevel.Debug,
                                                                                    warningLevel: 0, // Suppress warnings by setting warning level to 0.
                                                                                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

            // Instantiate a protocol description parser with the compilation result.
            var parser = new Protocol_Description_Parser(compilation);

            // Emit the compiled assembly to a memory stream.
            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms); // Write IL code into memory.

                // Check if the compilation was successful.
                if (!result.Success)
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
                foreach (var type in _types) types.Add(type.ToString().Replace("+", "."), type);
            }

            // Visit all syntax trees to parse project details.
            foreach (var tree in trees)
                parser.Visit(tree.GetRoot());

            // Ensure at least one project is detected.
            if (projects.Count == 0)
                AdHocAgent.exit($"No project detected. Provided file {AdHocAgent.provided_path} is incomplete or in the wrong format. Try using the init template.");

            var problematic_projects = string.Join(",\n", projects.Where(prj => string.IsNullOrEmpty(prj._namespacE)).Select(prj => prj.symbol! + " in the file " + prj.node!.SyntaxTree.FilePath));
            if (problematic_projects != "")
            {
                AdHocAgent.LOG.Error("The following projects do not have a namespace defined: {problematic_projects}", problematic_projects);
                AdHocAgent.exit("Please define the appropriate namespaces and try again.");
            }

            var root_project = projects[0]; // Set the first project as the root and include it in the project structure.
            root_project._included = true;

            root_project.source = AdHocAgent.zip(processing_files); // Create a zip of the processing files to the root project.

            #region Create proxy-packs for imported projects
            // Proxy packs are designed to encapsulate all relevant information from imported projects while maintaining and supporting the hierarchical structure of projects-packs.
            foreach (var prj in projects.Skip(1))
            {
                prj.proxy = new HostImpl.PackImpl((Entity)prj);
                foreach (var e in entities.Values.Where(e => e.parent_by_source_code == prj))
                    e.parent_artificial = prj.proxy;
            }
            #endregion


            var once = new HashSet<object>(20); // A HashSet is used to track objects and avoid cycles in references (prevent circular references).
            root_project.Init(once);            // Initialize the root project

            #region Detect projects with same uid
            foreach (var by_uid in projects
                                   .GroupBy(p => p.uid)
                                   .Where(g => g.Count() > 1))
            {
                var chars = new char[10];

                AdHocAgent.exit($"Projects:\n{string.Join("\n", by_uid.Select(prj => prj.symbol!.ToString()))}\n have the same UID /*{new string(chars, 0, by_uid.Key.to_base256_chars(chars))}*/." +
                                //
                                $"Please manually change the project UID to /*{new string(chars, 0, (by_uid.Key + 1).to_base256_chars(chars))}*/.");
            }
            #endregion


            // Collect all typedef packs and preserve them while removing from the root project.
            var typedefs = root_project.all_packs.Where(pack => pack.is_typedef).Distinct().ToArray();
            root_project.all_packs.RemoveAll(pack => pack.is_typedef);

            // Include all constants-packs and enums in the root project by default.
            foreach (var pack in root_project.constants_packs)
                pack._included = true;


            #region Process in the root_project collected channels
            // Filter channels, exclude modifiers, include only valid ones, and assign each a unique index.
            root_project.channels = root_project.channels
                                                .Distinct()
                                                .Select((ch, idx) =>
                                                        {
                                                            ch._included = true;
                                                            ch.idx = idx; // Assign index to each channel.
                                                            return ch;
                                                        })
                                                .ToList();

            // If no valid channels are found, exit with an error.
            if (root_project.channels.Count == 0)
                AdHocAgent.exit("There is no information available about communication channels.", 45);

            foreach (var ch in root_project.channels) ch.set_transmitting_packs(once);
            #endregion


            #region Collect, enumerate, and validate hosts
            // Filter, deduplicate, and sort hosts
            root_project.hosts = root_project.hosts
                                             .Where(host => host.included && !host.IsModifier)
                                             .Distinct()
                                             .OrderBy(host => host._name)
                                             .ToList();

            // Assign each host a unique index.
            for (var idx = 0; idx < root_project.hosts.Count; idx++)
                root_project.hosts[idx].idx = idx;

            // Validate that all hosts have language implementation information.
            var missingLangInfo = root_project.hosts.Where(host => host._langs == 0).ToList();
            if (missingLangInfo.Any())
            {
                foreach (var host in missingLangInfo)
                    AdHocAgent.LOG.Error(
                                         "The host {host} lacks language implementation information. Please use the C# `///<see cref=\"InLANG\"/>` XML comment on the host to add it.",
                                         host.symbol
                                        );

                AdHocAgent.exit("Correct detected problems and restart.", 45);
            }

            // Remove from each host's scope any packs registered at the project level.
            foreach (var host in root_project.hosts)
                host.packs.RemoveAll(pack => root_project.constants_packs.Contains(pack));
            #endregion


            HostImpl.PackImpl.FieldImpl.init(root_project);

            #region Process typedefs
            {
                var flds = raw_fields.Values;
                // Continue processing until no further changes are made.
                for (var rerun = true; rerun;)
                {
                    rerun = false;

                    foreach (var T in typedefs)
                    {
                        var src = T.fields[0];
                        raw_fields.Remove(src.symbol!); // Remove typedef field.

                        foreach (var dst in flds)
                        {
                            void copy_type(HostImpl.PackImpl.FieldImpl src, HostImpl.PackImpl.FieldImpl dst)
                            {
                                if ((src.is_Map || src.is_Set) && (dst.is_Map || dst.is_Set || dst._name == ""))
                                    AdHocAgent.exit($"The type definition '{src.symbol}' declares a Map/Set that contains another Map/Set type '{dst.symbol}'. " +
                                                    "This nested Map/Set declaration is not supported. Please adjust the type hierarchy and try again.");

                                rerun = true;

                                // Copy basic type properties from source to destination.
                                dst.exT_pack = src.exT_pack;
                                dst.exT_primitive = src.exT_primitive;
                                dst.inT = src.inT;

                                dst._dir = src._dir;

                                // Check for type nesting or clashes.
                                if (
                                    dst._map_set_len != null && src._map_set_len != null ||
                                    dst._map_set_array != null && src._map_set_array != null ||
                                    dst._exT_len != null && src._exT_len != null ||
                                    dst._exT_array != null && src._exT_array != null
                                )
                                {
                                    AdHocAgent.LOG.Error("Typedef {typedef} may generate invalid type nesting or clashes when embedded in {field}.", T, dst);
                                    AdHocAgent.exit("Please fix the problem and rerun");
                                }

                                // Handle an edge case for Map Value types.
                                if (dst._name == "") //Map Value
                                    if (src._map_set_len != null ||
                                        src._map_set_array != null || src.dims != null)
                                    {
                                        AdHocAgent.LOG.Error("Typedef {typedef} may generate invalid type nesting or clashes when embedded in Value type of {field}.", T, dst);
                                        AdHocAgent.exit("Please fix the problem and rerun");
                                    }

                                // Merge dimensions.
                                if (src.dims != null)
                                    if (dst.dims == null) dst.dims = src.dims;
                                    else dst.dims = dst.dims.Concat(src.dims).ToArray();
                                // Copy map and array properties.
                                if (src._map_set_len != null) dst._map_set_len = src._map_set_len;
                                if (src._map_set_array != null) dst._map_set_array = src._map_set_array;

                                if (src._exT_len != null) dst._exT_len = src._exT_len;
                                if (src._exT_array != null) dst._exT_array = src._exT_array;

                                if (src.is_Map) dst.V = src.V; // Copy map value properties.

                                dst._min_value = src._min_value;
                                dst._max_value = src._max_value;

                                dst._min_valueD = src._min_valueD;
                                dst._max_valueD = src._max_valueD;

                                dst._bits = src._bits;

                                // Merge null info.
                                if (src._null_value.HasValue)
                                {
                                    dst._null_value = dst._null_value == null ?
                                                          src._null_value :
                                                          (byte)(dst._null_value.Value | src._null_value.Value);

                                    if ((dst.is_Map || //Map Key
                                         dst._name == "" || //Map Value
                                         dst.is_Set) &&
                                        src.fld_node?.Declaration.Type is NullableTypeSyntax) //declare looks like T? field;
                                        dst.set_null_value_bit(2);                             //set Generic nullable bit
                                }
                            }


                            // Apply typedef fields to matching destination fields.

                            if (SymbolEqualityComparer.Default.Equals(T.symbol, dst.exT_pack)) copy_type(src, dst);

                            if (dst.is_Map && SymbolEqualityComparer.Default.Equals(T.symbol, dst.V!.exT_pack)) copy_type(src, dst.V);
                        }
                    }
                }
            }
            #endregion


            HostImpl.PackImpl.init(root_project);
            #region Resolve Host Language Configurations
            foreach (var host in root_project.hosts)
            {
                // Start with the initial default value from the host's constructor.
                var finalDefault = 0xFFFFFFFF;

                foreach (var scope in host.LangScopes)
                    if (scope.Targets.Count == 0) // An empty scope sets or overwrites the default. The last one processed wins.
                        finalDefault = scope.Config;
                    else
                        // This is an override scope. Populate the final implementation dictionaries.
                        foreach (var targetSymbol in scope.Targets)
                            if (targetSymbol.Kind == SymbolKind.Field)
                                host.field_impl[targetSymbol] = (Project.Host.Langs)(scope.Config >> 16);
                            else if (targetSymbol.Kind == SymbolKind.NamedType)
                                if (named_packs.TryGetValue(targetSymbol, out var packSet)) // It's a Pack Set. Apply the config to each pack within the set.
                                    foreach (var pack in packSet.packs)
                                        host.pack_impl[pack.symbol!] = scope.Config;
                                else
                                    // It's a single Pack.
                                    host.pack_impl[targetSymbol] = scope.Config;

                // After iterating through all scopes, set the final default value on the host.
                // This value will be used for any pack not explicitly mentioned in an override scope.
                host._default_impl_hash_equal = finalDefault;
            }
            #endregion

            #region Distribute transmittable packs across available channels
            HashSet<HostImpl.PackImpl> packs_ = [];

            foreach (var ch in root_project.channels)
            {
                packs_.Clear();
                foreach (var pack in ch.hostL_transmitting_packs) pack.related_packs(packs_);
                foreach (var pack in ch.hostL_transmitting_packs) packs_.Remove(pack); //subpacks purification
                ch.hostL_related_packs.AddRange(packs_);
                ch.hostL_related_packs.ForEach(pack => pack._included = true); //mark

                packs_.Clear();
                foreach (var pack in ch.hostR_transmitting_packs) pack.related_packs(packs_);
                foreach (var pack in ch.hostR_transmitting_packs) packs_.Remove(pack); //subpacks purification
                ch.hostR_related_packs.AddRange(packs_);
                ch.hostR_related_packs.ForEach(pack => pack._included = true); //mark
            }
            #endregion

            // Read packs, add their IDs, and update project metadata, excluding the root project.
            once.Clear();

            // transmittable packs without related
            var packs = root_project.read_packs_id_info_and_write_update(once); //temporary


            // Add transmittable packs and enums/constants marked as "included" to the collection.
            packs.UnionWith(root_project.all_packs.Where(p => p.included && !p.is_Header));
            packs.UnionWith(root_project.constants_packs.Where(c => c.included));

            foreach (var pack in root_project.all_packs.Where(pack => pack._id is (int)Project.Host.Pack.Field.DataType.t_subpack or (int)Project.Host.Pack.Field.DataType.t_constants))
                pack.fields.RemoveAll(fld => fld._name.StartsWith("_header")); //Headers are only present on **Standalone Packets**, not **Sub-Packets**.

            root_project.all_packs.RemoveAll(pack => pack._id is (int)Project.Host.Pack.Field.DataType.t_subpack or (int)Project.Host.Pack.Field.DataType.t_constants);

            #region Ensure unique names, resolving conflicts in-place before user confirmation.
            var header_names = new HashSet<string>();
            var fields_names = new HashSet<string>();
            var was_renamed = false;


            foreach (var pack in root_project.header_packs)
            {
                var suffix = 1;
                var originalName = pack._name;
                while (header_names.Contains(pack._name))
                {
                    was_renamed = true;
                    pack._name = $"{originalName}_{suffix++}";
                }

                if (originalName != pack._name)
                    AdHocAgent.LOG.Warning($"Header '{pack.symbol}' at line {pack.line_in_src_code}: Name '{originalName}' was a duplicate and proposed to be renamed to '{pack._name}'.");

                header_names.Add(pack._name);
                foreach (var fld in pack.fields)
                {
                    suffix = 1;
                    originalName = fld._name;
                    while (fields_names.Contains(fld._name) || fld._name == pack._name)
                    {
                        was_renamed = true;
                        fld._name = $"{originalName}_{suffix++}";
                    }

                    if (originalName != fld._name)
                        AdHocAgent.LOG.Warning($"Field '{fld.symbol}' at line {fld.line_in_src_code}: Name '{originalName}' had a conflict and proposed to be renamed to '{fld._name}'.");

                    fields_names.Add(fld._name);
                }
            }

            if (was_renamed)
            {
                AdHocAgent.LOG.Warning("Proposed renames were performed to resolve naming conflicts. Do you accept these changes and wish to continue? (yes/no)");

                if (Console.ReadLine()?.Trim().ToLower() != "yes")
                    AdHocAgent.exit("User declined the automatic renames. Please correct the source files manually and restart.");
            }
            #endregion

            var nullableHeaderFields = string.Join('\n', root_project
                                                         .header_packs
                                                         .SelectMany(pack => pack.fields)
                                                         .Where(fld => fld.nullable)
                                                         .Select(fld => fld.symbol + " at line:" + fld.line_in_src_code));
            if (0 < nullableHeaderFields.Length)
                AdHocAgent.exit("Error: Nullable header fields are not allowed.\n" +
                                "Please correct the following fields:\n" +
                                nullableHeaderFields);


            // Include packs not transmitted but necessary for building the namespace hierarchy.
            foreach (var p in packs.ToArray())
                for (var pack = p; pack.parent_by_source_code is HostImpl.PackImpl parent_pack; pack = parent_pack)
                    if (packs.Add(parent_pack))
                    {
                        parent_pack._id = (ushort)Project.Host.Pack.Field.DataType.t_constants; //make them totally empty shell
                        parent_pack.fields.Clear();
                        parent_pack._constants_.Clear();
                    }

            // Traverse hierarchy upwards and ensure parent packs are included as shells for namespace support.
            foreach (var pack in packs.ToArray())
                for (var p = pack; ;)
                    if (p.parent_by_source_code is HostImpl.PackImpl parent_pack) //run up to hierarchy
                    {
                        parent_pack._included = true; // Mark container packs as included.

                        if (!packs.Contains(parent_pack))
                        {
                            pack._id = (ushort)Project.Host.Pack.Field.DataType.t_constants; // Mark as true shell pack.
                            pack.fields.Clear();                                             // Keep constants only for hierarchical support.
                            packs.Add(pack);
                        }

                        p = parent_pack;
                    }
                    else break;

            // Add packs from hosts, ensuring they are marked as included
            foreach (var host in root_project.hosts)
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

            packs.Clear(); // re-use
            #region Detect redundant pack's language information.
            root_project.hosts.ForEach(host =>
                                       {
                                           packs.Clear(); // re-use

                                           foreach (var ch in root_project.channels.Where(ch => ch.hostL == host || ch.hostR == host))
                                           {
                                               packs.UnionWith(ch.hostL_transmitting_packs);
                                               packs.UnionWith(ch.hostL_related_packs);

                                               packs.UnionWith(ch.hostR_transmitting_packs);
                                               packs.UnionWith(ch.hostR_related_packs);
                                           }
                                           //now in packs all host's transmitted and received packs


                                           //check enums usage, and to do precise distributions among hosts
                                           var host_used_enums = packs
                                                                 .SelectMany(pack => pack.fields)
                                                                 .Select(fld => fld.exT_pack)
                                                                 .Where(exT => exT != null && exT.EnumUnderlyingType != null)
                                                                 .Select(exT => (HostImpl.PackImpl)entities[exT]).Distinct();


                                           //import used enums in the  host scope
                                           host.packs.AddRange(host_used_enums);
                                           host.packs = host.packs.Distinct().ToList();
                                       });


            foreach (var ch in root_project.channels) //Remove non-transmittable packs
            {
                ch.hostL_related_packs.RemoveAll(pack => !pack.is_transmittable);
                ch.hostR_related_packs.RemoveAll(pack => !pack.is_transmittable);
            }


            //To make packs that are present in every host's scope globally available, move them to the topmost scope of the project.
            if (root_project.hosts.All(host => 0 < host.packs.Count))
            {
                packs.Clear(); //re-use
                packs.UnionWith(root_project.hosts[0].packs);
                root_project.hosts.ForEach(host => packs.IntersectWith(host.packs));

                //now in the `packs` only globaly used packs, move them on the top by deleting from narrow hosts scope
                root_project.hosts.ForEach(host => host.packs.RemoveAll(pack => packs.Contains(pack)));
            }
            #endregion

            #region Set pack indices and collect all fields
            HashSet<HostImpl.PackImpl.FieldImpl> fields = [];
            HashSet<HostImpl.PackImpl.ConstantImpl> constants = [];

            for (var idx = 0; idx < root_project.packs.Count; idx++) // Assign each pack a unique index and collect its fields.
            {
                var pack = root_project.packs[idx];
                pack.across_idx = (ushort?)(pack.idx = idx); // Assign the pack's storage index.

                fields.AddRange(pack.fields);         // Collect all fields from the pack.
                constants.AddRange(pack._constants_); // Collect all constants from the pack.
            }


            constants.AddRange(root_project.hosts.SelectMany(host => host._constants_));                            // Collect all constants from the hosts.
            constants.AddRange(root_project.channels.SelectMany(ch => ch._constants_));                             // Collect all constants from the channels.
            constants.AddRange(root_project.channels.SelectMany(ch => ch.stages.SelectMany(st => st._constants_))); // Collect all constants from the stages.

            //for more predictable stable order
            root_project.fields = fields.OrderBy(fld => root_project.packs.First(pack => pack.fields.Contains(fld)).full_path + fld._name).ToList();
            root_project.constant_fields = constants.OrderBy(fld => root_project.packs.FirstOrDefault(pack => pack._constants_.Contains(fld))?.full_path ?? "" + fld._name).ToList();

            for (var idx = 0; idx < root_project.fields.Count; idx++) root_project.fields[idx].idx = idx; //set fields  idx
            for (var idx = 0; idx < root_project.constant_fields.Count; idx++) root_project.constant_fields[idx].idx = idx; //set fields  idx
            #endregion

            var transmittable = root_project.packs.Where(p => !p.is_Header).ToArray();


            // Windows file system treats file and directory names as case-insensitive. FOO.txt and foo.txt are treated as equivalent.
            // Linux file system treats file and directory names as case-sensitive. FOO.txt and foo.txt are treated as distinct files.
            //
            // This creates a problem in Java, where case-sensitive class names can cause issues when compiled on a case-insensitive file system like Windows.
            // The best workaround is to detect and prevent this situation.

            var problem = false;

            foreach (var by_parent in transmittable.GroupBy(pack => pack.parent_by_source_code))
                foreach (var by_name in by_parent.GroupBy(pack => pack._name.ToLower()).Where(g => 1 < g.Count()))
                {
                    AdHocAgent.LOG.Error(@"Detected duplicate nested types in the pack {pack} : {nested_types} ",
                                         by_parent.Key == null ?
                                             root_project.symbol :
                                             by_parent.Key is HostImpl.PackImpl pack ?
                                                 pack.symbol :
                                                 by_parent.Key.symbol,
                                         string.Join(", ", by_name.Select(x => x._name)));
                    problem = true;
                }

            if (problem)
                AdHocAgent.exit(@"File and directory naming conventions differ across file systems:  
- On Windows, names are case-insensitive (e.g., `FOO.txt` and `foo.txt` are equivalent).  
- On Linux, names are case-sensitive (e.g., `FOO.txt` and `foo.txt` are distinct).  

This inconsistency can cause issues in Java, where case-sensitive class names may conflict when compiled into class files on case-insensitive systems like Windows.  
Resolution: Rename duplicate nested types to ensure compatibility across file systems.");


            var packs_with_parent = transmittable.Where(pack => pack._parent != null).ToArray();

            bool repeat;
            do
            {
                repeat = false;
                foreach (var pack in packs_with_parent)
                    if (pack.parent_by_source_code != null && pack._name.Equals(pack.parent_by_source_code!._name))
                    {
                        var new_name = mangling(pack._name);
                        AdHocAgent.LOG.Warning("Pack `{Pack}` is declared inside body of the parent pack {ParentPack} and has the same as parent name . Some languages (Java) not allowed this.\n The name will be changed to `{NewName}`",
                                               pack.symbol, pack.parent_by_source_code._name, new_name);
                        pack._name = new_name;
                        repeat = true; //name changed, this may bring new conflict, repeat check
                    }
            }
            while (repeat);

            #region Update referred status of packs
            foreach (var pack in transmittable)
                pack._referred = pack.is_transmittable && root_project.fields.Any(fld => fld.get_exT_pack == pack || fld.is_Map && fld.V.get_exT_pack == pack);
            #endregion

            #region Read attributes for packs, fields, hosts and channels
            for (int ii = 0, max = transmittable.Length; ii < max; ii++) //the collection root_project.packs may grow!
            {
                var pack = transmittable[ii];
                if (pack.node != null) // real pack
                    pack.read_attributes(pack.model, pack.node);

                foreach (var fld in pack.fields)
                    fld.read_attributes(fld.model, fld.fld_node!);
            }

            root_project.hosts.ForEach(host => host.read_attributes(host.model, host.node!));
            root_project.channels.ForEach(ch =>
                                          {
                                              ch.read_attributes(ch.model, ch.node!);
                                              ch.stages.ForEach(st => st.read_attributes(st.model, st.node!));
                                          });
            #endregion

            // Configures `across_idx` for managing parent-child relationships.
            //  For instance, a pack declaration may be nested within another pack, a project, or a host. To manage this, we need a consistent ordering.
            // `across_idx` is assigned sequentially across a virtual collection of collections ordered as follows: packs, hosts, channels, and stages.


            var acrossIdx = (ushort)root_project.packs.Count; //

            foreach (var host1 in root_project.hosts) host1.across_idx = acrossIdx++;

            foreach (var channel in root_project.channels) channel.across_idx = acrossIdx++;

            foreach (var stage in root_project.channels.SelectMany(channel => channel.stages)) stage.across_idx = acrossIdx++;

            return root_project;
        }


        public override void Init(HashSet<object> once)
        {
            // Process imported projects first.
            foreach (var I in symbol!.Interfaces)
                if (entities.TryGetValue(I, out var value) && value is ProjectImpl prj)
                {
                    prj.proxy!.parent_artificial = proxy; // Build artificial hierarchy for proxies.
                    prj.Init(once);                       // Recursively initialize an imported project.

                    if (imported_projects_uid == null)
                    {
                        imported_projects_uid = [prj.uid];
                        prj.proxy!.uid = 0xFFFF - 0; //If the UID is in the range 0xFFFF - 255 to 0xFFFF, this pack acts as a project's proxy.
                    }
                    else
                    {
                        var i = Array.IndexOf(imported_projects_uid, uid);
                        if (i == -1)
                        {
                            Array.Resize(ref imported_projects_uid, imported_projects_uid.Length + 1);
                            imported_projects_uid[^1] = prj.uid;
                            prj.proxy.uid = (ulong)(0xFFFF - (imported_projects_uid.Length - 1)); //If the UID is in the range 0xFFFF - 255 to 0xFFFF, this pack acts as a project's proxy.
                        }
                        else
                            prj.proxy.uid = (ulong)i; //If the UID is in the range 0xFFFF - 255 to 0xFFFF, this pack acts as a project's proxy.
                    }

                    // Helper to check for duplicate names within a set of entities.
                    void check_duplicate(string prefix, IEnumerable<Entity> set)
                    {
                        var error = set.GroupBy(c => c._name).Where(g => 1 < g.Count()).Aggregate("", (current, i) => current + string.Join("\n", i.Select(e => e.symbol)) + "\n");

                        if (error != "")
                            AdHocAgent.exit($"The following {prefix} have duplicate names: \n{error}This is unacceptable. Please resolve the issue.");
                    }

                    // Merge entities from imported projects.
                    hosts.AddRange(prj.hosts);
                    check_duplicate("hosts", hosts);

                    channels.AddRange(prj.channels.Where(ch => !ch.is_Modifier));
                    check_duplicate("channels", channels);

                    constants_packs.AddNew(prj.constants_packs);

                    all_packs.AddNew(prj.all_packs);
                }

            // If this project doesn't have a UID yet, generate a new one.
            if (uid == ulong.MaxValue) //! process after imported projects
                updated_uid.Add((
                                    uid_pos,
                                    uid = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 0x192_8A31_D95EUL // Generate a "unique" identifier based on the current time.
                                ));

            foreach (var host in hosts.Where(host => host.IsModifier)) //process host modifications
            {
                var modify_host = host.modify_host;

                modify_host._langs |= host._langs; // Merge the language targets (e.g., InCS, InJAVA).

                // Merge the unresolved language configuration scopes from the modifier to the target host.
                // The main resolution logic in the init() method will process these scopes later,
                // after Pack Sets have been fully resolved.
                modify_host.LangScopes.AddRange(host.LangScopes);
            }

            hosts.RemoveAll(host => host.IsModifier); // Remove modifier hosts after they've been applied.

            // Multi-pass initialization for different entity types to resolve dependencies.
            once.Clear();
            start();
            foreach (var pack in named_packs.Values) pack.Init(once);
            if (restart())
                foreach (var pack in named_packs.Values)
                    pack.Init(once);

            start();
            foreach (var pack in injector_packs) pack.Init(once);
            if (restart())
                foreach (var pack in injector_packs)
                    pack.Init(once);

            foreach (var injector in injector_packs) //inject template fields
                foreach (var target in injector.target_packs!.Where(pack => !pack.is_Header && !pack.is_HeaderModifier && !pack.is_Modifier && !pack.is_FieldsInjectInto))
                {
                    target.fields.RemoveAll(fld => injector.fields.Any(f => fld._name == f._name));
                    target.fields.AddRange(injector.fields);
                }


            start();
            foreach (var pack in header_packs) pack.Init(once);
            if (restart())
                foreach (var pack in header_packs)
                    pack.Init(once);

            //==================
            start();
            foreach (var header_modifier in header_mofifiers)
            {
                if (header_modifier.target_packs == null)
                {
                    var header = (HostImpl.PackImpl)entities[header_modifier.this_modify.First().TypeArguments[0]];
                    header_modifier.target_packs = header.target_packs; //redirect modification to header of the header target packs collection
                }

                header_modifier.Init(once);
            }

            if (restart())
                foreach (var header_modifier in header_mofifiers)
                    header_modifier.Init(once);


            foreach (var header_modifier in header_mofifiers) //inject fields to headers from modifiers
            {
                var header = (HostImpl.PackImpl)entities[header_modifier.this_modify.First().TypeArguments[0]];
                header.fields.RemoveAll(fld => header_modifier.fields.Any(f => fld._name == f._name));
                header.fields.AddRange(header_modifier.fields);
            }

            //===================
            for (var index = 0; index < header_packs.Count; index++)
            {
                var header = header_packs[index];
                foreach (var target in header.target_packs!.Where(pack => !pack.is_Header && !pack.is_FieldsInjectInto))
                {
                    var fld = new HostImpl.PackImpl.FieldImpl(this, null, null); //artificial field bind to the header definition
                    fld._name = "_header" + index;                            //special unique name
                    fld.exT_pack = header.symbol;                                //reference to the header pack
                    target.fields.Add(fld);
                }
            }


            once.Clear();
            start();
            foreach (var ch in channels) ch.Init(once);
            if (restart())
                foreach (var ch in channels)
                    ch.Init(once);


            once.Clear(); // Stages are processed only once, as there are no cycle-referencing dependencies.
            start();
            foreach (var st in channels.SelectMany(ch => ch.stages).ToArray())
                st.Init(once); //calculate + create branches !!!


            once.Clear();
            start();
            foreach (var pack in all_packs)
            {
                pack.Init(once);
                pack._nested_max = (byte)pack.calculate_fields_type_depth(once);
            }

            if (restart())
                foreach (var pack in all_packs)
                    pack.Init(once);


            #region Assign project scope UIDs
            List<Entity> unassigned = [];
            HashSet<ulong> assigned = [];

            void set_persistent_uid(IEnumerable<Entity> entities) // Our goal is to minimize the UID, to reduce the footprint in the source code
            {
                var uid = 0U;
                // Check for duplicate UIDs within the same project.
                foreach (var by_projects in entities
                                            .Where(e => e.uid < ulong.MaxValue)
                                            .GroupBy(e => e.project)) // Group by project
                {
                    foreach (var by_uid in by_projects
                                           .GroupBy(e => e.uid)
                                           .Where(g => g.Count() > 1)) // Find duplicate UIDs within the project group
                    {
                        var list = string.Join("\n", by_uid.Select(e => $"{e.symbol}  line: {e.line_in_src_code}"));
                        AdHocAgent.LOG.Warning(
                                               "Duplicate entities detected: {List} with the same UID = {Id}. This may have been accidentally copied. Please delete the duplicate assignment.",
                                               list,
                                               $"/*{by_uid.Key.to_base256_chars()}*/"
                                              );
                        AdHocAgent.exit("", 66);
                    }
                }

                foreach (var entity in entities.Where(e => e.origin == null)) // Exclude clones and include only items belonging to this project
                    if (entity.uid == ulong.MaxValue) unassigned.Add(entity);
                    else assigned.Add(entity.uid);

                foreach (var entity in unassigned)
                {
                    while (assigned.Contains(uid)) uid++;
                    if (entity.uid_pos < 0) throw new Exception();
                    updated_uid.Add((entity.uid_pos, uid));
                    entity.uid = (ushort)uid++;
                }

                assigned.Clear();
                unassigned.Clear();
            }

            set_persistent_uid(hosts.Where(e => e.project == this));


            set_persistent_uid(channels.Where(e => e.project == this));


            set_persistent_uid(all_packs.Concat(constants_packs).Where(p => !p.is_typedef && p.project == this));

            HashSet<ChannelImpl.BranchImpl> unassigned_ = [];

            foreach (var stages in channels.Where(e => e.project == this).Select(channel => channel.stages))
            {
                set_persistent_uid(stages);

                foreach (var stage in stages)
                {
                    foreach (var branch in stage.branchesR.Where(br => br.origin == null))
                        if (branch.uid == ulong.MaxValue) unassigned_.Add(branch);
                        else assigned.Add(branch.uid);

                    foreach (var branch in stage.branchesL.Concat(stage.branchesR.Where(br => br.origin == null))) //exclude R branches of LR (clones of L)
                        if (branch.uid == ulong.MaxValue) unassigned_.Add(branch);
                        else assigned.Add(branch.uid);

                    var uid = 0UL;
                    foreach (var branch in unassigned_)
                    {
                        while (assigned.Contains(uid)) uid++;
                        if (branch.uid_pos < 0) throw new Exception();
                        updated_uid.Add((branch.uid_pos, uid));
                        branch.uid = (ushort)uid++;
                    }

                    foreach (var br in stage.branchesR.Where(br => br.origin != null)) //only R branches of LR (clones of L)
                    {
                        if (br.origin!.uid == ulong.MaxValue) //it can happen if br.origin was deleted as the result of modification
                        {
                            while (assigned.Contains(uid)) uid++;
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

        public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once)
        {
            if (entities.TryGetValue(by_what, out var value)) //project's entity
                switch (value)
                {
                    case ProjectImpl prj: return;
                    case HostImpl host:
                        if (add)
                            AdHocAgent.LOG.Information("Only enums, constant sets, channels, or other projects can be imported into the project. Importing the host {host} will be ignored. Instead, reference the host {host} as the endpoint within the {project} channels.", host, host, _name);
                        else
                        {
                            hosts.Remove(host);
                            channels.RemoveAll(ch => ch.hostR == host || ch.hostL == host);
                        }

                        break;
                    case HostImpl.PackImpl pack: //import single pack to the project

                        if (pack.is_constants_set || pack.is_enum)
                        {
                            if (!add) constants_packs.Remove(pack);
                            else if (!constants_packs.Contains(pack)) constants_packs.Add(pack);
                            return;
                        }

                        if (add)
                            AdHocAgent.LOG.Information("Only enums, constant sets, channels, or other projects may be imported into the project. Importing the pack {pack}  will be ignored. instead, reference the pack {pack} in a branch of a stage within the {project} channels.", pack, pack, _name);
                        else
                            foreach (var st in channels.SelectMany(ch => ch.stages))
                            {
                                foreach (var br in st.branchesL)
                                    br.packs.Remove(pack);
                                foreach (var br in st.branchesR)
                                    br.packs.Remove(pack);
                            }

                        break;
                    case ChannelImpl channel: // import single channel to the project.

                        if (!add) channels.Remove(channel);
                        else if (!channels.Contains(channel)) channels.Add(channel);
                        return;
                }
            else { AdHocAgent.exit($"The source code for '{by_what}' could not be found. Have you remembered to include the source files of other imported projects?"); }
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
            this.node = node;

            if (root_project == null) return; //not root project
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
        /// A byte array containing the zipped source code of all files in the project.
        /// </summary>
        public byte[] source = [];

        /// <summary>
        /// Provides access to the zipped source code array for serialization.
        /// </summary>
        /// <returns>An object containing the zipped source code byte array.</returns>
        public object? _source() => source;

        /// <summary>
        /// Gets the length of the zipped source code byte array.
        /// </summary>
        public int _source_len => source.Length;

        /// <summary>
        /// Retrieves a specific byte from the zipped source code array at the given index.
        /// </summary>
        /// <param name="ctx">The transmitter context (not used here).</param>
        /// <param name="slot">The transmitter slot (not used here).</param>
        /// <param name="item">The index of the byte to retrieve.</param>
        /// <returns>The byte at the specified index in the zipped source code array.</returns>
        public byte _source(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item) => source[item];


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
        public Project.Host _hosts(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int d) => hosts[d];


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
        public Project.Host.Pack.Constant _constant_fields(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item) => constant_fields[item];

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
        public Project.Host.Pack.Field _fields(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item) => fields[item];

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
        public Project.Host.Pack _packs(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int d) => packs[d];

        /// <summary>
        /// List of channels defined in this project.
        /// </summary>
        public List<ChannelImpl> channels = [];

        /// <summary>
        /// Gets the count of channels in this project.
        /// </summary>
        public int _channels_len => channels.Count;

        /// <summary>
        /// Provides access to the list of channels for serialization purposes.
        /// Returns null if the list is empty.
        /// </summary>
        /// <returns>The list of channels or null if empty.</returns>
        public object? _channels() => channels.Count < 1 ?
                                          null :
                                          channels;

        /// <summary>
        /// Retrieves a specific channel at the given index.
        /// </summary>
        /// <param name="ctx">The transmitter context (not used here).</param>
        /// <param name="slot">The transmitter slot (not used here).</param>
        /// <param name="d">The index of the channel to retrieve.</param>
        /// <returns>The channel at the specified index.</returns>
        public Project.Channel _channels(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int d) => channels[d];

        /// <summary>
        /// Represents the implementation of a Host entity within a project.
        /// </summary>
        public class HostImpl : Entity, Project.Host
        {
            public byte _uid => (byte)uid;

            public ushort _contexts { get; set; } = Project.Host._contexts_.NULL;

            /// <summary>
            /// Iterates through packs within the host's scope, applying an action to transmittable packs.
            /// If depth is 0, includes only packs defined directly under the host.
            /// If depth > 0, recursively includes all nested packs within the host.
            /// </summary>
            /// <param name="depth">The depth of scope to traverse (0 for immediate host scope, >0 for deeper scopes).</param>
            /// <param name="dst">The action to apply to each transmittable pack.</param>
            public void for_packs_in_scope(uint depth, Action<PackImpl> dst)
            {
                foreach (var entity in entities.Values.Where(e => e.in_host == this && (0 < depth || e.parent_by_source_code == this)))
                    if (entity is PackImpl pack && pack.is_transmittable)
                        dst(pack);
            }

            public class LangScope
            {
                public uint Config;
                public readonly List<ISymbol> Targets = [];
            }

            public readonly List<LangScope> LangScopes = [];

            /// <summary>
            /// Gets or sets the target languages for which code should be generated for this host.
            /// </summary>
            public Project.Host.Langs _langs { get; set; }

            public override bool included => _included ?? in_project.included;
            public bool IsModifier => modify_host != null;

            /// <summary>
            /// Initializes a new instance of the <see cref="HostImpl"/> class.
            /// </summary>
            /// <param name="project">The project this host belongs to.</param>
            /// <param name="compilation">The Roslyn compilation object.</param>
            /// <param name="host">The syntax node representing the host's struct declaration.</param>
            public HostImpl(ProjectImpl project, CSharpCompilation compilation, StructDeclarationSyntax host) : base(project, compilation, host)
            {
                _default_impl_hash_equal = 0xFFFF_FFFF; // Default: Automatically generate hash code and equals methods implementation. One bit per language.
                project.hosts.Add(this);
                var contexts = host.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.ValueText == "Contexts");

                if (contexts == null) return;
                var interfaces = symbol.Interfaces;

                if (interfaces.Any(i =>
                                       equals(i.ConstructedFrom, Meta_MultiContextHost) ||
                                       equals(i.ConstructedFrom, Meta_Modify_Target) && i.TypeArguments[0].Interfaces.Any(ii => equals(ii.ConstructedFrom, Meta_MultiContextHost))
                                  ))
                {
                    ExpressionSyntax? value_expression = null;
                    if (contexts.ExpressionBody != null)
                        value_expression = contexts.ExpressionBody.Expression;
                    else if (contexts.Initializer != null)
                        value_expression = contexts.Initializer.Value;
                    else if (contexts.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration))?.ExpressionBody != null)
                        value_expression = contexts.AccessorList.Accessors.First().ExpressionBody!.Expression;

                    if (value_expression != null)
                    {
                        var constant_value = model.GetConstantValue(value_expression);
                        if (constant_value.HasValue && constant_value.Value is int contextsValue)
                            _contexts = (ushort)contextsValue;
                        else
                            AdHocAgent.exit($"The 'Contexts' property on host '{symbol}' has a malformed format. It must be a compile-time constant integer, but was '{value_expression}'.");
                    }
                    else
                        AdHocAgent.exit($"Could not parse a constant value from the 'Contexts' property on host '{symbol}'. It must be a simple constant expression like '=> 10;'.");
                }
                else
                    AdHocAgent.LOG.Warning("The host '{host}' defines a 'Contexts' property but does not implement 'org.unirail.Meta.MultiContextHost'. The property will be ignored.", symbol);
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
            /// Value: Configuration flags (uint) indicating language-specific implementation settings.
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
            public void _pack_impl_hash_equal_Init(Base.Transmitter ctx, Base.Transmitter.Slot slot) => pack_impl_enum = pack_impl.GetEnumerator();


            /// <summary>
            /// Moves to the next item in the <see cref="pack_impl"/> dictionary and retrieves the key (pack index).
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <returns>The index of the next pack.</returns>
            public ushort _pack_impl_hash_equal_NextItem_Key(Base.Transmitter ctx, Base.Transmitter.Slot slot)
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
            public uint _pack_impl_hash_equal_Val(Base.Transmitter ctx, Base.Transmitter.Slot slot) => pack_impl_enum.Current.Value;

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
            public void _field_impl_Init(Base.Transmitter ctx, Base.Transmitter.Slot slot) => field_impl_enum = field_impl.GetEnumerator();

            /// <summary>
            /// Moves to the next item in the <see cref="field_impl"/> dictionary and retrieves the key (field index).
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <returns>The index of the next field.</returns>
            public ushort _field_impl_NextItem_Key(Base.Transmitter ctx, Base.Transmitter.Slot slot)
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
            public Project.Host.Langs _field_impl_Val(Base.Transmitter ctx, Base.Transmitter.Slot slot) => field_impl_enum.Current.Value;
            #endregion

            /// <summary>
            /// List of constant packs dedicated to this host.
            /// </summary>
            public List<PackImpl> packs = []; // Host-dedicated constants packs

            /// <summary>
            /// Provides access to the list of host-dedicated packs for serialization purposes.
            /// Returns null if the list is empty.
            /// </summary>
            /// <returns>The list of host-dedicated packs or null if empty.</returns>
            public object? _packs() => 0 < packs.Count ?
                                           packs :
                                           null;

            /// <summary>
            /// Gets the count of host-dedicated packs.
            /// </summary>
            public int _packs_len => packs.Count;

            /// <summary>
            /// Retrieves the index of a specific host-dedicated pack at the given index.
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <param name="item">The index of the host-dedicated pack to retrieve.</param>
            /// <returns>The index of the host-dedicated pack at the specified index.</returns>
            public ushort _packs(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => (ushort)packs[item].idx;

            public override bool Init_As_Modifier_Dispatch_Modifications_On_Targets(HashSet<object> once) => false;
            protected override void Init_Collect_Modification(Entity target, HashSet<object> once) { }
            public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once) { }

            /// <summary>
            /// Represents the implementation of a Pack entity within a Host or Project.
            /// Packs can be sub-packs, enums, or constant sets.
            /// </summary>
            public class PackImpl : Entity, Project.Host.Pack
            {
                public ushort _uid => (ushort)uid;

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
                    if (is_transmittable) dst(this);
                    // A shallow search includes only this pack if it's transmittable.
                    if (depth == 0) return;


                    // A recursive search starts by including this pack...

                    // ...then RECURSIVELY includes all its descendant packs.
                    foreach (var entity in entities.Values.Where(e => e.parent_by_source_code == this))
                        if (entity is PackImpl pack)
                        {
                            dst(pack);
                            pack.for_packs_in_scope(depth - 1, dst);
                        }
                }

                public ushort _id { get; set; } = (int)Project.Host.Pack.Field.DataType.t_subpack; //pack id

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
                public int _fields(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => fields[item].idx;

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
                public bool is_typedef => fields is [{ _name: "TYPEDEF" }];

                /// <summary>
                /// Indicates whether this pack is transmittable (i.e., not an enum, constant set, or typedef).
                /// </summary>
                public bool is_transmittable => !is_enum && !is_constants_set && !is_typedef && !is_FieldsInjectInto && !is_Header && !is_Modifier;

                public HashSet<PackImpl>? target_packs; // is_FieldsInjectInto || is_HeaderFor collect packs

                public bool is_Header;
                public bool is_HeaderModifier;
                public bool is_FieldsInjectInto;

                #region Enum pack constructor
                /// <summary>
                /// Initializes a new instance of the <see cref="PackImpl"/> class for enum types.
                /// </summary>
                /// <param name="project">The project this pack belongs to.</param>
                /// <param name="compilation">The Roslyn compilation object.</param>
                /// <param name="ENUM">The EnumDeclarationSyntax node representing the enum.</param>
                //+ used as Attribute data holder
                public PackImpl(ProjectImpl project, CSharpCompilation? compilation = null, EnumDeclarationSyntax? ENUM = null) : base(project, compilation, ENUM)
                {
                    if ((enum_node = ENUM) == null) return;

                    _id = (ushort)(symbol.GetAttributes().Any(a => a.AttributeClass!.ToString()!.Equals("System.FlagsAttribute")) ? //enum type
                                       Project.Host.Pack.Field.DataType.t_enum_flags :
                                       Project.Host.Pack.Field.DataType.t_enum_sw); //probably need to check

                    project.constants_packs.Add(this); //enums register
                    in_host?.packs.Add(this);          //register enum on host level scope. else stays in the project scope
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
                    in_host?.packs.Add(this);                                   //register on host level scope. else stays in the project scope
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

                    if (is_attribute) return;

                    is_Header = symbol.Interfaces.Any(i => equals(i.ConstructedFrom, Meta_HeaderFor));
                    is_FieldsInjectInto = symbol.Interfaces.Any(i => equals(i.ConstructedFrom, Meta_FieldsInjectInto));

                    if (is_FieldsInjectInto) //The template class itself is not preserved as a packet; only its fields are distributed.
                    {
                        project.injector_packs.Add(this);
                        target_packs = [];
                        return;
                    }

                    if (is_HeaderModifier = symbol.Interfaces.Any(i =>
                                                                      equals(i.ConstructedFrom, Meta_Modify_Target) &&
                                                                      i.TypeArguments.Length == 1 &&
                                                                      i.TypeArguments[0].Interfaces.Any(i => equals(i.ConstructedFrom, Meta_HeaderFor))
                                                                 ))
                    {
                        project.header_mofifiers.Add(this);
                        return;
                    }

                    if (is_Header)
                    {
                        project.header_packs.Add(this);
                        target_packs = [];
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
                    uid = (ulong)(0xFFFF - projects.Count); //some kind of

                    _name = project._name;
                    _doc = project._doc;
                    _inline_doc = project._inline_doc;
                    symbol = project.symbol;
                    _constants_ = project._constants_;

                    _id = (ushort)Project.Host.Pack.Field.DataType.t_constants; //constants set
                    root_project.constants_packs.Add(this);                     // register constants set
                    in_host?.packs.Add(this);                                   //register on host level scope. else stays in the project scope
                }
                #endregion

                /// <summary>
                /// Recursively collects related packs (sub-packs and enums) into a set.
                /// </summary>
                /// <param name="dst">The set to add related packs to.</param>
                internal void related_packs(ISet<PackImpl> dst) //collect subpacks into dst ALSO incude ENUMS
                {
                    foreach (var pack in fields.Select(fld => fld.get_exT_pack).Concat(fields.Select(fld => fld.V?.get_exT_pack)).Where(pack => pack != null))
                        if (dst.Add(pack!))
                            pack.related_packs(dst);
                }

                /// <summary>
                /// Inheritance depth of this pack (currently unused).
                /// </summary>
                int inheritance_depth;

                /// <summary>
                /// Initializes static data and processes enums and default collection lengths for all packs.
                /// </summary>
                /// <param name="root_project">The root project instance.</param>
                internal static void init(ProjectImpl root_project)
                {
                    var all_fields = ProjectImpl.all_fields().ToList();


                    #region all_default_collection_capacity read, delete, and apply operations.
                    var all_default_collection_capacity = root_project.constants_packs.Where(en => en._name.Equals("_DefaultMaxLengthOf")).ToArray();

                    foreach (var pack in all_default_collection_capacity.OrderBy(en => en.in_project == root_project)) //The root project settings should be placed last in order to override all inherited project settings
                        pack._constants_.ForEach(fld =>
                                                 {
                                                     switch (fld._name)
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
                                                     }
                                                 });

                    foreach (var en in all_default_collection_capacity) // remove _DefaultMaxLengthOf from enums
                    {
                        en._constants_.ForEach(fld => raw_fields.Remove(fld.symbol!));
                        root_project.constants_packs.Remove(en);
                    }

                    //apply acquired default length settings
                    foreach (var fld in all_fields)
                    {
                        if (fld!.is_Map) fld._map_set_len ??= (uint?)FieldImpl._DefaultMaxLengthOf.Maps;
                        else if (fld.is_Set) fld._map_set_len ??= (uint?)FieldImpl._DefaultMaxLengthOf.Sets;

                        if (fld.is_String) fld._exT_len ??= (uint?)FieldImpl._DefaultMaxLengthOf.Strings;

                        if (fld._map_set_array is < 8) //no length part specified
                            fld._map_set_array = (uint)FieldImpl._DefaultMaxLengthOf.Arrays << 3 | fld._map_set_array!.Value;

                        if (fld._exT_array is < 8) //no length part specified
                            fld._exT_array = (uint)FieldImpl._DefaultMaxLengthOf.Arrays << 3 | fld._exT_array!.Value;
                    }
                    #endregion


                    #region process enums
                    foreach (var enum_ in root_project.constants_packs.Where(e => e.is_enum))
                    {
                        if (!enum_.included && enum_.in_project.included) enum_._included = true;

                        if (enum_._constants_.Count < 2)
                            AdHocAgent.exit($"Enum {enum_.symbol} line:{enum_.line_in_src_code} has only one field. This is redundant. Delete it or add more fields.");


                        switch (enum_._id) //auto-numbering
                        {
                            case (int)Project.Host.Pack.Field.DataType.t_enum_flags:
                                // For flag enums, calculate max by combining all flags using bitwise OR
                                var M = enum_._constants_.Where(fld => fld._value_int != null).Aggregate(0L, (i, fld) => i | fld._value_int!.Value);

                                var s = 1L;

                                foreach (var fld in enum_._constants_.Where(fld => fld._value_int == null))
                                {
                                    while ((M | s) == M) s <<= 1;
                                    fld._value_int = s;
                                    s <<= 1;
                                }

                                break;
                            case (int)Project.Host.Pack.Field.DataType.t_enum_sw:
                                // Find enum fields that have non-null integer values and sort by their value
                                var has_value = enum_._constants_.Where(f => f._value_int != null).OrderBy(f => f._value_int).ToArray();

                                var i = 0L;

                                if (has_value.Any()) // If there are fields with values
                                {
                                    // Check if this might be a flags enum that's missing the [Flags] attribute
                                    if (1 < has_value.Length)
                                    {
                                        // Count the number of fields that are power-of-two values (typical for flags)
                                        var flags = has_value.Select(fld => fld._value_int!).Count(val => val != 0 && (val & val - 1) == 0);

                                        // If more than half the fields are power-of-two, suggest adding [Flags] attribute
                                        if (has_value.Length / 2 < flags)
                                            AdHocAgent.LOG.Information("The`{Enum}` enum appears to be a flags enum. The {Flags} attribute may be missing. {correct} ?", enum_._name, "[Flags]", "\n[Flags]\nenum " + enum_._name + "{...}");
                                    }

                                    // Get the minimum value and set the initial counter
                                    var mIn = i = (long)has_value.First()._value_int!;

                                    // If not all static fields have values
                                    if (has_value.Length < enum_._constants_.Count)
                                    {
                                        var z = 0;
                                        // Fill in missing values between existing values
                                        foreach (var fld in has_value.Skip(1).Where(fld => ++i < fld._value_int))
                                        {
                                            while (i < fld._value_int)
                                            {
                                                // Find the next field with a null value
                                                z = enum_._constants_.FindIndex(z, f => f._value_int == null);
                                                if (z == -1) goto done; // Exit if no more null fields

                                                // Assign sequential values to null fields
                                                enum_._constants_[z++]._value_int = i++;
                                            }
                                        }

                                        // Fill in with values below the minimum value
                                        while (0 < mIn)
                                        {
                                            var fs = enum_._constants_[z++];
                                            fs._value_int ??= --mIn;
                                            if (z == enum_._constants_.Count) goto done;
                                        }

                                        // Fill in remaining null fields with increasing values
                                        while (z < enum_._constants_.Count)
                                        {
                                            var fs = enum_._constants_[z++];
                                            fs._value_int ??= ++i;
                                        }
                                    }

                                done:

                                    // Verify that the values are sequential
                                    if (enum_._constants_.OrderBy(f => f._value_int).Skip(1).Any(fld => ++mIn < fld._value_int)) goto next;
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
                        if (enum_._id == (int)Project.Host.Pack.Field.DataType.t_enum_flags)             // the enum is flag
                            max = enum_._constants_.Aggregate(0L, (i, fld) => i | fld._value_int!.Value); //set all flags


                        var this_enum_used_fields = all_fields.Where(fld => equals(enum_.symbol, fld?.exT_pack)).ToArray(); // all fields that use this enum type


                        if (enum_.is_calculate_enum_type)                  // If the user does not explicitly specify enum type (defaults to int),
                            enum_._constants_[0].set_exT_ByRange(min, max); // calculate the most efficient numeric type based on the enum's value range


                        #region Propagate enum parameters to the fields where they are used.
                        // Convert enum to boolean if it has no practical use as enum
                        // This happens when: no fields, single field, or all fields have same value
                        if (max == min && 0 < this_enum_used_fields.Length)
                        {
                            enum_._id = (ushort)Project.Host.Pack.Field.DataType.t_subpack; //mark on delete
                            var problem = enum_._constants_.Count == 0 ?
                                              " no field" :
                                              enum_._constants_.Count == 1 ?
                                                  " only one field" :
                                                  " fields with same values";

                            AdHocAgent.LOG.Warning("Enum {EnumSymbol} has {Problem}. As field data type it\'s useless and will be replaced with boolean", enum_.symbol, problem);

                            foreach (var fld in this_enum_used_fields)
                                fld.switch_to_boolean();

                            continue;
                        }

                        //rest of fields
                        foreach (var fld in this_enum_used_fields) //=================================================== propagate
                        {
                            // Set internal type range based on enum kind
                            if (enum_._id == (int)Project.Host.Pack.Field.DataType.t_enum_sw)
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
                    if (path.Count == 0) cyclic_depth = 0;

                    try
                    {
                        foreach (var datatype in fields.Where(f => f.exT_pack != null).Select(f => (PackImpl)entities[f.exT_pack!])
                                                       .Concat(fields.Where(f => f.is_Map && f.V.exT_pack != null).Select(f => (PackImpl)entities[f.V!.exT_pack!])).Distinct())
                        {
                            if (!path.Add(datatype))
                            {
                                cyclic_depth = Math.Max(cyclic_depth, path.Count);
                                continue;
                            }

                            datatype.calculate_fields_type_depth(path);
                            path.Remove(datatype);
                        }
                    }
                    catch (Exception e)
                    {
                        foreach (var fld in fields.Where(f => f.exT_pack != null && !entities.ContainsKey(f.exT_pack!))
                                                  .Concat(fields.Where(f => f.V != null && f.V.exT_pack != null && !entities.ContainsKey(f.V.exT_pack!))))
                            AdHocAgent.LOG.Error("Line {line}: Unsupported field type {type} detected for field {field}. Consider using the binary array type to put anything into the field.", fld.line_in_src_code,
                                                 fld.fld_node.Declaration.Type, fld);
                        AdHocAgent.exit("", 23);
                    }

                    return path.Count == 0 ?
                               cyclic_depth :
                               0;
                }


                public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once)
                {
                    if (target_packs != null) //is_FieldsInjectInto || is_HeaderFor collect packs
                    {
                        void apply_(PackImpl pack, bool add) //modify collection of target packs to which FieldsInjectInto of   Header applyed
                        {
                            if (add) target_packs.Add(pack);
                            else target_packs.Remove(pack);
                        }

                        ChannelImpl.NamedPackSet.collect_packs_in_scope(by_what, add, depth, apply_);
                    }
                    else if (raw_fields.TryGetValue(by_what, out var by_fld)) apply(by_fld);
                    else if (raw_static_fields.TryGetValue(by_what, out var by_st_fld)) apply2(by_st_fld);
                    else if (entities.TryGetValue(by_what, out var value) && value is PackImpl by_pack)
                    {
                        by_pack.Init(once);
                        foreach (var fLd in by_pack.fields) apply(fLd);
                        foreach (var fLd in by_pack._constants_) apply2(fLd);
                    }
                    else
                        AdHocAgent.exit($"Unexpected attempt to apply {(add ? "add" : "remove")} modification by {by_what} to the pack {symbol} (line: {line_in_src_code}).");


                    void apply(FieldImpl fld)
                    {
                        if (add)
                        {
                            var i = fields.FindIndex(f => f._name == fld._name); //with the same name

                            if (i == -1) fields.Add(fld);
                            else if (inited) fields[i] = fld; //modifier force override existing
                        }
                        else
                            fields.Remove(fld);
                    }

                    void apply2(ConstantImpl fld)
                    {
                        if (add)
                        {
                            var i = _constants_.FindIndex(s => s._name == fld._name); //same name

                            if (-1 < i)
                                if (inited) _constants_.RemoveAt(i); //modifier force
                                else return;                          //normal init

                            _constants_.Remove(fld);
                        }
                        else
                            _constants_.Remove(fld);
                    }
                }

                /// <summary>
                /// Checks if a field with the given name exists in this pack (either as a field or a constant).
                /// </summary>
                /// <param name="fld_name">The name of the field to check.</param>
                /// <returns>True if a field or constant with the given name exists, false otherwise.</returns>
                public bool exists(string fld_name) => fields.Any(fld => fld._name.Equals(fld_name)) || _constants_.Any(fld => fld._name.Equals(fld_name));

                public override string ToString() => _name +
                                                     _id switch
                                                     {
                                                         (int)Project.Host.Pack.Field.DataType.t_enum_exp => " : enum expr",
                                                         (int)Project.Host.Pack.Field.DataType.t_enum_sw => " : enum switch",
                                                         (int)Project.Host.Pack.Field.DataType.t_enum_flags => " : enum flags",
                                                         (int)Project.Host.Pack.Field.DataType.t_constants => " : constants",
                                                         (int)Project.Host.Pack.Field.DataType.t_subpack => is_Header ?
                                                                                                                " : header" :
                                                                                                                " : sub pack",
                                                         < (int)Project.Host.Pack.Field.DataType.t_subpack => $" : pack {_id} ",
                                                         _ => "???"
                                                     };

                /// <summary>
                /// Represents the implementation of a Constant entity within a Pack.
                /// </summary>
                public class ConstantImpl : HasDocs, Project.Host.Pack.Constant
                {
                    /// <summary>
                    /// Gets the value of this constant. If a substitute field is defined, its value is returned; otherwise, the constant's own value is returned.
                    /// </summary>
                    /// <returns>The value of the constant, potentially from a substituted field.</returns>
                    public object? value_of() // Get the real value, possibly from a substituted field if defined
                    {
                        // If a substitution value is defined for the field, use the substituted field’s value; otherwise, return the value of the original field

                        var info = fld_reflection((substitute_value_from == null ?
                                                       this :
                                                       raw_static_fields[substitute_value_from])
                                                  .symbol!);

                        try // Return the calculated value of the substituted or original field via runtime reflection
                        {
                            return info.GetValue(null);
                        }
                        catch (Exception e) { return info.GetRawConstantValue(); }
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
                    public string _array(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => _array_!.GetValue(item)!.ToString()!;

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
                    /// Byte array value of the constant (currently unused).
                    /// </summary>
                    public int? _value_bytes { get; set; }

                    /// <summary>
                    /// Symbol of the static field from which this constant's value is substituted (used with [ValueFor] attribute).
                    /// Null if no substitution is used.
                    /// </summary>
                    public ISymbol? substitute_value_from;

                    /// <summary>
                    /// Semantic model for accessing semantic information about the code.
                    /// </summary>
                    public readonly SemanticModel model;

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
                        if (_name.Equals(symbol.Name)) return;
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
                        if (min == max)
                        {
                            AdHocAgent.LOG.Error("The applied value range for the '{field}' field line:{line} doesn't make sense.", this, line_in_src_code);
                            AdHocAgent.exit("", -1);
                        }

                        if (min < 0)
                            if (min < int.MinValue || int.MaxValue < max) { _exT = (int)Project.Host.Pack.Field.DataType.t_int64; }
                            else if (min < short.MinValue || short.MaxValue < max) { _exT = (int)Project.Host.Pack.Field.DataType.t_int32; }
                            else if (min < sbyte.MinValue || sbyte.MaxValue < max) { _exT = (int)Project.Host.Pack.Field.DataType.t_int16; }
                            else { _exT = (int)Project.Host.Pack.Field.DataType.t_int8; }
                        else if (max > uint.MaxValue) { _exT = (int)Project.Host.Pack.Field.DataType.t_uint64; }
                        else if (max > ushort.MaxValue) { _exT = (int)Project.Host.Pack.Field.DataType.t_uint32; }
                        else if (max > byte.MaxValue) { _exT = (int)Project.Host.Pack.Field.DataType.t_uint16; }
                        else { _exT = (int)Project.Host.Pack.Field.DataType.t_uint8; }
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
                        symbol = model.GetDeclaredSymbol(node)!;

                        check_name();
                        if (entities[symbol!.ContainingType] is PackImpl pack) pack._constants_.Add(this);
                        else AdHocAgent.exit($"`{entities[symbol!.ContainingType].full_path}` cannot contains any fields. Delete `{_name}`.");

                        raw_static_fields.Add(symbol, this);

                        init_exT(symbol.ContainingType.EnumUnderlyingType!, node.EqualsValue?.Value);
                        if (!_name.Equals(symbol.Name))
                            AdHocAgent.LOG.Warning("The name of {entity} is prohibited and changed to {new_name}. Please correct the name.", symbol, _name);
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
                        symbol = model.GetDeclaredSymbol(variable)!;
                        check_name();
                        fld_node = node;

                        raw_static_fields.Add(symbol, this);


                        entities[symbol!.ContainingType]._constants_.Add(this);
                        if (variable.Initializer == null)
                            AdHocAgent.exit($"The static field `{symbol}` is not initialized but must be. Please correct the code.");

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
                    void init_exT(ITypeSymbol Type, ExpressionSyntax? src, object? constant = null)
                    {
                        var is_array = Type is IArrayTypeSymbol; // Check if the parameter type is an array


                        // Determine the actual type to work with (element type if it's an array)
                        var actualType = Type;


                        if (src != null)
                            if (is_array)
                            {
                                actualType = ((IArrayTypeSymbol)Type).ElementType;
                                try { _array_ = (Array?)(constant ?? value_of()); }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    throw;
                                }

                                constant = null;
                            }
                            else if (constant == null)
                                constant = src.IsKind(SyntaxKind.NumericLiteralExpression) ?
                                               model.GetConstantValue(src).Value :
                                               src.IsKind(SyntaxKind.IdentifierName) ?
                                                   raw_static_fields[model.GetSymbolInfo(src).Symbol!].value_of() :
                                                   value_of(); // runtime constant value


                        switch (actualType.SpecialType)
                        {
                            case SpecialType.System_Boolean:
                                _exT = (int)Project.Host.Pack.Field.DataType.t_bool;

                                if (constant != null)
                                    _value_int = (bool)constant ?
                                                     1 :
                                                     0;
                                break;
                            case SpecialType.System_SByte:
                                _exT = (int)Project.Host.Pack.Field.DataType.t_int8;

                                if (constant != null) _value_int = Convert.ToInt64(constant);
                                break;
                            case SpecialType.System_Byte:
                                if (constant != null) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_uint8;
                                break;
                            case SpecialType.System_Int16:
                                if (constant != null) _value_int = (short?)constant;
                                _exT = (int)Project.Host.Pack.Field.DataType.t_int16;
                                break;
                            case SpecialType.System_UInt16:
                                if (constant != null) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                break;
                            case SpecialType.System_Char:
                                if (constant != null) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_char;
                                break;
                            case SpecialType.System_Int32:
                                if (constant != null) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_int32;
                                break;
                            case SpecialType.System_UInt32:
                                if (constant != null) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_uint32;
                                break;
                            case SpecialType.System_Int64:
                                if (constant != null) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_int64;
                                break;
                            case SpecialType.System_UInt64:
                                if (constant != null) _value_int = Convert.ToInt64(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                break;
                            case SpecialType.System_Single:
                                if (constant != null) _value_double = Convert.ToDouble(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_float;
                                break;
                            case SpecialType.System_Double:
                                if (constant != null) _value_double = Convert.ToDouble(constant);
                                _exT = (int)Project.Host.Pack.Field.DataType.t_double;
                                break;
                            case SpecialType.System_String:
                                if (constant != null) _value_string = constant?.ToString();
                                _exT = (int)Project.Host.Pack.Field.DataType.t_string;
                                break;
                            default:
                                if (actualType.ToString()!.Equals("org.unirail.Meta.Binary"))
                                    _exT = (int)Project.Host.Pack.Field.DataType.t_binary;
                                else //       none primitive types
                                    AdHocAgent.exit($"Constant field {_name} cannot have {actualType} type.", 56);

                                break;
                        }
                    }
                }

                /// <summary>
                /// Represents the implementation of a Field entity within a Pack.
                /// </summary>
                public class FieldImpl : HasDocs, Project.Host.Pack.Field
                {
                    /// <summary>
                    /// List of attributes applied to this field (only constant packs are stored here).
                    /// </summary>
                    public List<PackImpl> attributes = []; // only constant packs here

                    /// <summary>
                    /// Provides access to the list of attributes for serialization purposes.
                    /// Returns null if the list is empty.
                    /// </summary>
                    /// <returns>The list of attributes or null if empty.</returns>
                    public object? _attributes() => attributes.Count == 0 ?
                                                        null :
                                                        attributes;

                    /// <summary>
                    /// Gets the count of attributes applied to this field.
                    /// </summary>
                    public int _attributes_len => attributes.Count;

                    /// <summary>
                    /// Retrieves a specific attribute at the given index.
                    /// </summary>
                    /// <param name="ctx">The transmitter context (not used here).</param>
                    /// <param name="__slot">The transmitter slot (not used here).</param>
                    /// <param name="item">The index of the attribute to retrieve.</param>
                    /// <returns>The attribute at the specified index.</returns>
                    public Project.Host.Pack _attributes(Base.Transmitter ctx, Base.Transmitter.Slot __slot, int item) => attributes[item];

                    /// <summary>
                    /// Semantic model for accessing semantic information about the code.
                    /// </summary>
                    public readonly SemanticModel model;

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
                        fld_node = node;
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

                    public bool is_Header = false;
                    public bool is_HeaderModifier = false;

                    public void notHeaderField()
                    {
                        if (is_Header || is_HeaderModifier)
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
                        symbol = model.GetDeclaredSymbol(variable)!;
                        fld_node = node;

                        if (_name.Length != 0)
                        {
                            if (!_name.Equals(symbol.Name))
                            {
                                AdHocAgent.LOG.Warning("The field '{entity}' name at the {provided_path} line: {line} is prohibited.", symbol, AdHocAgent.provided_path, line_in_src_code);
                                AdHocAgent.exit(" Please correct the name.");
                            }
                        }

                        var T = model.GetTypeInfo(node.Declaration.Type).Type!;

                        if (((INamedTypeSymbol)symbol!.ContainingSymbol).TypeKind == TypeKind.Struct)
                            AdHocAgent.exit($"All fields in the constant collection " +
                                            $"{(INamedTypeSymbol)model.GetTypeInfo(node.Declaration.Type)!.Type!.ContainingSymbol}" +
                                            " must be const or static with assigned value, but the field '" +
                                            $"{symbol.Name}' is not. Correct the problem and rerun.");

                        raw_fields.Add(symbol, this);

                        switch (entities[symbol!.ContainingType])
                        {
                            case PackImpl pack:
                                pack.fields.Add(this);
                                is_Header = pack.is_Header;
                                is_HeaderModifier = pack.is_HeaderModifier;
                                break;
                            default: AdHocAgent.exit($"The entity `{entities[symbol!.ContainingType].full_path}` cannot contains any fields. Delete the field `{_name}` line:{line_in_src_code}."); break;
                        }


                        void KV_params(ITypeSymbol KV, FieldImpl dst) // Processes key-value parameters for collection types (Sets and Maps)
                        {
                            notHeaderField();

                            if (KV.NullableAnnotation == NullableAnnotation.Annotated) dst.set_null_value_bit(2); //the field's Generic type is nullable  Set<Type?> field; ( Set<Type[]?> field; )
                            switch (KV)
                            {
                                case IArrayTypeSymbol array:                                                                          //  Set<Type[,,]> field;
                                    if (array.ElementType.NullableAnnotation == NullableAnnotation.Annotated) set_null_value_bit(0); //  Set<Type?[,,]> field;
                                    dst._exT_array = (uint)(array.Rank - 1);

                                    dst.init_exT((INamedTypeSymbol)array.ElementType); // Initialize with the array's element type
                                    return;
                                case INamedTypeSymbol type:
                                    dst.init_exT(type); // Handle non-array types directly
                                    return;
                            }
                        }


                        if (T.isSet())
                        {
                            is_Set = true;

                            switch (T)
                            {
                                case IArrayTypeSymbol array:                                                                          //Set<int>[,] field;
                                    if (array.ElementType.NullableAnnotation == NullableAnnotation.Annotated) set_null_value_bit(1); //Set<int>?[,] field;
                                    _map_set_array = (uint)(array.Rank - 1);

                                    KV_params(((INamedTypeSymbol)array.ElementType).TypeArguments[0], this);

                                    break;
                                case INamedTypeSymbol type:
                                    KV_params(type.TypeArguments[0], this);
                                    break;
                            }

                            if (exT_primitive == (int)Project.Host.Pack.Field.DataType.t_bool)
                                AdHocAgent.exit($"The field `{symbol}` at the line: {line_in_src_code} is a Set of `bool` type, which is unsupported and unnecessary.");
                        }
                        else if (T.isMap())
                        {
                            V = new FieldImpl(project, node, model); //The Map Value info

                            void KV(INamedTypeSymbol type)
                            {
                                KV_params(type.TypeArguments[0], this);
                                KV_params(type.TypeArguments[1], V);
                            }

                            switch (T)
                            {
                                case IArrayTypeSymbol array:                                                                          //Map<int, int> [,] field;
                                    if (array.ElementType.NullableAnnotation == NullableAnnotation.Annotated) set_null_value_bit(1); //Map<int, int>?[,] field;
                                    _map_set_array = (uint)(array.Rank - 1);

                                    KV((INamedTypeSymbol)array.ElementType);

                                    break;
                                case INamedTypeSymbol type:
                                    KV(type);
                                    break;
                            }

                            if (exT_primitive == (int)Project.Host.Pack.Field.DataType.t_bool)
                                AdHocAgent.exit($"The field `{symbol}` at the line: {line_in_src_code} is a Map with a key of type `bool`, which is unsupported and unnecessary.");
                        }
                        else
                            switch (T)
                            {
                                case IArrayTypeSymbol array:
                                    notHeaderField();

                                    switch (array.ElementType)
                                    {
                                        case IArrayTypeSymbol array_ext:                                                              //int[][]  field;
                                            if (array_ext.NullableAnnotation == NullableAnnotation.Annotated) set_null_value_bit(1); //int[]?[] field;
                                            _map_set_array = (uint)(array.Rank - 1);

                                            _exT_array = (uint)(array_ext.Rank - 1);
                                            init_exT((INamedTypeSymbol)array_ext.ElementType);
                                            break;

                                        case INamedTypeSymbol type:
                                            if (array.ElementType.NullableAnnotation == NullableAnnotation.Annotated) set_null_value_bit(0);
                                            _exT_array = (uint)(array.Rank - 1);
                                            init_exT(type);

                                            break;
                                    }

                                    break;

                                case INamedTypeSymbol type:
                                    if (fld_node?.Declaration.Type is NullableTypeSyntax) //the field is a single nullable primitive (e.g., int? field;)
                                    {
                                        set_null_value_bit(0); // !!!
                                        init_exT(type is { SpecialType: SpecialType.None, TypeArguments.Length: > 0 } ?
                                                     (INamedTypeSymbol)type.TypeArguments[0] :
                                                     type);
                                    }
                                    else
                                        init_exT(type);

                                    if (exT_primitive is null or (int)Project.Host.Pack.Field.DataType.t_string or (int)Project.Host.Pack.Field.DataType.t_binary || nullable) notHeaderField();
                                    break;
                            }
                    }

                    /// <summary>
                    /// Switches this field to boolean type, used when an empty pack is used as a field type.
                    /// </summary>
                    public void switch_to_boolean()
                    {
                        exT_pack = null;
                        exT_primitive = (int?)Project.Host.Pack.Field.DataType.t_bool;
                        inT = (int?)Project.Host.Pack.Field.DataType.t_bool;
                        _bits = (byte)(nullable ?
                                           2 :
                                           1);
                    }

                    /// <summary>
                    /// Static class to store default maximum lengths for collection types.
                    /// </summary>
                    public class _DefaultMaxLengthOf
                    {
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
                    }

                    #region External Type Configuration (_exT)
                    /// <summary>
                    /// Initializes the external type (_exT) of the field based on the provided type symbol.
                    /// </summary>
                    /// <param name="T">The INamedTypeSymbol representing the field's type.</param>
                    /// <returns>The INamedTypeSymbol that was used for initialization (potentially unwrapped from nullable type).</returns>
                    INamedTypeSymbol init_exT(INamedTypeSymbol T)
                    {
                        if (T.NullableAnnotation == NullableAnnotation.Annotated)
                        {
                            set_null_value_bit(0);
                            T = T.TypeArguments.Length == 0 ?
                                    T.ConstructedFrom :
                                    (INamedTypeSymbol)T.TypeArguments[0];
                        }

                        switch (T.SpecialType)
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
                            case SpecialType.System_String:
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_string;
                                break;
                            default:
                                if (T.ToString()!.Equals("org.unirail.Meta.Binary"))
                                    exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_binary;
                                else //       none primitive types
                                {
                                    exT_primitive = null;
                                    if (T.IsImplicitClass)
                                        AdHocAgent.exit($"Constants set {T} cannot be referenced. But field {_name} do.", 56);
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
                        if (min == max)
                        {
                            AdHocAgent.LOG.Error("The applied value range for the '{field}' field line:{line} doesn't make sense.", this, line_in_src_code);
                            AdHocAgent.exit("", -1);
                        }

                        if (min < 0)
                            if (min < int.MinValue || int.MaxValue < max) { exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int64; }
                            else if (min < short.MinValue || short.MaxValue < max) { exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int32; }
                            else if (min < sbyte.MinValue || sbyte.MaxValue < max) { exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int16; }
                            else { exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int8; }
                        else if (max > uint.MaxValue) { exT_primitive = (int)Project.Host.Pack.Field.DataType.t_uint64; }
                        else if (max > ushort.MaxValue) { exT_primitive = (int)Project.Host.Pack.Field.DataType.t_uint32; }
                        else if (max > byte.MaxValue) { exT_primitive = (int)Project.Host.Pack.Field.DataType.t_uint16; }
                        else { exT_primitive = (int)Project.Host.Pack.Field.DataType.t_uint8; }
                    }

                    /// <summary>
                    /// Gets the PackImpl instance representing the external pack type of this field.
                    /// Returns null if the field is of a primitive type.
                    /// </summary>
                    internal PackImpl? get_exT_pack => exT_pack == null ?
                                                           null :
                                                           (PackImpl)entities[exT_pack];

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
                    /// Maximum length of the external type (e.g., for String or Binary fields).
                    /// </summary>
                    public uint? _exT_len { get; set; }

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

                    /// <summary>
                    /// Maximum value for external type based on its primitive type.
                    /// </summary>
                    public BigInteger exT_MaxValue => (Project.Host.Pack.Field.DataType)exT_primitive! switch
                    {
                        Project.Host.Pack.Field.DataType.t_bool => 1,

                        Project.Host.Pack.Field.DataType.t_int8 => sbyte.MaxValue,

                        Project.Host.Pack.Field.DataType.t_uint8 => byte.MaxValue,
                        Project.Host.Pack.Field.DataType.t_int16 => short.MaxValue,
                        Project.Host.Pack.Field.DataType.t_uint16 => ushort.MaxValue,
                        Project.Host.Pack.Field.DataType.t_char => char.MaxValue,
                        Project.Host.Pack.Field.DataType.t_int32 => int.MaxValue,
                        Project.Host.Pack.Field.DataType.t_uint32 => uint.MaxValue,
                        Project.Host.Pack.Field.DataType.t_int64 => long.MaxValue,
                        Project.Host.Pack.Field.DataType.t_uint64 => ulong.MaxValue,
                    };

                    /// <summary>
                    /// Minimum value for external type based on its primitive type.
                    /// </summary>
                    public BigInteger exT_MinValue => (Project.Host.Pack.Field.DataType)exT_primitive! switch
                    {
                        Project.Host.Pack.Field.DataType.t_bool => 1,

                        Project.Host.Pack.Field.DataType.t_int8 => sbyte.MinValue,

                        Project.Host.Pack.Field.DataType.t_uint8 => 0,
                        Project.Host.Pack.Field.DataType.t_int16 => short.MinValue,
                        Project.Host.Pack.Field.DataType.t_uint16 => 0,
                        Project.Host.Pack.Field.DataType.t_char => 0,
                        Project.Host.Pack.Field.DataType.t_int32 => int.MinValue,
                        Project.Host.Pack.Field.DataType.t_uint32 => 0,
                        Project.Host.Pack.Field.DataType.t_int64 => long.MinValue,
                        Project.Host.Pack.Field.DataType.t_uint64 => 0,
                    };
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
                        if (min == max)
                        {
                            AdHocAgent.LOG.Error("The applied value range for the '{field}' field line:{line} doesn't make sense.", this, line_in_src_code);
                            AdHocAgent.exit("", -1);
                        }

                        var range = max - min;

                        if (nullable && range < 0x80) range += 1;

                        switch ((ulong)range) //by range
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


                    public int? inT; //internal Type. if it is null then  the field type is a reference  to other packs
                    public ushort? _inT => (ushort)(inT ?? get_exT_pack!.idx);
                    #endregion

                    /// <summary>
                    /// Data direction for VarInt encoding (1 for ascending, -1 for descending, 0 for zero-based amplitude).
                    /// Null if not specified or not applicable.
                    /// </summary>
                    public sbyte _dir { get; set; } = Project.Host.Pack.Field._dir_.NULL;

                    /// <summary>
                    /// Minimum allowed value for the field (used for validation and range checks).
                    /// </summary>
                    public long? _min_value { get; set; }

                    /// <summary>
                    /// Maximum allowed value for the field (used for validation and range checks).
                    /// </summary>
                    public long? _max_value { get; set; }

                    /// <summary>
                    /// Flag to ensure MinMax attribute check is performed only once.
                    /// </summary>
                    bool _check_once;

                    /// <summary>
                    /// Checks if the MinMax attribute is used correctly and exits if it's misused with VarInt attributes.
                    /// </summary>
                    void check_MinMax_using()
                    {
                        if (!_check_once)
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
                    public byte _bits { get; set; } = Project.Host.Pack.Field._bits_.NULL;

                    /// <summary>
                    /// Null value flags for nullable fields.
                    /// Bit 0: Nullable primitive type (e.g., `int?`).
                    /// Bit 1: Nullable collection type (e.g., `Set<int>?`).
                    /// Bit 2: Nullable generic type parameter (e.g., `Set<int?>`).
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


                    public ushort? _exTV => V?._exT;
                    public uint? _exTV_len => V?._exT_len;
                    public uint? _exTV_array => V?._exT_array;

                    public ushort? _inTV => V?._inT;
                    public sbyte _dirV => V?._dir ?? Project.Host.Pack.Field._dirV_.NULL;
                    public long? _min_valueV => V?._min_value;
                    public long? _max_valueV => V?._max_value;
                    public double? _min_valueDV => V?._min_valueD;
                    public double? _max_valueDV => V?._max_valueD;

                    public byte _bitsV => V?._bits ?? Project.Host.Pack.Field._bitsV_.NULL;

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
                    public int _dims(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => dims![item]; // Potential null ref exception
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
                        foreach (var src_fld in raw_static_fields.Values.Where(fld => fld.fld_node != null))
                            foreach (var args_list in from list in src_fld.fld_node!.AttributeLists
                                                      from attr in list.Attributes
                                                      where attr.Name.ToString().Equals("ValueFor")
                                                      select attr.ArgumentList
                                                      into args_list
                                                      where args_list != null
                                                      select args_list)
                            {
                                // Transfer calculated values of static fields with the [ValueFor(dst_const_field)] attribute to their corresponding dedicated constant fields.
                                var dst_const_fld = raw_static_fields[src_fld.model.GetSymbolInfo(args_list.Arguments[0].Expression).Symbol!];
                                if (dst_const_fld.substitute_value_from != null)
                                {
                                    AdHocAgent.LOG.Error("The const field {const_field} already has a value assigned from static field {current_static}, and the static field {new_static} would override it. This redundancy is unnecessary and serves no purpose.", dst_const_fld, dst_const_fld.substitute_value_from, src_fld.symbol);
                                    AdHocAgent.exit("Fix the problem and rerun");
                                }

                                dst_const_fld.substitute_value_from = src_fld.symbol;
                                dst_const_fld._exT = src_fld._exT;
                                dst_const_fld._value_double = src_fld._value_double;
                                dst_const_fld._value_int = src_fld._value_int;
                                dst_const_fld._value_string = src_fld._value_string;
                                dst_const_fld._array_ = src_fld._array_;
                            }
                        #endregion

                        var dims = new List<int>();

                        foreach (var fld in raw_fields.Values)
                        {
                            var FLD = fld;

                            T _value_of<T>(ExpressionSyntax src) => (T)Convert.ChangeType(FLD.value_of(src)!, typeof(T));

                            BigInteger big_int_value_of(ExpressionSyntax src) => FLD.value_of(src)! switch
                            {
                                ulong value => value,
                                long value => value,
                                uint value => value,
                                int value => value,
                                ushort value => value,
                                short value => value,
                                char value => value,
                                byte value => value,
                                sbyte value => value,
                                _ => throw new InvalidOperationException("Unsupported data type")
                            };

                            foreach (var list in fld.fld_node!.AttributeLists) //process fields attributes
                            {
                                var KV = list.Target == null ? //allpy to the map/set generics
                                             ' ' :
                                             list.Target.ToString().ToUpper()[0];
                                switch (KV) //check that the generic target attributes are used correctly
                                {
                                    case 'V':
                                        if ((FLD = fld.V) == null) //attributes are for Value generic of the Map type field
                                            AdHocAgent.exit($"You have inappropriately used the 'Val:' attributes target on the '{fld}' field. The 'Val:' can only be applied to fields of Map type.", 2);
                                        break;
                                    case 'K' when !(fld.is_Set || fld.is_Map): //attributes are for Key generic of the Map/Set type field
                                        AdHocAgent.exit($"You have inappropriately used the 'Key:' attributes target on the '{fld}' field. The 'Key:' can only be applied to fields of Map or Set type.", 2);
                                        break;
                                }

                                FLD!._check_once = false;
                                foreach (var attr_ctor in list.Attributes)
                                {
                                    var name = attr_ctor.Name.ToString();
                                    var attr_args_list = attr_ctor.ArgumentList;

                                    switch (!name.EndsWith("Attribute") ?
                                                $"{name}Attribute" :
                                                name)
                                    {
                                        case "DAttribute":
                                            {
                                                FLD.notHeaderField();

                                                var attr_args = attr_args_list!.Arguments;
                                                if (attr_args.Count == 0)
                                                    AdHocAgent.exit($"The [Dims] attribute on the field {fld} has no declared dimensions, which is incorrect.", 2);

                                                foreach (var exp in attr_args.Select(arg => arg.Expression))
                                                    if (exp is PrefixUnaryExpressionSyntax _exp) //read and control of using Dims attribute args
                                                    {
                                                        var val = _value_of<uint>(_exp.Operand);

                                                        switch (_exp.ToString()[0])
                                                        {
                                                            case '-': //A const length dimension
                                                                dims.Add((int)(val << 1));
                                                                continue;
                                                            case '~': //A fixed length dimension, set at field initialization.
                                                                dims.Add((int)(val << 1 | 1));
                                                                continue;
                                                            case '+': //the type size(length) value.
                                                                if (KV == ' ' && (fld.is_Map || fld.is_Set)) fld._map_set_len = val;
                                                                else if (fld.is_String) FLD._exT_len = val;
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
                                                        if (FLD!._exT_array == null && fld._map_set_array == null) //the length of an array without a declared array detected.
                                                        {
                                                            AdHocAgent.LOG.Error("The `[D]` attribute argument `{arg}`, without prefix character, specifies the maximum length of an array. However, the field ‘{field}’ (line:{line}) does not have an array declaration such as ‘[]’, ‘[,]’, or ‘[,,]’ .", exp, FLD, fld.line_in_src_code);
                                                            AdHocAgent.exit("Please specify array type and retry", -1);
                                                        }

                                                        if (FLD._exT_array != null)                         //fully correct using argument
                                                            FLD._exT_array |= _value_of<uint>(exp) << 3;     //take the max length of the array from `exp`
                                                        else                                                 //not fully correct but ok
                                                            FLD._map_set_array |= _value_of<uint>(exp) << 3; //take the max length of the array of Map/Set/Array collection from the `exp`
                                                    }

                                                if (KV == ' ') // not Key or Val generics
                                                {
                                                    switch (dims.Count)
                                                    {
                                                        case 0: continue;
                                                        case 1:
                                                            if ((fld.is_Set || fld.is_Map) && fld._map_set_array == null)
                                                            {
                                                                AdHocAgent.LOG.Error("The `[D]` attribute argument `{arg}`, specifies the maximum length of an array of collection. However, the field ‘{field}’ on line {line}’ does not have an array declaration such as ‘[]’, ‘[,]’, or ‘[,,]’ .", attr_args[0], FLD, fld.line_in_src_code);
                                                                AdHocAgent.exit("Please specify array type and retry", -1);
                                                            }

                                                            if (fld._map_set_array != null)                     //fully correct using argument
                                                                fld._map_set_array |= (uint)(dims[0] >> 1 << 3); //take the max length of the array of Map/Set/Array collection from the dims[0]
                                                            else if (fld._map_set_array == null)                //not fully correct but ok, simple flat array
                                                            {
                                                                if (FLD!._exT_array == null)
                                                                {
                                                                    AdHocAgent.LOG.Error("The `[D]` attribute argument `{arg}`, specifies the maximum length of an array. However, the field ‘{field}’ on line {line}’ does not have an array declaration such as ‘[]’, ‘[,]’, or ‘[,,]’ .", attr_args[0], FLD, fld.line_in_src_code);
                                                                    AdHocAgent.exit("Please specify array type and retry", -1);
                                                                }

                                                                FLD._exT_array |= (uint?)(dims[0] >> 1 << 3); //take the max length of the array from dims[0]
                                                            }

                                                            dims.Clear();

                                                            continue;
                                                    }

                                                    FLD!.dims = dims.ToArray(); //Multidimensional array
                                                    fld._map_set_array = null;           //Cancel the array of Map/Set/Array collections. it cannot coexist with Multidimensional array.
                                                    dims.Clear();
                                                }
                                                else if (0 < dims.Count) // in dims cannot be apply to the Key or Val generics params
                                                {
                                                    AdHocAgent.LOG.Error("The attribute’s [D] arguments for Key or Value types cannot be multi-dimensional, meaning they cannot have a prepended - or ~ character. However, the field {field} on line {line} does not meet this requirement", FLD, fld.line_in_src_code);
                                                    AdHocAgent.exit("", -1);
                                                }
                                            }
                                            continue;

                                        case "MinMaxAttribute":
                                            {
                                                FLD.check_MinMax_using();

                                                if (FLD.exT_primitive is
                                                    (int)Project.Host.Pack.Field.DataType.t_bool or
                                                    <= (int)Project.Host.Pack.Field.DataType.t_string)
                                                {
                                                    AdHocAgent.LOG.Error("The '{attribute}' attribute cannot be applied to the '{field}' field because its type does not support this attribute.", FLD, name);
                                                    AdHocAgent.exit("", -1);
                                                }

                                                var attr_args = attr_args_list!.Arguments;
                                                if (FLD.exT_primitive is (int)Project.Host.Pack.Field.DataType.t_float or (int)Project.Host.Pack.Field.DataType.t_double)
                                                {
                                                    FLD._min_valueD = (double?)FLD.value_of(attr_args[0].Expression);
                                                    FLD._max_valueD = (double?)FLD.value_of(attr_args[1].Expression);

                                                    if (FLD._max_valueD < FLD._min_valueD) (FLD._min_valueD, FLD._max_valueD) = (FLD._max_valueD, FLD._min_valueD);

                                                    if (FLD._min_valueD < float.MinValue || float.MaxValue < FLD._max_valueD) { FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_double; }
                                                    else { FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_float; }
                                                }
                                                else
                                                    setByRange(FLD, big_int_value_of(attr_args[0].Expression), big_int_value_of(attr_args[1].Expression));
                                            }
                                            continue;
                                        case "AAttribute":
                                            FLD.notHeaderField();
                                            FLD.check_MinMax_using();
                                            FLD._dir = 1;

                                            if (attr_args_list == null ||
                                                attr_args_list.Arguments.Count == 1 &&
                                                (
                                                    attr_args_list.Arguments[0].NameColon == null ||
                                                    attr_args_list.Arguments[0].NameColon!.Name.ToString().Equals("Min_most_probable_value")
                                                //Max is omitted and determined by the applied type.
                                                )
                                              )
                                            {
                                                var most_probable_value = attr_args_list == null ?
                                                                              0 :
                                                                              big_int_value_of(attr_args_list.Arguments[0].Expression);

                                                switch (FLD.exT_primitive)
                                                {
                                                    case (int)Project.Host.Pack.Field.DataType.t_int16:
                                                        FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                                        FLD._max_value = short.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint16:
                                                    case (int)Project.Host.Pack.Field.DataType.t_char:
                                                        FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                                        FLD._max_value = ushort.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_int32:
                                                        FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint32;
                                                        FLD._max_value = int.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint32:
                                                        FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint32;
                                                        FLD._max_value = uint.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_int64:
                                                        FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                                        FLD._max_value = long.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint64:
                                                        FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                                        FLD._max_value = -1L; // ulong.MaxValue;
                                                        break;
                                                }

                                                if (attr_args_list == null) FLD._min_value = 0;
                                                else
                                                    setByRange(FLD, most_probable_value, (FLD._max_value == -1L ?
                                                                                              ulong.MaxValue :
                                                                                              (ulong)FLD._max_value!.Value) + most_probable_value);
                                            }
                                            else if (attr_args_list.Arguments.Count == 1) //one argument that is named Max: and Min_most_probable_value == 0
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
                                            FLD.notHeaderField();
                                            FLD.check_MinMax_using();
                                            FLD._dir = -1;

                                            if (attr_args_list == null ||
                                                attr_args_list.Arguments.Count == 1 &&
                                                (
                                                    attr_args_list.Arguments[0].NameColon == null ||
                                                    attr_args_list.Arguments[0].NameColon!.Name.ToString().Equals("Max_most_probable_value")
                                                //Min is omitted and determined by the applied type.
                                                )
                                              )
                                            {
                                                var most_probable_value = attr_args_list == null ?
                                                                              0 :
                                                                              big_int_value_of(attr_args_list.Arguments[0].Expression);
                                                switch (FLD.exT_primitive)
                                                {
                                                    case (int)Project.Host.Pack.Field.DataType.t_int16:
                                                        FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int16;
                                                        FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                                        FLD._min_value = short.MinValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint16:
                                                    case (int)Project.Host.Pack.Field.DataType.t_char:
                                                        FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int32;
                                                        FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                                        FLD._min_value = -ushort.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_int32:
                                                        FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int32;
                                                        FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint32;
                                                        FLD._min_value = int.MinValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint32:
                                                        FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int64;
                                                        FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                                        FLD._min_value = -uint.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_int64:
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint64:
                                                        FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int64;
                                                        FLD.inT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                                        FLD._min_value = long.MinValue;
                                                        break;
                                                }

                                                if (attr_args_list == null) FLD._max_value = 0;
                                                else
                                                    setByRange(FLD, most_probable_value + FLD._min_value!.Value, most_probable_value);
                                            }
                                            else if (attr_args_list.Arguments.Count == 1) //one argument that is named Min: and Max_most_probable_value == 0
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
                                            FLD.notHeaderField();
                                            FLD.check_MinMax_using();
                                            FLD._dir = 0;

                                            if (attr_args_list == null ||
                                                attr_args_list.Arguments.Count == 1 &&
                                                attr_args_list.Arguments[0].NameColon != null &&
                                                attr_args_list.Arguments[0].NameColon!.Name.ToString().Equals("Zero")
                                              )
                                            //Amplitude is omitted and determined by the applied type.
                                            {
                                                long min;
                                                long max;

                                                switch (FLD.exT_primitive)
                                                {
                                                    case (int)Project.Host.Pack.Field.DataType.t_int16:
                                                        if (attr_args_list == null)
                                                        {
                                                            FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_int16;
                                                            goto chk;
                                                        }

                                                        min = -short.MaxValue;
                                                        max = short.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_char:
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint16:

                                                        min = -ushort.MaxValue;
                                                        FLD._max_value = max = ushort.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_int32:
                                                        if (attr_args_list == null)
                                                        {
                                                            FLD.exT_primitive = FLD.inT = (int)Project.Host.Pack.Field.DataType.t_int32;
                                                            goto chk;
                                                        }

                                                        min = -int.MaxValue;
                                                        max = int.MaxValue;
                                                        break;
                                                    case (int)Project.Host.Pack.Field.DataType.t_uint32:
                                                        min = -uint.MaxValue;
                                                        FLD._max_value = max = uint.MaxValue;
                                                        break;
                                                    default:
                                                        if (attr_args_list == null)
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
                                                var zero = 0L;
                                                var amplitude = 0L;

                                                if (attr_args_list.Arguments[0].NameColon == null || attr_args_list.Arguments[0].NameColon!.Name.ToString().Equals("Amplitude"))
                                                {
                                                    amplitude = _value_of<long>(attr_args_list.Arguments[0].Expression);
                                                    if (1 < attr_args_list.Arguments.Count)
                                                        zero = _value_of<long>(attr_args_list.Arguments[1].Expression);
                                                }
                                                else
                                                {
                                                    zero = _value_of<long>(attr_args_list.Arguments[0].Expression);
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

                                    void check_is_varinTable(FieldImpl FLD)
                                    {
                                        if ((int)Project.Host.Pack.Field.DataType.t_float < FLD._exT && FLD._exT < (int)Project.Host.Pack.Field.DataType.t_uint8) return;
                                        AdHocAgent.exit($"The VARINT attribute cannot be assigned to the {FLD.symbol} field (line:{FLD.line_in_src_code}) if it is of non-primitive type, or if it is a float or double, or if the data range is less than 2 bytes.");
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
                        if (max < min) (max, min) = (min, max); //swap

                        fld._min_value = (long)min;
                        try { fld._max_value = (long)max; }
                        catch (Exception e) { fld._max_value = (long)(ulong)max; }

                        fld.set_exT_ByRange(min, max);
                        fld.set_inT_ByRange(min, max);
                    }
                }
            }
        }

        /// <summary>
        /// Represents the implementation of a Channel entity within a Project.
        /// Channels define communication pathways between Hosts.
        /// </summary>
        public class ChannelImpl : Entity, Project.Channel
        {
            public byte _uid => (byte)uid;

            /// <summary>
            /// Initializes a new instance of the <see cref="ChannelImpl"/> class.
            /// </summary>
            /// <param name="project">The project this channel belongs to.</param>
            /// <param name="compilation">The Roslyn compilation object.</param>
            /// <param name="Channel">The InterfaceDeclarationSyntax node representing the channel interface.</param>
            public ChannelImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax Channel) : base(project, compilation, Channel) //struct based
            {
                project.channels.Add(this);
                if (parent_by_source_code is not ProjectImpl) AdHocAgent.exit($"The definition of the channel {symbol} should be placed directly within the project’s scope.");


                var interfaces = symbol!.Interfaces;
                if (0 < interfaces.Length)
                    switch (interfaces[0].OriginalDefinition)
                    {
                        case var def when equals(def, Meta_ChannelFor):
                            if (!equals(symbol!.Interfaces[0].TypeArguments[0], symbol!.Interfaces[0].TypeArguments[1])) return;
                            AdHocAgent.LOG.Error("The channel {ch} should connect two distinct hosts.", symbol);
                            AdHocAgent.exit("Fix the problem and restart");
                            return;

                        case var def when equals(def, Meta_Modify_Channel):
                            if (symbol!.Interfaces[0].TypeArguments[0] is INamedTypeSymbol sym && sym.TypeKind == TypeKind.Interface) return; //minimal test
                            AdHocAgent.LOG.Error("The channel {channel} can Modify other channels only. But {Modify} is not", symbol, symbol!.Interfaces[0].TypeArguments[0]);
                            AdHocAgent.exit("Fix the problem and restart");

                            return;
                    }

                AdHocAgent.LOG.Error("The channel {channel} should `implements` the {ChannelFor} or {Modify}  interfaces.", symbol, "org.unirail.Meta.ChannelFor<HostA,HostB>", "org.unirail.Meta.Modify<ModifyChannel>");
                AdHocAgent.exit("Fix the problem and restart");
            }

            /// <summary>
            /// Creates a clone of this ChannelImpl instance, including its stages and branches, for modification purposes.
            /// </summary>
            /// <returns>A new ChannelImpl instance that is a clone of the current instance.</returns>
            public ChannelImpl clone() => new(project, null, (InterfaceDeclarationSyntax)node!)
            {
                origin = this,
                _name = _name,
                _doc = _doc,
                _inline_doc = _inline_doc,
                char_in_source_code = char_in_source_code,
                project = project,
                node = node,
                symbol = symbol,
                model = model,
                uid = uid,

                stages = stages.Select(st => st.clone()).ToList(),

                hostL = hostL,
                hostL_transmitting_packs = hostL_transmitting_packs.ToList(),
                hostL_related_packs = hostL_related_packs.ToList(),

                hostR = hostR,
                hostR_transmitting_packs = hostR_transmitting_packs.ToList(),
                hostR_related_packs = hostR_related_packs.ToList(),
            };

            public override bool included => _included ?? in_project.included;

            /// <summary>
            /// Host entity on the left side of the channel.
            /// </summary>
            public HostImpl? hostL;

            public byte _hostL => (byte)hostL.idx;

            /// <summary>
            /// List of packs transmitted by the left host in this channel.
            /// </summary>
            public List<HostImpl.PackImpl> hostL_transmitting_packs = [];

            /// <summary>
            /// Provides access to the list of packs transmitted by the left host for serialization purposes.
            /// Returns null if the list is empty.
            /// </summary>
            /// <returns>The list of packs transmitted by the left host or null if empty.</returns>
            public object? _hostL_transmitting_packs() => hostL_transmitting_packs.Count == 0 ?
                                                              null :
                                                              hostL_transmitting_packs;

            /// <summary>
            /// Gets the count of packs transmitted by the left host.
            /// </summary>
            public int _hostL_transmitting_packs_len => hostL_transmitting_packs.Count;

            /// <summary>
            /// Retrieves the index of a specific pack transmitted by the left host at the given index.
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <param name="item">The index of the pack to retrieve.</param>
            /// <returns>The index of the pack transmitted by the left host at the specified index.</returns>
            public ushort _hostL_transmitting_packs(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => (ushort)hostL_transmitting_packs[item].idx;

            /// <summary>
            /// List of packs related to transmission by the left host in this channel (including sub-packs and enums).
            /// </summary>
            public List<HostImpl.PackImpl> hostL_related_packs = [];

            /// <summary>
            /// Provides access to the list of packs related to transmission by the left host for serialization purposes.
            /// Returns null if the list is empty.
            /// </summary>
            /// <returns>The list of packs related to transmission by the left host or null if empty.</returns>
            public object? _hostL_related_packs() => hostL_related_packs.Count == 0 ?
                                                         null :
                                                         hostL_related_packs;

            /// <summary>
            /// Gets the count of packs related to transmission by the left host.
            /// </summary>
            public int _hostL_related_packs_len => hostL_related_packs.Count;

            /// <summary>
            /// Retrieves the index of a specific pack related to transmission by the left host at the given index.
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <param name="item">The index of the pack to retrieve.</param>
            /// <returns>The index of the pack related to transmission by the left host at the specified index.</returns>
            public ushort _hostL_related_packs(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => (ushort)hostL_related_packs[item].idx;

            /// <summary>
            /// Host entity on the right side of the channel.
            /// </summary>
            public HostImpl? hostR;

            public byte _hostR => (byte)hostR!.idx;

            /// <summary>
            /// List of packs transmitted by the right host in this channel.
            /// </summary>
            public List<HostImpl.PackImpl> hostR_transmitting_packs = [];

            /// <summary>
            /// Provides access to the list of packs transmitted by the right host for serialization purposes.
            /// Returns null if the list is empty.
            /// </summary>
            /// <returns>The list of packs transmitted by the right host or null if empty.</returns>
            public object? _hostR_transmitting_packs() => hostR_transmitting_packs.Count == 0 ?
                                                              null :
                                                              hostR_transmitting_packs;

            /// <summary>
            /// Gets the count of packs transmitted by the right host.
            /// </summary>
            public int _hostR_transmitting_packs_len => hostR_transmitting_packs.Count;

            /// <summary>
            /// Retrieves the index of a specific pack transmitted by the right host at the given index.
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <param name="item">The index of the pack to retrieve.</param>
            /// <returns>The index of the pack transmitted by the right host at the specified index.</returns>
            public ushort _hostR_transmitting_packs(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => (ushort)hostR_transmitting_packs[item].idx;

            /// <summary>
            /// List of packs related to transmission by the right host in this channel (including sub-packs and enums).
            /// </summary>
            public List<HostImpl.PackImpl> hostR_related_packs = [];

            /// <summary>
            /// Provides access to the list of packs related to transmission by the right host for serialization purposes.
            /// Returns null if the list is empty.
            /// </summary>
            /// <returns>The list of packs related to transmission by the right host or null if empty.</returns>
            public object? _hostR_related_packs() => hostR_related_packs.Count == 0 ?
                                                         null :
                                                         hostR_related_packs;

            /// <summary>
            /// Gets the count of packs related to transmission by the right host.
            /// </summary>
            public int _hostR_related_packs_len => hostR_related_packs.Count;

            /// <summary>
            /// Retrieves the index of a specific pack related to transmission by the right host at the given index.
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <param name="item">The index of the pack to retrieve.</param>
            /// <returns>The index of the pack related to transmission by the right host at the specified index.</returns>
            public ushort _hostR_related_packs(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => (ushort)hostR_related_packs[item].idx;

            /// <summary>
            /// List of stages defined within this channel. Stages represent processing steps within the channel.
            /// </summary>
            public List<StageImpl> stages = [];

            /// <summary>
            /// Provides access to the list of stages for serialization purposes.
            /// Returns null if the list is empty.
            /// </summary>
            /// <returns>The list of stages or null if empty.</returns>
            public object? _stages() => stages.Count == 0 ?
                                            null :
                                            stages;

            /// <summary>
            /// Gets the count of stages in this channel.
            /// </summary>
            public int _stages_len => stages.Count;

            /// <summary>
            /// Retrieves a specific stage at the given index.
            /// </summary>
            /// <param name="ctx">The transmitter context (not used here).</param>
            /// <param name="slot">The transmitter slot (not used here).</param>
            /// <param name="item">The index of the stage to retrieve.</param>
            /// <returns>The stage at the specified index.</returns>
            public Project.Channel.Stage _stages(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => stages[item];


            public override bool Init_As_Modifier_Dispatch_Modifications_On_Targets(HashSet<object> once)
            {
                foreach (var modified_channel in this_modify.Select(s => s.TypeArguments[0])) //normally only one modified channel
                    foreach (var stage in symbol!.GetTypeMembers())                           //stages declared in this channel body
                        foreach (var by_stage_modefied_entity in stage.Interfaces.Where(I => I.isModify()).Select(I => I.TypeArguments[0]))
                            if (!equals(modified_channel, by_stage_modefied_entity) && !equals(modified_channel, by_stage_modefied_entity.OriginalDefinition.ContainingType))
                                AdHocAgent.LOG.Warning("Stage {stage} (line: {line}) in channel {symbol}, modifying Channel {ch}, is attempting to modify entity <{modefied}> from a different channel. This is likely an error.", stage, entities[stage].line_in_src_code, symbol, modified_channel, by_stage_modefied_entity);

                foreach (var I in this_modify)
                {
                    is_Modifier = true;
                    var target_channel = (ChannelImpl)entities[I.TypeArguments[0]];

                    if (I.TypeArguments.Length == 3) // Modify<TargetChannel, HostA, HostB>
                    {
                        target_channel.hostL = (HostImpl)entities[I.TypeArguments[1]];
                        target_channel.hostR = (HostImpl)entities[I.TypeArguments[2]];
                    }

                    Init_Collect_Modification(target_channel, once);
                    return true;
                }


                foreach (var stage in symbol!.GetTypeMembers().SelectMany(s => s.Interfaces).Where(I => I.isModify()))
                {
                    //      interface ModifyChannel  {
                    //          interface ModifyStage Modify<ModifiedStage>,
                    //            _<
                    //                Server.Info,//
                    //                AuthorisationRequest
                    //            >{ }
                    //
                    //  case
                    is_Modifier = true;
                    Init_Collect_Modification(entities[stage.TypeArguments[0].OriginalDefinition.ContainingType], once);
                    return true;
                }

                var i = symbol!.Interfaces.First(I => I.isChannelFor());
                hostL = (HostImpl)entities[i.TypeArguments[0]];
                hostR = (HostImpl)entities[i.TypeArguments[1]];

                if (stages.Count == 0) AdHocAgent.exit($"Channel {symbol} does not have any stages. Please add a stage and restart.");
                return false;
            }

            /// <summary>
            /// Applies a stage modification to this channel, adding or removing a stage.
            /// </summary>
            /// <param name="stage">The stage to apply.</param>
            /// <param name="add">True to add the stage, false to remove it.</param>
            /// <param name="SwapHosts">True if the hosts in the cloned stage branches should be swapped (for LR channel modifications).</param>
            void apply(StageImpl stage, bool add, bool SwapHosts)
            {
                var i = stages.FindIndex(st => equals(st.symbol, stage.symbol));

                if (i == -1)
                {
                    if (!add) return;

                    var cloned = stage.clone();
                    if (SwapHosts)
                        (cloned.branchesL, cloned.branchesR) = (cloned.branchesR, cloned.branchesL);

                    stages.Add(cloned);
                }
                else if (!add) stages.RemoveAt(i);
            }

            public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once)
            {
                bool SwapHosts;

                if (entities.TryGetValue((SwapHosts = by_what.Name.Equals("SwapHosts")) && by_what.isMeta() ?
                                             ((INamedTypeSymbol)by_what).TypeArguments[0] :
                                             by_what, out var value))
                    switch (value)
                    {
                        case ChannelImpl channel:
                            channel.Init(once);

                            channel.stages.ForEach(stage => apply(stage, add, SwapHosts));
                            break;
                        case StageImpl stage:
                            apply(stage, add, SwapHosts);
                            break;
                    }
            }

            /// <summary>
            /// Sets the transmitting packs for this channel by analyzing stage branches and collecting packs used in each branch.
            /// </summary>
            /// <param name="once">HashSet to track visited stages and prevent infinite recursion during stage traversal.</param>
            public void set_transmitting_packs(HashSet<object> once)
            {
                once.Clear(); // Clear the set to prepare for storing visited stages

                #region Sweep stages not reachable from root stage for single-context hosts
                void scan(StageImpl src) // Recursive method to traverse and mark all reachable stages
                {
                    if (!once.Add(src)) return;                             // If the stage has already been visited, return
                    src.idx = -1;                                            // Mark the stage as reachable from root - stages[0]
                    foreach (var br in src.branchesL.Concat(src.branchesR)) // Recursively scan through all branches, following links to other stages
                        scan(br.goto_stage ?? (br.goto_stage = src));
                }

                scan(stages[0]); // Start the traversal from the root stage (stages[0])

                var single_context = hostL!._contexts! < 2 && hostR!._contexts! < 2;
                // Remove stages that were not marked as reachable (idx != -1) and reindex the remaining ones
                stages = stages.Where(st => !single_context || st.idx == -1).Select((st, idx) =>
                                                                                    {
                                                                                        st.idx = idx; // Index of this stage within the channel's stages collection. Used as goto_stage in branches
                                                                                        return st;
                                                                                    }).ToList();
                #endregion

                #region Merge branches with same goto stage
                void merge_same_goto_stage_branches(List<BranchImpl> brs)
                {
                    foreach (var g in brs.GroupBy(br => br.goto_stage!.symbol, SymbolEqualityComparer.Default).Where(g => 1 < g.Count()).ToArray())
                    {
                        var dst = g.First();
                        foreach (var br in g.Skip(1))
                        {
                            brs.Remove(br);
                            dst.packs.AddRange(br.packs);
                        }

                        dst.packs = dst.packs
                                       .GroupBy(p => p.symbol, SymbolEqualityComparer.Default)
                                       .Select(g => g.First())
                                       .ToList();
                    }
                }

                foreach (var st in stages)
                {
                    merge_same_goto_stage_branches(st.branchesL);
                    merge_same_goto_stage_branches(st.branchesR);
                }
                #endregion


                hostL._included = true;
                hostR._included = true;

                foreach (var pack in stages.SelectMany(stage => stage.branchesL.SelectMany(branch => branch.packs)).Distinct())
                {
                    hostL_transmitting_packs.Add(pack);
                    pack._included = true;
                }

                foreach (var pack in stages.SelectMany(stage => stage.branchesR.SelectMany(branch => branch.packs)).Distinct())
                {
                    hostR_transmitting_packs.Add(pack);

                    pack._included = true;
                }

                if (hostL_transmitting_packs.Count == 0 && hostR_transmitting_packs.Count == 0)
                    AdHocAgent.exit($"The channel {symbol} does not have any packs to transmit.");
            }

            /// <summary>
            /// Represents a named set of packs, grouped under an interface declaration with the '_' interface name.
            /// </summary>
            public class NamedPackSet : Entity
            {
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


                public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once)
                {
                    void apply(HostImpl.PackImpl pack, bool add)
                    {
                        if (add) packs.Add(pack);
                        else packs.Remove(pack);
                    }

                    collect_packs_in_scope(by_what, add, depth, apply);
                }

                // Method to collect packs within a given scope and apply an action to them
                // Parameters:
                // - scope: The symbol representing the scope to search for packs
                // - add: Boolean indicating whether to add or remove packs
                // - depth: The depth of the scope traversal
                // - apply: An action delegate to apply to each pack found, taking a PackImpl and the 'add' boolean
                public static void collect_packs_in_scope(ISymbol scope, bool add, uint depth, Action<HostImpl.PackImpl, bool> apply)
                {
                    // Check if the scope contains named packs in the named_packs dictionary
                    if (named_packs.TryGetValue(scope, out var nps)) //named pack set
                    {
                        foreach (var pack in nps.packs)
                            apply(pack, add);
                        return;
                    }

                    if (entities.TryGetValue(scope, out var entity))
                        switch (entity)
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


                    AdHocAgent.LOG.Error("Unexpected item {entity} in the scope of {scope}", entity, scope);
                    AdHocAgent.exit("Fix the problem and restart");
                }
            }

            /// <summary>
            /// Represents a Stage entity within a Channel. Stages define processing steps in a communication flow.
            /// </summary>
            public class StageImpl : Entity, Project.Channel.Stage
            {
                public ushort _uid => (ushort)uid;

                /// <summary>
                /// Static instance representing the Exit stage, used as a target for branches that terminate the channel flow.
                /// </summary>
                public static StageImpl? Exit;

                /// <summary>
                /// Initializes a new instance of the <see cref="StageImpl"/> class for the Exit stage.
                /// This constructor is used only for creating the static Exit stage instance.
                /// </summary>
                internal StageImpl(ProjectImpl project) : base(project, null, null) { _name = ""; }

                /// <summary>
                /// Initializes a new instance of the <see cref="StageImpl"/> class.
                /// </summary>
                /// <param name="project">The project this stage belongs to.</param>
                /// <param name="compilation">The Roslyn compilation object.</param>
                /// <param name="stage">The InterfaceDeclarationSyntax node representing the stage interface.</param>
                internal StageImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax stage) : base(project, compilation, stage)
                {
                    _name = symbol!.Name;

                    if (parent_by_source_code is not ChannelImpl)
                    {
                        AdHocAgent.LOG.Error("Stage {stage} declaration must be within a channel scope, but {parent_entity} is not a channel", symbol, symbol!.OriginalDefinition.ContainingType);
                        AdHocAgent.exit("Fix the problem and try again");
                    }

                    var ch = project.channels.Last();

                    ch.stages.Add(this);
                }


                //  on code
                //
                //      interface ModifyChannel  {
                //          interface ModifyStage Modify<ModifiedStage>,
                //            _<
                //                Server.Info//
                //               ,//
                //                AuthorisationRequest
                //            >{ }
                //
                // pass to Init Modify<ModifiedStage>
                //Gets the interfaces implemented by the parent channel that are of type Modify<Stage>.
                //Used to identify stages that modify other stages in the same channel.
                public IEnumerable<INamedTypeSymbol> by_parent_channel_modify => symbol!.OriginalDefinition.ContainingType.Interfaces.Where(I => I.isModify());

                public override bool Init_As_Modifier_Dispatch_Modifications_On_Targets(HashSet<object> once)
                {
                    if (base.Init_As_Modifier_Dispatch_Modifications_On_Targets(once)) return true; //direct modification of other stages

                    Init_Collect_Modification(this, once); //just build self

                    if (by_parent_channel_modify.Any()) //and cast to target channels
                        foreach (var what_modify in by_parent_channel_modify)
                            ((ChannelImpl)entities[what_modify.TypeArguments[0]]).apply(this, true, false);

                    return true;
                }


                protected override void Init_Collect_Modification(Entity target, HashSet<object> once)
                {
                    var target_stage = (StageImpl)target;

                    List<BranchImpl>? branches = null;
                    var LR = false;

                    foreach (var item in node!.BaseList!.Types)
                    {
                        var str = item.ToString();
                        var line = item.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        switch (str)
                        {
                            case "L":
                                branches = target_stage.branchesL;
                                continue;
                            case "R":
                                branches = target_stage.branchesR;
                                continue;
                            case "LR":
                                LR = true;
                                branches = target_stage.branchesL;
                                continue;
                            default:

                                bool scan(INamedTypeSymbol src, Func<INamedTypeSymbol, bool> todo) => todo(src) && src.TypeArguments.OfType<INamedTypeSymbol>().All(sym => scan(sym, todo));

                                if (str.StartsWith("Modify<")) continue;

                                if (str.StartsWith("_<")) //Branch start
                                {
                                    var uid_pos = item.SpanStart + 2; // "_<".length
                                    BranchImpl? branch;

                                    if (is_Modifier) // modify specific stage
                                    {
                                        #region Search - maybe modify specific branch
                                        StageImpl? update_from_goto_stage = null;
                                        StageImpl? update_to_goto_stage = null;

                                        //fast self modifier pre scan goto stages
                                        var generics = item.DescendantNodes().OfType<GenericNameSyntax>().First();
                                        foreach (var stage_sym in generics.TypeArgumentList.Arguments.Select(generic => model.GetSymbolInfo(generic).Symbol!))
                                            if (stage_sym.isMeta())
                                            {
                                                if (stage_sym.Name == "X") //maybe this is the replaceable goto stage
                                                    scan((INamedTypeSymbol)stage_sym, t => !entities.TryGetValue(t, out var entity) || entity is not StageImpl st || (update_from_goto_stage = st) == null);
                                            }
                                            else if (stage_sym.Name != "Exit")
                                                if (entities[stage_sym] is StageImpl st)
                                                {
                                                    st.Init(once);
                                                    var targget_channel = (ChannelImpl)target.parent_by_source_code!;

                                                    update_to_goto_stage = targget_channel.stages.First(s => equals(s.symbol, st.symbol));
                                                }

                                        var search_branch_by_goto_stage = update_from_goto_stage ?? update_to_goto_stage;

                                        branch = branches!.FirstOrDefault(br => br.goto_stage == search_branch_by_goto_stage);
                                        #endregion

                                        if (branch == null)
                                            branches.Add(branch = new BranchImpl(project)
                                            {
                                                uid_pos = uid_pos,
                                                _doc = string.Join(' ', item.GetLeadingTrivia().Select(t => get_doc(t)))
                                            });

                                        branch.goto_stage = update_to_goto_stage ?? update_from_goto_stage;
                                    }
                                    else
                                        branches!.Add(branch = new BranchImpl(project)
                                        {
                                            uid_pos = uid_pos,
                                            _doc = string.Join(' ', item.GetLeadingTrivia().Select(t => get_doc(t))),
                                            goto_stage = this //self referenced by default
                                        });

                                    #region Getting branch's /*UID*/ and inline comments
                                    foreach (var t in item.DescendantTrivia().Where(t => item.Span.Start < t.Span.Start))
                                        if (t.IsKind(SyntaxKind.EndOfLineTrivia)) break;
                                        else if (t.IsKind(SyntaxKind.MultiLineCommentTrivia))
                                        {
                                            var m = HasDocs._uid.Match(t.ToString());
                                            if (m.Success) branch.uid = (ushort)m.Groups[1].Value.to_base256_value();
                                            else branch._doc += get_doc(t);
                                        }
                                        else if (t.IsKind(SyntaxKind.SingleLineCommentTrivia)) branch._doc += get_doc(t);
                                    #endregion

                                    void apply(HostImpl.PackImpl _pack, bool add)
                                    {
                                        if (!add)
                                            branch.packs.RemoveAll(p => equals(p.symbol, _pack.symbol));
                                        else if (branch.packs.All(p => !equals(p.symbol, _pack.symbol)))
                                            branch.packs.Add(_pack);
                                    }

                                    modify_by_implement(item, true, (sym, sn, add, depth) =>
                                                                    {
                                                                        if (sym.isMeta())
                                                                        {
                                                                            if (sym.Name != "Exit") return;
                                                                            Exit ??= new StageImpl(projects[0])
                                                                            {
                                                                                symbol = (INamedTypeSymbol?)sym
                                                                            };

                                                                            if (!is_Modifier) branch.goto_stage = Exit;
                                                                        }
                                                                        else if (named_packs.TryGetValue(sym, out var nps))
                                                                            if (!add)
                                                                                branch.packs.RemoveAll(p => nps.packs.Any(pp => equals(p.symbol, pp.symbol)));
                                                                            else
                                                                                branch.packs.AddRange(nps.packs.Where(p => !branch.packs.Any(pp => equals(p.symbol, pp.symbol))));
                                                                        else
                                                                            switch (entities[sym])
                                                                            {
                                                                                case ProjectImpl prj:
                                                                                    prj.for_packs_in_scope(depth, pack => apply(pack, add));
                                                                                    break;
                                                                                case HostImpl host:
                                                                                    host.for_packs_in_scope(depth, pack => apply(pack, add));
                                                                                    break;
                                                                                case HostImpl.PackImpl pack:
                                                                                    pack.for_packs_in_scope(depth, _pack => apply(_pack, add));
                                                                                    return;

                                                                                case StageImpl stage:                             //branch goto target stage
                                                                                    if (!is_Modifier) branch.goto_stage = stage; //modifier do it upper, differently
                                                                                    break;
                                                                                default:

                                                                                    AdHocAgent.LOG.Error("Unexpected item {item} (like:{line}) in the {stage} declaration",
                                                                                                         sym, sn.GetLocation().GetLineSpan().StartLinePosition.Line + 1, symbol);
                                                                                    AdHocAgent.exit("Fix the problem and restart.");
                                                                                    break;
                                                                            }
                                                                    });

                                    if (LR) target_stage.branchesR.Add(branch.clone());

                                    continue;
                                }


                                AdHocAgent.LOG.Error("The stage {stage} may only have either the {L} or {R} side. The presence of the {item} is unacceptable.", symbol, "Meta.L", "Meta.R", item);
                                AdHocAgent.exit("Fix the problem and try again");
                                continue;
                        }
                    }
                }

                public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once) { }

                /// <summary>
                /// Timeout value for this stage (currently unused).
                /// </summary>
                ushort timeout = 0xFFFF;

                public ushort _timeout { get => timeout; set => timeout = value; }

                /// <summary>
                /// List of branches for the left side of this stage.
                /// </summary>
                public List<BranchImpl> branchesL = [];

                /// <summary>
                /// Provides access to the list of left branches for serialization purposes.
                /// </summary>
                /// <returns>The list of left branches.</returns>
                public object? _branchesL() => branchesL;

                /// <summary>
                /// Gets the count of left branches in this stage.
                /// </summary>
                public int _branchesL_len => branchesL.Count;

                /// <summary>
                /// Retrieves a specific left branch at the given index.
                /// </summary>
                /// <param name="ctx">The transmitter context (not used here).</param>
                /// <param name="slot">The transmitter slot (not used here).</param>
                /// <param name="item">The index of the left branch to retrieve.</param>
                /// <returns>The left branch at the specified index.</returns>
                public Project.Channel.Stage.Branch _branchesL(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => branchesL[item];

                /// <summary>
                /// List of branches for the right side of this stage.
                /// </summary>
                public List<BranchImpl> branchesR = [];

                /// <summary>
                /// Provides access to the list of right branches for serialization purposes.
                /// </summary>
                /// <returns>The list of right branches.</returns>
                public object? _branchesR() => branchesR;

                /// <summary>
                /// Gets the count of right branches in this stage.
                /// </summary>
                public int _branchesR_len => branchesR.Count;

                /// <summary>
                /// Retrieves a specific right branch at the given index.
                /// </summary>
                /// <param name="ctx">The transmitter context (not used here).</param>
                /// <param name="slot">The transmitter slot (not used here).</param>
                /// <param name="item">The index of the right branch to retrieve.</param>
                /// <returns>The right branch at the specified index.</returns>
                public Project.Channel.Stage.Branch _branchesR(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => branchesR[item];

                /// <summary>
                /// Creates a clone of this StageImpl instance, including its branches, for modification purposes.
                /// </summary>
                /// <returns>A new StageImpl instance that is a clone of the current instance.</returns>
                public StageImpl clone() => new(project)
                {
                    origin = this,
                    _doc = _doc,
                    _inline_doc = _inline_doc,
                    _name = _name,
                    symbol = symbol,
                    uid = uid,
                    timeout = timeout,
                    branchesL = branchesL.Select(br => br.clone()).ToList(),
                    branchesR = branchesR.Select(br => br.clone()).ToList(),
                };
            }

            /// <summary>
            /// Represents a Branch entity within a Stage. Branches define communication flows within a stage and specify packs to be transmitted.
            /// </summary>
            public class BranchImpl(ProjectImpl project) : Project.Channel.Stage.Branch
            {
                /// <summary>
                /// Original BranchImpl instance if this is a clone, otherwise null.
                /// </summary>
                public BranchImpl? origin;

                /// <summary>
                /// Creates a clone of this BranchImpl instance for modification purposes.
                /// </summary>
                /// <returns>A new BranchImpl instance that is a clone of the current instance.</returns>
                public BranchImpl clone() => new(project)
                {
                    origin = this,
                    _doc = _doc,
                    uid_pos = -1,
                    uid = uid < ulong.MaxValue ?
                                                           0xFFFF - uid :
                                                           ulong.MaxValue,
                    packs = packs.ToList(),
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
                /// Stage entity that this branch transitions to.
                /// </summary>
                public StageImpl? goto_stage; //if null Exit stage

                public ushort _goto_stage => goto_stage == StageImpl.Exit ?
                                                 Project.Channel.Stage.Exit :
                                                 (ushort)goto_stage!.idx;

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
                public ushort _packs(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => (ushort)packs[item].idx;
            }
        }
    }

    /// <summary>
    /// Abstract base class for entities that have documentation and names.
    /// </summary>
    public abstract class HasDocs
    {
        static readonly Regex LeadingSpaces = new(@"^\s+", RegexOptions.Multiline);
        static readonly Regex InlineCommentsCleaner = new(@"^\s*/{2,}", RegexOptions.Multiline);
        static readonly Regex BlockCommentsStart = new(@"/\*+", RegexOptions.Multiline);
        static readonly Regex block_comments_start_line = new(@"/\*+\s*(\r\n|\r|\n)", RegexOptions.Multiline);
        static readonly Regex BlockCommentsEnd = new(@"\s*\*+/", RegexOptions.Multiline);
        static readonly Regex block_comments_end_line = new(@"\s*\*+/", RegexOptions.Multiline);
        static readonly Regex CleanupAsterisk = new(@"^\s*\*+", RegexOptions.Multiline);
        static readonly Regex CleanupSeeCref = new(@"<\s*see\s*cref .*>", RegexOptions.Multiline);

        /// <summary>
        /// Regular expression to match UID comments in the source code (e.g., /*ÿ*/).
        /// </summary>
        public static readonly Regex _uid = new(@"\/\*([\u00FF-\u01FF\s]+)\*\/");


        // Pre-compiled Regex for performance. They are thread-safe.
        private static readonly Regex LeadingWhitespaceRegex = new Regex(@"^\s*", RegexOptions.Compiled);
        private static readonly Regex CommentStripperRegex = new Regex(@"^\s*(///|/\*\*|\*|\*/)\s?", RegexOptions.Compiled | RegexOptions.Multiline);

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
            if (string.IsNullOrWhiteSpace(rawComment) || rawComment.TrimStart().StartsWith("#"))
            {
                return ""; // Skip empty comments and preprocessor directives
            }

            var lines = rawComment.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Normalize indentation by removing the most common leading whitespace
            var mostCommonIndent = 0;
            var indentCounts = new Dictionary<int, int>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var indentLength = LeadingWhitespaceRegex.Match(line).Value.Replace("\t", "    ").Length;
                indentCounts.TryGetValue(indentLength, out var count);
                indentCounts[indentLength] = count + 1;
            }

            var indentRemover = new Regex($"^\\s{{0,{mostCommonIndent}}}");
            var cleanedLines = lines.Select(line => indentRemover.Replace(line, "", 1));

            var rebuiltComment = string.Join("\n", cleanedLines);
            var cleanXml = CommentStripperRegex.Replace(rebuiltComment, "").Trim();

            if (string.IsNullOrWhiteSpace(cleanXml)) { return ""; }

            // --- Part 2: Convert Clean XML to HTML ---
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml($"<root>{cleanXml}</root>");
                var htmlBuilder = new StringBuilder();

                // C# 7.0 Local Function for recursive transformation
                void TransformNode(XmlNode node)
                {
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        switch (child.NodeType)
                        {
                            case XmlNodeType.Text:
                                htmlBuilder.Append(WebUtility.HtmlEncode(child.Value));
                                break;

                            case XmlNodeType.Element:
                                var element = (XmlElement)child;
                                var tagName = element.LocalName.ToLowerInvariant();
                                string content;

                                switch (tagName)
                                {
                                    case "summary":
                                    case "remarks":
                                    case "returns":
                                        htmlBuilder.Append($"<div class=\"doc-{tagName}\"><strong>{char.ToUpper(tagName[0]) + tagName.Substring(1)}</strong>");
                                        TransformNode(element); // Recurse for nested tags
                                        htmlBuilder.Append("</div>");
                                        break;
                                    case "param":
                                    case "typeparam":
                                        content = $"<dt>{WebUtility.HtmlEncode(element.GetAttribute("name"))}</dt><dd>";
                                        htmlBuilder.Append($"<div class=\"doc-{tagName}\">{content}");
                                        TransformNode(element);
                                        htmlBuilder.Append("</dd></div>");
                                        break;
                                    case "exception":
                                        var cref = element.GetAttribute("cref");
                                        content = $"<dt><code>{WebUtility.HtmlEncode(cref.Split('.').Last())}</code></dt><dd>";
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
                                        htmlBuilder.Append($"<div class=\"doc-code-block\"><pre><code class=\"language-csharp\">{WebUtility.HtmlEncode(content)}</code></pre></div>");
                                        break;
                                    case "c":
                                        htmlBuilder.Append($"<code>{WebUtility.HtmlEncode(element.InnerText)}</code>");
                                        break;
                                    case "see":
                                        var seeCref = element.GetAttribute("cref");
                                        var seeText = string.IsNullOrWhiteSpace(element.InnerText) ?
                                                          seeCref.Split('.').Last() :
                                                          element.InnerText;
                                        htmlBuilder.Append($"<code>{WebUtility.HtmlEncode(seeText)}</code>");
                                        break;
                                    case "paramref":
                                    case "typeparamref":
                                        htmlBuilder.Append($"<em>{WebUtility.HtmlEncode(element.GetAttribute("name"))}</em>");
                                        break;
                                    // Add other tags like <list>, <item> here if needed
                                    default:
                                        TransformNode(element); // Process children of unknown tags
                                        break;
                                }

                                break;
                        }
                    }
                }

                TransformNode(xmlDoc.DocumentElement);
                return htmlBuilder.ToString();
            }
            catch (XmlException)
            {
                // Fallback for malformed XML: display as pre-formatted text
                return $"<div class=\"doc-malformed\"><pre>{WebUtility.HtmlEncode(cleanXml)}</pre></div>";
            }
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
                if (node is XText textNode) { builder.Append(textNode.Value); }
                // Case 2: The node is an XML element (a tag).
                else if (node is XElement element)
                {
                    // Use a switch to handle known tags.
                    // Unknown tags will have their content processed, but the tag itself is ignored.
                    switch (element.Name.LocalName.ToLower())
                    {
                        case "summary":
                            builder.Append("@@SUMMARY_START@@");
                            // Recurse to process all children of this tag.
                            foreach (var childNode in element.Nodes()) ProcessNode(childNode, builder);
                            builder.Append("@@SUMMARY_END@@");
                            break;

                        case "remarks":
                            builder.Append("@@REMARKS_START@@");
                            foreach (var childNode in element.Nodes()) ProcessNode(childNode, builder);
                            builder.Append("@@REMARKS_END@@");
                            break;

                        case "para":
                            // Add a space for better separation in the intermediate format.
                            builder.Append(" @@PARA_START@@ ");
                            foreach (var childNode in element.Nodes()) ProcessNode(childNode, builder);
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
                            foreach (var childNode in element.Nodes()) ProcessNode(childNode, builder);
                            break;
                    }
                }
            }

            if (string.IsNullOrEmpty(xmlContent))
                return string.Empty;

            try
            {
                // Wrap content in a single <root> to handle XML fragments.
                var xmlRoot = XElement.Parse($"<root>{xmlContent}</root>");
                var resultBuilder = new StringBuilder();

                // Start processing from the root, iterating through all its children.
                // This ensures that text outside of <summary> or <remarks> is also included.
                foreach (var node in xmlRoot.Nodes()) { ProcessNode(node, resultBuilder); }

                // Return the final string, trimming any potential whitespace from the <root> wrapper.
                return resultBuilder.ToString().Trim();
            }
            catch (System.Xml.XmlException ex)
            {
                // It's good practice to return the original text if XML parsing fails,
                // as it might be plain text without any tags.
                Console.WriteLine($"XML parsing failed: {ex.Message}. Treating content as plain text.");
                return xmlContent;
            }
        }


        /// <summary>
        /// Extracts documentation text from a SyntaxTrivia object, normalizing and cleaning up the documentation.
        /// </summary>
        /// <param name="trivia">The SyntaxTrivia object containing documentation comments.</param>
        /// <returns>The cleaned and normalized documentation string.</returns>
        public static string get_doc(SyntaxTrivia trivia)
        {
            if (AdHocAgent.is_diagramming) return ToHtml(trivia);

            var str = trivia.ToFullString();
            if (string.IsNullOrWhiteSpace(str) || str.Trim().StartsWith('#')) return ""; // Skip preprocessor instructions

            // --- Indentation Normalization ---
            var len2Count = new Dictionary<int, int>();
            var allLines = str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var line in allLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var match = LeadingSpaces.Match(line);
                if (match.Success)
                {
                    var spaces = match.Value.Replace("\t", "    ");
                    len2Count.TryGetValue(spaces.Length, out var count);
                    len2Count[spaces.Length] = count + 1;
                }
            }

            if (len2Count.Any())
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

        /// <summary>
        /// Dictionary to count leading spaces of documentation lines for normalization.
        /// </summary>
        static Dictionary<int, int> len2count = new();

        public override string ToString() => _name;
        public string _name { get; set; }
        public string? _doc { get; set; }
        public string? _inline_doc { get; set; }

        /// <summary>
        /// Index of this entity in the root project's typed collection (packs, hosts, channels, stages).
        /// Initialized to int.MaxValue and assigned during project initialization.
        /// </summary>
        public int idx = int.MaxValue; //index in the root project typed collection

        /// <summary>
        /// Brushes a given name to make it valid, capitalizing the first lowercase character if needed.
        /// Used to avoid naming conflicts with reserved keywords or prohibited names.
        /// </summary>
        /// <param name="name">The name to brush.</param>
        /// <param name="class_name">Optional class name to check for conflicts against (not currently used).</param>
        /// <returns>The brushed name, or the original name if no brushing is needed.</returns>
        public static string brush(string name, string class_name = "")
        {
            if (name != class_name && (name.Equals("_DefaultMaxLengthOf") || !is_prohibited(name))) return name;


            var new_name = name;

            for (var i = 0; i < name.Length; i++)
                if (char.IsLower(name[i]))
                {
                    new_name = new_name[..i] + char.ToUpper(new_name[i]) + new_name[(i + 1)..];
                    if (new_name == class_name || is_prohibited(new_name)) continue;

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

            if (node == null) return;

            name = name[(name.LastIndexOf('.') + 1)..];
            _name = brush(name);

            char_in_source_code = node.GetLocation().SourceSpan.Start;

            //To exclude lines like <see cref='Metrics.Login' id='14'/> from the collected documentation
            var trivias = node.GetLeadingTrivia().Where(t => t.GetStructure() is DocumentationCommentTriviaSyntax dt && dt.Content.All(xml => xml is not XmlEmptyElementSyntax));

            var doc = trivias.Aggregate("", (current, trivia) => current + get_doc(trivia));

            if (project.packs_id_info_end == -1) project.packs_id_info_end = char_in_source_code;


            if (0 < (doc = doc.Trim('\r', '\n', '\t', ' ')).Length) _doc = doc + "\n";
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
        /// <returns>True if the name is prohibited, false otherwise.</returns>
        public static bool is_prohibited(string name)
        {
            if (name[0] == '_' || name[^1] == '_')
            {
                AdHocAgent.LOG.Error("Entity names cannot start or end with an underscore _. Please correct the name '{name}' and try again.", name);
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
        public int _constants(Base.Transmitter ctx, Base.Transmitter.Slot slot, int item) => _constants_[item].idx;

        /// <summary>
        /// Reads and processes attributes applied to this entity (packs, fields, hosts, channels, stages).
        /// Populates the `attributes` list for FieldImpl or adds attribute-packs to the root project's pack list.
        /// </summary>
        /// <param name="model">The semantic model to resolve attribute symbols.</param>
        /// <param name="node">The MemberDeclarationSyntax node representing the entity with attributes.</param>
        public void read_attributes(SemanticModel model, MemberDeclarationSyntax node) //call only after all is done
        {
            if (symbol == null) return; //artificial entity
            var owner_attributes = this is ProjectImpl.HostImpl.PackImpl.FieldImpl fld ?
                                       fld.attributes : // field attributes are store into distinct storage
                                       ProjectImpl.root_project.packs;

            var attr_data = symbol is INamedTypeSymbol ?
                                ProjectImpl.pack_reflection(symbol!).GetCustomAttributesData() : //attributes of pack
                                ProjectImpl.fld_reflection(symbol!).GetCustomAttributesData();   //attributes of field

            foreach (var by_attr_name in node.AttributeLists
                                             .SelectMany(attrList => attrList.Attributes)
                                             .GroupBy(attr_ctor => model.GetSymbolInfo(attr_ctor).Symbol!.ContainingType, SymbolEqualityComparer.Default)
                                             .Where(g =>
                                                    {
                                                        var attr = g.Key!.ToString();
                                                        return !attr.StartsWith("org.unirail.Meta.") && attr != "System.FlagsAttribute";
                                                    })
                                             .GroupBy(g => g.Key?.Name))
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

                ProjectImpl.HostImpl.PackImpl attr_pack;

                if (by_attr_name.Count() == 1 &&
                    by_attr_name.First().Count() == 1 &&
                    (
                        by_attr_name?.First()?.First()?.ArgumentList == null || //attribute no arguments or
                        by_attr_name?.First()?.First()?.ArgumentList?.Arguments.Count < 2        //or attribute has less than 2 arguments
                    ))
                {
                    add_field(by_attr_name!.First().First(), _constants_, 0, true);
                    continue;
                }

                attr_pack = new ProjectImpl.HostImpl.PackImpl(project) // pack-proxy represents the attribute
                {
                    _id = (ushort)Project.Host.Pack.Field.DataType.t_constants,
                    idx = owner_attributes.Count,
                    across_idx = (ushort)owner_attributes.Count, // For field attributes, this is the only valid method.
                    _name = by_attr_name.Key![..^"Attribute".Length],
                    parent_artificial = this as Entity //attributes hierarchy!
                };

                owner_attributes.Add(attr_pack);

                if (1 < by_attr_name.Count()) // attributes with the same name but originate from different namespaces
                {
                    foreach (var attr_ctors in by_attr_name)
                    {
                        var namespace_pack = new ProjectImpl.HostImpl.PackImpl(project) //path_to_Tag
                        {
                            _id = (ushort)Project.Host.Pack.Field.DataType.t_constants,
                            idx = owner_attributes.Count,
                            across_idx = (ushort)owner_attributes.Count, // For field attributes, this is the only valid method.
                            _name = attr_ctors.Key!.ToString()!.Replace('.', '_'),
                            parent_artificial = attr_pack, //attributes hierarchy!
                        };
                        owner_attributes.Add(namespace_pack);

                        var i = 0;
                        foreach (var attr_ctor in attr_ctors)
                        {
                            var pack_i = new ProjectImpl.HostImpl.PackImpl(project) // _0
                            {
                                _id = (ushort)Project.Host.Pack.Field.DataType.t_constants,
                                idx = owner_attributes.Count,
                                across_idx = (ushort)owner_attributes.Count, // For field attributes, this is the only valid method.
                                _name = $"_{i}",
                                parent_artificial = namespace_pack, //attributes hierarchy!
                            };
                            owner_attributes.Add(pack_i);
                            add_field(attr_ctor, pack_i._constants_, i);
                            i++;
                        }
                    }

                    continue;
                }


                if (1 < by_attr_name.First().Count())
                {
                    var i = 0;

                    foreach (var attr_ctor in by_attr_name.First())
                    {
                        var pack_i = new ProjectImpl.HostImpl.PackImpl(project) // _0
                        {
                            _id = (ushort)Project.Host.Pack.Field.DataType.t_constants,
                            idx = owner_attributes.Count,
                            across_idx = (ushort)owner_attributes.Count, // For field attributes, this is the only valid method.
                            _name = $"_{i}",
                            parent_artificial = attr_pack, //attributes hierarchy!
                        };

                        owner_attributes.Add(pack_i);
                        add_field(attr_ctor, pack_i._constants_, i);
                        i++;
                    }

                    continue;
                }

                add_field(by_attr_name.First().First(), attr_pack._constants_);

                void add_field(AttributeSyntax attr_ctor, List<ProjectImpl.HostImpl.PackImpl.ConstantImpl>? dst, int same_name_index = 0, bool inline = false)
                {
                    if (attr_ctor.ArgumentList == null) return;

                    var attr_ctor_sym = (IMethodSymbol)model.GetSymbolInfo(attr_ctor).Symbol!;
                    var attr_full_name = attr_ctor_sym.ContainingType.Name;

                    var data = attr_data.Where(a => a.AttributeType.Name == attr_full_name).ElementAt(same_name_index);

                    for (int i = 0, max = attr_ctor.ArgumentList.Arguments.Count; i < max; i++)
                    {
                        var arg = attr_ctor.ArgumentList!.Arguments[i];
                        var arg_param = attr_ctor_sym.Parameters[i];

                        var fld = new ProjectImpl.HostImpl.PackImpl.ConstantImpl(project, model, arg_param.Type, arg.Expression,
                                                                                 arg.Expression is IdentifierNameSyntax const_fld ?
                                                                                     ProjectImpl.raw_static_fields[model.GetSymbolInfo(const_fld).Symbol!].value_of() : //referenced constant field value
                                                                                     data == null ?
                                                                                         model.GetConstantValue(arg.Expression).Value! :
                                                                                         data.ConstructorArguments[i].Value is ReadOnlyCollection<CustomAttributeTypedArgument> array ?
                                                                                             array.Select(e => e.Value).ToArray() :
                                                                                             data.ConstructorArguments[i].Value)
                        {
                            idx = ProjectImpl.root_project.constant_fields.Count,
                            _name = inline ?
                                                  attr_full_name[..^"Attribute".Length] :
                                                  arg_param.Name
                        };
                        dst.Add(fld);
                        ProjectImpl.root_project.constant_fields.Add(fld);
                        if (arg_param.IsParams) return;
                    }
                }
            }
        }

        /// <summary>
        /// Symbol representing this entity in the Roslyn syntax tree.
        /// </summary>
        public ISymbol? symbol;
    }

    public abstract class Entity : HasDocs
    {
        #region declaration parent-children relationship
        //  `across_idx` is for managing parent-child relationships.
        //  For instance, a pack declaration may be nested within another pack, a project, or a host. To manage this, we need a consistent ordering.
        // `across_idx` is assigned sequentially across a virtual collection of collections ordered as follows: packs, hosts, channels, and stages.

        public ushort? across_idx;

        public Entity? parent_artificial;

        public Entity? parent_by_source_code => symbol == null || symbol!.OriginalDefinition.ContainingType == null ?
                                                    null :
                                                    entities[symbol!.OriginalDefinition.ContainingType];

        public ushort _parent => ((parent_by_source_code is ProjectImpl prj ?
                                       prj.proxy :
                                       parent_artificial ?? parent_by_source_code)?.across_idx ?? Project.Host._parent_.NULL);
        #endregion

        public Entity(ProjectImpl prj, CSharpCompilation? compilation, BaseTypeDeclarationSyntax? node) : base(prj, node == null ?
                                                                                                                        "" :
                                                                                                                        node.Identifier.ToString(), node)
        {
            if (compilation == null || node == null) return;
            this.node = node;
            model = compilation.GetSemanticModel(node.SyntaxTree);
            base.symbol = symbol = model.GetDeclaredSymbol(node)!;

            if (parent_by_source_code == prj) parent_artificial = project.proxy;


            entities.Add(symbol, this);
            if (!_name.Equals(symbol.Name))
            {
                AdHocAgent.LOG.Warning("The entity '{entity}' name at the {provided_path} line: {line} is prohibited. Please correct the name manually.", symbol, AdHocAgent.provided_path, line_in_src_code);
                AdHocAgent.exit("");
            }


            foreach (var t in node.DescendantTrivia())
            {
                var tl = t.GetLocation().GetMappedLineSpan().StartLinePosition.Line + 1;
                if (line_in_src_code < tl) break;
                if (tl < line_in_src_code || !t.IsKind(SyntaxKind.MultiLineCommentTrivia)) continue;

                var m = _uid.Match(t.ToString());
                if (!m.Success) continue;
                if (m.Groups[1].Value.Contains(' '))
                {
                    var arr = m.Groups[1].Value.Split(' ').Select(str => str.to_base256_value()).ToArray();
                    var p = (ProjectImpl)this;
                    p.uid = arr[0];
                    p.imported_projects_uid = arr.Skip(1).Select(u => p.uid - u).ToArray(); // restore
                }
                else
                    uid = m.Groups[1].Value.to_base256_value();

                break;
            }
        }


        public Entity? origin;
        public static readonly Dictionary<ISymbol, Entity> entities = new(SymbolEqualityComparer.Default);

        public BaseTypeDeclarationSyntax? node;


        public ProjectImpl in_project
        {
            get
            {
                for (var e = this; ; e = e.parent_by_source_code)
                    if (e is ProjectImpl project)
                        return project;
            }
        }


        public ProjectImpl.HostImpl? in_host
        {
            get
            {
                for (var e = this; e != null; e = e.parent_by_source_code)
                    switch (e)
                    {
                        case ProjectImpl.HostImpl host: return host;
                        case ProjectImpl: return null;
                    }

                return null;
            }
        }

        public ProjectImpl.ChannelImpl? in_channel
        {
            get
            {
                for (var e = this; e != null; e = e.parent_by_source_code)
                    switch (e)
                    {
                        case ProjectImpl.ChannelImpl ch: return ch;
                        case ProjectImpl.HostImpl:
                        case ProjectImpl: return null;
                    }

                return null;
            }
        }


        public string full_path => parent_by_source_code == null || parent_by_source_code == ProjectImpl.root_project ?
                                       _name :
                                       parent_by_source_code.full_path + "." + _name;


        public INamedTypeSymbol? symbol;
        public SemanticModel model;

        public bool? _included;

        public virtual bool included => _included ?? false;


        //Identify and mark the scope of entities requiring re-initialization due to a detected cyclic dependency.
        uint _inited = int.MaxValue;

        static bool cyclic; //If a cyclic dependency is detected during initialization, re-initialization is required.
        static uint inited_seed;
        static uint fix_inited_seed;

        /// <summary>
        /// Starts a new initialization pass.
        /// </summary>
        public static void start()
        {
            cyclic = false;
            fix_inited_seed = inited_seed;
        }

        /// <summary>
        /// Checks if a re-initialization pass is required due to a detected cycle.
        /// </summary>
        /// <returns><c>true</c> if a restart is needed; otherwise, <c>false</c>.</returns>
        public static bool restart()
        {
            if (!cyclic) return false;
            inited_seed = fix_inited_seed;
            cyclic = false;
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
            if (inited || !once.Add(this) && (cyclic = true)) return; //Ensure the entity is initialized only once and prevent re-entry caused by cyclic references.

            if (symbol != null)
                if (!Init_As_Modifier_Dispatch_Modifications_On_Targets(once))
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
            foreach (var m in this_modify)
            {
                is_Modifier = true;

                Init_Collect_Modification(entities[m.TypeArguments[0]], once);
            }

            return is_Modifier;
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
            foreach (var comment in node!.GetLeadingTrivia()
                                         .Select(t => t.GetStructure())
                                         .OfType<DocumentationCommentTriviaSyntax>())
                foreach (var see in comment.DescendantNodes()
                                           .OfType<XmlCrefAttributeSyntax>())
                    target.modify(model.GetSymbolInfo(see.Cref).Symbol!, !see.Parent!.Parent!.DescendantNodes().FirstOrDefault(t => see.Span.End < t.Span.Start)!.ToString().Trim().StartsWith("-"), 0, once);

            if (node!.BaseList == null) return;

            //left -> right
            // Process modifications from implemented interfaces BaseList.
            foreach (var item in node!.BaseList!.Types)
                modify_by_implement(item, true, (sym, sn, add, depth) => target.modify(sym, add, depth, once));
        }

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

            if (type is GenericNameSyntax gns)
                switch (gns.Identifier.ValueText)
                {
                    case "_":
                    case "FieldsInjectInto":
                    case "HeaderFor":
                        foreach (var arg in gns.TypeArgumentList.Arguments)
                            modify_by_implement(arg, add, modify); //adding entity
                        return;
                    case "X":
                        foreach (var arg in gns.TypeArgumentList.Arguments)
                            modify_by_implement(arg, false, modify); //removing entity
                        return;
                    case "Modify":
                    case "ChannelFor":
                        return;
                }

            modify(model.GetTypeInfo(type).Type!, sn, add, sn.GetFirstToken().Text[0] == '@' ?
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
        /// in an inheritance list (e.g., `interface MyStage : @PackGroup`), while `0` is the default.
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
        /// to handle specific modification logic. For instance, it is used for adding/removing packs in a channel or injecting fields for <c>FieldsInjectInto</c> and <c>HeaderFor</c> packs.
        /// </remarks>
        public abstract void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once);


        public ulong uid = ulong.MaxValue; // Unique identifier for the entity, used by the visualizer in code looks like this    /*ÿ*/

        public int uid_pos //uid position in the source code
        {
            get
            {
                if (symbol == null) return -1;
                var span = symbol.Locations[0].SourceSpan;
                return span.Start + span.Length;
            }
        }

        public bool no_info = true;
    }

    /// <summary>
    /// Provides extension methods for various types used in the protocol parser.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Checks if the symbol represents a `ChannelFor<...>` meta-interface.
        /// </summary>
        /// <param name="sym">The symbol to check.</param>
        /// <returns><c>true</c> if the symbol is a `ChannelFor` interface; otherwise, <c>false</c>.</returns>
        public static bool isChannelFor(this ISymbol? sym) => sym != null && sym.ToString()!.StartsWith("org.unirail.Meta.ChannelFor<");

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
            for (var i = 0; i < str.Length; i++)
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
            var chars = new char[5];
            return new string(chars, 0, src.to_base256_chars(chars));
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
            while (0 < (src >>= 8));

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
                                                                                       if (!dst.Contains(t)) dst.Add(t);
                                                                                   });
    }
}