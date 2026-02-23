using InventoryManagement.Core.Data;
using InventoryManagement.Services;
using InventoryManagement.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Entities;

namespace InventoryManagement.Test.Services;

[TestFixture]   
public class CategoryServiceTests
{
    private InventoryDbContext _dbcontext = null!;
    private ILogger<UnitOfWork> _logger = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CategoryService _categoryService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new  DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName:Guid.NewGuid().ToString())
            .Options;
        _dbcontext = new InventoryDbContext(options);
        _logger = new LoggerFactory().CreateLogger<UnitOfWork>();
        _unitOfWork = new UnitOfWork(_dbcontext, _logger);
        _categoryService = new CategoryService(_unitOfWork);
    }
    
    [Test]
    public async Task GetAllCategoriesAsync_ShouldReturnAllCategories()
    {
        // Arrange
        var category1 = new Category { Id = 1, Name = "Category 1", Description="", SerialNumber=1, IsActive=true , IsDeleted = false, CreatedBy=1 ,CreatedOn= DateTime.UtcNow, UpdatedBy=0 };
        var category2 = new Category { Id = 1, Name = "Category 2", Description="", SerialNumber=1, IsActive=true , IsDeleted = false, CreatedBy=1 ,CreatedOn= DateTime.UtcNow, UpdatedBy=0 };

        _dbcontext.Categories.AddRange(category1, category2);
        await _dbcontext.SaveChangesAsync();
        // Act
        var result = await _categoryService.GetAllCategoriesAsync();
        // Assert
        Assert.That(result.Count , Is.EqualTo(2));
        Assert.That(result.Any(c => c.Name == "Category 1"));
        Assert.That(result.Any(c => c.Name == "Category 2"));
    }
    
    
    [TearDown]
    public void TearDown()
    {
        _dbcontext.Dispose();
        _unitOfWork.Dispose();
    }

}
