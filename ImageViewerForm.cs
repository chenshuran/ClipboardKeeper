// File: ImageViewerForm.cs
// Purpose: Shows a larger standalone preview window for saved clipboard images.

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
}
