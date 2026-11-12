CREATE TABLE [dbo].[SalesBonusRules]
(
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [RecipeId]          INT            NOT NULL,
    [ThresholdQuantity] INT            NOT NULL,
    [BonusQuantity]     INT            NOT NULL,
    [IsActive]          BIT            NOT NULL CONSTRAINT [DF_SalesBonusRules_IsActive] DEFAULT (1),
    [CreatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SalesBonusRules_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SalesBonusRules_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_SalesBonusRules] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SalesBonusRules_Recipe_Threshold] UNIQUE ([RecipeId], [ThresholdQuantity]),
    CONSTRAINT [FK_SalesBonusRules_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_SalesBonusRules_Quantities] CHECK ([ThresholdQuantity] > 0 AND [BonusQuantity] > 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_SalesBonusRules_RecipeId_IsActive]
    ON [dbo].[SalesBonusRules] ([RecipeId] ASC, [IsActive] ASC);
GO
