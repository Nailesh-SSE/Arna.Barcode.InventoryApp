using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Core.Entities;

public partial class Users : Common
{
    [Key]
    public int Id { get; set; }
    public int RoleId { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required")]
    [StringLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [StringLength(100)]
    [EmailAddress]
    public string EmailId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255)]
    public string Password { get; set; } = string.Empty;

    [StringLength(20)]
    [Phone]
    public string ContactNo { get; set; } = string.Empty;

    public bool IsLoginAllowed { get; set; } = true;


    [NotMapped]
    public string Name => $"{FirstName} {LastName}";

    [NotMapped]
    public string RoleName { get; set; } = string.Empty;
}