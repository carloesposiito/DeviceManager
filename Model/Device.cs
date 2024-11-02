using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private string documentFolderPath = "/storage/emulated/0/Documents/";

        #endregion

        #region "Getters and setters"

        public string Name { get => _name; set => _name = value; }
        public string ID { get => _id; set => _id = value; }
        public bool IsAuthorized { get => _authorized; set => _authorized = value; }
        public bool HasUnlimitedBackup { get => _hasUnlimitedBackup; set => _hasUnlimitedBackup = value; }
        public bool IsWirelessConnected { get => _isWirelessConnected; set => _isWirelessConnected = value; }
        public string DocumentFolder { get => documentFolderPath; set => documentFolderPath = value; }



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
