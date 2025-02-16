using System;
using PlatformTools;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace DeviceManager
{
    public partial class Home : Window, INotifyPropertyChanged
    {
        #region "Private variables"

        private bool _isBusy = false;
        private int _activeDevice;
        private UIElementCollection UC_Devices;

        #endregion

        #region "Properties"

        /// <summary>
        /// Describes count of connected devices.
        /// </summary>
        public int DevicesCount
        {
            get
            {
                return stackPanel_Devices.Children.Count;
            }
        }
        
        /// <summary>
        /// Describes if an operation is in progress and program is busy.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Describes index of active device.
        /// </summary>
        public int ActiveDevice
        {
            get => _activeDevice;
            set
            {
                if (_activeDevice != value)
                {
                    _activeDevice = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        public Home()
        {
            InitializeComponent();
            DataContext = this;
            this.Loaded += Window_Loaded;
            UC_Devices = stackPanel_Devices.Children;
        }

        #region "Form events"

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!await ADB.Initialize())
            {
                MessageBox.Show("Error while initializing ADB!\nAborting program...");
                Process.GetCurrentProcess().Kill();
            }
            else
            {
                this.Visibility = Visibility.Visible;
            }
        }

        private async void menuItem_ScanDevices_Click(object sender, RoutedEventArgs e)
        {
            await ScanDevices();
        }

        private void menuItem_Devices_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickedMenuItem = sender as MenuItem;
            int clickedMenuItemIndex = menuItem_Devices.Items.IndexOf(clickedMenuItem);

            if (!clickedMenuItemIndex.Equals(ActiveDevice))
            {
                // Hide previous UC_Device user control
                // Uncheck previous device MenuItem
                UC_Devices[ActiveDevice].Visibility = Visibility.Collapsed;
                var previousMenuItem = menuItem_Devices.Items[ActiveDevice] as MenuItem;
                previousMenuItem.IsChecked = false;

                // Update active device number
                // Show current UC_Device user control
                // Check active device MenuItem
                ActiveDevice = clickedMenuItemIndex;
                UC_Devices[ActiveDevice].Visibility = Visibility.Visible;
                clickedMenuItem.IsChecked = true;
            }
        }

        private async void menuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            await ADB.KillServer();
            Process.GetCurrentProcess().Kill();
        }

        private void border_Upper_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        #endregion

        #region "Functions"

        private async Task ScanDevices()
        {
            if (!_isBusy)
            {
                try
                {
                    IsBusy = true;

                    // Update cursor status
                    // It's impossible to use it with binding
                    Cursor = System.Windows.Input.Cursors.Wait;

                    // Clear old devices from collection
                    // Also refresh devices count
                    // This is the only point where it's value changes
                    // So an ObservableColletion is avoided
                    UC_Devices.Clear();
                    menuItem_Devices.Items.Clear();
                    OnPropertyChanged(nameof(DevicesCount));

                    // Get connected devices
                    var foundDevices = await ADB.ScanDevices();
#if DEBUG
                    // If debug add some fake device
                    int fakeDevicesCount = 3;
                    for (int i = 1; i <= fakeDevicesCount; i++)
                    {
                        foundDevices.Add(new Device($"Device {foundDevices.Count + 1}\tdevice"));
                    }
#endif

                    // Add each found device to UI stackpanel
                    // And also add it into menu item
                    foreach (Device foundDevice in foundDevices)
                    {
                        UC_Devices.Add(new UC_Device(foundDevice));

                        var deviceMenuItem = new MenuItem();
                        deviceMenuItem.Header = foundDevice.Model;
                        deviceMenuItem.Click += menuItem_Devices_Click;
                        menuItem_Devices.Items.Add(deviceMenuItem);
                    }

                    // Refresh devices count
                    OnPropertyChanged(nameof(DevicesCount));

                    if (DevicesCount > 0)
                    {
                        ActiveDevice = 0;
                        UC_Devices[ActiveDevice].Visibility = Visibility.Visible;
                        MenuItem activeMenuItem = menuItem_Devices.Items[ActiveDevice] as MenuItem;
                        activeMenuItem.IsChecked = true;
                    }

                    // Restore cursor
                    Cursor = System.Windows.Input.Cursors.Arrow;
                }
                catch (Exception ex)
                {
                    UC_Devices.Clear();
                    MessageBox.Show($"Error while scanning devices!\n{ex.Message}");
                }
                finally
                {
                    IsBusy = false;
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
