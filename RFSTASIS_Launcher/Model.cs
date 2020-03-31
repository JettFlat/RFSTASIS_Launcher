using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace RFSTASIS_Launcher
{
    class Model
    {
        static public void Start()
        {
            Hash.CompareFiles();
        }
        public class Hash
        {
            static public void CompareFiles()
            {
                using (var file1 = File.OpenRead(@"D:\WORK\RFSTASIS_Launcher\Adv.dll"))
                {
                    using (var file2 = File.OpenRead(@"D:\WORK\RFSTASIS_Launcher\Adv2.dll"))
                    {
                        var res = HashCompare(file1, file2);
                    }

                }
            }
            static bool HashCompare(Stream file1, Stream file2)
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
        }
    }
}
