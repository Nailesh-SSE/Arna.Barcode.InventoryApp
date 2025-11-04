using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagement.Services.Interfaces
{
    public interface IColourService
    {

        Task<List<ColourModel>> GetAllColourAsync();
        Task<Colour?> GetColourByIdAsync(int id);
        Task<bool> CreateColourAsync(ColourModel colourModel);
        Task<bool> UpdateColourAsync(ColourModel colourModel);
        Task<bool> DeleteColourAsync(int id,int deletedBy);
        Task<bool> IsColourAndNameUniqueAsync(string name, string code, int? id = null);

    }
}
