using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services;

public class InwardService : IInwardService
{
    private readonly IUnitOfWork _unitOfWork;

    public InwardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<InwardModel>> GetAllAsync()
    {
        var inwardRepo = _unitOfWork.GetRepository<Inward>();
        var inwards = await inwardRepo.FindAsync(i => !i.IsDeleted);

        return inwards.Select(i => new InwardModel
        {
            Id = i.Id,
            InwardNo = i.InwardNo,
            InwardDate = i.InwardDate,
            ShipMentCompanyId = i.ShipMentCompanyId,
            Remarks = i.Remarks,
            IsActive = i.IsActive
        }).ToList();
    }

    public async Task<InwardModel?> GetByIdAsync(int id)
    {
        var inwardRepo = _unitOfWork.GetRepository<Inward>();
        var entity = await inwardRepo.GetByIdAsync(id);
        if (entity == null) return null;

        var items = await GetInwardItemsByInwardIdAsync(id);
        return new InwardModel
        {
            Id = entity.Id,
            InwardNo = entity.InwardNo,
            InwardDate = entity.InwardDate,
            ShipMentCompanyId = entity.ShipMentCompanyId,
            Remarks = entity.Remarks,
            IsActive = entity.IsActive,
            InwardItems = items
        };
    }

    public async Task<bool> CreateAsync(InwardModel model)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            var inwardRepo = _unitOfWork.GetRepository<Inward>();
            var inwardItemRepo = _unitOfWork.GetRepository<InwardItem>();
            var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();
            if (string.IsNullOrEmpty(model.InwardNo))
            {
                model.InwardNo = await GenerateInwardNoAsync(model.InwardDate);
            }

            // Create inward
            var inward = new Inward
            {
                InwardNo = model.InwardNo,
                InwardDate = model.InwardDate,
                ShipMentCompanyId = model.ShipMentCompanyId,
                CategoryId = model.CategoryId,
                Remarks = model.Remarks,
                IsActive = model.IsActive,
                IsDeleted = false
            };

            await inwardRepo.AddAsync(inward);
            await _unitOfWork.SaveChangesAsync();

            // Create all inward items using AddRange
            var serialNo = await inwardItemRepo.CountAsync();
            var inwardItems = new List<InwardItem>();

            foreach (var itemModel in model.InwardItems)
            {
                serialNo++;
                var batchNo = GenerateBatchNo(serialNo);

                inwardItems.Add(new InwardItem
                {
                    InwardId = inward.Id,
                    ProductId = itemModel.ProductId,
                    Quantity = itemModel.Quantity,
                    Unit = itemModel.Unit,
                    SerialNo = serialNo.ToString(),
                    BatchNo = batchNo,
                    IsDeleted = false
                });
            }

            await inwardItemRepo.AddRangeAsync(inwardItems);
            await _unitOfWork.SaveChangesAsync();

            // Create all barcodes
            var barcodes = new List<InwardBarcodeItem>();
            int barcodeCounter = 0;

            foreach (var item in inwardItems)
            {
                for (int i = 0; i < item.Quantity; i++)
                {
                    barcodeCounter++;
                    barcodes.Add(new InwardBarcodeItem
                    {
                        InwardId = inward.Id,
                        InwardItemId = item.Id,
                        BarcodeNo = GenerateBarcodeNumber(item.InwardId, barcodeCounter),
                        TransactionDate = inward.InwardDate,
                        IsInStock = true
                    });
                }
            }

            if (barcodes.Any())
            {
                await barcodeRepo.AddRangeAsync(barcodes);
            }
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
            var inwardRepo = _unitOfWork.GetRepository<Inward>();
            var itemRepo = _unitOfWork.GetRepository<InwardItem>();
            var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();

            var entity = await inwardRepo.GetByIdAsync(model.Id);
            if (entity == null) return false;

            // Update main fields
            entity.InwardNo = model.InwardNo;
            entity.InwardDate = model.InwardDate;
            entity.ShipMentCompanyId = model.ShipMentCompanyId;
            entity.CategoryId = model.CategoryId;
            entity.Remarks = model.Remarks;
            entity.IsActive = model.IsActive;

            // Delete existing items and their barcodes
            var existingItems = await itemRepo.FindAsync(i => i.InwardId == entity.Id);
            foreach (var item in existingItems)
            {
                var barcodeItems = await barcodeRepo.FindAsync(b => b.InwardItemId == item.Id);
                foreach (var barcode in barcodeItems)
                    await barcodeRepo.DeleteAsync(barcode.Id);

                await itemRepo.DeleteAsync(item.Id);
            }

            // Create all inward items using AddRange
            var serialNo = await itemRepo.CountAsync();
            var inwardItems = new List<InwardItem>();

            foreach (var itemModel in model.InwardItems)
            {
                serialNo++;
                var batchNo = GenerateBatchNo(serialNo);

                inwardItems.Add(new InwardItem
                {
                    InwardId = entity.Id,
                    ProductId = itemModel.ProductId,
                    Quantity = itemModel.Quantity,
                    Unit = itemModel.Unit,
                    SerialNo = serialNo.ToString(),
                    BatchNo = batchNo,
                    IsDeleted = false
                });
            }

            await itemRepo.AddRangeAsync(inwardItems);
            await _unitOfWork.SaveChangesAsync();

            // Create all barcodes
            var barcodes = new List<InwardBarcodeItem>();
            int barcodeCounter = 0;

            foreach (var item in inwardItems)
            {
                for (int i = 0; i < item.Quantity; i++)
                {
                    barcodeCounter++;
                    barcodes.Add(new InwardBarcodeItem
                    {
                        InwardId = entity.Id,
                        InwardItemId = item.Id,
                        BarcodeNo = GenerateBarcodeNumber(item.InwardId, barcodeCounter),
                        TransactionDate = entity.InwardDate,
                        IsInStock = true
                    });
                }
            }

            if (barcodes.Any())
            {
                await barcodeRepo.AddRangeAsync(barcodes);
            }
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            inwardRepo.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }


    public async Task<bool> DeleteAsync(int id)
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

        return items.Select(i => new InwardItemModel
        {
            Id = i.Id,
            InwardId = i.InwardId,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            Unit = i.Unit,
            BatchNo = i.BatchNo
        }).ToList();
    }

    private (int startYear, int endYear) GetFinancialYear(DateTime date)
    {
        int year = date.Year;
        if (date.Month < 4)
            return (year - 1, year);
        else
            return (year, year + 1);
    }

    private async Task<string> GenerateInwardNoAsync(DateTime inwardDate)
    {
        var inwardRepo = _unitOfWork.GetRepository<Inward>();

        var (startYear, endYear) = GetFinancialYear(inwardDate);

        var count = await inwardRepo.CountAsync();
        count++;

        string inwardNo = $"IN-{startYear % 100}-{endYear % 100}/{count}";
        return inwardNo;
    }

    private string GenerateBarcodeNumber(int inwardId, int counter)
    {
        return $"1{inwardId:D3}{counter:D3}";
    }

    private string GenerateBatchNo(int serialNo)
    {
        return $"1{serialNo:D3}";
    }
}
