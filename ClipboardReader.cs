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
}
