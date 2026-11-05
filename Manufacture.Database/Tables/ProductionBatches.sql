CREATE TABLE [dbo].[ProductionBatches]
(
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [BatchNumber]           NVARCHAR (40)  NOT NULL,
    [RecipeId]              INT            NOT NULL,
    [RecipeName]            NVARCHAR (150) NOT NULL,
    [TargetQuantity]        INT            NOT NULL,
    [ActualQuantity]        INT            NOT NULL CONSTRAINT [DF_ProductionBatches_ActualQuantity] DEFAULT (0),
    [Variance]              AS ([ActualQuantity] - [TargetQuantity]) PERSISTED,
    [Shift]                 NVARCHAR (40)  NOT NULL CONSTRAINT [DF_ProductionBatches_Shift] DEFAULT (N'Morning'),
    [BakerInCharge]         NVARCHAR (150) NOT NULL CONSTRAINT [DF_ProductionBatches_BakerInCharge] DEFAULT (N''),
    [FlourBagsUsed]         DECIMAL (18, 3) NOT NULL CONSTRAINT [DF_ProductionBatches_FlourBagsUsed] DEFAULT (0),
    [ProductCategory]       INT            NOT NULL CONSTRAINT [DF_ProductionBatches_ProductCategory] DEFAULT (0),
    [BatchInputQuantity]    DECIMAL (18, 3) NOT NULL CONSTRAINT [DF_ProductionBatches_BatchInputQuantity] DEFAULT (1),
    [BatchInputUnitLabel]   NVARCHAR (60)  NOT NULL CONSTRAINT [DF_ProductionBatches_BatchInputUnitLabel] DEFAULT (N'Flour Bags (50kg)'),
    [OutputUnitLabel]       NVARCHAR (40)  NOT NULL CONSTRAINT [DF_ProductionBatches_OutputUnitLabel] DEFAULT (N'loaves'),
    [TargetOrderQuantity]   INT            NOT NULL CONSTRAINT [DF_ProductionBatches_TargetOrderQuantity] DEFAULT (0),
    [LinkedOrderId]         INT            NULL,
    [LinkedOrderNumber]     NVARCHAR (40)  NULL,
    [CustomerName]          NVARCHAR (150) NULL,
    [Status]                INT            NOT NULL CONSTRAINT [DF_ProductionBatches_Status] DEFAULT (0),
    [ProductionDate]        DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductionBatches_ProductionDate] DEFAULT (SYSUTCDATETIME()),
    [Notes]                 NVARCHAR (500) NOT NULL CONSTRAINT [DF_ProductionBatches_Notes] DEFAULT (N''),
    [TotalBatchCost]        DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_ProductionBatches_TotalBatchCost] DEFAULT (0),
    [CalculatedUnitCost]    DECIMAL (18, 4) NOT NULL CONSTRAINT [DF_ProductionBatches_CalculatedUnitCost] DEFAULT (0),
    [ExpectedUnitCost]      DECIMAL (18, 4) NOT NULL CONSTRAINT [DF_ProductionBatches_ExpectedUnitCost] DEFAULT (0),
    [LoggedBy]              NVARCHAR (150) NOT NULL CONSTRAINT [DF_ProductionBatches_LoggedBy] DEFAULT (N''),
    [CreatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductionBatches_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductionBatches_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]            ROWVERSION     NOT NULL,

    CONSTRAINT [PK_ProductionBatches] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductionBatches_BatchNumber] UNIQUE ([BatchNumber]),
    CONSTRAINT [FK_ProductionBatches_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]),
    CONSTRAINT [FK_ProductionBatches_Orders] FOREIGN KEY ([LinkedOrderId]) REFERENCES [dbo].[Orders] ([Id]),
    CONSTRAINT [CK_ProductionBatches_Quantities] CHECK ([TargetQuantity] >= 0 AND [ActualQuantity] >= 0 AND [FlourBagsUsed] >= 0 AND [BatchInputQuantity] >= 0),
    CONSTRAINT [CK_ProductionBatches_ProductCategory] CHECK ([ProductCategory] IN (0, 1, 2)),
    CONSTRAINT [CK_ProductionBatches_Status] CHECK ([Status] IN (0, 1, 2, 3, 4)),
    CONSTRAINT [CK_ProductionBatches_Costs] CHECK ([TotalBatchCost] >= 0 AND [CalculatedUnitCost] >= 0 AND [ExpectedUnitCost] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_ProductionBatches_ProductionDate]
    ON [dbo].[ProductionBatches] ([ProductionDate] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_ProductionBatches_RecipeId_Status]
    ON [dbo].[ProductionBatches] ([RecipeId] ASC, [Status] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ProductionBatches_LinkedOrderId]
    ON [dbo].[ProductionBatches] ([LinkedOrderId] ASC)
    WHERE [LinkedOrderId] IS NOT NULL;
GO
