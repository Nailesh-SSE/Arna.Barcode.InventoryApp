using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models
{
    public class RoleModel:CommonModel
    {
        public int Id { get; set; }

        [StringLength(20)]
        public string Name { get; set; }
        public int RoleLevel { get; set; }
        public string? Description { get; set; }
    }
}
