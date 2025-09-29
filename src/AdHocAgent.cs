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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using org.unirail.Agent;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Tommy;

//https://github.com/dezhidki/Tommy - TOML parser library

namespace org.unirail
{
    /// <summary>
    /// Static class responsible for AdHoc Agent operations, including protocol description processing,
    /// code generation, and deployment.
    /// </summary>
    static class AdHocAgent
    {
        /// <summary>
        /// Serilog enricher to add file path and line number to log events.
        /// </summary>
        class CallerEnricher : ILogEventEnricher
        {
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

        /// <summary>
        /// Gets the path to the application properties file (AdHocAgent.toml).
        /// If the file doesn't exist, it extracts a template from embedded resources.
        /// </summary>
        public static string app_props_file
        {
            get
            {
                var file = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "AdHocAgent.toml");

                if (File.Exists(file))
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
        /// Unzips a stream to a destination folder. Creates a temporary file for processing.
        /// </summary>
        /// <param name="sourceStream">The source stream to unzip.</param>
        /// <param name="destinationFolder">The destination folder for the unzipped files.</param>
        public static void unzip(Stream src, string dst_folder)
        {
            var tmp_src = File.OpenWrite(new_random_tmp_path);
            src.CopyTo(tmp_src);
            tmp_src.Flush();
            tmp_src.Close();
            unzip(tmp_src.Name, dst_folder);
            File.Delete(tmp_src.Name);
        }

        /// <summary>
        /// Unzips a file to a destination folder using 7-Zip command-line tool.
        /// Requires 7-Zip to be installed and in the system PATH.
        /// </summary>
        /// <param name="sourceFile">The path to the zip file.</param>
        /// <param name="destinationFolder">The destination folder for the unzipped files.</param>
        public static void unzip(string src_file, string dst_folder) => Process.Start(new ProcessStartInfo
        {
            FileName = "7z",
            RedirectStandardOutput = true,
            Arguments = $" x \"{src_file}\" -aoa -o\"{dst_folder}\"",
            WindowStyle = ProcessWindowStyle.Hidden
        })!.WaitForExit();

        /// <summary>
        /// Generates a new random temporary file path using GUID.
        /// </summary>
        static string new_random_tmp_path => Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());

        /// <summary>
        /// Zips a byte array into a 7z archive with a given name.
        /// Creates a temporary file to store the byte array before zipping.
        /// </summary>
        /// <param name="sourceBytes">The byte array to zip.</param>
        /// <param name="name">The name of the zip archive (without extension).</param>
        /// <returns>The zipped byte array.</returns>
        public static byte[] zip(byte[] src, string name)
        {
            name = Path.Join(Path.GetTempPath(), name);
            File.WriteAllBytes(name, src);

            var ret = zip([name]);
            File.Delete(name);
            return ret;
        }

        /// <summary>
        /// Zips a list of files into a 7z archive using 7-Zip command-line tool.
        /// Uses PPMd compression for maximum compression.
        /// </summary>
        /// <param name="filesPaths">An enumerable of file paths to be zipped.</param>
        /// <returns>The zipped byte array.</returns>
        public static byte[] zip(IEnumerable<string> files_paths)
        {
            var tmp_zip = new_random_tmp_path;

            Process.Start(new ProcessStartInfo
            {
                FileName = "7z", //Use the PPMd compression, the compression level to the maximum
                Arguments = $" a -t7z -m0=PPMd -mx=9 -mmem=256m  \"{tmp_zip}.\" {string.Join(' ', files_paths.Select(path => "\"" + path + "\""))}",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            })!.WaitForExit();

            using var ret = new MemoryStream();
            using (var file = File.OpenRead(tmp_zip))
                file.CopyTo(ret);


            File.Delete(tmp_zip);
            return ret.ToArray();
        }

        /// <summary>
        /// Flag indicating if the agent is in testing mode (triggered by '!' suffix in path argument).
        /// </summary>
        public static bool is_testing;

        public static bool is_diagramming;

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

            using (var _7z = new Process()) //check that 7z is or
            {
                _7z.StartInfo = new ProcessStartInfo //ensure check
                {
                    FileName = "7z",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false, // Required for redirection to work
                    CreateNoWindow = true   // Do not create a visible console window
                };

                _7z.Start();
                _7z.WaitForExit();

                if (_7z.ExitCode != 0)
                    throw new Exception("7z command is not available or failed to execute. Please ensure 7-Zip is installed and added to the system PATH.");
            }

            if (paths.Length == 0)
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

            if (!paths[0].Contains('.')) //UUID
            {
                updatePersonalVolatileUUID(paths[0]);
                LOG.Information("Volatile personal UUID updated successfully!");
                return;
            }

            if (paths[0].EndsWith(".json") || paths[0].EndsWith(".yaml")) // Converts OpenAPI json/yaml file into AdHoc protocol description format.
            {
                provided_path = paths[0];
                await OpenApi_To_AdHoc_Converter.convert(paths[0], paths.Length == 1 ?
                                                                       paths[0][..^4] + "cs" : //If the second argument is skipped, the AdHocAgent utility will output the `.cs` file next to the provided OpenAPI file.
                                                                       paths[1]);
                return;
            }


            if (paths[0].EndsWith(".md")) //  repeat only the deployment process according to instructions in the .md file, already received source files in the `working directory`
            {
                provided_path = paths[0];
                Deployment.redeploy(provided_path = paths[0]);
                return;
            }

            if (paths[0].EndsWith(".cs?")) // run provided protocol description file viewer
            {
                is_diagramming = true;
                provided_path = paths[0][..^1]; //cut '?'
                set_provided_paths(paths[1..]);
                await ChannelToObserver.Start();
                return;
            }

            if (!app_props.HasKey("PersonalVolatileUUID"))
                exit($"Cannot find your personal volatile UUID in the {app_props_file} file. Please request and apply one according to the instructions in the manual https://github.com/AdHoc-Protocol/AdHoc-protocol.");


            is_testing = paths[0].EndsWith("!");

            provided_path = is_testing ?
                                paths[0][..^1] :
                                paths[0];

            var result_output_folder = Path.Join(destination_dir_path, Path.GetFileName(provided_path)[..^Path.GetExtension(provided_path).Length]);

            /*
            if (new FileInfo(provided_path).IsReadOnly)     //since last time content is not changed
                if (Directory.Exists(result_output_folder)) //generated result from server exists
                {
                    Deployment.deploy(result_output_folder); //just re-deploy
                    return;
                }
                else //to query uploaded task result
                {
                    ChannelToServer.Start();
                    return;
                }
                */


            #region .cs files - protocol description file processing
            if (provided_path.EndsWith(".cs"))
            {
                set_provided_paths(paths[1..]);
                await ChannelToServer.Start(ProjectImpl.init());
                return;
            }
            #endregion
            #region .proto files processing
            if (1 < paths.Length && !paths[^1].EndsWith(".proto")) //if destination_dir_path explicitly provided instead of Directory.GetCurrentDirectory()
                if (!Directory.Exists(destination_dir_path = paths[^1]))
                    Directory.CreateDirectory(destination_dir_path);


            var all_files = new Dictionary<string, List<string>>();

            foreach (var file in (File.Exists(provided_path) ?
                                      [provided_path] :
                                      Directory.EnumerateFiles(provided_path, "*.proto", SearchOption.AllDirectories))
                    .Concat(
                            1 < paths.Length && paths[1].EndsWith(".proto") ?
                                Directory.EnumerateFiles(paths[1], "*.proto", SearchOption.AllDirectories) :
                                []
                           ))
            {
                var key = Path.GetFileName(file);
                if (all_files.TryGetValue(key, out var val))
                    val.Add(file);
                else all_files[key] = [.. new[] { file }];
            }

            var syntax = new Regex(@"^\s*syntax\s*=\s*.*;", RegexOptions.Multiline);
            var imported = new HashSet<string>();
            Regex imports = new(@"^\s*import\s+(?:public\s+)?""([^""]+)""\s*;", RegexOptions.Multiline); //  import "myproject/other_protos.proto"; /  import public "new.proto";
            Regex package = new(@"^\s*package\s+[""']?([^""';\s]+)[""']?\s*;", RegexOptions.Multiline);  // package foo.bar;
            Regex dot = new(@"(?<=\s)\.(?=[^\.\s])", RegexOptions.Multiline);                        // package foo.bar;
            Regex asterisk = new(@"(?<=^\s*//.*)\*(?=\*/|\/*)", RegexOptions.Multiline);


            var package_proto = new Dictionary<string, string>();

            string process_proto_files(IEnumerable<string> files)
            {
                void process_proto_file(string proto_file_path)
                {
                    if (!imported.Add(proto_file_path)) return;
                    var proto = File.ReadAllText(proto_file_path);

                    var pack = package.Matches(proto).Select(m => m.Groups[1].Value).FirstOrDefault() ?? "";

                    proto = $"\n//@@#region {HasDocs.brush(Path.GetFileName(proto_file_path))}\n" +
                            proto +
                            $"\n//@@#endregion {HasDocs.brush(Path.GetFileName(proto_file_path))}\n";

                    proto = package.Replace(syntax.Replace(proto, ""), "");

                    if (package_proto.ContainsKey(pack)) package_proto[pack] = package_proto[pack] + proto;
                    else package_proto.Add(pack, proto);


                    foreach (var match in imports.Matches(proto).OrderBy(Match => -Match.Index)) //bottom ==> up
                    {
                        var import = match.Groups[1].Value;

                        var import_file = "";
                        if (all_files.TryGetValue(Path.GetFileName(import), out var paths))
                            if (paths.Count == 1)
                                import_file = paths[0];
                            else
                            {
                                while (!paths.Any(p => p.Replace('\\', '/').EndsWith(import)))
                                    import = import[(import.IndexOf('/', 1) + 1)..];

                                import_file = paths.First(p => p.Replace('\\', '/').EndsWith(import));
                            }
                        else
                            exit($"The .proto file `{import}` does not exist and cannot be imported into .proto file `{proto_file_path}`.");

                        process_proto_file(import_file);
                    }
                }

                imported.Clear();
                package_proto.Clear();
                foreach (var file in files) process_proto_file(file);
                var result_proto = "";

            repeat:
                while (0 < package_proto.Count)
                    foreach (var (key, value) in package_proto.OrderBy(p => -p.Key.Count(ch => ch == '.')))
                    {
                        var path = key.Split('.');
                        var code = $$"""
                                     //@@public struct {{(path[^1] == "" ? "MyPack" : HasDocs.brush(path[^1]))}} {
                                       {{value}}
                                     //@@}
                                     """;
                        package_proto.Remove(key);

                        if (1 < path.Length)
                        {
                            var key2 = string.Join('.', path, 0, path.Length - 1);
                            package_proto[key2] = code + (package_proto.TryGetValue(key2, out var val) ?
                                                              val :
                                                              "");
                            goto repeat;
                        }

                        result_proto += code;
                    }

                result_proto = dot.Replace(result_proto, ""); //removing the dot at the beginning of types
                //				                          .tensorflow.DataType  [] dtype;
                //                               repeated .tensorflow.TensorProto tensor = 1;

                result_proto = asterisk.Replace(result_proto, "⁕"); // replace '*' with '⁕' in comments like
                //  // */ <- This should not close the generated doc comment

                return result_proto.Length == 0 ?
                           "" :
                           $"""
                            syntax = "proto3";
                            {result_proto}
                            """;
            }


            if (File.Exists(provided_path))
            {
                var str = process_proto_files([provided_path]);
                var bytes = new byte[AdHoc.varint_bytes(str)];
                AdHoc.varint(str.AsSpan(), new Span<byte>(bytes));

                await ChannelToServer.Start(new AdHocProtocol.Agent_.Proto
                {
                    task = task,
                    name = Path.GetFileName(provided_path),
                    _proto = zip(bytes, Path.GetFileName(provided_path)[..^6])
                });
            }
            else if (!Directory.Exists(provided_path))
                exit($"Provided path {provided_path} does not exists.", 2);


            var tmp = Directory.CreateDirectory(new_random_tmp_path).FullName;
            var cut = Path.GetDirectoryName(provided_path)!.Length + 1;

            var files = new List<string>();
            var buf = new byte[1000];

            Span<byte> span(int size) => new(buf.Length < size ?
                                                 buf = new byte[size] :
                                                 buf, 0, size);

            void proto_file(string path)
            {
                var dst_path = Path.Combine(tmp, path[cut..].Replace('\\', '_').Replace('/', '_'));
                var str = process_proto_files(Directory.EnumerateFiles(path, "*.proto"));
                if (str.Length == 0) return;

                files.Add(dst_path);


                var bytes = span(AdHoc.varint_bytes(str));
                AdHoc.varint(str.AsSpan(), bytes);
                using (var dst = new FileStream(dst_path, FileMode.Create, FileAccess.Write)) dst.Write(bytes);
            }

            proto_file(provided_path);

            foreach (var dir in Directory.EnumerateDirectories(provided_path, "*", SearchOption.AllDirectories).ToArray())
                proto_file(dir);

            if (files.Count == 0)
                exit($"No useful information found at the path: {provided_path}");

            await ChannelToServer.Start(new AdHocProtocol.Agent_.Proto
            {
                task = task,
                name = Path.GetFileName(provided_path),
                _proto = zip(files)
            });
            #endregion
        }

        static void set_provided_paths(string[] paths)
        {
            if (paths.Length == 0) return;

            var collect = new HashSet<string>();

            foreach (var path in paths)
                if (File.Exists(path))
                {
                    if (path.EndsWith(".csproj"))
                    {
                        var csproj = paths[1];

                        var dir = Path.GetDirectoryName(csproj)!;

                        foreach (var xml_path in XElement.Load(csproj)
                                                         .Descendants()
                                                         .Where(n => n.Name.ToString().Equals("Compile"))
                                                         .Select(n => Path.GetFullPath(n.Attribute("Include")!.Value, dir)))
                            collect.Add(xml_path);
                    }
                    else if (path.EndsWith(".cs")) collect.Add(path);
                }
                else if (!Directory.Exists(destination_dir_path = path))
                    Directory.CreateDirectory(destination_dir_path);

            collect.Remove(provided_path);

            provided_paths = collect.ToArray();
        }


        //folder for downloading files before their processing and deployment ( working(current) directory by default)
        public static string destination_dir_path = Directory.GetCurrentDirectory();

        public static string RawFilesDirPath => Path.Combine(destination_dir_path, Path.GetFileName(provided_path)[..^3]);


        public static int exit(string banner, int code = 1)
        {
            if (0 < banner.Length)
                if (code == 0)
                    LOG.Information(banner);
                else
                    LOG.Error(banner);

            LOG.Information("Press ENTER to exit");
            try { Console.In.ReadLine(); }
            catch (IOException ignored) { }

            Environment.Exit(code);
            return code;
        }

        public static string program_file_dir => Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase!).Path))!;
        public static string provided_path; //can be AdHoc ptotocol description or Protocol buffers convertor input
        public static string[] provided_paths = [];
        public static string task => HashFor(Path.GetDirectoryName(provided_path)!).ToString("X") + "_" + Path.GetFileName(provided_path);

        static ulong HashFor(string str)
        {
            var ret = 3074457345618258791ul;
            foreach (var ch in str)
            {
                ret += ch;
                ret *= 3074457345618258799ul;
            }

            return ret;
        }


        public class Deployment
        {
            static string deployment_instructions_txt;
            static string raw_files_dir_path;

            // A shared StringBuilder to reduce memory allocations during string construction.
            static StringBuilder sb = new();

            /// Represents a single file or directory in the deployment source tree.
            /// It holds information about its path, display properties, and deployment rules.
            /// </summary>
            class LineInfo
            {
                /// <summary>
                /// Initializes a new LineInfo object and adds it to the global dictionary.
                /// </summary>
                /// <param name="path">The full file system path to the file or directory.</param>
                /// <param name="icon">The icon to use when rendering this line in Markdown.</param>
                /// <param name="indent">The indentation string for Markdown rendering.</param>
                /// <param name="key">The relative path used as a unique key in the dictionary.</param>
                public LineInfo(string path, string icon, string indent, string key)
                {
                    this.path = path;
                    this.icon = icon;
                    this.indent = indent;
                    // 'asis' files are treated as binary/library files. They are copied directly without merging.
                    asis = key.Contains("/lib/") || key.Contains(@"\lib\") || key.Contains(@"\__\") || key.Contains(@"/__/");
                    receiver_files_lines.Add(key, this);
                }

                // --- Deployment Properties ---
                public bool skipped; // True if this file/folder should be ignored during deployment.
                public (Regex Selector, string[] Destinations)[]? targets; // Deployment rules: a selector regex and destination paths.

                // --- Reporting Properties ---
                public List<string>? report; // A list of messages detailing the outcome of deployment for this item.

                public void add_report(string report) => (this.report ??= new List<string>(2)).Add(report);

                // --- Display Properties ---
                public string indent;
                public bool asis; // If true, file is copied verbatim without smart merge.
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
                    if (path.Contains(' '))
                    {
                        sb.Append('<');
                        // Normalize Windows drive letter paths for better cross-platform link compatibility.
                        if (path[1] == ':') sb.Append('/');
                        sb.Append(path).Append(">) ");
                    }
                    else
                    {
                        if (path[1] == ':') sb.Append('/');
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

                    if (File.Exists(path))
                        if (skipped || report == null)
                            sb.Append(" ⛔ "); // Skipped or no action taken.
                        else
                        {
                            // Append multi-line reports with proper indentation for readability.
                            sb.Append(' ');
                            var chars = sb.Length;
                            sb.Append(report[0]);
                            for (var r = 1; r < report.Count; r++)
                            {
                                sb.Append('\n');
                                for (var i = 0; i < chars; i++) sb.Append(' ');
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
                var root_path_len = raw_files_dir_path.Length + (raw_files_dir_path.EndsWith('/') || raw_files_dir_path.EndsWith('\\') ?
                                                                     0 :
                                                                     1);

                // Helper to create a LineInfo object for a given path.
                void add(string icon, string path, int level)
                {
                    sb.Clear();
                    for (var i = 0; i < level; i++) sb.Append("  "); // Create indentation string.
                    // The key is the path relative to the root, with normalized forward slashes.
                    new LineInfo(path, icon, sb.ToString(), path[root_path_len..].Replace('\\', '/'));
                }

                // Recursive function to scan directories.
                void scan(string dir, int level)
                {
                    // The initial call with level -1 skips adding the root directory itself to the visual tree.
                    if (-1 < level) add("📁", dir, level);

                    level++;
                    foreach (var dir_ in Directory.GetDirectories(dir)) scan(dir_, level);
                    foreach (var file in Directory.GetFiles(dir))
                        add(Path.GetExtension(file) switch
                        {
                            ".cs" => "＃",
                            ".cpp" => "🧩",
                            ".h" => "🧾",
                            ".java" => "☕",
                            ".ts" => "🌀",
                            ".js" => "📜",
                            ".html" => "🌐",
                            ".css" => "🎨",
                            ".go" => "🐹",
                            ".rs" => "⚙️",
                            ".kt" => "🟪",
                            ".swift" => "🐦",
                            ".json" => "{}",
                            _ => "📄"
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
                if (!Directory.Exists(raw_files_dir_path) && !Directory.Exists(raw_files_dir_path = Path.Join(Directory.GetCurrentDirectory(), Path.GetFileName(raw_files_dir_path))))
                    exit($"Cannot find source folder {Path.GetFileName(raw_files_dir_path)} at {Path.GetDirectoryName(deployment_instructions_file)} and at working directory {Directory.GetCurrentDirectory()} redeploy process canceled");

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
            public static void deploy(string raw_files_dir_path)
            {
                Deployment.raw_files_dir_path = raw_files_dir_path;
                var deployment_instructions_file_name = Path.GetFileName(raw_files_dir_path) + ".md";

                // --- Search for the deployment instructions file in priority order ---
                // 1. Next to the raw files directory itself.
                var deployment_instructions_file_path = Path.Join(Path.GetDirectoryName(raw_files_dir_path)!, deployment_instructions_file_name);
                if (File.Exists(deployment_instructions_file_path)) goto deploy;

                // 2. In the current working directory (where the tool was executed from).
                deployment_instructions_file_path = Path.Join(Directory.GetCurrentDirectory(), deployment_instructions_file_name);
                if (File.Exists(deployment_instructions_file_path)) goto deploy;

                // --- If not found, generate a default deployment instructions file ---
                deployment_instructions_file_path = Path.Join(Path.GetDirectoryName(raw_files_dir_path)!, deployment_instructions_file_name);

                var deployment_instructions_file = File.OpenWrite(deployment_instructions_file_path);

                deployment_instructions_file.Write(UTF8_NO_BOM.GetBytes(@"**Autogenerated Deployment Instructions File**

This file is crucial for managing the deployment process. ✅⛔✔️ ✖️ ❌ ❎ 🟢 🔴 🟩 🟥 🟡 🔵 ⚠️ 🚫 🔺 🔻 ❓❗👀📅🕒

**Important:**
- **Do not rename this file.**
- If you need to move it, ensure it remains in the correct folder layout.
- Refer to the manual for further guidance if needed.


"));


                build_receiver_files_lines();
                sb.Clear();

                foreach (var info in receiver_files_lines
                                     .Where(e => !(e.Value.asis)) //exclude libs and finally generated files
                                     .OrderBy(e => e.Key)
                                     .Select(e => e.Value))
                    info.append_md_line().Append('\n');

                var tree = sb.ToString();
                deployment_instructions_file.Write(UTF8_NO_BOM.GetBytes(tree));

                deployment_instructions_file.Write(UTF8_NO_BOM.GetBytes(@$" 


**Rerun Deployment Process:**
   - Execute the following command in your terminal or command prompt:
     ```shell
     AdHocAgent ""{deployment_instructions_file_path}""
     ```


If a path starts with one of the following prefixes:
- `/InCPP/`
- `/InCS/`
- `/InGO/`
- `/InJAVA/`
- `/InRS/`
- `/InTS/`

it indicates that the path is relative to the root of the folder containing the received files.

Formatting:
  
```regexp
\.(java|cs|cpp|h|)$
```

```shell
clang-format -i -style=""{{ColumnLimit: 0,IndentWidth: 4, 
                        TabWidth: 4, 
                        UseTab: Never, 
                        BreakBeforeBraces: Allman, 
                        IndentCaseLabels: true, 
                        AllowShortBlocksOnASingleLine: false,
                        SpacesInLineCommentPrefix: {{Minimum: 0, Maximum: 0}}}}"" FILE_PATH 
```

```other option is

Formatting using [Artistic Style (AStyle)](https://sourceforge.net/projects/astyle/files/astyle/):

astyle  --style=allman
        --indent-switches
        --indent-cases
        --indent-namespaces
        --break-closing-braces
        --remove-braces
        --keep-one-line-blocks
        --attach-return-type
        --attach-return-type-decl
        --attach-return-type
        --delete-empty-lines
        --unpad-paren
        --pad-oper
        --pad-comma
        --suffix=none
        --quiet
  FILE_PATH 
```

To format C# files with `dotnet format` use the command in format `before and after deployment execution`
```shell
［before deployment](dotnet format ""/InCS/Host_in_C#"" )
```
Install `prettier` globally `npm install -g prettier` to ensure it is available in the console as `prettier`.
```regexp
\.ts$
```

```shell
prettier --write FILE_PATH --tab-width 4 --bracket-spacing false --print-width  999
```

```regexp
\.rs$
```

```shell
rustfmt FILE_PATH
```

```regexp
\.go$
```

```shell
gofmt -w FILE_PATH
```

Removing whitespace before region directives in all files.

```regexp
.*
```

```csharp
""System.Linq.Enumerable""
""System.Text.RegularExpressions""

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

public class Program
{{
    // Define the regex pattern
    static string pattern = @""^\s+(?=//#region|//#endregion|#region|#endregion|//region|//endregion|// region|// endregion)"";
    
    public static void Main(string[] args)
    {{
        // Read the file content with UTF-8 encoding
        var content = File.ReadAllText( args[0], System.Text.Encoding.UTF8);

        // Perform the replacement
        var updatedContent = Regex.Replace(content, pattern,"""", RegexOptions.Multiline);

        // Write the updated content back to the file with UTF-8 encoding
        //According to the Unicode standard, the BOM for UTF-8 files is not recommended !!!
        File.WriteAllText( args[0], updatedContent,  new System.Text.UTF8Encoding()); 
    }}
}}
```

**For reference: **
markdown file / folder link syntax examples:

`[A relative link](../../some/dir/filename.ext)  `  
`[Link to file in another dir on same drive](/another/dir/filename.ext)  `  
`[Link to file in another dir on a different drive](/D:/dir/filename.ext)  `  
`[If you have spaces in the filename](</C:/Program Files (x86)>)`  

"));

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
                var reconciled_keys = new HashSet<string>(); // Tracks keys of files that were found in both the .md and the filesystem.
                var Key_path_segment = new Regex(@"(?<=\/|\\)(CPP|InCS|InGO|InJAVA|InRS|InTS)[\\/]*.*");
                var markdown_regex = new Regex(@"- (?:📁|＃|🧩|🧾|☕|🌀|📜|🌐|🎨|🐹|⚙️|🟪|🐦|📄|\{\})(?:\s*\[([^\]]*)\]\(([^)]*)\)[^\[\n\r]*)*(?:\s*⛔)?", RegexOptions.Multiline);

                Match? first_match = null;
                Match? last_match = null;

                // STEP 2: Parse the existing .md file and reconcile with the ground truth.
                foreach (Match match in markdown_regex.Matches(deployment_instructions_txt))
                {
                    last_match = match;
                    first_match ??= match;
                    // the link
                    //      InCS/Agent/lib/collections/BitList.cs
                    //      InJAVA/Server/collections/org/unirail/collections/BitList.java
                    var key = Key_path_segment.Match(match.Groups[2].Captures[0].Value).Groups[0].Value.Replace('\\', '/').Replace(">", "").Trim();

                    if (receiver_files_lines.TryGetValue(key, out var info))
                    {
                        // MATCH FOUND: This file exists. Preserve its customization.
                        //- 📁[InCS](/AdHocTMP/AdHocProtocol/InCS) ✅ copy full structure [](/AdHoc/Protocol/Generated/InCS)
                        //           <--------- head ------------> <------------------- customization --------------------->
                        var head = match.Groups[2].Captures[0];
                        info.customization = match.ToString()[(head.Index + head.Length + 1 - match.Index)..];
                        reconciled_keys.Add(key);
                    }
                    else
                    {
                        // NO MATCH: This instruction is obsolete.
                        obsolete_md_lines.Add(match.ToString());
                    }
                }

                var new_files = receiver_files_lines.Keys.Where(k => !reconciled_keys.Contains(k)).ToList();

                // STEP 3: Check for discrepancies (new files or obsolete instructions).
                if (obsolete_md_lines.Count > 0 || new_files.Count > 0)
                {
                    LOG.Warning("Discrepancy detected between the deployment instructions file and the source directory.");

                    if (new_files.Count > 0)
                    {
                        LOG.Information("New files/directories detected:");
                        foreach (var key in new_files) Console.Out.WriteLine($"  + {key}");
                    }

                    if (obsolete_md_lines.Count > 0)
                    {
                        LOG.Information("Obsolete instructions found (files may have been removed or renamed):");
                        foreach (var line in obsolete_md_lines) Console.Out.WriteLine($"  - {line}");
                    }

                    LOG.Warning("The file list in '{md_file}' will be regenerated to reflect these changes.", Path.GetFileName(deployment_instructions_file));
                    LOG.Information("Your existing deployment rules (target paths, skip markers) for unchanged files will be preserved.");

                    // Ask for user confirmation.
                    Console.Write("Proceed with updating the instructions file? (y/N): ");
                    if (Console.ReadKey().Key != ConsoleKey.Y)
                    {
                        exit("\nOperation cancelled by user.", -1);
                        return; // return is needed for non-exit methods
                    }

                    Console.WriteLine();

                    // 1. Get the basic parts of the file path.
                    var dir = Path.GetDirectoryName(deployment_instructions_file)!;
                    var name = Path.GetFileNameWithoutExtension(deployment_instructions_file);
                    var ext = Path.GetExtension(deployment_instructions_file);

                    // 2. Find the highest existing backup number.
                    // The search pattern now looks for files like "deployment-instructions.*.json"
                    var backup_num = Directory.GetFiles(dir, $"{name}.*{ext}")
                                              .Select(f =>
                                              {
                                                  // For a file like "deployment-instructions.5.json",
                                                  // this gets the middle part: ".5"
                                                  var nameWithoutBase = Path.GetFileNameWithoutExtension(f).Substring(name.Length);

                                                  // Remove the leading dot to get "5"
                                                  var numStr = nameWithoutBase.TrimStart('.');

                                                  int.TryParse(numStr, out var num);
                                                  return num; // Returns the parsed number, or 0 if it fails
                                              })
                                              .DefaultIfEmpty(0) // If no backup files are found, start with 0
                                              .Max() + 1;        // Get the highest number and add 1

                    // 3. Create the new backup path without adding ".bak".
                    // This will create a path like "C:\MyApp\deployment-instructions.1.json"
                    var backup_path = Path.Combine(dir, $"{name}.{backup_num}{ext}");
                    File.Copy(deployment_instructions_file, backup_path);
                    LOG.Information("A backup of the old instructions file has been created at: {backup_path}", backup_path);

                    // REGENERATE the file list section.
                    if (first_match != null && last_match != null)
                    {
                        var sb_new_tree = new StringBuilder();
                        foreach (var info in receiver_files_lines.OrderBy(e => e.Key).Select(e => e.Value))
                        {
                            info.append_md_line().Append(info.customization).Append('\n');
                            sb_new_tree.Append(sb.ToString());
                            sb.Clear();
                        }

                        var header = deployment_instructions_txt[..first_match.Index];
                        var footer = deployment_instructions_txt[(last_match.Index + last_match.Length)..];

                        deployment_instructions_txt = header + sb_new_tree.ToString() + footer;
                        File.WriteAllText(deployment_instructions_file, deployment_instructions_txt, UTF8_NO_BOM);

                        LOG.Information("Instructions file has been successfully updated.");
                    }
                }

                // STEP 4: Parse the final, up-to-date instructions to prepare for deployment.
                var any = new Regex(".*");
                var has_some_targets = false;

                foreach (var info in receiver_files_lines.Values)
                {
                    // This regex finds the skip marker and all target definitions `[...](...)` within the customization string.
                    var parser_regex = new Regex(@"(\[([^\]]*)\]\(([^)]*)\))|(\s*⛔)");
                    var targets = new List<(string Selector, string Destination)>();

                    foreach (Match match in parser_regex.Matches(info.customization))
                    {
                        if (match.Groups[4].Success) // Matched the skip marker ⛔
                        {
                            info.skipped = true;
                        }
                        else if (match.Groups[1].Success) // Matched a target []()
                        {
                            var selector = match.Groups[2].Value;
                            var dest = match.Groups[3].Value;
                            dest = dest[0] == '/' && dest[2] == ':' ?
                                       dest[1..].Replace('/', '\\') :
                                       dest;

                            // To skip a file or folder from deployment, add `⛔` to the line or use an empty target `[]()`.
                            if (selector == "" && dest == "") { info.skipped = true; }
                            else { targets.Add((selector, dest)); }
                        }
                    }

                    if (targets.Count > 0)
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

                if (!has_some_targets)
                    exit($"No `target locations` detected. Please add deployment targets to `{deployment_instructions_file}`.");

                // Execute pre-deployment commands.
                foreach (var before_deployment in new Regex(@"\[before deployment\]\((.+)\)").Matches(deployment_instructions_txt).Select(m => m.Groups[1].Value))
                    Start_and_wait(before_deployment, raw_files_dir_path);

                var shell_tasks = new Regex(@"^([^\s].+?)(?:\r?\n(?=\s)|\r?\n?$)(?:\s+(.+?)(?:\r?\n(?=\s)|\r?\n?$))*", RegexOptions.Multiline);

                //======================== Execute custom code (shell or C#) on received files for formatting and other processing

                foreach (var (type, match) in new Regex(@"```regexp\s+(.+?)\s*```\s*```shell\s*([\s\S]*?)\s*```")
                                              .Matches(deployment_instructions_txt).Select(m => (0, m))
                                              .Concat(new Regex(@"```regexp\s+(.+?)\s*```\s*```csharp\s*([\s\S]*?)\s*```", RegexOptions.Multiline).Matches(deployment_instructions_txt).Select(m => (1, m)))
                                              .OrderBy(m => m.m.Index))
                {
                    var selector = string.IsNullOrEmpty(match.Groups[1].Value) ?
                                       null :
                                       new Regex(match.Groups[1].Value);
                    var target = match.Groups[2].Value;
                    int i;
                    switch (type)
                    {
                        case 0: //shell. formatting for example
                            foreach (Match m in shell_tasks.Matches(target))
                                foreach (var info in receiver_files_lines.Values.Where(info => selector == null || selector.IsMatch(info.path)))
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

                            if (0 < cut) target = target[cut..];
                            var compilation = CSharpCompilation.Create(Path.GetRandomFileName(), syntaxTrees: new[] { CSharpSyntaxTree.ParseText(target) }, references: refs, options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

                            var ms = new MemoryStream();
                            var result = compilation.Emit(ms);
                            if (!result.Success)
                            {
                                foreach (var diagnostic in result.Diagnostics)
                                    Console.Error.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                                exit("");
                            }

                            ms.Seek(0, SeekOrigin.Begin);
                            var compiledAssembly = Assembly.Load(ms.ToArray());
                            var args = new string[1];
                            var objs = new object[] { args };
                            var main = compiledAssembly.GetType("Program")!.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;

                            foreach (var info in receiver_files_lines.Values.Where(info => File.Exists(info.path) && (selector == null || selector.IsMatch(info.path))))
                                try
                                {
                                    args[0] = info.path;
                                    main.Invoke(null, objs);
                                }
                                catch (Exception e)
                                {
                                    LOG.Error("Error: " + e.Message);
                                    exit("");
                                }

                            continue;
                    }
                }

                // --- Main Deployment Logic: Copy/Merge files based on instructions ---

                var tempDir = Path.GetTempPath();
                Directory.CreateDirectory(tempDir);
                var tempFiles = new Dictionary<string, string>();
                var ancestors = new List<LineInfo>();

                foreach (var info in receiver_files_lines.Values)
                {
                    // Maintain the ancestor stack. Pop directories from the stack until the current item's parent is on top.
                    while (ancestors.Count > 0 && !Path.GetDirectoryName(info.path)!.Equals(ancestors[^1].path, StringComparison.OrdinalIgnoreCase))
                        ancestors.RemoveAt(ancestors.Count - 1);

                    // An item is skipped if marked explicitly, OR if it's inside a skipped folder.
                    var shouldSkip = info.skipped || ancestors.Any(a => a.skipped);

                    // If it's a directory, push it to the stack for its children's reference.
                    if (Directory.Exists(info.path))
                    {
                        ancestors.Add(info);
                        continue;
                    }

                    if (shouldSkip)
                        continue;

                    // If we reach here, the item is a file that needs to be processed.
                    var received_src_file = info.path;

                    // Apply deployment rules inherited from parent folders.
                    foreach (var parent_folder in ancestors.Where(i => i.targets is { Length: > 0 }))
                        foreach (var target_path in parent_folder.targets!.Where(t => t.Selector.IsMatch(received_src_file)).SelectMany(t => t.Destinations))
                        {
                            var target_base = target_path.EndsWith('/') || target_path.EndsWith('\\') ?
                                                  Path.Combine(target_path, Path.GetFileName(parent_folder.path)) :
                                                  target_path;
                            var relativePath = Path.GetRelativePath(parent_folder.path, received_src_file);
                            var existingDstFilePath = Path.Combine(target_base, relativePath);

                            var tempPath = Path.Combine(tempDir, Guid.NewGuid() + Path.GetExtension(existingDstFilePath));

                            if (info.asis)
                            {
                                File.Copy(received_src_file, tempPath);
                                info.add_report("👉 " + existingDstFilePath);
                            }
                            else
                            {
                                var (report, content) = MergeCustomCodeIntoNewlyGeneratedFile(received_src_file, existingDstFilePath);
                                File.WriteAllText(tempPath, content, UTF8_NO_BOM);
                                info.add_report(report);
                            }

                            tempFiles[existingDstFilePath] = tempPath;
                        }

                    // Apply deployment rules from the file itself.
                    if (info.targets == null) continue;

                    foreach (var target_path in info.targets.SelectMany(t => t.Destinations))
                    {
                        var existingDstFilePath = target_path.EndsWith('/') || target_path.EndsWith('\\') ?
                                                      Path.Combine(target_path, Path.GetFileName(received_src_file)) :
                                                      target_path;

                        var tempPath = Path.Combine(tempDir, Guid.NewGuid() + Path.GetExtension(existingDstFilePath));

                        if (info.asis)
                        {
                            File.Copy(received_src_file, tempPath, true);
                            info.add_report("👉 " + existingDstFilePath);
                        }
                        else
                        {
                            var (report, content) = MergeCustomCodeIntoNewlyGeneratedFile(received_src_file, existingDstFilePath);
                            File.WriteAllText(tempPath, content, UTF8_NO_BOM);
                            info.add_report(report);
                        }

                        tempFiles[existingDstFilePath] = tempPath;
                    }
                }

                // --- Finalization: Reporting, Backup, and Cleanup ---

                foreach (var info in receiver_files_lines.OrderBy(e => e.Key).Select(e => e.Value))
                {
                    sb.Clear();
                    info.append_report_line();
                    Console.Out.Write(sb.ToString());
                }

                // Execute post-deployment commands.
                foreach (var after_deployment in new Regex(@"\[after deployment\]\((.+)\)").Matches(deployment_instructions_txt).Select(m => m.Groups[1].Value))
                    Start_and_wait(after_deployment, raw_files_dir_path);

                // Create a backup of all overwritten files.
                var backup_dir = Path.GetDirectoryName(deployment_instructions_file);
                var backup_name = Path.GetFileName(raw_files_dir_path);
                var backupDir = Path.Combine(backup_dir, backup_name + "_" + (Directory.GetDirectories(backup_dir, $"{backup_name}_*").Select(dirPath =>
                                                                                                                                {
                                                                                                                                    int.TryParse(Path.GetFileName(dirPath).Substring(backup_name.Length + 1), out var number);
                                                                                                                                    return number;
                                                                                                                                }).DefaultIfEmpty(0).Max() + 1));
                Directory.CreateDirectory(backupDir);
                var restorePlan = new Dictionary<string, string>();

                foreach (var dest in tempFiles.Keys.Where(File.Exists))
                {
                    var pathRoot = Path.GetPathRoot(dest);
                    var relativeDestPath = string.IsNullOrEmpty(pathRoot) ?
                                               dest :
                                               dest[pathRoot.Length..];
                    var initialBackupPath = Path.Combine(backupDir, relativeDestPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(initialBackupPath)!);
                    var uniqueBackupPath = GetUniqueBackupPath(initialBackupPath);
                    File.Copy(dest, uniqueBackupPath);
                    restorePlan.Add(dest, uniqueBackupPath);
                }

                GenerateRestoreScripts(restorePlan, backupDir);

                // --- START: INSERT NEW CLEANUP LOGIC HERE ---

                // After backing up, we perform a smart cleanup of the destination directories.
                // This removes stale files that are no longer part of the deployment source.
                LOG.Information("Cleaning up stale files from destination locations...");

                // Create a fast lookup set of all files that WILL be deployed. Case-insensitive for Windows.
                var filesToDeploy = new HashSet<string>(tempFiles.Keys, StringComparer.OrdinalIgnoreCase);

                // Get all unique destination directories that are part of this deployment.
                var destinationDirs = new HashSet<string>(
                    tempFiles.Keys.Select(path => Path.GetDirectoryName(path)!),
                    StringComparer.OrdinalIgnoreCase
                );

                foreach (var dir in destinationDirs.Where(d => Directory.Exists(d)))
                    // Recursively get all files in the target directory and its subdirectories.
                    foreach (var existingFile in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                        // If an existing file is NOT in our set of files to deploy, it's stale.
                        if (!filesToDeploy.Contains(existingFile))
                            try
                            {
                                File.Delete(existingFile);
                            }
                            catch (Exception ex)
                            {
                                LOG.Warning("Could not delete stale file {staleFile}: {errorMessage}", existingFile, ex.Message);
                            }


                // Now, clean up any directories that may have become empty.
                // We must process from the deepest directories upwards.
                var allSubDirs = destinationDirs
                    .Where(d => Directory.Exists(d))
                    .SelectMany(d => Directory.GetDirectories(d, "*", SearchOption.AllDirectories))
                    .ToList();
                allSubDirs.AddRange(destinationDirs); // Also check the root target dirs themselves

                var emptyDirsDeleted = 0;
                foreach (var subDir in allSubDirs.Distinct().OrderByDescending(p => p.Length))
                    if (Directory.Exists(subDir) && !Directory.EnumerateFileSystemEntries(subDir).Any())
                        try
                        {
                            Directory.Delete(subDir);
                            emptyDirsDeleted++;
                        }
                        catch (Exception ex)
                        {
                            LOG.Warning("Could not delete empty directory {emptyDir}: {errorMessage}", subDir, ex.Message);
                        }


                // --- END: NEW CLEANUP LOGIC ---

                // Overwrite target files with the processed temp files.
                foreach (var (dest, tempPath) in tempFiles)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(tempPath, dest, true);
                }

                try
                {
                    Directory.Delete(tempDir, true); //be nice.remove garbage
                }
                catch (Exception)
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
            //in .NET does not work !!!
            //Console.WriteLine(new Regex(@"\p{So}", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.NonBacktracking).Matches("").Count);
            // output 0!!!!
            //https://github.com/dotnet/runtime/issues/36425
            static readonly Regex GEN_BLOCK = new(@"\s*//(?<marker>\p{So})<\s*(?<content>.*?)\s*//\k<marker>/>", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.Singleline);
            static readonly Regex JAVA = new(@"//#region\s*>\s*(?:.*?)\r?\n(?<content>[\s\S]*?)//#endregion\s*>\s*(?<uid>.*?)\s*?$", RegexOptions.Multiline | RegexOptions.Compiled);
            static readonly Regex CS = new(@"#region\s*>\s*(?:.*?)\r?\n(?<content>[\s\S]*?)#endregion\s*>\s*(?<uid>.*?)\s*?$", RegexOptions.Multiline | RegexOptions.Compiled);


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
                if (!File.Exists(existingDstFilePath))
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

                foreach (Match regionMatch in regionRegex.Matches(existingFileContent))
                {
                    var customCode = regionMatch.Groups["content"].Value;
                    var uid = regionMatch.Groups["uid"].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(uid) && !string.IsNullOrWhiteSpace(customCode.Trim()))
                        existingRegionsByUid[uid] = customCode;
                }

                // =================================================================================
                // STEP 2: REBUILD - Build the new file content using the NEWLY GENERATED file as the template.
                // We walk through the new file, and for each region, we inject the user's saved
                // customizations if a matching UID exists.
                // =================================================================================
                var mergedResult = new StringBuilder();
                var newFileContent = File.ReadAllText(newlyGeneratedFilePath);
                var lastIndex = 0;

                foreach (var newRegionMatch in regionRegex.Matches(newFileContent).Cast<Match>())
                {
                    // Append the generated scaffolding that comes *before* this region.
                    mergedResult.Append(newFileContent, lastIndex, newRegionMatch.Index - lastIndex);

                    var uid = newRegionMatch.Groups["uid"].Value.Trim();
                    var newRegionContent = newRegionMatch.Groups["content"].Value;
                    var fullNewRegionBlock = newRegionMatch.Value;

                    // Do we have saved custom code for this UID from the old file?
                    if (existingRegionsByUid.TryGetValue(uid, out var existingRegionContent))
                    {
                        // YES: This region existed before. Perform a smart merge of its content.
                        var mergedRegionContent = MergeRegionContent(existingRegionContent, newRegionContent);

                        // Reconstruct the full region block with the newly merged content.
                        // We do this by replacing the original content of the new region with our merged version.
                        if (newRegionContent.Length == 0 && !fullNewRegionBlock.Contains("\n\n")) // Handle empty new regions carefully
                            mergedResult.Append(fullNewRegionBlock.Insert(fullNewRegionBlock.LastIndexOf('\n') + 1, mergedRegionContent));
                        else
                            mergedResult.Append(fullNewRegionBlock.Replace(newRegionContent, mergedRegionContent));

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
                if (existingRegionsByUid.Count == 0)
                    return ("✅  " + existingDstFilePath, mergedResult.ToString());

                // An orphan is a region the user had, but which the generator no longer creates.
                // This is a critical safety net.
                LOG.Error("Orphaned custom code detected in {ExistingFilePath}. These regions existed in your file but were removed in the new version. Your changes will be lost if you proceed.", existingDstFilePath);
                foreach (var (id, code) in existingRegionsByUid)
                {
                    Console.WriteLine($"--- ORPHANED REGION (UID: {id}) ---");
                    Console.WriteLine(code);
                    Console.WriteLine("------------------------------------");
                }

                Console.WriteLine(); // Add spacing for readability
                LOG.Warning("If you continue, the orphaned code above will be removed from the active file: '{0}'", Path.GetFileName(existingDstFilePath));
                LOG.Information("IMPORTANT: A full backup of the original file will be created before any changes are made. Your code will NOT be permanently lost and can be recovered from the backup.");

                // Ask for explicit user confirmation with the new, less alarming context.
                LOG.Error("Proceed with the merge? (y/N): ");
                if (Console.ReadKey().Key != ConsoleKey.Y)
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
                    if (existingContent.Length == 0) return newContent;

                    // Helper function to comment out every line of a text block.
                    string CommentOutBlock(string blockText) => Regex.Replace(blockText, "^", "//", RegexOptions.Multiline);

                    // Helper function to uncomment a block, removing the leading "//" from each line.
                    string UncommentBlock(string blockText) => Regex.Replace(blockText, @"^// ?", "", RegexOptions.Multiline);

                    // Find all OLD generated blocks from the existing file.
                    var existingBlocks = ParseGenBlocks(existingContent);
                    if (existingBlocks.Count == 0) return newContent + existingContent;

                    var result = new StringBuilder();
                    var i = 0;


                    // Index all NEW generated blocks by their unique marker for quick lookup.
                    var newBlocks = ParseGenBlocks(newContent);
                    if (newBlocks.Count == 0) //remove all blocks from existing content
                    {
                        foreach (var block in existingBlocks.Values)
                        {
                            result.Append(existingContent, i, block.BlockStart - i);
                            i = block.BlockEnd;
                        }

                        return result.ToString();
                    }


                    // --- Main Merge Loop: Iterate through the existing content's structure ---
                    // This loop respects the user's ordering of blocks and any custom code written between them.
                    foreach (var existingBlock in existingBlocks.OrderBy(e => e.Value.BlockStart))
                    {
                        // Append the user's custom code located *between* the previous block and this one.
                        result.Append(existingContent, i, existingBlock.Value.BlockStart - i);

                        var marker = existingBlock.Key;

                        // A block is considered "enabled" if its first line does not start with "//" (ignoring whitespace).
                        // This is the key to preserving the user's choice.
                        var isExistingBlockEnabled = !existingBlock.Value.Content.TrimStart().StartsWith("//");

                        if (newBlocks.TryGetValue(marker, out var newBlock))
                        {
                            // RULE: A corresponding block exists in the new file.
                            result.Append(newContent, newBlock.BlockStart, newBlock.ContentStart - newBlock.BlockStart);
                            result.Append(
                                          // We will use the NEW content, but respect the user's ENABLED/DISABLED state.
                                          isExistingBlockEnabled ?
                                              UncommentBlock(newBlock.Content) : // User wants it enabled.
                                              CommentOutBlock(newBlock.Content)  // User wants it disabled.
                                         );
                            result.Append(newContent, newBlock.ContentEnd, newBlock.BlockEnd - newBlock.ContentEnd);

                            // Mark the new block as processed so it isn't added again at the end.
                            newBlocks.Remove(marker);
                        }
                        else if (isExistingBlockEnabled)
                        {
                            // RULE: A block the user HAD ENABLED was removed by the generator.
                            // Preserve it as commented-out code with a "todo" warning to prevent data loss.
                            result.AppendLine();
                            result.AppendLine("//todo 🔴 The following code block was removed by the code generator. Please review.");
                            result.Append(existingContent, existingBlock.Value.BlockStart, existingBlock.Value.ContentStart - existingBlock.Value.BlockStart);
                            result.Append(CommentOutBlock(existingBlock.Value.Content));
                            result.Append(existingContent, existingBlock.Value.ContentEnd, existingBlock.Value.BlockEnd - existingBlock.Value.ContentEnd);
                            result.AppendLine();
                        }
                        // If the block was removed by the generator AND the user already had it disabled, we simply let it disappear.

                        i = existingBlock.Value.BlockEnd;
                    }


                    // --- Final Step: Add any completely new blocks ---
                    // These are blocks that exist in the new file but not in the old one.
                    if (newBlocks.Count != 0)
                    {
                        // Check if at least one of the new, unprocessed blocks is ACTIVE by default.
                        var newActivated = newBlocks.Values.Any(m => !m.Content.TrimStart().StartsWith("//"));

                        // RULE: If a new ACTIVE block is added, warn the user as it might change behavior.
                        if (newActivated)
                        {
                            result.AppendLine();
                            result.AppendLine("//todo 🔴 New active generated code was added by the generator. Please review as it may affect your custom logic.");
                        }

                        // Append all new blocks at the end of the region, ordered as they appear in the new file.
                        foreach (var newBlock in newBlocks.OrderBy(e => e.Value.BlockStart)) result.Append(newContent, newBlock.Value.BlockStart, newBlock.Value.BlockEnd - newBlock.Value.BlockStart);
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
                    var currentIndex = 0;

                    while (currentIndex < content.Length)
                    {
                        var startOffset = content.Slice(currentIndex).IndexOf("//");
                        if (startOffset == -1) break;

                        var blockStart = currentIndex + startOffset;
                        var cursor = blockStart + 2;
                        if (cursor >= content.Length) break;

                        var (marker, markerLength) = GetMarker(content.Slice(cursor));
                        if (marker == null)
                        {
                            currentIndex = blockStart + 2;
                            continue;
                        }

                        cursor += markerLength;
                        if (cursor >= content.Length || content[cursor] != '<')
                        {
                            currentIndex = blockStart + 2;
                            continue;
                        }

                        cursor++; // Skip '<'

                        // --- START OF THE FIX ---
                        // Find the start of the content, skipping only HORIZONTAL whitespace (spaces, tabs).
                        // This preserves newlines.
                        var contentStart = cursor;
                        while (contentStart < content.Length && (content[contentStart] == ' ' || content[contentStart] == '\t')) { contentStart++; }
                        // --- END OF THE FIX ---

                        var endTag = $"//{marker}/>";
                        var endTagOffset = content.Slice(contentStart).IndexOf(endTag);
                        if (endTagOffset == -1)
                        {
                            currentIndex = blockStart + 2;
                            continue;
                        }

                        var endTagStart = contentStart + endTagOffset;
                        var blockEnd = endTagStart + endTag.Length;

                        // --- START OF THE FIX ---
                        // Find the end of the content, trimming only HORIZONTAL whitespace from the end.
                        var contentEnd = endTagStart;
                        while (contentEnd > contentStart && (content[contentEnd - 1] == ' ' || content[contentEnd - 1] == '\t')) { contentEnd--; }
                        // --- END OF THE FIX ---

                        var blockContent = content[contentStart..contentEnd].ToString();

                        result[marker] = new GenBlock(
                                                      Content: blockContent,
                                                      BlockStart: blockStart,
                                                      BlockEnd: blockEnd,
                                                      ContentStart: contentStart,
                                                      ContentEnd: contentEnd
                                                     );

                        currentIndex = blockEnd;
                    }

                    return result;
                }


                static (string? Marker, int Length) GetMarker(ReadOnlySpan<char> span)
                {
                    if (span.IsEmpty) return (null, 0);

                    var category = char.GetUnicodeCategory(span[0]);
                    if (category != UnicodeCategory.OtherSymbol && category != UnicodeCategory.Surrogate)
                        return (null, 0);

                    var length = char.IsSurrogatePair(span[0], span.Length > 1 ?
                                                                   span[1] :
                                                                   '\0') ?
                                     2 :
                                     1;
                    return (span.Slice(0, length).ToString(), length);
                }
            }


            public readonly record struct GenBlock(
                string Content,
                int BlockStart,
                int BlockEnd,
                int ContentStart,
                int ContentEnd
            );

            // Replace the ENTIRE GenerateRestoreScripts method with this new version
            /// <summary>
            /// Generates restore scripts (Batch, PowerShell, Shell) in the backup directory.
            /// </summary>
            static void GenerateRestoreScripts(Dictionary<string, string> restorePlan, string backupDir)
            {
                var restoreBat = new StringBuilder("@echo off\r\n");
                var restorePs1 = new StringBuilder("# PowerShell Restore Script\r\n");
                var restoreSh = new StringBuilder("#!/bin/sh\n# Run 'chmod +x restore.sh' to make this script executable.\n");

                foreach (var (originalPath, backupPath) in restorePlan)
                {
                    var relativeBackupPath = Path.GetRelativePath(backupDir, backupPath);

                    var destDir = Path.GetDirectoryName(originalPath)!;

                    // --- Batch Script (.bat) ---
                    // Use the relative path for the source file.
                    restoreBat.AppendLine($"if not exist \"{destDir}\" mkdir \"{destDir}\"");
                    restoreBat.AppendLine($"copy /Y \"{relativeBackupPath}\" \"{originalPath}\"");

                    // --- PowerShell Script (.ps1) ---
                    // Use the relative path for the source path.
                    restorePs1.AppendLine($"Copy-Item -Path \"{relativeBackupPath}\" -Destination \"{originalPath}\" -Recurse -Force");

                    // --- Shell Script (.sh) ---
                    // Use the relative path for the source (ensure forward slashes).

                    restoreSh.AppendLine($"mkdir -p \"{destDir.Replace('\\', '/')}\"");
                    restoreSh.AppendLine($"cp -rf \"{relativeBackupPath.Replace('\\', '/')}\" \"{originalPath.Replace('\\', '/')}\"");
                }

                File.WriteAllText(Path.Combine(backupDir, "restore.bat"), restoreBat.ToString(), UTF8_NO_BOM);
                File.WriteAllText(Path.Combine(backupDir, "restore.ps1"), restorePs1.ToString(), UTF8_NO_BOM);
                File.WriteAllText(Path.Combine(backupDir, "restore.sh"), restoreSh.ToString(), UTF8_NO_BOM);
            }

            static string GetUniqueBackupPath(string fullPath)
            {
                if (!File.Exists(fullPath))
                    return fullPath;

                var directory = Path.GetDirectoryName(fullPath)!;
                var filename = Path.GetFileNameWithoutExtension(fullPath);
                var extension = Path.GetExtension(fullPath);
                var counter = 1;
                string newFullPath;

                do { newFullPath = Path.Combine(directory, $"{filename} ({counter++}){extension}"); }
                while (File.Exists(newFullPath));

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
                                                    if (File.Exists(s) || Directory.Exists(s) || !root_path.IsMatch(s)) return s;

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
                FileName = exe,
                Arguments = args,
                WorkingDirectory = WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = true,
                CreateNoWindow = true
            };
            var error = "";
            // Attempt to execute process with different configurations to handle path/extension issues
            for (var i = 0;
                 i < 3;
                 i++, startInfo.FileName = exe + (OperatingSystem.IsWindows() ?
                                                      i == 1 ?
                                                          ".exe" : // Try .exe extension on Windows
                                                          ".cmd" : // Try .cmd on Windows
                                                      i == 1 ?
                                                          ".sh" :                                      // Try .sh on non-Windows
                                                          ".bash"), startInfo.UseShellExecute = true) // Try different extensions and shell execution
            {
                try
                {
                    // First attempt: Execute the process
                    using (var process = Process.Start(startInfo))
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
                catch (Exception e)
                {
                    // Second attempt: Modify start info and retry
                    startInfo.UseShellExecute = false; // Disable shell execution for second attempt
                    try
                    {
                        // Second attempt: Execute the process with modified start info
                        using (var process = Process.Start(startInfo))
                        {
                            error += process.StandardError.ReadToEnd();
                            var output = error == "" ? //!!!
                                             process.StandardOutput.ReadToEnd() :
                                             "";
                            process.WaitForExit();
                            if (0 < error.Length)
                            {
                                LOG.Error("An error {error} occurred while executing {command_line}. Would you like to continue? Enter 'N' to stop.", error, (exe + " " + startInfo.Arguments).Replace('\\', '/'));

                                if (Console.Read() is 'N' or 'n')
                                {
                                    exit("Bye", -1);
                                    return "";
                                }
                            }

                            return output; // Return standard output on success
                        }
                    }
                    catch (Exception ee)
                    {
                        // If both attempts fail after the first iteration, exit with error
                        if (1 < i) { exit($"Error executing {exe} {args}. Exception details:\n{ee}", -1); }
                        // If first iteration fails, loop will continue to next attempt
                    }
                }
            }

            // Note: Code should not reach here in normal execution, indicates both attempts on first iteration failed.
            return exe + " " + args; // Return command line for debugging if something unexpected happens
        }

        /// <summary>
        /// Updates the PersonalVolatileUUID in the application properties file.
        /// </summary>
        /// <param name="uuidString">The UUID string to set.</param>
        public static void updatePersonalVolatileUUID(string UUID)
        {
            // Ensure the input is a valid GUID before saving.
            if (!Guid.TryParse(UUID, out _)) return; // Or throw an exception if invalid input is a critical error.

            if (app_props.HasKey("PersonalVolatileUUID")) // If UUID exists, update it and back up the old one.
                app_props["PersonalVolatileUUID"].AsString.Value = UUID;
            else // Otherwise, add the new UUID.
                app_props.Add("PersonalVolatileUUID", new TomlString { Value = UUID });

            using var writer = File.CreateText(app_props_file);
            app_props.WriteTo(writer);
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

            if (!app_props.HasKey("PersonalVolatileUUID"))
                exit("Error: 'PersonalVolatileUUID' not found in configuration. Please provide one first.");

            var uuidString = app_props["PersonalVolatileUUID"].AsString!.Value;

            // Use Guid.TryParse to safely handle any valid GUID format (with or without hyphens, etc.).
            if (!Guid.TryParse(uuidString, out var guid))
                return; // Failed to parse, uuid_hi and uuid_lo remain 0.

            // Convert the parsed GUID to a clean 32-digit hex string ("N" format).
            var cleanHexString = guid.ToString("N");

            // Split the string into high and low parts. Substring is safe here due to the fixed length.
            var hiString = cleanHexString.Substring(0, 16);
            var loString = cleanHexString.Substring(16);

            // Use the safer TryParse to convert hex strings to ulongs.
            ulong.TryParse(hiString, System.Globalization.NumberStyles.HexNumber, null, out uuid_hi);
            ulong.TryParse(loString, System.Globalization.NumberStyles.HexNumber, null, out uuid_lo);
        }
    }
}