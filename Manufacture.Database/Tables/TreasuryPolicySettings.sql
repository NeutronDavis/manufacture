CREATE TABLE [dbo].[TreasuryPolicySettings]
(
    [Id]                     TINYINT        NOT NULL,
    [PlannedCashPercent]     DECIMAL (5, 2) NOT NULL CONSTRAINT [DF_TreasuryPolicySettings_PlannedCashPercent] DEFAULT (32.00),
    [AlertTolerancePercent]  DECIMAL (5, 2) NOT NULL CONSTRAINT [DF_TreasuryPolicySettings_AlertTolerancePercent] DEFAULT (6.00),
    [PolicyMemo]             NVARCHAR (500) NOT NULL CONSTRAINT [DF_TreasuryPolicySettings_PolicyMemo] DEFAULT (N''),
    [LastUpdatedUtc]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_TreasuryPolicySettings_LastUpdatedUtc] DEFAULT (SYSUTCDATETIME()),
    [UpdatedBy]              NVARCHAR (150) NOT NULL CONSTRAINT [DF_TreasuryPolicySettings_UpdatedBy] DEFAULT (N'SuperAdmin'),

    CONSTRAINT [PK_TreasuryPolicySettings] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [CK_TreasuryPolicySettings_SingleRow] CHECK ([Id] = 1),
    CONSTRAINT [CK_TreasuryPolicySettings_Percentages] CHECK ([PlannedCashPercent] BETWEEN 0 AND 100 AND [AlertTolerancePercent] BETWEEN 0 AND 100)
);
GO
