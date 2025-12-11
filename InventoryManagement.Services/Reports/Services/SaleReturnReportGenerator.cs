//using InventoryManagement.Core.Entities;
//using InventoryManagement.Infrastructure.Repositories;
//using InventoryManagement.Services.Interfaces;
//using InventoryManagement.Services.Models.ReportModels;
//using Microsoft.EntityFrameworkCore;

//namespace InventoryManagement.Services.Reports.Services
//{
//    public class SaleReturnReportGenerator : ISaleReturnReportService
//    {
//        private readonly IUnitOfWork _unitOfWork;

//        public SaleReturnReportGenerator(IUnitOfWork unitOfWork)
//        {
//            _unitOfWork = unitOfWork;
//        }

//        public async Task<SaleReturnReportResult> GenerateSaleReturnReportAsync(SaleReturnFilter filter)
//        {
//            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

//            try
//            {
//                var query = BuildBaseQuery(filter);

//                var items = await ExecuteQuery(query, filter);

//                // Apply paging
//                var pagedItems = items.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToList();

//                // Generate summary
//                var summary = await GenerateSummaryAsync(filter);

//                stopwatch.Stop();

//                return new SaleReturnReportResult
//                {
//                    Items = pagedItems,
//                    Summary = summary,
//                    TotalRecords = items.Count,
//                    PageNumber = filter.PageNumber,
//                    PageSize = filter.PageSize,
//                    GenerationTime = stopwatch.Elapsed,
//                    PageCount = (int)Math.Ceiling((double)items.Count / filter.PageSize),
//                    HasNextPage = filter.PageNumber * filter.PageSize < items.Count,
//                    HasPreviousPage = filter.PageNumber > 1
//                };
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("Error generating sale return report", ex);
//            }
//        }

//        // Similar to Outward, build the query, apply filters, and execute
//        private IQueryable<SaleReturn> BuildBaseQuery(SaleReturnFilter filter)
//        {
//            var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>();
//            var query = saleReturnRepo.GetQueryable()
//                .Where(sr => !sr.IsDeleted);

//            // Apply filters
//            if (!string.IsNullOrEmpty(filter.ReturnNumbers))
//                query = query.Where(sr => filter.ReturnNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(sr.ReturnNo));

//            if (filter.BillToCompanyId.HasValue)
//                query = query.Where(sr => sr.BillToCompanyId == filter.BillToCompanyId);

//            if (filter.MinQuantity.HasValue)
//                query = query.Where(sr => sr.Quantity >= filter.MinQuantity);

//            if (filter.MaxQuantity.HasValue)
//                query = query.Where(sr => sr.Quantity <= filter.MaxQuantity);

//            if (!filter.IncludeInactive)
//                query = query.Where(sr => sr.IsActive);

//            // Apply sorting
//            query = ApplySorting(query, filter);

//            return query;
//        }

//        private async Task<List<SaleReturnReportItem>> ExecuteQuery(IQueryable<SaleReturn> query, SaleReturnFilter filter)
//        {
//            var saleReturns = await query.ToListAsync();
//            var items = new List<SaleReturnReportItem>();

//            foreach (var saleReturn in saleReturns)
//            {
//                items.Add(new SaleReturnReportItem
//                {
//                    SaleReturnId = saleReturn.Id,
//                    ReturnNo = saleReturn.ReturnNo,
//                    ReturnDate = saleReturn.ReturnDate,
//                    ProductName = saleReturn.Product?.Name ?? "Unknown",
//                    SKU = saleReturn.Product?.SKU ?? "",
//                    CategoryName = saleReturn.Product?.Category?.Name ?? "",
//                    Quantity = saleReturn.Quantity,
//                    Unit = "PCS",
//                    BillToCompanyId = saleReturn.BillToCompanyId,
//                    BillToCompanyName = saleReturn.BillToCompany?.Name ?? "N/A",
//                    IsActive = saleReturn.IsActive,
//                    CreatedBy = saleReturn.CreatedBy,
//                    CreatedOn = saleReturn.CreatedOn
//                });
//            }

//            return items;
//        }

//        private IQueryable<SaleReturn> ApplySorting(IQueryable<SaleReturn> query, SaleReturnFilter filter)
//        {
//            return filter.SortBy?.ToLower() switch
//            {
//                "returndate" => filter.SortDescending ? query.OrderByDescending(sr => sr.ReturnDate) : query.OrderBy(sr => sr.ReturnDate),
//                "returnno" => filter.SortDescending ? query.OrderByDescending(sr => sr.ReturnNo) : query.OrderBy(sr => sr.ReturnNo),
//                "company" => filter.SortDescending ? query.OrderByDescending(sr => sr.BillToCompany.Name) : query.OrderBy(sr => sr.BillToCompany.Name),
//                _ => query.OrderByDescending(sr => sr.ReturnDate)
//            };
//        }

//        private async Task<SaleReturnReportSummary> GenerateSummaryAsync(SaleReturnFilter filter)
//        {
//            var query = BuildBaseQuery(filter);
//            var saleReturns = await query.ToListAsync();

//            var items = new List<SaleReturnReportItem>();
//            foreach (var saleReturn in saleReturns)
//            {
//                items.Add(new SaleReturnReportItem
//                {
//                    Quantity = saleReturn.Quantity,
//                    Unit = "PCS",
//                    BillToCompanyId = saleReturn.BillToCompanyId,
//                    BillToCompanyName = saleReturn.BillToCompany?.Name ?? "",
//                    ProductName = saleReturn.Product?.Name ?? "",
//                    CategoryName = saleReturn.Product?.Category?.Name ?? ""
//                });
//            }

//            return new SaleReturnReportSummary
//            {
//                TotalReturns = saleReturns.Count,
//                TotalQuantity = items.Sum(i => i.Quantity),
//                TotalValue = items.Sum(i => i.TotalCost),
//                UniqueProductsCount = items.Select(i => i.ProductName).Distinct().Count(),
//                UniqueSuppliersCount = items.Select(i => i.BillToCompanyId).Distinct().Count(),
//                AverageQuantityPerReturn = saleReturns.Any() ? items.Sum(i => i.Quantity) / saleReturns.Count : 0,
//                AverageValuePerReturn = saleReturns.Any() ? items.Sum(i => i.TotalCost) / saleReturns.Count : 0,
//                CountByProduct = items.GroupBy(i => i.ProductName).ToDictionary(g => g.Key, g => g.Count())
//            };
//        }
//    }
//}

