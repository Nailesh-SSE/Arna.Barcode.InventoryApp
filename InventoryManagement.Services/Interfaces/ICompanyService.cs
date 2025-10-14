using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface ICompanyService
{
    Task<List<CompanyModel>> GetAllCompaniesAsync();
    Task<List<CompanyModel>> GetCompaniesByTypeAsync(CompanyType type);
    Task<Company?> GetCompanyByIdAsync(int id);
    Task<bool> CreateCompanyAsync(CompanyModel companyModel);
    Task<bool> UpdateCompanyAsync(CompanyModel companyModel);
    Task<bool> DeleteCompanyAsync(int id);
    Task<bool> IsCompanyCodeUniqueAsync(string code, int? excludeId = null);
    Task<bool> IsCompanyNameUniqueAsync(string Name, CompanyType type, int? Id = null);
    Task GenerateCompanyCodeAndSquenceNumberAsync(CompanyModel companyModel);
}