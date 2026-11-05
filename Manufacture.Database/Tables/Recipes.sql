CREATE TABLE [dbo].[Recipes]
(
    [Id]                         INT            IDENTITY (1, 1) NOT NULL,
    [Name]                       NVARCHAR (150) NOT NULL,
    [Code]                       NVARCHAR (40)  NOT NULL,
    [Description]                NVARCHAR (500) NOT NULL CONSTRAINT [DF_Recipes_Description] DEFAULT (N''),
    [ProductType]                INT            NOT NULL CONSTRAINT [DF_Recipes_ProductType] DEFAULT (0),
    [BaselineUnit]               INT            NOT NULL CONSTRAINT [DF_Recipes_BaselineUnit] DEFAULT (0),
    [ExpectedYield]              DECIMAL (18, 3) NOT NULL,
    [OutputUnit]                 NVARCHAR (40)  NOT NULL CONSTRAINT [DF_Recipes_OutputUnit] DEFAULT (N'loaves'),
    [PackagingCost]              DECIMAL (18, 4) NOT NULL CONSTRAINT [DF_Recipes_PackagingCost] DEFAULT (0),
    [LaborAndOverheadPerUnit]    DECIMAL (18, 4) NOT NULL CONSTRAINT [DF_Recipes_LaborAndOverheadPerUnit] DEFAULT (0),
    [SellingPrice]               DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_Recipes_SellingPrice] DEFAULT (0),
    [IsActive]                   BIT            NOT NULL CONSTRAINT [DF_Recipes_IsActive] DEFAULT (1),
    [CreatedAt]                  DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Recipes_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]                  DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Recipes_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]                 ROWVERSION     NOT NULL,

    CONSTRAINT [PK_Recipes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Recipes_Code] UNIQUE ([Code]),
    CONSTRAINT [CK_Recipes_ProductType] CHECK ([ProductType] IN (0, 1, 2)),
    CONSTRAINT [CK_Recipes_BaselineUnit] CHECK ([BaselineUnit] IN (0, 1, 2)),
    CONSTRAINT [CK_Recipes_Amounts] CHECK ([ExpectedYield] > 0 AND [PackagingCost] >= 0 AND [LaborAndOverheadPerUnit] >= 0 AND [SellingPrice] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_Recipes_ProductType_IsActive]
    ON [dbo].[Recipes] ([ProductType] ASC, [IsActive] ASC);
GO
