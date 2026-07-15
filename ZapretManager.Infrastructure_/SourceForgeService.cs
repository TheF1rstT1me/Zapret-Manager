using System.Text.RegularExpressions;
using System.Xml.Linq;
using ZapretManager.Core_.Exceptions;
using ZapretManager.Core_.Interfaces;
using ZapretManager.Core_.Models;

namespace ZapretManager.Infrastructure_
{
    public class SourceForgeService(HttpClient httpClient) : ISourceForgeService
    {
        private HttpClient _httpClient = httpClient;

        public async Task<bool> IsServiceAvaibleAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_httpClient.BaseAddress);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<SourceForgeVersionRelease>> GetLatestVersionsAsync(string projectName, int count = 5)
        {
            string url = $"projects/{projectName}/rss?path=/";
            string xmlContent = string.Empty;

            try
            {
                xmlContent = await _httpClient.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                throw new SourceForgeException("400", ex);
            }

            XDocument doc = XDocument.Parse(xmlContent);

            var rawItems = doc.Descendants("item")
                .Select(item =>
                {
                    string title = item.Element("title")?.Value ?? string.Empty;
                    string link = item.Element("link")?.Value ?? string.Empty;
                    DateTime pubDate = DateTime.TryParse(item.Element("pubDate")?.Value, out var d)
                        ? d
                        : DateTime.MinValue;

                    return (Title: title, Link: link, PubDate: pubDate);
                })
                .ToList();

            var versionRegex = new Regex(@"^/([^/]+)/(.+)$");

            var grouped = rawItems
                .Select(item =>
                {
                    var match = versionRegex.Match(item.Title);
                    if (!match.Success) return null;

                    string version = match.Groups[1].Value;
                    string fileName = match.Groups[2].Value;

                    return new
                    {
                        Version = version,
                        FileName = fileName,
                        item.Link,
                        item.PubDate
                    };
                })
                .Where(x => x != null)
                .Where(x =>
                    !x!.FileName.Equals("README.md", StringComparison.OrdinalIgnoreCase) &&
                    !x.FileName.Contains("source code", StringComparison.OrdinalIgnoreCase) &&
                    !x.FileName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) &&
                    (x.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || x.FileName.EndsWith(".rar", StringComparison.OrdinalIgnoreCase) || x.FileName.EndsWith("_windows.exe", StringComparison.OrdinalIgnoreCase))
                )
                .GroupBy(x => x!.Version)
                .Select(g => new SourceForgeVersionRelease
                (
                    Version: g.Key,
                    ZipUrl: g.FirstOrDefault(x => x!.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))?.Link,
                    RarUrl: g.FirstOrDefault(x => x!.FileName.EndsWith(".rar", StringComparison.OrdinalIgnoreCase))?.Link,
                    ExeUrl: g.FirstOrDefault(x => x!.FileName.EndsWith("_windows.exe", StringComparison.OrdinalIgnoreCase))?.Link,
                    PubDate: g.Max(x => x!.PubDate)
                ))
                .OrderByDescending(v => v.PubDate)
                .Take(count)
                .ToList();

            return grouped;
        }

        public async Task<SourceForgeVersionRelease?> GetVersionAsync(string projectName, string version)
        {
            var versions = await GetLatestVersionsAsync(projectName, 100);
            return versions.FirstOrDefault(v => v.Version == version);
        }

        public async Task<string?> DownloadAssetAsync(string url, string destinationPath, CancellationToken ctToken)
        {
            try
            {

                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            }
            catch (Exception ex)
            {
                throw new SourceForgeException("DIRECTORY EXCEPTION", ex);
            }

            try
            { 
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ctToken);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                using var contentStream = await response.Content.ReadAsStreamAsync(ctToken);
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int read;

                while ((read = await contentStream.ReadAsync(buffer, cancellationToken: ctToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken: ctToken);
                    totalRead += read;
                }

                return fileStream.Name;
            }
            catch (OperationCanceledException ex)
            {
                return null;
            }
            catch (Exception ex)
            {
                throw new SourceForgeException($"{ex.Message}", ex);
            }
        }
    }
}
