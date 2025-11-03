using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models.ReportModels;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services.Services;

public class InstockReportGenerator : IInstockReportGenerator
{
    private readonly IUnitOfWork _unitOfWork;
    
    public InstockReportGenerator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<InstockReportResult> GenerateReportAsync(InstockFilter filter)
    {
        var startTime = DateTime.UtcNow;

        var result = new InstockReportResult
        {
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

        try
        {
            var inwardFilter = new Filters.InwardReportFilter(filter);
            if (!inwardFilter.IsValid())
            {
                result.Warnings.Add("Invalid filter criteria provided");
                return result;
            }

            var inwardRepository = _unitOfWork.GetRepository<Inward>();

            // ✅ Build query with all related data in ONE query
            var inwardQuery = inwardRepository.GetQueryable()
                .Include(i => i.InwardItems)
                    .ThenInclude(ii => ii.Product)
                        .ThenInclude(p => p.Category)
                .Include(i => i.InwardItems)
                    .ThenInclude(ii => ii.InwardBarcodeItems)
                .Include(i => i.Category)
                .Include(i => i.ShipMentCompany)
                .AsNoTracking(); // ✅ Better performance for read-only

            // ✅ Apply filters progressively
            if (filter.StartDate.HasValue)
                inwardQuery = inwardQuery.Where(i => i.InwardDate.Date >= filter.StartDate.Value.Date);

            if (filter.EndDate.HasValue)
                inwardQuery = inwardQuery.Where(i => i.InwardDate.Date <= filter.EndDate.Value.Date);

            if (filter.CategoryId.HasValue)
                inwardQuery = inwardQuery.Where(i => i.CategoryId == filter.CategoryId.Value);

            if (filter.CompanyId.HasValue)
                inwardQuery = inwardQuery.Where(i => i.ShipMentCompanyId == filter.CompanyId.Value);

            // ✅ Execute query - gets everything in one database call
            var inwards = await inwardQuery.ToListAsync();

            if (!inwards.Any())
            {
                result.Warnings.Add("No inward records found");
                return result;
            }

            // ✅ Now work with in-memory data - no more database calls needed
            var inwardItems = inwards
                .SelectMany(i => i.InwardItems)
                .ToList();

            if (!inwardItems.Any())
            {
                result.Warnings.Add("No inward items found");
                return result;
            }

            var products = inwardItems
                .Select(ii => ii.Product)
                .Where(p => p != null)
                .Distinct()
                .ToList();

            var categories = products
                .Select(p => p.Category)
                .Where(c => c != null)
                .Distinct()
                .ToList();

            var barcodeItems = inwardItems
                .SelectMany(ii => ii.InwardBarcodeItems)
                .ToList();

            // ✅ Apply additional filters if needed
            var filteredItems = FilterInwardItems(inwardItems, products, categories, filter);

            // ✅ Calculate stock balances

            var mockQuantities = new[] { 150m, 25m, 8m, 300m, 49m };

            // 2. Get the unique product IDs from the items that have already been filtered.
            var productIdsForMocking = filteredItems.Select(item => item.ProductId).Distinct().ToList();

            // 3. Create the dictionary by assigning a quantity from our static list to each product.
            //    The modulo operator (%) ensures we just cycle through the list of mock quantities.
            var stockBalances = productIdsForMocking.Select((productId, index) => new
            {
                Id = productId,
                Quantity = mockQuantities[index % mockQuantities.Length] // Cycle through the static values
            })
                .ToDictionary(
                    item => item.Id,
                    item => item.Quantity
                );
           // var stockBalances = await CalculateStockBalances(filteredItems, filter);

            // ✅ Map to report items
            result.Items = MapToReportItems(stockBalances, products, categories, filter);

            // ✅ Calculate aggregates
            result.TotalQuantity = result.Items.Sum(i => i.CurrentQuantity);
            result.TotalValue = result.Items.Sum(i => i.TotalValue);
            result.LowStockItemsCount = result.Items.Count(i => i.IsLowStock);
            result.ExpiredItemsCount = result.Items.Count(i => i.IsExpired);
            result.UniqueProductsCount = result.Items.Select(i => i.ProductId).Distinct().Count();
            result.TotalRecords = result.Items.Count;

            // ✅ Apply pagination on in-memory data
            if (filter.PageSize > 0)
            {
                result.Items = result.Items
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToList();
            }

            // ✅ Apply sorting
            result.Items = ApplySorting(result.Items, filter);
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Error generating report: {ex.Message}");
            // ✅ Log full exception for debugging
            Console.WriteLine($"Full error: {ex}");
            throw; // Re-throw to see stack trace
        }
        finally
        {
            result.GenerationTime = DateTime.UtcNow - startTime;
        }

        return result;
    }

    public async Task<bool> ValidateFilterAsync(InstockFilter filter)
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
    
    private List<InwardItem> FilterInwardItems(
        List<InwardItem> inwardItems,
        List<Product> products,
        List<Category> categories,
        InstockFilter filter)
    {
        var filtered = inwardItems.AsQueryable();
        
        // Filter by products
        if (!string.IsNullOrEmpty(filter.ProductSKUs))
        {
            var skus = filter.ProductSKUs.Split(',').Select(s => s.Trim()).ToList();
            var productIds = products.Where(p => skus.Contains(p.SKU)).Select(p => p.Id).ToList();
            filtered = filtered.Where(ii => productIds.Contains(ii.ProductId));
        }
        
        // Filter by batch numbers
        if (!string.IsNullOrEmpty(filter.BatchNumbers))
        {
            var batches = filter.BatchNumbers.Split(',').Select(b => b.Trim()).ToList();
            filtered = filtered.Where(ii => batches.Contains(ii.BatchNo));
        }
        
        // Filter by category
        if (filter.CategoryId.HasValue)
        {
            var categoryProductIds = products.Where(p => p.CategoryId == filter.CategoryId.Value).Select(p => p.Id).ToList();
            filtered = filtered.Where(ii => categoryProductIds.Contains(ii.ProductId));
        }
        
        return filtered.ToList();
    }

    // THIS IS THE RECOMMENDED WORKAROUND
    private async Task<Dictionary<int, decimal>> CalculateStockBalances(List<InwardItem> inwardItems, InstockFilter filter)
    {
        try
        {
            if (!inwardItems.Any())
                return new Dictionary<int, decimal>();

            var productIds = inwardItems.Select(ii => ii.ProductId).Distinct().ToList();

            // ========== STEP 1: FETCH RAW DATA WITH A SIMPLE QUERY ==========
            // This query is simple enough that EF Core will NOT fail to translate it.
            // It gets ALL consumption for the products, which we will filter later.
            var outwardDetailRepository = _unitOfWork.GetRepository<OutwardDetail>();
            var allConsumptionForProducts = await outwardDetailRepository.GetQueryable()
                .Where(od => productIds.Contains(od.ProductId))
                .Select(od => new { od.ProductId, od.OutwardId, od.Quantity }) // Select only what we need
                .AsNoTracking()
                .ToListAsync();


            // ========== STEP 2: APPLY THE DATE FILTER IN-MEMORY ==========
            // This part runs in your application, not on the SQL server.

            // Start by assuming we will use all the data we fetched.
            var filteredConsumption = allConsumptionForProducts;

            if (filter.StartDate.HasValue || filter.EndDate.HasValue)
            {
                // Run a second, simple query to get the list of valid outward IDs.
                var outwardRepository = _unitOfWork.GetRepository<Outward>();
                var outwardQuery = outwardRepository.GetQueryable();

                if (filter.StartDate.HasValue)
                    outwardQuery = outwardQuery.Where(o => o.OutwardDate >= filter.StartDate.Value);
                if (filter.EndDate.HasValue)
                    outwardQuery = outwardQuery.Where(o => o.OutwardDate <= filter.EndDate.Value);

                var validOutwardIds = await outwardQuery.Select(o => o.Id).ToListAsync();

                // A HashSet is much faster for lookups than a List.
                var validOutwardIdSet = new HashSet<int>(validOutwardIds);

                // Now, filter the list we already have using LINQ-to-Objects (in-memory).
                filteredConsumption = allConsumptionForProducts
                    .Where(od => validOutwardIdSet.Contains(od.OutwardId))
                    .ToList();
            }

            // ========== STEP 3: PERFORM FINAL AGGREGATION IN-MEMORY ==========
            // This GroupBy and ToDictionary also runs in your application.

            // FIX: Cast the result of Sum() to decimal to create a Dictionary<int, decimal>
            var consumptionByProduct = filteredConsumption
                .GroupBy(od => od.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => (decimal)g.Sum(od => od.Quantity)
                );

            // FIX: Also cast here to ensure the types match.
            var stockByProduct = inwardItems
                .GroupBy(ii => ii.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => (decimal)g.Sum(ii => ii.Quantity)
                );

            // ========== STEP 4: CALCULATE FINAL BALANCES ==========
            var balances = new Dictionary<int, decimal>();
            foreach (var productId in stockByProduct.Keys)
            {
                var inward = stockByProduct[productId];
                // This now works because consumptionByProduct is Dictionary<int, decimal>
                var outward = consumptionByProduct.GetValueOrDefault(productId, 0m);
                balances[productId] = inward - outward;
            }

            return balances;
        }
        catch (Exception ex)
        {
            // For debugging, put a breakpoint here and inspect ex.ToString()
            // It will contain more details about the error.
            throw;
        }
    }

    private List<InstockReportItem> MapToReportItems(
        Dictionary<int, decimal> stockBalances,
        List<Product> products,
        List<Category> categories,
        InstockFilter filter)
    {
        var items = new List<InstockReportItem>();
        
        foreach (var balance in stockBalances)
        {
            var product = products.FirstOrDefault(p => p.Id == balance.Key);
            if (product == null) continue;
            
            var category = categories.FirstOrDefault(c => c.Id == product.CategoryId);
            
            // Check low stock
            var isLowStock = filter.IncludeLowStock && balance.Value <= filter.LowStockThreshold;
            
            // Check expired (mock implementation)
            var isExpired = filter.IncludeExpired && false; // Would need expiry date logic
            
            // Only include if meets filter criteria
            if (filter.MinQuantity.HasValue && balance.Value < filter.MinQuantity.Value) continue;
            if (filter.MaxQuantity.HasValue && balance.Value > filter.MaxQuantity.Value) continue;
            if (filter.IncludeLowStock && !isLowStock) continue;
            if (filter.IncludeExpired && !isExpired) continue;
            
            items.Add(new InstockReportItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                CategoryName = category?.Name ?? "Unknown",
                BatchNumber = filter.BatchNumbers ?? "", // Mock data
                CurrentQuantity = balance.Value,
                Unit = "PCS",
                LastInwardDate = DateTime.UtcNow.AddDays(-30), // Mock data
                IsLowStock = isLowStock,
                IsExpired = isExpired,
                UnitCost = 100m, // Mock data
                TotalValue = balance.Value * 100m // Mock calculation
            });
        }
        
        return items;
    }
    
    private List<InstockReportItem> ApplySorting(List<InstockReportItem> items, InstockFilter filter)
    {
        if (string.IsNullOrEmpty(filter.SortBy))
            return items.OrderByDescending(i => i.CurrentQuantity).ToList();
        
        return filter.SortBy.ToLower() switch
        {
            "productname" => filter.SortDescending 
                ? items.OrderByDescending(i => i.ProductName).ToList()
                : items.OrderBy(i => i.ProductName).ToList(),
            "sku" => filter.SortDescending 
                ? items.OrderByDescending(i => i.SKU).ToList()
                : items.OrderBy(i => i.SKU).ToList(),
            "category" => filter.SortDescending 
                ? items.OrderByDescending(i => i.CategoryName).ToList()
                : items.OrderBy(i => i.CategoryName).ToList(),
            "quantity" => filter.SortDescending 
                ? items.OrderByDescending(i => i.CurrentQuantity).ToList()
                : items.OrderBy(i => i.CurrentQuantity).ToList(),
            "value" => filter.SortDescending 
                ? items.OrderByDescending(i => i.TotalValue).ToList()
                : items.OrderBy(i => i.TotalValue).ToList(),
            _ => filter.SortDescending 
                ? items.OrderByDescending(i => i.CurrentQuantity).ToList()
                : items.OrderBy(i => i.CurrentQuantity).ToList()
        };
    }
}