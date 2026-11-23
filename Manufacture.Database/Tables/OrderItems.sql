CREATE TABLE [dbo].[OrderItems]
(
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [OrderId]     INT            NOT NULL,
    [RecipeId]    INT            NOT NULL,
    [ProductName]   NVARCHAR (150) NOT NULL,
    [Quantity]      INT            NOT NULL,
    [BonusQuantity] INT            NOT NULL CONSTRAINT [DF_OrderItems_BonusQuantity] DEFAULT (0),
    [UnitPrice]     DECIMAL (18, 2) NOT NULL,
    [TotalQuantity] AS ([Quantity] + [BonusQuantity]) PERSISTED,
    [LineTotal]     AS ([Quantity] * [UnitPrice]) PERSISTED,
    [CreatedAt]   DATETIME2 (3)  NOT NULL CONSTRAINT [DF_OrderItems_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]   DATETIME2 (3)  NOT NULL CONSTRAINT [DF_OrderItems_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_OrderItems] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_OrderItems_Order_Recipe] UNIQUE ([OrderId], [RecipeId]),
    CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]),
    CONSTRAINT [CK_OrderItems_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [CK_OrderItems_BonusQuantity] CHECK ([BonusQuantity] >= 0),
    CONSTRAINT [CK_OrderItems_UnitPrice] CHECK ([UnitPrice] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_OrderItems_RecipeId]
    ON [dbo].[OrderItems] ([RecipeId] ASC);
GO
