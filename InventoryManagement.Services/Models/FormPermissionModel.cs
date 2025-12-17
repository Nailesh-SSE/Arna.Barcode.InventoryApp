using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagement.Services.Models;

public class FormPermissionModel
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int FormId { get; set; }

    public string RoleName { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public int CreatedBy { get; set; }
}
