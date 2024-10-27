using GoogleBackupManager.Model.Exceptions;
using GoogleBackupManager.Model;
using GoogleBackupManager.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
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

        private static Process _cmdProcess = new Process();
        private static List<string> _output = new List<string>();
        private static List<string> _errors = new List<string>();

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
                throw new PlatformToolsException("Platform tools folder not found!");
            }

            #endregion

            #region "Configure process info"

            _cmdProcess.StartInfo.CreateNoWindow = true;
            _cmdProcess.StartInfo.FileName = "cmd.exe";
            _cmdProcess.StartInfo.RedirectStandardInput = true;
            _cmdProcess.StartInfo.RedirectStandardOutput = true;
            _cmdProcess.StartInfo.RedirectStandardError = true;
            _cmdProcess.StartInfo.UseShellExecute = false;
            _cmdProcess.StartInfo.WorkingDirectory = _platformToolsDirectory;

            // Add event to read received output data
            // Exclude command sent by user
            _cmdProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data) && !e.Data.Contains("adb "))
                {
                    _output.Add(e.Data);
                }
            };

            // Add event to read received errors
            // Exclude command sent by user
            _cmdProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data) && !e.Data.Contains("adb "))
                {
                    _errors.Add(e.Data);
                }
            };

            // Start process
            _cmdProcess.Start();
            _cmdProcess.BeginOutputReadLine();
            _cmdProcess.BeginErrorReadLine();

            #endregion

            #region "Preliminary operations"

            // Set echo to off
            SendCommand("@echo off");
            ClearOutput();

            SendCommand("adb start-server");
            ClearOutput();

            #endregion

            ScanDevices();
        }

        internal static void ConnectWirelessDevice(string deviceIp, string devicePort, string devicePairingCode = "Undefined")
        {
            //if (devicePairingCode.Equals("Undefined"))
            //{
            //    string connectionOutput = SendCommand($"adb connect {deviceIp}:{devicePort}")[0];

            //    if (!string.IsNullOrWhiteSpace(connectionOutput) && connectionOutput.Contains("connected"))
            //    {
            //        return true;
            //    }
            //    else
            //    {
            //        return false;
            //    }
            //}
            //else
            //{
            //    return SendPairingCommand(deviceIp, devicePort, devicePairingCode);
            //}
        }

        /// <summary>
        /// Scans connected devices and populates <see cref="ConnectedDevices"/> list.
        /// </summary>
        /// <exception cref="NoDevicesException">Thrown when device is not authorized after three attempts.</exception>
        private static void ScanDevices()
        {
            List<String> unlimitedBackupDeviceNames = new List<String>()
            {
                "Pixel 1",
                "Pixel 2",
                "Pixel 3",
                "Pixel 4",
                "Pixel 5"
            };

            SendCommand("adb devices");

            // Remove line containig "List of devices attached"
            _output.RemoveAt(0);           

            // Check on remaining strings
            if (_output.Count == 0)
            {
                throw new NoDevicesException("No devices connected!");
            }
            else
            {
                List<string> devices = new List<string>(_output);
                ClearOutput();

                foreach (string device in devices)
                {
                    // Get device id and authorization status
                    List<string> lineParts = device.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    string name = "Undefined";
                    string id = lineParts[0].Trim();
                    bool authStatus = lineParts[1].Trim() == "device";
                    bool hasUnlimitedBackup = false;

                    if (authStatus)
                    {
                        // If device is authorized
                        // Get device name
                        SendCommand($"adb -s {id} shell getprop ro.product.model");
                        name = _output.Last();

                        // Check if device has unlimited backup
                        foreach (string unlimitedBackupDeviceName in unlimitedBackupDeviceNames)
                        {
                            if (name.Contains(unlimitedBackupDeviceName))
                            {
                                hasUnlimitedBackup = true;
                            }
                        }
                    }
                    else
                    {
                        // Device is not authorized
                        // Try to authorize it for three times
                        //authStatus = AuthorizeDevice(id);

                    }

                    // Add device to connected device list
                    ConnectedDevices.Add(new Device(name, id, authStatus, hasUnlimitedBackup));
                }
            }


        }

        /// <summary>
        /// Tries to authorize device described by parameters.
        /// </summary>
        /// <param name="deviceId">Id of the device to be authorized.</param>
        private static void AuthorizeDevice(string deviceId)
        {
            //bool isAuthorized = false;

            //for (int i = 1; i <= 3; i++)
            //{
            //    if (isAuthorized)
            //    {
            //        break;
            //    }

            //    SendCommand("adb kill-server");
            //    SendCommand("adb devices");

            //    // Wait for authorization
            //    MessageDialog messageDialog = new MessageDialog(
            //        $"Please authorize this computer via the popup displayed on smartphone screen (ID: {deviceId}), then click OK!\n" +
            //        $"Attempt {i}/3");
            //    messageDialog.ShowDialog();

            //    // Check device auth status
            //    List<string> devicesOutput = SendCommand("adb devices");
            //    foreach (string line in devicesOutput)
            //    {
            //        if (line.Contains(deviceId))
            //        {
            //            if (line.Contains("device"))
            //            {
            //                isAuthorized = true;
            //                break;
            //            }
            //        }
            //    }
            //}

            //return isAuthorized;
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                                COMMANDS                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        internal static void RefreshDevices()
        {
            SendCommand("adb kill-server");
            SendCommand("adb devices");
        }

        /// <summary>
        /// Executes command passed as parameter.<br/>
        /// </summary>
        /// <param name="command">Command to be executed.</param>
        /// <exception cref="CommandPromptException">Thrown if command prompt process is not active anymore.</exception>
        internal static void SendCommand(string command)
        {
            if (!_cmdProcess.HasExited)
            {
                // Write command
                _cmdProcess.StandardInput.WriteLine(command);
                _cmdProcess.StandardInput.Flush();

                // Wait for response
                Task.Delay(250).Wait();
            }
            else
            {
                throw new CommandPromptException("Command prompt process is not active anymore!");
            }
        }

        /// <summary>
        /// Pair device via wireless ADB.
        /// </summary>
        /// <param name="deviceIp">Device IP address.</param>
        /// <param name="devicePort">Device port address.</param>
        /// <param name="devicePairingCode">Device pairing code.</param>
        /// <returns>True if connection is successful, otherwise false.</returns>
        private static void SendPairingCommand(string deviceIp, string devicePort, string devicePairingCode)
        {
            //using (Process pairProcess = new Process())
            //{
            //    // Start cmd process
            //    pairProcess.StartInfo.FileName = "cmd.exe";
            //    pairProcess.StartInfo.WorkingDirectory = _platformToolsDirectory;
            //    pairProcess.StartInfo.UseShellExecute = false;
            //    pairProcess.StartInfo.RedirectStandardInput = true;
            //    pairProcess.StartInfo.RedirectStandardOutput = true;
            //    pairProcess.StartInfo.RedirectStandardError = true;
            //    pairProcess.StartInfo.CreateNoWindow = true;
            //    pairProcess.Start();

            //    // Read output
            //    pairProcess.BeginOutputReadLine();

            //    // Send commands
            //    if (!pairProcess.HasExited)
            //    {
            //        // Send pair command
            //        pairProcess.StandardInput.WriteLine($"adb pair {deviceIp}:{devicePort}");
            //        pairProcess.StandardInput.Flush();

            //        // Send connection command
            //        pairProcess.StandardInput.WriteLine($"{devicePairingCode}");
            //        pairProcess.StandardInput.Flush();

            //        // Close command prompt
            //        pairProcess.StandardInput.WriteLine("exit");
            //        pairProcess.StandardInput.Flush();
            //        pairProcess.Close();
            //    }

            //    return SendCommand($"adb connect {deviceIp}").Contains("connected");
            //}
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

        private static void ClearOutput()
        {
            _output.Clear();
            _errors.Clear();
        }
    }
}
