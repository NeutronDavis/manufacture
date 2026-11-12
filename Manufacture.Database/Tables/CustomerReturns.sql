CREATE TABLE [dbo].[CustomerReturns]
(
    [Id]                 INT            IDENTITY (1, 1) NOT NULL,
    [ReturnNumber]       NVARCHAR (40)  NOT NULL,
    [OrderId]            INT            NULL,
    [CustomerId]         INT            NULL,
    [RecipeId]           INT            NOT NULL,
    [SalesRepId]         INT            NULL,
    [Quantity]           INT            NOT NULL,
    [UnitPrice]          DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_CustomerReturns_UnitPrice] DEFAULT (0),
    [RefundAmount]       AS ([Quantity] * [UnitPrice]) PERSISTED,
    [ReplacementQuantity] INT           NOT NULL CONSTRAINT [DF_CustomerReturns_ReplacementQuantity] DEFAULT (0),
    [ReturnDate]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_CustomerReturns_ReturnDate] DEFAULT (SYSUTCDATETIME()),
    [Reason]             NVARCHAR (250) NOT NULL CONSTRAINT [DF_CustomerReturns_Reason] DEFAULT (N''),
    [ReplacementIssued]  BIT            NOT NULL CONSTRAINT [DF_CustomerReturns_ReplacementIssued] DEFAULT (0),
    [Notes]              NVARCHAR (400) NOT NULL CONSTRAINT [DF_CustomerReturns_Notes] DEFAULT (N''),
    [RecordedBy]         NVARCHAR (150) NOT NULL CONSTRAINT [DF_CustomerReturns_RecordedBy] DEFAULT (N''),
    [CreatedAt]          DATETIME2 (3)  NOT NULL CONSTRAINT [DF_CustomerReturns_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_CustomerReturns] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_CustomerReturns_ReturnNumber] UNIQUE ([ReturnNumber]),
    CONSTRAINT [FK_CustomerReturns_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]),
    CONSTRAINT [FK_CustomerReturns_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
    CONSTRAINT [FK_CustomerReturns_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]),
    CONSTRAINT [FK_CustomerReturns_SalesReps] FOREIGN KEY ([SalesRepId]) REFERENCES [dbo].[SalesReps] ([Id]),
    CONSTRAINT [CK_CustomerReturns_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [CK_CustomerReturns_ReplacementQuantity] CHECK ([ReplacementQuantity] >= 0),
    CONSTRAINT [CK_CustomerReturns_UnitPrice] CHECK ([UnitPrice] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_CustomerReturns_RecipeId_ReturnDate]
    ON [dbo].[CustomerReturns] ([RecipeId] ASC, [ReturnDate] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_CustomerReturns_SalesRepId_ReturnDate]
    ON [dbo].[CustomerReturns] ([SalesRepId] ASC, [ReturnDate] DESC)
    WHERE [SalesRepId] IS NOT NULL;
GO
