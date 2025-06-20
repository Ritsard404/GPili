using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using System.Diagnostics;

namespace ServiceLibrary.Services.Repositories
{
    public class AuthRepository(DataContext _dataContext, IAuditLog _auditLog, IGPiliTerminalMachine _terminalMachine) : IAuth
    {
        public async Task<(bool isSuccess, string message)> CashWithdrawDrawer(string cashierEmail, string managerEmail, decimal cash)
        {
            var timestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Where(t => t.Cashier.Email == cashierEmail && t.TsOut == null && t.CashInDrawerAmount != null && t.CashInDrawerAmount >= 1000)
                .FirstOrDefaultAsync();

            var manager = await IsManagerValid(managerEmail);
            if (!manager.isSuccess || manager.manager == null)
                return (false, "Invalid manager credential.");

            if (timestamp?.CashInDrawerAmount is not { } available)
                return (false, "No active session or drawer amount not set.");

            var tsIn = timestamp.TsIn;

            // First get all valid orders for the cashier
            var pendingInvoices = await _dataContext.Invoice
                .Where(i => i.Cashier.Email == cashierEmail &&
                i.Status == InvoiceStatusType.Paid.ToString() &&
                i.CashTendered != null &&
                i.TotalAmount < 0)
                .ToListAsync();

            decimal totalCashInDrawer = pendingInvoices
                .Sum(i => i.CashTendered ?? 0m - i.ChangeAmount ?? 0m);

            if(cash > available + totalCashInDrawer)
                return (false, "Cash amount exceeds available cash in drawer and pending orders total.");

            timestamp.WithdrawnDrawerAmount += cash;

            await _auditLog.AddManagerAudit(manager.manager, AuditActionType.Approve.ToString(), "Manager approved cash withdrawal from drawer", cash);
            await _auditLog.AddCashierAudit(timestamp.Cashier, AuditActionType.CashWithdrawDrawer.ToString(), "Cash withdrawn from drawer", cash);
            return (true, "Cash withdrawal recorded.");
        }

        public async Task<User[]> GetCashiers()
        {
            var cashiers = await _dataContext.User
                .Where(u => u.Role == RoleType.Cashier.ToString())
                .OrderBy(u => u.FName)
                .ThenBy(u => u.LName)
                .ToListAsync();

            cashiers.Insert(0, new User
            {
                Email = "",
                FName = "",
                LName = "",
                Role = RoleType.Cashier.ToString(),
                IsActive = true
            });

            return cashiers.ToArray();
        }

        public async Task<(bool isSuccess, string cashierName, string cashierEmail, List<Item> pendingItems)> HasPendingOrder()
        {
            var pendingInvoice = await _dataContext.Invoice
                .Include(i => i.Cashier)
                .Where(i => i.Status == InvoiceStatusType.Pending)
                .Select(i => new { i.Cashier.FName, i.Cashier.LName, i.Cashier.Email })
                .FirstOrDefaultAsync();

            var activeTimestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Where(t => t.TsOut == null && t.CashInDrawerAmount != null && t.CashInDrawerAmount >= 1000)
                .Select(t => new { t.Cashier.FName, t.Cashier.LName, t.Cashier.Email })
                .FirstOrDefaultAsync();

            var pendingItems = await _dataContext.Item
                .Where(i => i.Invoice.Status == InvoiceStatusType.Pending)
                .ToListAsync();

            if (pendingInvoice != null)
                return (true, $"{pendingInvoice.FName} {pendingInvoice.LName}", pendingInvoice.Email, pendingItems);

            if (activeTimestamp != null)
                return (true, $"{activeTimestamp.FName} {activeTimestamp.LName}", activeTimestamp.Email, new List<Item>());

            return (false, "", "", new List<Item>());
        }

        public async Task<bool> IsCashedDrawer(string cashierEmail)
        {
            var timestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Where(t => t.Cashier.Email == cashierEmail && t.TsOut == null && t.CashInDrawerAmount != null && t.CashInDrawerAmount >= 1000)
                .FirstOrDefaultAsync();

            return timestamp != null;
        }

        public async Task<(bool isSuccess, User? cashier)> IsCashierValid(string cashierEmail)
        {
            var cashier = await _dataContext.User
                .FirstOrDefaultAsync(u => u.Email == cashierEmail && u.Role == RoleType.Cashier.ToString());

            if (cashier == null)
                return (false, null);

            return (true, cashier);
        }

        public async Task<(bool isSuccess, User? manager)> IsManagerValid(string managerEmail)
        {
            var manager = await _dataContext.User
                .FirstOrDefaultAsync(u => u.Email == managerEmail && u.Role == RoleType.Manager.ToString());

            if (manager == null)
                return (false, null);

            return (true, manager);
        }

        public async Task<(bool isSuccess, string Role, string email, string name, string message)> LogIn(string managerEmail, string cashierEmail)
        {
            // Fetch both users in a single query
            var users = await _dataContext.User
                .Where(u => (u.Email == managerEmail && u.Role != RoleType.Cashier.ToString()) ||
                            (u.Email == cashierEmail && u.Role == RoleType.Cashier.ToString()))
                .ToListAsync();

            var manager = users.FirstOrDefault(u => u.Email == managerEmail && u.Role != RoleType.Cashier.ToString());
            var cashier = users.FirstOrDefault(u => u.Email == cashierEmail && u.Role == RoleType.Cashier.ToString());

            if (manager == null && cashier == null)
                return (false, string.Empty, string.Empty, string.Empty, "Invalid credentials. Please try again.");

            if (manager != null && cashier == null)
            {
                await _auditLog.AddManagerAudit(manager, AuditActionType.Login, "Manager logged in", null);
                return (true, RoleType.Manager, manager.Email, $"{manager.FName} {manager.LName}", "Manager logged in successfully.");
            }

            if (cashier != null && manager != null)
            {
                var hasProducts = await _dataContext.Product.AnyAsync(p => p.IsAvailable);
                if (!hasProducts)
                    return (false, string.Empty, string.Empty, string.Empty, "No products available. Please add products to continue.");

                await using var transaction = await _dataContext.Database.BeginTransactionAsync();

                try
                {
                    await _auditLog.AddCashierAudit(cashier, AuditActionType.Login, "Cashier logged in", null);

                    var timestamp = new Timestamp
                    {
                        TsIn = DateTime.UtcNow,
                        Cashier = cashier,
                        ManagerIn = manager,
                        IsTrainMode = await _terminalMachine.IsTrainMode(),
                    };

                    _dataContext.Timestamp.Add(timestamp);

                    await _auditLog.AddManagerAudit(manager, AuditActionType.Approve.ToString(), "Manager approved cashier logged in", null);
                    await _auditLog.AddCashierAudit(cashier, AuditActionType.Approve.ToString(), "Manager approved cashier logged in", null);

                    await _dataContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, RoleType.Cashier.ToString(), cashier.Email, $"{cashier.FName} {cashier.LName}", "Cashier logged in successfully.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Debug.WriteLine(ex);
                    return (false, string.Empty, string.Empty, string.Empty, $"An error occurred during login. Please try again.\n{ex}");
                }
            }

            return (false, string.Empty, string.Empty, string.Empty, "Invalid credential!");
        }



        public async Task<(bool isSuccess, string message)> LogOut(string managerEmail, string cashierEmail, decimal cash)
        {
            // Fetch both users in a single query
            var users = await _dataContext.User
                .Where(u => (u.Email == managerEmail && u.Role != RoleType.Cashier.ToString()) ||
                            (u.Email == cashierEmail && u.Role == RoleType.Cashier.ToString()))
                .ToListAsync();

            var manager = users.FirstOrDefault(u => u.Email == managerEmail && u.Role != RoleType.Cashier.ToString());
            var cashier = users.FirstOrDefault(u => u.Email == cashierEmail && u.Role == RoleType.Cashier.ToString());

            // If neither found, fail
            if (manager == null || cashier == null)
                return (false, "Invalid credentials. Please try again.");

            var hasPendingInvoice = await _dataContext.Invoice
                .AnyAsync(i => i.Cashier == cashier && i.Status == InvoiceStatusType.Pending.ToString());

            if (hasPendingInvoice)
                return (false, "Cashier has pending item!");

            var timestamp = await _dataContext.Timestamp
                .Where(t => t.Cashier == cashier && t.TsOut == null)
                .FirstOrDefaultAsync();

            if (timestamp == null)
                return (false, "Cashier is not clocked in!");

            timestamp.TsOut = DateTime.UtcNow; // Set the time-out to now
            timestamp.ManagerOut = manager; // Manager who authorized the logout
            timestamp.CashOutDrawerAmount = cash;

            await _auditLog.AddManagerAudit(manager, AuditActionType.Approve.ToString(), "Manager approved cashier logged out", null);
            await _auditLog.AddCashierAudit(cashier, AuditActionType.Logout.ToString(), "Cashier logged out", null);

            await _auditLog.AddManagerAudit(manager, AuditActionType.Approve.ToString(), "Manager approved cashier cashed out", cash);
            await _auditLog.AddCashierAudit(cashier, AuditActionType.CashOutDrawer.ToString(), "Cash out drawer set", cash);

            await _dataContext.SaveChangesAsync();

            return (true, "Cashier Logged Out!");
        }

        public async Task<(bool isSuccess, string message)> SetCashInDrawer(string cashierEmail, decimal cash)
        {
            var cashier = await IsCashierValid(cashierEmail);
            if (!cashier.isSuccess || cashier.cashier == null)
                return (false, "Invalid cashier credential.");

            // Get the active timestamp (where cashier is clocked in)
            var timestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Where(t => t.Cashier.Email == cashierEmail && t.TsOut == null)
                .FirstOrDefaultAsync();

            if (timestamp == null)
                return (false, "No active session found for this cashier. Please clock in first.");

            // Validate cash amount
            if (cash < 0)
                return (false, "Cash amount cannot be negative!");


            // Set the cash amount
            timestamp.CashInDrawerAmount = cash;
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddCashierAudit(cashier.cashier, AuditActionType.CashInDrawer.ToString(), "Cash in drawer set", cash);

            return (true, "Cash in drawer set successfully!");
        }
    }
}
