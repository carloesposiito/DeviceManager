using GoogleBackupManager.Model.Exceptions;
using GoogleBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

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
        private static StringBuilder _rawOutput = new StringBuilder();
        private static List<string> _output = new List<string>();

        #endregion

        #region "Getters and setters"

        public static string CurrentDirectory { get => _currentDirectory; set => _currentDirectory = value; }
        public static string PlatformToolsDirectory { get => _platformToolsDirectory; set => _platformToolsDirectory = value; }
        public static string ProgramFilesFolder { get => _programFilesFolder; set => _programFilesFolder = value; }
        internal static List<Device> ConnectedDevices { get => _connectedDevices; set => _connectedDevices = value; }
        public static String RawOutput { get => _rawOutput.ToString(); }

        #endregion

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                               FUNCTIONS                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes ADB connection:
        /// <list type="number">
        /// <item>Checks PlatfromTools directory.</item>
        /// <item>Starts ADB server.</item>
        /// <item>Scans connected devices.<br/>
        /// If a device is not authorized, tries to authorize it three times.
        /// If a device is connected both via USB and wireless, keep only USB device.
        /// </item> 
        /// </list>
        /// </summary>
        /// <returns>True if all device are ready to work, otherwise false.</returns>
        internal static bool InitializeConnection()
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
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    if (!e.Data.Contains("adb "))
                    {
                        _output.Add(e.Data);
                    }
                    _rawOutput.AppendLine(e.Data);
                }
            };

            // Add event to read received errors
            _cmdProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    _rawOutput.AppendLine(e.Data);
                }
            };

            // Start process
            _cmdProcess.Start();
            _cmdProcess.BeginOutputReadLine();
            _cmdProcess.BeginErrorReadLine();

            #endregion

            #region "Preliminary operations"

            SendCommand("adb start-server");
            _output.Clear();

            #endregion

            return ScanDevices();
        }

        /// <summary>
        /// Scans connected devices and populates <see cref="ConnectedDevices"/> list.
        /// </summary>
        /// <returns>True if connected devices are authorized, otherwise false.</returns>
        /// <exception cref="NoDevicesException">Thrown when no devices are found.</exception>
        /// <exception cref="DevicesCountException">Thrown when devices found are less than two.</exception>
        internal static bool ScanDevices()
        {
            #region "Preliminary operations"

            ConnectedDevices.Clear();

            bool result = true;

            List<String> unlimitedBackupDeviceNames = new List<String>()
            {
                "Pixel 1",
                "Pixel 2",
                "Pixel 3",
                "Pixel 4",
                "Pixel 5"
            };

            #endregion
            
            SendCommand("adb devices", 7500);

            if (_output.Count() > 0)
            {
                // Remove line containig "List of devices attached"
                _output.RemoveAt(0);

                // Check on remaining strings
                if (_output.Count.Equals(0))
                {
                    throw new NoDevicesException("No devices found!");
                }
                else
                {
                    try
                    {
                        List<string> devices = new List<string>(_output);
                        _output.Clear();

                        foreach (string device in devices)
                        {
                            bool addDevice = true;

                            #region "Get device info"

                            List<string> lineParts = device.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                            string name = "Undefined";
                            string id = lineParts[0].Trim();
                            bool authStatus = lineParts[1].Trim() == "device";
                            bool hasUnlimitedBackup = false;
                            bool isWirelessConnected = id.Contains(":") || id.Contains("adb");

                            if (authStatus)
                            {
                                // If device is authorized
                                // Get device name
                                SendCommand($"adb -s {id} shell getprop ro.product.model", 750);

                                if (_output.Count > 0)
                                {
                                    name = _output[0];
                                }
                                else
                                {
                                    name = id;
                                }

                                // Check if device has unlimited backup
                                hasUnlimitedBackup = unlimitedBackupDeviceNames.Any(unlimitedBackupDeviceName => name.Contains(unlimitedBackupDeviceName));
                                hasUnlimitedBackup = name.Equals(id) ? true : hasUnlimitedBackup;
                            }

                            #endregion

                            // Check if device already exists and if yes, update device data keeping only its USB ID
                            // This because USB file transfer speed is more than wireless one
                            // Otherwise add it to devices list

                            #region "Check if device already exists"

                            foreach (var existingDevice in ConnectedDevices)
                            {
                                // If a device with same name is found
                                if (existingDevice.Name == name)
                                {
                                    // Keep the USB ID that should not containt "abd" or ":" strings
                                    existingDevice.ID = existingDevice.ID.Contains("adb") || existingDevice.ID.Contains(":") ? id : existingDevice.ID;
                                    existingDevice.IsWirelessConnected = false;
                                    addDevice = false;
                                }
                            }

                            if (addDevice)
                            {
                                ConnectedDevices.Add(new Device(name, id, authStatus, hasUnlimitedBackup, isWirelessConnected));
                            }

                            #endregion

                            // Clear output
                            _output.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        _output.Clear();
                        ADB.ConnectedDevices.Clear();
                        throw ex;
                    }                    
                }

                // Check if connected devices are at least two
                if (ConnectedDevices.Count < 2)
                {
                    throw new DevicesCountException("Connected devices must be at least two!");
                }
                else
                {
                    int authorizedDevicesCount = 0;
                    int unlimitedBackupDevicesCount = 0;
                    foreach (Device device in ADB.ConnectedDevices)
                    {
                        authorizedDevicesCount = device.IsAuthorized ? authorizedDevicesCount + 1 : authorizedDevicesCount;
                        unlimitedBackupDevicesCount = device.HasUnlimitedBackup ? unlimitedBackupDevicesCount + 1 : unlimitedBackupDevicesCount;
                    }

                    return authorizedDevicesCount >= 2 && unlimitedBackupDevicesCount >= 1 ? true : false;
                }
            }
            else
            {
                throw new PlatformToolsTimeoutException("Error waiting for an ADB reply!\nTry to run again program.");
            }
        }

        /// <summary>
        /// Tries to authorize device with ID passed as parameter.
        /// </summary>
        /// <param name="deviceId">ID of the device to be authorized.</param>
        /// <returns>True if authorization succed.</returns>
        internal static bool AuthorizeDevice(string deviceId)
        {
            bool isAuthorized = false;

            for (int i = 1; i <= 3; i++)
            {
                if (isAuthorized)
                {
                    break;
                }

                // Command to show popup
                SendCommand("adb kill-server");
                SendCommand("adb devices");

                // Show message waiting for authorization
                Utils.ShowMessageDialog(
                    $"Please authorize this computer via the popup displayed on device screen (ID = {deviceId}), then click OK!\n" +
                    $"Attempt {i}/3"
                );

                _output.Clear();

                // Command to check device new auth status
                SendCommand("adb devices");

                // Output should contain only connected devices
                if (_output.Any(str => str.Contains(deviceId) && str.Contains("device")))
                {
                    isAuthorized = true;
                }

                // Clear output for next attempt
                _output.Clear();
            }

            return isAuthorized;
        }

        /// <summary>
        /// Connects to a device via wireless ADB.
        /// </summary>
        /// <param name="deviceIp">Device IP address.</param>
        /// <param name="devicePort">Device port.</param>
        /// <returns>True if connection is successful, otherwise false.</returns>
        internal static bool ConnectWirelessDevice(string deviceIp, string devicePort)
        {
            _output.Clear();

            // Proceed with connection
            SendCommand($"adb connect {deviceIp}:{devicePort}");

            if (_output.Count > 0)
            {
                string commandOutput = _output.Last();
                _output.Clear();

                // Check received output
                if (!string.IsNullOrWhiteSpace(commandOutput) && commandOutput.Contains("connected"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Pair a device via wireless ADB.
        /// </summary>
        /// <param name="deviceIp">Device IP address.</param>
        /// <param name="devicePort">Device port.</param>
        /// <param name="devicePairingCode">Device pairing code.</param>
        /// <returns>True if pairing is successful, otherwise false.</returns>
        internal static bool PairWirelessDevice(string deviceIp, string devicePort, string devicePairingCode)
        {
            _output.Clear();

            // Proceed with pairing
            SendCommand($"adb pair {deviceIp}:{devicePort}", devicePairingCode);

            if (_output.Count > 0)
            {
                string commandOutput = _output.Last();
                _output.Clear();

                if (!string.IsNullOrWhiteSpace(commandOutput) && commandOutput.Contains("Enter"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                                COMMANDS                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Executes command passed as parameter.<br/>
        /// </summary>
        /// <param name="command">Command to be executed.</param>
        /// <param name="delay">[OPTIONAL] Time to wait after sending command.</param>
        /// <exception cref="CommandPromptException">Thrown if command prompt process is not active anymore.</exception>
        private static void SendCommand(string command, int delay = 500)
        {
            if (!_cmdProcess.HasExited)
            {
                // Write command
                _cmdProcess.StandardInput.WriteLine(command);
                _cmdProcess.StandardInput.Flush();

                // Wait for response
                Task.Delay(delay).Wait();
            }
            else
            {
                throw new CommandPromptException("Command prompt process is not active anymore!");
            }
        }

        /// <summary>
        /// Executes command and then replies with data passed as parameters.<br/>
        /// </summary>
        /// <param name="command">Command to be executed.</param>
        /// <param name="response">Response to be given.</param>
        /// <exception cref="CommandPromptException">Thrown if command prompt process is not active anymore.</exception>
        private static void SendCommand(string command, string response)
        {
            if (!_cmdProcess.HasExited)
            {
                // Write command
                _cmdProcess.StandardInput.WriteLine(command);
                _cmdProcess.StandardInput.Flush();

                // Write response
                _cmdProcess.StandardInput.WriteLine(response);
                _cmdProcess.StandardInput.Flush();

                // Wait for response
                Task.Delay(500).Wait();
            }
            else
            {
                throw new CommandPromptException("Command prompt process is not active anymore!");
            }
        }

        internal static void CloseConnection()
        {
            SendCommand("adb kill-server");
            _cmdProcess.Close();
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
