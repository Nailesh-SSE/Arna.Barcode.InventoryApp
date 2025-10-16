using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagement.Services.Models
{
    public class ColourModel:CommonModel
    {

        public int Id { get; set; }
       
        [StringLength(50)]
        public string Name { get; set; }
     
        [StringLength(15)]
        public string Code { get; set; }

    }
}
