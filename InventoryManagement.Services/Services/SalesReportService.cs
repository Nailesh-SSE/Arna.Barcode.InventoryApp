using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace InventoryManagement.Services.Services;

public class SalesReportService : ISalesReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public SalesReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<MonthlySalesModel>> GetMonthlySalesReportAsync(int year)
    {
        var outwardRepository = _unitOfWork.GetRepository<Outward>();

        // Get all outwards with related data using FindAsync
        var allOutwards = await outwardRepository.FindAsync(o => o.OutwardDate.Year == year);

        // Since we can't include related entities directly, we'll need to load them separately
        var outwardIds = allOutwards.Select(o => o.Id).ToList();

        // Get outward details for these outwards
        var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();
        var outwardDetails = await outwardDetailRepository.FindAsync(od => outwardIds.Contains(od.OutwardId));

        // Get company information
        var companyRepository = _unitOfWork.GetRepository<Company>();
        var companyIds = allOutwards.Select(o => o.BillToCompanyId).Distinct().ToList();
        var companies = await companyRepository.FindAsync(c => companyIds.Contains(c.Id));

        // Group by month and calculate metrics
        var monthlyData = allOutwards
            .GroupBy(o => o.OutwardDate.Month)
            .Select(g => new
            {
                Month = g.Key,
                OrderCount = g.Count(),
                TotalItems = outwardDetails
                    .Where(od => g.Select(o => o.Id).Contains(od.OutwardId))
                    .Sum(od => od.Quantity),
                TotalRevenue = 0m, // Would need pricing data
                UniqueCustomers = g.Select(o => o.BillToCompanyId).Distinct().Count()
            })
            .ToList();

        var result = new List<MonthlySalesModel>();
        for (int month = 1; month <= 12; month++)
        {
            var monthData = monthlyData.FirstOrDefault(m => m.Month == month);
            result.Add(new MonthlySalesModel
            {
                Month = month,
                MonthName = DateTimeFormatInfo.CurrentInfo.GetMonthName(month),
                TotalOutwardCount = monthData?.OrderCount ?? 0,
                TotalItemsSold = monthData?.TotalItems ?? 0,
                TotalRevenue = monthData?.TotalRevenue ?? 0,
                UniqueCustomers = monthData?.UniqueCustomers ?? 0,
                AverageOrderValue = monthData?.OrderCount > 0 ?
                    (monthData?.TotalRevenue ?? 0) / monthData.OrderCount : 0
            });
        }

        return result;
    }

    public async Task<List<ProductSalesModel>> GetProductWiseSalesReportAsync(DateTime? startDate, DateTime? endDate)
    {
        var outwardRepository = _unitOfWork.GetRepository<Outward>();
        var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();
        var productRepository = _unitOfWork.GetRepository<Product>();

        // Get outwards within date range
        var outwardsQuery = await outwardRepository.FindAsync(o =>
            (!startDate.HasValue || o.OutwardDate >= startDate.Value) &&
            (!endDate.HasValue || o.OutwardDate <= endDate.Value));

        var outwardIds = outwardsQuery.Select(o => o.Id).ToList();

        // Get outward details for these outwards
        var outwardDetails = await outwardDetailRepository.FindAsync(od => outwardIds.Contains(od.OutwardId));

        // Get products
        var productIds = outwardDetails.Select(od => od.ProductId).Distinct().ToList();
        var products = await productRepository.FindAsync(p => productIds.Contains(p.Id));

        // Group by product and calculate metrics
        var productSales = outwardDetails
            .GroupBy(od => od.ProductId)
            .Select(g =>
            {
                var product = products.FirstOrDefault(p => p.Id == g.Key);
                return new ProductSalesModel
                {
                    ProductId = g.Key,
                    ProductName = product?.Name ?? "Unknown",
                    SKU = product?.SKU ?? string.Empty,
                    Category = product?.Category?.Name ?? string.Empty,
                    MakeCompany = product?.MakeCompany ?? string.Empty,
                    TotalQuantitySold = g.Sum(od => od.Quantity),
                    TotalRevenue = 0m, // Would need pricing data
                    AveragePrice = 0m, // Would need pricing data
                    TimesSold = g.Count(),
                    LastSoldDate = g.Max(od => outwardsQuery.First(o => o.Id == od.OutwardId).OutwardDate)
                };
            })
            .OrderByDescending(p => p.TotalQuantitySold)
            .ToList();

        return productSales;
    }

    public async Task<List<CustomerSalesModel>> GetCustomerWiseSalesReportAsync(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var outwardRepository = _unitOfWork.GetRepository<Outward>();
            var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();

            // Single query to get all required data
            var query = from o in outwardRepository.GetQueryable()
                        join od in outwardDetailRepository.GetQueryable() on o.Id equals od.OutwardId
                        where (!startDate.HasValue || o.OutwardDate >= startDate.Value) &&
                              (!endDate.HasValue || o.OutwardDate <= endDate.Value)
                        select new
                        {
                            o.BillToCompanyId,
                            o.BillToCompany.Name,
                            o.BillToCompany.Code,
                            o.Id,
                            o.OutwardDate,
                            od.ProductId,
                            od.Quantity
                        };

            var data = await query.ToListAsync();

            if (!data.Any())
                return new List<CustomerSalesModel>();

            // Process in memory
            var result = data
                .GroupBy(x => new { x.BillToCompanyId, x.Name, x.Code })
                .Select(g => new CustomerSalesModel
                {
                    CustomerId = g.Key.BillToCompanyId,
                    CustomerName = g.Key.Name ?? "Unknown",
                    CustomerCode = g.Key.Code ?? string.Empty,
                    CustomerType = "BillTo",
                    TotalOrders = g.Select(x => x.Id).Distinct().Count(),
                    TotalItemsPurchased = g.Sum(x => x.Quantity),
                    TotalSpent = 0m, // Add your pricing logic here
                    AverageOrderValue = 0m, // Add your pricing logic here
                    FirstOrderDate = g.Min(x => x.OutwardDate),
                    LastOrderDate = g.Max(x => x.OutwardDate),
                    TopProducts = g.GroupBy(x => x.ProductId)
                                  .OrderByDescending(pg => pg.Sum(p => p.Quantity))
                                  .Take(5)
                                  .Select(pg => pg.First().Name ?? "Unknown")
                                  .ToList()
                })
                .OrderByDescending(c => c.TotalItemsPurchased)
                .ToList();

            return result;
        }
        catch (Exception ex)
        {
            // Log exception here
            throw;
        }
    }

    public async Task<SalesSummaryModel> GetSalesSummaryAsync(DateTime? startDate, DateTime? endDate)
    {
        var outwardRepository = _unitOfWork.GetRepository<Outward>();
        var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();

        // Get outwards within date range
        var outwardsQuery = await outwardRepository.FindAsync(o =>
            (!startDate.HasValue || o.OutwardDate >= startDate.Value) &&
            (!endDate.HasValue || o.OutwardDate <= endDate.Value));

        var outwardIds = outwardsQuery.Select(o => o.Id).ToList();

        // Get outward details
        var outwardDetails = await outwardDetailRepository.FindAsync(od => outwardIds.Contains(od.OutwardId));

        var totalItemsSold = outwardDetails.Sum(od => od.Quantity);
        var uniqueProducts = outwardDetails.Select(od => od.ProductId).Distinct().Count();

        return new SalesSummaryModel
        {
            TotalOrders = outwardsQuery.Count(),
            TotalItemsSold = totalItemsSold,
            TotalRevenue = 0m, // Would need pricing data
            UniqueCustomers = outwardsQuery.Select(o => o.BillToCompanyId).Distinct().Count(),
            AverageOrderValue = outwardsQuery.Count() > 0 ? 0m : 0m, // Would need pricing data
            UniqueProductsSold = uniqueProducts,
            PeriodStart = startDate,
            PeriodEnd = endDate
        };
    }

    public async Task<List<DailySalesModel>> GetDailySalesReportAsync(DateTime startDate, DateTime endDate)
    {
        var outwardRepository = _unitOfWork.GetRepository<Outward>();
        var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();

        // Get outwards within date range
        var outwardsQuery = await outwardRepository.FindAsync(o =>
            o.OutwardDate.Date >= startDate.Date && o.OutwardDate.Date <= endDate.Date);

        var outwardIds = outwardsQuery.Select(o => o.Id).ToList();

        // Get outward details
        var outwardDetails = await outwardDetailRepository.FindAsync(od => outwardIds.Contains(od.OutwardId));

        // Group by date and calculate daily metrics
        var dailyData = outwardsQuery
            .GroupBy(o => o.OutwardDate.Date)
            .Select(g =>
            {
                var dayOutwardIds = g.Select(o => o.Id).ToList();
                var dayOutwardDetails = outwardDetails.Where(od => dayOutwardIds.Contains(od.OutwardId)).ToList();

                return new DailySalesModel
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    ItemsSold = dayOutwardDetails.Sum(od => od.Quantity),
                    Revenue = 0m, // Would need pricing data
                    AverageOrderValue = g.Count() > 0 ? 0m : 0m // Would need pricing data
                };
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Fill in missing dates with zero values
        var result = new List<DailySalesModel>();
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var dayData = dailyData.FirstOrDefault(d => d.Date == date);
            result.Add(dayData ?? new DailySalesModel
            {
                Date = date,
                OrderCount = 0,
                ItemsSold = 0,
                Revenue = 0,
                AverageOrderValue = 0
            });
        }

        return result;
    }
}