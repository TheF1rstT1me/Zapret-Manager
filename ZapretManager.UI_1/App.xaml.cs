using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using ZapretManager.Core_.Interfaces;
using ZapretManager.Infrastructure_;

namespace ZapretManager.UI_1

{
    public partial class App : System.Windows.Application
    {
        public IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            using (var scope = ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ZapretContext>();
                await context.MigrateAsync();

                var settings = await context.GetOrInitializeSettingsAsync();

                if (settings.IsFirstLaunch)
                {
                    var autostart = new AutoLoadService();
                    autostart.EnableAutostart();

                    settings.IsFirstLaunch = false;
                    await context.SaveChangesAsync();
                }
            }

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {

            services.AddInfrastructure();

            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<MainViewModel>();

            services.AddSingleton<IWorkerService, WorkerService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<INavigationService, NavigationService>();

            services.AddTransient<MainWindow>();
            services.AddTransient<SettingsWindow>();
        }
    }

}
