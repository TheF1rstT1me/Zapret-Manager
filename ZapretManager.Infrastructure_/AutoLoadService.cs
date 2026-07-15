using Microsoft.Win32;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Diagnostics;
using System.Windows.Forms;
using ZapretManager.Core_.Exceptions;
using ZapretManager.Core_.Interfaces;
using ZapretManager.Core_.Models;

namespace ZapretManager.Infrastructure_
{
    public class AutoLoadService: IAutoLoadService
    {
        private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "ZapretManager";

        private static string GetExePath() =>
            Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule!.FileName;

        public void EnableAutostart()
        {
            #if DEBUG
                return;
            #endif

            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
            key?.SetValue(AppName, $"\"{GetExePath()}\"");
        }

        public void DisableAutostart()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
            key?.DeleteValue(AppName, throwOnMissingValue: false);
        }

        public bool IsAutostartEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            return key?.GetValue(AppName) != null;
        }
    }
}