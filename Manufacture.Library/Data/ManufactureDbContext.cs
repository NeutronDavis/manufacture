using System;
using System.Collections.Generic;
using Manufacture.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Manufacture.Library.Data;

public partial class ManufactureDbContext : DbContext
{
    public ManufactureDbContext(DbContextOptions<ManufactureDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CharterBooking> CharterBookings { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerReturn> CustomerReturns { get; set; }

    public virtual DbSet<DailyRepRouteSheet> DailyRepRouteSheets { get; set; }

    public virtual DbSet<Driver> Drivers { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<FuelLog> FuelLogs { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<InventoryItem> InventoryItems { get; set; }

    public virtual DbSet<MaintenanceLog> MaintenanceLogs { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<PayrollDeductionLine> PayrollDeductionLines { get; set; }

    public virtual DbSet<PayrollRecord> PayrollRecords { get; set; }

    public virtual DbSet<PayrollRun> PayrollRuns { get; set; }

    public virtual DbSet<PayrollSetting> PayrollSettings { get; set; }

    public virtual DbSet<PosTerminal> PosTerminals { get; set; }

    public virtual DbSet<ProductionBatch> ProductionBatches { get; set; }

    public virtual DbSet<ProductionDefect> ProductionDefects { get; set; }

    public virtual DbSet<RbacModule> RbacModules { get; set; }

    public virtual DbSet<RbacPermissionAudit> RbacPermissionAudits { get; set; }

    public virtual DbSet<RbacPermissionLevel> RbacPermissionLevels { get; set; }

    public virtual DbSet<RbacRolePermission> RbacRolePermissions { get; set; }

    public virtual DbSet<Recipe> Recipes { get; set; }

    public virtual DbSet<RecipeIngredient> RecipeIngredients { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RouteRate> RouteRates { get; set; }

    public virtual DbSet<RouteSheetItem> RouteSheetItems { get; set; }

    public virtual DbSet<SalaryAdvance> SalaryAdvances { get; set; }

    public virtual DbSet<SalesRep> SalesReps { get; set; }

    public virtual DbSet<StoreRequest> StoreRequests { get; set; }

    public virtual DbSet<StoreRequestItem> StoreRequestItems { get; set; }

    public virtual DbSet<SupermarketAccount> SupermarketAccounts { get; set; }

    public virtual DbSet<SupermarketAccountPrice> SupermarketAccountPrices { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<TreasuryPolicySetting> TreasuryPolicySettings { get; set; }

    public virtual DbSet<TripLeg> TripLegs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<WeeklyFleetExpense> WeeklyFleetExpenses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => new { e.EntityName, e.EntityId }, "IX_AuditLogs_EntityName_EntityId");

            entity.HasIndex(e => e.OccurredAt, "IX_AuditLogs_OccurredAt").IsDescending();

            entity.Property(e => e.Action).HasMaxLength(40);
            entity.Property(e => e.EntityId).HasMaxLength(60);
            entity.Property(e => e.EntityName).HasMaxLength(120);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.OccurredAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_AuditLogs_OccurredAt");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AuditLogs_Users");
        });

        modelBuilder.Entity<CharterBooking>(entity =>
        {
            entity.HasIndex(e => new { e.DepartureDate, e.TripStatus }, "IX_CharterBookings_DepartureDate_Status").IsDescending(true, false);

            entity.HasIndex(e => e.BookingRef, "UQ_CharterBookings_BookingRef").IsUnique();

            entity.Property(e => e.BookingRef).HasMaxLength(40);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_CharterBookings_CreatedAt");
            entity.Property(e => e.CustomerName).HasMaxLength(150);
            entity.Property(e => e.CustomerPhone)
                .HasMaxLength(30)
                .HasDefaultValue("", "DF_CharterBookings_CustomerPhone");
            entity.Property(e => e.DriverAllowance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DriverName)
                .HasMaxLength(150)
                .HasDefaultValue("", "DF_CharterBookings_DriverName");
            entity.Property(e => e.GrossCharterFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NetCharterRevenue)
                .HasComputedColumnSql("([GrossCharterFee]-(([DriverAllowance]+[SecurityEscortFee])+[TollsAndRoadLevies]))", true)
                .HasColumnType("decimal(21, 2)");
            entity.Property(e => e.Notes)
                .HasMaxLength(500)
                .HasDefaultValue("", "DF_CharterBookings_Notes");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.SecurityEscortFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TollsAndRoadLevies).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalTripIncidentals)
                .HasComputedColumnSql("(([DriverAllowance]+[SecurityEscortFee])+[TollsAndRoadLevies])", true)
                .HasColumnType("decimal(20, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_CharterBookings_UpdatedAt");

            entity.HasOne(d => d.Driver).WithMany(p => p.CharterBookings)
                .HasForeignKey(d => d.DriverId)
                .HasConstraintName("FK_CharterBookings_Drivers");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.CharterBookings)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CharterBookings_Vehicles");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => new { e.Category, e.IsActive }, "IX_Customers_Category_IsActive");

            entity.HasIndex(e => e.MemberCode, "UQ_Customers_MemberCode").IsUnique();

            entity.Property(e => e.Address)
                .HasMaxLength(300)
                .HasDefaultValue("", "DF_Customers_Address");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Customers_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Customers_IsActive");
            entity.Property(e => e.MemberCode).HasMaxLength(40);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.OutstandingBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30)
                .HasDefaultValue("", "DF_Customers_PhoneNumber");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Customers_UpdatedAt");
        });

        modelBuilder.Entity<CustomerReturn>(entity =>
        {
            entity.HasIndex(e => new { e.RecipeId, e.ReturnDate }, "IX_CustomerReturns_RecipeId_ReturnDate").IsDescending(false, true);

            entity.HasIndex(e => new { e.SalesRepId, e.ReturnDate }, "IX_CustomerReturns_SalesRepId_ReturnDate")
                .IsDescending(false, true)
                .HasFilter("([SalesRepId] IS NOT NULL)");

            entity.HasIndex(e => e.ReturnNumber, "UQ_CustomerReturns_ReturnNumber").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_CustomerReturns_CreatedAt");
            entity.Property(e => e.Notes)
                .HasMaxLength(400)
                .HasDefaultValue("", "DF_CustomerReturns_Notes");
            entity.Property(e => e.Reason)
                .HasMaxLength(250)
                .HasDefaultValue("", "DF_CustomerReturns_Reason");
            entity.Property(e => e.RecordedBy)
                .HasMaxLength(150)
                .HasDefaultValue("", "DF_CustomerReturns_RecordedBy");
            entity.Property(e => e.RefundAmount)
                .HasComputedColumnSql("([Quantity]*[UnitPrice])", true)
                .HasColumnType("decimal(29, 2)");
            entity.Property(e => e.ReturnDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_CustomerReturns_ReturnDate");
            entity.Property(e => e.ReturnNumber).HasMaxLength(40);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerReturns)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_CustomerReturns_Customers");

            entity.HasOne(d => d.Order).WithMany(p => p.CustomerReturns)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_CustomerReturns_Orders");

            entity.HasOne(d => d.Recipe).WithMany(p => p.CustomerReturns)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomerReturns_Recipes");

            entity.HasOne(d => d.SalesRep).WithMany(p => p.CustomerReturns)
                .HasForeignKey(d => d.SalesRepId)
                .HasConstraintName("FK_CustomerReturns_SalesReps");
        });

        modelBuilder.Entity<DailyRepRouteSheet>(entity =>
        {
            entity.HasIndex(e => new { e.RouteDate, e.Status }, "IX_DailyRepRouteSheets_RouteDate_Status").IsDescending(true, false);

            entity.HasIndex(e => new { e.VehicleId, e.RouteDate }, "IX_DailyRepRouteSheets_VehicleId_RouteDate")
                .IsDescending(false, true)
                .HasFilter("([VehicleId] IS NOT NULL)");

            entity.HasIndex(e => e.RouteSheetRef, "UQ_DailyRepRouteSheets_Ref").IsUnique();

            entity.HasIndex(e => new { e.SalesRepId, e.RouteDate }, "UQ_DailyRepRouteSheets_Rep_Date").IsUnique();

            entity.Property(e => e.BankTransferCollected).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CashCollected).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_DailyRepRouteSheets_CreatedAt");
            entity.Property(e => e.NewOutstandingAccountBalance)
                .HasComputedColumnSql("(([PreviousAccountBalance]+[TotalExpectedSalesRevenue])-(([CashCollected]+[BankTransferCollected])+[PosTerminalCollected]))", true)
                .HasColumnType("decimal(21, 2)");
            entity.Property(e => e.Notes)
                .HasMaxLength(500)
                .HasDefaultValue("", "DF_DailyRepRouteSheets_Notes");
            entity.Property(e => e.PosTerminalCode).HasMaxLength(20);
            entity.Property(e => e.PosTerminalCollected).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PreviousAccountBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReconciledAt).HasPrecision(3);
            entity.Property(e => e.ReconciledBy).HasMaxLength(150);
            entity.Property(e => e.RouteName).HasMaxLength(120);
            entity.Property(e => e.RouteSheetRef).HasMaxLength(40);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.SignedOffAt).HasPrecision(3);
            entity.Property(e => e.SignedOffByDispatcher).HasMaxLength(150);
            entity.Property(e => e.TotalExpectedSalesRevenue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalPaymentsCollectedToday)
                .HasComputedColumnSql("(([CashCollected]+[BankTransferCollected])+[PosTerminalCollected])", true)
                .HasColumnType("decimal(20, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_DailyRepRouteSheets_UpdatedAt");
            entity.Property(e => e.VehicleRegistration).HasMaxLength(20);

            entity.HasOne(d => d.PosTerminal).WithMany(p => p.DailyRepRouteSheets)
                .HasForeignKey(d => d.PosTerminalId)
                .HasConstraintName("FK_DailyRepRouteSheets_PosTerminals");

            entity.HasOne(d => d.SalesRep).WithMany(p => p.DailyRepRouteSheets)
                .HasForeignKey(d => d.SalesRepId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DailyRepRouteSheets_SalesReps");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.DailyRepRouteSheets)
                .HasForeignKey(d => d.VehicleId)
                .HasConstraintName("FK_DailyRepRouteSheets_Vehicles");
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasIndex(e => e.LicenseNumber, "UX_Drivers_LicenseNumber")
                .IsUnique()
                .HasFilter("([LicenseNumber] IS NOT NULL)");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Drivers_CreatedAt");
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Drivers_IsActive");
            entity.Property(e => e.LicenseNumber).HasMaxLength(40);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30)
                .HasDefaultValue("", "DF_Drivers_PhoneNumber");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Drivers_UpdatedAt");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => new { e.Department, e.IsActive }, "IX_Employees_Department_IsActive");

            entity.HasIndex(e => e.StaffCode, "UQ_Employees_StaffCode").IsUnique();

            entity.HasIndex(e => e.Email, "UX_Employees_Email")
                .IsUnique()
                .HasFilter("([Email]<>N'')");

            entity.Property(e => e.Address)
                .HasMaxLength(300)
                .HasDefaultValue("", "DF_Employees_Address");
            entity.Property(e => e.BankAccountNumber)
                .HasMaxLength(30)
                .HasDefaultValue("", "DF_Employees_BankAccountNumber");
            entity.Property(e => e.BankName)
                .HasMaxLength(100)
                .HasDefaultValue("", "DF_Employees_BankName");
            entity.Property(e => e.BaseSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Employees_CreatedAt");
            entity.Property(e => e.Department).HasMaxLength(80);
            entity.Property(e => e.Designation).HasMaxLength(100);
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasDefaultValue("", "DF_Employees_Email");
            entity.Property(e => e.EmergencyContactName)
                .HasMaxLength(150)
                .HasDefaultValue("", "DF_Employees_EmergencyContactName");
            entity.Property(e => e.EmergencyContactPhone)
                .HasMaxLength(30)
                .HasDefaultValue("", "DF_Employees_EmergencyContactPhone");
            entity.Property(e => e.EmergencyRelationship)
                .HasMaxLength(80)
                .HasDefaultValue("", "DF_Employees_EmergencyRelationship");
            entity.Property(e => e.EmploymentDate).HasDefaultValueSql("(CONVERT([date],sysutcdatetime()))", "DF_Employees_EmploymentDate");
            entity.Property(e => e.EmploymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Active", "DF_Employees_EmploymentStatus");
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.GuarantorAddress)
                .HasMaxLength(300)
                .HasDefaultValue("", "DF_Employees_GuarantorAddress");
            entity.Property(e => e.GuarantorName)
                .HasMaxLength(150)
                .HasDefaultValue("", "DF_Employees_GuarantorName");
            entity.Property(e => e.GuarantorOccupation)
                .HasMaxLength(100)
                .HasDefaultValue("", "DF_Employees_GuarantorOccupation");
            entity.Property(e => e.GuarantorPhone)
                .HasMaxLength(30)
                .HasDefaultValue("", "DF_Employees_GuarantorPhone");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Employees_IsActive");
            entity.Property(e => e.PhoneNumber).HasMaxLength(30);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.StaffCode).HasMaxLength(30);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Employees_UpdatedAt");
        });

        modelBuilder.Entity<FuelLog>(entity =>
        {
            entity.HasIndex(e => e.FuelDate, "IX_FuelLogs_FuelDate").IsDescending();

            entity.HasIndex(e => new { e.VehicleId, e.FuelDate }, "IX_FuelLogs_VehicleId_FuelDate").IsDescending(false, true);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_FuelLogs_CreatedAt");
            entity.Property(e => e.FuelDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_FuelLogs_FuelDate");
            entity.Property(e => e.FuelType)
                .HasMaxLength(40)
                .HasDefaultValue("Petrol", "DF_FuelLogs_FuelType");
            entity.Property(e => e.LitresDispensed).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PetrolStation)
                .HasMaxLength(120)
                .HasDefaultValue("", "DF_FuelLogs_PetrolStation");
            entity.Property(e => e.PricePerLitre).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RouteDescription)
                .HasMaxLength(200)
                .HasDefaultValue("", "DF_FuelLogs_RouteDescription");
            entity.Property(e => e.TotalCost)
                .HasComputedColumnSql("([LitresDispensed]*[PricePerLitre])", true)
                .HasColumnType("decimal(37, 4)");

            entity.HasOne(d => d.Driver).WithMany(p => p.FuelLogs)
                .HasForeignKey(d => d.DriverId)
                .HasConstraintName("FK_FuelLogs_Drivers");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.FuelLogs)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FuelLogs_Vehicles");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasIndex(e => new { e.Category, e.IsActive }, "IX_Ingredients_Category_IsActive");

            entity.HasIndex(e => e.SupplierId, "IX_Ingredients_SupplierId").HasFilter("([SupplierId] IS NOT NULL)");

            entity.HasIndex(e => e.ItemCode, "UX_Ingredients_ItemCode")
                .IsUnique()
                .HasFilter("([ItemCode] IS NOT NULL)");

            entity.Property(e => e.Category)
                .HasMaxLength(60)
                .HasDefaultValue("Raw Materials", "DF_Ingredients_Category");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Ingredients_CreatedAt");
            entity.Property(e => e.CurrentStock).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Ingredients_IsActive");
            entity.Property(e => e.ItemCode).HasMaxLength(40);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.PackageCostPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PackageStockOnHand).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.PackageUnit).HasMaxLength(40);
            entity.Property(e => e.Unit)
                .HasMaxLength(40)
                .HasDefaultValue("kg", "DF_Ingredients_Unit");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Ingredients_UpdatedAt");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_Ingredients_Suppliers");
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasIndex(e => new { e.Category, e.IsActive }, "IX_InventoryItems_Category_IsActive");

            entity.HasIndex(e => e.PreferredSupplierId, "IX_InventoryItems_PreferredSupplierId").HasFilter("([PreferredSupplierId] IS NOT NULL)");

            entity.HasIndex(e => e.ItemCode, "UQ_InventoryItems_ItemCode").IsUnique();

            entity.Property(e => e.Category)
                .HasMaxLength(60)
                .HasDefaultValue("Raw Materials", "DF_InventoryItems_Category");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_InventoryItems_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_InventoryItems_IsActive");
            entity.Property(e => e.ItemCode).HasMaxLength(40);
            entity.Property(e => e.LastRestockedDate).HasDefaultValueSql("(CONVERT([date],sysutcdatetime()))", "DF_InventoryItems_LastRestockedDate");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.QuantityInStock).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.ReorderThreshold).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Unit)
                .HasMaxLength(40)
                .HasDefaultValue("Bags (50kg)", "DF_InventoryItems_Unit");
            entity.Property(e => e.UnitCostPrice).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_InventoryItems_UpdatedAt");

            entity.HasOne(d => d.PreferredSupplier).WithMany(p => p.InventoryItems)
                .HasForeignKey(d => d.PreferredSupplierId)
                .HasConstraintName("FK_InventoryItems_Suppliers");
        });

        modelBuilder.Entity<MaintenanceLog>(entity =>
        {
            entity.HasIndex(e => new { e.VehicleId, e.ServiceDate }, "IX_MaintenanceLogs_VehicleId_ServiceDate").IsDescending(false, true);

            entity.Property(e => e.Cost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_MaintenanceLogs_CreatedAt");
            entity.Property(e => e.Description).HasMaxLength(400);
            entity.Property(e => e.InvoiceNumber)
                .HasMaxLength(60)
                .HasDefaultValue("", "DF_MaintenanceLogs_InvoiceNumber");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_MaintenanceLogs_UpdatedAt");
            entity.Property(e => e.WorkshopOrVendor)
                .HasMaxLength(150)
                .HasDefaultValue("", "DF_MaintenanceLogs_WorkshopOrVendor");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.MaintenanceLogs)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MaintenanceLogs_Vehicles");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => new { e.CustomerId, e.OrderDate }, "IX_Orders_CustomerId_OrderDate").IsDescending(false, true);

            entity.HasIndex(e => e.OrderDate, "IX_Orders_OrderDate").IsDescending();

            entity.HasIndex(e => new { e.SalesRepId, e.OrderDate }, "IX_Orders_SalesRepId_OrderDate")
                .IsDescending(false, true)
                .HasFilter("([SalesRepId] IS NOT NULL)");

            entity.HasIndex(e => new { e.Status, e.TargetDeliveryDate }, "IX_Orders_Status_TargetDeliveryDate");

            entity.HasIndex(e => e.OrderNumber, "UQ_Orders_OrderNumber").IsUnique();

            entity.Property(e => e.AmountPaid).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BalanceDue)
                .HasComputedColumnSql("(([SubTotal]-[Discount])-[AmountPaid])", true)
                .HasColumnType("decimal(20, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Orders_CreatedAt");
            entity.Property(e => e.CustomerName).HasMaxLength(150);
            entity.Property(e => e.Discount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LoadedAt).HasPrecision(3);
            entity.Property(e => e.Notes)
                .HasMaxLength(500)
                .HasDefaultValue("", "DF_Orders_Notes");
            entity.Property(e => e.OrderDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Orders_OrderDate");
            entity.Property(e => e.OrderNumber).HasMaxLength(40);
            entity.Property(e => e.PosTerminalCode).HasMaxLength(20);
            entity.Property(e => e.PrintedAt).HasPrecision(3);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.SalesRepName).HasMaxLength(150);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TargetDeliveryDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Orders_TargetDeliveryDate");
            entity.Property(e => e.TotalAmount)
                .HasComputedColumnSql("([SubTotal]-[Discount])", true)
                .HasColumnType("decimal(19, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Orders_UpdatedAt");
            entity.Property(e => e.VerifiedByLoaderName).HasMaxLength(150);

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Customers");

            entity.HasOne(d => d.PosTerminal).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PosTerminalId)
                .HasConstraintName("FK_Orders_PosTerminals");

            entity.HasOne(d => d.SalesRep).WithMany(p => p.Orders)
                .HasForeignKey(d => d.SalesRepId)
                .HasConstraintName("FK_Orders_SalesReps");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasIndex(e => e.RecipeId, "IX_OrderItems_RecipeId");

            entity.HasIndex(e => new { e.OrderId, e.RecipeId }, "UQ_OrderItems_Order_Recipe").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_OrderItems_CreatedAt");
            entity.Property(e => e.LineTotal)
                .HasComputedColumnSql("([Quantity]*[UnitPrice])", true)
                .HasColumnType("decimal(29, 2)");
            entity.Property(e => e.ProductName).HasMaxLength(150);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_OrderItems_UpdatedAt");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Recipe).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Recipes");
        });

        modelBuilder.Entity<PayrollDeductionLine>(entity =>
        {
            entity.HasIndex(e => new { e.EmployeeId, e.CreatedAt }, "IX_PayrollDeductionLines_EmployeeId_CreatedAt").IsDescending(false, true);

            entity.HasIndex(e => e.PayrollRecordId, "IX_PayrollDeductionLines_PayrollRecordId");

            entity.HasIndex(e => e.SalaryAdvanceId, "IX_PayrollDeductionLines_SalaryAdvanceId").HasFilter("([SalaryAdvanceId] IS NOT NULL)");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_PayrollDeductionLines_CreatedAt");
            entity.Property(e => e.DeductionType).HasMaxLength(40);
            entity.Property(e => e.Description).HasMaxLength(300);

            entity.HasOne(d => d.Employee).WithMany(p => p.PayrollDeductionLines)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollDeductionLines_Employees");

            entity.HasOne(d => d.PayrollRecord).WithMany(p => p.PayrollDeductionLines)
                .HasForeignKey(d => d.PayrollRecordId)
                .HasConstraintName("FK_PayrollDeductionLines_PayrollRecords");

            entity.HasOne(d => d.SalaryAdvance).WithMany(p => p.PayrollDeductionLines)
                .HasForeignKey(d => d.SalaryAdvanceId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_PayrollDeductionLines_SalaryAdvances");
        });

        modelBuilder.Entity<PayrollRecord>(entity =>
        {
            entity.HasIndex(e => e.PayrollRunId, "IX_PayrollRecords_PayrollRunId").HasFilter("([PayrollRunId] IS NOT NULL)");

            entity.HasIndex(e => new { e.PayrollYear, e.PayrollMonth, e.PaymentStatus }, "IX_PayrollRecords_Period_Status").IsDescending(true, true, false);

            entity.HasIndex(e => new { e.EmployeeId, e.PayrollYear, e.PayrollMonth }, "UQ_PayrollRecords_Employee_Period").IsUnique();

            entity.Property(e => e.AdvanceDeduction).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BaseSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_PayrollRecords_CreatedAt");
            entity.Property(e => e.DisciplinaryDeductions).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GrossSalary)
                .HasComputedColumnSql("(((([BaseSalary]+[PerformanceBonus])+[HousingAllowance])+[TransportAllowance])+[OtherAllowances])", true)
                .HasColumnType("decimal(22, 2)");
            entity.Property(e => e.HealthInsuranceDeduction).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.HousingAllowance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NetSalary)
                .HasComputedColumnSql("((((([BaseSalary]+[PerformanceBonus])+[HousingAllowance])+[TransportAllowance])+[OtherAllowances])-(((([DisciplinaryDeductions]+[AdvanceDeduction])+[TaxAndLevies])+[PensionDeduction])+[HealthInsuranceDeduction]))", true)
                .HasColumnType("decimal(23, 2)");
            entity.Property(e => e.Notes)
                .HasMaxLength(400)
                .HasDefaultValue("", "DF_PayrollRecords_Notes");
            entity.Property(e => e.OtherAllowances).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaymentDate).HasPrecision(3);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending", "DF_PayrollRecords_PaymentStatus");
            entity.Property(e => e.PensionDeduction).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PerformanceBonus).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxAndLevies).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalDeductions)
                .HasComputedColumnSql("(((([DisciplinaryDeductions]+[AdvanceDeduction])+[TaxAndLevies])+[PensionDeduction])+[HealthInsuranceDeduction])", true)
                .HasColumnType("decimal(22, 2)");
            entity.Property(e => e.TransportAllowance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_PayrollRecords_UpdatedAt");

            entity.HasOne(d => d.Employee).WithMany(p => p.PayrollRecords)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollRecords_Employees");

            entity.HasOne(d => d.PayrollRun).WithMany(p => p.PayrollRecords)
                .HasForeignKey(d => d.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PayrollRecords_PayrollRuns");
        });

        modelBuilder.Entity<PayrollRun>(entity =>
        {
            entity.HasIndex(e => e.GeneratedAt, "IX_PayrollRuns_GeneratedAt").IsDescending();

            entity.HasIndex(e => new { e.PayrollYear, e.PayrollMonth }, "UQ_PayrollRuns_Period").IsUnique();

            entity.HasIndex(e => e.RunNumber, "UQ_PayrollRuns_RunNumber").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_PayrollRuns_CreatedAt");
            entity.Property(e => e.DeductionsTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GeneratedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_PayrollRuns_GeneratedAt");
            entity.Property(e => e.GeneratedBy)
                .HasMaxLength(150)
                .HasDefaultValue("System", "DF_PayrollRuns_GeneratedBy");
            entity.Property(e => e.GrossTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NetTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Notes)
                .HasMaxLength(400)
                .HasDefaultValue("", "DF_PayrollRuns_Notes");
            entity.Property(e => e.RunNumber).HasMaxLength(40);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending", "DF_PayrollRuns_Status");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_PayrollRuns_UpdatedAt");
        });

        modelBuilder.Entity<PayrollSetting>(entity =>
        {
            entity.Property(e => e.HealthInsurancePercent)
                .HasDefaultValue(2.65m, "DF_PayrollSettings_HealthInsurancePercent")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.HousingAllowancePercent)
                .HasDefaultValue(12.50m, "DF_PayrollSettings_HousingAllowancePercent")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.LoanMonthlyRepaymentPercent)
                .HasDefaultValue(20.00m, "DF_PayrollSettings_LoanMonthlyRepaymentPercent")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.MaxLoanPercentOfSalary)
                .HasDefaultValue(50.00m, "DF_PayrollSettings_MaxLoanPercentOfSalary")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.PayeTaxPercent)
                .HasDefaultValue(3.50m, "DF_PayrollSettings_PayeTaxPercent")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.PensionPercent)
                .HasDefaultValue(5.00m, "DF_PayrollSettings_PensionPercent")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TransportAllowancePercent)
                .HasDefaultValue(6.25m, "DF_PayrollSettings_TransportAllowancePercent")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_PayrollSettings_UpdatedAt");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(150)
                .HasDefaultValue("SuperAdmin", "DF_PayrollSettings_UpdatedBy");
        });

        modelBuilder.Entity<PosTerminal>(entity =>
        {
            entity.HasIndex(e => e.AssignedUserId, "IX_PosTerminals_AssignedUserId").HasFilter("([AssignedUserId] IS NOT NULL)");

            entity.HasIndex(e => e.TerminalCode, "UQ_PosTerminals_TerminalCode").IsUnique();

            entity.Property(e => e.CashAccountName)
                .HasMaxLength(100)
                .HasDefaultValue("Bakery Cash Drawer", "DF_PosTerminals_CashAccountName");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_PosTerminals_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_PosTerminals_IsActive");
            entity.Property(e => e.PosBankAccountName)
                .HasMaxLength(120)
                .HasDefaultValue("Moniepoint POS - 9028129", "DF_PosTerminals_PosBankAccountName");
            entity.Property(e => e.TerminalCode).HasMaxLength(20);
            entity.Property(e => e.TerminalName).HasMaxLength(100);
            entity.Property(e => e.TransferAccountName)
                .HasMaxLength(120)
                .HasDefaultValue("FirstBank - 3021948291", "DF_PosTerminals_TransferAccountName");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_PosTerminals_UpdatedAt");

            entity.HasOne(d => d.AssignedUser).WithMany(p => p.PosTerminals)
                .HasForeignKey(d => d.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_PosTerminals_Users");
        });

        modelBuilder.Entity<ProductionBatch>(entity =>
        {
            entity.HasIndex(e => e.LinkedOrderId, "IX_ProductionBatches_LinkedOrderId").HasFilter("([LinkedOrderId] IS NOT NULL)");

            entity.HasIndex(e => e.ProductionDate, "IX_ProductionBatches_ProductionDate").IsDescending();

            entity.HasIndex(e => new { e.RecipeId, e.Status }, "IX_ProductionBatches_RecipeId_Status");

            entity.HasIndex(e => e.BatchNumber, "UQ_ProductionBatches_BatchNumber").IsUnique();

            entity.Property(e => e.BakerInCharge)
                .HasMaxLength(150)
                .HasDefaultValue("", "DF_ProductionBatches_BakerInCharge");
            entity.Property(e => e.BatchInputQuantity)
                .HasDefaultValue(1m, "DF_ProductionBatches_BatchInputQuantity")
                .HasColumnType("decimal(18, 3)");
            entity.Property(e => e.BatchInputUnitLabel)
                .HasMaxLength(60)
                .HasDefaultValue("Flour Bags (50kg)", "DF_ProductionBatches_BatchInputUnitLabel");
            entity.Property(e => e.BatchNumber).HasMaxLength(40);
            entity.Property(e => e.CalculatedUnitCost).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_ProductionBatches_CreatedAt");
            entity.Property(e => e.CustomerName).HasMaxLength(150);
            entity.Property(e => e.ExpectedUnitCost).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.FlourBagsUsed).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.LinkedOrderNumber).HasMaxLength(40);
            entity.Property(e => e.LoggedBy)
                .HasMaxLength(150)
                .HasDefaultValue("", "DF_ProductionBatches_LoggedBy");
            entity.Property(e => e.Notes)
                .HasMaxLength(500)
                .HasDefaultValue("", "DF_ProductionBatches_Notes");
            entity.Property(e => e.OutputUnitLabel)
                .HasMaxLength(40)
                .HasDefaultValue("loaves", "DF_ProductionBatches_OutputUnitLabel");
            entity.Property(e => e.ProductionDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_ProductionBatches_ProductionDate");
            entity.Property(e => e.RecipeName).HasMaxLength(150);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Shift)
                .HasMaxLength(40)
                .HasDefaultValue("Morning", "DF_ProductionBatches_Shift");
            entity.Property(e => e.TotalBatchCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_ProductionBatches_UpdatedAt");
            entity.Property(e => e.Variance).HasComputedColumnSql("([ActualQuantity]-[TargetQuantity])", true);

            entity.HasOne(d => d.LinkedOrder).WithMany(p => p.ProductionBatches)
                .HasForeignKey(d => d.LinkedOrderId)
                .HasConstraintName("FK_ProductionBatches_Orders");

            entity.HasOne(d => d.Recipe).WithMany(p => p.ProductionBatches)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductionBatches_Recipes");
        });

        modelBuilder.Entity<ProductionDefect>(entity =>
        {
            entity.HasIndex(e => new { e.RecipeId, e.RecordedAt }, "IX_ProductionDefects_RecipeId_RecordedAt").IsDescending(false, true);

            entity.HasIndex(e => e.RecordedAt, "IX_ProductionDefects_RecordedAt").IsDescending();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_ProductionDefects_CreatedAt");
            entity.Property(e => e.DefectReason)
                .HasMaxLength(250)
                .HasDefaultValue("", "DF_ProductionDefects_DefectReason");
            entity.Property(e => e.RecordedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_ProductionDefects_RecordedAt");
            entity.Property(e => e.RecordedBy)
                .HasMaxLength(150)
                .HasDefaultValue("", "DF_ProductionDefects_RecordedBy");

            entity.HasOne(d => d.ProductionBatch).WithMany(p => p.ProductionDefects)
                .HasForeignKey(d => d.ProductionBatchId)
                .HasConstraintName("FK_ProductionDefects_ProductionBatches");

            entity.HasOne(d => d.Recipe).WithMany(p => p.ProductionDefects)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductionDefects_Recipes");
        });

        modelBuilder.Entity<RbacModule>(entity =>
        {
            entity.HasIndex(e => new { e.SortOrder, e.IsActive }, "IX_RbacModules_SortOrder_IsActive");

            entity.HasIndex(e => e.ModuleKey, "UQ_RbacModules_ModuleKey").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RbacModules_CreatedAt");
            entity.Property(e => e.Description)
                .HasMaxLength(300)
                .HasDefaultValue("", "DF_RbacModules_Description");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_RbacModules_IsActive");
            entity.Property(e => e.ModuleKey).HasMaxLength(60);
            entity.Property(e => e.ModuleName).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RbacModules_UpdatedAt");
        });

        modelBuilder.Entity<RbacPermissionAudit>(entity =>
        {
            entity.HasIndex(e => e.ChangedAt, "IX_RbacPermissionAudits_ChangedAt").IsDescending();

            entity.HasIndex(e => new { e.RoleId, e.ModuleId, e.ChangedAt }, "IX_RbacPermissionAudits_Role_Module").IsDescending(false, false, true);

            entity.Property(e => e.ChangedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RbacPermissionAudits_ChangedAt");
            entity.Property(e => e.ChangedBy)
                .HasMaxLength(150)
                .HasDefaultValue("System", "DF_RbacPermissionAudits_ChangedBy");
            entity.Property(e => e.ModuleKey).HasMaxLength(60);
            entity.Property(e => e.NewPermissionLevelName).HasMaxLength(80);
            entity.Property(e => e.OldPermissionLevelName).HasMaxLength(80);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<RbacPermissionLevel>(entity =>
        {
            entity.HasIndex(e => new { e.SortOrder, e.IsActive }, "IX_RbacPermissionLevels_SortOrder_IsActive");

            entity.HasIndex(e => e.Name, "UQ_RbacPermissionLevels_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RbacPermissionLevels_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_RbacPermissionLevels_IsActive");
            entity.Property(e => e.Name).HasMaxLength(80);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RbacPermissionLevels_UpdatedAt");
        });

        modelBuilder.Entity<RbacRolePermission>(entity =>
        {
            entity.HasIndex(e => e.ModuleId, "IX_RbacRolePermissions_ModuleId");

            entity.HasIndex(e => e.PermissionLevelId, "IX_RbacRolePermissions_PermissionLevelId");

            entity.HasIndex(e => new { e.RoleId, e.ModuleId }, "UQ_RbacRolePermissions_Role_Module").IsUnique();

            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RbacRolePermissions_UpdatedAt");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(150)
                .HasDefaultValue("System", "DF_RbacRolePermissions_UpdatedBy");

            entity.HasOne(d => d.Module).WithMany(p => p.RbacRolePermissions)
                .HasForeignKey(d => d.ModuleId)
                .HasConstraintName("FK_RbacRolePermissions_RbacModules");

            entity.HasOne(d => d.PermissionLevel).WithMany(p => p.RbacRolePermissions)
                .HasForeignKey(d => d.PermissionLevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RbacRolePermissions_RbacPermissionLevels");

            entity.HasOne(d => d.Role).WithMany(p => p.RbacRolePermissions)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_RbacRolePermissions_Roles");
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasIndex(e => new { e.ProductType, e.IsActive }, "IX_Recipes_ProductType_IsActive");

            entity.HasIndex(e => e.Code, "UQ_Recipes_Code").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(40);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Recipes_CreatedAt");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasDefaultValue("", "DF_Recipes_Description");
            entity.Property(e => e.ExpectedYield).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Recipes_IsActive");
            entity.Property(e => e.LaborAndOverheadPerUnit).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.OutputUnit)
                .HasMaxLength(40)
                .HasDefaultValue("loaves", "DF_Recipes_OutputUnit");
            entity.Property(e => e.PackagingCost).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Recipes_UpdatedAt");
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasIndex(e => e.IngredientId, "IX_RecipeIngredients_IngredientId");

            entity.HasIndex(e => new { e.RecipeId, e.IngredientId }, "UQ_RecipeIngredients_Recipe_Ingredient").IsUnique();

            entity.Property(e => e.BaselineYield)
                .HasDefaultValue(1m, "DF_RecipeIngredients_BaselineYield")
                .HasColumnType("decimal(18, 3)");
            entity.Property(e => e.BatchLineCost)
                .HasComputedColumnSql("([QuantityPerBatch]*[IngredientUnitCost])", true)
                .HasColumnType("decimal(37, 8)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RecipeIngredients_CreatedAt");
            entity.Property(e => e.IngredientUnitCost).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.QuantityPerBatch).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.QuantityPerUnit)
                .HasComputedColumnSql("(case when [BaselineYield]>(0) then [QuantityPerBatch]/[BaselineYield] else (0) end)", true)
                .HasColumnType("decimal(38, 21)");
            entity.Property(e => e.QuantityUnit).HasMaxLength(40);
            entity.Property(e => e.Unit).HasMaxLength(40);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RecipeIngredients_UpdatedAt");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.RecipeIngredients)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecipeIngredients_Ingredients");

            entity.HasOne(d => d.Recipe).WithMany(p => p.RecipeIngredients)
                .HasForeignKey(d => d.RecipeId)
                .HasConstraintName("FK_RecipeIngredients_Recipes");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_Roles_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Roles_CreatedAt");
            entity.Property(e => e.Description)
                .HasMaxLength(300)
                .HasDefaultValue("", "DF_Roles_Description");
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.IsSystemRole).HasDefaultValue(true, "DF_Roles_IsSystemRole");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Roles_UpdatedAt");
        });

        modelBuilder.Entity<RouteRate>(entity =>
        {
            entity.HasIndex(e => e.IsActive, "IX_RouteRates_IsActive");

            entity.HasIndex(e => new { e.OriginState, e.DestinationState, e.VehicleTypeRequired }, "UQ_RouteRates_Route_VehicleType").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RouteRates_CreatedAt");
            entity.Property(e => e.DestinationState).HasMaxLength(120);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_RouteRates_IsActive");
            entity.Property(e => e.OriginState).HasMaxLength(80);
            entity.Property(e => e.RateWithFuel).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RateWithoutFuel).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RouteRates_UpdatedAt");
            entity.Property(e => e.VehicleTypeRequired).HasMaxLength(60);
        });

        modelBuilder.Entity<RouteSheetItem>(entity =>
        {
            entity.HasIndex(e => e.RecipeId, "IX_RouteSheetItems_RecipeId");

            entity.HasIndex(e => new { e.RouteSheetId, e.RecipeId }, "UQ_RouteSheetItems_RouteSheet_Recipe").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RouteSheetItems_CreatedAt");
            entity.Property(e => e.ExpectedItemRevenue)
                .HasComputedColumnSql("([QtySold]*[UnitSellingPrice])", true)
                .HasColumnType("decimal(29, 2)");
            entity.Property(e => e.ProductCode).HasMaxLength(40);
            entity.Property(e => e.ProductName).HasMaxLength(150);
            entity.Property(e => e.TotalActiveInventory).HasComputedColumnSql("([YesterdayClosingStock]+[TodayNewlyLoadedQty])", true);
            entity.Property(e => e.UnitSellingPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RouteSheetItems_UpdatedAt");

            entity.HasOne(d => d.Recipe).WithMany(p => p.RouteSheetItems)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RouteSheetItems_Recipes");

            entity.HasOne(d => d.RouteSheet).WithMany(p => p.RouteSheetItems)
                .HasForeignKey(d => d.RouteSheetId)
                .HasConstraintName("FK_RouteSheetItems_DailyRepRouteSheets");
        });

        modelBuilder.Entity<SalaryAdvance>(entity =>
        {
            entity.HasIndex(e => new { e.EmployeeId, e.Status }, "IX_SalaryAdvances_EmployeeId_Status");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_SalaryAdvances_CreatedAt");
            entity.Property(e => e.MonthlyDeductionAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrincipalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Reason)
                .HasMaxLength(300)
                .HasDefaultValue("", "DF_SalaryAdvances_Reason");
            entity.Property(e => e.RemainingBalance)
                .HasComputedColumnSql("([PrincipalAmount]-[TotalRepaid])", true)
                .HasColumnType("decimal(19, 2)");
            entity.Property(e => e.RequestDate).HasDefaultValueSql("(CONVERT([date],sysutcdatetime()))", "DF_SalaryAdvances_RequestDate");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active", "DF_SalaryAdvances_Status");
            entity.Property(e => e.TotalRepaid).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_SalaryAdvances_UpdatedAt");

            entity.HasOne(d => d.Employee).WithMany(p => p.SalaryAdvances)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalaryAdvances_Employees");
        });

        modelBuilder.Entity<SalesRep>(entity =>
        {
            entity.HasIndex(e => new { e.RouteName, e.IsActive }, "IX_SalesReps_RouteName_IsActive");

            entity.HasIndex(e => e.VehicleId, "IX_SalesReps_VehicleId").HasFilter("([VehicleId] IS NOT NULL)");

            entity.HasIndex(e => e.RepCode, "UQ_SalesReps_RepCode").IsUnique();

            entity.HasIndex(e => e.UserId, "UX_SalesReps_UserId")
                .IsUnique()
                .HasFilter("([UserId] IS NOT NULL)");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_SalesReps_CreatedAt");
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_SalesReps_IsActive");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30)
                .HasDefaultValue("", "DF_SalesReps_PhoneNumber");
            entity.Property(e => e.ProductFocus)
                .HasMaxLength(120)
                .HasDefaultValue("", "DF_SalesReps_ProductFocus");
            entity.Property(e => e.RepCode).HasMaxLength(20);
            entity.Property(e => e.RouteName).HasMaxLength(120);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_SalesReps_UpdatedAt");

            entity.HasOne(d => d.PosTerminal).WithMany(p => p.SalesReps)
                .HasForeignKey(d => d.PosTerminalId)
                .HasConstraintName("FK_SalesReps_PosTerminals");

            entity.HasOne(d => d.User).WithOne(p => p.SalesRep)
                .HasForeignKey<SalesRep>(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_SalesReps_Users");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.SalesReps)
                .HasForeignKey(d => d.VehicleId)
                .HasConstraintName("FK_SalesReps_Vehicles");
        });

        modelBuilder.Entity<StoreRequest>(entity =>
        {
            entity.HasIndex(e => new { e.RequestedAt, e.Status }, "IX_StoreRequests_RequestedAt_Status").IsDescending(true, false);

            entity.HasIndex(e => e.RequestNumber, "UQ_StoreRequests_RequestNumber").IsUnique();

            entity.Property(e => e.ApprovedAt).HasPrecision(3);
            entity.Property(e => e.ApprovedBy).HasMaxLength(150);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_StoreRequests_CreatedAt");
            entity.Property(e => e.Department)
                .HasMaxLength(80)
                .HasDefaultValue("Bakery Floor", "DF_StoreRequests_Department");
            entity.Property(e => e.Purpose)
                .HasMaxLength(300)
                .HasDefaultValue("", "DF_StoreRequests_Purpose");
            entity.Property(e => e.RejectionReason).HasMaxLength(300);
            entity.Property(e => e.RequestNumber).HasMaxLength(40);
            entity.Property(e => e.RequestedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_StoreRequests_RequestedAt");
            entity.Property(e => e.RequestedBy).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_StoreRequests_UpdatedAt");
        });

        modelBuilder.Entity<StoreRequestItem>(entity =>
        {
            entity.HasIndex(e => e.InventoryItemId, "IX_StoreRequestItems_InventoryItemId");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_StoreRequestItems_CreatedAt");
            entity.Property(e => e.IssuedQuantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.ItemName).HasMaxLength(150);
            entity.Property(e => e.RequestedQuantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Unit).HasMaxLength(40);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_StoreRequestItems_UpdatedAt");

            entity.HasOne(d => d.InventoryItem).WithMany(p => p.StoreRequestItems)
                .HasForeignKey(d => d.InventoryItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StoreRequestItems_InventoryItems");

            entity.HasOne(d => d.StoreRequest).WithMany(p => p.StoreRequestItems)
                .HasForeignKey(d => d.StoreRequestId)
                .HasConstraintName("FK_StoreRequestItems_StoreRequests");
        });

        modelBuilder.Entity<SupermarketAccount>(entity =>
        {
            entity.HasIndex(e => e.AssignedRepId, "IX_SupermarketAccounts_AssignedRepId").HasFilter("([AssignedRepId] IS NOT NULL)");

            entity.HasIndex(e => e.CustomerId, "UQ_SupermarketAccounts_CustomerId").IsUnique();

            entity.Property(e => e.AccountName).HasMaxLength(150);
            entity.Property(e => e.Address)
                .HasMaxLength(300)
                .HasDefaultValue("", "DF_SupermarketAccounts_Address");
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(150)
                .HasDefaultValue("", "DF_SupermarketAccounts_ContactPerson");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_SupermarketAccounts_CreatedAt");
            entity.Property(e => e.CreditLimit)
                .HasDefaultValue(250000m, "DF_SupermarketAccounts_CreditLimit")
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CurrentOutstandingCredit).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_SupermarketAccounts_IsActive");
            entity.Property(e => e.PaymentTerms)
                .HasMaxLength(60)
                .HasDefaultValue("Net 15 Days", "DF_SupermarketAccounts_PaymentTerms");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30)
                .HasDefaultValue("", "DF_SupermarketAccounts_PhoneNumber");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_SupermarketAccounts_UpdatedAt");

            entity.HasOne(d => d.AssignedRep).WithMany(p => p.SupermarketAccounts)
                .HasForeignKey(d => d.AssignedRepId)
                .HasConstraintName("FK_SupermarketAccounts_SalesReps");

            entity.HasOne(d => d.Customer).WithOne(p => p.SupermarketAccount)
                .HasForeignKey<SupermarketAccount>(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupermarketAccounts_Customers");
        });

        modelBuilder.Entity<SupermarketAccountPrice>(entity =>
        {
            entity.HasIndex(e => e.RecipeId, "IX_SupermarketAccountPrices_RecipeId");

            entity.HasIndex(e => new { e.SupermarketAccountId, e.RecipeId }, "UQ_SupermarketAccountPrices_Account_Recipe").IsUnique();

            entity.Property(e => e.AgreedPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_SupermarketAccountPrices_CreatedAt");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_SupermarketAccountPrices_UpdatedAt");

            entity.HasOne(d => d.Recipe).WithMany(p => p.SupermarketAccountPrices)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupermarketAccountPrices_Recipes");

            entity.HasOne(d => d.SupermarketAccount).WithMany(p => p.SupermarketAccountPrices)
                .HasForeignKey(d => d.SupermarketAccountId)
                .HasConstraintName("FK_SupermarketAccountPrices_SupermarketAccounts");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(e => new { e.SupplyCategory, e.IsActive }, "IX_Suppliers_SupplyCategory_IsActive");

            entity.HasIndex(e => e.Name, "UQ_Suppliers_Name").IsUnique();

            entity.Property(e => e.Address)
                .HasMaxLength(300)
                .HasDefaultValue("", "DF_Suppliers_Address");
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(150)
                .HasDefaultValue("", "DF_Suppliers_ContactPerson");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Suppliers_CreatedAt");
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasDefaultValue("", "DF_Suppliers_Email");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Suppliers_IsActive");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30)
                .HasDefaultValue("", "DF_Suppliers_PhoneNumber");
            entity.Property(e => e.SupplyCategory)
                .HasMaxLength(80)
                .HasDefaultValue("Flour & Grains", "DF_Suppliers_SupplyCategory");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Suppliers_UpdatedAt");
        });

        modelBuilder.Entity<TreasuryPolicySetting>(entity =>
        {
            entity.Property(e => e.AlertTolerancePercent)
                .HasDefaultValue(6.00m, "DF_TreasuryPolicySettings_AlertTolerancePercent")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.LastUpdatedUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_TreasuryPolicySettings_LastUpdatedUtc");
            entity.Property(e => e.PlannedCashPercent)
                .HasDefaultValue(32.00m, "DF_TreasuryPolicySettings_PlannedCashPercent")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.PolicyMemo)
                .HasMaxLength(500)
                .HasDefaultValue("", "DF_TreasuryPolicySettings_PolicyMemo");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(150)
                .HasDefaultValue("SuperAdmin", "DF_TreasuryPolicySettings_UpdatedBy");
        });

        modelBuilder.Entity<TripLeg>(entity =>
        {
            entity.HasIndex(e => new { e.CharterBookingId, e.LegNumber }, "UQ_TripLegs_Booking_Leg").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_TripLegs_CreatedAt");
            entity.Property(e => e.DestinationState).HasMaxLength(120);
            entity.Property(e => e.LegCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OriginState).HasMaxLength(80);

            entity.HasOne(d => d.CharterBooking).WithMany(p => p.TripLegs)
                .HasForeignKey(d => d.CharterBookingId)
                .HasConstraintName("FK_TripLegs_CharterBookings");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.PosTerminalCode, "IX_Users_PosTerminalCode").HasFilter("([PosTerminalCode] IS NOT NULL)");

            entity.HasIndex(e => new { e.RoleId, e.IsActive }, "IX_Users_RoleId_IsActive");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Users_CreatedAt");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Users_IsActive");
            entity.Property(e => e.LastLoginAt).HasPrecision(3);
            entity.Property(e => e.MustChangePassword).HasDefaultValue(true, "DF_Users_MustChangePassword");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(30);
            entity.Property(e => e.PinCodeHash).HasMaxLength(256);
            entity.Property(e => e.PosTerminalCode).HasMaxLength(20);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.SecurityStamp).HasDefaultValueSql("(newid())", "DF_Users_SecurityStamp");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Users_UpdatedAt");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasIndex(e => e.AssignedDriverId, "IX_Vehicles_AssignedDriverId").HasFilter("([AssignedDriverId] IS NOT NULL)");

            entity.HasIndex(e => e.AssignedEmployeeId, "IX_Vehicles_AssignedEmployeeId").HasFilter("([AssignedEmployeeId] IS NOT NULL)");

            entity.HasIndex(e => new { e.Status, e.IsActive }, "IX_Vehicles_Status_IsActive");

            entity.HasIndex(e => e.RegistrationNumber, "UQ_Vehicles_RegistrationNumber").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Vehicles_CreatedAt");
            entity.Property(e => e.CurrentOdometerKm).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FuelType)
                .HasMaxLength(40)
                .HasDefaultValue("PMS (Petrol)", "DF_Vehicles_FuelType");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Vehicles_IsActive");
            entity.Property(e => e.MakeAndModel).HasMaxLength(120);
            entity.Property(e => e.RegistrationNumber).HasMaxLength(20);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Active", "DF_Vehicles_Status");
            entity.Property(e => e.TankCapacityLitres)
                .HasDefaultValue(70m, "DF_Vehicles_TankCapacityLitres")
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Vehicles_UpdatedAt");
            entity.Property(e => e.VehicleFunction)
                .HasMaxLength(60)
                .HasDefaultValue("Deliveries", "DF_Vehicles_VehicleFunction");
            entity.Property(e => e.VehicleType)
                .HasMaxLength(40)
                .HasDefaultValue("Van", "DF_Vehicles_VehicleType");

            entity.HasOne(d => d.AssignedDriver).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.AssignedDriverId)
                .HasConstraintName("FK_Vehicles_Drivers");

            entity.HasOne(d => d.AssignedEmployee).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.AssignedEmployeeId)
                .HasConstraintName("FK_Vehicles_Employees");
        });

        modelBuilder.Entity<WeeklyFleetExpense>(entity =>
        {
            entity.HasIndex(e => e.OperatingWeekStart, "IX_WeeklyFleetExpenses_Week").IsDescending();

            entity.HasIndex(e => new { e.VehicleId, e.OperatingWeekStart }, "UQ_WeeklyFleetExpenses_Vehicle_Week").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_WeeklyFleetExpenses_CreatedAt");
            entity.Property(e => e.DriverName)
                .HasMaxLength(150)
                .HasDefaultValue("", "DF_WeeklyFleetExpenses_DriverName");
            entity.Property(e => e.FuelExpenseTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IncidentalsTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaintenanceExpenseTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalWeeklyRunningCost)
                .HasComputedColumnSql("(([FuelExpenseTotal]+[MaintenanceExpenseTotal])+[IncidentalsTotal])", true)
                .HasColumnType("decimal(20, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_WeeklyFleetExpenses_UpdatedAt");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.WeeklyFleetExpenses)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WeeklyFleetExpenses_Vehicles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
