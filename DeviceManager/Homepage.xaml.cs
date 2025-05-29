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
        private Device _activeDevice;
        private bool _pairing = false;

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
            if (IsFree)
            {
                try
                {
                    IsFree = false;

                    bool operationResult = Pairing ? await _adb.PairWirelessDevice(tb_DeviceIpAddress.Text, tb_DevicePort.Text, tb_DevicePairingCode.Text) : await _adb.ConnectWirelessDevice(tb_DeviceIpAddress.Text, tb_DevicePort.Text);
                    if (operationResult)
                    {
                        await ScanDevices();
                    }
                    else
                    {
                        MessageBox.Show("Wireless connection failed! Please try again.");
                    }
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
                    OnPropertyChanged(nameof(ActiveDevice));
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
