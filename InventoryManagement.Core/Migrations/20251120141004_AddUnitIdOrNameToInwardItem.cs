using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitIdOrNameToInwardItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "InwardItems",
                newName: "InwardUnitName");

            migrationBuilder.AddColumn<int>(
                name: "InwardUnitId",
                table: "InwardItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_ProductId",
                table: "SaleReturns",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleReturns_Products_ProductId",
                table: "SaleReturns",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleReturns_Products_ProductId",
                table: "SaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturns_ProductId",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "InwardUnitId",
                table: "InwardItems");

            migrationBuilder.RenameColumn(
                name: "InwardUnitName",
                table: "InwardItems",
                newName: "Unit");
        }
    }
}
