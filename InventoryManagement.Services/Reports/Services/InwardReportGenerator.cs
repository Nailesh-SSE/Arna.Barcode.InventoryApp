using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models.ReportModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace InventoryManagement.Services.Reports.Services;
public class InwardReportGenerator : IInwardReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InwardReportGenerator> _logger;

    public InwardReportGenerator(IUnitOfWork unitOfWork, ILogger<InwardReportGenerator> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<InwardReportResult> GenerateInwardReportAsync(InwardFilter filter)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var query = BuildBaseQuery(filter);

            var items = await ExecuteQuery(query, filter);

            // Apply paging
            var pagedItems = items.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToList();

            // Generate summary
            var summary = await GenerateSummaryAsync(filter);

            stopwatch.Stop();

            return new InwardReportResult
            {
                Items = pagedItems,
                Summary = summary,
                TotalRecords = items.Count,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                GenerationTime = stopwatch.Elapsed,
                PageCount = (int)Math.Ceiling((double)items.Count / filter.PageSize),
                HasNextPage = filter.PageNumber * filter.PageSize < items.Count,
                HasPreviousPage = filter.PageNumber > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inward report");
            throw;
        }
    }

    public async Task<InwardReportResult> GetInwardsWithPagingAsync(InwardFilter filter)
    {
        return await GenerateInwardReportAsync(filter);
    }

    public async Task<InwardReportSummary> GetInwardSummaryAsync(InwardFilter filter)
    {
        return await GenerateSummaryAsync(filter);
    }

    public async Task<byte[]> ExportToExcelAsync(InwardFilter filter)
    {
        // Remove paging for export
        filter.PageSize = int.MaxValue;
        filter.PageNumber = 1;

        var report = await GenerateInwardReportAsync(filter);

        IWorkbook workbook = new XSSFWorkbook();
        ISheet sheet = workbook.CreateSheet("Inward Report");

        // Header row
        IRow headerRow = sheet.CreateRow(0);
        string[] headers = new string[]
        {
        "Inward Number", "Date", "Product", "SKU", "Category",
        "Quantity", "Unit", "Supplier",
        "Batch", "Status"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            headerRow.CreateCell(i).SetCellValue(headers[i]);
        }

        // Data rows
        for (int i = 0; i < report.Items.Count; i++)
        {
            var item = report.Items[i];
            IRow row = sheet.CreateRow(i + 1);

            row.CreateCell(0).SetCellValue(item.InwardNumber);
            row.CreateCell(1).SetCellValue(item.InwardDate.ToString("dd/MM/yyyy"));
            row.CreateCell(2).SetCellValue(item.ProductName);
            row.CreateCell(3).SetCellValue(item.SKU);
            row.CreateCell(4).SetCellValue(item.CategoryName);
            row.CreateCell(5).SetCellValue((double)item.Quantity);
            row.CreateCell(6).SetCellValue(item.Unit);
            row.CreateCell(7).SetCellValue(item.ShipmentCompanyName);
            row.CreateCell(8).SetCellValue(item.BatchNumber);
            row.CreateCell(9).SetCellValue(item.IsActive ? "Active" : "Inactive");
        }

        // Autosize all columns
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

    public async Task<byte[]> ExportToPdfAsync(InwardFilter filter)
    {
        filter.PageSize = int.MaxValue;
        filter.PageNumber = 1;

        var report = await GenerateInwardReportAsync(filter);

        using (var memoryStream = new MemoryStream())
        {
            var pdf = new PdfSharp.Pdf.PdfDocument();
            var page = pdf.AddPage();
            var graphics = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
            var font = new PdfSharp.Drawing.XFont("Arial", 10);

            double y = 50;

            // Title
            graphics.DrawString("Inward Report", new PdfSharp.Drawing.XFont("Arial", 16),
                PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(50, y));
            y += 30;

            // Headers
            var headers = new[] { "Inward#", "Date", "Product", "SKU", "Qty", "Unit", "Cost", "Total", "Supplier" };
            double[] widths = { 60, 60, 150, 60, 40, 30, 40, 50, 80 };
            double x = 50;

            for (int i = 0; i < headers.Length; i++)
            {
                graphics.DrawString(headers[i], new PdfSharp.Drawing.XFont("Arial", 8),
                    PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(x, y));
                x += widths[i];
            }
            y += 15;

            // Data
            foreach (var item in report.Items.Take(100)) // Limit for PDF
            {
                x = 50;
                var values = new[] {
                    item.InwardNumber,
                    item.InwardDate.ToString("dd/MM/yyyy"),
                    item.ProductName,
                    item.SKU,
                    item.Quantity.ToString(),
                    item.Unit,
                    item.UnitCost.ToString("F2"),
                    item.TotalCost.ToString("F2"),
                    item.ShipmentCompanyName
                };

                for (int i = 0; i < values.Length; i++)
                {
                    graphics.DrawString(values[i], font, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(x, y));
                    x += widths[i];
                }
                y += 12;

                if (y > page.Height - 50)
                {
                    page = pdf.AddPage();
                    graphics = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
                    y = 50;
                }
            }

            pdf.Close();
            return memoryStream.ToArray();
        }
    }

    public async Task<List<InwardReportItem>> GetTopProductsAsync(int count, DateTime? startDate = null, DateTime? endDate = null)
    {
        var filter = new InwardFilter
        {
            StartDate = startDate,
            EndDate = endDate,
            GroupByProduct = true,
            PageSize = count
        };

        var query = BuildBaseQuery(filter);
        var items = await ExecuteQuery(query, filter);

        return items.GroupBy(i => i.ProductId)
                    .Select(g => g.First())
                    .Take(count)
                    .ToList();
    }

    public async Task<List<InwardReportItem>> GetRecentInwardsAsync(int count)
    {
        var filter = new InwardFilter
        {
            PageSize = count,
            SortBy = "InwardDate",
            SortDescending = true
        };

        var query = BuildBaseQuery(filter);
        return await ExecuteQuery(query, filter);
    }

    private IQueryable<Inward> BuildBaseQuery(InwardFilter filter)
    {
        var inwardRepo = _unitOfWork.GetRepository<Inward>();
        var query = inwardRepo.GetQueryable()
            .Include(i => i.InwardItems)
                .ThenInclude(ii => ii.Product)
                    .ThenInclude(p => p.Category)
            .Include(i => i.ShipMentCompany)
            .Where(i => !i.IsDeleted);

        // Apply filters
        if (filter.StartDate.HasValue)
            query = query.Where(i => i.InwardDate.Date >= filter.StartDate.Value.Date);

        if (filter.EndDate.HasValue)
            query = query.Where(i => i.InwardDate.Date <= filter.EndDate.Value.Date);

        if (filter.CategoryId.HasValue)
            query = query.Where(i => i.CategoryId == filter.CategoryId);

        if (filter.ShipmentCompanyId.HasValue)
            query = query.Where(i => i.ShipMentCompanyId == filter.ShipmentCompanyId);

        if (!filter.IncludeInactive)
            query = query.Where(i => i.IsActive);

        // Apply sorting
        query = ApplySorting(query, filter);

        return query;
    }

    private IQueryable<Inward> ApplySorting(IQueryable<Inward> query, InwardFilter filter)
    {
        return filter.SortBy?.ToLower() switch
        {
            "inwarddate" => filter.SortDescending ? query.OrderByDescending(i => i.InwardDate) : query.OrderBy(i => i.InwardDate),
            "inwardno" => filter.SortDescending ? query.OrderByDescending(i => i.InwardNo) : query.OrderBy(i => i.InwardNo),
            "shipmentcompany" => filter.SortDescending ? query.OrderByDescending(i => i.ShipMentCompany.Name) : query.OrderBy(i => i.ShipMentCompany.Name),
            "createdon" => filter.SortDescending ? query.OrderByDescending(i => i.CreatedOn) : query.OrderBy(i => i.CreatedOn),
            _ => query.OrderByDescending(i => i.InwardDate)
        };
    }

    private async Task<List<InwardReportItem>> ExecuteQuery(IQueryable<Inward> query, InwardFilter filter)
    {
        var inwards = await query.ToListAsync();
        var items = new List<InwardReportItem>();

        foreach (var inward in inwards)
        {
            foreach (var item in inward.InwardItems.Where(ii => !ii.IsDeleted))
            {
                // Apply additional item-level filters
                if (filter.MinQuantity.HasValue && item.Quantity < filter.MinQuantity.Value) continue;
                if (filter.MaxQuantity.HasValue && item.Quantity > filter.MaxQuantity.Value) continue;
                if (!string.IsNullOrEmpty(filter.InwardNumbers) &&
                    !filter.InwardNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(item.Inward?.InwardNo)) continue;
                if (!string.IsNullOrEmpty(filter.ProductSKUs) &&
                    !filter.ProductSKUs.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(item.Product?.SKU)) continue;
                if (!string.IsNullOrEmpty(filter.BatchNumbers) &&
                    !filter.BatchNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(item.BatchNo)) continue;

                var unitCost = item.Quantity > 0 ? 0m : 0m; // You may need to get this from product cost
                var totalCost = unitCost * item.Quantity;

                items.Add(new InwardReportItem
                {
                    InwardId = inward.Id,
                    InwardNumber = inward.InwardNo,
                    InwardDate = inward.InwardDate,
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? "Unknown",
                    SKU = item.Product?.SKU ?? "",
                    CategoryName = item.Product?.Category?.Name ?? "",
                    Quantity = item.Quantity,
                    Unit = item.Unit ?? "",
                    UnitCost = unitCost,
                    TotalCost = totalCost,
                    BatchNumber = item.BatchNo,
                    SerialNumber = item.SerialNo,
                    ShipmentCompanyId = inward.ShipMentCompanyId,
                    ShipmentCompanyName = inward.ShipMentCompany?.Name ?? "",
                    Remarks = inward.Remarks,
                    IsActive = inward.IsActive,
                    CreatedBy = inward.CreatedBy,
                    CreatedOn = inward.CreatedOn
                });
            }
        }

        return items;
    }

    private async Task<InwardReportSummary> GenerateSummaryAsync(InwardFilter filter)
    {
        var query = BuildBaseQuery(filter);
        var inwards = await query.ToListAsync();

        var items = new List<InwardReportItem>();
        foreach (var inward in inwards)
        {
            foreach (var item in inward.InwardItems.Where(ii => !ii.IsDeleted))
            {
                items.Add(new InwardReportItem
                {
                    Quantity = item.Quantity,
                    Unit = item.Unit ?? "",
                    ShipmentCompanyId = inward.ShipMentCompanyId,
                    ShipmentCompanyName = inward.ShipMentCompany?.Name ?? "",
                    ProductName = item.Product?.Name ?? "",
                    CategoryName = item.Product?.Category?.Name ?? ""
                });
            }
        }

        return new InwardReportSummary
        {
            TotalInwards = inwards.Count,
            TotalQuantity = items.Sum(i => i.Quantity),
            TotalValue = items.Sum(i => i.TotalCost),
            UniqueProductsCount = items.Select(i => i.ProductName).Distinct().Count(),
            UniqueSuppliersCount = items.Select(i => i.ShipmentCompanyId).Distinct().Count(),
            AverageQuantityPerInward = inwards.Any() ? items.Sum(i => i.Quantity) / inwards.Count : 0,
            AverageValuePerInward = inwards.Any() ? items.Sum(i => i.TotalCost) / inwards.Count : 0,
            CountByProduct = items.GroupBy(i => i.ProductName).ToDictionary(g => g.Key, g => g.Count())
        };
    }
}