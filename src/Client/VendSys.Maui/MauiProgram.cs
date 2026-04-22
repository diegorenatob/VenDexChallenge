using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using VendSys.Maui.Services;
using VendSys.Maui.ViewModels;

namespace VendSys.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IDexFileService, DexFileService>();
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

        builder.Services
            .AddHttpClient(ApiConstants.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(ApiConstants.BaseUrl);
            })
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(
                    3,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
