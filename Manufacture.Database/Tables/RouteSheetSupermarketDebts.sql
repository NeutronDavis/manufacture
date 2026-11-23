CREATE TABLE [dbo].[RouteSheetSupermarketDebts]
(
    [Id]                   INT            IDENTITY (1, 1) NOT NULL,
    [RouteSheetId]         INT            NOT NULL,
    [SupermarketAccountId] INT            NOT NULL,
    [IsSettled]            BIT            NOT NULL CONSTRAINT [DF_RouteSheetSupermarketDebts_IsSettled] DEFAULT (0),
    [DebtAmount]           DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_RouteSheetSupermarketDebts_DebtAmount] DEFAULT (0),
    [AppliedCredit]        DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_RouteSheetSupermarketDebts_AppliedCredit] DEFAULT (0),
    [RecordedBy]           NVARCHAR (150) NOT NULL CONSTRAINT [DF_RouteSheetSupermarketDebts_RecordedBy] DEFAULT (N''),
    [RecordedAt]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RouteSheetSupermarketDebts_RecordedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]            DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RouteSheetSupermarketDebts_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_RouteSheetSupermarketDebts] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RouteSheetSupermarketDebts_RouteSheet_Account] UNIQUE ([RouteSheetId], [SupermarketAccountId]),
    CONSTRAINT [FK_RouteSheetSupermarketDebts_DailyRepRouteSheets] FOREIGN KEY ([RouteSheetId]) REFERENCES [dbo].[DailyRepRouteSheets] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RouteSheetSupermarketDebts_SupermarketAccounts] FOREIGN KEY ([SupermarketAccountId]) REFERENCES [dbo].[SupermarketAccounts] ([Id]),
    CONSTRAINT [CK_RouteSheetSupermarketDebts_Amounts] CHECK ([DebtAmount] >= 0 AND [AppliedCredit] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_RouteSheetSupermarketDebts_SupermarketAccountId]
    ON [dbo].[RouteSheetSupermarketDebts] ([SupermarketAccountId] ASC);
GO
