CREATE TABLE [dbo].[Vehicles]
(
    [Id]                  INT            IDENTITY (1, 1) NOT NULL,
    [RegistrationNumber]  NVARCHAR (20)  NOT NULL,
    [MakeAndModel]        NVARCHAR (120) NOT NULL,
    [VehicleType]         NVARCHAR (40)  NOT NULL CONSTRAINT [DF_Vehicles_VehicleType] DEFAULT (N'Van'),
    [VehicleFunction]     NVARCHAR (60)  NOT NULL CONSTRAINT [DF_Vehicles_VehicleFunction] DEFAULT (N'Deliveries'),
    [AssignedEmployeeId]  INT            NULL,
    [FuelType]            NVARCHAR (40)  NOT NULL CONSTRAINT [DF_Vehicles_FuelType] DEFAULT (N'Petrol'),
    [ImageUrl]            NVARCHAR (500) NULL,
    [TankCapacityLitres]  DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_Vehicles_TankCapacityLitres] DEFAULT (70),
    [CurrentOdometerKm]   DECIMAL (18, 2) NOT NULL CONSTRAINT [DF_Vehicles_CurrentOdometerKm] DEFAULT (0),
    [Status]              NVARCHAR (30)  NOT NULL CONSTRAINT [DF_Vehicles_Status] DEFAULT (N'Active'),
    [LastServiceDate]     DATE           NULL,
    [IsActive]            BIT            NOT NULL CONSTRAINT [DF_Vehicles_IsActive] DEFAULT (1),
    [CreatedAt]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Vehicles_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]           DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Vehicles_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]          ROWVERSION     NOT NULL,

    CONSTRAINT [PK_Vehicles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Vehicles_RegistrationNumber] UNIQUE ([RegistrationNumber]),
    CONSTRAINT [FK_Vehicles_Employees] FOREIGN KEY ([AssignedEmployeeId]) REFERENCES [dbo].[Employees] ([Id]),
    CONSTRAINT [CK_Vehicles_TankCapacity] CHECK ([TankCapacityLitres] > 0),
    CONSTRAINT [CK_Vehicles_Odometer] CHECK ([CurrentOdometerKm] >= 0),
    CONSTRAINT [CK_Vehicles_Status] CHECK ([Status] IN (N'Active', N'InMaintenance', N'Grounded'))
);
GO

CREATE NONCLUSTERED INDEX [IX_Vehicles_Status_IsActive]
    ON [dbo].[Vehicles] ([Status] ASC, [IsActive] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Vehicles_AssignedEmployeeId]
    ON [dbo].[Vehicles] ([AssignedEmployeeId] ASC)
    WHERE [AssignedEmployeeId] IS NOT NULL;
GO
