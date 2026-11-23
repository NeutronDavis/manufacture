CREATE TABLE [dbo].[FleetDispatches]
(
    [Id]                 INT             IDENTITY (1, 1) NOT NULL,
    [DispatchRef]        NVARCHAR (40)   NOT NULL,
    [DispatchDate]       DATE            NOT NULL,
    [VehicleId]          INT             NOT NULL,
    [DriverEmployeeId]   INT             NULL,
    [DriverName]         NVARCHAR (150)  NOT NULL CONSTRAINT [DF_FleetDispatches_DriverName] DEFAULT (N''),
    [Purpose]            NVARCHAR (200)  NOT NULL CONSTRAINT [DF_FleetDispatches_Purpose] DEFAULT (N''),
    [Status]             INT             NOT NULL CONSTRAINT [DF_FleetDispatches_Status] DEFAULT (0),
    [OdometerEnd]        DECIMAL (18, 2) NULL,
    [Notes]              NVARCHAR (1000) NOT NULL CONSTRAINT [DF_FleetDispatches_Notes] DEFAULT (N''),
    [CreatedByUserId]    INT             NULL,
    [CreatedAt]          DATETIME2 (3)   NOT NULL CONSTRAINT [DF_FleetDispatches_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]          DATETIME2 (3)   NOT NULL CONSTRAINT [DF_FleetDispatches_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]         ROWVERSION      NOT NULL,

    CONSTRAINT [PK_FleetDispatches] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_FleetDispatches_Ref] UNIQUE ([DispatchRef]),
    CONSTRAINT [FK_FleetDispatches_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]),
    CONSTRAINT [FK_FleetDispatches_Employees] FOREIGN KEY ([DriverEmployeeId]) REFERENCES [dbo].[Employees] ([Id]),
    CONSTRAINT [FK_FleetDispatches_Users] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id]),
    CONSTRAINT [CK_FleetDispatches_Status] CHECK ([Status] IN (0, 1, 2, 3, 4, 5)),
    CONSTRAINT [CK_FleetDispatches_Odometer] CHECK ([OdometerEnd] IS NULL OR [OdometerEnd] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_FleetDispatches_Date_Status]
    ON [dbo].[FleetDispatches] ([DispatchDate] DESC, [Status] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_FleetDispatches_Vehicle_Date]
    ON [dbo].[FleetDispatches] ([VehicleId] ASC, [DispatchDate] DESC);
GO
