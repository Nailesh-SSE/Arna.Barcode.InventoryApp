using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Core.Entities;

public class Company : Common
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Company name is required")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company code is required")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company type is required")]
    public CompanyType CompanyType { get; set; }

    [NotMapped]
    public string TypeName => CompanyType.ToString();
}