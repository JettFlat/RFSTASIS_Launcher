using MiniLauncher.Helper;
using MiniLauncher.Network;
using MiniLauncher.Network.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static RFSTASIS_Launcher.Model;

namespace RFSTASIS_Launcher
{
    public class GameClient
    {
        static NetworkClient networkClient;
        public static string Path => Environment.CurrentDirectory;
        public static ConcurrentBag<FileInfoContainer> GetFilesHash()
        {
            ConcurrentBag<FileInfoContainer> res = new ConcurrentBag<FileInfoContainer>();
            var files = Directory.GetFiles(Path, "", SearchOption.AllDirectories);
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 1 }, file =>
            {
                var fi = new FileInfo(file);
                var item = new FileInfoContainer { Name = fi.Name, FilePath = fi.FullName, DateEdit = fi.LastWriteTime };
                using (var str = File.OpenRead(fi.FullName))
                    item.MD5Hash = MD5Hash.GetHash(str);
                res.Add(item);
            });
            return res;
        }
        public static void GetUpdates()
        {
            var local = GetFilesHash();
            var server = FileInfoContainer.Read(Server.DownloadHashFile());
            List<FileInfoContainer> toupdate = new List<FileInfoContainer>(server.Where(s => local.FirstOrDefault(l => s.FilePath == l.FilePath)?.MD5Hash != s.MD5Hash));
            var remove = SettingsCur.FilesToIgnore.Concat(SettingsCur.FilesToNotUpdate).Distinct().ToList();
            toupdate = new List<FileInfoContainer>(toupdate.Where(x => !remove.Any(y => y == x.FilePath)));
            var tmp = SettingsCur.FilesToNotUpdate.Except(local.Select(y => y.FilePath)).ToList();
            var todownload = server.Where(s => tmp.Any(l => s.FilePath == l)).ToList();
            toupdate.AddRange(todownload);
            Server.Download(new ConcurrentBag<FileInfoContainer>(toupdate));
            if (File.Exists($"[NEW]{ExecutionFileName}"))
            {
                //TODO Run EXE Replace program
                var id = Process.GetCurrentProcess().Id;
                try
                {
                    var args = $"{id} \"{ExecutionFileName}\" \"[NEW]{ExecutionFileName}\"";
                    Process.Start("Replacer.exe", args);
                    Environment.Exit(0);
                }
                catch (Exception exc)
                {

                }
            }
        }
        public static void Start()
        {
            Login();
        }
        public static void InitializeNetwork()
        {
            var serverCfg = Model.SettingsCur.Servercfg;
            string ip = serverCfg.LogginAddress.Split(':')[0];
            int port = int.Parse(serverCfg.LogginAddress.Split(':')[1]);
            networkClient = new NetworkClient(ip, port);
            networkClient.OnError += NetworkClient_OnError;
            networkClient.OnConnected += NetworkClient_OnConnected;
            networkClient.ClientEvents += NetworkClient_ClientEvents;
            Task.Run(() => { networkClient.StartClient(); });
        }
        static void NetworkClient_OnConnected(object sender, EventArgs e)
        {
            //ChangeStatus(true);
        }
        static void NetworkClient_ClientEvents(object sender, NetworkClientEventArgs e)
        {
            switch (e.CState)
            {
                case NetworkClientEventArgs.Callback.CRYPTO_KEY_INFORM:
                    //EnableLoginBtn(true);
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_WRONG_LOGIN:
                    //EnableLoginBtn(true);
                    //MessageBox.Show("Wrong Login", "Error");
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_WRONG_PW:
                    //EnableLoginBtn(true);
                    //MessageBox.Show("Wrong Password", "Error");
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_SERVER_CLOSED:
                    //EnableLoginBtn(true);
                    //MessageBox.Show("Login Server Closed, Contact Admin", "Error");
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_BANNED:
                    //MessageBox.Show("Account Blocked", "Error");
                    break;
                case NetworkClientEventArgs.Callback.SERVER_LIST_INFORM:
                    FillServerList(e.Servers);
                    break;
                case NetworkClientEventArgs.Callback.SERVER_SESSION_RESULT:
                    RunGame(e.DefaultSet);
                    break;
            }
        }
        static void FillServerList(List<ServerState> _serverList)
        {
            Task.Run(()=>{ networkClient.SelectWordlRequest(Model.SettingsCur.Servercfg.ServerIndexSelect); });

        }
        static void NetworkClient_OnError(object sender, EventArgs e)
        {
          //  ChangeStatus(false);
        }
        static void RunGame(Default_Set defaultSet)
        {
            defaultSet.nation_code = 3;
            ClientRunHelper.WriteTmp(@"System/DefaultSet.tmp", defaultSet);
            ClientRunHelper.RunClient(@"RF_Online.bin");
            networkClient.StopListen();
            Environment.Exit(0);
        }
        static void Login()
        {
            Task.Run(() =>
            {
                Thread.Sleep(5000);
                networkClient.DoLogin("jett", "NVcjk347dl");
            });
        }
    }
}
