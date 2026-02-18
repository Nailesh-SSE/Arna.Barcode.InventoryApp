using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class addplatformInSaleReturnItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "platformId",
                table: "SaleReturnItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnItems_platformId",
                table: "SaleReturnItems",
                column: "platformId");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleReturnItems_Platform_platformId",
                table: "SaleReturnItems",
                column: "platformId",
                principalTable: "Platform",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleReturnItems_Platform_platformId",
                table: "SaleReturnItems");

            migrationBuilder.DropIndex(
                name: "IX_SaleReturnItems_platformId",
                table: "SaleReturnItems");

            migrationBuilder.DropColumn(
                name: "platformId",
                table: "SaleReturnItems");
        }
    }
}
