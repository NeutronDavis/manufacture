CREATE TABLE [dbo].[SupermarketAccountPrices]
(
    [Id]                  INT            IDENTITY (1, 1) NOT NULL,
    [SupermarketAccountId] INT           NOT NULL,
    [RecipeId]            INT            NOT NULL,
    [AgreedPrice]         DECIMAL (18, 2) NOT NULL,
    [CreatedAt]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SupermarketAccountPrices_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SupermarketAccountPrices_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_SupermarketAccountPrices] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SupermarketAccountPrices_Account_Recipe] UNIQUE ([SupermarketAccountId], [RecipeId]),
    CONSTRAINT [FK_SupermarketAccountPrices_SupermarketAccounts] FOREIGN KEY ([SupermarketAccountId]) REFERENCES [dbo].[SupermarketAccounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SupermarketAccountPrices_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]),
    CONSTRAINT [CK_SupermarketAccountPrices_AgreedPrice] CHECK ([AgreedPrice] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_SupermarketAccountPrices_RecipeId]
    ON [dbo].[SupermarketAccountPrices] ([RecipeId] ASC);
GO
