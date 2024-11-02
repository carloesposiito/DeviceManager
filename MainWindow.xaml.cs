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
using System.IO;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media.Animation;
using System.Collections.Generic;

namespace GoogleBackupManager
{
    public partial class MainWindow : Window
    {
        #region "Variables"

        private WaitingDialog _waitingDialog;
        private bool _isUnlimitedBackupAvailable = false;

        // Monitor variables
        private readonly Object _syncMonitor = new object();
        private bool _isScanning = false;

        // Mouse scrolling variables
        private bool _isScrolling = false;
        private Point _startPoint;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            _waitingDialog = new WaitingDialog("Loading program...");
            _waitingDialog.Show();
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                               FUNCTIONS                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        #region "Functions"        

        private async void ScanDevices()
        {
            if (!_isScanning)
            {
                bool lockAcquired = false;
                try
                {
                    _isScanning = true;
                    lockAcquired = Monitor.TryEnter(_syncMonitor);

                    if (lockAcquired)
                    {
                        ResetForm();

                        await RunOnUIThreadAsync(() => WriteToOutput("Scanning devices...", true, true));

                        try
                        {
                            await ADB.ScanDevices();
                            await RunOnUIThreadAsync(() => RefreshForm(false));
                        }
                        catch (PlatformToolsFolderException ex)
                        {
                            await RunOnUIThreadAsync(() =>
                            {
                                Utils.ShowMessageDialog($"{ex.Message}\nAborting...");
                                Process.GetCurrentProcess().Kill();
                            });
                        }
                        catch (CommandPromptException ex)
                        {
                            await RunOnUIThreadAsync(() =>
                            {
                                Utils.ShowMessageDialog($"{ex.Message}\nAborting...");
                                Process.GetCurrentProcess().Kill();
                            });
                        }
                        catch (PlatformToolsProcessException ex)
                        {
                            await RunOnUIThreadAsync(() =>
                            {
                                RefreshForm(false);
                                WriteToOutput($"{ex.Message}", true, false, false);
                            });
                        }
                        catch (Exception ex)
                        {
                            await RunOnUIThreadAsync(() =>
                            {
                                RefreshForm(false);
                                WriteToOutput($"{ex.Message}\nPlease try again.", true, false, false);
                            });
                        }
                    }
                }
                finally
                {
                    if (lockAcquired)
                    {
                        Monitor.Exit(_syncMonitor);
                    }
                    _isScanning = false;
                }
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
                textBlock_FilteredOutput.Text += $"{messageToWrite}\n";
            }

            if (newLineAfterMessage)
            {
                textBlock_FilteredOutput.Text += "\n";
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
            ResetForm();

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

                    // Add to unlimited backup device combo only if a Pixel
                    if (device.HasUnlimitedBackup)
                    {
                        comboBox_BackupDevice.Items.Add(device);
                        comboBox_BackupDevice.SelectedIndex = 0;
                    }

                    // Add to extract device combo
                    comboBox_ExtractDevice.Items.Add(device);
                    comboBox_ExtractDevice.SelectedIndex = 0;
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

                if (ADB.ConnectedDevices.Any(connectedDevice => !connectedDevice.IsAuthorized))
                {
                    WriteToOutput("Seems there is/are devices unauthorized!\nPlease authorize it/them to use it/them.", false, true, false);
                }

                if (connectedDevicesCount > 1)
                {
                    if (!_isUnlimitedBackupAvailable)
                    {
                        WriteToOutput("Unlimited backup tab disabled.", false, true, false);
                    }
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
                WriteToOutput("No devices connected!\nScan devices again to continue.", false, false, false);
            }
        }

        /// <summary>
        /// Resets all controls in form.
        /// </summary>
        private void ResetForm()
        {
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
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                                EVENTS                              //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        #region "Events"

        #region "Form general events"

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;

            try
            {
                await ADB.InitializeConnection();
            }
            catch (PlatformToolsFolderException ex)
            {
                _waitingDialog.Close();
                Utils.ShowMessageDialog(ex.Message);
                Process.GetCurrentProcess().Kill();
            }
            finally
            {
                _waitingDialog.Close();
                ResetForm();
                this.ShowInTaskbar = true;
                this.Visibility = Visibility.Visible;

#if DEBUG
                button_AddFakeDevice.Visibility = Visibility.Visible;
#endif

                // Clear filtered output because scanning is not already done
                textBlock_FilteredOutput.Text = string.Empty;
            }
        }

        private void border_Upper_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private async void button_Close_Click(object sender, RoutedEventArgs e)
        {
            await ADB.CloseConnection();
            Close();
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                              DEVICES TAB                           //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        private void button_ScanDevices_Click(object sender, RoutedEventArgs e)
        {
            ScanDevices();
        }

        private async void button_AuthorizeDevice_Click(object sender, RoutedEventArgs e)
        {
            if (!_isScanning && Monitor.TryEnter(_syncMonitor))
            {
                try
                {
                    Device selectedDevice = comboBox_AuthDevices.SelectedItem as Device;
                    if (await ADB.AuthorizeDevice(selectedDevice.ID))
                    {
                        ScanDevices();
                    }
                    else
                    {
                        ScanDevices();
                    }
                }
                finally
                {
                    Monitor.Exit(_syncMonitor);
                }
            }
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

        private async void button_ConnectPairDevice_Click(object sender, RoutedEventArgs e)
        {
            if (!_isScanning && Monitor.TryEnter(_syncMonitor))
            {
                try
                {
                    // Check valid data
                    if (IPAddress.TryParse(textBox_DeviceIp.Text, out _) && int.TryParse(textbox_DevicePort.Text, out _))
                    {
                        bool result = false;

                        if ((bool)checkBox_Pairing.IsChecked)
                        {
                            result = await ADB.PairWirelessDevice(textBox_DeviceIp.Text, textbox_DevicePort.Text, textbox_DevicePairingCode.Text);
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
                            result = await ADB.ConnectWirelessDevice(textBox_DeviceIp.Text, textbox_DevicePort.Text);
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
                finally
                {
                    Monitor.Exit(_syncMonitor);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                                 APPS TAB                           //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                                FILES TAB                           //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

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

        private void checkBox_Everything_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)checkBox_Everything.IsChecked)
            {
                // Check all checkboxes
                foreach (var item in grid_ExtractCheckBoxes.Children)
                {
                    var itemType = item.GetType();
                    if (itemType.Name.Equals("CheckBox"))
                    {
                        CheckBox tempItem = item as CheckBox;
                        tempItem.IsChecked = true;
                    }
                }
            }
            else
            {
                // Uncheck all checkboxes
                foreach (var item in grid_ExtractCheckBoxes.Children)
                {
                    var itemType = item.GetType();
                    if (itemType.Name.Equals("CheckBox"))
                    {
                        CheckBox tempItem = item as CheckBox;
                        tempItem.IsChecked = false;
                    }
                }
            }
        }


        private async void button_Extract_Click(object sender, RoutedEventArgs e)
        {
            WriteToOutput("Extracting files, please wait...");

            // Get selected source device
            Device sourceDevice = comboBox_SelectFilesDevice.SelectedItem as Device;
            
            // Populate folder to be extracted list according to selected checkboxes
            List<string> foldersToBeExtracted = new List<string>();

            if ((bool)checkBox_Everything.IsChecked)
            {
                foldersToBeExtracted.Add(sourceDevice.DeviceFolderPath);
            }
            else
            {
                if ((bool)checkBox_WhatsAppAll.IsChecked)
                {
                    foldersToBeExtracted.Add(sourceDevice.WhatsAppFolderPath);
                }
                else
                {
                    if ((bool)checkBox_WhatsApp_Backups.IsChecked)
                    {
                        foldersToBeExtracted.Add(sourceDevice.WhatsAppBackupsFolderPath);
                    }

                    if ((bool)checkBox_WhatsApp_Database.IsChecked)
                    {
                        foldersToBeExtracted.Add(sourceDevice.WhatsAppDatabasesFolderPath);
                    }
                    
                    if ((bool)checkBox_WhatsApp_Media.IsChecked)
                    {
                        foldersToBeExtracted.Add(sourceDevice.WhatsAppMediaFolderPath);
                    }
                }

                if ((bool)checkBox_Alarms.IsChecked)
                {
                    foldersToBeExtracted.Add(sourceDevice.AlarmsFolderPath);
                }

                if ((bool)checkBox_DCIM.IsChecked)
                {
                    foldersToBeExtracted.Add(sourceDevice.DcimFolderPath);
                }

                if ((bool)checkBox_Documents.IsChecked)
                {
                    foldersToBeExtracted.Add(sourceDevice.DocumentsFolderPath);
                }

                if ((bool)checkBox_Music.IsChecked)
                {
                    foldersToBeExtracted.Add(sourceDevice.MusicFolderPath);
                }

                if ((bool)checkBox_Pictures.IsChecked)
                {
                    foldersToBeExtracted.Add(sourceDevice.PicturesFolderPath);
                }

                if ((bool)checkBox_Ringtones.IsChecked)
                {
                    foldersToBeExtracted.Add(sourceDevice.RingtonesFolderPath);
                }
            }

            var operationResult = await ADB.ExecutePullCommand(sourceDevice, foldersToBeExtracted);

           // WriteToOutput($"{operationResult}/{filesCount} files copied.", false, true, true);
        }




        private void button_SelectTransferFileFolder(object sender, RoutedEventArgs e)
        {
            textBox_FilesToTransferPath.Text = Utils.SelectFolder();
        }

        private async void button_TransferFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedDir = textBox_FilesToTransferPath.Text;

                int filesCount = Directory.GetFiles(selectedDir, "*", SearchOption.AllDirectories).Count();
                if (Directory.Exists(selectedDir) && filesCount > 0)
                {
                    WriteToOutput("Copying files, please wait...");

                    Device destinationDevice = comboBox_TransferFilesDevice.SelectedItem as Device;
                    var operationResult = await ADB.ExecutePushCommand(destinationDevice.ID, destinationDevice.DocumentsFolderPath, $"{selectedDir}");

                    WriteToOutput($"{operationResult}/{filesCount} files copied.", false, true, true);                   
                }
                else
                {
                    Utils.ShowMessageDialog("Selected folder not exists or is empty!");
                }
            }
            catch (Exception ex)
            {
                WriteToOutput($"Error while copying files!\n{ex.Message}", false, true, true);
            }
            finally
            {
                textBox_FilesToTransferPath.Text = string.Empty;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                           UNLIMITED BACKUP TAB                     //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles backup operation.
        /// </summary>
        private async void button_StartBackup_Click(object sender, RoutedEventArgs e)
        {
            if (!_isScanning && Monitor.TryEnter(_syncMonitor))
            {
                try
                {
                    var extractDevice = comboBox_ExtractDevice.SelectedItem as Device;
                    var backupDevice = comboBox_BackupDevice.SelectedItem as Device;

                    if (!extractDevice.Equals(backupDevice))
                    {
                        //await ADB.PerformBackup(extractDevice, backupDevice, (bool)checkBox_SaveFilesIntoPC.IsChecked);
                    }
                    else
                    {
                        Utils.ShowMessageDialog("Extract and backup device can't be the same!\nPlease choose different devices!");
                    }
                }
                finally
                {
                    Monitor.Exit(_syncMonitor);
                }
            }
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

            #region "Check if connected devices are enough to perform backup"

            int authorizedDevicesCount = 0;
            int unlimitedBackupDevicesCount = 0;
            foreach (Device device in ADB.ConnectedDevices)
            {
                authorizedDevicesCount = device.IsAuthorized ? authorizedDevicesCount + 1 : authorizedDevicesCount;
                unlimitedBackupDevicesCount = device.HasUnlimitedBackup ? unlimitedBackupDevicesCount + 1 : unlimitedBackupDevicesCount;
            }

            _isUnlimitedBackupAvailable = authorizedDevicesCount >= 2 && unlimitedBackupDevicesCount >= 1 ? true : false;

            #endregion

            RefreshForm(true);
        }

        private void button_ShowRawOutput_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button_StartScreenCopy_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                           PRIVATE UTILITIES                        //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        private async void ScanDevices_NoMonitor()
        {
            WriteToOutput("Scanning devices...", true, true);

            try
            {
                await ADB.ScanDevices();
                RefreshForm(false);
            }
            catch (PlatformToolsFolderException ex)
            {
                // What to do when platform tools folder is not found
                Utils.ShowMessageDialog($"{ex.Message}\nAborting...");
                Process.GetCurrentProcess().Kill();
            }
            catch (CommandPromptException ex)
            {
                // What to do when command prompt is not active anymore
                Utils.ShowMessageDialog($"{ex.Message}\nAborting...");
                Process.GetCurrentProcess().Kill();
            }
            catch (PlatformToolsProcessException ex)
            {
                // What to do when platform tools folder is not found
                RefreshForm(false);
                WriteToOutput($"{ex.Message}", true, false, false);
            }
            catch (Exception ex)
            {
                // What to do when there's a general error.
                RefreshForm(false);
                WriteToOutput($"{ex.Message}\nPlease try again.", true, false, false);
            }
        }

        private Task RunOnUIThreadAsync(Action action)
        {
            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }

        
    }
}
