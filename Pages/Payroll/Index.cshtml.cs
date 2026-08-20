using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Payroll
{
    public class IndexModel : PageModel
    {
        private readonly MockPayrollService _payrollService;

        public IndexModel(MockPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        public List<PayrollRecord> PayrollRecords { get; set; } = new();
        public int SelectedMonth { get; set; } = DateTime.UtcNow.Month;
        public int SelectedYear { get; set; } = DateTime.UtcNow.Year;

        public void OnGet(int? month, int? year)
        {
            SelectedMonth = month ?? DateTime.UtcNow.Month;
            SelectedYear = year ?? DateTime.UtcNow.Year;
            PayrollRecords = _payrollService.GetPayrollForMonth(SelectedMonth, SelectedYear);
            if (!PayrollRecords.Any())
            {
                PayrollRecords = _payrollService.GetAllPayrollRecords();
            }
        }

        public IActionResult OnPostMarkPaid(int id)
        {
            _payrollService.MarkPaid(id);
            TempData["SuccessMessage"] = "Payment status marked as Paid.";
            return RedirectToPage();
        }
    }
}
