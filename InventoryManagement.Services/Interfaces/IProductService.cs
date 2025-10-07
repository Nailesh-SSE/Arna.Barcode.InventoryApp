using InventoryManagement.Core.Entities;

namespace InventoryManagement.Services.Interfaces;

public interface IProductService
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
    Task<bool> CreateProductAsync(Product product);
    Task<bool> UpdateProductAsync(Product product);
    Task<bool> DeleteProductAsync(int id);
    Task<bool> IsSkuUniqueAsync(string sku, int? excludeId = null);
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
  
}