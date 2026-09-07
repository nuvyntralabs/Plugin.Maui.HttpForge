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
            .UseHttpForge(settings =>
            {
                settings.UrlParameterKeyFormatter = UrlParameterKeyFormatter.CamelCase;
                settings.AuthorizationHeaderValueGetter = (request, _) =>
                {
                    var uri = request.RequestUri;
                    var host = uri is { IsAbsoluteUri: true } ? uri.Host : "";
                    if (host.Contains("httpbin.org", StringComparison.OrdinalIgnoreCase))
                        return Task.FromResult<string?>("Bearer httpforge-sample");
                    return Task.FromResult<string?>(null);
                };
            })
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

        builder.Services.AddHttpForgeClient<ISseApi>(client =>
        {
            client.BaseAddress = new Uri("https://stream.wikimedia.org/");
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Plugin.Maui.HttpForge.Sample/1.1 (https://github.com/nuvyntralabs/Plugin.Maui.HttpForge)");
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
