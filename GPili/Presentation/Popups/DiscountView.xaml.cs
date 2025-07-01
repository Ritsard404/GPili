using ServiceLibrary.Services.DTO.Payment;

namespace GPili.Presentation.Popups;

public partial class DiscountView : Popup
{
	public DiscountView()
    {
        PopupState.PopupInfo.OpenPopup("Discount", "Add Discount");
        InitializeComponent(); 
		var vm = IPlatformApplication.Current.Services.GetRequiredService<DiscountViewModel>();
        vm.Popup = this;
        BindingContext = vm;
        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();
    }
    public void CloseWithResult(object? result = null)
        => Close(result);
}