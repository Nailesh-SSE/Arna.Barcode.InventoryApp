using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models;

public class DashboardSummaryModel
{
    public int TotalProducts { get; set; }
    public int TotalInwardEntries { get; set; }
    public int TotalOutwardEntries { get; set; }
    public int ActiveCompanies { get; set; }
    public int LowStockProducts { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<MonthlyTransactionModel> MonthlyTransactions { get; set; } = new();
    public List<ProductMovementModel> TopMovingProducts { get; set; } = new();
}

public class InwardSummaryModel
{
    public int Id { get; set; }
    public string InwardNo { get; set; } = string.Empty;
    public DateTime InwardDate { get; set; }
    public string ShipMentCompany { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class OutwardSummaryModel
{
    public int Id { get; set; }
    public string OutwardNo { get; set; } = string.Empty;
    public DateTime OutwardDate { get; set; }
    public string BillToCompany { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ProductStockModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
    public int SoldQuantity { get; set; }
}

public class InventoryStatsModel
{
    public int TotalCategories { get; set; }
    public int TotalCompanies { get; set; }
    public int TotalColors { get; set; }
    public int UniqueProducts { get; set; }
    public int TodayInward { get; set; }
    public int TodayOutward { get; set; }
    public int ThisMonthInward { get; set; }
    public int ThisMonthOutward { get; set; }
}

public class MonthlyTransactionModel
{
    public string Month { get; set; } = string.Empty;
    public int InwardCount { get; set; }
    public int OutwardCount { get; set; }
    public decimal InwardValue { get; set; }
    public decimal OutwardValue { get; set; }
}

public class ProductMovementModel
{
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int TotalSold { get; set; }
    public decimal TotalRevenue { get; set; }
}