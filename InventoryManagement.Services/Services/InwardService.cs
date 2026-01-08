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
                model.InwardNo = await GenerateInwardNoAsync(model.InwardDate,model.IsSalesReturn);
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
            await ReplaceInwardItemsAsync(entity.Id, model.InwardItems, entity.InwardDate);

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
            .Where(ii => ii.InwardId == inwardId && !ii.IsDeleted)
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
            entity.UpdatedOn = model.UpdatedOn ?? DateTime.UtcNow;
            entity.InwardUnitId = model.InwardUnitId;
            entity.InwardUnitName = model.InwardUnitName;
            entity.ItemQuantity = model.ItemQuantity;
            entity.BoxQuantity = model.BoxQuantity;
            if (entity.InwardUnitId == (int)UnitType.PCS)
                entity.BoxQuantity = 0;

            if (model.ItemQuantity > 0)
            {
                await DeleteBarcodesForItemAsync(entity.Id);
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

    public async Task<bool> DeleteInwardItemAsync(int id)
    {
        try
        {
            await DeleteBarcodesForItemAsync(id);

            var itemRepo = _unitOfWork.GetRepository<InwardItem>();
            await itemRepo.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error Deleting inward item: {ex.Message}");
            return false;
        }
    }

    public async Task<int> AddReturnedItemToExistingInwardAsync(ReturnItemDto parameter)
    {
        var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>();
        var inwardRepo = _unitOfWork.GetRepository<Inward>();

        try
        {
            var inward = await inwardRepo.GetByIdAsync(parameter.InwardId);
            if (inward == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Inward {parameter.InwardId} not found.");
            }

            var serialNo = await inwardItemRepo.CountAsync() + 1;
            var inwardItem = new InwardItem
            {
                InwardId = inward.Id,
                ProductId = parameter.ProductId,
                InwardUnitId = parameter.UnitId,

                InwardUnitName = parameter.UnitName,
                ItemQuantity = parameter.ItemQuantity,
                BoxQuantity = parameter.BoxQuantity,
                SerialNo = serialNo.ToString(),
                BatchNo = GenerateBatchNo(serialNo),
                IsDeleted = false,

                CreatedBy = parameter.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            await inwardItemRepo.AddAsync(inwardItem);
            await _unitOfWork.SaveChangesAsync();

            await CreateBarcodesForSingleItemAsync(inwardItem, inward.InwardDate);

            await _unitOfWork.SaveChangesAsync();

            return inwardItem.Id;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
    public async Task<bool> AddReturnItemToSaleInwardAsync(SaleToInwardDto saleReturnItems)
    {
        try
        {
            var inwardRepo = _unitOfWork.GetRepository<Inward>();

            var Date = saleReturnItems.ReturnDate.Date;
            var existingReturnInward = await inwardRepo.GetQueryable()
                .FirstOrDefaultAsync(i =>
                    i.IsSalesReturn &&
                    i.InwardDate.Date == Date &&
                    i.ShipMentCompanyId == saleReturnItems.ShipToCompanyId &&
                    i.CategoryId == saleReturnItems.CategoryId);

            var inwardId = existingReturnInward?.Id
                           ?? await CreateSalesReturnInwardAsync(saleReturnItems);

            if (inwardId == 0)
                return false;

            await CreateSalesReturnInwardItemAsync(inwardId, saleReturnItems);

            return true;
        }
        catch (Exception ex)
        {
            // TODO: log exception
            return false;
        }
    }
    public async Task<bool> DeleteSalesReturnInwardItemAsync(SaleToInwardDto saleReturnItems)
    {
        try
        {
            var inwardRepo = _unitOfWork.GetRepository<Inward>();
            var date = saleReturnItems.ReturnDate.Date;

            var existingReturnInward = await inwardRepo.GetQueryable()
                .FirstOrDefaultAsync(i =>
                    i.IsSalesReturn &&
                    i.InwardDate.Date == date &&
                    i.ShipMentCompanyId == saleReturnItems.ShipToCompanyId &&
                    i.CategoryId == saleReturnItems.CategoryId);

            var inwardId = existingReturnInward?.Id ?? 0;

            if (inwardId == 0)
                return false;
            await FindSalesReturnInwardAsync(inwardId, saleReturnItems);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting inward item: {ex.Message}");
            throw;
        }
    }
    public async Task RemoveReturnedReturnedItemFromStockAsync(int inwardItemId)
    {
        var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>();
        var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();

        try
        {
            var inwardItem = await inwardItemRepo.GetByIdAsync(inwardItemId);
            if (inwardItem == null)
            {
                return;
            }

            var barcodes = await barcodeRepo.FindAsync(b =>
                b.InwardItemId == inwardItemId && !b.IsDeleted);

            foreach (var barcode in barcodes)
            {
                barcode.IsDeleted = true;
                barcode.IsInStock = false;
                barcodeRepo.Update(barcode);
            }

            inwardItem.IsDeleted = true;
            inwardItemRepo.Update(inwardItem);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            throw;
        }
    }

    public async Task<byte[]> GenerateItemBarcodePrnAsync(int inwardItemId)
    {
        var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();
        var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>();

        var barcodeItems = (await barcodeRepo.FindAsync(
                b => b.InwardItemId == inwardItemId && !b.IsDeleted && b.IsActive))
                .OrderBy(b => b.BarcodeNo)
                .ToList();

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

        return Encoding.UTF8.GetBytes(sb.ToString());
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
        entity.UpdatedOn = model.UpdatedOn ?? DateTime.UtcNow;
    }

    #endregion

    #region Inward Item Operations

    private async Task CreateInwardItemsWithBarcodesAsync(int inwardId, List<InwardItemModel> itemModels, DateTime transactionDate)
    {
        if (!itemModels.Any()) return;

        var items = await CreateInwardItemEntitiesAsync(inwardId, itemModels);
        await CreateBarcodesForItemsAsync(items, transactionDate);
    }

    private async Task ReplaceInwardItemsAsync(int inwardId, List<InwardItemModel> newItems, DateTime transactionDate)
    {
        await DeleteExistingInwardItemsAsync(inwardId);
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

    private async Task DeleteExistingInwardItemsAsync(int inwardId)
    {
        var itemRepo = _unitOfWork.GetRepository<InwardItem>();
        var existingItems = await itemRepo.FindAsync(i => i.InwardId == inwardId);

        foreach (var item in existingItems)
        {
            await DeleteBarcodesForItemAsync(item.Id);
            await itemRepo.DeleteAsync(item.Id);
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
    private async Task<bool> CreateSalesReturnInwardItemAsync(int inwardId, SaleToInwardDto items)
    {
        try
        {
            var brandId = await GetBrandIdByProductId(items.ProductId);
            var unitName = CommonUtils.UnitList
                .FirstOrDefault(u => u.Id == items.UnitId)?.Name ?? string.Empty;
            var model = new InwardItemModel()
            {
                ProductId = items.ProductId,
                BrandId = brandId,
                InwardId = inwardId,
                InwardUnitId = items.UnitId,
                InwardUnitName = unitName,
                CreatedBy = items.CreatedBy,
                CreatedOn = items.CreatedOn,
                UpdatedBy = items.UpdatedBy,
                UpdatedOn = items.UpdatedOn,

            };

            if (items.UnitId == (int)UnitType.BOX)
            {
                model.BoxQuantity = 1;
                model.ItemQuantity = items.ReturnQuantity;
            }
            else
            {
                model.ItemQuantity = items.ReturnQuantity;
                model.BoxQuantity = 0;
            }
            var entity = await CreateInwardItemEntityAsync(model);
            await CreateBarcodesForSingleItemAsync(entity, DateTime.UtcNow);
            return true;
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error creating SaleReturn Inward Items: {ex.Message}");
            throw;
        }

    }
    private async Task<bool> FindSalesReturnInwardAsync(int inwardId,SaleToInwardDto saleToInwardDto) 
    {
        try
        {
            var itemRepo = _unitOfWork.GetRepository<InwardItem>();
            var item = itemRepo.GetQueryable().FirstOrDefault(i => i.InwardId == inwardId &&
                                                            i.ProductId == saleToInwardDto.ProductId &&
                                                            i.InwardUnitId == saleToInwardDto.UnitId &&
                                                            i.ItemQuantity == saleToInwardDto.ReturnQuantity);
            if (item == null) return false;

            var result = await DeleteInwardItemAsync(item.Id);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting inward item: {ex.Message}");
            throw;
        }
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
            b.TransactionDate.Date == transactionDate.Date &&
            b.IsActive && !b.IsDeleted);

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

    private async Task DeleteBarcodesForItemAsync(int itemId)
    {
        var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();
        var barcodes = await barcodeRepo.FindAsync(b => b.InwardItemId == itemId);

        foreach (var barcode in barcodes)
        {
            await barcodeRepo.DeleteAsync(barcode.Id);
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
            HasOutOfStockBarcodes = entity.InwardBarcodeItems.Any(b => !b.IsInStock)
        };
    }

    #endregion
}