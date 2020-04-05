using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Diagnostics;
using MiniLauncher.Data;

namespace RFSTASIS_Launcher
{
    class Model
    {
        public static Settings SettingsCur = Settings.Deserialize();
        public static GameClient GClient = new GameClient();
        public static string ExecutionFileName { get; } = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
        
        static public void Start()
        {
            GClient.GetUpdates();
        }
        public class Settings
        {
            public string WebClientPath { get; set; }
            public List<string> FilesToIgnore { get; set; }
            public List<string> FilesToNotUpdate { get; set; }
            public ParallelOptions HashOptions { get; set; }
            public ParallelOptions DownloadOptions { get; set; }
            public ServerSetting Servercfg { get; set; }
            public static Settings Deserialize(string path = "Settings.json")
            {
                if (File.Exists(path))
                {
                    var cset = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
                    if (cset.FilesToIgnore != null && cset.FilesToNotUpdate != null && cset.WebClientPath != null && cset.Servercfg.IsCorrect)
                        return cset;
                }
                var set = new Settings
                {
                    WebClientPath = "http://update1.rfstasis.com/clientpatch/",
                    FilesToIgnore = new List<string> {
                        "MD5 Hash Updater.deps.json","MD5 Hash Updater.dll","MD5 Hash Updater.exe","MD5 Hash Updater.pdb","MD5 Hash Updater.runtimeconfig.dev.json","MD5 Hash Updater.runtimeconfig.json"
                        ,"Exceptions.txt","NetLog\\rfclient.log","NetLog\\Odin.log","NetLog\\EffectLog.log","NetLog\\Client-Net.log","NetLog\\CEngineLog.log","NetLog\\Critical.Log","ClientSettings.json",
                        "RFStasis_Launcher.exe",
                    },
                    FilesToNotUpdate = new List<string> { "R3Engine.ini", "System\\DefaultSet.tmp" },
                    Servercfg = new ServerSetting
                    {
                        LogginAddress = "64.31.6.86:10001",
                        OverrideServerAddress = false,
                        OverrideServerSelection = true,
                        ServerAddress = "64.31.6.86:27780",
                        ServerIndexSelect = 0
                    },
                    HashOptions = new ParallelOptions { MaxDegreeOfParallelism=4},
                    DownloadOptions = new ParallelOptions(),
                };
                set.Serialize();
                return set;
            }
            public void Serialize(string path = "Settings.json")
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(this));
            }
        }

        public class MD5Hash
        {
            public static string GetHash(Stream file)
            {
                try
                {
                    using (var md5hash = MD5.Create())
                    {
                        var hash = md5hash.ComputeHash(file);
                        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                    }
                }
                finally
                {
                    if (file != null)
                        file.Close();
                }
            }
            public static bool Compare(Stream file1, Stream file2)
            {
                try
                {
                    using (var md5hash1 = MD5.Create())
                    {
                        using (var md5hash2 = MD5.Create())
                        {
                            var hash1 = md5hash1.ComputeHash(file1);
                            var hash2 = md5hash2.ComputeHash(file2);
                            if (BitConverter.ToString(hash1).Replace("-", string.Empty).ToLower() == BitConverter.ToString(hash2).Replace("-", string.Empty).ToLower())
                                return true;
                            return false;
                        }
                    }
                }
                finally
                {
                    if (file1 != null)
                        file1.Close();
                    if (file2 != null)
                        file2.Close();
                }
            }
        }
    }
    public class Server : VMBase
    {
        BlockingCollection<string> Paths { get; set; } = new BlockingCollection<string> { Model.SettingsCur.WebClientPath };
        BlockingCollection<string> FilesLink { get; set; } = new BlockingCollection<string>();
        int _FilesToDownloadCount = 0;
        public int FilesToDownloadCount {
            get => _FilesToDownloadCount;
            set
            {
                _FilesToDownloadCount = value;
                OnPropertyChanged();
                OnPropertyChanged("NeedUpdate");
            }
        }
        bool _NeedUpdate = false;
        public bool NeedUpdate
        {
            get
            {
                if (FilesToDownloadCount > 0)
                    return true;
                return false;
            }
        }
        public int DownloadedFiles { get; set; } = 0;
        int _DownloadProgres = 0;
        public int DownloadProgres
        {
            get => _DownloadProgres;
            set
            {
                _DownloadProgres = value;
                OnPropertyChanged();
            }
        }
        string _DownloadedFileName ="";
        public string DownloadedFileName
        {
            get => _DownloadedFileName;
            set
            {
                _DownloadedFileName = value;
                OnPropertyChanged();
            }
        }
        public Stream GetStream(string Url)
        {
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(Url);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            var str = Response.GetResponseStream();
            return str;
        }
        public async Task<string> DownloadPage(string url)
        {
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var code = response.StatusCode;
                    if (code == HttpStatusCode.OK)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        return responseBody;
                    }
                }
            }
            return null;
        }
       
        public byte[] DownloadHashFile()
        {
            try
            {
                using (var wc = new WebClient())
                    return wc.DownloadData(Model.SettingsCur.WebClientPath + "HashSum.hs");
            }
            catch (Exception exc)
            {
                throw new Exception("No access to server", exc);
            }
        }
        public void Download(ConcurrentBag<FileInfoContainer> Files, string path = ".\\")
        {
            object locker = new object();
            FilesToDownloadCount = Files.Count;
            Parallel.ForEach(Files, Model.SettingsCur.DownloadOptions, file =>
               {
                   var cfile = (file as FileInfoContainer);
                   try
                   {
                       string webpath = WebUtility.UrlDecode(Model.SettingsCur.WebClientPath + cfile.FilePath.Replace("\\", "/"));
                       var filepath = new FileInfo(path + cfile.FilePath);
                       if (!filepath.Directory.Exists)
                           filepath.Directory.Create();
                       var pathtosave = filepath.FullName;
                       if (cfile.Name.ToLower() == Model.ExecutionFileName.ToLower())
                       {
                           pathtosave = pathtosave.Replace(cfile.Name, $"[NEW]{cfile.Name}");
                       }
                       using (var wc = new WebClient())
                           wc.DownloadFile(webpath, pathtosave);
                   }
                   catch (Exception exc)
                   {
                       new Exception($"Can't Download file {cfile.Name}", exc).Write();
                   }
                   finally
                   {
                       lock(locker)
                       {
                           DownloadedFiles++;
                           DownloadedFileName = cfile.FilePath;
                           var test  = (int)((double)DownloadedFiles / (double)FilesToDownloadCount * 100);
                           DownloadProgres = test;
                       }
                       //System.Threading.Interlocked.Increment(ref DownloadedFiles.Value);
                   }
               });
        }
    }
    public class FileInfoContainer
    {
        public string Name { get; set; }
        string _FilePath;
        public string FilePath
        {
            get => _FilePath;
            set
            {
                _FilePath = Path.GetRelativePath(Environment.CurrentDirectory, value);
            }
        }
        public DateTime DateEdit { get; set; }
        public string MD5Hash { get; set; }
        public static void Write(ConcurrentBag<FileInfoContainer> collection, string PathToWrtie = ".\\HashSum.hs")
        {
            File.WriteAllText(PathToWrtie, JsonConvert.SerializeObject(collection));
        }
        public static ConcurrentBag<FileInfoContainer> Read(string PathToRead = ".\\HashSum.hs")
        {
            return JsonConvert.DeserializeObject<ConcurrentBag<FileInfoContainer>>(File.ReadAllText(PathToRead));
        }
        public static ConcurrentBag<FileInfoContainer> Read(byte[] array)
        {
            return JsonConvert.DeserializeObject<ConcurrentBag<FileInfoContainer>>(Encoding.Default.GetString(array));
        }
    }
}
