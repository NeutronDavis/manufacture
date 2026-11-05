CREATE TABLE [dbo].[SalaryAdvances]
(
    [Id]                     INT            IDENTITY (1, 1) NOT NULL,
    [EmployeeId]             INT            NOT NULL,
    [PrincipalAmount]        DECIMAL (18, 2) NOT NULL,
    [MonthlyDeductionAmount] DECIMAL (18, 2) NOT NULL,
    [TotalRepaid]            DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_SalaryAdvances_TotalRepaid] DEFAULT (0),
    [RemainingBalance]       AS ([PrincipalAmount] - [TotalRepaid]) PERSISTED,
    [RequestDate]            DATE           NOT NULL CONSTRAINT [DF_SalaryAdvances_RequestDate] DEFAULT (CONVERT(DATE, SYSUTCDATETIME())),
    [Reason]                 NVARCHAR (300) NOT NULL CONSTRAINT [DF_SalaryAdvances_Reason] DEFAULT (N''),
    [Status]                 NVARCHAR (20)  NOT NULL CONSTRAINT [DF_SalaryAdvances_Status] DEFAULT (N'Active'),
    [CreatedAt]              DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SalaryAdvances_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]              DATETIME2 (3)  NOT NULL CONSTRAINT [DF_SalaryAdvances_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_SalaryAdvances] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SalaryAdvances_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees] ([Id]),
    CONSTRAINT [CK_SalaryAdvances_Amounts] CHECK ([PrincipalAmount] >= 0 AND [TotalRepaid] >= 0 AND [MonthlyDeductionAmount] > 0),
    CONSTRAINT [CK_SalaryAdvances_Status] CHECK ([Status] IN (N'Active', N'FullyRepaid', N'Cancelled'))
);
GO

CREATE NONCLUSTERED INDEX [IX_SalaryAdvances_EmployeeId_Status]
    ON [dbo].[SalaryAdvances] ([EmployeeId] ASC, [Status] ASC);
GO
