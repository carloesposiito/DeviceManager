namespace GoogleBackupManager.Model
{
    internal class Device
    {
        #region "Properties"

        private string _name;
        private string _id;
        private bool _authorized;
        private bool _hasUnlimitedBackup;
        private bool _isWirelessConnected;

        private string alarmsFolderPath = "/storage/emulated/0/Alarms/";
        private string dcimFolderPath = "/storage/emulated/0/DCIM/";
        private string documentsFolderPath = "/storage/emulated/0/Documents/";
        private string downloadsFolderPath = "/storage/emulated/0/Download/";
        private string musicFolderPath = "/storage/emulated/0/Music/";
        private string picturesFolderPath = "/storage/emulated/0/Pictures/";
        private string ringtonesFolderPath = "/storage/emulated/0/Ringtones/";
        private string deviceFolderPath = "/storage/emulated/";
        private string whatsAppBackupsFolderPath = "/storage/emulated/0/Android/media/com.whatsapp/WhatsApp/Backups";
        private string whatsAppDatabasesFolderPath = "/storage/emulated/0/Android/media/com.whatsapp/WhatsApp/Databases";
        private string whatsAppMediaFolderPath = "/storage/emulated/0/Android/media/com.whatsapp/WhatsApp/Media";
        private string whatsAppFolderPath = "/storage/emulated/0/Android/media/com.whatsapp/WhatsApp";

        #endregion

        #region "Getters and setters"

        public string Name { get => _name; set => _name = value; }
        public string ID { get => _id; set => _id = value; }
        public bool IsAuthorized { get => _authorized; set => _authorized = value; }
        public bool HasUnlimitedBackup { get => _hasUnlimitedBackup; set => _hasUnlimitedBackup = value; }
        public bool IsWirelessConnected { get => _isWirelessConnected; set => _isWirelessConnected = value; }
        public string AlarmsFolderPath { get => alarmsFolderPath; set => alarmsFolderPath = value; }
        public string DcimFolderPath { get => dcimFolderPath; set => dcimFolderPath = value; }
        public string DocumentsFolderPath { get => documentsFolderPath; set => documentsFolderPath = value; }
        public string DownloadsFolderPath { get => downloadsFolderPath; set => downloadsFolderPath = value; }
        public string MusicFolderPath { get => musicFolderPath; set => musicFolderPath = value; }
        public string PicturesFolderPath { get => picturesFolderPath; set => picturesFolderPath = value; }
        public string RingtonesFolderPath { get => ringtonesFolderPath; set => ringtonesFolderPath = value; }
        public string DeviceFolderPath { get => deviceFolderPath; set => deviceFolderPath = value; }
        public string WhatsAppBackupsFolderPath { get => whatsAppBackupsFolderPath; set => whatsAppBackupsFolderPath = value; }
        public string WhatsAppDatabasesFolderPath { get => whatsAppDatabasesFolderPath; set => whatsAppDatabasesFolderPath = value; }
        public string WhatsAppMediaFolderPath { get => whatsAppMediaFolderPath; set => whatsAppMediaFolderPath = value; }
        public string WhatsAppFolderPath { get => whatsAppFolderPath; set => whatsAppFolderPath = value; }

        #endregion

        public Device() { }

        public Device(string name, string id, bool authStatus, bool hasUnlimitedBackup, bool isWirelessConnected)
        {
            _name = name;
            _id = id;
            _authorized = authStatus;
            _hasUnlimitedBackup = hasUnlimitedBackup;
            _isWirelessConnected = isWirelessConnected;
        }

        public override string ToString()
        {
            string toSt =
                $"[{_name}]\n" +
                $"ID = {_id}\n" +
                $"Status = {(_authorized ? "Authorized" : "Not authorized")}\n" +
                $"Backup = {(_hasUnlimitedBackup ? "Unlimited" : "Limited")}\n" +
                $"Connection mode = {(_isWirelessConnected ? "Wireless" : "USB")}";

            return toSt;
        }
    }
}
