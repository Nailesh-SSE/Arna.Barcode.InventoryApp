using InventoryManagement.Services.Models.ReportModels;

namespace InventoryManagement.Services.Interfaces;

public interface IReportService
{
    Task<InstockReportResult> GenerateInstockReportAsync(InstockFilter filter);
    Task<OutstockReportResult> GenerateOutstockReportAsync(OutstockFilter filter);
    Task<ReturnSaleReportResult> GenerateReturnSaleReportAsync(ReturnSaleFilter filter);
}