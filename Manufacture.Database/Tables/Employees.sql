CREATE TABLE [dbo].[Employees]
(
    [Id]                      INT            IDENTITY (1, 1) NOT NULL,
    [FullName]                NVARCHAR (150) NOT NULL,
    [StaffCode]               NVARCHAR (30)  NOT NULL,
    [DepartmentId]            INT            NULL,
    [JobRoleId]               INT            NULL,
    [Department]              NVARCHAR (80)  NOT NULL,
    [Designation]             NVARCHAR (100) NOT NULL,
    [PhoneNumber]             NVARCHAR (30)  NOT NULL,
    [Email]                   NVARCHAR (256) NOT NULL CONSTRAINT [DF_Employees_Email] DEFAULT (N''),
    [Address]                 NVARCHAR (300) NOT NULL CONSTRAINT [DF_Employees_Address] DEFAULT (N''),
    [PassportUrl]             NVARCHAR (500) NULL,
    [BankName]                NVARCHAR (100) NOT NULL CONSTRAINT [DF_Employees_BankName] DEFAULT (N''),
    [BankAccountNumber]       NVARCHAR (30)  NOT NULL CONSTRAINT [DF_Employees_BankAccountNumber] DEFAULT (N''),
    [EmploymentStatus]        NVARCHAR (20)  NOT NULL CONSTRAINT [DF_Employees_EmploymentStatus] DEFAULT (N'Active'),
    [BaseSalary]              DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_Employees_BaseSalary] DEFAULT (0),
    [EmergencyContactName]    NVARCHAR (150) NOT NULL CONSTRAINT [DF_Employees_EmergencyContactName] DEFAULT (N''),
    [EmergencyContactPhone]   NVARCHAR (30)  NOT NULL CONSTRAINT [DF_Employees_EmergencyContactPhone] DEFAULT (N''),
    [EmergencyRelationship]   NVARCHAR (80)  NOT NULL CONSTRAINT [DF_Employees_EmergencyRelationship] DEFAULT (N''),
    [GuarantorName]           NVARCHAR (150) NOT NULL CONSTRAINT [DF_Employees_GuarantorName] DEFAULT (N''),
    [GuarantorPhone]          NVARCHAR (30)  NOT NULL CONSTRAINT [DF_Employees_GuarantorPhone] DEFAULT (N''),
    [GuarantorAddress]        NVARCHAR (300) NOT NULL CONSTRAINT [DF_Employees_GuarantorAddress] DEFAULT (N''),
    [GuarantorOccupation]     NVARCHAR (100) NOT NULL CONSTRAINT [DF_Employees_GuarantorOccupation] DEFAULT (N''),
    [EmploymentDate]          DATE           NOT NULL CONSTRAINT [DF_Employees_EmploymentDate] DEFAULT (CONVERT(DATE, SYSUTCDATETIME())),
    [IsActive]                BIT            NOT NULL CONSTRAINT [DF_Employees_IsActive] DEFAULT (1),
    [IsDeleted]               BIT            NOT NULL CONSTRAINT [DF_Employees_IsDeleted] DEFAULT (0),
    [DeletedAt]               DATETIME2 (3)  NULL,
    [DeletedBy]               INT            NULL,
    [TerminationDate]         DATE           NULL,
    [TerminationReason]       NVARCHAR (300) NOT NULL CONSTRAINT [DF_Employees_TerminationReason] DEFAULT (N''),
    [CreatedAt]               DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Employees_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]               DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Employees_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]              ROWVERSION     NOT NULL,

    CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Employees_Departments] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([Id]),
    CONSTRAINT [FK_Employees_JobRoles] FOREIGN KEY ([JobRoleId]) REFERENCES [dbo].[JobRoles] ([Id]),
    CONSTRAINT [CK_Employees_BaseSalary] CHECK ([BaseSalary] >= 0),
    CONSTRAINT [CK_Employees_EmploymentStatus] CHECK ([EmploymentStatus] IN (N'Active', N'OnLeave', N'Inactive'))
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_Employees_StaffCode]
    ON [dbo].[Employees] ([StaffCode] ASC)
    WHERE [IsDeleted] = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_Employees_Email]
    ON [dbo].[Employees] ([Email] ASC)
    WHERE [IsDeleted] = 0 AND [Email] <> N'';
GO

CREATE NONCLUSTERED INDEX [IX_Employees_Department_IsActive]
    ON [dbo].[Employees] ([Department] ASC, [IsActive] ASC)
    INCLUDE ([IsDeleted]);
GO

CREATE NONCLUSTERED INDEX [IX_Employees_DepartmentId_IsActive]
    ON [dbo].[Employees] ([DepartmentId] ASC, [IsActive] ASC)
    WHERE [DepartmentId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Employees_JobRoleId]
    ON [dbo].[Employees] ([JobRoleId] ASC)
    WHERE [JobRoleId] IS NOT NULL;
GO
