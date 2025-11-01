//using InventoryManagement.Core.Entities;
//using InventoryManagement.Infrastructure.Repositories;
//using InventoryManagement.Services.Interfaces;
//using InventoryManagement.Services.Models;
//using Microsoft.EntityFrameworkCore;

//namespace InventoryManagement.Services.Services;

//public class DashboardService : IDashboardService
//{
//    private readonly IUnitOfWork _unitOfWork;

//    public DashboardService(IUnitOfWork unitOfWork)
//    {
//        _unitOfWork = unitOfWork;
//    }

//    public async Task<DashboardSummaryModel> GetDashboardSummaryAsync()
//    {
//        var products = await _unitOfWork.Repository<Product>().GetAllAsync();
//        var inwards = await _unitOfWork.Repository<Inward>().GetAllAsync();
//        var outwards = await _unitOfWork.Repository<Outward>().GetAllAsync();
//        var companies = await _unitOfWork.Repository<Company>().GetAllAsync();

//        return new DashboardSummaryModel
//        {
//            TotalProducts = products.Count(),
//            TotalInwardEntries = inwards.Count(),
//            TotalOutwardEntries = outwards.Count(),
//            ActiveCompanies = companies.Count(),
//            LowStockProducts = await GetLowStockCountAsync(),
//            TotalInventoryValue = await CalculateInventoryValueAsync(),
//            MonthlyTransactions = await GetMonthlyTransactionsAsync(),
//            TopMovingProducts = await GetTopMovingProductsAsync()
//        };
//    }

//    public async Task<List<InwardSummaryModel>> GetRecentInwardTransactionsAsync(int count = 10)
//    {
//        var inwards = await _unitOfWork.Repository<Inward>()
//            .GetAllIncludingAsync(i => i.ShipMentCompany, i => i.InwardItems);

//        return inwards
//            .OrderByDescending(i => i.InwardDate)
//            .Take(count)
//            .Select(i => new InwardSummaryModel
//            {
//                Id = i.Id,
//                InwardNo = i.InwardNo,
//                InwardDate = i.InwardDate,
//                ShipMentCompany = i.ShipMentCompany.Name,
//                TotalItems = i.InwardItems.Sum(item => item.Quantity),
//                Status = "Completed"
//            })
//            .ToList();
//    }

//    public async Task<List<OutwardSummaryModel>> GetRecentOutwardTransactionsAsync(int count = 10)
//    {
//        var outwards = await _unitOfWork.Repository<Outward>()
//            .GetAllIncludingAsync(o => o.BillToCompany, o => o.OutwardDetails);

//        return outwards
//            .OrderByDescending(o => o.OutwardDate)
//            .Take(count)
//            .Select(o => new OutwardSummaryModel
//            {
//                Id = o.Id,
//                OutwardNo = o.OutwardNo,
//                OutwardDate = o.OutwardDate,
//                BillToCompany = o.BillToCompany.Name,
//                TotalItems = o.OutwardDetails.Sum(detail => detail.Quantity),
//                Status = "Completed"
//            })
//            .ToList();
//    }

//    public async Task<List<ProductStockModel>> GetLowStockProductsAsync(int threshold = 10)
//    {
//        // Calculate current stock for each product
//        var inwardItems = await _unitOfWork.Repository<InwardItem>().GetAllAsync();
//        var outwardDetails = await _unitOfWork.Repository<OutwardDetail>().GetAllAsync();
//        var products = await _unitOfWork.Repository<Product>().GetAllIncludingAsync(p => p.Category);

//        var stockLevels = inwardItems
//            .GroupBy(i => i.ProductId)
//            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

//        var soldQuantities = outwardDetails
//            .GroupBy(o => o.ProductId)
//            .ToDictionary(g => g.Key, g => g.Sum(o => o.Quantity));

//        return products
//            .Select(p => new ProductStockModel
//            {
//                Id = p.Id,
//                Name = p.Name,
//                SKU = p.SKU,
//                Category = p.Category.Name,
//                CurrentStock = stockLevels.GetValueOrDefault(p.Id, 0) - soldQuantities.GetValueOrDefault(p.Id, 0),
//                ReorderLevel = threshold,
//                SoldQuantity = soldQuantities.GetValueOrDefault(p.Id, 0)
//            })
//            .Where(p => p.CurrentStock <= threshold && p.CurrentStock >= 0)
//            .OrderBy(p => p.CurrentStock)
//            .ToList();
//    }

//    public async Task<InventoryStatsModel> GetInventoryStatsAsync()
//    {
//        var categories = await _unitOfWork.Repository<Category>().GetAllAsync();
//        var companies = await _unitOfWork.Repository<Company>().GetAllAsync();
//        var colors = await _unitOfWork.Repository<Colour>().GetAllAsync();
//        var products = await _unitOfWork.Repository<Product>().GetAllAsync();
//        var inwards = await _unitOfWork.Repository<Inward>().GetAllAsync();
//        var outwards = await _unitOfWork.Repository<Outward>().GetAllAsync();

//        var today = DateTime.Today;
//        var thisMonthStart = new DateTime(today.Year, today.Month, 1);

//        return new InventoryStatsModel
//        {
//            TotalCategories = categories.Count(),
//            TotalCompanies = companies.Count(),
//            TotalColors = colors.Count(),
//            UniqueProducts = products.Count(),
//            TodayInward = inwards.Count(i => i.InwardDate.Date == today),
//            TodayOutward = outwards.Count(o => o.OutwardDate.Date == today),
//            ThisMonthInward = inwards.Count(i => i.InwardDate >= thisMonthStart),
//            ThisMonthOutward = outwards.Count(o => o.OutwardDate >= thisMonthStart)
//        };
//    }

//    private async Task<int> GetLowStockCountAsync()
//    {
//        var lowStockProducts = await GetLowStockProductsAsync();
//        return lowStockProducts.Count;
//    }

//    private async Task<decimal> CalculateInventoryValueAsync()
//    {
//        // This would need price information to calculate accurately
//        // For now, return a placeholder
//        return 0;
//    }

//    private async Task<List<MonthlyTransactionModel>> GetMonthlyTransactionsAsync()
//    {
//        var inwards = await _unitOfWork.Repository<Inward>().GetAllAsync();
//        var outwards = await _unitOfWork.Repository<Outward>().GetAllAsync();

//        var monthlyData = inwards
//            .GroupBy(i => new { i.InwardDate.Year, i.InwardDate.Month })
//            .Join(outwards
//                .GroupBy(o => new { o.OutwardDate.Year, o.OutwardDate.Month }),
//                i => i.Key,
//                o => o.Key,
//                (i, o) => new MonthlyTransactionModel
//                {
//                    Month = $"{i.Key.Year}-{i.Key.Month:D2}",
//                    InwardCount = i.Count(),
//                    OutwardCount = o.Count(),
//                    InwardValue = 0, // Would need pricing data
//                    OutwardValue = 0  // Would need pricing data
//                })
//            .OrderByDescending(m => m.Month)
//            .Take(12)
//            .ToList();

//        return monthlyData;
//    }

//    private async Task<List<ProductMovementModel>> GetTopMovingProductsAsync()
//    {
//        var outwardDetails = await _unitOfWork.Repository<OutwardDetail>().GetAllAsync();
//        var products = await _unitOfWork.Repository<Product>().GetAllAsync();

//        return outwardDetails
//            .GroupBy(od => od.ProductId)
//            .Join(products,
//                g => g.Key,
//                p => p.Id,
//                (g, p) => new ProductMovementModel
//                {
//                    ProductName = p.Name,
//                    SKU = p.SKU,
//                    TotalSold = g.Sum(od => od.Quantity),
//                    TotalRevenue = 0 // Would need pricing data
//                })
//            .OrderByDescending(pm => pm.TotalSold)
//            .Take(10)
//            .ToList();
//    }
//}