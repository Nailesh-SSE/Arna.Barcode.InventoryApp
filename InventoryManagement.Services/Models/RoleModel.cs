using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models
{
    public class RoleModel:CommonModel
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string Name { get; set; }
    }
}
