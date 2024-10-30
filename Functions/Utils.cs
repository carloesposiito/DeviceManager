using GoogleBackupManager.Model.Exceptions;
using GoogleBackupManager.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

        /// <summary>
        /// Holds all folder that program needs to work.
        /// </summary>
        internal static class ProgramFolders
        {
            private static string _platformToolsDirectory;
            private static string _extractDeviceDirectory;
            private static string _backupDeviceDirectory;

            #region "Getters and setters"

            internal static string PlatformToolsDirectory { get => _platformToolsDirectory; set => _platformToolsDirectory = value; }
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
        /// <exception cref="PlatformToolsException">Thrown if check fails.</exception>
        internal static void CheckPlatformToolsFolder()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string platformToolsArchive = $"{currentDirectory}\\Resources\\PlatformTools.zip";

            ProgramFolders.PlatformToolsDirectory = $"{currentDirectory}\\PlatformTools";

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
                        throw new PlatformToolsException("And old ADB process is still running!\nPlease close it manually or restart computer.");
                    }     
                }

                Directory.CreateDirectory(ProgramFolders.PlatformToolsDirectory);

                // Unzip platform tools archive into platform tools folders
                if (!Utils.UnzipArchive(platformToolsArchive, ProgramFolders.PlatformToolsDirectory))
                {
                    throw new PlatformToolsException("Error while extracting platform tools archive!");
                }
            }
            else
            {
                throw new PlatformToolsException("PlatformTools archive not found!");
            }
        }

        /// <summary>
        /// Unzips an archive to a destination path according to parameters.
        /// </summary>
        /// <returns>True if operation is successful, otherwise false.</returns>
        internal static bool UnzipArchive(string archivePath, string destinationPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(archivePath, destinationPath);
                return true;
            }
            catch (Exception ex)
            {
                ShowMessageDialog(ex.Message);
                return false;
            }
        }

    }
}
