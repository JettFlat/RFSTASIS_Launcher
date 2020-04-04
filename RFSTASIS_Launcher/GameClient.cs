using MiniLauncher.Helper;
using MiniLauncher.Network;
using MiniLauncher.Network.Packets;
using Newtonsoft.Json;
using rlgn;
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
    public class GameClient : VMBase
    {
        public ClientSettings clientSettings { get; set; } = ClientSettings.Deserialize();
        NetworkClient networkClient;
        bool _IsServerOnline = false;
        public string ServerStatus
        {
            get
            {
                if (IsServerOnline)
                    return "ONLINE";
                else
                    return "OFFLINE";
            }
        }
        public bool IsServerOnline
        {
            get => _IsServerOnline;
            set
            {
                _IsServerOnline = value;
                OnPropertyChanged();
                OnPropertyChanged("ServerStatus");
            }
        }
        bool _IsResiveNow = false;
        public bool IsResiveNow
        {
            get => _IsResiveNow;
            set
            {
                _IsResiveNow = value;
                OnPropertyChanged();
            }
        }
        string _ServerMessage;
        public string ServerMessage
        {
            get => _ServerMessage;
            set
            {
                _ServerMessage = value;
                OnPropertyChanged();
            }
        }
        public string Path => Environment.CurrentDirectory;
        public ConcurrentBag<FileInfoContainer> GetFilesHash()
        {
            ConcurrentBag<FileInfoContainer> res = new ConcurrentBag<FileInfoContainer>();
            var files = Directory.GetFiles(Path, "", SearchOption.AllDirectories);
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 1 }, file =>
            {
                try
                {
                    var fi = new FileInfo(file);
                    var item = new FileInfoContainer { Name = fi.Name, FilePath = fi.FullName, DateEdit = fi.LastWriteTime };
                    using (var str = File.OpenRead(fi.FullName))
                        item.MD5Hash = MD5Hash.GetHash(str);
                    res.Add(item);
                }
                catch (Exception exc)
                {
                    new Exception($"Can't get hash of file{file}", exc).Write();
                }
            });
            return res;
        }
        public void GetUpdates()
        {
            var local = GetFilesHash();
            var server = FileInfoContainer.Read(Server.DownloadHashFile());
            List<FileInfoContainer> toupdate = new List<FileInfoContainer>(server.Where(s => local.FirstOrDefault(l => s.FilePath == l.FilePath)?.MD5Hash != s.MD5Hash));
            var remove = SettingsCur.FilesToIgnore.Concat(SettingsCur.FilesToNotUpdate).Distinct().ToList();
            toupdate = toupdate.Where(x => !remove.Any(y => y == x.FilePath)).ToList();
            var tmp = SettingsCur.FilesToNotUpdate.Except(local.Select(y => y.FilePath)).ToList();
            var todownload = server.Where(s => tmp.Any(l => s.FilePath == l)).ToList();
            toupdate.AddRange(todownload);
            Server.Download(new ConcurrentBag<FileInfoContainer>(toupdate));
            if (File.Exists($"[NEW]{ExecutionFileName}"))
            {
                var id = Process.GetCurrentProcess().Id;
                try
                {
                    var args = $"{id} \"{ExecutionFileName}\" \"[NEW]{ExecutionFileName}\"";
                    Process.Start("Replacer.exe", args);
                    Environment.Exit(0);
                }
                catch (Exception exc)
                {
                    new Exception("Can't start Replacer.exe", exc).Write();
                }
            }
        }
        public void Start()
        {
            IsResiveNow = true;
            Login();
        }
        public void InitializeNetwork()
        {
            var serverCfg = Model.SettingsCur.Servercfg;
            string ip = "";
            int port;
            try
            {
                ip = serverCfg.LogginAddress.Split(':')[0];
                port = int.Parse(serverCfg.LogginAddress.Split(':')[1]);
            }
            catch (Exception exc)
            {
                throw new Exception("Incorrect IP and Port", exc);
            }
            networkClient = new NetworkClient(ip, port);
            networkClient.OnError += NetworkClient_OnError;
            networkClient.OnConnected += NetworkClient_OnConnected;
            networkClient.ClientEvents += NetworkClient_ClientEvents;
            Task.Run(() => { networkClient.StartClient(); });
        }
        void NetworkClient_OnConnected(object sender, EventArgs e)
        {
            IsServerOnline = true;
        }
        void NetworkClient_ClientEvents(object sender, NetworkClientEventArgs e)
        {
            switch (e.CState)
            {
                case NetworkClientEventArgs.Callback.CRYPTO_KEY_INFORM:
                    IsResiveNow = false;
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_WRONG_LOGIN:
                    IsResiveNow = false;
                    ServerMessage = "Wrong Login";
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_WRONG_PW:
                    IsResiveNow = false;
                    ServerMessage = "Wrong Password";
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_SERVER_CLOSED:
                    IsResiveNow = false;
                    ServerMessage = "Login Server Closed, Contact Admin";
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_BANNED:
                    IsResiveNow = false;
                    ServerMessage = "Account Blocked";
                    break;
                case NetworkClientEventArgs.Callback.SERVER_LIST_INFORM:
                    IsResiveNow = true;
                    FillServerList(e.Servers);
                    break;
                case NetworkClientEventArgs.Callback.SERVER_SESSION_RESULT:
                    IsResiveNow = true;
                    RunGame(e.DefaultSet);
                    break;
            }
        }
        void FillServerList(List<ServerState> _serverList)
        {
            Task.Run(() => { networkClient.SelectWordlRequest(Model.SettingsCur.Servercfg.ServerIndexSelect); });
        }
        void NetworkClient_OnError(object sender, EventArgs e)
        {
            IsServerOnline = false;
        }
        void RunGame(Default_Set defaultSet)
        {
            try
            {
                defaultSet.nation_code = 3;
                ClientRunHelper.WriteTmp(@"System/DefaultSet.tmp", defaultSet);
                ClientRunHelper.RunClient(@"RF_Online.bin");
                networkClient.StopListen();
            }
            catch (Exception exc)
            {
                exc.Write();
                exc.ShowMessage();
            }
            finally
            {
                Environment.Exit(0);
            }
        }
        void Login()
        {
            Task.Run(() =>
            {
                Thread.Sleep(5000);
                networkClient.DoLogin(clientSettings.NickName, clientSettings.Password);
            });
        }
        public class ClientSettings
        {
            public string NickName { get; set; }
            public string Password { get; set; }
            public EngineSettings engineSettings { get; set; }
            public static ClientSettings Deserialize(string path = "ClientSettings.json")
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var settings = JsonConvert.DeserializeObject<ClientSettings>(File.ReadAllText(path));
                        return settings;
                    }
                    catch (Exception) { }
                }
                var set = new ClientSettings
                {
                    NickName = "",
                    Password = "",
                    engineSettings = new EngineSettings(),
                };
                set.engineSettings.Read();
                set.Serialize();
                return set;
            }
            public void Serialize(string path = "ClientSettings.json")
            {
                var tosave = this;
                tosave.Password = "";
                File.WriteAllText(path, JsonConvert.SerializeObject(tosave));
            }
            public void WriteEngineSettings()
            {
                engineSettings.SaveSettings();
            }
        }
    }
}
