namespace GPili.Mobile.Presentation.Features.Manager.Data;

public partial class UsersPage : ContentPage
{
	public UsersPage()
	{
		InitializeComponent();
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DataViewModel vm)
        {
            vm.CurrenDataPage = DataPageType.Users;
            await vm.InitializeData();
        }
    }
}