CREATE TABLE [dbo].[Customers]
(
    [Id]                  INT            IDENTITY (1, 1) NOT NULL,
    [Name]                NVARCHAR (150) NOT NULL,
    [MemberCode]          NVARCHAR (40)  NOT NULL,
    [Category]            INT            NOT NULL CONSTRAINT [DF_Customers_Category] DEFAULT (0),
    [PhoneNumber]         NVARCHAR (30)  NOT NULL CONSTRAINT [DF_Customers_PhoneNumber] DEFAULT (N''),
    [Address]             NVARCHAR (300) NOT NULL CONSTRAINT [DF_Customers_Address] DEFAULT (N''),
    [OutstandingBalance]  DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_Customers_OutstandingBalance] DEFAULT (0),
    [IsActive]            BIT            NOT NULL CONSTRAINT [DF_Customers_IsActive] DEFAULT (1),
    [CreatedAt]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Customers_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Customers_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]          ROWVERSION     NOT NULL,

    CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Customers_MemberCode] UNIQUE ([MemberCode]),
    CONSTRAINT [CK_Customers_Category] CHECK ([Category] IN (0, 1, 2, 3)),
    CONSTRAINT [CK_Customers_OutstandingBalance] CHECK ([OutstandingBalance] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_Customers_Category_IsActive]
    ON [dbo].[Customers] ([Category] ASC, [IsActive] ASC);
GO
