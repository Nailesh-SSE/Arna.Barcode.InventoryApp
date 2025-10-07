using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;

namespace InventoryManagement.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        return await productRepository.FindAsync(p => !p.IsDeleted);
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        return await productRepository.GetByIdAsync(id);
    }

    public async Task<bool> CreateProductAsync(Product product)
    {
        try
        {
            if (!await IsSkuUniqueAsync(product.SKU))
                return false;

            var categoryRepository = _unitOfWork.GetRepository<Category>();
            var category = await categoryRepository.GetByIdAsync(product.CategoryId);
            if (category != null)
            {
                product.CategoryName = category.Name;
            }

            var productRepository = _unitOfWork.GetRepository<Product>();
            await productRepository.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        try
        {
            if (!await IsSkuUniqueAsync(product.SKU, product.Id))
                return false;

            var categoryRepository = _unitOfWork.GetRepository<Category>();
            var category = await categoryRepository.GetByIdAsync(product.CategoryId);
            if (category != null)
            {
                product.CategoryName = category.Name;
            }

            var productRepository = _unitOfWork.GetRepository<Product>();
            productRepository.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        try
        {
            var productRepository = _unitOfWork.GetRepository<Product>();
            var product = await productRepository.GetByIdAsync(id);
            if (product == null) return false;

            // Check if product has any inward items
            var inwardItemRepository = _unitOfWork.GetRepository<InwardItem>();
            var hasInwardItems = await inwardItemRepository.FindAsync(ii => ii.ProductId == id && !ii.IsDeleted);
            if (hasInwardItems.Any())
            {
                return false; // Cannot delete product with inward items
            }

            product.IsDeleted = true;
            product.IsActive = false;
            productRepository.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsSkuUniqueAsync(string sku, int? excludeId = null)
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        var products = await productRepository.FindAsync(p => p.SKU == sku && !p.IsDeleted);
        
        if (excludeId.HasValue)
            products = products.Where(p => p.Id != excludeId.Value);

        return !products.Any();
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        return await productRepository.FindAsync(p => p.CategoryId == categoryId && !p.IsDeleted);
    }
}