using InventoryManagement.Services.Models.ReportModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagement.Services.Reports.Interfaces
{
    public interface IInStockReportService
    {
        Task<InStockReportResult> GenerateInStockReportAsync(InStockFilter filter);
        Task<byte[]> ExportToExcelAsync(InStockFilter filter);
    }
}
