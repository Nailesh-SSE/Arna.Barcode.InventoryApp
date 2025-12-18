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
        var repo = _unitOfWork.GetRepository<FormMaster>();
        var forms = await repo.FindAsync(x => !x.IsDeleted);

        return forms.Select(f => new FormMasterModel
        {
            Id = f.Id,
            FormName = f.FormName,
            Route = f.Route,
            IsActive = f.IsActive
        }).ToList();
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
            IsActive = true,
            IsDeleted = false,
            CreatedBy = model.CreatedBy,
            CreatedOn = DateTime.UtcNow
        };

        await repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(FormMasterModel model)
    {
        var repo = _unitOfWork.GetRepository<FormMaster>();
        var entity = await repo.GetByIdAsync(model.Id);
        if (entity == null) return false;

        entity.FormName = model.FormName.ToUpper();
        entity.Route = model.Route;
        entity.IsActive = model.IsActive;
        entity.UpdatedBy = model.UpdatedBy;
        entity.UpdatedOn = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return true;
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

        await DeleteFormPermissionAsync(id,userId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task DeleteFormPermissionAsync(int formId,int userId) 
    {
        var permissionRepo = _unitOfWork.GetRepository<RoleFormPermission>();
        var entities = await permissionRepo.FindAsync(p => p.FormId == formId);
        foreach(var entity in entities) 
        {
            entity.IsDeleted = true;
            entity.IsActive = false;
            entity.UpdatedBy = userId;
            entity.UpdatedOn = DateTime.UtcNow;

            permissionRepo.Update(entity);
        }
        await _unitOfWork.SaveChangesAsync();
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
}
