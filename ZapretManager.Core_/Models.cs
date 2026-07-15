using System.Runtime;
using System.Text.Json.Serialization;

namespace ZapretManager.Core_.Models

{

    public enum MODE
    {
        Zapret = 0,
        TgWsProxy = 1
    }

    public enum ACTUALITY
    {
        Relevant = 0,
        Irrelevant = 1
    }

    public record SourceForgeVersionRelease(
        string Version,
        string? ZipUrl,
        string? RarUrl,
        string? ExeUrl,
        DateTime PubDate
    );

    public record GitRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("assets")] List<GitAsset> Assets
    );

    public record GitAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string DownloadUrl
    );

    public record VersionInfo(
        string Version,
        GitRelease? GitHubRelease,
        SourceForgeVersionRelease? SourceForgeRelease
    );

    public static class Constants {

        public const string VERSION_UNACTUAL_COLOR = "#FF6B5B";
        public const string VERSION_ACTUAL_COLOR = "LightGreen";

        public const string RELEVANT_COLOR = "LightGreen";
        public const string IRRELEVANT_COLOR = "#FF8E24";

        public const int PATH_VISIBLE_LENGTH = 13;
        public const string ALWAYS_UPDATE = "Anyway update to actual";

        public static readonly Dictionary<string, string> processesNames = new()
        {
            ["Zapret"] = "winws",
            ["TgWsProxy"] = "tgwsproxy"
        };

        public static readonly Dictionary<string, (string GitHubRepo, string SourceForgeProject)> repos = new()
        {
            ["Zapret"] = ("Flowseal/zapret-discord-youtube", "zapret-discord-youtube.mirror"),
            ["TgWsProxy"] = ("Flowseal/tg-ws-proxy", "tg-ws-proxy.mirror")
        };
    }

    public class PathsToFolder
    {
        public string Zapret { get; set; } = "";
        public string TgWsProxy { get; set; } = "";
    }

    public class Versions
    {
        public string Zapret { get; set; } = "";
        public string ZapretInstalled { get; set; } = "";
        public string TgWsProxyInstalled { get; set; } = "";
        public string TgWsProxy { get; set; } = "";
    }

    public class ZapretSettings
    {
        public int Id { get; set; }

        public PathsToFolder Paths { get; set; } = new();
        public Versions Versions { get; set; } = new();

        public bool IsFirstLaunch { get; set; } = true;
        public bool IsZapretAutoUpdate { get; set; } = false;
        public bool IsTgWsProxyAutoUpdate { get; set; } = false;
        public bool IsZapretAutoOpen { get; set; } = false;
        public bool IsTgWsProxyAutoOpen { get; set; } = false;
        public bool IsSaveDomainList { get; set; } = false;

        public ZapretSettings(int id)
        {
            Id = id;
        }


    }
}
