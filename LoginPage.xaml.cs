using CommunityToolkit.Mvvm.Input;
using Microsoft.Identity.Client;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Input;

namespace IrreoExFirmware
{

    public class LoginBtnModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ICommand LoginCommand { get; }
        B2CConfiguration B2CConfiguration { get; set; }
        IPublicClientApplication B2CApplication { get; set; }

        public LoginBtnModel()

        {
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
            
            LoginCommand = new RelayCommand(OnLoginClicked);
        }

        private async void OnLoginClicked()
        {
            var accountRes = await B2CApplication.GetAccountsAsync();
            var accountRet = accountRes.FirstOrDefault();

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

            Debug.WriteLine("Token: " + result.AccessToken);
        }
    }
}





    /*



    internal class LoginPage : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        B2CConfiguration B2CConfiguration { get; set; }
        IPublicClientApplication B2CApplication { get; set; }
        public ICommand OnLoginClick { get; private set; }


        public LoginPage()
        {
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

            OnLoginClick = new Command((o) =>  );
        }









        private async void OnLoginClicked(object sender, EventArgs e)
        {

            var accountRes = await B2CApplication.GetAccountsAsync();
            var accountRet = accountRes.FirstOrDefault();

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
            Debug.WriteLine("Token: ", result.AccessToken);
        }
    }
}
    */