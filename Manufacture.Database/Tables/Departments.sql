CREATE TABLE [dbo].[Departments]
(
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (80)  NOT NULL,
    [Description] NVARCHAR (300) NOT NULL CONSTRAINT [DF_Departments_Description] DEFAULT (N''),
    [IsActive]    BIT            NOT NULL CONSTRAINT [DF_Departments_IsActive] DEFAULT (1),
    [CreatedAt]   DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Departments_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]   DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Departments_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_Departments] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Departments_Name] UNIQUE ([Name])
);
GO
