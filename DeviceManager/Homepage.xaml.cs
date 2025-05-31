using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PlatformTools;

namespace DeviceManager
{
    public partial class Homepage : Window, INotifyPropertyChanged
    {
        #region "Private variables"

        private ADB _adb;
        private List<string> _rawOutput = new List<string>();
        private List<string> _output = new List<string>();
        private bool _programInitialized = false;
        private bool _isFree = true;
        private ObservableCollection<Device> _connectedDevices = new ObservableCollection<Device>();
        private Device _activeDevice = null;
        private bool _pairing = false;
        private string _destinationFolder = string.Empty;
        private string _backupFolder = string.Empty;
        private ObservableCollection<string> _allApps = new ObservableCollection<string>();
        private ObservableCollection<string> _systemApps = new ObservableCollection<string>();
        private ObservableCollection<string> _thirdyPartApps = new ObservableCollection<string>();

        #endregion

        #region "Properties"

        public List<string> RawOutput
        {
            get => _rawOutput;
            set
            {
                _rawOutput = value;
                OnPropertyChanged(nameof(RawOutput));
            }
        }

        public List<string> Output
        {
            get => _output;
            set
            {
                _output = value;
                OnPropertyChanged(nameof(Output));
            }
        }

        public bool ProgramInitialized
        {
            get => _programInitialized;
            set
            {
                if (_programInitialized != value)
                {
                    _programInitialized = value;
                    OnPropertyChanged(nameof(ProgramInitialized));
                }
            }
        }

        public bool IsFree
        {
            get => _isFree;
            set
            {
                if (_isFree != value)
                {
                    _isFree = value;
                    OnPropertyChanged(nameof(IsFree));
                }
            }
        }

        public ObservableCollection<Device> ConnectedDevices
        {
            get => _connectedDevices;
            set
            {
                if (_connectedDevices != value)
                {
                    _connectedDevices = value;
                    OnPropertyChanged(nameof(ConnectedDevices));
                }
            }
        }

        public Device ActiveDevice
        {
            get => _activeDevice;
            set
            {
                if (_activeDevice != value)
                {
                    _activeDevice = value;
                    OnPropertyChanged(nameof(ActiveDevice));
                }
            }
        }

        public bool Pairing
        {
            get => _pairing;
            set
            {
                if (_pairing != value)
                {
                    _pairing = value;
                    OnPropertyChanged(nameof(Pairing));
                }
            }
        }

        public string DestinationFolder
        {
            get => _destinationFolder;
            set
            {
                _destinationFolder = value;
                OnPropertyChanged(nameof(DestinationFolder));
            }
        }

        public string BackupFolder
        {
            get => _backupFolder;
            set
            {
                _backupFolder = value;
                OnPropertyChanged(nameof(BackupFolder));
            }
        }

        public ObservableCollection<string> AllApps
        {
            get => _allApps;
            set
            {
                if (_allApps != value)
                {
                    _allApps = value;
                    OnPropertyChanged(nameof(AllApps));
                }
            }
        }

        public ObservableCollection<string> ThirdyPartApps
        {
            get => _thirdyPartApps;
            set
            {
                if (_thirdyPartApps != value)
                {
                    _thirdyPartApps = value;
                    OnPropertyChanged(nameof(ThirdyPartApps));
                }
            }
        }

        public ObservableCollection<string> SystemApps
        {
            get => _systemApps;
            set
            {
                if (_systemApps != value)
                {
                    _systemApps = value;
                    OnPropertyChanged(nameof(SystemApps));
                }
            }
        }

        #endregion

        public Homepage()
        {
            InitializeComponent();
            DataContext = this;

            // Initialize adb object
            _adb = new ADB(ref _rawOutput, ref _output);

            // Add event to loading completed
            this.Loaded += Homepage_LoadingCompleted;
        }

        private async void Homepage_LoadingCompleted(object sender, RoutedEventArgs e)
        {
            ProgramInitialized = await _adb.Initialize();
        }

        private async void Homepage_Closed(object sender, System.EventArgs e)
        {
            await _adb.KillServer();
        }

        private async void btn_ScanDevices_Click(object sender, RoutedEventArgs e)
        {
            await ScanDevices();
        }

        private void checkBox_PairingNeeded_Click(object sender, RoutedEventArgs e)
        {
            Pairing = (bool)checkBox_PairingNeeded.IsChecked;
        }

        private async void btn_ConnectWirelessDevice_Click(object sender, RoutedEventArgs e)
        {
            bool operationResult = false;

            if (IsFree)
            {
                try
                {
                    IsFree = false;

                    operationResult = Pairing ? await _adb.PairWirelessDevice(tb_DeviceIpAddress.Text, tb_DevicePort.Text, tb_DevicePairingCode.Text) : await _adb.ConnectWirelessDevice(tb_DeviceIpAddress.Text, tb_DevicePort.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show
                    (
                        $"Error connecting wireless device! Error details:\n\n" +
                        $"{ex.Message}"
                    );
                }
                finally
                {
                    IsFree = true;

                    // Clean all textboxes and deselect checkbox
                    foreach (var item in grid_WirelessConnection.Children)
                    {
                        if (item is CheckBox checkBox)
                        {
                            checkBox.IsChecked = false;
                        }
                        else if (item is TextBox textBox)
                        {
                            textBox.Text = string.Empty;
                        }
                    }

                    if (operationResult)
                    {
                        await ScanDevices();
                        MessageBox.Show("Wireless connection succeed!");
                    }
                    else
                    {
                        MessageBox.Show("Wireless connection failed! Please try again.");
                    }
                }
            }
        }

        private void cb_ConnectedDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                Device selectedDevice = comboBox.SelectedItem as Device;
                if (selectedDevice != null)
                {
                    ActiveDevice = selectedDevice;
                }
            }
        }

        private async void btn_AuthorizeDevice_Click(object sender, RoutedEventArgs e)
        {
            if (IsFree)
            {
                try
                {
                    IsFree = false;

#if DEBUG
                    ActiveDevice.AuthStatus = Enums.DeviceAuthStatus.AUTHORIZED;
#else
                    ActiveDevice.AuthStatus = await _adb.AuthorizeDevice(ActiveDevice.Id) ? Enums.DeviceAuthStatus.AUTHORIZED : Enums.DeviceAuthStatus.UNAUTHORIZED;
#endif
                    // Force refresh
                    OnPropertyChanged(nameof(ActiveDevice));
                    OnPropertyChanged(nameof(ConnectedDevices));
                }
                catch (Exception ex)
                {
                    MessageBox.Show
                    (
                        $"Error authorizing wireless device! Error details:\n\n" +
                        $"{ex.Message}"
                    );
                }
                finally
                {
                    IsFree = true;
                }
            }
        }

        private async void btn_TransferFiles_Click(object sender, RoutedEventArgs e)
        {
            if (IsFree)
            {
                try
                {
                    IsFree = false;

                    Tuple<int, int, int> operationResult = new Tuple<int, int, int>(0, 0, 0);
                    operationResult = await _adb.TransferFolder(ActiveDevice);

                    if (!operationResult.Item1.Equals(0) && !operationResult.Item2.Equals(0))
                    {
                        // Everything OK
                        MessageBox.Show(
                            $"Transferred {operationResult.Item2}//{operationResult.Item1} files.\n" +
                            $"{operationResult.Item3} files skipped.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show
                    (
                        $"Error transferring files! Error details:\n\n" +
                        $"{ex.Message}"
                    );
                }
                finally
                {
                    IsFree = true;
                }
            }
        }

        private void btn_SelectDestinationFolder_Click(object sender, RoutedEventArgs e)
        {
            Utilities utilities = new Utilities();
            DestinationFolder = utilities.BrowseFolder();
        }

        private void btn_SelectBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            Utilities utilities = new Utilities();
            BackupFolder = utilities.BrowseFolder();
        }

        private async void btn_PerformBackup_Click(object sender, RoutedEventArgs e)
        {
            if (IsFree)
            {
                try
                {
                    IsFree = false;

                    #region "Populate folder to be extracted list according to selected checkboxes"

                    List<string> foldersToBeExtracted = new List<string>();

                    if ((bool)checkBox_Everything.IsChecked)
                    {
                        foldersToBeExtracted.Add(ActiveDevice.DeviceFolderPath);
                    }
                    else
                    {
                        if ((bool)checkBox_WhatsAppAll.IsChecked)
                        {
                            foldersToBeExtracted.Add(ActiveDevice.WhatsAppFolderPath);
                        }
                        else
                        {
                            if ((bool)checkBox_WhatsApp_Backups.IsChecked)
                            {
                                foldersToBeExtracted.Add(ActiveDevice.WhatsAppBackupsFolderPath);
                            }

                            if ((bool)checkBox_WhatsApp_Database.IsChecked)
                            {
                                foldersToBeExtracted.Add(ActiveDevice.WhatsAppDatabasesFolderPath);
                            }

                            if ((bool)checkBox_WhatsApp_Media.IsChecked)
                            {
                                foldersToBeExtracted.Add(ActiveDevice.WhatsAppMediaFolderPath);
                            }
                        }

                        if ((bool)checkBox_Alarms.IsChecked)
                        {
                            foldersToBeExtracted.Add(ActiveDevice.AlarmsFolderPath);
                        }

                        if ((bool)checkBox_DCIM.IsChecked)
                        {
                            foldersToBeExtracted.Add(ActiveDevice.DcimFolderPath);
                        }

                        if ((bool)checkBox_Documents.IsChecked)
                        {
                            foldersToBeExtracted.Add(ActiveDevice.DocumentsFolderPath);
                        }

                        if ((bool)checkBox_Downloads.IsChecked)
                        {
                            foldersToBeExtracted.Add(ActiveDevice.DownloadsFolderPath);
                        }

                        if ((bool)checkBox_Music.IsChecked)
                        {
                            foldersToBeExtracted.Add(ActiveDevice.MusicFolderPath);
                        }

                        if ((bool)checkBox_Pictures.IsChecked)
                        {
                            foldersToBeExtracted.Add(ActiveDevice.PicturesFolderPath);
                        }

                        if ((bool)checkBox_Ringtones.IsChecked)
                        {
                            foldersToBeExtracted.Add(ActiveDevice.RingtonesFolderPath);
                        }
                    }

                    #endregion

                    var operationResult = await _adb.BackupFolders(ActiveDevice, foldersToBeExtracted, DestinationFolder);
                    if (!operationResult.Item1.Equals(0))
                    {
                        string destinationFolder = string.IsNullOrWhiteSpace(DestinationFolder) ? Constants.PATHS.BACKUP_DIR : DestinationFolder;

                        // Everything OK
                        MessageBox.Show(
                            $"Pulled {operationResult.Item1} files.\n" +
                            $"{operationResult.Item2} files skipped.\n\n" +
                            $"Destination folder:\n" +
                            $"\"{destinationFolder}\"");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show
                    (
                        $"Error while performing backup! Error details:\n\n" +
                        $"{ex.Message}"
                    );
                }
                finally
                {
                    IsFree = true;

                    // De-check all checkboxes
                    foreach (var item in grid_FolderCheckboxes.Children)
                    {
                        if (item is CheckBox checkbox)
                        {
                            checkbox.IsChecked = false;
                        }
                    }
                }
            }
        }

        private async void btn_RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            if (IsFree)
            {
                try
                {
                    IsFree = false;

                    var operationResult = await _adb.RestoreBackup(ActiveDevice, BackupFolder);
                    if (!operationResult.Item1.Equals(0) && !operationResult.Item2.Equals(0))
                    {
                        // Everything OK
                        MessageBox.Show(
                            $"Transferred {operationResult.Item2}//{operationResult.Item1} files.\n" +
                            $"{operationResult.Item3} files skipped.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show
                    (
                        $"Error while restoring a backup! Error details:\n\n" +
                        $"{ex.Message}"
                    );
                }
                finally
                {
                    IsFree = true;

                    // Reset field in textbox
                    BackupFolder = string.Empty;
                }
            }
        }

        private async void btn_RefreshApps_Click(object sender, RoutedEventArgs e)
        {
            if (IsFree)
            {
                try
                {
                    IsFree = false;

                    AllApps = new ObservableCollection<string>();
                    ThirdyPartApps = new ObservableCollection<string>();
                    SystemApps = new ObservableCollection<string>();

                    var operationResult = await _adb.GetApplications(ActiveDevice.Id);
                    if (operationResult.Item1.Count > 0 && operationResult.Item2.Count > 0 && operationResult.Item3.Count > 0)
                    {
                        AllApps = new ObservableCollection<string>(operationResult.Item1);
                        cb_AllApps.SelectedIndex = 0;

                        SystemApps = new ObservableCollection<string>(operationResult.Item2);
                        cb_SystemApps.SelectedIndex = 0;

                        ThirdyPartApps = new ObservableCollection<string>(operationResult.Item2);
                        cb_ThirdyPartApps.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show
                    (
                        $"Error while refreshing device apps! Error details:\n\n" +
                        $"{ex.Message}"
                    );
                }
                finally
                {
                    IsFree = true;
                }
            }
        }

        private async void btn_DisableApp_Sys_Click(object sender, RoutedEventArgs e)
        {
            string packageName = cb_SystemApps.SelectedItem as string;
            await DisableApp(packageName);
        }

        private async void btn_DisableApp_Third_Click(object sender, RoutedEventArgs e)
        {
            string packageName = cb_ThirdyPartApps.SelectedItem as string;
            await DisableApp(packageName);
        }

        private async void btn_DisableApp_All_Click(object sender, RoutedEventArgs e)
        {
            string packageName = cb_AllApps.SelectedItem as string;
            await DisableApp(packageName);
        }

        #region "Functions"

        private async Task ScanDevices()
        {
            if (IsFree)
            {
                try
                {
                    IsFree = false;
                    ActiveDevice = null;
                    ConnectedDevices = new ObservableCollection<Device>();

                    // Refresh
                    ConnectedDevices = new ObservableCollection<Device>(await _adb.ScanDevices());

                    // Select first element of the list
                    cb_ConnectedDevices.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show
                    (
                        $"Error scanning devices! Error details:\n\n" +
                        $"{ex.Message}"
                    );
                }
                finally
                {
                    IsFree = true;
                }
            }
        }

        private async Task DisableApp(string packageName)
        {
            bool uninstallResult = false;

            if (IsFree)
            {
                try
                {
                    IsFree = false;

                    if (packageName != null && !string.IsNullOrWhiteSpace(packageName))
                    {
                        uninstallResult = await _adb.UninstallApp(ActiveDevice.Id, packageName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show
                    (
                        $"Error while uninstalling device app! Error details:\n\n" +
                        $"{ex.Message}"
                    );
                }
                finally
                {
                    IsFree = true;

                    if (uninstallResult)
                    {
                        MessageBox.Show("Package removed succesfully!");

                        // Refresh app list
                        btn_RefreshApps_Click(null, null);
                    }
                }
            }
        }

        #endregion

        #region "Binding"

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}
