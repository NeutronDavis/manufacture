/*
    Baseline configuration rows.

    Keep this file idempotent: it is executed every time the development
    database is redeployed.
*/

IF NOT EXISTS (SELECT 1 FROM [dbo].[TreasuryPolicySettings] WHERE [Id] = 1)
BEGIN
    INSERT INTO [dbo].[TreasuryPolicySettings]
    (
        [Id],
        [PlannedCashPercent],
        [AlertTolerancePercent],
        [PolicyMemo],
        [LastUpdatedUtc],
        [UpdatedBy]
    )
    VALUES
    (
        1,
        32.00,
        6.00,
        N'Standard retail benchmark: 32% expected in physical cash; 68% routed via bank transfer and POS terminals to limit route cash-in-transit risk.',
        SYSUTCDATETIME(),
        N'SuperAdmin'
    );
END;
GO
