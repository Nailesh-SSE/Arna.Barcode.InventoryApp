using InventoryManagement.Services.Models;
namespace InventoryManagement.Services.Interfaces;

public interface ISaleReturnService
{
    Task<List<SaleReturnModel>> GetAllSaleReturnsAsync();
   Task<SaleReturnModel> GetSaleReturnByIdAsync(int id);
    Task<int> CreateSaleReturnAsync(SaleReturnModel model);
    Task<bool> UpdateSaleReturnAsync(SaleReturnModel model);
    //Task<bool> DeleteSaleReturnAsync(int id, int userId);
    Task<bool> CreateSaleReturnItem(SaleReturnItemsModel model);
    Task<SaleReturnValidationResult> ValidateBarcodeForSaleReturnAsync(string barcodeNo, int companyId);
    Task<bool> DeleteSaleReturnItem(int itemId);
    Task<List<SaleReturnItemsModel>> GetSaleReturnItemsByReturnId(int saleReturnId);
}