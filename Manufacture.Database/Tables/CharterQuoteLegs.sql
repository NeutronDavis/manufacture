CREATE TABLE [dbo].[CharterQuoteLegs]
(
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [CharterQuoteRequestId] INT            NOT NULL,
    [LegNumber]             INT            NOT NULL,
    [OriginAddress]         NVARCHAR (300) NOT NULL,
    [OriginCityState]       NVARCHAR (120) NOT NULL,
    [DestinationAddress]    NVARCHAR (300) NOT NULL,
    [DestinationCityState]  NVARCHAR (120) NOT NULL,
    [DistanceKm]            DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_CharterQuoteLegs_DistanceKm] DEFAULT (0),
    [FuelOption]            INT            NOT NULL CONSTRAINT [DF_CharterQuoteLegs_FuelOption] DEFAULT (0),
    [QuotedLegAmount]       DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_CharterQuoteLegs_QuotedLegAmount] DEFAULT (0),
    [Notes]                 NVARCHAR (300) NULL,
    [CreatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_CharterQuoteLegs_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_CharterQuoteLegs_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_CharterQuoteLegs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_CharterQuoteLegs_Quote_Leg] UNIQUE ([CharterQuoteRequestId], [LegNumber]),
    CONSTRAINT [FK_CharterQuoteLegs_CharterQuoteRequests] FOREIGN KEY ([CharterQuoteRequestId]) REFERENCES [dbo].[CharterQuoteRequests] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_CharterQuoteLegs_FuelOption] CHECK ([FuelOption] IN (0, 1)),
    CONSTRAINT [CK_CharterQuoteLegs_LegNumber] CHECK ([LegNumber] > 0),
    CONSTRAINT [CK_CharterQuoteLegs_Amounts] CHECK ([DistanceKm] >= 0 AND [QuotedLegAmount] > 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_CharterQuoteLegs_QuoteRequestId]
    ON [dbo].[CharterQuoteLegs] ([CharterQuoteRequestId] ASC);
GO
