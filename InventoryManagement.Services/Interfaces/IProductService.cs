using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Models;
namespace InventoryManagement.Services.Interfaces;

public interface IProductService
{
    Task<List<ProductModel>> GetAllProductsAsync();
    Task<ProductModel?> GetProductByIdAsync(int id);
    Task<bool> CreateProductAsync(ProductModel productModel);
    Task<bool> UpdateProductAsync(ProductModel productModel);
    Task<bool> DeleteProductAsync(int id);
    Task<bool> IsSkuUniqueAsync(string sku, int? excludeId = null);
    Task<bool> IsProductNameUniqueAsync(string name, int? excludeId = null);
    Task<ProductModel> GetLastProductAsync();
    Task<List<ProductModel>> GetProductsByCategoryAsync(int categoryId);
  
}