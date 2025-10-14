using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductModel>> GetAllProductsAsync()
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        var products = await productRepository.FindAsync(p => !p.IsDeleted);

        var getallproducts = products.Select(p => new ProductModel
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.SKU,
            Description = p.Description,
            CategoryId = p.CategoryId,
            CategoryName = p.CategoryName,
            CreatedBy = p.CreatedBy,
            CreatedOn = p.CreatedOn,
            UpdatedBy = p.UpdatedBy,
            UpdatedOn = p.UpdatedOn,
            IsActive = p.IsActive,
            IsDeleted = p.IsDeleted,
            Unit=p.Unit,
            UnitId = p.UnitId,
            MakeCompanyId = p.MakeCompanyId,
            MakeCompany = p.MakeCompany

        }).ToList();

        return getallproducts;
    }

    public async Task<ProductModel?> GetProductByIdAsync(int id)
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        var product = await productRepository.GetByIdAsync(id);

        var getproductbyid=new ProductModel
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Description = product.Description,
            CategoryId = product.CategoryId,
            CategoryName = product.CategoryName,
            CreatedBy = product.CreatedBy,
            CreatedOn = product.CreatedOn,
            UpdatedBy = product.UpdatedBy,
            UpdatedOn = product.UpdatedOn,
            IsActive = product.IsActive,
            IsDeleted = product.IsDeleted,
            Unit= product.Unit,
            UnitId = product.UnitId,
            MakeCompanyId = product.MakeCompanyId,
            MakeCompany = product.MakeCompany
        };
        return getproductbyid;
    }

    public async Task<bool> CreateProductAsync(ProductModel productModel)
    {
        try
        {
            if (!await IsSkuUniqueAsync(productModel.SKU))
                return false;

            if (!await IsProductNameUniqueAsync(productModel.Name, productModel.Id))
                return false;

            var categoryRepository = _unitOfWork.GetRepository<Category>();
            var category = await categoryRepository.GetByIdAsync(productModel.CategoryId);
            if (category != null)
            {
                productModel.CategoryName = category.Name;
            }
            else
            {

                return false; // Invalid CategoryId
            }

                var createdProduct = new Product
                {
                    Name = productModel.Name,
                    SKU = productModel.SKU,
                    Description = productModel.Description,
                    CategoryId = productModel.CategoryId,
                    CategoryName = productModel.CategoryName,
                    CreatedBy = productModel.CreatedBy,
                    CreatedOn = productModel.CreatedOn,
                    IsActive = productModel.IsActive,
                    IsDeleted = productModel.IsDeleted,
                    Unit = productModel.Unit,
                    UnitId = productModel.UnitId,
                    MakeCompany = productModel.MakeCompany,
                    MakeCompanyId = productModel.MakeCompanyId
                };

            var productRepository = _unitOfWork.GetRepository<Product>();
            await productRepository.AddAsync(createdProduct);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateProductAsync(ProductModel productModel)
    {
        try
        {
            if (!await IsSkuUniqueAsync(productModel.SKU, productModel.Id))
                return false;

            if (!await IsProductNameUniqueAsync(productModel.Name, productModel.Id))
                return false;

            var categoryRepository = _unitOfWork.GetRepository<Category>();
            var category = await categoryRepository.GetByIdAsync(productModel.CategoryId);
            if (category != null)
            {
                productModel.CategoryName = category.Name;
            }
            else
            {
                return false;
            }

            var ProductRepository = _unitOfWork.GetRepository<Product>();
            var existingProduct = await ProductRepository.GetByIdAsync(productModel.Id);


            existingProduct.Name = productModel.Name;
            existingProduct.SKU = productModel.SKU;
            existingProduct.Description = productModel.Description;
            existingProduct.CategoryId = productModel.CategoryId;
            existingProduct.CategoryName = productModel.CategoryName;
            existingProduct.UpdatedBy = productModel.UpdatedBy;
            existingProduct.UpdatedOn = productModel.UpdatedOn;
            existingProduct.IsActive = productModel.IsActive;
            existingProduct.IsDeleted = productModel.IsDeleted;
            existingProduct.Unit = productModel.Unit;
            existingProduct.UnitId = productModel.UnitId;
            existingProduct.MakeCompany = productModel.MakeCompany;
            existingProduct.MakeCompanyId = productModel.MakeCompanyId; 

            var productRepository = _unitOfWork.GetRepository<Product>();
            productRepository.UpdateAsync(existingProduct);
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
    public async Task<bool> IsProductNameUniqueAsync(string name, int? excludeId = null)
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        var products = await productRepository.FindAsync(p => p.Name == name && !p.IsDeleted);

        if (excludeId.HasValue)
            products = products.Where(p => p.Id != excludeId.Value);

        return !products.Any();
    }

    public async Task<List<ProductModel>> GetProductsByCategoryAsync(int categoryId)
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        var products = await productRepository.FindAsync(p => p.CategoryId == categoryId && !p.IsDeleted);

        var getproductsbycategory = products.Select(p => new ProductModel
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.SKU,
            Description = p.Description,
            CategoryId = p.CategoryId,
            CategoryName = p.CategoryName,
            CreatedBy = p.CreatedBy,
            CreatedOn = p.CreatedOn,
            UpdatedBy = p.UpdatedBy,
            UpdatedOn = p.UpdatedOn,
            IsActive = p.IsActive,
            IsDeleted = p.IsDeleted,
            Unit = p.Unit,
            UnitId = p.UnitId,
            MakeCompany = p.MakeCompany,
            MakeCompanyId = p.MakeCompanyId
        }).ToList();
        return getproductsbycategory;
    }
    public async Task<ProductModel> GetLastProductAsync()
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        var lastProduct= (await productRepository.FindAsync(p => !p.IsDeleted))
                                .OrderByDescending(p => p.Id)
                                .FirstOrDefault();

        var LastproductModel = new ProductModel
        {
            Id = lastProduct.Id,
            Name = lastProduct.Name,
            SKU = lastProduct.SKU,
            Description = lastProduct.Description,
            CategoryId = lastProduct.CategoryId,
            CategoryName = lastProduct.CategoryName,
            CreatedBy = lastProduct.CreatedBy,
            CreatedOn = lastProduct.CreatedOn,
            UpdatedBy = lastProduct.UpdatedBy,
            UpdatedOn = lastProduct.UpdatedOn,
            IsActive = lastProduct.IsActive,
            IsDeleted = lastProduct.IsDeleted,
            Unit = lastProduct.Unit,
            UnitId = lastProduct.UnitId,
            MakeCompany = lastProduct.MakeCompany,
            MakeCompanyId = lastProduct.MakeCompanyId
        };
        return LastproductModel;
    }
}