using System.Diagnostics;
using System.IO.Compression;
using System.IO.Ports;
using MS.WindowsAPICodePack.Internal;
using Microsoft.WindowsAPICodePack.Shell;


namespace IrreoExFirmware;

public partial class DisplayConfig : ContentPage
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

    public DisplayConfig()
	{
		InitializeComponent();
        
        pickerCOM.ItemsSource = SerialPort.GetPortNames().Select(s => new NamedResult<string> { Name = s, Result = s }).ToList();
        pickerCOM.SelectedItem = pickerCOM.ItemsSource[0];
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
        pickerRev.SelectedIndex = -1;

        _versionSelected = null;
        pickerVersion.SelectedIndex = -1;
        pickerVersion.ItemsSource = new List<NamedResult<Version>>();

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
            return files.Select(f => new NamedResult<Version>
            {
                Name = f.Split("\\").Last().Substring(0, 8),
                Result = Version.Parse(f.Split("\\").Last().Substring(1, 7))
            });
        }

        return new List<NamedResult<Version>>();
    }


    public void OnPickerRevSelected(object sender, EventArgs e)
    {

        if (_boardLocation != null && pickerRev.SelectedIndex >= 0)
        {
            _boardRev = (string)pickerRev.ItemsSource[pickerRev.SelectedIndex];
            var finalLocation = Path.Combine(_boardLocation, pickerRev.ItemsSource[pickerRev.SelectedIndex].ToString());
            Debug.WriteLine(finalLocation);

            var firmwareVersions = GetFirmwareVersionsFromPath(finalLocation);

            pickerVersion.ItemsSource = firmwareVersions.OrderByDescending(n => n.Result).ToList();
            pickerVersion.SelectedIndex = 0;
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
        if (pickerVersion.ItemsSource.Count > 0 && pickerVersion.SelectedIndex >= 0)
        {
            _versionSelected = ((NamedResult <Version>)pickerVersion.ItemsSource[pickerVersion.SelectedIndex]).Result;
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



    private void Slider_OnValueChanged(object? sender, ValueChangedEventArgs e)
    {

		int maxProgressbarValue = 100;
		var taskbarInstance = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
		taskbarInstance.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.Normal);
		taskbarInstance.SetProgressValue((int)e.NewValue, maxProgressbarValue);

		if (e.NewValue >= maxProgressbarValue)
		{
			taskbarInstance.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.NoProgress);
		}
    }


    public void FlashFirmwareBtn(object sender, EventArgs e)
    {
        if (_boardLocation != null && _boardRev != null)
        {
            var finalLocation = Path.Combine(_boardLocation, pickerRev.ItemsSource[pickerRev.SelectedIndex].ToString());
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

        }
    }

    public void OnEntryTextChanged(object sender, EventArgs e)
    {
        if ((entry.Text).Length == 16)
        {
            Register.IsEnabled = true;
        }
        else
        {
            Register.IsEnabled = false;
        }
    }
   
    public void RegisterDeviceBtn(object sender, EventArgs e)
    {

    }

    public void OnEntryCompleted(object sender, EventArgs e)
    {
        
    }


    public void GetDeviceInfo(object sender, EventArgs e)
    {
    //has to be done. return the info of the device which is the uid.  maybe it would be good if we get this info from the flash button itself..
    }

}
