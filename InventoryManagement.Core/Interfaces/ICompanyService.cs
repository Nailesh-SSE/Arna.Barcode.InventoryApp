using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Core.Interfaces;

public interface ICompanyService
{
    Task<IEnumerable<Company>> GetAllCompaniesAsync();
    Task<IEnumerable<Company>> GetCompaniesByTypeAsync(CompanyType type);
    Task<Company?> GetCompanyByIdAsync(int id);
    Task<bool> CreateCompanyAsync(Company company);
    Task<bool> UpdateCompanyAsync(Company company);
    Task<bool> DeleteCompanyAsync(int id);
    Task<bool> IsCompanyCodeUniqueAsync(string code, int? excludeId = null);
    
    // Simplified method names for UI
    Task<IEnumerable<Company>> GetAllAsync() => GetAllCompaniesAsync();
    Task AddAsync(Company company) => CreateCompanyAsync(company).ContinueWith(t => { });
    Task UpdateAsync(Company company) => UpdateCompanyAsync(company).ContinueWith(t => { });
    Task DeleteAsync(int id) => DeleteCompanyAsync(id).ContinueWith(t => { });
}