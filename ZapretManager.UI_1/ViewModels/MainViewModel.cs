using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.IO;
using ZapretManager.Core_.Exceptions;
using ZapretManager.Core_.Interfaces;
using ZapretManager.Core_.Messages;
using ZapretManager.Core_.Models;

using MessageBox = System.Windows.MessageBox;
using Constants = ZapretManager.Core_.Models.Constants;

namespace ZapretManager.UI_1
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IWorkerService _workerService;
        private readonly IGitHubService _gitHubService;
        private readonly IZapretContext _zapretContext;
        private readonly INavigationService _navigationService;
        private readonly ISourceForgeService _sourceForgeService;

        private ZapretSettings _settings = null!;

        public MainViewModel(IGitHubService gitHubService, IZapretContext zapretContext, 
            INavigationService navigationService, IWorkerService workerService, ISourceForgeService sourceForgeService)
        {
            _workerService = workerService;
            _gitHubService = gitHubService;
            _zapretContext = zapretContext;
            _navigationService = navigationService;
            _sourceForgeService = sourceForgeService;

            WeakReferenceMessenger.Default.Register<SelectedVersionChangedMessage>(this, (recipient, message) 
                => ProcessChangeVersion(message.Mode, message.NewVersion)
            );

            WeakReferenceMessenger.Default.Register<ChangedDirectory>(this, async (recipient, message)
                => await ProcessChangeDirectory(message.Mode, message.NewDirectory, message.OldDirectory)
            );
        }

        [ObservableProperty]
        private string _appVersion = "Checking services is avaible...";
        [ObservableProperty]
        private string _hintStatus = "";
        [ObservableProperty]
        private string _relevantColor = Constants.IRRELEVANT_COLOR;
        [ObservableProperty]
        private string _versionColor = Constants.VERSION_UNACTUAL_COLOR;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ModeName))]
        private MODE _mode = MODE.Zapret;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ActualityName))]
        private ACTUALITY _actuality = ACTUALITY.Irrelevant;

        public string ModeName => Mode.ToString();
        public string ActualityName => Actuality.ToString();

        private void SwitchActualityMode(bool actual)
        {
            if (actual)
            {
                Actuality = ACTUALITY.Relevant;
                RelevantColor = Constants.RELEVANT_COLOR;
                VersionColor = Constants.VERSION_ACTUAL_COLOR;
                return;
            }

            Actuality = ACTUALITY.Irrelevant;
            RelevantColor = Constants.IRRELEVANT_COLOR;
            VersionColor = Constants.VERSION_UNACTUAL_COLOR;
        }

        private void UpdateUI()
        {
            AppVersion = _workerService.GetVersionInstalled(_settings, ModeName) == string.Empty ? Constants.ALWAYS_UPDATE : _workerService.GetVersionInstalled(_settings, ModeName);

            string settingsVersion = _workerService.GetVersionInSettings(_settings, ModeName);
            string myVersion = _workerService.GetVersionInstalled(_settings, ModeName);
            string actualVersion = _zapretContext.Versions[ModeName].FirstOrDefault()?.Version!;

            if (settingsVersion == myVersion && actualVersion != myVersion)
            {
                HintStatus = $" (Change {ModeName} version to actual and click update)";
                SwitchActualityMode(false);
            } else if (settingsVersion == myVersion && actualVersion  == myVersion)
            {
                HintStatus = "";
                SwitchActualityMode(true);
            } else if (settingsVersion != myVersion && actualVersion == settingsVersion)
            {
                HintStatus = $" (Click update to change version to actual {settingsVersion})";
                SwitchActualityMode(false);
            } else if (settingsVersion != myVersion && actualVersion == myVersion)
            {
                HintStatus = $" (Click update to change version to unactual {settingsVersion})";
                SwitchActualityMode(true);
            }
        }

        private async Task<bool> CheckServicesAvaibleAsync()
        { 
            var results = await Task.WhenAll(
                _gitHubService.IsServiceAvaibleAsync(),
                _sourceForgeService.IsServiceAvaibleAsync()
            );

            return results.Any(r => r);
        }

        private async Task AutoUpdateAsync()
        {
            string myVersionZapret = _workerService.GetVersionInstalled(_settings, "Zapret");
            string myVersionTgWsProxy = _workerService.GetVersionInSettings(_settings, "TgWsProxy");

            string actualVersionZapret = _zapretContext.Versions["Zapret"].FirstOrDefault()?.Version!;
            string actualVersionTgWsProxy = _zapretContext.Versions["TgWsProxy"].FirstOrDefault()?.Version!;

            bool IsZapretAutoUpdates = _workerService.GetAutoUpdate(_settings, "Zapret");
            bool IsTgWsProxyAutoUpdates = _workerService.GetAutoUpdate(_settings, "TgWsProxy");

            if (IsZapretAutoUpdates && myVersionZapret != actualVersionZapret)
            {
                _settings.Versions.Zapret = actualVersionZapret;
                await _zapretContext.SaveChangesAsync();

                if (ModeName != "Zapret") SwitchMode();
                await Update();
            }

            if (IsTgWsProxyAutoUpdates && myVersionTgWsProxy != actualVersionTgWsProxy)
            {
                _settings.Versions.TgWsProxy = actualVersionTgWsProxy;
                await _zapretContext.SaveChangesAsync();

                if (ModeName != "TgWsProxy") SwitchMode();
                await Update();
            }
        }

        private async Task UpdateVersions()
        {
            try
            {
                Task<IEnumerable<GitRelease?>> TaskReleasesZapret = _gitHubService.GetReleasesAsync("Flowseal/zapret-discord-youtube", 6);
                Task<IEnumerable<GitRelease?>> TaskReleasesTgWsProxy = _gitHubService.GetReleasesAsync("Flowseal/tg-ws-proxy", 6);

                await Task.WhenAll(TaskReleasesTgWsProxy, TaskReleasesZapret);

                IEnumerable<GitRelease?> releasesTgWsProxy = await TaskReleasesTgWsProxy;
                IEnumerable<GitRelease?> releasesZapret = await TaskReleasesZapret;

                foreach (GitRelease? release in releasesTgWsProxy)
                {
                    _zapretContext.Versions["TgWsProxy"].Add(new VersionInfo( 
                        Version: release!.TagName,
                        GitHubRelease: release,
                        SourceForgeRelease: null
                    ));
                }

                foreach (GitRelease? release in releasesZapret)
                {
                    _zapretContext.Versions["Zapret"].Add(new VersionInfo(
                        Version: release!.TagName,
                        GitHubRelease: release,
                        SourceForgeRelease: null
                    ));
                }
            }
            catch (GitHubServiceException)
            {
                Task<IEnumerable<SourceForgeVersionRelease>> TaskReleasesZapret = _sourceForgeService.GetLatestVersionsAsync("zapret-discord-youtube.mirror", 5);
                Task<IEnumerable<SourceForgeVersionRelease>> TaskReleasesTgWsProxy = _sourceForgeService.GetLatestVersionsAsync("tg-ws-proxy.mirror", 5);

                await Task.WhenAll(TaskReleasesTgWsProxy, TaskReleasesZapret);

                IEnumerable<SourceForgeVersionRelease> releasesTgWsProxy = await TaskReleasesTgWsProxy;
                IEnumerable<SourceForgeVersionRelease> releasesZapret = await TaskReleasesZapret;

                foreach (SourceForgeVersionRelease release in releasesTgWsProxy)
                {
                    _zapretContext.Versions["TgWsProxy"].Add(new VersionInfo(
                        Version: release.Version,
                        GitHubRelease: null,
                        SourceForgeRelease: release
                    ));
                }

                foreach (SourceForgeVersionRelease release in releasesZapret)
                {
                    _zapretContext.Versions["Zapret"].Add(new VersionInfo(
                        Version: release.Version,
                        GitHubRelease: null,
                        SourceForgeRelease: release
                    ));
                }
            }
        }

        private void ProcessChangeVersion(string modeChanged, string version)
        {
            if (modeChanged != ModeName) { SwitchMode(); return; }
            UpdateUI();
        }

        private async Task ProcessChangeDirectory(string modeChanged, string directory, string oldDirectory)
        {

            string curVersionPath = "";
            DirectoryInfo curDir = Directory.Exists(directory) ? new DirectoryInfo(directory) : new FileInfo(directory).Directory!;

            if (oldDirectory != string.Empty)
            {
                DirectoryInfo oldDir = Directory.Exists(oldDirectory) ? new DirectoryInfo(oldDirectory) : new FileInfo(oldDirectory).Directory!;
                if (curDir.Name == oldDir.Name) return;
            }

            curVersionPath = Path.Combine(curDir.FullName, "curversion.txt");

            switch (modeChanged)
            {
                case "Zapret":
                    if (File.Exists(curVersionPath))
                    {
                        string curTag = await File.ReadAllTextAsync(curVersionPath);

                        _settings.Versions.ZapretInstalled = curTag;
                        _settings.Versions.Zapret = curTag;

                        break;
                    }

                    _settings.Versions.ZapretInstalled = "";
                    break;
                case "TgWsProxy":
                    if (File.Exists(curVersionPath))
                    {
                        string curTag = await File.ReadAllTextAsync(curVersionPath);

                        _settings.Versions.TgWsProxyInstalled = curTag;
                        _settings.Versions.TgWsProxy = curTag;

                        break;
                    }

                    _settings.Versions.TgWsProxyInstalled = "";
                    break;
                default:
                    break;
            }
            
            await _zapretContext.SaveChangesAsync();

            UpdateUI();
        }

        [RelayCommand]
        public void CloseWindow()
        {
            _navigationService.CloseMainWindow();
        }

        [RelayCommand]
        public void MinimizeWindow()
        {
            _navigationService.MainWindowMinimize();
        }

        [RelayCommand]
        public void SwitchMode() {
            Mode = Mode == MODE.Zapret? MODE.TgWsProxy : MODE.Zapret;
            UpdateUI();
        }

        [RelayCommand]
        public void SettingsOpen() => _navigationService.OpenSettings();

        [RelayCommand]
        public async Task RunApplication()
        {

            bool IsServicesAvaible = await CheckServicesAvaibleAsync();
            if (!IsServicesAvaible)
            {
                MessageBox.Show("ERROR: Services not avaible. Check the ethernet!");
                _navigationService.CloseMainWindow();
            }

            if (_settings != null) return;

            // await _zapretContext.ClearDB();
            _settings = await _zapretContext.GetOrInitializeSettingsAsync();
            
            await UpdateVersions();
            UpdateUI();

            await AutoUpdateAsync();

            _ = _workerService.AutoOpen(_settings, "Zapret");
            _ = _workerService.AutoOpen(_settings, "TgWsProxy");
        }

        [RelayCommand]
        public async Task Update()
        {
            if (_settings == null) return;

            string myVersion = _workerService.GetVersionInstalled(_settings, ModeName);
            string tagVersion = _workerService.GetVersionInSettings(_settings, ModeName);
            string actualVersion = _zapretContext.Versions[ModeName].FirstOrDefault()?.Version!;

            if (myVersion == actualVersion && tagVersion == actualVersion) { MessageBox.Show($"ERROR: Version {ModeName} is actual."); return; }

            MessageBox.Show($"Update {ModeName} started.");

            if (tagVersion == string.Empty)
            {
                tagVersion = actualVersion;
            } else
            {
                tagVersion = _zapretContext.Versions[ModeName].FirstOrDefault(r => r.Version == tagVersion)?.Version!;
            }

            if (tagVersion == null) { MessageBox.Show("ERROR: Application versions could not be detected."); return; }

            string mode = ModeName;
            bool? response = await _workerService.Update(_settings, mode, tagVersion, async (string newDirectory) =>
            {
                switch (mode)
                {
                    case "Zapret":

                        string directoryPath = _workerService.GetFilePath(_settings, mode);
                        string? oldAltFile = null;

                        if (directoryPath != string.Empty && System.IO.Path.GetFileName(directoryPath) != "new.txt")
                        {
                            oldAltFile = System.IO.Path.GetFileName(directoryPath);
                        }

                        string newPath = oldAltFile == null ? System.IO.Path.Combine(newDirectory, "general.bat") : System.IO.Path.Combine(newDirectory, oldAltFile);

                        _settings.Versions.Zapret = tagVersion;
                        _settings.Versions.ZapretInstalled = tagVersion;
                        _settings.Paths.Zapret = newPath;

                        await _zapretContext.SaveChangesAsync();

                        break;
                    case "TgWsProxy":
                        _settings.Versions.TgWsProxy = tagVersion;
                        _settings.Versions.TgWsProxyInstalled = tagVersion;
                        _settings.Paths.TgWsProxy = newDirectory;

                        await _zapretContext.SaveChangesAsync();

                        break;
                    default:
                        break;
                }

                MessageBox.Show($"Update {mode} to {tagVersion} successfully ended.");
                UpdateUI();
            });

            if (response == null) { MessageBox.Show($"ERROR: Update {ModeName} in processing."); return; }
        }

    }
}
