using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IFormMasterService
{
    Task<List<FormMasterModel>> GetAllAsync();
    Task<List<FormMasterModel>> GetParentFormsAsync();
    Task<FormMasterModel?> GetByIdAsync(int id);
    Task<bool> CreateAsync(FormMasterModel model);
    Task<bool> UpdateAsync(FormMasterModel model);
    Task<bool> DeleteAsync(int id, int userId);
    Task<bool> IsFormNameUniqueAsync(string name, int id = 0);
    Task<bool> IsDisplayIndexInUseAsync(int index,int? parentid, int id = 0);
}
