CREATE TABLE [dbo].[RouteSheetItems]
(
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [RouteSheetId]          INT            NOT NULL,
    [RecipeId]              INT            NOT NULL,
    [ProductCode]           NVARCHAR (40)  NOT NULL,
    [ProductName]           NVARCHAR (150) NOT NULL,
    [Category]              INT            NOT NULL CONSTRAINT [DF_RouteSheetItems_Category] DEFAULT (0),
    [UnitSellingPrice]      DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_RouteSheetItems_UnitSellingPrice] DEFAULT (0),
    [RepOrderRequestedQty]  INT            NOT NULL CONSTRAINT [DF_RouteSheetItems_RepOrderRequestedQty] DEFAULT (0),
    [YesterdayClosingStock] INT            NOT NULL CONSTRAINT [DF_RouteSheetItems_YesterdayClosingStock] DEFAULT (0),
    [TodayNewlyLoadedQty]   INT            NOT NULL CONSTRAINT [DF_RouteSheetItems_TodayNewlyLoadedQty] DEFAULT (0),
    [TotalActiveInventory]  AS ([YesterdayClosingStock] + [TodayNewlyLoadedQty]) PERSISTED,
    [QtySold]               INT            NOT NULL CONSTRAINT [DF_RouteSheetItems_QtySold] DEFAULT (0),
    [ClosingStockQty]       INT            NOT NULL CONSTRAINT [DF_RouteSheetItems_ClosingStockQty] DEFAULT (0),
    [QtyDamaged]            INT            NOT NULL CONSTRAINT [DF_RouteSheetItems_QtyDamaged] DEFAULT (0),
    [ExpectedItemRevenue]   AS ([QtySold] * [UnitSellingPrice]) PERSISTED,
    [CreatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RouteSheetItems_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RouteSheetItems_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_RouteSheetItems] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RouteSheetItems_RouteSheet_Recipe] UNIQUE ([RouteSheetId], [RecipeId]),
    CONSTRAINT [FK_RouteSheetItems_DailyRepRouteSheets] FOREIGN KEY ([RouteSheetId]) REFERENCES [dbo].[DailyRepRouteSheets] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RouteSheetItems_Recipes] FOREIGN KEY ([RecipeId]) REFERENCES [dbo].[Recipes] ([Id]),
    CONSTRAINT [CK_RouteSheetItems_Category] CHECK ([Category] IN (0, 1, 2)),
    CONSTRAINT [CK_RouteSheetItems_Quantities] CHECK ([RepOrderRequestedQty] >= 0 AND [YesterdayClosingStock] >= 0 AND [TodayNewlyLoadedQty] >= 0 AND [QtySold] >= 0 AND [ClosingStockQty] >= 0 AND [QtyDamaged] >= 0),
    CONSTRAINT [CK_RouteSheetItems_UnitSellingPrice] CHECK ([UnitSellingPrice] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_RouteSheetItems_RecipeId]
    ON [dbo].[RouteSheetItems] ([RecipeId] ASC);
GO
