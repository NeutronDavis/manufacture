CREATE TABLE [dbo].[ProductionShifts]
(
    [Id]         INT           IDENTITY (1, 1) NOT NULL,
    [Name]       NVARCHAR (80) NOT NULL,
    [StartTime]  TIME (0)      NULL,
    [EndTime]    TIME (0)      NULL,
    [IsActive]   BIT           NOT NULL CONSTRAINT [DF_ProductionShifts_IsActive] DEFAULT (1),
    [CreatedAt]  DATETIME2 (3) NOT NULL CONSTRAINT [DF_ProductionShifts_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]  DATETIME2 (3) NOT NULL CONSTRAINT [DF_ProductionShifts_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION    NOT NULL,

    CONSTRAINT [PK_ProductionShifts] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProductionShifts_Name] UNIQUE ([Name])
);
GO

CREATE NONCLUSTERED INDEX [IX_ProductionShifts_IsActive]
    ON [dbo].[ProductionShifts] ([IsActive] ASC);
GO
