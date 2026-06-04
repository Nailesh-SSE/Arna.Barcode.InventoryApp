using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class updateBarcodeTackingReportToShowNewbarcode : Migration
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

        CASE 
            WHEN ibi.ParentId = 0 AND it.InwardUnitId = 2
                THEN NULL
            ELSE ibi.BarcodeNo
        END AS Item_Barcode,

        CASE 
            WHEN ibi.ParentId = 0 AND it.InwardUnitId = 2
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

    -- OUTWARD (BOX TO ITEM) 
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

    -- RETURN
    SELECT 
        'RETURN' AS TransactionType,
        sr.SaleReturnNo AS TransactionNo,

        CASE 
            WHEN sri.UnitId = 2 THEN
                CASE 
                    WHEN ibt.ParentId <> 0 THEN ibt.BarcodeNo
                    ELSE NULL
                END
            ELSE sri.BarCodeNo
        END AS Item_Barcode,

        CASE 
            WHEN sri.UnitId = 2
                THEN sri.BarCodeNo
            ELSE box.BarcodeNo
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
    FROM InwardBarcodeItems ibt
    LEFT JOIN InwardBarcodeItems box
        ON ibt.ParentId = box.Id
    INNER JOIN SaleReturnItems sri
        ON 
        (
            sri.BarCodeNo = ibt.BarcodeNo

            OR

            (
                ibt.ParentId <> 0 
                AND sri.BarCodeNo = box.BarcodeNo
                AND NOT EXISTS (
                    SELECT 1 
                    FROM SaleReturnItems sri2
                    WHERE sri2.BarCodeNo = ibt.BarcodeNo
                )
            )
        )
    INNER JOIN SaleReturns sr
        ON sri.SaleReturnId = sr.Id
    LEFT JOIN Users u
        ON u.Id = CASE 
                      WHEN sri.UpdatedBy = 0 THEN sri.CreatedBy
                      ELSE sri.UpdatedBy
                  END
    WHERE ibt.BarcodeNo = @BarcodeNo 
      AND ibt.IsInStock = 0

    UNION ALL

    -- NEW INWARD BARCODE CREATED FROM OLD BARCODE
    SELECT 
        'NEW INWARD' AS TransactionType,
        newInward.InwardNo AS TransactionNo,

        CASE 
            WHEN newBarcode.ParentId = 0 AND newItem.InwardUnitId = 2
                THEN NULL
            ELSE newBarcode.BarcodeNo
        END AS Item_Barcode,

        CASE 
            WHEN newBarcode.ParentId = 0 AND newItem.InwardUnitId = 2
                THEN newBarcode.BarcodeNo
            ELSE newParent.BarcodeNo
        END AS Box_Barcode,

        newBarcode.CreatedOn AS TransactionTime,
        CASE 
            WHEN newBarcode.UpdatedBy = 0 THEN newBarcode.CreatedBy
            ELSE newBarcode.UpdatedBy
        END AS UserId,
        u.UserName,
        CASE 
            WHEN newBarcode.IsDeleted = 1 THEN 'Deleted' 
            ELSE '' 
        END AS Status,
        newBarcode.UpdatedOn
    FROM InwardBarcodeItems AS oldBarcode
    INNER JOIN InwardBarcodeItems AS newBarcode
        ON newBarcode.OldBarcodeId = oldBarcode.Id
    LEFT JOIN InwardBarcodeItems AS newParent
        ON newBarcode.ParentId = newParent.Id
    INNER JOIN Inwards AS newInward
        ON newBarcode.InwardId = newInward.Id
    INNER JOIN InwardItems AS newItem
        ON newBarcode.InwardItemId = newItem.Id
    LEFT JOIN Users AS u
        ON u.Id = CASE 
                      WHEN newBarcode.UpdatedBy = 0 THEN newBarcode.CreatedBy
                      ELSE newBarcode.UpdatedBy
                  END
    WHERE oldBarcode.BarcodeNo = @BarcodeNo

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
    }
}
