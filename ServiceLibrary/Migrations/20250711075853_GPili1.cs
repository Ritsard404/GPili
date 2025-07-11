using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceLibrary.Migrations
{
    /// <inheritdoc />
    public partial class GPili1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTrainMode",
                table: "InvoiceDocument",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTrainMode",
                table: "InvoiceDocument");
        }
    }
}
