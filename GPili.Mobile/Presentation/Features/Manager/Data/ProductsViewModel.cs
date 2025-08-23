using ServiceLibrary.Utils;

namespace GPili.Mobile.Presentation.Features.Manager
{
    [QueryProperty(nameof(Products), nameof(Products))]
    [QueryProperty(nameof(Categories), nameof(Categories))]
    [QueryProperty(nameof(ManagerEmail), nameof(ManagerEmail))]
    [QueryProperty(nameof(IsRestoType), nameof(IsRestoType))]
    public partial class ProductsViewModel(IInventory _inventory,
        INavigationService _navigation) : ObservableObject
    {

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.95;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.65;

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



        // Product CRUD

        [ObservableProperty]
        private bool _isSaveProdDisplay = false;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title))]
        private bool _isEdit = false;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private Product? _product;

        private string? _tempImagePath;
        private string? _finalImagePath;


        public string Title => Product != null && IsEdit ? "Edit Product " + Product.Name : "Add Product";
        public string StatusText => Product != null && Product.IsAvailable ? "Available" : "Unavailable";


        [RelayCommand]
        private void CloseSaveProduct()
        {
            // Clean up temp image if it exists and hasn't been saved
            if (!string.IsNullOrEmpty(_tempImagePath) && File.Exists(_tempImagePath))
            {
                File.Delete(_tempImagePath);
                _tempImagePath = null;
            }
            IsSaveProdDisplay = false;
            ResetProduct();
        }
        private void ResetProduct()
        {
            // Clean up temp image if it exists and hasn't been saved
            if (!string.IsNullOrEmpty(_tempImagePath) && File.Exists(_tempImagePath))
            {
                File.Delete(_tempImagePath);
                _tempImagePath = null;
            }
            Product = new Product
            {
                ProdId = Guid.NewGuid().ToString(),
                Name = string.Empty,
                Barcode = string.Empty,
                BaseUnit = string.Empty,
                Quantity = 0,
                Cost = 0,
                Price = 0,
                IsAvailable = true,
                ItemType = string.Empty,
                VatType = "Vatable",
                Category = Categories.FirstOrDefault()!,
                ImagePath = null
            };
            IsEdit = false;
            _finalImagePath = null;
        }
        [RelayCommand]
        private async Task PickImage()
        {
            try
            {
                IsLoading = true;
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select Product Image",
                    FileTypes = FilePickerFileType.Images
                });
                if (result != null)
                {
                    // Clean up previous temp image if exists
                    if (!string.IsNullOrEmpty(_tempImagePath) && File.Exists(_tempImagePath))
                    {
                        File.Delete(_tempImagePath);
                    }
                    var imageFolder = Path.Combine(FileSystem.CacheDirectory, "ProductImagePreview");
                    if (!Directory.Exists(imageFolder))
                        Directory.CreateDirectory(imageFolder);
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(result.FileName)}";
                    var destPath = Path.Combine(imageFolder, fileName);
                    using (var sourceStream = await result.OpenReadAsync())
                    using (var destStream = File.Create(destPath))
                    {
                        await sourceStream.CopyToAsync(destStream);
                    }
                    _tempImagePath = destPath;
                    Product.ImagePath = destPath;
                    OnPropertyChanged(nameof(Product));
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
        [RelayCommand]
        private void RemovePickedImage()
        {
            // Delete temp image if exists
            if (!string.IsNullOrEmpty(_tempImagePath) && File.Exists(_tempImagePath))
            {
                File.Delete(_tempImagePath);
                _tempImagePath = null;
            }
            Product.ImagePath = null;
            OnPropertyChanged(nameof(Product));
        }
        [RelayCommand]
        private async Task EditProduct(Product product)
        {
            // Find the matching category reference from the Categories collection
            var matchingCategory = Categories.FirstOrDefault(c => c.Id == product.Category?.Id);
            // Clean up temp image if it exists and hasn't been saved
            if (!string.IsNullOrEmpty(_tempImagePath) && File.Exists(_tempImagePath))
            {
                File.Delete(_tempImagePath);
                _tempImagePath = null;
            }
            Product = new Product
            {
                Id = product.Id,
                ProdId = product.ProdId,
                Name = product.Name,
                Barcode = product.Barcode,
                BaseUnit = product.BaseUnit,
                Quantity = product.Quantity,
                Cost = product.Cost,
                Price = product.Price,
                IsAvailable = product.IsAvailable,
                ItemType = product.ItemType,
                VatType = product.VatType,
                Category = matchingCategory, // Use the reference from Categories
                ImagePath = product.ImagePath
            };
            _finalImagePath = product.ImagePath;
            IsSaveProdDisplay = true;
            IsEdit = true;
        }
        [RelayCommand]
        public void AddProduct()
        {
            ResetProduct();
            IsSaveProdDisplay = true;
            IsEdit = false;
        }
        [RelayCommand]
        private async Task Save()
        {
            IsLoading = true;
            try
            {
                if (!IsRestoType)
                {
                    Product.ImagePath = null;
                }
                else if (!string.IsNullOrEmpty(_tempImagePath) && File.Exists(_tempImagePath))
                {
                    // Move temp image to permanent location
                    var imageFolder = FolderPath.ImagePath.Image;
                    if (!Directory.Exists(imageFolder))
                        Directory.CreateDirectory(imageFolder);
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(_tempImagePath)}";
                    var destPath = Path.Combine(imageFolder, fileName);
                    File.Copy(_tempImagePath, destPath, true);
                    Product.ImagePath = destPath;
                    _finalImagePath = destPath;
                    // Clean up temp image
                    File.Delete(_tempImagePath);
                    _tempImagePath = null;
                }
                if (IsEdit)
                {
                    var (isSuccess, message) = await _inventory.UpdateProduct(Product, ManagerEmail);
                    if (isSuccess)
                    {
                        await Shell.Current.DisplayAlert("Success", "Product updated successfully.", "OK");
                        Products = await _inventory.GetProducts();
                        IsSaveProdDisplay = false;
                        Products = await _inventory.GetProducts();
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error", message, "OK");
                        return;
                    }
                    // Optionally, delete old image if it was replaced
                    if (!string.IsNullOrEmpty(_finalImagePath) && _finalImagePath != Product.ImagePath && File.Exists(_finalImagePath))
                    {
                        File.Delete(_finalImagePath);
                    }
                }
                else
                {
                    var (isSuccess, message) = await _inventory.NewProduct(Product, ManagerEmail);
                    if (isSuccess)
                    {
                        await Shell.Current.DisplayAlert("Success", "Product added successfully.", "OK");
                        IsSaveProdDisplay = false;
                        Products = await _inventory.GetProducts();
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error", message, "OK");
                        return;
                    }
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RemoveProduct(Product product)
        {
            IsLoading = true;
            try
            {
                var (isSuccess, message) = await _inventory.DeleteProduct(product.Id, ManagerEmail);
                if (isSuccess)
                {
                    await Toast.Make("Product deleted successfully.").Show();
                    Products = await _inventory.GetProducts();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", $"Error deleting product: {message}", "OK");
                }
            }
            finally { IsLoading = false; }

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
        private async Task PrintBarcodes()
        {
            await Task.Delay(2000);
            IsLoading = true;
            await _inventory.GetProductBarcodes();
            await Shell.Current.DisplayAlert("Success", "Product printed successfully.", "OK");
            IsLoading = false;
        }

    }
}
