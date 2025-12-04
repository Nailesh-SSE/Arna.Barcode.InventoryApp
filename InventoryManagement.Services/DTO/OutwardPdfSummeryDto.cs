namespace InventoryManagement.Services.DTO;

public class OutwardPdfSummeryDto
{
    public int Id { get; set; }
    public int BillToCompanyId { get; set; }
    public string CompanyName { get; set; }
    public int TotalQuantity { get; set; }
}
