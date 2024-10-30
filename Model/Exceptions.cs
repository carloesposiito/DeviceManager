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
    /// Exception thrown when passed platform tools timeout.
    /// </summary>
    internal class PlatformToolsTimeoutException : Exception
    {
        public PlatformToolsTimeoutException(string message) : base(message) { }
    }
}
