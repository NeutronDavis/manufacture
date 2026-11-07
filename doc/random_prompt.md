## bilding Action
- First
    This application is going to be allow login from the following users
    1. Super Admin (Full Enterprise Control & Settings)
    2. Production Manager (Recipes, Batches, Oven Output)
    3. Store Manager (Inventory, Requisitions, Material Issues)
    4. Sales Representative (POS Registers, Counter Sales, Pre-Orders)
    5. Vendor (Pre-Orders Portal)
    6. HR & Payroll Manager (Staff Records, Advances, Payroll Run)

    In this /UserManagement/Create page, an admin registers a new system user
    it shows the form which is going to be used to register users with rights to this application

- Secondly
    i will be using EFCore and DTO in the entire project
    create or check if the DTO already exist

- Third
    I noticed that there is no Role table, include it and link with the user table

- Fourth
    I will need a seed data for the all 6 users in the databse, before that create the Role table and purpulate it with the roles

- Fifth
 Disable the Mockdata for the /UserManagement/Create page and linke the page to the databse display the real data in /UserManagement page make sure to make the Deactiveate and Activate user function to work also include a delete action to delete a user

 - Work on the /Login page, make sure it validate the loged in user before sign in that user
 - sensitize the input agains SQL injection

 - Make everything that concerns this /UserManagement/Permissions to be dynamic
 - If ther is no databse table for it, create and impliment it
 - but befor implimentation, i would ilke to see an implimentaion plan on how to solve this problem

 ## HR module

### The Staff Directory
 - Let's work on the Employees which is more like the Staff Directory in /HR/StaffDirectory page 
 - clear all the Mock data for this employee link it up with the databse. Provide 6 diffrent employee as a seed data from the 5 diffrent department

 ### The Staff Records & Payroll
 - For the /HR page which is for the Staff Records & Payroll, ill like to have all the mock data out and the page be linked up with the databse, remove any field that is not form the databse and if any fiels seems to be needed and spposed to be ther think about it and include it in the employees databse . Take note of that.
 - The KPI in the Staff Records & Payroll page i.e /HR should be well calculated befor display 

 ### Staff Salary Advances & Loan Ledger
 - Form the /Payroll/Advances page which list out all the loan records from employees, 
 - First:
    - i will like to the admin to be able to set up a standard variable for the Monthly Repayment percentage which will be deducted from the employees salary, the setting of that should be in this /Settings/Treasury page, redesign the page to fit mullty settings configuration becasue more settings might fall into that page, also rename that page to something that fits it's purpose.

 - Secondly:
    - work on the /Payroll/Advances/Create form page link it up with the databse
    - i want the /Payroll/Advances to be rubostic such that, it values for changes when ever a Monthly Payroll is exicuted at the /Payroll/Run, the status should also be tracked base on the Remaining Loan
    - i would like each deduction with discription to be documented in the database so it will be displayed at the Staff Records page /HR when the employee is selected

### Process Monthly Payroll Run
-For this /Payroll/Run page i have few observation
- First:
    - The Active Loan Recoveries KPI should be removed. 
    - The Estimated Net Payout KPI should be well calculated base on the value of the Total Base Salary minus the value of the active Loan form the /Payroll/Advances, always check for the salary advance with status Active Loan and also use the newlly introduced Monthly Repayment percentage table for the percentage which will be deducted based on the Remaining Loan amount, Think about this calculation befor displaying it
    - At the  Staff Compensation & Adjustments section in the /Payroll/Run the Loan Recovery column value should be acutrate, the Bal: under the badge should calculate and display the remaining loan balance
 

 Note, make sure to think about every single implimentation to make it profesional an perfect 

### Payroll
 - The /Payroll page should dis play list of payrolls generated and it should have button for view action which will be opend in a view page showing the information for that payrole
 - the payrole list page shoud contain a date column showing the date that it was generated
 - that the KPI will be taken off entilly  in this / Payroll page 
 - include a date filter 

### Staff Directory
- The /HR page should become the Staff Directory, it should be seen as the Staff Directory page
- Take off the /HR/StaffDirectory and the /HR/Employees/Details/ page 
-  Make sure to display all the employees information at the right side of the / HR page when that employee is selected
- hilight the selected employee at the left to show that it's the selected one 
- the KPI is not really neede in the /HR page 
- remove the Staff Directory button in the /HR page 
- Also add a status filter in the filter section in the /HR page 
- display a table of payrole for a the selected user at the right side of the /HR page 
- also include a table of salary advance for a selected user at the right side of the /HR page 



### Staff Salary Advances & Loan Ledger
- The /Payroll/Advances page should have an imput to sort the table either by date or the sortcode and the Emoloyee name 
- the table should include a view and edit and delete action, the view action should take the user to a diffrent page to view the details, the delete action should opena a modal asking if the user is sure about deleting the entity, the modal should contain two action button delete and cancel button

### Staff Directory
- I want all the employees information taht was included in the Add New Employee form at /HR/Employees/Create page should be included at the right side of the /HR page when an employee is selected

### Payroll
- i noticed that there is no payroll seed data but i noticed that a salary advance seed data was created on 6 set 2026 for Emeka Obi and on 6 oct 2026 for Usman Danjuma. On the 
    - Repaid column for Emeka Obi 12,000.00 and Remaining is 48,000.00 Principal was 60,000.00	
    - Repaid column for Usman Danjuma 6,000.00 and Remaining is 24,000.00 Principal was 30,000.00	
    #### Note the Monthly Loan Recovery (%) is 20%
    - create a payroll seed for September 2026 indicating loan payment for Emeka Obi with employee id of 1 staffCode EMP-001 Loan Recovery of 12,000.00  and Usman Danjuma with employee id of 4 staffCode EMP-004 Loan Recovery of 6,000.00.
    - the filter on the /Payroll page is not looking nice , arange it well

 ### Staff Salary Advances 
 - Salary advance should not have a repaid coulumn, instead it should have the total number of month to be paid back. 

 - The /Payroll/Advances page needed a filter not a sort field. Filetr by status, date, (saff code and name )
 - The /Payroll/Advances/Details/ page needs to fill the page and not centerd same for the payroll details page if exist
 - make sure the Repayment History shows in the /Payroll/Advances/Details/

 ### Monthly Recovery Calculation
 - i was expecting it to always calculate the percentage (20% Monthly Loan Recovery (%) set by admin) by the Principal loan amount, pls make it work like that and not calculate form the Remaining

 ### Payroll
 - i wanted the /Payroll page to display a table were each row is for a particuler payroll which was generated for all the employees where by when clicked it will take to ab=nother page to display all the employess and thier coulumns and they should be a KPI in the view page 
 - keep the current /Payroll/Details/ content which shows a summary for a particuler employee, create another one for a particuler payroll and link up


### Staff Salary Advances 
 - Salary advance should not have a repaid coulumn, instead it should have the total number of month to be paid back. 
 - thier repayment history should be listed at the Repayment History section in /Payroll/Advances/Details/ page 
 - the Total Repaid KPI in the  /Payroll/Advances/Details/ page  should be the summation of all paid history
 
 ### Payroll
- what happens when the remaining salary advance amount is less than the Monthly Loan Recover percentage. I expect the wole remaining salary advance to fall in for payment at that point

## Fleet module
  
   #### Vehicle Directory
- 
    - From the /Logistics/Vehicles page, which displayes all the vehicles, let's work on making everything come from the databse.
    - The /Fleet/Vehicles/New page is the new vehicle form, work on the vehicle table and DTO to insert the tables into the database and display them on the /Logistics/Vehicles
    - ill need 6 diffrent vehicle seed data in the database to be created
    - take note of the Vehicle Type field , the dropdown in /Fleet/Vehicles/New page is not listing out the correct vehicle type e.g Sedan , Truck, Bus, SUV, EV
    - the Fuel Type should also include EV carging 
    - the Assigned Driver Name field should list out employees in the logistics department only which probably might have a driver role 
    - dont make it compulsory to assign a driver  during vehicle creation

    #### Daily Fuel & Vehicle Operating Expenses
    - from the /Logistics/FuelLogs page, first remove the Odometer (KM) coulumn form the table
    - from the /Fleet/FuelLogs/New page which is the fule log form any registerd vehicle, first remove the Current Odometer (KM) field, bring in fuel type file which might be prtrol, Desel or EV Charge depending on the vehicle type. Also remove the Receipt field and remove it too in the /Logistics/FuelLogs page in the Petrol Station & Receipt coulumn rename it to Petrol Station
    - work on it's the fuel log database table and unlink the mock data service form the page, it should all save and display data from the databse also include seed data for fuel logs in the database

    #### Weekly Fleet Fuel & Maintenance Ledger
    - from the /Fleet/WeeklyRunningCosts page make sure the page displays a well calculated data form the databse all the filter should work as expected

    #### 360° Statewide Route Pricing Matrix
    - this /Fleet/Rates page displayes route rate which will be used for estimation cost for booking
    - the KPI are not really explanatory, ill prefare something simple the label shpuld be descriptive 
    - the /Fleet/Rates/New page is the route pricing form, where user registers a route with the price for booking 
    - ill need all this to be working with the databse and not the mock data service also ill need seed data for the route pricing

    #### Commercial Haulage & Charter Bookings
    - in the /Fleet/Charter page, it's actually where all Charter Bookings are displayed 
    - the KPI Gross Freight Billable is not descriptive, dont ue the tearms "Freight" or "Haulage" 
    - the /Fleet/Charter/New page is the charter booking form, the Multi-Leg Layover Route Builder section will be using the route pricing
    - make everything be link to the databse not the mock seervice data, also crate 3 booking whith the 3 diffrent status available 
    - the /Fleet/Charter/Details page is the view page for charter booking details where it has a function to toggle the booking status

    #### Fleet & Delivery Logistics
    - the Fleet & Delivery Logistics page is more like the logistics overview page 
    - make all display data come form the databse not the mock service data
    