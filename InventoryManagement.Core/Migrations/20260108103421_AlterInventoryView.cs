using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class AlterInventoryView : Migration
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
                                  WHEN od.IsDeleted = 1 THEN 0 
                                  WHEN it.BoxQuantity > 0 THEN (it.ItemQuantity * od.Quantity) 
                                  ELSE od.Quantity 
                              END
                          ) AS IssuedQty
                      FROM dbo.OutwardDetails AS od
                      INNER JOIN dbo.InwardBarcodeItems AS ibt ON od.BarcodeNo = ibt.BarcodeNo
                      INNER JOIN dbo.InwardItems AS it ON it.Id = ibt.InwardItemId
                      GROUP BY it.ProductId
                  ),
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql
                (@"  
                
               ALTER VIEW dbo.vw_InventoryReport
                AS
                WITH InwardCTE AS
                (
                    SELECT 
                        it.ProductId,
                        SUM(
                            CASE 
                                WHEN it.IsDeleted = 1 THEN 0
                                WHEN it.BoxQuantity > 0 THEN (it.ItemQuantity * it.BoxQuantity)
                                ELSE it.ItemQuantity
                            END
                        ) AS TotalInwardQty
                    FROM dbo.InwardItems it
                    GROUP BY it.ProductId
                ),
                IssuedCTE AS
                (
                    SELECT 
                        it.ProductId,
                        SUM(
                            CASE 
                                WHEN od.IsDeleted = 1 THEN 0
                                WHEN it.BoxQuantity > 0 THEN (it.ItemQuantity * od.Quantity)
                                ELSE od.Quantity
                            END
                        ) AS IssuedQty
                    FROM dbo.OutwardDetails od
                    INNER JOIN dbo.InwardBarcodeItems ibt ON od.BarcodeNo = ibt.BarcodeNo
                    INNER JOIN dbo.InwardItems it ON it.Id = ibt.InwardItemId
                    GROUP BY it.ProductId
                ),
                ReturnCTE AS
                (
                    SELECT 
                        sri.ProductId,
                        SUM(
                            CASE 
                                WHEN sri.IsDeleted = 1 THEN 0
                                WHEN sri.IsTakeInStock = 1 THEN sri.ReturnQuantity
                                ELSE 0
                            END
                        ) AS ReturnQty
                    FROM dbo.SaleReturnItems sri
                    GROUP BY sri.ProductId
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
                    ISNULL(r.ReturnQty, 0) AS Returns,
                    ISNULL(s.IssuedQty, 0) - ISNULL(r.ReturnQty, 0) AS TotalSale,
                    ISNULL(i.TotalInwardQty, 0) 
                        - (ISNULL(s.IssuedQty, 0) - ISNULL(r.ReturnQty, 0)) AS GoodStock,
                    p.CategoryId,
                    p.MakeCompanyId,
                    p.ColourId
                FROM dbo.Products p
                LEFT JOIN InwardCTE i ON i.ProductId = p.Id
                LEFT JOIN IssuedCTE s ON s.ProductId = p.Id
                LEFT JOIN ReturnCTE r ON r.ProductId = p.Id;
               ");
        }
    }
}
