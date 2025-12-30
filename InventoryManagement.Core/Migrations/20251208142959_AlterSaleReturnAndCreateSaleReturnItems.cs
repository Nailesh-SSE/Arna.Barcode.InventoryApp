using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class AlterSaleReturnAndCreateSaleReturnItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_SaleReturns_ProductId'
      AND object_id = OBJECT_ID('SaleReturns')
)
BEGIN
    DROP INDEX [IX_SaleReturns_ProductId] ON [SaleReturns];
END
");


            migrationBuilder.Sql(@"
IF COL_LENGTH('SaleReturns','BarcodeNo') IS NOT NULL
    ALTER TABLE SaleReturns DROP COLUMN BarcodeNo;

IF COL_LENGTH('SaleReturns','BillingDate') IS NOT NULL
    ALTER TABLE SaleReturns DROP COLUMN BillingDate;

IF COL_LENGTH('SaleReturns','BoxQuantity') IS NOT NULL
    ALTER TABLE SaleReturns DROP COLUMN BoxQuantity;

IF COL_LENGTH('SaleReturns','IsTakeInStock') IS NOT NULL
    ALTER TABLE SaleReturns DROP COLUMN IsTakeInStock;

IF COL_LENGTH('SaleReturns','ProductId') IS NOT NULL
    ALTER TABLE SaleReturns DROP COLUMN ProductId;

IF COL_LENGTH('SaleReturns','Quantity') IS NOT NULL
    ALTER TABLE SaleReturns DROP COLUMN Quantity;

IF COL_LENGTH('SaleReturns','Reason') IS NOT NULL
    ALTER TABLE SaleReturns DROP COLUMN Reason;

IF COL_LENGTH('SaleReturns','Remarks') IS NOT NULL
    ALTER TABLE SaleReturns DROP COLUMN Remarks;

IF COL_LENGTH('SaleReturns','ReturnInwardItemId') IS NOT NULL
    ALTER TABLE SaleReturns DROP COLUMN ReturnInwardItemId;

IF COL_LENGTH('SaleReturns','ReturnNo') IS NOT NULL
    ALTER TABLE SaleReturns DROP COLUMN ReturnNo;

IF COL_LENGTH('SaleReturns','ReturnType') IS NOT NULL
    ALTER TABLE SaleReturns DROP COLUMN ReturnType;

IF COL_LENGTH('SaleReturns','UnitId') IS NOT NULL
    ALTER TABLE SaleReturns DROP COLUMN UnitId;
");

            migrationBuilder.RenameColumn(
                name: "ReturnDate",
                table: "SaleReturns",
                newName: "SaleReturnDate");

            migrationBuilder.AddColumn<string>(
                name: "SaleReturnNo",
                table: "SaleReturns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsSalesReturn",
                table: "Inwards",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SaleReturnItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleReturnId = table.Column<int>(type: "int", nullable: false),
                    OutwardId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SerialNo = table.Column<int>(type: "int", nullable: false),
                    BarCodeNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    ReturnQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ShipToCompanyId = table.Column<int>(type: "int", nullable: false),
                    ReturnType = table.Column<int>(type: "int", nullable: false),
                    IsTakeInStock = table.Column<bool>(type: "bit", nullable: false),
                    ReasonToReturn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleReturnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleReturnItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaleReturnItems_Companies_ShipToCompanyId",
                        column: x => x.ShipToCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaleReturnItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaleReturnItems_SaleReturns_SaleReturnId",
                        column: x => x.SaleReturnId,
                        principalTable: "SaleReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnItems_CategoryId",
                table: "SaleReturnItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnItems_ProductId",
                table: "SaleReturnItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnItems_SaleReturnId",
                table: "SaleReturnItems",
                column: "SaleReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnItems_ShipToCompanyId",
                table: "SaleReturnItems",
                column: "ShipToCompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaleReturnItems");

            migrationBuilder.DropColumn(
                name: "SaleReturnNo",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "IsSalesReturn",
                table: "Inwards");

            migrationBuilder.RenameColumn(
                name: "SaleReturnDate",
                table: "SaleReturns",
                newName: "ReturnDate");

            migrationBuilder.AddColumn<string>(
                name: "BarcodeNo",
                table: "SaleReturns",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingDate",
                table: "SaleReturns",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "BoxQuantity",
                table: "SaleReturns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsTakeInStock",
                table: "SaleReturns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "SaleReturns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "SaleReturns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "SaleReturns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "SaleReturns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReturnInwardItemId",
                table: "SaleReturns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnNo",
                table: "SaleReturns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReturnType",
                table: "SaleReturns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "SaleReturns",
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
    }
}
