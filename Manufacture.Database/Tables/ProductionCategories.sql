CREATE TABLE [dbo].[ProductionCategories]
(
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (80)  NOT NULL,
    [Description] NVARCHAR (300) NOT NULL CONSTRAINT [DF_ProductionCategories_Description] DEFAULT (N''),
    [IsActive]    BIT            NOT NULL CONSTRAINT [DF_ProductionCategories_IsActive] DEFAULT (1),
    [CreatedAt]   DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductionCategories_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]   DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductionCategories_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]  ROWVERSION     NOT NULL,

    CONSTRAINT [PK_ProductionCategories] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductionCategories_Name] UNIQUE ([Name])
);
GO

CREATE NONCLUSTERED INDEX [IX_ProductionCategories_IsActive]
    ON [dbo].[ProductionCategories] ([IsActive] ASC);
GO
