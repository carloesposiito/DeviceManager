using System.IO;

namespace PlatformTools
{
    internal class Constants
    {
        /// <summary>
        /// Holds all paths.
        /// </summary>
        internal class PATHS
        {
            internal static string CURRENT_DIR = Directory.GetCurrentDirectory();
            internal static string PLATFORM_TOOLS_ZIP = $"{CURRENT_DIR}\\Resources\\PlatformTools.zip";
            internal static string PLATFORM_TOOLS_DIR = $"{CURRENT_DIR}\\PlatformTools";
        }

        /// <summary>
        /// Holds all recursive patterns.
        /// </summary>
        internal class PATTERNS
        {
            internal const string SENT_COMMAND_PATTERN = "adb ";
            internal const string DEVICES_COMMAND_PATTERN = "attached";
            internal const string MICROSOFT_PATTERN = "Microsoft";
            internal const string CURRENT_DIR_PATTERN = "PlatformTools";
            internal const string EXIT_COMMAND_PATTERN = "exit";
        }

        /// <summary>
        /// Holds all other constants.
        /// </summary>
        internal class MISC
        {
            internal const int DEFAULT_WAITING_TIME = 100;
            internal const int SCAN_DEVICES_TIMEOUT = 5000;
        }
    }
}
