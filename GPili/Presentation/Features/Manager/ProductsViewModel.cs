using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;

namespace GPili.Presentation.Features.Manager
{
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
        private string? _searchProduct;

        public async Task LoadProducts()
        {

            Products = await _inventory.GetProducts();
            Categories = await _inventory.GetCategories();

        }

        [RelayCommand]
        private async Task EditProduct(Product product)
        {
            await Snackbar.Make(product.ProdId).Show();
            Debug.WriteLine(product.ProdId);
            Debug.WriteLine(product.ProdId);
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


    }
}
