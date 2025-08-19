namespace GPili.Mobile.Presentation.Features.Cashier;

public partial class TenderPage : ContentPage
{
	public TenderPage()
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