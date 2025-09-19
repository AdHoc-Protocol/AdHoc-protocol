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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using org.unirail.Agent;
using org.unirail.Communication;
using Tommy;
using static org.unirail.AdHocAgent;
using static org.unirail.Agent.AdHocProtocol.Server_;

namespace org.unirail
{
    public class ChannelToServer
    {
        static ChannelToServer()
        {
            Action<Context, Stages.TodoJobRequest.Transmitter> uploadTask = (context, transmitter) =>
                                                                            {
                                                                                if (proto == null)
                                                                                    if (provided_path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                                                                                        transmitter.send(project ?? ProjectImpl.init(), context);
                                                                                    else
                                                                                        exit("Unsupported file type: " + provided_path, -1);
                                                                                else
                                                                                    transmitter.send(proto, context);
                                                                            };

            Invitation.OnReceived_via_Communication_at_LoginResponse.handlers += uploadTask; //server invites agent to upload a client task


            Invitation.OnReceived_via_Communication_at_VersionMatching.handlers += (context, transmitter) =>
                                                                                   {
                                                                                       PersonalVolatileUUID(out var hi, out var lo);                                             //get personal volatile UUID
                                                                                       transmitter.send(new AdHocProtocol.Agent_.Login { uuid_hi = hi, uuid_lo = lo }, context); //send login
                                                                                   };
            InvitationUpdate.OnReceived_via_Communication_at_LoginResponse.handlers += (pack, context, transmitter) =>
                                                                                       {
                                                                                           PersonalVolatileUUID(out var hi, out var lo);
                                                                                           if (hi != pack.uuid_hi || lo != pack.uuid_lo)
                                                                                               updatePersonalVolatileUUID(Guid.Parse($"{pack.uuid_hi:x16}{pack.uuid_lo:x16}").ToString("D"));

                                                                                           uploadTask(context, transmitter);
                                                                                       };


            Result.OnReceived_via_Communication_at_Project.handlers += (pack, context) =>
                                                                       {
                                                                           context.channel.ext_channal.CloseAndDispose();

                                                                           _ = Task.Run(() =>
                                                                                        {
                                                                                            using var zipped_bytes = new MemoryStream(pack._result!);
                                                                                            LOG.Information("Obtaining the generated code");
                                                                                            try
                                                                                            {
                                                                                                if (Directory.Exists(RawFilesDirPath)) Directory.Delete(RawFilesDirPath, true);

                                                                                                unzip(zipped_bytes, RawFilesDirPath);        // extract into the destination_dir_path/project_name
                                                                                                new FileInfo(provided_path).IsReadOnly = false; // remove `file uploaded` mark

                                                                                                LOG.Information("Received result of the task {task} into the {folder}", pack.task!, RawFilesDirPath);
                                                                                                if (pack.info != null) LOG.Information("Information:\n{info}", pack.info); //output info into console

                                                                                                Deployment.deploy(RawFilesDirPath); //code deployment is starting
                                                                                                done.SetResult(true);
                                                                                            }
                                                                                            catch (Exception e)
                                                                                            {
                                                                                                Console.WriteLine(e);
                                                                                                throw;
                                                                                            }
                                                                                        });
                                                                       };
            Info.OnReceived_via_Communication_at_Project.handlers += (pack, context) => LOG.Information("Received new information:\n{information}", pack.info);

            Result.OnReceived_via_Communication_at_Proto.handlers += (pack, context) =>
                                                                     {
                                                                         context.channel.ext_channal.CloseAndDispose();

                                                                         using var zipped_bytes = new MemoryStream(pack._result!);

                                                                         LOG.Information("Receiving result of .proto format conversion");
                                                                         unzip(zipped_bytes, destination_dir_path);

                                                                         if (!string.IsNullOrEmpty(pack.info)) Console.Out.WriteLine($"Information:\n{pack.info}"); //output info into console
                                                                         exit("Here is the result of the .proto format conversion: " + destination_dir_path, 0);
                                                                     };
            Info.OnReceived_via_Communication_at_VersionMatching.handlers += (pack, context) => //the agent and server have incompatible protocol versions
                                                                             {
                                                                                 LOG.Error("{info}", pack.info);
                                                                                 exit("Resolve the issue and try again.");
                                                                             };

            Info.OnReceived_via_Communication_at_LoginResponse.handlers += (pack, context) =>
                                                                           {
                                                                               context.channel.ext_channal.CloseAndDispose();
                                                                               LOG.Error(pack.info);
                                                                           };

            AdHocProtocol.Agent_.Project.OnSent_via_Communication_at_TodoJobRequest.handlers += (pack, context) =>
                                                                                                {
                                                                                                    new FileInfo(provided_path).IsReadOnly = true;                                             //+ delete old files - mark:  the file was sent
                                                                                                    var result_output_folder = Path.Combine(destination_dir_path, ((ProjectImpl)pack!)._name); // destination_dir_path/project_name
                                                                                                    if (Directory.Exists(result_output_folder))
                                                                                                        Directory.Delete(result_output_folder, true);
                                                                                                };
#if DEBUG
            Channel.OnEvent.handlers += (channel, evenT) => Network.TCP.onEventPrintConsole(channel.ext_channal, evenT);
#endif
        }


        static ProjectImpl? project;
        static TaskCompletionSource<bool> done = null!;
        public static AdHocProtocol.Agent_.Proto? proto;

        static readonly Random random = new Random();

        public static async Task Start(ProjectImpl project) //send a project task
        {
            ChannelToServer.project = project;
            await Start();
        }

        public static async Task Start(AdHocProtocol.Agent_.Proto proto) //send protocol buffers task
        {
            ChannelToServer.proto = proto;
            await Start();
        }

        static async Task Start()
        {
            done = new TaskCompletionSource<bool>();
            foreach (var connection in app_props["server"].AsArray.RawArray.Select(c => c.AsString.Value))
            {
                LOG.Information("Connecting to the {connection}", connection);
                Channel channel = null;

                if (connection.StartsWith("ws:", StringComparison.OrdinalIgnoreCase) || connection.StartsWith("wss:", StringComparison.OrdinalIgnoreCase))
                    channel = await new Network.TCP.WebSocket.Client<Channel>("http_client", (ext) => new Channel(ext), Network.TCP.onFailurePrintConsole, 1024).ConnectAsync(new Uri(connection), TimeSpan.FromSeconds(10));
                else
                {
                    var uri = new Uri("http://" + connection);
                    var ipAddress = IPAddress.Loopback;

                    if (!uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                        try
                        {
                            var addresses = (await Dns.GetHostAddressesAsync(uri.Host));
                            if (addresses.Length == 0)
                            {
                                LOG.Warning("No IP address found for host {host}.", uri.Host);
                                continue;
                            }

                            ipAddress = addresses[random.Next(addresses.Length)];
                        }
                        catch (SocketException ex)
                        {
                            LOG.Warning("DNS lookup failed for {host}: {message}", uri.Host, ex.Message);
                            continue;
                        }

                    channel = await new Network.TCP.Client<Channel>("tcp_client", (ext) => new Channel(ext), Network.TCP.onFailurePrintConsole, 1024).ConnectAsync(new IPEndPoint(ipAddress, uri.Port), TimeSpan.FromSeconds(10));
                }

                if (channel == null)
                {
                    LOG.Warning("The connection to {connection} has failed.", connection);
                    continue;
                }

                LOG.Information("Connected to {connection}.", connection);
                Stages.O.transmitter.send(new AdHocProtocol.Agent_.Version(VER), channel.context(0));
                await done.Task;
                return;
            }

            exit("There are no servers available.");
        }


        const uint VER = 1; //version

        const string INFO_MARK = "///" + "\uFFFF"; //generated section mark
    }
}