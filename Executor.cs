using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace IrreoExFirmware
{
    //public class Programm
    //{
    //    static async void FirmwareMain(string[] args)
    //    {

    //        using (var exec = new Executor())
    //        {
    //            Console.CancelKeyPress += (sender, eventArgs) =>
    //            {
    //                eventArgs.Cancel = true;
    //                exec.Cancel();
    //            };

    //            _ = await exec.ExecuteBuild();
    //            var task = exec.ExecuteMonitor();
    //            task.Wait();
    //            Console.WriteLine("Found deviceUID: " + task.Result);
    //        }
    //    }
    //}

    public class Executor : INotifyPropertyChanged
    {

        Process                 _proc = null;
        CancellationTokenSource _cts = new CancellationTokenSource();

        public event PropertyChangedEventHandler PropertyChanged;
        
            private string _progressState = "";

            public string ProgressState
            {
                get => _progressState;
                set
                {
                    if (_progressState != value)
                    {
                        _progressState = value;
                        OnPropertyChanged();
                    }
                }
            }
                public void OnPropertyChanged([CallerMemberName] string name = "") =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose()
        {
            if (_proc != null)
            {
                _proc.Kill();
                _proc = null;
                Console.WriteLine("Process disposed!");
            }
        }
        public void Cancel()
        {
            _cts.Cancel();
        }

        public async Task<bool> ExecuteBuild(string COM, string firmwarePath)
        {
            if (_proc != null)
            {
                _proc.Close();
                _proc = null;
            }

            if (_cts.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
            }

            ProgressState = "0";

            string findID = @"MAC: ([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})";
            //ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "powershell.exe", 
            //            Arguments = $".\\flash.ps1 -Port {COM} -FirmwarePath {firmwarePath}" };

            string findFlashPerc = @"Writing at 0x[0-9A-Fa-f\.]{11} \([0-9]+ %\)";

            Debug.WriteLine($"Current work directory: {AppDomain.CurrentDomain.BaseDirectory}");
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "flash.ps1")} -Port {COM} -FirmwarePath {firmwarePath}",
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                CreateNoWindow = true,
            };

            startInfo.RedirectStandardOutput = true;
            _proc = new Process() { StartInfo = startInfo };
            var result = _proc.Start();
            if (!result)
                return false;


            _proc.OutputDataReceived += (sender, e) =>
            {
                string data = e.Data;
                if (data != null)
                {
                    Debug.WriteLine(data);
                    Match macMatch = Regex.Match(data, findID);
                    if (macMatch.Success)
                    {
                        var id = macMatch.Value;
                        Debug.WriteLine("The Mac ID of Device is: " + id);
                    }

                    Match percMatch = Regex.Match(data, findFlashPerc);
                    if (percMatch.Success)
                    {
                        var startIdx = percMatch.Value.IndexOf('(');
                        var stopIdx = percMatch.Value.IndexOf(' ', startIdx);


                        ProgressState = percMatch.Value.Substring(startIdx + 1, stopIdx - startIdx - 1);

                        Debug.WriteLine("Progress %: " + ProgressState);
                        OnPropertyChanged(nameof(ProgressState));
                        
                        
                        //OnFlashProgress?.Invoke(this, perc);


                    }
                }
            };
            _proc.BeginOutputReadLine();

            try
            {
                await _proc.WaitForExitAsync(_cts.Token);
            }
            catch
            {

            }
            finally
            {
                Debug.WriteLine("Process closed!");
            }

            if (_proc.ExitCode != 0)
                return false;

            return true;
        }
        public async Task<string> ExecuteMonitor(string COM)
        {
            if (_proc != null)
            {
                _proc.Close();
                _proc = null;
            }

            if (_cts.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
            }

            string deviceUID = null;

            string deviceUIDRegex = @"Value: ([0-9A-Fa-f]+)";
            Debug.WriteLine($"Current work directory: {AppDomain.CurrentDomain.BaseDirectory}");
            ProcessStartInfo startInfo = new ProcessStartInfo() 
            { 
                FileName = "powershell.exe", 
                Arguments = $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "monitor.ps1")} -Port {COM}",
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                CreateNoWindow = true,
            };
            _proc = new Process() { StartInfo = startInfo, };
            // Console.WriteLine("Starting..");

            _proc.StartInfo.RedirectStandardOutput = true;
            _proc.StartInfo.RedirectStandardError = true;
            
            _proc.OutputDataReceived += (sender, e) =>
            {
                string data = e.Data;
                if (data != null)
                {
                    Debug.WriteLine(data);
                    Match match = Regex.Match(data, deviceUIDRegex);
                    if (match.Success)
                    {
                        Group value = match.Groups[1];
                        deviceUID = value.Value;
                        Debug.WriteLine("DeviceUID: " + deviceUID);
                        _cts.Cancel();
                    }
                }
            };

            _proc.Start();
            _proc.BeginOutputReadLine();
            //_proc.BeginErrorReadLine();
            try
            {
                await _proc.WaitForExitAsync(_cts.Token);
            }
            catch
            {

            }
            finally
            {
                _proc.Close();
                _proc = null;
                //_proc.Kill(true);
                
                Debug.WriteLine("Process ended!");
            }

            return deviceUID;
        }


        //public async void ExecuteTest(string COM)

        public async Task<string> ExecuteTest(string COM)
        {
            if (_proc != null)
            {
                _proc.Close();
                _proc = null;
            }

            if (_cts.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
            }

            string Telemetry = null;

            string TelemetryRegex = @"Emitting telem: ([0-9A-Fa-f]{48})";
            Debug.WriteLine($"Current work directory: {AppDomain.CurrentDomain.BaseDirectory}");
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "monitor.ps1")} -Port {COM}",
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                CreateNoWindow = true,
            };
            _proc = new Process() { StartInfo = startInfo, };

            _proc.StartInfo.RedirectStandardOutput = true;
            _proc.StartInfo.RedirectStandardError = true;

            _proc.OutputDataReceived += (sender, e) =>
            {
                string data = e.Data;
                if (data != null)
                {
                    Debug.WriteLine(data);
                    Match match = Regex.Match(data, TelemetryRegex);
                    if (match.Success)
                    {
                        Group value = match.Groups[1];
                        Telemetry = value.Value;
                        Debug.WriteLine("Got Telemetry: " + Telemetry);
                        _cts.Cancel();
                    }
                }
            };

            _proc.Start();
            _proc.BeginOutputReadLine();
            //_proc.BeginErrorReadLine();
            try
            {
                await _proc.WaitForExitAsync(_cts.Token);
            }
            catch
            {

            }
            finally
            {
                _proc.Close();
                _proc = null;
                Debug.WriteLine("Process ended!");
            }

            return Telemetry;
        }
    }
}























