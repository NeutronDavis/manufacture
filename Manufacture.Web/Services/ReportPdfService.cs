using System.Globalization;
using System.Text;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;

namespace Manufacture.Services
{
    /// <summary>
    /// Renders a <see cref="ReportResultDto"/> as a formatted, paginated PDF.
    ///
    /// Layout, column selection and value formatting all come from
    /// <see cref="ReportColumnCatalog"/> — the same metadata the on-screen table
    /// and the CSV export use — so all three outputs can never drift apart.
    /// </summary>
    public static class ReportPdfService
    {
        private const double MarginX = 40d;
        private const double HeaderHeight = 74d;
        private const double FooterHeight = 34d;
        private const double CellPaddingX = 5d;
        private const double RowHeight = 15d;
        private const double HeaderRowHeight = 19d;
        private const double TileHeight = 44d;

        private static readonly PdfColor Ink = PdfColor.FromBytes(24, 24, 27);
        private static readonly PdfColor Muted = PdfColor.FromBytes(113, 113, 122);
        private static readonly PdfColor Hairline = PdfColor.FromBytes(228, 228, 231);
        private static readonly PdfColor Zebra = PdfColor.FromBytes(250, 250, 250);
        private static readonly PdfColor Band = PdfColor.FromBytes(39, 39, 42);
        private static readonly PdfColor TileFill = PdfColor.FromBytes(245, 245, 245);
        private static readonly PdfColor WarningFill = PdfColor.FromBytes(254, 243, 199);
        private static readonly PdfColor ErrorFill = PdfColor.FromBytes(254, 226, 226);
        private static readonly PdfColor Accent = PdfColor.FromBytes(180, 83, 9);

        /// <summary>Renders the report, letting each page hold as many rows as physically fit.</summary>
        public static byte[] BuildPdf(ReportResultDto result) => BuildPdf(result, 0);

        /// <summary>
        /// Renders the report, optionally capping the rows a single page will hold.
        /// Pass 0 for <paramref name="maxRowsPerPage"/> to use the physical page
        /// height; a positive cap forces an early break, which is how the
        /// continuation header and repeated table header get exercised.
        /// </summary>
        public static byte[] BuildPdf(ReportResultDto result, int maxRowsPerPage)
        {
            var doc = new PdfDocument();
            var contentWidth = doc.PageWidth - (MarginX * 2);
            var rows = result.AllRows.Count > 0 ? result.AllRows : result.Rows;
            var layout = BuildColumnLayout(result.Columns, contentWidth);

            var page = doc.AddPage();
            var cursor = DrawHeader(page, result, contentWidth);
            cursor = DrawMeta(page, result, cursor);
            cursor = DrawTiles(page, result, cursor, contentWidth);
            cursor = DrawFlagLegend(page, cursor);

            var tableBottom = doc.PageHeight - FooterHeight;
            var y = cursor + 10d;
            y = DrawTableHeader(page, result, layout, y, contentWidth);

            var index = 0;
            var onPage = 0;
            foreach (var row in rows)
            {
                if ((maxRowsPerPage > 0 && onPage >= maxRowsPerPage) || y + RowHeight > tableBottom)
                {
                    DrawFooter(page, result, doc.PageCount);
                    page = doc.AddPage();
                    y = DrawTableHeader(page, result, layout, MarginX * 0.6, contentWidth, repeat: true);
                    onPage = 0;
                }

                y = DrawTableRow(page, row, layout, y, contentWidth, index++);
                onPage++;
            }

            // The closing totals band gets a fresh page rather than being clipped.
            // The row cap deliberately does not apply here: it paces the table, and
            // the band is only a few points tall.
            if (y + 26d > tableBottom)
            {
                DrawFooter(page, result, doc.PageCount);
                page = doc.AddPage();
                y = DrawTableHeader(page, result, layout, MarginX * 0.6, contentWidth, repeat: true);
            }

            DrawTotalsRow(page, result, layout, y, contentWidth);
            DrawFooter(page, result, doc.PageCount);

            return doc.Build();
        }

        public static string SuggestedFileName(ReportResultDto result)
        {
            var module = result.Module.ToString().ToLowerInvariant();
            var stamp = result.Query.AnchorDate.ToString("yyyyMMdd");
            var scope = result.Query.ChartGroupByRep ? "reps" : result.Query.GroupBy.ToString().ToLowerInvariant();
            return $"bakeryflow-{module}-{scope}-{stamp}.pdf";
        }

        // =============================================================
        // Page furniture
        // =============================================================

        private static double DrawHeader(PdfPage page, ReportResultDto result, double contentWidth)
        {
            page.FillRect(0, 0, page.Width, HeaderHeight, Band);
            page.FillRect(0, HeaderHeight - 3, page.Width, 3, Accent);

            page.DrawText("BakeryFlow ERP", PdfFont.Bold, 9, PdfColor.FromBytes(253, 186, 116),
                MarginX, 22);

            page.DrawText(result.ModuleLabel, PdfFont.Bold, 17, PdfColor.FromBytes(250, 250, 250),
                MarginX, 44);

            page.DrawText("North Plant  |  " + result.Query.RangeLabel, PdfFont.Regular, 9,
                PdfColor.FromBytes(161, 161, 170), MarginX, 60);

            page.DrawText("Generated " + DateTime.UtcNow.ToString("dd MMM yyyy HH:mm") + " UTC",
                PdfFont.Regular, 8, PdfColor.FromBytes(161, 161, 170),
                page.Width - MarginX, 44, PdfAlign.Right);

            page.DrawText(result.GroupByLabel + " grouping", PdfFont.Bold, 8,
                PdfColor.FromBytes(161, 161, 170), page.Width - MarginX, 58, PdfAlign.Right);

            return HeaderHeight + 18d;
        }

        private static double DrawMeta(PdfPage page, ReportResultDto result, double y)
        {
            var entries = new (string Label, string Value)[]
            {
                ("Timeframe", result.Query.Timeframe.ToString()),
                ("Range", result.Query.RangeLabel),
                ("Rows", result.TotalRows.ToString("N0")),
                ("Clamped", result.Query.IsClamped ? "Yes (to date)" : "No")
            };

            var colWidth = (page.Width - (MarginX * 2)) / entries.Length;
            for (var i = 0; i < entries.Length; i++)
            {
                var x = MarginX + (i * colWidth);
                page.DrawText(entries[i].Label.ToUpperInvariant(), PdfFont.Bold, 6.5, Muted, x, y);
                page.DrawText(entries[i].Value, PdfFont.Bold, 9, Ink, x, y + 12);
            }

            y += 20d;
            page.StrokeLine(MarginX, y, page.Width - MarginX, y, Hairline);

            // Filter chips, wrapped onto as many lines as the labels need.
            if (result.ActiveFilterLabels.Count > 0)
            {
                y += 13d;
                page.DrawText("ACTIVE FILTERS", PdfFont.Bold, 6.5, Muted, MarginX, y);
                y += 10d;

                var x = MarginX + 4d;
                const double chipPadding = 7d;
                const double chipGap = 5d;
                var chipHeight = 13d;

                foreach (var label in result.ActiveFilterLabels)
                {
                    var text = PdfEncoding.Sanitise(label);
                    var chipWidth = PdfEncoding.Measure(text, PdfFont.Regular, 7.5) + (chipPadding * 2);
                    if (x + chipWidth > page.Width - MarginX)
                    {
                        x = MarginX + 4d;
                        y += chipHeight + 3d;
                    }

                    page.FillRect(x, y - 9d, chipWidth, chipHeight, TileFill);
                    page.DrawText(text, PdfFont.Regular, 7.5, Ink, x + chipPadding, y, PdfAlign.Left);
                    x += chipWidth + chipGap;
                }

                y += chipHeight + 4d;
            }

            return y;
        }

        private static double DrawTiles(PdfPage page, ReportResultDto result, double y, double contentWidth)
        {
            var tiles = Tiles(result);
            if (tiles.Count == 0) return y;

            y += 8d;
            const int perRow = 5;
            const double gap = 6d;
            var tileWidth = (contentWidth - (gap * (perRow - 1))) / perRow;

            for (var i = 0; i < tiles.Count; i++)
            {
                var row = i / perRow;
                var col = i % perRow;
                var x = MarginX + (col * (tileWidth + gap));
                var top = y + (row * (TileHeight + gap));

                page.FillRect(x, top, tileWidth, TileHeight, TileFill);
                page.FillRect(x, top, 2.5, TileHeight, Accent);

                page.DrawText(tiles[i].Label.ToUpperInvariant(), PdfFont.Bold, 6, Muted, x + 8, top + 14);
                page.DrawText(Truncate(tiles[i].Value, PdfFont.Bold, 12, tileWidth - 14), PdfFont.Bold, 12,
                    Ink, x + 8, top + 30);
                if (!string.IsNullOrEmpty(tiles[i].Note))
                {
                    page.DrawText(Truncate(tiles[i].Note, PdfFont.Regular, 6.5, tileWidth - 14),
                        PdfFont.Regular, 6.5, Muted, x + 8, top + 39);
                }
            }

            var rows = (int)Math.Ceiling(tiles.Count / (double)perRow);
            return y + (rows * (TileHeight + gap));
        }

        private static List<(string Label, string Value, string Note)> Tiles(ReportResultDto result)
        {
            var totals = result.GrandTotals;
            string Pct(decimal v) => v.ToString("0.#") + "%";

            switch (result.Module)
            {
                case AnalyticsModule.Financial:
                    return new List<(string, string, string)>
                    {
                        ("Expected revenue", Money(totals.ExpectedRevenue), "Invoiced on sold units"),
                        ("Cash collected", Money(result.Tender.Cash), "Counter and spot cash"),
                        ("Transfer collected", Money(result.Tender.BankTransfer), "Direct bank inflows"),
                        ("POS bank", Money(result.Tender.PosBank), "Card settlement"),
                        ("Outstanding", Money(totals.OutstandingBalance), "Still owed on rep accounts"),
                        ("Collection rate", Pct(totals.CollectionRatePercent), "Collected vs invoiced"),
                        ("Sales tickets", totals.TicketCount.ToString("N0"), "Tickets behind the volume"),
                        ("Units sold", totals.Sold.ToString("N0"), "Across the filtered scope"),
                        ("Returned", totals.Returned.ToString("N0"), "Closing stock off the routes"),
                        ("Damaged", totals.Damaged.ToString("N0"), "Written off on return")
                    };

                case AnalyticsModule.Logistics:
                {
                    var fleet = result.LogisticsSummary.Rows;
                    decimal Sum(Func<LogisticsPointDto, decimal> selector) => fleet.Sum(selector);
                    decimal Avg(Func<LogisticsPointDto, decimal> selector, decimal fallback) =>
                        fleet.Count == 0 ? fallback : Math.Round(fleet.Average(selector), 1);

                    return new List<(string, string, string)>
                    {
                        ("Vehicles", fleet.Count.ToString("N0"), "On the selected routes"),
                        ("Trips", Sum(r => r.Trips).ToString("N0"), "Deliveries completed"),
                        ("Distance", Sum(r => r.DistanceKm).ToString("N0") + " km", "Total covered"),
                        ("Litres", Sum(r => r.Litres).ToString("N0") + " L", "Diesel consumed"),
                        ("Fuel cost", Money(Sum(r => r.FuelCost)), "Pump receipts"),
                        ("Maintenance", Money(Sum(r => r.MaintenanceCost)), "Breakdown spend"),
                        ("Trip expenses", Money(Sum(r => r.TripExpenses)), "Loading and tolls"),
                        ("On-time", Avg(r => r.OnTimePercent, 0).ToString("0.#") + "%", "Fleet average"),
                        ("Efficiency", Avg(r => r.DeliveryEfficiencyIndex, 0).ToString("N0"), "Units per 100 km"),
                        ("Cost / unit sold", Money(Avg(r => r.CostPerUnitSold, 0)), "Fleet average")
                    };
                }

                default:
                    return new List<(string, string, string)>
                    {
                        ("Requested", totals.OrdersPlaced.ToString("N0"), "Reps placed orders"),
                        ("Produced", totals.Produced.ToString("N0"), "Out of the ovens"),
                        ("Loaded", totals.Loaded.ToString("N0"), "Released at the bay"),
                        ("Sold", totals.Sold.ToString("N0"), "Converted on the route"),
                        ("Returned", totals.Returned.ToString("N0"), "Closing stock"),
                        ("Damaged", totals.Damaged.ToString("N0"), "Written off"),
                        ("Fill rate", Pct(totals.FillRatePercent), "Produced vs requested"),
                        ("Sell-through", Pct(totals.SellThroughPercent), "Sold vs loaded"),
                        ("Damage rate", Pct(totals.DamagePercent), "Damaged vs returned"),
                        ("Revenue", Money(totals.ExpectedRevenue), "Expected from sold units")
                    };
            }
        }

        private static double DrawFlagLegend(PdfPage page, double y)
        {
            var entries = new (PdfColor Fill, PdfColor Ink, string Text)[]
            {
                (WarningFill, PdfColor.FromBytes(120, 53, 15), "Warning: shortfall, high closing stock or unsettled account"),
                (ErrorFill, PdfColor.FromBytes(153, 27, 27), "Error: damage or variance above tolerance")
            };

            var x = MarginX;
            foreach (var entry in entries)
            {
                page.FillRect(x, y, 8, 8, entry.Fill);
                page.StrokeLine(x, y, x + 8, y + 8, entry.Ink, 0.4);
                page.StrokeLine(x + 8, y, x, y + 8, entry.Ink, 0.4);

                page.DrawText(entry.Text, PdfFont.Regular, 6.5, Muted, x + 11, y + 6.5);
                x += 11d + PdfEncoding.Measure(entry.Text, PdfFont.Regular, 6.5) + 16d;
            }

            return y + 12d;
        }

        private static void DrawFooter(PdfPage page, ReportResultDto result, int pageNumber)
        {
            var y = page.Height - 22d;
            page.StrokeLine(MarginX, y - 12d, page.Width - MarginX, y - 12d, Hairline);

            page.DrawText("BakeryFlow ERP  |  " + result.ModuleLabel + "  |  " + result.Query.RangeLabel,
                PdfFont.Regular, 7, Muted, MarginX, y);

            page.DrawText("Page " + pageNumber.ToString(CultureInfo.InvariantCulture),
                PdfFont.Bold, 7, Muted, page.Width - MarginX, y, PdfAlign.Right);
        }

        // =============================================================
        // Table
        // =============================================================

        private readonly record struct ColumnLayout(ReportColumnDto Column, double X, double Width);

        private static List<ColumnLayout> BuildColumnLayout(List<ReportColumnDto> columns, double contentWidth)
        {
            // A leading "Range" column matches the CSV header so both files line up.
            var widths = new List<double> { 62d };
            foreach (var column in columns)
            {
                var sample = column.Kind switch
                {
                    ReportColumnKind.Money => "N999,999,999",
                    ReportColumnKind.Percent => "999.9%",
                    ReportColumnKind.Quantity => "999,999",
                    ReportColumnKind.Alert => "Damage above tolerance",
                    _ => "Column heading"
                };

                widths.Add(Math.Max(
                    PdfEncoding.Measure(PdfEncoding.Sanitise(column.Label), PdfFont.Bold, 7.5),
                    PdfEncoding.Measure(PdfEncoding.Sanitise(sample), PdfFont.Regular, 8)) + (CellPaddingX * 2));
            }

            var total = widths.Sum();
            if (total > contentWidth)
            {
                // Squeeze proportionally, but never below a readable minimum.
                var floor = widths.Select((_, i) => i == 0 ? 48d : 40d).ToArray();
                var floorTotal = floor.Sum();
                var slack = contentWidth - floorTotal;

                if (slack > 0)
                {
                    var excess = total - floorTotal;
                    for (var i = 0; i < widths.Count; i++)
                    {
                        widths[i] = floor[i] + (slack * ((widths[i] - floor[i]) / excess));
                    }
                }
                else
                {
                    for (var i = 0; i < widths.Count; i++) widths[i] = floor[i];
                }
            }
            else if (total < contentWidth)
            {
                // Absorb the slack into the text column so the table fills the page.
                widths[0] += contentWidth - total;
            }

            var layout = new List<ColumnLayout>();
            var x = MarginX;
            var headers = new[] { "Range" }.Concat(columns.Select(c => c.Label)).ToList();

            layout.Add(new ColumnLayout(new ReportColumnDto { Key = "Range", Kind = ReportColumnKind.Text, Label = headers[0] }, x, widths[0]));
            x += widths[0];

            for (var i = 0; i < columns.Count; i++)
            {
                layout.Add(new ColumnLayout(columns[i], x, widths[i + 1]));
                x += widths[i + 1];
            }

            return layout;
        }

        private static double DrawTableHeader(
            PdfPage page, ReportResultDto result, List<ColumnLayout> layout, double y, double contentWidth, bool repeat = false)
        {
            page.FillRect(MarginX, y, contentWidth, HeaderRowHeight, Band);

            foreach (var column in layout)
            {
                var align = column.Column.IsNumeric ? PdfAlign.Right : PdfAlign.Left;
                var textX = align == PdfAlign.Right ? column.X + column.Width - CellPaddingX : column.X + CellPaddingX;
                var label = PdfEncoding.Sanitise(column.Column.Label.ToUpperInvariant());
                page.DrawText(Truncate(label, PdfFont.Bold, 7.5, column.Width - (CellPaddingX * 2)),
                    PdfFont.Bold, 7.5, PdfColor.FromBytes(250, 250, 250), textX, y + 12.5, align);
            }

            var next = y + HeaderRowHeight;
            if (repeat)
            {
                // No parentheses here: they would be escaped inside the PDF literal
                // string, and they clutter a table header that is already dense.
                page.DrawText("Continued", PdfFont.Italic, 6.5, Muted, page.Width - MarginX, next + 9, PdfAlign.Right);
            }

            return next;
        }

        private static double DrawTableRow(
            PdfPage page, ReportRowDto row, List<ColumnLayout> layout, double y, double contentWidth, int index = 0)
        {
            PdfColor? alertFill = row.AlertLevel switch
            {
                ReportAlertLevel.Error => ErrorFill,
                ReportAlertLevel.Warning => WarningFill,
                _ => null
            };

            if (alertFill.HasValue)
            {
                page.FillRect(MarginX, y, contentWidth, RowHeight, alertFill.Value);
                page.FillRect(MarginX, y, 2.5, RowHeight,
                    row.AlertLevel == ReportAlertLevel.Error
                        ? PdfColor.FromBytes(185, 28, 28)
                        : PdfColor.FromBytes(202, 138, 4));
            }
            else if (index % 2 == 1)
            {
                page.FillRect(MarginX, y, contentWidth, RowHeight, Zebra);
            }

            var ink = alertFill.HasValue ? PdfColor.FromBytes(69, 26, 3) : Ink;

            foreach (var column in layout)
            {
                var align = column.Column.IsNumeric ? PdfAlign.Right : PdfAlign.Left;
                var textX = align == PdfAlign.Right ? column.X + column.Width - CellPaddingX : column.X + CellPaddingX;
                var raw = column.Column.Key == "Range"
                    ? row.EntitySubLabel
                    : ReportColumnCatalog.FormatValue(row, column.Column.Key, column.Column);

                var text = PdfEncoding.Sanitise(raw);
                page.DrawText(Truncate(text, PdfFont.Regular, 8, column.Width - (CellPaddingX * 2)),
                    PdfFont.Regular, 8, ink, textX, y + 10.5, align);
            }

            page.StrokeLine(MarginX, y + RowHeight, page.Width - MarginX, y + RowHeight, Hairline, 0.35);
            return y + RowHeight;
        }

        private static void DrawTotalsRow(
            PdfPage page, ReportResultDto result, List<ColumnLayout> layout, double y, double contentWidth)
        {
            y += 4d;
            page.FillRect(MarginX, y, contentWidth, RowHeight + 3d, Band);

            var totals = ReportTotals.For(result);
            foreach (var column in layout)
            {
                var align = column.Column.IsNumeric ? PdfAlign.Right : PdfAlign.Left;
                var textX = align == PdfAlign.Right ? column.X + column.Width - CellPaddingX : column.X + CellPaddingX;
                var raw = column.Column.Key == "Range"
                    ? "TOTAL"
                    : totals.TryGetValue(column.Column.Key, out var value) ? value : string.Empty;

                var text = PdfEncoding.Sanitise(raw);
                page.DrawText(Truncate(text, PdfFont.Bold, 8, column.Width - (CellPaddingX * 2)),
                    PdfFont.Bold, 8, PdfColor.FromBytes(250, 250, 250), textX, y + 13.5, align);
            }
        }

        // =============================================================
        // Text helpers
        // =============================================================

        private static string Money(decimal value) => ReportColumnCatalog.FormatMoney(value);

        private static string Truncate(string text, PdfFont font, double size, double maxWidth)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 4) return string.Empty;

            if (PdfEncoding.Measure(text, font, size) <= maxWidth) return text;

            var sb = new StringBuilder(text);
            while (sb.Length > 1 && PdfEncoding.Measure(sb.ToString() + "..", font, size) > maxWidth)
            {
                sb.Length--;
            }

            return sb + "..";
        }
    }
}
