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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using org.unirail.Agent;
using org.unirail.Agent.AdHocProtocol.Observer_;
using Type = org.unirail.Agent.AdHocProtocol.Observer_.Entity.Type;

namespace org.unirail
{
    public interface ChannelToObserver
    {
        static ObserverCommunication.Transmitter transmitter = new();
        static ObserverCommunication.Receiver receiver = new();

        //Sets the processing time of this channel (connection) to a value that guarantees the project will be uploaded on the next request.
        static DateTime projectSentTime = DateTime.MinValue; //

        static void send_project()
        {
            try
            {
                if (ProjectImpl.refresh(projectSentTime))
                {
                    transmitter.send(ProjectImpl.projects_root);      // reply with updated information
                    projectSentTime = DateTime.Now; // on this channel (connection)
                }
                else
                    transmitter.send(new Up_to_date()); //nothing update notitication
            }
            catch (Exception e)
            {
                AdHocAgent.LOG.Error(e.ToString());
                //Sets the processing time of this channel (connection) to a value that guarantees the project will be uploaded on the next request.
                projectSentTime = DateTime.MinValue;
                transmitter.send(new Up_to_date { info = e.ToString() });
            }
        }

        //request to send updated Project pack or Up_to_date  if data is not changed
        public void Received(ObserverCommunication.Receiver via, Up_to_date pack) => send_project();

        public void Received(ObserverCommunication.Receiver via, Show_Code pack)
        {
            var file_path = "";
            var line = 0;
            var char_pos = 0;
            var src = "";
            HasDocs item = pack.tYpe switch
            {
                Type.Project => ProjectImpl.projects_root,
                Type.Host => ProjectImpl.projects_root.hosts[pack.idx],
                Type.Pack => ProjectImpl.projects_root.packs[pack.idx],
                Type.Field => ProjectImpl.projects_root.fields[pack.idx],
                Type.Channel => ProjectImpl.projects_root.channels[pack.idx],
                Type.Stage => ProjectImpl.projects_root.channels[pack.idx],
                _ => throw new Exception("Unknown entity type")
            };


            file_path = item.project.file_path;
            char_pos = item.char_in_source_code;
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
                                           .Replace("<line number>", line.ToString())
                                           .Replace("<char number>", char_pos.ToString());

            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = show_code_exe,
                Arguments = show_code_args
            });
        }


        private static async Task StartWebSocketAsync(HttpListenerContext ctx)
        {
            var ws = (await ctx.AcceptWebSocketAsync(null)).WebSocket;
            if (ws.State != WebSocketState.Open) AdHocAgent.exit("Observer Connection Issue");

            AdHocAgent.LOG.Information("Observer connected");

            await using (var fs = File.OpenRead(AdHocAgent.layout.Value))
                if (UID_Impl.data_bytes < fs.Length)
                {
                    AdHocAgent.LOG.Information("Found and send {RawFilesDirPath} to the observer", AdHocAgent.layout.Value);
                    var mem = new byte[fs.Length - UID_Impl.data_bytes];
                    fs.Position = UID_Impl.data_bytes;
                    fs.Read(mem); // read layout binary from file

                    await ws.SendAsync(mem, WebSocketMessageType.Binary, true, CancellationToken.None); //write layout to observer
                }


            var snd_buff = new byte[1024];
            transmitter.subscribeOnNewBytesToTransmitArrive(async src =>
                                                            {
                                                                for (int len; 0 < (len = src.Read(snd_buff, 0, snd_buff.Length));)
                                                                    await ws.SendAsync(snd_buff[..len], WebSocketMessageType.Binary, true, CancellationToken.None);
                                                            });
            send_project();

            for (var rsv_buff = new byte[1024]; ;)
            {
                var ret = await ws.ReceiveAsync(rsv_buff, CancellationToken.None); //block here

                if (ret.MessageType == WebSocketMessageType.Close) break;

                receiver.Write(rsv_buff, 0, ret.Count);
            }

            await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

            transmitter.Close();
            receiver.Close();
        }


        public static async Task Start(ushort port = 4321)
        {
            ProjectImpl.init(); //for preliminary testing purposes

            var listener = new HttpListener();
            var url = $"http://localhost:{port}/";
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

                async void save(string dst)
                {
                    await using var fs = new FileStream(dst, FileMode.Open, FileAccess.ReadWrite);
                    fs.Position = UID_Impl.data_bytes; // Seek to the position where you want to start writing (Info.data_bytes)
                    await ctx.Request.InputStream.CopyToAsync(fs);
                    fs.SetLength(fs.Position); // Truncate the file at the current position, removing any leftover bytes from the original file
                }

                if (ctx.Request.IsWebSocketRequest) StartWebSocketAsync(ctx);
                else
                {
                    var filename = ctx.Request.Url!.AbsolutePath[1..];

                    switch (filename)
                    {
                        case "":
                            //Sets the processing time of this channel (connection) to a value that guarantees the project will be uploaded on the next request.
                            projectSentTime = DateTime.MinValue;
                            filename = "index.html";
                            break;

                        case "crash_layout":

                            if (ctx.Request.ContentLength64 == 0) AdHocAgent.LOG.Warning("Layout info is empty");
                            else
                            {
                                var unsaved_layout = AdHocAgent.raw_files_dir_path + ".unsaved_layout";

                                File.Copy(AdHocAgent.layout.Value, unsaved_layout, true);
                                save(unsaved_layout);
                            }

                            continue;
                        case "confirmed_layout":

                            if (ctx.Request.ContentLength64 == 0) AdHocAgent.LOG.Warning("Layout info is empty");
                            else save(AdHocAgent.layout.Value);

                            continue;
                    }
                    await using( var stream = new FileStream(@"D:\AdHoc\Observer\Observer\" + filename, FileMode.Open, FileAccess.Read) ) // System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AdHocAgent.Observer." + filename))
                    {
                        if (stream == null)
                        {
                            AdHocAgent.LOG.Error("The file {filename} embedded as an assembly resource could not be found.", filename);
                            ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            return;
                        }

                        ctx.Response.ContentType = Path.GetExtension(filename) switch
                        {
                            ".css" => "text/css",
                            ".htm" => "text/html",
                            ".html" => "text/html",
                            ".gif" => "image/gif",
                            ".ico" => "image/x-icon",
                            ".jpeg" => "image/jpeg",
                            ".jpg" => "image/jpeg",
                            ".png" => "image/png",
                            ".wbmp" => "image/vnd.wap.wbmp",
                            ".js" => "application/x-javascript",
                            ".xml" => "text/xml",
                            ".zip" => "application/zip",
                            ".jar" => "application/java-archive",
                            _ => "application/octet-stream"
                        };
                        ctx.Response.ContentLength64 = stream.Length;
                        // Ctrl + Shift + R in the browser To ensure that the browser never uses cached resources
                        ctx.Response.AddHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
                        ctx.Response.AddHeader("Pragma", "no-cache");
                        ctx.Response.AddHeader("Expires", "0");
                        ctx.Response.Headers["ETag"] = Guid.NewGuid().ToString();
                        ctx.Response.Headers["Last-Modified"] = DateTime.UtcNow.ToString("r");

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