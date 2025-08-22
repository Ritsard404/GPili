using GPili.Mobile.Presentation.Features.Manager.Report;

namespace GPili.Mobile.Presentation.Features.Manager.Data;

public partial class SettingPage : ContentPage
{
	public SettingPage()
	{
		InitializeComponent();
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DataViewModel vm)
        {
            vm.CurrenDataPage = DataPageType.Settings;
            await vm.InitializeData();
        }
    }
}