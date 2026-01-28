using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;
public interface IUserService
{
    Task<bool> AuthenticateAsync(string username, string password);
    Task<UserModel?> GetUserByUsernameAsync(string username);
    Task<bool> CreateUserAsync(UserModel user);
    Task<bool> UpdateUserAsync(UserModel user);
    Task<bool> DeleteUserAsync(int id,int UpdatedBy);
    Task<IEnumerable<UserModel>> GetAllUsersAsync(int userRoleId);
    Task<(bool UserNameExists, bool PhoneExists, bool EmailExists)> CheckDuplicate(int? id, string userName, string phone, string email);
    Task<List<Roles>> GetAllRolesAsync();
    Task<bool> ChangePasswordAsync(int userId, string newPassword);
    Task<Users?> GetUserByUserIdAsync(int userId);
}