using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.DTO.Order;
using ServiceLibrary.Services.DTO.Report;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using System.Diagnostics;
using System.Linq;

namespace ServiceLibrary.Services.Repositories
{
    public class OrderRepository(DataContext _dataContext, 
        IGPiliTerminalMachine _terminalMachine, 
        IAuth _auth, IAuditLog _auditLog, 
        IReport _report, IPrinterService _printer,
        IInventory _inventory) : IOrder
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

            if (product.Quantity < qty)
                return (false, $"Insufficient stock. Only {product.Quantity} “{product.Name}” left in inventory.");

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
                    GrossAmount = 0,
                    IsTrainMode = isTrainMode,
                    CreatedAt = DateTime.Now
                };
                _dataContext.Invoice.Add(pendingOrder);
            }

            var existItem = await _dataContext.Item.FirstOrDefaultAsync(i => i.Product.Id == prodId &&
                    i.Product.IsAvailable &&
                    i.Invoice.Id == pendingOrder.Id &&
                    i.Status == InvoiceStatusType.Pending);

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
                    CreatedAt = DateTime.Now,
                    Product = product,
                    Invoice = pendingOrder
                };

                // Add item to invoice and context
                pendingOrder.Items.Add(item);
                _dataContext.Item.Add(item);

                // Log the new item addition
                await _auditLog.AddCashierAudit(
                    cashierResult.cashier,
                    AuditActionType.AddItem,
                    $"New item '{product.Name}' (Qty: {qty}) added to invoice {invNum:D12}.",
                    null
                );
            }
            else
            {
                var oldQty = existItem.Qty;
                existItem.Qty += qty;
                existItem.SubTotal = existItem.Qty * existItem.Price;

                // Log the item update
                await _auditLog.AddCashierAudit(
                    cashierResult.cashier,
                    AuditActionType.Update,
                    $"Updated item '{product.Name}' in invoice {invNum:D12}. Quantity changed from {oldQty} to {existItem.Qty}.",
                    null
                );
            }

            await _dataContext.SaveChangesAsync();

            return (true, "Item added successfully.");
        }
        public async Task<(bool isSuccess, string message)> EditQtyTotalPriceItem(long itemId, decimal qty, decimal subtotal)
        {
            var existingItem = await _dataContext.Item
                .Include(i => i.Product)
                .Include(i => i.Invoice)
                .FirstOrDefaultAsync(i => i.Id == itemId);


            if (existingItem == null)
                return (false, "Item not found.");

            if (qty <= 0 || subtotal <= 0)
                return (false, "Quantity and subtotal must be greater than zero.");

            var oldQty = existingItem.Qty;
            if(existingItem.Product.Quantity < qty + oldQty) 
                return (false, $"Insufficient stock. Only {existingItem.Product.Quantity} “{existingItem.Product.Name}” left in inventory.");


            var isTrainMode = await _terminalMachine.IsTrainMode();

            // Check if there is a pending order
            var invoice = await PendingOrder(isTrainMode);
            if (invoice == null)
                return (false, "No pending order found.");

            // Update item details
            existingItem.Qty = qty;
            existingItem.SubTotal = subtotal;
            existingItem.Price = subtotal / qty; // Update price based on new subtotal and quantity
            existingItem.UpdatedAt = DateTime.Now;
            _dataContext.Item.Update(existingItem);

            // Log the edit action
            await _auditLog.AddCashierAudit(existingItem.Invoice.Cashier,
                AuditActionType.UpdateItem,
                $"Edited item ID {existingItem.Id} - New Qty: {qty}, New SubTotal: {subtotal}",
                null);

            await _dataContext.SaveChangesAsync();

            return (true, "Item updated successfully!");
        }

        public async Task<List<Item>> GetPendingItems()
        {
            var isTrainMode = await _terminalMachine.IsTrainMode();
            var pendingOrder = await PendingOrder(isTrainMode);

            return await _dataContext.Item
                .Include(p => p.Product)
                .Include(p => p.Invoice)
                .Where(i => i.Invoice == pendingOrder &&
                    i.Status == InvoiceStatusType.Pending)
                .AsNoTracking()
                .OrderDescending()
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
                    item.UpdatedAt = DateTime.Now;
                    // Record inventory IN transaction for each returned item
                    await _inventory.RecordInventoryTransaction(
                        InventoryAction.Actions.In,
                        item.Product,
                        item.Qty,
                        $"Return Invoice #{invoiceToRefund.InvoiceNumber}",
                        managerResult.Result.manager
                    );
                }
                _dataContext.Item.Update(item);
            }

            // Check if the invoice is already returned
            invoiceToRefund.Status = InvoiceStatusType.Returned;
            invoiceToRefund.StatusChangeDate = DateTime.Now;

            // Log the return action
            await _auditLog.AddManagerAudit(managerResult.Result.manager,
                AuditActionType.ReturnInvoice,
                $"Invoice {invoiceNumber:D12} returned by {managerResult.Result.manager.Email}", null);

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

            decimal returnAmount = 0m;
            foreach (var item in items)
            {
                var itemToReturn = invoiceToRefund.Items.FirstOrDefault(i => i.Id == item.Id && i.Status == InvoiceStatusType.Paid);
                if (itemToReturn == null)
                    return (false, $"Item with ID {item.Id} not found or not in a valid state for return.");

                itemToReturn.Status = InvoiceStatusType.Returned;
                itemToReturn.UpdatedAt = DateTime.Now;
                returnAmount += itemToReturn.SubTotal;
                _dataContext.Item.Update(itemToReturn);

                // Record inventory IN transaction for each returned item
                await _inventory.RecordInventoryTransaction(
                    InventoryAction.Actions.In,
                    itemToReturn.Product,
                    itemToReturn.Qty,
                    $"Return Item #{itemToReturn.Id} from Invoice #{invoiceToRefund.InvoiceNumber}",
                    managerResult.manager
                );
            }

            // Check if the invoice is already returned
            invoiceToRefund.Status = InvoiceStatusType.Returned;
            invoiceToRefund.StatusChangeDate = DateTime.Now;
            invoiceToRefund.ReturnedAmount = returnAmount;

            // Log the return action
            await _auditLog.AddManagerAudit(managerResult.manager, AuditActionType.ReturnItem, $"Items returned from invoice {invoiceNumber:D12} by {managerResult.manager.Email}", null);
            await _dataContext.SaveChangesAsync();



            // add to journal
            await _auditLog.AddPwdScJournal(invoiceToRefund.Id);
            await _auditLog.AddItemsJournal(invoiceToRefund.Id);
            await _auditLog.AddTendersJournal(invoiceToRefund.Id);
            await _auditLog.AddTotalsJournal(invoiceToRefund.Id);

            var returnedInvoice = await _report.GetInvoiceById(invoiceToRefund.Id);
            await _printer.PrintInvoice(returnedInvoice!);

            return (true, "Items successfully returned!");
        }

        public async Task<(bool isSuccess, string message)> VoidItem(string mgrEmail, string cashrEmail, long itemId)
        {
            var isTrainMode = await _terminalMachine.IsTrainMode();
            var managerResult = await _auth.IsManagerValid(mgrEmail);
            if (!(managerResult.isSuccess && managerResult.manager != null))
                return (false, "Invalid manager email. Please check and try again.");

            var cashierResult = await _auth.IsCashierValid(cashrEmail);
            if (!cashierResult.isSuccess || cashierResult.cashier == null)
                return (false, "Invalid cashier email. Please check and try again.");

            var itemToVoid = await _dataContext.Item
                .Include(i => i.Invoice)
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == itemId);
            if (itemToVoid == null)
                return (false, "Item not found or not in a valid state for voiding.");

            var wasPaid = itemToVoid.Status == InvoiceStatusType.Paid;

            itemToVoid.Status = InvoiceStatusType.Void;
            itemToVoid.UpdatedAt = DateTime.Now;
            _dataContext.Item.Update(itemToVoid);

            // If the item was paid, return stock
            if (wasPaid)
            {
                await _inventory.RecordInventoryTransaction(
                    InventoryAction.Actions.In,
                    itemToVoid.Product,
                    itemToVoid.Qty,
                    $"Void Item #{itemToVoid.Id}",
                    managerResult.manager
                );
            }

            // Log the void action
            await _auditLog.AddManagerAudit(managerResult.manager, AuditActionType.VoidItem, $"Item ID {itemId} voided by {managerResult.manager.Email} at the request of cashier {cashierResult.cashier.Email}", itemToVoid.SubTotal);
            await _auditLog.AddCashierAudit(cashierResult.cashier, AuditActionType.VoidItem, $"Item ID {itemId} voided at the request of manager {managerResult.manager.Email}", itemToVoid.SubTotal);

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
                if (item.Status == InvoiceStatusType.Pending || item.Status == InvoiceStatusType.Paid)
                {
                    // If the item was paid, return stock
                    if (item.Status == InvoiceStatusType.Paid)
                    {
                        await _inventory.RecordInventoryTransaction(
                            InventoryAction.Actions.In,
                            item.Product,
                            item.Qty,
                            $"Void Order #{pendingOrder.InvoiceNumber}",
                            managerResult.manager
                        );
                    }
                    item.Status = InvoiceStatusType.Void;
                    item.UpdatedAt = DateTime.Now;
                    _dataContext.Item.Update(item);
                }
            }

            // Set the order status to Void
            pendingOrder.Status = InvoiceStatusType.Void;
            pendingOrder.StatusChangeDate = DateTime.Now;
            _dataContext.Invoice.Update(pendingOrder);

            // Log the void action
            await _auditLog.AddManagerAudit(managerResult.manager, AuditActionType.VoidOrder, $"Order {pendingOrder.InvoiceNumber:D12} voided by {managerResult.manager.Email} at the request of cashier {cashierResult.cashier.Email}", null);
            await _auditLog.AddCashierAudit(cashierResult.cashier, AuditActionType.VoidOrder, $"Order {pendingOrder.InvoiceNumber:D12} voided at the request of manager {managerResult.manager.Email}", null);
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
            await using var transaction = await _dataContext.Database.BeginTransactionAsync();
            try
            {
                var isTrainMode = await _terminalMachine.IsTrainMode();

                if (pay.ChangeAmount < 0)
                    return (false, "Invalid amount to pay. Please check and try again.", null);

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
                pendingOrder.StatusChangeDate = DateTime.Now;
                pendingOrder.TotalAmount = pay.TotalAmount;
                pendingOrder.GrossAmount = pay.GrossAmount;
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



                // Map Discount
                if (pay.Discount != null)
                {
                    pendingOrder.EligibleDiscName = pay.Discount.EligibleDiscName;
                    pendingOrder.OSCAIdNum = pay.Discount.OSCAIdNum;
                    pendingOrder.DiscountType = pay.Discount.DiscountType;
                    pendingOrder.DiscountPercent = pay.Discount.DiscountPercent;
                }

                // Map EPayments
                pendingOrder.EPayments = new List<EPayment>();
                foreach (var dto in pay.OtherPayment)
                {
                    var saleType = await _dataContext.SaleType.FirstOrDefaultAsync(st => st.Id == dto.SaleTypeId);
                    if (saleType == null)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Invalid SaleTypeId: {dto.SaleTypeId}", null);
                    }

                    var ePayment = new EPayment
                    {
                        Reference = dto.Reference,
                        Amount = dto.Amount,
                        SaleType = saleType,
                        Invoice = pendingOrder
                    };
                    pendingOrder.EPayments.Add(ePayment);
                }

                foreach (var item in pendingOrder.Items)
                {
                    item.Status = InvoiceStatusType.Paid;
                    // Record inventory OUT transaction for each item
                    await _inventory.RecordInventoryTransaction(
                        InventoryAction.Actions.Out,
                        item.Product,
                        item.Qty,
                        $"Invoice #{pendingOrder.InvoiceNumber}",
                        cashierResult.cashier
                    );
                }

                await _auditLog.AddCashierAudit(
                    cashierResult.cashier,
                    AuditActionType.PayOrder,
                    $"Cashier {cashierResult.cashier.Email} successfully processed payment for Order #{pendingOrder.InvoiceNumber.InvoiceFormat()} with a total amount of {pay.TotalAmount.PesoFormat()}.",
                    pay.TotalAmount
                );

                await _auditLog.AddItemsJournal(pendingOrder.Id);
                await _auditLog.AddTendersJournal(pendingOrder.Id);
                await _auditLog.AddTotalsJournal(pendingOrder.Id);

                await _dataContext.SaveChangesAsync();

                // Print the invoice
                invoice = await _report.GetInvoiceById(pendingOrder.Id);
                await _printer.PrintInvoice(invoice!);

                await transaction.CommitAsync();
                return (true, "Order paid successfully!", invoice);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Debug.WriteLine($"Payment failed: {ex.Message}");
                return (false, $"Payment failed: {ex.Message}", null);
            }
        }

        public async Task<List<Item>> GetToRefundItems(long invNum)
        {
            var isTrainMode = await _terminalMachine.IsTrainMode();

            return await _dataContext.Item
                .Include(i => i.Product)
                .Include(i => i.Invoice)
                .Where(i => i.Invoice.InvoiceNumber == invNum &&
                    i.Invoice.Status == InvoiceStatusType.Paid 
                    && i.IsTrainingMode == isTrainMode)
                .ToListAsync();
        }
    }
}
