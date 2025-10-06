using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Interfaces;

namespace InventoryManagement.Services.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        var userRepository = _unitOfWork.GetRepository<Users>();
        var user = (await userRepository.FindAsync(u => u.UserName == username && u.IsActive && !u.IsDeleted)).FirstOrDefault();
        
        if (user == null) return false;
        
        // In a real application, you would hash and verify the password
        // For now, we'll do a simple comparison (NOT SECURE - for demo only)
        return user.Password == password;
    }

    public async Task<Users?> GetUserByUsernameAsync(string username)
    {
        var userRepository = _unitOfWork.GetRepository<Users>();
        return (await userRepository.FindAsync(u => u.UserName == username && u.IsActive && !u.IsDeleted)).FirstOrDefault();
    }

    public async Task<bool> CreateUserAsync(Users user)
    {
        try
        {
            var userRepository = _unitOfWork.GetRepository<Users>();
            
            // Check if username already exists
            var existingUser = (await userRepository.FindAsync(u => u.UserName == user.UserName)).FirstOrDefault();
            if (existingUser != null)
            {
                return false;
            }

            // Hash the password (for demo, we'll just store it - NOT SECURE)
            user.Password = user.Password; // Should be hashed in production
            
            await userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateUserAsync(Users user)
    {
        try
        {
            var userRepository = _unitOfWork.GetRepository<Users>();
            userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        try
        {
            var userRepository = _unitOfWork.GetRepository<Users>();
            var user = await userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.IsDeleted = true;
            user.IsActive = false;
            userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Users>> GetAllUsersAsync()
    {
        var userRepository = _unitOfWork.GetRepository<Users>();
        return await userRepository.FindAsync(u => !u.IsDeleted);
    }
}