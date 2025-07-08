using GPili.Presentation.Features.Manager;
using ServiceLibrary.Models;
namespace GPili.Presentation.Popups.Manager;

public partial class CategoriesView : Popup
{
    public CategoriesView(Category[] categories, string managerEmail)
    {
        PopupState.PopupInfo.OpenPopup("Product", "Product");
        InitializeComponent();
        var vm = IPlatformApplication.Current.Services.GetRequiredService<ProductsViewModel>();
        vm.Categories = categories;
        vm.ManagerEmail = managerEmail;
        vm.PopupCategory = this;
        _ = vm.LoadProducts();
        BindingContext = vm;
        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();
    }
}