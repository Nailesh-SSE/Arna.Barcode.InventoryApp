using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Mapper;
public static class FormMasterMapper
{
    public static FormMasterModel ToModel(this FormMaster entity)
    {
        return new FormMasterModel
        {
            Id = entity.Id,
            FormName = entity.FormName,
            Route = entity.Route,
            DisplayIndex = entity.DisplayIndex,
            Icon = entity.Icon ?? string.Empty,
            ParentId = entity.ParentId,
            ParentName = entity.ParentName,
            IsActive = entity.IsActive,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy
        };
    }

    public static List<FormMasterModel> ToModelList(
        this IEnumerable<FormMaster> entities)
    {
        return entities
            .Select(e => e.ToModel())
            .OrderBy(e => e.DisplayIndex)
            .ToList();
    }
}
