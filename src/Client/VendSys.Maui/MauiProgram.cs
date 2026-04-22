using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Polly;
using VendSys.Client.Application.Interfaces;
using VendSys.Maui.Infrastructure;
using VendSys.Maui.Infrastructure.Services;
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

        builder.Services
            .AddSingleton<IDexFileService>(_ => new DexFileService(typeof(MauiProgram).Assembly))
            .AddSingleton<IApiService, ApiService>()
            .AddSingleton<MainViewModel>()
            .AddSingleton<MainPage>()
            .AddHttpClient(ApiConstants.HttpClientName, c => c.BaseAddress = new Uri(ApiConstants.BaseUrl))
            .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
