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
            migrationBuilder.AddColumn<string>(
                name: "CardId",
                table: "User",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardId",
                table: "User");
        }
    }
}
