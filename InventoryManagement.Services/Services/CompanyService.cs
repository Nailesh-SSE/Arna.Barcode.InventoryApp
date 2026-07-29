using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

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
        var company = await companyRepository.FindAsync(c => !c.IsDeleted);

        var imageRepo = _unitOfWork.GetRepository<ImageMapper>();
        var images = await imageRepo.FindAsync(i => i.Type == "Company" && !i.IsDeleted);
        var imageDict = images.GroupBy(i => i.ItemId).ToDictionary(g => g.Key, g => g.FirstOrDefault()?.ImagePath);

        var model = company.Select(c => new CompanyModel
        {
            Id = c.Id,
            Name = c.Name,
            Code = c.Code,
            CompanyType = c.CompanyType,
            IsActive = c.IsActive,
            Remark = c.Remark,
            ImagePath = imageDict.TryGetValue(c.Id, out var imgPath) ? imgPath : null

        }).ToList();
        return model;
    }

    public async Task<List<CompanyModel>> GetCompaniesByTypeAsync(CompanyType type)
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        var company = await companyRepository.FindAsync(c => c.CompanyType == type && !c.IsDeleted);
        if (company == null)
            return new List<CompanyModel>();

        var imageRepo = _unitOfWork.GetRepository<ImageMapper>();
        var images = await imageRepo.FindAsync(i => i.Type == "Company" && !i.IsDeleted);
        var imageDict = images.GroupBy(i => i.ItemId).ToDictionary(g => g.Key, g => g.FirstOrDefault()?.ImagePath);

        var model = company.Select(c => new CompanyModel
        {
            Id = c.Id,
            Name = c.Name,
            Code = c.Code,
            CompanyType = c.CompanyType,
            Remark = c.Remark,
            IsActive = c.IsActive,
            ImagePath = imageDict.TryGetValue(c.Id, out var imgPath) ? imgPath : null
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
                Name = companyModel.Name.Trim(),
                IsActive = true,
                CompanyType = companyModel.CompanyType,
                SerialNumber = companyModel.SerialNumber,
                Remark = companyModel.Remark,
                CreatedBy = companyModel.CreatedBy,
                CreatedOn = DateTime.Now
            };

            await companyRepository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            if (!string.IsNullOrEmpty(companyModel.ImagePath))
            {
                var imageRepo = _unitOfWork.GetRepository<ImageMapper>();
                var imageEntity = new ImageMapper
                {
                    ItemId = entity.Id,
                    Type = "Company",
                    ImagePath = companyModel.ImagePath,
                    DisplayOrder = 1,
                    CreatedBy = companyModel.CreatedBy,
                    CreatedOn = DateTime.Now,
                    IsActive = true,
                    IsDeleted = false
                };
                await imageRepo.AddAsync(imageEntity);
                await _unitOfWork.SaveChangesAsync();
            }

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
            entity.Name = companyModel.Name.Trim();
            entity.CompanyType = companyModel.CompanyType;
            entity.Id = companyModel.Id;
            entity.IsActive = companyModel.IsActive;
            entity.IsDeleted = false;
            entity.Remark = companyModel.Remark;
            entity.UpdatedBy = companyModel.UpdatedBy;
            entity.UpdatedOn = DateTime.Now;

            companyRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            var imageRepo = _unitOfWork.GetRepository<ImageMapper>();
            var existingImages = (await imageRepo.FindAsync(i => i.ItemId == companyModel.Id && i.Type == "Company" && !i.IsDeleted)).ToList();

            if (string.IsNullOrEmpty(companyModel.ImagePath))
            {
                foreach (var img in existingImages)
                {
                    img.IsDeleted = true;
                    img.IsActive = false;
                    img.UpdatedBy = companyModel.UpdatedBy;
                    img.UpdatedOn = DateTime.Now;
                    imageRepo.Update(img);
                }
            }
            else
            {
                var firstImg = existingImages.FirstOrDefault();
                if (firstImg != null)
                {
                    firstImg.ImagePath = companyModel.ImagePath;
                    firstImg.UpdatedBy = companyModel.UpdatedBy;
                    firstImg.UpdatedOn = DateTime.Now;
                    imageRepo.Update(firstImg);

                    foreach (var extraImg in existingImages.Skip(1))
                    {
                        extraImg.IsDeleted = true;
                        extraImg.IsActive = false;
                        extraImg.UpdatedBy = companyModel.UpdatedBy;
                        extraImg.UpdatedOn = DateTime.Now;
                        imageRepo.Update(extraImg);
                    }
                }
                else
                {
                    var imageEntity = new ImageMapper
                    {
                        ItemId = companyModel.Id,
                        Type = "Company",
                        ImagePath = companyModel.ImagePath,
                        DisplayOrder = 1,
                        CreatedBy = companyModel.UpdatedBy,
                        CreatedOn = DateTime.Now,
                        IsActive = true,
                        IsDeleted = false
                    };
                    await imageRepo.AddAsync(imageEntity);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> DeleteCompanyAsync(int id,int deletedBy)
    {
        try
        {
            var companyRepository = _unitOfWork.GetRepository<Company>();
            var company = await companyRepository.GetByIdAsync(id);
            if (company == null) return false;

            company.IsDeleted = true;
            company.IsActive = false;
            company.UpdatedBy = deletedBy;
            company.UpdatedOn = DateTime.Now;
            companyRepository.Update(company);

            var imageRepo = _unitOfWork.GetRepository<ImageMapper>();
            var images = await imageRepo.FindAsync(i => i.ItemId == id && i.Type == "Company" && !i.IsDeleted);
            foreach (var img in images)
            {
                img.IsDeleted = true;
                img.IsActive = false;
                img.UpdatedBy = deletedBy;
                img.UpdatedOn = DateTime.Now;
                imageRepo.Update(img);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> IsCompanyCodeUniqueAsync(string code, int? Id = null)
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        var companies = await companyRepository.FindAsync(c => c.Code == code && !c.IsDeleted);

        if (Id > 0)
            companies = companies.Where(c => c.Id != Id.Value);

        return !companies.Any();
    }
    public async Task<bool> IsCompanyNameUniqueAsync(string Name, CompanyType type, int? Id = null)
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        var companies = await companyRepository.FindAsync(c => c.Name.Trim().ToLower() == Name.Trim().ToLower() && c.CompanyType == type
        && c.IsActive && !c.IsDeleted && c.Id != Id);

        return !companies.Any();
    }
    public async Task GenerateCompanyCodeAndSquenceNumberAsync(CompanyModel model)
    {
        var companyRepository = _unitOfWork.GetRepository<Company>();
        var companies = await companyRepository.GetAllAsync();
        var company = companies.Where(a => !a.IsDeleted).OrderByDescending(a => a.Id).FirstOrDefault();
        model.SerialNumber = company != null ? company.SerialNumber + 1 : 0;
        model.Code = "Comp" + model.SerialNumber;
    }
    public async Task<byte[]> ExportToExcelAsync(CompanyType type)
    {
        var companies=await GetCompaniesByTypeAsync(type);
        IWorkbook workbook = new XSSFWorkbook();
        ISheet sheet = workbook.CreateSheet(type == CompanyType.ShipTo ? "Ship Company Report" : "Bill Company Report");

        // Header row
        IRow headerRow = sheet.CreateRow(0);
        string[] headers = new string[]
        {
            "No.",
            type == CompanyType.ShipTo ? "Ship Company Name" : "Bill Company Name",
            "Remark",
            "Status"
        };

        for (int i = 0; i < headers.Length; i++)
            {
                headerRow.CreateCell(i).SetCellValue(headers[i]);
            }
        var counter = 1;
        // Data rows
        foreach (var company in companies)
        {
            var i = counter;
            IRow row = sheet.CreateRow(i);
            row.CreateCell(0).SetCellValue(i);
            row.CreateCell(1).SetCellValue(company.Name);
            row.CreateCell(2).SetCellValue(company.Remark);
            row.CreateCell(3).SetCellValue(company.IsActive ? "Active" : "Inactive");
            counter++;
        }

        // Autosize all columns
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.AutoSizeColumn(i);
        }

        // Write to memory stream and return as byte array
        using (var exportData = new MemoryStream())
        {
            workbook.Write(exportData);
            return exportData.ToArray();
        }

    }
}