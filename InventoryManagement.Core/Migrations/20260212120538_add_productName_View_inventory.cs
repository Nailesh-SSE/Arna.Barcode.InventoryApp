using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class add_productName_View_inventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            ALTER VIEW [dbo].[vw_InventoryReport]
                         AS
                         WITH InwardCTE AS (
                             SELECT 
                                 ProductId, 
                                 SUM(
                                     CASE 
                                         WHEN it.IsDeleted = 1 THEN 0 
                                         WHEN i.IsSalesReturn = 1 THEN 0  
                                         WHEN it.BoxQuantity > 0 THEN (it.ItemQuantity * it.BoxQuantity) 
                                         ELSE it.ItemQuantity 
                                     END
                                 ) AS TotalInwardQty
                             FROM dbo.InwardItems AS it
                             INNER JOIN dbo.Inwards AS i ON i.Id = it.InwardId
                             GROUP BY ProductId
                         ),
                         IssuedCTE AS (
             SELECT
               it.ProductId,
               SUM(
                   CASE
                       -- PCS sold separately AND belongs to a BOX
                       WHEN od.Unit = 'PCS'
                            AND ibt.IsDeleted = 0
                            AND ibt.ParentId IS NOT NULL
                            AND ibt.ParentId <> 0
                       THEN
                           CASE
                               -- Parent BOX already sold → do NOT count
                               WHEN parent.IsDeleted = 0
                                    AND parent.IsInStock = 0 THEN 0
                               -- Parent BOX not sold → count PCS
                               ELSE 1
                           END

                       -- PCS sold separately WITHOUT parent
                       WHEN od.Unit = 'PCS'
                            AND ibt.IsDeleted = 0
                            AND (ibt.ParentId IS NULL OR ibt.ParentId = 0)
                            AND ibt.IsInStock = 0 THEN 1

                       -- BOX sold
                       WHEN od.Unit = 'BOX'
                            AND od.IsDeleted = 0
                            AND ibt.IsDeleted = 0
                       THEN (it.ItemQuantity * od.Quantity)

                       ELSE 0
                   END
               ) AS IssuedQty
           FROM dbo.OutwardDetails od
           INNER JOIN dbo.InwardBarcodeItems ibt
               ON od.BarcodeNo = ibt.BarcodeNo
           INNER JOIN dbo.InwardItems it
               ON it.Id = ibt.InwardItemId
           LEFT JOIN dbo.InwardBarcodeItems parent
               ON parent.Id = ibt.ParentId
           WHERE od.IsDeleted = 0
           GROUP BY it.ProductId
)       ,
                         ReturnCTE AS (
                             SELECT 
                                 ProductId, 
                                 SUM(
                                     CASE 
                                         WHEN sri.IsDeleted = 1 THEN 0 
                                         WHEN sri.IsTakeInStock = 1 THEN sri.ReturnQuantity 
                                         ELSE 0 
                                     END
                                 ) AS ReturnQty
                             FROM dbo.SaleReturnItems AS sri
                             GROUP BY ProductId
                         )
                         SELECT 
                             p.Id, 
							 p.Name as ProductName,
                             p.SKU, 
                             p.MakeCompany, 
                             p.ColourName, 
                             p.CategoryName, 
                             p.IsActive, 
                             p.IsDeleted, 
                             ISNULL(i.TotalInwardQty, 0) AS Inward, 
                             ISNULL(s.IssuedQty, 0) AS Outward, 
                             ISNULL(r.ReturnQty, 0) AS [Returns], 
                             ISNULL(s.IssuedQty, 0) - ISNULL(r.ReturnQty, 0) AS TotalSale, 
                             ISNULL(i.TotalInwardQty, 0) - (ISNULL(s.IssuedQty, 0) - ISNULL(r.ReturnQty, 0)) AS GoodStock, 
                             p.CategoryId, 
                             p.MakeCompanyId, 
                             p.ColourId
                         FROM dbo.Products AS p
                         LEFT JOIN InwardCTE AS i ON i.ProductId = p.Id
                         LEFT JOIN IssuedCTE AS s ON s.ProductId = p.Id
                         LEFT JOIN ReturnCTE AS r ON r.ProductId = p.Id;
             ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"

              ALTER VIEW [dbo].[vw_InventoryReport]
                         AS
                         WITH InwardCTE AS (
                             SELECT 
                                 ProductId, 
                                 SUM(
                                     CASE 
                                         WHEN it.IsDeleted = 1 THEN 0 
                                         WHEN i.IsSalesReturn = 1 THEN 0  
                                         WHEN it.BoxQuantity > 0 THEN (it.ItemQuantity * it.BoxQuantity) 
                                         ELSE it.ItemQuantity 
                                     END
                                 ) AS TotalInwardQty
                             FROM dbo.InwardItems AS it
                             INNER JOIN dbo.Inwards AS i ON i.Id = it.InwardId
                             GROUP BY ProductId
                         ),
                         IssuedCTE AS (
             SELECT
               it.ProductId,
               SUM(
                   CASE
                       -- PCS sold separately AND belongs to a BOX
                       WHEN od.Unit = 'PCS'
                            AND ibt.IsDeleted = 0
                            AND ibt.ParentId IS NOT NULL
                            AND ibt.ParentId <> 0
                       THEN
                           CASE
                               -- Parent BOX already sold → do NOT count
                               WHEN parent.IsDeleted = 0
                                    AND parent.IsInStock = 0 THEN 0
                               -- Parent BOX not sold → count PCS
                               ELSE 1
                           END

                       -- PCS sold separately WITHOUT parent
                       WHEN od.Unit = 'PCS'
                            AND ibt.IsDeleted = 0
                            AND (ibt.ParentId IS NULL OR ibt.ParentId = 0)
                            AND ibt.IsInStock = 0 THEN 1

                       -- BOX sold
                       WHEN od.Unit = 'BOX'
                            AND od.IsDeleted = 0
                            AND ibt.IsDeleted = 0
                       THEN (it.ItemQuantity * od.Quantity)

                       ELSE 0
                   END
               ) AS IssuedQty
           FROM dbo.OutwardDetails od
           INNER JOIN dbo.InwardBarcodeItems ibt
               ON od.BarcodeNo = ibt.BarcodeNo
           INNER JOIN dbo.InwardItems it
               ON it.Id = ibt.InwardItemId
           LEFT JOIN dbo.InwardBarcodeItems parent
               ON parent.Id = ibt.ParentId
           WHERE od.IsDeleted = 0
           GROUP BY it.ProductId
)       ,
                         ReturnCTE AS (
                             SELECT 
                                 ProductId, 
                                 SUM(
                                     CASE 
                                         WHEN sri.IsDeleted = 1 THEN 0 
                                         WHEN sri.IsTakeInStock = 1 THEN sri.ReturnQuantity 
                                         ELSE 0 
                                     END
                                 ) AS ReturnQty
                             FROM dbo.SaleReturnItems AS sri
                             GROUP BY ProductId
                         )
                         SELECT 
                             p.Id, 
                             p.SKU, 
                             p.MakeCompany, 
                             p.ColourName, 
                             p.CategoryName, 
                             p.IsActive, 
                             p.IsDeleted, 
                             ISNULL(i.TotalInwardQty, 0) AS Inward, 
                             ISNULL(s.IssuedQty, 0) AS Outward, 
                             ISNULL(r.ReturnQty, 0) AS [Returns], 
                             ISNULL(s.IssuedQty, 0) - ISNULL(r.ReturnQty, 0) AS TotalSale, 
                             ISNULL(i.TotalInwardQty, 0) - (ISNULL(s.IssuedQty, 0) - ISNULL(r.ReturnQty, 0)) AS GoodStock, 
                             p.CategoryId, 
                             p.MakeCompanyId, 
                             p.ColourId
                         FROM dbo.Products AS p
                         LEFT JOIN InwardCTE AS i ON i.ProductId = p.Id
                         LEFT JOIN IssuedCTE AS s ON s.ProductId = p.Id
                         LEFT JOIN ReturnCTE AS r ON r.ProductId = p.Id;
             ");
        }
    }
}
