using InventoryManagement.Services.Models.ReportModels;

namespace InventoryManagement.Services.Interfaces
{
    public interface IInstockReportGenerator : IReportGenerator<InstockFilter, InstockReportResult>
    {
    }
    
    public interface IOutstockReportGenerator : IReportGenerator<OutstockFilter, OutstockReportResult>
    {
    }
    
    public interface IReturnSaleReportGenerator : IReportGenerator<ReturnSaleFilter, ReturnSaleReportResult>
    {
    }
}