CREATE TABLE [dbo].[PayrollRecords]
(
    [Id]                      INT            IDENTITY (1, 1) NOT NULL,
    [EmployeeId]              INT            NOT NULL,
    [PayrollMonth]            TINYINT        NOT NULL,
    [PayrollYear]             SMALLINT       NOT NULL,
    [BaseSalary]              DECIMAL (18, 2) NOT NULL,
    [PerformanceBonus]        DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_PayrollRecords_PerformanceBonus] DEFAULT (0),
    [DisciplinaryDeductions]  DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_PayrollRecords_DisciplinaryDeductions] DEFAULT (0),
    [AdvanceDeduction]        DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_PayrollRecords_AdvanceDeduction] DEFAULT (0),
    [TaxAndLevies]            DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_PayrollRecords_TaxAndLevies] DEFAULT (0),
    [GrossSalary]             AS ([BaseSalary] + [PerformanceBonus]) PERSISTED,
    [TotalDeductions]         AS ([DisciplinaryDeductions] + [AdvanceDeduction] + [TaxAndLevies]) PERSISTED,
    [NetSalary]               AS (([BaseSalary] + [PerformanceBonus]) - ([DisciplinaryDeductions] + [AdvanceDeduction] + [TaxAndLevies])) PERSISTED,
    [PaymentStatus]           NVARCHAR (20)  NOT NULL CONSTRAINT [DF_PayrollRecords_PaymentStatus] DEFAULT (N'Pending'),
    [PaymentDate]             DATETIME2 (3)  NULL,
    [Notes]                   NVARCHAR (400) NOT NULL CONSTRAINT [DF_PayrollRecords_Notes] DEFAULT (N''),
    [CreatedAt]               DATETIME2 (3)  NOT NULL CONSTRAINT [DF_PayrollRecords_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]               DATETIME2 (3)  NOT NULL CONSTRAINT [DF_PayrollRecords_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_PayrollRecords] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_PayrollRecords_Employee_Period] UNIQUE ([EmployeeId], [PayrollYear], [PayrollMonth]),
    CONSTRAINT [FK_PayrollRecords_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees] ([Id]),
    CONSTRAINT [CK_PayrollRecords_Month] CHECK ([PayrollMonth] BETWEEN 1 AND 12),
    CONSTRAINT [CK_PayrollRecords_Year] CHECK ([PayrollYear] >= 2000),
    CONSTRAINT [CK_PayrollRecords_Amounts] CHECK ([BaseSalary] >= 0 AND [PerformanceBonus] >= 0 AND [DisciplinaryDeductions] >= 0 AND [AdvanceDeduction] >= 0 AND [TaxAndLevies] >= 0),
    CONSTRAINT [CK_PayrollRecords_PaymentStatus] CHECK ([PaymentStatus] IN (N'Pending', N'Paid', N'OnHold'))
);
GO

CREATE NONCLUSTERED INDEX [IX_PayrollRecords_Period_Status]
    ON [dbo].[PayrollRecords] ([PayrollYear] DESC, [PayrollMonth] DESC, [PaymentStatus] ASC);
GO
