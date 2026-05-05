namespace InventoryManagement.Core.Entities.SP_Entities;

public class BarcodeTrackDTO
{
    public string? Item_Barcode { get; set; }
    public string? Box_Barcode { get; set; }
    public DateTime TransactionTime { get; set; }
    public string TransactionNo { get; set; }
    public string TransactionType { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; }=string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? UpdatedOn { get; set; }

}