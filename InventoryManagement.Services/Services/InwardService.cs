using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.DTO;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace InventoryManagement.Services;

public class InwardService : IInwardService
{
    private readonly IUnitOfWork _unitOfWork;

    public InwardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    #region Public Methods
    public async Task<List<InwardModel>> GetAllAsync(bool isAdmin)
    {
        var inwardRepo = _unitOfWork.GetRepository<Inward>();
        var query = inwardRepo.GetQueryable()
            .Include(i => i.ShipMentCompany)
            .Include(i => i.Category)
            .Where(i => !i.IsDeleted);

        if (!isAdmin)
        {
            query = query.Where(i => i.CreatedOn.Date == DateTime.Now.Date);
        }

        var inwards = await query
            .OrderByDescending(i => i.InwardDate)
            .AsNoTracking()
            .ToListAsync();

        return inwards.Select(MapToModel).ToList();
    }

    public async Task<InwardModel?> GetByIdAsync(int id)
    {
        var inwardRepo = _unitOfWork.GetRepository<Inward>();
        var entity = await inwardRepo.GetByIdAsync(id);
        if (entity == null) return null;

        var model = MapToModel(entity);
        model.InwardItems = await GetInwardItemsByInwardIdAsync(id);
        return model;
    }

    public async Task<int> CreateAsync(InwardModel model)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            if (string.IsNullOrEmpty(model.InwardNo))
            {
                model.InwardNo = await GenerateInwardNoAsync(model.InwardDate, model.IsSalesReturn);
            }

            var inward = await CreateInwardEntityAsync(model);
            await CreateInwardItemsWithBarcodesAsync(inward.Id, model.InwardItems, inward.InwardDate);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return inward.Id;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            Console.WriteLine($"Error creating inward: {ex.Message}");
            return 0;
        }
    }

    public async Task<bool> UpdateAsync(InwardModel model)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var inwardRepo = _unitOfWork.GetRepository<Inward>();
            var entity = await inwardRepo.GetByIdAsync(model.Id);
            if (entity == null) return false;

            UpdateInwardEntity(entity, model);
            await ReplaceInwardItemsAsync(entity.Id, model.InwardItems, entity.InwardDate, entity.UpdatedBy);

            inwardRepo.Update(entity);
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

    public async Task<bool> DeleteAsync(int id, int deletedBy)
    {
        try
        {
            var repo = _unitOfWork.GetRepository<Inward>();
            var entity = await repo.GetByIdAsync(id);
            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.IsActive = false;
            repo.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<InwardItemModel>> GetInwardItemsByInwardIdAsync(int inwardId)
    {
        var itemRepo = _unitOfWork.GetRepository<InwardItem>();

        var query = itemRepo.GetQueryable();

        var items = await query.Include(a => a.InwardBarcodeItems).Include(a => a.Product)
            .Where(ii => ii.InwardId == inwardId && !ii.IsDeleted).OrderByDescending(i => i.Id).ThenByDescending(i => i.CreatedOn)
            .ToListAsync();


        return items.Select(MapItemToModel).ToList();
    }


    public async Task<bool> CreateInwardItemAsync(InwardItemModel model)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var inwardRepo = _unitOfWork.GetRepository<Inward>();
            var inward = await inwardRepo.GetByIdAsync(model.InwardId);
            if (inward == null) return false;

            var entity = await CreateInwardItemEntityAsync(model);
            await CreateBarcodesForSingleItemAsync(entity, inward.InwardDate);

            await _unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            Console.WriteLine($"Error creating inward item: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateInwardItemAsync(InwardItemModel model)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>();
            var entity = await inwardItemRepo.GetByIdAsync(model.Id);

            var inwardRepo = _unitOfWork.GetRepository<Inward>();
            var inward = await GetByIdAsync(model.InwardId);

            if (entity == null) return false;
            if (inward == null)
                throw new Exception("Related Inward record not found.");

            // Update common fields
            entity.ProductId = model.ProductId;
            entity.BrandId = model.BrandId;
            entity.UpdatedBy = model.UpdatedBy ?? 0;
            entity.UpdatedOn = model.UpdatedOn ?? DateTime.Now;
            entity.InwardUnitId = model.InwardUnitId;
            entity.InwardUnitName = model.InwardUnitName;
            entity.ItemQuantity = model.ItemQuantity;
            entity.BoxQuantity = model.BoxQuantity;
            if (entity.InwardUnitId == (int)UnitType.PCS)
                entity.BoxQuantity = 0;

            if (model.ItemQuantity > 0)
            {
                await DeleteBarcodesForItemAsync(entity.Id, entity.UpdatedBy);
                await CreateBarcodesForSingleItemAsync(entity, inward.InwardDate);
            }
            entity.InwardUnitId = model.InwardUnitId;

            inwardItemRepo.Update(entity);

            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            Console.WriteLine($"Error updating inward item: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteInwardItemAsync(int id, int userId)
    {
        try
            {
            await DeleteBarcodesForItemAsync(id, userId);

            var itemRepo = _unitOfWork.GetRepository<InwardItem>();
            var item = await itemRepo.GetByIdAsync(id);

            item.IsActive = false;
            item.IsDeleted = true;
            item.UpdatedOn = DateTime.Now;
            item.UpdatedBy = userId;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error Deleting inward item: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AddReturnItemToSaleInwardAsync(SaleToInwardDto saleReturnItems)
    {
        try
        {
            var existingReturnInward = await GetSaleReturnInwardAsync(saleReturnItems);
            var inwardId = existingReturnInward?.Id
                           ?? await CreateSalesReturnInwardAsync(saleReturnItems);

            if (inwardId == 0)
                return false;

            var itemModel = await ConvertDtoToItemModel(inwardId, saleReturnItems);
            return await SaveSalesReturnInwardItemAsync(itemModel);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error Adding inward item: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteSalesReturnInwardItemAsync(SaleToInwardDto saleReturnItems)
    {
        try
        {
            var existingReturnInward = await GetSaleReturnInwardAsync(saleReturnItems);

            if (existingReturnInward == null)
                return false;

            var model = await ConvertDtoToItemModel(existingReturnInward.Id, saleReturnItems);
                model.UpdatedBy = saleReturnItems.UserId;
            return await DeleteSaleReturnInwardItemAsync(model);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting inward item: {ex.Message}");
            return false;
        }
    }

    public async Task<byte[]> GenerateItemBarcodePrnAsync(int inwardItemId,int userId)
    {
        var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();
        var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>();

        var query = barcodeRepo.GetQueryable()
         .Where(b => b.InwardItemId == inwardItemId && !b.IsDeleted && b.IsActive);

        var unprintedBarcodes = await query
                                     .Where(b => !b.IsPrinted)
                                     .OrderBy(b => b.BarcodeNo)
                                     .ToListAsync();

        List<InwardBarcodeItem> barcodeItems;
        if (unprintedBarcodes.Any())
        {
            barcodeItems = unprintedBarcodes;
        }
        else
        {
            barcodeItems = await query
                .OrderBy(b => b.BarcodeNo)
                .ToListAsync();
        }
        if (!barcodeItems.Any())
            return null;

        var item = await inwardItemRepo.GetQueryable()
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == inwardItemId);

        if (item == null)
            return null;

        var productName = item.Product?.SKU ?? "N/A";
        var batchNo = item.BatchNo;

        var sb = new StringBuilder();

        AppendPrnHeader(sb);

        for (int i = 0; i < barcodeItems.Count; i += 2)
        {
            sb.AppendLine("CLS");
            AddBarcodeLabel(
                sb,
                barcodeItems[i],
                productName,
                BatchNo(barcodeItems[i], item, batchNo),
                TotalQty(barcodeItems[i], item, item.ItemQuantity),
                BarcodePositionType.Left);

            if (i + 1 < barcodeItems.Count)
            {
                AddBarcodeLabel(
                    sb,
                    barcodeItems[i + 1],
                    productName,
                    BatchNo(barcodeItems[i + 1], item, batchNo),
                    TotalQty(barcodeItems[i + 1], item, item.ItemQuantity),
                    BarcodePositionType.Right);
            }

            sb.AppendLine("PRINT 1,1");
        }

        // Mark printed
        foreach (var barcode in barcodeItems)
        {
            barcode.IsPrinted = true;
            barcode.PrintedBy = userId;
            barcodeRepo.Update(barcode);
        }

        await _unitOfWork.SaveChangesAsync();
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
    public async Task<byte[]> GenerateSingleBarcodePrnAsync(string barcodeNo, int userId)
    {
        try
        {
            var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();
            var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>();

            var barcode = await barcodeRepo.GetQueryable()
                .FirstOrDefaultAsync(b =>
                    b.BarcodeNo == barcodeNo &&
                    !b.IsDeleted &&
                    b.IsActive);

            if (barcode == null)
                return null;

            var item = await inwardItemRepo.GetQueryable()
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == barcode.InwardItemId);

            if (item == null)
                return null;

            var sb = new StringBuilder();
            AppendPrnHeader(sb);

            sb.AppendLine("CLS");
            AddBarcodeLabel(
                sb,
                barcode,
                item.Product?.SKU ?? "N/A",
                BatchNo(barcode, item, item.BatchNo),
                TotalQty(barcode, item, item.ItemQuantity),
                BarcodePositionType.Left);

            sb.AppendLine("PRINT 1,1");

            // Mark barcode as printed
            barcode.IsPrinted = true;
            barcode.PrintedBy = userId;

            barcodeRepo.Update(barcode);

            await _unitOfWork.SaveChangesAsync();
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
        catch (Exception e)
        {
            Console.WriteLine("Error in GenerateSingleBarcodePrnAsync service" + e.Message);
            return null;
        }

    }
    public async Task<BarcodeValidationResult> ValidateBarcodeForReprintAsync(string barcodeNo)
    {
        try
        {
            var barcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();

            var barcodeItem = await barcodeItemRepository
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(bi => bi.BarcodeNo == barcodeNo && !bi.IsDeleted);

            if (barcodeItem == null)
            {
                return new BarcodeValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid barcode. Barcode does not exist in inventory."
                };
            }

            if (!barcodeItem.IsInStock)
            {
                return new BarcodeValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "This barcode has already been sold."
                };
            }

            return new BarcodeValidationResult { IsValid = true };
        }
        catch (Exception e)
        {
            throw;
        }
    }

    private static bool IsParentBarcode(InwardBarcodeItem barcode, InwardItem inward)
    {
        return (barcode.ParentId == null || barcode.ParentId == 0)
            && inward.InwardUnitId == (int)UnitType.BOX
            && inward.BoxQuantity > 0;
    }
    private static string BatchNo(InwardBarcodeItem barcode, InwardItem item, string batchNo)
    {
        return IsParentBarcode(barcode, item) ? batchNo : string.Empty;
    }
    private static string TotalQty(InwardBarcodeItem barcode, InwardItem item, decimal totalQty)
    {
        return IsParentBarcode(barcode, item) ? $"QTY: {totalQty.ToString()}" : string.Empty;
    }
    private static void AppendPrnHeader(StringBuilder sb)
    {
        sb.AppendLine("SIZE 108 mm, 25 mm");
        sb.AppendLine("DIRECTION 0,0");
        sb.AppendLine("REFERENCE 0,0");
        sb.AppendLine("OFFSET 0 mm");
        sb.AppendLine("SET PEEL OFF");
        sb.AppendLine("SET CUTTER OFF");
        sb.AppendLine("SET PARTIAL_CUTTER OFF");
        sb.AppendLine("SET TEAR ON");
    }

    private static class BarcodeLayout
    {
        public static readonly BarcodeLayoutDto Left = new BarcodeLayoutDto
        {
            BarcodeX = 820,
            BarcodeTextX = 728,
            ProductTextX = 820,
            DateTextX = 656,
            BatchTextX = 820,
            TotaQtyX = 570
        };

        public static readonly BarcodeLayoutDto Right = new BarcodeLayoutDto
        {
            BarcodeX = 420,
            BarcodeTextX = 305,
            ProductTextX = 420,
            DateTextX = 256,
            BatchTextX = 420,
            TotaQtyX = 170
        };
    }

    /// <summary>
    /// barcode label with dynamic data to the PRN content
    /// </summary>
    private void AddBarcodeLabel(StringBuilder sb, InwardBarcodeItem barcodeItem, string productName, string batchNo, string totalQty, BarcodePositionType position)
    {
        var date = DateTime.Now;

        var layout = position == BarcodePositionType.Left ? BarcodeLayout.Left : BarcodeLayout.Right;

        sb.AppendLine(
            $"BARCODE {layout.BarcodeX},159,\"128M\",75,0,180,3,6,\"!105{barcodeItem.BarcodeNo}\"");

        sb.AppendLine(
            $"TEXT {layout.BarcodeTextX},80,\"ROMAN.TTF\",180,1,8,\"{barcodeItem.BarcodeNo}\"");

        sb.AppendLine(
            $"TEXT {layout.ProductTextX},43,\"ROMAN.TTF\",180,1,8,\"{productName}\"");

        if (!string.IsNullOrEmpty(batchNo))
        {
            sb.AppendLine(
                $"TEXT {layout.BatchTextX},188,\"ROMAN.TTF\",180,0,8,\"BATCH: {batchNo}\"");
        }

        if (!string.IsNullOrEmpty(totalQty))
        {
            sb.AppendLine(
                $"TEXT {layout.TotaQtyX},43,\"ROMAN.TTF\",180,1,9,\"{totalQty}\"");
        }

        sb.AppendLine(
            $"TEXT {layout.DateTextX},188,\"0\",180,0,8,\"DT:{date:dd-MM-yyyy hh:mm tt}\"");
    }
    #endregion

    #region Inward Entity Operations

    private async Task<Inward> CreateInwardEntityAsync(InwardModel model)
    {
        var inwardRepo = _unitOfWork.GetRepository<Inward>();
        var inward = new Inward
        {
            InwardNo = model.InwardNo ?? string.Empty,
            InwardDate = model.InwardDate,
            ShipMentCompanyId = model.ShipMentCompanyId,
            CategoryId = model.CategoryId,
            Remarks = model.Remarks,
            IsActive = model.IsActive,
            IsDeleted = false,
            CreatedBy = model.CreatedBy,
            CreatedOn = model.CreatedOn,
            IsSalesReturn = model.IsSalesReturn
        };

        await inwardRepo.AddAsync(inward);
        await _unitOfWork.SaveChangesAsync();
        return inward;
    }

    private void UpdateInwardEntity(Inward entity, InwardModel model)
    {
        entity.InwardNo = model.InwardNo;
        entity.InwardDate = model.InwardDate;
        entity.ShipMentCompanyId = model.ShipMentCompanyId;
        entity.CategoryId = model.CategoryId;
        entity.Remarks = model.Remarks;
        entity.IsActive = model.IsActive;
        entity.UpdatedBy = model.UpdatedBy ?? 0;
        entity.UpdatedOn = model.UpdatedOn ?? DateTime.Now;
    }

    #endregion

    #region Inward Item Operations

    private async Task CreateInwardItemsWithBarcodesAsync(int inwardId, List<InwardItemModel> itemModels, DateTime transactionDate)
    {
        if (!itemModels.Any()) return;

        var items = await CreateInwardItemEntitiesAsync(inwardId, itemModels);
        await CreateBarcodesForItemsAsync(items, transactionDate);
    }

    private async Task ReplaceInwardItemsAsync(int inwardId, List<InwardItemModel> newItems, DateTime transactionDate, int userId)
    {
        await DeleteExistingInwardItemsAsync(inwardId, userId);
        await CreateInwardItemsWithBarcodesAsync(inwardId, newItems, transactionDate);
    }

    private async Task<List<InwardItem>> CreateInwardItemEntitiesAsync(int inwardId, List<InwardItemModel> itemModels)
    {
        var itemRepo = _unitOfWork.GetRepository<InwardItem>();
        var serialNo = await itemRepo.CountAsync();
        var items = new List<InwardItem>();

        foreach (var itemModel in itemModels)
        {
            serialNo++;
            items.Add(new InwardItem
            {
                InwardId = inwardId,
                BrandId = itemModel.BrandId,
                ProductId = itemModel.ProductId,
                ItemQuantity = itemModel.ItemQuantity,
                InwardUnitId = itemModel.InwardUnitId,
                InwardUnitName = itemModel.InwardUnitName,
                SerialNo = serialNo.ToString(),
                BatchNo = GenerateBatchNo(serialNo),
                IsDeleted = false,
                CreatedBy = itemModel.CreatedBy,
                BoxQuantity = itemModel.BoxQuantity
            });
        }

        await itemRepo.AddRangeAsync(items);
        await _unitOfWork.SaveChangesAsync();
        return items;
    }

    private async Task<InwardItem> CreateInwardItemEntityAsync(InwardItemModel model)
    {
        var itemRepo = _unitOfWork.GetRepository<InwardItem>();
        var serialNo = await itemRepo.CountAsync() + 1;

        var entity = new InwardItem
        {
            InwardId = model.InwardId,
            ProductId = model.ProductId,
            ItemQuantity = model.ItemQuantity,
            InwardUnitId = model.InwardUnitId,
            InwardUnitName = model.InwardUnitName,
            SerialNo = serialNo.ToString(),
            BatchNo = GenerateBatchNo(serialNo),
            IsDeleted = false,
            CreatedBy = model.CreatedBy,
            CreatedOn = model.CreatedOn,
            BoxQuantity = model.BoxQuantity,
            BrandId = model.BrandId
        };

        await itemRepo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return entity;
    }

    private async Task DeleteExistingInwardItemsAsync(int inwardId, int userId)
    {
        var itemRepo = _unitOfWork.GetRepository<InwardItem>();
        var existingItems = await itemRepo.FindAsync(i => i.InwardId == inwardId);

        foreach (var item in existingItems)
        {
            await DeleteBarcodesForItemAsync(item.Id, userId);
            item.IsActive = false;
            item.IsDeleted = true;
            item.UpdatedBy = userId;
            item.UpdatedOn = DateTime.Now;
            itemRepo.Update(item);

        }
    }
    private async Task<int> GetBrandIdByProductId(int productId)
    {
        var productRepo = _unitOfWork.GetRepository<Product>();
        var product = await productRepo.GetByIdAsync(productId);
        if (product == null) return 0;

        return product.MakeCompanyId;
    }
    #endregion
    #region SaleReturn

    private async Task<int> CreateSalesReturnInwardAsync(SaleToInwardDto saleReturnItems)
    {
        try
        {
            var inwardModel = new InwardModel()
            {
                InwardDate = saleReturnItems.ReturnDate,
                CategoryId = saleReturnItems.CategoryId,
                ShipMentCompanyId = saleReturnItems.ShipToCompanyId,
                IsActive = true,
                CreatedBy = saleReturnItems.CreatedBy,
                CreatedOn = saleReturnItems.CreatedOn,
                UpdatedBy = saleReturnItems.UpdatedBy,
                UpdatedOn = saleReturnItems.UpdatedOn,
                IsSalesReturn = true

            };

            if (string.IsNullOrEmpty(inwardModel.InwardNo))
            {
                inwardModel.InwardNo = await GenerateInwardNoAsync(inwardModel.InwardDate, inwardModel.IsSalesReturn);
            }

            var inward = await CreateInwardEntityAsync(inwardModel);
            //   await CreateInwardItemsWithBarcodesAsync(inward.Id, inwardModel.InwardItems, inward.InwardDate);

            return inward.Id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating SaleReturn Inward : {ex.Message}");
            throw;
        }

    }

    private async Task<bool> CreateSalesReturnInwardItemAsync(InwardItemModel model)
    {
        try
        {
            var entity = await CreateInwardItemEntityAsync(model);
            
            if (entity == null)
                return false;

            try
            {
                await CreateBarcodesForSingleItemAsync(entity, entity.CreatedOn);
            }
            catch
            {
                // rollback inward item
                var repo = _unitOfWork.GetRepository<InwardItem>();

                entity.IsDeleted = true;
                entity.IsActive = false;
                entity.UpdatedOn = DateTime.Now;
                entity.UpdatedBy = model.CreatedBy;

                repo.Update(entity);
                await _unitOfWork.SaveChangesAsync();

                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating SaleReturn Inward Items: {ex.Message}");
            return false;
        }   
    }

    private async Task<bool> UpdateSaleReturnInwardItemAsync(InwardItem existingItem, InwardItemModel model, bool isDeleting)
    {

        var delta = isDeleting ? -model.ItemQuantity : model.ItemQuantity;
        var newQuantity = existingItem.ItemQuantity + delta;

        if (newQuantity < 0)
            throw new Exception("Inward item quantity cannot be negative.");

        if (newQuantity == 0)
        {
            return await DeleteInwardItemAsync(
                existingItem.Id,
                model.UpdatedBy ?? 0);
        }

        try
        {
            bool result = isDeleting
                       ? await DeleteBarcodeOfSaleReturnInward(existingItem, model.ItemQuantity, model.UpdatedBy ?? 0)
                       : await CreateAdditionalBarcodesAsync(existingItem, model.ItemQuantity, DateTime.Now);

            if (!result)
                return false;

            var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>();

            // Only update quantity AFTER barcode success
            existingItem.ItemQuantity = newQuantity;
            existingItem.UpdatedBy = model.UpdatedBy ?? 0;
            existingItem.UpdatedOn = DateTime.Now;

            if (existingItem.InwardUnitId == (int)UnitType.PCS)
                existingItem.BoxQuantity = 0;

            inwardItemRepo.Update(existingItem);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Barcode operation failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> SaveSalesReturnInwardItemAsync(InwardItemModel model)
    {
        var existingItem = await GetExistingItemAsync(model);

        if (existingItem == null || existingItem.InwardUnitId == (int)UnitType.BOX)
        {
            return await CreateSalesReturnInwardItemAsync(model);
        }
        return await UpdateSaleReturnInwardItemAsync(existingItem, model, false);
    }

    private async Task<bool> DeleteSaleReturnInwardItemAsync(InwardItemModel model)
    {
        var existingItem = await GetExistingItemAsync(model);

        if (existingItem == null)
            return false;

        if (existingItem.ItemQuantity == model.ItemQuantity &&
            existingItem.InwardUnitId == model.InwardUnitId)
        {
            return await DeleteInwardItemAsync(existingItem.Id, model.UpdatedBy ?? 0);
        }
        return await UpdateSaleReturnInwardItemAsync(existingItem, model, true);
    }

    private async Task<InwardItem?> GetExistingItemAsync(InwardItemModel model)
    {
        var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>();

        var query = inwardItemRepo.GetQueryable().Where(i =>
            !i.IsDeleted &&
            i.InwardId == model.InwardId &&
            i.ProductId == model.ProductId &&
            i.InwardUnitId == model.InwardUnitId);

        if (model.InwardUnitId == (int)UnitType.BOX)
            query = query.Where(i => i.ItemQuantity == model.ItemQuantity);

        return await query.FirstOrDefaultAsync();
    }

    private async Task<Inward?> GetSaleReturnInwardAsync(SaleToInwardDto saleReturnItems)
    {
        var inwardRepo = _unitOfWork.GetRepository<Inward>();
        var date = saleReturnItems.ReturnDate.Date;

        var existingReturnInward = await inwardRepo.GetQueryable()
            .Include(i => i.InwardItems)
            .FirstOrDefaultAsync(i =>
                i.IsSalesReturn &&
                i.InwardDate.Date == date &&
                i.ShipMentCompanyId == saleReturnItems.ShipToCompanyId &&
                i.CategoryId == saleReturnItems.CategoryId);

        return existingReturnInward;
    }
    #endregion

    #region Barcode Operations

    private async Task CreateBarcodesForItemsAsync(List<InwardItem> items, DateTime transactionDate)
    {
        var parentBarcodes = new List<InwardBarcodeItem>();
        var childBarcodes = new List<InwardBarcodeItem>();
        var parentId = parentBarcodes.Any() ? parentBarcodes.First().Id : 0;
        int barcodeCounter = 0;

        foreach (var item in items)
        {
            if (item.InwardUnitId == (int)UnitType.BOX)
            {
                for (int i = 0; i < item.BoxQuantity; i++)
                {
                    barcodeCounter++;
                    parentBarcodes.Add(CreateBarcodeEntity(item, barcodeCounter, transactionDate, 0));
                }
                if (parentBarcodes.Any())
                {
                    var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();
                    await barcodeRepo.AddRangeAsync(parentBarcodes);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            for (int i = 0; i < item.ItemQuantity; i++)
            {
                barcodeCounter++;
                childBarcodes.Add(CreateBarcodeEntity(item, barcodeCounter, transactionDate, parentId));
            }
        }

        if (childBarcodes.Any())
        {
            var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();
            await barcodeRepo.AddRangeAsync(childBarcodes);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private async Task CreateBarcodesForSingleItemAsync(InwardItem item, DateTime transactionDate)
    {
        try
        {
            var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();

            // Get all existing barcodes for this inward to find the highest counter
            var existingBarcodes = await barcodeRepo.FindAsync(b =>
            b.InwardId == item.InwardId &&
            b.TransactionDate.Date == transactionDate.Date);

            int maxCounter = 1; // Default start

            if (existingBarcodes != null && existingBarcodes.Any())
            {
                maxCounter = existingBarcodes
                               .Select(b => int.Parse(b.BarcodeNo[^6..]))
                               .Max() + 1;
            }
                
            var parentBarcodes = new List<InwardBarcodeItem>();

            if (item.InwardUnitId == (int)UnitType.BOX)
            {
                for (int i = 0; i < item.BoxQuantity; i++)
                {
                    parentBarcodes.Add(CreateBarcodeEntity(item, maxCounter, transactionDate, 0));
                    maxCounter++;
                }
                if (parentBarcodes.Any())
                {
                    await barcodeRepo.AddRangeAsync(parentBarcodes);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            var childBarcodes = new List<InwardBarcodeItem>();
            var parentId = parentBarcodes.Any() ? parentBarcodes.First().Id : 0;
            for (int i = 0; i < item.ItemQuantity; i++)
            {
                childBarcodes.Add(CreateBarcodeEntity(item, maxCounter, transactionDate, parentId));
                maxCounter++;
            }

            if (childBarcodes.Any())
            {
                await barcodeRepo.AddRangeAsync(childBarcodes);
                await _unitOfWork.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // Log the exception properly
            Console.WriteLine($"Error creating barcodes: {ex.Message}");
            throw; // Re-throw to handle in calling method
        }
    }

    private async Task DeleteBarcodesForItemAsync(int itemId, int userId)
    {
        var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();
        var barcodes = await barcodeRepo.FindAsync(b => b.InwardItemId == itemId && !b.IsDeleted);

        if (barcodes == null || !barcodes.Any())
            return;

        foreach (var barcode in barcodes)
        {
            barcode.IsActive = false;
            barcode.IsInStock = false;
            barcode.IsDeleted = true;
            barcode.UpdatedOn = DateTime.Now;
            barcode.UpdatedBy = userId;
            barcodeRepo.Update(barcode);

        }
    }

    public async Task<bool> DeleteSingleBarcode(string barcode,int userId)
    {
        var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();
        var singleBarcode = await barcodeRepo
            .GetQueryable()
            .FirstOrDefaultAsync(b => b.BarcodeNo == barcode && !b.IsDeleted);
     
        if (singleBarcode == null) return false;
     
        singleBarcode.IsActive = false;
        singleBarcode.IsInStock = false;
        singleBarcode.IsDeleted = true;
        singleBarcode.UpdatedOn = DateTime.Now;
        singleBarcode.UpdatedBy = userId;
    
        barcodeRepo.Update(singleBarcode);
        await _unitOfWork.SaveChangesAsync();

        return true;

    }

    private async Task<bool> DeleteBarcodeOfSaleReturnInward(InwardItem item, decimal deletingQty, int userId)
    {
        try
        {
            var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();

            var barcodesToDelete = await barcodeRepo
                .GetQueryable()
                .Where(b => b.InwardItemId == item.Id &&
                            !b.IsDeleted &&
                            b.IsInStock)
                .OrderByDescending(b => b.Id)
                .Take((int)deletingQty)
                .ToListAsync();

            if (!barcodesToDelete.Any())
                return false;

            foreach (var barcode in barcodesToDelete)
            {
                barcode.IsActive = false;
                barcode.IsInStock = false;
                barcode.IsDeleted = true;
                barcode.UpdatedOn = DateTime.Now;
                barcode.UpdatedBy = userId;
            }

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting barcodes: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> CreateAdditionalBarcodesAsync(InwardItem item, decimal qty, DateTime transactionDate)
    {
        try
        {
            var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();

            var existingBarcodes = await barcodeRepo.FindAsync(b =>
                                   b.InwardId == item.InwardId);

            int maxCounter = 1; // Default start

            if (existingBarcodes != null && existingBarcodes.Any())
            {
                maxCounter = existingBarcodes
                               .Select(b => int.Parse(b.BarcodeNo[^6..]))
                               .Max() + 1;
            }

            var newBarcodes = new List<InwardBarcodeItem>();

            for (int i = 0; i < (int)qty; i++)
            {
                newBarcodes.Add(CreateBarcodeEntity(item, maxCounter, transactionDate, 0));
                maxCounter++;
            }

            await barcodeRepo.AddRangeAsync(newBarcodes);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating additional barcodes: {ex.Message}");
            return false;
        }
    }

    private InwardBarcodeItem CreateBarcodeEntity(InwardItem item, int counter, DateTime transactionDate, int parentId)
    {
        return new InwardBarcodeItem
        {
            InwardId = item.InwardId,
            CreatedBy = item.CreatedBy,
            InwardItemId = item.Id,
            BarcodeNo = GenerateBarcodeNumber(transactionDate, item.InwardId, counter),
            TransactionDate = transactionDate,
            IsInStock = true,
            ParentId = parentId
        };
    }

    #endregion

    #region Number Generators

    private async Task<string> GenerateInwardNoAsync(DateTime inwardDate, bool isSaleReturn)
    {
        var inwardRepo = _unitOfWork.GetRepository<Inward>();
        var (startYear, endYear) = GetFinancialYear(inwardDate);
        var count = await inwardRepo.CountAsync();
        count++;
        var prefix = isSaleReturn ? "SRIN" : "IN";
        return $"{prefix}-{startYear % 100}-{endYear % 100}/{count}";
    }

    private string GenerateBarcodeNumber(DateTime TransactionDate, int inwardId, int counter)
    {
        string datePart = TransactionDate.ToString("ddMMyy");

        return $"{datePart}{inwardId:D4}{counter:D6}";
    }

    private string GenerateBatchNo(int serialNo)
    {
        return $"1{serialNo:D3}";
    }

    private (int startYear, int endYear) GetFinancialYear(DateTime date)
    {
        int year = date.Year;
        if (date.Month < 4)
            return (year - 1, year);
        else
            return (year, year + 1);
    }

    #endregion

    #region Mapping Methods

    private InwardModel MapToModel(Inward entity)
    {
        return new InwardModel
        {
            Id = entity.Id,
            InwardNo = entity.InwardNo,
            InwardDate = entity.InwardDate,
            ShipMentCompanyId = entity.ShipMentCompanyId,
            Remarks = entity.Remarks,
            ShipMentCompanyName = entity.ShipMentCompany?.Name ?? string.Empty,
            IsSalesReturn = entity.IsSalesReturn,
            IsActive = entity.IsActive,
            CategoryId = entity.CategoryId,
            CreatedBy = entity.CreatedBy,
        };
    }

    private InwardItemModel MapItemToModel(InwardItem entity)
    {
        return new InwardItemModel
        {
            Id = entity.Id,
            InwardId = entity.InwardId,
            BrandId = entity.BrandId,
            ProductId = entity.ProductId,
            ItemQuantity = entity.ItemQuantity,
            InwardUnitId = entity.InwardUnitId,
            InwardUnitName = entity.InwardUnitName,
            BatchNo = entity.BatchNo,
            CreatedBy = entity.CreatedBy,
            BoxQuantity = entity.BoxQuantity,
            ProductSearchText = entity.Product.SKU,
            HasOutOfStockBarcodes = entity.InwardBarcodeItems.Any(b => !b.IsInStock && !b.IsDeleted)
        };
    }

    private async Task<InwardItemModel> ConvertDtoToItemModel(int inwardId, SaleToInwardDto dto)
    {
        var brandId = await GetBrandIdByProductId(dto.ProductId);

        var unitName = CommonUtils.UnitList
            .FirstOrDefault(u => u.Id == dto.UnitId)?.Name ?? string.Empty;

        var model = new InwardItemModel
        {
            InwardId = inwardId,
            ProductId = dto.ProductId,
            BrandId = brandId,
            InwardUnitId = dto.UnitId,
            InwardUnitName = unitName,
            CreatedBy = dto.CreatedBy,
            CreatedOn = dto.CreatedOn,
            UpdatedBy = dto.UpdatedBy,
            UpdatedOn = dto.UpdatedOn
        };
        // Quantity logic (BOX vs PCS)
        if (dto.UnitId == (int)UnitType.BOX)
        {
            model.BoxQuantity = 1;
            model.ItemQuantity = dto.ReturnQuantity;
        }
        else
        {
            model.BoxQuantity = 0;
            model.ItemQuantity = dto.ReturnQuantity;
        }
        return model;
    }
    #endregion
}