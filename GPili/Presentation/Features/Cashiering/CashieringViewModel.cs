﻿using CommunityToolkit.Maui.Core.Extensions;
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
                        title: "Cash In Drawer",
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
                        await Snackbar.Make("Enter a valid amount of ₱1000 or more.", duration: TimeSpan.FromSeconds(1)).Show();
                    }
                } while (!validCash);

                await _popUpService.ShowAsync("Loading Products...", true);

                await _auth.SetCashInDrawer(CashierState.Info.CashierEmail!, cashValue);
                await Snackbar.Make($"₱{cashValue} has been stored in the drawer.", duration: TimeSpan.FromSeconds(1)).Show();
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

                if (product != null && SelectedKeypadAction != KeypadActions.PLU)
                {
                    var qty = CurrentItem.InitialQty > 0 ? CurrentItem.InitialQty : 1;
                    var (isSuccess, message) = await _order.AddOrderItem(
                        prodId: product.Id,
                        qty: qty,
                        cashierEmail: CashierState.Info.CashierEmail!);


                    Products = await _inventory.GetProducts();

                    await LoadItems();
                    SearchProduct = string.Empty;
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
                await Snackbar.Make(message, duration: TimeSpan.FromSeconds(1)).Show();
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
                await Snackbar.Make("An error occurred while selecting the item.", duration: TimeSpan.FromSeconds(1)).Show();
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
                await Snackbar.Make("Please enter a valid amount to pay.", duration: TimeSpan.FromSeconds(1)).Show();
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
                TotalTendered = Tenders.TenderAmount,
                GrossAmount = Tenders.GrossTotal,
                Discount = Tenders.Discount
            };

            var result = await _order.PayOrder(payOrder);
            if (result.isSuccess)
            {
                await Snackbar.Make("Order paid successfully!", 
                    duration: TimeSpan.FromSeconds(1)).Show();

                await LoadItems();
                Products = await _inventory.GetProducts();
                ClearQty();
                SelectedKeypadAction = KeypadActions.QTY;
                Tenders.Discount = null;
            }
            else
            {
                await Snackbar.Make(result.message, duration: TimeSpan.FromSeconds(1)).Show();

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


                var reason = await Shell.Current.DisplayPromptAsync(
                    title: "Void Order",
                    message: "Please enter the reason for the void:",
                    accept: "Submit",
                    cancel: "Not specified",
                    placeholder: "e.g., Damaged item, Wrong order",
                    keyboard: Keyboard.Text
                );

                //if (string.IsNullOrWhiteSpace(reason))
                //{
                //    await Shell.Current.DisplayAlert(
                //        title: "Invalid Input",
                //        message: "Reason for void cannot be empty. Please try again.",
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

                await _popUpService.ShowAsync("Processing...", true);

                var (isSuccess, message) = await _order.VoidOrder(cashierEmail: CashierState.Info.CashierEmail!,
                    managerEmail: managerEmail, reason: reason);
                if (isSuccess)
                {
                    await Snackbar.Make(message,
                        duration: TimeSpan.FromSeconds(1)).Show();
                    await LoadItems();
                    Tenders.Discount = new();
                }
                else
                {
                    await Snackbar.Make(message,
                        duration: TimeSpan.FromSeconds(1)).Show();
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
                await Snackbar.Make("There are no pending items or payments at the moment. " +
                    "Select an order before proceeding.",
                    duration: TimeSpan.FromSeconds(1)).Show();
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
                await Snackbar.Make("There are no items available for a discount. Please select an order before applying a discount.",
                    duration: TimeSpan.FromSeconds(1)).Show();
                return;
            }

            if (Tenders.DiscountAmount > 0)
            {
                await Snackbar.Make("A discount has already been applied to this order.",
                    duration: TimeSpan.FromSeconds(1)).Show();
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
            if (Items.Any())
            {
                var cts = new CancellationTokenSource();
                _ = Task.Delay(1000).ContinueWith(_ => cts.Cancel());

                await Snackbar.Make("Cashier has pending item/s. Action denied.",
                    duration: TimeSpan.FromSeconds(1)).Show();
                return;
            }

            var result = await _popupService.ShowPopupAsync<ManagerAuthViewModel>();

            if (result is not string managerEmail || string.IsNullOrWhiteSpace(managerEmail))
                return;

            await _navigationService.GoToManager();
        }
    }
}
