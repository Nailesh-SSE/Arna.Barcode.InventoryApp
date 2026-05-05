using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class Create_SP_GetInventoryReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            IF OBJECT_ID('dbo.sp_GetInventoryReport', 'P') IS NOT NULL
            DROP PROCEDURE dbo.sp_GetInventoryReport;

        ");
            migrationBuilder.Sql(@"CREATE PROCEDURE [dbo].[sp_GetInventoryReport]
            (
            	 @BrandId INT = NULL,
            	 @CategoryId INT = NULL,
            	 @ColourId INT = NULL,  
                 @IsActive BIT = NULL,
                 @IsDeleted BIT = NULL,
                 @MinGoodStock INT = NULL,
                 @MaxGoodStock INT = NULL
            
            )	
            As 
            Begin 
            SET NOCOUNT ON;
            select 
            		v.Sku ,
            		v.MakeCompany,
            		v.CategoryName,
            		v.Inward,
            		v.Outward,
            		v.Returns,
            		v.TotalSale,
            		v.GoodStock
            		FROM dbo.vw_InventoryReport AS v
             WHERE
                    (@ColourId IS NULL OR v.ColourId = @ColourId)
                    AND (@BrandId IS NULL OR v.MakeCompanyId = @BrandId)
                    AND (@CategoryId IS NULL OR v.CategoryId = @CategoryId)
                    AND (@IsActive IS NULL OR v.IsActive = @IsActive)
                    AND (@IsDeleted IS NULL OR v.IsDeleted = @IsDeleted)
                    AND (@MinGoodStock IS NULL OR v.GoodStock >= @MinGoodStock)
                    AND (@MaxGoodStock IS NULL OR v.GoodStock <= @MaxGoodStock)
            		 ORDER BY
                    v.MakeCompanyId,
                    v.Id;  
            End
            ");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
              DROP PROCEDURE IF EXISTS dbo.sp_GetInventoryReport;
          ");
        }
    }
}
