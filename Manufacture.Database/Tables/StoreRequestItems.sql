CREATE TABLE [dbo].[StoreRequestItems]
(
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [StoreRequestId]    INT            NOT NULL,
    [InventoryItemId]   INT            NOT NULL,
    [ItemName]          NVARCHAR (150) NOT NULL,
    [Unit]              NVARCHAR (40)  NOT NULL,
    [RequestedQuantity] DECIMAL (18, 3) NOT NULL,
    [IssuedQuantity]    DECIMAL (18, 3) NOT NULL CONSTRAINT [DF_StoreRequestItems_IssuedQuantity] DEFAULT (0),
    [CreatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_StoreRequestItems_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_StoreRequestItems_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_StoreRequestItems] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_StoreRequestItems_StoreRequests] FOREIGN KEY ([StoreRequestId]) REFERENCES [dbo].[StoreRequests] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StoreRequestItems_InventoryItems] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[InventoryItems] ([Id]),
    CONSTRAINT [CK_StoreRequestItems_Quantities] CHECK ([RequestedQuantity] >= 0 AND [IssuedQuantity] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_StoreRequestItems_InventoryItemId]
    ON [dbo].[StoreRequestItems] ([InventoryItemId] ASC);
GO
