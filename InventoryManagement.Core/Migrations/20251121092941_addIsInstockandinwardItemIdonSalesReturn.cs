using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class addIsInstockandinwardItemIdonSalesReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTakeInStock",
                table: "SaleReturns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReturnInwardItemId",
                table: "SaleReturns",
                type: "int",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTakeInStock",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "ReturnInwardItemId",
                table: "SaleReturns");
        }
    }
}
