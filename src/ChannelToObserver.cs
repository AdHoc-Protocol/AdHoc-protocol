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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json; // <-- ADDED for StringBuilder and Encoding
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using org.unirail.Agent;
using org.unirail.ObserverCommunication;
using static org.unirail.Agent.AdHocProtocol.LayoutFile_;
using static org.unirail.Agent.AdHocProtocol.Observer_;
using static org.unirail.ProjectImpl;
using Type = org.unirail.Agent.AdHocProtocol.Item.Type;

namespace org.unirail
{
    public class ChannelToObserver
    {
        static readonly string DiagramData = AdHocAgent.destination_dir_path;
        public static readonly string confirmed_layout = Path.Combine(DiagramData, "layout");
        public static readonly string unsaved_layout = Path.Combine(DiagramData, "unsaved", "layout");

        static DateTime projectSentTime = DateTime.MinValue;

        static ChannelToObserver()
        {
            //request to send updated Project pack or Up_to_date if data is not changed
            Up_to_date.OnReceived_via_ObserverCommunication_at_Operate.handlers += (pack, context, transmitter) =>
                                                                                   {
                                                                                       try
                                                                                       {
                                                                                           if (refresh(projectSentTime)) //request if the project is updated on the time
                                                                                           {
                                                                                               transmitter.send(root_project, context); // reply with updated project
                                                                                               projectSentTime = DateTime.Now;          // on this channel (connection)
                                                                                           }
                                                                                           else transmitter.send(new Up_to_date(), context); //nothing update notitication
                                                                                       }
                                                                                       catch (Exception e)
                                                                                       {
                                                                                           AdHocAgent.LOG.Error(e.ToString());
                                                                                           projectSentTime = DateTime.MinValue;                               //Sets the processing time moment to a value that guarantees the project will be uploaded on the next request.
                                                                                           transmitter.send(new Up_to_date { info = e.ToString() }, context); //send error message
                                                                                       }
                                                                                   };
            Show_Code.OnReceived_via_ObserverCommunication_at_Operate.handlers += (pack, context) =>
                                                                                  {
                                                                                      HasDocs item = pack.tYpe switch
                                                                                      {
                                                                                          Type.Project => root_project,
                                                                                          Type.Host => root_project.hosts[pack.idx],
                                                                                          Type.Pack => root_project.packs[pack.idx],
                                                                                          Type.Field => root_project.fields[pack.idx],
                                                                                          Type.Constant => root_project.constant_fields[pack.idx],
                                                                                          Type.Channel => root_project.channels[pack.idx],
                                                                                          Type.Stage => root_project.channels[pack.idx],
                                                                                          _ => throw new Exception("Unknown entity type")
                                                                                      };


                                                                                      var file_path = item.project.file_path;
                                                                                      var char_pos = item.char_in_source_code;
                                                                                      new StreamReader(file_path).Close();

                                                                                      var ms = br.Matches(new StreamReader(file_path).ReadToEnd()[..char_pos]);
                                                                                      var line = ms.Count + 1;
                                                                                      var last = ms[line - 2];
                                                                                      char_pos -= last.Index + last.Length;

                                                                                      var showCodeExe = AdHocAgent.app_props["show_code_exe"].AsString.Value;

                                                                                      var showCodeArgs = AdHocAgent.app_props["show_code_args"].AsString.Value
                                                                                                                   .Replace("<path to file>", $"\"{file_path}\"")
                                                                                                                   .Replace("<line number>", line.ToString())
                                                                                                                   .Replace("<char number>", char_pos.ToString());

                                                                                      try
                                                                                      {
                                                                                          Process.Start(new ProcessStartInfo
                                                                                          {
                                                                                              UseShellExecute = true,
                                                                                              FileName = showCodeExe,
                                                                                              Arguments = showCodeArgs
                                                                                          });
                                                                                      }
                                                                                      catch (Exception e) { AdHocAgent.LOG.Error("Show code command  " + showCodeExe + $" {showCodeArgs} error." + e); }
                                                                                  };
        }


        class WebSocket : AdHoc.Channel.External
        {
            public int ReceiveTimeout { get; set; }
            public int TransmitTimeout { get; set; }
            public AdHoc.Channel.Internal Internal { get; set; }

            public void CloseAndDispose() { }
            public void Abort() { }
            public void Close() { }

            public static async Task RunAsync(HttpListenerContext ctx)
            {
                var ws = (await ctx.AcceptWebSocketAsync(null)).WebSocket;
                if (ws.State != WebSocketState.Open) AdHocAgent.exit($"Connection issue detected with Observer at {ctx.Request.RemoteEndPoint.Address}");

                AdHocAgent.LOG.Information($"Observer connected from {ctx.Request.RemoteEndPoint.Address}");


                if (Layout.layout_INFO_bytes != null)
                {
                    AdHocAgent.LOG.Information($"Upload layout file");
                    await ws.SendAsync(Layout.layout_INFO_bytes, WebSocketMessageType.Binary, true, CancellationToken.None);
                }


                var snd_buff = new byte[1024];
                var channel = new Channel(new WebSocket());
                channel.transmitter.subscribeOnNewBytesToTransmitArrive(async src =>
                                                                        {
                                                                            for (int len; 0 < (len = src.Read(snd_buff, 0, snd_buff.Length));)
                                                                                await ws.SendAsync(new ReadOnlyMemory<byte>(snd_buff, 0, len), WebSocketMessageType.Binary, true, CancellationToken.None);
                                                                        });

                Stages.O.transmitter.send(root_project, channel.context(0));

                for (var rsv_buff = new byte[1024]; ;)
                {
                    var response = await ws.ReceiveAsync(rsv_buff, CancellationToken.None);
                    if (response.MessageType == WebSocketMessageType.Close) break;

                    channel.receiver.Write(rsv_buff, 0, response.Count);
                }

                await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }

        public static async Task Start(ushort port = 4321)
        {
            init(); //for preliminary testing purposes

            Layout.update(); //read saved layout info

            var listener = new HttpListener();
            var url = $"http://localhost:{port}/";
            listener.Prefixes.Add(url);
            listener.IgnoreWriteExceptions = true;
            listener.Start();

            AdHocAgent.LOG.Information("Waiting for browser connection on {Url}...", url);

            while (true)
            {
                var ctx = await listener.GetContextAsync();

                // Handle WebSocket requests separately and exit the loop iteration early.
                // This prevents the finally block from interfering with the WebSocket connection.
                if (ctx.Request.IsWebSocketRequest)
                {
                    _ = WebSocket.RunAsync(ctx);
                    continue; // Skip the rest of the HTTP handling logic
                }

                var request = ctx.Request;
                var response = ctx.Response;

                try
                {
                    // --- HTTP REQUEST ROUTER ---
                    switch (request.HttpMethod)
                    {
                        case "GET" when request.Url!.AbsolutePath.StartsWith("/stickers/"):

                            await HandleLoadStickersAsync(response, request.Url.Segments[2].TrimEnd('/'));
                            continue;

                        case "POST":
                            switch (request.Url.AbsolutePath)
                            {
                                case "/confirmed_layout":
                                    if (request.ContentLength64 == 0) AdHocAgent.LOG.Warning("Layout info is empty");
                                    else SaveLayout(confirmed_layout);
                                    SetSuccessResponse(response, HttpStatusCode.OK);
                                    continue;

                                case "/crash_layout":
                                    if (request.ContentLength64 == 0) AdHocAgent.LOG.Warning("Layout info is empty");
                                    else SaveLayout(unsaved_layout);
                                    SetSuccessResponse(response, HttpStatusCode.OK);
                                    continue;

                                default:
                                    var pathSegments = request.Url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                                    if (2 <= pathSegments.Length) // Sticker save: /owner/stickers or /crash/owner/stickers
                                    {
                                        await HandleSaveStickerAsync(request, pathSegments);
                                        SetSuccessResponse(response, HttpStatusCode.Created);
                                        continue;
                                    }

                                    break;
                            }

                            break;
                    }

                    var filename = request.Url!.AbsolutePath.TrimStart('/');
                    if (string.IsNullOrEmpty(filename)) filename = "index.html";

                    try
                    {
                        await using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AdHocAgent.Observer." + filename);
                        response.ContentType = Path.GetExtension(filename) switch
                        {
                            ".css" => "text/css",
                            ".html" => "text/html",
                            ".js" => "application/javascript",
                            ".png" => "image/png",
                            ".jpg" => "image/jpeg",
                            ".ico" => "image/x-icon",
                            _ => "application/octet-stream"
                        };
                        response.ContentLength64 = stream.Length;
                        response.AddHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");

                        await stream.CopyToAsync(response.OutputStream);
                        response.StatusCode = (int)HttpStatusCode.OK;
                    }
                    catch (FileNotFoundException)
                    {
                        AdHocAgent.LOG.Error("Static file not found: {filename}", filename);
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                }
                catch (Exception ex)
                {
                    AdHocAgent.LOG.Error("Error processing HTTP request: {error}", ex.ToString());

                    // Attempt to set the error status code. If headers are already sent, this will fail, and we'll ignore the failure.
                    try { response.StatusCode = (int)HttpStatusCode.InternalServerError; }
                    catch (Exception innerEx) { AdHocAgent.LOG.Warning("Could not set 500 status code on the response, as it has likely already started sending. Inner exception: {innerEx}", innerEx.Message); }
                }
                finally { response.Close(); }

                continue;

                void SaveLayout(string destinationPath)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
                    fs.Write(Layout.layout_UID_bytes);
                    request.InputStream.CopyTo(fs);
                    AdHocAgent.LOG.Information("Layout information saved to {path}", destinationPath);
                    fs.Close();
                    Layout.update(); // Re-read the new layout into memory
                }
            }
        }

        record StickerData(string name, string content);
        // ====================================================================
        // --- Sticker and Layout Helper Methods ---
        // ====================================================================

        /// <summary>
        /// Handles a POST request to save a sticker's content and metadata to a file.
        /// The path determines the owner and sticker name.
        /// </summary>
        static async Task HandleSaveStickerAsync(HttpListenerRequest request, string[] path)
        {
            // URL will be "/save_stickers/{owner}" or "/crash_stickers/{owner}"
            // path[0] will be "save_stickers" or "crash_stickers"
            var owner = path[1];

            // Read the entire request body which contains the JSON payload
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var jsonPayload = await reader.ReadToEndAsync();

            var dst_path = Path.Combine(Path.GetDirectoryName(path[0].StartsWith("crash") ?
                                                                  unsaved_layout :
                                                                  confirmed_layout)!
                                      , owner);

            if (Directory.Exists(dst_path))
                Directory.Delete(dst_path, true);

            Directory.CreateDirectory(dst_path);

            foreach (var sticker in JsonSerializer.Deserialize<List<StickerData>>(jsonPayload)!)
                await File.WriteAllTextAsync(Path.Combine(dst_path, $"{sticker.name}.html"), sticker.content);

            AdHocAgent.LOG.Information("Saved {owner} stickers to {path}", owner, dst_path);
        }

        /// <summary>
        /// Handles a GET request to load all saved stickers. It scans the storage directory,
        /// and for each sticker file, it calls a helper to parse and append its JSON representation
        /// to a StringBuilder. This avoids reflection-based serialization for performance.
        /// </summary>
        static async Task HandleLoadStickersAsync(HttpListenerResponse response, string owner)
        {
            var stickersList = new List<object>();
            var stickersDir = Path.Combine(DiagramData, owner);

            if (Directory.Exists(stickersDir))
                foreach (var file in Directory.EnumerateFiles(stickersDir, "*.html"))
                    stickersList.Add(new { name = Path.GetFileNameWithoutExtension(file), html = await File.ReadAllTextAsync(file) });

            var jsonString = JsonSerializer.Serialize(stickersList);

            response.ContentType = "application/json; charset=utf-8";
            response.StatusCode = (int)HttpStatusCode.OK;
            var jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            response.ContentLength64 = jsonBytes.Length;
            await response.OutputStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
        }


        /// <summary>
        /// Sets a standard success status code on the HTTP response.
        /// </summary>
        static void SetSuccessResponse(HttpListenerResponse response, HttpStatusCode statusCode) => response.StatusCode = (int)statusCode;


        static readonly Regex br = new(@"\r\n|\r|\n", RegexOptions.Multiline);
    }

    class Layout : AdHoc.Channel.External
    {
        public static byte[]? layout_UID_bytes;
        public static byte[]? layout_INFO_bytes;

        public AdHoc.Channel.Internal Internal
        {
            get => throw new NotImplementedException();
            set
            {
                UID? layout_UID = null;
                Info? layout_INFO = null;


                var buffer = new byte[1024];

                #region read saved layout
                if (File.Exists(ChannelToObserver.confirmed_layout)) //layout file exists
                {
                    AdHocAgent.LOG.Information("Found {layout_file} layout file.", ChannelToObserver.confirmed_layout);

                    //read layout file bytes.
                    //the result will be out to the following event handlers

                    Action<UID, SaveLayout.Context, SaveLayout.Stages.Start.Transmitter> x = (pack, context, transmitter) => layout_UID = pack;
                    UID.OnReceived_via_SaveLayout_at_Start.handlers += x;

                    Action<Info, SaveLayout.Context, SaveLayout.Stages.Start.Transmitter> y = (pack, context, transmitter) => layout_INFO = pack;
                    Info.OnReceived_via_SaveLayout_at_Start.handlers += y;

                    using var layout_file = new FileStream(ChannelToObserver.confirmed_layout, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                    var len = layout_file.Read(buffer, 0, buffer.Length);
                    do value.BytesDst!.Write(buffer, 0, len);
                    while (0 < (len = layout_file.Read(buffer, 0, buffer.Length)));
                    layout_file.Close();
                    UID.OnReceived_via_SaveLayout_at_Start.handlers -= x;
                    Info.OnReceived_via_SaveLayout_at_Start.handlers -= y;
                }
                #endregion

                var project = root_project;


                var packs = project.packs // Not every pack in project.packs has XY position information.
                                          // Exclude the following packs:
                                          // - Packs that serve solely as attributes of channels or stages.
                                   .Where(pack =>
                                          {
                                              for (Entity? e = pack; (e = e.parent_artificial ?? e.parent_by_source_code) != null;)
                                                  if (e is ChannelImpl or ChannelImpl.StageImpl)
                                                      return false;

                                              return true;
                                          })
                                   .ToArray();


                foreach (var host in root_project.hosts)
                    host.uid = host.project.uid << 8 | // Project UID (1 byte)
                               host.uid;               // Host UID (1 byte)

                foreach (var pack in root_project.packs)
                    pack.uid = pack.project.uid << 16 | // Project UID (1 byte)
                               pack.uid;                // Pack UID (2 bytes)

                foreach (var channel in root_project.channels)
                {
                    channel.uid = channel.project.uid << 8 | // Project UID (1 byte)
                                  channel.uid;               // Channel UID (1 byte)
                    foreach (var st in channel.stages)
                    {
                        st.uid = st.project.uid << 24 | // Project UID (1 byte)
                                 channel.uid << 16 | // Channel UID (1 byte)
                                 st.uid;                // Stage UID (2 bytes)
                        foreach (var br in st.branchesL)
                            br.uid =
                                st.uid << 24 | // Stage UID (4 bytes)
                                1UL << 16 | // Side marker: 1 for Left (1 byte)
                                br.uid;        // Branch UID (2 bytes)
                        foreach (var br in st.branchesR)
                            br.uid =
                                st.uid << 24 | // Stage UID (4 bytes)
                                               //0           // Side marker: 0 for Right (1 byte, implicit)
                                br.uid; // Branch UID (2 bytes)
                    }
                }


                List<byte> bytes = [];

                value.BytesSrc!.subscribeOnNewBytesToTransmitArrive(BytesSrc => //subscribe on new bytes available in a BytesSrc
                                                                    {
                                                                        for (int len; 0 < (len = BytesSrc.Read(buffer, 0, buffer.Length));) //pull bytes
                                                                            bytes.AddRange(buffer.Take(len));                                //push to 'bytes' array
                                                                    });


                if (layout_UID == null) // layout file does not exist
                {
                    layout_UID = new UID();

                    layout_UID!._packs = packs.Select(pack => pack.uid).ToArray();
                    layout_UID!._hosts = project.hosts.Select(host => host.uid).ToArray();
                    layout_UID._branches = project.channels.SelectMany(ch => ch.stages.SelectMany(st => st.branchesL.Select(br => br.uid).Concat(st.branchesR.Select(br => br.uid))))
                                                  .ToArray();

                    SaveLayout.Stages.O.transmitter.send(layout_UID, ((SaveLayout.Channel)value!).context(0)); // send collected pack, to get its binary representation
                    layout_UID_bytes = bytes.ToArray();                                                        // in the `bytes` array

                    //based on project data can build only layout_UID_bytes.
                    return; // no any XY info to upload to the Observer
                }


                var tmp = new List<Info.XY>();

                void restore_layout_Info(ulong[] layout_uid, ulong[] project_uid, ref Info.XY[] xy)
                {
                    var k = 0;
                    if (layout_uid.Length == project_uid.Length)
                    {
                        for (; k < layout_uid!.Length; k++)      // Iterate through the layout_uids array; standard mode assumes arrays must be identical in content and order.
                            if (project_uid[k] != layout_uid[k]) // Check if the project_uids and layout_uids arrays differ at any index, indicating the project was edited (entities added/removed).
                                goto update;
                        return;
                    }

                update:

                    tmp.Clear();
                    tmp.AddRange(xy.Take(k));

                    foreach (var uid in project_uid.Skip(k)) //xy by ref
                    {
                        var layout_idx = Array.IndexOf(layout_uid!, uid);

                        tmp.Add(layout_idx == -1 ?
                                    new Info.XY { x = int.MinValue } : //empty XY
                                    xy[layout_idx]);
                    }

                    xy = tmp.ToArray();
                }

                restore_layout_Info(layout_UID._packs!, layout_UID._packs = packs.Select(pack => pack.uid).ToArray(), ref layout_INFO!._packs!);
                restore_layout_Info(layout_UID._hosts!, layout_UID._hosts = project.hosts.Select(host => host.uid).ToArray(), ref layout_INFO!._hosts!);
                restore_layout_Info(layout_UID._branches!, layout_UID._branches = project.channels.SelectMany(ch => ch.stages.SelectMany(st => st.branchesL.Select(br => br.uid).Concat(st.branchesR.Select(br => br.uid))))
                                                                                                                                                                             .ToArray(), ref layout_INFO!._branches!);
                //now in the memory having consistent layout_UID

                // Identify and remove UIDs belonging to orphaned projects.

                SaveLayout.Stages.O.transmitter.send(layout_UID, ((SaveLayout.Channel)value).context(0)); // send collected pack,
                layout_UID_bytes = bytes.ToArray();                                                       // to get its bytes in the `bytes` array

                bytes.Clear();

                SaveLayout.Stages.O.transmitter.send(layout_INFO, ((SaveLayout.Channel)value!).context(0)); // send collected pack,
                layout_INFO_bytes = bytes.ToArray();                                                        // to get its bytes in the `bytes` array
            }
        }

        public int ReceiveTimeout { get; set; }
        public int TransmitTimeout { get; set; }
        public void CloseAndDispose() { }
        public void Abort() { }
        public void Close() { }

        public static void update() { _ = new SaveLayout.Channel(new Layout()); }
    }
}