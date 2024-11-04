using System;

namespace AndroidDeviceManager.Model.Exceptions
{
    /// <summary>
    /// Exception thrown when platform tools folder is not found.
    /// </summary>
    internal class PlatformToolsFolderException : Exception
    {
        internal PlatformToolsFolderException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when passed platform tools timeout.
    /// </summary>
    internal class PlatformToolsProcessException : Exception
    {
        public PlatformToolsProcessException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when passed platform tools timeout.
    /// </summary>
    internal class PlatformToolsTransferException : Exception
    {
        public PlatformToolsTransferException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when command prompt is not active anymore.
    /// </summary>
    internal class CommandPromptException : Exception
    {
        internal CommandPromptException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when command prompt is not active anymore.
    /// </summary>
    internal class ScrcpyProcessException : Exception
    {
        internal ScrcpyProcessException(string message) : base(message) { }
    }

}
