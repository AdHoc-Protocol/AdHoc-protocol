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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using org.unirail.Agent;
using org.unirail.Communication;
using static org.unirail.AdHocAgent;

namespace org.unirail{
    public class ToServer{
        static ToServer()
        {
            Action<Connection, Actor0, Actor0.State.TodoJobRequest.Transmitter> uploadTask = (conn, actor, transmitter) =>
                                                                                             {
                                                                                                 if( proto == null )
                                                                                                     if( provided_path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) )
                                                                                                         transmitter.send(project ?? ProjectImpl.init(), conn);
                                                                                                     else
                                                                                                         exit("Unsupported file type: " + provided_path, -1);
                                                                                                 else
                                                                                                     transmitter.send(proto, conn);
                                                                                             };

            Actor0.State.LoginResponse.OnReceiveD.AdHocProtocol_Server_Invitation.handlers += uploadTask; //server invites agent to upload a client task


            Actor0.State.VersionMatching.OnReceiveD.AdHocProtocol_Server_Invitation.handlers += (conn, actor, transmitter) =>
                                                                                                {
                                                                                                    PersonalVolatileUUID(out var hi, out var lo);                                          //get personal volatile UUID
                                                                                                    transmitter.send(new AdHocProtocol.Agent_.Login { uuid_hi = hi, uuid_lo = lo }, conn); //send login
                                                                                                };
            Actor0.State.LoginResponse.OnReceiveD.AdHocProtocol_Server_InvitationUpdate.handlers += (pack, conn, actor, transmitter) =>
                                                                                                    {
                                                                                                        PersonalVolatileUUID(out var hi, out var lo);
                                                                                                        if( hi != pack.uuid_hi || lo != pack.uuid_lo )
                                                                                                            updatePersonalVolatileUUID(Guid.Parse($"{pack.uuid_hi:x16}{pack.uuid_lo:x16}").ToString("D"));

                                                                                                        uploadTask(conn, actor, transmitter);
                                                                                                    };


            Actor0.State.Project.OnReceiveD.AdHocProtocol_Server_Result.handlers += (pack, conn, actor) =>
                                                                                    {
                                                                                        result_delivered = true;

                                                                                        //conn.ext_connection.CloseAndDispose();

                                                                                        _ = Task.Run(() =>
                                                                                                     {
                                                                                                         LOG.Information("Obtaining the generated code");
                                                                                                         try
                                                                                                         {
                                                                                                             // The entries already streamed themselves into RawFilesDirPath while the pack was being
                                                                                                             // deserialized — the folder was cleared and armed back in Start(project), before the connection.
                                                                                                             new FileInfo(provided_path).IsReadOnly = false; // remove `file uploaded` mark

                                                                                                             LOG.Information("Received result of the task {task} into the {folder}", pack.task!, RawFilesDirPath);
                                                                                                             if( pack.info != null ) LOG.Information("Information:\n{info}", pack.info); //output info into console

                                                                                                             // the hosts renumbered by this run: their custom code follows them, now and on a later redeploy
                                                                                                             var renumbered = Path.Join(RawFilesDirPath, Deployment.RENUMBERED);
                                                                                                             var lines      = ProjectImpl.renumbered_prefixes().Select(p => p.old + " " + p.@new).ToArray();
                                                                                                             if( 0 < lines.Length ) File.WriteAllLines(renumbered, lines);
                                                                                                             else File.Delete(renumbered);

                                                                                                             Deployment.deploy(RawFilesDirPath); //code deployment is starting
                                                                                                             done.SetResult(true);
                                                                                                         }
                                                                                                         catch( Exception e )
                                                                                                         {
                                                                                                             Console.WriteLine(e);
                                                                                                             throw;
                                                                                                         }
                                                                                                     });
                                                                                    };
            Actor0.State.Project.OnReceiveD.AdHocProtocol_Server_Info.handlers += (pack, conn, actor) => LOG.Information("Received new information:\n{information}", pack.info);

            Actor0.State.Proto.OnReceiveD.AdHocProtocol_Server_Result.handlers += (pack, conn, actor) =>
                                                                                  {
                                                                                      result_delivered = true;
                                                                                     // conn.ext_connection.CloseAndDispose();


                                                                                      LOG.Information("Received result of .proto format conversion"); // already on disk: the entries streamed into destination_dir_path as they arrived

                                                                                      if( !string.IsNullOrEmpty(pack.info) ) Console.Out.WriteLine($"Information:\n{pack.info}"); //output info into console
                                                                                      exit("Here is the result of the .proto format conversion: " + destination_dir_path, 0);
                                                                                  };
            Actor0.State.VersionMatching.OnReceiveD.AdHocProtocol_Server_Info.handlers += (pack, conn, actor) => //the agent and server have incompatible protocol versions
                                                                                          {
                                                                                              LOG.Error("{info}", pack.info);
                                                                                              exit("Resolve the issue and try again.");
                                                                                          };

            Actor0.State.LoginResponse.OnReceiveD.AdHocProtocol_Server_Info.handlers += (pack, conn, actor) =>
                                                                                        {
                                                                                           // conn.ext_connection.CloseAndDispose();
                                                                                            LOG.Error(pack.info);
                                                                                            // The token the last rotation replaced is still in the config. The server keeps it valid until
                                                                                            // the new one is confirmed, so if the current UUID was rejected as unknown, re-applying the
                                                                                            // previous one gets a fresh token issued - no new sign-up needed.
                                                                                            if( app_props.HasKey("PreviousVolatileUUID") )
                                                                                                LOG.Information("The UUID this one replaced is kept in {file} as PreviousVolatileUUID. If the current UUID was rejected as unknown, re-apply it: AdHocAgent {uuid}", app_props_file, app_props["PreviousVolatileUUID"].AsString?.Value);
                                                                                        };

            Actor0.State.TodoJobRequest.OnSerializeD.AdHocProtocol_Agent_Project.handlers += (pack, conn, actor) =>
                                                                                             {
                                                                                                 new FileInfo(provided_path).IsReadOnly = true;                                             //+ delete old files - mark:  the file was sent
                                                                                                 var result_output_folder = Path.Combine(destination_dir_path, ((ProjectImpl)pack!)._name); // destination_dir_path/project_name
                                                                                                 if( Directory.Exists(result_output_folder) ) Directory.Delete(result_output_folder, true);
                                                                                             };
            // A connection that dies BEFORE the result is complete used to leave the Agent parked on
            // `done.Task` for ever — no error, no exit, just a half-written tree as the only clue.
            // Report what arrived and let Start() return instead of waiting for a completion that cannot come.
            Connection.OnEvent.handlers += (conn, evenT) =>
                                           {
                                               if( result_delivered || !lost_connection.Contains(evenT) ) return;

                                               LOG.Error("Connection closed before the result was complete: {event}. Nothing was deployed; whatever arrived is in {folder:l}", evenT, receiving_into);
                                               done?.TrySetResult(false);
                                           };
#if DEBUG
            Connection.OnEvent.handlers += (conn, evenT) => Network.TCP.onEventPrintConsole(conn.ext_connection, evenT);
#endif
        }

        // Events that end the exchange for reasons other than "we are done": every remote close, every abrupt
        // close of ours, and the receive timeout.
        static readonly HashSet<Network.TCP.ExternalConnection.Event> lost_connection =
        [
            Network.TCP.ExternalConnection.Event.REMOTE_CLOSE_GRACEFUL,
            Network.TCP.ExternalConnection.Event.REMOTE_CLOSE_ABRUPTLY,
            Network.TCP.ExternalConnection.Event.THIS_CLOSE_ABRUPTLY,
            Network.TCP.ExternalConnection.Event.RECEIVE_TIMEOUT,
            Network.TCP.ExternalConnection.Event.WEBSOCKET_REMOTE_CLOSE_GRACEFUL,
            Network.TCP.ExternalConnection.Event.WEBSOCKET_REMOTE_CLOSE_ABRUPTLY,
            Network.TCP.ExternalConnection.Event.WEBSOCKET_THIS_CLOSE_ABRUPTLY,
            Network.TCP.ExternalConnection.Event.WEBSOCKET_PROTOCOL_ERROR,
        ];

        // Set the moment a Result pack has been fully deserialized: after that the closing connection is ours,
        // and expected.
        static bool result_delivered;

        // Where the incoming `FileEntry.List` entries land — whatever the running task armed `Files.ONE` with.
        // The abort message needs THIS, not `RawFilesDirPath`: that one is built by cutting a `.cs` off the
        // provided path, so for a `.proto` task it names a folder that does not exist (`My.proto` → `My.pr`)
        // and throws outright when the file name is shorter than the extension it cuts.
        static string receiving_into = "";


        static        ProjectImpl?                project;
        static        TaskCompletionSource<bool>  done = null!;
        public static AdHocProtocol.Agent_.Proto? proto;

        static readonly Random random = new();

        public static async Task Start(ProjectImpl project) //send a project task
        {
            ToServer.project = project;

            // Cleared and armed BEFORE the connection opens. `Result.result` entries stream straight to disk as
            // they arrive, so the folder is already populated by the time the receive handler runs — wiping it
            // there (as the pre-streaming code did) would delete exactly what was just received.
            if( Directory.Exists(RawFilesDirPath) ) Directory.Delete(RawFilesDirPath, true);
            Files.ONE.Receive(receiving_into = RawFilesDirPath);

            await Start();
        }

        public static async Task Start(AdHocProtocol.Agent_.Proto proto) //send protocol buffers task
        {
            ToServer.proto = proto;
            Files.ONE.Receive(receiving_into = destination_dir_path); //overwrite in place, as the extraction used to
            await Start();
        }

        static async Task Start()
        {
            done = new TaskCompletionSource<bool>();
            foreach( var url in app_props["server"].AsArray.RawArray.Select(c => c.AsString.Value) )
            {
                LOG.Information("Connecting to the {connection}", url);
                Connection connection = null;

                // A refused/unreachable server must only cost us THIS url: both clients report a failed connect by
                // throwing (the WebSocket one completes its task with the exception, the TCP one re-throws), so an
                // uncaught attempt would escape Main and the remaining urls would never be tried. The try wraps the
                // connect ALONE — a failure after this point is a protocol problem, not a reason to try elsewhere.
                try
                {
                    if( url.StartsWith("ws:", StringComparison.OrdinalIgnoreCase) || url.StartsWith("wss:", StringComparison.OrdinalIgnoreCase) )
                        connection = await new Network.TCP.WebSocket.Client<Connection>("http_client", (ext) => new Connection(), Network.TCP.onFailurePrintConsole, 1024, mux: true).ConnectAsync(new Uri(url), TimeSpan.FromSeconds(10));
                    else
                    {
                        var uri       = new Uri("http://" + url);
                        var ipAddress = IPAddress.Loopback;

                        if( !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) )
                            try
                            {
                                var addresses = (await Dns.GetHostAddressesAsync(uri.Host));
                                if( addresses.Length == 0 )
                                {
                                    LOG.Warning("No IP address found for host {host}.", uri.Host);
                                    continue;
                                }

                                ipAddress = addresses[random.Next(addresses.Length)];
                            }
                            catch( SocketException ex )
                            {
                                LOG.Warning("DNS lookup failed for {host}: {message}", uri.Host, ex.Message);
                                continue;
                            }

                        connection = await new Network.TCP.Client<Connection>("tcp_client", (ext) => new Connection(), Network.TCP.onFailurePrintConsole, 1024, mux: true).ConnectAsync(new IPEndPoint(ipAddress, uri.Port), TimeSpan.FromSeconds(10));
                    }
                }
                catch( Exception ex ) //refused, unreachable, connect timeout, bad url — all mean "try the next one"
                {
                    LOG.Warning("The connection to {connection} has failed: {message}", url, ex.Message);
                    continue;
                }

                if( connection == null ) //the older clients signalled failure with null instead of an exception
                {
                    LOG.Warning("The connection to {connection} has failed.", url);
                    continue;
                }

                LOG.Information("Connected to {connection}.", url);

                Actor0.State.O.transmitter.send(new AdHocProtocol.Agent_.Version
                                                {
                                                    LCID = (byte)CultureInfo.CurrentUICulture.LCID,
                                                    zone = (byte)(TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes / 15),
                                                    uid  = (ushort)VER
                                                }, connection);
                await done.Task;
                return;
            }

            exit("There are no servers available.");
        }


        const uint VER = 1; //version
    }
}
