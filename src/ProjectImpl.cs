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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using org.unirail.Agent;
using org.unirail.Agent.AdHocProtocol.LayoutFile_;
using Project = org.unirail.Agent.AdHocProtocol.Agent_.Project;

// Microsoft.CodeAnalysis >>>> https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel?view=roslyn-dotnet-3.11.0
namespace org.unirail
{
    public class ProjectImpl : Entity, Project
    {
        public void for_packs_in_scope(uint depth, Action<HostImpl.PackImpl> dst)
        {
            foreach (var entity in entities.Values.Where(e => e.in_project == this && (0 < depth || e.in_host == null)))
                if (entity is HostImpl.PackImpl pack && pack.is_transmittable)
                    dst(pack);
        }

        public static readonly Dictionary<ISymbol, ChannelImpl.NamedPackSet> named_packs = new(SymbolEqualityComparer.Default); //Group related packets under a descriptive name


        public Dictionary<string, Type> types; //runtime reflection types

        public FieldInfo runtimeFieldInfo(ISymbol field) //runtime field info
        {
            var str = field.ToString();
            var i = str.LastIndexOf(".", StringComparison.Ordinal);
            return types[str[..i]].GetField(str[(i + 1)..], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)!;
        }

        public int packs_id_info_start = -1;
        public int packs_id_info_end = -1;
        public string file_path;
        public static readonly List<ProjectImpl> projects = []; //all projects
        public static ProjectImpl projects_root => projects[0];

        private class Protocol_Description_Parser : CSharpSyntaxWalker
        {
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

            public HasDocs? HasDocs_instance;

            public Dictionary<string, Type> types = new();
            private ProjectImpl project;

            private readonly CSharpCompilation compilation;

            public Protocol_Description_Parser(CSharpCompilation compilation) : base(SyntaxWalkerDepth.StructuredTrivia) { this.compilation = compilation; }

            private string namespace_ = "";
            private string namespace_doc = "";


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
                    HasDocs_instance = project = new ProjectImpl(projects.Count == 0 //root project
                                                                     ?
                                                                     null :
                                                                     projects[0], compilation, node, namespace_);
                    projects.Add(project);

                    project.types = types;
                    if (!string.IsNullOrEmpty(namespace_doc))
                    {
                        project._doc = namespace_doc + project._doc;
                        namespace_doc = "";
                    }
                }
                else
                {
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
                                    HasDocs_instance = new ChannelImpl(project, compilation, node); //Channel
                                    break;
                                case "L" or "R" or "LR":
                                    HasDocs_instance = new ChannelImpl.StageImpl(project, compilation, node); //host stage
                                    break;
                                case "_":
                                    named_packs.Add(symbol, new ChannelImpl.NamedPackSet(project, compilation, node)); //set of packs
                                    HasDocs_instance = null;
                                    break;
                                default:
                                    HasDocs_instance = null;
                                    break;
                            }
                        else AdHocAgent.exit($"Unknown type interface entity {symbol}");

                        break;
                    }
                }

                base.VisitInterfaceDeclaration(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(node)!;
                var interfaces = symbol.Interfaces;

                if (entities[symbol.ContainingType] is ProjectImpl && 0 < interfaces.Length)
                    switch (interfaces[0].Name)
                    {
                        case "Host":
                        case "Modify":
                            HasDocs_instance = new HostImpl(project, compilation, node); //host
                            break;

                        default:
                            AdHocAgent.exit($"Unknown struct {symbol} entity type. If it is a Host, it should extend 'org.unirail.Meta.Host'. If it is a Host Modifier, it should extend 'org.unirail.Meta.Modify'.");
                            break;
                    }
                else
                    HasDocs_instance = new HostImpl.PackImpl(project, compilation, node); //constants set

                base.VisitStructDeclaration(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax clazz)
            {
                HasDocs_instance = new HostImpl.PackImpl(project, compilation, clazz);


                base.VisitClassDeclaration(clazz);
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax ENUM)
            {
                HasDocs_instance = new HostImpl.PackImpl(project, compilation, ENUM);

                base.VisitEnumDeclaration(ENUM);
            }


            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);

                foreach (var variable in node.Declaration.Variables) { HasDocs_instance = new HostImpl.PackImpl.FieldImpl(project, node, variable, model); }

                base.VisitFieldDeclaration(node);
            }


            public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);

                HasDocs_instance = new HostImpl.PackImpl.FieldImpl(project, node, model);
                base.VisitEnumMemberDeclaration(node);
            }


            public override void VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                    if (HasDocs_instance != null && HasDocs_instance.line_in_src_code == trivia.GetLocation().GetMappedLineSpan().StartLinePosition.Line + 1)
                        HasDocs_instance._inline_doc += trivia.ToString().Trim('\r', '\n', '\t', ' ', '/');

                base.VisitTrivia(trivia);
            }


            public override void VisitXmlCrefAttribute(XmlCrefAttributeSyntax node)
            {
                var comment_line = (XmlEmptyElementSyntax)node.Parent!;

                var model = compilation.GetSemanticModel(node.Cref.SyntaxTree);
                var cref = model.GetSymbolInfo(node.Cref).Symbol;

                if (cref == null)
                    AdHocAgent.exit($"In meta information `{comment_line}` the reference to `{node.Cref.ToString()}` on `{HasDocs_instance}` is unreachable. ");


                HasDocs_instance._doc = HasDocs_instance._doc?.Replace(comment_line.Parent!.GetText().ToString(), "");


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
                    if (HasDocs_instance is HostImpl host) //language &  generate/skip implementation at current lang config
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
                            #region read host language configuration
                            host._langs |= (Project.Host.Langs)lang; //register language config
                            var txt = comment_line.Parent!.DescendantNodes().FirstOrDefault(t => node.Span.End < t.Span.Start)?.ToString().Trim() ?? "";

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
                                '+' => (byte)(host._default_impl_hash_equal | lang),  //Implementing the hash and equals methods for the pack.
                                '-' => (byte)(host._default_impl_hash_equal & ~lang), //Abstracting the hash and equals methods for the pack.
                                _ => host._default_impl_hash_equal
                            };
                            #endregion
                            goto END;
                        }

                        #region apply current language configuration (default_impl_INT) on the host entity
                        if (cref == null) AdHocAgent.exit($"`Reference to unknown entity {node.Cref} on {host} host configuration detected.");
                        switch (cref!.Kind)
                        {
                            case SymbolKind.Field: // ref to a field

                                host.field_impl.Add(cref, (Project.Host.Langs)(host._default_impl_hash_equal >> 16)); //special field lang configuration
                                break;

                            case SymbolKind.NamedType: //set  host's  enclosing pack language configuration

                                host.pack_impl.Add(cref, host._default_impl_hash_equal); //fixing impl hash equals  config on pack
                                break;

                            default:
                                AdHocAgent.exit($"Reference to unknown entity {node.Cref} on {host} host");
                                break;
                        }
                        #endregion
                        goto END;
                    } //  --------------------------- host scope lang config end


                    if (cref == null) AdHocAgent.exit($"`Reference to unknown entity {node.Parent} detected. Correct or delete it");
                }

            END:
                base.VisitXmlCrefAttribute(node);
            }
        }

        public readonly Dictionary<INamedTypeSymbol, int> pack_id_info = new(SymbolEqualityComparer.Default); //saved in source file packs ids

        //calling only on the root project
        //                                                                                                    imported_projects_pack_id_info - pack_id_info from other included projects
        public ISet<HostImpl.PackImpl> read_packs_id_info_and_write_update(IEnumerable<ProjectImpl> imported_projects)
        {
            // Packs in stages are transmittable and must have a valid `id`.
            var packs = channels
                        .Where(ch => hosts[ch._hostL].included && hosts[ch._hostR].included)
                        .SelectMany(ch => ch.stages)
                        .SelectMany(stage => stage.branchesL.Concat(stage.branchesR))
                        .SelectMany(branch => branch.packs).ToHashSet(); //pack collector collect valid transmittable packs

            // Check for the correct usage of empty packs as type.
            #region Validate empty packs
            foreach (var pack in project.all_packs.Where(pack => pack.fields.Count == 0)) // Empty packs: packs without fields.
            {
                var used = false;

                foreach (var fld in raw_fields.Values.Where(fld => fld.V != null && fld.get_exT_pack == pack))
                    AdHocAgent.exit($"The field `{fld.symbol}` at the line: {fld.line_in_src_code} is a Map with a key of empty pack {pack.symbol}, which is unsupported and unnecessary.");

                foreach (var fld in raw_fields.Values.Where(fld => fld.get_exT_pack == pack)
                                              .Concat(raw_fields.Values.Where(fld => fld.V != null && fld.V.get_exT_pack == pack).Select(fld => fld.V!)) //field value has empty packs as type
                       )                                                                                                                                 //change field type to boolean
                {
                    used = true;
                    fld.switch_to_boolean();
                }

                if (used) AdHocAgent.LOG.Warning("Pack {Pack} is empty and, as a field datatype, is therefore redundant. References to it will be replaced with a boolean.", pack.symbol);
                if (packs.Contains(pack)) continue; // If pack is transmittable


                //NOT transmittable

                if (pack._static_fields_.Count == 0) //pack is neither transmittable nor a set of constants.`
                {
                    pack._id = 0; //mark to delete
                    continue;
                }

                //NOT transmittable constants set
                pack._id = (ushort)Project.Host.Pack.Field.DataType.t_constants; //Switch to using pack as a set of constants.
                project.constants_packs.Add(pack);
            }
            #endregion

            project.all_packs.RemoveAll(pack => pack._id is 0 or (int)Project.Host.Pack.Field.DataType.t_constants);

            #region read/write packs id
            var update_packs_id_info = false; //Update packs_id_info in the source file is needed to reflect changes.

            var imported_projects_pack_id_info = imported_projects
                                                 .SelectMany(prj => prj.pack_id_info)
                                                 .ToDictionary(pair => pair.Key, pair => pair.Value, SymbolEqualityComparer.Default);

            var included_packs = packs.Where(p => p.included).ToArray();

            foreach (var pack in included_packs) //extract saved communication packs id  info
                if (!pack._name.Equals(pack.symbol!.Name))
                {
                    AdHocAgent.LOG.Error("The name of the pack {entity} (line:{line}) has been changed to {new_name}. However, the pack cannot be assigned an ID until its name is manually corrected", pack.symbol, pack.line_in_src_code, pack._name);
                    AdHocAgent.exit("", 66);
                }
                else if (pack_id_info.TryGetValue(pack.symbol!, out var id) || imported_projects_pack_id_info.TryGetValue(pack.symbol!, out id)) //root does not has id info... maybe imported projects have
                    pack._id = (ushort)id;

            if (new FileInfo(AdHocAgent.provided_path).IsReadOnly) //Protocol description file is locked - packs id updating process skipped.
                return packs;

            #region detect pack's id duplication
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


            #region renumbering
            for (var id = 0; ; id++) //set new packs id
                if (included_packs.All(pack => pack._id != id))
                {
                    var pack = packs.FirstOrDefault(pack => pack._id == (int)Project.Host.Pack.Field.DataType.t_subpack);
                    if (pack == null) break;    //no more pack without id
                    update_packs_id_info = true; //mark need to update packs_id_info in protocol description file
                    pack._id = (ushort)id;
                }
            #endregion


            var top = 0;
            var tmp = new char[4];

            void write_updated_uid(StreamWriter _file, string _source_code, List<(int, ulong)> updated_uid)
            {
                foreach (var (pos, uid) in updated_uid.OrderBy(b => b.Item1))
                {
                    _file.Write(_source_code[top..pos]);
                    _file.Write("/*");
                    var len = uid.to_base256_chars(tmp);
                    _file.Write(tmp, 0, len);
                    _file.Write("*/");
                    top = pos;
                }

                _file.Write(_source_code[top..]);

                _file.Flush();
                _file.Close();
            }


            foreach (var prj in imported_projects.Where(prj => 0 < prj.updated_uid.Count))
            {
                using StreamWriter _file = new(prj.node!.SyntaxTree.FilePath);
                top = 0;
                write_updated_uid(_file, prj.node!.SyntaxTree.ToString(), prj.updated_uid);
            }


            if (!update_packs_id_info && updated_uid.Count == 0) return packs;

            //================================= Update the current packs' ID information in the protocol description file.
            top = 0;
            var long_full_path = (HostImpl.PackImpl pack) => pack.project == project ?
                                                                 pack.full_path :
                                                                 pack.symbol!.ToString(); //namespace + project_name + pack.full_path

            var text_max_width = packs.Select(p => long_full_path(p).Length).Max() + 4;
            var source_code = node!.SyntaxTree.ToString();
            using StreamWriter file = new(AdHocAgent.provided_path);


            if (update_packs_id_info)
            {
                if (packs_id_info_start == -1) //no saved packs id info in the source file
                {
                    file.Write("/**\n");
                    file.Write(source_code[..packs_id_info_end]);
                }
                else
                    file.Write(source_code[..source_code.LastIndexOf('\n', packs_id_info_end)]); //trim last */

                //packs without saved info
                foreach (var pack in included_packs
                                     .Where(pack => !pack_id_info.TryGetValue(pack.symbol!, out var id) &&
                                                    !imported_projects_pack_id_info.TryGetValue(pack.symbol!, out id) || pack._id != id).OrderBy(pack => long_full_path(pack)))
                {
                    file.Write("\t\t<see cref = '");

                    var path = long_full_path(pack);
                    file.Write(path);
                    file.Write("'");

                    for (var i = text_max_width - path.Length; 0 < i; i--) file.Write(" ");
                    file.Write("id = '");
                    file.Write(pack._id.ToString());
                    file.Write("'/>\n");
                }

                file.Write("\t*/\n\t");
                top = packs_id_info_end;
            }

            write_updated_uid(file, source_code, updated_uid);
            #endregion
            return packs;
        }


        // Refreshes the project if any project files have changed since processing.
        public static bool refresh(DateTime on_time)
        {
            if (processing_files.All(path => new FileInfo(path).LastWriteTime < processing_time)) return on_time < processing_time; // Check if all files' last write times are older than processingTime.
            init();                                                                                                                  // If any file changed, reinitialize the project.
            return true;
        }

        // Stores the paths of processed files.
        static List<string> processing_files = [];

        static DateTime processing_time = DateTime.Now;

        public static ProjectImpl init()
        {
            processing_files.Clear();
            processing_time = DateTime.Now;
            projects.Clear();

            // Parse syntax trees from provided paths.
            var trees = new[] { AdHocAgent.provided_path }
                        .Concat(AdHocAgent.provided_paths)
                        .Select(path =>
                                {
                                    // Add file path and last write time to respective lists.
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
                    AdHocAgent.LOG.Error("The protocol description file {ProvidedPath} has an issue:\n{issue}",
                                         AdHocAgent.provided_path,
                                         string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString()).ToArray()));
                    AdHocAgent.exit("Please fix the problem and rerun");
                }

                // Reset the stream position and load the assembly from memory.
                ms.Seek(0, SeekOrigin.Begin);
                var types = Assembly.Load(ms.ToArray()).GetTypes();

                // Add parsed types to the protocol parser.
                foreach (var type in types) parser.types.Add(type.ToString().Replace("+", "."), type);
            }

            // Visit all syntax trees to parse project details.
            foreach (var tree in trees)
                parser.Visit(tree.GetRoot());

            // Ensure at least one project is detected.
            if (projects.Count == 0)
                AdHocAgent.exit($@"No project detected. Provided file {AdHocAgent.provided_path} is incomplete or in the wrong format. Try using the init template.");

            // Set the first project as the root and include it in the project structure.
            var root_project = projects[0]; // Switch to root project.
            root_project._included = true;

            root_project.source = AdHocAgent.zip(processing_files);

            foreach (var prj in projects.Where(prj => prj.uid == ulong.MaxValue)) // Assign a pseudo-random "unique" identifier (based on current time) to projects without a UID
            {
                prj.updated_uid.Add((prj.uid_pos, prj.uid = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 0x192_8A31_D95EUL)); //8 bytes Pseudo-random "unique" identifier
                Thread.Sleep(1);                                                                                                         // Ensure unique UIDs by delaying 1ms between assignments
            }

            foreach (var prj in projects.Skip(1))
            {
                new HostImpl.PackImpl(prj, prj.symbol, prj._name)
                {
                    _doc = prj._doc,
                    _inline_doc = prj._inline_doc
                }; //virtual pack for imported project
                foreach (var host in prj.hosts)
                    new HostImpl.PackImpl(prj, host.symbol, host._name + "_")
                    { //slitely change the name
                        _doc = host._doc,
                        _inline_doc = host._inline_doc
                    };
            }

            #region process project's imports
            var once = new HashSet<object>(20);
            root_project.Init(once);
            #endregion

            var typedefs = root_project.all_packs.Where(pack => pack.is_typedef).Distinct().ToArray(); //preserve
            root_project.all_packs.RemoveAll(pack => pack.is_typedef);

            //all constants-packs, enums  in the root project are included by default
            foreach (var pack in root_project.constants_packs) pack._included = true;


            #region process project Channels
            root_project.channels = root_project.channels
                                                .Where(ch => !ch.modifier && (ch._included = true).Value)
                                                .Distinct()
                                                .Select((ch, idx) =>
                                                        {
                                                            ch.idx = idx;
                                                            return ch;
                                                        })
                                                .ToList();

            if (root_project.channels.Count == 0) AdHocAgent.exit("There is no information available about communication channels.", 45);
            foreach (var ch in root_project.channels) ch.set_transmitting_packs(once);
            #endregion


            #region collect, enumerate and check hosts
            {
                root_project.hosts = root_project.hosts.Where(host => host.included).Distinct().OrderBy(host => host.full_path).ToList();
                for (var idx = 0; idx < root_project.hosts.Count; idx++) root_project.hosts[idx].idx = idx;
                var exit = false;
                foreach (var host in root_project.hosts.Where(host => host._langs == 0))
                {
                    exit = true;
                    AdHocAgent.LOG.Error("The host {host} lacks language implementation information. Please use the C# `///<see cref=\"InLANG\"/>` XML comment on the host to add it.", host.symbol);
                }

                if (exit) AdHocAgent.exit("Correct detected problems and restart", 45);


                if (exit) AdHocAgent.exit("Fix the problem and retry.");
                // Remove from host's scopes: packs registered on project scope
                foreach (var host in root_project.hosts)
                    host.packs.RemoveAll(pack => root_project.constants_packs.Contains(pack));
            }
            #endregion


            HostImpl.PackImpl.FieldImpl.init(root_project); //process all fields

            //after typedef fields pocessed
            #region process typedefs
            {
                var flds = raw_fields.Values;

                for (var rerun = true; rerun;)
                {
                    rerun = false;

                    foreach (var T in typedefs)
                    {
                        var src = T.fields[0];
                        raw_fields.Remove(src.symbol!); //remove typedef field

                        foreach (var dst in flds)
                        {
                            void copy_type(HostImpl.PackImpl.FieldImpl src, HostImpl.PackImpl.FieldImpl dst)
                            {
                                rerun = true;
                                dst.exT_pack = src.exT_pack;
                                dst.exT_primitive = src.exT_primitive;
                                dst.inT = src.inT;

                                dst._dir ??= src._dir;

                                if (
                                    (dst._map_set_len != null && src._map_set_len != null) ||
                                    (dst._map_set_array != null && src._map_set_array != null) ||
                                    (dst._exT_len != null && src._exT_len != null) ||
                                    (dst._exT_array != null && src._exT_array != null)
                                )
                                {
                                    AdHocAgent.LOG.Error("Typedef {typedef} may generate invalid type nesting or clashes when embedded in {field}.", T, dst);
                                    AdHocAgent.exit("Please fix the problem and rerun");
                                }

                                if (dst._name == "") //dst is Value of Map
                                    if (src._map_set_len != null ||
                                        src._map_set_array != null || src.dims != null)
                                    {
                                        AdHocAgent.LOG.Error("Typedef {typedef} may generate invalid type nesting or clashes when embedded in Value type of {field}.", T, dst);
                                        AdHocAgent.exit("Please fix the problem and rerun");
                                    }


                                if (src.dims != null)
                                    if (dst.dims == null) dst.dims = src.dims;
                                    else dst.dims = dst.dims.Concat(src.dims).ToArray();

                                if (src._map_set_len != null) dst._map_set_len = src._map_set_len;
                                if (src._map_set_array != null) dst._map_set_array = src._map_set_array;

                                if (src._exT_len != null) dst._exT_len = src._exT_len;
                                if (src._exT_array != null) dst._exT_array = src._exT_array;

                                if (src.is_Map) dst.V = src.V;

                                dst._min_value = src._min_value;
                                dst._max_value = src._max_value;

                                dst._min_valueD = src._min_valueD;
                                dst._max_valueD = src._max_valueD;

                                dst._bits = src._bits;
                                dst._value_bytes = src._value_bytes;
                                if (src._null_value.HasValue)
                                    dst._null_value = dst._null_value == null ?
                                                          src._null_value :
                                                          (byte)(dst._null_value.Value | src._null_value.Value);
                            }

                            if (SymbolEqualityComparer.Default.Equals(T.symbol, dst.exT_pack)) copy_type(src, dst);
                            if (dst.V != null && SymbolEqualityComparer.Default.Equals(T.symbol, dst.V.exT_pack)) copy_type(src, dst.V);
                        }
                    }
                }
            }
            #endregion


            HostImpl.PackImpl.init(root_project);
            HostImpl.init(root_project);


            var packs = root_project.read_packs_id_info_and_write_update(projects.Skip(1));
            packs.UnionWith(root_project.all_packs.Where(p => p.included));       //add included transmittable to the packs
            packs.UnionWith(root_project.constants_packs.Where(c => c.included)); //add included enums & constants sets to the packs


            //include packs that are not transmitted but build a namespace hierarchy
            foreach (var p in packs.ToArray())
                for (var pack = p; pack.parent_entity is HostImpl.PackImpl parent_pack; pack = parent_pack)
                    if (packs.Add(parent_pack))
                    {
                        parent_pack._id = (ushort)Project.Host.Pack.Field.DataType.t_constants; //make them totaly empty shell
                        parent_pack.fields.Clear();
                        parent_pack._static_fields_.Clear();
                    }


            foreach (var pack in packs.ToArray())
                for (var p = pack; ;)
                    if (p.parent_entity is HostImpl.PackImpl parent_pack) //run up to hierarchy
                    {
                        parent_pack._included = true; //container packs

                        if (!packs.Contains(parent_pack))
                        {
                            pack._id = (ushort)Project.Host.Pack.Field.DataType.t_constants; //make true shell_pack
                            pack.fields.Clear();                                             // true shell_pack contains only constants and exists only to support namespace hierarchical relationships only
                            packs.Add(pack);
                        }

                        p = parent_pack;
                    }
                    else break;

            foreach (var host in root_project.hosts)
                packs.UnionWith(host.pack_impl.Keys.Where(symbol1 => entities[symbol1] is HostImpl.PackImpl).Select(symbol2 =>
                                                                                                                    {
                                                                                                                        var pack = (HostImpl.PackImpl)entities[symbol2];
                                                                                                                        pack._included = true;
                                                                                                                        return pack;
                                                                                                                    }));

            root_project.packs = packs.OrderBy(pack => pack.full_path).ToList(); //save all used packs


            #region Detect redundant pack's language information.
            root_project.hosts.ForEach(
                                       host =>
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


            #region set packs idx (storage place index)  and collect all fields
            HashSet<HostImpl.PackImpl.FieldImpl> fields = [];
            for (var idx = 0; idx < root_project.packs.Count; idx++)
            {
                var pack = root_project.packs[idx];
                pack.idx = idx; //set pack's idx

                foreach (var fld in pack.fields)
                    fields.Add(fld);

                foreach (var fld in pack._static_fields_)
                    fields.Add(fld);
            }
            #endregion


            // Windows file system treats file and directory names as case-insensitive. FOO.txt and foo.txt are treated as equivalent.
            // Linux file system treats file and directory names as case-sensitive. FOO.txt and foo.txt are treated as distinct files.
            //
            // This creates a problem in Java, where case-sensitive class names can cause issues when compiled on a case-insensitive file system like Windows.
            // The best workaround is to detect and prevent this situation.

            var problem = false;

            foreach (var par_child in root_project.packs.GroupBy(pack => pack.parent_entity))
            {
                var parent = (par_child.Key as HostImpl.PackImpl)!;

                foreach (var gr in par_child.GroupBy(item => item._name.ToLower()).Where(gr => 1 < gr.Count()))
                {
                    var nested_types = string.Join(",", gr.Select(x => x._name));
                    if (!problem)
                        AdHocAgent.LOG.Error(@"The Windows file system treats file and directory names as case-insensitive, so FOO.txt and foo.txt are treated as equivalent files.
The Linux file system treats file and directory names as case-sensitive, so FOO.txt and foo.txt are treated as distinct files.
This creates a problem in Java, where case-sensitive class names can cause issues when compiled on a case-insensitive file system like Windows.
The best workaround is to detect and prevent this situation.
Please rename duplicate nested types.");

                    AdHocAgent.LOG.Error(@"in the pack {pack} : {nested_types} ",
                                         parent == null ?
                                             root_project.symbol :
                                             parent.symbol,
                                         0 < nested_types.Length ?
                                             nested_types :
                                             "");
                    problem = true;
                }
            }

            if (problem) AdHocAgent.exit("Fix the problem and try again.", 22);


            var packs_with_parent = root_project.packs.Where(pack => pack._parent != null).ToArray();

            bool repeat;
            do
            {
                repeat = false;
                foreach (var pack in packs_with_parent)
                    if (pack._name.Equals(pack.parent_entity!._name))
                    {
                        var new_name = mangling(pack._name);
                        AdHocAgent.LOG.Warning("Pack `{Pack}` is declared inside body of the parent pack {ParentPack} and has the same as parent name . Some languages (Java) not allowed this.\n The name will be changed to `{NewName}`",
                                               pack.symbol, pack.parent_entity._name, new_name);
                        pack._name = new_name;
                        repeat = true; //name changed, this may bring new conflict, repeat check
                    }
            }
            while (repeat);


            //for more predictable stable order
            string orderBy(HostImpl.PackImpl.FieldImpl fld) => root_project.packs.First(pack => pack.fields.Contains(fld) || pack._static_fields_.Contains(fld)).full_path + fld._name;

            root_project.fields = fields.OrderBy(fld => orderBy(fld)).ToArray();

            for (var idx = 0; idx < root_project.fields.Length; idx++) root_project.fields[idx].idx = idx; //set fields  idx


            #region update referred
            foreach (var pack in root_project.packs)
                pack._referred = pack.is_transmittable && root_project.fields.Any(fld => fld.get_exT_pack == pack || (fld.V != null && fld.V.get_exT_pack == pack));
            #endregion

            UID_Impl.read_write_uid();
            return root_project;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////
        public static string mangling(string name)
        {
            for (var i = 0; i < name.Length; i++)
                if (char.IsLower(name[i])) { return name[..i] + char.ToUpper(name[i]) + name[(i + 1)..]; }

            return name;
        }

        public override void Init(HashSet<object> once)
        {
            foreach (var I in symbol!.Interfaces)
                if (entities.TryGetValue(I, out var value) && value is ProjectImpl prj)
                {
                    prj.Init(once);


                    void check_duplicate(string prefix, IEnumerable<Entity> set)
                    {
                        var error = set.GroupBy(c => c._name).Where(g => 1 < g.Count()).Aggregate("", (current, i) => current + (string.Join("\n", i.Select(e => e.symbol)) + "\n"));

                        if (error != "")
                            AdHocAgent.exit($"The following {prefix} have duplicate names: \n{error}This is unacceptable. Please resolve the issue.");
                    }


                    hosts.AddRange(prj.hosts);
                    check_duplicate("hosts", hosts);

                    channels.AddRange(prj.channels.Where(ch => !ch.modifier));
                    check_duplicate("channels", channels);

                    constants_packs.AddNew(prj.constants_packs);

                    all_packs.AddNew(prj.all_packs);
                }

            foreach (var host in hosts) //process host modification
            {
                var modify_host = host.modify_host;
                if (modify_host == null) continue;
                modify_host._langs |= host._langs;
                foreach (var (K, V) in host.field_impl)
                    if (!modify_host.field_impl.TryAdd(K, V))
                        modify_host.field_impl[K] = V;

                foreach (var (K, V) in host.pack_impl)
                    if (!modify_host.pack_impl.TryAdd(K, V))
                        modify_host.pack_impl[K] = V;
            }

            hosts.RemoveAll(host => host.modify_host != null); //remove all host modifier

            once.Clear();
            start();
            foreach (var pack in named_packs.Values) pack.Init(once);
            if (restart())
                foreach (var pack in named_packs.Values)
                    pack.Init(once);

            once.Clear();
            start();
            foreach (var ch in channels) ch.Init(once);
            if (restart())
                foreach (var ch in channels)
                    ch.Init(once);


            once.Clear(); // Stages are processed only once, as there are no cycle-referencing dependencies.
            start();
            foreach (var st in channels.SelectMany(ch => ch.stages).ToArray())
                st.Init(once);

            once.Clear();
            start();
            foreach (var pack in all_packs)
            {
                pack.Init(once);
                pack._nested_max = (ushort)pack.calculate_fields_type_depth(once);
            }

            if (restart())
                foreach (var pack in all_packs)
                    pack.Init(once);


            List<Entity> unassigned = [];
            HashSet<ulong> assigned = [];

            void set_persistent_uid(IEnumerable<Entity> entities) // Our goal is to minimize the UID, to reduce the footprint in the source code
            {
                var uid = 0U;
                foreach (var pks in entities.Where(e => e.uid < ulong.MaxValue).GroupBy(e => e.uid)
                                            .Where(g => g.Count() > 1))
                {
                    var list = pks.Aggregate("", (current, pk) => current + pk.full_path + "  line:" + pk.line_in_src_code + "     ");
                    AdHocAgent.LOG.Warning("Duplicate entities detected: {List} with the same UID = {Id}. This may have been accidentally copied. Please delete the duplicate assignment.", list, "/*" + pks.Key.to_base256_chars() + "*/");
                    AdHocAgent.exit("", 66);
                }

                foreach (var entity in entities.Where(e => e.origin == null)) // Exclude clones and include only items belonging to this project
                    if (entity.uid == ulong.MaxValue) unassigned.Add(entity);
                    else assigned.Add(entity.uid);

                foreach (var entity in unassigned)
                {
                    while (assigned.Contains(uid)) uid++;
                    updated_uid.Add((entity.uid_pos, uid));
                    entity.uid = (ushort)uid++;
                }

                assigned.Clear();
                unassigned.Clear();
            }

            set_persistent_uid(hosts);
            set_persistent_uid(channels);

            foreach (var p in projects_root.constants_packs.Where(p => p.is_virtual_for_project_or_host))
                p.uid = 0xFFFF - entities[p.symbol!].uid; //apply virtual pack UID from the real entity it represents project/host.
                                                          //      0xFFFF - UID  : because the host is converting to `a packet`. This may result in a UID conflict with other real packets.

            set_persistent_uid(all_packs.Concat(constants_packs).Where(p => !p.is_typedef));

            List<ChannelImpl.BranchImpl> unassigned_ = [];

            foreach (var stages in channels.Select(channel => channel.stages))
            {
                set_persistent_uid(stages);

                foreach (var stage in stages)
                {
                    foreach (var branch in stage.branchesL.Concat(stage.branchesR).Where(br => br.origin == null))
                        if (branch.uid == ushort.MaxValue) unassigned_.Add(branch);
                        else assigned.Add(branch.uid);

                    var uid = 0UL;
                    foreach (var branch in unassigned_)
                    {
                        while (assigned.Contains(uid)) uid++;
                        updated_uid.Add((branch.uid_pos, uid));
                        branch.uid = (ushort)uid++;
                    }

                    assigned.Clear();
                    unassigned_.Clear();
                }
            }
        }

        //    pos, in the source file
        //     ↓     uid
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

        // In the root project, `root_project` is null, but the 'project' field references itself.
        public ProjectImpl(ProjectImpl? root_project, CSharpCompilation compilation, InterfaceDeclarationSyntax node, string namespace_) : base(null, compilation, node)
        {
            file_path = node.SyntaxTree.FilePath;
            if (root_project != null) //not root project
                project = root_project;

            _namespacE = namespace_;
            this.node = node;
        }

        public string? _task => AdHocAgent.task;

        public string? _namespacE { get; set; }

        public long _time { get; set; }

        public byte[] source = [];
        public object? _source() => source;
        public int _source_len => source.Length;
        public byte _source(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => source[item];
        public List<HostImpl> hosts = new(3);

        public int _hosts_len => hosts.Count;
        public object? _hosts() => hosts;
        public Project.Host _hosts(Context.Transmitter ctx, Context.Transmitter.Slot slot, int d) => hosts[d];

        //All fields, including the virtual `V` field, are used as the value of a `Map` data type.
        static IEnumerable<HostImpl.PackImpl.FieldImpl?> all_fields() => raw_fields.Values.Concat(raw_fields.Values.Select(fld => fld.V).Where(fld => fld != null)).Distinct();

        public static Dictionary<ISymbol, HostImpl.PackImpl.FieldImpl> raw_fields = new(SymbolEqualityComparer.Default);


        public HostImpl.PackImpl.FieldImpl[] fields = [];

        public object? _fields() => 0 < fields.Length ?
                                        fields :
                                        null;

        public int _fields_len => fields.Length;
        public Project.Host.Pack.Field _fields(Context.Transmitter ctx, Context.Transmitter.Slot slot, int d) => fields[d];


        public readonly List<HostImpl.PackImpl> all_packs = []; //eventually only transmittable packs
        public readonly List<HostImpl.PackImpl> constants_packs = []; //enums + constant sets


        public List<HostImpl.PackImpl> packs;

        public object? _packs() => 0 < packs.Count ?
                                       packs :
                                       null;

        public int _packs_len => packs.Count;
        public Project.Host.Pack _packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int d) => packs[d];

        public List<ChannelImpl> channels = [];

        public int _channels_len => channels.Count;

        public object? _channels() => channels.Count < 1 ?
                                          null :
                                          channels;

        public Project.Channel _channels(Context.Transmitter ctx, Context.Transmitter.Slot slot, int d) => channels[d];


        public class HostImpl : Entity, Project.Host
        {
            public void for_packs_in_scope(uint depth, Action<PackImpl> dst)
            {
                foreach (var entity in entities.Values.Where(e => e.in_host == this && (0 < depth || e.parent_entity == this)))
                    if (entity is PackImpl pack && pack.is_transmittable)
                        dst(pack);
            }

            public byte _uid => (byte)uid;
            public Project.Host.Langs _langs { get; set; }

            public override bool included => _included ?? in_project.included;

            public HostImpl(ProjectImpl project, CSharpCompilation compilation, StructDeclarationSyntax host) : base(project, compilation, host)
            {
                _default_impl_hash_equal = 0xFFFF_FFFF; // Default: Automatically generate hash code and equals methods implementation. One bit per language.
                project.hosts.Add(this);
            }

            public HostImpl? modify_host => symbol!.Interfaces[0].Name == "Modify" && 0 < symbol!.Interfaces[0].TypeArguments.Length ?
                                                (HostImpl)entities[symbol!.Interfaces[0].TypeArguments[0]] :
                                                null;

            #region pack_impl_hash_equal
            public readonly Dictionary<ISymbol, uint> pack_impl = new(SymbolEqualityComparer.Default); //pack -> impl information
            private Dictionary<ISymbol, uint>.Enumerator pack_impl_enum;

            public object? _pack_impl_hash_equal() => pack_impl.Count == 0 ?
                                                          null :
                                                          pack_impl;

            public int _pack_impl_hash_equal_len => pack_impl.Count;
            public void _pack_impl_hash_equal_Init(Context.Transmitter ctx, Context.Transmitter.Slot slot) => pack_impl_enum = pack_impl.GetEnumerator();

            public ushort _pack_impl_hash_equal_NextItem_Key(Context.Transmitter ctx, Context.Transmitter.Slot slot)
            {
                pack_impl_enum.MoveNext();
                return (ushort)entities[pack_impl_enum.Current.Key].idx;
            }

            public uint _pack_impl_hash_equal_Val(Context.Transmitter ctx, Context.Transmitter.Slot slot) => pack_impl_enum.Current.Value;
            public uint _default_impl_hash_equal { get; set; } //by default a bit per language
            #endregion


            #region field_impl
            public readonly Dictionary<ISymbol, Project.Host.Langs> field_impl = new(SymbolEqualityComparer.Default);
            private Dictionary<ISymbol, Project.Host.Langs>.Enumerator field_impl_enum;


            public object? _field_impl() => field_impl.Count == 0 ?
                                                null :
                                                field_impl;

            public int _field_impl_len => field_impl.Count;
            public void _field_impl_Init(Context.Transmitter ctx, Context.Transmitter.Slot slot) => field_impl_enum = field_impl.GetEnumerator();

            public ushort _field_impl_NextItem_Key(Context.Transmitter ctx, Context.Transmitter.Slot slot)
            {
                field_impl_enum.MoveNext();
                return (ushort)raw_fields[field_impl_enum.Current.Key].idx;
            }

            public Project.Host.Langs _field_impl_Val(Context.Transmitter ctx, Context.Transmitter.Slot slot) => field_impl_enum.Current.Value;
            #endregion

            public List<PackImpl> packs = []; // Host-dedicated constants packs

            public object? _packs() => 0 < packs.Count ?
                                           packs :
                                           null;

            public int _packs_len => packs.Count;
            public ushort _packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => (ushort)packs[item].idx;

            public override bool Init_As_Modifier_Dispatch_Modifications_On_Targets(HashSet<object> once) => false;
            protected override void Init_Collect_Modification(Entity target, HashSet<object> once) { }
            public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once) { }

            public static void init(ProjectImpl project) //host
            {
                HashSet<PackImpl> packs = [];

                foreach (var ch in project.channels)
                {
                    //validate hosts.
                    //if an port._packs.Count == 0 it is OK -> port can receive packs only,
                    //but if booth, connected with Channel ports are empty, this is not OK
                    if (ch.hostL_transmitting_packs.Count == 0 && ch.hostR_transmitting_packs.Count == 0)
                        AdHocAgent.exit($"The channel {ch.symbol} does not have any packs to transmit.");

                    packs.Clear();
                    foreach (var pack in ch.hostL_transmitting_packs) pack.related_packs(packs);
                    foreach (var pack in ch.hostL_transmitting_packs) packs.Remove(pack); //subpacks purification
                    ch.hostL_related_packs.AddRange(packs);
                    ch.hostL_related_packs.ForEach(pack => pack._included = true); //mark

                    packs.Clear();
                    foreach (var pack in ch.hostR_transmitting_packs) pack.related_packs(packs);
                    foreach (var pack in ch.hostR_transmitting_packs) packs.Remove(pack); //subpacks purification
                    ch.hostR_related_packs.AddRange(packs);
                    ch.hostR_related_packs.ForEach(pack => pack._included = true); //mark
                }
            }


            public class PackImpl : Entity, Project.Host.Pack
            {
                public void for_packs_in_scope(uint depth, Action<PackImpl> dst, ISymbol pack_set)
                {
                    if (depth == 0)
                        if (!is_transmittable)
                            AdHocAgent.LOG.Warning("Adding a non-transmittable pack {pack} to the set of packs {this} has been detected and will be ignored.", symbol, pack_set);
                        else
                            dst(this);
                    else
                        foreach (var entity in entities.Values.Where(e => e.parent_entity == this))
                            if (entity is PackImpl pack && pack.is_transmittable)
                                dst(pack);
                }

                public ushort _uid => (ushort)uid;
                public ushort _id { get; set; } //pack id


                public ushort? _nested_max { get; set; }
                public bool _referred { get; set; }

                public List<FieldImpl> fields = [];

                public object? _fields() => 0 < fields.Count ?
                                                fields :
                                                null;

                public int _fields_len => fields.Count;
                public int _fields(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => fields[item].idx;

                public List<FieldImpl> _static_fields_ = [];

                public object? _static_fields() => 0 < _static_fields_.Count ?
                                                       _static_fields_ :
                                                       null;

                public int _static_fields_len => _static_fields_.Count;
                public int _static_fields(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => _static_fields_[item].idx;


                //the pack is included if it is explicitly included or if it is isEnum or Constants_set it's project included

                // ============================

                private EnumDeclarationSyntax? enum_node;
                public bool is_calculate_enum_type => enum_node!.BaseList == null; //user does not explicitly assign enum type (int by default)


                public bool is_enum => enum_node != null;
                public bool is_constants_set => _id == (int)Project.Host.Pack.Field.DataType.t_constants;
                public bool is_typedef => fields is [{ _name: "TYPEDEF" }];
                public bool is_transmittable => !is_enum && !is_constants_set && !is_typedef;


                #region enum
                public PackImpl(ProjectImpl project, CSharpCompilation compilation, EnumDeclarationSyntax ENUM) : base(project, compilation, ENUM)
                {
                    enum_node = ENUM;

                    _id = (ushort)(symbol.GetAttributes().Any(a => a.AttributeClass!.ToString()!.Equals("System.FlagsAttribute")) //enum type
                                       ?
                                       Project.Host.Pack.Field.DataType.t_flags :
                                       Project.Host.Pack.Field.DataType.t_enum_sw); //probably need to check

                    project.constants_packs.Add(this); //enums register
                    in_host?.packs.Add(this);          //register enum on host level scope. else stays in the project scope
                }
                #endregion

                #region struct based constants set
                public PackImpl(ProjectImpl project, CSharpCompilation compilation, StructDeclarationSyntax constants_set) : base(project, compilation, constants_set)
                {
                    _id = (ushort)Project.Host.Pack.Field.DataType.t_constants; //constants set
                    project.constants_packs.Add(this);                          // register constants set
                    in_host?.packs.Add(this);                                   //register on host level scope. else stays in the project scope
                }
                #endregion


                #region class based pack
                public PackImpl(ProjectImpl project, CSharpCompilation compilation, ClassDeclarationSyntax pack) : base(project, compilation, pack)
                {
                    _id = (int)Project.Host.Pack.Field.DataType.t_subpack; //by default subpack type

                    project.all_packs.Add(this);
                }
                #endregion

                public bool is_virtual_for_project_or_host => origin == this;                                                    //Virtual pack for imported projects or hosts
                public bool is_virtual_for_host => is_virtual_for_project_or_host && !equals(project.symbol, symbol); // Virtual pack for host

                public PackImpl(ProjectImpl project, INamedTypeSymbol? symbol, string name) : base(project, null) // Virtual pack for imported projects and hosts to represent hierarchy
                {
                    _name = name;   //for name in path only
                    this.symbol = symbol; // will have project's or host's symbol

                    _id = (int)Project.Host.Pack.Field.DataType.t_constants; // Container pack
                    _included = true;
                    origin = this;                              //mark it is virtual
                    projects_root.constants_packs.Insert(0, this); // register in the root project at start and not register in the Entity.entities
                }

                internal void related_packs(ISet<PackImpl> dst) //collect subpacks into dst ALSO incude ENUMS
                {
                    foreach (var pack in fields.Select(fld => fld.get_exT_pack).Concat(fields.Select(fld => fld.V?.get_exT_pack)).Where(pack => pack != null))
                        if (dst.Add(pack!))
                            pack.related_packs(dst);
                }


                private int inheritance_depth;

                internal static void init(ProjectImpl root_project)
                {
                    var all_fields = ProjectImpl.all_fields().ToList();
                    #region process enums
                    foreach (var enum_ in root_project.constants_packs.Where(e => e.is_enum))
                    {
                        if (!enum_.included && enum_.in_project.included) enum_._included = true;

                        foreach (var dst in enum_._static_fields_.Where(fld => fld.substitute_value_from != null)) //substitute value
                        {
                            var src = raw_fields[dst.substitute_value_from!];
                            dst._value_int = src._value_int;
                        }

                        switch (enum_._id) //auto-numbering
                        {
                            case (int)Project.Host.Pack.Field.DataType.t_flags:

                                FieldImpl fi;

                                for (var auto_val = 1UL;
                                     (fi = enum_._static_fields_.FirstOrDefault(f => f._value_int == null)!) != null;
                                     fi._value_int = (long?)auto_val,
                                     auto_val <<= 1
                                   )
                                    while (enum_._static_fields_.Exists(f => f._value_int != null && (ulong)f._value_int == auto_val))
                                        auto_val <<= 1;


                                break;
                            case (int)Project.Host.Pack.Field.DataType.t_enum_sw:

                                var has_value = enum_._static_fields_.Where(f => f._value_int != null).OrderBy(f => f._value_int).ToArray();

                                var i = 0L;

                                if (has_value.Any())
                                {
                                    if (1 < has_value.Length) //maybe the flag enum but missed the flag attribute
                                    {
                                        var flags = has_value.Select(fld => fld._value_int!).Count(val => val != 0 && (val & (val - 1)) == 0);
                                        if (has_value.Length / 2 < flags)
                                            AdHocAgent.LOG.Information("The`{Enum}` enum appears to be a flags enum. The {Flags} attribute may be missing. {correct} ?", enum_._name, "[Flags]", "\n[Flags]\nenum " + enum_._name + "{...}");
                                    }

                                    var mIn = i = (long)has_value.First()._value_int!;

                                    foreach (var fld in has_value) //maybe one-by-one
                                    {
                                        while (1 < fld._value_int - i)
                                        {
                                            if ((fi = enum_._static_fields_.FirstOrDefault(f => f._value_int == null)!) == null) break;
                                            fi._value_int = ++i;
                                        }

                                        i = (long)fld._value_int!;
                                    }

                                    if (0 < mIn) //try to fill to zero
                                    {
                                        var k = mIn;
                                        foreach (var fld in enum_._static_fields_.Where(f => f._value_int == null))
                                        {
                                            fld._value_int = --k;
                                            if (k == 0) break;
                                        }
                                    }

                                    foreach (var fld in enum_._static_fields_.Where(f => f._value_int == null)) fld._value_int = ++i;

                                    i = mIn;
                                    foreach (var fld in enum_._static_fields_.OrderBy(f => f._value_int))
                                        if (1 < fld._value_int - i) goto next; //preserve t_enum_sw type
                                        else i = (long)fld._value_int!;
                                }
                                else //set all
                                    enum_._static_fields_.ForEach(fld => fld._value_int = i++);

                                enum_._id = (int)Project.Host.Pack.Field.DataType.t_enum_exp;

                                break;
                        }

                    next:

                        var fld_0 = enum_._static_fields_[0]; //first enum field

                        var min = enum_._static_fields_.Count == 0 //enums without fields are like typed boolean can be T or null
                                      ?
                                      0 :
                                      enum_._static_fields_.Min(f => f._value_int)!.Value;

                        var max = enum_._static_fields_.Count == 0 //enums without fields are like typed boolean  can be T or null
                                      ?
                                      0 :
                                      enum_._static_fields_.Max(f => f._value_int)!.Value;


                        if (enum_._id == (int)Project.Host.Pack.Field.DataType.t_flags)                      // the enum is flag
                            max = enum_._static_fields_.Aggregate(0L, (i, fld) => i | fld._value_int!.Value); //set all flags

                        if (enum_.is_calculate_enum_type)   //user does not explicitly assign enum type (int by default)
                            fld_0.set_exT_ByRange(min, max); // calculate enum type, by estimate max and min values and apply on zero field

                        if (enum_._id == (int)Project.Host.Pack.Field.DataType.t_enum_sw)
                            fld_0.set_inT_ByRange(0, enum_._static_fields_len);
                        else
                            fld_0.set_inT_ByRange(min, max);

                        fld_0._min_value = min; //store calculated min/max on the first enum field
                        fld_0._max_value = max;


                        #region Propagate enum parameters to the fields where they are used.
                        var this_enum_used_fields = all_fields.Where(fld => SymbolEqualityComparer.Default.Equals(enum_.symbol, fld?.exT_pack)).ToArray();


                        if (max == min && 0 < this_enum_used_fields.Length) // enums with less the one fields or if all fields have same value are like typed boolean can be T or null
                        {
                            enum_._id = (ushort)Project.Host.Pack.Field.DataType.t_subpack; //mark on delete
                            var problem = enum_._static_fields_.Count == 0 ?
                                              " no field" :
                                              enum_._static_fields_.Count == 1 ?
                                                  " only one field" :
                                                  " fields with same values";

                            AdHocAgent.LOG.Warning("Enum {EnumSymbol} has {Problem}. As field data type it\'s useless and will be replaced with boolean", enum_.symbol, problem);

                            foreach (var fld in this_enum_used_fields)
                                fld.switch_to_boolean();

                            continue;
                        }

                        #region Detect nullable fields using this bit-range enum and allocate space for a null value.
                        if (fld_0._bits != null && this_enum_used_fields.Any(fld => fld.nullable))
                        {
                            var null_value = (long)(byte)enum_._static_fields_.Count;
                            var bits_if_field_nullable = 64 - BitOperations.LeadingZeroCount((ulong)null_value);

                            if (enum_._id == (int)Project.Host.Pack.Field.DataType.t_flags) //search a skipped bit in flags enum, to allocate for the null_value
                                for (var s = 0; ; s++)
                                    if (s == 64)
                                    {
                                        bits_if_field_nullable = 65;
                                        break;
                                    }
                                    else if (max == (max | (1L << s))) continue;
                                    else
                                    {
                                        null_value = 1L << s;
                                        break;
                                    }

                            fld_0._bits = (byte)bits_if_field_nullable;

                            foreach (var fld in this_enum_used_fields) //=================================================== propagate
                            {
                                fld.inT = fld_0.inT;
                                fld._min_value = min; //acceptable range
                                fld._max_value = max; //acceptable range

                                if (fld.nullable)
                                {
                                    if (7 < bits_if_field_nullable) continue; //not bits field anymore

                                    fld._bits = (byte?)bits_if_field_nullable;
                                }
                                else fld._bits = fld_0._bits;
                            }

                            continue; //============>>> to next enum_
                        }
                        #endregion
                        //rest of fields
                        foreach (var fld in this_enum_used_fields) //=================================================== propagate
                        {
                            fld.inT = fld_0.inT;
                            fld._min_value = min; //acceptable range
                            fld._max_value = max; //acceptable range
                            fld._bits = fld_0._bits;
                            fld._value_bytes = fld_0._value_bytes;
                        }
                        #endregion
                    }

                    #region _DefaultMaxLengthOf read, delete, and apply operations.
                    var all_default_collection_capacity = root_project.constants_packs.Where(en => en._name.Equals("_DefaultMaxLengthOf")).ToArray();

                    foreach (var pack in all_default_collection_capacity.OrderBy(en => en.in_project == root_project)) //The root project settings should be placed last in order to override all inherited project settings
                        pack._static_fields_.ForEach(fld =>
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

                    foreach (var en in all_default_collection_capacity)
                    {
                        en._static_fields_.ForEach(fld => raw_fields.Remove(fld.symbol!));
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


                    root_project.constants_packs.RemoveAll(enum_ => enum_._id == (ushort)Project.Host.Pack.Field.DataType.t_subpack); // remove marked to delete enums
                    #endregion
                }


                private int cyclic_depth;

                internal int calculate_fields_type_depth(ISet<object> path)
                {
                    if (path.Count == 0) cyclic_depth = 0;

                    try
                    {
                        foreach (var datatype in fields.Where(f => f.exT_pack != null).Select(f => (PackImpl)entities[f.exT_pack!])
                                                       .Concat(fields.Where(f => f.V != null && f.V.exT_pack != null).Select(f => (PackImpl)entities[f.V!.exT_pack!])).Distinct())
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
                            AdHocAgent.LOG.Error("Line {line}: Unsupported field type '{type}' detected for field '{field}'.", fld.line_in_src_code, fld.exT_pack, fld);
                        AdHocAgent.exit("", 23);
                    }

                    return path.Count == 0 ?
                               cyclic_depth :
                               0;
                }


                public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once)
                {
                    if (raw_fields.TryGetValue(by_what, out var by_fld)) apply(by_fld);
                    else if (entities.TryGetValue(by_what, out var value) && value is PackImpl by_pack)
                    {
                        by_pack.Init(once);
                        foreach (var fLd in by_pack.fields.Concat(by_pack._static_fields_)) apply(fLd);
                    }
                    else
                        AdHocAgent.exit($"Unexpected attempt to apply {(add ? "add" : "remove")} modification by {by_what} to the pack {symbol} (line: {line_in_src_code}).");


                    void apply(FieldImpl fld)
                    {
                        if (add)
                        {
                            var flds = fields;
                            var i = flds.FindIndex(f => f._name == fld._name); //same name
                            if (i == -1)
                                i = (flds = _static_fields_).FindIndex(s => s._name == fld._name); //same name


                            if (-1 < i)
                                if (inited) flds.RemoveAt(i); //modifier force
                                else return;                   //normal init

                            (fld.is_const ?
                                 _static_fields_ :
                                 fields).Add(fld);
                        }
                        else
                            (fld.is_const ?
                                 _static_fields_ :
                                 fields).Remove(fld);
                    }
                }


                public bool exists(string fld_name) => fields.Any(fld => fld._name.Equals(fld_name)) || _static_fields_.Any(fld => fld._name.Equals(fld_name));

                public override string ToString() => _name +
                                                     _id switch
                                                     {
                                                         (int)Project.Host.Pack.Field.DataType.t_enum_exp => " : enum_exp",
                                                         (int)Project.Host.Pack.Field.DataType.t_enum_sw => " : enum_sw",
                                                         (int)Project.Host.Pack.Field.DataType.t_flags => " : enum_flags",
                                                         (int)Project.Host.Pack.Field.DataType.t_constants => " : const_set",
                                                         (int)Project.Host.Pack.Field.DataType.t_subpack => " : subpack",
                                                         < (int)Project.Host.Pack.Field.DataType.t_subpack => $" : pack {_id} ",
                                                         _ => "???"
                                                     };

                public class FieldImpl : HasDocs, Project.Host.Pack.Field
                {
                    public ISymbol? substitute_value_from;
                    private readonly SemanticModel model;
                    public readonly FieldDeclarationSyntax? fld_node;

                    private FieldImpl(ProjectImpl project, FieldDeclarationSyntax? node, SemanticModel? model) : base(project, "", null) //virtual field used to hold information of V in Map(K,V)
                    {
                        fld_node = node;
                        this.model = model;
                    }

                    void check_name()
                    {
                        if (_name.Equals(symbol.Name)) return;
                        AdHocAgent.LOG.Warning("The field '{entity}' name at the {provided_path} line: {line} is prohibited. Please correct the name.", symbol, AdHocAgent.provided_path, line_in_src_code);
                        AdHocAgent.exit("");
                    }

                    public readonly bool is_const;

                    public FieldImpl(ProjectImpl project, EnumMemberDeclarationSyntax node, SemanticModel model) : base(project, node.Identifier.ToString(), node) //enum field
                    {
                        this.model = model;
                        symbol = model.GetDeclaredSymbol(node)!;
                        check_name();
                        if (entities[symbol!.ContainingType] is PackImpl pack) pack._static_fields_.Add(this);
                        else AdHocAgent.exit($"`{entities[symbol!.ContainingType].full_path}` cannot contains any fields. Delete `{_name}`.");

                        raw_fields.Add(symbol, this);

                        var user_assigned_value = node.EqualsValue == null ?
                                                      null :
                                                      project.runtimeFieldInfo(symbol).GetRawConstantValue();
                        init_exT(symbol.ContainingType.EnumUnderlyingType!, user_assigned_value);
                        is_const = true;
                        if (!_name.Equals(symbol.Name))
                            AdHocAgent.LOG.Warning("The name of {entity} is prohibited and changed to {new_name}. Please correct the name.", symbol, _name);
                    }

                    public bool is_Set;
                    public bool is_Map => V != null;
                    public bool is_String => exT_primitive == (int?)Project.Host.Pack.Field.DataType.t_string;

                    public FieldImpl(ProjectImpl project, FieldDeclarationSyntax node, VariableDeclaratorSyntax variable, SemanticModel model) : base(project, model.GetDeclaredSymbol(variable)!.Name, node) //pack fields
                    {
                        this.model = model;
                        symbol = model.GetDeclaredSymbol(variable)!;
                        check_name();
                        fld_node = node;
                        var T = model.GetTypeInfo(node.Declaration.Type).Type!;

                        raw_fields.Add(symbol, this);
                        if (entities[symbol!.ContainingType] is PackImpl pack)
                            if (symbol is IFieldSymbol fld && (is_const = fld.IsStatic || fld.IsConst)) //  static/const field
                            {
                                pack._static_fields_.Add(this);

                                var constant = project.runtimeFieldInfo(symbol).GetValue(null); // runtime constant value
                                switch (T)
                                {
                                    case IArrayTypeSymbol array:
                                        init_exT((INamedTypeSymbol)array.ElementType, null);
                                        _array_ = (Array?)constant;
                                        break;
                                    case INamedTypeSymbol type:
                                        init_exT(type, constant);
                                        break;
                                }
                            }
                            else
                            {
                                pack.fields.Add(this);

                                void KV_params(ITypeSymbol KV, FieldImpl dst)
                                {
                                    switch (KV)
                                    {
                                        case IArrayTypeSymbol array:
                                            if (KV.NullableAnnotation == NullableAnnotation.Annotated) dst.set_null_value_bit(1);
                                            dst._exT_array = (uint)(array.Rank - 1);

                                            dst.init_exT((INamedTypeSymbol)array.ElementType, null);
                                            return;
                                        case INamedTypeSymbol type:
                                            dst.init_exT(type, null);
                                            return;
                                    }
                                }

                                var nullable = fld_node?.Declaration.Type is NullableTypeSyntax;

                                if (T.isSet())
                                {
                                    is_Set = true;

                                    switch (T)
                                    {
                                        case IArrayTypeSymbol array: //Set<int>[,]
                                            if (nullable) set_null_value_bit(3);
                                            if (array.ElementType.NullableAnnotation == NullableAnnotation.Annotated) set_null_value_bit(2);
                                            _map_set_array = (uint)(array.Rank - 1);

                                            KV_params(((INamedTypeSymbol)array.ElementType).TypeArguments[0], this);

                                            break;
                                        case INamedTypeSymbol type:
                                            if (nullable) set_null_value_bit(2);
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
                                        case IArrayTypeSymbol array: //Map<int, int>[,]
                                            if (nullable) set_null_value_bit(3);
                                            if (array.ElementType.NullableAnnotation == NullableAnnotation.Annotated) set_null_value_bit(2);
                                            _map_set_array = (uint)(array.Rank - 1);

                                            KV((INamedTypeSymbol)array.ElementType);

                                            break;
                                        case INamedTypeSymbol type:
                                            if (nullable) set_null_value_bit(2);
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

                                            switch (array.ElementType)
                                            {
                                                case IArrayTypeSymbol array_ext:
                                                    if (nullable) set_null_value_bit(3);
                                                    if (array_ext.NullableAnnotation == NullableAnnotation.Annotated) set_null_value_bit(2);
                                                    _map_set_array = (uint)(array.Rank - 1);

                                                    _exT_array = (uint)(array_ext.Rank - 1);
                                                    init_exT((INamedTypeSymbol)array_ext.ElementType, null);
                                                    break;
                                                case INamedTypeSymbol type:
                                                    if (nullable) set_null_value_bit(2);

                                                    _exT_array = (uint)(array.Rank - 1);
                                                    init_exT(type, null);

                                                    break;
                                            }

                                            break;
                                        case INamedTypeSymbol type:
                                            if (nullable)
                                            {
                                                set_null_value_bit(0);
                                                init_exT(type is { SpecialType: SpecialType.None, TypeArguments.Length: > 0 } ?
                                                             (INamedTypeSymbol)type.TypeArguments[0] :
                                                             type, null);
                                            }
                                            else
                                                init_exT(type, null);

                                            break;
                                    }
                            }
                        else AdHocAgent.exit($"The entity `{entities[symbol!.ContainingType].full_path}` cannot contains any fields. Delete the field `{_name}` line:{line_in_src_code}.");

                        if (!_name.Equals(symbol.Name))
                            AdHocAgent.LOG.Warning("The name of {entity} is prohibited and changed to {new_name}. Please correct the name manually", symbol, _name);
                    }

                    public void switch_to_boolean()
                    {
                        exT_pack = null;
                        exT_primitive = (int?)Project.Host.Pack.Field.DataType.t_bool;
                        inT = (int?)Project.Host.Pack.Field.DataType.t_bool;
                        _bits = (byte?)(nullable ?
                                            2 :
                                            1);
                    }

                    public class _DefaultMaxLengthOf
                    {
                        public static int Strings = 255;
                        public static int Arrays = 255;
                        public static int Maps = 255;
                        public static int Sets = 255;
                    }

                    #region exT
                    private INamedTypeSymbol init_exT(INamedTypeSymbol T, object? constant)
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

                                if (constant != null)
                                    _value_int = (bool)constant ?
                                                     1 :
                                                     0;
                                break;
                            case SpecialType.System_SByte:
                                _value_int = (sbyte?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_int8;
                                _value_bytes = 1;
                                break;
                            case SpecialType.System_Byte:
                                _value_int = (byte?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_uint8;
                                _value_bytes = 1;
                                break;
                            case SpecialType.System_Int16:
                                _value_int = (short?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_int16;
                                _value_bytes = 2;
                                break;
                            case SpecialType.System_UInt16:
                                _value_int = (ushort?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_uint16;
                                _value_bytes = 2;
                                break;
                            case SpecialType.System_Char:
                                _value_int = (char?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_char;
                                _value_bytes = 2;
                                break;
                            case SpecialType.System_Int32:
                                _value_int = (int?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_int32;
                                _value_bytes = 4;
                                break;
                            case SpecialType.System_UInt32:
                                _value_int = (uint?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_uint32;
                                _value_bytes = 4;
                                break;
                            case SpecialType.System_Int64:
                                _value_int = (long?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_int64;
                                _value_bytes = 8;
                                break;
                            case SpecialType.System_UInt64:
                                _value_int = (long?)(ulong?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_uint64;
                                _value_bytes = 8;
                                break;
                            case SpecialType.System_Single:
                                _value_double = (float?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_float;
                                _value_bytes = 4;
                                break;
                            case SpecialType.System_Double:
                                _value_double = (double?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_double;
                                _value_bytes = 8;
                                break;
                            case SpecialType.System_String:
                                _value_string = constant?.ToString();
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

                    public void set_exT_ByRange(BigInteger min, BigInteger max)
                    {
                        if (min == max && !is_const)
                        {
                            AdHocAgent.LOG.Error("The applied value range for the '{field}' field line:{line} doesn't make sense.", this, line_in_src_code);
                            AdHocAgent.exit("", -1);
                        }

                        if (min < 0)
                            if (min < int.MinValue || int.MaxValue < max)
                            {
                                exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int64;
                                _value_bytes = 8;
                            }
                            else if (min < short.MinValue || short.MaxValue < max)
                            {
                                exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int32;
                                _value_bytes = 4;
                            }
                            else if (min < sbyte.MinValue || sbyte.MaxValue < max)
                            {
                                exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int16;
                                _value_bytes = 2;
                            }
                            else
                            {
                                exT_primitive = (int)Project.Host.Pack.Field.DataType.t_int8;
                                _value_bytes = 1;
                            }
                        else if (max > uint.MaxValue)
                        {
                            exT_primitive = (int)Project.Host.Pack.Field.DataType.t_uint64;
                            _value_bytes = 8;
                        }
                        else if (max > ushort.MaxValue)
                        {
                            exT_primitive = (int)Project.Host.Pack.Field.DataType.t_uint32;
                            _value_bytes = 4;
                        }
                        else if (max > byte.MaxValue)
                        {
                            exT_primitive = (int)Project.Host.Pack.Field.DataType.t_uint16;
                            _value_bytes = 2;
                        }
                        else
                        {
                            exT_primitive = (int)Project.Host.Pack.Field.DataType.t_uint8;
                            _value_bytes = 1;
                        }
                    }

                    internal PackImpl? get_exT_pack => exT_pack == null ?
                                                           null :
                                                           (PackImpl)entities[exT_pack];

                    public INamedTypeSymbol? exT_pack;
                    public int? exT_primitive;
                    public ushort _exT => (ushort)(exT_primitive ?? get_exT_pack?.idx)!;


                    public uint? _exT_len { get; set; }
                    public uint? _exT_array { get; set; }

                    public uint? _map_set_len { get; set; } //mandatory if Map or Set
                    public uint? _map_set_array { get; set; } //the flat array of Map/Set/Array collection params


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

                    #region inT
                    public void set_inT_ByRange(BigInteger min, BigInteger max)
                    {
                        if (min == max && !is_const)
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
                    public sbyte? _dir { get; set; }
                    public long? _min_value { get; set; }
                    public long? _max_value { get; set; }

                    private bool _check_once;

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

                    public double? _min_valueD { get; set; }
                    public double? _max_valueD { get; set; }
                    public byte? _bits { get; set; }
                    public byte? _null_value { get; set; }

                    void set_null_value_bit(int bit) => _null_value = (byte)(_null_value == null ?
                                                                                 1 << bit :
                                                                                 _null_value.Value | (1 << bit));

                    internal bool nullable => _null_value != null && (_null_value.Value & 1) == 1;

                    #region V
                    public FieldImpl? V; //Map Value datatype Info


                    public ushort? _exTV => V?._exT;
                    public uint? _exTV_len => V?._exT_len;
                    public uint? _exTV_array => V?._exT_array;

                    public ushort? _inTV => V?._inT;
                    public sbyte? _dirV => V?._dir;
                    public long? _min_valueV => V?._min_value;
                    public long? _max_valueV => V?._max_value;
                    public double? _min_valueDV => V?._min_valueD;
                    public double? _max_valueDV => V?._max_valueD;

                    public byte? _bitsV => V?._bits;

                    public byte? _null_valueV => V?._null_value;
                    #endregion


                    public long? _value_int { get; set; }
                    public double? _value_double { get; set; }
                    public string? _value_string { get; set; }

                    #region array
                    private Array? _array_;
                    public string _array(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => _array_!.GetValue(item)!.ToString()!;

                    public int _array_len => _array_!.Length;
                    public object? _array() => _array_;
                    #endregion

                    public int? _value_bytes { get; set; }

                    private BigInteger value_of(ExpressionSyntax src) //get real value
                    {
                        if (src.IsKind(SyntaxKind.IdentifierName))
                        {
                            var fld = raw_fields[model.GetSymbolInfo(src).Symbol!];
                            return (fld.substitute_value_from == null ?
                                        fld :
                                        raw_fields[fld.substitute_value_from])._value_int!.Value; //return sudstitute value
                        }

                        try { return Convert.ToInt64(model.GetConstantValue(src).Value); }
                        catch (Exception) { return Convert.ToUInt64(model.GetConstantValue(src).Value); }
                    }

                    private double dbl_val(ExpressionSyntax src)
                    {
                        if (!src.IsKind(SyntaxKind.IdentifierName)) return Convert.ToDouble(model.GetConstantValue(src).Value);

                        var fld = raw_fields[model.GetSymbolInfo(src).Symbol!];
                        return (fld.substitute_value_from == null ?
                                    fld :
                                    raw_fields[fld.substitute_value_from])._value_double!.Value; //return sudstitute value
                    }

                    #region dims
                    public int[]? dims;

                    public object? _dims() => dims;
                    public int _dims_len => dims!.Length;
                    public int _dims(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => dims![item];
                    #endregion


                    public static void init(ProjectImpl project)
                    {
                        var fields = raw_fields.Values;
                        #region processs Attributes
                        foreach (var fld in fields.Where(fld => fld.is_const && fld.fld_node != null)) //calculated  values for const fields
                            foreach (var args_list in from list in fld.fld_node!.AttributeLists
                                                      from attr in list.Attributes
                                                      where (attr.Name + "Attribute").Equals("ValueForAttribute")
                                                      select attr.ArgumentList
                                                      into args_list
                                                      where args_list != null
                                                      select args_list)
                            {
                                var dst_const_fld = raw_fields[fld.model.GetSymbolInfo(args_list.Arguments[0].Expression).Symbol!];
                                if (dst_const_fld.substitute_value_from != null)
                                {
                                    AdHocAgent.LOG.Error("The const field {const_field} already has a value assigned from static field {current_static}, and the static field {new_static} would override it. This redundancy is unnecessary and serves no purpose.", dst_const_fld, dst_const_fld.substitute_value_from, fld.symbol);
                                    AdHocAgent.exit("Fix the problem and rerun");
                                }

                                dst_const_fld.substitute_value_from = fld.symbol;
                            }

                        var dims = new List<int>();

                        foreach (var fld in fields.Where(fld => !fld.is_const))
                        {
                            var FLD = fld;

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
                                foreach (var attr in list.Attributes)
                                {
                                    var name = attr.Name.ToString();
                                    var attr_args_list = attr.ArgumentList;

                                    switch (!name.EndsWith("Attribute") ?
                                                $"{name}Attribute" :
                                                name)
                                    {
                                        case "DAttribute":
                                            {
                                                var attr_args = attr_args_list!.Arguments;
                                                if (attr_args.Count == 0)
                                                    AdHocAgent.exit($"The [Dims] attribute on the field {fld} has no declared dimensions, which is incorrect.", 2);

                                                foreach (var exp in attr_args.Select(arg => arg.Expression))
                                                    if (exp is PrefixUnaryExpressionSyntax _exp) //read and control of using Dims attribute args
                                                    {
                                                        var val = (uint)FLD!.value_of(_exp.Operand);

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

                                                        if (FLD._exT_array != null)                            //fully correct using argument
                                                            FLD._exT_array |= (uint)FLD.value_of(exp) << 3;     //take the max length of the array from `exp`
                                                        else                                                    //not fully correct but ok
                                                            FLD._map_set_array |= (uint)FLD.value_of(exp) << 3; //take the max length of the array of Map/Set/Array collection from the `exp`
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
                                                    FLD._min_valueD = FLD.dbl_val(attr_args[0].Expression);
                                                    FLD._max_valueD = FLD.dbl_val(attr_args[1].Expression);

                                                    if (FLD._max_valueD < FLD._min_valueD) (FLD._min_valueD, FLD._max_valueD) = (FLD._max_valueD, FLD._min_valueD);

                                                    if (FLD._min_valueD < float.MinValue || float.MaxValue < FLD._max_valueD)
                                                    {
                                                        FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_double;
                                                        FLD._value_bytes = 8;
                                                    }
                                                    else
                                                    {
                                                        FLD.exT_primitive = (int)Project.Host.Pack.Field.DataType.t_float;
                                                        FLD._value_bytes = 4;
                                                    }
                                                }
                                                else
                                                    setByRange(FLD, FLD.value_of(attr_args[0].Expression), FLD.value_of(attr_args[1].Expression));
                                            }
                                            continue;

                                        case "AAttribute":
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
                                                                              FLD.value_of(attr_args_list.Arguments[0].Expression);

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
                                                           FLD.value_of(attr_args_list.Arguments[0].Expression));
                                            else //two arguments
                                                setByRange(FLD,
                                                           FLD.value_of(attr_args_list.Arguments[0].Expression),
                                                           FLD.value_of(attr_args_list.Arguments[1].Expression));

                                            check_is_varinTable(FLD);
                                            break;
                                        case "VAttribute":
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
                                                                              FLD.value_of(attr_args_list.Arguments[0].Expression);
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
                                                           FLD.value_of(attr_args_list.Arguments[0].Expression),
                                                           0);
                                            else //two arguments
                                                setByRange(FLD,
                                                           FLD.value_of(attr_args_list.Arguments[1].Expression),
                                                           FLD.value_of(attr_args_list.Arguments[0].Expression));


                                            check_is_varinTable(FLD);
                                            break;
                                        case "XAttribute":
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
                                                                                       (long)FLD.value_of(attr_args_list.Arguments[0].Expression));
                                                FLD.set_exT_ByRange(min + zero, max + zero);
                                            }
                                            else
                                            {
                                                var zero = 0L;
                                                var amplitude = 0L;

                                                if (attr_args_list.Arguments[0].NameColon == null || attr_args_list.Arguments[0].NameColon!.Name.ToString().Equals("Amplitude"))
                                                {
                                                    amplitude = (long)FLD.value_of(attr_args_list.Arguments[0].Expression);
                                                    if (1 < attr_args_list.Arguments.Count)
                                                        zero = (long)FLD.value_of(attr_args_list.Arguments[1].Expression);
                                                }
                                                else
                                                {
                                                    zero = (long)FLD.value_of(attr_args_list.Arguments[0].Expression);
                                                    amplitude = (long)FLD.value_of(attr_args_list.Arguments[1].Expression);
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

                        #region Process constants substitute
                        foreach (var dst_fld in fields.Where(fld => !fld.is_const && fld.substitute_value_from != null)) //not enums but static fields
                        {
                            var src_fld = raw_fields[dst_fld.substitute_value_from!]; //takes type and value from src
                            dst_fld.exT_primitive = dst_fld.inT = src_fld.exT_primitive;
                            dst_fld._value_double = src_fld._value_double;
                            dst_fld._value_int = src_fld._value_int;
                            dst_fld._value_string = src_fld._value_string;
                        }
                        #endregion
                    }


                    private static void setByRange(FieldImpl fld, BigInteger min, BigInteger max)
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


        public class ChannelImpl : Entity, Project.Channel
        {
            public byte _uid => (byte)uid;

            public ChannelImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax Channel) : base(project, compilation, Channel) //struct based
            {
                project.channels.Add(this);
                if (parent_entity is not ProjectImpl) AdHocAgent.exit($"The definition of the channel {symbol} should be placed directly within the project’s scope.");

                var interfaces = symbol!.Interfaces;
                if (0 < interfaces.Length)
                    switch (interfaces[0].Name)
                    {
                        case "ChannelFor":
                            if (!equals(symbol!.Interfaces[0].TypeArguments[0], symbol!.Interfaces[0].TypeArguments[1])) return;
                            AdHocAgent.LOG.Error("The channel {ch} should connect two distinct hosts.", symbol);
                            AdHocAgent.exit("Fix the problem and restart");
                            return;

                        case "Modify":
                            if (symbol!.Interfaces[0].TypeArguments[0] is INamedTypeSymbol sym && sym.TypeKind == TypeKind.Interface) return; //minimal test
                            AdHocAgent.LOG.Error("The channel {channel} can Modify other channels only. But {Modify} is not", symbol, symbol!.Interfaces[0].TypeArguments[0]);
                            AdHocAgent.exit("Fix the problem and restart");

                            return;
                    }

                AdHocAgent.LOG.Error("The channel {channel} should `implements` the {ChannelFor} or {Modify}  interfaces.", symbol, "org.unirail.Meta.ChannelFor<HostA,HostB>", "org.unirail.Meta.Modify<ModifyChannel>");
                AdHocAgent.exit("Fix the problem and restart");
            }

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


            public HostImpl? hostL;
            public ushort _hostL => (ushort)hostL.idx;

            public List<HostImpl.PackImpl> hostL_transmitting_packs = [];

            public object? _hostL_transmitting_packs() => hostL_transmitting_packs.Count == 0 ?
                                                              null :
                                                              hostL_transmitting_packs;

            public int _hostL_transmitting_packs_len => hostL_transmitting_packs.Count;
            public ushort _hostL_transmitting_packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => (ushort)hostL_transmitting_packs[item].idx;


            public List<HostImpl.PackImpl> hostL_related_packs = [];

            public object? _hostL_related_packs() => hostL_related_packs.Count == 0 ?
                                                         null :
                                                         hostL_related_packs;

            public int _hostL_related_packs_len => hostL_related_packs.Count;
            public ushort _hostL_related_packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => (ushort)hostL_related_packs[item].idx;


            public HostImpl? hostR;
            public ushort _hostR => (ushort)hostR!.idx;

            public List<HostImpl.PackImpl> hostR_transmitting_packs = [];

            public object? _hostR_transmitting_packs() => hostR_transmitting_packs.Count == 0 ?
                                                              null :
                                                              hostR_transmitting_packs;

            public int _hostR_transmitting_packs_len => hostR_transmitting_packs.Count;
            public ushort _hostR_transmitting_packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => (ushort)hostR_transmitting_packs[item].idx;


            public List<HostImpl.PackImpl> hostR_related_packs = [];

            public object? _hostR_related_packs() => hostR_related_packs.Count == 0 ?
                                                         null :
                                                         hostR_related_packs;

            public int _hostR_related_packs_len => hostR_related_packs.Count;
            public ushort _hostR_related_packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => (ushort)hostR_related_packs[item].idx;


            public List<StageImpl> stages = [];

            public object? _stages() => stages.Count == 0 ?
                                            null :
                                            stages;

            public int _stages_len => stages.Count;
            public Project.Channel.Stage _stages(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => stages[item];


            public override bool Init_As_Modifier_Dispatch_Modifications_On_Targets(HashSet<object> once)
            {
                foreach (var modified_channel in this_modified.Select(s => s.TypeArguments[0])) //normally only one modified channel
                    foreach (var stage in symbol!.GetTypeMembers())                             //stages declared in this channel body
                        foreach (var by_stage_modefied_entity in stage.Interfaces.Where(I => I.isModify()).Select(I => I.TypeArguments[0]))
                            if (!equals(modified_channel, by_stage_modefied_entity) && !equals(modified_channel, by_stage_modefied_entity.OriginalDefinition.ContainingType))
                                AdHocAgent.LOG.Warning("Stage {stage} (line: {line}) in channel {symbol}, modifying Channel {ch}, is attempting to modify entity <{modefied}> from a different channel. This is likely an error.", stage, entities[stage].line_in_src_code, symbol, modified_channel, by_stage_modefied_entity);

                foreach (var I in this_modified)
                {
                    modifier = true;
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
                    modifier = true;
                    Init_Collect_Modification(entities[stage.TypeArguments[0].OriginalDefinition.ContainingType], once);
                    return true;
                }

                var i = symbol!.Interfaces.First(I => I.isChannelFor());
                hostL = (HostImpl)entities[i.TypeArguments[0]];
                hostR = (HostImpl)entities[i.TypeArguments[1]];

                if (stages.Count == 0)
                    AdHocAgent.exit($"Channel {symbol} does not have any stages. Please add a stage and restart.");
                return false;
            }

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


            public void set_transmitting_packs(HashSet<object> once)
            {
                once.Clear(); // Clear the set to prepare for storing visited stages

                #region sweap stages not reachable from root stage
                void scan(StageImpl src) // Recursive method to traverse and mark all reachable stages
                {
                    if (!once.Add(src)) return;                             // If the stage has already been visited, return
                    src.idx = -1;                                            // Mark the stage as reachable from root - stages[0]
                    foreach (var br in src.branchesL.Concat(src.branchesR)) // Recursively scan through all branches, following links to other stages
                        scan(br.goto_stage ?? (br.goto_stage = src));
                }

                scan(stages[0]); // Start the traversal from the root stage (stages[0])

                // Remove stages that were not marked as reachable (idx != -1) and reindex the remaining ones
                stages = stages.Where(st => st.idx == -1).Select((st, i) =>
                                                                 {
                                                                     st.idx = i; // Assign a new index to each remaining stage
                                                                     return st;
                                                                 }).ToList();
                #endregion
                #region merge branches with same goto stage
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

                        dst.packs = dst.packs.GroupBy(p => p.symbol, SymbolEqualityComparer.Default)
                                       .Select(g => g.First()).ToList();
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
            }


            public class NamedPackSet : Entity
            {
                public HashSet<HostImpl.PackImpl> packs = [];

                internal NamedPackSet(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax stage) : base(project, compilation, stage) { }

                public void apply(HostImpl.PackImpl pack, bool add)
                {
                    if (add)
                        packs.Add(pack);
                    else
                        packs.Remove(pack);
                }

                public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once)
                {
                    if (named_packs.TryGetValue(by_what, out var nps))
                    {
                        foreach (var pack in nps.packs)
                            apply(pack, add);
                        return;
                    }

                    if (entities.TryGetValue(by_what, out var value))
                        switch (value)
                        {
                            case ProjectImpl prj:
                                prj.for_packs_in_scope(depth, pack => apply(pack, add));
                                return;
                            case HostImpl host:
                                host.for_packs_in_scope(depth, pack => apply(pack, add));
                                return;
                            case HostImpl.PackImpl pack:
                                pack.for_packs_in_scope(depth, _pack => apply(_pack, add), symbol!);
                                return;
                        }


                    AdHocAgent.LOG.Error("Unexpected item {item} import in the named packs set {pack}", by_what, symbol);
                    AdHocAgent.exit("Fix the problem and restart");
                }
            }

            public class StageImpl : Entity, Project.Channel.Stage
            {
                public ushort _uid => (ushort)uid;
                public static StageImpl? Exit;


                internal StageImpl(ProjectImpl project) : base(project, null, null) { _name = ""; }


                internal StageImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax stage) : base(project, compilation, stage)
                {
                    _name = symbol!.Name;

                    if (parent_entity is not ChannelImpl)
                    {
                        AdHocAgent.LOG.Error("Stage {stage} declaration must be within a channel scope, but {parent_entity} is not a channel", symbol, symbol!.OriginalDefinition.ContainingType);
                        AdHocAgent.exit("Fix the problem and try again");
                    }

                    var ch = project.channels.Last();

                    ch.stages.Add(this);
                    foreach (var attr in node!.AttributeLists.SelectMany(list => list.Attributes))
                        switch (attr.Name.ToString())
                        {
                            case "Timeout":
                                _timeout = Convert.ToUInt16(model.GetConstantValue(attr.ArgumentList!.Arguments[0].Expression).Value);
                                break;
                        }
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


                public bool _LR => branchesR == branchesL;

                protected override void Init_Collect_Modification(Entity target, HashSet<object> once)
                {
                    var target_stage = (StageImpl)target;

                    List<BranchImpl>? branches = null;

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
                                branches = target_stage.branchesR = target_stage.branchesL;
                                continue;
                            default:

                                bool scan(INamedTypeSymbol src, Func<INamedTypeSymbol, bool> todo) => todo(src) && src.TypeArguments.OfType<INamedTypeSymbol>().All(sym => scan(sym, todo));

                                if (str.StartsWith("Modify<")) continue;

                                if (str.StartsWith("_<")) //Branch start
                                {
                                    var uid_pos = item.SpanStart + 2; // "_<".length
                                    BranchImpl? branch;

                                    if (modifier) // modify specific stage
                                    {
                                        #region search - maybe modify specific branch
                                        StageImpl? update_from_goto_stage = null;
                                        StageImpl? update_to_goto_stage = null;

                                        //fast self modifier pre scan goto stages
                                        var generics = item.DescendantNodes().OfType<GenericNameSyntax>().First();
                                        foreach (var stage_sym in generics.TypeArgumentList.Arguments.Select(generic => model.GetSymbolInfo(generic).Symbol!))
                                            if (stage_sym.isMeta())
                                            {
                                                if (stage_sym.Name == "X") //maybe this is the replaceable goto stage
                                                    scan((INamedTypeSymbol)stage_sym, t => (!entities.TryGetValue(t, out var entity) || entity is not StageImpl st) || (update_from_goto_stage = st) == null);
                                            }
                                            else if (stage_sym.Name != "Exit")
                                                if (entities[stage_sym] is StageImpl st)
                                                {
                                                    st.Init(once);
                                                    var targget_channel = (ChannelImpl)target.parent_entity!;

                                                    update_to_goto_stage = targget_channel.stages.First(s => equals(s.symbol, st.symbol));
                                                }

                                        var search_branch_by_goto_stage = update_from_goto_stage ?? update_to_goto_stage;

                                        branch = branches!.FirstOrDefault(br => br.goto_stage == search_branch_by_goto_stage);
                                        #endregion

                                        if (branch == null)
                                            branches.Add(branch = new BranchImpl(project)
                                            {
                                                uid_pos = uid_pos,
                                                line_in_src_code = (uint)line,
                                                _doc = string.Join(' ', item.GetLeadingTrivia().Select(t => get_doc(t)))
                                            });

                                        branch.goto_stage = update_to_goto_stage ?? update_from_goto_stage;
                                    }
                                    else
                                        branches!.Add(branch = new BranchImpl(project)
                                        {
                                            uid_pos = uid_pos,
                                            line_in_src_code = (uint)line,
                                            _doc = string.Join(' ', item.GetLeadingTrivia().Select(t => get_doc(t))),
                                            goto_stage = this //self referenced by default
                                        });

                                    #region getting branch's /*UID*/ and inline comments
                                    foreach (var t in item.DescendantTrivia().Where(t => item.Span.Start < t.Span.Start))
                                        if (t.IsKind(SyntaxKind.EndOfLineTrivia)) break;
                                        else if (t.IsKind(SyntaxKind.MultiLineCommentTrivia))
                                        {
                                            var m = HasDocs.uid.Match(t.ToString());
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

                                    modify_by_extends(item, true, (sym, sn, add, depth) =>
                                                                  {
                                                                      if (sym.isMeta())
                                                                      {
                                                                          if (sym.Name != "Exit") return;
                                                                          Exit ??= new StageImpl(projects[0])
                                                                          {
                                                                              symbol = (INamedTypeSymbol?)sym
                                                                          };

                                                                          if (!modifier) branch.goto_stage = Exit;
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
                                                                                  pack.for_packs_in_scope(depth, _pack => apply(_pack, add), symbol!);
                                                                                  return;

                                                                              case StageImpl stage:                          //branch goto target stage
                                                                                  if (!modifier) branch.goto_stage = stage; //modifier do it upper, differently
                                                                                  break;
                                                                              default:

                                                                                  AdHocAgent.LOG.Error("Unexpected item {item} (like:{line}) in the {stage} declaration",
                                                                                                       sym, sn.GetLocation().GetLineSpan().StartLinePosition.Line + 1, symbol);
                                                                                  AdHocAgent.exit("Fix the problem and restart.");
                                                                                  break;
                                                                          }
                                                                  });


                                    continue;
                                }


                                AdHocAgent.LOG.Error("The stage {stage} may only have either the {L} or {R} side. The presence of the {item} is unacceptable.", symbol, "Meta.L", "Meta.R", item);
                                AdHocAgent.exit("Fix the problem and try again");
                                continue;
                        }
                    }
                }

                public override void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once) { }


                private ushort timeout = 0xFFFF;

                public ushort _timeout { get => timeout; set => timeout = value; }
                public List<BranchImpl> branchesL = [];
                public object? _branchesL() => branchesL;
                public int _branchesL_len => branchesL.Count;
                public Project.Channel.Stage.Branch _branchesL(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => branchesL[item];

                public List<BranchImpl> branchesR = [];

                public object? _branchesR() => _LR ?
                                                   null :
                                                   branchesR;

                public int _branchesR_len => _LR ?
                                                 0 :
                                                 branchesR.Count;

                public Project.Channel.Stage.Branch _branchesR(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => branchesR[item];

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

            public class BranchImpl(ProjectImpl project) : Project.Channel.Stage.Branch
            {
                public BranchImpl? origin;

                public BranchImpl clone() => new(project)
                {
                    origin = this,
                    _doc = _doc,
                    uid = uid,
                    packs = packs.ToList(),
                };

                public ushort uid = ushort.MaxValue;
                public ushort _uid => uid;
                public bool no_info = true;

                public ProjectImpl project = project;
                public int uid_pos;
                public uint line_in_src_code;

                public string? _doc { get; set; }

                public StageImpl? goto_stage; //if null Exit stage

                public ushort _goto_stage => goto_stage == StageImpl.Exit ?
                                                 Project.Channel.Stage.Exit :
                                                 (ushort)goto_stage!.idx;


                public List<HostImpl.PackImpl> packs = [];


                public object? _packs() => packs.Count == 0 ?
                                               null :
                                               packs;

                public int _packs_len => packs.Count;
                public ushort _packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => (ushort)packs[item].idx;
            }
        }
    }


    public abstract class HasDocs
    {
        private static readonly Regex leading_spaces = new(@"^\s+", RegexOptions.Multiline);
        private static readonly Regex inline_comments_cleaner = new(@"^\s*/{2,}", RegexOptions.Multiline);
        private static readonly Regex block_comments_start = new(@"/\*+", RegexOptions.Multiline);
        private static readonly Regex block_comments_start_line = new(@"/\*+\s*(\r\n|\r|\n)", RegexOptions.Multiline);
        private static readonly Regex block_comments_end = new(@"\s*\*+/", RegexOptions.Multiline);
        private static readonly Regex block_comments_end_line = new(@"\s*\*+/", RegexOptions.Multiline);
        private static readonly Regex cleanup_asterisk = new(@"^\s*\*+", RegexOptions.Multiline);
        private static readonly Regex cleanup_see_cref = new(@"<\s*see\s*cref .*>", RegexOptions.Multiline);
        public static readonly Regex uid = new(@"\/\*([\u00FF-\u01FF]+)\*\/");

        public static string get_doc(SyntaxTrivia trivia)
        {
            var str = trivia.ToFullString().Trim();
            if (str.Length == 0 || str.StartsWith('#')) return ""; //skip preprocessor instructions

            //normalize doc. apply "left alignment"

            foreach (var m in leading_spaces.Matches(str).Reverse()) //Reverse!!
            {
                var s = m.Groups[0].Value;
                if (-1 < s.IndexOf('\t'))
                {
                    s = s.Replace("\t", "    ");
                    str = str[..m.Groups[0].Index] + s + str[(m.Groups[0].Index + m.Groups[0].Length)..];
                }

                len2count.TryGetValue(s.Length, out var count);
                count++;
                len2count[s.Length] = count;
            }

            if (0 < len2count.Count)
            {
                var most = len2count.ToArray().OrderBy(e => -e.Value).First().Key;
                len2count.Clear();

                str = new Regex(@"^\s" + "{1," + most + "}", RegexOptions.Multiline).Replace(str, "");
            }

            str = inline_comments_cleaner.Replace(str, "");

            var st = block_comments_start.Match(str);
            if (st.Success)
            {
                st = block_comments_start_line.Match(str);
                if (st.Success)
                    str = block_comments_start_line.Replace(str, "", 1);
                else
                    str = block_comments_start.Replace(str, "", 1);

                var es = block_comments_end_line.Matches(str);

                if (0 < es.Count)
                    if (es[0].Success)
                        str = block_comments_end_line.Replace(str, "", 1, es.Last().Index);
                    else
                    {
                        es = block_comments_end.Matches(str);
                        if (es[0].Success)
                            str = block_comments_end.Replace(str, "", 1, es.Last().Index);
                    }

                str = cleanup_asterisk.Replace(str, "");
            }

            var tmp = cleanup_see_cref
                      .Replace(str, "")
                      .Trim('\n', '\r', '\t', ' ', '+', '-');
            if (tmp.Length == 0) return "";


            switch (str[str.Length - 1])
            {
                case '\r':
                case '\n':
                    return str;
            }

            return str + "\n";
        }

        private static Dictionary<int, int> len2count = new();

        public override string ToString() => _name;
        public string _name { get; set; }
        public string? _doc { get; set; }
        public string? _inline_doc { get; set; }
        public int idx = int.MaxValue; //place index

        public static string brush(string name)
        {
            if (name.Equals("_DefaultMaxLengthOf") || !is_prohibited(name)) return name;


            var new_name = name;

            for (var i = 0; i < name.Length; i++)
                if (char.IsLower(name[i]))
                {
                    new_name = new_name[..i] + char.ToUpper(new_name[i]) + new_name[(i + 1)..];
                    if (is_prohibited(new_name)) continue;

                    return new_name;
                }

            return name;
        }

        public int char_in_source_code = -1;

        public ProjectImpl project;

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


        public static bool equals(ISymbol? x, ISymbol? y) => SymbolEqualityComparer.Default.Equals(x, y);


        private static bool is_prohibited(string name)
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
                    "map" or "package" or "range" or "return" or "select" or "struct" or "switch" or "type" or
                    "var" or

                    // Reserved keywords or special cases across multiple languages
                    "arguments" or "eval" or "null" or "true" or "false" or "undefined" or "void" => true,
                _ => false
            };
        }

        public int line_in_src_code => symbol == null ?
                                           -1 :
                                           symbol!.Locations[0].GetLineSpan().StartLinePosition.Line + 1;

        public ISymbol? symbol;
    }

    public abstract class Entity : HasDocs
    {
        public Entity? origin;
        public static readonly Dictionary<ISymbol, Entity> entities = new(SymbolEqualityComparer.Default);

        public BaseTypeDeclarationSyntax? node;

        public Entity? parent_entity => symbol!.OriginalDefinition.ContainingType == null ?
                                            null :
                                            entities.GetValueOrDefault(symbol!.OriginalDefinition.ContainingType);

        public virtual ushort? _parent => parent_entity switch
        {
            ProjectImpl.HostImpl.PackImpl pack => (ushort)pack.idx,
            _ =>
                parent_entity == null || project == ProjectImpl.projects_root ?
                    null :
                    (ushort?)ProjectImpl.projects_root.constants_packs.Find(p => equals(p.symbol!, parent_entity.symbol!))?.idx, //the parent is fake pack
        };

        public ProjectImpl in_project
        {
            get
            {
                for (var e = this; ; e = e.parent_entity)
                    if (e is ProjectImpl project)
                        return project;
            }
        }


        public ProjectImpl.HostImpl? in_host
        {
            get
            {
                for (var e = this; e != null; e = e.parent_entity)
                    switch (e)
                    {
                        case ProjectImpl.HostImpl host: return host;
                        case ProjectImpl: return null;
                    }

                return null;
            }
        }


        public string full_path
        {
            get
            {
                if (this == ProjectImpl.projects_root) return "";
                if (this is ProjectImpl) return _name;

                return parent_entity == null ?
                           _name :
                           parent_entity.full_path + "." + _name;
            }
        }


        public INamedTypeSymbol? symbol;
        public SemanticModel model;


        public bool? _included;
        public virtual bool included => _included ?? false;


        //Identify and mark the scope of entities requiring re-initialization due to a detected cyclic dependency.
        private uint _inited = int.MaxValue;

        private static bool cyclic; //If a cyclic dependency is detected during initialization, re-initialization is required.
        private static uint inited_seed;
        private static uint fix_inited_seed;

        public static void start()
        {
            cyclic = false;
            fix_inited_seed = inited_seed;
        }

        public static bool restart()
        {
            if (!cyclic) return false;
            inited_seed = fix_inited_seed;
            cyclic = false;
            return true;
        }

        public bool inited => _inited < inited_seed;

        public void set_inited() => _inited = ++inited_seed;

        public IEnumerable<INamedTypeSymbol> this_modified => symbol == null ? //if this is the virtual Stage created for channels without a body
                                                                  Array.Empty<INamedTypeSymbol>() :
                                                                  symbol.Interfaces.Where(I => I.isModify());

        public virtual void Init(HashSet<object> once)
        {
            if (inited || !once.Add(this) && (cyclic = true)) return; //Ensure the entity is initialized only once and prevent re-entry caused by cyclic references.

            if (symbol != null)
                if (!Init_As_Modifier_Dispatch_Modifications_On_Targets(once))
                    Init_Collect_Modification(this, once);

            once.Remove(this);
            set_inited(); //Only from this point is the entity fully initialized
        }

        public bool modifier;

        public virtual bool Init_As_Modifier_Dispatch_Modifications_On_Targets(HashSet<object> once)
        {
            foreach (var m in this_modified)
            {
                modifier = true;
                Init_Collect_Modification(entities[m.TypeArguments[0]], once);
            }

            return modifier;
        }


        protected virtual void Init_Collect_Modification(Entity target, HashSet<object> once)
        {
            //   UP
            //   |
            //   V
            //  down

            foreach (var comment in node!.GetLeadingTrivia()
                                         .Select(t => t.GetStructure())
                                         .OfType<DocumentationCommentTriviaSyntax>())
                foreach (var see in comment.DescendantNodes()
                                           .OfType<XmlCrefAttributeSyntax>())
                    target.modify(model.GetSymbolInfo(see.Cref).Symbol!, see.Parent!.Parent!.DescendantNodes().FirstOrDefault(t => see.Span.End < t.Span.Start)?.ToString().Trim()[0] != '-', 0, once);

            //left -> right

            if (node!.BaseList == null) return;

            foreach (var item in node!.BaseList!.Types)
                modify_by_extends(item, true, (sym, sn, add, depth) => target.modify(sym, add, depth, once));
        }

        protected void modify_by_extends(SyntaxNode sn, bool add, Action<ISymbol, SyntaxNode, bool, uint> modify)
        {
            var str = sn.ToString();
            if (str.StartsWith("_<") ||          //list items
                !(add = !str.StartsWith("X<"))) //list to delete items
                foreach (var arg in sn.DescendantNodes().OfType<GenericNameSyntax>())
                    foreach (var t in arg.TypeArgumentList.Arguments)
                        modify_by_extends(t, add, modify);
            else if (!str.StartsWith("Modify<") &&
                     !str.StartsWith("ChannelFor<"))
                modify(model.GetTypeInfo(sn is SimpleBaseTypeSyntax sbt ?
                                             sbt.Type :
                                             sn).Type!, sn, add, str[0] == '@' ?
                                                                     1U :
                                                                     0);
        }

        //                  pack: pack self,
        //                  project/host: packs declared within the body
        //depth ==1 -> if(by_what) is
        //                  pack:packs declared within the body,
        //                  project/host: all transmittable packs recursively, including packs throughout the hierarchical structure
        public abstract void modify(ISymbol by_what, bool add, uint depth, HashSet<object> once);

        public Entity(ProjectImpl prj, CSharpCompilation? compilation, BaseTypeDeclarationSyntax? node) : base(prj, node == null ?
                                                                                                                        "" :
                                                                                                                        node.Identifier.ToString(), node)
        {
            if (compilation == null || node == null) return;
            this.node = node;
            model = compilation.GetSemanticModel(node.SyntaxTree);
            base.symbol = symbol = model.GetDeclaredSymbol(node)!;


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

                var m = HasDocs.uid.Match(t.ToString());
                if (!m.Success) continue;
                uid = m.Groups[1].Value.to_base256_value();
                break;
            }
        }

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

        public Entity(ProjectImpl project, BaseTypeDeclarationSyntax? node) : base(project, node?.Identifier.ToString() ?? "", node) { this.node = node; }
    }

    public static class Extensions
    {
        public static bool isChannelFor(this ISymbol? sym) => sym != null && sym.ToString()!.StartsWith("org.unirail.Meta.ChannelFor<");
        public static bool isMeta(this ISymbol? sym) => sym != null && sym.ToString()!.StartsWith("org.unirail.Meta");
        public static bool is_(this ISymbol? sym) => sym != null && sym.ToString()!.StartsWith("org.unirail.Meta._<");
        public static bool isX(this ISymbol? sym) => sym != null && sym.ToString()!.StartsWith("org.unirail.Meta.X<");
        public static bool isModify(this ISymbol? sym) => sym != null && sym.ToString()!.StartsWith("org.unirail.Meta.Modify<");
        public static bool isSet(this ISymbol? sym) => sym != null && sym.ToString()!.StartsWith("org.unirail.Meta.Set<");
        public static bool isMap(this ISymbol? sym) => sym != null && sym.ToString()!.StartsWith("org.unirail.Meta.Map<");

        public static ulong to_base256_value(this string str)
        {
            var ret = 0UL;
            for (var i = 0; i < str.Length; i++)
                ret |= (ulong)(str[i] - base256) << i * 8;

            return ret;
        }

        private const int base256 = 0xFF;

        public static string to_base256_chars(this ulong src)
        {
            var chars = new char[5];
            return new string(chars, 0, src.to_base256_chars(chars));
        }

        public static int to_base256_chars(this ulong src, char[] dst)
        {
            var i = 0;
            do dst[i++] = (char)((src & 0xFF) + base256);
            while (0 < (src >>= 8));

            return i;
        }


        public static void AddNew<T>(this List<T> dst, List<T> src) => src.ForEach(t =>
                                                                                   {
                                                                                       if (!dst.Contains(t)) dst.Add(t);
                                                                                   });
    }

    internal class UID_Impl : UID
    {
        public static UID? data;
        public static int data_bytes; //bytes allocated for Info

        public override void Received(SaveLayout_UID.Receiver via)
        {
            data = this;
            data_bytes += via.byte_;
            via.byte_ = via.byte_max; //pack received. preventing continue
        }

        public static void read_write_uid() //We need to map the imported projects scope unique ID (project.uid << 16 | entity.uid) to the current project scope unique ID.
        {
            data = null;
            data_bytes = 0;
            foreach (var same_uid in ProjectImpl.projects
                                                .GroupBy(p => p.uid)
                                                .Where(g => g.Count() > 1))
            {
                var chars = new char[10];

                AdHocAgent.exit($"Projects:\n{string.Join("\n", same_uid.Select(prj => prj.symbol!.ToString()))}\n have the same UID /*{new string(chars, 0, same_uid.Key.to_base256_chars(chars))}*/." +
                                //
                                $"Please manually change the project UID to /*{new string(chars, 0, (same_uid.Key + 1).to_base256_chars(chars))}*/.");
            }

            var update_info = !File.Exists(AdHocAgent.layout.Value); //need update info
            var buffer = new byte[1024];

            using var layout_file = new FileStream(AdHocAgent.layout.Value, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

            if (!update_info)
            {
                var reader = new SaveLayout_UID.Receiver();
                _Allocator.DEFAULT.new_AdHocProtocol_LayoutFile__UID = src => new UID_Impl(); //switch UID_Info allocator to Info

                var len = layout_file.Read(buffer, 0, buffer.Length);
                do
                {
                    var i = reader.Write(buffer, 0, len);
                    if (data != null) break;
                    data_bytes += i;
                }
                while (0 < (len = layout_file.Read(buffer, 0, buffer.Length)));
            }

            if (update_info = data == null) data = new UID_Impl();


            var new_uid = 0U;
            var root_project = ProjectImpl.projects[0];


            if (data!._projects == null) // Initialize _projects and assign UIDs sequentially
            {
                data._projects = new Dictionary<ulong, byte>(ProjectImpl.projects.Count);
                foreach (var prj in ProjectImpl.projects)
                    data._projects.Add(prj.uid, (byte)(prj.uid = new_uid++));

                update_info = true;
            }
            else
            {
                foreach (var prj in ProjectImpl.projects) // First loop: Update existing project UIDs or add with a default value
                    if (data._projects!.TryGetValue(prj.uid, out var uid))
                        prj.uid = uid;

                foreach (var prj in ProjectImpl.projects.Where(p => 0xFF < p.uid)) // Second loop: Assign new UIDs where necessary
                {
                    while (data._projects!.ContainsValue((byte)new_uid)) new_uid++; // Ensure no duplicate UIDs
                    data._projects!.Add(prj.uid, (byte)(prj.uid = new_uid++));       // Store new UID
                    update_info = true;
                }
            }

            //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@=================================== Unique Indexing the hosts diagram
            ushort host_path(ProjectImpl.HostImpl host) => (ushort)(host.project.uid << 8 | //1byte
                                                                    host.uid);              //1byte

            new_uid = 0U;
            if (data._hosts == null) // Initialize _hosts and assign UIDs sequentially
            {
                data._hosts = new Dictionary<ushort, byte>(root_project.hosts.Count);
                foreach (var h in root_project.hosts)
                    data._hosts.Add(host_path(h), (byte)(h.uid = new_uid++));

                update_info = true;
            }
            else
            {
                foreach (var h in root_project.hosts) // First loop to update existing hosts' UIDs
                    if (data._hosts.TryGetValue(host_path(h), out var uid))
                    {
                        h.uid = uid;
                        h.no_info = false;
                    }

                foreach (var h in root_project.hosts.Where(h => h.no_info)) // Second loop to assign new UIDs to hosts without one
                {
                    while (data._hosts.ContainsValue((byte)new_uid)) new_uid++; // Ensure no duplicate UIDs
                    data._hosts.Add(host_path(h), (byte)(h.uid = new_uid++));
                    update_info = true;
                }
            }


            //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@===================================  Unique Indexing the packs diagram
            uint pack_path(ProjectImpl.HostImpl.PackImpl pack) => (uint)((
                                                                             pack.is_virtual_for_host ? //add special to prevent clash
                                                                                 pack.uid + 1 :
                                                                                 0) << 24 | //1 byte
                                                                         pack.project.uid << 16 | //1byte
                                                                         pack.uid);               //2byte

            new_uid = 0U;
            if (data._packs == null) // Initialize _packs and assign UIDs sequentially
            {
                data._packs = new Dictionary<uint, ushort>(root_project.packs.Count);
                foreach (var p in root_project.packs)
                    data._packs.Add(pack_path(p), (byte)(p.uid = new_uid++));

                update_info = true;
            }
            else
            {
                foreach (var p in root_project.packs) // First loop to update existing packs' UIDs
                    if (data._packs.TryGetValue(pack_path(p), out var uid))
                    {
                        p.uid = uid;
                        p.no_info = false;
                    }

                foreach (var p in root_project.packs.Where(h => h.no_info)) // Second loop to assign new UIDs to packs without one
                {
                    while (data._packs.ContainsValue((byte)new_uid)) new_uid++; // Ensure no duplicate UIDs
                    data._packs.Add(pack_path(p), (byte)(p.uid = new_uid++));
                    update_info = true;
                }
            }


            //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ ===================================  Unique Indexing the channels-stages-branches diagram
            ushort channel_path(ProjectImpl.ChannelImpl ch) => (ushort)(ch.project.uid << 8 | //1byte
                                                                        ch.uid);              //1byte

            new_uid = 0U;
            if (data!._channels == null) // Initialize _channels and assign UIDs sequentially
            {
                data._channels = new Dictionary<ushort, byte>(root_project.channels.Count);
                foreach (var ch in root_project.channels)
                    data._channels.Add(channel_path(ch), (byte)(ch.uid = new_uid++));

                update_info = true;
            }
            else
            {
                foreach (var ch in root_project.channels) // First loop to update existing channels' UIDs
                    if (data._channels.TryGetValue(channel_path(ch), out var uid))
                    {
                        ch.uid = uid;
                        ch.no_info = false;
                    }

                foreach (var ch in root_project.channels.Where(ch => ch.no_info)) // Second loop to assign new UIDs to channels without one
                {
                    while (data._channels.ContainsValue((byte)new_uid)) new_uid++; // Ensure no duplicate UIDs
                    data._channels.Add(channel_path(ch), (byte)(ch.uid = new_uid++));
                    update_info = true;
                }
            }


            //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
            uint stage_path(ProjectImpl.ChannelImpl ch, ProjectImpl.ChannelImpl.StageImpl st) => (uint)(ch.uid << 24 |         //1byte
                                                                                                        st.project.uid << 16 | //1byte
                                                                                                        st.uid);               //2bytes

            new_uid = 0;
            if (data._stages == null) // Initialize _stages and assign UIDs sequentially
            {
                data._stages = new Dictionary<uint, ushort>(10);

                foreach (var ch in root_project.channels)
                    foreach (var st in ch.stages)
                        data._stages.Add(stage_path(ch, st), (ushort)(st.uid = new_uid++));
            }
            else
            {
                foreach (var ch in root_project.channels)
                    foreach (var st in ch.stages)
                        if (data._stages.TryGetValue(stage_path(ch, st), out var uid))
                        {
                            st.uid = uid;
                            st.no_info = false;
                        }


                foreach (var ch in root_project.channels)
                    foreach (var st in ch.stages.Where(st => st.no_info))
                    {
                        while (data._stages.ContainsValue((byte)new_uid)) new_uid++; // Ensure no duplicate UIDs
                        data._stages.Add(stage_path(ch, st), (ushort)(st.uid = new_uid++));
                        update_info = true;
                    }
            }

            //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
            ulong branch_path(ProjectImpl.ChannelImpl ch, ProjectImpl.ChannelImpl.StageImpl st, bool L, ProjectImpl.ChannelImpl.BranchImpl br) => (L ?
                                                                                                                                                       1UL << 41 :
                                                                                                                                                       0U) | //1bit
                                                                                                                                                  ch.uid << 32 | //1byte
                                                                                                                                                  st.uid << 24 | //2bytes
                                                                                                                                                  br.project.uid << 16 | //1byte
                                                                                                                                                  br.uid;                //2bytes

            bool check(ProjectImpl.ChannelImpl ch, ProjectImpl.ChannelImpl.StageImpl st, ProjectImpl.ChannelImpl.BranchImpl br)
            {
                if (br.packs.Count == 0)
                    AdHocAgent.exit($"In channel '{ch.symbol}', stage '{st.symbol}', the branch targeting '{br.goto_stage!.symbol}' has no associated packs. This is an error. Please fix the issue.");
                return true;
            }

            new_uid = 0;
            if (data._branches == null) // Initialize _branches and assign UIDs sequentially
            {
                data._branches = new Dictionary<ulong, ushort>(10);

                foreach (var ch in root_project.channels)
                    foreach (var st in ch.stages)
                        foreach (var br in st.branchesL.Where(br => check(ch, st, br)))
                            data._branches.Add(branch_path(ch, st, true, br), br.uid = (ushort)new_uid++);

                foreach (var ch in root_project.channels)
                    foreach (var st in ch.stages)
                        foreach (var br in st.branchesR.Where(br => check(ch, st, br)))
                            data._branches.Add(branch_path(ch, st, false, br), br.uid = (ushort)new_uid++);
            }
            else
            {
                foreach (var ch in root_project.channels)
                    foreach (var st in ch.stages)
                        foreach (var br in st.branchesL.Where(br => check(ch, st, br)))
                            if (data._branches.TryGetValue(branch_path(ch, st, true, br), out var uid))
                            {
                                br.uid = uid;
                                br.no_info = false;
                            }

                foreach (var ch in root_project.channels)
                    foreach (var st in ch.stages)
                        foreach (var br in st.branchesR.Where(br => check(ch, st, br)))
                            if (data._branches.TryGetValue(branch_path(ch, st, false, br), out var uid))
                            {
                                br.uid = uid;
                                br.no_info = false;
                            }

                foreach (var ch in root_project.channels)
                    foreach (var st in ch.stages)
                        foreach (var br in st.branchesL.Where(br => br.no_info))
                        {
                            while (data._branches.ContainsValue((byte)new_uid)) new_uid++; // Ensure no duplicate UIDs
                            data._branches.Add(branch_path(ch, st, true, br), br.uid = (ushort)new_uid++);
                            update_info = true;
                        }

                foreach (var ch in root_project.channels)
                    foreach (var st in ch.stages)
                        foreach (var br in st.branchesR.Where(br => br.no_info))
                        {
                            while (data._branches.ContainsValue((byte)new_uid)) new_uid++; // Ensure no duplicate UIDs
                            try { data._branches.Add(branch_path(ch, st, false, br), br.uid = (ushort)new_uid++); }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }

                            update_info = true;
                        }
            }


            if (!update_info) return;

            byte[]? preserve_bytes = null; //bytes needed to preserve
            if (data_bytes < layout_file.Length)
            {
                layout_file.Position = data_bytes;
                layout_file.Read(preserve_bytes = new byte[layout_file.Length - data_bytes]);
            }

            layout_file.Position = 0; //write info first

            var writer = new SaveLayout_UID.Transmitter();

            writer.subscribeOnNewBytesToTransmitArrive(src =>
                                                       {
                                                           for (var len = src.Read(buffer, 0, buffer.Length); 0 < len; len = src.Read(buffer, 0, buffer.Length))
                                                               layout_file!.Write(buffer, 0, len);
                                                       });
            writer.send(data);

            data_bytes = (int)layout_file.Position; //fix info size
            if (preserve_bytes != null) layout_file!.Write(preserve_bytes);
        }
    }
}