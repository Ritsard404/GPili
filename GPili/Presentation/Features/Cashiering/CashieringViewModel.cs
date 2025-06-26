using CommunityToolkit.Maui.Alerts;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using System.Collections.ObjectModel;

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
        private decimal _qty = 1;

        [ObservableProperty]
        private InitialItem _currentItem;

        [ObservableProperty]
        private ItemTotals _tenders = new();

        public async Task InitializeAsync()
        {

            bool isCashedDrawer =  await _auth.IsCashedDrawer(CashierState.Info.CashierEmail);

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
            if (product is null || Qty <= 0)
                return;

            var (isSuccess, message) = await _order.AddOrderItem(
                prodId: product.Id,
                qty: Qty,
                cashierEmail: CashierState.Info.CashierEmail!);

            if (!isSuccess)
            {
                await Toast.Make(message).Show();
                return;
            }

            Items = await _order.GetPendingItems();
            Tenders.ItemsToPaid = new ObservableCollection<Item>(Items);
        }
    }
}
