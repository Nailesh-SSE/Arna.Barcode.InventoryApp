using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class updateBarcodeTrackingReport : Migration
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

    -- INWARD 
    SELECT 
        'INWARD' AS TransactionType,
        i.InwardNo AS TransactionNo,

        -- ITEM BARCODE (NULL when scanned barcode is BOX)
        CASE 
            WHEN ibi.ParentId = 0 AND it.InwardUnitId = 2  -- BOX
                THEN NULL
            ELSE ibi.BarcodeNo
        END AS Item_Barcode,

        -- BOX BARCODE (NULL when loose PCS)
        CASE 
            WHEN ibi.ParentId = 0 AND it.InwardUnitId = 2  -- BOX
                THEN ibi.BarcodeNo
            ELSE parent.BarcodeNo
        END AS Box_Barcode,

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
    LEFT JOIN InwardBarcodeItems AS parent
        ON ibi.ParentId = parent.Id
    INNER JOIN Inwards AS i
        ON ibi.InwardId = i.Id
    INNER JOIN InwardItems AS it
        ON ibi.InwardItemId = it.Id
    LEFT JOIN Users AS u
        ON u.Id = CASE 
                      WHEN ibi.UpdatedBy = 0 THEN ibi.CreatedBy
                      ELSE ibi.UpdatedBy
                  END
    WHERE ibi.BarcodeNo = @BarcodeNo

    UNION ALL

    -- OUTWARD (DIRECT ITEM) 
    SELECT 
        'OUTWARD' AS TransactionType,
        o.OutwardNo AS TransactionNo,

        CASE 
            WHEN od.Unit = 'BOX'
                THEN NULL
            ELSE od.BarcodeNo
        END AS Item_Barcode,

        CASE 
            WHEN od.Unit = 'BOX'
                THEN od.BarcodeNo
            ELSE NULL
        END AS Box_Barcode,

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
    INNER JOIN Outwards AS o
        ON od.OutwardId = o.Id
    LEFT JOIN Users AS u
        ON u.Id = CASE 
                      WHEN od.UpdatedBy = 0 THEN od.CreatedBy
                      ELSE od.UpdatedBy
                  END
    WHERE od.BarcodeNo = @BarcodeNo

    UNION ALL

    -- OUTWARD (BOX → ITEM) 
    SELECT
        'OUTWARD' AS TransactionType,
        o.OutwardNo AS TransactionNo,
        ibt.BarcodeNo AS Item_Barcode,
        parent.BarcodeNo AS Box_Barcode,
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
    FROM InwardBarcodeItems AS ibt
    INNER JOIN InwardBarcodeItems AS parent
        ON ibt.ParentId = parent.Id
    INNER JOIN OutwardDetails AS od
        ON parent.BarcodeNo = od.BarcodeNo
    INNER JOIN Outwards AS o
        ON od.OutwardId = o.Id
    LEFT JOIN Users AS u
        ON u.Id = CASE 
                      WHEN od.UpdatedBy = 0 THEN od.CreatedBy
                      ELSE od.UpdatedBy
                  END
    WHERE ibt.BarcodeNo = @BarcodeNo
      AND ibt.IsInStock = 0
      AND parent.IsInStock = 0
      AND NOT EXISTS (
            SELECT 1
            FROM OutwardDetails od2
            WHERE od2.BarcodeNo = ibt.BarcodeNo
              AND od2.IsDeleted = 0
      )

    UNION ALL

    --  RETURN
    SELECT 
        'RETURN' AS TransactionType,
        sr.SaleReturnNo AS TransactionNo,

        CASE 
            WHEN sri.UnitId = 2  -- BOX
                THEN NULL
            ELSE sri.BarCodeNo  -- PCS
        END AS Item_Barcode,

        CASE 
            WHEN sri.UnitId = 2  -- BOX
                THEN sri.BarCodeNo
            ELSE parent.BarcodeNo
        END AS Box_Barcode,

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
    INNER JOIN SaleReturns AS sr
        ON sri.SaleReturnId = sr.Id
    LEFT JOIN InwardBarcodeItems AS ibt
        ON sri.BarCodeNo = ibt.BarcodeNo
    LEFT JOIN InwardBarcodeItems AS parent
        ON ibt.ParentId = parent.Id
    LEFT JOIN Users AS u
        ON u.Id = CASE 
                      WHEN sri.UpdatedBy = 0 THEN sri.CreatedBy
                      ELSE sri.UpdatedBy
                  END
    WHERE sri.BarCodeNo = @BarcodeNo

    ORDER BY TransactionTime;
END;
            ");
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
    }
}
