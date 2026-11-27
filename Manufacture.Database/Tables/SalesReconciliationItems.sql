CREATE TABLE [dbo].[SalesReconciliationItems]
(
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [SalesReconciliationId] INT            NOT NULL,
    [RecipeId]              INT            NOT NULL,
    [ProductName]           NVARCHAR (150) NOT NULL CONSTRAINT [DF_SalesReconciliationItems_ProductName] DEFAULT (N''),
    [ProductCode]           NVARCHAR (40)  NOT NULL CONSTRAINT [DF_SalesReconciliationItems_ProductCode] DEFAULT (N''),
    [UnitPrice]             DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_SalesReconciliationItems_UnitPrice] DEFAULT (0),
    [OpeningStock]          INT            NOT NULL CONSTRAINT [DF_SalesReconciliationItems_OpeningStock] DEFAULT (0),
    [QtySold]               INT            NOT NULL CONSTRAINT [DF_SalesReconciliationItems_QtySold] DEFAULT (0),
    [QtyDamaged]            INT            NOT NULL CONSTRAINT [DF_SalesReconciliationItems_QtyDamaged] DEFAULT (0),
    [QtyReplacement]        INT            NOT NULL CONSTRAINT [DF_SalesReconciliationItems_QtyReplacement] DEFAULT (0),
    [ClosingStock]          AS ([OpeningStock] - (([QtySold] + [QtyDamaged]) + [QtyReplacement])) PERSISTED,
    [ExpectedRevenue]       AS ([QtySold] * [UnitPrice]) PERSISTED,
    [CreatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SalesReconciliationItems_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SalesReconciliationItems_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_SalesReconciliationItems] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SalesReconciliationItems_Reconciliation_Recipe] UNIQUE ([SalesReconciliationId], [RecipeId]),
    CONSTRAINT [FK_SalesReconciliationItems_SalesReconciliations] FOREIGN KEY ([SalesReconciliationId]) REFERENCES [dbo].[SalesReconciliations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SalesReconciliationItems_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]),
    CONSTRAINT [CK_SalesReconciliationItems_Quantities] CHECK ([OpeningStock] >= 0 AND [QtySold] >= 0 AND [QtyDamaged] >= 0 AND [QtyReplacement] >= 0 AND [OpeningStock] >= ([QtySold] + [QtyDamaged] + [QtyReplacement])),
    CONSTRAINT [CK_SalesReconciliationItems_UnitPrice] CHECK ([UnitPrice] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_SalesReconciliationItems_RecipeId]
    ON [dbo].[SalesReconciliationItems] ([RecipeId] ASC);
GO
