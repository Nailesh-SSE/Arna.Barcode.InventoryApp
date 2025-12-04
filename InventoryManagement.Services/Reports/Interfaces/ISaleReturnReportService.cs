using InventoryManagement.Services.Models.ReportModels;

namespace InventoryManagement.Services.Interfaces;

public interface ISaleReturnReportService
{
    Task<SaleReturnReportResult> GenerateSaleReturnReportAsync(SaleReturnFilter filter);
}

