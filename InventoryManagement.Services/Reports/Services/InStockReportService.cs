using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Models.ReportModels;
using InventoryManagement.Services.Reports.Interfaces;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace InventoryManagement.Services.Reports.Services
{
    public class InStockReportService : IInStockReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InStockReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<InStockReportResult> GenerateInStockReportAsync(InStockFilter filter)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var query = BuildBaseQuery(filter);
                var (items, totalRecords) = await ExecuteQuery(query, filter);

                stopwatch.Stop();

                return new InStockReportResult
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

        public async Task<byte[]> ExportToExcelAsync(InStockFilter filter)
        {
            // Remove paging for export
            filter.PageSize = int.MaxValue;
            filter.PageNumber = 1;

            var report = await GenerateInStockReportAsync(filter);

            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("InStock Report");

            // Header row
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = new string[]
            {
              "No", "Barcode", "Date", "Status",
              "Brand", "ProductName", "Category", "UserName","BatchNo","InwardNo"   
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

                row.CreateCell(0).SetCellValue(i + 1); // No (sequence)

                row.CreateCell(1).SetCellValue(item.BarcodeNo);
                row.CreateCell(2).SetCellValue(item.CreatedOn.ToString("yyyy/MM/dd"));
                row.CreateCell(3).SetCellValue(item.IsInStock ? "In Stock" : "Sold");

                row.CreateCell(4).SetCellValue(item.BrandName);
                row.CreateCell(5).SetCellValue(item.SKU);
                row.CreateCell(6).SetCellValue(item.CategoryName);
                row.CreateCell(7).SetCellValue(item.UserName);
               
                row.CreateCell(8).SetCellValue(item.BatchNo);
                row.CreateCell(9).SetCellValue(item.InwardNo);

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

        private IQueryable<InwardBarcodeItem> BuildBaseQuery(InStockFilter filter)
        {
            var inwardBarcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();

            var query = inwardBarcodeRepo.GetQueryable()
                .AsNoTracking()
                .Where(ibt => !ibt.IsDeleted && ibt.IsActive == true);

            // Apply date filters
            if (filter.StartDate.HasValue)
                query = query.Where(i => i.CreatedOn.Date >= filter.StartDate.Value.Date);

            if (filter.EndDate.HasValue)
                query = query.Where(i => i.CreatedOn.Date <= filter.EndDate.Value.Date);

            // Apply stock filter
            query = query.Where(i => i.IsInStock == filter.IsInStock);

            return query;
        }

        private async Task<(List<InStockReportItem> Items, int TotalRecords)> ExecuteQuery(
            IQueryable<InwardBarcodeItem> baseQuery,
            InStockFilter filter)
        {
            var productRepo = _unitOfWork.GetRepository<Product>();
            var categoryRepo = _unitOfWork.GetRepository<Category>();
            var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>();
            var userRepo = _unitOfWork.GetRepository<Users>();
            var inwardRepo = _unitOfWork.GetRepository<Inward>();

            var query = baseQuery
                .Join(inwardItemRepo.GetQueryable(),
                    ibt => ibt.InwardItemId,
                    it => it.Id,
                    (ibt, it) => new { ibt, it })
                .Join(productRepo.GetQueryable(),
                    x => x.it.ProductId,
                    p => p.Id,
                    (x, p) => new { x.ibt, x.it, p })
                .Join(categoryRepo.GetQueryable(),
                    x => x.p.CategoryId,
                    c => c.Id,
                    (x, c) => new { x.ibt, x.it, x.p, c })
                .Join(inwardRepo.GetQueryable(),
                    x => x.ibt.InwardId,
                    i => i.Id,
                    (x, i) => new { x.ibt, x.it, x.p, x.c, i })
                .GroupJoin(userRepo.GetQueryable(),
                    x => x.ibt.CreatedBy,
                    uc => uc.Id,
                    (x, ucs) => new { x, ucs })
                .SelectMany(
                    x => x.ucs.DefaultIfEmpty(),
                    (x, uc) => new { x.x.ibt, x.x.it, x.x.p, x.x.c, x.x.i, uc });

            // Filters
            if (filter.CategoryId.HasValue)
                query = query.Where(x => x.p.CategoryId == filter.CategoryId);

            if (filter.CompanyId.HasValue)
                query = query.Where(x => x.p.MakeCompanyId == filter.CompanyId);

            var totalRecords = await query.CountAsync();

            // Sorting
            bool desc = filter.SortDescending;
            query = filter.SortBy?.ToLower() switch
            {
                "instockdate" =>
                    desc ? query.OrderByDescending(x => x.ibt.CreatedOn)
                         : query.OrderBy(x => x.ibt.CreatedOn),

                "brand" =>
                    desc ? query.OrderByDescending(x => x.p.MakeCompanyId).ThenByDescending(x => x.p.Name)
                         : query.OrderBy(x => x.p.MakeCompanyId).ThenBy(x => x.p.Name),

                "product" =>
                    desc ? query.OrderByDescending(x => x.p.Name)
                         : query.OrderBy(x => x.p.Name),

                "category" =>
                    desc ? query.OrderByDescending(x => x.c.Id)
                               .ThenByDescending(x => x.p.MakeCompanyId)
                               .ThenByDescending(x => x.p.Name)
                         : query.OrderBy(x => x.c.Id)
                               .ThenBy(x => x.p.MakeCompanyId)
                               .ThenBy(x => x.p.Name),

                _ =>
                    desc ? query.OrderByDescending(x => x.ibt.CreatedOn)
                         : query.OrderBy(x => x.ibt.CreatedOn),
            };

            // Paging
            query = query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize);

            // Map to DTO
            var items = query.Select(x => new InStockReportItem
            {
                BrandId = x.p.MakeCompanyId,
                BrandName = x.p.MakeCompany,
              
                ProductId = x.p.Id,
                SKU = x.p.SKU,
              
                BarcodeNo = x.ibt.BarcodeNo,
                IsInStock = x.ibt.IsInStock,
             
                CategoryId = x.c.Id,
                CategoryName = x.c.Name,
            
                CreatedBy = x.ibt.CreatedBy,
                UserName = x.uc != null ? x.uc.UserName : "",
                CreatedOn = x.ibt.CreatedOn,

                BatchNo= x.it.BatchNo,
                InwardNo = x.i.InwardNo

            }).ToList();

            return (items, totalRecords);
        }
    }
}
