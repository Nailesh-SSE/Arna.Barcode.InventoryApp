using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Services;

public class ColourService: IColourService
{
    private readonly IUnitOfWork _unitOfWork;
    public ColourService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Colour> GetColourByIdAsync(int id)
    {
        var colourRepository = _unitOfWork.GetRepository<Colour>();
        return await colourRepository.GetByIdAsync(id);
    }
    public async Task<List<ColourModel>> GetAllColourAsync()
    {
        var colourRepository = _unitOfWork.GetRepository<Colour>();
        var colours = await colourRepository.FindAsync(c => !c.IsDeleted);
        var model = colours.Select(c => new ColourModel
        {
            Id = c.Id,
            Name = c.Name,
            Code = c.Code,
            IsActive = c.IsActive,
            
        }).ToList();
        return model;
    }
    public async Task<bool> CreateColourAsync(ColourModel colourModel)
    {
        var colourRepository = _unitOfWork.GetRepository<Colour>();
        var colour = new Colour
        {
            Id = colourModel.Id,
            Name = colourModel.Name,
            Code = colourModel.Code,
            IsActive = true,
            IsDeleted = false,
            CreatedBy=colourModel.CreatedBy,
            CreatedOn = DateTime.Now
        };
        await colourRepository.AddAsync(colour);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdateColourAsync(ColourModel colourModel)
    {
        var colourRepository = _unitOfWork.GetRepository<Colour>();
        var colour = await colourRepository.GetByIdAsync(colourModel.Id);

        colour.Id = colourModel.Id;
        colour.Name = colourModel.Name;
        colour.Code = colourModel.Code;
        colour.IsActive = colourModel.IsActive;
        colour.IsDeleted = colourModel.IsDeleted;
        colour.UpdatedBy = colourModel.UpdatedBy;
        colour.UpdatedOn = DateTime.Now;

        colourRepository.Update(colour);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteColourAsync(int id, int deletedBy)
    {
        try
        {

            var colourRepository = _unitOfWork.GetRepository<Colour>();
            var colour = await colourRepository.GetByIdAsync(id);
            if (colour == null) return false;
            colour.IsDeleted = true;
            colour.IsActive = false;
            colour.UpdatedOn = DateTime.Now;
            colour.UpdatedBy = deletedBy;
            colourRepository.Update(colour);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<bool> IsColourNameUniqueAsync(string name, int? id = null)
    {
        var colourRepository = _unitOfWork.GetRepository<Colour>();
        var colours = await colourRepository.FindAsync(c => c.Name.ToLower() == name.Trim().ToLower() && !c.IsDeleted);
        if (id.HasValue)
        {
            return !colours.Any(c => c.Id != id.Value);
        }
        return !colours.Any();
    }
    public async Task<bool> IsColourCodeUniqueAsync(string code, int? id = null)
    {
        var colourRepository = _unitOfWork.GetRepository<Colour>();
        var colours = await colourRepository.FindAsync(c => c.Code.ToLower() == code.Trim().ToLower() && !c.IsDeleted);
        if (id.HasValue)
        {
            return !colours.Any(c => c.Id != id.Value);
        }
        return !colours.Any();
    }
    public async Task<bool> IsColourCodeUnique( string code, int? id = null)
    {
        var colourRepository = _unitOfWork.GetRepository<Colour>();

        var colours = await colourRepository.FindAsync(c =>  c.Code.ToLower() == code.Trim().ToLower() && !c.IsDeleted);
        if (id.HasValue)
        {
            colours = colours.Where(c => c.Id != id.Value); 
        }
        return !colours.Any();
    }

    public async Task<bool> IsColourNameUnique(string Name, int? id = null)
    {
        var colourRepository = _unitOfWork.GetRepository<Colour>();

        var colours = await colourRepository.FindAsync(c => c.Name.ToLower() == Name.Trim().ToLower() && !c.IsDeleted);
        if (id.HasValue)
        {
            colours = colours.Where(c => c.Id != id.Value);
        }
        return !colours.Any();
    }
}