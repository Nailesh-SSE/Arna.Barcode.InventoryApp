namespace InventoryManagement.Services.DTO;

public class ReturnItemDto
{
    public int InwardId { get; set; }
    public int ProductId { get; set; }
    public int ItemQuantity { get; set; }
    public int BoxQuantity { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; }    
    public int UpdatedBy { get; set; }
    public int CreatedBy { get; set; }
   
}
