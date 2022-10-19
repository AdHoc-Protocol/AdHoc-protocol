using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using org.unirail.Agent;
using Project = org.unirail.Agent.Project;

// Microsoft.CodeAnalysis >>>> https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel?view=roslyn-dotnet-3.11.0
namespace org.unirail
{
    public class HasDocs
    {
        private static readonly Regex leading_spaces            = new(@"^\s+", RegexOptions.Multiline);
        private static readonly Regex inline_comments_cleaner   = new(@"^\s*/{2,}", RegexOptions.Multiline);
        private static readonly Regex block_comments_start      = new(@"/\*+", RegexOptions.Multiline);
        private static readonly Regex block_comments_start_line = new(@"/\*+\s*(\r\n|\r|\n)", RegexOptions.Multiline);
        private static readonly Regex block_comments_end        = new(@"\s*\*+/", RegexOptions.Multiline);
        private static readonly Regex block_comments_end_line   = new(@"(\r\n|\r|\n)\s*\*+/", RegexOptions.Multiline);
        private static readonly Regex cleanup_asterisk          = new(@"^\s*\*+", RegexOptions.Multiline);
        private static readonly Regex cleanup_see_cref          = new(@"<\s*see\s*cref .*>", RegexOptions.Multiline);

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
                    s   = s.Replace("\t", "    ");
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

        public override string  ToString()  => _name;
        public          string  _name       { get; set; }
        public          string? _doc        { get; set; }
        public          string? _inline_doc { get; set; }
        public          int     uid = int.MaxValue;


        public static string check(string name)
        {
            if (!is_prohibited(name)) return name;

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
            name  = name[(name.LastIndexOf('.') + 1)..];
            _name = check(name);

            char_in_source_code = node.GetLocation().SourceSpan.Start;


            var trivias = node.GetLeadingTrivia();

            var doc = trivias.Aggregate("", (current, trivia) => current + get_doc(trivia));

            if (project.packs_id_info_end == -1) project.packs_id_info_end = char_in_source_code;


            if (0 < (doc = doc.Trim('\r', '\n', '\t', ' ')).Length) _doc = doc + "\n";
        }

        public List<INamedTypeSymbol> add = new();

        public void add_from(INamedTypeSymbol symbol)
        {
            var src = symbol.BaseType == null || symbol.BaseType.Name.Equals("Object")
                          ? symbol.Interfaces
                          : symbol.Interfaces.Concat(new[] { symbol.BaseType });

            foreach (var Interface in src) //add `inhereted` items
                add.Add(extract(Interface));
        }

        public static INamedTypeSymbol extract(INamedTypeSymbol Interface) => Interface.ToString()!.StartsWith("org.unirail.Meta._<")
                                                                                  ? (INamedTypeSymbol)Interface.TypeArguments[0]
                                                                                  : Interface;

        public List<INamedTypeSymbol> del = new();

        public List<ISymbol> add_fld = new();
        public List<ISymbol> del_fld = new();


        private static bool is_prohibited(string name)
        {
            switch (name)
            {
                case "abstract":
                case "assert":
                case "become":
                case "bool":
                case "boolean":
                case "box":
                case "break":
                case "by":
                case "byte":
                case "case":
                case "cast":
                case "catch":
                case "char":
                case "char16_t":
                case "char32_t":
                case "checked":
                case "class":
                case "companion":
                case "const":
                case "const_cast":
                case "constexpr":
                case "constructor":
                case "continue":
                case "crate":
                case "crossinline":
                case "data":
                case "debugger":
                case "decimal":
                case "declare":
                case "decltype":
                case "default":
                case "delegate":
                case "delete":
                case "deprecated":
                case "dllexport":
                case "dllimport":
                case "do":
                case "double":
                case "dst":
                case "dyn":
                case "dynamic":
                case "dynamic_cast":
                case "each":
                case "else":
                case "enum":
                case "Error":
                case "eval":
                case "event":
                case "expect":
                case "explicit":
                case "export":
                case "extends":
                case "extern":
                case "external":
                case "false":
                case "field":
                case "file":
                case "final":
                case "finally":
                case "fixed":
                case "float":
                case "fn":
                case "for":
                case "foreach":
                case "friend":
                case "from":
                case "fun":
                case "function":
                case "gcnew":
                case "generic":
                case "get":
                case "goto":
                case "i128":
                case "i16":
                case "i32":
                case "i64":
                case "i8":
                case "if":
                case "implements":
                case "implicit":
                case "import":
                case "in":
                case "infix":
                case "init":
                case "inline":
                case "inner":
                case "instanceof":
                case "int":
                case "int16_t":
                case "int32_t":
                case "int64_t":
                case "int8_t":
                case "interface":
                case "interior":
                case "internal":
                case "is":
                case "lateinit":
                case "let":
                case "literal":
                case "lock":
                case "long":
                case "loop":
                case "macro":
                case "match":
                case "mod":
                case "module":
                case "move":
                case "mut":
                case "mutable":
                case "naked":
                case "namespace":
                case "native":
                case "new":
                case "noexcept":
                case "noinline":
                case "noreturn":
                case "nothrow":
                case "novtable":
                case "null":
                case "nullptr":
                case "number":
                case "object":
                case "only":
                case "open":
                case "operator":
                case "out":
                case "override":
                case "pack":
                case "package":
                case "param":
                case "params":
                case "priv":
                case "private":
                case "property":
                case "protected":
                case "ptr":
                case "pub":
                case "public":
                case "readonly":
                case "receiver":
                case "ref":
                case "register":
                case "reified":
                case "reinterpret_":
                case "reinterpret_cast":
                case "require":
                case "return":
                case "safecast":
                case "sbyte":
                case "sealed":
                case "selectany":
                case "Self":
                case "set":
                case "setparam":
                case "short":
                case "signed":
                case "sizeof":
                case "src":
                case "stackalloc":
                case "static":
                case "static_assert":
                case "static_cast":
                case "str":
                case "strictfp":
                case "string":
                case "struct":
                case "super":
                case "suspend":
                case "switch":
                case "symbol":
                case "synchronized":
                case "tailrec":
                case "template":
                case "this":
                case "thread":
                case "throw":
                case "throws":
                case "trait":
                case "transient":
                case "true":
                case "try":
                case "type":
                case "typealias":
                case "typedef":
                case "typeid":
                case "typename":
                case "typeof":
                case "u128":
                case "u16":
                case "u32":
                case "u64":
                case "u8":
                case "uint":
                case "uint16_t":
                case "uint32_t":
                case "uint64_t":
                case "ulong":
                case "unchecked":
                case "union":
                case "unsafe":
                case "unsigned":
                case "unsized":
                case "use":
                case "ushort":
                case "using":
                case "uuid":
                case "val":
                case "value":
                case "Value":
                case "vararg":
                case "virtual":
                case "void":
                case "volatile":
                case "wchar_t":
                case "where":
                case "while":
                case "with":
                case "yield":


                    return true;
            }

            return false;
        }
    }

    public class Entity : HasDocs
    {
        public BaseTypeDeclarationSyntax? node;

        public Entity? parent_entity => project.entities[symbol.OriginalDefinition.ContainingType];

        public ProjectImpl in_project
        {
            get
            {
                for (var e = this;; e = e.parent_entity)
                    if (e is ProjectImpl project)
                        return project;
            }
        }

        public ProjectImpl.HostImpl.PortImpl? in_port
        {
            get
            {
                for (var e = this;; e = e.parent_entity)
                    switch (e)
                    {
                        case ProjectImpl.HostImpl.PortImpl port: return port;
                        case ProjectImpl:                        return null;
                    }
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
                        case ProjectImpl:               return null;
                    }

                return null;
            }
        }


        public string full_path
        {
            get
            {
                if (this is ProjectImpl)
                    return this == project
                               ? ""
                               : symbol!.ToString() ?? "";

                var path = _name;
                for (var e = parent_entity;; path = e._name + "." + path, e = e.parent_entity)
                    if (e is ProjectImpl)
                        return (e == project //root project
                                    ? ""
                                    : e.symbol + "." //full path
                               ) + path;
            }
        }


        public INamedTypeSymbol? symbol;
        public SemanticModel     model;


        public         bool? _included;
        public virtual bool  included => _included ?? false;


        public Entity(ProjectImpl prj, CSharpCompilation compilation, BaseTypeDeclarationSyntax node) : base(prj, node.Identifier.ToString(), node)
        {
            this.node = node;
            model     = compilation.GetSemanticModel(node.SyntaxTree);
            symbol    = model.GetDeclaredSymbol(node)!;
            project.entities.Add(symbol, this);

            if (!_name.Equals(symbol.Name))
                AdHocAgent.LOG.Warning("The name of {entity} is prohibited and changed to {new_name}. Please correct the name manually", symbol, _name);
        }

        public Entity(ProjectImpl project, BaseTypeDeclarationSyntax? node) : base(project, node?.Identifier.ToString() ?? "", node) { this.node = node; }
    }


    public class ProjectImpl : Entity, Project
    {
        public readonly Dictionary<ISymbol, Entity> entities = new();

        public Dictionary<string, Type> types; //runtime reflection types

        public FieldInfo runtimeFieldInfo(ISymbol field) //runtime field info
        {
            var str = field.ToString();
            var i   = str.LastIndexOf(".", StringComparison.Ordinal);
            return types[str[..i]].GetField(str[(i + 1)..], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)!;
        }

        public int    packs_id_info_start = -1;
        public int    packs_id_info_end   = -1;
        public string file_path;

        private class Protocol_Description_Parser : CSharpSyntaxWalker
        {
            public readonly List<ProjectImpl> projects = new(); //all projects

            public HasDocs? HasDocs_instance;

            public int inline_doc_line;

            public  Dictionary<string, Type> types = new();
            private ProjectImpl              project;

            private readonly CSharpCompilation compilation;

            public Protocol_Description_Parser(CSharpCompilation compilation) : base(SyntaxWalkerDepth.StructuredTrivia) { this.compilation = compilation; }

            private string namespace_    = "";
            private string namespace_doc = "";


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

                if (symbol.OriginalDefinition.ContainingType == null) //top level C# interface - it's a project
                {
                    HasDocs_instance = project = new ProjectImpl(projects.Count == 0 //root project
                                                                     ? null
                                                                     : projects[0], compilation, node, namespace_);
                    projects.Add(project);

                    project.types = types;
                    if (!string.IsNullOrEmpty(namespace_doc))
                    {
                        project._doc  = namespace_doc + project._doc;
                        namespace_doc = "";
                    }
                }
                else
                {
                    var interfaces = symbol.Interfaces;

                    HasDocs_instance = interfaces.Length == 1 && interfaces[0].Name.Equals("Communication_Channel_Of")
                                           ? new ChannelImpl(project, compilation, node)        //channel
                                           : new HostImpl.PortImpl(project, compilation, node); //host port 
                }

                inline_doc_line = node.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
                base.VisitInterfaceDeclaration(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                var model      = compilation.GetSemanticModel(node.SyntaxTree);
                var symbol     = model.GetDeclaredSymbol(node)!;
                var interfaces = symbol.Interfaces;

                if (project.entities[symbol.ContainingType] is ProjectImpl && 0 < interfaces.Length && interfaces[0].Name.Equals("Host"))
                    HasDocs_instance = new HostImpl(project, compilation, node); //host
                else
                    HasDocs_instance = new HostImpl.PortImpl.PackImpl(project, compilation, node); //constants set

                inline_doc_line = node.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
                base.VisitStructDeclaration(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax clazz)
            {
                HasDocs_instance = new HostImpl.PortImpl.PackImpl(project, compilation, clazz);

                inline_doc_line = clazz.GetLocation().GetMappedLineSpan().StartLinePosition.Line;

                base.VisitClassDeclaration(clazz);
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax ENUM)
            {
                HasDocs_instance = new HostImpl.PortImpl.PackImpl(project, compilation, ENUM);
                inline_doc_line  = ENUM.GetLocation().GetMappedLineSpan().StartLinePosition.Line;

                base.VisitEnumDeclaration(ENUM);
            }


            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);

                foreach (var variable in node.Declaration.Variables) { HasDocs_instance = new HostImpl.PortImpl.PackImpl.FieldImpl(project, node, variable, model); }

                inline_doc_line = node.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
                base.VisitFieldDeclaration(node);
            }


            public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);

                HasDocs_instance = new HostImpl.PortImpl.PackImpl.FieldImpl(project, node, model);
                inline_doc_line  = node.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
                base.VisitEnumMemberDeclaration(node);
            }


            public override void VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.Kind() == SyntaxKind.SingleLineCommentTrivia)
                    if (HasDocs_instance != null && inline_doc_line == trivia.GetLocation().GetMappedLineSpan().StartLinePosition.Line)
                        HasDocs_instance._inline_doc += trivia.ToString().Trim('\r', '\n', '\t', ' ', '/');

                base.VisitTrivia(trivia);
            }


            public override void VisitXmlCrefAttribute(XmlCrefAttributeSyntax node)
            {
                var comment_line = (XmlEmptyElementSyntax)node.Parent!;

                var model = compilation.GetSemanticModel(node.Cref.SyntaxTree);
                var cref  = model.GetSymbolInfo(node.Cref).Symbol;

                if (cref == null)
                    AdHocAgent.exit($"In meta information `{comment_line}` the reference to `{node.Cref.ToString()}` on `{HasDocs_instance}` is unreachable. ");


                HasDocs_instance._doc = HasDocs_instance._doc?.Replace(comment_line!.Parent!.GetText().ToString(), "");


#region reading of the project saved packs id info
                if (node.SpanStart < project.packs_id_info_end)
                    switch (cref!.Kind)
                    {
                        case SymbolKind.NamedType:
                            if (project.packs_id_info_start == -1)
                            {
                                project.packs_id_info_start = comment_line.Parent.Span.Start - 3; //-3 cut `/**`
                                project.packs_id_info_end   = comment_line.Parent.Span.End;
                            }

                            var id = int.Parse(((XmlTextAttributeSyntax)comment_line.Attributes[1]).TextTokens.ToString());

                            project!.pack_id_info.Add((INamedTypeSymbol)cref, id);

                            break;
                        default:
                            AdHocAgent.exit($"Packs id info contains reference to unknown entity {node}");
                            break;
                    }
#endregion
                else
                {
                    var txt = comment_line!.Parent!.ToFullString()[(comment_line.FullSpan.End - comment_line!.Parent!.FullSpan.Start)..];


                    if (HasDocs_instance is HostImpl host) //language &  generate/skip implementation at current lang config
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
                        if (0 < lang)
                        {
#region read host language configuration
                            host._langs = host._langs | (Project.Host.Langs)lang; //register language config

                            host._default_impl_hash_equal = (0 < txt.Length //the first char after last `>`
                                                                 ? txt[0]
                                                                 : ' ') switch
                                                            {
                                                                '+' => host._default_impl_hash_equal | (uint)(lang << 16),    //pack implements in this lang
                                                                '-' => (uint)(host._default_impl_hash_equal & ~(lang << 16)), //pack abstract in this lang
                                                                _   => host._default_impl_hash_equal
                                                            };

                            host._default_impl_hash_equal = (1 < txt.Length //the second char after last `>
                                                                 ? txt[1]
                                                                 : ' ') switch
                                                            {
                                                                '+' => (byte)(host._default_impl_hash_equal | lang),  //implements pack's hash equals
                                                                '-' => (byte)(host._default_impl_hash_equal & ~lang), //abstract pack's hash equals
                                                                _   => host._default_impl_hash_equal
                                                            };
#endregion
                            goto END;
                        }

#region apply current language configuration (default_impl_INT) on the host entity
                        if (cref == null) AdHocAgent.exit($"`Reference to unknown entity {node.Cref} on {host} host configuration detected.");
                        switch (cref!.Kind)
                        {
                            case SymbolKind.Method: // ref to a field
                                host.field_impl.Add(cref, (Project.Host.Langs)(host._default_impl_hash_equal >> 16));
                                break;

                            case SymbolKind.Field: //ref to a const fields value source

                                AdHocAgent.exit($"Delete constant {cref} on {host}. Constants are always implemented.");
                                break;

                            case SymbolKind.NamedType: //set  host's  enclosing pack language configuration 

                                host._pack_impl_hash_equal_.Add(cref, host._default_impl_hash_equal); //fixing impl hash equals  config
                                break;

                            default:
                                AdHocAgent.exit($"Reference to unknown entity {node.Cref} on {host} host");
                                break;
                        }
#endregion
                        goto END;
                    } //  host scope lang config end


                    if (cref == null) AdHocAgent.exit($"`Reference to unknown entity {node.Parent} detected. Correct or delete it");

                    var del = 0 < txt.Length && txt[0] == '-'; //the first char after last `>` is `-`

#region alter port / pack configuration
                    switch (cref!.Kind)
                    {
                        case SymbolKind.Field:
                            switch (HasDocs_instance)
                            {
                                case HostImpl.PortImpl.PackImpl dst_pack: // add single field to the pack entity from others 

                                    if (cref is IFieldSymbol src_field && (src_field.IsStatic || src_field.IsConst))
                                    {
                                        AdHocAgent.LOG.Error("Field {src_field} is {static_const} but only instance field can be {add_del} to {dst_pack} pack.",
                                                             src_field,
                                                             src_field.IsConst
                                                                 ? "const"
                                                                 : "static",
                                                             del
                                                                 ? "del"
                                                                 : "add",
                                                             dst_pack);
                                        
                                        AdHocAgent.exit("Fix the problem and try again", -9);
                                    }

                                    (del
                                         ? dst_pack.del_fld
                                         : dst_pack.add_fld).Add(cref);
                                    break;
                                case HostImpl.PortImpl.PackImpl.FieldImpl dst: //const fields value source ( const proxy )

                                    if (dst.symbol is IFieldSymbol { IsConst: true })
                                        if (cref is IFieldSymbol src && (src.IsStatic || src.IsConst))
                                            dst!.substitute_value_from = src;
                                        else
                                            AdHocAgent.LOG.Warning("{src_field} should be a static od const. Skipped", cref);
                                    else
                                        AdHocAgent.LOG.Warning("{dst_field} should be a const. Skipped", dst.symbol);

                                    break;
                                default:
                                    AdHocAgent.LOG.Warning(" unrecognised usage pattern of {src_field} on {dst}, Skipped", cref, HasDocs_instance);
                                    break;
                            }

                            break;
                        case SymbolKind.NamedType:
                            switch (HasDocs_instance)
                            {
                                case HostImpl.PortImpl port: //add a pack or other port packs to the port
                                    (del
                                         ? port.del
                                         : port.add).Add((INamedTypeSymbol)cref);
                                    break;
                                case HostImpl.PortImpl.PackImpl add_fields_to_pack: //add fields from pack to the pack
                                    (del
                                         ? add_fields_to_pack.del
                                         : add_fields_to_pack.add).Add((INamedTypeSymbol)cref);
                                    break;
                                default:
                                    AdHocAgent.LOG.Warning("{Cref} cannot be apply to the {HasDocsInstance}  type only", cref, HasDocs_instance);
                                    break;
                            }

                            break;
                        default:
                            AdHocAgent.exit($"Reference to unknown entity {node.Cref} at {HasDocs_instance}");
                            break;
                    }
#endregion
                }

                END:
                base.VisitXmlCrefAttribute(node);
            }
        }

        public readonly Dictionary<INamedTypeSymbol, int> pack_id_info = new(); //saved in source file packs ids

        //calling only on root project
        //imported_projects_pack_id_info - pack_id_info from other included projects
        public ISet<HostImpl.PortImpl.PackImpl> packs_id_info_read_write_update(Dictionary<INamedTypeSymbol, int>? imported_projects_pack_id_info)
        {
            // _packs in host's ports contains transmittable packs, so this packs should have valid `id`

            HashSet<HostImpl.PortImpl.PackImpl> packs = new(); //packs collector

            foreach (var port in ports.Where(port => hosts[port._host].included)) //collect valid transmittable packs 
                packs.UnionWith(port.transmitted_packs);

#region check correct using empty packs
            foreach (var pack in project.transmittable_packs.Where(pack => pack.fields.Count == 0)) // empty(with no fields) packs
            {
                var used_as_fields_type = project.raw_fields.Values.Where(fld => fld.get_exT_pack == pack).ToArray(); //fields has empty packs as type
                var used                = 0 < used_as_fields_type.Length;

                foreach (var fld in used_as_fields_type) //change field type to boolean
                    fld.switch_to_boolean();

                if (packs.Contains(pack)) //transmittable
                {
                    if (used) AdHocAgent.LOG.Warning("Pack {Pack} is empty. As field datatype it\'s useless and will be replaced with boolean type", pack.symbol);
                    continue;
                }

                if (pack._static_fields_.Count == 0)
                {
                    if (used) AdHocAgent.LOG.Warning("Pack {Pack} is empty. As field datatype it\'s useless and will be replaced with boolean type and deleted", pack.symbol);
                    pack._id = 0; //mark to delete
                }
                else
                {
                    if (used) AdHocAgent.LOG.Warning("Pack {Pack} is empty. As field datatype it\'s useless and will be replaced with boolean type and used as constants set", pack.symbol);
                    pack._id = (ushort)Project.Host.Port.Pack.Field.DataType.t_constants; //switch use as constants set
                    project.constants_packs.Add(pack);
                }
            }
#endregion

            project.transmittable_packs.RemoveAll(pack => pack._id is 0 or (int)Project.Host.Port.Pack.Field.DataType.t_constants);

#region read/write packs id
            foreach (var pack in packs) //extract saved communication packs id  info 
            {
                if (!pack._name.Equals(pack.symbol!.Name))
                {
                    AdHocAgent.LOG.Error("The name of the pack {entity} was changed to {new_name} and pack cannot be assigned id before its name is corrected manually", pack.symbol, pack._name);
                    AdHocAgent.exit("", 66);
                }

                var key = pack.symbol;
                if (pack_id_info.ContainsKey(key!)) pack._id                        = (ushort)pack_id_info[key]; //root does not has id info... maybe imported projects have
                else if (imported_projects_pack_id_info!.ContainsKey(key)) pack._id = (ushort)pack_id_info[key];
            }

            if (new FileInfo(AdHocAgent.provided_path).IsReadOnly) //Protocol description file is locked - packs id updating process skipped.
                return packs;

#region detect pack's id duplication
            foreach (var pks in packs.Where(pk => pk._id < (int)Project.Host.Port.Pack.Field.DataType.t_subpack).GroupBy(pack => pack._id).Where(g => 1 < g.Count()))
            {
                var _1   = pks.First();
                var list = pks.Aggregate("", (current, pk) => current + pk.full_path + "\n");
                AdHocAgent.LOG.Warning("Packs \n{List} with equal id = {Id} detected. Will preserve one assignment, others will be renumbered", list, _1._id);

                //find a one to preserve it's id in root project first
                if (_1.project != project)
                {
                    var pk             = pks.FirstOrDefault(pk => pk.project == project);
                    if (pk != null) _1 = pk;
                }

                foreach (var pk in pks) //
                    if (pk != _1)
                        pk._id = (int)Project.Host.Port.Pack.Field.DataType.t_subpack; //reset for renumbering 
            }
#endregion

            // check that pack_id_info and `packs` have fully idenical packs
            var update_packs_id_info = pack_id_info.Count != packs.Count || !packs.All(pack => pack_id_info.ContainsKey(pack.symbol!)); //is need update packs_id_info in source file 

            ////////////                                   renumbering
            for (var id = 0;; id++) //set new packs id 
                if (packs.All(pack => pack._id != id))
                {
                    var pack = packs.FirstOrDefault(pack => pack._id == (int)Project.Host.Port.Pack.Field.DataType.t_subpack);
                    if (pack == null) break;     //no more pack without id
                    update_packs_id_info = true; //mark need to update packs_id_info in protocol description file 
                    pack._id             = (ushort)id;
                }


            if (!update_packs_id_info) return packs; //================================= update saved packs id info in protocol description file

            var long_full_path = (HostImpl.PortImpl.PackImpl pack) => pack.project == project
                                                                          ? pack.full_path
                                                                          : pack.symbol!.ToString(); //namespace + project_name + pack.full_path

            var text_max_width = packs.Select(p => long_full_path(p).Length).Max() + 4;
            var source_code    = node.SyntaxTree.ToString();

            if (packs_id_info_start == -1) packs_id_info_start = packs_id_info_end; //no saved packs id info in source file

            using StreamWriter file = new(AdHocAgent.provided_path);
            file.Write(source_code[..packs_id_info_start]);
            file.Write("/**\n");
            foreach (var pack in packs.OrderBy(pack => long_full_path(pack)))
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
            file.Write(source_code[packs_id_info_end..]);
            file.Flush();
            file.Close();
#endregion
            return packs;
        }

        string[] compiled_files       = Array.Empty<string>();
        int[]    compiled_files_times = Array.Empty<int>();

        public ProjectImpl? refresh() => compiled_files
                                         .Where((path, i) => new FileInfo(path).LastWriteTime.Millisecond != compiled_files_times[i]).Any()
                                             ? init()
                                             : null;


        public static ProjectImpl init()
        {
            var compiled_files       = new List<string>();
            var compiled_files_times = new List<int>();

            var trees = new[] { AdHocAgent.provided_path }
                        .Concat(AdHocAgent.provided_paths)
                        .Select(path =>
                                {
                                    compiled_files.Add(path);
                                    compiled_files_times.Add(new FileInfo(path).LastWriteTime.Millisecond);

                                    StreamReader file = new(path);
                                    var          src  = file.ReadToEnd();
                                    file.Close();
                                    return SyntaxFactory.ParseSyntaxTree(src, path: path);
                                }).ToArray();

            var compilation = CSharpCompilation.Create("Output",
                                                       trees,
                                                       ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)!
                                                       .Split(Path.PathSeparator)
                                                       .Select(path => MetadataReference.CreateFromFile(path)),
                                                       new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                                                    optimizationLevel: OptimizationLevel.Debug,
                                                                                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

            var parser = new Protocol_Description_Parser(compilation);

            try
            {
                using (var ms = new MemoryStream())
                {
                    compilation.Emit(ms);         // write IL code into memory
                    ms.Seek(0, SeekOrigin.Begin); // load this 'virtual' DLL so that we can use
                    var types = Assembly.Load(ms.ToArray()).GetTypes();
                    foreach (var type in types) parser.types.Add(type.ToString().Replace("+", "."), type);
                }
            }
            catch (Exception e)
            {
                AdHocAgent.LOG.Error("Source {ProvidedPath} compilation problem\n {E}", AdHocAgent.provided_path, e);
                throw;
            }

            foreach (var tree in trees) //parsing all projects
                parser.Visit(tree.GetRoot());

            if (parser.projects.Count == 0)
                AdHocAgent.exit($@"No any project detected. Provided file {AdHocAgent.provided_path} has not complete or wrong format. Try to start from init template.");
            var project = parser.projects[0]; //switch to root project        
            project._included            = true;
            project.compiled_files       = compiled_files.ToArray();
            project.compiled_files_times = compiled_files_times.ToArray();

#region merge everything into the root project
            foreach (var prj in parser.projects.Skip(1))
            {
                foreach (var kv in prj.entities) project.entities.Add(kv.Key, kv.Value);
                project.hosts.AddRange(prj.hosts);
                project.channels.AddRange(prj.channels);
                project.ports.AddRange(prj.ports);

                project.constants_packs.AddRange(prj.constants_packs);
                project.transmittable_packs.AddRange(prj.transmittable_packs);

                foreach (var field_info in prj.raw_fields) project.raw_fields.Add(field_info.Key, field_info.Value);
            }
#endregion


#region process project imports constants/ enum / values packs
            while (project.collect_imported_entities(new HashSet<ProjectImpl>())) ;

            project.constants_packs.AddRange(project.project_constants_packs);
            project.transmittable_packs.AddRange(project.project_value_packs);

            //all constants, enums packs in the root project are included by default
            foreach (var pack in project.constants_packs.Where(pack => pack.is_constants_set || pack.is_enum)) pack._included = true;

            //all  in the root project are included by default
            foreach (var pack in project.transmittable_packs.Where(pack => pack._value_type)) pack._included = true;
#endregion

#region collect, enumerate and check hosts
            {
                project.hosts = project.hosts.Where(host => host.included).Distinct().OrderBy(host => host.full_path).ToList();
                for (var i = 0; i < project.hosts.Count; i++) project.hosts[i].uid = i;
                var exit                                                           = false;
                foreach (var host in project.hosts.Where(host => host._langs == 0))
                {
                    exit = true;
                    AdHocAgent.LOG.Error("Host {host} has no language implementation information. Use <see cref = \'InLANG\'/> comments construction to add it.", host.symbol);
                }

                if (exit) AdHocAgent.exit("Correct detected problems and restart", 45);

                //expand pack scope from host to project
                //remove registered on project scope level packs, from reg on host's level scopes
                foreach (var host in project.hosts)
                    host.packs.RemoveAll(pack => project.project_constants_packs.Contains(pack) || project.project_value_packs.Contains(pack));
            }
#endregion

#region process project channels and include connected ports
            project.channels = project.channels.Where(ch => ch.included).Distinct().OrderBy(ch => ch._name).ToList();

            if (project.channels.Count == 0)
                AdHocAgent.exit("No any information about communication channels.", 45);

            for (var i = 0; i < project.channels.Count; i++)
            {
                var ch = project.channels[i];
                ch.uid = i;

                if (project.entities[ch.A.ContainingType] == project.entities[ch.B.ContainingType]) AdHocAgent.exit($"Channel {ch} should connect ports from different hosts only.");

                project.entities[ch.A]._included = true;
                project.entities[ch.B]._included = true;
            }
#endregion
            HostImpl.PortImpl.PackImpl.FieldImpl.init(project);
            HostImpl.PortImpl.PackImpl.init(project);
            HostImpl.PortImpl.init(project);

            //imported_pack_id_info  - collection of pack_id_info from imported projects
            //root project pack_id_info override  imported projects pack_id_info
            var imported_projects_pack_id_info = parser.projects.Skip(1).SelectMany(prj => prj.pack_id_info).ToDictionary(kv => kv.Key, kv => kv.Value);

            var packs = project.packs_id_info_read_write_update(imported_projects_pack_id_info);

            packs.UnionWith(project.transmittable_packs.Where(p => p.included)); //add included transmittable to the packs
            packs.UnionWith(project.constants_packs.Where(c => c.included));     //add included enums & constants sets to the packs

            //include packs that are not transmited but build a namespaces hierarchy
            foreach (var p in packs.ToArray())
                for (var pack = p; pack.parent_entity is HostImpl.PortImpl.PackImpl parent_pack; pack = parent_pack)
                    if (packs.Add(parent_pack))
                    {
                        parent_pack._id = (ushort)AdhocProtocol.Agent.Project.Host.Port.Pack.Field.DataType.t_constants; //make them totaly empty shell
                        parent_pack.fields.Clear();
                        parent_pack._static_fields_.Clear();
                    }


            foreach (var pack in packs.ToArray())
            {
                for (var p = pack;;)
                    if (p.parent_entity is HostImpl.PortImpl.PackImpl parent_pack) //run up to hierarchy
                    {
                        parent_pack._included = true;

                        if (!packs.Contains(parent_pack))
                        {
                            pack._id = (ushort)Project.Host.Port.Pack.Field.DataType.t_constants; //make true shell_pack
                            pack.fields.Clear();                                                  // true shell_pack contains only constants and exists only to support namespace hierarchical relationships only 
                            packs.Add(pack);
                        }

                        p = parent_pack;
                    }
                    else break;
            }

            project.packs = packs.OrderBy(pack => pack.full_path).ToList(); //save all used packs


#region detect useless pack language information
            project.hosts.ForEach(
                                  host =>
                                  {
                                      packs.Clear();
                                      foreach (var port in project.ports.Where(port => port.parent_entity == host))
                                      {
                                          packs.UnionWith(port.transmitted_packs);
                                          packs.UnionWith(port.related_packs);

                                          foreach (var ch in project.channels)
                                          {
                                              HostImpl.PortImpl _port;
                                              if (ch._portA      == port.uid) _port = project.ports[ch._portB];
                                              else if (ch._portB == port.uid) _port = project.ports[ch._portA];
                                              else continue;
                                              packs.UnionWith(_port.transmitted_packs);
                                              packs.UnionWith(_port.related_packs);
                                          }
                                      }
                                      //now in packs all host's transmitted and received packs

                                      //detect useless pack language information
                                      foreach (var symbol in host._pack_impl_hash_equal_.Keys.Where(symbol => !packs.Contains(project.entities[symbol])))
                                      {
                                          AdHocAgent.LOG.Warning("On host {Host} language configuration of pack {Symbol} will be ignore because this pack in not used in host\'s ports", host, symbol);
                                          host._pack_impl_hash_equal_.Remove(symbol); //Remove lang config of packs that is not used in host
                                      }

                                      //check enums usage, and to do precise distributions among hosts
                                      var host_used_enums = packs
                                                            .SelectMany(pack => pack.fields)
                                                            .Select(fld => fld.exT_pack)
                                                            .Where(exT => exT != null && exT.EnumUnderlyingType != null)
                                                            .Select(exT => (HostImpl.PortImpl.PackImpl)project.entities[exT]).Distinct();

                                      //import used enumd in user host scope
                                      host.packs.AddRange(host_used_enums);
                                      host.packs = host.packs.Distinct().ToList();
                                  });


            //now packs, that are present in every hosts scope, just make globaly available - move to the top most project scope
            if (project.hosts.All(host => 0 < host.packs.Count))
            {
                packs.Clear();
                packs.UnionWith(project.hosts[0].packs);
                project.hosts.ForEach(host => packs.IntersectWith(host.packs));

                //now in the `packs` only globaly used packs, move them on the top by deleting from narrow hosts scope
                project.hosts.ForEach(host => host.packs.RemoveAll(pack => packs.Contains(pack)));
            }

            //delete on host scope registered packs, if they already register globaly
            project.hosts.ForEach(host => host.packs.RemoveAll(pack => project.project_constants_packs.Contains(pack) ||
                                                                       project.project_value_packs.Contains(pack)));
#endregion


#region set packs uid(storage place index)  and collect all fields
            HashSet<HostImpl.PortImpl.PackImpl.FieldImpl> fields = new();
            for (var i = 0; i < project.packs.Count; i++)
            {
                var pack = project.packs[i];
                pack.uid = i; //set pack's uid

                foreach (var fld in pack.fields)
                    fields.Add(fld);

                foreach (var fld in pack._static_fields_)
                    fields.Add(fld);
            }
#endregion
            var has_parent = project.packs.Where(pack => pack._parent != null);

            bool repeat;
            do
            {
                try
                {
                    repeat = false;
                    foreach (var pack in has_parent)
                        if (pack._name!.Equals(pack.parent_entity._name))
                        {
                            var new_name = mangling(pack._name);
                            AdHocAgent.LOG.Warning("Pack `{PackSymbol}` is declared inside body of the parent pack{Name1} and has same as parent name . Some languages (Java) not allowed this.\n The name will be changed to `{NewName}`",
                                                   pack.symbol, pack.parent_entity._name, new_name);
                            pack._name = new_name;
                            repeat     = true; //name changed, this may bring new conflict, repeat chrck
                        }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            while (repeat);

            //for more predictable stable order
            string orderBy(HostImpl.PortImpl.PackImpl.FieldImpl fld) => project.packs.First(pack => pack.fields.Contains(fld) || pack._static_fields_.Contains(fld)).full_path + fld._name;

            project.fields = fields.OrderBy(fld => orderBy(fld)).ToArray();

            for (var i = 0; i < project.fields.Length; i++) project.fields[i].uid = i; //set fields  uid 

            var error = false;

#region update referred
            foreach (var pack in project.packs)
                pack._referred = pack.is_transmittable && project.fields.Any(fld => fld.get_exT_pack == pack || (fld.V != null && fld.V.get_exT_pack == pack));
#endregion

            if (error) AdHocAgent.exit("Please correct detected problems and repeat.", 44);
            return project;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////
        public static string mangling(string name)
        {
            for (var i = 0; i < name.Length; i++)
                if (char.IsLower(name[i])) { return name[..i] + char.ToUpper(name[i]) + name[(i + 1)..]; }

            return name;
        }

        readonly HashSet<HostImpl.PortImpl.PackImpl> project_constants_packs = new(); //project scope constants packs
        readonly HashSet<HostImpl.PortImpl.PackImpl> project_value_packs     = new(); //project scope value packs

        bool collect_imported_entities(HashSet<ProjectImpl> path) //return true if collected collection size changed
        {
            path.Add(this);

            var fix_constants_count = project_constants_packs.Count;
            var fix_valuess_count   = project_value_packs.Count;

            foreach (var symbol in project.add)
                if (!project.entities.ContainsKey(symbol))
                    AdHocAgent.LOG.Warning("A project can only import other projects/enums/constants/values packs but {Symbol} is not this type, its import will be skipped", symbol);
                else
                    switch (project.entities[symbol])
                    {
                        case ProjectImpl prj: //import all constants and value packs from

                            if (path.Contains(prj)) continue;

                            prj.collect_imported_entities(path);

                            project_constants_packs.UnionWith(prj.project_constants_packs);
                            project_value_packs.UnionWith(prj.project_value_packs);

                            continue;

                        case HostImpl.PortImpl.PackImpl pack:

                            if (pack.is_constants_set || pack.is_enum)
                            {
                                if (project_constants_packs.Add(pack) && !constants_packs.Contains(pack))
                                    constants_packs.Add(pack);
                            }
                            else if (pack._value_type)
                                if (project_value_packs.Add(pack) && !transmittable_packs.Contains(pack))
                                    transmittable_packs.Add(pack);

                            continue;
                    }

            path.Remove(this);
            return project_constants_packs.Count != fix_constants_count || project_value_packs.Count != fix_valuess_count;
        }


        //in root project - root_project == null but `project` field point to itself 
        public ProjectImpl(ProjectImpl? root_project, CSharpCompilation compilation, InterfaceDeclarationSyntax node, string namespace_) : base(null, compilation, node)
        {
            file_path = node.SyntaxTree.FilePath;
            if (root_project != null) //not root project 
                project = root_project;

            add_from(symbol!);

            _namespace_ = namespace_;
            this.node   = node;
        }

        public string? _task => AdHocAgent.task;

        public string? _namespace_ { get; set; }

        public long _time { get; set; }

        public List<HostImpl> hosts = new(3);

        public int     _hosts_Count => hosts.Count;
        public object? _hosts()     => hosts;

        public Project.Host? _hosts(Context.Provider ctx, int d) => hosts[d];


        //all fields - include virtual V field - used as Value of Map datatype 
        IEnumerable<HostImpl.PortImpl.PackImpl.FieldImpl?> all_fields() => raw_fields.Values.Concat(raw_fields.Values.Select(fld => fld.V).Where(fld => fld != null)).Distinct();

        public Dictionary<ISymbol, HostImpl.PortImpl.PackImpl.FieldImpl> raw_fields = new();


        public HostImpl.PortImpl.PackImpl.FieldImpl[] fields = Array.Empty<HostImpl.PortImpl.PackImpl.FieldImpl>();

        public object? _fields() => 0 < fields.Length
                                        ? fields
                                        : null;

        public int                           _fields_Count                        => fields.Length;
        public Project.Host.Port.Pack.Field? _fields(Context.Provider ctx, int d) => fields[d];


        public readonly List<HostImpl.PortImpl.PackImpl> transmittable_packs = new();
        public readonly List<HostImpl.PortImpl.PackImpl> constants_packs     = new(); //enums + constant sets


        public List<HostImpl.PortImpl.PackImpl> packs;

        public object? _packs() => 0 < packs.Count
                                       ? packs
                                       : null;

        public int                     _packs_Count                        => packs.Count;
        public Project.Host.Port.Pack? _packs(Context.Provider ctx, int d) => packs[d];

        public List<ChannelImpl> channels = new();

        public int _channels_Count => channels.Count;

        public object? _channels() => channels.Count < 1
                                          ? null
                                          : channels;

        public Project.Channel? _channels(Context.Provider ctx, int d) => channels[d];

        public List<HostImpl.PortImpl> ports = new();

        public int _ports_Count => ports.Count;

        public object? _ports() => ports.Count < 1
                                       ? null
                                       : ports;

        public Project.Host.Port? _ports(Context.Provider ctx, int d) => ports[d];

        void Communication.Transmittable.Sent(Communication via)
        {
            new FileInfo(AdHocAgent.provided_path).IsReadOnly = true;                        //+ delete old files - mark:  the file was sent  
            var result_output_folder = Path.Combine(AdHocAgent.destination_dir_path, _name); // destination_dir_path/project_name
            if (Directory.Exists(result_output_folder))
                Directory.Delete(result_output_folder, true);
        }


        public class HostImpl : Entity, Project.Host
        {
            public Project.Host.Langs _langs { get; set; }

            public override bool included => _included ?? in_project.included;

            public HostImpl(ProjectImpl project, CSharpCompilation compilation, StructDeclarationSyntax host) : base(project, compilation, host)
            {
                _default_impl_hash_equal = 0xFFFF_FFFF; //generate hash equals methods implementation by default, a bit per language
                project.hosts.Add(this);
            }

#region pack_impl_hash_equal
            public Dictionary<ISymbol, uint> _pack_impl_hash_equal_ = new();

            public object? _pack_impl_hash_equal() => _pack_impl_hash_equal_.Count == 0
                                                          ? null
                                                          : _pack_impl_hash_equal_;

            private Dictionary<ISymbol, uint>.Enumerator en;
            public  int                                  _pack_impl_hash_equal_Count()                    => _pack_impl_hash_equal_.Count;
            public  void                                 _pack_impl_hash_equal_Init(Context.Provider ctx) => en = _pack_impl_hash_equal_.GetEnumerator();

            public ushort _pack_impl_hash_equal_Next_K(Context.Provider ctx)
            {
                en.MoveNext();
                return (ushort)project.entities[en.Current.Key].uid;
            }

            public uint _pack_impl_hash_equal_V(Context.Provider ctx) => en.Current.Value;
            public uint _default_impl_hash_equal                      { get; set; } //by default a bit per language
#endregion


#region field_impl
            public Dictionary<ISymbol, Project.Host.Langs> field_impl = new();

            public object? _field_impl() => field_impl.Count == 0
                                                ? null
                                                : field_impl;

            private Dictionary<ISymbol, Project.Host.Langs>.Enumerator en2;
            public  int                                                _field_impl_Count()                    => field_impl.Count;
            public  void                                               _field_impl_Init(Context.Provider ctx) => en2 = field_impl.GetEnumerator();

            public ushort _field_impl_Next_K(Context.Provider ctx)
            {
                en2.MoveNext();
                return (ushort)project.raw_fields[en2.Current.Key].uid;
            }

            public Project.Host.Langs _field_impl_V(Context.Provider ctx) => en2.Current.Value;
#endregion

            public List<PortImpl.PackImpl> packs = new(); //host dedicated  packs 


            public object? _packs() => 0 < packs.Count
                                           ? packs
                                           : null;

            public int    _packs_Count                           => packs.Count;
            public ushort _packs(Context.Provider ctx, int item) => (ushort)packs[item].uid;

            public class PortImpl : Entity, Project.Host.Port
            {
                public ushort _host => (ushort)parent_entity.uid;


                internal PortImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax host_port) : base(project, compilation, host_port)
                {
                    if (parent_entity is not HostImpl) AdHocAgent.exit($"A host port {symbol} definition should be inside a host declaration.");
                    add_from(symbol!);
                    project.ports.Add(this);
                }

                public List<PackImpl> transmitted_packs = new(); //transmittable packs

                public object? _transmitted_packs() => 0 < transmitted_packs.Count
                                                           ? transmitted_packs
                                                           : null;

                public int    _transmitted_packs_Count                           => transmitted_packs.Count;
                public ushort _transmitted_packs(Context.Provider ctx, int item) => (ushort)transmitted_packs[item].uid;


                public List<PackImpl> related_packs = new(); //non-transmittable directly sub packs

                public object? _related_packs() => 0 < related_packs.Count
                                                       ? related_packs
                                                       : null;

                public int    _related_packs_Count                           => related_packs.Count;
                public ushort _related_packs(Context.Provider ctx, int item) => (ushort)related_packs[item].uid;

                public static void init(ProjectImpl project)
                {
                    project.ports = project.ports.Where(port => port.included).Distinct().OrderBy(port => port._name).ToList(); //cleanup from  duplicated and excluded
                    for (var p = 0; p < project.ports.Count; p++) project.ports[p].uid = p;                                     //write store places


                    HashSet<PortImpl> port_path     = new();
                    HashSet<PackImpl> related_packs = new();

                    foreach (var port in project.ports)
                    {
                        port.collect_packs(port_path, port.transmitted_packs);

                        related_packs.Clear();
                        port.transmitted_packs.ForEach(pack => pack.related_packs(related_packs));
                        port.transmitted_packs.ForEach(pack => related_packs.Remove(pack)); //subpacks purification 


                        port.related_packs = related_packs.Where(pack =>
                                                                 {
                                                                     pack._included = true;        //mark
                                                                     return pack.is_transmittable; //exclude none transmittable enums and constants - they are going to every hosts scope
                                                                 }).ToList();
                    }

                    //validate ports.
                    //if an port._packs.Count == 0 it is OK -> port can receive packs only,
                    //but if booth, connected with channel ports are empty, this is not OK
                    foreach (var ch in project.channels.Where(ch => ((PortImpl)project.entities[ch.A]).transmitted_packs.Count == 0 && ((PortImpl)project.entities[ch.B]).transmitted_packs.Count == 0))
                        AdHocAgent.exit($"Channel {ch.full_path} connect two empty communication ports - it useless.");
                }

                private void collect_packs(ISet<PortImpl> path, List<PackImpl> dst)
                {
                    var packs_declared_in_the_port_body = project.transmittable_packs.Where(pack => pack.in_port == this).Select(pack => pack.symbol).ToArray();

                    if (0 < packs_declared_in_the_port_body.Length)
                        new PackImpl(project, symbol, parent_entity._name + _name); // fake empty `constants pack` that name = host.name +  port.name, Its provide parent-children path for packs declared inside


                    var symbols_of_port_transmits_packs = add                                      //packs added by means of C# inheritance
                                                          .Concat(packs_declared_in_the_port_body) // + packs declared in the port body
                                                          .Distinct();

                    foreach (var sym in symbols_of_port_transmits_packs)
                        switch (project.entities[sym!])
                        {
                            case PortImpl port_add_packs_from: // add all packs from a port
                                path.Add(this);

                                if (!path.Contains(port_add_packs_from)) port_add_packs_from.collect_packs(path, dst);

                                path.Remove(this);

                                continue;

                            case PackImpl add_a_pack:                                  //add a pack to port
                                if (add_a_pack.is_enum || add_a_pack.is_constants_set) //explicit import the constant in the port's parent host scope
                                {
                                    add_a_pack._included = true;
                                    ((HostImpl)parent_entity!).packs.Add(add_a_pack);
                                }
                                else dst.Add(add_a_pack);

                                continue;
                        }

                    //delete individual packs
                    foreach (var sym in del)
                        if (project.entities[sym!] is PackImpl del_a_pack)
                            dst.Remove(del_a_pack);
                        else
                            AdHocAgent.LOG.Warning("{Sym} delete from port {Symbol} ignored because supported delete only individual pack", sym, symbol);

                    del.Clear();
                }

                public class PackImpl : Entity, Project.Host.Port.Pack
                {
                    public ushort _id         { get; set; }
                    public bool   _value_type { get; set; }

                    public virtual ushort? _parent => parent_entity switch
                                                      {
                                                          PackImpl pack => (ushort)pack.uid,
                                                          PortImpl port => (ushort?)project.constants_packs.Find(pack => pack.symbol == port.symbol)?.uid, //the parent is fake "port" pack 
                                                          _             => null
                                                      };

                    public ushort? _nested_max { get; set; }

                    public bool? _referred { get; set; }

                    public List<FieldImpl> fields = new();

                    public object? _fields() => 0 < fields.Count
                                                    ? fields
                                                    : null;

                    public int _fields_Count                           => fields.Count;
                    public int _fields(Context.Provider ctx, int item) => fields[item].uid;

                    public List<FieldImpl> _static_fields_ = new();

                    public object? _static_fields() => 0 < _static_fields_.Count
                                                           ? _static_fields_
                                                           : null;

                    public int _static_fields_Count                           => _static_fields_.Count;
                    public int _static_fields(Context.Provider ctx, int item) => _static_fields_[item].uid;


                    //the pack is included if it is explicitly included or if it is isEnum or Constants_set it's project included

                    // ============================

                    private EnumDeclarationSyntax? enum_node;
                    public  bool                   is_calculate_enum_type => enum_node!.BaseList == null; //user does not explicitly assign enum type (int by default)

                    public bool is_enum          => enum_node != null;
                    public bool is_constants_set => _id       == (int)Project.Host.Port.Pack.Field.DataType.t_constants;
                    public bool is_transmittable => !is_enum && !is_constants_set;


#region enum
                    public PackImpl(ProjectImpl project, CSharpCompilation compilation, EnumDeclarationSyntax ENUM) : base(project, compilation, ENUM)
                    {
                        enum_node   = ENUM;
                        _value_type = false;

                        _id = (ushort)(symbol.GetAttributes().Any(a => a.AttributeClass!.ToString()!.Equals("System.FlagsAttribute")) //enum type
                                           ? Project.Host.Port.Pack.Field.DataType.t_flags
                                           : Project.Host.Port.Pack.Field.DataType.t_enum_sw); //probably

                        project.constants_packs.Add(this); //enums register
                        in_host?.packs.Add(this);          //register enum on host level scope. else stays in the project scope
                    }
#endregion

#region struct based constants set
                    public PackImpl(ProjectImpl project, CSharpCompilation compilation, StructDeclarationSyntax constants_set) : base(project, compilation, constants_set)
                    {
                        _value_type = false;
                        _id         = (ushort)Project.Host.Port.Pack.Field.DataType.t_constants; //constants set
                        project.constants_packs.Add(this);                                       // register constants set
                        in_host?.packs.Add(this);                                                //register on host level scope. else stays in the project scope
                    }
#endregion


#region class based pack
                    public PackImpl(ProjectImpl project, CSharpCompilation compilation, ClassDeclarationSyntax pack) : base(project, compilation, pack)
                    {
                        _id = (int)Project.Host.Port.Pack.Field.DataType.t_subpack; //by default subpack type
                        add_from(symbol!);
                        project.transmittable_packs.Add(this);
                    }
#endregion


                    public PackImpl(ProjectImpl project, INamedTypeSymbol? symbol, string name) : base(project, null) // fake empty constants pack from port to provide parent-children path for packs declared inside
                    {
                        _name       = name; //for name in path only
                        this.symbol = symbol;
                        _id         = (int)Project.Host.Port.Pack.Field.DataType.t_constants; // fake constants pack
                        _included   = true;
                        project.constants_packs.Add(this); // register
                    }

                    internal void related_packs(ISet<PackImpl> dst) //collect subpacks into dst ALSO incude ENUMS
                    {
                        foreach (var pack in fields.Select(fld => fld.get_exT_pack).Concat(fields.Select(fld => fld.V?.get_exT_pack)).Where(pack => pack != null))
                            if (dst.Add(pack!))
                                pack.related_packs(dst);
                    }

                    internal static void init(ProjectImpl project)
                    {
#region set packs inheritance_depth , nested_max and verify Value packs
                        HashSet<PackImpl> path = new();

                        project.transmittable_packs.ForEach(pack =>
                                                            {
                                                                path.Clear();
                                                                pack.inheritance_depth = pack.calculate_inheritance_depth(path, 0);
                                                            });

                        foreach (var pack in project.transmittable_packs.OrderBy(pack => pack.inheritance_depth)) //packs without inheretace go first
                        {
                            path.Clear();
                            pack.collect_fields(path);

                            path.Clear();
                            pack._nested_max = (ushort)pack.calculate_fields_type_depth(path, 0);
                        }
#endregion

#region detect and register Value packs
                        foreach (var pack in project.transmittable_packs.Where(pack => 1 < pack.fields.Count))
                        {
                            var bits = 0;
                            pack._value_type = pack.fields.All(fld =>
                                                               {
                                                                   if (fld.inT is null or <= (int)Project.Host.Port.Pack.Field.DataType.t_string || 0 < fld._dims_.Length) return false;


                                                                   return (bits += fld.bits ?? fld._value_bytes!.Value * 8 + (fld.nullable
                                                                                                                                  ? 1
                                                                                                                                  : 0)) < 65;
                                                               }
                                                              );
                            if (pack._value_type) pack.in_host?.packs.Add(pack); //register on host level scope(if declare inside). else stay in project scope
                        }
#endregion


#region process enums
                        var all_fields = project.all_fields().ToList();
                        foreach (var enum_ in project.constants_packs.Where(e => e.is_enum))
                        {
                            if (!enum_.included && enum_.in_project.included) enum_._included = true;

                            foreach (var dst in enum_._static_fields_.Where(fld => fld.substitute_value_from != null)) //substitute value
                            {
                                var src = project.raw_fields[dst.substitute_value_from!];
                                dst._value_int = src._value_int;
                            }

                            switch (enum_._id) //auto-numbering
                            {
                                case (int)Project.Host.Port.Pack.Field.DataType.t_flags:

                                    FieldImpl fi;

                                    for (var auto_val = 1UL;
                                         (fi = enum_._static_fields_!.FirstOrDefault(f => f._value_int == null)!) != null;
                                         fi._value_int = (long?)auto_val,
                                         auto_val <<= 1
                                        )
                                        while (enum_._static_fields_.Exists(f => f._value_int != null && (ulong)f._value_int == auto_val))
                                            auto_val <<= 1;


                                    break;
                                case (int)Project.Host.Port.Pack.Field.DataType.t_enum_sw:

                                    var has_value = enum_._static_fields_.Where(f => f._value_int != null).OrderBy(f => f._value_int);

                                    var i = 0L;

                                    if (has_value.Any())
                                    {
                                        var mIn = i = (long)has_value.First()._value_int;

                                        foreach (var fld in has_value)
                                        {
                                            while (1 < fld._value_int - i)
                                            {
                                                if ((fi = enum_._static_fields_!.FirstOrDefault(f => f._value_int == null)!) == null) break;
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

                                    enum_._id = (int)Project.Host.Port.Pack.Field.DataType.t_enum_exp;

                                    break;
                            }

                            next:

                            var fld_0 = enum_._static_fields_[0]; //first enum field

                            var min = enum_._static_fields_.Count == 0 //enums without fields are like typed boolean can be T or null
                                          ? 0
                                          : enum_._static_fields_.Min(f => f._value_int)!.Value;

                            var max = enum_._static_fields_.Count == 0 //enums without fields are like typed boolean  can be T or null
                                          ? 0
                                          : enum_._static_fields_.Max(f => f._value_int)!.Value;


                            if (enum_._id == (int)Project.Host.Port.Pack.Field.DataType.t_flags) // the enum is flag
                                max = enum_._static_fields_.Aggregate(0L, (i, fld) => i | fld._value_int!.Value);

                            if (enum_.is_calculate_enum_type)    //user does not explicitly assign enum type (int by default)
                                fld_0.set_EXT_ByRange(min, max); // calculate enum type, by estimate max and min values and apply on zero field

                            if (enum_._id == (int)Project.Host.Port.Pack.Field.DataType.t_enum_sw)
                                fld_0.set_INT_ByRange(0, enum_.fields.Count);
                            else
                                fld_0.set_INT_ByRange(min, max);

                            fld_0._min_value = min; //store calculated min/max on the first enum field
                            fld_0._max_value = max;


#region propagate enum params to fields where it used
                            var this_enum_used_fields = all_fields.Where(fld => enum_.symbol.Equals(fld?.exT_pack)).ToArray();


                            if (max == min && 0 < this_enum_used_fields.Length) // enums with less the one fields or if all fields have same value are like typed boolean can be T or null
                            {
                                enum_._id = (ushort)Project.Host.Port.Pack.Field.DataType.t_subpack; //mark on delete
                                var problem = enum_._static_fields_.Count == 0
                                                  ? " no field"
                                                  : enum_._static_fields_.Count == 1
                                                      ? " only one field"
                                                      : " fields with same values";

                                AdHocAgent.LOG.Warning("Enum {EnumSymbol} has {Problem}. As field data type it\'s useless and will be replaced with boolean", enum_.symbol, problem);

                                foreach (var fld in this_enum_used_fields)
                                    fld.switch_to_boolean();

                                continue;
                            }

#region detect nullable fields used this bits-range enum and make room for null value
                            if (fld_0.bits != null && this_enum_used_fields.Any(fld => fld.nullable))
                            {
                                var null_value             = long.MaxValue; //search space for null_val
                                var bits_if_field_nullable = 0;
                                switch (enum_._id)
                                {
                                    //no space for null_value inside enum field's value range
                                    case (int)Project.Host.Port.Pack.Field.DataType.t_enum_exp:

                                        null_value = max - min + 1; //set bits_null_value
                                        break;

                                    case (int)Project.Host.Port.Pack.Field.DataType.t_flags:

                                        if (enum_._static_fields_.Any(fld => fld._value_int == 0)) //search place for null_value
                                        {
                                            null_value = 1;
                                            while (max == (max | null_value)) null_value <<= 1;
                                        }
                                        else null_value = 0;

                                        break;
                                    default:
                                        foreach (var fld in enum_._static_fields_.OrderBy(fld => fld._value_int))
                                            if (null_value < fld._value_int)
                                            {
                                                null_value -= min;
                                                break;
                                            }
                                            else null_value = fld._value_int!.Value + 1;

                                        break;
                                }

                                bits_if_field_nullable = 32 - BitOperations.LeadingZeroCount((uint)null_value); //values range <127 ?

                                foreach (var fld in this_enum_used_fields) //=================================================== propagate
                                {
                                    fld.inT        = fld_0.inT;
                                    fld._min_value = min; //acceptable range
                                    fld._max_value = max; //acceptable range

                                    if (fld.nullable)
                                    {
                                        if (7 < bits_if_field_nullable) continue; //not bits field anymore

                                        fld._null_value = (byte?)null_value;
                                        fld.bits        = (byte?)bits_if_field_nullable;
                                    }
                                    else fld.bits = fld_0.bits;
                                }

                                continue; //============>>> to next enum_
                            }
#endregion
                            //rest of fields
                            foreach (var fld in this_enum_used_fields) //=================================================== propagate
                            {
                                fld.inT          = fld_0.inT;
                                fld._min_value   = min; //acceptable range
                                fld._max_value   = max; //acceptable range
                                fld.bits         = fld_0.bits;
                                fld._value_bytes = fld_0._value_bytes;
                            }
#endregion
                        }

                        project.constants_packs.RemoveAll(enum_ => enum_._id == (ushort)Project.Host.Port.Pack.Field.DataType.t_subpack); // remove marked to delete enums
#endregion
                    }


                    private int inheritance_depth;

                    private int calculate_inheritance_depth(ISet<PackImpl> path, int depth)
                    {
                        foreach (var sym in add)
                        {
                            try
                            {
                                var pack = (PackImpl)project.entities[sym];
                                if (!path.Add(pack)) continue;
                                depth = pack.calculate_inheritance_depth(path, Math.Max(path.Count, depth));
                                path.Remove(pack);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }
                        }

                        return depth;
                    }

                    private int calculate_fields_type_depth(ISet<PackImpl> path, int depth)
                    {
                        foreach (var fld in fields.Where(f => f.exT_pack != null))
                        {
                            var datatype = (PackImpl)project.entities[fld.exT_pack!];
                            if (!path.Add(datatype)) continue;
                            depth = datatype.calculate_fields_type_depth(path, Math.Max(path.Count, depth));
                            path.Remove(datatype);
                        }

                        return depth;
                    }


                    private List<FieldImpl> collect_fields(ISet<PackImpl> path)
                    {
                        //add fields from inherited and added packs  
                        var add_packs = add.Select(sym => (PackImpl)project.entities[sym]).Where(pack => pack != this && !path.Contains(pack));

                        if (add_packs.Any())
                        {
                            path.Add(this);

                            foreach (var pack in add_packs)
                                foreach (var fld in pack.collect_fields(path).Where(fld => !exists(fld._name!)))
                                    fields.Add(fld);

                            path.Remove(this);
                        }

                        add.Clear();

                        //add individual fields   
                        foreach (var fld in add_fld.Select(sym => project.raw_fields[sym]).Where(fld => !exists(fld._name!)))
                            fields.Add(fld);

                        add_fld.Clear();


                        //del individual fields   
                        foreach (var fld in del_fld.Select(sym => project.raw_fields[sym]))
                            fields.Remove(fld);

                        del_fld.Clear();

                        return fields;
                    }


                    public bool exists(string fld_name) => fields.Any(fld => fld._name!.Equals(fld_name)) || _static_fields_.Any(fld => fld._name!.Equals(fld_name));

                    public override string ToString() => _name +
                                                         _id switch
                                                         {
                                                             (int)Project.Host.Port.Pack.Field.DataType.t_enum_exp  => " : enum_exp",
                                                             (int)Project.Host.Port.Pack.Field.DataType.t_enum_sw   => " : enum_sw",
                                                             (int)Project.Host.Port.Pack.Field.DataType.t_flags     => " : enum_flags",
                                                             (int)Project.Host.Port.Pack.Field.DataType.t_constants => " : const_set",
                                                             (int)Project.Host.Port.Pack.Field.DataType.t_subpack   => " : subpack",
                                                             < (int)Project.Host.Port.Pack.Field.DataType.t_subpack => $" : {(_value_type ? "value" : "")} pack {_id} ",
                                                             _                                                      => "???"
                                                         };

                    public class FieldImpl : HasDocs, Project.Host.Port.Pack.Field
                    {
                        public Entity parent_entity => project.entities[symbol.OriginalDefinition.ContainingType];

                        public ISymbol? substitute_value_from;

                        public readonly  ISymbol?                 symbol;
                        private readonly SemanticModel            model;
                        public readonly  MemberDeclarationSyntax? fld_node;

                        private FieldImpl(ProjectImpl project, MemberDeclarationSyntax? node, SemanticModel? model) : base(project, "", null) //virtual field used to hold information of V in Map(K,V)
                        {
                            fld_node   = node;
                            this.model = model;
                        }


                        public readonly bool is_const;

                        public FieldImpl(ProjectImpl project, EnumMemberDeclarationSyntax node, SemanticModel model) : base(project, node.Identifier.ToString(), node) //enum field
                        {
                            this.model = model;
                            symbol     = model.GetDeclaredSymbol(node)!;

                            if (project.entities[symbol!.ContainingType] is PackImpl pack) pack._static_fields_.Add(this);
                            else AdHocAgent.exit($"`{project.entities[symbol!.ContainingType].full_path}` cannot contains any fields. Delete `{_name}`.");

                            project.raw_fields.Add(symbol, this);

                            var user_assigned_value = node.EqualsValue == null
                                                          ? null
                                                          : project.runtimeFieldInfo(symbol).GetRawConstantValue();
                            init_exT(symbol.ContainingType.EnumUnderlyingType!, user_assigned_value);
                            is_const = true;
                            if (!_name.Equals(symbol.Name))
                                AdHocAgent.LOG.Warning("The name of {entity} is prohibited and changed to {new_name}. Please correct the name manually", symbol, _name);
                        }

                        public FieldImpl(ProjectImpl project, FieldDeclarationSyntax node, VariableDeclaratorSyntax variable, SemanticModel model) //pack fields
                            : base(project, model.GetDeclaredSymbol(variable)!.Name, node)
                        {
                            this.model = model;
                            symbol     = model.GetDeclaredSymbol(variable)!;

                            fld_node = node;
                            var T = model.GetTypeInfo(node!.Declaration.Type).Type!;
                            project.raw_fields.Add(symbol, this);

                            if (symbol is IFieldSymbol fld && (is_const = fld.IsStatic || fld.IsConst)) //  static/const field
                            {
                                if (project.entities[symbol!.ContainingType] is PackImpl pack) pack._static_fields_.Add(this);
                                else AdHocAgent.exit($"`{project.entities[symbol!.ContainingType].full_path}` cannot contains any fields. Delete `{_name}`.");

                                var constant = project.runtimeFieldInfo(symbol).GetValue(null); // runtime constant value

                                if (T.Kind == SymbolKind.ArrayType)
                                {
                                    init_exT((INamedTypeSymbol)((IArrayTypeSymbol)T).ElementType, null);
                                    _array_ = (Array?)constant;
                                }
                                else init_exT((INamedTypeSymbol)T, constant);
                            }
                            else
                            {
                                if (project.entities[symbol!.ContainingType] is PackImpl pack) pack.fields.Add(this);
                                else AdHocAgent.exit($"`{project.entities[symbol!.ContainingType].full_path}` cannot contains any fields. Delete `{_name}`.");

                                if (T.ToString()!.StartsWith("org.unirail.Meta.Set<"))
                                {
                                    init_exT((INamedTypeSymbol)((INamedTypeSymbol)T).TypeArguments[0], null); //K
                                    _the_set = true;                                                          //set type
                                }
                                else if (T.ToString()!.StartsWith("org.unirail.Meta.Map<"))
                                {
                                    V = new FieldImpl(project, (MemberDeclarationSyntax)node, model); //The Map Value info

                                    init_exT((INamedTypeSymbol)((INamedTypeSymbol)T).TypeArguments[0], null);   //K
                                    V.init_exT((INamedTypeSymbol)((INamedTypeSymbol)T).TypeArguments[1], null); //V
                                }
                                else if (T.Kind == SymbolKind.ArrayType)
                                    init_exT((INamedTypeSymbol)((IArrayTypeSymbol)T).ElementType, null);
                                else init_exT((INamedTypeSymbol)T,                                null);
                            }

                            if (!_name.Equals(symbol.Name))
                                AdHocAgent.LOG.Warning("The name of {entity} is prohibited and changed to {new_name}. Please correct the name manually", symbol, _name);
                        }

                        public void switch_to_boolean()
                        {
                            exT_pack      = null;
                            exT_primitive = (int?)Project.Host.Port.Pack.Field.DataType.t_bool;
                            inT           = (int?)Project.Host.Port.Pack.Field.DataType.t_bool;
                            bits          = 1;
                        }

#region exT
                        private void init_exT(INamedTypeSymbol T, object? constant)
                        {
                            if (T.ToString()!.EndsWith("?"))
                            {
                                if (T.NullableAnnotation == NullableAnnotation.Annotated)
                                    T = T.TypeArguments.Length == 0
                                            ? T.ConstructedFrom
                                            : (INamedTypeSymbol)T.TypeArguments[0];
                                else
                                    T = T.SpecialType == SpecialType.None
                                            ? (INamedTypeSymbol)T.TypeArguments[0]
                                            : T;
                                _null_value = NULL; //mark as a nullable
                            }
                            else _null_value = null; //sure not nullable

                            switch (T.SpecialType)
                            {
                                case SpecialType.System_Boolean:
                                    if (nullable)
                                    {
                                        _null_value = 0; //0->NULL   2->false    3->true 
                                        if (constant != null)
                                            _value_int = (bool)constant
                                                             ? 1
                                                             : 2;
                                        bits = 2;
                                    }
                                    else
                                    {
                                        if (constant != null)
                                            _value_int = (bool)constant
                                                             ? 1
                                                             : 0;
                                        bits = 1;
                                    }

                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_bool;
                                    break;
                                case SpecialType.System_SByte:
                                    _value_int    = (sbyte?)constant;
                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_int8;
                                    _value_bytes  = 1;
                                    break;
                                case SpecialType.System_Byte:
                                    _value_int    = (byte?)constant;
                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_uint8;
                                    _value_bytes  = 1;
                                    break;
                                case SpecialType.System_Int16:
                                    _value_int    = (short?)constant;
                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_int16;
                                    _value_bytes  = 2;
                                    break;
                                case SpecialType.System_UInt16:
                                    _value_int    = (ushort?)constant;
                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_uint16;
                                    _value_bytes  = 2;
                                    break;
                                case SpecialType.System_Char:
                                    _value_int    = (char?)constant;
                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_char;
                                    _value_bytes  = 2;
                                    break;
                                case SpecialType.System_Int32:
                                    _value_int    = (int?)constant;
                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_int32;
                                    _value_bytes  = 4;
                                    break;
                                case SpecialType.System_UInt32:
                                    _value_int    = (long?)constant;
                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_uint32;
                                    _value_bytes  = 3;
                                    break;
                                case SpecialType.System_Int64:
                                    _value_int    = (long?)constant;
                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_int64;
                                    _value_bytes  = 8;
                                    break;
                                case SpecialType.System_UInt64:
                                    _value_int    = (long?)constant;
                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_uint64;
                                    _value_bytes  = 8;
                                    break;
                                case SpecialType.System_Single:
                                    _value_double = (float?)constant;
                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_float;
                                    _value_bytes  = 4;
                                    break;
                                case SpecialType.System_Double:
                                    _value_double = (double?)constant;
                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_double;
                                    _value_bytes  = 8;
                                    break;
                                case SpecialType.System_String:
                                    _null_value   = null; //no need this settings. this type is nullable by default
                                    _value_string = constant?.ToString();
                                    exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_string;
                                    break;
                                default:
                                    if (T.ToString()!.Equals("org.unirail.Meta.Binary"))
                                    {
                                        _null_value   = null; //no need this settings. this type is nullable by default
                                        exT_primitive = inT = (int)Project.Host.Port.Pack.Field.DataType.t_binary;
                                    }
                                    else //       other none primitive types
                                    {
                                        _null_value   = null; //no need this settings. this type is nullable by default
                                        exT_primitive = null;
                                        if (T.IsImplicitClass)
                                            AdHocAgent.exit($"Constants set {T} cannot be referenced. But field {_name} do.", 56);
                                        exT_pack = T;
                                    }

                                    break;
                            }
                        }

                        public void set_EXT_ByRange(long min, long max)
                        {
                            if (min < 0)
                                if (min < int.MinValue || int.MaxValue < max)
                                {
                                    exT_primitive = (int)Project.Host.Port.Pack.Field.DataType.t_int64;
                                    _value_bytes  = 8;
                                }
                                else if (min < short.MinValue || short.MaxValue < max)
                                {
                                    exT_primitive = (int)Project.Host.Port.Pack.Field.DataType.t_int32;
                                    _value_bytes  = 4;
                                }
                                else if (min < sbyte.MinValue || sbyte.MaxValue < max)
                                {
                                    exT_primitive = (int)Project.Host.Port.Pack.Field.DataType.t_int16;
                                    _value_bytes  = 2;
                                }
                                else
                                {
                                    exT_primitive = (int)Project.Host.Port.Pack.Field.DataType.t_int8;
                                    _value_bytes  = 1;
                                }
                            else
                                switch (max)
                                {
                                    case > uint.MaxValue:
                                        exT_primitive = (int)Project.Host.Port.Pack.Field.DataType.t_uint64;
                                        _value_bytes  = 8;
                                        break;
                                    case > int.MaxValue:
                                        exT_primitive = (int)Project.Host.Port.Pack.Field.DataType.t_int64;
                                        _value_bytes  = 8;
                                        break;
                                    case > ushort.MaxValue:
                                        exT_primitive = (int)Project.Host.Port.Pack.Field.DataType.t_int32;
                                        _value_bytes  = 4;
                                        break;
                                    case > short.MaxValue:
                                        exT_primitive = (int)Project.Host.Port.Pack.Field.DataType.t_uint16;
                                        _value_bytes  = 2;
                                        break;
                                    case > byte.MaxValue:
                                        exT_primitive = (int)Project.Host.Port.Pack.Field.DataType.t_int16;
                                        _value_bytes  = 2;
                                        break;
                                    default:
                                        exT_primitive = (int)Project.Host.Port.Pack.Field.DataType.t_uint8;
                                        _value_bytes  = 1;
                                        break;
                                }
                        }

                        internal PackImpl? get_exT_pack => exT_pack == null
                                                               ? null
                                                               : (PackImpl)project.entities[exT_pack];


                        public        INamedTypeSymbol? exT_pack;
                        public        int?              exT_primitive;
                        public        ushort            _Ext                     => (ushort)(exT_primitive ?? get_exT_pack?.uid)!;
                        public static bool              varintable(int datatype) => (int)Project.Host.Port.Pack.Field.DataType.t_float < datatype && datatype < (int)Project.Host.Port.Pack.Field.DataType.t_uint8;

                        public long datatype_max => (Project.Host.Port.Pack.Field.DataType)exT_primitive! switch
                                                    {
                                                        Project.Host.Port.Pack.Field.DataType.t_bool => 1,

                                                        Project.Host.Port.Pack.Field.DataType.t_int8 => sbyte.MaxValue,

                                                        Project.Host.Port.Pack.Field.DataType.t_uint8  => byte.MaxValue,
                                                        Project.Host.Port.Pack.Field.DataType.t_int16  => short.MaxValue,
                                                        Project.Host.Port.Pack.Field.DataType.t_uint16 => ushort.MaxValue,
                                                        Project.Host.Port.Pack.Field.DataType.t_char   => char.MaxValue,
                                                        Project.Host.Port.Pack.Field.DataType.t_int32  => int.MaxValue,
                                                        Project.Host.Port.Pack.Field.DataType.t_uint32 => uint.MaxValue,
                                                        Project.Host.Port.Pack.Field.DataType.t_int64  => long.MaxValue,
                                                        Project.Host.Port.Pack.Field.DataType.t_uint64 => long.MaxValue,
                                                    };

                        public long datatype_min => (Project.Host.Port.Pack.Field.DataType)exT_primitive! switch
                                                    {
                                                        Project.Host.Port.Pack.Field.DataType.t_bool => 1,

                                                        Project.Host.Port.Pack.Field.DataType.t_int8 => sbyte.MinValue,

                                                        Project.Host.Port.Pack.Field.DataType.t_uint8  => 0,
                                                        Project.Host.Port.Pack.Field.DataType.t_int16  => short.MinValue,
                                                        Project.Host.Port.Pack.Field.DataType.t_uint16 => 0,
                                                        Project.Host.Port.Pack.Field.DataType.t_char   => 0,
                                                        Project.Host.Port.Pack.Field.DataType.t_int32  => int.MinValue,
                                                        Project.Host.Port.Pack.Field.DataType.t_uint32 => 0,
                                                        Project.Host.Port.Pack.Field.DataType.t_int64  => long.MinValue,
                                                        Project.Host.Port.Pack.Field.DataType.t_uint64 => 0,
                                                    };
#endregion

#region inT
                        public void set_INT_ByRange(long min, long max)
                        {
                            var diff = new BigInteger(max) - new BigInteger(min);

                            switch (long.MaxValue < diff
                                        ? long.MaxValue
                                        : (long)diff) //by range
                            {
                                case < 127:

                                    inT = (int)Project.Host.Port.Pack.Field.DataType.t_uint8; //bits field. values range <127

                                    bits = (byte?)(32 - BitOperations.LeadingZeroCount(nullable
                                                                                           ? (uint)(_null_value = (byte?)(max - min + 1)) //+ a bit for null_value
                                                                                           : (uint)(max - min)));
                                    break;
                                case <= byte.MaxValue:
                                    inT = _value_bytes == 1
                                              ? exT_primitive
                                              : (int)Project.Host.Port.Pack.Field.DataType.t_uint8;
                                    break;
                                case <= ushort.MaxValue:
                                    inT = _value_bytes == 2
                                              ? exT_primitive
                                              : (int)Project.Host.Port.Pack.Field.DataType.t_uint16;
                                    break;
                                case <= uint.MaxValue:
                                    inT = _value_bytes == 4
                                              ? exT_primitive
                                              : (int)Project.Host.Port.Pack.Field.DataType.t_uint32;
                                    break;
                                default:
                                    inT = _value_bytes == 8
                                              ? exT_primitive
                                              : (int)Project.Host.Port.Pack.Field.DataType.t_uint64;
                                    break;
                            }
                        }


                        public int?    inT; //internal Type if null ->  none - (primitive or enum)
                        public ushort? _Int => (ushort)(inT ?? get_exT_pack!.uid);
#endregion
                        public   sbyte?  dir;
                        public   byte?   _dirˍ       => Project.Host.Port.Pack.Field.dir_.INT(dir);
                        public   long?   _min_value  { get; set; }
                        public   long?   _max_value  { get; set; }
                        public   double? _min_valueD { get; set; }
                        public   double? _max_valueD { get; set; }
                        public   byte?   bits;
                        public   byte?   _bitsˍ      => Project.Host.Port.Pack.Field.bits_.INT(bits);
                        public   byte?   _null_value { get; set; } // if 0 < bits ( bits-field) this is the value that substitute NULL, if field has primitive type, any value(NULL) assigned to `_null_value` make primitive type  nullable 
                        internal bool    nullable    => _null_value != null;


                        public bool       MapValueAttributes; // if true apply left attributes to V 
                        public FieldImpl? V;                  //Map Value datatype Info

#region ExtV
                        public ushort? _ExtV => V?._Ext;
#endregion

#region IntV
                        public ushort? _IntV => V?._Int;
#endregion
                        public byte?   _dirVˍ       => V?._dirˍ;
                        public long?   _min_valueV  => V?._min_value;
                        public long?   _max_valueV  => V?._max_value;
                        public double? _min_valueDV => V?._min_valueD;
                        public double? _max_valueDV => V?._max_valueD;

                        public byte? _bitsVˍ => V?._bitsˍ;

                        public byte? _null_valueV => V?._null_value;

#region dims
                        public int[]   _dims_ = Array.Empty<int>();
                        public object? _dims()                               => _dims_;
                        public int     _dims_Count                           => _dims_.Length;
                        public int     _dims(Context.Provider ctx, int item) => _dims_[item];
#endregion

                        public bool _the_set { get; set; }

                        public long?   _value_int    { get; set; }
                        public double? _value_double { get; set; }
                        public string? _value_string { get; set; }

#region array
                        private Array? _array_;

                        public string? _array(Context.Provider ctx, int item) => _array_!.GetValue(item)?.ToString();


                        public int     _array_Count => _array_!.Length;
                        public object? _array()     => _array_;
#endregion

                        public int? _value_bytes { get; set; }

                        private long int_val(ExpressionSyntax src) //get real value
                        {
                            if (!src.IsKind(SyntaxKind.IdentifierName)) return Convert.ToInt64(model.GetConstantValue(src).Value);

                            var fld = project.raw_fields[model.GetSymbolInfo(src).Symbol!];
                            return (fld.substitute_value_from == null
                                        ? fld
                                        : project.raw_fields[fld.substitute_value_from])._value_int!.Value; //return sudstitute value
                        }

                        private double dbl_val(ExpressionSyntax src)
                        {
                            if (!src.IsKind(SyntaxKind.IdentifierName)) return Convert.ToDouble(model.GetConstantValue(src).Value);

                            var fld = project.raw_fields[model.GetSymbolInfo(src).Symbol!];
                            return (fld.substitute_value_from == null
                                        ? fld
                                        : project.raw_fields[fld.substitute_value_from])._value_double!.Value; //return sudstitute value
                        }


                        public static void init(ProjectImpl project)
                        {
                            var fields = project.raw_fields.Values;
#region processs Attributes
                            foreach (var fld in fields.Where(fld => !fld.is_const))
                            {
                                var dst_fld = fld;

                                foreach (var list in fld.fld_node!.AttributeLists) //process fields attributes
                                    foreach (var attr in list.Attributes)
                                    {
                                        var name  = attr.Name.ToString();
                                        var alist = attr.ArgumentList;

                                        switch (!name.EndsWith("Attribute")
                                                    ? $"{name}Attribute"
                                                    : name)
                                        {
                                            case "MapValueParamsAttribute":
                                                fld.MapValueAttributes = true;
                                                dst_fld                = fld.V; //rest of attributes are  for Value of the Map type field

                                                if (dst_fld == null)
                                                    AdHocAgent.exit($"Unappropriated use of MapValueParams Attribute on field {fld}. MapValueParams Attribute may be apply on Map type fields only", 2);
                                                continue;

                                            case "MinMaxAttribute":
                                            {
                                                if (dst_fld.exT_primitive is (int)Project.Host.Port.Pack.Field.DataType.t_bool or <= (int)Project.Host.Port.Pack.Field.DataType.t_string)
                                                    AdHocAgent.exit($"Field {dst_fld} with it's type has nonsenses {name} attribute.", -1);

                                                var args = alist!.Arguments;
                                                if (dst_fld.exT_primitive is (int)Project.Host.Port.Pack.Field.DataType.t_float or (int)Project.Host.Port.Pack.Field.DataType.t_double)
                                                {
                                                    dst_fld._min_valueD = dst_fld.dbl_val(args[0].Expression);
                                                    dst_fld._max_valueD = dst_fld.dbl_val(args[1].Expression);

                                                    if (dst_fld._max_valueD < dst_fld._min_valueD) (dst_fld._min_valueD, dst_fld._max_valueD) = (dst_fld._max_valueD, dst_fld._min_valueD);

                                                    if (dst_fld._min_valueD < float.MinValue || float.MaxValue < dst_fld._max_valueD)
                                                    {
                                                        dst_fld.exT_primitive = (int)Project.Host.Port.Pack.Field.DataType.t_double;
                                                        dst_fld._value_bytes  = 8;
                                                    }
                                                    else
                                                    {
                                                        dst_fld.exT_primitive = (int)Project.Host.Port.Pack.Field.DataType.t_float;
                                                        dst_fld._value_bytes  = 4;
                                                    }
                                                }
                                                else
                                                    setByRange(dst_fld, dst_fld.int_val(args[0].Expression), dst_fld.int_val(args[1].Expression));
                                            }
                                                continue;

                                            case "AAttribute":
                                                if (!varintable(dst_fld._Ext)) AdHocAgent.exit($"Cannot assign VARINT [A] attribute  to the {fld.symbol} field with none primitive or smale range datatype.");
                                                dst_fld.dir = 1;

                                                if (alist != null)
                                                {
                                                    setByRange(dst_fld,
                                                               dst_fld.int_val(alist.Arguments[0].Expression),
                                                               1 < alist.Arguments.Count
                                                                   ? dst_fld.int_val(alist.Arguments[1].Expression)
                                                                   : dst_fld.datatype_max);

                                                    if (dst_fld._max_value - dst_fld._min_value < 0xFF)
                                                        AdHocAgent.exit($"Field {fld.symbol}  has VARINT attribute [A] but narrow(less then 2 bytes) value range.");
                                                }

                                                break;
                                            case "VAttribute":
                                                if (!varintable(dst_fld._Ext)) AdHocAgent.exit($"Cannot assign VARINT [V] attribute  to the {fld.symbol} field with none primitive or smale range datatype.");
                                                dst_fld.dir = -1;
                                                if (alist != null)
                                                {
                                                    setByRange(dst_fld,
                                                               1 < alist.Arguments.Count
                                                                   ? dst_fld.int_val(alist.Arguments[1].Expression)
                                                                   : dst_fld.datatype_min,
                                                               dst_fld.int_val(alist.Arguments[0].Expression));

                                                    if (dst_fld._max_value - dst_fld._min_value < 0xFF) AdHocAgent.exit($"Field {fld.symbol} has VARINT attribute [V] but narrow(less then 2 bytes) value range.");
                                                }

                                                break;
                                            case "XAttribute":
                                                if (!varintable(dst_fld._Ext)) AdHocAgent.exit($"Cannot assign VARINT [X] attribute to the {fld.symbol} field with none primitive or smale range datatype.");
                                                dst_fld.dir = 0;

                                                if (alist != null)
                                                {
                                                    dst_fld._max_value = dst_fld.int_val(alist.Arguments[0].Expression); //Amplitude
                                                    dst_fld._min_value = 1 < alist.Arguments.Count
                                                                             ? dst_fld.int_val(alist.Arguments[1].Expression)
                                                                             : 0; //Zero

                                                    var min = dst_fld._min_value.Value - dst_fld._max_value.Value;
                                                    var max = dst_fld._min_value.Value + dst_fld._max_value.Value;

                                                    dst_fld.set_EXT_ByRange(min, max);
                                                    dst_fld.set_INT_ByRange(min, max);

                                                    if (dst_fld._max_value < 0xFF && dst_fld._max_value * 2 < 0xFF) //very large number protection 
                                                        AdHocAgent.exit($"Field {fld.symbol}  has VARINT attribute [X] but narrow(less then 2 bytes) value range.");
                                                }

                                                break;

                                            case "DimsAttribute":
                                            {
                                                var args = alist!.Arguments;
                                                dst_fld._dims_ = new int[args.Count];

                                                long val;
                                                for (var i = 0; i < args.Count; dst_fld._dims_[i] = (int)val, i++)
                                                {
                                                    var arg = args[i].Expression;

                                                    if (arg is PrefixUnaryExpressionSyntax exp)
                                                    {
                                                        var s = exp.ToString();
                                                        val = dst_fld.int_val(exp.Operand);

                                                        if (s.StartsWith("-")) //dimention with variable length
                                                        {
                                                            val |= 4 << 29; //variable dimension
                                                            continue;
                                                        }

                                                        //array params have to goes last
                                                        if (i < args.Count - 1)
                                                            AdHocAgent.exit($"Field {fld} attribute Dims dimension {i} has wrong formatted value {arg}.", 2);

                                                        if (s.StartsWith("~~")) val     =  ~val | 1 << 29; //Each array has its own length
                                                        else if (s.StartsWith("~")) val |= 2 << 29;        //All arrays are have the same variable length that set once at field creation
                                                        else if (s.StartsWith("+")) val |= 3 << 29;        //All arrays are have constant length
                                                    }
                                                    else
                                                        val = dst_fld.int_val(arg); //dimention with fixed length
                                                }
                                            }
                                                continue;
                                        }
                                    }
                            }
#endregion

#region Process constants substitute
                            foreach (var dst_fld in fields.Where(fld => !fld.is_const && fld.substitute_value_from != null)) //not enums but static fields
                            {
                                var src_fld                         = project.raw_fields[dst_fld.substitute_value_from!]; //takes type and value from src
                                dst_fld.exT_primitive = dst_fld.inT = src_fld.exT_primitive;
                                dst_fld._value_double = src_fld._value_double;
                                dst_fld._value_int    = src_fld._value_int;
                                dst_fld._value_string = src_fld._value_string;
                            }
#endregion

#region merge fields with reference to single-field-packs
                            foreach (var fld in fields.Where(fld => fld.exT_pack != null)) //select fields with a non-primitive type
                            {
                                fld.V?.merge_in_ref_place();
                                fld.merge_in_ref_place();
                                fld.V?.merge_in_ref_place();
                            }
#endregion
                        }

                        private bool merge_in_ref_place()
                        {
                            var exT = get_exT_pack!;
                            if (exT._value_type || exT.fields.Count != 1) return false;


                            var fld = exT.fields[0];

                            while (fld.exT_pack != null) //possible cyclic reference
                            {
                                var src_fld_exT = fld.get_exT_pack!;
                                if (src_fld_exT == exT)
                                    AdHocAgent.exit($"Cyclic reference of single-field-pack {exT.symbol} detected.", 34);

                                if (src_fld_exT._value_type || src_fld_exT.fields.Count != 1) break;
                                fld = src_fld_exT.fields[0];
                            }

                            // fld takes type and format from src_fld

                            if (_dims_.Length == 0) _dims_ = fld._dims_;
                            else if (0        < fld._dims_.Length)
                            {
                                //merge(stack)  _dims_ and src_fld._dims_ in one _dims_

                                if (contains_collection) //change collection format to dim
                                    _dims_[^1] = (int)(collection_has_fixed_size
                                                           ? collection_size | 4u << 29
                                                           : collection_size);

                                var len = _dims_.Length;

                                Array.Resize(ref _dims_, len + fld._dims_.Length);
                                Array.Copy(fld._dims_, 0, _dims_, len, fld._dims_.Length);
                            }

                            exT_pack      = fld.exT_pack;
                            exT_primitive = fld.exT_primitive;
                            inT           = fld.inT;
                            dir           = fld.dir;
                            bits          = fld.bits;
                            if (!_the_set) _the_set = fld._the_set; //set
                            V ??= fld.V;

                            _null_value   = fld._null_value;
                            _min_value    = fld._min_value;
                            _max_value    = fld._max_value;
                            _min_valueD   = fld._min_valueD;
                            _max_valueD   = fld._max_valueD;
                            _value_int    = fld._value_int;
                            _value_double = fld._value_double;
                            _value_string = fld._value_string;
                            _value_bytes  = fld._value_bytes;

                            if (_dims_.Length == 0 && _null_value == null && is_primitive) //merged single non-nullable primitive, make it nullable, because merged pack is nullable
                                _null_value = (byte?)(bits == null
                                                          ? NULL
                                                          : bits + 1); //mark as nullable 


                            return true;
                        }

                        public  bool is_primitive              => exT_primitive != null         && (int)Project.Host.Port.Pack.Field.DataType.t_string < exT_primitive;
                        private bool contains_collection       => 0             < _dims_.Length && (uint)_dims_[^1] >> 29 is < 4 and > 0;
                        private bool collection_has_fixed_size => 0             < _dims_.Length && (uint)_dims_[^1] >> 29 == 3;
                        private uint collection_size           => (uint)_dims_[^1] << 3 >> 3;

                        private static void setByRange(FieldImpl fld, long min, long max)
                        {
                            if (max < min) (max, min) = (min, max); //swap
                            fld._min_value = min;
                            fld._max_value = max;

                            fld.set_EXT_ByRange(min, max);
                            fld.set_INT_ByRange(min, max);
                        }

                        private const byte NULL = 0xFF;
                    }
                }
            }
        }

        public class ChannelImpl : Entity, Project.Channel
        {
            public ChannelImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax channel) : base(project, compilation, channel) //struct based
            {
                var interfaces = symbol.Interfaces;
                if (interfaces.Length == 0 || !interfaces[0].Name.Equals("Communication_Channel_Of"))
                    AdHocAgent.exit($"Channel {symbol} should implements Communication_Channel_Of C# interface and connect two ports of different hosts");

                if (parent_entity is not ProjectImpl)
                    AdHocAgent.exit($"A channel {symbol} definition should go directly inside project declaration.");

                project.channels.Add(this);
            }

            public override bool included => _included ?? in_project.included;

            internal INamedTypeSymbol A => (INamedTypeSymbol)symbol!.Interfaces[0].TypeArguments[0];
            internal INamedTypeSymbol B => (INamedTypeSymbol)symbol!.Interfaces[0].TypeArguments[1];

            public ushort _portA => (ushort)project.entities[A].uid;
            public ushort _portB => (ushort)project.entities[B].uid;
        }
    }
}