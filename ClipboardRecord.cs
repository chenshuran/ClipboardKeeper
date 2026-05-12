// File: ClipboardRecord.cs
// Purpose: Represents one saved clipboard item and exposes XML-friendly encrypted fields plus convenience properties.

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
