# Things to fix in this application 

### Dashboard
- Sequential Operational Lifecycle Pipeline Flow
    The dashboard must present daily operational metrics sequentially across a strict 6+ stage lifecycle pipeline  :

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

    - Note:make sure the KPI are functional
    1. when Orders Placed cliked, it should open at the side panel showing the oder placed by Sales Rep, i.e group them by sales rep

    2. when Produced clicked, it should open at the side panel showing the dayly products produced by thier category, (`Bread`, `Water`,`Snack`)

    3. when Loaded clicked, it should open at the side panel showing the dayly products loaded into the  distribution trucks/vans, i.e group them by vehicle category, (`Bread`, `Water`,`Snack`) and the vehicle they where loaded into

    4. when Factory Defects is clicked, it should open at the side panel showing the dayly products identified as defective or damaged within the factory before loading/distribution, i.e group them by product category, (`Bread`, `Water`,`Snack`) 

    5. When Sold is cliked, it should open at the side panel showing the dayly products sold by sales rep, i.e group them by sales rep and product category, (`Bread`, `Water`,`Snack`) 

    6. When Closing Stock is cliked, it should open at the side panel showing the dayly products brought back by sales rep, i.e group them by sales rep and product category, (`Bread`, `Water`,`Snack`) 

    7. When Damaged is cliked , it should open at the side panel showing the dayly products damaged in transit or on route, i.e group them by product category, (`Bread`, `Water`,`Snack`)

    8. When Returns is cliked, it should open at the side panel showing the dayly products returned by supermarkets/retailers from prior sales, i.e group them by product category, (`Bread`, `Water`,`Snack`

