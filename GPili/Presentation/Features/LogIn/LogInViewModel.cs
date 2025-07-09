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
                    await Snackbar.Make(message,
                        duration: TimeSpan.FromSeconds(1)).Show();
                    continue;
                }

                if (message.StartsWith("Warning"))
                    await Snackbar.Make(message,
                        duration: TimeSpan.FromSeconds(1)).Show();

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
                    await Snackbar.Make(message, duration: TimeSpan.FromSeconds(2)).Show();
                    return;
                }

                switch (role)
                {
                    case RoleType.Developer:
                        await _navigationService.GoToManager(email, role == RoleType.Developer);
                        CashierState.Info.UpdateCashierInfo(name, email, role);
                        return;

                    case RoleType.Manager:
                        await _navigationService.GoToManager(email);
                        CashierState.Info.UpdateCashierInfo(name, email, role);
                        return;

                    case RoleType.Cashier:
                        await _navigationService.NavigateToAsync(AppRoutes.Cashiering);
                        CashierState.Info.UpdateCashierInfo(name, email, role);
                        return;

                    default:
                        await Snackbar.Make(message,
                            duration: TimeSpan.FromSeconds(1)).Show();
                        AdminEmail = string.Empty;
                        SelectedCashier = Cashiers[0];
                        return;
                }
            }
            catch (Exception ex)
            {
                // Optional: log the exception to a service or file
                await Shell.Current.DisplayAlert("An unexpected error occurred", $"{ex.Message}","Ok"
                    );
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
