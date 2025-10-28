using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models
{
    public class OutwardModel:CommonModel
    {
        public int Id { get; set; }
        [StringLength(50)]
        public string OutwardNo { get; set; } = string.Empty;
        public DateTime OutwardDate { get; set; } = DateTime.UtcNow.Date;
        public int BillToCompanyId { get; set; }
        public string? InvoiceNo { get; set; }

        public DateTime? InvoiceDate { get; set; }

        [StringLength(50)]
        public string? ChallanNo { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public List<OutWardItem> OutwardDetails { get; set; } = new();
    }
    public class OutWardItem
    {
        public int Id { get; set; }
        public int OutwardId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public string? BarcodeNo { get; set; }
    }
}
