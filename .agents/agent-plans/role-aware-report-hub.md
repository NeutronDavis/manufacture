# Plan: Role-Aware Report Hub & Database-Backed Reports

**Status**: Done (Phase 1 and Phase 2 delivered; customer-facing reports remain a later portal feature)

## Summary

Replace the current single synthetic `/Reports` page with a code-defined report catalogue, database-backed per-report query handlers, and role-aware visibility.

The current `/Reports` page is still powered by `MockAnalyticsService`, exposes the same three synthetic modules to every role that can reach it, and has no per-report permissions. This plan keeps the UI shell out of scope until the report catalogue is agreed, but defines the report engine, output contract, permissions, and the exact reports available from the current database.

The first delivery ships the core operational reports for Sales, Inventory, Production, and Executive users. HR/Payroll, Fleet/Logistics, Settings/RBAC, and secondary reports follow in later phases. SuperAdmin can see every report.

## Decisions Already Made

- **HR & Payroll Manager**: grants `Reports = View Only` and sees only HR/Payroll reports.
- **Customer**: no internal `/Reports` access. A customer-facing statement/order report can be added later.
- **Data source**: replace the mock report engine with database-backed queries.
- **Catalogue storage**: code-based `ReportCatalog`, not a database report-definition table.
- **Query implementation**: one typed query class per report, sharing a common base for filters, scope, paging, totals, and exports.
- **First delivery**: core Sales, Inventory, Production, and Executive reports.
- **UI design**: intentionally deferred. The engine and catalogue must work independently; the hub/runner visual design will be agreed separately.

## Current State

- `/Reports` uses `MockAnalyticsService.QueryReports`.
- `/api/reports/query` also uses `MockAnalyticsService` and has no authentication/RBAC.
- The current UI has three module tabs: Operational, Financial, Logistics & Fleet.
- The current engine has only three modules and three groupings; there is no named report catalogue.
- `Reports/Index` computes `CanView` but does not enforce it on `OnGet`.
- HrPayrollManager has no Reports permission and no sidebar link, but can deep-link to `/Reports`.
- Customer is redirected away from `/Reports`.
- Report columns and totals are keyed by module, not by report.
- Exports are CSV/PDF and currently cover `AllRows`.
- The SuperAdmin dashboard is already DB-backed; `/Reports` is not.
- `RouteSheetItems.Category` and the `ProductType` enum are legacy Bread/Water/Popcorn constraints; new report queries should use `ProductionCategories` through `Recipes`.

## Report Catalogue

Legend:
- **P1** = first delivery, core reports.
- **P2** = later module phase.
- **Later** = customer-facing or requires a design decision.
- **Caveat** = data exists but the metric is approximate or current-price based.

### Executive / Cross-Module

| Key | Report | Grain / dimensions | Key measures | Source | Phase |
|---|---|---|---|---|---|
| `exec-daily-funnel` | Daily Operating Funnel | Day × category × rep | Orders, produced, loaded, factory defects, sold, closing stock, damaged, returns, fill rate, sell-through | Route sheets, batches, defects, customer returns | P1 |
| `exec-product-line` | Product Line & SKU Performance | Period × category × SKU | Volume, revenue, share, orders, produced, loaded, sold, closing, returns, margin where cost is available | Route sheets, orders, recipes, production | P1 |
| `exec-rep-performance` | Sales Rep Performance & Cash Variance | Period × rep × route | Requested, loaded, sold, closing, damaged, revenue, collected, outstanding, cash variance | Route sheets, route financials | P1 |
| `exec-revenue-cash-debt` | Revenue-to-Cash-to-Debt Bridge | Period × rep/terminal/customer | Expected revenue, order invoiced value, cash/transfer/POS, outstanding, supermarket credit, salary advances | Orders, payments, route sheets, supermarket accounts, salary advances | P1 |
| `exec-unit-economics` | Unit Economics / Product Profitability | Period × recipe/category | Selling price, live material cost/unit, actual batch unit cost, margin amount/%, markup | Recipes, recipe ingredients, batches | P1 |
| `exec-management-pack` | Management Monthly Pack | Month × department/category | Production, sales, collections, inventory value, payroll, fleet cost, headcount | Cross-module | P2 |
| `exec-working-capital` | Working Capital View | Period | Inventory value, receivables, supermarket credit exposure, active salary advances, unpaid payroll | Inventory, orders, supermarket accounts, salary advances, payroll | P2 |
| `exec-integrity-pack` | Reconciliation / Integrity Exceptions | Entity × period | Computed-vs-detail differences, orphan records, stale balances | Multiple modules | P2 |
| `exec-alerts` | Exception / Alert Report | Alert × entity | Low stock, high receivables, damage spikes, loading shortfall, overdue service | Cross-module | P2 |

### Sales & POS

| Key | Report | Grain / dimensions | Key measures | Source | Phase |
|---|---|---|---|---|---|
| `sales-register` | Sales Register | Order × customer × rep × terminal | Units, gross, discount, net, paid, balance, status | Orders, order items, payments | P1 |
| `sales-product` | Product & Product-Line Sales | Date × recipe/category | Units, bonus units, line revenue, average price, share | Order items, recipes, production categories | P1 |
| `sales-customer-ledger` | Customer Sales Ledger / Statement | Customer × order | Orders, units, gross/net, paid, balance, returns/refunds | Orders, payments, customer returns | P1 |
| `sales-ar-ageing` | Accounts Receivable Ageing | Customer × age bucket | Invoiced, paid, balance, oldest days, exposure | Orders, payments, customers | P1 |
| `sales-collections` | Collections & Payment-Mode Register | Payment date × method × received-by | Payment count, amount, method share, settled orders | Order payments | P1 |
| `sales-route-performance` | Sales Rep Route Performance | Route date × rep × SKU/category | Requested, loaded, sold, closing, damaged, replacements, expected revenue, collected, outstanding, sell-through, damage % | Route sheets | P1 |
| `sales-loading-shortfall` | Ordered vs Loaded | Date × rep × SKU | Requested, loaded, short-load units/value, fill rate | Route sheets | P1 |
| `sales-closing-damage` | Closing Stock & Damage Write-Off | Date × rep × SKU/category | Closing qty/value, damaged qty/value, damage %, next-day opening | Route sheets | P1 |
| `sales-customer-returns` | Customer Returns & Replacements | Date × customer × rep × SKU × reason | Return qty, refund value, replacement qty, return rate | Customer returns | P1 |
| `sales-supermarket-credit` | Supermarket Credit Exposure | Account/customer × assigned rep | Outstanding credit, limit, utilization %, headroom, open-order balance, risk flag | Supermarket accounts, orders | P1 |
| `sales-bonus-discount` | Bonus & Discount Leakage | Product/customer/rep/month | Bonus units/value, discount amount/rate, realization vs list price | Order items, orders | P1 (caveat: no price history) |
| `sales-fulfillment` | Order Fulfillment Pipeline & Cycle Time | Order/date × status × terminal/rep | Order count/value, printed %, dispatched %, average hours between timestamps, backlog | Orders | P1 |
| `sales-terminal-productivity` | Terminal / Cashier Productivity | Terminal × cashier/employee × day | Orders, revenue, units, average basket, payment mix, cancellations | Orders, terminals, employees | P2 |
| `sales-preorders` | Pre-Order Book & Delivery Schedule | Customer × delivery date × SKU | Pre-order count, units, value, paid, balance, status, lead time | Orders, order items | P2 |
| `sales-price-realization` | Price Realization vs List / Agreed Price | Customer/account × SKU | Actual price, current list price, agreed price, variance, below-list lines | Orders, recipes, supermarket prices | P2 (caveat: current prices only) |

### Inventory

| Key | Report | Grain / dimensions | Key measures | Source | Phase |
|---|---|---|---|---|---|
| `inventory-stock-valuation` | Stock on Hand & Valuation | Item × category × supplier | Qty, package size, base qty, unit cost, stock value, last restocked | Inventory items | P1 |
| `inventory-low-stock` | Low-Stock / Reorder Worklist | Item × supplier | Stock, threshold, gap, suggested order, days since restock | Inventory items | P1 |
| `inventory-movements` | Stock Movement Ledger | Date × item × movement type × reference | Receipts, issues, running balance, movement count | Stock movements | P1 |
| `inventory-requisitions` | Requisition Register & Approval Aging | Request × department × requester × status | Requests, lines, requested/issued value, pending age, turnaround | Store requests, request items | P1 |
| `inventory-requisition-fill` | Requisition Fill / Issue Variance | Request line × item × department | Requested, issued, short-issue qty/%, conversions | Store request items | P1 |
| `inventory-department-consumption` | Departmental Material Consumption | Department × item/category × period | Issued qty, issued value, request count, top items | Store request items, inventory items | P1 |
| `inventory-receipts-issues` | Receipts vs Issues Summary | Period × item/category | Opening, receipts, issues, closing, net change | Stock movements | P1 |
| `inventory-slow-moving` | Slow-Moving / Non-Moving Stock | Item × category × supplier | On-hand value, last issue date, days since movement | Inventory, stock movements | P1 |
| `inventory-supplier-price` | Supplier Price List & Change History | Item × supplier × request | Current cost, previous/requested cost, delta, approval status | Inventory, price-change requests | P1 |
| `inventory-turnover` | Inventory Turnover & Days of Cover | Item × category × period | Issue value, average stock value, turnover, days of cover | Stock movements, inventory | P2 (caveat: current cost) |
| `inventory-density-coverage` | Unit Conversion / Density Coverage | Item × density record | Items with/without density, package-to-base factor, incompatible units | Inventory, measurement densities | P2 |
| `inventory-requisition-cycle` | Requisition Cycle-Time | Request → approval → issue | Request-to-approval, approval-to-issue, end-to-end days | Store requests | P2 |
| `inventory-theoretical-actual` | Theoretical vs Actual Material Usage | Recipe/item × period | Expected use, actual issued use, variance | Recipe ingredients, batches, stock issues | P2 (caveat: approximate) |

### Production

| Key | Report | Grain / dimensions | Key measures | Source | Phase |
|---|---|---|---|---|---|
| `production-daily` | Daily Production Summary | Date × shift × category × product type × recipe | Batches, target, actual, variance, yield %, flour bags, cost, unit cost | Production batches | P1 |
| `production-yield` | Batch Yield & Variance | Batch × recipe × shift × baker | Target, actual, variance, compliance %, output unit | Production batches | P1 |
| `production-batch-cost` | Batch Cost & Unit-Cost Variance | Batch/date × recipe × shift | Batch cost, actual/expected unit cost, cost variance | Production batches | P1 |
| `production-recipe-margin` | Recipe Profitability & Margin | Recipe × category × product type | Selling price, cost/unit, actual unit cost, margin, markup | Recipes, ingredients, batches | P1 |
| `production-defects` | Factory Defects Analysis | Date × batch × recipe × category × reason | Defect qty, rate vs actual production, top reasons | Production defects | P1 |
| `production-price-audit` | Ingredient Price Change Audit | Request × item × category | Previous/requested/current cost, delta, status, turnaround | Ingredient price change requests | P1 |
| `production-volume` | Production Volume by Category / SKU | Date/month × category × product type × SKU | Actual output, target, yield, share | Production batches | P1 |
| `production-shift-baker` | Shift & Baker Performance | Shift × baker/team × period | Batches, output, yield %, defects, cost/unit, cancellations | Production batches, defects | P1 |
| `production-batch-status` | Batch Status / Aging | Status × batch age | Counts, completion %, age of open batches | Production batches | P2 |
| `production-bom-cost` | Recipe BOM Cost Breakdown | Recipe × ingredient × supplier/category | Qty/batch, qty/unit, unit cost, line cost, share | Recipe ingredients, inventory | P2 |
| `production-demand` | Demand vs Production | Recipe/SKU × date | Pre-order demand, target order qty, produced, gap | Orders, production batches | P2 (caveat: link fields often blank) |
| `production-to-sales` | Production-to-Sales Fulfillment | Recipe/category × period | Produced, loaded, sold, closing, damage, conversion rates | Batches, route sheets | P2 |

### HR / Payroll

| Key | Report | Grain / dimensions | Key measures | Source | Phase |
|---|---|---|---|---|---|
| `hr-headcount` | Employee Master & Headcount | Department × role × status × employee | Headcount, status, salary total, average salary, tenure | Employees, departments, job roles | P2 |
| `hr-hires-terminations` | New Hires, Terminations & Attrition | Month × department × role | Hires, terminations, net headcount, attrition %, reasons | Employees | P2 |
| `payroll-register` | Payroll Register | Employee × month/year × run | Base, bonus, allowances, gross, deductions, net, status | Payroll records, runs | P2 |
| `payroll-summary` | Payroll Summary by Department/Role | Period × department × role | Headcount, gross, deductions, net, average net | Payroll records, employees | P2 |
| `payroll-deductions` | Earnings & Deductions Breakdown | Deduction type × period × department | Amount and share by tax/pension/health/loan/disciplinary | Payroll deduction lines | P2 |
| `payroll-advances` | Salary Advance / Loan Register | Employee × advance × status | Principal, monthly deduction, repaid, balance, age | Salary advances | P2 |
| `payroll-reconciliation` | Payroll Run Reconciliation | Run × period | Run totals vs records, employee count, pending/paid/on-hold, variance | Payroll runs, records | P2 |
| `payroll-bank-schedule` | Bank Payment Schedule | Employee × period | Net salary, bank, account, payment status/date | Payroll records, employees | P2 |
| `payroll-compliance` | Employee Data Compliance | Employee × field completeness | Missing bank/guarantor/contact/email, duplicate staff codes | Employees | P2 |
| `payroll-cost-per-unit` | Payroll Cost per Production Unit | Month × department/company | Payroll cost, units produced, cost per unit | Payroll records, production batches | P2 (caveat: no labour allocation) |

### Fleet / Logistics

| Key | Report | Grain / dimensions | Key measures | Source | Phase |
|---|---|---|---|---|---|
| `fleet-vehicles` | Vehicle Register & Status | Vehicle × type × function × driver | Count, status, odometer, last service, capacity | Vehicles | P2 |
| `fleet-fuel` | Fuel Purchase & Cost | Date × vehicle × driver × fuel type × station | Litres, price/litre, total cost, purchase count | Fuel logs, vehicles | P2 |
| `fleet-maintenance` | Maintenance Cost & Service History | Vehicle × service date × vendor | Cost, service count, invoice, cost/vehicle | Maintenance logs | P2 |
| `fleet-service-due` | Service Due / Last-Service Aging | Vehicle | Days since last service, overdue flag | Vehicles, maintenance logs | P2 |
| `fleet-weekly-cost` | Weekly Fleet Running Cost | Week × vehicle × driver | Fuel, maintenance, incidentals, total, share | Fuel, maintenance, weekly expenses | P2 |
| `fleet-charter` | Charter Revenue & Profitability | Booking × vehicle × driver × route | Gross fee, allowances, tolls, net revenue, margin % | Charter bookings, trip legs | P2 |
| `fleet-utilization` | Driver / Vehicle Utilization | Vehicle × driver × rep × period | Route-sheet count, trips, units sold, fuel, maintenance | Route sheets, fleet logs | P2 |
| `fleet-cost-per-unit` | Fleet Cost per Unit Sold | Vehicle × week/month | Total fleet cost / units sold, fuel/unit, maintenance/unit | Fleet logs, route sheets | P2 (caveat: proxy allocation) |
| `fleet-trip-incidentals` | Trip Incidentals Report | Booking × driver × vehicle × date | Driver allowance, security, tolls, total, % of gross | Charter bookings | P2 |
| `fleet-fuel-comparison` | Fuel Type / Station Comparison | Fuel type × station × period | Min/max/avg price, volume, cost share | Fuel logs | P2 |

### Settings / RBAC / Admin

| Key | Report | Grain / dimensions | Key measures | Source | Phase |
|---|---|---|---|---|---|
| `admin-user-access` | User Access Register | User × role × employee/customer | Role, active status, last login, linked record, terminal, must-change-password | Users, roles, employees, customers | P2 |
| `admin-role-distribution` | Role Distribution & Access Coverage | Role | User count, active users, modules accessible, full-access modules | Roles, users, RBAC | P2 |
| `admin-rbac-matrix` | RBAC Matrix / Effective Permissions | Role × module × level | Capability flags and coverage | Rbac modules/levels/permissions | P2 |
| `admin-permission-audit` | Permission Change Audit | Date × role × module × user | Changes, old→new level, trend | Rbac permission audits | P2 |
| `admin-dormant-users` | Dormant / Never-Logged-In Users | User × role | Never logged in, days since login, inactive accounts | Users | P2 |
| `admin-login-coverage` | Employee-to-Login Coverage | Employee × department | Employees with/without login, orphan links, customer users | Employees, users | P2 |
| `admin-master-data` | Master Data Register | Entity × status | Departments, jobs, categories, product types, shifts, densities, bonus rules; active vs referenced | Settings tables | P2 |
| `admin-configuration` | Configuration Register | Setting | Treasury/payroll percentages, last updated by/at | Treasury/payroll settings | P2 |
| `admin-audit-extract` | General Audit Extract | Date × user × action × entity | Event count, changed entities, IP, old/new values | Audit logs | P2 |
| `admin-data-integrity` | Data Integrity Exceptions | Entity/relationship | Users without roles, roles without permissions, employees without department/job role, duplicates | Multiple tables | P2 |

### Customer Portal (Later / Not Internal Reports)

| Key | Report | Grain / dimensions | Key measures | Source | Phase |
|---|---|---|---|---|---|
| `customer-orders` | My Order History | Customer × order | Lines, qty, bonus, price, total, paid, balance, status | Orders, order items | Later |
| `customer-statement` | My Statement / AR Ageing | Customer × order | Invoiced, paid, balance, returns/refunds, ageing | Orders, payments, returns | Later |
| `customer-preorders` | My Pre-Order Schedule | Customer × delivery date × SKU | Pre-order qty/value, paid, balance, lead time | Orders | Later |
| `customer-returns` | My Returns & Replacements | Customer × return date × product | Return qty, refund, replacement, rate | Customer returns | Later |
| `customer-credit` | My Credit Position | Customer/account | Limit, outstanding, utilization, headroom, terms | Supermarket accounts | Later |
| `customer-prices` | My Agreed Price List | Customer/account × SKU | Agreed price, list price, difference | Supermarket prices | Later |

## Role → Report Access

| Role | Report access | Scope |
|---|---|---|
| `SuperAdmin` | Every report in the catalogue | All data, all modules |
| `ProductionManager` | Executive operational, Production, Inventory request/movement, Sales demand/read-only, recipe/price reports | No Financial, Logistics/Fleet, HR/Payroll, Settings/RBAC; own module scope only |
| `StoreManager` | Inventory stock, movements, requisitions, suppliers/pricing, density | No Production, Sales, Financial, Fleet, HR, Settings |
| `SalesRep` | Sales register, product sales, own customers, AR/collections, route performance, closing stock, returns, pre-orders | Only own rep/customer/route records |
| `HrPayrollManager` | HR/Payroll family only | All HR/payroll data, no operational modules |
| `Customer` | No internal `/Reports`; later customer statement page | Own records only |

RBAC changes:
- Grant `HrPayrollManager` → `Reports = View Only` in `RbacDefaults` and seed data.
- Keep `Customer` → `Reports = None`.
- SuperAdmin remains Full Access.
- Report visibility is additionally filtered by the code catalogue, not only the `Reports` module permission.

## Implementation Architecture

### 1. Report catalogue

Add a code-based `ReportCatalog` with a `ReportDefinition` for every report:

- `Key`, `Title`, `Description`, `Group`, `IconClass`
- `AllowedRoles` and `RequiredModules`
- `Scope` (`Global`, `OwnSalesRep`, `OwnCustomer`, `OwnDepartment`)
- `DefaultTimeframe`, `DefaultGroupBy`
- `ColumnKeys` or `ReportColumnDefinition` list
- `SupportsCsv`, `SupportsPdf`, `SupportsCharts`
- `HandlerType` / query key

The catalogue must be the single source of truth for the hub, permissions, navigation, and exports.

### 2. Shared report contracts

- `ReportFilterDto`: date range, category IDs, product type IDs, recipe IDs, rep IDs, customer IDs, terminal IDs, status, search, page, page size, group by.
- `ReportQueryContext`: role, user ID, linked SalesRepId, linked CustomerId, allowed scope, filters.
- `ReportColumnDefinition`: key, label, data type, alignment, format, total mode.
- `ReportResult`: columns, rows, totals, metadata, charts, row count, generated timestamp.
- `IReportHandler`: `ReportResult Execute(ReportQueryContext context)`.
- `ReportHandlerBase`: shared filter application, pagination, totals, date normalization, and export metadata.

### 3. One handler per report

Implement one typed query class per report, for example:

- `DailyOperatingFunnelReport`
- `SalesRegisterReport`
- `ArAgeingReport`
- `StockValuationReport`
- `RequisitionRegisterReport`
- `ProductionYieldReport`
- `BatchCostReport`
- `RecipeProfitabilityReport`

Handlers query `ManufactureDbContext` directly. They must not call `MockAnalyticsService`.

### 4. Authorization and data scope

- `ReportAuthorizationService` filters the catalogue by role.
- `SuperAdmin` bypasses the catalogue filter.
- `SalesRep` queries must add `SalesRepId = currentRepId` or linked customer/order/route predicates.
- `HrPayrollManager` only receives HR/Payroll report keys.
- `StoreManager` cannot receive production/sales/financial/fleet keys even if a module row exists.
- All report execution and exports must enforce the same authorization; UI hiding is not sufficient.
- Fix the current `/Reports` `OnGet` gap and protect `/api/reports` with the same context.

### 5. Hub and runner

- `/Reports` becomes a role-filtered hub listing only authorized report definitions.
- Reports are grouped by module/family with search and description.
- Selecting a report opens a generic runner keyed by report key.
- The runner renders the report's own filters, table, optional charts, totals, and CSV/PDF actions.
- The visual design is intentionally not finalized in this plan. The engine, catalogue, and output contract are the deliverables; the hub/runner design will be agreed separately.

### 6. Exports

- Reuse `ReportExportService` and `ReportPdfService` concepts, but make them report-definition-driven, not module-driven.
- File names must include the report key.
- CSV/PDF must use the same columns, totals, and filters as the screen result.

## Implementation Steps

### Phase 1: Report Foundation

1. Add `ReportDefinition`, `ReportColumnDefinition`, `ReportFilterDto`, `ReportQueryContext`, `ReportResult`, and `IReportHandler` to `Manufacture.Library`.
2. Add `ReportCatalog` with the Phase 1 report definitions.
3. Add `ReportAuthorizationService` and role/data-scope rules.
4. Add `ReportQueryService` with handler resolution, authorization, filter normalization, paging, and totals.
5. Add generic CSV/PDF adapters keyed by report definition.
6. Grant HrPayrollManager `Reports = View Only` in `RbacDefaults` and seed data.
7. Add report-level authorization tests for all six roles.

### Phase 2: Core Report Handlers

8. Executive: Daily Operating Funnel, Product Line & SKU Performance, Sales Rep Performance, Revenue-to-Cash-to-Debt Bridge, Unit Economics.
9. Sales: Sales Register, AR Ageing, Collections & Tender, Route Performance, Ordered vs Loaded, Closing Stock & Damage, Customer Returns, Supermarket Credit Exposure, Bonus & Discount, Fulfillment.
10. Inventory: Stock on Hand & Valuation, Low-Stock/Reorder, Stock Movement Ledger, Requisition Register/Aging, Requisition Fill, Departmental Consumption, Receipts vs Issues, Slow-Moving, Supplier Price/History.
11. Production: Daily Production Summary, Batch Yield & Variance, Batch Cost, Recipe Profitability, Factory Defects, Ingredient Price Change Audit, Production Volume, Shift/Baker Performance.
12. Add database-backed integration tests for each handler group.

### Phase 3: Hub and Runner UI

13. Build `/Reports` hub using `ReportCatalog` and `ReportAuthorizationService`.
14. Build the generic report runner keyed by report key.
15. Add module/family grouping, search, filters, table, charts, totals, CSV/PDF.
16. Defer visual styling decisions to a separate design review.

### Phase 4: HR, Fleet, Admin, and Secondary Reports

17. Add HR/Payroll handlers and catalogue entries.
18. Add Fleet/Logistics handlers and catalogue entries.
19. Add Settings/RBAC/Admin handlers and catalogue entries.
20. Add remaining Sales/Inventory/Production reports.
21. Add customer statement/order reports only if the customer portal is extended later.

## Relevant Files & Symbols

- `Manufacture.Web/Pages/Reports/Index.cshtml`, `Index.cshtml.cs`, `_Table.cshtml`, `_Charts.cshtml`: current page to replace/retire.
- `Manufacture.Web/Services/MockAnalyticsService.cs`: current mock engine; not used by the new report handlers.
- `Manufacture.Web/Services/ReportExportService.cs`, `ReportPdfService.cs`, `SimplePdfWriter.cs`, `ReportTotals.cs`: export patterns to adapt.
- `Manufacture.Library/Models/DTOs/AnalyticsDtos.cs`: existing report DTOs and `ReportColumnCatalog`; new generic contracts belong in a new report DTO file.
- `Manufacture.Web/Services/SuperAdminDashboardService.cs`, `SuperAdminDashboardAggregator.cs`: DB fact patterns to reuse.
- `Manufacture.Library/Data/ManufactureDbContext.cs`: EF queries for report handlers.
- `Manufacture.Web/Services/RbacCache.cs`, `RbacDefaults.cs`, `RbacService.cs`: report visibility and HR access.
- `scripts/seed-database.sql`: HR Reports permission seed.
- `Manufacture.Web/Pages/Shared/_Layout.cshtml`: Reports navigation for HR.
- `Manufacture.Web/Api/AnalyticsApi.cs`: protect or replace `/api/reports/query`.

## Verification Plan

1. **Automated**
   - Build:
     `dotnet build Manufacture.slnx -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\`
   - Tests:
     `dotnet test Manufacture.Tests/Manufacture.Tests.csproj --no-build -p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\`
   - New tests for catalogue visibility, role scoping, date filters, totals, and export output.

2. **Role access matrix**
   - SuperAdmin sees all Phase 1 reports.
   - ProductionManager sees production/inventory-request/sales-read reports; no financial/fleet/HR.
   - StoreManager sees inventory-only reports.
   - SalesRep sees only own orders/route/customers/collections.
   - HrPayrollManager sees only HR/Payroll reports and can export them.
   - Customer cannot open `/Reports`; direct access redirects.
   - API report execution enforces the same context.

3. **Data correctness**
   - Compare each core report to direct SQL for a known date/range.
   - Verify closing stock and customer returns stay separate.
   - Verify produced, loaded, sold, damaged, and returns use their correct source columns.
   - Verify route-sheet reports use `Recipes -> ProductionProductTypes -> ProductionCategories` for dynamic categories.
   - Verify export totals match on-screen totals.

## Context & Decisions Log

- **2026-11-14**: User requested a discussion of `/Reports`, noted all application users appear to have the report model, and asked for the exact reports available from all modules. SuperAdmin must have all reports.
- **2026-11-14**: User selected HR & Payroll reports only for HrPayrollManager (Reports = View Only).
- **2026-11-14**: User selected no internal Reports access for Customer; customer statement later.
- **2026-11-14**: User selected database-backed reports and a report hub with a generic runner.
- **2026-11-14**: User selected per-report query classes, a code-based catalogue, and core operational reports first.
- **2026-11-14**: User requested that the current Reports page design be ignored and the report-building approach be decided together. UI design remains deferred.

## Key Decisions & Trade-offs

- **Per-report handlers over a generic SQL builder**: Safer joins, explicit permissions, and testable report logic. More classes, but each report is auditable.
- **Code catalogue**: No schema change, easy review, and no admin report-definition UI. Adding a report is a deployment change.
- **Database-backed**: Reports will match the operational tables and the SuperAdmin dashboard. The mock engine will no longer be user-facing.
- **Role-specific scope**: Visibility is filtered by report definition and data scope, not just the single `Reports` RBAC module.
- **Core first**: Phase 1 focuses on reports with the highest operational value and existing data. HR, Fleet, Admin, and secondary reports follow.
- **Known data limits**: No accounting ledger, procurement cycle, attendance, finished-goods ledger, or historical price snapshot. Reports must label current-price or approximate metrics.
