using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models.ReportModels;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services.Services
{
    /// <summary>
    /// Outstock report generator implementing Single Responsibility Principle
    /// </summary>
    public class OutstockReportGenerator : IOutstockReportGenerator
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public OutstockReportGenerator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        
        public async Task<OutstockReportResult> GenerateReportAsync(OutstockFilter filter)
        {
            var startTime = DateTime.UtcNow;
            
            var result = new OutstockReportResult
            {
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
            
            try
            {
                // Get outwards with related data
                var outwardRepository = _unitOfWork.GetRepository<Outward>();
                var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();
                var productRepository = _unitOfWork.GetRepository<Product>();
                var categoryRepository = _unitOfWork.GetRepository<Category>();
                var companyRepository = _unitOfWork.GetRepository<Company>();
                
                // Apply base filters
                var outwardFilter = new Filters.OutwardReportFilter(filter);
                if (!outwardFilter.IsValid())
                {
                    result.Warnings.Add("Invalid filter criteria provided");
                    return result;
                }
                
                var outwards = await outwardRepository.FindAsync(outwardFilter.GetFilterExpression());
                var outwardIds = outwards.Select(o => o.Id).ToList();
                
                // Get outward details
                var outwardDetails = await outwardDetailRepository.FindAsync(od => outwardIds.Contains(od.OutwardId));
                
                // Get related data
                var productIds = outwardDetails.Select(od => od.ProductId).Distinct().ToList();
                var products = await productRepository.FindAsync(p => productIds.Contains(p.Id));
                var categoryIds = products.Select(p => p.CategoryId).Distinct().ToList();
                var categories = await categoryRepository.FindAsync(c => categoryIds.Contains(c.Id));
                var companyIds = outwards.Select(o => o.BillToCompanyId).Distinct().ToList();
                var companies = await companyRepository.FindAsync(c => companyIds.Contains(c.Id));
                
                // Apply additional filters and map to report items
                result.Items = await FilterAndMapToReportItems(
                    outwards.ToList(), outwardDetails.ToList(), products.ToList(), categories.ToList(),
                    companies.ToList(), filter);
                
                // Calculate aggregates
                result.TotalQuantity = result.Items.Sum(i => i.Quantity);
                result.TotalRevenue = result.Items.Sum(i => i.TotalPrice);
                result.UniqueCustomersCount = result.Items.Select(i => i.CustomerCompanyId).Distinct().Count();
                result.UniqueProductsCount = result.Items.Select(i => i.ProductId).Distinct().Count();
                result.TotalRecords = result.Items.Count;
                
                // Calculate category-wise statistics
                result.SalesByCategory = result.Items
                    .GroupBy(i => i.CategoryName)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.TotalPrice));
                
                result.QuantityByProduct = result.Items
                    .GroupBy(i => i.ProductName)
                    .ToDictionary(g => g.Key, g => (int)g.Sum(i => i.Quantity));
                
                // Apply grouping if requested
                if (filter.GroupByProduct || filter.GroupByCustomer)
                {
                    result.Items = ApplyGrouping(result.Items, filter);
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
        
        public async Task<bool> ValidateFilterAsync(OutstockFilter filter)
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
            
            // Validate quantity ranges
            if (filter.MinQuantity.HasValue && filter.MaxQuantity.HasValue)
            {
                if (filter.MinQuantity.Value > filter.MaxQuantity.Value)
                    return false;
            }
            
            return true;
        }
        
        private async Task<List<OutstockReportItem>> FilterAndMapToReportItems(
            List<Outward> outwards,
            List<OutwardDetail> outwardDetails,
            List<Product> products,
            List<Category> categories,
            List<Company> companies,
            OutstockFilter filter)
        {
            var items = new List<OutstockReportItem>();
            
            foreach (var outward in outwards)
            {
                var company = companies.FirstOrDefault(c => c.Id == outward.BillToCompanyId);
                var details = outwardDetails.Where(od => od.OutwardId == outward.Id).ToList();
                
                foreach (var detail in details)
                {
                    var product = products.FirstOrDefault(p => p.Id == detail.ProductId);
                    if (product == null) continue;
                    
                    var category = categories.FirstOrDefault(c => c.Id == product.CategoryId);
                    
                    // Apply additional filters
                    if (filter.MinQuantity.HasValue && detail.Quantity < filter.MinQuantity.Value) continue;
                    if (filter.MaxQuantity.HasValue && detail.Quantity > filter.MaxQuantity.Value) continue;
                    
                    // Filter by customer companies
                    if (!string.IsNullOrEmpty(filter.CustomerCompanyIds))
                    {
                        var customerIds = filter.CustomerCompanyIds.Split(',').Select(id => int.Parse(id.Trim())).ToList();
                        if (!customerIds.Contains(outward.BillToCompanyId)) continue;
                    }
                    
                    // Filter by sales channel
                    if (!string.IsNullOrEmpty(filter.SalesChannel) && 
                        detail.BarcodeNo != filter.SalesChannel) // Mock logic
                    {
                        continue;
                    }
                    
                    items.Add(new OutstockReportItem
                    {
                        OutwardId = outward.Id,
                        OutwardNumber = outward.OutwardNo,
                        OutwardDate = outward.OutwardDate,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        SKU = product.SKU,
                        CategoryName = category?.Name ?? "Unknown",
                        Quantity = detail.Quantity,
                        Unit = detail.Unit,
                        CustomerCompanyId = outward.BillToCompanyId,
                        CustomerName = company?.Name ?? "Unknown",
                        CustomerCode = company?.Code ?? string.Empty,
                        BarcodeNumber = detail.BarcodeNo,
                        UnitPrice = 150m, // Mock data
                        TotalPrice = detail.Quantity * 150m, // Mock calculation
                        SalesChannel = filter.SalesChannel ?? "Retail", // Mock data
                        Remarks = outward.Remarks
                    });
                }
            }
            
            return items;
        }
        
        private List<OutstockReportItem> ApplyGrouping(List<OutstockReportItem> items, OutstockFilter filter)
        {
            if (filter.GroupByProduct)
            {
                return items
                    .GroupBy(i => new { i.ProductId, i.ProductName, i.SKU, i.CategoryName })
                    .Select(g => new OutstockReportItem
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName,
                        SKU = g.Key.SKU,
                        CategoryName = g.Key.CategoryName,
                        Quantity = g.Sum(i => i.Quantity),
                        Unit = "PCS",
                        UnitPrice = g.Average(i => i.UnitPrice),
                        TotalPrice = g.Sum(i => i.TotalPrice),
                        OutwardDate = g.Max(i => i.OutwardDate),
                        CustomerName = $"{g.Count()} Customers"
                    })
                    .ToList();
            }
            
            if (filter.GroupByCustomer)
            {
                return items
                    .GroupBy(i => new { i.CustomerCompanyId, i.CustomerName, i.CustomerCode })
                    .Select(g => new OutstockReportItem
                    {
                        CustomerCompanyId = g.Key.CustomerCompanyId,
                        CustomerName = g.Key.CustomerName,
                        CustomerCode = g.Key.CustomerCode,
                        Quantity = g.Sum(i => i.Quantity),
                        UnitPrice = g.Average(i => i.UnitPrice),
                        TotalPrice = g.Sum(i => i.TotalPrice),
                        OutwardDate = g.Max(i => i.OutwardDate),
                        ProductName = $"{g.Count()} Products",
                        CategoryName = "Multiple"
                    })
                    .ToList();
            }
            
            return items;
        }
        
        private List<OutstockReportItem> ApplySorting(List<OutstockReportItem> items, OutstockFilter filter)
        {
            if (string.IsNullOrEmpty(filter.SortBy))
                return items.OrderByDescending(i => i.OutwardDate).ToList();
            
            return filter.SortBy.ToLower() switch
            {
                "date" => filter.SortDescending 
                    ? items.OrderByDescending(i => i.OutwardDate).ToList()
                    : items.OrderBy(i => i.OutwardDate).ToList(),
                "productname" => filter.SortDescending 
                    ? items.OrderByDescending(i => i.ProductName).ToList()
                    : items.OrderBy(i => i.ProductName).ToList(),
                "customer" => filter.SortDescending 
                    ? items.OrderByDescending(i => i.CustomerName).ToList()
                    : items.OrderBy(i => i.CustomerName).ToList(),
                "quantity" => filter.SortDescending 
                    ? items.OrderByDescending(i => i.Quantity).ToList()
                    : items.OrderBy(i => i.Quantity).ToList(),
                "revenue" => filter.SortDescending 
                    ? items.OrderByDescending(i => i.TotalPrice).ToList()
                    : items.OrderBy(i => i.TotalPrice).ToList(),
                _ => filter.SortDescending 
                    ? items.OrderByDescending(i => i.OutwardDate).ToList()
                    : items.OrderBy(i => i.OutwardDate).ToList()
            };
        }
    }
}