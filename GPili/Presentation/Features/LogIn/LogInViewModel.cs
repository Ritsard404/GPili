using CommunityToolkit.Maui.Alerts;
using GPili.Presentation.Features.Cashiering;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using System.Diagnostics;

namespace GPili.Presentation.Features.LogIn
{
    public partial class LogInViewModel : ObservableObject
    {
        private readonly IAuth _auth;
        private readonly ILoaderService _loaderService;
        private readonly INavigationService _navigationService;
        private readonly CashieringViewModel _cashieringView;
        [ObservableProperty]
        private string _adminEmail;

        [ObservableProperty]
        private User? _selectedCashier;

        [ObservableProperty]
        private User[] _cashiers = [];

        public LogInViewModel(IAuth auth, ILoaderService loaderService, INavigationService navigationService, CashieringViewModel cashieringView)
        {
            _auth = auth;
            _loaderService = loaderService;
            _navigationService = navigationService;
            _cashieringView = cashieringView;
        }

        public async ValueTask InitializeAsync()
        {
            Cashiers = await _auth.GetCashiers();

            SelectedCashier = Cashiers[0];
        }

        [RelayCommand]
        public async Task LogIn()
        {

            await _loaderService.ShowAsync("Logging in...", true);

            try
            {
                var (isSuccess, role, email, name, message) = await _auth.LogIn(AdminEmail, SelectedCashier?.Email ?? string.Empty);

                if (!isSuccess)
                {
                    await Shell.Current.DisplayAlert("Log In Failed", message, "OK");
                    return;
                }

                switch (role)
                {
                    case RoleType.Manager:
                        await _navigationService.GoToManager();
                        return;

                    case RoleType.Cashier:
                        await _navigationService.NavigateToAsync(AppRoutes.Cashiering);
                        //await _cashieringView.InitializeAsync();
                        return;

                    default:
                        await Shell.Current.DisplayAlert("Log In", message, "OK");
                        AdminEmail = string.Empty;
                        SelectedCashier = Cashiers[0];
                        return;
                }
            }
            catch (Exception ex)
            {
                // Optional: log the exception to a service or file
                await Shell.Current.DisplayAlert("Error", $"An unexpected error occurred:\n{ex.Message}", "OK");
            }
            finally
            {
                await _loaderService.ShowAsync("", false); // Ensure cleanup
                AdminEmail = string.Empty;
                SelectedCashier = Cashiers[0];
            }
        }
    }
}
