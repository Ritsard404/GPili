using ServiceLibrary.Models;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IAuth
    {
        Task<(bool isSuccess, string Role, string email, string name, string message)> LogIn(string managerEmail, string cashierEmail);
        Task<(bool isSuccess, string message)> LogOut(string managerEmail, string cashierEmail, decimal cash);
        Task<(bool isSuccess, string cashierName, string cashierEmail, List<Item> pendingItems)> HasPendingOrder();
        Task<(bool isSuccess, User? manager)> IsManagerValid(string managerEmail);
        Task<(bool isSuccess, User? cashier)> IsCashierValid(string cashierEmail);

        Task<(bool isSuccess, string message)> SetCashInDrawer(string cashierEmail, decimal cash);
        Task<(bool isSuccess, string message)> CashWithdrawDrawer(string cashierEmail, string managerEmail, decimal cash);
        Task<bool> IsCashedDrawer(string cashierEmail);

        Task<User[]> GetCashiers();

    }
}
