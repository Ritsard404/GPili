using Plugin.NFC;
using ServiceLibrary.Services.Interfaces;

namespace GPili.Mobile.Presentation.Popups
{
    public partial class ManagerAuthViewModel(IPopupService _popupService, IAuth _auth)
        : ObservableObject, IAsyncDisposable
    {
        [ObservableProperty]
        private string? _managerEmail = string.Empty;

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.8;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.35;

        private bool _eventsSubscribed;
        private bool _isDeviceiOS;

        public async Task InitializeAsync()
        {
            if (!CrossNFC.IsSupported)
            {
                await Snackbar.Make("NFC not supported on this device").Show();
                return;
            }

            if (!CrossNFC.Current.IsAvailable || !CrossNFC.Current.IsEnabled)
            {
                await Snackbar.Make("NFC is disabled or unavailable").Show();
                return;
            }

            if (DeviceInfo.Platform == DevicePlatform.iOS)
                _isDeviceiOS = true;

            SubscribeEvents();
            await BeginListening();
        }

        [RelayCommand]
        public async Task ValidateManagerEmail(string? serial = null)
        {
            if (string.IsNullOrWhiteSpace(ManagerEmail) && string.IsNullOrWhiteSpace(serial))
                return;

            var (isSuccess, user) = await _auth.IsManagerValid(ManagerEmail.Trim(), cardId: serial);

            if (isSuccess)
            {
                await _popupService.ClosePopupAsync(user.Email);
            }
            else
            {
                await Toast.Make("Invalid manager email.").Show();
            }
        }

        private void SubscribeEvents()
        {
            if (_eventsSubscribed)
                return;

            _eventsSubscribed = true;

            CrossNFC.Current.OnMessageReceived += OnMessageReceived;

            if (_isDeviceiOS)
                CrossNFC.Current.OniOSReadingSessionCancelled += OniOSReadingSessionCancelled;
        }

        private void UnsubscribeEvents()
        {
            if (!_eventsSubscribed)
                return;

            CrossNFC.Current.OnMessageReceived -= OnMessageReceived;

            if (_isDeviceiOS)
                CrossNFC.Current.OniOSReadingSessionCancelled -= OniOSReadingSessionCancelled;

            _eventsSubscribed = false;
        }

        private void OnMessageReceived(ITagInfo tagInfo)
        {
            if (tagInfo == null)
                return;

            var serial = NFCUtils.ByteArrayToHexString(tagInfo.Identifier, "");
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await ValidateManagerEmail(serial);
            });
        }

        private void OniOSReadingSessionCancelled(object sender, EventArgs e)
        {
            Snackbar.Make("NFC session cancelled").Show();
        }

        private async Task BeginListening()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CrossNFC.Current.StartListening();
                });
            }
            catch (Exception ex)
            {
                await Snackbar.Make($"Error: {ex.Message}").Show();
            }
        }

        private void StopListening()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CrossNFC.Current.StopListening();
                });
            }
            catch { }
        }

        public ValueTask DisposeAsync()
        {
            StopListening();
            UnsubscribeEvents();
            return ValueTask.CompletedTask;
        }
    }
}
