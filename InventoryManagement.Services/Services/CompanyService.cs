using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Core.Interfaces;

namespace InventoryManagement.Services.Services;

public class CompanyService : ICompanyService
{
    private readonly IUnitOfWork _unitOfWork;

    public CompanyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Company>> GetAllCompaniesAsync()
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        return await companyRepository.FindAsync(c => !c.IsDeleted);
    }

    public async Task<IEnumerable<Company>> GetCompaniesByTypeAsync(CompanyType type)
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        return await companyRepository.FindAsync(c => c.CompanyType == type && !c.IsDeleted);
    }

    public async Task<Company?> GetCompanyByIdAsync(int id)
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        return await companyRepository.GetByIdAsync(id);
    }

    public async Task<bool> CreateCompanyAsync(Company company)
    {
        try
        {
            if (!await IsCompanyCodeUniqueAsync(company.Code))
                return false;

            var companyRepository = _unitOfWork.GetRepository<Company>();
            await companyRepository.AddAsync(company);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateCompanyAsync(Company company)
    {
        try
        {
            if (!await IsCompanyCodeUniqueAsync(company.Code, company.Id))
                return false;

            var companyRepository = _unitOfWork.GetRepository<Company>();
            companyRepository.UpdateAsync(company);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteCompanyAsync(int id)
    {
        try
        {
            var companyRepository = _unitOfWork.GetRepository<Company>();
            var company = await companyRepository.GetByIdAsync(id);
            if (company == null) return false;

            company.IsDeleted = true;
            company.IsActive = false;
            companyRepository.UpdateAsync(company);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsCompanyCodeUniqueAsync(string code, int? excludeId = null)
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        var companies = await companyRepository.FindAsync(c => c.Code == code && !c.IsDeleted);
        
        if (excludeId.HasValue)
            companies = companies.Where(c => c.Id != excludeId.Value);

        return !companies.Any();
    }
}