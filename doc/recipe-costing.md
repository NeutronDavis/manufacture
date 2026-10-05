```
# TASK: Implement Production Recipe Costing, Batch Management & Dynamic Unit Costing Engine

## Objective
Implement the backend models, dynamic calculation engine, and frontend UI components for a multi-product manufacturing management system (Bread, Water, and Popcorn). The system must handle baseline batch recipes, dynamic raw material price updates, daily production yield entry, and automated unit cost/profit margin tracking.

---

## 1. Core Business Logic & Rules

### A. Bread Production (50 kg Flour Baseline Standard)
- **Baseline Batch Unit**: 1 Bag of Flour (50 kg) is the standard unit of measurement for industrial mixers.
- **Recipe Definition**: Define ingredient quantities (sugar, salt, yeast, softener, milk flavor, preservatives) relative to 1 bag of flour (50 kg).
- **Dough Allocation**: 1 batch of dough from 50 kg flour can be split into different product recipes (Jumbo, Medium, Family Loaf, Round, 6-in-1).
- **Per-Loaf Recipe Calculation**:
  `Ingredient per Loaf = (Total Quantity of Ingredient in 50 kg Batch) / (Expected Batch Yield)`

### B. Water & Popcorn Production Adaptations
- **Water (18.9 L Dispenser Bottles)**: Recipe batch cost is defined for a full production run; unit cost = total batch material cost / actual filled dispenser bottles (e.g., 250 bottles).
- **Popcorn**: Baseline batch is 1 standard cooking pot (~2 kg corn + oil + seasonings); unit cost = total pot cost / actual output (racks/bags produced).

### C. Dynamic Yield Costing Engine
- **Master Ingredient Price List**: Maintain raw material unit prices (per kg, g, L). Admin updates vendor prices dynamically (e.g., flour price changes).
- **Total Batch Cost Calculation**: 
  `Total Batch Cost = SUM(Ingredient Batch Quantity * Current Ingredient Unit Price)`
- **Actual Yield Adjustment**: Production Managers input the actual daily yield (e.g., 60 loaves/pack).
- **Actual Production Unit Cost**:
  `Actual Unit Cost = Total Batch Cost / Actual Batch Yield`

### D. Profitability & Sales Margin Engine
- **Admin Selling Price**: Configure product wholesale/retail selling prices independently of production cost.
- **Automated Profit Margin**:
  `Profit Margin per Unit = Configured Selling Price - Actual Unit Production Cost`

---

## 2. Database Schema & Data Models

1. `ingredients`: `id`, `name`, `unit_of_measure` (kg, g, L), `current_unit_cost`, `updated_at`
2. `recipes`: `id`, `product_name`, `product_type` (bread, water, popcorn), `baseline_unit` (50kg_flour_bag, 18.9L_run, popcorn_pot), `expected_yield`
3. `recipe_ingredients`: `id`, `recipe_id`, `ingredient_id`, `quantity_per_batch`
4. `production_batches`: `id`, `recipe_id`, `batch_date`, `actual_yield`, `total_batch_cost`, `calculated_unit_cost`, `logged_by`
5. `products`: `id`, `name`, `category`, `configured_selling_price`

---

## 3. API Endpoints

- `POST /api/ingredients` & `PUT /api/ingredients/:id`: Manage raw material inventory & vendor pricing updates.
- `POST /api/recipes`: Create/edit baseline recipes with ingredient mappings.
- `POST /api/production-batches`:
  - Receives `recipe_id` and `actual_yield`.
  - Dynamically calculates `total_batch_cost` based on current ingredient costs.
  - Calculates `calculated_unit_cost = total_batch_cost / actual_yield`.
  - Saves the production run record and returns the itemized cost breakdown.
- `GET /api/reports/profitability`: Compares `calculated_unit_cost` vs `configured_selling_price` to output real-time profit margins.

---

## 4. UI Requirements

1. **Recipe Builder Component**:
   - Form to select raw materials and input batch quantities for a 50 kg flour bag / water run / popcorn pot.
   - Display calculated ingredient breakdown per single item based on expected batch yield.
2. **Daily Production Entry Screen**:
   - Simple modal for Production Managers to select a recipe and enter the actual daily yield.
   - Displays real-time total batch cost and per-unit production cost.
3. **Admin Price & Margin Dashboard**:
   - Table for updating ingredient prices.
   - Dynamic view comparing `Actual Unit Production Cost` vs `Selling Price` with calculated profit margins.

---

## 5. Instructions 
- Ensure proper unit conversion support (e.g., grams to kilograms) when calculating ingredient costs.
- Write unit tests for the yield-based unit cost calculation and dynamic price updating logic.
```
