using InventoryManagement.Services.DTO;
using InventoryManagement.Services.Models;
using System.Threading.Tasks;

namespace InventoryManagement.Services.Interfaces;

public interface IInwardService
{
    Task<List<InwardModel>> GetAllAsync();
    Task<InwardModel?> GetByIdAsync(int id);
    Task<bool> CreateAsync(InwardModel model);
    Task<bool> UpdateAsync(InwardModel model);
    Task<bool> DeleteAsync(int id, int deletedBy);

    Task<List<InwardItemModel>> GetInwardItemsByInwardIdAsync(int inwardId);
    Task<bool> CreateInwardItemAsync(InwardItemModel model);
    Task<bool> UpdateInwardItemAsync(InwardItemModel model);
    Task<bool> DeleteInwardItemAsync(int id);
    Task<int> AddReturnedItemToExistingInwardAsync(ReturnItemDto parameter);
    Task RemoveReturnedReturnedItemFromStockAsync(int inwardItemId);
    Task<byte[]> GenerateItemBarcodePdfAsync(int inwardItemId);
    Task<bool> AreAllBarcodesInStock(int itemId);
}
