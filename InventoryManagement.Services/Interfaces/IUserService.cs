using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;
public interface IUserService
{
    Task<bool> AuthenticateAsync(string username, string password);
    Task<Users?> GetUserByUsernameAsync(string username);
    Task<bool> CreateUserAsync(UserModel user);
    Task<bool> UpdateUserAsync(UserModel user);
    Task<bool> DeleteUserAsync(int id);
    Task<IEnumerable<Users>> GetAllUsersAsync();

    Task<bool> IsUsernameUniqueAsync(string username, int? id = null);
    Task<bool> IsEmailUniqueAsync(string email, int? id = null);
    Task<bool> IsPhoneUniqueAsync(string phone, int? id = null);

    Task<List<Roles>> GetAllRolesAsync();
}