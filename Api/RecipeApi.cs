using Manufacture.Models.DTOs;
using Manufacture.Services;

namespace Manufacture.Api
{
    /// <summary>
    /// Minimal API endpoints for the Dual-Mode (Hybrid) Recipe Costing Engine.
    /// Exposes calculation, top-down/bottom-up conversions, and recipe DTO retrieval.
    /// </summary>
    public static class RecipeApi
    {
        public static void MapRecipeApi(this WebApplication app)
        {
            // Calculate a recipe payload in either BATCH_FIRST or SINGLE_UNIT mode
            app.MapPost("/api/recipes/cost-engine/calculate", (RecipeDto dto, RecipeCostingEngine engine) =>
            {
                var result = engine.CalculateDualModeRecipe(dto);
                return Results.Ok(result);
            });

            // Get all recipes as dual-mode RecipeDtos
            app.MapGet("/api/recipes/dual-mode", (HttpRequest request, MockProductionService production, RecipeCostingEngine engine) =>
            {
                var modeQuery = request.Query["mode"].ToString();
                var mode = Enum.TryParse<RecipeEntryMode>(modeQuery, ignoreCase: true, out var parsedMode)
                    ? parsedMode
                    : (string.Equals(modeQuery, "SINGLE_UNIT", StringComparison.OrdinalIgnoreCase) ? RecipeEntryMode.SingleUnit : RecipeEntryMode.BatchFirst);

                var recipes = production.GetAllRecipeDtos(mode, engine);
                return Results.Ok(recipes);
            });

            // Get a single recipe by ID as dual-mode RecipeDto
            app.MapGet("/api/recipes/dual-mode/{id:int}", (int id, HttpRequest request, MockProductionService production, RecipeCostingEngine engine) =>
            {
                var modeQuery = request.Query["mode"].ToString();
                var mode = Enum.TryParse<RecipeEntryMode>(modeQuery, ignoreCase: true, out var parsedMode)
                    ? parsedMode
                    : (string.Equals(modeQuery, "SINGLE_UNIT", StringComparison.OrdinalIgnoreCase) ? RecipeEntryMode.SingleUnit : RecipeEntryMode.BatchFirst);

                var recipe = production.GetRecipeDto(id, mode, engine);
                return recipe != null ? Results.Ok(recipe) : Results.NotFound();
            });

            // Get compatible units for a material / base unit
            app.MapGet("/api/units/compatible", (string? unit, string? material, IUnitConversionService converter) =>
            {
                var units = converter.GetCompatibleUnits(unit, material);
                return Results.Ok(units);
            });

            // Convert quantity between units
            app.MapGet("/api/units/convert", (decimal qty, string from, string to, string? material, IUnitConversionService converter) =>
            {
                var converted = converter.Convert(qty, from, to, material);
                var hint = converter.FormatConversionHint(qty, from, to, material);
                return Results.Ok(new
                {
                    success = converter.CanConvert(from, to, material),
                    originalQuantity = qty,
                    fromUnit = from,
                    convertedQuantity = converted,
                    toUnit = to,
                    hint = hint
                });
            });
        }
    }
}
