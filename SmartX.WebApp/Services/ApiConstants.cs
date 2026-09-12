using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.WebApp.Services
{
    public static class ApiConstants
    {
        public static string LocalhostUrl =
        DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "localhost";

        //public static string Scheme = DeviceInfo.Platform == DevicePlatform.Android ? "http" : "https";

        //public static string Port = DeviceInfo.Platform == DevicePlatform.Android ? "5042" : "7231";
        public static string Scheme = "http";
        public static string Port = "5042";
        public static string BaseUrl = $"{Scheme}://{LocalhostUrl}:{Port}";
    }
}

// References:
//   Microsoft. (2026b). Create backend services for native mobile apps with ASP.NET Core.
//     https://learn.microsoft.com/en-us/aspnet/core/mobile/native-mobile-backend?view=aspnetcore-10.0