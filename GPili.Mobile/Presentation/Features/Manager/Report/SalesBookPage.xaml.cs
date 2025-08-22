namespace GPili.Mobile.Presentation.Features.Manager.Report;

public partial class SalesBookPage : ContentPage
{
	public SalesBookPage()
	{
		InitializeComponent();
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ReportViewModel vm)
        {
            vm.ResetDateRange();
            vm.CurrentReport = ReportType.SalesBook;
            await vm.SearchReportCommand.ExecuteAsync(null);
        }
    }
}