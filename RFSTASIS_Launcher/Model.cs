using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Net;

namespace RFSTASIS_Launcher
{
    class Model
    {
        static public void Start()
        {
            //Hash.CompareFiles();
            var test = Server.GetStream("http://update1.rfstasis.com/clientpatch/Adv.dll");
            using var file2 = File.OpenRead(@"D:\WORK\RFSTASIS_Launcher\Adv.dll");
            var res = MD5Hash.Compare(test, file2);
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
                            if (Convert.ToBase64String(hash1) == Convert.ToBase64String(hash2))
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
    }
}
