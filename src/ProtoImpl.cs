using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using org.unirail.Agent;

namespace org.unirail;

/// <summary>
/// Protocol Buffers (<c>.proto</c>) → AdHoc protocol-description conversion. The converted result rides to the
/// Server in the <see cref="AdHocProtocol.Agent_.Proto"/> pack, which the description now declares as
/// <c>[Zstd] [D(+5_120_000)] string proto</c>.
/// <para>
/// A plain <c>string</c> field, so the whole <c>.proto</c> tree is merged into ONE protocol-description document
/// and simply assigned: the framework's string leaf does the varint-char encoding this code used to hand-roll,
/// and the field's chain compresses it. Nothing is staged on disk — the former temp files and 7-Zip archive
/// existed only because a single payload could not carry several named entries. Hence this type holds no
/// instance state at all; the pack is concrete and is built inline in <see cref="Process"/>.
/// </para>
/// </summary>
public static class ProtoImpl
{
    /// <summary>
    /// This field's IDL <c>[D(+N)]</c> budget, in CHARACTERS — taken from the generated pack so it cannot drift
    /// from the description. Checked at conversion time so an over-long document is refused with a clear message:
    /// the transmit-side string leaf writes whatever length it is handed, and the cap is enforced only by the
    /// Server's decoder, which would reject the upload as a protocol error instead.
    /// </summary>
    const int PROTO_MAX_CHARS = AdHocProtocol.Agent_.Proto.proto__.STR_LEN_MAX;

    // ============================================================================================
    //  Moved from AdHocAgent.cs: Protocol Buffers (.proto) file(s) → AdHoc protocol-description format
    // ============================================================================================

    /// <summary>
    /// Entry point for the `.proto` / directory conversion task: builds the import set, converts, and
    /// uploads the result as an <see cref="AdHocProtocol.Agent_.Proto"/> pack. <paramref name="paths"/> is the raw CLI
    /// argument list (paths[0] = provided path; trailing args = import dirs and/or destination dir).
    /// </summary>
    public static async Task Process(string[] paths)
    {
        var provided_path = AdHocAgent.provided_path;

        if (1 < paths.Length && !paths[^1].EndsWith(".proto")) //if destination_dir_path explicitly provided instead of Directory.GetCurrentDirectory()
            if (!Directory.Exists(AdHocAgent.destination_dir_path = paths[^1]))
                Directory.CreateDirectory(AdHocAgent.destination_dir_path);

        var is_file = File.Exists(provided_path);

        if (!is_file && !Directory.Exists(provided_path))
            AdHocAgent.exit($"Provided path {provided_path} does not exists.", 2);

        var import_dirs = 1 < paths.Length && !paths[^1].EndsWith(".proto") ?
                              paths[1..^1] : // last arg is destination dir, exclude it
                              paths[1..];    // all remaining args are import search paths

        // Materialised: the set is walked twice below, and a re-walk could disagree with itself.
        var source_files = is_file ?
                               [provided_path] :
                               Directory.EnumerateFiles(provided_path, "*.proto", SearchOption.AllDirectories).ToList();

        var all_files = new Dictionary<string, List<string>>();

        foreach (var file in source_files
                .Concat(import_dirs.SelectMany(dir => Directory.EnumerateFiles(dir, "*.proto", SearchOption.AllDirectories))))
        {
            var key = Path.GetFileName(file);
            if (all_files.TryGetValue(key, out var val))
                val.Add(file);
            else all_files[key] = [.. new[] { file }];
        }

        // The whole tree becomes ONE document: a single conduit carries a single payload, and packages keep the
        // structure that the former per-directory split used to carry in archive entry names.
        var document = process_proto_files(source_files, all_files);

        // An empty conversion used to be caught twice: per directory (`if (str.Length == 0) return;`) and in
        // aggregate (`if (files.Count == 0)`). Merging the whole tree into one document leaves this the only
        // place to catch it — and the pack transmits any non-null `proto`, so an empty payload would otherwise
        // reach the Server indistinguishable from a successful conversion.
        if (document.Length == 0)
            AdHocAgent.exit($"No useful information found at the path: {provided_path}");

        if (PROTO_MAX_CHARS < document.Length)
            AdHocAgent.exit($"The converted description of {provided_path} takes {document.Length} characters, over the {PROTO_MAX_CHARS} the `proto` field accepts. Convert a smaller subtree.", 2);

        await ToServer.Start(new AdHocProtocol.Agent_.Proto
                             {
                                 task  = AdHocAgent.task,
                                 name  = Path.GetFileName(provided_path),
                                 proto = document
                             });
    }

    /// <summary>
    /// Merges a set of `.proto` files (resolving `import` directives transitively) into a single AdHoc
    /// protocol-description string, grouping declarations by package into nested <c>struct</c>s.
    /// </summary>
    static string process_proto_files(IEnumerable<string> files, Dictionary<string, List<string>> all_files)
    {
        var syntax       = new Regex(@"^\s*syntax\s*=\s*.*;",                        RegexOptions.Multiline);
        var imports      = new Regex(@"^\s*import\s+(?:public\s+)?""([^""]+)""\s*;",  RegexOptions.Multiline); // import "myproject/other_protos.proto"; / import public "new.proto";
        var package      = new Regex(@"^\s*package\s+[""']?([^""';\s]+)[""']?\s*;",   RegexOptions.Multiline); // package foo.bar;
        var dot          = new Regex(@"(?<=\s)\.(?=[^\.\s])",                         RegexOptions.Multiline); // leading-dot in type refs: `.tensorflow.DataType` → `tensorflow.DataType`
        var asterisk     = new Regex(@"(?<=^\s*//.*)\*(?=\*/|\/*)",                   RegexOptions.Multiline); // neutralise `*/` in `//` comments so a stray close can't terminate a downstream /** */ doc comment
        var option_block = new Regex(
                                     @"^\s*option\s+[^;]+\s*=\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}\s*;\s*\n?",
                                     RegexOptions.Multiline | RegexOptions.Singleline
                                    );

        var imported      = new HashSet<string>();
        var package_proto = new Dictionary<string, StringBuilder>();

        void process_proto_file(string proto_file_path)
        {
            if (!imported.Add(proto_file_path)) return;
            var proto = File.ReadAllText(proto_file_path);

            var pack    = package.Matches(proto).Select(m => m.Groups[1].Value).FirstOrDefault() ?? "";
            var matches = imports.Matches(proto); // capture imports before the strip pass below removes them

            var file_name = HasDocs.brush(Path.GetFileName(proto_file_path));
            proto = $"\n//@@#region {file_name}\n" + proto + $"\n//@@#endregion {file_name}\n";

            proto = package.Replace(syntax.Replace(imports.Replace(option_block.Replace(proto, ""), ""), ""), "");

            if (!package_proto.TryGetValue(pack, out var bucket)) package_proto[pack] = bucket = new StringBuilder();
            bucket.Append(proto);

            foreach (var match in matches.OrderBy(Match => -Match.Index)) //bottom ==> up
            {
                var import = match.Groups[1].Value;

                var import_file = "";
                if (all_files.TryGetValue(Path.GetFileName(import), out var candidates))
                    if (candidates.Count == 1)
                        import_file = candidates[0];
                    else
                    {
                        while (!candidates.Any(p => p.Replace('\\', '/').EndsWith(import)))
                            import = import[(import.IndexOf('/', 1) + 1)..];

                        import_file = candidates.First(p => p.Replace('\\', '/').EndsWith(import));
                    }
                else
                    AdHocAgent.exit($"The .proto file `{import}` does not exist and cannot be imported into .proto file `{proto_file_path}`.");

                process_proto_file(import_file);
            }
        }

        foreach (var file in files) process_proto_file(file);
        var result_proto = new StringBuilder();

        repeat:
        while (0 < package_proto.Count)
            foreach (var (key, value) in package_proto.OrderBy(p => -p.Key.Count(conn => conn == '.')))
            {
                var path = key.Split('.');
                // Trailing "\n" is REQUIRED: without it, two adjacent packages concatenate as
                //   //@@}//@@public struct bcl {
                // on the same line. The proto lexer then treats the whole run as a single line
                // comment, so the server's xmlDocify only strips the FIRST //@@ and the second
                // package's opening brace is emitted as a literal comment.
                var code = $$"""
                             //@@public struct {{(path[^1] == "" ? "MyPack" : HasDocs.brush(path[^1]))}} {
                               {{value}}
                             //@@}
                             """ + "\n";
                package_proto.Remove(key);

                if (1 < path.Length)
                {
                    var key2 = string.Join('.', path, 0, path.Length - 1);
                    if (package_proto.TryGetValue(key2, out var existing))
                        existing.Insert(0, code);
                    else
                        package_proto[key2] = new StringBuilder(code);
                    goto repeat;
                }

                result_proto.Append(code);
            }

        var final_proto = dot.Replace(result_proto.ToString(), ""); //removing the dot at the beginning of types
        //				                          .tensorflow.DataType  [] dtype;
        //                               repeated .tensorflow.TensorProto tensor = 1;

        final_proto = asterisk.Replace(final_proto, "⁕"); // replace '*' with '⁕' in comments like
        //  // */ <- This should not close the generated doc comment

        return final_proto.Length == 0 ?
                   "" :
                   $"""
                    syntax = "proto3";
                    {final_proto}
                    """;
    }
}
