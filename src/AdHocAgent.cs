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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Tommy;

//https://github.com/dezhidki/Tommy - TOML parser library

namespace org.unirail{
    /// <summary>
    /// Static class responsible for AdHoc Agent operations, including protocol description processing,
    /// code generation, and deployment.
    /// </summary>
    static class AdHocAgent{
        /// <summary>
        /// Serilog enricher to add file path and line number to log events.
        /// </summary>
        class CallerEnricher : ILogEventEnricher{
            StringBuilder sb = new();

            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                // Captures stack frame information, skipping the enricher itself and Serilog internals.
                var stack = new StackTrace(true) // true to capture file information
                            .GetFrames().Skip(1).FirstOrDefault(stack => stack.GetFileName() != null);

                logEvent.AddPropertyIfAbsent(new LogEventProperty("FileLine", new ScalarValue(sb.Clear()
                                                                                                .Append(stack!.GetFileName())
                                                                                                .Append(":line ")
                                                                                                .Append(stack.GetFileLineNumber())
                                                                                                .ToString())));
            }
        }

        /// <summary>
        /// Global logger instance for the AdHoc Agent, configured with console output and caller information.
        /// Uses Serilog for structured logging: https://github.com/serilog/serilog/wiki/Getting-Started
        /// </summary>
        public static readonly Logger LOG = new LoggerConfiguration()
                                            .Enrich.With<CallerEnricher>()                                                                  // Add file path and line number to logs
                                            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message} in {FileLine}\n") // Configure console output format
                                            .CreateLogger();

        // True when stdout is a real terminal that can render ANSI escape sequences
        // (colors + box-drawing). Disabled when output is redirected or NO_COLOR is set.
        static readonly bool color_ok = !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") == null;

        /// <summary>
        /// Wraps the given string in an ANSI SGR escape sequence when colors are
        /// enabled, otherwise returns it unchanged. <paramref name="code"/> is the
        /// SGR parameter list, e.g. <c>"1;31"</c> for bold red.
        /// </summary>
        public static string ansi(string s, string code) => color_ok ?
                                                                $"\u001b[{code}m{s}\u001b[0m" :
                                                                s;

        /// <summary>
        /// Renders a multi-line annotated diagnostic block to stdout: a colored,
        /// timestamped header followed by an indented body framed with box-drawing
        /// characters. Used for problem reports that need to point at locations
        /// inside the protocol description file — bypasses Serilog's single-line
        /// template so the message itself controls layout and color.
        /// </summary>
        /// <param name="level">"WRN", "ERR", or "INF" — drives the header color.</param>
        /// <param name="title">Headline that follows the level tag (rendered bold).</param>
        /// <param name="body">Body lines (indent + frame are added here).</param>
        public static void report(string level, string title, params string[] body)
        {
            var ts         = DateTime.Now.ToString("HH:mm:ss");
            var levelColor = level == "ERR" ? "1;31" : level == "WRN" ? "1;33" : "1;36";
            Console.Out.WriteLine($"{ansi($"[{ts} {level}]", levelColor)} {ansi(title, "1")}");
            Console.Out.WriteLine($"  {ansi("┌─", "2")}");
            foreach( var line in body )
                Console.Out.WriteLine($"  {ansi("│", "2")} {line}");
            Console.Out.WriteLine($"  {ansi("└─", "2")}");
        }

        /// <summary>
        /// Gets the path to the application properties file (AdHocAgent.toml).
        /// If the file doesn't exist, it extracts a template from embedded resources.
        /// </summary>
        public static string app_props_file
        {
            get
            {
                var file = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "AdHocAgent.toml");

                if( File.Exists(file) )
                {
                    LOG.Information("Using the AdHocAgent Configuration File {file}", file);
                    return file;
                }

                var toml = File.OpenWrite(file);
                Assembly.GetExecutingAssembly().GetManifestResourceStream("AdHocAgent.Templates.AdHocAgent.toml")!.CopyToAsync(toml);
                LOG.Warning("The application configuration file {file} has been extracted from the template. Please note that its content may be outdated.", file);
                toml.Flush();
                toml.Close();
                return file;
            }
        }

        /// <summary>
        /// Application properties loaded from the TOML configuration file.
        /// Uses Tommy library to parse TOML: https://www.ryansouthgate.com/2016/03/23/iconfiguration-in-netcore/
        /// </summary>
        public static TomlTable app_props = TOML.Parse(File.OpenText(app_props_file)); //load application 'toml file'   https://www.ryansouthgate.com/2016/03/23/iconfiguration-in-netcore/

        /// <summary>
        /// Flag indicating if the agent is in testing mode (triggered by '!' suffix in path argument).
        /// </summary>
        public static bool is_testing;

        public static bool is_diagramming;

        // `Server.Result.result` is a FileEntry.List: the deserializer asks for the list and for each entry
        // through these two producers, and every entry streams itself straight to disk as it arrives. There is
        // no archive to hand back, so `Result` itself needs no subclass any more.
        static AdHocAgent()
        {
            _Allocator.DEFAULT.new_AdHocProtocol_FileEntry_List = src => Files.ONE;
            _Allocator.DEFAULT.new_AdHocProtocol_FileEntry      = src => Files.ONE.entry;
        }

        /**
         first - required full path to the description_file.cs
                 or
                 file.proto file
                 or
                 folder.proto directory to translate into AdHoc format

                if description_file.cs has references to other files, next arguments should be paths to:
                the .csproj project files, that conains references information, and/or paths to referensed files

         last - optional: full path to the folder where generated code will be temporary deployed
                if not provided, current (working) directory path will be used

           |                                    state                               |             action               |
           |--------------------------|---------------------------------------------|----------------------------------|
           | provided_path.IsReadOnly | Exists( destination_dir_path/project_name ) |                                  |
           |--------------------------|---------------------------------------------|----------------------------------|
           |                          |             YES                             |           re-deploy              |
           |           YES            |---------------------------------------------|----------------------------------|
           |                          |             NO                              |    query uploaded task result    |
           |--------------------------|---------------------------------------------|----------------------------------|
           |                                 all others cases                       |    upload new task to the server |
        */
        public static async Task Main(string[] paths)
        {
            Console.OutputEncoding = Encoding.UTF8; // !!!!!!!!!!!!!!!!!!!! damn, every .NET console application must start with that !!!!!!!!!!!

            if( paths.Length == 0 )
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(" AdHocAgent Utility - Your friendly command-line assistant for project workflows.");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("===============================================================================================");
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Commands:");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  UUID   ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("         Saves the provided personal, volatile UUID to the AdHocAgent.toml configuration file.");
                Console.ResetColor();
                Console.WriteLine();


                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(" File - based Tasks:");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("  The first argument is the path to the task file. The file extension determines the action:");
                Console.ForegroundColor = ConsoleColor.White;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t.cs   ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("- Uploads the protocol description file to the server to generate source code.");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t.cs?  ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("- Displays information about the protocol description file in the viewer.");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t.md   ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("          - Repeats the deployment process using instructions from the .md file.");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t.proto");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("         - Converts Protocol Buffers file(s) to AdHoc protocol description format.");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("\n  Additional Arguments:");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\t- Remaining arguments can be paths to source (.cs) and project (.csproj) files.");
                Console.WriteLine("\t- If the last argument is a folder, it's used as the output directory.");
                Console.WriteLine("\t- For `.proto` files, the second argument can be a directory of imported `.proto` files.");
                Console.WriteLine("\t- Open source mode: after the file arguments, the URL of the `adhoc` folder of your public GitHub repository");
                Console.WriteLine("\t  (at least 500 stars, the folder holds a copy of every uploaded file) followed by the project's topical tags:");
                Console.WriteLine("\t  AdHocAgent MyProtocol.cs https://github.com/owner/repo/tree/main/path/to/adhoc networking telemetry");
                Console.WriteLine();


                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Configuration:");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("  This utility requires the following files in its directory:\n");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  AdHocAgent.toml ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(" - Contains server URL and paths to local resources (e.g., IDE).");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\t\t     If not found, a template will be generated for you to fill out.");


                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  Deployment_instructions.md");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(" - Required for code generation. Contains deployment instructions.");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\t\t\t\t   If not found, a template will be generated for you to customize.");
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("A template for your protocol description file can be found at: ");
                Console.ForegroundColor = ConsoleColor.Green;
                var path = Path.Join(Directory.GetCurrentDirectory(), "MyProtocolDescription.cs");
                Console.WriteLine(path);

                Console.ResetColor();

                await using var file = File.Create(path);
                await Assembly.GetExecutingAssembly().GetManifestResourceStream("AdHocAgent.Templates.ProtocolDescription.cs")!.CopyToAsync(file);
                Console.ReadLine();
                return;
            }

            if( !paths[0].Contains('\\') && !paths[0].Contains('/') ) //UUID
            {
                updatePersonalVolatileUUID(paths[0]);
                LOG.Information("Volatile personal UUID updated successfully!");
                return;
            }

            if( paths[0].EndsWith(".json") || paths[0].EndsWith(".yaml") ) // Converts OpenAPI json/yaml file into AdHoc protocol description format.
            {
                provided_path = paths[0];
                await OpenApi_To_AdHoc_Converter.convert(paths[0], paths.Length == 1 ?
                                                                       paths[0][..^4] + "cs" : //If the second argument is skipped, the AdHocAgent utility will output the `.cs` file next to the provided OpenAPI file.
                                                                       paths[1]);
                return;
            }


            if( paths[0].EndsWith(".md") ) //  repeat only the deployment process according to instructions in the .md file, already received source files in the `working directory`
            {
                provided_path = paths[0];
                Deployment.redeploy(provided_path = paths[0]);
                return;
            }

            if( paths[0].EndsWith(".cs?") ) // run provided protocol description file viewer
            {
                is_diagramming = true;
                provided_path  = paths[0][..^1]; //cut '?'
                set_provided_paths(paths[1..]);
                await ToObserver.Start();
                return;
            }

            if( !app_props.HasKey("PersonalVolatileUUID") )
                exit($"Cannot find your personal volatile UUID in the {app_props_file} file. Please request and apply one according to the instructions in the manual https://github.com/AdHoc-Protocol/AdHoc-protocol.");


            is_testing = paths[0].EndsWith("!");

            provided_path = is_testing ?
                                paths[0][..^1] :
                                paths[0];

#region .cs files - protocol description file processing
            if( provided_path.EndsWith(".cs") )
            {
                set_provided_paths(paths[1..]);
                var project = ProjectImpl.init();

                // Debug dump: emit the resolved FSM so we can eyeball branch/pack assignment
                // and confirm the Dashboard writer. Controlled by env var so it stays cheap.
                if( Environment.GetEnvironmentVariable("ADHOC_DUMP_BRANCHES") == "1" )
                    DumpBranchesDiagnostic(project);

                // Dev switch: build and validate the description locally and stop — nothing is uploaded.
                if( Environment.GetEnvironmentVariable("ADHOC_PARSE_ONLY") == "1" ) return;

                // Open source mode: the description is valid, now prove the published copy before anything is uploaded.
                if( github != null ) await OpenSource.verify();

                await ToServer.Start(project);
                return;
            }
#endregion
#region .proto files processing
            await ProtoImpl.Process(paths);
#endregion
        }

        /// <summary>
        /// Diagnostic: write the resolved FSM — every connection → actor → state → branch — to a file
        /// alongside the protocol, so we can eyeball exactly which packs each branch collected.
        /// </summary>
        static void DumpBranchesDiagnostic(ProjectImpl root)
        {
            var sb = new StringBuilder();
            var seen = new HashSet<object>();
            void Walk(ProjectImpl prj)
            {
                if( !seen.Add(prj) ) return;
                foreach( var imp in prj.symbol!.Interfaces )
                    if( ProjectImpl.entities.TryGetValue(imp, out var e) && e is ProjectImpl sub )
                        Walk(sub);

                sb.AppendLine($"# PROJECT {prj.symbol}");
                foreach( var conn in prj.connections )
                {
                    sb.AppendLine($"  CONNECTION {conn._name}  L={conn.hostL?._name ?? "?"}  R={conn.hostR?._name ?? "?"}");
                    foreach( var actor in conn.actors )
                    {
                        sb.AppendLine($"    ACTOR {actor._name}  MaxActiveInstances={actor._MaxActiveInstances}");
                        foreach( var st in actor.states )
                        {
                            sb.AppendLine($"      STATE {st._name}  uid={st.uid}");
                            foreach( var br in st.branchesL.Concat(st.branchesR) )
                            {
                                var authority = br.IsMasterAuthority ? "Master" : "Follower";
                                var kind      = br.IsTransitional ? "Transitional" : "Non-transitional";
                                var target    = br.goto_state?._name ?? "<same>";
                                sb.AppendLine($"        BRANCH side={br.Side}  {authority}  {kind}  goto={target}  packs[{br.packs.Count}]:");
                                foreach( var p in br.packs.OrderBy(p => p.full_path) )
                                    sb.AppendLine($"          - {p.full_path}  id={p._id}");
                            }
                        }
                    }
                }
            }

            Walk(root);

            // Pack distribution per connection. `transmit` + `related` is what the RECEIVING side parses;
            // `to_packs` what only the sender materializes; `from_packs` the subset of `related` the sender does
            // not serialize. So: peer parses = transmit ∪ related, sender encodes = transmit ∪ (related − from) ∪ to.
            sb.AppendLine("# DISTRIBUTION");
            foreach( var conn in root.connections )
            {
                string list(IEnumerable<ProjectImpl.HostImpl.PackImpl> src) => !src.Any() ?
                                                                                   "-" :
                                                                                   string.Join(", ", src.Select(p => p.full_path).OrderBy(s => s));

                sb.AppendLine($"  CONNECTION {conn._name}  L={conn.hostL?._name ?? "?"}  R={conn.hostR?._name ?? "?"}");
                sb.AppendLine($"    hostL_to_packs   (L serializes)   : {list(conn.toL)}");
                sb.AppendLine($"    hostL_from_packs (R deserializes) : {list(conn.fromL)}");
                sb.AppendLine($"    hostR_to_packs   (R serializes)   : {list(conn.toR)}");
                sb.AppendLine($"    hostR_from_packs (L deserializes) : {list(conn.fromR)}");
                foreach( var actor in conn.actors )
                {
                    sb.AppendLine($"    ACTOR {actor._name}");
                    sb.AppendLine($"      hostL_transmit : {list(actor.hostL_directly_transmitting_packs)}");
                    sb.AppendLine($"      hostL_related  : {list(actor.hostL_indirectly_transmitting_packs)}");
                    sb.AppendLine($"      hostR_transmit : {list(actor.hostR_directly_transmitting_packs)}");
                    sb.AppendLine($"      hostR_related  : {list(actor.hostR_indirectly_transmitting_packs)}");
                }
            }

            // Headers: each header pack, its constants and the packs that carry it as an artificial _headerN field.
            sb.AppendLine("# HEADERS");
            foreach( var header in root.header_packs )
            {
                sb.AppendLine($"  HEADER {header._name}  id={header._id}  idx={header.idx}  referred={header._referred}  nested_max={header._nested_max}  included={header.included}  fields[{header.fields.Count}]  constants[{header._constants_.Count}]:");
                sb.AppendLine($"    parent={header._parent}  uid={header.uid}  in_host={header.in_host?._name ?? "-"}  by_source={header.parent_by_source_code?.GetType().Name ?? "-"}");
                foreach( var f in header.fields )
                    sb.AppendLine($"    field[{f.idx}] {f._name}  exT={f._exT}  inT={f._inT}  is_Header={f.is_Header}  in root.fields={root.fields.Contains(f)}");
                foreach( var c in header._constants_ )
                    sb.AppendLine($"    const[{c.idx}] {c._name} = {c._value_int}  in root.constant_fields={root.constant_fields.Contains(c)}");
                foreach( var target in root.all_packs.Where(pk => pk.fields.Any(f => f._name.StartsWith("_header") && f.get_exT_pack == header)).OrderBy(pk => pk.full_path) )
                {
                    var hf = target.fields.First(f => f._name.StartsWith("_header") && f.get_exT_pack == header);
                    sb.AppendLine($"    -> {target.full_path}  id={target._id}  {hf._name}[{hf.idx}] exT={hf._exT} (header idx {header.idx})");
                }
            }

            // Resumable service packs: the trio, its ids, and which host of which connection sends each.
            sb.AppendLine("# RESUMABLE SERVICE");
            foreach( var pack in root.resumable_service )
            {
                sb.AppendLine($"  PACK {pack._name}  id={pack._id}  idx={pack.idx}  uid={pack.uid}  parent={pack._parent}  included={pack.included}  in root.packs={root.packs.Contains(pack)}  fields: {string.Join(", ", pack.fields.Select(f => $"{f._name}[{f.idx}] exT={f._exT} inT={f._inT} in root.fields={root.fields.Contains(f)}"))}");
                foreach( var conn in root.connections )
                    foreach( var actor in conn.actors )
                    {
                        if( actor.hostL_directly_transmitting_packs.Contains(pack) ) sb.AppendLine($"    sent by {conn.hostL?._name} on {conn._name}  (actor {actor._name})");
                        if( actor.hostR_directly_transmitting_packs.Contains(pack) ) sb.AppendLine($"    sent by {conn.hostR?._name} on {conn._name}  (actor {actor._name})");
                    }
            }

            // Per-host language configuration: the host default and every per-pack override (impl bits low, hash/equals bits high).
            sb.AppendLine("# HOST IMPL");
            foreach( var host in root.hosts )
            {
                sb.AppendLine($"  HOST {host._name}  langs={host._langs}  default=0x{host._default_impl_hash_equal:X8}");
                foreach( var (sym, cfg) in host.pack_impl.OrderBy(kv => kv.Key.ToString()) )
                    sb.AppendLine($"    0x{cfg:X8}  {sym}");
            }

            var outPath = Path.Combine(Path.GetDirectoryName(provided_path)!, "branches.dump.txt");
            File.WriteAllText(outPath, sb.ToString());
            LOG.Information("Branch dump written to {path}", outPath);
        }

        static void set_provided_paths(string[] paths)
        {
            paths = OpenSource.take(paths); // the GitHub URL and the tags after it are not paths
            if( paths.Length == 0 ) return;

            var collect = new HashSet<string>();

            foreach( var path in paths )
                if( File.Exists(path) )
                {
                     if( path.EndsWith(".csproj") )
                    {
                        var csproj = path;

                        var dir = Path.GetDirectoryName(csproj)!;

                        foreach( var xml_path in XElement.Load(csproj)
                                                         .Descendants()
                                                         .Where(n => n.Name.ToString().Equals("Compile"))
                                                         .Select(n => Path.GetFullPath(n.Attribute("Include")!.Value, dir)) )
                            collect.Add(xml_path);
                    }
                    else if( path.EndsWith(".cs") ) collect.Add(path);
                }
                else if( !Directory.Exists(destination_dir_path = path) )
                    Directory.CreateDirectory(destination_dir_path);

            collect.Remove(provided_path);

            provided_paths = collect.ToArray();
        }


        //folder for downloading files before their processing and deployment ( working(current) directory by default)
        public static string destination_dir_path = Directory.GetCurrentDirectory();

        public static string RawFilesDirPath => Path.Combine(destination_dir_path, Path.GetFileName(provided_path)[..^3]);


        public static int exit(string banner, int code = 1)
        {
            if( 0 < banner.Length )
                if( code == 0 )
                    LOG.Information(banner);
                else
                    LOG.Error(banner);

            Console.Out.WriteLine(ansi("Press ENTER to exit", "2"));
            try { Console.In.ReadLine(); }
            catch( IOException ) { }

            Environment.Exit(code);
            return code;
        }

        public static string   provided_path; //can be AdHoc ptotocol description or Protocol buffers convertor input
        public static string[] provided_paths = [];
        public static string   task => HashFor(Path.GetDirectoryName(provided_path)!).ToString("X") + "_" + Path.GetFileName(provided_path);

        /// <summary>Open source mode: the URL of the project's `adhoc` folder on GitHub, as given on the command line. Null when the URL is not given.</summary>
        public static string? github;

        /// <summary>Open source mode: the project's topical tags from the command line, joined by a single space. Null when the URL is not given.</summary>
        public static string? tags;

#region Open source mode
        /// <summary>
        /// Code generation is free. A project that wants to be listed in the catalog of projects using the AdHoc protocol
        /// meets the conditions below: a public GitHub repository with at least <see cref="MIN_STARS"/> stars, a published copy of
        /// the description and the project's tags.
        /// The mode is switched on by the URL of the repository's <see cref="FOLDER"/>
        /// folder placed after the file arguments and followed by the project's topical tags:
        /// <code>AdHocAgent MyProtocol.cs [more .cs / .csproj / output folder] https://github.com/owner/repo/tree/main/path/to/adhoc tag1 tag2</code>
        /// The folder must hold a copy of every protocol description file that is uploaded, under the same file name.
        /// Before the upload the Agent checks the stars, the folder and the copies; any mismatch refuses the whole task.
        /// The Server repeats the check on its side.
        /// </summary>
        public static class OpenSource{
            public const int    MIN_STARS = 500;
            public const string FOLDER    = "adhoc";

            static readonly Regex url_head = new(@"^(?:https?://)?(?:www\.)?github\.com/", RegexOptions.IgnoreCase);
            static readonly Regex tag_form = new(@"^[A-Za-z][A-Za-z0-9_\-]*$");

            public static bool is_url(string arg) => url_head.IsMatch(arg);

            /// <summary>
            /// Splits the arguments at the GitHub URL: what precedes it is returned for the path processing, the URL lands in
            /// <see cref="github"/>, what follows it in <see cref="tags"/>. Without a URL the arguments are returned untouched.
            /// </summary>
            public static string[] take(string[] args)
            {
                var at = Array.FindIndex(args, is_url);
                if( at < 0 ) return args;

                var location = parse(args[at]); // refuses a malformed URL before anything else happens
                github = args[at];

                var list = args[(at + 1)..];
                if( list.Length == 0 ) exit($"Open source mode: list the project's topical tags after {github} - English words separated by spaces.");

                foreach( var tag in list )
                    if( !tag_form.IsMatch(tag) )
                        exit($"Open source mode: the tag `{tag}` is not an English word. A tag is letters, digits, `-` and `_`, and starts with a letter.");

                tags = string.Join(' ', list);
                LOG.Information("Open source mode: folder `{folder}` of {owner}/{repo}, tags: {tags}", location.path.Length == 0 ? FOLDER : location.path, location.owner, location.repo, tags);
                return args[..at];
            }

            /// <summary>Where the URL points. <c>branch</c> is null when the URL names no branch; <c>path</c> is the folder path inside the repository, empty for the root.</summary>
            public record Location(string owner, string repo, string? branch, string path);

            public static Location parse(string url)
            {
                const string form = "https://github.com/owner/repo/tree/branch/path/to/" + FOLDER;

                var seg = url_head.Replace(url, "").Split('/', StringSplitOptions.RemoveEmptyEntries);
                if( seg.Length < 2 ) exit($"Open source mode: `{url}` names no repository. Expected {form}");

                var owner = seg[0];
                var repo  = seg[1].EndsWith(".git") ? seg[1][..^4] : seg[1];

                if( seg.Length == 2 ) // the repository itself is the folder
                {
                    if( repo != FOLDER ) exit($"Open source mode: `{url}` must point to a folder named `{FOLDER}`. Expected {form}");
                    return new Location(owner, repo, null, "");
                }

                if( seg[2] != "tree" || seg.Length < 5 ) exit($"Open source mode: `{url}` is not a folder URL. Expected {form}");
                if( seg[^1] != FOLDER ) exit($"Open source mode: the folder `{seg[^1]}` in `{url}` must be named `{FOLDER}`. Expected {form}");

                return new Location(owner, repo, seg[3], string.Join('/', seg[4..]));
            }

            /// <summary>
            /// Checks the repository and the folder against the files about to be uploaded (<see cref="provided_path"/> and
            /// <see cref="provided_paths"/>) and exits on the first mismatch. Line endings and a UTF-8 BOM do not count as a
            /// difference: git rewrites both on checkout. <c>GITHUB_TOKEN</c> in the environment lifts the anonymous
            /// rate limit of the GitHub API.
            /// </summary>
            public static async Task verify()
            {
                var location = parse(github!);
                var files    = new[] { provided_path }.Concat(provided_paths).ToArray();

                var duplicate = files.Select(Path.GetFileName).GroupBy(name => name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => 1 < g.Count());
                if( duplicate != null ) exit($"Open source mode: several uploaded files are named `{duplicate.Key}`, but one folder on GitHub holds one file of a name.");

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("AdHocAgent");
                http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
                var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
                if( !string.IsNullOrEmpty(token) ) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var api = $"https://api.github.com/repos/{Uri.EscapeDataString(location.owner)}/{Uri.EscapeDataString(location.repo)}";

                using var repo = JsonDocument.Parse(await get(http, api, $"the repository {location.owner}/{location.repo}"));

                if( repo.RootElement.TryGetProperty("private", out var isPrivate) && isPrivate.GetBoolean() )
                    exit($"Open source mode: the repository {location.owner}/{location.repo} is private. Open source mode needs a public repository.");

                var stars = repo.RootElement.GetProperty("stargazers_count").GetInt32();
                if( stars < MIN_STARS )
                    exit($"Open source mode: the repository {location.owner}/{location.repo} has {stars} stars. Open source mode needs at least {MIN_STARS}, the project does not qualify.");

                var branch = location.branch ?? repo.RootElement.GetProperty("default_branch").GetString()!;
                var folder = $"{location.owner}/{location.repo}/tree/{branch}/{location.path}".TrimEnd('/');

                var listing_url = location.path.Length == 0 ?
                                      $"{api}/contents?ref={Uri.EscapeDataString(branch)}" :
                                      $"{api}/contents/{string.Join('/', location.path.Split('/').Select(Uri.EscapeDataString))}?ref={Uri.EscapeDataString(branch)}";

                using var listing = JsonDocument.Parse(await get(http, listing_url, $"the folder {folder}"));
                if( listing.RootElement.ValueKind != JsonValueKind.Array ) exit($"Open source mode: {folder} is a file, not a folder.");

                var remote = new Dictionary<string, string>(StringComparer.Ordinal); // file name -> raw download URL
                foreach( var entry in listing.RootElement.EnumerateArray() )
                    if( entry.GetProperty("type").GetString() == "file" )
                        remote[entry.GetProperty("name").GetString()!] = entry.GetProperty("download_url").GetString()!;

                foreach( var file in files )
                {
                    var name = Path.GetFileName(file);
                    if( !remote.TryGetValue(name, out var download) ) exit($"Open source mode: the folder {folder} holds no copy of `{name}`. Publish every uploaded file there.");

                    var copy = normalize(await http.GetByteArrayAsync(download));
                    var mine = normalize(await File.ReadAllBytesAsync(file));
                    if( !copy.AsSpan().SequenceEqual(mine) ) exit($"Open source mode: the copy of `{name}` in {folder} differs from {file}. Publish the file as it is uploaded.");
                }

                LOG.Information("Open source mode: {owner}/{repo} has {stars} stars and {folder} holds a copy of every uploaded file.", location.owner, location.repo, stars, folder);
            }

            static async Task<string> get(HttpClient http, string url, string what)
            {
                HttpResponseMessage response;
                try { response = await http.GetAsync(url); }
                catch( Exception e )
                {
                    exit($"Open source mode: cannot reach GitHub to check {what}: {e.Message}");
                    throw;
                }

                using( response )
                {
                    var body = await response.Content.ReadAsStringAsync();
                    if( response.IsSuccessStatusCode ) return body;

                    exit(response.StatusCode switch
                         {
                             HttpStatusCode.NotFound => $"Open source mode: {what} is not found on GitHub, or the repository is private.",
                             HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests => $"Open source mode: the GitHub API refused the request for {what} ({(int)response.StatusCode}). The anonymous rate limit is 60 requests an hour; set GITHUB_TOKEN to lift it.",
                             _ => $"Open source mode: GitHub answered {(int)response.StatusCode} for {what}: {body}"
                         });
                    throw new InvalidOperationException();
                }
            }

            /// <summary>Strips a UTF-8 BOM and turns CRLF and CR line endings into LF, so a git checkout on any platform compares equal to the original.</summary>
            static byte[] normalize(byte[] bytes)
            {
                var from = 3 <= bytes.Length && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
                var dst  = new byte[bytes.Length - from];
                var len  = 0;
                for( var i = from; i < bytes.Length; i++ )
                {
                    var b = bytes[i];
                    if( b == '\r' )
                    {
                        if( i + 1 < bytes.Length && bytes[i + 1] == '\n' ) continue; // CRLF: the LF that follows is kept
                        b = (byte)'\n';                                             // lone CR
                    }

                    dst[len++] = b;
                }

                return len == dst.Length ? dst : dst[..len];
            }
        }
#endregion

        static ulong HashFor(string str)
        {
            var ret = 3074457345618258791ul;
            foreach( var conn in str )
            {
                ret += conn;
                ret *= 3074457345618258799ul;
            }

            return ret;
        }


        public class Deployment{
            static string deployment_instructions_txt;
            static string raw_files_dir_path;

            // A shared StringBuilder to reduce memory allocations during string construction.
            static StringBuilder sb = new();

            /// Represents a single file or directory in the deployment source tree.
            /// It holds information about its path, display properties, and deployment rules.
            /// </summary>
            class LineInfo{
                /// <summary>
                /// Initializes a new LineInfo object and adds it to the global dictionary.
                /// </summary>
                /// <param name="path">The full file system path to the file or directory.</param>
                /// <param name="icon">The icon to use when rendering this line in Markdown.</param>
                /// <param name="indent">The indentation string for Markdown rendering.</param>
                /// <param name="key">The relative path used as a unique key in the dictionary.</param>
                public LineInfo(string path, string icon, string indent, string key)
                {
                    this.path   = path;
                    this.icon   = icon;
                    this.indent = indent;
                    // Markdown carried along with the generated code (licenses, notes) is not source the user
                    // routes or edits: it rides to the destination of the folder it arrived in, byte for byte.
                    // 'asis' keeps it out of the merge, `save == false` keeps it out of the instructions list, and
                    // the post-processing steps below skip it, so a `.*` formatter never rewrites it.
                    md = key.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
                    // 'asis' files are treated as binary/library files. They are copied directly without merging.
                    asis = md || key.Contains("/lib/") || key.Contains(@"\lib\") || key.Contains(@"\__\") || key.Contains(@"/__/");
                    save = !asis && !key.Contains(@"\gen\") && !key.Contains(@"/gen/") && !key.EndsWith(@"\gen") && !key.EndsWith(@"/gen") && !key.EndsWith(@"\lib") && !key.EndsWith(@"/lib");
                    receiver_files_lines.Add(key, this);
                }

                // --- Deployment Properties ---
                public bool                                       skipped; // True if this file/folder should be ignored during deployment.
                public (Regex Selector, string[] Destinations)[]? targets; // Deployment rules: a selector regex and destination paths.

                // --- Reporting Properties ---
                public List<string>? report; // A list of messages detailing the outcome of deployment for this item.

                public void add_report(string report) => (this.report ??= new List<string>(2)).Add(report);


                // --- Display Properties ---
                public string indent;
                public bool   asis; // If true, file is copied verbatim without smart merge.
                public bool   md;   // If true, a markdown file: copied verbatim, never listed, never post-processed.
                public bool   save; // save this line in the instructions file.
                public string path;
                public string icon;
                public string customization = ""; // User-added text from the .md file (e.g., "✅ copy full tree"), preserved on regeneration.

                /// <summary>
                /// Appends a Markdown-formatted line representing this file/folder to the shared StringBuilder.
                /// Used for generating or updating the deployment instructions file.
                /// </summary>
                public StringBuilder append_md_line()
                {
                    sb.Append(indent)
                      .Append("- ")
                      .Append(icon)
                      .Append('[')
                      .Append(Path.GetFileName(path))
                      .Append("](");

                    // Handle paths with spaces by wrapping them in angle brackets <...>, as per Markdown spec.
                    if( path.Contains(' ') )
                    {
                        sb.Append('<');
                        // Normalize Windows drive letter paths for better cross-platform link compatibility.
                        if( path[1] == ':' ) sb.Append('/');
                        sb.Append(path).Append(">) ");
                    }
                    else
                    {
                        if( path[1] == ':' ) sb.Append('/');
                        sb.Append(path);
                        sb.Append(") ");
                    }

                    sb.Replace('\\', '/'); // Always use forward slashes in Markdown links.
                    return sb;
                }

                /// <summary>
                /// Appends a line to the console report summarizing the deployment result for this file.
                /// </summary>
                public void append_report_line()
                {
                    sb.Append(indent)
                      .Append(icon)
                      .Append(Path.GetFileName(path));

                    if( File.Exists(path) )
                        if( skipped || report == null )
                            sb.Append(" ⛔ "); // Skipped or no action taken.
                        else
                        {
                            // Append multi-line reports with proper indentation for readability.
                            sb.Append(' ');
                            var chars = sb.Length;
                            sb.Append(report[0]);
                            for( var r = 1; r < report.Count; r++ )
                            {
                                sb.Append('\n');
                                for( var i = 0; i < chars; i++ ) sb.Append(' ');
                                sb.Append(report[r]);
                            }
                        }

                    sb.Replace('\\', '/').Append('\n');
                }
            }

            /// <summary>
            /// A dictionary mapping relative file paths (e.g., "InCS/Agent/MyFile.cs") to their LineInfo objects.
            /// This is the central data structure holding the state of all source files to be deployed.
            /// </summary>
            static Dictionary<string, LineInfo> receiver_files_lines = new();

            /// <summary>
            /// Scans the source directory (`raw_files_dir_path`) recursively and populates the `receiver_files_lines` dictionary.
            /// </summary>
            /// <returns>The length of the root path, used for creating relative keys for the dictionary.</returns>
            public static int build_receiver_files_lines()
            {
                // Normalized FIRST: every path below is derived from this one, while `Path.GetDirectoryName` — used
                // to walk a file back to its parent folder — always answers with `\`. A root that came in with `/`
                // (a path typed or pasted on the command line) would then never compare equal to a parent, the
                // ancestor stack would stay empty, and every rule inherited from a folder would deploy nothing.
                raw_files_dir_path = Path.GetFullPath(raw_files_dir_path);

                var root_path_len = raw_files_dir_path.Length + (raw_files_dir_path.EndsWith('/') || raw_files_dir_path.EndsWith('\\') ?
                                                                     0 :
                                                                     1);

                // Helper to create a LineInfo object for a given path.
                void add(string icon, string path, int level)
                {
                    sb.Clear();
                    for( var i = 0; i < level; i++ ) sb.Append("  "); // Create indentation string.
                    // The key is the path relative to the root, with normalized forward slashes.
                    new LineInfo(path, icon, sb.ToString(), path[root_path_len..].Replace('\\', '/'));
                }

                // Recursive function to scan directories.
                void scan(string dir, int level)
                {
                    // The initial call with level -1 skips adding the root directory itself to the visual tree.
                    if( -1 < level ) add("📁", dir, level);

                    level++;
                    foreach( var dir_ in Directory.GetDirectories(dir) ) scan(dir_, level);
                    foreach( var file in Directory.GetFiles(dir) )
                        add(Path.GetExtension(file) switch
                            {
                                ".cs"    => "＃",
                                ".cpp"   => "🧩",
                                ".h"     => "🧾",
                                ".java"  => "☕",
                                ".ts"    => "🌀",
                                ".js"    => "📜",
                                ".html"  => "🌐",
                                ".css"   => "🎨",
                                ".go"    => "🐹",
                                ".rs"    => "⚙️",
                                ".kt"    => "🟪",
                                ".swift" => "🐦",
                                ".json"  => "{}",
                                _        => "📄"
                            }, file, level);
                }

                scan(raw_files_dir_path, -1);
                return root_path_len;
            }

            /// <summary>
            /// Redeploys source files based on an existing deployment instructions file.
            /// This is typically called after the initial `deploy` has been configured by the user.
            /// </summary>
            /// <param name="deployment_instructions_file">Path to the deployment instructions .md file.</param>
            public static void redeploy(string deployment_instructions_file)
            {
                // Infer the source directory path by removing the ".md" extension from the instructions file name.
                raw_files_dir_path = deployment_instructions_file[..^".md".Length];

                // Search for the source directory first next to the .md file, then in the current working directory.
                if( !Directory.Exists(raw_files_dir_path) && !Directory.Exists(raw_files_dir_path = Path.Join(Directory.GetCurrentDirectory(), Path.GetFileName(raw_files_dir_path))) )
                    exit($"Cannot find source folder {Path.GetFileName(raw_files_dir_path)} at {Path.GetDirectoryName(deployment_instructions_file)} and at working directory {Directory.GetCurrentDirectory()} redeploy process canceled");

                read_renumbered();
                process(deployment_instructions_file);
            }

            /// <summary>
            /// A shared UTF8Encoding instance that does not emit a Byte Order Mark (BOM).
            /// This is crucial for compatibility with many tools and systems (e.g., clang-format, Java compilers, shell scripts)
            /// that do not correctly handle a BOM.
            /// </summary>
            public static readonly UTF8Encoding UTF8_NO_BOM = new(false);

            /// <summary>
            /// Performs the initial deployment. It locates or generates a default deployment instructions file
            /// and then processes it to deploy files from the raw source directory.
            /// </summary>
            /// <param name="raw_files_dir_path">Path to the directory containing the received source files.</param>
            /// <summary>The file, in the generated files' folder, that lists the region-key prefixes of the renumbered hosts: `old new` a line.</summary>
            public const string RENUMBERED = ".renumbered";

            static (string old, string now)[] renumbered_prefixes = [];

            static void read_renumbered()
            {
                var file = Path.Join(raw_files_dir_path, RENUMBERED);
                renumbered_prefixes = File.Exists(file) ?
                                          File.ReadAllLines(file).Select(l => l.Split(' ')).Where(p => p.Length == 2).Select(p => (p[0], p[1])).ToArray() :
                                          [];
            }

            public static void deploy(string raw_files_dir_path)
            {
                Deployment.raw_files_dir_path = raw_files_dir_path;
                read_renumbered();
                var deployment_instructions_file_name = Path.GetFileName(raw_files_dir_path) + ".md";

                // --- Search for the deployment instructions file in priority order ---
                // 1. Next to the raw files directory itself.
                var deployment_instructions_file_path = Path.Join(Path.GetDirectoryName(raw_files_dir_path)!, deployment_instructions_file_name);
                if( File.Exists(deployment_instructions_file_path) ) goto deploy;

                // 2. In the current working directory (where the tool was executed from).
                deployment_instructions_file_path = Path.Join(Directory.GetCurrentDirectory(), deployment_instructions_file_name);
                if( File.Exists(deployment_instructions_file_path) ) goto deploy;

                // --- If not found, generate a default deployment instructions file ---
                deployment_instructions_file_path = Path.Join(Path.GetDirectoryName(raw_files_dir_path)!, deployment_instructions_file_name);

                var deployment_instructions_file = File.OpenWrite(deployment_instructions_file_path);

                var received_name = Path.GetFileName(raw_files_dir_path);
                deployment_instructions_file.Write(UTF8_NO_BOM.GetBytes(DeploymentText.HEADER
                                                                       .Replace("{received}", received_name)
                                                                       .Replace("{md_name}", deployment_instructions_file_name)));


                build_receiver_files_lines();
                sb.Clear();

                foreach( var info in receiver_files_lines
                                     .Where(e => !(e.Value.asis)) //exclude libs and finally generated files
                                     .OrderBy(e => e.Key)
                                     .Select(e => e.Value) )
                    info.append_md_line().Append('\n');

                var tree = sb.ToString();
                deployment_instructions_file.Write(UTF8_NO_BOM.GetBytes(tree));

                deployment_instructions_file.Write(UTF8_NO_BOM.GetBytes(DeploymentText.FOOTER.Replace("{md_path}", deployment_instructions_file_path).Replace("{received}", received_name)));

                deployment_instructions_file.Flush();
                deployment_instructions_file.Close();

                LOG.Warning(@"{deployment_file_name} not found in searched directories:
{raw_files_dir_path}
{protocol_description_dir_path}
Default {deployment_instructions} file generated at {deployment_instructions_file_path}.
Please update the {target_locations} and {copy_instructions} in the file according your projects layout. 
After making the necessary modifications, you can rerun the deployment process by executing the following command in the context of the {working_dir} directory: 
        AdHocAgent {deployment_instructions_file_path}",
                            deployment_instructions_file_name,
                            Path.GetDirectoryName(provided_path),
                            Path.GetDirectoryName(raw_files_dir_path),
                            "`deployment instructions`",
                            deployment_instructions_file_path,
                            "`target locations`",
                            "`copy instructions`",
                            Path.GetDirectoryName(deployment_instructions_file_path),
                            deployment_instructions_file_path
                           );
                return;

                // Label to jump to when the deployment file is found and ready for processing.
                deploy:
                process(deployment_instructions_file_path);
            }

            /// <summary>
            /// The core processing engine that reads the deployment instructions, executes tasks,
            /// merges/copies files, and generates reports and backups.
            /// </summary>
            /// <param name="deployment_instructions_file">The path to the configured .md instructions file.</param>
            static void process(string deployment_instructions_file)
            {
                // STEP 1: Get the ground truth by scanning the current filesystem.
                build_receiver_files_lines();
                deployment_instructions_txt = File.ReadAllText(deployment_instructions_file, UTF8_NO_BOM);
                LOG.Information("Starting deployment of files from \"{src_dir}\", according to instructions in \"{md_file}\"", raw_files_dir_path, deployment_instructions_file);

                // --- Data structures for reconciliation ---
                var obsolete_md_lines = new List<string>();    // Holds full markdown lines from the .md file that no longer correspond to a real file.
                var reconciled_keys   = new HashSet<string>(); // Tracks keys of files that were found in both the .md and the filesystem.
                var Key_path_segment  = new Regex(@"(?<=\/|\\)(CPP|InCS|InGO|InJAVA|InRS|InTS)[\\/]*.*");
                var markdown_regex    = new Regex(@"- (?:📁|＃|🧩|🧾|☕|🌀|📜|🌐|🎨|🐹|⚙️|🟪|🐦|📄|\{\})(?:\s*\[([^\]]*)\]\(([^)]*)\)[^\[\n\r]*)*(?:\s*⛔)?", RegexOptions.Multiline);

                Match? first_match = null;
                Match? last_match  = null;

                // STEP 2: Parse the existing .md file and reconcile with the ground truth.
                foreach( Match match in markdown_regex.Matches(deployment_instructions_txt) )
                {
                    last_match  =   match;
                    first_match ??= match;
                    // the link
                    //      InCS/Agent/lib/collections/BitList.cs
                    //      InJAVA/Server/collections/org/unirail/collections/BitList.java
                    var key = Key_path_segment.Match(match.Groups[2].Captures[0].Value).Groups[0].Value.Replace('\\', '/').Replace(">", "").Trim();

                    if( receiver_files_lines.TryGetValue(key, out var info) )
                    {
                        if( info.save )
                        {
                            // MATCH FOUND: This file exists. Preserve its customization.
                            //- 📁[InCS](/AdHocTMP/AdHocProtocol/InCS) ✅ copy full structure [](/AdHoc/Protocol/Generated/InCS)
                            //           <--------- head ------------> <------------------- customization --------------------->
                            var head = match.Groups[2].Captures[0];
                            info.customization = match.ToString()[(head.Index + head.Length + 1 - match.Index)..];
                            reconciled_keys.Add(key);
                        }
                        else
                            // File exists on disk, but it is a library/binary file ('asis').
                            // We treat the *instruction line* as obsolete so it's removed from the MD file.
                            obsolete_md_lines.Add(match.ToString());
                    }
                    else
                    {
                        // NO MATCH: This instruction is obsolete.
                        obsolete_md_lines.Add(match.ToString());
                    }
                }

                // A file is only "new" if it was supposed to be saved but wasn't in the reconciled list.
                // This correctly ignores files in 'lib' and 'gen' directories.
                var new_files = receiver_files_lines
                                .Where(kvp => kvp.Value.save && !reconciled_keys.Contains(kvp.Key))
                                .Select(kvp => kvp.Key)
                                .ToList();

                // Rules are parsed BEFORE anything is reported, so the report can name the exact path each new
                // file would be written to instead of leaving the user to work it out. Parsing reads
                // `info.customization`, captured in STEP 2 and untouched by any later regeneration, so doing it
                // here rather than after is safe.
                var any              = new Regex(".*");
                var has_some_targets = false;

                foreach( var info in receiver_files_lines.Values )
                {
                    // This regex finds the skip marker and all target definitions `[...](...)` within the customization string.
                    var parser_regex = new Regex(@"(\[([^\]]*)\]\(([^)]*)\))|(\s*⛔)");
                    var targets      = new List<(string Selector, string Destination)>();

                    foreach( Match match in parser_regex.Matches(info.customization) )
                    {
                        if( match.Groups[4].Success ) // Matched the skip marker ⛔
                        {
                            info.skipped = true;
                        }
                        else if( match.Groups[1].Success ) // Matched a target []()
                        {
                            var selector = match.Groups[2].Value;
                            var dest     = match.Groups[3].Value.Trim();
                            // Markdown writes a path that contains spaces as `<...>` (the README shows that form); the
                            // brackets are notation, not part of the path. A plain path with spaces is fine as well.
                            if( 1 < dest.Length && dest[0] == '<' && dest[^1] == '>' ) dest = dest[1..^1].Trim();
                            // `/D:/dir/` is the Markdown spelling of an absolute Windows path. (An empty `[]()` is the
                            // skip marker, handled below; it must not be indexed.)
                            if( 2 < dest.Length && dest[0] == '/' && dest[2] == ':' ) dest = dest[1..].Replace('/', '\\');

                            // To skip a file or folder from deployment, add `⛔` to the line or use an empty target `[]()`.
                            if( selector == "" && dest == "" ) { info.skipped = true; }
                            else { targets.Add((selector, dest)); }
                        }
                    }

                    if( targets.Count > 0 )
                    {
                        has_some_targets = true;
                        info.targets = targets.GroupBy(t => t.Selector)
                                              .Select(group => (
                                                                   group.Key.Equals("") ?
                                                                       any :
                                                                       new Regex(group.Key),
                                                                   group.Select(pair => pair.Destination).ToArray()
                                                               )).ToArray();
                    }
                }

                // NOTE: no bail-out here. An instructions file with no target locations yet is the normal state of
                // a freshly generated one, and a stale file carries none that still match — in both cases the
                // list has to be brought up to date with what actually arrived FIRST, because that regenerated
                // list is the thing the user then writes the targets on. The bail-out lives below the refresh.

                // Both backup paths are decided here, ABOVE the report, because the report promises them by name:
                // "a numbered folder beside the instructions file" is not something anyone should have to resolve
                // in their head. Nothing is created yet — the folder appears only if the deployment goes ahead.
                var md_backup_dir = Path.GetDirectoryName(deployment_instructions_file)!;
                var md_stem       = Path.GetFileNameWithoutExtension(deployment_instructions_file);
                var md_ext        = Path.GetExtension(deployment_instructions_file);

                // `<stem>.<n><ext>` — one past the highest number already sitting beside the file.
                var md_backup_path = Path.Combine(md_backup_dir,
                                                  $"{md_stem}.{Directory.GetFiles(md_backup_dir, $"{md_stem}.*{md_ext}")
                                                                        .Select(f =>
                                                                                {
                                                                                    int.TryParse(Path.GetFileNameWithoutExtension(f)[md_stem.Length..].TrimStart('.'), out var num);
                                                                                    return num;
                                                                                })
                                                                        .DefaultIfEmpty(0)
                                                                        .Max() + 1}{md_ext}");

                // `<received folder name>_<n>` — same numbering, applied to the overwritten-files backup.
                var backup_name = Path.GetFileName(raw_files_dir_path);
                var backupDir = Path.Combine(md_backup_dir,
                                             backup_name + "_" + (Directory.GetDirectories(md_backup_dir, $"{backup_name}_*")
                                                                           .Select(dirPath =>
                                                                                   {
                                                                                       int.TryParse(Path.GetFileName(dirPath)[(backup_name.Length + 1)..], out var number);
                                                                                       return number;
                                                                                   })
                                                                           .DefaultIfEmpty(0)
                                                                           .Max() + 1));

                // WHERE EVERY FILE GOES is decided here, before a single byte is written or reported. The report
                // has to promise exactly what the deployment will do — which files land where, and which
                // pre-existing files are about to be deleted — so one pass builds the plan and everything
                // downstream reads it. No second implementation of the rules to drift out of step.
                var plan      = new List<(LineInfo info, string dst)>();
                var ancestors = new List<LineInfo>();

                foreach( var info in receiver_files_lines.Values )
                {
                    // Maintain the ancestor stack. Pop directories until the current item's parent is on top.
                    while( ancestors.Count > 0 && !Path.GetDirectoryName(info.path)!.Equals(ancestors[^1].path, StringComparison.OrdinalIgnoreCase) )
                        ancestors.RemoveAt(ancestors.Count - 1);

                    // An item is skipped if marked explicitly, OR if it sits inside a skipped folder.
                    var shouldSkip = info.skipped || ancestors.Any(a => a.skipped);

                    if( Directory.Exists(info.path) ) // a directory: remember it for its children
                    {
                        ancestors.Add(info);
                        continue;
                    }

                    if( shouldSkip ) continue;

                    // Rules inherited from a parent folder: the file keeps its path relative to the folder that
                    // carries the rule. This is what deploys a line that has no rule of its own.
                    foreach( var parent_folder in ancestors.Where(i => i.targets is { Length: > 0 }) )
                        foreach( var target_path in parent_folder.targets!.Where(t => t.Selector.IsMatch(info.path)).SelectMany(t => t.Destinations) )
                            plan.Add((info, Path.Combine(target_path, Path.GetRelativePath(parent_folder.path, info.path))));

                    // Rules on the line itself: a destination ending in a separator is a folder to drop the file
                    // into; anything else IS the full destination path.
                    if( info.targets != null )
                        foreach( var target_path in info.targets.SelectMany(t => t.Destinations) )
                            plan.Add((info, target_path.EndsWith('/') || target_path.EndsWith('\\') ?
                                                Path.Combine(target_path, Path.GetFileName(info.path)) :
                                                target_path));
                }

                // Case-insensitive throughout: these are Windows paths.
                var filesToDeploy   = new HashSet<string>(plan.Select(p => p.dst), StringComparer.OrdinalIgnoreCase);
                var destinationDirs = new HashSet<string>(plan.Select(p => Path.GetDirectoryName(p.dst)!), StringComparer.OrdinalIgnoreCase);

                // ORPHANS: files sitting in a directory that receives generated output, which this deployment is
                // not writing. A directory receiving generated output is generator territory — output the
                // protocol no longer produces (renamed entities, dropped packs), and anything else parked among
                // it, does not belong there. They are listed before the decision and removed with it.
                //
                // Distinct(): destination directories nest (`gen` and `gen\__`), so a file is reachable from more
                // than one walk root. Dot-folders (`.git`, `.idea`, `.claude`, `.vs`) are never descended into by
                // `walk`, so tool and VCS state living inside a destination tree is never touched.
                var orphans = destinationDirs.Where(Directory.Exists)
                                             .SelectMany(walk)
                                             .Distinct(StringComparer.OrdinalIgnoreCase)
                                             .Where(existing => !filesToDeploy.Contains(existing))
                                             .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                                             .ToList();

                // STEP 3: ANY change in the received file set is put to the user, in full, before anything is
                // written. Nothing here is a formality: the list being regenerated is where the deployment rules
                // live, a newly listed file can be swept into a real source tree by an inherited folder rule, and
                // an obsolete line takes its rules with it. So: say what arrived, say what vanished, say where
                // each new file would land, say what deploying will do and how to undo it — then let the user
                // choose between deploying now and stopping to edit the rules first.
                var list_changed = obsolete_md_lines.Count > 0 || new_files.Count > 0;

                if( list_changed || 0 < orphans.Count )
                {
                    var md_name = Path.GetFileName(deployment_instructions_file);
                    var W       = Console.Out;

                    // `:l` — plain text, no structured-logging quotes: these are paths a person has to read.
                    if( list_changed )
                        LOG.Warning("The received file set no longer matches the file list in {md_file:l}", md_name);
                    else
                        LOG.Warning("Files that are no part of this deployment are sitting in its destination directories.");

                    W.WriteLine();

                    if( list_changed )
                    {
                        W.WriteLine($"  The list in {md_name} is where your deployment rules live — the target paths and ⛔");
                        W.WriteLine($"  markers written on its lines. It is rebuilt from what actually arrived, so it changes");
                        W.WriteLine($"  when the generator's output changes. Lines that still match a received file keep their");
                        W.WriteLine($"  rules untouched; only what is listed below is affected.");
                        W.WriteLine();
                    }

                    if( 0 < new_files.Count )
                    {
                        W.WriteLine($"  NEW — arrived in the result, not yet listed in {md_name}  ({new_files.Count}):");
                        foreach( var key in new_files.OrderBy(k => k, StringComparer.OrdinalIgnoreCase) )
                        {
                            W.WriteLine($"    + {key}");

                            var info = receiver_files_lines[key];
                            if( Directory.Exists(info.path) ) continue; // folder: its files speak for it

                            // With no targets anywhere the per-file "no rule matches it" would repeat on every
                            // single line; the one message printed after this report says it once instead.
                            if( !has_some_targets ) continue;

                            var dsts = plan.Where(p => ReferenceEquals(p.info, info)).Select(p => p.dst).ToArray();
                            if( dsts.Length == 0 )
                                W.WriteLine($"        no rule matches it — it will NOT be deployed anywhere");
                            else
                                foreach( var dst in dsts )
                                    W.WriteLine($"        an inherited folder rule WILL deploy it to:  {dst}");
                        }

                        W.WriteLine($"      A new line carries no rule of its own, so it is the rule on a parent folder that");
                        W.WriteLine($"      decides. Put ⛔ on the line to keep the file out of the deployment.");
                        W.WriteLine();
                    }

                    if( 0 < obsolete_md_lines.Count )
                    {
                        W.WriteLine($"  REMOVED — listed in {md_name}, no longer produced  ({obsolete_md_lines.Count}):");
                        // The markdown line already opens with its own "- ", so it is printed as it stands.
                        foreach( var line in obsolete_md_lines ) W.WriteLine($"    {line.Trim()}");
                        W.WriteLine($"      These lines disappear together with any target path or ⛔ marker written on them.");
                        W.WriteLine();
                    }

                    if( 0 < orphans.Count )
                    {
                        W.WriteLine($"  ORPHANED — already in the destination directories, not part of this deployment  ({orphans.Count}):");
                        foreach( var orphan in orphans ) W.WriteLine($"    ! {orphan}");
                        W.WriteLine($"      A directory that receives generated output is generator territory: output the");
                        W.WriteLine($"      protocol no longer produces, and anything else parked among it, does not belong");
                        W.WriteLine($"      there. Deploying BACKS EACH ONE UP AND DELETES IT. Nothing inside a dot-folder");
                        W.WriteLine($"      (.git, .idea, .claude, .vs) is ever looked at. If one of these should stay, give");
                        W.WriteLine($"      it a home outside the destination directories before deploying.");
                        W.WriteLine();
                    }

                    if( list_changed )
                    {
                        W.WriteLine($"  UNCHANGED — {reconciled_keys.Count} line(s) keep their rules exactly as they are.");
                        W.WriteLine();
                        W.WriteLine($"  The version of the list you have now is first copied to");
                        W.WriteLine($"      {md_backup_path}");
                        W.WriteLine($"  so it stays recoverable, and then the list in {md_name} is rebuilt.");
                        W.WriteLine();
                    }

                    // With not a single target location written anywhere in the list there is no deployment to
                    // offer, so the choice is not put: the list is still refreshed below — that is the whole
                    // point, it is what the targets get written on — and the run then stops with the reason.
                    var deploy_now = false;

                    if( has_some_targets )
                    {
                        W.WriteLine($"  DEPLOY NOW (y) — every target file that gets overwritten, and every orphan that gets");
                        W.WriteLine($"      deleted, is copied first into");
                        W.WriteLine($"      {backupDir}");
                        W.WriteLine($"      together with restore.bat / restore.ps1 / restore.sh. Running any one of those puts");
                        W.WriteLine($"      every one of them back and deletes the files this deployment created — an exact");
                        W.WriteLine($"      return to the current state.");
                        W.WriteLine($"  LATER (n) — nothing is deployed and nothing is deleted. The received files stay in");
                        W.WriteLine($"      {raw_files_dir_path}");
                        W.WriteLine($"      so you can edit the rules on the refreshed list and then redeploy, with no need to");
                        W.WriteLine($"      regenerate anything, by running:");
                        W.WriteLine($"          AdHocAgent \"{deployment_instructions_file}\"");
                        W.WriteLine();

                        // ReadKey THROWS when stdin is redirected, which is every scripted or piped run — take a
                        // line there instead. Interactive behaviour (single keypress, no Enter) is unchanged.
                        W.Write("Deploy now? (y = deploy now / n = update the list and stop, deploy later): ");
                        var answer = Console.IsInputRedirected ?
                                         Console.ReadLine() ?? "" :
                                         Console.ReadKey().KeyChar.ToString();
                        W.WriteLine();

                        deploy_now = answer.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);
                    }

                    if( list_changed )
                    {
                        // `md_backup_path` was fixed before the report, which already named it to the user.
                        File.Copy(deployment_instructions_file, md_backup_path);
                        LOG.Information("A backup of the old instructions file has been created at: {backup_path:l}", md_backup_path);

                        // REGENERATE the file list section.
                        var sb_new_tree = new StringBuilder();
                        foreach( var info in receiver_files_lines.Where(e => e.Value.save).OrderBy(e => e.Key).Select(e => e.Value) )
                        {
                            info.append_md_line().Append(info.customization).Append('\n');
                            sb_new_tree.Append(sb.ToString());
                            sb.Clear();
                        }

                        if( first_match != null && last_match != null )
                        {
                            // The file has a list: it is replaced in place, so everything written around it —
                            // the header, the shell/C# post-processing blocks, the notes — survives untouched.
                            var header = deployment_instructions_txt[..first_match.Index];
                            var footer = deployment_instructions_txt[(last_match.Index + last_match.Length)..];

                            deployment_instructions_txt = header + sb_new_tree + footer;
                        }
                        else
                            // No list at all — the file was hand-made, emptied, or written by an Agent whose line
                            // format the current one no longer recognises. There is nothing to splice into, so the
                            // freshly built list goes in front and the existing text is kept below it in full.
                            deployment_instructions_txt = sb_new_tree + "\n\n" + deployment_instructions_txt;

                        File.WriteAllText(deployment_instructions_file, deployment_instructions_txt, UTF8_NO_BOM);

                        LOG.Information("Instructions file has been successfully updated.");
                    }

                    // "Later" stops here — after the list has been refreshed, which is the point: the rules can
                    // now be edited against what actually arrived, and the redeploy picks the received files up
                    // from disk without asking the server for anything again.
                    if( !deploy_now && has_some_targets )
                    {
                        // Both templates take the same two arguments in the same order — Serilog binds positionally.
                        LOG.Information(list_changed ?
                                            "Nothing deployed, nothing deleted. The refreshed list is in {md_file:l} — add or adjust rules there, then redeploy with:\n\n        AdHocAgent \"{md_path:l}\"\n" :
                                            "Nothing deployed, nothing deleted. {md_file:l} is unchanged. When you are ready, redeploy with:\n\n        AdHocAgent \"{md_path:l}\"\n",
                                        md_name, deployment_instructions_file);
                        exit("", 0);
                        return;
                    }
                }

                // The list is now up to date with what arrived — refreshed just above if it needed it, generated
                // by `deploy` if the file was missing entirely. Only now does the absence of targets become the
                // reason to stop: the user is sent to a file that lists exactly the files they have to route.
                if( !has_some_targets )
                    exit($"No `target locations` detected. Please add deployment targets to `{deployment_instructions_file}` and rerun:\n\n        AdHocAgent \"{deployment_instructions_file}\"\n");

                // STEP 4: the instructions have already been parsed, above STEP 3 — the report needed the rules
                // in order to name where each newly listed file would land.

                // Execute pre-deployment commands.
                foreach( var before_deployment in new Regex(@"\[before deployment\]\((.+)\)").Matches(deployment_instructions_txt).Select(m => m.Groups[1].Value) )
                    Start_and_wait(before_deployment, raw_files_dir_path);

                var shell_tasks = new Regex(@"^([^\s].+?)(?:\r?\n(?=\s)|\r?\n?$)(?:\s+(.+?)(?:\r?\n(?=\s)|\r?\n?$))*", RegexOptions.Multiline);

                //======================== Execute custom code (shell or C#) on received files for formatting and other processing

                foreach( var (type, match) in new Regex(@"```regexp\s+(.+?)\s*```\s*```shell\s*([\s\S]*?)\s*```")
                                              .Matches(deployment_instructions_txt).Select(m => (0, m))
                                              .Concat(new Regex(@"```regexp\s+(.+?)\s*```\s*```csharp\s*([\s\S]*?)\s*```", RegexOptions.Multiline).Matches(deployment_instructions_txt).Select(m => (1, m)))
                                              .OrderBy(m => m.m.Index) )
                {
                    var selector = string.IsNullOrEmpty(match.Groups[1].Value) ?
                                       null :
                                       new Regex(match.Groups[1].Value);
                    var target = match.Groups[2].Value;
                    switch( type )
                    {
                        case 0: //shell. formatting for example
                            foreach( Match m in shell_tasks.Matches(target) )
                                foreach( var info in receiver_files_lines.Values.Where(info => !info.md && (selector == null || selector.IsMatch(info.path))) )
                                {
                                    var fullCommand = (m.Groups[1].Value + " " + string.Join(" ", m.Groups[2].Captures.Cast<Capture>()))
                                        .Replace("FILE_PATH", info.path.Contains(' ') ?
                                                                  $"\"{info.path}\"" :
                                                                  info.path);

                                    Start_and_wait(fullCommand, raw_files_dir_path);
                                }

                            continue;

                        case 1: // Compile and execute C# code on files matching the regex.
                            var cut = -1;
                            var obj = Path.GetDirectoryName(typeof(object).Assembly.Location);
                            var refs = new[] { "mscorlib.dll", "System.dll", "System.Core.dll", "System.Runtime.dll" }.Select(s => Path.Combine(obj, s))
                                                                                                                      .Concat(new[] { typeof(object).Assembly.Location, typeof(Console).Assembly.Location, Assembly.Load("System.Runtime").Location, Assembly.Load("System.Collections").Location, Assembly.Load("System.Linq").Location, Assembly.Load("System.Text.RegularExpressions").Location })
                                                                                                                      .Select(p => MetadataReference.CreateFromFile(p))
                                                                                                                      .Concat(new Regex(@"^(?:\""([^\""]+)\""\r?\n)*").Matches(target).SelectMany(m =>
                                                                                                                                                                                                  {
                                                                                                                                                                                                      cut = m.Index + m.Length;
                                                                                                                                                                                                      return m.Groups[1].Captures;
                                                                                                                                                                                                  }).SelectMany(c => AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetTypes().Any(t => t.FullName.StartsWith(c.Value)))).Distinct().Select(assembly => MetadataReference.CreateFromFile(assembly.Location)).ToArray());

                            if( 0 < cut ) target = target[cut..];
                            var compilation      = CSharpCompilation.Create(Path.GetRandomFileName(), syntaxTrees: new[] { CSharpSyntaxTree.ParseText(target) }, references: refs, options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

                            var ms     = new MemoryStream();
                            var result = compilation.Emit(ms);
                            if( !result.Success )
                            {
                                foreach( var diagnostic in result.Diagnostics )
                                    Console.Error.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                                exit("");
                            }

                            ms.Seek(0, SeekOrigin.Begin);
                            var compiledAssembly = Assembly.Load(ms.ToArray());
                            var args             = new string[1];
                            var objs             = new object[] { args };
                            var main             = compiledAssembly.GetType("Program")!.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;

                            foreach( var info in receiver_files_lines.Values.Where(info => !info.md && File.Exists(info.path) && (selector == null || selector.IsMatch(info.path))) )
                                try
                                {
                                    args[0] = info.path;
                                    main.Invoke(null, objs);
                                }
                                catch( Exception e )
                                {
                                    LOG.Error("Error: " + e.Message);
                                    exit("");
                                }

                            continue;
                    }
                }

                // --- Main Deployment Logic: Copy/Merge files based on instructions ---

                // The plan decided WHERE each file goes; this decides WHAT is written there. Content is staged in
                // temp files so a failure part-way cannot leave the destinations half-updated.
                var tempDir = Path.GetTempPath();
                Directory.CreateDirectory(tempDir);
                var tempFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach( var (info, dst) in plan )
                {
                    var tempPath = Path.Combine(tempDir, Guid.NewGuid() + Path.GetExtension(dst));

                    if( info.asis ) // library/binary file: copied verbatim, never merged
                    {
                        File.Copy(info.path, tempPath, true);
                        info.add_report("👉 " + dst);
                    }
                    else
                    {
                        var (report, content) = MergeCustomCodeIntoNewlyGeneratedFile(info.path, dst);
                        File.WriteAllText(tempPath, content, UTF8_NO_BOM);
                        info.add_report(report);
                    }

                    tempFiles[dst] = tempPath;
                }

                // --- Finalization: Reporting, Backup, and Cleanup ---

                foreach( var info in receiver_files_lines.OrderBy(e => e.Key).Select(e => e.Value) )
                {
                    sb.Clear();
                    info.append_report_line();
                    Console.Out.Write(sb.ToString());
                }


                // Execute post-deployment commands.
                foreach( var after_deployment in new Regex(@"\[after deployment\]\((.+)\)").Matches(deployment_instructions_txt).Select(m => m.Groups[1].Value) )
                    Start_and_wait(after_deployment, raw_files_dir_path);

                // Create a backup of all overwritten files. `backupDir` was fixed before the report, which
                // already named it to the user — it comes into existence only here, if we actually deploy.
                Directory.CreateDirectory(backupDir);
                var restorePlan = new Dictionary<string, string>();

                // Helper: copy `source` into the backup tree (mirroring its drive-relative path) and
                // record it in `restorePlan` so the generated restore scripts can put it back. Used for
                // both OVERWRITTEN files and STALE-deleted files — both categories must be recoverable.
                void backup_for_restore(string source)
                {
                    var pathRoot = Path.GetPathRoot(source);
                    var relativeDestPath = string.IsNullOrEmpty(pathRoot) ?
                                               source :
                                               source[pathRoot.Length..];
                    var initialBackupPath = Path.Combine(backupDir, relativeDestPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(initialBackupPath)!);
                    var uniqueBackupPath = GetUniqueBackupPath(initialBackupPath);
                    File.Copy(source, uniqueBackupPath);
                    restorePlan[source] = uniqueBackupPath;
                }

                // Destinations that do NOT yet exist are brand-new files created by THIS deployment.
                // There is nothing to back up for them, but a faithful restore must DELETE them — otherwise
                // running restore leaves the originals PLUS these additions, which is not the exact snapshot.
                // Captured here (before the overwrite at the end of process()) so File.Exists reflects the
                // pre-deployment state.
                var newly_created_files = tempFiles.Keys.Where(dest => !File.Exists(dest)).ToList();

                foreach( var dest in tempFiles.Keys.Where(File.Exists) )
                    backup_for_restore(dest);

                // --- START: ORPHANED-FILE CLEANUP ---

                // After backing up, remove everything in the destination directories that this deployment is
                // NOT writing: files EARLIER deployments generated and the protocol no longer produces (renamed
                // entities, dropped packs), and anything else parked among them.
                //
                // The rule is: a directory that receives generated output is generator territory, and what is
                // not in the write set does not belong there. That includes disabled leftovers like
                // `Compressed.cs_` — an extension nobody writes any more is exactly what "stale" looks like.
                // Every removal is backed up first and undone by the restore scripts, so this is reversible.
                //
                // The one exemption is dot-folders (`.git`, `.idea`, `.claude`, `.vs`): `walk` never descends
                // into them, so tool and VCS state living inside a destination tree is never touched.
                // `orphans` was gathered before the report, which listed every one of them by name and said they
                // would be removed. This is that promise being kept — the same list, not a fresh scan that could
                // have picked up something the user never saw.
                LOG.Information("Cleaning up {count} orphaned file(s) from destination locations...", orphans.Count);

                // Walks `root` recursively but never descends into tool/IDE state folders (`.git`, `.idea`,
                // `.claude`, `.vs`). Those hold user configuration and are never generated code.
                static IEnumerable<string> walk(string root)
                {
                    var pending = new Stack<string>();
                    pending.Push(root);
                    while( pending.Count > 0 )
                    {
                        var dir = pending.Pop();
                        string[] files;
                        string[] subdirs;
                        try
                        {
                            files   = Directory.GetFiles(dir);
                            subdirs = Directory.GetDirectories(dir);
                        }
                        catch( Exception ) { continue; } // unreadable directory - leave it alone

                        foreach( var file in files ) yield return file;
                        foreach( var subdir in subdirs )
                            if( !Path.GetFileName(subdir).StartsWith('.') )
                                pending.Push(subdir);
                    }
                }

                // Same traversal rule as `walk`, but yielding the directories themselves - used to find
                // directories emptied by the cleanup above. Dot-folders are skipped, so an empty `.claude`
                // is never pruned.
                static IEnumerable<string> walk_dirs(string root)
                {
                    var pending = new Stack<string>();
                    pending.Push(root);
                    while( pending.Count > 0 )
                    {
                        var dir = pending.Pop();
                        string[] subdirs;
                        try { subdirs = Directory.GetDirectories(dir); }
                        catch( Exception ) { continue; } // unreadable directory - leave it alone

                        foreach( var subdir in subdirs )
                            if( !Path.GetFileName(subdir).StartsWith('.') )
                            {
                                pending.Push(subdir);
                                yield return subdir;
                            }
                    }
                }

                var stale_deleted = 0;
                var undeletable   = new List<string>();

                foreach( var existingFile in orphans )
                {
                    try
                    {
                        // Back up BEFORE deletion so the restore scripts can put the file back. Without this,
                        // stale files vanish with no recovery path — even though we told the user
                        // "A full backup will be created".
                        backup_for_restore(existingFile);
                        File.Delete(existingFile);
                        stale_deleted++;
                        LOG.Information("Backed up and deleted orphaned file {orphan:l}", existingFile);
                    }
                    catch( Exception ex )
                    {
                        undeletable.Add(existingFile);
                        LOG.Warning("Could not delete orphaned file {orphan:l}: {errorMessage}", existingFile, ex.Message);
                    }
                }

                // Never silently: say exactly how much was removed, and where it can be recovered from.
                LOG.Information("Orphan cleanup: {deleted} file(s) backed up to {backup:l} and deleted{failed:l}.",
                                stale_deleted, backupDir, undeletable.Count == 0 ?
                                                              "" :
                                                              $", {undeletable.Count} could not be removed");

                // Now that overwrites and deletions are both recorded, emit restore scripts.
                // newly_created_files tells the scripts which additions to delete for an exact snapshot.
                GenerateRestoreScripts(restorePlan, newly_created_files, backupDir);


                // Now, clean up any directories that may have become empty.
                // We must process from the deepest directories upwards.
                // Only directories reachable by `walk` are considered, so an empty `.claude`/`.idea`/`.git`
                // is left alone exactly as its contents were: those folders are not ours to prune.
                var allSubDirs = destinationDirs
                                 .Where(d => Directory.Exists(d))
                                 .SelectMany(walk_dirs)
                                 .ToList();
                allSubDirs.AddRange(destinationDirs); // Also check the root target dirs themselves

                var emptyDirsDeleted = 0;
                foreach( var subDir in allSubDirs.Distinct().OrderByDescending(p => p.Length) )
                    if( Directory.Exists(subDir) && !Directory.EnumerateFileSystemEntries(subDir).Any() )
                        try
                        {
                            Directory.Delete(subDir);
                            emptyDirsDeleted++;
                        }
                        catch( Exception ex ) { LOG.Warning("Could not delete empty directory {emptyDir}: {errorMessage}", subDir, ex.Message); }


                // --- END: ORPHANED-FILE CLEANUP ---

                // Overwrite target files with the processed temp files.
                foreach( var (dest, tempPath) in tempFiles )
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(tempPath, dest, true);
                }

                try
                {
                    Directory.Delete(tempDir, true); //be nice.remove garbage
                }
                catch( Exception )
                { // ignored
                }

                // This final regeneration check is no longer needed here, as it's handled at the start.
                // You can safely remove the old `if (obsoletes_targets.Count > 0 ...)` block.

                LOG.Information("✔ Deployment successful!");
                LOG.Information("A backup of all overwritten files has been created at: {backupLocation}", backupDir);
                LOG.Information("To undo changes, run one of the restore scripts inside that directory.");
                exit("", 0);
            }

            // These are truly constant and can remain static
            // `[^\S\r\n]*` is a HORIZONTAL space run - spaces and tabs, never a line break.
            //
            // It used to be `\s*`, which also matches newlines, and that quietly ate a line of user code: when a
            // region header carried no name after the `>` (just `//#region >` or trailing blanks), the greedy `\s*`
            // walked onto the NEXT line, and `(?:.*?)\r?\n` then consumed the first line of the body as if it were
            // the header's name. The merge kept everything else, so the loss looked like a stray edit - e.g. an
            // `if (...) ...` vanished and left its `else if (...)` behind, which no longer compiles.
            static readonly Regex JAVA      = new(@"//#region[^\S\r\n]*>[^\S\r\n]*(?:.*?)\r?\n(?<content>[\s\S]*?)//#endregion[^\S\r\n]*>[^\S\r\n]*(?<uid>.*?)[^\S\r\n]*$", RegexOptions.Multiline | RegexOptions.Compiled);
            static readonly Regex CS        = new(@"#region[^\S\r\n]*>[^\S\r\n]*(?:.*?)\r?\n(?<content>[\s\S]*?)#endregion[^\S\r\n]*>[^\S\r\n]*(?<uid>.*?)[^\S\r\n]*$", RegexOptions.Multiline     | RegexOptions.Compiled);


            /// <summary>
            /// Intelligently merges a newly generated source file with an existing, user-modified version.
            /// This is the core safety feature that preserves custom code across deployments. It works by identifying
            /// special "injection points" (regions marked with a Unique ID) where user code is safe.
            /// Inside these regions, it can also update "generated blocks" while respecting the user's
            /// decision to enable, disable, or reorder them.
            /// </summary>
            /// <param name="newlyGeneratedFilePath">The path to the pristine file from the generator.</param>
            /// <param name="existingDstFilePath">The path to the existing file in the destination, which may contain user code.</param>
            /// <returns>A tuple containing a status report string and the final merged file content.</returns>
            static (string status, string content) MergeCustomCodeIntoNewlyGeneratedFile(string newlyGeneratedFilePath, string existingDstFilePath)
            {
                // If the existing file doesn't exist, there's nothing to merge.
                // We simply copy the new file to the target location.
                if( !File.Exists(existingDstFilePath) )
                    return ("👉 " + existingDstFilePath, File.ReadAllText(newlyGeneratedFilePath));

                // =================================================================================
                // STEP 1: EXTRACT - Scan the EXISTING file for all custom code.
                // We build a dictionary of all user-modified regions, keyed by their Unique ID (UID).
                // This preserves a snapshot of the user's work.
                // =================================================================================
                var existingRegionsByUid = new Dictionary<string, string>();
                var regionRegex = existingDstFilePath.EndsWith(".java") || existingDstFilePath.EndsWith(".ts") ?
                                      JAVA :
                                      CS;
                var existingFileContent = File.ReadAllText(existingDstFilePath);

                foreach( Match regionMatch in regionRegex.Matches(existingFileContent) )
                {
                    var customCode = regionMatch.Groups["content"].Value;
                    var uid        = regionMatch.Groups["uid"].Value.Trim();
                    if( !string.IsNullOrWhiteSpace(uid) && !string.IsNullOrWhiteSpace(customCode.Trim()) )
                        existingRegionsByUid[uid] = customCode;
                }

                // =================================================================================
                // STEP 1b: FOLLOW A RENUMBERED HOST. A region's UID starts with its project's slot and its entity's number
                // (`ÿÿ.HostImport`, `ÿÿ_.NamespaceCode`). A host whose number an imported project took is renumbered: the
                // generation that did it lists the old and new prefixes (`RENUMBERED`). A region whose UID the new file lacks
                // moves to the UID with the new prefix - only if the new file has that region, and nothing claims it already.
                // =================================================================================
                var newUids = regionRegex.Matches(File.ReadAllText(newlyGeneratedFilePath)).Select(m => m.Groups["uid"].Value.Trim()).Where(u => u.Length != 0).ToHashSet();

                foreach( var uid in existingRegionsByUid.Keys.Where(u => !newUids.Contains(u)).ToList() )
                    foreach( var (old, now) in renumbered_prefixes )
                    {
                        if( !uid.StartsWith(old) || uid.Length == old.Length || uid[old.Length] is not ('.' or '_') ) continue;
                        var to = now + uid[old.Length..];
                        if( !newUids.Contains(to) || existingRegionsByUid.ContainsKey(to) ) continue;

                        LOG.Information("{file}: the custom code of region {old} follows its renumbered host to region {new}", Path.GetFileName(existingDstFilePath), uid, to);
                        existingRegionsByUid[to] = existingRegionsByUid[uid];
                        existingRegionsByUid.Remove(uid);
                        break;
                    }

                // =================================================================================
                // STEP 2: REBUILD - Build the new file content using the NEWLY GENERATED file as the template.
                // We walk through the new file, and for each region, we inject the user's saved
                // customizations if a matching UID exists.
                // =================================================================================
                var mergedResult   = new StringBuilder();
                var newFileContent = File.ReadAllText(newlyGeneratedFilePath);
                var lastIndex      = 0;

                foreach( var newRegionMatch in regionRegex.Matches(newFileContent).Cast<Match>() )
                {
                    // Append the generated scaffolding that comes *before* this region.
                    mergedResult.Append(newFileContent, lastIndex, newRegionMatch.Index - lastIndex);

                    var uid                = newRegionMatch.Groups["uid"].Value.Trim();
                    var newRegionContent   = newRegionMatch.Groups["content"].Value;
                    var fullNewRegionBlock = newRegionMatch.Value;

                    // Do we have saved custom code for this UID from the old file?
                    if( existingRegionsByUid.TryGetValue(uid, out var existingRegionContent) )
                    {
                        // YES: This region existed before. Perform a smart merge of its content.
                        var mergedRegionContent = MergeRegionContent(existingRegionContent, newRegionContent);

                        // Rebuild the block BY POSITION: header, merged content, footer. The content group knows
                        // exactly where it sits inside the match, so this holds for an empty region too.
                        //
                        // It used to be `fullNewRegionBlock.Replace(newRegionContent, merged)`, which is a search and
                        // replace of ALL occurrences: a one-character content like "\n" would have been replaced in
                        // the header and the footer as well, and an EMPTY one throws (Replace does not take "").
                        var contentGroup = newRegionMatch.Groups["content"];
                        var contentEnd   = contentGroup.Index + contentGroup.Length;
                        mergedResult.Append(newFileContent, newRegionMatch.Index, contentGroup.Index - newRegionMatch.Index)
                                    .Append(mergedRegionContent)
                                    .Append(newFileContent, contentEnd, newRegionMatch.Index + newRegionMatch.Length - contentEnd);

                        // Mark this UID as processed by removing it. Any remaining UIDs at the end
                        // are "orphans"—regions that existed in the old file but not the new one.
                        existingRegionsByUid.Remove(uid);
                    }
                    else
                        // NO: This is a brand-new region. Just append it as-is from the generator.
                        mergedResult.Append(fullNewRegionBlock);

                    // Advance the index past the entire region we just processed.
                    lastIndex = newRegionMatch.Index + newRegionMatch.Length;
                }

                // Append the final segment of the new file (anything after the last region).
                mergedResult.Append(newFileContent.Substring(lastIndex));

                // =================================================================================
                // STEP 3: FINALIZE - Check for orphaned custom code to prevent data loss.
                // =================================================================================
                if( existingRegionsByUid.Count == 0 )
                    return ("✅  " + existingDstFilePath, mergedResult.ToString());

                var trulyOrphanedRegions = new Dictionary<string, string>();
                foreach( var (uid, orphanContent) in existingRegionsByUid )
                {
                    // Parse the generator blocks within this specific orphaned region.
                    var genBlocksInOrphan = ParseGenBlocks(orphanContent);

                    // If there are no generator blocks, any non-whitespace content is user code.
                    if( genBlocksInOrphan.Count == 0 )
                    {
                        if( !string.IsNullOrWhiteSpace(orphanContent) ) trulyOrphanedRegions[uid] = orphanContent;
                        continue;
                    }

                    // If there ARE generator blocks, we check the code *between* them.
                    var userCodeOnly = new StringBuilder();
                    var lastBlockEnd = 0;

                    // Iterate through blocks sorted by position to reconstruct the non-generated parts.
                    foreach( var block in genBlocksInOrphan.Values.OrderBy(b => b.BlockStart) )
                    {
                        // Append the user code that came before this block.
                        if( block.BlockStart > lastBlockEnd ) userCodeOnly.Append(orphanContent, lastBlockEnd, block.BlockStart - lastBlockEnd);

                        lastBlockEnd = block.BlockEnd; // Move cursor past this block.
                    }

                    // Append any final piece of user code that came after the last block.
                    if( lastBlockEnd < orphanContent.Length ) userCodeOnly.Append(orphanContent.Substring(lastBlockEnd));

                    // If the reconstructed string (containing only user code) is not empty, it's a true orphan.
                    if( !string.IsNullOrWhiteSpace(userCodeOnly.ToString()) )
                        trulyOrphanedRegions[uid] = orphanContent; // Add the *original* content for the report.
                }

                if( trulyOrphanedRegions.Count == 0 )
                    return ("✅  " + existingDstFilePath, mergedResult.ToString());

                // An orphan is a region the user had, but which the generator no longer creates.
                // This is a critical safety net.
                LOG.Warning("Orphaned custom code detected in {ExistingFilePath}. These regions existed in your file but were removed in the new version. Your changes will be lost if you proceed.", existingDstFilePath);

                // This loop now only runs for orphans that were confirmed to have user code.
                foreach( var (id, code) in trulyOrphanedRegions )
                {
                    Console.WriteLine($"--- ORPHANED REGION (UID: {id}) ---");
                    Console.WriteLine(code);
                    Console.WriteLine("------------------------------------");
                }

                Console.WriteLine(); // Add spacing for readability
                LOG.Warning("If you continue, the orphaned code above will be removed from the active file: '{0}'", Path.GetFileName(existingDstFilePath));
                LOG.Information("IMPORTANT: A full backup of the original file will be created before any changes are made. Your code will NOT be permanently lost and can be recovered from the backup.");

                // Ask for explicit user confirmation with the new, less alarming context.
                LOG.Warning("Proceed with the merge? (y/N): ");
                if( Console.ReadKey().Key != ConsoleKey.Y )
                {
                    exit("\nOperation cancelled by user to prevent data loss.", -1);
                    return ($"CANCELLED: {Path.GetFileName(existingDstFilePath)}", "");
                }

                Console.WriteLine();
                return ("✅⚠️ " + existingDstFilePath, mergedResult.ToString());

                // <summary>
                // Intelligently merges the content of a single injection region from an code block and new file.
                // It preserves user-written code while updating generator-provided blocks, respecting user
                // decisions like reordering or enabling/disabling blocks.
                // </summary>
                // <param name="existingContent">The code from inside the region in the existing file (contains user changes).</param>
                // <param name="newContent">The code from inside the region in the newly generated file (the template).</param>
                // <returns>The merged content for the injection region.</returns>
                string MergeRegionContent(string existingContent, string newContent)
                {
                    if( existingContent.Length == 0 ) return newContent;
                    // Clean up outdated generator warnings so they don't accumulate or linger
                    existingContent = Regex.Replace(existingContent, @"(?:\r?\n)?[ \t]*//todo 🔴[^\r\n]*", "");


                    // Helper function to comment out every line of a text block, preserving indentation and ignoring empty lines.
                    string CommentOutBlock(string blockText) => Regex.Replace(blockText, @"(?m)^([ \t]*)(?=\S)", "$1// ");

                    // Helper function to uncomment a block, removing the leading "//" from each line while preserving indentation.
                    string UncommentBlock(string blockText) => Regex.Replace(blockText, @"(?m)^([ \t]*)// ?", "$1");

                    // Checks if the content of a block is fully commented out (ignoring whitespace-only lines)
                    bool IsContentDisabled(string text)
                    {
                        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var codeLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                        return codeLines.Count > 0 && codeLines.All(l => l.TrimStart().StartsWith("//"));
                    }

                    // Find all OLD generated blocks from the existing file.
                    var existingBlocks = ParseGenBlocks(existingContent);
                    if( existingBlocks.Count == 0 ) return newContent + existingContent;

                    var result = new StringBuilder();
                    var i      = 0;

                    // Index all NEW generated blocks by their unique marker for quick lookup.
                    var newBlocks = ParseGenBlocks(newContent);


                    // --- Main Merge Loop: Iterate through the existing content's structure ---
                    // This loop respects the user's ordering of blocks and any custom code written between them.
                    foreach( var existingBlock in existingBlocks.OrderBy(e => e.Value.BlockStart) )
                    {
                        // Append the user's custom code located *between* the previous block and this one.
                        result.Append(existingContent, i, existingBlock.Value.BlockStart - i);

                        var marker = existingBlock.Key;

                        // Per instructions: "Comment out the entire block (including tags) to disable".
                        // Check if the user explicitly commented out the tags on the same line.
                        var lineStartIdx = existingContent.LastIndexOf('\n', existingBlock.Value.BlockStart);
                        lineStartIdx = lineStartIdx == -1 ? 0 : lineStartIdx + 1;
                        var linePrefix = existingContent.Substring(lineStartIdx, existingBlock.Value.BlockStart - lineStartIdx);
                        var isUserDisabledByTag = linePrefix.TrimStart().StartsWith("//");

                        var isExistingContentDisabled = IsContentDisabled(existingBlock.Value.Content);

                        if( newBlocks.TryGetValue(marker, out var newBlock) )
                        {
                            // RULE: A corresponding block exists in the new file.
                            var openTag = newContent.Substring(newBlock.BlockStart, newBlock.ContentStart - newBlock.BlockStart);
                            var closeTag = newContent.Substring(newBlock.ContentEnd, newBlock.BlockEnd - newBlock.ContentEnd);

                            if( isUserDisabledByTag )
                            {
                                // User explicitly disabled the block by commenting out the tags. Apply comments safely.
                                result.Append(CommentOutBlock(openTag));
                                result.Append(CommentOutBlock(newBlock.Content));
                                result.Append(CommentOutBlock(closeTag));
                            }
                            else
                            {
                                // Tags are NOT commented out.
                                result.Append(openTag);

                                // If the existing content was NOT fully commented, but the new one is, we UNCOMMENT the new content
                                // to respect the user's choice to enable a block disabled-by-default.
                                // If the existing content WAS fully commented, we leave the new content AS IS.
                                if( !isExistingContentDisabled && IsContentDisabled(newBlock.Content) )
                                    result.Append(UncommentBlock(newBlock.Content));
                                else
                                    result.Append(newBlock.Content);

                                result.Append(closeTag);
                            }

                            // Mark the new block as processed so it isn't added again at the end.
                            newBlocks.Remove(marker);
                        }
                        else if( !isExistingContentDisabled )
                        {
                            // RULE: A block the user HAD ENABLED was removed by the generator.
                            // Preserve it as commented-out code with a "todo" warning to prevent data loss.
                            result.AppendLine();
                            result.AppendLine("//todo 🔴 The following code block was removed by the code generator. Please review.");
                            var openTag = existingContent
                                          .Substring(existingBlock.Value.BlockStart, existingBlock.Value.ContentStart - existingBlock.Value.BlockStart)
                                          .Replace($"//{existingBlock.Key}<", "");

                            var closeTag = existingContent
                                           .Substring(existingBlock.Value.ContentEnd, existingBlock.Value.BlockEnd - existingBlock.Value.ContentEnd)
                                           .Replace($"//{existingBlock.Key}/>", "");

                            result.Append(openTag);
                            result.Append(CommentOutBlock(existingBlock.Value.Content));
                            result.Append(closeTag);
                            result.AppendLine();
                        }
                        // If the block was removed by the generator AND the user already had it disabled, we simply let it disappear.

                        i = existingBlock.Value.BlockEnd;
                    }


                    // --- Final Step: Add any completely new blocks ---
                    // These are blocks that exist in the new file but not in the old one.
                    if( newBlocks.Count != 0 )
                    {
                        // Check if at least one of the new, unprocessed blocks is ACTIVE by default.
                        var newActivated = newBlocks.Values.Any(m => !m.Content.TrimStart().StartsWith("//"));

                        // RULE: If a new ACTIVE block is added, warn the user as it might change behavior.
                        if( newActivated )
                        {
                            result.AppendLine();
                            result.AppendLine("//todo 🔴 New active generated code was added by the generator. Please review as it may affect your custom logic.");
                        }

                        // Append all new blocks at the end of the region, ordered as they appear in the new file.
                        foreach( var newBlock in newBlocks.OrderBy(e => e.Value.BlockStart) ) result.Append(newContent, newBlock.Value.BlockStart, newBlock.Value.BlockEnd - newBlock.Value.BlockStart);
                    }

                    // Append any remaining user code that was after the very last generated block.
                    result.Append(existingContent.AsSpan(i));


                    return result.ToString();
                }

                // <summary>
                // Parses the content and extracts all generator blocks, identified by special markers.
                // </summary>
                // <param name="content">The source code content to parse.</param>
                // <returns>A dictionary mapping each block's unique marker to its parsed information.</returns>
                static Dictionary<string, GenBlock> ParseGenBlocks(ReadOnlySpan<char> content)
                {
                    var result = new Dictionary<string, GenBlock>();
                    var i      = 0;

                    while( i < content.Length )
                    {
                        var start = content.Slice(i).IndexOf("//");
                        if( start == -1 ) break;

                        start = i + start;
                        var ii = start + 2;
                        if( content.Length <= ii ) break;

                        var (marker, markerLength) = GetMarker(content.Slice(ii));
                        if( marker == null )
                        {
                            i = ii;
                            continue;
                        }

                        ii += markerLength;
                        if( content.Length <= ii || content[ii] != '<' )
                        {
                            i = start + 2;
                            continue;
                        }

                        ii++; // Skip '<'

                        // Find the start of the content, skipping only HORIZONTAL whitespace (spaces, tabs).
                        // This preserves newlines.
                        var contentStart = ii;
                        while( contentStart < content.Length && (content[contentStart] == ' ' || content[contentStart] == '\t') ) { contentStart++; }

                        var endTag       = $"//{marker}/>";
                        var endTagOffset = content.Slice(contentStart).IndexOf(endTag);
                        if( endTagOffset == -1 )
                        {
                            i = start + 2;
                            continue;
                        }

                        var endTagStart = contentStart + endTagOffset;
                        var blockEnd    = endTagStart  + endTag.Length;

                        // Find the end of the content, trimming only HORIZONTAL whitespace from the end.
                        var contentEnd = endTagStart;
                        while( contentEnd > contentStart && (content[contentEnd - 1] == ' ' || content[contentEnd - 1] == '\t') ) { contentEnd--; }

                        var blockContent = content[contentStart..contentEnd].ToString();

                        result[marker] = new GenBlock(
                                                      Content: blockContent,
                                                      BlockStart: start,
                                                      BlockEnd: blockEnd,
                                                      ContentStart: contentStart,
                                                      ContentEnd: contentEnd
                                                     );
                        i = blockEnd;
                    }

                    return result;
                }


                static (string? Marker, int markerLength) GetMarker(ReadOnlySpan<char> span)
                {
                    if( span.IsEmpty ) return (null, 0);

                    var category = char.GetUnicodeCategory(span[0]);
                    if( category != UnicodeCategory.OtherSymbol && category != UnicodeCategory.Surrogate ) return (null, 0);

                    var length = char.IsSurrogatePair(span[0], 1 < span.Length ?
                                                                   span[1] :
                                                                   '\0') ?
                                     2 :
                                     1;
                    return (span.Slice(0, length).ToString(), length);
                }
            }


            public readonly record struct GenBlock(
                string Content,
                int    BlockStart,
                int    BlockEnd,
                int    ContentStart,
                int    ContentEnd
            );

            // Replace the ENTIRE GenerateRestoreScripts method with this new version
            /// <summary>
            /// Generates restore scripts (Batch, PowerShell, Shell) in the backup directory.
            /// </summary>
            static void GenerateRestoreScripts(Dictionary<string, string> restorePlan, List<string> newlyCreatedFiles, string backupDir)
            {
                // Each script copies from backup paths written RELATIVE to this script's own folder.
                // So the very first thing every script must do is switch its working directory to that
                // folder — otherwise running the script by full path (or from any other directory) leaves
                // the relative sources unresolved and `copy`/`cp` silently restores nothing.
                var restoreBat = new StringBuilder("@echo off\r\ncd /d \"%~dp0\"\r\n");
                var restorePs1 = new StringBuilder("# PowerShell Restore Script\r\nSet-Location -LiteralPath $PSScriptRoot\r\n");
                var restoreSh  = new StringBuilder("#!/bin/sh\n# Run 'chmod +x restore.sh' to make this script executable.\ncd \"$(dirname \"$0\")\" || exit 1\n");

                foreach( var (originalPath, backupPath) in restorePlan )
                {
                    var relativeBackupPath = Path.GetRelativePath(backupDir, backupPath);

                    var destDir = Path.GetDirectoryName(originalPath)!;

                    // --- Batch Script (.bat) ---
                    // Use the relative path for the source file.
                    restoreBat.AppendLine($"if not exist \"{destDir}\" mkdir \"{destDir}\"");
                    restoreBat.AppendLine($"copy /Y \"{relativeBackupPath}\" \"{originalPath}\"");

                    // --- PowerShell Script (.ps1) ---
                    // Recreate the destination directory first: stale files whose directories were removed
                    // by the post-deploy empty-directory cleanup no longer have a parent folder, and
                    // Copy-Item does NOT create intermediate directories (unlike the .bat/.sh branches,
                    // which mkdir up front), so without this the copy fails with
                    // "Could not find a part of the path".
                    restorePs1.AppendLine($"New-Item -ItemType Directory -Force -Path \"{destDir}\" | Out-Null");
                    // Use the relative path for the source path.
                    restorePs1.AppendLine($"Copy-Item -Path \"{relativeBackupPath}\" -Destination \"{originalPath}\" -Recurse -Force");

                    // --- Shell Script (.sh) ---
                    // Use the relative path for the source (ensure forward slashes).

                    restoreSh.AppendLine($"mkdir -p \"{destDir.Replace('\\', '/')}\"");
                    restoreSh.AppendLine($"cp -rf \"{relativeBackupPath.Replace('\\', '/')}\" \"{originalPath.Replace('\\', '/')}\"");
                }

                // Files this deployment ADDED (absent at backup time) are NOT deleted unconditionally:
                // copying the originals back already restores A and B, and a blunt delete here would be a
                // surprise. Instead the restore scripts ASK the user at restore time — [A]ll / [N]one /
                // [S]elect per-file (default: keep) — so the user decides, and can pick individual files.
                if( newlyCreatedFiles.Count > 0 )
                {
                    // ---------- Batch (.bat) ----------
                    restoreBat.AppendLine();
                    restoreBat.AppendLine("rem === Files added by this deployment (absent at backup time) ===");
                    restoreBat.AppendLine("echo.");
                    restoreBat.AppendLine($"echo The following {newlyCreatedFiles.Count} file(s) were ADDED by the deployment and did not exist at backup time:");
                    foreach( var p in newlyCreatedFiles ) restoreBat.AppendLine($"echo   {p}");
                    restoreBat.AppendLine("echo.");
                    restoreBat.AppendLine("set \"ADHOC_MODE=\"");
                    restoreBat.AppendLine("set /p \"ADHOC_MODE=Delete added files? [A]ll / [N]one / [S]elect per-file (default N): \"");
                    restoreBat.AppendLine("if /i \"%ADHOC_MODE%\"==\"A\" goto __adhoc_del_all");
                    restoreBat.AppendLine("if /i \"%ADHOC_MODE%\"==\"S\" goto __adhoc_del_select");
                    restoreBat.AppendLine("echo Keeping all added files.");
                    restoreBat.AppendLine("goto __adhoc_del_done");
                    restoreBat.AppendLine(":__adhoc_del_all");
                    foreach( var p in newlyCreatedFiles ) restoreBat.AppendLine($"if exist \"{p}\" del /F /Q \"{p}\"");
                    restoreBat.AppendLine("goto __adhoc_del_done");
                    restoreBat.AppendLine(":__adhoc_del_select");
                    foreach( var p in newlyCreatedFiles )
                    {
                        restoreBat.AppendLine("set \"ADHOC_ANS=\"");
                        restoreBat.AppendLine($"set /p \"ADHOC_ANS=Delete [{p}]? [y/N] \"");
                        restoreBat.AppendLine($"if /i \"%ADHOC_ANS%\"==\"y\" if exist \"{p}\" del /F /Q \"{p}\"");
                    }
                    restoreBat.AppendLine("goto __adhoc_del_done");
                    restoreBat.AppendLine(":__adhoc_del_done");

                    // ---------- PowerShell (.ps1) ----------
                    restorePs1.AppendLine();
                    restorePs1.AppendLine("# === Files added by this deployment (absent at backup time) ===");
                    restorePs1.AppendLine("$adhocAdded = @(");
                    for( var i = 0; i < newlyCreatedFiles.Count; i++ )
                        restorePs1.AppendLine($"  \"{newlyCreatedFiles[i]}\"{(i < newlyCreatedFiles.Count - 1 ? "," : "")}");
                    restorePs1.AppendLine(")");
                    restorePs1.AppendLine("Write-Host \"\"");
                    restorePs1.AppendLine("Write-Host \"The following $($adhocAdded.Count) file(s) were ADDED by the deployment and did not exist at backup time:\"");
                    restorePs1.AppendLine("$adhocAdded | ForEach-Object { Write-Host \"  $_\" }");
                    restorePs1.AppendLine("$adhocMode = Read-Host \"Delete added files? [A]ll / [N]one / [S]elect per-file (default N)\"");
                    restorePs1.AppendLine("if ($adhocMode -match '^[Aa]') { $adhocAdded | ForEach-Object { if (Test-Path -LiteralPath $_) { Remove-Item -LiteralPath $_ -Force } } }");
                    restorePs1.AppendLine("elseif ($adhocMode -match '^[Ss]') {");
                    restorePs1.AppendLine("  foreach ($adhocF in $adhocAdded) {");
                    restorePs1.AppendLine("    $adhocAns = Read-Host \"Delete `\"$adhocF`\"? [y/N]\"");
                    restorePs1.AppendLine("    if ($adhocAns -match '^[Yy]' -and (Test-Path -LiteralPath $adhocF)) { Remove-Item -LiteralPath $adhocF -Force }");
                    restorePs1.AppendLine("  }");
                    restorePs1.AppendLine("}");
                    restorePs1.AppendLine("else { Write-Host \"Keeping all added files.\" }");

                    // ---------- Shell (.sh) ----------
                    restoreSh.AppendLine();
                    restoreSh.AppendLine("# === Files added by this deployment (absent at backup time) ===");
                    restoreSh.AppendLine("echo \"\"");
                    restoreSh.AppendLine($"echo \"The following {newlyCreatedFiles.Count} file(s) were ADDED by the deployment and did not exist at backup time:\"");
                    foreach( var p in newlyCreatedFiles ) restoreSh.AppendLine($"echo \"  {p.Replace('\\', '/')}\"");
                    restoreSh.AppendLine("printf \"Delete added files? [A]ll / [N]one / [S]elect per-file (default N): \"");
                    restoreSh.AppendLine("read adhoc_mode");
                    restoreSh.AppendLine("case \"$adhoc_mode\" in");
                    restoreSh.AppendLine("  [Aa]*)");
                    foreach( var p in newlyCreatedFiles ) restoreSh.AppendLine($"    rm -f \"{p.Replace('\\', '/')}\"");
                    restoreSh.AppendLine("    ;;");
                    restoreSh.AppendLine("  [Ss]*)");
                    foreach( var p in newlyCreatedFiles )
                    {
                        var fp = p.Replace('\\', '/');
                        restoreSh.AppendLine($"    printf 'Delete \"%s\"? [y/N] ' \"{fp}\"; read adhoc_ans; case \"$adhoc_ans\" in [Yy]*) rm -f \"{fp}\";; esac");
                    }
                    restoreSh.AppendLine("    ;;");
                    restoreSh.AppendLine("  *) echo \"Keeping all added files.\" ;;");
                    restoreSh.AppendLine("esac");
                }

                File.WriteAllText(Path.Combine(backupDir, "restore.bat"), restoreBat.ToString(), UTF8_NO_BOM);
                File.WriteAllText(Path.Combine(backupDir, "restore.ps1"), restorePs1.ToString(), UTF8_NO_BOM);
                File.WriteAllText(Path.Combine(backupDir, "restore.sh"),  restoreSh.ToString(),  UTF8_NO_BOM);
            }

            static string GetUniqueBackupPath(string fullPath)
            {
                if( !File.Exists(fullPath) )
                    return fullPath;

                var    directory = Path.GetDirectoryName(fullPath)!;
                var    filename  = Path.GetFileNameWithoutExtension(fullPath);
                var    extension = Path.GetExtension(fullPath);
                var    counter   = 1;
                string newFullPath;

                do { newFullPath = Path.Combine(directory, $"{filename} ({counter++}){extension}"); }
                while( File.Exists(newFullPath) );

                return newFullPath;
            }
        }

        public static string Start_and_wait(string exe_args, string WorkingDirectory)
        {
            var regex = new Regex(@"^(?:""([^""]+)""|(\S+))\s+(.*)");
            var match = regex.Match(exe_args);
            var exe = match.Groups[1].Success ?
                          match.Groups[1].Value :
                          match.Groups[2].Value;
            var args = match.Groups[3].Value;
            return Start_and_wait(exe, args, WorkingDirectory);
        }

        // Regular expression to match quoted paths or paths without spaces
        static readonly Regex paths = new("(\".*?\"\\s*)|(\\S+\\s*)", RegexOptions.Compiled);

        static readonly Regex root_path = new(@"^\""?[\\/](InCPP|InCS|InGO|InJAVA|InRS|InTS)[\\/]", RegexOptions.Compiled);

        // External formatters/compilers (prettier, tsc, astyle...) colorize their diagnostics with ANSI SGR
        // sequences. Keep them only when stdout is a terminal that can paint them; otherwise they would show
        // up as unreadable literal escapes.
        static readonly Regex ansi_sgr = new(@"\x1B\[[0-9;]*[a-zA-Z]", RegexOptions.Compiled);

        static string terminal_ready(string text) => color_ok ?
                                                         text :
                                                         ansi_sgr.Replace(text, "");

        /// <summary>
        /// Starts and waits for a process to exit, handling path conversions, and error reporting.
        /// </summary>
        /// <param name="exe">The path to the executable.</param>
        /// <param name="args">The arguments for the executable.</param>
        /// <param name="WorkingDirectory">The working directory for the process.</param>
        /// <returns>The standard output of the process.</returns>
        public static string Start_and_wait(string exe, string args, string WorkingDirectory)
        {
            args = string.Join("", paths.Matches(args)
                                        .Select(match => match.Value)
                                        .Select(s =>
                                                {
                                                    if( File.Exists(s) || Directory.Exists(s) || !root_path.IsMatch(s) ) return s;

                                                    var to = Path.DirectorySeparatorChar;
                                                    var from = Path.DirectorySeparatorChar == '/' ?
                                                                   '\\' :
                                                                   '/';

                                                    var ret = Path.Join(RawFilesDirPath.Replace("\"", "").Replace(from, to), s.Replace("\"", "").Replace(from, to));
                                                    return ret.Contains(' ') ?
                                                               "\"" + ret + "\"" :
                                                               ret;
                                                }
                                               ));


            var startInfo = new ProcessStartInfo
                            {
                                FileName               = exe,
                                Arguments              = args,
                                WorkingDirectory       = WorkingDirectory,
                                RedirectStandardOutput = true,
                                RedirectStandardError  = true,
                                UseShellExecute        = true,
                                CreateNoWindow         = true
                            };
            var error = "";
            // Attempt to execute process with different configurations to handle path/extension issues
            for( var i = 0;
                 i < 3;
                 i++, startInfo.FileName = exe + (OperatingSystem.IsWindows() ?
                                                      i == 1 ?
                                                          ".exe" : // Try .exe extension on Windows
                                                          ".cmd" : // Try .cmd on Windows
                                                      i == 1 ?
                                                          ".sh" :                                      // Try .sh on non-Windows
                                                          ".bash"), startInfo.UseShellExecute = true ) // Try different extensions and shell execution
            {
                try
                {
                    // First attempt: Execute the process
                    using( var process = Process.Start(startInfo) )
                    {
                        // Read the entire output of the process
                        error += process.StandardError.ReadToEnd();
                        var output = error == "" ? //!!!
                                         process.StandardOutput.ReadToEnd() :
                                         "";
                        // Wait for the process to complete
                        process.WaitForExit();
                        // Return the output if successful
                        return output;
                    }
                }
                catch( Exception )
                {
                    // Second attempt: Modify start info and retry
                    startInfo.UseShellExecute = false; // Disable shell execution for second attempt
                    try
                    {
                        // Second attempt: Execute the process with modified start info
                        using( var process = Process.Start(startInfo) )
                        {
                            error += process.StandardError.ReadToEnd();
                            var output = error == "" ? //!!!
                                             process.StandardOutput.ReadToEnd() :
                                             "";
                            process.WaitForExit();
                            if( 0 < error.Length )
                            {
                                // `:l` renders the properties literally — without it Serilog emits them as quoted JSON
                                // strings, turning newlines into `\n` and the tools ANSI colors into `[36m` noise.
                                LOG.Error("An error occurred while executing\n{command_line:l}\n\n{error:l}\n\nWould you like to continue? Enter 'N' to stop.",
                                          (exe + " " + startInfo.Arguments).Replace('\\', '/'),
                                          terminal_ready(error.TrimEnd()));

                                if( Console.Read() is 'N' or 'n' )
                                {
                                    exit("Bye", -1);
                                    return "";
                                }
                            }

                            return output; // Return standard output on success
                        }
                    }
                    catch( Exception ee )
                    {
                        // If both attempts fail after the first iteration, exit with error
                        if( 1 < i ) { exit($"Error executing {exe} {args}. Exception details:\n{ee}", -1); }
                        // If first iteration fails, loop will continue to next attempt
                    }
                }
            }

            // Note: Code should not reach here in normal execution, indicates both attempts on first iteration failed.
            return exe + " " + args; // Return command line for debugging if something unexpected happens
        }

        /// <summary>
        /// Updates the PersonalVolatileUUID in the application properties file.
        ///
        /// The token being replaced is kept as PreviousVolatileUUID. The server keeps the old token valid until the
        /// new one is confirmed, so if the new one is ever rejected, re-applying the previous one is the way back in
        /// without a new sign-up. The file is written beside and swapped in: a crash in the middle of rewriting the
        /// real file used to leave a truncated config and no token at all.
        /// </summary>
        /// <param name="UUID">The UUID string to set.</param>
        public static void updatePersonalVolatileUUID(string UUID)
        {
            // Ensure the input is a valid GUID before saving.
            if( !Guid.TryParse(UUID, out var guid) ) return; // Or throw an exception if invalid input is a critical error.
            var fresh = guid.ToString("D");

            if( app_props.HasKey("PersonalVolatileUUID") ) // If UUID exists, update it and back up the old one.
            {
                var current = app_props["PersonalVolatileUUID"].AsString?.Value ?? "";
                if( current == fresh ) return; // already there: nothing to back up, no reason to rewrite the file

                if( !string.IsNullOrWhiteSpace(current) ) app_props["PreviousVolatileUUID"] = new TomlString { Value = current };
                app_props["PersonalVolatileUUID"]                                           = new TomlString { Value = fresh };
            }
            else // Otherwise, add the new UUID.
                app_props.Add("PersonalVolatileUUID", new TomlString { Value = fresh });

            var file = app_props_file;
            var tmp  = file + ".tmp";
            using( var writer = File.CreateText(tmp) ) app_props.WriteTo(writer);
            File.Move(tmp, file, overwrite: true);
        }

        /// <summary>
        /// Retrieves the PersonalVolatileUUID and parses it into two ulong values.
        /// </summary>
        /// <param name="uuid_hi">Output parameter for the high 64 bits of the UUID.</param>
        /// <param name="uuid_lo">Output parameter for the low 64 bits of the UUID.</param>
        public static void PersonalVolatileUUID(out ulong uuid_hi, out ulong uuid_lo)
        {
            // Default to zero in case of failure.
            uuid_hi = 0;
            uuid_lo = 0;

            if( !app_props.HasKey("PersonalVolatileUUID") )
                exit("Error: 'PersonalVolatileUUID' not found in configuration. Please provide one first.");

            var uuidString = app_props["PersonalVolatileUUID"].AsString!.Value;

            // Use Guid.TryParse to safely handle any valid GUID format (with or without hyphens, etc.).
            // A silent return here sent Login{0,0} and the server's rejection looked like a lost token.
            if( !Guid.TryParse(uuidString, out var guid) )
                exit($"Error: 'PersonalVolatileUUID' in {app_props_file} is not a valid UUID: '{uuidString}'. Re-apply your UUID: AdHocAgent <uuid>");

            // Convert the parsed GUID to a clean 32-digit hex string ("N" format).
            var cleanHexString = guid.ToString("N");

            // Split the string into high and low parts. Substring is safe here due to the fixed length.
            var hiString = cleanHexString.Substring(0, 16);
            var loString = cleanHexString.Substring(16);

            // Use the safer TryParse to convert hex strings to ulongs.
            ulong.TryParse(hiString, NumberStyles.HexNumber, null, out uuid_hi);
            ulong.TryParse(loString, NumberStyles.HexNumber, null, out uuid_lo);
        }
    }
}