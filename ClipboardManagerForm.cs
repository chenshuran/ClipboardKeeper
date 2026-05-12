// File: ClipboardManagerForm.cs
// Purpose: Implements the main Clipboard Keeper window, including capture flow, history list, preview pane, tray behavior, and user actions.

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
}
