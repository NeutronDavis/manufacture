using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Payroll
{
    public class DetailsModel : PageModel
    {
        private readonly IPayrollService _payrollService;

        public DetailsModel(IPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        public PayrollRecordDto Payroll { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var record = await _payrollService.GetPayrollRecordByIdAsync(id);
            if (record == null)
            {
                TempData["ErrorMessage"] = "Payroll record not found.";
                return RedirectToPage("/Payroll/Index");
            }

            Payroll = record;
            return Page();
        }
    }
}
