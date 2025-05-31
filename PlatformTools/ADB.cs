using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlatformTools
{
    public class ADB
    {
        #region "Private variables"

        private static List<string> _rawOutput;
        private static List<string> _output;
        private Utilities _utilities = new Utilities();

        #endregion

        /// <summary>
        /// Constructor of the class.
        /// </summary>
        /// <param name="rawOutput">Reference to raw output string list.</param>
        /// <param name="output">Reference to output string list.</param>
        public ADB(ref List<string> rawOutput, ref List<string> output)
        {
            _rawOutput = rawOutput;
            _output = output;
        }

        /// <summary>
        /// Initializes ADB connection.<br/>
        /// Handles exceptions returning false.
        /// </summary>
        /// <returns>True if OK, otherwise false.</returns>
        public async Task<bool> Initialize()
        {
            bool operationResult = true;

            try
            {
                _utilities.CheckPlatformTools();
                await ExecuteCommand("adb start-server");
            }
            catch (Exception ex)
            {
                operationResult = false;
                MessageBox.Show
                (
                    $"Error during program initialize! Error details:\n\n" +
                    $"{ex.Message}"
                );
            }

            return operationResult;
        }

        /// <summary>
        /// Executes an ADB command and, if set, sends a response.<br/>
        /// Throws exception if operation fails.
        /// </summary>
        /// <param name="command">Command to be executed.</param>
        /// <param name="response">[OPTIONAL] Response to be given.</param>
        private async Task ExecuteCommand(string command, string response = "")
        {
            try
            {
                _output.Clear();

                using (Process _adbProcess = new Process())
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource())
                    {
                        #region "Create process"

                        _adbProcess.StartInfo.CreateNoWindow = true;
                        _adbProcess.StartInfo.FileName = "cmd.exe";
                        _adbProcess.StartInfo.RedirectStandardInput = true;
                        _adbProcess.StartInfo.RedirectStandardOutput = true;
                        _adbProcess.StartInfo.RedirectStandardError = true;
                        _adbProcess.StartInfo.UseShellExecute = false;
                        _adbProcess.StartInfo.WorkingDirectory = Constants.PATHS.PLATFORM_TOOLS_DIR;

                        _adbProcess.OutputDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrWhiteSpace(e.Data))
                            {
                                _rawOutput.Add(e.Data);
                                _utilities.WriteOutput(e.Data, ref _output);
                            }
                        };

                        _adbProcess.Start();
                        _adbProcess.StandardInput.WriteLine("echo off");
                        _adbProcess.BeginOutputReadLine();

                        #endregion

                        #region "Run command"

                        _adbProcess.StandardInput.WriteLine(command);
                        _adbProcess.StandardInput.Flush();
                        await Task.Delay(_utilities.GetWaitingTime(command));

                        if (!string.IsNullOrEmpty(response))
                        {
                            _adbProcess.StandardInput.WriteLine(response);
                            _adbProcess.StandardInput.Flush();
                            await Task.Delay(_utilities.GetWaitingTime(command));
                        }

                        _adbProcess.StandardInput.WriteLine("exit");

                        #endregion

                        await Task.Run(() => _adbProcess.WaitForExit());
                    }
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Scans connected devices.<br/>
        /// Timeout set to <see cref="Constants.MISC.SCAN_DEVICES_TIMEOUT"/> value.<br/>
        /// Handles exceptions returning an empty list.
        /// </summary>
        /// <returns>List containing connected devices.</returns>
        public async Task<List<Device>> ScanDevices()
        {
            List<Device> foundDevices = new List<Device>();

            try
            {
                // Clear previous output
                _output.Clear();

                using (Process _adbProcess = new Process())
                {
                    #region "Create process"

                    _adbProcess.StartInfo.CreateNoWindow = true;
                    _adbProcess.StartInfo.FileName = "cmd.exe";
                    _adbProcess.StartInfo.RedirectStandardInput = true;
                    _adbProcess.StartInfo.RedirectStandardOutput = true;
                    _adbProcess.StartInfo.RedirectStandardError = true;
                    _adbProcess.StartInfo.UseShellExecute = false;
                    _adbProcess.StartInfo.WorkingDirectory = Constants.PATHS.PLATFORM_TOOLS_DIR;

                    // Add event to read received output data
                    _adbProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            _rawOutput.Add(e.Data);
                            _utilities.WriteOutput(e.Data, ref _output);
                        }
                    };

                    // Start process
                    _adbProcess.Start();
                    _adbProcess.StandardInput.WriteLine("echo off");
                    _adbProcess.BeginOutputReadLine();

                    #endregion

                    #region "Execute command"

                    _adbProcess.StandardInput.WriteLine("adb devices");
                    _adbProcess.StandardInput.Flush();

                    // Wait for scan to be completed
                    // Maximum waiting time for this operation is 5 seconds
                    int waitedTime = 0;
                    while (_output.Count.Equals(0))
                    {
                        await Task.Delay(Constants.MISC.DEFAULT_WAITING_TIME);
                        waitedTime += Constants.MISC.DEFAULT_WAITING_TIME;
                        if (waitedTime >= Constants.MISC.SCAN_DEVICES_TIMEOUT)
                        {
                            break;
                        }
                    }

                    _adbProcess.StandardInput.WriteLine("exit");

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
                    foundDevices.Add(new Device($"Pixel 1\tdevice"));
                    foundDevices.Add(new Device($"Pixel 2 XL\tunauthorized"));
#endif
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show
                (
                    $"Error scanning devices! Error details:\n\n" +
                    $"{ex.Message}"
                );
            }

            return foundDevices;
        }

        /// <summary>
        /// Connects to a device via Wireless ADB through its IP and port.<br/>
        /// Handles exceptions returning false.
        /// </summary>
        /// <param name="deviceIp">Device IP.</param>
        /// <param name="devicePort">Device port.</param>
        /// <returns>True if connected, otherwise false.</returns>
        public async Task<bool> ConnectWirelessDevice(string deviceIp, string devicePort)
        {
            bool connectionResult = false;

            try
            {
                await ExecuteCommand($"adb connect {deviceIp}:{devicePort}");

                // Save and check output before clear it
                if (_output.Count > 0 && !string.IsNullOrWhiteSpace(_output.Last()) && _output.Last().Contains("connected"))
                {
                    connectionResult = true;
                }
            }
            catch (Exception ex)
            {
                connectionResult = false;

                MessageBox.Show
                (
                    $"Error connecting wireless device! Error details:\n\n" +
                    $"{ex.Message}"
                );
            }

            return connectionResult;
        }

        /// <summary>
        /// Pair a device via Wireless ADB through its device IP, port and pairing code.<br/>
        /// Handles exceptions returning false.
        /// </summary>
        /// <param name="deviceIp">Device IP.</param>
        /// <param name="devicePort">Device port.</param>
        /// <param name="devicePairingCode">Device pairing code.</param>
        /// <returns>True if paired, otherwise false.</returns>
        public async Task<bool> PairWirelessDevice(string deviceIp, string devicePort, string devicePairingCode)
        {
            bool pairingResult = false;

            try
            {
                await ExecuteCommand($"adb pair {deviceIp}:{devicePort}", devicePairingCode);

                // Save and check output before clear it
                if (_output.Count > 0 && !string.IsNullOrWhiteSpace(_output.Last()) && _output.Last().Contains("paired"))
                {
                    pairingResult = true;
                }
            }
            catch (Exception ex)
            {
                pairingResult = false;

                MessageBox.Show
                (
                    $"Error pairing wireless device! Error details:\n\n" +
                    $"{ex.Message}"
                );
            }

            return pairingResult;
        }

        /// <summary>
        /// Kills ADB server.
        /// </summary>
        public async Task KillServer()
        {
            try
            {
                await ExecuteCommand("adb kill-server");
            }
            catch (Exception ex)
            {
                MessageBox.Show
                (
                    $"Error killing ADB server! Error details:\n\n" +
                    $"{ex.Message}"
                );
            }
        }

        /// <summary>
        /// Tries to authorize a device given its ID.<br/>
        /// Handles exceptions returning false.
        /// </summary>
        /// <param name="deviceIdentifier">Device ID.</param>
        /// <returns>True if authorization successful, otherwise false.</returns>
        public async Task<bool> AuthorizeDevice(string deviceIdentifier)
        {
            bool authResult = false;

            try
            {
                // Restart server to permit to show popup on device
                await ExecuteCommand("adb kill-server");
                await ExecuteCommand("adb start-server");

                // Show message waiting for authorization
                MessageBox.Show($"Please authorize this computer via the popup displayed on device (ID = {deviceIdentifier}), then click OK!");

                // Send devices command to check device new auth status
                await ExecuteCommand("adb devices");

                // Output should contain only connected devices
                // If output contains device id and "device" string it means it's authorized
                authResult = _output.Any(str => str.Contains(deviceIdentifier) && str.Contains("device"));
            }
            catch (Exception ex)
            {
                authResult = false;

                MessageBox.Show
                (
                    $"Error authorizing device! Error details:\n\n" +
                    $"{ex.Message}"
                );
            }

            return authResult;
        }

        /// <summary>
        /// Executes a push command in ADB command prompt.<br/>
        /// Handles exceptions returning 0 values.
        /// </summary>
        /// <returns>
        /// Returns a tuple where:<br/>
        /// - Item 1 is files to be transferred count.<br/>
        /// - Item 2 is transferred files count.<br/>
        /// - Item 3 is skipped files count.
        /// </returns>
        private async Task<Tuple<int, int, int>> ExecutePushCommand(string destinationDeviceIdentifier, string destinationDeviceFolder, string folderToBeTransferred)
        {
            Tuple<int, int, int> operationResult = new Tuple<int, int, int>(0, 0, 0);
            int transferredFiles = 0;
            int skippedFiles = 0;
            int totalFiles = Directory.GetFiles(folderToBeTransferred, "*", SearchOption.AllDirectories).Length;

            try
            {
                if (!totalFiles.Equals(0))
                {
                    _output.Clear();

                    using (Process _adbProcess = new Process())
                    {
                        using (CancellationTokenSource cts = new CancellationTokenSource())
                        {
                            #region "Create process"

                            _adbProcess.StartInfo.CreateNoWindow = true;
                            _adbProcess.StartInfo.FileName = "cmd.exe";
                            _adbProcess.StartInfo.RedirectStandardInput = true;
                            _adbProcess.StartInfo.RedirectStandardOutput = true;
                            _adbProcess.StartInfo.RedirectStandardError = true;
                            _adbProcess.StartInfo.UseShellExecute = false;
                            _adbProcess.StartInfo.WorkingDirectory = Constants.PATHS.PLATFORM_TOOLS_DIR;

                            _adbProcess.OutputDataReceived += (sender, e) =>
                            {
                                if (!string.IsNullOrWhiteSpace(e.Data))
                                {
                                    _rawOutput.Add(e.Data);
                                    if (e.Data.Contains(Constants.PATTERNS.PUSHED_COMMAND_PATTERN) && e.Data.Contains(Constants.PATTERNS.SKIPPED_COMMAND_PATTERN))
                                    {
                                        _output.Add(e.Data);
                                    }
                                }
                            };

                            // Add event to read received errors
                            // Transferred files lines are errors ???
                            _adbProcess.ErrorDataReceived += (sender, e) =>
                            {
                                if (!string.IsNullOrWhiteSpace(e.Data))
                                {
                                    _rawOutput.Add(e.Data);
                                    if (e.Data.Contains(Constants.PATTERNS.PUSHED_COMMAND_PATTERN) && e.Data.Contains(Constants.PATTERNS.SKIPPED_COMMAND_PATTERN))
                                    {
                                        _output.Add(e.Data);
                                    }
                                }
                            };

                            _adbProcess.Start();
                            _adbProcess.StandardInput.WriteLine("echo off");
                            _adbProcess.BeginOutputReadLine();
                            _adbProcess.BeginErrorReadLine();

                            #endregion

                            #region "Run command"

                            string command = $"adb -s {destinationDeviceIdentifier} push \"{folderToBeTransferred}\" \"{destinationDeviceFolder}\"";

                            _adbProcess.StandardInput.WriteLine(command);
                            _adbProcess.StandardInput.Flush();

                            // When transferring files there are no written lines
                            // Only at the end of the process a text will appear
                            // Wait till that moment
                            while (!_output.Count.Equals(1))
                            {
                                await Task.Delay(Constants.MISC.DEFAULT_WAITING_TIME);
                            }
                            await Task.Delay(250);

                            // Output should contain only final line after transferring completed.
                            // Read skipped files count from it
                            skippedFiles = _utilities.ReadSkippedFiles(_output.Last());
                            transferredFiles = totalFiles - skippedFiles;

                            // Send exit command
                            _adbProcess.StandardInput.WriteLine("exit");

                            // Waiting process exit
                            await Task.Run(() => _adbProcess.WaitForExit());

                            #endregion

                            // Return according to process exit code
                            if (!_adbProcess.ExitCode.Equals(0))
                            {
                                throw new Exception("Wrong process exit code! Files may be transferred correctly.");
                            }

                            operationResult = new Tuple<int, int, int>(totalFiles, transferredFiles, skippedFiles);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                operationResult = new Tuple<int, int, int>(0, 0, 0);

                MessageBox.Show
                (
                    $"Error transferring files to device! Error details:\n\n" +
                    $"{ex.Message}"
                );
            }

            return operationResult;
        }

        /// <summary>
        /// Asks to select a folder to transfer it to device Documents folder.<br/>
        /// Handles exceptions returning 0 values.
        /// </summary>
        /// <param name="destinationDeviceIdentifier">Destination device identifier.</param>
        /// <returns>
        /// Returns a tuple where:<br/>
        /// - Item 1 is files to be transferred count.<br/>
        /// - Item 2 is transferred files count.<br/>
        /// - Item 3 is skipped files count.
        /// </returns>
        public async Task<Tuple<int, int, int>> TransferFolder(Device destinationDevice)
        {
            Tuple<int, int, int> operationResult = new Tuple<int, int, int>(0, 0, 0);
            string folderToBeTransferred = _utilities.BrowseFolder();

            if (!string.IsNullOrWhiteSpace(folderToBeTransferred))
            {
                operationResult = await ExecutePushCommand(destinationDevice.Id, destinationDevice.DocumentsFolderPath, folderToBeTransferred);
            }

            return operationResult;
        }

        /// <summary>
        /// Deletes Camera folder from target device.
        /// </summary>
        /// <param name="destionationDeviceIdentifier">Target device ID.</param>
        public async Task ExecuteDeleteCameraCommand(string destionationDeviceIdentifier)
        {
            try
            {
                string command = $"adb -s {destionationDeviceIdentifier} shell \"rm -r /sdcard/DCIM/Camera\"";
                await ExecuteCommand(command);
            }
            catch (Exception ex)
            {
                MessageBox.Show
                (
                    $"Error deleting extracted photos folder! Error details:\n\n" +
                    $"{ex.Message}"
                );
            }
        }

        /// <summary>
        /// Executes a pull command in ADB command prompt.<br/>
        /// Handles exceptions returning 0 values.
        /// </summary>
        /// <returns>
        /// Returns a tuple where:<br/>
        /// - Item 1 are total pulled files count.<br/>
        /// - Item 2 are totale skipped files count.
        /// </returns>
        private async Task<Tuple<int, int>> ExecutePullCommand(string sourceDeviceIdentifier, string folderToBePulled, string destinationFolder)
        {
            Tuple<int, int> operationResult = new Tuple<int, int>(0, 0);

            try
            {
                _output.Clear();

                #region "Format destination folder to work with CMD"

                List<string> splittedSourceDeviceFolder = folderToBePulled.Split(new char[] { '\\', '/' }).Where
                        (splitted =>
                            !string.IsNullOrWhiteSpace(splitted) &&
                            !splitted.Contains("storage") &&
                            !splitted.Contains("emulated") &&
                            !splitted.Contains("0")
                        ).ToList();

                // Add folder name to destination folder
                destinationFolder = $"{destinationFolder}\\{string.Join("\\", splittedSourceDeviceFolder)}";
                
                #endregion

                using (Process _adbProcess = new Process())
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource())
                    {
                        #region "Create process"

                        _adbProcess.StartInfo.CreateNoWindow = true;
                        _adbProcess.StartInfo.FileName = "cmd.exe";
                        _adbProcess.StartInfo.RedirectStandardInput = true;
                        _adbProcess.StartInfo.RedirectStandardOutput = true;
                        _adbProcess.StartInfo.RedirectStandardError = true;
                        _adbProcess.StartInfo.UseShellExecute = false;
                        _adbProcess.StartInfo.WorkingDirectory = Constants.PATHS.PLATFORM_TOOLS_DIR;

                        _adbProcess.OutputDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrWhiteSpace(e.Data))
                            {
                                _rawOutput.Add(e.Data);
                                if (e.Data.Contains(Constants.PATTERNS.PULLED_COMMAND_PATTERN) && e.Data.Contains(Constants.PATTERNS.SKIPPED_COMMAND_PATTERN))
                                {
                                    _output.Add(e.Data);
                                }
                            }
                        };

                        // Add event to read received errors
                        // Transferred files lines are errors ???
                        _adbProcess.ErrorDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrWhiteSpace(e.Data))
                            {
                                _rawOutput.Add(e.Data);
                                if (e.Data.Contains(Constants.PATTERNS.PULLED_COMMAND_PATTERN) && e.Data.Contains(Constants.PATTERNS.SKIPPED_COMMAND_PATTERN))
                                {
                                    _output.Add(e.Data);
                                }
                            }
                        };

                        _adbProcess.Start();
                        _adbProcess.StandardInput.WriteLine("echo off");
                        _adbProcess.BeginOutputReadLine();
                        _adbProcess.BeginErrorReadLine();

                        #endregion

                        #region "Run command"

                        string command = $"adb -s {sourceDeviceIdentifier} pull \"{folderToBePulled}\" \"{destinationFolder}\"";

                        _adbProcess.StandardInput.WriteLine(command);
                        _adbProcess.StandardInput.Flush();

                        // When transferring files there are no written lines
                        // Only at the end of the process a text will appear
                        // Wait till that moment
                        while (!_output.Count.Equals(1))
                        {
                            await Task.Delay(Constants.MISC.DEFAULT_WAITING_TIME);
                        }
                        await Task.Delay(250);

                        // Output should contain only final line after transferring completed.
                        // Read skipped files count from it
                        operationResult = _utilities.ReadPulledFiles(_output.Last());

                        // Send exit command
                        _adbProcess.StandardInput.WriteLine("exit");

                        // Waiting process exit
                        await Task.Run(() => _adbProcess.WaitForExit());

                        #endregion

                        // Return according to process exit code
                        if (!_adbProcess.ExitCode.Equals(0))
                        {
                            throw new Exception("Wrong process exit code! Files may be transferred correctly.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                operationResult = new Tuple<int, int>(0, 0);

                MessageBox.Show
                (
                    $"Error pulling files from device! Error details:\n\n" +
                    $"{ex.Message}"
                );
            }

            return operationResult;
        }

        /// <summary>
        /// Saves device folder in <paramref name="foldersToBackup"/> list to <paramref name="destinationFolder"/>.<br/>
        /// If <paramref name="destinationFolder"/> is not set, backup will be saved to program directory.<br/>
        /// A folder with device ID and 0 folder will be created at the root to permit backup restoring.<br/>
        /// Handles exceptions returning 0 values.
        /// </summary>
        /// <param name="destinationDeviceIdentifier">Destination device identifier.</param>
        /// <returns>
        /// Returns a tuple where:<br/>
        /// - Item 1 are total pulled files count.<br/>
        /// - Item 2 are totale skipped files count.
        /// </returns>
        public async Task<Tuple<int, int>> BackupFolders(Device sourceDevice, List<string> foldersToBackup, string destinationFolder = "")
        {
            Tuple<int, int> operationResult = new Tuple<int, int>(0, 0);
            int transferredFiles = 0;
            int skippedFiles = 0;

            try
            {
                if (string.IsNullOrWhiteSpace(destinationFolder))
                {
                    destinationFolder = Constants.PATHS.BACKUP_DIR;
                    Directory.CreateDirectory(destinationFolder);
                }

                if (foldersToBackup.Count > 0 && !string.IsNullOrWhiteSpace(destinationFolder) && Directory.Exists(destinationFolder))
                {
                    string deviceModel = sourceDevice.Id.Contains(":") ? sourceDevice.Id.Replace(":", ".") : sourceDevice.Id;
                    destinationFolder = $"{destinationFolder}\\{deviceModel}_{DateTime.Now.ToString("yy.MM.dd_HH.mm.ss")}\\0";

                    // Create folder
                    Directory.CreateDirectory(destinationFolder);

                    foreach (string folderToBackup in foldersToBackup)
                    {
                        var subOperationResult = await ExecutePullCommand(sourceDevice.Id, folderToBackup, destinationFolder);
                        transferredFiles += subOperationResult.Item1;
                        skippedFiles += subOperationResult.Item2;
                    }

                    operationResult = new Tuple<int, int>(transferredFiles, skippedFiles);
                }
            }
            catch (Exception ex)
            {
                operationResult = new Tuple<int, int>(0, 0);

                MessageBox.Show
                (
                    $"Error while performing backup! Error details:\n\n" +
                    $"{ex.Message}"
                );
            }

            return operationResult;
        }

        /// <summary>
        /// Restore a backup made previously with this program.<br/>
        /// </summary>
        /// <param name="destinationDevice">Destination device.</param>
        /// <param name="folderToBeRestored">Folder of backup previously made (the folder with device name/identifier).</param>
        /// <returns>
        /// Returns a tuple where:<br/>
        /// - Item 1 is files to be transferred count.<br/>
        /// - Item 2 is transferred files count.<br/>
        /// - Item 3 is skipped files count.
        /// </returns>
        public async Task<Tuple<int, int, int>> RestoreBackup(Device destinationDevice, string folderToBeRestored)
        {
            Tuple<int, int, int> operationResult = new Tuple<int, int, int>(0, 0, 0);

            if (!string.IsNullOrWhiteSpace(folderToBeRestored))
            {
                folderToBeRestored += $"\\0";

                if (Directory.Exists(folderToBeRestored))
                {
                    operationResult = await ExecutePushCommand(destinationDevice.Id, destinationDevice.DeviceFolderPath, folderToBeRestored);
                }
                else
                {
                    MessageBox.Show
                    (
                        "Selected folder not seems to be a valid backup or a backup made with this program!\n" +
                        "Please select the folder with the device name/identifier to proceed."
                    );
                }
            }

            return operationResult;
        }

        /// <summary>
        /// Scans applications on device.<br/>
        /// Handles exceptions returning empty lists.
        /// </summary>
        /// <param name="deviceIdentifier">Device identifier of the device to be scanned.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item>Item 1 is all device apps.</item>
        /// <item>Item 2 is device system apps.</item>
        /// <item>Item 3 is device third party apps.</item>
        /// </list>
        /// </returns>
        public async Task<Tuple<List<string>, List<string>, List<string>>> GetApplications(string deviceIdentifier)
        {
            List<string> allApps = new List<string>();
            List<string> systemApps = new List<string>();
            List<string> thirdPartyApps = new List<string>();

            try
            {
                int startIndex;
                string packageName;

                // Get all apps
                await ExecuteCommand($"adb -s {deviceIdentifier} shell pm list packages");
                if (_output.Count > 0)
                {
                    foreach (string allApp in _output)
                    {
                        startIndex = allApp.IndexOf(Constants.PATTERNS.APP_START_PATTERN) + Constants.PATTERNS.APP_START_PATTERN.Length;
                        packageName = allApp.Substring(startIndex);
                        allApps.Add(packageName);
                    }
                }

                // Get only system apps
                await ExecuteCommand($"adb -s {deviceIdentifier} shell pm list packages -s");
                if (_output.Count > 0)
                {
                    foreach (string systemApp in _output)
                    {
                        startIndex = systemApp.IndexOf(Constants.PATTERNS.APP_START_PATTERN) + Constants.PATTERNS.APP_START_PATTERN.Length;
                        packageName = systemApp.Substring(startIndex);
                        systemApps.Add(packageName);
                    }
                }

                // Get only 3d party packages
                await ExecuteCommand($"adb -s {deviceIdentifier} shell pm list packages -3");
                if (_output.Count > 0)
                {
                    foreach (string thirdPartyApp in _output)
                    {
                        startIndex = thirdPartyApp.IndexOf(Constants.PATTERNS.APP_START_PATTERN) + Constants.PATTERNS.APP_START_PATTERN.Length;
                        packageName = thirdPartyApp.Substring(startIndex);
                        thirdPartyApps.Add(packageName);
                    }
                }
            }
            catch (Exception ex)
            {
                allApps = new List<string>();
                systemApps = new List<string>();
                thirdPartyApps = new List<string>();

                MessageBox.Show
                (
                    $"Error getting device app list! Error details:\n\n" +
                    $"{ex.Message}"
                );
            }

            return new Tuple<List<string>, List<string>, List<string>>(allApps, systemApps, thirdPartyApps);
        }

        /// <summary>
        /// Uninstall an app on target device.
        /// </summary>
        /// <param name="deviceIdentifier">Target device ID.</param>
        /// <param name="packageName">Name of the package to uninstall.</param>
        /// <returns>True if uninstalled, otherwise false.</returns>
        public async Task<bool> UninstallApp(string deviceIdentifier, string packageName)
        {
            bool result = false;

            try
            {
                await ExecuteCommand($"adb -s {deviceIdentifier} shell pm uninstall -k --user 0 {packageName}");
                if (_output.Count > 0)
                {
                    result = _output.Last().Equals("Success") ? true : false;
                }
            }
            catch (Exception ex)
            {
                result = false;

                MessageBox.Show
                (
                    $"Error uninstalling app from device! Error details:\n\n" +
                    $"{ex.Message}"
                );
            }

            return result;
        }
    }
}