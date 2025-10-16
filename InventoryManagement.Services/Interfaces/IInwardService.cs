using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IInwardService
{
    Task<List<InwardModel>> GetAllAsync();
    Task<InwardModel?> GetByIdAsync(int id);
    Task<bool> CreateAsync(InwardModel model);
    Task<bool> UpdateAsync(InwardModel model);
    Task<bool> DeleteAsync(int id);

    Task<List<InwardItemModel>> GetInwardItemsByInwardIdAsync(int inwardId);
}
