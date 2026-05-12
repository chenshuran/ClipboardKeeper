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
    public sealed class ClipboardHistory
    {
        public ClipboardHistory()
        {
            Records = new List<ClipboardRecord>();
        }

        public List<ClipboardRecord> Records { get; set; }
    }
}
