using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceLibrary.Migrations
{
    /// <inheritdoc />
    public partial class GPili7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoidedByEmail",
                table: "Invoice",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_VoidedByEmail",
                table: "Invoice",
                column: "VoidedByEmail");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoice_User_VoidedByEmail",
                table: "Invoice",
                column: "VoidedByEmail",
                principalTable: "User",
                principalColumn: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoice_User_VoidedByEmail",
                table: "Invoice");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_VoidedByEmail",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "VoidedByEmail",
                table: "Invoice");
        }
    }
}
