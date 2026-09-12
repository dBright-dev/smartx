using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.WebApp.Models
{
    public class Pillar
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string StatusText => IsEnabled ? "Active" : "Disabled";
        public Color StatusColor => IsEnabled ? Colors.Green : Colors.Gray;
        public string Route { get; set; } = string.Empty;
    }
}

// References:
// - Microsoft. (2024). What is .NET MAUI? [Online].
//   Available at: https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui?view=net-maui-10.0
// - dotnet. (2022). .NET MAUI Data Binding with MVVM & XAML [5 of 8] [Video].
//   Available at: https://www.youtube.com/watch?v=5Qga2pniN78