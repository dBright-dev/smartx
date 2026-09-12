using SmartX.WebApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.WebApp.Views
{
    public partial class IngestionPage : ContentPage
    {
        public IngestionPage(IngestionViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
