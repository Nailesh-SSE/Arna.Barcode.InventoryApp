using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Services;

public class PlatformService : IPlatformService
{
    private readonly IUnitOfWork _unitOfWork;

    public PlatformService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<List<PlatformModel>> GetAllPlatformAsync()
    {
        var platformRepository = _unitOfWork.GetRepository<Platform>();
        var entities = await platformRepository.FindAsync(p => !p.IsDeleted);
        var platformModels = entities.Select(p => new PlatformModel
        {
            Id = p.Id,
            Name = p.Name,
            Remark = p.Remark,
            ImagePath = p.ImagePath,
            CreatedBy = p.CreatedBy,
            CreatedOn = p.CreatedOn,
            UpdatedBy = p.UpdatedBy,
            UpdatedOn = p.UpdatedOn,
            IsActive = p.IsActive,
            IsDeleted = p.IsDeleted
        }).ToList();
        return platformModels;
    }
    public async Task<bool> CreatePlatformAsync(PlatformModel platformModel)
    {
        try
        {
            var categoryRepository = _unitOfWork.GetRepository<Platform>();
            if (platformModel == null && !await IsPlatformNameUniqueAsync(platformModel.Name))
                return false;
            var newPlatform = new Platform
            {
                Name = platformModel.Name,
                Remark = platformModel.Remark,
                ImagePath = platformModel.ImagePath,
                CreatedBy = platformModel.CreatedBy,
                CreatedOn = platformModel.CreatedOn,
                IsActive = platformModel.IsActive,
                IsDeleted = platformModel.IsDeleted
            };
            await categoryRepository.AddAsync(newPlatform);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
    public async Task<bool> UpdatePlatformAsync(PlatformModel platformModel)
    {
        var platformRepository = _unitOfWork.GetRepository<Platform>();
        var existingPlatform = await platformRepository.GetByIdAsync(platformModel.Id);
        if (existingPlatform == null && !await IsPlatformNameUniqueAsync(platformModel.Name, platformModel.Id))
            return false;

        existingPlatform.Name = platformModel.Name;
        existingPlatform.Remark = platformModel.Remark;
        existingPlatform.ImagePath = platformModel.ImagePath;
        existingPlatform.UpdatedBy = platformModel.UpdatedBy;
        existingPlatform.UpdatedOn = DateTime.Now;
        existingPlatform.IsActive = platformModel.IsActive;
        existingPlatform.IsDeleted = platformModel.IsDeleted;

        platformRepository.Update(existingPlatform);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeletePlatformAsync(int id, int userid)
    {
        var platformRepository = _unitOfWork.GetRepository<Platform>();
        var existingPlatform = await platformRepository.GetByIdAsync(id);
        if (existingPlatform == null)
            return false;
        existingPlatform.IsDeleted = true;
        existingPlatform.IsActive = false;
        existingPlatform.UpdatedBy = userid;
        existingPlatform.UpdatedOn = DateTime.Now;
        platformRepository.Update(existingPlatform);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    public async Task<bool> IsPlatformNameUniqueAsync(string name, int? excludeId = null)
    {
        var platformRepository = _unitOfWork.GetRepository<Platform>();
        var platforms = await platformRepository.FindAsync(p => p.Name.ToLower() == name.ToLower() && !p.IsDeleted);

        if (excludeId.HasValue)
        {
            platforms = platforms.Where(p => p.Id != excludeId.Value).ToList();
        }
        return !platforms.Any();
    }
}
