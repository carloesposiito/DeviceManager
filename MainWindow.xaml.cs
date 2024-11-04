using AndroidDeviceManager.Model.Exceptions;
using AndroidDeviceManager.Functions;
using AndroidDeviceManager.Model;
using System;
using System.Windows;
using System.Windows.Input;
using System.Net;
using AndroidDeviceManager.UI;
using System.Linq;
using System.IO;
using System.Windows.Controls;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using static AndroidDeviceManager.Functions.Utils;
using System.Diagnostics;

namespace AndroidDeviceManager
{
    public partial class MainWindow : Window
    {
        #region "Variables"

        private bool _isUnlimitedBackupAvailable = false;
        private List<string> _extractedDirs = new List<string>();

        // Monitor variables
        private readonly Object _syncMonitor = new object();
        private bool isBusy = false;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                               FUNCTIONS                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        #region "Functions"        

        private async void ScanDevices()
        {
            bool isLocked = !Monitor.TryEnter(_syncMonitor);

            if (!isBusy && !isLocked)
            {
                try
                {
                    isBusy = true;

                    ResetForm();
                    WriteToOutput("Scanning devices...", true, true, false);

                    await ADB.ScanDevices();

                    RefreshForm();
                }
                catch (PlatformToolsProcessException ex)
                {
                    WriteToOutput(ex.Message, false, true, true);
                }
                catch (Exception ex)
                {
                    WriteToOutput(ex.Message, false, true, true);
                }
                finally
                {
                    if (isLocked)
                    {
                        Monitor.Exit(_syncMonitor);
                    }
                    isBusy = false;
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
        /// Refreshes form and filtered output.
        /// </summary>
        private void RefreshForm()
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
                        comboBox_UninstallAppsDevice.Items.Add(device);

                        // Add to extract files combo
                        comboBox_ExtractFilesDevice.Items.Add(device);

                        // Add to restore files combo
                        comboBox_RestoreFilesDevice.Items.Add(device);

                        // Add to transfer files combo
                        comboBox_TransferFilesDevice.Items.Add(device);

                        // Add to scrcpy combo
                        comboBox_ScrcpyDevice.Items.Add(device);

                        // Set selected element to first index
                        comboBox_UninstallAppsDevice.SelectedIndex = comboBox_ExtractFilesDevice.SelectedIndex = comboBox_RestoreFilesDevice.SelectedIndex = comboBox_TransferFilesDevice.SelectedIndex = comboBox_ScrcpyDevice.SelectedIndex = 0;
                    }

                    // Add to unlimited backup device combo only if a Pixel
                    if (device.HasUnlimitedBackup)
                    {
                        comboBox_UnlimitedBackupDevice.Items.Add(device);
                        comboBox_UnlimitedBackupDevice.SelectedIndex = 0;
                    }

                    // Add to extract device combo
                    comboBox_ExtractDevice.Items.Add(device);
                    comboBox_ExtractDevice.SelectedIndex = 0;
                }

                #endregion

                #region "Enable/disable groupboxes and comboboxes"

                // Enable authorization group only if there's a device in its combo
                groupBox_Authorization.IsEnabled = comboBox_AuthDevices.Items.Count > 0;

                // Make unlimited backup tab visible only if there's at least two devices
                if (connectedDevicesCount >= 2)
                {
                    _isUnlimitedBackupAvailable = comboBox_ExtractDevice.Items.Count > 0 && comboBox_UnlimitedBackupDevice.Items.Count > 0;
                    tabItem_UnlimitedBackup.Visibility = Visibility.Visible;
                }

                // Enable apps, files and utils tab only if there's at least one device in uninstall app combobox
#if DEBUG
                tabItem_Apps.IsEnabled = tabItem_Files.IsEnabled = groupBox_ScreenCopy.IsEnabled = comboBox_UninstallAppsDevice.Items.Count > 0;
#else
                tabItem_Apps.IsEnabled = tabItem_Files.IsEnabled = tabItem_Utils.IsEnabled = comboBox_UninstallAppsDevice.Items.Count > 0;
#endif

                #endregion

                #endregion

                #region "Write info about devices in filtered output"

                if (ADB.ConnectedDevices.Any(connectedDevice => !connectedDevice.IsAuthorized))
                {
                    WriteToOutput("Seems there is/are devices unauthorized!\nPlease authorize it/them to use it/them.", false, true, false);
                }

                WriteToOutput(string.Empty, false, false, true);
                WriteToOutput("Connected devices:", false, true, false);

                foreach (Device device in ADB.ConnectedDevices)
                {
                    WriteToOutput(device.ToString(), false, true, false);
                }

                WriteToOutput(string.Empty, false, false, true);

                #endregion
            }
            else
            {
                WriteToOutput("No devices connected!\nScan devices again to continue.", false, true, true);
            }
        }

        /// <summary>
        /// Refreshes extracted folders combobox.
        /// </summary>
        private void RefreshExtractedFolders()
        {
            _extractedDirs = Directory.GetDirectories(Utils.ProgramFolders.ExtractDirectory).ToList();
            comboBox_RestoredFiles.Items.Clear();

            if (_extractedDirs.Count() > 0)
            {
                foreach (string dir in _extractedDirs)
                {
                    const string PATTERN = "\\Extracted\\";
                    comboBox_RestoredFiles.Items.Add(dir.Substring(dir.IndexOf(PATTERN) + PATTERN.Length));
                    comboBox_RestoredFiles.SelectedIndex = 0;
                };

                groupBox_RestoreFolders.IsEnabled = true;
            }
            else
            {
                groupBox_RestoreFolders.IsEnabled = false;
            }
        }

        /// <summary>
        /// Resets all controls in form.
        /// </summary>
        private void ResetForm()
        {
            // Clear old combobox data
            comboBox_UnlimitedBackupDevice.Items.Clear();
            comboBox_ExtractDevice.Items.Clear();
            comboBox_AuthDevices.Items.Clear();
            comboBox_UninstallAppsDevice.Items.Clear();
            comboBox_ExtractFilesDevice.Items.Clear();
            comboBox_ScrcpyDevice.Items.Clear();
            comboBox_TransferFilesDevice.Items.Clear();
            comboBox_RestoreFilesDevice.Items.Clear();

            // Hide unlimited backup tab
            // Disable apps, files and utils tabs
#if DEBUG
            tabItem_UnlimitedBackup.Visibility = Visibility.Collapsed;
            tabItem_Apps.IsEnabled = tabItem_Files.IsEnabled = false;

            groupBox_ScreenCopy.IsEnabled = false;
#else
            tabItem_UnlimitedBackup.Visibility = Visibility.Collapsed;
            tabItem_Apps.IsEnabled = tabItem_Files.IsEnabled = tabItem_Utils.IsEnabled = false;
#endif

            // Disable authorization group
            groupBox_Authorization.IsEnabled = false;
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
            // Keep window hidden till ADB server initialized
            Visibility = Visibility.Collapsed;

            // Show waiting dialog
            WaitingDialog _waitingDialog;
            _waitingDialog = new WaitingDialog();
            _waitingDialog.Show();

            try
            {
                await ADB.InitializeConnection();
            }
            catch (PlatformToolsFolderException ex)
            {
                WriteToOutput($"Error during initialize:", false, true, false);
                WriteToOutput(ex.Message, false, true, false);
                WriteToOutput("Plese download program again!", false, true, true);

                // Disable scan and connect/pair groupboxes
                button_ScanDevices.IsEnabled = false;
                groupBox_ConnectPairDevice.IsEnabled = false;
            }
            catch (PlatformToolsProcessException ex)
            {
                WriteToOutput(ex.Message, false, true, true);
            }
            finally
            {
                // Populate extracted folders combobox
                RefreshExtractedFolders();

                ResetForm();

#if DEBUG
                button_AddFakeDevice.Visibility = Visibility.Visible;
#endif

                _waitingDialog.Close();

                // Make window visible after initializing ADB server
                ShowInTaskbar = true;
                Visibility = Visibility.Visible;
            }
        }

        private void border_Upper_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private async void button_Close_Click(object sender, RoutedEventArgs e)
        {
            if (!isBusy && Monitor.TryEnter(_syncMonitor))
            {
                isBusy = true;

                try
                {
                    WriteToOutput("Closing program...");
                    await ADB.CloseConnection();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex.Message);
#endif
                }
                finally
                {
                    isBusy = false;
                    Monitor.Exit(_syncMonitor);
                    Close();
                }
            }
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
            if (!isBusy && Monitor.TryEnter(_syncMonitor))
            {
                isBusy = true;
                
                try
                {    
                    Device selectedDevice = comboBox_AuthDevices.SelectedItem as Device;
                    await ADB.AuthorizeDevice(selectedDevice.ID);
                    ScanDevices();
                }
                finally
                {
                    isBusy = false;
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
            if (!isBusy && Monitor.TryEnter(_syncMonitor))
            {
                isBusy = true;

                try
                {
                    // Check valid data
                    if (IPAddress.TryParse(textBox_DeviceIp.Text, out _) && int.TryParse(textbox_DevicePort.Text, out _))
                    {
                        if ((bool)checkBox_Pairing.IsChecked)
                        {
                            if (await ADB.PairWirelessDevice(textBox_DeviceIp.Text, textbox_DevicePort.Text, textbox_DevicePairingCode.Text))
                            {
                                WriteToOutput("Pairing succed! Try to scan devices again.\nIf device doesn't appear, try connect it first.", false, true, true);
                            }
                            else
                            {
                                WriteToOutput("Pairing failed! Check inserted data and try again.", false, true, true);
                            }

                            checkBox_Pairing.IsChecked = false;
                            textbox_DevicePort.Text = textbox_DevicePairingCode.Text = string.Empty;
                        }
                        else
                        {
                            if (await ADB.ConnectWirelessDevice(textBox_DeviceIp.Text, textbox_DevicePort.Text))
                            {
                                // Erase ip textbox
                                textBox_DeviceIp.Text = string.Empty;
                                ScanDevices();
                            }
                            else
                            {
                                WriteToOutput("Connection failed! Try to pair device first.", false, true, true);
                            }

                            // Erase port and device pairing code
                            textbox_DevicePort.Text = textbox_DevicePairingCode.Text = string.Empty;
                        }
                    }
                    else
                    {
                        // Erase data in all TextBoxes
                        textBox_DeviceIp.Text = textbox_DevicePort.Text = textbox_DevicePairingCode.Text = string.Empty;
                        WriteToOutput("Please check inserted IP and port!", false, true, true);
                    }
                }
                catch (PlatformToolsProcessException ex)
                {
                    WriteToOutput(ex.Message, false, true, true);
                }
                finally
                {
                    isBusy = false;
                    Monitor.Exit(_syncMonitor);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                                 APPS TAB                           //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        private async void comboBox_UninstallAppsDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_UninstallAppsDevice.SelectedItem != null)
            {
                Device selectedDevice = comboBox_UninstallAppsDevice.SelectedItem as Device;
                var apps = await ADB.GetApplications(selectedDevice.ID);
                var allApps = apps.Item1;
                var systemApps = apps.Item2;
                var thirdPartyApps = apps.Item3;

                foreach (var item in allApps)
                {
                    comboBox_DeviceAllApps.Items.Add(item);
                }
                comboBox_DeviceAllApps.SelectedIndex = 0;

                foreach (var item in systemApps)
                {
                    comboBox_DeviceSystemApps.Items.Add(item);
                }
                comboBox_DeviceSystemApps.SelectedIndex = 0;

                foreach (var item in thirdPartyApps)
                {
                    comboBox_DeviceThirdPartyApps.Items.Add(item);
                }
                comboBox_DeviceThirdPartyApps.SelectedIndex = 0;
            }
        }

        private async void button_UninstallSystemApp_Click(object sender, RoutedEventArgs e)
        {
            Device selectedDevice = comboBox_UninstallAppsDevice.SelectedItem as Device;
            string selectedItem = comboBox_DeviceSystemApps.SelectedItem as string;
            await UninstallApp(selectedDevice, selectedItem);
        }

        private async void button_UninstallThirdPartyApp_Click(object sender, RoutedEventArgs e)
        {
            Device selectedDevice = comboBox_UninstallAppsDevice.SelectedItem as Device;
            string selectedItem = comboBox_DeviceThirdPartyApps.SelectedItem as string;
            await UninstallApp(selectedDevice, selectedItem);
        }

        private async void button_UninstallApp_Click(object sender, RoutedEventArgs e)
        {
            Device selectedDevice = comboBox_UninstallAppsDevice.SelectedItem as Device;
            string selectedItem = comboBox_DeviceAllApps.SelectedItem as string;
            await UninstallApp(selectedDevice, selectedItem);
        }

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
            if (!isBusy && Monitor.TryEnter(_syncMonitor))
            {
                isBusy = true;

                try
                {
                    WriteToOutput("Extracting files, please wait...", false, false, false);

                    // Get selected source device
                    Device sourceDevice = comboBox_ExtractFilesDevice.SelectedItem as Device;

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

                        if ((bool)checkBox_Downloads.IsChecked)
                        {
                            foldersToBeExtracted.Add(sourceDevice.DownloadsFolderPath);
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

                    var filesCount = await ADB.ExecutePullCommand(sourceDevice, foldersToBeExtracted);

                    WriteToOutput($"{filesCount} files extracted.", false, true, true);
                }
                catch (PlatformToolsProcessException ex)
                {
                    WriteToOutput(ex.Message, false, true, true);
                }
                catch (Exception ex)
                {
                    WriteToOutput($"Error while extracing files from device!\n{ex.Message}", false, true, true);
                }
                finally
                {
                    isBusy = false;
                    Monitor.Exit(_syncMonitor);
                    RefreshExtractedFolders();

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
        }

        private async void button_Restore_Click(object sender, RoutedEventArgs e)
        {
            if (!isBusy && Monitor.TryEnter(_syncMonitor))
            {
                isBusy = true;

                try
                {
                    if (_extractedDirs.Count() > 0)
                    {
                        WriteToOutput("Restoring files, please wait...", false, false, false);

                        // Get selected destination device
                        Device destinationDevice = comboBox_RestoreFilesDevice.SelectedItem as Device;

                        string selectedFolder = comboBox_RestoredFiles.SelectedItem as string;
                        string selectedFolderPath = _extractedDirs.Where(dir => dir.Contains(selectedFolder)).First();

                        int filesCount = Directory.GetFiles(selectedFolderPath, "*", SearchOption.AllDirectories).Count();
                        int restoredFilesCount = await RestoringProcess(selectedFolderPath, destinationDevice.ID, destinationDevice.DeviceFolderPath);

                        WriteToOutput($"{restoredFilesCount}/{filesCount} files restored.", false, true, true);
                    }
                    else
                    {
                        WriteToOutput($"0 files restored.", false, true, true);
                    }
                }
                catch (PlatformToolsProcessException ex)
                {
                    WriteToOutput(ex.Message, false, true, true);
                }
                catch (Exception ex)
                {
                    WriteToOutput($"Error while restoring files to device!\n{ex.Message}", false, true, true);
                }
                finally
                {
                    isBusy = false;
                    Monitor.Exit(_syncMonitor);
                }
            }
        }

        private void button_SelectTransferFileFolder(object sender, RoutedEventArgs e)
        {
            textBox_FilesToTransferPath.Text = Utils.SelectFolder();
        }

        private async void button_TransferFiles_Click(object sender, RoutedEventArgs e)
        {
            if (!isBusy && Monitor.TryEnter(_syncMonitor))
            {
                isBusy = true;

                try
                {
                    string selectedDir = textBox_FilesToTransferPath.Text;

                    int filesCount = Directory.GetFiles(selectedDir, "*", SearchOption.AllDirectories).Count();
                    if (Directory.Exists(selectedDir) && filesCount > 0)
                    {
                        WriteToOutput("Transferring files, please wait...", false, false, false);

                        Device destinationDevice = comboBox_TransferFilesDevice.SelectedItem as Device;
                        var operationResult = await ADB.ExecutePushCommand(destinationDevice.ID, destinationDevice.DocumentsFolderPath, $"{selectedDir}");

                        WriteToOutput($"{operationResult}/{filesCount} files transferred to device.", false, true, true);
                    }
                    else
                    {
                        Utils.ShowMessageDialog("Selected folder doesn't exists or is empty!");
                    }
                }
                catch (Exception ex)
                {
                    WriteToOutput($"Error while transferring files to device!\n{ex.Message}", false, true, true);
                }
                finally
                {
                    isBusy = false;
                    textBox_FilesToTransferPath.Text = string.Empty;
                    Monitor.Exit(_syncMonitor);
                }
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
            if (!isBusy && Monitor.TryEnter(_syncMonitor))
            {
                isBusy = true;

                try
                {
                    var extractDevice = comboBox_ExtractDevice.SelectedItem as Device;
                    var backupDevice = comboBox_UnlimitedBackupDevice.SelectedItem as Device;

                    if (!extractDevice.Equals(backupDevice))
                    {
                        WriteToOutput("Unlimited backup process started.", false, true, false);

                        #region "Pull from extract device"

                        List<string> tempCameraList = new List<string>
                        {
                            extractDevice.CameraFolderPath
                        };

                        WriteToOutput($"Extracting files in Camera folder from extract device...", false, false, false);

                        // Executes a pull command direct to backup unlimited device folder
                        int pulledFromExtractDeviceCount = await ADB.ExecutePullCommand(extractDevice, tempCameraList, true);

                        WriteToOutput($"Extracted {pulledFromExtractDeviceCount} files.", false, true, false);

                        #endregion

                        #region "Push into backup device"

                        // Shutdown WiFi if connected with cable in order to disable backup in google photo before deleting that photos.
                        // Ensure backup device is connected with USB

                        WriteToOutput($"Transferring files to Camera folder of unlimited backup device...", false, false, false);

                        int pushedToBackupDeviceCount = await RestoringProcess(Utils.ProgramFolders.UnlimitedBackupDeviceDirectory, backupDevice.ID, backupDevice.DeviceFolderPath);

                        WriteToOutput($"Tranferred {pushedToBackupDeviceCount}/{pulledFromExtractDeviceCount} files.", false, true, false);

                        #endregion

                        if ((bool)checkBox_DeleteExtractedPhotos.IsChecked)
                        {
                            // Delete photos from extract device
                            // They'll be uploaded again after upload in unlimited backup device
                            if (pulledFromExtractDeviceCount == pushedToBackupDeviceCount)
                            {
                                WriteToOutput($"Deleting photos from extract device...", false, false, false);
                                await ADB.ExecuteDeleteCameraCommand(extractDevice.ID);
                                WriteToOutput($"Photos deleted successfully!", false, true, true);
                            }
                            else
                            {
                                WriteToOutput($"Extracted and trasferred files count mismatch!\nDelete photos from extract device skipped for safety!", false, true, true);
                            }
                        }
                    }
                    else
                    {
                        Utils.ShowMessageDialog("Extract and backup device can't be the same!\nPlease choose different devices!");
                    }
                }
                catch (Exception ex)
                {
                    WriteToOutput($"Error during unlimited backup process!\n{ex.Message}", false, true, true);
                }
                finally
                {
                    isBusy = false;
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
            #region "Add fake device"

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

            #endregion

            textBlock_FilteredOutput.Text = string.Empty;

            RefreshForm();
        }

        private void button_ShowRawOutput_Click(object sender, RoutedEventArgs e)
        {
            TextBlockDialog textBlockDialog = new TextBlockDialog(ADB.RawOutput);
            textBlockDialog.Show();
        }

        private void button_StartScreenCopy_Click(object sender, RoutedEventArgs e)
        {
            if (!isBusy && Monitor.TryEnter(_syncMonitor))
            {
                isBusy = true;

                try
                {
                    Device selectedScrcpyDevice = comboBox_ScrcpyDevice.SelectedItem as Device;
                    string scrcpyArchive = $"{ProgramFolders.CurrentDirectory}\\Resources\\Scrcpy_v2.7.zip";

                    bool canStart = true;

                    // If platform tools doesn't exist and archive yes then extract it
                    if (!Directory.Exists(ProgramFolders.ScrcpyDirectory))
                    {
                        if (File.Exists(scrcpyArchive))
                        {
                            Directory.CreateDirectory(ProgramFolders.ScrcpyDirectory);

                            // Unzip platform tools archive into platform tools folders
                            if (!UnzipArchive(scrcpyArchive, ProgramFolders.ScrcpyDirectory))
                            {
                                canStart = Utils.UnzipArchive($"{ProgramFolders.CurrentDirectory}\\Resources\\Scrcpy_v2.7.zip", ProgramFolders.ScrcpyDirectory);
                            }
                        }
                        else
                        {
                            WriteToOutput("Scrcpy archive not found!", false, true, true);
                        }
                    }

                    if (canStart)
                    {
                        Process cmdProcess = new Process();
                        cmdProcess.StartInfo.FileName = "cmd.exe";
                        cmdProcess.StartInfo.UseShellExecute = false;
                        cmdProcess.StartInfo.RedirectStandardInput = true;
                        cmdProcess.StartInfo.RedirectStandardOutput = true;
                        cmdProcess.StartInfo.CreateNoWindow = false;
                        cmdProcess.StartInfo.WorkingDirectory = ProgramFolders.ScrcpyDirectory;
                        cmdProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                        cmdProcess.Start();

                        using (var writer = cmdProcess.StandardInput)
                        {
                            if (writer.BaseStream.CanWrite)
                            {
                                writer.WriteLine($"scrcpy -s {selectedScrcpyDevice.ID}");
                            }
                        }

                        cmdProcess.WaitForExit();
                    }
                    else
                    {
                        WriteToOutput("Can't start Scrcpy", false, true, true);
                    }
                }
                catch (Exception ex)
                {
                    WriteToOutput($"Error starting Scrcpy!\n{ex.Message}", false, true, true);
                }
                finally
                {
                    isBusy = false;
                    Monitor.Exit(_syncMonitor);
                }
            }
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                           PRIVATE UTILITIES                        //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        private async Task<int> RestoringProcess(string selectedFolderPath, string destinationDeviceIdentifier, string destinationDeviceFolder)
        {
            void CopyDirectory(string sourceDir, string destinationDir)
            {
                Directory.CreateDirectory(destinationDir);

                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destinationDir, fileName);
                    File.Copy(file, destFile, true);
                }

                foreach (string subDir in Directory.GetDirectories(sourceDir))
                {
                    string dirName = Path.GetFileName(subDir);
                    string destSubDir = Path.Combine(destinationDir, dirName);
                    CopyDirectory(subDir, destSubDir);
                }
            }

            // If it's a restoring operation, a trick is needed:
            // Copy selected folder to a new one with name 0, so it can be pushed to device storage folder
            string renamedSelectedFolderPath;

            #region "Get new name"

            List<string> splittedSelectedFolderPath = selectedFolderPath.Split('\\').ToList();

            // Replace last part with 0
            string lastPart = splittedSelectedFolderPath.Last();
            splittedSelectedFolderPath.Remove(lastPart);
            splittedSelectedFolderPath.Add("0");

            // Build new folder path
            renamedSelectedFolderPath = string.Join("\\", splittedSelectedFolderPath);

            #endregion

            CopyDirectory(selectedFolderPath, renamedSelectedFolderPath);

            int restoredFilesCount = await ADB.ExecutePushCommand(destinationDeviceIdentifier, destinationDeviceFolder, renamedSelectedFolderPath);

            // Delete copied folder
            try
            {
                Directory.Delete(renamedSelectedFolderPath, true);
            }
            catch (Exception)
            {
                WriteToOutput(string.Empty, false, false, false);
                WriteToOutput($"Error deleting \"{renamedSelectedFolderPath}\" folder!\nDon't worry it's a temporary folder, please delete it manually.", false, true, false);
            }

            return restoredFilesCount;
        }

        private async Task UninstallApp(Device selectedDevice, string selectedItem)
        {
            if (!isBusy && Monitor.TryEnter(_syncMonitor))
            {
                isBusy = true;

                try
                {
                    WriteToOutput($"Uninstalling {selectedItem} on {selectedDevice.Name}...", false, true, false);
                    WriteToOutput($"Sometimes this process requires a lot of time and application freezes.\n" +
                        $"If so, please check if app is successfully uninstalled then restart program!", false, true, false);
                    WriteToOutput(await ADB.UninstallApp(selectedDevice.ID, selectedItem) ? "App successfully uninstalled." : "Error while uninstalling app!", false, true, true);
                }
                catch (Exception ex)
                {
                    WriteToOutput(ex.Message, false, true, true);
                }
                finally
                {
                    // Trigger functions to refresh device packages
                    comboBox_UninstallAppsDevice_SelectionChanged(null, null);

                    isBusy = false;
                    Monitor.Exit(_syncMonitor);
                }
            }
        }
    }
}
