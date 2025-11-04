namespace InventoryManagement.Services.Models.ReportModels;

#region Base Models

public abstract class BaseReportFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? CategoryId { get; set; }
    public int? CompanyId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

public abstract class BaseReportResult
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TotalRecords { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public TimeSpan GenerationTime { get; set; }
    public List<string> Warnings { get; set; } = new();
}

#endregion


#region Inward Report Models

public class InwardFilter : BaseReportFilter
{
    public string? InwardNumbers { get; set; }
    public int? ShipmentCompanyId { get; set; }
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public string? ProductSKUs { get; set; }
    public string? BatchNumbers { get; set; }
    public bool IncludeInactive { get; set; }
    public bool GroupByProduct { get; set; }
    public bool GroupBySupplier { get; set; }
    public bool SummaryOnly { get; set; }
}

public class InwardReportItem
{
    public int InwardId { get; set; }
    public string InwardNumber { get; set; } = string.Empty;
    public DateTime InwardDate { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public int ShipmentCompanyId { get; set; }
    public string ShipmentCompanyName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public bool IsActive { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class InwardReportSummary
{
    public int TotalInwards { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public int UniqueProductsCount { get; set; }
    public int UniqueSuppliersCount { get; set; }
    public decimal AverageQuantityPerInward { get; set; }
    public decimal AverageValuePerInward { get; set; }
    public Dictionary<string, decimal> QuantityByCategory { get; set; } = new();
    public Dictionary<string, decimal> ValueBySupplier { get; set; } = new();
    public Dictionary<string, int> CountByProduct { get; set; } = new();
}

public class InwardReportResult : BaseReportResult
{
    public List<InwardReportItem> Items { get; set; } = new();
    public InwardReportSummary Summary { get; set; } = new();
    public int PageCount { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
#endregion
#region Instock Report Models

public class InstockFilter : BaseReportFilter
{
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public string? ProductSKUs { get; set; }
    public string? BatchNumbers { get; set; }
    public bool IncludeLowStock { get; set; }
    public decimal LowStockThreshold { get; set; } = 10m;
    public bool IncludeExpired { get; set; }
    public string? Location { get; set; }
}

public class InstockReportItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime LastInwardDate { get; set; }
    public string? SerialNumber { get; set; }
    public string? Location { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsExpired { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
}

public class InstockReportResult : BaseReportResult
{
    public List<InstockReportItem> Items { get; set; } = new();
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockItemsCount { get; set; }
    public int ExpiredItemsCount { get; set; }
    public int UniqueProductsCount { get; set; }
}

#endregion

#region Outstock Report Models

public class OutstockFilter : BaseReportFilter
{
    /// <summary>
    /// Filter by customer company IDs (comma-separated)
    /// </summary>
    public string? CustomerCompanyIds { get; set; }
    
    /// <summary>
    /// Minimum quantity sold to include
    /// </summary>
    public decimal? MinQuantity { get; set; }
    
    /// <summary>
    /// Maximum quantity sold to include
    /// </summary>
    public decimal? MaxQuantity { get; set; }
    
    /// <summary>
    /// Filter by outward numbers (comma-separated)
    /// </summary>
    public string? OutwardNumbers { get; set; }
    
    /// <summary>
    /// Group results by product
    /// </summary>
    public bool GroupByProduct { get; set; }
    
    /// <summary>
    /// Group results by customer
    /// </summary>
    public bool GroupByCustomer { get; set; }
    
    /// <summary>
    /// Include only returns
    /// </summary>
    public bool IncludeReturns { get; set; }
    
    /// <summary>
    /// Filter by sales channel
    /// </summary>
    public string? SalesChannel { get; set; }
}

public class OutstockReportItem
{
    public int OutwardId { get; set; }
    public string OutwardNumber { get; set; } = string.Empty;
    public DateTime OutwardDate { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int CustomerCompanyId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string? BarcodeNumber { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? SalesChannel { get; set; }
    public string? Remarks { get; set; }
}

public class OutstockReportResult : BaseReportResult
{
    public List<OutstockReportItem> Items { get; set; } = new();
    public decimal TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
    public int UniqueCustomersCount { get; set; }
    public int UniqueProductsCount { get; set; }
    public Dictionary<string, decimal> SalesByCategory { get; set; } = new();
    public Dictionary<string, int> QuantityByProduct { get; set; } = new();
}

#endregion

#region Return Sale Report Models

public class ReturnSaleFilter : BaseReportFilter
{
    public string? ReturnType { get; set; }
    public string? ReasonCodes { get; set; }
    public string? BarcodeNumbers { get; set; }
    public string? ReturnNumbers { get; set; }
    public bool HighValueOnly { get; set; }
    public decimal HighValueThreshold { get; set; } = 1000m;
    public bool GroupByReason { get; set; }
    public bool PendingOnly { get; set; }
}

public class ReturnSaleReportItem
{
    public int ReturnId { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string ReturnType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string BarcodeNumber { get; set; } = string.Empty;
    public int CustomerCompanyId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string? OriginalInvoiceNumber { get; set; }
    public DateTime? OriginalSaleDate { get; set; }
    public string? Status { get; set; }
    public string? Remarks { get; set; }
}

public class ReturnSaleReportResult : BaseReportResult
{
    public List<ReturnSaleReportItem> Items { get; set; } = new();
    public decimal TotalReturnedQuantity { get; set; }
    public decimal TotalReturnedAmount { get; set; }
    public int UniqueCustomersCount { get; set; }
    public int UniqueProductsCount { get; set; }
    public Dictionary<string, decimal> ReturnsByReason { get; set; } = new();
    public Dictionary<string, int> QuantityByReturnType { get; set; } = new();
    public decimal AverageReturnAmount { get; set; }
    public double ReturnRate { get; set; }
}

#endregion
