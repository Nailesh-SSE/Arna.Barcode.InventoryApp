using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class Add_BoxQuantity_To_InwardItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "InwardItems",
                newName: "ItemQuantity");

            migrationBuilder.AddColumn<decimal>(
                name: "BoxQuantity",
                table: "InwardItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoxQuantity",
                table: "InwardItems");

            migrationBuilder.RenameColumn(
                name: "ItemQuantity",
                table: "InwardItems",
                newName: "Quantity");
        }
    }
}
