using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.DTO;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using Microsoft.EntityFrameworkCore;


namespace InventoryManagement.Services.Services;

public class SaleReturnService : ISaleReturnService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInwardService _inwardService;
    public SaleReturnService(IUnitOfWork unitOfWork, IInwardService inwardService)
    {
        _unitOfWork = unitOfWork;
        _inwardService = inwardService;
    }
    #region SaleReturns
    public async Task<List<SaleReturnModel>> GetAllSaleReturnsAsync()
    {
        var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>();
        var saleReturns = await saleReturnRepo.GetQueryable()
                         .Include(sr => sr.BillToCompany)
                         .Where(o => !o.IsDeleted)
                         .OrderByDescending(o => o.SaleReturnDate)
                         .ThenByDescending(o => o.Id)
                         .ToListAsync();

        return saleReturns.Select(MapToModel).ToList();
    }
    public async Task<SaleReturnModel?> GetSaleReturnByIdAsync(int id)
    {
        var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>();
        var entity = await saleReturnRepo.GetByIdAsync(id);
        if (entity == null) return null;

        var model = MapToModel(entity);
        model.SaleReturnItemsList = await GetSaleReturnItemsByReturnId(id);
        return model;
    }
    public async Task<int> CreateSaleReturnAsync(SaleReturnModel model)
    {
        try
        {  
            model.SaleReturnNo = await GenerateSaleReturnNumberAsync(model.SaleReturnDate);
            var newSaleReturn = await CreateSaleReturnEntityAsync(model);
         
            return newSaleReturn.Id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating inward: {ex.Message}");
            return 0;
        }

    }

    public async Task<bool> UpdateSaleReturnAsync(SaleReturnModel model)
    {
        var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>();
        var entity = await saleReturnRepo.GetByIdAsync(model.Id);
        if (entity == null) return false;

        UpdateSaleReturnEntityAsync(model, entity);

        saleReturnRepo.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
    public async Task<SaleReturn> CreateSaleReturnEntityAsync(SaleReturnModel model)
    {
        var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>();
        var newSaleReturn = new SaleReturn()
        {
            BillToCompanyId = model.BillToCompanyId,
            SaleReturnDate = model.SaleReturnDate,
            SaleReturnNo = model.SaleReturnNo,
            CreatedBy = model.CreatedBy,
            CreatedOn = model.CreatedOn
        };
        await saleReturnRepo.AddAsync(newSaleReturn);
        await _unitOfWork.SaveChangesAsync();
        return newSaleReturn;
    }
    public void UpdateSaleReturnEntityAsync(SaleReturnModel model, SaleReturn entity)
    {
        var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>();

        entity.BillToCompanyId = model.BillToCompanyId;
        entity.SaleReturnDate = model.SaleReturnDate;
        entity.UpdatedBy = model.UpdatedBy;
        entity.UpdatedOn = DateTime.Now;
    }
    #endregion

    #region ReturnItems
    public async Task<List<SaleReturnItemsModel>> GetSaleReturnItemsByReturnId(int saleReturnId)
    {
        var saleReturnItemRepo = _unitOfWork.GetRepository<SaleReturnItems>();
        var items = await saleReturnItemRepo.GetQueryable()
                    .Where(i => !i.IsDeleted && i.SaleReturnId == saleReturnId)
                    .OrderByDescending(i => i.Id)
                    .ThenByDescending(i => i.ReturnDate)
                    .ToListAsync();
        return items.Select(i => new SaleReturnItemsModel
        {
            Id = i.Id,
            SaleReturnId = i.SaleReturnId,
            ProductId = i.ProductId,
            ReturnDate = i.ReturnDate,
            ReturnType = i.ReturnType,
            ReasonToReturn = i.ReasonToReturn,
            BarCodeNo = i.BarCodeNo,
            OutwardId = i.OutwardId,
            CategoryId = i.CategoryId,
            UnitId = i.UnitId,
            ReturnQuantity = i.ReturnQuantity,
            SerialNo = i.SerialNo,
            ShipToCompanyId = i.ShipToCompanyId,
            IsTakeInStock = i.IsTakeInStock,
            CreatedBy = i.CreatedBy,
            CreatedOn = i.CreatedOn,
            UpdatedBy = i.UpdatedBy,
            UpdatedOn = i.UpdatedOn
        }).ToList();
    }
    public async Task<bool> CreateSaleReturnItem(SaleReturnItemsModel model)
    {
        try 
        {
            var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>();
            var saleReturns = await saleReturnRepo.GetByIdAsync(model.SaleReturnId);

            if (saleReturns == null) return false;

            var item = await CreateSaleReturnItemEntity(model);
            if (item.IsTakeInStock)
            {
                var saleToInwardDto = await ConvertToDto(item);
                await _inwardService.AddReturnItemToSaleInwardAsync(saleToInwardDto);
            }

            return true;
        }
        catch (Exception ez)
        {         
            throw;
        }      
    }
    private async Task<SaleReturnItems> CreateSaleReturnItemEntity(SaleReturnItemsModel model)
    {
        var saleReturnItemRepo = _unitOfWork.GetRepository<SaleReturnItems>();
        var serialNo = await saleReturnItemRepo.CountAsync() + 1;
        var categoryId = await GetCategoryIdByProductId(model.ProductId);
        var entity = new SaleReturnItems()
        {
            SaleReturnId = model.SaleReturnId,
            ProductId = model.ProductId,
            OutwardId = model.OutwardId,
            CategoryId = categoryId,
            ShipToCompanyId = model.ShipToCompanyId,
            BarCodeNo = model.BarCodeNo,
            SerialNo = serialNo,
            ReturnType = model.ReturnType,
            IsTakeInStock = model.IsTakeInStock,
            ReturnDate = model.ReturnDate,
            ReasonToReturn = model.ReasonToReturn,
            UnitId = model.UnitId,
            ReturnQuantity = model.ReturnQuantity,

            CreatedBy = model.CreatedBy,
            CreatedOn = model.CreatedOn
        };
        await saleReturnItemRepo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return entity;

    }
    public async Task<bool> UpdateSaleReturnItem(SaleReturnItemsModel model)
    {
        var saleReturnItemRepo = _unitOfWork.GetRepository<SaleReturnItems>();
        var entity = await saleReturnItemRepo.GetByIdAsync(model.Id);
        if (entity == null )
            return false;

        await UpdateSaleReturnItemEntity(model);
        return true;
    }
    private async Task<bool> UpdateSaleReturnItemEntity(SaleReturnItemsModel model)
    {
        var saleReturnItemRepo = _unitOfWork.GetRepository<SaleReturnItems>();
        var entity = await saleReturnItemRepo.GetByIdAsync(model.Id);
        var categoryId = await GetCategoryIdByProductId(model.ProductId);

        if (entity == null) return false;

        entity.SaleReturnId = model.SaleReturnId;
        entity.ProductId = model.ProductId;
        entity.OutwardId = model.OutwardId;
        entity.CategoryId = categoryId;
        entity.ShipToCompanyId = model.ShipToCompanyId;
        entity.BarCodeNo = model.BarCodeNo;
        entity.SerialNo = model.SerialNo;
        entity.ReturnDate = model.ReturnDate;
        entity.ReturnType = model.ReturnType;
        entity.ReasonToReturn = model.ReasonToReturn;
        entity.IsTakeInStock = model.IsTakeInStock;
        entity.UnitId = model.UnitId;
        entity.ReturnQuantity = model.ReturnQuantity;

        entity.UpdatedBy = model.UpdatedBy;
        entity.UpdatedOn = DateTime.UtcNow;

        saleReturnItemRepo.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteSaleReturnItem(int id)
    {
        try
        {
            var itemRepo = _unitOfWork.GetRepository<SaleReturnItems>();
            var item = await itemRepo.GetQueryable()
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null) return false;
            await itemRepo.DeleteAsync(id);                   
            await _unitOfWork.SaveChangesAsync();

            var saleToInwardDto = await ConvertToDto(item);
            await _inwardService.DeleteSalesReturnInwardItemAsync(saleToInwardDto);

            return true;
        }
        catch
        {
            return false;           
        }
    }
    #endregion

    #region Numbering
    private async Task<string> GenerateSaleReturnNumberAsync(DateTime saleReturnDate)
    {
        var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>();
        var count = await saleReturnRepo.CountAsync();
        count++;
        var newNumber = $"SRN-{saleReturnDate.Year % 100}/{count}";
        return newNumber;
    }

    #endregion

    #region Helper Method

    private async Task<int> GetCategoryIdByProductId(int productId)
    {
        var productrepo = _unitOfWork.GetRepository<Product>();
        var product =await productrepo.GetByIdAsync(productId);
        if (product == null) return 0;
        return product.CategoryId;
    }
    #endregion
    #region BarcodeOperation
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
        var barcodeItems = await barcodeItemRepository.FindWithIncludesAsync(bi =>
            bi.BarcodeNo == barcodeNo && !bi.IsDeleted,
             bi => bi.Inward, bi => bi.InwardItem);

        var barcodeItem = barcodeItems.FirstOrDefault();

        if (barcodeItem == null)
        {
            return new SaleReturnValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid barcode. Barcode does not exist in inventory."
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

        var saleReturnItemRepo = _unitOfWork.GetRepository<SaleReturnItems>();
        bool isAlreadyReturned = await saleReturnItemRepo
            .GetQueryable()
            .AnyAsync(s => s.BarCodeNo == barcodeNo);

        if (isAlreadyReturned)
        {
            return new SaleReturnValidationResult
            {
                IsValid = false,
                ErrorMessage = "This Barcode Already Returned"
            };
        }

        var shipmentCompanyId = barcodeItem.Inward?.ShipMentCompanyId;
        var unitId = barcodeItem.InwardItem?.InwardUnitId;

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
        var outwardId = outward.Id;

        var productRepository = _unitOfWork.GetRepository<Product>();
        var product = await productRepository.GetByIdAsync(outwardDetail.ProductId);
        var productName = product != null ? product.SKU : "Unknown Product";

        return new SaleReturnValidationResult
        {
            IsValid = true,
            ProductName = productName,
            ProductId = outwardDetail.ProductId,
            ShipToId = shipmentCompanyId,
            UnitId = unitId,
            OutwardId = outwardId,
            BarcodeNo = barcodeNo

        };
    }
    private async Task<SaleToInwardDto> ConvertToDto(SaleReturnItems item)
    {
        var result = new SaleToInwardDto()
        {
            ProductId = item.ProductId,
            CategoryId = item.CategoryId,
            ShipToCompanyId = item.ShipToCompanyId,
            ReturnDate = item.ReturnDate ?? DateTime.UtcNow,
            UnitId = item.UnitId,
            ReturnQuantity = item.ReturnQuantity,
            CreatedBy = item.CreatedBy,
            CreatedOn = item.CreatedOn,
            UpdatedBy = item.UpdatedBy,
            UpdatedOn = item.UpdatedOn,
            IsDeleted = item.IsDeleted,
            IsActive = item.IsActive,


        };
        return result;
    }
    #endregion
    private SaleReturnModel MapToModel(SaleReturn entity)
    {
        return new SaleReturnModel()
        {
            Id = entity.Id,
            SaleReturnDate = entity.SaleReturnDate,
            BillToCompanyName = entity.BillToCompany != null ? entity.BillToCompany.Name : string.Empty,
            BillToCompanyId = entity.BillToCompanyId,
            SaleReturnNo = entity.SaleReturnNo,
            CreatedBy = entity.CreatedBy,
            CreatedOn = entity.CreatedOn,
            UpdatedBy = entity.UpdatedBy,
            UpdatedOn = entity.UpdatedOn,
        };

    }
}

