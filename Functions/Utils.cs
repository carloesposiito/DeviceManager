using GoogleBackupManager.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleBackupManager.Functions
{
    internal static class Utils
    {
        /// <summary>
        /// Show a dialog with message passed as parameter.
        /// </summary>
        /// <param name="message">Message to be shown.</param>
        internal static void ShowMessageDialog(string message)
        {
            MessageDialog messageDialog = new MessageDialog(message);
            messageDialog.Topmost = true;
            messageDialog.ShowDialog();
        }
    }
}
