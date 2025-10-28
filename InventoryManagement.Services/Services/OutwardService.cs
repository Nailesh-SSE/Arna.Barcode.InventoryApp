using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services;

public class OutwardService : IOutwardService
{
    private readonly IUnitOfWork _unitOfWork;

    public OutwardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<OutwardModel>> GetAllOutwardsAsync()
    {
        var outwardRepository = _unitOfWork.GetRepository<Outward>();
        var outwards= await outwardRepository.FindAsync(o => !o.IsDeleted);

        return outwards.Select(i => new OutwardModel
        {
            Id = i.Id,
            OutwardNo = i.OutwardNo,
            OutwardDate = i.OutwardDate,
            BillToCompanyId = i.BillToCompanyId,
            InvoiceDate=i.InvoiceDate,
            InvoiceNo=i.InvoiceNo,
            ChallanNo=i.ChallanNo,
            Remarks = i.Remarks,
            IsActive = i.IsActive
        }).ToList();
    }

    public async Task<OutwardModel?> GetOutwardByIdAsync(int id)
    {
        var outwardRepository = _unitOfWork.GetRepository<Outward>();
        var outward = await outwardRepository.GetByIdAsync(id);
        if (outward == null) return null;

        var model = new OutwardModel
        {
            Id = outward.Id,
            OutwardNo = outward.OutwardNo,
            OutwardDate = outward.OutwardDate,
            BillToCompanyId = outward.BillToCompanyId,
            Remarks = outward.Remarks,
            IsActive = outward.IsActive,
            InvoiceDate = outward.InvoiceDate,
            InvoiceNo = outward.InvoiceNo,
            ChallanNo = outward.ChallanNo,
        };
        return model;
    }

    public async Task<bool> CreateOutwardAsync(OutwardModel outward, List<int> barcodeItemIds)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var outwardRepository = _unitOfWork.GetRepository<Outward>();
            await GenerateOutwardNumberAsync(outward);
            var newOutward = new Outward
            {
                OutwardNo = outward.OutwardNo,
                OutwardDate = outward.OutwardDate,
                BillToCompanyId = outward.BillToCompanyId,
                Remarks = outward.Remarks,
                InvoiceDate = outward.InvoiceDate,
                InvoiceNo = outward.InvoiceNo,
                ChallanNo = outward.ChallanNo,
                IsActive = true,
                IsDeleted = false
            };
            await outwardRepository.AddAsync(newOutward);
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
                    InwardItemId = barcodeItemId,
                    BarcodeNo = barcodeItem.BarcodeNo,
                   //TransactionDate = outward.TransactionDate
                };
                await outwardDetailRepository.AddAsync(outwardDetail);

                // Update barcode item status
                barcodeItem.IsInStock = false;
                inwardBarcodeItemRepository.Update(barcodeItem);
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

    public async Task<bool> UpdateOutwardAsync(OutwardModel outward)
    {
        try
        {
            var outwardRepository = _unitOfWork.GetRepository<Outward>();
            var existingoutword = await outwardRepository.GetByIdAsync(outward.Id);

            if (existingoutword == null) return false;

            existingoutword.OutwardNo = outward.OutwardNo;
            existingoutword.OutwardDate = outward.OutwardDate;
            existingoutword.BillToCompanyId = outward.BillToCompanyId;
            existingoutword.Remarks = outward.Remarks;
            existingoutword.InvoiceDate = outward.InvoiceDate;
            existingoutword.InvoiceNo = outward.InvoiceNo;
            existingoutword.ChallanNo = outward.ChallanNo;
            existingoutword.IsActive = outward.IsActive;
            existingoutword.IsDeleted = !outward.IsActive;
            outwardRepository.Update(existingoutword);
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
            outwardRepository.Update(outward);
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

    public async Task<List<OutWardItem>> GetOutwardDetailsByOutwardIdAsync(int outwardId)
    {
        var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();
        var outwarddetails= await outwardDetailRepository.FindAsync(od => od.OutwardId == outwardId && !od.IsDeleted);
        if(outwarddetails == null)
        {
            return new List<OutWardItem>();
        }
        return outwarddetails.Select(o => new OutWardItem {
            Id=o.Id,
            OutwardId=o.OutwardId,
            ProductId =o.ProductId,
            BarcodeNo=o.BarcodeNo,
            Quantity = o.Quantity,
            Unit = o.Unit,
        }).ToList();
    }

    public async Task GenerateOutwardNumberAsync(OutwardModel model)
    {
        var Repository = _unitOfWork.GetRepository<Outward>();
        var outwards = await Repository.GetAllAsync();
        var outward = outwards.Where(a => a.IsActive && !a.IsDeleted).OrderByDescending(a => a.Id).FirstOrDefault();
        if (outward != null && int.TryParse(outward.OutwardNo, out int lastNumber))
        {
            model.OutwardNo = (lastNumber + 1).ToString();
        }
        else
        {
            model.OutwardNo = "1";
        }
    }
}