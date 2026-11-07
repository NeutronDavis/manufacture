CREATE TABLE [dbo].[PayrollDeductionLines]
(
    [Id]              BIGINT         IDENTITY (1, 1) NOT NULL,
    [PayrollRecordId] INT            NOT NULL,
    [EmployeeId]      INT            NOT NULL,
    [DeductionType]   NVARCHAR (40)  NOT NULL,
    [Description]     NVARCHAR (300) NOT NULL,
    [Amount]          DECIMAL (18, 2) NOT NULL,
    [SalaryAdvanceId] INT            NULL,
    [CreatedAt]       DATETIME2 (3)  NOT NULL CONSTRAINT [DF_PayrollDeductionLines_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_PayrollDeductionLines] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PayrollDeductionLines_PayrollRecords] FOREIGN KEY ([PayrollRecordId]) REFERENCES [dbo].[PayrollRecords] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PayrollDeductionLines_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees] ([Id]),
    CONSTRAINT [FK_PayrollDeductionLines_SalaryAdvances] FOREIGN KEY ([SalaryAdvanceId]) REFERENCES [dbo].[SalaryAdvances] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [CK_PayrollDeductionLines_Amount] CHECK ([Amount] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_PayrollDeductionLines_PayrollRecordId]
    ON [dbo].[PayrollDeductionLines] ([PayrollRecordId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_PayrollDeductionLines_EmployeeId_CreatedAt]
    ON [dbo].[PayrollDeductionLines] ([EmployeeId] ASC, [CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_PayrollDeductionLines_SalaryAdvanceId]
    ON [dbo].[PayrollDeductionLines] ([SalaryAdvanceId] ASC)
    WHERE [SalaryAdvanceId] IS NOT NULL;
GO
