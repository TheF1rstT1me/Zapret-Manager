

namespace ZapretManager.Core_.Messages

{
    public class SelectedVersionChangedMessage(string mode, string newVersion)
    {
        public string Mode { get; } = mode;
        public string NewVersion { get; } = newVersion;
    }

    public class ChangedDirectory(string mode, string newDirectory)
    {
        public string Mode { get; } = mode;
        public string NewDirectory { get; } = newDirectory;
    }
}
