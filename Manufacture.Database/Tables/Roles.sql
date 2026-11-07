CREATE TABLE [dbo].[Roles]
(
    [Id]            INT            IDENTITY (1, 1) NOT NULL,
    [Name]          NVARCHAR (50)  NOT NULL,
    [DisplayName]   NVARCHAR (100) NOT NULL,
    [Description]   NVARCHAR (300) NOT NULL CONSTRAINT [DF_Roles_Description] DEFAULT (N''),
    [IsSystemRole]  BIT            NOT NULL CONSTRAINT [DF_Roles_IsSystemRole] DEFAULT (1),
    [CreatedAt]     DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Roles_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]     DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Roles_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Roles_Name] UNIQUE ([Name])
);
GO
