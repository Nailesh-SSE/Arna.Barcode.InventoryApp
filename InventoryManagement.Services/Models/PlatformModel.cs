using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models;

public class PlatformModel : CommonModel
{
    public int Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Remark { get; set; } = string.Empty;
}
