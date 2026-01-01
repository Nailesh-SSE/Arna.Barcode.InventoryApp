using InventoryManagement.Services.Models;
using InventoryManagement.Services.Models.ReportModels;

namespace InventoryManagement.Services.Reports.Interfaces;

public interface IBarcodeTrackingReportService 
{
    Task<BarCodeReportResult> GenerateBarcodeTarckReportAsync(BarcodeFilter filter);
    Task<BarcodeValidationResult> ValidateBarcode(string barcodeNo);
}
