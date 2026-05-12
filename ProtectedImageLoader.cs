// File: ProtectedImageLoader.cs
// Purpose: Loads an encrypted saved image file and returns a disposable bitmap copy for UI display.

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
}
