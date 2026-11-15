## bilding Action

## Sales & POS module
`Note: i want this module to be unlinked form the Mock Data Services and be link to the databse which means tables will be created pushed and some how purpulated with seed data for the entire table created for this module`

1. correct the `Client & Vendors` to just `Customers` also in the /Sales/Customers page correct `Clients, Supermarkets & Vendor Directory` to `Clients, Supermarkets Directory` 
2. The `Member / Account Code` field in the `/Sales/Customers/Create` page littrally the Register Client form should be generated brandomly by the system also rename the page tittle form `Register Client / Vendor Account` to `Register Client Account`
3. the `Assigned Sales Rep / Cashier` in the `/Sales/Terminals/Create` page which is the Register New POS Terminal form should come from the HR module where thier department is `Sales`

`Note: The department in the HR should have a page in the setting where they can set Department and Roles seperately, the both should all come from the databse and note that the Designation / Role field in the /HR/Employees/Create page should be a dropdown of roles, they should be a table for that and make sure to link it up in the HR modules tables and employee creation form in /HR/Employees/Create,  `

4. In the `/Sales/Dispatch` page at the right side thy should be an action to delete it incase it was mistakenly created 
` Note: "Gross" vs. "Net" Revenue Labeling: The initial value calculated when stock is loaded must be labeled Gross Sales/Revenue, not "Net" Net revenue is only determined at end-of-day closing after deductions for customer returns, damages, and bad products`

5. In the `/Sales/Orders/Create` page at the `Product Basket & Quantities` section include `Bonus Quantity` column it's value should come from the settings where they will be set for a particuler quantity a customer order a specified amount of that item will be added as a bonus quantity. create that at the settings and link it up it the `/Sales/Orders/Create` page and every other model that needs it

- I need a page at the setting to set the amount of bonus quantity to be sent out for a particuler product, the form should have the selected product, the thrashold of the order to be taken by a costomer befor a bonus can be given, then the bonus quantity 

- they should also be a page in the setting to set and managethe department and the jobroles in the system which are used by the HR

### Take Note of 6 to 11
6. Renaming to "Sales Reconciliation": The end-of-day closing process should be labeled Sales Reconciliation (rather than "Route Reconciliation")
7. Closing Stock: Good, unsold loaves brought back by the rep at the end of the day
. These are not treated as losses; they remain with the rep and automatically roll over as the next morning's Opening Stock

8. Customer Returns (Mold / Expired Bread): Bread sold previously (e.g., a week earlier to a supermarket) that went bad within warranty limits
. The rep collects the bad bread and replaces it with fresh loaves from their current truck stock
. The system must account for this so the rep isn't penalized for a "missing" fresh loaf

9. Damaged Goods: Loaves spoiled or broken on the route (e.g., damaged during transit or rat-eaten overnight) that could not be sold
. These are returned to the factory for inventory write-off

10. Factory Defects: Defective items identified before or during loading at the factory (tracked under production yield rather than sales rep losses)

11. Order-Tied Debt Tracking: Every sales distribution order must be tied directly to a specific Sales Rep and Order Date

### Open vs. Closed Order Lifecycle:
- Daily orders remain in an open status (e.g., Pending) until the full monetary value of that specific order is paid back

- A newer order (e.g., Day 2) can be fully settled and marked Completed while an older order (e.g., Day 1) remains open awaiting customer debt recovery

- When a rep remits collected customer debts days or weeks later, staff go back and apply the payment to the historical date and ID of the original order

- Audit Trail Across Staff Changes: If a sales rep is reassigned, replaced, or dismissed, the historical ledger and debt records tied to their POS ID must remain permanent and accessible for auditing


# THink about the implimentattion and crearte a plan for this 

## There is a problem
- in the `/Sales/Orders/Create` at the `Product Basket & Quantities` section the items listed there are not comming from the inventry, the inventory consit of all the raw material, fuel and utilities.
- we where supose to do the Production modules be for Salse & POS so to get the product that can be sold and odered 

## but go on with the implimentation

### correction
- i want the Users table to include CustomerId and the /UserManagement/Create page shpould ask a question if the registerd person is a customer , it yes display a dropdown for the user to select the registerd customer

### pre order
- Note: pre order sould be an order created without payment a salse rep will pick it up  later at the `Order & Invoices` page

- the `Pre-Order Schedule` page  should be a seperate page newlly created to only display order were the  `CustomerId` is the signed in customer, this means that the new page will only be available for `Customer` users.

- If the signed in user is a Customer, then in `Place pre order` page the `Order Flow Type` field should set to option with `Supermarket / Partner Order`, the `Customer / Account Name` dropdown field should select the customers that signs in and the both `Order Flow Type` and `Customer / Account Name` fields should be disabled

- hide `POS Register Terminal, Sales Rep / Order Owner` fields

- the `Payment Method` field should be `Customer Credit` option and the field should be disabled 

- make sure the dashboard showcase the  Daily Standing Pre-Order

i dont like the fact that Customers is refared as Vendor
- crete a page to manage the Role table which is for platform access then put the buton to the role page in  `/UserManagement` page 

- if a user signin as a Customer first: in the /Dashboard page at the Daily Standing Pre-Orders section should only list out orderwhere CustomerId is the same as the CustomerId of the sign in user
- currently the /Salse/Order/Create page is not working for a Customer user check the reason

## Update for SalesRep user
- the sales person /Dashboard page is reading the wrong `My Sales Today`, `Completed Tickets` KPI
- in the /Sales/Orders/Create page, the `POS Register Terminal` field shoul only select the POS where AssignedEmployeeId is the logged in Sales Rep employee

- in  the /Sales/Orders page only display order where the SalesRepId is the same with the loged in Sales rep and order that has no seles rep assigned. remove the Pre-Orders Only button on the top page 
- in the /Recipes/Details page remove `Log Daily Output` button

- In the /Recipes page hide the `Price & Margin Admin` and `Build New Recipe` buttons for a SalesRep user

- In the /Sales/Orders/Create page, make sure that the signed in Sales Rep is selected in the `Sales Rep / Order Owner` field and the field should be disabled

- In the /Sales/Dispatch page, only the `Daily Rep Route Sheets ` informations created by the signed in Sales Rep should be displayed in the page 

- In the /Sales/Dispatch page, at the `Dispatch Staff Gate Pass Sign-Off` section the field `Dispatcher Name` should be a dropdown list of people in the Logistics Department showing their role in a braket

- In the /Sales/Reconciliation page, only the `Daily Rep Route Sheets ` informations created by the signed in Sales Rep should be displayed in the page for Sales Reconciliation & Payment Drawer

- remove the /Sales/Terminals page for Sales Rep users

## General Update
- In the /Sales page, i need only 3 KPS in the , remove the `Total Revenue Tracked` KPI
- In the /Sales/Supermarkets page, at the `Supermarkets & Depots` section the listed supermarket should not have any action buttons in it. Make the cards clikable to view at the right side 


## Go through al this changes carefully and impliment


### Production module 
- i want to work on the Production module 
1. I want to go through the project understand it and unlink the whole production moduel form the mock data service and link them to the databse

## On /Recipes/Create page 

- Create a production settings module in the settings where they will be a Category settings, Note : there are `Bread, Water and Snacks` category so far.
Also create a product type and it's Baseline will be in a one table taking categoryId as a FK.

- for now here are the product type available:
1. Bread: Baseline: 1 Bag of Flour (50 kg)
   Output Unit Noun:loaves

2. Water: Baseline: 1 Production Run
   Output Unit Noun:bottles

3. Popcorn: Baseline: 1 Cooking Pot (~2 kg corn)
   Output Unit Noun:bags

- mapp them to their category, Note the Product type table has 3 fields: 
1. Product type name
2. Baseline
3. Output Unit Noun

- After that go to the /Recipes/Create page, at the `Baseline Production Unit & Expected Yield` section purpulate the `Product Type` field with the product type then when anyone is selected it purpulate the baseling in the `Baseline Unit` field and the `Output Unit Noun` field with the output unit noun. Note: the `Baseline Unit` and `Output Unit Noun` field should be disabled textbox input

- Remove the `Handling & Label Cost per Unit (₦)` and `Labor & Overhead per Unit (₦)` fields and remove the fields too from the calculation

- At the `Raw Ingredient Formula (per baseline batch)` section, the listed RAW MATERIAL should list out all the inventory items except those with category of `Fuel` and `Utilities`. Only display the product name.

- Note: Inventory purchases raw materials in bulk by weight/mass (e.g., 50 kg bags of flour, 12.5 kg containers of softener, or cartons)
. However, on the recipe form, bakers input micro-quantities (e.g., 40 g or 20 g) or volumetric measures (e.g., 2 L cups of liquid flavor or milk).

The system must execute behind-the-scenes conversion formulas based on ingredient density parameters (obtained from suppliers) to automatically calculate the exact cost deduction

-  Note: The form calculates unit cost by taking the total sum of all ingredients mixed for that baseline (e.g., 1 bag of 50 kg flour + sugar + yeast + packaging) and dividing it by the Expected Yield (e.g., dividing by 60 for Jumbo loaves) to output the exact production cost per loaf

## Update

- in the /Production/Batches page, the Batches filters should be based on category firm the databse
- remove the Log Daily Output button and it's popup `Daily Production Entry & Unit Cost Snapper` form
- At default, all the batch production should be listed out
- in the /Production/Batches/Create page , ` the batch production is not linked to any customers order, the production guys runs a production by shift base on the availability of product remaining and the bae on how many ordedr comming in for that particuler product so first:`
1. remove the `Linked Customer Order / Production Demand` field
2. the will be a Production Shift setting at the setting, so create a page for that and link it to the `Production Shift` field
3. remove  the Line listed on top of the form, `All Line, Bread(50kg Bag) `etc
4. remove `Demand from Rep Orders` close to `Target Quantity` field
5. rebuld the batch production record form, make it neat and straight



## Update 
- in the /Inventory/Items/Create page , i noticed the field `Density (kg/L)` with a discrption under it `Required for liquids that need mass-to-volume conversion.`. The thing here is that there are some raw materials that are stored in Kg in the inventory e.g `Corn` to make popcorn, the kichen guys mesure it in liters out of the inventory, so now we need a conversion engin that will help us convert litters to kg so we can deduct it from the inventory when making Requisition of Raw Materials in /Inventory/Requests/Create page


- in the /Inventory/Requests/Create page, at the `` section, i want the material selection to be 
|RAW MATERIAL|QTY| UNIT|Action|
|---|---|---|---|
- the Unit should be a dropdown of units just as it is at `Raw Ingredient Formula (per baseline batch)` section in /Recipes/Create page 

- Think about this and give me your surgesstion on how to solve it. Ask question for clearity

- the best option so fat for this measurment thing is is to crate a page in setting for measurment settings. it will have some field a text box to raw material name, then another text box to save the density for that raw material using this instruction
Don’t rely on a textbook figure, because bulk density depends on the product and how it’s packed. Measure it once:

1. Fill a 1 L container with the corn, level it off, and don’t shake it down.
2. Weigh the contents and subtract the container’s weight.
3. Repeat 2 or 3 times and take the average.

- then at /Inventory/Items/Create page, which is the inventory form. then `Density` field should be a dropdown of the densities set at the measurment settings page showing the raw material name(density) and it should have a default value 0 for products that will not be requested in liters 

- during request in the /Inventory/Requests/Create page it should calculate it and get trady to deduct from inventory when the request is approved 

- The /Settings page is not well organised, I want the page to show me cards of the settings page as a quick link page , each card should tell what the page is for

- in the /Inventory/Items/Details page, at `Floor Requisition History` first change it to `Requisition History`. The history is not clear not discriptive 
- Note: at the /Inventory/Requests/Create page, the user is requesting for a certin amount of the raw material not either in kg or litters, the `Requisition History` section should be able to give a discriptive data just the way it is but the `Quantity` column should display the actual quantity that was requested 
- the KPI in /Inventory/Items/Details page is not acurate  especially the `Stock on Hand` and the `Total Stock Value` 
-  the `Warehouse Item Specifications` section in the /Inventory/Items/Details page should also contain the information about the density value which will be used to request the material, if the material dosnt have a density value assigned to it then put down a text to say that the product is will not be requestted using litters as the measurment

- work on the `Used in Formulations` section too, make sure it's displaying the right information

## Store Manger user
1. First, the /Dashboard page, at the `Store Issue Requisitions Awaiting Approval` section the review button is not taking the user to /Inventory/Requests page , Note that the Store manager and the Admin only should see the `Approve &Issue Stock` anf the `Reject` button in the /Inventory/Requests page 

- the Store manager user should not have access to the /Inventory/Suppliers page and should not have access to the Production module as a whole
## General update
- In the /Inventory/Movements page, Incluede a data filter. At the `Movement History` section the `Quantity` column is not showing the actual Quantity that was requested which makes it not discriptive, ill also surgest it should have a view action 

## Production Manager User
- include `Defects Logged `section in the /Production/Batches/Details page to log the total number of defected product for that batch
- the Production Manager User should see it in the `Defects Logged` KPI in the /Dashboard page
- remove the `Material Cost Today` KPI


- at the /Inventory/Items/Details page, hide the `Quick Restock` and `Esit Item` button including the `Stock Replenishment` section form the Production Manager User
- in the /Sales/Orders/Details page, the Production Manager User should not see thre `Receipt/ Loading Slip` button
- the Production Manager User should not have access to the fleet module


- In the /Dashboard page, the KPI are not making sence, it's only for bread the only one that makes sence is the `Batches Mixed Today` that is becasue the factory produces not just bread but snaks included. Also at the `Today's Oven Batch Log` section the table dosn't have a review batch action button
- i want the dashboard to read relevant data for the Production Manager User
- the /Production/Pricing page, they should be a workflow such that then a new item price is enterd it dosnt save imidiately but need to be approved by the superAdmin and they shoul be price change history to track the item price taht was changed
- in the /Inventory page the Production Manager User should not see the `Add Stock Item` button and in the table the Production Manager User should not see the `Edit` and `Restock` actions

- The Production Manager User should not have access to the `/Sales/Orders/Create` page in the `Sales & POS` moduel
- The Production Manager User should not have access to the `New POS Order` button in the /Sales/Orders page 
- in the /Sales/Orders page, the Production Manager User should not see the the Slip action in the table
- in the /Sales/Orders/Details page, the Production Manager User should not see the `Sales Rep Assignmen` and `Settlement` sections

## General update
- in the /Logistics/FuelLogs page, include the filters like date filter search filter and any one posible

## Super Admin Dashboard update
- before doing anything, go through the Super Admin Dashboard, draft out the difrent databse tables the is responsible to supply data to the Super Admin Dashboard dashbard also, i know it looks messy, form the looks of thing you can make any adjustment to the user interface bring in and removing 

- there are 8-stage sequence KPI on the Super Admin Dashboard :
`Orders Placed → Produced → Loaded → Factory Defects → Sold → Closing Stock → Damaged → Returns`

- Note:
    1. Factory Defects (placed directly after Loaded): `Items damaged during production or spotted before leaving the factory, tracked under production analysis`

    2. Closing Stock (replaces the original "Return" label): `Good, unsold products a sales rep brings back at the end of the day. These remain in the reps custody and automatically become the next day's opening stock`

    3. Damaged: `Products loaded onto the truck in good condition that later became spoiled or unusable before sale (e.g., eaten by rats overnight) and are returned for write-off`

    4. Returns (Customer Returns): `Previously sold goods (e.g., bread sold to a supermarket a week or two earlier) that went bad within return limits; the rep collects the bad bread and replaces it with fresh stock from their current load`

- Clicking on summary cards (such as Orders Placed, Closing Stock/Returns, or product-line sales totals) must open a side bar, showing the data grouped by individual sales reps rather than only aggregated totals table

-  at the `Product Line Performance • Today` section in the /Dahboard i noticed that the product category `Bread, Snacks, Water` seems to be hardcoded. What if the client deside to add another category at /Settings/Production page

- Let's discause about the /Reports page, all users that have access to this application seems to have the Report model 
- and all those users have thier respected report to generate base on the models they have in thier side bar, but first let know the exact reports we can generate so far out of all the module we have, Note: the super admin must have access to generate all the report
- ill go with option 1, note ignore the entire design of the report page, let's make the desistion together on how to build the report

## Fleet module update
- concerning the /Fleet/Rates page, the client droped a feedback saying ` reminder that we did not approve VIP booking prices in the fleet module. Prices are currently generalized by state or city, so we will instead generate prices based on client requests, since not all routes within a region share a unified distance.` 
- this means that any new cganges that will be made will affect the `/Fleet/Rates/New `page, and `/Fleet/Charter/New` page
- base on this ill like use to thing on how to impliment this  for the client

- in /Fleet/Quotes/New page, remove validation for `Distance (km)` field and dont make it compulsory. also i noticed that the user must fill the `Quote Notes / Client Requirements` field before the form can submit, you can put an asterics showing that it is a compulsory field and makeit to show validation message if it's skeeped

- in /Fleet/Charter/New page, at the `Booking Assignment` section, the `Driver` field should be a ropdown of staff in the logistics department, also the field will be reacting to the changes of the `Vehicle` field, because vehicles are assigned to drivers. So when a vehicle is selected, it shoul select the driver assigned to it, also note that another Driver can be assigned to that booking right thee even though taht driver is not the one assigned to that vehicle

- remove the Distance (km) field at the Multi-Stop Route & Manual Pricing section in the /Fleet/Quotes/New page
-  the Quoted Leg Amount (₦) field is validation is requesting for a kobo 01 in every proce put filled. if the user put 20000, it requires for 20000.01 for example , pls remove the validation in that field
- ill like to have a fuel record list and action in the /Fleet/Charter/Details page to record fuel for the asigned vehicle mapped to that particuler charter
- for every production delivery done for route dispatch which is not a chater, i want the user to be able to register the movement of such, in the form they should be a place to enter multiple delivery poin, and also track the status of the vehicle, maybe just a page that will show all the dispatch ana button to create a new dispache in the view page of that dispach, they should also be a list of fuel record for mapped to that dispatch and a button to record more fuel 

- Note: now it is clear that fuel is recorded for two things, preorder delivery and chater for vip movement

- i want the /FuelLogs/Index page to indicate if the fuel log was for a dispatch or chater also they should be a filter on top for date and another dropdown to filter between dispatch or chater fuellog
- the monitory values in the /Logistics page is not acurate enough and it's overwalming, take off all the money value and redesign the /Logistics page to fit the current fleet module

- i also want to know if the setting in /Settings/Finance page at `Benchmark Configuration Form` and `Live Policy Visualizer` section including it's KPI is responsible for any action in this application ?

- i want to include a column if the fuel purchase was for chater or dispatch or general in `Fuel Purchase & Cost` i.e  `/Reports?report=fleet-fuel` report page table. The same in this `Fuel Type / Station Comparison` page table `/Reports?report=fleet-fuel-comparison` 
