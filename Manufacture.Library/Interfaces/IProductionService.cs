using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    /// <summary>
    /// Database-backed production service for recipes, costing, batches,
    /// production settings, and production demand.
    /// </summary>
    public interface IProductionService
    {
        List<ProductionCategory> GetCategories(bool includeInactive = false);
        ProductionCategory SaveCategory(ProductionCategory input);
        bool ToggleCategoryStatus(int id);

        List<ProductionProductType> GetProductTypes(int? categoryId = null, bool includeInactive = false);
        ProductionProductType SaveProductType(ProductionProductType input);
        bool ToggleProductTypeStatus(int id);

        List<ProductionShift> GetShifts(bool includeInactive = false);
        ProductionShift SaveShift(ProductionShift input);
        bool ToggleShiftStatus(int id);

        List<Ingredient> GetAllIngredients();
        Ingredient? GetIngredientById(int id);

        List<Recipe> GetAllRecipes(bool includeInactive = false);
        Recipe? GetRecipeById(int id);
        Recipe CreateRecipe(RecipeBuilderDto dto);
        bool UpdateRecipe(RecipeBuilderDto dto);
        bool SoftDeleteRecipe(int id);

        /// <summary>
        /// Returns a human-readable reason when the recipe cannot be permanently
        /// deleted because linked records exist; otherwise returns <c>null</c>.
        /// </summary>
        string? GetRecipeDeleteBlocker(int id);
        bool DeleteRecipe(int id);

        List<ProductionBatch> GetAllBatches();
        ProductionBatch? GetBatchById(int id);
        ProductionBatch CreateBatch(ProductionBatchDto dto, string loggedBy);
        ProductionBatch LogDailyProduction(DailyProductionEntryDto dto, string loggedBy);
        bool UpdateBatchStatus(int id, BatchStatus status, int actualQuantity);
        ProductionBatch? RecordBatchActualOutput(int id, int actualQuantity, BatchStatus status, string? notes = null, string? bakerInCharge = null);

        List<ProductionDefectDto> GetBatchDefects(int batchId);
        ProductionDefectDto? SaveBatchDefects(int batchId, int quantity, string? reason, string recordedBy);

        decimal CalculateBatchMaterialCost(int recipeId, decimal batchInputQuantity);
        decimal CalculateRunUnitCost(decimal totalBatchMaterialCost, decimal actualYieldQuantity);
        BatchProductionRunDto CalculateBatchProductionRun(int recipeId, decimal batchInputQuantity, int actualYieldQuantity, int targetOrderQuantity = 0);
        CategoryLabelsDto GetCategoryLabels(ProductType category);
        bool UpdateIngredientPrice(int inventoryItemId, decimal newBaseUnitCost);
        int ApplyPriceUpdates(IEnumerable<UpdateIngredientPriceDto> updates);
        int SyncAllPriceSnapshots();

        List<IngredientPriceChangeRequestDto> GetIngredientPriceChangeRequests(IngredientPriceChangeStatus? status = null);
        PriceChangeSubmissionResultDto SubmitIngredientPriceChangeRequests(
            IEnumerable<UpdateIngredientPriceDto> updates,
            int requestedByUserId,
            string requestedByName);
        PriceChangeActionResultDto ApproveIngredientPriceChangeRequest(
            int requestId,
            int reviewedByUserId,
            string reviewedByName,
            string? notes = null);
        PriceChangeActionResultDto RejectIngredientPriceChangeRequest(
            int requestId,
            int reviewedByUserId,
            string reviewedByName,
            string reason);

        ProductionDashboardDto GetProductionDashboard(DateTime date);

        int GetDemandForRecipe(int recipeId);
        List<Order> GetOpenOrdersForRecipe(int? recipeId = null);
        List<Order> GetPreOrders();
        Order? GetOrderById(int id);
    }
}
