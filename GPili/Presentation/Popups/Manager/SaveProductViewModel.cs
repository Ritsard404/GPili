using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;

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

        [RelayCommand]
        private async Task Save()
        {
            if (IsEdit)
            {
                // Edit
                var (isSuccess, message) = await _inventory.UpdateProduct(Product, ManagerEmail);
                if(isSuccess)
                {
                    await Shell.Current.DisplayAlert("Success", "Product updated successfully.", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", message, "OK");
                    return;
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

            Popup.Close();
        }
    
    }
}
