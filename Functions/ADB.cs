using GoogleBackupManager.Model.Exceptions;
using GoogleBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace GoogleBackupManager.Functions
{
    internal static class ADB
    {
        #region "Variables       

        private static List<Device> _connectedDevices = new List<Device>();
        private static List<string> _rawOutput = new List<string>();
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
        /// Checks platform tools folder then starts adb server.
        /// </summary>
        /// <exception cref="PlatformToolsFolderException"></exception>
        /// <exception cref="PlatformToolsProcessException"></exception>
        internal static async Task InitializeConnection()
        {
            Utils.CheckPlatformToolsFolder();
            await StartServer();
        }

        /// <summary>
        /// Closes ADB server.
        /// </summary>
        /// <exception cref="PlatformToolsProcessException"></exception>
        internal static async Task CloseConnection()
        {
            await ExecuteCommand("adb kill-server");
        }

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

            if (unlimitedBackupProcess)
            {
                Utils.CreateUnlimitedBackupProgramFolders(sourceDevice);
            }
            else
            {
                Utils.CreateProgramFolders(sourceDevice);
            }

            foreach (string sourceDeviceFolder in sourceDeviceFolders)
            {
                totalFilesCount += await ExecutePullCommand(sourceDevice.ID, sourceDeviceFolder, unlimitedBackupProcess);
            }

            return totalFilesCount;
        }

        ///// <summary>
        ///// Performs backup operations.
        ///// </summary>
        ///// <param name="extractDevice">The device to make backup from.</param>
        ///// <param name="backupDevice">The device to make backup from.</param>
        ///// <returns>True if operation is successful, otherwise false.</returns>
        //internal static async Task<bool> PerformBackup(Device extractDevice, Device backupDevice, bool saveLocally = false)
        //{
        //    async Task ExecuteADBCommandAsync(string command)
        //    {
        //        // Invia il comando al processo
        //        await Task.Run(() =>
        //        {
        //            _commandPromptProcess.StandardInput.WriteLine(command);
        //            _commandPromptProcess.StandardInput.Flush();
        //            _commandPromptProcess.StandardInput.WriteLine("exit");
        //        });

        //        // Attendi che il processo termini
        //        await Task.Run(() => _commandPromptProcess.WaitForExit());

        //        // Gestisci il risultato finale
        //        if (_commandPromptProcess.ExitCode != 0)
        //        {
        //            throw new Exception($"ADB command failed: {_rawOutput.ToString()}");
        //        }

        //        Console.WriteLine("Command executed successfully.");
        //        // Puoi usare _filteredOutput per analizzare l'output
        //    }

        //    bool result = false;

        //    if (saveLocally)
        //    {
        //        Utils.CreateProgramFolders(extractDevice, backupDevice);

        //        try
        //        {
        //            // Pull della cartella dal dispositivo con id1
        //            string pullCommand = $"adb -s {extractDevice.ID} pull /storage/emulated/0/Android/media/com.whatsapp/WhatsApp/Databases {Utils.ProgramFolders.ExtractDeviceDirectory}";
        //            //string pullCommand = $"adb -s {extractDevice.ID} pull /storage/emulated/0/DCIM/Camera {Utils.ProgramFolders.ExtractDeviceDirectory}";
        //            await ExecuteADBCommandAsync(pullCommand);

        //            // Sincronizza i file dal locale al remoto sul dispositivo con id2
        //            //string syncPushCommand = $"-s {id2} sync {localFolderPath} {remoteFolderPath}";
        //            //await ExecuteADBCommandAsync(syncPushCommand); // Esecuzione asincrona

        //            Console.WriteLine("Operazione completata con successo.");
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Errore: {ex.Message}");
        //        }
        //    }
        //    else
        //    {


        //        // Pull files from extract device directly into backup device
        //        // Only files that doesn't exist

        //        //try
        //        //{
        //        //    // Pull della cartella dal dispositivo con id1
        //        //    string pullCommand = $"-s {extractDevice.ID} pull /storage/emulated/0/Documents/DCIM {localFolderPath}";
        //        //    await ExecuteADBCommandAsync(pullCommand);

        //        //    // Sincronizza i file dal locale al remoto sul dispositivo con id2
        //        //    string syncPushCommand = $"-s {id2} sync {localFolderPath} {remoteFolderPath}";
        //        //    await ExecuteADBCommandAsync(syncPushCommand); // Esecuzione asincrona

        //        //    Console.WriteLine("Operazione completata con successo.");
        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    Console.WriteLine($"Errore: {ex.Message}");
        //        //}
        //    }

        //    return result;
        //}

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
