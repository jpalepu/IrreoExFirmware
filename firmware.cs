using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IrreoExFirmware
{
    public class Programm
    {
        static void Main2(string[] args)
        {

            using (var exec = new Executor())
            {
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    exec.Cancel();
                };

                _ = exec.ExecuteBuild();
                var task = exec.ExecuteMonitor();
                task.Wait();
                Console.WriteLine("Found deviceUID: " + task.Result);
            }
        }
    }

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


        public async Task<bool> ExecuteBuild()
        {
            string findID = @"MAC: ([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})";
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "powershell.exe", Arguments = ".\\flash.ps1 -Port " };

            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;

            _proc.StandardInput.Flush();
            _proc.StandardInput.Close();

            _proc = new Process() { StartInfo = startInfo };

            _proc.Start();

            var output = _proc.StandardOutput.ReadToEnd();
            Console.WriteLine(output);

            Match match = Regex.Match(output, findID);
            Console.WriteLine("Flashing Successfull...");

            if (match.Success)
            {
                var id = match.Value;
                Console.WriteLine("The Mac ID of Device is: " + id);
                return true;
            }
            else
            {
                Console.WriteLine("No ID found in the output.");
                return false;
            }
        }
        public async Task<string> ExecuteMonitor()
        {
            if (_proc != null)
            {
                _proc.Kill();
                _proc = null;
            }

            string deviceUID = null;

            string deviceUIDRegex = @"Value: ([0-9A-Fa-f]+)";
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "powershell.exe", Arguments = ".\\monitor.ps1", };
            _proc = new Process() { StartInfo = startInfo, };
            // Console.WriteLine("Starting..");

            _proc.StartInfo.RedirectStandardOutput = true;
            _proc.StartInfo.RedirectStandardError = true;
            _proc.OutputDataReceived += (sender, e) =>
            {
                string data = e.Data;
                if (data != null)
                {
                    Console.WriteLine(data);
                    Match match = Regex.Match(data, deviceUIDRegex);
                    if (match.Success)
                    {
                        Group value = match.Groups[1];
                        deviceUID = value.Value;
                        Console.WriteLine("DeviceUID: " + deviceUID);
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
                Console.WriteLine("Python process ended!");
            }
            return deviceUID;
        }
    }
}























