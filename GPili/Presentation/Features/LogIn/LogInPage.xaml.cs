namespace GPili.Presentation.Features.LogIn;

public partial class LogInPage : ContentPage
{
	public LogInPage()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is LogInViewModel vm)
        {
            await vm.InitializeAsync();
        }

        AdminAuth.Focus();

    }
    private async void AdminAuth_Completed(object sender, EventArgs e)
    {

        if (BindingContext is LogInViewModel vm)
        {
            await vm.LogIn();
        }
    }
}