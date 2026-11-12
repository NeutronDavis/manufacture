using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Sales.PosAccounts
{
    public class PaymentModel : PageModel
    {
        private readonly ISalesService _salesService;

        public PaymentModel(ISalesService salesService)
        {
            _salesService = salesService;
        }

        public List<SupermarketAccount> Accounts { get; set; } = new();
        public SupermarketAccount? SelectedAccount { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(int? id, CancellationToken cancellationToken = default)
        {
            await LoadDataAsync(id ?? Id, cancellationToken);
        }

        public async Task<IActionResult> OnPostAsync(
            int accountId,
            decimal amountPaid,
            string? paymentMethod,
            string? reference,
            CancellationToken cancellationToken = default)
        {
            if (accountId <= 0 || amountPaid <= 0)
            {
                ErrorMessage = "Please specify a valid supermarket account and payment amount.";
                await LoadDataAsync(accountId, cancellationToken);
                return Page();
            }

            var ok = await _salesService.RecordSupermarketPaymentAsync(
                accountId,
                amountPaid,
                paymentMethod ?? "BankTransfer",
                reference ?? "PAY-REC",
                cancellationToken);

            if (ok)
            {
                var account = await _salesService.GetSupermarketAccountAsync(accountId, cancellationToken);
                SuccessMessage = $"Payment of ₦{amountPaid:N2} credited to {account?.AccountName ?? "account"}. Balance updated successfully.";
                return RedirectToPage("/Sales/PosAccounts/Edit", new { id = accountId });
            }

            ErrorMessage = "Failed to record payment credit. Please check account details.";
            await LoadDataAsync(accountId, cancellationToken);
            return Page();
        }

        private async Task LoadDataAsync(int? accountId, CancellationToken cancellationToken)
        {
            Accounts = (await _salesService.GetSupermarketAccountsAsync(cancellationToken: cancellationToken))
                .Where(a => a.IsActive)
                .ToList();

            if (accountId.HasValue && accountId.Value > 0)
            {
                SelectedAccount = await _salesService.GetSupermarketAccountAsync(accountId.Value, cancellationToken);
            }
            else if (Accounts.Any())
            {
                SelectedAccount = Accounts.First();
            }
        }
    }
}
