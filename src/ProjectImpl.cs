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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using org.unirail.Agent;

// Microsoft.CodeAnalysis >>>> https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel?view=roslyn-dotnet-3.11.0
namespace org.unirail
{
    public class HasDocs
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
            if (prj == null && node == null) return;
            project = prj ?? (ProjectImpl)this; //prj == null only for projects

            if (node == null) return;

            name = name[(name.LastIndexOf('.') + 1)..];
            _name = brush(name);

            char_in_source_code = node.GetLocation().SourceSpan.Start;


            var trivias = node.GetLeadingTrivia();

            var doc = trivias.Aggregate("", (current, trivia) => current + get_doc(trivia));

            if (project.packs_id_info_end == -1) project.packs_id_info_end = char_in_source_code;


            if (0 < (doc = doc.Trim('\r', '\n', '\t', ' ')).Length) _doc = doc + "\n";
        }

        public List<INamedTypeSymbol> add = new();

        public void add_from(INamedTypeSymbol symbol)
        {
            var src = symbol.BaseType == null || symbol.BaseType.Name.Equals("Object") ?
                          symbol.Interfaces :
                          symbol.Interfaces.Concat(new[] { symbol.BaseType });

            foreach (var Interface in src) //add `inhereted` items
                extract(Interface);
        }

        private void extract(INamedTypeSymbol s)
        {
            if (s.ToString()!.StartsWith("org.unirail.Meta._<"))
                foreach (var arg in s.TypeArguments)
                    extract((INamedTypeSymbol)arg);

            else add.Add(s);
        }

        public List<INamedTypeSymbol> del = new();

        public List<ISymbol> add_fld = new();
        public List<ISymbol> del_fld = new();


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
    }

    public class Entity : HasDocs
    {
        public static bool equals(ISymbol x, ISymbol y) => SymbolEqualityComparer.Default.Equals(x, y);

        public static bool doAlter(INamedTypeSymbol entity, Action<ISymbol> on_add, Action<ISymbol> on_del)
        {
            if (!entity.Name.Equals("Alter") || !entity.ToString()!.StartsWith("org.")) return false;

            if (entity.TypeArguments[0] is not INamedTypeSymbol add_item) on_add(entity.TypeArguments[0]);
            else if (!do_(add_item, on_add))
                on_add(add_item);

            if (entity.TypeArguments[1] is not INamedTypeSymbol del_item) on_del(entity.TypeArguments[0]);
            else if (!do_(del_item, on_del))
                on_add(del_item);

            return true;
        }

        public static bool do_(INamedTypeSymbol entity, Action<ISymbol> on_item)
        {
            if (!entity.Name.Equals("_") || !entity.ToString()!.StartsWith("org.")) return false;

            foreach (var item in entity.TypeParameters) on_item(item);

            return true;
        }

        public BaseTypeDeclarationSyntax? node;

        public Entity? parent_entity => project.entities[symbol.OriginalDefinition.ContainingType];

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
                if (this is ProjectImpl)
                    return this == project ?
                               "" :
                               symbol!.ToString() ?? "";

                var path = _name;
                for (var e = parent_entity; ; path = e._name + "." + path, e = e.parent_entity)
                    if (e is ProjectImpl)
                        return (e == project //root project
                                    ?
                                    "" :
                                    e.symbol + "." //full path
                               ) + path;
            }
        }

        public int line_in_src_code => symbol!.Locations[0].GetLineSpan().StartLinePosition.Line + 1;
        public INamedTypeSymbol? symbol;
        public SemanticModel model;


        public bool? _included;
        public virtual bool included => _included ?? false;


        public Entity(ProjectImpl prj, CSharpCompilation? compilation, BaseTypeDeclarationSyntax? node) : base(prj, node == null ?
                                                                                                                        "" :
                                                                                                                        node.Identifier.ToString(), node)
        {
            if (compilation == null || node == null) return;
            this.node = node;
            model = compilation.GetSemanticModel(node.SyntaxTree);
            symbol = model.GetDeclaredSymbol(node)!;
            project.entities.Add(symbol, this);
            if (!_name.Equals(symbol.Name))
            {
                AdHocAgent.LOG.Warning("The entity '{entity}' name at the {provided_path} line: {line} is prohibited. Please correct the name manually.", symbol, AdHocAgent.provided_path, line_in_src_code);
                AdHocAgent.exit("");
            }

            var txt = node.SyntaxTree.ToString();
            var s = uid_pos;
            var rn = txt.IndexOf('\n', s);
            if (rn == -1) rn = txt.IndexOf('\r', s);

            var comments_after = node.DescendantTrivia().Where(
                                                               t =>
                                                                   (t.IsKind(SyntaxKind.MultiLineCommentTrivia) || t.IsKind(SyntaxKind.SingleLineCommentTrivia)) &&
                                                                   s <= t.SpanStart &&
                                                                   t.SpanStart < rn
                                                              );
            if (!comments_after.Any()) return;

            var m = HasDocs.uid.Match(comments_after.First().ToString());
            if (!m.Success) return;

            uid = (ushort)str2int(m.Groups[1].Value);
            comments_after = comments_after.Skip(1);

            //_inline_doc += string.Join(' ', comments_after.Select(get_doc));
        }

        public bool is_real => symbol != null;

        public ushort uid = ushort.MaxValue;
        public ushort _uid => uid;

        public int uid_pos
        {
            get
            {
                var span = symbol!.Locations[0].SourceSpan;
                return span.Start + span.Length;
            }
        }


        public Entity(ProjectImpl project, BaseTypeDeclarationSyntax? node) : base(project, node?.Identifier.ToString() ?? "", node) { this.node = node; }

        private const int base256 = 0xFF;

        private static byte[] bytes = new byte[20];

        public static int str2int(string str)
        {
            var ret = 0;
            for (var i = 0; i < str.Length; i++)
                ret |= (str[i] - base256) << i * 8;

            return ret;
        }

        public static int int2str(int src, char[] dst)
        {
            var i = 0;
            do { dst[i++] = (char)((src & 0xFF) + base256); }
            while (0 < (src >>= 8));

            return i;
        }
    }


    public class ProjectImpl : Entity, Project
    {
        public readonly Dictionary<INamedTypeSymbol, HostImpl.PackImpl[]> named_packs = new(SymbolEqualityComparer.Default); //Group related packets under a descriptive name

        public readonly Dictionary<ISymbol, Entity> entities = new(SymbolEqualityComparer.Default);

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

        private class Protocol_Description_Parser : CSharpSyntaxWalker
        {
            public readonly List<ProjectImpl> projects = new(); //all projects

            public HasDocs? HasDocs_instance;

            public int inline_doc_line;

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

                if (symbol.OriginalDefinition.ContainingType == null) //top-level C# interface - it's a project
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
                    var interfaces = symbol.Interfaces;
                    var str = interfaces[0].ToString()!;
                    if (0 < interfaces.Length && str.StartsWith("org."))
                        switch (interfaces[0].Name)
                        {
                            case "ChannelFor":
                                HasDocs_instance = new ChannelImpl(project, compilation, node); //Channel
                                break;
                            case "L" or "R":
                                HasDocs_instance = new ChannelImpl.StageImpl(project, compilation, node); //host stage
                                break;
                            case "_":
                                project.named_packs.Add(symbol, Array.Empty<HostImpl.PackImpl>()); //set of packs
                                HasDocs_instance = null;
                                break;
                            default:
                                HasDocs_instance = null;
                                break;
                        }
                }

                inline_doc_line = node.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
                base.VisitInterfaceDeclaration(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(node)!;
                var interfaces = symbol.Interfaces;

                if (project.entities[symbol.ContainingType] is ProjectImpl && 0 < interfaces.Length && interfaces[0].Name.Equals("Host"))
                    HasDocs_instance = new HostImpl(project, compilation, node); //host
                else
                    HasDocs_instance = new HostImpl.PackImpl(project, compilation, node); //constants set

                inline_doc_line = node.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
                base.VisitStructDeclaration(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax clazz)
            {
                HasDocs_instance = new HostImpl.PackImpl(project, compilation, clazz);

                inline_doc_line = clazz.GetLocation().GetMappedLineSpan().StartLinePosition.Line;

                base.VisitClassDeclaration(clazz);
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax ENUM)
            {
                HasDocs_instance = new HostImpl.PackImpl(project, compilation, ENUM);
                inline_doc_line = ENUM.GetLocation().GetMappedLineSpan().StartLinePosition.Line;

                base.VisitEnumDeclaration(ENUM);
            }


            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);

                foreach (var variable in node.Declaration.Variables) { HasDocs_instance = new HostImpl.PackImpl.FieldImpl(project, node, variable, model); }

                inline_doc_line = node.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
                base.VisitFieldDeclaration(node);
            }


            public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);

                HasDocs_instance = new HostImpl.PackImpl.FieldImpl(project, node, model);
                inline_doc_line = node.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
                base.VisitEnumMemberDeclaration(node);
            }


            public override void VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                    if (HasDocs_instance != null && inline_doc_line == trivia.GetLocation().GetMappedLineSpan().StartLinePosition.Line)
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
                    var txt = comment_line.Parent!.ToFullString()[(comment_line.FullSpan.End - comment_line.Parent!.FullSpan.Start)..];


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

                            host._default_impl_hash_equal = (0 < txt.Length //the first char after last `>` impl pack
                                                                 ?
                                                                 txt[0] :
                                                                 ' ') switch
                            {
                                '+' => host._default_impl_hash_equal | (uint)(lang << 16),    //Implementing pack in this language.
                                '-' => (uint)(host._default_impl_hash_equal & ~(lang << 16)), //Abstracting pack in this language.
                                _ => host._default_impl_hash_equal
                            };

                            host._default_impl_hash_equal = (1 < txt.Length //the second char after last `>` - hash and equals methods
                                                                 ?
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

                    var del = 0 < txt.Length && txt[0] == '-'; //the first char after last `>` is `-`

                    #region alter pack configuration
                    switch (cref!.Kind)
                    {
                        case SymbolKind.Field: // Reference to a field
                            switch (HasDocs_instance)
                            {
                                case HostImpl.PackImpl dst_pack: // Adding or removing a field from the pack entity

                                    if (cref is IFieldSymbol src_field && (src_field.IsStatic || src_field.IsConst))
                                    {
                                        AdHocAgent.LOG.Error("Field '{src_field}' is {static_const}, but only instance fields can be {add_del} to '{dst_pack}'.",
                                                             src_field,
                                                             src_field.IsConst ?
                                                                 "const" :
                                                                 "static",
                                                             del ?
                                                                 "del" :
                                                                 "add",
                                                             dst_pack);

                                        AdHocAgent.exit("Please correct the error and try again.", -9);
                                    }

                                    (del ?
                                         dst_pack.del_fld :
                                         dst_pack.add_fld).Add(cref);
                                    break;
                                default:
                                    AdHocAgent.LOG.Warning("Unrecognized use of '{src_field}' in '{dst}', operation skipped.", cref, HasDocs_instance);
                                    break;
                            }

                            break;
                        case SymbolKind.NamedType:
                            switch (HasDocs_instance)
                            {
                                case HostImpl.PackImpl add_fields_to_pack: // Add fields from one pack to another
                                    (del ?
                                         add_fields_to_pack.del :
                                         add_fields_to_pack.add).Add((INamedTypeSymbol)cref);
                                    break;

                                default:
                                    AdHocAgent.LOG.Warning("{Cref} cannot be apply to the {HasDocsInstance}  type only", cref, HasDocs_instance);
                                    break;
                            }

                            break;

                        default:
                            AdHocAgent.exit($"Reference to an unknown entity {node.Cref} at {HasDocs_instance}");
                            break;
                    }
                    #endregion
                }

            END:
                base.VisitXmlCrefAttribute(node);
            }
        }

        public readonly Dictionary<INamedTypeSymbol, int> pack_id_info = new(SymbolEqualityComparer.Default); //saved in source file packs ids

        //calling only on root project
        //                                                                                                    imported_projects_pack_id_info - pack_id_info from other included projects
        public ISet<HostImpl.PackImpl> read_packs_id_info_and_write_update(Dictionary<INamedTypeSymbol, int>? imported_projects_pack_id_info)
        {
            // Packs in stages are transmittable and must have a valid `id`.
            var packs = channels
                        .Where(ch => hosts[ch._hostL].included && hosts[ch._hostR].included)
                        .SelectMany(ch => ch.stages)
                        .SelectMany(stage => stage.branchesL.Concat(stage.branchesR))
                        .SelectMany(branch => branch.packs).ToHashSet(); //packs collector collect valid transmittable packs

            // Check for correct usage of empty packs as type.
            #region Validate empty packs
            foreach (var pack in project.all_packs.Where(pack => pack.fields.Count == 0)) // Empty packs: packs without fields.
            {
                var used = false;

                foreach (var fld in project.raw_fields.Values.Where(fld => fld.V != null && fld.get_exT_pack == pack))
                    AdHocAgent.exit($"The field `{fld.symbol}` at the line: {fld.line_in_src_code} is a Map with a key of empty pack {pack.symbol}, which is unsupported and unnecessary.");

                foreach (var fld in project.raw_fields.Values.Where(fld => fld.get_exT_pack == pack)
                                           .Concat(project.raw_fields.Values.Where(fld => fld.V != null && fld.V.get_exT_pack == pack).Select(fld => fld.V!)) //field value has empty packs as type
                       )                                                                                                                                      //change field type to boolean
                {
                    used = true;
                    fld.switch_to_boolean();
                }


                if (packs.Contains(pack)) // If pack is transmittable
                {
                    if (used) AdHocAgent.LOG.Warning("Pack {Pack} is empty and, as field datatype, it is useless. The reference to it will be replaced with boolean", pack.symbol);
                    continue;
                }

                //NOT transmittable

                if (pack._static_fields_.Count == 0) //NOT transmittable and no any constants
                {
                    if (used) AdHocAgent.LOG.Warning("Pack {Pack} is empty and, as field datatype, it is useless. The reference to it will be replaced with boolean", pack.symbol);
                    pack._id = 0; //mark to delete
                    continue;
                }

                //NOT transmittable constants set
                if (used) AdHocAgent.LOG.Warning("Pack {Pack} is empty and, as field datatype, it is useless. The reference to it will be replaced with boolean", pack.symbol);
                pack._id = (ushort)Project.Host.Pack.Field.DataType.t_constants; //switch use as constants set
                project.constants_packs.Add(pack);
            }
            #endregion

            project.all_packs.RemoveAll(pack => pack._id is 0 or (int)Project.Host.Pack.Field.DataType.t_constants);

            #region read/write packs id
            foreach (var pack in packs) //extract saved communication packs id  info
            {
                if (!pack._name.Equals(pack.symbol!.Name))
                {
                    AdHocAgent.LOG.Error("The name of the pack {entity} (line:{line}) has been changed to {new_name}. However, the pack cannot be assigned an ID until its name is manually corrected", pack.symbol, pack.line_in_src_code, pack._name);
                    AdHocAgent.exit("", 66);
                }

                var key = pack.symbol;
                if (pack_id_info.TryGetValue(key!, out var current_id)) pack._id = (ushort)current_id; //root does not has id info... maybe imported projects have
                else if (imported_projects_pack_id_info!.ContainsKey(key)) pack._id = (ushort)pack_id_info[key];
            }

            if (new FileInfo(AdHocAgent.provided_path).IsReadOnly) //Protocol description file is locked - packs id updating process skipped.
                return packs;

            #region detect pack's id duplication
            foreach (var pks in packs.Where(pk => pk._id < (int)Project.Host.Pack.Field.DataType.t_subpack).GroupBy(pack => pack._id).Where(g => 1 < g.Count()))
            {
                var _1 = pks.First();
                var list = pks.Aggregate("", (current, pk) => current + pk.full_path + "\n");
                AdHocAgent.LOG.Warning("Packs \n{List} with equal id = {Id} detected. Will preserve one assignment, others will be renumbered", list, _1._id);

                //find a one to preserve it's id in root project first
                if (_1.project != project)
                {
                    var pk = pks.FirstOrDefault(pk => pk.project == project);
                    if (pk != null) _1 = pk;
                }

                foreach (var pk in pks) //
                    if (pk != _1)
                        pk._id = (int)Project.Host.Pack.Field.DataType.t_subpack; //reset for renumbering
            }
            #endregion


            #region renumbering
            // check that pack_id_info and `packs` have fully idenical packs
            var update_packs_id_info = pack_id_info.Count != packs.Count || !packs.All(pack => pack_id_info.ContainsKey(pack.symbol!)); //is need update packs_id_info in source file


            for (var id = 0; ; id++) //set new packs id
                if (packs.All(pack => pack._id != id))
                {
                    var pack = packs.FirstOrDefault(pack => pack._id == (int)Project.Host.Pack.Field.DataType.t_subpack);
                    if (pack == null) break;    //no more pack without id
                    update_packs_id_info = true; //mark need to update packs_id_info in protocol description file
                    pack._id = (ushort)id;
                }
            #endregion

            var updates = new List<(int, int)>(10);

            var uid = 0;

            void set_uid(Entity dst, Func<int, bool> check)
            {
                while (check(uid)) uid++;
                if (dst.is_real) updates.Add((dst.uid_pos, uid));
                dst.uid = (ushort)uid++;
            }

            foreach (var host in hosts.Where(host => host.uid == 0xFFFF))
                set_uid(host, uid => hosts.Any(h => h.uid == uid));

            uid = 0;
            foreach (var channel in channels.Where(ch => ch.uid == 0xFFFF))
                set_uid(channel, uid => channels.Any(ch => ch.uid == uid));

            uid = 0;
            foreach (var pack in packs.Where(pack => pack.uid == 0xFFFF))
                set_uid(pack, uid => packs.Any(pack => pack.uid == uid));

            uid = 0;
            foreach (var channel in channels)
            {
                var stages = channel.stages;
                foreach (var stage in stages.Where(host => host.uid == 0xFFFF))
                    set_uid(stage, uid => stages.Any(stage => stage.uid == uid));

                var uid_ = 0;
                foreach (var stage in stages.Where(st => st.is_real))
                {
                    var branches = stage.branchesL.Concat(stage.branchesR).ToArray();
                    foreach (var br in branches.Where(b => b.uid == 0xFFFF))
                    {
                        while (branches.Any(pack => pack.uid == uid_)) uid_++;
                        updates.Add((br.uid_pos, uid_));
                        br.uid = (ushort)uid_++;
                    }
                }
            }

            if (!update_packs_id_info && updates.Count == 0) return packs; //================================= Update the current packs' ID information in the protocol description file.

            var long_full_path = (HostImpl.PackImpl pack) => pack.project == project ?
                                                                 pack.full_path :
                                                                 pack.symbol!.ToString(); //namespace + project_name + pack.full_path

            var text_max_width = packs.Select(p => long_full_path(p).Length).Max() + 4;
            var source_code = node!.SyntaxTree.ToString();
            using StreamWriter file = new(AdHocAgent.provided_path);

            var top = 0;
            if (update_packs_id_info)
            {
                if (packs_id_info_start == -1) packs_id_info_start = packs_id_info_end; //no saved packs id info in source file


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
                top = packs_id_info_end;
            }

            var tmp = new char[4];
            foreach (var (pos, id) in updates.OrderBy((b) => b.Item1))
            {
                file.Write(source_code[top..pos]);
                file.Write("/*");
                var len = int2str(id, tmp);
                file.Write(tmp, 0, len);
                file.Write("*/");
                top = pos;
            }

            file.Write(source_code[top..]);

            file.Flush();
            file.Close();
            #endregion
            return packs;
        }

        string[] compiled_files = Array.Empty<string>();
        int[] compiled_files_times = Array.Empty<int>();

        public ProjectImpl? refresh() => compiled_files
                                         .Where((path, i) => new FileInfo(path).LastWriteTime.Millisecond != compiled_files_times[i]).Any() ?
                                             init() :
                                             null;


        public static ProjectImpl init()
        {
            var compiled_files = new List<string>();
            var compiled_files_times = new List<int>();

            var trees = new[] { AdHocAgent.provided_path }
                        .Concat(AdHocAgent.provided_paths)
                        .Select(path =>
                                {
                                    compiled_files.Add(path);
                                    compiled_files_times.Add(new FileInfo(path).LastWriteTime.Millisecond);

                                    StreamReader file = new(path);
                                    var src = file.ReadToEnd();
                                    file.Close();
                                    return SyntaxFactory.ParseSyntaxTree(src, path: path);
                                }).ToArray();

            var compilation = CSharpCompilation.Create("Output",
                                                       trees,
                                                       ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                                                       .Split(Path.PathSeparator)
                                                       .Select(path => MetadataReference.CreateFromFile(path)),
                                                       new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                                                    optimizationLevel: OptimizationLevel.Debug,
                                                                                    warningLevel: 0, // Set warning level to 0 to suppress warnings
                                                                                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

            var parser = new Protocol_Description_Parser(compilation);


            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms); // write IL code into memory
                if (!result.Success)
                {
                    AdHocAgent.LOG.Error("The protocol description file {ProvidedPath} has an issues:\n{issue}", AdHocAgent.provided_path, string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString()).ToArray()));
                    AdHocAgent.exit("Please fix the problem and rerun");
                }

                ms.Seek(0, SeekOrigin.Begin); // load this 'virtual' DLL so that we can use
                var types = Assembly.Load(ms.ToArray()).GetTypes();
                foreach (var type in types) parser.types.Add(type.ToString().Replace("+", "."), type);
            }


            foreach (var tree in trees) //parsing all projects
                parser.Visit(tree.GetRoot());

            if (parser.projects.Count == 0)
                AdHocAgent.exit($@"No any project detected. Provided file {AdHocAgent.provided_path} has not complete or wrong format. Try to start from init template.");
            var project = parser.projects[0]; //switch to root project
            project._included = true;
            project.compiled_files = compiled_files.ToArray();
            project.compiled_files_times = compiled_files_times.ToArray();

            project.source = AdHocAgent.zip(project.compiled_files);

            #region merge everything into the root project
            foreach (var prj in parser.projects.Skip(1))
            {
                foreach (var (key, value) in prj.entities) project.entities.Add(key, value);

                project.hosts.AddRange(prj.hosts);
                project.channels.AddRange(prj.channels);

                foreach (var (key, value) in prj.named_packs) project.named_packs.Add(key, value);

                project.constants_packs.AddRange(prj.constants_packs);
                project.all_packs.AddRange(prj.all_packs);

                foreach (var (key, value) in prj.raw_fields) project.raw_fields.Add(key, value);
            }
            #endregion

            var typedefs = project.all_packs.Where(pack => pack.is_typedef).Distinct().ToArray();
            project.all_packs.RemoveAll(pack => pack.is_typedef);

            #region process project imports constants/ enum
            while (project.collect_imported_entities(new HashSet<ProjectImpl>())) ;

            project.constants_packs.AddRange(project.project_constants_packs);

            //all constants, enums packs in the root project are included by default
            foreach (var pack in project.constants_packs.Where(pack => pack.is_constants_set || pack.is_enum)) pack._included = true;
            #endregion

            #region process named pack set
            {
                var rerun = true;

                while (rerun)
                {
                    rerun = false;
                    foreach (var name in project.named_packs.Keys) process_named_packs_set(name);
                }

                void process_named_packs_set(INamedTypeSymbol dst)
                {
                    var tmp = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                    var pks = new HashSet<HostImpl.PackImpl>();
                    foreach (var symbol in dst.Interfaces)
                        add(symbol, tmp, pks);

                    var len = project.named_packs[dst].Length;
                    project.named_packs[dst] = pks.ToArray();
                    if (!rerun) rerun = len != project.named_packs[dst].Length; //rerun if added

                    pks.Clear();
                    tmp.Clear();
                }


                void add(INamedTypeSymbol symbol, HashSet<INamedTypeSymbol> tmp, HashSet<HostImpl.PackImpl> pks)
                {
                    if (symbol.ToString()!.StartsWith("org.unirail.Meta._<"))
                        foreach (var arg in symbol.TypeArguments)
                            add((INamedTypeSymbol)arg, tmp, pks);
                    else if (tmp.Add(symbol))
                        if (project.named_packs.ContainsKey(symbol))
                        {
                            process_named_packs_set(symbol);
                            pks.UnionWith(project.named_packs[symbol]);
                        }
                        else if (project.entities.TryGetValue(symbol, out var item) && item is HostImpl.PackImpl { is_constants_set: false, is_enum: false } pack)
                            pks.Add(pack);
                        else
                        {
                            AdHocAgent.LOG.Error("Unexpected item {item} in named packs set", symbol);
                            AdHocAgent.exit("Fix the problem and restart");
                        }
                }
            }
            #endregion

            #region process project Channels
            project.channels = project.channels
                                      .Where(ch => ch.included)
                                      .Distinct()
                                      .ToList();

            if (project.channels.Count == 0) AdHocAgent.exit("No any information about communication Channels.", 45);
            for (var idx = 0; idx < project.channels.Count; idx++)
            {
                var ch = project.channels[idx];
                ch.idx = idx; //set storage place index
                ch.hostL._included = true;
                ch.hostR._included = true;
                ch.Init();

                foreach (var pack in ch.stages.SelectMany(stage => stage.branchesL.SelectMany(branch => branch.packs)).Distinct())
                {
                    ch.hostL_transmitting_packs.Add(pack);
                    pack._included = true;
                }

                foreach (var pack in ch.stages.SelectMany(stage => stage.branchesR.SelectMany(branch => branch.packs)).Distinct())
                {
                    ch.hostR_transmitting_packs.Add(pack);
                    pack._included = true;
                }
            }
            #endregion


            #region collect, enumerate and check hosts
            {
                project.hosts = project.hosts.Where(host => host.included).Distinct().OrderBy(host => host.full_path).ToList();
                for (var i = 0; i < project.hosts.Count; i++) project.hosts[i].idx = i;
                var exit = false;
                foreach (var host in project.hosts.Where(host => host._langs == 0))
                {
                    exit = true;
                    AdHocAgent.LOG.Error("Host {host} has no language implementation information. Use <see cref = \'InLANG\'/> comments construction to add it.", host.symbol);
                }

                if (exit) AdHocAgent.exit("Correct detected problems and restart", 45);

                // Remove from host's scopes: packs registered on project scope
                foreach (var host in project.hosts)
                    host.packs.RemoveAll(pack => project.project_constants_packs.Contains(pack));
            }
            #endregion


            HostImpl.PackImpl.FieldImpl.init(project); //process all fields

            //after typedef fields pocessed
            #region process typedefs
            {
                var flds = project.raw_fields.Values;

                for (var rerun = true; rerun;)
                {
                    rerun = false;

                    foreach (var T in typedefs)
                    {
                        var src = T.fields[0];
                        project.raw_fields.Remove(src.symbol!); //remove typedef field

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


            HostImpl.PackImpl.init(project);
            HostImpl.init(project);

            //imported_pack_id_info  - collection of pack_id_info from imported projects
            //root project pack_id_info override  imported projects pack_id_info
            var imported_projects_pack_id_info = new Dictionary<INamedTypeSymbol, int>(SymbolEqualityComparer.Default);
            foreach (var prj in parser.projects.Skip(1))
                foreach (var pair in prj.pack_id_info)
                    imported_projects_pack_id_info.Add(pair.Key, pair.Value);

            var packs = project.read_packs_id_info_and_write_update(imported_projects_pack_id_info);
            packs.UnionWith(project.all_packs.Where(p => p.included));       //add included transmittable to the packs
            packs.UnionWith(project.constants_packs.Where(c => c.included)); //add included enums & constants sets to the packs


            //include packs that are not transmited but build a namespaces hierarchy
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

            foreach (var host in project.hosts)
                packs.UnionWith(host.pack_impl.Keys.Where(symbol1 => project.entities[symbol1] is HostImpl.PackImpl).Select(symbol2 =>
                                                                                                                            {
                                                                                                                                var pack = (HostImpl.PackImpl)project.entities[symbol2];
                                                                                                                                pack._included = true;
                                                                                                                                return pack;
                                                                                                                            }));

            project.packs = packs.OrderBy(pack => pack.full_path).ToList(); //save all used packs


            #region Detect redundant pack's language information.
            project.hosts.ForEach(
                                  host =>
                                  {
                                      packs.Clear(); // re-use

                                      foreach (var ch in project.channels.Where(ch => ch.hostL == host || ch.hostR == host))
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
                                                            .Select(exT => (HostImpl.PackImpl)project.entities[exT]).Distinct();


                                      //import used enums in the  host scope
                                      host.packs.AddRange(host_used_enums);
                                      host.packs = host.packs.Distinct().ToList();
                                  });


            foreach (var ch in project.channels) //Remove non-transmittable packs
            {
                ch.hostL_related_packs.RemoveAll(pack => !pack.is_transmittable);
                ch.hostR_related_packs.RemoveAll(pack => !pack.is_transmittable);
            }


            //To make packs that are present in every host's scope globally available, move them to the topmost scope of the project.
            if (project.hosts.All(host => 0 < host.packs.Count))
            {
                packs.Clear(); //re-use
                packs.UnionWith(project.hosts[0].packs);
                project.hosts.ForEach(host => packs.IntersectWith(host.packs));

                //now in the `packs` only globaly used packs, move them on the top by deleting from narrow hosts scope
                project.hosts.ForEach(host => host.packs.RemoveAll(pack => packs.Contains(pack)));
            }

            //delete on host scope registered packs, if they already register globaly
            project.hosts.ForEach(host => host.packs.RemoveAll(pack => project.project_constants_packs.Contains(pack)));
            #endregion


            #region set packs idx (storage place index)  and collect all fields
            HashSet<HostImpl.PackImpl.FieldImpl> fields = new();
            for (var idx = 0; idx < project.packs.Count; idx++)
            {
                var pack = project.packs[idx];
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

            foreach (var par_child in project.packs.GroupBy(pack => pack.parent_entity))
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
                                             project.symbol :
                                             parent.symbol,
                                         0 < nested_types.Length ?
                                             nested_types :
                                             "");
                    problem = true;
                }
            }

            if (problem) AdHocAgent.exit("Fix the problem and try again.", 22);


            var packs_with_parent = project.packs.Where(pack => pack._parent != null).ToArray();

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
            string orderBy(HostImpl.PackImpl.FieldImpl fld) => project.packs.First(pack => pack.fields.Contains(fld) || pack._static_fields_.Contains(fld)).full_path + fld._name;

            project.fields = fields.OrderBy(fld => orderBy(fld)).ToArray();

            for (var idx = 0; idx < project.fields.Length; idx++) project.fields[idx].idx = idx; //set fields  idx

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

        readonly HashSet<HostImpl.PackImpl> project_constants_packs = new(); //project scope constants packs

        bool collect_imported_entities(HashSet<ProjectImpl> path) //return true if collected collection size changed
        {
            path.Add(this);

            var fix_constants_count = project_constants_packs.Count;

            foreach (var symbol in project.add)
                if (project.entities.TryGetValue(symbol, out var item))
                    switch (item)
                    {
                        case ProjectImpl prj: //import all constants and value packs from

                            if (path.Contains(prj)) continue;

                            prj.collect_imported_entities(path);

                            project_constants_packs.UnionWith(prj.project_constants_packs);

                            continue;

                        case HostImpl.PackImpl pack:

                            if (pack.is_constants_set || pack.is_enum)
                                if (project_constants_packs.Add(pack) && !constants_packs.Contains(pack))
                                    constants_packs.Add(pack);

                            continue;
                        case ChannelImpl channel:
                            channel._included = true;
                            continue;
                    }
                else AdHocAgent.LOG.Warning("A project can only import other projects/enums/constants/values packs but {Symbol} is not this type, its import will be skipped", symbol);

            path.Remove(this);
            return project_constants_packs.Count != fix_constants_count;
        }


        // In the root project, root_project == null, but the 'project' field points to itself.
        public ProjectImpl(ProjectImpl? root_project, CSharpCompilation compilation, InterfaceDeclarationSyntax node, string namespace_) : base(null, compilation, node)
        {
            file_path = node.SyntaxTree.FilePath;
            if (root_project != null) //not root project
                project = root_project;

            add_from(symbol!);

            _namespacE = namespace_;
            this.node = node;
        }

        public string? _task => AdHocAgent.task;

        public string? _namespacE { get; set; }

        public long _time { get; set; }

        public byte[] source = Array.Empty<byte>();
        public object? _source() => source;
        public int _source_len => source.Length;
        public byte _source(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => source[item];
        public List<HostImpl> hosts = new(3);

        public int _hosts_len => hosts.Count;
        public object? _hosts() => hosts;
        public Project.Host _hosts(Context.Transmitter ctx, Context.Transmitter.Slot slot, int d) => hosts[d];


        //all fields - include virtual V field - used as Value of Map datatype
        IEnumerable<HostImpl.PackImpl.FieldImpl?> all_fields() => raw_fields.Values.Concat(raw_fields.Values.Select(fld => fld.V).Where(fld => fld != null)).Distinct();

        public Dictionary<ISymbol, HostImpl.PackImpl.FieldImpl> raw_fields = new Dictionary<ISymbol, HostImpl.PackImpl.FieldImpl>(SymbolEqualityComparer.Default);


        public HostImpl.PackImpl.FieldImpl[] fields = Array.Empty<HostImpl.PackImpl.FieldImpl>();

        public object? _fields() => 0 < fields.Length ?
                                        fields :
                                        null;

        public int _fields_len => fields.Length;
        public Project.Host.Pack.Field _fields(Context.Transmitter ctx, Context.Transmitter.Slot slot, int d) => fields[d];


        public readonly List<HostImpl.PackImpl> all_packs = new();
        public readonly List<HostImpl.PackImpl> constants_packs = new(); //enums + constant sets


        public List<HostImpl.PackImpl> packs;

        public object? _packs() => 0 < packs.Count ?
                                       packs :
                                       null;

        public int _packs_len => packs.Count;
        public Project.Host.Pack _packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int d) => packs[d];

        public List<ChannelImpl> channels = new();

        public int _channels_len => channels.Count;

        public object? _channels() => channels.Count < 1 ?
                                          null :
                                          channels;

        public Project.Channel _channels(Context.Transmitter ctx, Context.Transmitter.Slot slot, int d) => channels[d];

        public void Sent(Communication.Transmitter via)
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
                _default_impl_hash_equal = 0xFFFF_FFFF; // Default: Automatically generate hash code and equals methods implementation. One bit per language.
                project.hosts.Add(this);
            }

            #region pack_impl_hash_equal
            public readonly Dictionary<ISymbol, uint> pack_impl = new Dictionary<ISymbol, uint>(SymbolEqualityComparer.Default); //pack -> impl information
            private Dictionary<ISymbol, uint>.Enumerator pack_impl_enum;

            public object? _pack_impl_hash_equal() => pack_impl.Count == 0 ?
                                                          null :
                                                          pack_impl;

            public int _pack_impl_hash_equal_len => pack_impl.Count;
            public void _pack_impl_hash_equal_Init(Context.Transmitter ctx, Context.Transmitter.Slot slot) => pack_impl_enum = pack_impl.GetEnumerator();

            public ushort _pack_impl_hash_equal_NextItem_Key(Context.Transmitter ctx, Context.Transmitter.Slot slot)
            {
                pack_impl_enum.MoveNext();
                return (ushort)project.entities[pack_impl_enum.Current.Key].idx;
            }

            public uint _pack_impl_hash_equal_Val(Context.Transmitter ctx, Context.Transmitter.Slot slot) => pack_impl_enum.Current.Value;
            public uint _default_impl_hash_equal { get; set; } //by default a bit per language
            #endregion


            #region field_impl
            public readonly Dictionary<ISymbol, Project.Host.Langs> field_impl = new Dictionary<ISymbol, Project.Host.Langs>(SymbolEqualityComparer.Default);
            private Dictionary<ISymbol, Project.Host.Langs>.Enumerator field_impl_enum;


            public object? _field_impl() => field_impl.Count == 0 ?
                                                null :
                                                field_impl;

            public int _field_impl_len => field_impl.Count;
            public void _field_impl_Init(Context.Transmitter ctx, Context.Transmitter.Slot slot) => field_impl_enum = field_impl.GetEnumerator();

            public ushort _field_impl_NextItem_Key(Context.Transmitter ctx, Context.Transmitter.Slot slot)
            {
                field_impl_enum.MoveNext();
                return (ushort)project.raw_fields[field_impl_enum.Current.Key].idx;
            }

            public Project.Host.Langs _field_impl_Val(Context.Transmitter ctx, Context.Transmitter.Slot slot) => field_impl_enum.Current.Value;
            #endregion

            public List<PackImpl> packs = new(); // Host-dedicated packs

            public object? _packs() => 0 < packs.Count ?
                                           packs :
                                           null;

            public int _packs_len => packs.Count;
            public ushort _packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => (ushort)packs[item].idx;


            public ChannelImpl.StageImpl[] stages;


            public static void init(ProjectImpl project) //host
            {
                HashSet<PackImpl> packs = new();

                foreach (var ch in project.channels)
                {
                    //validate ports.
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
                public ushort _id { get; set; }

                public virtual ushort? _parent => parent_entity switch
                {
                    PackImpl pack => (ushort)pack.idx,
                    HostImpl host => (ushort?)project.constants_packs.Find(pack => equals(pack.symbol!, host.symbol!))?.idx, //the parent is fake "port" pack
                    _ => null
                };

                public ushort? _nested_max { get; set; }
                public bool _referred { get; set; }
                public List<FieldImpl> fields = new();

                public object? _fields() => 0 < fields.Count ?
                                                fields :
                                                null;

                public int _fields_len => fields.Count;
                public int _fields(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => fields[item].idx;

                public List<FieldImpl> _static_fields_ = new();

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
                    add_from(symbol!);

                    project.all_packs.Add(this);
                }
                #endregion

                public PackImpl(ProjectImpl project, INamedTypeSymbol? symbol, string name) : base(project, null) // Container pack from port to provide parent-children path for packs declared inside
                {
                    _name = name; //for name in path only
                    this.symbol = symbol;

                    _id = (int)Project.Host.Pack.Field.DataType.t_constants; // Container pack
                    _included = true;
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

                    project.all_packs.ForEach(pack =>
                                              {
                                                  path.Clear();
                                                  pack.inheritance_depth = pack.calculate_inheritance_depth(path, 0);
                                              });

                    foreach (var pack in project
                                         .all_packs
                                         .Where(pack => !pack.is_typedef)
                                         .OrderBy(pack => pack.inheritance_depth)) //packs without inheretace go first
                    {
                        path.Clear();
                        pack.collect_fields(path);

                        path.Clear();
                        pack._nested_max = (ushort)pack.calculate_fields_type_depth(path);
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


                        #region propagate enum params to fields where it used
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
                            var null_value = (long)((byte)enum_._static_fields_.Count);
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

                    #region _DefaultMaxLengthOf reading, delete and apply
                    var all_default_collection_capacity = project.constants_packs.Where(en => en._name.Equals("_DefaultMaxLengthOf")).ToArray();

                    foreach (var pack in all_default_collection_capacity.OrderBy(en => en.in_project == project)) //The root project settings should be placed last in order to override all inherited project settings
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
                        en._static_fields_.ForEach(fld => project.raw_fields.Remove(fld.symbol!));
                        project.constants_packs.Remove(en);
                    }

                    //apply acquired default length settings
                    foreach (var fld in project.all_fields())
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


                    project.constants_packs.RemoveAll(enum_ => enum_._id == (ushort)Project.Host.Pack.Field.DataType.t_subpack); // remove marked to delete enums
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

                private int cyclic_depth;

                private int calculate_fields_type_depth(ISet<PackImpl> path)
                {
                    if (path.Count == 0) cyclic_depth = 0;

                    try
                    {
                        foreach (var datatype in fields.Where(f => f.exT_pack != null).Select(f => (PackImpl)project.entities[f.exT_pack!])
                                                       .Concat(fields.Where(f => f.V != null && f.V.exT_pack != null).Select(f => (PackImpl)project.entities[f.V!.exT_pack!])).Distinct())
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
                        foreach (var fld in fields.Where(f => f.exT_pack != null && !project.entities.ContainsKey(f.exT_pack!))
                                                  .Concat(fields.Where(f => f.V != null && f.V.exT_pack != null && !project.entities.ContainsKey(f.V.exT_pack!))))
                            AdHocAgent.LOG.Error("Line {line}: Unsupported field type '{type}' detected for field '{field}'.", fld.line_in_src_code, fld.exT_pack, fld);
                        AdHocAgent.exit("", 23);
                    }

                    return path.Count == 0 ?
                               cyclic_depth :
                               0;
                }


                private List<FieldImpl> collect_fields(ISet<PackImpl> path)
                {
                    //add fields from inherited and added packs
                    var add_packs = add.Select(sym => (PackImpl)project.entities[sym]).Where(pack => pack != this && !path.Contains(pack));

                    if (add_packs.Any())
                    {
                        path.Add(this);

                        foreach (var pack in add_packs)
                            foreach (var fld in pack.collect_fields(path).Where(fld => !exists(fld._name)))
                                fields.Add(fld);

                        path.Remove(this);
                    }

                    add.Clear();

                    //add individual fields
                    foreach (var fld in add_fld.Select(sym => project.raw_fields[sym]).Where(fld => !exists(fld._name)))
                        fields.Add(fld);

                    add_fld.Clear();


                    //del individual fields
                    foreach (var fld in del_fld.Select(sym => project.raw_fields[sym]))
                        fields.Remove(fld);

                    del_fld.Clear();

                    return fields;
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
                    public Entity parent_entity => project.entities[symbol.OriginalDefinition.ContainingType];

                    public ISymbol? substitute_value_from;
                    public int line_in_src_code => symbol!.Locations[0].GetLineSpan().StartLinePosition.Line + 1;
                    public readonly ISymbol? symbol;
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
                        if (project.entities[symbol!.ContainingType] is PackImpl pack) pack._static_fields_.Add(this);
                        else AdHocAgent.exit($"`{project.entities[symbol!.ContainingType].full_path}` cannot contains any fields. Delete `{_name}`.");

                        project.raw_fields.Add(symbol, this);

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

                        project.raw_fields.Add(symbol, this);

                        if (symbol is IFieldSymbol fld && (is_const = fld.IsStatic || fld.IsConst)) //  static/const field
                        {
                            if (project.entities[symbol!.ContainingType] is PackImpl pack) pack._static_fields_.Add(this);
                            else AdHocAgent.exit($"`{project.entities[symbol!.ContainingType].full_path}` cannot contains any fields. Delete `{_name}`.");

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
                            if (project.entities[symbol!.ContainingType] is PackImpl pack) pack.fields.Add(this);
                            else AdHocAgent.exit($"`{project.entities[symbol!.ContainingType].full_path}` cannot contains any fields. Delete `{_name}`.");


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

                            if (T.ToString()!.StartsWith("org.unirail.Meta.Set<"))
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
                            else if (T.ToString()!.StartsWith("org.unirail.Meta.Map<"))
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
                                _value_int = (long?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_uint32;
                                _value_bytes = 4;
                                break;
                            case SpecialType.System_Int64:
                                _value_int = (long?)constant;
                                exT_primitive = inT = (int)Project.Host.Pack.Field.DataType.t_int64;
                                _value_bytes = 8;
                                break;
                            case SpecialType.System_UInt64:
                                _value_int = (long?)constant;
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
                                                           (PackImpl)project.entities[exT_pack];

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

                        AdHocAgent.LOG.Error("The MinMax attribute for Field {field} (line: {line}) cannot be used with VarInt attributes[X, V, A]. Use VarInt attributes to set MinMax restrictions.", _name, line_in_src_code);
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
                            var fld = project.raw_fields[model.GetSymbolInfo(src).Symbol!];
                            return (fld.substitute_value_from == null ?
                                        fld :
                                        project.raw_fields[fld.substitute_value_from])._value_int!.Value; //return sudstitute value
                        }

                        try { return Convert.ToInt64(model.GetConstantValue(src).Value); }
                        catch (Exception) { return Convert.ToUInt64(model.GetConstantValue(src).Value); }
                    }

                    private double dbl_val(ExpressionSyntax src)
                    {
                        if (!src.IsKind(SyntaxKind.IdentifierName)) return Convert.ToDouble(model.GetConstantValue(src).Value);

                        var fld = project.raw_fields[model.GetSymbolInfo(src).Symbol!];
                        return (fld.substitute_value_from == null ?
                                    fld :
                                    project.raw_fields[fld.substitute_value_from])._value_double!.Value; //return sudstitute value
                    }

                    #region dims
                    public int[]? dims;

                    public object? _dims() => dims;
                    public int _dims_len => dims!.Length;
                    public int _dims(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => dims![item];
                    #endregion


                    public static void init(ProjectImpl project)
                    {
                        var fields = project.raw_fields.Values;
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
                                var dst_const_fld = project.raw_fields[fld.model.GetSymbolInfo(args_list.Arguments[0].Expression).Symbol!];
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
                                                        var val = (uint)(FLD!.value_of(_exp.Operand));

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
                                                            AdHocAgent.LOG.Error("The `[D]` attribute argument `{arg}`, without prefix character, specifies the maximum length of an array. However, the field ‘{field}’ on line {line}’ does not have an array declaration such as ‘[]’, ‘[,]’, or ‘[,,]’ .", exp, FLD, fld.line_in_src_code);
                                                            AdHocAgent.exit("Please specify array type and retry", -1);
                                                        }

                                                        if (FLD!._exT_array != null)                              //fully correct using argument
                                                            FLD!._exT_array |= (uint)(FLD.value_of(exp)) << 3;     //take the max length of the array from `exp`
                                                        else                                                       //not fully correct but ok
                                                            FLD!._map_set_array |= (uint)(FLD.value_of(exp)) << 3; //take the max length of the array of Map/Set/Array collection from the `exp`
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

                                                                FLD!._exT_array |= (uint?)(dims[0] >> 1 << 3); //take the max length of the array from dims[0]
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
                            var src_fld = project.raw_fields[dst_fld.substitute_value_from!]; //takes type and value from src
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
            public ChannelImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax Channel) : base(project, compilation, Channel) //struct based
            {
                project.channels.Add(this);

                var interfaces = symbol!.Interfaces;
                if (interfaces.Length == 0 || !interfaces[0].Name.Equals("ChannelFor"))
                {
                    AdHocAgent.LOG.Error("The channel {channel} should implement the {interface} interface and connect two distinct hosts.", symbol, "org.unirail.Meta.ChannelFor<HostA,HostB>");
                    AdHocAgent.exit("Fix the problem and restart");
                }

                if (parent_entity is not ProjectImpl) AdHocAgent.exit($"The definition of channel {symbol} should be placed directly within a project scope.");

                if (!equals(symbol!.Interfaces[0].TypeArguments[0], symbol!.Interfaces[0].TypeArguments[1])) return;

                AdHocAgent.LOG.Error("The channel {ch} should connect two separate hosts.", symbol);
                AdHocAgent.exit("Fix the problem and restart");
            }


            public override bool included => _included ?? in_project.included;

            internal ITypeSymbol L => symbol!.Interfaces[0].TypeArguments[0];
            internal ITypeSymbol R => symbol!.Interfaces[0].TypeArguments[1];


            internal HostImpl hostL => (HostImpl)project.entities[L];
            public ushort _hostL => (ushort)hostL.idx;

            public List<HostImpl.PackImpl> hostL_transmitting_packs = new();

            public object? _hostL_transmitting_packs() => hostL_transmitting_packs.Count == 0 ?
                                                              null :
                                                              hostL_transmitting_packs;

            public int _hostL_transmitting_packs_len => hostL_transmitting_packs.Count;
            public ushort _hostL_transmitting_packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => (ushort)hostL_transmitting_packs[item].idx;


            public List<HostImpl.PackImpl> hostL_related_packs = new();

            public object? _hostL_related_packs() => hostL_related_packs.Count == 0 ?
                                                         null :
                                                         hostL_related_packs;

            public int _hostL_related_packs_len => hostL_related_packs.Count;
            public ushort _hostL_related_packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => (ushort)hostL_related_packs[item].idx;


            internal HostImpl hostR => (HostImpl)project.entities[R];
            public ushort _hostR => (ushort)hostR.idx;

            public List<HostImpl.PackImpl> hostR_transmitting_packs = new();

            public object? _hostR_transmitting_packs() => hostR_transmitting_packs.Count == 0 ?
                                                              null :
                                                              hostR_transmitting_packs;

            public int _hostR_transmitting_packs_len => hostR_transmitting_packs.Count;
            public ushort _hostR_transmitting_packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => (ushort)hostR_transmitting_packs[item].idx;


            public List<HostImpl.PackImpl> hostR_related_packs = new();

            public object? _hostR_related_packs() => hostR_related_packs.Count == 0 ?
                                                         null :
                                                         hostR_related_packs;

            public int _hostR_related_packs_len => hostR_related_packs.Count;
            public ushort _hostR_related_packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => (ushort)hostR_related_packs[item].idx;


            public List<StageImpl> stages = new();

            public object? _stages() => stages.Count == 0 ?
                                            null :
                                            stages;

            public int _stages_len => stages.Count;
            public Project.Channel.Stage _stages(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => stages[item];


            private bool inited;

            public void Init() //channel
            {
                if (inited) return; //double inited protection
                inited = true;

                if (stages.Count == 0)
                {
                    //The communication channel with a completely empty body. Constructing the default channel stages.

                    var stage = new StageImpl();
                    stage._name = "Init";
                    stage.branchesL.Add(new BranchImpl()
                    {
                        packs = project.all_packs.Where(pack =>
                                                        {
                                                            var host = pack.in_host;
                                                            return host == null || pack.in_host == hostL;
                                                        }).ToList()
                    });

                    stage.branchesR.Add(new BranchImpl()
                    {
                        packs = project.all_packs.Where(pack =>
                                                        {
                                                            var host = pack.in_host;
                                                            return host == null || pack.in_host == hostR;
                                                        }).ToList()
                    });
                    stages.Add(stage);
                }

                stages.ForEach(stage => stage.Init());

                if (1 < symbol!.Interfaces.Length) //channel import other channel
                {
                    //inheritance detected
                    var count = this.stages.Count;

                    var stages = this.stages.ToDictionary(stage => stage.symbol!.Name);

                    var SwapHosts = false;
                    foreach (var entity in symbol!.Interfaces.Skip(1))
                        if (project.entities[
                                             (SwapHosts = entity.Name.Equals("SwapHosts")) ?
                                                 entity.TypeArguments[0] :
                                                 entity
                                            ] is ChannelImpl ch_ancestor)
                        {
                            ch_ancestor.Init();

                            var imported_stages = ch_ancestor.stages.Where(s => !stages.ContainsKey(s.symbol!.Name)).ToArray();


                            if (SwapHosts)
                                foreach (var stage in imported_stages)
                                {
                                    var s = stage.clone();
                                    s.branchesL = stage.branchesR;
                                    s.branchesR = stage.branchesL;

                                    this.stages.Add(s);
                                }
                            else
                                this.stages.AddRange(imported_stages);
                        }
                        else
                        {
                            AdHocAgent.LOG.Error("The communication channel {channel} can only inherit from other channels. However, {entity} is not a channel.", this, entity);
                            AdHocAgent.exit("Fix the problem and restart");
                        }

                    //re link stage's branches

                    foreach (var stage in this.stages.GetRange(count, this.stages.Count - count))
                    {
                        var branches = stage.branchesL.Concat(stage.branchesR).ToArray();
                        for (var i = 0; i < branches.Length; i++)
                        {
                            var branch = branches[i];
                            var name = branch.goto_stage!._name;
                            if (branch.goto_stage == null || !stages.ContainsKey(name) || stages[name] == branch.goto_stage) continue;
                            branch = branches[i] = branch.clone();
                            branch.goto_stage = stages[name];
                        }
                    }
                }

                var idx = 0;
                stages.ForEach(stage => stage.idx = idx++);
            }


            public class StageImpl : Entity, Project.Channel.Stage
            {
                public static readonly StageImpl Exit = new();


                internal StageImpl() : base(null, null, null) { _name = ""; }


                internal StageImpl(ProjectImpl project, CSharpCompilation compilation, InterfaceDeclarationSyntax stage) : base(project, compilation, stage)
                {
                    _name = symbol!.Name;

                    if (parent_entity is not ChannelImpl)
                    {
                        AdHocAgent.LOG.Error("The declaration of stage {stage} should be nested within a channel scope.", symbol);
                        AdHocAgent.exit("Fix the problem and try again");
                    }

                    add_from(symbol!);
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

                public void Init()
                {
                    if (symbol == null) return;

                    var txt = node.SyntaxTree.ToString();


                    List<BranchImpl>? branches = null;
                    foreach (var item in node!.BaseList!.Types)
                    {
                        var str = item.ToString();
                        switch (str)
                        {
                            case "L":
                                branches = branchesL;
                                continue;
                            case "R":
                                branches = branchesR;
                                continue;
                            default:
                                if (str.StartsWith("_<")) //Branch start
                                {
                                    var s = item.SpanStart;
                                    var branch = new BranchImpl()
                                    {
                                        uid_pos = s + 2, // "_<".length
                                        _doc = string.Join(' ', item.GetLeadingTrivia().Select(t => get_doc(t)))
                                    };
                                    branches.Add(branch);

                                    #region getting branch's /*UID*/ and inline comments
                                    var rn = txt.IndexOf('\n', s);
                                    if (rn == -1) rn = txt.IndexOf('\r', s);

                                    var comments_after_branch_declare = item.DescendantTrivia().Where(
                                                                                                      t =>
                                                                                                          (t.IsKind(SyntaxKind.MultiLineCommentTrivia) || t.IsKind(SyntaxKind.SingleLineCommentTrivia)) &&
                                                                                                          s < t.SpanStart &&
                                                                                                          t.SpanStart < rn
                                                                                                     );
                                    if (comments_after_branch_declare.Any())
                                    {
                                        var m = HasDocs.uid.Match(comments_after_branch_declare.First().ToString());
                                        if (m.Success)
                                        {
                                            branch.uid = (ushort)str2int(m.Groups[1].Value);
                                            comments_after_branch_declare = comments_after_branch_declare.Skip(1);
                                        }
                                    }

                                    branch._doc += string.Join(' ', comments_after_branch_declare.Select(t => get_doc(t)));
                                    #endregion

                                    void set_goto(StageImpl goto_stage)
                                    {
                                        if (branch.goto_stage != null)
                                        {
                                            AdHocAgent.LOG.Error("There are multiple unexpected stages {stage1} and {stage2} within a branch of the {stage}  while there should be only one.", branch.goto_stage.symbol, goto_stage.symbol, symbol);
                                            AdHocAgent.exit("Fix the problem and restart.");
                                        }

                                        branch.goto_stage = goto_stage;
                                    }

                                    var nodes = item.DescendantNodes().Where(n => !(n is GenericNameSyntax || n is TypeArgumentListSyntax)).ToArray();

                                    for (var i = 0; i < nodes.Length; i++)
                                    {
                                        var node = nodes[i];

                                        var d = node.ToString().Count(ch => ch == '.'); // QualifiedNameSyntax  --- > "Agent.Version"
                                        if (0 < d) i += d + 1;                                 // skip repeated for additional, separated  Agent and Version

                                        var node_symbol = model.GetSymbolInfo(node).Symbol;

                                        if (node_symbol.Name.Equals("Exit") && node_symbol.ToString()!.StartsWith("org.unirail"))
                                        {
                                            Exit.symbol = (INamedTypeSymbol?)node_symbol;
                                            set_goto(Exit);
                                        }
                                        else if (project.named_packs.TryGetValue((INamedTypeSymbol)node_symbol, out var pks))
                                        {
                                            foreach (var pack in pks)
                                                if (!branch.packs.Contains(pack))
                                                    branch.packs.Add(pack);
                                        }
                                        else
                                            switch (project.entities[node_symbol])
                                            {
                                                case HostImpl.PackImpl pack:
                                                    branch.packs.Add(pack);
                                                    var doc = item.GetLeadingTrivia();
                                                    if (!pack.is_transmittable) //exclude none transmittable enums and constants - they are going to every hosts scope)
                                                    {
                                                        AdHocAgent.LOG.Error("The {pack} (like:{line}) on the stage {stage} is not transmittable.",
                                                                             pack, node.GetLocation().GetLineSpan().StartLinePosition.Line + 1, symbol);
                                                        AdHocAgent.exit("Delete the pack and restart");
                                                    }

                                                    continue;

                                                case StageImpl stage:
                                                    set_goto(stage);
                                                    continue;
                                                default:

                                                    AdHocAgent.LOG.Error("Unexpected item {item} (like:{line}) in the {stage} declaration",
                                                                         node_symbol, node.GetLocation().GetLineSpan().StartLinePosition.Line + 1, symbol);
                                                    AdHocAgent.exit("Fix the problem and restart.");
                                                    return;
                                            }
                                    }

                                    continue;
                                }


                                AdHocAgent.LOG.Error("The stage {stage} may only have either the {L} or {R} side. The presence of the {item} is unacceptable.", symbol, "Meta.L", "Meta.R", item);
                                AdHocAgent.exit("Fix the problem and try again");
                                continue;
                        }
                    }
                }

                private ushort timeout = 0xFFFF;

                public ushort _timeout { get => timeout; set => timeout = value; }

                public List<BranchImpl> branchesL = new();
                public object? _branchesL() => branchesL;
                public int _branchesL_len => branchesL.Count;
                public Project.Channel.Stage.Branch _branchesL(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => branchesL[item];

                public List<BranchImpl> branchesR = new();
                public object? _branchesR() => branchesR;
                public int _branchesR_len => branchesR.Count;
                public Project.Channel.Stage.Branch _branchesR(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => branchesR[item];
                public StageImpl clone() => (StageImpl)MemberwiseClone();
            }

            public class BranchImpl : Project.Channel.Stage.Branch
            {
                public ushort uid = ushort.MaxValue;
                public ushort _uid => uid;

                public int uid_pos;

                public string? _doc { get; set; }

                public StageImpl? goto_stage; //if null Exit stage
                public BranchImpl clone() => (BranchImpl)MemberwiseClone();

                public ushort _goto_stage => goto_stage == StageImpl.Exit ? (ushort)Project.Channel.Stage.Type.Exit :
                                             goto_stage == null ? (ushort)Project.Channel.Stage.Type.None :
                                                                            (ushort)goto_stage.idx;


                public List<HostImpl.PackImpl> packs = new();


                public object? _packs() => packs.Count == 0 ?
                                               null :
                                               packs;

                public int _packs_len => packs.Count;
                public ushort _packs(Context.Transmitter ctx, Context.Transmitter.Slot slot, int item) => (ushort)packs[item].idx;
            }
        }
    }
}