CREATE TABLE [dbo].[Notifications]
(
    [Id]              BIGINT         IDENTITY (1, 1) NOT NULL,
    [RecipientUserId] INT            NOT NULL,
    [Title]           NVARCHAR (200) NOT NULL,
    [Message]         NVARCHAR (1000) NOT NULL CONSTRAINT [DF_Notifications_Message] DEFAULT (N''),
    [LinkUrl]         NVARCHAR (300) NULL,
    [Type]            INT            NOT NULL CONSTRAINT [DF_Notifications_Type] DEFAULT (0),
    [IsRead]          BIT            NOT NULL CONSTRAINT [DF_Notifications_IsRead] DEFAULT (0),
    [ReadAt]          DATETIME2 (3)  NULL,
    [CreatedByUserId] INT            NULL,
    [CreatedAt]       DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Notifications_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]      ROWVERSION     NOT NULL,

    CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Notifications_RecipientUser] FOREIGN KEY ([RecipientUserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Notifications_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id]),
    CONSTRAINT [CK_Notifications_Type] CHECK ([Type] IN (0, 1, 2, 3))
);
GO

CREATE NONCLUSTERED INDEX [IX_Notifications_Recipient_IsRead_CreatedAt]
    ON [dbo].[Notifications] ([RecipientUserId] ASC, [IsRead] ASC, [CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_Notifications_CreatedAt]
    ON [dbo].[Notifications] ([CreatedAt] DESC);
GO
