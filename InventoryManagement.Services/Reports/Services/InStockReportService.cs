using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
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
              "No","Ship Company","InwardNo", "Inward Date" , "Inward Time"  , "Category" , 
              "Brand","Product" ,"Product Name" , " Item Barcode" ,"BoxBarcode", "Unit","Box Qty","Item Qty", "Remark", "Status", "UserName" , "BatchNo"   
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
                row.CreateCell(1).SetCellValue(item.ShipCompanyname);
                row.CreateCell(2).SetCellValue(item.InwardNo);
                row.CreateCell(3).SetCellValue(item.CreatedOn.ToString("dd/MM/yyyy"));
                row.CreateCell(4).SetCellValue(item.CreatedOn.ToString("HH:mm:ss"));
                row.CreateCell(5).SetCellValue(item.CategoryName);
                row.CreateCell(6).SetCellValue(item.BrandName);
                row.CreateCell(7).SetCellValue(item.ProductName);
                row.CreateCell(8).SetCellValue(item.SKU);
                row.CreateCell(9).SetCellValue(item.ItemBarcodeNo);
                row.CreateCell(10).SetCellValue(item.BoxBarcodeNo);
                row.CreateCell(11).SetCellValue(item.Unit);
                row.CreateCell(12).SetCellValue((double)item.BoxQuantity);
                row.CreateCell(13).SetCellValue((double)item.Quantity);
                row.CreateCell(14).SetCellValue(item.Remark);
                row.CreateCell(15).SetCellValue(item.IsInStock ? "In Stock" : "Sold");
                row.CreateCell(16).SetCellValue(item.UserName);
                row.CreateCell(17).SetCellValue(item.BatchNo);

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
            var productRepo = _unitOfWork.GetRepository<Product>().GetQueryable().AsNoTracking();
            var categoryRepo = _unitOfWork.GetRepository<Category>().GetQueryable().AsNoTracking();
            var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>().GetQueryable().AsNoTracking();
            var userRepo = _unitOfWork.GetRepository<Users>().GetQueryable().AsNoTracking();
            var inwardRepo = _unitOfWork.GetRepository<Inward>().GetQueryable().AsNoTracking();
            var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>().GetQueryable().AsNoTracking();

            var barcodeCounts =
                 from b in barcodeRepo
                 where b.ParentId != 0 && b.IsInStock == filter.IsInStock
                 group b by b.ParentId into g
                 select new
                 {
                     ParentId = g.Key,
                     Count = g.Count()
                 };

            var query =
                from ibt in baseQuery
                join it in inwardItemRepo
                    on ibt.InwardItemId equals it.Id
             
                join p in productRepo
                    on it.ProductId equals p.Id
             
                join c in categoryRepo
                    on p.CategoryId equals c.Id
             
                join i in inwardRepo
                    on ibt.InwardId equals i.Id
             
                join parent in barcodeRepo
                     on ibt.ParentId equals parent.Id into parentBarcodes
                from parentBarcode in parentBarcodes.DefaultIfEmpty()
             
                join uc in userRepo
                     on ibt.CreatedBy equals uc.Id into userGroup
                from uc in userGroup.DefaultIfEmpty()
              
                join bc in barcodeCounts
                     on ibt.Id equals bc.ParentId into bcGroup
                from bc in bcGroup.DefaultIfEmpty()
                select new
                {
                    ibt,
                    it,
                    p,
                    c,
                    i,
                    ParentBarcode = parentBarcode,
                    uc,
                    BarcodeCount = (int?)bc.Count ?? 0
                };
            // Filters
            if (filter.CategoryId.HasValue)
                query = query.Where(x => x.p.CategoryId == filter.CategoryId);

            if (filter.CompanyId.HasValue)
                query = query.Where(x => x.p.MakeCompanyId == filter.CompanyId);

            if (!string.IsNullOrWhiteSpace(filter.ProductName))
                query = query.Where(x => x.p.Name == filter.ProductName);

            if (filter.BrandId.HasValue && filter.BrandId > 0)
                query = query.Where(x => x.p.MakeCompanyId == filter.BrandId.Value);

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
            var items = query
                       .Select(x => new
                       {
                           x,
                           IsBox = x.it.InwardUnitId == (int)UnitType.BOX && x.ibt.ParentId == 0
                       })
                       .Select(t => new InStockReportItem
                       {
                           InwardNo = t.x.i.InwardNo,
                           CreatedOn = t.x.ibt.CreatedOn,
                      
                           ShipCompanyId = t.x.i.ShipMentCompanyId,
                           ShipCompanyname = t.x.i.ShipMentCompany.Name,
                      
                           CategoryId = t.x.c.Id,
                           CategoryName = t.x.c.Name,
                      
                           BrandId = t.x.p.MakeCompanyId,
                           BrandName = t.x.p.MakeCompany,
                      
                           ProductId = t.x.p.Id,
                           ProductName = t.x.p.Name,
                           SKU = t.x.p.SKU,
                      
                           Unit = t.IsBox ? "BOX" : "PCS",
                      
                           Quantity = t.IsBox
                               ? Math.Max(0, (decimal)t.x.BarcodeCount)
                               : 1m,
                      
                           BoxQuantity = t.IsBox ? t.x.it.BoxQuantity : 0,
                      
                           ItemBarcodeNo = t.IsBox ? string.Empty : t.x.ibt.BarcodeNo,
                      
                           BoxBarcodeNo = t.x.ParentBarcode != null
                               ? t.x.ParentBarcode.BarcodeNo
                               : (t.IsBox ? t.x.ibt.BarcodeNo : string.Empty),
                      
                           IsInStock = t.x.ibt.IsInStock,
                      
                           Remark = t.x.i.Remarks,
                           CreatedBy = t.x.ibt.CreatedBy,
                           UserName = t.x.uc != null ? t.x.uc.UserName : "",
                      
                           BatchNo = t.x.it.BatchNo
                       })
                       .ToList();

            return (items, totalRecords);
        }
    }
}
