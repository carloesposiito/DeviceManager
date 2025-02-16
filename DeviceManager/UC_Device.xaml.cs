using PlatformTools;
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DeviceManager
{
    public partial class UC_Device : UserControl
    {
        #region "Private variables"

        private Device _device;

        #endregion

        #region "Properties"

        /// <summary>
        /// Device ID.
        /// </summary>
        public string Id { get => _device.Id; }

        /// <summary>
        /// Device name.
        /// </summary>
        public string IdDescription { get => _device.IdDescription; }

        /// <summary>
        /// Device name.
        /// </summary>
        public string Model { get => _device.Model; }

        /// <summary>
        /// Device authorization status.
        /// </summary>
        public Enums.DeviceAuthStatus AuthStatus { get => _device.AuthStatus; }

        /// <summary>
        /// Describes if device is wireless connected.
        /// </summary>
        public bool WirelessConnected { get => _device.WirelessConnected; }

        #endregion

        public UC_Device(Device device)
        {
            InitializeComponent();
            this.DataContext = this;
            _device = device;
        }
    }
}
