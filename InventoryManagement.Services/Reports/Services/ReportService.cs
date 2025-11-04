using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models.ReportModels;

namespace InventoryManagement.Services.Services
{
    public class ReportService : IReportService
    {
        private readonly IInstockReportGenerator _instockReportGenerator;
        private readonly IOutstockReportGenerator _outstockReportGenerator;
        private readonly IReturnSaleReportGenerator _returnSaleReportGenerator;

        public ReportService(
            IInstockReportGenerator instockReportGenerator,
            IOutstockReportGenerator outstockReportGenerator,
            IReturnSaleReportGenerator returnSaleReportGenerator)
        {
            _instockReportGenerator = instockReportGenerator ?? throw new ArgumentNullException(nameof(instockReportGenerator));
            _outstockReportGenerator = outstockReportGenerator ?? throw new ArgumentNullException(nameof(outstockReportGenerator));
            _returnSaleReportGenerator = returnSaleReportGenerator ?? throw new ArgumentNullException(nameof(returnSaleReportGenerator));
        }

        public async Task<InstockReportResult> GenerateInstockReportAsync(InstockFilter filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            // Validate filter using the generator
            if (!await _instockReportGenerator.ValidateFilterAsync(filter))
            {
                return new InstockReportResult
                {
                    Warnings = new List<string> { "Invalid filter criteria provided" }
                };
            }

            // Generate report using the specialized generator
            return await _instockReportGenerator.GenerateReportAsync(filter);
        }

        /// <summary>
        /// Generates an outstock report using the dedicated generator
        /// </summary>
        public async Task<OutstockReportResult> GenerateOutstockReportAsync(OutstockFilter filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            // Validate filter using the generator
            if (!await _outstockReportGenerator.ValidateFilterAsync(filter))
            {
                return new OutstockReportResult
                {
                    Warnings = new List<string> { "Invalid filter criteria provided" }
                };
            }

            // Generate report using the specialized generator
            return await _outstockReportGenerator.GenerateReportAsync(filter);
        }

        /// <summary>
        /// Generates a return sale report using the dedicated generator
        /// </summary>
        public async Task<ReturnSaleReportResult> GenerateReturnSaleReportAsync(ReturnSaleFilter filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            // Validate filter using the generator
            if (!await _returnSaleReportGenerator.ValidateFilterAsync(filter))
            {
                return new ReturnSaleReportResult
                {
                    Warnings = new List<string> { "Invalid filter criteria provided" }
                };
            }

            // Generate report using the specialized generator
            return await _returnSaleReportGenerator.GenerateReportAsync(filter);
        }
    }
}