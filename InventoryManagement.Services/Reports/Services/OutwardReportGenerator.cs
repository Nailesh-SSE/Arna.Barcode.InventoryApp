using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models.ReportModels;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Linq.Expressions;

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

                var (items, totalRecords) = await ExecuteQuery(query, filter);

                stopwatch.Stop();

                return new OutwardReportResult
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
              "No.","Bill To Company", "Platform","Outward Number", "Outward Date","Outward Time",
              "Category", "Brand" ,"Product","SKU","Barcode ",
              "Unit","Box Quantity","Item Quantity","Remark","Status","UserName","Action"
            };

            int[] columnWidths = new int[]
            {
                 6,  //No.
                 40, // Bill To Company
                 14, // Platform
                 16, // Outward Number
                 14, // Outward Date
                 12, // Outward Time
                 24, // Category
                 18, // Brand
                 20, // Product
                 40, // SKU (largest)
                 22, // Barcode (16–20 chars)
                 8,  // Unit (3 chars)
                 12, // Box Quantity
                 12, // Item Quantity
                 25, // Remark (free text)
                 8,  // Status
                 15, // UserName
                 8,  // Action
            };

            for (int i = 0; i < headers.Length; i++)
            {
                headerRow.CreateCell(i).SetCellValue(headers[i]);
                sheet.SetColumnWidth(i, columnWidths[i] * 256);
            }

            // Data rows
            for (int i = 0; i < report.Items.Count; i++)
            {
                var item = report.Items[i];
                IRow row = sheet.CreateRow(i + 1);

                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(item.BillToCompanyName);                  
                row.CreateCell(2).SetCellValue(item.PlatformName);                       
                row.CreateCell(3).SetCellValue(item.OutwardNumber);                      
                row.CreateCell(4).SetCellValue(item.OutwardDate.ToString("dd/MM/yyyy")); 
                row.CreateCell(5).SetCellValue(item.CreatedOn.ToString("HH:mm:ss"));        
                row.CreateCell(6).SetCellValue(item.CategoryName);                       
                row.CreateCell(7).SetCellValue(item.BrandName);                          
                row.CreateCell(8).SetCellValue(item.ProductName);                        
                row.CreateCell(9).SetCellValue(item.SKU);                                
                row.CreateCell(10).SetCellValue(item.Barcode ??string.Empty);             
                row.CreateCell(11).SetCellValue(item.Unit);                              
                row.CreateCell(12).SetCellValue(item.BoxQuantity);                       
                row.CreateCell(13).SetCellValue(item.ItemQuantity);                      
                row.CreateCell(14).SetCellValue(item.Remarks);                           
                row.CreateCell(15).SetCellValue(item.IsActive ? "Active" : "Inactive");  
                row.CreateCell(16).SetCellValue(item.UserName);  
                row.CreateCell(17).SetCellValue(item.IsDeleted ? "Deleted" : "Saved");  
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
                .AsNoTracking()
                .Where(o => !o.IsDeleted);

            if (!string.IsNullOrWhiteSpace(filter.OutwardNumbers))
            {
                var outwardNos = filter.OutwardNumbers
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();
                if (outwardNos.Count > 0)
                {
                    var param = Expression.Parameter(typeof(Outward), "sr");
                    var prop = Expression.Property(param, nameof(Outward.OutwardNo));

                    Expression? body = null;
                    foreach (var on in outwardNos)
                    {
                        var constant = Expression.Constant(on);
                        var equals = Expression.Equal(prop, constant);
                        body = body == null ? equals : Expression.OrElse(body, equals);
                    }

                    var predicate = body != null
                        ? Expression.Lambda<Func<Outward, bool>>(body, param)
                        : (Expression<Func<Outward, bool>>)(sr => false);

                    query = query.Where(predicate);

                    return query;
                }
            }

            // Apply filters
            if (filter.StartDate.HasValue)
                query = query.Where(o => o.OutwardDate >= filter.StartDate.Value.Date);

            if (filter.EndDate.HasValue)
                query = query.Where(o => o.OutwardDate < filter.EndDate.Value.Date.AddDays(1));

            if (filter.BillToCompanyId.HasValue)
                query = query.Where(o => o.BillToCompanyId == filter.BillToCompanyId);

            if (filter.PlatformId.HasValue)
                query = query.Where(p => p.PlatformId == filter.PlatformId);

            return query;
        }

        private async Task<(List<OutwardReportItem> Items, int TotalRecords)> ExecuteQuery(IQueryable<Outward> basequery, OutwardFilter filter)
        {

            List<string>? outwardNos = null;

            var productRepo = _unitOfWork.GetRepository<Product>().GetQueryable().AsNoTracking();
            var categoryRepo = _unitOfWork.GetRepository<Category>().GetQueryable().AsNoTracking();
            var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>().GetQueryable().AsNoTracking();
            var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>().GetQueryable().AsNoTracking();
            var companyRepo = _unitOfWork.GetRepository<Company>().GetQueryable().AsNoTracking();
            var platfromRepo = _unitOfWork.GetRepository<Platform>().GetQueryable().AsNoTracking();
            var userRepo = _unitOfWork.GetRepository<Users>().GetQueryable().AsNoTracking();

            var query =
                from o in basequery
                from item in o.OutwardDetails

                join p in productRepo
                    on item.ProductId equals p.Id

                join plat in platfromRepo
                    on o.PlatformId equals plat.Id

                join c in categoryRepo
                    on p.CategoryId equals c.Id into cata
                    from c in cata.DefaultIfEmpty()
          
                join comp in companyRepo
                    on o.BillToCompanyId equals comp.Id into compJoin
                    from comp in compJoin.DefaultIfEmpty()

                join bc in barcodeRepo
                    on item.BarcodeNo equals bc.BarcodeNo into bcJoin
                    from bc in bcJoin.DefaultIfEmpty()

                join it in inwardItemRepo
                     on bc.InwardItemId equals it.Id into itJoin
                     from it in itJoin.DefaultIfEmpty()
                join user in userRepo
                     on item.CreatedBy equals user.Id into userJoin
                     from user in userJoin.DefaultIfEmpty()

                select new
                {
                    o,
                    item,
                    p,
                    c,
                    comp,
                    plat,
                    bc,
                    it,
                    user
                };
       
            if (filter.CategoryId.HasValue)
                query = query.Where(x => x.p.CategoryId == filter.CategoryId);
            if (filter.ShowDeleted == false)
            {
                query = query.Where(x => !x.item.IsDeleted);
            }
            // Filters
            if (string.IsNullOrWhiteSpace(filter.OutwardNumbers))
            {           
                if (filter.BillToCompanyId.HasValue)
                {
                    query = query.Where(x => x.o.BillToCompanyId == filter.BillToCompanyId);
                }

                if (filter.PlatformId.HasValue) 
                {
                    query = query.Where(x => x.o.PlatformId == filter.PlatformId);
                }
            }
            if (!string.IsNullOrWhiteSpace(filter.ProductName))
                query = query.Where(x => x.p.Name == filter.ProductName);

            if (filter.BrandId.HasValue && filter.BrandId > 0)
                query = query.Where(x => x.p.MakeCompanyId == filter.BrandId.Value);


            var totalRecords = await query.CountAsync();
            // ---------------- Sorting----------------

            bool desc = filter.SortDescending;
            query = filter.SortBy?.ToLower() switch
            {
                // DATE
                "outwarddate" =>
                    desc
                        ? query.OrderByDescending(x => x.o.OutwardDate)
                        : query.OrderBy(x => x.o.OutwardDate),

                // OUTWARD NO
                "outwardno" =>
                    desc
                        ? query.OrderByDescending(x => x.o.OutwardNo)
                        : query.OrderBy(x => x.o.OutwardNo),

                // BILL TO COMPANY
                "billtocompany" =>
                    desc
                        ? query.OrderByDescending(x => x.comp.Name)
                               .ThenByDescending(x => x.o.OutwardDate)
                        : query.OrderBy(x => x.comp.Name)
                               .ThenBy(x => x.o.OutwardDate),

                //CATEGORY
                "category" =>
                    desc
                        ? query.OrderByDescending(x => x.c.Name)
                               .ThenByDescending(x => x.o.Id)
                        : query.OrderBy(x => x.c.Name)
                               .ThenBy(x => x.o.Id),

                //PLATFORM
                "platform" =>
                     desc
                         ? query.OrderByDescending(x => x.plat.Name)
                               .ThenByDescending(x => x.o.OutwardDate)

                         : query.OrderBy(x => x.plat.Name)
                               .ThenBy(x => x.o.OutwardDate),

                // DEFAULT
                _ =>
                    desc
                        ? query.OrderByDescending(x => x.o.OutwardDate)
                        : query.OrderBy(x => x.o.OutwardDate)
            };

            // Paging (ALWAYS order first)
            query = query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize);

            // Fetch from DB

            var raw = await query.ToListAsync();

            // Map to DTO (AFTER DB for safety/performance)

            var items = raw.Select(x => new OutwardReportItem
            {
                OutwardId = x.o.Id,
                OutwardNumber = x.o.OutwardNo,
                OutwardDate = x.o.OutwardDate,
                Unit = x.item.Unit,
                Barcode=x.item.BarcodeNo,

                ProductId = x.p.Id,
                ProductName = x.p.Name,
                SKU = x.p.SKU,
                CategoryName = x.c?.Name ?? "",
                BrandName = x.p.MakeCompany ?? "",
                BoxQuantity = x.item.Unit == UnitType.BOX.ToString() ? 1 : 0,
                ItemQuantity = x.item.Unit == UnitType.PCS.ToString() ? 1 : (int)(x.it == null ? 0m : x.it.ItemQuantity),

                BillToCompanyId = x.comp?.Id ?? 0,
                BillToCompanyName = x.comp?.Name ?? "",

                PlatformId = x.plat.Id,
                PlatformName = x.plat.Name,

                Remarks=x.o.Remarks?? string.Empty,
                IsActive = x.item.IsActive,
                CreatedBy = x.item.CreatedBy,
                CreatedOn = x.item.CreatedOn,

                UserName = x.user.Name,
                IsDeleted = x.item.IsDeleted
            })
            .ToList();

            return (items, totalRecords);
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
                        ItemQuantity = item.Quantity,
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
                TotalQuantity = items.Sum(i => i.ItemQuantity),
                TotalValue = items.Sum(i => i.TotalCost),
                UniqueProductsCount = items.Select(i => i.ProductName).Distinct().Count(),
                UniqueSuppliersCount = items.Select(i => i.BillToCompanyId).Distinct().Count(),
                AverageQuantityPerOutward = outwards.Any() ? items.Sum(i => i.ItemQuantity) / outwards.Count : 0,
                AverageValuePerOutward = outwards.Any() ? items.Sum(i => i.TotalCost) / outwards.Count : 0,
                CountByProduct = items.GroupBy(i => i.ProductName).ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }
}