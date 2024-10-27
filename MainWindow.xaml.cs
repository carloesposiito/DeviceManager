using GoogleBackupManager.Functions;
using GoogleBackupManager.Model;
using GoogleBackupManager.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GoogleBackupManager
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            MessageDialog testMessageDialog = new MessageDialog("Test message dialog, visible only in debug mode!");
            testMessageDialog.ShowDialog();
#endif

            try
            {
                ADB.InitializeConnection();
                RefreshConnectedDevices(true);
            }
            catch (Exception ex)
            {
                MessageDialog messageDialog = new MessageDialog($"Error while initializing connection:\n{ex.Message}\nAborting program...");
                messageDialog.ShowDialog();
                Process.GetCurrentProcess().Kill();
            }

            /* To do:
             * 1. Save photos from new device. Only save photos that doesn't exist on pc.
             * 
             * 
            */

            
            
            //ADB.CheckDevicesAuthorization();


        }

        private void RefreshConnectedDevices(bool clearOuput = false)
        {
            if (clearOuput)
            {
                richTextBox_Output.Document.Blocks.Clear();
            }
            else
            {
                richTextBox_Output.AppendText("\n");
            }
            
            // Update UI
            richTextBox_Output.AppendText("Connected devices:\n");
            foreach (Device device in ADB.ConnectedDevices)
            {
                richTextBox_Output.AppendText($"{device.Name}, ID: {device.ID}, Status: {(device.IsAuthorized ? "Authorized" : "Not authorized")}, Unlimited backup: {(device.HasUnlimitedBackup ? "Yes" : "No")}\n");
            }
        }

    }
}
