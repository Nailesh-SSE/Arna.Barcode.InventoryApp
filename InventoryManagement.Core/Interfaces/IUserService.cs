using InventoryManagement.Core.Entities;

namespace InventoryManagement.Core.Interfaces;

public interface IUserService
{
    Task<bool> AuthenticateAsync(string username, string password);
    Task<Users?> GetUserByUsernameAsync(string username);
    Task<bool> CreateUserAsync(Users user);
    Task<bool> UpdateUserAsync(Users user);
    Task<bool> DeleteUserAsync(int id);
    Task<IEnumerable<Users>> GetAllUsersAsync();
}