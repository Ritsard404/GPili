using GPili.Presentation.Popups;
using GPili.Presentation.Popups.Manager;
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using WinRT.Interop;

namespace GPili.Presentation.Features.Manager
{
    [QueryProperty(nameof(ManagerEmail), nameof(ManagerEmail))]
    [QueryProperty(nameof(IsDeveloper), nameof(IsDeveloper))]
    public partial class ManagerViewModel(IInventory _inventory,
        IAuditLog _auditLog,
        IAuth _auth,
        IGPiliTerminalMachine _terminalMachine,
        IEPayment _ePayment,
        IReport _report, IOrder _order,
        IPopupService _popupService,
        INavigationService _navigationService,
        IPrinterService _printer) : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCashiering))]
        private string? _managerEmail;

        public bool IsCashiering => !string.IsNullOrEmpty(ManagerEmail);

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ModeText))]
        [NotifyPropertyChangedFor(nameof(ModeButtonColor))]
        private bool _isTrainingMode = POSInfo.Terminal.IsTrainMode;

        public string ModeText => IsTrainingMode ? "Training Mode" : "Live Mode";
        public Color ModeButtonColor => IsTrainingMode ? Colors.Orange : Colors.Green;

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.8;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.8;

        // Refund
        public double PopupRefundWidth => Shell.Current.CurrentPage.Width * 0.5;
        public double PopupRefundHeight => Shell.Current.CurrentPage.Height * 0.8;

        [ObservableProperty]
        private bool _isDeveloper = false;

        [ObservableProperty]
        private bool _isDisplayTransactLists = false;

        // Refund
        [ObservableProperty]
        private bool _isRefundDisplay = false;
        [ObservableProperty]
        private long _invId;
        [ObservableProperty]
        private List<Item> _toRefundItems = new();
        [ObservableProperty]
        private List<Item> _toSelectedRefundItems = new();

        // Reports
        [ObservableProperty]
        private DateTime _from = DateTime.Now;
        [ObservableProperty]
        private DateTime _to = DateTime.Now.AddDays(1);
        [ObservableProperty]
        private List<GetInvoiceDocumentDTO> _transactLists = new();

        // Sale Types
        [ObservableProperty]
        private bool _isSaleTypesDisplay = false;
        [ObservableProperty]
        private List<SaleType> _saleTypes = new();


        [RelayCommand]
        private async Task LoadData()
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Snackbar.Make("No internet connection. Please check your network.", duration: TimeSpan.FromSeconds(1)).Show();
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
                await Snackbar.Make("Data loaded successfully.", duration: TimeSpan.FromSeconds(1)).Show();
            }
        }

        [RelayCommand]
        private async Task PushJournal()
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Snackbar.Make("No internet connection. Please check your network.", duration: TimeSpan.FromSeconds(1)).Show();
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
                    await Shell.Current.DisplayAlert("Success", "Data pushed successfully.", "OK");
                }

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
                    await Snackbar.Make("Cashier logged out successfully.", duration: TimeSpan.FromSeconds(1)).Show();
                    ManagerEmail = null;
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

        [RelayCommand]
        private async Task GoBack()
        {
            await _navigationService.GoBack();
        }

        [RelayCommand]
        private async Task Settings()
        {
            IsLoading = true;

            var popup = new TerminalMachinePopup();
            var result = await Shell.Current.ShowPopupAsync(popup);

            IsLoading = false;
        }

        [RelayCommand]
        private async Task Products()
        {
            IsLoading = true;

            var products = await _inventory.GetProducts();
            var categories = await _inventory.GetCategories();

            if (categories.Length == 0)
            {
                await Shell.Current.DisplayAlert("Error", "No categories found.", "OK");
                IsLoading = false;
                return;
            }

            await _navigationService.NavigateToAsync(AppRoutes.ProductPage,
                new Dictionary<string, object>
                {
                    {"Products", products },
                    {"Categories", categories },
                    {"ManagerEmail", ManagerEmail },
                });

            IsLoading = false;
        }

        [RelayCommand]
        private async Task Users()
        {
            IsLoading = true;

            var users = await _auth.Users();

            await _navigationService.NavigateToAsync(AppRoutes.UsersPage,
                new Dictionary<string, object>
                {
                    {"Users", users },
                    {"ManagerEmail", ManagerEmail },
                });

            IsLoading = false;
        }

        [RelayCommand]
        private async Task Categories()
        {
            IsLoading = true;

            var categories = await _inventory.GetCategories();

            var popup = new CategoriesView(categories: categories, managerEmail: ManagerEmail);
            var result = await Shell.Current.ShowPopupAsync(popup);

            IsLoading = false;
        }

        [RelayCommand]
        private async Task ChangeMode()
        {
            IsLoading = true;
            if (!string.IsNullOrEmpty(ManagerEmail))
            {
                var result = await _terminalMachine.ChangeMode(ManagerEmail);

                IsTrainingMode = result;
                POSInfo.Terminal = await _terminalMachine.GetTerminalInfo();
            }

            IsLoading = false;
        }

        // Sale Types
        [ObservableProperty]
        private bool _isAddSaleTypeDisplay = false;
        [ObservableProperty]
        private SaleType _newSaleType;
        public double PopupSaleTypeWidth => Shell.Current.CurrentPage.Width * 0.4;
        public double PopupSaleTypeHeight => Shell.Current.CurrentPage.Height * 0.6;


        [RelayCommand]
        private void ToggleAddSalesType()
        {
            IsLoading = true;


            if (!IsAddSaleTypeDisplay)
            {
                // When opening the popup, initialize NewSaleType
                NewSaleType = new SaleType { Name = "", Type = "", Account = "" };
            }

            IsAddSaleTypeDisplay = !IsAddSaleTypeDisplay;
            IsLoading = false;
        }
        [RelayCommand]
        private async Task ToggleSalesType()
        {
            IsLoading = true;
            SaleTypes.Clear();
            SaleTypes = await _ePayment.SaleTypes();

            IsSaleTypesDisplay = !IsSaleTypesDisplay;
            IsLoading = false;
        }
        [RelayCommand]
        private async Task UpdateSaleType(SaleType saleType)
        {
            IsLoading = true;

            var (isSuccess, message) = await _ePayment.UpdateSaleType(saleType, ManagerEmail!);
            if (isSuccess)
            {
                SaleTypes.Clear();
                SaleTypes = await _ePayment.SaleTypes();

            }
            else
            {
                await Shell.Current.DisplayAlert("Error", message, "Ok");
            }
            IsLoading = false;
        }
        [RelayCommand]
        private async Task RemoveSaleType(SaleType saleType)
        {
            IsLoading = true;

            var (isSuccess, message) = await _ePayment.DeleteSaleType(saleType.Id, ManagerEmail!);
            if (isSuccess)
            {
                SaleTypes.Clear();
                SaleTypes = await _ePayment.SaleTypes();

            }
            else
            {
                await Shell.Current.DisplayAlert("Error", message, "Ok");
            }
            IsLoading = false;
        }
        
        [RelayCommand]
        private async Task AddSaleType()
        {
            IsLoading = true;

            var (isSuccess, message) = await _ePayment.AddSaleType(NewSaleType, ManagerEmail!);
            if (isSuccess)
            {
                SaleTypes.Clear();
                SaleTypes = await _ePayment.SaleTypes();
                IsAddSaleTypeDisplay = false;

            }
            else
            {
                await Shell.Current.DisplayAlert("Error", message, "Ok");
            }
            IsLoading = false;
        }

        // Refund
        [RelayCommand]
        private void ToggleRefundInvoice()
        {
            IsLoading = true;

            ToRefundItems.Clear();
            ToSelectedRefundItems.Clear();
            InvId = 0;

            IsRefundDisplay = !IsRefundDisplay;
            IsLoading = false;
        }
        [RelayCommand]
        private async Task SearchRefundItems()
        {
            IsLoading = true;
            ToRefundItems.Clear();
            ToSelectedRefundItems.Clear();
            var items = await _order.GetToRefundItems(invNum: InvId);


            if (items == null || !items.Any())
            {
                await Shell.Current.DisplayAlert(
                    "Not found!",
                    "Invalid Invoice.",
                    "OK");
                return;
            }

            ToRefundItems = items;
            IsLoading = false;
        }
        [RelayCommand]
        private async Task RefundItems()
        {
            IsLoading = true;
            if (!ToSelectedRefundItems.Any())
                return;

            var reason = await Shell.Current.DisplayPromptAsync(
                title: "Return Item",
                message: "Please enter the reason for the return:",
                accept: "Submit",
                cancel: "Not specified",
                placeholder: "e.g., Damaged item, Wrong order",
                keyboard: Keyboard.Text
            );

            //if (string.IsNullOrWhiteSpace(reason))
            //{
            //    await Shell.Current.DisplayAlert(
            //        title: "Invalid Input",
            //        message: "Reason for return cannot be empty. Please try again.",
            //        cancel: "OK"
            //    );
            //    IsRefundDisplay = false;
            //    IsLoading = false;
            //    return;
            //}

            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = "Not specified"; // Default reason if none provided
            }


            var (isSuccess, message) = await _order.ReturnItems(ManagerEmail!, InvId, ToSelectedRefundItems, reason);
            if (isSuccess)
            {
                await Shell.Current.DisplayAlert("Refunded", message, "Ok");

                InvId = 0;
                ToRefundItems.Clear();
                ToSelectedRefundItems.Clear();

                IsRefundDisplay = false;
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", message, "Ok");
            }
            IsLoading = false;
        }

        // Reports
        [RelayCommand]
        private async Task ToggleTransaclists()
        {
            IsLoading = true;
            if (!IsDisplayTransactLists)
            {
                TransactLists = await _report.InvoiceDocuments(From, To);
            }
            else
            {
                From = DateTime.Now;
                To = DateTime.Now.AddDays(1);
            }


            IsDisplayTransactLists = !IsDisplayTransactLists;
            IsLoading = false;
        }

        [RelayCommand]
        private async Task SearchInvoices()
        {
            IsLoading = true;
            TransactLists = await _report.InvoiceDocuments(From, To);

            IsLoading = false;
        }

        [RelayCommand]
        private async Task PrintInvoices()
        {
            IsLoading = true;
            TransactLists = await _report.InvoiceDocuments(From, To);
            IsLoading = false;
        }

        [RelayCommand]
        private async Task RePrintInvoice(GetInvoiceDocumentDTO documentDTO)
        {
            IsLoading = true;
            switch (documentDTO.Type)
            {
                case InvoiceDocumentType.Invoice:
                    await _printer.ReprintInvoice(documentDTO.Id);
                    break;

                case InvoiceDocumentType.XReport:
                    await _printer.ReprintPrintXReading(documentDTO.Id);
                    break;

                case InvoiceDocumentType.ZReport:
                    await _printer.ReprintPrintZReading(documentDTO.Id);
                    break;
            }
            IsLoading = false;
        }

        [RelayCommand]
        private async Task PrintTransactionLists()
        {
            IsLoading = true;

            var vm = new SelectionOfDateViewModel(_popupService, isRangeMode: true);
            var popup = new DateSelectionPopup(vm);
            var result = await Shell.Current.ShowPopupAsync(popup);

            if (result is ValueTuple<DateTime, DateTime> range)
            {
                var fromDate = range.Item1;
                var toDate = range.Item2;

                var print = await _report.GetTransactList(fromDate, toDate);

                await Shell.Current.DisplayAlert("Transaction List Printed",
                    $"File Path: {print.FilePath}",
                    "OK");
            }

            IsLoading = false;
        }

        [RelayCommand]
        private async Task PrintAuditTrail()
        {
            IsLoading = true;

            var vm = new SelectionOfDateViewModel(_popupService, isRangeMode: true);
            var popup = new DateSelectionPopup(vm);
            var result = await Shell.Current.ShowPopupAsync(popup);

            if (result is ValueTuple<DateTime, DateTime> range)
            {
                var fromDate = range.Item1;
                var toDate = range.Item2;

                var filePath = await _report.GetAuditTrail(fromDate, toDate);

                await Shell.Current.DisplayAlert("Audit Trail Printed",
                    $"File Path: {filePath}",
                    "OK");
            }

            IsLoading = false;
        }

        [RelayCommand]
        private async Task PrintSalesHistory()
        {
            IsLoading = true;

            var vm = new SelectionOfDateViewModel(_popupService, isRangeMode: true);
            var popup = new DateSelectionPopup(vm);
            var result = await Shell.Current.ShowPopupAsync(popup);

            if (result is ValueTuple<DateTime, DateTime> range)
            {
                var fromDate = range.Item1;
                var toDate = range.Item2;

                var filePath = await _report.GetSalesReport(fromDate, toDate);

                await Shell.Current.DisplayAlert("Sales Report Printed",
                    $"File Path: {filePath}",
                    "OK");
            }

            IsLoading = false;
        }

        [RelayCommand]
        private async Task PrintSalesBook()
        {
            IsLoading = true;

            var vm = new SelectionOfDateViewModel(_popupService, isRangeMode: true);
            var popup = new DateSelectionPopup(vm);
            var result = await Shell.Current.ShowPopupAsync(popup);

            if (result is ValueTuple<DateTime, DateTime> range)
            {
                var fromDate = range.Item1;
                var toDate = range.Item2;

                var filePath = await _report.GetSalesBook(fromDate, toDate);

                await Shell.Current.DisplayAlert("Sales Book Printed",
                    $"File Path: {filePath}",
                    "OK");
            }

            IsLoading = false;
        }

        [RelayCommand]
        private async Task PrintVoidedLists()
        {
            IsLoading = true;

            var vm = new SelectionOfDateViewModel(_popupService, isRangeMode: true);
            var popup = new DateSelectionPopup(vm);
            var result = await Shell.Current.ShowPopupAsync(popup);

            if (result is ValueTuple<DateTime, DateTime> range)
            {
                var fromDate = range.Item1;
                var toDate = range.Item2;

                var filePath = await _report.GetVoidedListsReport(fromDate, toDate);

                await Shell.Current.DisplayAlert("Voided Lists Printed",
                    $"File Path: {filePath}",
                    "OK");
            }

            IsLoading = false;
        }

        [RelayCommand]
        private async Task PrintPwdOrSeniorLists()
        {
            IsLoading = true;

            var vm = new SelectionOfDateViewModel(_popupService, isRangeMode: true, isPwdOrSenior: true);
            var popup = new DateSelectionPopup(vm);
            var result = await Shell.Current.ShowPopupAsync(popup);

            if (result is ValueTuple<DateTime, DateTime, string> range)
            {
                var fromDate = range.Item1;
                var toDate = range.Item2;
                var type = range.Item3;

                var print = await _report.GetPwdOrSeniorList(fromDate, toDate, type);

                await Shell.Current.DisplayAlert($"{type} List Printed",
                    $"File Path: {print.FilePath}",
                    "OK");
            }

            IsLoading = false;
        }

    }
}
