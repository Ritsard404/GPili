namespace GPili.Mobile.Presentation.Features.Manager.Sales;

public partial class TranxListPage : ContentPage
{
	public TranxListPage()
	{
		InitializeComponent();
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is SalesViewModel vm)
           await vm.InitializeTransactLists();
    }
}