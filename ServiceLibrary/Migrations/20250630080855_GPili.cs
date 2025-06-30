using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceLibrary.Migrations
{
    /// <inheritdoc />
    public partial class GPili : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountJournal",
                columns: table => new
                {
                    UniqueId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Entry_Type = table.Column<string>(type: "TEXT", nullable: false),
                    Entry_No = table.Column<string>(type: "TEXT", nullable: false),
                    Entry_Line_No = table.Column<string>(type: "TEXT", nullable: false),
                    Entry_Date = table.Column<string>(type: "TEXT", nullable: false),
                    CostCenter = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: false),
                    Qty = table.Column<string>(type: "TEXT", nullable: false),
                    Cost = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<string>(type: "TEXT", nullable: false),
                    TotalPrice = table.Column<string>(type: "TEXT", nullable: false),
                    Debit = table.Column<string>(type: "TEXT", nullable: false),
                    Credit = table.Column<string>(type: "TEXT", nullable: false),
                    AccountBalance = table.Column<string>(type: "TEXT", nullable: false),
                    Prev_Reading = table.Column<string>(type: "TEXT", nullable: false),
                    Curr_Reading = table.Column<string>(type: "TEXT", nullable: false),
                    Memo = table.Column<string>(type: "TEXT", nullable: false),
                    AccountName = table.Column<string>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: false),
                    Entry_Name = table.Column<string>(type: "TEXT", nullable: false),
                    Cashier = table.Column<string>(type: "TEXT", nullable: false),
                    Count_Type = table.Column<string>(type: "TEXT", nullable: false),
                    Deposited = table.Column<string>(type: "TEXT", nullable: false),
                    Deposit_Date = table.Column<string>(type: "TEXT", nullable: false),
                    Deposit_Reference = table.Column<string>(type: "TEXT", nullable: false),
                    Deposit_By = table.Column<string>(type: "TEXT", nullable: false),
                    Deposit_Time = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", nullable: false),
                    SubTotal = table.Column<string>(type: "TEXT", nullable: false),
                    TotalTax = table.Column<string>(type: "TEXT", nullable: false),
                    GrossTotal = table.Column<string>(type: "TEXT", nullable: false),
                    Discount_Type = table.Column<string>(type: "TEXT", nullable: false),
                    Discount_Amount = table.Column<string>(type: "TEXT", nullable: false),
                    NetPayable = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    User_Email = table.Column<string>(type: "TEXT", nullable: false),
                    QtyPerBaseUnit = table.Column<string>(type: "TEXT", nullable: false),
                    QtyBalanceInBaseUnit = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountJournal", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CtgryName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PosTerminalInfo",
                columns: table => new
                {
                    PosSerialNumber = table.Column<string>(type: "TEXT", nullable: false),
                    MinNumber = table.Column<string>(type: "TEXT", nullable: false),
                    AccreditationNumber = table.Column<string>(type: "TEXT", nullable: false),
                    PtuNumber = table.Column<string>(type: "TEXT", nullable: false),
                    DateIssued = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PosName = table.Column<string>(type: "TEXT", nullable: false),
                    RegisteredName = table.Column<string>(type: "TEXT", nullable: false),
                    OperatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    VatTinNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Vat = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscountMax = table.Column<decimal>(type: "TEXT", nullable: false),
                    CostCenter = table.Column<string>(type: "TEXT", nullable: false),
                    BranchCenter = table.Column<string>(type: "TEXT", nullable: false),
                    PrinterName = table.Column<string>(type: "TEXT", nullable: false),
                    ResetCounterNo = table.Column<int>(type: "INTEGER", nullable: false),
                    ResetCounterTrainNo = table.Column<int>(type: "INTEGER", nullable: false),
                    ZCounterNo = table.Column<int>(type: "INTEGER", nullable: false),
                    ZCounterTrainNo = table.Column<int>(type: "INTEGER", nullable: false),
                    IsTrainMode = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosTerminalInfo", x => x.PosSerialNumber);
                });

            migrationBuilder.CreateTable(
                name: "SaleType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Account = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    FName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Email);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProdId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Barcode = table.Column<string>(type: "TEXT", nullable: false),
                    BaseUnit = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: true),
                    Cost = table.Column<decimal>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    ItemType = table.Column<string>(type: "TEXT", nullable: false),
                    VatType = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CashierEmail = table.Column<string>(type: "TEXT", nullable: true),
                    ManagerEmail = table.Column<string>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Changes = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: true),
                    isTrainMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLog_User_CashierEmail",
                        column: x => x.CashierEmail,
                        principalTable: "User",
                        principalColumn: "Email");
                    table.ForeignKey(
                        name: "FK_AuditLog_User_ManagerEmail",
                        column: x => x.ManagerEmail,
                        principalTable: "User",
                        principalColumn: "Email");
                });

            migrationBuilder.CreateTable(
                name: "Invoice",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    SubTotal = table.Column<decimal>(type: "TEXT", nullable: true),
                    CashTendered = table.Column<decimal>(type: "TEXT", nullable: true),
                    DueAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    TotalTendered = table.Column<decimal>(type: "TEXT", nullable: true),
                    ChangeAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    VatSales = table.Column<decimal>(type: "TEXT", nullable: true),
                    VatExempt = table.Column<decimal>(type: "TEXT", nullable: true),
                    VatAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    VatZero = table.Column<decimal>(type: "TEXT", nullable: true),
                    CustomerName = table.Column<string>(type: "TEXT", nullable: false),
                    EligibleDiscName = table.Column<string>(type: "TEXT", nullable: true),
                    OSCAIdNum = table.Column<string>(type: "TEXT", nullable: true),
                    DiscountType = table.Column<string>(type: "TEXT", nullable: true),
                    DiscountPercent = table.Column<int>(type: "INTEGER", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    CashierEmail = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StatusChangeDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsTrainMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrintCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoice_User_CashierEmail",
                        column: x => x.CashierEmail,
                        principalTable: "User",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Timestamp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TsIn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TsOut = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CashInDrawerAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    CashOutDrawerAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    WithdrawnDrawerAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    IsTrainMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    CashierEmail = table.Column<string>(type: "TEXT", nullable: false),
                    ManagerInEmail = table.Column<string>(type: "TEXT", nullable: false),
                    ManagerOutEmail = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timestamp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timestamp_User_CashierEmail",
                        column: x => x.CashierEmail,
                        principalTable: "User",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Timestamp_User_ManagerInEmail",
                        column: x => x.ManagerInEmail,
                        principalTable: "User",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Timestamp_User_ManagerOutEmail",
                        column: x => x.ManagerOutEmail,
                        principalTable: "User",
                        principalColumn: "Email");
                });

            migrationBuilder.CreateTable(
                name: "Inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventory_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EPayment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Reference = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<long>(type: "INTEGER", nullable: false),
                    SaleTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EPayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EPayment_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EPayment_SaleType_SaleTypeId",
                        column: x => x.SaleTypeId,
                        principalTable: "SaleType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Item",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Qty = table.Column<decimal>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    SubTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    IsTrainingMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    InvoiceId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Item", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Item_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Item_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_CashierEmail",
                table: "AuditLog",
                column: "CashierEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_ManagerEmail",
                table: "AuditLog",
                column: "ManagerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_EPayment_InvoiceId",
                table: "EPayment",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_EPayment_SaleTypeId",
                table: "EPayment",
                column: "SaleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_ProductId",
                table: "Inventory",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_CashierEmail",
                table: "Invoice",
                column: "CashierEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Item_InvoiceId",
                table: "Item",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Item_ProductId",
                table: "Item",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_CategoryId",
                table: "Product",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Timestamp_CashierEmail",
                table: "Timestamp",
                column: "CashierEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Timestamp_ManagerInEmail",
                table: "Timestamp",
                column: "ManagerInEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Timestamp_ManagerOutEmail",
                table: "Timestamp",
                column: "ManagerOutEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountJournal");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "EPayment");

            migrationBuilder.DropTable(
                name: "Inventory");

            migrationBuilder.DropTable(
                name: "Item");

            migrationBuilder.DropTable(
                name: "PosTerminalInfo");

            migrationBuilder.DropTable(
                name: "Timestamp");

            migrationBuilder.DropTable(
                name: "SaleType");

            migrationBuilder.DropTable(
                name: "Invoice");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Category");
        }
    }
}
