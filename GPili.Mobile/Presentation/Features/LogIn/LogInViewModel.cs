
using GPili.Mobile.Utils;
using GPili.Mobile.Utils.State;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;

namespace GPili.Mobile.Presentation.Features.LogIn
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
                    await Snackbar.Make(message, duration: TimeSpan.FromSeconds(2)).Show();
                    return;
                }

                var userDetails = new UserInfo
                {
                    Email = email,
                    Name = name,
                    Role = role
                };

                App.UserInfo = userDetails;
                await AppConstant.AddTabMenus();

            }
            catch (Exception ex)
            {
                // Optional: log the exception to a service or file
                await Shell.Current.DisplayAlert("An unexpected error occurred", $"{ex.Message}", "Ok"
                    );

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
