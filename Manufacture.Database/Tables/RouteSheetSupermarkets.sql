CREATE TABLE [dbo].[RouteSheetSupermarkets]
(
    [Id]                   INT           IDENTITY (1, 1) NOT NULL,
    [RouteSheetId]         INT           NOT NULL,
    [SupermarketAccountId] INT           NOT NULL,
    [CreatedAt]            DATETIME2 (3) NOT NULL CONSTRAINT [DF_RouteSheetSupermarkets_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_RouteSheetSupermarkets] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RouteSheetSupermarkets_RouteSheet_Account] UNIQUE ([RouteSheetId], [SupermarketAccountId]),
    CONSTRAINT [FK_RouteSheetSupermarkets_DailyRepRouteSheets] FOREIGN KEY ([RouteSheetId]) REFERENCES [dbo].[DailyRepRouteSheets] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RouteSheetSupermarkets_SupermarketAccounts] FOREIGN KEY ([SupermarketAccountId]) REFERENCES [dbo].[SupermarketAccounts] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_RouteSheetSupermarkets_SupermarketAccountId]
    ON [dbo].[RouteSheetSupermarkets] ([SupermarketAccountId] ASC);
GO
