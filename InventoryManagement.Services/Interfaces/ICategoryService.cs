using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;
public interface ICategoryService
{
    Task<List<CategoryModel>> GetAllCategoriesAsync();
    Task<List<CategoryModel>> GetRootCategoriesAsync();
    Task<List<CategoryModel>> GetSubCategoriesAsync(int parentId);
    Task<CategoryModel?> GetCategoryByIdAsync(int id);
    Task<bool> CreateCategoryAsync(CategoryModel category);
    Task<bool> UpdateCategoryAsync(CategoryModel category);
    Task<bool> DeleteCategoryAsync(int id);
    Task<bool> IsCategoryNameUniqueAsync(string name, int? excludeId = null);
 }