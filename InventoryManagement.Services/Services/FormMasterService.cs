using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services;

public class FormMasterService : IFormMasterService
{
    private readonly IUnitOfWork _unitOfWork;

    public FormMasterService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<FormMasterModel>> GetAllAsync()
    {
        try
        {
            var repo = _unitOfWork.GetRepository<FormMaster>();
            var forms = await repo.FindAsync(x => !x.IsDeleted);

            var result = forms
                .OrderBy(f => f.ParentId)
                .ThenBy(f => f.DisplayIndex)
                .Select(f => new FormMasterModel
                {
                    Id = f.Id,
                    FormName = f.FormName,
                    Route = f.Route,
                    DisplayIndex = f.DisplayIndex,
                    Icon = f.Icon,
                    ParentId = f.ParentId,
                    ParentName = f.ParentName,
                    IsActive = f.IsActive
                })
                .ToList();

            if (result.Any())
            {
                return result;
            }

            return new();

        }
        catch (Exception ex)
        {

            throw;
        }

    }

    public async Task<FormMasterModel?> GetByIdAsync(int id)
    {
        var repo = _unitOfWork.GetRepository<FormMaster>();
        var form = await repo.GetByIdAsync(id);

        if (form == null || form.IsDeleted)
            return null;

        return new FormMasterModel
        {
            Id = form.Id,
            FormName = form.FormName,
            Route = form.Route,
            IsActive = form.IsActive
        };
    }

    public async Task<bool> CreateAsync(FormMasterModel model)
    {
        var repo = _unitOfWork.GetRepository<FormMaster>();

        var entity = new FormMaster
        {
            FormName = model.FormName.ToUpper(),
            Route = model.Route,
            DisplayIndex = model.DisplayIndex,
            Icon = model.Icon,
            ParentId = model.ParentId,
            ParentName = model.ParentName,
            IsActive = true,
            IsDeleted = false,
            CreatedBy = model.CreatedBy,
            CreatedOn = DateTime.UtcNow
        };
        try
        {
            await repo.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

        }
        catch (Exception ex)
        {
        }
        return true;
    }

    public async Task<bool> UpdateAsync(FormMasterModel model)
    {
        try
        {

            var repo = _unitOfWork.GetRepository<FormMaster>();
            var entity = await repo.GetByIdAsync(model.Id);
            if (entity == null) return false;

            entity.FormName = model.FormName.ToUpper();
            entity.Route = model.Route;
            entity.DisplayIndex = model.DisplayIndex;
            entity.Icon = model.Icon;
            entity.ParentId = model.ParentId;
            entity.ParentName = model.ParentName;
            entity.IsActive = model.IsActive;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedOn = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            return true;

        }
        catch (Exception ex)
        {

            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var repo = _unitOfWork.GetRepository<FormMaster>();
        var entity = await repo.GetByIdAsync(id);
        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = userId;
        entity.UpdatedOn = DateTime.UtcNow;

        await DeleteFormPermissionAsync(id, userId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task DeleteFormPermissionAsync(int formId, int userId)
    {
        var permissionRepo = _unitOfWork.GetRepository<RoleFormPermission>();
        var entities = await permissionRepo.FindAsync(p => p.FormId == formId);
        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.IsActive = false;
            entity.UpdatedBy = userId;
            entity.UpdatedOn = DateTime.UtcNow;

            permissionRepo.Update(entity);
        }
    }

    public async Task<bool> IsFormNameUniqueAsync(string name, int id = 0)
    {
        var repo = _unitOfWork.GetRepository<FormMaster>();
        var forms = await repo.FindAsync(x =>
            x.FormName.ToLower() == name.ToLower() && !x.IsDeleted);

        if (id > 0)
            forms = forms.Where(x => x.Id != id);

        return !forms.Any();
    }
    public async Task<bool> IsDisplayIndexInUseAsync(int index, int? parentid, int id = 0)
    {
        var repo = _unitOfWork.GetRepository<FormMaster>();

        var forms = await repo.FindAsync(x =>
            !x.IsDeleted &&
            x.ParentId == parentid &&
            x.DisplayIndex == index);

        if (id > 0)
            forms = forms.Where(x => x.Id != id);

        return !forms.Any();
    }


    public async Task<List<FormMasterModel>> GetParentFormsAsync()
    {
        try
        {
            var repo = _unitOfWork.GetRepository<FormMaster>();
            var forms = await repo.FindAsync(x => x.IsActive && !x.IsDeleted && x.Route == "#");

            var result = forms
                .OrderBy(f => f.DisplayIndex)
                .Select(f => new FormMasterModel
                {
                    Id = f.Id,
                    FormName = f.FormName,
                    Route = f.Route,
                    DisplayIndex = f.DisplayIndex,
                    Icon = f.Icon,
                    ParentId = f.ParentId,
                    ParentName = f.ParentName,
                    IsActive = f.IsActive
                })
                .ToList();

            if (result.Any())
            {
                return result;
            }

            return new();
        }
        catch (Exception ex)
        {

            throw;
        }
    }
}
