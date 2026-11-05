CREATE TABLE [dbo].[InventoryItems]
(
    [Id]                  INT            IDENTITY (1, 1) NOT NULL,
    [ItemCode]            NVARCHAR (40)  NOT NULL,
    [Name]                NVARCHAR (150) NOT NULL,
    [Category]            NVARCHAR (60)  NOT NULL CONSTRAINT [DF_InventoryItems_Category] DEFAULT (N'Raw Materials'),
    [Unit]                NVARCHAR (40)  NOT NULL CONSTRAINT [DF_InventoryItems_Unit] DEFAULT (N'Bags (50kg)'),
    [QuantityInStock]     DECIMAL (18, 3) NOT NULL CONSTRAINT [DF_InventoryItems_QuantityInStock] DEFAULT (0),
    [ReorderThreshold]    DECIMAL (18, 3) NOT NULL CONSTRAINT [DF_InventoryItems_ReorderThreshold] DEFAULT (0),
    [UnitCostPrice]       DECIMAL (18, 4) NOT NULL CONSTRAINT [DF_InventoryItems_UnitCostPrice] DEFAULT (0),
    [PreferredSupplierId] INT            NULL,
    [LastRestockedDate]   DATE           NOT NULL CONSTRAINT [DF_InventoryItems_LastRestockedDate] DEFAULT (CONVERT(DATE, SYSUTCDATETIME())),
    [IsActive]            BIT            NOT NULL CONSTRAINT [DF_InventoryItems_IsActive] DEFAULT (1),
    [CreatedAt]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_InventoryItems_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_InventoryItems_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_InventoryItems] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_InventoryItems_ItemCode] UNIQUE ([ItemCode]),
    CONSTRAINT [FK_InventoryItems_Suppliers] FOREIGN KEY ([PreferredSupplierId]) REFERENCES [dbo].[Suppliers] ([Id]),
    CONSTRAINT [CK_InventoryItems_Quantities] CHECK ([QuantityInStock] >= 0 AND [ReorderThreshold] >= 0 AND [UnitCostPrice] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_InventoryItems_Category_IsActive]
    ON [dbo].[InventoryItems] ([Category] ASC, [IsActive] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_InventoryItems_PreferredSupplierId]
    ON [dbo].[InventoryItems] ([PreferredSupplierId] ASC)
    WHERE [PreferredSupplierId] IS NOT NULL;
GO
