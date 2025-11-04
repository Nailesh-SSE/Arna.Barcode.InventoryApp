using System.Linq.Expressions;

namespace InventoryManagement.Services.Interfaces;
public interface IReportFilter<T>
{
    /// <summary>
    /// Gets the filter expression
    /// </summary>
    Expression<Func<T, bool>> GetFilterExpression();
    
    /// <summary>
    /// Validates the filter criteria
    /// </summary>
    bool IsValid();
}