using System.Globalization;
using System.Text;

namespace Manufacture.Services
{
    /// <summary>The three base-14 PDF fonts the exporter draws with.</summary>
    public enum PdfFont
    {
        Regular = 0,
        Bold = 1,
        Italic = 2
    }

    /// <summary>An RGB colour with components in the 0..1 range.</summary>
    public readonly struct PdfColor
    {
        public PdfColor(double r, double g, double b)
        {
            R = r;
            G = g;
            B = b;
        }

        public double R { get; }
        public double G { get; }
        public double B { get; }

        /// <summary>Builds a colour from "#rrggbb" or "#rgb"; returns black on bad input.</summary>
        public static PdfColor FromHex(string hex)
        {
            var raw = (hex ?? string.Empty).TrimStart('#');
            if (raw.Length == 3)
            {
                raw = string.Concat(raw[0], raw[0], raw[1], raw[1], raw[2], raw[2]);
            }

            if (raw.Length != 6 ||
                !int.TryParse(raw[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
                !int.TryParse(raw.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
                !int.TryParse(raw.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            {
                return new PdfColor(0, 0, 0);
            }

            return new PdfColor(r / 255d, g / 255d, b / 255d);
        }

        public static PdfColor FromBytes(int r, int g, int b) => new(r / 255d, g / 255d, b / 255d);
    }

    /// <summary>Horizontal alignment for <see cref="PdfPage.DrawText"/>.</summary>
    public enum PdfAlign
    {
        Left = 0,
        Center = 1,
        Right = 2
    }

    /// <summary>
    /// A very small PDF 1.4 writer: just enough of the format to lay out a
    /// paginated, ruled and shaded report without pulling in a PDF library.
    ///
    /// Coordinates are expressed top-down (y = 0 is the top edge of the page)
    /// because that is how report layouts are naturally written; the writer
    /// flips to PDF's bottom-left origin when it emits the content stream.
    /// </summary>
    public sealed class PdfPage
    {
        private readonly List<string> _ops = new();

        internal PdfPage(double width, double height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>Page width in points (A4 portrait = 595.28).</summary>
        public double Width { get; }

        /// <summary>Page height in points (A4 portrait = 841.89).</summary>
        public double Height { get; }

        /// <summary>Draws a single line of text with <paramref name="x"/>/<paramref name="y"/> as the baseline.</summary>
        public PdfPage DrawText(
            string text,
            PdfFont font,
            double size,
            PdfColor color,
            double x,
            double y,
            PdfAlign align = PdfAlign.Left)
        {
            var sanitised = PdfEncoding.Sanitise(text);
            if (sanitised.Length == 0) return this;

            var width = PdfEncoding.Measure(sanitised, font, size);
            var drawX = align switch
            {
                PdfAlign.Center => x - (width / 2d),
                PdfAlign.Right => x - width,
                _ => x
            };

            var baseline = Height - y;
            _ops.Add(
                "q " + ColorOp(color) +
                " BT /F" + ((int)font + 1) + " " + Fmt(size) + " Tf" +
                " 1 0 0 1 " + Fmt(drawX) + " " + Fmt(baseline) + " Tm" +
                " (" + PdfEncoding.Escape(sanitised) + ") Tj ET Q");

            return this;
        }

        /// <summary>Fills a rectangle whose top-left corner is at (<paramref name="x"/>, <paramref name="y"/>).</summary>
        public PdfPage FillRect(double x, double y, double w, double h, PdfColor color)
        {
            var bottom = Height - y - h;
            _ops.Add("q " + ColorOp(color) + " " + Fmt(x) + " " + Fmt(bottom) + " " + Fmt(w) + " " + Fmt(h) + " re f Q");
            return this;
        }

        /// <summary>Strokes a straight line.</summary>
        public PdfPage StrokeLine(double x1, double y1, double x2, double y2, PdfColor color, double lineWidth = 0.5)
        {
            _ops.Add("q " + ColorOp(color) + " " + Fmt(lineWidth) + " w " +
                     Fmt(x1) + " " + Fmt(Height - y1) + " m " +
                     Fmt(x2) + " " + Fmt(Height - y2) + " l S Q");
            return this;
        }

        internal string ContentStream() => string.Join("\n", _ops) + "\n";

        private static string ColorOp(PdfColor c) => Fmt(c.R) + " " + Fmt(c.G) + " " + Fmt(c.B) + " rg";

        internal static string Fmt(double value) =>
            Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>Accumulates pages and serialises them as a single PDF file.</summary>
    public sealed class PdfDocument
    {
        private const double A4Width = 595.28;
        private const double A4Height = 841.89;

        private readonly List<PdfPage> _pages = new();

        public double PageWidth => A4Width;
        public double PageHeight => A4Height;

        public PdfPage AddPage() => AddPage(A4Width, A4Height);

        public PdfPage AddPage(double width, double height)
        {
            var page = new PdfPage(width, height);
            _pages.Add(page);
            return page;
        }

        public int PageCount => _pages.Count;

        /// <summary>
        /// Serialises the document. Object numbering is fixed and simple:
        /// 1 catalog, 2 page tree, 3-5 fonts, then content/page pairs.
        /// </summary>
        public byte[] Build()
        {
            if (_pages.Count == 0) AddPage();

            var objects = new List<string>();

            // Page objects first so we know their ids, then fill in the tree.
            var pageIds = new int[_pages.Count];
            var firstPageObject = 6;
            for (var i = 0; i < _pages.Count; i++)
            {
                pageIds[i] = firstPageObject + (i * 2) + 1;
            }

            var catalogId = 1;
            var pagesId = 2;

            objects.Add(catalogId.ToString(CultureInfo.InvariantCulture) +
                        " 0 obj\n<< /Type /Catalog /Pages " + pagesId + " 0 R >>\nendobj\n");

            var kids = string.Join(" ", pageIds.Select(id => id + " 0 R"));
            objects.Add(pagesId.ToString(CultureInfo.InvariantCulture) +
                        " 0 obj\n<< /Type /Pages /Kids [" + kids + "] /Count " + _pages.Count + " >>\nendobj\n");

            objects.Add(FontObject(3, "Helvetica"));
            objects.Add(FontObject(4, "Helvetica-Bold"));
            objects.Add(FontObject(5, "Helvetica-Oblique"));

            var streams = new Dictionary<int, byte[]>();

            for (var i = 0; i < _pages.Count; i++)
            {
                var contentId = firstPageObject + (i * 2);
                var bytes = Latin1(_pages[i].ContentStream());
                streams[contentId] = bytes;

                // The body is spliced in as raw bytes when the file is written;
                // a byte[] interpolated into a string would corrupt the stream.
                objects.Add(contentId.ToString(CultureInfo.InvariantCulture) + " 0 obj\n<< /Length " +
                            bytes.Length + " >>\nstream\n" + StreamPlaceholder + "endstream\nendobj\n");

                objects.Add(pageIds[i].ToString(CultureInfo.InvariantCulture) +
                            " 0 obj\n<< /Type /Page /Parent " + pagesId + " 0 R" +
                            " /MediaBox [0 0 " + PdfPage.Fmt(_pages[i].Width) + " " + PdfPage.Fmt(_pages[i].Height) + "]" +
                            " /Resources << /Font << /F1 3 0 R /F2 4 0 R /F3 5 0 R >> >>" +
                            " /Contents " + contentId + " 0 R >>\nendobj\n");
            }

            var output = new MemoryStream();
            void Write(string text)
            {
                var data = Latin1(text);
                output.Write(data);
            }

            Write("%PDF-1.4\n");
            // Binary comment marks the file as containing 8-bit data.
            output.Write(new byte[] { (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n' });

            var offsets = new long[objects.Count + 1];
            for (var i = 0; i < objects.Count; i++)
            {
                offsets[i + 1] = output.Position;

                // Splice each content stream in as raw bytes rather than
                // round-tripping it through a string.
                var text = objects[i];
                var marker = text.IndexOf(StreamPlaceholder, StringComparison.Ordinal);
                if (marker < 0)
                {
                    Write(text);
                }
                else
                {
                    Write(text[..marker]);
                    if (streams.TryGetValue(i + 1, out var body)) output.Write(body);
                    Write(text[(marker + StreamPlaceholder.Length)..]);
                }
            }

            var xrefPosition = output.Position;
            var sb = new StringBuilder();
            sb.Append("xref\n0 ").Append(objects.Count + 1).Append('\n');
            sb.Append("0000000000 65535 f \n");
            for (var i = 1; i <= objects.Count; i++)
            {
                sb.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
            }

            sb.Append("trailer\n<< /Size ").Append(objects.Count + 1)
              .Append(" /Root ").Append(catalogId).Append(" 0 R >>\n");
            sb.Append("startxref\n").Append(xrefPosition).Append("\n%%EOF\n");

            Write(sb.ToString());
            return output.ToArray();
        }

        /// <summary>Placeholder marking where a raw content stream belongs.</summary>
        private const string StreamPlaceholder = "<<RAWSTREAM>>";

        private static string FontObject(int id, string name) =>
            id.ToString(CultureInfo.InvariantCulture) +
            " 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /" + name +
            " /Encoding /WinAnsiEncoding >>\nendobj\n";

        private static byte[] Latin1(string text)
        {
            var bytes = new byte[text.Length];
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                bytes[i] = c <= 0xFF ? (byte)c : (byte)'?';
            }

            return bytes;
        }
    }

    /// <summary>
    /// WinAnsi text handling: the base-14 fonts have no embedded glyph data, so
    /// unsupported characters are folded to ASCII before they reach the page.
    /// Widths come from the Adobe base-14 metrics so text can be measured for
    /// column sizing and right-alignment.
    /// </summary>
    internal static class PdfEncoding
    {
        // Adobe base-14 advance widths (1/1000 em) for code points 32..126.
        private static readonly int[] RegularWidths =
        {
            278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
            556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
            1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778,
            667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
            333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
            556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584
        };

        private static readonly int[] BoldWidths =
        {
            278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
            556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611,
            975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778,
            667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556,
            333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611,
            611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584
        };

        /// <summary>Folds a string into printable WinAnsi, dropping what cannot be represented.</summary>
        public static string Sanitise(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                switch (ch)
                {
                    // Typography the report format produces that WinAnsi lacks a glyph for.
                    case '₦': sb.Append("N"); break;       // naira sign -> ASCII fallback
                    case '–': case '—': sb.Append('-'); break;
                    case '‘': case '’': sb.Append('\''); break;
                    case '“': case '”': sb.Append('"'); break;
                    case '…': sb.Append("..."); break;
                    case '•': sb.Append('-'); break;
                    case '\t': sb.Append(' '); break;
                    case '\r':
                    case '\n': sb.Append(' '); break;
                    default:
                        if (ch is >= ' ' and <= '~') sb.Append(ch);
                        else if (ch <= 0xFF) sb.Append(ch);              // Latin-1 supplement
                        else if (WinAnsiHigh.TryGetValue(ch, out var code)) sb.Append((char)code);
                        else sb.Append('?');
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>Code points above Latin-1 that WinAnsi places in 0x80..0x9F.</summary>
        private static readonly Dictionary<char, byte> WinAnsiHigh = new()
        {
            ['€'] = 0x80, ['‚'] = 0x82, ['ƒ'] = 0x83, ['„'] = 0x84,
            ['…'] = 0x85, ['†'] = 0x86, ['‡'] = 0x87, ['‰'] = 0x89,
            ['‹'] = 0x8B, ['‘'] = 0x91, ['’'] = 0x92, ['“'] = 0x93,
            ['”'] = 0x94, ['•'] = 0x95, ['–'] = 0x96, ['—'] = 0x97,
            ['›'] = 0x9B
        };

        /// <summary>Advances the pen by the rendered width of <paramref name="text"/>.</summary>
        public static double Measure(string text, PdfFont font, double size)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var widths = font == PdfFont.Bold ? BoldWidths : RegularWidths;
            var total = 0;
            foreach (var ch in text)
            {
                var index = ch - 32;
                total += index >= 0 && index < widths.Length ? widths[index] : 556;
            }

            return total / 1000d * size;
        }

        /// <summary>Escapes a string for use inside a PDF literal string.</summary>
        public static string Escape(string text) =>
            text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
