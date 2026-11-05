using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Services;
using Manufacture.Models.Domain;

namespace Manufacture.Pages.Sales.Terminals
{
    public class CreateModel : PageModel
    {
        private readonly MockSalesService _salesService;
        private readonly MockUserService _userService;

        public CreateModel(MockSalesService salesService, MockUserService userService)
        {
            _salesService = salesService;
            _userService = userService;
        }

        [BindProperty]
        public PosTerminal Terminal { get; set; } = new()
        {
            TerminalCode = "POS-04",
            TerminalName = "Retail Checkout Register 4",
            CashAccountName = "Bakery Cash Till #4",
            TransferAccountName = "FirstBank PLC - 3021948291 (Bakery Ops)",
            PosBankAccountName = "Moniepoint POS #04 - 9028129",
            IsActive = true
        };

        public List<User> EligibleUsers { get; set; } = new();

        public void OnGet()
        {
            EligibleUsers = _userService.GetAll();
            var all = _salesService.GetAllTerminals();
            int nextNum = all.Any() ? all.Max(t => t.Id) + 1 : 1;
            Terminal.TerminalCode = $"POS-{nextNum:00}";
            Terminal.TerminalName = $"Checkout Register #{nextNum}";
            Terminal.CashAccountName = $"Cash Till #{nextNum}";
            Terminal.PosBankAccountName = $"Moniepoint POS #{nextNum:00} - 9028129";
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

            var created = _salesService.CreateTerminal(Terminal);
            TempData["SuccessMessage"] = $"POS Terminal '{created.TerminalName}' ({created.TerminalCode}) registered successfully.";
            return RedirectToPage("/Sales/Terminals/Index");
        }
    }
}
