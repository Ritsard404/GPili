using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceLibrary.Migrations
{
    /// <inheritdoc />
    public partial class GPili3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrinterName",
                table: "PosTerminalInfo",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Vat",
                table: "PosTerminalInfo",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrinterName",
                table: "PosTerminalInfo");

            migrationBuilder.DropColumn(
                name: "Vat",
                table: "PosTerminalInfo");
        }
    }
}
