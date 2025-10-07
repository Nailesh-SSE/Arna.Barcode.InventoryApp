namespace InventoryManagement.Services.Models;

public abstract class CommonModel
{
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public int UpdatedBy { get; set; }
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}