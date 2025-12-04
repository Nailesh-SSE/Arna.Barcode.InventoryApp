using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IPlatformService
{
    Task<List<PlatformModel>> GetAllPlatformAsync();
    Task<bool> CreatePlatformAsync(PlatformModel platformModel);
    Task<bool> UpdatePlatformAsync(PlatformModel platformModel);
    Task<bool> DeletePlatformAsync(int id, int userid);
    Task<bool> IsPlatformNameUniqueAsync(string name, int? excludeId = null);
}
