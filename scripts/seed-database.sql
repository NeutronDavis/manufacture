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
        vendor@bakery.com       PIN 555555   Vendor
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

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'Vendor')
    INSERT INTO [dbo].[Roles] ([Name], [DisplayName], [Description]) VALUES
    (N'Vendor', N'Vendor', N'External vendor / supermarket pre-orders portal.');

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
         (SELECT [Id] FROM [dbo].[Roles] WHERE [Name] = N'Vendor'),
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
   Fleet seed data
   ------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM [dbo].[Drivers] WHERE [FullName] = N'Usman Danjuma')
    INSERT INTO [dbo].[Drivers] ([FullName], [PhoneNumber], [LicenseNumber], [IsActive])
    VALUES (N'Usman Danjuma', N'+2348056789012', N'LAG-DRV-001', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Drivers] WHERE [FullName] = N'Ibrahim Yakubu')
    INSERT INTO [dbo].[Drivers] ([FullName], [PhoneNumber], [LicenseNumber], [IsActive])
    VALUES (N'Ibrahim Yakubu', N'+2348039871122', N'LAG-DRV-002', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Drivers] WHERE [FullName] = N'Ganiyu Muritala')
    INSERT INTO [dbo].[Drivers] ([FullName], [PhoneNumber], [LicenseNumber], [IsActive])
    VALUES (N'Ganiyu Muritala', N'+2348031199221', N'LAG-DRV-003', 1);

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
DECLARE @V1 INT = (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'KJA-892-XA');
DECLARE @V2 INT = (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'LSR-441-YB');
DECLARE @V3 INT = (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'EKY-319-ZC');
DECLARE @V4 INT = (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'APP-714-DX');
DECLARE @V5 INT = (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'IKJ-520-YY');
DECLARE @V6 INT = (SELECT [Id] FROM [dbo].[Vehicles] WHERE [RegistrationNumber] = N'EV-001-LG');

IF NOT EXISTS (SELECT 1 FROM [dbo].[FuelLogs] WHERE [VehicleId] = @V1 AND CONVERT(DATE, [FuelDate]) = CONVERT(DATE, DATEADD(DAY, -1, SYSUTCDATETIME())))
    INSERT INTO [dbo].[FuelLogs] ([VehicleId], [FuelDate], [FuelType], [LitresDispensed], [PricePerLitre], [PetrolStation], [RouteDescription])
    VALUES (@V1, DATEADD(DAY, -1, SYSUTCDATETIME()), N'Petrol', 45.00, 980.00, N'NNPC Mega Station, Ikeja', N'Ikeja & Ojota delivery route');

IF NOT EXISTS (SELECT 1 FROM [dbo].[FuelLogs] WHERE [VehicleId] = @V2 AND CONVERT(DATE, [FuelDate]) = CONVERT(DATE, DATEADD(DAY, -2, SYSUTCDATETIME())))
    INSERT INTO [dbo].[FuelLogs] ([VehicleId], [FuelDate], [FuelType], [LitresDispensed], [PricePerLitre], [PetrolStation], [RouteDescription])
    VALUES (@V2, DATEADD(DAY, -2, SYSUTCDATETIME()), N'Diesel', 120.00, 1250.00, N'TotalEnergies Apapa', N'Lagos to Ibadan wholesale route');

IF NOT EXISTS (SELECT 1 FROM [dbo].[FuelLogs] WHERE [VehicleId] = @V3 AND CONVERT(DATE, [FuelDate]) = CONVERT(DATE, DATEADD(DAY, -3, SYSUTCDATETIME())))
    INSERT INTO [dbo].[FuelLogs] ([VehicleId], [FuelDate], [FuelType], [LitresDispensed], [PricePerLitre], [PetrolStation], [RouteDescription])
    VALUES (@V3, DATEADD(DAY, -3, SYSUTCDATETIME()), N'Diesel', 80.00, 1250.00, N'NNPC Retail Ojota', N'Staff shuttle route');

IF NOT EXISTS (SELECT 1 FROM [dbo].[FuelLogs] WHERE [VehicleId] = @V4 AND CONVERT(DATE, [FuelDate]) = CONVERT(DATE, DATEADD(DAY, -4, SYSUTCDATETIME())))
    INSERT INTO [dbo].[FuelLogs] ([VehicleId], [FuelDate], [FuelType], [LitresDispensed], [PricePerLitre], [PetrolStation], [RouteDescription])
    VALUES (@V4, DATEADD(DAY, -4, SYSUTCDATETIME()), N'Petrol', 35.00, 980.00, N'Mobil Victoria Island', N'VIP airport transfer');

IF NOT EXISTS (SELECT 1 FROM [dbo].[FuelLogs] WHERE [VehicleId] = @V5 AND CONVERT(DATE, [FuelDate]) = CONVERT(DATE, DATEADD(DAY, -5, SYSUTCDATETIME())))
    INSERT INTO [dbo].[FuelLogs] ([VehicleId], [FuelDate], [FuelType], [LitresDispensed], [PricePerLitre], [PetrolStation], [RouteDescription])
    VALUES (@V5, DATEADD(DAY, -5, SYSUTCDATETIME()), N'Petrol', 60.00, 980.00, N'NNPC Mega Station, Lekki', N'General operations route');

IF NOT EXISTS (SELECT 1 FROM [dbo].[FuelLogs] WHERE [VehicleId] = @V6 AND CONVERT(DATE, [FuelDate]) = CONVERT(DATE, DATEADD(DAY, -6, SYSUTCDATETIME())))
    INSERT INTO [dbo].[FuelLogs] ([VehicleId], [FuelDate], [FuelType], [LitresDispensed], [PricePerLitre], [PetrolStation], [RouteDescription])
    VALUES (@V6, DATEADD(DAY, -6, SYSUTCDATETIME()), N'EV Charging', 50.00, 350.00, N'GreenCharge Hub, Lekki', N'Electric delivery route');

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
DECLARE @DriverUsman INT = (SELECT [Id] FROM [dbo].[Drivers] WHERE [FullName] = N'Usman Danjuma');
DECLARE @DriverIbrahim INT = (SELECT [Id] FROM [dbo].[Drivers] WHERE [FullName] = N'Ibrahim Yakubu');
DECLARE @DriverGaniyu INT = (SELECT [Id] FROM [dbo].[Drivers] WHERE [FullName] = N'Ganiyu Muritala');

IF NOT EXISTS (SELECT 1 FROM [dbo].[CharterBookings] WHERE [BookingRef] = N'CHT-2026-001')
BEGIN
    INSERT INTO [dbo].[CharterBookings]
        ([BookingRef], [CustomerName], [CustomerPhone], [VehicleId], [DriverId], [DriverName],
         [DriverAllowance], [SecurityEscortFee], [TollsAndRoadLevies], [GrossCharterFee], [TripStatus],
         [DepartureDate], [ReturnDate], [Notes])
    VALUES
        (N'CHT-2026-001', N'Lagos Foods Distribution Ltd', N'+2348021114444', @V1, @DriverUsman, N'Usman Danjuma',
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
        ([BookingRef], [CustomerName], [CustomerPhone], [VehicleId], [DriverId], [DriverName],
         [DriverAllowance], [SecurityEscortFee], [TollsAndRoadLevies], [GrossCharterFee], [TripStatus],
         [DepartureDate], [ReturnDate], [Notes])
    VALUES
        (N'CHT-2026-002', N'Port Harcourt Retail Hub', N'+2348032225555', @V4, @DriverIbrahim, N'Ibrahim Yakubu',
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
        ([BookingRef], [CustomerName], [CustomerPhone], [VehicleId], [DriverId], [DriverName],
         [DriverAllowance], [SecurityEscortFee], [TollsAndRoadLevies], [GrossCharterFee], [TripStatus],
         [DepartureDate], [ReturnDate], [Notes])
    VALUES
        (N'CHT-2026-003', N'Abuja Commercial Accounts', N'+2348043336666', @V2, @DriverGaniyu, N'Ganiyu Muritala',
         90000.00, 150000.00, 45000.00, 1400000.00, 2,
         CONVERT(DATE, DATEADD(DAY, -10, SYSUTCDATETIME())), CONVERT(DATE, DATEADD(DAY, -8, SYSUTCDATETIME())),
         N'Completed commercial route');

    INSERT INTO [dbo].[TripLegs] ([CharterBookingId], [LegNumber], [OriginState], [DestinationState], [FuelOption], [LegCost])
    SELECT [Id], 1, N'Lagos', N'Abuja (FCT Commercial)', 0, 1400000.00
    FROM [dbo].[CharterBookings] WHERE [BookingRef] = N'CHT-2026-003';
END;
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
        (N'Recipes',   N'Vendor',            N'None'),
        (N'Recipes',   N'HrPayrollManager',  N'None'),

        (N'Production',N'SuperAdmin',        N'Full Access'),
        (N'Production',N'ProductionManager', N'View, Edit'),
        (N'Production',N'StoreManager',      N'View Only'),
        (N'Production',N'SalesRep',          N'None'),
        (N'Production',N'Vendor',            N'None'),
        (N'Production',N'HrPayrollManager',  N'None'),

        (N'Inventory', N'SuperAdmin',        N'Full Access'),
        (N'Inventory', N'ProductionManager', N'Request Only'),
        (N'Inventory', N'StoreManager',      N'View, Issue, Edit'),
        (N'Inventory', N'SalesRep',          N'None'),
        (N'Inventory', N'Vendor',            N'None'),
        (N'Inventory', N'HrPayrollManager',  N'None'),

        (N'Sales',     N'SuperAdmin',        N'Full Access'),
        (N'Sales',     N'ProductionManager', N'None'),
        (N'Sales',     N'StoreManager',      N'None'),
        (N'Sales',     N'SalesRep',          N'Create, Print'),
        (N'Sales',     N'Vendor',            N'None'),
        (N'Sales',     N'HrPayrollManager',  N'None'),

        (N'PreOrders', N'SuperAdmin',        N'Full Access'),
        (N'PreOrders', N'ProductionManager', N'View Only'),
        (N'PreOrders', N'StoreManager',      N'None'),
        (N'PreOrders', N'SalesRep',          N'Create, Print'),
        (N'PreOrders', N'Vendor',            N'Book Own Order'),
        (N'PreOrders', N'HrPayrollManager',  N'None'),

        (N'Dispatch',  N'SuperAdmin',        N'Full Access'),
        (N'Dispatch',  N'ProductionManager', N'None'),
        (N'Dispatch',  N'StoreManager',      N'None'),
        (N'Dispatch',  N'SalesRep',          N'Create, Print'),
        (N'Dispatch',  N'Vendor',            N'View Only'),
        (N'Dispatch',  N'HrPayrollManager',  N'None'),

        (N'HR',        N'SuperAdmin',        N'Full Access'),
        (N'HR',        N'ProductionManager', N'None'),
        (N'HR',        N'StoreManager',      N'None'),
        (N'HR',        N'SalesRep',          N'None'),
        (N'HR',        N'Vendor',            N'None'),
        (N'HR',        N'HrPayrollManager',  N'Manage & Disburse'),

        (N'Fleet',     N'SuperAdmin',        N'Full Access'),
        (N'Fleet',     N'ProductionManager', N'View, Edit'),
        (N'Fleet',     N'StoreManager',      N'None'),
        (N'Fleet',     N'SalesRep',          N'None'),
        (N'Fleet',     N'Vendor',            N'None'),
        (N'Fleet',     N'HrPayrollManager',  N'None'),

        (N'Reports',   N'SuperAdmin',        N'Full Access'),
        (N'Reports',   N'ProductionManager', N'View Only'),
        (N'Reports',   N'StoreManager',      N'View Only'),
        (N'Reports',   N'SalesRep',          N'View Only'),
        (N'Reports',   N'Vendor',            N'None'),
        (N'Reports',   N'HrPayrollManager',  N'None'),

        (N'Settings',  N'SuperAdmin',        N'Full Access'),
        (N'Settings',  N'ProductionManager', N'None'),
        (N'Settings',  N'StoreManager',      N'None'),
        (N'Settings',  N'SalesRep',          N'None'),
        (N'Settings',  N'Vendor',            N'None'),
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
