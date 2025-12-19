using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models.ReportModels;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace InventoryManagement.Services.Reports.Services
{
    public class SaleReturnReportGenerator : ISaleReturnReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SaleReturnReportGenerator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SaleReturnReportResult> GenerateSaleReturnReportAsync(SaleReturnFilter filter)
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

                return new SaleReturnReportResult
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
                throw new Exception("Error generating sale return report", ex);
            }
        }

        public async Task<byte[]> ExportToExcelAsync(SaleReturnFilter filter)
        {
            // Remove paging for export
            filter.PageSize = int.MaxValue;
            filter.PageNumber = 1;

            var report = await GenerateSaleReturnReportAsync(filter);

            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("SaleReturn Report");

            // Header row
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = new string[]
            {
                "SaleReturn Number", "Date", "Product", "SKU", "Category",
                "Quantity", "Unit", "Supplier", "Status"
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

                row.CreateCell(0).SetCellValue(item.ReturnNo);
                row.CreateCell(1).SetCellValue(item.ReturnDate.ToString("dd/MM/yyyy"));
                row.CreateCell(2).SetCellValue(item.ProductName);
                row.CreateCell(3).SetCellValue(item.SKU);
                row.CreateCell(4).SetCellValue(item.CategoryName);
                row.CreateCell(5).SetCellValue((double)item.Quantity);
                row.CreateCell(6).SetCellValue(item.Unit);
                row.CreateCell(7).SetCellValue(item.BillToCompanyName);
                //row.CreateCell(8).SetCellValue(item.BatchNumber);
                row.CreateCell(8).SetCellValue(item.IsActive ? "Active" : "Inactive");
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

        // Similar to Outward, build the query, apply filters, and execute
        private IQueryable<SaleReturn> BuildBaseQuery(SaleReturnFilter filter)
        {
            var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>();
            var query = saleReturnRepo.GetQueryable()
                .Include(o => o.SaleReturnItems)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Category)
                .Where(sr => !sr.IsDeleted);

            // Apply filters
            if (filter.StartDate.HasValue)
                query = query.Where(i => i.SaleReturnDate.Date >= filter.StartDate.Value.Date);

            if (filter.EndDate.HasValue)
                query = query.Where(i => i.SaleReturnDate.Date <= filter.EndDate.Value.Date);

            if (filter.BillToCompanyId.HasValue)
                query = query.Where(sr => sr.BillToCompanyId == filter.BillToCompanyId);

            if (!filter.IncludeInactive)
                query = query.Where(sr => sr.IsActive);

            // Apply sorting
            query = ApplySorting(query, filter);

            return query;
        }

        private async Task<List<SaleReturnReportItem>> ExecuteQuery(IQueryable<SaleReturn> query, SaleReturnFilter filter)
        {
            List<string>? returnNos = null;
            var saleReturns = await query.ToListAsync();
            var items = new List<SaleReturnReportItem>();

            if (!string.IsNullOrWhiteSpace(filter.ReturnNumbers))
            {
                returnNos = filter.ReturnNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            }

            foreach (var saleReturn in saleReturns)
            {
                foreach(var item in saleReturn.SaleReturnItems)
                {
                    if (returnNos != null && returnNos.Any() && !returnNos.Contains(saleReturn.SaleReturnNo)) continue;
                    if (filter.CategoryId.HasValue && item.Product?.CategoryId != filter.CategoryId) continue;

                    items.Add(new SaleReturnReportItem
                    {
                        SaleReturnId = saleReturn.Id,
                        ReturnNo = saleReturn.SaleReturnNo,
                        ReturnDate = saleReturn.SaleReturnDate,
                        Unit = "PCS",
                        ProductName = item.Product?.Name ?? "Unknown",
                        SKU = item.Product?.SKU ?? "",
                        CategoryName = item.Product?.Category?.Name ?? "",
                        Quantity = item.ReturnQuantity,
                        BillToCompanyId = saleReturn.BillToCompanyId,
                        BillToCompanyName = saleReturn.BillToCompany?.Name ?? "N/A",
                        IsActive = saleReturn.IsActive,
                        CreatedBy = saleReturn.CreatedBy,
                        CreatedOn = saleReturn.CreatedOn
                    });
                }
            }

            return items;
        }

        private IQueryable<SaleReturn> ApplySorting(IQueryable<SaleReturn> query, SaleReturnFilter filter)
        {
            return filter.SortBy?.ToLower() switch
            {
                "returndate" => filter.SortDescending ? query.OrderByDescending(sr => sr.SaleReturnDate) : query.OrderBy(sr => sr.SaleReturnDate),
                "returnno" => filter.SortDescending ? query.OrderByDescending(sr => sr.SaleReturnNo) : query.OrderBy(sr => sr.SaleReturnNo),
                "shipmentcompany" => filter.SortDescending ? query.OrderByDescending(sr => sr.BillToCompany.Name) : query.OrderBy(sr => sr.BillToCompany.Name),
                _ => query.OrderByDescending(sr => sr.SaleReturnDate)
            };
        }

        private async Task<SaleReturnReportSummary> GenerateSummaryAsync(SaleReturnFilter filter)
        {
            var query = BuildBaseQuery(filter);
            var saleReturns = await query.ToListAsync();

            var items = new List<SaleReturnReportItem>();
            foreach (var saleReturn in saleReturns)
            {
                foreach (var item in saleReturn.SaleReturnItems)
                {
                    items.Add(new SaleReturnReportItem
                    {
                        Unit = "PCS",
                        BillToCompanyId = saleReturn.BillToCompanyId,
                        BillToCompanyName = saleReturn.BillToCompany?.Name ?? "",
                        Quantity = item.ReturnQuantity
                    });
                }
            }

            return new SaleReturnReportSummary
            {
                TotalReturns = saleReturns.Count,
                TotalQuantity = items.Sum(i => i.Quantity),
                TotalValue = items.Sum(i => i.TotalCost),
                UniqueProductsCount = items.Select(i => i.ProductName).Distinct().Count(),
                UniqueSuppliersCount = items.Select(i => i.BillToCompanyId).Distinct().Count(),
                AverageQuantityPerReturn = saleReturns.Any() ? items.Sum(i => i.Quantity) / saleReturns.Count : 0,
                AverageValuePerReturn = saleReturns.Any() ? items.Sum(i => i.TotalCost) / saleReturns.Count : 0,
                CountByProduct = items.GroupBy(i => i.ProductName).ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }
}
