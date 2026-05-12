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
}
