using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface ISalesService
    {
        Task<List<Customer>> GetCustomersAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

        Task<Customer?> GetCustomerAsync(int id, CancellationToken cancellationToken = default);

        Task<Customer?> GetCustomerByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<Customer> CreateCustomerAsync(CustomerDto dto, CancellationToken cancellationToken = default);

        Task<List<PosTerminal>> GetTerminalsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

        Task<PosTerminal?> GetTerminalAsync(int id, CancellationToken cancellationToken = default);

        Task<PosTerminal> CreateTerminalAsync(PosTerminal terminal, CancellationToken cancellationToken = default);

        Task<bool> UpdateTerminalAsync(PosTerminal terminal, CancellationToken cancellationToken = default);

        Task<bool> ToggleTerminalStatusAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> AssignTerminalEmployeeAsync(int terminalId, int? employeeId, CancellationToken cancellationToken = default);

        Task<List<SalesRepProfile>> GetSalesRepsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

        Task<SalesRepProfile?> GetSalesRepAsync(int id, CancellationToken cancellationToken = default);

        Task<SalesRepProfile?> GetSalesRepByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<List<Recipe>> GetSellableProductsAsync(CancellationToken cancellationToken = default);

        Task<List<(int Id, string Code, string Name, ProductType Category, decimal Price)>> GetCatalogSkusAsync(CancellationToken cancellationToken = default);

        Task<List<Order>> GetOrdersAsync(CancellationToken cancellationToken = default);

        Task<List<Order>> GetPreOrdersAsync(int? customerId = null, CancellationToken cancellationToken = default);

        Task<Order?> GetOrderAsync(int id, CancellationToken cancellationToken = default);

        Task<Order> CreateOrderAsync(
            CreateOrderDto dto,
            int? currentUserId,
            string? currentUserName,
            string? currentUserRole = null,
            CancellationToken cancellationToken = default);

        Task<bool> AssignOrderSalesRepAsync(int orderId, int? salesRepId, CancellationToken cancellationToken = default);

        Task<bool> MarkPrintedForLoadingAsync(int orderId, CancellationToken cancellationToken = default);

        Task<bool> VerifyAndDispatchAsync(int orderId, string loaderName, CancellationToken cancellationToken = default);

        Task<List<OrderPayment>> GetOrderPaymentsAsync(int orderId, CancellationToken cancellationToken = default);

        Task<bool> ApplyOrderPaymentAsync(OrderPaymentInputDto input, CancellationToken cancellationToken = default);

        Task<List<SupermarketAccount>> GetSupermarketAccountsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

        Task<SupermarketAccount?> GetSupermarketAccountAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> SaveSupermarketAccountAsync(SupermarketAccount account, CancellationToken cancellationToken = default);

        Task<bool> SetCustomAgreedPriceAsync(int accountId, int productId, decimal customPrice, CancellationToken cancellationToken = default);

        Task<bool> SetBatchCustomAgreedPricesAsync(int accountId, Dictionary<int, decimal> prices, CancellationToken cancellationToken = default);

        Task<bool> RecordSupermarketPaymentAsync(int accountId, decimal amountPaid, string paymentMethod, string reference, CancellationToken cancellationToken = default);

        Task<bool> ToggleSupermarketStatusAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> FulfillSupermarketOrderAsync(int accountId, int repId, List<OrderItemDto> items, string notes, CancellationToken cancellationToken = default);

        Task<List<DailyRepRouteSheet>> GetRouteSheetsAsync(DateTime? date = null, int? repId = null, RouteSheetStatus? status = null, CancellationToken cancellationToken = default);

        Task<DailyRepRouteSheet?> GetRouteSheetAsync(int id, CancellationToken cancellationToken = default);

        Task<DailyRepRouteSheet> CreateRouteSheetAsync(int repId, DateTime date, Dictionary<int, int>? requestedQuantities = null, CancellationToken cancellationToken = default);

        Task<bool> SignOffDispatchAsync(int sheetId, Dictionary<int, int> loadedQuantities, string dispatcherName, string notes, CancellationToken cancellationToken = default);

        Task<bool> ReconcileDailyRepAccountAsync(ReconciliationInputDto input, CancellationToken cancellationToken = default);

        Task<bool> DeleteRouteSheetAsync(int sheetId, string deletedBy, CancellationToken cancellationToken = default);

        Task<List<CustomerReturn>> GetCustomerReturnsAsync(DateTime? date = null, int? salesRepId = null, CancellationToken cancellationToken = default);

        Task<CustomerReturn> RecordCustomerReturnAsync(CustomerReturnInputDto input, CancellationToken cancellationToken = default);

        Task<List<BonusRule>> GetBonusRulesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

        Task<int> CalculateBonusQuantityAsync(int recipeId, int quantity, CancellationToken cancellationToken = default);

        Task<BonusRule> SaveBonusRuleAsync(BonusRuleDto dto, CancellationToken cancellationToken = default);

        Task<bool> ToggleBonusRuleStatusAsync(int id, CancellationToken cancellationToken = default);
    }
}
