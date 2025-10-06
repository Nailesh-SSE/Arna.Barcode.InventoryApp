using InventoryManagement.Core.Entities;

namespace InventoryManagement.Core.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    Task<IEnumerable<Category>> GetRootCategoriesAsync();
    Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId);
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<bool> CreateCategoryAsync(Category category);
    Task<bool> UpdateCategoryAsync(Category category);
    Task<bool> DeleteCategoryAsync(int id);
    Task<bool> IsCategoryNameUniqueAsync(string name, int? excludeId = null);
    
    // Simplified method names for UI
    Task<IEnumerable<Category>> GetAllAsync() => GetAllCategoriesAsync();
    Task AddAsync(Category category) => CreateCategoryAsync(category).ContinueWith(t => { });
    Task UpdateAsync(Category category) => UpdateCategoryAsync(category).ContinueWith(t => { });
    Task DeleteAsync(int id) => DeleteCategoryAsync(id).ContinueWith(t => { });
}