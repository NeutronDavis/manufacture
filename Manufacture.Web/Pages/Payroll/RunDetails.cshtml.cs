using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Payroll
{
    public class RunDetailsModel : PageModel
    {
        private readonly IPayrollService _payrollService;

        public RunDetailsModel(IPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        public PayrollRunDetailsDto Details { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var details = await _payrollService.GetPayrollRunAsync(id);
            if (details == null)
            {
                TempData["ErrorMessage"] = "Payroll run not found.";
                return RedirectToPage("/Payroll/Index");
            }

            Details = details;
            return Page();
        }

        public async Task<IActionResult> OnPostApproveRunAsync(int id)
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "Administrator";
            var updated = await _payrollService.MarkRunPaidAsync(id, userName);

            TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                ? "Payroll run approved. All employee payments are now marked as Paid."
                : "Payroll run not found or contains no employee records.";

            return RedirectToPage("/Payroll/RunDetails", new { id });
        }
    }
}
