using ServiceLibrary.Models;

namespace GPili.Presentation.Popups;

public partial class EditItemView : Popup
{
	public EditItemView(Item item)
	{
		InitializeComponent(); 
		var vm = IPlatformApplication.Current.Services.GetRequiredService<EditItemViewModel>();
		vm.Item = item;
        vm.Qty = item.Qty;
        vm.SubTotal = item.SubTotal;
		vm._popup = this;
        BindingContext = vm;
    }
    public void CloseWithResult(object? result = null)
    {
        Close(result);
    }
}