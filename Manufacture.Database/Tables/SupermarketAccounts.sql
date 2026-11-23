CREATE TABLE [dbo].[SupermarketAccounts]
(
    [Id]                      INT            IDENTITY (1, 1) NOT NULL,
    [CustomerId]              INT            NOT NULL,
    [AccountName]             NVARCHAR (150) NOT NULL,
    [ContactPerson]           NVARCHAR (150) NOT NULL CONSTRAINT [DF_SupermarketAccounts_ContactPerson] DEFAULT (N''),
    [PhoneNumber]             NVARCHAR (30)  NOT NULL CONSTRAINT [DF_SupermarketAccounts_PhoneNumber] DEFAULT (N''),
    [Address]                 NVARCHAR (300) NOT NULL CONSTRAINT [DF_SupermarketAccounts_Address] DEFAULT (N''),
    [AssignedRepId]           INT            NULL,
    [CurrentOutstandingCredit] DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_SupermarketAccounts_CurrentOutstandingCredit] DEFAULT (0),
    [CreditLimit]             DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_SupermarketAccounts_CreditLimit] DEFAULT (250000),
    [PaymentTerms]            NVARCHAR (60)  NOT NULL CONSTRAINT [DF_SupermarketAccounts_PaymentTerms] DEFAULT (N'Net 15 Days'),
    [IsActive]                BIT            NOT NULL CONSTRAINT [DF_SupermarketAccounts_IsActive] DEFAULT (1),
    [CreatedAt]               DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SupermarketAccounts_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]               DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SupermarketAccounts_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_SupermarketAccounts] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SupermarketAccounts_CustomerId] UNIQUE ([CustomerId]),
    CONSTRAINT [FK_SupermarketAccounts_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
    CONSTRAINT [FK_SupermarketAccounts_SalesReps] FOREIGN KEY ([AssignedRepId]) REFERENCES [dbo].[SalesReps] ([Id]),
    CONSTRAINT [CK_SupermarketAccounts_Credit] CHECK ([CurrentOutstandingCredit] >= 0 AND [CreditLimit] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_SupermarketAccounts_AssignedRepId]
    ON [dbo].[SupermarketAccounts] ([AssignedRepId] ASC)
    WHERE [AssignedRepId] IS NOT NULL;
GO
