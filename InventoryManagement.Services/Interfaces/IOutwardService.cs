using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IOutwardService
{
    Task<List<OutwardModel>> GetAllOutwardsAsync();
    Task<OutwardModel?> GetOutwardByIdAsync(int id);
    Task<bool> CreateOutwardAsync(OutwardModel outward, List<int> barcodeItemIds);
    Task<bool> UpdateOutwardAsync(OutwardModel outward);
    Task<bool> DeleteOutwardAsync(int id);
    Task GenerateOutwardNumberAsync(OutwardModel model);
    Task<IEnumerable<InwardBarcodeItem>> GetAvailableBarcodeItemsAsync();
    Task<bool> IsBarcodeAvailableAsync(string barcodeNo);
    Task<List<OutWardItem>> GetOutwardDetailsByOutwardIdAsync(int outwardId);
    Task<bool> UpdateAsync(OutwardModel model);
}