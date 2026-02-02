using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models.ReportModels;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace InventoryManagement.Services.Reports.Services
{
    public class OutwardReportGenerator : IOutwardReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OutwardReportGenerator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<OutwardReportResult> GenerateOutwardReportAsync(OutwardFilter filter)
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

                return new OutwardReportResult
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
                throw new Exception("Error generating outward report", ex);
            }
        }

        public async Task<byte[]> ExportToExcelAsync(OutwardFilter filter)
        {
            // Remove paging for export
            filter.PageSize = int.MaxValue;
            filter.PageNumber = 1;

            var report = await GenerateOutwardReportAsync(filter);

            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Outward Report");

            // Header row
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = new string[]
            {
                "Outward Number", "Date", "Brand" ,"Product", "SKU", "Category",
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

                row.CreateCell(0).SetCellValue(item.OutwardNumber);
                row.CreateCell(1).SetCellValue(item.OutwardDate.ToString("dd/MM/yyyy"));
                row.CreateCell(2).SetCellValue(item.BrandName);
                row.CreateCell(3).SetCellValue(item.ProductName);
                row.CreateCell(4).SetCellValue(item.SKU);
                row.CreateCell(5).SetCellValue(item.CategoryName);
                row.CreateCell(6).SetCellValue((double)item.Quantity);
                row.CreateCell(7).SetCellValue(item.Unit);
                row.CreateCell(8).SetCellValue(item.BillToCompanyName);
                //row.CreateCell(8).SetCellValue(item.BatchNumber);
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

        // Similar to Inward, build the query, apply filters, and execute
        private IQueryable<Outward> BuildBaseQuery(OutwardFilter filter)
        {
            var outwardRepo = _unitOfWork.GetRepository<Outward>();
            var query = outwardRepo.GetQueryable()
                .Include(o => o.OutwardDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Category)
                .Where(o => !o.IsDeleted);

            // Apply filters
            if (filter.StartDate.HasValue)
                query = query.Where(i => i.OutwardDate.Date >= filter.StartDate.Value.Date);

            if (filter.EndDate.HasValue)
                query = query.Where(i => i.OutwardDate.Date <= filter.EndDate.Value.Date);

            if (filter.BillToCompanyId.HasValue)
                query = query.Where(o => o.BillToCompanyId == filter.BillToCompanyId);

            if (filter.MinQuantity.HasValue)
                query = query.Where(o => o.OutwardDetails.Sum(od => od.Quantity) >= filter.MinQuantity);

            if (filter.MaxQuantity.HasValue)
                query = query.Where(o => o.OutwardDetails.Sum(od => od.Quantity) <= filter.MaxQuantity);

            if (!filter.IncludeInactive)
                query = query.Where(o => o.IsActive);

            // Apply sorting
            query = ApplySorting(query, filter);

            return query;
        }

        private async Task<List<OutwardReportItem>> ExecuteQuery(IQueryable<Outward> query, OutwardFilter filter)
        {
            
            List<string>? outwardNos = null;
            var outwards = await query.ToListAsync();
            var items = new List<OutwardReportItem>();

            if (!string.IsNullOrWhiteSpace(filter.OutwardNumbers))
            {
                outwardNos = filter.OutwardNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            }

            foreach (var outward in outwards)
            {
                foreach (var item in outward.OutwardDetails)
                {
                    // Apply additional item-level filters
                    if (outwardNos != null && outwardNos.Any() && !outwardNos.Contains(outward.OutwardNo)) continue;
                    if (filter.CategoryId.HasValue && item.Product?.CategoryId != filter.CategoryId) continue;

                    items.Add(new OutwardReportItem
                    {
                        OutwardId = outward.Id,
                        OutwardNumber = outward.OutwardNo,
                        OutwardDate = outward.OutwardDate,
                        ProductName = item.Product?.Name ?? "Unknown",
                        SKU = item.Product?.SKU ?? "",
                        CategoryName = item.Product?.Category?.Name ?? "",
                        BrandName = item.Product?.MakeCompany ?? "",
                        Quantity = item.Quantity,
                        Unit = item.Unit ?? "",
                        BillToCompanyId = outward.BillToCompanyId,
                        BillToCompanyName = outward.BillToCompany?.Name ?? "N/A",
                        IsActive = outward.IsActive,
                        CreatedBy = outward.CreatedBy,
                        CreatedOn = outward.CreatedOn
                    });
                }
            }

            return items;
        }

        private IQueryable<Outward> ApplySorting(IQueryable<Outward> query, OutwardFilter filter)
        {
            return filter.SortBy?.ToLower() switch
            {
                "outwarddate" => filter.SortDescending ? query.OrderByDescending(o => o.OutwardDate) : query.OrderBy(o => o.OutwardDate),
                "outwardno" => filter.SortDescending ? query.OrderByDescending(o => o.OutwardNo) : query.OrderBy(o => o.OutwardNo),
                "shipmentcompany" => filter.SortDescending ? query.OrderByDescending(o => o.BillToCompany.Name) : query.OrderBy(o => o.BillToCompany.Name),
                _ => query.OrderByDescending(o => o.OutwardDate)
            };
        }

        private async Task<OutwardReportSummary> GenerateSummaryAsync(OutwardFilter filter)
        {
            var query = BuildBaseQuery(filter);
            var outwards = await query.ToListAsync();

            var items = new List<OutwardReportItem>();
            foreach (var outward in outwards)
            {
                foreach (var item in outward.OutwardDetails)
                {
                    items.Add(new OutwardReportItem
                    {
                        Quantity = item.Quantity,
                        Unit = item.Unit ?? "",
                        BillToCompanyId = outward.BillToCompanyId,
                        BillToCompanyName = outward.BillToCompany?.Name ?? "",
                        ProductName = item.Product?.Name ?? "",
                        CategoryName = item.Product?.Category?.Name ?? ""
                    });
                }
            }

            return new OutwardReportSummary
            {
                TotalOutwards = outwards.Count,
                TotalQuantity = items.Sum(i => i.Quantity),
                TotalValue = items.Sum(i => i.TotalCost),
                UniqueProductsCount = items.Select(i => i.ProductName).Distinct().Count(),
                UniqueSuppliersCount = items.Select(i => i.BillToCompanyId).Distinct().Count(),
                AverageQuantityPerOutward = outwards.Any() ? items.Sum(i => i.Quantity) / outwards.Count : 0,
                AverageValuePerOutward = outwards.Any() ? items.Sum(i => i.TotalCost) / outwards.Count : 0,
                CountByProduct = items.GroupBy(i => i.ProductName).ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }
}
