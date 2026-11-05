CREATE TABLE [dbo].[RecipeIngredients]
(
    [Id]                 INT            IDENTITY (1, 1) NOT NULL,
    [RecipeId]           INT            NOT NULL,
    [IngredientId]       INT            NOT NULL,
    [Unit]               NVARCHAR (40)  NOT NULL,
    [QuantityUnit]       NVARCHAR (40)  NOT NULL,
    [QuantityPerBatch]   DECIMAL (18, 4) NOT NULL,
    [BaselineYield]      DECIMAL (18, 3) NOT NULL CONSTRAINT [DF_RecipeIngredients_BaselineYield] DEFAULT (1),
    [IngredientUnitCost] DECIMAL (18, 4) NOT NULL CONSTRAINT [DF_RecipeIngredients_IngredientUnitCost] DEFAULT (0),
    [QuantityPerUnit]    AS (CASE WHEN [BaselineYield] > 0 THEN [QuantityPerBatch] / [BaselineYield] ELSE 0 END) PERSISTED,
    [BatchLineCost]      AS ([QuantityPerBatch] * [IngredientUnitCost]) PERSISTED,
    [CreatedAt]          DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RecipeIngredients_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]          DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RecipeIngredients_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_RecipeIngredients] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RecipeIngredients_Recipe_Ingredient] UNIQUE ([RecipeId], [IngredientId]),
    CONSTRAINT [FK_RecipeIngredients_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RecipeIngredients_Ingredients] FOREIGN KEY ([IngredientId]) REFERENCES [dbo].[Ingredients] ([Id]),
    CONSTRAINT [CK_RecipeIngredients_Quantities] CHECK ([QuantityPerBatch] >= 0 AND [BaselineYield] > 0 AND [IngredientUnitCost] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_RecipeIngredients_IngredientId]
    ON [dbo].[RecipeIngredients] ([IngredientId] ASC);
GO
