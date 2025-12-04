using InventoryManagement.Core.Data;
using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.DTO;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.Formula.Functions;

namespace InventoryManagement.Services;

public class OutwardService : IOutwardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly OutwardPdfService _pdfService;
    private readonly InventoryDbContext _context;
    public OutwardService(IUnitOfWork unitOfWork, OutwardPdfService pdfService, InventoryDbContext context)
    {
        _unitOfWork = unitOfWork;
        _pdfService = pdfService;
        _context = context;
    }

    public async Task<List<OutwardModel>> GetAllOutwardsAsync()
    {
        var outwardRepository = _unitOfWork.GetRepository<Outward>();

        var outwards = await outwardRepository
                             .GetQueryable()
                             .Where(o => !o.IsDeleted)
                             .OrderByDescending(o => o.OutwardDate)
                             .ThenByDescending(o => o.Id)
                             .ToListAsync();
        // var outwards = await outwardRepository.FindAsync(o => !o.IsDeleted);

        return outwards.Select(MapToModel).ToList();
    }

    public async Task<OutwardModel?> GetOutwardByIdAsync(int id)
    {
        var outwardRepository = _unitOfWork.GetRepository<Outward>();
        var outward = await outwardRepository.GetByIdAsync(id);

        return outward == null ? null : MapToModel(outward);
    }

    public async Task<int> CreateOutwardAsync(OutwardModel model)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var outwardRepository = _unitOfWork.GetRepository<Outward>();
            await GenerateOutwardNumberAsync(model);

            var newOutward = new Outward
            {
                OutwardNo = model.OutwardNo,
                OutwardDate = model.OutwardDate,
                BillToCompanyId = model.BillToCompanyId,
                PlatformId = model.PlatformId,
                Remarks = model.Remarks,
                IsActive = true,
                IsDeleted = false,
                IsFinished = false,
                CreatedBy = model.CreatedBy,
                CreatedOn = model.CreatedOn
            };

            await outwardRepository.AddAsync(newOutward);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return newOutward.Id;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            return 0; ;
        }
    }

    public async Task<bool> UpdateOutwardAsync(OutwardModel model)
    {
        try
        {
            var outwardRepository = _unitOfWork.GetRepository<Outward>();
            var existing = await outwardRepository.GetByIdAsync(model.Id);

            if (existing == null)
                return false;

            existing.OutwardDate = model.OutwardDate;
            existing.BillToCompanyId = model.BillToCompanyId;
            existing.PlatformId = model.PlatformId;
            existing.Remarks = model.Remarks;
            existing.IsActive = model.IsActive;
            existing.UpdatedBy = model.UpdatedBy;
            existing.UpdatedOn = DateTime.UtcNow;
            existing.IsFinished = model.IsFinished;

            outwardRepository.Update(existing);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> DeleteOutwardAsync(int id, int userid)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var outwardRepository = _unitOfWork.GetRepository<Outward>();
            var outward = await outwardRepository.GetByIdAsync(id);

            if (outward == null)
                return false;

            var details = await GetOutwardDetailsByOutwardIdAsync(id);
            foreach (var detail in details)
            {
                await UpdateBarcodeStockStatus(detail.BarcodeNo, true);
            }

            outward.IsDeleted = true;
            outward.IsActive = false;
            outward.UpdatedOn = DateTime.UtcNow;
            outward.UpdatedBy = userid;
            outwardRepository.Update(outward);

            var detailRepository = _unitOfWork.GetRepository<OutwardDetail>();
            var outwardDetails = await detailRepository.FindAsync(od => od.OutwardId == id);
            foreach (var detail in outwardDetails)
            {
                detail.IsDeleted = true;
                detail.UpdatedOn = DateTime.UtcNow;
                detail.UpdatedBy = userid;
                detailRepository.Update(detail);
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

    public async Task<List<OutWardItemModel>> GetOutwardDetailsByOutwardIdAsync(int outwardId)
    {
        var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();
        var productRepository = _unitOfWork.GetRepository<Product>();

        var details = await outwardDetailRepository.FindAsync(od =>
            od.OutwardId == outwardId && !od.IsDeleted);

        var result = new List<OutWardItemModel>();

        foreach (var detail in details)
        {
            var product = await productRepository.GetByIdAsync(detail.ProductId);
            result.Add(new OutWardItemModel
            {
                Id = detail.Id,
                OutwardId = detail.OutwardId,
                ProductId = detail.ProductId,
                ProductName = product != null ? $"{product?.Name} ({product?.ColourName})" : "Unknown",
                BarcodeNo = detail.BarcodeNo,
                Quantity = detail.Quantity,
                Unit = detail.Unit
            });
        }

        return result;
    }

    public async Task<BarcodeValidationResult> ValidateBarcodeForOutwardAsync(string barcodeNo, int outwardId)
    {
        var barcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();
        var barcodeItems = await barcodeItemRepository.FindAsync(bi =>
            bi.BarcodeNo == barcodeNo && !bi.IsDeleted);

        var barcodeItem = barcodeItems.FirstOrDefault();

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
                ErrorMessage = "This Barcode already scanned."
            };
        }

        var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();
        var existingDetails = await outwardDetailRepository.FindAsync(od =>
            od.OutwardId == outwardId &&
            od.BarcodeNo == barcodeNo &&
            !od.IsDeleted);

        if (existingDetails.Any())
        {
            return new BarcodeValidationResult
            {
                IsValid = false,
                ErrorMessage = "This barcode has already been added to this outward."
            };
        }

        return new BarcodeValidationResult { IsValid = true };
    }

    public async Task<OutWardItemModel?> AddOutwardItemAsync(int outwardId, string barcodeNo, int userid)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var barcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();
            var barcodeItem = (await barcodeItemRepository.FindAsync(bi =>
                bi.BarcodeNo == barcodeNo && !bi.IsDeleted)).FirstOrDefault();

            if (barcodeItem == null)
                throw new Exception("Barcode item not found.");

            var inwardItemRepository = _unitOfWork.GetRepository<InwardItem>();
            var inwardItem = await inwardItemRepository.GetByIdAsync(barcodeItem.InwardItemId);

            if (inwardItem == null)
                throw new Exception("Inward item not found.");

            var productRepository = _unitOfWork.GetRepository<Product>();
            var product = await productRepository.GetByIdAsync(inwardItem.ProductId);

            var detailRepository = _unitOfWork.GetRepository<OutwardDetail>();
            var newDetail = new OutwardDetail
            {
                OutwardId = outwardId,
                ProductId = inwardItem.ProductId,
                Quantity = 1,
                Unit = inwardItem.InwardUnitName,
                BarcodeNo = barcodeNo,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = userid,
                IsDeleted = false,
                IsActive = true
            };

            await detailRepository.AddAsync(newDetail);

            // Update stock status
            await UpdateBarcodeStockStatus(barcodeNo, false);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new OutWardItemModel
            {
                Id = newDetail.Id,
                OutwardId = outwardId,
                ProductId = inwardItem.ProductId,
                ProductName = product?.SKU ?? "Unknown",
                BarcodeNo = barcodeNo,
                Quantity = 1,
                Unit = inwardItem.InwardUnitName
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> DeleteOutwardItemAsync(int outwardDetailId, string barcodeNo, int userid)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var detailRepository = _unitOfWork.GetRepository<OutwardDetail>();
            var detail = await detailRepository.GetByIdAsync(outwardDetailId);

            if (detail == null)
                return false;

            detail.IsDeleted = true;
            detail.UpdatedOn = DateTime.UtcNow;
            detail.UpdatedBy = userid;
            detailRepository.Update(detail);

            await UpdateBarcodeStockStatus(barcodeNo, true);

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

    private async Task UpdateBarcodeStockStatus(string barcodeNo, bool isInStock)
    {
        var barcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();
        var barcodeItem = (await barcodeItemRepository.FindAsync(bi =>
            bi.BarcodeNo == barcodeNo && !bi.IsDeleted)).FirstOrDefault();

        if (barcodeItem != null)
        {
            barcodeItem.IsInStock = isInStock;
            barcodeItemRepository.Update(barcodeItem);
        }
    }

    private async Task GenerateOutwardNumberAsync(OutwardModel model)
    {
        var repository = _unitOfWork.GetRepository<Outward>();
        var outwards = await repository.GetAllAsync();
        var lastOutward = outwards
            .Where(a => a.IsActive && !a.IsDeleted)
            .OrderByDescending(a => a.Id)
            .FirstOrDefault();

        if (lastOutward != null && int.TryParse(lastOutward.OutwardNo, out int lastNumber))
        {
            model.OutwardNo = (lastNumber + 1).ToString("D6");
        }
        else
        {
            model.OutwardNo = "000001";
        }
    }

    private OutwardModel MapToModel(Outward entity)
    {
        return new OutwardModel
        {
            Id = entity.Id,
            OutwardNo = entity.OutwardNo,
            OutwardDate = entity.OutwardDate,
            BillToCompanyId = entity.BillToCompanyId,
            PlatformId = entity.PlatformId,
            Remarks = entity.Remarks,
            IsActive = entity.IsActive,
            IsFinished = entity.IsFinished
        };
    }
    public async Task<byte[]> GenerateOutwardPdf(int outwardId)
    {
        var repo = _unitOfWork.GetRepository<Outward>();

        var outwardData = await _context.Outwards
            .Where(o => o.Id == outwardId)
            .Select(o => new
            {
                o.Id,
                o.BillToCompanyId,
                CompanyName = o.BillToCompany.Name,
                TotalQuantity = o.OutwardDetails.Sum(d => d.Quantity)
            })
            .FirstOrDefaultAsync();

        if (outwardData == null)
            throw new Exception("Outward not found");

        var model = new OutwardPdfSummeryDto
        {
            Id = outwardData.Id,
            BillToCompanyId = outwardData.BillToCompanyId,
            CompanyName = outwardData.CompanyName,
            TotalQuantity = outwardData.TotalQuantity
        };
        return _pdfService.GeneratePdf(model);
    }
}