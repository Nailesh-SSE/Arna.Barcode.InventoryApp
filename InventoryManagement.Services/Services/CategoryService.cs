using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CategoryModel>> GetAllCategoriesAsync()
    {
        var categoryRepository = _unitOfWork.GetRepository<Category>();
        var categoryModelRepository = _unitOfWork.GetRepository<CategoryModel>();
        var entities= await categoryRepository.FindAsync(c => !c.IsDeleted);

        var categorymodels= entities.Select(c => new CategoryModel
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ParentCategoryId = c.ParentCategoryId,
            ParentCategoryName = c.ParentCategoryName,
            CreatedBy = c.CreatedBy,
            CreatedOn = c.CreatedOn,
            UpdatedBy = c.UpdatedBy,
            UpdatedOn = c.UpdatedOn,
            IsActive = c.IsActive,
            IsDeleted = c.IsDeleted
        }).ToList();
        return categorymodels;
    }

    public async Task<IEnumerable<Category>> GetRootCategoriesAsync()
    {
        var categoryRepository = _unitOfWork.GetRepository<Category>();
        return await categoryRepository.FindAsync(c => c.ParentCategoryId == null && !c.IsDeleted);
    }

    public async Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId)
    {
        var categoryRepository = _unitOfWork.GetRepository<Category>();
        return await categoryRepository.FindAsync(c => c.ParentCategoryId == parentId && !c.IsDeleted);
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        var categoryRepository = _unitOfWork.GetRepository<Category>();
        return await categoryRepository.GetByIdAsync(id);
    }

    public async Task<bool> CreateCategoryAsync(Category category)
    {
        try
        {
            if (!await IsCategoryNameUniqueAsync(category.Name))
                return false;

            if (category.ParentCategoryId.HasValue)
            {
                var parentCategory = await GetCategoryByIdAsync(category.ParentCategoryId.Value);
                //var category = new Category();
                //category.Name = categorymodel.Name;
                //category.Id = categorymodel.Id;
                //category.Description = categorymodel.Description;
                //category.ParentCategoryId=categorymodel.ParentCategoryId;
                //category.ParentCategoryName = categorymodel.ParentCategoryName;
                ////category.ParentCategory = categorymodel.ParentCategory;
                ////category.SubCategories = categorymodel.SubCategories;
                //category.Products = categorymodel.Products;
                if (parentCategory != null)
                {
                    category.ParentCategoryName = parentCategory.Name;
                }
            }

            var categoryRepository = _unitOfWork.GetRepository<Category>();
            await categoryRepository.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateCategoryAsync(Category category)
    {
        try
        {
            if (!await IsCategoryNameUniqueAsync(category.Name, category.Id))
                return false;

            if (category.ParentCategoryId.HasValue)
            {
                var parentCategory = await GetCategoryByIdAsync(category.ParentCategoryId.Value);
                if (parentCategory != null)
                {
                    category.ParentCategoryName = parentCategory.Name;
                }
            }
            else
            {
                category.ParentCategoryName = null;
            }

            var categoryRepository = _unitOfWork.GetRepository<Category>();
            categoryRepository.UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        try
        {
            var categoryRepository = _unitOfWork.GetRepository<Category>();
            var category = await categoryRepository.GetByIdAsync(id);
            if (category == null) return false;

            // Check if category has subcategories
            var subCategories = await GetSubCategoriesAsync(id);
            if (subCategories.Any())
            {
                return false; // Cannot delete category with subcategories
            }

            category.IsDeleted = true;
            category.IsActive = false;
            categoryRepository.UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsCategoryNameUniqueAsync(string name, int? excludeId = null)
    {
        var categoryRepository = _unitOfWork.GetRepository<Category>();
        var categories = await categoryRepository.FindAsync(c => c.Name == name && !c.IsDeleted);
        
        if (excludeId.HasValue)
            categories = categories.Where(c => c.Id != excludeId.Value);

        return !categories.Any();
    }
}