namespace GPili
{
    public partial class App : Application
    {
        private readonly IDatabaseInitializerService _databaseInitializer;
        public App(AppShell appShell, IDatabaseInitializerService databaseInitializer)
        {
            InitializeComponent();

            MainPage = appShell;
            _databaseInitializer = databaseInitializer;

        }

        protected override async void OnStart()
        {
            // Handle when your app starts
            base.OnStart();
            await _databaseInitializer.InitializeAsync();
        }
}
}
