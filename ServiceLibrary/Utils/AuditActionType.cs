namespace ServiceLibrary.Utils
{
    public enum AuditActionType
    {
        Create,
        Update,
        Delete,
        Login,
        Logout,
        Approve,
        CashOutDrawer, 
        CashInDrawer,
        CashWithdrawDrawer,
        ChangePassword,
        ResetPassword,
        AddCashier,
        RemoveCashier,
        AddManager,
        RemoveManager,
        AddDiscount,
        RemoveDiscount,
        UpdateDiscount
    }
}
