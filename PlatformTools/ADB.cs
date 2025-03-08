using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlatformTools
{
    public class ADB
    {
        #region "Constants"

        private const int WAITING_INTERVAL = 100;

        #endregion

        #region "Private variables"

        private static List<string> _rawOutput = new List<string>();
        private static List<string> _output = new List<string>();

        #endregion

        /// <summary>
        /// Checks platform tools then initializes ADB connection.
        /// </summary>
        /// <returns>When async operation is completed, a bool describing operation result:<br/>
        /// True if successful, otherwise false.
        /// </returns>
        public static async Task<bool> Initialize()
        {
            bool operationResult = false;

            try
            {
                operationResult = Utilities.CheckPlatformTools();
                
                if (operationResult)
                {
                    await ExecuteCommand("adb start-server");
                }
            }
            catch (Exception exception)
            {
                operationResult = false;

                MessageBox.Show(
                    $"[Initialize]\n" +
                    $"{exception.Message}\n"
                    );
            }
            
            return operationResult;
        }

        /// <summary>
        /// Scans connected devices.<br/>
        /// Timeout currently set to 5 seconds.
        /// </summary>
        /// <returns>List containing connected devices.</returns>
        public static async Task<List<Device>> ScanDevices()
        {
            List<Device> foundDevices = new List<Device>();

            try
            {
                // Clear previous output
                _output.Clear();

                using (Process _adbProcess = new Process())
                {
                    #region "Create command prompt process"

                    _adbProcess.StartInfo.CreateNoWindow = true;
                    _adbProcess.StartInfo.FileName = "cmd.exe";
                    _adbProcess.StartInfo.RedirectStandardInput = true;
                    _adbProcess.StartInfo.RedirectStandardOutput = true;
                    _adbProcess.StartInfo.RedirectStandardError = true;
                    _adbProcess.StartInfo.UseShellExecute = false;
                    _adbProcess.StartInfo.WorkingDirectory = Utilities.PlatformToolsDir;

                    // Add event to read received output data
                    _adbProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            _rawOutput.Add(e.Data);
                            Utilities.WriteOutput(e.Data, ref _output);
                        }
                    };

                    #endregion

                    // Start process
                    _adbProcess.Start();
                    _adbProcess.StandardInput.WriteLine("echo off");
                    _adbProcess.BeginOutputReadLine();

                    #region "Execute command"

                    await Task.Run(async () =>
                    {
                        _adbProcess.StandardInput.WriteLine("adb devices");
                        _adbProcess.StandardInput.Flush();

                        // Wait for scan to be completed
                        // Maximum waiting time for this operation is 5 seconds
                        int waitedTime = 0;
                        while (_output.Count.Equals(0))
                        {
                            await Task.Delay(WAITING_INTERVAL);
                            waitedTime += WAITING_INTERVAL;
                            if (waitedTime >= 5000)
                            {
                                break;
                            }
                        }

                        _adbProcess.StandardInput.WriteLine("exit");
                    });

                    #endregion

                    // Waiting process exit
                    await Task.Run(() => _adbProcess.WaitForExit());

                    // Copy output in order to execute other commands in this function
                    List<string> tempOutput = new List<string>(_output);

                    // Create device object for each scannned one
                    foreach (var scannedDevice in tempOutput)
                    {
                        Device device = new Device(scannedDevice);
                        
                        if (device.AuthStatus.Equals(Enums.DeviceAuthStatus.AUTHORIZED))
                        {
                            // Get device model
                            // First send command, then read last output line.
                            await ExecuteCommand($"adb -s {device.Id} shell getprop ro.product.model");
                            device.Model = _output.Last();
                        }

                        foundDevices.Add(device);
                    }

#if DEBUG
                    // If no device in debug mode, add some fake devices
                    if (foundDevices.Count.Equals(0))
                    {
                        int fakeDevicesCount = 3;
                        for (int i = 1; i <= fakeDevicesCount; i++)
                        {
                            foundDevices.Add(new Device($"Device {foundDevices.Count + 1}\tdevice"));
                        }
                    }
#endif
                }
            }
            catch (Exception exception)
            {
                foundDevices.Clear();

                MessageBox.Show(
                    $"[ScanDevices]\n" +
                    $"{exception.Message}\n"
                    );
            }

            return foundDevices;
        }

        /// <summary>
        /// Tries to authorize a device given its ID.
        /// </summary>
        /// <param name="deviceIdentifier">Device ID.</param>
        /// <returns>True if authorization successful, otherwise false.</returns>
        public static async Task<bool> AuthorizeDevice(string deviceIdentifier)
        {
            bool authResult = false;

            try
            {
                // Restart server to permit to show popup on device
                await ExecuteCommand("adb kill-server");
                await ExecuteCommand("adb start-server");

                // Show message waiting for authorization
                MessageBox.Show($"Please authorize this computer via the popup displayed on device screen (ID = {deviceIdentifier}), then click OK!");

                // Send devices command to check device new auth status
                await ExecuteCommand("adb devices");

                // Output should contain only connected devices
                // If output contains device id and "device" string it means it's authorized
                authResult = _output.Any(str => str.Contains(deviceIdentifier) && str.Contains("device"));
            }
            catch (Exception exception)
            {
                authResult = false;

                MessageBox.Show(
                    $"[AuthorizeDevice]\n" +
                    $"{exception.Message}\n"
                    );
            }

            return authResult;
        }

        /// <summary>
        /// Connects to a device via Wireless ADB through its IP and port.
        /// </summary>
        /// <param name="deviceIp">Device IP.</param>
        /// <param name="devicePort">Device port.</param>
        /// <returns>True if connection successful, otherwise false.</returns>
        public static async Task<bool> ConnectWirelessDevice(string deviceIp, string devicePort)
        {
            bool connectionResult = false;

            try
            {
                await ExecuteCommand($"adb connect {deviceIp}:{devicePort}");

                if (_output.Count > 0)
                {
                    // Save output before clear it
                    string commandOutput = _output.Last();

                    // Check received output
                    if (!string.IsNullOrWhiteSpace(commandOutput) && commandOutput.Contains("connected"))
                    {
                        connectionResult = true;
                    }
                }
            }
            catch (Exception exception)
            {
                connectionResult = false;

                MessageBox.Show(
                    $"[ConnectWirelessDevice]\n" +
                    $"{exception.Message}\n"
                    );
            }

            return connectionResult;
        }

        /// <summary>
        /// Pair a device via Wireless ADB through its device IP, port and pairing code.
        /// </summary>
        /// <param name="deviceIp">Device IP.</param>
        /// <param name="devicePort">Device port.</param>
        /// <param name="devicePairingCode">Device pairing code.</param>
        /// <returns>True if pairing is successful, otherwise false.</returns>
        public static async Task<bool> PairWirelessDevice(string deviceIp, string devicePort, string devicePairingCode)
        {
            bool pairingResult = false;

            try
            {
                // Send pairing command
                await ExecuteCommand($"adb pair {deviceIp}:{devicePort}", devicePairingCode);

                if (_output.Count > 0 && _output.Last().Contains("paired"))
                {
                    pairingResult = true;
                }
            }
            catch (Exception exception)
            {
                pairingResult = false;

                MessageBox.Show(
                    $"[PairWirelessDevice]\n" +
                    $"{exception.Message}\n"
                    );
            }

            return pairingResult;
        }

        /// <summary>
        /// Kills ADB server.
        /// </summary>
        /// <returns>When async operation is completed, a task object.</returns>
        public static async Task KillServer()
        {
            await ExecuteCommand("adb kill-server");
        }

        #region "Private functions"

        /// <summary>
        /// Executes an ADB command.<br/>
        /// If set, sends also response.
        /// </summary>
        /// <param name="command">Command to be executed.</param>
        /// <param name="response">[OPTIONAL] Response to be given.</param>
        /// <returns>When async operation is completed, a task object.</returns>
        private static async Task ExecuteCommand(string command, string response = "")
        {
            try
            {
                // Clear previous output
                _output.Clear();

                using (Process _adbProcess = new Process())
                {
                    #region "Create command prompt process"

                    _adbProcess.StartInfo.CreateNoWindow = true;
                    _adbProcess.StartInfo.FileName = "cmd.exe";
                    _adbProcess.StartInfo.RedirectStandardInput = true;
                    _adbProcess.StartInfo.RedirectStandardOutput = true;
                    _adbProcess.StartInfo.RedirectStandardError = true;
                    _adbProcess.StartInfo.UseShellExecute = false;
                    _adbProcess.StartInfo.WorkingDirectory = Utilities.PlatformToolsDir;

                    // Add event to read received output data
                    _adbProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            _rawOutput.Add(e.Data);
                            Utilities.WriteOutput(e.Data, ref _output);
                        }
                    };

                    #endregion

                    // Start process
                    _adbProcess.Start();
                    _adbProcess.StandardInput.WriteLine("echo off");
                    _adbProcess.BeginOutputReadLine();

                    #region "Execute command"

                    await Task.Run(async () =>
                    {
                        _adbProcess.StandardInput.WriteLine(command);
                        _adbProcess.StandardInput.Flush();

                        // Wait for operation to be completed according to sent command
                        await Task.Delay(Utilities.GetWaitingTime(command));

                        // If a response is set then write it into command prompt
                        if (response != null)
                        {
                            _adbProcess.StandardInput.WriteLine(response);
                            _adbProcess.StandardInput.Flush();

                            await Task.Delay(Utilities.GetWaitingTime(command));
                        }

                        _adbProcess.StandardInput.WriteLine("exit");
                    });

                    #endregion

                    // Waiting process exit
                    await Task.Run(() => _adbProcess.WaitForExit());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"[SendCommand]\n" +
                    $"{ex.Message}\n"
                    );
            }
        }

        #endregion
    }
}

/*

using AndroidDeviceManager.Model.Exceptions;
using AndroidDeviceManager.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace AndroidDeviceManager.Functions
{
    internal static class ADB
    {
        #region "Variables       

        private static List<Device> _connectedDevices = new List<Device>();
        

        #endregion

        #region "Getters and setters"

        internal static List<Device> ConnectedDevices { get => _connectedDevices; set => _connectedDevices = value; }
        internal static string RawOutput { get => string.Join("\n", _rawOutput); }

        #endregion

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                               FUNCTIONS                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        

        
        /// <summary>
        /// Scans connected devices and populates <see cref="ConnectedDevices"/> list.
        /// </summary>
        /// <exception cref="PlatformToolsProcessException"></exception>
        internal static async Task ScanDevices()
        {
            try
            {
                // Clear previous devices
                ConnectedDevices.Clear();

                // Send request devices command
                await ExecuteCommand("adb devices");

                if (_filteredOutput.Count() > 0)
                {
                    List<string> _filteredOutputCopy = new List<string>(_filteredOutput);

                    foreach (string deviceLine in _filteredOutputCopy)
                    {
                        bool addDevice = true;

                        #region "Get device info"

                        // Split device line into parts
                        List<string> lineParts = deviceLine.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                        // Get device identifier
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
                            await ExecuteCommand($"adb -s {deviceIdentifier} shell getprop ro.product.model");

                            // If it's a valid name, assing it
                            deviceName = _filteredOutput.Count > 0 ? _filteredOutput.Last() : deviceIdentifier;

                            foreach (Device connectedDevice in _connectedDevices)
                            {
                                // If devices have same name but different identifier
                                // Then they are different devices
                                // So add identifier to their names
                                if (connectedDevice.Name == deviceName && !connectedDevice.ID.Contains(deviceIdentifier))
                                {
                                    connectedDevice.Name = $"{connectedDevice.Name} - {connectedDevice.ID}";
                                    deviceName = $"{deviceName} - {deviceIdentifier}";
                                }

                                // If devices have same identifier
                                // Then they are the same devices connected both via WiFi and USB
                                // So keep the device with USB identifier
                                if (connectedDevice.ID.Contains(deviceIdentifier) || connectedDevice.ID.Equals(deviceIdentifier))
                                {
                                    // Devices should not be added because it already exists
                                    addDevice = false;
                                    connectedDevice.ID = deviceIsWirelessConnected ? connectedDevice.ID : deviceIdentifier;
                                }
                            }

                            // With device real name, check if it has unlimited backup
                            deviceHasUnlimitedBackup = Utils.UnlimitedBackupDevices.Any(unlimitedBackupDeviceName => deviceName.Contains(unlimitedBackupDeviceName));
                        }

                        #endregion

                        if (addDevice)
                        {
                            Device device = new Device(deviceName, deviceIdentifier, deviceAuthorizationStatus, deviceHasUnlimitedBackup, deviceIsWirelessConnected);
                            _connectedDevices.Add(device);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Clear previous devices
                ConnectedDevices.Clear();
                throw;
            }
        }

        /// <summary>
        /// Tries to authorize device with parameter identifier.
        /// </summary>
        /// <param name="deviceIdentifier">Identifier of the device to be authorized.</param>
        /// <returns>True if device is authorized, otherwise false.</returns>
        /// <exception cref="PlatformToolsProcessException"></exception>
        internal static async Task<bool> AuthorizeDevice(string deviceIdentifier)
        {
            // Restart server to permit to show popup on device
            await ExecuteCommand("adb kill-server");
            await StartServer();

            // Show message waiting for authorization
            Utils.ShowMessageDialog(
                $"Please authorize this computer via the popup displayed on device screen (ID = {deviceIdentifier}), then click OK!"
            );

            // Send devices command to check device new auth status
            await ExecuteCommand("adb devices");

            // Output should contain only connected devices
            // If output contains device id and "device" string it means it's authorized
            return _filteredOutput.Any(str => str.Contains(deviceIdentifier) && str.Contains("device"));
        }

        /// <summary>
        /// Connects to a device via wireless ADB.
        /// </summary>
        /// <returns>True if connection is successful, otherwise false.</returns>
        /// /// <exception cref="PlatformToolsProcessException"></exception>
        internal static async Task<bool> ConnectWirelessDevice(string deviceIp, string devicePort)
        {
            await ExecuteCommand($"adb connect {deviceIp}:{devicePort}");

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
        /// /// <exception cref="PlatformToolsProcessException"></exception>
        internal static async Task<bool> PairWirelessDevice(string deviceIp, string devicePort, string devicePairingCode)
        {
            // Send pairing command
            await ExecuteCommand($"adb pair {deviceIp}:{devicePort}", devicePairingCode);

            if (_filteredOutput.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(_filteredOutput.Last()) && _filteredOutput.Last().Contains("paired"))
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
        /// Executes a push command in ADB command prompt.
        /// </summary>
        /// <returns>Pushed files count.</returns>
        /// <exception cref="PlatformToolsProcessException"></exception>
        /// <exception cref="PlatformToolsTransferException"></exception>
        internal static async Task<int> ExecutePushCommand(string destinationDeviceIdentifier, string destinationDeviceFolder, string sourceFolderPath)
        {
            _filteredOutput.Clear();

            int skippedFiles = 0;

            // Count total files in parent and children directories to be transferred
            var totalFilesCount = Directory.GetFiles(sourceFolderPath, "*", SearchOption.AllDirectories).Length;

            // Transfer files
            string command = $"adb -s {destinationDeviceIdentifier} push \"{sourceFolderPath}\" \"{destinationDeviceFolder}\"";

            using (Process _adbProcess = new Process())
            {
                _adbProcess.StartInfo.CreateNoWindow = true;
                _adbProcess.StartInfo.FileName = "cmd.exe";
                _adbProcess.StartInfo.RedirectStandardInput = true;
                _adbProcess.StartInfo.RedirectStandardOutput = true;
                _adbProcess.StartInfo.RedirectStandardError = true;
                _adbProcess.StartInfo.UseShellExecute = false;
                _adbProcess.StartInfo.WorkingDirectory = Utils.ProgramFolders.PlatformToolsDirectory;

                // Add event to read received output data
                _adbProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        _rawOutput.Add(e.Data);
                    }
                };

                // Add event to read received errors
                _adbProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        _rawOutput.Add(e.Data);

                        // Exclude sent commands
                        if (e.Data.Contains("pushed") && e.Data.Contains("skipped"))
                        {
                            _filteredOutput.Add(e.Data);
                        }
                    }
                };

                // Start process
                _adbProcess.Start();
                _adbProcess.StandardInput.WriteLine("echo off");
                _adbProcess.BeginOutputReadLine();
                _adbProcess.BeginErrorReadLine();

                // Send parameter command
                _adbProcess.StandardInput.WriteLine(command);
                _adbProcess.StandardInput.Flush();

                // Wait operation to be completed asynchronously
                while (!_filteredOutput.Count.Equals(1))
                {
                    await Task.Delay(50);

#if DEBUG
                    Debug.WriteLine("Transfer in progress...");
#endif
                }

                #region "Check copied files count"

                var lastLine = _filteredOutput.Last();

                if (lastLine.Contains("pushed"))
                {
                    const string START_PATTERN = ", ";
                    int start = lastLine.IndexOf(START_PATTERN) + START_PATTERN.Length;

                    const string END_PATTERN = " skipped";
                    int end = lastLine.IndexOf(END_PATTERN);

                    string skippedFilesString = lastLine.Substring(start, end - start);
                    if (!int.TryParse(skippedFilesString, out skippedFiles))
                    {
                        throw new PlatformToolsTransferException("ExecuteCommand(): Error parsing transferred files count.");
                    }
                }
                else
                {
                    throw new PlatformToolsTransferException("ExecuteCommand(): Error reading output last line.");
                }

                #endregion

                // Send exit command
                _adbProcess.StandardInput.WriteLine("exit");

                // Waiting process exit
                await Task.Run(() => _adbProcess.WaitForExit());

                // Return according to process exit code
                if (!_adbProcess.ExitCode.Equals(0))
                {
                    throw new PlatformToolsProcessException("ExecuteCommand(): Process exit code 1.");
                }
                else
                {
                    return totalFilesCount - skippedFiles;
                }
            }
        }

        /// <summary>
        /// Executes a pull command in ADB command prompt.
        /// </summary>
        /// <returns>Pulled files count.</returns>
        /// <exception cref="PlatformToolsProcessException"></exception>
        /// <exception cref="PlatformToolsTransferException"></exception>
        internal static async Task<int> ExecutePullCommand(Device sourceDevice, List<string> sourceDeviceFolders, bool unlimitedBackupProcess = false)
        {
            int totalFilesCount = 0;

            // Sometimes device ID contains invalid characters
            string oldDeviceIdentifier = sourceDevice.ID;
            sourceDevice.ID = sourceDevice.ID.Contains(":") ? sourceDevice.ID.Replace(":", ".") : sourceDevice.ID;

            if (unlimitedBackupProcess)
            {
                Utils.CreateUnlimitedBackupProgramFolders(sourceDevice);
            }
            else
            {
                Utils.CreateProgramFolders(sourceDevice);
            }

            sourceDevice.ID = oldDeviceIdentifier;
            foreach (string sourceDeviceFolder in sourceDeviceFolders)
            {
                totalFilesCount += await ExecutePullCommand(sourceDevice.ID, sourceDeviceFolder, unlimitedBackupProcess);
            }

            return totalFilesCount;
        }

        /// <summary>
        /// Deletes Camera folder from target device.
        /// </summary>
        /// <param name="destionationDeviceIdentifier">Target device ID.</param>
        /// <returns></returns>
        /// <exception cref="PlatformToolsProcessException"></exception>
        internal static async Task ExecuteDeleteCameraCommand(string destionationDeviceIdentifier)
        {
            string command = $"adb -s {destionationDeviceIdentifier} shell \"rm -r /sdcard/DCIM/Camera\"";
            await ExecuteCommand(command);
        }

        /// <summary>
        /// Scans applications on device.
        /// </summary>
        /// <param name="deviceIdentifier">ID of the device to be scanned.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item>Item 1 is all device apps.</item>
        /// <item>Item 2 is device system apps.</item>
        /// <item>Item 3 is device third party apps.</item>
        /// </list>
        /// </returns>
        /// <exception cref="PlatformToolsProcessException"></exception>
        internal static async Task<Tuple<List<string>, List<string>, List<string>>> GetApplications(string deviceIdentifier)
        {
            List<string> allApps = new List<string>();
            List<string> systemApps = new List<string>();
            List<string> thirdPartyApps = new List<string>();

            const string START_PATTERN = "package:";
            int startIndex;
            string packageName;

            // Get all apps
            await ExecuteCommand($"adb -s {deviceIdentifier} shell pm list packages");
            if (_filteredOutput.Count > 0)
            {
                foreach (string allApp in _filteredOutput)
                {
                    startIndex = allApp.IndexOf(START_PATTERN) + START_PATTERN.Length;
                    packageName = allApp.Substring(startIndex);
                    allApps.Add(packageName);
                }
            }

            // Get only system apps
            await ExecuteCommand($"adb -s {deviceIdentifier} shell pm list packages -s");
            if (_filteredOutput.Count > 0)
            {
                foreach (string systemApp in _filteredOutput)
                {
                    startIndex = systemApp.IndexOf(START_PATTERN) + START_PATTERN.Length;
                    packageName = systemApp.Substring(startIndex);
                    systemApps.Add(packageName);
                }
            }

            // Get only 3d party packages
            await ExecuteCommand($"adb -s {deviceIdentifier} shell pm list packages -3");
            if (_filteredOutput.Count > 0)
            {
                foreach (string thirdPartyApp in _filteredOutput)
                {
                    startIndex = thirdPartyApp.IndexOf(START_PATTERN) + START_PATTERN.Length;
                    packageName = thirdPartyApp.Substring(startIndex);
                    thirdPartyApps.Add(packageName);
                }
            }

            return new Tuple<List<string>, List<string>, List<string>>(allApps, systemApps, thirdPartyApps);
        }

        /// <summary>
        /// Uninstall an app on target device.
        /// </summary>
        /// <param name="deviceIdentifier">Target device ID.</param>
        /// <param name="packageName">Name of the package to uninstall.</param>
        /// <returns>True if uninstalled successfully, otherwise false.</returns>
        /// <exception cref="PlatformToolsProcessException"></exception>
        internal static async Task<bool> UninstallApp(string deviceIdentifier, string packageName)
        {
            bool result = false;

            await ExecuteCommand($"adb -s {deviceIdentifier} shell pm uninstall -k --user 0 {packageName}");

            if (_filteredOutput.Count > 0)
            {
                result = _filteredOutput.Last().Equals("Success") ? true : false;
            }

            return result;
        }

        ////////////////////////////////////////////////////////////////////////
        //                                                                    //
        //                                COMMANDS                            //
        //                                                                    //
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Executes a command in ADB command prompt.
        /// </summary>
        /// <param name="command">Command to be executed.</param>
        /// <param name="response">Response to be given.</param>
        /// <exception cref="PlatformToolsProcessException"></exception>
        private static async Task ExecuteCommand(string command, string response = null)
        {
            _filteredOutput.Clear();

            using (Process _adbProcess = new Process())
            {
                _adbProcess.StartInfo.CreateNoWindow = true;
                _adbProcess.StartInfo.FileName = "cmd.exe";
                _adbProcess.StartInfo.RedirectStandardInput = true;
                _adbProcess.StartInfo.RedirectStandardOutput = true;
                _adbProcess.StartInfo.RedirectStandardError = true;
                _adbProcess.StartInfo.UseShellExecute = false;
                _adbProcess.StartInfo.WorkingDirectory = Utils.ProgramFolders.PlatformToolsDirectory;

                // Add event to read received output data
                _adbProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        _rawOutput.Add(e.Data);

                        // Exclude sent commands
                        if (!e.Data.Contains("adb ") && !e.Data.Contains("attached") && !e.Data.Contains("Microsoft") && !e.Data.Contains("PlatformTools") && !e.Data.Contains("exit"))
                        {
                            _filteredOutput.Add(e.Data);
                        }
                    }
                };

                // Start process
                _adbProcess.Start();
                _adbProcess.StandardInput.WriteLine("echo off");
                _adbProcess.BeginOutputReadLine();
                _adbProcess.BeginErrorReadLine();

                // Wait for command to be executed
                await Task.Run(async () =>
                {
                    // Send parameter command
                    _adbProcess.StandardInput.WriteLine(command);
                    _adbProcess.StandardInput.Flush();

                    // Wait operation to be completed
                    await Task.Delay(Utils.CalculateWaitingTime(command));

                    if (response != null)
                    {
                        _adbProcess.StandardInput.WriteLine(response);
                        _adbProcess.StandardInput.Flush();

                        await Task.Delay(Utils.CalculateWaitingTime(command));
                    }

                    // Send exit command
                    _adbProcess.StandardInput.WriteLine("exit");
                });

                // Waiting process exit
                await Task.Run(() => _adbProcess.WaitForExit());

                // Return according to process exit code
                if (!_adbProcess.ExitCode.Equals(0))
                {
                    throw new PlatformToolsProcessException("ExecuteCommand(): Process exit code 1");
                }
            }
        }

        /// <summary>
        /// Starts ADB server.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="PlatformToolsProcessException"></exception>
        private static async Task StartServer()
        {
            await ExecuteCommand("adb start-server");
        }

        /// <summary>
        /// Executes a pull command in ADB command prompt.
        /// </summary>
        /// <returns>Pulled files count.</returns>
        /// <exception cref="PlatformToolsProcessException"></exception>
        /// <exception cref="PlatformToolsTransferException"></exception>
        private static async Task<int> ExecutePullCommand(string sourceDeviceIdentifier, string sourceDeviceFolder, bool unlimitedBackupProcess = false)
        {
            _filteredOutput.Clear();

            List<string> splittedSourceDeviceFolder = sourceDeviceFolder.Split(new char[] { '\\', '/' }).Where
                    (splitted =>
                        !string.IsNullOrWhiteSpace(splitted) &&
                        !splitted.Contains("storage") &&
                        !splitted.Contains("emulated") &&
                        !splitted.Contains("0")
                    ).ToList();

            splittedSourceDeviceFolder.RemoveAt(splittedSourceDeviceFolder.Count() - 1);
            string destinationPath = string.Join("/", splittedSourceDeviceFolder);

            string localDestinationFolderPath = string.Empty;

            // If it's a unlimited backup process, set right folders
            if (unlimitedBackupProcess)
            {
                localDestinationFolderPath = Path.Combine(Utils.ProgramFolders.UnlimitedBackupDeviceDirectory, destinationPath).Replace('\\', '/');
            }
            else
            {
                localDestinationFolderPath = Path.Combine(Utils.ProgramFolders.ExtractDeviceDirectory, destinationPath).Replace('\\', '/');
            }

            Directory.CreateDirectory(localDestinationFolderPath);

            string command = $"adb -s {sourceDeviceIdentifier} pull \"{sourceDeviceFolder}\" \"{localDestinationFolderPath}\"";

            using (Process _adbProcess = new Process())
            {
                _adbProcess.StartInfo.CreateNoWindow = true;
                _adbProcess.StartInfo.FileName = "cmd.exe";
                _adbProcess.StartInfo.RedirectStandardInput = true;
                _adbProcess.StartInfo.RedirectStandardOutput = true;
                _adbProcess.StartInfo.RedirectStandardError = true;
                _adbProcess.StartInfo.UseShellExecute = false;
                _adbProcess.StartInfo.WorkingDirectory = Utils.ProgramFolders.PlatformToolsDirectory;

                // Add an event to read received output data
                _adbProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        _rawOutput.Add(e.Data);
                    }
                };

                // Add an event to read received errors
                _adbProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        _rawOutput.Add(e.Data);

                        // Filter output data
                        if (e.Data.Contains("pulled") && e.Data.Contains("skipped"))
                        {
                            _filteredOutput.Add(e.Data);
                        }
                    }
                };

                // Start the process
                _adbProcess.Start();
                _adbProcess.StandardInput.WriteLine("echo off");
                _adbProcess.BeginOutputReadLine();
                _adbProcess.BeginErrorReadLine();

                // Send command with specified parameters
                _adbProcess.StandardInput.WriteLine(command);
                _adbProcess.StandardInput.Flush();

                // Wait asynchronously for the operation to complete
                while (!_filteredOutput.Count.Equals(1))
                {
                    await Task.Delay(50);

#if DEBUG
                    Debug.WriteLine("Extracting in progress...");
#endif
                }

                #region "Check copied files count"

                int pulledFiles = 0;
                int skippedFiles = 0;
                var lastLine = _filteredOutput.Last();

                if (lastLine.Contains("pulled"))
                {
                    // Extract pulled files count
                    const string START_PATTERN_3 = ": ";
                    const string END_PATTERN_3 = " files";
                    int start_3 = lastLine.IndexOf(START_PATTERN_3) + START_PATTERN_3.Length;
                    int end_3 = lastLine.IndexOf(END_PATTERN_3);
                    string pulledFilesString = lastLine.Substring(start_3, end_3 - start_3);

                    const string START_PATTERN_4 = ", ";
                    const string END_PATTERN_4 = " skipped";
                    int start_4 = lastLine.IndexOf(START_PATTERN_4) + START_PATTERN_4.Length;
                    int end_4 = lastLine.IndexOf(END_PATTERN_4);
                    string skippedFilesString = lastLine.Substring(start_4, end_4 - start_4);

                    if (!int.TryParse(pulledFilesString, out pulledFiles) && !int.TryParse(skippedFilesString, out skippedFiles))
                    {
                        throw new PlatformToolsTransferException("ExecutePullCommand(): Error parsing pulled files count.");
                    }
                }
                else
                {
                    throw new PlatformToolsTransferException("ExecutePullCommand(): Error reading output last line.");
                }

                #endregion

                // Send exit command
                _adbProcess.StandardInput.WriteLine("exit");

                // Wait for process to exit
                await Task.Run(() => _adbProcess.WaitForExit());

                // Return based on process exit code
                if (!_adbProcess.ExitCode.Equals(0))
                {
                    throw new PlatformToolsProcessException("ExecutePullCommand(): Process exit code 1.");
                }
                else
                {
                    return pulledFiles - skippedFiles;
                }
            }
        }

    }
}

*/