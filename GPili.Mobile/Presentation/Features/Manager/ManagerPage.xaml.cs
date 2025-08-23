using ServiceLibrary.Utils;
namespace GPili.Mobile.Presentation.Features.Manager;

public partial class ManagerPage : ContentPage
{
	public ManagerPage()
	{
		InitializeComponent();
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (App.UserInfo != null && App.UserInfo.Role == RoleType.Cashier)
        {
            Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
            XReading.IsEnabled = false; 
            TrainModeButton.IsVisible = false;
        }

        if (App.UserInfo != null && App.UserInfo.Role != RoleType.Cashier)
        {
            CashPullOut.IsEnabled = false; 
        }


    }
}