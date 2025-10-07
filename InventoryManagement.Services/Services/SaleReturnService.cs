using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;

namespace InventoryManagement.Services;

public class SaleReturnService : ISaleReturnService
{
    private readonly IUnitOfWork _unitOfWork;

    public SaleReturnService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SaleReturn>> GetAllSaleReturnsAsync()
    {
        var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
        return await saleReturnRepository.FindAsync(sr => !sr.IsDeleted);
    }

    public async Task<SaleReturn?> GetSaleReturnByIdAsync(int id)
    {
        var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
        return await saleReturnRepository.GetByIdAsync(id);
    }

    public async Task<bool> CreateSaleReturnAsync(SaleReturn saleReturn)
    {
        try
        {
            var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
            await saleReturnRepository.AddAsync(saleReturn);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateSaleReturnAsync(SaleReturn saleReturn)
    {
        try
        {
            var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
            saleReturnRepository.UpdateAsync(saleReturn);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteSaleReturnAsync(int id)
    {
        try
        {
            var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
            var saleReturn = await saleReturnRepository.GetByIdAsync(id);
            if (saleReturn == null) return false;

            saleReturn.IsDeleted = true;
            saleReturn.IsActive = false;
            saleReturnRepository.UpdateAsync(saleReturn);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<SaleReturn>> GetSaleReturnsByCompanyAsync(int companyId)
    {
        var saleReturnRepository = _unitOfWork.GetRepository<SaleReturn>();
        return await saleReturnRepository.FindAsync(sr => sr.BillToCompanyId == companyId && !sr.IsDeleted);
    }
}