using CommunityToolkit.Mvvm.ComponentModel;

namespace ZapretManager.UI_1

{
    public partial class SettingBase: ObservableObject
    {
        public string Title { get; set; }
    }

    public class ToggleSetting : SettingBase
    {
        public bool _IsOn { get; set; }

        public bool IsOn {
            get => _IsOn;
            set
            {
                _IsOn = value;
                OnChanged?.Invoke(value);
            }
        }

        public Action<bool> OnChanged { get; set; }
    }

    public partial class FolderSetting : SettingBase
    {
        [ObservableProperty]
        private string _pathFolder;

        public string Title { get; set; }

        public string FileFilter { get; set; }

        public string FileDialogTitle { get; set; }

        public Action<string> OnChanged { get; set; }
    }

    public partial class DropdownSetting : SettingBase
    {
        public IEnumerable<string> Options { get; set; }

        [ObservableProperty]
        private string _selectedOption;

        partial void OnSelectedOptionChanged(string value)
        {
            OnChanged?.Invoke(value);
        }

        public Action<string>? OnChanged { get; set; }
    }
}
