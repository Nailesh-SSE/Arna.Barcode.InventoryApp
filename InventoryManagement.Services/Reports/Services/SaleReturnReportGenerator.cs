using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models.ReportModels;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Diagnostics;
using System.Linq.Expressions;

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

                var (items, totalRecords) = await ExecuteQuery(query, filter);

                // Generate summary
                // var summary = await GenerateSummaryAsync(filter);

                stopwatch.Stop();

                return new SaleReturnReportResult
                {
                    Items = items,
                    TotalRecords = totalRecords,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    GenerationTime = stopwatch.Elapsed,
                    PageCount = (int)Math.Ceiling((double)totalRecords / filter.PageSize),
                    HasNextPage = filter.PageNumber * filter.PageSize < totalRecords,
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
                "No.","Return Company","Ship To","PlatForm","SaleReturn Number", "SaleReturn Date" , "SaleReturn Time" , "Category" ,"Brand","Product", "SKU",
                 "Barcode","Unit","Box Qty","Item Qty","Type","ToStock","Reason","Status"
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

                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(item.BillToCompanyName);
                row.CreateCell(2).SetCellValue(item.shipToCompanyName);
                row.CreateCell(3).SetCellValue(item.PlatformName);
                row.CreateCell(4).SetCellValue(item.ReturnNo);
                row.CreateCell(5).SetCellValue(item.ReturnDate.ToString("dd/MM/yyyy"));
                row.CreateCell(6).SetCellValue(item.ReturnDate.ToString("HH:mm:ss"));
                row.CreateCell(7).SetCellValue(item.CategoryName);
                row.CreateCell(8).SetCellValue(item.BrandName);
                row.CreateCell(9).SetCellValue(item.ProductName);
                row.CreateCell(10).SetCellValue(item.SKU);
                row.CreateCell(11).SetCellValue(item.Barcode);
                row.CreateCell(12).SetCellValue(item.Unit);
                row.CreateCell(13).SetCellValue((double)item.BoxQuantity);
                row.CreateCell(14).SetCellValue((double)item.Quantity);
                row.CreateCell(15).SetCellValue(item.Type);
                row.CreateCell(16).SetCellValue(item.ToStock ? "Yes" : "No");
                row.CreateCell(17).SetCellValue(item.Reason);
                //row.CreateCell(8).SetCellValue(item.BatchNumber);
                row.CreateCell(18).SetCellValue(item.IsActive ? "Active" : "Inactive");
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
            try 
            {
                var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>();
                
                var query = saleReturnRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(sr => !sr.IsDeleted);

                if (!string.IsNullOrWhiteSpace(filter.ReturnNumbers))
                {
                    var returnNos = filter.ReturnNumbers
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                    if (returnNos.Count > 0)
                    {
                        // Build an OR expression instead of relying on EF translation to OPENJSON
                        var param = Expression.Parameter(typeof(SaleReturn), "sr");
                        var prop =Expression.Property(param, nameof(SaleReturn.SaleReturnNo));

                        Expression? body = null;
                        foreach (var rn in returnNos)
                        {
                            var constant = Expression.Constant(rn);
                            var equals = Expression.Equal(prop, constant);
                            body = body == null ? equals : Expression.OrElse(body, equals);
                        }

                        var predicate = body != null
                            ? Expression.Lambda<Func<SaleReturn, bool>>(body, param)
                            : (Expression<Func<SaleReturn, bool>>)(sr => false);

                        query = query.Where(predicate);

                        return query;
                    }
                }
                // ---------------- Other filters (applied ONLY when ReturnNumbers is empty)

                if (filter.StartDate.HasValue)
                    query = query.Where(i => i.SaleReturnDate.Date >= filter.StartDate.Value.Date);

                if (filter.EndDate.HasValue)
                    query = query.Where(i => i.SaleReturnDate.Date <= filter.EndDate.Value.Date);

                if (filter.BillToCompanyId.HasValue)
                    query = query.Where(sr =>
                        sr.BillToCompanyId == filter.BillToCompanyId || sr.BillToCompanyId == null);

                if (!filter.IncludeInactive)
                    query = query.Where(sr => sr.IsActive);

                return query;
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating sale return report", ex);
            }
        }

        private async Task<(List<SaleReturnReportItem> Items, int TotalRecords)> ExecuteQuery(
        IQueryable<SaleReturn> baseQuery,
        SaleReturnFilter filter)
        {
            try 
            {
                // Build SQL query (joins only, no navigation)

                var productRepo = _unitOfWork.GetRepository<Product>();
                var categoryRepo = _unitOfWork.GetRepository<Category>();
                var companyRepo = _unitOfWork.GetRepository<Company>();
                var outwardRepo = _unitOfWork.GetRepository<Outward>();
                var outwardDetailRepo = _unitOfWork.GetRepository<OutwardDetail>();
                var platformRepo = _unitOfWork.GetRepository<Platform>();

                var query =
                    from sr in baseQuery
                    from item in sr.SaleReturnItems.Where(i => !i.IsDeleted)

                    join p in productRepo.GetQueryable()
                        on item.ProductId equals p.Id

                    join c in categoryRepo.GetQueryable()
                        on p.CategoryId equals c.Id into cat
                    from c in cat.DefaultIfEmpty()
                    
                    join od in outwardDetailRepo.GetQueryable().Where(y => !y.IsDeleted)
                        on item.BarCodeNo equals od.BarcodeNo into odJoin
                    from od in odJoin.DefaultIfEmpty()

                    join o in outwardRepo.GetQueryable()
                        on item.OutwardId equals o.Id into oJoin
                    from o in oJoin.DefaultIfEmpty()

                    join plat in platformRepo.GetQueryable()
                        on o.PlatformId equals plat.Id into platJoin
                    from plat in platJoin.DefaultIfEmpty()

                        // ⭐ BillToCompany join using effective id (item > parent)
                    join billcomp in companyRepo.GetQueryable()
                        on (item.BillToCompanyId != 0
                            ? item.BillToCompanyId
                            : sr.BillToCompanyId) equals billcomp.Id into billcompJoin
                    from billcomp in billcompJoin.DefaultIfEmpty()
                   
                    // ⭐ ShipToCompany join
                    join shipComp in companyRepo.GetQueryable()
                           on item.ShipToCompanyId equals shipComp.Id into shipJoin
                    from shipComp in shipJoin.DefaultIfEmpty()

                    select new
                    {
                        sr,
                        item,
                        p,
                        c,
                        billcomp,
                        shipComp,
                        o,
                        plat
                    };
                // Filters
                if (string.IsNullOrWhiteSpace(filter.ReturnNumbers))
                {
                    if (filter.CategoryId.HasValue)
                        query = query.Where(x => x.p.CategoryId == filter.CategoryId);

                    if (filter.BillToCompanyId.HasValue)
                    {
                        int filterId = filter.BillToCompanyId.Value;

                        query = query.Where(x =>
                            (x.item.BillToCompanyId != 0
                                ? x.item.BillToCompanyId
                                : x.sr.BillToCompanyId) == filterId);
                    }
                }

                var totalRecords = await query.CountAsync();
                // ---------------- Sorting----------------

                bool desc = filter.SortDescending;
                query = filter.SortBy?.ToLower() switch
                {
                    // DATE
                    "returndate" =>
                        desc
                            ? query.OrderByDescending(x => x.sr.SaleReturnDate)
                            : query.OrderBy(x => x.sr.SaleReturnDate),

                    // RETURN NO
                    "returnno" =>
                        desc
                            ? query.OrderByDescending(x => x.sr.SaleReturnNo)
                            : query.OrderBy(x => x.sr.SaleReturnNo),

                    // SUPPLIER
                    "shipmentcompany" =>
                        desc
                            ? query.OrderByDescending(x => x.billcomp.Name)
                                   .ThenByDescending(x => x.sr.SaleReturnDate)
                            : query.OrderBy(x => x.billcomp.Name)
                                   .ThenBy(x => x.sr.SaleReturnDate),

                    // DEFAULT
                    _ =>
                        desc
                            ? query.OrderByDescending(x => x.sr.SaleReturnDate)
                            : query.OrderBy(x => x.sr.SaleReturnDate)
                };

                // Paging (ALWAYS order first)
                query = query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize);

                // Fetch from DB

                var raw = await query.ToListAsync();

                // Map to DTO (AFTER DB for safety/performance)

                var items = raw.Select(x => new SaleReturnReportItem
                {
                    BillToCompanyId = x.billcomp?.Id ?? 0,
                    BillToCompanyName = x.billcomp?.Name ?? "",
                    
                    PlatformId = x.plat?.Id ?? 0,
                    PlatformName = x.plat?.Name ?? "",

                    ShipToCompanyId = x.shipComp.Id,
                    shipToCompanyName = x.shipComp?.Name ?? "",

                    SaleReturnId = x.sr.Id,
                    ReturnNo = x.sr.SaleReturnNo,
                    ReturnDate = x.item.CreatedOn,

                    CategoryName = x.c?.Name ?? "",
                    BrandId = x.p.MakeCompanyId ,
                    BrandName= x.p.MakeCompany,

                    ProductId = x.p.Id,
                    ProductName = x.p.Name,
                    SKU = x.p.SKU,

                    Barcode = x.item.BarCodeNo ?? "",
                    Type = ((ReturnType)x.item.ReturnType).ToString(),
                    ToStock = x.item.IsTakeInStock,
                    Reason = x.item.ReasonToReturn ?? "",
                    Unit = ((UnitType)x.item.UnitId).ToString(),
                    BoxQuantity = x.item.UnitId == (int)UnitType.BOX ? 1:0,
                    Quantity = x.item.ReturnQuantity,

                    IsActive = x.sr.IsActive,
                    CreatedBy = x.sr.CreatedBy,
                    CreatedOn = x.sr.CreatedOn
                })
                .ToList();

                return (items, totalRecords);
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating sale return report", ex);
            }
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
