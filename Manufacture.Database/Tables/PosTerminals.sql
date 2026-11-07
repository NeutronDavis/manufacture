CREATE TABLE [dbo].[PosTerminals]
(
    [Id]                     INT            IDENTITY (1, 1) NOT NULL,
    [TerminalCode]           NVARCHAR (20)  NOT NULL,
    [TerminalName]           NVARCHAR (100) NOT NULL,
    [AssignedUserId]         INT            NULL,
    [CashAccountName]        NVARCHAR (100) NOT NULL CONSTRAINT [DF_PosTerminals_CashAccountName] DEFAULT (N'Bakery Cash Drawer'),
    [TransferAccountName]    NVARCHAR (120) NOT NULL CONSTRAINT [DF_PosTerminals_TransferAccountName] DEFAULT (N'FirstBank - 3021948291'),
    [PosBankAccountName]     NVARCHAR (120) NOT NULL CONSTRAINT [DF_PosTerminals_PosBankAccountName] DEFAULT (N'Moniepoint POS - 9028129'),
    [IsActive]               BIT            NOT NULL CONSTRAINT [DF_PosTerminals_IsActive] DEFAULT (1),
    [CreatedAt]              DATETIME2 (3)  NOT NULL CONSTRAINT [DF_PosTerminals_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]              DATETIME2 (3)  NOT NULL CONSTRAINT [DF_PosTerminals_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_PosTerminals] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_PosTerminals_TerminalCode] UNIQUE ([TerminalCode]),
    CONSTRAINT [FK_PosTerminals_Users] FOREIGN KEY ([AssignedUserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE SET NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_PosTerminals_AssignedUserId]
    ON [dbo].[PosTerminals] ([AssignedUserId] ASC)
    WHERE [AssignedUserId] IS NOT NULL;
GO
