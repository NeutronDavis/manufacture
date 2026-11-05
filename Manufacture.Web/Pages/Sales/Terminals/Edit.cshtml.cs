using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Domain;

namespace Manufacture.Pages.Sales.Terminals
{
    public class EditModel : PageModel
    {
        private readonly MockSalesService _salesService;
        private readonly MockUserService _userService;

        public EditModel(MockSalesService salesService, MockUserService userService)
        {
            _salesService = salesService;
            _userService = userService;
        }

        [BindProperty]
        public PosTerminal Terminal { get; set; } = new();

        public List<User> EligibleUsers { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet(int id)
        {
            var term = _salesService.GetTerminalById(id);
            if (term == null)
            {
                ErrorMessage = $"POS Terminal #{id} was not found.";
                return RedirectToPage("/Sales/Terminals/Index");
            }

            Terminal = new PosTerminal
            {
                Id = term.Id,
                TerminalCode = term.TerminalCode,
                TerminalName = term.TerminalName,
                AssignedUserId = term.AssignedUserId,
                AssignedUserName = term.AssignedUserName,
                CashAccountName = term.CashAccountName,
                TransferAccountName = term.TransferAccountName,
                PosBankAccountName = term.PosBankAccountName,
                IsActive = term.IsActive
            };

            EligibleUsers = _userService.GetAll();
            return Page();
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Terminal.TerminalName))
            {
                ModelState.AddModelError("Terminal.TerminalName", "Terminal name is required.");
                EligibleUsers = _userService.GetAll();
                return Page();
            }

            if (Terminal.AssignedUserId.HasValue && Terminal.AssignedUserId.Value > 0)
            {
                var user = _userService.GetById(Terminal.AssignedUserId.Value);
                Terminal.AssignedUserName = user?.Name ?? "Unassigned";
            }
            else
            {
                Terminal.AssignedUserId = null;
                Terminal.AssignedUserName = "Unassigned";
            }

            var ok = _salesService.UpdateTerminal(Terminal);
            if (ok)
            {
                SuccessMessage = $"POS Terminal '{Terminal.TerminalName}' ({Terminal.TerminalCode}) updated successfully.";
                return RedirectToPage("/Sales/Terminals/Index");
            }

            ErrorMessage = "Could not update the specified terminal.";
            EligibleUsers = _userService.GetAll();
            return Page();
        }

        public IActionResult OnPostToggleStatus(int id)
        {
            var term = _salesService.GetTerminalById(id);
            if (term == null)
            {
                ErrorMessage = "Terminal not found.";
                return RedirectToPage("/Sales/Terminals/Index");
            }

            var ok = _salesService.ToggleTerminalStatus(id);
            if (ok)
            {
                string state = term.IsActive ? "activated" : "set to offline";
                SuccessMessage = $"POS Terminal '{term.TerminalCode}' has been {state}.";
            }

            return RedirectToPage(new { id });
        }
    }
}
