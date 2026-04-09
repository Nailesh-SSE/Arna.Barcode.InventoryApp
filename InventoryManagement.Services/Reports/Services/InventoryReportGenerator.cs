using InventoryManagement.Core.Data;
using InventoryManagement.Core.Entities.SP_Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Models.ReportModels;
using InventoryManagement.Services.Reports.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;
using System.Diagnostics;

namespace InventoryManagement.Services.Reports.Services;

public class InventoryReportGenerator : IInventoryReportService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly InventoryDbContext _db;

    public InventoryReportGenerator(IUnitOfWork unitOfWork, InventoryDbContext db)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }
    public async Task<InventoryReportResult> GenerateInventoryReportAsync(InventoryReportFilter filter)
    {
        var stopwatch = Stopwatch.StartNew();
        var items = await Call_sp_GetInventoryReport(filter);
        if (!string.IsNullOrWhiteSpace(filter.ProductName) &&
            !string.IsNullOrWhiteSpace(filter.BrandName))
        {
            items = items
                .Where(x =>
                    x.ProductName != null &&
                    x.MakeCompany != null &&
                    x.ProductName.Trim().ToLower() == filter.ProductName.Trim().ToLower() &&
                    x.MakeCompany.Trim().ToLower() == filter.BrandName.Trim().ToLower()
                )
                .ToList();
        }
        var pagedItems = items.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToList();

        var result = new InventoryReportResult
        {
            Items = pagedItems,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalRecords = items.Count,
            GenerationTime = stopwatch.Elapsed,
            PageCount = (int)Math.Ceiling((double)items.Count / filter.PageSize),
            HasNextPage = filter.PageNumber * filter.PageSize < items.Count,
            HasPreviousPage = filter.PageNumber > 1
        };

        return result;
    }
    public async Task<byte[]> ExportToExcelAsync(InventoryReportFilter filter)
    {
        // Remove paging for export
        // call stored procedures or queries to get inventory data based on the filter
        var report = await Call_sp_GetInventoryReport(filter);
        var today = DateTime.Now.ToString("dd-MM-yyyy");

        IWorkbook workbook = new XSSFWorkbook();
        ISheet sheet = workbook.CreateSheet(today + "Inventory Report");

        //header row
        IRow headerRow = sheet.CreateRow(0);
        string[] headers = new string[]
        {
           "No","Category","Brand","Product","Item Name","Inward", "Issued", "Return", "Total Sale", "Good Stock"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            headerRow.CreateCell(i).SetCellValue(headers[i]);
        }
        //data rows
        for (int i = 0; i < report.Count; i++)
        {
            var item = report[i];
            IRow row = sheet.CreateRow(i + 1);
            row.CreateCell(0).SetCellValue(i + 1);
            row.CreateCell(1).SetCellValue(item.CategoryName);
            row.CreateCell(2).SetCellValue(item.MakeCompany);
            row.CreateCell(3).SetCellValue(item.ProductName);
            row.CreateCell(4).SetCellValue(item.Sku);
            row.CreateCell(5).SetCellValue((double)item.Inward);
            row.CreateCell(6).SetCellValue((double)item.Outward);
            row.CreateCell(7).SetCellValue((double)item.Returns);
            row.CreateCell(8).SetCellValue((double)item.TotalSale);
            row.CreateCell(9).SetCellValue((double)item.GoodStock);

        }
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.AutoSizeColumn(i);
        }
        // Write to memory stream and return as byte array
        using (var exportData = new MemoryStream())
        {
            workbook.Write(exportData);
            return exportData.ToArray();
        }
    }

    private async Task<List<InventoryReportDTO>> Call_sp_GetInventoryReport(InventoryReportFilter filter)
    {
        // call stored procedures 
        var result = new List<InventoryReportDTO>();
        var BrandParam = new SqlParameter("@BrandId", SqlDbType.Int) { Value = (object?)filter.BrandId ?? DBNull.Value };
        var CategoryParam = new SqlParameter("@CategoryId", SqlDbType.Int) { Value = (object?)filter.CategoryId ?? DBNull.Value };
        var ColourParam = new SqlParameter("@ColourId", SqlDbType.Int) { Value = (object?)filter.ColourId ?? DBNull.Value };
        var IsActiveParam = new SqlParameter("@IsActive", SqlDbType.Bit) { Value = (object?)filter.IsActive ?? DBNull.Value };
        var IsDeletedParam = new SqlParameter("@IsDeleted", SqlDbType.Bit) { Value = (object?)filter.IsDeleted ?? DBNull.Value };
        var MinGoodStockParam = new SqlParameter("@MinGoodStock", SqlDbType.Decimal) { Value = (object?)filter.MInGoodMinGoodStock ?? DBNull.Value };
        var MaxGoodStockParam = new SqlParameter("@MaxGoodStock", SqlDbType.Decimal) { Value = (object?)filter.MaxGoodStock ?? DBNull.Value };
        try
        {
            result = await _db.InventoryReportDTO.FromSqlRaw(
             "EXEC sp_GetInventoryReport @BrandId, @CategoryId, @ColourId, @IsActive, @IsDeleted, @MinGoodStock, @MaxGoodStock",
             BrandParam,
             CategoryParam,
             ColourParam,
             IsActiveParam,
             IsDeletedParam,
             MinGoodStockParam,
             MaxGoodStockParam
         )
         .AsNoTracking()
         .ToListAsync();
            return result;
        }
        catch (Exception ex)
        {
            return result;
        }

    }
}
