using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Payroll.Advances
{
    public class EditModel : PageModel
    {
        private readonly IPayrollService _payrollService;

        public EditModel(IPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        [BindProperty]
        public SalaryAdvanceDto AdvanceInput { get; set; } = new();

        public SalaryAdvanceListDto Advance { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var advance = await _payrollService.GetAdvanceByIdAsync(id);
            if (advance == null)
            {
                TempData["ErrorMessage"] = "Salary advance not found.";
                return RedirectToPage("/Payroll/Advances/Index");
            }

            Advance = advance;
            AdvanceInput = new SalaryAdvanceDto
            {
                Id = advance.Id,
                EmployeeId = advance.EmployeeId,
                PrincipalAmount = advance.PrincipalAmount,
                Reason = advance.Reason
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var existing = await _payrollService.GetAdvanceByIdAsync(AdvanceInput.Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Salary advance not found.";
                return RedirectToPage("/Payroll/Advances/Index");
            }

            Advance = existing;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var userName = HttpContext.Session.GetString("UserName") ?? "System";
                var updated = await _payrollService.UpdateAdvanceAsync(AdvanceInput, userName);
                TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                    ? "Salary advance updated successfully."
                    : "Salary advance not found.";

                return RedirectToPage("/Payroll/Advances/Details", new { id = AdvanceInput.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
