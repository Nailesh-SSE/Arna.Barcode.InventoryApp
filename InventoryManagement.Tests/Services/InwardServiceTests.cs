using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services;
using InventoryManagement.Services.Models;
using MockQueryable;
using Moq;
using System.Linq.Expressions;

namespace InventoryManagement.Tests.Services;

public class InwardServiceTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<IRepository<Inward>> _inwardRepoMock;
    private Mock<IRepository<InwardItem>> _inwardItemRepoMock;
    private InwardService _target;

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _inwardRepoMock = new Mock<IRepository<Inward>>();
        _inwardItemRepoMock = new Mock<IRepository<InwardItem>>();

        _unitOfWorkMock
            .Setup(u => u.GetRepository<Inward>())
            .Returns(_inwardRepoMock.Object);

        _unitOfWorkMock
            .Setup(u => u.GetRepository<InwardItem>())
            .Returns(_inwardItemRepoMock.Object);

        _target = new InwardService(_unitOfWorkMock.Object);
    }

    #region [GetAllAsync]

    [Test]
    public async Task GetAllAsync_ReturnsData()
    {
        // Arrange
        var expectedResult = new List<Inward>
        {
            new Inward
            {
                Id = 1,
                InwardNo = "IN001"
            }
        }; 

        var mock = expectedResult.BuildMock().AsQueryable();

        _inwardRepoMock
            .Setup(r => r.GetQueryable())
            .Returns(mock);

        // Act
        var result = await _target.GetAllAsync();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
    }

    #endregion

    #region [GetByIdAsync]

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsInward()
    {
        // Arrange
        var expectedResult = new Inward
        {
            Id = 1,
            InwardNo = "IN001",
            InwardItems = new List<InwardItem>
            {
                new InwardItem
                {
                    Id = 1,
                    InwardId = 1
                }
            }
        };

        var mock = expectedResult.InwardItems.BuildMock().AsQueryable();

        _inwardRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<Expression<Func<Inward, object>>[]>()))
            .ReturnsAsync(expectedResult);

        _inwardItemRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<InwardItem, bool>>>()))
            .ReturnsAsync(mock);

        // Act
        var result = await _target.GetByIdAsync(1);

        // Assert
        Assert.That(result?.Id, Is.EqualTo(1));
    }

    [Test]
    public async Task GetByIdAsync_DoesNotExistingId_ReturnsNull()
    {
        // Arrange 
        var expectedResult = new Inward(); 

        var mock = expectedResult.InwardItems.BuildMock().AsQueryable();

        _inwardRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<Expression<Func<Inward, object>>[]>()))
            .ReturnsAsync(expectedResult);

        _inwardItemRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<InwardItem, bool>>>()))
            .ReturnsAsync(mock);

        // Act
        var result = await _target.GetByIdAsync(0);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region [CreateInwardAsync]

    [Test]
    public async Task CreateInwardAsync_ValidInward_ReturnsTrue()
    {
        // Arrange
        var inwardModel = new InwardModel
        {
            InwardNo = "IN002",
            InwardDate = DateTime.UtcNow
        };

        var entityInward = new Inward
        {
            InwardNo = inwardModel.InwardNo,
            InwardDate = inwardModel.InwardDate
        };

        _unitOfWorkMock
            .Setup(uow => uow.BeginTransactionAsync())
            .Returns(Task.CompletedTask);

        _inwardRepoMock
            .Setup(r => r.AddAsync(entityInward))
            .ReturnsAsync(entityInward);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _target.CreateAsync(inwardModel);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CreateInwardAsync_InvalidInward_ReturnsFalse()
    {
        // Arrange
        InwardModel? inwardModel = null;

        // Act
        var result = await _target.CreateAsync(inwardModel!);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CreateInwardAsync_ExceptionThrown_ReturnsFalse()
    {
        // Arrange
        var inwardModel = new InwardModel();

        _inwardRepoMock
             .Setup(r => r.AddAsync(It.IsAny<Inward>()))
            .ThrowsAsync(new Exception("Error creating inward"));

        // Act
        var result = await _target.CreateAsync(inwardModel);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region [UpdateInwardAsync]

    [Test]
    public async Task UpdateInwardAsync_ValidInward_ReturnsTrue()
    {
        // Arrange
        var inwardModel = new InwardModel
        {
            Id = 1,
            InwardNo = "IN001"
        };

        var entityInward = new Inward
        {
            Id = 1,
            InwardNo = "IN002"
        };

        _unitOfWorkMock
            .Setup(uow => uow.BeginTransactionAsync())
            .Returns(Task.CompletedTask);

        _inwardRepoMock
            .Setup(repo => repo.GetByIdAsync(inwardModel.Id))
            .ReturnsAsync(entityInward);

        _inwardRepoMock
            .Setup(repo => repo.Update(It.IsAny<Inward>()));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(entityInward.Id);

        _unitOfWorkMock
            .Setup(uow => uow.CommitTransactionAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _target.UpdateAsync(inwardModel);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UpdateInwardAsync_DoesNotExistingInwardId_ReturnsFalse()
    {
        // Arrange
        var inwardModel = new InwardModel(); 
        var entityInward = new Inward();

        _unitOfWorkMock
            .Setup(uow => uow.BeginTransactionAsync())
            .Returns(Task.CompletedTask);

        _inwardRepoMock
            .Setup(repo => repo.GetByIdAsync(1, It.IsAny<Expression<Func<Inward, object>>[]>()))
            .ReturnsAsync(entityInward);

        // Act
        var result = await _target.UpdateAsync(inwardModel);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UpdateInwardAsync_InvalidInward_ReturnsFalse()
    {
        // Arrange
        InwardModel? inwardModel = null;   

        // Act
        var result = await _target.UpdateAsync(inwardModel!);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UpdateInwardAsync_ExceptionThrown_ReturnsFalse()
    {
        // Arrange
        var inwardModel = new InwardModel();

        _inwardRepoMock
            .Setup(repo => repo.Update(It.IsAny<Inward>()));

        // Act
        var result = await _target.UpdateAsync(inwardModel);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region [DeleteInwardAsync]

    [Test]
    public async Task DeleteInwardAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        int inwardId = 1;
        int deletedBy = 2;

        _inwardRepoMock
            .Setup(repo => repo.GetByIdAsync(inwardId))
            .ReturnsAsync(new Inward());

        _inwardRepoMock
            .Setup(repo => repo.Update(It.IsAny<Inward>()));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(inwardId);

        // Act
        var result = await _target.DeleteAsync(inwardId, deletedBy);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeleteInwardAsync_DoesNotExistingInwardId_ReturnsFalse()
    {
        // Arrange
        int inwardId = 2;
        int deletedBy = 2; 

        _inwardRepoMock
            .Setup(repo => repo.GetByIdAsync(1, It.IsAny<Expression<Func<Inward, object>>[]>()))
            .ReturnsAsync(new Inward());

        // Act
        var result = await _target.DeleteAsync(inwardId, deletedBy);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteInwardAsync_ExceptionThrown_ReturnsFalse()
    {
        // Arrange
        int inwardId = 1;
        int deletedBy = 2;

        _inwardRepoMock
            .Setup(repo => repo.Update(It.IsAny<Inward>()));

        // Act
        var result = await _target.DeleteAsync(inwardId, deletedBy);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region [GetInwardItemsByInwardIdAsync]

    [Test]
    public async Task GetInwardItemsByInwardIdAsync_ReturnsData()
    {
        // Arrange
        int inwardId = 1;
        var expectedResult = new List<InwardItem>
        {
            new InwardItem
            {
                Id = 1,
                InwardId = inwardId
            }
        };

        var mock = expectedResult.BuildMock().AsQueryable();

        _inwardItemRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<InwardItem, bool>>>()))
            .ReturnsAsync(mock);

        // Act
        var result = await _target.GetInwardItemsByInwardIdAsync(inwardId);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
    }

    #endregion


}
