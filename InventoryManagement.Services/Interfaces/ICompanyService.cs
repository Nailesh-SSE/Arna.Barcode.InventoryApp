using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Services.Interfaces;

public interface ICompanyService
{
    Task<IEnumerable<Company>> GetAllCompaniesAsync();
    Task<IEnumerable<Company>> GetCompaniesByTypeAsync(CompanyType type);
    Task<Company?> GetCompanyByIdAsync(int id);
    Task<bool> CreateCompanyAsync(Company company);
    Task<bool> UpdateCompanyAsync(Company company);
    Task<bool> DeleteCompanyAsync(int id);
    Task<bool> IsCompanyCodeUniqueAsync(string code, int? excludeId = null);
}