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

        var imageRepo = _unitOfWork.GetRepository<ImageMapper>();
        var images = await imageRepo.FindAsync(i => i.Type == "Platform" && !i.IsDeleted);
        var imageDict = images.GroupBy(i => i.ItemId).ToDictionary(g => g.Key, g => g.FirstOrDefault()?.ImagePath);

        var platformModels = entities.Select(p => new PlatformModel
        {
            Id = p.Id,
            Name = p.Name,
            Remark = p.Remark,
            ImagePath = imageDict.TryGetValue(p.Id, out var imgPath) ? imgPath : null,
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
                CreatedBy = platformModel.CreatedBy,
                CreatedOn = platformModel.CreatedOn,
                IsActive = platformModel.IsActive,
                IsDeleted = platformModel.IsDeleted
            };
            await categoryRepository.AddAsync(newPlatform);
            await _unitOfWork.SaveChangesAsync();

            if (!string.IsNullOrEmpty(platformModel.ImagePath))
            {
                var imageRepo = _unitOfWork.GetRepository<ImageMapper>();
                var imageEntity = new ImageMapper
                {
                    ItemId = newPlatform.Id,
                    Type = "Platform",
                    ImagePath = platformModel.ImagePath,
                    DisplayOrder = 1,
                    CreatedBy = platformModel.CreatedBy,
                    CreatedOn = platformModel.CreatedOn,
                    IsActive = true,
                    IsDeleted = false
                };
                await imageRepo.AddAsync(imageEntity);
                await _unitOfWork.SaveChangesAsync();
            }

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
        existingPlatform.UpdatedBy = platformModel.UpdatedBy;
        existingPlatform.UpdatedOn = DateTime.Now;
        existingPlatform.IsActive = platformModel.IsActive;
        existingPlatform.IsDeleted = platformModel.IsDeleted;

        platformRepository.Update(existingPlatform);
        await _unitOfWork.SaveChangesAsync();

        var imageRepo = _unitOfWork.GetRepository<ImageMapper>();
        var existingImages = (await imageRepo.FindAsync(i => i.ItemId == platformModel.Id && i.Type == "Platform" && !i.IsDeleted)).ToList();

        if (string.IsNullOrEmpty(platformModel.ImagePath))
        {
            foreach (var img in existingImages)
            {
                img.IsDeleted = true;
                img.IsActive = false;
                img.UpdatedBy = platformModel.UpdatedBy;
                img.UpdatedOn = DateTime.Now;
                imageRepo.Update(img);
            }
        }
        else
        {
            var firstImg = existingImages.FirstOrDefault();
            if (firstImg != null)
            {
                firstImg.ImagePath = platformModel.ImagePath;
                firstImg.UpdatedBy = platformModel.UpdatedBy;
                firstImg.UpdatedOn = DateTime.Now;
                imageRepo.Update(firstImg);

                foreach (var extraImg in existingImages.Skip(1))
                {
                    extraImg.IsDeleted = true;
                    extraImg.IsActive = false;
                    extraImg.UpdatedBy = platformModel.UpdatedBy;
                    extraImg.UpdatedOn = DateTime.Now;
                    imageRepo.Update(extraImg);
                }
            }
            else
            {
                var imageEntity = new ImageMapper
                {
                    ItemId = platformModel.Id,
                    Type = "Platform",
                    ImagePath = platformModel.ImagePath,
                    DisplayOrder = 1,
                    CreatedBy = platformModel.UpdatedBy,
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

        var imageRepo = _unitOfWork.GetRepository<ImageMapper>();
        var images = await imageRepo.FindAsync(i => i.ItemId == id && i.Type == "Platform" && !i.IsDeleted);
        foreach (var img in images)
        {
            img.IsDeleted = true;
            img.IsActive = false;
            img.UpdatedBy = userid;
            img.UpdatedOn = DateTime.Now;
            imageRepo.Update(img);
        }

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
