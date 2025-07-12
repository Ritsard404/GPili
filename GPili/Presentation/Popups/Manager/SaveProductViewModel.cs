using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;

namespace GPili.Presentation.Popups.Manager
{
    public partial class SaveProductViewModel(IInventory _inventory) : ObservableObject
    {
        public SaveProduct Popup;
        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.5;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.75;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title))]
        private bool _isEdit = false;
        public string Title => Product != null && IsEdit ? "Edit Product " + Product.Name : "Add Product";
        public string StatusText => Product != null && Product.IsAvailable ? "Available" : "Unavailable";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private Product? _product;

        [ObservableProperty]
        private Category[] _categories;
        [ObservableProperty]
        private string _managerEmail;

        [ObservableProperty]
        private bool _isRestoType = false;

        [RelayCommand]
        private async Task Save()
        {
            if (!IsRestoType)
            {
                Product.ImagePath = null;
            }

            if (IsEdit)
            {
                // Edit
                var (isSuccess, message) = await _inventory.UpdateProduct(Product, ManagerEmail);
                if (isSuccess)
                {
                    await Shell.Current.DisplayAlert("Success", "Product updated successfully.", "OK");
                    Popup.CloseResult(isSuccess);
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", message, "OK");
                    return;
                }

                if (!string.IsNullOrEmpty(Product.ImagePath) && File.Exists(Product.ImagePath))
                {
                    // Delete old image if exists
                    File.Delete(Product.ImagePath);
                }
            }
            else
            {
                // Edit
                var (isSuccess, message) = await _inventory.NewProduct(Product, ManagerEmail);
                if (isSuccess)
                {
                    await Shell.Current.DisplayAlert("Success", "Product added successfully.", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", message, "OK");
                    return;
                }
            }
        }
        [RelayCommand]
        private async Task PickImage()
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select Product Image",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                var imageFolder = FolderPath.ImagePath.Image;
                if (!Directory.Exists(imageFolder))
                    Directory.CreateDirectory(imageFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(result.FileName)}";
                var destPath = Path.Combine(imageFolder, fileName);

                using (var sourceStream = await result.OpenReadAsync())
                using (var destStream = File.Create(destPath))
                {
                    await sourceStream.CopyToAsync(destStream);
                }

                Product.ImagePath = destPath;
                OnPropertyChanged(nameof(Product));
            }
        }
    
        [RelayCommand]
        private async Task RemovePickedImage()
        {

            if (!string.IsNullOrEmpty(Product.ImagePath) && File.Exists(Product.ImagePath))
            {
                // Delete old image if exists
                File.Delete(Product.ImagePath);
                OnPropertyChanged(nameof(Product));
            }
        }
    
    
    }
}
