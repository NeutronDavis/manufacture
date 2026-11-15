CREATE TABLE [dbo].[FleetDispatchStops]
(
    [Id]                 INT             IDENTITY (1, 1) NOT NULL,
    [FleetDispatchId]    INT             NOT NULL,
    [StopNumber]         INT             NOT NULL,
    [DeliveryPointName]  NVARCHAR (150)  NOT NULL,
    [Address]            NVARCHAR (300)  NOT NULL,
    [CityState]          NVARCHAR (120)  NOT NULL,
    [ContactName]        NVARCHAR (150)  NULL,
    [ContactPhone]       NVARCHAR (30)   NULL,
    [Status]             INT             NOT NULL CONSTRAINT [DF_FleetDispatchStops_Status] DEFAULT (0),
    [DeliveredAt]        DATETIME2 (3)   NULL,
    [Notes]              NVARCHAR (500)  NULL,
    [CreatedAt]          DATETIME2 (3)   NOT NULL CONSTRAINT [DF_FleetDispatchStops_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]          DATETIME2 (3)   NOT NULL CONSTRAINT [DF_FleetDispatchStops_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_FleetDispatchStops] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_FleetDispatchStops_Dispatch_Stop] UNIQUE ([FleetDispatchId], [StopNumber]),
    CONSTRAINT [FK_FleetDispatchStops_FleetDispatches] FOREIGN KEY ([FleetDispatchId]) REFERENCES [dbo].[FleetDispatches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_FleetDispatchStops_StopNumber] CHECK ([StopNumber] > 0),
    CONSTRAINT [CK_FleetDispatchStops_Status] CHECK ([Status] IN (0, 1, 2))
);
GO

CREATE NONCLUSTERED INDEX [IX_FleetDispatchStops_Dispatch]
    ON [dbo].[FleetDispatchStops] ([FleetDispatchId] ASC, [StopNumber] ASC);
GO
