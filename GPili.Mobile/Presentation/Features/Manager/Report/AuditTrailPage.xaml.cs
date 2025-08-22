namespace GPili.Mobile.Presentation.Features.Manager.Report;

public partial class AuditTrailPage : ContentPage
{
	public AuditTrailPage()
	{
		InitializeComponent();
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ReportViewModel vm)
        {
            vm.ResetDateRange();
            vm.CurrentReport = ReportType.AuditTrail;
            await vm.SearchReportCommand.ExecuteAsync(null);
        }
    }
}