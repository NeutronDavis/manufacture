CREATE TABLE [dbo].[Suppliers]
(
    [Id]             INT            IDENTITY (1, 1) NOT NULL,
    [Name]           NVARCHAR (150) NOT NULL,
    [ContactPerson]  NVARCHAR (150) NOT NULL CONSTRAINT [DF_Suppliers_ContactPerson] DEFAULT (N''),
    [PhoneNumber]    NVARCHAR (30)  NOT NULL CONSTRAINT [DF_Suppliers_PhoneNumber] DEFAULT (N''),
    [Email]          NVARCHAR (256) NOT NULL CONSTRAINT [DF_Suppliers_Email] DEFAULT (N''),
    [SupplyCategory] NVARCHAR (80)  NOT NULL CONSTRAINT [DF_Suppliers_SupplyCategory] DEFAULT (N'Flour & Grains'),
    [Address]        NVARCHAR (300) NOT NULL CONSTRAINT [DF_Suppliers_Address] DEFAULT (N''),
    [IsActive]       BIT            NOT NULL CONSTRAINT [DF_Suppliers_IsActive] DEFAULT (1),
    [CreatedAt]      DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Suppliers_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]      DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Suppliers_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_Suppliers] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Suppliers_Name] UNIQUE ([Name])
);
GO

CREATE NONCLUSTERED INDEX [IX_Suppliers_SupplyCategory_IsActive]
    ON [dbo].[Suppliers] ([SupplyCategory] ASC, [IsActive] ASC);
GO
