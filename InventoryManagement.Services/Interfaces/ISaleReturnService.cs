using InventoryManagement.Core.Entities;
namespace InventoryManagement.Services.Interfaces;

public interface ISaleReturnService
{
    Task<IEnumerable<SaleReturn>> GetAllSaleReturnsAsync();
    Task<SaleReturn?> GetSaleReturnByIdAsync(int id);
    Task<bool> CreateSaleReturnAsync(SaleReturn saleReturn);
    Task<bool> UpdateSaleReturnAsync(SaleReturn saleReturn);
    Task<bool> DeleteSaleReturnAsync(int id);
    Task<IEnumerable<SaleReturn>> GetSaleReturnsByCompanyAsync(int companyId);
  
}