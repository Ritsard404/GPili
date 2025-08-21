
using GPili.Mobile.Utils;
using ServiceLibrary.Services.Interfaces;

namespace GPili.Mobile.Presentation.Features.Manager
{
    [QueryProperty(nameof(ManagerEmail), nameof(ManagerEmail))]
    [QueryProperty(nameof(IsDeveloper), nameof(IsDeveloper))]
    public partial class ManagerViewModel(IInventory _inventory,
        IAuditLog _auditLog, IDatabaseService _databaseService,
        IGPiliTerminalMachine _terminalMachine,
        IEPayment _ePayment,
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
        private string _statusMessage;

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
        private async Task BackCashiering()
        {
            await AppConstant.AddTabMenus();
        }
    }
    public class ButtonData
    {
        public string Image { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
    }
}
