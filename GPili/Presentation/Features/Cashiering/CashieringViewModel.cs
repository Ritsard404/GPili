using CommunityToolkit.Maui.Core.Extensions;
using GPili.Presentation.Popups;
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Order;
using ServiceLibrary.Services.DTO.Payment;
using ServiceLibrary.Services.Interfaces;

namespace GPili.Presentation.Features.Cashiering
{
    public partial class CashieringViewModel(IAuth _auth,
        IPopUpService _popUpService,
        IPopupService _popupService,
        INavigationService _navigationService,
        IOrder _order,
        IInventory _inventory) : ObservableObject
    {

        [ObservableProperty]
        private Product[] _products = [];

        [ObservableProperty]
        private string? _searchProduct;

        [ObservableProperty]
        private ObservableCollection<Item> _items = new();

        [ObservableProperty]
        private InitialItem _currentItem = new();

        [ObservableProperty]
        private ItemTotals _tenders = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPayKeypadSelected))]
        private string _selectedKeypadAction = KeypadActions.QTY; 
        public bool IsPayKeypadSelected => SelectedKeypadAction == KeypadActions.PAY;

        public async Task InitializeAsync()
        {

            bool isCashedDrawer = await _auth.IsCashedDrawer(CashierState.Info.CashierEmail);

            PopupState.PopupInfo.OpenPopup("Set Drawer", "Set drawer cash");

            if (!isCashedDrawer)
            {
                decimal cashValue = 0;
                bool validCash = false;

                do
                {
                    var input = await Shell.Current.DisplayPromptAsync(
                        title: "Cash Drawer",
                        message: "Please enter the amount of cash to store in the drawer (₱1000 or more):",
                        accept: "Store Cash", "", "1000.00", -1, Keyboard.Numeric);

                    if (input == null)
                        continue; // user clicked cancel — keep looping

                    if (decimal.TryParse(input, out cashValue) && cashValue >= 1000)
                    {
                        validCash = true;
                    }
                    else
                    {
                        await Toast.Make("Enter a valid amount of ₱1000 or more.").Show();
                    }
                } while (!validCash);

                await _popUpService.ShowAsync("Loading Products...", true);

                await _auth.SetCashInDrawer(CashierState.Info.CashierEmail!, cashValue);
                await Toast.Make($"₱{cashValue} has been stored in the drawer.").Show();
                isCashedDrawer = true;
            }

            Products = await _inventory.GetProducts();

            await LoadItems();

            await _popUpService.ShowAsync("", false);
            PopupState.PopupInfo.ClosePopup();
        }
        private async Task LoadItems()
        {
            var newItems = await _order.GetPendingItems();

            Items.Clear();
            Items = Tenders.ItemsToPaid = newItems.ToObservableCollection();
        }

        [RelayCommand]
        private async Task Search()
        {
            await _popUpService.ShowAsync("Loading...", true);

            try
            {
                if (string.IsNullOrWhiteSpace(SearchProduct))
                {
                    Products = await _inventory.GetProducts();
                    return;
                }

                var product = await _inventory.GetProductByBarcode(SearchProduct);

                if (product != null)
                {
                    var qty = CurrentItem.InitialQty > 0 ? CurrentItem.InitialQty : 1;
                    var (isSuccess, message) = await _order.AddOrderItem(
                        prodId: product.Id,
                        qty: qty,
                        cashierEmail: CashierState.Info.CashierEmail!);

                    // Optionally handle isSuccess/message here if needed

                    return;
                }

                Products = await _inventory.SearchProducts(SearchProduct);
            }
            finally
            {
                await _popUpService.ShowAsync("Loaded", false);
            }
        }


        [RelayCommand]
        private async Task AddItem(Product? product)
        {
            if (product is null)
                return;

            var (isSuccess, message) = await _order.AddOrderItem(
                prodId: product.Id,
                qty: CurrentItem.InitialQty <= 0 ? 1 : CurrentItem.InitialQty,
                cashierEmail: CashierState.Info.CashierEmail!);

            if (!isSuccess)
            {
                await Toast.Make(message).Show();
                return;
            }

            ClearQty();
            await LoadItems();
        }

        [RelayCommand]
        private async Task SelectItem(Item? item)
        {
            if (item is null)
                return;

            try
            {
                var popup = new EditItemView(item);
                var result = await Shell.Current.ShowPopupAsync(popup);
                if (result is bool b && b)
                {
                    await LoadItems();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Toast.Make("An error occurred while selecting the item.").Show();
            }
        }

        [RelayCommand]
        private void AddPresetQty(string content)
        {
            var presetAmounts = new[] { "20", "50", "100", "200", "500", "1000" };

            if (SelectedKeypadAction == KeypadActions.QTY)
            {
                if (content == ".")
                {
                    if (!CurrentItem.QtyBuffer.Contains("."))
                        CurrentItem.QtyBuffer += ".";
                }
                else
                {
                    CurrentItem.QtyBuffer += content;
                }

                if (decimal.TryParse(CurrentItem.QtyBuffer, out decimal preset))
                {
                    CurrentItem.InitialQty = preset;
                }
            }
            else if (SelectedKeypadAction == KeypadActions.PAY)
            {
                if (presetAmounts.Contains(content))
                {
                    // Add the value to the current cash tender amount
                    if (decimal.TryParse(content, out decimal addValue))
                    {
                        Tenders.CashTenderAmount += addValue;
                        Tenders.PayBuffer = Tenders.CashTenderAmount.ToString();
                    }
                }
                else if (content == ".")
                {
                    if (!Tenders.PayBuffer.Contains("."))
                        Tenders.PayBuffer += ".";
                }
                else
                {
                    Tenders.PayBuffer += content;
                }

                if (decimal.TryParse(Tenders.PayBuffer, out decimal payValue))
                {
                    Tenders.CashTenderAmount = payValue;
                }
            }
        }


        [RelayCommand]
        private async Task PayOrder(string payContent)
        {

            await _popUpService.ShowAsync("Paying...", true);

            if (payContent == KeypadActions.EXACT_PAY)
                Tenders.SetExactCashAmount();

            if (payContent == KeypadActions.ENTER && Tenders.ChangeAmount < 0)
            {
                await Toast.Make("Please enter a valid amount to pay.").Show();
                await _popUpService.ShowAsync("Paid", false);
                return;
            }

            var payOrder = new PayOrderDTO
            {
                CashierEmail = CashierState.Info.CashierEmail!,
                CashTendered = Tenders.CashTenderAmount,
                OtherPayment = Tenders.HasOtherPayments ? Tenders.OtherPayments.ToList() : new(),
                ChangeAmount = Tenders.ChangeAmount,
                DueAmount = Tenders.AmountDue,
                TotalAmount = Tenders.TotalAmount,
                SubTotal = Tenders.SubTotal,
                DiscountAmount = Tenders.DiscountAmount,
                VatExempt = Tenders.VatExemptSales,
                VatSales = Tenders.VatSales,
                VatAmount = Tenders.VatAmount,
                VatZero = Tenders.VatZero,
                TotalTendered = Tenders.TenderAmount
            };

            var result = await _order.PayOrder(payOrder);
            if (result.isSuccess)
            {
                await Toast.Make("Order paid successfully!").Show();

                await LoadItems();
                Products = await _inventory.GetProducts();
                ClearQty();
                SelectedKeypadAction = KeypadActions.QTY;
                Tenders.Discount = null;
            }
            else
            {
                await Toast.Make(result.message).Show();
            }

            await _popUpService.ShowAsync("Paid", false);
        }

        [RelayCommand]
        private void ClearQty()
        {
            CurrentItem.QtyBuffer = "0";
            CurrentItem.InitialQty = 0;
            Tenders.PayBuffer = "";
            Tenders.CashTenderAmount = 0;
            Tenders.OtherPayments = new();
        }

        [RelayCommand]
        private void SelectKeypadAction(string action)
        {
            SelectedKeypadAction = action;

            if (action == KeypadActions.QTY)
            {

                CurrentItem.QtyBuffer = "1";
                CurrentItem.InitialQty = 1;
                Tenders.PayBuffer = "";
                Tenders.CashTenderAmount = 0;
                return;
            }
            ClearQty();
        }

        [RelayCommand]
        private async Task VoidOrder()
        {
            try
            {

                var result = await _popupService.ShowPopupAsync<ManagerAuthViewModel>();
                var managerEmail = result as string;

                if (string.IsNullOrWhiteSpace(managerEmail))
                    return;

                await _popUpService.ShowAsync("Processing...", true);

                var (isSuccess, message) = await _order.VoidOrder(cashierEmail: CashierState.Info.CashierEmail!,
                    managerEmail: managerEmail);
                if (isSuccess)
                {
                    await Toast.Make(message).Show();
                    await LoadItems();
                    Tenders.Discount = new();
                }
                else
                {
                    await Toast.Make(message).Show();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

            }
            finally
            {
                await _popUpService.ShowAsync("Voided", false);
            }
        }

        [RelayCommand]
        private async Task EPayments()
        {
            if (!Items.Any())
            {
                await Shell.Current.DisplayAlert(
                    "Nothing to Pay",
                    "There are no pending items or payments at the moment. Select an order before proceeding.",
                    "OK"
                );
                return;
            }

            var popup = new EPaymentView();
            var result = await Shell.Current.ShowPopupAsync(popup);
            if (result is ObservableCollection<EPaymentDTO> payments && payments.Any())
            {
                Tenders.OtherPayments = payments;
            }
        }

        [RelayCommand]
        private async Task Discount()
        {
            if (!Items.Any())
            {
                await Shell.Current.DisplayAlert(
                    "No Eligible Items",
                    "There are no items available for a discount. Please select an order before applying a discount.",
                    "OK"
                );
                return;
            }

            if (Tenders.DiscountAmount > 0)
            {
                await Shell.Current.DisplayAlert(
                    "Discount Already Applied",
                    "A discount has already been applied to this order.",
                    "OK"
                );
                return;
            }

            var popupResult = await _popupService.ShowPopupAsync<ManagerAuthViewModel>();
            var managerEmail = popupResult as string;

            if (string.IsNullOrWhiteSpace(managerEmail))
                return;

            var popup = new DiscountView();
            var result = await Shell.Current.ShowPopupAsync(popup);
            if (result is DiscountDTO discount)
            {
                Tenders.Discount = discount;
            }
        }

        [RelayCommand]
        private async Task Manager()
        {
            await _navigationService.GoToManager();
        }
    }
}
