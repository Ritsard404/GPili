namespace GPili.Mobile.Presentation.Features.Manager.Data;

public partial class ProductPage : ContentPage
{
	public ProductPage()
	{
		InitializeComponent();
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DataViewModel vm)
        {
            vm.CurrenDataPage = DataPageType.Products;
            await vm.InitializeData();
        }
    }
}