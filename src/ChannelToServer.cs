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
using Tommy;
using org.unirail.Agent;
using Version = org.unirail.Agent.Version;

namespace org.unirail
{
    public class ChannelToServer : Communication.Receiver.Receivable.Handler
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


        #region refresh Authorization Code
        public static ulong? AuthorizationCode()
        {
            if (!AdHocAgent.app_props.HasKey("PersonalAdHocSecretAuthorizationCode")) return null;
            var current = AdHocAgent.app_props["PersonalAdHocSecretAuthorizationCode"].AsString;


            try { return ulong.Parse(current, NumberStyles.HexNumber); }
            catch (Exception ex)
            {
                AdHocAgent.LOG.Error("The file {app_props_file}  contains wrong `PersonalAdHocSecretAuthorizationCode` do you want update it? Enter 'N' - if no and exit.", AdHocAgent.app_props_file);
                switch (Console.Read())
                {
                    case 'N':
                    case 'n':
                        AdHocAgent.exit("Bye", -1);
                        return null;
                    default:
                        return null;
                }
            }
        }

        static readonly byte[] close_browser_bytes = Encoding.ASCII.GetBytes("<script>window.close();</script>");

        public static string? updatePersonalAdHocSecretAuthorizationCode()
        {
            var adhoc_id = AdHocAgent.app_props["adhoc_id"].AsString.Value;

            //https://docs.github.com/en/developers/apps/building-oauth-apps/authorizing-oauth-apps#1-request-a-users-github-identity
            var url = $"https://github.com/login/oauth/authorize?client_id={adhoc_id}&scope={HttpUtility.UrlEncode("read:user user:email", Encoding.UTF8)}";

            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            AdHocAgent.LOG.Information("Authorization. Open browser {0} and accept security request... Waiting for GITHUB reply...", url);

            var oauth = "";
            try
            {
                var listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:9000/");
                listener.IgnoreWriteExceptions = true; //ignore "The specified network name is no longer available"
                listener.Start();

                var ctx = listener.GetContext(); //block

                oauth = ctx.Request.QueryString["code"];
                var resp = ctx.Response;
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.ASCII;
                resp.ContentLength64 = close_browser_bytes.LongLength;

                resp.OutputStream.Write(close_browser_bytes);
                resp.OutputStream.Close();
            }
            catch (Exception e)
            {
                AdHocAgent.LOG.Error(e, "Error while updatePersonalAdHocSecretAuthorizationCode");
                throw;
            }

            if (oauth == null) AdHocAgent.LOG.Error("Authorization failure");
            return oauth;
        }
        #endregion

        static ulong? login;
        static string? oauth;

        private static void Start()
        {
            if ((login = AuthorizationCode()) == null && (oauth = updatePersonalAdHocSecretAuthorizationCode()) == null) return;

            Start(() => to_server!.send(new Version(VER))); //inform server about client version
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


        static ChannelToServer() { Communication.Receiver.onReceive = new ChannelToServer(); }

        public void Received(Communication.Receiver via, Invitation invitation) //server invite agent to upload client task
        {
            if (via.curr_stage == Communication.Stages.Login) //the server and client protocol versions are matching
            {
                if (login == null)
                    Start(() => to_server!.send(new Signup { _oauth = Encoding.ASCII.GetBytes(oauth!) }));
                else
                    Start(() => to_server!.send(new Login { uid = login.Value }));

                return;
            }

            if (via.curr_stage == Communication.Stages.TodoJobRequest)
            {
                //invitation may contains new user id
                if (AdHocAgent.app_props.HasKey("PersonalAdHocSecretAuthorizationCode"))
                {
                    var current = AuthorizationCode();
                    if (current != null && current.Value == invitation.uid) goto code_ok;
                    AdHocAgent.app_props["PersonalAdHocSecretAuthorizationCode"].AsString.Value = invitation.uid.ToString("X");
                }
                else AdHocAgent.app_props.Add("PersonalAdHocSecretAuthorizationCode", new TomlString { Value = invitation.uid.ToString("X") });

                var writer = File.CreateText(AdHocAgent.app_props_file);
                AdHocAgent.app_props.WriteTo(writer);
                writer.Flush();
                writer.Close();

            code_ok:

                if (proto == null)
                    if (AdHocAgent.provided_path.EndsWith(".cs"))
                        /*if (new FileInfo(AdHocAgent.provided_path).IsReadOnly) //task was uploaded, requesting result
                            Channel.send(new RequestResult() { task = AdHocAgent.task });
                        else*/
                        to_server.send(project ?? ProjectImpl.init());
                    else AdHocAgent.exit("Unsupported file type: " + AdHocAgent.provided_path, -1);
                else to_server.send(proto);
            }
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

        public void Received(Communication.Receiver via, Agent.Info pack)
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
                if (!pack.info.Contains("PersonalAdHocSecretAuthorizationCode")) return;

                Task.Run(() => //===================== NEVER BLOCK IN HANDLERS !!!!
                         {
                             AdHocAgent.LOG.Error("Do you want to try re-authorization? Enter 'N' - if no and exit");
                             switch (Console.Read())
                             {
                                 case 'N':
                                 case 'n':
                                     AdHocAgent.exit("Bye", -1);
                                     return;
                                 default:
                                     updatePersonalAdHocSecretAuthorizationCode();
                                     return;
                             }
                         });
            }
            else
                AdHocAgent.LOG.Information("Received new information:\n{information}", pack.info);
        }


        private const uint VER = 1; //version

        private const string INFO_MARK = "///" + "\uFFFF"; //generated section mark

        //public static Memory<byte> get_global_settings(string key)=>  AdHoc.value($"{key}.unirail.org") ;
    }
}