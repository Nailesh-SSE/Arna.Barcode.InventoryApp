using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public abstract class Common
{
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.Now;
    public int UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
}