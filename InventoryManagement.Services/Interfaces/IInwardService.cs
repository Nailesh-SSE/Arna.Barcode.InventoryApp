using InventoryManagement.Core.Entities;

namespace InventoryManagement.Services.Interfaces;

public interface IInwardService
{
    Task<IEnumerable<Inward>> GetAllInwardsAsync();
    Task<Inward?> GetInwardByIdAsync(int id);
    Task<bool> CreateInwardAsync(Inward inward, List<InwardItem> items);
    Task<bool> UpdateInwardAsync(Inward inward);
    Task<bool> DeleteInwardAsync(int id);
    Task<string> GenerateBarcodeNumberAsync(int inwardId, int inwardItemId, string batchNo);
    Task<IEnumerable<InwardBarcodeItem>> GetBarcodeItemsByInwardIdAsync(int inwardId);
    Task<IEnumerable<InwardItem>> GetInwardItemsByInwardIdAsync(int inwardId);
  
}