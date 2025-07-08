using ServiceLibrary.Models;
namespace GPili.Presentation.Popups.Manager;

public partial class CategoriesView : Popup
{
	public CategoriesView(Category[] category, string managerEmail)
    {
        PopupState.PopupInfo.OpenPopup("Product", "Product");
        InitializeComponent(); 
		var vm = IPlatformApplication.Current.Services.GetRequiredService<SaveProductViewModel>();
		BindingContext = vm; 
        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();

    }
}