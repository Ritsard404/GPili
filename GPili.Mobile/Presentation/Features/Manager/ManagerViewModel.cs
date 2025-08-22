
using GPili.Mobile.Presentation.Popups;
using GPili.Mobile.Utils;
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;

namespace GPili.Mobile.Presentation.Features.Manager
{
    [QueryProperty(nameof(ManagerEmail), nameof(ManagerEmail))]
    [QueryProperty(nameof(IsDeveloper), nameof(IsDeveloper))]
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
        private string? _managerEmail;

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


        [RelayCommand]
        private async Task BackCashiering()
        {
            await AppConstant.AddTabMenus();
        }

        [RelayCommand]
        private async Task BackLogIn()
        {
            await _navigationService.NavigateToAsync(AppRoutes.Login);
        }
    }
}
