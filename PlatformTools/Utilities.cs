using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace PlatformTools
{
    internal class Utilities
    {
        #region "Constants"

        // Patterns
        private const string SENT_COMMAND_PATTERN = "adb ";
        private const string DEVICES_COMMAND_PATTERN = "attached";
        private const string MICROSOFT_PATTERN = "Microsoft";
        private const string CURRENT_DIR_PATTERN = "PlatformTools";
        private const string EXIT_COMMAND_PATTERN = "exit";

        #endregion

        #region "Private variables"

        // Directories
        private static string _currentDir = Directory.GetCurrentDirectory();
        private static string _platformToolsZip = $"{_currentDir}\\Resources\\PlatformTools.zip";
        private static string _platformToolsDir = $"{_currentDir}\\PlatformTools";

        #endregion

        #region "Properties"

        /// <summary>
        /// Platform tools directory.
        /// </summary>
        public static string PlatformToolsDir { get => _platformToolsDir; set => _platformToolsDir = value; }

        #endregion

        #region "Functions"

        /// <summary>
        /// Checks if platform tools folder and its files exist.
        /// </summary>
        /// <returns>True if platform tools are ok, otherwise false.</returns>
        internal static bool CheckPlatformTools()
        {
            bool operationResult = false;

            try
            {
CheckAgain:
                if (Directory.Exists(_platformToolsDir))
                {
                    // Check files count (14)
                    if (Directory.GetFiles(_platformToolsDir).Count().Equals(14))
                    {
                        operationResult = true;
                    }
                    else
                    {
                        Directory.Delete(_platformToolsDir, true);
                        goto CheckAgain;
                    }
                }
                else
                {
                    if (Unzip(_platformToolsZip, _platformToolsDir))
                    {
                        operationResult = false;
                        goto CheckAgain;
                    }
                }
            }
            catch (Exception exception)
            {
                operationResult = false; 
                
                Debug.WriteLine(
                    $"[CheckPlatformTools]\n" +
                    $"{exception.Message}\n"
                    );
            }

            return operationResult;
        }

        /// <summary>
        /// Writes a line into output if passes pattern filter.
        /// </summary>
        /// <param name="line">Line to be written.</param>
        /// <param name="output">Reference to output string list.</param>
        internal static void WriteOutput(string line, ref List<string> output)
        {
            if
            (
                !line.Contains(SENT_COMMAND_PATTERN) &&
                !line.Contains(DEVICES_COMMAND_PATTERN) &&
                !line.Contains(MICROSOFT_PATTERN) &&
                !line.Contains(CURRENT_DIR_PATTERN) &&
                !line.Contains(EXIT_COMMAND_PATTERN)
            )
            {
                output.Add(line);
            }
        }

        /// <summary>
        /// Calculate waiting time according to sent command.
        /// </summary>
        /// <param name="command">Command sent to device.</param>
        /// <returns>Time to wait to continue operations.</returns>
        internal static int GetWaitingTime(string command)
        {
            int waitingTime = 500;    

            if (command.Equals("start-server"))
            {
                waitingTime = 5000;
            }
            else if (command.Equals("adb kill-server"))
            {
                waitingTime = 250;
            }
            else if (command.Contains("pair") || command.Contains("mkdir"))
            {
                waitingTime = 100;
            }
            else if (command.Contains("connect"))
            {
                waitingTime = 1000;
            }
            else if (command.Contains("uninstall"))
            {
                waitingTime = 10000;
            }
            
            return waitingTime;
        }

        /// <summary>
        /// Extracts zip archive to selected directory.<br/>
        /// It creates destination directory if not existing.
        /// </summary>
        /// <param name="archivePath">Path to zip archive.</param>
        /// <param name="destinationPath">Path to destination directory.</param>
        /// <returns>True if extracting successful, otherwise false.</returns>
        internal static bool Unzip(string archivePath, string destinationPath)
        {
            bool operationResult = false;

            try
            {
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                ZipFile.ExtractToDirectory(archivePath, destinationPath);
                operationResult = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[Unzip]\n" +
                    $"{ex.Message}\n"
                    );
            }

            return operationResult;
        }

        #endregion
    }
}
