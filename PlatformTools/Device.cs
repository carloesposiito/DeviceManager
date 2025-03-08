using System;
using System.Collections.Generic;
using System.Linq;

namespace PlatformTools
{
    /// <summary>
    /// Structure that holds details about a scanned device.
    /// </summary>
    public class Device
    {
        #region "Private variables"

        private string _id;
        private string _model;
        private Enums.DeviceAuthStatus _authStatus;
        private bool _wirelessConnected;

        #endregion

        #region "Properties"

        /// <summary>
        /// Device ID.
        /// </summary>
        public string Id { get => _id; }

        /// <summary>
        /// Device ID for description.
        /// </summary>
        public string IdDescription { get => $"[{_id}]"; }

        /// <summary>
        /// Device name.
        /// </summary>
        public string Model { get => _model; set => _model = value; }

        /// <summary>
        /// Device authorization status.
        /// </summary>
        public Enums.DeviceAuthStatus AuthStatus { get => _authStatus; }

        /// <summary>
        /// Describes if device is wireless connected.
        /// </summary>
        public bool WirelessConnected { get => _wirelessConnected; }

        #endregion

        /// <summary>
        /// Constructor of the class.
        /// </summary>
        // Permits to create device also from WPF, but only in debug mode
        internal Device(string scannedDeviceLine)
        {
            // Split device line into parts
            List<string> lineParts = scannedDeviceLine.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // Get device ID
            // At this point device name is equal to device ID
            _id = _model = lineParts[0].Trim();

            // Get device connection type (usb/wireless)
            _wirelessConnected = _id.Contains(":") || _id.Contains("adb");

            // Get device authorization status
            if (lineParts[1].Trim().Equals("device"))
            {
                _authStatus = Enums.DeviceAuthStatus.AUTHORIZED;
            }
            else if (lineParts[1].Trim().Equals("unauthorized"))
            {
                _authStatus = Enums.DeviceAuthStatus.UNAUTHORIZED;
            }
            else
            {
                _authStatus = Enums.DeviceAuthStatus.UNKNOWN;
            }
        }
    
    }
}
