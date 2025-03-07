using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using PlatformTools;

namespace DeviceManager
{
    /// <summary>
    /// Logica di interazione per Homepage.xaml
    /// </summary>
    public partial class Homepage : Window
    {
        #region "Private variables"

        private List<Device> _scannedDevices = new List<Device>();

        #endregion

        #region "Properties"

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
            _scannedDevices = await ADB.ScanDevices();
            RefreshScannedDevicesControls();
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

        #region "Functions"

        private void RefreshScannedDevicesControls()
        {
            // Clear scanned devices listbox
            lb_ScannedDevices.Items.Clear();

            // Clear active device combobox
            cb_ActiveDevice.Items.Clear();

            // For each scanned device add it to listbox and active device combobox
            foreach (Device scannedDevice in _scannedDevices)
            {
                lb_ScannedDevices.Items.Add(scannedDevice);
                cb_ActiveDevice.Items.Add(scannedDevice);
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
