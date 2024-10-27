using GoogleBackupManager.Model;
using GoogleBackupManager.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace GoogleBackupManager.Functions
{
    internal static class ADB
    {
        #region "Variables"

        private static string _currentDirectory;
        private static string _platformToolsDirectory;
        private static string _programFilesFolder;
        private static string _unlimitedDeviceFolder;
        private static string _newDeviceFolder;

        private static List<Device> _connectedDevices = new List<Device>();
        private static List<String> _unlimitedBackupDeviceNames = new List<String>()
        {
            "Pixel 1",
            "Pixel 2",
            "Pixel 3",
            "Pixel 4",
            "Pixel 5"
        };

        #endregion

        #region "Getters and setters"

        public static string CurrentDirectory { get => _currentDirectory; set => _currentDirectory = value; }
        public static string PlatformToolsDirectory { get => _platformToolsDirectory; set => _platformToolsDirectory = value; }
        public static string ProgramFilesFolder { get => _programFilesFolder; set => _programFilesFolder = value; }
        internal static List<Device> ConnectedDevices { get => _connectedDevices; set => _connectedDevices = value; }

        #endregion

        /// <summary>
        /// Initializes connection checking directories, starting adb server and scanning connected devices.<br/>
        /// If a device is not authorized, tries to authorize it three times.
        /// </summary>
        internal static void InitializeConnection()
        {
            #region "Check platform tools folder"

            _currentDirectory = Directory.GetCurrentDirectory();
            _platformToolsDirectory = $"{_currentDirectory}\\PlatformTools";

            if (!Directory.Exists(_platformToolsDirectory))
            {
                throw new DirectoryNotFoundException("Platform tools folder not found!");
            }

            #endregion

            // Start adb server
            SendCommand("adb start-server");

            ScanDevices();
        }

        /// <summary>
        /// Scans connected devices and populates <see cref="ConnectedDevices"/> list.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Thrown when device is not authorized after three attempts.</exception>
        private static void ScanDevices()
        {
            var devicesOutput = SendCommand("adb devices");
            devicesOutput.RemoveAt(0);

            foreach (string deviceOutput in devicesOutput)
            {
                List<string> lineComponents = deviceOutput.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                string id = lineComponents[0].Trim();
                bool authStatus = lineComponents[1].Trim() == "device" ? true : false;

                if (!authStatus)
                {
                    authStatus = AuthorizeDevice(id);
                    if (!authStatus)
                    {
                        throw new UnauthorizedAccessException($"Authorizing device with ID = {id} failed!");
                    }
                }

                string name = SendCommand($"adb -s {id} shell getprop ro.product.model")[0];

                bool hasUnlimitedBackup = false;
                foreach (string unlimitedBackupDeviceName in _unlimitedBackupDeviceNames)
                {
                    if (name.Contains(unlimitedBackupDeviceName))
                    {
                        hasUnlimitedBackup = true;
                    }
                }

                // Add device to connected device list
                ConnectedDevices.Add(new Device(name, id, authStatus, hasUnlimitedBackup));
            }
        }

        internal static void PairWirelessDevice()
        {

        }

        /// <summary>
        /// Tries to authorize device described by parameters.
        /// </summary>
        /// <param name="deviceId">Id of the device to be authorized.</param>
        private static bool AuthorizeDevice(string deviceId)
        {
            bool isAuthorized = false;

            for (int i = 1; i <= 3; i++)
            {
                if (isAuthorized)
                {
                    break;
                }

                SendCommand("adb kill-server");
                SendCommand("adb devices");

                // Wait for authorization
                MessageDialog messageDialog = new MessageDialog(
                    $"Please authorize this computer via the popup displayed on smartphone screen (ID: {deviceId}), then click OK!\n" +
                    $"Attempt {i}/3");
                messageDialog.ShowDialog();

                // Check device auth status
                List<string> devicesOutput = SendCommand("adb devices");
                foreach (string line in devicesOutput)
                {
                    if (line.Contains(deviceId))
                    {
                        if (line.Contains("device"))
                        {
                            isAuthorized = true;
                            break;
                        }
                    }
                }
            }
            
            return isAuthorized;
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                                COMMANDS                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create a command prompt in Platform Tools folder and executes command passed as parameter.<br/>
        /// </summary>
        /// <param name="command">Command to be executed.</param>
        /// <returns>Output from command prompt as list of strings.</returns>
        /// <exception cref="ArgumentNullException">Thrown when command is an empty string.</exception>
        private static List<string> SendCommand(string command)
        {
            if (String.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentNullException("Command not defined!");
            };

            List<string> outputLines = new List<string>();

            // Create cmd process with command passed as parameter
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.WorkingDirectory = _platformToolsDirectory;
            process.StartInfo.Arguments = $"/C {command}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            // Read output
            string output = process.StandardOutput.ReadToEnd();

            // Add to list splitting through new line characters
            outputLines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // Return output
            return outputLines;
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                               UTILITIES                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        private static void CreateProgramFolders()
        {
            _programFilesFolder = $"{_currentDirectory}\\Program files";

            if (!Directory.Exists(_programFilesFolder))
            {
                Directory.CreateDirectory(_programFilesFolder);
            }

            _unlimitedDeviceFolder = $"{_programFilesFolder}\\";
        }
    }
}
