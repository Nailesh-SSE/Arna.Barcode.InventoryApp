using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface ISalesReportService
{
    Task<List<MonthlySalesModel>> GetMonthlySalesReportAsync(int year);
    Task<List<ProductSalesModel>> GetProductWiseSalesReportAsync(DateTime? startDate, DateTime? endDate);
    Task<List<CustomerSalesModel>> GetCustomerWiseSalesReportAsync(DateTime? startDate, DateTime? endDate);
    Task<SalesSummaryModel> GetSalesSummaryAsync(DateTime? startDate, DateTime? endDate);
    Task<List<DailySalesModel>> GetDailySalesReportAsync(DateTime startDate, DateTime endDate);
}