using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartX.WebApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SmartX.WebApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public ObservableCollection<Pillar> Pillars { get; }

        public MainViewModel()
        {
            // Pillar 1: Sensor Data Ingestion and Telemetry - ACTIVE (Part 1)
            // Pillar 2: Real-Time Command Stream - DISABLED (Part 2)
            // Pillar 3: Network Topology and Mesh Routing - DISABLED (Final PoE)
            Pillars = new ObservableCollection<Pillar>
            {
                new Pillar
                {
                    Title = "Sensor Data Ingestion",
                    Description = "Register devices and ingest telemetry",
                    Icon = "📡",
                    IsEnabled = true,
                    Route = "ingestion"
                },
                new Pillar
                {
                    Title = "Real-Time Command Stream",
                    Description = "Comming Soon - Stay Tuned",
                    Icon = "⚡",
                    IsEnabled = false,
                    Route = string.Empty
                },
                new Pillar
                {
                    Title = "Network Topology",
                    Description = "Comming Soon - Stay Tuned",
                    Icon = "🌐",
                    IsEnabled = false,
                    Route = string.Empty
                }
            };
        }

        [RelayCommand]
        private async Task NavigateToPillar(Pillar pillar)
        {
            if (pillar is null || !pillar.IsEnabled || string.IsNullOrEmpty(pillar.Route))
                return;

            // Shell navigation preserves the app state during transitions,
            // as recommended in the .NET MAUI documentation (Microsoft, 2024).
            await Shell.Current.GoToAsync(pillar.Route);
        }
    }
}
