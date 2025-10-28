using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class pudateOutwardDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutwardDetails_InwardBarcodeItems_InwardBarcodeItemId",
                table: "OutwardDetails");

            migrationBuilder.DropIndex(
                name: "IX_OutwardDetails_InwardBarcodeItemId",
                table: "OutwardDetails");

            migrationBuilder.RenameColumn(
                name: "InwardBarcodeItemId",
                table: "OutwardDetails",
                newName: "ProductId");

            migrationBuilder.AddColumn<int>(
                name: "InwardItemId",
                table: "OutwardDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "OutwardDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OutwardDetails_InwardItemId",
                table: "OutwardDetails",
                column: "InwardItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_OutwardDetails_InwardBarcodeItems_InwardItemId",
                table: "OutwardDetails",
                column: "InwardItemId",
                principalTable: "InwardBarcodeItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutwardDetails_InwardBarcodeItems_InwardItemId",
                table: "OutwardDetails");

            migrationBuilder.DropIndex(
                name: "IX_OutwardDetails_InwardItemId",
                table: "OutwardDetails");

            migrationBuilder.DropColumn(
                name: "InwardItemId",
                table: "OutwardDetails");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "OutwardDetails");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "OutwardDetails",
                newName: "InwardBarcodeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OutwardDetails_InwardBarcodeItemId",
                table: "OutwardDetails",
                column: "InwardBarcodeItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_OutwardDetails_InwardBarcodeItems_InwardBarcodeItemId",
                table: "OutwardDetails",
                column: "InwardBarcodeItemId",
                principalTable: "InwardBarcodeItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
