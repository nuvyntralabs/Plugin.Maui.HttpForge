using Microsoft.Extensions.Logging;
using Plugin.Maui.HttpForge;
using Plugin.Maui.HttpForge.Sample.Demo;

namespace Plugin.Maui.HttpForge.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<MainPage>();

        builder
            .UseMauiApp<App>()
            .UseHttpForge()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddHttpForgeClient<IPostApi>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        builder.Services.AddHttpForgeClient<IHttpBinApi>(client =>
        {
            client.BaseAddress = new Uri("https://httpbin.org/");
            client.Timeout = TimeSpan.FromSeconds(20);
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
