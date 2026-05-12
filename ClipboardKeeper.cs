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
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int awareness);

        [STAThread]
        private static void Main()
        {
            TryEnableDpiAwareness();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ClipboardManagerForm());
        }

        private static void TryEnableDpiAwareness()
        {
            try
            {
                if (SetProcessDpiAwarenessContext(new IntPtr(-4)))
                {
                    return;
                }
            }
            catch
            {
            }

            try
            {
                SetProcessDpiAwareness(2);
                return;
            }
            catch
            {
            }

            try
            {
                SetProcessDPIAware();
            }
            catch
            {
            }
        }
    }

    internal static class UiFonts
    {
        public static Font Create(float size, FontStyle style)
        {
            try
            {
                return new Font("Microsoft YaHei UI", size, style, GraphicsUnit.Point);
            }
            catch
            {
                return new Font(SystemFonts.MessageBoxFont.FontFamily, size, style, GraphicsUnit.Point);
            }
        }
    }

    internal static class ProfessionalIcons
    {
        private const string ResourcePrefix = "ClipboardKeeper.UiIcons.";
        private static readonly Dictionary<string, Image> Images = new Dictionary<string, Image>();

        public static Image GetImage(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            lock (Images)
            {
                Image image;
                if (!Images.TryGetValue(name, out image))
                {
                    image = LoadImage(name);
                    Images[name] = image;
                }

                return image;
            }
        }

        public static bool Draw(Graphics graphics, string name, Rectangle bounds, bool enabled)
        {
            Image image = GetImage(name);
            if (image == null)
            {
                return false;
            }

            GraphicsState state = graphics.Save();
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;

            if (image.Width == bounds.Width && image.Height == bounds.Height)
            {
                graphics.DrawImageUnscaled(image, bounds.Location);
            }
            else
            {
                graphics.DrawImage(image, bounds);
            }

            graphics.Restore(state);

            if (!enabled)
            {
                using (var overlay = new SolidBrush(Color.FromArgb(135, SystemColors.Control)))
                {
                    graphics.FillRectangle(overlay, bounds);
                }
            }

            return true;
        }

        private static Image LoadImage(string name)
        {
            string resourceName = ResourcePrefix + name + ".png";
            using (Stream stream = typeof(Program).Assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                using (var image = Image.FromStream(stream))
                {
                    return new Bitmap(image);
                }
            }
        }
    }

    internal enum ButtonGlyphKind
    {
        Copy,
        Edit,
        Star,
        Unstar,
        Delete,
        ClearAll,
        Pause,
        Resume,
        Folder,
        Exit,
        MainWindow,
        Check
    }

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

    internal sealed class GlyphButton : Button
    {
        private ButtonGlyphKind glyphKind;
        private bool isHot;
        private bool isPressed;

        public GlyphButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Standard;
        }

        public ButtonGlyphKind GlyphKind
        {
            get { return glyphKind; }
            set
            {
                if (glyphKind != value)
                {
                    glyphKind = value;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHot = true;
            base.OnMouseEnter(e);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHot = false;
            isPressed = false;
            base.OnMouseLeave(e);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left)
            {
                isPressed = true;
            }

            base.OnMouseDown(mevent);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            isPressed = false;
            base.OnMouseUp(mevent);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            System.Windows.Forms.VisualStyles.PushButtonState state = System.Windows.Forms.VisualStyles.PushButtonState.Normal;
            if (!Enabled)
            {
                state = System.Windows.Forms.VisualStyles.PushButtonState.Disabled;
            }
            else if (isPressed)
            {
                state = System.Windows.Forms.VisualStyles.PushButtonState.Pressed;
            }
            else if (isHot || Focused)
            {
                state = System.Windows.Forms.VisualStyles.PushButtonState.Hot;
            }

            ButtonRenderer.DrawButton(pevent.Graphics, ClientRectangle, state);

            int iconSize = Math.Min(18, Math.Min(ClientSize.Width - 8, ClientSize.Height - 6));
            if (iconSize < 12)
            {
                iconSize = Math.Min(ClientSize.Width, ClientSize.Height);
            }

            int offset = isPressed && Enabled ? 1 : 0;
            var iconBounds = new Rectangle(
                (ClientSize.Width - iconSize) / 2 + offset,
                (ClientSize.Height - iconSize) / 2 + offset,
                iconSize,
                iconSize);

            ButtonGlyphs.Draw(pevent.Graphics, GlyphKind, iconBounds, Enabled);

            if (Focused && ShowFocusCues)
            {
                Rectangle focusBounds = Rectangle.Inflate(ClientRectangle, -4, -4);
                ControlPaint.DrawFocusRectangle(pevent.Graphics, focusBounds);
            }
        }
    }

    internal sealed class ClipboardManagerForm : Form
    {
        private const int WmClipboardUpdate = 0x031D;
        private const int TypeColumnIndex = 1;
        private const int DescriptionColumnIndex = 2;
        private const int HistoryMinRowHeight = 32;
        private const int HistoryMaxDescriptionLines = 3;
        private const int HideToTrayFadeDurationMs = 1000;
        private const int HistoryNameColumnWidth = 230;
        private const int HistoryTypeColumnWidth = 64;
        private const int HistoryDefaultDescriptionColumnWidth = 430;
        private const int HistoryImageDescriptionMinColumnWidth = 170;
        private const int HistoryImageDescriptionMaxColumnWidth = 340;
        private static readonly Color HistoryRowBackColor = Color.White;
        private static readonly Color HistoryAlternateRowBackColor = Color.FromArgb(225, 239, 255);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private readonly HistoryStore store;
        private readonly SplitContainer mainSplit;
        private readonly DataGridView historyList;
        private readonly TextBox textPreview;
        private readonly PictureBox imagePreview;
        private readonly Label previewHeader;
        private readonly Panel imagePathPanel;
        private readonly TextBox imagePathTextBox;
        private readonly Button openImagePathButton;
        private readonly RowStyle imagePathRowStyle;
        private readonly Label statusLabel;
        private readonly ComboBox typeFilterComboBox;
        private readonly Button copyButton;
        private readonly System.Windows.Forms.Timer copyFeedbackTimer;
        private readonly ToolTip toolTip;
        private readonly Button editNameButton;
        private readonly Button starButton;
        private readonly Button deleteButton;
        private readonly Button clearButton;
        private readonly Button pauseButton;
        private readonly Button openFolderButton;
        private readonly Button exitButton;
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;
        private readonly ToolStripMenuItem pauseTrayMenuItem;
        private readonly System.Windows.Forms.Timer traySingleClickTimer;
        private readonly System.Windows.Forms.Timer hideToTrayFadeTimer;
        private CompactClipboardPopup compactPopup;

        private bool isPaused;
        private bool isExiting;
        private bool hideToTrayPending;
        private bool suppressNextTraySingleClick;
        private bool isFadingToTray;
        private DateTime hideToTrayFadeStartedAt;

        public ClipboardManagerForm()
        {
            store = HistoryStore.LoadDefault();
            toolTip = new ToolTip();

            Text = "Clipboard Keeper";
            TryUseApplicationIcon();
            Font = UiFonts.Create(9.0f, FontStyle.Regular);
            Width = 1150;
            Height = 680;
            MinimumSize = new Size(940, 500);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 3;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

            var toolbar = new FlowLayoutPanel();
            toolbar.Dock = DockStyle.Fill;
            toolbar.FlowDirection = FlowDirection.LeftToRight;
            toolbar.Padding = new Padding(8, 10, 8, 8);
            toolbar.WrapContents = false;

            var filterLabel = new Label();
            filterLabel.Text = "Filter";
            filterLabel.AutoSize = false;
            filterLabel.Width = 42;
            filterLabel.Height = ButtonGlyphs.ButtonHeight;
            filterLabel.TextAlign = ContentAlignment.MiddleLeft;
            filterLabel.Margin = new Padding(0, 0, 6, 0);

            typeFilterComboBox = new ComboBox();
            typeFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            typeFilterComboBox.DrawMode = DrawMode.OwnerDrawFixed;
            typeFilterComboBox.ItemHeight = ButtonGlyphs.ButtonHeight - 6;
            typeFilterComboBox.Width = 86;
            typeFilterComboBox.Height = ButtonGlyphs.ButtonHeight;
            typeFilterComboBox.Margin = new Padding(0, 0, 12, 0);
            typeFilterComboBox.Items.Add("All");
            typeFilterComboBox.Items.Add("Text");
            typeFilterComboBox.Items.Add("Image");
            typeFilterComboBox.Items.Add("Star");
            typeFilterComboBox.SelectedIndex = 0;
            typeFilterComboBox.DrawItem += OnTypeFilterComboBoxDrawItem;

            copyButton = CreateButton("Copy selected", ButtonGlyphKind.Copy, delegate { CopySelected(); });
            copyFeedbackTimer = new System.Windows.Forms.Timer();
            copyFeedbackTimer.Interval = 1200;
            copyFeedbackTimer.Tick += delegate
            {
                copyFeedbackTimer.Stop();
                ResetCopyButtonFeedback();
            };
            editNameButton = CreateButton("Edit name", ButtonGlyphKind.Edit, delegate { EditSelectedName(); });
            starButton = CreateButton("Star", ButtonGlyphKind.Star, delegate { ToggleSelectedStar(); });
            deleteButton = CreateButton("Delete selected", ButtonGlyphKind.Delete, delegate { DeleteSelected(); });
            clearButton = CreateButton("Clear all", ButtonGlyphKind.ClearAll, delegate { ClearAll(); });
            pauseButton = CreateButton("Pause", ButtonGlyphKind.Pause, delegate { TogglePause(); });
            openFolderButton = CreateButton("Open storage", ButtonGlyphKind.Folder, delegate { OpenStorageFolder(); });
            exitButton = CreateButton("Exit", ButtonGlyphKind.Exit, delegate { ExitApplication(); });

            toolbar.Controls.Add(filterLabel);
            toolbar.Controls.Add(typeFilterComboBox);
            toolbar.Controls.Add(copyButton);
            toolbar.Controls.Add(editNameButton);
            toolbar.Controls.Add(starButton);
            toolbar.Controls.Add(deleteButton);
            toolbar.Controls.Add(clearButton);
            toolbar.Controls.Add(pauseButton);
            toolbar.Controls.Add(openFolderButton);
            toolbar.Controls.Add(exitButton);

            mainSplit = new SplitContainer();
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.SplitterDistance = 640;

            historyList = new DataGridView();
            historyList.Dock = DockStyle.Fill;
            historyList.Font = UiFonts.Create(10.0f, FontStyle.Regular);
            historyList.AllowUserToAddRows = false;
            historyList.AllowUserToDeleteRows = false;
            historyList.AllowUserToResizeColumns = false;
            historyList.AllowUserToResizeRows = false;
            historyList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            historyList.BackgroundColor = SystemColors.Window;
            historyList.BorderStyle = BorderStyle.FixedSingle;
            historyList.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            historyList.EditMode = DataGridViewEditMode.EditProgrammatically;
            historyList.MultiSelect = true;
            historyList.ReadOnly = false;
            historyList.RowHeadersVisible = false;
            historyList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            historyList.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            historyList.GridColor = Color.FromArgb(191, 219, 254);
            historyList.RowsDefaultCellStyle.BackColor = HistoryRowBackColor;
            historyList.AlternatingRowsDefaultCellStyle.BackColor = HistoryAlternateRowBackColor;

            var nameColumn = new DataGridViewTextBoxColumn();
            nameColumn.HeaderText = "Name";
            nameColumn.Name = "Name";
            nameColumn.MinimumWidth = 140;
            nameColumn.Width = HistoryNameColumnWidth;

            var typeColumn = new DataGridViewTextBoxColumn();
            typeColumn.HeaderText = "Type";
            typeColumn.Name = "Type";
            typeColumn.MinimumWidth = 48;
            typeColumn.Width = HistoryTypeColumnWidth;
            typeColumn.ReadOnly = true;

            var descriptionColumn = new DataGridViewTextBoxColumn();
            descriptionColumn.HeaderText = "Description";
            descriptionColumn.Name = "Description";
            descriptionColumn.MinimumWidth = 360;
            descriptionColumn.Width = HistoryDefaultDescriptionColumnWidth;
            descriptionColumn.ReadOnly = true;
            descriptionColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            historyList.Columns.Add(nameColumn);
            historyList.Columns.Add(typeColumn);
            historyList.Columns.Add(descriptionColumn);
            historyList.SelectionChanged += delegate { PreviewSelected(); };
            historyList.CellEndEdit += OnHistoryNameEdited;
            historyList.CellPainting += OnHistoryCellPainting;
            historyList.KeyDown += OnHistoryListKeyDown;
            historyList.CellDoubleClick += delegate { CopySelected(); };
            historyList.SizeChanged += delegate
            {
                AdjustDescriptionColumnForAvailableWidth();
                ResizeHistoryRows();
            };
            mainSplit.Panel1.Controls.Add(historyList);

            typeFilterComboBox.SelectedIndexChanged += delegate { ApplyTypeFilter(); };

            var previewPanel = new TableLayoutPanel();
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.RowCount = 3;
            previewPanel.ColumnCount = 1;
            previewPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            imagePathRowStyle = new RowStyle(SizeType.Absolute, 0);
            previewPanel.RowStyles.Add(imagePathRowStyle);
            previewPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            previewHeader = new Label();
            previewHeader.Dock = DockStyle.Fill;
            previewHeader.TextAlign = ContentAlignment.MiddleLeft;
            previewHeader.Padding = new Padding(8, 0, 8, 0);

            var imagePathLayout = new TableLayoutPanel();
            imagePathPanel = imagePathLayout;
            imagePathPanel.Dock = DockStyle.Fill;
            imagePathPanel.Padding = new Padding(8, 6, 8, 6);
            imagePathPanel.Visible = false;
            imagePathLayout.RowCount = 1;
            imagePathLayout.ColumnCount = 2;
            imagePathLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, ButtonGlyphs.ButtonHeight));
            imagePathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            imagePathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ButtonGlyphs.ButtonWidth + 8));

            openImagePathButton = CreateButton("Open image path", ButtonGlyphKind.Folder, delegate { OpenSelectedImagePath(); });
            openImagePathButton.Dock = DockStyle.Fill;
            openImagePathButton.Width = ButtonGlyphs.ButtonWidth;
            openImagePathButton.Margin = new Padding(8, 0, 0, 0);

            imagePathTextBox = new TextBox();
            imagePathTextBox.Dock = DockStyle.Fill;
            imagePathTextBox.AutoSize = false;
            imagePathTextBox.Height = ButtonGlyphs.ButtonHeight;
            imagePathTextBox.ReadOnly = true;
            imagePathTextBox.BorderStyle = BorderStyle.FixedSingle;
            imagePathTextBox.Margin = new Padding(0);

            imagePathLayout.Controls.Add(imagePathTextBox, 0, 0);
            imagePathLayout.Controls.Add(openImagePathButton, 1, 0);

            var previewSurface = new Panel();
            previewSurface.Dock = DockStyle.Fill;
            previewSurface.Padding = new Padding(8);

            imagePreview = new PictureBox();
            imagePreview.Dock = DockStyle.Fill;
            imagePreview.BackColor = Color.White;
            imagePreview.BorderStyle = BorderStyle.FixedSingle;
            imagePreview.SizeMode = PictureBoxSizeMode.Zoom;
            imagePreview.DoubleClick += delegate { OpenSelectedImageLarge(); };

            textPreview = new TextBox();
            textPreview.Dock = DockStyle.Fill;
            textPreview.Multiline = true;
            textPreview.ReadOnly = true;
            textPreview.ScrollBars = ScrollBars.Both;
            textPreview.WordWrap = true;
            textPreview.Font = UiFonts.Create(10.0f, FontStyle.Regular);

            previewSurface.Controls.Add(imagePreview);
            previewSurface.Controls.Add(textPreview);

            previewPanel.Controls.Add(previewHeader, 0, 0);
            previewPanel.Controls.Add(imagePathPanel, 0, 1);
            previewPanel.Controls.Add(previewSurface, 0, 2);
            mainSplit.Panel2.Controls.Add(previewPanel);

            statusLabel = new Label();
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.Padding = new Padding(8, 0, 8, 0);

            root.Controls.Add(toolbar, 0, 0);
            root.Controls.Add(mainSplit, 0, 1);
            root.Controls.Add(statusLabel, 0, 2);
            Controls.Add(root);

            pauseTrayMenuItem = new ToolStripMenuItem("Pause", null, delegate { TogglePause(); });
            trayMenu = CreateTrayMenu(pauseTrayMenuItem);
            traySingleClickTimer = new System.Windows.Forms.Timer();
            traySingleClickTimer.Interval = Math.Max(150, SystemInformation.DoubleClickTime);
            traySingleClickTimer.Tick += delegate
            {
                traySingleClickTimer.Stop();
                if (suppressNextTraySingleClick)
                {
                    suppressNextTraySingleClick = false;
                    return;
                }

                ShowCompactPopupNearCursor();
            };
            hideToTrayFadeTimer = new System.Windows.Forms.Timer();
            hideToTrayFadeTimer.Interval = 16;
            hideToTrayFadeTimer.Tick += delegate { UpdateHideToTrayFade(); };
            trayIcon = CreateTrayIcon(trayMenu);
            compactPopup = CreateCompactPopup();

            RefreshHistoryList(null);
            UpdateButtonState();
            UpdateStatus("Monitoring. Local-only. Up to 100 text and image items.");
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            BeginInvoke(new MethodInvoker(CaptureClipboardSafely));
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AddClipboardFormatListener(Handle);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            RemoveClipboardFormatListener(Handle);
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmClipboardUpdate)
            {
                if (!isPaused)
                {
                    CaptureClipboardSafely();
                }
            }

            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized && !isExiting)
            {
                QueueHideToTrayAfterMinimize();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!isExiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                BeginHideToTrayFade();
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposePreviewImage();
                if (compactPopup != null && !compactPopup.IsDisposed)
                {
                    compactPopup.Dispose();
                }

                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }

                if (trayMenu != null)
                {
                    trayMenu.Dispose();
                }

                if (copyFeedbackTimer != null)
                {
                    copyFeedbackTimer.Dispose();
                }

                if (traySingleClickTimer != null)
                {
                    traySingleClickTimer.Dispose();
                }

                if (hideToTrayFadeTimer != null)
                {
                    hideToTrayFadeTimer.Dispose();
                }

                if (toolTip != null)
                {
                    toolTip.Dispose();
                }

            }

            base.Dispose(disposing);
        }

        private void TryUseApplicationIcon()
        {
            try
            {
                Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    Icon = appIcon;
                }
            }
            catch
            {
            }
        }

        private ContextMenuStrip CreateTrayMenu(ToolStripMenuItem pauseItem)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Quick Panel", null, delegate { ShowCompactPopupNearCursor(); });
            menu.Items.Add("Open Full Window", null, delegate { ShowFullWindow(); });
            menu.Items.Add(pauseItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate { ExitApplication(); });
            return menu;
        }

        private NotifyIcon CreateTrayIcon(ContextMenuStrip menu)
        {
            var icon = new NotifyIcon();
            icon.Text = "Clipboard Keeper";
            icon.Icon = Icon == null ? SystemIcons.Application : Icon;
            icon.ContextMenuStrip = menu;
            icon.Visible = true;
            icon.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (suppressNextTraySingleClick)
                    {
                        suppressNextTraySingleClick = false;
                        traySingleClickTimer.Stop();
                    }
                    else
                    {
                        traySingleClickTimer.Stop();
                        traySingleClickTimer.Start();
                    }
                }
            };

            icon.DoubleClick += delegate
            {
                suppressNextTraySingleClick = true;
                traySingleClickTimer.Stop();
                ShowFullWindow();
            };
            return icon;
        }

        private CompactClipboardPopup CreateCompactPopup()
        {
            return new CompactClipboardPopup(
                store,
                Icon,
                CopyRecord,
                ToggleRecordStar,
                DeleteRecord,
                ShowFullWindow,
                ExitApplication);
        }

        private CompactClipboardPopup EnsureCompactPopup()
        {
            if (compactPopup == null || compactPopup.IsDisposed)
            {
                compactPopup = CreateCompactPopup();
            }

            return compactPopup;
        }

        private Button CreateButton(string tooltipText, ButtonGlyphKind kind, EventHandler handler)
        {
            var button = new GlyphButton();
            ButtonGlyphs.Apply(button, kind, tooltipText, toolTip);
            button.Click += handler;
            return button;
        }

        private void SetButtonGlyph(Button button, ButtonGlyphKind kind, string tooltipText)
        {
            ButtonGlyphs.Apply(button, kind, tooltipText, toolTip);
        }

        private void ShowCopyButtonFeedback()
        {
            copyFeedbackTimer.Stop();
            SetButtonGlyph(copyButton, ButtonGlyphKind.Check, "Copied");
            copyFeedbackTimer.Start();
        }

        private void ResetCopyButtonFeedback()
        {
            SetButtonGlyph(copyButton, ButtonGlyphKind.Copy, "Copy selected");
        }

        private void CaptureClipboardSafely()
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    var record = ClipboardReader.TryRead(store.ImagesDirectory);
                    if (record == null)
                    {
                        return;
                    }

                    store.Upsert(record);
                    RefreshHistoryList(record.Id);
                    RefreshCompactPopup(record.Id);
                    UpdateStatus("Captured " + record.Kind.ToLowerInvariant() + ". " + store.Count + " item(s) saved locally.");
                    return;
                }
                catch (ExternalException)
                {
                    Thread.Sleep(60);
                }
                catch (ThreadStateException)
                {
                    UpdateStatus("Clipboard is not available on this thread.");
                    return;
                }
                catch (Exception ex)
                {
                    UpdateStatus("Clipboard capture skipped: " + ex.Message);
                    return;
                }
            }

            UpdateStatus("Clipboard is busy. Capture skipped.");
        }

        private void RefreshHistoryList(string selectedId)
        {
            string previousId = selectedId;
            if (previousId == null)
            {
                previousId = GetSelectedRecordId();
            }

            ConfigureHistoryColumnsForFilter();
            historyList.SuspendLayout();
            historyList.Rows.Clear();
            bool restoredSelection = false;

            foreach (ClipboardRecord record in GetMainListRecords())
            {
                int rowIndex = historyList.Rows.Add(record.Name ?? string.Empty, record.Kind, record.Preview);
                DataGridViewRow row = historyList.Rows[rowIndex];
                row.Tag = record.Id;
                row.Height = CalculateHistoryRowHeight(record.Preview);

                if (record.Id == previousId)
                {
                    row.Selected = true;
                    historyList.CurrentCell = row.Cells[0];
                    restoredSelection = true;
                }
            }

            historyList.ResumeLayout();

            if (restoredSelection)
            {
                EnsureSelectedRowVisible();
            }
            else
            {
                historyList.ClearSelection();
                historyList.CurrentCell = null;
                ClearPreview();
            }

            UpdateButtonState();
            AdjustHistoryListWidth();
        }

        private IEnumerable<ClipboardRecord> GetMainListRecords()
        {
            return store.Records
                .Where(ShouldShowInMainList)
                .OrderByDescending(record => record.IsStarred)
                .ThenByDescending(record => record.IsStarred ? record.StarredAt : DateTime.MinValue)
                .ThenByDescending(record => record.CapturedAt);
        }

        private int CalculateHistoryRowHeight(string description)
        {
            int descriptionWidth = 420;
            if (historyList.Columns.Count > DescriptionColumnIndex)
            {
                descriptionWidth = Math.Max(160, historyList.Columns[DescriptionColumnIndex].Width - 10);
            }

            string text = string.IsNullOrEmpty(description) ? " " : description;
            Size measured = TextRenderer.MeasureText(
                text,
                historyList.Font,
                new Size(descriptionWidth, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

            int lineHeight = TextRenderer.MeasureText("Ag", historyList.Font).Height;
            int maxHeight = lineHeight * HistoryMaxDescriptionLines + 12;
            int desiredHeight = Math.Min(measured.Height + 12, maxHeight);
            return Math.Max(HistoryMinRowHeight, desiredHeight);
        }

        private void ResizeHistoryRows()
        {
            if (historyList.Rows.Count == 0 || historyList.IsDisposed)
            {
                return;
            }

            foreach (DataGridViewRow row in historyList.Rows)
            {
                ClipboardRecord record = GetRecordForGridRow(row);
                if (record != null)
                {
                    row.Height = CalculateHistoryRowHeight(record.Preview);
                }
            }
        }

        private void EnsureSelectedRowVisible()
        {
            DataGridViewRow row = GetFirstSelectedHistoryRow();
            if (row == null)
            {
                return;
            }

            try
            {
                historyList.FirstDisplayedScrollingRowIndex = row.Index;
            }
            catch
            {
            }
        }

        private void AdjustHistoryListWidth()
        {
            if (mainSplit == null || mainSplit.Width <= 0)
            {
                return;
            }

            int desiredListWidth = GetHistoryColumnsWidth() + SystemInformation.VerticalScrollBarWidth + 18;
            int minListWidth = IsImageFilterSelected() ? 500 : 610;
            int maxListWidth = Math.Max(minListWidth, mainSplit.Width - 320);
            desiredListWidth = Math.Min(Math.Max(minListWidth, desiredListWidth), maxListWidth);

            if (desiredListWidth > mainSplit.Panel1MinSize && desiredListWidth < mainSplit.Width - mainSplit.Panel2MinSize)
            {
                mainSplit.SplitterDistance = desiredListWidth;
            }

            AdjustDescriptionColumnForAvailableWidth();
        }

        private bool ShouldShowInMainList(ClipboardRecord record)
        {
            if (record == null || typeFilterComboBox == null || typeFilterComboBox.SelectedItem == null)
            {
                return true;
            }

            string filter = Convert.ToString(typeFilterComboBox.SelectedItem);
            if (string.Equals(filter, "Text", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(record.Kind, ClipboardRecord.TextKind, StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(filter, "Image", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(record.Kind, ClipboardRecord.ImageKind, StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(filter, "Star", StringComparison.OrdinalIgnoreCase))
            {
                return record.IsStarred;
            }

            return true;
        }

        private void ApplyTypeFilter()
        {
            RefreshHistoryList(null);
        }

        private void OnTypeFilterComboBoxDrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index >= 0)
            {
                string text = Convert.ToString(typeFilterComboBox.Items[e.Index]);
                Rectangle textBounds = Rectangle.Inflate(e.Bounds, -4, 0);
                TextRenderer.DrawText(
                    e.Graphics,
                    text,
                    e.Font,
                    textBounds,
                    e.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            e.DrawFocusRectangle();
        }

        private void ConfigureHistoryColumnsForFilter()
        {
            if (historyList == null || historyList.Columns.Count <= DescriptionColumnIndex)
            {
                return;
            }

            DataGridViewColumn descriptionColumn = historyList.Columns[DescriptionColumnIndex];
            if (IsImageFilterSelected())
            {
                descriptionColumn.MinimumWidth = HistoryImageDescriptionMinColumnWidth;
                descriptionColumn.Width = CalculateImageDescriptionColumnWidth();
            }
            else
            {
                descriptionColumn.MinimumWidth = 360;
                descriptionColumn.Width = Math.Max(HistoryDefaultDescriptionColumnWidth, CalculateAvailableDescriptionWidth());
            }
        }

        private void AdjustDescriptionColumnForAvailableWidth()
        {
            if (historyList == null || historyList.Columns.Count <= DescriptionColumnIndex || IsImageFilterSelected())
            {
                return;
            }

            DataGridViewColumn descriptionColumn = historyList.Columns[DescriptionColumnIndex];
            int desiredWidth = Math.Max(HistoryDefaultDescriptionColumnWidth, CalculateAvailableDescriptionWidth());
            if (descriptionColumn.Width != desiredWidth)
            {
                descriptionColumn.Width = desiredWidth;
            }
        }

        private int CalculateAvailableDescriptionWidth()
        {
            if (historyList == null || historyList.ClientSize.Width <= 0)
            {
                return HistoryDefaultDescriptionColumnWidth;
            }

            int fixedWidth = 0;
            for (int index = 0; index < historyList.Columns.Count; index++)
            {
                if (index != DescriptionColumnIndex && historyList.Columns[index].Visible)
                {
                    fixedWidth += historyList.Columns[index].Width;
                }
            }

            int chromeWidth = SystemInformation.VerticalScrollBarWidth + 8;
            return Math.Max(HistoryDefaultDescriptionColumnWidth, historyList.ClientSize.Width - fixedWidth - chromeWidth);
        }

        private int CalculateImageDescriptionColumnWidth()
        {
            int desiredWidth = HistoryImageDescriptionMinColumnWidth;
            foreach (ClipboardRecord record in store.Records.Where(ShouldShowInMainList))
            {
                string text = string.IsNullOrEmpty(record.Preview) ? " " : record.Preview;
                Size measured = TextRenderer.MeasureText(
                    text,
                    historyList.Font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                desiredWidth = Math.Max(desiredWidth, measured.Width + 22);
            }

            return Math.Min(desiredWidth, HistoryImageDescriptionMaxColumnWidth);
        }

        private int GetHistoryColumnsWidth()
        {
            int width = 0;
            foreach (DataGridViewColumn column in historyList.Columns)
            {
                if (column.Visible)
                {
                    width += column.Width;
                }
            }

            return width;
        }

        private bool IsImageFilterSelected()
        {
            return typeFilterComboBox != null
                && string.Equals(Convert.ToString(typeFilterComboBox.SelectedItem), "Image", StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshCompactPopup(string selectedId)
        {
            if (compactPopup != null && !compactPopup.IsDisposed)
            {
                compactPopup.RefreshItems(selectedId);
            }
        }

        private void OnHistoryListKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                EditSelectedName();
                e.Handled = true;
            }
        }

        private void EditSelectedName()
        {
            DataGridViewRow row = GetFirstSelectedHistoryRow();
            if (row == null)
            {
                return;
            }

            historyList.CurrentCell = row.Cells[0];
            historyList.BeginEdit(true);
        }

        private void ToggleSelectedStar()
        {
            ToggleRecordStar(GetSelectedRecord());
        }

        private void ToggleRecordStar(ClipboardRecord record)
        {
            if (record == null)
            {
                return;
            }

            bool isStarred = store.ToggleStar(record.Id);
            RefreshHistoryList(record.Id);
            RefreshCompactPopup(record.Id);
            UpdateStatus(isStarred ? "Item starred and moved to top." : "Item unstarred.");
        }

        private void OnHistoryNameEdited(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 0 || e.RowIndex >= historyList.Rows.Count)
            {
                return;
            }

            DataGridViewRow row = historyList.Rows[e.RowIndex];
            string name = NormalizeRecordName(Convert.ToString(row.Cells[0].Value));
            string id = Convert.ToString(row.Tag);

            row.Cells[0].Value = name;
            store.UpdateName(id, name);
            UpdateStatus(string.IsNullOrEmpty(name) ? "Name cleared." : "Name saved.");
        }

        private static string NormalizeRecordName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            string normalized = name.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
            if (normalized.Length > 120)
            {
                normalized = normalized.Substring(0, 120);
            }

            return normalized;
        }

        private void OnHistoryCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            DataGridViewRow row = historyList.Rows[e.RowIndex];
            ClipboardRecord record = GetRecordForGridRow(row);
            bool selected = row.Selected;
            Color backColor = selected ? e.CellStyle.SelectionBackColor : e.CellStyle.BackColor;
            Color foreColor = selected ? e.CellStyle.SelectionForeColor : e.CellStyle.ForeColor;
            if (backColor.IsEmpty)
            {
                backColor = row.Index % 2 == 0 ? HistoryRowBackColor : HistoryAlternateRowBackColor;
            }

            if (foreColor.IsEmpty)
            {
                foreColor = historyList.ForeColor;
            }

            using (var background = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(background, e.CellBounds);
            }

            if (e.ColumnIndex == TypeColumnIndex)
            {
                DrawTypeIcon(e.Graphics, e.CellBounds, record);
                e.Handled = true;
                return;
            }

            Rectangle textBounds = Rectangle.Inflate(e.CellBounds, -5, -4);
            if (e.ColumnIndex == 0 && record != null && record.IsStarred)
            {
                Rectangle starBounds = new Rectangle(e.CellBounds.Left + 5, e.CellBounds.Top + (e.CellBounds.Height - 17) / 2, 17, 17);
                DrawStarIcon(e.Graphics, starBounds);
                textBounds.X += 22;
                textBounds.Width = Math.Max(0, textBounds.Width - 22);
            }

            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPrefix;
            if (e.ColumnIndex == DescriptionColumnIndex)
            {
                flags |= TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis;
            }
            else
            {
                flags |= TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            }

            string text = Convert.ToString(e.Value) ?? string.Empty;
            TextRenderer.DrawText(e.Graphics, text, e.CellStyle.Font, textBounds, foreColor, flags);
            e.Handled = true;
        }

        private ClipboardRecord GetRecordForGridRow(DataGridViewRow row)
        {
            if (row == null)
            {
                return null;
            }

            return store.Find(Convert.ToString(row.Tag));
        }

        internal static void DrawTypeIcon(Graphics graphics, Rectangle bounds, ClipboardRecord record)
        {
            if (record == null)
            {
                return;
            }

            const int typeIconSize = 18;
            Rectangle iconBounds = new Rectangle(
                bounds.Left + (bounds.Width - typeIconSize) / 2,
                bounds.Top + (bounds.Height - typeIconSize) / 2,
                typeIconSize,
                typeIconSize);

            string iconName = record.IsImage ? "type-image" : "type-text";
            if (ProfessionalIcons.Draw(graphics, iconName, iconBounds, true))
            {
                return;
            }

            SmoothingMode oldSmoothing = graphics.SmoothingMode;
            PixelOffsetMode oldPixelOffset = graphics.PixelOffsetMode;
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.PixelOffsetMode = PixelOffsetMode.Default;

            if (record.IsImage)
            {
                DrawImageTypeIcon(graphics, iconBounds);
            }
            else
            {
                DrawTextTypeIcon(graphics, iconBounds);
            }

            graphics.SmoothingMode = oldSmoothing;
            graphics.PixelOffsetMode = oldPixelOffset;
        }

        private static void DrawTextTypeIcon(Graphics graphics, Rectangle bounds)
        {
            using (var pageBrush = new SolidBrush(Color.FromArgb(59, 130, 246)))
            using (var innerBrush = new SolidBrush(Color.FromArgb(239, 246, 255)))
            using (var lineBrush = new SolidBrush(Color.White))
            using (var borderPen = new Pen(Color.FromArgb(29, 78, 216), 1.0f))
            {
                Rectangle page = new Rectangle(bounds.Left + 1, bounds.Top + 1, bounds.Width - 2, bounds.Height - 2);
                graphics.FillRectangle(pageBrush, page);
                graphics.DrawRectangle(borderPen, page);
                graphics.FillRectangle(innerBrush, page.Left + 4, page.Top + 3, page.Width - 7, page.Height - 6);

                graphics.FillRectangle(lineBrush, page.Left + 6, page.Top + 6, page.Width - 11, 2);
                graphics.FillRectangle(lineBrush, page.Left + 6, page.Top + 10, page.Width - 10, 2);
            }
        }

        private static void DrawImageTypeIcon(Graphics graphics, Rectangle bounds)
        {
            using (var frameBrush = new SolidBrush(Color.FromArgb(6, 182, 212)))
            using (var skyBrush = new SolidBrush(Color.FromArgb(207, 250, 254)))
            using (var hillBrush = new SolidBrush(Color.FromArgb(34, 197, 94)))
            using (var sunBrush = new SolidBrush(Color.FromArgb(250, 204, 21)))
            using (var borderPen = new Pen(Color.FromArgb(8, 145, 178), 1.0f))
            {
                Rectangle frame = new Rectangle(bounds.Left + 1, bounds.Top + 1, bounds.Width - 2, bounds.Height - 2);
                graphics.FillRectangle(frameBrush, frame);
                graphics.DrawRectangle(borderPen, frame);
                Rectangle inside = new Rectangle(frame.Left + 3, frame.Top + 3, frame.Width - 6, frame.Height - 6);
                graphics.FillRectangle(skyBrush, inside);
                graphics.FillEllipse(sunBrush, inside.Right - 5, inside.Top + 2, 3, 3);

                Point[] hill =
                {
                    new Point(inside.Left, inside.Bottom - 1),
                    new Point(inside.Left + 4, inside.Top + 7),
                    new Point(inside.Left + 7, inside.Bottom - 2),
                    new Point(inside.Left + 10, inside.Top + 8),
                    new Point(inside.Right, inside.Bottom - 1)
                };
                graphics.FillPolygon(hillBrush, hill);
            }
        }

        internal static void DrawStarIcon(Graphics graphics, Rectangle bounds)
        {
            PointF center = new PointF(bounds.Left + bounds.Width / 2.0f, bounds.Top + bounds.Height / 2.0f);
            float outer = bounds.Width / 2.0f;
            float inner = outer * 0.48f;
            PointF[] points = new PointF[10];

            for (int i = 0; i < points.Length; i++)
            {
                double angle = -Math.PI / 2.0 + i * Math.PI / 5.0;
                float radius = i % 2 == 0 ? outer : inner;
                points[i] = new PointF(
                    center.X + (float)Math.Cos(angle) * radius,
                    center.Y + (float)Math.Sin(angle) * radius);
            }

            using (var fill = new SolidBrush(Color.FromArgb(250, 204, 21)))
            using (var outline = new Pen(Color.FromArgb(217, 119, 6), 1.2f))
            {
                graphics.FillPolygon(fill, points);
                graphics.DrawPolygon(outline, points);
            }
        }

        private void PreviewSelected()
        {
            ClipboardRecord record = GetSelectedRecord();
            if (record == null)
            {
                ClearPreview();
                return;
            }

            previewHeader.Text = record.Kind + " | " + record.CapturedAt.ToString("yyyy-MM-dd HH:mm:ss") + " | " + record.SizeText;
            DisposePreviewImage();

            if (record.IsImage)
            {
                string imagePath = store.GetImagePath(record);
                textPreview.Visible = false;
                imagePreview.Visible = true;
                SetImagePathBar(imagePath);
                imagePreview.Image = LoadImageCopy(imagePath);
            }
            else
            {
                SetImagePathBar(null);
                imagePreview.Visible = false;
                textPreview.Visible = true;
                textPreview.Text = record.Text ?? string.Empty;
            }

            UpdateButtonState();
        }

        private void ClearPreview()
        {
            previewHeader.Text = "No item selected";
            textPreview.Text = string.Empty;
            textPreview.Visible = true;
            imagePreview.Visible = false;
            SetImagePathBar(null);
            DisposePreviewImage();
            UpdateButtonState();
        }

        private void SetImagePathBar(string imagePath)
        {
            bool hasPath = !string.IsNullOrEmpty(imagePath);
            imagePathTextBox.Text = hasPath ? imagePath : string.Empty;
            imagePathPanel.Visible = hasPath;
            openImagePathButton.Enabled = hasPath;
            imagePathRowStyle.Height = hasPath ? 42 : 0;
        }

        private void OpenSelectedImagePath()
        {
            ClipboardRecord record = GetSelectedRecord();
            if (record == null || !record.IsImage)
            {
                return;
            }

            string imagePath = store.GetImagePath(record);
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                UpdateStatus("Image file is missing.");
                return;
            }

            try
            {
                System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + imagePath + "\"");
            }
            catch (Exception ex)
            {
                UpdateStatus("Could not open image path: " + ex.Message);
            }
        }

        private void OpenSelectedImageLarge()
        {
            ClipboardRecord record = GetSelectedRecord();
            if (record == null || !record.IsImage)
            {
                return;
            }

            string imagePath = store.GetImagePath(record);
            Image image = LoadImageCopy(imagePath);
            if (image == null)
            {
                UpdateStatus("Image file is missing.");
                return;
            }

            var viewer = new ImageViewerForm(image, imagePath, Icon);
            viewer.Show(this);
        }

        private void CopySelected()
        {
            CopyRecord(GetSelectedRecord());
        }

        private void CopyRecord(ClipboardRecord record)
        {
            if (record == null)
            {
                return;
            }

            try
            {
                if (record.IsImage)
                {
                    string imagePath = store.GetImagePath(record);
                    Image image = LoadImageCopy(imagePath);
                    if (image == null)
                    {
                        UpdateStatus("Image file is missing.");
                        return;
                    }

                    Clipboard.SetImage(image);
                }
                else
                {
                    Clipboard.SetText(record.Text ?? string.Empty, TextDataFormat.UnicodeText);
                }

                UpdateStatus("Copied selected " + record.Kind.ToLowerInvariant() + " back to clipboard.");
                ShowCopyButtonFeedback();
            }
            catch (Exception ex)
            {
                UpdateStatus("Copy failed: " + ex.Message);
            }
        }

        private void DeleteSelected()
        {
            List<string> selectedIds = GetSelectedRecordIds();
            if (selectedIds.Count == 0)
            {
                return;
            }

            int deletedCount = store.DeleteMany(selectedIds);
            RefreshHistoryList(null);
            RefreshCompactPopup(null);
            UpdateStatus("Deleted " + deletedCount + " selected item(s).");
        }

        private void DeleteRecord(ClipboardRecord record)
        {
            if (record == null)
            {
                return;
            }

            store.Delete(record.Id);
            RefreshHistoryList(null);
            RefreshCompactPopup(null);
            UpdateStatus("Deleted selected item.");
        }

        private void ClearAll()
        {
            if (store.Count == 0)
            {
                return;
            }

            DialogResult result = MessageBox.Show(
                this,
                "Delete all saved clipboard items?",
                "Clipboard Keeper",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            store.Clear();
            RefreshHistoryList(null);
            RefreshCompactPopup(null);
            UpdateStatus("All saved items were deleted.");
        }

        private void TogglePause()
        {
            isPaused = !isPaused;
            SetButtonGlyph(pauseButton, isPaused ? ButtonGlyphKind.Resume : ButtonGlyphKind.Pause, isPaused ? "Resume" : "Pause");
            pauseTrayMenuItem.Text = isPaused ? "Resume" : "Pause";
            UpdateStatus(isPaused
                ? "Paused. Clipboard changes are not recorded."
                : "Monitoring. Local-only. Up to 100 text and image items.");
        }

        private void OpenStorageFolder()
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", store.RootDirectory);
            }
            catch (Exception ex)
            {
                UpdateStatus("Could not open storage folder: " + ex.Message);
            }
        }

        private void HideToTray()
        {
            StopHideToTrayFade();
            Opacity = 1.0;
            ShowInTaskbar = false;
            Hide();
            UpdateStatus("Minimized to the system tray. Click the tray icon for the quick panel.");
        }

        private void BeginHideToTrayFade()
        {
            if (isFadingToTray || isExiting || !Visible)
            {
                return;
            }

            isFadingToTray = true;
            hideToTrayFadeStartedAt = DateTime.UtcNow;
            Opacity = 1.0;
            hideToTrayFadeTimer.Stop();
            hideToTrayFadeTimer.Start();
        }

        private void UpdateHideToTrayFade()
        {
            if (!isFadingToTray)
            {
                hideToTrayFadeTimer.Stop();
                return;
            }

            double elapsed = (DateTime.UtcNow - hideToTrayFadeStartedAt).TotalMilliseconds;
            double progress = Math.Max(0.0, Math.Min(1.0, elapsed / HideToTrayFadeDurationMs));
            double easedProgress = 1.0 - Math.Pow(1.0 - progress, 3.0);
            Opacity = Math.Max(0.02, 1.0 - easedProgress);

            if (progress >= 1.0)
            {
                CompleteHideToTrayFade();
            }
        }

        private void CompleteHideToTrayFade()
        {
            hideToTrayFadeTimer.Stop();
            isFadingToTray = false;
            ShowInTaskbar = false;
            Hide();
            Opacity = 1.0;
            UpdateStatus("Minimized to the system tray. Click the tray icon for the quick panel.");
        }

        private void StopHideToTrayFade()
        {
            if (hideToTrayFadeTimer != null)
            {
                hideToTrayFadeTimer.Stop();
            }

            isFadingToTray = false;
        }

        private void QueueHideToTrayAfterMinimize()
        {
            if (hideToTrayPending)
            {
                return;
            }

            hideToTrayPending = true;
            BeginInvoke(new MethodInvoker(delegate
            {
                hideToTrayPending = false;
                if (!isExiting && WindowState == FormWindowState.Minimized)
                {
                    HideToTray();
                }
            }));
        }

        private void ShowFullWindow()
        {
            StopHideToTrayFade();
            Opacity = 1.0;
            if (compactPopup != null && !compactPopup.IsDisposed)
            {
                compactPopup.Hide();
            }

            ShowInTaskbar = true;
            Show();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            Activate();
        }

        private void ShowCompactPopupNearCursor()
        {
            ShowCompactPopupNearCursor(false);
        }

        private void ShowCompactPopupNearCursor(bool forceShow)
        {
            CompactClipboardPopup popup = EnsureCompactPopup();
            if (popup == null)
            {
                return;
            }

            popup.RefreshItems(GetSelectedRecordId());
            popup.ShowNear(Cursor.Position, forceShow);
        }

        private void ExitApplication()
        {
            isExiting = true;
            StopHideToTrayFade();
            Opacity = 1.0;

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
            }

            Close();
        }

        private ClipboardRecord GetSelectedRecord()
        {
            string id = GetSelectedRecordId();
            if (id == null)
            {
                return null;
            }

            return store.Find(id);
        }

        private string GetSelectedRecordId()
        {
            DataGridViewRow row = GetFirstSelectedHistoryRow();
            if (row == null)
            {
                return null;
            }

            return Convert.ToString(row.Tag);
        }

        private List<string> GetSelectedRecordIds()
        {
            var ids = new List<string>();
            foreach (DataGridViewRow row in GetSelectedHistoryRows())
            {
                string id = Convert.ToString(row.Tag);
                if (!string.IsNullOrEmpty(id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private DataGridViewRow GetFirstSelectedHistoryRow()
        {
            return GetSelectedHistoryRows().FirstOrDefault();
        }

        private IEnumerable<DataGridViewRow> GetSelectedHistoryRows()
        {
            return historyList.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(row => row != null && !row.IsNewRow)
                .OrderBy(row => row.Index);
        }

        private void UpdateButtonState()
        {
            bool hasSelection = historyList.SelectedRows.Count > 0;
            copyButton.Enabled = hasSelection;
            editNameButton.Enabled = hasSelection;
            starButton.Enabled = hasSelection;
            ClipboardRecord record = GetSelectedRecord();
            SetButtonGlyph(starButton, record != null && record.IsStarred ? ButtonGlyphKind.Unstar : ButtonGlyphKind.Star, record != null && record.IsStarred ? "Unstar" : "Star");
            deleteButton.Enabled = hasSelection;
            clearButton.Enabled = store.Count > 0;
        }

        private void UpdateStatus(string message)
        {
            statusLabel.Text = message + " Storage: " + store.RootDirectory;
        }

        private void DisposePreviewImage()
        {
            Image old = imagePreview.Image;
            imagePreview.Image = null;
            if (old != null)
            {
                old.Dispose();
            }
        }

        private static Image LoadImageCopy(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            return ProtectedImageLoader.LoadImageCopy(path);
        }
    }

    internal sealed class ImageViewerForm : Form
    {
        private readonly Image image;
        private readonly string imagePath;
        private readonly PictureBox pictureBox;
        private readonly ToolTip toolTip;

        public ImageViewerForm(Image image, string imagePath, Icon icon)
        {
            this.image = image;
            this.imagePath = imagePath;
            toolTip = new ToolTip();

            Text = "Image Preview";
            Icon = icon;
            Font = UiFonts.Create(9.0f, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterParent;
            Width = 980;
            Height = 720;
            MinimumSize = new Size(520, 360);

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 2;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var pathPanel = new Panel();
            pathPanel.Dock = DockStyle.Fill;
            pathPanel.Padding = new Padding(8, 5, 8, 5);

            var openPathButton = new GlyphButton();
            ButtonGlyphs.Apply(openPathButton, ButtonGlyphKind.Folder, "Open image path", toolTip);
            openPathButton.Dock = DockStyle.Right;
            openPathButton.Width = ButtonGlyphs.ButtonWidth;
            openPathButton.Click += delegate { OpenPath(); };

            var pathTextBox = new TextBox();
            pathTextBox.Dock = DockStyle.Fill;
            pathTextBox.ReadOnly = true;
            pathTextBox.Text = imagePath ?? string.Empty;
            pathTextBox.BorderStyle = BorderStyle.FixedSingle;

            pathPanel.Controls.Add(openPathButton);
            pathPanel.Controls.Add(pathTextBox);

            pictureBox = new PictureBox();
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.BackColor = Color.White;
            pictureBox.BorderStyle = BorderStyle.FixedSingle;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.Image = image;

            root.Controls.Add(pathPanel, 0, 0);
            root.Controls.Add(pictureBox, 0, 1);
            Controls.Add(root);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pictureBox.Image = null;
                if (image != null)
                {
                    image.Dispose();
                }

                if (toolTip != null)
                {
                    toolTip.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void OpenPath()
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return;
            }

            System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + imagePath + "\"");
        }
    }

    internal sealed class CompactClipboardPopup : Form
    {
        private const int QuickTypeColumnIndex = 1;
        private const int QuickDescriptionColumnIndex = 2;
        private const int QuickRowHeight = 58;
        private const int QuickNameColumnWidth = 230;
        private const int QuickTypeColumnWidth = 64;
        private const int QuickMinimumDescriptionColumnWidth = 260;
        private static readonly Color QuickRowBackColor = Color.White;
        private static readonly Color QuickAlternateRowBackColor = Color.FromArgb(225, 239, 255);

        private readonly HistoryStore store;
        private readonly Action<ClipboardRecord> copyAction;
        private readonly Action<ClipboardRecord> starAction;
        private readonly Action<ClipboardRecord> deleteAction;
        private readonly Action openFullAction;
        private readonly Action exitAction;
        private readonly ListView quickList;
        private readonly ImageList rowImages;
        private readonly ComboBox typeFilterComboBox;
        private readonly ToolTip toolTip;
        private readonly Button copyButton;
        private readonly Button starButton;
        private readonly Button deleteButton;

        public CompactClipboardPopup(
            HistoryStore store,
            Icon icon,
            Action<ClipboardRecord> copyAction,
            Action<ClipboardRecord> starAction,
            Action<ClipboardRecord> deleteAction,
            Action openFullAction,
            Action exitAction)
        {
            this.store = store;
            this.copyAction = copyAction;
            this.starAction = starAction;
            this.deleteAction = deleteAction;
            this.openFullAction = openFullAction;
            this.exitAction = exitAction;
            toolTip = new ToolTip();

            Text = "Clipboard Keeper";
            Icon = icon;
            Font = UiFonts.Create(9.0f, FontStyle.Regular);
            Width = 760;
            Height = 500;
            MinimumSize = new Size(640, 360);
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            ShowInTaskbar = false;
            TopMost = true;

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 3;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            var header = new FlowLayoutPanel();
            header.Dock = DockStyle.Fill;
            header.FlowDirection = FlowDirection.LeftToRight;
            header.Padding = new Padding(10, 10, 8, 8);
            header.WrapContents = false;

            var title = new Label();
            title.Text = "Recent Clipboard";
            title.AutoSize = false;
            title.Width = 140;
            title.Height = ButtonGlyphs.ButtonHeight;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Font = UiFonts.Create(9.0f, FontStyle.Bold);
            title.Margin = new Padding(0, 0, 14, 0);

            var filterLabel = new Label();
            filterLabel.Text = "Filter";
            filterLabel.AutoSize = false;
            filterLabel.Width = 42;
            filterLabel.Height = ButtonGlyphs.ButtonHeight;
            filterLabel.TextAlign = ContentAlignment.MiddleLeft;
            filterLabel.Margin = new Padding(0, 0, 6, 0);

            typeFilterComboBox = new ComboBox();
            typeFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            typeFilterComboBox.DrawMode = DrawMode.OwnerDrawFixed;
            typeFilterComboBox.ItemHeight = ButtonGlyphs.ButtonHeight - 6;
            typeFilterComboBox.Width = 86;
            typeFilterComboBox.Height = ButtonGlyphs.ButtonHeight;
            typeFilterComboBox.Margin = new Padding(0);
            typeFilterComboBox.Items.Add("All");
            typeFilterComboBox.Items.Add("Text");
            typeFilterComboBox.Items.Add("Image");
            typeFilterComboBox.Items.Add("Star");
            typeFilterComboBox.SelectedIndex = 0;
            typeFilterComboBox.SelectedIndexChanged += delegate { RefreshItems(null); };
            typeFilterComboBox.DrawItem += OnQuickFilterComboBoxDrawItem;

            header.Controls.Add(title);
            header.Controls.Add(filterLabel);
            header.Controls.Add(typeFilterComboBox);

            rowImages = new ImageList();
            rowImages.ImageSize = new Size(1, QuickRowHeight);
            rowImages.ColorDepth = ColorDepth.Depth32Bit;
            rowImages.Images.Add(new Bitmap(1, QuickRowHeight));

            quickList = new ListView();
            quickList.Dock = DockStyle.Fill;
            quickList.Font = UiFonts.Create(10.0f, FontStyle.Regular);
            quickList.View = View.Details;
            quickList.FullRowSelect = true;
            quickList.HideSelection = false;
            quickList.MultiSelect = false;
            quickList.SmallImageList = rowImages;
            quickList.OwnerDraw = true;
            quickList.Columns.Add("Name", QuickNameColumnWidth);
            quickList.Columns.Add("Type", QuickTypeColumnWidth);
            quickList.Columns.Add("Description", QuickMinimumDescriptionColumnWidth);
            quickList.SelectedIndexChanged += delegate { UpdateButtonState(); };
            quickList.DoubleClick += delegate { CopySelected(); };
            quickList.DrawColumnHeader += OnQuickDrawColumnHeader;
            quickList.DrawItem += OnQuickDrawItem;
            quickList.DrawSubItem += OnQuickDrawSubItem;
            quickList.SizeChanged += delegate { AdjustQuickDescriptionColumnWidth(); };

            var buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(8, 8, 8, 6);
            buttons.WrapContents = false;

            copyButton = CreateButton("Copy", ButtonGlyphKind.Copy, delegate { CopySelected(); });
            starButton = CreateButton("Star", ButtonGlyphKind.Star, delegate { StarSelected(); });
            deleteButton = CreateButton("Delete", ButtonGlyphKind.Delete, delegate { DeleteSelected(); });
            var openButton = CreateButton("Main window", ButtonGlyphKind.MainWindow, delegate { OpenFullWindow(); });
            var exitButton = CreateButton("Exit", ButtonGlyphKind.Exit, delegate { exitAction(); });

            buttons.Controls.Add(exitButton);
            buttons.Controls.Add(openButton);
            buttons.Controls.Add(deleteButton);
            buttons.Controls.Add(starButton);
            buttons.Controls.Add(copyButton);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(quickList, 0, 1);
            root.Controls.Add(buttons, 0, 2);
            Controls.Add(root);

            AdjustQuickDescriptionColumnWidth();
            RefreshItems(null);
        }

        public void ShowNear(Point anchor, bool forceShow)
        {
            if (Visible && !forceShow)
            {
                Hide();
                return;
            }

            PositionNear(anchor);
            Show();
            Activate();
        }

        public void RefreshItems(string selectedId)
        {
            string previousId = selectedId;
            if (previousId == null && quickList.SelectedItems.Count > 0)
            {
                previousId = Convert.ToString(quickList.SelectedItems[0].Tag);
            }

            quickList.BeginUpdate();
            quickList.Items.Clear();

            foreach (ClipboardRecord record in GetQuickListRecords())
            {
                var item = new ListViewItem(record.Name ?? string.Empty);
                item.SubItems.Add(record.Kind);
                item.SubItems.Add(record.Preview);
                item.Tag = record.Id;

                quickList.Items.Add(item);
                if (record.Id == previousId)
                {
                    item.Selected = true;
                    item.Focused = true;
                }
            }

            quickList.EndUpdate();

            if (quickList.SelectedItems.Count > 0)
            {
                quickList.SelectedItems[0].EnsureVisible();
            }

            UpdateButtonState();
            AdjustQuickDescriptionColumnWidth();
        }

        private IEnumerable<ClipboardRecord> GetQuickListRecords()
        {
            return store.Records
                .Where(ShouldShowInQuickList)
                .OrderByDescending(record => record.IsStarred)
                .ThenByDescending(record => record.IsStarred ? record.StarredAt : DateTime.MinValue)
                .ThenByDescending(record => record.CapturedAt);
        }

        private bool ShouldShowInQuickList(ClipboardRecord record)
        {
            if (record == null || typeFilterComboBox == null || typeFilterComboBox.SelectedItem == null)
            {
                return true;
            }

            string filter = Convert.ToString(typeFilterComboBox.SelectedItem);
            if (string.Equals(filter, "Text", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(record.Kind, ClipboardRecord.TextKind, StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(filter, "Image", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(record.Kind, ClipboardRecord.ImageKind, StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(filter, "Star", StringComparison.OrdinalIgnoreCase))
            {
                return record.IsStarred;
            }

            return true;
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);

            if (!ContainsFocus)
            {
                Hide();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }

        private void OnQuickDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void OnQuickFilterComboBoxDrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index >= 0)
            {
                string text = Convert.ToString(typeFilterComboBox.Items[e.Index]);
                Rectangle textBounds = Rectangle.Inflate(e.Bounds, -4, 0);
                TextRenderer.DrawText(
                    e.Graphics,
                    text,
                    e.Font,
                    textBounds,
                    e.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            e.DrawFocusRectangle();
        }

        private void OnQuickDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (quickList.View != View.Details)
            {
                e.DrawDefault = true;
            }
        }

        private void OnQuickDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            bool selected = e.Item.Selected;
            Color backColor = selected
                ? SystemColors.Highlight
                : (e.Item.Index % 2 == 0 ? QuickRowBackColor : QuickAlternateRowBackColor);
            Color foreColor = selected ? SystemColors.HighlightText : quickList.ForeColor;
            ClipboardRecord record = GetRecordForListItem(e.Item);

            using (var background = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(background, e.Bounds);
            }

            if (e.ColumnIndex == QuickTypeColumnIndex)
            {
                ClipboardManagerForm.DrawTypeIcon(e.Graphics, e.Bounds, record);
                return;
            }

            Rectangle textBounds = Rectangle.Inflate(e.Bounds, -5, -4);
            if (e.ColumnIndex == 0 && record != null && record.IsStarred)
            {
                Rectangle starBounds = new Rectangle(e.Bounds.Left + 5, e.Bounds.Top + (e.Bounds.Height - 17) / 2, 17, 17);
                ClipboardManagerForm.DrawStarIcon(e.Graphics, starBounds);
                textBounds.X += 22;
                textBounds.Width = Math.Max(0, textBounds.Width - 22);
            }

            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPrefix;
            if (e.ColumnIndex == QuickDescriptionColumnIndex)
            {
                flags |= TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis;
            }
            else
            {
                flags |= TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            }

            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.SubItem.Font, textBounds, foreColor, flags);
        }

        private void AdjustQuickDescriptionColumnWidth()
        {
            if (quickList == null || quickList.Columns.Count <= QuickDescriptionColumnIndex || quickList.ClientSize.Width <= 0)
            {
                return;
            }

            int chromeWidth = SystemInformation.VerticalScrollBarWidth + 8;
            int desiredWidth = quickList.ClientSize.Width - QuickNameColumnWidth - QuickTypeColumnWidth - chromeWidth;
            desiredWidth = Math.Max(QuickMinimumDescriptionColumnWidth, desiredWidth);

            if (quickList.Columns[QuickDescriptionColumnIndex].Width != desiredWidth)
            {
                quickList.Columns[QuickDescriptionColumnIndex].Width = desiredWidth;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeRowImages();
                rowImages.Dispose();
                if (toolTip != null)
                {
                    toolTip.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private ClipboardRecord GetRecordForListItem(ListViewItem item)
        {
            if (item == null)
            {
                return null;
            }

            return store.Find(Convert.ToString(item.Tag));
        }

        private Button CreateButton(string tooltipText, ButtonGlyphKind kind, EventHandler handler)
        {
            var button = new GlyphButton();
            ButtonGlyphs.Apply(button, kind, tooltipText, toolTip);
            button.Click += handler;
            return button;
        }

        private void SetButtonGlyph(Button button, ButtonGlyphKind kind, string tooltipText)
        {
            ButtonGlyphs.Apply(button, kind, tooltipText, toolTip);
        }

        private void PositionNear(Point anchor)
        {
            Rectangle area = Screen.FromPoint(anchor).WorkingArea;
            int margin = 8;
            int x = anchor.X - Width + 24;
            int y = anchor.Y - Height - 12;

            if (y < area.Top + margin)
            {
                y = anchor.Y + 12;
            }

            x = Math.Max(area.Left + margin, Math.Min(x, area.Right - Width - margin));
            y = Math.Max(area.Top + margin, Math.Min(y, area.Bottom - Height - margin));
            Location = new Point(x, y);
        }

        private void CopySelected()
        {
            ClipboardRecord record = GetSelectedRecord();
            if (record == null)
            {
                return;
            }

            copyAction(record);
            Hide();
        }

        private void DeleteSelected()
        {
            ClipboardRecord record = GetSelectedRecord();
            if (record == null)
            {
                return;
            }

            deleteAction(record);
            RefreshItems(null);
        }

        private void StarSelected()
        {
            ClipboardRecord record = GetSelectedRecord();
            if (record == null)
            {
                return;
            }

            starAction(record);
            RefreshItems(record.Id);
        }

        private void OpenFullWindow()
        {
            Hide();
            openFullAction();
        }

        private ClipboardRecord GetSelectedRecord()
        {
            if (quickList.SelectedItems.Count == 0)
            {
                return null;
            }

            string id = Convert.ToString(quickList.SelectedItems[0].Tag);
            return store.Find(id);
        }

        private void UpdateButtonState()
        {
            bool hasSelection = quickList.SelectedItems.Count > 0;
            copyButton.Enabled = hasSelection;
            starButton.Enabled = hasSelection;
            ClipboardRecord record = GetSelectedRecord();
            SetButtonGlyph(starButton, record != null && record.IsStarred ? ButtonGlyphKind.Unstar : ButtonGlyphKind.Star, record != null && record.IsStarred ? "Unstar" : "Star");
            deleteButton.Enabled = hasSelection;
        }

        private void DisposeRowImages()
        {
            foreach (Image image in rowImages.Images)
            {
                image.Dispose();
            }
        }
    }

    internal static class ClipboardReader
    {
        public static ClipboardRecord TryRead(string imagesDirectory)
        {
            Directory.CreateDirectory(imagesDirectory);

            if (Clipboard.ContainsImage())
            {
                using (Image image = Clipboard.GetImage())
                {
                    if (image == null)
                    {
                        return null;
                    }

                    using (var pngStream = new MemoryStream())
                    {
                        image.Save(pngStream, ImageFormat.Png);
                        byte[] bytes = pngStream.ToArray();
                        string id = "image-" + HashBytes(bytes);
                        string fileName = id + ".bin";
                        string path = Path.Combine(imagesDirectory, fileName);
                        File.WriteAllBytes(path, ProtectedDataHelper.ProtectBytes(bytes));

                        return new ClipboardRecord
                        {
                            Id = id,
                            Kind = ClipboardRecord.ImageKind,
                            CapturedAt = DateTime.Now,
                            ImageFile = fileName,
                            Width = image.Width,
                            Height = image.Height,
                            ByteCount = bytes.Length,
                            Preview = image.Width + " x " + image.Height + " image"
                        };
                    }
                }
            }

            if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                string text = Clipboard.GetText(TextDataFormat.UnicodeText);
                if (text == null || text.Length == 0)
                {
                    return null;
                }

                string id = "text-" + HashString(text);
                return new ClipboardRecord
                {
                    Id = id,
                    Kind = ClipboardRecord.TextKind,
                    CapturedAt = DateTime.Now,
                    Text = text,
                    Preview = BuildTextPreview(text),
                    CharacterCount = text.Length
                };
            }

            return null;
        }

        private static string BuildTextPreview(string text)
        {
            string preview = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
            if (preview.Length == 0)
            {
                preview = "(whitespace text)";
            }

            if (preview.Length > 120)
            {
                preview = preview.Substring(0, 117) + "...";
            }

            return preview;
        }

        private static string HashString(string value)
        {
            return HashBytes(Encoding.UTF8.GetBytes("text:" + value));
        }

        private static string HashBytes(byte[] bytes)
        {
            using (var sha = new SHA256Managed())
            {
                byte[] hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (byte item in hash)
                {
                    builder.Append(item.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }

    internal sealed class HistoryStore
    {
        private const int MaxItems = 100;
        private const string AppFolderName = "JiraAceClipboardManager";
        private const string HistoryFileName = "history.xml";
        private const string ImagesFolderName = "images";

        private readonly ClipboardHistory history;
        private readonly string historyPath;

        private HistoryStore(string rootDirectory, ClipboardHistory history)
        {
            RootDirectory = rootDirectory;
            ImagesDirectory = Path.Combine(rootDirectory, ImagesFolderName);
            historyPath = Path.Combine(rootDirectory, HistoryFileName);
            this.history = history ?? new ClipboardHistory();
            Directory.CreateDirectory(RootDirectory);
            Directory.CreateDirectory(ImagesDirectory);
        }

        public string RootDirectory { get; private set; }

        public string ImagesDirectory { get; private set; }

        public IList<ClipboardRecord> Records
        {
            get { return history.Records; }
        }

        public int Count
        {
            get { return history.Records.Count; }
        }

        public static HistoryStore LoadDefault()
        {
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppFolderName);

            string historyPath = Path.Combine(root, HistoryFileName);
            ClipboardHistory loaded = null;

            if (File.Exists(historyPath))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(ClipboardHistory));
                    using (var stream = File.OpenRead(historyPath))
                    {
                        loaded = serializer.Deserialize(stream) as ClipboardHistory;
                    }
                }
                catch
                {
                    string brokenPath = historyPath + ".broken-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    try
                    {
                        File.Move(historyPath, brokenPath);
                    }
                    catch
                    {
                    }
                }
            }

            return new HistoryStore(root, loaded ?? new ClipboardHistory());
        }

        public void Upsert(ClipboardRecord record)
        {
            ClipboardRecord existing = Find(record.Id);
            if (existing != null)
            {
                record.Name = existing.Name;
                record.StarredAt = existing.StarredAt;
                history.Records.Remove(existing);
            }

            history.Records.Insert(0, record);
            Trim();
            Save();
        }

        public ClipboardRecord Find(string id)
        {
            return history.Records.FirstOrDefault(item => item.Id == id);
        }

        public void UpdateName(string id, string name)
        {
            ClipboardRecord record = Find(id);
            if (record == null)
            {
                return;
            }

            record.Name = name ?? string.Empty;
            Save();
        }

        public bool ToggleStar(string id)
        {
            ClipboardRecord record = Find(id);
            if (record == null)
            {
                return false;
            }

            if (record.IsStarred)
            {
                record.StarredAt = DateTime.MinValue;
                Save();
                return false;
            }

            record.StarredAt = DateTime.Now;
            Save();
            return true;
        }

        public void Delete(string id)
        {
            ClipboardRecord record = Find(id);
            if (record == null)
            {
                return;
            }

            history.Records.Remove(record);
            DeleteImageFile(record);
            Save();
        }

        public int DeleteMany(IEnumerable<string> ids)
        {
            if (ids == null)
            {
                return 0;
            }

            var idSet = new HashSet<string>(ids.Where(id => !string.IsNullOrEmpty(id)));
            if (idSet.Count == 0)
            {
                return 0;
            }

            List<ClipboardRecord> recordsToDelete = history.Records
                .Where(record => idSet.Contains(record.Id))
                .ToList();

            foreach (ClipboardRecord record in recordsToDelete)
            {
                history.Records.Remove(record);
                DeleteImageFile(record);
            }

            if (recordsToDelete.Count > 0)
            {
                Save();
            }

            return recordsToDelete.Count;
        }

        public void Clear()
        {
            foreach (ClipboardRecord record in history.Records.ToList())
            {
                DeleteImageFile(record);
            }

            history.Records.Clear();
            Save();
        }

        public string GetImagePath(ClipboardRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.ImageFile))
            {
                return null;
            }

            return Path.Combine(ImagesDirectory, record.ImageFile);
        }

        private void Trim()
        {
            while (history.Records.Count > MaxItems)
            {
                ClipboardRecord last = history.Records[history.Records.Count - 1];
                history.Records.RemoveAt(history.Records.Count - 1);
                DeleteImageFile(last);
            }
        }

        private void Save()
        {
            Directory.CreateDirectory(RootDirectory);
            Directory.CreateDirectory(ImagesDirectory);

            string tempPath = historyPath + ".tmp";
            var serializer = new XmlSerializer(typeof(ClipboardHistory));
            using (var stream = File.Create(tempPath))
            {
                serializer.Serialize(stream, history);
            }

            if (File.Exists(historyPath))
            {
                File.Delete(historyPath);
            }

            File.Move(tempPath, historyPath);
        }

        private void DeleteImageFile(ClipboardRecord record)
        {
            if (record == null || !record.IsImage || string.IsNullOrEmpty(record.ImageFile))
            {
                return;
            }

            string path = GetImagePath(record);
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }

    internal static class ThumbnailLoader
    {
        public static Image Load(string path, Size size)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            using (Image original = ProtectedImageLoader.LoadImageCopy(path))
            {
                if (original == null)
                {
                    return null;
                }

                var thumb = new Bitmap(size.Width, size.Height);
                using (Graphics graphics = Graphics.FromImage(thumb))
                {
                    graphics.Clear(Color.White);
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    Rectangle target = FitRectangle(original.Size, size);
                    graphics.DrawImage(original, target);
                }

                return thumb;
            }
        }

        private static Rectangle FitRectangle(Size source, Size bounds)
        {
            double scale = Math.Min(
                (double)bounds.Width / Math.Max(1, source.Width),
                (double)bounds.Height / Math.Max(1, source.Height));

            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));
            int x = (bounds.Width - width) / 2;
            int y = (bounds.Height - height) / 2;
            return new Rectangle(x, y, width, height);
        }
    }

    internal static class ProtectedImageLoader
    {
        public static Image LoadImageCopy(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            byte[] protectedBytes = File.ReadAllBytes(path);
            byte[] imageBytes = ProtectedDataHelper.UnprotectBytes(protectedBytes);

            using (var stream = new MemoryStream(imageBytes))
            using (Image original = Image.FromStream(stream))
            {
                return new Bitmap(original);
            }
        }
    }

    internal static class ProtectedDataHelper
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ClipboardKeeper.v1");

        public static string ProtectString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            byte[] plain = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(ProtectBytes(plain));
        }

        public static string UnprotectString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            try
            {
                byte[] protectedBytes = Convert.FromBase64String(value);
                byte[] plain = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }
            catch
            {
                return value;
            }
        }

        public static byte[] ProtectBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return bytes;
            }

            return ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        }

        public static byte[] UnprotectBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return bytes;
            }

            try
            {
                return ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
                return bytes;
            }
        }
    }

    [Serializable]
    public sealed class ClipboardHistory
    {
        public ClipboardHistory()
        {
            Records = new List<ClipboardRecord>();
        }

        public List<ClipboardRecord> Records { get; set; }
    }

    [Serializable]
    public sealed class ClipboardRecord
    {
        public const string TextKind = "Text";
        public const string ImageKind = "Image";

        public string Id { get; set; }
        public string Kind { get; set; }
        public DateTime CapturedAt { get; set; }
        [XmlIgnore]
        public string Name { get; set; }
        [XmlIgnore]
        public string Text { get; set; }
        public string ImageFile { get; set; }
        public string Preview { get; set; }
        public int CharacterCount { get; set; }
        public int ByteCount { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime StarredAt { get; set; }

        public string ProtectedText
        {
            get { return ProtectedDataHelper.ProtectString(Text); }
            set { Text = ProtectedDataHelper.UnprotectString(value); }
        }

        public string ProtectedName
        {
            get { return ProtectedDataHelper.ProtectString(Name); }
            set { Name = ProtectedDataHelper.UnprotectString(value); }
        }

        [XmlIgnore]
        public bool IsStarred
        {
            get { return StarredAt > DateTime.MinValue; }
        }

        [XmlIgnore]
        public bool IsImage
        {
            get { return string.Equals(Kind, ImageKind, StringComparison.OrdinalIgnoreCase); }
        }

        [XmlIgnore]
        public string SizeText
        {
            get
            {
                if (IsImage)
                {
                    return Width + "x" + Height;
                }

                return CharacterCount + " chars";
            }
        }
    }
}
