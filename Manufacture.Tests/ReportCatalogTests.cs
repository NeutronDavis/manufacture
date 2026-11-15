using Manufacture.Models.DTOs;
using Manufacture.Services.Reports;
using Xunit;

namespace Manufacture.Tests;

public class ReportCatalogTests
{
    [Fact]
    public void SuperAdminSeesTheFullCatalogue()
    {
        var reports = ReportCatalog.ForRole("SuperAdmin");

        Assert.Equal(ReportCatalog.All.Count, reports.Count);
        Assert.Contains(reports, report => report.Key == "production-yield");
        Assert.Contains(reports, report => report.Key == "sales-ar-ageing");
        Assert.Contains(reports, report => report.Key == "inventory-stock-valuation");
    }

    [Fact]
    public void SalesRepOnlySeesSalesAndOwnScopeReports()
    {
        var reports = ReportCatalog.ForRole("SalesRep");

        Assert.Contains(reports, report => report.Key == "sales-register");
        Assert.Contains(reports, report => report.Key == "exec-rep-performance");
        Assert.DoesNotContain(reports, report => report.Key == "inventory-stock-valuation");
        Assert.DoesNotContain(reports, report => report.Key == "production-yield");
        Assert.DoesNotContain(reports, report => report.Key == "exec-daily-funnel");
    }

    [Fact]
    public void StoreManagerOnlySeesInventoryReports()
    {
        var reports = ReportCatalog.ForRole("StoreManager");

        Assert.NotEmpty(reports);
        Assert.All(reports, report => Assert.Equal("Inventory", report.Group));
        Assert.Contains(reports, report => report.Key == "inventory-requisitions");
        Assert.DoesNotContain(reports, report => report.Key == "sales-register");
        Assert.DoesNotContain(reports, report => report.Key == "production-daily");
    }

    [Fact]
    public void ProductionManagerSeesProductionInventoryAndExecutiveOperationalReports()
    {
        var reports = ReportCatalog.ForRole("ProductionManager");

        Assert.Contains(reports, report => report.Key == "production-yield");
        Assert.Contains(reports, report => report.Key == "inventory-stock-valuation");
        Assert.Contains(reports, report => report.Key == "exec-daily-funnel");
        Assert.DoesNotContain(reports, report => report.Key == "sales-ar-ageing");
        Assert.DoesNotContain(reports, report => report.Key == "exec-rep-performance");
    }

    [Fact]
    public void HrManagerOnlySeesHrAndPayrollReports()
    {
        var reports = ReportCatalog.ForRole("HrPayrollManager");

        Assert.NotEmpty(reports);
        Assert.All(reports, report => Assert.Equal("HR & Payroll", report.Group));
        Assert.Contains(reports, report => report.Key == "payroll-register");
        Assert.DoesNotContain(reports, report => report.Key == "sales-register");
        Assert.DoesNotContain(reports, report => report.Key == "fleet-fuel");
        Assert.Empty(ReportCatalog.ForRole("Customer"));
    }

    [Fact]
    public void FleetAndAdminReportsAreSuperAdminOnly()
    {
        var superAdmin = ReportCatalog.ForRole("SuperAdmin");
        var production = ReportCatalog.ForRole("ProductionManager");
        var store = ReportCatalog.ForRole("StoreManager");

        Assert.Contains(superAdmin, report => report.Key == "fleet-fuel");
        Assert.Contains(superAdmin, report => report.Key == "admin-rbac-matrix");
        Assert.DoesNotContain(production, report => report.Key == "fleet-fuel");
        Assert.DoesNotContain(store, report => report.Key == "admin-user-access");
    }

    [Fact]
    public void AuthorizationRejectsUnauthorizedReportKeys()
    {
        var authorization = new ReportAuthorizationService();

        Assert.True(authorization.CanAccess("SuperAdmin", "production-yield"));
        Assert.True(authorization.CanAccess("StoreManager", "inventory-low-stock"));
        Assert.False(authorization.CanAccess("StoreManager", "production-yield"));
        Assert.False(authorization.CanAccess("SalesRep", "inventory-low-stock"));
        Assert.False(authorization.CanAccess("HrPayrollManager", "sales-register"));
    }

    [Fact]
    public void FormatterUsesMoneyPercentAndDateFormats()
    {
        Assert.Equal("₦1,250", ReportFormatter.Format(1250m, ReportColumnType.Money));
        Assert.Equal("93.5%", ReportFormatter.Format(93.5m, ReportColumnType.Percent));
        Assert.Equal("2026-11-14", ReportFormatter.Format(new DateTime(2026, 11, 14), ReportColumnType.Date));
    }
}
