using InventoryManagement.Services.Models.ReportModels;

namespace InventoryManagement.Services.Interfaces;
public interface IOutwardReportService
{
    Task<OutwardReportResult> GenerateOutwardReportAsync(OutwardFilter filter);
    Task<byte[]> ExportToExcelAsync(OutwardFilter filter);
}

