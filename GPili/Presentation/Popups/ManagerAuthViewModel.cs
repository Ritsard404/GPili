using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using ServiceLibrary.Services.Interfaces;

namespace GPili.Presentation.Popups
{
    public partial class ManagerAuthViewModel(IPopupService _popupService,
        IAuth _auth) : ObservableObject
    {
        [ObservableProperty]
        private string? _managerEmail;

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.25;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.35;


        [RelayCommand]
        public async Task ValidateManagerEmail()
        {
            if (string.IsNullOrWhiteSpace(ManagerEmail))
                return;

            var (isSuccess, user) = await _auth.IsManagerValid(ManagerEmail.Trim());

            if (isSuccess)
            {
                await Toast.Make("Manager Authorized Action!", ToastDuration.Short).Show();
                await _popupService.ClosePopupAsync(ManagerEmail);
            }
            else
            {
                await Toast.Make("Invalid manager auth.", ToastDuration.Short).Show();
            }

        }
    }
}
