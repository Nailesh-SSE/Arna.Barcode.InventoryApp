using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models;

public class UserModel
{
    public int Id { get; set; }

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string ContactNo { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string RoleName { get; set; }
    public int RoleId { get; set; }
    public int CreatedBy { get; set; }
    public int UpdatedBy { get; set; }
}
