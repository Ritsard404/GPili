namespace GPili.Mobile.Presentation.Features.Cashier;

public partial class CartPage : ContentPage
{
	public CartPage()
	{
		InitializeComponent();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        //await Task.Delay(1500);
        //if (BindingContext is CashierViewModel vm)
        //    await vm.InitializeAsync();
    }
}