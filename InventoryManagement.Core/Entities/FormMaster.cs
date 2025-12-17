using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class FormMaster : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string FormName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Route { get; set; } = string.Empty;
}
