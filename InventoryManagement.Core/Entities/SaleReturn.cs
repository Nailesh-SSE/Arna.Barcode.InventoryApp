using InventoryManagement.Core.Entities;
using System.ComponentModel.DataAnnotations;

public class SaleReturn : Common
{
    [Key]
    public int Id { get; set; }
    public string SaleReturnNo { get; set; }
    public int BillToCompanyId { get; set; } 
    public DateTime SaleReturnDate { get; set; } = DateTime.UtcNow;

    public virtual Company BillToCompany { get; set; }
    public virtual ICollection<SaleReturnItems> SaleReturnItems { get; set; } =  new List<SaleReturnItems>();
}

