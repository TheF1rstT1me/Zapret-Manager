using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.IO;
using ZapretManager.Core_.Interfaces;
using ZapretManager.Core_.Messages;
using ZapretManager.Core_.Models;

namespace ZapretManager.UI_1
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IGitHubService _gitHubService;
        private readonly IDialogService _dialogService;
        private readonly IZapretContext _zapretContext;
        private readonly INavigationService _navigationService;
        private readonly IWorkerService _workerService;
        private readonly ISourceForgeService _sourceForgeService;

        private ZapretSettings _settings = null!;

        public SettingsViewModel(IGitHubService gitHubService, IZapretContext zapretContext,
            INavigationService navigationService, IDialogService dialogService, IWorkerService workerService, 
            ISourceForgeService sourceForgeService)
        {
            _dialogService = dialogService;
            _gitHubService = gitHubService;
            _zapretContext = zapretContext;
            _navigationService = navigationService;
            _workerService = workerService;
            _sourceForgeService = sourceForgeService;
        }

        public ObservableCollection<SettingBase> LeftSettings { get; } = new();
        public ObservableCollection<SettingBase> MiddleSettings { get; } = new();
        public ObservableCollection<SettingBase> RightSettings { get; } = new();

        private static FolderSetting FabricateFolder(string Title, string PathFolder, string DialogTitle, string FileFilter)
        {
            return new()
            {
                Title = Title,
                PathFolder = PathFolder,
                FileDialogTitle = DialogTitle, 
                FileFilter = FileFilter
            };
        }

        private static DropdownSetting FabricateDropDown(IEnumerable<string> Options, string SelectedOption, string Title)
        {
            return new()
            {
                Options = Options,
                SelectedOption = SelectedOption,
                Title = Title
            };
        }

        [RelayCommand]
        public void ApplySettings() => _navigationService.CloseIfExistsWindow<SettingsWindow>();

        [RelayCommand]
        public void CloseEnvironment()
        {
            LeftSettings.Clear();
            MiddleSettings.Clear();
            RightSettings.Clear();
        }

        [RelayCommand]
        public async Task LoadEnvironment()
        {
            if ( _settings == null) return;

            FolderSetting zapretFolder = FabricateFolder("Directory (Zapret)", _settings.Paths.Zapret == string.Empty ? "Please, select..." : 
                _settings.Paths.Zapret.Substring(0, Constants.PATH_VISIBLE_LENGTH) + "...", 
                "Select \"new.txt\" file for new directory or .bat for update current", "New Directory File|new.txt|ALT file|*.bat");
            zapretFolder.OnChanged = async (newDirectory) =>
            {
                WeakReferenceMessenger.Default.Send(new ChangedDirectory(
                    "Zapret",
                newDirectory, _workerService.GetFilePath(_settings, "Zapret")));

                FileInfo fileInfo = new FileInfo(newDirectory);

                if (fileInfo.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) newDirectory = fileInfo.Directory!.FullName;

                zapretFolder.PathFolder = newDirectory.Substring(0, Constants.PATH_VISIBLE_LENGTH) + "...";
                bool altsHavent = await _workerService.CheckAltsZapret(fileInfo.Directory!);

                if (!altsHavent)
                {
                    _dialogService.ShowMessage("ZAPRET NOT FOUND IN THE FOLDER!\n\nClick 'update' in the main menu to install zapret in this folder (Do not touch the version field, the value will set itself after the update)");
                };

                _settings.Paths.Zapret = newDirectory; 
                await _zapretContext.SaveChangesAsync();
            };
            
            FolderSetting proxyFolder = FabricateFolder("Directory (TgWsProxy)", _settings.Paths.TgWsProxy == string.Empty ? "Please, select..." : 
                _settings.Paths.TgWsProxy.Substring(0, Constants.PATH_VISIBLE_LENGTH) + "...",
                "Select \"new.txt\" file for new directory or .exe for update current", "New Directory File|new.txt|TgWsProxy file|*.exe");
            proxyFolder.OnChanged = async (newDirectory) =>
            {

                WeakReferenceMessenger.Default.Send(new ChangedDirectory(
                    "TgWsProxy",
                newDirectory, _workerService.GetFilePath(_settings, "TgWsProxy")));

                FileInfo fileInfo = new FileInfo(newDirectory);

                if (fileInfo.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) newDirectory = fileInfo.Directory!.FullName;
                proxyFolder.PathFolder = newDirectory.Substring(0, Constants.PATH_VISIBLE_LENGTH) + "...";

                bool haveTgWsProxy = await _workerService.CheckTgWsProxyExe(fileInfo.Directory!);

                if (!haveTgWsProxy)
                {
                    _dialogService.ShowMessage("TGWSPROXY NOT FOUND IN THE FOLDER!\n\nClick 'update' in the main menu to install TgWsProxy in this folder (Do not touch the version field, the value will set itself after the update)");
                }

                _settings.Paths.TgWsProxy = newDirectory;
                await _zapretContext.SaveChangesAsync();
            };

            DropdownSetting dropdownZapret = FabricateDropDown(_zapretContext.Versions["Zapret"].Select(r => r.Version), 
                _settings.Versions.Zapret == string.Empty ? Constants.ALWAYS_UPDATE : _settings.Versions.Zapret, "Zapret");
            dropdownZapret.OnChanged = async (newVersion) =>
            {
                _settings.Versions.Zapret = newVersion;
                await _zapretContext.SaveChangesAsync();

                WeakReferenceMessenger.Default.Send(new SelectedVersionChangedMessage(dropdownZapret.Title, newVersion));
            };

            DropdownSetting dropdownTgWsProxy = FabricateDropDown(_zapretContext.Versions["TgWsProxy"].Select(r => r.Version), 
                _settings.Versions.TgWsProxy == string.Empty ? Constants.ALWAYS_UPDATE : _settings.Versions.TgWsProxy, "TgWsProxy");
            dropdownTgWsProxy.OnChanged = async (newVersion) =>
            {
                _settings.Versions.TgWsProxy = newVersion;
                await _zapretContext.SaveChangesAsync();

                WeakReferenceMessenger.Default.Send(new SelectedVersionChangedMessage(dropdownTgWsProxy.Title, newVersion));
            };

            LeftSettings.Add(zapretFolder);
            LeftSettings.Add(proxyFolder);

            LeftSettings.Add(new ToggleSetting { Title = "Autoupdate Zapret:", IsOn = _settings.IsZapretAutoUpdate, 
                OnChanged = async (newToggle) =>
                {
                    _settings.IsZapretAutoUpdate = newToggle;
                    await _zapretContext.SaveChangesAsync();
                }
            });
            LeftSettings.Add(new ToggleSetting { Title = "Autoupdate TgWsProxy:", IsOn = _settings.IsTgWsProxyAutoUpdate,
                OnChanged = async (newToggle) =>
                {
                    _settings.IsTgWsProxyAutoUpdate = newToggle;
                    await _zapretContext.SaveChangesAsync();
                }
            });

            MiddleSettings.Add(dropdownZapret);
            MiddleSettings.Add(dropdownTgWsProxy);

            MiddleSettings.Add(new ToggleSetting { Title = "Auto-opening Zapret:", IsOn = _settings.IsZapretAutoOpen,
                OnChanged = async (newToggle) =>
                {
                    _settings.IsZapretAutoOpen = newToggle;
                    await _zapretContext.SaveChangesAsync();
                }
            });
            MiddleSettings.Add(new ToggleSetting { Title = "Auto-opening TgWsProxy:", IsOn = _settings.IsTgWsProxyAutoOpen,
                OnChanged = async (newToggle) =>
                {
                    _settings.IsTgWsProxyAutoOpen = newToggle;
                    await _zapretContext.SaveChangesAsync();
                }
            });

            RightSettings.Add(new ToggleSetting { Title = "Save users domain-lists:", IsOn = _settings.IsSaveDomainList,
                OnChanged = async (newToggle) =>
                {
                    _settings.IsSaveDomainList = newToggle;
                    await _zapretContext.SaveChangesAsync();
                }
            });

           // _windowInitialized = true;
        }

        [RelayCommand]
        public async Task RunDataBase()
        {
            if (_settings != null) return;

            // await _zapretContext.ClearDB();
            _settings = await _zapretContext.GetOrInitializeSettingsAsync();
        }

        [RelayCommand]
        public void FolderButton(FolderSetting folderSetting)
        {
            if (_settings == null) return;
            string? result = _dialogService.SelectFile(folderSetting.FileDialogTitle, folderSetting.FileFilter);

            if (File.Exists(result))
            {
                folderSetting.OnChanged?.Invoke(result);
                return;
            };

            _dialogService.ShowMessage("FILE NOT EXISTS!");
        }

        [RelayCommand]
        public void ChangeVersion(DropdownSetting dropdownSetting)
        {
            if (_settings == null) return;
        }

    }
}
