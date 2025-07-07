
namespace GPili.Presentation.Features.Manager;

public partial class ProductsPage : ContentPage
{
	public ProductsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();


        if (BindingContext is ProductsViewModel vm)
        {
            vm.IsLoading = true;
            await vm.LoadProducts();
            vm.IsLoading = false;
        }
    }


}