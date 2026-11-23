using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Payroll.Advances
{
    public class DetailsModel : PageModel
    {
        private readonly IPayrollService _payrollService;

        public DetailsModel(IPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        public SalaryAdvanceListDto Advance { get; set; } = new();
        public List<PayrollDeductionLineDto> Repayments { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var advance = await _payrollService.GetAdvanceByIdAsync(id);
            if (advance == null)
            {
                TempData["ErrorMessage"] = "Salary advance not found.";
                return RedirectToPage("/Payroll/Advances/Index");
            }

            Advance = advance;
            Repayments = await _payrollService.GetAdvanceRepaymentsAsync(id);
            return Page();
        }
    }
}
