using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class updateSaleReturnFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BarcodeNo",
                table: "SaleReturns",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_OutwardDetails_ProductId",
                table: "OutwardDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Inwards_CategoryId",
                table: "Inwards",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inwards_Categories_CategoryId",
                table: "Inwards",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OutwardDetails_Products_ProductId",
                table: "OutwardDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inwards_Categories_CategoryId",
                table: "Inwards");

            migrationBuilder.DropForeignKey(
                name: "FK_OutwardDetails_Products_ProductId",
                table: "OutwardDetails");

            migrationBuilder.DropIndex(
                name: "IX_OutwardDetails_ProductId",
                table: "OutwardDetails");

            migrationBuilder.DropIndex(
                name: "IX_Inwards_CategoryId",
                table: "Inwards");

            migrationBuilder.AlterColumn<string>(
                name: "BarcodeNo",
                table: "SaleReturns",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
