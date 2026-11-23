CREATE TABLE [dbo].[IngredientPriceChangeRequests]
(
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [InventoryItemId]   INT            NOT NULL,
    [InventoryItemName] NVARCHAR (150) NOT NULL,
    [RequestNumber]     NVARCHAR (40)  NOT NULL,
    [PreviousUnitCost]  DECIMAL (18, 4) NOT NULL,
    [RequestedUnitCost] DECIMAL (18, 4) NOT NULL,
    [Status]            INT            NOT NULL CONSTRAINT [DF_IngredientPriceChangeRequests_Status] DEFAULT (0),
    [RequestedByUserId] INT            NULL,
    [RequestedByName]   NVARCHAR (150) NOT NULL CONSTRAINT [DF_IngredientPriceChangeRequests_RequestedByName] DEFAULT (N''),
    [RequestedAt]       DATETIME2 (3)  NOT NULL CONSTRAINT [DF_IngredientPriceChangeRequests_RequestedAt] DEFAULT (SYSUTCDATETIME()),
    [ReviewedByUserId]  INT            NULL,
    [ReviewedByName]    NVARCHAR (150) NULL,
    [ReviewedAt]        DATETIME2 (3)  NULL,
    [ReviewNotes]       NVARCHAR (500) NOT NULL CONSTRAINT [DF_IngredientPriceChangeRequests_ReviewNotes] DEFAULT (N''),
    [AppliedAt]         DATETIME2 (3)  NULL,
    [CreatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_IngredientPriceChangeRequests_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_IngredientPriceChangeRequests_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]        ROWVERSION     NOT NULL,

    CONSTRAINT [PK_IngredientPriceChangeRequests] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_IngredientPriceChangeRequests_RequestNumber] UNIQUE ([RequestNumber]),
    CONSTRAINT [FK_IngredientPriceChangeRequests_InventoryItems] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[InventoryItems] ([Id]),
    CONSTRAINT [FK_IngredientPriceChangeRequests_RequestedByUsers] FOREIGN KEY ([RequestedByUserId]) REFERENCES [dbo].[Users] ([Id]),
    CONSTRAINT [FK_IngredientPriceChangeRequests_ReviewedByUsers] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [dbo].[Users] ([Id]),
    CONSTRAINT [CK_IngredientPriceChangeRequests_Costs] CHECK ([PreviousUnitCost] >= 0 AND [RequestedUnitCost] >= 0),
    CONSTRAINT [CK_IngredientPriceChangeRequests_Status] CHECK ([Status] IN (0, 1, 2))
);
GO

CREATE NONCLUSTERED INDEX [IX_IngredientPriceChangeRequests_Status_RequestedAt]
    ON [dbo].[IngredientPriceChangeRequests] ([Status] ASC, [RequestedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_IngredientPriceChangeRequests_InventoryItemId]
    ON [dbo].[IngredientPriceChangeRequests] ([InventoryItemId] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_IngredientPriceChangeRequests_OpenItem]
    ON [dbo].[IngredientPriceChangeRequests] ([InventoryItemId] ASC)
    WHERE [Status] = 0;
GO
