namespace SmartX.WebApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("ingestion", typeof(Views.IngestionPage));
        }
    }
}
