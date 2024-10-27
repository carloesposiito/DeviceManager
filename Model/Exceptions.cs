using System;

namespace GoogleBackupManager.Model.Exceptions
{
    /// <summary>
    /// Exception thrown when no devices are connected.
    /// </summary>
    internal class NoDevicesException : Exception
    {
        public NoDevicesException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when command prompt is not active anymore.
    /// </summary>
    internal class CommandPromptException : Exception
    {
        public CommandPromptException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when platform tools folder is not found.
    /// </summary>
    internal class PlatformToolsException : Exception
    {
        public PlatformToolsException(string message) : base(message) { }
    }
}
