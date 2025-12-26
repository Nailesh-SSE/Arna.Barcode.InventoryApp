namespace InventoryManagement.Core.Entities.SP_Entities
{
    public class InventoryReportDTO
    {
        public string Sku { get; set; }
        public string? MakeCompany { get; set; }   // maps to BrandName in UI if needed
        public string? CategoryName { get; set; }
        public decimal Inward { get; set; }
        public decimal Outward { get; set; }
        public decimal Returns { get; set; }
        public decimal TotalSale { get; set; }
        public decimal GoodStock { get; set; }
    }
}
