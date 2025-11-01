//using InventoryManagement.Core.Entities;
//using InventoryManagement.Infrastructure.Repositories;
//using InventoryManagement.Services.Interfaces;
//using InventoryManagement.Services.Models;
//using Microsoft.EntityFrameworkCore;

//namespace InventoryManagement.Services.Services;

//public class SalesReportService : ISalesReportService
//{
//    private readonly IUnitOfWork _unitOfWork;

//    public SalesReportService(IUnitOfWork unitOfWork)
//    {
//        _unitOfWork = unitOfWork;
//    }

//    public async Task<List<MonthlySalesModel>> GetMonthlySalesReportAsync(int year)
//    {
//        var outwards = await _unitOfWork.GetRepository<Outward>()
//            .GetAllIncludingAsync(o => o.BillToCompany, o => o.OutwardDetails);

//        var monthlyData = outwards
//            .Where(o => o.OutwardDate.Year == year)
//            .GroupBy(o => o.OutwardDate.Month)
//            .Select(g => new
//            {
//                Month = g.Key,
//                OrderCount = g.Count(),
//                TotalItems = g.SumMany(o => o.OutwardDetails.Sum(od => od.Quantity)),
//                TotalRevenue = 0m, // Would need pricing data
//                UniqueCustomers = g.Select(o => o.BillToCompanyId).Distinct().Count()
//            })
//            .ToList();

//        var result = new List<MonthlySalesModel>();
//        for (int month = 1; month <= 12; month++)
//        {
//            var monthData = monthlyData.FirstOrDefault(m => m.Month == month);
//            result.Add(new MonthlySalesModel
//            {
//                Month = month,
//                MonthName = DateTimeFormatInfo.CurrentInfo.GetMonthName(month),
//                TotalOutwardCount = monthData?.OrderCount ?? 0,
//                TotalItemsSold = monthData?.TotalItems ?? 0,
//                TotalRevenue = monthData?.TotalRevenue ?? 0,
//                UniqueCustomers = monthData?.UniqueCustomers ?? 0,
//                AverageOrderValue = monthData?.OrderCount > 0 ? 
//                    (monthData?.TotalRevenue ?? 0) / monthData.OrderCount : 0
//            });
//        }

//        return result;
//    }

//    public async Task<List<ProductSalesModel>> GetProductWiseSalesReportAsync(DateTime? startDate, DateTime? endDate)
//    {
//        var outwardDetails = await _unitOfWork.Repository<OutwardDetail>()
//            .GetAllIncludingAsync(od => od.Outward, od => od.Product, 
//                od => od.Product.Category, od => od.Product.MakeCompany);

//        var query = outwardDetails.AsQueryable();

//        if (startDate.HasValue)
//            query = query.Where(od => od.Outward.OutwardDate >= startDate.Value);

//        if (endDate.HasValue)
//            query = query.Where(od => od.Outward.OutwardDate <= endDate.Value);

//        return query
//            .GroupBy(od => od.ProductId)
//            .Select(g => new ProductSalesModel
//            {
//                ProductId = g.Key,
//                ProductName = g.First().Product.Name,
//                SKU = g.First().Product.SKU,
//                Category = g.First().Product.Category.Name,
//                MakeCompany = g.First().Product.MakeCompany,
//                TotalQuantitySold = g.Sum(od => od.Quantity),
//                TotalRevenue = 0m, // Would need pricing data
//                AveragePrice = 0m, // Would need pricing data
//                TimesSold = g.Count(),
//                LastSoldDate = g.Max(od => od.Outward.OutwardDate)
//            })
//            .OrderByDescending(p => p.TotalQuantitySold)
//            .ToList();
//    }

//    public async Task<List<CustomerSalesModel>> GetCustomerWiseSalesReportAsync(DateTime? startDate, DateTime? endDate)
//    {
//        var outwards = await _unitOfWork.Repository<Outward>()
//            .GetAllIncludingAsync(o => o.BillToCompany, o => o.OutwardDetails);

//        var query = outwards.AsQueryable();

//        if (startDate.HasValue)
//            query = query.Where(o => o.OutwardDate >= startDate.Value);

//        if (endDate.HasValue)
//            query = query.Where(o => o.OutwardDate <= endDate.Value);

//        var customerData = query
//            .GroupBy(o => o.BillToCompanyId)
//            .Select(g => new
//            {
//                CustomerId = g.Key,
//                CustomerName = g.First().BillToCompany.Name,
//                CustomerCode = g.First().BillToCompany.Code,
//                TotalOrders = g.Count(),
//                TotalItems = g.SumMany(o => o.OutwardDetails.Sum(od => od.Quantity)),
//                TotalRevenue = 0m, // Would need pricing data
//                FirstOrderDate = g.Min(o => o.OutwardDate),
//                LastOrderDate = g.Max(o => o.OutwardDate),
//                Products = g.SelectMany(o => o.OutwardDetails).GroupBy(od => od.ProductId)
//                            .OrderByDescending(pg => pg.Sum(pd => pd.Quantity))
//                            .Take(5)
//                            .Select(pg => pg.First().Outward.Product.Name)
//                            .ToList()
//            })
//            .ToList();

//        return customerData
//            .Select(c => new CustomerSalesModel
//            {
//                CustomerId = c.CustomerId,
//                CustomerName = c.CustomerName,
//                CustomerCode = c.CustomerCode,
//                CustomerType = "BillTo",
//                TotalOrders = c.TotalOrders,
//                TotalItemsPurchased = c.TotalItems,
//                TotalSpent = c.TotalRevenue,
//                AverageOrderValue = c.TotalOrders > 0 ? c.TotalRevenue / c.TotalOrders : 0,
//                FirstOrderDate = c.FirstOrderDate,
//                LastOrderDate = c.LastOrderDate,
//                TopProducts = c.Products
//            })
//            .OrderByDescending(c => c.TotalSpent)
//            .ToList();
//    }

//    public async Task<SalesSummaryModel> GetSalesSummaryAsync(DateTime? startDate, DateTime? endDate)
//    {
//        var outwards = await _unitOfWork.Repository<Outward>()
//            .GetAllIncludingAsync(o => o.BillToCompany, o => o.OutwardDetails);

//        var query = outwards.AsQueryable();

//        if (startDate.HasValue)
//            query = query.Where(o => o.OutwardDate >= startDate.Value);

//        if (endDate.HasValue)
//            query = query.Where(o => o.OutwardDate <= endDate.Value);

//        var salesData = await query.ToListAsync();

//        return new SalesSummaryModel
//        {
//            TotalOrders = salesData.Count,
//            TotalItemsSold = salesData.SumMany(o => o.OutwardDetails.Sum(od => od.Quantity)),
//            TotalRevenue = 0m, // Would need pricing data
//            UniqueCustomers = salesData.Select(o => o.BillToCompanyId).Distinct().Count(),
//            AverageOrderValue = salesData.Count > 0 ? 0m : 0m, // Would need pricing data
//            UniqueProductsSold = salesData.SelectMany(o => o.OutwardDetails).Select(od => od.ProductId).Distinct().Count(),
//            PeriodStart = startDate,
//            PeriodEnd = endDate
//        };
//    }

//    public async Task<List<DailySalesModel>> GetDailySalesReportAsync(DateTime startDate, DateTime endDate)
//    {
//        var outwards = await _unitOfWork.Repository<Outward>()
//            .GetAllIncludingAsync(o => o.OutwardDetails);

//        var dailyData = outwards
//            .Where(o => o.OutwardDate.Date >= startDate.Date && o.OutwardDate.Date <= endDate.Date)
//            .GroupBy(o => o.OutwardDate.Date)
//            .Select(g => new DailySalesModel
//            {
//                Date = g.Key,
//                OrderCount = g.Count(),
//                ItemsSold = g.SumMany(o => o.OutwardDetails.Sum(od => od.Quantity)),
//                Revenue = 0m, // Would need pricing data
//                AverageOrderValue = g.Count() > 0 ? 0m : 0m // Would need pricing data
//            })
//            .OrderBy(d => d.Date)
//            .ToList();

//        // Fill in missing dates with zero values
//        var result = new List<DailySalesModel>();
//        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
//        {
//            var dayData = dailyData.FirstOrDefault(d => d.Date == date);
//            result.Add(dayData ?? new DailySalesModel
//            {
//                Date = date,
//                OrderCount = 0,
//                ItemsSold = 0,
//                Revenue = 0,
//                AverageOrderValue = 0
//            });
//        }

//        return result;
//    }
//}

//// Extension method for Sum on nested collections
//public static class EnumerableExtensions
//{
//    public static int SumMany<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
//    {
//        return source.Sum(selector);
//    }
//}