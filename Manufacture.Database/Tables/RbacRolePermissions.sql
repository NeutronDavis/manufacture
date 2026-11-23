CREATE TABLE [dbo].[RbacRolePermissions]
(
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [RoleId]            INT            NOT NULL,
    [ModuleId]          INT            NOT NULL,
    [PermissionLevelId] INT            NOT NULL,
    [UpdatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RbacRolePermissions_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedBy]         NVARCHAR (150) NOT NULL CONSTRAINT [DF_RbacRolePermissions_UpdatedBy] DEFAULT (N'System'),

    CONSTRAINT [PK_RbacRolePermissions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RbacRolePermissions_Role_Module] UNIQUE ([RoleId], [ModuleId]),
    CONSTRAINT [FK_RbacRolePermissions_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RbacRolePermissions_RbacModules] FOREIGN KEY ([ModuleId]) REFERENCES [dbo].[RbacModules] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RbacRolePermissions_RbacPermissionLevels] FOREIGN KEY ([PermissionLevelId]) REFERENCES [dbo].[RbacPermissionLevels] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_RbacRolePermissions_ModuleId]
    ON [dbo].[RbacRolePermissions] ([ModuleId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_RbacRolePermissions_PermissionLevelId]
    ON [dbo].[RbacRolePermissions] ([PermissionLevelId] ASC);
GO
