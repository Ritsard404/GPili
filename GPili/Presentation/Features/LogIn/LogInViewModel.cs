using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;

namespace GPili.Presentation.Features.LogIn
{
    public partial class LogInViewModel: ObservableObject
    {
        private readonly IAuth _auth;

        [ObservableProperty]
        private string _adminEmail;

        [ObservableProperty]
        private User? _selectedCashier;

        [ObservableProperty]
        private User[] _cashiers = [];

        [ObservableProperty]
        private bool _isLoading;

        public LogInViewModel(IAuth auth)
        {
            _auth = auth;
        }

        public async ValueTask InitializeAsync()
        {
            IsLoading = true;

            Cashiers = await _auth.GetCashiers();

            SelectedCashier = Cashiers[0];

            IsLoading = false;
        }

        [RelayCommand]
        public async Task LogIn()
        {
            await Shell.Current.DisplayAlert("Log In", $"Cashier {SelectedCashier?.Email} \n Admin {AdminEmail}", "OK");
        }
    }
}
