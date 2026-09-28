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
using org.unirail.ObserverCommunication;
using static org.unirail.Agent.AdHocProtocol.LayoutFile_;
using static org.unirail.Agent.AdHocProtocol.Observer_;
using static org.unirail.ProjectImpl;
using Type = org.unirail.Agent.AdHocProtocol.Item.Type;

namespace org.unirail{
    public class ToObserver{
        static readonly        string DiagramData      = AdHocAgent.destination_dir_path;
        public static readonly string confirmed_layout = Path.Combine(DiagramData, "layout");
        public static readonly string unsaved_layout   = Path.Combine(DiagramData, "unsaved", "layout");

        static DateTime projectSentTime = DateTimeOffset.MinValue.LocalDateTime;

        /// <summary>
        /// A project-wide flat registry of all logical actors across all connections, in the order
        /// the Observer flattens them: connections → actors. `Show_Code(Type.Actor, idx)` indexes it.
        /// </summary>
        /// <remarks>
        /// DERIVED, never cached: `refresh()` re-runs `init()` and replaces `projects[0]`, so a snapshot
        /// taken at startup would keep resolving into the previous parse's object graph and open source
        /// positions from a stale file. Recomputed per lookup — Show_Code is a user gesture, not a hot path.
        /// </remarks>
        static ConnectionImpl.ActorImpl[] all_actors => root_project.connections
                                                                   .SelectMany(conn => conn.actors)
                                                                   .ToArray();

        /// <summary>
        /// A project-wide flat registry of all reachable states across all actors, in the order the
        /// Observer flattens them: connections → actors → states. `Show_Code(Type.State, idx)` indexes it.
        /// </summary>
        /// <remarks>
        /// Derived for the same reason as <see cref="all_actors"/>. Note that these positions are NOT
        /// written back onto the entities: `StateImpl.idx` is the actor-LOCAL state index that
        /// `BranchImpl._goto_state` serializes, and overwriting it would corrupt every FSM transition
        /// the Observer draws.
        /// </remarks>
        static ConnectionImpl.StateImpl[] all_states => all_actors.SelectMany(actor => actor.states).ToArray();

        static ToObserver()
        {
            //request to send updated Project pack or Up_to_date if data is not changed
            Actor0.State.Operate.OnReceiveD.AdHocProtocol_Observer_Up_to_date.handlers += (pack, conn, actor, transmitter) =>
                                                                                          {
                                                                                              try
                                                                                              {
                                                                                                  if( refresh(projectSentTime) ) //request if the project is updated on the time
                                                                                                  {
                                                                                                      transmitter.send(root_project, conn); // reply with updated project
                                                                                                      projectSentTime = DateTime.Now;       // on this connection (connection)
                                                                                                  }
                                                                                                  else transmitter.send(new Up_to_date(), conn); //nothing update notitication
                                                                                              }
                                                                                              catch( Exception e )
                                                                                              {
                                                                                                  AdHocAgent.LOG.Error(e.ToString());
                                                                                                  projectSentTime = DateTimeOffset.MinValue.LocalDateTime;        //Sets the processing time moment to a value that guarantees the project will be uploaded on the next request.
                                                                                                  transmitter.send(new Up_to_date { info = e.ToString() }, conn); //send error message
                                                                                              }
                                                                                          };
            Actor0.State.Operate.OnReceiveD.AdHocProtocol_Observer_Show_Code.handlers += (pack, conn, actor) =>
                                                                                         {
                                                                                             HasDocs item = pack.tYpe switch
                                                                                                            {
                                                                                                                Type.Project    => root_project,
                                                                                                                Type.Host       => root_project.hosts[pack.idx],
                                                                                                                Type.Pack       => root_project.packs[pack.idx],
                                                                                                                Type.Field      => root_project.fields[pack.idx],
                                                                                                                Type.Constant   => root_project.constant_fields[pack.idx],
                                                                                                                Type.Connection => root_project.connections[pack.idx],
                                                                                                                Type.Actor      => all_actors[pack.idx],
                                                                                                                Type.State      => all_states[pack.idx],
                                                                                                                _               => throw new Exception("Unknown entity type")
                                                                                                            };


                                                                                             var file_path = item.project.file_path;
                                                                                             var char_pos  = item.char_in_source_code;
                                                                                             new StreamReader(file_path).Close();

                                                                                             var ms   = br.Matches(new StreamReader(file_path).ReadToEnd()[..char_pos]);
                                                                                             var line = ms.Count + 1;
                                                                                             var last = ms[line - 2];
                                                                                             char_pos -= last.Index + last.Length;

                                                                                             var showCodeExe = AdHocAgent.app_props["show_code_exe"].AsString.Value;

                                                                                             var showCodeArgs = AdHocAgent.app_props["show_code_args"].AsString.Value
                                                                                                                          .Replace("<path to file>", $"\"{file_path}\"")
                                                                                                                          .Replace("<line number>",  line.ToString())
                                                                                                                          .Replace("<char number>",  char_pos.ToString());

                                                                                             try
                                                                                             {
                                                                                                 Process.Start(new ProcessStartInfo
                                                                                                               {
                                                                                                                   UseShellExecute = true,
                                                                                                                   FileName        = showCodeExe,
                                                                                                                   Arguments       = showCodeArgs
                                                                                                               });
                                                                                             }
                                                                                             catch( Exception e ) { AdHocAgent.LOG.Error("Show code command  " + showCodeExe + $" {showCodeArgs} error." + e); }
                                                                                         };
        }


        class WebSocket : AdHoc.Connection.External{
            readonly System.Net.WebSockets.WebSocket _ws;
            readonly byte[]                          _snd_buff = new byte[1024];

            WebSocket(System.Net.WebSockets.WebSocket ws) => _ws = ws;

            public int Id => 0;

            public int                       ReceiveTimeout    { get; set; }
            public int                       TransmitTimeout   { get; set; }
            public AdHoc.Connection.Internal Internal          { get; set; }
            public bool                      IsOpen            => _ws.State == WebSocketState.Open;
            public void                      CloseAndDispose() { }
            public void                      Abort()           { }
            public void                      Close()           { }

            public async void WakeTransmit()
            {
                if( Internal?.BytesSrc is { } src )
                    for( int len; 0 < (len = src.Read(_snd_buff, 0, _snd_buff.Length)); )
                        await _ws.SendAsync(new ReadOnlyMemory<byte>(_snd_buff, 0, len), WebSocketMessageType.Binary, true, CancellationToken.None);
            }

            public static async Task RunAsync(HttpListenerContext ctx)
            {
                var ws = (await ctx.AcceptWebSocketAsync(null)).WebSocket;
                if( ws.State != WebSocketState.Open ) AdHocAgent.exit($"Connection issue detected with Observer at {ctx.Request.RemoteEndPoint.Address}");

                AdHocAgent.LOG.Information($"Observer connected from {ctx.Request.RemoteEndPoint.Address}");


                if( Layout.layout_INFO_bytes != null )
                {
                    AdHocAgent.LOG.Information($"Upload layout file");
                    await ws.SendAsync(Layout.layout_INFO_bytes, WebSocketMessageType.Binary, true, CancellationToken.None);
                }


                var connection = new Connection();
                connection.External(new WebSocket(ws));
                //WakeTransmit override on WebSocket drives the send loop — no Wake assignment needed.

                Actor0.State.O.transmitter.send(root_project, connection);
                projectSentTime = DateTime.Now; // the Observer now holds current data; record it so the
                                                // first `Up_to_date` can answer "unchanged" instead of
                                                // re-sending the whole project unconditionally.

                for( var rsv_buff = new byte[1024];; )
                {
                    var response = await ws.ReceiveAsync(rsv_buff, CancellationToken.None);
                    if( response.MessageType == WebSocketMessageType.Close ) break;

                    connection.receiver.Write(rsv_buff, 0, response.Count);
                }

                await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }

        public static async Task Start(ushort port = 4321)
        {
            init(); //for preliminary testing purposes

            Layout.update(); //read saved layout info

            // `all_actors` / `all_states` are derived properties — nothing to build here, and nothing
            // is stamped onto the entities: the Observer sends the flat position it computed from the
            // same connections → actors → states order, so plain array indexing resolves it.

            var listener = new HttpListener();
            var url      = $"http://localhost:{port}/";
            listener.Prefixes.Add(url);
            listener.IgnoreWriteExceptions = true;
            listener.Start();

            AdHocAgent.LOG.Information("Waiting for browser connection on {Url}...", url);

            while( true )
            {
                var ctx = await listener.GetContextAsync();

                // Handle WebSocket requests separately and exit the loop iteration early.
                // This prevents the finally block from interfering with the WebSocket connection.
                if( ctx.Request.IsWebSocketRequest )
                {
                    _ = WebSocket.RunAsync(ctx);
                    continue; // Skip the rest of the HTTP handling logic
                }

                var request  = ctx.Request;
                var response = ctx.Response;

                try
                {
                    // --- HTTP REQUEST ROUTER ---
                    switch( request.HttpMethod )
                    {
                        case "GET" when request.Url!.AbsolutePath.StartsWith("/stickers/"):

                            await HandleLoadStickersAsync(response, request.Url.Segments[2].TrimEnd('/'));
                            continue;

                        case "POST":
                            switch( request.Url.AbsolutePath )
                            {
                                case "/confirmed_layout":
                                    if( request.ContentLength64 == 0 ) AdHocAgent.LOG.Warning("Layout info is empty");
                                    else SaveLayout(confirmed_layout);
                                    SetSuccessResponse(response, HttpStatusCode.OK);
                                    continue;

                                case "/crash_layout":
                                    if( request.ContentLength64 == 0 ) AdHocAgent.LOG.Warning("Layout info is empty");
                                    else SaveLayout(unsaved_layout);
                                    SetSuccessResponse(response, HttpStatusCode.OK);
                                    continue;

                                default:
                                    var pathSegments = request.Url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                                    if( 2 <= pathSegments.Length ) // Sticker save: /owner/stickers or /crash/owner/stickers
                                    {
                                        await HandleSaveStickerAsync(request, pathSegments);
                                        SetSuccessResponse(response, HttpStatusCode.Created);
                                        continue;
                                    }

                                    break;
                            }

                            break;
                    }

                    var filename                                  = request.Url!.AbsolutePath.TrimStart('/');
                    if( string.IsNullOrEmpty(filename) ) filename = "index.html";

                    try
                    {
                        await using var stream = new FileStream(@"D:\AdHoc\Observer\Observer\" + filename, FileMode.Open, FileAccess.Read); // System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AdHocAgent.Observer." + filename);
                        response.ContentType = Path.GetExtension(filename) switch
                                               {
                                                   ".css"  => "text/css",
                                                   ".html" => "text/html",
                                                   ".js"   => "application/javascript",
                                                   ".png"  => "image/png",
                                                   ".jpg"  => "image/jpeg",
                                                   ".ico"  => "image/x-icon",
                                                   _       => "application/octet-stream"
                                               };
                        response.ContentLength64 = stream.Length;
                        response.AddHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");

                        // A HEAD response carries the headers set above and nothing else. `HttpListener`
                        // caps its body at zero bytes whatever `ContentLength64` says, so copying the
                        // payload threw `ProtocolViolationException` ("Bytes to be written to the stream
                        // exceed the Content-Length bytes size specified") and the probe saw a 500 for a
                        // file that serves fine over GET.
                        if( !string.Equals(request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase) )
                            await stream.CopyToAsync(response.OutputStream);

                        response.StatusCode = (int)HttpStatusCode.OK;
                    }
                    // A missing directory is as much a "not found" as a missing file: a request for
                    // `/nope/app.js` raises DirectoryNotFoundException, which used to escape to the outer
                    // handler and answer 500 for what is plainly a 404.
                    catch( Exception e ) when( e is FileNotFoundException or DirectoryNotFoundException )
                    {
                        AdHocAgent.LOG.Error("Static file not found: {filename}", filename);
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                }
                catch( Exception ex )
                {
                    AdHocAgent.LOG.Error("Error processing HTTP request: {error}", ex.ToString());

                    // Attempt to set the error status code. If headers are already sent, this will fail, and we'll ignore the failure.
                    try { response.StatusCode = (int)HttpStatusCode.InternalServerError; }
                    catch( Exception innerEx ) { AdHocAgent.LOG.Warning("Could not set 500 status code on the response, as it has likely already started sending. Inner exception: {innerEx}", innerEx.Message); }
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
            using var reader      = new StreamReader(request.InputStream, request.ContentEncoding);
            var       jsonPayload = await reader.ReadToEndAsync();

            var dst_path = Path.Combine(Path.GetDirectoryName(path[0].StartsWith("crash") ?
                                                                  unsaved_layout :
                                                                  confirmed_layout)!
                                      , owner);

            if( Directory.Exists(dst_path) )
                Directory.Delete(dst_path, true);

            Directory.CreateDirectory(dst_path);

            foreach( var sticker in JsonSerializer.Deserialize<List<StickerData>>(jsonPayload)! )
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
            var stickersDir  = Path.Combine(DiagramData, owner);

            if( Directory.Exists(stickersDir) )
                foreach( var file in Directory.EnumerateFiles(stickersDir, "*.html") )
                    stickersList.Add(new { name = Path.GetFileNameWithoutExtension(file), html = await File.ReadAllTextAsync(file) });

            var jsonString = JsonSerializer.Serialize(stickersList);

            response.ContentType = "application/json; charset=utf-8";
            response.StatusCode  = (int)HttpStatusCode.OK;
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

    class Layout : AdHoc.Connection.External{
        public        int     Id => 0;
        public static byte[]? layout_UID_bytes;
        public static byte[]? layout_INFO_bytes;

        public AdHoc.Connection.Internal Internal
        {
            get => throw new NotImplementedException();
            set
            {
                UID?  layout_UID  = null;
                Info? layout_INFO = null;


                var buffer = new byte[1024];

#region read saved layout
                if( File.Exists(ToObserver.confirmed_layout) ) //layout file exists
                {
                    AdHocAgent.LOG.Information("Found {layout_file} layout file.", ToObserver.confirmed_layout);

                    //read layout file bytes.
                    //the result will be out to the following event handlers

                    Action<UID, SaveLayout.Connection, SaveLayout.Actor0> x = (pack, conn, actor) => layout_UID = pack;
                    SaveLayout.Actor0.Start.OnReceiveD.AdHocProtocol_LayoutFile_UID.handlers += x;

                    Action<Info, SaveLayout.Connection, SaveLayout.Actor0> y = (pack, conn, actor) => layout_INFO = pack;
                    SaveLayout.Actor0.Start.OnReceiveD.AdHocProtocol_LayoutFile_Info.handlers += y;

                    using var layout_file = new FileStream(ToObserver.confirmed_layout, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                    var       len         = layout_file.Read(buffer, 0, buffer.Length);
                    do value.BytesDst!.Write(buffer, 0, len);
                    while( 0 < (len = layout_file.Read(buffer, 0, buffer.Length)) );
                    layout_file.Close();
                    SaveLayout.Actor0.Start.OnReceiveD.AdHocProtocol_LayoutFile_UID.handlers              -= x;
                    SaveLayout.Actor0.Start.OnReceiveD.AdHocProtocol_LayoutFile_Info.handlers -= y;
                }
#endregion

                var project = root_project;


                var packs = project.packs // Not every pack in project.packs has XY position information.
                                   // Exclude the following packs:
                                   // - Packs that serve solely as attributes of connections or states.
                                   .Where(pack =>
                                          {
                                              for( HasDocs? e = pack; (e = e.parent_artificial ?? e.parent_by_source_code) != null; )
                                                  if( e is ConnectionImpl or ConnectionImpl.StateImpl )
                                                      return false;

                                              return true;
                                          })
                                   .ToArray();
                // The layout file keys each node by a number of its own across the project and everything it imports: the
                // entity's project slot (the root's is 0) and its own numbers down its chain. Computed, never written onto the
                // entities - their numbers are what the parser persists and the generator keys regions by.
                static ulong host_key(ProjectImpl.HostImpl host, ulong uid) => host.project.uid << 8 | uid;  // Project(8) + Host(8)
                static ulong pack_key(ProjectImpl.HostImpl.PackImpl pack) => pack.project.uid << 16 | pack.uid; // Project(8) + Pack(16)

                // Branch: Project(8) + Connection(8) + Actor(8) + State(16) + Side(8) + Branch(16) = 64 bits
                static IEnumerable<ulong> branch_keys(ProjectImpl project) =>
                    from connection in project.connections
                    let c = connection.project.uid << 8 | connection.uid
                    from actor in connection.actors
                    let a = c << 8 | actor.uid
                    from st in actor.states
                    let s = a << 16 | st.uid
                    from key in st.branchesL.Select(br => s << 24 | 1UL << 16 | br.uid) // L then R: the Observer's order
                                  .Concat(st.branchesR.Select(br => s << 24 | br.uid))
                    select key;

                ulong[] host_keys() => root_project.hosts.Select(h => host_key(h, h.uid)).ToArray();

                // A host renumbered because an import took its number keeps its place: its old key finds it
                var aliases = root_project.hosts.Select(h => (h, was: h.project.was(h)))
                                          .Where(p => p.was != ulong.MaxValue)
                                          .ToDictionary(p => host_key(p.h, p.h.uid), p => host_key(p.h, p.was));

                List<byte> bytes = [];
                var        bytesSrc = value.BytesSrc;

                void drain()
                {
                    if( bytesSrc == null ) return;
                    for( int len; 0 < (len = bytesSrc.Read(buffer, 0, buffer.Length)); )
                        bytes.AddRange(buffer.Take(len));
                }

                if( layout_UID == null ) // layout file does not exist
                {
                    layout_UID = new UID();

                    layout_UID!._packs    = packs.Select(pack_key).ToArray();
                    layout_UID!._hosts    = host_keys();
                    layout_UID._branches = branch_keys(project).ToArray();

                    SaveLayout.Actor0.Start.send(layout_UID, (SaveLayout.Connection)value!); // send collected pack, to get its binary representation
                    drain();
                    layout_UID_bytes = bytes.ToArray();                                      // in the `bytes` array

                    //based on project data can build only layout_UID_bytes.
                    return; // no any XY info to upload to the Observer
                }


                var tmp = new List<Info.XY>();

                void restore_layout_Info(ulong[] layout_uid, ulong[] project_uid, ref Info.XY[] xy)
                {
                    var k = 0;
                    if( layout_uid.Length == project_uid.Length )
                    {
                        for( ; k < layout_uid!.Length; k++ )      // Iterate through the layout_uids array; standard mode assumes arrays must be identical in content and order.
                            if( project_uid[k] != layout_uid[k] ) // Check if the project_uids and layout_uids arrays differ at any index, indicating the project was edited (entities added/removed).
                                goto update;
                        return;
                    }

                    update:

                    tmp.Clear();
                    tmp.AddRange(xy.Take(k));

                    foreach( var uid in project_uid.Skip(k) ) //xy by ref
                    {
                        var layout_idx = Array.IndexOf(layout_uid!, uid);
                        if( layout_idx == -1 && aliases.TryGetValue(uid, out var old) ) layout_idx = Array.IndexOf(layout_uid!, old); // renumbered: its old key

                        tmp.Add(layout_idx == -1 ?
                                    new Info.XY { x = int.MinValue } : //empty XY
                                    xy[layout_idx]);
                    }

                    xy = tmp.ToArray();
                }

                restore_layout_Info(layout_UID._packs!,    layout_UID._packs    = packs.Select(pack_key).ToArray(),     ref layout_INFO!._packs!);
                restore_layout_Info(layout_UID._hosts!,    layout_UID._hosts    = host_keys(),                         ref layout_INFO!._hosts!);
                restore_layout_Info(layout_UID._branches!, layout_UID._branches = branch_keys(project).ToArray(),     ref layout_INFO!._branches!);
                //now in the memory having consistent layout_UID

                // Identify and remove UIDs belonging to orphaned projects.

                SaveLayout.Actor0.Start.send(layout_UID, (SaveLayout.Connection)value); // send collected pack,
                drain();
                layout_UID_bytes = bytes.ToArray();                                     // to get its bytes in the `bytes` array

                bytes.Clear();

                SaveLayout.Actor0.Start.send(layout_INFO, (SaveLayout.Connection)value!); // send collected pack,
                drain();
                layout_INFO_bytes = bytes.ToArray();                                      // to get its bytes in the `bytes` array
            }
        }

        public int  ReceiveTimeout    { get; set; }
        public int  TransmitTimeout   { get; set; }
        public bool IsOpen            => true;
        public void CloseAndDispose() { }
        public void Abort()           { }
        public void Close()           { }

        public static void update() { new SaveLayout.Connection().External(new Layout()); }
    }
}