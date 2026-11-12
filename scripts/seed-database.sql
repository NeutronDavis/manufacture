SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/*
    Baseline configuration and seed rows.

    Keep this file idempotent: it is executed every time the development
    database is redeployed.

    Demo credentials (password for every seeded account: Password123!)

        admin@bakery.com        PIN 111111   SuperAdmin
        production@bakery.com   PIN 222222   ProductionManager
        store@bakery.com        PIN 333333   StoreManager
        sales@bakery.com        PIN 444444   SalesRep (POS-01)
        vendor@bakery.com       PIN 555555   Customer
        hr@bakery.com           PIN 666666   HrPayrollManager
*/

IF NOT EXISTS (SELECT 1 FROM [dbo].[TreasuryPolicySettings] WHERE [Id] = 1)
BEGIN
    INSERT INTO [dbo].[TreasuryPolicySettings]
    (
        [Id],
        [PlannedCashPercent],
        [AlertTolerancePercent],
        [PolicyMemo],
        [LastUpdatedUtc],
        [UpdatedBy]
    )
    VALUES
    (
        1,
        32.00,
        6.00,
        N'Standard retail benchmark: 32% expected in physical cash; 68% routed via bank transfer and POS terminals to limit route cash-in-transit risk.',
        SYSUTCDATETIME(),
        N'SuperAdmin'
    );
END;
GO

/* -------------------------------------------------------------------------
   Roles
   ------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'SuperAdmin')
    INSERT INTO [dbo].[Roles] ([Name], [DisplayName], [Description]) VALUES
    (N'SuperAdmin', N'Super Admin', N'Full enterprise control and settings.');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'ProductionManager')
    INSERT INTO [dbo].[Roles] ([Name], [DisplayName], [Description]) VALUES
    (N'ProductionManager', N'Production Manager', N'Recipes, batches, and oven output.');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'StoreManager')
    INSERT INTO [dbo].[Roles] ([Name], [DisplayName], [Description]) VALUES
    (N'StoreManager', N'Store Manager', N'Inventory, requisitions, and material issues.');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'SalesRep')
    INSERT INTO [dbo].[Roles] ([Name], [DisplayName], [Description]) VALUES
    (N'SalesRep', N'Sales Representative', N'POS registers, counter sales, and pre-orders.');

IF EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'Vendor')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'Customer')
    UPDATE [dbo].[Roles]
    SET [Name] = N'Customer',
        [DisplayName] = N'Customer',
        [Description] = N'Customer portal for self-service pre-orders and standing orders.',
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [Name] = N'Vendor';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'Customer')
    INSERT INTO [dbo].[Roles] ([Name], [DisplayName], [Description]) VALUES
    (N'Customer', N'Customer', N'Customer portal for self-service pre-orders and standing orders.');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'HrPayrollManager')
    INSERT INTO [dbo].[Roles] ([Name], [DisplayName], [Description]) VALUES
    (N'HrPayrollManager', N'HR & Payroll Manager', N'Staff records, advances, and payroll runs.');
GO

/* -------------------------------------------------------------------------
   Demo users (one per system role)

   PasswordHash/PinCodeHash values were generated with ASP.NET Core's
   PasswordHasher using the production defaults. Never store plaintext.
   ------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = N'admin@bakery.com')
    INSERT INTO [dbo].[Users]
        ([Name], [Email], [RoleId], [PinCodeHash], [PasswordHash], [MustChangePassword], [PhoneNumber], [IsActive])
    VALUES
        (N'Adekunle Johnson', N'admin@bakery.com',
         (SELECT [Id] FROM [dbo].[Roles] WHERE [Name] = N'SuperAdmin'),
         N'AQAAAAIAAYagAAAAEN2fB6Gm80DH/72PU5Ol/S9J8nYJQfeadr0U+Zbb74dIZadBzXBkGwTZce0Fy08Tng==',
         N'AQAAAAIAAYagAAAAENuCi0Rzrw3zfRHF7vP5SOUzzVLkYLD8ckUbuoDxS70ubvIIpJ+VjVMVqW+p6wPKXA==',
         0, N'+2348011112222', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = N'production@bakery.com')
    INSERT INTO [dbo].[Users]
        ([Name], [Email], [RoleId], [PinCodeHash], [PasswordHash], [MustChangePassword], [PhoneNumber], [IsActive])
    VALUES
        (N'Chidinma Okoro', N'production@bakery.com',
         (SELECT [Id] FROM [dbo].[Roles] WHERE [Name] = N'ProductionManager'),
         N'AQAAAAIAAYagAAAAEPs9jkfdQR5EP7MTjCIKOEEU44yFOSi0TL+DmqkZzSBAGf6yRlMD0g4K6exMak9Eag==',
         N'AQAAAAIAAYagAAAAEH+JEl9vdSDDEUYMWzBYjz6bblK6aiZZC+/AfFbUpDt/8mZoy7MXUFEK9/wC2NRjXg==',
         0, N'+2348022223333', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = N'store@bakery.com')
    INSERT INTO [dbo].[Users]
        ([Name], [Email], [RoleId], [PinCodeHash], [PasswordHash], [MustChangePassword], [PhoneNumber], [IsActive])
    VALUES
        (N'Musa Ibrahim', N'store@bakery.com',
         (SELECT [Id] FROM [dbo].[Roles] WHERE [Name] = N'StoreManager'),
         N'AQAAAAIAAYagAAAAEFNmBGZQ8Sj1IQlI9+w7lpzFlm7wgGLEpEjd9GJfLML3fYJwi01kpzGsywtGPwUJSw==',
         N'AQAAAAIAAYagAAAAEE5pvMnwpmwv1bGmRNPWESwE0jr/X8ViIS8AaYxR9cOkT/7jxOsRL8TXOhIYT0ldfA==',
         0, N'+2348033334444', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = N'sales@bakery.com')
    INSERT INTO [dbo].[Users]
        ([Name], [Email], [RoleId], [PinCodeHash], [PasswordHash], [MustChangePassword], [PosTerminalCode], [PhoneNumber], [IsActive])
    VALUES
        (N'Babatunde Alabi', N'sales@bakery.com',
         (SELECT [Id] FROM [dbo].[Roles] WHERE [Name] = N'SalesRep'),
         N'AQAAAAIAAYagAAAAEOI4ASXzk6OkHxNl69pKtrU81uAG3MvjBNRhfWx30L1rNwqxZylcivLjhr7mjqlhRw==',
         N'AQAAAAIAAYagAAAAEAT8y2OWAITp3ZeFhi8lCuyAqiEJobgy5CbkGZNmOsEyQW5fd51nNbwSqPsuXwzv2w==',
         0, N'POS-01', N'+2348044445555', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = N'vendor@bakery.com')
    INSERT INTO [dbo].[Users]
        ([Name], [Email], [RoleId], [PinCodeHash], [PasswordHash], [MustChangePassword], [PhoneNumber], [IsActive])
    VALUES
        (N'Goodness Supermarkets', N'vendor@bakery.com',
         (SELECT [Id] FROM [dbo].[Roles] WHERE [Name] = N'Customer'),
         N'AQAAAAIAAYagAAAAEGD5g+unPj7fxxxpJ42+e/wDL4fgxocDXo8wdm5MHEFDUQl4F5ozorMVmRkuntqBQg==',
         N'AQAAAAIAAYagAAAAEE/rfipIppys9YZFc0/Ntsy+V4LGTvWdtAQDmFtq6Xm5JbdPKQsY+Rt5Hks+glLZbA==',
         0, N'+2348055556666', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = N'hr@bakery.com')
    INSERT INTO [dbo].[Users]
        ([Name], [Email], [RoleId], [PinCodeHash], [PasswordHash], [MustChangePassword], [PhoneNumber], [IsActive])
    VALUES
        (N'Ngozi Eze', N'hr@bakery.com',
         (SELECT [Id] FROM [dbo].[Roles] WHERE [Name] = N'HrPayrollManager'),
         N'AQAAAAIAAYagAAAAEE1V4Vz0ve5snvmFjveOTnEG5e5dcige9/tQLCfOcl+RbuwAFxvLBER2vOopY+hERw==',
         N'AQAAAAIAAYagAAAAEPofJjuFhb0pAHYezYNMMK7cSEKFLIRZCaD8dM2/ej4//yV/5VyiLlD2KxOe8IE+ug==',
         0, N'+2348066667777', 1);
GO

/* -------------------------------------------------------------------------
   Payroll and loan policy
   ------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM [dbo].[PayrollSettings] WHERE [Id] = 1)
    INSERT INTO [dbo].[PayrollSettings]
        ([Id], [LoanMonthlyRepaymentPercent], [MaxLoanPercentOfSalary], [HousingAllowancePercent],
         [TransportAllowancePercent], [PayeTaxPercent], [PensionPercent], [HealthInsurancePercent],
         [UpdatedBy])
    VALUES
        (1, 20.00, 50.00, 12.50, 6.25, 3.50, 5.00, 2.65, N'Seed');
GO

/* -------------------------------------------------------------------------
   Department and Job Role master data
   ------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Name] = N'Production')
    INSERT INTO [dbo].[Departments] ([Name], [Description]) VALUES (N'Production', N'Bakery, water, popcorn and production floor staff.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Name] = N'Sales')
    INSERT INTO [dbo].[Departments] ([Name], [Description]) VALUES (N'Sales', N'Cashiers, sales representatives and field distribution staff.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Name] = N'Logistics')
    INSERT INTO [dbo].[Departments] ([Name], [Description]) VALUES (N'Logistics', N'Drivers, dispatch and vehicle operations.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Name] = N'Store')
    INSERT INTO [dbo].[Departments] ([Name], [Description]) VALUES (N'Store', N'Warehouse, store keeping and inventory control.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Name] = N'Admin')
    INSERT INTO [dbo].[Departments] ([Name], [Description]) VALUES (N'Admin', N'Administration, HR, finance and management.');

DECLARE @DeptProduction INT = (SELECT [Id] FROM [dbo].[Departments] WHERE [Name] = N'Production');
DECLARE @DeptSales INT = (SELECT [Id] FROM [dbo].[Departments] WHERE [Name] = N'Sales');
DECLARE @DeptLogistics INT = (SELECT [Id] FROM [dbo].[Departments] WHERE [Name] = N'Logistics');
DECLARE @DeptStore INT = (SELECT [Id] FROM [dbo].[Departments] WHERE [Name] = N'Store');
DECLARE @DeptAdmin INT = (SELECT [Id] FROM [dbo].[Departments] WHERE [Name] = N'Admin');

IF NOT EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = N'Head Baker / Mixer')
    INSERT INTO [dbo].[JobRoles] ([DepartmentId], [Name], [Description]) VALUES (@DeptProduction, N'Head Baker / Mixer', N'Bread mixing and oven production lead.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = N'Quality Control Supervisor')
    INSERT INTO [dbo].[JobRoles] ([DepartmentId], [Name], [Description]) VALUES (@DeptProduction, N'Quality Control Supervisor', N'Production quality and batch compliance.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = N'Production Manager')
    INSERT INTO [dbo].[JobRoles] ([DepartmentId], [Name], [Description]) VALUES (@DeptProduction, N'Production Manager', N'Production planning and yield oversight.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = N'Senior Sales Executive')
    INSERT INTO [dbo].[JobRoles] ([DepartmentId], [Name], [Description]) VALUES (@DeptSales, N'Senior Sales Executive', N'Key account and field sales leadership.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = N'Sales Representative')
    INSERT INTO [dbo].[JobRoles] ([DepartmentId], [Name], [Description]) VALUES (@DeptSales, N'Sales Representative', N'Daily route sales and customer debt collection.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = N'Cashier')
    INSERT INTO [dbo].[JobRoles] ([DepartmentId], [Name], [Description]) VALUES (@DeptSales, N'Cashier', N'POS terminal cashier and drawer balancing.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = N'Lead Delivery Driver')
    INSERT INTO [dbo].[JobRoles] ([DepartmentId], [Name], [Description]) VALUES (@DeptLogistics, N'Lead Delivery Driver', N'Vehicle delivery lead.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = N'Delivery Driver')
    INSERT INTO [dbo].[JobRoles] ([DepartmentId], [Name], [Description]) VALUES (@DeptLogistics, N'Delivery Driver', N'Route delivery and vehicle operations.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = N'Store Keeper')
    INSERT INTO [dbo].[JobRoles] ([DepartmentId], [Name], [Description]) VALUES (@DeptStore, N'Store Keeper', N'Warehouse receipt and material issuance.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = N'Inventory Officer')
    INSERT INTO [dbo].[JobRoles] ([DepartmentId], [Name], [Description]) VALUES (@DeptStore, N'Inventory Officer', N'Stock counts and inventory records.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = N'HR & Admin Manager')
    INSERT INTO [dbo].[JobRoles] ([DepartmentId], [Name], [Description]) VALUES (@DeptAdmin, N'HR & Admin Manager', N'Human resources and administration.');
IF NOT EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = N'Accountant')
    INSERT INTO [dbo].[JobRoles] ([DepartmentId], [Name], [Description]) VALUES (@DeptAdmin, N'Accountant', N'Finance and account reconciliation.');
GO

/* -------------------------------------------------------------------------
   Staff directory (6 employees across 5 departments)
   ------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-001')
    INSERT INTO [dbo].[Employees]
        ([FullName], [StaffCode], [Department], [Designation], [PhoneNumber], [Email], [Address],
         [BankName], [BankAccountNumber], [EmploymentStatus], [BaseSalary],
         [EmergencyContactName], [EmergencyContactPhone], [EmergencyRelationship],
         [GuarantorName], [GuarantorPhone], [GuarantorAddress], [GuarantorOccupation],
         [EmploymentDate], [IsActive])
    VALUES
        (N'Emeka Obi', N'EMP-001', N'Production', N'Head Baker / Mixer', N'+2348034567890', N'emeka.obi@bakery.com', N'14 Ikorodu Road, Ketu, Lagos',
         N'Access Bank', N'0123456789', N'Active', 120000.00,
         N'Blessing Obi', N'+2348034567891', N'Wife',
         N'Chief Arthur Eze', N'+2348021113333', N'8 Commercial Ave, Yaba, Lagos', N'Business Executive',
         CONVERT(DATE, DATEADD(YEAR, -2, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-002')
    INSERT INTO [dbo].[Employees]
        ([FullName], [StaffCode], [Department], [Designation], [PhoneNumber], [Email], [Address],
         [BankName], [BankAccountNumber], [EmploymentStatus], [BaseSalary],
         [EmergencyContactName], [EmergencyContactPhone], [EmergencyRelationship],
         [GuarantorName], [GuarantorPhone], [GuarantorAddress], [GuarantorOccupation],
         [EmploymentDate], [IsActive])
    VALUES
        (N'Chidinma Okoro', N'EMP-002', N'Production', N'Quality Control Supervisor', N'+2348022223333', N'chidinma.okoro@bakery.com', N'7 Ogba Road, Ikeja, Lagos',
         N'Guaranty Trust Bank', N'0234567890', N'Active', 105000.00,
         N'Ifeanyi Okoro', N'+2348022223334', N'Husband',
         N'Dr. Adaobi Nwosu', N'+2348031114444', N'12 Allen Avenue, Ikeja, Lagos', N'Medical Practitioner',
         CONVERT(DATE, DATEADD(MONTH, -20, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-003')
    INSERT INTO [dbo].[Employees]
        ([FullName], [StaffCode], [Department], [Designation], [PhoneNumber], [Email], [Address],
         [BankName], [BankAccountNumber], [EmploymentStatus], [BaseSalary],
         [EmergencyContactName], [EmergencyContactPhone], [EmergencyRelationship],
         [GuarantorName], [GuarantorPhone], [GuarantorAddress], [GuarantorOccupation],
         [EmploymentDate], [IsActive])
    VALUES
        (N'Folashade Adeyemi', N'EMP-003', N'Sales', N'Senior Sales Executive', N'+2348045678901', N'folashade.a@bakery.com', N'22 Allen Avenue, Ikeja, Lagos',
         N'Zenith Bank', N'0345678901', N'Active', 95000.00,
         N'Tunde Adeyemi', N'+2348045678902', N'Brother',
         N'Barrister Kayode Salami', N'+2348092224444', N'15 Toyin Street, Ikeja, Lagos', N'Legal Practitioner',
         CONVERT(DATE, DATEADD(YEAR, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-004')
    INSERT INTO [dbo].[Employees]
        ([FullName], [StaffCode], [Department], [Designation], [PhoneNumber], [Email], [Address],
         [BankName], [BankAccountNumber], [EmploymentStatus], [BaseSalary],
         [EmergencyContactName], [EmergencyContactPhone], [EmergencyRelationship],
         [GuarantorName], [GuarantorPhone], [GuarantorAddress], [GuarantorOccupation],
         [EmploymentDate], [IsActive])
    VALUES
        (N'Usman Danjuma', N'EMP-004', N'Logistics', N'Lead Delivery Driver', N'+2348056789012', N'usman.d@bakery.com', N'5 Agege Motor Road, Mushin, Lagos',
         N'United Bank for Africa', N'0456789012', N'Active', 85000.00,
         N'Amina Danjuma', N'+2348056789013', N'Sister',
         N'Alhaji Bello Garba', N'+2348083335555', N'40 Mile 12 Market Rd, Kosofe, Lagos', N'Logistics Contractor',
         CONVERT(DATE, DATEADD(MONTH, -18, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-005')
    INSERT INTO [dbo].[Employees]
        ([FullName], [StaffCode], [Department], [Designation], [PhoneNumber], [Email], [Address],
         [BankName], [BankAccountNumber], [EmploymentStatus], [BaseSalary],
         [EmergencyContactName], [EmergencyContactPhone], [EmergencyRelationship],
         [GuarantorName], [GuarantorPhone], [GuarantorAddress], [GuarantorOccupation],
         [EmploymentDate], [IsActive])
    VALUES
        (N'Kelechi Nnamdi', N'EMP-005', N'Store', N'Store Keeper', N'+2348067890123', N'kelechi.n@bakery.com', N'9 Oshodi Expressway, Lagos',
         N'First Bank', N'0567890123', N'Active', 80000.00,
         N'Nkechi Nnamdi', N'+2348067890124', N'Mother',
         N'Dr. Samuel Okonkwo', N'+2348074446666', N'33 Awolowo Way, Ikeja, Lagos', N'Medical Doctor',
         CONVERT(DATE, DATEADD(MONTH, -8, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-006')
    INSERT INTO [dbo].[Employees]
        ([FullName], [StaffCode], [Department], [Designation], [PhoneNumber], [Email], [Address],
         [BankName], [BankAccountNumber], [EmploymentStatus], [BaseSalary],
         [EmergencyContactName], [EmergencyContactPhone], [EmergencyRelationship],
         [GuarantorName], [GuarantorPhone], [GuarantorAddress], [GuarantorOccupation],
         [EmploymentDate], [IsActive])
    VALUES
        (N'Ngozi Eze', N'EMP-006', N'Admin', N'HR & Admin Manager', N'+2348066667777', N'ngozi.eze@bakery.com', N'3 Maryland Mall, Maryland, Lagos',
         N'Access Bank', N'0678901234', N'Active', 110000.00,
         N'Chinedu Eze', N'+2348066667778', N'Husband',
         N'Mrs. Bola Okafor', N'+2348052227777', N'18 Opebi Road, Ikeja, Lagos', N'Bank Manager',
         CONVERT(DATE, DATEADD(YEAR, -3, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-007')
    INSERT INTO [dbo].[Employees]
        ([FullName], [StaffCode], [Department], [Designation], [PhoneNumber], [Email], [Address],
         [BankName], [BankAccountNumber], [EmploymentStatus], [BaseSalary],
         [EmergencyContactName], [EmergencyContactPhone], [EmergencyRelationship],
         [GuarantorName], [GuarantorPhone], [GuarantorAddress], [GuarantorOccupation],
         [EmploymentDate], [IsActive])
    VALUES
        (N'Babatunde Alabi', N'EMP-007', N'Sales', N'Sales Representative', N'+2348044445555', N'babatunde.alabi@bakery.com', N'Ikeja, Lagos',
         N'First Bank', N'0700000007', N'Active', 90000.00,
         N'Kemi Alabi', N'+2348044445556', N'Wife',
         N'Mr. Tunde Bakare', N'+2348044445557', N'Ikeja, Lagos', N'Business Owner',
         CONVERT(DATE, DATEADD(YEAR, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-008')
    INSERT INTO [dbo].[Employees]
        ([FullName], [StaffCode], [Department], [Designation], [PhoneNumber], [Email], [Address],
         [BankName], [BankAccountNumber], [EmploymentStatus], [BaseSalary],
         [EmergencyContactName], [EmergencyContactPhone], [EmergencyRelationship],
         [GuarantorName], [GuarantorPhone], [GuarantorAddress], [GuarantorOccupation],
         [EmploymentDate], [IsActive])
    VALUES
        (N'Chiamaka Nwosu', N'EMP-008', N'Sales', N'Sales Representative', N'+2348055556677', N'chiamaka.nwosu@bakery.com', N'Lagos Island, Lagos',
         N'Guaranty Trust Bank', N'0700000008', N'Active', 90000.00,
         N'Ifeanyi Nwosu', N'+2348055556678', N'Brother',
         N'Mrs. Ada Nwosu', N'+2348055556679', N'Lagos Island, Lagos', N'Trader',
         CONVERT(DATE, DATEADD(YEAR, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-009')
    INSERT INTO [dbo].[Employees]
        ([FullName], [StaffCode], [Department], [Designation], [PhoneNumber], [Email], [Address],
         [BankName], [BankAccountNumber], [EmploymentStatus], [BaseSalary],
         [EmergencyContactName], [EmergencyContactPhone], [EmergencyRelationship],
         [GuarantorName], [GuarantorPhone], [GuarantorAddress], [GuarantorOccupation],
         [EmploymentDate], [IsActive])
    VALUES
        (N'Ibrahim Sanni', N'EMP-009', N'Sales', N'Sales Representative', N'+2348066667788', N'ibrahim.sanni@bakery.com', N'Yaba, Lagos',
         N'Access Bank', N'0700000009', N'Active', 90000.00,
         N'Fatima Sanni', N'+2348066667789', N'Wife',
         N'Alhaji Sanni', N'+2348066667790', N'Yaba, Lagos', N'Business Owner',
         CONVERT(DATE, DATEADD(MONTH, -10, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-010')
    INSERT INTO [dbo].[Employees]
        ([FullName], [StaffCode], [Department], [Designation], [PhoneNumber], [Email], [Address],
         [BankName], [BankAccountNumber], [EmploymentStatus], [BaseSalary],
         [EmergencyContactName], [EmergencyContactPhone], [EmergencyRelationship],
         [GuarantorName], [GuarantorPhone], [GuarantorAddress], [GuarantorOccupation],
         [EmploymentDate], [IsActive])
    VALUES
        (N'Blessing Etim', N'EMP-010', N'Sales', N'Sales Representative', N'+2348077778899', N'blessing.etim@bakery.com', N'Maryland, Lagos',
         N'Zenith Bank', N'0700000010', N'Active', 90000.00,
         N'Emmanuel Etim', N'+2348077778900', N'Husband',
         N'Mrs. Grace Etim', N'+2348077778901', N'Maryland, Lagos', N'Teacher',
         CONVERT(DATE, DATEADD(MONTH, -10, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-011')
    INSERT INTO [dbo].[Employees]
        ([FullName], [StaffCode], [Department], [Designation], [PhoneNumber], [Email], [Address],
         [BankName], [BankAccountNumber], [EmploymentStatus], [BaseSalary],
         [EmergencyContactName], [EmergencyContactPhone], [EmergencyRelationship],
         [GuarantorName], [GuarantorPhone], [GuarantorAddress], [GuarantorOccupation],
         [EmploymentDate], [IsActive])
    VALUES
        (N'Segun Adewale', N'EMP-011', N'Sales', N'Sales Representative', N'+2348088889900', N'segun.adewale@bakery.com', N'Agege, Lagos',
         N'First Bank', N'0700000011', N'Active', 90000.00,
         N'Bola Adewale', N'+2348088889901', N'Wife',
         N'Mr. Adewale', N'+2348088889902', N'Agege, Lagos', N'Business Owner',
         CONVERT(DATE, DATEADD(MONTH, -8, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-012')
    INSERT INTO [dbo].[Employees]
        ([FullName], [StaffCode], [Department], [Designation], [PhoneNumber], [Email], [Address],
         [BankName], [BankAccountNumber], [EmploymentStatus], [BaseSalary],
         [EmergencyContactName], [EmergencyContactPhone], [EmergencyRelationship],
         [GuarantorName], [GuarantorPhone], [GuarantorAddress], [GuarantorOccupation],
         [EmploymentDate], [IsActive])
    VALUES
        (N'Halima Yusuf', N'EMP-012', N'Sales', N'Sales Representative', N'+2348099990011', N'halima.yusuf@bakery.com', N'Oshodi, Lagos',
         N'Access Bank', N'0700000012', N'Active', 90000.00,
         N'Yusuf Bello', N'+2348099990012', N'Brother',
         N'Mrs. Amina Yusuf', N'+2348099990013', N'Oshodi, Lagos', N'Trader',
         CONVERT(DATE, DATEADD(MONTH, -8, SYSUTCDATETIME())), 1);

UPDATE [dbo].[Employees]
SET [DepartmentId] = (SELECT [Id] FROM [dbo].[Departments] WHERE [Name] = [dbo].[Employees].[Department])
WHERE [DepartmentId] IS NULL;

UPDATE [dbo].[Employees]
SET [JobRoleId] = (SELECT [Id] FROM [dbo].[JobRoles] WHERE [Name] = [dbo].[Employees].[Designation])
WHERE [JobRoleId] IS NULL
  AND EXISTS (SELECT 1 FROM [dbo].[JobRoles] WHERE [Name] = [dbo].[Employees].[Designation]);
GO

/* -------------------------------------------------------------------------
   Sellable product catalog (Production recipe master used by Sales & POS)
   ------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM [dbo].[Recipes] WHERE [Code] = N'BRD-JMB')
    INSERT INTO [dbo].[Recipes] ([Name], [Code], [Description], [ProductType], [BaselineUnit], [ExpectedYield], [OutputUnit], [PackagingCost], [LaborAndOverheadPerUnit], [SellingPrice], [IsActive])
    VALUES (N'Jumbo Family Bread', N'BRD-JMB', N'Family-size jumbo loaf.', 0, 0, 200.000, N'loaves', 35.0000, 80.0000, 1400.00, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Recipes] WHERE [Code] = N'BRD-MED')
    INSERT INTO [dbo].[Recipes] ([Name], [Code], [Description], [ProductType], [BaselineUnit], [ExpectedYield], [OutputUnit], [PackagingCost], [LaborAndOverheadPerUnit], [SellingPrice], [IsActive])
    VALUES (N'Medium Loaf Bread', N'BRD-MED', N'Medium everyday loaf.', 0, 0, 240.000, N'loaves', 22.0000, 55.0000, 900.00, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Recipes] WHERE [Code] = N'BRD-61')
    INSERT INTO [dbo].[Recipes] ([Name], [Code], [Description], [ProductType], [BaselineUnit], [ExpectedYield], [OutputUnit], [PackagingCost], [LaborAndOverheadPerUnit], [SellingPrice], [IsActive])
    VALUES (N'Special 61 Butter Bread', N'BRD-61', N'Butter-rich special loaf.', 0, 0, 180.000, N'loaves', 30.0000, 70.0000, 1100.00, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Recipes] WHERE [Code] = N'BRD-RND')
    INSERT INTO [dbo].[Recipes] ([Name], [Code], [Description], [ProductType], [BaselineUnit], [ExpectedYield], [OutputUnit], [PackagingCost], [LaborAndOverheadPerUnit], [SellingPrice], [IsActive])
    VALUES (N'Round Family Loaf', N'BRD-RND', N'Round family loaf.', 0, 0, 220.000, N'loaves', 20.0000, 50.0000, 850.00, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Recipes] WHERE [Code] = N'BRD-6IN1')
    INSERT INTO [dbo].[Recipes] ([Name], [Code], [Description], [ProductType], [BaselineUnit], [ExpectedYield], [OutputUnit], [PackagingCost], [LaborAndOverheadPerUnit], [SellingPrice], [IsActive])
    VALUES (N'6-in-1 Multigrain Loaf', N'BRD-6IN1', N'Multigrain sandwich loaf.', 0, 0, 180.000, N'loaves', 25.0000, 60.0000, 780.00, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Recipes] WHERE [Code] = N'WTR-189')
    INSERT INTO [dbo].[Recipes] ([Name], [Code], [Description], [ProductType], [BaselineUnit], [ExpectedYield], [OutputUnit], [PackagingCost], [LaborAndOverheadPerUnit], [SellingPrice], [IsActive])
    VALUES (N'Purified Water 18.9L', N'WTR-189', N'18.9L dispenser bottle.', 1, 1, 250.000, N'bottles', 95.0000, 40.0000, 250.00, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Recipes] WHERE [Code] = N'POP-40')
    INSERT INTO [dbo].[Recipes] ([Name], [Code], [Description], [ProductType], [BaselineUnit], [ExpectedYield], [OutputUnit], [PackagingCost], [LaborAndOverheadPerUnit], [SellingPrice], [IsActive])
    VALUES (N'Popcorn 40g Bag', N'POP-40', N'40g ready-to-eat popcorn bag.', 2, 2, 500.000, N'bags', 6.0000, 20.0000, 200.00, 1);
GO

/* -------------------------------------------------------------------------
   Sales bonus rules (buy X quantity, receive Y bonus units)
   ------------------------------------------------------------------------- */

DECLARE @BonusJumbo INT = (SELECT [Id] FROM [dbo].[Recipes] WHERE [Code] = N'BRD-JMB');
DECLARE @BonusMedium INT = (SELECT [Id] FROM [dbo].[Recipes] WHERE [Code] = N'BRD-MED');
DECLARE @BonusWater INT = (SELECT [Id] FROM [dbo].[Recipes] WHERE [Code] = N'WTR-189');
DECLARE @BonusPopcorn INT = (SELECT [Id] FROM [dbo].[Recipes] WHERE [Code] = N'POP-40');

IF @BonusJumbo IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[SalesBonusRules] WHERE [RecipeId] = @BonusJumbo AND [ThresholdQuantity] = 10)
    INSERT INTO [dbo].[SalesBonusRules] ([RecipeId], [ThresholdQuantity], [BonusQuantity]) VALUES (@BonusJumbo, 10, 1);
IF @BonusJumbo IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[SalesBonusRules] WHERE [RecipeId] = @BonusJumbo AND [ThresholdQuantity] = 50)
    INSERT INTO [dbo].[SalesBonusRules] ([RecipeId], [ThresholdQuantity], [BonusQuantity]) VALUES (@BonusJumbo, 50, 6);
IF @BonusMedium IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[SalesBonusRules] WHERE [RecipeId] = @BonusMedium AND [ThresholdQuantity] = 20)
    INSERT INTO [dbo].[SalesBonusRules] ([RecipeId], [ThresholdQuantity], [BonusQuantity]) VALUES (@BonusMedium, 20, 2);
IF @BonusWater IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[SalesBonusRules] WHERE [RecipeId] = @BonusWater AND [ThresholdQuantity] = 20)
    INSERT INTO [dbo].[SalesBonusRules] ([RecipeId], [ThresholdQuantity], [BonusQuantity]) VALUES (@BonusWater, 20, 2);
IF @BonusPopcorn IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[SalesBonusRules] WHERE [RecipeId] = @BonusPopcorn AND [ThresholdQuantity] = 50)
    INSERT INTO [dbo].[SalesBonusRules] ([RecipeId], [ThresholdQuantity], [BonusQuantity]) VALUES (@BonusPopcorn, 50, 5);
GO

/* -------------------------------------------------------------------------
   Opening salary advances (active loan ledger)
   ------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM [dbo].[SalaryAdvances] WHERE [EmployeeId] = (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-001') AND [Reason] = N'Family Emergency / Child School Fees')
    INSERT INTO [dbo].[SalaryAdvances]
        ([EmployeeId], [PrincipalAmount], [MonthlyDeductionAmount], [TotalRepaid], [RequestDate], [Reason], [Status])
    VALUES
        ((SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-001'),
         60000.00, 12000.00, 48000.00, CONVERT(DATE, DATEADD(MONTH, -1, SYSUTCDATETIME())),
         N'Family Emergency / Child School Fees', N'Active');

IF NOT EXISTS (SELECT 1 FROM [dbo].[SalaryAdvances] WHERE [EmployeeId] = (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-004') AND [Reason] = N'Vehicle Repair Support')
    INSERT INTO [dbo].[SalaryAdvances]
        ([EmployeeId], [PrincipalAmount], [MonthlyDeductionAmount], [TotalRepaid], [RequestDate], [Reason], [Status])
    VALUES
        ((SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-004'),
         30000.00, 6000.00, 24000.00, CONVERT(DATE, DATEADD(MONTH, -1, SYSUTCDATETIME())),
         N'Vehicle Repair Support', N'Active');
GO

/* -------------------------------------------------------------------------
   September 2026 payroll seed

   Opening payroll run that demonstrates the 20% principal-based loan recovery:
     EMP-001 Emeka Obi     recovery 12,000.00 (20% of 60,000)
     EMP-004 Usman Danjuma recovery  6,000.00 (20% of 30,000)
   ------------------------------------------------------------------------- */

DECLARE @PayrollMonth TINYINT = 9;
DECLARE @PayrollYear SMALLINT = 2026;
DECLARE @PayrollCreatedAt DATETIME2(3) = '2026-09-30T18:00:00';
DECLARE @SettingsId TINYINT = 1;

DECLARE @EmekaId INT = (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-001');
DECLARE @UsmanId INT = (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-004');
DECLARE @EmekaAdvanceId INT = (SELECT [Id] FROM [dbo].[SalaryAdvances] WHERE [EmployeeId] = @EmekaId AND [Reason] = N'Family Emergency / Child School Fees');
DECLARE @UsmanAdvanceId INT = (SELECT [Id] FROM [dbo].[SalaryAdvances] WHERE [EmployeeId] = @UsmanId AND [Reason] = N'Vehicle Repair Support');
DECLARE @PayrollRunId INT = (SELECT [Id] FROM [dbo].[PayrollRuns] WHERE [PayrollYear] = @PayrollYear AND [PayrollMonth] = @PayrollMonth);

IF @PayrollRunId IS NULL
BEGIN
    INSERT INTO [dbo].[PayrollRuns]
        ([RunNumber], [PayrollMonth], [PayrollYear], [GeneratedAt], [GeneratedBy],
         [EmployeeCount], [GrossTotal], [DeductionsTotal], [NetTotal], [Status], [Notes],
         [CreatedAt], [UpdatedAt])
    VALUES
        (N'PAY-2026-09', @PayrollMonth, @PayrollYear, @PayrollCreatedAt, N'Seed',
         0, 0.00, 0.00, 0.00, N'Pending', N'September 2026 payroll for all active employees',
         @PayrollCreatedAt, @PayrollCreatedAt);

    SET @PayrollRunId = SCOPE_IDENTITY();
END;

/* One payroll record for every active employee. Loan recovery is applied only
   to the two employees with active advances. */
INSERT INTO [dbo].[PayrollRecords]
    ([PayrollRunId], [EmployeeId], [PayrollMonth], [PayrollYear], [BaseSalary], [PerformanceBonus],
     [HousingAllowance], [TransportAllowance], [OtherAllowances],
     [DisciplinaryDeductions], [AdvanceDeduction], [TaxAndLevies],
     [PensionDeduction], [HealthInsuranceDeduction], [PaymentStatus], [Notes],
     [CreatedAt], [UpdatedAt])
SELECT
    @PayrollRunId,
    e.[Id],
    @PayrollMonth,
    @PayrollYear,
    e.[BaseSalary],
    0.00,
    ROUND(e.[BaseSalary] * ps.[HousingAllowancePercent] / 100.0, 2),
    ROUND(e.[BaseSalary] * ps.[TransportAllowancePercent] / 100.0, 2),
    0.00,
    0.00,
    CASE
        WHEN e.[Id] = @EmekaId THEN 12000.00
        WHEN e.[Id] = @UsmanId THEN 6000.00
        ELSE 0.00
    END,
    ROUND(
        (e.[BaseSalary]
            + ROUND(e.[BaseSalary] * ps.[HousingAllowancePercent] / 100.0, 2)
            + ROUND(e.[BaseSalary] * ps.[TransportAllowancePercent] / 100.0, 2))
        * ps.[PayeTaxPercent] / 100.0,
        2),
    ROUND(e.[BaseSalary] * ps.[PensionPercent] / 100.0, 2),
    ROUND(e.[BaseSalary] * ps.[HealthInsurancePercent] / 100.0, 2),
    N'Pending',
    N'September 2026 payroll seed - generated for all active employees',
    @PayrollCreatedAt,
    @PayrollCreatedAt
FROM [dbo].[Employees] e
CROSS JOIN [dbo].[PayrollSettings] ps
WHERE ps.[Id] = @SettingsId
  AND e.[IsActive] = 1
  AND e.[EmploymentStatus] <> N'Inactive'
  AND NOT EXISTS
  (
      SELECT 1
      FROM [dbo].[PayrollRecords] p
      WHERE p.[EmployeeId] = e.[Id]
        AND p.[PayrollMonth] = @PayrollMonth
        AND p.[PayrollYear] = @PayrollYear
  );

/* Itemised deduction lines for every employee in the run. */
INSERT INTO [dbo].[PayrollDeductionLines]
    ([PayrollRecordId], [EmployeeId], [DeductionType], [Description], [Amount], [SalaryAdvanceId], [CreatedAt])
SELECT p.[Id], p.[EmployeeId], N'Tax', N'PAYE tax and statutory levies', p.[TaxAndLevies], NULL, @PayrollCreatedAt
FROM [dbo].[PayrollRecords] p
WHERE p.[PayrollRunId] = @PayrollRunId
  AND NOT EXISTS (SELECT 1 FROM [dbo].[PayrollDeductionLines] l WHERE l.[PayrollRecordId] = p.[Id] AND l.[DeductionType] = N'Tax')
UNION ALL
SELECT p.[Id], p.[EmployeeId], N'Pension', N'Employee pension contribution', p.[PensionDeduction], NULL, @PayrollCreatedAt
FROM [dbo].[PayrollRecords] p
WHERE p.[PayrollRunId] = @PayrollRunId
  AND NOT EXISTS (SELECT 1 FROM [dbo].[PayrollDeductionLines] l WHERE l.[PayrollRecordId] = p.[Id] AND l.[DeductionType] = N'Pension')
UNION ALL
SELECT p.[Id], p.[EmployeeId], N'Health', N'Health insurance contribution', p.[HealthInsuranceDeduction], NULL, @PayrollCreatedAt
FROM [dbo].[PayrollRecords] p
WHERE p.[PayrollRunId] = @PayrollRunId
  AND NOT EXISTS (SELECT 1 FROM [dbo].[PayrollDeductionLines] l WHERE l.[PayrollRecordId] = p.[Id] AND l.[DeductionType] = N'Health')
UNION ALL
SELECT p.[Id], p.[EmployeeId], N'Loan',
    CONCAT(N'Salary advance repayment - ', sa.[Reason], N' (Loan #', sa.[Id], N')'),
    p.[AdvanceDeduction], sa.[Id], @PayrollCreatedAt
FROM [dbo].[PayrollRecords] p
INNER JOIN [dbo].[SalaryAdvances] sa
    ON sa.[EmployeeId] = p.[EmployeeId]
   AND sa.[Id] IN (@EmekaAdvanceId, @UsmanAdvanceId)
WHERE p.[PayrollRunId] = @PayrollRunId
  AND p.[AdvanceDeduction] > 0
  AND NOT EXISTS (SELECT 1 FROM [dbo].[PayrollDeductionLines] l WHERE l.[PayrollRecordId] = p.[Id] AND l.[DeductionType] = N'Loan');

IF @PayrollRunId IS NOT NULL
BEGIN
    UPDATE [dbo].[PayrollRuns]
    SET [EmployeeCount] = (SELECT COUNT(*) FROM [dbo].[PayrollRecords] WHERE [PayrollRunId] = @PayrollRunId),
        [GrossTotal] = (SELECT COALESCE(SUM([GrossSalary]), 0) FROM [dbo].[PayrollRecords] WHERE [PayrollRunId] = @PayrollRunId),
        [DeductionsTotal] = (SELECT COALESCE(SUM([TotalDeductions]), 0) FROM [dbo].[PayrollRecords] WHERE [PayrollRunId] = @PayrollRunId),
        [NetTotal] = (SELECT COALESCE(SUM([NetSalary]), 0) FROM [dbo].[PayrollRecords] WHERE [PayrollRunId] = @PayrollRunId),
        [Status] = CASE
            WHEN EXISTS (SELECT 1 FROM [dbo].[PayrollRecords] WHERE [PayrollRunId] = @PayrollRunId AND [PaymentStatus] <> N'Paid') THEN N'Pending'
            ELSE N'Paid'
        END,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [Id] = @PayrollRunId;
END;
GO

/* -------------------------------------------------------------------------
   Inventory seed data

   Categories:
     Raw Materials   - flour, sugar, yeast, softeners, flavours
     Packaging       - nylon wraps, dispenser bottles, caps, sealing/outer lines, labels
     Additive/Flavor - preservatives, seasoning, flavour powders
     Fuel            - petrol and LPG
     Utilities       - bulk diesel for generators and water treatment stock

   Vehicles are deliberately not part of store inventory. Fleet assets are
   managed exclusively in the Vehicle Fleet & Logistics module.
   ------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM [dbo].[Suppliers] WHERE [Name] = N'Flour Mills of Nigeria PLC (Golden Penny)')
    INSERT INTO [dbo].[Suppliers] ([Name], [ContactPerson], [PhoneNumber], [Email], [SupplyCategory], [Address], [IsActive])
    VALUES (N'Flour Mills of Nigeria PLC (Golden Penny)', N'Alhaji Shehu Garba', N'+2348039988771', N'orders@fmnplc.com', N'Flour & Grains', N'Apapa Port Complex, Lagos', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Suppliers] WHERE [Name] = N'Dangote Sugar Refinery')
    INSERT INTO [dbo].[Suppliers] ([Name], [ContactPerson], [PhoneNumber], [Email], [SupplyCategory], [Address], [IsActive])
    VALUES (N'Dangote Sugar Refinery', N'Mrs. Bisi Adeleke', N'+2348028877662', N'sales@dangotesugar.com', N'Sweeteners & Salt', N'Shed 20, NPA Apapa, Lagos', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Suppliers] WHERE [Name] = N'FlexiPack Ltd (Bakery Packaging)')
    INSERT INTO [dbo].[Suppliers] ([Name], [ContactPerson], [PhoneNumber], [Email], [SupplyCategory], [Address], [IsActive])
    VALUES (N'FlexiPack Ltd (Bakery Packaging)', N'Mr. Kenneth Okon', N'+2348076655443', N'packaging@flexipack.ng', N'Packaging (Nylon & Tags)', N'Industrial Estate, Ikeja, Lagos', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Suppliers] WHERE [Name] = N'Lesaffre / Saf-Instant Yeast West Africa')
    INSERT INTO [dbo].[Suppliers] ([Name], [ContactPerson], [PhoneNumber], [Email], [SupplyCategory], [Address], [IsActive])
    VALUES (N'Lesaffre / Saf-Instant Yeast West Africa', N'David Mensah', N'+2348091122334', N'sales@lesaffre.com', N'Yeast & Additives', N'Oregun Industrial Area, Lagos', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Suppliers] WHERE [Name] = N'AquaPure Industrial Systems')
    INSERT INTO [dbo].[Suppliers] ([Name], [ContactPerson], [PhoneNumber], [Email], [SupplyCategory], [Address], [IsActive])
    VALUES (N'AquaPure Industrial Systems', N'Engr. Patrick Chukwu', N'+2348023456789', N'support@aquapure.ng', N'Water Treatment & Bottles', N'Plot 14 Ogba Industrial Scheme, Lagos', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Suppliers] WHERE [Name] = N'SeedCo Grains & Agro Allied')
    INSERT INTO [dbo].[Suppliers] ([Name], [ContactPerson], [PhoneNumber], [Email], [SupplyCategory], [Address], [IsActive])
    VALUES (N'SeedCo Grains & Agro Allied', N'Malam Bello Zaria', N'+2348035678901', N'sales@seedcoagro.ng', N'Corn & Grains', N'Grain Terminal, Dawanau / Iddo, Lagos', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Suppliers] WHERE [Name] = N'Presco PLC / Grand Cereals')
    INSERT INTO [dbo].[Suppliers] ([Name], [ContactPerson], [PhoneNumber], [Email], [SupplyCategory], [Address], [IsActive])
    VALUES (N'Presco PLC / Grand Cereals', N'Mrs. Folake Adebayo', N'+2348041234567', N'oilsupply@prescoplc.ng', N'Vegetable Oils & Fats', N'Commercial Avenue, Yaba, Lagos', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Suppliers] WHERE [Name] = N'TotalEnergies Nigeria PLC')
    INSERT INTO [dbo].[Suppliers] ([Name], [ContactPerson], [PhoneNumber], [Email], [SupplyCategory], [Address], [IsActive])
    VALUES (N'TotalEnergies Nigeria PLC', N'Chinedu Eze', N'+2348099881122', N'fleet.orders@totalenergies.ng', N'Fuel & Lubricants', N'Marina, Lagos Island, Lagos', 1);

DECLARE @SupFlour INT = (SELECT [Id] FROM [dbo].[Suppliers] WHERE [Name] = N'Flour Mills of Nigeria PLC (Golden Penny)');
DECLARE @SupSugar INT = (SELECT [Id] FROM [dbo].[Suppliers] WHERE [Name] = N'Dangote Sugar Refinery');
DECLARE @SupPackaging INT = (SELECT [Id] FROM [dbo].[Suppliers] WHERE [Name] = N'FlexiPack Ltd (Bakery Packaging)');
DECLARE @SupYeast INT = (SELECT [Id] FROM [dbo].[Suppliers] WHERE [Name] = N'Lesaffre / Saf-Instant Yeast West Africa');
DECLARE @SupWater INT = (SELECT [Id] FROM [dbo].[Suppliers] WHERE [Name] = N'AquaPure Industrial Systems');
DECLARE @SupGrains INT = (SELECT [Id] FROM [dbo].[Suppliers] WHERE [Name] = N'SeedCo Grains & Agro Allied');
DECLARE @SupOils INT = (SELECT [Id] FROM [dbo].[Suppliers] WHERE [Name] = N'Presco PLC / Grand Cereals');
DECLARE @SupFuel INT = (SELECT [Id] FROM [dbo].[Suppliers] WHERE [Name] = N'TotalEnergies Nigeria PLC');

/* Raw materials */
IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-FLR-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'RAW-FLR-01', N'Golden Penny Wheat Flour', N'Raw Materials', N'Bags (50kg)', 45, 20, 55000, @SupFlour, CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-SGR-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'RAW-SGR-01', N'Dangote Refined Sugar', N'Raw Materials', N'Bags (50kg)', 12, 10, 72500, @SupSugar, CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-YST-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'RAW-YST-01', N'Saf-Instant Dry Yeast (Carton)', N'Raw Materials', N'Cartons (10kg)', 6, 8, 38000, @SupYeast, CONVERT(DATE, DATEADD(DAY, -2, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-SHT-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'RAW-SHT-01', N'Golden Margarine / Shortening', N'Raw Materials', N'Buckets (15kg)', 18, 8, 33000, @SupFlour, CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-SLT-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'RAW-SLT-01', N'Dangote Pure Iodized Salt', N'Raw Materials', N'Bags (25kg)', 8, 5, 10000, @SupSugar, CONVERT(DATE, DATEADD(DAY, -3, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-WTR-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'RAW-WTR-01', N'Purified Process Water', N'Raw Materials', N'Litres', 5000, 1500, 10, @SupWater, CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-CRN-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'RAW-CRN-01', N'Popcorn Maize (Mota)', N'Raw Materials', N'Bags (50kg)', 10, 4, 82500, @SupGrains, CONVERT(DATE, DATEADD(DAY, -2, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-OIL-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'RAW-OIL-01', N'Vegetable Frying Oil', N'Raw Materials', N'Jerrycans (25L)', 8, 4, 60000, @SupOils, CONVERT(DATE, DATEADD(DAY, -2, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-SFT-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'RAW-SFT-01', N'Dough Softener / Improver', N'Raw Materials', N'Cartons (10kg)', 5, 2, 48000, @SupYeast, CONVERT(DATE, DATEADD(DAY, -4, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-FLV-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'RAW-FLV-01', N'Vanilla Flavour Concentrate', N'Raw Materials', N'Litres', 20, 6, 18000, @SupYeast, CONVERT(DATE, DATEADD(DAY, -3, SYSUTCDATETIME())), 1);

/* Packaging, including the five distinct water-line components */
IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-NYL-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'PKG-NYL-01', N'Jumbo Bread Printed Nylon Wrappers', N'Packaging', N'Bundles (1,000 pcs)', 25, 10, 12000, @SupPackaging, CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-NYL-02')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'PKG-NYL-02', N'Medium Bread Printed Nylon Wrappers', N'Packaging', N'Bundles (1,000 pcs)', 30, 10, 8000, @SupPackaging, CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-BTL-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'PKG-BTL-01', N'18.9L Dispenser Bottles', N'Packaging', N'Pieces', 1200, 300, 95, @SupWater, CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-CAP-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'PKG-CAP-01', N'Dispenser Bottle Caps', N'Packaging', N'Packs (1,000 pcs)', 30, 10, 4500, @SupWater, CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-SEA-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'PKG-SEA-01', N'Cap Sealing Lines', N'Packaging', N'Rolls', 40, 15, 4500, @SupPackaging, CONVERT(DATE, DATEADD(DAY, -2, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-OUT-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'PKG-OUT-01', N'Outer Packaging Lines', N'Packaging', N'Bundles', 35, 12, 7000, @SupPackaging, CONVERT(DATE, DATEADD(DAY, -2, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-LBL-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'PKG-LBL-01', N'Bottle Labels/Tags', N'Packaging', N'Packs (1,000 pcs)', 18, 6, 3500, @SupPackaging, CONVERT(DATE, DATEADD(DAY, -3, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-BAG-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'PKG-BAG-01', N'Popcorn Small Bag (Printed)', N'Packaging', N'Packs (500 pcs)', 20, 8, 3000, @SupPackaging, CONVERT(DATE, DATEADD(DAY, -2, SYSUTCDATETIME())), 1);

/* Additives and flavours */
IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'ADD-PRV-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'ADD-PRV-01', N'Calcium Propionate (Preservative)', N'Additive/Flavor', N'Cartons (25kg)', 3, 2, 112500, @SupYeast, CONVERT(DATE, DATEADD(DAY, -4, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'ADD-FLV-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'ADD-FLV-01', N'Milk Flavour Powder', N'Additive/Flavor', N'Cartons (10kg)', 4, 2, 95000, @SupYeast, CONVERT(DATE, DATEADD(DAY, -4, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'ADD-SEA-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'ADD-SEA-01', N'Popcorn Butter Seasoning Powder', N'Additive/Flavor', N'Cartons (10kg)', 5, 2, 62000, @SupYeast, CONVERT(DATE, DATEADD(DAY, -3, SYSUTCDATETIME())), 1);

/* Fuel */
IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'FUE-PMS-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'FUE-PMS-01', N'Premium Motor Spirit (PMS Petrol)', N'Fuel', N'Litres', 650, 250, 980, @SupFuel, CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'FUE-LPG-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'FUE-LPG-01', N'Liquefied Petroleum Gas (LPG)', N'Fuel', N'kg', 300, 100, 1200, @SupFuel, CONVERT(DATE, DATEADD(DAY, -2, SYSUTCDATETIME())), 1);

/* Utilities: diesel is retained here because it is bought and stored in bulk for generators. */
IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'UTL-DSL-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'UTL-DSL-01', N'Automotive Gas Oil (AGO Diesel Fuel)', N'Utilities', N'Litres', 800, 300, 1250, @SupFuel, CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'UTL-WTR-01')
    INSERT INTO [dbo].[InventoryItems] ([ItemCode], [Name], [Category], [Unit], [QuantityInStock], [ReorderThreshold], [UnitCostPrice], [PreferredSupplierId], [LastRestockedDate], [IsActive])
    VALUES (N'UTL-WTR-01', N'Water Treatment Chemicals', N'Utilities', N'Bags (25kg)', 120, 40, 3500, @SupWater, CONVERT(DATE, DATEADD(DAY, -2, SYSUTCDATETIME())), 1);

/* Store requests and issued lines */
DECLARE @ItemFlour INT = (SELECT [Id] FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-FLR-01');
DECLARE @ItemSugar INT = (SELECT [Id] FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-SGR-01');
DECLARE @ItemYeast INT = (SELECT [Id] FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-YST-01');
DECLARE @ItemJumboNylon INT = (SELECT [Id] FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-NYL-01');
DECLARE @ItemBottle INT = (SELECT [Id] FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-BTL-01');
DECLARE @ItemCap INT = (SELECT [Id] FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-CAP-01');
DECLARE @ItemSeal INT = (SELECT [Id] FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-SEA-01');
DECLARE @ItemOuter INT = (SELECT [Id] FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-OUT-01');
DECLARE @ItemLabel INT = (SELECT [Id] FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-LBL-01');
DECLARE @ItemMaize INT = (SELECT [Id] FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-CRN-01');
DECLARE @ItemOil INT = (SELECT [Id] FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'RAW-OIL-01');
DECLARE @ItemPopBag INT = (SELECT [Id] FROM [dbo].[InventoryItems] WHERE [ItemCode] = N'PKG-BAG-01');
DECLARE @RequestId INT;

IF NOT EXISTS (SELECT 1 FROM [dbo].[StoreRequests] WHERE [RequestNumber] = N'REQ-2026-001')
BEGIN
    INSERT INTO [dbo].[StoreRequests] ([RequestNumber], [RequestedBy], [Department], [Purpose], [Status], [RequestedAt], [ApprovedBy], [ApprovedAt])
    VALUES (N'REQ-2026-001', N'Chidinma Okoro (Production Mgr)', N'Bakery Production Floor', N'Morning Shift Batch #101 Mixing (400 Jumbo Loaves)', 3, DATEADD(HOUR, -4, SYSUTCDATETIME()), N'Musa Ibrahim (Store Mgr)', DATEADD(HOUR, -3, SYSUTCDATETIME()));
    SET @RequestId = CAST(SCOPE_IDENTITY() AS INT);
    INSERT INTO [dbo].[StoreRequestItems] ([StoreRequestId], [InventoryItemId], [ItemName], [Unit], [RequestedQuantity], [IssuedQuantity])
    VALUES
        (@RequestId, @ItemFlour, N'Golden Penny Wheat Flour', N'Bags (50kg)', 4, 4),
        (@RequestId, @ItemSugar, N'Dangote Refined Sugar', N'Bags (50kg)', 1, 1),
        (@RequestId, @ItemJumboNylon, N'Jumbo Bread Printed Nylon Wrappers', N'Bundles (1,000 pcs)', 1, 1);
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[StoreRequests] WHERE [RequestNumber] = N'REQ-2026-002')
BEGIN
    INSERT INTO [dbo].[StoreRequests] ([RequestNumber], [RequestedBy], [Department], [Purpose], [Status], [RequestedAt])
    VALUES (N'REQ-2026-002', N'Chidinma Okoro (Production Mgr)', N'Bakery Production Floor', N'Afternoon Shift Batch #102 Mixing', 0, DATEADD(MINUTE, -45, SYSUTCDATETIME()));
    SET @RequestId = CAST(SCOPE_IDENTITY() AS INT);
    INSERT INTO [dbo].[StoreRequestItems] ([StoreRequestId], [InventoryItemId], [ItemName], [Unit], [RequestedQuantity], [IssuedQuantity])
    VALUES
        (@RequestId, @ItemFlour, N'Golden Penny Wheat Flour', N'Bags (50kg)', 3, 0),
        (@RequestId, @ItemYeast, N'Saf-Instant Dry Yeast (Carton)', N'Cartons (10kg)', 1, 0);
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[StoreRequests] WHERE [RequestNumber] = N'REQ-2026-003')
BEGIN
    INSERT INTO [dbo].[StoreRequests] ([RequestNumber], [RequestedBy], [Department], [Purpose], [Status], [RequestedAt], [ApprovedBy], [ApprovedAt])
    VALUES (N'REQ-2026-003', N'Fatima Bello (Water Supervisor)', N'Water Bottling Line', N'Morning Run #04 Purified Dispenser Refilling (250 Bottles)', 3, DATEADD(HOUR, -2, SYSUTCDATETIME()), N'Musa Ibrahim (Store Mgr)', DATEADD(HOUR, -1, SYSUTCDATETIME()));
    SET @RequestId = CAST(SCOPE_IDENTITY() AS INT);
    INSERT INTO [dbo].[StoreRequestItems] ([StoreRequestId], [InventoryItemId], [ItemName], [Unit], [RequestedQuantity], [IssuedQuantity])
    VALUES
        (@RequestId, @ItemBottle, N'18.9L Dispenser Bottles', N'Pieces', 250, 250),
        (@RequestId, @ItemCap, N'Dispenser Bottle Caps', N'Packs (1,000 pcs)', 1, 1),
        (@RequestId, @ItemSeal, N'Cap Sealing Lines', N'Rolls', 2, 2),
        (@RequestId, @ItemOuter, N'Outer Packaging Lines', N'Bundles', 2, 2),
        (@RequestId, @ItemLabel, N'Bottle Labels/Tags', N'Packs (1,000 pcs)', 1, 1);
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[StoreRequests] WHERE [RequestNumber] = N'REQ-2026-004')
BEGIN
    INSERT INTO [dbo].[StoreRequests] ([RequestNumber], [RequestedBy], [Department], [Purpose], [Status], [RequestedAt], [ApprovedBy], [ApprovedAt])
    VALUES (N'REQ-2026-004', N'Blessing Etim (Snacks Supervisor)', N'Popcorn & Confectionery Section', N'Batch #05 Popcorn Cooking Run', 3, DATEADD(HOUR, -2, SYSUTCDATETIME()), N'Musa Ibrahim (Store Mgr)', DATEADD(HOUR, -1, SYSUTCDATETIME()));
    SET @RequestId = CAST(SCOPE_IDENTITY() AS INT);
    INSERT INTO [dbo].[StoreRequestItems] ([StoreRequestId], [InventoryItemId], [ItemName], [Unit], [RequestedQuantity], [IssuedQuantity])
    VALUES
        (@RequestId, @ItemMaize, N'Popcorn Maize (Mota)', N'Bags (50kg)', 1, 1),
        (@RequestId, @ItemOil, N'Vegetable Frying Oil', N'Jerrycans (25L)', 1, 1),
        (@RequestId, @ItemPopBag, N'Popcorn Small Bag (Printed)', N'Packs (500 pcs)', 1, 1);
END;

/* Seed the historical stock ledger from the opening balances and the issued
   store requests above. New ledger rows are written by the application. */
IF NOT EXISTS (SELECT 1 FROM [dbo].[StockMovements])
BEGIN
    ;WITH IssuedTotals AS
    (
        SELECT ri.[InventoryItemId], SUM(ri.[IssuedQuantity]) AS [TotalIssued]
        FROM [dbo].[StoreRequestItems] ri
        INNER JOIN [dbo].[StoreRequests] r ON r.[Id] = ri.[StoreRequestId]
        WHERE r.[Status] = 3
        GROUP BY ri.[InventoryItemId]
    )
    INSERT INTO [dbo].[StockMovements]
        ([InventoryItemId], [MovementType], [Quantity], [BalanceAfter], [StoreRequestId], [ReferenceNumber], [Notes], [PerformedBy], [CreatedAt])
    SELECT
        i.[Id],
        N'Receipt',
        i.[QuantityInStock] + ISNULL(t.[TotalIssued], 0),
        i.[QuantityInStock] + ISNULL(t.[TotalIssued], 0),
        NULL,
        N'OPENING',
        N'Opening stock balance',
        N'Seed',
        DATEADD(DAY, -10, SYSUTCDATETIME())
    FROM [dbo].[InventoryItems] i
    LEFT JOIN IssuedTotals t ON t.[InventoryItemId] = i.[Id]
    WHERE i.[QuantityInStock] + ISNULL(t.[TotalIssued], 0) > 0;

    ;WITH IssuedTotals AS
    (
        SELECT ri.[InventoryItemId], SUM(ri.[IssuedQuantity]) AS [TotalIssued]
        FROM [dbo].[StoreRequestItems] ri
        INNER JOIN [dbo].[StoreRequests] r ON r.[Id] = ri.[StoreRequestId]
        WHERE r.[Status] = 3
        GROUP BY ri.[InventoryItemId]
    ),
    IssuedLines AS
    (
        SELECT
            ri.[InventoryItemId],
            r.[Id] AS [StoreRequestId],
            r.[RequestNumber],
            ri.[ItemName],
            ri.[Unit],
            ri.[IssuedQuantity],
            r.[ApprovedBy],
            COALESCE(r.[ApprovedAt], r.[RequestedAt]) AS [MovementAt],
            SUM(ri.[IssuedQuantity]) OVER
            (
                PARTITION BY ri.[InventoryItemId]
                ORDER BY COALESCE(r.[ApprovedAt], r.[RequestedAt]), ri.[Id]
                ROWS UNBOUNDED PRECEDING
            ) AS [RunningIssued],
            i.[QuantityInStock] + ISNULL(t.[TotalIssued], 0) AS [OpeningQty]
        FROM [dbo].[StoreRequestItems] ri
        INNER JOIN [dbo].[StoreRequests] r ON r.[Id] = ri.[StoreRequestId]
        INNER JOIN [dbo].[InventoryItems] i ON i.[Id] = ri.[InventoryItemId]
        LEFT JOIN IssuedTotals t ON t.[InventoryItemId] = ri.[InventoryItemId]
        WHERE r.[Status] = 3
          AND ri.[IssuedQuantity] > 0
    )
    INSERT INTO [dbo].[StockMovements]
        ([InventoryItemId], [MovementType], [Quantity], [BalanceAfter], [StoreRequestId], [ReferenceNumber], [Notes], [PerformedBy], [CreatedAt])
    SELECT
        [InventoryItemId],
        N'Issue',
        [IssuedQuantity],
        [OpeningQty] - [RunningIssued],
        [StoreRequestId],
        [RequestNumber],
        N'Approved store release (seed history)',
        ISNULL([ApprovedBy], N'Seed'),
        [MovementAt]
    FROM IssuedLines;
END;
GO

/* -------------------------------------------------------------------------
   Fleet seed data
   ------------------------------------------------------------------------- */


DECLARE @UsmanEmployeeId INT = (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-004');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'KJA-892-XA')
    INSERT INTO [dbo].[Vehicles]
        ([RegistrationNumber], [MakeAndModel], [VehicleType], [VehicleFunction], [AssignedEmployeeId],
         [FuelType], [TankCapacityLitres], [CurrentOdometerKm], [Status], [LastServiceDate], [IsActive])
    VALUES
        (N'KJA-892-XA', N'Toyota HiAce 2.7 Delivery Van', N'Van', N'Deliveries', @UsmanEmployeeId,
         N'Petrol', 70.00, 128400.00, N'Active', CONVERT(DATE, DATEADD(DAY, -20, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'LSR-441-YB')
    INSERT INTO [dbo].[Vehicles]
        ([RegistrationNumber], [MakeAndModel], [VehicleType], [VehicleFunction], [AssignedEmployeeId],
         [FuelType], [TankCapacityLitres], [CurrentOdometerKm], [Status], [LastServiceDate], [IsActive])
    VALUES
        (N'LSR-441-YB', N'MAN TGS 26.440 Box Truck', N'Truck', N'Commercial Haulage', NULL,
         N'Diesel', 300.00, 214000.00, N'Active', CONVERT(DATE, DATEADD(DAY, -12, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'EKY-319-ZC')
    INSERT INTO [dbo].[Vehicles]
        ([RegistrationNumber], [MakeAndModel], [VehicleType], [VehicleFunction], [AssignedEmployeeId],
         [FuelType], [TankCapacityLitres], [CurrentOdometerKm], [Status], [LastServiceDate], [IsActive])
    VALUES
        (N'EKY-319-ZC', N'Toyota Coaster Staff Bus', N'Bus', N'Staff Transport', NULL,
         N'Diesel', 120.00, 96500.00, N'Active', CONVERT(DATE, DATEADD(DAY, -35, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'APP-714-DX')
    INSERT INTO [dbo].[Vehicles]
        ([RegistrationNumber], [MakeAndModel], [VehicleType], [VehicleFunction], [AssignedEmployeeId],
         [FuelType], [TankCapacityLitres], [CurrentOdometerKm], [Status], [LastServiceDate], [IsActive])
    VALUES
        (N'APP-714-DX', N'Toyota Camry 2.5 Executive', N'Sedan', N'VIP Logistics', NULL,
         N'Petrol', 60.00, 54800.00, N'Active', CONVERT(DATE, DATEADD(DAY, -18, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'IKJ-520-YY')
    INSERT INTO [dbo].[Vehicles]
        ([RegistrationNumber], [MakeAndModel], [VehicleType], [VehicleFunction], [AssignedEmployeeId],
         [FuelType], [TankCapacityLitres], [CurrentOdometerKm], [Status], [LastServiceDate], [IsActive])
    VALUES
        (N'IKJ-520-YY', N'Toyota Land Cruiser Prado SUV', N'SUV', N'General Operations', NULL,
         N'Petrol', 90.00, 78200.00, N'Active', CONVERT(DATE, DATEADD(DAY, -9, SYSUTCDATETIME())), 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'EV-001-LG')
    INSERT INTO [dbo].[Vehicles]
        ([RegistrationNumber], [MakeAndModel], [VehicleType], [VehicleFunction], [AssignedEmployeeId],
         [FuelType], [TankCapacityLitres], [CurrentOdometerKm], [Status], [LastServiceDate], [IsActive])
    VALUES
        (N'EV-001-LG', N'BYD Atto 3 Electric Delivery', N'EV', N'Deliveries', NULL,
         N'EV Charging', 60.00, 12400.00, N'Active', CONVERT(DATE, DATEADD(DAY, -6, SYSUTCDATETIME())), 1);
GO

/* Fuel logs: current operating week */
DECLARE @UsmanEmployeeId INT = (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-004');
DECLARE @V1 INT = (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'KJA-892-XA');
DECLARE @V2 INT = (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'LSR-441-YB');
DECLARE @V3 INT = (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'EKY-319-ZC');
DECLARE @V4 INT = (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'APP-714-DX');
DECLARE @V5 INT = (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'IKJ-520-YY');
DECLARE @V6 INT = (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'EV-001-LG');

IF NOT EXISTS (SELECT 1 FROM [dbo].[FuelLogs] WHERE [VehicleId] = @V1 AND CONVERT(DATE, [FuelDate]) = CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())))
    INSERT INTO [dbo].[FuelLogs] ([VehicleId], [EmployeeId], [FuelDate], [FuelType], [LitresDispensed], [PricePerLitre], [PetrolStation], [RouteDescription])
    VALUES (@V1, @UsmanEmployeeId, DATEADD(DAY, -1, SYSUTCDATETIME()), N'Petrol', 45.00, 980.00, N'NNPC Mega Station, Ikeja', N'Ikeja & Ojota delivery route');

IF NOT EXISTS (SELECT 1 FROM [dbo].[FuelLogs] WHERE [VehicleId] = @V2 AND CONVERT(DATE, [FuelDate]) = CONVERT(DATE, DATEADD(DAY, -2, SYSUTCDATETIME())))
    INSERT INTO [dbo].[FuelLogs] ([VehicleId], [EmployeeId], [FuelDate], [FuelType], [LitresDispensed], [PricePerLitre], [PetrolStation], [RouteDescription])
    VALUES (@V2, @UsmanEmployeeId, DATEADD(DAY, -2, SYSUTCDATETIME()), N'Diesel', 120.00, 1250.00, N'TotalEnergies Apapa', N'Lagos to Ibadan wholesale route');

IF NOT EXISTS (SELECT 1 FROM [dbo].[FuelLogs] WHERE [VehicleId] = @V3 AND CONVERT(DATE, [FuelDate]) = CONVERT(DATE, DATEADD(DAY, -3, SYSUTCDATETIME())))
    INSERT INTO [dbo].[FuelLogs] ([VehicleId], [EmployeeId], [FuelDate], [FuelType], [LitresDispensed], [PricePerLitre], [PetrolStation], [RouteDescription])
    VALUES (@V3, @UsmanEmployeeId, DATEADD(DAY, -3, SYSUTCDATETIME()), N'Diesel', 80.00, 1250.00, N'NNPC Retail Ojota', N'Staff shuttle route');

IF NOT EXISTS (SELECT 1 FROM [dbo].[FuelLogs] WHERE [VehicleId] = @V4 AND CONVERT(DATE, [FuelDate]) = CONVERT(DATE, DATEADD(DAY, -4, SYSUTCDATETIME())))
    INSERT INTO [dbo].[FuelLogs] ([VehicleId], [EmployeeId], [FuelDate], [FuelType], [LitresDispensed], [PricePerLitre], [PetrolStation], [RouteDescription])
    VALUES (@V4, @UsmanEmployeeId, DATEADD(DAY, -4, SYSUTCDATETIME()), N'Petrol', 35.00, 980.00, N'Mobil Victoria Island', N'VIP airport transfer');

IF NOT EXISTS (SELECT 1 FROM [dbo].[FuelLogs] WHERE [VehicleId] = @V5 AND CONVERT(DATE, [FuelDate]) = CONVERT(DATE, DATEADD(DAY, -5, SYSUTCDATETIME())))
    INSERT INTO [dbo].[FuelLogs] ([VehicleId], [EmployeeId], [FuelDate], [FuelType], [LitresDispensed], [PricePerLitre], [PetrolStation], [RouteDescription])
    VALUES (@V5, @UsmanEmployeeId, DATEADD(DAY, -5, SYSUTCDATETIME()), N'Petrol', 60.00, 980.00, N'NNPC Mega Station, Lekki', N'General operations route');

IF NOT EXISTS (SELECT 1 FROM [dbo].[FuelLogs] WHERE [VehicleId] = @V6 AND CONVERT(DATE, [FuelDate]) = CONVERT(DATE, DATEADD(DAY, -6, SYSUTCDATETIME())))
    INSERT INTO [dbo].[FuelLogs] ([VehicleId], [EmployeeId], [FuelDate], [FuelType], [LitresDispensed], [PricePerLitre], [PetrolStation], [RouteDescription])
    VALUES (@V6, @UsmanEmployeeId, DATEADD(DAY, -6, SYSUTCDATETIME()), N'EV Charging', 50.00, 350.00, N'GreenCharge Hub, Lekki', N'Electric delivery route');

/* Maintenance logs for weekly ledger */
IF NOT EXISTS (SELECT 1 FROM [dbo].[MaintenanceLogs] WHERE [InvoiceNumber] = N'SVC-2026-001')
    INSERT INTO [dbo].[MaintenanceLogs] ([VehicleId], [ServiceDate], [Description], [Cost], [WorkshopOrVendor], [InvoiceNumber])
    VALUES (@V1, CONVERT(DATE, DATEADD(DAY, -2, SYSUTCDATETIME())), N'Engine oil, filters and brake inspection', 25000.00, N'AutoMedic Ikeja', N'SVC-2026-001');

IF NOT EXISTS (SELECT 1 FROM [dbo].[MaintenanceLogs] WHERE [InvoiceNumber] = N'SVC-2026-002')
    INSERT INTO [dbo].[MaintenanceLogs] ([VehicleId], [ServiceDate], [Description], [Cost], [WorkshopOrVendor], [InvoiceNumber])
    VALUES (@V2, CONVERT(DATE, DATEADD(DAY, -4, SYSUTCDATETIME())), N'Gearbox service and tyre rotation', 80000.00, N'MAN Service Centre', N'SVC-2026-002');

IF NOT EXISTS (SELECT 1 FROM [dbo].[MaintenanceLogs] WHERE [InvoiceNumber] = N'SVC-2026-003')
    INSERT INTO [dbo].[MaintenanceLogs] ([VehicleId], [ServiceDate], [Description], [Cost], [WorkshopOrVendor], [InvoiceNumber])
    VALUES (@V3, CONVERT(DATE, DATEADD(DAY, -5, SYSUTCDATETIME())), N'Air conditioning and suspension check', 45000.00, N'Coaster Care Lagos', N'SVC-2026-003');

/* Statewide route pricing matrix */
MERGE [dbo].[RouteRates] AS target
USING (VALUES
    (N'Lagos', N'Rivers (Port Harcourt)', N'30-Ton Haulage Truck', 1850000.00, 1350000.00, 18),
    (N'Lagos', N'Kano (Dawanau Grain Corridor)', N'30-Ton Haulage Truck', 2200000.00, 1550000.00, 24),
    (N'Lagos', N'Abuja (FCT Commercial)', N'15-Ton Box Truck', 1400000.00, 1050000.00, 14),
    (N'Lagos', N'Edo (Benin City Hub)', N'15-Ton Box Truck', 750000.00, 550000.00, 7),
    (N'Lagos', N'Anambra (Onitsha Main Market)', N'30-Ton Haulage Truck', 1600000.00, 1200000.00, 12),
    (N'Lagos', N'Delta (Warri Energy Route)', N'30-Ton Haulage Truck', 1450000.00, 1100000.00, 11),
    (N'Lagos', N'Oyo (Ibadan Wholesale Hub)', N'Delivery Van', 280000.00, 210000.00, 3),
    (N'Edo (Benin City Hub)', N'Rivers (Port Harcourt)', N'30-Ton Haulage Truck', 950000.00, 700000.00, 7),
    (N'Ogun (Agbara)', N'Kaduna (Industrial Zone)', N'30-Ton Haulage Truck', 1950000.00, 1400000.00, 20),
    (N'Lagos', N'Abia (Aba Commercial Depot)', N'15-Ton Box Truck', 1500000.00, 1150000.00, 14)
) AS source ([OriginState], [DestinationState], [VehicleTypeRequired], [RateWithFuel], [RateWithoutFuel], [EstimatedHours])
ON target.[OriginState] = source.[OriginState]
   AND target.[DestinationState] = source.[DestinationState]
   AND target.[VehicleTypeRequired] = source.[VehicleTypeRequired]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([OriginState], [DestinationState], [VehicleTypeRequired], [RateWithFuel], [RateWithoutFuel], [EstimatedHours], [IsActive])
    VALUES (source.[OriginState], source.[DestinationState], source.[VehicleTypeRequired], source.[RateWithFuel], source.[RateWithoutFuel], source.[EstimatedHours], 1);

/* Charter bookings across all three statuses */

IF NOT EXISTS (SELECT 1 FROM [dbo].[CharterBookings] WHERE [BookingRef] = N'CHT-2026-001')
BEGIN
    INSERT INTO [dbo].[CharterBookings]
        ([BookingRef], [CustomerName], [CustomerPhone], [VehicleId], [DriverEmployeeId], [DriverName],
         [DriverAllowance], [SecurityEscortFee], [TollsAndRoadLevies], [GrossCharterFee], [TripStatus],
         [DepartureDate], [ReturnDate], [Notes])
    VALUES
        (N'CHT-2026-001', N'Lagos Foods Distribution Ltd', N'+2348021114444', @V1, @UsmanEmployeeId, N'Usman Danjuma',
         80000.00, 120000.00, 35000.00, 280000.00, 0,
         CONVERT(DATE, DATEADD(DAY, 2, SYSUTCDATETIME())), CONVERT(DATE, DATEADD(DAY, 3, SYSUTCDATETIME())),
         N'Booked commercial delivery charter');

    INSERT INTO [dbo].[TripLegs] ([CharterBookingId], [LegNumber], [OriginState], [DestinationState], [FuelOption], [LegCost])
    SELECT [Id], 1, N'Lagos', N'Oyo (Ibadan Wholesale Hub)', 0, 280000.00
    FROM [dbo].[CharterBookings] WHERE [BookingRef] = N'CHT-2026-001';
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[CharterBookings] WHERE [BookingRef] = N'CHT-2026-002')
BEGIN
    INSERT INTO [dbo].[CharterBookings]
        ([BookingRef], [CustomerName], [CustomerPhone], [VehicleId], [DriverEmployeeId], [DriverName],
         [DriverAllowance], [SecurityEscortFee], [TollsAndRoadLevies], [GrossCharterFee], [TripStatus],
         [DepartureDate], [ReturnDate], [Notes])
    VALUES
        (N'CHT-2026-002', N'Port Harcourt Retail Hub', N'+2348032225555', @V4, @UsmanEmployeeId, N'Usman Danjuma',
         150000.00, 200000.00, 60000.00, 1850000.00, 1,
         CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())), CONVERT(DATE, DATEADD(DAY, 1, SYSUTCDATETIME())),
         N'In-transit interstate charter');

    INSERT INTO [dbo].[TripLegs] ([CharterBookingId], [LegNumber], [OriginState], [DestinationState], [FuelOption], [LegCost])
    SELECT [Id], 1, N'Lagos', N'Rivers (Port Harcourt)', 0, 1850000.00
    FROM [dbo].[CharterBookings] WHERE [BookingRef] = N'CHT-2026-002';
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[CharterBookings] WHERE [BookingRef] = N'CHT-2026-003')
BEGIN
    INSERT INTO [dbo].[CharterBookings]
        ([BookingRef], [CustomerName], [CustomerPhone], [VehicleId], [DriverEmployeeId], [DriverName],
         [DriverAllowance], [SecurityEscortFee], [TollsAndRoadLevies], [GrossCharterFee], [TripStatus],
         [DepartureDate], [ReturnDate], [Notes])
    VALUES
        (N'CHT-2026-003', N'Abuja Commercial Accounts', N'+2348043336666', @V2, @UsmanEmployeeId, N'Usman Danjuma',
         90000.00, 150000.00, 45000.00, 1400000.00, 2,
         CONVERT(DATE, DATEADD(DAY, -10, SYSUTCDATETIME())), CONVERT(DATE, DATEADD(DAY, -8, SYSUTCDATETIME())),
         N'Completed commercial route');

    INSERT INTO [dbo].[TripLegs] ([CharterBookingId], [LegNumber], [OriginState], [DestinationState], [FuelOption], [LegCost])
    SELECT [Id], 1, N'Lagos', N'Abuja (FCT Commercial)', 0, 1400000.00
    FROM [dbo].[CharterBookings] WHERE [BookingRef] = N'CHT-2026-003';
END;
GO

/* -------------------------------------------------------------------------
   Sales & POS seed data
   ------------------------------------------------------------------------- */

/* Customers */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [MemberCode] = N'WLK-001')
    INSERT INTO [dbo].[Customers] ([Name], [MemberCode], [Category], [PhoneNumber], [Address], [OutstandingBalance], [IsActive])
    VALUES (N'General Walk-in Counter', N'WLK-001', 0, N'N/A', N'Bakery Front Desk', 0, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [MemberCode] = N'SUP-101')
    INSERT INTO [dbo].[Customers] ([Name], [MemberCode], [Category], [PhoneNumber], [Address], [OutstandingBalance], [IsActive])
    VALUES (N'Goodness Supermarket Ikeja', N'SUP-101', 3, N'+2348033221100', N'12 Oba Akran Way, Ikeja', 107000, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [MemberCode] = N'SUP-102')
    INSERT INTO [dbo].[Customers] ([Name], [MemberCode], [Category], [PhoneNumber], [Address], [OutstandingBalance], [IsActive])
    VALUES (N'Grand Square Supermarket VI', N'SUP-102', 3, N'+2348055443322', N'Victoria Island, Lagos', 42500, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [MemberCode] = N'CUS-201')
    INSERT INTO [dbo].[Customers] ([Name], [MemberCode], [Category], [PhoneNumber], [Address], [OutstandingBalance], [IsActive])
    VALUES (N'Alhaja Basirat Bread Depot', N'CUS-201', 2, N'+2348099887766', N'Oshodi Market Shop 44', 0, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [MemberCode] = N'CUS-202')
    INSERT INTO [dbo].[Customers] ([Name], [MemberCode], [Category], [PhoneNumber], [Address], [OutstandingBalance], [IsActive])
    VALUES (N'Madam Ngozi Wholesalers', N'CUS-202', 2, N'+2348077665544', N'Agege Main Depot', 0, 1);

UPDATE [dbo].[Users]
SET [CustomerId] = (SELECT [Id] FROM [dbo].[Customers] WHERE [MemberCode] = N'SUP-101')
WHERE [Email] IN (N'vendor@bakery.com', N'customer@bakery.com');

/* Link matching seeded users to their HR employee records. */
UPDATE u
SET u.[EmployeeId] = e.[Id]
FROM [dbo].[Users] AS u
INNER JOIN [dbo].[Employees] AS e
    ON e.[FullName] = u.[Name]
   AND e.[IsDeleted] = 0
WHERE u.[EmployeeId] IS NULL;

DECLARE @CustWalkIn INT = (SELECT [Id] FROM [dbo].[Customers] WHERE [MemberCode] = N'WLK-001');
DECLARE @CustGoodness INT = (SELECT [Id] FROM [dbo].[Customers] WHERE [MemberCode] = N'SUP-101');
DECLARE @CustGrandSquare INT = (SELECT [Id] FROM [dbo].[Customers] WHERE [MemberCode] = N'SUP-102');
DECLARE @CustAlhaja INT = (SELECT [Id] FROM [dbo].[Customers] WHERE [MemberCode] = N'CUS-201');
DECLARE @CustNgozi INT = (SELECT [Id] FROM [dbo].[Customers] WHERE [MemberCode] = N'CUS-202');

/* POS terminals assigned to HR Sales employees */
IF NOT EXISTS (SELECT 1 FROM [dbo].[PosTerminals] WHERE [TerminalCode] = N'POS-01')
    INSERT INTO [dbo].[PosTerminals] ([TerminalCode], [TerminalName], [AssignedEmployeeId], [AssignedUserId], [CashAccountName], [TransferAccountName], [PosBankAccountName], [IsActive])
    VALUES (N'POS-01', N'Front Retail Register 1', (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-007'), (SELECT [Id] FROM [dbo].[Users] WHERE [Email] = N'sales@bakery.com'), N'Bakery Till #1 (Main Cash)', N'FirstBank PLC - 3021948291 (Bakery Ops)', N'Moniepoint POS #01 - 9028129', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[PosTerminals] WHERE [TerminalCode] = N'POS-02')
    INSERT INTO [dbo].[PosTerminals] ([TerminalCode], [TerminalName], [AssignedEmployeeId], [AssignedUserId], [CashAccountName], [TransferAccountName], [PosBankAccountName], [IsActive])
    VALUES (N'POS-02', N'Wholesale & Dispatch Register', (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-009'), NULL, N'Dispatch Till #2', N'Zenith Bank PLC - 1012938475 (Wholesale)', N'OPay Merchant POS - 8102938', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[PosTerminals] WHERE [TerminalCode] = N'POS-03')
    INSERT INTO [dbo].[PosTerminals] ([TerminalCode], [TerminalName], [AssignedEmployeeId], [AssignedUserId], [CashAccountName], [TransferAccountName], [PosBankAccountName], [IsActive])
    VALUES (N'POS-03', N'Van 1 Mobile Sales POS', (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-008'), NULL, N'Driver Float Cash', N'FirstBank PLC - 3021948291 (Bakery Ops)', N'PalmPay POS #03 - 7012948', 1);

DECLARE @Pos1 INT = (SELECT [Id] FROM [dbo].[PosTerminals] WHERE [TerminalCode] = N'POS-01');
DECLARE @Pos2 INT = (SELECT [Id] FROM [dbo].[PosTerminals] WHERE [TerminalCode] = N'POS-02');
DECLARE @Pos3 INT = (SELECT [Id] FROM [dbo].[PosTerminals] WHERE [TerminalCode] = N'POS-03');

/* Sales reps linked to HR employees */
IF NOT EXISTS (SELECT 1 FROM [dbo].[SalesReps] WHERE [RepCode] = N'REP-001')
    INSERT INTO [dbo].[SalesReps] ([RepCode], [EmployeeId], [UserId], [FullName], [PhoneNumber], [RouteName], [PosTerminalId], [VehicleId], [ProductFocus], [IsActive])
    VALUES (N'REP-001', (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-007'), (SELECT [Id] FROM [dbo].[Users] WHERE [Email] = N'sales@bakery.com'), N'Babatunde Alabi', N'+2348044445555', N'Ikeja & Ojota', @Pos1, (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'KJA-892-XA'), N'Bread & Water', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SalesReps] WHERE [RepCode] = N'REP-002')
    INSERT INTO [dbo].[SalesReps] ([RepCode], [EmployeeId], [UserId], [FullName], [PhoneNumber], [RouteName], [PosTerminalId], [VehicleId], [ProductFocus], [IsActive])
    VALUES (N'REP-002', (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-008'), NULL, N'Chiamaka Nwosu', N'+2348055556677', N'Lagos Island & Victoria Island', @Pos3, (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'KJA-892-XA'), N'Bread & Popcorn', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SalesReps] WHERE [RepCode] = N'REP-003')
    INSERT INTO [dbo].[SalesReps] ([RepCode], [EmployeeId], [UserId], [FullName], [PhoneNumber], [RouteName], [PosTerminalId], [VehicleId], [ProductFocus], [IsActive])
    VALUES (N'REP-003', (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-009'), NULL, N'Ibrahim Sanni', N'+2348066667788', N'Yaba & Surulere', @Pos2, (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'LSR-441-YB'), N'Water & Bread', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SalesReps] WHERE [RepCode] = N'REP-004')
    INSERT INTO [dbo].[SalesReps] ([RepCode], [EmployeeId], [UserId], [FullName], [PhoneNumber], [RouteName], [PosTerminalId], [VehicleId], [ProductFocus], [IsActive])
    VALUES (N'REP-004', (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-010'), NULL, N'Blessing Etim', N'+2348077778899', N'Maryland & Ikeja', @Pos2, (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'LSR-441-YB'), N'Popcorn & Water', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SalesReps] WHERE [RepCode] = N'REP-005')
    INSERT INTO [dbo].[SalesReps] ([RepCode], [EmployeeId], [UserId], [FullName], [PhoneNumber], [RouteName], [PosTerminalId], [VehicleId], [ProductFocus], [IsActive])
    VALUES (N'REP-005', (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-011'), NULL, N'Segun Adewale', N'+2348088889900', N'Agege & Ilupeju', @Pos2, (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'EKY-319-ZC'), N'Bread', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SalesReps] WHERE [RepCode] = N'REP-006')
    INSERT INTO [dbo].[SalesReps] ([RepCode], [EmployeeId], [UserId], [FullName], [PhoneNumber], [RouteName], [PosTerminalId], [VehicleId], [ProductFocus], [IsActive])
    VALUES (N'REP-006', (SELECT [Id] FROM [dbo].[Employees] WHERE [StaffCode] = N'EMP-012'), NULL, N'Halima Yusuf', N'+2348099990011', N'Oshodi & Isolo', @Pos3, (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'EKY-319-ZC'), N'Water & Popcorn', 1);

DECLARE @RepBabatunde INT = (SELECT [Id] FROM [dbo].[SalesReps] WHERE [RepCode] = N'REP-001');
DECLARE @RepChiamaka INT = (SELECT [Id] FROM [dbo].[SalesReps] WHERE [RepCode] = N'REP-002');
DECLARE @RepIbrahim INT = (SELECT [Id] FROM [dbo].[SalesReps] WHERE [RepCode] = N'REP-003');
DECLARE @RepSegun INT = (SELECT [Id] FROM [dbo].[SalesReps] WHERE [RepCode] = N'REP-005');
DECLARE @RepHalima INT = (SELECT [Id] FROM [dbo].[SalesReps] WHERE [RepCode] = N'REP-006');

DECLARE @ProductJumbo INT = (SELECT [Id] FROM [dbo].[Recipes] WHERE [Code] = N'BRD-JMB');
DECLARE @ProductMedium INT = (SELECT [Id] FROM [dbo].[Recipes] WHERE [Code] = N'BRD-MED');
DECLARE @Product61 INT = (SELECT [Id] FROM [dbo].[Recipes] WHERE [Code] = N'BRD-61');
DECLARE @ProductRound INT = (SELECT [Id] FROM [dbo].[Recipes] WHERE [Code] = N'BRD-RND');
DECLARE @Product6in1 INT = (SELECT [Id] FROM [dbo].[Recipes] WHERE [Code] = N'BRD-6IN1');
DECLARE @ProductWater INT = (SELECT [Id] FROM [dbo].[Recipes] WHERE [Code] = N'WTR-189');
DECLARE @ProductPopcorn INT = (SELECT [Id] FROM [dbo].[Recipes] WHERE [Code] = N'POP-40');

/* Orders: an older open order and a newer completed order are deliberately included. */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2026-001')
    INSERT INTO [dbo].[Orders] ([OrderNumber], [OrderType], [CustomerId], [CustomerName], [CustomerCategory], [SalesRepId], [SalesRepName], [PosTerminalId], [PosTerminalCode], [SubTotal], [Discount], [AmountPaid], [PaymentMethod], [PaymentStatus], [Status], [OrderDate], [TargetDeliveryDate], [IsPrintedForLoading], [PrintedAt], [VerifiedByLoaderName], [LoadedAt], [Notes])
    VALUES (N'ORD-2026-001', 0, @CustWalkIn, N'General Walk-in Counter', 0, @RepBabatunde, N'Babatunde Alabi', @Pos1, N'POS-01', 14000, 0, 14000, 0, 2, 4, DATEADD(DAY, -2, SYSUTCDATETIME()), DATEADD(DAY, -2, SYSUTCDATETIME()), 1, DATEADD(DAY, -2, SYSUTCDATETIME()), N'Kelechi Nnamdi', DATEADD(DAY, -2, SYSUTCDATETIME()), N'Completed walk-in counter sale');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2026-002')
    INSERT INTO [dbo].[Orders] ([OrderNumber], [OrderType], [CustomerId], [CustomerName], [CustomerCategory], [SalesRepId], [SalesRepName], [PosTerminalId], [PosTerminalCode], [SubTotal], [Discount], [AmountPaid], [PaymentMethod], [PaymentStatus], [Status], [OrderDate], [TargetDeliveryDate], [IsPrintedForLoading], [PrintedAt], [VerifiedByLoaderName], [LoadedAt], [Notes])
    VALUES (N'ORD-2026-002', 0, @CustAlhaja, N'Alhaja Basirat Bread Depot', 2, @RepSegun, N'Segun Adewale', @Pos2, N'POS-02', 140000, 5000, 135000, 1, 2, 4, DATEADD(DAY, -1, SYSUTCDATETIME()), DATEADD(DAY, -1, SYSUTCDATETIME()), 1, DATEADD(DAY, -1, SYSUTCDATETIME()), N'Kelechi Nnamdi', DATEADD(DAY, -1, SYSUTCDATETIME()), N'Completed wholesale depot order');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2026-003')
    INSERT INTO [dbo].[Orders] ([OrderNumber], [OrderType], [CustomerId], [CustomerName], [CustomerCategory], [SalesRepId], [SalesRepName], [PosTerminalId], [PosTerminalCode], [SubTotal], [Discount], [AmountPaid], [PaymentMethod], [PaymentStatus], [Status], [OrderDate], [TargetDeliveryDate], [IsPrintedForLoading], [Notes])
    VALUES (N'ORD-2026-003', 1, @CustGoodness, N'Goodness Supermarket Ikeja', 3, @RepChiamaka, N'Chiamaka Nwosu', @Pos3, N'POS-03', 157000, 0, 50000, 3, 1, 2, DATEADD(DAY, -10, SYSUTCDATETIME()), DATEADD(DAY, 1, SYSUTCDATETIME()), 0, N'Older supermarket order still open awaiting debt recovery');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2026-004')
    INSERT INTO [dbo].[Orders] ([OrderNumber], [OrderType], [CustomerId], [CustomerName], [CustomerCategory], [SalesRepId], [SalesRepName], [PosTerminalId], [PosTerminalCode], [SubTotal], [Discount], [AmountPaid], [PaymentMethod], [PaymentStatus], [Status], [OrderDate], [TargetDeliveryDate], [IsPrintedForLoading], [PrintedAt], [Notes])
    VALUES (N'ORD-2026-004', 1, @CustNgozi, N'Madam Ngozi Wholesalers', 2, @RepHalima, N'Halima Yusuf', @Pos3, N'POS-03', 99000, 2000, 97000, 2, 2, 3, DATEADD(HOUR, -5, SYSUTCDATETIME()), SYSUTCDATETIME(), 1, DATEADD(HOUR, -4, SYSUTCDATETIME()), N'Completed wholesale order ready to load');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2026-005')
    INSERT INTO [dbo].[Orders] ([OrderNumber], [OrderType], [CustomerId], [CustomerName], [CustomerCategory], [SalesRepId], [SalesRepName], [PosTerminalId], [PosTerminalCode], [SubTotal], [Discount], [AmountPaid], [PaymentMethod], [PaymentStatus], [Status], [OrderDate], [TargetDeliveryDate], [IsPrintedForLoading], [PrintedAt], [Notes])
    VALUES (N'ORD-2026-005', 1, @CustGrandSquare, N'Grand Square Supermarket VI', 3, @RepChiamaka, N'Chiamaka Nwosu', @Pos3, N'POS-03', 62500, 0, 20000, 3, 1, 4, DATEADD(DAY, -15, SYSUTCDATETIME()), DATEADD(DAY, -14, SYSUTCDATETIME()), 1, DATEADD(DAY, -15, SYSUTCDATETIME()), N'Historical order with outstanding debt');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2026-006')
    INSERT INTO [dbo].[Orders] ([OrderNumber], [OrderType], [CustomerId], [CustomerName], [CustomerCategory], [SalesRepId], [SalesRepName], [PosTerminalId], [PosTerminalCode], [SubTotal], [Discount], [AmountPaid], [PaymentMethod], [PaymentStatus], [Status], [OrderDate], [TargetDeliveryDate], [IsPrintedForLoading], [PrintedAt], [VerifiedByLoaderName], [LoadedAt], [Notes])
    VALUES (N'ORD-2026-006', 0, @CustGoodness, N'Goodness Supermarket Ikeja', 3, @RepChiamaka, N'Chiamaka Nwosu', @Pos3, N'POS-03', 24000, 0, 24000, 1, 2, 4, DATEADD(DAY, -1, SYSUTCDATETIME()), DATEADD(DAY, -1, SYSUTCDATETIME()), 1, DATEADD(DAY, -1, SYSUTCDATETIME()), N'Kelechi Nnamdi', DATEADD(DAY, -1, SYSUTCDATETIME()), N'Newer order fully paid while an older order remains open');

DECLARE @Order1 INT = (SELECT [Id] FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2026-001');
DECLARE @Order2 INT = (SELECT [Id] FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2026-002');
DECLARE @Order3 INT = (SELECT [Id] FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2026-003');
DECLARE @Order4 INT = (SELECT [Id] FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2026-004');
DECLARE @Order5 INT = (SELECT [Id] FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2026-005');
DECLARE @Order6 INT = (SELECT [Id] FROM [dbo].[Orders] WHERE [OrderNumber] = N'ORD-2026-006');

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderItems] WHERE [OrderId] = @Order1)
    INSERT INTO [dbo].[OrderItems] ([OrderId], [RecipeId], [ProductName], [Quantity], [BonusQuantity], [UnitPrice])
    VALUES (@Order1, @ProductJumbo, N'Jumbo Family Bread', 10, 1, 1400);

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderItems] WHERE [OrderId] = @Order2)
    INSERT INTO [dbo].[OrderItems] ([OrderId], [RecipeId], [ProductName], [Quantity], [BonusQuantity], [UnitPrice])
    VALUES (@Order2, @ProductJumbo, N'Jumbo Family Bread', 100, 6, 1400);

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderItems] WHERE [OrderId] = @Order3)
BEGIN
    INSERT INTO [dbo].[OrderItems] ([OrderId], [RecipeId], [ProductName], [Quantity], [BonusQuantity], [UnitPrice])
    VALUES (@Order3, @ProductJumbo, N'Jumbo Family Bread', 80, 6, 1400);
    INSERT INTO [dbo].[OrderItems] ([OrderId], [RecipeId], [ProductName], [Quantity], [BonusQuantity], [UnitPrice])
    VALUES (@Order3, @ProductMedium, N'Medium Loaf Bread', 50, 2, 900);
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderItems] WHERE [OrderId] = @Order4)
    INSERT INTO [dbo].[OrderItems] ([OrderId], [RecipeId], [ProductName], [Quantity], [BonusQuantity], [UnitPrice])
    VALUES (@Order4, @ProductMedium, N'Medium Loaf Bread', 110, 2, 900);

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderItems] WHERE [OrderId] = @Order5)
    INSERT INTO [dbo].[OrderItems] ([OrderId], [RecipeId], [ProductName], [Quantity], [BonusQuantity], [UnitPrice])
    VALUES (@Order5, @ProductWater, N'Purified Water 18.9L', 250, 2, 250);

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderItems] WHERE [OrderId] = @Order6)
    INSERT INTO [dbo].[OrderItems] ([OrderId], [RecipeId], [ProductName], [Quantity], [BonusQuantity], [UnitPrice])
    VALUES (@Order6, @ProductPopcorn, N'Popcorn 40g Bag', 120, 5, 200);

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [OrderId] = @Order1)
    INSERT INTO [dbo].[OrderPayments] ([OrderId], [Amount], [PaymentMethod], [PaymentDate], [Reference], [Notes], [ReceivedBy])
    VALUES (@Order1, 14000, 0, DATEADD(DAY, -2, SYSUTCDATETIME()), N'PAY-ORD-001', N'Full settlement at counter', N'Adekunle Johnson');

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [OrderId] = @Order2)
    INSERT INTO [dbo].[OrderPayments] ([OrderId], [Amount], [PaymentMethod], [PaymentDate], [Reference], [Notes], [ReceivedBy])
    VALUES (@Order2, 135000, 1, DATEADD(DAY, -1, SYSUTCDATETIME()), N'PAY-ORD-002', N'Bank transfer settlement', N'Adekunle Johnson');

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [OrderId] = @Order3)
    INSERT INTO [dbo].[OrderPayments] ([OrderId], [Amount], [PaymentMethod], [PaymentDate], [Reference], [Notes], [ReceivedBy])
    VALUES (@Order3, 50000, 3, DATEADD(DAY, -10, SYSUTCDATETIME()), N'PAY-ORD-003', N'Part payment on open supermarket order', N'Adekunle Johnson');

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [OrderId] = @Order4)
    INSERT INTO [dbo].[OrderPayments] ([OrderId], [Amount], [PaymentMethod], [PaymentDate], [Reference], [Notes], [ReceivedBy])
    VALUES (@Order4, 97000, 2, DATEADD(HOUR, -4, SYSUTCDATETIME()), N'PAY-ORD-004', N'POS card settlement', N'Adekunle Johnson');

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [OrderId] = @Order5)
    INSERT INTO [dbo].[OrderPayments] ([OrderId], [Amount], [PaymentMethod], [PaymentDate], [Reference], [Notes], [ReceivedBy])
    VALUES (@Order5, 20000, 3, DATEADD(DAY, -14, SYSUTCDATETIME()), N'PAY-ORD-005', N'Part payment on historical supermarket order', N'Adekunle Johnson');

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderPayments] WHERE [OrderId] = @Order6)
    INSERT INTO [dbo].[OrderPayments] ([OrderId], [Amount], [PaymentMethod], [PaymentDate], [Reference], [Notes], [ReceivedBy])
    VALUES (@Order6, 24000, 1, DATEADD(DAY, -1, SYSUTCDATETIME()), N'PAY-ORD-006', N'Full bank transfer settlement', N'Adekunle Johnson');

/* Supermarket accounts and agreed prices */
IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccounts] WHERE [CustomerId] = @CustGoodness)
    INSERT INTO [dbo].[SupermarketAccounts] ([CustomerId], [AccountName], [ContactPerson], [PhoneNumber], [Address], [AssignedRepId], [CurrentOutstandingCredit], [CreditLimit], [PaymentTerms], [IsActive])
    VALUES (@CustGoodness, N'Goodness Supermarket Ikeja', N'Mrs. Ngozi Okafor', N'+2348033221100', N'12 Oba Akran Way, Ikeja', @RepChiamaka, 107000, 200000, N'Net 15 Days', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccounts] WHERE [CustomerId] = @CustGrandSquare)
    INSERT INTO [dbo].[SupermarketAccounts] ([CustomerId], [AccountName], [ContactPerson], [PhoneNumber], [Address], [AssignedRepId], [CurrentOutstandingCredit], [CreditLimit], [PaymentTerms], [IsActive])
    VALUES (@CustGrandSquare, N'Grand Square Supermarket VI', N'Mr. Femi Adeyemi', N'+2348055443322', N'Victoria Island, Lagos', @RepChiamaka, 42500, 300000, N'Net 30 Days', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccounts] WHERE [CustomerId] = @CustAlhaja)
    INSERT INTO [dbo].[SupermarketAccounts] ([CustomerId], [AccountName], [ContactPerson], [PhoneNumber], [Address], [AssignedRepId], [CurrentOutstandingCredit], [CreditLimit], [PaymentTerms], [IsActive])
    VALUES (@CustAlhaja, N'Alhaja Basirat Bread Depot', N'Alhaja Basirat', N'+2348099887766', N'Oshodi Market Shop 44', @RepSegun, 0, 150000, N'Net 7 Days', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccounts] WHERE [CustomerId] = @CustNgozi)
    INSERT INTO [dbo].[SupermarketAccounts] ([CustomerId], [AccountName], [ContactPerson], [PhoneNumber], [Address], [AssignedRepId], [CurrentOutstandingCredit], [CreditLimit], [PaymentTerms], [IsActive])
    VALUES (@CustNgozi, N'Madam Ngozi Wholesalers', N'Madam Ngozi Eze', N'+2348077665544', N'Agege Main Depot', @RepHalima, 0, 100000, N'Net 14 Days', 1);

DECLARE @AcctGoodness INT = (SELECT [Id] FROM [dbo].[SupermarketAccounts] WHERE [CustomerId] = @CustGoodness);
DECLARE @AcctGrandSquare INT = (SELECT [Id] FROM [dbo].[SupermarketAccounts] WHERE [CustomerId] = @CustGrandSquare);
DECLARE @AcctAlhaja INT = (SELECT [Id] FROM [dbo].[SupermarketAccounts] WHERE [CustomerId] = @CustAlhaja);
DECLARE @AcctNgozi INT = (SELECT [Id] FROM [dbo].[SupermarketAccounts] WHERE [CustomerId] = @CustNgozi);

IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccountPrices] WHERE [SupermarketAccountId] = @AcctGoodness AND [RecipeId] = @ProductJumbo)
    INSERT INTO [dbo].[SupermarketAccountPrices] ([SupermarketAccountId], [RecipeId], [AgreedPrice]) VALUES (@AcctGoodness, @ProductJumbo, 1320);
IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccountPrices] WHERE [SupermarketAccountId] = @AcctGoodness AND [RecipeId] = @ProductMedium)
    INSERT INTO [dbo].[SupermarketAccountPrices] ([SupermarketAccountId], [RecipeId], [AgreedPrice]) VALUES (@AcctGoodness, @ProductMedium, 850);
IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccountPrices] WHERE [SupermarketAccountId] = @AcctGoodness AND [RecipeId] = @ProductWater)
    INSERT INTO [dbo].[SupermarketAccountPrices] ([SupermarketAccountId], [RecipeId], [AgreedPrice]) VALUES (@AcctGoodness, @ProductWater, 230);
IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccountPrices] WHERE [SupermarketAccountId] = @AcctGrandSquare AND [RecipeId] = @ProductJumbo)
    INSERT INTO [dbo].[SupermarketAccountPrices] ([SupermarketAccountId], [RecipeId], [AgreedPrice]) VALUES (@AcctGrandSquare, @ProductJumbo, 1300);
IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccountPrices] WHERE [SupermarketAccountId] = @AcctGrandSquare AND [RecipeId] = @Product61)
    INSERT INTO [dbo].[SupermarketAccountPrices] ([SupermarketAccountId], [RecipeId], [AgreedPrice]) VALUES (@AcctGrandSquare, @Product61, 1020);
IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccountPrices] WHERE [SupermarketAccountId] = @AcctGrandSquare AND [RecipeId] = @ProductPopcorn)
    INSERT INTO [dbo].[SupermarketAccountPrices] ([SupermarketAccountId], [RecipeId], [AgreedPrice]) VALUES (@AcctGrandSquare, @ProductPopcorn, 180);
IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccountPrices] WHERE [SupermarketAccountId] = @AcctAlhaja AND [RecipeId] = @ProductJumbo)
    INSERT INTO [dbo].[SupermarketAccountPrices] ([SupermarketAccountId], [RecipeId], [AgreedPrice]) VALUES (@AcctAlhaja, @ProductJumbo, 1250);
IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccountPrices] WHERE [SupermarketAccountId] = @AcctAlhaja AND [RecipeId] = @ProductMedium)
    INSERT INTO [dbo].[SupermarketAccountPrices] ([SupermarketAccountId], [RecipeId], [AgreedPrice]) VALUES (@AcctAlhaja, @ProductMedium, 800);
IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccountPrices] WHERE [SupermarketAccountId] = @AcctAlhaja AND [RecipeId] = @ProductRound)
    INSERT INTO [dbo].[SupermarketAccountPrices] ([SupermarketAccountId], [RecipeId], [AgreedPrice]) VALUES (@AcctAlhaja, @ProductRound, 760);
IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccountPrices] WHERE [SupermarketAccountId] = @AcctNgozi AND [RecipeId] = @ProductMedium)
    INSERT INTO [dbo].[SupermarketAccountPrices] ([SupermarketAccountId], [RecipeId], [AgreedPrice]) VALUES (@AcctNgozi, @ProductMedium, 820);
IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccountPrices] WHERE [SupermarketAccountId] = @AcctNgozi AND [RecipeId] = @ProductRound)
    INSERT INTO [dbo].[SupermarketAccountPrices] ([SupermarketAccountId], [RecipeId], [AgreedPrice]) VALUES (@AcctNgozi, @ProductRound, 770);
IF NOT EXISTS (SELECT 1 FROM [dbo].[SupermarketAccountPrices] WHERE [SupermarketAccountId] = @AcctNgozi AND [RecipeId] = @Product6in1)
    INSERT INTO [dbo].[SupermarketAccountPrices] ([SupermarketAccountId], [RecipeId], [AgreedPrice]) VALUES (@AcctNgozi, @Product6in1, 710);

/* Route sheets: ordered, loaded and reconciled examples */
IF NOT EXISTS (SELECT 1 FROM [dbo].[DailyRepRouteSheets] WHERE [RouteSheetRef] = N'RTE-2026-001')
    INSERT INTO [dbo].[DailyRepRouteSheets] ([RouteSheetRef], [RouteDate], [SalesRepId], [RouteName], [VehicleId], [VehicleRegistration], [PosTerminalId], [PosTerminalCode], [PreviousAccountBalance], [TotalExpectedSalesRevenue], [CashCollected], [BankTransferCollected], [PosTerminalCollected], [Status], [SignedOffByDispatcher], [SignedOffAt], [Notes])
    VALUES (N'RTE-2026-001', CONVERT(DATE, SYSUTCDATETIME()), @RepBabatunde, N'Ikeja & Ojota', (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'KJA-892-XA'), N'KJA-892-XA', @Pos1, N'POS-01', 22000, 106400, 75000, 15000, 5000, 1, N'Kelechi Nnamdi (Dispatch Mgr)', DATEADD(HOUR, -3, SYSUTCDATETIME()), N'Morning wholesale and supermarket dispatch.');

IF NOT EXISTS (SELECT 1 FROM [dbo].[DailyRepRouteSheets] WHERE [RouteSheetRef] = N'RTE-2026-002')
    INSERT INTO [dbo].[DailyRepRouteSheets] ([RouteSheetRef], [RouteDate], [SalesRepId], [RouteName], [VehicleId], [VehicleRegistration], [PosTerminalId], [PosTerminalCode], [PreviousAccountBalance], [Status], [Notes])
    VALUES (N'RTE-2026-002', CONVERT(DATE, SYSUTCDATETIME()), @RepChiamaka, N'Lagos Island & Victoria Island', (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'KJA-892-XA'), N'KJA-892-XA', @Pos3, N'POS-03', 10000, 0, N'Island route awaiting vehicle bay release.');

IF NOT EXISTS (SELECT 1 FROM [dbo].[DailyRepRouteSheets] WHERE [RouteSheetRef] = N'RTE-2026-003')
    INSERT INTO [dbo].[DailyRepRouteSheets] ([RouteSheetRef], [RouteDate], [SalesRepId], [RouteName], [VehicleId], [VehicleRegistration], [PosTerminalId], [PosTerminalCode], [PreviousAccountBalance], [TotalExpectedSalesRevenue], [CashCollected], [BankTransferCollected], [PosTerminalCollected], [Status], [SignedOffByDispatcher], [SignedOffAt], [ReconciledBy], [ReconciledAt], [Notes])
    VALUES (N'RTE-2026-003', DATEADD(DAY, -1, CONVERT(DATE, SYSUTCDATETIME())), @RepIbrahim, N'Yaba & Surulere', (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'LSR-441-YB'), N'LSR-441-YB', @Pos2, N'POS-02', 15000, 149500, 75000, 60000, 14500, 2, N'Kelechi Nnamdi (Dispatch Mgr)', DATEADD(DAY, -1, DATEADD(HOUR, 7, SYSUTCDATETIME())), N'Adekunle Johnson (SuperAdmin)', DATEADD(DAY, -1, DATEADD(HOUR, 14, SYSUTCDATETIME())), N'Yaba and Surulere route fully reconciled.');

DECLARE @Sheet1 INT = (SELECT [Id] FROM [dbo].[DailyRepRouteSheets] WHERE [RouteSheetRef] = N'RTE-2026-001');
DECLARE @Sheet2 INT = (SELECT [Id] FROM [dbo].[DailyRepRouteSheets] WHERE [RouteSheetRef] = N'RTE-2026-002');
DECLARE @Sheet3 INT = (SELECT [Id] FROM [dbo].[DailyRepRouteSheets] WHERE [RouteSheetRef] = N'RTE-2026-003');

IF NOT EXISTS (SELECT 1 FROM [dbo].[RouteSheetItems] WHERE [RouteSheetId] = @Sheet1 AND [RecipeId] = @ProductJumbo)
    INSERT INTO [dbo].[RouteSheetItems] ([RouteSheetId], [RecipeId], [ProductCode], [ProductName], [Category], [UnitSellingPrice], [RepOrderRequestedQty], [YesterdayClosingStock], [TodayNewlyLoadedQty], [QtySold], [ClosingStockQty], [QtyDamaged], [QtyReplacementIssued])
    VALUES (@Sheet1, @ProductJumbo, N'BRD-JMB', N'Jumbo Family Bread', 0, 1400, 98, 12, 88, 76, 20, 3, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[RouteSheetItems] WHERE [RouteSheetId] = @Sheet1 AND [RecipeId] = @ProductWater)
    INSERT INTO [dbo].[RouteSheetItems] ([RouteSheetId], [RecipeId], [ProductCode], [ProductName], [Category], [UnitSellingPrice], [RepOrderRequestedQty], [YesterdayClosingStock], [TodayNewlyLoadedQty], [QtySold], [ClosingStockQty], [QtyDamaged], [QtyReplacementIssued])
    VALUES (@Sheet1, @ProductWater, N'WTR-189', N'Purified Water 18.9L', 1, 250, 40, 0, 40, 35, 3, 2, 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[RouteSheetItems] WHERE [RouteSheetId] = @Sheet2 AND [RecipeId] = @ProductJumbo)
    INSERT INTO [dbo].[RouteSheetItems] ([RouteSheetId], [RecipeId], [ProductCode], [ProductName], [Category], [UnitSellingPrice], [RepOrderRequestedQty], [YesterdayClosingStock], [TodayNewlyLoadedQty])
    VALUES (@Sheet2, @ProductJumbo, N'BRD-JMB', N'Jumbo Family Bread', 0, 1400, 120, 10, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[RouteSheetItems] WHERE [RouteSheetId] = @Sheet2 AND [RecipeId] = @ProductPopcorn)
    INSERT INTO [dbo].[RouteSheetItems] ([RouteSheetId], [RecipeId], [ProductCode], [ProductName], [Category], [UnitSellingPrice], [RepOrderRequestedQty], [YesterdayClosingStock], [TodayNewlyLoadedQty])
    VALUES (@Sheet2, @ProductPopcorn, N'POP-40', N'Popcorn 40g Bag', 2, 200, 80, 5, 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[RouteSheetItems] WHERE [RouteSheetId] = @Sheet3 AND [RecipeId] = @ProductJumbo)
    INSERT INTO [dbo].[RouteSheetItems] ([RouteSheetId], [RecipeId], [ProductCode], [ProductName], [Category], [UnitSellingPrice], [RepOrderRequestedQty], [YesterdayClosingStock], [TodayNewlyLoadedQty], [QtySold], [ClosingStockQty], [QtyDamaged], [QtyReplacementIssued])
    VALUES (@Sheet3, @ProductJumbo, N'BRD-JMB', N'Jumbo Family Bread', 0, 1400, 80, 0, 80, 76, 3, 1, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[RouteSheetItems] WHERE [RouteSheetId] = @Sheet3 AND [RecipeId] = @ProductWater)
    INSERT INTO [dbo].[RouteSheetItems] ([RouteSheetId], [RecipeId], [ProductCode], [ProductName], [Category], [UnitSellingPrice], [RepOrderRequestedQty], [YesterdayClosingStock], [TodayNewlyLoadedQty], [QtySold], [ClosingStockQty], [QtyDamaged], [QtyReplacementIssued])
    VALUES (@Sheet3, @ProductWater, N'WTR-189', N'Purified Water 18.9L', 1, 250, 150, 0, 150, 142, 8, 0, 0);

/* Customer returns and replacements */
IF NOT EXISTS (SELECT 1 FROM [dbo].[CustomerReturns] WHERE [ReturnNumber] = N'RET-2026-001')
    INSERT INTO [dbo].[CustomerReturns] ([ReturnNumber], [OrderId], [CustomerId], [RecipeId], [SalesRepId], [Quantity], [UnitPrice], [ReplacementQuantity], [ReturnDate], [Reason], [ReplacementIssued], [Notes], [RecordedBy])
    VALUES (N'RET-2026-001', @Order2, @CustAlhaja, @ProductJumbo, @RepSegun, 5, 1400, 5, DATEADD(DAY, -1, SYSUTCDATETIME()), N'Mold / expired bread', 1, N'Replaced with fresh loaves from truck stock.', N'Adekunle Johnson');

IF NOT EXISTS (SELECT 1 FROM [dbo].[CustomerReturns] WHERE [ReturnNumber] = N'RET-2026-002')
    INSERT INTO [dbo].[CustomerReturns] ([ReturnNumber], [OrderId], [CustomerId], [RecipeId], [SalesRepId], [Quantity], [UnitPrice], [ReplacementQuantity], [ReturnDate], [Reason], [ReplacementIssued], [Notes], [RecordedBy])
    VALUES (N'RET-2026-002', @Order3, @CustGoodness, @ProductJumbo, @RepChiamaka, 3, 1400, 3, DATEADD(DAY, -2, SYSUTCDATETIME()), N'Damaged / moldy bread', 1, N'Replacement issued from current route stock.', N'Adekunle Johnson');
GO

/* -------------------------------------------------------------------------
   RBAC modules
   ------------------------------------------------------------------------- */

MERGE [dbo].[RbacModules] AS target
USING (VALUES
    (N'Recipes',   N'Recipe Builder & Unit Costing',      N'Ingredients, packaging formulas, margin targets & selling price',  10),
    (N'Production',N'Batch Production Logger',            N'Flour bags, target loaf count, actual yield & shift variance',      20),
    (N'Inventory', N'Raw Material Store & Requests',      N'Flour, sugar, yeast inventory, reorder thresholds & issuance',     30),
    (N'Sales',     N'POS Checkout & Cash Routing',        N'Direct counter sales, cashier drawer & multi-channel tender',      40),
    (N'PreOrders', N'Pre-Order Booking Portal',           N'Supermarket advance standing bookings & dispatch scheduling',      50),
    (N'Dispatch',  N'Print-to-Load Dispatch Slips',       N'Vehicle gate-pass security verification & loading ticket stamp',   60),
    (N'HR',        N'Staff & Payroll Engine',             N'Guarantors, salary advance deductions & monthly wage runs',        70),
    (N'Fleet',     N'Logistics & Fuel Logs',              N'Delivery vehicles, driver routes & fuel consumption receipts',     80),
    (N'Reports',   N'Executive Reports & Analytics',      N'Operational, financial and logistics reconciliation with exports', 90),
    (N'Settings',  N'System Settings & RBAC',             N'User accounts, terminal PINs, color themes & RBAC permissions',   100)
) AS source ([ModuleKey], [ModuleName], [Description], [SortOrder])
ON target.[ModuleKey] = source.[ModuleKey]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([ModuleKey], [ModuleName], [Description], [SortOrder])
    VALUES (source.[ModuleKey], source.[ModuleName], source.[Description], source.[SortOrder]);
GO

/* -------------------------------------------------------------------------
   RBAC permission levels and capability flags
   ------------------------------------------------------------------------- */

MERGE [dbo].[RbacPermissionLevels] AS target
USING (VALUES
    (N'None',              0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0),
    (N'View Only',         1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10),
    (N'Request Only',      2, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 20),
    (N'View, Edit',        3, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 30),
    (N'View, Issue, Edit', 4, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 40),
    (N'Create, Print',     5, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 50),
    (N'Book Own Order',    6, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 60),
    (N'Manage & Disburse', 7, 1, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 0, 70),
    (N'Full Access',       8, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 80)
) AS source
(
    [Name], [Rank], [CanView], [CanCreate], [CanEdit], [CanDelete], [CanApprove],
    [CanPrint], [CanManage], [CanRequest], [CanIssue], [CanDisburse], [CanBookOwn],
    [IsNone], [SortOrder]
)
ON target.[Name] = source.[Name]
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        [Name], [Rank], [CanView], [CanCreate], [CanEdit], [CanDelete], [CanApprove],
        [CanPrint], [CanManage], [CanRequest], [CanIssue], [CanDisburse], [CanBookOwn],
        [IsNone], [SortOrder]
    )
    VALUES
    (
        source.[Name], source.[Rank], source.[CanView], source.[CanCreate], source.[CanEdit],
        source.[CanDelete], source.[CanApprove], source.[CanPrint], source.[CanManage],
        source.[CanRequest], source.[CanIssue], source.[CanDisburse], source.[CanBookOwn],
        source.[IsNone], source.[SortOrder]
    );
GO

/* -------------------------------------------------------------------------
   Default role/module permission matrix
   ------------------------------------------------------------------------- */

MERGE [dbo].[RbacRolePermissions] AS target
USING
(
    SELECT m.[Id] AS [ModuleId], r.[Id] AS [RoleId], l.[Id] AS [PermissionLevelId]
    FROM (VALUES
        (N'Recipes',   N'SuperAdmin',        N'Full Access'),
        (N'Recipes',   N'ProductionManager', N'View, Edit'),
        (N'Recipes',   N'StoreManager',      N'View Only'),
        (N'Recipes',   N'SalesRep',          N'View Only'),
        (N'Recipes',   N'Customer',            N'None'),
        (N'Recipes',   N'HrPayrollManager',  N'None'),

        (N'Production',N'SuperAdmin',        N'Full Access'),
        (N'Production',N'ProductionManager', N'View, Edit'),
        (N'Production',N'StoreManager',      N'View Only'),
        (N'Production',N'SalesRep',          N'None'),
        (N'Production',N'Customer',            N'None'),
        (N'Production',N'HrPayrollManager',  N'None'),

        (N'Inventory', N'SuperAdmin',        N'Full Access'),
        (N'Inventory', N'ProductionManager', N'Request Only'),
        (N'Inventory', N'StoreManager',      N'View, Issue, Edit'),
        (N'Inventory', N'SalesRep',          N'None'),
        (N'Inventory', N'Customer',            N'None'),
        (N'Inventory', N'HrPayrollManager',  N'None'),

        (N'Sales',     N'SuperAdmin',        N'Full Access'),
        (N'Sales',     N'ProductionManager', N'None'),
        (N'Sales',     N'StoreManager',      N'None'),
        (N'Sales',     N'SalesRep',          N'Create, Print'),
        (N'Sales',     N'Customer',            N'None'),
        (N'Sales',     N'HrPayrollManager',  N'None'),

        (N'PreOrders', N'SuperAdmin',        N'Full Access'),
        (N'PreOrders', N'ProductionManager', N'View Only'),
        (N'PreOrders', N'StoreManager',      N'None'),
        (N'PreOrders', N'SalesRep',          N'Create, Print'),
        (N'PreOrders', N'Customer',            N'Book Own Order'),
        (N'PreOrders', N'HrPayrollManager',  N'None'),

        (N'Dispatch',  N'SuperAdmin',        N'Full Access'),
        (N'Dispatch',  N'ProductionManager', N'None'),
        (N'Dispatch',  N'StoreManager',      N'None'),
        (N'Dispatch',  N'SalesRep',          N'Create, Print'),
        (N'Dispatch',  N'Customer',            N'View Only'),
        (N'Dispatch',  N'HrPayrollManager',  N'None'),

        (N'HR',        N'SuperAdmin',        N'Full Access'),
        (N'HR',        N'ProductionManager', N'None'),
        (N'HR',        N'StoreManager',      N'None'),
        (N'HR',        N'SalesRep',          N'None'),
        (N'HR',        N'Customer',            N'None'),
        (N'HR',        N'HrPayrollManager',  N'Manage & Disburse'),

        (N'Fleet',     N'SuperAdmin',        N'Full Access'),
        (N'Fleet',     N'ProductionManager', N'View, Edit'),
        (N'Fleet',     N'StoreManager',      N'None'),
        (N'Fleet',     N'SalesRep',          N'None'),
        (N'Fleet',     N'Customer',            N'None'),
        (N'Fleet',     N'HrPayrollManager',  N'None'),

        (N'Reports',   N'SuperAdmin',        N'Full Access'),
        (N'Reports',   N'ProductionManager', N'View Only'),
        (N'Reports',   N'StoreManager',      N'View Only'),
        (N'Reports',   N'SalesRep',          N'View Only'),
        (N'Reports',   N'Customer',            N'None'),
        (N'Reports',   N'HrPayrollManager',  N'None'),

        (N'Settings',  N'SuperAdmin',        N'Full Access'),
        (N'Settings',  N'ProductionManager', N'None'),
        (N'Settings',  N'StoreManager',      N'None'),
        (N'Settings',  N'SalesRep',          N'None'),
        (N'Settings',  N'Customer',            N'None'),
        (N'Settings',  N'HrPayrollManager',  N'None')
    ) AS src ([ModuleKey], [RoleName], [LevelName])
    INNER JOIN [dbo].[RbacModules] m ON m.[ModuleKey] = src.[ModuleKey]
    INNER JOIN [dbo].[Roles] r ON r.[Name] = src.[RoleName]
    INNER JOIN [dbo].[RbacPermissionLevels] l ON l.[Name] = src.[LevelName]
) AS source
ON target.[RoleId] = source.[RoleId] AND target.[ModuleId] = source.[ModuleId]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([RoleId], [ModuleId], [PermissionLevelId], [UpdatedBy])
    VALUES (source.[RoleId], source.[ModuleId], source.[PermissionLevelId], N'Seed');
GO
