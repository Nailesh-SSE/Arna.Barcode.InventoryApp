using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services;

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
        var user = (await userRepository.FindAsync(u => u.UserName.ToLower() == username.ToLower() && u.IsActive && !u.IsDeleted)).FirstOrDefault();

        if (user == null) return false;

        // In a real application, you would hash and verify the password
        // For now, we'll do a simple comparison (NOT SECURE - for demo only)
        return user.Password == password;
    }

    public async Task<Users?> GetUserByUsernameAsync(string username)
    {
        var userRepository = _unitOfWork.GetRepository<Users>();
        return (await userRepository.FindAsync(u => u.UserName.ToLower() == username.ToLower() && u.IsActive && !u.IsDeleted)).FirstOrDefault();
    }

    public async Task<bool> UpdateUserAsync(UserModel model)
    {
        var repo = _unitOfWork.GetRepository<Users>();
        var user = await repo.GetByIdAsync(model.Id);
        if (user == null) return false;

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.UserName = model.UserName.Trim();
        user.EmailId = model.Email.Trim();
        user.ContactNo = model.ContactNo.Trim();
        user.IsActive = model.IsActive;
        user.UpdatedBy = model.UpdatedBy;
        user.UpdatedOn = DateTime.UtcNow;
        user.Password = model.Password.Trim();
        user.RoleId = model.RoleId;
        await _unitOfWork.SaveChangesAsync();
        return true;
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
            userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Users>> GetAllUsersAsync(int userRoleId)
    {
        var roleRepository = _unitOfWork.GetRepository<Roles>();
        var userRepository = _unitOfWork.GetRepository<Users>();

        var roles = (await roleRepository.FindAsync(r =>
                        !r.IsDeleted && r.IsActive))
                        .ToList();

        var users = (await userRepository.FindAsync(u =>
                        !u.IsDeleted && u.IsActive))
                        .ToList();

        var currentRoleLevel = roles
            .FirstOrDefault(r => r.Id == userRoleId)?
            .RoleLevel ?? int.MaxValue;

        if (currentRoleLevel >= 1 && currentRoleLevel <= 4)
        {
            return users;
        }

        var allowedRoleIds = roles
            .Where(r => r.RoleLevel >= 5)
            .Select(r => r.Id)
            .ToHashSet();

        return users
            .Where(u => allowedRoleIds.Contains(u.RoleId))
            .ToList();
    }


    public async Task<(bool UserNameExists, bool PhoneExists, bool EmailExists)> CheckDuplicate(int? id,string userName,string phone,string email) 
    {
        var repo = _unitOfWork.GetRepository<Users>();
        var users = await repo.FindAsync(x =>
               !x.IsDeleted &&
               (
                   x.ContactNo.ToLower() == phone.ToLower() ||
                   x.EmailId.ToLower() == email.ToLower() ||
                   x.UserName.ToLower() == userName.ToLower()
               )
               && (!id.HasValue || x.Id != id.Value)
         );
        bool userNameExists = users.Any(x => x.UserName.ToLower() == userName.ToLower());
        bool phoneExists = users.Any(x => x.ContactNo.ToLower() == phone.ToLower());
        bool emailExists = users.Any(x => x.EmailId.ToLower() == email.ToLower());


        return (userNameExists, phoneExists, emailExists);
    }
    public async Task<bool> CreateUserAsync(UserModel model)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepository<Users>();
            var roleRepo = _unitOfWork.GetRepository<UsersInRole>();

            var user = new Users
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                UserName = model.UserName.Trim(),
                EmailId = model.Email.Trim(),
                ContactNo = model.ContactNo.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedBy = model.CreatedBy,
                CreatedOn = DateTime.UtcNow,
                Password = model.Password.Trim(),
                RoleId=model.RoleId
            };

            await userRepo.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            await roleRepo.AddAsync(new UsersInRole
            {
                UserId = user.Id,
                RoleId = model.RoleId,
                CompanyId = 1,
                CreatedBy = model.CreatedBy,
                CreatedOn = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
            return true;

        }
        catch (Exception ex)
        {

            throw;
        }

    }
    public async Task<List<Roles>> GetAllRolesAsync()
    {
        var roleRepository = _unitOfWork.GetRepository<Roles>();
        return (await roleRepository.GetAllAsync()).ToList();
    }
    public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
    {
        try
        {
            var userRepository = _unitOfWork.GetRepository<Users>();
            var user = await userRepository.GetByIdAsync(userId);

            if (user == null)
                return false;

            user.Password = newPassword;
            user.UpdatedOn = DateTime.UtcNow;

            userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    public async Task<Users?> GetUserByUserIdAsync(int userId)
    {
        var userRepository = _unitOfWork.GetRepository<Users>();
        return await userRepository.GetByIdAsync(userId);
    }
}