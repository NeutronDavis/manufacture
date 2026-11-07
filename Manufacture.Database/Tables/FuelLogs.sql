CREATE TABLE [dbo].[FuelLogs]
(
    [Id]                 INT            IDENTITY (1, 1) NOT NULL,
    [VehicleId]          INT            NOT NULL,
    [DriverId]           INT            NULL,
    [FuelDate]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_FuelLogs_FuelDate] DEFAULT (SYSUTCDATETIME()),
    [FuelType]           NVARCHAR (40)  NOT NULL CONSTRAINT [DF_FuelLogs_FuelType] DEFAULT (N'Petrol'),
    [LitresDispensed]    DECIMAL (18, 2) NOT NULL,
    [PricePerLitre]      DECIMAL (18, 2) NOT NULL,
    [TotalCost]          AS ([LitresDispensed] * [PricePerLitre]) PERSISTED,
    [PetrolStation]      NVARCHAR (120) NOT NULL CONSTRAINT [DF_FuelLogs_PetrolStation] DEFAULT (N''),
    [RouteDescription]   NVARCHAR (200) NOT NULL CONSTRAINT [DF_FuelLogs_RouteDescription] DEFAULT (N''),
    [CreatedAt]          DATETIME2 (3)  NOT NULL CONSTRAINT [DF_FuelLogs_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_FuelLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_FuelLogs_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]),
    CONSTRAINT [FK_FuelLogs_Drivers] FOREIGN KEY ([DriverId]) REFERENCES [dbo].[Drivers] ([Id]),
    CONSTRAINT [CK_FuelLogs_FuelType] CHECK ([FuelType] IN (N'Petrol', N'Diesel', N'EV Charging')),
    CONSTRAINT [CK_FuelLogs_Amounts] CHECK ([LitresDispensed] > 0 AND [PricePerLitre] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_FuelLogs_VehicleId_FuelDate]
    ON [dbo].[FuelLogs] ([VehicleId] ASC, [FuelDate] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_FuelLogs_FuelDate]
    ON [dbo].[FuelLogs] ([FuelDate] DESC);
GO
