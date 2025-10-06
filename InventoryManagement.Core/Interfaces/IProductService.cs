using InventoryManagement.Core.Entities;

namespace InventoryManagement.Core.Interfaces;

public interface IProductService
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
    Task<bool> CreateProductAsync(Product product);
    Task<bool> UpdateProductAsync(Product product);
    Task<bool> DeleteProductAsync(int id);
    Task<bool> IsSkuUniqueAsync(string sku, int? excludeId = null);
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
    
    // Simplified method names for UI
    Task<IEnumerable<Product>> GetAllAsync() => GetAllProductsAsync();
    Task AddAsync(Product product) => CreateProductAsync(product).ContinueWith(t => { });
    Task UpdateAsync(Product product) => UpdateProductAsync(product).ContinueWith(t => { });
    Task DeleteAsync(int id) => DeleteProductAsync(id).ContinueWith(t => { });
}