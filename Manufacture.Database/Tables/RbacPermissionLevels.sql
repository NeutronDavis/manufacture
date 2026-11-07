CREATE TABLE [dbo].[RbacPermissionLevels]
(
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (80)  NOT NULL,
    [Rank]        INT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_Rank] DEFAULT (0),
    [CanView]     BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_CanView] DEFAULT (0),
    [CanCreate]   BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_CanCreate] DEFAULT (0),
    [CanEdit]     BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_CanEdit] DEFAULT (0),
    [CanDelete]   BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_CanDelete] DEFAULT (0),
    [CanApprove]  BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_CanApprove] DEFAULT (0),
    [CanPrint]    BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_CanPrint] DEFAULT (0),
    [CanManage]   BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_CanManage] DEFAULT (0),
    [CanRequest]  BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_CanRequest] DEFAULT (0),
    [CanIssue]    BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_CanIssue] DEFAULT (0),
    [CanDisburse] BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_CanDisburse] DEFAULT (0),
    [CanBookOwn]  BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_CanBookOwn] DEFAULT (0),
    [IsNone]      BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_IsNone] DEFAULT (0),
    [SortOrder]   INT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_SortOrder] DEFAULT (0),
    [IsActive]    BIT            NOT NULL CONSTRAINT [DF_RbacPermissionLevels_IsActive] DEFAULT (1),
    [CreatedAt]   DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RbacPermissionLevels_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]   DATETIME2 (3)  NOT NULL CONSTRAINT [DF_RbacPermissionLevels_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_RbacPermissionLevels] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RbacPermissionLevels_Name] UNIQUE ([Name])
);
GO

CREATE NONCLUSTERED INDEX [IX_RbacPermissionLevels_SortOrder_IsActive]
    ON [dbo].[RbacPermissionLevels] ([SortOrder] ASC, [IsActive] ASC);
GO
