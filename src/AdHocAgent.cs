using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Serilog;
using Tommy; //https://github.com/dezhidki/Tommy

namespace org.unirail
{
    // https: //docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/semantic-analysis
    class AdHocAgent
    {
        public static readonly Serilog.Core.Logger LOG = new LoggerConfiguration() // >> https://github.com/serilog/serilog/wiki/Getting-Started <<
                                                         .MinimumLevel.Debug()
                                                         .WriteTo.Console().CreateLogger();


#region refresh GitHUB Code
        public static async Task updateMyPersonalSecretGitHubAuthorizationCode()
        {
            //https://docs.github.com/en/developers/apps/building-oauth-apps/authorizing-oauth-apps#1-request-a-users-github-identity
            var url = $"https://github.com/login/oauth/authorize?client_id={app_props["adhoc_id"]}&scope={HttpUtility.UrlEncode("read:user user:email", Encoding.UTF8)}";

            Process.Start(new ProcessStartInfo() { FileName = url, UseShellExecute = true });
            LOG.Information("Authorization. Open browser {0} and accept security request... Waiting for GITHUB reply...", url);

            await HandleIncomingConnections();
        }


        private static async Task HandleIncomingConnections()
        {
            var MyPersonalSecretGitHubAuthorizationCode = "";
            try
            {
                var listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:4321/");
                listener.IgnoreWriteExceptions = true; //ignore "The specified network name is no longer available" 
                listener.Start();

                var ctx = listener.GetContext(); //block

                Console.Out.WriteLine("listener.GetContext()");

                MyPersonalSecretGitHubAuthorizationCode = ctx.Request.QueryString["code"];
                AuthorizationCodeOK                     = true;
                var resp = ctx.Response;
                resp.ContentType     = "text/html";
                resp.ContentEncoding = Encoding.ASCII;
                resp.ContentLength64 = close_browser_bytes.LongLength;

                resp.OutputStream.Write(close_browser_bytes);
                resp.OutputStream.Close();
            }
            catch (Exception e)
            {
                LOG.Error(e.ToString());
                throw;
            }


            if (MyPersonalSecretGitHubAuthorizationCode == null) LOG.Error("Authorization failure");
            else
            {
                if (app_props.HasKey("MyPersonalSecretGitHubAuthorizationCode")) app_props["MyPersonalSecretGitHubAuthorizationCode"] = MyPersonalSecretGitHubAuthorizationCode;
                else app_props.Add("MyPersonalSecretGitHubAuthorizationCode", new TomlString() { Value = MyPersonalSecretGitHubAuthorizationCode });

                await using var writer = File.CreateText(app_props_file);
                app_props.WriteTo(writer);
                await writer.FlushAsync();
            }
        }
#endregion

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
                if (File.Exists(file)) return file;
                var toml = File.OpenWrite(file);
                Assembly.GetExecutingAssembly().GetManifestResourceStream("AdHocAgent.Templates.AdHocAgent.toml")!.CopyToAsync(toml);
                LOG.Warning("Application configuration file {0} is extracted from template, its content maybe outdated.", file);
                toml.Flush();
                toml.Close();
                return file;
            }
        }

        public static TomlTable app_props = TOML.Parse(File.OpenText(app_props_file)); //load application 'toml file'   https://www.ryansouthgate.com/2016/03/23/iconfiguration-in-netcore/

        public static bool AuthorizationCodeOK = app_props.HasKey("MyPersonalSecretGitHubAuthorizationCode");

        static string zip_exe
        {
            get
            {
                var zip_exe = app_props["7zip_exe"].AsString.Value;
                if (!File.Exists(zip_exe))
                    exit($" The value of 7zip_exe in {Path.Join(app_props_file, "AdHocAgent.toml")} point to none exiting 7zip binary file {zip_exe}. Please install 7zip (https://www.7-zip.org/download.html) and edit path.");
                return zip_exe;
            }
        }

        public static void unzip(Stream src, string dst_folder)
        {
            var tmp_src = File.OpenWrite(tmp);
            src.CopyTo(tmp_src);
            tmp_src.Flush();
            tmp_src.Close();
            unzip(tmp_src.Name, dst_folder);
            File.Delete(tmp_src.Name);
        }

        public static void unzip(string src_file, string dst_folder) => Process.Start(new ProcessStartInfo()
                                                                                      {
                                                                                          FileName               = zip_exe,
                                                                                          RedirectStandardOutput = true,
                                                                                          Arguments              = $" x \"{src_file}\" -aoa -o\"{dst_folder}\"",
                                                                                          WindowStyle            = ProcessWindowStyle.Hidden
                                                                                      })!.WaitForExit();

        private static string tmp => Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());

        public static byte[] zip(Stream src)
        {
            var tmp_src = File.OpenWrite(tmp);
            src.CopyTo(tmp_src);
            tmp_src.Flush();
            tmp_src.Close();
            var tmp_dst = tmp;
            zip(tmp_src.Name, tmp_dst);

            using var ret = new MemoryStream();
            using (var file = File.OpenRead(tmp_dst))
                file.CopyTo(ret);

            File.Delete(tmp_src.Name);
            File.Delete(tmp_dst);
            return ret.ToArray();
        }

        public static void zip(string src, string dst_file)
        {
            Process.Start(new ProcessStartInfo()
                          {
                              FileName               = zip_exe,
                              Arguments              = $" a -t7z -mx=9 -mfb=64  -m0=lzma  \"{dst_file}\" \"{src}\"",
                              RedirectStandardOutput = true,
                              WindowStyle            = ProcessWindowStyle.Hidden
                          })!.WaitForExit();

            if (!File.Exists(dst_file))                //7 zip append `.7z` extension to the name. cut it
                File.Move(dst_file + ".7z", dst_file); //cut .7z extension
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
            if (paths.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("AdHocAgent utility");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" accept following commandline input:");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("  the first argument is the path to the task file,");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\tif provided path ends with:");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t\t.cs   ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" - is means to upload the provided protocol description file to the server to generate source code");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t\t.cs!  ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" - to upload the provided protocol description file to generate source code and test it");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t\t.cs?  ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" - to show of the information of the provided protocol description file in the viewer");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\t\t.proto");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" - is means provided file is in Protocol Buffers format and will send to the server to convert it into Adhoc protocol description format");
                Console.Write("\tthe rest arguments are paths to source ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(".cs ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("and  project ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(".csproj ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("files imported and used with the root protocol description file.");
                Console.WriteLine("  if the last argument is a path to a folder it's used as the intermediate result output folder. if not provided - the current working directory is used.");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Besides command-line arguments, the ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("AdHocAgent utility");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" needs:");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  AdHocAgent.toml ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("- the file that contains ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\t\turl to the server ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("\tand, paths to local:  ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\t\tIDE");
                Console.WriteLine("\t\t7zip compression utility(used for best compression)");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("\tAdHocAgent utility");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(" search ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("AdHocAgent.toml");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(" file next to self.");
                Console.WriteLine("\tIf the file does not exist, the utility generates this file template, so just needed to update the information in this file according to your configuration.");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  Deployment_instructions.md");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(" - only for code generation task. The file with received result deployment instructions.");


                Console.ForegroundColor = ConsoleColor.White;

                return;
            }

            if (paths[0].EndsWith(".cs?")) // run provided protocol description file viewer
            {
                provided_path = paths[0][..^1]; //cut '?'
                paths_parser(paths);
                await ChannelToObserver.start();
                return;
            }


            is_testing = paths[0].EndsWith("!");

            provided_path = is_testing
                                ? paths[0][..^1]
                                : paths[0];

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
            if (provided_path.EndsWith(".cs"))
            {
                paths_parser(paths);
                ChannelToServer.Start(ProjectImpl.init());
                return;
            }
#endregion
#region .proto files processing
            if (1 < paths.Length) //if destination_dir_path explicitly provided instead of Directory.GetCurrentDirectory()  
                if (!Directory.Exists(destination_dir_path = paths[1]))
                    Directory.CreateDirectory(destination_dir_path);


            var str           = "";
            var proto_package = "";

            IEnumerable<string>? files = null;
            if (File.Exists(provided_path)) files = new[] { provided_path };
            else if (Directory.Exists(provided_path))
            {
                files = Directory.EnumerateFiles(provided_path, "*.proto").ToArray();

                if (!files.Any()) exit($"No any *.proto files found on provided paths:\n {provided_path}.", 12);
            }
            else exit($"Provided path {provided_path} does not exists.", 2);

            var imported = new HashSet<string>();

            foreach (var proto_file in files)
            {
                if (imported.Contains(proto_file)) continue;
                imported.Add(proto_file);

                var proto = File.ReadAllText(proto_file);
                var ret   = "";
                var top   = 0;

                foreach (var match in package.Matches(proto).OrderBy(Match => -Match.Index))
                {
                    if (proto_package.Length == 0)
                        proto_package = match.Groups[match.Groups[1].Success
                                                         ? 1
                                                         : 2].Value;
                    if (ret.Length == 0) ret =  proto[(match.Index + match.Length)..];
                    else ret                 += proto[(match.Index + match.Length)..top];
                    top = match.Index;
                }

                if (0 < ret.Length) proto = proto[..top] + ret;
                top = 0;
                ret = "";

                foreach (var match in syntax.Matches(proto).OrderBy(Match => -Match.Index))
                {
                    if (ret.Length == 0) ret =  proto[(match.Index + match.Length)..];
                    else ret                 += proto[(match.Index + match.Length)..top];
                    top = match.Index;
                }

                if (0 < ret.Length) proto = proto[..top] + ret;
                top = 0;
                ret = "";

                foreach (var match in imports.Matches(proto).OrderBy(Match => -Match.Index))
                {
                    if (ret.Length == 0) ret =  proto[(match.Index + match.Length)..];
                    else ret                 += proto[(match.Index + match.Length)..top];
                    top = match.Index;


                    var import = match.Groups[match.Groups[1].Success
                                                  ? 1
                                                  : 2].Value;

                    var file = search_import(proto_file, import);

                    if (imported.Add(file)) ret = process_proto_file(file, imported) + ret;
                }

                str += $"\n\n//======================={Path.GetFileName(proto_file)}================\n\n" + (0 < ret.Length
                                                                                                                 ? proto[..top] + ret
                                                                                                                 : proto);
            }

            if (0 < proto_package.Length) str = $"package {proto_package};\n\n" + str;

            str = "syntax = \"proto3\";\n\n" + str;

            ChannelToServer.Start(new Agent.AgentToServer.Proto()
                                  {
                                      task  = task,
                                      name  = Path.GetFileName(provided_path),
                                      proto = zip(new MemoryStream(Encoding.UTF8.GetBytes(str)))
                                  });
#endregion
        }

        private static void paths_parser(string[] paths)
        {
            if (paths.Length == 1) return;

            var collect = new HashSet<string>();

            foreach (var path in paths)
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
                else if (File.Exists(path)) //                   can be only after_deployment_exec file
                    if (Deployment.after_deployment_exec.Length == 0)
                        Deployment.after_deployment_exec = path; //the path to after deployment executable file
                    else
                        exit($"In the command line the second path to after-deployment-executable-binary {path} detected. The first path was {Deployment.after_deployment_exec}. Can accept only one.", -222);
                else if (!Directory.Exists(destination_dir_path = path))
                    Directory.CreateDirectory(destination_dir_path);

            collect.Remove(provided_path);

            provided_paths = collect.ToArray();
        }


        private static readonly Regex strings_regex_escaper = new(@"【([\u0000-\uFFFF]*?)】", RegexOptions.Multiline);


        //folder for downloaded files before their processing and deployment ( working(current) directory by default)
        public static string destination_dir_path = Directory.GetCurrentDirectory();

        public static string raw_files_dir_path => Path.Combine(destination_dir_path, Path.GetFileName(provided_path)[..^3]);

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

        public static string   program_file_dir => Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase!).Path))!;
        public static string   provided_path; //can be AdHoc ptotocol description or Protocol buffers convertor input
        public static string[] provided_paths = Array.Empty<string>();
        public static string   task => HashFor(Path.GetDirectoryName(provided_path)!).ToString("X") + "_" + Path.GetFileName(provided_path);

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

        static readonly byte[] close_browser_bytes = Encoding.ASCII.GetBytes("<script>window.close();</script>");


#region process proto file
        private static string process_proto_file(string proto_file, HashSet<string>? imported)
        {
            var proto = File.ReadAllText(proto_file);

            if (imported == null) imported = new HashSet<string>();
            else
            {
                proto = syntax.Replace(proto, "", 1);
                proto = package.Replace(proto, "", 1);
            }

            var ret = "";
            var top = 0;


            foreach (var match in imports.Matches(proto).OrderBy(Match => -Match.Index))
            {
                if (ret.Length == 0) ret =  proto[(match.Index + match.Length)..];
                else ret                 += proto[(match.Index + match.Length)..top];

                top = match.Index;
                var import = match.Groups[match.Groups[1].Success
                                              ? 1
                                              : 2].Value;

                var import_file = search_import(proto_file, import);

                if (imported.Add(import_file)) ret = process_proto_file(import_file, imported) + ret;
            }

            return ret.Length == 0
                       ? proto
                       : proto[..top] + ret;
        }

        private static string search_import(string dst_proto_file, string seek_import)
        {
            var file = Path.Join(Path.GetDirectoryName(dst_proto_file)!, seek_import); //serch next to dst

            if (File.Exists(file)) return file;

            var search_from_CurrentDirectory = Path.Join(Directory.GetCurrentDirectory(), seek_import);

            if (File.Exists(search_from_CurrentDirectory)) return search_from_CurrentDirectory;

            var search_in_CurrentDirectory = Path.Join(Directory.GetCurrentDirectory(), Path.GetFileName(seek_import));
            if (File.Exists(search_in_CurrentDirectory)) return search_in_CurrentDirectory;

            var search_in_dst_Directory = Path.Join(Path.GetDirectoryName(dst_proto_file)!, Path.GetFileName(seek_import));
            if (File.Exists(search_in_dst_Directory)) return search_in_dst_Directory;

            LOG.Error("Imported into file {dst} proto file {import} does not exist.\n Searched on paths: \n{File}\n{SearchFromCurrentDirectory}\n{SearchInCurrentDirectory}\n{search_in_dst_Directory}",
                      dst_proto_file, Path.GetFileName(file), file, search_from_CurrentDirectory, search_in_CurrentDirectory, search_in_dst_Directory);
            exit("", 44);

            return file;
        }

        private static readonly Regex imports = new(@"^\s*import\s+public\s+\042(.+)\042\s*;|^\s*import\s+\042(.+)\042\s*;", RegexOptions.Multiline);    //  import "myproject/other_protos.proto"; /  import public "new.proto";
        private static readonly Regex package = new(@"^\s*option\s+java_package\s+=\s+\042(.+)\042\s*;|^\s*package\s+(.+)\s*;", RegexOptions.Multiline); // package foo.bar;
        private static readonly Regex syntax  = new(@"^\s*syntax\s.+;", RegexOptions.Multiline);
#endregion


        public class Deployment
        {
            public abstract class Pointer
            {
                public Match match;

                public abstract void     execute();
                public          Pointer? next;
            }

            public static string after_deployment_exec = ""; //binary execute after deployment 

            private static string deployment_instructions_txt;
            private static string raw_files_dir_path;

            static int index2line(int index)
            {
                var line = 0;
                for (var i = 0; i < index; i++)
                    switch (deployment_instructions_txt[i])
                    {
                        case '\r':
                            line++;
                            if (i + 1 < deployment_instructions_txt.Length && deployment_instructions_txt[i + 1] == '\n') i++;
                            continue;
                        case '\n':
                            line++;
                            continue;
                    }

                return line++;
            }

            public static void deploy(string raw_files_dir_path) //protocol description case only
            {
                Deployment.raw_files_dir_path = raw_files_dir_path;

                var deployment_instructions_file_name = Path.GetFileName(raw_files_dir_path) + "Deployment.md"; //cut `.cs`

                //looking for deployment instructions file

                //take a look next to provided file
                var deployment_instructions_file_path = Path.Join(Path.GetDirectoryName(provided_path)!, deployment_instructions_file_name);
                if (File.Exists(deployment_instructions_file_path)) goto deploy;

                //take a look at working dir
                deployment_instructions_file_path = Path.Join(Path.GetDirectoryName(raw_files_dir_path)!, deployment_instructions_file_name);
                if (File.Exists(deployment_instructions_file_path)) goto deploy; //prefer working directory to extract template 


                //extract template
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AdHocAgent.Templates.Deployment_instructions.md"))
                {
                    var file = File.OpenWrite(deployment_instructions_file_path);
                    stream!.CopyTo(file);
                    file.Flush();
                    file.Close();
                }

                LOG.Warning(@"Cannot find deployment instructions file {deploy_file_path}. 
Search the file in the following directories:
    {provided_path}
    {raw_files_dir_path}    with no luck. Extract default template deployment instruction. 
Edit {deployment_instructions_file_path} according to your deployment needs",
                            deployment_instructions_file_name,
                            Path.GetDirectoryName(provided_path),
                            Path.GetDirectoryName(raw_files_dir_path),
                            deployment_instructions_file_path);
                return;

                deploy:

                deployment_instructions_txt = strings_regex_escaper.Replace(File.ReadAllText(deployment_instructions_file_path, Encoding.UTF8), m => Regex.Escape(m.Groups[1].Value));
                var root = new Section.Pointer();

                LOG.Information("Starting deployment process according to the instructions in the `{deployment_instructions_file_path}`", deployment_instructions_file_path);

                while (true)
                {
                    Pointer? p = root, selected = null;

                    do
                        if (p.match.Success && (selected == null || p.match.Index < selected.match.Index))
                            selected = p;
                    while ((p = p.next) != null);

                    if (selected == null) break;
                    selected.execute();
                }


                LOG.Information($"Deployment process finish.");
                if (0 < after_deployment_exec.Length)
                {
                    LOG.Information($"Execute after deploy process after_deployment_exec.");

                    Process.Start(new ProcessStartInfo { FileName = after_deployment_exec, WorkingDirectory = raw_files_dir_path })!.WaitForExit();
                }

                exit("", 0);
            }

            private static readonly List<Section> sections = new();
            private static          Section       section;

            private static void deploy(string src_file_path, string dst_file_path)
            {
                var src                = File.ReadAllText(src_file_path);
                var path_matching_part = src_file_path[raw_files_dir_path.Length ..];

                if (!Directory.Exists(Path.GetDirectoryName(dst_file_path))) Directory.CreateDirectory(Path.GetDirectoryName(dst_file_path));
                File.WriteAllText(dst_file_path, sections.Where(s => s.files_selector.Match(path_matching_part).Success).SelectMany(s => s.modification_commands).Aggregate(src, (current, modifier) => modifier.modify(current)));
            }

            public class Section
            {
                public readonly Regex files_selector;
                public Section(string files_selector) { this.files_selector = new Regex(files_selector); }

                public List<SourceCodeModificationCommand> modification_commands = new();


                public class Pointer : AdHocAgent.Deployment.Pointer
                {
                    private static readonly Regex files_selector_extractor = new(@"^\#{3,}\s*(.*?)\s*$", RegexOptions.Multiline);

                    public Pointer()
                    {
                        match = files_selector_extractor.Match(deployment_instructions_txt);
                        next  = new SourceCodeModificationCommand.Pointer();
                    }

                    public override void execute()
                    {
                        sections.Add(section = new Section(match.Groups[1].Value));
                        match = match.NextMatch();
                    }
                }

                public class SourceCodeModificationCommand
                {
                    private readonly Regex  source_code_selector;
                    public readonly  string source_code_modification;

                    public SourceCodeModificationCommand(string source_code_selector, string source_code_modification)
                    {
                        this.source_code_selector     = new Regex(source_code_selector);
                        this.source_code_modification = source_code_modification;
                    }

                    public string modify(string src) => source_code_selector.Replace(src, source_code_modification);

                    public class Pointer : AdHocAgent.Deployment.Pointer
                    {
                        private static readonly Regex regex_commands = new(@"^```.*$([\u0000-\uFFFF]*?)```", RegexOptions.Multiline);

                        public Pointer()
                        {
                            match = regex_commands.Match(deployment_instructions_txt);

                            next = new ExecuteAndDeploy();
                        }

                        public override void execute()
                        {
                            var p = match!.Groups[1].Value.Split("➤"); // regex-select-source_code ➤ regex_replace
                            if (p.Length == 2)
                                section.modification_commands.Add(new SourceCodeModificationCommand(p[0].Trim(), p[1]));
                            else
                                LOG.Warning("Command `{Value}` is malformed. It should contain '➤' symbol", match!.Groups[1].Value);

                            match = match.NextMatch();
                        }
                    }

                    class ExecuteAndDeploy : AdHocAgent.Deployment.Pointer //commands lists
                    {
                        private Match src_column;
                        private Match src_filter;
                        private Match dst_column;

                        public ExecuteAndDeploy()
                        {
                            src_column = new Regex(@"\|\s+\[.+?\]\(\s*(.+?)\s*\).*\|.*\|\s*\n").Match(deployment_instructions_txt);
                            src_filter = new Regex(@"\|.+\[.+\]\(.+\)\s*""(.+)"".*?\|").Match(deployment_instructions_txt);
                            dst_column = new Regex(@"\[.+?\]\(\s*(.+?)\s*\)").Match(deployment_instructions_txt);
                            match      = new Regex(@"\|\s+src\s+\|\s+dst\s+\|\s*\|-+\|-+\|\s*\n", RegexOptions.Multiline).Match(deployment_instructions_txt); // 'execute and deploy table' first row end finder

                            next = null;
                        }

                        private int line_end_index;

                        int next_line_end()
                        {
                            for (; line_end_index < deployment_instructions_txt.Length; line_end_index++)
                                if (deployment_instructions_txt[line_end_index] == '\n')
                                    return line_end_index++;

                            return int.MaxValue;
                        }

                        static string cleanup_md_path(string path)
                        {
                            path = path.Trim();
                            if (path[0] == '<') path = path[1..^1]; //             [If you have spaces in the filename](</C:/Program Files (x86)>)
                            if (path[2] == ':') path = path[1..];   //             [Link to file in another dir on a different drive](/D:/AdHoc/) 

                            return path;
                        }

                        public override void execute()
                        {
                            line_end_index = match.Index + match.Length; //execute and deploy table first row end

                            while (src_column.Success && src_column.Index < line_end_index) src_column = src_column.NextMatch(); //search `src_path` in the src column of the first row
                            next_line_end();

                            while (true) // execute modify instructions and deploy result
                            {
                                while (src_filter.Success && src_filter.Index < line_end_index) src_filter = src_filter.NextMatch();

                                while (dst_column.Success && dst_column.Index < src_column.Index + 3) dst_column = dst_column.NextMatch(); //search first `dst_path` in the dst column of the first row
                                if (!dst_column.Success || line_end_index <= dst_column.Index)
                                    exit($"The `dst` column contains no path at line: {index2line(line_end_index)}");

                                var src_path = Path.GetFullPath(Path.Join(destination_dir_path, cleanup_md_path(src_column.Groups[1].Value)));

                                if (File.Exists(src_path)) // src_path is a file
                                    for (; dst_column.Index < line_end_index; dst_column = dst_column.NextMatch())
                                    {
                                        var dst_path = cleanup_md_path(dst_column.Groups[1].Value);
                                        deploy(src_path, dst_path.EndsWith(Path.GetExtension(src_path))
                                                             ? dst_path                                        //src_path is a file copy to  dst_path a file 
                                                             : Path.Join(dst_path, Path.GetFileName(src_path)) //src_path is a file copy to  dst_path a folder 
                                              );
                                    }
                                else if (Directory.Exists(src_path)) // src_path is a folder
                                    for (; dst_column.Index < line_end_index; dst_column = dst_column.NextMatch())
                                    {
                                        var filter = src_filter.Success && src_filter.Index == src_column.Index
                                                         ? src_filter.Groups[1].Value
                                                         : "*.*";

                                        var dst_dir_path = cleanup_md_path(dst_column.Groups[1].Value); //folder can be copy to folder only

                                        foreach (var src_file_path in Directory.GetFiles(src_path, filter, SearchOption.AllDirectories)) //deploiment  
                                            deploy(src_file_path, Path.Join(dst_dir_path, src_file_path[src_path.Length..]));

                                        dst_column = dst_column.NextMatch();
                                    }
                                else LOG.Warning("Path {SrcPath} at line: {Index2Line} in `src` column does not exists. Skipped", src_path, index2line(line_end_index));

                                if (!(src_column = src_column.NextMatch()).Success || next_line_end() < src_column.Index) break;
                            }

                            sections.Clear();
                            match = match.NextMatch(); //to next execute and deploy table
                        }
                    }
                }
            }
        }
    }
}