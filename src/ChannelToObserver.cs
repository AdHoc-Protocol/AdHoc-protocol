using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using org.unirail.Agent;
using org.unirail.Agent.ObserverToAgent;

namespace org.unirail
{
    public class ChannelToObserver : ObserverCommunication, ObserverCommunication.Receivable.Listener
    {
        private ProjectImpl? project;

        void send_project()
        {
            try
            {
                if (project == null) send(project = ProjectImpl.init());
                else
                {
                    var prj = project.refresh();
                    if (prj == null) send(new Up_to_date()); //nothing update notitication
                    else send(project = prj);                // reply with updated information
                }
            }
            catch (Exception e)
            {
                project = null;
                send(new Up_to_date { info = e.ToString() });
            }
        }

        private ChannelToObserver() { onReceiveListener = this; }

        public void Received(ObserverCommunication via, Layout data) { }

        //request to send updated Project pack or Up_to_date  if data is not changed
        public void Received(ObserverCommunication via, Up_to_date pack) => send_project();

        public void Received(ObserverCommunication via, Show_Code entity)
        {
            var file_path = "";
            var line      = 0;
            var char_pos  = 0;
            var src       = "";
            HasDocs item = entity.Type switch
                           {
                               Agent.Entity.Type.Project => project,
                               Agent.Entity.Type.Host    => project.hosts[(int)entity.uid],
                               Agent.Entity.Type.Port    => project.ports[(int)entity.uid],
                               Agent.Entity.Type.Pack    => project.packs[(int)entity.uid],
                               Agent.Entity.Type.Field   => project.fields[(int)entity.uid],
                               Agent.Entity.Type.Channel => project.channels[(int)entity.uid],
                               _                         => throw new Exception("Unknown entity type")
                           };

            file_path = item.project.file_path;
            char_pos  = item.char_in_source_code;
            StreamReader file = new(file_path);
            src = file.ReadToEnd();
            file.Close();

            var ms = br.Matches(src[..char_pos]);
            line = ms.Count + 1;
            var last = ms[line - 2];
            char_pos -= last.Index + last.Length;

            var show_code_exe = AdHocAgent.app_props["show_code_exe"].AsString.Value;

            var show_code_args = AdHocAgent.app_props["show_code_args"].AsString.Value
                                           .Replace("<path to file>", $"\"{file_path}\"")
                                           .Replace("<line number>",  line.ToString())
                                           .Replace("<char number>",  char_pos.ToString());

            Process.Start(new ProcessStartInfo
                          {
                              UseShellExecute = true,
                              FileName        = show_code_exe,
                              Arguments       = show_code_args
                          });
        }


        private static async Task process(HttpListenerContext ctx)
        {
            var sendBuffer    = new byte[1024];
            var receiveBuffer = new byte[1024];
            var ws_ctx        = await ctx.AcceptWebSocketAsync(null);

            var channel = new ChannelToObserver();


            while (true)
            {
                switch (ws_ctx.WebSocket.State)
                {
                    case WebSocketState.Open:

#region very begining, transmitter is not binded to websocket
                        if (channel.ext_src.token() != ws_ctx) //just opened connection
                        {
                            async void sending(AdHoc.EXT.BytesSrc src)
                            {
                                for (int len; 0 < (len = src.Read(sendBuffer, 0, sendBuffer.Length));)
                                    await ws_ctx.WebSocket.SendAsync(sendBuffer[..len], WebSocketMessageType.Binary, true, CancellationToken.None);
                            }

                            channel.ext_src.subscribe(sending, ws_ctx); //bind transmitter >>> websocket

                            AdHocAgent.LOG.Information("Using {RawFilesDirPath}.layout", AdHocAgent.raw_files_dir_path);
                            if (File.Exists(AdHocAgent.raw_files_dir_path + ".layout")) //present layout file
                            {
                                var mem = new MemoryStream();

                                await using (var fs = File.OpenRead(AdHocAgent.raw_files_dir_path + ".layout")) await fs.CopyToAsync(mem); // read layout binary from file

                                await ws_ctx.WebSocket.SendAsync(mem.ToArray(), WebSocketMessageType.Binary, true, CancellationToken.None); //write layout to observer
                            }

                            channel.send_project();
                        }
#endregion
                        var ret = await ws_ctx.WebSocket.ReceiveAsync(receiveBuffer, CancellationToken.None); //block

                        if (ret.MessageType == WebSocketMessageType.Close)
                        {
                            await ws_ctx.WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                            await ws_ctx.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                            return;
                        }

                        channel.ext_dst.Write(receiveBuffer, 0, ret.Count);
                        break;
                    case WebSocketState.None:
                        AdHocAgent.LOG.Information("WebSocketState.None");
                        continue;
                    case WebSocketState.Connecting:
                        AdHocAgent.LOG.Information("WebSocketState.Connecting");
                        continue;
                    case WebSocketState.CloseSent:
                        AdHocAgent.LOG.Information("WebSocketState.CloseSent");
                        return;
                    case WebSocketState.CloseReceived:
                        AdHocAgent.LOG.Information("WebSocketState.None");
                        return;
                    case WebSocketState.Closed:
                        AdHocAgent.LOG.Information("WebSocketState.CloseReceived");
                        return;
                    case WebSocketState.Aborted:
                        AdHocAgent.LOG.Information("WebSocketState.Aborted");
                        return;
                    default:
                        return;
                }
            }
        }


        public static async Task start(ushort port = 4321)
        {
            ProjectImpl.init();//for preliminary testing purpose
            var listener = new HttpListener();
            var url      = $"http://localhost:{port}/";
            listener.Prefixes.Add(url);
            listener.IgnoreWriteExceptions = true; //ignore "The specified network name is no longer available" 
            listener.Start();
            try
            {
                //run browser
                //Process.Start(new ProcessStartInfo() { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex) { AdHocAgent.exit($"Tried to open link {url} to Visualizer user interface but got an error {ex}."); }

            AdHocAgent.LOG.Information("Waiting for browser connection on {Url}...", url);


            while (true)
            {
                var ctx = await listener.GetContextAsync(); //block


                if (ctx != null && ctx.Request.IsWebSocketRequest) process(ctx);

                else
                {
                    var filename = ctx.Request.Url.AbsolutePath;
                    Debug.Print(filename);

                    switch (filename)
                    {
                        case "/":
                            filename = "index.html";
                            break;

                        case "/crash_layout":

                            if (ctx.Request.ContentLength64 == 0) AdHocAgent.LOG.Warning("Layout info is empty");
                            else
                                await using (var fs = File.Open(AdHocAgent.raw_files_dir_path + ".crash_layout", FileMode.Create, FileAccess.Write))
                                    await ctx.Request.InputStream.CopyToAsync(fs);

                            continue;
                        case "/confirmed_layout":

                            if (ctx.Request.ContentLength64 == 0) AdHocAgent.LOG.Warning("Layout info is empty");
                            else
                                await using (var fs = File.Open(AdHocAgent.raw_files_dir_path + ".layout", FileMode.Create, FileAccess.Write))
                                    await ctx.Request.InputStream.CopyToAsync(fs);

                            continue;
                    }


                    // filename = "AdHocAgent.Observer." + filename;
                    //  using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filename))

                    filename = Path.Join("D:/AdHoc/Observer/Observer", filename);
                    if (!File.Exists(filename))
                    {
                        AdHocAgent.LOG.Error($"File {filename} in resource is not found.");
                        ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                    else
                        using (var stream = File.Open(filename, FileMode.Open)) //Assembly.GetExecutingAssembly().GetManifestResourceStream(filename))
                        {
                            ctx.Response.ContentType = Path.GetExtension(filename) switch
                                                       {
                                                           ".css"  => "text/css",
                                                           ".htm"  => "text/html",
                                                           ".html" => "text/html",
                                                           ".gif"  => "image/gif",
                                                           ".ico"  => "image/x-icon",
                                                           ".jpeg" => "image/jpeg",
                                                           ".jpg"  => "image/jpeg",
                                                           ".png"  => "image/png",
                                                           ".wbmp" => "image/vnd.wap.wbmp",
                                                           ".js"   => "application/x-javascript",
                                                           ".xml"  => "text/xml",
                                                           ".zip"  => "application/zip",
                                                           ".jar"  => "application/java-archive",
                                                           _       => "application/octet-stream"
                                                       };
                            ctx.Response.ContentLength64 = stream.Length;
                            ctx.Response.AddHeader("Date",          DateTime.Now.ToString("r"));
                            ctx.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));

                            try //The specified network name is no longer available.
                            {
                                await stream.CopyToAsync(ctx.Response.OutputStream);
                            }
                            catch (Exception e) { continue; }

                            stream.Close();
                            ctx.Response.OutputStream.Flush();

                            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                        }

                    ctx.Response.OutputStream.Close();
                }
            }
        }

        private static readonly Regex br = new(@"\r\n|\r|\n", RegexOptions.Multiline);
    }
}