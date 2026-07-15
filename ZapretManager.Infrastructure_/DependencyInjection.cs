using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using ZapretManager.Core_.Interfaces;

namespace ZapretManager.Infrastructure_
{
    public static class InfrastructureModule
    {

        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {

            services.AddScoped<IZapretContext>(provider => provider.GetRequiredService<ZapretContext>());

            services.AddHttpClient<IGitHubService, GitHubService>(client =>
            {
                client.BaseAddress = new Uri("https://api.github.com/");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.7268.91 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3.html+json");
            });

            services.AddHttpClient<ISourceForgeService, SourceForgeService>(client =>
            {
                client.BaseAddress = new Uri("https://sourceforge.net/");
                client.DefaultRequestHeaders.Add("User-Agent", "curl/8.4.0");
            });

            services.AddDbContext<ZapretContext>(options =>
            {
                var appDataPath = Path.Combine(
                    new FileInfo(Environment.ProcessPath!).DirectoryName 
                    ?? new FileInfo(Process.GetCurrentProcess().MainModule!.FileName).DirectoryName!,
                    "ZapretManager_DB"
                );
                Directory.CreateDirectory(appDataPath);

                var dbPath = Path.Combine(appDataPath, "zapret.db");
                options.UseSqlite($"Data Source={dbPath}");
            });


            return services;
        }
    }
}
