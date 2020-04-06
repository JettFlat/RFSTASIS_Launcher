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
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static RFSTASIS_Launcher.Model;

namespace RFSTASIS_Launcher
{
    public class GameClient : VMBase
    {
        public ClientSettings clientSettings { get; set; } = ClientSettings.Deserialize();
        public Server Servak = new Server();
        NetworkClient networkClient;

        bool _IsUpdateComplete = false;
        public bool IsUpdateComplete
        {
            get => _IsUpdateComplete;
            set
            {
                _IsUpdateComplete = value;
                OnPropertyChanged();
                OnPropertyChanged("CanLogin");
            }
        }
        public bool CanLogin
        {
            get
            {
                return (IsUpdateComplete && !IsResiveNow);
            }
        }
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
                OnPropertyChanged("CanLogin");
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
        public GameClient()
        {
            Servak.PropertyChanged += (s, e) => { OnPropertyChanged(e.PropertyName); };
        }
        public ConcurrentBag<FileInfoContainer> GetFilesHash()
        {
            ConcurrentBag<FileInfoContainer> res = new ConcurrentBag<FileInfoContainer>();
            var files = Directory.GetFiles(Path, "", SearchOption.AllDirectories);
            Parallel.ForEach(files, Model.SettingsCur.HashOptions, file =>
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
            var server = FileInfoContainer.Read(Servak.DownloadHashFile());
            List<FileInfoContainer> toupdate = new List<FileInfoContainer>(server.Where(s => local.FirstOrDefault(l => s.FilePath == l.FilePath)?.MD5Hash != s.MD5Hash));
            var remove = SettingsCur.FilesToIgnore.Concat(SettingsCur.FilesToNotUpdate).Distinct().ToList();
            toupdate = toupdate.Where(x => !remove.Any(y => y == x.FilePath)).ToList();
            var tmp = SettingsCur.FilesToNotUpdate.Except(local.Select(y => y.FilePath)).ToList();
            var todownload = server.Where(s => tmp.Any(l => s.FilePath == l)).ToList();
            toupdate.AddRange(todownload);
            Servak.Download(new ConcurrentBag<FileInfoContainer>(toupdate));
            if (File.Exists($"[NEW]{ExecutionFileName}"))
            {
                var id = Process.GetCurrentProcess().Id;
                try
                {
                    var args = $"{id} \"{ExecutionFileName}\" \"[NEW]{ExecutionFileName}\"";
                    var filename = "Replacer.exe";
                    if (!File.Exists(filename))
                    {
                        var obj = GetStreamFromEmbeddedResources(filename);
                        using (var fileStream = File.Create(filename))
                        {
                            obj.CopyTo(fileStream);
                        }
                        obj.Close();
                    }
                    Process.Start("Replacer.exe", args);
                    Environment.Exit(0);
                }
                catch (Exception exc)
                {
                    new Exception("Can't start Replacer.exe", exc).Write();
                }
            }
            IsUpdateComplete = true;
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
                        settings.engineSettings.GraphicsAdapter = GetGPU();
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
            static string GetGPU()
            {
                ManagementObjectSearcher search = new ManagementObjectSearcher("select * from Win32_VideoController");
                var gpulist = new List<string>();
                foreach (ManagementObject mo in search.Get())
                {
                    gpulist.Add(mo.GetPropertyValue("Name").ToString());
                }
                return gpulist.FirstOrDefault();
            }
            [DllImport("user32.dll")]
            static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);
            [StructLayout(LayoutKind.Sequential)]
            struct DEVMODE
            {
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string dmDeviceName;
                public short dmSpecVersion;
                public short dmDriverVersion;
                public short dmSize;
                public short dmDriverExtra;
                public int dmFields;
                public int dmPositionX;
                public int dmPositionY;
                public int dmDisplayOrientation;
                public int dmDisplayFixedOutput;
                public short dmColor;
                public short dmDuplex;
                public short dmYResolution;
                public short dmTTOption;
                public short dmCollate;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string dmFormName;
                public short dmLogPixels;
                public int dmBitsPerPel;
                public int dmPelsWidth;
                public int dmPelsHeight;
                public int dmDisplayFlags;
                public int dmDisplayFrequency;
                public int dmICMMethod;
                public int dmICMIntent;
                public int dmMediaType;
                public int dmDitherType;
                public int dmReserved1;
                public int dmReserved2;
                public int dmPanningWidth;
                public int dmPanningHeight;
            }
            public static string GetResolution()
            {
                try
                {
                    const int ENUM_CURRENT_SETTINGS = -1;
                    DEVMODE devMode = default;
                    EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode);
                    var width = devMode.dmPelsWidth;
                    var height = devMode.dmPelsHeight;
                    return $"{width}×{height}";
                }
                catch (Exception) { }
                return "800×600";
            }
            public static List<string> GetResolutions()
            {
                var res = GetResolution();
                string textres = "320×240:352×240:352×288:400×240:480×576:640×240:640×360:640×480:800×480:800×600:854×480:1024×600:1024×768:1152×864:1200×600:1280×768:1366×768:1280×1024:1440×900:1400×1050:1536×960:1536×1024:1600×900:1600×1024:1600×1200:1680×1050:1920×1080:1920×1200:2048×1152:2048×1536:2560×1440:2560×1600:3200×2048:3200×2400:3840×2400:3840×2160:5120×4096:6400×4096:6400×4800:7680×4320:7680×4800";
                var allres = textres.Split(':').ToList();
                if (allres.Contains(res))
                {
                    var ind = allres.IndexOf(res);
                    var result = allres.Take(ind + 1).ToList();
                    return result;
                }
                return allres.Take(allres.IndexOf("800×600") + 1).ToList();
            }
        }
    }
}
