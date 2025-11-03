namespace InventoryManagement.Services.Models;

public class MonthlySalesModel
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int TotalOutwardCount { get; set; }
    public int TotalItemsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int UniqueCustomers { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class ProductSalesModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string MakeCompany { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AveragePrice { get; set; }
    public int TimesSold { get; set; }
    public DateTime? LastSoldDate { get; set; }
}

public class CustomerSalesModel
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerType { get; set; } = string.Empty; // BillTo, ShipTo, Make
    public int TotalOrders { get; set; }
    public int TotalItemsPurchased { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime? FirstOrderDate { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public List<string> TopProducts { get; set; } = new();
}

public class SalesSummaryModel
{
    public int TotalOrders { get; set; }
    public int TotalItemsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int UniqueCustomers { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int UniqueProductsSold { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public decimal GrowthPercentage { get; set; }
}

public class DailySalesModel
{
    public DateTime Date { get; set; }
    public int OrderCount { get; set; }
    public int ItemsSold { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}