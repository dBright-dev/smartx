using SmartX.WebApp.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace SmartX.WebApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;

        public ApiService(HttpClient http)
        {
            _http = http;
            System.Diagnostics.Debug.WriteLine($"[ApiService] BaseAddress = {http.BaseAddress}");
        }

        public Task<List<SensorDevice>?> GetDevicesAsync()
            => _http.GetFromJsonAsync<List<SensorDevice>>("/api/devices");

        public async Task<SensorDevice?> RegisterDeviceAsync(SensorDevice device)
        {
            var response = await _http.PostAsJsonAsync("/api/devices/register", device);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<SensorDevice>();
        }

        public async Task<bool> SendTelemetryAsync<T>(string deviceId, T value, string unit, SensorCategory category)
        {
            // Serialise the generic wrapper. The API closes T on the matching
            // endpoint (/float, /int, /bool) as described in Microsoft (2026c).
            var packet = new
            {
                deviceId,
                timestamp = DateTime.UtcNow,
                value,
                unit,
                category = (int)category
            };

            var typeSegment = typeof(T) switch
            {
                var t when t == typeof(float) => "float",
                var t when t == typeof(int) => "int",
                var t when t == typeof(bool) => "bool",
                _ => throw new NotSupportedException($"Telemetry type {typeof(T)} not supported")
            };

            var response = await _http.PostAsJsonAsync($"/api/telemetry/{typeSegment}", packet);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UploadFileAsync(string macAddress, Stream stream, string fileName)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(stream), "file", fileName);

            var response = await _http.PostAsync($"/api/devices/{macAddress}/upload", content);
            return response.IsSuccessStatusCode;
        }

        public Task<List<SensorReading>?> GetReadingsAsync(string macAddress)
            => _http.GetFromJsonAsync<List<SensorReading>>($"/api/devices/{macAddress}/readings");
    }
}


// References:
//   Microsoft. (2026b). Create backend services for native mobile apps with ASP.NET Core.
//     https://learn.microsoft.com/en-us/aspnet/core/mobile/native-mobile-backend?view=aspnetcore-10.0
//   Microsoft. (2026d). Create responses in Minimal API applications.
//     https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0