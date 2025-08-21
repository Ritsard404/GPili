
using GPili.Mobile.Presentation.Features.Cashier;
using GPili.Mobile.Presentation.Features.Manager;
using GPili.Mobile.Presentation.Popups;
using ServiceLibrary.Utils;

namespace GPili.Mobile.Presentation.Contents.Manager;

public partial class TitleViewControl : ContentView
{
	public TitleViewControl()
	{
		InitializeComponent();
	}
	protected override void OnParentSet()
	{
		base.OnParentSet();

        if (App.UserInfo != null && App.UserInfo.Role != RoleType.Cashier)
		{
            BackButton.IsVisible = false;
            LogOutButton.IsVisible = false;
		}
		else
		{
            BackLogInButton.IsVisible = false;
        }
    }
	//public async void GoBack(object sender, EventArgs e)
 //   {
 //       if (BindingContext is ManagerViewModel vm)
 //           await vm.BackCashiering();
 //   }
}