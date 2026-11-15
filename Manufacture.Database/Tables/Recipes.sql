CREATE TABLE [dbo].[Recipes]
(
    [Id]                         INT            IDENTITY (1, 1) NOT NULL,
    [Name]                       NVARCHAR (150) NOT NULL,
    [Code]                       NVARCHAR (40)  NOT NULL,
    [Description]                NVARCHAR (500) NOT NULL CONSTRAINT [DF_Recipes_Description] DEFAULT (N''),
    [ProductionProductTypeId]    INT            NULL,
    [ExpectedYield]              DECIMAL (18, 3) NOT NULL,
    [OutputUnit]                 NVARCHAR (40)  NOT NULL CONSTRAINT [DF_Recipes_OutputUnit] DEFAULT (N'loaves'),
    [SellingPrice]               DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_Recipes_SellingPrice] DEFAULT (0),
    [IsActive]                   BIT            NOT NULL CONSTRAINT [DF_Recipes_IsActive] DEFAULT (1),
    [CreatedAt]                  DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Recipes_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]                  DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Recipes_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]                 ROWVERSION     NOT NULL,

    CONSTRAINT [PK_Recipes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Recipes_Code] UNIQUE ([Code]),
    CONSTRAINT [FK_Recipes_ProductionProductTypes] FOREIGN KEY ([ProductionProductTypeId]) REFERENCES [dbo].[ProductionProductTypes] ([Id]),
    CONSTRAINT [CK_Recipes_Amounts] CHECK ([ExpectedYield] > 0 AND [SellingPrice] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_Recipes_ProductType_IsActive]
    ON [dbo].[Recipes] ([ProductionProductTypeId] ASC, [IsActive] ASC);
GO
