CREATE TABLE [dbo].[Orders]
(
    [Id]                  INT            IDENTITY (1, 1) NOT NULL,
    [OrderNumber]         NVARCHAR (40)  NOT NULL,
    [OrderType]           INT            NOT NULL CONSTRAINT [DF_Orders_OrderType] DEFAULT (0),
    [CustomerId]          INT            NOT NULL,
    [CustomerName]        NVARCHAR (150) NOT NULL,
    [CustomerCategory]    INT            NOT NULL CONSTRAINT [DF_Orders_CustomerCategory] DEFAULT (0),
    [SalesRepId]          INT            NULL,
    [SalesRepName]        NVARCHAR (150) NULL,
    [PosTerminalId]       INT            NULL,
    [PosTerminalCode]     NVARCHAR (20)  NULL,
    [SubTotal]            DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_Orders_SubTotal] DEFAULT (0),
    [Discount]            DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_Orders_Discount] DEFAULT (0),
    [TotalAmount]         AS ([SubTotal] - [Discount]) PERSISTED,
    [AmountPaid]          DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_Orders_AmountPaid] DEFAULT (0),
    [BalanceDue]          AS (([SubTotal] - [Discount]) - [AmountPaid]) PERSISTED,
    [PaymentMethod]       INT            NOT NULL CONSTRAINT [DF_Orders_PaymentMethod] DEFAULT (0),
    [Status]              INT            NOT NULL CONSTRAINT [DF_Orders_Status] DEFAULT (0),
    [OrderDate]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Orders_OrderDate] DEFAULT (SYSUTCDATETIME()),
    [TargetDeliveryDate]  DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Orders_TargetDeliveryDate] DEFAULT (SYSUTCDATETIME()),
    [IsPrintedForLoading] BIT            NOT NULL CONSTRAINT [DF_Orders_IsPrintedForLoading] DEFAULT (0),
    [PrintedAt]           DATETIME2 (3)  NULL,
    [VerifiedByLoaderName] NVARCHAR (150) NULL,
    [LoadedAt]            DATETIME2 (3)  NULL,
    [Notes]               NVARCHAR (500) NOT NULL CONSTRAINT [DF_Orders_Notes] DEFAULT (N''),
    [CreatedAt]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Orders_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Orders_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]          ROWVERSION     NOT NULL,

    CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Orders_OrderNumber] UNIQUE ([OrderNumber]),
    CONSTRAINT [FK_Orders_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
    CONSTRAINT [FK_Orders_SalesReps] FOREIGN KEY ([SalesRepId]) REFERENCES [dbo].[SalesReps] ([Id]),
    CONSTRAINT [FK_Orders_PosTerminals] FOREIGN KEY ([PosTerminalId]) REFERENCES [dbo].[PosTerminals] ([Id]),
    CONSTRAINT [CK_Orders_OrderType] CHECK ([OrderType] IN (0, 1)),
    CONSTRAINT [CK_Orders_CustomerCategory] CHECK ([CustomerCategory] IN (0, 1, 2, 3)),
    CONSTRAINT [CK_Orders_PaymentMethod] CHECK ([PaymentMethod] IN (0, 1, 2, 3)),
    CONSTRAINT [CK_Orders_Status] CHECK ([Status] IN (0, 1, 2, 3, 4, 5)),
    CONSTRAINT [CK_Orders_Amounts] CHECK ([SubTotal] >= 0 AND [Discount] >= 0 AND [Discount] <= [SubTotal] AND [AmountPaid] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_Orders_OrderDate]
    ON [dbo].[Orders] ([OrderDate] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_Orders_CustomerId_OrderDate]
    ON [dbo].[Orders] ([CustomerId] ASC, [OrderDate] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_Orders_SalesRepId_OrderDate]
    ON [dbo].[Orders] ([SalesRepId] ASC, [OrderDate] DESC)
    WHERE [SalesRepId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Orders_Status_TargetDeliveryDate]
    ON [dbo].[Orders] ([Status] ASC, [TargetDeliveryDate] ASC);
GO
