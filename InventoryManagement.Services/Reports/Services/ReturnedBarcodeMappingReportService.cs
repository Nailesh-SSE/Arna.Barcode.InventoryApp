using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Models.ReportModels;
using InventoryManagement.Services.Reports.Interfaces;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Linq.Expressions;

namespace InventoryManagement.Services.Reports.Services;

public class ReturnedBarcodeMappingReportService : IReturnedBarcodeMappingReportService
{
    private readonly IUnitOfWork _unitOfWork;
    public ReturnedBarcodeMappingReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReturnBarcodeMappingReportResult> GenerateReturnBarcodeMappingReportAsync(SaleReturnFilter filter)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var query = BuildBaseQuery(filter);

            var (items, totalRecords) = await ExecuteQuery(query, filter);

            stopwatch.Stop();

            return new ReturnBarcodeMappingReportResult
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
    private IQueryable<SaleReturn> BuildBaseQuery(SaleReturnFilter filter)
    {
        try
        {
            var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>().GetQueryable().AsNoTracking();

            var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>().GetQueryable().AsNoTracking();

            var query = saleReturnRepo
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
                    var prop = Expression.Property(param, nameof(SaleReturn.SaleReturnNo));

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

                    //return query;
                }
            }

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

    private async Task<(List<ReturnBarcodeMappingReportItem>, int)> ExecuteQuery(
 IQueryable<SaleReturn> baseQuery,
 SaleReturnFilter filter)
    {
        try
        {
            var inwardBarcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>().GetQueryable().AsNoTracking();
            var inwardRepo = _unitOfWork.GetRepository<Inward>().GetQueryable().AsNoTracking();
            var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>().GetQueryable().AsNoTracking();
            var productRepo = _unitOfWork.GetRepository<Product>().GetQueryable().AsNoTracking();
            var companyRepo = _unitOfWork.GetRepository<Company>().GetQueryable().AsNoTracking();
            var userRepo = _unitOfWork.GetRepository<Users>().GetQueryable().AsNoTracking();

            var baseQueryData =
                from sr in baseQuery

                from sri in sr.SaleReturnItems
                    .Where(x => !x.IsDeleted)

                join ibOld in inwardBarcodeRepo
                    on sri.BarCodeNo equals ibOld.BarcodeNo into ibOldJoin
                from ibOld in ibOldJoin.DefaultIfEmpty().Where(x =>!x.IsDeleted)

                join ibNew in inwardBarcodeRepo
                    on ibOld.Id equals ibNew.OldBarcodeId into ibNewJoin
                from ibNew in ibNewJoin.DefaultIfEmpty()

                join ii in inwardItemRepo
                    on ibNew.InwardItemId equals ii.Id into iiJoin
                from ii in iiJoin.DefaultIfEmpty()

                join p in productRepo
                    on ii.ProductId equals p.Id into pJoin
                from p in pJoin.DefaultIfEmpty()

                join comp in companyRepo
                    on sri.BillToCompanyId equals comp.Id into compJoin
                from comp in compJoin.DefaultIfEmpty()

                join user in userRepo
                    on sr.CreatedBy equals user.Id into userJoin
                from user in userJoin.DefaultIfEmpty()

                where ibOld != null && ibNew != null
                where ii.IsDeleted == false
                where ibNew.IsDeleted == false
                select new
                {
                    sr,
                    sri,
                    ibOld,
                    ibNew,
                    ii,
                    p,
                    user,
                    comp
                };

            var boxAndpcsQuery =
                from x in baseQueryData

                join inward in inwardRepo
                    on x.ibNew.InwardId equals inward.Id into inwardJoin
                from inward in inwardJoin.DefaultIfEmpty()

                let unitType = (UnitType)x.ii.InwardUnitId
                let isBox = unitType == UnitType.BOX
                let isPcsDirect = unitType == UnitType.PCS && x.ibNew.ParentId == 0

                where x.ii != null && (isBox || isPcsDirect)

                select new ReturnBarcodeMappingReportItem
                {
                    SaleReturnId = x.sr.Id,
                    SaleReturnNo = x.sr.SaleReturnNo,
                    ReturnDate = x.sri.CreatedOn,

                    Type = x.sri.ReturnType,
                    SaleReturnItemId = x.sri.Id,

                    OldBarcode = x.ibOld.BarcodeNo,
                    NewdBarcode = x.ibNew.BarcodeNo,

                    ProductId = x.p != null ? x.p.Id : 0,
                    ProductName = x.p != null ? x.p.Name : "",
                    SKU = x.p != null ? x.p.SKU : "",

                    InwardId = x.ibNew.InwardId,
                    InwardNo = inward.InwardNo,
                    InwardDate = x.ibNew.TransactionDate,
                    IsDeleted = x.ibNew.IsDeleted,
                    IsActive = x.sri.IsActive,

                    Quantity = isBox ? x.sri.ReturnQuantity : 1,
                    BoxQuantity = isBox ? 1 : 0,
                    Unit = isBox ? UnitType.BOX.ToString() : UnitType.PCS.ToString(),

                    BrandId = x.p.MakeCompanyId,
                    BrandName = x.p.MakeCompany,

                    CategoryId = x.p.CategoryId,
                    CateogryName = x.p.Category.Name,

                    ToStock = x.ibNew.IsInStock,
                    Reason = x.sri.ReasonToReturn,
                    Remarks = inward.Remarks,

                    BillToCompanyId = x.sri.BillToCompanyId,
                    BillToName = x.comp.Name,

                    PlatFormName = x.sri.platform.Name,
                    ShipToName = x.sri.ShipToCompany.Name,

                    UserName = x.user != null ? x.user.UserName : ""
                };

            var childQuery =
                from x in baseQueryData

                join ibChild in inwardBarcodeRepo
                    on x.ibNew.Id equals ibChild.ParentId

                join ibChildOld in inwardBarcodeRepo
                    on ibChild.OldBarcodeId equals ibChildOld.Id into oldJoin
                from ibChildOld in oldJoin.DefaultIfEmpty()

                join ii in inwardItemRepo
                    on ibChild.InwardItemId equals ii.Id into iiJoin
                from ii in iiJoin.DefaultIfEmpty()

                join p in productRepo
                    on ii.ProductId equals p.Id into pJoin
                from p in pJoin.DefaultIfEmpty()

                join inward in inwardRepo
                    on ibChild.InwardId equals inward.Id into inwardJoin
                from inward in inwardJoin.DefaultIfEmpty()

                select new ReturnBarcodeMappingReportItem
                {
                    SaleReturnId = x.sr.Id,
                    SaleReturnNo = x.sr.SaleReturnNo,
                    ReturnDate = x.sri.CreatedOn,

                    SaleReturnItemId = x.sri.Id,

                    OldBarcode = ibChildOld != null ? ibChildOld.BarcodeNo : "",
                    NewdBarcode = ibChild.BarcodeNo,

                    ProductId = p != null ? p.Id : 0,
                    ProductName = p != null ? p.Name : "",
                    SKU = p != null ? p.SKU : "",

                    InwardId = ibChild.InwardId,
                    InwardNo = inward.InwardNo,

                    Quantity = 1,
                    BoxQuantity = 0,
                    Unit = UnitType.PCS.ToString(),

                    Reason = x.sri.ReasonToReturn,
                    Remarks = inward.Remarks,

                    InwardDate = ibChild.TransactionDate,
                    IsDeleted = ibChild.IsDeleted,
                    IsActive = ibChild.IsActive,

                    BillToCompanyId = x.sri.BillToCompanyId,
                    BillToName = x.comp.Name,

                    BrandId = p != null ? p.MakeCompanyId : 0,
                    BrandName = p != null ? p.MakeCompany : "",

                    CategoryId = p != null ? p.CategoryId : 0,
                    CateogryName = p != null ? p.Category.Name : "",

                    PlatFormName = x.sri.platform.Name,
                    ShipToName = x.sri.ShipToCompany.Name,

                    Type = x.sri.ReturnType,
                    ToStock = ibChild.IsInStock,

                    UserName = x.user != null ? x.user.UserName : ""
                };

            var query = boxAndpcsQuery.Concat(childQuery);


            if (string.IsNullOrWhiteSpace(filter.ReturnNumbers))
            {
                if (filter.BillToCompanyId.HasValue)
                {
                    int filterId = filter.BillToCompanyId.Value;

                    query = query.Where(x => x.BillToCompanyId == filterId);
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.ProductName))
                query = query.Where(x => x.ProductName == filter.ProductName);

            if (filter.BrandId.HasValue && filter.BrandId > 0)
                query = query.Where(x => x.BrandId == filter.BrandId.Value);

            var totalRecords = await query.CountAsync();

            // Sorting
            query = filter.SortDescending
                ? query.OrderByDescending(x => x.SaleReturnId)
                : query.OrderBy(x => x.SaleReturnId);

            // Paging
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalRecords);
        }
        catch (Exception ex)
        {
            throw new Exception("Error executing return barcode mapping query", ex);
        }
    }

    public async Task<byte[]> ExportToExcelAsync(SaleReturnFilter filter)
    {
        // Remove paging for export
        filter.PageSize = int.MaxValue;
        filter.PageNumber = 1;

        var report = await GenerateReturnBarcodeMappingReportAsync(filter);

        IWorkbook workbook = new XSSFWorkbook();
        ISheet sheet = workbook.CreateSheet("New Barcode Report");

        // Header row
        IRow headerRow = sheet.CreateRow(0);
        string[] headers = new string[]
           {
         "No.", "Return Comp", "Ship To", "Platform", "Return No","Return Date","Return Time", "Inward No","Inward Date","Inward Time",
               "OldBarcode","NewBarcode", "Category" ,"Brand", "Product" ,"SKU",
         "Unit","Box Quantity", "Item Quantity","Return Type","To Stock","Action","Status","Reason","User Name"
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
            row.CreateCell(1).SetCellValue(item.BillToName);
            row.CreateCell(2).SetCellValue(item.ShipToName);
            row.CreateCell(3).SetCellValue(item.PlatFormName);
            row.CreateCell(4).SetCellValue(item.SaleReturnNo);
            row.CreateCell(5).SetCellValue(item.ReturnDate.ToString("dd/MM/yyyy"));
            row.CreateCell(6).SetCellValue(item.ReturnDate.ToString("HH:mm:ss"));
            row.CreateCell(7).SetCellValue(item.InwardNo);
            row.CreateCell(8).SetCellValue(item.InwardDate.ToString("dd/MM/yyyy"));
            row.CreateCell(9).SetCellValue(item.InwardDate.ToString("HH:mm:ss"));
            row.CreateCell(10).SetCellValue(item.OldBarcode);
            row.CreateCell(11).SetCellValue(item.NewdBarcode);
            row.CreateCell(12).SetCellValue(item.CateogryName);
            row.CreateCell(13).SetCellValue(item.BrandName);
            row.CreateCell(14).SetCellValue(item.ProductName);
            row.CreateCell(15).SetCellValue(item.SKU);
            row.CreateCell(16).SetCellValue(item.Unit);
            row.CreateCell(17).SetCellValue((double)item.BoxQuantity);
            row.CreateCell(18).SetCellValue((double)item.Quantity);
            row.CreateCell(19).SetCellValue(((ReturnType)item.Type).ToString());
            row.CreateCell(20).SetCellValue(item.ToStock ? "Yes" : "No");
            row.CreateCell(21).SetCellValue(item.IsActive ? "Active" : "Inactive");
            row.CreateCell(22).SetCellValue(item.IsDeleted ? "Deleted" : "Saved");
            row.CreateCell(23).SetCellValue(item.Reason);
            row.CreateCell(24).SetCellValue(item.UserName);
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
}