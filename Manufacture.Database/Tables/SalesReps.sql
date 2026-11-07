CREATE TABLE [dbo].[SalesReps]
(
    [Id]              INT            IDENTITY (1, 1) NOT NULL,
    [RepCode]         NVARCHAR (20)  NOT NULL,
    [UserId]          INT            NULL,
    [FullName]        NVARCHAR (150) NOT NULL,
    [PhoneNumber]     NVARCHAR (30)  NOT NULL CONSTRAINT [DF_SalesReps_PhoneNumber] DEFAULT (N''),
    [RouteName]       NVARCHAR (120) NOT NULL,
    [PosTerminalId]   INT            NULL,
    [VehicleId]       INT            NULL,
    [ProductFocus]    NVARCHAR (120) NOT NULL CONSTRAINT [DF_SalesReps_ProductFocus] DEFAULT (N''),
    [IsActive]        BIT            NOT NULL CONSTRAINT [DF_SalesReps_IsActive] DEFAULT (1),
    [CreatedAt]       DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SalesReps_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]       DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SalesReps_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]      ROWVERSION     NOT NULL,

    CONSTRAINT [PK_SalesReps] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SalesReps_RepCode] UNIQUE ([RepCode]),
    CONSTRAINT [FK_SalesReps_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_SalesReps_PosTerminals] FOREIGN KEY ([PosTerminalId]) REFERENCES [dbo].[PosTerminals] ([Id]),
    CONSTRAINT [FK_SalesReps_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id])
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_SalesReps_UserId]
    ON [dbo].[SalesReps] ([UserId] ASC)
    WHERE [UserId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_SalesReps_RouteName_IsActive]
    ON [dbo].[SalesReps] ([RouteName] ASC, [IsActive] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_SalesReps_VehicleId]
    ON [dbo].[SalesReps] ([VehicleId] ASC)
    WHERE [VehicleId] IS NOT NULL;
GO
