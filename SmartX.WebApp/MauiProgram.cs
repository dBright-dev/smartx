using Microsoft.Extensions.Logging;
using SmartX.WebApp.Services;
using SmartX.WebApp.ViewModels;
using SmartX.WebApp.Views;

namespace SmartX.WebApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddHttpClient<ApiService>(client =>
            {
                client.BaseAddress = new Uri(ApiConstants.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() =>
            {
                // Accept the dev certificate on Android in DEBUG only.
                var handler = new HttpClientHandler();
#if DEBUG
                handler.ServerCertificateCustomValidationCallback =
                    (msg, cert, chain, errors) => true;
#endif
                return handler;
            });

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddTransient<IngestionViewModel>();

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<IngestionPage>();


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
