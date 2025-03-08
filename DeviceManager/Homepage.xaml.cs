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
        #region "Constants"

        private const string TITLE = "DeviceManager";

        #endregion

        #region "Private variables"

        private bool _isFree = true;
        private ObservableCollection<Device> _connectedDevices = new ObservableCollection<Device>();
        private int _devicesCount = 0;
        private Device _activeDevice = null;
        

        #endregion

        #region "Properties"

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

                    // Refresh conncted devices count
                    DevicesCount = _connectedDevices.Count;
                }
            }
        }

        public int DevicesCount
        {
            get => _devicesCount;
            private set
            {
                if (_devicesCount != value)
                {
                    _devicesCount = value;
                    OnPropertyChanged(nameof(DevicesCount));
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

        #endregion

        public Homepage()
        {
            InitializeComponent();
            DataContext = this;
            this.Loaded += Homepage_LoadingCompleted;
        }

        private async void Homepage_LoadingCompleted(object sender, RoutedEventArgs e)
        {
            await ADB.Initialize();
        }

        private async void Homepage_Closed(object sender, System.EventArgs e)
        {
            await ADB.KillServer();
        }

        private async void btn_ScanDevices_Click(object sender, RoutedEventArgs e)
        {
            await ScanDevices();
        }       

        private void checkBox_PairingNeeded_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)checkBox_PairingNeeded.IsChecked)
            {
                lbl_DevicePairingCode.Visibility = tb_DevicePairingCode.Visibility = Visibility.Visible;
            }
            else
            {
                lbl_DevicePairingCode.Visibility = tb_DevicePairingCode.Visibility = Visibility.Collapsed;
                tb_DevicePairingCode.Text = string.Empty;
            }
        }

        private async void btn_ConnectWirelessDevice_Click(object sender, RoutedEventArgs e)
        {
            if (IsFree)
            {
                try
                {
                    IsFree = false;
                    bool connectionResult;

                    if ((bool)checkBox_PairingNeeded.IsChecked)
                    {
                        connectionResult = await ADB.PairWirelessDevice(tb_DeviceIpAddress.Text, tb_DevicePort.Text, tb_DevicePairingCode.Text);
                    }
                    else
                    {
                        connectionResult = await ADB.ConnectWirelessDevice(tb_DeviceIpAddress.Text, tb_DevicePort.Text);
                    }

                    // Inform user of connection result
                    MessageBox.Show(connectionResult ? "Connection succedeed! Please scan devices again." : "Connection failed! Please try again!");
                }
                catch (Exception exception)
                {
                    ConnectedDevices = new ObservableCollection<Device>();

                    MessageBox.Show(
                        $"[btn_ConnectWirelessDevice_Click]\n" +
                        $"{exception.Message}\n"
                        );
                }
                finally
                {
                    IsFree = true;
                }
            }            
        }

        private void lb_ConnectedDevices_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
#if DEBUG
            this.Title = TITLE;
#endif

            ListBox senderListBox = sender as ListBox;
            Device selectedDevice = senderListBox.SelectedValue as Device;

            if (selectedDevice != null)
            {
                ActiveDevice = selectedDevice;
#if DEBUG
                this.Title = $"{Title} [{ActiveDevice.Model}]";
#endif
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

                    // First clear connected devices list
                    // Then scan again
                    ConnectedDevices = new ObservableCollection<Device>();
                    ConnectedDevices = new ObservableCollection<Device>(await ADB.ScanDevices());
                }
                catch (Exception exception)
                {
                    ConnectedDevices = new ObservableCollection<Device>();

                    MessageBox.Show(
                        $"[ScanDevices]\n" +
                        $"{exception.Message}\n"
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
