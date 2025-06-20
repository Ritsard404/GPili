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

        [ObservableProperty]
        private string _adminEmail;

        [ObservableProperty]
        private User? _selectedCashier;

        [ObservableProperty]
        private User[] _cashiers = [];

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _loadingMessage;

        public LogInViewModel(IAuth auth, ILoaderService loaderService)
        {
            _auth = auth;
            _loaderService = loaderService;
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
            IsLoading = true;

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
                        await Shell.Current.DisplayAlert("Log In", $"Manager {SelectedCashier?.Email} \n Admin {AdminEmail}", "OK");
                        // await Shell.Current.GoToAsync("//MainPage");
                        return;

                    case RoleType.Cashier:
                        await Shell.Current.DisplayAlert("Log In", $"Cashier {SelectedCashier?.Email} \n Admin {AdminEmail}", "OK");
                        // await Shell.Current.GoToAsync("//CashierPage");
                        return;

                    default:
                        await Shell.Current.DisplayAlert("Log In", message, "OK");
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
                IsLoading = false;
                await _loaderService.ShowAsync("", false); // Ensure cleanup
            }
        }
    }
}
