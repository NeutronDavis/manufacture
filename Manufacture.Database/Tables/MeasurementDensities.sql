CREATE TABLE [dbo].[MeasurementDensities]
(
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [MaterialName]      NVARCHAR (150) NOT NULL,
    [DensityKgPerLitre] DECIMAL (18, 4) NOT NULL,
    [IsActive]          BIT            NOT NULL CONSTRAINT [DF_MeasurementDensities_IsActive] DEFAULT (1),
    [CreatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_MeasurementDensities_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]         DATETIME2 (3)  NOT NULL CONSTRAINT [DF_MeasurementDensities_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [RowVersion]        ROWVERSION     NOT NULL,

    CONSTRAINT [PK_MeasurementDensities] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_MeasurementDensities_MaterialName] UNIQUE ([MaterialName]),
    CONSTRAINT [CK_MeasurementDensities_Density] CHECK ([DensityKgPerLitre] > 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_MeasurementDensities_IsActive]
    ON [dbo].[MeasurementDensities] ([IsActive] ASC);
GO
