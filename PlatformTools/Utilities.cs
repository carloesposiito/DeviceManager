using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;

namespace PlatformTools
{
    /// <summary>
    /// Class holding all program utilities.<br/>
    /// </summary>
    internal class Utilities
    {
        /// <summary>
        /// Constructor of the class.
        /// </summary>
        internal Utilities() { }

        /// <summary>
        /// Extracts zip archive to destination directory (created if not existing).<br/>
        /// Throws exception if operation fails.
        /// </summary>
        private void Unzip(string archivePath, string destinationPath)
        {
            try
            {
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                ZipFile.ExtractToDirectory(archivePath, destinationPath);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Checks if all Platform Tools files exist.<br/>
        /// If not, deletes folder and extract Platform Tools zip again.<br/>
        /// Tries to do it for three times maximum.
        /// Throws exception if operation fails.
        /// </summary>
        internal void CheckPlatformTools()
        {
            try
            {
                int maximumAttempts = 3;
                int currentAttempt = 1;

            CheckAgain:
                if (Directory.Exists(Constants.PATHS.PLATFORM_TOOLS_DIR))
                {
                    if (!Directory.GetFiles(Constants.PATHS.PLATFORM_TOOLS_DIR).Count().Equals(14))
                    {
                        Directory.Delete(Constants.PATHS.PLATFORM_TOOLS_DIR, true);
                        if (currentAttempt <= maximumAttempts)
                        {
                            currentAttempt++;
                            goto CheckAgain;
                        }
                        else
                        {
                            throw new Exception("Platform tools file count mismatch!");
                        }
                    }
                }
                else
                {
                    Unzip(Constants.PATHS.PLATFORM_TOOLS_ZIP, Constants.PATHS.PLATFORM_TOOLS_DIR);
                    goto CheckAgain;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Writes a line into output if passes pattern filters.<br/>
        /// This avoids to write useless lines.
        /// </summary>
        /// <param name="line">Line to be written.</param>
        /// <param name="output">Reference to output string list.</param>
        internal void WriteOutput(string line, ref List<string> output)
        {
            if (line != null)
            {
                if
                (
                    !line.Contains(Constants.PATTERNS.SENT_COMMAND_PATTERN) &&
                    !line.Contains(Constants.PATTERNS.DEVICES_COMMAND_PATTERN) &&
                    !line.Contains(Constants.PATTERNS.MICROSOFT_PATTERN) &&
                    !line.Contains(Constants.PATTERNS.CURRENT_DIR_PATTERN) &&
                    !line.Contains(Constants.PATTERNS.EXIT_COMMAND_PATTERN)
                )
                {
                    output.Add(line);
                }
            }
        }

        /// <summary>
        /// Calculate waiting time according to sent command.
        /// </summary>
        /// <param name="command">Command sent to device.</param>
        /// <returns>Time to wait to continue operations.</returns>
        internal int GetWaitingTime(string command)
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
        /// Read skipped files from final string of a push operation.<br/>
        /// Throws exception if operation fails.
        /// </summary>
        /// <param name="pushedLine">Final line.</param>
        /// <returns>Skipped files count.</returns>
        internal int ReadSkippedFiles(string pushedLine)
        {
            int skippedFiles = 0;

            try
            {
                if (!string.IsNullOrWhiteSpace(pushedLine))
                {
                    if (pushedLine.Contains(Constants.PATTERNS.PUSHED_COMMAND_PATTERN))
                    {
                        string START_PATTERN = ", ";
                        int start = pushedLine.IndexOf(START_PATTERN) + START_PATTERN.Length;

                        string END_PATTERN = $" {Constants.PATTERNS.SKIPPED_COMMAND_PATTERN}";
                        int end = pushedLine.IndexOf(END_PATTERN);

                        string transferredFilesStr = pushedLine.Substring(start, end - start);
                        if (!int.TryParse(transferredFilesStr, out skippedFiles))
                        {
                            throw new Exception("Error reading line of transferred/skipped files!");
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return skippedFiles;
        }

        /// <summary>
        /// Browse folder.
        /// </summary>
        /// <returns>Path to selected folder.</returns>
        internal string BrowseFolder()
        {
            string selectedFolder = string.Empty;

            try
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    DialogResult result = dialog.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                    {
                        selectedFolder = dialog.SelectedPath;
                    }
                }
            }
            catch (Exception)
            {
                selectedFolder = string.Empty;
            }
            
            return selectedFolder;
        }
    }
}
