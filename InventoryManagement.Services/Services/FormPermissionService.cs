using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services.Services;

public class FormPermissionService : IFormPermissionService
{
    private readonly IUnitOfWork _unitOfWork;

    public FormPermissionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<FormPermissionModel>> GetAllAsync()
    {
        var repo = _unitOfWork.GetRepository<RoleFormPermission>();
        var formRepo = _unitOfWork.GetRepository<FormMaster>();
        var roleRepo = _unitOfWork.GetRepository<Roles>();

        var permissions = (await repo.FindAsync(x => !x.IsDeleted)).ToList();
        var forms = (await formRepo.FindAsync(x => !x.IsDeleted)).ToList();
        var roles = (await roleRepo.FindAsync(x => !x.IsDeleted)).ToList();

        return permissions.Select(p => new FormPermissionModel
        {
            Id = p.Id,
            RoleId = p.RoleId,
            FormId = p.FormId,
            CanView = p.CanView,
            CanCreate = p.CanCreate,
            CanEdit = p.CanEdit,
            CanDelete = p.CanDelete,
            CreatedBy = p.CreatedBy,
            CreatedOn = p.CreatedOn,
            UpdatedBy = p.UpdatedBy,
            UpdatedOn = p.UpdatedOn,
            IsActive = p.IsActive,
            IsDeleted = p.IsDeleted,
            FormName = forms.FirstOrDefault(f => f.Id == p.FormId)?.FormName ?? string.Empty,
            RoleName = roles.FirstOrDefault(r => r.Id == p.RoleId)?.Name ?? string.Empty
        }).ToList();
    }

    public async Task<List<FormMaster>> GetAllFormsAsync()
    {
        return (await _unitOfWork.GetRepository<FormMaster>()
            .FindAsync(x => !x.IsDeleted && x.IsActive)).ToList();
    }

    public async Task<List<Roles>> GetAllRolesAsync()
    {
        return (await _unitOfWork.GetRepository<Roles>()
            .FindAsync(x => !x.IsDeleted && x.IsActive)).ToList();
    }

    public async Task<bool> ExistsAsync(int roleId, int formId, int? id = null)
    {
        var repo = _unitOfWork.GetRepository<RoleFormPermission>();
        var data = await repo.FindAsync(x =>
            x.RoleId == roleId && x.FormId == formId && !x.IsDeleted);

        if (id.HasValue)
            data = data.Where(x => x.Id != id.Value);

        return data.Any();
    }

    public async Task<bool> CreateAsync(FormPermissionModel model)
    {
        var repo = _unitOfWork.GetRepository<RoleFormPermission>();

        await repo.AddAsync(new RoleFormPermission
        {
            RoleId = model.RoleId,
            FormId = model.FormId,
            CanView = model.CanView,
            CanCreate = model.CanCreate,
            CanEdit = model.CanEdit,
            CanDelete = model.CanDelete,
            CreatedBy = model.CreatedBy,
            CreatedOn = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false,
        });

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(FormPermissionModel model)
    {
        var repo = _unitOfWork.GetRepository<RoleFormPermission>();
        var entity = await repo.GetByIdAsync(model.Id);

        if (entity == null)
            return false;

        entity.RoleId = model.RoleId;
        entity.FormId = model.FormId;
        entity.CanView = model.CanView;
        entity.CanCreate = model.CanCreate;
        entity.CanEdit = model.CanEdit;
        entity.CanDelete = model.CanDelete;
        entity.UpdatedBy = model.UpdatedBy;
        entity.UpdatedOn = DateTime.UtcNow;

        repo.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int deletedBy)
    {
        var repo = _unitOfWork.GetRepository<RoleFormPermission>();
        var entity = await repo.GetByIdAsync(id);

        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.UpdatedBy = deletedBy;
        entity.UpdatedOn = DateTime.UtcNow;

        repo.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    public async Task<bool> CheckDuplicate(int roleId, int formId, int? ignoreId = null)
    {
        var repo = _unitOfWork.GetRepository<RoleFormPermission>();
        var result = await repo.GetQueryable().AnyAsync(e => e.RoleId == roleId &&
                                                       e.FormId == formId &&
                                                       e.Id != ignoreId &&
                                                       !e.IsDeleted);

        return result;
    }
}
