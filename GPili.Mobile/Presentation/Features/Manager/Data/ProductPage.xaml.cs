namespace GPili.Mobile.Presentation.Features.Manager.Data;

public partial class ProductPage : ContentPage
{
    public ProductPage()
    {
        InitializeComponent();
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is ProductsViewModel vm)
        {
            vm.IsSaveProdDisplay = false;
        }
    }
}