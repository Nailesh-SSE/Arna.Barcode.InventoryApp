using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models;

public class FormMasterModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Form name is required")]
    [StringLength(100)]
    public string FormName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Route is required")]
    [StringLength(200)]
    public string Route { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int CreatedBy { get; set; }
    public int UpdatedBy { get; set; }
}
