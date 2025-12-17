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
        var user = (await userRepository.FindAsync(u => u.UserName == username && u.IsActive && !u.IsDeleted)).FirstOrDefault();

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

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.UserName = model.UserName;
        user.EmailId = model.Email;
        user.ContactNo = model.ContactNo;
        user.IsActive = model.IsActive;
        user.UpdatedBy = model.UpdatedBy;
        user.UpdatedOn = DateTime.UtcNow;
        user.Password = model.Password;
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

    public async Task<IEnumerable<Users>> GetAllUsersAsync()
    {
        var userRepository = _unitOfWork.GetRepository<Users>();
        return await userRepository.FindAsync(u => !u.IsDeleted);
    }

    public async Task<bool> IsUsernameUniqueAsync(string username, int? id = null)
    {
        var repo = _unitOfWork.GetRepository<Users>();
        var users = await repo.FindAsync(x =>
            x.UserName.ToLower() == username.ToLower() && !x.IsDeleted);

        if (id.HasValue)
            users = users.Where(x => x.Id != id.Value);

        return !users.Any();
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? id = null)
    {
        var repo = _unitOfWork.GetRepository<Users>();
        var users = await repo.FindAsync(x =>
            x.EmailId.ToLower() == email.ToLower() && !x.IsDeleted);
        if (id.HasValue)
            users = users.Where(x => x.Id != id.Value);
        return !users.Any();
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone, int? id = null)
    {
        var repo = _unitOfWork.GetRepository<Users>();
        var users = await repo.FindAsync(x =>
            x.ContactNo.ToLower() == phone.ToLower() && !x.IsDeleted);
        if (id.HasValue)
            users = users.Where(x => x.Id != id.Value);
        return !users.Any();
    }

    public async Task<bool> CreateUserAsync(UserModel model)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepository<Users>();
            var roleRepo = _unitOfWork.GetRepository<UsersInRole>();

            var user = new Users
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.UserName,
                EmailId = model.Email,
                ContactNo = model.ContactNo,
                IsActive = true,
                IsDeleted = false,
                CreatedBy = model.CreatedBy,
                CreatedOn = DateTime.UtcNow,
                Password = model.Password
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
}