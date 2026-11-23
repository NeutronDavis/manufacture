CREATE TABLE [dbo].[ProductionProductTypes]
(
    [Id]                   INT            IDENTITY (1, 1) NOT NULL,
    [ProductionCategoryId] INT            NOT NULL,
    [Name]                 NVARCHAR (120) NOT NULL,
    [Baseline]             NVARCHAR (150) NOT NULL,
    [OutputUnitNoun]       NVARCHAR (40)  NOT NULL,
    [IsActive]             BIT            NOT NULL CONSTRAINT [DF_ProductionProductTypes_IsActive] DEFAULT (1),
    [CreatedAt]            DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductionProductTypes_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]            DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductionProductTypes_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]           ROWVERSION     NOT NULL,

    CONSTRAINT [PK_ProductionProductTypes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductionProductTypes_Category_Name] UNIQUE ([ProductionCategoryId], [Name]),
    CONSTRAINT [FK_ProductionProductTypes_ProductionCategories] FOREIGN KEY ([ProductionCategoryId]) REFERENCES [dbo].[ProductionCategories] ([Id]),
    CONSTRAINT [CK_ProductionProductTypes_Text] CHECK (LEN([Baseline]) > 0 AND LEN([OutputUnitNoun]) > 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_ProductionProductTypes_Category_IsActive]
    ON [dbo].[ProductionProductTypes] ([ProductionCategoryId] ASC, [IsActive] ASC);
GO
