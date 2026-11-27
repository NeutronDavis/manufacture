CREATE TABLE [dbo].[ProductRepositoryStocks]
(
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [ProductRepositoryId]   INT            NOT NULL,
    [RecipeId]              INT            NOT NULL,
    [QuantityOnHand]        DECIMAL (18, 3) NOT NULL CONSTRAINT [DF_ProductRepositoryStocks_QuantityOnHand] DEFAULT (0),
    [ReorderThreshold]      DECIMAL (18, 3) NOT NULL CONSTRAINT [DF_ProductRepositoryStocks_ReorderThreshold] DEFAULT (0),
    [CreatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductRepositoryStocks_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductRepositoryStocks_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]            ROWVERSION     NOT NULL,

    CONSTRAINT [PK_ProductRepositoryStocks] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductRepositoryStocks_Repo_Recipe] UNIQUE ([ProductRepositoryId], [RecipeId]),
    CONSTRAINT [FK_ProductRepositoryStocks_ProductRepositories] FOREIGN KEY ([ProductRepositoryId]) REFERENCES [dbo].[ProductRepositories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductRepositoryStocks_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]),
    CONSTRAINT [CK_ProductRepositoryStocks_Quantities] CHECK ([QuantityOnHand] >= 0 AND [ReorderThreshold] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_ProductRepositoryStocks_RecipeId]
    ON [dbo].[ProductRepositoryStocks] ([RecipeId] ASC);
GO
