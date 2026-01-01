using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSPBarcodeTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID('dbo.sp_BarcodeTracking', 'P') IS NOT NULL
                BEGIN
                    DROP PROCEDURE dbo.sp_BarcodeTracking;
                END
            ");

                    migrationBuilder.Sql(@"
                CREATE PROCEDURE dbo.sp_BarcodeTracking
                    @BarcodeNo NVARCHAR(50)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        'INWARD' AS TransactionType,
                        i.InwardNo AS TransactionNo,
                        ibi.BarcodeNo,
                        ibi.CreatedOn AS TransactionTime
                    FROM InwardBarcodeItems AS ibi
                    INNER JOIN Inwards i 
                        ON ibi.InwardId = i.Id
                    WHERE 
                        i.IsDeleted = 0
                        AND ibi.IsDeleted = 0
                        AND ibi.BarcodeNo = @BarcodeNo

                    UNION ALL

                    SELECT 
                        'OUTWARD' AS TransactionType,
                        o.OutwardNo AS TransactionNo,
                        od.BarcodeNo,
                        od.CreatedOn AS TransactionTime
                    FROM OutwardDetails AS od
                    INNER JOIN Outwards o 
                        ON od.OutwardId = o.Id
                    WHERE 
                        od.IsDeleted = 0
                        AND od.BarcodeNo = @BarcodeNo

                    UNION ALL

                    SELECT 
                        'RETURN' AS TransactionType,
                        sr.SaleReturnNo AS TransactionNo,
                        sri.BarCodeNo,
                        sri.CreatedOn AS TransactionTime
                    FROM SaleReturnItems AS sri
                    INNER JOIN SaleReturns sr 
                        ON sri.SaleReturnId = sr.Id
                    WHERE 
                        sri.IsDeleted = 0
                        AND sri.BarCodeNo = @BarcodeNo

                    ORDER BY TransactionTime;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID('dbo.sp_BarcodeTracking', 'P') IS NOT NULL
                BEGIN
                    DROP PROCEDURE dbo.sp_BarcodeTracking;
                END
            ");
        }
    }
}
