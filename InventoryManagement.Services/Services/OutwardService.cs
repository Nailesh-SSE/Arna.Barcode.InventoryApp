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
                    OutwardId = newOutward.Id,
                    BarcodeNo = barcodeItem.BarcodeNo,
                    ProductId=barcodeItem.InwardItem.ProductId,
                    Quantity = 1,
                    Unit = "PCS",
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
    public async Task<bool> UpdateAsync(OutwardModel model)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var outwardRepo = _unitOfWork.GetRepository<Outward>();
            var entity = await outwardRepo.GetByIdAsync(model.Id);
            if (entity == null) return false;

            entity.OutwardNo= model.OutwardNo;
            entity.OutwardDate= model.OutwardDate;  
            entity.BillToCompanyId= model.BillToCompanyId;
            entity.IsActive= model.IsActive;
            entity.Remarks= model.Remarks;

            await ReplaceOutwardItemsAsync(entity.Id, model.OutwardDetails, entity.OutwardDate, saveChanges: false);
            outwardRepo.Update(entity);
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
    public async Task<bool> CreateOutwardItemAsync(OutWardItem model)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var outwardrepo = _unitOfWork.GetRepository<Outward>();
            var outward = await outwardrepo.GetByIdAsync(model.OutwardId);
            if (outward == null) return false;


            var entity = await CreateOutwardItemEntityAsync(model);

            await _unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch(Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
        
    }
    //public async Task<bool> UpdateOutwardItemAsync(OutWardItem model)
    //{
    //    try
    //    {
    //        await _unitOfWork.BeginTransactionAsync();

    //        var repo = _unitOfWork.GetRepository<OutwardDetail>();
    //        var entity = await repo.GetByIdAsync(model.Id);
    //        if (entity == null) return false;

    //        entity.ProductId = model.ProductId;
    //        entity.Quantity = model.Quantity;
    //        entity.Unit = model.Unit;

    //        repo.Update(entity);
    //        await _unitOfWork.SaveChangesAsync();

    //        await _unitOfWork.CommitTransactionAsync();
    //        return true;
    //    }
    //    catch (Exception ex)
    //    {
    //        await _unitOfWork.RollbackTransactionAsync();
    //        Console.WriteLine($"Error updating inward item: {ex.Message}");
    //        return false;
    //    }
    //}

    private async Task<OutwardDetail> CreateOutwardItemEntityAsync(OutWardItem itemmodel, bool saveChanges = true)
    {
        var outwardDetailRepo = _unitOfWork.GetRepository<OutwardDetail>();
        var inwardBarcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();

        var barcodeItem = (await inwardBarcodeRepo.FindAsync(x => x.BarcodeNo == itemmodel.BarcodeNo)).FirstOrDefault();

        if (barcodeItem == null)
            throw new Exception($"Invalid barcode number: {itemmodel.BarcodeNo}");

        if (barcodeItem.InwardItem == null)
            throw new Exception($"Barcode {itemmodel.BarcodeNo} is not linked to any InwardItem.");

        // ? Build outward detail entity
        var entity = new OutwardDetail
        {
            OutwardId = itemmodel.OutwardId,
            BarcodeNo = barcodeItem.BarcodeNo,          // ? correct link to DB record
            ProductId = barcodeItem.InwardItem.ProductId,
            Unit = itemmodel.Unit ?? "PCS",
            //Quantity = itemmodel.Quantity,
            IsDeleted = false
        };

        // ? Save to DB
        await outwardDetailRepo.AddAsync(entity);
        if (saveChanges)
            await _unitOfWork.SaveChangesAsync();

        // ? Mark barcode as used
        barcodeItem.IsInStock = false;
        inwardBarcodeRepo.Update(barcodeItem);
        if (saveChanges)
            await _unitOfWork.SaveChangesAsync();

        return entity;
    }

    private async Task ReplaceOutwardItemsAsync(int OutwardId, List<OutWardItem> newItems, DateTime transactionDate, bool saveChanges = true)
    {
        //await DeleteExistingOutwardItemsAsync(OutwardId);
        await CreateOutwardItemsAsync(OutwardId, newItems, transactionDate);
    }
    private async Task DeleteExistingOutwardItemsAsync(int outwardId)
    {
        var itemRepo = _unitOfWork.GetRepository<OutwardDetail>();
        var existingItems = await itemRepo.FindAsync(i => i.OutwardId == outwardId);

        foreach (var item in existingItems)
        {
            await itemRepo.DeleteAsync(item.Id);
        }
    }
    private async Task CreateOutwardItemsAsync(int OutwardId, List<OutWardItem> itemModels, DateTime transactionDate)
    {
        if (!itemModels.Any()) return;
        var itemRepo = _unitOfWork.GetRepository<OutwardDetail>();
        var items = new List<OutwardDetail>();

        foreach (var item in itemModels)
        {
            items.Add(new OutwardDetail
            {
                OutwardId = OutwardId,
                ProductId = item.ProductId,
                //Quantity = item.Quantity,
                Unit = item.Unit,
                BarcodeNo = item.BarcodeNo,
                IsDeleted = false
            });
        }

        await itemRepo.AddRangeAsync(items);
        await _unitOfWork.SaveChangesAsync();
    }
    //private async Task<List<OutwardDetail>> CreateOutwardItemEntitiesAsync(int outwardId, List<OutWardItem> itemModels)
    //{
    //    var itemRepo = _unitOfWork.GetRepository<OutwardDetail>();
    //    var serialNo = await itemRepo.CountAsync();
    //    var items = new List<OutwardDetail>();

    //    foreach (var itemModel in itemModels)
    //    {
    //        serialNo++;
    //        items.Add(new OutwardDetail
    //        {
    //            OutwardId = outwardId,
    //            ProductId = itemModel.ProductId,
    //            Quantity = itemModel.Quantity,
    //            Unit = itemModel.Unit,
    //            IsDeleted = false
    //        });
    //    }

    //    await itemRepo.AddRangeAsync(items);
    //    await _unitOfWork.SaveChangesAsync();
    //    return items;
    //}
}