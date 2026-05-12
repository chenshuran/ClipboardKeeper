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
}
