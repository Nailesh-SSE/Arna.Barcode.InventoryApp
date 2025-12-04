using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces
{
    public interface IColourService
    {

        Task<List<ColourModel>> GetAllColourAsync();
        Task<Colour?> GetColourByIdAsync(int id);
        Task<bool> CreateColourAsync(ColourModel colourModel);
        Task<bool> UpdateColourAsync(ColourModel colourModel);
        Task<bool> DeleteColourAsync(int id,int deletedBy);
        Task<bool> IsColourCodeUnique( string code, int? id = null);
        Task<bool> IsColourNameUnique(string Name, int? id = null);

    }
}
