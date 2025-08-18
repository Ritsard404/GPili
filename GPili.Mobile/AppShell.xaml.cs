using GPili.Mobile.Presentation.Features.Cashier;
using GPili.Mobile.Presentation.Features.LogIn;
using GPili.Mobile.Presentation.Features.Manager;
using GPili.Mobile.Utils;
using GPili.Mobile.Utils.State;

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

            App.UserInfo = null;
            CashierState.Info.Reset();
            await Shell.Current.GoToAsync(AppRoutes.Login);
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            using var scope = IPlatformApplication.Current.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IDatabaseService>().InitializeAsync();

            await scope.ServiceProvider.GetRequiredService<INavigationService>().InitializeAsync();
        }
    }
}
