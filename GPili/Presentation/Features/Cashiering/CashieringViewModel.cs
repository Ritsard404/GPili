using CommunityToolkit.Maui.Alerts;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace GPili.Presentation.Features.Cashiering
{
    public partial class CashieringViewModel(IAuth _auth,
        ILoaderService _loaderService,
        INavigationService _navigationService,
        IOrder _order,
        IInventory _inventory) : ObservableObject
    {

        [ObservableProperty]
        private Product[] _products = [];

        [ObservableProperty]
        private string? _searchProduct;

        [ObservableProperty]
        private List<Item> _items = [];

        [ObservableProperty]
        private InitialItem _currentItem = new();

        [ObservableProperty]
        private ItemTotals _tenders = new();

        [ObservableProperty]
        private string _selectedKeypadAction = KeypadActions.QTY;

        public async Task InitializeAsync()
        {

            bool isCashedDrawer = await _auth.IsCashedDrawer(CashierState.Info.CashierEmail);

            await Task.Delay(1000);

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

                await _loaderService.ShowAsync("Loading Products...", true);

                await _auth.SetCashInDrawer(CashierState.Info.CashierEmail!, cashValue);
                await Toast.Make($"₱{cashValue} has been stored in the drawer.").Show();
                isCashedDrawer = true;
            }

            Products = await _inventory.GetProducts();

            Items = await _order.GetPendingItems();
            Tenders.ItemsToPaid = new ObservableCollection<Item>(Items);

            await _loaderService.ShowAsync("", false);
        }

        [RelayCommand]
        private async Task Search()
        {
            if (!string.IsNullOrWhiteSpace(SearchProduct))
            {
                Products = await _inventory.SearchProducts(keyword: SearchProduct);
            }
            else
            {
                Products = await _inventory.GetProducts();
            }
        }

        [RelayCommand]
        private async Task AddItem(Product? product)
        {
            if (product is null || CurrentItem.InitialQty <= 0)
                return;

            var (isSuccess, message) = await _order.AddOrderItem(
                prodId: product.Id,
                qty: CurrentItem.InitialQty,
                cashierEmail: CashierState.Info.CashierEmail!);

            if (!isSuccess)
            {
                await Toast.Make(message).Show();
                return;
            }

            Items = await _order.GetPendingItems();
            Tenders.ItemsToPaid = new ObservableCollection<Item>(Items);
        }

        [RelayCommand]
        private void AddPresetQty(string content)
        {
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

            if (SelectedKeypadAction == KeypadActions.PAY)
            {
                if (content == ".")
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
        private void ClearQty()
        {
            CurrentItem.QtyBuffer = "";
            CurrentItem.InitialQty = 0;
            Tenders.PayBuffer = "";
            Tenders.CashTenderAmount = 0;
        }

        [RelayCommand]
        private void SelectKeypadAction(string action)
        {
            SelectedKeypadAction = action;
            CurrentItem.QtyBuffer = "";
            CurrentItem.InitialQty = 0;
            Tenders.PayBuffer = "";
            Tenders.CashTenderAmount = 0;
            Debug.Print($"Selected Keypad Action: {SelectedKeypadAction}");
        }
    
    
    }
}
