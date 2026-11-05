CREATE TABLE [dbo].[TripLegs]
(
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [CharterBookingId]  INT            NOT NULL,
    [LegNumber]         INT            NOT NULL,
    [OriginState]       NVARCHAR (80)  NOT NULL,
    [DestinationState]  NVARCHAR (120) NOT NULL,
    [FuelOption]        INT            NOT NULL CONSTRAINT [DF_TripLegs_FuelOption] DEFAULT (0),
    [LegCost]           DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_TripLegs_LegCost] DEFAULT (0),
    [CreatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_TripLegs_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_TripLegs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_TripLegs_Booking_Leg] UNIQUE ([CharterBookingId], [LegNumber]),
    CONSTRAINT [FK_TripLegs_CharterBookings] FOREIGN KEY ([CharterBookingId]) REFERENCES [dbo].[CharterBookings] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_TripLegs_FuelOption] CHECK ([FuelOption] IN (0, 1)),
    CONSTRAINT [CK_TripLegs_LegNumber] CHECK ([LegNumber] > 0),
    CONSTRAINT [CK_TripLegs_LegCost] CHECK ([LegCost] >= 0)
);
GO
