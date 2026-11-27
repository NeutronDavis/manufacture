CREATE TABLE [dbo].[ProductRepositoryMovements]
(
    [Id]                    BIGINT         IDENTITY (1, 1) NOT NULL,
    [ProductRepositoryId]   INT            NOT NULL,
    [RecipeId]              INT            NOT NULL,
    [MovementType]          INT            NOT NULL,
    [Quantity]              DECIMAL (18, 3) NOT NULL,
    [BalanceAfter]          DECIMAL (18, 3) NOT NULL,
    [ReferenceType]         NVARCHAR (40)  NOT NULL CONSTRAINT [DF_ProductRepositoryMovements_ReferenceType] DEFAULT (N''),
    [ReferenceId]           INT            NULL,
    [ReferenceNumber]       NVARCHAR (40)  NOT NULL CONSTRAINT [DF_ProductRepositoryMovements_ReferenceNumber] DEFAULT (N''),
    [Notes]                 NVARCHAR (300) NOT NULL CONSTRAINT [DF_ProductRepositoryMovements_Notes] DEFAULT (N''),
    [PerformedBy]           NVARCHAR (150) NOT NULL CONSTRAINT [DF_ProductRepositoryMovements_PerformedBy] DEFAULT (N''),
    [CreatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductRepositoryMovements_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_ProductRepositoryMovements] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProductRepositoryMovements_ProductRepositories] FOREIGN KEY ([ProductRepositoryId]) REFERENCES [dbo].[ProductRepositories] ([Id]),
    CONSTRAINT [FK_ProductRepositoryMovements_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]),
    CONSTRAINT [CK_ProductRepositoryMovements_Type] CHECK ([MovementType] IN (0, 1, 2, 3, 4, 5, 6, 7, 8)),
    CONSTRAINT [CK_ProductRepositoryMovements_Quantities] CHECK ([Quantity] > 0 AND [BalanceAfter] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_ProductRepositoryMovements_Repo_Recipe_CreatedAt]
    ON [dbo].[ProductRepositoryMovements] ([ProductRepositoryId] ASC, [RecipeId] ASC, [CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_ProductRepositoryMovements_CreatedAt]
    ON [dbo].[ProductRepositoryMovements] ([CreatedAt] DESC);
GO
