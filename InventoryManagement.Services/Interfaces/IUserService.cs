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
    Task<IEnumerable<Users>> GetAllUsersAsync(int userRoleId);
    Task<(bool UserNameExists, bool PhoneExists, bool EmailExists)> CheckDuplicate(int? id, string userName, string phone, string email);
    Task<List<Roles>> GetAllRolesAsync();
}