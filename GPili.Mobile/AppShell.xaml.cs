using GPili.Mobile.Presentation.Features.Cashier;
using GPili.Mobile.Presentation.Features.LogIn;
using GPili.Mobile.Presentation.Features.Manager;
using GPili.Mobile.Utils;

namespace GPili.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent(); 
            
            RegisterRoutes();
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute(AppRoutes.Login, typeof(LogInPage));
            Routing.RegisterRoute(AppRoutes.Cashiering, typeof(CashierPage));

            Routing.RegisterRoute(AppRoutes.Manager, typeof(ManagerPage));
        }
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            Debug.WriteLine("Logout clicked!");

            // Show confirmation popup
            bool shouldLogout = await DisplayAlert(
                "Confirm Logout",
                "Are you sure you want to logout?",
                "Yes, Logout",
                "Cancel");

            await Shell.Current.GoToAsync(AppRoutes.Login);
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();

            using (var scope = IPlatformApplication.Current.Services.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
                dbInitializer.InitializeAsync().GetAwaiter().GetResult();
            }
        }
    }
}
