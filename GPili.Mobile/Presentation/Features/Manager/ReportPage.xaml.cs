using ServiceLibrary.Utils;

namespace GPili.Mobile.Presentation.Features.Manager;

public partial class ReportPage : ContentPage
{
	public ReportPage()
	{
		InitializeComponent();
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (App.UserInfo != null && App.UserInfo.Role == RoleType.Cashier)
            Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
    }
}