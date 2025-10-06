using InventoryManagement.Core.Entities;

namespace InventoryManagement.Core.Interfaces;

public interface ISaleReturnService
{
    Task<IEnumerable<SaleReturn>> GetAllSaleReturnsAsync();
    Task<SaleReturn?> GetSaleReturnByIdAsync(int id);
    Task<bool> CreateSaleReturnAsync(SaleReturn saleReturn);
    Task<bool> UpdateSaleReturnAsync(SaleReturn saleReturn);
    Task<bool> DeleteSaleReturnAsync(int id);
    Task<IEnumerable<SaleReturn>> GetSaleReturnsByCompanyAsync(int companyId);
    
    // Simplified method names for UI
    Task<IEnumerable<SaleReturn>> GetAllAsync() => GetAllSaleReturnsAsync();
    Task AddAsync(SaleReturn saleReturn) => CreateSaleReturnAsync(saleReturn).ContinueWith(t => { });
    Task UpdateAsync(SaleReturn saleReturn) => UpdateSaleReturnAsync(saleReturn).ContinueWith(t => { });
    Task DeleteAsync(int id) => DeleteSaleReturnAsync(id).ContinueWith(t => { });
}