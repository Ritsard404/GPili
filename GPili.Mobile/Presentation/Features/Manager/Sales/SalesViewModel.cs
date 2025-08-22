using GPili.Mobile.Presentation.Popups;
using ServiceLibrary.Utils;

namespace GPili.Mobile.Presentation.Features.Manager.Sales
{
    public partial class SalesViewModel(IReport _report, IOrder _order, 
        IPopupService _popupService,IPrinterService _printer) : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private DateTime _from = DateTime.Now;
        [ObservableProperty]
        private DateTime _to = DateTime.Now.AddDays(1);

        //Transaction Lists
        [ObservableProperty]
        private bool _isDisplayTransactLists = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TransactionListTitle))]
        private List<GetInvoiceDocumentDTO> _transactLists = new();

        public string TransactionListTitle => $"Transaction Lists ({TransactLists.Count})";


        public async Task InitializeTransactLists()
        {
            IsLoading = true;
            try
            {
                TransactLists = await _report.InvoiceDocuments(From, To);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SearchInvoices()
        {
            await InitializeTransactLists();
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

        // Refund
        [ObservableProperty]
        private bool _isRefundDisplay = false;
        [ObservableProperty]
        private long _invId;
        [ObservableProperty]
        private List<Item> _toRefundItems = new();
        [ObservableProperty]
        private List<Item> _toSelectedRefundItems = new();

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
            if (!ToSelectedRefundItems.Any())
                return;

            var result = await _popupService.ShowPopupAsync<ManagerAuthViewModel>();
            var managerEmail = result as string;

            if (string.IsNullOrWhiteSpace(managerEmail))
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

            IsLoading = true;

            var (isSuccess, message) = await _order.ReturnItems(managerEmail, InvId, ToSelectedRefundItems, reason);
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
    }
}
