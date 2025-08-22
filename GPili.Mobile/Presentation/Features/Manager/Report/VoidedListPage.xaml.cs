using GPili.Mobile.Presentation.Features.Manager.Sales;

namespace GPili.Mobile.Presentation.Features.Manager.Report;

public partial class VoidedListPage : ContentPage
{
	public VoidedListPage()
	{
		InitializeComponent();
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ReportViewModel vm)
        {
            vm.ResetDateRange();
            vm.CurrentReport = ReportType.VoidedList;
            await vm.SearchReportCommand.ExecuteAsync(null);
        }
    }
}