CREATE TABLE [dbo].[DailyRepRouteSheets]
(
    [Id]                            INT            IDENTITY (1, 1) NOT NULL,
    [RouteSheetRef]                 NVARCHAR (40)  NOT NULL,
    [RouteDate]                     DATE           NOT NULL,
    [SalesRepId]                    INT            NOT NULL,
    [RouteName]                     NVARCHAR (120) NOT NULL,
    [VehicleId]                     INT            NULL,
    [VehicleRegistration]           NVARCHAR (20)  NULL,
    [PosTerminalId]                 INT            NULL,
    [PosTerminalCode]               NVARCHAR (20)  NULL,
    [PreviousAccountBalance]        DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_DailyRepRouteSheets_PreviousAccountBalance] DEFAULT (0),
    [TotalExpectedSalesRevenue]     DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_DailyRepRouteSheets_TotalExpectedSalesRevenue] DEFAULT (0),
    [CashCollected]                 DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_DailyRepRouteSheets_CashCollected] DEFAULT (0),
    [BankTransferCollected]         DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_DailyRepRouteSheets_BankTransferCollected] DEFAULT (0),
    [PosTerminalCollected]          DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_DailyRepRouteSheets_PosTerminalCollected] DEFAULT (0),
    [TotalPaymentsCollectedToday]   AS ([CashCollected] + [BankTransferCollected] + [PosTerminalCollected]) PERSISTED,
    [NewOutstandingAccountBalance]  AS (([PreviousAccountBalance] + [TotalExpectedSalesRevenue]) - ([CashCollected] + [BankTransferCollected] + [PosTerminalCollected])) PERSISTED,
    [Status]                        INT            NOT NULL CONSTRAINT [DF_DailyRepRouteSheets_Status] DEFAULT (0),
    [SignedOffByDispatcher]         NVARCHAR (150) NULL,
    [SignedOffAt]                   DATETIME2 (3)  NULL,
    [ReconciledBy]                  NVARCHAR (150) NULL,
    [ReconciledAt]                  DATETIME2 (3)  NULL,
    [Notes]                         NVARCHAR (500) NOT NULL CONSTRAINT [DF_DailyRepRouteSheets_Notes] DEFAULT (N''),
    [CreatedAt]                     DATETIME2 (3)  NOT NULL CONSTRAINT [DF_DailyRepRouteSheets_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]                     DATETIME2 (3)  NOT NULL CONSTRAINT [DF_DailyRepRouteSheets_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]                    ROWVERSION     NOT NULL,

    CONSTRAINT [PK_DailyRepRouteSheets] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_DailyRepRouteSheets_Rep_Date] UNIQUE ([SalesRepId], [RouteDate]),
    CONSTRAINT [UQ_DailyRepRouteSheets_Ref] UNIQUE ([RouteSheetRef]),
    CONSTRAINT [FK_DailyRepRouteSheets_SalesReps] FOREIGN KEY ([SalesRepId]) REFERENCES [dbo].[SalesReps] ([Id]),
    CONSTRAINT [FK_DailyRepRouteSheets_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]),
    CONSTRAINT [FK_DailyRepRouteSheets_PosTerminals] FOREIGN KEY ([PosTerminalId]) REFERENCES [dbo].[PosTerminals] ([Id]),
    CONSTRAINT [CK_DailyRepRouteSheets_Status] CHECK ([Status] IN (0, 1, 2, 3)),
    CONSTRAINT [CK_DailyRepRouteSheets_Amounts] CHECK ([PreviousAccountBalance] >= 0 AND [TotalExpectedSalesRevenue] >= 0 AND [CashCollected] >= 0 AND [BankTransferCollected] >= 0 AND [PosTerminalCollected] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_DailyRepRouteSheets_RouteDate_Status]
    ON [dbo].[DailyRepRouteSheets] ([RouteDate] DESC, [Status] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_DailyRepRouteSheets_VehicleId_RouteDate]
    ON [dbo].[DailyRepRouteSheets] ([VehicleId] ASC, [RouteDate] DESC)
    WHERE [VehicleId] IS NOT NULL;
GO
