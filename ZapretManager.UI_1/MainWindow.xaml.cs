using System.Windows;
using System.Windows.Input;
using ZapretManager.Core_.Interfaces;

namespace ZapretManager.UI_1
{

    public partial class MainWindow : Window
    {
        // private CancellationTokenSource? _cts;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            System.Windows.Application.Current.Shutdown();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { WindowState = WindowState.Minimized; return; }

            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}