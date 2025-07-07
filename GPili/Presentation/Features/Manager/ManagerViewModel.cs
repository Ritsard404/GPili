using GPili.Presentation.Popups;
using ServiceLibrary.Services.Interfaces;

namespace GPili.Presentation.Features.Manager
{
    [QueryProperty(nameof(ManagerEmail), nameof(ManagerEmail))]
    public partial class ManagerViewModel(IInventory _inventory,
        IAuditLog _auditLog,
        IAuth _auth,
        IEPayment _ePayment,
        IReport _report,
        IPopupService _popupService,
        INavigationService _navigationService,
        IPrinterService _printer) : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCashiering))]
        private string? _managerEmail;

        public bool IsCashiering => !string.IsNullOrEmpty(ManagerEmail);

        [ObservableProperty]
        private DateTime _dateToPushJournal = DateTime.Now;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressPercent))]
        [NotifyPropertyChangedFor(nameof(IsLoaderOnly))]
        private decimal _progressValue;
        public string ProgressPercent => $"{(int)(ProgressValue * 100)}%";

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private bool _isLoading = false;
        public bool IsLoaderOnly => ProgressValue <= 0;

        [RelayCommand]
        private async Task LoadData()
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlert("Error", "No internet connection. Please check your network.", "OK");
                return;
            }

            try
            {
                IsLoading = true;

                var progress = new Progress<(int current, int total, string status)>(report =>
                {
                    StatusMessage = report.status;
                    ProgressValue = report.total > 0 ? (decimal)report.current / report.total : 0;
                });

                var (success, message) = await _inventory.LoadOnlineProducts(progress);

                StatusMessage = message;

                if (!success)
                {
                    // Handle error state here if needed
                    Debug.WriteLine("Failed to load products.");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Unexpected error occurred.";
                Debug.WriteLine($"Error loading data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                ProgressValue = 0;
                await Shell.Current.DisplayAlert("Error", "Data loaded successfully.", "OK");
            }
        }

        [RelayCommand]
        private async Task PushJournal()
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlert("Error", "No internet connection. Please check your network.", "OK");
                return;
            }

            try
            {
                IsLoading = true;

                var progress = new Progress<(int current, int total, string status)>(report =>
                {
                    StatusMessage = report.status;
                    ProgressValue = report.total > 0 ? (decimal)report.current / report.total : 0;
                });

                var (success, message) = await _auditLog.PushJournals(DateToPushJournal, progress);

                StatusMessage = message;

                if (!success)
                {
                    // Handle error state here if needed
                    Debug.WriteLine("Failed to Push.");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Unexpected error occurred.";
                Debug.WriteLine($"Error pushing data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                ProgressValue = 0;
                await Shell.Current.DisplayAlert("Error", "Data pushed successfully.", "OK");
            }
        }

        [RelayCommand]
        private async Task CashTrack()
        {
            try
            {
                IsLoading = true;


                var (cashInDrawer, currentCashDrawer, cashierName)
                    = await _report.CashTrack(CashierState.Info.CashierEmail!);

                _printer.PrintCashTrack(CashInDrawer: cashInDrawer,
                    CurrentCashDrawer: currentCashDrawer,
                    cashierName: CashierState.Info.CashierName);

                StatusMessage = "Loading...";

            }
            catch (Exception ex)
            {
                StatusMessage = "Unexpected error occurred.";
                Debug.WriteLine($"Error pushing data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CashWithdraw()
        {
            try
            {
                var result = await _popupService.ShowPopupAsync<ManagerAuthViewModel>();
                var managerEmail = result as string;

                if (string.IsNullOrWhiteSpace(managerEmail))
                    return;

                decimal cashValue = 0;
                //bool validCash = false;

                //do
                //{
                    var input = await Shell.Current.DisplayPromptAsync(
                        title: "Cash Withdraw",
                        message: "Please enter the total amount of cash in the drawer:",
                        accept: "Submit Cash", "", "0.00", -1, Keyboard.Numeric);

                    //if (input == null)
                    //    continue; // user clicked cancel — keep looping

                    if (decimal.TryParse(input, out cashValue))
                    {
                        //validCash = true;
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error", "Enter a valid amount.", "OK");
                    return;
                }
                //} while (!validCash);

                IsLoading = true;

                var (isSuccess, message) = await _auth.CashWithdrawDrawer(
                    CashierState.Info.CashierEmail!,
                    managerEmail,
                    cashValue);

                if (isSuccess)
                {
                    await Shell.Current.DisplayAlert("Error", message, "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", $"Withdraw failed: {message}", "OK");
                }

            }
            catch (Exception ex)
            {
                StatusMessage = "Unexpected error occurred.";
                Debug.WriteLine($"Error pushing data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ZReading()
        {
            try
            {
                IsLoading = true;

                await _printer.PrintZReading();

                StatusMessage = "Loading...";
            }
            catch (Exception ex)
            {
                StatusMessage = "Unexpected error occurred.";
                Debug.WriteLine($"Error pushing data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        [RelayCommand]
        private async Task LogoutCashier()
        {
            try
            {

                var result = await _popupService.ShowPopupAsync<ManagerAuthViewModel>();
                var managerEmail = result as string;

                if (string.IsNullOrWhiteSpace(managerEmail))
                    return;

                decimal cashValue = 0;
                //bool validCash = false;

                //do
                //{
                //    var input = await Shell.Current.DisplayPromptAsync(
                //        title: "Cash Out Drawer",
                //        message: "Please enter the total amount of cash in the drawer:",
                //        accept: "Submit Cash", "", "1000.00", -1, Keyboard.Numeric);

                //    if (input == null)
                //        continue; // user clicked cancel — keep looping

                //    if (decimal.TryParse(input, out cashValue) && cashValue >= 1000)
                //    {
                //        validCash = true;
                //    }
                //    else
                //    {
                //        await Toast.Make("Enter a valid amount of ₱1000 or more.", ToastDuration.Short).Show();
                //    }
                //} while (!validCash);

                var input = await Shell.Current.DisplayPromptAsync(
                    title: "Cash Out Drawer",
                    message: "Please enter the total amount of cash in the drawer:",
                    accept: "Submit Cash", "", "1000.00", -1, Keyboard.Numeric);

                if (decimal.TryParse(input, out cashValue) && cashValue < 1000)
                {
                    await Shell.Current.DisplayAlert("Error", "Enter a valid amount of ₱1000 or more.", "OK");
                    return;
                }

                IsLoading = true;

                var (isSuccess, message) = await _auth.LogOut(
                    cashierEmail: CashierState.Info.CashierEmail!,
                    managerEmail: managerEmail,
                    cash: cashValue);

                if (isSuccess)
                {
                    await _printer.PrintXReading();
                    await Shell.Current.DisplayAlert("Error", "Cashier logged out successfully.", "OK");
                    ManagerEmail = null;
                    await _navigationService.Logout();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", $"Logout failed: {message}", "OK");

                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Unexpected error occurred.";
                Debug.WriteLine($"Error pushing data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    
        [RelayCommand]
        private async Task GoBack()
        {
            await _navigationService.GoBack();
        }
    
    }
}
