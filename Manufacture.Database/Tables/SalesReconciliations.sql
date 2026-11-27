CREATE TABLE [dbo].[SalesReconciliations]
(
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [ReconciliationNumber]  NVARCHAR (40)  NOT NULL,
    [ReconciliationDate]    DATE           NOT NULL,
    [SalesRepId]            INT            NOT NULL,
    [PosTerminalId]         INT            NULL,
    [ProductRepositoryId]   INT            NULL,
    [RouteName]             NVARCHAR (120) NOT NULL CONSTRAINT [DF_SalesReconciliations_RouteName] DEFAULT (N''),
    [VehicleRegistration]   NVARCHAR (40)  NOT NULL CONSTRAINT [DF_SalesReconciliations_VehicleRegistration] DEFAULT (N''),
    [CashHandedOver]        DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_SalesReconciliations_CashHandedOver] DEFAULT (0),
    [PosTerminalReceipts]   DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_SalesReconciliations_PosTerminalReceipts] DEFAULT (0),
    [TotalCollected]        AS ([CashHandedOver] + [PosTerminalReceipts]) PERSISTED,
    [TotalExpectedRevenue]  DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_SalesReconciliations_TotalExpectedRevenue] DEFAULT (0),
    [Variance]              AS ([TotalExpectedRevenue] - ([CashHandedOver] + [PosTerminalReceipts])) PERSISTED,
    [Status]                INT            NOT NULL CONSTRAINT [DF_SalesReconciliations_Status] DEFAULT (0),
    [StockPostedAt]         DATETIME2 (3)  NULL,
    [Notes]                 NVARCHAR (500) NOT NULL CONSTRAINT [DF_SalesReconciliations_Notes] DEFAULT (N''),
    [ReconciledBy]          NVARCHAR (150) NULL,
    [ReconciledAt]          DATETIME2 (3)  NULL,
    [CreatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SalesReconciliations_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SalesReconciliations_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]            ROWVERSION     NOT NULL,

    CONSTRAINT [PK_SalesReconciliations] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SalesReconciliations_Number] UNIQUE ([ReconciliationNumber]),
    CONSTRAINT [UQ_SalesReconciliations_Rep_Date] UNIQUE ([SalesRepId], [ReconciliationDate]),
    CONSTRAINT [FK_SalesReconciliations_SalesReps] FOREIGN KEY ([SalesRepId]) REFERENCES [dbo].[SalesReps] ([Id]),
    CONSTRAINT [FK_SalesReconciliations_PosTerminals] FOREIGN KEY ([PosTerminalId]) REFERENCES [dbo].[PosTerminals] ([Id]),
    CONSTRAINT [FK_SalesReconciliations_ProductRepositories] FOREIGN KEY ([ProductRepositoryId]) REFERENCES [dbo].[ProductRepositories] ([Id]),
    CONSTRAINT [CK_SalesReconciliations_Status] CHECK ([Status] IN (0, 1)),
    CONSTRAINT [CK_SalesReconciliations_Amounts] CHECK ([CashHandedOver] >= 0 AND [PosTerminalReceipts] >= 0 AND [TotalExpectedRevenue] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_SalesReconciliations_Date_Status]
    ON [dbo].[SalesReconciliations] ([ReconciliationDate] DESC, [Status] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_SalesReconciliations_PosTerminalId]
    ON [dbo].[SalesReconciliations] ([PosTerminalId] ASC)
    WHERE [PosTerminalId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_SalesReconciliations_ProductRepositoryId]
    ON [dbo].[SalesReconciliations] ([ProductRepositoryId] ASC)
    WHERE [ProductRepositoryId] IS NOT NULL;
GO
