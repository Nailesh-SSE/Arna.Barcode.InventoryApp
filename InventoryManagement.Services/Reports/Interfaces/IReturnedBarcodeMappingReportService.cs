using InventoryManagement.Services.Models.ReportModels;

namespace InventoryManagement.Services.Reports.Interfaces;

public interface IReturnedBarcodeMappingReportService
{
    Task<ReturnBarcodeMappingReportResult> GenerateReturnBarcodeMappingReportAsync(SaleReturnFilter filter);
    Task<byte[]> ExportToExcelAsync(SaleReturnFilter filter);

}
