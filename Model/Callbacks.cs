using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleBackupManager.Model
{
    internal class Callbacks
    {
        /// <summary>
        /// Function to be called when a raw output changed.
        /// </summary>
        /// <param name="rawOutput">Raw output.</param>
        public delegate void RawOutputChanged(StringBuilder rawOutput);
    }
}
