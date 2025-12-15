using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.DTO;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
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

    public async Task<int> CreateAsync(InwardModel model)
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
        catch
        {
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

    public async Task<byte[]> GenerateItemBarcodePdfAsync(int inwardItemId)
    {
        var barcodeRepo = _unitOfWork.GetRepository<InwardBarcodeItem>();
        var barcodeItems = (await barcodeRepo.FindAsync(
                b => b.InwardItemId == inwardItemId && !b.IsDeleted && b.IsActive))
            .OrderBy(b => b.BarcodeNo)           // ensure consistent order (1,2,3,4,…)
            .ToList();

        if (!barcodeItems.Any()) return null;

        // ==========================================================
        // LABEL SIZE : 50 mm x 25 mm  (5.0 cm x 2.5 cm)
        // ==========================================================
        float widthInCm = 5.0f;   // 50 mm
        float heightInCm = 2.5f;  // 25 mm

        float widthPoints = widthInCm * 28.35f;   // cm -> points
        float heightPoints = heightInCm * 28.35f;

        var pageSize = new Rectangle(widthPoints, heightPoints);

        using var fs = new MemoryStream();
        Document doc = new Document(pageSize, 2f, 2f, 2f, 2f);  // small margins

        try
        {
            PdfWriter writer = PdfWriter.GetInstance(doc, fs);
            doc.Open();

            PdfContentByte cb = writer.DirectContent;
            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

            // width of each column (left/right)
            float cellWidth = doc.PageSize.Width / 2f;

            // ==========================================================
            // LOOP: 2 barcodes per page -> (1,2), (3,4), (5,6), ...
            // ==========================================================
            for (int i = 0; i < barcodeItems.Count; i += 2)
            {
                doc.NewPage();

                // LEFT BARCODE
                AddBarcodeToCell(doc, cb, bf, barcodeItems[i],
                                 cellIndex: 0, cellWidth: cellWidth);

                // RIGHT BARCODE (if exists)
                if (i + 1 < barcodeItems.Count)
                {
                    AddBarcodeToCell(doc, cb, bf, barcodeItems[i + 1],
                                     cellIndex: 1, cellWidth: cellWidth);
                }
            }
        }
        finally
        {
            doc.Close();
        }

        return fs.ToArray();
    }

    // Helper method: puts one barcode into left (0) or right (1) cell.
    private void AddBarcodeToCell(Document doc, PdfContentByte cb, BaseFont bf,
                                  InwardBarcodeItem item, int cellIndex, float cellWidth)
    {
        Barcode128 bc = new Barcode128
        {
            Code = item.BarcodeNo,
            CodeType = Barcode128.CODE128,
            StartStopText = false,
            Font = null,
            BarHeight = 18f,   // adjust if you need shorter/taller bars
            X = 0.6f           // bar thickness
        };

        Image img = bc.CreateImageWithBarcode(cb, BaseColor.BLACK, BaseColor.BLACK);

        // Make sure barcode fits in its half of the label
        float maxImgWidth = cellWidth - 4f;                // small padding
        float maxImgHeight = doc.PageSize.Height - 10f;    // leave room for text
        img.ScaleToFit(maxImgWidth, maxImgHeight);

        // X: center inside the cell (0 = left cell, 1 = right cell)
        float cellLeft = cellIndex * cellWidth;
        float imgX = cellLeft + (cellWidth - img.ScaledWidth) / 2f;

        // Y: center vertically a bit higher so text can be below
        float imgY = (doc.PageSize.Height - img.ScaledHeight) / 2f + 3f;

        img.SetAbsolutePosition(imgX, imgY);
        doc.Add(img);

        // ========== Text under barcode ==========
        cb.BeginText();
        float fontSize = 7f;
        cb.SetFontAndSize(bf, fontSize);

        float textWidth = bf.GetWidthPoint(item.BarcodeNo, fontSize);
        float textX = cellLeft + (cellWidth - textWidth) / 2f;
        float textY = imgY - 8f; // below the barcode

        cb.SetTextMatrix(textX, textY);
        cb.ShowText(item.BarcodeNo);
        cb.EndText();
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
                BrandId=itemModel.BrandId,
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
            BrandId= model.BrandId
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
            if (item.InwardUnitId == (int)UnitType.BOX)
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

            if (item.InwardUnitId == (int)UnitType.BOX)
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