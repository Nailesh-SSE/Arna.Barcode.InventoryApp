using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagement.Core.Entities
{
    public class Colour:Common
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Colour name is required")]
        [StringLength(50)]
        public string Name { get; set; }
        [Required(ErrorMessage = "Colour code is required")]
        [StringLength(15)]
        public string Code { get; set; }    
        
    }
}
