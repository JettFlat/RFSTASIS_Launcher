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
using AngleSharp;
using AngleSharp.Html.Dom;
using System.Diagnostics;

namespace RFSTASIS_Launcher
{
    class Model
    {
        static string ExecutionFileName { get; } = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
        public static Settings SettingsCur = Settings.Deserialize();
        static ParallelOptions parallelOptions = new ParallelOptions();// { MaxDegreeOfParallelism = 1 };
        static public void Start()
        {
            //var files = FileInfoContainer.Read(@"D:\WORK\RFSTASIS_Launcher\RFSTASIS_Launcher\bin\Debug\netcoreapp3.1\serverhash.hs");
            //Server.Parse();
            GameClient.GetUpdates();
        }
        public class Settings
        {
            public string WebClientPath { get; set; }
            public List<string> FilesToIgnore { get; set; }
            public List<string> FilesToNotUpdate { get; set; }
            public static Settings Deserialize(string path = "Settings.json")
            {
                if (File.Exists(path))
                    return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
                else
                {
                    var set = new Settings
                    {
                        WebClientPath = "http://update1.rfstasis.com/clientpatch/",
                        FilesToIgnore = new List<string> {
                        "MD5 Hash Updater.deps.json","MD5 Hash Updater.dll","MD5 Hash Updater.exe","MD5 Hash Updater.pdb","MD5 Hash Updater.runtimeconfig.dev.json","MD5 Hash Updater.runtimeconfig.json"
                        ,"Exceptions.txt","NetLog\\rfclient.log","NetLog\\Odin.log","NetLog\\EffectLog.log","NetLog\\Client-Net.log","NetLog\\CEngineLog.log","NetLog\\Critical.Log"
                    },
                        FilesToNotUpdate = new List<string> { "R3Engine.ini", "System\\DefaultSet.tmp" }
                    };
                    set.Serialize();
                    return set;
                }
            }
            public void Serialize(string path = "Settings.json")
            {

                File.WriteAllText(path, JsonConvert.SerializeObject(this));

            }
        }

        public class Server
        {
            static BlockingCollection<string> Paths { get; set; } = new BlockingCollection<string> { SettingsCur.WebClientPath };
            static BlockingCollection<string> FilesLink { get; set; } = new BlockingCollection<string>();
            public static Stream GetStream(string Url)
            {
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(Url);
                HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
                var str = Response.GetResponseStream();
                return str;
            }
            public static async Task<string> DownloadPage(string url)
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
            public static void Parse()
            {
                while (Paths.Count > 0)
                {
                    string url = Paths.Take();
                    try
                    {
                        var page = new WebClient().DownloadString(url);
                        var config = Configuration.Default;
                        var context = BrowsingContext.New(config);
                        var document = context.OpenAsync(req => req.Content(page)).Result;
                        var list = document.QuerySelectorAll("a").ToList();
                        list.RemoveRange(0, 5);
                        Parallel.ForEach(list, parallelOptions, o =>
                        {
                            var text = (o as AngleSharp.Dom.IElement)?.Attributes[0]?.Value;
                            var textdecoded = WebUtility.UrlDecode(text);
                            var cpath = url + text;
                            if (SettingsCur.FilesToIgnore.Any(x => textdecoded.ToLower().Contains(x.ToLower())))
                                return;
                            if (text.Contains('/'))
                            {
                                Paths.Add(cpath);
                            }
                            else
                            {
                                FilesLink.Add(cpath);
                            }
                        });
                    }
                    catch (Exception exc)
                    {

                    }
                }
                File.WriteAllText("FilesLink.txt", JsonConvert.SerializeObject(FilesLink));
            }
            public static byte[] DownloadHashFile()
            {
                try
                {
                    using (var wc = new WebClient())
                        return wc.DownloadData(SettingsCur.WebClientPath + "HashSum.hs");
                }
                catch (Exception exc)
                {
                    throw new Exception("No access to server", exc);
                }
            }
            public static void Download(ConcurrentBag<FileInfoContainer> Files, string path = ".\\")
            {
                Parallel.ForEach(Files, parallelOptions, file =>
                   {
                       try
                       {
                           var cfile = (file as FileInfoContainer);
                           string webpath = WebUtility.UrlDecode(SettingsCur.WebClientPath + cfile.FilePath.Replace("\\", "/"));
                           var filepath = new FileInfo(path + cfile.FilePath);
                           if (!filepath.Directory.Exists)
                               filepath.Directory.Create();
                           var pathtosave = filepath.FullName;
                           if (cfile.Name.ToLower() == ExecutionFileName.ToLower())
                           {
                               pathtosave = pathtosave.Replace(cfile.Name, $"[NEW]{cfile.Name}");
                           }
                           using (var wc = new WebClient())
                               wc.DownloadFile(webpath, pathtosave);
                       }
                       catch (Exception exc)
                       {

                       }
                   });
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
        public class GameClient
        {
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
                //var server = FileInfoContainer.Read(@"D:\WORK\RFSTASIS_Launcher\RFSTASIS_Launcher\bin\Debug\netcoreapp3.1\serverhash.hs");
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
                    catch(Exception exc)
                    {

                    }
                }
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
}
