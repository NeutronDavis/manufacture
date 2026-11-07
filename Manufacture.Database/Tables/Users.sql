CREATE TABLE [dbo].[Users]
(
    [Id]                 INT              IDENTITY (1, 1) NOT NULL,
    [Name]               NVARCHAR (150)   NOT NULL,
    [Email]              NVARCHAR (256)   NOT NULL,
    [RoleId]             INT              NOT NULL,
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
    CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_Users_RoleId_IsActive]
    ON [dbo].[Users] ([RoleId] ASC, [IsActive] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Users_PosTerminalCode]
    ON [dbo].[Users] ([PosTerminalCode] ASC)
    WHERE [PosTerminalCode] IS NOT NULL;
GO
