CREATE TABLE [dbo].[ProductionDefects]
(
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [ProductionBatchId] INT            NOT NULL,
    [RecipeId]          INT            NOT NULL,
    [Quantity]          INT            NOT NULL,
    [DefectReason]      NVARCHAR (250) NOT NULL CONSTRAINT [DF_ProductionDefects_DefectReason] DEFAULT (N''),
    [RecordedAt]        DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductionDefects_RecordedAt] DEFAULT (SYSUTCDATETIME()),
    [RecordedBy]        NVARCHAR (150) NOT NULL CONSTRAINT [DF_ProductionDefects_RecordedBy] DEFAULT (N''),
    [CreatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductionDefects_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_ProductionDefects] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProductionDefects_ProductionBatches] FOREIGN KEY ([ProductionBatchId]) REFERENCES [dbo].[ProductionBatches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductionDefects_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]),
    CONSTRAINT [CK_ProductionDefects_Quantity] CHECK ([Quantity] > 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_ProductionDefects_RecipeId_RecordedAt]
    ON [dbo].[ProductionDefects] ([RecipeId] ASC, [RecordedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_ProductionDefects_RecordedAt]
    ON [dbo].[ProductionDefects] ([RecordedAt] DESC);
GO
