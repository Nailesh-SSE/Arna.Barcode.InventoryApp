using EnumsNET;
using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services;

public class OutwardService : IOutwardService
{
    private readonly IUnitOfWork _unitOfWork;

    public OutwardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<OutwardModel>> GetAllOutwardsAsync(bool isAdmin)
    {
        var outwardRepository = _unitOfWork.GetRepository<Outward>();
        var outwards = await outwardRepository
                             .GetQueryable()
                             .Include(x => x.BillToCompany)
                             .Include(x => x.Platform)
                             .Where(o => !o.IsDeleted)
                             .OrderByDescending(o => o.OutwardDate)
                             .ThenByDescending(o => o.Id)
                             .ToListAsync();
        // var outwards = await outwardRepository.FindAsync(o => !o.IsDeleted);
        if (!isAdmin)
        {
            var today = DateTime.Today;
            outwards = outwards.Where(o => o.OutwardDate.Date == today).ToList();
        }
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
            if (string.IsNullOrEmpty(model.OutwardNo))
            {
                model.OutwardNo = await GenerateOutwardNumberAsync(model.OutwardDate);
            }
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
            existing.UpdatedOn = DateTime.Now;
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
            outward.UpdatedOn = DateTime.Now;
            outward.UpdatedBy = userid;
            outwardRepository.Update(outward);

            var detailRepository = _unitOfWork.GetRepository<OutwardDetail>();
            var outwardDetails = await detailRepository.FindAsync(od => od.OutwardId == id);
            foreach (var detail in outwardDetails)
            {
                detail.IsDeleted = true;
                detail.UpdatedOn = DateTime.Now;
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
                ProductName = product != null ? product?.SKU : "Unknown",
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
            var goodBoxQty = 0;
            if (barcodeItem == null)
                throw new Exception("Barcode item not found.");

            var inwardItemRepository = _unitOfWork.GetRepository<InwardItem>();
            var inwardItem = await inwardItemRepository.GetByIdAsync(barcodeItem.InwardItemId);

            if (inwardItem == null)
                throw new Exception("Inward item not found.");

            var productRepository = _unitOfWork.GetRepository<Product>();
            var product = await productRepository.GetByIdAsync(inwardItem.ProductId);

            if ((barcodeItem.ParentId == 0 || barcodeItem.ParentId == null) &&
                inwardItem.InwardUnitId == (int)UnitType.BOX)
            {
                var AvailableBoxItem = await barcodeItemRepository.FindAsync(bi => bi.ParentId == barcodeItem.Id && !bi.IsDeleted && bi.IsInStock);
                goodBoxQty = AvailableBoxItem.Count();
                if (goodBoxQty == 0)
                    throw new Exception("No available items in this box.");
            }

            var detailRepository = _unitOfWork.GetRepository<OutwardDetail>();
            var newDetail = new OutwardDetail
            {
                OutwardId = outwardId,
                ProductId = inwardItem.ProductId,
                Quantity = 1,
                Unit = barcodeItem.ParentId > 0 ? UnitType.PCS.GetName() : inwardItem.InwardUnitName,
                BarcodeNo = barcodeNo,
                CreatedOn = DateTime.Now,
                CreatedBy = userid,
                IsDeleted = false,
                IsActive = true
            };

            if (newDetail.Unit == UnitType.BOX.GetName())
            {
                newDetail.Quantity = goodBoxQty;
            }

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
                Quantity = newDetail.Quantity,
                Unit = newDetail.Unit
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
            detail.UpdatedOn = DateTime.Now;
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

    public async Task<List<OutWardItemModel>> FindSaleReturnBarcode(int outwardId)
    {
        try
        {
            var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();
            var saleReturnItemRepository = _unitOfWork.GetRepository<SaleReturnItems>();
            var saleReturnItems = saleReturnItemRepository
                .GetQueryable()
                .Where(sri => !sri.IsDeleted && sri.BarCodeNo != null);

            return await (
                from outwardDetail in outwardDetailRepository.GetQueryable()
                join saleReturnItem in saleReturnItems
                    on outwardDetail.BarcodeNo equals saleReturnItem.BarCodeNo into matchingSaleReturnItems
                where outwardDetail.OutwardId == outwardId && !outwardDetail.IsDeleted
                select new OutWardItemModel
                {
                    Id = outwardDetail.Id,
                    OutwardId = outwardDetail.OutwardId,
                    ProductId = outwardDetail.ProductId,
                    ProductName = outwardDetail.Product != null ? outwardDetail.Product.SKU : "Unknown",
                    BarcodeNo = outwardDetail.BarcodeNo,
                    Quantity = outwardDetail.Quantity,
                    Unit = outwardDetail.Unit,
                    IsInSalereturn = matchingSaleReturnItems.Any()
                })
                .ToListAsync();
        }
        catch
        {
            throw;
        }
    }

    private async Task UpdateBarcodeStockStatus(string barcodeNo, bool isInStock)
    {
        var barcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();

        var barcodeItem = (await barcodeItemRepository.FindWithIncludesAsync(
    bi => bi.BarcodeNo == barcodeNo && !bi.IsDeleted,
    CancellationToken.None,
    bi => bi.InwardItem
)).FirstOrDefault();

        if (barcodeItem == null) return;

        //Only if Box and it has Child Items
        if ((barcodeItem?.ParentId == 0 || barcodeItem?.ParentId == null)
            && barcodeItem?.InwardItem.InwardUnitId == (int)UnitType.BOX
            && barcodeItem.InwardItem.BoxQuantity >= 0)
        {
            var detailRepository = _unitOfWork.GetRepository<OutwardDetail>();
            var childBarcodes = await barcodeItemRepository.FindAsync(bi =>
               bi.ParentId == barcodeItem.Id
               && !bi.IsDeleted
               && !detailRepository.GetQueryable().Any(od => od.BarcodeNo == bi.BarcodeNo && !od.IsDeleted)
           );

            foreach (var item in childBarcodes)
            {
                item.IsInStock = isInStock;
                barcodeItemRepository.Update(item);
            }
        }
        //Update Single Barcode
        if (barcodeItem != null)
        {
            barcodeItem.IsInStock = isInStock;
            barcodeItemRepository.Update(barcodeItem);
        }
    }

    private async Task<string> GenerateOutwardNumberAsync(DateTime outwardDate)
    {
        var outwardRepo = _unitOfWork.GetRepository<Outward>();
        var (startYear, endYear) = GetFinancialYear(outwardDate);
        var count = await outwardRepo.CountAsync();
        count++;

        return $"OT-{startYear % 100}-{endYear % 100}/{count}";
    }
    private (int startYear, int endYear) GetFinancialYear(DateTime date)
    {
        int year = date.Year;
        if (date.Month < 4)
            return (year - 1, year);
        else
            return (year, year + 1);
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
            platformName = entity.Platform.Name ?? string.Empty,
            Remarks = entity.Remarks,
            BillToCompanyName = entity.BillToCompany.Name ?? string.Empty,
            IsActive = entity.IsActive,
            IsFinished = entity.IsFinished
        };
    }
    public async Task<OutwardModel?> GetOutwardPdfDataAsync(int outwardId)
    {
        var outwardRepository = _unitOfWork.GetRepository<Outward>();
        var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();

        var outward = await outwardRepository
            .GetQueryable()
            .Include(x => x.BillToCompany)
            .Include(x => x.Platform)
            .FirstOrDefaultAsync(x => x.Id == outwardId && !x.IsDeleted);

        if (outward == null)
            return null;

        var groupedItems = await outwardDetailRepository
            .GetQueryable()
            .Where(x => x.OutwardId == outwardId && !x.IsDeleted)
            .GroupBy(x => new
            {
                x.ProductId,
                ProductName = x.Product.SKU, // or x.Product.Name if you want product name
                x.Unit
            })
            .Select(g => new OutWardItemModel
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                Unit = g.Key.Unit,
                Quantity = g.Sum(x => x.Quantity),
                BoxQuantity = g.Count(x => x.Unit == UnitType.BOX.GetName())
            })
            .OrderBy(x => x.ProductName)
            .ThenBy(x => x.Unit)
            .ToListAsync();

        return new OutwardModel
        {
            Id = outward.Id,
            OutwardNo = outward.OutwardNo,
            OutwardDate = outward.OutwardDate,
            BillToCompanyName = outward.BillToCompany.Name,
            platformName = outward.Platform.Name,
            Remarks = outward.Remarks,
            OutwardItems = groupedItems
        };
    }
    //public async Task<byte[]> GenerateExcelAsync(int outwardId)
    //{
    //    try
    //    {
    //        var data = await GetOutwardPdfDataAsync(outwardId);

    //        IWorkbook workbook = new XSSFWorkbook();
    //        ISheet sheet = workbook.CreateSheet("Inward Report");

    //        // Header row
    //        IRow headerRow = sheet.CreateRow(0);
    //        string[] headers = new string[]
    //           {
    //            "No.", "Outward Number", "Outward Date", "Bill To", "Platform" , "Product", "Unit","Quantity"
    //           };

    //        for (int i = 0; i < headers.Length; i++)
    //        {
    //            headerRow.CreateCell(i).SetCellValue(headers[i]);
    //        }

    //        // Data rows
    //        for (int i = 0; i < data?.OutwardItems.Count; i++)
    //        {
    //            var item = data.OutwardItems[i];
    //            IRow row = sheet.CreateRow(i + 1);
    //            row.CreateCell(0).SetCellValue(i + 1);
    //            row.CreateCell(1).SetCellValue(data.OutwardNo);
    //            row.CreateCell(2).SetCellValue(data.OutwardDate.ToString("dd/MM/yyyy"));
    //            row.CreateCell(3).SetCellValue(data.BillToCompanyName);
    //            row.CreateCell(4).SetCellValue(data?.platformName);
    //            row.CreateCell(5).SetCellValue(item.ProductName);
    //            row.CreateCell(6).SetCellValue(item.Unit);
    //            row.CreateCell(7).SetCellValue((double)item.Quantity);
    //        }

    //        // Autosize all columns
    //        for (int i = 0; i < headers.Length; i++)
    //        {
    //            sheet.AutoSizeColumn(i);
    //        }

    //        // Write to memory stream and return as byte array
    //        using (var exportData = new MemoryStream())
    //        {
    //            workbook.Write(exportData);
    //            return exportData.ToArray();
    //        }

    //    }
    //    catch (Exception)
    //    {
    //        throw;
    //    }
    //}

    public async Task<byte[]> GeneratePdfAsync(int outwardId)
    {
        try
        {
            var data = await GetOutwardPdfDataAsync(outwardId);

            if (data == null)
                throw new Exception("Outward data not found.");

            using var stream = new MemoryStream();

            var document = new Document(PageSize.A4, 25, 25, 30, 30);
            PdfWriter.GetInstance(document, stream);

            document.Open();

            // Fonts
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            // Title
            var title = new Paragraph("Outward Report", titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(title);

            // Outward basic details table
            PdfPTable infoTable = new PdfPTable(2);
            infoTable.WidthPercentage = 100;
            infoTable.SetWidths(new float[] { 30, 70 });
            infoTable.SpacingAfter = 20;

            AddInfoRow(infoTable, "Outward No", data.OutwardNo, headerFont, normalFont);
            AddInfoRow(infoTable, "Outward Date", data.OutwardDate.ToString("dd/MM/yyyy"), headerFont, normalFont);
            AddInfoRow(infoTable, "Outward Time", data.OutwardDate.ToString("HH:mm:ss"), headerFont, normalFont);
            AddInfoRow(infoTable, "Bill To", data.BillToCompanyName, headerFont, normalFont);
            AddInfoRow(infoTable, "Platform", data.platformName, headerFont, normalFont);
            AddInfoRow(infoTable, "Remark", data.Remarks, headerFont, normalFont);

            document.Add(infoTable);

            // Items table
            PdfPTable itemTable = new PdfPTable(5);
            itemTable.WidthPercentage = 100;
            itemTable.SetWidths(new float[] { 10, 50, 10, 10, 20 });

            AddHeaderCell(itemTable, "No.", headerFont);
            AddHeaderCell(itemTable, "Product", headerFont);
            AddHeaderCell(itemTable, "Unit", headerFont);
            AddHeaderCell(itemTable, "Box Qty", headerFont);
            AddHeaderCell(itemTable, "Item Qty", headerFont);


            if (data.OutwardItems != null && data.OutwardItems.Any())
            {
                int srNo = 1;

                foreach (var item in data.OutwardItems)
                {
                    AddBodyCell(itemTable, srNo.ToString(), normalFont, Element.ALIGN_CENTER);
                    AddBodyCell(itemTable, item.ProductName ?? "", normalFont, Element.ALIGN_LEFT);
                    AddBodyCell(itemTable, item.Unit ?? "", normalFont, Element.ALIGN_CENTER);
                    AddBodyCell(itemTable, item.BoxQuantity.ToString(), normalFont, Element.ALIGN_CENTER);
                    AddBodyCell(itemTable, item.Quantity.ToString(), normalFont, Element.ALIGN_CENTER);

                    srNo++;
                }
                var totalBoxQty = data.OutwardItems.Sum(x => x.BoxQuantity);
                var totalItemQty = data.OutwardItems.Sum(x => x.Quantity);

                PdfPCell totalLabelCell = new PdfPCell(new Phrase("Total", headerFont))
                {
                    Colspan = 3,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 6,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                itemTable.AddCell(totalLabelCell);

                PdfPCell totalBoxCell = new PdfPCell(new Phrase(totalBoxQty.ToString(), headerFont))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 6,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                itemTable.AddCell(totalBoxCell);

                PdfPCell totalItemCell = new PdfPCell(new Phrase(totalItemQty.ToString(), headerFont))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 6,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                itemTable.AddCell(totalItemCell);
            }
            else
            {
                PdfPCell noDataCell = new PdfPCell(new Phrase("No items found", normalFont))
                {
                    Colspan = 5,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 8
                };

                itemTable.AddCell(noDataCell);
            }

            document.Add(itemTable);

            document.Close();

            return stream.ToArray();
        }
        catch
        {
            throw;
        }
    }

    private static void AddInfoRow(
    PdfPTable table,
    string label,
    string? value,
    Font labelFont,
    Font valueFont)
    {
        PdfPCell labelCell = new PdfPCell(new Phrase(label, labelFont))
        {
            Padding = 6,
            BackgroundColor = BaseColor.LIGHT_GRAY
        };

        PdfPCell valueCell = new PdfPCell(new Phrase(value ?? "", valueFont))
        {
            Padding = 6
        };

        table.AddCell(labelCell);
        table.AddCell(valueCell);
    }

    private static void AddHeaderCell(PdfPTable table, string text, Font font)
    {
        PdfPCell cell = new PdfPCell(new Phrase(text, font))
        {
            HorizontalAlignment = Element.ALIGN_CENTER,
            Padding = 6,
            BackgroundColor = BaseColor.LIGHT_GRAY
        };

        table.AddCell(cell);
    }

    private static void AddBodyCell(
        PdfPTable table,
        string text,
        Font font,
        int alignment)
    {
        PdfPCell cell = new PdfPCell(new Phrase(text, font))
        {
            HorizontalAlignment = alignment,
            Padding = 5
        };

        table.AddCell(cell);
    }
}