using GoogleBackupManager.Model.Exceptions;
using GoogleBackupManager.Functions;
using GoogleBackupManager.Model;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Net;
using System.IO;
using GoogleBackupManager.UI;

namespace GoogleBackupManager
{
    public partial class MainWindow : Window
    {
        #region "Variables"

        /// <summary>
        /// Describes if all devices are authorized.
        /// </summary>
        private bool initializeResult = false;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

#if !DEBUG
            tabItem_Development.Visibility = Visibility.Hidden;
#endif
            ADB.InitializeConnection();
            RefreshForm();
            textBlock_FilteredOutput.Text = string.Empty;
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                               FUNCTIONS                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        private void ScanDevices()
        {
            WaitingDialog waitingDialog = new WaitingDialog();

            try
            {
                waitingDialog.Show();
                initializeResult = ADB.ScanDevices();

                if (initializeResult)
                {
                    RefreshForm(false);
                }
                else
                {
                    WriteToOutput("Please authorize your device to permit backup!", true, true, true);
                    RefreshForm(false);
                }
                
                waitingDialog.Close();
            }
            catch (PlatformToolsException ex)
            {
                // What to do when platform tools folder is not found
                waitingDialog.Close();
                Utils.ShowMessageDialog($"{ex.Message}\nAborting...");
                Process.GetCurrentProcess().Kill();
            }
            catch (PlatformToolsTimeoutException ex)
            {
                // What to do when platform tools folder is not found
                waitingDialog.Close();
                Utils.ShowMessageDialog($"{ex.Message}\nPlease try again.");
            }
            catch (CommandPromptException ex)
            {
                // What to do when command prompt is not active anymore
                waitingDialog.Close();
                Utils.ShowMessageDialog($"{ex.Message}\nAborting...");
                Process.GetCurrentProcess().Kill();
            }
            catch (NoDevicesException ex)
            {
                // What to do when no devices are found
                waitingDialog.Close();
                initializeResult = false;
                RefreshForm(false);
                WriteToOutput(ex.Message, true, false, false);
            }
            catch (DevicesCountException ex)
            {
                // What to do when connected devices are less than two
                waitingDialog.Close();
                WriteToOutput($"{ex.Message}\nPlease connect another device to proceed.", true, true, true);
                RefreshForm(false);
            }
            catch (Exception ex)
            {
                // What to do when there's a general error.
                waitingDialog.Close();
                RefreshForm(false);
                WriteToOutput($"{ex.Message}\nPlease try again.", true, false, false);
            }
        }

        /// <summary>
        /// Writes message to filtered output TextBlock.
        /// </summary>
        /// <param name="messageToWrite">Message to be written.</param>
        /// <param name="clearPreviousOutput">[OPTIONAL] If true clear previous output.</param>
        /// <param name="newLineAfterMessage">[OPTIONAL] If true put a new line after message.</param>
        /// <param name="insertSeparator">[OPTIONAL] If true put a separator after message.</param>
        private void WriteToOutput(string messageToWrite, bool clearPreviousOutput = false, bool newLineAfterMessage = false, bool insertSeparator = false)
        {
            if (clearPreviousOutput)
            {
                textBlock_FilteredOutput.Text = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(messageToWrite))
            {
                textBlock_FilteredOutput.Text += messageToWrite;
            }

            if (newLineAfterMessage)
            {
                textBlock_FilteredOutput.Text += "\n\n";
            }

            if (insertSeparator)
            {
                textBlock_FilteredOutput.Text += "-----------------------------------------------------------------------\n\n";
            }
        }

        /// <summary>
        /// Refresh form and devices.
        /// </summary>
        /// /// <param name="clearPreviousOutput">[OPTIONAL] If true clear previous output.</param>
        private void RefreshForm(bool clearPreviousOutput = false)
        {
            WriteToOutput("Connected devices:", clearPreviousOutput, true, false);

            foreach (Device device in ADB.ConnectedDevices)
            {
                if (device == ADB.ConnectedDevices.Last())
                {
                    WriteToOutput(device.ToString());
                }
                else
                {
                    WriteToOutput(device.ToString(), false, true);
                }
            }

            WriteToOutput(string.Empty, false, true, true);

            #region "Update form"

            // Clear old combobox data
            comboBox_BackupDevice.Items.Clear();
            comboBox_ExtractDevice.Items.Clear();
            comboBox_AuthDevices.Items.Clear();

            switch (ADB.ConnectedDevices.Count)
            {
                case 0:
                    grid_Backup.IsEnabled = false;
                    comboBox_AuthDevices.IsEnabled = button_AuthorizeDevice.IsEnabled = false;
                    tabControl_Program.SelectedIndex = 1;
                    break;

                default:
                    grid_Backup.IsEnabled = true;
                    button_StartBackup.IsEnabled = initializeResult;

                    foreach (Device device in ADB.ConnectedDevices)
                    {
                        if (device.HasUnlimitedBackup && device.IsAuthorized)
                        {
                            if (device.Name.Equals(device.ID))
                            {
                                // Device without name so add it also in extract devices
                                comboBox_ExtractDevice.Items.Add(device);
                            }

                            comboBox_BackupDevice.Items.Add(device);
                            comboBox_BackupDevice.SelectedIndex = 0;
                        }

                        if (!device.HasUnlimitedBackup && device.IsAuthorized)
                        {
                            comboBox_ExtractDevice.Items.Add(device);
                            comboBox_ExtractDevice.SelectedIndex = 0;
                        }

                        if (!device.IsAuthorized)
                        {
                            comboBox_AuthDevices.Items.Add(device);
                            comboBox_AuthDevices.SelectedIndex = 0;
                        }                        
                    }

                    comboBox_AuthDevices.IsEnabled = button_AuthorizeDevice.IsEnabled = comboBox_AuthDevices.Items.Count > 0;
                    comboBox_BackupDevice.IsEnabled = comboBox_BackupDevice.Items.Count > 0;
                    comboBox_ExtractDevice.IsEnabled = comboBox_ExtractDevice.Items.Count > 0;

                    break;
            }

            #endregion
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                                EVENTS                              //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        private void border_Upper_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void button_Close_Click(object sender, RoutedEventArgs e)
        {
            ADB.CloseConnection();
            Close();
        }

        /// <summary>
        /// Handles connecting or pairing operations.
        /// </summary>
        private void checkBox_Pairing_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)checkBox_Pairing.IsChecked)
            {
                // Show pairing row
                grid_Pairing.Visibility = Visibility.Visible;
                button_ConnectPairDevice.Content = "Pair";
            }
            else
            {
                // Hide pairing row
                grid_Pairing.Visibility = Visibility.Collapsed;
                button_ConnectPairDevice.Content = "Connect";
            }
        }

        private void button_ConnectPairDevice_Click(object sender, RoutedEventArgs e)
        {
            // Check valid data
            if (IPAddress.TryParse(textBox_DeviceIp.Text, out _) && int.TryParse(textbox_DevicePort.Text, out _))
            {
                bool result = false;

                if ((bool)checkBox_Pairing.IsChecked)
                {
                    result = ADB.PairWirelessDevice(textBox_DeviceIp.Text, textbox_DevicePort.Text, textbox_DevicePairingCode.Text);
                    if (result)
                    {
                        // Erase data in all TextBoxes
                        textbox_DevicePort.Text = textbox_DevicePairingCode.Text = string.Empty;
                        checkBox_Pairing.IsChecked = false;
                        Utils.ShowMessageDialog("Pairing succed! It's now possible to connect to device.");
                    }
                    else
                    {
                        textbox_DevicePort.Text = textbox_DevicePairingCode.Text = string.Empty;
                        Utils.ShowMessageDialog("Pairing failed! Check inserted data and try again.");
                    }
                }
                else
                {
                    result = ADB.ConnectWirelessDevice(textBox_DeviceIp.Text, textbox_DevicePort.Text);
                    if (result)
                    {
                        // Erase data in all TextBoxes
                        textBox_DeviceIp.Text = textbox_DevicePort.Text = textbox_DevicePairingCode.Text = string.Empty;
                        ScanDevices();
                    }
                    else
                    {
                        // Erase only port and device pairing code
                        textbox_DevicePort.Text = textbox_DevicePairingCode.Text = string.Empty;
                        Utils.ShowMessageDialog("Connection failed! Try to pair device first.");
                    }
                }
            }
            else
            {
                // Erase data in all TextBoxes
                textBox_DeviceIp.Text = textbox_DevicePort.Text = textbox_DevicePairingCode.Text = string.Empty;
                Utils.ShowMessageDialog("Please check inserted IP and port!");
            }
        }

        private void button_ScanDevices_Click(object sender, RoutedEventArgs e)
        {
            ScanDevices();
        }

        private void button_AuthorizeDevice_Click(object sender, RoutedEventArgs e)
        {
            Device selectedDevice = comboBox_AuthDevices.SelectedItem as Device;
            selectedDevice.IsAuthorized = ADB.AuthorizeDevice(selectedDevice.ID);
            RefreshForm(true);         
        }

        /// <summary>
        /// Handles backup operation.
        /// </summary>
        private void button_StartBackup_Click(object sender, RoutedEventArgs e)
        {
            var extractDevice = comboBox_ExtractDevice.SelectedItem != null ? (Device)comboBox_ExtractDevice.SelectedItem : null;
            var backupDevice = comboBox_BackupDevice.SelectedItem != null ? (Device)comboBox_BackupDevice.SelectedItem : null;
            ADB.PerformBackup(extractDevice, backupDevice);
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                              DEVELOPMENT                           //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        private void button_ClearFilteredOutput_Click(object sender, RoutedEventArgs e)
        {
            textBlock_FilteredOutput.Text = string.Empty;
        }

        private void button_AddFakeDevice_Click(object sender, RoutedEventArgs e)
        {
            var random = new Random();
            Device fakeDevice = new Device();
            fakeDevice.ID = random.Next(1234, 6544).ToString();
            fakeDevice.IsWirelessConnected = true;

            switch (random.Next(0, 4))
            {
                case 0:
                    fakeDevice.Name = $"Pixel {random.Next(1, 5)}";
                    fakeDevice.IsAuthorized = true;
                    fakeDevice.HasUnlimitedBackup = true;
                    break;
                case 1:
                    fakeDevice.Name = $"Pixel {random.Next(1, 5)}";
                    fakeDevice.IsAuthorized = false;
                    fakeDevice.HasUnlimitedBackup = true;
                    break;
                case 2:
                    fakeDevice.Name = $"Redmi Note {random.Next(3, 15)}";
                    fakeDevice.IsAuthorized = false;
                    fakeDevice.HasUnlimitedBackup = false;
                    break;
                case 3:
                    fakeDevice.Name = $"Redmi Note {random.Next(3, 15)}";
                    fakeDevice.IsAuthorized = true;
                    fakeDevice.HasUnlimitedBackup = false;
                    break;
            }

            ADB.ConnectedDevices.Add(fakeDevice);

            int authorizedDevicesCount = 0;
            int unlimitedBackupDevicesCount = 0;
            foreach (Device device in ADB.ConnectedDevices)
            {
                authorizedDevicesCount = device.IsAuthorized ? authorizedDevicesCount + 1 : authorizedDevicesCount;
                unlimitedBackupDevicesCount = device.HasUnlimitedBackup ? unlimitedBackupDevicesCount + 1 : unlimitedBackupDevicesCount;
            }

            initializeResult = authorizedDevicesCount >= 2 && unlimitedBackupDevicesCount >= 1 ? true : false;
            
            RefreshForm(true);
        }
    }
}
