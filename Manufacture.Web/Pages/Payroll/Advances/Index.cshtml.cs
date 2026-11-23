using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Payroll.Advances
{
    public class IndexModel : PageModel
    {
        private readonly IPayrollService _payrollService;
        private readonly ISettingsService _settingsService;

        public IndexModel(IPayrollService payrollService, ISettingsService settingsService)
        {
            _payrollService = payrollService;
            _settingsService = settingsService;
        }

        public List<SalaryAdvanceListDto> Advances { get; set; } = new();
        public PayrollSettingsDto Settings { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? Date { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public async Task OnGetAsync()
        {
            Settings = await _settingsService.GetPayrollSettingsAsync();
            var advances = await _payrollService.GetAdvancesAsync();

            if (!string.IsNullOrWhiteSpace(Status))
            {
                advances = advances
                    .Where(a => string.Equals(a.Status, Status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (Date.HasValue)
            {
                advances = advances.Where(a => a.RequestDate == Date.Value).ToList();
            }

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                advances = advances
                    .Where(a =>
                        a.StaffCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        a.EmployeeName.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            Advances = advances.OrderByDescending(a => a.RequestDate).ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var deleted = await _payrollService.DeleteAdvanceAsync(id);
                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
                    ? "Salary advance deleted successfully."
                    : "Salary advance not found.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage(new { status = Status, date = Date, search = Search });
        }
    }
}
