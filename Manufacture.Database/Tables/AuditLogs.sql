CREATE TABLE [dbo].[AuditLogs]
(
    [Id]         BIGINT         IDENTITY (1, 1) NOT NULL,
    [UserId]     INT            NULL,
    [Action]     NVARCHAR (40)  NOT NULL,
    [EntityName] NVARCHAR (120) NOT NULL,
    [EntityId]   NVARCHAR (60)  NULL,
    [OldValues]  NVARCHAR (MAX) NULL,
    [NewValues]  NVARCHAR (MAX) NULL,
    [IpAddress]  NVARCHAR (64)  NULL,
    [OccurredAt] DATETIME2 (3)  NOT NULL CONSTRAINT [DF_AuditLogs_OccurredAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AuditLogs_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE SET NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogs_OccurredAt]
    ON [dbo].[AuditLogs] ([OccurredAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogs_EntityName_EntityId]
    ON [dbo].[AuditLogs] ([EntityName] ASC, [EntityId] ASC);
GO
