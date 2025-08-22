namespace GPili.Mobile.Presentation.Features.Manager.Report;

public partial class SalesHistoryPage : ContentPage
{
	public SalesHistoryPage()
	{
		InitializeComponent();
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ReportViewModel vm)
        {
            vm.ResetDateRange();
            vm.CurrentReport = ReportType.SalesReport;
            await vm.SearchReportCommand.ExecuteAsync(null);
        }
    }
}