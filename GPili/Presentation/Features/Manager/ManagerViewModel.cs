using GPili.Presentation.Popups;
using GPili.Presentation.Popups.Manager;
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using System.ComponentModel.DataAnnotations;

#if WINDOWS
using WinRT;
using static ServiceLibrary.Utils.FolderPath;
#endif

namespace GPili.Presentation.Features.Manager
{
    [QueryProperty(nameof(ManagerEmail), nameof(ManagerEmail))]
    [QueryProperty(nameof(IsDeveloper), nameof(IsDeveloper))]
    public partial class ManagerViewModel(IInventory _inventory,
        IAuditLog _auditLog, IDatabaseService _databaseService,
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

        // POS Terminal 
        public double PopupSettingsWidth => Shell.Current.CurrentPage.Width * 0.4;
        public double PopupSettingsHeight => Shell.Current.CurrentPage.Height * 0.8;

        [ObservableProperty]
        private TerminalConfiguration? _terminalConfig;
        [ObservableProperty]
        private bool _isSettingsDisplay = false;

        [RelayCommand]
        private async Task SettingsDisplay()
        {
            //IsLoading = true;
            //var vm = new TerminalMachineViewModel(_terminalMachine);
            //var popup = new TerminalMachinePopup(vm);
            //var result = await Shell.Current.ShowPopupAsync(popup);
            IsLoading = true;
            if (!IsSettingsDisplay)
            {
                var posInfo = await _terminalMachine.GetTerminalInfo();
                if (posInfo != null)
                {
                    TerminalConfig = new TerminalConfiguration
                    {
                        PosSerialNumber = posInfo.PosSerialNumber,
                        MinNumber = posInfo.MinNumber,
                        AccreditationNumber = posInfo.AccreditationNumber,
                        PtuNumber = posInfo.PtuNumber,
                        DateIssued = posInfo.DateIssued,
                        ValidUntil = posInfo.ValidUntil,
                        PosName = posInfo.PosName,
                        RegisteredName = posInfo.RegisteredName,
                        OperatedBy = posInfo.OperatedBy,
                        Address = posInfo.Address,
                        VatTinNumber = posInfo.VatTinNumber,
                        Vat = posInfo.Vat,
                        DiscountMax = posInfo.DiscountMax,
                        CostCenter = posInfo.CostCenter,
                        BranchCenter = posInfo.BranchCenter,
                        UseCenter = posInfo.UseCenter,
                        DbName = posInfo.DbName,
                        PrinterName = posInfo.PrinterName,
                        IsRetailType = posInfo.IsRetailType,

                    };
                }
                else
                {
                    TerminalConfig = new TerminalConfiguration();

                }
            }

            IsSettingsDisplay = !IsSettingsDisplay;

            IsLoading = false;
        }
        [RelayCommand]
        private async Task SaveSettings()
        {
            if (TerminalConfig is null)
            {
                return;
            }

            TerminalConfig.ValidateAll();

            if (TerminalConfig.HasErrors)
            {
                return;
            }

            var info = new PosTerminalInfo
            {
                AccreditationNumber = TerminalConfig.AccreditationNumber,
                Address = TerminalConfig.Address,
                BranchCenter = TerminalConfig.BranchCenter,
                CostCenter = TerminalConfig.CostCenter,
                DateIssued = TerminalConfig.DateIssued,
                DbName = TerminalConfig.DbName,
                DiscountMax = TerminalConfig.DiscountMax,
                MinNumber = TerminalConfig.MinNumber,
                OperatedBy = TerminalConfig.OperatedBy,
                PtuNumber = TerminalConfig.PtuNumber,
                PosName = TerminalConfig.PosName,
                PosSerialNumber = TerminalConfig.PosSerialNumber,
                RegisteredName = TerminalConfig.RegisteredName,
                UseCenter = TerminalConfig.UseCenter,
                Vat = TerminalConfig.Vat,
                VatTinNumber = TerminalConfig.VatTinNumber,
                ValidUntil = TerminalConfig.ValidUntil,
                PrinterName = TerminalConfig.PrinterName,
                IsRetailType = TerminalConfig.IsRetailType
            };

            var (isSuccess, message) = await _terminalMachine.SetPosTerminalInfo(info);

            if (isSuccess)
            {
                await Snackbar.Make(message,
                    duration: TimeSpan.FromSeconds(1)).Show();

                POSInfo.Terminal = await _terminalMachine.GetTerminalInfo();
                IsSettingsDisplay = false;
            }
        }

        [RelayCommand]
        private void CloseSettings()
        {
            IsSettingsDisplay = false;
        }

        [RelayCommand]
        private async Task Products()
        {
            IsLoading = true;

            var products = await _inventory.GetProducts();
            var categories = await _inventory.GetCategories();

            var IsRestoType = !POSInfo.Terminal.IsRetailType;

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
                    {"IsRestoType", IsRestoType },
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
        private bool _isSaleTypesDisplay = false;

        [ObservableProperty]
        private List<SaleType> _saleTypes = new();

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
                IsLoading = false;
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
        public double PopupReportWidth => Shell.Current.CurrentPage.Width * 0.95;
        public double PopupReportHeight => Shell.Current.CurrentPage.Height * 0.9;

        [ObservableProperty]
        private bool _fakePopupReportDisplay = false;
        [ObservableProperty]
        private bool _salesHistoryDisplay = false;
        [ObservableProperty]
        private bool _auditTrailDisplay = false;
        [ObservableProperty]
        private bool _dailyTranxDisplay = false;
        [ObservableProperty]
        private bool _voidedListDisplay = false;
        [ObservableProperty]
        private bool _salesBookDisplay = false;
        [ObservableProperty]
        private bool _pwdOrScDisplay = false;

        [ObservableProperty]
        private bool _isBusy = false;

        // Sales Report
        [ObservableProperty]
        private List<SalesReportDTO> _salesReports = new();

        [ObservableProperty]
        private TotalSalesReportDTO _totalSalesReports;

        // Audit Trail
        [ObservableProperty]
        private List<AuditTrailDTO> _auditTrail= new();

        // Tranx List
        [ObservableProperty]
        private List<TransactionListDTO> _tranxList = new();

        [ObservableProperty]
        private TotalTransactionListDTO _totalTranxList;

        // Sales Book
        [ObservableProperty]
        private List<Reading> _salesBook = new();

        // Voided List
        [ObservableProperty]
        private List<VoidedListDTO> _voidedList = new();

        [ObservableProperty]
        private TotalVoidedListDTO _totalVoidedList;

        // Pwd or Senior List
        [ObservableProperty]
        private List<TransactionListDTO> _pwdOrSeniorList = new();

        [ObservableProperty]
        private TotalTransactionListDTO _totalPwdOrSeniorList;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchReportCommand))]
        private string _discountType = "PWD";

        // Title
        [ObservableProperty]
        private string _reportTitle;

        [RelayCommand]
        private async Task ToggleTransaclists()
        {
            IsBusy = true;
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
            IsBusy = false;
        }
        [RelayCommand]
        private async Task ToggleReport(string reportType)
        {
            IsLoading = true;

            From = DateTime.Now;
            To = DateTime.Now.AddDays(1);

            // Reset all display flags to false
            SalesHistoryDisplay = false;
            AuditTrailDisplay = false;
            SalesBookDisplay = false;
            DailyTranxDisplay = false;
            VoidedListDisplay = false;
            PwdOrScDisplay = false;

            switch (reportType)
            {
                case "SalesHistory":
                    var getSalesReports = await _report.GetSalesReportData(From, To);
                    TotalSalesReports = getSalesReports.totalSalesReport;
                    SalesReports = getSalesReports.salesReports;
                    SalesHistoryDisplay = true;
                    break;

                case "AuditTrail":
                    AuditTrail = await _report.GetAuditTrailData(From, To);
                    AuditTrailDisplay = true;
                    break;

                case "SalesBook":
                    SalesBook = await _report.GetSalesBookData(From, To);
                    SalesBookDisplay = true;
                    break;

                case "TranxList":
                    var getTranxList = await _report.GetTransactListData(From, To);
                    TotalTranxList = getTranxList.Item2;
                    TranxList = getTranxList.Item1;
                    DailyTranxDisplay = true;
                    break;

                case "VoidedList":
                    var getVoidedList = await _report.GetVoidedListsData(From, To);
                    TotalVoidedList = getVoidedList.totalVoidedList;
                    VoidedList = getVoidedList.voidedOrdersLists;
                    VoidedListDisplay = true;
                    break;

                case "PwdOrSeniorList":
                    var getPwdOrSeniorList = await _report.GetPwdOrSeniorData(From, To, DiscountType);
                    PwdOrSeniorList = getPwdOrSeniorList.Item1;
                    TotalPwdOrSeniorList = getPwdOrSeniorList.Item2;
                    PwdOrScDisplay = true;
                    break;
            }

            SetReportTitle();

            FakePopupReportDisplay = !FakePopupReportDisplay;

            IsLoading = false;
        }

        [RelayCommand]
        private async Task SearchReport()
        {
            IsBusy = true;

            if (SalesHistoryDisplay)
            {
                var getSalesReports = await _report.GetSalesReportData(From, To);
                TotalSalesReports = getSalesReports.totalSalesReport;
                SalesReports = getSalesReports.salesReports;
            }
            else if (AuditTrailDisplay)
            {
                AuditTrail = await _report.GetAuditTrailData(From, To);
            }
            else if (SalesBookDisplay)
            {
                SalesBook = await _report.GetSalesBookData(From, To);
            }
            else if (DailyTranxDisplay)
            {
                var getTranxList = await _report.GetTransactListData(From, To);
                TotalTranxList = getTranxList.Item2;
                TranxList = getTranxList.Item1;
            }
            else if (VoidedListDisplay)
            {
                var getVoidedList = await _report.GetVoidedListsData(From, To);
                TotalVoidedList = getVoidedList.totalVoidedList;
                VoidedList = getVoidedList.voidedOrdersLists;
            }
            else if (PwdOrScDisplay)
            {
                var getPwdOrSeniorList = await _report.GetPwdOrSeniorData(From, To, DiscountType);
                PwdOrSeniorList = getPwdOrSeniorList.Item1;
                TotalPwdOrSeniorList = getPwdOrSeniorList.Item2;
            }

            IsBusy = false;
        }
        private void SetReportTitle()
        {
            if (SalesHistoryDisplay)
                ReportTitle = "Sales History Report";
            else if (AuditTrailDisplay)
                ReportTitle = "Audit Trail Report";
            else if (SalesBookDisplay)
                ReportTitle = "Sales Book Report";
            else if (DailyTranxDisplay)
                ReportTitle = "Daily Transactions Report";
            else if (VoidedListDisplay)
                ReportTitle = "Voided Transactions Report";
            else if (PwdOrScDisplay)
                ReportTitle = DiscountType == "PWD" ? "PWD Discount Report" : "Senior Citizen Discount Report";
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

            //var vm = new SelectionOfDateViewModel(_popupService, isRangeMode: true);
            //var popup = new DateSelectionPopup(vm);
            //var result = await Shell.Current.ShowPopupAsync(popup);

            //if (result is ValueTuple<DateTime, DateTime> range)
            //{
                //var fromDate = range.Item1;
                //var toDate = range.Item2;

                var print = await _report.GetTransactList(From, To);

                await Shell.Current.DisplayAlert("Transaction List Printed",
                    $"File Path: {print.FilePath}",
                    "OK");
            //}

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

            //var vm = new SelectionOfDateViewModel(_popupService, isRangeMode: true);
            //var popup = new DateSelectionPopup(vm);
            //var result = await Shell.Current.ShowPopupAsync(popup);

            //if (result is ValueTuple<DateTime, DateTime> range)
            //{
            //    var fromDate = range.Item1;
            //    var toDate = range.Item2;

            //    var filePath = await _report.GetSalesReport(fromDate, toDate);

            //    await Shell.Current.DisplayAlert("Sales Report Printed",
            //        $"File Path: {filePath}",
            //        "OK");
            //}

            var filePath = await _report.GetSalesReport(From, To);

            await Shell.Current.DisplayAlert("Sales Report Printed",
                $"File Path: {filePath}",
                "OK");

            IsLoading = false;
        }

        [RelayCommand]
        private async Task PrintSalesBook()
        {
            IsLoading = true;

            //var vm = new SelectionOfDateViewModel(_popupService, isRangeMode: true);
            //var popup = new DateSelectionPopup(vm);
            //var result = await Shell.Current.ShowPopupAsync(popup);

            //if (result is ValueTuple<DateTime, DateTime> range)
            //{
            //    var fromDate = range.Item1;
            //    var toDate = range.Item2;

                var filePath = await _report.GetSalesBook(From, To);

                await Shell.Current.DisplayAlert("Sales Book Printed",
                    $"File Path: {filePath}",
                    "OK");
            //}

            IsLoading = false;
        }

        [RelayCommand]
        private async Task PrintVoidedLists()
        {
            IsLoading = true;

            //var vm = new SelectionOfDateViewModel(_popupService, isRangeMode: true);
            //var popup = new DateSelectionPopup(vm);
            //var result = await Shell.Current.ShowPopupAsync(popup);

            //if (result is ValueTuple<DateTime, DateTime> range)
            //{
            //    var fromDate = range.Item1;
            //    var toDate = range.Item2;

                var filePath = await _report.GetVoidedListsReport(From, To);

                await Shell.Current.DisplayAlert("Voided Lists Printed",
                    $"File Path: {filePath}",
                    "OK");
            //}

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
                // Inject and call your database reset service here
                await _databaseService.ResetDatabaseAsync(); // For example
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


    }
    public partial class TerminalConfiguration : ObservableValidator
    {
        // POS machine details
        [Required(ErrorMessage = "POS Serial Number is required")]
        [ObservableProperty]
        private string _posSerialNumber = string.Empty;

        [Required(ErrorMessage = "MIN Number is required")]
        [ObservableProperty]
        private string _minNumber = string.Empty;

        [Required(ErrorMessage = "Accreditation Number is required")]
        [ObservableProperty]
        private string _accreditationNumber = string.Empty;

        [Required(ErrorMessage = "PTU Number is required")]
        [ObservableProperty]
        private string _ptuNumber = string.Empty;

        [Required(ErrorMessage = "Date Issued is required")]
        [ObservableProperty]
        private DateTime _dateIssued;

        [Required(ErrorMessage = "Valid Until date is required")]
        [ObservableProperty]
        private DateTime _validUntil;

        // Business details
        [Required(ErrorMessage = "POS Name is required")]
        [ObservableProperty]
        private string _posName = string.Empty;

        [Required(ErrorMessage = "Registered Name is required")]
        [ObservableProperty]
        private string _registeredName = string.Empty;

        [Required(ErrorMessage = "Operated By is required")]
        [ObservableProperty]
        private string _operatedBy = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [ObservableProperty]
        private string _address = string.Empty;

        [Required(ErrorMessage = "VAT TIN Number is required")]
        [ObservableProperty]
        private string _vatTinNumber = string.Empty;

        [Required(ErrorMessage = "VAT percentage is required")]
        [Range(0, int.MaxValue, ErrorMessage = "VAT must be a non-negative number")]
        [ObservableProperty]
        private int _vat;

        [Required(ErrorMessage = "Discount Max is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Discount Max must be a non-negative value")]
        [ObservableProperty]
        private decimal _discountMax;

        // API Flags
        [Required(ErrorMessage = "Cost Center is required")]
        [ObservableProperty]
        private string _costCenter = string.Empty;

        [Required(ErrorMessage = "Branch Center is required")]
        [ObservableProperty]
        private string _branchCenter = string.Empty;

        [Required(ErrorMessage = "Use Center is required")]
        [ObservableProperty]
        private string _useCenter = string.Empty;

        [Required(ErrorMessage = "Database Name is required")]
        [ObservableProperty]
        private string _dbName = string.Empty;

        [Required(ErrorMessage = "Printer Name is required")]
        [ObservableProperty]
        private string _printerName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PosTypeName))]
        private bool _isRetailType;
        public string PosTypeName => IsRetailType ? "Retail POS" : "Restaurant POS";

        /// <summary>
        /// Call this method to validate all properties.
        /// </summary>
        public void ValidateAll()
        {
            ValidateAllProperties();
        }
    }

}
