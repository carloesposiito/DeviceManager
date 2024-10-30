using GoogleBackupManager.Model.Exceptions;
using GoogleBackupManager.Functions;
using GoogleBackupManager.Model;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Net;
using GoogleBackupManager.UI;

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
            tabItem_Development.Visibility = Visibility.Visible;
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

            // Disable everything, leave only the possibility to scan devices again
            // Disable backup tab and put devices one as active
            tabItem_UnlimitedBackup.IsEnabled = false;
            tabControl_Program.SelectedIndex = 1;

            // Disable authorization GroupBox
            groupBox_Authorization.IsEnabled = false;

            #endregion

            int connectedDevicesCount = ADB.ConnectedDevices.Count;

            if (connectedDevicesCount > 0)
            {
                #region "Refresh form"

                // Enable backup tab
                tabItem_UnlimitedBackup.IsEnabled = true;

                // Enable backup button in backup tab only if program is ready to work
                if (connectedDevicesCount.Equals(1))
                {
                    WriteToOutput("Only one device connected!\nPlease connect another device to continue.", true, true, true);
                }

                #region "Put elements in right ComboBoxes"

                foreach (Device device in ADB.ConnectedDevices)
                {
                    if (!device.IsAuthorized)
                    {
                        comboBox_AuthDevices.Items.Add(device);
                        comboBox_AuthDevices.SelectedIndex = 0;
                        continue;
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

                #region "Enable/disable ComboBoxes"

                // Enable authorization group only if a device needs to be authorized
                groupBox_Authorization.IsEnabled = comboBox_AuthDevices.Items.Count > 0;

                // Enable extract device ComboBox only there's at least one device
                comboBox_ExtractDevice.IsEnabled = comboBox_ExtractDevice.Items.Count > 0;

                // Enable backup device ComboBox only there's at least one device
                comboBox_BackupDevice.IsEnabled = comboBox_BackupDevice.Items.Count > 0;

                // Enable backup button only if there are enough devices.
                if (comboBox_ExtractDevice.IsEnabled && comboBox_BackupDevice.IsEnabled)
                {
                    button_PerformBackup.IsEnabled = true;
                    tabControl_Program.SelectedIndex = 0;
                }
                else
                {
                    button_PerformBackup.IsEnabled = false;
                }

                #endregion

                #endregion

                WriteToOutput("Connected devices:", clearPreviousOutput, true, false);

                foreach (Device device in ADB.ConnectedDevices)
                {
                    WriteToOutput(device.ToString(), false, true, false);
                }

                WriteToOutput(string.Empty, false, false, true);
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
        //                              DEVELOPMENT                           //
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
    }
}
