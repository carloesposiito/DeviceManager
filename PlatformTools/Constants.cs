using System.IO;

namespace PlatformTools
{
    public class Constants
    {
        /// <summary>
        /// Holds all paths.
        /// </summary>
        public class PATHS
        {
            public static string CURRENT_DIR = Directory.GetCurrentDirectory();
            public static string PLATFORM_TOOLS_ZIP = $"{CURRENT_DIR}\\Resources\\PlatformTools.zip";
            public static string PLATFORM_TOOLS_DIR = $"{CURRENT_DIR}\\PlatformTools";
            public static string BACKUP_DIR = $"{CURRENT_DIR}\\Backups";
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
            internal const string PUSHED_COMMAND_PATTERN = "pushed";
            internal const string PULLED_COMMAND_PATTERN = "pulled";
            internal const string SKIPPED_COMMAND_PATTERN = "skipped";
            internal const string APP_START_PATTERN = "package:";
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
