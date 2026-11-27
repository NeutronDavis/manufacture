CREATE TABLE [dbo].[ProductionBatchItems]
(
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [ProductionBatchId]     INT            NOT NULL,
    [RecipeId]              INT            NOT NULL,
    [RecipeName]            NVARCHAR (150) NOT NULL CONSTRAINT [DF_ProductionBatchItems_RecipeName] DEFAULT (N''),
    [ProductCode]           NVARCHAR (40)  NOT NULL CONSTRAINT [DF_ProductionBatchItems_ProductCode] DEFAULT (N''),
    [TargetQuantity]        INT            NOT NULL CONSTRAINT [DF_ProductionBatchItems_TargetQuantity] DEFAULT (0),
    [ActualQuantity]        INT            NOT NULL CONSTRAINT [DF_ProductionBatchItems_ActualQuantity] DEFAULT (0),
    [Variance]              AS ([ActualQuantity] - [TargetQuantity]) PERSISTED,
    [BatchInputQuantity]    DECIMAL (18, 3) NOT NULL CONSTRAINT [DF_ProductionBatchItems_BatchInputQuantity] DEFAULT (0),
    [BatchInputUnitLabel]   NVARCHAR (60)  NOT NULL CONSTRAINT [DF_ProductionBatchItems_BatchInputUnitLabel] DEFAULT (N''),
    [OutputUnitLabel]       NVARCHAR (40)  NOT NULL CONSTRAINT [DF_ProductionBatchItems_OutputUnitLabel] DEFAULT (N''),
    [TotalBatchCost]        DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_ProductionBatchItems_TotalBatchCost] DEFAULT (0),
    [ExpectedUnitCost]      DECIMAL (18, 4) NOT NULL CONSTRAINT [DF_ProductionBatchItems_ExpectedUnitCost] DEFAULT (0),
    [CalculatedUnitCost]    DECIMAL (18, 4) NOT NULL CONSTRAINT [DF_ProductionBatchItems_CalculatedUnitCost] DEFAULT (0),
    [ActualRecordedAt]      DATETIME2 (3)  NULL,
    [CreatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductionBatchItems_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductionBatchItems_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_ProductionBatchItems] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductionBatchItems_Batch_Recipe] UNIQUE ([ProductionBatchId], [RecipeId]),
    CONSTRAINT [FK_ProductionBatchItems_ProductionBatches] FOREIGN KEY ([ProductionBatchId]) REFERENCES [dbo].[ProductionBatches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductionBatchItems_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]),
    CONSTRAINT [CK_ProductionBatchItems_Quantities] CHECK ([TargetQuantity] >= 0 AND [ActualQuantity] >= 0 AND [BatchInputQuantity] >= 0),
    CONSTRAINT [CK_ProductionBatchItems_Costs] CHECK ([TotalBatchCost] >= 0 AND [ExpectedUnitCost] >= 0 AND [CalculatedUnitCost] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_ProductionBatchItems_RecipeId]
    ON [dbo].[ProductionBatchItems] ([RecipeId] ASC);
GO
