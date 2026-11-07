CREATE TABLE [dbo].[PayrollRuns]
(
    [Id]              INT            IDENTITY (1, 1) NOT NULL,
    [RunNumber]       NVARCHAR (40)  NOT NULL,
    [PayrollMonth]    TINYINT        NOT NULL,
    [PayrollYear]     SMALLINT       NOT NULL,
    [GeneratedAt]     DATETIME2 (3)  NOT NULL CONSTRAINT [DF_PayrollRuns_GeneratedAt] DEFAULT (SYSUTCDATETIME()),
    [GeneratedBy]     NVARCHAR (150) NOT NULL CONSTRAINT [DF_PayrollRuns_GeneratedBy] DEFAULT (N'System'),
    [EmployeeCount]   INT            NOT NULL CONSTRAINT [DF_PayrollRuns_EmployeeCount] DEFAULT (0),
    [GrossTotal]      DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_PayrollRuns_GrossTotal] DEFAULT (0),
    [DeductionsTotal] DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_PayrollRuns_DeductionsTotal] DEFAULT (0),
    [NetTotal]        DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_PayrollRuns_NetTotal] DEFAULT (0),
    [Status]          NVARCHAR (20)  NOT NULL CONSTRAINT [DF_PayrollRuns_Status] DEFAULT (N'Pending'),
    [Notes]           NVARCHAR (400) NOT NULL CONSTRAINT [DF_PayrollRuns_Notes] DEFAULT (N''),
    [CreatedAt]       DATETIME2 (3)  NOT NULL CONSTRAINT [DF_PayrollRuns_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]       DATETIME2 (3)  NOT NULL CONSTRAINT [DF_PayrollRuns_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_PayrollRuns] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_PayrollRuns_RunNumber] UNIQUE ([RunNumber]),
    CONSTRAINT [UQ_PayrollRuns_Period] UNIQUE ([PayrollYear], [PayrollMonth]),
    CONSTRAINT [CK_PayrollRuns_Month] CHECK ([PayrollMonth] BETWEEN 1 AND 12),
    CONSTRAINT [CK_PayrollRuns_Year] CHECK ([PayrollYear] >= 2000),
    CONSTRAINT [CK_PayrollRuns_Status] CHECK ([Status] IN (N'Pending', N'PartiallyPaid', N'Paid')),
    CONSTRAINT [CK_PayrollRuns_Amounts] CHECK ([GrossTotal] >= 0 AND [DeductionsTotal] >= 0 AND [NetTotal] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_PayrollRuns_GeneratedAt]
    ON [dbo].[PayrollRuns] ([GeneratedAt] DESC);
GO
