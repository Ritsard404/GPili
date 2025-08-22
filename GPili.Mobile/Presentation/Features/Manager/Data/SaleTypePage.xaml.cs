namespace GPili.Mobile.Presentation.Features.Manager.Data;

public partial class SaleTypePage : ContentPage
{
	public SaleTypePage()
	{
		InitializeComponent();
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DataViewModel vm)
        {
            vm.CurrenDataPage = DataPageType.SaleTypes;
            await vm.InitializeData();
        }
    }
}