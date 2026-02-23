using InventoryManagement.Core.Data;
using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services;
using InventoryManagement.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryManagement.Test.Services;

    [TestFixture]
    public class CompanyServiceTests
    {
        private InventoryDbContext _dbContext = null!;
        private IUnitOfWork _unitOfWork = null!;
        private CompanyService _companyService = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new InventoryDbContext(options);

            var logger = NullLogger<UnitOfWork>.Instance;
            _unitOfWork = new UnitOfWork(_dbContext, logger);
            _companyService = new CompanyService(_unitOfWork);
        }

        [Test]
        public async Task GetAllCompaniesAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var result = await _companyService.GetAllCompaniesAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
        [Test]
        public async Task GetAllCompaniesAsync_WithData_ReturnsAllCompanies()
        {
            // Arrange
            var allCompanies = await CommpanyData();
            var companies = allCompanies.Where(c => !c.IsDeleted).ToList();
            foreach (var company in allCompanies)
            {
                _dbContext.Companies.Add(new Company
                {
                    Id = company.Id,
                    Name = company.Name,
                    Code = company.Code,
                    CompanyType = company.CompanyType,
                    SerialNumber = company.SerialNumber,
                    Remark = company.Remark,
                    IsActive = company.IsActive,
                    IsDeleted = company.IsDeleted
                });
            }
            await _dbContext.SaveChangesAsync();
            // Act
            var result = await _companyService.GetAllCompaniesAsync();
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(companies.Count));
        }
    [Test]
    public async Task GetAllCompaniesAsync_ShouldMapCompanyFieldsCorrectly()
    {
        var allCompanines= await CommpanyData();
        var companines = allCompanines.Where(c => !c.IsDeleted).ToList();
        foreach (var company in companines)
        {
            _dbContext.Companies.Add(new Company
            {
                Id = company.Id,
                Name = company.Name,
                Code = company.Code,
                CompanyType = company.CompanyType,
                SerialNumber = company.SerialNumber,
                Remark = company.Remark,
                IsActive = company.IsActive,
                IsDeleted = company.IsDeleted
            });
        }
        await _dbContext.SaveChangesAsync();
        var result = await _companyService.GetAllCompaniesAsync();
        foreach(var company in companines)
        {
            var resultCompany = result.FirstOrDefault(c => c.Id == company.Id);
            Assert.That(resultCompany, Is.Not.Null);
            Assert.That(resultCompany!.Name, Is.EqualTo(company.Name));
            Assert.That(resultCompany.Code, Is.EqualTo(company.Code));
            Assert.That(resultCompany.CompanyType, Is.EqualTo(company.CompanyType));
            Assert.That(resultCompany.Remark, Is.EqualTo(company.Remark));
            Assert.That(resultCompany.IsActive, Is.EqualTo(company.IsActive));
        }
    }
        public async Task<List<CompanyModel>> CommpanyData()
        {
            var companies = new List<CompanyModel>
        {
            new CompanyModel
            {
                Id = 1,
                Name = "Test Company",
                Code = "TC001",
                CompanyType = CompanyType.MakeTo,
                SerialNumber = 100,
                Remark = "Test Remark",
                IsActive = true
            },
            new CompanyModel
            {
                Id = 2,
                Name = "Another Company",
                Code = "TC002",
                CompanyType = CompanyType.MakeTo,
                SerialNumber = 101,
                Remark = "Second Test",
                IsActive = true
            },
            new CompanyModel
            {
                Id = 3,
                Name = "Inactive Company",
                Code = "TC003",
                CompanyType = CompanyType.MakeTo,
                SerialNumber = 102,
                Remark = "Inactive Test",
                IsActive = false,
                IsDeleted = true
            }
        };

            return await Task.FromResult(companies);
        }
        [TearDown]
        public void TearDown()
        {
            _unitOfWork.Dispose();
            _dbContext.Dispose();
        }
    }
