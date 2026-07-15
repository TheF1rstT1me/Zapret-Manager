using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using ZapretManager.Core_.Interfaces;
using ZapretManager.Core_.Models;
using ZapretManager.Infrastructure_;

namespace ZapretManager.UI_1
{

    public partial class SettingsWindow : Window
    {

        private readonly IGitHubService _gitHubService;
        private readonly IZapretContext _zapretContext;

        public SettingsWindow(SettingsViewModel viewModel, IGitHubService gitHubService, IZapretContext zapretContext)
        {

            InitializeComponent();

            _gitHubService = gitHubService;
            _zapretContext = zapretContext;

            DataContext = viewModel;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

    }
}