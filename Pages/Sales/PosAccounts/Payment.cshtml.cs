using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Manufacture.Models.Entities;
using Manufacture.Services;

namespace Manufacture.Pages.Sales.PosAccounts
{
    public class PaymentModel : PageModel
    {
        private readonly MockSalesService _salesService;

        public PaymentModel(MockSalesService salesService)
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

        public void OnGet(int? id)
        {
            LoadData(id ?? Id);
        }

        public IActionResult OnPost(int accountId, decimal amountPaid, string? paymentMethod, string? reference)
        {
            if (accountId <= 0 || amountPaid <= 0)
            {
                ErrorMessage = "Please specify a valid supermarket account and payment amount.";
                LoadData(accountId);
                return Page();
            }

            var ok = _salesService.RecordSupermarketPayment(accountId, amountPaid, paymentMethod ?? "BankTransfer", reference ?? "PAY-REC");
            if (ok)
            {
                var account = _salesService.GetSupermarketAccountById(accountId);
                SuccessMessage = $"Payment of ₦{amountPaid:N2} credited to {account?.AccountName ?? "account"}. Balance updated successfully.";
                return RedirectToPage("/Sales/PosAccounts/Edit", new { id = accountId });
            }

            ErrorMessage = "Failed to record payment credit. Please check account details.";
            LoadData(accountId);
            return Page();
        }

        private void LoadData(int? accountId)
        {
            Accounts = _salesService.GetAllSupermarketAccounts().Where(a => a.IsActive).ToList();

            if (accountId.HasValue && accountId.Value > 0)
            {
                SelectedAccount = _salesService.GetSupermarketAccountById(accountId.Value);
            }
            else if (Accounts.Any())
            {
                SelectedAccount = Accounts.First();
            }
        }
    }
}
