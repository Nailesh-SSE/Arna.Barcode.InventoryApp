using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services;

public class SaleReturnService : ISaleReturnService
{
    private readonly IUnitOfWork _unitOfWork;

    public SaleReturnService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
                BillingDate = model.BillingDate, 
                BillToCompanyId = model.BillToCompanyId,
                ProductId = model.ProductId, 
                ReturnType = model.ReturnType.GetValueOrDefault(),
                BarcodeNo = model.BarcodeNo,
                Quantity = model.Quantity,
                Reason = model.Reason,
                Remarks = model.Remarks,
                IsActive = true,
                IsDeleted = false,
                CreatedBy = model.CreatedBy,
                CreatedOn = model.CreatedOn
            };

            await saleReturnRepository.AddAsync(newSaleReturn);
            await _unitOfWork.SaveChangesAsync();
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
        try
        {
            var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
            var existing = await saleReturnRepository.GetByIdAsync(model.Id);

            if (existing == null)
                return false;

            existing.ReturnDate = model.ReturnDate;
            existing.BillingDate = model.BillingDate;
            existing.BillToCompanyId = model.BillToCompanyId;
            existing.ProductId = model.ProductId;
            existing.ReturnType = model.ReturnType.GetValueOrDefault();
            existing.BarcodeNo = model.BarcodeNo;
            existing.Quantity = model.Quantity;
            existing.Reason = model.Reason;
            existing.Remarks = model.Remarks;
            existing.IsActive = model.IsActive;
            existing.UpdatedBy = model.UpdatedBy;
            existing.UpdatedOn = DateTime.UtcNow;

            saleReturnRepository.Update(existing);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch
        {
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

        // Check outward detail exists for this barcode
        var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();
        var outwardDetails = await outwardDetailRepository.FindAsync(od =>
            od.BarcodeNo == barcodeNo && !od.IsDeleted);

        var outwardDetail = outwardDetails.FirstOrDefault();
        if (outwardDetail == null)
        {
            return new SaleReturnValidationResult
            {
                IsValid = false,
                ErrorMessage = "This barcode was not sold (no outward record found)."
            };
        }

        // verify outward's billing company
        var outward = outwardDetail.Outward;
        if (outward == null || outward.BillToCompanyId != companyId)
        {
            return new SaleReturnValidationResult
            {
                IsValid = false,
                ErrorMessage = "This barcode was not sold to the selected company."
            };
        }

        // verify InwardBarcodeItem.IsInStock == false (i.e., item was sold)
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
            Reason = entity.Reason,
            Remarks = entity.Remarks,
            IsActive = entity.IsActive,
            CompanyName = entity.BillToCompany?.Name ?? "N/A",
            CreatedBy = entity.CreatedBy,
            CreatedOn = entity.CreatedOn,
            UpdatedBy = entity.UpdatedBy,
            UpdatedOn = entity.UpdatedOn
        };
    }

}