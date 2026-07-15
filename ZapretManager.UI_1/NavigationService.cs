using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Windows;
using ZapretManager.Core_.Exceptions;
using ZapretManager.Core_.Interfaces;
using ZapretManager.Core_.Models;

namespace ZapretManager.UI_1
{
    public class NavigationService(IServiceProvider serviceProvider) : INavigationService
    {
        private IServiceProvider _serviceProvider = serviceProvider;

        public bool OpenIfExistsWindow<T>() where T : Window
        {
            var window = System.Windows.Application.Current.Windows.OfType<T>().FirstOrDefault();
            if (window != null)
            {
                window.Show();
                if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
                return true;
            }

            return false;
        }

        public bool CloseIfExistsWindow<T>() where T : Window
        {
            var window = System.Windows.Application.Current.Windows.OfType<T>().FirstOrDefault();

            if (window != null)
            {
                window.Close();
                return true;
            }

            return false;
        }

        public bool MinimizeIfExistsWindow<T>() where T : Window
        {
            var window = System.Windows.Application.Current.Windows.OfType<T>().FirstOrDefault();

            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
                return true;
            }

            return false;
        }

        public void OpenSettings()
        {
            var settWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
            if (OpenIfExistsWindow<SettingsWindow>()) return;
            settWindow.Show();
        }

        public void CloseMainWindow()
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            if (CloseIfExistsWindow<MainWindow>()) return;
            mainWindow.Close();
        }

        public void MainWindowMinimize()
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            if (MinimizeIfExistsWindow<MainWindow>()) return;
            mainWindow.WindowState = WindowState.Minimized;
        }

    }
}
