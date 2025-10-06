using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Interfaces;

namespace InventoryManagement.Services.Services;

public class OutwardService : IOutwardService
{
    private readonly IUnitOfWork _unitOfWork;

    public OutwardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Outward>> GetAllOutwardsAsync()
    {
        var outwardRepository = _unitOfWork.GetRepository<Outward>();
        return await outwardRepository.FindAsync(o => !o.IsDeleted);
    }

    public async Task<Outward?> GetOutwardByIdAsync(int id)
    {
        var outwardRepository = _unitOfWork.GetRepository<Outward>();
        return await outwardRepository.GetByIdAsync(id);
    }

    public async Task<bool> CreateOutwardAsync(Outward outward, List<int> barcodeItemIds)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var outwardRepository = _unitOfWork.GetRepository<Outward>();
            await outwardRepository.AddAsync(outward);
            await _unitOfWork.SaveChangesAsync();

            var inwardBarcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();
            var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();

            foreach (var barcodeItemId in barcodeItemIds)
            {
                var barcodeItem = await inwardBarcodeItemRepository.GetByIdAsync(barcodeItemId);
                if (barcodeItem == null || !barcodeItem.IsInStock)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                // Create outward detail
                var outwardDetail = new OutwardDetail
                {
                    OutwardId = outward.Id,
                    InwardBarcodeItemId = barcodeItemId,
                    BarcodeNo = barcodeItem.BarcodeNo,
                   //TransactionDate = outward.TransactionDate
                };
                await outwardDetailRepository.AddAsync(outwardDetail);

                // Update barcode item status
                barcodeItem.IsInStock = false;
                inwardBarcodeItemRepository.UpdateAsync(barcodeItem);
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

    public async Task<bool> UpdateOutwardAsync(Outward outward)
    {
        try
        {
            var outwardRepository = _unitOfWork.GetRepository<Outward>();
            outwardRepository.UpdateAsync(outward);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteOutwardAsync(int id)
    {
        try
        {
            var outwardRepository = _unitOfWork.GetRepository<Outward>();
            var outward = await outwardRepository.GetByIdAsync(id);
            if (outward == null) return false;

            outward.IsDeleted = true;
            outward.IsActive = false;
            outwardRepository.UpdateAsync(outward);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<InwardBarcodeItem>> GetAvailableBarcodeItemsAsync()
    {
        var barcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();
        return await barcodeItemRepository.FindAsync(bi => bi.IsInStock && !bi.IsDeleted);
    }

    public async Task<bool> IsBarcodeAvailableAsync(string barcodeNo)
    {
        var barcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();
        var barcodeItems = await barcodeItemRepository.FindAsync(bi => bi.BarcodeNo == barcodeNo && !bi.IsDeleted);
        var barcodeItem = barcodeItems.FirstOrDefault();
        
        return barcodeItem != null && barcodeItem.IsInStock;
    }

    public async Task<IEnumerable<OutwardDetail>> GetOutwardDetailsByOutwardIdAsync(int outwardId)
    {
        var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();
        return await outwardDetailRepository.FindAsync(od => od.OutwardId == outwardId && !od.IsDeleted);
    }
}