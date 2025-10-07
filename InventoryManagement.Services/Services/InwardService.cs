using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;

namespace InventoryManagement.Services;

public class InwardService : IInwardService
{
    private readonly IUnitOfWork _unitOfWork;

    public InwardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Inward>> GetAllInwardsAsync()
    {
        var inwardRepository = _unitOfWork.GetRepository<Inward>();
        return await inwardRepository.FindAsync(i => !i.IsDeleted);
    }

    public async Task<Inward?> GetInwardByIdAsync(int id)
    {
        var inwardRepository = _unitOfWork.GetRepository<Inward>();
        return await inwardRepository.GetByIdAsync(id);
    }

    public async Task<bool> CreateInwardAsync(Inward inward, List<InwardItem> items)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var inwardRepository = _unitOfWork.GetRepository<Inward>();
            await inwardRepository.AddAsync(inward);
            await _unitOfWork.SaveChangesAsync();

            var inwardItemRepository = _unitOfWork.GetRepository<InwardItem>();
            var inwardBarcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();

            foreach (var item in items)
            {
                item.InwardId = inward.Id;
                await inwardItemRepository.AddAsync(item);
                await _unitOfWork.SaveChangesAsync();

                // Create barcode items based on quantity
                for (int i = 0; i < item.Quantity; i++)
                {
                    var barcodeItem = new InwardBarcodeItem
                    {
                        InwardId = inward.Id,
                        InwardItemId = item.Id,
                        BarcodeNo = await GenerateBarcodeNumberAsync(inward.Id, item.Id, item.BatchNo),
                        TransactionDate = inward.InwardDate,
                        IsInStock = true
                    };
                    await inwardBarcodeItemRepository.AddAsync(barcodeItem);
                }
            }

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

    public async Task<bool> UpdateInwardAsync(Inward inward)
    {
        try
        {
            var inwardRepository = _unitOfWork.GetRepository<Inward>();
            inwardRepository.UpdateAsync(inward);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteInwardAsync(int id)
    {
        try
        {
            var inwardRepository = _unitOfWork.GetRepository<Inward>();
            var inward = await inwardRepository.GetByIdAsync(id);
            if (inward == null) return false;

            // Check if any barcode items have been used in outward transactions
            var barcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();
            var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();

            var barcodeItems = await barcodeItemRepository.FindAsync(bi => bi.InwardId == id && !bi.IsDeleted);
            foreach (var barcodeItem in barcodeItems)
            {
                var outwardDetails = await outwardDetailRepository.FindAsync(od => od.InwardBarcodeItemId == barcodeItem.Id && !od.IsDeleted);
                if (outwardDetails.Any())
                {
                    return false; // Cannot delete inward with used barcode items
                }
            }

            inward.IsDeleted = true;
            inward.IsActive = false;
            inwardRepository.UpdateAsync(inward);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GenerateBarcodeNumberAsync(int inwardId, int inwardItemId, string batchNo)
    {
        var datePattern = DateTime.UtcNow.ToString("yyyyMMdd");
        var serialPattern = $"{inwardId:D4}{inwardItemId:D4}";
        return $"{datePattern}{serialPattern}{batchNo}";
    }

    public async Task<IEnumerable<InwardBarcodeItem>> GetBarcodeItemsByInwardIdAsync(int inwardId)
    {
        var barcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();
        return await barcodeItemRepository.FindAsync(bi => bi.InwardId == inwardId && !bi.IsDeleted);
    }

    public async Task<IEnumerable<InwardItem>> GetInwardItemsByInwardIdAsync(int inwardId)
    {
        var inwardItemRepository = _unitOfWork.GetRepository<InwardItem>();
        return await inwardItemRepository.FindAsync(ii => ii.InwardId == inwardId && !ii.IsDeleted);
    }
}