using InventoryManagement.Services.Models.ReportModels;

namespace InventoryManagement.Services.Interfaces;

public interface IInwardReportService
{
    Task<InwardReportResult> GenerateInwardReportAsync(InwardFilter filter);
    Task<InwardReportResult> GetInwardsWithPagingAsync(InwardFilter filter);
    Task<InwardReportSummary> GetInwardSummaryAsync(InwardFilter filter);
    Task<byte[]> ExportToExcelAsync(InwardFilter filter);
    Task<byte[]> ExportToPdfAsync(InwardFilter filter);
    Task<List<InwardReportItem>> GetTopProductsAsync(int count, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<InwardReportItem>> GetRecentInwardsAsync(int count);
}