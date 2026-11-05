CREATE TABLE [dbo].[Ingredients]
(
    [Id]                   INT            IDENTITY (1, 1) NOT NULL,
    [ItemCode]             NVARCHAR (40)  NULL,
    [Name]                 NVARCHAR (150) NOT NULL,
    [Unit]                 NVARCHAR (40)  NOT NULL CONSTRAINT [DF_Ingredients_Unit] DEFAULT (N'kg'),
    [UnitCost]             DECIMAL (18, 4) NOT NULL CONSTRAINT [DF_Ingredients_UnitCost] DEFAULT (0),
    [CurrentStock]         DECIMAL (18, 3) NOT NULL CONSTRAINT [DF_Ingredients_CurrentStock] DEFAULT (0),
    [Category]             NVARCHAR (60)  NOT NULL CONSTRAINT [DF_Ingredients_Category] DEFAULT (N'Raw Materials'),
    [PackageUnit]          NVARCHAR (40)  NULL,
    [PackageCostPrice]     DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_Ingredients_PackageCostPrice] DEFAULT (0),
    [PackageStockOnHand]   DECIMAL (18, 3) NOT NULL CONSTRAINT [DF_Ingredients_PackageStockOnHand] DEFAULT (0),
    [SupplierId]           INT            NULL,
    [IsActive]             BIT            NOT NULL CONSTRAINT [DF_Ingredients_IsActive] DEFAULT (1),
    [CreatedAt]            DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Ingredients_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]            DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Ingredients_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_Ingredients] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Ingredients_Suppliers] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers] ([Id]),
    CONSTRAINT [CK_Ingredients_Amounts] CHECK ([UnitCost] >= 0 AND [CurrentStock] >= 0 AND [PackageCostPrice] >= 0 AND [PackageStockOnHand] >= 0)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_Ingredients_ItemCode]
    ON [dbo].[Ingredients] ([ItemCode] ASC)
    WHERE [ItemCode] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Ingredients_Category_IsActive]
    ON [dbo].[Ingredients] ([Category] ASC, [IsActive] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Ingredients_SupplierId]
    ON [dbo].[Ingredients] ([SupplierId] ASC)
    WHERE [SupplierId] IS NOT NULL;
GO
