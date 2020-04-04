using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace RFSTASIS_Launcher
{
    public static class Extensions
    {
        public static object ExceptionLocker = new object();
        public static string ToLogFormat(this string text)
        {
            return $"[{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}] {text}{Environment.NewLine}";
        }
        public static void Write(this Exception exc, string path = "Exceptions.txt")
        {
            var file = new FileInfo(path);
            if (!file.Directory.Exists)
                file.Directory.Create();
            string text = $"[{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}]{Environment.NewLine}";
            var exception = exc;
            string space = "";
            var tmp = new Exception();
            do
            {
                text += $"{space}Message: {exception.Message}{Environment.NewLine}{space}Stack Trace:{exception.StackTrace}{Environment.NewLine}";
                tmp = exception;
                space += "    ";
                exception = exception.InnerException;
            }
            while (tmp.InnerException != null);
            lock (ExceptionLocker)
            {
                File.AppendAllText(path, text);
            }
        }
        public static void ShowMessage(this Exception exc)
        {
            MessageBox.Show(exc.Message, "Error");
        }

    }
}
