using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.DTO
{
    public class SaleToInwardDto :CommonModel
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; } = 0;
        public int ShipToCompanyId { get; set; }
        public  DateTime ReturnDate { get; set; }
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public decimal ReturnQuantity { get; set; }
        public int UserId { get; set; }  

    }
}
