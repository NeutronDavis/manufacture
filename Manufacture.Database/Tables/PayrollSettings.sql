CREATE TABLE [dbo].[PayrollSettings]
(
    [Id]                          TINYINT        NOT NULL,
    [LoanMonthlyRepaymentPercent] DECIMAL (5, 2) NOT NULL CONSTRAINT [DF_PayrollSettings_LoanMonthlyRepaymentPercent] DEFAULT (20.00),
    [MaxLoanPercentOfSalary]      DECIMAL (5, 2) NOT NULL CONSTRAINT [DF_PayrollSettings_MaxLoanPercentOfSalary] DEFAULT (50.00),
    [HousingAllowancePercent]     DECIMAL (5, 2) NOT NULL CONSTRAINT [DF_PayrollSettings_HousingAllowancePercent] DEFAULT (12.50),
    [TransportAllowancePercent]   DECIMAL (5, 2) NOT NULL CONSTRAINT [DF_PayrollSettings_TransportAllowancePercent] DEFAULT (6.25),
    [PayeTaxPercent]              DECIMAL (5, 2) NOT NULL CONSTRAINT [DF_PayrollSettings_PayeTaxPercent] DEFAULT (3.50),
    [PensionPercent]              DECIMAL (5, 2) NOT NULL CONSTRAINT [DF_PayrollSettings_PensionPercent] DEFAULT (5.00),
    [HealthInsurancePercent]      DECIMAL (5, 2) NOT NULL CONSTRAINT [DF_PayrollSettings_HealthInsurancePercent] DEFAULT (2.65),
    [UpdatedAt]                   DATETIME2 (3)  NOT NULL CONSTRAINT [DF_PayrollSettings_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedBy]                   NVARCHAR (150) NOT NULL CONSTRAINT [DF_PayrollSettings_UpdatedBy] DEFAULT (N'SuperAdmin'),

    CONSTRAINT [PK_PayrollSettings] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [CK_PayrollSettings_SingleRow] CHECK ([Id] = 1),
    CONSTRAINT [CK_PayrollSettings_Percentages] CHECK (
        [LoanMonthlyRepaymentPercent] BETWEEN 0 AND 100 AND
        [MaxLoanPercentOfSalary] BETWEEN 0 AND 100 AND
        [HousingAllowancePercent] BETWEEN 0 AND 100 AND
        [TransportAllowancePercent] BETWEEN 0 AND 100 AND
        [PayeTaxPercent] BETWEEN 0 AND 100 AND
        [PensionPercent] BETWEEN 0 AND 100 AND
        [HealthInsurancePercent] BETWEEN 0 AND 100)
);
GO
