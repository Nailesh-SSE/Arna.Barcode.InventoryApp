using InventoryManagement.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagement.Services.Models
{
    public class CompanyModel :CommonModel
    {
        [Key]
        public int Id { get; set; }

         [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        //[Required(ErrorMessage = "Company code is required")]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company type is required")]
        public CompanyType CompanyType { get; set; }

        [NotMapped]
        public string TypeName => CompanyType.ToString();
        public int SerialNumber { get; set; }
        [StringLength(1000)]
        public string? Remark { get; set; }
    }
}
