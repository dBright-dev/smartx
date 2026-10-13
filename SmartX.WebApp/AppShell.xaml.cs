namespace SmartX.WebApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("ingestion", typeof(Views.IngestionPage));
            // Part 2 — Real-Time Command Stream (Pillar 2).
            Routing.RegisterRoute("commandstream", typeof(Views.CommandStreamPage));
        }
    }
}
