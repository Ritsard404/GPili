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

        // Invoice
        public const string CreateInvoice = "Create Invoice";
        public const string ReturnInvoice = "Return Invoice";

        // Items
        public const string AddItem = "Add Item";
        public const string UpdateItem = "Update Item";
        public const string VoidItem = "Void Item";
        public const string ReturnItem = "Return Item";

        // Products
        public const string AddProduct = "Add Product";
        public const string UpdateProduct = "Update Product";
        public const string RemoveProduct = "Remove Product";

        // Categories
        public const string AddCategory = "Add Category";
        public const string UpdateCategory = "Update Category";
        public const string RemoveCategory = "Remove Category";

        // Order
        public const string AddOrder = "Add Order";
        public const string UpdateOrder = "Update Order";
        public const string VoidOrder = "Void Order";
        public const string PayOrder = "Pay Order";


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
