using System;

namespace GoogleBackupManager.Model.Exceptions
{
    /// <summary>
    /// Exception thrown when platform tools folder is not found.
    /// </summary>
    internal class PlatformToolsException : Exception
    {
        internal PlatformToolsException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when command prompt is not active anymore.
    /// </summary>
    internal class CommandPromptException : Exception
    {
        internal CommandPromptException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when no devices are connected.
    /// </summary>
    internal class NoDevicesException : Exception
    {
        internal NoDevicesException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when conncected devices are less than two.
    /// </summary>
    internal class DevicesCountException : Exception
    {
        internal DevicesCountException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when device authorization fails.
    /// </summary>
    internal class AuthException : Exception
    {
        public AuthException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when passed platform tools timeout.
    /// </summary>
    internal class PlatformToolsTimeoutException : Exception
    {
        public PlatformToolsTimeoutException(string message) : base(message) { }
    }
}
