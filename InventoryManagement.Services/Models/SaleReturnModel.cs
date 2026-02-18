using InventoryManagement.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models
{
    public class SaleReturnModel : CommonModel
    {
        public int Id { get; set; }

        public string SaleReturnNo { get; set; }
        public int? BillToCompanyId { get; set; }

        [Required(ErrorMessage = "Sale Return Date is required.")]
        public DateTime SaleReturnDate { get; set; } = DateTime.Now;
        public DateOnly SaleReturnDateOnly => DateOnly.FromDateTime(SaleReturnDate);
        public string? BillToCompanyName { get; set; } = string.Empty;

        public List<SaleReturnItemsModel> SaleReturnItemsList { get; set; } = new();
    }


    public class SaleReturnItemsModel : CommonModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "SaleReturnId is required.")]
        public int SaleReturnId { get; set; }

        public int? OutwardId { get; set; } = null;

        [Required(ErrorMessage = "Product is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Select a valid Product.")]
        public int ProductId { get; set; }

        public int SerialNo { get; set; }
        public string? BarCodeNo { get; set; } = null;

        [Required(ErrorMessage = "Unit is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Select a valid Unit.")]
        public int UnitId { get; set; }

        [Required(ErrorMessage = "Return Quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Return Quantity must be at least 1.")]
        public decimal ReturnQuantity { get; set; } = 1;

        [Required(ErrorMessage = "Category is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Select a valid Category.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Ship To Company is required.")]
        //  [Range(1, int.MaxValue, ErrorMessage = "Select a valid Shipping Company.")]
        public int ShipToCompanyId { get; set; } = 0;


        [Required(ErrorMessage = "bill To Company is required.")]
        //  [Range(1, int.MaxValue, ErrorMessage = "Select a valid Shipping Company.")]
        public int BillToCompanyId { get; set; } = 0;

        [Required(ErrorMessage = "Return Type is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Select a valid Return Type.")]
        public int ReturnType { get; set; }

        public bool IsTakeInStock { get; set; } = false;

        [StringLength(200, ErrorMessage = "Reason cannot exceed 200 characters.")]
        public string? ReasonToReturn { get; set; } = null;
        public int? PlatformId { get; set; }
        public string? PlatformName { get; set; }
        public DateTime? ReturnDate { get; set; } = DateTime.Now;
        public string? ProductSearchText { get; set; } = string.Empty;
        public string? OutwardSearchText { get; set; } = string.Empty;
        public List<Platform> PlatformList { get; set; } = new();
        public List<ProductModel> ProductLIst { get; set; } = new();
        public List<CategoryModel> CategoryList { get; set; } = new();
        public List<CompanyModel> ShipToCompanyList { get; set; } = new();
        public List<CompanyModel> BillToCompanyList { get; set; } = new();
        public List<OutwardModel> OutwardList { get; set; } = new();
    }

    public class SaleReturnValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int? ProductId { get; set; }
        public int? ShipToId { get; set; }
        public int? BillToId { get; set; }
        public int? UnitId { get; set; }
        public int? OutwardId { get; set;}
        public string? BarcodeNo { get;set; }
        public int PlatformId { get; set; }
        public string? Platform { get; set; } 

    }
}