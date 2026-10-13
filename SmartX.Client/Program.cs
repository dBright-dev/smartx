using SmartX.Client.Services;

namespace SmartX.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // ---- Part 2: typed HttpClient for the Command Stream module ----
            // REFERENCE: Microsoft, 2026. Typed HTTP clients in ASP.NET Core.
            // [online] Available at:
            // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
            // [Accessed 13 October 2026].
            var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5042";
            builder.Services.AddHttpClient<ICommandStreamClient, CommandStreamClient>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });

            // Shared HttpClient for controllers that inject HttpClient directly
            // (Sensors dashboard from Part 1).
            builder.Services.AddSingleton(_ => new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl)
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
