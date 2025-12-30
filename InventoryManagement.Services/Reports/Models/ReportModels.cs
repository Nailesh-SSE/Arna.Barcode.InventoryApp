using InventoryManagement.Core.Entities.SP_Entities;

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
    public int? BrandId { get; set; }
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
    public int? BrandId { get; set; }
    public string? BrandName { get; set; }
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

#region Outstock Report Models
public class OutwardReportResult : BaseReportResult
{
    public List<OutwardReportItem> Items { get; set; } = new();
    public OutwardReportSummary Summary { get; set; } = new();
    public int PageCount { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class OutwardFilter : BaseReportFilter
{
    public string? OutwardNumbers { get; set; }
    public int? BillToCompanyId { get; set; }
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public string? ProductSKUs { get; set; }
    public string? BatchNumbers { get; set; }
    public bool IncludeInactive { get; set; }
    public bool GroupByProduct { get; set; }
    public bool GroupBySupplier { get; set; }
    public bool SummaryOnly { get; set; }
}
public class OutwardReportItem
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
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public int BillToCompanyId { get; set; }
    public string BillToCompanyName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public bool IsActive { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
}
public class OutwardReportSummary
{
    public int TotalOutwards { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public int UniqueProductsCount { get; set; }
    public int UniqueSuppliersCount { get; set; }
    public decimal AverageQuantityPerOutward { get; set; }
    public decimal AverageValuePerOutward { get; set; }
    public Dictionary<string, decimal> QuantityByCategory { get; set; } = new();
    public Dictionary<string, decimal> ValueBySupplier { get; set; } = new();
    public Dictionary<string, int> CountByProduct { get; set; } = new();
}


#endregion

#region Return Sale Report Models
public class SaleReturnReportResult : BaseReportResult
{
    public List<SaleReturnReportItem> Items { get; set; } = new();
    public SaleReturnReportSummary Summary { get; set; } = new();
    public int PageCount { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
public class SaleReturnFilter : BaseReportFilter
{
    public string? ReturnNumbers { get; set; }
    public int? BillToCompanyId { get; set; }
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public string? ProductSKUs { get; set; }
    public bool IncludeInactive { get; set; }
    public bool GroupByProduct { get; set; }
    public bool GroupBySupplier { get; set; }
    public bool SummaryOnly { get; set; }
}
public class SaleReturnReportItem
{
    public int SaleReturnId { get; set; }
    public string ReturnNo { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? BatchNumber { get; set; }
    public int BillToCompanyId { get; set; }
    public string BillToCompanyName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public bool IsActive { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
}
public class SaleReturnReportSummary
{
    public int TotalReturns { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public int UniqueProductsCount { get; set; }
    public int UniqueSuppliersCount { get; set; }
    public decimal AverageQuantityPerReturn { get; set; }
    public decimal AverageValuePerReturn { get; set; }
    public Dictionary<string, decimal> QuantityByCategory { get; set; } = new();
    public Dictionary<string, decimal> ValueBySupplier { get; set; } = new();
    public Dictionary<string, int> CountByProduct { get; set; } = new();
}


#endregion
#region  Inveotry Report Models
public class InventoryReportResult : BaseReportResult
{
    public List<InventoryReportDTO> Items { get; set; } = new();
    public SaleReturnReportSummary Summary { get; set; } = new();
    public int PageCount { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
public class InventoryReportFilter : BaseReportFilter 
{

    public int? BrandId { get; set; }   
    public int? CategoryId { get; set; }
    public int? ColourId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public int? MInGoodMinGoodStock { get; set; }
    public int? MaxGoodStock { get; set; }

}
//public class InventoryReportModel 
//{
//    public int ProductId { get; set; }
//    public int? CategoryId { get; set; }
//    public int? BrandId { get; set; }
//    public string SKU { get; set; }
//    public string? BrandName { get; set; }
//    public string? CategoryName { get; set; }
//    public string? Color { get; set; } 
//    public decimal Inward { get; set; }
//    public decimal Outward { get; set; }
//    public decimal SaleReturn { get; set; }
//    public decimal TotalSale { get; set; }
//    public decimal GoodStock { get; set; }
//    public bool IsActive { get; set; } = true;
//    public bool IsDelete { get; set; } = false;

//}
#endregion