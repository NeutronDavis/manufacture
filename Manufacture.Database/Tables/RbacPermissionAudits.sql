CREATE TABLE [dbo].[RbacPermissionAudits]
(
    [Id]                      BIGINT         IDENTITY (1, 1) NOT NULL,
    [RoleId]                  INT            NOT NULL,
    [RoleName]                NVARCHAR (50)  NOT NULL,
    [ModuleId]                INT            NOT NULL,
    [ModuleKey]               NVARCHAR (60)  NOT NULL,
    [OldPermissionLevelId]    INT            NULL,
    [OldPermissionLevelName]  NVARCHAR (80)  NULL,
    [NewPermissionLevelId]    INT            NOT NULL,
    [NewPermissionLevelName]  NVARCHAR (80)  NOT NULL,
    [ChangedBy]               NVARCHAR (150) NOT NULL CONSTRAINT [DF_RbacPermissionAudits_ChangedBy] DEFAULT (N'System'),
    [ChangedAt]               DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RbacPermissionAudits_ChangedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_RbacPermissionAudits] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_RbacPermissionAudits_ChangedAt]
    ON [dbo].[RbacPermissionAudits] ([ChangedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_RbacPermissionAudits_Role_Module]
    ON [dbo].[RbacPermissionAudits] ([RoleId] ASC, [ModuleId] ASC, [ChangedAt] DESC);
GO
