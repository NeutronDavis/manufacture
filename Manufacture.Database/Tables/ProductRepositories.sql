CREATE TABLE [dbo].[ProductRepositories]
(
    [Id]                         INT            IDENTITY (1, 1) NOT NULL,
    [Code]                       NVARCHAR (30)  NOT NULL,
    [Name]                       NVARCHAR (120) NOT NULL,
    [LocationType]               INT            NOT NULL CONSTRAINT [DF_ProductRepositories_LocationType] DEFAULT (0),
    [Address]                    NVARCHAR (300) NOT NULL CONSTRAINT [DF_ProductRepositories_Address] DEFAULT (N''),
    [IsDefaultProductionOutput]  BIT            NOT NULL CONSTRAINT [DF_ProductRepositories_IsDefaultProductionOutput] DEFAULT (0),
    [IsActive]                   BIT            NOT NULL CONSTRAINT [DF_ProductRepositories_IsActive] DEFAULT (1),
    [CreatedAt]                  DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductRepositories_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]                  DATETIME2 (3)  NOT NULL CONSTRAINT [DF_ProductRepositories_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]                 ROWVERSION     NOT NULL,

    CONSTRAINT [PK_ProductRepositories] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductRepositories_Code] UNIQUE ([Code]),
    CONSTRAINT [CK_ProductRepositories_LocationType] CHECK ([LocationType] IN (0, 1)),
    CONSTRAINT [CK_ProductRepositories_DefaultIsFactory] CHECK ([IsDefaultProductionOutput] = 0 OR [LocationType] = 0)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_ProductRepositories_DefaultOutput]
    ON [dbo].[ProductRepositories] ([IsDefaultProductionOutput] ASC)
    WHERE [IsDefaultProductionOutput] = 1;
GO

CREATE NONCLUSTERED INDEX [IX_ProductRepositories_LocationType_IsActive]
    ON [dbo].[ProductRepositories] ([LocationType] ASC, [IsActive] ASC);
GO
