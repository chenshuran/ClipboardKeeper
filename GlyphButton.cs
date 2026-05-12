// File: GlyphButton.cs
// Purpose: Provides a custom owner-drawn WinForms button optimized for compact glyph-only toolbar actions.

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
}
