using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models.ReportModels;
using System.Linq.Expressions;

namespace InventoryManagement.Services.Filters;

public static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
        var left = leftVisitor.Visit(expr1.Body);

        var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
        var right = rightVisitor.Visit(expr2.Body);

        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(left, right), parameter);
    }

    private class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override Expression Visit(Expression node)
        {
            return node == _oldValue ? _newValue : base.Visit(node);
        }
    }
}

public class InwardReportFilter : IReportFilter<Inward>
{
    private readonly InstockFilter _filter;

    public InwardReportFilter(InstockFilter filter)
    {
        _filter = filter;
    }

    public Expression<Func<Inward, bool>> GetFilterExpression()
    {
        Expression<Func<Inward, bool>> expression = i => true;

        if (_filter.StartDate.HasValue)
            expression = expression.And(i => i.InwardDate.Date >= _filter.StartDate.Value.Date);

        if (_filter.EndDate.HasValue)
            expression = expression.And(i => i.InwardDate.Date <= _filter.EndDate.Value.Date);

        if (_filter.CategoryId.HasValue)
            expression = expression.And(i => i.CategoryId == _filter.CategoryId.Value);

        if (_filter.CompanyId.HasValue)
            expression = expression.And(i => i.ShipMentCompanyId == _filter.CompanyId.Value);

        return expression;
    }

    public bool IsValid()
    {
        if (_filter.StartDate.HasValue && _filter.EndDate.HasValue)
            return _filter.StartDate.Value <= _filter.EndDate.Value;

        return true;
    }
}

public class OutwardReportFilter : IReportFilter<Outward>
{
    private readonly OutstockFilter _filter;

    public OutwardReportFilter(OutstockFilter filter)
    {
        _filter = filter;
    }

    public Expression<Func<Outward, bool>> GetFilterExpression()
    {
        Expression<Func<Outward, bool>> expression = o => true;

        if (_filter.StartDate.HasValue)
            expression = expression.And(o => o.OutwardDate >= _filter.StartDate.Value);

        if (_filter.EndDate.HasValue)
            expression = expression.And(o => o.OutwardDate <= _filter.EndDate.Value);

        if (_filter.CompanyId.HasValue)
            expression = expression.And(o => o.BillToCompanyId == _filter.CompanyId.Value);

        if (!string.IsNullOrEmpty(_filter.OutwardNumbers))
        {
            var numbers = _filter.OutwardNumbers.Split(',').Select(n => n.Trim()).ToList();
            expression = expression.And(o => numbers.Contains(o.OutwardNo));
        }

        return expression;
    }

    public bool IsValid()
    {
        if (_filter.StartDate.HasValue && _filter.EndDate.HasValue)
            return _filter.StartDate.Value <= _filter.EndDate.Value;

        return true;
    }
}

public class SaleReturnReportFilter : IReportFilter<SaleReturn>
{
    private readonly ReturnSaleFilter _filter;

    public SaleReturnReportFilter(ReturnSaleFilter filter)
    {
        _filter = filter;
    }

    public Expression<Func<SaleReturn, bool>> GetFilterExpression()
    {
        Expression<Func<SaleReturn, bool>> expression = sr => true;

        if (_filter.StartDate.HasValue)
            expression = expression.And(sr => sr.ReturnDate >= _filter.StartDate.Value);

        if (_filter.EndDate.HasValue)
            expression = expression.And(sr => sr.ReturnDate <= _filter.EndDate.Value);

        if (_filter.CompanyId.HasValue)
            expression = expression.And(sr => sr.BillToCompanyId == _filter.CompanyId.Value);

        if (!string.IsNullOrEmpty(_filter.ReturnType))
        {
            if (Enum.TryParse<Core.Enums.ReturnType>(_filter.ReturnType, out var returnType))
                expression = expression.And(sr => sr.ReturnType == returnType);
        }

        if (!string.IsNullOrEmpty(_filter.BarcodeNumbers))
        {
            var barcodes = _filter.BarcodeNumbers.Split(',').Select(b => b.Trim()).ToList();
            expression = expression.And(sr => barcodes.Contains(sr.BarcodeNo));
        }

        if (!string.IsNullOrEmpty(_filter.ReturnNumbers))
        {
            var returnNumbers = _filter.ReturnNumbers.Split(',').Select(r => r.Trim()).ToList();
            expression = expression.And(sr => returnNumbers.Contains(sr.ReturnNo));
        }

        return expression;
    }

    public bool IsValid()
    {
        if (_filter.StartDate.HasValue && _filter.EndDate.HasValue)
            return _filter.StartDate.Value <= _filter.EndDate.Value;

        return true;
    }
}
