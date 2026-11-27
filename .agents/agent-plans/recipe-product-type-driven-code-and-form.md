# Plan: Product-Type-Driven Recipe Code and Form Reorder

**Status**: Done

## Summary

Make the **Product Type** the first field in the recipe builder and use it as the source of truth for the recipe's Product Name, Product Code, Baseline Unit, and Output Unit Noun.

- Product Type moves above Product Name on both **Create** and **Edit**.
- Selecting a Product Type auto-fills:
  - **Product Name** with the Product Type Name (still editable).
  - **Baseline Unit** from the Product Type settings (read-only).
  - **Output Unit Noun** from the Product Type settings (read-only).
  - **Product Code** using the Product Type prefix plus a running number, e.g. `BRD-001`, `WTR-001`, `POP-001` (read-only).
- On Edit, the existing name/code are preserved on load; changing the Product Type regenerates them. Switching back to the original type restores the original name and code.
- No database schema change is required. Existing recipe codes are not migrated.

## Scope

- **In-Scope**:
  - New recipe-code generation helper and service method.
  - Create Recipe form reorder + autofill JavaScript.
  - Edit Recipe form reorder + safe regeneration JavaScript and server logic.
  - Server-side authoritative code generation on save.
  - Unit tests for prefix/code generation.
- **Out-of-Scope**:
  - Changes to the Settings → Production product type form or database schema.
  - Migrating or renaming existing recipe codes.
  - Changing recipe codes when a product type is renamed later.
  - The Recipes Index/Costing overview page (it has no Product Type or Product Code input).

## Implementation Steps

### Phase 1: Code Generation Core

1. **Add `RecipeCodeGenerator` helper**
   - New file: `Manufacture.Web/Services/RecipeCodeGenerator.cs`, namespace `Manufacture.Services`.
   - Public static methods:
     - `string PrefixFor(string? productTypeName)`
     - `string NextCode(string prefix, IEnumerable<string> existingCodes)`
   - `PrefixFor` rules:
     - Normalize to uppercase alphanumeric/word tokens.
     - Fixed mappings first: contains `BREAD` → `BRD`, `WATER` → `WTR`, `POPCORN` → `POP`, `SNACK` → `SNK`.
     - Generic fallback: for multi-word names use the first letter of up to three words; for one word use its first three alphanumeric characters; pad with `X` to three characters if shorter; fall back to `GEN` when nothing usable remains.
   - `NextCode` rules:
     - Consider only codes beginning with `"{prefix}-"`, case-insensitively.
     - Parse the remaining suffix as an integer; ignore non-numeric suffixes such as `JMB` or `6IN1`.
     - Return `"{prefix}-{next:000}"`, starting at `001` and skipping any already-used number.
   - *Dependencies*: None.

2. **Add tests for the generator**
   - New file: `Manufacture.Tests/RecipeCodeGeneratorTests.cs`.
   - Cover: Bread/Water/Popcorn mappings, generic single-word and multi-word fallbacks, empty code lists, sequential numbering, ignored non-numeric suffixes, and 3-digit padding.
   - *Dependencies*: Depends on Step 1.

3. **Expose authoritative code generation on the production service**
   - `Manufacture.Library/Interfaces/IProductionService.cs`: add
     `string GenerateRecipeCode(int productionProductTypeId, int? excludeRecipeId = null);`
   - `Manufacture.Web/Services/ProductionService.cs`: implement it.
     - Load the product type by id; throw `InvalidOperationException("Select a valid product type.")` when missing.
     - Query `_db.Recipes` codes for the generated prefix, excluding `excludeRecipeId` when supplied.
     - Use `RecipeCodeGenerator.NextCode`.
   - *Dependencies*: Depends on Step 1.

### Phase 2: Create Recipe Page

4. **Update `Create.cshtml.cs` model**
   - Add `public Dictionary<int, string> NextCodes { get; set; } = new();`.
   - In `LoadFormData()`:
     - Use `_productionService.GetAllRecipes(includeInactive: true)` so inactive recipe codes are never reused.
     - Build `NextCodes` for every active product type using `RecipeCodeGenerator.PrefixFor` + `NextCode` against all existing codes.
   - Replace the hard-coded `RecipeInput.Code = "BRD-NEW"` in `SeedDefaults()` with the selected type's `NextCodes` value.
   - Keep `SeedDefaults(preserveInput: true)` from overwriting a user-entered name/code after a validation failure; it should still refresh `ProductType`, `BaselineUnit`, and `OutputUnit`.
   - In `OnPost()`:
     - After `LoadFormData()` and before the `ModelState.IsValid` check, when `RecipeInput.ProductionProductTypeId` matches a loaded product type:
       - Set `RecipeInput.Code = _productionService.GenerateRecipeCode(RecipeInput.ProductionProductTypeId);`
       - Call `ModelState.Remove("RecipeInput.Code");` so the read-only/empty posted field cannot block saving.
     - Leave the rest of the existing yield/ingredient validation and save flow unchanged.
   - *Dependencies*: Depends on Step 3.

5. **Reorder and auto-fill `Create.cshtml`**
   - Move the **Product Type** select out of "Baseline Production Unit & Expected Yield" and make it the first field in **Product Recipe Details**, above Product Name.
   - New field order in Card 1: Product Type → Product Name → Product Code → Description.
   - Card 2 keeps Baseline Unit, Expected Yield, and Output Unit Noun (the Product Type select is removed there). Keep the baseline hint.
   - Product Code input:
     - Keep `asp-for="RecipeInput.Code"` and `required`.
     - Add `readonly` and a muted/read-only style (same treatment as Baseline Unit).
     - Add helper text: "Auto-generated from the selected product type."
   - Product Name helper text: "Prefilled from the selected product type; rename to match this SKU."
   - Serialize `NextCodes` to JavaScript alongside `productTypes`.
   - Replace `syncBaselineDefaults()` with `syncProductTypeDefaults()`:
     - Set `RecipeInput_Name.value = type.name`.
     - Set `RecipeInput_Code.value = nextCodes[type.id]` (fallback to an empty string).
     - Set `RecipeInput_BaselineUnit.value = type.baseline`.
     - Set `RecipeInput_OutputUnit.value = type.outputUnit`.
     - Call `renderBaselineHint()` and `renderTable()`.
   - Change the select's `onchange` to `syncProductTypeDefaults()`.
   - On `DOMContentLoaded`:
     - Call `syncProductTypeDefaults()` only on the fresh GET.
     - On a failed POST, skip it so the user's entered name/code are preserved. Detect with a Razor-rendered boolean such as `const preserveInput = @((!ViewData.ModelState.IsValid).ToString().ToLowerInvariant());`.
   - *Dependencies*: Depends on Step 4.

### Phase 3: Edit Recipe Page

6. **Update `Edit.cshtml.cs` model**
   - Add `[BindProperty] public int OriginalProductTypeId { get; set; }`.
   - Add `public Dictionary<int, string> NextCodes { get; set; } = new();`.
   - In `OnGet()` set `OriginalProductTypeId = RecipeInput.ProductionProductTypeId` after mapping the recipe.
   - In `LoadFormData()` build `NextCodes` from `GetAllRecipes(includeInactive: true)` **excluding the current recipe id**, so the preview does not collide with itself.
   - In `OnPost()`:
     - Load the existing recipe from the database; redirect to the index if missing.
     - If `RecipeInput.ProductionProductTypeId != OriginalProductTypeId` and the new type exists:
       - Set `RecipeInput.Name = type.Name`, `RecipeInput.ProductType = type.Name`.
       - Set `RecipeInput.BaselineUnit = type.Baseline`, `RecipeInput.OutputUnit = type.OutputUnitNoun`.
       - Set `RecipeInput.Code = _productionService.GenerateRecipeCode(type.Id, RecipeInput.Id)`.
       - `ModelState.Remove("RecipeInput.Code")`.
     - If the type is unchanged, force `RecipeInput.Code = existing.Code` and refresh `ProductType`, `BaselineUnit`, and `OutputUnit` from the type.
     - Keep the existing yield/ingredient validation and update flow unchanged.
   - *Dependencies*: Depends on Step 3.

7. **Reorder and auto-fill `Edit.cshtml`**
   - Apply the same field order as Create: Product Type first in Card 1, then Product Name, Product Code, Description. Remove Product Type from Card 2.
   - Make Product Code `readonly` with the same helper text.
   - Add a hidden input for `OriginalProductTypeId`.
   - Serialize `NextCodes`, `Model.OriginalProductTypeId`, the original name, and the original code to JavaScript.
   - Add `syncProductTypeDefaults()`:
     - If the selected type equals `OriginalProductTypeId`: restore the original name and original code.
     - Otherwise: set name to `type.name`, code to `nextCodes[type.id]`, baseline to `type.baseline`, and output unit to `type.outputUnit`.
     - Call `renderBaselineHint()` and `renderTable()`.
   - Change the select's `onchange` to `syncProductTypeDefaults()`.
   - On `DOMContentLoaded`, only call `renderBaselineHint()`; do not overwrite the existing name/code.
   - *Dependencies*: Depends on Step 6.

### Phase 4: Verification

8. **Build, test, and manually verify**
   - *Dependencies*: Depends on Phases 1–3.
   - See the Verification Plan below.

## Relevant Files & Symbols

- `Manufacture.Web/Services/RecipeCodeGenerator.cs` — **new**; prefix and next-code logic.
- `Manufacture.Tests/RecipeCodeGeneratorTests.cs` — **new**; unit tests.
- `Manufacture.Library/Interfaces/IProductionService.cs` — add `GenerateRecipeCode`.
- `Manufacture.Web/Services/ProductionService.cs` — implement code generation near the recipe methods (`GetAllRecipes`, `GetRecipeById`, `CreateRecipe`, `UpdateRecipe`).
- `Manufacture.Web/Pages/Production/Recipes/Create.cshtml.cs` — `LoadFormData`, `SeedDefaults`, `ApplyProductTypeDefaults`, `OnPost`.
- `Manufacture.Web/Pages/Production/Recipes/Create.cshtml` — Card 1 / Card 2 markup, Code input, `productTypes` JS, `syncBaselineDefaults`, `DOMContentLoaded`.
- `Manufacture.Web/Pages/Production/Recipes/Edit.cshtml.cs` — `OnGet`, `OnPost`, `LoadFormData`, `MapToBuilderDto`.
- `Manufacture.Web/Pages/Production/Recipes/Edit.cshtml` — Card 1 / Card 2 markup, Code input, `productTypes` JS, `syncBaselineDefaults`, `DOMContentLoaded`.
- Reference pattern: `Manufacture.Web/Services/InventoryService.cs` — `GenerateItemCodeAsync`, `CategoryPrefix`, `NameToken` show the existing prefix/sequence style.
- Domain shape reference: `Manufacture.Library/Models/Domain/ProductionEntities.cs` — `ProductionProductType`, `Recipe`.
- DTO reference: `Manufacture.Library/Models/DTOs/RecipeCostingDtos.cs` — `RecipeBuilderDto`.

## Verification Plan

1. **Automated Tests**
   - `dotnet test "Manufacture.Tests/Manufacture.Tests.csproj" --nologo`
   - If the running web app locks build outputs, close it first, or build to a temporary output directory:
     `dotnet build "Manufacture.Web/Manufacture.Web.csproj" -p:OutDir="C:\Users\user\AppData\Local\Temp\opencode\manufacture-build\" --nologo`
   - Run the full existing test suite to confirm no regressions.

2. **Manual Verification**
   - Open **Production → Recipes → Create**.
   - Confirm **Product Type** is the first field, above Product Name.
   - Select `Bread`: Product Name becomes `Bread`, Product Code shows `BRD-001`, Baseline Unit and Output Unit Noun populate, and the code field is read-only.
   - Rename the product to a custom name: the code must not change.
   - Save: the Details page shows `BRD-001`.
   - Create a second Bread recipe: it receives `BRD-002`.
   - Create Water and Popcorn recipes: they receive `WTR-001` and `POP-001`.
   - Submit an invalid form (e.g. no ingredients): the form re-renders with the entered name and code preserved.
   - Open **Edit** on an existing recipe: name and code stay unchanged on load.
   - Change the Product Type: name, baseline, output unit, and code regenerate for the new type.
   - Change back to the original type: the original name and original code return.
   - Save an edit without changing the type: the existing code is preserved.
   - Confirm a non–Super Admin/other role can still use the pages as before and the existing delete permission behavior is untouched.

## Context & Decisions Log

- **2026-11-23**: User requirement verbatim: "at the Recipes/Costing page, the product type shpuld be rus to genereate the Product code, that means the product type".
- **2026-11-23**: User requirement verbatim: "the product type field should come first before the product name".
- **2026-11-23**: User requirement verbatim: "when selecting the product type field in the /Recipes/Create page it should populate the product name field with the product type name , Baseline, output Unit Noun fields too. this also means that the product type field will come first in the form so the form must be rearange".
- **2026-11-23**: Decision: Product Code format is **type prefix + running number** (e.g. `BRD-001`).
- **2026-11-23**: Decision: Product Name is **editable**; Product Code is **read-only**; Baseline Unit and Output Unit Noun are auto-filled.
- **2026-11-23**: Decision: Apply the same Product Type-first behavior to **Edit**, with safe regeneration only when the Product Type changes.
- **2026-11-23**: Decision: No database schema change; derive the prefix from the existing Product Type Name.

## Key Decisions & Trade-offs

- **Prefix source**: Deriving from the Product Type Name avoids a new database column and migration. Trade-off: unknown future product types use a generic 3-character abbreviation, and renaming a Product Type changes only future codes.
- **Code format**: `PREFIX-###` gives deterministic, readable, unique codes. Trade-off: codes are less descriptive than existing manual codes such as `BRD-JMB`, but they remain stable after creation.
- **Server authority**: Create always regenerates the code server-side; the client value is only a preview. Trade-off: a rare concurrent save may produce a different final code than previewed, but the database unique index and generation loop prevent duplicates.
- **Edit safety**: Existing recipes keep their codes unless the Product Type actually changes. Switching back to the original type restores the original name and code in the UI, and the server also keeps the stored code.
- **Name autofill on Create**: Changing the Product Type overwrites Product Name with the type name, as requested. The admin can rename it afterward before saving.
