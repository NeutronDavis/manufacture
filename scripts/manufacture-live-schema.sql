/* ============================================================================
   Manufacture - FULL DATABASE RESET + schema + minimal seed data
   ----------------------------------------------------------------------------
   WARNING: DESTRUCTIVE. The first section drops every user view, foreign key
   and table currently in the connected database (plus leftover procedures,
   functions and synonyms), then recreates the full application schema and
   inserts:
     * all roles from the local development database
     * the SuperAdmin login only (no other demo users)

   HOW TO RUN (one command, against the live database)
     sqlcmd -S 104.40.192.145 -d Manufacture-DB -U SBUser -P <password> -b -i scripts\manufacture-live-schema.sql

   Or connect to the target database in SQL Server Management Studio / Azure
   Data Studio and execute the whole script. It has no USE / CREATE DATABASE
   statements, so it always applies to the database you are connected to.

   The script is repeatable: running it again wipes the existing tables and
   rebuilds them, so the database ends up exactly matching this file.

   DO NOT point this at a database that also holds data for another
   application - everything in it will be deleted.

   SUPERADMIN LOGIN (seeded below)
     Email    : admin@bakery.com
     Password : Password123!
     PIN      : 111111
   ============================================================================ */

SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

/* ============================================================================
   STEP 1 - REMOVE EVERYTHING THAT ALREADY EXISTS
   Drops, in dependency order:
     views -> foreign keys -> tables -> procedures -> functions -> synonyms
   Defaults, checks, indexes and primary keys are removed with their table.
   ============================================================================ */

DECLARE @dropSql NVARCHAR(MAX) = N'';
DECLARE @objectName NVARCHAR(300);

/* Schema-bound views must go before the tables they reference. */
DECLARE viewCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT QUOTENAME(SCHEMA_NAME([schema_id])) + N'.' + QUOTENAME([name])
    FROM sys.views
    WHERE [is_ms_shipped] = 0;

OPEN viewCursor;
FETCH NEXT FROM viewCursor INTO @objectName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @dropSql = @dropSql + N'DROP VIEW ' + @objectName + N';' + CHAR(10);
    FETCH NEXT FROM viewCursor INTO @objectName;
END;
CLOSE viewCursor;
DEALLOCATE viewCursor;

/* Foreign keys first so tables can be dropped in any order. */
DECLARE fkCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.[schema_id])) + N'.' + QUOTENAME(t.[name])
         + N' DROP CONSTRAINT ' + QUOTENAME(fk.[name])
    FROM sys.foreign_keys AS fk
    INNER JOIN sys.tables AS t ON t.[object_id] = fk.[parent_object_id]
    WHERE t.[is_ms_shipped] = 0;

OPEN fkCursor;
FETCH NEXT FROM fkCursor INTO @objectName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @dropSql = @dropSql + @objectName + N';' + CHAR(10);
    FETCH NEXT FROM fkCursor INTO @objectName;
END;
CLOSE fkCursor;
DEALLOCATE fkCursor;

IF @dropSql <> N''
    EXEC sp_executesql @dropSql;
GO

DECLARE @dropSql NVARCHAR(MAX) = N'';
DECLARE @objectName NVARCHAR(300);

/* Drop every user table (all schemas). */
DECLARE tableCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT QUOTENAME(SCHEMA_NAME([schema_id])) + N'.' + QUOTENAME([name])
    FROM sys.tables
    WHERE [is_ms_shipped] = 0;

OPEN tableCursor;
FETCH NEXT FROM tableCursor INTO @objectName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @dropSql = @dropSql + N'DROP TABLE ' + @objectName + N';' + CHAR(10);
    FETCH NEXT FROM tableCursor INTO @objectName;
END;
CLOSE tableCursor;
DEALLOCATE tableCursor;

/* Leftover programmable objects that reference the dropped tables. */
DECLARE procCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT QUOTENAME(SCHEMA_NAME([schema_id])) + N'.' + QUOTENAME([name])
    FROM sys.procedures
    WHERE [is_ms_shipped] = 0;

OPEN procCursor;
FETCH NEXT FROM procCursor INTO @objectName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @dropSql = @dropSql + N'DROP PROCEDURE ' + @objectName + N';' + CHAR(10);
    FETCH NEXT FROM procCursor INTO @objectName;
END;
CLOSE procCursor;
DEALLOCATE procCursor;

DECLARE functionCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT QUOTENAME(SCHEMA_NAME([schema_id])) + N'.' + QUOTENAME([name])
    FROM sys.objects
    WHERE [is_ms_shipped] = 0
      AND [type] IN (N'FN', N'IF', N'TF', N'FS', N'FT');

OPEN functionCursor;
FETCH NEXT FROM functionCursor INTO @objectName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @dropSql = @dropSql + N'DROP FUNCTION ' + @objectName + N';' + CHAR(10);
    FETCH NEXT FROM functionCursor INTO @objectName;
END;
CLOSE functionCursor;
DEALLOCATE functionCursor;

DECLARE aggregateCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT QUOTENAME(SCHEMA_NAME([schema_id])) + N'.' + QUOTENAME([name])
    FROM sys.objects
    WHERE [is_ms_shipped] = 0
      AND [type] = N'AF';

OPEN aggregateCursor;
FETCH NEXT FROM aggregateCursor INTO @objectName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @dropSql = @dropSql + N'DROP AGGREGATE ' + @objectName + N';' + CHAR(10);
    FETCH NEXT FROM aggregateCursor INTO @objectName;
END;
CLOSE aggregateCursor;
DEALLOCATE aggregateCursor;

DECLARE synonymCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT QUOTENAME(SCHEMA_NAME([schema_id])) + N'.' + QUOTENAME([name])
    FROM sys.synonyms
    WHERE [is_ms_shipped] = 0;

OPEN synonymCursor;
FETCH NEXT FROM synonymCursor INTO @objectName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @dropSql = @dropSql + N'DROP SYNONYM ' + @objectName + N';' + CHAR(10);
    FETCH NEXT FROM synonymCursor INTO @objectName;
END;
CLOSE synonymCursor;
DEALLOCATE synonymCursor;

IF @dropSql <> N''
    EXEC sp_executesql @dropSql;
GO

/* ============================================================================
   STEP 2 - CREATE THE APPLICATION SCHEMA
   ============================================================================ */

PRINT N'Creating Table [dbo].[AuditLogs]...';


GO
CREATE TABLE [dbo].[AuditLogs] (
    [Id]         BIGINT         IDENTITY (1, 1) NOT NULL,
    [UserId]     INT            NULL,
    [Action]     NVARCHAR (40)  NOT NULL,
    [EntityName] NVARCHAR (120) NOT NULL,
    [EntityId]   NVARCHAR (60)  NULL,
    [OldValues]  NVARCHAR (MAX) NULL,
    [NewValues]  NVARCHAR (MAX) NULL,
    [IpAddress]  NVARCHAR (64)  NULL,
    [OccurredAt] DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[AuditLogs].[IX_AuditLogs_EntityName_EntityId]...';


GO
CREATE NONCLUSTERED INDEX [IX_AuditLogs_EntityName_EntityId]
    ON [dbo].[AuditLogs]([EntityName] ASC, [EntityId] ASC);


GO
PRINT N'Creating Index [dbo].[AuditLogs].[IX_AuditLogs_OccurredAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_AuditLogs_OccurredAt]
    ON [dbo].[AuditLogs]([OccurredAt] DESC);


GO
PRINT N'Creating Table [dbo].[CharterBookings]...';


GO
CREATE TABLE [dbo].[CharterBookings] (
    [Id]                   INT             IDENTITY (1, 1) NOT NULL,
    [BookingRef]           NVARCHAR (40)   NOT NULL,
    [CustomerName]         NVARCHAR (150)  NOT NULL,
    [CustomerPhone]        NVARCHAR (30)   NOT NULL,
    [VehicleId]            INT             NOT NULL,
    [QuoteRequestId]       INT             NULL,
    [DriverEmployeeId]     INT             NULL,
    [DriverName]           NVARCHAR (150)  NOT NULL,
    [DriverAllowance]      DECIMAL (18, 2) NOT NULL,
    [SecurityEscortFee]    DECIMAL (18, 2) NOT NULL,
    [TollsAndRoadLevies]   DECIMAL (18, 2) NOT NULL,
    [TotalTripIncidentals] AS              ([DriverAllowance] + [SecurityEscortFee] + [TollsAndRoadLevies]) PERSISTED,
    [GrossCharterFee]      DECIMAL (18, 2) NOT NULL,
    [NetCharterRevenue]    AS              ([GrossCharterFee] - ([DriverAllowance] + [SecurityEscortFee] + [TollsAndRoadLevies])) PERSISTED,
    [TripStatus]           INT             NOT NULL,
    [DepartureDate]        DATE            NOT NULL,
    [ReturnDate]           DATE            NULL,
    [Notes]                NVARCHAR (500)  NOT NULL,
    [CreatedAt]            DATETIME2 (3)   NOT NULL,
    [UpdatedAt]            DATETIME2 (3)   NOT NULL,
    [RowVersion]           ROWVERSION      NOT NULL,
    CONSTRAINT [PK_CharterBookings] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_CharterBookings_BookingRef] UNIQUE NONCLUSTERED ([BookingRef] ASC)
);


GO
PRINT N'Creating Index [dbo].[CharterBookings].[UX_CharterBookings_QuoteRequestId]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_CharterBookings_QuoteRequestId]
    ON [dbo].[CharterBookings]([QuoteRequestId] ASC) WHERE [QuoteRequestId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[CharterBookings].[IX_CharterBookings_DepartureDate_Status]...';


GO
CREATE NONCLUSTERED INDEX [IX_CharterBookings_DepartureDate_Status]
    ON [dbo].[CharterBookings]([DepartureDate] DESC, [TripStatus] ASC);


GO
PRINT N'Creating Table [dbo].[CharterQuoteLegs]...';


GO
CREATE TABLE [dbo].[CharterQuoteLegs] (
    [Id]                    INT             IDENTITY (1, 1) NOT NULL,
    [CharterQuoteRequestId] INT             NOT NULL,
    [LegNumber]             INT             NOT NULL,
    [OriginAddress]         NVARCHAR (300)  NOT NULL,
    [OriginCityState]       NVARCHAR (120)  NOT NULL,
    [DestinationAddress]    NVARCHAR (300)  NOT NULL,
    [DestinationCityState]  NVARCHAR (120)  NOT NULL,
    [DistanceKm]            DECIMAL (18, 2) NOT NULL,
    [FuelOption]            INT             NOT NULL,
    [QuotedLegAmount]       DECIMAL (18, 2) NOT NULL,
    [Notes]                 NVARCHAR (300)  NULL,
    [CreatedAt]             DATETIME2 (3)   NOT NULL,
    [UpdatedAt]             DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_CharterQuoteLegs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_CharterQuoteLegs_Quote_Leg] UNIQUE NONCLUSTERED ([CharterQuoteRequestId] ASC, [LegNumber] ASC)
);


GO
PRINT N'Creating Index [dbo].[CharterQuoteLegs].[IX_CharterQuoteLegs_QuoteRequestId]...';


GO
CREATE NONCLUSTERED INDEX [IX_CharterQuoteLegs_QuoteRequestId]
    ON [dbo].[CharterQuoteLegs]([CharterQuoteRequestId] ASC);


GO
PRINT N'Creating Table [dbo].[CharterQuoteRequests]...';


GO
CREATE TABLE [dbo].[CharterQuoteRequests] (
    [Id]                   INT             IDENTITY (1, 1) NOT NULL,
    [QuoteRef]             NVARCHAR (40)   NOT NULL,
    [CustomerName]         NVARCHAR (150)  NOT NULL,
    [CustomerPhone]        NVARCHAR (30)   NOT NULL,
    [CustomerEmail]        NVARCHAR (200)  NULL,
    [RequestDate]          DATETIME2 (3)   NOT NULL,
    [TripType]             INT             NOT NULL,
    [VehicleTypeRequired]  NVARCHAR (60)   NOT NULL,
    [VehicleId]            INT             NULL,
    [TotalDistanceKm]      DECIMAL (18, 2) NOT NULL,
    [QuotedAmount]         DECIMAL (18, 2) NOT NULL,
    [DriverAllowance]      DECIMAL (18, 2) NOT NULL,
    [SecurityEscortFee]    DECIMAL (18, 2) NOT NULL,
    [TollsAndRoadLevies]   DECIMAL (18, 2) NOT NULL,
    [TotalTripIncidentals] AS              ([DriverAllowance] + [SecurityEscortFee] + [TollsAndRoadLevies]) PERSISTED,
    [NetEstimate]          AS              ([QuotedAmount] - ([DriverAllowance] + [SecurityEscortFee] + [TollsAndRoadLevies])) PERSISTED,
    [Status]               INT             NOT NULL,
    [ValidUntil]           DATE            NULL,
    [Notes]                NVARCHAR (1000) NOT NULL,
    [RequestedByUserId]    INT             NULL,
    [SubmittedAt]          DATETIME2 (3)   NULL,
    [ApprovedByUserId]     INT             NULL,
    [ApprovedAt]           DATETIME2 (3)   NULL,
    [RejectionReason]      NVARCHAR (500)  NULL,
    [CreatedAt]            DATETIME2 (3)   NOT NULL,
    [UpdatedAt]            DATETIME2 (3)   NOT NULL,
    [RowVersion]           ROWVERSION      NOT NULL,
    CONSTRAINT [PK_CharterQuoteRequests] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_CharterQuoteRequests_QuoteRef] UNIQUE NONCLUSTERED ([QuoteRef] ASC)
);


GO
PRINT N'Creating Index [dbo].[CharterQuoteRequests].[IX_CharterQuoteRequests_CustomerName]...';


GO
CREATE NONCLUSTERED INDEX [IX_CharterQuoteRequests_CustomerName]
    ON [dbo].[CharterQuoteRequests]([CustomerName] ASC);


GO
PRINT N'Creating Index [dbo].[CharterQuoteRequests].[IX_CharterQuoteRequests_Status_RequestDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_CharterQuoteRequests_Status_RequestDate]
    ON [dbo].[CharterQuoteRequests]([Status] ASC, [RequestDate] DESC);


GO
PRINT N'Creating Table [dbo].[CustomerReturns]...';


GO
CREATE TABLE [dbo].[CustomerReturns] (
    [Id]                  INT             IDENTITY (1, 1) NOT NULL,
    [ReturnNumber]        NVARCHAR (40)   NOT NULL,
    [OrderId]             INT             NULL,
    [CustomerId]          INT             NULL,
    [RecipeId]            INT             NOT NULL,
    [SalesRepId]          INT             NULL,
    [Quantity]            INT             NOT NULL,
    [UnitPrice]           DECIMAL (18, 2) NOT NULL,
    [RefundAmount]        AS              ([Quantity] * [UnitPrice]) PERSISTED,
    [ReplacementQuantity] INT             NOT NULL,
    [ReturnDate]          DATETIME2 (3)   NOT NULL,
    [Reason]              NVARCHAR (250)  NOT NULL,
    [ReplacementIssued]   BIT             NOT NULL,
    [Notes]               NVARCHAR (400)  NOT NULL,
    [RecordedBy]          NVARCHAR (150)  NOT NULL,
    [CreatedAt]           DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_CustomerReturns] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_CustomerReturns_ReturnNumber] UNIQUE NONCLUSTERED ([ReturnNumber] ASC)
);


GO
PRINT N'Creating Index [dbo].[CustomerReturns].[IX_CustomerReturns_RecipeId_ReturnDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_CustomerReturns_RecipeId_ReturnDate]
    ON [dbo].[CustomerReturns]([RecipeId] ASC, [ReturnDate] DESC);


GO
PRINT N'Creating Index [dbo].[CustomerReturns].[IX_CustomerReturns_SalesRepId_ReturnDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_CustomerReturns_SalesRepId_ReturnDate]
    ON [dbo].[CustomerReturns]([SalesRepId] ASC, [ReturnDate] DESC) WHERE [SalesRepId] IS NOT NULL;


GO
PRINT N'Creating Table [dbo].[Customers]...';


GO
CREATE TABLE [dbo].[Customers] (
    [Id]                 INT             IDENTITY (1, 1) NOT NULL,
    [Name]               NVARCHAR (150)  NOT NULL,
    [MemberCode]         NVARCHAR (40)   NOT NULL,
    [Category]           INT             NOT NULL,
    [PhoneNumber]        NVARCHAR (30)   NOT NULL,
    [Address]            NVARCHAR (300)  NOT NULL,
    [OutstandingBalance] DECIMAL (18, 2) NOT NULL,
    [IsActive]           BIT             NOT NULL,
    [CreatedAt]          DATETIME2 (3)   NOT NULL,
    [UpdatedAt]          DATETIME2 (3)   NOT NULL,
    [RowVersion]         ROWVERSION      NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Customers_MemberCode] UNIQUE NONCLUSTERED ([MemberCode] ASC)
);


GO
PRINT N'Creating Index [dbo].[Customers].[IX_Customers_Category_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_Customers_Category_IsActive]
    ON [dbo].[Customers]([Category] ASC, [IsActive] ASC);


GO
PRINT N'Creating Table [dbo].[Departments]...';


GO
CREATE TABLE [dbo].[Departments] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (80)  NOT NULL,
    [Description] NVARCHAR (300) NOT NULL,
    [IsActive]    BIT            NOT NULL,
    [CreatedAt]   DATETIME2 (3)  NOT NULL,
    [UpdatedAt]   DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Departments_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);


GO
PRINT N'Creating Table [dbo].[Employees]...';


GO
CREATE TABLE [dbo].[Employees] (
    [Id]                    INT             IDENTITY (1, 1) NOT NULL,
    [FullName]              NVARCHAR (150)  NOT NULL,
    [StaffCode]             NVARCHAR (30)   NOT NULL,
    [DepartmentId]          INT             NULL,
    [JobRoleId]             INT             NULL,
    [Department]            NVARCHAR (80)   NOT NULL,
    [Designation]           NVARCHAR (100)  NOT NULL,
    [PhoneNumber]           NVARCHAR (30)   NOT NULL,
    [Email]                 NVARCHAR (256)  NOT NULL,
    [Address]               NVARCHAR (300)  NOT NULL,
    [PassportUrl]           NVARCHAR (500)  NULL,
    [BankName]              NVARCHAR (100)  NOT NULL,
    [BankAccountNumber]     NVARCHAR (30)   NOT NULL,
    [EmploymentStatus]      NVARCHAR (20)   NOT NULL,
    [BaseSalary]            DECIMAL (18, 2) NOT NULL,
    [EmergencyContactName]  NVARCHAR (150)  NOT NULL,
    [EmergencyContactPhone] NVARCHAR (30)   NOT NULL,
    [EmergencyRelationship] NVARCHAR (80)   NOT NULL,
    [GuarantorName]         NVARCHAR (150)  NOT NULL,
    [GuarantorPhone]        NVARCHAR (30)   NOT NULL,
    [GuarantorAddress]      NVARCHAR (300)  NOT NULL,
    [GuarantorOccupation]   NVARCHAR (100)  NOT NULL,
    [EmploymentDate]        DATE            NOT NULL,
    [IsActive]              BIT             NOT NULL,
    [IsDeleted]             BIT             NOT NULL,
    [DeletedAt]             DATETIME2 (3)   NULL,
    [DeletedBy]             INT             NULL,
    [TerminationDate]       DATE            NULL,
    [TerminationReason]     NVARCHAR (300)  NOT NULL,
    [CreatedAt]             DATETIME2 (3)   NOT NULL,
    [UpdatedAt]             DATETIME2 (3)   NOT NULL,
    [RowVersion]            ROWVERSION      NOT NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[Employees].[IX_Employees_DepartmentId_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_Employees_DepartmentId_IsActive]
    ON [dbo].[Employees]([DepartmentId] ASC, [IsActive] ASC) WHERE [DepartmentId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[Employees].[UX_Employees_Email]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_Employees_Email]
    ON [dbo].[Employees]([Email] ASC) WHERE [IsDeleted] = 0 AND [Email] <> N'';


GO
PRINT N'Creating Index [dbo].[Employees].[UX_Employees_StaffCode]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_Employees_StaffCode]
    ON [dbo].[Employees]([StaffCode] ASC) WHERE [IsDeleted] = 0;


GO
PRINT N'Creating Index [dbo].[Employees].[IX_Employees_Department_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_Employees_Department_IsActive]
    ON [dbo].[Employees]([Department] ASC, [IsActive] ASC)
    INCLUDE([IsDeleted]);


GO
PRINT N'Creating Index [dbo].[Employees].[IX_Employees_JobRoleId]...';


GO
CREATE NONCLUSTERED INDEX [IX_Employees_JobRoleId]
    ON [dbo].[Employees]([JobRoleId] ASC) WHERE [JobRoleId] IS NOT NULL;


GO
PRINT N'Creating Table [dbo].[FleetDispatches]...';


GO
CREATE TABLE [dbo].[FleetDispatches] (
    [Id]               INT             IDENTITY (1, 1) NOT NULL,
    [DispatchRef]      NVARCHAR (40)   NOT NULL,
    [DispatchDate]     DATE            NOT NULL,
    [VehicleId]        INT             NOT NULL,
    [DriverEmployeeId] INT             NULL,
    [DriverName]       NVARCHAR (150)  NOT NULL,
    [Purpose]          NVARCHAR (200)  NOT NULL,
    [Status]           INT             NOT NULL,
    [OdometerEnd]      DECIMAL (18, 2) NULL,
    [Notes]            NVARCHAR (1000) NOT NULL,
    [CreatedByUserId]  INT             NULL,
    [CreatedAt]        DATETIME2 (3)   NOT NULL,
    [UpdatedAt]        DATETIME2 (3)   NOT NULL,
    [RowVersion]       ROWVERSION      NOT NULL,
    CONSTRAINT [PK_FleetDispatches] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_FleetDispatches_Ref] UNIQUE NONCLUSTERED ([DispatchRef] ASC)
);


GO
PRINT N'Creating Index [dbo].[FleetDispatches].[IX_FleetDispatches_Vehicle_Date]...';


GO
CREATE NONCLUSTERED INDEX [IX_FleetDispatches_Vehicle_Date]
    ON [dbo].[FleetDispatches]([VehicleId] ASC, [DispatchDate] DESC);


GO
PRINT N'Creating Index [dbo].[FleetDispatches].[IX_FleetDispatches_Date_Status]...';


GO
CREATE NONCLUSTERED INDEX [IX_FleetDispatches_Date_Status]
    ON [dbo].[FleetDispatches]([DispatchDate] DESC, [Status] ASC);


GO
PRINT N'Creating Table [dbo].[FleetDispatchStops]...';


GO
CREATE TABLE [dbo].[FleetDispatchStops] (
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [FleetDispatchId]   INT            NOT NULL,
    [StopNumber]        INT            NOT NULL,
    [DeliveryPointName] NVARCHAR (150) NOT NULL,
    [Address]           NVARCHAR (300) NOT NULL,
    [CityState]         NVARCHAR (120) NOT NULL,
    [ContactName]       NVARCHAR (150) NULL,
    [ContactPhone]      NVARCHAR (30)  NULL,
    [Status]            INT            NOT NULL,
    [DeliveredAt]       DATETIME2 (3)  NULL,
    [Notes]             NVARCHAR (500) NULL,
    [CreatedAt]         DATETIME2 (3)  NOT NULL,
    [UpdatedAt]         DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_FleetDispatchStops] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_FleetDispatchStops_Dispatch_Stop] UNIQUE NONCLUSTERED ([FleetDispatchId] ASC, [StopNumber] ASC)
);


GO
PRINT N'Creating Index [dbo].[FleetDispatchStops].[IX_FleetDispatchStops_Dispatch]...';


GO
CREATE NONCLUSTERED INDEX [IX_FleetDispatchStops_Dispatch]
    ON [dbo].[FleetDispatchStops]([FleetDispatchId] ASC, [StopNumber] ASC);


GO
PRINT N'Creating Table [dbo].[FuelLogs]...';


GO
CREATE TABLE [dbo].[FuelLogs] (
    [Id]               INT             IDENTITY (1, 1) NOT NULL,
    [VehicleId]        INT             NOT NULL,
    [CharterBookingId] INT             NULL,
    [FleetDispatchId]  INT             NULL,
    [EmployeeId]       INT             NULL,
    [FuelDate]         DATETIME2 (3)   NOT NULL,
    [FuelType]         NVARCHAR (40)   NOT NULL,
    [LitresDispensed]  DECIMAL (18, 2) NOT NULL,
    [PricePerLitre]    DECIMAL (18, 2) NOT NULL,
    [TotalCost]        AS              ([LitresDispensed] * [PricePerLitre]) PERSISTED,
    [PetrolStation]    NVARCHAR (120)  NOT NULL,
    [RouteDescription] NVARCHAR (200)  NOT NULL,
    [CreatedAt]        DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_FuelLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[FuelLogs].[IX_FuelLogs_VehicleId_FuelDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_FuelLogs_VehicleId_FuelDate]
    ON [dbo].[FuelLogs]([VehicleId] ASC, [FuelDate] DESC);


GO
PRINT N'Creating Index [dbo].[FuelLogs].[IX_FuelLogs_FuelDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_FuelLogs_FuelDate]
    ON [dbo].[FuelLogs]([FuelDate] DESC);


GO
PRINT N'Creating Index [dbo].[FuelLogs].[IX_FuelLogs_FleetDispatchId]...';


GO
CREATE NONCLUSTERED INDEX [IX_FuelLogs_FleetDispatchId]
    ON [dbo].[FuelLogs]([FleetDispatchId] ASC) WHERE [FleetDispatchId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[FuelLogs].[IX_FuelLogs_EmployeeId]...';


GO
CREATE NONCLUSTERED INDEX [IX_FuelLogs_EmployeeId]
    ON [dbo].[FuelLogs]([EmployeeId] ASC) WHERE [EmployeeId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[FuelLogs].[IX_FuelLogs_CharterBookingId]...';


GO
CREATE NONCLUSTERED INDEX [IX_FuelLogs_CharterBookingId]
    ON [dbo].[FuelLogs]([CharterBookingId] ASC) WHERE [CharterBookingId] IS NOT NULL;


GO
PRINT N'Creating Table [dbo].[IngredientPriceChangeRequests]...';


GO
CREATE TABLE [dbo].[IngredientPriceChangeRequests] (
    [Id]                INT             IDENTITY (1, 1) NOT NULL,
    [InventoryItemId]   INT             NOT NULL,
    [InventoryItemName] NVARCHAR (150)  NOT NULL,
    [RequestNumber]     NVARCHAR (40)   NOT NULL,
    [PreviousUnitCost]  DECIMAL (18, 4) NOT NULL,
    [RequestedUnitCost] DECIMAL (18, 4) NOT NULL,
    [Status]            INT             NOT NULL,
    [RequestedByUserId] INT             NULL,
    [RequestedByName]   NVARCHAR (150)  NOT NULL,
    [RequestedAt]       DATETIME2 (3)   NOT NULL,
    [ReviewedByUserId]  INT             NULL,
    [ReviewedByName]    NVARCHAR (150)  NULL,
    [ReviewedAt]        DATETIME2 (3)   NULL,
    [ReviewNotes]       NVARCHAR (500)  NOT NULL,
    [AppliedAt]         DATETIME2 (3)   NULL,
    [CreatedAt]         DATETIME2 (3)   NOT NULL,
    [UpdatedAt]         DATETIME2 (3)   NOT NULL,
    [RowVersion]        ROWVERSION      NOT NULL,
    CONSTRAINT [PK_IngredientPriceChangeRequests] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_IngredientPriceChangeRequests_RequestNumber] UNIQUE NONCLUSTERED ([RequestNumber] ASC)
);


GO
PRINT N'Creating Index [dbo].[IngredientPriceChangeRequests].[UX_IngredientPriceChangeRequests_OpenItem]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_IngredientPriceChangeRequests_OpenItem]
    ON [dbo].[IngredientPriceChangeRequests]([InventoryItemId] ASC) WHERE [Status] = 0;


GO
PRINT N'Creating Index [dbo].[IngredientPriceChangeRequests].[IX_IngredientPriceChangeRequests_InventoryItemId]...';


GO
CREATE NONCLUSTERED INDEX [IX_IngredientPriceChangeRequests_InventoryItemId]
    ON [dbo].[IngredientPriceChangeRequests]([InventoryItemId] ASC);


GO
PRINT N'Creating Index [dbo].[IngredientPriceChangeRequests].[IX_IngredientPriceChangeRequests_Status_RequestedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_IngredientPriceChangeRequests_Status_RequestedAt]
    ON [dbo].[IngredientPriceChangeRequests]([Status] ASC, [RequestedAt] DESC);


GO
PRINT N'Creating Table [dbo].[InventoryItems]...';


GO
CREATE TABLE [dbo].[InventoryItems] (
    [Id]                   INT             IDENTITY (1, 1) NOT NULL,
    [ItemCode]             NVARCHAR (40)   NOT NULL,
    [Name]                 NVARCHAR (150)  NOT NULL,
    [Category]             NVARCHAR (60)   NOT NULL,
    [Unit]                 NVARCHAR (40)   NOT NULL,
    [QuantityInStock]      DECIMAL (18, 3) NOT NULL,
    [ReorderThreshold]     DECIMAL (18, 3) NOT NULL,
    [UnitCostPrice]        DECIMAL (18, 4) NOT NULL,
    [PackageSize]          DECIMAL (18, 3) NOT NULL,
    [PackageUnitOfMeasure] NVARCHAR (20)   NOT NULL,
    [DensityKgPerLitre]    DECIMAL (18, 4) NULL,
    [MeasurementDensityId] INT             NULL,
    [PreferredSupplierId]  INT             NULL,
    [LastRestockedDate]    DATE            NOT NULL,
    [IsActive]             BIT             NOT NULL,
    [CreatedAt]            DATETIME2 (3)   NOT NULL,
    [UpdatedAt]            DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_InventoryItems] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_InventoryItems_ItemCode] UNIQUE NONCLUSTERED ([ItemCode] ASC)
);


GO
PRINT N'Creating Index [dbo].[InventoryItems].[IX_InventoryItems_PreferredSupplierId]...';


GO
CREATE NONCLUSTERED INDEX [IX_InventoryItems_PreferredSupplierId]
    ON [dbo].[InventoryItems]([PreferredSupplierId] ASC) WHERE [PreferredSupplierId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[InventoryItems].[IX_InventoryItems_Category_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_InventoryItems_Category_IsActive]
    ON [dbo].[InventoryItems]([Category] ASC, [IsActive] ASC);


GO
PRINT N'Creating Index [dbo].[InventoryItems].[IX_InventoryItems_MeasurementDensityId]...';


GO
CREATE NONCLUSTERED INDEX [IX_InventoryItems_MeasurementDensityId]
    ON [dbo].[InventoryItems]([MeasurementDensityId] ASC) WHERE [MeasurementDensityId] IS NOT NULL;


GO
PRINT N'Creating Table [dbo].[JobRoles]...';


GO
CREATE TABLE [dbo].[JobRoles] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [DepartmentId] INT            NULL,
    [Name]         NVARCHAR (100) NOT NULL,
    [Description]  NVARCHAR (300) NOT NULL,
    [IsActive]     BIT            NOT NULL,
    [CreatedAt]    DATETIME2 (3)  NOT NULL,
    [UpdatedAt]    DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_JobRoles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_JobRoles_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);


GO
PRINT N'Creating Index [dbo].[JobRoles].[IX_JobRoles_DepartmentId_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_JobRoles_DepartmentId_IsActive]
    ON [dbo].[JobRoles]([DepartmentId] ASC, [IsActive] ASC) WHERE [DepartmentId] IS NOT NULL;


GO
PRINT N'Creating Table [dbo].[MaintenanceLogs]...';


GO
CREATE TABLE [dbo].[MaintenanceLogs] (
    [Id]               INT             IDENTITY (1, 1) NOT NULL,
    [VehicleId]        INT             NOT NULL,
    [ServiceDate]      DATE            NOT NULL,
    [Description]      NVARCHAR (400)  NOT NULL,
    [Cost]             DECIMAL (18, 2) NOT NULL,
    [WorkshopOrVendor] NVARCHAR (150)  NOT NULL,
    [InvoiceNumber]    NVARCHAR (60)   NOT NULL,
    [BeforeImageUrl]   NVARCHAR (500)  NULL,
    [AfterImageUrl]    NVARCHAR (500)  NULL,
    [CreatedAt]        DATETIME2 (3)   NOT NULL,
    [UpdatedAt]        DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_MaintenanceLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[MaintenanceLogs].[IX_MaintenanceLogs_VehicleId_ServiceDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_MaintenanceLogs_VehicleId_ServiceDate]
    ON [dbo].[MaintenanceLogs]([VehicleId] ASC, [ServiceDate] DESC);


GO
PRINT N'Creating Table [dbo].[MeasurementDensities]...';


GO
CREATE TABLE [dbo].[MeasurementDensities] (
    [Id]                INT             IDENTITY (1, 1) NOT NULL,
    [MaterialName]      NVARCHAR (150)  NOT NULL,
    [DensityKgPerLitre] DECIMAL (18, 4) NOT NULL,
    [IsActive]          BIT             NOT NULL,
    [CreatedAt]         DATETIME2 (3)   NOT NULL,
    [UpdatedAt]         DATETIME2 (3)   NOT NULL,
    [RowVersion]        ROWVERSION      NOT NULL,
    CONSTRAINT [PK_MeasurementDensities] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_MeasurementDensities_MaterialName] UNIQUE NONCLUSTERED ([MaterialName] ASC)
);


GO
PRINT N'Creating Index [dbo].[MeasurementDensities].[IX_MeasurementDensities_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_MeasurementDensities_IsActive]
    ON [dbo].[MeasurementDensities]([IsActive] ASC);


GO
PRINT N'Creating Table [dbo].[Notifications]...';


GO
CREATE TABLE [dbo].[Notifications] (
    [Id]              BIGINT          IDENTITY (1, 1) NOT NULL,
    [RecipientUserId] INT             NOT NULL,
    [Title]           NVARCHAR (200)  NOT NULL,
    [Message]         NVARCHAR (1000) NOT NULL,
    [LinkUrl]         NVARCHAR (300)  NULL,
    [Type]            INT             NOT NULL,
    [IsRead]          BIT             NOT NULL,
    [ReadAt]          DATETIME2 (3)   NULL,
    [CreatedByUserId] INT             NULL,
    [CreatedAt]       DATETIME2 (3)   NOT NULL,
    [RowVersion]      ROWVERSION      NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[Notifications].[IX_Notifications_CreatedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_Notifications_CreatedAt]
    ON [dbo].[Notifications]([CreatedAt] DESC);


GO
PRINT N'Creating Index [dbo].[Notifications].[IX_Notifications_Recipient_IsRead_CreatedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_Notifications_Recipient_IsRead_CreatedAt]
    ON [dbo].[Notifications]([RecipientUserId] ASC, [IsRead] ASC, [CreatedAt] DESC);


GO
PRINT N'Creating Table [dbo].[OrderItems]...';


GO
CREATE TABLE [dbo].[OrderItems] (
    [Id]            INT             IDENTITY (1, 1) NOT NULL,
    [OrderId]       INT             NOT NULL,
    [RecipeId]      INT             NOT NULL,
    [ProductName]   NVARCHAR (150)  NOT NULL,
    [Quantity]      INT             NOT NULL,
    [BonusQuantity] INT             NOT NULL,
    [UnitPrice]     DECIMAL (18, 2) NOT NULL,
    [TotalQuantity] AS              ([Quantity] + [BonusQuantity]) PERSISTED,
    [LineTotal]     AS              ([Quantity] * [UnitPrice]) PERSISTED,
    [CreatedAt]     DATETIME2 (3)   NOT NULL,
    [UpdatedAt]     DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_OrderItems_Order_Recipe] UNIQUE NONCLUSTERED ([OrderId] ASC, [RecipeId] ASC)
);


GO
PRINT N'Creating Index [dbo].[OrderItems].[IX_OrderItems_RecipeId]...';


GO
CREATE NONCLUSTERED INDEX [IX_OrderItems_RecipeId]
    ON [dbo].[OrderItems]([RecipeId] ASC);


GO
PRINT N'Creating Table [dbo].[OrderPayments]...';


GO
CREATE TABLE [dbo].[OrderPayments] (
    [Id]            INT             IDENTITY (1, 1) NOT NULL,
    [OrderId]       INT             NOT NULL,
    [Amount]        DECIMAL (18, 2) NOT NULL,
    [PaymentMethod] INT             NOT NULL,
    [PaymentDate]   DATETIME2 (3)   NOT NULL,
    [Reference]     NVARCHAR (80)   NOT NULL,
    [Notes]         NVARCHAR (300)  NOT NULL,
    [ReceivedBy]    NVARCHAR (150)  NOT NULL,
    [CreatedAt]     DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_OrderPayments] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[OrderPayments].[IX_OrderPayments_PaymentDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_OrderPayments_PaymentDate]
    ON [dbo].[OrderPayments]([PaymentDate] DESC);


GO
PRINT N'Creating Index [dbo].[OrderPayments].[IX_OrderPayments_OrderId_PaymentDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_OrderPayments_OrderId_PaymentDate]
    ON [dbo].[OrderPayments]([OrderId] ASC, [PaymentDate] DESC);


GO
PRINT N'Creating Table [dbo].[Orders]...';


GO
CREATE TABLE [dbo].[Orders] (
    [Id]                  INT             IDENTITY (1, 1) NOT NULL,
    [OrderNumber]         NVARCHAR (40)   NOT NULL,
    [OrderType]           INT             NOT NULL,
    [CustomerId]          INT             NOT NULL,
    [CustomerName]        NVARCHAR (150)  NOT NULL,
    [CustomerCategory]    INT             NOT NULL,
    [SalesRepId]          INT             NULL,
    [SalesRepName]        NVARCHAR (150)  NULL,
    [PosTerminalId]       INT             NULL,
    [PosTerminalCode]     NVARCHAR (20)   NULL,
    [ProductRepositoryId] INT             NULL,
    [BookedByType]        INT             NOT NULL,
    [CreatedByUserId]     INT             NULL,
    [SubTotal]            DECIMAL (18, 2) NOT NULL,
    [Discount]            DECIMAL (18, 2) NOT NULL,
    [TotalAmount]         AS              ([SubTotal] - [Discount]) PERSISTED,
    [AmountPaid]          DECIMAL (18, 2) NOT NULL,
    [BalanceDue]          AS              (([SubTotal] - [Discount]) - [AmountPaid]) PERSISTED,
    [PaymentMethod]       INT             NOT NULL,
    [PaymentStatus]       INT             NOT NULL,
    [Status]              INT             NOT NULL,
    [ClosedAt]            DATETIME2 (3)   NULL,
    [OrderDate]           DATETIME2 (3)   NOT NULL,
    [TargetDeliveryDate]  DATETIME2 (3)   NOT NULL,
    [StockDeductedAt]     DATETIME2 (3)   NULL,
    [StockDeductedBy]     NVARCHAR (150)  NULL,
    [Notes]               NVARCHAR (500)  NOT NULL,
    [CreatedAt]           DATETIME2 (3)   NOT NULL,
    [UpdatedAt]           DATETIME2 (3)   NOT NULL,
    [RowVersion]          ROWVERSION      NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Orders_OrderNumber] UNIQUE NONCLUSTERED ([OrderNumber] ASC)
);


GO
PRINT N'Creating Index [dbo].[Orders].[IX_Orders_Status_TargetDeliveryDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_Orders_Status_TargetDeliveryDate]
    ON [dbo].[Orders]([Status] ASC, [TargetDeliveryDate] ASC);


GO
PRINT N'Creating Index [dbo].[Orders].[IX_Orders_SalesRepId_OrderDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_Orders_SalesRepId_OrderDate]
    ON [dbo].[Orders]([SalesRepId] ASC, [OrderDate] DESC) WHERE [SalesRepId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[Orders].[IX_Orders_CreatedByUserId]...';


GO
CREATE NONCLUSTERED INDEX [IX_Orders_CreatedByUserId]
    ON [dbo].[Orders]([CreatedByUserId] ASC) WHERE [CreatedByUserId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[Orders].[IX_Orders_CustomerId_OrderDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_Orders_CustomerId_OrderDate]
    ON [dbo].[Orders]([CustomerId] ASC, [OrderDate] DESC);


GO
PRINT N'Creating Index [dbo].[Orders].[IX_Orders_ProductRepositoryId]...';


GO
CREATE NONCLUSTERED INDEX [IX_Orders_ProductRepositoryId]
    ON [dbo].[Orders]([ProductRepositoryId] ASC) WHERE [ProductRepositoryId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[Orders].[IX_Orders_StockDeductedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_Orders_StockDeductedAt]
    ON [dbo].[Orders]([StockDeductedAt] DESC) WHERE [StockDeductedAt] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[Orders].[IX_Orders_OrderDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_Orders_OrderDate]
    ON [dbo].[Orders]([OrderDate] DESC);


GO
PRINT N'Creating Index [dbo].[Orders].[IX_Orders_PaymentStatus_OrderDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_Orders_PaymentStatus_OrderDate]
    ON [dbo].[Orders]([PaymentStatus] ASC, [OrderDate] DESC);


GO
PRINT N'Creating Table [dbo].[PayrollDeductionLines]...';


GO
CREATE TABLE [dbo].[PayrollDeductionLines] (
    [Id]              BIGINT          IDENTITY (1, 1) NOT NULL,
    [PayrollRecordId] INT             NOT NULL,
    [EmployeeId]      INT             NOT NULL,
    [DeductionType]   NVARCHAR (40)   NOT NULL,
    [Description]     NVARCHAR (300)  NOT NULL,
    [Amount]          DECIMAL (18, 2) NOT NULL,
    [SalaryAdvanceId] INT             NULL,
    [CreatedAt]       DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_PayrollDeductionLines] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[PayrollDeductionLines].[IX_PayrollDeductionLines_PayrollRecordId]...';


GO
CREATE NONCLUSTERED INDEX [IX_PayrollDeductionLines_PayrollRecordId]
    ON [dbo].[PayrollDeductionLines]([PayrollRecordId] ASC);


GO
PRINT N'Creating Index [dbo].[PayrollDeductionLines].[IX_PayrollDeductionLines_EmployeeId_CreatedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_PayrollDeductionLines_EmployeeId_CreatedAt]
    ON [dbo].[PayrollDeductionLines]([EmployeeId] ASC, [CreatedAt] DESC);


GO
PRINT N'Creating Index [dbo].[PayrollDeductionLines].[IX_PayrollDeductionLines_SalaryAdvanceId]...';


GO
CREATE NONCLUSTERED INDEX [IX_PayrollDeductionLines_SalaryAdvanceId]
    ON [dbo].[PayrollDeductionLines]([SalaryAdvanceId] ASC) WHERE [SalaryAdvanceId] IS NOT NULL;


GO
PRINT N'Creating Table [dbo].[PayrollRecords]...';


GO
CREATE TABLE [dbo].[PayrollRecords] (
    [Id]                       INT             IDENTITY (1, 1) NOT NULL,
    [PayrollRunId]             INT             NULL,
    [EmployeeId]               INT             NOT NULL,
    [PayrollMonth]             TINYINT         NOT NULL,
    [PayrollYear]              SMALLINT        NOT NULL,
    [BaseSalary]               DECIMAL (18, 2) NOT NULL,
    [PerformanceBonus]         DECIMAL (18, 2) NOT NULL,
    [HousingAllowance]         DECIMAL (18, 2) NOT NULL,
    [TransportAllowance]       DECIMAL (18, 2) NOT NULL,
    [OtherAllowances]          DECIMAL (18, 2) NOT NULL,
    [DisciplinaryDeductions]   DECIMAL (18, 2) NOT NULL,
    [AdvanceDeduction]         DECIMAL (18, 2) NOT NULL,
    [TaxAndLevies]             DECIMAL (18, 2) NOT NULL,
    [PensionDeduction]         DECIMAL (18, 2) NOT NULL,
    [HealthInsuranceDeduction] DECIMAL (18, 2) NOT NULL,
    [GrossSalary]              AS              ([BaseSalary] + [PerformanceBonus] + [HousingAllowance] + [TransportAllowance] + [OtherAllowances]) PERSISTED,
    [TotalDeductions]          AS              ([DisciplinaryDeductions] + [AdvanceDeduction] + [TaxAndLevies] + [PensionDeduction] + [HealthInsuranceDeduction]) PERSISTED,
    [NetSalary]                AS              (([BaseSalary] + [PerformanceBonus] + [HousingAllowance] + [TransportAllowance] + [OtherAllowances]) - ([DisciplinaryDeductions] + [AdvanceDeduction] + [TaxAndLevies] + [PensionDeduction] + [HealthInsuranceDeduction])) PERSISTED,
    [PaymentStatus]            NVARCHAR (20)   NOT NULL,
    [PaymentDate]              DATETIME2 (3)   NULL,
    [Notes]                    NVARCHAR (400)  NOT NULL,
    [CreatedAt]                DATETIME2 (3)   NOT NULL,
    [UpdatedAt]                DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_PayrollRecords] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_PayrollRecords_Employee_Period] UNIQUE NONCLUSTERED ([EmployeeId] ASC, [PayrollYear] ASC, [PayrollMonth] ASC)
);


GO
PRINT N'Creating Index [dbo].[PayrollRecords].[IX_PayrollRecords_PayrollRunId]...';


GO
CREATE NONCLUSTERED INDEX [IX_PayrollRecords_PayrollRunId]
    ON [dbo].[PayrollRecords]([PayrollRunId] ASC) WHERE [PayrollRunId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[PayrollRecords].[IX_PayrollRecords_Period_Status]...';


GO
CREATE NONCLUSTERED INDEX [IX_PayrollRecords_Period_Status]
    ON [dbo].[PayrollRecords]([PayrollYear] DESC, [PayrollMonth] DESC, [PaymentStatus] ASC);


GO
PRINT N'Creating Table [dbo].[PayrollRuns]...';


GO
CREATE TABLE [dbo].[PayrollRuns] (
    [Id]              INT             IDENTITY (1, 1) NOT NULL,
    [RunNumber]       NVARCHAR (40)   NOT NULL,
    [PayrollMonth]    TINYINT         NOT NULL,
    [PayrollYear]     SMALLINT        NOT NULL,
    [GeneratedAt]     DATETIME2 (3)   NOT NULL,
    [GeneratedBy]     NVARCHAR (150)  NOT NULL,
    [EmployeeCount]   INT             NOT NULL,
    [GrossTotal]      DECIMAL (18, 2) NOT NULL,
    [DeductionsTotal] DECIMAL (18, 2) NOT NULL,
    [NetTotal]        DECIMAL (18, 2) NOT NULL,
    [Status]          NVARCHAR (20)   NOT NULL,
    [Notes]           NVARCHAR (400)  NOT NULL,
    [CreatedAt]       DATETIME2 (3)   NOT NULL,
    [UpdatedAt]       DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_PayrollRuns] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_PayrollRuns_Period] UNIQUE NONCLUSTERED ([PayrollYear] ASC, [PayrollMonth] ASC),
    CONSTRAINT [UQ_PayrollRuns_RunNumber] UNIQUE NONCLUSTERED ([RunNumber] ASC)
);


GO
PRINT N'Creating Index [dbo].[PayrollRuns].[IX_PayrollRuns_GeneratedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_PayrollRuns_GeneratedAt]
    ON [dbo].[PayrollRuns]([GeneratedAt] DESC);


GO
PRINT N'Creating Table [dbo].[PayrollSettings]...';


GO
CREATE TABLE [dbo].[PayrollSettings] (
    [Id]                          TINYINT        NOT NULL,
    [LoanMonthlyRepaymentPercent] DECIMAL (5, 2) NOT NULL,
    [MaxLoanPercentOfSalary]      DECIMAL (5, 2) NOT NULL,
    [HousingAllowancePercent]     DECIMAL (5, 2) NOT NULL,
    [TransportAllowancePercent]   DECIMAL (5, 2) NOT NULL,
    [PayeTaxPercent]              DECIMAL (5, 2) NOT NULL,
    [PensionPercent]              DECIMAL (5, 2) NOT NULL,
    [HealthInsurancePercent]      DECIMAL (5, 2) NOT NULL,
    [UpdatedAt]                   DATETIME2 (3)  NOT NULL,
    [UpdatedBy]                   NVARCHAR (150) NOT NULL,
    CONSTRAINT [PK_PayrollSettings] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Table [dbo].[PosTerminals]...';


GO
CREATE TABLE [dbo].[PosTerminals] (
    [Id]                  INT            IDENTITY (1, 1) NOT NULL,
    [TerminalCode]        NVARCHAR (20)  NOT NULL,
    [TerminalName]        NVARCHAR (100) NOT NULL,
    [AssignedEmployeeId]  INT            NULL,
    [AssignedUserId]      INT            NULL,
    [ProductRepositoryId] INT            NULL,
    [CashAccountName]     NVARCHAR (100) NOT NULL,
    [TransferAccountName] NVARCHAR (120) NOT NULL,
    [PosBankAccountName]  NVARCHAR (120) NOT NULL,
    [IsActive]            BIT            NOT NULL,
    [CreatedAt]           DATETIME2 (3)  NOT NULL,
    [UpdatedAt]           DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_PosTerminals] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_PosTerminals_TerminalCode] UNIQUE NONCLUSTERED ([TerminalCode] ASC)
);


GO
PRINT N'Creating Index [dbo].[PosTerminals].[IX_PosTerminals_AssignedEmployeeId]...';


GO
CREATE NONCLUSTERED INDEX [IX_PosTerminals_AssignedEmployeeId]
    ON [dbo].[PosTerminals]([AssignedEmployeeId] ASC) WHERE [AssignedEmployeeId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[PosTerminals].[IX_PosTerminals_AssignedUserId]...';


GO
CREATE NONCLUSTERED INDEX [IX_PosTerminals_AssignedUserId]
    ON [dbo].[PosTerminals]([AssignedUserId] ASC) WHERE [AssignedUserId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[PosTerminals].[IX_PosTerminals_ProductRepositoryId]...';


GO
CREATE NONCLUSTERED INDEX [IX_PosTerminals_ProductRepositoryId]
    ON [dbo].[PosTerminals]([ProductRepositoryId] ASC) WHERE [ProductRepositoryId] IS NOT NULL;


GO
PRINT N'Creating Table [dbo].[ProductionBatches]...';


GO
CREATE TABLE [dbo].[ProductionBatches] (
    [Id]                      INT             IDENTITY (1, 1) NOT NULL,
    [BatchNumber]             NVARCHAR (40)   NOT NULL,
    [RecipeId]                INT             NULL,
    [RecipeName]              NVARCHAR (150)  NOT NULL,
    [TargetQuantity]          INT             NOT NULL,
    [ActualQuantity]          INT             NOT NULL,
    [Variance]                AS              ([ActualQuantity] - [TargetQuantity]) PERSISTED,
    [Shift]                   NVARCHAR (40)   NOT NULL,
    [BakerInCharge]           NVARCHAR (150)  NOT NULL,
    [FlourBagsUsed]           DECIMAL (18, 3) NOT NULL,
    [ProductionProductTypeId] INT             NULL,
    [BatchInputQuantity]      DECIMAL (18, 3) NOT NULL,
    [BatchInputUnitLabel]     NVARCHAR (60)   NOT NULL,
    [OutputUnitLabel]         NVARCHAR (40)   NOT NULL,
    [TargetOrderQuantity]     INT             NOT NULL,
    [LinkedOrderId]           INT             NULL,
    [LinkedOrderNumber]       NVARCHAR (40)   NULL,
    [CustomerName]            NVARCHAR (150)  NULL,
    [Status]                  INT             NOT NULL,
    [ProductionDate]          DATETIME2 (3)   NOT NULL,
    [Notes]                   NVARCHAR (500)  NOT NULL,
    [TotalBatchCost]          DECIMAL (18, 2) NOT NULL,
    [CalculatedUnitCost]      DECIMAL (18, 4) NOT NULL,
    [ExpectedUnitCost]        DECIMAL (18, 4) NOT NULL,
    [LoggedBy]                NVARCHAR (150)  NOT NULL,
    [CompletedAt]             DATETIME2 (3)   NULL,
    [CreatedAt]               DATETIME2 (3)   NOT NULL,
    [UpdatedAt]               DATETIME2 (3)   NOT NULL,
    [RowVersion]              ROWVERSION      NOT NULL,
    CONSTRAINT [PK_ProductionBatches] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductionBatches_BatchNumber] UNIQUE NONCLUSTERED ([BatchNumber] ASC)
);


GO
PRINT N'Creating Index [dbo].[ProductionBatches].[IX_ProductionBatches_LinkedOrderId]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductionBatches_LinkedOrderId]
    ON [dbo].[ProductionBatches]([LinkedOrderId] ASC) WHERE [LinkedOrderId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[ProductionBatches].[IX_ProductionBatches_RecipeId_Status]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductionBatches_RecipeId_Status]
    ON [dbo].[ProductionBatches]([RecipeId] ASC, [Status] ASC);


GO
PRINT N'Creating Index [dbo].[ProductionBatches].[IX_ProductionBatches_ProductionDate]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductionBatches_ProductionDate]
    ON [dbo].[ProductionBatches]([ProductionDate] DESC);


GO
PRINT N'Creating Index [dbo].[ProductionBatches].[IX_ProductionBatches_ProductionProductTypeId]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductionBatches_ProductionProductTypeId]
    ON [dbo].[ProductionBatches]([ProductionProductTypeId] ASC) WHERE [ProductionProductTypeId] IS NOT NULL;


GO
PRINT N'Creating Table [dbo].[ProductionBatchItems]...';


GO
CREATE TABLE [dbo].[ProductionBatchItems] (
    [Id]                  INT             IDENTITY (1, 1) NOT NULL,
    [ProductionBatchId]   INT             NOT NULL,
    [RecipeId]            INT             NOT NULL,
    [RecipeName]          NVARCHAR (150)  NOT NULL,
    [ProductCode]         NVARCHAR (40)   NOT NULL,
    [TargetQuantity]      INT             NOT NULL,
    [ActualQuantity]      INT             NOT NULL,
    [Variance]            AS              ([ActualQuantity] - [TargetQuantity]) PERSISTED,
    [BatchInputQuantity]  DECIMAL (18, 3) NOT NULL,
    [BatchInputUnitLabel] NVARCHAR (60)   NOT NULL,
    [OutputUnitLabel]     NVARCHAR (40)   NOT NULL,
    [TotalBatchCost]      DECIMAL (18, 2) NOT NULL,
    [ExpectedUnitCost]    DECIMAL (18, 4) NOT NULL,
    [CalculatedUnitCost]  DECIMAL (18, 4) NOT NULL,
    [ActualRecordedAt]    DATETIME2 (3)   NULL,
    [CreatedAt]           DATETIME2 (3)   NOT NULL,
    [UpdatedAt]           DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_ProductionBatchItems] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductionBatchItems_Batch_Recipe] UNIQUE NONCLUSTERED ([ProductionBatchId] ASC, [RecipeId] ASC)
);


GO
PRINT N'Creating Index [dbo].[ProductionBatchItems].[IX_ProductionBatchItems_RecipeId]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductionBatchItems_RecipeId]
    ON [dbo].[ProductionBatchItems]([RecipeId] ASC);


GO
PRINT N'Creating Table [dbo].[ProductionCategories]...';


GO
CREATE TABLE [dbo].[ProductionCategories] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (80)  NOT NULL,
    [Description] NVARCHAR (300) NOT NULL,
    [IsActive]    BIT            NOT NULL,
    [CreatedAt]   DATETIME2 (3)  NOT NULL,
    [UpdatedAt]   DATETIME2 (3)  NOT NULL,
    [RowVersion]  ROWVERSION     NOT NULL,
    CONSTRAINT [PK_ProductionCategories] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductionCategories_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);


GO
PRINT N'Creating Index [dbo].[ProductionCategories].[IX_ProductionCategories_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductionCategories_IsActive]
    ON [dbo].[ProductionCategories]([IsActive] ASC);


GO
PRINT N'Creating Table [dbo].[ProductionDefects]...';


GO
CREATE TABLE [dbo].[ProductionDefects] (
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [ProductionBatchId] INT            NOT NULL,
    [RecipeId]          INT            NOT NULL,
    [Quantity]          INT            NOT NULL,
    [DefectReason]      NVARCHAR (250) NOT NULL,
    [RecordedAt]        DATETIME2 (3)  NOT NULL,
    [RecordedBy]        NVARCHAR (150) NOT NULL,
    [CreatedAt]         DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_ProductionDefects] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[ProductionDefects].[IX_ProductionDefects_RecordedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductionDefects_RecordedAt]
    ON [dbo].[ProductionDefects]([RecordedAt] DESC);


GO
PRINT N'Creating Index [dbo].[ProductionDefects].[IX_ProductionDefects_RecipeId_RecordedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductionDefects_RecipeId_RecordedAt]
    ON [dbo].[ProductionDefects]([RecipeId] ASC, [RecordedAt] DESC);


GO
PRINT N'Creating Table [dbo].[ProductionProductTypes]...';


GO
CREATE TABLE [dbo].[ProductionProductTypes] (
    [Id]                   INT            IDENTITY (1, 1) NOT NULL,
    [ProductionCategoryId] INT            NOT NULL,
    [Name]                 NVARCHAR (120) NOT NULL,
    [Baseline]             NVARCHAR (150) NOT NULL,
    [OutputUnitNoun]       NVARCHAR (40)  NOT NULL,
    [IsActive]             BIT            NOT NULL,
    [CreatedAt]            DATETIME2 (3)  NOT NULL,
    [UpdatedAt]            DATETIME2 (3)  NOT NULL,
    [RowVersion]           ROWVERSION     NOT NULL,
    CONSTRAINT [PK_ProductionProductTypes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductionProductTypes_Category_Name] UNIQUE NONCLUSTERED ([ProductionCategoryId] ASC, [Name] ASC)
);


GO
PRINT N'Creating Index [dbo].[ProductionProductTypes].[IX_ProductionProductTypes_Category_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductionProductTypes_Category_IsActive]
    ON [dbo].[ProductionProductTypes]([ProductionCategoryId] ASC, [IsActive] ASC);


GO
PRINT N'Creating Table [dbo].[ProductionShifts]...';


GO
CREATE TABLE [dbo].[ProductionShifts] (
    [Id]         INT           IDENTITY (1, 1) NOT NULL,
    [Name]       NVARCHAR (80) NOT NULL,
    [StartTime]  TIME (0)      NULL,
    [EndTime]    TIME (0)      NULL,
    [IsActive]   BIT           NOT NULL,
    [CreatedAt]  DATETIME2 (3) NOT NULL,
    [UpdatedAt]  DATETIME2 (3) NOT NULL,
    [RowVersion] ROWVERSION    NOT NULL,
    CONSTRAINT [PK_ProductionShifts] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductionShifts_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);


GO
PRINT N'Creating Index [dbo].[ProductionShifts].[IX_ProductionShifts_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductionShifts_IsActive]
    ON [dbo].[ProductionShifts]([IsActive] ASC);


GO
PRINT N'Creating Table [dbo].[ProductRepositories]...';


GO
CREATE TABLE [dbo].[ProductRepositories] (
    [Id]                        INT            IDENTITY (1, 1) NOT NULL,
    [Code]                      NVARCHAR (30)  NOT NULL,
    [Name]                      NVARCHAR (120) NOT NULL,
    [LocationType]              INT            NOT NULL,
    [Address]                   NVARCHAR (300) NOT NULL,
    [IsDefaultProductionOutput] BIT            NOT NULL,
    [IsActive]                  BIT            NOT NULL,
    [CreatedAt]                 DATETIME2 (3)  NOT NULL,
    [UpdatedAt]                 DATETIME2 (3)  NOT NULL,
    [RowVersion]                ROWVERSION     NOT NULL,
    CONSTRAINT [PK_ProductRepositories] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductRepositories_Code] UNIQUE NONCLUSTERED ([Code] ASC)
);


GO
PRINT N'Creating Index [dbo].[ProductRepositories].[IX_ProductRepositories_LocationType_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductRepositories_LocationType_IsActive]
    ON [dbo].[ProductRepositories]([LocationType] ASC, [IsActive] ASC);


GO
PRINT N'Creating Index [dbo].[ProductRepositories].[UX_ProductRepositories_DefaultOutput]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_ProductRepositories_DefaultOutput]
    ON [dbo].[ProductRepositories]([IsDefaultProductionOutput] ASC) WHERE [IsDefaultProductionOutput] = 1;


GO
PRINT N'Creating Table [dbo].[ProductRepositoryMovements]...';


GO
CREATE TABLE [dbo].[ProductRepositoryMovements] (
    [Id]                  BIGINT          IDENTITY (1, 1) NOT NULL,
    [ProductRepositoryId] INT             NOT NULL,
    [RecipeId]            INT             NOT NULL,
    [MovementType]        INT             NOT NULL,
    [Quantity]            DECIMAL (18, 3) NOT NULL,
    [BalanceAfter]        DECIMAL (18, 3) NOT NULL,
    [ReferenceType]       NVARCHAR (40)   NOT NULL,
    [ReferenceId]         INT             NULL,
    [ReferenceNumber]     NVARCHAR (40)   NOT NULL,
    [Notes]               NVARCHAR (300)  NOT NULL,
    [PerformedBy]         NVARCHAR (150)  NOT NULL,
    [CreatedAt]           DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_ProductRepositoryMovements] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[ProductRepositoryMovements].[IX_ProductRepositoryMovements_Repo_Recipe_CreatedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductRepositoryMovements_Repo_Recipe_CreatedAt]
    ON [dbo].[ProductRepositoryMovements]([ProductRepositoryId] ASC, [RecipeId] ASC, [CreatedAt] DESC);


GO
PRINT N'Creating Index [dbo].[ProductRepositoryMovements].[IX_ProductRepositoryMovements_CreatedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductRepositoryMovements_CreatedAt]
    ON [dbo].[ProductRepositoryMovements]([CreatedAt] DESC);


GO
PRINT N'Creating Table [dbo].[ProductRepositoryStocks]...';


GO
CREATE TABLE [dbo].[ProductRepositoryStocks] (
    [Id]                  INT             IDENTITY (1, 1) NOT NULL,
    [ProductRepositoryId] INT             NOT NULL,
    [RecipeId]            INT             NOT NULL,
    [QuantityOnHand]      DECIMAL (18, 3) NOT NULL,
    [ReorderThreshold]    DECIMAL (18, 3) NOT NULL,
    [CreatedAt]           DATETIME2 (3)   NOT NULL,
    [UpdatedAt]           DATETIME2 (3)   NOT NULL,
    [RowVersion]          ROWVERSION      NOT NULL,
    CONSTRAINT [PK_ProductRepositoryStocks] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductRepositoryStocks_Repo_Recipe] UNIQUE NONCLUSTERED ([ProductRepositoryId] ASC, [RecipeId] ASC)
);


GO
PRINT N'Creating Index [dbo].[ProductRepositoryStocks].[IX_ProductRepositoryStocks_RecipeId]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProductRepositoryStocks_RecipeId]
    ON [dbo].[ProductRepositoryStocks]([RecipeId] ASC);


GO
PRINT N'Creating Table [dbo].[RbacModules]...';


GO
CREATE TABLE [dbo].[RbacModules] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [ModuleKey]   NVARCHAR (60)  NOT NULL,
    [ModuleName]  NVARCHAR (150) NOT NULL,
    [Description] NVARCHAR (300) NOT NULL,
    [SortOrder]   INT            NOT NULL,
    [IsActive]    BIT            NOT NULL,
    [CreatedAt]   DATETIME2 (3)  NOT NULL,
    [UpdatedAt]   DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_RbacModules] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RbacModules_ModuleKey] UNIQUE NONCLUSTERED ([ModuleKey] ASC)
);


GO
PRINT N'Creating Index [dbo].[RbacModules].[IX_RbacModules_SortOrder_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_RbacModules_SortOrder_IsActive]
    ON [dbo].[RbacModules]([SortOrder] ASC, [IsActive] ASC);


GO
PRINT N'Creating Table [dbo].[RbacPermissionAudits]...';


GO
CREATE TABLE [dbo].[RbacPermissionAudits] (
    [Id]                     BIGINT         IDENTITY (1, 1) NOT NULL,
    [RoleId]                 INT            NOT NULL,
    [RoleName]               NVARCHAR (50)  NOT NULL,
    [ModuleId]               INT            NOT NULL,
    [ModuleKey]              NVARCHAR (60)  NOT NULL,
    [OldPermissionLevelId]   INT            NULL,
    [OldPermissionLevelName] NVARCHAR (80)  NULL,
    [NewPermissionLevelId]   INT            NOT NULL,
    [NewPermissionLevelName] NVARCHAR (80)  NOT NULL,
    [ChangedBy]              NVARCHAR (150) NOT NULL,
    [ChangedAt]              DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_RbacPermissionAudits] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[RbacPermissionAudits].[IX_RbacPermissionAudits_ChangedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_RbacPermissionAudits_ChangedAt]
    ON [dbo].[RbacPermissionAudits]([ChangedAt] DESC);


GO
PRINT N'Creating Index [dbo].[RbacPermissionAudits].[IX_RbacPermissionAudits_Role_Module]...';


GO
CREATE NONCLUSTERED INDEX [IX_RbacPermissionAudits_Role_Module]
    ON [dbo].[RbacPermissionAudits]([RoleId] ASC, [ModuleId] ASC, [ChangedAt] DESC);


GO
PRINT N'Creating Table [dbo].[RbacPermissionLevels]...';


GO
CREATE TABLE [dbo].[RbacPermissionLevels] (
    [Id]          INT           IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (80) NOT NULL,
    [Rank]        INT           NOT NULL,
    [CanView]     BIT           NOT NULL,
    [CanCreate]   BIT           NOT NULL,
    [CanEdit]     BIT           NOT NULL,
    [CanDelete]   BIT           NOT NULL,
    [CanApprove]  BIT           NOT NULL,
    [CanPrint]    BIT           NOT NULL,
    [CanManage]   BIT           NOT NULL,
    [CanRequest]  BIT           NOT NULL,
    [CanIssue]    BIT           NOT NULL,
    [CanDisburse] BIT           NOT NULL,
    [CanBookOwn]  BIT           NOT NULL,
    [IsNone]      BIT           NOT NULL,
    [SortOrder]   INT           NOT NULL,
    [IsActive]    BIT           NOT NULL,
    [CreatedAt]   DATETIME2 (3) NOT NULL,
    [UpdatedAt]   DATETIME2 (3) NOT NULL,
    CONSTRAINT [PK_RbacPermissionLevels] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RbacPermissionLevels_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);


GO
PRINT N'Creating Index [dbo].[RbacPermissionLevels].[IX_RbacPermissionLevels_SortOrder_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_RbacPermissionLevels_SortOrder_IsActive]
    ON [dbo].[RbacPermissionLevels]([SortOrder] ASC, [IsActive] ASC);


GO
PRINT N'Creating Table [dbo].[RbacRolePermissions]...';


GO
CREATE TABLE [dbo].[RbacRolePermissions] (
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [RoleId]            INT            NOT NULL,
    [ModuleId]          INT            NOT NULL,
    [PermissionLevelId] INT            NOT NULL,
    [UpdatedAt]         DATETIME2 (3)  NOT NULL,
    [UpdatedBy]         NVARCHAR (150) NOT NULL,
    CONSTRAINT [PK_RbacRolePermissions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RbacRolePermissions_Role_Module] UNIQUE NONCLUSTERED ([RoleId] ASC, [ModuleId] ASC)
);


GO
PRINT N'Creating Index [dbo].[RbacRolePermissions].[IX_RbacRolePermissions_PermissionLevelId]...';


GO
CREATE NONCLUSTERED INDEX [IX_RbacRolePermissions_PermissionLevelId]
    ON [dbo].[RbacRolePermissions]([PermissionLevelId] ASC);


GO
PRINT N'Creating Index [dbo].[RbacRolePermissions].[IX_RbacRolePermissions_ModuleId]...';


GO
CREATE NONCLUSTERED INDEX [IX_RbacRolePermissions_ModuleId]
    ON [dbo].[RbacRolePermissions]([ModuleId] ASC);


GO
PRINT N'Creating Table [dbo].[RecipeIngredients]...';


GO
CREATE TABLE [dbo].[RecipeIngredients] (
    [Id]                 INT             IDENTITY (1, 1) NOT NULL,
    [RecipeId]           INT             NOT NULL,
    [InventoryItemId]    INT             NOT NULL,
    [Unit]               NVARCHAR (40)   NOT NULL,
    [QuantityUnit]       NVARCHAR (40)   NOT NULL,
    [QuantityPerBatch]   DECIMAL (18, 4) NOT NULL,
    [BaselineYield]      DECIMAL (18, 3) NOT NULL,
    [IngredientUnitCost] DECIMAL (18, 4) NOT NULL,
    [QuantityPerUnit]    AS              (CASE WHEN [BaselineYield] > 0 THEN [QuantityPerBatch] / [BaselineYield] ELSE 0 END) PERSISTED,
    [BatchLineCost]      AS              ([QuantityPerBatch] * [IngredientUnitCost]) PERSISTED,
    [CreatedAt]          DATETIME2 (3)   NOT NULL,
    [UpdatedAt]          DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_RecipeIngredients] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RecipeIngredients_Recipe_Ingredient] UNIQUE NONCLUSTERED ([RecipeId] ASC, [InventoryItemId] ASC)
);


GO
PRINT N'Creating Index [dbo].[RecipeIngredients].[IX_RecipeIngredients_IngredientId]...';


GO
CREATE NONCLUSTERED INDEX [IX_RecipeIngredients_IngredientId]
    ON [dbo].[RecipeIngredients]([InventoryItemId] ASC);


GO
PRINT N'Creating Table [dbo].[Recipes]...';


GO
CREATE TABLE [dbo].[Recipes] (
    [Id]                      INT             IDENTITY (1, 1) NOT NULL,
    [Name]                    NVARCHAR (150)  NOT NULL,
    [Code]                    NVARCHAR (40)   NOT NULL,
    [Description]             NVARCHAR (500)  NOT NULL,
    [ProductionProductTypeId] INT             NULL,
    [ExpectedYield]           DECIMAL (18, 3) NOT NULL,
    [OutputUnit]              NVARCHAR (40)   NOT NULL,
    [SellingPrice]            DECIMAL (18, 2) NOT NULL,
    [IsActive]                BIT             NOT NULL,
    [CreatedAt]               DATETIME2 (3)   NOT NULL,
    [UpdatedAt]               DATETIME2 (3)   NOT NULL,
    [RowVersion]              ROWVERSION      NOT NULL,
    CONSTRAINT [PK_Recipes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Recipes_Code] UNIQUE NONCLUSTERED ([Code] ASC)
);


GO
PRINT N'Creating Index [dbo].[Recipes].[IX_Recipes_ProductType_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_Recipes_ProductType_IsActive]
    ON [dbo].[Recipes]([ProductionProductTypeId] ASC, [IsActive] ASC);


GO
PRINT N'Creating Table [dbo].[Roles]...';


GO
CREATE TABLE [dbo].[Roles] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [Name]         NVARCHAR (50)  NOT NULL,
    [DisplayName]  NVARCHAR (100) NOT NULL,
    [Description]  NVARCHAR (300) NOT NULL,
    [IsSystemRole] BIT            NOT NULL,
    [CreatedAt]    DATETIME2 (3)  NOT NULL,
    [UpdatedAt]    DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Roles_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);


GO
PRINT N'Creating Table [dbo].[SalaryAdvances]...';


GO
CREATE TABLE [dbo].[SalaryAdvances] (
    [Id]                     INT             IDENTITY (1, 1) NOT NULL,
    [EmployeeId]             INT             NOT NULL,
    [PrincipalAmount]        DECIMAL (18, 2) NOT NULL,
    [MonthlyDeductionAmount] DECIMAL (18, 2) NOT NULL,
    [TotalRepaid]            DECIMAL (18, 2) NOT NULL,
    [RemainingBalance]       AS              ([PrincipalAmount] - [TotalRepaid]) PERSISTED,
    [RequestDate]            DATE            NOT NULL,
    [Reason]                 NVARCHAR (300)  NOT NULL,
    [Status]                 NVARCHAR (20)   NOT NULL,
    [CreatedAt]              DATETIME2 (3)   NOT NULL,
    [UpdatedAt]              DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_SalaryAdvances] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[SalaryAdvances].[IX_SalaryAdvances_EmployeeId_Status]...';


GO
CREATE NONCLUSTERED INDEX [IX_SalaryAdvances_EmployeeId_Status]
    ON [dbo].[SalaryAdvances]([EmployeeId] ASC, [Status] ASC);


GO
PRINT N'Creating Table [dbo].[SalesBonusRules]...';


GO
CREATE TABLE [dbo].[SalesBonusRules] (
    [Id]                INT           IDENTITY (1, 1) NOT NULL,
    [RecipeId]          INT           NOT NULL,
    [ThresholdQuantity] INT           NOT NULL,
    [BonusQuantity]     INT           NOT NULL,
    [IsActive]          BIT           NOT NULL,
    [CreatedAt]         DATETIME2 (3) NOT NULL,
    [UpdatedAt]         DATETIME2 (3) NOT NULL,
    CONSTRAINT [PK_SalesBonusRules] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SalesBonusRules_Recipe_Threshold] UNIQUE NONCLUSTERED ([RecipeId] ASC, [ThresholdQuantity] ASC)
);


GO
PRINT N'Creating Index [dbo].[SalesBonusRules].[IX_SalesBonusRules_RecipeId_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_SalesBonusRules_RecipeId_IsActive]
    ON [dbo].[SalesBonusRules]([RecipeId] ASC, [IsActive] ASC);


GO
PRINT N'Creating Table [dbo].[SalesReconciliationItems]...';


GO
CREATE TABLE [dbo].[SalesReconciliationItems] (
    [Id]                    INT             IDENTITY (1, 1) NOT NULL,
    [SalesReconciliationId] INT             NOT NULL,
    [RecipeId]              INT             NOT NULL,
    [ProductName]           NVARCHAR (150)  NOT NULL,
    [ProductCode]           NVARCHAR (40)   NOT NULL,
    [UnitPrice]             DECIMAL (18, 2) NOT NULL,
    [OpeningStock]          INT             NOT NULL,
    [QtySold]               INT             NOT NULL,
    [QtyDamaged]            INT             NOT NULL,
    [QtyReplacement]        INT             NOT NULL,
    [ClosingStock]          AS              ([OpeningStock] - (([QtySold] + [QtyDamaged]) + [QtyReplacement])) PERSISTED,
    [ExpectedRevenue]       AS              ([QtySold] * [UnitPrice]) PERSISTED,
    [CreatedAt]             DATETIME2 (3)   NOT NULL,
    [UpdatedAt]             DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_SalesReconciliationItems] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SalesReconciliationItems_Reconciliation_Recipe] UNIQUE NONCLUSTERED ([SalesReconciliationId] ASC, [RecipeId] ASC)
);


GO
PRINT N'Creating Index [dbo].[SalesReconciliationItems].[IX_SalesReconciliationItems_RecipeId]...';


GO
CREATE NONCLUSTERED INDEX [IX_SalesReconciliationItems_RecipeId]
    ON [dbo].[SalesReconciliationItems]([RecipeId] ASC);


GO
PRINT N'Creating Table [dbo].[SalesReconciliations]...';


GO
CREATE TABLE [dbo].[SalesReconciliations] (
    [Id]                   INT             IDENTITY (1, 1) NOT NULL,
    [ReconciliationNumber] NVARCHAR (40)   NOT NULL,
    [ReconciliationDate]   DATE            NOT NULL,
    [SalesRepId]           INT             NOT NULL,
    [PosTerminalId]        INT             NULL,
    [ProductRepositoryId]  INT             NULL,
    [RouteName]            NVARCHAR (120)  NOT NULL,
    [VehicleRegistration]  NVARCHAR (40)   NOT NULL,
    [CashHandedOver]       DECIMAL (18, 2) NOT NULL,
    [PosTerminalReceipts]  DECIMAL (18, 2) NOT NULL,
    [TotalCollected]       AS              ([CashHandedOver] + [PosTerminalReceipts]) PERSISTED,
    [TotalExpectedRevenue] DECIMAL (18, 2) NOT NULL,
    [Variance]             AS              ([TotalExpectedRevenue] - ([CashHandedOver] + [PosTerminalReceipts])) PERSISTED,
    [Status]               INT             NOT NULL,
    [StockPostedAt]        DATETIME2 (3)   NULL,
    [Notes]                NVARCHAR (500)  NOT NULL,
    [ReconciledBy]         NVARCHAR (150)  NULL,
    [ReconciledAt]         DATETIME2 (3)   NULL,
    [CreatedAt]            DATETIME2 (3)   NOT NULL,
    [UpdatedAt]            DATETIME2 (3)   NOT NULL,
    [RowVersion]           ROWVERSION      NOT NULL,
    CONSTRAINT [PK_SalesReconciliations] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SalesReconciliations_Number] UNIQUE NONCLUSTERED ([ReconciliationNumber] ASC),
    CONSTRAINT [UQ_SalesReconciliations_Rep_Date] UNIQUE NONCLUSTERED ([SalesRepId] ASC, [ReconciliationDate] ASC)
);


GO
PRINT N'Creating Index [dbo].[SalesReconciliations].[IX_SalesReconciliations_PosTerminalId]...';


GO
CREATE NONCLUSTERED INDEX [IX_SalesReconciliations_PosTerminalId]
    ON [dbo].[SalesReconciliations]([PosTerminalId] ASC) WHERE [PosTerminalId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[SalesReconciliations].[IX_SalesReconciliations_Date_Status]...';


GO
CREATE NONCLUSTERED INDEX [IX_SalesReconciliations_Date_Status]
    ON [dbo].[SalesReconciliations]([ReconciliationDate] DESC, [Status] ASC);


GO
PRINT N'Creating Index [dbo].[SalesReconciliations].[IX_SalesReconciliations_ProductRepositoryId]...';


GO
CREATE NONCLUSTERED INDEX [IX_SalesReconciliations_ProductRepositoryId]
    ON [dbo].[SalesReconciliations]([ProductRepositoryId] ASC) WHERE [ProductRepositoryId] IS NOT NULL;


GO
PRINT N'Creating Table [dbo].[SalesReps]...';


GO
CREATE TABLE [dbo].[SalesReps] (
    [Id]            INT            IDENTITY (1, 1) NOT NULL,
    [RepCode]       NVARCHAR (20)  NOT NULL,
    [EmployeeId]    INT            NULL,
    [UserId]        INT            NULL,
    [FullName]      NVARCHAR (150) NOT NULL,
    [PhoneNumber]   NVARCHAR (30)  NOT NULL,
    [RouteName]     NVARCHAR (120) NOT NULL,
    [PosTerminalId] INT            NULL,
    [VehicleId]     INT            NULL,
    [ProductFocus]  NVARCHAR (120) NOT NULL,
    [IsActive]      BIT            NOT NULL,
    [CreatedAt]     DATETIME2 (3)  NOT NULL,
    [UpdatedAt]     DATETIME2 (3)  NOT NULL,
    [RowVersion]    ROWVERSION     NOT NULL,
    CONSTRAINT [PK_SalesReps] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SalesReps_RepCode] UNIQUE NONCLUSTERED ([RepCode] ASC)
);


GO
PRINT N'Creating Index [dbo].[SalesReps].[UX_SalesReps_UserId]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_SalesReps_UserId]
    ON [dbo].[SalesReps]([UserId] ASC) WHERE [UserId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[SalesReps].[IX_SalesReps_RouteName_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_SalesReps_RouteName_IsActive]
    ON [dbo].[SalesReps]([RouteName] ASC, [IsActive] ASC);


GO
PRINT N'Creating Index [dbo].[SalesReps].[IX_SalesReps_VehicleId]...';


GO
CREATE NONCLUSTERED INDEX [IX_SalesReps_VehicleId]
    ON [dbo].[SalesReps]([VehicleId] ASC) WHERE [VehicleId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[SalesReps].[IX_SalesReps_EmployeeId]...';


GO
CREATE NONCLUSTERED INDEX [IX_SalesReps_EmployeeId]
    ON [dbo].[SalesReps]([EmployeeId] ASC) WHERE [EmployeeId] IS NOT NULL;


GO
PRINT N'Creating Table [dbo].[StockMovements]...';


GO
CREATE TABLE [dbo].[StockMovements] (
    [Id]                 INT             IDENTITY (1, 1) NOT NULL,
    [InventoryItemId]    INT             NOT NULL,
    [MovementType]       NVARCHAR (20)   NOT NULL,
    [Quantity]           DECIMAL (18, 3) NOT NULL,
    [BalanceAfter]       DECIMAL (18, 3) NOT NULL,
    [StoreRequestId]     INT             NULL,
    [StoreRequestItemId] INT             NULL,
    [ReferenceNumber]    NVARCHAR (40)   NULL,
    [Notes]              NVARCHAR (300)  NULL,
    [PerformedBy]        NVARCHAR (150)  NOT NULL,
    [CreatedAt]          DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_StockMovements] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[StockMovements].[IX_StockMovements_InventoryItemId_CreatedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_StockMovements_InventoryItemId_CreatedAt]
    ON [dbo].[StockMovements]([InventoryItemId] ASC, [CreatedAt] DESC);


GO
PRINT N'Creating Index [dbo].[StockMovements].[IX_StockMovements_StoreRequestItemId]...';


GO
CREATE NONCLUSTERED INDEX [IX_StockMovements_StoreRequestItemId]
    ON [dbo].[StockMovements]([StoreRequestItemId] ASC) WHERE [StoreRequestItemId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[StockMovements].[IX_StockMovements_StoreRequestId]...';


GO
CREATE NONCLUSTERED INDEX [IX_StockMovements_StoreRequestId]
    ON [dbo].[StockMovements]([StoreRequestId] ASC) WHERE [StoreRequestId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[StockMovements].[IX_StockMovements_CreatedAt]...';


GO
CREATE NONCLUSTERED INDEX [IX_StockMovements_CreatedAt]
    ON [dbo].[StockMovements]([CreatedAt] DESC);


GO
PRINT N'Creating Table [dbo].[StoreRequestItems]...';


GO
CREATE TABLE [dbo].[StoreRequestItems] (
    [Id]                INT             IDENTITY (1, 1) NOT NULL,
    [StoreRequestId]    INT             NOT NULL,
    [InventoryItemId]   INT             NOT NULL,
    [ItemName]          NVARCHAR (150)  NOT NULL,
    [Unit]              NVARCHAR (40)   NOT NULL,
    [RequestedQuantity] DECIMAL (18, 3) NOT NULL,
    [BaseQuantity]      DECIMAL (18, 3) NOT NULL,
    [BaseUnit]          NVARCHAR (40)   NOT NULL,
    [StockQuantity]     DECIMAL (18, 6) NOT NULL,
    [StockUnit]         NVARCHAR (40)   NOT NULL,
    [IssuedQuantity]    DECIMAL (18, 3) NOT NULL,
    [CreatedAt]         DATETIME2 (3)   NOT NULL,
    [UpdatedAt]         DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_StoreRequestItems] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[StoreRequestItems].[IX_StoreRequestItems_InventoryItemId]...';


GO
CREATE NONCLUSTERED INDEX [IX_StoreRequestItems_InventoryItemId]
    ON [dbo].[StoreRequestItems]([InventoryItemId] ASC);


GO
PRINT N'Creating Table [dbo].[StoreRequests]...';


GO
CREATE TABLE [dbo].[StoreRequests] (
    [Id]              INT            IDENTITY (1, 1) NOT NULL,
    [RequestNumber]   NVARCHAR (40)  NOT NULL,
    [RequestedBy]     NVARCHAR (150) NOT NULL,
    [Department]      NVARCHAR (80)  NOT NULL,
    [Purpose]         NVARCHAR (300) NOT NULL,
    [Status]          INT            NOT NULL,
    [RequestedAt]     DATETIME2 (3)  NOT NULL,
    [ApprovedBy]      NVARCHAR (150) NULL,
    [ApprovedAt]      DATETIME2 (3)  NULL,
    [RejectionReason] NVARCHAR (300) NULL,
    [CreatedAt]       DATETIME2 (3)  NOT NULL,
    [UpdatedAt]       DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_StoreRequests] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_StoreRequests_RequestNumber] UNIQUE NONCLUSTERED ([RequestNumber] ASC)
);


GO
PRINT N'Creating Index [dbo].[StoreRequests].[IX_StoreRequests_RequestedAt_Status]...';


GO
CREATE NONCLUSTERED INDEX [IX_StoreRequests_RequestedAt_Status]
    ON [dbo].[StoreRequests]([RequestedAt] DESC, [Status] ASC);


GO
PRINT N'Creating Table [dbo].[SupermarketAccountPrices]...';


GO
CREATE TABLE [dbo].[SupermarketAccountPrices] (
    [Id]                   INT             IDENTITY (1, 1) NOT NULL,
    [SupermarketAccountId] INT             NOT NULL,
    [RecipeId]             INT             NOT NULL,
    [AgreedPrice]          DECIMAL (18, 2) NOT NULL,
    [CreatedAt]            DATETIME2 (3)   NOT NULL,
    [UpdatedAt]            DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_SupermarketAccountPrices] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SupermarketAccountPrices_Account_Recipe] UNIQUE NONCLUSTERED ([SupermarketAccountId] ASC, [RecipeId] ASC)
);


GO
PRINT N'Creating Index [dbo].[SupermarketAccountPrices].[IX_SupermarketAccountPrices_RecipeId]...';


GO
CREATE NONCLUSTERED INDEX [IX_SupermarketAccountPrices_RecipeId]
    ON [dbo].[SupermarketAccountPrices]([RecipeId] ASC);


GO
PRINT N'Creating Table [dbo].[SupermarketAccounts]...';


GO
CREATE TABLE [dbo].[SupermarketAccounts] (
    [Id]                       INT             IDENTITY (1, 1) NOT NULL,
    [CustomerId]               INT             NOT NULL,
    [AccountName]              NVARCHAR (150)  NOT NULL,
    [ContactPerson]            NVARCHAR (150)  NOT NULL,
    [PhoneNumber]              NVARCHAR (30)   NOT NULL,
    [Address]                  NVARCHAR (300)  NOT NULL,
    [AssignedRepId]            INT             NULL,
    [CurrentOutstandingCredit] DECIMAL (18, 2) NOT NULL,
    [CreditLimit]              DECIMAL (18, 2) NOT NULL,
    [PaymentTerms]             NVARCHAR (60)   NOT NULL,
    [IsActive]                 BIT             NOT NULL,
    [CreatedAt]                DATETIME2 (3)   NOT NULL,
    [UpdatedAt]                DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_SupermarketAccounts] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SupermarketAccounts_CustomerId] UNIQUE NONCLUSTERED ([CustomerId] ASC)
);


GO
PRINT N'Creating Index [dbo].[SupermarketAccounts].[IX_SupermarketAccounts_AssignedRepId]...';


GO
CREATE NONCLUSTERED INDEX [IX_SupermarketAccounts_AssignedRepId]
    ON [dbo].[SupermarketAccounts]([AssignedRepId] ASC) WHERE [AssignedRepId] IS NOT NULL;


GO
PRINT N'Creating Table [dbo].[Suppliers]...';


GO
CREATE TABLE [dbo].[Suppliers] (
    [Id]             INT            IDENTITY (1, 1) NOT NULL,
    [Name]           NVARCHAR (150) NOT NULL,
    [ContactPerson]  NVARCHAR (150) NOT NULL,
    [PhoneNumber]    NVARCHAR (30)  NOT NULL,
    [Email]          NVARCHAR (256) NOT NULL,
    [SupplyCategory] NVARCHAR (80)  NOT NULL,
    [Address]        NVARCHAR (300) NOT NULL,
    [IsActive]       BIT            NOT NULL,
    [CreatedAt]      DATETIME2 (3)  NOT NULL,
    [UpdatedAt]      DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Suppliers_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);


GO
PRINT N'Creating Index [dbo].[Suppliers].[IX_Suppliers_SupplyCategory_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_Suppliers_SupplyCategory_IsActive]
    ON [dbo].[Suppliers]([SupplyCategory] ASC, [IsActive] ASC);


GO
PRINT N'Creating Table [dbo].[TreasuryPolicySettings]...';


GO
CREATE TABLE [dbo].[TreasuryPolicySettings] (
    [Id]                    TINYINT        NOT NULL,
    [PlannedCashPercent]    DECIMAL (5, 2) NOT NULL,
    [AlertTolerancePercent] DECIMAL (5, 2) NOT NULL,
    [PolicyMemo]            NVARCHAR (500) NOT NULL,
    [LastUpdatedUtc]        DATETIME2 (3)  NOT NULL,
    [UpdatedBy]             NVARCHAR (150) NOT NULL,
    CONSTRAINT [PK_TreasuryPolicySettings] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Table [dbo].[TripLegs]...';


GO
CREATE TABLE [dbo].[TripLegs] (
    [Id]                 INT             IDENTITY (1, 1) NOT NULL,
    [CharterBookingId]   INT             NOT NULL,
    [LegNumber]          INT             NOT NULL,
    [OriginState]        NVARCHAR (80)   NOT NULL,
    [OriginAddress]      NVARCHAR (300)  NULL,
    [DestinationState]   NVARCHAR (120)  NOT NULL,
    [DestinationAddress] NVARCHAR (300)  NULL,
    [DistanceKm]         DECIMAL (18, 2) NULL,
    [FuelOption]         INT             NOT NULL,
    [LegCost]            DECIMAL (18, 2) NOT NULL,
    [CreatedAt]          DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_TripLegs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_TripLegs_Booking_Leg] UNIQUE NONCLUSTERED ([CharterBookingId] ASC, [LegNumber] ASC)
);


GO
PRINT N'Creating Table [dbo].[Users]...';


GO
CREATE TABLE [dbo].[Users] (
    [Id]                 INT              IDENTITY (1, 1) NOT NULL,
    [Name]               NVARCHAR (150)   NOT NULL,
    [Email]              NVARCHAR (256)   NOT NULL,
    [RoleId]             INT              NOT NULL,
    [EmployeeId]         INT              NULL,
    [CustomerId]         INT              NULL,
    [PinCodeHash]        NVARCHAR (256)   NOT NULL,
    [PasswordHash]       NVARCHAR (256)   NOT NULL,
    [SecurityStamp]      UNIQUEIDENTIFIER NOT NULL,
    [MustChangePassword] BIT              NOT NULL,
    [PosTerminalCode]    NVARCHAR (20)    NULL,
    [PhoneNumber]        NVARCHAR (30)    NULL,
    [IsActive]           BIT              NOT NULL,
    [LastLoginAt]        DATETIME2 (3)    NULL,
    [CreatedAt]          DATETIME2 (3)    NOT NULL,
    [UpdatedAt]          DATETIME2 (3)    NOT NULL,
    [RowVersion]         ROWVERSION       NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Users_Email] UNIQUE NONCLUSTERED ([Email] ASC)
);


GO
PRINT N'Creating Index [dbo].[Users].[IX_Users_PosTerminalCode]...';


GO
CREATE NONCLUSTERED INDEX [IX_Users_PosTerminalCode]
    ON [dbo].[Users]([PosTerminalCode] ASC) WHERE [PosTerminalCode] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[Users].[IX_Users_RoleId_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_Users_RoleId_IsActive]
    ON [dbo].[Users]([RoleId] ASC, [IsActive] ASC);


GO
PRINT N'Creating Index [dbo].[Users].[IX_Users_CustomerId]...';


GO
CREATE NONCLUSTERED INDEX [IX_Users_CustomerId]
    ON [dbo].[Users]([CustomerId] ASC) WHERE [CustomerId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[Users].[UX_Users_EmployeeId]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_Users_EmployeeId]
    ON [dbo].[Users]([EmployeeId] ASC) WHERE [EmployeeId] IS NOT NULL;


GO
PRINT N'Creating Table [dbo].[Vehicles]...';


GO
CREATE TABLE [dbo].[Vehicles] (
    [Id]                 INT             IDENTITY (1, 1) NOT NULL,
    [RegistrationNumber] NVARCHAR (20)   NOT NULL,
    [MakeAndModel]       NVARCHAR (120)  NOT NULL,
    [VehicleType]        NVARCHAR (40)   NOT NULL,
    [VehicleFunction]    NVARCHAR (60)   NOT NULL,
    [AssignedEmployeeId] INT             NULL,
    [FuelType]           NVARCHAR (40)   NOT NULL,
    [ImageUrl]           NVARCHAR (500)  NULL,
    [TankCapacityLitres] DECIMAL (18, 2) NOT NULL,
    [CurrentOdometerKm]  DECIMAL (18, 2) NOT NULL,
    [Status]             NVARCHAR (30)   NOT NULL,
    [LastServiceDate]    DATE            NULL,
    [IsActive]           BIT             NOT NULL,
    [CreatedAt]          DATETIME2 (3)   NOT NULL,
    [UpdatedAt]          DATETIME2 (3)   NOT NULL,
    [RowVersion]         ROWVERSION      NOT NULL,
    CONSTRAINT [PK_Vehicles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Vehicles_RegistrationNumber] UNIQUE NONCLUSTERED ([RegistrationNumber] ASC)
);


GO
PRINT N'Creating Index [dbo].[Vehicles].[IX_Vehicles_AssignedEmployeeId]...';


GO
CREATE NONCLUSTERED INDEX [IX_Vehicles_AssignedEmployeeId]
    ON [dbo].[Vehicles]([AssignedEmployeeId] ASC) WHERE [AssignedEmployeeId] IS NOT NULL;


GO
PRINT N'Creating Index [dbo].[Vehicles].[IX_Vehicles_Status_IsActive]...';


GO
CREATE NONCLUSTERED INDEX [IX_Vehicles_Status_IsActive]
    ON [dbo].[Vehicles]([Status] ASC, [IsActive] ASC);


GO
PRINT N'Creating Table [dbo].[WeeklyFleetExpenses]...';


GO
CREATE TABLE [dbo].[WeeklyFleetExpenses] (
    [Id]                      INT             IDENTITY (1, 1) NOT NULL,
    [OperatingWeekStart]      DATE            NOT NULL,
    [OperatingWeekEnd]        DATE            NOT NULL,
    [VehicleId]               INT             NOT NULL,
    [DriverName]              NVARCHAR (150)  NOT NULL,
    [FuelExpenseTotal]        DECIMAL (18, 2) NOT NULL,
    [MaintenanceExpenseTotal] DECIMAL (18, 2) NOT NULL,
    [IncidentalsTotal]        DECIMAL (18, 2) NOT NULL,
    [TotalWeeklyRunningCost]  AS              ([FuelExpenseTotal] + [MaintenanceExpenseTotal] + [IncidentalsTotal]) PERSISTED,
    [CreatedAt]               DATETIME2 (3)   NOT NULL,
    [UpdatedAt]               DATETIME2 (3)   NOT NULL,
    CONSTRAINT [PK_WeeklyFleetExpenses] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_WeeklyFleetExpenses_Vehicle_Week] UNIQUE NONCLUSTERED ([VehicleId] ASC, [OperatingWeekStart] ASC)
);


GO
PRINT N'Creating Index [dbo].[WeeklyFleetExpenses].[IX_WeeklyFleetExpenses_Week]...';


GO
CREATE NONCLUSTERED INDEX [IX_WeeklyFleetExpenses_Week]
    ON [dbo].[WeeklyFleetExpenses]([OperatingWeekStart] DESC);


GO
PRINT N'Creating Default Constraint [dbo].[DF_AuditLogs_OccurredAt]...';


GO
ALTER TABLE [dbo].[AuditLogs]
    ADD CONSTRAINT [DF_AuditLogs_OccurredAt] DEFAULT (SYSUTCDATETIME()) FOR [OccurredAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterBookings_SecurityEscortFee]...';


GO
ALTER TABLE [dbo].[CharterBookings]
    ADD CONSTRAINT [DF_CharterBookings_SecurityEscortFee] DEFAULT (0) FOR [SecurityEscortFee];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterBookings_TollsAndRoadLevies]...';


GO
ALTER TABLE [dbo].[CharterBookings]
    ADD CONSTRAINT [DF_CharterBookings_TollsAndRoadLevies] DEFAULT (0) FOR [TollsAndRoadLevies];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterBookings_TripStatus]...';


GO
ALTER TABLE [dbo].[CharterBookings]
    ADD CONSTRAINT [DF_CharterBookings_TripStatus] DEFAULT (0) FOR [TripStatus];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterBookings_UpdatedAt]...';


GO
ALTER TABLE [dbo].[CharterBookings]
    ADD CONSTRAINT [DF_CharterBookings_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterBookings_DriverAllowance]...';


GO
ALTER TABLE [dbo].[CharterBookings]
    ADD CONSTRAINT [DF_CharterBookings_DriverAllowance] DEFAULT (0) FOR [DriverAllowance];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterBookings_DriverName]...';


GO
ALTER TABLE [dbo].[CharterBookings]
    ADD CONSTRAINT [DF_CharterBookings_DriverName] DEFAULT (N'') FOR [DriverName];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterBookings_GrossCharterFee]...';


GO
ALTER TABLE [dbo].[CharterBookings]
    ADD CONSTRAINT [DF_CharterBookings_GrossCharterFee] DEFAULT (0) FOR [GrossCharterFee];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterBookings_Notes]...';


GO
ALTER TABLE [dbo].[CharterBookings]
    ADD CONSTRAINT [DF_CharterBookings_Notes] DEFAULT (N'') FOR [Notes];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterBookings_CreatedAt]...';


GO
ALTER TABLE [dbo].[CharterBookings]
    ADD CONSTRAINT [DF_CharterBookings_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterBookings_CustomerPhone]...';


GO
ALTER TABLE [dbo].[CharterBookings]
    ADD CONSTRAINT [DF_CharterBookings_CustomerPhone] DEFAULT (N'') FOR [CustomerPhone];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteLegs_CreatedAt]...';


GO
ALTER TABLE [dbo].[CharterQuoteLegs]
    ADD CONSTRAINT [DF_CharterQuoteLegs_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteLegs_DistanceKm]...';


GO
ALTER TABLE [dbo].[CharterQuoteLegs]
    ADD CONSTRAINT [DF_CharterQuoteLegs_DistanceKm] DEFAULT (0) FOR [DistanceKm];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteLegs_FuelOption]...';


GO
ALTER TABLE [dbo].[CharterQuoteLegs]
    ADD CONSTRAINT [DF_CharterQuoteLegs_FuelOption] DEFAULT (0) FOR [FuelOption];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteLegs_QuotedLegAmount]...';


GO
ALTER TABLE [dbo].[CharterQuoteLegs]
    ADD CONSTRAINT [DF_CharterQuoteLegs_QuotedLegAmount] DEFAULT (0) FOR [QuotedLegAmount];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteLegs_UpdatedAt]...';


GO
ALTER TABLE [dbo].[CharterQuoteLegs]
    ADD CONSTRAINT [DF_CharterQuoteLegs_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteRequests_TollsAndRoadLevies]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests]
    ADD CONSTRAINT [DF_CharterQuoteRequests_TollsAndRoadLevies] DEFAULT (0) FOR [TollsAndRoadLevies];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteRequests_TotalDistanceKm]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests]
    ADD CONSTRAINT [DF_CharterQuoteRequests_TotalDistanceKm] DEFAULT (0) FOR [TotalDistanceKm];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteRequests_TripType]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests]
    ADD CONSTRAINT [DF_CharterQuoteRequests_TripType] DEFAULT (0) FOR [TripType];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteRequests_UpdatedAt]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests]
    ADD CONSTRAINT [DF_CharterQuoteRequests_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteRequests_CreatedAt]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests]
    ADD CONSTRAINT [DF_CharterQuoteRequests_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteRequests_CustomerPhone]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests]
    ADD CONSTRAINT [DF_CharterQuoteRequests_CustomerPhone] DEFAULT (N'') FOR [CustomerPhone];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteRequests_DriverAllowance]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests]
    ADD CONSTRAINT [DF_CharterQuoteRequests_DriverAllowance] DEFAULT (0) FOR [DriverAllowance];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteRequests_Notes]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests]
    ADD CONSTRAINT [DF_CharterQuoteRequests_Notes] DEFAULT (N'') FOR [Notes];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteRequests_QuotedAmount]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests]
    ADD CONSTRAINT [DF_CharterQuoteRequests_QuotedAmount] DEFAULT (0) FOR [QuotedAmount];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteRequests_RequestDate]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests]
    ADD CONSTRAINT [DF_CharterQuoteRequests_RequestDate] DEFAULT (SYSUTCDATETIME()) FOR [RequestDate];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteRequests_SecurityEscortFee]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests]
    ADD CONSTRAINT [DF_CharterQuoteRequests_SecurityEscortFee] DEFAULT (0) FOR [SecurityEscortFee];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CharterQuoteRequests_Status]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests]
    ADD CONSTRAINT [DF_CharterQuoteRequests_Status] DEFAULT (0) FOR [Status];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CustomerReturns_UnitPrice]...';


GO
ALTER TABLE [dbo].[CustomerReturns]
    ADD CONSTRAINT [DF_CustomerReturns_UnitPrice] DEFAULT (0) FOR [UnitPrice];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CustomerReturns_ReplacementQuantity]...';


GO
ALTER TABLE [dbo].[CustomerReturns]
    ADD CONSTRAINT [DF_CustomerReturns_ReplacementQuantity] DEFAULT (0) FOR [ReplacementQuantity];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CustomerReturns_ReturnDate]...';


GO
ALTER TABLE [dbo].[CustomerReturns]
    ADD CONSTRAINT [DF_CustomerReturns_ReturnDate] DEFAULT (SYSUTCDATETIME()) FOR [ReturnDate];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CustomerReturns_Reason]...';


GO
ALTER TABLE [dbo].[CustomerReturns]
    ADD CONSTRAINT [DF_CustomerReturns_Reason] DEFAULT (N'') FOR [Reason];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CustomerReturns_RecordedBy]...';


GO
ALTER TABLE [dbo].[CustomerReturns]
    ADD CONSTRAINT [DF_CustomerReturns_RecordedBy] DEFAULT (N'') FOR [RecordedBy];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CustomerReturns_ReplacementIssued]...';


GO
ALTER TABLE [dbo].[CustomerReturns]
    ADD CONSTRAINT [DF_CustomerReturns_ReplacementIssued] DEFAULT (0) FOR [ReplacementIssued];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CustomerReturns_CreatedAt]...';


GO
ALTER TABLE [dbo].[CustomerReturns]
    ADD CONSTRAINT [DF_CustomerReturns_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_CustomerReturns_Notes]...';


GO
ALTER TABLE [dbo].[CustomerReturns]
    ADD CONSTRAINT [DF_CustomerReturns_Notes] DEFAULT (N'') FOR [Notes];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Customers_PhoneNumber]...';


GO
ALTER TABLE [dbo].[Customers]
    ADD CONSTRAINT [DF_Customers_PhoneNumber] DEFAULT (N'') FOR [PhoneNumber];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Customers_UpdatedAt]...';


GO
ALTER TABLE [dbo].[Customers]
    ADD CONSTRAINT [DF_Customers_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Customers_OutstandingBalance]...';


GO
ALTER TABLE [dbo].[Customers]
    ADD CONSTRAINT [DF_Customers_OutstandingBalance] DEFAULT (0) FOR [OutstandingBalance];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Customers_CreatedAt]...';


GO
ALTER TABLE [dbo].[Customers]
    ADD CONSTRAINT [DF_Customers_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Customers_IsActive]...';


GO
ALTER TABLE [dbo].[Customers]
    ADD CONSTRAINT [DF_Customers_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Customers_Address]...';


GO
ALTER TABLE [dbo].[Customers]
    ADD CONSTRAINT [DF_Customers_Address] DEFAULT (N'') FOR [Address];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Customers_Category]...';


GO
ALTER TABLE [dbo].[Customers]
    ADD CONSTRAINT [DF_Customers_Category] DEFAULT (0) FOR [Category];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Departments_UpdatedAt]...';


GO
ALTER TABLE [dbo].[Departments]
    ADD CONSTRAINT [DF_Departments_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Departments_IsActive]...';


GO
ALTER TABLE [dbo].[Departments]
    ADD CONSTRAINT [DF_Departments_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Departments_Description]...';


GO
ALTER TABLE [dbo].[Departments]
    ADD CONSTRAINT [DF_Departments_Description] DEFAULT (N'') FOR [Description];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Departments_CreatedAt]...';


GO
ALTER TABLE [dbo].[Departments]
    ADD CONSTRAINT [DF_Departments_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_GuarantorName]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_GuarantorName] DEFAULT (N'') FOR [GuarantorName];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_CreatedAt]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_EmergencyRelationship]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_EmergencyRelationship] DEFAULT (N'') FOR [EmergencyRelationship];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_BaseSalary]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_BaseSalary] DEFAULT (0) FOR [BaseSalary];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_EmergencyContactPhone]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_EmergencyContactPhone] DEFAULT (N'') FOR [EmergencyContactPhone];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_UpdatedAt]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_IsActive]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_TerminationReason]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_TerminationReason] DEFAULT (N'') FOR [TerminationReason];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_GuarantorPhone]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_GuarantorPhone] DEFAULT (N'') FOR [GuarantorPhone];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_EmergencyContactName]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_EmergencyContactName] DEFAULT (N'') FOR [EmergencyContactName];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_BankName]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_BankName] DEFAULT (N'') FOR [BankName];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_GuarantorAddress]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_GuarantorAddress] DEFAULT (N'') FOR [GuarantorAddress];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_Address]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_Address] DEFAULT (N'') FOR [Address];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_IsDeleted]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_IsDeleted] DEFAULT (0) FOR [IsDeleted];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_GuarantorOccupation]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_GuarantorOccupation] DEFAULT (N'') FOR [GuarantorOccupation];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_EmploymentStatus]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_EmploymentStatus] DEFAULT (N'Active') FOR [EmploymentStatus];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_Email]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_Email] DEFAULT (N'') FOR [Email];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_BankAccountNumber]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_BankAccountNumber] DEFAULT (N'') FOR [BankAccountNumber];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Employees_EmploymentDate]...';


GO
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [DF_Employees_EmploymentDate] DEFAULT (CONVERT(DATE, SYSUTCDATETIME())) FOR [EmploymentDate];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FleetDispatches_Notes]...';


GO
ALTER TABLE [dbo].[FleetDispatches]
    ADD CONSTRAINT [DF_FleetDispatches_Notes] DEFAULT (N'') FOR [Notes];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FleetDispatches_UpdatedAt]...';


GO
ALTER TABLE [dbo].[FleetDispatches]
    ADD CONSTRAINT [DF_FleetDispatches_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FleetDispatches_Status]...';


GO
ALTER TABLE [dbo].[FleetDispatches]
    ADD CONSTRAINT [DF_FleetDispatches_Status] DEFAULT (0) FOR [Status];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FleetDispatches_DriverName]...';


GO
ALTER TABLE [dbo].[FleetDispatches]
    ADD CONSTRAINT [DF_FleetDispatches_DriverName] DEFAULT (N'') FOR [DriverName];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FleetDispatches_CreatedAt]...';


GO
ALTER TABLE [dbo].[FleetDispatches]
    ADD CONSTRAINT [DF_FleetDispatches_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FleetDispatches_Purpose]...';


GO
ALTER TABLE [dbo].[FleetDispatches]
    ADD CONSTRAINT [DF_FleetDispatches_Purpose] DEFAULT (N'') FOR [Purpose];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FleetDispatchStops_Status]...';


GO
ALTER TABLE [dbo].[FleetDispatchStops]
    ADD CONSTRAINT [DF_FleetDispatchStops_Status] DEFAULT (0) FOR [Status];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FleetDispatchStops_CreatedAt]...';


GO
ALTER TABLE [dbo].[FleetDispatchStops]
    ADD CONSTRAINT [DF_FleetDispatchStops_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FleetDispatchStops_UpdatedAt]...';


GO
ALTER TABLE [dbo].[FleetDispatchStops]
    ADD CONSTRAINT [DF_FleetDispatchStops_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FuelLogs_PetrolStation]...';


GO
ALTER TABLE [dbo].[FuelLogs]
    ADD CONSTRAINT [DF_FuelLogs_PetrolStation] DEFAULT (N'') FOR [PetrolStation];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FuelLogs_FuelDate]...';


GO
ALTER TABLE [dbo].[FuelLogs]
    ADD CONSTRAINT [DF_FuelLogs_FuelDate] DEFAULT (SYSUTCDATETIME()) FOR [FuelDate];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FuelLogs_CreatedAt]...';


GO
ALTER TABLE [dbo].[FuelLogs]
    ADD CONSTRAINT [DF_FuelLogs_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FuelLogs_FuelType]...';


GO
ALTER TABLE [dbo].[FuelLogs]
    ADD CONSTRAINT [DF_FuelLogs_FuelType] DEFAULT (N'Petrol') FOR [FuelType];


GO
PRINT N'Creating Default Constraint [dbo].[DF_FuelLogs_RouteDescription]...';


GO
ALTER TABLE [dbo].[FuelLogs]
    ADD CONSTRAINT [DF_FuelLogs_RouteDescription] DEFAULT (N'') FOR [RouteDescription];


GO
PRINT N'Creating Default Constraint [dbo].[DF_IngredientPriceChangeRequests_UpdatedAt]...';


GO
ALTER TABLE [dbo].[IngredientPriceChangeRequests]
    ADD CONSTRAINT [DF_IngredientPriceChangeRequests_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_IngredientPriceChangeRequests_Status]...';


GO
ALTER TABLE [dbo].[IngredientPriceChangeRequests]
    ADD CONSTRAINT [DF_IngredientPriceChangeRequests_Status] DEFAULT (0) FOR [Status];


GO
PRINT N'Creating Default Constraint [dbo].[DF_IngredientPriceChangeRequests_RequestedByName]...';


GO
ALTER TABLE [dbo].[IngredientPriceChangeRequests]
    ADD CONSTRAINT [DF_IngredientPriceChangeRequests_RequestedByName] DEFAULT (N'') FOR [RequestedByName];


GO
PRINT N'Creating Default Constraint [dbo].[DF_IngredientPriceChangeRequests_CreatedAt]...';


GO
ALTER TABLE [dbo].[IngredientPriceChangeRequests]
    ADD CONSTRAINT [DF_IngredientPriceChangeRequests_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_IngredientPriceChangeRequests_ReviewNotes]...';


GO
ALTER TABLE [dbo].[IngredientPriceChangeRequests]
    ADD CONSTRAINT [DF_IngredientPriceChangeRequests_ReviewNotes] DEFAULT (N'') FOR [ReviewNotes];


GO
PRINT N'Creating Default Constraint [dbo].[DF_IngredientPriceChangeRequests_RequestedAt]...';


GO
ALTER TABLE [dbo].[IngredientPriceChangeRequests]
    ADD CONSTRAINT [DF_IngredientPriceChangeRequests_RequestedAt] DEFAULT (SYSUTCDATETIME()) FOR [RequestedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_InventoryItems_PackageUnitOfMeasure]...';


GO
ALTER TABLE [dbo].[InventoryItems]
    ADD CONSTRAINT [DF_InventoryItems_PackageUnitOfMeasure] DEFAULT (N'kg') FOR [PackageUnitOfMeasure];


GO
PRINT N'Creating Default Constraint [dbo].[DF_InventoryItems_ReorderThreshold]...';


GO
ALTER TABLE [dbo].[InventoryItems]
    ADD CONSTRAINT [DF_InventoryItems_ReorderThreshold] DEFAULT (0) FOR [ReorderThreshold];


GO
PRINT N'Creating Default Constraint [dbo].[DF_InventoryItems_UnitCostPrice]...';


GO
ALTER TABLE [dbo].[InventoryItems]
    ADD CONSTRAINT [DF_InventoryItems_UnitCostPrice] DEFAULT (0) FOR [UnitCostPrice];


GO
PRINT N'Creating Default Constraint [dbo].[DF_InventoryItems_CreatedAt]...';


GO
ALTER TABLE [dbo].[InventoryItems]
    ADD CONSTRAINT [DF_InventoryItems_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_InventoryItems_LastRestockedDate]...';


GO
ALTER TABLE [dbo].[InventoryItems]
    ADD CONSTRAINT [DF_InventoryItems_LastRestockedDate] DEFAULT (CONVERT(DATE, SYSUTCDATETIME())) FOR [LastRestockedDate];


GO
PRINT N'Creating Default Constraint [dbo].[DF_InventoryItems_QuantityInStock]...';


GO
ALTER TABLE [dbo].[InventoryItems]
    ADD CONSTRAINT [DF_InventoryItems_QuantityInStock] DEFAULT (0) FOR [QuantityInStock];


GO
PRINT N'Creating Default Constraint [dbo].[DF_InventoryItems_Unit]...';


GO
ALTER TABLE [dbo].[InventoryItems]
    ADD CONSTRAINT [DF_InventoryItems_Unit] DEFAULT (N'Bags (50kg)') FOR [Unit];


GO
PRINT N'Creating Default Constraint [dbo].[DF_InventoryItems_IsActive]...';


GO
ALTER TABLE [dbo].[InventoryItems]
    ADD CONSTRAINT [DF_InventoryItems_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_InventoryItems_Category]...';


GO
ALTER TABLE [dbo].[InventoryItems]
    ADD CONSTRAINT [DF_InventoryItems_Category] DEFAULT (N'Raw Materials') FOR [Category];


GO
PRINT N'Creating Default Constraint [dbo].[DF_InventoryItems_UpdatedAt]...';


GO
ALTER TABLE [dbo].[InventoryItems]
    ADD CONSTRAINT [DF_InventoryItems_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_InventoryItems_PackageSize]...';


GO
ALTER TABLE [dbo].[InventoryItems]
    ADD CONSTRAINT [DF_InventoryItems_PackageSize] DEFAULT (1) FOR [PackageSize];


GO
PRINT N'Creating Default Constraint [dbo].[DF_JobRoles_UpdatedAt]...';


GO
ALTER TABLE [dbo].[JobRoles]
    ADD CONSTRAINT [DF_JobRoles_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_JobRoles_Description]...';


GO
ALTER TABLE [dbo].[JobRoles]
    ADD CONSTRAINT [DF_JobRoles_Description] DEFAULT (N'') FOR [Description];


GO
PRINT N'Creating Default Constraint [dbo].[DF_JobRoles_CreatedAt]...';


GO
ALTER TABLE [dbo].[JobRoles]
    ADD CONSTRAINT [DF_JobRoles_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_JobRoles_IsActive]...';


GO
ALTER TABLE [dbo].[JobRoles]
    ADD CONSTRAINT [DF_JobRoles_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_MaintenanceLogs_WorkshopOrVendor]...';


GO
ALTER TABLE [dbo].[MaintenanceLogs]
    ADD CONSTRAINT [DF_MaintenanceLogs_WorkshopOrVendor] DEFAULT (N'') FOR [WorkshopOrVendor];


GO
PRINT N'Creating Default Constraint [dbo].[DF_MaintenanceLogs_CreatedAt]...';


GO
ALTER TABLE [dbo].[MaintenanceLogs]
    ADD CONSTRAINT [DF_MaintenanceLogs_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_MaintenanceLogs_InvoiceNumber]...';


GO
ALTER TABLE [dbo].[MaintenanceLogs]
    ADD CONSTRAINT [DF_MaintenanceLogs_InvoiceNumber] DEFAULT (N'') FOR [InvoiceNumber];


GO
PRINT N'Creating Default Constraint [dbo].[DF_MaintenanceLogs_Cost]...';


GO
ALTER TABLE [dbo].[MaintenanceLogs]
    ADD CONSTRAINT [DF_MaintenanceLogs_Cost] DEFAULT (0) FOR [Cost];


GO
PRINT N'Creating Default Constraint [dbo].[DF_MaintenanceLogs_UpdatedAt]...';


GO
ALTER TABLE [dbo].[MaintenanceLogs]
    ADD CONSTRAINT [DF_MaintenanceLogs_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_MeasurementDensities_UpdatedAt]...';


GO
ALTER TABLE [dbo].[MeasurementDensities]
    ADD CONSTRAINT [DF_MeasurementDensities_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_MeasurementDensities_CreatedAt]...';


GO
ALTER TABLE [dbo].[MeasurementDensities]
    ADD CONSTRAINT [DF_MeasurementDensities_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_MeasurementDensities_IsActive]...';


GO
ALTER TABLE [dbo].[MeasurementDensities]
    ADD CONSTRAINT [DF_MeasurementDensities_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Notifications_Message]...';


GO
ALTER TABLE [dbo].[Notifications]
    ADD CONSTRAINT [DF_Notifications_Message] DEFAULT (N'') FOR [Message];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Notifications_Type]...';


GO
ALTER TABLE [dbo].[Notifications]
    ADD CONSTRAINT [DF_Notifications_Type] DEFAULT (0) FOR [Type];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Notifications_CreatedAt]...';


GO
ALTER TABLE [dbo].[Notifications]
    ADD CONSTRAINT [DF_Notifications_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Notifications_IsRead]...';


GO
ALTER TABLE [dbo].[Notifications]
    ADD CONSTRAINT [DF_Notifications_IsRead] DEFAULT (0) FOR [IsRead];


GO
PRINT N'Creating Default Constraint [dbo].[DF_OrderItems_BonusQuantity]...';


GO
ALTER TABLE [dbo].[OrderItems]
    ADD CONSTRAINT [DF_OrderItems_BonusQuantity] DEFAULT (0) FOR [BonusQuantity];


GO
PRINT N'Creating Default Constraint [dbo].[DF_OrderItems_CreatedAt]...';


GO
ALTER TABLE [dbo].[OrderItems]
    ADD CONSTRAINT [DF_OrderItems_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_OrderItems_UpdatedAt]...';


GO
ALTER TABLE [dbo].[OrderItems]
    ADD CONSTRAINT [DF_OrderItems_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_OrderPayments_Notes]...';


GO
ALTER TABLE [dbo].[OrderPayments]
    ADD CONSTRAINT [DF_OrderPayments_Notes] DEFAULT (N'') FOR [Notes];


GO
PRINT N'Creating Default Constraint [dbo].[DF_OrderPayments_ReceivedBy]...';


GO
ALTER TABLE [dbo].[OrderPayments]
    ADD CONSTRAINT [DF_OrderPayments_ReceivedBy] DEFAULT (N'') FOR [ReceivedBy];


GO
PRINT N'Creating Default Constraint [dbo].[DF_OrderPayments_Reference]...';


GO
ALTER TABLE [dbo].[OrderPayments]
    ADD CONSTRAINT [DF_OrderPayments_Reference] DEFAULT (N'') FOR [Reference];


GO
PRINT N'Creating Default Constraint [dbo].[DF_OrderPayments_PaymentDate]...';


GO
ALTER TABLE [dbo].[OrderPayments]
    ADD CONSTRAINT [DF_OrderPayments_PaymentDate] DEFAULT (SYSUTCDATETIME()) FOR [PaymentDate];


GO
PRINT N'Creating Default Constraint [dbo].[DF_OrderPayments_CreatedAt]...';


GO
ALTER TABLE [dbo].[OrderPayments]
    ADD CONSTRAINT [DF_OrderPayments_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_OrderPayments_PaymentMethod]...';


GO
ALTER TABLE [dbo].[OrderPayments]
    ADD CONSTRAINT [DF_OrderPayments_PaymentMethod] DEFAULT (0) FOR [PaymentMethod];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_CreatedAt]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_SubTotal]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_SubTotal] DEFAULT (0) FOR [SubTotal];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_OrderDate]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_OrderDate] DEFAULT (SYSUTCDATETIME()) FOR [OrderDate];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_UpdatedAt]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_CustomerCategory]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_CustomerCategory] DEFAULT (0) FOR [CustomerCategory];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_OrderType]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_OrderType] DEFAULT (0) FOR [OrderType];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_TargetDeliveryDate]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_TargetDeliveryDate] DEFAULT (SYSUTCDATETIME()) FOR [TargetDeliveryDate];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_PaymentStatus]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_PaymentStatus] DEFAULT (0) FOR [PaymentStatus];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_AmountPaid]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_AmountPaid] DEFAULT (0) FOR [AmountPaid];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_Discount]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_Discount] DEFAULT (0) FOR [Discount];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_PaymentMethod]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_PaymentMethod] DEFAULT (0) FOR [PaymentMethod];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_Status]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_Status] DEFAULT (0) FOR [Status];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_BookedByType]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_BookedByType] DEFAULT (0) FOR [BookedByType];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Orders_Notes]...';


GO
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [DF_Orders_Notes] DEFAULT (N'') FOR [Notes];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollDeductionLines_CreatedAt]...';


GO
ALTER TABLE [dbo].[PayrollDeductionLines]
    ADD CONSTRAINT [DF_PayrollDeductionLines_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_UpdatedAt]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_HealthInsuranceDeduction]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_HealthInsuranceDeduction] DEFAULT (0) FOR [HealthInsuranceDeduction];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_TransportAllowance]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_TransportAllowance] DEFAULT (0) FOR [TransportAllowance];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_DisciplinaryDeductions]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_DisciplinaryDeductions] DEFAULT (0) FOR [DisciplinaryDeductions];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_TaxAndLevies]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_TaxAndLevies] DEFAULT (0) FOR [TaxAndLevies];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_Notes]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_Notes] DEFAULT (N'') FOR [Notes];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_HousingAllowance]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_HousingAllowance] DEFAULT (0) FOR [HousingAllowance];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_PaymentStatus]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_PaymentStatus] DEFAULT (N'Pending') FOR [PaymentStatus];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_OtherAllowances]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_OtherAllowances] DEFAULT (0) FOR [OtherAllowances];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_PerformanceBonus]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_PerformanceBonus] DEFAULT (0) FOR [PerformanceBonus];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_CreatedAt]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_PensionDeduction]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_PensionDeduction] DEFAULT (0) FOR [PensionDeduction];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRecords_AdvanceDeduction]...';


GO
ALTER TABLE [dbo].[PayrollRecords]
    ADD CONSTRAINT [DF_PayrollRecords_AdvanceDeduction] DEFAULT (0) FOR [AdvanceDeduction];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRuns_GeneratedAt]...';


GO
ALTER TABLE [dbo].[PayrollRuns]
    ADD CONSTRAINT [DF_PayrollRuns_GeneratedAt] DEFAULT (SYSUTCDATETIME()) FOR [GeneratedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRuns_EmployeeCount]...';


GO
ALTER TABLE [dbo].[PayrollRuns]
    ADD CONSTRAINT [DF_PayrollRuns_EmployeeCount] DEFAULT (0) FOR [EmployeeCount];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRuns_DeductionsTotal]...';


GO
ALTER TABLE [dbo].[PayrollRuns]
    ADD CONSTRAINT [DF_PayrollRuns_DeductionsTotal] DEFAULT (0) FOR [DeductionsTotal];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRuns_CreatedAt]...';


GO
ALTER TABLE [dbo].[PayrollRuns]
    ADD CONSTRAINT [DF_PayrollRuns_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRuns_UpdatedAt]...';


GO
ALTER TABLE [dbo].[PayrollRuns]
    ADD CONSTRAINT [DF_PayrollRuns_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRuns_NetTotal]...';


GO
ALTER TABLE [dbo].[PayrollRuns]
    ADD CONSTRAINT [DF_PayrollRuns_NetTotal] DEFAULT (0) FOR [NetTotal];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRuns_Status]...';


GO
ALTER TABLE [dbo].[PayrollRuns]
    ADD CONSTRAINT [DF_PayrollRuns_Status] DEFAULT (N'Pending') FOR [Status];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRuns_Notes]...';


GO
ALTER TABLE [dbo].[PayrollRuns]
    ADD CONSTRAINT [DF_PayrollRuns_Notes] DEFAULT (N'') FOR [Notes];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRuns_GeneratedBy]...';


GO
ALTER TABLE [dbo].[PayrollRuns]
    ADD CONSTRAINT [DF_PayrollRuns_GeneratedBy] DEFAULT (N'System') FOR [GeneratedBy];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollRuns_GrossTotal]...';


GO
ALTER TABLE [dbo].[PayrollRuns]
    ADD CONSTRAINT [DF_PayrollRuns_GrossTotal] DEFAULT (0) FOR [GrossTotal];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollSettings_UpdatedBy]...';


GO
ALTER TABLE [dbo].[PayrollSettings]
    ADD CONSTRAINT [DF_PayrollSettings_UpdatedBy] DEFAULT (N'SuperAdmin') FOR [UpdatedBy];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollSettings_UpdatedAt]...';


GO
ALTER TABLE [dbo].[PayrollSettings]
    ADD CONSTRAINT [DF_PayrollSettings_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollSettings_HealthInsurancePercent]...';


GO
ALTER TABLE [dbo].[PayrollSettings]
    ADD CONSTRAINT [DF_PayrollSettings_HealthInsurancePercent] DEFAULT (2.65) FOR [HealthInsurancePercent];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollSettings_HousingAllowancePercent]...';


GO
ALTER TABLE [dbo].[PayrollSettings]
    ADD CONSTRAINT [DF_PayrollSettings_HousingAllowancePercent] DEFAULT (12.50) FOR [HousingAllowancePercent];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollSettings_LoanMonthlyRepaymentPercent]...';


GO
ALTER TABLE [dbo].[PayrollSettings]
    ADD CONSTRAINT [DF_PayrollSettings_LoanMonthlyRepaymentPercent] DEFAULT (20.00) FOR [LoanMonthlyRepaymentPercent];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollSettings_PensionPercent]...';


GO
ALTER TABLE [dbo].[PayrollSettings]
    ADD CONSTRAINT [DF_PayrollSettings_PensionPercent] DEFAULT (5.00) FOR [PensionPercent];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollSettings_MaxLoanPercentOfSalary]...';


GO
ALTER TABLE [dbo].[PayrollSettings]
    ADD CONSTRAINT [DF_PayrollSettings_MaxLoanPercentOfSalary] DEFAULT (50.00) FOR [MaxLoanPercentOfSalary];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollSettings_PayeTaxPercent]...';


GO
ALTER TABLE [dbo].[PayrollSettings]
    ADD CONSTRAINT [DF_PayrollSettings_PayeTaxPercent] DEFAULT (3.50) FOR [PayeTaxPercent];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PayrollSettings_TransportAllowancePercent]...';


GO
ALTER TABLE [dbo].[PayrollSettings]
    ADD CONSTRAINT [DF_PayrollSettings_TransportAllowancePercent] DEFAULT (6.25) FOR [TransportAllowancePercent];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PosTerminals_PosBankAccountName]...';


GO
ALTER TABLE [dbo].[PosTerminals]
    ADD CONSTRAINT [DF_PosTerminals_PosBankAccountName] DEFAULT (N'Moniepoint POS - 9028129') FOR [PosBankAccountName];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PosTerminals_TransferAccountName]...';


GO
ALTER TABLE [dbo].[PosTerminals]
    ADD CONSTRAINT [DF_PosTerminals_TransferAccountName] DEFAULT (N'FirstBank - 3021948291') FOR [TransferAccountName];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PosTerminals_CashAccountName]...';


GO
ALTER TABLE [dbo].[PosTerminals]
    ADD CONSTRAINT [DF_PosTerminals_CashAccountName] DEFAULT (N'Bakery Cash Drawer') FOR [CashAccountName];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PosTerminals_CreatedAt]...';


GO
ALTER TABLE [dbo].[PosTerminals]
    ADD CONSTRAINT [DF_PosTerminals_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PosTerminals_IsActive]...';


GO
ALTER TABLE [dbo].[PosTerminals]
    ADD CONSTRAINT [DF_PosTerminals_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PosTerminals_UpdatedAt]...';


GO
ALTER TABLE [dbo].[PosTerminals]
    ADD CONSTRAINT [DF_PosTerminals_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_Shift]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_Shift] DEFAULT (N'Morning') FOR [Shift];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_ExpectedUnitCost]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_ExpectedUnitCost] DEFAULT (0) FOR [ExpectedUnitCost];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_Status]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_Status] DEFAULT (0) FOR [Status];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_FlourBagsUsed]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_FlourBagsUsed] DEFAULT (0) FOR [FlourBagsUsed];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_CalculatedUnitCost]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_CalculatedUnitCost] DEFAULT (0) FOR [CalculatedUnitCost];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_ProductionDate]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_ProductionDate] DEFAULT (SYSUTCDATETIME()) FOR [ProductionDate];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_UpdatedAt]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_CreatedAt]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_BatchInputQuantity]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_BatchInputQuantity] DEFAULT (1) FOR [BatchInputQuantity];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_Notes]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_Notes] DEFAULT (N'') FOR [Notes];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_BatchInputUnitLabel]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_BatchInputUnitLabel] DEFAULT (N'Flour Bags (50kg)') FOR [BatchInputUnitLabel];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_TotalBatchCost]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_TotalBatchCost] DEFAULT (0) FOR [TotalBatchCost];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_OutputUnitLabel]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_OutputUnitLabel] DEFAULT (N'loaves') FOR [OutputUnitLabel];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_ActualQuantity]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_ActualQuantity] DEFAULT (0) FOR [ActualQuantity];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_LoggedBy]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_LoggedBy] DEFAULT (N'') FOR [LoggedBy];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_TargetOrderQuantity]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_TargetOrderQuantity] DEFAULT (0) FOR [TargetOrderQuantity];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatches_BakerInCharge]...';


GO
ALTER TABLE [dbo].[ProductionBatches]
    ADD CONSTRAINT [DF_ProductionBatches_BakerInCharge] DEFAULT (N'') FOR [BakerInCharge];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatchItems_ActualQuantity]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems]
    ADD CONSTRAINT [DF_ProductionBatchItems_ActualQuantity] DEFAULT (0) FOR [ActualQuantity];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatchItems_ExpectedUnitCost]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems]
    ADD CONSTRAINT [DF_ProductionBatchItems_ExpectedUnitCost] DEFAULT (0) FOR [ExpectedUnitCost];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatchItems_UpdatedAt]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems]
    ADD CONSTRAINT [DF_ProductionBatchItems_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatchItems_RecipeName]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems]
    ADD CONSTRAINT [DF_ProductionBatchItems_RecipeName] DEFAULT (N'') FOR [RecipeName];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatchItems_CreatedAt]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems]
    ADD CONSTRAINT [DF_ProductionBatchItems_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatchItems_TotalBatchCost]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems]
    ADD CONSTRAINT [DF_ProductionBatchItems_TotalBatchCost] DEFAULT (0) FOR [TotalBatchCost];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatchItems_ProductCode]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems]
    ADD CONSTRAINT [DF_ProductionBatchItems_ProductCode] DEFAULT (N'') FOR [ProductCode];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatchItems_CalculatedUnitCost]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems]
    ADD CONSTRAINT [DF_ProductionBatchItems_CalculatedUnitCost] DEFAULT (0) FOR [CalculatedUnitCost];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatchItems_BatchInputQuantity]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems]
    ADD CONSTRAINT [DF_ProductionBatchItems_BatchInputQuantity] DEFAULT (0) FOR [BatchInputQuantity];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatchItems_TargetQuantity]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems]
    ADD CONSTRAINT [DF_ProductionBatchItems_TargetQuantity] DEFAULT (0) FOR [TargetQuantity];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatchItems_OutputUnitLabel]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems]
    ADD CONSTRAINT [DF_ProductionBatchItems_OutputUnitLabel] DEFAULT (N'') FOR [OutputUnitLabel];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionBatchItems_BatchInputUnitLabel]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems]
    ADD CONSTRAINT [DF_ProductionBatchItems_BatchInputUnitLabel] DEFAULT (N'') FOR [BatchInputUnitLabel];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionCategories_UpdatedAt]...';


GO
ALTER TABLE [dbo].[ProductionCategories]
    ADD CONSTRAINT [DF_ProductionCategories_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionCategories_CreatedAt]...';


GO
ALTER TABLE [dbo].[ProductionCategories]
    ADD CONSTRAINT [DF_ProductionCategories_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionCategories_IsActive]...';


GO
ALTER TABLE [dbo].[ProductionCategories]
    ADD CONSTRAINT [DF_ProductionCategories_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionCategories_Description]...';


GO
ALTER TABLE [dbo].[ProductionCategories]
    ADD CONSTRAINT [DF_ProductionCategories_Description] DEFAULT (N'') FOR [Description];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionDefects_RecordedAt]...';


GO
ALTER TABLE [dbo].[ProductionDefects]
    ADD CONSTRAINT [DF_ProductionDefects_RecordedAt] DEFAULT (SYSUTCDATETIME()) FOR [RecordedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionDefects_DefectReason]...';


GO
ALTER TABLE [dbo].[ProductionDefects]
    ADD CONSTRAINT [DF_ProductionDefects_DefectReason] DEFAULT (N'') FOR [DefectReason];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionDefects_RecordedBy]...';


GO
ALTER TABLE [dbo].[ProductionDefects]
    ADD CONSTRAINT [DF_ProductionDefects_RecordedBy] DEFAULT (N'') FOR [RecordedBy];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionDefects_CreatedAt]...';


GO
ALTER TABLE [dbo].[ProductionDefects]
    ADD CONSTRAINT [DF_ProductionDefects_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionProductTypes_IsActive]...';


GO
ALTER TABLE [dbo].[ProductionProductTypes]
    ADD CONSTRAINT [DF_ProductionProductTypes_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionProductTypes_CreatedAt]...';


GO
ALTER TABLE [dbo].[ProductionProductTypes]
    ADD CONSTRAINT [DF_ProductionProductTypes_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionProductTypes_UpdatedAt]...';


GO
ALTER TABLE [dbo].[ProductionProductTypes]
    ADD CONSTRAINT [DF_ProductionProductTypes_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionShifts_IsActive]...';


GO
ALTER TABLE [dbo].[ProductionShifts]
    ADD CONSTRAINT [DF_ProductionShifts_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionShifts_CreatedAt]...';


GO
ALTER TABLE [dbo].[ProductionShifts]
    ADD CONSTRAINT [DF_ProductionShifts_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductionShifts_UpdatedAt]...';


GO
ALTER TABLE [dbo].[ProductionShifts]
    ADD CONSTRAINT [DF_ProductionShifts_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositories_UpdatedAt]...';


GO
ALTER TABLE [dbo].[ProductRepositories]
    ADD CONSTRAINT [DF_ProductRepositories_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositories_IsDefaultProductionOutput]...';


GO
ALTER TABLE [dbo].[ProductRepositories]
    ADD CONSTRAINT [DF_ProductRepositories_IsDefaultProductionOutput] DEFAULT (0) FOR [IsDefaultProductionOutput];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositories_IsActive]...';


GO
ALTER TABLE [dbo].[ProductRepositories]
    ADD CONSTRAINT [DF_ProductRepositories_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositories_Address]...';


GO
ALTER TABLE [dbo].[ProductRepositories]
    ADD CONSTRAINT [DF_ProductRepositories_Address] DEFAULT (N'') FOR [Address];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositories_LocationType]...';


GO
ALTER TABLE [dbo].[ProductRepositories]
    ADD CONSTRAINT [DF_ProductRepositories_LocationType] DEFAULT (0) FOR [LocationType];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositories_CreatedAt]...';


GO
ALTER TABLE [dbo].[ProductRepositories]
    ADD CONSTRAINT [DF_ProductRepositories_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositoryMovements_ReferenceNumber]...';


GO
ALTER TABLE [dbo].[ProductRepositoryMovements]
    ADD CONSTRAINT [DF_ProductRepositoryMovements_ReferenceNumber] DEFAULT (N'') FOR [ReferenceNumber];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositoryMovements_Notes]...';


GO
ALTER TABLE [dbo].[ProductRepositoryMovements]
    ADD CONSTRAINT [DF_ProductRepositoryMovements_Notes] DEFAULT (N'') FOR [Notes];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositoryMovements_ReferenceType]...';


GO
ALTER TABLE [dbo].[ProductRepositoryMovements]
    ADD CONSTRAINT [DF_ProductRepositoryMovements_ReferenceType] DEFAULT (N'') FOR [ReferenceType];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositoryMovements_PerformedBy]...';


GO
ALTER TABLE [dbo].[ProductRepositoryMovements]
    ADD CONSTRAINT [DF_ProductRepositoryMovements_PerformedBy] DEFAULT (N'') FOR [PerformedBy];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositoryMovements_CreatedAt]...';


GO
ALTER TABLE [dbo].[ProductRepositoryMovements]
    ADD CONSTRAINT [DF_ProductRepositoryMovements_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositoryStocks_UpdatedAt]...';


GO
ALTER TABLE [dbo].[ProductRepositoryStocks]
    ADD CONSTRAINT [DF_ProductRepositoryStocks_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositoryStocks_QuantityOnHand]...';


GO
ALTER TABLE [dbo].[ProductRepositoryStocks]
    ADD CONSTRAINT [DF_ProductRepositoryStocks_QuantityOnHand] DEFAULT (0) FOR [QuantityOnHand];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositoryStocks_CreatedAt]...';


GO
ALTER TABLE [dbo].[ProductRepositoryStocks]
    ADD CONSTRAINT [DF_ProductRepositoryStocks_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProductRepositoryStocks_ReorderThreshold]...';


GO
ALTER TABLE [dbo].[ProductRepositoryStocks]
    ADD CONSTRAINT [DF_ProductRepositoryStocks_ReorderThreshold] DEFAULT (0) FOR [ReorderThreshold];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacModules_Description]...';


GO
ALTER TABLE [dbo].[RbacModules]
    ADD CONSTRAINT [DF_RbacModules_Description] DEFAULT (N'') FOR [Description];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacModules_SortOrder]...';


GO
ALTER TABLE [dbo].[RbacModules]
    ADD CONSTRAINT [DF_RbacModules_SortOrder] DEFAULT (0) FOR [SortOrder];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacModules_CreatedAt]...';


GO
ALTER TABLE [dbo].[RbacModules]
    ADD CONSTRAINT [DF_RbacModules_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacModules_IsActive]...';


GO
ALTER TABLE [dbo].[RbacModules]
    ADD CONSTRAINT [DF_RbacModules_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacModules_UpdatedAt]...';


GO
ALTER TABLE [dbo].[RbacModules]
    ADD CONSTRAINT [DF_RbacModules_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionAudits_ChangedBy]...';


GO
ALTER TABLE [dbo].[RbacPermissionAudits]
    ADD CONSTRAINT [DF_RbacPermissionAudits_ChangedBy] DEFAULT (N'System') FOR [ChangedBy];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionAudits_ChangedAt]...';


GO
ALTER TABLE [dbo].[RbacPermissionAudits]
    ADD CONSTRAINT [DF_RbacPermissionAudits_ChangedAt] DEFAULT (SYSUTCDATETIME()) FOR [ChangedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_CanView]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_CanView] DEFAULT (0) FOR [CanView];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_CanEdit]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_CanEdit] DEFAULT (0) FOR [CanEdit];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_CanBookOwn]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_CanBookOwn] DEFAULT (0) FOR [CanBookOwn];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_UpdatedAt]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_CanPrint]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_CanPrint] DEFAULT (0) FOR [CanPrint];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_IsNone]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_IsNone] DEFAULT (0) FOR [IsNone];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_CanDelete]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_CanDelete] DEFAULT (0) FOR [CanDelete];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_CanDisburse]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_CanDisburse] DEFAULT (0) FOR [CanDisburse];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_CanRequest]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_CanRequest] DEFAULT (0) FOR [CanRequest];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_Rank]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_Rank] DEFAULT (0) FOR [Rank];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_CreatedAt]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_CanIssue]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_CanIssue] DEFAULT (0) FOR [CanIssue];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_CanManage]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_CanManage] DEFAULT (0) FOR [CanManage];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_SortOrder]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_SortOrder] DEFAULT (0) FOR [SortOrder];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_IsActive]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_CanApprove]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_CanApprove] DEFAULT (0) FOR [CanApprove];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacPermissionLevels_CanCreate]...';


GO
ALTER TABLE [dbo].[RbacPermissionLevels]
    ADD CONSTRAINT [DF_RbacPermissionLevels_CanCreate] DEFAULT (0) FOR [CanCreate];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacRolePermissions_UpdatedBy]...';


GO
ALTER TABLE [dbo].[RbacRolePermissions]
    ADD CONSTRAINT [DF_RbacRolePermissions_UpdatedBy] DEFAULT (N'System') FOR [UpdatedBy];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RbacRolePermissions_UpdatedAt]...';


GO
ALTER TABLE [dbo].[RbacRolePermissions]
    ADD CONSTRAINT [DF_RbacRolePermissions_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RecipeIngredients_BaselineYield]...';


GO
ALTER TABLE [dbo].[RecipeIngredients]
    ADD CONSTRAINT [DF_RecipeIngredients_BaselineYield] DEFAULT (1) FOR [BaselineYield];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RecipeIngredients_UpdatedAt]...';


GO
ALTER TABLE [dbo].[RecipeIngredients]
    ADD CONSTRAINT [DF_RecipeIngredients_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RecipeIngredients_CreatedAt]...';


GO
ALTER TABLE [dbo].[RecipeIngredients]
    ADD CONSTRAINT [DF_RecipeIngredients_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_RecipeIngredients_IngredientUnitCost]...';


GO
ALTER TABLE [dbo].[RecipeIngredients]
    ADD CONSTRAINT [DF_RecipeIngredients_IngredientUnitCost] DEFAULT (0) FOR [IngredientUnitCost];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Recipes_IsActive]...';


GO
ALTER TABLE [dbo].[Recipes]
    ADD CONSTRAINT [DF_Recipes_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Recipes_OutputUnit]...';


GO
ALTER TABLE [dbo].[Recipes]
    ADD CONSTRAINT [DF_Recipes_OutputUnit] DEFAULT (N'loaves') FOR [OutputUnit];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Recipes_SellingPrice]...';


GO
ALTER TABLE [dbo].[Recipes]
    ADD CONSTRAINT [DF_Recipes_SellingPrice] DEFAULT (0) FOR [SellingPrice];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Recipes_CreatedAt]...';


GO
ALTER TABLE [dbo].[Recipes]
    ADD CONSTRAINT [DF_Recipes_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Recipes_UpdatedAt]...';


GO
ALTER TABLE [dbo].[Recipes]
    ADD CONSTRAINT [DF_Recipes_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Recipes_Description]...';


GO
ALTER TABLE [dbo].[Recipes]
    ADD CONSTRAINT [DF_Recipes_Description] DEFAULT (N'') FOR [Description];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Roles_CreatedAt]...';


GO
ALTER TABLE [dbo].[Roles]
    ADD CONSTRAINT [DF_Roles_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Roles_Description]...';


GO
ALTER TABLE [dbo].[Roles]
    ADD CONSTRAINT [DF_Roles_Description] DEFAULT (N'') FOR [Description];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Roles_IsSystemRole]...';


GO
ALTER TABLE [dbo].[Roles]
    ADD CONSTRAINT [DF_Roles_IsSystemRole] DEFAULT (1) FOR [IsSystemRole];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Roles_UpdatedAt]...';


GO
ALTER TABLE [dbo].[Roles]
    ADD CONSTRAINT [DF_Roles_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalaryAdvances_CreatedAt]...';


GO
ALTER TABLE [dbo].[SalaryAdvances]
    ADD CONSTRAINT [DF_SalaryAdvances_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalaryAdvances_Status]...';


GO
ALTER TABLE [dbo].[SalaryAdvances]
    ADD CONSTRAINT [DF_SalaryAdvances_Status] DEFAULT (N'Active') FOR [Status];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalaryAdvances_Reason]...';


GO
ALTER TABLE [dbo].[SalaryAdvances]
    ADD CONSTRAINT [DF_SalaryAdvances_Reason] DEFAULT (N'') FOR [Reason];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalaryAdvances_TotalRepaid]...';


GO
ALTER TABLE [dbo].[SalaryAdvances]
    ADD CONSTRAINT [DF_SalaryAdvances_TotalRepaid] DEFAULT (0) FOR [TotalRepaid];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalaryAdvances_RequestDate]...';


GO
ALTER TABLE [dbo].[SalaryAdvances]
    ADD CONSTRAINT [DF_SalaryAdvances_RequestDate] DEFAULT (CONVERT(DATE, SYSUTCDATETIME())) FOR [RequestDate];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalaryAdvances_UpdatedAt]...';


GO
ALTER TABLE [dbo].[SalaryAdvances]
    ADD CONSTRAINT [DF_SalaryAdvances_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesBonusRules_IsActive]...';


GO
ALTER TABLE [dbo].[SalesBonusRules]
    ADD CONSTRAINT [DF_SalesBonusRules_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesBonusRules_CreatedAt]...';


GO
ALTER TABLE [dbo].[SalesBonusRules]
    ADD CONSTRAINT [DF_SalesBonusRules_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesBonusRules_UpdatedAt]...';


GO
ALTER TABLE [dbo].[SalesBonusRules]
    ADD CONSTRAINT [DF_SalesBonusRules_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliationItems_QtyDamaged]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems]
    ADD CONSTRAINT [DF_SalesReconciliationItems_QtyDamaged] DEFAULT (0) FOR [QtyDamaged];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliationItems_ProductName]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems]
    ADD CONSTRAINT [DF_SalesReconciliationItems_ProductName] DEFAULT (N'') FOR [ProductName];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliationItems_QtySold]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems]
    ADD CONSTRAINT [DF_SalesReconciliationItems_QtySold] DEFAULT (0) FOR [QtySold];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliationItems_QtyReplacement]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems]
    ADD CONSTRAINT [DF_SalesReconciliationItems_QtyReplacement] DEFAULT (0) FOR [QtyReplacement];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliationItems_UpdatedAt]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems]
    ADD CONSTRAINT [DF_SalesReconciliationItems_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliationItems_CreatedAt]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems]
    ADD CONSTRAINT [DF_SalesReconciliationItems_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliationItems_UnitPrice]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems]
    ADD CONSTRAINT [DF_SalesReconciliationItems_UnitPrice] DEFAULT (0) FOR [UnitPrice];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliationItems_ProductCode]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems]
    ADD CONSTRAINT [DF_SalesReconciliationItems_ProductCode] DEFAULT (N'') FOR [ProductCode];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliationItems_OpeningStock]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems]
    ADD CONSTRAINT [DF_SalesReconciliationItems_OpeningStock] DEFAULT (0) FOR [OpeningStock];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliations_RouteName]...';


GO
ALTER TABLE [dbo].[SalesReconciliations]
    ADD CONSTRAINT [DF_SalesReconciliations_RouteName] DEFAULT (N'') FOR [RouteName];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliations_Status]...';


GO
ALTER TABLE [dbo].[SalesReconciliations]
    ADD CONSTRAINT [DF_SalesReconciliations_Status] DEFAULT (0) FOR [Status];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliations_PosTerminalReceipts]...';


GO
ALTER TABLE [dbo].[SalesReconciliations]
    ADD CONSTRAINT [DF_SalesReconciliations_PosTerminalReceipts] DEFAULT (0) FOR [PosTerminalReceipts];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliations_VehicleRegistration]...';


GO
ALTER TABLE [dbo].[SalesReconciliations]
    ADD CONSTRAINT [DF_SalesReconciliations_VehicleRegistration] DEFAULT (N'') FOR [VehicleRegistration];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliations_TotalExpectedRevenue]...';


GO
ALTER TABLE [dbo].[SalesReconciliations]
    ADD CONSTRAINT [DF_SalesReconciliations_TotalExpectedRevenue] DEFAULT (0) FOR [TotalExpectedRevenue];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliations_UpdatedAt]...';


GO
ALTER TABLE [dbo].[SalesReconciliations]
    ADD CONSTRAINT [DF_SalesReconciliations_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliations_CreatedAt]...';


GO
ALTER TABLE [dbo].[SalesReconciliations]
    ADD CONSTRAINT [DF_SalesReconciliations_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliations_Notes]...';


GO
ALTER TABLE [dbo].[SalesReconciliations]
    ADD CONSTRAINT [DF_SalesReconciliations_Notes] DEFAULT (N'') FOR [Notes];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReconciliations_CashHandedOver]...';


GO
ALTER TABLE [dbo].[SalesReconciliations]
    ADD CONSTRAINT [DF_SalesReconciliations_CashHandedOver] DEFAULT (0) FOR [CashHandedOver];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReps_IsActive]...';


GO
ALTER TABLE [dbo].[SalesReps]
    ADD CONSTRAINT [DF_SalesReps_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReps_CreatedAt]...';


GO
ALTER TABLE [dbo].[SalesReps]
    ADD CONSTRAINT [DF_SalesReps_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReps_UpdatedAt]...';


GO
ALTER TABLE [dbo].[SalesReps]
    ADD CONSTRAINT [DF_SalesReps_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReps_ProductFocus]...';


GO
ALTER TABLE [dbo].[SalesReps]
    ADD CONSTRAINT [DF_SalesReps_ProductFocus] DEFAULT (N'') FOR [ProductFocus];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SalesReps_PhoneNumber]...';


GO
ALTER TABLE [dbo].[SalesReps]
    ADD CONSTRAINT [DF_SalesReps_PhoneNumber] DEFAULT (N'') FOR [PhoneNumber];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StockMovements_CreatedAt]...';


GO
ALTER TABLE [dbo].[StockMovements]
    ADD CONSTRAINT [DF_StockMovements_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StockMovements_PerformedBy]...';


GO
ALTER TABLE [dbo].[StockMovements]
    ADD CONSTRAINT [DF_StockMovements_PerformedBy] DEFAULT (N'System') FOR [PerformedBy];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequestItems_UpdatedAt]...';


GO
ALTER TABLE [dbo].[StoreRequestItems]
    ADD CONSTRAINT [DF_StoreRequestItems_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequestItems_StockQuantity]...';


GO
ALTER TABLE [dbo].[StoreRequestItems]
    ADD CONSTRAINT [DF_StoreRequestItems_StockQuantity] DEFAULT (0) FOR [StockQuantity];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequestItems_StockUnit]...';


GO
ALTER TABLE [dbo].[StoreRequestItems]
    ADD CONSTRAINT [DF_StoreRequestItems_StockUnit] DEFAULT (N'') FOR [StockUnit];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequestItems_BaseUnit]...';


GO
ALTER TABLE [dbo].[StoreRequestItems]
    ADD CONSTRAINT [DF_StoreRequestItems_BaseUnit] DEFAULT (N'') FOR [BaseUnit];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequestItems_CreatedAt]...';


GO
ALTER TABLE [dbo].[StoreRequestItems]
    ADD CONSTRAINT [DF_StoreRequestItems_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequestItems_IssuedQuantity]...';


GO
ALTER TABLE [dbo].[StoreRequestItems]
    ADD CONSTRAINT [DF_StoreRequestItems_IssuedQuantity] DEFAULT (0) FOR [IssuedQuantity];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequestItems_BaseQuantity]...';


GO
ALTER TABLE [dbo].[StoreRequestItems]
    ADD CONSTRAINT [DF_StoreRequestItems_BaseQuantity] DEFAULT (0) FOR [BaseQuantity];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequests_RequestedAt]...';


GO
ALTER TABLE [dbo].[StoreRequests]
    ADD CONSTRAINT [DF_StoreRequests_RequestedAt] DEFAULT (SYSUTCDATETIME()) FOR [RequestedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequests_Purpose]...';


GO
ALTER TABLE [dbo].[StoreRequests]
    ADD CONSTRAINT [DF_StoreRequests_Purpose] DEFAULT (N'') FOR [Purpose];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequests_UpdatedAt]...';


GO
ALTER TABLE [dbo].[StoreRequests]
    ADD CONSTRAINT [DF_StoreRequests_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequests_Status]...';


GO
ALTER TABLE [dbo].[StoreRequests]
    ADD CONSTRAINT [DF_StoreRequests_Status] DEFAULT (0) FOR [Status];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequests_Department]...';


GO
ALTER TABLE [dbo].[StoreRequests]
    ADD CONSTRAINT [DF_StoreRequests_Department] DEFAULT (N'Bakery Floor') FOR [Department];


GO
PRINT N'Creating Default Constraint [dbo].[DF_StoreRequests_CreatedAt]...';


GO
ALTER TABLE [dbo].[StoreRequests]
    ADD CONSTRAINT [DF_StoreRequests_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SupermarketAccountPrices_CreatedAt]...';


GO
ALTER TABLE [dbo].[SupermarketAccountPrices]
    ADD CONSTRAINT [DF_SupermarketAccountPrices_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SupermarketAccountPrices_UpdatedAt]...';


GO
ALTER TABLE [dbo].[SupermarketAccountPrices]
    ADD CONSTRAINT [DF_SupermarketAccountPrices_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SupermarketAccounts_CurrentOutstandingCredit]...';


GO
ALTER TABLE [dbo].[SupermarketAccounts]
    ADD CONSTRAINT [DF_SupermarketAccounts_CurrentOutstandingCredit] DEFAULT (0) FOR [CurrentOutstandingCredit];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SupermarketAccounts_IsActive]...';


GO
ALTER TABLE [dbo].[SupermarketAccounts]
    ADD CONSTRAINT [DF_SupermarketAccounts_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SupermarketAccounts_CreatedAt]...';


GO
ALTER TABLE [dbo].[SupermarketAccounts]
    ADD CONSTRAINT [DF_SupermarketAccounts_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SupermarketAccounts_CreditLimit]...';


GO
ALTER TABLE [dbo].[SupermarketAccounts]
    ADD CONSTRAINT [DF_SupermarketAccounts_CreditLimit] DEFAULT (250000) FOR [CreditLimit];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SupermarketAccounts_Address]...';


GO
ALTER TABLE [dbo].[SupermarketAccounts]
    ADD CONSTRAINT [DF_SupermarketAccounts_Address] DEFAULT (N'') FOR [Address];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SupermarketAccounts_UpdatedAt]...';


GO
ALTER TABLE [dbo].[SupermarketAccounts]
    ADD CONSTRAINT [DF_SupermarketAccounts_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SupermarketAccounts_ContactPerson]...';


GO
ALTER TABLE [dbo].[SupermarketAccounts]
    ADD CONSTRAINT [DF_SupermarketAccounts_ContactPerson] DEFAULT (N'') FOR [ContactPerson];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SupermarketAccounts_PaymentTerms]...';


GO
ALTER TABLE [dbo].[SupermarketAccounts]
    ADD CONSTRAINT [DF_SupermarketAccounts_PaymentTerms] DEFAULT (N'Net 15 Days') FOR [PaymentTerms];


GO
PRINT N'Creating Default Constraint [dbo].[DF_SupermarketAccounts_PhoneNumber]...';


GO
ALTER TABLE [dbo].[SupermarketAccounts]
    ADD CONSTRAINT [DF_SupermarketAccounts_PhoneNumber] DEFAULT (N'') FOR [PhoneNumber];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Suppliers_CreatedAt]...';


GO
ALTER TABLE [dbo].[Suppliers]
    ADD CONSTRAINT [DF_Suppliers_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Suppliers_UpdatedAt]...';


GO
ALTER TABLE [dbo].[Suppliers]
    ADD CONSTRAINT [DF_Suppliers_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Suppliers_Address]...';


GO
ALTER TABLE [dbo].[Suppliers]
    ADD CONSTRAINT [DF_Suppliers_Address] DEFAULT (N'') FOR [Address];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Suppliers_SupplyCategory]...';


GO
ALTER TABLE [dbo].[Suppliers]
    ADD CONSTRAINT [DF_Suppliers_SupplyCategory] DEFAULT (N'Flour & Grains') FOR [SupplyCategory];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Suppliers_ContactPerson]...';


GO
ALTER TABLE [dbo].[Suppliers]
    ADD CONSTRAINT [DF_Suppliers_ContactPerson] DEFAULT (N'') FOR [ContactPerson];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Suppliers_IsActive]...';


GO
ALTER TABLE [dbo].[Suppliers]
    ADD CONSTRAINT [DF_Suppliers_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Suppliers_PhoneNumber]...';


GO
ALTER TABLE [dbo].[Suppliers]
    ADD CONSTRAINT [DF_Suppliers_PhoneNumber] DEFAULT (N'') FOR [PhoneNumber];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Suppliers_Email]...';


GO
ALTER TABLE [dbo].[Suppliers]
    ADD CONSTRAINT [DF_Suppliers_Email] DEFAULT (N'') FOR [Email];


GO
PRINT N'Creating Default Constraint [dbo].[DF_TreasuryPolicySettings_PolicyMemo]...';


GO
ALTER TABLE [dbo].[TreasuryPolicySettings]
    ADD CONSTRAINT [DF_TreasuryPolicySettings_PolicyMemo] DEFAULT (N'') FOR [PolicyMemo];


GO
PRINT N'Creating Default Constraint [dbo].[DF_TreasuryPolicySettings_PlannedCashPercent]...';


GO
ALTER TABLE [dbo].[TreasuryPolicySettings]
    ADD CONSTRAINT [DF_TreasuryPolicySettings_PlannedCashPercent] DEFAULT (32.00) FOR [PlannedCashPercent];


GO
PRINT N'Creating Default Constraint [dbo].[DF_TreasuryPolicySettings_LastUpdatedUtc]...';


GO
ALTER TABLE [dbo].[TreasuryPolicySettings]
    ADD CONSTRAINT [DF_TreasuryPolicySettings_LastUpdatedUtc] DEFAULT (SYSUTCDATETIME()) FOR [LastUpdatedUtc];


GO
PRINT N'Creating Default Constraint [dbo].[DF_TreasuryPolicySettings_UpdatedBy]...';


GO
ALTER TABLE [dbo].[TreasuryPolicySettings]
    ADD CONSTRAINT [DF_TreasuryPolicySettings_UpdatedBy] DEFAULT (N'SuperAdmin') FOR [UpdatedBy];


GO
PRINT N'Creating Default Constraint [dbo].[DF_TreasuryPolicySettings_AlertTolerancePercent]...';


GO
ALTER TABLE [dbo].[TreasuryPolicySettings]
    ADD CONSTRAINT [DF_TreasuryPolicySettings_AlertTolerancePercent] DEFAULT (6.00) FOR [AlertTolerancePercent];


GO
PRINT N'Creating Default Constraint [dbo].[DF_TripLegs_LegCost]...';


GO
ALTER TABLE [dbo].[TripLegs]
    ADD CONSTRAINT [DF_TripLegs_LegCost] DEFAULT (0) FOR [LegCost];


GO
PRINT N'Creating Default Constraint [dbo].[DF_TripLegs_FuelOption]...';


GO
ALTER TABLE [dbo].[TripLegs]
    ADD CONSTRAINT [DF_TripLegs_FuelOption] DEFAULT (0) FOR [FuelOption];


GO
PRINT N'Creating Default Constraint [dbo].[DF_TripLegs_CreatedAt]...';


GO
ALTER TABLE [dbo].[TripLegs]
    ADD CONSTRAINT [DF_TripLegs_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Users_MustChangePassword]...';


GO
ALTER TABLE [dbo].[Users]
    ADD CONSTRAINT [DF_Users_MustChangePassword] DEFAULT (1) FOR [MustChangePassword];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Users_UpdatedAt]...';


GO
ALTER TABLE [dbo].[Users]
    ADD CONSTRAINT [DF_Users_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Users_IsActive]...';


GO
ALTER TABLE [dbo].[Users]
    ADD CONSTRAINT [DF_Users_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Users_SecurityStamp]...';


GO
ALTER TABLE [dbo].[Users]
    ADD CONSTRAINT [DF_Users_SecurityStamp] DEFAULT (NEWID()) FOR [SecurityStamp];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Users_CreatedAt]...';


GO
ALTER TABLE [dbo].[Users]
    ADD CONSTRAINT [DF_Users_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Vehicles_CurrentOdometerKm]...';


GO
ALTER TABLE [dbo].[Vehicles]
    ADD CONSTRAINT [DF_Vehicles_CurrentOdometerKm] DEFAULT (0) FOR [CurrentOdometerKm];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Vehicles_Status]...';


GO
ALTER TABLE [dbo].[Vehicles]
    ADD CONSTRAINT [DF_Vehicles_Status] DEFAULT (N'Active') FOR [Status];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Vehicles_IsActive]...';


GO
ALTER TABLE [dbo].[Vehicles]
    ADD CONSTRAINT [DF_Vehicles_IsActive] DEFAULT (1) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Vehicles_UpdatedAt]...';


GO
ALTER TABLE [dbo].[Vehicles]
    ADD CONSTRAINT [DF_Vehicles_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Vehicles_CreatedAt]...';


GO
ALTER TABLE [dbo].[Vehicles]
    ADD CONSTRAINT [DF_Vehicles_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Vehicles_VehicleType]...';


GO
ALTER TABLE [dbo].[Vehicles]
    ADD CONSTRAINT [DF_Vehicles_VehicleType] DEFAULT (N'Van') FOR [VehicleType];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Vehicles_FuelType]...';


GO
ALTER TABLE [dbo].[Vehicles]
    ADD CONSTRAINT [DF_Vehicles_FuelType] DEFAULT (N'Petrol') FOR [FuelType];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Vehicles_TankCapacityLitres]...';


GO
ALTER TABLE [dbo].[Vehicles]
    ADD CONSTRAINT [DF_Vehicles_TankCapacityLitres] DEFAULT (70) FOR [TankCapacityLitres];


GO
PRINT N'Creating Default Constraint [dbo].[DF_Vehicles_VehicleFunction]...';


GO
ALTER TABLE [dbo].[Vehicles]
    ADD CONSTRAINT [DF_Vehicles_VehicleFunction] DEFAULT (N'Deliveries') FOR [VehicleFunction];


GO
PRINT N'Creating Default Constraint [dbo].[DF_WeeklyFleetExpenses_IncidentalsTotal]...';


GO
ALTER TABLE [dbo].[WeeklyFleetExpenses]
    ADD CONSTRAINT [DF_WeeklyFleetExpenses_IncidentalsTotal] DEFAULT (0) FOR [IncidentalsTotal];


GO
PRINT N'Creating Default Constraint [dbo].[DF_WeeklyFleetExpenses_FuelExpenseTotal]...';


GO
ALTER TABLE [dbo].[WeeklyFleetExpenses]
    ADD CONSTRAINT [DF_WeeklyFleetExpenses_FuelExpenseTotal] DEFAULT (0) FOR [FuelExpenseTotal];


GO
PRINT N'Creating Default Constraint [dbo].[DF_WeeklyFleetExpenses_UpdatedAt]...';


GO
ALTER TABLE [dbo].[WeeklyFleetExpenses]
    ADD CONSTRAINT [DF_WeeklyFleetExpenses_UpdatedAt] DEFAULT (SYSUTCDATETIME()) FOR [UpdatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_WeeklyFleetExpenses_CreatedAt]...';


GO
ALTER TABLE [dbo].[WeeklyFleetExpenses]
    ADD CONSTRAINT [DF_WeeklyFleetExpenses_CreatedAt] DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_WeeklyFleetExpenses_MaintenanceExpenseTotal]...';


GO
ALTER TABLE [dbo].[WeeklyFleetExpenses]
    ADD CONSTRAINT [DF_WeeklyFleetExpenses_MaintenanceExpenseTotal] DEFAULT (0) FOR [MaintenanceExpenseTotal];


GO
PRINT N'Creating Default Constraint [dbo].[DF_WeeklyFleetExpenses_DriverName]...';


GO
ALTER TABLE [dbo].[WeeklyFleetExpenses]
    ADD CONSTRAINT [DF_WeeklyFleetExpenses_DriverName] DEFAULT (N'') FOR [DriverName];


GO
PRINT N'Creating Foreign Key [dbo].[FK_AuditLogs_Users]...';


GO
ALTER TABLE [dbo].[AuditLogs] WITH NOCHECK
    ADD CONSTRAINT [FK_AuditLogs_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE SET NULL;


GO
PRINT N'Creating Foreign Key [dbo].[FK_CharterBookings_Employees]...';


GO
ALTER TABLE [dbo].[CharterBookings] WITH NOCHECK
    ADD CONSTRAINT [FK_CharterBookings_Employees] FOREIGN KEY ([DriverEmployeeId]) REFERENCES [dbo].[Employees] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_CharterBookings_QuoteRequests]...';


GO
ALTER TABLE [dbo].[CharterBookings] WITH NOCHECK
    ADD CONSTRAINT [FK_CharterBookings_QuoteRequests] FOREIGN KEY ([QuoteRequestId]) REFERENCES [dbo].[CharterQuoteRequests] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_CharterBookings_Vehicles]...';


GO
ALTER TABLE [dbo].[CharterBookings] WITH NOCHECK
    ADD CONSTRAINT [FK_CharterBookings_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_CharterQuoteLegs_CharterQuoteRequests]...';


GO
ALTER TABLE [dbo].[CharterQuoteLegs] WITH NOCHECK
    ADD CONSTRAINT [FK_CharterQuoteLegs_CharterQuoteRequests] FOREIGN KEY ([CharterQuoteRequestId]) REFERENCES [dbo].[CharterQuoteRequests] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_CharterQuoteRequests_ApprovedBy]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests] WITH NOCHECK
    ADD CONSTRAINT [FK_CharterQuoteRequests_ApprovedBy] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [dbo].[Users] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_CharterQuoteRequests_Vehicles]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests] WITH NOCHECK
    ADD CONSTRAINT [FK_CharterQuoteRequests_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_CharterQuoteRequests_RequestedBy]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests] WITH NOCHECK
    ADD CONSTRAINT [FK_CharterQuoteRequests_RequestedBy] FOREIGN KEY ([RequestedByUserId]) REFERENCES [dbo].[Users] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_CustomerReturns_Orders]...';


GO
ALTER TABLE [dbo].[CustomerReturns] WITH NOCHECK
    ADD CONSTRAINT [FK_CustomerReturns_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_CustomerReturns_Recipes]...';


GO
ALTER TABLE [dbo].[CustomerReturns] WITH NOCHECK
    ADD CONSTRAINT [FK_CustomerReturns_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_CustomerReturns_Customers]...';


GO
ALTER TABLE [dbo].[CustomerReturns] WITH NOCHECK
    ADD CONSTRAINT [FK_CustomerReturns_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_CustomerReturns_SalesReps]...';


GO
ALTER TABLE [dbo].[CustomerReturns] WITH NOCHECK
    ADD CONSTRAINT [FK_CustomerReturns_SalesReps] FOREIGN KEY ([SalesRepId]) REFERENCES [dbo].[SalesReps] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_Employees_JobRoles]...';


GO
ALTER TABLE [dbo].[Employees] WITH NOCHECK
    ADD CONSTRAINT [FK_Employees_JobRoles] FOREIGN KEY ([JobRoleId]) REFERENCES [dbo].[JobRoles] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_Employees_Departments]...';


GO
ALTER TABLE [dbo].[Employees] WITH NOCHECK
    ADD CONSTRAINT [FK_Employees_Departments] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_FleetDispatches_Users]...';


GO
ALTER TABLE [dbo].[FleetDispatches] WITH NOCHECK
    ADD CONSTRAINT [FK_FleetDispatches_Users] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_FleetDispatches_Employees]...';


GO
ALTER TABLE [dbo].[FleetDispatches] WITH NOCHECK
    ADD CONSTRAINT [FK_FleetDispatches_Employees] FOREIGN KEY ([DriverEmployeeId]) REFERENCES [dbo].[Employees] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_FleetDispatches_Vehicles]...';


GO
ALTER TABLE [dbo].[FleetDispatches] WITH NOCHECK
    ADD CONSTRAINT [FK_FleetDispatches_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_FleetDispatchStops_FleetDispatches]...';


GO
ALTER TABLE [dbo].[FleetDispatchStops] WITH NOCHECK
    ADD CONSTRAINT [FK_FleetDispatchStops_FleetDispatches] FOREIGN KEY ([FleetDispatchId]) REFERENCES [dbo].[FleetDispatches] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_FuelLogs_Employees]...';


GO
ALTER TABLE [dbo].[FuelLogs] WITH NOCHECK
    ADD CONSTRAINT [FK_FuelLogs_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_FuelLogs_CharterBookings]...';


GO
ALTER TABLE [dbo].[FuelLogs] WITH NOCHECK
    ADD CONSTRAINT [FK_FuelLogs_CharterBookings] FOREIGN KEY ([CharterBookingId]) REFERENCES [dbo].[CharterBookings] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_FuelLogs_Vehicles]...';


GO
ALTER TABLE [dbo].[FuelLogs] WITH NOCHECK
    ADD CONSTRAINT [FK_FuelLogs_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_FuelLogs_FleetDispatches]...';


GO
ALTER TABLE [dbo].[FuelLogs] WITH NOCHECK
    ADD CONSTRAINT [FK_FuelLogs_FleetDispatches] FOREIGN KEY ([FleetDispatchId]) REFERENCES [dbo].[FleetDispatches] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_IngredientPriceChangeRequests_RequestedByUsers]...';


GO
ALTER TABLE [dbo].[IngredientPriceChangeRequests] WITH NOCHECK
    ADD CONSTRAINT [FK_IngredientPriceChangeRequests_RequestedByUsers] FOREIGN KEY ([RequestedByUserId]) REFERENCES [dbo].[Users] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_IngredientPriceChangeRequests_InventoryItems]...';


GO
ALTER TABLE [dbo].[IngredientPriceChangeRequests] WITH NOCHECK
    ADD CONSTRAINT [FK_IngredientPriceChangeRequests_InventoryItems] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[InventoryItems] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_IngredientPriceChangeRequests_ReviewedByUsers]...';


GO
ALTER TABLE [dbo].[IngredientPriceChangeRequests] WITH NOCHECK
    ADD CONSTRAINT [FK_IngredientPriceChangeRequests_ReviewedByUsers] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [dbo].[Users] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_InventoryItems_Suppliers]...';


GO
ALTER TABLE [dbo].[InventoryItems] WITH NOCHECK
    ADD CONSTRAINT [FK_InventoryItems_Suppliers] FOREIGN KEY ([PreferredSupplierId]) REFERENCES [dbo].[Suppliers] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_InventoryItems_MeasurementDensities]...';


GO
ALTER TABLE [dbo].[InventoryItems] WITH NOCHECK
    ADD CONSTRAINT [FK_InventoryItems_MeasurementDensities] FOREIGN KEY ([MeasurementDensityId]) REFERENCES [dbo].[MeasurementDensities] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_JobRoles_Departments]...';


GO
ALTER TABLE [dbo].[JobRoles] WITH NOCHECK
    ADD CONSTRAINT [FK_JobRoles_Departments] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_MaintenanceLogs_Vehicles]...';


GO
ALTER TABLE [dbo].[MaintenanceLogs] WITH NOCHECK
    ADD CONSTRAINT [FK_MaintenanceLogs_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_Notifications_RecipientUser]...';


GO
ALTER TABLE [dbo].[Notifications] WITH NOCHECK
    ADD CONSTRAINT [FK_Notifications_RecipientUser] FOREIGN KEY ([RecipientUserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_Notifications_CreatedByUser]...';


GO
ALTER TABLE [dbo].[Notifications] WITH NOCHECK
    ADD CONSTRAINT [FK_Notifications_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_OrderItems_Recipes]...';


GO
ALTER TABLE [dbo].[OrderItems] WITH NOCHECK
    ADD CONSTRAINT [FK_OrderItems_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_OrderItems_Orders]...';


GO
ALTER TABLE [dbo].[OrderItems] WITH NOCHECK
    ADD CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_OrderPayments_Orders]...';


GO
ALTER TABLE [dbo].[OrderPayments] WITH NOCHECK
    ADD CONSTRAINT [FK_OrderPayments_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_Orders_CreatedByUser]...';


GO
ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [FK_Orders_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_Orders_ProductRepositories]...';


GO
ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [FK_Orders_ProductRepositories] FOREIGN KEY ([ProductRepositoryId]) REFERENCES [dbo].[ProductRepositories] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_Orders_SalesReps]...';


GO
ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [FK_Orders_SalesReps] FOREIGN KEY ([SalesRepId]) REFERENCES [dbo].[SalesReps] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_Orders_Customers]...';


GO
ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [FK_Orders_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_Orders_PosTerminals]...';


GO
ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [FK_Orders_PosTerminals] FOREIGN KEY ([PosTerminalId]) REFERENCES [dbo].[PosTerminals] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_PayrollDeductionLines_SalaryAdvances]...';


GO
ALTER TABLE [dbo].[PayrollDeductionLines] WITH NOCHECK
    ADD CONSTRAINT [FK_PayrollDeductionLines_SalaryAdvances] FOREIGN KEY ([SalaryAdvanceId]) REFERENCES [dbo].[SalaryAdvances] ([Id]) ON DELETE SET NULL;


GO
PRINT N'Creating Foreign Key [dbo].[FK_PayrollDeductionLines_PayrollRecords]...';


GO
ALTER TABLE [dbo].[PayrollDeductionLines] WITH NOCHECK
    ADD CONSTRAINT [FK_PayrollDeductionLines_PayrollRecords] FOREIGN KEY ([PayrollRecordId]) REFERENCES [dbo].[PayrollRecords] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_PayrollDeductionLines_Employees]...';


GO
ALTER TABLE [dbo].[PayrollDeductionLines] WITH NOCHECK
    ADD CONSTRAINT [FK_PayrollDeductionLines_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_PayrollRecords_Employees]...';


GO
ALTER TABLE [dbo].[PayrollRecords] WITH NOCHECK
    ADD CONSTRAINT [FK_PayrollRecords_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_PayrollRecords_PayrollRuns]...';


GO
ALTER TABLE [dbo].[PayrollRecords] WITH NOCHECK
    ADD CONSTRAINT [FK_PayrollRecords_PayrollRuns] FOREIGN KEY ([PayrollRunId]) REFERENCES [dbo].[PayrollRuns] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_PosTerminals_Employees]...';


GO
ALTER TABLE [dbo].[PosTerminals] WITH NOCHECK
    ADD CONSTRAINT [FK_PosTerminals_Employees] FOREIGN KEY ([AssignedEmployeeId]) REFERENCES [dbo].[Employees] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_PosTerminals_Users]...';


GO
ALTER TABLE [dbo].[PosTerminals] WITH NOCHECK
    ADD CONSTRAINT [FK_PosTerminals_Users] FOREIGN KEY ([AssignedUserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE SET NULL;


GO
PRINT N'Creating Foreign Key [dbo].[FK_PosTerminals_ProductRepositories]...';


GO
ALTER TABLE [dbo].[PosTerminals] WITH NOCHECK
    ADD CONSTRAINT [FK_PosTerminals_ProductRepositories] FOREIGN KEY ([ProductRepositoryId]) REFERENCES [dbo].[ProductRepositories] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProductionBatches_Orders]...';


GO
ALTER TABLE [dbo].[ProductionBatches] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductionBatches_Orders] FOREIGN KEY ([LinkedOrderId]) REFERENCES [dbo].[Orders] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProductionBatches_ProductionProductTypes]...';


GO
ALTER TABLE [dbo].[ProductionBatches] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductionBatches_ProductionProductTypes] FOREIGN KEY ([ProductionProductTypeId]) REFERENCES [dbo].[ProductionProductTypes] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProductionBatches_Recipes]...';


GO
ALTER TABLE [dbo].[ProductionBatches] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductionBatches_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProductionBatchItems_ProductionBatches]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductionBatchItems_ProductionBatches] FOREIGN KEY ([ProductionBatchId]) REFERENCES [dbo].[ProductionBatches] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProductionBatchItems_Recipes]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductionBatchItems_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProductionDefects_Recipes]...';


GO
ALTER TABLE [dbo].[ProductionDefects] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductionDefects_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProductionDefects_ProductionBatches]...';


GO
ALTER TABLE [dbo].[ProductionDefects] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductionDefects_ProductionBatches] FOREIGN KEY ([ProductionBatchId]) REFERENCES [dbo].[ProductionBatches] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProductionProductTypes_ProductionCategories]...';


GO
ALTER TABLE [dbo].[ProductionProductTypes] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductionProductTypes_ProductionCategories] FOREIGN KEY ([ProductionCategoryId]) REFERENCES [dbo].[ProductionCategories] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProductRepositoryMovements_Recipes]...';


GO
ALTER TABLE [dbo].[ProductRepositoryMovements] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductRepositoryMovements_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProductRepositoryMovements_ProductRepositories]...';


GO
ALTER TABLE [dbo].[ProductRepositoryMovements] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductRepositoryMovements_ProductRepositories] FOREIGN KEY ([ProductRepositoryId]) REFERENCES [dbo].[ProductRepositories] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProductRepositoryStocks_ProductRepositories]...';


GO
ALTER TABLE [dbo].[ProductRepositoryStocks] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductRepositoryStocks_ProductRepositories] FOREIGN KEY ([ProductRepositoryId]) REFERENCES [dbo].[ProductRepositories] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProductRepositoryStocks_Recipes]...';


GO
ALTER TABLE [dbo].[ProductRepositoryStocks] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductRepositoryStocks_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_RbacRolePermissions_RbacPermissionLevels]...';


GO
ALTER TABLE [dbo].[RbacRolePermissions] WITH NOCHECK
    ADD CONSTRAINT [FK_RbacRolePermissions_RbacPermissionLevels] FOREIGN KEY ([PermissionLevelId]) REFERENCES [dbo].[RbacPermissionLevels] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_RbacRolePermissions_RbacModules]...';


GO
ALTER TABLE [dbo].[RbacRolePermissions] WITH NOCHECK
    ADD CONSTRAINT [FK_RbacRolePermissions_RbacModules] FOREIGN KEY ([ModuleId]) REFERENCES [dbo].[RbacModules] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_RbacRolePermissions_Roles]...';


GO
ALTER TABLE [dbo].[RbacRolePermissions] WITH NOCHECK
    ADD CONSTRAINT [FK_RbacRolePermissions_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_RecipeIngredients_Recipes]...';


GO
ALTER TABLE [dbo].[RecipeIngredients] WITH NOCHECK
    ADD CONSTRAINT [FK_RecipeIngredients_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_RecipeIngredients_InventoryItems]...';


GO
ALTER TABLE [dbo].[RecipeIngredients] WITH NOCHECK
    ADD CONSTRAINT [FK_RecipeIngredients_InventoryItems] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[InventoryItems] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_Recipes_ProductionProductTypes]...';


GO
ALTER TABLE [dbo].[Recipes] WITH NOCHECK
    ADD CONSTRAINT [FK_Recipes_ProductionProductTypes] FOREIGN KEY ([ProductionProductTypeId]) REFERENCES [dbo].[ProductionProductTypes] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_SalaryAdvances_Employees]...';


GO
ALTER TABLE [dbo].[SalaryAdvances] WITH NOCHECK
    ADD CONSTRAINT [FK_SalaryAdvances_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_SalesBonusRules_Recipes]...';


GO
ALTER TABLE [dbo].[SalesBonusRules] WITH NOCHECK
    ADD CONSTRAINT [FK_SalesBonusRules_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_SalesReconciliationItems_Recipes]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems] WITH NOCHECK
    ADD CONSTRAINT [FK_SalesReconciliationItems_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_SalesReconciliationItems_SalesReconciliations]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems] WITH NOCHECK
    ADD CONSTRAINT [FK_SalesReconciliationItems_SalesReconciliations] FOREIGN KEY ([SalesReconciliationId]) REFERENCES [dbo].[SalesReconciliations] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_SalesReconciliations_SalesReps]...';


GO
ALTER TABLE [dbo].[SalesReconciliations] WITH NOCHECK
    ADD CONSTRAINT [FK_SalesReconciliations_SalesReps] FOREIGN KEY ([SalesRepId]) REFERENCES [dbo].[SalesReps] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_SalesReconciliations_PosTerminals]...';


GO
ALTER TABLE [dbo].[SalesReconciliations] WITH NOCHECK
    ADD CONSTRAINT [FK_SalesReconciliations_PosTerminals] FOREIGN KEY ([PosTerminalId]) REFERENCES [dbo].[PosTerminals] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_SalesReconciliations_ProductRepositories]...';


GO
ALTER TABLE [dbo].[SalesReconciliations] WITH NOCHECK
    ADD CONSTRAINT [FK_SalesReconciliations_ProductRepositories] FOREIGN KEY ([ProductRepositoryId]) REFERENCES [dbo].[ProductRepositories] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_SalesReps_Users]...';


GO
ALTER TABLE [dbo].[SalesReps] WITH NOCHECK
    ADD CONSTRAINT [FK_SalesReps_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE SET NULL;


GO
PRINT N'Creating Foreign Key [dbo].[FK_SalesReps_Employees]...';


GO
ALTER TABLE [dbo].[SalesReps] WITH NOCHECK
    ADD CONSTRAINT [FK_SalesReps_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_SalesReps_Vehicles]...';


GO
ALTER TABLE [dbo].[SalesReps] WITH NOCHECK
    ADD CONSTRAINT [FK_SalesReps_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_SalesReps_PosTerminals]...';


GO
ALTER TABLE [dbo].[SalesReps] WITH NOCHECK
    ADD CONSTRAINT [FK_SalesReps_PosTerminals] FOREIGN KEY ([PosTerminalId]) REFERENCES [dbo].[PosTerminals] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_StockMovements_StoreRequests]...';


GO
ALTER TABLE [dbo].[StockMovements] WITH NOCHECK
    ADD CONSTRAINT [FK_StockMovements_StoreRequests] FOREIGN KEY ([StoreRequestId]) REFERENCES [dbo].[StoreRequests] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_StockMovements_InventoryItems]...';


GO
ALTER TABLE [dbo].[StockMovements] WITH NOCHECK
    ADD CONSTRAINT [FK_StockMovements_InventoryItems] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[InventoryItems] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_StockMovements_StoreRequestItems]...';


GO
ALTER TABLE [dbo].[StockMovements] WITH NOCHECK
    ADD CONSTRAINT [FK_StockMovements_StoreRequestItems] FOREIGN KEY ([StoreRequestItemId]) REFERENCES [dbo].[StoreRequestItems] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_StoreRequestItems_InventoryItems]...';


GO
ALTER TABLE [dbo].[StoreRequestItems] WITH NOCHECK
    ADD CONSTRAINT [FK_StoreRequestItems_InventoryItems] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[InventoryItems] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_StoreRequestItems_StoreRequests]...';


GO
ALTER TABLE [dbo].[StoreRequestItems] WITH NOCHECK
    ADD CONSTRAINT [FK_StoreRequestItems_StoreRequests] FOREIGN KEY ([StoreRequestId]) REFERENCES [dbo].[StoreRequests] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_SupermarketAccountPrices_Recipes]...';


GO
ALTER TABLE [dbo].[SupermarketAccountPrices] WITH NOCHECK
    ADD CONSTRAINT [FK_SupermarketAccountPrices_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_SupermarketAccountPrices_SupermarketAccounts]...';


GO
ALTER TABLE [dbo].[SupermarketAccountPrices] WITH NOCHECK
    ADD CONSTRAINT [FK_SupermarketAccountPrices_SupermarketAccounts] FOREIGN KEY ([SupermarketAccountId]) REFERENCES [dbo].[SupermarketAccounts] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_SupermarketAccounts_Customers]...';


GO
ALTER TABLE [dbo].[SupermarketAccounts] WITH NOCHECK
    ADD CONSTRAINT [FK_SupermarketAccounts_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_SupermarketAccounts_SalesReps]...';


GO
ALTER TABLE [dbo].[SupermarketAccounts] WITH NOCHECK
    ADD CONSTRAINT [FK_SupermarketAccounts_SalesReps] FOREIGN KEY ([AssignedRepId]) REFERENCES [dbo].[SalesReps] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_TripLegs_CharterBookings]...';


GO
ALTER TABLE [dbo].[TripLegs] WITH NOCHECK
    ADD CONSTRAINT [FK_TripLegs_CharterBookings] FOREIGN KEY ([CharterBookingId]) REFERENCES [dbo].[CharterBookings] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_Users_Employees]...';


GO
ALTER TABLE [dbo].[Users] WITH NOCHECK
    ADD CONSTRAINT [FK_Users_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_Users_Roles]...';


GO
ALTER TABLE [dbo].[Users] WITH NOCHECK
    ADD CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_Users_Customers]...';


GO
ALTER TABLE [dbo].[Users] WITH NOCHECK
    ADD CONSTRAINT [FK_Users_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_Vehicles_Employees]...';


GO
ALTER TABLE [dbo].[Vehicles] WITH NOCHECK
    ADD CONSTRAINT [FK_Vehicles_Employees] FOREIGN KEY ([AssignedEmployeeId]) REFERENCES [dbo].[Employees] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_WeeklyFleetExpenses_Vehicles]...';


GO
ALTER TABLE [dbo].[WeeklyFleetExpenses] WITH NOCHECK
    ADD CONSTRAINT [FK_WeeklyFleetExpenses_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]);


GO
PRINT N'Creating Check Constraint [dbo].[CK_CharterBookings_TripStatus]...';


GO
ALTER TABLE [dbo].[CharterBookings] WITH NOCHECK
    ADD CONSTRAINT [CK_CharterBookings_TripStatus] CHECK ([TripStatus] IN (0, 1, 2, 3));


GO
PRINT N'Creating Check Constraint [dbo].[CK_CharterBookings_Amounts]...';


GO
ALTER TABLE [dbo].[CharterBookings] WITH NOCHECK
    ADD CONSTRAINT [CK_CharterBookings_Amounts] CHECK ([DriverAllowance] >= 0 AND [SecurityEscortFee] >= 0 AND [TollsAndRoadLevies] >= 0 AND [GrossCharterFee] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_CharterQuoteLegs_LegNumber]...';


GO
ALTER TABLE [dbo].[CharterQuoteLegs] WITH NOCHECK
    ADD CONSTRAINT [CK_CharterQuoteLegs_LegNumber] CHECK ([LegNumber] > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_CharterQuoteLegs_FuelOption]...';


GO
ALTER TABLE [dbo].[CharterQuoteLegs] WITH NOCHECK
    ADD CONSTRAINT [CK_CharterQuoteLegs_FuelOption] CHECK ([FuelOption] IN (0, 1));


GO
PRINT N'Creating Check Constraint [dbo].[CK_CharterQuoteLegs_Amounts]...';


GO
ALTER TABLE [dbo].[CharterQuoteLegs] WITH NOCHECK
    ADD CONSTRAINT [CK_CharterQuoteLegs_Amounts] CHECK ([DistanceKm] >= 0 AND [QuotedLegAmount] > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_CharterQuoteRequests_Amounts]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests] WITH NOCHECK
    ADD CONSTRAINT [CK_CharterQuoteRequests_Amounts] CHECK ([TotalDistanceKm] >= 0 AND [QuotedAmount] >= 0 AND [DriverAllowance] >= 0 AND [SecurityEscortFee] >= 0 AND [TollsAndRoadLevies] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_CharterQuoteRequests_TripType]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests] WITH NOCHECK
    ADD CONSTRAINT [CK_CharterQuoteRequests_TripType] CHECK ([TripType] IN (0, 1, 2));


GO
PRINT N'Creating Check Constraint [dbo].[CK_CharterQuoteRequests_Status]...';


GO
ALTER TABLE [dbo].[CharterQuoteRequests] WITH NOCHECK
    ADD CONSTRAINT [CK_CharterQuoteRequests_Status] CHECK ([Status] IN (0, 1, 2, 3, 4, 5));


GO
PRINT N'Creating Check Constraint [dbo].[CK_CustomerReturns_ReplacementQuantity]...';


GO
ALTER TABLE [dbo].[CustomerReturns] WITH NOCHECK
    ADD CONSTRAINT [CK_CustomerReturns_ReplacementQuantity] CHECK ([ReplacementQuantity] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_CustomerReturns_Quantity]...';


GO
ALTER TABLE [dbo].[CustomerReturns] WITH NOCHECK
    ADD CONSTRAINT [CK_CustomerReturns_Quantity] CHECK ([Quantity] > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_CustomerReturns_UnitPrice]...';


GO
ALTER TABLE [dbo].[CustomerReturns] WITH NOCHECK
    ADD CONSTRAINT [CK_CustomerReturns_UnitPrice] CHECK ([UnitPrice] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_Customers_Category]...';


GO
ALTER TABLE [dbo].[Customers] WITH NOCHECK
    ADD CONSTRAINT [CK_Customers_Category] CHECK ([Category] IN (0, 1, 2, 3));


GO
PRINT N'Creating Check Constraint [dbo].[CK_Customers_OutstandingBalance]...';


GO
ALTER TABLE [dbo].[Customers] WITH NOCHECK
    ADD CONSTRAINT [CK_Customers_OutstandingBalance] CHECK ([OutstandingBalance] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_Employees_EmploymentStatus]...';


GO
ALTER TABLE [dbo].[Employees] WITH NOCHECK
    ADD CONSTRAINT [CK_Employees_EmploymentStatus] CHECK ([EmploymentStatus] IN (N'Active', N'OnLeave', N'Inactive'));


GO
PRINT N'Creating Check Constraint [dbo].[CK_Employees_BaseSalary]...';


GO
ALTER TABLE [dbo].[Employees] WITH NOCHECK
    ADD CONSTRAINT [CK_Employees_BaseSalary] CHECK ([BaseSalary] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_FleetDispatches_Status]...';


GO
ALTER TABLE [dbo].[FleetDispatches] WITH NOCHECK
    ADD CONSTRAINT [CK_FleetDispatches_Status] CHECK ([Status] IN (0, 1, 2, 3, 4, 5));


GO
PRINT N'Creating Check Constraint [dbo].[CK_FleetDispatches_Odometer]...';


GO
ALTER TABLE [dbo].[FleetDispatches] WITH NOCHECK
    ADD CONSTRAINT [CK_FleetDispatches_Odometer] CHECK ([OdometerEnd] IS NULL OR [OdometerEnd] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_FleetDispatchStops_Status]...';


GO
ALTER TABLE [dbo].[FleetDispatchStops] WITH NOCHECK
    ADD CONSTRAINT [CK_FleetDispatchStops_Status] CHECK ([Status] IN (0, 1, 2));


GO
PRINT N'Creating Check Constraint [dbo].[CK_FleetDispatchStops_StopNumber]...';


GO
ALTER TABLE [dbo].[FleetDispatchStops] WITH NOCHECK
    ADD CONSTRAINT [CK_FleetDispatchStops_StopNumber] CHECK ([StopNumber] > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_FuelLogs_Amounts]...';


GO
ALTER TABLE [dbo].[FuelLogs] WITH NOCHECK
    ADD CONSTRAINT [CK_FuelLogs_Amounts] CHECK ([LitresDispensed] > 0 AND [PricePerLitre] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_FuelLogs_OneAssignment]...';


GO
ALTER TABLE [dbo].[FuelLogs] WITH NOCHECK
    ADD CONSTRAINT [CK_FuelLogs_OneAssignment] CHECK ([CharterBookingId] IS NULL OR [FleetDispatchId] IS NULL);


GO
PRINT N'Creating Check Constraint [dbo].[CK_FuelLogs_FuelType]...';


GO
ALTER TABLE [dbo].[FuelLogs] WITH NOCHECK
    ADD CONSTRAINT [CK_FuelLogs_FuelType] CHECK ([FuelType] IN (N'Petrol', N'Diesel', N'EV Charging'));


GO
PRINT N'Creating Check Constraint [dbo].[CK_IngredientPriceChangeRequests_Costs]...';


GO
ALTER TABLE [dbo].[IngredientPriceChangeRequests] WITH NOCHECK
    ADD CONSTRAINT [CK_IngredientPriceChangeRequests_Costs] CHECK ([PreviousUnitCost] >= 0 AND [RequestedUnitCost] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_IngredientPriceChangeRequests_Status]...';


GO
ALTER TABLE [dbo].[IngredientPriceChangeRequests] WITH NOCHECK
    ADD CONSTRAINT [CK_IngredientPriceChangeRequests_Status] CHECK ([Status] IN (0, 1, 2));


GO
PRINT N'Creating Check Constraint [dbo].[CK_InventoryItems_Quantities]...';


GO
ALTER TABLE [dbo].[InventoryItems] WITH NOCHECK
    ADD CONSTRAINT [CK_InventoryItems_Quantities] CHECK ([QuantityInStock] >= 0 AND [ReorderThreshold] >= 0 AND [UnitCostPrice] >= 0 AND [PackageSize] > 0 AND ([DensityKgPerLitre] IS NULL OR [DensityKgPerLitre] > 0));


GO
PRINT N'Creating Check Constraint [dbo].[CK_MaintenanceLogs_Cost]...';


GO
ALTER TABLE [dbo].[MaintenanceLogs] WITH NOCHECK
    ADD CONSTRAINT [CK_MaintenanceLogs_Cost] CHECK ([Cost] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_MeasurementDensities_Density]...';


GO
ALTER TABLE [dbo].[MeasurementDensities] WITH NOCHECK
    ADD CONSTRAINT [CK_MeasurementDensities_Density] CHECK ([DensityKgPerLitre] > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_Notifications_Type]...';


GO
ALTER TABLE [dbo].[Notifications] WITH NOCHECK
    ADD CONSTRAINT [CK_Notifications_Type] CHECK ([Type] IN (0, 1, 2, 3));


GO
PRINT N'Creating Check Constraint [dbo].[CK_OrderItems_BonusQuantity]...';


GO
ALTER TABLE [dbo].[OrderItems] WITH NOCHECK
    ADD CONSTRAINT [CK_OrderItems_BonusQuantity] CHECK ([BonusQuantity] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_OrderItems_Quantity]...';


GO
ALTER TABLE [dbo].[OrderItems] WITH NOCHECK
    ADD CONSTRAINT [CK_OrderItems_Quantity] CHECK ([Quantity] > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_OrderItems_UnitPrice]...';


GO
ALTER TABLE [dbo].[OrderItems] WITH NOCHECK
    ADD CONSTRAINT [CK_OrderItems_UnitPrice] CHECK ([UnitPrice] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_OrderPayments_Amount]...';


GO
ALTER TABLE [dbo].[OrderPayments] WITH NOCHECK
    ADD CONSTRAINT [CK_OrderPayments_Amount] CHECK ([Amount] > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_OrderPayments_Method]...';


GO
ALTER TABLE [dbo].[OrderPayments] WITH NOCHECK
    ADD CONSTRAINT [CK_OrderPayments_Method] CHECK ([PaymentMethod] IN (0, 1, 2, 3));


GO
PRINT N'Creating Check Constraint [dbo].[CK_Orders_Amounts]...';


GO
ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [CK_Orders_Amounts] CHECK ([SubTotal] >= 0 AND [Discount] >= 0 AND [Discount] <= [SubTotal] AND [AmountPaid] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_Orders_CustomerCategory]...';


GO
ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [CK_Orders_CustomerCategory] CHECK ([CustomerCategory] IN (0, 1, 2, 3));


GO
PRINT N'Creating Check Constraint [dbo].[CK_Orders_BookedByType]...';


GO
ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [CK_Orders_BookedByType] CHECK ([BookedByType] IN (0, 1, 2));


GO
PRINT N'Creating Check Constraint [dbo].[CK_Orders_PaymentMethod]...';


GO
ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [CK_Orders_PaymentMethod] CHECK ([PaymentMethod] IN (0, 1, 2, 3));


GO
PRINT N'Creating Check Constraint [dbo].[CK_Orders_OrderType]...';


GO
ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [CK_Orders_OrderType] CHECK ([OrderType] IN (0, 1));


GO
PRINT N'Creating Check Constraint [dbo].[CK_Orders_PaymentStatus]...';


GO
ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [CK_Orders_PaymentStatus] CHECK ([PaymentStatus] IN (0, 1, 2));


GO
PRINT N'Creating Check Constraint [dbo].[CK_Orders_Status]...';


GO
ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [CK_Orders_Status] CHECK ([Status] IN (0, 1, 2, 3, 4, 5, 6));


GO
PRINT N'Creating Check Constraint [dbo].[CK_PayrollDeductionLines_Amount]...';


GO
ALTER TABLE [dbo].[PayrollDeductionLines] WITH NOCHECK
    ADD CONSTRAINT [CK_PayrollDeductionLines_Amount] CHECK ([Amount] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_PayrollRecords_Year]...';


GO
ALTER TABLE [dbo].[PayrollRecords] WITH NOCHECK
    ADD CONSTRAINT [CK_PayrollRecords_Year] CHECK ([PayrollYear] >= 2000);


GO
PRINT N'Creating Check Constraint [dbo].[CK_PayrollRecords_PaymentStatus]...';


GO
ALTER TABLE [dbo].[PayrollRecords] WITH NOCHECK
    ADD CONSTRAINT [CK_PayrollRecords_PaymentStatus] CHECK ([PaymentStatus] IN (N'Pending', N'Paid', N'OnHold'));


GO
PRINT N'Creating Check Constraint [dbo].[CK_PayrollRecords_Amounts]...';


GO
ALTER TABLE [dbo].[PayrollRecords] WITH NOCHECK
    ADD CONSTRAINT [CK_PayrollRecords_Amounts] CHECK ([BaseSalary] >= 0 AND [PerformanceBonus] >= 0 AND
        [HousingAllowance] >= 0 AND [TransportAllowance] >= 0 AND [OtherAllowances] >= 0 AND
        [DisciplinaryDeductions] >= 0 AND [AdvanceDeduction] >= 0 AND [TaxAndLevies] >= 0 AND
        [PensionDeduction] >= 0 AND [HealthInsuranceDeduction] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_PayrollRecords_Month]...';


GO
ALTER TABLE [dbo].[PayrollRecords] WITH NOCHECK
    ADD CONSTRAINT [CK_PayrollRecords_Month] CHECK ([PayrollMonth] BETWEEN 1 AND 12);


GO
PRINT N'Creating Check Constraint [dbo].[CK_PayrollRuns_Amounts]...';


GO
ALTER TABLE [dbo].[PayrollRuns] WITH NOCHECK
    ADD CONSTRAINT [CK_PayrollRuns_Amounts] CHECK ([GrossTotal] >= 0 AND [DeductionsTotal] >= 0 AND [NetTotal] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_PayrollRuns_Year]...';


GO
ALTER TABLE [dbo].[PayrollRuns] WITH NOCHECK
    ADD CONSTRAINT [CK_PayrollRuns_Year] CHECK ([PayrollYear] >= 2000);


GO
PRINT N'Creating Check Constraint [dbo].[CK_PayrollRuns_Status]...';


GO
ALTER TABLE [dbo].[PayrollRuns] WITH NOCHECK
    ADD CONSTRAINT [CK_PayrollRuns_Status] CHECK ([Status] IN (N'Pending', N'PartiallyPaid', N'Paid'));


GO
PRINT N'Creating Check Constraint [dbo].[CK_PayrollRuns_Month]...';


GO
ALTER TABLE [dbo].[PayrollRuns] WITH NOCHECK
    ADD CONSTRAINT [CK_PayrollRuns_Month] CHECK ([PayrollMonth] BETWEEN 1 AND 12);


GO
PRINT N'Creating Check Constraint [dbo].[CK_PayrollSettings_SingleRow]...';


GO
ALTER TABLE [dbo].[PayrollSettings] WITH NOCHECK
    ADD CONSTRAINT [CK_PayrollSettings_SingleRow] CHECK ([Id] = 1);


GO
PRINT N'Creating Check Constraint [dbo].[CK_PayrollSettings_Percentages]...';


GO
ALTER TABLE [dbo].[PayrollSettings] WITH NOCHECK
    ADD CONSTRAINT [CK_PayrollSettings_Percentages] CHECK ([LoanMonthlyRepaymentPercent] BETWEEN 0 AND 100 AND
        [MaxLoanPercentOfSalary] BETWEEN 0 AND 100 AND
        [HousingAllowancePercent] BETWEEN 0 AND 100 AND
        [TransportAllowancePercent] BETWEEN 0 AND 100 AND
        [PayeTaxPercent] BETWEEN 0 AND 100 AND
        [PensionPercent] BETWEEN 0 AND 100 AND
        [HealthInsurancePercent] BETWEEN 0 AND 100);


GO
PRINT N'Creating Check Constraint [dbo].[CK_ProductionBatches_Costs]...';


GO
ALTER TABLE [dbo].[ProductionBatches] WITH NOCHECK
    ADD CONSTRAINT [CK_ProductionBatches_Costs] CHECK ([TotalBatchCost] >= 0 AND [CalculatedUnitCost] >= 0 AND [ExpectedUnitCost] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_ProductionBatches_Quantities]...';


GO
ALTER TABLE [dbo].[ProductionBatches] WITH NOCHECK
    ADD CONSTRAINT [CK_ProductionBatches_Quantities] CHECK ([TargetQuantity] >= 0 AND [ActualQuantity] >= 0 AND [FlourBagsUsed] >= 0 AND [BatchInputQuantity] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_ProductionBatches_Status]...';


GO
ALTER TABLE [dbo].[ProductionBatches] WITH NOCHECK
    ADD CONSTRAINT [CK_ProductionBatches_Status] CHECK ([Status] IN (0, 1, 2, 3, 4));


GO
PRINT N'Creating Check Constraint [dbo].[CK_ProductionBatchItems_Costs]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems] WITH NOCHECK
    ADD CONSTRAINT [CK_ProductionBatchItems_Costs] CHECK ([TotalBatchCost] >= 0 AND [ExpectedUnitCost] >= 0 AND [CalculatedUnitCost] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_ProductionBatchItems_Quantities]...';


GO
ALTER TABLE [dbo].[ProductionBatchItems] WITH NOCHECK
    ADD CONSTRAINT [CK_ProductionBatchItems_Quantities] CHECK ([TargetQuantity] >= 0 AND [ActualQuantity] >= 0 AND [BatchInputQuantity] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_ProductionDefects_Quantity]...';


GO
ALTER TABLE [dbo].[ProductionDefects] WITH NOCHECK
    ADD CONSTRAINT [CK_ProductionDefects_Quantity] CHECK ([Quantity] > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_ProductionProductTypes_Text]...';


GO
ALTER TABLE [dbo].[ProductionProductTypes] WITH NOCHECK
    ADD CONSTRAINT [CK_ProductionProductTypes_Text] CHECK (LEN([Baseline]) > 0 AND LEN([OutputUnitNoun]) > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_ProductRepositories_DefaultIsFactory]...';


GO
ALTER TABLE [dbo].[ProductRepositories] WITH NOCHECK
    ADD CONSTRAINT [CK_ProductRepositories_DefaultIsFactory] CHECK ([IsDefaultProductionOutput] = 0 OR [LocationType] = 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_ProductRepositories_LocationType]...';


GO
ALTER TABLE [dbo].[ProductRepositories] WITH NOCHECK
    ADD CONSTRAINT [CK_ProductRepositories_LocationType] CHECK ([LocationType] IN (0, 1));


GO
PRINT N'Creating Check Constraint [dbo].[CK_ProductRepositoryMovements_Type]...';


GO
ALTER TABLE [dbo].[ProductRepositoryMovements] WITH NOCHECK
    ADD CONSTRAINT [CK_ProductRepositoryMovements_Type] CHECK ([MovementType] IN (0, 1, 2, 3, 4, 5, 6, 7, 8));


GO
PRINT N'Creating Check Constraint [dbo].[CK_ProductRepositoryMovements_Quantities]...';


GO
ALTER TABLE [dbo].[ProductRepositoryMovements] WITH NOCHECK
    ADD CONSTRAINT [CK_ProductRepositoryMovements_Quantities] CHECK ([Quantity] > 0 AND [BalanceAfter] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_ProductRepositoryStocks_Quantities]...';


GO
ALTER TABLE [dbo].[ProductRepositoryStocks] WITH NOCHECK
    ADD CONSTRAINT [CK_ProductRepositoryStocks_Quantities] CHECK ([QuantityOnHand] >= 0 AND [ReorderThreshold] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_RecipeIngredients_Quantities]...';


GO
ALTER TABLE [dbo].[RecipeIngredients] WITH NOCHECK
    ADD CONSTRAINT [CK_RecipeIngredients_Quantities] CHECK ([QuantityPerBatch] >= 0 AND [BaselineYield] > 0 AND [IngredientUnitCost] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_Recipes_Amounts]...';


GO
ALTER TABLE [dbo].[Recipes] WITH NOCHECK
    ADD CONSTRAINT [CK_Recipes_Amounts] CHECK ([ExpectedYield] > 0 AND [SellingPrice] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_SalaryAdvances_Status]...';


GO
ALTER TABLE [dbo].[SalaryAdvances] WITH NOCHECK
    ADD CONSTRAINT [CK_SalaryAdvances_Status] CHECK ([Status] IN (N'Active', N'FullyRepaid', N'Cancelled'));


GO
PRINT N'Creating Check Constraint [dbo].[CK_SalaryAdvances_Amounts]...';


GO
ALTER TABLE [dbo].[SalaryAdvances] WITH NOCHECK
    ADD CONSTRAINT [CK_SalaryAdvances_Amounts] CHECK ([PrincipalAmount] >= 0 AND [TotalRepaid] >= 0 AND [MonthlyDeductionAmount] > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_SalesBonusRules_Quantities]...';


GO
ALTER TABLE [dbo].[SalesBonusRules] WITH NOCHECK
    ADD CONSTRAINT [CK_SalesBonusRules_Quantities] CHECK ([ThresholdQuantity] > 0 AND [BonusQuantity] > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_SalesReconciliationItems_Quantities]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems] WITH NOCHECK
    ADD CONSTRAINT [CK_SalesReconciliationItems_Quantities] CHECK ([OpeningStock] >= 0 AND [QtySold] >= 0 AND [QtyDamaged] >= 0 AND [QtyReplacement] >= 0 AND [OpeningStock] >= ([QtySold] + [QtyDamaged] + [QtyReplacement]));


GO
PRINT N'Creating Check Constraint [dbo].[CK_SalesReconciliationItems_UnitPrice]...';


GO
ALTER TABLE [dbo].[SalesReconciliationItems] WITH NOCHECK
    ADD CONSTRAINT [CK_SalesReconciliationItems_UnitPrice] CHECK ([UnitPrice] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_SalesReconciliations_Amounts]...';


GO
ALTER TABLE [dbo].[SalesReconciliations] WITH NOCHECK
    ADD CONSTRAINT [CK_SalesReconciliations_Amounts] CHECK ([CashHandedOver] >= 0 AND [PosTerminalReceipts] >= 0 AND [TotalExpectedRevenue] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_SalesReconciliations_Status]...';


GO
ALTER TABLE [dbo].[SalesReconciliations] WITH NOCHECK
    ADD CONSTRAINT [CK_SalesReconciliations_Status] CHECK ([Status] IN (0, 1));


GO
PRINT N'Creating Check Constraint [dbo].[CK_StockMovements_Amounts]...';


GO
ALTER TABLE [dbo].[StockMovements] WITH NOCHECK
    ADD CONSTRAINT [CK_StockMovements_Amounts] CHECK ([Quantity] > 0 AND [BalanceAfter] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_StockMovements_Type]...';


GO
ALTER TABLE [dbo].[StockMovements] WITH NOCHECK
    ADD CONSTRAINT [CK_StockMovements_Type] CHECK ([MovementType] IN (N'Receipt', N'Issue'));


GO
PRINT N'Creating Check Constraint [dbo].[CK_StoreRequestItems_Quantities]...';


GO
ALTER TABLE [dbo].[StoreRequestItems] WITH NOCHECK
    ADD CONSTRAINT [CK_StoreRequestItems_Quantities] CHECK ([RequestedQuantity] >= 0 AND [BaseQuantity] >= 0 AND [StockQuantity] >= 0 AND [IssuedQuantity] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_StoreRequests_Status]...';


GO
ALTER TABLE [dbo].[StoreRequests] WITH NOCHECK
    ADD CONSTRAINT [CK_StoreRequests_Status] CHECK ([Status] IN (0, 1, 2, 3));


GO
PRINT N'Creating Check Constraint [dbo].[CK_SupermarketAccountPrices_AgreedPrice]...';


GO
ALTER TABLE [dbo].[SupermarketAccountPrices] WITH NOCHECK
    ADD CONSTRAINT [CK_SupermarketAccountPrices_AgreedPrice] CHECK ([AgreedPrice] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_SupermarketAccounts_Credit]...';


GO
ALTER TABLE [dbo].[SupermarketAccounts] WITH NOCHECK
    ADD CONSTRAINT [CK_SupermarketAccounts_Credit] CHECK ([CurrentOutstandingCredit] >= 0 AND [CreditLimit] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_TreasuryPolicySettings_SingleRow]...';


GO
ALTER TABLE [dbo].[TreasuryPolicySettings] WITH NOCHECK
    ADD CONSTRAINT [CK_TreasuryPolicySettings_SingleRow] CHECK ([Id] = 1);


GO
PRINT N'Creating Check Constraint [dbo].[CK_TreasuryPolicySettings_Percentages]...';


GO
ALTER TABLE [dbo].[TreasuryPolicySettings] WITH NOCHECK
    ADD CONSTRAINT [CK_TreasuryPolicySettings_Percentages] CHECK ([PlannedCashPercent] BETWEEN 0 AND 100 AND [AlertTolerancePercent] BETWEEN 0 AND 100);


GO
PRINT N'Creating Check Constraint [dbo].[CK_TripLegs_LegNumber]...';


GO
ALTER TABLE [dbo].[TripLegs] WITH NOCHECK
    ADD CONSTRAINT [CK_TripLegs_LegNumber] CHECK ([LegNumber] > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_TripLegs_FuelOption]...';


GO
ALTER TABLE [dbo].[TripLegs] WITH NOCHECK
    ADD CONSTRAINT [CK_TripLegs_FuelOption] CHECK ([FuelOption] IN (0, 1));


GO
PRINT N'Creating Check Constraint [dbo].[CK_TripLegs_LegCost]...';


GO
ALTER TABLE [dbo].[TripLegs] WITH NOCHECK
    ADD CONSTRAINT [CK_TripLegs_LegCost] CHECK ([LegCost] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_Vehicles_Status]...';


GO
ALTER TABLE [dbo].[Vehicles] WITH NOCHECK
    ADD CONSTRAINT [CK_Vehicles_Status] CHECK ([Status] IN (N'Active', N'InMaintenance', N'Grounded', N'InTransit'));


GO
PRINT N'Creating Check Constraint [dbo].[CK_Vehicles_Odometer]...';


GO
ALTER TABLE [dbo].[Vehicles] WITH NOCHECK
    ADD CONSTRAINT [CK_Vehicles_Odometer] CHECK ([CurrentOdometerKm] >= 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_Vehicles_TankCapacity]...';


GO
ALTER TABLE [dbo].[Vehicles] WITH NOCHECK
    ADD CONSTRAINT [CK_Vehicles_TankCapacity] CHECK ([TankCapacityLitres] > 0);


GO
PRINT N'Creating Check Constraint [dbo].[CK_WeeklyFleetExpenses_Dates]...';


GO
ALTER TABLE [dbo].[WeeklyFleetExpenses] WITH NOCHECK
    ADD CONSTRAINT [CK_WeeklyFleetExpenses_Dates] CHECK ([OperatingWeekEnd] >= [OperatingWeekStart]);


GO
PRINT N'Creating Check Constraint [dbo].[CK_WeeklyFleetExpenses_Amounts]...';


GO
ALTER TABLE [dbo].[WeeklyFleetExpenses] WITH NOCHECK
    ADD CONSTRAINT [CK_WeeklyFleetExpenses_Amounts] CHECK ([FuelExpenseTotal] >= 0 AND [MaintenanceExpenseTotal] >= 0 AND [IncidentalsTotal] >= 0);


GO
PRINT N'Checking existing data against newly created constraints';


GO
ALTER TABLE [dbo].[AuditLogs] WITH CHECK CHECK CONSTRAINT [FK_AuditLogs_Users];

ALTER TABLE [dbo].[CharterBookings] WITH CHECK CHECK CONSTRAINT [FK_CharterBookings_Employees];

ALTER TABLE [dbo].[CharterBookings] WITH CHECK CHECK CONSTRAINT [FK_CharterBookings_QuoteRequests];

ALTER TABLE [dbo].[CharterBookings] WITH CHECK CHECK CONSTRAINT [FK_CharterBookings_Vehicles];

ALTER TABLE [dbo].[CharterQuoteLegs] WITH CHECK CHECK CONSTRAINT [FK_CharterQuoteLegs_CharterQuoteRequests];

ALTER TABLE [dbo].[CharterQuoteRequests] WITH CHECK CHECK CONSTRAINT [FK_CharterQuoteRequests_ApprovedBy];

ALTER TABLE [dbo].[CharterQuoteRequests] WITH CHECK CHECK CONSTRAINT [FK_CharterQuoteRequests_Vehicles];

ALTER TABLE [dbo].[CharterQuoteRequests] WITH CHECK CHECK CONSTRAINT [FK_CharterQuoteRequests_RequestedBy];

ALTER TABLE [dbo].[CustomerReturns] WITH CHECK CHECK CONSTRAINT [FK_CustomerReturns_Orders];

ALTER TABLE [dbo].[CustomerReturns] WITH CHECK CHECK CONSTRAINT [FK_CustomerReturns_Recipes];

ALTER TABLE [dbo].[CustomerReturns] WITH CHECK CHECK CONSTRAINT [FK_CustomerReturns_Customers];

ALTER TABLE [dbo].[CustomerReturns] WITH CHECK CHECK CONSTRAINT [FK_CustomerReturns_SalesReps];

ALTER TABLE [dbo].[Employees] WITH CHECK CHECK CONSTRAINT [FK_Employees_JobRoles];

ALTER TABLE [dbo].[Employees] WITH CHECK CHECK CONSTRAINT [FK_Employees_Departments];

ALTER TABLE [dbo].[FleetDispatches] WITH CHECK CHECK CONSTRAINT [FK_FleetDispatches_Users];

ALTER TABLE [dbo].[FleetDispatches] WITH CHECK CHECK CONSTRAINT [FK_FleetDispatches_Employees];

ALTER TABLE [dbo].[FleetDispatches] WITH CHECK CHECK CONSTRAINT [FK_FleetDispatches_Vehicles];

ALTER TABLE [dbo].[FleetDispatchStops] WITH CHECK CHECK CONSTRAINT [FK_FleetDispatchStops_FleetDispatches];

ALTER TABLE [dbo].[FuelLogs] WITH CHECK CHECK CONSTRAINT [FK_FuelLogs_Employees];

ALTER TABLE [dbo].[FuelLogs] WITH CHECK CHECK CONSTRAINT [FK_FuelLogs_CharterBookings];

ALTER TABLE [dbo].[FuelLogs] WITH CHECK CHECK CONSTRAINT [FK_FuelLogs_Vehicles];

ALTER TABLE [dbo].[FuelLogs] WITH CHECK CHECK CONSTRAINT [FK_FuelLogs_FleetDispatches];

ALTER TABLE [dbo].[IngredientPriceChangeRequests] WITH CHECK CHECK CONSTRAINT [FK_IngredientPriceChangeRequests_RequestedByUsers];

ALTER TABLE [dbo].[IngredientPriceChangeRequests] WITH CHECK CHECK CONSTRAINT [FK_IngredientPriceChangeRequests_InventoryItems];

ALTER TABLE [dbo].[IngredientPriceChangeRequests] WITH CHECK CHECK CONSTRAINT [FK_IngredientPriceChangeRequests_ReviewedByUsers];

ALTER TABLE [dbo].[InventoryItems] WITH CHECK CHECK CONSTRAINT [FK_InventoryItems_Suppliers];

ALTER TABLE [dbo].[InventoryItems] WITH CHECK CHECK CONSTRAINT [FK_InventoryItems_MeasurementDensities];

ALTER TABLE [dbo].[JobRoles] WITH CHECK CHECK CONSTRAINT [FK_JobRoles_Departments];

ALTER TABLE [dbo].[MaintenanceLogs] WITH CHECK CHECK CONSTRAINT [FK_MaintenanceLogs_Vehicles];

ALTER TABLE [dbo].[Notifications] WITH CHECK CHECK CONSTRAINT [FK_Notifications_RecipientUser];

ALTER TABLE [dbo].[Notifications] WITH CHECK CHECK CONSTRAINT [FK_Notifications_CreatedByUser];

ALTER TABLE [dbo].[OrderItems] WITH CHECK CHECK CONSTRAINT [FK_OrderItems_Recipes];

ALTER TABLE [dbo].[OrderItems] WITH CHECK CHECK CONSTRAINT [FK_OrderItems_Orders];

ALTER TABLE [dbo].[OrderPayments] WITH CHECK CHECK CONSTRAINT [FK_OrderPayments_Orders];

ALTER TABLE [dbo].[Orders] WITH CHECK CHECK CONSTRAINT [FK_Orders_CreatedByUser];

ALTER TABLE [dbo].[Orders] WITH CHECK CHECK CONSTRAINT [FK_Orders_ProductRepositories];

ALTER TABLE [dbo].[Orders] WITH CHECK CHECK CONSTRAINT [FK_Orders_SalesReps];

ALTER TABLE [dbo].[Orders] WITH CHECK CHECK CONSTRAINT [FK_Orders_Customers];

ALTER TABLE [dbo].[Orders] WITH CHECK CHECK CONSTRAINT [FK_Orders_PosTerminals];

ALTER TABLE [dbo].[PayrollDeductionLines] WITH CHECK CHECK CONSTRAINT [FK_PayrollDeductionLines_SalaryAdvances];

ALTER TABLE [dbo].[PayrollDeductionLines] WITH CHECK CHECK CONSTRAINT [FK_PayrollDeductionLines_PayrollRecords];

ALTER TABLE [dbo].[PayrollDeductionLines] WITH CHECK CHECK CONSTRAINT [FK_PayrollDeductionLines_Employees];

ALTER TABLE [dbo].[PayrollRecords] WITH CHECK CHECK CONSTRAINT [FK_PayrollRecords_Employees];

ALTER TABLE [dbo].[PayrollRecords] WITH CHECK CHECK CONSTRAINT [FK_PayrollRecords_PayrollRuns];

ALTER TABLE [dbo].[PosTerminals] WITH CHECK CHECK CONSTRAINT [FK_PosTerminals_Employees];

ALTER TABLE [dbo].[PosTerminals] WITH CHECK CHECK CONSTRAINT [FK_PosTerminals_Users];

ALTER TABLE [dbo].[PosTerminals] WITH CHECK CHECK CONSTRAINT [FK_PosTerminals_ProductRepositories];

ALTER TABLE [dbo].[ProductionBatches] WITH CHECK CHECK CONSTRAINT [FK_ProductionBatches_Orders];

ALTER TABLE [dbo].[ProductionBatches] WITH CHECK CHECK CONSTRAINT [FK_ProductionBatches_ProductionProductTypes];

ALTER TABLE [dbo].[ProductionBatches] WITH CHECK CHECK CONSTRAINT [FK_ProductionBatches_Recipes];

ALTER TABLE [dbo].[ProductionBatchItems] WITH CHECK CHECK CONSTRAINT [FK_ProductionBatchItems_ProductionBatches];

ALTER TABLE [dbo].[ProductionBatchItems] WITH CHECK CHECK CONSTRAINT [FK_ProductionBatchItems_Recipes];

ALTER TABLE [dbo].[ProductionDefects] WITH CHECK CHECK CONSTRAINT [FK_ProductionDefects_Recipes];

ALTER TABLE [dbo].[ProductionDefects] WITH CHECK CHECK CONSTRAINT [FK_ProductionDefects_ProductionBatches];

ALTER TABLE [dbo].[ProductionProductTypes] WITH CHECK CHECK CONSTRAINT [FK_ProductionProductTypes_ProductionCategories];

ALTER TABLE [dbo].[ProductRepositoryMovements] WITH CHECK CHECK CONSTRAINT [FK_ProductRepositoryMovements_Recipes];

ALTER TABLE [dbo].[ProductRepositoryMovements] WITH CHECK CHECK CONSTRAINT [FK_ProductRepositoryMovements_ProductRepositories];

ALTER TABLE [dbo].[ProductRepositoryStocks] WITH CHECK CHECK CONSTRAINT [FK_ProductRepositoryStocks_ProductRepositories];

ALTER TABLE [dbo].[ProductRepositoryStocks] WITH CHECK CHECK CONSTRAINT [FK_ProductRepositoryStocks_Recipes];

ALTER TABLE [dbo].[RbacRolePermissions] WITH CHECK CHECK CONSTRAINT [FK_RbacRolePermissions_RbacPermissionLevels];

ALTER TABLE [dbo].[RbacRolePermissions] WITH CHECK CHECK CONSTRAINT [FK_RbacRolePermissions_RbacModules];

ALTER TABLE [dbo].[RbacRolePermissions] WITH CHECK CHECK CONSTRAINT [FK_RbacRolePermissions_Roles];

ALTER TABLE [dbo].[RecipeIngredients] WITH CHECK CHECK CONSTRAINT [FK_RecipeIngredients_Recipes];

ALTER TABLE [dbo].[RecipeIngredients] WITH CHECK CHECK CONSTRAINT [FK_RecipeIngredients_InventoryItems];

ALTER TABLE [dbo].[Recipes] WITH CHECK CHECK CONSTRAINT [FK_Recipes_ProductionProductTypes];

ALTER TABLE [dbo].[SalaryAdvances] WITH CHECK CHECK CONSTRAINT [FK_SalaryAdvances_Employees];

ALTER TABLE [dbo].[SalesBonusRules] WITH CHECK CHECK CONSTRAINT [FK_SalesBonusRules_Recipes];

ALTER TABLE [dbo].[SalesReconciliationItems] WITH CHECK CHECK CONSTRAINT [FK_SalesReconciliationItems_Recipes];

ALTER TABLE [dbo].[SalesReconciliationItems] WITH CHECK CHECK CONSTRAINT [FK_SalesReconciliationItems_SalesReconciliations];

ALTER TABLE [dbo].[SalesReconciliations] WITH CHECK CHECK CONSTRAINT [FK_SalesReconciliations_SalesReps];

ALTER TABLE [dbo].[SalesReconciliations] WITH CHECK CHECK CONSTRAINT [FK_SalesReconciliations_PosTerminals];

ALTER TABLE [dbo].[SalesReconciliations] WITH CHECK CHECK CONSTRAINT [FK_SalesReconciliations_ProductRepositories];

ALTER TABLE [dbo].[SalesReps] WITH CHECK CHECK CONSTRAINT [FK_SalesReps_Users];

ALTER TABLE [dbo].[SalesReps] WITH CHECK CHECK CONSTRAINT [FK_SalesReps_Employees];

ALTER TABLE [dbo].[SalesReps] WITH CHECK CHECK CONSTRAINT [FK_SalesReps_Vehicles];

ALTER TABLE [dbo].[SalesReps] WITH CHECK CHECK CONSTRAINT [FK_SalesReps_PosTerminals];

ALTER TABLE [dbo].[StockMovements] WITH CHECK CHECK CONSTRAINT [FK_StockMovements_StoreRequests];

ALTER TABLE [dbo].[StockMovements] WITH CHECK CHECK CONSTRAINT [FK_StockMovements_InventoryItems];

ALTER TABLE [dbo].[StockMovements] WITH CHECK CHECK CONSTRAINT [FK_StockMovements_StoreRequestItems];

ALTER TABLE [dbo].[StoreRequestItems] WITH CHECK CHECK CONSTRAINT [FK_StoreRequestItems_InventoryItems];

ALTER TABLE [dbo].[StoreRequestItems] WITH CHECK CHECK CONSTRAINT [FK_StoreRequestItems_StoreRequests];

ALTER TABLE [dbo].[SupermarketAccountPrices] WITH CHECK CHECK CONSTRAINT [FK_SupermarketAccountPrices_Recipes];

ALTER TABLE [dbo].[SupermarketAccountPrices] WITH CHECK CHECK CONSTRAINT [FK_SupermarketAccountPrices_SupermarketAccounts];

ALTER TABLE [dbo].[SupermarketAccounts] WITH CHECK CHECK CONSTRAINT [FK_SupermarketAccounts_Customers];

ALTER TABLE [dbo].[SupermarketAccounts] WITH CHECK CHECK CONSTRAINT [FK_SupermarketAccounts_SalesReps];

ALTER TABLE [dbo].[TripLegs] WITH CHECK CHECK CONSTRAINT [FK_TripLegs_CharterBookings];

ALTER TABLE [dbo].[Users] WITH CHECK CHECK CONSTRAINT [FK_Users_Employees];

ALTER TABLE [dbo].[Users] WITH CHECK CHECK CONSTRAINT [FK_Users_Roles];

ALTER TABLE [dbo].[Users] WITH CHECK CHECK CONSTRAINT [FK_Users_Customers];

ALTER TABLE [dbo].[Vehicles] WITH CHECK CHECK CONSTRAINT [FK_Vehicles_Employees];

ALTER TABLE [dbo].[WeeklyFleetExpenses] WITH CHECK CHECK CONSTRAINT [FK_WeeklyFleetExpenses_Vehicles];

ALTER TABLE [dbo].[CharterBookings] WITH CHECK CHECK CONSTRAINT [CK_CharterBookings_TripStatus];

ALTER TABLE [dbo].[CharterBookings] WITH CHECK CHECK CONSTRAINT [CK_CharterBookings_Amounts];

ALTER TABLE [dbo].[CharterQuoteLegs] WITH CHECK CHECK CONSTRAINT [CK_CharterQuoteLegs_LegNumber];

ALTER TABLE [dbo].[CharterQuoteLegs] WITH CHECK CHECK CONSTRAINT [CK_CharterQuoteLegs_FuelOption];

ALTER TABLE [dbo].[CharterQuoteLegs] WITH CHECK CHECK CONSTRAINT [CK_CharterQuoteLegs_Amounts];

ALTER TABLE [dbo].[CharterQuoteRequests] WITH CHECK CHECK CONSTRAINT [CK_CharterQuoteRequests_Amounts];

ALTER TABLE [dbo].[CharterQuoteRequests] WITH CHECK CHECK CONSTRAINT [CK_CharterQuoteRequests_TripType];

ALTER TABLE [dbo].[CharterQuoteRequests] WITH CHECK CHECK CONSTRAINT [CK_CharterQuoteRequests_Status];

ALTER TABLE [dbo].[CustomerReturns] WITH CHECK CHECK CONSTRAINT [CK_CustomerReturns_ReplacementQuantity];

ALTER TABLE [dbo].[CustomerReturns] WITH CHECK CHECK CONSTRAINT [CK_CustomerReturns_Quantity];

ALTER TABLE [dbo].[CustomerReturns] WITH CHECK CHECK CONSTRAINT [CK_CustomerReturns_UnitPrice];

ALTER TABLE [dbo].[Customers] WITH CHECK CHECK CONSTRAINT [CK_Customers_Category];

ALTER TABLE [dbo].[Customers] WITH CHECK CHECK CONSTRAINT [CK_Customers_OutstandingBalance];

ALTER TABLE [dbo].[Employees] WITH CHECK CHECK CONSTRAINT [CK_Employees_EmploymentStatus];

ALTER TABLE [dbo].[Employees] WITH CHECK CHECK CONSTRAINT [CK_Employees_BaseSalary];

ALTER TABLE [dbo].[FleetDispatches] WITH CHECK CHECK CONSTRAINT [CK_FleetDispatches_Status];

ALTER TABLE [dbo].[FleetDispatches] WITH CHECK CHECK CONSTRAINT [CK_FleetDispatches_Odometer];

ALTER TABLE [dbo].[FleetDispatchStops] WITH CHECK CHECK CONSTRAINT [CK_FleetDispatchStops_Status];

ALTER TABLE [dbo].[FleetDispatchStops] WITH CHECK CHECK CONSTRAINT [CK_FleetDispatchStops_StopNumber];

ALTER TABLE [dbo].[FuelLogs] WITH CHECK CHECK CONSTRAINT [CK_FuelLogs_Amounts];

ALTER TABLE [dbo].[FuelLogs] WITH CHECK CHECK CONSTRAINT [CK_FuelLogs_OneAssignment];

ALTER TABLE [dbo].[FuelLogs] WITH CHECK CHECK CONSTRAINT [CK_FuelLogs_FuelType];

ALTER TABLE [dbo].[IngredientPriceChangeRequests] WITH CHECK CHECK CONSTRAINT [CK_IngredientPriceChangeRequests_Costs];

ALTER TABLE [dbo].[IngredientPriceChangeRequests] WITH CHECK CHECK CONSTRAINT [CK_IngredientPriceChangeRequests_Status];

ALTER TABLE [dbo].[InventoryItems] WITH CHECK CHECK CONSTRAINT [CK_InventoryItems_Quantities];

ALTER TABLE [dbo].[MaintenanceLogs] WITH CHECK CHECK CONSTRAINT [CK_MaintenanceLogs_Cost];

ALTER TABLE [dbo].[MeasurementDensities] WITH CHECK CHECK CONSTRAINT [CK_MeasurementDensities_Density];

ALTER TABLE [dbo].[Notifications] WITH CHECK CHECK CONSTRAINT [CK_Notifications_Type];

ALTER TABLE [dbo].[OrderItems] WITH CHECK CHECK CONSTRAINT [CK_OrderItems_BonusQuantity];

ALTER TABLE [dbo].[OrderItems] WITH CHECK CHECK CONSTRAINT [CK_OrderItems_Quantity];

ALTER TABLE [dbo].[OrderItems] WITH CHECK CHECK CONSTRAINT [CK_OrderItems_UnitPrice];

ALTER TABLE [dbo].[OrderPayments] WITH CHECK CHECK CONSTRAINT [CK_OrderPayments_Amount];

ALTER TABLE [dbo].[OrderPayments] WITH CHECK CHECK CONSTRAINT [CK_OrderPayments_Method];

ALTER TABLE [dbo].[Orders] WITH CHECK CHECK CONSTRAINT [CK_Orders_Amounts];

ALTER TABLE [dbo].[Orders] WITH CHECK CHECK CONSTRAINT [CK_Orders_CustomerCategory];

ALTER TABLE [dbo].[Orders] WITH CHECK CHECK CONSTRAINT [CK_Orders_BookedByType];

ALTER TABLE [dbo].[Orders] WITH CHECK CHECK CONSTRAINT [CK_Orders_PaymentMethod];

ALTER TABLE [dbo].[Orders] WITH CHECK CHECK CONSTRAINT [CK_Orders_OrderType];

ALTER TABLE [dbo].[Orders] WITH CHECK CHECK CONSTRAINT [CK_Orders_PaymentStatus];

ALTER TABLE [dbo].[Orders] WITH CHECK CHECK CONSTRAINT [CK_Orders_Status];

ALTER TABLE [dbo].[PayrollDeductionLines] WITH CHECK CHECK CONSTRAINT [CK_PayrollDeductionLines_Amount];

ALTER TABLE [dbo].[PayrollRecords] WITH CHECK CHECK CONSTRAINT [CK_PayrollRecords_Year];

ALTER TABLE [dbo].[PayrollRecords] WITH CHECK CHECK CONSTRAINT [CK_PayrollRecords_PaymentStatus];

ALTER TABLE [dbo].[PayrollRecords] WITH CHECK CHECK CONSTRAINT [CK_PayrollRecords_Amounts];

ALTER TABLE [dbo].[PayrollRecords] WITH CHECK CHECK CONSTRAINT [CK_PayrollRecords_Month];

ALTER TABLE [dbo].[PayrollRuns] WITH CHECK CHECK CONSTRAINT [CK_PayrollRuns_Amounts];

ALTER TABLE [dbo].[PayrollRuns] WITH CHECK CHECK CONSTRAINT [CK_PayrollRuns_Year];

ALTER TABLE [dbo].[PayrollRuns] WITH CHECK CHECK CONSTRAINT [CK_PayrollRuns_Status];

ALTER TABLE [dbo].[PayrollRuns] WITH CHECK CHECK CONSTRAINT [CK_PayrollRuns_Month];

ALTER TABLE [dbo].[PayrollSettings] WITH CHECK CHECK CONSTRAINT [CK_PayrollSettings_SingleRow];

ALTER TABLE [dbo].[PayrollSettings] WITH CHECK CHECK CONSTRAINT [CK_PayrollSettings_Percentages];

ALTER TABLE [dbo].[ProductionBatches] WITH CHECK CHECK CONSTRAINT [CK_ProductionBatches_Costs];

ALTER TABLE [dbo].[ProductionBatches] WITH CHECK CHECK CONSTRAINT [CK_ProductionBatches_Quantities];

ALTER TABLE [dbo].[ProductionBatches] WITH CHECK CHECK CONSTRAINT [CK_ProductionBatches_Status];

ALTER TABLE [dbo].[ProductionBatchItems] WITH CHECK CHECK CONSTRAINT [CK_ProductionBatchItems_Costs];

ALTER TABLE [dbo].[ProductionBatchItems] WITH CHECK CHECK CONSTRAINT [CK_ProductionBatchItems_Quantities];

ALTER TABLE [dbo].[ProductionDefects] WITH CHECK CHECK CONSTRAINT [CK_ProductionDefects_Quantity];

ALTER TABLE [dbo].[ProductionProductTypes] WITH CHECK CHECK CONSTRAINT [CK_ProductionProductTypes_Text];

ALTER TABLE [dbo].[ProductRepositories] WITH CHECK CHECK CONSTRAINT [CK_ProductRepositories_DefaultIsFactory];

ALTER TABLE [dbo].[ProductRepositories] WITH CHECK CHECK CONSTRAINT [CK_ProductRepositories_LocationType];

ALTER TABLE [dbo].[ProductRepositoryMovements] WITH CHECK CHECK CONSTRAINT [CK_ProductRepositoryMovements_Type];

ALTER TABLE [dbo].[ProductRepositoryMovements] WITH CHECK CHECK CONSTRAINT [CK_ProductRepositoryMovements_Quantities];

ALTER TABLE [dbo].[ProductRepositoryStocks] WITH CHECK CHECK CONSTRAINT [CK_ProductRepositoryStocks_Quantities];

ALTER TABLE [dbo].[RecipeIngredients] WITH CHECK CHECK CONSTRAINT [CK_RecipeIngredients_Quantities];

ALTER TABLE [dbo].[Recipes] WITH CHECK CHECK CONSTRAINT [CK_Recipes_Amounts];

ALTER TABLE [dbo].[SalaryAdvances] WITH CHECK CHECK CONSTRAINT [CK_SalaryAdvances_Status];

ALTER TABLE [dbo].[SalaryAdvances] WITH CHECK CHECK CONSTRAINT [CK_SalaryAdvances_Amounts];

ALTER TABLE [dbo].[SalesBonusRules] WITH CHECK CHECK CONSTRAINT [CK_SalesBonusRules_Quantities];

ALTER TABLE [dbo].[SalesReconciliationItems] WITH CHECK CHECK CONSTRAINT [CK_SalesReconciliationItems_Quantities];

ALTER TABLE [dbo].[SalesReconciliationItems] WITH CHECK CHECK CONSTRAINT [CK_SalesReconciliationItems_UnitPrice];

ALTER TABLE [dbo].[SalesReconciliations] WITH CHECK CHECK CONSTRAINT [CK_SalesReconciliations_Amounts];

ALTER TABLE [dbo].[SalesReconciliations] WITH CHECK CHECK CONSTRAINT [CK_SalesReconciliations_Status];

ALTER TABLE [dbo].[StockMovements] WITH CHECK CHECK CONSTRAINT [CK_StockMovements_Amounts];

ALTER TABLE [dbo].[StockMovements] WITH CHECK CHECK CONSTRAINT [CK_StockMovements_Type];

ALTER TABLE [dbo].[StoreRequestItems] WITH CHECK CHECK CONSTRAINT [CK_StoreRequestItems_Quantities];

ALTER TABLE [dbo].[StoreRequests] WITH CHECK CHECK CONSTRAINT [CK_StoreRequests_Status];

ALTER TABLE [dbo].[SupermarketAccountPrices] WITH CHECK CHECK CONSTRAINT [CK_SupermarketAccountPrices_AgreedPrice];

ALTER TABLE [dbo].[SupermarketAccounts] WITH CHECK CHECK CONSTRAINT [CK_SupermarketAccounts_Credit];

ALTER TABLE [dbo].[TreasuryPolicySettings] WITH CHECK CHECK CONSTRAINT [CK_TreasuryPolicySettings_SingleRow];

ALTER TABLE [dbo].[TreasuryPolicySettings] WITH CHECK CHECK CONSTRAINT [CK_TreasuryPolicySettings_Percentages];

ALTER TABLE [dbo].[TripLegs] WITH CHECK CHECK CONSTRAINT [CK_TripLegs_LegNumber];

ALTER TABLE [dbo].[TripLegs] WITH CHECK CHECK CONSTRAINT [CK_TripLegs_FuelOption];

ALTER TABLE [dbo].[TripLegs] WITH CHECK CHECK CONSTRAINT [CK_TripLegs_LegCost];

ALTER TABLE [dbo].[Vehicles] WITH CHECK CHECK CONSTRAINT [CK_Vehicles_Status];

ALTER TABLE [dbo].[Vehicles] WITH CHECK CHECK CONSTRAINT [CK_Vehicles_Odometer];

ALTER TABLE [dbo].[Vehicles] WITH CHECK CHECK CONSTRAINT [CK_Vehicles_TankCapacity];

ALTER TABLE [dbo].[WeeklyFleetExpenses] WITH CHECK CHECK CONSTRAINT [CK_WeeklyFleetExpenses_Dates];

ALTER TABLE [dbo].[WeeklyFleetExpenses] WITH CHECK CHECK CONSTRAINT [CK_WeeklyFleetExpenses_Amounts];


GO

/* -------------------------------------------------------------------------
   Roles

   Role IDs are an application contract (Manufacture.Models.Domain.UserRole):
     1 SuperAdmin, 2 ProductionManager, 3 StoreManager,
     4 SalesRep, 5 Customer, 6 HrPayrollManager.
   Insert them with explicit IDs so navigation/authorization by ID stays stable
   even if a role name is changed later.
   ------------------------------------------------------------------------- */

IF EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'Vendor')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = N'Customer')
    UPDATE [dbo].[Roles]
    SET [Name] = N'Customer',
        [DisplayName] = N'Customer',
        [Description] = N'Customer portal for self-service pre-orders and standing orders.',
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [Name] = N'Vendor';

SET IDENTITY_INSERT [dbo].[Roles] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 1)
    INSERT INTO [dbo].[Roles] ([Id], [Name], [DisplayName], [Description], [IsSystemRole]) VALUES
    (1, N'SuperAdmin', N'Super Admin', N'Full enterprise control and settings.', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 2)
    INSERT INTO [dbo].[Roles] ([Id], [Name], [DisplayName], [Description], [IsSystemRole]) VALUES
    (2, N'ProductionManager', N'Production Manager', N'Recipes, batches, and oven output.', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 3)
    INSERT INTO [dbo].[Roles] ([Id], [Name], [DisplayName], [Description], [IsSystemRole]) VALUES
    (3, N'StoreManager', N'Store Manager', N'Inventory, requisitions, and material issues.', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 4)
    INSERT INTO [dbo].[Roles] ([Id], [Name], [DisplayName], [Description], [IsSystemRole]) VALUES
    (4, N'SalesRep', N'Sales Representative', N'POS registers, counter sales, and pre-orders.', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 5)
    INSERT INTO [dbo].[Roles] ([Id], [Name], [DisplayName], [Description], [IsSystemRole]) VALUES
    (5, N'Customer', N'Customer', N'External vendor / supermarket pre-orders portal.', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 6)
    INSERT INTO [dbo].[Roles] ([Id], [Name], [DisplayName], [Description], [IsSystemRole]) VALUES
    (6, N'HrPayrollManager', N'HR & Payroll Manager', N'Staff records, advances, and payroll runs.', 1);

SET IDENTITY_INSERT [dbo].[Roles] OFF;
GO

IF (SELECT COUNT(*) FROM [dbo].[Roles] WHERE [Id] BETWEEN 1 AND 6) <> 6
    THROW 50001, N'The six system roles must exist with IDs 1-6. Check the Roles table before seeding.', 1;
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

/* ============================================================================
   SEED: the SuperAdmin user only
   ============================================================================ */

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = N'admin@bakery.com')
    INSERT INTO [dbo].[Users]
        ([Name], [Email], [RoleId], [PinCodeHash], [PasswordHash], [SecurityStamp],
         [MustChangePassword], [PhoneNumber], [IsActive], [CreatedAt], [UpdatedAt])
    VALUES
        (N'Adekunle Johnson',
         N'admin@bakery.com',
         1,
         N'AQAAAAIAAYagAAAAEN2fB6Gm80DH/72PU5Ol/S9J8nYJQfeadr0U+Zbb74dIZadBzXBkGwTZce0Fy08Tng==',
         N'AQAAAAIAAYagAAAAENuCi0Rzrw3zfRHF7vP5SOUzzVLkYLD8ckUbuoDxS70ubvIIpJ+VjVMVqW+p6wPKXA==',
         N'3DF8B711-DD04-49C1-A02A-F9F3CB3ED14F',
         0,
         N'+2348011112222',
         1,
         SYSUTCDATETIME(),
         SYSUTCDATETIME());
GO

PRINT N'Schema and SuperAdmin seed complete.';
GO
