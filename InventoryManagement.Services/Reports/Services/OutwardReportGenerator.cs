using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models.ReportModels;
using Microsoft.EntityFrameworkCore;

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
            if (!string.IsNullOrEmpty(filter.OutwardNumbers))
                query = query.Where(o => filter.OutwardNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(o.OutwardNo));

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
            var outwards = await query.ToListAsync();
            var items = new List<OutwardReportItem>();

            foreach (var outward in outwards)
            {
                foreach (var item in outward.OutwardDetails)
                {
                    items.Add(new OutwardReportItem
                    {
                        OutwardId = outward.Id,
                        OutwardNumber = outward.OutwardNo,
                        OutwardDate = outward.OutwardDate,
                        ProductName = item.Product?.Name ?? "Unknown",
                        SKU = item.Product?.SKU ?? "",
                        CategoryName = item.Product?.Category?.Name ?? "",
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
                "company" => filter.SortDescending ? query.OrderByDescending(o => o.BillToCompany.Name) : query.OrderBy(o => o.BillToCompany.Name),
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
