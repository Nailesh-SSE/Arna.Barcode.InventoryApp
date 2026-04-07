using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Models.ReportModels;
using InventoryManagement.Services.Reports.Interfaces;
using Microsoft.EntityFrameworkCore;
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
                    p
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
                    ReturnDate = x.sr.SaleReturnDate,

                    SaleReturnItemId = x.sri.Id,

                    OldBarcode = x.ibOld.BarcodeNo,
                    NewdBarcode = x.ibNew.BarcodeNo,

                    ProductId = x.p != null ? x.p.Id : 0,
                    ProductName = x.p != null ? x.p.Name : "",
                    SKU = x.p != null ? x.p.SKU : "",

                    InwardId = x.ibNew.InwardId,
                    InwardNo = inward.InwardNo,

                    Quantity = isBox ? x.sri.ReturnQuantity : 1,
                    BoxQuantity = isBox ? 1 : 0,
                    Unit = isBox ? UnitType.BOX.ToString() : UnitType.PCS.ToString(),


                    Reason = x.sri.ReasonToReturn,
                    Remarks = inward.Remarks,
                    BillToCompanyId = x.sri.BillToCompanyId, 

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
                    ReturnDate = x.sr.SaleReturnDate,

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
                    BillToCompanyId = x.sri.BillToCompanyId,

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
}