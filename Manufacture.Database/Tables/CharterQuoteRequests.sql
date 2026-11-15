CREATE TABLE [dbo].[CharterQuoteRequests]
(
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [QuoteRef]              NVARCHAR (40)  NOT NULL,
    [CustomerName]          NVARCHAR (150) NOT NULL,
    [CustomerPhone]         NVARCHAR (30)  NOT NULL CONSTRAINT [DF_CharterQuoteRequests_CustomerPhone] DEFAULT (N''),
    [CustomerEmail]         NVARCHAR (200) NULL,
    [RequestDate]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_CharterQuoteRequests_RequestDate] DEFAULT (SYSUTCDATETIME()),
    [TripType]              INT            NOT NULL CONSTRAINT [DF_CharterQuoteRequests_TripType] DEFAULT (0),
    [VehicleTypeRequired]   NVARCHAR (60)  NOT NULL,
    [VehicleId]             INT            NULL,
    [TotalDistanceKm]       DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_CharterQuoteRequests_TotalDistanceKm] DEFAULT (0),
    [QuotedAmount]          DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_CharterQuoteRequests_QuotedAmount] DEFAULT (0),
    [DriverAllowance]       DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_CharterQuoteRequests_DriverAllowance] DEFAULT (0),
    [SecurityEscortFee]     DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_CharterQuoteRequests_SecurityEscortFee] DEFAULT (0),
    [TollsAndRoadLevies]    DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_CharterQuoteRequests_TollsAndRoadLevies] DEFAULT (0),
    [TotalTripIncidentals]  AS ([DriverAllowance] + [SecurityEscortFee] + [TollsAndRoadLevies]) PERSISTED,
    [NetEstimate]           AS ([QuotedAmount] - ([DriverAllowance] + [SecurityEscortFee] + [TollsAndRoadLevies])) PERSISTED,
    [Status]                INT            NOT NULL CONSTRAINT [DF_CharterQuoteRequests_Status] DEFAULT (0),
    [ValidUntil]            DATE           NULL,
    [Notes]                 NVARCHAR (1000) NOT NULL CONSTRAINT [DF_CharterQuoteRequests_Notes] DEFAULT (N''),
    [RequestedByUserId]     INT            NULL,
    [SubmittedAt]           DATETIME2 (3)  NULL,
    [ApprovedByUserId]      INT            NULL,
    [ApprovedAt]            DATETIME2 (3)  NULL,
    [RejectionReason]       NVARCHAR (500) NULL,
    [CreatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_CharterQuoteRequests_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_CharterQuoteRequests_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]            ROWVERSION     NOT NULL,

    CONSTRAINT [PK_CharterQuoteRequests] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_CharterQuoteRequests_QuoteRef] UNIQUE ([QuoteRef]),
    CONSTRAINT [FK_CharterQuoteRequests_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]),
    CONSTRAINT [FK_CharterQuoteRequests_RequestedBy] FOREIGN KEY ([RequestedByUserId]) REFERENCES [dbo].[Users] ([Id]),
    CONSTRAINT [FK_CharterQuoteRequests_ApprovedBy] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [dbo].[Users] ([Id]),
    CONSTRAINT [CK_CharterQuoteRequests_Status] CHECK ([Status] IN (0, 1, 2, 3, 4, 5)),
    CONSTRAINT [CK_CharterQuoteRequests_TripType] CHECK ([TripType] IN (0, 1, 2)),
    CONSTRAINT [CK_CharterQuoteRequests_Amounts] CHECK ([TotalDistanceKm] >= 0 AND [QuotedAmount] >= 0 AND [DriverAllowance] >= 0 AND [SecurityEscortFee] >= 0 AND [TollsAndRoadLevies] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_CharterQuoteRequests_Status_RequestDate]
    ON [dbo].[CharterQuoteRequests] ([Status] ASC, [RequestDate] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_CharterQuoteRequests_CustomerName]
    ON [dbo].[CharterQuoteRequests] ([CustomerName] ASC);
GO
