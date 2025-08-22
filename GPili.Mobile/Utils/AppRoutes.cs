using GPili.Mobile.Presentation.Features.Cashier;

namespace GPili.Mobile.Utils
{
    public static class AppRoutes
    {
        public const string Login = "//LogInPage";

        public const string Cashiering = "//CashierPage";
        public const string Cart = $"//{nameof(CartPage)}";

        public const string Manager = "//ManagerPage";

        // Manager Sub Pages
        // Sales 
        public const string TranxLists = "/TranxListPage";
        public const string RefundInvoice = "/RefundInvoicePage";

        // Reports
        public const string AuditTrail = "/AuditTrailPage";
        public const string DailyTranx = "/DailyTranxPage";
        public const string PwdOrScList = "/PwdOrScListPage";
        public const string SalesBook = "/SalesBookPage";
        public const string SalesHistory = "/SalesHistoryPage";
        public const string VoidedList = "/VoidedListPage";

        // Data
        public const string Category = "/CategoryPage";
        public const string Product = "/ProductPage";
        public const string SaleType = "/SaleTypePage";
        public const string Setting = "/SettingPage";
        public const string User = "/UserPage";
    }
}
