using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests
{
    /// <summary>
    /// Tests for the deterministic analytics engine: date-range resolution,
    /// range clamping, allocation invariants, and the guarantee that the same
    /// filter always produces the same numbers.
    /// </summary>
    public class MockAnalyticsServiceTests
    {
        private readonly MockAnalyticsService _analytics;
        private readonly MockSalesService _sales;
        private readonly MockLogisticsService _logistics;
        private readonly MockUserService _users;

        public MockAnalyticsServiceTests()
        {
            // Wired the same way as the container: users and fleet first, then sales,
            // which owns the rep roster the analytics reads.
            _users = new MockUserService();
            _logistics = new MockLogisticsService();
            _sales = new MockSalesService(_users, _logistics);
            _analytics = new MockAnalyticsService(
                new MockProductionService(),
                _users,
                _logistics,
                _sales);
        }

        private static DateTime Anchor => new(2026, 9, 15); // a Tuesday

        // =============================================================
        // Range resolution
        // =============================================================

        [Theory]
        [InlineData("2026-09-15", "Daily", "2026-09-15", "2026-09-15")]
        [InlineData("2026-09-15", "Week", "2026-09-13", "2026-09-19")]     // Sun-Sat
        [InlineData("2026-09-15", "Monthly", "2026-09-01", "2026-09-30")]
        [InlineData("2026-09-15", "Yearly", "2026-01-01", "2026-12-31")]
        public void ResolveRange_ProducesTheExpectedWindow(string anchor, string timeframe, string start, string end)
        {
            var (resolvedStart, resolvedEnd, label) = MockAnalyticsService.ResolveRange(
                Enum.Parse<ReportTimeframe>(timeframe),
                DateTime.Parse(anchor),
                null,
                null);

            Assert.Equal(DateTime.Parse(start), resolvedStart);
            Assert.Equal(DateTime.Parse(end), resolvedEnd);
            Assert.False(string.IsNullOrWhiteSpace(label));
        }

        [Fact]
        public void WeekStart_IsAlwaysTheSundayOfThatWeek()
        {
            foreach (var day in Enumerable.Range(1, 28).Select(d => new DateTime(2026, 9, d)))
            {
                var start = MockAnalyticsService.WeekStart(day);
                Assert.Equal(DayOfWeek.Sunday, start.DayOfWeek);
                Assert.True(start <= day && day <= start.AddDays(6));
            }
        }

        [Fact]
        public void CustomRange_SwapsReversedDates()
        {
            var (start, end, _) = MockAnalyticsService.ResolveRange(
                ReportTimeframe.Custom,
                Anchor,
                new DateTime(2026, 9, 20),
                new DateTime(2026, 9, 5));

            Assert.Equal(new DateTime(2026, 9, 5), start);
            Assert.Equal(new DateTime(2026, 9, 20), end);
        }

        [Fact]
        public void Normalise_ClampsAFutureWeekToToday()
        {
            // Anchor far in the future: the nominal week is real, but the queried
            // window must stop at today so no shift is booked before it ran.
            var query = new ReportsQueryDto
            {
                Timeframe = ReportTimeframe.Week,
                AnchorDate = DateTime.UtcNow.Date.AddDays(60)
            };

            var normalised = _analytics.Normalise(query, DateTime.UtcNow.Date);

            Assert.Equal(query.StartDate, normalised.StartDate);
            Assert.Equal(DateTime.UtcNow.Date, normalised.EffectiveEndDate);
            Assert.True(normalised.IsClamped);
            Assert.Contains("to date", normalised.RangeLabel);
        }

        [Fact]
        public void Normalise_KeepsACompletePastWeekUnclamped()
        {
            var query = new ReportsQueryDto { Timeframe = ReportTimeframe.Monthly, AnchorDate = new DateTime(2026, 1, 20) };

            var normalised = _analytics.Normalise(query, DateTime.UtcNow.Date);

            Assert.False(normalised.IsClamped);
            Assert.Equal(normalised.EndDate, normalised.EffectiveEndDate);
        }

        [Fact]
        public void Normalise_ClampsPageAndLimitIntoSafeBounds()
        {
            var query = new ReportsQueryDto { AnchorDate = Anchor, Page = -4, Limit = 5000 };

            var normalised = _analytics.Normalise(query, DateTime.UtcNow.Date);

            Assert.Equal(1, normalised.Page);
            Assert.Equal(100, normalised.Limit);
        }

        // =============================================================
        // Determinism and aggregation invariants
        // =============================================================

        [Fact]
        public void QueryReports_IsDeterministicAcrossRepeatedCalls()
        {
            var first = Query(Anchor, ReportTimeframe.Monthly, ReportGroupBy.ProductLine);
            var second = Query(Anchor, ReportTimeframe.Monthly, ReportGroupBy.ProductLine);

            Assert.Equal(first.TotalRows, second.TotalRows);
            Assert.Equal(
                first.GrandTotals.OrdersPlaced + first.GrandTotals.Produced + first.GrandTotals.Sold,
                second.GrandTotals.OrdersPlaced + second.GrandTotals.Produced + second.GrandTotals.Sold);
            Assert.Equal(
                first.Rows.Select(r => r.RowKey + ":" + r.Sold).ToList(),
                second.Rows.Select(r => r.RowKey + ":" + r.Sold).ToList());
        }

        [Fact]
        public void SingleDay_EqualsItsSliceOfAWiderRange()
        {
            var week = Query(Anchor, ReportTimeframe.Week, ReportGroupBy.SalesRep);
            var day = Query(Anchor, ReportTimeframe.Daily, ReportGroupBy.SalesRep);

            // The daily report is one of the days the weekly report aggregates,
            // so its grand totals must appear as a subset of the week's activity
            // rather than contradicting it.
            Assert.True(day.GrandTotals.Sold > 0, "The anchor day should have sales activity.");
            Assert.True(day.GrandTotals.Sold <= week.GrandTotals.Sold,
                "A single day cannot sell more than the week that contains it.");
            Assert.True(day.GrandTotals.OrdersPlaced <= week.GrandTotals.OrdersPlaced);
        }

        [Fact]
        public void FilteringByRep_NarrowsTotalsWithoutChangingThem()
        {
            var all = Query(Anchor, ReportTimeframe.Week, ReportGroupBy.SalesRep);
            var rep = _analytics.QueryReports(new ReportsQueryDto
            {
                Timeframe = ReportTimeframe.Week,
                AnchorDate = Anchor,
                RepIds = { 1 }
            });

            Assert.True(rep.TotalRows > 0, "Rep 1 should have activity in the week.");
            Assert.All(rep.Rows, r => Assert.Equal(1, r.RepId));
            Assert.True(rep.GrandTotals.Sold < all.GrandTotals.Sold);
        }

        [Fact]
        public void FilteringByProductLine_ExcludesOtherLines()
        {
            var bread = _analytics.QueryReports(new ReportsQueryDto
            {
                Timeframe = ReportTimeframe.Week,
                AnchorDate = Anchor,
                Lines = { ProductType.Bread }
            });

            Assert.NotEmpty(bread.Rows);
            Assert.All(bread.Rows, r => Assert.Equal("Bread", r.ProductLine));
        }

        [Fact]
        public void EveryLifecycleStageStaysWithinItsUpstreamStage()
        {
            var result = Query(Anchor, ReportTimeframe.Week, ReportGroupBy.ProductLine);

            foreach (var row in result.Rows)
            {
                // Produced and loaded are capped by what was requested.
                Assert.True(row.Produced <= row.OrdersPlaced + 1, $"{row.RowKey}: produced exceeds requested.");
                Assert.True(row.Loaded <= row.Produced + 1, $"{row.RowKey}: loaded exceeds produced.");

                // Sold plus returned must reconcile back to what went on the van,
                // and damage is a subset of what came back.
                Assert.True(row.Sold + row.Returned <= row.Loaded + 1, $"{row.RowKey}: sold + returned exceeds loaded.");
                Assert.True(row.Damaged <= row.Returned, $"{row.RowKey}: damaged exceeds returned.");
            }
        }

        [Fact]
        public void GrandTotalsAreTheSumOfTheVisibleRows()
        {
            // Group by SKU with a limit above the row count so nothing paginates away.
            var result = _analytics.QueryReports(new ReportsQueryDto
            {
                Timeframe = ReportTimeframe.Monthly,
                AnchorDate = Anchor,
                GroupBy = ReportGroupBy.Sku,
                Limit = 100
            });

            Assert.Equal(result.Rows.Sum(r => r.Sold), result.GrandTotals.Sold);
            Assert.Equal(result.Rows.Sum(r => r.OrdersPlaced), result.GrandTotals.OrdersPlaced);
            Assert.Equal(result.Rows.Sum(r => r.Damaged), result.GrandTotals.Damaged);
        }

        [Fact]
        public void PaginationCoversEveryRowExactlyOnce()
        {
            var query = new ReportsQueryDto
            {
                Timeframe = ReportTimeframe.Monthly,
                AnchorDate = Anchor,
                GroupBy = ReportGroupBy.Sku,
                Limit = 5
            };

            var paged = _analytics.QueryReports(query);
            var keys = new List<string>();
            for (var page = 1; page <= paged.TotalPages; page++)
            {
                query.Page = page;
                keys.AddRange(_analytics.QueryReports(query).Rows.Select(r => r.RowKey));
            }

            Assert.Equal(paged.TotalRows, keys.Count);
            Assert.Equal(keys.Count, keys.Distinct().Count());
        }

        [Fact]
        public void SortingByAColumnReordersWithoutChangingTheRowSet()
        {
            var query = new ReportsQueryDto
            {
                Timeframe = ReportTimeframe.Monthly,
                AnchorDate = Anchor,
                GroupBy = ReportGroupBy.ProductLine,
                Limit = 100
            };

            var ascending = _analytics.QueryReports(new ReportsQueryDto
            {
                Timeframe = query.Timeframe,
                AnchorDate = Anchor,
                GroupBy = ReportGroupBy.ProductLine,
                Sort = nameof(ReportRowDto.Sold),
                Direction = "asc",
                Limit = 100
            });

            var sold = ascending.Rows.Select(r => r.Sold).ToList();
            Assert.Equal(sold.OrderBy(v => v).ToList(), sold);
            Assert.Equal(3, ascending.Rows.Count); // Bread, Water, Popcorn
        }

        // =============================================================
        // Charts and dashboard
        // =============================================================

        [Fact]
        public void LifecycleChart_ExposesEveryStageForEveryCategory()
        {
            var result = Query(Anchor, ReportTimeframe.Week, ReportGroupBy.ProductLine);
            var chart = result.Charts.Lifecycle;

            Assert.Equal(5, chart.Series.Count);
            Assert.Equal(
                new[] { "Requested", "Loaded", "Sold", "Returned", "Damaged" },
                chart.Series.Select(s => s.Label));

            // Every series must line up with the category axis or Chart.js renders blanks.
            foreach (var series in chart.Series)
            {
                Assert.Equal(chart.Categories.Count, series.Data.Count);
            }
        }

        [Fact]
        public void VolumeChart_SharesSumToOneHundredPercent()
        {
            var result = Query(Anchor, ReportTimeframe.Week, ReportGroupBy.ProductLine);
            var chart = result.Charts.Volume;

            Assert.NotEmpty(chart.Slices);
            Assert.Equal(100m, chart.Slices.Sum(s => s.SecondaryValue));
        }

        [Fact]
        public void RepPerformance_RanksRepsByUnitsSoldDescending()
        {
            var result = Query(Anchor, ReportTimeframe.Monthly, ReportGroupBy.SalesRep);
            var rows = result.Charts.RepPerformance.Rows;

            Assert.NotEmpty(rows);
            Assert.Equal(rows.OrderByDescending(r => r.SoldUnits).Select(r => r.RepId), rows.Select(r => r.RepId));
            Assert.Equal(result.GrandTotals.Sold, rows.Sum(r => r.SoldUnits));
        }

        [Fact]
        public void EveryAnalyticRepIsARealPersonOnARealTerminalAndAVan()
        {
            // The dashboard used to invent its own six-rep roster, five of whom could
            // not log in and appeared in no other module. This pins the roster to the
            // masters so that divergence fails the build rather than shipping.
            var reps = _analytics.GetSalesReps();
            var users = _users.GetAll(includeInactive: false);
            var terminals = _sales.GetAllTerminals().Select(t => t.TerminalCode).ToHashSet();
            var vehicles = _logistics.GetAllVehicles().Select(v => v.RegistrationNumber).ToHashSet();

            Assert.NotEmpty(reps);

            foreach (var rep in reps)
            {
                // A real, active login account, and the name has to be that person's.
                Assert.NotNull(rep.UserId);
                var user = users.FirstOrDefault(u => u.Id == rep.UserId);
                Assert.True(user is not null, $"Rep {rep.FullName} has no user account.");
                Assert.Equal(nameof(UserRole.SalesRep), user!.Role);
                Assert.Equal(user.Name, rep.FullName);
                Assert.Equal(user.PhoneNumber, rep.PhoneNumber);

                // The terminal and van they sell from must exist, so a renamed or
                // renumbered record cannot silently orphan a rep.
                Assert.Contains(rep.PosTerminalCode, terminals);
                Assert.Contains(rep.VehicleRegistration, vehicles);

                Assert.False(string.IsNullOrWhiteSpace(rep.RouteName));
                Assert.NotEmpty(rep.FocusLines);
            }

            // Rep ids are the analytics join key, so they must be unique and stable.
            Assert.Equal(reps.Count, reps.Select(r => r.Id).Distinct().Count());
        }

        [Fact]
        public void AnalyticsReadsTheSameRosterSalesOwns()
        {
            // One owner per fact: the analytics must not hold a private copy.
            var owned = _sales.GetAllReps().Select(r => (r.Id, r.FullName, r.RouteName, r.PosTerminalCode, r.VehicleRegistration)).ToList();
            var read = _analytics.GetSalesReps().Select(r => (r.Id, r.FullName, r.RouteName, r.PosTerminalCode, r.VehicleRegistration)).ToList();

            Assert.Equal(owned, read);
        }

        [Fact]
        public void DashboardRepRowsResolveBackToTheRealRoster()
        {
            // Every rep id that reaches the screen has to be findable in the master,
            // and the route shown has to be the master's route, not a stored copy.
            var byId = _analytics.GetSalesReps().ToDictionary(r => r.Id);
            var overview = _analytics.GetDashboardOverview();

            var rows = overview.DrillDownScopes
                .SelectMany(s => s.Periods.Values)
                .SelectMany(r => r)
                .ToList();

            Assert.NotEmpty(rows);
            foreach (var row in rows)
            {
                Assert.True(byId.ContainsKey(row.RepId), $"Rep {row.RepId} ({row.RepName}) is not in the roster.");
                Assert.Equal(byId[row.RepId].FullName, row.RepName);
                Assert.Equal(byId[row.RepId].RouteName, row.Route);
            }
        }

        [Fact]
        public void DashboardOverview_ExposesFourCumulativePeriodsPlusDrillDownScopes()
        {
            var overview = _analytics.GetDashboardOverview(Anchor);

            Assert.Equal(new[] { "Today", "This Week", "This Month", "This Year" },
                overview.Periods.Select(p => p.Label));

            // Cumulative windows are nested, so each period must be at least the
            // previous one unless the window has not opened yet.
            Assert.True(overview.Periods[1].Totals.Sold >= overview.Today.Sold);
            Assert.True(overview.Periods[2].Totals.Sold >= overview.Periods[1].Totals.Sold);
            Assert.True(overview.Periods[3].Totals.Sold >= overview.Periods[2].Totals.Sold);

            Assert.Equal(4, overview.DrillDownScopes.Count); // all + Bread + Water + Popcorn
            foreach (var scope in overview.DrillDownScopes)
            {
                Assert.Equal(4, scope.Periods.Count);
                Assert.All(scope.Periods.Values, rows => Assert.NotEmpty(rows));
            }

            Assert.NotEmpty(overview.ProductLines);
            Assert.NotEmpty(overview.Alerts);
        }

        [Fact]
        public void DashboardDrillDown_ProductLineRepsSumToThatLineTotal()
        {
            var overview = _analytics.GetDashboardOverview();
            var scope = overview.DrillDownScopes.First(s => s.ScopeKey == nameof(ProductType.Bread));
            var repSum = scope.Periods["Today"].Sum(r => r.Sold);

            var line = overview.ProductLines.First(l => l.Line == ProductType.Bread);

            Assert.Equal(line.Totals.Sold, repSum);
        }

        [Fact]
        public void DashboardOverview_ComputesEveryWidgetForEveryWindow()
        {
            // The timeframe switcher reveals a pre-computed panel rather than
            // re-deriving numbers in the browser, so each window must carry the
            // full set of widgets the dashboard renders inside it.
            var overview = _analytics.GetDashboardOverview();

            foreach (var period in overview.Periods)
            {
                Assert.True(overview.ProductLinesByPeriod.ContainsKey(period.Key));
                Assert.True(overview.TenderByPeriod.ContainsKey(period.Key));
                Assert.True(overview.AlertsByPeriod.ContainsKey(period.Key));

                var lines = overview.ProductLinesByPeriod[period.Key];
                Assert.NotEmpty(lines);
                Assert.All(lines, line => Assert.NotEmpty(line.SkuNames));
                Assert.Equal(100m, lines.Sum(l => l.ShareOfVolumePercent), 0);

                // Alerts must be a real answer for this window, not an empty list
                // standing in for "not computed yet".
                Assert.NotEmpty(overview.AlertsByPeriod[period.Key]);
                Assert.All(overview.AlertsByPeriod[period.Key], a => Assert.False(string.IsNullOrWhiteSpace(a.Title)));
            }

            // The singular properties stay pinned to Today for existing consumers.
            Assert.Same(overview.ProductLinesByPeriod["Today"], overview.ProductLines);
            Assert.Same(overview.TenderByPeriod["Today"], overview.Tender);
            Assert.Same(overview.AlertsByPeriod["Today"], overview.Alerts);
        }

        [Fact]
        public void DashboardOverview_WindowTotalsGrowWithTheWindow()
        {
            // Selecting a different timeframe has to actually change what is on
            // screen, otherwise the selector is decoration.
            var overview = _analytics.GetDashboardOverview(Anchor);

            var soldByWindow = overview.Periods
                .Select(p => overview.ProductLinesByPeriod[p.Key].Sum(l => l.Totals.Sold))
                .ToList();

            for (var i = 1; i < soldByWindow.Count; i++)
            {
                Assert.True(soldByWindow[i] >= soldByWindow[i - 1],
                    $"{overview.Periods[i].Label} sold ({soldByWindow[i]}) should not be below " +
                    $"{overview.Periods[i - 1].Label} ({soldByWindow[i - 1]}).");
            }

            Assert.True(soldByWindow[^1] > soldByWindow[0]);

            // Tender is money in the bank, so it scales the same way.
            var cashByWindow = overview.Periods
                .Select(p => overview.TenderByPeriod[p.Key].Cash)
                .ToList();
            for (var i = 1; i < cashByWindow.Count; i++)
            {
                Assert.True(cashByWindow[i] >= cashByWindow[i - 1]);
            }
        }

        [Fact]
        public void DashboardDrillDown_SplitsCashFromOtherTenderAndStaysWithinCollected()
        {
            // "Cash Collected" has to mean physical cash, not total collections,
            // and can never exceed what was actually banked.
            var overview = _analytics.GetDashboardOverview();
            var rows = overview.DrillDownScopes.First(s => s.ScopeKey == "all").Periods["Today"];

            Assert.NotEmpty(rows);
            foreach (var row in rows)
            {
                Assert.True(row.CashCollected <= row.Collected,
                    $"{row.RepName}: cash {row.CashCollected} exceeds collected {row.Collected}.");
                Assert.True(row.CashCollected >= 0);
            }

            // The whole roster's physical cash must be no more than the roster's total.
            Assert.True(rows.Sum(r => r.CashCollected) <= rows.Sum(r => r.Collected) + 0.01m);
        }

        [Fact]
        public void ModuleColumns_MatchTheRowsActuallyRendered()
        {
            foreach (var module in Enum.GetValues<AnalyticsModule>())
            {
                var result = _analytics.QueryReports(new ReportsQueryDto
                {
                    Timeframe = ReportTimeframe.Week,
                    AnchorDate = Anchor,
                    Module = module
                });

                Assert.NotEmpty(result.Columns);
                Assert.Equal(module, result.Module);
                Assert.NotEmpty(result.Rows);
            }
        }

        // =============================================================
        // Exports
        // =============================================================

        [Fact]
        public void CsvExport_StartsWithABomAndCoversEveryFilteredRow()
        {
            var result = _analytics.QueryReports(new ReportsQueryDto
            {
                Timeframe = ReportTimeframe.Monthly,
                AnchorDate = Anchor,
                GroupBy = ReportGroupBy.ProductLine,
                Limit = 5
            });

            var csv = ReportExportService.BuildCsv(result);
            var text = System.Text.Encoding.UTF8.GetString(csv);

            Assert.Equal('\uFEFF', text[0]); // Excel needs the BOM for the naira sign
            Assert.Contains(result.ModuleLabel, text);
            Assert.Contains("TOTAL", text);

            // One header plus one line per row, regardless of the page size.
            Assert.Equal(result.TotalRows, CountDataLines(result, text));
        }

        [Fact]
        public void PdfExport_IsAWellFormedSingleFile()
        {
            var result = _analytics.QueryReports(new ReportsQueryDto
            {
                Timeframe = ReportTimeframe.Monthly,
                AnchorDate = Anchor
            });

            var pdf = ReportPdfService.BuildPdf(result);
            var header = System.Text.Encoding.ASCII.GetString(pdf, 0, 8);
            var tail = System.Text.Encoding.ASCII.GetString(pdf, pdf.Length - 6, 6);

            Assert.Equal("%PDF-1.4", header);
            Assert.Equal("%%EOF\n", tail);

            var text = System.Text.Encoding.GetEncoding(28591).GetString(pdf);
            Assert.Contains("/Type /Catalog", text);
            Assert.Contains("/Type /Pages", text);
            Assert.Contains("startxref", text);

            // Every byte offset in the xref table must actually land on its object.
            foreach (var (offset, objectNumber) in ReadXrefOffsets(text))
            {
                Assert.StartsWith(objectNumber + " 0 obj", text.Substring(offset), StringComparison.Ordinal);
            }
        }

        [Fact]
        public void PdfExport_PaginatesLongReportsAndRepeatsTheHeader()
        {
            // The catalog only holds a handful of SKUs, so a real report never fills
            // a page. Capping the rows per page exercises the break-and-repeat path.
            var result = _analytics.QueryReports(new ReportsQueryDto
            {
                Timeframe = ReportTimeframe.Yearly,
                AnchorDate = Anchor,
                GroupBy = ReportGroupBy.Sku,
                Limit = 5
            });

            Assert.True(result.TotalRows > 3, "Need enough rows to force a page break.");
            var text = System.Text.Encoding.GetEncoding(28591)
                .GetString(ReportPdfService.BuildPdf(result, maxRowsPerPage: 3));

            Assert.Contains("/Count 3 ", text);   // 7 rows over 3 pages
            Assert.Contains("Continued", text);   // header repeated on pages 2 and 3
        }

        [Fact]
        public void PdfExport_WithoutACapFitsASinglePage()
        {
            var result = _analytics.QueryReports(new ReportsQueryDto
            {
                Timeframe = ReportTimeframe.Monthly,
                AnchorDate = Anchor,
                GroupBy = ReportGroupBy.Sku
            });

            var text = System.Text.Encoding.GetEncoding(28591).GetString(ReportPdfService.BuildPdf(result));

            Assert.Contains("/Count 1 ", text);
            Assert.DoesNotContain("Continued", text);
        }

        [Fact]
        public void ExportFileNames_DistinguishModuleAndScope()
        {
            var result = _analytics.QueryReports(new ReportsQueryDto
            {
                Timeframe = ReportTimeframe.Week,
                AnchorDate = Anchor,
                Module = AnalyticsModule.Financial
            });

            Assert.EndsWith(".csv", ReportExportService.SuggestedFileName(result), StringComparison.Ordinal);
            Assert.EndsWith(".pdf", ReportPdfService.SuggestedFileName(result), StringComparison.Ordinal);
            Assert.Contains("financial", ReportPdfService.SuggestedFileName(result), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetDashboardOverviewApi_MatchesContractStructure()
        {
            var apiResponse = _analytics.GetDashboardOverviewApi("today", Anchor);

            Assert.Equal("today", apiResponse.Timeframe);
            Assert.Equal("2026-09-13", apiResponse.OperatingWeekRange.Start); // Sunday
            Assert.Equal("2026-09-19", apiResponse.OperatingWeekRange.End);   // Saturday

            Assert.True(apiResponse.LifecycleSummary.OrdersPlaced > 0);
            Assert.True(apiResponse.LifecycleSummary.Produced > 0);
            Assert.True(apiResponse.LifecycleSummary.Loaded > 0);
            Assert.True(apiResponse.LifecycleSummary.Sold > 0);
            Assert.True(apiResponse.LifecycleSummary.FillRatePct > 0);
            Assert.True(apiResponse.LifecycleSummary.SellThroughPct > 0);

            Assert.Equal(3, apiResponse.Categories.Count);
            var categoryIds = apiResponse.Categories.Select(c => c.CategoryId).ToList();
            Assert.Contains("bread", categoryIds);
            Assert.Contains("water", categoryIds);
            Assert.Contains("popcorn", categoryIds);

            Assert.True(apiResponse.TenderSplit.Cash >= 0);
            Assert.True(apiResponse.TenderSplit.Transfer >= 0);
            Assert.True(apiResponse.TenderSplit.Pos >= 0);

            Assert.NotEmpty(apiResponse.AttentionAlerts);
            Assert.All(apiResponse.AttentionAlerts, a =>
            {
                Assert.False(string.IsNullOrWhiteSpace(a.Type));
                Assert.False(string.IsNullOrWhiteSpace(a.Severity));
                Assert.False(string.IsNullOrWhiteSpace(a.Message));
            });
        }

        [Theory]
        [InlineData("today", "today")]
        [InlineData("weekly", "weekly")]
        [InlineData("monthly", "monthly")]
        [InlineData("yearly", "yearly")]
        public void GetDashboardOverviewApi_SupportsAllTimeframes(string inputTf, string expectedTf)
        {
            var apiResponse = _analytics.GetDashboardOverviewApi(inputTf, Anchor);
            Assert.Equal(expectedTf, apiResponse.Timeframe);
            Assert.NotEmpty(apiResponse.Categories);
            Assert.True(apiResponse.LifecycleSummary.Sold > 0);
        }

        [Fact]
        public void GetDashboardOverviewApi_SerializesToExpectedContractJson()
        {
            var apiResponse = _analytics.GetDashboardOverviewApi("today", Anchor);
            var jsonString = System.Text.Json.JsonSerializer.Serialize(apiResponse);
            using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("timeframe", out _));
            Assert.True(root.TryGetProperty("operating_week_range", out var owr));
            Assert.True(owr.TryGetProperty("start", out _));
            Assert.True(owr.TryGetProperty("end", out _));

            Assert.True(root.TryGetProperty("lifecycle_summary", out var ls));
            Assert.True(ls.TryGetProperty("orders_placed", out _));
            Assert.True(ls.TryGetProperty("produced", out _));
            Assert.True(ls.TryGetProperty("loaded", out _));
            Assert.True(ls.TryGetProperty("sold", out _));
            Assert.True(ls.TryGetProperty("returned", out _));
            Assert.True(ls.TryGetProperty("damaged", out _));
            Assert.True(ls.TryGetProperty("fill_rate_pct", out _));
            Assert.True(ls.TryGetProperty("sell_through_pct", out _));
            Assert.True(ls.TryGetProperty("damage_rate_pct", out _));
            Assert.True(ls.TryGetProperty("collection_rate_pct", out _));

            Assert.True(root.TryGetProperty("categories", out var cats));
            Assert.True(cats.GetArrayLength() > 0);
            var firstCat = cats[0];
            Assert.True(firstCat.TryGetProperty("category_id", out _));
            Assert.True(firstCat.TryGetProperty("category_name", out _));
            Assert.True(firstCat.TryGetProperty("volume_share_pct", out _));
            Assert.True(firstCat.TryGetProperty("total_sold_units", out _));
            Assert.True(firstCat.TryGetProperty("gross_revenue", out _));
            Assert.True(firstCat.TryGetProperty("outstanding_balance", out _));
            Assert.True(firstCat.TryGetProperty("lifecycle", out var cl));
            Assert.True(cl.TryGetProperty("orders", out _));
            Assert.True(cl.TryGetProperty("produced", out _));
            Assert.True(cl.TryGetProperty("loaded", out _));
            Assert.True(cl.TryGetProperty("sold", out _));
            Assert.True(cl.TryGetProperty("returned", out _));
            Assert.True(cl.TryGetProperty("damaged", out _));

            Assert.True(root.TryGetProperty("tender_split", out var ts));
            Assert.True(ts.TryGetProperty("cash", out _));
            Assert.True(ts.TryGetProperty("transfer", out _));
            Assert.True(ts.TryGetProperty("pos", out _));
            Assert.True(ts.TryGetProperty("outstanding", out _));

            Assert.True(root.TryGetProperty("attention_alerts", out var alerts));
            Assert.True(alerts.GetArrayLength() > 0);
            var firstAlert = alerts[0];
            Assert.True(firstAlert.TryGetProperty("type", out _));
            Assert.True(firstAlert.TryGetProperty("severity", out _));
            Assert.True(firstAlert.TryGetProperty("message", out _));
        }

        // =============================================================
        // Helpers
        // =============================================================

        private ReportResultDto Query(DateTime anchor, ReportTimeframe timeframe, ReportGroupBy groupBy) =>
            _analytics.QueryReports(new ReportsQueryDto
            {
                Timeframe = timeframe,
                AnchorDate = anchor,
                GroupBy = groupBy
            });

        private static int CountDataLines(ReportResultDto result, string csv)
        {
            // Trim the CR: the exporter writes CRLF, and a "\r" segment is not an
            // empty string, so the blank spacer lines would otherwise be counted.
            var lines = csv.Split('\n')
                .Select(l => l.TrimEnd('\r'))
                .Where(l => l.Length > 0)
                .ToArray();

            var headerIndex = Array.FindIndex(lines, l => l.StartsWith("Range,", StringComparison.Ordinal));
            var totalIndex = Array.FindIndex(lines, l => l.StartsWith("TOTAL", StringComparison.Ordinal));

            return Math.Max(0, totalIndex - headerIndex - 1);
        }

        private static List<(int Offset, string ObjectNumber)> ReadXrefOffsets(string pdf)
        {
            var xrefStart = pdf.LastIndexOf("xref\n0 ", StringComparison.Ordinal);
            Assert.True(xrefStart > 0, "The PDF should carry an xref table.");

            var lines = pdf[xrefStart..].Split('\n');
            // Line 0 is the "xref" keyword, line 1 the "first count" subsection
            // header, and the entries for object N follow on line N + 2.
            var count = int.Parse(lines[1].Split(' ')[1]);

            var offsets = new List<(int, string)>();
            for (var objectNumber = 1; objectNumber < count; objectNumber++)
            {
                var entry = lines[objectNumber + 2];
                if (entry.Contains(" f ")) continue; // the free-list head
                offsets.Add((int.Parse(entry[..10]), objectNumber.ToString()));
            }

            return offsets;
        }
    }
}
