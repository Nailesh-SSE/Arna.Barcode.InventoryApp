using InventoryManagement.Core.Entities.SP_Entities;
using InventoryManagement.Services.Models.ReportModels;

namespace InventoryManagement.Services.Reports.Interfaces
{
    public interface IInventoryReportService
    {
        Task<InventoryReportResult> GenerateInventoryReportAsync(InventoryReportFilter filter);
        Task<byte[]> ExportToExcelAsync(InventoryReportFilter filter);
    }
}
