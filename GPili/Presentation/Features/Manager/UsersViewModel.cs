using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using System.ComponentModel.DataAnnotations;

namespace GPili.Presentation.Features.Manager
{
    [QueryProperty(nameof(Users), nameof(Users))]
    [QueryProperty(nameof(ManagerEmail), nameof(ManagerEmail))]
    public partial class UsersViewModel(INavigationService _navigation,
        IAuth _auth) : ObservableObject
    {
        public UsersPage PopupUsers;

        [ObservableProperty]
        private User[]? _users;

        [ObservableProperty]
        private User? _user;

        [ObservableProperty]
        private string _managerEmail;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isAddUser = false;

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.3;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.65;

        [RelayCommand]
        public async Task Back()
        {
            await _navigation.GoBack();
        }
        [RelayCommand]
        private void ToggleAddUser()
        {
            if (!IsAddUser)
            {
                User = new User
                {
                    Email = string.Empty,
                    FName = string.Empty,
                    LName = string.Empty,
                    Role = RoleType.Cashier
                };
            }
            IsAddUser = !IsAddUser;
        }

        [RelayCommand]
        public async Task UpdateUser(User user)
        {
            IsLoading = true;
            user.FName = user.FName.Capitalize().Trim();
            user.LName = user.LName.Capitalize().Trim();

            var validationContext = new ValidationContext(user);
            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(
                user,
                validationContext,
                validationResults,
                validateAllProperties: true
            );

            if (!isValid)
            {
                // Aggregate error messages
                var errors = string.Join("\n", validationResults.Select(vr => vr.ErrorMessage));
                await Shell.Current.DisplayAlert("Validation Error", errors, "OK");
                IsLoading = false;
                return;
            }

            var (isSuccess, message) = await _auth.UpdateUser(user, ManagerEmail);
            if (isSuccess)
            {
                await Snackbar.Make("User updated successfully.", duration: TimeSpan.FromSeconds(1)).Show();
                Users = await _auth.Users();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", $"Error updating user: {message}", "OK");
            }

            IsLoading = false;
        }

        [RelayCommand]
        public async Task RemoveUser(User user)
        {
            IsLoading = true;
            var (isSuccess, message) = await _auth.DeleteUser(emailOrId: user.Email, managerEmail: ManagerEmail, role: user.Role);
            if (isSuccess)
            {
                await Snackbar.Make("User deleted successfully.", duration: TimeSpan.FromSeconds(1)).Show();
                Users = await _auth.Users();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", $"Error deleting user: {message}", "OK");
            }

            IsLoading = false;
        }

        [RelayCommand]
        public async Task AddUser()
        {
            IsLoading = true;

            if (User == null)
            {
                await Shell.Current.DisplayAlert("Error", "Please fill in all user details.", "OK");
                IsLoading = false;
                return;
            }

            User.Email = User.Email.Trim().ToLower();
            User.FName = User.FName.Capitalize().Trim();
            User.LName = User.LName.Capitalize().Trim();


            var (isSuccess, message) = await _auth.AddUser(user: User, managerEmail: ManagerEmail);
            if (isSuccess)
            {
                await Snackbar.Make("User added successfully.", duration: TimeSpan.FromSeconds(1)).Show();
                Users = await _auth.Users();
                User = null;
                IsAddUser = false; // Close the popup after adding user
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", $"Error adding user: {message}", "OK");
            }

            IsLoading = false;
        }

    }
}
