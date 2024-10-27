using GoogleBackupManager.Model.Exceptions;
using GoogleBackupManager.Functions;
using GoogleBackupManager.Model;
using GoogleBackupManager.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
using static System.Net.Mime.MediaTypeNames;

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

            try
            {
                ADB.InitializeConnection();




                RefreshConnectedDevices(true);
            }
            catch (NoDevicesException ex)
            {
                MessageDialog messageDialog = new MessageDialog($"{ex.Message}\nTry to connect smartphone via WiFi");
                messageDialog.ShowDialog();
            }
            catch (DirectoryNotFoundException ex)
            {
                MessageDialog messageDialog = new MessageDialog($"Error while initializing connection:\n{ex.Message}\nAborting program...");
                messageDialog.ShowDialog();
                Process.GetCurrentProcess().Kill();
            }
            finally
            {
                RefreshConnectedDevices();
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




        private void button_PairWiFiDevice_Click(object sender, RoutedEventArgs e)
        {
            //string deviceIp = textBox_DeviceIp.Text;
            //string devicePort = textbox_DevicePort.Text;
            //string devicePairingCode = textbox_DevicePairingCode.Text;

            //// Check valid ip and port
            //if (IPAddress.TryParse(deviceIp, out IPAddress parsedIpAddress) && int.TryParse(devicePort, out int parsedDevicePort))
            //{
            //    if (devicePairingCode.Equals("Undefined"))
            //    {
            //        if (!ADB.ConnectWirelessDevice(deviceIp, devicePort))
            //        {
            //            MessageDialog messageDialog = new MessageDialog("Connection failed!\nTry to pair device via pairing code.");
            //            messageDialog.ShowDialog();
            //        }
            //    }
            //    else
            //    {
            //        ADB.ConnectWirelessDevice(deviceIp, devicePort, devicePairingCode);
            //    }

            //    RefreshConnectedDevices();
            //}
            //else
            //{
            //    MessageDialog messageDialog = new MessageDialog("Check inserted ip and port addresses!");
            //    messageDialog.ShowDialog();
            //}            
        }

        private void button_RefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            ADB.RefreshDevices();

            RefreshConnectedDevices(true);
        }
    }
}
