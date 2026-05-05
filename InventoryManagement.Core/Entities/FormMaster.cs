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

    [Required]
    public int DisplayIndex { get; set; }

    public string? Icon { get; set; } = string.Empty;

    public int? ParentId { get; set; }

    public string? ParentName { get; set; }

    }
