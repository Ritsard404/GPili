using GPili.Mobile.Utils;
using GPili.Mobile.Utils.State;
using Plugin.NFC;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;

namespace GPili.Mobile.Presentation.Features.LogIn;

public partial class LogInViewModel(
    IAuth _auth,
    IGPiliTerminalMachine _terminalMachine,
    INavigationService _navigationService) : ObservableObject, IAsyncDisposable
{
    private bool _eventsAlreadySubscribed = false;
    private bool _isDeviceiOS = false;

    [ObservableProperty]
    private string _adminEmail = string.Empty;

    [ObservableProperty]
    private User? _selectedCashier;

    [ObservableProperty]
    private User[] _cashiers = [];

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isNfcSupported = false;

    public async ValueTask InitializeAsync()
    {
        IsLoading = true;
        while (true)
        {
            var (isValid, message) = await _terminalMachine.ValidateTerminalExpiration();

            if (!isValid)
            {
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
        SelectedCashier = Cashiers.FirstOrDefault();

        await InitializeNfcAsync();

        IsLoading = false;
    }

    private async Task InitializeNfcAsync()
    {
        CrossNFC.Legacy = false;

        if (CrossNFC.IsSupported)
        {
            if (!CrossNFC.Current.IsAvailable)
            {
                await Snackbar.Make("NFC is not available").Show();
                IsNfcSupported = false;
                return;
            }

            if (!CrossNFC.Current.IsEnabled)
            {
                await Snackbar.Make("NFC is disabled").Show();
                return;
            }

            if (DeviceInfo.Platform == DevicePlatform.iOS)
                _isDeviceiOS = true;

            IsNfcSupported = true;
            await AutoStartAsync();
        }
    }

    async Task AutoStartAsync()
    {
        await Task.Delay(500); // Avoid Android dispatch crash
        await StartListeningIfNotiOS();
    }

    void SubscribeEvents()
    {
        if (_eventsAlreadySubscribed)
            UnsubscribeEvents();

        _eventsAlreadySubscribed = true;

        CrossNFC.Current.OnMessageReceived += Current_OnMessageReceived;
        CrossNFC.Current.OnTagDiscovered += (_, _) => Snackbar.Make("NFC Tag Discovered").Show();

        if (_isDeviceiOS)
            CrossNFC.Current.OniOSReadingSessionCancelled += (_, _) => Snackbar.Make("iOS NFC session cancelled").Show();
    }

    void UnsubscribeEvents()
    {
        CrossNFC.Current.OnMessageReceived -= Current_OnMessageReceived;

        if (_isDeviceiOS)
            CrossNFC.Current.OniOSReadingSessionCancelled -= (_, _) => { };

        _eventsAlreadySubscribed = false;
    }

    void Current_OnMessageReceived(ITagInfo tagInfo)
    {
        if (tagInfo == null)
        {
            Snackbar.Make("No NFC tag found").Show();
            return;
        }

        var serialNumber = NFCUtils.ByteArrayToHexString(tagInfo.Identifier, "");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await LogIn(serialNumber);
        });
    }

    async Task StartListeningIfNotiOS()
    {
        if (_isDeviceiOS)
        {
            SubscribeEvents();
            return;
        }
        await BeginListening();
    }

    async Task BeginListening()
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SubscribeEvents();
                CrossNFC.Current.StartListening();
            });
        }
        catch (Exception ex)
        {
            await Snackbar.Make($"Error starting NFC: {ex.Message}").Show();
        }
    }

    void StopListening()
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CrossNFC.Current.StopListening();
                UnsubscribeEvents();
            });
        }
        catch (Exception ex)
        {
            Snackbar.Make($"Error stopping NFC: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LogIn(string? cardId = null)
    {
        IsLoading = true;

        try
        {
            // Fallback: use AdminEmail if no NFC card
            var managerKey = string.IsNullOrWhiteSpace(cardId) ? AdminEmail : cardId;

            var (isSuccess, role, email, name, message) =
                await _auth.LogIn(managerKey, SelectedCashier?.Email ?? string.Empty, cardId);

            if (!isSuccess)
            {
                await Toast.Make(message).Show();
                return;
            }

            var userDetails = new UserInfo
            {
                Email = email,
                Name = name,
                Role = role
            };
            await DisposeAsync();

            App.UserInfo = userDetails;
            CashierState.Info.UpdateCashierInfo(name, email, role);
            await AppConstant.AddTabMenus();
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
            var logDir = Android.OS.Environment
                .GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments)
                .AbsolutePath;
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "login-error.txt");
            File.WriteAllText(logPath, error);

            await Shell.Current.DisplayAlert("Login Error", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
            AdminEmail = string.Empty;
            SelectedCashier = Cashiers.FirstOrDefault();
        }
    }

    public ValueTask DisposeAsync()
    {
        Task.Run(() => StopListening());
        return ValueTask.CompletedTask;
    }
}
