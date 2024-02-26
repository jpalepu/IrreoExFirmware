using CommunityToolkit.Mvvm.Input;
using Microsoft.Identity.Client;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using System.IO.Compression;
using System.IO.Ports;
using System.Text.Json;

namespace IrreoExFirmware
{
    public partial class HomePage : ContentPage
    {

        string                               _baseLocation = "D:\\ExFiles";
        
        string                               _tmpLocation = "D:\\ExFiles\\tmp";
        
        string                               _boardLocation;
        
        string                               _boardRev;
        
        string                               _port;
        
        private string                       _name;
        public string                        Surname                 { get; private set; } = "User not LoggedIn";
        public string                        GivenName
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }
        public bool                          IsLoggedIn              { get; private set; }

        Version?                             _versionSelected;

        B2CConfiguration                     B2CConfiguration        { get; set; }
        IPublicClientApplication             B2CApplication          { get; set; }
        public class NamedResult<T>
        {
            public string                    Name { get; set; }
            public T                         Result { get; set; }
        }


        public HomePage()
        {
            InitializeComponent();


            PickerCOM.ItemsSource = SerialPort.GetPortNames().Select(s => new NamedResult<string> { Name = s, Result = s }).ToList();
            PickerCOM.SelectedItem = PickerCOM.ItemsSource[0];
            Register.IsEnabled = false;



            using (var stream = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auth_config.json"),
                    FileMode.Open, FileAccess.Read))
            {
                B2CConfiguration = JsonSerializer.Deserialize<B2CConfiguration>(stream);
            }

            B2CApplication = PublicClientApplicationBuilder.Create(B2CConfiguration.ClientId)
                .WithB2CAuthority(B2CConfiguration.Authority)
                .WithRedirectUri(B2CConfiguration.RedirectUri)
                .WithCacheOptions(new CacheOptions { UseSharedCache = true })
                .Build();

        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await AcquireToken();
            
            nome.Text = GivenName;
            surname.Text = Surname;
            LoginBtn.IsVisible = false;
            LogoutBtn.IsVisible = true;
        }
        
        public async Task<(string token, string givenname)> AcquireToken()
        {
            var accountRes = await B2CApplication.GetAccountsAsync();
            var accountRet = accountRes.FirstOrDefault();

            try
            {
                AuthenticationResult result = null;
                if (accountRet != null)
                {
                    result = await B2CApplication.AcquireTokenSilent(B2CConfiguration.Scopes, accountRet).ExecuteAsync();
                    if (result == null || DateTime.UtcNow >= result.ExpiresOn)
                    {
                        result = await B2CApplication.AcquireTokenInteractive(B2CConfiguration.Scopes).ExecuteAsync();
                    }
                }
                else
                {
                    result = await B2CApplication.AcquireTokenInteractive(B2CConfiguration.Scopes).ExecuteAsync();
                }

                if (result != null && result.ClaimsPrincipal != null)
                {
                    GivenName = result.ClaimsPrincipal.Claims.Where(c => c.Type == "given_name").Select(c => c.Value).FirstOrDefault();
                    Surname = result.ClaimsPrincipal.Claims.Where(c => c.Type == "family_name").Select(c => c.Value).FirstOrDefault();

                    OnPropertyChanged(nameof(GivenName));
                    OnPropertyChanged(nameof(Surname));
                    Debug.WriteLine("Name: {0}, Surname: {1}, IsLogggedIn: {2}", GivenName, Surname, IsLoggedIn);

                }

                Debug.WriteLine("Token: " + result.AccessToken);
                return (result.AccessToken, GivenName);

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Login Failed: {ex.Message}");
            }
            return (null, null);
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
                _versionSelected = ((NamedResult<Version>)PickerVersion.ItemsSource[PickerVersion.SelectedIndex]).Result;
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
        
        public async void TestDevice(object sender, EventArgs e)
        {
            InfoResultEntry.IsEnabled = false;
            InfoResultEntry.Text = "Retrieving data...";
            ToggleAllButtons(false);
            Register.IsEnabled = false;
            TestActivity.IsRunning = true;
            TestActivity.IsVisible = true;

            //string doneTest = await MyExecutor.ExecuteTest(_port);
            //InfoResultEntry.Text = doneTest;

            var result = await MyExecutor.ExecuteTest(_port);
            bool evtstatus = result.status;
            string telemetry = result.telemetry;

            TestResultEntry.Text = telemetry;
            if(evtstatus != false && telemetry != null)
            {
                string combinedtext = $"Join Status: {(evtstatus ? "joined" : "failed")}\nTelemetry: {telemetry}";
                TestResultEntry.Text = combinedtext;
                TestActivity.IsRunning = false;
                TestActivity.IsVisible = false;
                TestResultPass.IsVisible = true;
            }
            ToggleAllButtons(true);
            Register.IsEnabled = true;
        }
        
        public void RegisterDeviceBtn(object sender, EventArgs e)
        {


        }
        private void OnLogoutClicked(object sender, EventArgs e)
        {
            Debug.WriteLine("Logout called..");
            LogoutBtn.IsVisible = false;
            LoginBtn.IsVisible = true;
            nome.Text = "user not logged in";
            surname.Text = "";
        }




    }
}
