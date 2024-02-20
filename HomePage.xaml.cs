using System.Diagnostics;
using System.IO.Compression;
using System.IO.Ports;
using MS.WindowsAPICodePack.Internal;
using Microsoft.WindowsAPICodePack.Shell;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Windows.ApplicationModel.Store;



namespace IrreoExFirmware;

public partial class HomePage : ContentPage
{
    /*
        public class Folders
        {
            private string _name;
            public string Name { get => _name; set => _name = value; }

        }

        private Folders _folder;
        public Folders SelectedPort
        {
            get => _folder;
            set
            {
                Debug.WriteLine(value.Name);
                _folder = value;
            }
        }

        */

    string      _baseLocation   = "D:\\ExFiles";
    string      _tmpLocation    = "D:\\ExFiles\\tmp";
    string      _boardLocation;
    string      _boardRev;

    Version?    _versionSelected; 
    string      _port;


    public class NamedResult<T>
    {
        public string   Name    { get; set; }
        public T        Result  { get; set; }
    }

    public HomePage()
	{
		InitializeComponent();
        
        PickerCOM.ItemsSource = SerialPort.GetPortNames().Select(s => new NamedResult<string> { Name = s, Result = s }).ToList();
        PickerCOM.SelectedItem = PickerCOM.ItemsSource[0];
        Register.IsEnabled = false;
    }

    public void RadioBtnSelectVersion(object sender, CheckedChangedEventArgs e)
    {
        
        RadioButton selectVersionBtn = sender as RadioButton;
 
        if (((string)selectVersionBtn.Content).ToLowerInvariant() == "edge com lora meteo" && e.Value)
        {
            _boardLocation = Path.Combine(_baseLocation, "Meteo");
        }
        else if (((string)selectVersionBtn.Content).ToLowerInvariant() == "edge com lora l2" && e.Value)
        {
            _boardLocation = Path.Combine(_baseLocation, "L2");
        }
        else if (((string)selectVersionBtn.Content).ToLowerInvariant() == "edge com lora l8" && e.Value)
        {
            _boardLocation = Path.Combine(_baseLocation, "L8");
        }
        else if (((string)selectVersionBtn.Content).ToLowerInvariant() == "edge com lora l16" && e.Value)
        {
            _boardLocation = Path.Combine(_baseLocation, "L16");
        }
        else if (e.Value)
        {
            Debug.WriteLine("Wrong path/ Something else is wrong");
        }
        else
        {
            _boardLocation = null;
            return;
        }

        Debug.WriteLine($"Board selected: {((string)selectVersionBtn.Content)}");

        _boardRev = null;
        PickerRev.SelectedIndex = -1;

        _versionSelected = null;
        PickerVersion.SelectedIndex = -1;
        PickerVersion.ItemsSource = new List<NamedResult<Version>>();

    }

    public IEnumerable<NamedResult<Version>> GetFirmwareVersionsFromPath(string path)
    {
        if (_boardLocation != null)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var files = Directory.GetFiles(path);
            return files.Select(f =>
            {
                var fileName = f.Split("\\").Last();

                return new NamedResult<Version>
                {
                    Name = fileName.Substring(0, fileName.Length - 4),
                    Result = Version.Parse(fileName.Substring(1, fileName.Length - 5))
                };
            });
            
        }

        return new List<NamedResult<Version>>();
    }
    public void OnPickerRevSelected(object sender, EventArgs e)
    {

        if (_boardLocation != null && PickerRev.SelectedIndex >= 0)
        {
            _boardRev = (string)PickerRev.ItemsSource[PickerRev.SelectedIndex];
            var finalLocation = Path.Combine(_boardLocation, PickerRev.ItemsSource[PickerRev.SelectedIndex].ToString());
            Debug.WriteLine(finalLocation);

            var firmwareVersions = GetFirmwareVersionsFromPath(finalLocation);

            PickerVersion.ItemsSource = firmwareVersions.OrderByDescending(n => n.Result).ToList();
            PickerVersion.SelectedIndex = 0;
            if (firmwareVersions.Count() > 0)
            {
                _versionSelected = firmwareVersions.OrderByDescending(n => n.Result).First().Result;
                Debug.WriteLine($"Version selected: {_versionSelected}");
            }
            else
            {
                _versionSelected = null;
            }
        }
    }
    public void OnPickerVersionSelected(object sender, EventArgs e)
    {
        if (PickerVersion.ItemsSource.Count > 0 && PickerVersion.SelectedIndex >= 0)
        {
            _versionSelected = ((NamedResult <Version>)PickerVersion.ItemsSource[PickerVersion.SelectedIndex]).Result;
            Debug.WriteLine($"Version selected: {_versionSelected}");
            FlashBtn.IsEnabled = true;
        }
    }
    public void OnPickerCOMSelected(object sender, EventArgs e)
    {
        var pickerCOM = sender as Picker;
        var port = pickerCOM.SelectedItem as NamedResult<string>;
        _port = port.Result;
        Debug.WriteLine($"Port selected: {_port}");
    }
    public async void FlashFirmwareBtn(object sender, EventArgs e)
    {
        if (_boardLocation != null && _boardRev != null)
        {
            
            if (Application.Current.RequestedTheme == AppTheme.Dark)
                FlashBtn.BackgroundColor = Colors.Teal;
            else
                FlashBtn.BackgroundColor = Colors.MediumSeaGreen;

            ToggleAllButtons(false);

            var finalLocation = Path.Combine(_boardLocation, PickerRev.ItemsSource[PickerRev.SelectedIndex].ToString());
            var fileName = "v" + _versionSelected.ToString() + ".zip";

            Debug.WriteLine($"Firmware file: {Path.Combine(finalLocation, fileName)}");

            if (!Directory.Exists(_tmpLocation))
            {
                Directory.CreateDirectory(_tmpLocation);
            }

            foreach (var file in Directory.GetFiles(_tmpLocation))
            {
                File.Delete(file);
            }

            ZipFile.ExtractToDirectory(Path.Combine(finalLocation, fileName), _tmpLocation);

            var result = await MyExecutor.ExecuteBuild(_port, _tmpLocation);

            Debug.WriteLine($"Flash result: {result}");
            if (!result)
            {
                if (Application.Current.RequestedTheme == AppTheme.Dark)
                    FlashBtn.BackgroundColor = Colors.Red;
                else
                    FlashBtn.BackgroundColor = Colors.Crimson;
            }
            ToggleAllButtons(true);

        }
    }
    private void ToggleAllButtons(bool status)
    {
        FlashBtn.IsEnabled = status;
        DeviceInfo.IsEnabled = status;
        TestDeviceButton.IsEnabled = status;
    }

    public async void GetDeviceInfo(object sender, EventArgs e)
    {
        //has to be done. return the info of the device which is the uid.  maybe it would be good if we get this info from the flash button itself..
        InfoResultEntry.IsEnabled = false;
        InfoResultEntry.Text = "Retrieving data...";
        ToggleAllButtons(false);
        Register.IsEnabled = false;

        string DeviceUID = await MyExecutor.ExecuteMonitor(_port);
        InfoResultEntry.Text = DeviceUID;
        InfoResultEntry.IsEnabled = true;
        ToggleAllButtons(true);
        Register.IsEnabled = true;
    }


    public void RegisterDeviceBtn(object sender, EventArgs e)
    {

    }
   


}
