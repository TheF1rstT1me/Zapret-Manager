using System.Diagnostics;
using ZapretManager.Core_.Interfaces;

namespace ZapretManager.Infrastructure_
{
    public class AutoLoadService: IAutoLoadService
    {
        private const string AppName = "ZapretManager";

        private static string GetExePath() =>
            Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule!.FileName;

        public void EnableAutostart()
        {
            #if DEBUG
                return;
            #endif

            var exePath = GetExePath();
            var args = $"/create /tn \"{AppName}\" /tr \"\\\"{exePath}\\\"\" /sc onlogon /delay 0000:30 /f";

            Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }

        public void DisableAutostart()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/delete /tn \"{AppName}\" /f",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }

        public bool IsAutostartEnabled()
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/query /tn \"{AppName}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            })!;

            process.WaitForExit();
            return process.ExitCode == 0;
        }
    }
}