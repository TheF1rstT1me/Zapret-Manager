using System.Windows.Forms;
using ZapretManager.Core_.Interfaces;

namespace ZapretManager.Infrastructure_
{
    public class DialogService() : IDialogService
    {

        public string? SelectFile(string title = "Select required item...", string fileFilter = "All Files|*.*")
        {

            using var dialog = new OpenFileDialog()
            {
                Title = title,
                Filter = fileFilter
            };

            string? result = null;

            try
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    result = dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message);
                result = null;
            }

            return result;
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
