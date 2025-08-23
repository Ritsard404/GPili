using System.Threading.Tasks;

namespace GPili.Mobile.Presentation.Features.LogIn;

public partial class LogInPage : ContentPage
{
	public LogInPage()
	{
		InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        TrainModeLabel.IsVisible = POSInfo.Terminal.IsTrainMode;

        if (BindingContext is LogInViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is LogInViewModel vm)
        {
           await vm.DisposeAsync();
        }
    }
}