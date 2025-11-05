using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class Colour:Common
{
    [Key]
    public int Id { get; set; }
    [Required]
    [StringLength(50)]
    public string Name { get; set; }
    [Required]
    [StringLength(15)]
    public string Code { get; set; }    
    
}
