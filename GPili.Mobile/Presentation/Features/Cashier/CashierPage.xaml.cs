namespace GPili.Mobile.Presentation.Features.Cashier;

public partial class CashierPage : ContentPage
{
	public CashierPage()
	{
		InitializeComponent();
	}
	protected override async void OnAppearing()
	{
		base.OnAppearing();

        await Task.Delay(1500);
        if (BindingContext is CashierViewModel vm)
            await vm.InitializeAsync();
    }
}