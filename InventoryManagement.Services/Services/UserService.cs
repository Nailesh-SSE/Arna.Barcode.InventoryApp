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

    public async Task<UserModel?> GetUserByUsernameAsync(string username)
    {
        var userRepository = _unitOfWork.GetRepository<Users>();
        var userInRoleRepo = _unitOfWork.GetRepository<UsersInRole>();
        var entity = (await userRepository.FindAsync(u => u.UserName.ToLower() == username.ToLower() && u.IsActive && !u.IsDeleted)).FirstOrDefault();
        if (entity == null) return null;
     
        var userRole = (await userInRoleRepo.FindAsync(ur => ur.UserId == entity.Id)).FirstOrDefault();
        if (userRole == null) return null;

        var model = new UserModel()
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            UserName = entity.UserName,
            Email = entity.EmailId,
            ContactNo = entity.ContactNo,
            IsActive = entity.IsActive,
            RoleId = userRole.RoleId
        };
        return model;
    }

    public async Task<bool> UpdateUserAsync(UserModel model)
    {
        var repo = _unitOfWork.GetRepository<Users>();
        var roleRepo = _unitOfWork.GetRepository<UsersInRole>();

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
        await _unitOfWork.SaveChangesAsync();

        var existingUserRole = roleRepo.GetQueryable()
        .FirstOrDefault(u => u.UserId == user.Id);

        if (existingUserRole != null)
        {
            // update the mapping to the requested role
            existingUserRole.RoleId = model.RoleId;
            existingUserRole.UpdatedBy = model.UpdatedBy;
            existingUserRole.UpdatedOn = DateTime.UtcNow;
            roleRepo.Update(existingUserRole);
        }
        else
        {
            // create a new mapping
            var newUserRole = new UsersInRole
            {
                UserId = user.Id,
                RoleId = model.RoleId,
                CreatedBy = model.UpdatedBy,
                CreatedOn = DateTime.UtcNow
            };
            await roleRepo.AddAsync(newUserRole);
        }
        await _unitOfWork.SaveChangesAsync();
        return true;
    }


    public async Task<bool> DeleteUserAsync(int id, int updatedBy)
    {
        try
        {
            var userRepository = _unitOfWork.GetRepository<Users>();
            var userInRoleRepo = _unitOfWork.GetRepository<UsersInRole>();
            var user = await userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.IsDeleted = true;
            user.IsActive = false;
            user.UpdatedOn= DateTime.UtcNow;
            user.UpdatedBy = updatedBy;
            userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
          
            var userRoles = userInRoleRepo
                .GetQueryable().Where(ur => ur.UserId == id).ToList();
         
            foreach (var ur in userRoles) 
            {
                ur.IsDeleted = true;
                ur.IsActive = false;
                ur.UpdatedOn = DateTime.UtcNow;
                ur.UpdatedBy = updatedBy;
                userInRoleRepo.Update(ur);

            }
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<UserModel>> GetAllUsersAsync(int userRoleId)
    {
        var roleRepository = _unitOfWork.GetRepository<Roles>();
        var userRepository = _unitOfWork.GetRepository<Users>();
        var userInRoleRepo = _unitOfWork.GetRepository<UsersInRole>();

        var roles = (await roleRepository.FindAsync(r =>
                        !r.IsDeleted))
                        .ToList();

        var users = (await userRepository.FindAsync(u =>
                        !u.IsDeleted)).ToList();

        var userRoles = (await userInRoleRepo.FindAsync(_ => true)).ToList();

        var currentRoleLevel = roles
            .FirstOrDefault(r => r.Id == userRoleId)?
            .RoleLevel ?? int.MaxValue;
      
        var allowedRoleIds = roles
                         .Where(r => r.RoleLevel >= 5)
                         .Select(r => r.Id)
                         .ToHashSet();
       
        if (currentRoleLevel <= 0)
        {
            return Enumerable.Empty<UserModel>();
        }
        if (currentRoleLevel >= 5 )
        {
            users = users
                .Where(u => userRoles
                    .Any(ur => ur.UserId == u.Id && allowedRoleIds.Contains(ur.RoleId)))
                .ToList();
        }
        var result = users.Select(u => new UserModel
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            UserName = u.UserName,
            Email = u.EmailId,
            ContactNo = u.ContactNo,
            IsActive = u.IsActive,
            RoleId = userRoles
             .Where(ur => ur.UserId == u.Id)
             .Select(ur => ur.RoleId)
             .FirstOrDefault()
        }).ToList();
        return result;
    }


    public async Task<(bool UserNameExists, bool PhoneExists, bool EmailExists)> CheckDuplicate(int? id, string userName, string phone, string email)
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