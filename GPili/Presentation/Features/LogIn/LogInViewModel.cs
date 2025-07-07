using CommunityToolkit.Maui.Alerts;
using GPili.Presentation.Features.Cashiering;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using System.Diagnostics;

namespace GPili.Presentation.Features.LogIn
{
    public partial class LogInViewModel(IAuth _auth,
            IPopUpService _popUpService,
            IGPiliTerminalMachine _terminalMachine,
            INavigationService _navigationService) : ObservableObject
    {
        [ObservableProperty]
        private string _adminEmail;

        [ObservableProperty]
        private User? _selectedCashier;

        [ObservableProperty]
        private User[] _cashiers = [];

        public async ValueTask InitializeAsync()
        {
            while (true)
            {
                var (isValid, message) = await _terminalMachine.ValidateTerminalExpiration();

                if (!isValid)
                {
                    // Spam display
                    await Shell.Current.DisplayAlert("Error", message, "");
                    continue;
                }

                if (message.StartsWith("Warning"))
                    await Shell.Current.DisplayAlert("Warning", message, "Continue");

                break;
            }

            Cashiers = await _auth.GetCashiers();

            SelectedCashier = Cashiers[0];
        }

        [RelayCommand]
        public async Task LogIn()
        {

            await _popUpService.ShowAsync("Logging in...", true);

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
                        await _navigationService.GoToManager(null);
                        CashierState.Info.UpdateCashierInfo(name, email, role);
                        return;

                    case RoleType.Cashier:
                        await _navigationService.NavigateToAsync(AppRoutes.Cashiering);
                        CashierState.Info.UpdateCashierInfo(name, email, role);
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
                await _popUpService.ShowAsync("", false); // Ensure cleanup
                AdminEmail = string.Empty;
                SelectedCashier = Cashiers[0];
            }
        }
    }
}
