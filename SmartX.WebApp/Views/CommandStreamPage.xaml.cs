using SmartX.WebApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.WebApp.Views
{
    public partial class CommandStreamPage : ContentPage
    {
        public CommandStreamPage(CommandStreamViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
