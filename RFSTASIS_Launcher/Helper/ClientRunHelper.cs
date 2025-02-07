﻿using MiniLauncher.Network.BinaryConverter;
using MiniLauncher.Network.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace MiniLauncher.Helper
{
    public static class ClientRunHelper
    {
        public static void WriteTmp(string path, Default_Set data)
        {
            if (!File.Exists(path))
            {
                File.Create(path);
            }
            data.encrypt();
            byte[] bDefaultSet = BinaryStructConverter.ToByteArray(data);
            File.WriteAllBytes(path, bDefaultSet);
        }
        public static void RunClient(string filepath)
        {
            Process clientProcess = new Process();
            clientProcess.StartInfo.CreateNoWindow = false;
            clientProcess.StartInfo.FileName = filepath;
            clientProcess.StartInfo.WorkingDirectory = ".\\";
            clientProcess.StartInfo.UseShellExecute = false;
            clientProcess.Start();
        }
    }
}
