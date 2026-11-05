# Mr. Emeka's Operational Requirements & Feedback: Executive Dashboard

## Executive Summary & Core Philosophy

The Executive Dashboard serves as the **"Quick Aggregated Morning Preview"** for top management and department executives. 

As specified by Mr. Emeka, top executives do not have the time to navigate through individual departmental sub-modules every day. Instead, the dashboard must act as a single-screen operational control center where leadership can log in every morning and instantly monitor factory-wide daily production, loading, distribution, sales, returns, and financial collections in real time.

---

## 1. Sequential Operational Lifecycle Pipeline Flow

The dashboard must present daily operational metrics sequentially across a strict 6+ stage lifecycle pipeline:

1. **Orders Placed (`Orders Placed by Sales Reps`)**
   * Total product quantities requested by sales reps prior to route departure.
2. **Quantity Produced (`Produced`)**
   * Total physical volume manufactured by the production team for the day.
3. **Quantity Distributed / Loaded (`Loaded`)**
   * Actual stock loaded into distribution trucks/vans (tracked independently from requested orders, as loading depends on available inventory).
4. **Factory Defects (`Factory Defects`)**
   * Products identified as defective or damaged within the factory before loading/distribution.
5. **Quantity Sold (`Sold`)**
   * Verified volume sold by sales reps on their routes.
6. **Closing Stock (`Closing Stock`)**
   * Unsold, good inventory brought back by sales reps at the end of the day. This stock remains in the vehicle to roll over into the next day's opening inventory.
7. **Damaged / Spoiled Goods (`Damaged`)**
   * Stock damaged in transit or on route (e.g., rodent damage, spoiled packaging) that cannot be sold and must be written off.
8. **Customer Returns (`Returns`)**
   * Expired or moldy items returned by supermarkets/retailers from prior sales (e.g., sold a week ago) that are replaced with fresh stock.

---

## 2. Terminology & Inventory Classification Rules

Mr. Emeka explicitly clarified the strict distinctions between stock categories to ensure accounting and inventory precision:

| Stock Term | Definition & Operational Rule |
| :--- | :--- |
| **Factory Defects** | Defective items identified during or immediately after production before being loaded out. |
| **Closing Stock** | Unsold, undamaged stock brought back by reps at the end of the day. Carried over as next day's opening stock. |
| **Damaged Goods** | Items ruined during transit/route (e.g., rat damage, broken seal) that are written off. |
| **Customer Returns** | Expired/bad items returned by supermarkets from past sales and replaced by reps with fresh stock. |

---

## 3. Product Line & Category Segmentation

The dashboard must maintain clear side-by-side separation across all manufacturing divisions:

* **Bread Category**: Jumbo Bread, Medium Loaf, Family Loaf, Round Bread, 6-in-1.
* **Water Category**: 18.9L Dispenser Bottles, Pure Water Packs.
* **Popcorn / Snack Category**: Standard Racks, various packet sizes.

> **Rule**: Cumulative figures must not be lumped into single company-wide totals. The UI must display product lines side-by-side (*"one side water, one side popcorn, one side bread"*). The system must also allow dynamic additions of sub-categories (e.g., Drinks, Snacks).

---

## 4. Cumulative Timeframe Tracking

* **Global Timeframe Selector**: Toggle between **Today**, **This Week**, **This Month**, and **This Year**.
* **Operating Week Standard**: "This Week" MUST run strictly from **Sunday to Saturday**.

---

## 5. Interactive Sales Rep Performance Drill-Downs

Clicking on any summary metric card (e.g., *"Cumulative Bread Sold"*) opens a slide-over modal detailing sales rep performance:

* `[ Sales Rep Name | Route / Vehicle | Requested Qty | Loaded Qty | Sold Qty | Closing Stock | Customer Returns | Damaged Qty | Cash / Transfer Revenue Collected | Running Account Balance ]`

---

## 6. Financial Reconciliation & Tender Tracking

* **Lump-Sum Payment Tracking**: Track cash, bank transfers, and POS collections as single aggregated transactions per rep (without forcing SKU-by-SKU cash breakdown).
* **Running Account Credit Balances**: Monitor cumulative rep credit balances updated dynamically daily.
* **Tender Split & Attention Alerts**: Flag uncollected debts, loading shortfalls, and damage spikes.
