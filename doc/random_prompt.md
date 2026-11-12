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