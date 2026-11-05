CREATE TABLE [dbo].[Users]
(
    [Id]                 INT              IDENTITY (1, 1) NOT NULL,
    [Name]               NVARCHAR (150)   NOT NULL,
    [Email]              NVARCHAR (256)   NOT NULL,
    [Role]               NVARCHAR (40)    NOT NULL CONSTRAINT [DF_Users_Role] DEFAULT (N'SalesRep'),
    [PinCodeHash]        NVARCHAR (256)   NOT NULL,
    [PasswordHash]       NVARCHAR (256)   NOT NULL,
    [SecurityStamp]      UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_Users_SecurityStamp] DEFAULT (NEWID()),
    [MustChangePassword] BIT              NOT NULL CONSTRAINT [DF_Users_MustChangePassword] DEFAULT (1),
    [PosTerminalCode]    NVARCHAR (20)    NULL,
    [PhoneNumber]        NVARCHAR (30)    NULL,
    [IsActive]           BIT              NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
    [LastLoginAt]        DATETIME2 (3)    NULL,
    [CreatedAt]          DATETIME2 (3)    NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]          DATETIME2 (3)    NOT NULL CONSTRAINT [DF_Users_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]         ROWVERSION       NOT NULL,

    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Users_Email] UNIQUE ([Email]),
    CONSTRAINT [CK_Users_Role] CHECK ([Role] IN (N'SuperAdmin', N'ProductionManager', N'StoreManager', N'SalesRep', N'Vendor', N'HrPayrollManager'))
);
GO

CREATE NONCLUSTERED INDEX [IX_Users_Role_IsActive]
    ON [dbo].[Users] ([Role] ASC, [IsActive] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Users_PosTerminalCode]
    ON [dbo].[Users] ([PosTerminalCode] ASC)
    WHERE [PosTerminalCode] IS NOT NULL;
GO
