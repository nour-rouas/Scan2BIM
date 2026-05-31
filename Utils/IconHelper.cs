using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace Metrika.Utils
{
    /// <summary>
    /// Generates toolbar icons procedurally — no external image files needed.
    /// All BitmapImages are Frozen (thread-safe, GC-friendly).
    /// Use size=32 for LargeImage, size=16 for Image (stacked buttons).
    /// </summary>
    internal static class IconHelper
    {
        private static readonly Color Blue   = Color.FromArgb(45,  105, 178);
        private static readonly Color LtBlue = Color.FromArgb(110, 165, 220);
        private static readonly Color Red    = Color.FromArgb(210,  50,  45);

        public static BitmapImage GeneratePoints(int sz)  => Build(DrawGeneratePoints,  sz);
        public static BitmapImage DeformFloor(int sz)     => Build(DrawDeformFloor,     sz);
        public static BitmapImage MeasureTopo(int sz)     => Build(DrawMeasureTopo,     sz);
        public static BitmapImage HideUnhide(int sz)      => Build(DrawHideUnhide,      sz);
        public static BitmapImage MeasureCloud(int sz)    => Build(DrawMeasureCloud,    sz);
        public static BitmapImage MeasureElement(int sz)  => Build(DrawMeasureElement,  sz);
        public static BitmapImage ExportMarkers(int sz)   => Build(DrawExportMarkers,   sz);
        public static BitmapImage ImportMarkers(int sz)   => Build(DrawImportMarkers,   sz);

        // ── Factory ──────────────────────────────────────────────────────────
        private static BitmapImage Build(Action<Graphics, float> draw, int sz)
        {
            using (Bitmap bmp = new Bitmap(sz, sz, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode    = SmoothingMode.AntiAlias;
                g.CompositingMode  = CompositingMode.SourceOver;
                g.Clear(Color.Transparent);

                float s = sz / 32f; // coordinate scale factor
                draw(g, s);

                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    ms.Position = 0;
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = ms;
                    bi.CacheOption  = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
            }
        }

        // ── 1. Generate Elevation Points ─────────────────────────────────────
        // 3×3 dot grid above a flat floor baseline
        private static void DrawGeneratePoints(Graphics g, float s)
        {
            using (Pen   p = new Pen(Blue, s * 1.8f))
            using (SolidBrush b = new SolidBrush(Blue))
            {
                // Floor baseline
                g.DrawLine(p, 3*s, 27*s, 29*s, 27*s);
                // 3×3 dots
                int[] xs = { 8, 16, 24 };
                int[] ys = { 6, 13, 21 };
                foreach (int x in xs)
                    foreach (int y in ys)
                        g.FillEllipse(b, (x - 2.5f)*s, (y - 2.5f)*s, 5*s, 5*s);
            }
        }

        // ── 2. Deform Floor ──────────────────────────────────────────────────
        // Sine wave showing deformed surface + downward arrow
        private static void DrawDeformFloor(Graphics g, float s)
        {
            using (Pen pw = new Pen(Blue, s * 2f) { LineJoin = LineJoin.Round })
            using (Pen pa = new Pen(Blue, s * 2f))
            {
                pa.EndCap = LineCap.ArrowAnchor;
                // Sine wave across the icon
                PointF[] wave = new PointF[17];
                for (int i = 0; i < 17; i++)
                {
                    double t = i / 16.0 * Math.PI * 2.0;
                    wave[i] = new PointF((4 + i * 1.5f) * s,
                                         (13 - (float)Math.Sin(t) * 5f) * s);
                }
                g.DrawCurve(pw, wave, 0.4f);
                // Arrow below indicating push-down
                g.DrawLine(pa, 16*s, 20*s, 16*s, 29*s);
            }
        }

        // ── 3. Measure Topography ────────────────────────────────────────────
        // Mountain silhouette + red crosshair at the left peak
        private static void DrawMeasureTopo(Graphics g, float s)
        {
            PointF[] mtn =
            {
                new PointF( 2*s, 28*s),
                new PointF(12*s,  7*s),
                new PointF(20*s, 18*s),
                new PointF(25*s, 11*s),
                new PointF(30*s, 28*s),
            };
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(150, LtBlue)))
            using (Pen        pp   = new Pen(Blue, s * 2f) { LineJoin = LineJoin.Round })
            using (Pen        pc   = new Pen(Red, s * 1.8f)
                                         { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.FillPolygon(fill, mtn);
                g.DrawLines(pp, mtn);
                float cx = 12*s, cy = 7*s, r = 4*s;
                g.DrawLine(pc, cx - r, cy,   cx + r, cy);
                g.DrawLine(pc, cx,   cy - r, cx,   cy + r);
            }
        }

        // ── 4. Hide / Unhide Point Cloud ─────────────────────────────────────
        // Eye shape (flat ellipse + pupil) + red diagonal strikethrough
        private static void DrawHideUnhide(Graphics g, float s)
        {
            using (Pen        pe = new Pen(Blue, s * 2f))
            using (SolidBrush bp = new SolidBrush(Blue))
            using (Pen        ps = new Pen(Red, s * 2.5f)
                                       { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                // Eye outline (flat ellipse = almond silhouette)
                g.DrawEllipse(pe, 5*s, 10*s, 22*s, 12*s);
                // Pupil
                g.FillEllipse(bp, 12.5f*s, 12.5f*s, 7*s, 7*s);
                // Red "no" diagonal from bottom-left to top-right
                g.DrawLine(ps, 5*s, 27*s, 27*s, 5*s);
            }
        }

        // ── 5. Measure Point in Cloud ────────────────────────────────────────
        // Scattered light dots (cloud) + precise crosshair with red centre dot
        private static void DrawMeasureCloud(Graphics g, float s)
        {
            float[] dx = {  5, 10, 14, 21, 26,  8, 19, 25,  6, 16 };
            float[] dy = {  7,  5, 10,  6, 11, 15, 14, 19, 22, 21 };
            using (SolidBrush bd  = new SolidBrush(Color.FromArgb(190, LtBlue)))
            using (Pen        pc  = new Pen(Blue, s * 1.5f))
            using (SolidBrush bcd = new SolidBrush(Red))
            {
                for (int i = 0; i < dx.Length; i++)
                    g.FillEllipse(bd, (dx[i] - 2)*s, (dy[i] - 2)*s, 4*s, 4*s);

                // Crosshair (4 tick lines)
                g.DrawLine(pc, 16*s,  6*s, 16*s, 12*s);
                g.DrawLine(pc, 16*s, 20*s, 16*s, 26*s);
                g.DrawLine(pc,  6*s, 16*s, 12*s, 16*s);
                g.DrawLine(pc, 20*s, 16*s, 26*s, 16*s);
                // Centre dot
                g.FillEllipse(bcd, 14*s, 14*s, 4*s, 4*s);
            }
        }

        // ── 6. Measure Element Face ──────────────────────────────────────────
        // Shaded floor/wall slab + red arrow pointing at surface
        private static void DrawMeasureElement(Graphics g, float s)
        {
            using (SolidBrush bf  = new SolidBrush(Color.FromArgb(80, LtBlue)))
            using (Pen        pr  = new Pen(Blue, s * 2f))
            using (Pen        pa  = new Pen(Red, s * 2f))
            using (SolidBrush bpt = new SolidBrush(Red))
            {
                pa.EndCap = LineCap.ArrowAnchor;
                // Element slab
                g.FillRectangle(bf, 3*s, 16*s, 20*s, 9*s);
                g.DrawRectangle(pr, 3*s, 16*s, 20*s, 9*s);
                // Arrow from top-right diagonally to slab surface
                g.DrawLine(pa, 28*s, 6*s, 18*s, 16*s);
                // Contact dot at arrow tip
                g.FillEllipse(bpt, 16*s, 14*s, 4*s, 4*s);
            }
        }

        // ── 7. Export Markers ───────────────────────────────────────────────
        // Document shape + arrow outwards
        private static void DrawExportMarkers(Graphics g, float s)
        {
            using (Pen        pd = new Pen(Blue, s * 2f))
            using (SolidBrush bd = new SolidBrush(Color.FromArgb(80, LtBlue)))
            using (Pen        pa = new Pen(Red, s * 2f))
            {
                pa.EndCap = LineCap.ArrowAnchor;

                g.FillRectangle(bd, 6*s, 5*s, 16*s, 22*s);
                g.DrawRectangle(pd, 6*s, 5*s, 16*s, 22*s);
                g.DrawLine(pd, 9*s, 11*s, 19*s, 11*s);
                g.DrawLine(pd, 9*s, 16*s, 19*s, 16*s);
                g.DrawLine(pd, 9*s, 21*s, 16*s, 21*s);

                g.DrawLine(pa, 20*s, 16*s, 29*s, 16*s);
            }
        }

        // ── 8. Import Markers ───────────────────────────────────────────────
        // Document shape + arrow inwards
        private static void DrawImportMarkers(Graphics g, float s)
        {
            using (Pen        pd = new Pen(Blue, s * 2f))
            using (SolidBrush bd = new SolidBrush(Color.FromArgb(80, LtBlue)))
            using (Pen        pa = new Pen(Red, s * 2f))
            {
                pa.EndCap = LineCap.ArrowAnchor;

                g.FillRectangle(bd, 10*s, 5*s, 16*s, 22*s);
                g.DrawRectangle(pd, 10*s, 5*s, 16*s, 22*s);
                g.DrawLine(pd, 13*s, 11*s, 23*s, 11*s);
                g.DrawLine(pd, 13*s, 16*s, 23*s, 16*s);
                g.DrawLine(pd, 13*s, 21*s, 20*s, 21*s);

                g.DrawLine(pa, 8*s, 16*s, 17*s, 16*s);
            }
        }
    }
}
