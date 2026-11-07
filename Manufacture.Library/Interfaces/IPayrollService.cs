using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface IPayrollService
    {
        Task<List<SalaryAdvanceListDto>> GetAdvancesAsync(CancellationToken cancellationToken = default);

        Task<List<SalaryAdvanceListDto>> GetAdvancesForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);

        Task<SalaryAdvanceListDto?> GetAdvanceByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<SalaryAdvanceListDto> CreateAdvanceAsync(SalaryAdvanceDto dto, string createdBy, CancellationToken cancellationToken = default);

        Task<bool> UpdateAdvanceAsync(SalaryAdvanceDto dto, string updatedBy, CancellationToken cancellationToken = default);

        Task<bool> DeleteAdvanceAsync(int id, CancellationToken cancellationToken = default);

        Task<List<PayrollDeductionLineDto>> GetAdvanceRepaymentsAsync(int advanceId, CancellationToken cancellationToken = default);

        Task<List<PayrollRunDto>> GetPayrollRunsAsync(DateOnly? generatedDate = null, CancellationToken cancellationToken = default);

        Task<PayrollRunDetailsDto?> GetPayrollRunAsync(int id, CancellationToken cancellationToken = default);

        Task<List<PayrollRecordDto>> GetPayrollRecordsAsync(int month, int year, CancellationToken cancellationToken = default);

        Task<List<PayrollRecordDto>> GetAllPayrollRecordsAsync(DateOnly? generatedDate = null, CancellationToken cancellationToken = default);

        Task<PayrollRecordDto?> GetPayrollRecordAsync(int employeeId, int month, int year, CancellationToken cancellationToken = default);

        Task<PayrollRecordDto?> GetPayrollRecordByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<PayrollRunEmployeeDto>> GetRunEmployeesAsync(int month, int year, CancellationToken cancellationToken = default);

        Task<decimal> GetEstimatedNetPayoutAsync(int month, int year, CancellationToken cancellationToken = default);

        Task ProcessMonthlyPayrollAsync(
            int month,
            int year,
            IReadOnlyList<PayrollEmployeeAdjustmentDto> adjustments,
            string processedBy,
            CancellationToken cancellationToken = default);

        Task<bool> MarkPaidAsync(int payrollId, string updatedBy, CancellationToken cancellationToken = default);

        Task<bool> MarkRunPaidAsync(int payrollRunId, string updatedBy, CancellationToken cancellationToken = default);

        Task<List<PayrollDeductionLineDto>> GetDeductionLinesAsync(int payrollRecordId, CancellationToken cancellationToken = default);
    }
}
