using InventoryManagement.Services.DTO;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IInwardService
{
    Task<List<InwardModel>> GetAllAsync(bool isAdmin);
    Task<InwardModel?> GetByIdAsync(int id);
    Task<int> CreateAsync(InwardModel model);
    Task<bool> UpdateAsync(InwardModel model);
    Task<bool> DeleteAsync(int id, int deletedBy);

    Task<List<InwardItemModel>> GetInwardItemsByInwardIdAsync(int inwardId);
    Task<bool> CreateInwardItemAsync(InwardItemModel model);
    Task<bool> UpdateInwardItemAsync(InwardItemModel model);
    Task<bool> DeleteInwardItemAsync(int id);
    Task<int> AddReturnedItemToExistingInwardAsync(ReturnItemDto parameter);
    Task<bool> AddReturnItemToSaleInwardAsync(SaleToInwardDto saleReturnItems);
    Task RemoveReturnedReturnedItemFromStockAsync(int inwardItemId);
    Task<byte[]> GenerateItemBarcodePrnAsync(int inwardItemId);
}
