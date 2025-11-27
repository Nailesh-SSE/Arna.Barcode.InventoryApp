using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.DTO;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services;

public class SaleReturnService : ISaleReturnService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInwardService _inwardService;

    public SaleReturnService(IUnitOfWork unitOfWork, IInwardService inwardService)
    {
        _unitOfWork = unitOfWork;
        _inwardService = inwardService;
    }

    public async Task<List<SaleReturnModel>> GetAllSaleReturnsAsync()
    {
        var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
        var saleReturns = await saleReturnRepository.FindAsync(sr => !sr.IsDeleted);

        return saleReturns.Select(MapToModel).ToList();
    }

    public async Task<SaleReturnModel?> GetSaleReturnByIdAsync(int id)
    {
        var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
        var saleReturn = await saleReturnRepository.GetByIdAsync(id);

        return saleReturn == null ? null : MapToModel(saleReturn);
    }

    public async Task<bool> CreateSaleReturnAsync(SaleReturnModel model)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
            await GenerateReturnNumberAsync(model);

            var newSaleReturn = new SaleReturn
            {
                ReturnNo = model.ReturnNo,
                ReturnDate = model.ReturnDate,
                BillingDate = model.BillingDate ?? DateTime.Now,
                BillToCompanyId = model.BillToCompanyId,
                ProductId = model.ProductId,
                ReturnType = model.ReturnType.GetValueOrDefault(),
                BarcodeNo = model.BarcodeNo,
                Quantity = model.Quantity,
                BoxQuantity = model.BoxQuantity,
                UnitId = model.UnitId,
                Reason = model.Reason,
                Remarks = model.Remarks,
                IsTakeInStock = model.IsTakeInStock,
                IsActive = true,
                IsDeleted = false,
                CreatedBy = model.CreatedBy,
                CreatedOn = model.CreatedOn
            };

            await saleReturnRepository.AddAsync(newSaleReturn);
            await _unitOfWork.SaveChangesAsync();

            if (model.IsTakeInStock)
            {
                var inwardId = await FindInwardIdForSaleReturnAsync(model);

                var parameter = new ReturnItemDto()
                {
                    InwardId = inwardId.Value,
                    ProductId = model.ProductId,
                    ItemQuantity = model.Quantity,
                    BoxQuantity = model.BoxQuantity,
                    UnitId = model.UnitId,
                    UnitName = model.UnitName,
                    CreatedBy = model.CreatedBy
                };
                if (inwardId.HasValue)
                {
                    var inwardItemId = await _inwardService.AddReturnedItemToExistingInwardAsync(parameter);

                    newSaleReturn.ReturnInwardItemId = inwardItemId;
                    saleReturnRepository.Update(newSaleReturn);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            await _unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            return false;
        }
    }

    public async Task<bool> UpdateSaleReturnAsync(SaleReturnModel model)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
            var existing = await saleReturnRepository.GetByIdAsync(model.Id);

            if (existing == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            var originalTakeInStock = existing.IsTakeInStock;

            existing.ReturnDate = model.ReturnDate;
            existing.BillingDate = model.BillingDate ?? DateTime.Now;
            existing.BillToCompanyId = model.BillToCompanyId;
            existing.ProductId = model.ProductId;
            existing.ReturnType = model.ReturnType.GetValueOrDefault();
            existing.BarcodeNo = model.BarcodeNo;
            existing.Quantity = model.Quantity;
            existing.UnitId = model.UnitId;

            existing.Reason = model.Reason;
            existing.Remarks = model.Remarks;
            existing.IsActive = model.IsActive;
            existing.UpdatedBy = model.UpdatedBy;
            existing.UpdatedOn = DateTime.UtcNow;

            if (!originalTakeInStock && model.IsTakeInStock)
            {
                var inwardId = await FindInwardIdForSaleReturnAsync(model);


                var parameter = new ReturnItemDto()
                {
                    InwardId = inwardId.Value,
                    ProductId = model.ProductId,
                    ItemQuantity = model.Quantity,
                    BoxQuantity = model.BoxQuantity,
                    UnitId = model.UnitId,
                    UnitName = model.UnitName,
                    UpdatedBy = model.UpdatedBy
                };

                if (inwardId.HasValue)
                {
                    var inwardItemId = await _inwardService.AddReturnedItemToExistingInwardAsync(parameter);
                    existing.ReturnInwardItemId = inwardItemId;
                }

                existing.IsTakeInStock = true;
            }
            else if (originalTakeInStock && !model.IsTakeInStock)
            {
                if (existing.ReturnInwardItemId.HasValue)
                {
                    await _inwardService.RemoveReturnedReturnedItemFromStockAsync(
                        existing.ReturnInwardItemId.Value);
                }

                existing.ReturnInwardItemId = null;
                existing.IsTakeInStock = false;
            }
            else
            {
                existing.IsTakeInStock = model.IsTakeInStock;
            }

            saleReturnRepository.Update(existing);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> DeleteSaleReturnAsync(int id, int userId)
    {
        try
        {
            var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
            var saleReturn = await saleReturnRepository.GetByIdAsync(id);

            if (saleReturn == null)
                return false;

            saleReturn.IsDeleted = true;
            saleReturn.IsActive = false;
            saleReturn.UpdatedOn = DateTime.UtcNow;
            saleReturn.UpdatedBy = userId;
            saleReturnRepository.Update(saleReturn);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
    public async Task<SaleReturnValidationResult> ValidateBarcodeForSaleReturnAsync(string barcodeNo, int companyId)
    {
        if (string.IsNullOrWhiteSpace(barcodeNo))
        {
            return new SaleReturnValidationResult
            {
                IsValid = false,
                ErrorMessage = "Please Scan Barcode."
            };
        }

        var barcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();
        var barcodeItems = await barcodeItemRepository.FindAsync(bi =>
            bi.BarcodeNo == barcodeNo && !bi.IsDeleted);

        var barcodeItem = barcodeItems.FirstOrDefault();

        if (barcodeItem == null)
        {
            return new SaleReturnValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid barcode. Barcode does not exist in inventory."
            };
        }

        var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();
        var outwardDetails = await outwardDetailRepository.FindWithIncludesAsync(
     od => od.BarcodeNo == barcodeNo && !od.IsDeleted,
     od => od.Outward
 );


        var outwardDetail = outwardDetails.FirstOrDefault();
        if (outwardDetail == null)
        {
            return new SaleReturnValidationResult
            {
                IsValid = false,
                ErrorMessage = "This barcode was not sold (no outward record found)."
            };
        }

        var outward = outwardDetail.Outward;
        if (outward == null || outward.BillToCompanyId != companyId)
        {
            return new SaleReturnValidationResult
            {
                IsValid = false,
                ErrorMessage = "This barcode was not sold to the selected company."
            };
        }

        if (barcodeItem.IsInStock)
        {
            return new SaleReturnValidationResult
            {
                IsValid = false,
                ErrorMessage = "Barcode indicates item is still in stock. Can't accept return for an item that was not sold."
            };
        }

        var productRepository = _unitOfWork.GetRepository<Product>();
        var product = await productRepository.GetByIdAsync(outwardDetail.ProductId);
        var productName = product != null ? product.Name : "Unknown Product";

        return new SaleReturnValidationResult
        {
            IsValid = true,
            ProductName = productName
        };
    }

    public async Task<IEnumerable<SaleReturnModel>> GetSaleReturnsByCompanyAsync(int companyId)
    {
        var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
        var saleReturns = await saleReturnRepository.FindAsync(
            sr => sr.BillToCompanyId == companyId && !sr.IsDeleted);

        return saleReturns.Select(MapToModel);
    }

    // ================== KEY CHANGE: FIND INWARD ==================
    /// <summary>
    /// 1) If Barcode is provided -> get InwardId from InwardBarcodeItem.
    /// 2) If no Barcode -> find an Outward using Company + Product + Date,
    ///    then use its BarcodeNo to get InwardId from InwardBarcodeItem.
    /// </summary>
    private async Task<int?> FindInwardIdForSaleReturnAsync(SaleReturnModel model)
    {
        var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();

        // 1) Use barcode directly if we have it
        if (!string.IsNullOrWhiteSpace(model.BarcodeNo))
        {
            var barcode = (await barcodeRepo.FindAsync(b =>
                    b.BarcodeNo == model.BarcodeNo && !b.IsDeleted))
                .FirstOrDefault();

            if (barcode != null)
                return barcode.InwardId;
        }

        var outwardRepo = _unitOfWork.GetRepository<Outward>();
        var outwardDetailRepo = _unitOfWork.GetRepository<OutwardDetail>();

        var targetDate = model.BillingDate ?? model.ReturnDate;

        var outwardQuery =
            from od in outwardDetailRepo.GetQueryable()
            join o in outwardRepo.GetQueryable() on od.OutwardId equals o.Id
            where !od.IsDeleted
                  && !o.IsDeleted
                  && od.ProductId == model.ProductId
                  && o.BillToCompanyId == model.BillToCompanyId
            select new { Outward = o, Detail = od };

        if (targetDate != default)
        {
            outwardQuery = outwardQuery
                .Where(x => x.Outward.OutwardDate.Date == targetDate.Date);
        }

        var outwardMatch = await outwardQuery
            .OrderByDescending(x => x.Outward.OutwardDate)
            .FirstOrDefaultAsync();

        if (outwardMatch == null)
            return null;

        var anyBarcodeNo = outwardMatch.Detail.BarcodeNo;
        if (string.IsNullOrWhiteSpace(anyBarcodeNo))
            return null;

        var inwardBarcode = (await barcodeRepo.FindAsync(b =>
                b.BarcodeNo == anyBarcodeNo && !b.IsDeleted))
            .FirstOrDefault();

        return inwardBarcode?.InwardId;
    }

    private async Task GenerateReturnNumberAsync(SaleReturnModel model)
    {
        var repository = _unitOfWork.GetRepository<SaleReturn>();
        var saleReturns = await repository.GetAllAsync();
        var lastReturn = saleReturns
            .Where(sr => sr.IsActive && !sr.IsDeleted)
            .OrderByDescending(sr => sr.Id)
            .FirstOrDefault();

        if (lastReturn != null && int.TryParse(lastReturn.ReturnNo.Replace("SR", ""), out int lastNumber))
        {
            model.ReturnNo = $"SR{(lastNumber + 1).ToString("D6")}";
        }
        else
        {
            model.ReturnNo = "SR000001";
        }
    }

    private SaleReturnModel MapToModel(SaleReturn entity)
    {
        return new SaleReturnModel
        {
            Id = entity.Id,
            ReturnNo = entity.ReturnNo,
            ReturnDate = entity.ReturnDate,
            BillingDate = entity.BillingDate,
            BillToCompanyId = entity.BillToCompanyId,
            ProductId = entity.ProductId,
            ReturnType = entity.ReturnType,
            BarcodeNo = entity.BarcodeNo,
            Quantity = entity.Quantity,
            BoxQuantity = entity.BoxQuantity,
            UnitId = entity.UnitId,
            Reason = entity.Reason,
            Remarks = entity.Remarks,
            IsActive = entity.IsActive,
            CompanyName = entity.BillToCompany?.Name ?? "N/A",
            CreatedBy = entity.CreatedBy,
            CreatedOn = entity.CreatedOn,
            UpdatedBy = entity.UpdatedBy,
            UpdatedOn = entity.UpdatedOn,
            IsTakeInStock = entity.IsTakeInStock,
            ReturnInwardItemId = entity.ReturnInwardItemId
        };
    }
}
