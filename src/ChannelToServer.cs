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
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using org.unirail.Agent;
using org.unirail.Agent.AdHocProtocol.Agent_;
using org.unirail.Agent.AdHocProtocol.Server_;
using Tommy;
using Version = org.unirail.Agent.AdHocProtocol.Agent_.Version;

namespace org.unirail
{
    public class ChannelToServer : Communication.Receiver.Receivable.Handler, Communication.Transmitter.Transmittable.Handler
    {
        private static ProjectImpl? project;

        public static void Start(ProjectImpl project)
        {
            ChannelToServer.project = project;
            Start();
        }

        public static Proto? proto;

        public static void Start(Proto proto)
        {
            ChannelToServer.proto = proto;
            Start();
        }


        private static void Start()
        {
            Start(() => to_server!.send(new Version(VER))); //inform server about the client version
        }


        private static void Start(Action onConnect)
        {
            if (to_server == null) try_to_connect(0, AdHocAgent.app_props["server"].AsArray.RawArray, onConnect);
            else onConnect();
        }


        private static void try_to_connect(int i, List<TomlNode>? connections, Action onConnect)
        {
            var connection = connections![i].AsString.Value;
            AdHocAgent.LOG.Information("Connecting to the {connection}", connection);

            if (connection.StartsWith("ws:", StringComparison.CurrentCultureIgnoreCase) || connection.StartsWith("wss:", StringComparison.CurrentCultureIgnoreCase))
                http_client.Connect(new Uri(connection),
                                    dst =>
                                    {
                                        to_server = dst;
                                        http_client.onEvent = (ch, ev) =>
                                                              {
                                                                  switch (ev)
                                                                  {
                                                                      case (int)Network.Channel.Event.EXT_INT_DISCONNECT:
                                                                          AdHocAgent.LOG.Warning("Peer {connection} dropped connection", connection);
                                                                          to_server = null;
                                                                          break;
                                                                      case (int)Network.Channel.Event.INT_EXT_DISCONNECT:
                                                                          AdHocAgent.LOG.Warning("Drop connection to the {connection}", connection);
                                                                          to_server = null;
                                                                          break;
                                                                  }
                                                              };
                                        AdHocAgent.LOG.Information("Connected to {connection}.", connection);
                                        onConnect();
                                    },
                                    ex =>
                                    {
                                        AdHocAgent.LOG.Warning("The connection to {connection} has failed.", connection);
                                        if (++i < connections.Count) try_to_connect(i, connections, onConnect);
                                        else AdHocAgent.exit("There are no servers available.");
                                    },
                                    TimeSpan.FromSeconds(10));
            else
            {
                var uri = new Uri("http://" + connection);
                var ipAddress = IPAddress.Loopback;

                if (!uri.Host.ToLower().Equals("localhost"))
                    if (!IPAddress.TryParse(uri.Host, out ipAddress))
                    {
                        var adrrs = Dns.GetHostEntry(uri.Host).AddressList;
                        ipAddress = adrrs[new Random().Next(0, adrrs.Length - 1)];
                    }

                tcp_client.Connect(new IPEndPoint(ipAddress, uri.Port),
                                   dst =>
                                   {
                                       to_server = dst;
                                       tcp_client.onEvent = (ch, ev) =>
                                                            {
                                                                switch (ev)
                                                                {
                                                                    case (int)Network.Channel.Event.EXT_INT_DISCONNECT:
                                                                        AdHocAgent.LOG.Warning("Peer {connection} has dropped the connection.", connection);
                                                                        to_server = null;
                                                                        break;
                                                                    case (int)Network.Channel.Event.INT_EXT_DISCONNECT:
                                                                        AdHocAgent.LOG.Warning("Drop connection to {connection}", connection);
                                                                        to_server = null;
                                                                        break;
                                                                }
                                                            };
                                       onConnect();
                                   },
                                   ex =>
                                   {
                                       AdHocAgent.LOG.Warning("The connection to {connection} has failed.", connection);
                                       if (++i < connections.Count) try_to_connect(i, connections, onConnect);
                                       else AdHocAgent.exit("There are no servers available.");
                                   },
                                   TimeSpan.FromSeconds(10));
            }


            Thread.Sleep(int.MaxValue);
            Console.Write("exit");
        }


        private static readonly Network.TCP<Communication.Transmitter, Communication.Receiver>.WebSocket.Client http_client = new("http_client", Communication.new_WebSocket_channel, 1024, TimeSpan.FromMinutes(10));
        private static readonly Network.TCP<Communication.Transmitter, Communication.Receiver>.Client tcp_client = new("tcp_client", Communication.new_TCP_channel, 1024, TimeSpan.FromMinutes(10));


        public static Communication.Transmitter? to_server;


        static ChannelToServer()
        {
            var THIS = new ChannelToServer();
            Communication.Transmitter.onTransmit = THIS;
            Communication.Receiver.onReceive = THIS;
        }

        public void Sent(Communication.Transmitter via, Version pack) { }
        public void Sent(Communication.Transmitter via, Login pack) { }

        public void Received_AdHocProtocol_Server__Invitation(Communication.Receiver via)
        {
            if (via.curr_stage == Communication.Stages.Login) //server invites agent to upload a client task
            {
                AdHocAgent.PersonalVolatileUUID(out var hi, out var lo);                //get personal volatile UUID
                Start(() => to_server!.send(new Login { uuid_hi = hi, uuid_lo = lo })); //send login
            }
            else uploadTask();
        }

        void uploadTask()
        {
            if (proto == null)
                if (AdHocAgent.provided_path.EndsWith(".cs")) to_server!.send(project ?? ProjectImpl.init());
                else AdHocAgent.exit("Unsupported file type: " + AdHocAgent.provided_path, -1);
            else
                to_server!.send(proto);
        }

        private ulong new_uuid_hi = 0;
        private ulong new_uuid_lo = 0;
        private bool update_uuid = false;

        public void Received(Communication.Receiver via, InvitationUpdate pack)
        {
            new_uuid_hi = pack.uuid_hi; //temporary fixing updated personal volatile uuid data
            new_uuid_lo = pack.uuid_lo;
            update_uuid = true;
            uploadTask();
        }

        public void Sent(Communication.Transmitter via, Project pack)
        {
            new FileInfo(AdHocAgent.provided_path).IsReadOnly = true;                            //+ delete old files - mark:  the file was sent
            var result_output_folder = Path.Combine(AdHocAgent.destination_dir_path, pack!._name); // destination_dir_path/project_name
            if (Directory.Exists(result_output_folder))
                Directory.Delete(result_output_folder, true);

            if (!update_uuid) return;
            //Update only once both the server and agent have confirmed the UUID update, ensuring no interruptions in the connection that could disrupt the process.
            update_uuid = false;
            AdHocAgent.updatePersonalVolatileUUID(new_uuid_hi, new_uuid_lo);
        }

        public void Sent(Communication.Transmitter via, Proto pack)
        {
            if (!update_uuid) return;
            //Update only once both the server and agent have confirmed the UUID update, ensuring no interruptions in the connection that could disrupt the process.
            update_uuid = false;
            AdHocAgent.updatePersonalVolatileUUID(new_uuid_hi, new_uuid_lo);
        }


        public void Received(Communication.Receiver via, Result pack)
        {
            via.channel.Close_and_dispose();

            using var zipped_bytes = new MemoryStream(pack._result!);

            if (pack.task!.EndsWith(".cs"))
            {
                AdHocAgent.LOG.Information("Obtaining the generated code");
                try
                {
                    if (Directory.Exists(AdHocAgent.raw_files_dir_path)) Directory.Delete(AdHocAgent.raw_files_dir_path, true);

                    AdHocAgent.unzip(zipped_bytes, AdHocAgent.raw_files_dir_path); // extract into the destination_dir_path/project_name
                    new FileInfo(AdHocAgent.provided_path).IsReadOnly = false;     // remove `file uploaded` mark

                    AdHocAgent.LOG.Information("Received result of the task {task} into the {folder}", pack.task!, AdHocAgent.raw_files_dir_path);
                    if (pack.info != null) AdHocAgent.LOG.Information("Information:\n{info}", pack.info); //output info into console

                    AdHocAgent.Deployment.deploy(AdHocAgent.raw_files_dir_path); //code deployment is starting
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                return;
            }

            AdHocAgent.LOG.Information("Receiving result of .proto format conversion");
            AdHocAgent.unzip(zipped_bytes, AdHocAgent.destination_dir_path);

            if (!string.IsNullOrEmpty(pack.info)) Console.Out.WriteLine($"Information \n {pack.info}:\n"); //output info into console
            AdHocAgent.exit("Here is the result of the .proto format conversion: " + AdHocAgent.destination_dir_path, 0);
        }

        public void Received(Communication.Receiver via, Info pack)
        {
            if (via.curr_stage == Communication.Stages.VersionMatching) //the agent and server have incompatible protocol versions
            {
                AdHocAgent.LOG.Error("{}", pack.info);
                AdHocAgent.exit("Resolve the issue and try again.");
            }
            else if (via.curr_stage == Communication.Stages.Login)
            {
                via.channel.Close();

                AdHocAgent.LOG.Error("{}", pack.info);
                if (!pack.info.Contains("PersonalVolatileUUID")) return;

                Task.Run(() => //===================== NEVER BLOCK IN HANDLERS !!!!
                         {
                             AdHocAgent.LOG.Error("Do you want to try re-connect? Enter 'N' - if no and exit");
                             switch (Console.Read())
                             {
                                 case 'N':
                                 case 'n':
                                     AdHocAgent.exit("Bye", -1);
                                     return;
                                 default:
                                     return;
                             }
                         });
            }
            else
                AdHocAgent.LOG.Information("Received new information:\n{information}", pack.info);
        }


        private const uint VER = 1; //version

        private const string INFO_MARK = "///" + "\uFFFF"; //generated section mark
    }
}