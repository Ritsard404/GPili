using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace GPili.Presentation.Popups
{
    public partial class EditItemViewModel(IPopupService _popupService,
        IOrder _order) : ObservableValidator
    {
        public EditItemView _popup;

        [ObservableProperty]
        private Item _item;

        [ObservableProperty]
        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        private decimal _qty;

        [ObservableProperty]
        [Required(ErrorMessage = "Subtotal is required.")]
        [Range(0.001, double.MaxValue, ErrorMessage = "Subtotal must be greater than 0.")]
        private decimal _subTotal;

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.3;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.45;

        partial void OnQtyChanged(decimal value)
        {
            ValidateProperty(value, nameof(Qty));
            SubTotal = Math.Round(Item.Price * value, 2);
        }

        partial void OnSubTotalChanged(decimal value) => ValidateProperty(value, nameof(SubTotal));

        [RelayCommand]
        public async Task SaveItem()
        {
            ValidateAllProperties();

            if (HasErrors)
            {
                await Toast.Make("Please correct the errors.", ToastDuration.Short).Show();
                return;
            }

            if (Item.Qty == Qty && Item.SubTotal == SubTotal)
            {
                _popup.CloseWithResult(false);
                return;
            }

            var (isSuccess, message) = await _order.EditQtyTotalPriceItem(itemId: Item.Id, qty: Qty, subtotal: SubTotal);

            if (isSuccess)
            {
                _popup.CloseWithResult(true);
            }
            else
            {
                await Toast.Make(message ?? "Please correct the errors.", ToastDuration.Short).Show();
                return;
            }
        }

        [RelayCommand]
        private async Task VoidItem()
        {
            var result = await _popupService.ShowPopupAsync<ManagerAuthViewModel>();
            var managerEmail = result as string;

            if (string.IsNullOrWhiteSpace(managerEmail))
                return;

            var (isSuccess, message) = await _order.VoidItem(cashrEmail: CashierState.Info.CashierEmail!,
                mgrEmail: managerEmail, itemId: Item.Id);
            if (isSuccess)
            {
                await Toast.Make(message, ToastDuration.Short).Show();
                _popup.CloseWithResult(true);
            }
            else
            {
                await Toast.Make(message, ToastDuration.Short).Show();
                _popup.CloseWithResult(false);
            }
        }
    }
}
