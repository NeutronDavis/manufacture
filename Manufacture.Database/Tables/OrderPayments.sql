CREATE TABLE [dbo].[OrderPayments]
(
    [Id]            INT            IDENTITY (1, 1) NOT NULL,
    [OrderId]       INT            NOT NULL,
    [Amount]        DECIMAL (18, 2) NOT NULL,
    [PaymentMethod] INT            NOT NULL CONSTRAINT [DF_OrderPayments_PaymentMethod] DEFAULT (0),
    [PaymentDate]   DATETIME2 (3)  NOT NULL CONSTRAINT [DF_OrderPayments_PaymentDate] DEFAULT (SYSUTCDATETIME()),
    [Reference]     NVARCHAR (80)  NOT NULL CONSTRAINT [DF_OrderPayments_Reference] DEFAULT (N''),
    [Notes]         NVARCHAR (300) NOT NULL CONSTRAINT [DF_OrderPayments_Notes] DEFAULT (N''),
    [ReceivedBy]    NVARCHAR (150) NOT NULL CONSTRAINT [DF_OrderPayments_ReceivedBy] DEFAULT (N''),
    [CreatedAt]     DATETIME2 (3)  NOT NULL CONSTRAINT [DF_OrderPayments_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_OrderPayments] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_OrderPayments_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]),
    CONSTRAINT [CK_OrderPayments_Amount] CHECK ([Amount] > 0),
    CONSTRAINT [CK_OrderPayments_Method] CHECK ([PaymentMethod] IN (0, 1, 2, 3))
);
GO

CREATE NONCLUSTERED INDEX [IX_OrderPayments_OrderId_PaymentDate]
    ON [dbo].[OrderPayments] ([OrderId] ASC, [PaymentDate] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_OrderPayments_PaymentDate]
    ON [dbo].[OrderPayments] ([PaymentDate] DESC);
GO
