using ServiceLibrary.Models;

namespace GPili.Presentation.Popups;

public partial class EditItemView : Popup
{
	public EditItemView(Item item)
    {
        PopupState.PopupInfo.OpenPopup("Edit Item", $"Editting item of {item.Product.Name}");

        InitializeComponent(); 
		var vm = IPlatformApplication.Current.Services.GetRequiredService<EditItemViewModel>();
		vm.Item = item;
        vm.Qty = item.Qty;
        vm.SubTotal = item.SubTotal;
		vm._popup = this;
        BindingContext = vm;

        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();

    }

    public void CloseWithResult(object? result = null)
        => Close(result);

    private async void Entry_Completed(object sender, EventArgs e)
    {

        if (BindingContext is EditItemViewModel vm)
        {
            await vm.SaveItem();
        }
    }
}