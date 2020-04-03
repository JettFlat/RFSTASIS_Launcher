using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RFSTASIS_Launcher.Model;

namespace RFSTASIS_Launcher
{
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
        public static void RunGame()
        {

        }
    }
}
