using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Payroll.Advances
{
    public class IndexModel : PageModel
    {
        private readonly MockPayrollService _payrollService;

        public IndexModel(MockPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        public List<SalaryAdvance> Advances { get; set; } = new();

        public void OnGet()
        {
            Advances = _payrollService.GetAllAdvances();
        }
    }
}
