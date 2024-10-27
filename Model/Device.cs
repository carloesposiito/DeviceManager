using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleBackupManager.Model
{
    internal class Device
    {
        private string _name = string.Empty;
        private string _id = string.Empty;
        private bool _authorized;
        private bool _hasUnlimitedBackup = false;

        public Device(string name, string id, bool authStatus, bool hasUnlimitedBackup)
        {
            _name = name;
            _id = id;
            _authorized = authStatus;
            _hasUnlimitedBackup = hasUnlimitedBackup;
        }

        public string Name { get => _name; set => _name = value; }
        public string ID { get => _id; set => _id = value; }
        public bool IsAuthorized { get => _authorized; set => _authorized = value; }
        public bool HasUnlimitedBackup { get => _hasUnlimitedBackup; set => _hasUnlimitedBackup = value; }
    }
}
