using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services;

public class InwardService : IInwardService
{
    private readonly IUnitOfWork _unitOfWork;

    public InwardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    #region Public Methods

    public async Task<List<InwardModel>> GetAllAsync()
    {
        var inwardRepo = _unitOfWork.GetRepository<Inward>();
        var inwards = await inwardRepo.GetQueryable()
              .Include(i => i.ShipMentCompany)
              .Include(i => i.Category)
              .Where(i => !i.IsDeleted)
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

    public async Task<bool> CreateAsync(InwardModel model)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            if (string.IsNullOrEmpty(model.InwardNo))
            {
                model.InwardNo = await GenerateInwardNoAsync(model.InwardDate);
            }

            var inward = await CreateInwardEntityAsync(model);
            await CreateInwardItemsWithBarcodesAsync(inward.Id, model.InwardItems, inward.InwardDate);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            Console.WriteLine($"Error creating inward: {ex.Message}");
            return false;
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

    public async Task<bool> DeleteAsync(int id,int deletedBy)
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
        var items = await itemRepo.FindAsync(ii => ii.InwardId == inwardId && !ii.IsDeleted);
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
            entity.UpdatedBy = model.UpdatedBy ?? 0;
            entity.UpdatedOn = model.UpdatedOn ?? DateTime.UtcNow;
            entity.InwardUnitName = model.InwardUnitName;
            // Handle quantity change

            if (entity.InwardUnitName == "BOX")
            {
                if(entity.InwardUnitId != model.InwardUnitId)
                {
                    entity.BoxQuantity = model.BoxQuantity;
                    await DeleteBarcodesForItemAsync(entity.Id);
                    await CreateBarcodesForSingleItemAsync(entity, inward.InwardDate);
                }

                else if (entity.BoxQuantity != model.BoxQuantity)
                {
                    entity.BoxQuantity = model.BoxQuantity;
                    await DeleteBarcodesForItemAsync(entity.Id);
                    await CreateBarcodesForSingleItemAsync(entity, inward.InwardDate);
                }
            }
            else 
            {
                if(entity.InwardUnitId != model.InwardUnitId)
                {
                    entity.ItemQuantity = model.ItemQuantity;
                    await DeleteBarcodesForItemAsync(entity.Id);
                    await CreateBarcodesForSingleItemAsync(entity, inward.InwardDate);
                }

                else if (entity.ItemQuantity != model.ItemQuantity)
                {
                    entity.ItemQuantity = model.ItemQuantity;
                    await DeleteBarcodesForItemAsync(entity.Id);
                    await CreateBarcodesForSingleItemAsync(entity, inward.InwardDate);
                }
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
        catch
        {
            return false;
        }
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
            CreatedOn = model.CreatedOn
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
            CreatedBy = model.CreatedBy ,
            CreatedOn = model.CreatedOn,
            BoxQuantity = model.BoxQuantity
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

    #endregion

    #region Barcode Operations

    private async Task CreateBarcodesForItemsAsync(List<InwardItem> items, DateTime transactionDate)
    {
        var barcodes = new List<InwardBarcodeItem>();
        int barcodeCounter = 0;

        foreach (var item in items)
        {
            if(item.InwardUnitName == "BOX")
            {
                for (int i = 0; i < item.BoxQuantity; i++)
                {
                    barcodeCounter++;
                    barcodes.Add(CreateBarcodeEntity(item, barcodeCounter, transactionDate));
                }
            }
            else
            {
                for (int i = 0; i < item.ItemQuantity; i++)
                {
                    barcodeCounter++;
                    barcodes.Add(CreateBarcodeEntity(item, barcodeCounter, transactionDate));
                }
            }
        }

        if (barcodes.Any())
        {
            var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();
            await barcodeRepo.AddRangeAsync(barcodes);
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

            int startCounter = 1; // Default start

            if (existingBarcodes != null && existingBarcodes.Any())
            {
                // Find the highest counter from existing barcodes
                var maxCounter = existingBarcodes
               .Select(b => int.Parse(b.BarcodeNo[^6..]))
               .Max();

                startCounter = maxCounter + 1;
            }

            var barcodes = new List<InwardBarcodeItem>();

            if(item.InwardUnitName == "BOX")
            {
                for (int i = 0; i < item.BoxQuantity; i++)
                {
                    barcodes.Add(CreateBarcodeEntity(item, startCounter + i, transactionDate));
                }
            }
            else
            {
                for (int i = 0; i < item.ItemQuantity; i++)
                {
                    barcodes.Add(CreateBarcodeEntity(item, startCounter + i, transactionDate));
                }
            }

            if (barcodes.Any())
            {
                await barcodeRepo.AddRangeAsync(barcodes);
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

    private InwardBarcodeItem CreateBarcodeEntity(InwardItem item, int counter, DateTime transactionDate)
    {
        return new InwardBarcodeItem
        {
            InwardId = item.InwardId,
            CreatedBy = item.CreatedBy,
            InwardItemId = item.Id,
            BarcodeNo = GenerateBarcodeNumber(transactionDate, item.InwardId, counter),
            TransactionDate = transactionDate,
            IsInStock = true
        };
    }

    #endregion

    #region Number Generators

    private async Task<string> GenerateInwardNoAsync(DateTime inwardDate)
    {
        var inwardRepo = _unitOfWork.GetRepository<Inward>();
        var (startYear, endYear) = GetFinancialYear(inwardDate);
        var count = await inwardRepo.CountAsync();
        count++;

        return $"IN-{startYear % 100}-{endYear % 100}/{count}";
    }

    private string GenerateBarcodeNumber(DateTime TransactionDate,int inwardId, int counter)
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
            IsActive = entity.IsActive,
            CategoryId=entity.CategoryId,
            CreatedBy = entity.CreatedBy,
        };
    }

    private InwardItemModel MapItemToModel(InwardItem entity)
    {
        return new InwardItemModel
        {
            Id = entity.Id,
            InwardId = entity.InwardId,
            ProductId = entity.ProductId,
            ItemQuantity = entity.ItemQuantity,
            InwardUnitId = entity.InwardUnitId,
            InwardUnitName = entity.InwardUnitName,
            BatchNo = entity.BatchNo,
            CreatedBy = entity.CreatedBy,
            BoxQuantity = entity.BoxQuantity
        };
    }

    #endregion
}