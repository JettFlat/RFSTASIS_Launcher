using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RFSTASIS_Launcher
{
    class Model
    {
        static public void Start()
        {
            //Hash.CompareFiles();
            //var test = Server.GetStream("http://update1.rfstasis.com/clientpatch/Character/Monster/Tex/EXILET.RFS");
            //using var file2 = File.OpenRead(@"D:\temp\OldLauncher\oldlaunchercode\New Launcher\rlgn\bin\Debug\Character\Monster\Tex\EXILET.RFS");
            //var hash = MD5Hash.GetHash(file2);
            //var test = Path.GetRelativePath(@"D:\temp\OldLauncher\oldlaunchercode\New Launcher\rlgn\bin\Debug", @"D:\temp\OldLauncher\oldlaunchercode\New Launcher\rlgn\bin\Debug\Character\Monster\Tex\EXILET.RFS");
            var res = GameClient.GetFilesHash();
            FileInfoContainer.Write(res);
        }
        public class Server
        {
            static public Stream GetStream(string Url)
            {
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(Url);
                HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
                var str = Response.GetResponseStream();
                return str;
            }
            //public static IEnumerable ParseFiles(string Url)
            //{

            //    yield return;
            //}
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
                        file.DisposeAsync();
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
                        file1.DisposeAsync();
                    if (file2 != null)
                        file2.DisposeAsync();
                }
            }
        }
        public class GameClient
        {
            public static string Path => @"D:\temp\OldLauncher\oldlaunchercode\New Launcher\rlgn\bin\Debug";
            public static List<FileInfoContainer> GetFilesHash()
            {
                List<FileInfoContainer> res = new List<FileInfoContainer>();
                var files = Directory.GetFiles(Path, "", SearchOption.AllDirectories);
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 1 }, file => {
                    var fi = new FileInfo(file);
                    var item = new FileInfoContainer { Name = fi.Name, FilePath = fi.FullName, DateEdit = fi.LastWriteTime };
                    using (var str = File.OpenRead(fi.FullName))
                        item.MD5Hash = MD5Hash.GetHash(str);
                    res.Add(item);
                });
                return res;
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
                    _FilePath = Path.GetRelativePath(value, Environment.CurrentDirectory);
                }
            }
            public DateTime DateEdit { get; set; }
            public string MD5Hash { get; set; }
            public static void Write(List<FileInfoContainer> collection, string PathToWrtie = ".\\HashSum.hs")
            {
                File.WriteAllText(PathToWrtie, JsonConvert.SerializeObject(collection));
                //File.WriteAllLines(PathToWrtie, collection.Select(x => $"{x.MD5Hash} * {x.Name} * {x.DateEdit.ToString("dd/MM/yyyy HH:mm")} * {x.Path}"));
            }
            public static List<FileInfoContainer> Read(string PathToRead = ".\\HashSum.hs")
            {
                return JsonConvert.DeserializeObject<List<FileInfoContainer>>(File.ReadAllText(PathToRead));
            }
        }
    }
}
