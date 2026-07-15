using System.Net.Http.Json;
using ZapretManager.Core_.Exceptions;
using ZapretManager.Core_.Interfaces;
using ZapretManager.Core_.Models;

namespace ZapretManager.Infrastructure_
{
    public class GitHubService(HttpClient httpClient) : IGitHubService
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

        public async Task<IEnumerable<GitRelease?>> GetReleasesAsync(string repoName)
        {
            try
            {
                List<GitRelease>? content = await _httpClient.GetFromJsonAsync<List<GitRelease>>($"repos/{repoName}/releases");
                if (content == null) throw new GitHubServiceException("400");
                
                return content ?? Enumerable.Empty<GitRelease>();

            } catch (HttpRequestException ex)
            {
                throw new GitHubServiceException("400", ex);
            }
        }

        public async Task<IEnumerable<GitRelease?>> GetReleasesAsync(string repoName, int countReleases)
        {
            try
            {
                List<GitRelease>? content = await _httpClient.GetFromJsonAsync<List<GitRelease>>($"repos/{repoName}/releases");
 
                if (content == null) throw new GitHubServiceException("400"); 
                if (content.Count == 0) return Enumerable.Empty<GitRelease>();

                int maxCount = countReleases;
                
                if (content.Count < countReleases) maxCount = content.Count;
                        
                return content.GetRange(0, maxCount);
            }
            catch (HttpRequestException ex)
            {
                throw new GitHubServiceException("400", ex);
            }
        }

        public async Task<string?> DownloadAssetAsync(string url, string destinationPath, CancellationToken ctToken) // IProgress<double> progress
        {
            try
            {

                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            } catch (Exception ex)
            {
                throw new GitHubServiceException("DIRECTORY EXCEPTION", ex);
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

                  //  if (totalBytes > 0)
                  //  {
                  //      progress?.Report((double)totalRead / totalBytes * 100);
                  //  }
                }

                return fileStream.Name;
            }
            catch (OperationCanceledException ex)
            {
                return null;
            }
            catch (Exception ex)
            {
                throw new GitHubServiceException($"{ex.Message}", ex);
            }
        }


    }
}
