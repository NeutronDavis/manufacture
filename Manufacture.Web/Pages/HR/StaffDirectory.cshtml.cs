using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.HR
{
    public class StaffDirectoryModel : PageModel
    {
        private readonly MockEmployeeService _employeeService;

        public StaffDirectoryModel(MockEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        public List<Employee> Employees { get; set; } = new();

        public void OnGet()
        {
            Employees = _employeeService.GetAll(includeInactive: true);
        }
    }
}
