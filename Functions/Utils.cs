using GoogleBackupManager.Model;
using GoogleBackupManager.Model.Exceptions;
using GoogleBackupManager.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoogleBackupManager.Functions
{
    internal static class Utils
    {
        /// <summary>
        /// Holds devices names with unlimited backup.
        /// </summary>
        internal static List<string> UnlimitedBackupDevices = new List<string>()
        {
            "Pixel 1",
            "Pixel 2",
            "Pixel 3",
            "Pixel 4",
            "Pixel 5"
        };

        /// <summary>
        /// Holds all different waiting times.
        /// </summary>
        internal enum WAITING_TIME
        {
            DEFAULT = 500,
            LONG_SCAN = 7500,
            SHORT_SCAN = 2500,
            GET_NAME = 750,
            START_SERVER = 3500
        }

        internal static int CalculateWaitingTime(string command)
        {
            if (command.Equals("start-server"))
            {
                return 5000;
            }
            else if (command.Equals("adb devices"))
            {
                return 7500;
            }
            else if (command.Equals("adb kill-server"))
            {
                return 250;
            }
            else if (command.Contains("pair") || command.Contains("mkdir"))
            {
                return 100;
            }
            else
            {
                return 500;
            }
        }

        /// <summary>
        /// Holds all folder that program needs to work.
        /// </summary>
        internal static class ProgramFolders
        {
            private static string _currentDirectory;
            private static string _platformToolsDirectory;
            private static string _unlimitedBackupDirectory;
            private static string _extractDeviceDirectory;
            private static string _backupDeviceDirectory;

            #region "Getters and setters"

            internal static string CurrentDirectory { get => _currentDirectory; set => _currentDirectory = value; }
            internal static string PlatformToolsDirectory { get => _platformToolsDirectory; set => _platformToolsDirectory = value; }

            internal static string UnlimitedBackupDirectory { get => _unlimitedBackupDirectory; set => _unlimitedBackupDirectory = value; }
            internal static string ExtractDeviceDirectory { get => _backupDeviceDirectory; set => _backupDeviceDirectory = value; }
            internal static string BackupDeviceDirectory { get => _extractDeviceDirectory; set => _extractDeviceDirectory = value; }

            #endregion
        }

        /// <summary>
        /// Show a dialog with parameter message.
        /// </summary>
        internal static void ShowMessageDialog(string message)
        {
            MessageDialog messageDialog = new MessageDialog(message);
            messageDialog.Topmost = true;
            messageDialog.ShowDialog();
        }

        /// <summary>
        /// Open a folder browser dialog to pick a folder.
        /// </summary>
        /// <returns>Selected folder path.</returns>
        internal static string SelectFolder()
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select files folder";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.SelectedPath;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Checks platform tools folder.
        /// </summary>
        /// <exception cref="PlatformToolsFolderException">Thrown if check fails.</exception>
        internal static void CheckPlatformToolsFolder()
        {
            ProgramFolders.CurrentDirectory = Directory.GetCurrentDirectory();
            ProgramFolders.PlatformToolsDirectory = $"{ProgramFolders.CurrentDirectory}\\PlatformTools";
            string platformToolsArchive = $"{ProgramFolders.CurrentDirectory}\\Resources\\PlatformTools.zip";

            // Check platform tools zip existing
            if (File.Exists(platformToolsArchive))
            {
                // If platform tools folder exists delete it and create a new one
                if (Directory.Exists(ProgramFolders.PlatformToolsDirectory))
                {
                    try
                    {
                        Directory.Delete(ProgramFolders.PlatformToolsDirectory, true);
                    }
                    catch (Exception)
                    {
                        if (KillOldPlatformToolsProcess())
                        {
                            Directory.Delete(ProgramFolders.PlatformToolsDirectory, true);
                        }
                        else
                        {
                            throw new PlatformToolsFolderException("And old ADB process is still running!\nPlease close it manually or restart computer.");
                        }
                    }
                }

                Directory.CreateDirectory(ProgramFolders.PlatformToolsDirectory);

                // Unzip platform tools archive into platform tools folders
                if (!Utils.UnzipArchive(platformToolsArchive, ProgramFolders.PlatformToolsDirectory))
                {
                    throw new PlatformToolsFolderException("Error while extracting platform tools archive!");
                }
            }
            else
            {
                throw new PlatformToolsFolderException("PlatformTools archive not found!");
            }
        }

        /// <summary>
        /// Unzips an archive to a destination path according to parameters.
        /// </summary>
        /// <returns>True if operation is successful, otherwise false.</returns>
        private static bool UnzipArchive(string archivePath, string destinationPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(archivePath, destinationPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool KillOldPlatformToolsProcess()
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c adb kill-server",
                    WorkingDirectory = ProgramFolders.PlatformToolsDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();
                    process.WaitForExit();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates folder needed to perform unlimited backup operations.
        /// </summary>
        internal static void CreateProgramFolders(Device extractDevice, Device backupDevice)
        {
            // Create unlimited backup folder
            ProgramFolders.UnlimitedBackupDirectory = $"{ProgramFolders.CurrentDirectory}\\UnlimitedBackup";

            if (!Directory.Exists(ProgramFolders.UnlimitedBackupDirectory))
            {
                Directory.CreateDirectory(ProgramFolders.UnlimitedBackupDirectory);
            }

            // Inside it create directory for extract device
            ProgramFolders.ExtractDeviceDirectory = $"{ProgramFolders.UnlimitedBackupDirectory}\\{extractDevice.Name.Replace(" ", "_")}_{extractDevice.ID}\\DCIM_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}";

            if (!Directory.Exists(ProgramFolders.ExtractDeviceDirectory))
            {
                Directory.CreateDirectory(ProgramFolders.ExtractDeviceDirectory);
            }

            // Inside it create directory for backup device
            ProgramFolders.BackupDeviceDirectory = $"{ProgramFolders.UnlimitedBackupDirectory}\\{backupDevice.Name.Replace(" ", "_")}_{backupDevice.ID}\\DCIM_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}";

            if (!Directory.Exists(ProgramFolders.BackupDeviceDirectory))
            {
                Directory.CreateDirectory(ProgramFolders.BackupDeviceDirectory);
            }
        }
    }
}
