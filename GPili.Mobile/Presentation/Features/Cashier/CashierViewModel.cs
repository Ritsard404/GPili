using CommunityToolkit.Maui.Core.Extensions;
using GPili.Mobile.Presentation.Features.Cashiering;
using GPili.Mobile.Presentation.Popups;
using GPili.Mobile.Utils;
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Order;
using ServiceLibrary.Services.DTO.Payment;
using ServiceLibrary.Services.Interfaces;

namespace GPili.Mobile.Presentation.Features.Cashier
{
    public partial class CashierViewModel(IAuth _auth,
        IPopupService _popupService,
        INavigationService _navigationService,
        IOrder _order,
        IInventory _inventory) : ObservableObject
    {

        [ObservableProperty]
        private Product[] _products = [];

        [ObservableProperty]
        private CategoryObservable[] _categories = [];

        [ObservableProperty]
        private CategoryObservable _selectedCategory;

        [ObservableProperty]
        private string? _searchProduct;

        [ObservableProperty]
        private bool _isRetail;

        [ObservableProperty]
        private ObservableCollection<Item> _items = new();

        [ObservableProperty]
        private InitialItem _currentItem = new();

        [ObservableProperty]
        private ItemTotals _tenders = new();

        [ObservableProperty]
        private bool _isLoading = true;



        public async Task InitializeAsync()
        {
            IsRetail = POSInfo.Terminal.IsRetailType;

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
                        message: "Please enter the amount of cash to store in the drawer (₱100 or more):",
                        accept: "Store Cash", "", "100.00", -1, Keyboard.Numeric);

                    if (input == null)
                        continue; // user clicked cancel — keep looping

                    if (decimal.TryParse(input, out cashValue) && cashValue >= 100)
                    {
                        validCash = true;
                    }
                    else
                    {
                        await Snackbar.Make("Enter a valid amount of ₱100 or more.", duration: TimeSpan.FromSeconds(1)).Show();
                    }
                } while (!validCash);

                IsLoading = true;

                await _auth.SetCashInDrawer(App.UserInfo.Email!, cashValue);
                await Snackbar.Make($"₱{cashValue} has been stored in the drawer.", duration: TimeSpan.FromSeconds(1)).Show();
                isCashedDrawer = true;
                PopupState.PopupInfo.ClosePopup();
            }

            if (!IsRetail)
            {

                var categories = await _inventory.GetCategories();

                Categories = categories.Select(c => new CategoryObservable
                {
                    Id = c.Id,
                    CtgryName = c.CtgryName,
                    IsSelected = false
                }).ToArray();

                Categories[0].IsSelected = true;
                SelectedCategory = Categories[0];

                Products = await _inventory.GetProductsByCategory(SelectedCategory.Id);
            }
            else
            {
                Products = await _inventory.GetProducts();
            }

            await LoadItems();

            IsLoading = false;
        }
        private async Task LoadItems()
        {
            var newItems = await _order.GetPendingItems();

            Items.Clear();
            Items = Tenders.ItemsToPaid = newItems.ToObservableCollection();
        }

        [RelayCommand]
        private async Task IncreaseQty(Item item)
        {
            if (item == null) return;

            item.Qty++;
            await LoadItems();
        }

        [RelayCommand]
        private async Task DecreaseQty(Item item)
        {
            if (item == null) return;

            if (item.Qty > 1)
            {
                item.Qty--;
            }
            else
            {
                // Optional: remove item from cart when qty hits 0
                Items.Remove(item);
            }

            await LoadItems();
        }

        [RelayCommand]
        private async Task Search()
        {
            IsLoading = true;

            try
            {
                if (string.IsNullOrWhiteSpace(SearchProduct))
                {
                    Products = await _inventory.GetProducts();
                    return;
                }

                var product = await _inventory.GetProductByBarcode(SearchProduct);

                if (product != null )
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
                IsLoading = false;
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
        private async Task NavigateToCart()
        {
            await _navigationService.NavigateToAsync(AppRoutes.Cart);
        }

        [RelayCommand]
        private async Task SelectCategory(CategoryObservable category)
        {
            if (SelectedCategory.Id == category.Id)
                return;
            IsLoading = true;

            var existSelectedCategory = Categories.First(c => c.IsSelected);
            existSelectedCategory.IsSelected = false;

            var newSelectedCategory = Categories.First(c => c.Id == category.Id);
            newSelectedCategory.IsSelected = true;

            SelectedCategory = newSelectedCategory;

            Products = await _inventory.GetProductsByCategory(category.Id);
            OnPropertyChanged(nameof(Products));

            IsLoading = false;
        }

        [RelayCommand]
        private async Task SelectItem(Item? item)
        {
            if (item is null)
                return;

            try
            {
                var result = await _popupService.ShowPopupAsync<EditItemViewModel>(
                     vm => vm.Initialize(item)
                 );

                if (result is true)
                {
                    await LoadItems();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.ToString(), "OK");
            }
        }

        private async Task<bool> RequestBluetoothAndLocationPermissions()
        {
            var permissionsToRequest = new List<string>();

            if (OperatingSystem.IsAndroidVersionAtLeast(12))
            {
                if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(Platform.CurrentActivity!, Android.Manifest.Permission.BluetoothConnect) != Android.Content.PM.Permission.Granted)
                    permissionsToRequest.Add(Android.Manifest.Permission.BluetoothConnect);

                if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(Platform.CurrentActivity!, Android.Manifest.Permission.BluetoothScan) != Android.Content.PM.Permission.Granted)
                    permissionsToRequest.Add(Android.Manifest.Permission.BluetoothScan);
            }

            // Location is required for scanning Bluetooth devices
            if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(Platform.CurrentActivity!, Android.Manifest.Permission.AccessFineLocation) != Android.Content.PM.Permission.Granted)
                permissionsToRequest.Add(Android.Manifest.Permission.AccessFineLocation);

            if (permissionsToRequest.Count > 0)
            {
                AndroidX.Core.App.ActivityCompat.RequestPermissions(
                    Platform.CurrentActivity!,
                    permissionsToRequest.ToArray(),
                    2001
                );

                await Task.Delay(500); // Give time for user response

                // Verify again
                foreach (var perm in permissionsToRequest)
                {
                    if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(Platform.CurrentActivity!, perm) != Android.Content.PM.Permission.Granted)
                        return false; // Permission denied
                }
            }

            return true;
        }

        [RelayCommand]
        private async Task PayOrder(string payContent)
        {
            IsLoading = true;


            try
            {
                bool permissionsGranted = await RequestBluetoothAndLocationPermissions();

                // 2️⃣ If denied, guide user to Settings
                if (!permissionsGranted)
                {
                    bool openSettings = await Shell.Current.DisplayAlert(
                        "Permissions Required",
                        "Bluetooth and Location permissions are required for printing receipts. Please allow them to continue.",
                        "Open Settings", "Cancel");

                    if (openSettings)
                    {
                        var intent = new Android.Content.Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
                        intent.SetData(Android.Net.Uri.Parse($"package:{Platform.CurrentActivity!.PackageName}"));
                        Platform.CurrentActivity!.StartActivity(intent);

                        await Snackbar.Make("Please enable Bluetooth and Location permissions in settings.", duration: TimeSpan.FromSeconds(3)).Show();
                    }
                }

                var bluetoothAdapter = Android.Bluetooth.BluetoothAdapter.DefaultAdapter;
                if (bluetoothAdapter != null && !bluetoothAdapter.IsEnabled)
                {
                    var enableBluetooth = await Shell.Current.DisplayAlert(
                        "Bluetooth Offline",
                        "Bluetooth is required for printing receipts. Would you like to enable Bluetooth?",
                        "Enable", "Cancel");

                    if (enableBluetooth)
                    {
                        if (await RequestBluetoothAndLocationPermissions())
                        {
                            var intent = new Android.Content.Intent(Android.Bluetooth.BluetoothAdapter.ActionRequestEnable);
                            Platform.CurrentActivity!.StartActivityForResult(intent, 1001);
                            await Snackbar.Make("Please enable Bluetooth in the prompt...", duration: TimeSpan.FromSeconds(2)).Show();
                            await Task.Delay(2000);
                        }
                    }
                    else
                    {
                        await Snackbar.Make("Payment completed. Receipt was not printed.", duration: TimeSpan.FromSeconds(2)).Show();
                    }
                }


                if (payContent == KeypadActions.EXACT_PAY)
                    Tenders.SetExactCashAmount();

                if (payContent == KeypadActions.ENTER && Tenders.ChangeAmount < 0)
                {
                    await Toast.Make("Please enter a valid amount to pay.").Show();
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
                    await Snackbar.Make("Order paid successfully!", duration: TimeSpan.FromSeconds(1)).Show();

                    Products = IsRetail
                        ? await _inventory.GetProducts()
                        : await _inventory.GetProductsByCategory(SelectedCategory.Id);

                    await LoadItems();
                    ClearQty();
                    Tenders.Discount = null;
                }
                else
                {
                    await Snackbar.Make(result.message, duration: TimeSpan.FromSeconds(1)).Show();
                }
            }
            catch (Exception ex)
            {
                // Optional: Log the exception or report it to your error tracking system
                await Shell.Current.DisplayAlert("Error", $"An error occurred while processing the payment: {ex.Message}", "OK");

            }
            finally
            {
                IsLoading = false;
            }
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

                IsLoading = true;

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

                var (isSuccess, message) = await _order.VoidOrder(cashierEmail: CashierState.Info.CashierEmail!,
                    managerEmail: managerEmail, reason: reason, pay: payOrder);
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
                IsLoading = false;
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

                await Shell.Current.DisplayAlert("Action Denied!", "Cashier has pending item/s.", "OK");
                return;
            }

            var result = await _popupService.ShowPopupAsync<ManagerAuthViewModel>();

            if (result is not string managerEmail || string.IsNullOrWhiteSpace(managerEmail))
                return;

            //await _navigationService.GoToManager();

            await AppConstant.AddTabManager();
        }
    }
}
