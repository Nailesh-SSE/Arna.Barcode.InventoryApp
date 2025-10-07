using InventoryManagement.Core.Entities;

namespace InventoryManagement.Services.Interfaces;

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
}