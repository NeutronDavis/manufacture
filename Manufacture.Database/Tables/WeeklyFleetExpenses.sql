CREATE TABLE [dbo].[WeeklyFleetExpenses]
(
    [Id]                       INT            IDENTITY (1, 1) NOT NULL,
    [OperatingWeekStart]       DATE           NOT NULL,
    [OperatingWeekEnd]         DATE           NOT NULL,
    [VehicleId]                INT            NOT NULL,
    [DriverName]               NVARCHAR (150) NOT NULL CONSTRAINT [DF_WeeklyFleetExpenses_DriverName] DEFAULT (N''),
    [FuelExpenseTotal]         DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_WeeklyFleetExpenses_FuelExpenseTotal] DEFAULT (0),
    [MaintenanceExpenseTotal]  DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_WeeklyFleetExpenses_MaintenanceExpenseTotal] DEFAULT (0),
    [IncidentalsTotal]         DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_WeeklyFleetExpenses_IncidentalsTotal] DEFAULT (0),
    [TotalWeeklyRunningCost]   AS ([FuelExpenseTotal] + [MaintenanceExpenseTotal] + [IncidentalsTotal]) PERSISTED,
    [CreatedAt]                DATETIME2 (3)  NOT NULL CONSTRAINT [DF_WeeklyFleetExpenses_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]                DATETIME2 (3)  NOT NULL CONSTRAINT [DF_WeeklyFleetExpenses_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_WeeklyFleetExpenses] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_WeeklyFleetExpenses_Vehicle_Week] UNIQUE ([VehicleId], [OperatingWeekStart]),
    CONSTRAINT [FK_WeeklyFleetExpenses_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]),
    CONSTRAINT [CK_WeeklyFleetExpenses_Dates] CHECK ([OperatingWeekEnd] >= [OperatingWeekStart]),
    CONSTRAINT [CK_WeeklyFleetExpenses_Amounts] CHECK ([FuelExpenseTotal] >= 0 AND [MaintenanceExpenseTotal] >= 0 AND [IncidentalsTotal] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_WeeklyFleetExpenses_Week]
    ON [dbo].[WeeklyFleetExpenses] ([OperatingWeekStart] DESC);
GO
