using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartX.WebApp.Models;
using SmartX.WebApp.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.WebApp.ViewModels
{
    public partial class IngestionViewModel : ObservableObject
    {
        private readonly ApiService _api;
        private readonly ILogger<IngestionViewModel> _logger;
        private FileResult? _pickedFile;

        public IngestionViewModel(ApiService api, ILogger<IngestionViewModel> logger)
        {
            _api = api;
            _logger = logger;

            // Load any existing devices at start-up.
            _ = LoadDevicesAsync();
        }

        // ------- Bindable properties -------

        [ObservableProperty] private SensorDevice _newDevice = new();
        [ObservableProperty] private List<SensorDevice>? _devices;
        [ObservableProperty] private List<SensorReading>? _readings;

        [ObservableProperty] private string _uploadMacAddress = string.Empty;
        [ObservableProperty] private string _pickedFileName = string.Empty;

        [ObservableProperty] private string _statusMessage = "Ready";
        [ObservableProperty] private Color _statusColor = Colors.Gray;
        [ObservableProperty] private bool _isBusy;

        // Exposes the enum names to a Picker control in XAML.(donet, 2022a).
        public List<string> Categories => Enum.GetNames(typeof(SensorCategory)).ToList();

        // ------- Commands -------

        [RelayCommand]
        private async Task LoadDevicesAsync()
        {
            IsBusy = true;
            try
            {
                Devices = await _api.GetDevicesAsync();
                StatusMessage = Devices?.Count > 0
                    ? $"Loaded {Devices.Count} devices."
                    : "No devices yet.";
                StatusColor = Colors.Green;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load devices");
                StatusMessage = "Load failed: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }


        [RelayCommand]
        private async Task RegisterDeviceAsync()
        {
            // Basic validation. In production, use FluentValidation or
            // DataAnnotations (Microsoft, 2026b).
            if (string.IsNullOrWhiteSpace(NewDevice.MacAddress))
            {
                StatusMessage = "MAC address is required.";
                StatusColor = Colors.Red;
                return;
            }

            IsBusy = true;
            try
            {
                var created = await _api.RegisterDeviceAsync(NewDevice);
                if (created is null)
                {
                    StatusMessage = "Registration failed.";
                    StatusColor = Colors.Red;
                    return;
                }

                StatusMessage = $"Registered {created.MacAddress}.";
                StatusColor = Colors.Green;
                NewDevice = new SensorDevice(); // reset the form
                await LoadDevicesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                StatusMessage = "Error: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task PickFileAsync()
        {
            // FilePicker is a cross-platform MAUI API. Platform-features demo
            // introduces FilePicker in dotnet (2022c).
            try
            {
                var pick = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a configuration file"
                });

                if (pick is null) return;

                _pickedFile = pick;
                PickedFileName = pick.FileName;
                StatusMessage = $"Picked {pick.FileName}.";
                StatusColor = Colors.Blue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File picker failed");
                StatusMessage = "File picker error: " + ex.Message;
                StatusColor = Colors.Red;
            }
        }

        [RelayCommand]
        private async Task UploadFileAsync()
        {
            if (string.IsNullOrWhiteSpace(UploadMacAddress))
            {
                StatusMessage = "Enter a MAC address before uploading.";
                StatusColor = Colors.Red;
                return;
            }
            if (_pickedFile is null)
            {
                StatusMessage = "Pick a file first.";
                StatusColor = Colors.Red;
                return;
            }

            IsBusy = true;
            try
            {
                await using var stream = await _pickedFile.OpenReadAsync();
                var ok = await _api.UploadFileAsync(UploadMacAddress, stream, _pickedFile.FileName);

                StatusMessage = ok ? "Upload succeeded." : "Upload failed.";
                StatusColor = ok ? Colors.Green : Colors.Red;

                if (ok) { _pickedFile = null; PickedFileName = string.Empty; }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed");
                StatusMessage = "Upload error: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task SendTestTelemetryAsync(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return;

            IsBusy = true;
            try
            {
                var value = (float)(new Random().NextDouble() * 40 + 10);
                var ok = await _api.SendTelemetryAsync(macAddress, value, "°C", SensorCategory.Environmental);

                StatusMessage = ok
                    ? $"Telemetry {value:F2}°C sent to {macAddress}."
                    : $"Telemetry to {macAddress} failed.";
                StatusColor = ok ? Colors.Green : Colors.Red;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telemetry failed");
                StatusMessage = "Telemetry error: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task ViewReadingsAsync(string macAddress)
        {
            IsBusy = true;
            try
            {
                Readings = await _api.GetReadingsAsync(macAddress);
                StatusMessage = $"Loaded {Readings?.Count ?? 0} readings for {macAddress}.";
                StatusColor = Colors.Green;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Readings failed");
                StatusMessage = "Readings error: " + ex.Message;
                StatusColor = Colors.Red;
            }
            finally { IsBusy = false; }
        }

    }
}
