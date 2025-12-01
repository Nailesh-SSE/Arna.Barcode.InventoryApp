using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IInwardService
{
    Task<List<InwardModel>> GetAllAsync();
    Task<InwardModel?> GetByIdAsync(int id);
    Task<bool> CreateAsync(InwardModel model);
    Task<bool> UpdateAsync(InwardModel model);
    Task<bool> DeleteAsync(int id,int deletedBy);

    Task<List<InwardItemModel>> GetInwardItemsByInwardIdAsync(int inwardId);
    Task<bool> CreateInwardItemAsync(InwardItemModel model);
    Task<bool> UpdateInwardItemAsync(InwardItemModel model);
    Task<bool> DeleteInwardItemAsync(int id);

    Task<int> AddReturnedItemToExistingInwardAsync(int inwardId, int productId, int quantity, int createdBy);
    Task RemoveReturnedReturnedItemFromStockAsync(int inwardItemId);
    Task<bool> AreAllBarcodesInStock(int itemId);
}
