CREATE TABLE [dbo].[RbacModules]
(
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [ModuleKey]   NVARCHAR (60)  NOT NULL,
    [ModuleName]  NVARCHAR (150) NOT NULL,
    [Description] NVARCHAR (300) NOT NULL CONSTRAINT [DF_RbacModules_Description] DEFAULT (N''),
    [SortOrder]   INT            NOT NULL CONSTRAINT [DF_RbacModules_SortOrder] DEFAULT (0),
    [IsActive]    BIT            NOT NULL CONSTRAINT [DF_RbacModules_IsActive] DEFAULT (1),
    [CreatedAt]   DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RbacModules_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]   DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RbacModules_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_RbacModules] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RbacModules_ModuleKey] UNIQUE ([ModuleKey])
);
GO

CREATE NONCLUSTERED INDEX [IX_RbacModules_SortOrder_IsActive]
    ON [dbo].[RbacModules] ([SortOrder] ASC, [IsActive] ASC);
GO
