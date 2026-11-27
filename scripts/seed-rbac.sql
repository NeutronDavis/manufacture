/* ============================================================================
   Manufacture - default RBAC seed (modules, permission levels, matrix)

   Idempotent and non-destructive. Run it once against an existing database to
   make the live permission matrix match Manufacture.Web/Services/RbacDefaults.cs.
   It merges:
     * 10 RbacModules by ModuleKey
     * 9  RbacPermissionLevels by Name
     * 60 RbacRolePermissions by (RoleId, ModuleId)

   Role IDs are the application contract: 1 SuperAdmin, 2 ProductionManager,
   3 StoreManager, 4 SalesRep, 5 Customer, 6 HrPayrollManager.

   Usage:
     sqlcmd -S <server> -d <database> -U <user> -P <password> -b -i scripts\seed-rbac.sql
   ============================================================================ */

SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF (SELECT COUNT(*) FROM [dbo].[Roles] WHERE [Id] BETWEEN 1 AND 6) <> 6
    THROW 50002, N'Role IDs 1-6 are required. Run the role seed or verify the Roles table before seeding RBAC.', 1;
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
   Keyed on role ID (dbo.Roles.Id), not role name.
   ------------------------------------------------------------------------- */

MERGE [dbo].[RbacRolePermissions] AS target
USING
(
    SELECT m.[Id] AS [ModuleId], r.[Id] AS [RoleId], l.[Id] AS [PermissionLevelId]
    FROM (VALUES
        (N'Recipes',   1, N'Full Access'),
        (N'Recipes',   2, N'View, Edit'),
        (N'Recipes',   3, N'View Only'),
        (N'Recipes',   4, N'None'),
        (N'Recipes',   5, N'None'),
        (N'Recipes',   6, N'None'),

        (N'Production',1, N'Full Access'),
        (N'Production',2, N'View, Edit'),
        (N'Production',3, N'View Only'),
        (N'Production',4, N'None'),
        (N'Production',5, N'None'),
        (N'Production',6, N'None'),

        (N'Inventory', 1, N'Full Access'),
        (N'Inventory', 2, N'Request Only'),
        (N'Inventory', 3, N'View, Issue, Edit'),
        (N'Inventory', 4, N'None'),
        (N'Inventory', 5, N'None'),
        (N'Inventory', 6, N'None'),

        (N'Sales',     1, N'Full Access'),
        (N'Sales',     2, N'None'),
        (N'Sales',     3, N'None'),
        (N'Sales',     4, N'Create, Print'),
        (N'Sales',     5, N'None'),
        (N'Sales',     6, N'None'),

        (N'PreOrders', 1, N'Full Access'),
        (N'PreOrders', 2, N'View Only'),
        (N'PreOrders', 3, N'None'),
        (N'PreOrders', 4, N'Create, Print'),
        (N'PreOrders', 5, N'Book Own Order'),
        (N'PreOrders', 6, N'None'),

        (N'Dispatch',  1, N'Full Access'),
        (N'Dispatch',  2, N'None'),
        (N'Dispatch',  3, N'None'),
        (N'Dispatch',  4, N'Create, Print'),
        (N'Dispatch',  5, N'View Only'),
        (N'Dispatch',  6, N'None'),

        (N'HR',        1, N'Full Access'),
        (N'HR',        2, N'None'),
        (N'HR',        3, N'None'),
        (N'HR',        4, N'None'),
        (N'HR',        5, N'None'),
        (N'HR',        6, N'Manage & Disburse'),

        (N'Fleet',     1, N'Full Access'),
        (N'Fleet',     2, N'View, Edit'),
        (N'Fleet',     3, N'None'),
        (N'Fleet',     4, N'None'),
        (N'Fleet',     5, N'None'),
        (N'Fleet',     6, N'None'),

        (N'Reports',   1, N'Full Access'),
        (N'Reports',   2, N'View Only'),
        (N'Reports',   3, N'View Only'),
        (N'Reports',   4, N'View Only'),
        (N'Reports',   5, N'None'),
        (N'Reports',   6, N'View Only'),

        (N'Settings',  1, N'Full Access'),
        (N'Settings',  2, N'None'),
        (N'Settings',  3, N'None'),
        (N'Settings',  4, N'None'),
        (N'Settings',  5, N'None'),
        (N'Settings',  6, N'None')
    ) AS src ([ModuleKey], [RoleId], [LevelName])
    INNER JOIN [dbo].[RbacModules] m ON m.[ModuleKey] = src.[ModuleKey]
    INNER JOIN [dbo].[Roles] r ON r.[Id] = src.[RoleId]
    INNER JOIN [dbo].[RbacPermissionLevels] l ON l.[Name] = src.[LevelName]
) AS source
ON target.[RoleId] = source.[RoleId] AND target.[ModuleId] = source.[ModuleId]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([RoleId], [ModuleId], [PermissionLevelId], [UpdatedBy])
    VALUES (source.[RoleId], source.[ModuleId], source.[PermissionLevelId], N'Seed')
WHEN MATCHED AND target.[PermissionLevelId] <> source.[PermissionLevelId] THEN
    UPDATE SET [PermissionLevelId] = source.[PermissionLevelId], [UpdatedBy] = N'Seed', [UpdatedAt] = SYSUTCDATETIME();
GO
PRINT N'RBAC defaults are in place (10 modules, 9 levels, 60 role/module cells).';
GO
