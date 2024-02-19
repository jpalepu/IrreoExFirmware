using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

    public class Executor : IDisposable
    {

        Process _proc = null;
        CancellationTokenSource _cts = new CancellationTokenSource();
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
            string findID = @"MAC: ([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})";
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "powershell.exe", 
                        Arguments = $".\\flash.ps1 -Port {COM} -FirmwarePath {firmwarePath}" };

            startInfo.RedirectStandardOutput = true;
            _proc = new Process() { StartInfo = startInfo };
            _proc.Start();

            var output = await _proc.StandardOutput.ReadToEndAsync();
            Debug.WriteLine(output);

            Match match = Regex.Match(output, findID);
            Debug.WriteLine("Flashing Successfull...");

            if (match.Success)
            {
                var id = match.Value;
                Debug.WriteLine("The Mac ID of Device is: " + id);
                return true;
            }
            else
            {
                Debug.WriteLine("No ID found in the output.");
                return false;
            }
        }
        public async Task<string> ExecuteMonitor(string COM)
        {
            if (_proc != null)
            {
                _proc.Kill();
                _proc = null;
            }

            string deviceUID = null;

            string deviceUIDRegex = @"Value: ([0-9A-Fa-f]+)";
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "powershell.exe", Arguments = $".\\monitor.ps1 -Port {COM}", };
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
            try
            {
                await _proc.WaitForExitAsync(_cts.Token);
            }
            catch
            {

            }
            finally
            {
                Debug.WriteLine("Python process ended!");
            }
            return deviceUID;
        }
    }
}























