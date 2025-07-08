using ServiceLibrary.Models;

namespace GPili.Presentation.Popups.Manager;

public partial class SaveProduct : Popup
{
    public SaveProduct(bool isEdit, Category[] category, string managerEmail, Product? product = null)
    {
        PopupState.PopupInfo.OpenPopup("Product", "Product");
        InitializeComponent();
        var vm = IPlatformApplication.Current.Services.GetRequiredService<SaveProductViewModel>();
        vm.Popup = this;
        vm.IsEdit = isEdit;
        vm.ManagerEmail = managerEmail; vm.Product = product ?? new Product
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
            VatType = string.Empty,
            Category = category.FirstOrDefault()!
        };
        vm.Categories = category;
        BindingContext = vm;
        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();
    }
}