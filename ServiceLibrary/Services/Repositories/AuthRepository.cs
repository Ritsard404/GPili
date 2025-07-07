using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Services.Repositories
{
    public class AuthRepository(DataContext _dataContext, IAuditLog _auditLog) : IAuth
    {
        public async Task<(bool isSuccess, string message)> CashWithdrawDrawer(string cashierEmail, string managerEmail, decimal cash)
        {
            var isTrainMode = await _dataContext.PosTerminalInfo.Select(t => t.IsTrainMode).FirstOrDefaultAsync();

            var timestamp = await _dataContext.Timestamp
                .Include(t => t.Cashier)
                .Where(t => t.Cashier.Email == cashierEmail &&
                    t.TsOut == null && t.CashInDrawerAmount != null &&
                    t.CashInDrawerAmount >= 1000 && t.IsTrainMode == isTrainMode)
                .FirstOrDefaultAsync();

            var manager = await IsManagerValid(managerEmail);
            if (!manager.isSuccess || manager.manager == null)
                return (false, "Invalid manager credential.");

            if (timestamp?.CashInDrawerAmount is not { } startingCash)
                return (false, "No active session or drawer amount not set.");

            var tsIn = timestamp.TsIn;

            // Fetch all orders with the cashier
            var orders = await _dataContext.Invoice
                .Include(o => o.Cashier)
                .ToListAsync();

            // First get all valid orders for the cashier
            decimal totalCashInDrawer = orders
                 .Where(o =>
                     o.Cashier.Email == cashierEmail &&
                     o.Status == InvoiceStatusType.Paid &&
                     o.CreatedAt >= tsIn &&
                     o.IsTrainMode == isTrainMode)
                 .Sum(o =>
                     o.CashTendered.GetValueOrDefault()
                     - o.ChangeAmount.GetValueOrDefault()
                     - o.ReturnedAmount.GetValueOrDefault()
                 );

            if (cash > startingCash + totalCashInDrawer)
                return (false, "Cash amount exceeds available cash in drawer and pending orders total.");

            timestamp.WithdrawnDrawerAmount += cash;
            timestamp.WithdrawnDrawerCount ++;

            await _auditLog.AddManagerAudit(manager.manager, AuditActionType.Approve, "Manager approved cash withdrawal from drawer", cash);
            await _auditLog.AddCashierAudit(timestamp.Cashier, AuditActionType.CashWithdrawDrawer, "Cash withdrawn from drawer", cash);
            return (true, "Cash withdrawal recorded.");
        }

        public async Task<(bool isSuccess, string message)> DeleteCashier(long id, string managerEmail)
        {
            var existing = await _dataContext.User.FirstOrDefaultAsync(u => u.Role == RoleType.Cashier 
                && u.IsActive && u.Email != null 
                && u.Email != "" && u.GetHashCode() == id);
            if (existing == null)
                return (false, "Cashier not found.");

            var managerResult = await IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid manager credential.");

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.Now;

            _dataContext.User.Update(existing);

            await _dataContext.SaveChangesAsync();
            await _auditLog.AddManagerAudit(managerResult.manager, 
                AuditActionType.RemoveCashier, $"Deleted cashier: {existing.FullName} ({existing.Email})", null);

            return (true, "Cashier deleted successfully");
        }

        public async Task<(bool isSuccess, string message)> DeleteManager(string email)
        {
            var existing = await _dataContext.User.FirstOrDefaultAsync(u => u.Role == RoleType.Manager && u.IsActive && u.Email.ToLower() == email.ToLower());
            if (existing == null)
                return (false, "Manager not found.");

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.Now;

            _dataContext.User.Update(existing);
            await _dataContext.SaveChangesAsync();
            await _auditLog.AddManagerAudit(existing, AuditActionType.RemoveManager, $"Deleted manager: {existing.FullName} ({existing.Email})", null);
            return (true, "Manager deleted successfully");
        }

        public async Task<User[]> GetCashiers()
        {
            var cashiers = await _dataContext.User
                .Where(u => u.Role == RoleType.Cashier)
                .OrderBy(u => u.FName)
                .ThenBy(u => u.LName)
                .ToListAsync();

            cashiers.Insert(0, new User
            {
                Email = "",
                FName = "",
                LName = "",
                Role = RoleType.Cashier,
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
                .Where(t => t.TsOut == null)
                //.Where(t => t.TsOut == null && t.CashInDrawerAmount != null && t.CashInDrawerAmount >= 1000)
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
                .FirstOrDefaultAsync(u => u.Email == cashierEmail && u.Role == RoleType.Cashier);

            if (cashier == null)
                return (false, null);

            return (true, cashier);
        }

        public async Task<(bool isSuccess, User? manager)> IsManagerValid(string managerEmail)
        {
            var manager = await _dataContext.User
                .FirstOrDefaultAsync(u => u.Email == managerEmail && u.Role == RoleType.Manager);

            if (manager == null)
                return (false, null);

            return (true, manager);
        }

        public async Task<(bool isSuccess, string Role, string email, string name, string message)> LogIn(string managerEmail, string cashierEmail)
        {
            // Fetch both users in a single query
            var users = await _dataContext.User
                .ToListAsync();

            var isTrainMode = await _dataContext.PosTerminalInfo.Select(t => t.IsTrainMode).FirstOrDefaultAsync();

            var manager = users.FirstOrDefault(u => u.Email == managerEmail && u.Role != RoleType.Cashier);
            var cashier = users.FirstOrDefault(u => u.Email == cashierEmail && u.Role == RoleType.Cashier);

            if (manager == null && cashier == null)
                return (false, string.Empty, string.Empty, string.Empty, "Invalid credentials. Please try again.");

            if (manager != null && cashier == null)
            {
                await _auditLog.AddManagerAudit(manager, AuditActionType.Login, "Manager logged in", null);

                var roleToReturn = manager.Role == RoleType.Developer ? RoleType.Developer : RoleType.Manager;

                return (true, roleToReturn, manager.Email, $"{manager.FName} {manager.LName}", $"{roleToReturn} logged in successfully.");
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
                        IsTrainMode = isTrainMode,
                    };

                    _dataContext.Timestamp.Add(timestamp);

                    await _auditLog.AddManagerAudit(manager, AuditActionType.Approve, "Manager approved cashier logged in", null);
                    await _auditLog.AddCashierAudit(cashier, AuditActionType.Approve, "Manager approved cashier logged in", null);

                    await _dataContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, RoleType.Cashier, cashier.Email, $"{cashier.FName} {cashier.LName}", "Cashier logged in successfully.");
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
                .Where(u => (u.Email == managerEmail && u.Role != RoleType.Cashier) ||
                            (u.Email == cashierEmail && u.Role == RoleType.Cashier))
                .ToListAsync();

            var manager = users.FirstOrDefault(u => u.Email == managerEmail && u.Role != RoleType.Cashier);
            var cashier = users.FirstOrDefault(u => u.Email == cashierEmail && u.Role == RoleType.Cashier);

            // If neither found, fail
            if (manager == null || cashier == null)
                return (false, "Invalid credentials. Please try again.");

            var hasPendingInvoice = await _dataContext.Invoice
                .AnyAsync(i => i.Cashier == cashier && i.Status == InvoiceStatusType.Pending);

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

            await _auditLog.AddManagerAudit(manager, AuditActionType.Approve, "Manager approved cashier logged out", null);
            await _auditLog.AddCashierAudit(cashier, AuditActionType.Logout, "Cashier logged out", null);

            await _auditLog.AddManagerAudit(manager, AuditActionType.Approve, "Manager approved cashier cashed out", cash);
            await _auditLog.AddCashierAudit(cashier, AuditActionType.CashOutDrawer, "Cash out drawer set", cash);

            await _dataContext.SaveChangesAsync();

            return (true, "Cashier Logged Out!");
        }

        public async Task<(bool isSuccess, string message)> NewCashier(User cashier, string managerEmail)
        {
            if (cashier == null || string.IsNullOrWhiteSpace(cashier.Email) 
                || string.IsNullOrWhiteSpace(cashier.FName) || string.IsNullOrWhiteSpace(cashier.LName) 
                || string.IsNullOrWhiteSpace(cashier.Role))
                return (false, "All cashier fields are required.");

            if (cashier.Role != RoleType.Cashier)
                return (false, "Role must be 'Cashier'.");

            if (!new EmailAddressAttribute().IsValid(cashier.Email))
                return (false, "Invalid email format.");

            var isExisting = await _dataContext.User.AnyAsync(u => u.Email.ToLower() == cashier.Email.ToLower());
            if (isExisting)
                return (false, "A user with this email already exists.");

            var managerResult = await IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid manager credential.");

            cashier.IsActive = true;
            cashier.CreatedAt = DateTime.Now;
            cashier.UpdatedAt = DateTime.Now;
            _dataContext.User.Add(cashier);

            await _dataContext.SaveChangesAsync();
            await _auditLog.AddManagerAudit(managerResult.manager, 
                AuditActionType.AddCashier, $"Added new cashier: {cashier.FullName} ({cashier.Email})", null);
            return (true, "Cashier added successfully");
        }

        public async Task<(bool isSuccess, string message)> NewManager(User manager)
        {
            if (manager == null || string.IsNullOrWhiteSpace(manager.Email) || string.IsNullOrWhiteSpace(manager.FName) || string.IsNullOrWhiteSpace(manager.LName) || string.IsNullOrWhiteSpace(manager.Role))
                return (false, "All manager fields are required.");

            if (manager.Role != RoleType.Manager)
                return (false, "Role must be 'Manager'.");

            if (!new EmailAddressAttribute().IsValid(manager.Email))
                return (false, "Invalid email format.");

            var isExisting = await _dataContext.User.AnyAsync(u => u.Email.ToLower() == manager.Email.ToLower());
            if (isExisting)
                return (false, "A user with this email already exists.");

            manager.IsActive = true;
            manager.CreatedAt = DateTime.Now;
            manager.UpdatedAt = DateTime.Now;
            _dataContext.User.Add(manager);

            await _dataContext.SaveChangesAsync();
            await _auditLog.AddManagerAudit(manager, 
                AuditActionType.AddManager, $"Added new manager: {manager.FullName} ({manager.Email})", null);

            return (true, "Manager added successfully");
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

            await _auditLog.AddCashierAudit(cashier.cashier, AuditActionType.CashInDrawer, "Cash in drawer set", cash);

            return (true, "Cash in drawer set successfully!");
        }

        public async Task<(bool isSuccess, string message)> UpdateCashier(User cashier, string managerEmail)
        {
            if (cashier == null || string.IsNullOrWhiteSpace(cashier.Email) 
                || string.IsNullOrWhiteSpace(cashier.FName) || string.IsNullOrWhiteSpace(cashier.LName) 
                || string.IsNullOrWhiteSpace(cashier.Role))
                return (false, "All cashier fields are required.");

            if (cashier.Role != RoleType.Cashier)
                return (false, "Role must be 'Cashier'.");

            if (!new EmailAddressAttribute().IsValid(cashier.Email))
                return (false, "Invalid email format.");

            var existing = await _dataContext.User.FirstOrDefaultAsync(u => u.Email.ToLower() == cashier.Email.ToLower() && u.Role == RoleType.Cashier);
            if (existing == null)
                return (false, "Cashier not found.");

            var managerResult = await IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid manager credential.");

            existing.FName = cashier.FName;
            existing.LName = cashier.LName;
            existing.IsActive = cashier.IsActive;
            existing.UpdatedAt = DateTime.Now;

            _dataContext.User.Update(existing);
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(managerResult.manager, 
                AuditActionType.Update, $"Updated cashier: {existing.FullName} ({existing.Email})", null);

            return (true, "Cashier updated successfully");
        }

        public async Task<(bool isSuccess, string message)> UpdateManager(User manager)
        {
            if (manager == null || string.IsNullOrWhiteSpace(manager.Email) 
                || string.IsNullOrWhiteSpace(manager.FName) || string.IsNullOrWhiteSpace(manager.LName) 
                || string.IsNullOrWhiteSpace(manager.Role))
                return (false, "All manager fields are required.");

            if (manager.Role != RoleType.Manager)
                return (false, "Role must be 'Manager'.");

            if (!new EmailAddressAttribute().IsValid(manager.Email))
                return (false, "Invalid email format.");

            var existing = await _dataContext.User.FirstOrDefaultAsync(u => u.Email.ToLower() == manager.Email.ToLower() && u.Role == RoleType.Manager);
            if (existing == null)
                return (false, "Manager not found.");

            existing.FName = manager.FName;
            existing.LName = manager.LName;
            existing.IsActive = manager.IsActive;
            existing.UpdatedAt = DateTime.Now;

            _dataContext.User.Update(existing);
            await _dataContext.SaveChangesAsync();

            await _auditLog.AddManagerAudit(existing, AuditActionType.Update, 
                $"Updated manager: {existing.FullName} ({existing.Email})", null);

            return (true, "Manager updated successfully");
        }

        public async Task<User[]> Users()
        {
            return await _dataContext.User.OrderBy(u => u.FName).ThenBy(u => u.LName).ToArrayAsync();
        }
    }
}
