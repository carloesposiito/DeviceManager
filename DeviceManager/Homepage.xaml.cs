using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using PlatformTools;

namespace DeviceManager
{
    /// <summary>
    /// Logica di interazione per Homepage.xaml
    /// </summary>
    public partial class Homepage : Window
    {
        #region "Constants"

        private const string TITLE = "DeviceManager";

        #endregion

        #region "Private variables"

        private List<Device> _connectedDevices = new List<Device>();
        private Device _activeDevice = null;

        #endregion

        #region "Properties"

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
            ResetControls();


            _connectedDevices = await ADB.ScanDevices();


            RefreshConnectedDevicesControls();
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
            bool connectionResult;

            if ((bool)checkBox_PairingNeeded.IsChecked)
            {
                connectionResult = await ADB.PairWirelessDevice(tb_DeviceIpAddress.Text, tb_DevicePort.Text, tb_DevicePairingCode.Text);
            }
            else
            {
                connectionResult = await ADB.ConnectWirelessDevice(tb_DeviceIpAddress.Text, tb_DevicePort.Text);
            }
        }

        private void lb_ConnectedDevices_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            this.Title = TITLE;

            ListBox senderListBox = sender as ListBox;
            Device selectedDevice = senderListBox.SelectedValue as Device;

            if (selectedDevice != null)
            {
                ActiveDevice = selectedDevice;
                this.Title = $"{Title} [{ActiveDevice.Model}]";
            }
        }

        #region "Functions"

        private void ResetControls()
        {
            // Hide connected devices
            grid_ConnectedDevices.Visibility = Visibility.Collapsed;

            // Clear scanned devices listbox
            lb_ConnectedDevices.Items.Clear();
        }

        /// <summary>
        /// Refresh controls realted to connected devices
        /// </summary>
        private void RefreshConnectedDevicesControls()
        {
            foreach (Device connectedDevice in _connectedDevices)
            {
                lb_ConnectedDevices.Items.Add(connectedDevice);
            }

            if (_connectedDevices.Count > 0)
            {
                grid_ConnectedDevices.Visibility = Visibility.Visible;

                // If only one device is connected, select it automatically
                //if (_connectedDevices.Count.Equals(1))
                //{
                //    lb_ConnectedDevices.SelectedIndex = 0;
                //}
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
