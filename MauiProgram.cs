using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Microsoft.Maui.LifecycleEvents;
using WinUIEx;
using Microsoft.Maui.Platform;

namespace IrreoExFirmware
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }).UseMauiCommunityToolkit();
#if DEBUG
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(wndLifeCycleBuilder =>
                {
                    wndLifeCycleBuilder.OnWindowCreated(window =>
                    {
                        IWindow window1 = new Window()
                        {
                            MaximumWidth = 1200,
                            MaximumHeight = 700,
                            Width = 1000,
                            Height = 600,
                            MinimumWidth = 1000,
                            MinimumHeight = 600
                        };
                        
                        window.CenterOnScreen(900, 600);
                        window.UpdateMaximumSize(window1);
                        window.UpdateMinimumSize(window1);
                    });
                });
            });
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}