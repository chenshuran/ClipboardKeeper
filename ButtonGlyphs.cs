using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ClipboardKeeper
{
    internal static class ButtonGlyphs
    {
        public const int ButtonWidth = 34;
        public const int ButtonHeight = 30;

        private const int IconSize = 18;
        private const float IconCanvas = 20.0f;
        private static readonly Dictionary<ButtonGlyphKind, Image> Images = new Dictionary<ButtonGlyphKind, Image>();

        public static void Apply(Button button, ButtonGlyphKind kind, string tooltipText, ToolTip toolTip)
        {
            button.Text = string.Empty;
            GlyphButton glyphButton = button as GlyphButton;
            if (glyphButton != null)
            {
                glyphButton.GlyphKind = kind;
                button.Image = null;
            }
            else
            {
                button.Image = ProfessionalIcons.GetImage(GetResourceName(kind)) ?? Get(kind);
            }

            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.TextImageRelation = TextImageRelation.Overlay;
            button.AutoSize = false;
            button.Width = ButtonWidth;
            button.Height = ButtonHeight;
            button.Margin = new Padding(0, 0, 8, 0);
            button.Padding = new Padding(0);
            button.FlatStyle = FlatStyle.Standard;
            button.UseVisualStyleBackColor = true;
            button.BackColor = SystemColors.Control;
            button.ForeColor = SystemColors.ControlText;
            button.AccessibleName = tooltipText;

            if (toolTip != null)
            {
                toolTip.SetToolTip(button, tooltipText);
            }
        }

        private static Image Get(ButtonGlyphKind kind)
        {
            lock (Images)
            {
                Image image;
                if (!Images.TryGetValue(kind, out image))
                {
                    image = Create(kind);
                    Images.Add(kind, image);
                }

                return image;
            }
        }

        private static Image Create(ButtonGlyphKind kind)
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.Clear(Color.Transparent);
                graphics.ScaleTransform(IconSize / IconCanvas, IconSize / IconCanvas);

                switch (kind)
                {
                    case ButtonGlyphKind.Copy:
                        DrawCopy(graphics);
                        break;
                    case ButtonGlyphKind.Edit:
                        DrawEdit(graphics);
                        break;
                    case ButtonGlyphKind.Star:
                        DrawStar(graphics, false);
                        break;
                    case ButtonGlyphKind.Unstar:
                        DrawStar(graphics, true);
                        break;
                    case ButtonGlyphKind.Delete:
                        DrawDelete(graphics);
                        break;
                    case ButtonGlyphKind.ClearAll:
                        DrawClearAll(graphics);
                        break;
                    case ButtonGlyphKind.Pause:
                        DrawPause(graphics);
                        break;
                    case ButtonGlyphKind.Resume:
                        DrawResume(graphics);
                        break;
                    case ButtonGlyphKind.Folder:
                        DrawFolder(graphics);
                        break;
                    case ButtonGlyphKind.Exit:
                        DrawExit(graphics);
                        break;
                    case ButtonGlyphKind.MainWindow:
                        DrawMainWindow(graphics);
                        break;
                    case ButtonGlyphKind.Check:
                        DrawCheck(graphics);
                        break;
                }
            }

            return bitmap;
        }

        public static void Draw(Graphics graphics, ButtonGlyphKind kind, Rectangle bounds, bool enabled)
        {
            if (ProfessionalIcons.Draw(graphics, GetResourceName(kind), bounds, enabled))
            {
                return;
            }

            GraphicsState state = graphics.Save();
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.TranslateTransform(bounds.Left, bounds.Top);
            graphics.ScaleTransform(bounds.Width / IconCanvas, bounds.Height / IconCanvas);

            switch (kind)
            {
                case ButtonGlyphKind.Copy:
                    DrawCopy(graphics);
                    break;
                case ButtonGlyphKind.Edit:
                    DrawEdit(graphics);
                    break;
                case ButtonGlyphKind.Star:
                    DrawStar(graphics, false);
                    break;
                case ButtonGlyphKind.Unstar:
                    DrawStar(graphics, true);
                    break;
                case ButtonGlyphKind.Delete:
                    DrawDelete(graphics);
                    break;
                case ButtonGlyphKind.ClearAll:
                    DrawClearAll(graphics);
                    break;
                case ButtonGlyphKind.Pause:
                    DrawPause(graphics);
                    break;
                case ButtonGlyphKind.Resume:
                    DrawResume(graphics);
                    break;
                case ButtonGlyphKind.Folder:
                    DrawFolder(graphics);
                    break;
                case ButtonGlyphKind.Exit:
                    DrawExit(graphics);
                    break;
                case ButtonGlyphKind.MainWindow:
                    DrawMainWindow(graphics);
                    break;
                case ButtonGlyphKind.Check:
                    DrawCheck(graphics);
                    break;
            }

            graphics.Restore(state);

            if (!enabled)
            {
                using (var overlay = new SolidBrush(Color.FromArgb(120, SystemColors.Control)))
                {
                    graphics.FillRectangle(overlay, bounds);
                }
            }
        }

        private static string GetResourceName(ButtonGlyphKind kind)
        {
            switch (kind)
            {
                case ButtonGlyphKind.Copy:
                    return "copy";
                case ButtonGlyphKind.Edit:
                    return "pencil-fill";
                case ButtonGlyphKind.Star:
                    return "star-fill";
                case ButtonGlyphKind.Unstar:
                    return "star";
                case ButtonGlyphKind.Delete:
                    return "trash3-fill";
                case ButtonGlyphKind.ClearAll:
                    return "eraser-fill";
                case ButtonGlyphKind.Pause:
                    return "pause-fill";
                case ButtonGlyphKind.Resume:
                    return "play-fill";
                case ButtonGlyphKind.Folder:
                    return "folder2-open";
                case ButtonGlyphKind.Exit:
                    return "box-arrow-right";
                case ButtonGlyphKind.MainWindow:
                    return "window";
                case ButtonGlyphKind.Check:
                    return "check-circle-fill";
                default:
                    return null;
            }
        }

        private static void DrawCopy(Graphics graphics)
        {
            using (var backBrush = new SolidBrush(Color.FromArgb(209, 250, 229)))
            using (var frontBrush = new SolidBrush(Color.White))
            using (var accentBrush = new SolidBrush(Color.FromArgb(20, 184, 166)))
            using (var borderPen = new Pen(Color.FromArgb(13, 148, 136), 1.5f))
            {
                FillRoundedRectangle(graphics, backBrush, new RectangleF(3, 2, 10, 13), 2.5f);
                DrawRoundedRectangle(graphics, borderPen, new RectangleF(3, 2, 10, 13), 2.5f);
                FillRoundedRectangle(graphics, frontBrush, new RectangleF(7, 5, 10, 13), 2.5f);
                DrawRoundedRectangle(graphics, borderPen, new RectangleF(7, 5, 10, 13), 2.5f);
                graphics.FillRectangle(accentBrush, 9, 9, 6, 1.5f);
                graphics.FillRectangle(accentBrush, 9, 12, 5, 1.5f);
            }
        }

        private static void DrawEdit(Graphics graphics)
        {
            using (var bodyPen = new Pen(Color.FromArgb(245, 158, 11), 4.0f))
            using (var edgePen = new Pen(Color.FromArgb(15, 118, 110), 1.8f))
            using (var tipBrush = new SolidBrush(Color.FromArgb(31, 41, 55)))
            {
                bodyPen.StartCap = LineCap.Round;
                bodyPen.EndCap = LineCap.Round;
                graphics.DrawLine(bodyPen, 5, 15, 14, 6);
                graphics.DrawLine(edgePen, 5, 15, 14, 6);
                graphics.FillPolygon(tipBrush, new[]
                {
                    new PointF(14, 4),
                    new PointF(17, 7),
                    new PointF(14, 8)
                });
            }
        }

        private static void DrawStar(Graphics graphics, bool withSlash)
        {
            PointF[] points = CreateStarPoints(new PointF(10, 10), 8.0f, 3.8f);
            using (var fill = new SolidBrush(Color.FromArgb(250, 204, 21)))
            using (var outline = new Pen(Color.FromArgb(217, 119, 6), 1.3f))
            {
                graphics.FillPolygon(fill, points);
                graphics.DrawPolygon(outline, points);
            }

            if (withSlash)
            {
                using (var slashPen = new Pen(Color.FromArgb(239, 68, 68), 2.4f))
                {
                    slashPen.StartCap = LineCap.Round;
                    slashPen.EndCap = LineCap.Round;
                    graphics.DrawLine(slashPen, 4, 16, 16, 4);
                }
            }
        }

        private static void DrawDelete(Graphics graphics)
        {
            using (var binBrush = new SolidBrush(Color.FromArgb(254, 226, 226)))
            using (var accentBrush = new SolidBrush(Color.FromArgb(239, 68, 68)))
            using (var pen = new Pen(Color.FromArgb(185, 28, 28), 1.5f))
            {
                graphics.FillRectangle(accentBrush, 6, 4, 8, 2);
                graphics.DrawLine(pen, 4, 7, 16, 7);
                FillRoundedRectangle(graphics, binBrush, new RectangleF(6, 8, 9, 9), 2.0f);
                DrawRoundedRectangle(graphics, pen, new RectangleF(6, 8, 9, 9), 2.0f);
                graphics.DrawLine(pen, 9, 10, 9, 15);
                graphics.DrawLine(pen, 12, 10, 12, 15);
            }
        }

        private static void DrawClearAll(Graphics graphics)
        {
            using (var eraserBrush = new SolidBrush(Color.FromArgb(251, 207, 232)))
            using (var edgeBrush = new SolidBrush(Color.FromArgb(34, 211, 238)))
            using (var pen = new Pen(Color.FromArgb(190, 24, 93), 1.5f))
            using (var sweepPen = new Pen(Color.FromArgb(14, 165, 233), 2.0f))
            {
                graphics.TranslateTransform(2, 1);
                graphics.RotateTransform(-18);
                graphics.FillRectangle(eraserBrush, 3, 8, 10, 6);
                graphics.FillRectangle(edgeBrush, 11, 8, 4, 6);
                graphics.DrawRectangle(pen, 3, 8, 12, 6);
                graphics.ResetTransform();
                sweepPen.StartCap = LineCap.Round;
                sweepPen.EndCap = LineCap.Round;
                graphics.DrawLine(sweepPen, 4, 17, 15, 17);
            }
        }

        private static void DrawPause(Graphics graphics)
        {
            using (var brush = new SolidBrush(Color.FromArgb(14, 116, 144)))
            {
                FillRoundedRectangle(graphics, brush, new RectangleF(5, 4, 4, 12), 1.5f);
                FillRoundedRectangle(graphics, brush, new RectangleF(11, 4, 4, 12), 1.5f);
            }
        }

        private static void DrawResume(Graphics graphics)
        {
            using (var brush = new SolidBrush(Color.FromArgb(22, 163, 74)))
            using (var pen = new Pen(Color.FromArgb(21, 128, 61), 1.4f))
            {
                PointF[] triangle =
                {
                    new PointF(6, 4),
                    new PointF(16, 10),
                    new PointF(6, 16)
                };
                graphics.FillPolygon(brush, triangle);
                graphics.DrawPolygon(pen, triangle);
            }
        }

        private static void DrawFolder(Graphics graphics)
        {
            using (var tabBrush = new SolidBrush(Color.FromArgb(253, 186, 116)))
            using (var bodyBrush = new SolidBrush(Color.FromArgb(251, 191, 36)))
            using (var accentBrush = new SolidBrush(Color.FromArgb(34, 211, 238)))
            using (var pen = new Pen(Color.FromArgb(180, 83, 9), 1.4f))
            {
                graphics.FillPolygon(tabBrush, new[]
                {
                    new PointF(3, 6),
                    new PointF(8, 6),
                    new PointF(10, 8),
                    new PointF(17, 8),
                    new PointF(17, 10),
                    new PointF(3, 10)
                });
                FillRoundedRectangle(graphics, bodyBrush, new RectangleF(3, 8, 14, 9), 2.0f);
                DrawRoundedRectangle(graphics, pen, new RectangleF(3, 8, 14, 9), 2.0f);
                graphics.FillEllipse(accentBrush, 12, 11, 3, 3);
            }
        }

        private static void DrawExit(Graphics graphics)
        {
            using (var doorPen = new Pen(Color.FromArgb(55, 65, 81), 1.6f))
            using (var arrowPen = new Pen(Color.FromArgb(239, 68, 68), 2.0f))
            {
                arrowPen.StartCap = LineCap.Round;
                arrowPen.EndCap = LineCap.Round;
                graphics.DrawRectangle(doorPen, 4, 4, 7, 12);
                graphics.DrawLine(arrowPen, 9, 10, 17, 10);
                graphics.DrawLines(arrowPen, new[]
                {
                    new Point(14, 7),
                    new Point(17, 10),
                    new Point(14, 13)
                });
            }
        }

        private static void DrawMainWindow(Graphics graphics)
        {
            using (var fill = new SolidBrush(Color.FromArgb(219, 234, 254)))
            using (var header = new SolidBrush(Color.FromArgb(59, 130, 246)))
            using (var pen = new Pen(Color.FromArgb(37, 99, 235), 1.4f))
            using (var arrowPen = new Pen(Color.FromArgb(22, 163, 74), 1.8f))
            {
                FillRoundedRectangle(graphics, fill, new RectangleF(3, 4, 14, 12), 2.0f);
                graphics.FillRectangle(header, 4, 5, 12, 3);
                DrawRoundedRectangle(graphics, pen, new RectangleF(3, 4, 14, 12), 2.0f);
                arrowPen.StartCap = LineCap.Round;
                arrowPen.EndCap = LineCap.Round;
                graphics.DrawLine(arrowPen, 8, 13, 14, 13);
                graphics.DrawLines(arrowPen, new[]
                {
                    new Point(12, 10),
                    new Point(15, 13),
                    new Point(12, 16)
                });
            }
        }

        private static void DrawCheck(Graphics graphics)
        {
            using (var fill = new SolidBrush(Color.FromArgb(34, 197, 94)))
            using (var checkPen = new Pen(Color.White, 2.4f))
            {
                graphics.FillEllipse(fill, 2, 2, 16, 16);
                checkPen.StartCap = LineCap.Round;
                checkPen.EndCap = LineCap.Round;
                graphics.DrawLines(checkPen, new[]
                {
                    new PointF(5.5f, 10.0f),
                    new PointF(8.5f, 13.0f),
                    new PointF(14.5f, 6.5f)
                });
            }
        }

        private static PointF[] CreateStarPoints(PointF center, float outer, float inner)
        {
            PointF[] points = new PointF[10];
            for (int i = 0; i < points.Length; i++)
            {
                double angle = -Math.PI / 2.0 + i * Math.PI / 5.0;
                float radius = i % 2 == 0 ? outer : inner;
                points[i] = new PointF(
                    center.X + (float)Math.Cos(angle) * radius,
                    center.Y + (float)Math.Sin(angle) * radius);
            }

            return points;
        }

        private static void FillRoundedRectangle(Graphics graphics, Brush brush, RectangleF rectangle, float radius)
        {
            using (GraphicsPath path = CreateRoundedRectangle(rectangle, radius))
            {
                graphics.FillPath(brush, path);
            }
        }

        private static void DrawRoundedRectangle(Graphics graphics, Pen pen, RectangleF rectangle, float radius)
        {
            using (GraphicsPath path = CreateRoundedRectangle(rectangle, radius))
            {
                graphics.DrawPath(pen, path);
            }
        }

        private static GraphicsPath CreateRoundedRectangle(RectangleF rectangle, float radius)
        {
            float diameter = radius * 2.0f;
            var path = new GraphicsPath();
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
