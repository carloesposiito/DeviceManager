using GoogleBackupManager.Model.Exceptions;
using GoogleBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleBackupManager.Functions
{
    internal static class ADB
    {
        #region "Variables       

        private static bool _isCommandPromptInitialized = false;
        private static bool _isServerRunning = false;
        private static Process _commandPromptProcess = new Process();
        private static List<Device> _connectedDevices = new List<Device>();
        private static StringBuilder _rawOutput = new StringBuilder();
        private static List<string> _filteredOutput = new List<string>();

        #endregion

        #region "Getters and setters"

        internal static List<Device> ConnectedDevices { get => _connectedDevices; set => _connectedDevices = value; }
        internal static String RawOutput { get => _rawOutput.ToString(); }

        #endregion

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                               FUNCTIONS                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes ADB connection:<br/>
        /// <list type="number">
        /// <item>Checks platform tools folder.</item>
        /// <item>Initializes command prompt.</item>
        /// <item>Start ADB server.</item>
        /// </list>
        /// </summary>
        internal static void InitializeConnection()
        {
            Utils.CheckPlatformToolsFolder();
            InitializeCommandPrompt();
            StartServer();
        }

        /// <summary>
        /// Scans connected devices and populates <see cref="ConnectedDevices"/> list.
        /// </summary>
        /// <returns>True if connected devices are enough to permit backup, otherwise false.</returns>
        internal static bool ScanDevices()
        {
            #region "Preliminary operations"

            if (!_isCommandPromptInitialized)
            {
                InitializeCommandPrompt();
            }

            if (!_isServerRunning)
            {
                StartServer();
            }

            // Clear previous output
            _filteredOutput.Clear();

            // Clear previous devices
            ConnectedDevices.Clear();

            #endregion

            // Send ADB devices request
            SendCommand("adb devices");

            if (_filteredOutput.Count() > 0)
            {
                // Remove line containig "List of devices attached"
                _filteredOutput.RemoveAt(0);

                // Check on remaining strings
                if (_filteredOutput.Count > 0)
                {
                    try
                    {
                        // Copy devices from output
                        List<string> deviceLines = new List<string>(_filteredOutput);

                        // Clear previous output
                        _filteredOutput.Clear();

                        foreach (string deviceLine in deviceLines)
                        {
                            #region "Get device info"

                            // Split device line into parts
                            List<string> lineParts = deviceLine.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                            // Get device id
                            string deviceIdentifier = lineParts[0].Trim();

                            // Get device authorization status
                            bool deviceAuthorizationStatus = lineParts[1].Trim() == "device";

                            // Get info about connection type
                            bool deviceIsWirelessConnected = deviceIdentifier.Contains(":") || deviceIdentifier.Contains("adb");

                            // Set default name
                            string deviceName = $"Undefined - {deviceIdentifier}";

                            // Set default unlimited backup status
                            bool deviceHasUnlimitedBackup = false;
                            
                            // If device is authorized it's possible to get real device name
                            if (deviceAuthorizationStatus)
                            {
                                // Get device name
                                SendCommand($"adb -s {deviceIdentifier} shell getprop ro.product.model");

                                // If it's a valid name, assing it
                                deviceName = _filteredOutput.Count > 0 ? _filteredOutput[0] : deviceIdentifier;

                                // Check if there's already an existing device with same name
                                foreach (Device connectedDevice in _connectedDevices)
                                {
                                    // If true, add id to their names
                                    if (connectedDevice.Name == deviceName)
                                    {
                                        connectedDevice.Name = $"{connectedDevice.Name} - {connectedDevice.ID}";
                                        deviceName = $"{deviceName} - {deviceIdentifier}";
                                    }
                                }

                                // With device real name, check if it has unlimited backup
                                deviceHasUnlimitedBackup = Utils.UnlimitedBackupDevices.Any(unlimitedBackupDeviceName => deviceName.Contains(unlimitedBackupDeviceName));
                                deviceHasUnlimitedBackup = deviceName.Equals(deviceIdentifier) ? true : deviceHasUnlimitedBackup;
                            }

                            _connectedDevices.Add(new Device(deviceName, deviceIdentifier, deviceAuthorizationStatus, deviceHasUnlimitedBackup, deviceIsWirelessConnected));

                            #endregion

                            // Clear previous output
                            _filteredOutput.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        // If something goes wrong
                        
                        // Clear previous output
                        _filteredOutput.Clear();

                        // Clear connected devices
                        ADB.ConnectedDevices.Clear();

                        throw ex;
                    }
                }

                #region "Check if connected devices are enough to perform backup"

                int authorizedDevicesCount = 0;
                int unlimitedBackupDevicesCount = 0;
                foreach (Device device in ADB.ConnectedDevices)
                {
                    authorizedDevicesCount = device.IsAuthorized ? authorizedDevicesCount + 1 : authorizedDevicesCount;
                    unlimitedBackupDevicesCount = device.HasUnlimitedBackup ? unlimitedBackupDevicesCount + 1 : unlimitedBackupDevicesCount;
                }

                bool enoughDevices = authorizedDevicesCount >= 2 && unlimitedBackupDevicesCount >= 1 ? true : false;

                #endregion

                return enoughDevices;
            }
            else
            {
                throw new PlatformToolsTimeoutException("Error waiting for an ADB reply!\nTry to run again program.");
            }
        }

        /// <summary>
        /// Tries to authorize device with ID passed as parameter.
        /// </summary>
        /// <param name="deviceIdentifier">Identifier of the device to be authorized.</param>
        /// <returns>True if authorization succed, otherwise false.</returns>
        internal static bool AuthorizeDevice(string deviceIdentifier)
        {
            bool isDeviceAuthorized = false;

            for (int i = 1; i <= 3; i++)
            {
                if (isDeviceAuthorized)
                {
                    break;
                }

                // Restart server to permit to show popup on device
                SendCommand("adb kill-server");
                StartServer();

                // Send devices command to show popup
                SendCommand("adb devices", Utils.WAITING_TIME.SHORT_SCAN);

                // Show message waiting for authorization
                Utils.ShowMessageDialog(
                    $"Please authorize this computer via the popup displayed on device screen (ID = {deviceIdentifier}), then click OK!\n" +
                    $"Attempt {i}/3"
                );

                _filteredOutput.Clear();

                // Send devices command to check device new auth status
                SendCommand("adb devices");

                // Output should contain only connected devices
                // If output contains device id and "device" string it means it's authorized
                if (_filteredOutput.Any(str => str.Contains(deviceIdentifier) && str.Contains("device")))
                {
                    isDeviceAuthorized = true;
                }

                // Clear output for next attempt
                _filteredOutput.Clear();
            }

            return isDeviceAuthorized;
        }

        /// <summary>
        /// Connects to a device via wireless ADB.
        /// </summary>
        /// <returns>True if connection is successful, otherwise false.</returns>
        internal static bool ConnectWirelessDevice(string deviceIp, string devicePort)
        {
            // Clear previous output
            _filteredOutput.Clear();

            // Send connection command
            SendCommand($"adb connect {deviceIp}:{devicePort}");

            if (_filteredOutput.Count > 0)
            {
                // Save output before clear it
                string commandOutput = _filteredOutput.Last();
                _filteredOutput.Clear();

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
        /// <returns>True if pairing is successful, otherwise false.</returns>
        internal static bool PairWirelessDevice(string deviceIp, string devicePort, string devicePairingCode)
        {
            _filteredOutput.Clear();

            // Send pairing command
            SendCommand($"adb pair {deviceIp}:{devicePort}", Utils.WAITING_TIME.DEFAULT, devicePairingCode);

            if (_filteredOutput.Count > 0)
            {
                // Save output before clear it
                string commandOutput = _filteredOutput.Last();
                _filteredOutput.Clear();

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

        /// <summary>
        /// Performs backup operations.
        /// </summary>
        /// <param name="extractDevice">The device to make backup from.</param>
        /// <param name="backupDevice">The device to make backup from.</param>
        /// <returns>True if operation is successful, otherwise false.</returns>
        internal static bool PerformBackup(Device extractDevice, Device backupDevice)
        {
            bool result = false;

            CreateProgramFolders();

            return result;
        }

        /// <summary>
        /// Handles connection closing.
        /// </summary>
        internal static void CloseConnection()
        {
            SendCommand("adb kill-server");
            _commandPromptProcess.Close();
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                                COMMANDS                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes command prompt process in PlatformTools folder.
        /// </summary>
        private static void InitializeCommandPrompt()
        {
            _commandPromptProcess.StartInfo.CreateNoWindow = true;
            _commandPromptProcess.StartInfo.FileName = "cmd.exe";
            _commandPromptProcess.StartInfo.RedirectStandardInput = true;
            _commandPromptProcess.StartInfo.RedirectStandardOutput = true;
            _commandPromptProcess.StartInfo.RedirectStandardError = true;
            _commandPromptProcess.StartInfo.UseShellExecute = false;
            _commandPromptProcess.StartInfo.WorkingDirectory = Utils.ProgramFolders.PlatformToolsDirectory;

            // Add event to read received output data
            // Exclude command sent by user
            _commandPromptProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    if (!e.Data.Contains("adb "))
                    {
                        _filteredOutput.Add(e.Data);
                    }
                    _rawOutput.AppendLine(e.Data);
                }
            };

            // Add event to read received errors
            _commandPromptProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    _rawOutput.AppendLine(e.Data);
                }
            };

            // Start process
            _commandPromptProcess.Start();
            _commandPromptProcess.BeginOutputReadLine();
            _commandPromptProcess.BeginErrorReadLine();

            _isCommandPromptInitialized = true;
        }

        /// <summary>
        /// Starts ADB server.
        /// </summary>
        private static void StartServer()
        {
            SendCommand("adb start-server");
            _isServerRunning = true;
            _filteredOutput.Clear();
        }

        /// <summary>
        /// Executes a command then waits a time according to sent command.<br/>
        /// If response parameter is specified, replies with its value.
        /// </summary>
        private static void SendCommand(string commandString, Utils.WAITING_TIME waitingTime = Utils.WAITING_TIME.DEFAULT, string responseString = null)
        {
            #region "Calculate waiting time"

            if (string.IsNullOrWhiteSpace(responseString))
            {
                // If a response is not needed and waiting time is not specified, set it according to sent command
                if (waitingTime.Equals(Utils.WAITING_TIME.DEFAULT))
                {
                    if (commandString.Contains("devices"))
                    {
                        waitingTime = Utils.WAITING_TIME.LONG_SCAN;
                    }
                    else if (commandString.Contains("start-server"))
                    {
                        waitingTime = Utils.WAITING_TIME.START_SERVER;
                    }
                    else if (commandString.Contains("ro.product.model"))
                    {
                        waitingTime = Utils.WAITING_TIME.GET_NAME;
                    }
                }
            }
            else
            {
                // If a response is needed, set waiting to default value
                waitingTime = Utils.WAITING_TIME.DEFAULT;
            }

            #endregion

            if (_isCommandPromptInitialized)
            {
                if (!_commandPromptProcess.HasExited)
                {
                    // Write command
                    _commandPromptProcess.StandardInput.WriteLine(commandString);
                    _commandPromptProcess.StandardInput.Flush();

                    if (!string.IsNullOrWhiteSpace(responseString))
                    {
                        // Write response
                        _commandPromptProcess.StandardInput.WriteLine(responseString);
                        _commandPromptProcess.StandardInput.Flush();
                    }

                    // Wait for response
                    Task.Delay((int)waitingTime).Wait();
                }
                else
                {
                    throw new CommandPromptException("Command prompt process is not active anymore!");
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                               UTILITIES                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        private static void CreateProgramFolders()
        {
            //_programFilesFolder = $"{_currentDirectory}\\Program files";

            //if (!Directory.Exists(_programFilesFolder))
            //{
            //    Directory.CreateDirectory(_programFilesFolder);
            //}

            //_unlimitedDeviceFolder = $"{_programFilesFolder}\\";
        }

    }
}
