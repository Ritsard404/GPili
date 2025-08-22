namespace GPili.Mobile.Presentation.Features.Manager.Report
{
    public partial class ReportViewModel(IReport _report) : ObservableObject
    {

        [ObservableProperty] private DateTime _from = DateTime.Now;
        [ObservableProperty] private DateTime _to = DateTime.Now.AddDays(1);
        [ObservableProperty] private bool _isLoading = false;
        [ObservableProperty] private bool _hasDiscType = false;
        [ObservableProperty] private ReportType _currentReport;

        // Generic loader that automatically resets From/To
        //private async Task LoadData<T>(Func<Task<T>> loadFunc, Action<T> setResult)
        //{
        //    IsLoading = true;
        //    try
        //    {
        //        var result = await loadFunc();
        //        setResult(result);
        //    }
        //    finally
        //    {
        //        IsLoading = false;
        //    }
        //}


        public void ResetDateRange()
        {
            From = DateTime.Now;
            To = DateTime.Now.AddDays(1);
            HasDiscType = false;
        }

        // Example: Sales Report
        [ObservableProperty] private List<SalesReportDTO> _salesReports = new();
        [ObservableProperty] private TotalSalesReportDTO _totalSalesReports;

        //[RelayCommand]
        //private Task SearchSalesReport() => LoadData(
        //    () => _report.GetSalesReportData(From, To),
        //    result =>
        //    {
        //        TotalSalesReports = result.totalSalesReport;
        //        SalesReports = result.salesReports;
        //    });

        // Audit Trail
        [ObservableProperty] private List<AuditTrailDTO> _auditTrail = new();
        //[RelayCommand]
        //private Task SearchAuditTrail() => LoadData(
        //    () => _report.GetAuditTrailData(From, To),
        //    result => AuditTrail = result);

        // Sales Book
        [ObservableProperty] private List<Reading> _salesBook = new();
        //[RelayCommand]
        //private Task SearchSalesBook() => LoadData(
        //    () => _report.GetSalesBookData(From, To),
        //    result => SalesBook = result);

        // Tranx List
        [ObservableProperty] private List<TransactionListDTO> _tranxList = new();
        [ObservableProperty] private TotalTransactionListDTO _totalTranxList;
        //[RelayCommand]
        //private Task SearchTranxList() => LoadData(
        //    () => _report.GetTransactListData(From, To),
        //    result =>
        //    {
        //        TranxList = result.Item1;
        //        TotalTranxList = result.Item2;
        //    });

        // Voided List
        [ObservableProperty] private List<VoidedListDTO> _voidedList = new();
        [ObservableProperty] private TotalVoidedListDTO _totalVoidedList;
        //[RelayCommand]
        //private Task SearchVoidedList() => LoadData(
        //    () => _report.GetVoidedListsData(From, To),
        //    result =>
        //    {
        //        VoidedList = result.voidedOrdersLists;
        //        TotalVoidedList = result.totalVoidedList;
        //    });

        // PWD or Senior
        [ObservableProperty] private List<TransactionListDTO> _pwdOrSeniorList = new();
        [ObservableProperty] private TotalTransactionListDTO _totalPwdOrSeniorList;
        [ObservableProperty]
        //[NotifyCanExecuteChangedFor(nameof(SearchPwdOrSeniorListCommand))]
        [NotifyCanExecuteChangedFor(nameof(SearchReportCommand))]
        private string _discountType = "PWD";


        //[RelayCommand]
        //private Task SearchPwdOrSeniorList() => LoadData(
        //    () => _report.GetPwdOrSeniorData(From, To, DiscountType),
        //    result =>
        //    {
        //        PwdOrSeniorList = result.Item1;
        //        TotalPwdOrSeniorList = result.Item2;
        //        HasDiscType = true;
        //    });

        [RelayCommand]
        private async Task SearchReport()
        {
            IsLoading = true;
            try
            {
                switch (CurrentReport)
                {
                    case ReportType.SalesReport:
                        var sales = await _report.GetSalesReportData(From, To);
                        TotalSalesReports = sales.totalSalesReport;
                        SalesReports = sales.salesReports;
                        break;

                    case ReportType.AuditTrail:
                        AuditTrail = await _report.GetAuditTrailData(From, To);
                        break;

                    case ReportType.SalesBook:
                        SalesBook = await _report.GetSalesBookData(From, To);
                        break;

                    case ReportType.TranxList:
                        var tranx = await _report.GetTransactListData(From, To);
                        TranxList = tranx.Item1;
                        TotalTranxList = tranx.Item2;
                        break;

                    case ReportType.VoidedList:
                        var voided = await _report.GetVoidedListsData(From, To);
                        VoidedList = voided.voidedOrdersLists;
                        TotalVoidedList = voided.totalVoidedList;
                        HasDiscType = false;
                        break;

                    case ReportType.PwdOrSenior:
                        var pwd = await _report.GetPwdOrSeniorData(From, To, DiscountType);
                        PwdOrSeniorList = pwd.Item1;
                        TotalPwdOrSeniorList = pwd.Item2;
                        HasDiscType = true;
                        break;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
    public enum ReportType
    {
        SalesReport,
        AuditTrail,
        SalesBook,
        TranxList,
        VoidedList,
        PwdOrSenior
    }

}
