using GPili.Presentation.Popups.Manager;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;

namespace GPili.Presentation.Features.Manager
{
    [QueryProperty(nameof(Products), nameof(Products))]
    [QueryProperty(nameof(Categories), nameof(Categories))]
    [QueryProperty(nameof(ManagerEmail), nameof(ManagerEmail))]
    public partial class ProductsViewModel(IInventory _inventory,
        INavigationService _navigation) : ObservableObject
    {

        public double PopupWidth => Shell.Current.CurrentPage.Width;
        public double PopupHeight => Shell.Current.CurrentPage.Height;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProductCount))]
        private Product[] _products = [];

        [ObservableProperty]
        private Category[] _categories = [];
        public int ProductCount => Products.Length;

        [ObservableProperty]
        private Product? _selectedProduct;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _managerEmail;

        [ObservableProperty]
        private string? _searchProduct;

        public async Task LoadProducts()
        {
            //Products = await _inventory.GetProducts();
            //Categories = await _inventory.GetCategories();

        }

        [RelayCommand]
        private async Task EditProduct(Product product)
        {
            IsLoading = true;
            var popup = new SaveProduct(isEdit: false, category: Categories,
                managerEmail: ManagerEmail, product: product);
            var result = await Shell.Current.ShowPopupAsync(popup);
            //Products = await _inventory.GetProducts();
            IsLoading = false;
        }

        [RelayCommand]
        public async Task AddProduct()
        {
            IsLoading = true;
            var popup = new SaveProduct(isEdit:false, category: Categories, 
                managerEmail: ManagerEmail);
            var result = await Shell.Current.ShowPopupAsync(popup);
            //Products = await _inventory.GetProducts();
            IsLoading = false;
        }
        [RelayCommand]
        public async Task Search()
        {
            IsLoading = true;

            try
            {
                if (string.IsNullOrWhiteSpace(SearchProduct))
                {
                    Products = await _inventory.GetProducts();
                    return;
                }

                Products = await _inventory.SearchProducts(SearchProduct);
            }
            finally
            {
                IsLoading = false;
                SearchProduct = null;
            }
        }
        [RelayCommand]
        public async Task Back()
        {
            await _navigation.GoBack();
        }


        [RelayCommand]
        private async Task PrintBarcodes()
        {
            IsLoading = true;
            await _inventory.GetProductBarcodes();
            await Shell.Current.DisplayAlert("Success", "Product added successfully.", "OK");
            IsLoading = false;
        }

    }
}
