CREATE TABLE [dbo].[MaintenanceLogs]
(
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [VehicleId]         INT            NOT NULL,
    [ServiceDate]       DATE           NOT NULL,
    [Description]       NVARCHAR (400) NOT NULL,
    [Cost]              DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_MaintenanceLogs_Cost] DEFAULT (0),
    [WorkshopOrVendor]  NVARCHAR (150) NOT NULL CONSTRAINT [DF_MaintenanceLogs_WorkshopOrVendor] DEFAULT (N''),
    [InvoiceNumber]     NVARCHAR (60)  NOT NULL CONSTRAINT [DF_MaintenanceLogs_InvoiceNumber] DEFAULT (N''),
    [CreatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_MaintenanceLogs_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_MaintenanceLogs_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_MaintenanceLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_MaintenanceLogs_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]),
    CONSTRAINT [CK_MaintenanceLogs_Cost] CHECK ([Cost] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_MaintenanceLogs_VehicleId_ServiceDate]
    ON [dbo].[MaintenanceLogs] ([VehicleId] ASC, [ServiceDate] DESC);
GO
