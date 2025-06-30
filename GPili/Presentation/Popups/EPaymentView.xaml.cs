namespace GPili.Presentation.Popups;

public partial class EPaymentView : Popup
{
	public EPaymentView()
    {
        PopupState.PopupInfo.OpenPopup("E-Payment", "Add E-Payment");

        InitializeComponent();

        var vm = IPlatformApplication.Current.Services.GetRequiredService<EPaymentViewModel>();
        _ = vm.LoadSaleTypes();
        vm.Popup = this;
        BindingContext = vm;
        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();
    }

    public void CloseWithResult(object? result = null)
        => Close(result);
}