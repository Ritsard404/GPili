
using GPili.Mobile.Presentation.Popups;
using GPili.Mobile.Presentation.Popups.Manager;
using GPili.Mobile.Utils;

namespace GPili.Mobile.Presentation.Features.Manager
{
    public partial class ManagerViewModel(IInventory _inventory,
        IAuditLog _auditLog, IDatabaseService _databaseService,
        IGPiliTerminalMachine _terminalMachine,
        IEPayment _ePayment, IAuth _auth,
        IReport _report, IOrder _order,
        IPopupService _popupService,
        INavigationService _navigationService,
        IPrinterService _printer) : ObservableObject
    {
        [ObservableProperty]
        private bool _isDeveloper = false;


        [ObservableProperty]
        private bool _isLoading = false;
        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private DateTime _from = DateTime.Now;
        [ObservableProperty]
        private DateTime _to = DateTime.Now.AddDays(1);

        [RelayCommand]
        private async Task Navigate(string route)
        {
            if (!string.IsNullOrWhiteSpace(route))
                await _navigationService.NavigateToAsync(route);
        }
        #region Sales



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
                    await Snackbar.Make("Enter a valid amount.", duration: TimeSpan.FromSeconds(1)).Show();
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
                    await Snackbar.Make(message, duration: TimeSpan.FromSeconds(1)).Show();
                }
                else
                {
                    await Snackbar.Make($"Withdraw failed: {message}", duration: TimeSpan.FromSeconds(1)).Show();
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

        // ZReading
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
        #endregion

        #region Data
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressPercent))]
        [NotifyPropertyChangedFor(nameof(IsLoaderOnly))]
        private decimal _progressValue;
        public string ProgressPercent => $"{(int)(ProgressValue * 100)}%";
        public bool IsLoaderOnly => ProgressValue <= 0;

        [RelayCommand]
        private async Task NavigatePageType(string type)
        {
            IsLoading = true;
            try
            {
                var managerEmail = App.UserInfo?.ManagerEmail ?? App.UserInfo?.Email;

                (string route, Dictionary<string, object>? parameters) result;

                switch (type)
                {
                    case "Users":
                        result = (
                            AppRoutes.User,
                            new Dictionary<string, object>
                            {
                        { "Users", await _auth.Users() },
                        { "ManagerEmail", managerEmail }
                            }
                        );
                        break;

                    case "Products":
                        var products = await _inventory.GetProducts();
                        var categories = await _inventory.GetCategories();

                        if (categories.Length == 0)
                        {
                            await Shell.Current.DisplayAlert("Error", "No categories found.", "OK");
                            return;
                        }

                        result = (
                            AppRoutes.Product,
                            new Dictionary<string, object>
                            {
                        { "Products", products },
                        { "Categories", categories },
                        { "ManagerEmail", managerEmail },
                        { "IsRestoType", !POSInfo.Terminal.IsRetailType }
                            }
                        );
                        break;

                    case "Categories":
                        result = (
                            AppRoutes.Category,
                            new Dictionary<string, object>
                            {
                        { "Categories", await _inventory.GetCategories() },
                        { "ManagerEmail", managerEmail }
                            }
                        );
                        break;

                    default:
                        throw new ArgumentException($"Unknown navigation type: {type}", nameof(type));
                }

                await _navigationService.NavigateToAsync(result.route, result.parameters!);
            }
            finally
            {
                IsLoading = false;
            }
        }


        //[RelayCommand]
        //private async Task Users()
        //{
        //    IsLoading = true;

        //    var users = await _auth.Users();
        //    var managerEmail = App.UserInfo != null && App.UserInfo.ManagerEmail == null ? App.UserInfo.Email : App.UserInfo.ManagerEmail;

        //    await _navigationService.NavigateToAsync(AppRoutes.User,
        //        new Dictionary<string, object>
        //        {
        //            {"Users", users },
        //            {"ManagerEmail", managerEmail },
        //        });

        //    IsLoading = false;
        //}

        //[RelayCommand]
        //private async Task Products()
        //{
        //    IsLoading = true;

        //    var products = await _inventory.GetProducts();
        //    var categories = await _inventory.GetCategories(); 
        //    var managerEmail = App.UserInfo != null && App.UserInfo.ManagerEmail == null ? App.UserInfo.Email : App.UserInfo.ManagerEmail;


        //    var IsRestoType = !POSInfo.Terminal.IsRetailType;

        //    if (categories.Length == 0)
        //    {
        //        await Shell.Current.DisplayAlert("Error", "No categories found.", "OK");
        //        IsLoading = false;
        //        return;
        //    }

        //    await _navigationService.NavigateToAsync(AppRoutes.Product,
        //        new Dictionary<string, object>
        //        {
        //            {"Products", products },
        //            {"Categories", categories },
        //            {"ManagerEmail", managerEmail },
        //            {"IsRestoType", IsRestoType },
        //        });

        //    IsLoading = false;
        //}

        //[RelayCommand]
        //private async Task Categories()
        //{
        //    IsLoading = true;

        //    var categories = await _inventory.GetCategories();
        //    var managerEmail = App.UserInfo != null && App.UserInfo.ManagerEmail == null ? App.UserInfo.Email : App.UserInfo.ManagerEmail;

        //    await _navigationService.NavigateToAsync(AppRoutes.Category,
        //        new Dictionary<string, object>
        //        {
        //            {"Categories", categories },
        //            {"ManagerEmail", managerEmail },
        //        });

        //    IsLoading = false;
        //}

        [RelayCommand]
        private async Task LoadData()
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Toast.Make("No internet connection. Please check your network.").Show();
                return;
            }
            // Ask “Are you sure?” with two buttons
            bool loadConfirmed = await Shell.Current.DisplayAlert(
                title: "Load Data",
                message: "Do you want to load the data now?",
                accept: "Yes",   // returns true
                cancel: "No"     // returns false
            );

            // If they tapped “No” (or pressed back), we bail out
            if (!loadConfirmed)
                return;

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
                    await Toast.Make(message).Show();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Unexpected error occurred.";
            }
            finally
            {
                IsLoading = false;
                ProgressValue = 0;
                await Toast.Make("Data loaded successfully.").Show();
            }
        }
        [RelayCommand]
        private async Task PushJournal()
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Toast.Make("No internet connection. Please check your network.").Show();
                return;
            }

            var vm = new SelectionOfDateViewModel(_popupService, isRangeMode: false);
            var popup = new DateSelectionPopup(vm);
            var result = await Shell.Current.ShowPopupAsync(popup);

            if (result is DateTime date)
            {
                try
                {
                    IsLoading = true;

                    var progress = new Progress<(int current, int total, string status)>(report =>
                    {
                        StatusMessage = report.status;
                        ProgressValue = report.total > 0 ? (decimal)report.current / report.total : 0;
                    });

                    var (success, message) = await _auditLog.PushJournals(date, progress);

                    StatusMessage = message;

                    if (!success)
                    {
                        await Toast.Make(message).Show();
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = "Unexpected error occurred.";
                }
                finally
                {
                    IsLoading = false;
                    ProgressValue = 0;
                    await Shell.Current.DisplayAlert("Success", "Data pushed successfully.", "OK");
                }

            }

        }
        [RelayCommand]
        private async Task ResetDatabase()
        {
            bool confirmed = await Shell.Current.DisplayAlert(
                "Confirm Reset",
                "This will delete all data and reset the database to its initial state. Are you sure you want to proceed?",
                "Reset",
                "Cancel"
            );

            if (!confirmed)
                return;

            IsLoading = true;

            try
            {
                await _databaseService.ResetDatabaseAsync();
                await Shell.Current.DisplayAlert("Success", "Database has been reset successfully.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to reset database:\n{ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion


        [RelayCommand]
        private async Task BackCashiering()
        {
            App.UserInfo.ManagerEmail = null;
            await AppConstant.AddTabMenus();
        }

        [RelayCommand]
        private async Task BackLogIn()
        {
            await _navigationService.NavigateToAsync(AppRoutes.Login);
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
                    accept: "Submit Cash", "", "100.00", -1, Keyboard.Numeric);

                if (decimal.TryParse(input, out cashValue) && cashValue <= 0)
                {
                    await Shell.Current.DisplayAlert("Error", "Cash value must be greater than zero.", "OK");
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
                    await Toast.Make("Cashier logged out successfully.").Show();
                    App.UserInfo = null;
                    await _navigationService.Logout();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Logout failed", $"{message}", "OK");

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

    }
}
