using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.Terminals
{
    public class IndexModel : PageModel
    {
        private readonly ISalesService _salesService;

        public IndexModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        public List<PosTerminal> Terminals { get; set; } = new();

        public int TotalCount => Terminals.Count;
        public int ActiveCount => Terminals.Count(t => t.IsActive);
        public int InactiveCount => Terminals.Count(t => !t.IsActive);

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            Terminals = await _salesService.GetTerminalsAsync(includeInactive: true, cancellationToken);
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id, CancellationToken cancellationToken = default)
        {
            var terminal = await _salesService.GetTerminalAsync(id, cancellationToken);
            if (terminal == null)
            {
                ErrorMessage = "Terminal not found.";
                return RedirectToPage();
            }

            var ok = await _salesService.ToggleTerminalStatusAsync(id, cancellationToken);
            if (ok)
            {
                SuccessMessage = $"POS Terminal '{terminal.TerminalCode}' status updated.";
            }
            else
            {
                ErrorMessage = "Failed to update terminal status.";
            }

            return RedirectToPage();
        }
    }
}
