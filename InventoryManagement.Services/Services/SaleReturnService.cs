using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.DTO;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;


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
    public async Task<List<SaleReturnModel>> GetAllSaleReturnsAsync(bool isAdmin)
    {
        var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>();
        var saleReturns = await saleReturnRepo.GetQueryable()
                         .Include(sr => sr.BillToCompany)
                         .Where(o => !o.IsDeleted)
                         .OrderByDescending(o => o.SaleReturnDate)
                         .ThenByDescending(o => o.Id)
                         .ToListAsync();
        if (!isAdmin)
        {
            var today = DateTime.Today;
            saleReturns = saleReturns.Where(s => s.SaleReturnDate.Date == today).ToList();
        }
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
        var saleReturnItemsRepo = _unitOfWork.GetRepository<SaleReturnItems>().GetQueryable().AsNoTracking();
        var productRepo = _unitOfWork.GetRepository<Product>().GetQueryable().AsNoTracking();
        var platformRepo = _unitOfWork.GetRepository<Platform>().GetQueryable().AsNoTracking();
        var companyRepo = _unitOfWork.GetRepository<Company>().GetQueryable().AsNoTracking();
        var outwardRepo = _unitOfWork.GetRepository<Outward>().GetQueryable().AsNoTracking();

        var query =
       from item in saleReturnItemsRepo

       join p in productRepo
           on item.ProductId equals p.Id into prod
       from p in prod.DefaultIfEmpty()

       join plat in platformRepo
           on item.platformId equals plat.Id into plat
       from pl in plat.DefaultIfEmpty()

       join bill in companyRepo
           on item.BillToCompanyId equals bill.Id into billCo
       from bill in billCo.DefaultIfEmpty()

       join ship in companyRepo
           on item.ShipToCompanyId equals ship.Id into shipCo
       from ship in shipCo.DefaultIfEmpty()

       join o in outwardRepo
           on item.OutwardId equals o.Id into outw
       from o in outw.DefaultIfEmpty()

       where !item.IsDeleted && item.SaleReturnId == saleReturnId
       orderby item.Id descending, item.ReturnDate descending

       select new SaleReturnItemsModel
       {
           Id = item.Id,
           SaleReturnId = item.SaleReturnId,

           ProductId = item.ProductId,
           ProductName = p != null ? p.SKU : string.Empty,

           PlatformId = item.platformId,
           PlatformName = pl != null ? pl.Name : string.Empty,

           BillToCompanyId = item.BillToCompanyId,
           BillToCompanyName = bill != null ? bill.Name : string.Empty,

           ShipToCompanyId = item.ShipToCompanyId,
           ShipToCompanyName = ship != null ? ship.Name : string.Empty,

           OutwardId = item.OutwardId,
           OutwardNo = o != null ? o.OutwardNo : string.Empty,

           BarCodeNo = item.BarCodeNo,
           ReturnQuantity = item.ReturnQuantity,
           ReturnType = item.ReturnType,
           IsTakeInStock = item.IsTakeInStock,
           ReasonToReturn = item.ReasonToReturn,
           ReturnDate = item.ReturnDate,

           UnitId = item.UnitId,
           CategoryId = item.CategoryId,
           SerialNo = item.SerialNo,

           CreatedBy = item.CreatedBy,
           CreatedOn = item.CreatedOn
       };

        return await query.ToListAsync();
    }
    public async Task<bool> CreateSaleReturnItem(SaleReturnItemsModel model)
    {
        try
        {
            var saleReturnRepo = _unitOfWork.GetRepository<SaleReturn>();
            var saleReturns = await saleReturnRepo.GetByIdAsync(model.SaleReturnId);

            if (saleReturns == null) return false;
            if (model.ShipToCompanyId == 0)
            {
                var shipTo = await _unitOfWork.GetRepository<Company>()
                                              .GetQueryable()
                                              .FirstOrDefaultAsync(c =>
                                                  c.CompanyType == CompanyType.ShipTo &&
                                                  c.Name.ToLower() == "other" &&
                                                  !c.IsDeleted
                                              );
                model.ShipToCompanyId = shipTo != null ? shipTo.Id : 0;
            }
            if (model.PlatformId == 0 || model.PlatformId == null)
            {
                var plat= await _unitOfWork.GetRepository<Platform>()
                                              .GetQueryable()
                                              .FirstOrDefaultAsync(p =>
                                                  p.Name.ToLower() == "other" &&
                                                  !p.IsDeleted
                                              );
                model.PlatformId = plat != null ? plat.Id : 0;
            }
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
            BillToCompanyId = model.BillToCompanyId,
            BarCodeNo = model.BarCodeNo,
            SerialNo = serialNo,
            ReturnType = model.ReturnType,
            IsTakeInStock = model.IsTakeInStock,
            ReturnDate = model.ReturnDate,
            ReasonToReturn = model.ReasonToReturn,
            UnitId = model.UnitId,
            ReturnQuantity = model.ReturnQuantity,
            platformId = model.PlatformId,
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
        if (entity == null)
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
        entity.BillToCompanyId = model.BillToCompanyId;
        entity.BarCodeNo = model.BarCodeNo;
        entity.SerialNo = model.SerialNo;
        entity.ReturnDate = model.ReturnDate;
        entity.ReturnType = model.ReturnType;
        entity.ReasonToReturn = model.ReasonToReturn;
        entity.IsTakeInStock = model.IsTakeInStock;
        entity.UnitId = model.UnitId;
        entity.ReturnQuantity = model.ReturnQuantity;
        entity.platformId = model.PlatformId;
        entity.UpdatedBy = model.UpdatedBy;
        entity.UpdatedOn = DateTime.Now;

        saleReturnItemRepo.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteSaleReturnItem(int id, int userId)
    {
        try
        {
            var itemRepo = _unitOfWork.GetRepository<SaleReturnItems>();
            var item = await itemRepo.GetByIdAsync(id);

            if (item == null) return false;
            item.IsActive = false;
            item.IsDeleted = true;
            item.UpdatedOn = DateTime.Now;
            item.UpdatedBy = userId;

            itemRepo.Update(item);

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
        var product = await productrepo.GetByIdAsync(productId);
        if (product == null) return 0;
        return product.CategoryId;
    }
    #endregion

    #region BarcodeOperation
    public async Task<SaleReturnValidationResult> ValidateBarcodeForSaleReturnAsync(string barcodeNo)
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
        var barcodeItems = await barcodeItemRepository.FindWithIncludesAsync(
      bi => bi.BarcodeNo == barcodeNo && !bi.IsDeleted,
      CancellationToken.None,
      bi => bi.Inward,
      bi => bi.InwardItem
  );

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
                ErrorMessage = "Barcode indicates item is still in stock at Inward No: "+ (barcodeItem.Inward?.InwardNo ?? "N/A")
            };
        }

        var saleReturnItemRepo = _unitOfWork.GetRepository<SaleReturnItems>();
        var saleReturnItem = (await saleReturnItemRepo
            .FindWithIncludesAsync(
                s => s.BarCodeNo == barcodeNo && !s.IsDeleted,
                CancellationToken.None,
                s => s.SaleReturn
            )).FirstOrDefault();
     
        if (saleReturnItem !=null)
        {
            return new SaleReturnValidationResult
            {
                IsValid = false,
                ErrorMessage = "This Barcode Already Returned at Sale Return No: " + (saleReturnItem.SaleReturn?.SaleReturnNo ?? "N/A")
            };
        }
        

        var shipmentCompanyId = barcodeItem.Inward?.ShipMentCompanyId;

        var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();
        var outwardDetails = await outwardDetailRepository.FindWithIncludesAsync(
       od => od.BarcodeNo == barcodeNo && !od.IsDeleted,
       CancellationToken.None,
       od => od.Outward
   );

        bool forceUnitToPCS = false;
        var outwardDetail = outwardDetails.FirstOrDefault();
        if (outwardDetail == null && barcodeItem.ParentId != 0)
        {
            var parentBarcodeItem = (await barcodeItemRepository.FindWithIncludesAsync(
                    bi => bi.Id == barcodeItem.ParentId && !bi.IsDeleted
                ))
                ?.FirstOrDefault();

            if (parentBarcodeItem != null)
            {
                outwardDetail = (await outwardDetailRepository.FindWithIncludesAsync(
                        od => od.BarcodeNo == parentBarcodeItem.BarcodeNo && !od.IsDeleted,
                        CancellationToken.None,
                        od => od.Outward
                    ))
                    ?.FirstOrDefault();

                // force PCS for parent barcode case
                if (outwardDetail != null)
                {
                    forceUnitToPCS = true;
                //    outwardDetail.Unit = UnitType.PCS.ToString();
                }
            }
        }

        if (outwardDetail == null)
        {
            return new SaleReturnValidationResult
            {
                IsValid = false,
                ErrorMessage = "This barcode was not sold (no outward record found)."
            };
        }
        string unitName = forceUnitToPCS
           ? UnitType.PCS.ToString()
           : outwardDetail.Unit;

        var unitId = Enum.TryParse<UnitType>(unitName, true, out var unit)
            ? (int)unit
            : (int)UnitType.PCS;

        var outward = outwardDetail.Outward;
        if (outward == null)
        {
            return new SaleReturnValidationResult
            {
                IsValid = false,
                ErrorMessage = "Outward record is missing for this barcode."
            };
        }
        //if (outward == null || outward.BillToCompanyId != companyId)
        //{
        //    return new SaleReturnValidationResult
        //    {
        //        IsValid = false,
        //        ErrorMessage = "This barcode was not sold to the selected company."
        //    };
        //}
        var outwardId = outward.Id;
        var billToCompanyId = outward.BillToCompanyId;

        var productRepository = _unitOfWork.GetRepository<Product>();
        var product = await productRepository.GetByIdAsync(outwardDetail.ProductId);
        var productName = product != null ? product.SKU : "Unknown Product";

        return new SaleReturnValidationResult
        {
            IsValid = true,
            ProductName = productName,
            ProductId = outwardDetail.ProductId,
            ShipToId = shipmentCompanyId,
            BillToId = billToCompanyId,
            UnitId = unitId,
            OutwardId = outwardId,
            BarcodeNo = barcodeNo,
            PlatformId = outward.PlatformId

        };
    }
    private async Task<SaleToInwardDto> ConvertToDto(SaleReturnItems item)
    {
        var result = new SaleToInwardDto()
        {
            ProductId = item.ProductId,
            CategoryId = item.CategoryId,
            ShipToCompanyId = item.ShipToCompanyId,
            ReturnDate = item.ReturnDate ?? DateTime.Now,
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

