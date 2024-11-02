using System.Windows;

namespace GoogleBackupManager.UI
{
    /// <summary>
    /// Logica di interazione per WaitingDialog.xaml
    /// </summary>
    public partial class WaitingDialog : Window
    {
        public WaitingDialog(string message)
        {
            InitializeComponent();
            label_Message.Content = message;
        }

        public void RefreshPercentage(string percentage)
        {
            label_Percentage.Content = percentage;
        }
    }
}
