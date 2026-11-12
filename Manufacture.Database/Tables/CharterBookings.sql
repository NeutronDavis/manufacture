CREATE TABLE [dbo].[CharterBookings]
(
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [BookingRef]            NVARCHAR (40)  NOT NULL,
    [CustomerName]          NVARCHAR (150) NOT NULL,
    [CustomerPhone]         NVARCHAR (30)  NOT NULL CONSTRAINT [DF_CharterBookings_CustomerPhone] DEFAULT (N''),
    [VehicleId]             INT            NOT NULL,
    [DriverEmployeeId]      INT            NULL,
    [DriverName]            NVARCHAR (150) NOT NULL CONSTRAINT [DF_CharterBookings_DriverName] DEFAULT (N''),
    [DriverAllowance]       DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_CharterBookings_DriverAllowance] DEFAULT (0),
    [SecurityEscortFee]     DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_CharterBookings_SecurityEscortFee] DEFAULT (0),
    [TollsAndRoadLevies]    DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_CharterBookings_TollsAndRoadLevies] DEFAULT (0),
    [TotalTripIncidentals]  AS ([DriverAllowance] + [SecurityEscortFee] + [TollsAndRoadLevies]) PERSISTED,
    [GrossCharterFee]       DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_CharterBookings_GrossCharterFee] DEFAULT (0),
    [NetCharterRevenue]     AS ([GrossCharterFee] - ([DriverAllowance] + [SecurityEscortFee] + [TollsAndRoadLevies])) PERSISTED,
    [TripStatus]            INT            NOT NULL CONSTRAINT [DF_CharterBookings_TripStatus] DEFAULT (0),
    [DepartureDate]         DATE           NOT NULL,
    [ReturnDate]            DATE           NULL,
    [Notes]                 NVARCHAR (500) NOT NULL CONSTRAINT [DF_CharterBookings_Notes] DEFAULT (N''),
    [CreatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_CharterBookings_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]             DATETIME2 (3)  NOT NULL CONSTRAINT [DF_CharterBookings_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]            ROWVERSION     NOT NULL,

    CONSTRAINT [PK_CharterBookings] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_CharterBookings_BookingRef] UNIQUE ([BookingRef]),
    CONSTRAINT [FK_CharterBookings_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]),
    CONSTRAINT [FK_CharterBookings_Employees] FOREIGN KEY ([DriverEmployeeId]) REFERENCES [dbo].[Employees] ([Id]),
    CONSTRAINT [CK_CharterBookings_TripStatus] CHECK ([TripStatus] IN (0, 1, 2, 3)),
    CONSTRAINT [CK_CharterBookings_Amounts] CHECK ([DriverAllowance] >= 0 AND [SecurityEscortFee] >= 0 AND [TollsAndRoadLevies] >= 0 AND [GrossCharterFee] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_CharterBookings_DepartureDate_Status]
    ON [dbo].[CharterBookings] ([DepartureDate] DESC, [TripStatus] ASC);
GO
