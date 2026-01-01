namespace InventoryManagement.Core.Entities.SP_Entities;

public class BarcodeTrackDTO
{
    public string BarCodeNo { get; set; }
    public DateTime TransactionTime { get; set; }
    public string TransactionNo { get; set; }
    public string TransactionType { get; set; }
}
