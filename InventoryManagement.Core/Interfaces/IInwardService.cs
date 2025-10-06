using InventoryManagement.Core.Entities;

namespace InventoryManagement.Core.Interfaces;

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
    
    // Simplified method names for UI
    Task<IEnumerable<Inward>> GetAllAsync() => GetAllInwardsAsync();
    Task AddAsync(Inward inward) => CreateInwardAsync(inward, inward.InwardItems?.ToList() ?? new List<InwardItem>()).ContinueWith(t => { });
    Task UpdateAsync(Inward inward) => UpdateInwardAsync(inward).ContinueWith(t => { });
    Task DeleteAsync(int id) => DeleteInwardAsync(id).ContinueWith(t => { });
}