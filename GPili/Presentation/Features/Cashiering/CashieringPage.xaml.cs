namespace GPili.Presentation.Features.Cashiering;

public partial class CashieringPage : ContentPage
{
    public CashieringPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is CashieringViewModel vm)
            await vm.InitializeAsync();
    }
}