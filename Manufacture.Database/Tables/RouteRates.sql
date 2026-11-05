CREATE TABLE [dbo].[RouteRates]
(
    [Id]                   INT            IDENTITY (1, 1) NOT NULL,
    [OriginState]          NVARCHAR (80)  NOT NULL,
    [DestinationState]     NVARCHAR (120) NOT NULL,
    [VehicleTypeRequired]  NVARCHAR (60)  NOT NULL,
    [RateWithFuel]         DECIMAL (18, 2) NOT NULL,
    [RateWithoutFuel]      DECIMAL (18, 2) NOT NULL,
    [EstimatedHours]       INT            NOT NULL,
    [IsActive]             BIT            NOT NULL CONSTRAINT [DF_RouteRates_IsActive] DEFAULT (1),
    [CreatedAt]            DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RouteRates_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]            DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RouteRates_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_RouteRates] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RouteRates_Route_VehicleType] UNIQUE ([OriginState], [DestinationState], [VehicleTypeRequired]),
    CONSTRAINT [CK_RouteRates_Amounts] CHECK ([RateWithFuel] >= 0 AND [RateWithoutFuel] >= 0 AND [EstimatedHours] > 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_RouteRates_IsActive]
    ON [dbo].[RouteRates] ([IsActive] ASC);
GO
