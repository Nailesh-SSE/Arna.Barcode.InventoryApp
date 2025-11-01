using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryModel> GetDashboardSummaryAsync();
    Task<List<InwardSummaryModel>> GetRecentInwardTransactionsAsync(int count = 10);
    Task<List<OutwardSummaryModel>> GetRecentOutwardTransactionsAsync(int count = 10);
    Task<List<ProductStockModel>> GetLowStockProductsAsync(int threshold = 10);
    Task<InventoryStatsModel> GetInventoryStatsAsync();
}