using ZapretManager.Core_.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace ZapretManager.Core_.Interfaces

{
    public interface IWorkerService
    {
        public Task<bool> openProcess(string pathToFile, string processName);
        
        public Task<bool> AutoOpen(ZapretSettings _settings, string mode);

        public Task<bool> CheckTgWsProxyExe(DirectoryInfo directoryInfo);

        public Task<bool> CheckAltsZapret(DirectoryInfo directoryInfo);

        public Task<bool?> Update(ZapretSettings _settings, string mode, string TagNameVersion, Action<string>? onUpdated = null);

        public Task<bool> ExtractArchiveAsync(string archivePath, string destinationDirectory, bool deleteArchiveAfter = true);

        public bool GetAutoOpen(ZapretSettings _settings, string mode);

        public string GetFilePath(ZapretSettings _settings, string mode);

        public bool GetAutoUpdate(ZapretSettings _settings, string mode);

        public string GetVersionInSettings(ZapretSettings _settings, string mode);

        public string GetVersionInstalled(ZapretSettings _settings, string mode);
    }

    public interface IGitHubService
    {
        public Task<bool> IsServiceAvaibleAsync();

        public Task<IEnumerable<GitRelease?>> GetReleasesAsync(string repoName);

        public Task<IEnumerable<GitRelease?>> GetReleasesAsync(string repoName, int countReleases);

        public Task<string?> DownloadAssetAsync(string url, string destinationPath, CancellationToken ctToken); // IProgress<double> progress
    }

    public interface ISourceForgeService
    {
        public Task<bool> IsServiceAvaibleAsync();

        public Task<IEnumerable<SourceForgeVersionRelease>> GetLatestVersionsAsync(string projectName, int count = 5);

        public Task<SourceForgeVersionRelease?> GetVersionAsync(string projectName, string version);

        public Task<string?> DownloadAssetAsync(string url, string destinationPath, CancellationToken ctToken);
    }

    public interface INavigationService
    {
        public bool CloseIfExistsWindow<T>() where T : Window;
        public bool OpenIfExistsWindow<T>() where T : Window;

        public bool MinimizeIfExistsWindow<T>() where T : Window;

        public void OpenSettings();

        public void CloseMainWindow();

        public void MainWindowMinimize();
    }

    public interface IZapretContext : IDisposable
    {
        public DbSet<ZapretSettings> Settings { get; }

        public Dictionary<string, List<VersionInfo>> Versions { get; set; }

        public Task<int> SaveChangesAsync(CancellationToken ct = default);

        public Task<ZapretSettings> GetOrInitializeSettingsAsync();

        public Task ClearDB();

        public Task MigrateAsync();
    }

    public interface IAutoLoadService
    {
        public void EnableAutostart();

        public bool IsAutostartEnabled();

        public void DisableAutostart();
    }

    public interface IDialogService
    {
        void ShowMessage(string message);   // показ сообщения
        string? SelectFile(string title = "Select required item...", string fileFilter = "All Files|*.*");
    }

}
