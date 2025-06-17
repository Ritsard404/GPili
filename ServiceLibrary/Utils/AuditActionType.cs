namespace ServiceLibrary.Utils
{
    public static class AuditActionType
    {
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string Login = "Login";
        public const string Logout = "Logout";
        public const string Approve = "Approve";

        public const string CashOutDrawer = "Cash Out Drawer";
        public const string CashInDrawer = "Cash In Drawer";
        public const string CashWithdrawDrawer = "Cash Withdraw Drawer";

        public const string ChangePassword = "Change Password";
        public const string ResetPassword = "Reset Password";

        public const string AddCashier = "Add Cashier";
        public const string RemoveCashier = "Remove Cashier";
        public const string AddManager = "Add Manager";
        public const string RemoveManager = "Remove Manager";

        public const string AddDiscount = "Add Discount";
        public const string RemoveDiscount = "Remove Discount";
        public const string UpdateDiscount = "Update Discount";
    }
}
