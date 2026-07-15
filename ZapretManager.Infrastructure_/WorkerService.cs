using SharpCompress.Archives;
using SharpCompress.Common;
using System.Diagnostics;
using System.Windows.Forms;
using ZapretManager.Core_.Exceptions;
using ZapretManager.Core_.Interfaces;
using ZapretManager.Core_.Models;

namespace ZapretManager.Infrastructure_
{
    public class WorkerService(IGitHubService gitHubService, ISourceForgeService sourceForgeService) : IWorkerService
    {
        private readonly IGitHubService _gitHubService = gitHubService;
        private readonly ISourceForgeService _sourceForgeService = sourceForgeService;

        private CancellationTokenSource _downloadCtsToken = new();
        private bool _isBusy = false;

        private string ValidateProcessName(string process)
        {
            bool isExist = Core_.Models.Constants.processesNames.TryGetValue(process, out string? processName);
            if (isExist) return processName!;
            return string.Empty;
        }

        private async Task<string?> DownloadAsset((string GitHubRepo, string SourceForgeProject) urlAuthor, string TagNameVersion, string savePath)
        {
           _downloadCtsToken.TryReset();

            try
            {
                IEnumerable<GitRelease?> releases = await _gitHubService.GetReleasesAsync(urlAuthor.GitHubRepo);

                GitRelease? firstRelease = releases.FirstOrDefault();
                if (firstRelease == null || firstRelease.Assets.Count == 0) return null;

                GitRelease? release = releases.FirstOrDefault(r => r?.TagName == TagNameVersion);
                if (release == null) return null;

                GitAsset? asset = release.Assets.FirstOrDefault(a => a.DownloadUrl.Contains(".zip", StringComparison.OrdinalIgnoreCase) || a.DownloadUrl.Contains(".rar", StringComparison.OrdinalIgnoreCase) || a.DownloadUrl.Contains("_windows.exe", StringComparison.OrdinalIgnoreCase));
                if (asset == null) return null;

                string fileName = Path.GetFileName(new Uri(asset.DownloadUrl).LocalPath);
                string fullFilePath = Path.Combine(savePath, fileName);

                return await _gitHubService.DownloadAssetAsync(asset.DownloadUrl, fullFilePath, _downloadCtsToken.Token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (GitHubServiceException ex)
            {
                try {
                    IEnumerable<SourceForgeVersionRelease> releases = await _sourceForgeService.GetLatestVersionsAsync(urlAuthor.SourceForgeProject, 5);

                    SourceForgeVersionRelease? firstRelease = releases.FirstOrDefault();
                    if (firstRelease == null) return null;

                    SourceForgeVersionRelease? release = releases.FirstOrDefault(r => r.Version == TagNameVersion);
                    if (release == null) return null;

                    string? downloadUrl = release.RarUrl ?? release.ZipUrl ?? release.ExeUrl;
                    if (downloadUrl == null) return null;

                    string fileName = Path.GetFileName(new Uri(downloadUrl.Replace("/download", "", StringComparison.OrdinalIgnoreCase)).LocalPath);
                    string fullFilePath = Path.Combine(savePath, fileName);

                    return await _sourceForgeService.DownloadAssetAsync(downloadUrl, fullFilePath, _downloadCtsToken.Token);
                } catch (OperationCanceledException)
                {
                    return null;
                }  catch (Exception _ex)
                {
   //                 MessageBox.Show($"Ошибка: {_ex.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
    //            MessageBox.Show($"Ошибка: {ex.Message}");
                return null;
            }
        }

        private async Task<(bool, string?)> UpdateZapret(string FilePath, string TagNameVersion, bool IsSaveDomains)
        {
            DirectoryInfo? directoryInfo = Directory.Exists(FilePath) ? new DirectoryInfo(FilePath) : new FileInfo(FilePath).Directory;
            if (directoryInfo == null) return (false, null);

            string ListsSaved = "";

            await StopProcesses(Core_.Models.Constants.processesNames["Zapret"]);

            string? downloadedFile = await DownloadAsset(Core_.Models.Constants.repos["Zapret"], TagNameVersion, Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            if (downloadedFile == null) return (false, null);

            bool altsHavent = await CheckAltsZapret(directoryInfo);
            if (altsHavent)
            {
                await Task.Run(() =>
                {
                    foreach (FileInfo file in directoryInfo.EnumerateFiles())
                    {
                        file.Delete();
                    }

                    if (IsSaveDomains)
                    {
                        ListsSaved = File.ReadAllText(Path.Combine(directoryInfo.FullName, "lists", "list-general.txt"));
                    }

                    foreach (DirectoryInfo directory in directoryInfo.EnumerateDirectories())
                    {
                        directory.Delete(recursive: true);
                    }

                    return 0;
                });
            }

            try
            {
                bool extracted = await ExtractArchiveAsync(downloadedFile, directoryInfo.FullName);
                if (ListsSaved != string.Empty) {
                    await File.WriteAllTextAsync(
                        Path.Combine(directoryInfo.FullName, "lists", "list-general.txt"), ListsSaved, System.Text.Encoding.UTF8
                    ); 
                }
                return (extracted, directoryInfo.FullName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка распаковки: {ex.Message}");
                return (false, null);
            }
        }

        private async Task<(bool, string?)> UpdateTgWsProxy(string FilePath, string TagNameVersion)
        {
            DirectoryInfo? directoryInfo = Directory.Exists(FilePath) ? new DirectoryInfo(FilePath) : new FileInfo(FilePath).Directory;
            if (directoryInfo == null) return (false, null);

            await StopProcesses(Core_.Models.Constants.processesNames["TgWsProxy"]);

            string? downloadedFile = await DownloadAsset(Core_.Models.Constants.repos["TgWsProxy"], TagNameVersion, Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            if (downloadedFile == null) return (false, null);

            bool proxyHavent = await CheckTgWsProxyExe(directoryInfo);
            if (proxyHavent)
            {
                await Task.Run(() =>
                {
                    foreach (FileInfo file in directoryInfo.EnumerateFiles())
                    {
                        file.Delete();
                    }

                    return 0;
                });
            }

            string downloadedFileName = Path.GetFileName(downloadedFile);

            try
            {
                File.Move(downloadedFile, Path.Combine(directoryInfo.FullName, downloadedFileName));
                return (true, Path.Combine(directoryInfo.FullName, downloadedFileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перемещения: {ex.Message}");
                return (false, null);
            }
        }

        private async Task<int?> StartProcess(string exePath, string arguments)
        {
            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = exePath,
                    Arguments = arguments,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                 //   CreateNoWindow = true,        // Скрыть окно консоли
                 //   UseShellExecute = false,      // Нужно для скрытия окна и перенаправления потоков
                 //   RedirectStandardOutput = true, // Позволяет читать логи процесса (если нужно)
                 //   RedirectStandardError = true
                };

                Process? process = Process.Start(startInfo);

                if (process != null)
                {
                    return process.Id;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска: {ex.Message}");
            }

            return null;
        }

        private async Task StopProcesses(string processName)
        {
            var runningProcesses = Process.GetProcesses()
                .Where(p => p.ProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase));

            foreach (var process in runningProcesses)
            {
                process.Kill();
                process.WaitForExit(1000);
            }
        }

        public async Task<bool> openProcess(string pathToFile, string processName)
        {
            if (!File.Exists(pathToFile)) return false;

            bool isApplicationRunning = Process.GetProcesses()
                .Any(p => p.ProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase));

            if (isApplicationRunning) {
                var runningProcesses = Process.GetProcesses()
                    .Where(p => p.ProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase));

                foreach (var process in runningProcesses)
                {
                    process.Kill();
                    process.WaitForExit(1000);
                }
            }

            await StartProcess(pathToFile, "");
            return true;
        }
    
        public async Task<bool> AutoOpen(ZapretSettings _settings, string mode)
        {
            bool isEnabled = GetAutoOpen(_settings, mode);
            
            if (isEnabled)
            {
                string FilePath = GetFilePath(_settings, mode);
                string ValidName = ValidateProcessName(mode);

                if (FilePath != string.Empty && ValidName != string.Empty 
                    && (FilePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || FilePath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
                ) return await openProcess(FilePath, ValidName);
                return false;
            }

            return isEnabled;
        }

        public async Task<bool> CheckAltsZapret(DirectoryInfo directoryInfo)
        {
            bool altsHavent = await Task.Run(() =>
            {
                return directoryInfo!.EnumerateDirectories()
                    .Any(d => d.Name.Contains("utils") ||
                              d.Name.Contains("bin") ||
                              d.Name.Contains("lists"));
            });

            return altsHavent;
        }

        public async Task<bool> CheckTgWsProxyExe(DirectoryInfo directoryInfo)
        {
            bool proxyHavent = await Task.Run(() =>
            {
                return directoryInfo!.EnumerateFiles()
                    .Any(f => f.Name.Contains("tgws", StringComparison.OrdinalIgnoreCase));
            });

            return proxyHavent;
        }

        public async Task<bool?> Update(ZapretSettings _settings, string mode, string TagNameVersion, Action<string>? onUpdated = null)
        {
            //           bool isEnabled = GetAutoUpdate(_settings, mode);
            //           if (isEnabled)

            if (_isBusy) return null;

            bool updated = false;
            string FilePath = GetFilePath(_settings, mode);

            if (FilePath == string.Empty) { MessageBox.Show($"Error of update: invalid filepath to {mode}."); return false; }

            _isBusy = true;

            (bool isSuccess, string? directory) updatedInfo;

            switch (mode)
            {
                case "Zapret":
                    updatedInfo = await UpdateZapret(FilePath, TagNameVersion, _settings.IsSaveDomainList);
                    if (updatedInfo.isSuccess) { updated = updatedInfo.isSuccess; onUpdated?.Invoke(updatedInfo.directory!); }
                    break;
                case "TgWsProxy":
                    updatedInfo = await UpdateTgWsProxy(FilePath, TagNameVersion);
                    if (updatedInfo.isSuccess) { updated = updatedInfo.isSuccess; onUpdated?.Invoke(updatedInfo.directory!); }
                    break;
            }

            _isBusy = false;
            return updated;
        }

        public async Task<bool> ExtractArchiveAsync(string archivePath, string destinationDirectory, bool deleteArchiveAfter = true)
        {
            if (!File.Exists(archivePath))
                throw new Exception($"Архив не найден: {archivePath}");

            try
            {
                await Task.Run(() =>
                {
                    using var archive = ArchiveFactory.OpenArchive(archivePath);

                    foreach (var entry in archive.Entries)
                    {
                        if (entry.IsDirectory) continue;

                        entry.WriteToDirectory(destinationDirectory, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                });

                // есть ли вложенная папка с именем архива
                string expectedNestedDirName = Path.GetFileNameWithoutExtension(archivePath);
                string nestedDirPath = Path.Combine(destinationDirectory, expectedNestedDirName);

                if (Directory.Exists(nestedDirPath))
                {
                    foreach (var entry in Directory.GetFileSystemEntries(nestedDirPath))
                    {
                        string entryName = Path.GetFileName(entry);
                        string destPath = Path.Combine(destinationDirectory, entryName);

                        if (Directory.Exists(entry))
                        {
                            if (Directory.Exists(destPath)) Directory.Delete(destPath, true);
                            Directory.Move(entry, destPath);
                        }
                        else
                        {
                            if (File.Exists(destPath)) File.Delete(destPath);
                            File.Move(entry, destPath);
                        }
                    }

                    Directory.Delete(nestedDirPath, recursive: true);
                }

                if (deleteArchiveAfter)
                {
                    File.Delete(archivePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка распаковки архива: {ex.Message}", ex);
            }
        }

        public bool GetAutoUpdate(ZapretSettings _settings, string mode)
        {

            return mode switch
            {
                "Zapret" => _settings.IsZapretAutoUpdate,
                "TgWsProxy" => _settings.IsTgWsProxyAutoUpdate,
                _ => false
            };
        }

        public string GetFilePath(ZapretSettings _settings, string mode)
        {
            return mode switch
            {
                "Zapret" => _settings.Paths.Zapret,
                "TgWsProxy" => _settings.Paths.TgWsProxy,
                _ => string.Empty
            };
        }

        public bool GetAutoOpen(ZapretSettings _settings, string mode)
        {

            return mode switch
            {
                "Zapret" => _settings.IsZapretAutoOpen,
                "TgWsProxy" => _settings.IsTgWsProxyAutoOpen,
                _ => false
            };
        }

        public string GetVersionInstalled(ZapretSettings _settings, string mode)
        {

            return mode switch
            {
                "Zapret" => _settings.Versions.ZapretInstalled,
                "TgWsProxy" => _settings.Versions.TgWsProxyInstalled,
                _ => string.Empty
            };
        }

        public string GetVersionInSettings(ZapretSettings _settings, string mode)
        {

            return mode switch
            {
                "Zapret" => _settings.Versions.Zapret,
                "TgWsProxy" => _settings.Versions.TgWsProxy,
                _ => string.Empty
            };
        }
    }
}