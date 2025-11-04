
namespace InventoryManagement.Services.Interfaces
{
    public interface IReportGenerator<TFilter, TResult>
    {
        Task<TResult> GenerateReportAsync(TFilter filter);
        Task<bool> ValidateFilterAsync(TFilter filter);
    }
}