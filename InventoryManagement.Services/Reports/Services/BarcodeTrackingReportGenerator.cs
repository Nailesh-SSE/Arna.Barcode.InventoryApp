using InventoryManagement.Core.Data;
using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Entities.SP_Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Models;
using InventoryManagement.Services.Models.ReportModels;
using InventoryManagement.Services.Reports.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace InventoryManagement.Services.Reports.Services
{
    public class BarcodeTrackingReportGenerator : IBarcodeTrackingReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly InventoryDbContext _db;

        public BarcodeTrackingReportGenerator(IUnitOfWork unitOfWork, InventoryDbContext db)
        {
            _db = db;
            _unitOfWork = unitOfWork;
        }

        public async Task<BarCodeReportResult> GenerateBarcodeTarckReportAsync(BarcodeFilter filter)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {


                var items = await Call_sp_BarcodeTracking(filter.barcode);

                // Apply paging
                var pagedItems = items.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToList();

                stopwatch.Stop();

                var result = new BarCodeReportResult
                {
                    Items = items
                };
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating Barcode Tracking report", ex);
            }
        }


        private async Task<List<BarcodeTrackDTO>> Call_sp_BarcodeTracking(string barcode)
        {
            try
            {
                var result = new List<BarcodeTrackDTO>();
                var barcodeParam = new SqlParameter("@ItemBarcodeNo", SqlDbType.NVarChar) { Value = barcode ?? (object)DBNull.Value };

                result = await _db.BarcodeTrackDTO.FromSqlRaw("EXEC sp_BarcodeTracking @ItemBarcodeNo", barcodeParam)
                    .AsNoTracking()
                    .ToListAsync();
                return result;
            }
            catch
            {
                throw;
            }
        }
        public async Task<BarcodeValidationResult> ValidateBarcode(string barcodeNo)
        {
            var barcodeItemRepository = _unitOfWork.GetRepository<InwardBarcodeItem>();
            var barcodeItems = await barcodeItemRepository.FindAsync(bi =>
                bi.BarcodeNo == barcodeNo);

            var barcodeItem = barcodeItems.FirstOrDefault();

            if (barcodeItem == null)
            {
                return new BarcodeValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid barcode. Barcode does not exist in inventory."
                };
            }

            return new BarcodeValidationResult { IsValid = true };
        }
    }
}

