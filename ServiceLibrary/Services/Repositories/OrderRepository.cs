using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Order;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;

namespace ServiceLibrary.Services.Repositories
{
    public class OrderRepository(DataContext _dataContext, IGPiliTerminalMachine _terminalMachine, IAuth _auth, IAuditLog _auditLog, IReport _report, IPrinterService _printer) : IOrder
    {
        public async Task<(bool isSuccess, string message)> AddOrderItem(long prodId, decimal qty, string cashierEmail)
        {
            var product = await _dataContext.Product.FirstOrDefaultAsync(p => p.Id == prodId && p.IsAvailable);
            if (product == null)
                return (false, "Invalid product. Please check and try again.");

            var cashierResult = await _auth.IsCashierValid(cashierEmail);
            if (!cashierResult.isSuccess || cashierResult.cashier == null)
                return (false, "Invalid cashier email. Please check and try again.");

            if (qty <= 0)
                return (false, "Invalid quantity.");

            var existItem = await _dataContext.Item.FirstOrDefaultAsync(i => i.Product.Id == prodId && i.Product.IsAvailable);

            var isTrainMode = await _terminalMachine.IsTrainMode();
            var pendingOrder = await PendingOrder(isTrainMode);
            long invNum = await GenerateInvoiceNumberAsync(isTrainMode);

            if (pendingOrder == null)
            {
                pendingOrder = new Invoice
                {
                    InvoiceNumber = invNum,
                    Status = InvoiceStatusType.Pending,
                    Cashier = cashierResult.cashier,
                    TotalAmount = 0, // Will be updated below
                    IsTrainMode = isTrainMode,
                    CreatedAt = DateTime.UtcNow
                };
                _dataContext.Invoice.Add(pendingOrder);
            }

            if (existItem == null)
            {

                // Create the new item
                var item = new Item
                {
                    Qty = qty,
                    Price = product.Price,
                    SubTotal = product.Price * qty,
                    Status = InvoiceStatusType.Pending,
                    IsTrainingMode = isTrainMode,
                    CreatedAt = DateTime.UtcNow,
                    Product = product,
                    Invoice = pendingOrder
                };

                // Add item to invoice and context
                pendingOrder.Items.Add(item);
                _dataContext.Item.Add(item);

                // Log the edit action
                await _auditLog.AddCashierAudit(cashierResult.cashier, "Add Item", $"New item added named {product.Name} to the order {invNum:D12}", null);

            }
            else
            {
                existItem.Qty += qty;
                existItem.SubTotal = existItem.Qty * existItem.Price;

            }

            await _dataContext.SaveChangesAsync();

            return (true, "Item added successfully.");
        }


        public async Task<(bool isSuccess, string message)> EditQtyTotalPriceItem(long itemId, decimal qty, decimal subtotal)
        {
            var existingItem = await _dataContext.Item
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.Status == InvoiceStatusType.Pending);

            if (existingItem == null)
                return (false, "Item not found.");
            if (qty <= 0 || subtotal <= 0)
                return (false, "Quantity and subtotal must be greater than zero.");

            var isTrainMode = await _terminalMachine.IsTrainMode();

            // Check if there is a pending order
            var invoice = await PendingOrder(isTrainMode);
            if (invoice == null)
                return (false, "No pending order found.");

            // Update item details
            existingItem.Qty = qty;
            existingItem.SubTotal = subtotal;
            existingItem.Price = subtotal / qty; // Update price based on new subtotal and quantity
            existingItem.UpdatedAt = DateTime.UtcNow;
            _dataContext.Item.Update(existingItem);

            // Log the edit action
            await _auditLog.AddCashierAudit(existingItem.Invoice.Cashier, "Edit Item", $"Edited item ID {existingItem.Id} - New Qty: {qty}, New SubTotal: {subtotal}", null);

            await _dataContext.SaveChangesAsync();

            return (true, "Item updated successfully!");
        }

        public async Task<List<Item>> GetPendingItems()
        {
            var isTrainMode = await _terminalMachine.IsTrainMode();
            var pendingOrder = await PendingOrder(isTrainMode);

            return await _dataContext.Item
                .Where(i => i.Invoice == pendingOrder && i.Status == InvoiceStatusType.Pending)
                .ToListAsync();
        }

        public async Task<List<Product>> GetProducts()
        {
            return await _dataContext.Product
                .Where(p => p.IsAvailable)
                .ToListAsync();
        }

        public async Task<(bool isSuccess, string message)> ReturnInvoice(string managerEmail, long invoiceNumber)
        {
            var isTrainMode = await _terminalMachine.IsTrainMode();
            var managerResult = _auth.IsManagerValid(managerEmail);
            if (!managerResult.Result.isSuccess || managerResult.Result.manager == null)
                return (false, "Invalid manager email. Please check and try again.");

            var invoiceToRefund = await _dataContext.Invoice
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber &&
                    i.Status == InvoiceStatusType.Paid &&
                    i.IsTrainMode == isTrainMode);
            if (invoiceToRefund == null)
                return (false, "Invoice not found or not in a valid state for return.");

            if (DateTime.Now - invoiceToRefund.CreatedAt > TimeSpan.FromDays(5))
                return (false, "Refund period has expired (more than 5 days since purchase).");


            foreach (var item in invoiceToRefund.Items)
            {
                if (item.Status == InvoiceStatusType.Paid)
                {
                    item.Status = InvoiceStatusType.Returned;
                    item.UpdatedAt = DateTime.UtcNow;
                }
                _dataContext.Item.Update(item);
            }

            // Check if the invoice is already returned
            invoiceToRefund.Status = InvoiceStatusType.Returned;
            invoiceToRefund.StatusChangeDate = DateTime.UtcNow;

            // Log the return action
            await _auditLog.AddManagerAudit(managerResult.Result.manager, "Return Invoice", $"Invoice {invoiceNumber:D12} returned by {managerResult.Result.manager.Email}", null);

            await _dataContext.SaveChangesAsync();

            // add to journal
            await _auditLog.AddPwdScJournal(invoiceToRefund.Id);
            await _auditLog.AddItemsJournal(invoiceToRefund.Id);
            await _auditLog.AddTendersJournal(invoiceToRefund.Id);
            await _auditLog.AddTotalsJournal(invoiceToRefund.Id);


            return (true, "Invoice successfully returned!");
        }

        public async Task<(bool isSuccess, string message)> ReturnItems(string managerEmail, long invoiceNumber, List<Item> items)
        {
            var isTrainMode = await _terminalMachine.IsTrainMode();
            var managerResult = await _auth.IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid manager email. Please check and try again.");

            var invoiceToRefund = await _dataContext.Invoice
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber &&
                    i.Status == InvoiceStatusType.Paid);

            if (invoiceToRefund == null)
                return (false, "Invoice not found or not in a valid state for return.");

            if (DateTime.Now - invoiceToRefund.CreatedAt > TimeSpan.FromDays(5))
                return (false, "Refund period has expired (more than 5 days since purchase).");

            foreach (var item in items)
            {
                var itemToReturn = invoiceToRefund.Items.FirstOrDefault(i => i.Id == item.Id && i.Status == InvoiceStatusType.Paid);
                if (itemToReturn == null)
                    return (false, $"Item with ID {item.Id} not found or not in a valid state for return.");

                itemToReturn.Status = InvoiceStatusType.Returned;
                itemToReturn.UpdatedAt = DateTime.UtcNow;
                _dataContext.Item.Update(itemToReturn);
            }

            // Check if the invoice is already returned
            invoiceToRefund.Status = InvoiceStatusType.Returned;
            invoiceToRefund.StatusChangeDate = DateTime.UtcNow;

            // Log the return action
            await _auditLog.AddManagerAudit(managerResult.manager, "Return Items", $"Items returned from invoice {invoiceNumber:D12} by {managerResult.manager.Email}", null);
            await _dataContext.SaveChangesAsync();

            // add to journal
            await _auditLog.AddPwdScJournal(invoiceToRefund.Id);
            await _auditLog.AddItemsJournal(invoiceToRefund.Id);
            await _auditLog.AddTendersJournal(invoiceToRefund.Id);
            await _auditLog.AddTotalsJournal(invoiceToRefund.Id);
            return (true, "Items successfully returned!");
        }

        public async Task<(bool isSuccess, string message)> VoidItem(string mgrEmail, string cashrEmail, long itemId)
        {
            var isTrainMode = await _terminalMachine.IsTrainMode();
            var managerResult = await _auth.IsManagerValid(mgrEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid manager email. Please check and try again.");

            var cashierResult = await _auth.IsCashierValid(cashrEmail);
            if (!cashierResult.isSuccess || cashierResult.cashier == null)
                return (false, "Invalid cashier email. Please check and try again.");

            var itemToVoid = await _dataContext.Item
                .Include(i => i.Invoice)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.Status == InvoiceStatusType.Pending);
            if (itemToVoid == null)
                return (false, "Item not found or not in a valid state for voiding.");


            itemToVoid.Status = InvoiceStatusType.Void;
            itemToVoid.UpdatedAt = DateTime.UtcNow;

            _dataContext.Item.Update(itemToVoid);

            // Log the void action
            await _auditLog.AddManagerAudit(managerResult.manager, "Void Item", $"Item ID {itemId} voided by {managerResult.manager.Email} at the request of cashier {cashierResult.cashier.Email}", itemToVoid.SubTotal);
            await _auditLog.AddCashierAudit(cashierResult.cashier, "Void Item", $"Item ID {itemId} voided at the request of manager {managerResult.manager.Email}", itemToVoid.SubTotal);

            return (true, "Item voided successfully!");
        }

        public async Task<(bool isSuccess, string message)> VoidOrder(string cashierEmail, string managerEmail)
        {
            var cashierResult = await _auth.IsCashierValid(cashierEmail);
            if (!cashierResult.isSuccess || cashierResult.cashier == null)
                return (false, "Invalid cashier email. Please check and try again.");
            var managerResult = await _auth.IsManagerValid(managerEmail);
            if (!managerResult.isSuccess || managerResult.manager == null)
                return (false, "Invalid manager email. Please check and try again.");

            var isTrainMode = await _terminalMachine.IsTrainMode();

            var pendingOrder = await PendingOrder(isTrainMode);
            if (pendingOrder == null)
                return (false, "No pending order found.");

            // Check if the order is already voided
            if (pendingOrder.Status == InvoiceStatusType.Void)
                return (false, "Order is already voided.");

            // Set the items to Void status
            foreach (var item in pendingOrder.Items)
            {
                if (item.Status == InvoiceStatusType.Pending)
                {
                    item.Status = InvoiceStatusType.Void;
                    item.UpdatedAt = DateTime.UtcNow;
                    _dataContext.Item.Update(item);
                }
            }

            // Set the order status to Void
            pendingOrder.Status = InvoiceStatusType.Void;
            pendingOrder.StatusChangeDate = DateTime.UtcNow;
            _dataContext.Invoice.Update(pendingOrder);

            // Log the void action
            await _auditLog.AddManagerAudit(managerResult.manager, "Void Order", $"Order {pendingOrder.InvoiceNumber:D12} voided by {managerResult.manager.Email} at the request of cashier {cashierResult.cashier.Email}", null);
            await _auditLog.AddCashierAudit(cashierResult.cashier, "Void Order", $"Order {pendingOrder.InvoiceNumber:D12} voided at the request of manager {managerResult.manager.Email}", null);
            await _dataContext.SaveChangesAsync();

            // add to journal
            await _auditLog.AddPwdScJournal(pendingOrder.Id);
            await _auditLog.AddItemsJournal(pendingOrder.Id);
            await _auditLog.AddTendersJournal(pendingOrder.Id);
            await _auditLog.AddTotalsJournal(pendingOrder.Id);
            return (true, "Order voided successfully!");
        }
        private async Task<Invoice?> PendingOrder(bool isTrainMode)
        {
            return await _dataContext.Invoice
                .Include(i => i.Items)
                .FirstOrDefaultAsync(p => p.Status == InvoiceStatusType.Pending && p.IsTrainMode == isTrainMode);
        }

        public async Task<long> GenerateInvoiceNumberAsync(bool isTrainingMode)
        {
            // Get the latest order number
            var latestOrder = await _dataContext.Invoice
                .Where(o => o.IsTrainMode == isTrainingMode)
                .OrderByDescending(o => o.InvoiceNumber)
                .FirstOrDefaultAsync();

            return latestOrder?.InvoiceNumber + 1 ?? 1;
        }

        public async Task<(bool isSuccess, string message, InvoiceDTO? invoiceInfo)> PayOrder(PayOrderDTO pay)
        {
            var isTrainMode = await _terminalMachine.IsTrainMode();

            var cashierResult = await _auth.IsCashierValid(pay.CashierEmail);
            if (!cashierResult.isSuccess || cashierResult.cashier == null)
                return (false, "Invalid cashier email. Please check and try again.", null);

            var pendingOrder = await PendingOrder(isTrainMode);
            if (pendingOrder == null)
                return (false, "No pending order found.", null);

            var invoice = await _report.GetInvoiceById(pendingOrder.Id);
            if (invoice == null)
                return (false, "Invoice not found.", null);

            pendingOrder.Status = InvoiceStatusType.Paid;
            pendingOrder.StatusChangeDate = DateTime.UtcNow;
            pendingOrder.TotalAmount = pay.TotalAmount;
            pendingOrder.SubTotal = pay.SubTotal;
            pendingOrder.Cashier = cashierResult.cashier;
            pendingOrder.CashTendered = pay.CashTendered;
            pendingOrder.DueAmount = pay.DueAmount;
            pendingOrder.TotalTendered = pay.TotalTendered;
            pendingOrder.ChangeAmount = pay.ChangeAmount;
            pendingOrder.DiscountAmount = pay.DiscountAmount;
            pendingOrder.VatSales = pay.VatSales;
            pendingOrder.VatExempt = pay.VatExempt;
            pendingOrder.VatAmount = pay.VatAmount;
            pendingOrder.VatZero = pay.VatZero;

            await _auditLog.AddCashierAudit(
                cashierResult.cashier,
                "Pay Order",
                $"Cashier {cashierResult.cashier.Email} successfully processed payment for Order #{pendingOrder.InvoiceNumber.InvoiceFormat()} with a total amount of {pay.TotalAmount.PesoFormat()}.",
                pay.TotalAmount
            );

            await _auditLog.AddItemsJournal(pendingOrder.Id);
            await _auditLog.AddTendersJournal(pendingOrder.Id);
            await _auditLog.AddTotalsJournal(pendingOrder.Id);

            await _dataContext.SaveChangesAsync();

            // Print the invoice
            await _printer.PrintInvoice(invoice);

            return (true, "Order paid successfully!", invoice);
        }
    }
}
