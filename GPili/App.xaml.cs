using Microsoft.Extensions.DependencyInjection;
using ServiceLibrary.Services;

namespace GPili
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                var dbInitializer = IPlatformApplication.Current.Services.GetService<IDatabaseInitializerService>();
                if (dbInitializer != null)
                {
                    // Run initialization synchronously since we're in the constructor
                    dbInitializer.InitializeAsync().GetAwaiter().GetResult();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Database initializer service not found");
                }
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex}");
                
                // Show error on the main thread
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Current.MainPage.DisplayAlert(
                        "Database Error",
                        "There was an error initializing the database. The app may not function correctly.",
                        "OK");
                });
            }
        }
    }
}
