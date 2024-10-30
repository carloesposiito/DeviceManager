using GoogleBackupManager.Model.Exceptions;
using GoogleBackupManager.Functions;
using GoogleBackupManager.Model;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Net;
using GoogleBackupManager.UI;
using System.Linq;

namespace GoogleBackupManager
{
    public partial class MainWindow : Window
    {
        #region "Variables"

        /// <summary>
        /// Describes if program is ready to perform a backup.
        /// </summary>
        private bool _isProgramReady = false;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            ADB.InitializeConnection();
            RefreshForm();

#if DEBUG
            button_AddFakeDevice.Visibility = Visibility.Visible;
#endif

            // Because scan is not done yet, clear output
            textBlock_FilteredOutput.Text = string.Empty;
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                               FUNCTIONS                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        private void ScanDevices()
        {
            // Show waiting dialog
            WaitingDialog waitingDialog = new WaitingDialog();
            waitingDialog.Show();

            try
            {
                _isProgramReady = ADB.ScanDevices();

                if (_isProgramReady)
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
                textBlock_FilteredOutput.Text += "------------------------------------------------------------\n\n";
            }
        }

        /// <summary>
        /// Refreshes form and devices in filtered output.
        /// </summary>
        private void RefreshForm(bool clearPreviousOutput = false)
        {
            #region "Reset form"

            // Clear old combobox data
            comboBox_BackupDevice.Items.Clear();
            comboBox_ExtractDevice.Items.Clear();
            comboBox_AuthDevices.Items.Clear();
            comboBox_SelectAppsDevice.Items.Clear();
            comboBox_SelectFilesDevice.Items.Clear();
            comboBox_ScrcpyDevice.Items.Clear();
            comboBox_TransferFilesDevice.Items.Clear();

            // Disable unlimited backup, apps and files tabs
            tabItem_UnlimitedBackup.IsEnabled = tabItem_Apps.IsEnabled = tabItem_Files.IsEnabled = false;

            // Disable scrcpy and authorization groups
            groupBox_Authorization.IsEnabled = groupBox_ScreenCopy.IsEnabled = false;

            #endregion

            int connectedDevicesCount = ADB.ConnectedDevices.Count;

            if (connectedDevicesCount > 0)
            {
                #region "Refresh form"               

                #region "Put elements in right combos"

                foreach (Device device in ADB.ConnectedDevices)
                {
                    if (!device.IsAuthorized)
                    {
                        comboBox_AuthDevices.Items.Add(device);
                        comboBox_AuthDevices.SelectedIndex = 0;
                        continue;
                    }
                    else
                    {
                        // Add to apps combo
                        comboBox_SelectAppsDevice.Items.Add(device);

                        // Add to files combo
                        comboBox_SelectFilesDevice.Items.Add(device);

                        // Add to transfer files combo
                        comboBox_TransferFilesDevice.Items.Add(device);

                        // Add to scrcpy combo
                        comboBox_ScrcpyDevice.Items.Add(device);

                        // Set selected element to first index
                        comboBox_SelectAppsDevice.SelectedIndex = comboBox_SelectFilesDevice.SelectedIndex = comboBox_TransferFilesDevice.SelectedIndex = comboBox_ScrcpyDevice.SelectedIndex = 0;
                    }

                    if (device.HasUnlimitedBackup)
                    {
                        // Device has no name so it's property about unlimited backup is not valid
                        // So I add it also in extract device ComboBox
                        if (device.Name.Contains("Undefined"))
                        {
                            comboBox_ExtractDevice.Items.Add(device);
                            comboBox_ExtractDevice.SelectedIndex = 0;
                        }

                        comboBox_BackupDevice.Items.Add(device);
                        comboBox_BackupDevice.SelectedIndex = 0;
                    }
                    else
                    {
                        comboBox_ExtractDevice.Items.Add(device);
                        comboBox_ExtractDevice.SelectedIndex = 0;
                    }
                }

                #endregion

                #region "Enable/disable unlimited backup combos"

                // Enable authorization group only if there's a device in its combo
                groupBox_Authorization.IsEnabled = comboBox_AuthDevices.Items.Count > 0;

                // Enable unlimited backup tab only if there's at least one device in both extract and backup combos
                tabItem_UnlimitedBackup.IsEnabled = comboBox_ExtractDevice.Items.Count > 0 && comboBox_BackupDevice.Items.Count > 0;

                // Enable apps, files and scrcpy tab only if there's at least one device in uninstall app combobox
                tabItem_Apps.IsEnabled = tabItem_Files.IsEnabled = groupBox_ScreenCopy.IsEnabled = comboBox_SelectAppsDevice.Items.Count > 0;

                #endregion

                #endregion

                #region "Write info about devices in filtered output"

                // Enable backup button in backup tab only if program is ready to work
                if (connectedDevicesCount.Equals(1))
                {
                    WriteToOutput("Only one device connected!\nPlease connect another device to continue.", true, true, false);
                }

                if (ADB.ConnectedDevices.Any(connectedDevice => !connectedDevice.IsAuthorized))
                {
                    WriteToOutput("Seems there are devices unauthorized!\nPlease authorize them to continue.", false, true, false);
                }

                WriteToOutput(string.Empty, false, false, true);

                WriteToOutput("Connected devices:", clearPreviousOutput, true, false);

                foreach (Device device in ADB.ConnectedDevices)
                {
                    WriteToOutput(device.ToString(), false, true, false);
                }

                WriteToOutput(string.Empty, false, false, true);

                #endregion
            }
            else
            {
                WriteToOutput("No devices connected!\nScan devices again to continue.", true, false, false);
            }
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
        //                                  UTILS                             //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

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

            RefreshForm(true);
        }

        private void button_StartScreenCopy_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button_ShowRawOutput_Click(object sender, RoutedEventArgs e)
        {

        }

        private void checkBox_WhatsAppAll_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)checkBox_WhatsAppAll.IsChecked)
            {
                // Check also other WhatsApp checkboxes
                checkBox_WhatsApp_Backups.IsChecked = checkBox_WhatsApp_Database.IsChecked = checkBox_WhatsApp_Media.IsChecked = true;
            }
            else
            {
                checkBox_WhatsApp_Backups.IsChecked = checkBox_WhatsApp_Database.IsChecked = checkBox_WhatsApp_Media.IsChecked = false;
            }
        }
    }
}
