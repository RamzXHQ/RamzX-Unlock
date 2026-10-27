// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MiftawerkUnlock
{
    /// <summary>RamzX colours and the RamzX logo, drawn as vectors (sharp at every size and DPI).</summary>
    internal static class Brand
    {
        internal static readonly Color Navy = Color.FromArgb(11, 18, 32);
        internal static readonly Color Cyan = Color.FromArgb(41, 182, 246);
        internal static readonly Color Blue = Color.FromArgb(37, 99, 235);
        internal static readonly Color Text = Color.FromArgb(17, 24, 39);
        internal static readonly Color Muted = Color.FromArgb(100, 110, 128);
        internal static readonly Color Card = Color.FromArgb(244, 247, 253);
        internal static readonly Color Border = Color.FromArgb(214, 222, 238);
        internal static readonly Color Ok = Color.FromArgb(22, 163, 74);
        internal static readonly Color Warn = Color.FromArgb(217, 119, 6);
        internal static readonly Color Error = Color.FromArgb(220, 38, 38);

        internal const string Website = "https://ramzx.de";

        // The "X" of the RamzX logo: two chevrons on a 460 x 460 grid (from the RamzX avatar).
        private static readonly PointF[] LeftMark =
        {
            new PointF(140, 175), new PointF(180, 175), new PointF(218, 230),
            new PointF(152, 322), new PointF(112, 322), new PointF(178, 230)
        };
        private static readonly PointF[] RightMark =
        {
            new PointF(310, 138), new PointF(348, 138), new PointF(282, 230),
            new PointF(320, 285), new PointF(282, 285), new PointF(242, 230)
        };

        /// <summary>The RamzX "X" in the brand gradient, fitted into <paramref name="bounds"/>.</summary>
        internal static void DrawMark(Graphics g, RectangleF bounds)
        {
            // The marks span x 112..348 and y 138..322 of the 460 grid; centre them.
            const float minX = 112, maxX = 348, minY = 138, maxY = 322;
            float s = Math.Min(bounds.Width / (maxX - minX), bounds.Height / (maxY - minY));
            float ox = bounds.Left + (bounds.Width - (maxX - minX) * s) / 2f - minX * s;
            float oy = bounds.Top + (bounds.Height - (maxY - minY) * s) / 2f - minY * s;

            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new PointF(ox + minX * s, oy + minY * s), new PointF(ox + maxX * s, oy + maxY * s), Cyan, Blue))
            {
                foreach (PointF[] mark in new[] { LeftMark, RightMark })
                {
                    PointF[] p = new PointF[mark.Length];
                    for (int i = 0; i < p.Length; ++i) p[i] = new PointF(ox + mark[i].X * s, oy + mark[i].Y * s);
                    g.FillPolygon(b, p);
                }
            }
            g.SmoothingMode = old;
        }

        /// <summary>App icon: rounded navy square with the RamzX mark.</summary>
        internal static Bitmap Logo(int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (GraphicsPath p = RoundRect(new RectangleF(0, 0, size - 1, size - 1), size * 0.22f))
                using (SolidBrush b = new SolidBrush(Navy))
                    g.FillPath(b, p);
                float pad = size * 0.2f;
                DrawMark(g, new RectangleF(pad, pad, size - 2 * pad, size - 2 * pad));
            }
            return bmp;
        }

        /// <summary>Menu icon in KeePass's small-icon size (DPI-aware).</summary>
        internal static Bitmap MenuIcon(Control dpiReference)
        {
            int size = 16;
            try { using (Graphics g = dpiReference.CreateGraphics()) size = (int)Math.Round(16 * g.DpiX / 96f); }
            catch (Exception) { }
            return Logo(Math.Max(16, size));
        }

        internal static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            float d = Math.Max(1f, radius * 2);
            GraphicsPath p = new GraphicsPath();
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>Face / fingerprint symbol for the Windows Hello question (line art in the brand blue).</summary>
        internal static Bitmap HelloSymbol(int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                float w = Math.Max(2f, size / 16f);
                using (LinearGradientBrush lb = new LinearGradientBrush(new Point(0, 0), new Point(size, size), Cyan, Blue))
                using (Pen pen = new Pen(lb, w) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    float m = size * 0.08f, c = size * 0.26f; // corner brackets of a face scan
                    g.DrawLines(pen, new[] { new PointF(m, m + c), new PointF(m, m), new PointF(m + c, m) });
                    g.DrawLines(pen, new[] { new PointF(size - m - c, m), new PointF(size - m, m), new PointF(size - m, m + c) });
                    g.DrawLines(pen, new[] { new PointF(m, size - m - c), new PointF(m, size - m), new PointF(m + c, size - m) });
                    g.DrawLines(pen, new[] { new PointF(size - m - c, size - m), new PointF(size - m, size - m), new PointF(size - m, size - m - c) });
                    // eyes, nose, smile
                    g.DrawLine(pen, size * 0.36f, size * 0.36f, size * 0.36f, size * 0.44f);
                    g.DrawLine(pen, size * 0.64f, size * 0.36f, size * 0.64f, size * 0.44f);
                    g.DrawLines(pen, new[] { new PointF(size * 0.5f, size * 0.38f), new PointF(size * 0.5f, size * 0.55f), new PointF(size * 0.45f, size * 0.55f) });
                    g.DrawArc(pen, size * 0.34f, size * 0.5f, size * 0.32f, size * 0.18f, 20, 140);
                }
            }
            return bmp;
        }
    }

    internal enum PillKind { Neutral, Ok, Warn, Error }

    /// <summary>Rounded status line with a coloured symbol (same look as RamzX OTP).</summary>
    internal sealed class StatusPill : Control
    {
        private PillKind m_kind;

        internal StatusPill()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        internal void Set(PillKind kind, string text)
        {
            m_kind = kind;
            Text = text;
            Height = PreferredHeight(Width);
            Invalidate();
        }

        private int Pad { get { return (int)(Font.Height * 0.55f); } }
        private int Dot { get { return (int)Math.Ceiling(Font.Height * 1.05f); } }

        /// <summary>Text area width - the same for measuring and drawing, so no line is cut off.</summary>
        private int TextWidth(int width) { return Math.Max(10, width - Pad * 3 - Dot); }

        private TextFormatFlags Flags
        {
            get
            {
                return TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl |
                    (RightToLeft == RightToLeft.Yes ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : 0);
            }
        }

        internal int PreferredHeight(int width)
        {
            Size s = TextRenderer.MeasureText(Text ?? string.Empty, Font, new Size(TextWidth(width), 0), Flags);
            return Math.Max(s.Height, Dot) + Pad * 2;
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); FitHeight(); }
        protected override void OnParentChanged(EventArgs e) { base.OnParentChanged(e); FitHeight(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); FitHeight(); }
        protected override void OnRightToLeftChanged(EventArgs e) { base.OnRightToLeftChanged(e); FitHeight(); }

        // Font and reading direction come from the dialog once the pill is added to it.
        private void FitHeight()
        {
            int h = PreferredHeight(Width);
            if (h != Height) Height = h;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color c = m_kind == PillKind.Ok ? Brand.Ok : m_kind == PillKind.Warn ? Brand.Warn
                : m_kind == PillKind.Error ? Brand.Error : Brand.Blue;

            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (GraphicsPath p = Brand.RoundRect(r, Math.Min(Height / 2f, Font.Height * 0.8f)))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(24, c)))
            using (Pen border = new Pen(Color.FromArgb(80, c)))
            {
                g.FillPath(bg, p);
                g.DrawPath(border, p);
            }

            int d = Dot;
            bool rtl = RightToLeft == RightToLeft.Yes;
            RectangleF dot = new RectangleF(rtl ? Width - Pad - d : Pad, Pad, d, d);
            using (SolidBrush b = new SolidBrush(c)) g.FillEllipse(b, dot);
            string glyph = m_kind == PillKind.Ok ? "✓" : m_kind == PillKind.Neutral ? "i" : "!";
            using (Font gf = new Font(Font, FontStyle.Bold))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(glyph, gf, Brushes.White, dot, sf);

            Rectangle text = new Rectangle(rtl ? Pad : Pad * 2 + d, Pad, TextWidth(Width), Height - Pad * 2);
            TextRenderer.DrawText(g, Text, Font, text, Brand.Text, Flags);
        }
    }

    /// <summary>Small section heading with an accent bar.</summary>
    internal sealed class SectionLabel : Label
    {
        internal SectionLabel(string text, Font baseFont)
        {
            Text = text;
            AutoSize = true;
            Font = new Font(baseFont, FontStyle.Bold);
            ForeColor = Brand.Text;
            Padding = new Padding((int)(baseFont.Height * 0.6f), 0, 0, 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            int w = Math.Max(3, Font.Height / 6);
            bool rtl = RightToLeft == RightToLeft.Yes;
            using (SolidBrush b = new SolidBrush(Brand.Blue))
                e.Graphics.FillRectangle(b, rtl ? Width - w : 0, 2, w, Height - 4);
        }
    }
}
