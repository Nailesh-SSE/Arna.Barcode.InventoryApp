using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models.ReportModels;
using Microsoft.EntityFrameworkCore;
using CoreReturnType = InventoryManagement.Core.Enums.ReturnType;

namespace InventoryManagement.Services.Services
{
    /// <summary>
    /// Return Sale report generator implementing Single Responsibility Principle
    /// Includes mock data implementation as requested
    /// </summary>
    public class ReturnSaleReportGenerator : IReturnSaleReportGenerator
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public ReturnSaleReportGenerator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        
        public async Task<ReturnSaleReportResult> GenerateReportAsync(ReturnSaleFilter filter)
        {
            var startTime = DateTime.UtcNow;
            
            var result = new ReturnSaleReportResult
            {
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
            
            try
            {
                // Get actual sale returns and combine with mock data
                var actualReturns = await GetActualReturns(filter);
                var mockReturns = GenerateMockReturns(filter);
                
                var allReturns = actualReturns.Concat(mockReturns).ToList();
                
                // Apply filters and map to report items
                result.Items = FilterAndMapToReportItems(allReturns, filter);
                
                // Calculate aggregates
                result.TotalReturnedQuantity = result.Items.Sum(i => i.Quantity);
                result.TotalReturnedAmount = result.Items.Sum(i => i.TotalAmount);
                result.UniqueCustomersCount = result.Items.Select(i => i.CustomerCompanyId).Distinct().Count();
                result.UniqueProductsCount = result.Items.Select(i => i.ProductId).Distinct().Count();
                result.AverageReturnAmount = result.Items.Count > 0 ? result.TotalReturnedAmount / result.Items.Count : 0;
                result.TotalRecords = result.Items.Count;
                
                // Calculate return statistics
                result.ReturnsByReason = result.Items
                    .Where(i => !string.IsNullOrEmpty(i.Reason))
                    .GroupBy(i => i.Reason!)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.TotalAmount));
                
                result.QuantityByReturnType = result.Items
                    .GroupBy(i => i.ReturnType)
                    .ToDictionary(g => g.Key, g => (int)g.Sum(i => i.Quantity));
                
                // Calculate return rate (mock calculation)
                var totalSales = 1000000m; // Mock total sales amount
                result.ReturnRate = totalSales > 0 ? (double)(result.TotalReturnedAmount / totalSales) * 100 : 0;
                
                // Apply grouping if requested
                if (filter.GroupByReason)
                {
                    result.Items = ApplyGroupingByReason(result.Items);
                }
                
                // Apply pagination
                if (filter.PageSize > 0)
                {
                    result.Items = result.Items
                        .Skip((filter.PageNumber - 1) * filter.PageSize)
                        .Take(filter.PageSize)
                        .ToList();
                }
                
                // Apply sorting
                result.Items = ApplySorting(result.Items, filter);
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Error generating report: {ex.Message}");
            }
            finally
            {
                result.GenerationTime = DateTime.UtcNow - startTime;
            }
            
            return result;
        }
        
        public async Task<bool> ValidateFilterAsync(ReturnSaleFilter filter)
        {
            // Validate date range
            if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            {
                if (filter.StartDate.Value > filter.EndDate.Value)
                    return false;
            }
            
            // Validate pagination
            if (filter.PageNumber < 1 || filter.PageSize < 0)
                return false;
            
            // Validate return type
            if (!string.IsNullOrEmpty(filter.ReturnType))
            {
                if (!Enum.TryParse<CoreReturnType>(filter.ReturnType, out _))
                    return false;
            }
            
            return true;
        }
        
        private async Task<List<SaleReturn>> GetActualReturns(ReturnSaleFilter filter)
        {
            var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
            
            var returnFilter = new Filters.SaleReturnReportFilter(filter);
            if (!returnFilter.IsValid())
            {
                return new List<SaleReturn>();
            }
            var salerturns = await saleReturnRepository.FindAsync(returnFilter.GetFilterExpression());
            return salerturns.ToList();
        }
        
        private List<SaleReturn> GenerateMockReturns(ReturnSaleFilter filter)
        {
            var mockReturns = new List<SaleReturn>();
            var random = new Random();
            var returnTypes = Enum.GetValues<CoreReturnType>();
            var reasons = new[] { "Defective Product", "Wrong Item", "Size Issue", "Color Mismatch", "Damaged in Transit", "Customer Dissatisfaction", "Expired Product" };
            var mockProducts = new[] { 1, 2, 3, 4, 5 }; // Mock product IDs
            var mockCompanies = new[] { 101, 102, 103, 104, 105 }; // Mock company IDs
            
            // Generate 20 mock returns
            for (int i = 0; i < 20; i++)
            {
                var returnDate = DateTime.UtcNow.AddDays(-random.Next(1, 90));
                
                // Apply date filter
                if (filter.StartDate.HasValue && returnDate < filter.StartDate.Value) continue;
                if (filter.EndDate.HasValue && returnDate > filter.EndDate.Value) continue;
                
                var returnType = returnTypes[random.Next(returnTypes.Length)];
                
                // Apply return type filter
                if (!string.IsNullOrEmpty(filter.ReturnType) && returnType.ToString() != filter.ReturnType) continue;
                
                var quantity = random.Next(1, 10);
                var unitPrice = (decimal)(random.NextDouble() * 500 + 50); // $50 to $550
                var totalAmount = quantity * unitPrice;
                
                // Apply high value filter
                if (filter.HighValueOnly && totalAmount < filter.HighValueThreshold) continue;
                
                mockReturns.Add(new SaleReturn
                {
                    Id = 10000 + i, // Mock IDs starting from 10000
                    ReturnNo = $"RET-MOCK-{DateTime.UtcNow:yyyyMMdd}-{i:D3}",
                    ReturnDate = returnDate,
                    BillToCompanyId = mockCompanies[random.Next(mockCompanies.Length)],
                    ReturnType = returnType,
                    BarcodeNo = $"MOCK-BAR-{random.Next(10000, 99999)}",
                    Quantity = quantity,
                    Reason = reasons[random.Next(reasons.Length)],
                    Remarks = "Mock return for testing purposes"
                });
            }
            
            return mockReturns;
        }
        
        private List<ReturnSaleReportItem> FilterAndMapToReportItems(List<SaleReturn> saleReturns, ReturnSaleFilter filter)
        {
            var items = new List<ReturnSaleReportItem>();
            var random = new Random();
            
            // Mock product and company data
            var mockProducts = new[]
            {
                new { Id = 1, Name = "Laptop Pro", SKU = "LAP-001", Category = "Electronics", UnitPrice = 1200m },
                new { Id = 2, Name = "Wireless Mouse", SKU = "MOU-002", Category = "Accessories", UnitPrice = 45m },
                new { Id = 3, Name = "USB Cable", SKU = "CAB-003", Category = "Accessories", UnitPrice = 15m },
                new { Id = 4, Name = "Monitor 24&quot;", SKU = "MON-004", Category = "Electronics", UnitPrice = 300m },
                new { Id = 5, Name = "Keyboard", SKU = "KEY-005", Category = "Accessories", UnitPrice = 80m }
            };
            
            var mockCompanies = new[]
            {
                new { Id = 101, Name = "Tech Corp", Code = "TC001" },
                new { Id = 102, Name = "Digital Solutions", Code = "DS002" },
                new { Id = 103, Name = "Innovation Ltd", Code = "IL003" },
                new { Id = 104, Name = "Global Systems", Code = "GS004" },
                new { Id = 105, Name = "Future Tech", Code = "FT005" }
            };
            
            foreach (var saleReturn in saleReturns)
            {
                // Apply additional filters
                if (!string.IsNullOrEmpty(filter.ReasonCodes))
                {
                    var reasons = filter.ReasonCodes.Split(',').Select(r => r.Trim()).ToList();
                    if (saleReturn.Reason == null || !reasons.Contains(saleReturn.Reason)) continue;
                }
                
                // Mock product assignment
                var product = mockProducts[random.Next(mockProducts.Length)];
                var company = mockCompanies.FirstOrDefault(c => c.Id == saleReturn.BillToCompanyId) ?? mockCompanies[0];
                
                var totalAmount = saleReturn.Quantity * product.UnitPrice;
                
                // Apply high value filter
                if (filter.HighValueOnly && totalAmount < filter.HighValueThreshold) continue;
                
                // Apply pending filter (mock status)
                if (filter.PendingOnly && random.Next(0, 10) > 3) continue; // 30% are pending
                
                items.Add(new ReturnSaleReportItem
                {
                    ReturnId = saleReturn.Id,
                    ReturnNumber = saleReturn.ReturnNo,
                    ReturnDate = saleReturn.ReturnDate,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    SKU = product.SKU,
                    CategoryName = product.Category,
                    Quantity = saleReturn.Quantity,
                    ReturnType = saleReturn.ReturnType.ToString(),
                    Reason = saleReturn.Reason ?? "No reason provided",
                    BarcodeNumber = saleReturn.BarcodeNo,
                    CustomerCompanyId = saleReturn.BillToCompanyId,
                    CustomerName = company.Name,
                    UnitPrice = product.UnitPrice,
                    TotalAmount = totalAmount,
                    OriginalInvoiceNumber = $"INV-{saleReturn.ReturnDate:yyyyMMdd}-{random.Next(1000, 9999)}",
                    OriginalSaleDate = saleReturn.ReturnDate.AddDays(-random.Next(1, 30)),
                    Status = filter.PendingOnly ? "Pending" : (random.Next(0, 10) > 2 ? "Processed" : "Pending"),
                    Remarks = saleReturn.Remarks
                });
            }
            
            return items;
        }
        
        private List<ReturnSaleReportItem> ApplyGroupingByReason(List<ReturnSaleReportItem> items)
        {
            return items
                .GroupBy(i => i.Reason)
                .Select(g => new ReturnSaleReportItem
                {
                    Reason = g.Key,
                    Quantity = g.Sum(i => i.Quantity),
                    TotalAmount = g.Sum(i => i.TotalAmount),
                    CustomerName = $"{g.Count()} Customers",
                    ProductName = $"{g.Count()} Products",
                    ReturnDate = g.Max(i => i.ReturnDate),
                    ReturnType = "Multiple"
                })
                .ToList();
        }
        
        private List<ReturnSaleReportItem> ApplySorting(List<ReturnSaleReportItem> items, ReturnSaleFilter filter)
        {
            if (string.IsNullOrEmpty(filter.SortBy))
                return items.OrderByDescending(i => i.ReturnDate).ToList();
            
            return filter.SortBy.ToLower() switch
            {
                "date" => filter.SortDescending 
                    ? items.OrderByDescending(i => i.ReturnDate).ToList()
                    : items.OrderBy(i => i.ReturnDate).ToList(),
                "productname" => filter.SortDescending 
                    ? items.OrderByDescending(i => i.ProductName).ToList()
                    : items.OrderBy(i => i.ProductName).ToList(),
                "customer" => filter.SortDescending 
                    ? items.OrderByDescending(i => i.CustomerName).ToList()
                    : items.OrderBy(i => i.CustomerName).ToList(),
                "quantity" => filter.SortDescending 
                    ? items.OrderByDescending(i => i.Quantity).ToList()
                    : items.OrderBy(i => i.Quantity).ToList(),
                "amount" => filter.SortDescending 
                    ? items.OrderByDescending(i => i.TotalAmount).ToList()
                    : items.OrderBy(i => i.TotalAmount).ToList(),
                "reason" => filter.SortDescending 
                    ? items.OrderByDescending(i => i.Reason).ToList()
                    : items.OrderBy(i => i.Reason).ToList(),
                _ => filter.SortDescending 
                    ? items.OrderByDescending(i => i.ReturnDate).ToList()
                    : items.OrderBy(i => i.ReturnDate).ToList()
            };
        }
    }
}