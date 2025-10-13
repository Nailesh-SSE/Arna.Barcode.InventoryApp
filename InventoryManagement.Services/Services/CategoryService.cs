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

    public async Task<List<CategoryModel>> GetAllCategoriesAsync()
    {
        var categoryRepository = _unitOfWork.GetRepository<Category>();
        var entities = await categoryRepository.FindAsync(c => !c.IsDeleted);

        var categorymodels = entities.Select(c => new CategoryModel
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

    public async Task<List<CategoryModel>> GetRootCategoriesAsync()
    {
        var categoryRepository = _unitOfWork.GetRepository<Category>();
        var rootcategories = await categoryRepository.FindAsync(c => c.ParentCategoryId == null && !c.IsDeleted);

        var getrootcategories = rootcategories.Select(c => new CategoryModel
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
        return getrootcategories;
    }

    public async Task<List<CategoryModel>> GetSubCategoriesAsync(int parentId)
    {
        var categoryRepository = _unitOfWork.GetRepository<Category>();
        var subcategories = await categoryRepository.FindAsync(c => c.ParentCategoryId == parentId && !c.IsDeleted);

        var getsubcategories = subcategories.Select(c => new CategoryModel
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
        return getsubcategories;
    }

    public async Task<CategoryModel?> GetCategoryByIdAsync(int id)
    {
        var categoryRepository = _unitOfWork.GetRepository<Category>();
        var categorybyid = await categoryRepository.GetByIdAsync(id);
        if (categorybyid == null)
            return null;
        var getcategorybyid = new CategoryModel
        {
            Id = categorybyid.Id,
            Name = categorybyid.Name,
            Description = categorybyid.Description,
            ParentCategoryId = categorybyid.ParentCategoryId,
            ParentCategoryName = categorybyid.ParentCategoryName,
            CreatedBy = categorybyid.CreatedBy,
            CreatedOn = categorybyid.CreatedOn,
            UpdatedBy = categorybyid.UpdatedBy,
            UpdatedOn = categorybyid.UpdatedOn,
            IsActive = categorybyid.IsActive,
            IsDeleted = categorybyid.IsDeleted
        };
        return getcategorybyid;
    }

    public async Task<bool> CreateCategoryAsync(CategoryModel categoryModel)
    {
        try
        {
            if (!await IsCategoryNameUniqueAsync(categoryModel.Name))
                return false;

            if (categoryModel.ParentCategoryId.HasValue)
            {
                var parentCategory = await GetCategoryByIdAsync(categoryModel.ParentCategoryId.Value);

                if (parentCategory != null)
                {
                    categoryModel.ParentCategoryName = parentCategory.Name;
                }
            }
            var createcategory = new Category
            {
                Name = categoryModel.Name,
                Description = categoryModel.Description,
                ParentCategoryId = categoryModel.ParentCategoryId,
                ParentCategoryName = categoryModel.ParentCategoryName,
                CreatedBy = categoryModel.CreatedBy,
                CreatedOn = categoryModel.CreatedOn,
                UpdatedBy = categoryModel.UpdatedBy,
                UpdatedOn = categoryModel.UpdatedOn,
                IsActive = categoryModel.IsActive,
                IsDeleted = categoryModel.IsDeleted
            };
            var categoryRepository = _unitOfWork.GetRepository<Category>();
            await categoryRepository.AddAsync(createcategory);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateCategoryAsync(CategoryModel categoryModel)
    {
        try
        {
            var categoryRepository = _unitOfWork.GetRepository<Category>();

            var existingCategory = await categoryRepository.GetByIdAsync(categoryModel.Id);
            
            if (!await IsCategoryNameUniqueAsync(categoryModel.Name,categoryModel.Id))
                
                return false;

            if (categoryModel.ParentCategoryId.HasValue)
            {
                var parentCategory = await GetCategoryByIdAsync(categoryModel.ParentCategoryId.Value);
                existingCategory.ParentCategoryName = parentCategory?.Name;
            }
            else
            {
                existingCategory.ParentCategoryName = null;
            }

            existingCategory.Name = categoryModel.Name;
            existingCategory.Description = categoryModel.Description;
            existingCategory.ParentCategoryId = categoryModel.ParentCategoryId;
            existingCategory.UpdatedBy = categoryModel.UpdatedBy;
            existingCategory.UpdatedOn = categoryModel.UpdatedOn;
            existingCategory.IsActive = categoryModel.IsActive;
            existingCategory.IsDeleted = categoryModel.IsDeleted;

            categoryRepository.UpdateAsync(existingCategory);
            await _unitOfWork.SaveChangesAsync();
            return true;

        }
        catch (Exception ex)
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