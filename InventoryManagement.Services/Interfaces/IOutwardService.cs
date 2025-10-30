using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IOutwardService
{
    Task<List<OutwardModel>> GetAllOutwardsAsync();
    Task<OutwardModel?> GetOutwardByIdAsync(int id);
    Task<bool> CreateOutwardAsync(OutwardModel model);
    Task<bool> UpdateOutwardAsync(OutwardModel model);
    Task<bool> DeleteOutwardAsync(int id);
    Task<List<OutWardItemModel>> GetOutwardDetailsByOutwardIdAsync(int outwardId);
    Task<BarcodeValidationResult> ValidateBarcodeForOutwardAsync(string barcodeNo, int outwardId);
    Task<OutWardItemModel?> AddOutwardItemAsync(int outwardId, string barcodeNo);
    Task<bool> DeleteOutwardItemAsync(int outwardDetailId, string barcodeNo);
}