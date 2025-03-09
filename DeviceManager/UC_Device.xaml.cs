using PlatformTools;
using System.Windows.Controls;

namespace DeviceManager
{
    public partial class UC_Device : UserControl
    {
        #region "Private variables"

        private Device _device;

        #endregion

        #region "Properties"

        /// <summary>
        /// Device object.
        /// </summary>
        public Device Device { get => _device; }

        #endregion

        public UC_Device(Device device)
        {
            InitializeComponent();
            this.DataContext = this;
            _device = device;
        }
    }
}
