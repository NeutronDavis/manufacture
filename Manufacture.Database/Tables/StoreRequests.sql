CREATE TABLE [dbo].[StoreRequests]
(
    [Id]              INT            IDENTITY (1, 1) NOT NULL,
    [RequestNumber]   NVARCHAR (40)  NOT NULL,
    [RequestedBy]     NVARCHAR (150) NOT NULL,
    [Department]      NVARCHAR (80)  NOT NULL CONSTRAINT [DF_StoreRequests_Department] DEFAULT (N'Bakery Floor'),
    [Purpose]         NVARCHAR (300) NOT NULL CONSTRAINT [DF_StoreRequests_Purpose] DEFAULT (N''),
    [Status]          INT            NOT NULL CONSTRAINT [DF_StoreRequests_Status] DEFAULT (0),
    [RequestedAt]     DATETIME2 (3)  NOT NULL CONSTRAINT [DF_StoreRequests_RequestedAt] DEFAULT (SYSUTCDATETIME()),
    [ApprovedBy]      NVARCHAR (150) NULL,
    [ApprovedAt]      DATETIME2 (3)  NULL,
    [RejectionReason] NVARCHAR (300) NULL,
    [CreatedAt]       DATETIME2 (3)  NOT NULL CONSTRAINT [DF_StoreRequests_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]       DATETIME2 (3)  NOT NULL CONSTRAINT [DF_StoreRequests_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_StoreRequests] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_StoreRequests_RequestNumber] UNIQUE ([RequestNumber]),
    CONSTRAINT [CK_StoreRequests_Status] CHECK ([Status] IN (0, 1, 2, 3))
);
GO

CREATE NONCLUSTERED INDEX [IX_StoreRequests_RequestedAt_Status]
    ON [dbo].[StoreRequests] ([RequestedAt] DESC, [Status] ASC);
GO
