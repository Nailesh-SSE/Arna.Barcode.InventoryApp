using AutoMapper;
using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Mapper;

public class CategoryMappingProfile : Profile
{
    public CategoryMappingProfile()
    {
        CreateMap<Category, CategoryModel>();
    }
}