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
        public CategoriesView PopupCategory;

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.5;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.7;

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
        private bool _isRestoType = false;

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
            var popup = new SaveProduct(isEdit: true, category: Categories,
                managerEmail: ManagerEmail, product: product);
            var result = await Shell.Current.ShowPopupAsync(popup); 
            
            if (result is bool boolResult && boolResult)
            {
                Products = await _inventory.GetProducts();
            }
            IsLoading = false;
        }

        [RelayCommand]
        public async Task AddProduct()
        {
            IsLoading = true;
            var popup = new SaveProduct(isEdit: false, category: Categories,
                managerEmail: ManagerEmail);
            var result = await Shell.Current.ShowPopupAsync(popup);
            Products = await _inventory.GetProducts();
            IsLoading = false;
        }

        [RelayCommand]
        public async Task RemoveProduct(Product product)
        {
            IsLoading = true;

            var (isSuccess, message) = await _inventory.DeleteProduct(product.Id, ManagerEmail);
            if (isSuccess)
            {
                await Snackbar.Make("Product deleted successfully.", duration: TimeSpan.FromSeconds(1)).Show();
                Products = await _inventory.GetProducts();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", $"Error deleting product: {message}", "OK");
            }

            IsLoading = false;
        }

        [RelayCommand]
        public async Task AddCategory()
        {
            IsLoading = true;

            var input = await Shell.Current.DisplayPromptAsync(
                title: "New Category",
                message: "Enter category name:",
                accept: "Add Category",
                cancel: "Cancel",
                placeholder: "e.g. Groceries",
                maxLength: 50,
                keyboard: Keyboard.Text);

            if (!string.IsNullOrWhiteSpace(input))
            {
                var (isSuccess, message) = await _inventory.NewCategory(new Category
                {
                    CtgryName = input.Trim().ToUpper()
                }, ManagerEmail);
                if (isSuccess)
                {
                    await Snackbar.Make("Category added successfully.", duration: TimeSpan.FromSeconds(1)).Show();
                    Categories = await _inventory.GetCategories();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", message, "OK");
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Please enter a valid category name.", "OK");
            }

            IsLoading = false;
        }

        [RelayCommand]
        public async Task EditCategory(Category category)
        {
            IsLoading = true;
            category.CtgryName.ToUpper();
            var (isSuccess, message) = await _inventory.UpdateCategory(category, ManagerEmail);
            if (isSuccess)
            {
                await Snackbar.Make("Category updated successfully.", duration: TimeSpan.FromSeconds(1)).Show();
                Categories = await _inventory.GetCategories();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", $"Error updating category: {message}", "OK");
            }

            IsLoading = false;
        }
        
        [RelayCommand]
        public async Task RemoveCategory(Category category)
        {
            IsLoading = true;

            var (isSuccess, message) = await _inventory.DeleteCategory(category.Id, ManagerEmail);
            if (isSuccess)
            {
                await Snackbar.Make("Category deleted successfully.", duration: TimeSpan.FromSeconds(1)).Show();
                Categories = await _inventory.GetCategories();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", $"Error deleting category: {message}", "OK");
            }

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
            await Task.Delay(2000);
            IsLoading = true;
            await _inventory.GetProductBarcodes();
            await Shell.Current.DisplayAlert("Success", "Product added successfully.", "OK");
            IsLoading = false;
        }

    }
}
