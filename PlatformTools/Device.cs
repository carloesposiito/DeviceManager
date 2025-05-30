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

        #region "Folders"

        private string _deviceFolderPath = "/storage/emulated/";
        private string _alarmsFolderPath = $"/storage/emulated/0/Alarms/";
        private string _dcimFolderPath = "/storage/emulated/0/DCIM/";
        private string _cameraFolderPath = "/storage/emulated/0/DCIM/Camera/";
        private string _documentsFolderPath = "/storage/emulated/0/Documents/";
        private string _downloadsFolderPath = "/storage/emulated/0/Download/";
        private string _musicFolderPath = "/storage/emulated/0/Music/";
        private string _picturesFolderPath = "/storage/emulated/0/Pictures/";
        private string _ringtonesFolderPath = "/storage/emulated/0/Ringtones/";
        private string _whatsAppBackupsFolderPath = "/storage/emulated/0/Android/media/com.whatsapp/WhatsApp/Backups";
        private string _whatsAppDatabasesFolderPath = "/storage/emulated/0/Android/media/com.whatsapp/WhatsApp/Databases";
        private string _whatsAppMediaFolderPath = "/storage/emulated/0/Android/media/com.whatsapp/WhatsApp/Media";
        private string _whatsAppFolderPath = "/storage/emulated/0/Android/media/com.whatsapp/WhatsApp";

        #endregion

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
        public Enums.DeviceAuthStatus AuthStatus { get => _authStatus; set => _authStatus = value; }

        /// <summary>
        /// Describes if device is wireless connected.
        /// </summary>
        public bool WirelessConnected { get => _wirelessConnected; }

        #region "Folders"

        public string DeviceFolderPath { get => _deviceFolderPath; set => _deviceFolderPath = value; }
        public string AlarmsFolderPath { get => _alarmsFolderPath; set => _alarmsFolderPath = value; }
        public string DcimFolderPath { get => _dcimFolderPath; set => _dcimFolderPath = value; }
        public string CameraFolderPath { get => _cameraFolderPath; set => _cameraFolderPath = value; }
        public string DocumentsFolderPath { get => _documentsFolderPath; set => _documentsFolderPath = value; }
        public string DownloadsFolderPath { get => _downloadsFolderPath; set => _downloadsFolderPath = value; }
        public string MusicFolderPath { get => _musicFolderPath; set => _musicFolderPath = value; }
        public string PicturesFolderPath { get => _picturesFolderPath; set => _picturesFolderPath = value; }
        public string RingtonesFolderPath { get => _ringtonesFolderPath; set => _ringtonesFolderPath = value; }
        public string WhatsAppBackupsFolderPath { get => _whatsAppBackupsFolderPath; set => _whatsAppBackupsFolderPath = value; }
        public string WhatsAppDatabasesFolderPath { get => _whatsAppDatabasesFolderPath; set => _whatsAppDatabasesFolderPath = value; }
        public string WhatsAppMediaFolderPath { get => _whatsAppMediaFolderPath; set => _whatsAppMediaFolderPath = value; }
        public string WhatsAppFolderPath { get => _whatsAppFolderPath; set => _whatsAppFolderPath = value; }

        #endregion
        
        #endregion

        /// <summary>
        /// Constructor of the class.
        /// </summary>
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
            else
            {
                _authStatus = Enums.DeviceAuthStatus.UNAUTHORIZED;
            }
        }
    
    }
}
