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
}
