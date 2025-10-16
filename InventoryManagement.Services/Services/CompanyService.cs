using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services;

public class CompanyService : ICompanyService
{
    private readonly IUnitOfWork _unitOfWork;

    public CompanyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CompanyModel>> GetAllCompaniesAsync()
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        var entities = await companyRepository.FindAsync(c => !c.IsDeleted);
        var model = entities.Select(c => new CompanyModel
        {
            Id = c.Id,
            Name = c.Name,
            Code = c.Code,
            CompanyType = c.CompanyType,
            IsActive = c.IsActive,
            Remark = c.Remark

        }).ToList();
        return model;
    }

    public async Task<List<CompanyModel>> GetCompaniesByTypeAsync(CompanyType type)
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        var entities = await companyRepository.FindAsync(c => c.CompanyType == type && !c.IsDeleted);
        var model = entities.Select(c => new CompanyModel
        {
            Id = c.Id,
            Name = c.Name,
            Code = c.Code,
            CompanyType = c.CompanyType,
            Remark = c.Remark,
            IsActive = c.IsActive
        }).ToList();
        return model;
    }

    public async Task<Company?> GetCompanyByIdAsync(int id)
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        return await companyRepository.GetByIdAsync(id);
    }

    public async Task<bool> CreateCompanyAsync(CompanyModel companyModel)
    {
        try
        {
            var companyRepository = _unitOfWork.GetRepository<Company>();
            await GenerateCompanyCodeAndSquenceNumberAsync(companyModel);

            var entity = new Company
            {
                Id = companyModel.Id,
                Code = companyModel.Code,
                Name = companyModel.Name.Trim().ToLower(),
                IsActive = companyModel.IsActive,
                CompanyType = companyModel.CompanyType,
                SerialNumber = companyModel.SerialNumber,
                Remark = companyModel.Remark
            };


            await companyRepository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> UpdateCompanyAsync(CompanyModel companyModel)
    {
        try
        {
            if (!await IsCompanyCodeUniqueAsync(companyModel.Code, companyModel.Id))
                return false;

            var companyRepository = _unitOfWork.GetRepository<Company>();
            var entity = await companyRepository.GetByIdAsync(companyModel.Id);

            entity.Code = companyModel.Code;
            entity.Name = companyModel.Name.Trim().ToLower();
            entity.CompanyType = companyModel.CompanyType;
            entity.Id = companyModel.Id;
            entity.IsActive = companyModel.IsActive;
            entity.IsDeleted = false;
            entity.Remark = companyModel.Remark;

            companyRepository.UpdateAsync(entity);
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

    public async Task<bool> IsCompanyCodeUniqueAsync(string code, int? Id = null)
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        var companies = await companyRepository.FindAsync(c => c.Code == code && !c.IsDeleted);

        if (Id.HasValue)
            companies = companies.Where(c => c.Id != Id.Value);

        return !companies.Any();
    }
    public async Task<bool> IsCompanyNameUniqueAsync(string Name, CompanyType type, int? Id = null)
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        var companies = await companyRepository.FindAsync(c => c.Name.Trim().ToLower() == Name.Trim().ToLower() && c.CompanyType == type
        && c.IsActive && !c.IsDeleted && c.Id != Id );
     
        return !companies.Any();
    }
    public async Task GenerateCompanyCodeAndSquenceNumberAsync(CompanyModel model)
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        var companies = await companyRepository.GetAllAsync();
        var company = companies.Where(a => a.IsActive && !a.IsDeleted).OrderByDescending(a => a.Id).FirstOrDefault();
        model.SerialNumber = company != null ? company.SerialNumber + 1 : 0;
        model.Code = model.Name + "-" + model.SerialNumber;
    }
}