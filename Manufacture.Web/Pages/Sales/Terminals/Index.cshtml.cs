using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Entities;

namespace Manufacture.Pages.Sales.Terminals
{
    public class IndexModel : PageModel
    {
        private readonly MockSalesService _salesService;
        private readonly MockUserService _userService;

        public IndexModel(MockSalesService salesService, MockUserService userService)
        {
            _salesService = salesService;
            _userService = userService;
        }

        public List<PosTerminal> Terminals { get; set; } = new();
        public List<User> EligibleUsers { get; set; } = new();

        public int TotalCount => Terminals.Count;
        public int ActiveCount => Terminals.Count(t => t.IsActive);
        public int InactiveCount => Terminals.Count(t => !t.IsActive);

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            LoadData();
        }

        public IActionResult OnPostCreateTerminal([FromForm] PosTerminal terminal)
        {
            if (string.IsNullOrWhiteSpace(terminal.TerminalName))
            {
                ErrorMessage = "Terminal name is required.";
                return RedirectToPage();
            }

            if (terminal.AssignedUserId.HasValue && terminal.AssignedUserId.Value > 0)
            {
                var user = _userService.GetById(terminal.AssignedUserId.Value);
                terminal.AssignedUserName = user?.Name ?? "Unassigned";
            }
            else
            {
                terminal.AssignedUserId = null;
                terminal.AssignedUserName = "Unassigned";
            }

            var created = _salesService.CreateTerminal(terminal);
            SuccessMessage = $"POS Terminal '{created.TerminalName}' ({created.TerminalCode}) registered successfully.";
            return RedirectToPage();
        }

        public IActionResult OnPostUpdateTerminal([FromForm] PosTerminal terminal)
        {
            if (terminal.Id <= 0)
            {
                ErrorMessage = "Invalid terminal identifier.";
                return RedirectToPage();
            }

            if (terminal.AssignedUserId.HasValue && terminal.AssignedUserId.Value > 0)
            {
                var user = _userService.GetById(terminal.AssignedUserId.Value);
                terminal.AssignedUserName = user?.Name ?? "Unassigned";
            }
            else
            {
                terminal.AssignedUserId = null;
                terminal.AssignedUserName = "Unassigned";
            }

            var ok = _salesService.UpdateTerminal(terminal);
            if (ok)
            {
                SuccessMessage = $"POS Terminal '{terminal.TerminalName}' ({terminal.TerminalCode}) updated successfully.";
            }
            else
            {
                ErrorMessage = "Could not find or update the specified terminal.";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostToggleStatus(int id)
        {
            var terminal = _salesService.GetTerminalById(id);
            if (terminal == null)
            {
                ErrorMessage = "Terminal not found.";
                return RedirectToPage();
            }

            var ok = _salesService.ToggleTerminalStatus(id);
            if (ok)
            {
                string state = terminal.IsActive ? "activated" : "deactivated / set to offline";
                SuccessMessage = $"POS Terminal '{terminal.TerminalCode}' has been {state}.";
            }
            else
            {
                ErrorMessage = "Failed to update terminal status.";
            }

            return RedirectToPage();
        }

        private void LoadData()
        {
            Terminals = _salesService.GetAllTerminals();
            EligibleUsers = _userService.GetAll();
        }
    }
}
