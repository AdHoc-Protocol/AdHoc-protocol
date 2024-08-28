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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using Serilog.Core;
using Tommy;
using org.unirail.Agent;
using Serilog.Events;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

//https://github.com/dezhidki/Tommy

namespace org.unirail{
    // https: //docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/semantic-analysis
    class AdHocAgent{
        class CallerEnricher : ILogEventEnricher{
            StringBuilder sb = new StringBuilder();

            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                var stackTrace = new StackTrace(true); // true to capture file information
                var stack      = stackTrace.GetFrame(6);

                logEvent.AddPropertyIfAbsent(new LogEventProperty("FileLine", new ScalarValue(sb.Clear()
                                                                                                .Append(stack.GetFileName())
                                                                                                .Append(":line ")
                                                                                                .Append(stack.GetFileLineNumber())
                                                                                                .ToString())));
            }
        }

        public static readonly Logger LOG = new LoggerConfiguration() // >> https://github.com/serilog/serilog/wiki/Getting-Started <<
                                            .Enrich.With<CallerEnricher>()
                                            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message} in {FileLine}\n")
                                            .CreateLogger();


        public static async void update_app_props_file()
        {
            await using var writer = File.CreateText(app_props_file);
            app_props.WriteTo(writer);
            await writer.FlushAsync();
        }

        public static string app_props_file
        {
            get
            {
                var file = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "AdHocAgent.toml");
                if( File.Exists(file) ) return file;
                var toml = File.OpenWrite(file);
                Assembly.GetExecutingAssembly().GetManifestResourceStream("AdHocAgent.Templates.AdHocAgent.toml")!.CopyToAsync(toml);
                LOG.Warning("The application configuration file {file} has been extracted from the template. Please note that its content may be outdated.", file);
                toml.Flush();
                toml.Close();
                return file;
            }
        }

        public static TomlTable app_props = TOML.Parse(File.OpenText(app_props_file)); //load application 'toml file'   https://www.ryansouthgate.com/2016/03/23/iconfiguration-in-netcore/


        static string zip_exe
        {
            get
            {
                var zip_exe = app_props["7zip_exe"].AsString.Value;
                if( !File.Exists(zip_exe) )
                    exit($"The value of 7zip_exe in {Path.Join(app_props_file, "AdHocAgent.toml")} point to none exiting file {zip_exe}. Please install 7zip (https://www.7-zip.org/download.html) and set path to the 7zip binary.");
                return zip_exe;
            }
        }

        public static void unzip(Stream src, string dst_folder)
        {
            var tmp_src = File.OpenWrite(new_random_tmp_path);
            src.CopyTo(tmp_src);
            tmp_src.Flush();
            tmp_src.Close();
            unzip(tmp_src.Name, dst_folder);
            File.Delete(tmp_src.Name);
        }

        public static void unzip(string src_file, string dst_folder) => Process.Start(new ProcessStartInfo
                                                                                      {
                                                                                          FileName               = zip_exe,
                                                                                          RedirectStandardOutput = true,
                                                                                          Arguments              = $" x \"{src_file}\" -aoa -o\"{dst_folder}\"",
                                                                                          WindowStyle            = ProcessWindowStyle.Hidden
                                                                                      })!.WaitForExit();

        private static string new_random_tmp_path => Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());


        public static byte[] zip(byte[] src, string name)
        {
            name = Path.Join(Path.GetTempPath(), name);
            File.WriteAllBytes(name, src);

            var ret = zip(new[] { name });
            File.Delete(name);
            return ret;
        }


        public static byte[] zip(IEnumerable<string> files_paths)
        {
            var tmp_zip = new_random_tmp_path;

            Process.Start(new ProcessStartInfo
                          {
                              FileName               = zip_exe, //Use the PPMd compression, the compression level to the maximum
                              Arguments              = $" a -t7z -m0=PPMd -mx=9 -mmem=256m  \"{tmp_zip}.\" {string.Join(' ', files_paths.Select(path => "\"" + path + "\""))}",
                              RedirectStandardOutput = true,
                              CreateNoWindow         = true
                          })!.WaitForExit();

            using var ret = new MemoryStream();
            using( var file = File.OpenRead(tmp_zip) )
                file.CopyTo(ret);


            File.Delete(tmp_zip);
            return ret.ToArray();
        }

        public static bool is_testing;


        /**
         first - required full path to the description_file.cs
                 or
                 file.proto file
                 or
                 folder.proto directory to translate into Adhoc format

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
                Console.Write("AdHocAgent utility");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" accepts the following command-line input:");

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("  The first argument is the path to the task file:");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\tIf the provided path ends with:");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t\t.cs   ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("- Uploads the protocol description file to the server to generate source code.");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t\t.cs!  ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("- Uploads the protocol description file to generate and test source code.");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t\t.cs?  ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("- Displays information about the protocol description file in the viewer.");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t\t.md  ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("- Repeats the deployment process according to the instructions in the .md file, using source files already in the working directory.");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t\t.proto");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("- Indicates that the file is in Protocol Buffers format and will be sent to the server for conversion to Adhoc protocol description format.");

                Console.Write("\tThe remaining arguments are paths to source ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(".cs ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("and project ");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(".csproj ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("files that are imported and used with the root protocol description file.");

                Console.WriteLine("  If the last argument is a path to a folder, it is used as the output folder for intermediate results. If not provided, the current working directory is used.");

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("In addition to command-line arguments, the ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("AdHocAgent utility");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" requires the following:");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  AdHocAgent.toml ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("- A file that contains:");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\t\tThe server URL.");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("\tAnd paths to local resources such as:");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\t\tIDE");
                Console.WriteLine("\t\t7-Zip compression utility (used for optimal compression).");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("\tThe AdHocAgent utility");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(" searches for the ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("AdHocAgent.toml");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(" file in its own directory.");
                Console.WriteLine("\tIf the file does not exist, the utility generates a template for this file. You only need to update the information in this file to match your configuration.");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  Deployment_instructions.md");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(" - Required only for code generation tasks. This file contains deployment instructions for the generated results.");


                Console.ForegroundColor = ConsoleColor.White;

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
                provided_path = paths[0][..^1]; //cut '?'
                paths_parser(paths);
                await ChannelToObserver.Start();
                return;
            }


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


            _ = zip_exe; //ensure check
#region .cs files - protocol description file processing
            if( provided_path.EndsWith(".cs") )
            {
                paths_parser(paths);
                ChannelToServer.Start(ProjectImpl.init());
                return;
            }
#endregion
#region .proto files processing
            if( 1 < paths.Length && !paths[^1].EndsWith(".proto") ) //if destination_dir_path explicitly provided instead of Directory.GetCurrentDirectory()
                if( !Directory.Exists(destination_dir_path = paths[^1]) )
                    Directory.CreateDirectory(destination_dir_path);


            var all_files = new Dictionary<string, List<string>>();

            foreach( var file in (File.Exists(provided_path) ?
                                      new[] { provided_path } :
                                      Directory.EnumerateFiles(provided_path, "*.proto", SearchOption.AllDirectories))
                    .Concat(
                            1 < paths.Length && paths[1].EndsWith(".proto") ?
                                Directory.EnumerateFiles(paths[1], "*.proto", SearchOption.AllDirectories) :
                                new string[] { }
                           ) )
            {
                var key = Path.GetFileName(file);
                if( all_files.TryGetValue(key, out var val) )
                    val.Add(file);
                else all_files[key] = new List<string>(new[] { file });
            }

            var   syntax   = new Regex(@"^\s*syntax\s*=\s*.*;", RegexOptions.Multiline);
            var   imported = new HashSet<string>();
            Regex imports  = new(@"^\s*import\s+(?:public\s+)?""([^""]+)""\s*;", RegexOptions.Multiline); //  import "myproject/other_protos.proto"; /  import public "new.proto";
            Regex package  = new(@"^\s*package\s+[""']?([^""';\s]+)[""']?\s*;", RegexOptions.Multiline);  // package foo.bar;
            Regex dot      = new(@"(?<=\s)\.(?=[^\.\s])", RegexOptions.Multiline);                        // package foo.bar;
            Regex asterisk = new(@"(?<=^\s*//.*)\*(?=\*/|\/*)", RegexOptions.Multiline);


            var package_proto = new Dictionary<string, string>();

            string process_proto_files(IEnumerable<string> files)
            {
                void process_proto_file(string proto_file_path)
                {
                    if( !imported.Add(proto_file_path) ) return;
                    var proto = File.ReadAllText(proto_file_path);

                    var pack = package.Matches(proto).Select(m => m.Groups[1].Value).FirstOrDefault() ?? "";

                    proto = $"\n//@@#region {HasDocs.brush(Path.GetFileName(proto_file_path))}\n" +
                            proto                                                                 +
                            $"\n//@@#endregion {HasDocs.brush(Path.GetFileName(proto_file_path))}\n";

                    proto = package.Replace(syntax.Replace(proto, ""), "");

                    if( package_proto.ContainsKey(pack) ) package_proto[pack] = package_proto[pack] + proto;
                    else package_proto.Add(pack, proto);


                    foreach( var match in imports.Matches(proto).OrderBy(Match => -Match.Index) ) //bottom ==> up
                    {
                        var import = match.Groups[1].Value;

                        var import_file = "";
                        if( all_files.TryGetValue(Path.GetFileName(import), out var paths) )
                            if( paths.Count == 1 )
                                import_file = paths[0];
                            else
                            {
                                while( !paths.Any(p => p.Replace('\\', '/').EndsWith(import)) )
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
                foreach( var file in files ) process_proto_file(file);
                var result_proto = "";

                repeat:
                while( 0 < package_proto.Count )
                    foreach( var (key, value) in package_proto.OrderBy(p => -p.Key.Count(ch => ch == '.')) )
                    {
                        var path = key.Split('.');
                        var code = $$"""
                                     //@@public struct {{(path[^1] == "" ? "MyPack" : HasDocs.brush(path[^1]))}} {
                                       {{value}}
                                     //@@}
                                     """;
                        package_proto.Remove(key);

                        if( 1 < path.Length )
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


            if( File.Exists(provided_path) )
            {
                var str   = process_proto_files(new[] { provided_path });
                var bytes = new byte[AdHoc.varint_bytes(str)];
                AdHoc.varint(str.AsSpan(), new Span<byte>(bytes));

                ChannelToServer.Start(new Proto
                                      {
                                          task   = task,
                                          name   = Path.GetFileName(provided_path),
                                          _proto = zip(bytes, Path.GetFileName(provided_path)[0..^6])
                                      });
            }
            else if( !Directory.Exists(provided_path) )
                exit($"Provided path {provided_path} does not exists.", 2);


            var tmp = Directory.CreateDirectory(new_random_tmp_path).FullName;
            var cut = Path.GetDirectoryName(provided_path)!.Length + 1;

            var files = new List<string>();
            var buf   = new byte[1000];

            Span<byte> span(int size) => new(buf.Length < size ?
                                                 buf = new byte[size] :
                                                 buf, 0, size);

            void proto_file(string path)
            {
                var dst_path = Path.Combine(tmp, path[cut..].Replace('\\', '_').Replace('/', '_'));
                var str      = process_proto_files(Directory.EnumerateFiles(path, "*.proto"));
                if( str.Length == 0 ) return;

                files.Add(dst_path);


                var bytes = span(AdHoc.varint_bytes(str));
                AdHoc.varint(str.AsSpan(), bytes);
                using( var dst = new FileStream(dst_path, FileMode.Create, FileAccess.Write) ) dst.Write(bytes);
            }

            proto_file(provided_path);

            foreach( var dir in Directory.EnumerateDirectories(provided_path, "*", SearchOption.AllDirectories).ToArray() )
                proto_file(dir);

            if( files.Count == 0 )
                exit($"No useful information found at the path: {provided_path}");

            ChannelToServer.Start(new Proto
                                  {
                                      task   = task,
                                      name   = Path.GetFileName(provided_path),
                                      _proto = zip(files)
                                  });
#endregion
        }

        private static void paths_parser(string[] paths)
        {
            if( paths.Length == 1 ) return;

            var collect = new HashSet<string>();

            foreach( var path in paths )
                if( path.EndsWith(".csproj") )
                {
                    var csproj = paths[1];

                    var dir = Path.GetDirectoryName(csproj)!;

                    foreach( var xml_path in XElement.Load(csproj)
                                                     .Descendants()
                                                     .Where(n => n.Name.ToString().Equals("Compile"))
                                                     .Select(n => Path.GetFullPath(n.Attribute("Include")!.Value, dir)) )
                        collect.Add(xml_path);
                }
                else if( path.EndsWith(".cs") ) collect.Add(path);
                else if( !Directory.Exists(destination_dir_path = path) )
                    Directory.CreateDirectory(destination_dir_path);

            collect.Remove(provided_path);

            provided_paths = collect.ToArray();
        }


        //folder for downloading files before their processing and deployment ( working(current) directory by default)
        public static string destination_dir_path = Directory.GetCurrentDirectory();

        public static string raw_files_dir_path => Path.Combine(destination_dir_path, Path.GetFileName(provided_path)[..^3]);

        public static int exit(string banner, int code = 1)
        {
            if( 0 < banner.Length )
                if( code == 0 )
                    LOG.Information(banner);
                else
                    LOG.Error(banner);

            LOG.Information("Press ENTER to exit");
            try { Console.In.ReadLine(); }
            catch( IOException ignored ) { }

            Environment.Exit(code);
            return code;
        }

        public static string   program_file_dir => Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase!).Path))!;
        public static string   provided_path; //can be AdHoc ptotocol description or Protocol buffers convertor input
        public static string[] provided_paths = Array.Empty<string>();
        public static string   task => HashFor(Path.GetDirectoryName(provided_path)!).ToString("X") + "_" + Path.GetFileName(provided_path);

        static ulong HashFor(string str)
        {
            var ret = 3074457345618258791ul;
            foreach( var ch in str )
            {
                ret += ch;
                ret *= 3074457345618258799ul;
            }

            return ret;
        }


        public class Deployment{
            static string deployment_instructions_txt;
            static string raw_files_dir_path;


            static StringBuilder sb = new StringBuilder();

            class LineInfo{
                public LineInfo(string path, string icon, string indent, string key)
                {
                    this.path   = path;
                    this.icon   = icon;
                    this.indent = indent;
                    receiver_files_lines.Add(key, this);
                }


                public bool                 skipped = false;
                public (Regex, string[])[]? targets;

                public List<string>? report;

                public void add_report(string report) => (this.report == null ?
                                                              this.report = new List<string>(2) :
                                                              this.report).Add(report);

                public string indent;
                public string path;
                public string icon;
                public string customization = "";

                public StringBuilder append_md_line()
                {
                    sb.Append(indent)
                      .Append("- ")
                      .Append(icon)
                      .Append('[')
                      .Append(Path.GetFileName(path))
                      .Append("](");

                    if( path.Contains(' ') )
                    {
                        sb.Append('<');
                        if( path[1] == ':' ) sb.Append('/');
                        sb.Append(path).Append(">) ");
                    }
                    else
                    {
                        if( path[1] == ':' ) sb.Append('/');
                        sb.Append(path);
                        sb.Append(") ");
                    }

                    sb.Replace('\\', '/');
                    return sb;
                }

                public void append_report_line()
                {
                    sb.Append(indent)
                      .Append(icon)
                      .Append(Path.GetFileName(path));

                    if( File.Exists(path) )
                        if( skipped || report == null )
                            sb.Append(" ⛔ ");
                        else
                        {
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

            // the keys
            //      InCS/Agent/lib/collections/BitList.cs
            //      InJAVA/Server/collections/org/unirail/collections/BitList.java
            static Dictionary<string, LineInfo> receiver_files_lines = new();

            public static int build_receiver_files_lines()
            {
                var root_path_len = raw_files_dir_path.Length + (raw_files_dir_path.EndsWith('/') || raw_files_dir_path.EndsWith('\\') ?
                                                                     0 :
                                                                     1);

                void add(string icon, string path, int level)
                {
                    sb.Clear();
                    for( var i = 0; i < level; i++ ) sb.Append("  ");
                    new LineInfo(path, icon, sb.ToString(), path[root_path_len..].Replace('\\', '/'));
                }

                void scan(string dir, int level)
                {
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

            public static void redeploy(string deployment_instructions_file)
            {
                raw_files_dir_path = provided_path[..^("Deployment.md".Length)]; //cut 'Deployment.md'  and get directory with source files
                if( !Directory.Exists(raw_files_dir_path) && !Directory.Exists(raw_files_dir_path = Path.Join(Directory.GetCurrentDirectory(), Path.GetFileName(raw_files_dir_path))) )
                    exit($"Cannot find source folder {Path.GetFileName(raw_files_dir_path)} at {Path.GetDirectoryName(provided_path)} and at working directory {Directory.GetCurrentDirectory()} redeploy process canceled");

                process(deployment_instructions_file);
            }

            public static void deploy(string raw_files_dir_path)
            {
                Deployment.raw_files_dir_path = raw_files_dir_path;

                var deployment_instructions_file_name = Path.GetFileName(raw_files_dir_path) + "Deployment.md"; //cut `.cs`

                //looking for deployment instructions file

                //1) take a look at working dir
                var deployment_instructions_file_path = Path.Join(Path.GetDirectoryName(raw_files_dir_path)!, deployment_instructions_file_name);
                if( File.Exists(deployment_instructions_file_path) ) goto deploy; //prefer working directory to extract template

                //2)take a look next to provided file
                deployment_instructions_file_path = Path.Join(Path.GetDirectoryName(provided_path)!, deployment_instructions_file_name);
                if( File.Exists(deployment_instructions_file_path) ) goto deploy;

                deployment_instructions_file_path = Path.Join(Path.GetDirectoryName(raw_files_dir_path)!, deployment_instructions_file_name);

                var deployment_instructions_file = File.OpenWrite(deployment_instructions_file_path);

                deployment_instructions_file.Write(Encoding.UTF8.GetBytes(@"**Autogenerated Deployment Instructions File**

This file is crucial for managing the deployment process. ✅⛔✔️ ✖️ ❌ ❎ 🟢 🔴 🟩 🟥 🟡 🔵 ⚠️ 🚫 🔺 🔻 ❓❗👀📅🕒

**Important:**
- **Do not rename this file.**
- If you need to move it, ensure it remains in the correct folder layout.
- Refer to the manual for further guidance if needed.


"));


                build_receiver_files_lines();
                sb.Clear();

                foreach( var info in receiver_files_lines.OrderBy(e => e.Key).Select(e => e.Value) )
                    info.append_md_line().Append('\n');

                var tree = sb.ToString();
                deployment_instructions_file.Write(Encoding.UTF8.GetBytes(tree));

                deployment_instructions_file.Write(Encoding.UTF8.GetBytes(@$" 


**Rerun Deployment Process:**
   - Execute the following command in your terminal or command prompt:
     ```shell
     AdHocAgent ""{deployment_instructions_file_path}""
     ```
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

```regexp
\.ts$
```

```shell
prettier --write FILE_PATH --print-width  999
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

                deploy:
                process(deployment_instructions_file_path);
            }


            private static void process(string deployment_instructions_file)
            {
                build_receiver_files_lines();

                deployment_instructions_txt = File.ReadAllText(deployment_instructions_file, Encoding.UTF8);

                LOG.Information("Starting the redeployment process of received source files in \"{src_dir}\", according to the {md} instructions file", raw_files_dir_path, provided_path);

                var    obsoletes_targets   = new List<string>(); //Items listed in the deployment instructions but not present among the receiver files
                var    Key_path_segment    = new Regex(@"(?<=\/|\\)(CPP|InCS|InGO|InJAVA|InRS|InTS)[\\/]*.*");
                var    start               = -1;
                Match? end                 = null;
                var    targets_lines_count = 0; //lines count
                var    any                 = new Regex(".*");
                var    has_some_targets    = false;

                foreach( Match match in new Regex(@"- (?:📁|＃|🧩|🧾|☕|🌀|📜|🌐|🎨|🐹|⚙️|🟪|🐦|📄|\{\})(?:\s*\[([^\]]*)\]\(([^)]*)\)[^\[\n\r]*)*(?:\s*⛔)?", RegexOptions.Multiline).Matches(deployment_instructions_txt) )
                {
                    targets_lines_count++;

                    end = match;
                    if( start == -1 ) start = match.Index;
                    // the link
                    //      InCS/Agent/lib/collections/BitList.cs
                    //      InJAVA/Server/collections/org/unirail/collections/BitList.java
                    var key = Key_path_segment.Match(match.Groups[2].Captures[0].Value).Groups[0].Value.Replace('\\', '/').Replace(">", "").Trim(); //Paths with whitespaces must be enclosed in <>
                    if( receiver_files_lines.TryGetValue(key, out var info) )
                    {
                        //- 📁[InCS](/AdHocTMP/AdhocProtocol/InCS) ✅ copy full structure [](/AdHoc/Protocol/Generated/InCS)
                        //           <--------- head ------------> <------------------- customization --------------------->
                        var head = match.Groups[2].Captures[0];
                        info.customization = match.ToString()[(head.Index + head.Length + 1 - match.Index)..]; //backup line customization
                        if( targets_lines_count == 1 &&
                            !string.Equals((match.Groups[2].Captures[0].Value).Trim(' ', '<', '>', '\\', '/'),
                                           Path.GetFullPath(info.path).Replace('\\', '/').Trim(' ', '\\', '/'),
                                           StringComparison.InvariantCultureIgnoreCase) )
                            targets_lines_count = int.MinValue; //need tree lines fully update mark


                        info.skipped = match.Value.Contains('⛔');

                        info.targets = match.Groups[1].Captures.Skip(1).Select(s => s.Value)
                                            .Zip(match.Groups[2].Captures.Skip(1).Select(t => t.Value[0] == '/' && t.Value[2] == ':' ?
                                                                                                  t.Value[1..].Replace('/', '\\') : //in the instruction, the path in the windows format
                                                                                                  t.Value))
                                            .GroupBy(_i_ =>
                                                     {
                                                         if( _i_.First == "" && _i_.Second == "" ) info.skipped = true; // []() case
                                                         return _i_.First;
                                                     })
                                            .Select(group => (group.Key.Equals("") ?
                                                                  any :
                                                                  new Regex(group.Key), group.Select(pair => pair.Second).ToArray())).ToArray();
                        if( 0 < info.targets.Length ) has_some_targets = true;

                        continue;
                    }

                    obsoletes_targets.Add(match.ToString()); //The line contains target information that is not applicable to the newly received code. Add it to the list of obsolete items.
                }

                if( end == null ) //Deployment instructions not found. Autogenerating default instructions.
                {
                    sb.Clear();

                    foreach( var info in receiver_files_lines.OrderBy(e => e.Key).Select(e => e.Value) )
                        info.append_md_line().Append('\n');

                    sb.Append("\n\n");
                    using( var deployment_instructions_file_ = File.OpenWrite(deployment_instructions_file) )
                    {
                        deployment_instructions_file_.Write(Encoding.UTF8.GetBytes(sb.ToString()));
                        deployment_instructions_file_.Write(Encoding.UTF8.GetBytes(deployment_instructions_txt));
                    }

                    exit($"Deployment instructions not fount and have been regenerated. Please update {deployment_instructions_file} with actual `deployment targets`");
                }

                if( !has_some_targets ) // Deployment instructions found, but no `target locations` detected.
                    exit($"No `target locations` detected. Ensure they are added, and verify that the provided deployment instructions file path `{deployment_instructions_file}` is correct.");


                //======================== binaries execute before deployment
                foreach( var before_deployment in new Regex(@"\[before deployment\]\((.+)\)").Matches(deployment_instructions_txt).Select(m => m.Groups[1].Value) )
                    Start_and_wait(before_deployment, "", raw_files_dir_path);


                var shell_tasks = new Regex(@"^([^\s].+?)(?:\r?\n(?=\s)|\r?\n?$)(?:\s+(.+?)(?:\r?\n(?=\s)|\r?\n?$))*", RegexOptions.Multiline);

                //======================== Execute custom code (shell or C#) on received files for formatting and other processing

                foreach( var (type, match) in new Regex(@"```regexp\s+(.+?)\s*```\s*```shell\s*([\s\S]*?)\s*```")
                                              .Matches(deployment_instructions_txt).Select(m => (0, m))
                                              .Concat(new Regex(@"```regexp\s+(.+?)\s*```\s*```csharp\s*([\s\S]*?)\s*```", RegexOptions.Multiline).Matches(deployment_instructions_txt).Select(m => (1, m)))
                                              .OrderBy(m => m.m.Index) ) //Ordered execution instructions
                {
                    var selector = string.IsNullOrEmpty(match.Groups[1].Value) ?
                                       null :
                                       new Regex(match.Groups[1].Value);
                    var target = match.Groups[2].Value;
                    int i;
                    switch( type )
                    {
                        case 0: //shell
                            foreach( var m in shell_tasks.Matches(target).Select(m => m) )
                                foreach( var info in receiver_files_lines.Values.Where(info => selector == null || selector.Match(info.path).Success) )

                                    Start_and_wait(m.Groups[1].Value[..(i = m.Groups[1].Value[0] == '"' ?
                                                                                m.Groups[1].Value.IndexOf('"', 1) + 1 :
                                                                                m.Groups[1].Value.IndexOf(' '))],
                                                   (m.Groups[1].Value[i..] + " " + string.Join(" ", m.Groups[2].Captures)).Replace("FILE_PATH", info.path.Contains(' ') ?
                                                                                                                                                    $"\"{info.path}\"" :
                                                                                                                                                    info.path), raw_files_dir_path);
                            continue;
                        case 1: //C#
                            var cut = -1;

                            var obj = Path.GetDirectoryName(typeof(object).Assembly.Location);
                            var refs = new[]
                                {
                                    "mscorlib.dll",
                                    "System.dll",
                                    "System.Core.dll",
                                    "System.Runtime.dll"
                                }.Select(s => Path.Combine(obj, s))
                                 .Concat(new[]
                                         {
                                             typeof(object).Assembly.Location,
                                             typeof(Console).Assembly.Location,
                                             Assembly.Load("System.Runtime").Location,
                                             Assembly.Load("System.Collections").Location,
                                             Assembly.Load("System.Linq").Location,
                                             Assembly.Load("System.Text.RegularExpressions").Location
                                         }).Select(p => MetadataReference.CreateFromFile(p))
                                 .Concat(
                                         new Regex(@"^(?:\""([^\""]+)\""\r?\n)*")
                                             .Matches(target)
                                             .SelectMany(m =>
                                                         {
                                                             cut = m.Index + m.Length;
                                                             return m.Groups[1].Captures;
                                                         })
                                             .SelectMany(c => AppDomain.CurrentDomain.GetAssemblies()
                                                                       .Where(a => a.GetTypes().Any(t => t.FullName.StartsWith(c.Value))))
                                             .Distinct()
                                             .Select(assembly => MetadataReference.CreateFromFile(assembly.Location)).ToArray()
                                        );


                            if( 0 < cut ) target = target[cut..];


                            var compilation = CSharpCompilation.Create(
                                                                       Path.GetRandomFileName(),
                                                                       syntaxTrees: new[] { CSharpSyntaxTree.ParseText(target) },
                                                                       references: refs,
                                                                       options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

                            var ms     = new MemoryStream();
                            var result = compilation.Emit(ms);

                            if( !result.Success )
                            {
                                foreach( var diagnostic in result.Diagnostics ) { Console.Error.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()}"); }

                                exit("");
                            }

                            ms.Seek(0, SeekOrigin.Begin);
                            var compiledAssembly = Assembly.Load(ms.ToArray());


                            var args = new string [1];
                            var objs = new object[] { args };
                            var main = compiledAssembly.GetType("Program")!.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;

                            foreach( var info in receiver_files_lines.Values.Where(info => File.Exists(info.path) && (selector == null || selector.Match(info.path).Success)) )
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

                //==============================  Copy custom injected code from current files to newly received files, then overwrite current files
                var infos = receiver_files_lines.Values.GetEnumerator();

                var ancestors = new List<LineInfo>();
                while( infos.MoveNext() )
                {
                    start:
                    var info = infos.Current;
                    if( info.skipped )
                    {
                        if( Directory.Exists(info.path) )
                            while( infos.MoveNext() && infos.Current.path.StartsWith(info.path) )
                                ;
                        else infos.MoveNext();
                        goto start;
                    }

                    while( 0 < ancestors.Count && !Path.GetDirectoryName(info.path)!.Equals(ancestors[^1].path) )
                        ancestors.RemoveAt(ancestors.Count - 1);

                    if( Directory.Exists(info.path) )
                    {
                        ancestors.Add(info);
                        continue;
                    }

                    var received_src_file = info.path;

                    //The deployment process will process `custom code injection point` and copy according instructions with matched selectors of a file's parent folders .
                    foreach( var parent_folder in ancestors.Where(i => i.targets is { Length: > 0 }) )
                        foreach( var target_path in parent_folder.targets!.Where(t => t.Item1.Match(received_src_file).Success).SelectMany(t => t.Item2) )
                            info.add_report(copy_custom_code_from_current_files_to_the_newly_received_files(received_src_file, Path.Combine(target_path[^1] == '/' || target_path[^1] == '\\' ? //If the link ends with '/', the received item will be copied into the specified path.
                                                                                                                                                Path.Combine(target_path, Path.GetFileName(parent_folder.path)) :
                                                                                                                                                target_path,
                                                                                                                                            received_src_file[(parent_folder.path.Length + 1)..])));
                    //self file instructions
                    if( info.targets == null ) continue;
                    foreach( var target_path in info.targets.SelectMany(t => t.Item2) )
                        info.add_report(copy_custom_code_from_current_files_to_the_newly_received_files(received_src_file, target_path[^1] == '/' || target_path[^1] == '\\' ? //If the link ends with '/', the received item will be copied into the specified path.
                                                                                                                               Path.Combine(target_path, Path.GetFileName(received_src_file)) :
                                                                                                                               target_path));
                }

                foreach( var info in receiver_files_lines.OrderBy(e => e.Key).Select(e => e.Value) )
                {
                    sb.Clear();
                    info.append_report_line();
                    Console.Out.Write(sb.ToString());
                }

                //binaries execute after deployment
                foreach( var after_deployment in new Regex(@"\[after deployment\]\((.+)\)").Matches(deployment_instructions_txt).Select(m => m.Groups[1].Value) )
                    Start_and_wait(after_deployment, "", raw_files_dir_path);

                if( 0 < obsoletes_targets.Count || targets_lines_count != receiver_files_lines.Count ) // Fully update tree lines needed
                {
                    if( 0 < obsoletes_targets.Count )
                    {
                        LOG.Information("Items listed in the deployment instructions but not present among the receiver files.");
                        foreach( var useless in obsoletes_targets )
                            Console.Out.WriteLine(useless);
                    }

                    using var deployment_instructions_file_ = File.OpenWrite(deployment_instructions_file);
                    deployment_instructions_file_.Write(Encoding.UTF8.GetBytes(deployment_instructions_txt[..start]));

                    foreach( var info in receiver_files_lines.OrderBy(e => e.Key).Select(e => e.Value) ) //Re-render tree
                    {
                        sb.Clear();
                        info.append_md_line().Append(info.customization).Append('\n'); //Preserve customization
                        deployment_instructions_file_.Write(Encoding.UTF8.GetBytes(sb.ToString()));
                    }

                    sb.Clear();
                    sb.Append("\n\n");
                    deployment_instructions_file_.Write(Encoding.UTF8.GetBytes(sb.ToString()));

                    deployment_instructions_file_.Write(Encoding.UTF8.GetBytes(deployment_instructions_txt[(end.Index + end.Length) ..]));
                }

                LOG.Information("Deployment process completed.");
                exit("", 0);
            }

            private static readonly Dictionary<string, string> uid2custom_code = new();
            private static readonly string[]                   rn              = { "\r\n", "\r", "\n" };

            private static readonly Regex  JAVA                 = new(@"//#region\s*>.*?$\s*([\s\S]*?)\s*//#endregion\s*>\s+?(.*?)\s*?$", RegexOptions.Multiline);
            private static readonly Regex  CS                   = new(@"#region\s*>.*?$\s*([\s\S]*?)\s*#endregion\s*>\s+?(.*?)\s*?$", RegexOptions.Multiline);
            private static          string result_code_tmp_file = Path.GetTempFileName();

            private static string copy_custom_code_from_current_files_to_the_newly_received_files(string raw_file_path, string target_file_path)
            {
                if( !Directory.Exists(Path.GetDirectoryName(target_file_path)) )
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target_file_path)!);
                    goto just_copy;
                }

                if( !File.Exists(target_file_path) ) goto just_copy;

                uid2custom_code.Clear();

                var regex = raw_file_path.EndsWith(".java") || raw_file_path.EndsWith(".ts") ?
                                JAVA :
                                CS;

                //collect custom code
                foreach( var injection_point in regex.Matches(File.ReadAllText(target_file_path)).Where(m => 0 < m.Groups[1].Value.Length) )
                {
                    var uid = injection_point.Groups[2].Value.Trim();

                    if( injection_point.Groups[1].Value.Split(rn, StringSplitOptions.RemoveEmptyEntries).All(line => line.Trim().EndsWith("//")) ) continue;

                    uid2custom_code.Add(uid, injection_point.Groups[1].Value + "\r\n");
                }

                if( uid2custom_code.Count == 0 ) goto just_copy;

                var result_code = File.OpenWrite(result_code_tmp_file);
                var raw_code    = File.ReadAllText(raw_file_path);
                var last_index  = 0;
                //copy custom code to the newly received files
                foreach( var injection_point in regex.Matches(raw_code).Select(m => m) )
                {
                    var uid = injection_point.Groups[2].Value;

                    if( !uid2custom_code.TryGetValue(uid, out var custom_code) ) continue;

                    var code_area = injection_point.Groups[1];
                    result_code.Write(Encoding.UTF8.GetBytes(raw_code[last_index..code_area.Index]));
                    result_code.Write(Encoding.UTF8.GetBytes(custom_code));
                    last_index = code_area.Index + code_area.Length; //skip generated code
                    uid2custom_code.Remove(uid);
                }

                result_code.Write(Encoding.UTF8.GetBytes(raw_code[last_index..raw_code.Length]));
                result_code.Flush();
                result_code.Close();

                if( 0 < uid2custom_code.Count ) //orphaned custom code has been detected
                {
                    LOG.Error("Orphaned custom code");
                    foreach( var (id, code) in uid2custom_code )
                    {
                        LOG.Error("{id}", id);
                        Console.WriteLine(code);
                    }

                    LOG.Error("In the file {target_file_path}, orphaned custom code has been detected. Would you like to continue anyway? Enter 'N' if you want to stop.", raw_file_path.Replace('\\', '/'));

                    if( Console.Read() is 'N' or 'n' )
                    {
                        exit("Bye", -1);
                        return "";
                    }
                }

                File.Move(result_code_tmp_file, target_file_path, true);
                return "✅  " + target_file_path;

                just_copy:
                File.Copy(raw_file_path, target_file_path, true);
                return "👉 " + target_file_path;
            }
        }

        public static string Start_and_wait(string exe, string args, string WorkingDirectory)
        {
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
            // Attempt to execute the process twice with different configurations
            for( var i = 0;
                 i < 3;
                 i++, startInfo.FileName = exe + (OperatingSystem.IsWindows() ?
                                                      i == 1 ?
                                                          ".exe" :
                                                          ".cmd" :
                                                      i == 1 ?
                                                          ".sh" :
                                                          ".bash"), startInfo.UseShellExecute = true )
            {
                try
                {
                    // First attempt: Execute the process
                    using( var process = Process.Start(startInfo) )
                    {
                        // Read the entire output of the process
                        var output = process.StandardOutput.ReadToEnd();
                        error += process.StandardError.ReadToEnd();
                        // Wait for the process to complete
                        process.WaitForExit();
                        // Return the output if successful
                        return output;
                    }
                }
                catch( Exception e )
                {
                    // If the first attempt fails, modify the start info for the second attempt
                    startInfo.UseShellExecute = false;
                    try
                    {
                        // Second attempt: Execute the process with modified start info
                        using( var process = Process.Start(startInfo) )
                        {
                            var output = process.StandardOutput.ReadToEnd();
                            error += process.StandardError.ReadToEnd();
                            process.WaitForExit();
                            if( 0 < error.Length )
                            {
                                LOG.Error("An error {error} occurred while executing {command_line}. Would you like to continue? Enter 'N' to stop.", error, (exe + " " + startInfo.Arguments).Replace('\\', '/'));

                                if( Console.Read() is 'N' or 'n' )
                                {
                                    exit("Bye", -1);
                                    return "";
                                }
                            }

                            return output;
                        }
                    }
                    catch( Exception ee )
                    {
                        // If both attempts fail on the second iteration, exit the application
                        if( 1 < i ) { exit($"Error executing {exe} {args}. Exception details:\n{ee}", -1); }
                        // If it's the first iteration, the loop will continue to the second attempt
                    }
                }
            }

            // Note: If execution reaches here, it means both attempts failed on the first iteration
            // and succeeded on the second iteration. This case is not explicitly handled.
            return exe + " " + args;
        }
    }
}