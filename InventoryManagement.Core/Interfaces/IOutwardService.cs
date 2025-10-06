using InventoryManagement.Core.Entities;

namespace InventoryManagement.Core.Interfaces;

public interface IOutwardService
{
    Task<IEnumerable<Outward>> GetAllOutwardsAsync();
    Task<Outward?> GetOutwardByIdAsync(int id);
    Task<bool> CreateOutwardAsync(Outward outward, List<int> barcodeItemIds);
    Task<bool> UpdateOutwardAsync(Outward outward);
    Task<bool> DeleteOutwardAsync(int id);
    Task<IEnumerable<InwardBarcodeItem>> GetAvailableBarcodeItemsAsync();
    Task<bool> IsBarcodeAvailableAsync(string barcodeNo);
    Task<IEnumerable<OutwardDetail>> GetOutwardDetailsByOutwardIdAsync(int outwardId);
    
    // Simplified method names for UI
    Task<IEnumerable<Outward>> GetAllAsync() => GetAllOutwardsAsync();
    Task AddAsync(Outward outward) => CreateOutwardAsync(outward, new List<int>()).ContinueWith(t => { });
    Task UpdateAsync(Outward outward) => UpdateOutwardAsync(outward).ContinueWith(t => { });
    Task DeleteAsync(int id) => DeleteOutwardAsync(id).ContinueWith(t => { });
}