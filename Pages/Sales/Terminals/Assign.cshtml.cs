using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Sales.Terminals
{
    public class AssignModel : PageModel
    {
        private readonly MockSalesService _salesService;
        private readonly MockUserService _userService;

        public AssignModel(MockSalesService salesService, MockUserService userService)
        {
            _salesService = salesService;
            _userService = userService;
        }

        public PosTerminal Terminal { get; set; } = new();
        public List<User> EligibleUsers { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var term = _salesService.GetTerminalById(id);
            if (term == null) return RedirectToPage("/Sales/Terminals/Index");

            Terminal = term;
            EligibleUsers = _userService.GetAll();
            return Page();
        }

        public IActionResult OnPost(int id, int selectedUserId)
        {
            var user = _userService.GetById(selectedUserId);
            _salesService.AssignTerminalUser(id, selectedUserId, user?.Name);
            TempData["SuccessMessage"] = $"Terminal assigned to {user?.Name ?? "User"}.";
            return RedirectToPage("/Sales/Terminals/Index");
        }
    }
}
