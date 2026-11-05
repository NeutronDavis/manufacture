CREATE TABLE [dbo].[Drivers]
(
    [Id]            INT            IDENTITY (1, 1) NOT NULL,
    [FullName]      NVARCHAR (150) NOT NULL,
    [PhoneNumber]   NVARCHAR (30)  NOT NULL CONSTRAINT [DF_Drivers_PhoneNumber] DEFAULT (N''),
    [LicenseNumber] NVARCHAR (40)  NULL,
    [IsActive]      BIT            NOT NULL CONSTRAINT [DF_Drivers_IsActive] DEFAULT (1),
    [CreatedAt]     DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Drivers_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]     DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Drivers_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_Drivers] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_Drivers_LicenseNumber]
    ON [dbo].[Drivers] ([LicenseNumber] ASC)
    WHERE [LicenseNumber] IS NOT NULL;
GO
