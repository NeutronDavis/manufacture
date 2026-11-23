CREATE TABLE [dbo].[JobRoles]
(
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [DepartmentId] INT            NULL,
    [Name]         NVARCHAR (100) NOT NULL,
    [Description]  NVARCHAR (300) NOT NULL CONSTRAINT [DF_JobRoles_Description] DEFAULT (N''),
    [IsActive]     BIT            NOT NULL CONSTRAINT [DF_JobRoles_IsActive] DEFAULT (1),
    [CreatedAt]    DATETIME2 (3)  NOT NULL CONSTRAINT [DF_JobRoles_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]    DATETIME2 (3)  NOT NULL CONSTRAINT [DF_JobRoles_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_JobRoles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_JobRoles_Name] UNIQUE ([Name]),
    CONSTRAINT [FK_JobRoles_Departments] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_JobRoles_DepartmentId_IsActive]
    ON [dbo].[JobRoles] ([DepartmentId] ASC, [IsActive] ASC)
    WHERE [DepartmentId] IS NOT NULL;
GO
