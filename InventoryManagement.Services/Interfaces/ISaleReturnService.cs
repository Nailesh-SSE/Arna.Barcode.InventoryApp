using InventoryManagement.Services.Models;
namespace InventoryManagement.Services.Interfaces;

public interface ISaleReturnService
{
    Task<List<SaleReturnModel>> GetAllSaleReturnsAsync(bool isAdmin);
   Task<SaleReturnModel> GetSaleReturnByIdAsync(int id);
    Task<int> CreateSaleReturnAsync(SaleReturnModel model);
    Task<bool> UpdateSaleReturnAsync(SaleReturnModel model);
    //Task<bool> DeleteSaleReturnAsync(int id, int userId);
    Task<bool> CreateSaleReturnItem(SaleReturnItemsModel model);
    Task<bool> UpdateSaleReturnItem(SaleReturnItemsModel model);
    Task<SaleReturnValidationResult> ValidateBarcodeForSaleReturnAsync(string barcodeNo);
    Task<bool> DeleteSaleReturnItem(int itemId,int userId);
    Task<List<SaleReturnItemsModel>> GetSaleReturnItemsByReturnId(int saleReturnId);
}