using InventoryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.TimeZoneInfo;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class AlterSpBarcodeTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            ALTER PROCEDURE [dbo].[sp_BarcodeTracking]
                @BarcodeNo NVARCHAR(50)
            AS
            BEGIN
                SET NOCOUNT ON;
            
                SELECT 
                    'INWARD' AS TransactionType,
                    i.InwardNo AS TransactionNo,
                    ibi.BarcodeNo,
                    ibi.CreatedOn AS TransactionTime,
                    CASE 
                        WHEN ibi.UpdatedBy = 0 THEN ibi.CreatedBy
                        ELSE ibi.UpdatedBy
                    END AS UserId,
                    u.UserName,
                    CASE 
                        WHEN ibi.IsDeleted = 1 THEN 'Deleted' 
                        ELSE '' 
                    END AS Status,
                    ibi.UpdatedOn
                FROM InwardBarcodeItems AS ibi
                INNER JOIN Inwards i 
                    ON ibi.InwardId = i.Id
                LEFT JOIN Users u
                    ON u.Id = CASE 
                                  WHEN ibi.UpdatedBy = 0 THEN ibi.CreatedBy
                                  ELSE ibi.UpdatedBy
                              END
                WHERE ibi.BarcodeNo = @BarcodeNo
            
                UNION ALL
            
                SELECT 
                    'OUTWARD' AS TransactionType,
                    o.OutwardNo AS TransactionNo,
                    od.BarcodeNo,
                    od.CreatedOn AS TransactionTime,
                    CASE 
                        WHEN od.UpdatedBy = 0 THEN od.CreatedBy
                        ELSE od.UpdatedBy
                    END AS UserId,
                    u.UserName,
                    CASE 
                        WHEN od.IsDeleted = 1 THEN 'Deleted' 
                        ELSE '' 
                    END AS Status,
                    od.UpdatedOn
                FROM OutwardDetails AS od
                INNER JOIN Outwards o 
                    ON od.OutwardId = o.Id
                LEFT JOIN Users u
                    ON u.Id = CASE 
                                  WHEN od.UpdatedBy = 0 THEN od.CreatedBy
                                  ELSE od.UpdatedBy
                              END
                WHERE od.BarcodeNo = @BarcodeNo
            
                UNION ALL
            
                SELECT 
                    'RETURN' AS TransactionType,
                    sr.SaleReturnNo AS TransactionNo,
                    sri.BarCodeNo,
                    sri.CreatedOn AS TransactionTime,
                    CASE 
                        WHEN sri.UpdatedBy = 0 THEN sri.CreatedBy
                        ELSE sri.UpdatedBy
                    END AS UserId,
                    u.UserName,
                    CASE 
                        WHEN sri.IsDeleted = 1 THEN 'Deleted' 
                        ELSE '' 
                    END AS Status,
                    sri.UpdatedOn
                FROM SaleReturnItems AS sri
                INNER JOIN SaleReturns sr 
                    ON sri.SaleReturnId = sr.Id
                LEFT JOIN Users u
                    ON u.Id = CASE 
                                  WHEN sri.UpdatedBy = 0 THEN sri.CreatedBy
                                  ELSE sri.UpdatedBy
                              END
                WHERE sri.BarCodeNo = @BarcodeNo
            
                ORDER BY TransactionTime;
            END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"   
                 ALTER PROCEDURE [dbo].[sp_BarcodeTracking]
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
            END");
        }
    }
}
