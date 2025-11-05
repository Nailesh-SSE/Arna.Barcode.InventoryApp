using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Models;
namespace InventoryManagement.Services.Interfaces;

public interface ISaleReturnService
{
    Task<List<SaleReturnModel>> GetAllSaleReturnsAsync();
    Task<SaleReturnModel?> GetSaleReturnByIdAsync(int id);
    Task<bool> CreateSaleReturnAsync(SaleReturnModel model);
    Task<bool> UpdateSaleReturnAsync(SaleReturnModel model);
    Task<bool> DeleteSaleReturnAsync(int id, int userId);
    Task<SaleReturnValidationResult> ValidateBarcodeForSaleReturnAsync(string barcodeNo, int companyId);
    Task<IEnumerable<SaleReturnModel>> GetSaleReturnsByCompanyAsync(int companyId);
}