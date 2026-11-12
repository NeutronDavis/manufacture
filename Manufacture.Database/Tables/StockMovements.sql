CREATE TABLE [dbo].[StockMovements]
(
    [Id]              INT            IDENTITY (1, 1) NOT NULL,
    [InventoryItemId] INT            NOT NULL,
    [MovementType]    NVARCHAR (20)  NOT NULL,
    [Quantity]        DECIMAL (18, 3) NOT NULL,
    [BalanceAfter]    DECIMAL (18, 3) NOT NULL,
    [StoreRequestId]  INT            NULL,
    [ReferenceNumber] NVARCHAR (40)  NULL,
    [Notes]           NVARCHAR (300) NULL,
    [PerformedBy]     NVARCHAR (150) NOT NULL CONSTRAINT [DF_StockMovements_PerformedBy] DEFAULT (N'System'),
    [CreatedAt]       DATETIME2 (3)  NOT NULL CONSTRAINT [DF_StockMovements_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_StockMovements] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_StockMovements_InventoryItems] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[InventoryItems] ([Id]),
    CONSTRAINT [FK_StockMovements_StoreRequests] FOREIGN KEY ([StoreRequestId]) REFERENCES [dbo].[StoreRequests] ([Id]),
    CONSTRAINT [CK_StockMovements_Type] CHECK ([MovementType] IN (N'Receipt', N'Issue')),
    CONSTRAINT [CK_StockMovements_Amounts] CHECK ([Quantity] > 0 AND [BalanceAfter] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_StockMovements_InventoryItemId_CreatedAt]
    ON [dbo].[StockMovements] ([InventoryItemId] ASC, [CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_StockMovements_StoreRequestId]
    ON [dbo].[StockMovements] ([StoreRequestId] ASC)
    WHERE [StoreRequestId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_StockMovements_CreatedAt]
    ON [dbo].[StockMovements] ([CreatedAt] DESC);
GO
