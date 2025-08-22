namespace GPili.Mobile.Presentation.Features.Manager.Report;

public partial class PwdOrScListPage : ContentPage
{
	public PwdOrScListPage()
	{
		InitializeComponent();
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ReportViewModel vm)
        {
            vm.ResetDateRange();
            vm.CurrentReport = ReportType.PwdOrSenior;
            await vm.SearchReportCommand.ExecuteAsync(null);
        }
    }
}