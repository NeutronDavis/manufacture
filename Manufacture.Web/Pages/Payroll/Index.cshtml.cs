using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Payroll
{
    public class IndexModel : PageModel
    {
        private readonly IPayrollService _payrollService;

        public IndexModel(IPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        public List<PayrollRunDto> PayrollRuns { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateOnly? GeneratedDate { get; set; }

        public string FilterLabel => GeneratedDate.HasValue
            ? $"Generated on {GeneratedDate.Value:dd MMM yyyy}"
            : "All payroll runs";

        public async Task OnGetAsync(DateOnly? date)
        {
            GeneratedDate = date ?? GeneratedDate;
            PayrollRuns = await _payrollService.GetPayrollRunsAsync(GeneratedDate);
        }
    }
}
