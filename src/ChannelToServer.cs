using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using org.unirail.Agent;
using org.unirail.Agent.AgentToServer;

namespace org.unirail
{
    public class ChannelToServer : Communication, Communication.Receivable.Listener
    {
        public static readonly ChannelToServer channel = new();

        static ChannelToServer() { onReceiveListener = channel; }

        private static readonly Network.TCP.Client client = new(1024, () => Console.WriteLine("Server is not reachable!"), TimeSpan.FromSeconds(3));

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

        public static void Start() //just query result
        {
            var server = AdHocAgent.app_props["server"].AsString.Value;

            var http = server.ToLower() switch
                       {
                           "http://"  => true,
                           "https://" => true,
                           _          => false
                       };

            var url = new Uri(http
                                  ? server
                                  : "http://" + server);

            var ipAddress = IPAddress.Loopback;

            if (!url.Host.ToLower().Equals("localhost"))
                if (!IPAddress.TryParse(url.Host, out ipAddress))
                {
                    var adrrs = Dns.GetHostEntry(url.Host).AddressList;
                    ipAddress = adrrs[new Random().Next(0, adrrs.Length - 1)];
                }

            client.bind(channel.ext_src, new IPEndPoint(ipAddress, url.Port));
            Login();

            Thread.Sleep(int.MaxValue);
            Console.Write("exit");
        }


        private static async void Login()
        {
            while (!AdHocAgent.AuthorizationCodeOK)
            {
                Console.WriteLine("Need to update your Personal GitHub Authorization Code. Do you want to try? Enter 'N' - if `NO` and exit.");
                switch (Console.Read())
                {
                    case 'N':
                    case 'n':
                        AdHocAgent.exit("Bye", -1);
                        return;
                    default:
                        await AdHocAgent.updateMyPersonalSecretGitHubAuthorizationCode();
                        break;
                }
            }

            channel.send(new Login() { client = AdHocAgent.app_props["MyPersonalSecretGitHubAuthorizationCode"] });
        }

        public void Received_Upload(Communication via) //server invite agent to upload client task
        {
            if (proto == null)
            {
                if (AdHocAgent.provided_path.EndsWith(".cs"))
                    /*if (new FileInfo(AdHocAgent.provided_path).IsReadOnly) //task was uploaded, requesting result
                        channel.send(new RequestResult() { task = AdHocAgent.task });
                    else*/
                    channel.send(project ?? ProjectImpl.init());
                else AdHocAgent.exit("Unsupported file type" + AdHocAgent.provided_path, -1);
            }
            else channel.send(proto);
        }

        public void Received_ServerToAgent_LoginRejected(Communication via)
        {
            Console.WriteLine("Login was rejected by server.");
            AdHocAgent.AuthorizationCodeOK = false;
            Login();
        }


        public void Received(Communication via, Agent.ServerToAgent.Result data)
        {
            AdHocAgent.LOG.Information("Receive result of the task");
            AdHocAgent.LOG.Information(data.task!);


            using var result = new MemoryStream(data.result!);
            if (data.task!.EndsWith(".cs"))
            {
                //  destination_dir_path/project_name

                if (Directory.Exists(AdHocAgent.raw_files_dir_path)) Directory.Delete(AdHocAgent.raw_files_dir_path, true);

                AdHocAgent.unzip(result, AdHocAgent.destination_dir_path); // extract into destination_dir_path/project_name
                new FileInfo(AdHocAgent.provided_path).IsReadOnly = false;
                //code deployment is starting
                AdHocAgent.Deployment.deploy(AdHocAgent.raw_files_dir_path);

                return;
            }

            AdHocAgent.unzip(result, AdHocAgent.destination_dir_path);

            var proto_file_conversion_result = Path.Combine(AdHocAgent.destination_dir_path, AdHocAgent.provided_path[..^5] + "cs"); //cut "proto" extention
            if (File.Exists(proto_file_conversion_result)) File.Delete(proto_file_conversion_result);
            File.Move(Path.Combine(AdHocAgent.destination_dir_path, data.task), proto_file_conversion_result);

            AdHocAgent.exit("Please find .proto file conversion result " + proto_file_conversion_result, 0);
        }


        public void Received(Communication via, Agent.ServerToAgent.Busy data) { Console.Write("Bysy"); }

        public void Received(Communication via, Agent.ServerToAgent.Info data) { AdHocAgent.LOG.Information(data.ToString()); }


        private static readonly string VER       = "1.0";            //version
        private static readonly string INFO_MARK = "///" + "\uFFFF"; //generated section mark

        //https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/recommended-tags-for-documentation-comments
    }
}