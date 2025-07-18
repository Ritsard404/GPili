using CommunityToolkit.Maui.Alerts;
using GPili.Presentation.Features.Cashiering;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using System.Diagnostics;

namespace GPili.Presentation.Features.LogIn
{
    public partial class LogInViewModel(IAuth _auth,
            IGPiliTerminalMachine _terminalMachine,
            INavigationService _navigationService) : ObservableObject
    {
        [ObservableProperty]
        private string _adminEmail;

        [ObservableProperty]
        private User? _selectedCashier;

        [ObservableProperty]
        private User[] _cashiers = [];

        [ObservableProperty]
        private bool _isLoading = false;

        public async ValueTask InitializeAsync()
        {
            IsLoading = true;
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
            IsLoading = false;
        }

        [RelayCommand]
        public async Task LogIn()
        {
            IsLoading = true;

            try
            {
                var (isSuccess, role, email, name, message) = await _auth.LogIn(AdminEmail, SelectedCashier?.Email ?? string.Empty);

                if (!isSuccess)
                {
                    await Shell.Current.DisplayAlert("Login Failed", message, "Ok");
                    //await Snackbar.Make(message, duration: TimeSpan.FromSeconds(2)).Show();
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
                await Shell.Current.DisplayAlert("An unexpected error occurred", $"{ex.Message}", "Ok"
                    );
                var error = ex.ToString();
                if (ex.InnerException != null)
                    error += "\n\nInnerException:\n" + ex.InnerException.ToString();

                // Write to a file in a specific folder on C:\
                var logDir = @"C:\\GPiliErrorLogs";
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);
                var logPath = Path.Combine(logDir, "startup-error.txt");
                File.WriteAllText(logPath, error);
                throw new Exception("An unexpected error occurred during login.", ex);
            }
            finally
            {

                IsLoading = false;
                AdminEmail = string.Empty;
                SelectedCashier = Cashiers[0];
            }
        }
    }
}
